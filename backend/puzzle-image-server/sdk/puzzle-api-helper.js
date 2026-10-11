/**
 * Calm Jigsaw — Puzzle Image Server Client & Native Bridge Helper SDK
 * Served at: http://localhost:4200/sdk/puzzle-api-helper.js
 *
 * Provides helper methods to:
 * 1. Fetch all available published image URLs (`getPublishedImageUrls`)
 * 2. Fetch the structured published manifest (`getPublishedPuzzles`) for Studio.NativeImagePipeline
 * 3. Fetch & decode image data via OffscreenCanvas / createImageBitmap (`fetchPuzzleImageData`)
 * 4. Enqueue published server images directly into Unity WebGL's NativeImageBridge (`enqueuePublishedIntoNativeBridge`)
 */
(function (global) {
  'use strict';

  const DEFAULT_SERVER_URL = 'http://localhost:4200';

  function normalizeBaseUrl(baseUrl) {
    return (baseUrl || DEFAULT_SERVER_URL).replace(/\/+$/, '');
  }

  const PuzzleServerAPI = {
    defaultServerUrl: DEFAULT_SERVER_URL,

    /**
     * Helper 1: Returns a flat array of URLs for all available published puzzle images on the server.
     * Example return: ["http://localhost:4200/uploads/alpine_sunset.jpg", ...]
     *
     * @param {string} [serverBaseUrl="http://localhost:4200"]
     * @returns {Promise<string[]>}
     */
    async getPublishedImageUrls(serverBaseUrl = DEFAULT_SERVER_URL) {
      const base = normalizeBaseUrl(serverBaseUrl);
      const res = await fetch(`${base}/api/puzzles/urls`, {
        method: 'GET',
        mode: 'cors',
        cache: 'no-store'
      });
      if (!res.ok) {
        throw new Error(`Failed to fetch published URLs (HTTP ${res.status})`);
      }
      const data = await res.json();
      return Array.isArray(data.urls) ? data.urls : [];
    },

    /**
     * Helper 2: Returns the full published manifest (`PuzzleManifestResponse` schema)
     * containing both `urls: string[]` and `puzzles: Array<{id, title, category, url, width, height, saveToRomOnDownload}>`.
     *
     * @param {string} [serverBaseUrl="http://localhost:4200"]
     * @param {string} [category] Optional category filter
     * @returns {Promise<{version: string, totalPublished: number, urls: string[], puzzles: Array<Object>}>}
     */
    async getPublishedPuzzles(serverBaseUrl = DEFAULT_SERVER_URL, category = '') {
      const base = normalizeBaseUrl(serverBaseUrl);
      const query = category && category !== 'All' ? `?category=${encodeURIComponent(category)}` : '';
      const res = await fetch(`${base}/api/puzzles/published${query}`, {
        method: 'GET',
        mode: 'cors',
        cache: 'no-store'
      });
      if (!res.ok) {
        throw new Error(`Failed to fetch published puzzles (HTTP ${res.status})`);
      }
      return await res.json();
    },

    /**
     * Helper 3: Fetches raw image data from a server URL (or puzzle ID), decodes & center-crops
     * it to a 1:1 square using OffscreenCanvas / HTML5 Canvas (matching NativeImageBridge.jslib),
     * and returns { bytes, blob, dataUrl, width, height, durationMs }.
     *
     * @param {string} imageUrlOrId Full image URL or puzzle ID
     * @param {Object} [options]
     * @param {number} [options.targetSizePx=1080] Target square size in pixels (e.g. 256, 512, 1080)
     * @param {string} [options.mimeType="image/jpeg"] Output MIME type ("image/jpeg" or "image/png")
     * @param {number} [options.quality=0.92] JPEG/WebP quality (0..1)
     * @param {string} [options.serverBaseUrl="http://localhost:4200"]
     * @returns {Promise<{url: string, width: number, height: number, bytes: Uint8Array, blob: Blob, dataUrl: string, durationMs: number}>}
     */
    async fetchPuzzleImageData(imageUrlOrId, options = {}) {
      const t0 = performance.now();
      const targetSizePx = options.targetSizePx || 1080;
      const mimeType = options.mimeType || 'image/jpeg';
      const quality = options.quality !== undefined ? options.quality : 0.92;
      const base = normalizeBaseUrl(options.serverBaseUrl || DEFAULT_SERVER_URL);

      const finalUrl = /^https?:\/\//i.test(imageUrlOrId)
        ? imageUrlOrId
        : `${base}/api/puzzles/${encodeURIComponent(imageUrlOrId)}/image`;

      const res = await fetch(finalUrl, { mode: 'cors' });
      if (!res.ok) {
        throw new Error(`Image fetch failed (${res.status}) for ${finalUrl}`);
      }
      const srcBlob = await res.blob();

      // Decode & square-crop via createImageBitmap + Canvas
      let bitmap = null;
      let imgEl = null;
      let srcW = 0;
      let srcH = 0;

      if (typeof createImageBitmap === 'function') {
        bitmap = await createImageBitmap(srcBlob);
        srcW = bitmap.width;
        srcH = bitmap.height;
      } else {
        imgEl = await new Promise((resolve, reject) => {
          const img = new Image();
          img.crossOrigin = 'anonymous';
          img.onload = () => resolve(img);
          img.onerror = reject;
          img.src = URL.createObjectURL(srcBlob);
        });
        srcW = imgEl.naturalWidth;
        imgEl.width = srcW;
        srcH = imgEl.naturalHeight;
      }

      const cropSide = Math.min(srcW, srcH);
      const sx = Math.floor((srcW - cropSide) * 0.5);
      const sy = Math.floor((srcH - cropSide) * 0.5);

      const canvas = document.createElement('canvas');
      canvas.width = targetSizePx;
      canvas.height = targetSizePx;
      const ctx = canvas.getContext('2d');
      ctx.imageSmoothingEnabled = true;
      ctx.imageSmoothingQuality = 'high';
      ctx.drawImage(bitmap || imgEl, sx, sy, cropSide, cropSide, 0, 0, targetSizePx, targetSizePx);

      if (bitmap && typeof bitmap.close === 'function') {
        bitmap.close();
      }

      const dataUrl = canvas.toDataURL(mimeType, quality);
      const outBlob = await new Promise(resolve => canvas.toBlob(resolve, mimeType, quality));
      const arrayBuf = await outBlob.arrayBuffer();
      const bytes = new Uint8Array(arrayBuf);
      const durationMs = Math.round(performance.now() - t0);

      return {
        url: finalUrl,
        width: targetSizePx,
        height: targetSizePx,
        byteLength: bytes.byteLength,
        bytes,
        blob: outBlob,
        dataUrl,
        durationMs
      };
    },

    /**
     * Helper 4: Fetches all published puzzle URLs from the server and enqueues them
     * into the Unity WebGL NativeImageBridge (`window._NIP`) if present.
     *
     * @param {string} [serverBaseUrl="http://localhost:4200"]
     * @returns {Promise<{enqueuedCount: number, urls: string[], puzzles: Array<Object>}>}
     */
    async enqueuePublishedIntoNativeBridge(serverBaseUrl = DEFAULT_SERVER_URL) {
      const manifest = await this.getPublishedPuzzles(serverBaseUrl);
      const puzzles = manifest.puzzles || [];

      if (global._NIP && typeof global._NIP.enqueue === 'function') {
        puzzles.forEach((p, idx) => {
          const priority = idx < 4 ? 2 : 1; // High priority for first 4 visible puzzles
          const tier = p.saveToRomOnDownload ? 1 : 0; // 1 = PermanentROM, 0 = VolatileCache
          global._NIP.enqueue(p.id, p.url, priority, tier);
        });
      }

      return {
        enqueuedCount: puzzles.length,
        urls: manifest.urls || [],
        puzzles
      };
    }
  };

  global.PuzzleServerAPI = PuzzleServerAPI;
})(typeof window !== 'undefined' ? window : globalThis);
