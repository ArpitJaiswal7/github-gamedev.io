#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <ImageIO/ImageIO.h>
#import <Network/Network.h>

extern "C" {
    void UnitySendMessage(const char* obj, const char* method, const char* msg);
}

static const char* kUnityReceiverObject = "__NativeImagePipelineReceiver";

static NSString* sCacheDirPath = nil;
static NSString* sRomDirPath = nil;
static int sDefaultIconSizePx = 256;
static int sDefaultFullSizePx = 1080;
static float sJpegQuality = 0.85f;
static int sMaxRetries = 2;

static NSOperationQueue* sDownloadQueue = nil;
static NSMutableDictionary<NSString*, NSOperation*>* sActiveOperations = nil;
static NSLock* sOperationsLock = nil;
static nw_path_monitor_t sPathMonitor = nil;
static BOOL sIsOnline = YES;

// Helper to map our C# RequestPriority (0..3) to NSOperationQueuePriority
static NSOperationQueuePriority MapPriority(int priority) {
    switch (priority) {
        case 3: return NSOperationQueuePriorityVeryHigh; // Critical
        case 2: return NSOperationQueuePriorityHigh;     // High (Viewport)
        case 1: return NSOperationQueuePriorityNormal;   // Normal
        default: return NSOperationQueuePriorityLow;     // Low (Background)
    }
}

static void EnsureExcludedFromiCloudBackup(NSString* filePath) {
    if (!filePath) return;
    NSURL* fileUrl = [NSURL fileURLWithPath:filePath];
    if (fileUrl) {
        NSError* error = nil;
        [fileUrl setResourceValue:@YES forKey:NSURLIsExcludedFromBackupKey error:&error];
    }
}

static NSString* FindExistingFile(NSString* requestKey, int sizePx, int* outStorageTier) {
    NSString* fileName = [NSString stringWithFormat:@"%@_%d.jpg", requestKey, sizePx];
    NSFileManager* fm = [NSFileManager defaultManager];

    if (sRomDirPath != nil) {
        NSString* romFile = [sRomDirPath stringByAppendingPathComponent:fileName];
        if ([fm fileExistsAtPath:romFile]) {
            NSDictionary* attrs = [fm attributesOfItemAtPath:romFile error:nil];
            if ([attrs fileSize] > 0) {
                if (outStorageTier) *outStorageTier = 1;
                return romFile;
            }
        }
    }

    if (sCacheDirPath != nil) {
        NSString* cacheFile = [sCacheDirPath stringByAppendingPathComponent:fileName];
        if ([fm fileExistsAtPath:cacheFile]) {
            NSDictionary* attrs = [fm attributesOfItemAtPath:cacheFile error:nil];
            if ([attrs fileSize] > 0) {
                if (outStorageTier) *outStorageTier = 0;
                return cacheFile;
            }
        }
    }
    return nil;
}

// Hardware-accelerated downscaling + alpha stripping -> opaque JPEG bytes
static NSData* TranscodeAndResizeToJpegData(NSData* sourceData, int maxDimensionPx, float quality) {
    if (!sourceData || sourceData.length == 0 || maxDimensionPx <= 0) return nil;

    CGImageSourceRef imageSource = CGImageSourceCreateWithData((__bridge CFDataRef)sourceData, NULL);
    if (!imageSource) return nil;

    NSDictionary* thumbOpts = @{
        (id)kCGImageSourceCreateThumbnailFromImageAlways : @YES,
        (id)kCGImageSourceShouldCacheImmediately : @YES,
        (id)kCGImageSourceCreateThumbnailWithTransform : @YES,
        (id)kCGImageSourceThumbnailMaxPixelSize : @(maxDimensionPx)
    };

    CGImageRef scaledCgImage = CGImageSourceCreateThumbnailAtIndex(imageSource, 0, (__bridge CFDictionaryRef)thumbOpts);
    CFRelease(imageSource);
    if (!scaledCgImage) return nil;

    size_t width = CGImageGetWidth(scaledCgImage);
    size_t height = CGImageGetHeight(scaledCgImage);

    // Flatten onto opaque white RGB context so transparent PNGs become clean JPEGs
    CGColorSpaceRef colorSpace = CGColorSpaceCreateDeviceRGB();
    CGContextRef ctx = CGBitmapContextCreate(
        NULL,
        width,
        height,
        8,
        width * 4,
        colorSpace,
        kCGImageAlphaNoneSkipLast | kCGBitmapByteOrder32Big);
    CGColorSpaceRelease(colorSpace);

    if (!ctx) {
        CGImageRelease(scaledCgImage);
        return nil;
    }

    CGContextSetRGBFillColor(ctx, 1.0, 1.0, 1.0, 1.0);
    CGContextFillRect(ctx, CGRectMake(0, 0, width, height));
    CGContextDrawImage(ctx, CGRectMake(0, 0, width, height), scaledCgImage);

    CGImageRef opaqueCgImage = CGBitmapContextCreateImage(ctx);
    CGContextRelease(ctx);
    CGImageRelease(scaledCgImage);

    if (!opaqueCgImage) return nil;

    UIImage* finalUiImage = [UIImage imageWithCGImage:opaqueCgImage];
    NSData* jpegData = UIImageJPEGRepresentation(finalUiImage, quality);
    CGImageRelease(opaqueCgImage);

    return jpegData;
}

