import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const PORT = process.env.PORT || 3000;
const CONTENT_DIR = path.join(__dirname, 'content');
const LEVELS_DIR = path.join(CONTENT_DIR, 'levels');
const MANIFEST_PATH = path.join(CONTENT_DIR, 'manifest.json');
const DEFAULT_SEED_PATH = 'C:\\Users\\arpit\\Downloads\\meowsweeper_levels_batch.json';

// Portfolio Page content directory (for static portfolio hosting on github-gamedev.io)
const PORTFOLIO_CONTENT_DIR = path.join(__dirname, '..', 'content');
const PORTFOLIO_LEVELS_DIR = path.join(PORTFOLIO_CONTENT_DIR, 'levels');
const PORTFOLIO_MANIFEST_PATH = path.join(PORTFOLIO_CONTENT_DIR, 'manifest.json');

// Ensure directories exist
fs.mkdirSync(LEVELS_DIR, { recursive: true });
fs.mkdirSync(PORTFOLIO_LEVELS_DIR, { recursive: true });

/**
 * Compute SHA-256 hash of a string or buffer
 */
function computeHash(content) {
  return crypto.createHash('sha256').update(content).digest('hex');
}

/**
 * Rebuild manifest.json from all JSON files in LEVELS_DIR
 */
function rebuildManifest() {
  const files = fs.readdirSync(LEVELS_DIR).filter(f => f.endsWith('.json'));
  const levelsMap = {};

  for (const file of files) {
    const filePath = path.join(LEVELS_DIR, file);
    try {
      const raw = fs.readFileSync(filePath, 'utf-8');
      const hash = computeHash(raw);
      const data = JSON.parse(raw);
      const levelId = data.levelId || path.basename(file, '.json');

      levelsMap[levelId] = {
        levelId,
        levelDisplayName: data.levelDisplayName || `Level ${levelId}`,
        order: data.order ?? 0,
        gridSize: data.gridSize ?? 5,
        tier: data.tier || 'Standard',
        hash,
        fileSize: Buffer.byteLength(raw, 'utf8'),
        url: `/levels/${file}`
      };
    } catch (err) {
      console.warn(`[Manifest] Skipping invalid JSON ${file}:`, err.message);
    }
  }

  const manifest = {
    schemaVersion: '2.0',
    game: 'Meowsweeper',
    updatedAt: new Date().toISOString(),
    totalLevels: Object.keys(levelsMap).length,
    levels: levelsMap
  };

  fs.writeFileSync(MANIFEST_PATH, JSON.stringify(manifest, null, 2), 'utf-8');
  try {
    fs.writeFileSync(PORTFOLIO_MANIFEST_PATH, JSON.stringify(manifest, null, 2), 'utf-8');
  } catch (e) {
    console.warn('[Manifest] Could not write to portfolio manifest:', e.message);
  }
  console.log(`[Manifest] Rebuilt manifest with ${manifest.totalLevels} levels (Synced to Server & Portfolio).`);
  return manifest;
}

/**
 * Seed initial levels from batch JSON if levels directory is empty
 */
function seedInitialLevelsIfEmpty() {
  const existing = fs.readdirSync(LEVELS_DIR).filter(f => f.endsWith('.json'));
  if (existing.length > 0) {
    console.log(`[Seed] Found ${existing.length} existing levels in cache.`);
    if (!fs.existsSync(MANIFEST_PATH)) {
      rebuildManifest();
    }
    return;
  }

  if (fs.existsSync(DEFAULT_SEED_PATH)) {
    console.log(`[Seed] Seeding from ${DEFAULT_SEED_PATH}...`);
    try {
      const raw = fs.readFileSync(DEFAULT_SEED_PATH, 'utf-8');
      const parsed = JSON.parse(raw);
      const levels = Array.isArray(parsed) ? parsed : (parsed.levels || []);

      for (const lvl of levels) {
        if (!lvl || !lvl.levelId) continue;
        const targetPath = path.join(LEVELS_DIR, `${lvl.levelId}.json`);
        fs.writeFileSync(targetPath, JSON.stringify(lvl, null, 2), 'utf-8');
      }
      console.log(`[Seed] Seeded ${levels.length} levels.`);
    } catch (err) {
      console.error(`[Seed] Failed to seed levels:`, err.message);
    }
  }

  rebuildManifest();
}

seedInitialLevelsIfEmpty();

/**
 * Handle incoming HTTP requests
 */
