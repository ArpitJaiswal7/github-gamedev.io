package com.studio.nativeimagepipeline;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Rect;
import android.net.ConnectivityManager;
import android.net.Network;
import android.net.NetworkCapabilities;
import android.os.Build;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import java.io.BufferedInputStream;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.io.FilenameFilter;
import java.io.InputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.Executors;
import java.util.concurrent.PriorityBlockingQueue;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.ThreadPoolExecutor;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Native Android Background Image Pipeline for Unity.
 * - Actively monitors internet connectivity at a configured interval and pauses/resumes queue automatically.
 * - Manages 100+ image requests via a de-duplicated PriorityBlockingQueue (supports live viewport promotion).
 * - Downloads source PNG/JPG once, strips alpha channel, downscales to 256px (Icon) and 1080px (FullSize),
 *   and saves compressed JPEGs atomically to Volatile Cache or Permanent ROM.
 * - Provides helper functions to generate custom sizes (e.g. 512px) directly from local 1080px cache if present.
 */
public class NativeImageBridge {
    private static final String TAG = "NativeImageBridge";
    private static final String UNITY_RECEIVER_OBJECT = "__NativeImagePipelineReceiver";

    private static File cacheDir;
    private static File romDir;
    private static int defaultIconSizePx = 256;
    private static int defaultFullSizePx = 1080;
    private static int jpegQuality = 85;
    private static int maxRetries = 2;
    private static int fillColorRgb = Color.WHITE;

    private static final AtomicBoolean isInitialized = new AtomicBoolean(false);
    private static final AtomicBoolean isOnline = new AtomicBoolean(false);
    private static final AtomicLong sequenceGenerator = new AtomicLong(0);

    private static PriorityBlockingQueue<Runnable> priorityQueue;
    private static ThreadPoolExecutor workerExecutor;
    private static ScheduledExecutorService connectivityScheduler;

    // Tracks queued/active tasks by requestKey for de-duplication, priority promotion, and cancellation
    private static final ConcurrentHashMap<String, DownloadTask> activeTasks = new ConcurrentHashMap<>();

    public static void initialize(
            String cacheDirPath,
            String romDirPath,
            int checkIntervalSec,
            int maxConcurrentWorkers,
            int iconSizePx,
            int fullSizePx,
            int quality,
            int retryLimit) {

        cacheDir = new File(cacheDirPath);
        romDir = new File(romDirPath);
        if (!cacheDir.exists()) cacheDir.mkdirs();
        if (!romDir.exists()) romDir.mkdirs();

        defaultIconSizePx = iconSizePx > 0 ? iconSizePx : 256;
        defaultFullSizePx = fullSizePx > 0 ? fullSizePx : 1080;
        jpegQuality = Math.max(50, Math.min(100, quality));
        maxRetries = Math.max(0, retryLimit);

        if (isInitialized.compareAndSet(false, true)) {
            int workers = Math.max(1, Math.min(8, maxConcurrentWorkers));
            priorityQueue = new PriorityBlockingQueue<>(128);
            workerExecutor = new ThreadPoolExecutor(
                    workers,
                    workers,
                    30L,
                    TimeUnit.SECONDS,
                    priorityQueue);

            connectivityScheduler = Executors.newSingleThreadScheduledExecutor();
            int interval = Math.max(1, checkIntervalSec);
            connectivityScheduler.scheduleAtFixedRate(
                    NativeImageBridge::checkConnectivityAndPumpQueue,
                    0,
                    interval,
                    TimeUnit.SECONDS);
        } else {
            checkConnectivityAndPumpQueue();
        }
    }

    public static boolean isConnected() {
        return isOnline.get();
    }

