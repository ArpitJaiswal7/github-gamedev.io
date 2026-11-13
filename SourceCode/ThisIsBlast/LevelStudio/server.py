#!/usr/bin/env python3
"""
This is Blast! - Level Studio & OmniPace Local Server
Serves the web studio interface, OmniPace dashboard, and provides complete
REST APIs for level generation, vault batching, journeys, cohorts, and live progression.
Zero external dependencies (uses standard Python 3 libraries only).
"""

import os
import sys
import json
import re
import shutil
import webbrowser
import zipfile
import io
from datetime import date
from http.server import HTTPServer, ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
from urllib.parse import urlparse, parse_qs

if sys.stdout and hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

PORT = 8080
STUDIO_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = STUDIO_DIR.parent
LEVELS_DIR = PROJECT_ROOT / "Assets" / "Resources" / "Levels"
BACKEND_DIR = PROJECT_ROOT / "backend"
DASHBOARD_DIR = BACKEND_DIR / "Dashboard"
HTML_VERSION_DIR = PROJECT_ROOT / "HTML_version"


def safe_id(val, default="default"):
    """Sanitizes an identifier to prevent directory traversal."""
    cleaned = re.sub(r'[^a-zA-Z0-9_\-]', '', str(val or '')).strip()
    return cleaned if cleaned else default