static void SendDownloadSuccess(NSString* key, long long iconBytes, long long fullBytes, long long durationMs, int storageTier) {
    NSString* json = [NSString stringWithFormat:
        @"{\"requestKey\":\"%@\",\"success\":true,\"iconSizeBytes\":%lld,\"fullSizeBytes\":%lld,\"durationMs\":%lld,\"storageTier\":%d,\"errorMessage\":\"\"}",
        key, iconBytes, fullBytes, durationMs, storageTier];
    UnitySendMessage(kUnityReceiverObject, "OnDownloadResultEvent", [json UTF8String]);
}

static void SendDownloadFailure(NSString* key, NSString* errorMsg, long long durationMs) {
    NSString* safeErr = [[errorMsg ?: @"Unknown error" stringByReplacingOccurrencesOfString:@"\"" withString:@"'"]
                         stringByReplacingOccurrencesOfString:@"\n" withString:@" "];
    NSString* json = [NSString stringWithFormat:
        @"{\"requestKey\":\"%@\",\"success\":false,\"iconSizeBytes\":0,\"fullSizeBytes\":0,\"durationMs\":%lld,\"storageTier\":0,\"errorMessage\":\"%@\"}",
        key, durationMs, safeErr];
    UnitySendMessage(kUnityReceiverObject, "OnDownloadResultEvent", [json UTF8String]);
}

static void SendCustomSizeResult(NSString* key, int sizePx, BOOL success, long long sizeBytes, long long durationMs, NSString* errorMsg) {
    NSString* safeErr = [[errorMsg ?: @"" stringByReplacingOccurrencesOfString:@"\"" withString:@"'"]
                         stringByReplacingOccurrencesOfString:@"\n" withString:@" "];
    NSString* json = [NSString stringWithFormat:
        @"{\"requestKey\":\"%@\",\"sizePx\":%d,\"success\":%@,\"sizeBytes\":%lld,\"durationMs\":%lld,\"errorMessage\":\"%@\"}",
        key, sizePx, success ? @"true" : @"false", sizeBytes, durationMs, safeErr];
    UnitySendMessage(kUnityReceiverObject, "OnCustomSizeResultEvent", [json UTF8String]);
}

