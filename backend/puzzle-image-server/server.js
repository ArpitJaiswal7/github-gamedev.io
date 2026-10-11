/**
 * Calm Jigsaw — Puzzle Image Backend Server & Catalog API
 * Port: 4200 (http://localhost:4200)
 *
 * Features:
 * - Full CORS headers (Access-Control-Allow-Origin: *) for Unity WebGL (.jslib) & HTML5 Canvas
 * - Drag & Drop single/batch image upload (stores clean 1:1 square PNG/JPG files in storage/uploads/)
 * - Local folder scanner (POST /api/puzzles/scan-folder or browser webkitdirectory upload)
 * - Publish / Unpublish toggle, Category & ROM tier metadata
 * - API Helper endpoints:
 *     GET  /api/puzzles/urls       -> { count, urls: [...] } (all published image URLs)
 *     GET  /api/puzzles/published  -> PuzzleManifestResponse (compatible with Studio.NativeImagePipeline)
 *     GET  /api/manifest.json      -> Alias for /api/puzzles/published
 *     GET  /api/puzzles            -> All puzzles (published + drafts) for Admin UI
 *     GET  /api/puzzles/:id/image  -> Direct binary image stream with CORS
 *     POST /api/puzzles/upload     -> Upload 1 or more images (base64/dataURL)
 *     POST /api/puzzles/scan-folder-> Scan a server-side local folder path for images
 *     PATCH /api/puzzles/:id       -> Update published status, title, category, saveToRomOnDownload
 *     DELETE /api/puzzles/:id      -> Delete puzzle image and metadata
 * - Serves Client API Helper SDK at:
 *     GET  /sdk/puzzle-api-helper.js
 */

const http = require('http');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { execSync } = require('child_process');

const PORT = process.env.PORT ? Number(process.env.PORT) : 4200;
const ROOT_DIR = __dirname;
const STORAGE_DIR = path.join(ROOT_DIR, 'storage');
const UPLOADS_DIR = path.join(STORAGE_DIR, 'uploads');
const CATALOG_FILE = path.join(STORAGE_DIR, 'catalog.json');
const PUBLIC_DIR = path.join(ROOT_DIR, 'public');
const SDK_DIR = path.join(ROOT_DIR, 'sdk');
const GH_REPO_DIR = fs.existsSync(path.resolve(ROOT_DIR, '..', 'github-gamedev.io'))
  ? path.resolve(ROOT_DIR, '..', 'github-gamedev.io')
  : path.resolve(ROOT_DIR, '..', '..');
const LABS_DIR = path.join(GH_REPO_DIR, 'labs');
const GH_PUZZLES_DIR = path.join(GH_REPO_DIR, 'assets', 'puzzles');
const GH_UPLOADS_DIR = path.join(GH_PUZZLES_DIR, 'uploads');
const GH_PAGES_BASE_URL = 'https://arpitjaiswal7.github.io/github-gamedev.io/assets/puzzles';

// Ensure directories exist
[STORAGE_DIR, UPLOADS_DIR, PUBLIC_DIR, SDK_DIR, GH_PUZZLES_DIR, GH_UPLOADS_DIR].forEach(dir => {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
});

// MIME types map
const MIME_TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'application/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.webp': 'image/webp',
  '.svg': 'image/svg+xml',
  '.ico': 'image/x-icon',
  '.wasm': 'application/wasm',
  '.data': 'application/octet-stream'
};

/**
 * Load or initialize catalog.json
 */
function loadCatalog() {
  if (!fs.existsSync(CATALOG_FILE)) {
    const initial = {
      version: '1.0',
      updatedAt: new Date().toISOString(),
      puzzles: []
    };
    fs.writeFileSync(CATALOG_FILE, JSON.stringify(initial, null, 2), 'utf8');
    return initial;
  }
  try {
    const raw = fs.readFileSync(CATALOG_FILE, 'utf8');
    const parsed = JSON.parse(raw);
    if (!parsed || !Array.isArray(parsed.puzzles)) {
      return { version: '1.0', updatedAt: new Date().toISOString(), puzzles: [] };
    }
    return parsed;
  } catch (err) {
    return { version: '1.0', updatedAt: new Date().toISOString(), puzzles: [] };
  }
}