class StudioRequestHandler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(STUDIO_DIR), **kwargs)

    def do_OPTIONS(self):
        self.send_response(200)
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        self.end_headers()

    def do_GET(self):
        parsed_url = urlparse(self.path)
        clean_path = parsed_url.path.lower().strip('/')
        query_params = parse_qs(parsed_url.query)

        # 1. Dashboard UI Route
        if clean_path in ('dashboard', 'dashboard.html', 'dashboard/index.html', 'dashboard.com') or clean_path.startswith('dashboard'):
            dashboard_html = DASHBOARD_DIR / "index.html"
            if dashboard_html.exists():
                self.send_response(200)
                self.send_header("Content-Type", "text/html; charset=utf-8")
                self.end_headers()
                with open(dashboard_html, "rb") as f:
                    self.wfile.write(f.read())
                return

        # 2. Project Info Endpoint
        if parsed_url.path == "/api/project-info":
            self.send_json_response(self.get_project_info())
            return

        # 2b. HTML Version / Playtest UI Route
        if clean_path in ('html_version', 'html_version/index.html', 'play', 'play.html', 'playtest') or clean_path.startswith('html_version'):
            sub = parsed_url.path.strip('/')
            if sub.lower().startswith('html_version'):
                sub = sub[len('html_version'):].lstrip('/')
            elif sub.lower().startswith('play'):
                sub = ''

            if not sub or sub.lower() in ('index.html', ''):
                target_file = HTML_VERSION_DIR / 'index.html'
            else:
                target_file = (HTML_VERSION_DIR / sub).resolve()

            if target_file.exists() and (HTML_VERSION_DIR == target_file or HTML_VERSION_DIR in target_file.parents or target_file.parent == HTML_VERSION_DIR):
                ext = target_file.suffix.lower()
                content_types = {
                    '.html': 'text/html; charset=utf-8',
                    '.js': 'application/javascript; charset=utf-8',
                    '.css': 'text/css; charset=utf-8',
                    '.json': 'application/json; charset=utf-8',
                    '.png': 'image/png',
                    '.jpg': 'image/jpeg',
                    '.jpeg': 'image/jpeg',
                    '.svg': 'image/svg+xml',
                    '.ico': 'image/x-icon',
                    '.woff': 'font/woff',
                    '.woff2': 'font/woff2'
                }
                ctype = content_types.get(ext, 'application/octet-stream')
                self.send_response(200)
                self.send_header('Content-Type', ctype)
                self.end_headers()
                with open(target_file, 'rb') as f:
                    self.wfile.write(f.read())
                return

        # 3. OmniPace Dashboard Overview Endpoint
        if parsed_url.path == "/api/dashboard/overview":
            self.handle_dashboard_overview(query_params)
            return

        # 3b. Archetype Batch Contents Endpoint
        if parsed_url.path == "/api/dashboard/archetype-contents":
            self.handle_dashboard_archetype_contents(query_params)
            return

        # 3c. Open Folder in Explorer Endpoint
        if parsed_url.path == "/api/dashboard/open-folder":
            self.handle_dashboard_open_folder(query_params)
            return

        # 3d. Batch Health & Integrity Validation Endpoint
        if parsed_url.path == "/api/dashboard/validate-batch":
            self.handle_dashboard_validate_batch(query_params)
            return

        # 3e. Level Grid Preview Endpoint
        if parsed_url.path == "/api/dashboard/level-preview":
            self.handle_dashboard_level_preview(query_params)
            return

        # 3f. Export Batch ZIP Endpoint
        if parsed_url.path == "/api/dashboard/export-batch-zip":
            self.handle_dashboard_export_batch_zip(query_params)
            return

        # 3g. LiveOps Progression API for Unity & Game Clients
        if parsed_url.path == "/api/game/progression":
            self.handle_game_progression(query_params)
            return

        # 4. Fallback to SimpleHTTPRequestHandler (serving static studio files)
        super().do_GET()

    def do_POST(self):
        parsed_url = urlparse(self.path)
        path = parsed_url.path

        if path == "/api/save-levels":
            self.handle_save_levels()
        elif path == "/api/save-manifest":
            self.handle_save_manifest()
        elif path == "/api/dashboard/save-journey":
            self.handle_dashboard_save_journey()
        elif path == "/api/dashboard/save-cohort":
            self.handle_dashboard_save_cohort()
        elif path == "/api/dashboard/toggle-batch-lock":
            self.handle_dashboard_toggle_batch_lock()
        elif path == "/api/dashboard/export-studio-batch":
            self.handle_dashboard_export_studio_batch()
        elif path == "/api/dashboard/delete-batch":
            self.handle_dashboard_delete_batch()
        elif path == "/api/dashboard/open-folder":
            self.handle_dashboard_open_folder_post()
        elif path == "/api/dashboard/update-batch-metadata":
            self.handle_dashboard_update_batch_metadata()
        elif path == "/api/dashboard/clone-batch":
            self.handle_dashboard_clone_batch()
        else:
            self.send_error(404, "Endpoint not found")

    def read_json_body(self):
        try:
            content_length = int(self.headers.get("Content-Length", 0))
            if content_length <= 0:
                return {}
            post_body = self.rfile.read(content_length)
            return json.loads(post_body.decode("utf-8"))
        except Exception:
            return {}

    # =========================================================================
    # 🎛️ OMNIPACE DASHBOARD REST APIS
    # =========================================================================

    def handle_dashboard_overview(self, query_params):
        """Returns batches, journeys, cohorts, and config for the active game & vault."""
        try:
            game = safe_id(query_params.get("game", ["blast"])[0], "blast")
            vault = safe_id(query_params.get("vault", ["vault_v1"])[0], "vault_v1")

            game_dir = BACKEND_DIR / "games" / game
            vault_dir = game_dir / vault

            # 1. Read config.json
            config_data = {}
            config_file = game_dir / "config.json"
            if config_file.exists():
                try:
                    with open(config_file, "r", encoding="utf-8") as f:
                        config_data = json.load(f)
                except Exception:
                    pass

            # 2. Discover Archetypes & Batches
            archetypes = []
            if vault_dir.exists():
                for arch_path in sorted(vault_dir.iterdir()):
                    if arch_path.is_dir() and arch_path.name.startswith("arc_"):
                        batches = []
                        for b_path in sorted(arch_path.iterdir()):
                            if b_path.is_dir() and b_path.name.startswith("b"):
                                manifest_file = b_path / "batch_manifest.json"
                                manifest_data = {}
                                if manifest_file.exists():
                                    try:
                                        with open(manifest_file, "r", encoding="utf-8") as mf:
                                            manifest_data = json.load(mf)
                                    except Exception:
                                        pass

                                json_files = list(b_path.glob("l*.json"))
                                bin_files = list(b_path.glob("l*.bin"))
                                file_count = len(json_files) + len(bin_files)
                                level_count = manifest_data.get("levelCount", file_count // 3 if file_count >= 3 else file_count)

                                batches.append({
                                    "batchId": b_path.name,
                                    "manifest": manifest_data,
                                    "fileCount": file_count,
                                    "levelCount": level_count,
                                    "isLocked": bool(manifest_data.get("locked", False)),
                                    "label": manifest_data.get("label", b_path.name.upper()),
                                    "description": manifest_data.get("description", ""),
                                    "generatedAt": manifest_data.get("generatedAt", "")
                                })

                        archetypes.append({
                            "archetypeId": arch_path.name,
                            "archetypeName": arch_path.name.replace("arc_", "").replace("_", " ").title(),
                            "batches": batches
                        })

            # 3. Discover Journeys
            journeys = []
            j_dir = game_dir / "journeys"
            if j_dir.exists():
                for jf in sorted(j_dir.glob("*.json")):
                    try:
                        with open(jf, "r", encoding="utf-8") as f:
                            jdata = json.load(f)
                            if "journeyId" not in jdata:
                                jdata["journeyId"] = jf.stem
                            journeys.append(jdata)
                    except Exception:
                        pass

            # 4. Discover Cohorts
            cohorts = []
            c_dir = game_dir / "cohorts"
            if c_dir.exists():
                for cf in sorted(c_dir.glob("*.json")):
                    try:
                        with open(cf, "r", encoding="utf-8") as f:
                            cdata = json.load(f)
                            if "cohortId" not in cdata:
                                cdata["cohortId"] = cf.stem
                            cohorts.append(cdata)
                    except Exception:
                        pass

            self.send_json_response({
                "success": True,
                "game": game,
                "vault": vault,
                "config": config_data,
                "archetypes": archetypes,
                "journeys": journeys,
                "cohorts": cohorts
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_dashboard_save_journey(self):
        """Saves or updates a journey JSON in backend/games/{game}/journeys/{journeyId}.json."""
        try:
            payload = self.read_json_body()
            game = safe_id(payload.get("game", "blast"), "blast")
            journey_id = safe_id(payload.get("journeyId"), "")
            journey_data = payload.get("journey")

            if not journey_id or not isinstance(journey_data, dict):
                self.send_json_response({"success": False, "error": "Invalid journeyId or payload"}, status_code=400)
                return

            j_dir = BACKEND_DIR / "games" / game / "journeys"
            j_dir.mkdir(parents=True, exist_ok=True)

            journey_data["journeyId"] = journey_id
            file_path = j_dir / f"{journey_id}.json"
            with open(file_path, "w", encoding="utf-8") as f:
                json.dump(journey_data, f, indent=2)

            self.send_json_response({
                "success": True,
                "journeyId": journey_id,
                "filePath": str(file_path)
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_dashboard_save_cohort(self):
        """Saves or updates a cohort JSON in backend/games/{game}/cohorts/{cohortId}.json."""
        try:
            payload = self.read_json_body()
            game = safe_id(payload.get("game", "blast"), "blast")
            cohort_id = safe_id(payload.get("cohortId"), "")
            cohort_data = payload.get("cohort")

            if not cohort_id or not isinstance(cohort_data, dict):
                self.send_json_response({"success": False, "error": "Invalid cohortId or payload"}, status_code=400)
                return

            c_dir = BACKEND_DIR / "games" / game / "cohorts"
            c_dir.mkdir(parents=True, exist_ok=True)

            cohort_data["cohortId"] = cohort_id
            file_path = c_dir / f"{cohort_id}.json"
            with open(file_path, "w", encoding="utf-8") as f:
                json.dump(cohort_data, f, indent=2)

            self.send_json_response({
                "success": True,
                "cohortId": cohort_id,
                "filePath": str(file_path)
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_dashboard_toggle_batch_lock(self):
        """Toggles 'locked' true/false inside batch_manifest.json for a specific batch."""
        try:
            payload = self.read_json_body()
            game = safe_id(payload.get("game", "blast"), "blast")
            vault = safe_id(payload.get("vault", "vault_v1"), "vault_v1")
            archetype = safe_id(payload.get("archetype", ""), "")
            batch_id = safe_id(payload.get("batchId", ""), "")
            locked = bool(payload.get("locked", False))

            if not archetype or not batch_id:
                self.send_json_response({"success": False, "error": "Missing archetype or batchId"}, status_code=400)
                return

            manifest_path = BACKEND_DIR / "games" / game / vault / archetype / batch_id / "batch_manifest.json"
            manifest_data = {}
            if manifest_path.exists():
                try:
                    with open(manifest_path, "r", encoding="utf-8") as f:
                        manifest_data = json.load(f)
                except Exception:
                    pass

            manifest_data["batchId"] = batch_id
            manifest_data["archetype"] = archetype
            manifest_data["locked"] = locked

            manifest_path.parent.mkdir(parents=True, exist_ok=True)
            with open(manifest_path, "w", encoding="utf-8") as f:
                json.dump(manifest_data, f, indent=2)

            self.send_json_response({
                "success": True,
                "batchId": batch_id,
                "archetype": archetype,
                "locked": locked
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_dashboard_delete_batch(self):
        """Safely deletes an unlocked batch folder from backend/games/{game}/{vault}/{archetype}/{batchId}."""
        try:
            payload = self.read_json_body()
            game = safe_id(payload.get("game", "blast"), "blast")
            vault = safe_id(payload.get("vault", "vault_v1"), "vault_v1")
            archetype = safe_id(payload.get("archetype", ""), "")
            batch_id = safe_id(payload.get("batchId", ""), "")

            if not archetype or not batch_id:
                self.send_json_response({"success": False, "error": "Missing archetype or batchId"}, status_code=400)
                return

            batch_dir = BACKEND_DIR / "games" / game / vault / archetype / batch_id
            if not batch_dir.exists():
                self.send_json_response({"success": False, "error": f"Batch '{batch_id}' not found in {archetype}"}, status_code=404)
                return

            # Governance check: Locked batches are production immutable and can NEVER be deleted
            manifest_path = batch_dir / "batch_manifest.json"
            if manifest_path.exists():
                try:
                    with open(manifest_path, "r", encoding="utf-8") as f:
                        m_data = json.load(f)
                        if m_data.get("locked", False):
                            self.send_json_response({
                                "success": False,
                                "error": f"Batch '{batch_id.upper()}' is locked (Production Immutable). Please unlock it before deleting."
                            }, status_code=403)
                            return
                except Exception:
                    pass

            # Permanently remove the batch directory and its binaries
            shutil.rmtree(batch_dir)

            self.send_json_response({
                "success": True,
                "batchId": batch_id,
                "archetype": archetype,
                "message": f"Batch '{batch_id.upper()}' was permanently deleted."
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_dashboard_open_folder_post(self):
        try:
            payload = self.read_json_body()
            game = safe_id(payload.get("game", "blast"), "blast")
            vault = safe_id(payload.get("vault", "vault_v1"), "vault_v1")
            archetype = safe_id(payload.get("archetype", ""), "")
            batch = safe_id(payload.get("batch", ""), "")
            self._open_folder_internal(game, vault, archetype, batch)
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_dashboard_open_folder(self, query_params):
        try:
            game = safe_id(query_params.get("game", ["blast"])[0], "blast")
            vault = safe_id(query_params.get("vault", ["vault_v1"])[0], "vault_v1")
            archetype = safe_id(query_params.get("archetype", [""])[0], "")
            batch = safe_id(query_params.get("batch", [""])[0], "")
            self._open_folder_internal(game, vault, archetype, batch)
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def _open_folder_internal(self, game, vault, archetype, batch):
        target_dir = BACKEND_DIR / "games" / game / vault
        if archetype:
            target_dir = target_dir / archetype
        if batch:
            target_dir = target_dir / batch

        target_path = target_dir.resolve()
        backend_resolved = BACKEND_DIR.resolve()

        if not str(target_path).startswith(str(backend_resolved)):
            self.send_json_response({"success": False, "error": "Access denied"}, status_code=403)
            return

        if not target_path.exists():
            self.send_json_response({"success": False, "error": f"Folder not found: {target_path}"}, status_code=404)
            return

        if sys.platform == "win32":
            os.startfile(str(target_path))
        elif sys.platform == "darwin":
            import subprocess
            subprocess.Popen(["open", str(target_path)])
        else:
            import subprocess
            subprocess.Popen(["xdg-open", str(target_path)])

        self.send_json_response({
            "success": True,
            "path": str(target_path),
            "name": target_path.name
        })

    def handle_dashboard_archetype_contents(self, query_params):
        """Returns detailed file structure and metadata for all batches in an archetype."""
        try:
            game = safe_id(query_params.get("game", ["blast"])[0], "blast")
            vault = safe_id(query_params.get("vault", ["vault_v1"])[0], "vault_v1")
            archetype = safe_id(query_params.get("archetype", [""])[0], "")

            if not archetype:
                self.send_json_response({"success": False, "error": "Missing archetype parameter"}, status_code=400)
                return

            arch_dir = (BACKEND_DIR / "games" / game / vault / archetype).resolve()
            backend_resolved = BACKEND_DIR.resolve()

            if not str(arch_dir).startswith(str(backend_resolved)) or not arch_dir.exists():
                self.send_json_response({"success": False, "error": "Archetype folder not found"}, status_code=404)
                return

            batches = []
            for b_path in sorted(arch_dir.iterdir()):
                if b_path.is_dir() and b_path.name.startswith("b"):
                    manifest_file = b_path / "batch_manifest.json"
                    manifest_data = {}
                    if manifest_file.exists():
                        try:
                            with open(manifest_file, "r", encoding="utf-8") as mf:
                                manifest_data = json.load(mf)
                        except Exception:
                            pass

                    files = []
                    total_bytes = 0
                    for f in sorted(b_path.iterdir()):
                        if f.is_file():
                            st = f.stat()
                            total_bytes += st.st_size
                            files.append({
                                "name": f.name,
                                "size": st.st_size,
                                "ext": f.suffix.lower().lstrip("."),
                                "mtime": int(st.st_mtime)
                            })

                    batches.append({
                        "batchId": b_path.name,
                        "batchPath": str(b_path),
                        "label": manifest_data.get("label", b_path.name.upper()),
                        "description": manifest_data.get("description", ""),
                        "isLocked": bool(manifest_data.get("locked", False)),
                        "levelCount": manifest_data.get("levelCount", len(files) // 3 if len(files) >= 3 else len(files)),
                        "manifest": manifest_data,
                        "fileCount": len(files),
                        "totalBytes": total_bytes,
                        "files": files
                    })

            self.send_json_response({
                "success": True,
                "game": game,
                "vault": vault,
                "archetypeId": archetype,
                "archetypeName": archetype.replace("arc_", "").replace("_", " ").title(),
                "folderPath": str(arch_dir),
                "batches": batches
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_dashboard_validate_batch(self, query_params):
        """Audits batch integrity: manifest, seeds (clb, frg, tgt), binary & json pair integrity."""
        try:
            game = safe_id(query_params.get("game", ["blast"])[0], "blast")
            vault = safe_id(query_params.get("vault", ["vault_v1"])[0], "vault_v1")
            archetype = safe_id(query_params.get("archetype", [""])[0], "")
            batch = safe_id(query_params.get("batch", [""])[0], "")

            batch_dir = (BACKEND_DIR / "games" / game / vault / archetype / batch).resolve()
            if not batch_dir.exists():
                self.send_json_response({"success": False, "error": "Batch directory not found"}, status_code=404)
                return

            manifest_file = batch_dir / "batch_manifest.json"
            if not manifest_file.exists():
                self.send_json_response({"success": False, "error": "Missing batch_manifest.json"}, status_code=400)
                return

            with open(manifest_file, "r", encoding="utf-8") as f:
                manifest = json.load(f)

            level_count = manifest.get("levelCount", 0)
            missing_files = []
            corrupt_files = []
            verified_levels = 0

            for lvl in range(1, level_count + 1):
                lvl_ok = True
                for seed in ["clb", "frg", "tgt"]:
                    for ext in ["json", "bin"]:
                        fn = f"l{lvl:03d}_{seed}.{ext}"
                        fp = batch_dir / fn
                        if not fp.exists():
                            missing_files.append(fn)
                            lvl_ok = False
                        elif fp.stat().st_size == 0:
                            corrupt_files.append(fn)
                            lvl_ok = False
                if lvl_ok:
                    verified_levels += 1

            total_files = len(list(batch_dir.glob("*.*")))
            expected_files = (level_count * 6) + 1
            is_healthy = (len(missing_files) == 0 and len(corrupt_files) == 0)

            self.send_json_response({
                "success": True,
                "batchId": batch,
                "archetype": archetype,
                "isHealthy": is_healthy,
                "levelCount": level_count,
                "verifiedLevels": verified_levels,
                "expectedFiles": expected_files,
                "actualFiles": total_files,
                "missingCount": len(missing_files),
                "corruptCount": len(corrupt_files),
                "missingFiles": missing_files[:30],
                "corruptFiles": corrupt_files[:30],
                "manifest": {
                    "label": manifest.get("label", batch.upper()),
                    "locked": manifest.get("locked", False),
                    "generatedAt": manifest.get("generatedAt", ""),
                    "description": manifest.get("description", ""),
                    "tags": manifest.get("tags", [])
                }
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_dashboard_level_preview(self, query_params):
        """Loads and returns parsed level data for visual board inspection."""
        try:
            game = safe_id(query_params.get("game", ["blast"])[0], "blast")
            vault = safe_id(query_params.get("vault", ["vault_v1"])[0], "vault_v1")
            archetype = safe_id(query_params.get("archetype", [""])[0], "")
            batch = safe_id(query_params.get("batch", [""])[0], "")
            filename = safe_id(query_params.get("filename", ["l001_clb.json"])[0].replace(".json", ""), "l001_clb") + ".json"

            target_file = (BACKEND_DIR / "games" / game / vault / archetype / batch / filename).resolve()
            if not target_file.exists():
                self.send_json_response({"success": False, "error": f"Level file not found: {filename}"}, status_code=404)
                return

            with open(target_file, "r", encoding="utf-8") as f:
                level_data = json.load(f)

            self.send_json_response({
                "success": True,
                "filename": filename,
                "archetype": archetype,
                "batchId": batch,
                "levelData": level_data
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_game_progression(self, query_params):
        """Resolves active phase, archetype, batch, and DDA seed for the player stage and returns LevelData."""
        try:
            game = safe_id(query_params.get("game", ["blast"])[0], "blast")
            vault = safe_id(query_params.get("vault", ["vault_v1"])[0], "vault_v1")
            stage = int(query_params.get("stage", [1])[0])
            fails = int(query_params.get("fails", [0])[0])
            wins = int(query_params.get("wins", [0])[0])
            journey_id = safe_id(query_params.get("journey", [""])[0], "")
            cohort_id = safe_id(query_params.get("cohort", [""])[0], "")

            game_dir = BACKEND_DIR / "games" / game
            vault_dir = game_dir / vault

            # 1. Fallback config
            config_file = game_dir / "config.json"
            config_data = {}
            if config_file.exists():
                try:
                    with open(config_file, "r", encoding="utf-8") as f:
                        config_data = json.load(f)
                except Exception:
                    pass

            if not journey_id:
                journey_id = config_data.get("defaultJourneyId", "journey_linear")
            if not cohort_id:
                cohort_id = config_data.get("defaultCohortId", "cohort_core")

            # 2. Load Journey and determine active phase
            journey_file = game_dir / "journeys" / f"{journey_id}.json"
            if not journey_file.exists():
                journey_file = game_dir / "journeys" / "journey_linear.json"
                journey_id = "journey_linear"

            phases = []
            if journey_file.exists():
                try:
                    with open(journey_file, "r", encoding="utf-8") as f:
                        jdata = json.load(f)
                        phases = jdata.get("phases", [])
                except Exception:
                    phases = []

            active_phase = None
            for p in phases:
                s_start = p.get("stageStart", 1)
                s_end = p.get("stageEnd", 200)
                if s_start <= stage <= s_end:
                    active_phase = p
                    break

            if not active_phase and phases:
                active_phase = phases[-1] if stage > phases[-1].get("stageEnd", 200) else phases[0]

            phase_name = active_phase.get("phaseName", "Standard Progression") if active_phase else "Standard Progression"
            archetype = active_phase.get("archetype", "arc_linear") if active_phase else "arc_linear"
            batch = active_phase.get("batch", "b01") if active_phase else "b01"
            seed_override = active_phase.get("seedOverride", "") if active_phase else ""

            # 3. Load Cohort rules for DDA
            cohort_file = game_dir / "cohorts" / f"{cohort_id}.json"
            if not cohort_file.exists():
                cohort_file = game_dir / "cohorts" / "cohort_core.json"
                cohort_id = "cohort_core"

            cohort_rules = {}
            default_seed = "clb"
            if cohort_file.exists():
                try:
                    with open(cohort_file, "r", encoding="utf-8") as f:
                        cdata = json.load(f)
                        default_seed = cdata.get("defaultSeed", "clb")
                        cohort_rules = cdata.get("rules", {})
                except Exception:
                    pass

            pity_fails = cohort_rules.get("silentPityFailStreak", 2)
            challenge_wins = cohort_rules.get("winStreakChallenge", 5)
            pity_seed = cohort_rules.get("pitySeed", "frg")
            challenge_seed = cohort_rules.get("challengeSeed", "tgt")

            resolved_seed = default_seed
            seed_reason = "Cohort Baseline"
            is_pity = False
            is_challenge = False

            if fails >= pity_fails:
                resolved_seed = pity_seed
                seed_reason = f"Silent Pity ({fails} losses)"
                is_pity = True
            elif wins >= challenge_wins:
                resolved_seed = challenge_seed
                seed_reason = f"Win-Streak Challenge ({wins} wins)"
                is_challenge = True
            elif seed_override:
                resolved_seed = seed_override
                seed_reason = f"Journey Phase Override ({phase_name})"

            # 4. Resolve level file (cycle 1-200)
            level_num = ((stage - 1) % 200) + 1
            padded_stage = f"{level_num:03d}"
            target_filename = f"l{padded_stage}_{resolved_seed}.json"

            batch_dir = vault_dir / archetype / batch
            level_file = batch_dir / target_filename

            # Fallbacks if specific seed does not exist
            if not level_file.exists():
                fallback_file = batch_dir / f"l{padded_stage}_clb.json"
                if fallback_file.exists():
                    level_file = fallback_file
                else:
                    candidates = list(batch_dir.glob(f"l{padded_stage}_*.json"))
                    if candidates:
                        level_file = candidates[0]

            level_data = None
            if level_file.exists():
                with open(level_file, "r", encoding="utf-8") as f:
                    level_data = json.load(f)

            if not level_data:
                self.send_json_response({
                    "success": False,
                    "error": f"Level {stage} could not be resolved in archetype {archetype}, batch {batch}"
                }, status_code=404)
                return

            self.send_json_response({
                "success": True,
                "game": game,
                "vault": vault,
                "stage": stage,
                "journeyId": journey_id,
                "cohortId": cohort_id,
                "phaseName": phase_name,
                "archetype": archetype,
                "batch": batch,
                "levelId": stage,
                "resolvedSeed": resolved_seed,
                "seedReason": seed_reason,
                "isPityActive": is_pity,
                "isChallengeActive": is_challenge,
                "levelData": level_data
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_dashboard_export_batch_zip(self, query_params):
        """Packages a batch directory into a .zip archive and streams it for download."""
        try:
            game = safe_id(query_params.get("game", ["blast"])[0], "blast")
            vault = safe_id(query_params.get("vault", ["vault_v1"])[0], "vault_v1")
            archetype = safe_id(query_params.get("archetype", [""])[0], "")
            batch = safe_id(query_params.get("batch", [""])[0], "")

            batch_dir = (BACKEND_DIR / "games" / game / vault / archetype / batch).resolve()
            if not batch_dir.exists():
                self.send_json_response({"success": False, "error": "Batch directory not found"}, status_code=404)
                return

            zip_buffer = io.BytesIO()
            with zipfile.ZipFile(zip_buffer, "w", zipfile.ZIP_DEFLATED) as zf:
                for f in sorted(batch_dir.iterdir()):
                    if f.is_file():
                        zf.write(f, arcname=f"{batch}/{f.name}")

            zip_bytes = zip_buffer.getvalue()
            zip_filename = f"{game}_{archetype}_{batch}.zip"

            self.send_response(200)
            self.send_header("Content-Type", "application/zip")
            self.send_header("Content-Disposition", f'attachment; filename="{zip_filename}"')
            self.send_header("Content-Length", str(len(zip_bytes)))
            self.end_headers()
            self.wfile.write(zip_bytes)
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_dashboard_update_batch_metadata(self):
        """Updates label, description, and tags in batch_manifest.json for mutable batches."""
        try:
            payload = self.read_json_body()
            game = safe_id(payload.get("game", "blast"), "blast")
            vault = safe_id(payload.get("vault", "vault_v1"), "vault_v1")
            archetype = safe_id(payload.get("archetype", ""), "")
            batch_id = safe_id(payload.get("batchId", ""), "")
            label = str(payload.get("label", "")).strip()
            description = str(payload.get("description", "")).strip()
            tags = payload.get("tags", [])

            manifest_file = BACKEND_DIR / "games" / game / vault / archetype / batch_id / "batch_manifest.json"
            if not manifest_file.exists():
                self.send_json_response({"success": False, "error": "Manifest not found"}, status_code=404)
                return

            with open(manifest_file, "r", encoding="utf-8") as f:
                manifest_data = json.load(f)

            if manifest_data.get("locked", False):
                self.send_json_response({"success": False, "error": "Cannot edit a locked production batch. Unlock first."}, status_code=400)
                return

            if label:
                manifest_data["label"] = label
            manifest_data["description"] = description
            if isinstance(tags, list):
                manifest_data["tags"] = tags

            with open(manifest_file, "w", encoding="utf-8") as f:
                json.dump(manifest_data, f, indent=2)

            self.send_json_response({
                "success": True,
                "batchId": batch_id,
                "label": manifest_data.get("label"),
                "description": manifest_data.get("description"),
                "tags": manifest_data.get("tags", [])
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_dashboard_clone_batch(self):
        """Clones an existing batch into a new mutable draft batch folder (e.g. b03)."""
        try:
            payload = self.read_json_body()
            game = safe_id(payload.get("game", "blast"), "blast")
            vault = safe_id(payload.get("vault", "vault_v1"), "vault_v1")
            archetype = safe_id(payload.get("archetype", ""), "")
            source_batch_id = safe_id(payload.get("sourceBatchId", ""), "")
            new_label = str(payload.get("newLabel", "")).strip()

            arch_dir = BACKEND_DIR / "games" / game / vault / archetype
            src_dir = arch_dir / source_batch_id
            if not src_dir.exists():
                self.send_json_response({"success": False, "error": "Source batch not found"}, status_code=404)
                return

            # Find next available batch ID
            existing_batches = [
                d.name for d in arch_dir.iterdir()
                if d.is_dir() and re.match(r'^b\d+$', d.name)
            ]
            batch_num = 1
            while f"b{batch_num:02d}" in existing_batches:
                batch_num += 1
            new_batch_id = f"b{batch_num:02d}"

            dest_dir = arch_dir / new_batch_id
            shutil.copytree(src_dir, dest_dir)

            # Update manifest to be unlocked mutable clone
            manifest_file = dest_dir / "batch_manifest.json"
            manifest_data = {}
            if manifest_file.exists():
                with open(manifest_file, "r", encoding="utf-8") as f:
                    manifest_data = json.load(f)
                manifest_data["batchId"] = new_batch_id
                manifest_data["locked"] = False
                manifest_data["clonedFrom"] = source_batch_id
                manifest_data["generatedAt"] = str(date.today())
                if new_label:
                    manifest_data["label"] = new_label
                else:
                    manifest_data["label"] = f"{manifest_data.get('label', source_batch_id.upper())} (Clone)"
                with open(manifest_file, "w", encoding="utf-8") as f:
                    json.dump(manifest_data, f, indent=2)

            self.send_json_response({
                "success": True,
                "newBatchId": new_batch_id,
                "sourceBatchId": source_batch_id,
                "archetype": archetype,
                "label": manifest_data.get("label", new_batch_id.upper())
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_dashboard_export_studio_batch(self):
        """Exports a generated batch from Studio into the next batch folder (e.g. b01, b02)."""
        try:
            payload = self.read_json_body()
            game = safe_id(payload.get("game", "blast"), "blast")
            vault = safe_id(payload.get("vault", "vault_v1"), "vault_v1")
            archetype = safe_id(payload.get("archetype", "arc_sawtooth"), "arc_sawtooth")
            label = str(payload.get("label", "Studio Export")).strip()
            description = str(payload.get("description", "")).strip()
            levels = payload.get("levels", [])

            arch_dir = BACKEND_DIR / "games" / game / vault / archetype
            arch_dir.mkdir(parents=True, exist_ok=True)

            # Determine next batch ID (b01, b02...)
            existing_batches = [
                d.name for d in arch_dir.iterdir()
                if d.is_dir() and re.match(r'^b\d+$', d.name)
            ]
            batch_num = 1
            if existing_batches:
                nums = [int(b[1:]) for b in existing_batches]
                batch_num = max(nums) + 1
            batch_id = f"b{batch_num:02d}"

            batch_dir = arch_dir / batch_id
            batch_dir.mkdir(parents=True, exist_ok=True)

            # Save levels formatted for unity into 3 seeds
            saved_count = 0
            for lvl in levels:
                lvl_id = lvl.get("levelId", 1)
                padded_id = f"{lvl_id:03d}"

                # Seed 1 (frg)
                s1_data = self.format_level_seed(lvl, 1)
                with open(batch_dir / f"l{padded_id}_frg.json", "w", encoding="utf-8") as f:
                    json.dump(s1_data, f, indent=2)

                # Seed 2 (clb)
                s2_data = self.format_level_seed(lvl, 2)
                with open(batch_dir / f"l{padded_id}_clb.json", "w", encoding="utf-8") as f:
                    json.dump(s2_data, f, indent=2)

                # Seed 3 (tgt)
                s3_data = self.format_level_seed(lvl, 3)
                with open(batch_dir / f"l{padded_id}_tgt.json", "w", encoding="utf-8") as f:
                    json.dump(s3_data, f, indent=2)

                saved_count += 1

            # Save batch_manifest.json
            manifest_data = {
                "batchId": batch_id,
                "archetype": archetype,
                "label": label or f"Batch {batch_id.upper()}",
                "description": description or f"Exported batch of {saved_count} levels",
                "generatedAt": str(date.today()),
                "levelCount": saved_count,
                "locked": False
            }
            with open(batch_dir / "batch_manifest.json", "w", encoding="utf-8") as f:
                json.dump(manifest_data, f, indent=2)

            self.send_json_response({
                "success": True,
                "batchId": batch_id,
                "archetype": archetype,
                "savedLevels": saved_count,
                "targetDir": str(batch_dir)
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    @staticmethod
    def format_level_seed(level, seed_index=1):
        seeds = level.get("seeds", [])
        seed = {}
        if seeds and len(seeds) >= seed_index:
            seed = seeds[seed_index - 1]
        elif seeds:
            seed = seeds[0]

        return {
            "levelId": level.get("levelId", 1),
            "tier": level.get("tier", 1),
            "gridWidth": level.get("gridWidth", 7),
            "gridHeight": level.get("gridHeight", 8),
            "gridDepth": level.get("gridDepth", 1),
            "arsenalColumnsCount": level.get("arsenalColumnsCount", 5),
            "winCondition": level.get("winCondition", 0),
            "clearPercentageRequired": level.get("clearPercentageRequired", 100.0),
            "targetChestsRequired": level.get("targetChestsRequired", 1),
            "enemyBlocks": level.get("enemyBlocks", []),
            "arsenalColumns": seed.get("arsenalColumns", level.get("arsenalColumns", []))
        }

    # =========================================================================
    # 💥 LEGACY UNITY ASSETS SYNC APIS
    # =========================================================================

    def get_project_info(self):
        LEVELS_DIR.mkdir(parents=True, exist_ok=True)
        level_files = list(LEVELS_DIR.glob("Level_*.json"))
        has_manifest = (LEVELS_DIR / "tracks_manifest.json").exists()

        return {
            "status": "online",
            "project_name": "This is Blast",
            "levels_dir": str(LEVELS_DIR),
            "level_count": len(level_files),
            "has_manifest": has_manifest
        }

    def handle_save_levels(self):
        try:
            payload = self.read_json_body()
            levels = payload.get("levels", [])
            save_pool = payload.get("save_pool", True)
            LEVELS_DIR.mkdir(parents=True, exist_ok=True)

            saved_count = 0
            for lvl in levels:
                lvl_id = lvl.get("levelId", 1)
                file_path = LEVELS_DIR / f"Level_{lvl_id}.json"
                with open(file_path, "w", encoding="utf-8") as f:
                    json.dump(lvl, f, indent=2)
                saved_count += 1

            if save_pool and levels:
                pool_path = LEVELS_DIR / "levels_pool.json"
                with open(pool_path, "w", encoding="utf-8") as f:
                    json.dump(levels, f, indent=2)

            self.send_json_response({
                "success": True,
                "saved_count": saved_count,
                "target_dir": str(LEVELS_DIR)
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def handle_save_manifest(self):
        try:
            payload = self.read_json_body()
            manifest = payload.get("manifest", {})
            LEVELS_DIR.mkdir(parents=True, exist_ok=True)

            manifest_path = LEVELS_DIR / "tracks_manifest.json"
            with open(manifest_path, "w", encoding="utf-8") as f:
                json.dump(manifest, f, indent=2)

            self.send_json_response({
                "success": True,
                "file_path": str(manifest_path),
                "tracks_count": len(manifest)
            })
        except Exception as e:
            self.send_json_response({"success": False, "error": str(e)}, status_code=500)

    def send_json_response(self, data, status_code=200):
        body = json.dumps(data).encode("utf-8")
        self.send_response(status_code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Access-Control-Allow-Origin", "*")
        self.end_headers()
        self.wfile.write(body)


def run_server():
    global PORT
    while PORT < 8090:
        try:
            server = ThreadingHTTPServer(("127.0.0.1", PORT), StudioRequestHandler)
            break
        except OSError:
            PORT += 1

    url = f"http://localhost:{PORT}"
    print("=" * 60)
    print("🚀 Level Studio & OmniPace Server running at:")
    print(f"   Level Studio: {url}")
    print(f"   Dashboard:    {url}/dashboard")
    print(f"   HTML Play:    {url}/HTML_version/")
    print("=" * 60)

    server.daemon_threads = True
    while True:
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            print("\nStopping server...")
            try:
                server.server_close()
            except Exception:
                pass
            break
        except Exception as e:
            print(f"Server recovered from error: {e}", flush=True)


if __name__ == "__main__":
    run_server()
