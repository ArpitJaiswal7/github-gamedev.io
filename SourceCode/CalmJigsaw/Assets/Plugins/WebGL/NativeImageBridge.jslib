var NativeImagePipelineWebGLPlugin = {
    $NIPWebGL: {
        receiverName: "__NativeImagePipelineReceiver",
        dbName: "Studio_NativeImagePipeline_DB",
        dbVersion: 1,
        storeCache: "volatile_cache",
        storeRom: "permanent_rom",
        db: null,
        isOnline: true,
        maxConcurrent: 2,
        activeWorkers: 0,
        defaultIconPx: 256,
        defaultFullPx: 1080,
        jpegQuality: 0.88,
        maxRetries: 2,
        seqCounter: 0,
        queue: [],
        activeKeys: {},

        openDatabase: function(callback) {
            if (this.db) {
                callback(this.db);
                return;
            }
            var self = this;
            var req = indexedDB.open(this.dbName, this.dbVersion);
            req.onupgradeneeded = function(e) {
                var db = e.target.result;
                if (!db.objectStoreNames.contains(self.storeCache)) {
                    db.createObjectStore(self.storeCache);
                }
                if (!db.objectStoreNames.contains(self.storeRom)) {
                    db.createObjectStore(self.storeRom);
                }
            };
            req.onsuccess = function(e) {
                self.db = e.target.result;
                callback(self.db);
            };
            req.onerror = function() {
                callback(null);
            };
        },

        putBlobBytes: function(storeName, fileKey, uint8Array, callback) {
            this.openDatabase(function(db) {
                if (!db) { if (callback) callback(false); return; }
                try {
                    var tx = db.transaction([storeName], "readwrite");
                    var store = tx.objectStore(storeName);
                    store.put(uint8Array, fileKey);
                    tx.oncomplete = function() { if (callback) callback(true); };
                    tx.onerror = function() { if (callback) callback(false); };
                } catch (e) {
                    if (callback) callback(false);
                }
            });
        },

        getBlobBytes: function(fileKey, callback) {
            var self = this;
            this.openDatabase(function(db) {
                if (!db) { callback(null, -1); return; }
                var txRom = db.transaction([self.storeRom], "readonly");
                var reqRom = txRom.objectStore(self.storeRom).get(fileKey);
                reqRom.onsuccess = function() {
                    if (reqRom.result && reqRom.result.byteLength > 0) {
                        callback(reqRom.result, 1);
                    } else {
                        var txCache = db.transaction([self.storeCache], "readonly");
                        var reqCache = txCache.objectStore(self.storeCache).get(fileKey);
                        reqCache.onsuccess = function() {
                            if (reqCache.result && reqCache.result.byteLength > 0) {
                                callback(reqCache.result, 0);
                            } else {
                                callback(null, -1);
                            }
                        };
                        reqCache.onerror = function() { callback(null, -1); };
                    }
                };
                reqRom.onerror = function() { callback(null, -1); };
            });
        },

        // Decodes PNG/JPG/WebP in browser native C++/GPU outside Unity WASM heap,
        // center-crops to 1:1 square (dimPx x dimPx), and encodes to JPEG Uint8Array
        transcodeAndResizeToJpeg: async function(sourceBlob, dimPx, quality) {
            var bitmap = await createImageBitmap(sourceBlob);
            var minSide = Math.min(bitmap.width, bitmap.height);
            var sx = Math.floor((bitmap.width - minSide) / 2);
            var sy = Math.floor((bitmap.height - minSide) / 2);

            var outBlob = null;
            if (typeof OffscreenCanvas !== "undefined") {
                var offscreen = new OffscreenCanvas(dimPx, dimPx);
                var ctx = offscreen.getContext("2d", { alpha: false });
                ctx.fillStyle = "#0d1117";
                ctx.fillRect(0, 0, dimPx, dimPx);
                ctx.drawImage(bitmap, sx, sy, minSide, minSide, 0, 0, dimPx, dimPx);
                bitmap.close();
                outBlob = await offscreen.convertToBlob({ type: "image/jpeg", quality: quality });
            } else {
                var canvas = document.createElement("canvas");
                canvas.width = dimPx;
                canvas.height = dimPx;
                var ctx2 = canvas.getContext("2d", { alpha: false });
                ctx2.fillStyle = "#0d1117";
                ctx2.fillRect(0, 0, dimPx, dimPx);
                ctx2.drawImage(bitmap, sx, sy, minSide, minSide, 0, 0, dimPx, dimPx);
                bitmap.close();
                outBlob = await new Promise(function(resolve) {
                    canvas.toBlob(resolve, "image/jpeg", quality);
                });
            }

            var arrayBuffer = await outBlob.arrayBuffer();
            return new Uint8Array(arrayBuffer);
        },

        putDualBlobBytes: function(storeName, key1, bytes1, key2, bytes2, callback) {
            this.openDatabase(function(db) {
                if (!db) { if (callback) callback(false); return; }
                try {
                    var tx = db.transaction([storeName], "readwrite");
                    var store = tx.objectStore(storeName);
                    store.put(bytes1, key1);
                    store.put(bytes2, key2);
                    tx.oncomplete = function() { if (callback) callback(true); };
                    tx.onerror = function() { if (callback) callback(false); };
                } catch (e) {
                    if (callback) callback(false);
                }
            });
        },

        pumpQueue: function() {
            if (!this.isOnline) return;
            var self = this;

            while (this.activeWorkers < this.maxConcurrent && this.queue.length > 0) {
                this.queue.sort(function(a, b) {
                    if (b.priority !== a.priority) return b.priority - a.priority;
                    return a.seq - b.seq;
                });

                var task = this.queue.shift();
                if (!task || task.cancelled) continue;

                this.activeWorkers++;
                task.started = true;
                this.executeTask(task).finally(function() {
                    self.activeWorkers--;
                    setTimeout(function() {
                        self.pumpQueue();
                    }, 35);
                });
            }
        },

        executeTask: async function(task) {
            var self = this;
            var startMs = performance.now();
            var key = task.requestKey;
            var iconFileKey = key + "_" + this.defaultIconPx + ".jpg";
            var fullFileKey = key + "_" + this.defaultFullPx + ".jpg";

            // Fast-path check in IndexedDB
            var existingCheck = await new Promise(function(resolve) {
                self.getBlobBytes(fullFileKey, function(fullBytes, tier) {
                    if (!fullBytes) { resolve(null); return; }
                    self.getBlobBytes(iconFileKey, function(iconBytes) {
                        var iconLen = iconBytes ? iconBytes.byteLength : fullBytes.byteLength;
                        resolve({ iconLen: iconLen, fullLen: fullBytes.byteLength, tier: tier });
                    });
                });
            });

            if (existingCheck) {
                delete this.activeKeys[key];
                this.sendDownloadResult(key, true, existingCheck.iconLen, existingCheck.fullLen, 0, existingCheck.tier, "");
                return;
            }

            var candidateUrls = [task.url];
            if (task.url && task.url.indexOf("/uploads/") !== -1) {
                var fname = task.url.split("/uploads/").pop();
                var ghUrl = "https://arpitjaiswal7.github.io/github-gamedev.io/assets/puzzles/uploads/" + fname;
                var localUrl = "http://localhost:4200/uploads/" + fname;
                if (typeof window !== "undefined" && window.location && window.location.protocol === "https:") {
                    candidateUrls = [ghUrl, task.url, "../../assets/puzzles/uploads/" + fname];
                } else {
                    if (candidateUrls.indexOf(ghUrl) === -1) candidateUrls.push(ghUrl);
                    if (candidateUrls.indexOf(localUrl) === -1) candidateUrls.push(localUrl);
                }
            }

            var lastError = "";
            for (var uIdx = 0; uIdx < candidateUrls.length; uIdx++) {
                var tryUrl = candidateUrls[uIdx];
                if (task.cancelled) break;
                try {
                    var resp = await fetch(tryUrl, { mode: "cors", credentials: "omit" });
                    if (!resp.ok) {
                        lastError = "HTTP " + resp.status;
                        continue;
                    }
                    var rawBlob = await resp.blob();
                    if (task.cancelled) break;

                    var fullBytes = await this.transcodeAndResizeToJpeg(rawBlob, this.defaultFullPx, this.jpegQuality);
                    var fullBlob = new Blob([fullBytes], { type: "image/jpeg" });
                    var iconBytes = await this.transcodeAndResizeToJpeg(fullBlob, this.defaultIconPx, 0.85);

                    var targetStore = (task.storageTier === 1) ? this.storeRom : this.storeCache;
                    await new Promise(function(r) {
                        self.putDualBlobBytes(targetStore, fullFileKey, fullBytes, iconFileKey, iconBytes, r);
                    });

                    var elapsedMs = Math.max(1, Math.round(performance.now() - startMs));
                    delete this.activeKeys[key];
                    this.sendDownloadResult(key, true, iconBytes.byteLength, fullBytes.byteLength, elapsedMs, task.storageTier, "");
                    return;
                } catch (err) {
                    lastError = err && err.message ? err.message : "WebGL fetch/decode error";
                }
            }

            delete this.activeKeys[key];
            if (!task.cancelled) {
                var failMs = Math.round(performance.now() - startMs);
                this.sendDownloadResult(key, false, 0, 0, failMs, 0, lastError);
            }
        },

        sendDownloadResult: function(key, success, iconBytes, fullBytes, durationMs, storageTier, errorMsg) {
            var payload = JSON.stringify({
                requestKey: key,
                success: success,
                iconSizeBytes: iconBytes,
                fullSizeBytes: fullBytes,
                durationMs: durationMs,
                storageTier: storageTier,
                errorMessage: errorMsg || ""
            });
            SendMessage(this.receiverName, "OnDownloadResultEvent", payload);
        },

        openFolderInspectorOverlay: function(requestKey) {
            var self = this;
            var existingModal = document.getElementById("nip-webgl-folder-modal");
            if (existingModal) existingModal.remove();

            var overlay = document.createElement("div");
            overlay.id = "nip-webgl-folder-modal";
            overlay.style.cssText = "position:fixed;inset:0;background:rgba(0,0,0,0.75);z-index:999999;display:flex;align-items:center;justify-content:center;font-family:monospace;color:#e6edf3;padding:20px;";

            var card = document.createElement("div");
            card.style.cssText = "background:#161b22;border:1px solid #30363d;border-radius:14px;max-width:740px;width:100%;max-height:85vh;display:flex;flex-direction:column;box-shadow:0 20px 50px rgba(0,0,0,0.8);overflow:hidden;";

            var header = document.createElement("div");
            header.style.cssText = "padding:14px 18px;border-bottom:1px solid #30363d;display:flex;justify-content:space-between;align-items:center;background:#0d1117;";
            header.innerHTML = "<div><strong style='font-size:14px;color:#58a6ff;'>📂 Native Image Bridge — IndexedDB Cache & ROM Explorer</strong><div style='font-size:11px;color:#8b949e;margin-top:3px;'>indexeddb://" + this.dbName + "/ (Unity WebGL + Web Shared DB)</div></div>";

            var closeBtn = document.createElement("button");
            closeBtn.textContent = "✕ Close Folder";
            closeBtn.style.cssText = "background:#21262d;color:#e6edf3;border:1px solid #30363d;border-radius:8px;padding:6px 12px;cursor:pointer;font-weight:bold;font-family:monospace;";
            closeBtn.onclick = function() { overlay.remove(); };
            header.appendChild(closeBtn);
            card.appendChild(header);

            var body = document.createElement("div");
            body.style.cssText = "padding:16px 18px;overflow-y:auto;display:flex;flex-direction:column;gap:10px;font-size:12px;";
            body.innerHTML = "<div style='color:#8b949e;'>Scanning IndexedDB object stores (permanent_rom & volatile_cache)...</div>";
            card.appendChild(body);
            overlay.appendChild(card);
            document.body.appendChild(overlay);

            this.openDatabase(function(db) {
                if (!db) {
                    body.innerHTML = "<div style='color:#f87171;'>IndexedDB unavailable.</div>";
                    return;
                }
                var items = [];
                var stores = [
                    { name: self.storeRom, tier: 1 },
                    { name: self.storeCache, tier: 0 }
                ];
                var doneStores = 0;

                stores.forEach(function(stObj) {
                    var tx = db.transaction([stObj.name], "readonly");
                    var store = tx.objectStore(stObj.name);
                    var reqKeys = store.getAllKeys();
                    var reqVals = store.getAll();
                    tx.oncomplete = function() {
                        var keys = reqKeys.result || [];
                        var vals = reqVals.result || [];
                        keys.forEach(function(k, idx) {
                            if (vals[idx] && vals[idx].byteLength > 0) {
                                items.push({ fileKey: k, bytes: vals[idx], tier: stObj.tier, storeName: stObj.name });
                            }
                        });
                        doneStores++;
                        if (doneStores === stores.length) {
                            body.innerHTML = "";
                            if (items.length === 0) {
                                body.innerHTML = "<div style='color:#f87171;'>No cached server images found yet in IndexedDB. Click Sync or select a Server Puzzle first.</div>";
                                return;
                            }
                            items.forEach(function(item) {
                                var isMatch = requestKey && item.fileKey.indexOf(requestKey) === 0;
                                var blob = new Blob([item.bytes], { type: "image/jpeg" });
                                var blobUrl = URL.createObjectURL(blob);

                                var row = document.createElement("div");
                                row.style.cssText = "display:flex;align-items:center;justify-content:space-between;gap:12px;padding:10px 12px;border-radius:10px;background:" + (isMatch ? "#1f2937" : "#0d1117") + ";border:1px solid " + (isMatch ? "#3b82f6" : "#30363d") + ";";
                                row.innerHTML = "<div style='display:flex;align-items:center;gap:12px;'>" +
                                    "<img src='" + blobUrl + "' style='width:52px;height:52px;object-fit:cover;border-radius:6px;border:1px solid #30363d;' />" +
                                    "<div><div style='font-weight:bold;color:#e6edf3;'>" + item.fileKey + (isMatch ? " <span style='color:#60a5fa;'>[ACTIVE]</span>" : "") + "</div>" +
                                    "<div style='color:#8b949e;font-size:11px;margin-top:2px;'>Folder: <span style='color:#34d399;'>indexeddb://" + self.dbName + "/" + item.storeName + "/" + item.fileKey + "</span></div>" +
                                    "<div style='color:#9ca3af;font-size:11px;margin-top:2px;'>Size: " + (item.bytes.byteLength / 1024).toFixed(1) + " KB</div></div></div>" +
                                    "<div style='display:flex;gap:8px;shrink:0;'>" +
                                    "<a href='" + blobUrl + "' target='_blank' style='background:#2563eb;color:#fff;text-decoration:none;padding:6px 10px;border-radius:6px;font-weight:bold;font-size:11px;'>↗ Open</a>" +
                                    "<a href='" + blobUrl + "' download='" + item.fileKey + "' style='background:#21262d;color:#e6edf3;border:1px solid #30363d;text-decoration:none;padding:6px 10px;border-radius:6px;font-weight:bold;font-size:11px;'>⬇ Save</a></div>";
                                body.appendChild(row);
                            });
                        }
                    };
                });
            });
        }
    },

    NIP_WebGL_Initialize: function(checkIntervalSec, maxConcurrentWorkers, iconSizePx, fullSizePx, jpegQuality, maxRetries) {
        NIPWebGL.maxConcurrent = Math.max(1, Math.min(2, maxConcurrentWorkers));
        NIPWebGL.defaultIconPx = iconSizePx > 0 ? iconSizePx : 256;
        NIPWebGL.defaultFullPx = fullSizePx > 0 ? fullSizePx : 1080;
        NIPWebGL.jpegQuality = Math.max(0.5, Math.min(1.0, jpegQuality / 100.0));
        NIPWebGL.maxRetries = Math.max(0, maxRetries);
        NIPWebGL.isOnline = navigator.onLine !== false;
        NIPWebGL.openDatabase(function() {});
    },

    NIP_WebGL_IsConnected: function() {
        return NIPWebGL.isOnline ? 1 : 0;
    },

    NIP_WebGL_EnqueueDownload: function(requestKeyPtr, urlPtr, priority, targetStorageTier) {
        var key = UTF8ToString(requestKeyPtr);
        var url = UTF8ToString(urlPtr);

        var existing = NIPWebGL.activeKeys[key];
        if (existing) {
            if (!existing.started && priority > existing.priority) {
                existing.priority = priority;
            }
            return;
        }

        var task = {
            requestKey: key,
            url: url,
            priority: priority,
            storageTier: targetStorageTier,
            seq: ++NIPWebGL.seqCounter,
            started: false,
            cancelled: false
        };
        NIPWebGL.activeKeys[key] = task;
        NIPWebGL.queue.push(task);
        NIPWebGL.pumpQueue();
    },

    // Decodes the cached IndexedDB JPEG in browser C++/GPU onto a bottom-up RGBA32 buffer (dimPx x dimPx)
    // and transfers raw pixels via HEAPU8 so Unity Texture2D.LoadRawTextureData works with 0 WASM JPEG decode overhead
    NIP_WebGL_ReadRgbaPixelsBottomUpAsync: function(requestKeyPtr, targetDimPx, callbackId) {
        var key = UTF8ToString(requestKeyPtr);
        var fileKey = key + "_" + NIPWebGL.defaultFullPx + ".jpg";
        var dim = targetDimPx > 0 ? targetDimPx : 512;

        NIPWebGL.getBlobBytes(fileKey, async function(uint8Bytes) {
            if (!uint8Bytes || uint8Bytes.byteLength === 0) {
                SendMessage(NIPWebGL.receiverName, "OnWebGLRgbaLoadedEvent", callbackId + "|0|0|" + dim);
                return;
            }
            try {
                var blob = new Blob([uint8Bytes], { type: "image/jpeg" });
                var bitmap = await createImageBitmap(blob);
                var canvas = document.createElement("canvas");
                canvas.width = dim;
                canvas.height = dim;
                var ctx = canvas.getContext("2d");
                // Flip vertically so y=0 is bottom-up, matching Unity Texture2D coordinate space
                ctx.translate(0, dim);
                ctx.scale(1, -1);
                ctx.drawImage(bitmap, 0, 0, dim, dim);
                bitmap.close();

                var imgData = ctx.getImageData(0, 0, dim, dim).data;
                var byteLen = imgData.byteLength;
                var bufferPtr = _malloc(byteLen);
                var heap = (typeof HEAPU8 !== "undefined") ? HEAPU8 : Module.HEAPU8;
                heap.set(imgData, bufferPtr);
                SendMessage(NIPWebGL.receiverName, "OnWebGLRgbaLoadedEvent", callbackId + "|" + bufferPtr + "|" + byteLen + "|" + dim);
            } catch (e) {
                SendMessage(NIPWebGL.receiverName, "OnWebGLRgbaLoadedEvent", callbackId + "|0|0|" + dim);
            }
        });
    },

    NIP_WebGL_FreeBuffer: function(bufferPtr) {
        if (bufferPtr) {
            _free(bufferPtr);
        }
    },

    NIP_WebGL_OpenCacheFolderModal: function(requestKeyPtr, sizePx) {
        var key = UTF8ToString(requestKeyPtr);
        NIPWebGL.openFolderInspectorOverlay(key);
    }
};

autoAddDeps(NativeImagePipelineWebGLPlugin, '$NIPWebGL');
mergeInto(LibraryManager.library, NativeImagePipelineWebGLPlugin);