/**
 * Sync published images and manifest JSON to github-gamedev.io/assets/puzzles/
 * so live HTTPS GitHub Pages builds also have all published server images!
 */
function syncToGitHubPagesStaticMirror(catalog) {
  try {
    if (!fs.existsSync(GH_UPLOADS_DIR)) {
      fs.mkdirSync(GH_UPLOADS_DIR, { recursive: true });
    }
    const published = (catalog.puzzles || []).filter(p => p.published);
    for (const p of published) {
      if (!p.filename) continue;
      const src = path.join(UPLOADS_DIR, p.filename);
      const dst = path.join(GH_UPLOADS_DIR, p.filename);
      if (fs.existsSync(src) && !fs.existsSync(dst)) {
        fs.copyFileSync(src, dst);
      }
    }

    const formatted = published.map(p => ({
      id: p.id,
      title: p.title,
      category: p.category || 'Default',
      filename: p.filename,
      url: `${GH_PAGES_BASE_URL}/uploads/${encodeURIComponent(p.filename)}`,
      imageEndpoint: `${GH_PAGES_BASE_URL}/uploads/${encodeURIComponent(p.filename)}`,
      published: true,
      saveToRomOnDownload: Boolean(p.saveToRomOnDownload),
      width: p.width || 1080,
      height: p.height || 1080,
      sizeBytes: p.sizeBytes || 0,
      mimeType: p.mimeType || 'image/jpeg',
      createdAt: p.createdAt
    }));

    const manifestPayload = {
      version: catalog.version || '1.0',
      count: formatted.length,
      totalPublished: formatted.length,
      updatedAt: catalog.updatedAt || new Date().toISOString(),
      urls: formatted.map(p => p.url),
      puzzles: formatted
    };

    fs.writeFileSync(path.join(GH_PUZZLES_DIR, 'published.json'), JSON.stringify(manifestPayload, null, 2), 'utf8');
    fs.writeFileSync(path.join(GH_PUZZLES_DIR, 'urls.json'), JSON.stringify(manifestPayload, null, 2), 'utf8');
  } catch (err) {
    console.warn('[PuzzleImageServer] Static mirror sync warning:', err.message);
  }
}

/**
 * Save catalog.json and sync static mirror
 */
function saveCatalog(catalog) {
  catalog.updatedAt = new Date().toISOString();
  fs.writeFileSync(CATALOG_FILE, JSON.stringify(catalog, null, 2), 'utf8');
  syncToGitHubPagesStaticMirror(catalog);
}

/**
 * Compute full absolute URL for a puzzle entry based on incoming request host
 */
function getBaseUrl(req) {
  const host = req.headers.host || `localhost:${PORT}`;
  const proto = req.headers['x-forwarded-proto'] || 'http';
  return `${proto}://${host}`;
}

function formatPuzzleForResponse(p, baseUrl) {
  const fullUrl = `${baseUrl}/uploads/${encodeURIComponent(p.filename)}`;
  return {
    id: p.id,
    title: p.title,
    category: p.category || 'Default',
    filename: p.filename,
    url: fullUrl,
    imageEndpoint: `${baseUrl}/api/puzzles/${encodeURIComponent(p.id)}/image`,
    published: Boolean(p.published),
    saveToRomOnDownload: Boolean(p.saveToRomOnDownload),
    width: p.width || 1080,
    height: p.height || 1080,
    sizeBytes: p.sizeBytes || 0,
    mimeType: p.mimeType || 'image/png',
    createdAt: p.createdAt
  };
}