    public static void enqueueDownload(String requestKey, String url, int priority, int targetStorageTier) {
        if (requestKey == null || url == null) return;

        // Fast-path: Check if both 256px and 1080px already exist in ROM or Cache
        File existingIcon = findExistingFile(requestKey, defaultIconSizePx);
        File existingFull = findExistingFile(requestKey, defaultFullSizePx);
        if (existingIcon != null && existingFull != null) {
            int foundTier = existingFull.getParentFile().equals(romDir) ? 1 : 0;
            sendDownloadSuccess(requestKey, existingIcon.length(), existingFull.length(), 0L, foundTier);
            return;
        }

        DownloadTask existingTask = activeTasks.get(requestKey);
        if (existingTask != null) {
            if (priority > existingTask.priority) {
                updatePriority(requestKey, priority);
            }
            return;
        }

        DownloadTask task = new DownloadTask(
                requestKey,
                url,
                priority,
                targetStorageTier,
                sequenceGenerator.incrementAndGet());

        activeTasks.put(requestKey, task);
        workerExecutor.execute(task);
    }

    public static void updatePriority(String requestKey, int newPriority) {
        DownloadTask existing = activeTasks.get(requestKey);
        if (existing != null && !existing.isStarted.get() && existing.priority != newPriority) {
            // Remove and re-insert into PriorityBlockingQueue with updated priority
            if (priorityQueue.remove(existing)) {
                existing.priority = newPriority;
                priorityQueue.offer(existing);
            }
        }
    }

    public static void cancelOrRemoveRequest(String requestKey) {
        DownloadTask existing = activeTasks.remove(requestKey);
        if (existing != null) {
            existing.isCancelled.set(true);
            priorityQueue.remove(existing);
        }
        deleteAllTiersInDir(cacheDir, requestKey);
    }

    public static void requestCustomSize(String requestKey, String fallbackUrl, int customSizePx, int priority) {
        if (requestKey == null || customSizePx <= 0) return;

        workerExecutor.execute(new CustomSizeTask(
                requestKey,
                fallbackUrl,
                customSizePx,
                priority,
                sequenceGenerator.incrementAndGet()));
    }

    public static boolean promoteToROM(String requestKey) {
        return moveAllTiersBetweenDirs(cacheDir, romDir, requestKey);
    }

    public static boolean demoteToCache(String requestKey) {
        return moveAllTiersBetweenDirs(romDir, cacheDir, requestKey);
    }

    public static void clearVolatileCache() {
        if (cacheDir != null && cacheDir.exists()) {
            File[] files = cacheDir.listFiles();
            if (files != null) {
                for (File f : files) {
                    if (f.isFile()) {
                        f.delete();
                    }
                }
            }
        }
    }

    // =========================================================================
    // Active Connectivity Monitor
    // =========================================================================

    private static void checkConnectivityAndPumpQueue() {
        boolean currentlyOnline = checkOSNetworkReachability();
        boolean previous = isOnline.getAndSet(currentlyOnline);
        if (currentlyOnline != previous) {
            UnityPlayer.UnitySendMessage(
                    UNITY_RECEIVER_OBJECT,
                    "OnConnectivityEvent",
                    currentlyOnline ? "1" : "0");
        }
    }

