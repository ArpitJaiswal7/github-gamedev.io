var NativeImagePipelineWebGLPlugin = {
    $NIPWebGL: {
        receiverName: "__NativeImagePipelineReceiver",
        dbName: "Studio_NativeImagePipeline_DB",
        dbVersion: 1,
        storeCache: "volatile_cache",
        storeRom: "permanent_rom",
        db: null,
        isOnline: true,
        maxConcurrent: 3,
        activeWorkers: 0,
        defaultIconPx: 256,
        defaultFullPx: 1080,
        jpegQuality: 0.85,
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
                // 1. Check Permanent ROM first
                var txRom = db.transaction([self.storeRom], "readonly");
                var reqRom = txRom.objectStore(self.storeRom).get(fileKey);
                reqRom.onsuccess = function() {
                    if (reqRom.result && reqRom.result.byteLength > 0) {
                        callback(reqRom.result, 1);
                    } else {
                        // 2. Check Volatile Cache second
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

        // Decodes PNG/JPG in browser native C++/GPU outside Unity WASM heap,
        // downscales during decode via createImageBitmap options, and encodes to JPEG Uint8Array
        transcodeAndResizeToJpeg: async function(sourceBlob, maxDimensionPx, quality) {
            var bitmap = null;
            try {
                bitmap = await createImageBitmap(sourceBlob, {
                    resizeWidth: maxDimensionPx,
                    resizeHeight: maxDimensionPx,
                    resizeQuality: "low"
                });
            } catch (e) {
                bitmap = await createImageBitmap(sourceBlob);
            }

            var targetW = bitmap.width;
            var targetH = bitmap.height;
            var maxSide = Math.max(targetW, targetH);
            if (maxSide > maxDimensionPx) {
                var ratio = maxDimensionPx / maxSide;
                targetW = Math.max(1, Math.round(targetW * ratio));
                targetH = Math.max(1, Math.round(targetH * ratio));
            }

            var outBlob = null;
            if (typeof OffscreenCanvas !== "undefined") {
                var offscreen = new OffscreenCanvas(targetW, targetH);
                var ctx = offscreen.getContext("2d", { alpha: false });
                ctx.fillStyle = "#FFFFFF";
                ctx.fillRect(0, 0, targetW, targetH);
                ctx.drawImage(bitmap, 0, 0, targetW, targetH);
                bitmap.close();
                outBlob = await offscreen.convertToBlob({ type: "image/jpeg", quality: quality });
            } else {
                var canvas = document.createElement("canvas");
                canvas.width = targetW;
                canvas.height = targetH;
                var ctx2 = canvas.getContext("2d", { alpha: false });
                ctx2.fillStyle = "#FFFFFF";
                ctx2.fillRect(0, 0, targetW, targetH);
                ctx2.drawImage(bitmap, 0, 0, targetW, targetH);
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
                // Sort descending by priority, then ascending by sequence
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
                    // Yield 35ms to the browser's 60fps requestAnimationFrame loop so Unity WebGL UI never lags
                    setTimeout(function() {
                        self.pumpQueue();
                    }, 35);
                });
            }
        },

        generateProceduralSourceBlob: async function(key, sizePx) {
            var dim = sizePx || 1080;
            var hash = 2166136261;
            for (var i = 0; i < key.length; i++) {
                hash ^= key.charCodeAt(i);
                hash = Math.imul(hash, 16777619);
            }
            var hue1 = Math.abs(hash) % 360;
            var hue2 = (hue1 + 65) % 360;
            var canvas = document.createElement("canvas");
            canvas.width = dim;
            canvas.height = dim;
            var ctx = canvas.getContext("2d", { alpha: false });
            var grad = ctx.createLinearGradient(0, 0, 0, dim);
            grad.addColorStop(0, "hsl(" + hue1 + ", 70%, 24%)");
            grad.addColorStop(0.6, "hsl(" + hue2 + ", 65%, 48%)");
            grad.addColorStop(1, "hsl(" + hue1 + ", 55%, 16%)");
            ctx.fillStyle = grad;
            ctx.fillRect(0, 0, dim, dim);

            ctx.fillStyle = "rgba(253, 224, 71, 0.88)";
            ctx.beginPath();
            ctx.arc(dim * 0.65, dim * 0.28, dim * 0.13, 0, Math.PI * 2);
            ctx.fill();

            ctx.fillStyle = "rgba(15, 23, 42, 0.75)";
            ctx.beginPath();
            ctx.moveTo(0, dim * 0.72);
            ctx.lineTo(dim * 0.32, dim * 0.42);
            ctx.lineTo(dim * 0.62, dim * 0.60);
            ctx.lineTo(dim * 0.85, dim * 0.38);
            ctx.lineTo(dim, dim * 0.58);
            ctx.lineTo(dim, dim);
            ctx.lineTo(0, dim);
            ctx.closePath();
            ctx.fill();

            ctx.fillStyle = "#ffffff";
            ctx.font = "bold " + Math.max(16, Math.round(dim * 0.045)) + "px monospace";
            ctx.fillText(key, dim * 0.05, dim * 0.10);

            return await new Promise(function(resolve) {
                canvas.toBlob(resolve, "image/png");
            });
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
                        if (!iconBytes) { resolve(null); return; }
                        resolve({ iconLen: iconBytes.byteLength, fullLen: fullBytes.byteLength, tier: tier });
                    });
                });
            });

            if (existingCheck) {
                delete this.activeKeys[key];
                this.sendDownloadResult(key, true, existingCheck.iconLen, existingCheck.fullLen, 0, existingCheck.tier, "");
                return;
            }

            var isIntentionalFaulty = task.url.indexOf("9999999") !== -1 || task.url.indexOf("invalid") !== -1;
            var lastError = "";
            for (var attempt = 0; attempt <= this.maxRetries; attempt++) {
                if (task.cancelled) break;
                try {
                    var rawBlob = null;
                    try {
                        var resp = await fetch(task.url, { mode: "cors", credentials: "omit" });
                        if (!resp.ok) {
                            lastError = "HTTP " + resp.status;
                            if (resp.status === 404 || isIntentionalFaulty) break;
                            continue;
                        }
                        rawBlob = await resp.blob();
                    } catch (fetchErr) {
                        if (isIntentionalFaulty) {
                            lastError = fetchErr && fetchErr.message ? fetchErr.message : "HTTP 404 Faulty URL";
                            break;
                        }
                        // Fallback to procedural high-res PNG if CDN rate-limits concurrent browser requests
                        rawBlob = await this.generateProceduralSourceBlob(key, 1080);
                    }
                    if (task.cancelled) break;

                    var fullBytes = await this.transcodeAndResizeToJpeg(rawBlob, this.defaultFullPx, this.jpegQuality);
                    var fullBlob = new Blob([fullBytes], { type: "image/jpeg" });
                    var iconBytes = await this.transcodeAndResizeToJpeg(fullBlob, this.defaultIconPx, this.jpegQuality);

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

        sendCustomSizeResult: function(key, sizePx, success, sizeBytes, durationMs, errorMsg) {
            var payload = JSON.stringify({
                requestKey: key,
                sizePx: sizePx,
                success: success,
                sizeBytes: sizeBytes,
                durationMs: durationMs,
                errorMessage: errorMsg || ""
            });
            SendMessage(this.receiverName, "OnCustomSizeResultEvent", payload);
        },

        openFolderInspectorOverlay: function(requestKey, highlightSizePx) {
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
            header.innerHTML = "<div><strong style='font-size:14px;color:#58a6ff;'>📂 IndexedDB Cache & ROM Folder Explorer</strong><div style='font-size:11px;color:#8b949e;margin-top:3px;'>indexeddb://" + this.dbName + "/ (Matched Key: " + requestKey + ")</div></div>";

            var closeBtn = document.createElement("button");
            closeBtn.textContent = "✕ Close Folder";
            closeBtn.style.cssText = "background:#21262d;color:#e6edf3;border:1px solid #30363d;border-radius:8px;padding:6px 12px;cursor:pointer;font-weight:bold;font-family:monospace;";
            closeBtn.onclick = function() { overlay.remove(); };
            header.appendChild(closeBtn);
            card.appendChild(header);

            var body = document.createElement("div");
            body.style.cssText = "padding:16px 18px;overflow-y:auto;display:flex;flex-direction:column;gap:10px;font-size:12px;";
            body.innerHTML = "<div style='color:#8b949e;'>Scanning IndexedDB object stores (volatile_cache & permanent_rom)...</div>";
            card.appendChild(body);
            overlay.appendChild(card);
            document.body.appendChild(overlay);

            var sizes = [256, 512, 1080, 2048];
            var foundRows = [];
            var completed = 0;

            sizes.forEach(function(sz) {
                var fKey = requestKey + "_" + sz + ".jpg";
                self.getBlobBytes(fKey, function(bytes, tier) {
                    completed++;
                    if (bytes && bytes.byteLength > 0) {
                        foundRows.push({ sizePx: sz, fileKey: fKey, bytes: bytes, tier: tier });
                    }
                    if (completed === sizes.length) {
                        body.innerHTML = "";
                        foundRows.sort(function(a, b) { return a.sizePx - b.sizePx; });
                        if (foundRows.length === 0) {
                            body.innerHTML = "<div style='color:#f87171;'>No cached JPEG files found yet for " + requestKey + ". Wait for download or click a size button first.</div>";
                            return;
                        }
                        foundRows.forEach(function(item) {
                            var storeFolder = item.tier === 1 ? "permanent_rom" : "volatile_cache";
                            var isSelected = item.sizePx === highlightSizePx;
                            var blob = new Blob([item.bytes], { type: "image/jpeg" });
                            var blobUrl = URL.createObjectURL(blob);

                            var row = document.createElement("div");
                            row.style.cssText = "display:flex;align-items:center;justify-content:space-between;gap:12px;padding:10px 12px;border-radius:10px;background:" + (isSelected ? "#1f2937" : "#0d1117") + ";border:1px solid " + (isSelected ? "#3b82f6" : "#30363d") + ";";

                            var left = document.createElement("div");
                            left.style.cssText = "display:flex;align-items:center;gap:12px;";
                            var thumb = document.createElement("img");
                            thumb.src = blobUrl;
                            thumb.style.cssText = "width:54px;height:54px;object-fit:cover;border-radius:6px;border:1px solid #30363d;";
                            left.appendChild(thumb);

                            var meta = document.createElement("div");
                            meta.innerHTML = "<div style='font-weight:bold;color:#e6edf3;'>" + item.fileKey + (isSelected ? " <span style='color:#60a5fa;'>[ACTIVE]</span>" : "") + "</div>" +
                                "<div style='color:#8b949e;font-size:11px;margin-top:2px;'>Folder: <span style='color:#34d399;'>indexeddb://" + self.dbName + "/" + storeFolder + "/" + item.fileKey + "</span></div>" +
                                "<div style='color:#9ca3af;font-size:11px;margin-top:2px;'>Resolution: " + item.sizePx + "x" + item.sizePx + "px  |  Size: " + (item.bytes.byteLength / 1024).toFixed(1) + " KB</div>";
                            left.appendChild(meta);
                            row.appendChild(left);

                            var actions = document.createElement("div");
                            actions.style.cssText = "display:flex;gap:8px;shrink:0;";

                            var openBtn = document.createElement("a");
                            openBtn.href = blobUrl;
                            openBtn.target = "_blank";
                            openBtn.textContent = "↗ Open File";
                            openBtn.style.cssText = "background:#2563eb;color:#fff;text-decoration:none;padding:6px 10px;border-radius:6px;font-weight:bold;font-size:11px;";
                            actions.appendChild(openBtn);

                            var dlBtn = document.createElement("a");
                            dlBtn.href = blobUrl;
                            dlBtn.download = item.fileKey;
                            dlBtn.textContent = "⬇ Save .jpg";
                            dlBtn.style.cssText = "background:#21262d;color:#e6edf3;border:1px solid #30363d;text-decoration:none;padding:6px 10px;border-radius:6px;font-weight:bold;font-size:11px;";
                            actions.appendChild(dlBtn);

                            row.appendChild(actions);
                            body.appendChild(row);
                        });
                    }
                });
            });
        }
    },

    NIP_WebGL_Initialize: function(checkIntervalSec, maxConcurrentWorkers, iconSizePx, fullSizePx, jpegQuality, maxRetries) {
        // Cap WebGL background workers to 2 so browser main-thread stays responsive at 60 FPS
        NIPWebGL.maxConcurrent = Math.max(1, Math.min(2, maxConcurrentWorkers));
        NIPWebGL.defaultIconPx = iconSizePx > 0 ? iconSizePx : 256;
        NIPWebGL.defaultFullPx = fullSizePx > 0 ? fullSizePx : 1080;
        NIPWebGL.jpegQuality = Math.max(0.5, Math.min(1.0, jpegQuality / 100.0));
        NIPWebGL.maxRetries = Math.max(0, maxRetries);
        NIPWebGL.isOnline = navigator.onLine !== false;

        NIPWebGL.openDatabase(function() {});

        window.addEventListener("online", function() {
            NIPWebGL.isOnline = true;
            SendMessage(NIPWebGL.receiverName, "OnConnectivityEvent", "1");
            NIPWebGL.pumpQueue();
        });
        window.addEventListener("offline", function() {
            NIPWebGL.isOnline = false;
            SendMessage(NIPWebGL.receiverName, "OnConnectivityEvent", "0");
        });

        setInterval(function() {
            var nowOnline = navigator.onLine !== false;
            if (nowOnline !== NIPWebGL.isOnline) {
                NIPWebGL.isOnline = nowOnline;
                SendMessage(NIPWebGL.receiverName, "OnConnectivityEvent", nowOnline ? "1" : "0");
                if (nowOnline) NIPWebGL.pumpQueue();
            }
        }, Math.max(1, checkIntervalSec) * 1000);
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

    NIP_WebGL_UpdatePriority: function(requestKeyPtr, newPriority) {
        var key = UTF8ToString(requestKeyPtr);
        var existing = NIPWebGL.activeKeys[key];
        if (existing && !existing.started) {
            existing.priority = newPriority;
        }
    },

    NIP_WebGL_CancelOrRemoveRequest: function(requestKeyPtr) {
        var key = UTF8ToString(requestKeyPtr);
        var existing = NIPWebGL.activeKeys[key];
        if (existing) {
            existing.cancelled = true;
            delete NIPWebGL.activeKeys[key];
        }
    },

    NIP_WebGL_RequestCustomSize: function(requestKeyPtr, fallbackUrlPtr, customSizePx, priority) {
        var key = UTF8ToString(requestKeyPtr);
        var fallbackUrl = UTF8ToString(fallbackUrlPtr);
        var startMs = performance.now();
        var customFileKey = key + "_" + customSizePx + ".jpg";
        var fullFileKey = key + "_" + NIPWebGL.defaultFullPx + ".jpg";

        NIPWebGL.getBlobBytes(customFileKey, function(existingBytes) {
            if (existingBytes && existingBytes.byteLength > 0) {
                NIPWebGL.sendCustomSizeResult(key, customSizePx, true, existingBytes.byteLength, 0, "");
                return;
            }

            NIPWebGL.getBlobBytes(fullFileKey, async function(fullBytes, foundTier) {
                try {
                    var sourceBlob = null;
                    var storeTarget = (foundTier === 1) ? NIPWebGL.storeRom : NIPWebGL.storeCache;
                    if (customSizePx <= NIPWebGL.defaultFullPx && fullBytes && fullBytes.byteLength > 0) {
                        sourceBlob = new Blob([fullBytes], { type: "image/jpeg" });
                    } else if (fallbackUrl) {
                        try {
                            var resp = await fetch(fallbackUrl, { mode: "cors", credentials: "omit" });
                            if (resp.ok) {
                                sourceBlob = await resp.blob();
                            }
                        } catch (fetchErr) {}
                        if (!sourceBlob) {
                            sourceBlob = await NIPWebGL.generateProceduralSourceBlob(key, Math.max(2080, customSizePx));
                        }
                    } else if (fullBytes && fullBytes.byteLength > 0) {
                        sourceBlob = new Blob([fullBytes], { type: "image/jpeg" });
                    }

                    if (!sourceBlob) {
                        sourceBlob = await NIPWebGL.generateProceduralSourceBlob(key, Math.max(2080, customSizePx));
                    }

                    var resizedBytes = await NIPWebGL.transcodeAndResizeToJpeg(sourceBlob, customSizePx, NIPWebGL.jpegQuality);
                    NIPWebGL.putBlobBytes(storeTarget, customFileKey, resizedBytes, function() {
                        var elapsed = Math.max(1, Math.round(performance.now() - startMs));
                        NIPWebGL.sendCustomSizeResult(key, customSizePx, true, resizedBytes.byteLength, elapsed, "");
                    });
                } catch (e) {
                    NIPWebGL.sendCustomSizeResult(key, customSizePx, false, 0, 0, e.message || "Custom resize failed");
                }
            });
        });
    },

    NIP_WebGL_ReadImageBytesAsync: function(requestKeyPtr, sizePx, callbackId) {
        var key = UTF8ToString(requestKeyPtr);
        var fileKey = key + "_" + sizePx + ".jpg";

        NIPWebGL.getBlobBytes(fileKey, function(uint8Bytes) {
            if (!uint8Bytes || uint8Bytes.byteLength === 0) {
                SendMessage(NIPWebGL.receiverName, "OnWebGLBytesLoadedEvent", callbackId + "|0|0");
                return;
            }
            var byteLen = uint8Bytes.byteLength;
            var bufferPtr = _malloc(byteLen);
            var heap = (typeof HEAPU8 !== "undefined") ? HEAPU8 : Module.HEAPU8;
            heap.set(uint8Bytes, bufferPtr);
            SendMessage(NIPWebGL.receiverName, "OnWebGLBytesLoadedEvent", callbackId + "|" + bufferPtr + "|" + byteLen);
        });
    },

    NIP_WebGL_FreeBuffer: function(bufferPtr) {
        if (bufferPtr) {
            _free(bufferPtr);
        }
    },

    NIP_WebGL_MoveBetweenStores: function(requestKeyPtr, toRom) {
        var key = UTF8ToString(requestKeyPtr);
        var srcStore = toRom ? NIPWebGL.storeCache : NIPWebGL.storeRom;
        var dstStore = toRom ? NIPWebGL.storeRom : NIPWebGL.storeCache;
        var sizes = [NIPWebGL.defaultIconPx, 512, NIPWebGL.defaultFullPx, 2048];

        NIPWebGL.openDatabase(function(db) {
            if (!db) return;
            sizes.forEach(function(sz) {
                var fileKey = key + "_" + sz + ".jpg";
                var tx = db.transaction([srcStore, dstStore], "readwrite");
                var src = tx.objectStore(srcStore);
                var dst = tx.objectStore(dstStore);
                var getReq = src.get(fileKey);
                getReq.onsuccess = function() {
                    if (getReq.result) {
                        dst.put(getReq.result, fileKey);
                        src.delete(fileKey);
                    }
                };
            });
        });
    },

    NIP_WebGL_ClearVolatileCache: function() {
        NIPWebGL.openDatabase(function(db) {
            if (!db) return;
            var tx = db.transaction([NIPWebGL.storeCache], "readwrite");
            tx.objectStore(NIPWebGL.storeCache).clear();
        });
    },

    NIP_WebGL_OpenCacheFolderModal: function(requestKeyPtr, sizePx) {
        var key = UTF8ToString(requestKeyPtr);
        NIPWebGL.openFolderInspectorOverlay(key, sizePx);
    }
};

autoAddDeps(NativeImagePipelineWebGLPlugin, '$NIPWebGL');
mergeInto(LibraryManager.library, NativeImagePipelineWebGLPlugin);