/**
 * Set universal CORS headers so Unity WebGL (.jslib) and HTML5 Canvas never taint
 */
function setCorsHeaders(res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Private-Network', 'true');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PATCH, PUT, DELETE, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization, X-Requested-With, Range, Access-Control-Request-Private-Network');
  res.setHeader('Access-Control-Expose-Headers', 'Content-Length, Content-Range, Accept-Ranges');
}

function sendJson(res, statusCode, payload) {
  setCorsHeaders(res);
  const body = JSON.stringify(payload, null, 2);
  res.writeHead(statusCode, {
    'Content-Type': 'application/json; charset=utf-8',
    'Content-Length': Buffer.byteLength(body)
  });
  res.end(body);
}

function readJsonBody(req, maxBytes = 60 * 1024 * 1024) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let total = 0;
    req.on('data', chunk => {
      total += chunk.length;
      if (total > maxBytes) {
        reject(new Error('Payload too large (max 60MB)'));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    req.on('end', () => {
      try {
        const str = Buffer.concat(chunks).toString('utf8');
        resolve(str ? JSON.parse(str) : {});
      } catch (e) {
        reject(new Error('Invalid JSON payload'));
      }
    });
    req.on('error', reject);
  });
}

/**
 * Sanitize filename and ensure uniqueness
 */
function makeSafeFilename(originalName, extFallback = '.png') {
  const parsed = path.parse(originalName || 'puzzle');
  const cleanBase = (parsed.name || 'puzzle')
    .toLowerCase()
    .replace(/[^a-z0-9_-]+/g, '_')
    .replace(/^_+|_+$/g, '')
    .slice(0, 48) || 'puzzle';
  const ext = (parsed.ext || extFallback).toLowerCase();
  const allowedExts = ['.png', '.jpg', '.jpeg', '.webp', '.svg'];
  const finalExt = allowedExts.includes(ext) ? ext : extFallback;
  const shortHash = crypto.randomBytes(3).toString('hex');
  return `${cleanBase}_${shortHash}${finalExt}`;
}

/**
 * Parse PNG or JPEG dimensions from buffer if not provided by client
 */
function detectImageDimensions(buf) {
  try {
    // PNG signature: 89 50 4E 47 0D 0A 1A 0A
    if (buf.length >= 24 && buf[0] === 0x89 && buf[1] === 0x50 && buf[2] === 0x4E && buf[3] === 0x47) {
      const width = buf.readUInt32BE(16);
      const height = buf.readUInt32BE(20);
      return { width, height };
    }
    // JPEG signature: FF D8
    if (buf.length >= 4 && buf[0] === 0xFF && buf[1] === 0xD8) {
      let offset = 2;
      while (offset + 9 < buf.length) {
        if (buf[offset] !== 0xFF) {
          offset++;
          continue;
        }
        const marker = buf[offset + 1];
        const len = buf.readUInt16BE(offset + 2);
        // SOF0 / SOF1 / SOF2 markers
        if (marker === 0xC0 || marker === 0xC1 || marker === 0xC2) {
          const height = buf.readUInt16BE(offset + 5);
          const width = buf.readUInt16BE(offset + 7);
          return { width, height };
        }
        offset += 2 + len;
      }
    }
  } catch (_) {}
  return { width: 1080, height: 1080 };
}

/**
 * Auto-import any existing images from github-gamedev.io/assets/games on first boot if catalog is empty
 */