const server = http.createServer((req, res) => {
  // CORS Headers to allow GitHub Pages, localhost, and file:// access
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS, PUT, DELETE');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization, X-Requested-With');

  if (req.method === 'OPTIONS') {
    res.writeHead(204);
    res.end();
    return;
  }

  const parsedUrl = new URL(req.url, `http://${req.headers.host || 'localhost'}`);
  const pathname = parsedUrl.pathname;

  // 1. Health & status check
  if (req.method === 'GET' && (pathname === '/' || pathname === '/api/status')) {
    const files = fs.readdirSync(LEVELS_DIR).filter(f => f.endsWith('.json'));
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({
      status: 'online',
      service: 'Meowsweeper Level Sync Server',
      totalLevels: files.length,
      manifestUrl: '/manifest.json',
      time: new Date().toISOString()
    }));
    return;
  }

  // 2. GET /manifest.json
  if (req.method === 'GET' && pathname === '/manifest.json') {
    if (!fs.existsSync(MANIFEST_PATH)) {
      rebuildManifest();
    }
    const data = fs.readFileSync(MANIFEST_PATH, 'utf-8');
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(data);
    return;
  }

  // 3. GET /levels/:filename
  if (req.method === 'GET' && pathname.startsWith('/levels/')) {
    const filename = path.basename(pathname);
    const targetFile = path.join(LEVELS_DIR, filename);

    if (fs.existsSync(targetFile)) {
      const data = fs.readFileSync(targetFile, 'utf-8');
      res.writeHead(200, {
        'Content-Type': 'application/json',
        'Cache-Control': 'no-cache'
      });
      res.end(data);
    } else {
      res.writeHead(404, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ error: `Level ${filename} not found` }));
    }
    return;
  }

  // Helper to read JSON request body
  function readJsonBody(callback) {
    let body = '';
    req.on('data', chunk => {
      body += chunk;
      if (body.length > 50 * 1024 * 1024) { // 50MB limit
        res.writeHead(413, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ error: 'Payload too large' }));
        req.destroy();
      }
    });
    req.on('end', () => {
      try {
        const json = JSON.parse(body || '{}');
        callback(null, json);
      } catch (err) {
        callback(err);
      }
    });
  }

  // 4. POST /api/publish-batch (Receives full batch from Level Studio)
  if (req.method === 'POST' && pathname === '/api/publish-batch') {
    readJsonBody((err, payload) => {
      if (err) {
        res.writeHead(400, { 'Content-Type': 'application/json' });
        return res.end(JSON.stringify({ error: 'Invalid JSON body', details: err.message }));
      }

      const levels = Array.isArray(payload) ? payload : (payload.levels || []);
      if (!Array.isArray(levels) || levels.length === 0) {
        res.writeHead(400, { 'Content-Type': 'application/json' });
        return res.end(JSON.stringify({ error: 'Payload must contain a non-empty levels array' }));
      }

      let savedCount = 0;
      for (const lvl of levels) {
        if (!lvl || !lvl.levelId) continue;
        const targetPath = path.join(LEVELS_DIR, `${lvl.levelId}.json`);
        const portfolioPath = path.join(PORTFOLIO_LEVELS_DIR, `${lvl.levelId}.json`);
        const jsonContent = JSON.stringify(lvl, null, 2);
        fs.writeFileSync(targetPath, jsonContent, 'utf-8');
        try {
          fs.writeFileSync(portfolioPath, jsonContent, 'utf-8');
        } catch (e) {}
        savedCount++;
      }

      const updatedManifest = rebuildManifest();
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({
        success: true,
        savedCount,
        totalLevels: updatedManifest.totalLevels,
        updatedAt: updatedManifest.updatedAt
      }));
    });
    return;
  }

  // 5. POST /api/publish-single (Publish or update a single level)
  if (req.method === 'POST' && pathname === '/api/publish-single') {
    readJsonBody((err, payload) => {
      if (err) {
        res.writeHead(400, { 'Content-Type': 'application/json' });
        return res.end(JSON.stringify({ error: 'Invalid JSON body' }));
      }

      const level = payload.level || payload;
      if (!level || !level.levelId) {
        res.writeHead(400, { 'Content-Type': 'application/json' });
        return res.end(JSON.stringify({ error: 'Level object must contain levelId' }));
      }

      const targetPath = path.join(LEVELS_DIR, `${level.levelId}.json`);
      const portfolioPath = path.join(PORTFOLIO_LEVELS_DIR, `${level.levelId}.json`);
      const jsonContent = JSON.stringify(level, null, 2);
      fs.writeFileSync(targetPath, jsonContent, 'utf-8');
      try {
        fs.writeFileSync(portfolioPath, jsonContent, 'utf-8');
      } catch (e) {}
      const updatedManifest = rebuildManifest();

      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({
        success: true,
        levelId: level.levelId,
        hash: updatedManifest.levels[level.levelId]?.hash,
        updatedAt: updatedManifest.updatedAt
      }));
    });
    return;
  }

  // 6. DELETE /api/delete-level or DELETE /levels/:filename (Direct removal without publish)
  if ((req.method === 'DELETE' || req.method === 'POST') && (pathname === '/api/delete-level' || pathname.startsWith('/levels/'))) {
    const handleDeletion = (levelId) => {
      if (!levelId) {
        res.writeHead(400, { 'Content-Type': 'application/json' });
        return res.end(JSON.stringify({ error: 'levelId is required' }));
      }

      const cleanId = levelId.replace(/\.json$/i, '');
      const targetFile = path.join(LEVELS_DIR, `${cleanId}.json`);
      const portfolioFile = path.join(PORTFOLIO_LEVELS_DIR, `${cleanId}.json`);

      let deleted = false;
      if (fs.existsSync(targetFile)) {
        fs.unlinkSync(targetFile);
        deleted = true;
      }
      if (fs.existsSync(portfolioFile)) {
        try {
          fs.unlinkSync(portfolioFile);
          deleted = true;
        } catch (e) {}
      }

      const updatedManifest = rebuildManifest();
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({
        success: true,
        deletedLevelId: cleanId,
        foundAndRemoved: deleted,
        totalLevels: updatedManifest.totalLevels,
        updatedAt: updatedManifest.updatedAt
      }));
    };

    if (pathname.startsWith('/levels/')) {
      const filename = path.basename(pathname);
      handleDeletion(filename);
      return;
    }

    readJsonBody((err, payload) => {
      if (err) {
        res.writeHead(400, { 'Content-Type': 'application/json' });
        return res.end(JSON.stringify({ error: 'Invalid JSON body' }));
      }
      handleDeletion(payload.levelId);
    });
    return;
  }

  // 404 for other routes
  res.writeHead(404, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify({ error: 'Not Found' }));
});

server.listen(PORT, () => {
  console.log(`=======================================================`);
  console.log(`🐾 Meowsweeper Level Sync Server running at:`);
  console.log(`👉 http://localhost:${PORT}`);
  console.log(`👉 Manifest: http://localhost:${PORT}/manifest.json`);
  console.log(`=======================================================`);
});