extern "C" {

    void NIP_Initialize(
        const char* cacheDir,
        const char* romDir,
        int checkIntervalSec,
        int maxConcurrentWorkers,
        int iconSizePx,
        int fullSizePx,
        int jpegQuality,
        int maxRetries) {

        sCacheDirPath = [NSString stringWithUTF8String:cacheDir];
        sRomDirPath = [NSString stringWithUTF8String:romDir];
        sDefaultIconSizePx = iconSizePx > 0 ? iconSizePx : 256;
        sDefaultFullSizePx = fullSizePx > 0 ? fullSizePx : 1080;
        sJpegQuality = fmaxf(0.5f, fminf(1.0f, jpegQuality / 100.0f));
        sMaxRetries = MAX(0, maxRetries);

        NSFileManager* fm = [NSFileManager defaultManager];
        [fm createDirectoryAtPath:sCacheDirPath withIntermediateDirectories:YES attributes:nil error:nil];
        [fm createDirectoryAtPath:sRomDirPath withIntermediateDirectories:YES attributes:nil error:nil];
        EnsureExcludedFromiCloudBackup(sRomDirPath);

        if (!sOperationsLock) {
            sOperationsLock = [[NSLock alloc] init];
            sActiveOperations = [[NSMutableDictionary alloc] init];
        }

        if (!sDownloadQueue) {
            sDownloadQueue = [[NSOperationQueue alloc] init];
            sDownloadQueue.name = @"com.studio.nativeimagepipeline.queue";
            sDownloadQueue.maxConcurrentOperationCount = MAX(1, MIN(8, maxConcurrentWorkers));
        }

        if (!sPathMonitor) {
            sPathMonitor = nw_path_monitor_create();
            nw_path_monitor_set_queue(sPathMonitor, dispatch_get_global_queue(QOS_CLASS_UTILITY, 0));
            nw_path_monitor_set_update_handler(sPathMonitor, ^(nw_path_t path) {
                nw_path_status_t status = nw_path_get_status(path);
                BOOL online = (status == nw_path_status_satisfied);
                sIsOnline = online;
                [sDownloadQueue setSuspended:!online];
                UnitySendMessage(kUnityReceiverObject, "OnConnectivityEvent", online ? "1" : "0");
            });
            nw_path_monitor_start(sPathMonitor);
        }
    }

    bool NIP_IsConnected() {
        return sIsOnline;
    }

    void NIP_EnqueueDownload(const char* requestKeyCStr, const char* urlCStr, int priority, int targetStorageTier) {
        if (!requestKeyCStr || !urlCStr) return;
        NSString* requestKey = [NSString stringWithUTF8String:requestKeyCStr];
        NSString* urlString = [NSString stringWithUTF8String:urlCStr];

        int foundTier = 0;
        NSString* existingIcon = FindExistingFile(requestKey, sDefaultIconSizePx, &foundTier);
        NSString* existingFull = FindExistingFile(requestKey, sDefaultFullSizePx, &foundTier);
        if (existingIcon && existingFull) {
            NSFileManager* fm = [NSFileManager defaultManager];
            long long iconBytes = [[fm attributesOfItemAtPath:existingIcon error:nil] fileSize];
            long long fullBytes = [[fm attributesOfItemAtPath:existingFull error:nil] fileSize];
            SendDownloadSuccess(requestKey, iconBytes, fullBytes, 0, foundTier);
            return;
        }

        [sOperationsLock lock];
        NSOperation* existingOp = sActiveOperations[requestKey];
        if (existingOp) {
            existingOp.queuePriority = MapPriority(priority);
            [sOperationsLock unlock];
            return;
        }

        NSBlockOperation* op = [[NSBlockOperation alloc] init];
        __weak NSBlockOperation* weakOp = op;
        op.queuePriority = MapPriority(priority);

        [op addExecutionBlock:^{
            if (weakOp.isCancelled) return;
            NSDate* startTime = [NSDate date];
            NSData* downloadedData = nil;
            NSString* lastError = nil;

            for (int attempt = 0; attempt <= sMaxRetries; attempt++) {
                if (weakOp.isCancelled) return;

                NSURL* url = [NSURL URLWithString:urlString];
                if (!url) {
                    lastError = @"Malformed URL";
                    break;
                }

                NSURLRequest* req = [NSURLRequest requestWithURL:url
                                                     cachePolicy:NSURLRequestReloadIgnoringLocalCacheData
                                                 timeoutInterval:15.0];
                dispatch_semaphore_t sem = dispatch_semaphore_create(0);
                __block NSData* respData = nil;
                __block NSError* netErr = nil;
                __block NSInteger statusCode = 0;

                NSURLSessionDataTask* task = [[NSURLSession sharedSession] dataTaskWithRequest:req
                    completionHandler:^(NSData* data, NSURLResponse* response, NSError* error) {
                        respData = data;
                        netErr = error;
                        if ([response isKindOfClass:[NSHTTPURLResponse class]]) {
                            statusCode = [(NSHTTPURLResponse*)response statusCode];
                        }
                        dispatch_semaphore_signal(sem);
                    }];
                [task resume];
                dispatch_semaphore_wait(sem, DISPATCH_TIME_FOREVER);

                if (netErr == nil && statusCode >= 200 && statusCode < 300 && respData.length > 0) {
                    downloadedData = respData;
                    break;
                } else {
                    lastError = netErr ? netErr.localizedDescription : [NSString stringWithFormat:@"HTTP %ld", (long)statusCode];
                    if (statusCode == 404) break;
                    [NSThread sleepForTimeInterval:0.5 * (attempt + 1)];
                }
            }

            [sOperationsLock lock];
            [sActiveOperations removeObjectForKey:requestKey];
            [sOperationsLock unlock];

            if (weakOp.isCancelled) return;

            long long elapsedMs = (long long)([[NSDate date] timeIntervalSinceDate:startTime] * 1000.0);
            if (!downloadedData) {
                SendDownloadFailure(requestKey, lastError, elapsedMs);
                return;
            }

            NSData* fullJpeg = TranscodeAndResizeToJpegData(downloadedData, sDefaultFullSizePx, sJpegQuality);
            NSData* iconJpeg = TranscodeAndResizeToJpegData(fullJpeg ?: downloadedData, sDefaultIconSizePx, sJpegQuality);

            if (!fullJpeg || !iconJpeg) {
                SendDownloadFailure(requestKey, @"Failed decoding/transcoding image to JPEG", elapsedMs);
                return;
            }

            NSString* targetDir = (targetStorageTier == 1) ? sRomDirPath : sCacheDirPath;
            NSString* fullPath = [targetDir stringByAppendingPathComponent:[NSString stringWithFormat:@"%@_%d.jpg", requestKey, sDefaultFullSizePx]];
            NSString* iconPath = [targetDir stringByAppendingPathComponent:[NSString stringWithFormat:@"%@_%d.jpg", requestKey, sDefaultIconSizePx]];

            [fullJpeg writeToFile:fullPath atomically:YES];
            [iconJpeg writeToFile:iconPath atomically:YES];

            if (targetStorageTier == 1) {
                EnsureExcludedFromiCloudBackup(fullPath);
                EnsureExcludedFromiCloudBackup(iconPath);
            }

            long long totalMs = (long long)([[NSDate date] timeIntervalSinceDate:startTime] * 1000.0);
            SendDownloadSuccess(requestKey, iconJpeg.length, fullJpeg.length, totalMs, targetStorageTier);
        }];

        sActiveOperations[requestKey] = op;
        [sOperationsLock unlock];
        [sDownloadQueue addOperation:op];
    }

    void NIP_UpdatePriority(const char* requestKeyCStr, int newPriority) {
        if (!requestKeyCStr) return;
        NSString* requestKey = [NSString stringWithUTF8String:requestKeyCStr];
        [sOperationsLock lock];
        NSOperation* op = sActiveOperations[requestKey];
        if (op && !op.isExecuting) {
            op.queuePriority = MapPriority(newPriority);
        }
        [sOperationsLock unlock];
    }

    void NIP_CancelOrRemoveRequest(const char* requestKeyCStr) {
        if (!requestKeyCStr) return;
        NSString* requestKey = [NSString stringWithUTF8String:requestKeyCStr];
        [sOperationsLock lock];
        NSOperation* op = sActiveOperations[requestKey];
        if (op) {
            [op cancel];
            [sActiveOperations removeObjectForKey:requestKey];
        }
        [sOperationsLock unlock];
    }

    void NIP_RequestCustomSize(const char* requestKeyCStr, const char* fallbackUrlCStr, int customSizePx, int priority) {
        if (!requestKeyCStr || customSizePx <= 0) return;
        NSString* requestKey = [NSString stringWithUTF8String:requestKeyCStr];
        NSString* fallbackUrl = fallbackUrlCStr ? [NSString stringWithUTF8String:fallbackUrlCStr] : @"";

        NSBlockOperation* op = [NSBlockOperation blockOperationWithBlock:^{
            NSDate* startTime = [NSDate date];
            int foundTier = 0;
            NSString* existingCustom = FindExistingFile(requestKey, customSizePx, &foundTier);
            if (existingCustom) {
                long long sz = [[[NSFileManager defaultManager] attributesOfItemAtPath:existingCustom error:nil] fileSize];
                SendCustomSizeResult(requestKey, customSizePx, YES, sz, 0, @"");
                return;
            }

            NSString* existingFull = FindExistingFile(requestKey, sDefaultFullSizePx, &foundTier);
            NSData* sourceData = nil;
            NSString* targetDir = (foundTier == 1) ? sRomDirPath : sCacheDirPath;

            if (customSizePx <= sDefaultFullSizePx && existingFull) {
                sourceData = [NSData dataWithContentsOfFile:existingFull];
            } else if (fallbackUrl.length > 0) {
                NSURL* url = [NSURL URLWithString:fallbackUrl];
                if (url) sourceData = [NSData dataWithContentsOfURL:url];
            } else if (existingFull) {
                sourceData = [NSData dataWithContentsOfFile:existingFull];
            }

            if (!sourceData) {
                SendCustomSizeResult(requestKey, customSizePx, NO, 0, 0, @"No local 1080px file or fallback URL available");
                return;
            }

            NSData* customJpeg = TranscodeAndResizeToJpegData(sourceData, customSizePx, sJpegQuality);
            if (!customJpeg) {
                SendCustomSizeResult(requestKey, customSizePx, NO, 0, 0, @"Failed resizing custom JPEG");
                return;
            }

            NSString* outPath = [targetDir stringByAppendingPathComponent:[NSString stringWithFormat:@"%@_%d.jpg", requestKey, customSizePx]];
            [customJpeg writeToFile:outPath atomically:YES];
            if (foundTier == 1) EnsureExcludedFromiCloudBackup(outPath);

            long long elapsedMs = (long long)([[NSDate date] timeIntervalSinceDate:startTime] * 1000.0);
            SendCustomSizeResult(requestKey, customSizePx, YES, customJpeg.length, elapsedMs, @"");
        }];

        op.queuePriority = MapPriority(priority);
        [sDownloadQueue addOperation:op];
    }
}