function seedInitialCatalogIfEmpty() {
  const catalog = loadCatalog();
  if (catalog.puzzles.length > 0) return;

  const candidateDirs = [
    path.resolve(ROOT_DIR, '../github-gamedev.io/assets/games')
  ];

  for (const dir of candidateDirs) {
    if (fs.existsSync(dir)) {
      const files = fs.readdirSync(dir).filter(f => /\.(png|jpe?g|webp)$/i.test(f));
      for (const file of files) {
        try {
          const srcPath = path.join(dir, file);
          const buf = fs.readFileSync(srcPath);
          const safeName = makeSafeFilename(file, path.extname(file));
          const destPath = path.join(UPLOADS_DIR, safeName);
          fs.writeFileSync(destPath, buf);

          const dims = detectImageDimensions(buf);
          const title = path.parse(file).name
            .replace(/[-_]+/g, ' ')
            .replace(/\b\w/g, c => c.toUpperCase());

          catalog.puzzles.push({
            id: `puz_${crypto.randomBytes(4).toString('hex')}`,
            title,
            category: 'Featured',
            filename: safeName,
            published: true,
            saveToRomOnDownload: true,
            width: dims.width,
            height: dims.height,
            sizeBytes: buf.length,
            mimeType: safeName.endsWith('.png') ? 'image/png' : 'image/jpeg',
            createdAt: new Date().toISOString()
          });
        } catch (_) {}
      }
    }
  }

  if (catalog.puzzles.length > 0) {
    saveCatalog(catalog);
    console.log(`[PuzzleImageServer] Seeded ${catalog.puzzles.length} starter puzzle image(s) into storage/uploads.`);
  }
}

seedInitialCatalogIfEmpty();
syncToGitHubPagesStaticMirror(loadCatalog());

/**
 * HTTP Server Request Handler
 */