    private static boolean checkOSNetworkReachability() {
        try {
            Context context = UnityPlayer.currentActivity.getApplicationContext();
            ConnectivityManager cm = (ConnectivityManager) context.getSystemService(Context.CONNECTIVITY_SERVICE);
            if (cm == null) return false;

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                Network network = cm.getActiveNetwork();
                if (network == null) return false;
                NetworkCapabilities caps = cm.getNetworkCapabilities(network);
                return caps != null &&
                        (caps.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) ||
                         caps.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR) ||
                         caps.hasTransport(NetworkCapabilities.TRANSPORT_ETHERNET)) &&
                        caps.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET);
            } else {
                android.net.NetworkInfo activeInfo = cm.getActiveNetworkInfo();
                return activeInfo != null && activeInfo.isConnected();
            }
        } catch (Exception e) {
            Log.w(TAG, "Connectivity check error: " + e.getMessage());
            return true;
        }
    }

    // =========================================================================
    // Priority Task Base & Implementations
    // =========================================================================

    private static abstract class PrioritizedRunnable implements Runnable, Comparable<PrioritizedRunnable> {
        volatile int priority;
        final long sequence;

        PrioritizedRunnable(int priority, long sequence) {
            this.priority = priority;
            this.sequence = sequence;
        }

        @Override
        public int compareTo(PrioritizedRunnable other) {
            // Higher priority value comes first
            int cmp = Integer.compare(other.priority, this.priority);
            if (cmp != 0) return cmp;
            return Long.compare(this.sequence, other.sequence);
        }
    }

    private static final class DownloadTask extends PrioritizedRunnable {
        final String requestKey;
        final String url;
        final int targetStorageTier;
        final AtomicBoolean isStarted = new AtomicBoolean(false);
        final AtomicBoolean isCancelled = new AtomicBoolean(false);

        DownloadTask(String requestKey, String url, int priority, int targetStorageTier, long sequence) {
            super(priority, sequence);
            this.requestKey = requestKey;
            this.url = url;
            this.targetStorageTier = targetStorageTier;
        }

        @Override
        public void run() {
            if (isCancelled.get()) {
                activeTasks.remove(requestKey);
                return;
            }

            // Wait cleanly if offline; auto-resumes when connectivity returns
            while (!isOnline.get() && !isCancelled.get()) {
                try {
                    Thread.sleep(1000L);
                } catch (InterruptedException e) {
                    Thread.currentThread().interrupt();
                    return;
                }
            }

            if (isCancelled.get()) {
                activeTasks.remove(requestKey);
                return;
            }

            isStarted.set(true);
            long startTimeMs = System.currentTimeMillis();
            int attempt = 0;
            String lastError = "";

            while (attempt <= maxRetries && !isCancelled.get()) {
                attempt++;
                try {
                    byte[] rawImageBytes = downloadBytesFromUrl(url, isCancelled);
                    if (isCancelled.get() || rawImageBytes == null) {
                        activeTasks.remove(requestKey);
                        return;
                    }

                    File targetDir = (targetStorageTier == 1) ? romDir : cacheDir;
                    File outFullFile = new File(targetDir, requestKey + "_" + defaultFullSizePx + ".jpg");
                    File outIconFile = new File(targetDir, requestKey + "_" + defaultIconSizePx + ".jpg");

                    // 1. Decode & downscale to 1080px FullSize first (with inSampleSize hardware efficiency)
                    Bitmap fullBitmap = decodeAndScaleOpaqueBitmap(rawImageBytes, defaultFullSizePx);
                    if (fullBitmap == null) {
                        throw new IllegalArgumentException("Corrupt or unsupported image format for URL: " + url);
                    }

                    long fullBytes = writeBitmapAsJpegAtomically(fullBitmap, outFullFile, jpegQuality);

                    // 2. Downscale from the 1080px bitmap to 256px Icon (super fast, no second decode)
                    Bitmap iconBitmap = scaleBitmapPreserveAspect(fullBitmap, defaultIconSizePx);
                    long iconBytes = writeBitmapAsJpegAtomically(iconBitmap, outIconFile, jpegQuality);

                    if (iconBitmap != fullBitmap) {
                        iconBitmap.recycle();
                    }
                    fullBitmap.recycle();

                    long elapsedMs = System.currentTimeMillis() - startTimeMs;
                    activeTasks.remove(requestKey);
                    sendDownloadSuccess(requestKey, iconBytes, fullBytes, elapsedMs, targetStorageTier);
                    return;

                } catch (Exception ex) {
                    lastError = ex.getMessage() != null ? ex.getMessage() : ex.getClass().getSimpleName();
                    if (lastError.contains("HTTP 404") || lastError.contains("Corrupt")) {
                        // Fatal error: do not retry faulty URLs
                        break;
                    }
                    try {
                        Thread.sleep(600L * attempt);
                    } catch (InterruptedException ignored) {
                        Thread.currentThread().interrupt();
                        break;
                    }
                }
            }

            activeTasks.remove(requestKey);
            if (!isCancelled.get()) {
                long elapsedMs = System.currentTimeMillis() - startTimeMs;
                sendDownloadFailure(requestKey, lastError, elapsedMs);
            }
        }
    }

    private static final class CustomSizeTask extends PrioritizedRunnable {
        final String requestKey;
        final String fallbackUrl;
        final int customSizePx;

        CustomSizeTask(String requestKey, String fallbackUrl, int customSizePx, int priority, long sequence) {
            super(priority, sequence);
            this.requestKey = requestKey;
            this.fallbackUrl = fallbackUrl;
            this.customSizePx = customSizePx;
        }

        @Override
        public void run() {
            long startMs = System.currentTimeMillis();
            try {
                File existingCustom = findExistingFile(requestKey, customSizePx);
                if (existingCustom != null && existingCustom.length() > 0) {
                    sendCustomSizeSuccess(requestKey, customSizePx, existingCustom.length(), 0L);
                    return;
                }

                // Prefer downscaling from local 1080px JPEG ONLY if customSizePx <= defaultFullSizePx (e.g. 512px).
                // For larger tiers like 2048px (Ultra2048), fetch from the remote high-res source so we never blurry-upscale!
                File existingFull = findExistingFile(requestKey, defaultFullSizePx);
                Bitmap sourceBitmap = null;
                File targetDir = (existingFull != null && existingFull.exists()) ? existingFull.getParentFile() : cacheDir;

                if (customSizePx <= defaultFullSizePx && existingFull != null && existingFull.exists()) {
                    sourceBitmap = BitmapFactory.decodeFile(existingFull.getAbsolutePath());
                } else if (fallbackUrl != null && !fallbackUrl.isEmpty()) {
                    byte[] rawBytes = downloadBytesFromUrl(fallbackUrl, new AtomicBoolean(false));
                    sourceBitmap = decodeAndScaleOpaqueBitmap(rawBytes, customSizePx);
                } else if (existingFull != null && existingFull.exists()) {
                    sourceBitmap = BitmapFactory.decodeFile(existingFull.getAbsolutePath());
                }

                if (sourceBitmap == null) {
                    sendCustomSizeFailure(requestKey, customSizePx, "Unable to decode source for custom size");
                    return;
                }

                Bitmap scaled = scaleBitmapPreserveAspect(sourceBitmap, customSizePx);
                File outCustomFile = new File(targetDir, requestKey + "_" + customSizePx + ".jpg");
                long writtenBytes = writeBitmapAsJpegAtomically(scaled, outCustomFile, jpegQuality);

                if (scaled != sourceBitmap) scaled.recycle();
                sourceBitmap.recycle();

                long elapsedMs = System.currentTimeMillis() - startMs;
                sendCustomSizeSuccess(requestKey, customSizePx, writtenBytes, elapsedMs);
            } catch (Exception ex) {
                sendCustomSizeFailure(requestKey, customSizePx, ex.getMessage());
            }
        }
    }

    // =========================================================================
    // HTTP Download & PNG -> JPG Resizing Helpers
    // =========================================================================

    private static byte[] downloadBytesFromUrl(String urlString, AtomicBoolean cancelFlag) throws Exception {
        HttpURLConnection conn = null;
        try {
            URL url = new URL(urlString);
            conn = (HttpURLConnection) url.openConnection();
            conn.setConnectTimeout(10000);
            conn.setReadTimeout(15000);
            conn.setInstanceFollowRedirects(true);
            conn.setRequestProperty("User-Agent", "Unity-NativeImagePipeline/1.0");

            int code = conn.getResponseCode();
            if (code < 200 || code >= 300) {
                throw new Exception("HTTP " + code + " for " + urlString);
            }

            try (InputStream in = new BufferedInputStream(conn.getInputStream());
                 ByteArrayOutputStream out = new ByteArrayOutputStream(256 * 1024)) {
                byte[] buffer = new byte[16 * 1024];
                int bytesRead;
                while ((bytesRead = in.read(buffer)) != -1) {
                    if (cancelFlag.get()) return null;
                    out.write(buffer, 0, bytesRead);
                }
                return out.toByteArray();
            }
        } finally {
            if (conn != null) {
                conn.disconnect();
            }
        }
    }

    private static Bitmap decodeAndScaleOpaqueBitmap(byte[] data, int maxDimensionPx) {
        if (data == null || data.length == 0) return null;

        // Bounds decode first
        BitmapFactory.Options boundsOpts = new BitmapFactory.Options();
        boundsOpts.inJustDecodeBounds = true;
        BitmapFactory.decodeByteArray(data, 0, data.length, boundsOpts);

        int srcW = boundsOpts.outWidth;
        int srcH = boundsOpts.outHeight;
        if (srcW <= 0 || srcH <= 0) return null;

        int sampleSize = 1;
        int largestSide = Math.max(srcW, srcH);
        while ((largestSide / (sampleSize * 2)) >= maxDimensionPx) {
            sampleSize *= 2;
        }

        BitmapFactory.Options decodeOpts = new BitmapFactory.Options();
        decodeOpts.inSampleSize = sampleSize;
        decodeOpts.inPreferredConfig = Bitmap.Config.ARGB_8888;

        Bitmap decoded = BitmapFactory.decodeByteArray(data, 0, data.length, decodeOpts);
        if (decoded == null) return null;

        Bitmap scaled = scaleBitmapPreserveAspect(decoded, maxDimensionPx);
        if (scaled != decoded) {
            decoded.recycle();
        }

        // Strip PNG alpha by compositing onto opaque RGB_565/ARGB_8888 canvas before JPEG encode
        if (scaled.hasAlpha()) {
            Bitmap opaque = Bitmap.createBitmap(scaled.getWidth(), scaled.getHeight(), Bitmap.Config.ARGB_8888);
            Canvas canvas = new Canvas(opaque);
            canvas.drawColor(fillColorRgb);
            canvas.drawBitmap(scaled, 0f, 0f, null);
            scaled.recycle();
            return opaque;
        }

        return scaled;
    }

    private static Bitmap scaleBitmapPreserveAspect(Bitmap src, int maxDimensionPx) {
        int w = src.getWidth();
        int h = src.getHeight();
        int maxSide = Math.max(w, h);
        if (maxSide <= maxDimensionPx) {
            return src;
        }

        float ratio = (float) maxDimensionPx / (float) maxSide;
        int targetW = Math.max(1, Math.round(w * ratio));
        int targetH = Math.max(1, Math.round(h * ratio));

        Bitmap opaqueOut = Bitmap.createBitmap(targetW, targetH, Bitmap.Config.ARGB_8888);
        Canvas canvas = new Canvas(opaqueOut);
        canvas.drawColor(fillColorRgb);
        Rect srcRect = new Rect(0, 0, w, h);
        Rect dstRect = new Rect(0, 0, targetW, targetH);
        canvas.drawBitmap(src, srcRect, dstRect, null);
        return opaqueOut;
    }

    private static long writeBitmapAsJpegAtomically(Bitmap bitmap, File finalFile, int quality) throws Exception {
        File tempFile = new File(finalFile.getParentFile(), finalFile.getName() + ".tmp");
        try (FileOutputStream fos = new FileOutputStream(tempFile)) {
            bitmap.compress(Bitmap.CompressFormat.JPEG, quality, fos);
            fos.flush();
        }
        if (finalFile.exists()) {
            finalFile.delete();
        }
        if (!tempFile.renameTo(finalFile)) {
            throw new Exception("Failed to rename temp JPEG file to " + finalFile.getAbsolutePath());
        }
        return finalFile.length();
    }

    private static File findExistingFile(String requestKey, int sizePx) {
        String fileName = requestKey + "_" + sizePx + ".jpg";
        if (romDir != null) {
            File romFile = new File(romDir, fileName);
            if (romFile.exists() && romFile.length() > 0) return romFile;
        }
        if (cacheDir != null) {
            File cacheFile = new File(cacheDir, fileName);
            if (cacheFile.exists() && cacheFile.length() > 0) return cacheFile;
        }
        return null;
    }

    private static boolean moveAllTiersBetweenDirs(File srcDir, File dstDir, final String requestKey) {
        if (srcDir == null || dstDir == null || !srcDir.exists()) return false;
        if (!dstDir.exists()) dstDir.mkdirs();

        final String prefix = requestKey + "_";
        File[] files = srcDir.listFiles(new FilenameFilter() {
            @Override
            public boolean accept(File dir, String name) {
                return name.startsWith(prefix) && name.endsWith(".jpg");
            }
        });

        if (files == null || files.length == 0) return false;
        boolean movedAny = false;
        for (File srcFile : files) {
            File dstFile = new File(dstDir, srcFile.getName());
            if (dstFile.exists()) dstFile.delete();
            if (srcFile.renameTo(dstFile)) {
                movedAny = true;
            }
        }
        return movedAny;
    }

    private static void deleteAllTiersInDir(File dir, final String requestKey) {
        if (dir == null || !dir.exists()) return;
        final String prefix = requestKey + "_";
        File[] files = dir.listFiles((d, name) -> name.startsWith(prefix));
        if (files != null) {
            for (File f : files) f.delete();
        }
    }

    // =========================================================================
    // JSON Event Dispatchers to Unity C#
    // =========================================================================

    private static void sendDownloadSuccess(String key, long iconBytes, long fullBytes, long durationMs, int storageTier) {
        String json = String.format(
                "{\"requestKey\":\"%s\",\"success\":true,\"iconSizeBytes\":%d,\"fullSizeBytes\":%d,\"durationMs\":%d,\"storageTier\":%d,\"errorMessage\":\"\"}",
                escapeJson(key), iconBytes, fullBytes, durationMs, storageTier);
        UnityPlayer.UnitySendMessage(UNITY_RECEIVER_OBJECT, "OnDownloadResultEvent", json);
    }

    private static void sendDownloadFailure(String key, String error, long durationMs) {
        String json = String.format(
                "{\"requestKey\":\"%s\",\"success\":false,\"iconSizeBytes\":0,\"fullSizeBytes\":0,\"durationMs\":%d,\"storageTier\":0,\"errorMessage\":\"%s\"}",
                escapeJson(key), durationMs, escapeJson(error));
        UnityPlayer.UnitySendMessage(UNITY_RECEIVER_OBJECT, "OnDownloadResultEvent", json);
    }

    private static void sendCustomSizeSuccess(String key, int sizePx, long sizeBytes, long durationMs) {
        String json = String.format(
                "{\"requestKey\":\"%s\",\"sizePx\":%d,\"success\":true,\"sizeBytes\":%d,\"durationMs\":%d,\"errorMessage\":\"\"}",
                escapeJson(key), sizePx, sizeBytes, durationMs);
        UnityPlayer.UnitySendMessage(UNITY_RECEIVER_OBJECT, "OnCustomSizeResultEvent", json);
    }

    private static void sendCustomSizeFailure(String key, int sizePx, String error) {
        String json = String.format(
                "{\"requestKey\":\"%s\",\"sizePx\":%d,\"success\":false,\"sizeBytes\":0,\"durationMs\":0,\"errorMessage\":\"%s\"}",
                escapeJson(key), sizePx, escapeJson(error));
        UnityPlayer.UnitySendMessage(UNITY_RECEIVER_OBJECT, "OnCustomSizeResultEvent", json);
    }

    private static String escapeJson(String s) {
        if (s == null) return "";
        return s.replace("\\", "\\\\").replace("\"", "\\\"").replace("\n", " ").replace("\r", "");
    }
}