const server = http.createServer(async (req, res) => {
  setCorsHeaders(res);

  if (req.method === 'OPTIONS') {
    res.writeHead(204);
    res.end();
    return;
  }

  const parsedUrl = new URL(req.url, `http://${req.headers.host || `localhost:${PORT}`}`);
  const pathname = decodeURIComponent(parsedUrl.pathname);
  const baseUrl = getBaseUrl(req);

  try {
    // =========================================================================
    // 1. API HELPER ENDPOINT: GET PUBLISHED IMAGE URLS (Flat Array + Count)
    // =========================================================================
    if (req.method === 'GET' && pathname === '/api/puzzles/urls') {
      const catalog = loadCatalog();
      const published = catalog.puzzles
        .filter(p => p.published)
        .map(p => formatPuzzleForResponse(p, baseUrl));

      return sendJson(res, 200, {
        version: catalog.version || '1.0',
        count: published.length,
        updatedAt: catalog.updatedAt,
        urls: published.map(p => p.url),
        puzzles: published.map(p => ({
          id: p.id,
          title: p.title,
          category: p.category,
          url: p.url,
          width: p.width,
          height: p.height
        }))
      });
    }

    // =========================================================================
    // 2. API HELPER ENDPOINT: PUBLISHED MANIFEST (Studio.NativeImagePipeline)
    // =========================================================================
    if (req.method === 'GET' && (pathname === '/api/puzzles/published' || pathname === '/api/manifest.json')) {
      const catalog = loadCatalog();
      const categoryFilter = parsedUrl.searchParams.get('category');

      let published = catalog.puzzles.filter(p => p.published);
      if (categoryFilter && categoryFilter !== 'All') {
        published = published.filter(p => (p.category || '').toLowerCase() === categoryFilter.toLowerCase());
      }

      const formatted = published.map(p => formatPuzzleForResponse(p, baseUrl));
      return sendJson(res, 200, {
        version: catalog.version || '1.0',
        totalPublished: formatted.length,
        updatedAt: catalog.updatedAt,
        urls: formatted.map(p => p.url),
        puzzles: formatted
      });
    }

    // =========================================================================
    // 3. ADMIN API: GET ALL PUZZLES (Published + Drafts + Stats)
    // =========================================================================
    if (req.method === 'GET' && pathname === '/api/puzzles') {
      const catalog = loadCatalog();
      const all = catalog.puzzles.map(p => formatPuzzleForResponse(p, baseUrl));
      const publishedCount = all.filter(p => p.published).length;
      const totalBytes = all.reduce((acc, item) => acc + (item.sizeBytes || 0), 0);

      return sendJson(res, 200, {
        version: catalog.version || '1.0',
        updatedAt: catalog.updatedAt,
        totalCount: all.length,
        publishedCount,
        draftCount: all.length - publishedCount,
        totalBytes,
        urls: all.filter(p => p.published).map(p => p.url),
        puzzles: all
      });
    }

    // =========================================================================
    // 4. DIRECT IMAGE STREAM BY PUZZLE ID: GET /api/puzzles/:id/image
    // =========================================================================
    const imgMatch = pathname.match(/^\/api\/puzzles\/([^/]+)\/image$/);
    if (req.method === 'GET' && imgMatch) {
      const puzzleId = imgMatch[1];
      const catalog = loadCatalog();
      const entry = catalog.puzzles.find(p => p.id === puzzleId);
      if (!entry) {
        return sendJson(res, 404, { error: `Puzzle '${puzzleId}' not found` });
      }
      const filePath = path.join(UPLOADS_DIR, entry.filename);
      if (!fs.existsSync(filePath)) {
        return sendJson(res, 404, { error: 'Image file missing on disk' });
      }
      const stat = fs.statSync(filePath);
      const ext = path.extname(entry.filename).toLowerCase();
      const contentType = MIME_TYPES[ext] || 'application/octet-stream';

      res.writeHead(200, {
        'Content-Type': contentType,
        'Content-Length': stat.size,
        'Cache-Control': 'public, max-age=3600'
      });
      fs.createReadStream(filePath).pipe(res);
      return;
    }

    // =========================================================================
    // 5. UPLOAD PUZZLE IMAGES (Single or Batch Drag-and-Drop / Folder Picker)
    // =========================================================================
    if (req.method === 'POST' && pathname === '/api/puzzles/upload') {
      const body = await readJsonBody(req);
      const items = Array.isArray(body.items) ? body.items : (body.dataUrl ? [body] : []);

      if (items.length === 0) {
        return sendJson(res, 400, { error: 'No image items provided in upload payload' });
      }

      const catalog = loadCatalog();
      const added = [];

      for (const item of items) {
        if (!item.dataUrl || typeof item.dataUrl !== 'string') continue;

        const match = item.dataUrl.match(/^data:(image\/[a-zA-Z0-9+.-]+);base64,(.+)$/);
        if (!match) continue;

        const mimeType = match[1];
        const base64Data = match[2];
        const buf = Buffer.from(base64Data, 'base64');

        const extMap = {
          'image/png': '.png',
          'image/jpeg': '.jpg',
          'image/webp': '.webp',
          'image/svg+xml': '.svg'
        };
        const ext = extMap[mimeType] || '.png';
        const safeFilename = makeSafeFilename(item.filename || item.title || 'puzzle', ext);
        const filePath = path.join(UPLOADS_DIR, safeFilename);
        fs.writeFileSync(filePath, buf);

        const detected = detectImageDimensions(buf);
        const width = Number(item.width) || detected.width || 1080;
        const height = Number(item.height) || detected.height || 1080;

        const cleanTitle = (item.title || path.parse(item.filename || 'Puzzle').name)
          .replace(/[-_]+/g, ' ')
          .trim() || 'Untitled Puzzle';

        const newEntry = {
          id: `puz_${crypto.randomBytes(4).toString('hex')}`,
          title: cleanTitle,
          category: (item.category || 'Custom').trim(),
          filename: safeFilename,
          published: item.published !== undefined ? Boolean(item.published) : true,
          saveToRomOnDownload: Boolean(item.saveToRomOnDownload),
          width,
          height,
          sizeBytes: buf.length,
          mimeType,
          createdAt: new Date().toISOString()
        };

        catalog.puzzles.unshift(newEntry);
        added.push(formatPuzzleForResponse(newEntry, baseUrl));
      }

      saveCatalog(catalog);
      return sendJson(res, 201, {
        message: `Uploaded ${added.length} puzzle image(s) successfully.`,
        addedCount: added.length,
        added
      });
    }

    // =========================================================================
    // 6. SCAN & IMPORT FROM LOCAL SERVER DISK FOLDER
    // =========================================================================
    if (req.method === 'POST' && pathname === '/api/puzzles/scan-folder') {
      const body = await readJsonBody(req);
      const folderPath = (body.folderPath || '').trim();
      const defaultCategory = (body.category || 'Imported').trim();
      const autoPublish = body.published !== undefined ? Boolean(body.published) : true;

      if (!folderPath) {
        return sendJson(res, 400, { error: 'Please provide a valid local folderPath.' });
      }

      const resolvedFolder = path.resolve(folderPath);
      if (!fs.existsSync(resolvedFolder) || !fs.statSync(resolvedFolder).isDirectory()) {
        return sendJson(res, 404, { error: `Directory not found on disk: ${resolvedFolder}` });
      }

      const collectImageFiles = (dir, list = []) => {
        for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
          const full = path.join(dir, entry.name);
          if (entry.isDirectory()) {
            collectImageFiles(full, list);
          } else if (entry.isFile() && /\.(png|jpe?g|webp)$/i.test(entry.name)) {
            list.push(full);
          }
        }
        return list;
      };

      const filePaths = collectImageFiles(resolvedFolder);
      if (filePaths.length === 0) {
        return sendJson(res, 400, { error: `No .png, .jpg, or .webp images found in: ${resolvedFolder}` });
      }

      const catalog = loadCatalog();
      const imported = [];

      for (const srcFull of filePaths) {
        const file = path.basename(srcFull);
        const buf = fs.readFileSync(srcFull);
        const ext = path.extname(file).toLowerCase();
        const safeFilename = makeSafeFilename(file, ext);
        const destFull = path.join(UPLOADS_DIR, safeFilename);
        fs.writeFileSync(destFull, buf);

        const dims = detectImageDimensions(buf);
        const title = path.parse(file).name
          .replace(/[-_]+/g, ' ')
          .replace(/\b\w/g, c => c.toUpperCase());

        const newEntry = {
          id: `puz_${crypto.randomBytes(4).toString('hex')}`,
          title,
          category: defaultCategory,
          filename: safeFilename,
          published: autoPublish,
          saveToRomOnDownload: false,
          width: dims.width,
          height: dims.height,
          sizeBytes: buf.length,
          mimeType: MIME_TYPES[ext] || 'image/jpeg',
          createdAt: new Date().toISOString()
        };

        catalog.puzzles.unshift(newEntry);
        imported.push(formatPuzzleForResponse(newEntry, baseUrl));
      }

      saveCatalog(catalog);
      return sendJson(res, 200, {
        message: `Imported ${imported.length} image(s) from ${resolvedFolder}`,
        importedCount: imported.length,
        imported
      });
    }

    // =========================================================================
    // 7. BULK PUBLISH / UNPUBLISH ALL
    // =========================================================================
    if (req.method === 'POST' && pathname === '/api/puzzles/bulk-status') {
      const body = await readJsonBody(req);
      const published = Boolean(body.published);
      const catalog = loadCatalog();
      catalog.puzzles.forEach(p => {
        p.published = published;
      });
      saveCatalog(catalog);
      return sendJson(res, 200, {
        message: `${published ? 'Published' : 'Unpublished'} all ${catalog.puzzles.length} puzzles.`,
        publishedCount: published ? catalog.puzzles.length : 0
      });
    }

    // =========================================================================
    // 8. UPDATE PUZZLE METADATA OR PUBLISH STATUS: PATCH /api/puzzles/:id
    // =========================================================================
    const patchMatch = pathname.match(/^\/api\/puzzles\/([^/]+)$/);
    if (req.method === 'PATCH' && patchMatch) {
      const puzzleId = patchMatch[1];
      const body = await readJsonBody(req);
      const catalog = loadCatalog();
      const entry = catalog.puzzles.find(p => p.id === puzzleId);

      if (!entry) {
        return sendJson(res, 404, { error: `Puzzle '${puzzleId}' not found` });
      }

      if (body.published !== undefined) entry.published = Boolean(body.published);
      if (body.saveToRomOnDownload !== undefined) entry.saveToRomOnDownload = Boolean(body.saveToRomOnDownload);
      if (typeof body.title === 'string' && body.title.trim()) entry.title = body.title.trim();
      if (typeof body.category === 'string' && body.category.trim()) entry.category = body.category.trim();

      saveCatalog(catalog);
      return sendJson(res, 200, {
        message: 'Updated puzzle entry.',
        puzzle: formatPuzzleForResponse(entry, baseUrl)
      });
    }

    // =========================================================================
    // 9. DELETE PUZZLE IMAGE: DELETE /api/puzzles/:id
    // =========================================================================
    if (req.method === 'DELETE' && patchMatch) {
      const puzzleId = patchMatch[1];
      const catalog = loadCatalog();
      const idx = catalog.puzzles.findIndex(p => p.id === puzzleId);
      if (idx === -1) {
        return sendJson(res, 404, { error: `Puzzle '${puzzleId}' not found` });
      }
      const [removed] = catalog.puzzles.splice(idx, 1);
      saveCatalog(catalog);

      if (removed && removed.filename) {
        const diskFile = path.join(UPLOADS_DIR, removed.filename);
        if (fs.existsSync(diskFile)) {
          try { fs.unlinkSync(diskFile); } catch (_) {}
        }
      }
      return sendJson(res, 200, {
        message: `Deleted '${removed.title}'`,
        deletedId: puzzleId
      });
    }

    // =========================================================================
    // 9b. PUSH PUBLISHED CATALOG & IMAGES TO LIVE GITHUB PAGES: POST /api/puzzles/push-live
    // =========================================================================
    if (req.method === 'POST' && pathname === '/api/puzzles/push-live') {
      const catalog = loadCatalog();
      syncToGitHubPagesStaticMirror(catalog);
      try {
        execSync('git add assets/puzzles', { cwd: GH_REPO_DIR, stdio: 'pipe' });
        const statusOut = execSync('git status --porcelain assets/puzzles', { cwd: GH_REPO_DIR, encoding: 'utf8' }).trim();
        if (statusOut) {
          execSync(`git commit -m "Sync ${catalog.puzzles.filter(p => p.published).length} published puzzle images to live GitHub Pages"`, { cwd: GH_REPO_DIR, stdio: 'pipe' });
        }
        execSync('git push origin main', { cwd: GH_REPO_DIR, stdio: 'pipe' });
        return sendJson(res, 200, {
          pushed: true,
          hadChanges: Boolean(statusOut),
          publishedCount: catalog.puzzles.filter(p => p.published).length,
          liveUrl: `${GH_PAGES_BASE_URL}/published.json`,
          message: statusOut
            ? 'Synced & pushed published puzzle images to live GitHub Pages!'
            : 'Live GitHub Pages is already up-to-date with all published puzzles!'
        });
      } catch (gitErr) {
        return sendJson(res, 500, {
          pushed: false,
          error: gitErr.message || 'Git push failed'
        });
      }
    }

    // =========================================================================
    // 10. SERVE UPLOADED PUZZLE IMAGES: GET /uploads/:filename
    // =========================================================================
    if (req.method === 'GET' && pathname.startsWith('/uploads/')) {
      const filename = path.basename(pathname.slice('/uploads/'.length));
      let filePath = path.join(UPLOADS_DIR, filename);
      if (!fs.existsSync(filePath)) {
        filePath = path.join(GH_UPLOADS_DIR, filename);
      }
      if (!fs.existsSync(filePath) || !fs.statSync(filePath).isFile()) {
        return sendJson(res, 404, { error: 'Image not found' });
      }
      const ext = path.extname(filename).toLowerCase();
      const contentType = MIME_TYPES[ext] || 'application/octet-stream';
      const stat = fs.statSync(filePath);

      res.writeHead(200, {
        'Content-Type': contentType,
        'Content-Length': stat.size,
        'Cache-Control': 'public, max-age=3600'
      });
      fs.createReadStream(filePath).pipe(res);
      return;
    }

    // =========================================================================
    // 11. SERVE CLIENT API HELPER SDK: GET /sdk/puzzle-api-helper.js
    // =========================================================================
    if (req.method === 'GET' && pathname === '/sdk/puzzle-api-helper.js') {
      const sdkFile = path.join(SDK_DIR, 'puzzle-api-helper.js');
      if (fs.existsSync(sdkFile)) {
        const stat = fs.statSync(sdkFile);
        res.writeHead(200, {
          'Content-Type': 'application/javascript; charset=utf-8',
          'Content-Length': stat.size
        });
        fs.createReadStream(sdkFile).pipe(res);
        return;
      }
    }

    // =========================================================================
    // 11b. SERVE WEB & UNITY WEBGL BUILDS: GET /labs/*
    // =========================================================================
    if (req.method === 'GET' && pathname.startsWith('/labs/')) {
      let rel = pathname.slice('/labs/'.length);
      if (!rel || rel.endsWith('/')) rel += 'index.html';
      const safePath = path.normalize(path.join(LABS_DIR, rel));
      if (safePath.startsWith(LABS_DIR) && fs.existsSync(safePath) && fs.statSync(safePath).isFile()) {
        const ext = path.extname(safePath).toLowerCase();
        const contentType = MIME_TYPES[ext] || 'application/octet-stream';
        const stat = fs.statSync(safePath);
        res.writeHead(200, {
          'Content-Type': contentType,
          'Content-Length': stat.size
        });
        fs.createReadStream(safePath).pipe(res);
        return;
      }
    }

    // =========================================================================
    // 12. SERVE ADMIN STUDIO FRONTEND UI: GET / (and static files in public/)
    // =========================================================================
    if (req.method === 'GET') {
      const reqFile = pathname === '/' ? 'index.html' : pathname.replace(/^\/+/, '');
      const safePath = path.normalize(path.join(PUBLIC_DIR, reqFile));
      if (safePath.startsWith(PUBLIC_DIR) && fs.existsSync(safePath) && fs.statSync(safePath).isFile()) {
        const ext = path.extname(safePath).toLowerCase();
        const contentType = MIME_TYPES[ext] || 'text/plain; charset=utf-8';
        const stat = fs.statSync(safePath);
        res.writeHead(200, {
          'Content-Type': contentType,
          'Content-Length': stat.size
        });
        fs.createReadStream(safePath).pipe(res);
        return;
      }
    }

    sendJson(res, 404, { error: `Route not found: ${req.method} ${pathname}` });
  } catch (err) {
    console.error('[PuzzleImageServer Error]:', err);
    sendJson(res, 500, { error: err.message || 'Internal Server Error' });
  }
});

server.listen(PORT, () => {
  console.log(`\n==================================================================`);
  console.log(` 🧩 Calm Jigsaw — Puzzle Image Server & Admin Studio Running!`);
  console.log(` 🌐 Admin Studio UI:          http://localhost:${PORT}`);
  console.log(` 🔗 Published URLs Helper:    http://localhost:${PORT}/api/puzzles/urls`);
  console.log(` 📦 NativePipeline Manifest:  http://localhost:${PORT}/api/puzzles/published`);
  console.log(` 🛠️ JS Client Helper SDK:     http://localhost:${PORT}/sdk/puzzle-api-helper.js`);
  console.log(`==================================================================\n`);
});
