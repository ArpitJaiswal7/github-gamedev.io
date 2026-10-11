# Argos Offline Localization for Unity — AI Agent Execution Playbook

> **Purpose of this file:** Give this single `.md` file to any AI Coding Agent inside a Unity project. The agent will automatically provision Python + **Argos Translate** (`https://www.argosopentech.com/` / `https://github.com/argosopentech/argos-translate`) on **Windows or macOS**, scan the Unity Hierarchy for all `TMP_Text` (`TextMeshProUGUI` / `TextMeshPro`) components, populate the English (`en`) base table in a Unity `StringTableCollection`, execute offline neural machine translation across a C# $\leftrightarrow$ Python JSON `stdin`/`stdout` process bridge, and write every translated string into its corresponding `(Row Key × Target Locale Column)` cell in Unity's Localization Tables.

---

## 0. Architectural Contract & System Boundary

The generated editor tooling must strictly adhere to the following architecture:

1. **Unity Side (C# Editor Tooling):**
   - **Hierarchy TMP Harvester & Idempotent Demo Bootstrap:** Scans all active scene `TMP_Text` components (or creates idempotent demo keys if testing in a blank scene) and registers them in the `SharedTableData` + English (`en`) `StringTable`.
   - **Menu Command & Context Entry:** Exposes both `Tools/Argos Localization/Harvest TMP & Translate All Locales` and a `StringTableCollection` context menu (`CONTEXT/StringTableCollection/Translate with Argos (Offline)...`).
   - **Argos Translate Tool:** Reads source (`en`) entries from the `StringTableCollection`, crosses the OS process boundary via a **dependency-free JSON codec** over `stdin`/`stdout`, runs **sequential per-locale calls**, writes translated values into each target locale `StringTable` cell, marks assets dirty with **version-guarded API usage**, and triggers **Editor window refresh via reflection** so open Unity Localization Table windows immediately display the new translations.
2. **OS Process Boundary (`JSON stdin` $\rightarrow$ Python $\rightarrow$ `JSON stdout`):**
   - **Python Executable Resolution:** Locates the local `.argos_venv` Python binary on both Windows (`.argos_venv\Scripts\python.exe`) and macOS (`.argos_venv/bin/python3`), falling back to system paths (`/opt/homebrew/bin/python3`, `/usr/local/bin/python3`, `%LocalAppData%\Programs\Python\...`).
   - **Python Subprocess (`argos_bridge.py`):** Runs `argostranslate` 100% offline (auto-installing missing `.argosmodel` language packs on first run), translates the passed string array for the requested locale, and outputs UTF-8 JSON to `stdout`.

---

## 1. Phase 1 — Automated OS Environment, Python & Argos SDK Provisioning (Windows & macOS)

**AI Agent Instructions:** Run these terminal steps automatically before touching Unity C# scripts. Do **not** assume Python or Argos Translate is pre-installed on the machine.

### Step 1.1: Detect or Install Python 3 (3.10 – 3.12 recommended for PyTorch/SentencePiece wheels)

#### On Windows (PowerShell):
```powershell
# 1. Check if a working Python 3.10+ exists
$pythonCmd = $null
foreach ($candidate in @("python", "py -3.11", "py -3")) {
    try {
        $ver = Invoke-Expression "$candidate --version 2>&1"
        if ($ver -match "Python 3\.(10|11|12)") { $pythonCmd = $candidate; break }
    } catch {}
}

# 2. If not installed, install Python 3.11 silently via winget
if (-not $pythonCmd) {
    Write-Host "Installing Python 3.11 via winget..."
    winget install -e --id Python.Python.3.11 --scope user --accept-package-agreements --accept-source-agreements
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
    $pythonCmd = "$env:LOCALAPPDATA\Programs\Python\Python311\python.exe"
}
```

#### On macOS (zsh / bash):
```bash
# 1. Check if Python 3.10+ exists
if ! command -v python3 >/dev/null 2>&1 || ! python3 -c 'import sys; exit(0 if sys.version_info >= (3, 10) else 1)'; then
    echo "Installing Python 3.11..."
    if command -v brew >/dev/null 2>&1; then
        brew install python@3.11
    else
        # Download official macOS universal2 pkg if Homebrew is unavailable
        curl -L "https://www.python.org/ftp/python/3.11.9/python-3.11.9-macos11.pkg" -o /tmp/python311.pkg
        sudo installer -pkg /tmp/python311.pkg -target /
    fi
fi
```

### Step 1.2: Create Project-Local Virtual Environment & Install Argos Translate

Create an isolated `.argos_venv` inside the Unity project root (next to `Assets/` and `Packages/`, excluded from Unity import) and install `argostranslate` from `https://github.com/argosopentech/argos-translate`:

#### Windows:
```powershell
& $pythonCmd -m venv .argos_venv
.\.argos_venv\Scripts\python.exe -m pip install --upgrade pip setuptools wheel
.\.argos_venv\Scripts\python.exe -m pip install argostranslate
```

#### macOS:
```bash
python3 -m venv .argos_venv
./.argos_venv/bin/python3 -m pip install --upgrade pip setuptools wheel
./.argos_venv/bin/python3 -m pip install argostranslate
```

### Step 1.3: Pre-Cache Offline Language Packs (`en` $\rightarrow$ `es`, `ja`, `hi`, `fr`, `de`)

Run a one-time bootstrap command using the venv Python so Argos downloads and caches the `.argosmodel` neural translation models for offline use:

```python
import argostranslate.package

TARGET_LOCALES = ["es", "ja", "hi", "fr", "de"]
argostranslate.package.update_package_index()
available = argostranslate.package.get_available_packages()
installed = {(p.from_code, p.to_code) for p in argostranslate.package.get_installed_packages()}

for target in TARGET_LOCALES:
    if ("en", target) not in installed:
        pkg = next((p for p in available if p.from_code == "en" and p.to_code == target), None)
        if pkg:
            print(f"Installing offline Argos model: en -> {target}...")
            argostranslate.package.install_from_path(pkg.download())
```

---

## 2. Phase 2 — Unity Package Verification (`com.unity.localization` & `TMPro`)

**AI Agent Instructions:** Inspect `Packages/manifest.json` in the Unity project. Ensure `com.unity.localization` and `com.unity.ugui` (which includes TextMeshPro) are present in `"dependencies"`:

```json
{
  "dependencies": {
    "com.unity.localization": "1.5.2",
    "com.unity.ugui": "2.0.0"
  }
}
```
*(If the project uses Unity 2022 LTS, `"com.unity.textmeshpro": "3.0.6"` and `"com.unity.localization": "1.4.5"` are also supported.)*

---

## 3. Phase 3 — Create the Python Offline Translation Helper (`Tools/ArgosBridge/argos_bridge.py`)

**AI Agent Instructions:** Create `Tools/ArgosBridge/argos_bridge.py` in the Unity project root (outside `Assets/` so Unity does not compile it, or inside `Assets/Editor/ArgosLocalization/Python~/argos_bridge.py` inside a `~` ignored folder).

This script accepts a JSON payload over `stdin`:
```json
{
  "source_locale": "en",
  "target_locale": "es",
  "texts": ["Play Game", "Settings", "High Score"]
}
```
And outputs a deterministic UTF-8 JSON payload to `stdout`:
```json
{
  "ok": true,
  "source_locale": "en",
  "target_locale": "es",
  "translations": ["Juego de juego", "Ajustes", "Puntuación alta"]
}
```

### Full Code for `Assets/Editor/ArgosLocalization/Python~/argos_bridge.py`:

```python
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Argos Offline Machine Translation Bridge for Unity Localization Tables.
Reads JSON from stdin:  {"source_locale": "en", "target_locale": "es", "texts": [...]}
Writes JSON to stdout:  {"ok": true, "target_locale": "es", "translations": [...]}
"""

import sys
import io
import json
import traceback


def normalize_code(code: str) -> str:
    """Map Unity locale codes like 'es-ES', 'ja-JP', 'hi-IN' to ISO 639-1 codes used by Argos."""
    if not code:
        return "en"
    base = code.strip().replace("_", "-").split("-")[0].lower()
    # Special case for Chinese if needed by Argos
    if base == "zh":
        return "zh"
    return base


def ensure_language_pair(from_code: str, to_code: str) -> None:
    import argostranslate.package

    installed = argostranslate.package.get_installed_packages()
    if any(p.from_code == from_code and p.to_code == to_code for p in installed):
        return

    # Auto-install model if missing
    argostranslate.package.update_package_index()
    available = argostranslate.package.get_available_packages()
    pkg = next((p for p in available if p.from_code == from_code and p.to_code == to_code), None)
    if pkg is None:
        raise RuntimeError(
            f"No Argos Translate package found for '{from_code}' -> '{to_code}'."
        )
    download_path = pkg.download()
    argostranslate.package.install_from_path(download_path)


def main() -> int:
    # Force UTF-8 across stdin/stdout on Windows & macOS
    sys.stdin = io.TextIOWrapper(sys.stdin.buffer, encoding="utf-8")
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", write_through=True)

    try:
        raw_input = sys.stdin.read()
        if not raw_input.strip():
            raise ValueError("Empty JSON input received on stdin.")

        payload = json.loads(raw_input)
        src_code = normalize_code(payload.get("source_locale", "en"))
        tgt_code = normalize_code(payload.get("target_locale", "es"))
        texts = payload.get("texts", [])

        if not isinstance(texts, list):
            raise ValueError("'texts' field must be a JSON array of strings.")

        if src_code == tgt_code or len(texts) == 0:
            out = {
                "ok": True,
                "source_locale": src_code,
                "target_locale": tgt_code,
                "translations": [str(t) for t in texts],
            }
            sys.stdout.write(json.dumps(out, ensure_ascii=False))
            return 0

        import argostranslate.translate

        ensure_language_pair(src_code, tgt_code)

        # Resolve installed translation object once per locale call
        installed_languages = argostranslate.translate.get_installed_languages()
        from_lang = next((l for l in installed_languages if l.code == src_code), None)
        to_lang = next((l for l in installed_languages if l.code == tgt_code), None)

        if from_lang is None or to_lang is None:
            raise RuntimeError(f"Language model not loaded for {src_code} -> {tgt_code}.")

        translator = from_lang.get_translation(to_lang)
        if translator is None:
            raise RuntimeError(f"Translator object unavailable for {src_code} -> {tgt_code}.")

        translations = []
        for item in texts:
            text_str = "" if item is None else str(item)
            if not text_str.strip():
                translations.append(text_str)
            else:
                translations.append(translator.translate(text_str))

        out = {
            "ok": True,
            "source_locale": src_code,
            "target_locale": tgt_code,
            "translations": translations,
        }
        sys.stdout.write(json.dumps(out, ensure_ascii=False))
        return 0

    except Exception as exc:
        err = {
            "ok": False,
            "error": str(exc),
            "traceback": traceback.format_exc(),
        }
        sys.stdout.write(json.dumps(err, ensure_ascii=False))
        return 1


if __name__ == "__main__":
    sys.exit(main())
```

---

## 4. Phase 4 — Create the Unity C# Editor Tool (`Assets/Editor/ArgosLocalization/ArgosTranslateTool.cs`)

**AI Agent Instructions:** Create `Assets/Editor/ArgosLocalization/ArgosTranslateTool.cs`. This single Editor script implements:
1. **Idempotent Demo Bootstrap** (creates LocalizationSettings, Locales `en`, `es`, `ja`, `hi`, `fr`, `de`, a `StringTableCollection` named `UI_Strings`, and demo keys if none exist).
2. **Scene Hierarchy `TMP_Text` Harvester** (finds all `TMP_Text` components in the active scene, generates clean keys, caches their text into the `en` `StringTable`, and optionally attaches/binds `LocalizeStringEvent`).
3. **Python Executable Resolution** (cross-platform Windows & macOS `.argos_venv` + system fallbacks).
4. **Dependency-Free JSON Codec** (minimal pure-C# JSON serializer/parser with full Unicode and escape handling so there is zero dependency on external JSON packages).
5. **Sequential Per-Locale Calls** over `ProcessStartInfo` (`stdin` $\rightarrow$ `stdout`) to translate every row in the English column and write it into the exact `(Key Row × Locale Column)` cell of each target `StringTable`.
6. **Version-Guarded API Usage & Editor Window Refresh via Reflection** so Unity's Localization Tables window immediately shows the newly populated cells without requiring an Editor restart.

### Full Code for `Assets/Editor/ArgosLocalization/ArgosTranslateTool.cs`:

```csharp
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace ArgosLocalization.Editor
{
    /// <summary>
    /// Offline Unity Localization Table translator powered by Argos Translate via a local Python subprocess.
    /// Crosses the OS process boundary using UTF-8 JSON over stdin/stdout.
    /// </summary>
    [InitializeOnLoad]
    public static class ArgosTranslateTool
    {
        private const string DefaultTableName = "UI_Strings";
        private const string LocalizationAssetsRoot = "Assets/Localization";
        private const string LocalesFolder = "Assets/Localization/Locales";
        private const string TablesFolder = "Assets/Localization/Tables";
        private const string BootstrapSessionKey = "ArgosLocalization_DemoBootstrapped_v1";

        // Default target locales to create & populate (English source + multilingual columns)
        private static readonly string[] DefaultLocaleCodes = { "en", "es", "ja", "hi", "fr", "de" };

        // Demo fallback keys if the active scene has no TMP_Text objects yet
        private static readonly (string Key, string EnglishText)[] DemoSeedEntries =
        {
            ("UI_PLAY_GAME", "Play Game"),
            ("UI_SETTINGS", "Settings"),
            ("UI_HIGH_SCORE", "High Score"),
            ("UI_LANGUAGE", "Select Language"),
            ("UI_EXIT_CONFIRM", "Are you sure you want to quit?")
        };

        static ArgosTranslateTool()
        {
            EditorApplication.delayCall += RunIdempotentDemoBootstrapOnStartup;
        }

        // =====================================================================
        // 1. MENU COMMANDS & CONTEXT ACTIONS
        // =====================================================================

        [MenuItem("Tools/Argos Localization/1. Harvest Scene TMP Texts & Translate All Locales", priority = 10)]
        public static void HarvestAndTranslateMenuCommand()
        {
            var collection = EnsureStringTableCollectionAndLocales(DefaultTableName, DefaultLocaleCodes);
            if (collection == null)
            {
                Debug.LogError("[Argos Localization] Failed to create or locate StringTableCollection.");
                return;
            }

            int harvestedCount = HarvestSceneTmpTextsIntoEnglishTable(collection);
            if (harvestedCount == 0)
            {
                EnsureDemoSeedEntries(collection);
            }

            TranslateStringTableCollection(collection);
        }

        [MenuItem("Tools/Argos Localization/2. Harvest Scene TMP Texts Only (Populate English Column)", priority = 11)]
        public static void HarvestOnlyMenuCommand()
        {
            var collection = EnsureStringTableCollectionAndLocales(DefaultTableName, DefaultLocaleCodes);
            if (collection == null) return;

            int count = HarvestSceneTmpTextsIntoEnglishTable(collection);
            if (count == 0)
            {
                EnsureDemoSeedEntries(collection);
            }

            SaveAndRefreshLocalizationWindows(collection);
            Debug.Log($"[Argos Localization] English column updated in '{collection.TableCollectionName}'.");
        }

        [MenuItem("Tools/Argos Localization/3. Translate Existing Table Collection (Offline Argos)", priority = 12)]
        public static void TranslateExistingCollectionMenuCommand()
        {
            var collection = Selection.activeObject as StringTableCollection;
            if (collection == null)
            {
                collection = EnsureStringTableCollectionAndLocales(DefaultTableName, DefaultLocaleCodes);
            }

            if (collection != null)
            {
                TranslateStringTableCollection(collection);
            }
        }

        // Right-click context menu directly on a StringTableCollection asset in Project/Inspector
        [MenuItem("CONTEXT/StringTableCollection/Translate with Argos (Offline)...")]
        private static void TranslateFromContextMenu(MenuCommand command)
        {
            if (command.context is StringTableCollection collection)
            {
                TranslateStringTableCollection(collection);
            }
        }

        // =====================================================================
        // 2. IDEMPOTENT DEMO BOOTSTRAP & TABLE/LOCALE PROVISIONING
        // =====================================================================

        private static void RunIdempotentDemoBootstrapOnStartup()
        {
            if (SessionState.GetBool(BootstrapSessionKey, false))
                return;

            SessionState.SetBool(BootstrapSessionKey, true);

            // Only auto-bootstrap if a StringTableCollection already exists and has 0 entries
            var collections = LocalizationEditorSettings.GetStringTableCollections();
            if (collections != null && collections.Count > 0)
            {
                var first = collections[0];
                if (first != null && first.SharedData != null && first.SharedData.Entries.Count == 0)
                {
                    EnsureDemoSeedEntries(first);
                    SaveAndRefreshLocalizationWindows(first);
                }
            }
        }

        public static StringTableCollection EnsureStringTableCollectionAndLocales(string tableName, string[] localeCodes)
        {
            EnsureFolderExists("Assets", "Localization");
            EnsureFolderExists(LocalizationAssetsRoot, "Locales");
            EnsureFolderExists(LocalizationAssetsRoot, "Tables");

            // Ensure LocalizationSettings asset exists
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                string settingsPath = $"{LocalizationAssetsRoot}/LocalizationSettings.asset";
                settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(settingsPath);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                    AssetDatabase.CreateAsset(settings, settingsPath);
                    AssetDatabase.SaveAssets();
                }
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            // Ensure each Locale exists and is registered
            var ensuredLocales = new List<Locale>();
            foreach (string code in localeCodes)
            {
                var localeId = new LocaleIdentifier(code);
                Locale locale = LocalizationEditorSettings.GetLocale(localeId);
                if (locale == null)
                {
                    string localeAssetPath = $"{LocalesFolder}/{code}.asset";
                    locale = AssetDatabase.LoadAssetAtPath<Locale>(localeAssetPath);
                    if (locale == null)
                    {
                        locale = Locale.CreateLocale(localeId);
                        locale.name = code;
                        AssetDatabase.CreateAsset(locale, localeAssetPath);
                    }
                    LocalizationEditorSettings.AddLocale(locale);
                }
                ensuredLocales.Add(locale);
            }

            // Find or create the StringTableCollection
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    tableName,
                    TablesFolder,
                    ensuredLocales
                );
            }
            else
            {
                // Make sure every requested locale has a table column in the collection
                foreach (Locale locale in ensuredLocales)
                {
                    if (collection.GetTable(locale.Identifier) == null)
                    {
                        collection.AddNewTable(locale.Identifier);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            return collection;
        }

        // =====================================================================
        // 3. SCENE HIERARCHY TMP_TEXT HARVESTER -> ENGLISH COLUMN
        // =====================================================================

        public static int HarvestSceneTmpTextsIntoEnglishTable(StringTableCollection collection)
        {
            var englishTable = GetEnglishTable(collection);
            if (englishTable == null)
            {
                Debug.LogError("[Argos Localization] English ('en') StringTable not found in collection.");
                return 0;
            }

            TMP_Text[] tmpComponents = FindAllSceneTmpTexts();
            if (tmpComponents.Length == 0)
            {
                Debug.Log("[Argos Localization] No TMP_Text found in active hierarchy; falling back to demo seed entries.");
                return 0;
            }

            int addedOrUpdated = 0;
            foreach (TMP_Text tmp in tmpComponents)
            {
                if (tmp == null) continue;
                string rawText = tmp.text != null ? tmp.text.Trim() : string.Empty;
                if (string.IsNullOrEmpty(rawText)) continue;

                string key = BuildDeterministicKey(tmp, rawText);

                // Ensure key exists in SharedTableData
                SharedTableData.SharedTableEntry sharedEntry = collection.SharedData.GetEntry(key)
                    ?? collection.SharedData.AddKey(key);

                // Populate the English ('en') column cell
                StringTableEntry enEntry = englishTable.GetEntry(sharedEntry.Id);
                if (enEntry == null)
                {
                    englishTable.AddEntry(key, rawText);
                }
                else
                {
                    enEntry.Value = rawText;
                }

                // Optionally bind LocalizeStringEvent so switching Locale in Play Mode updates TMP_Text live
                BindLocalizeStringEvent(tmp, collection.TableCollectionName, key);

                addedOrUpdated++;
            }

            MarkAssetDirtyVersionGuarded(collection.SharedData);
            MarkAssetDirtyVersionGuarded(englishTable);
            MarkAssetDirtyVersionGuarded(collection);

            Debug.Log($"[Argos Localization] Harvested {addedOrUpdated} TMP_Text string(s) from hierarchy into English ('en') column.");
            return addedOrUpdated;
        }

        public static void EnsureDemoSeedEntries(StringTableCollection collection)
        {
            var englishTable = GetEnglishTable(collection);
            if (englishTable == null) return;

            foreach (var (key, englishText) in DemoSeedEntries)
            {
                var shared = collection.SharedData.GetEntry(key) ?? collection.SharedData.AddKey(key);
                var entry = englishTable.GetEntry(shared.Id);
                if (entry == null)
                {
                    englishTable.AddEntry(key, englishText);
                }
                else if (string.IsNullOrEmpty(entry.Value))
                {
                    entry.Value = englishText;
                }
            }

            MarkAssetDirtyVersionGuarded(collection.SharedData);
            MarkAssetDirtyVersionGuarded(englishTable);
        }

        private static TMP_Text[] FindAllSceneTmpTexts()
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
#else
            return UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
#endif
        }

        private static string BuildDeterministicKey(TMP_Text tmp, string text)
        {
            // Sanitize GameObject path + short slug so keys are human-readable and stable
            string goName = Regex.Replace(tmp.gameObject.name.ToUpperInvariant(), @"[^A-Z0-9]+", "_").Trim('_');
            string textSlug = Regex.Replace(text.ToUpperInvariant(), @"[^A-Z0-9]+", "_").Trim('_');
            if (textSlug.Length > 24) textSlug = textSlug.Substring(0, 24).TrimEnd('_');
            if (string.IsNullOrEmpty(goName)) goName = "TMP";
            if (string.IsNullOrEmpty(textSlug)) textSlug = "TEXT";
            return $"{goName}_{textSlug}";
        }

        private static void BindLocalizeStringEvent(TMP_Text tmp, string tableName, string entryKey)
        {
            var locEvent = tmp.GetComponent<LocalizeStringEvent>();
            if (locEvent == null)
            {
                locEvent = Undo.AddComponent<LocalizeStringEvent>(tmp.gameObject);
            }
            locEvent.StringReference.SetReference(tableName, entryKey);
            EditorUtility.SetDirty(locEvent);
        }

        // =====================================================================
        // 4. SEQUENTIAL PER-LOCALE CALLS ACROSS C# <-> PYTHON BRIDGE
        // =====================================================================

        public static void TranslateStringTableCollection(StringTableCollection collection)
        {
            if (collection == null || collection.SharedData == null)
            {
                Debug.LogError("[Argos Localization] Invalid StringTableCollection.");
                return;
            }

            StringTable englishTable = GetEnglishTable(collection);
            if (englishTable == null)
            {
                Debug.LogError("[Argos Localization] Could not find an English ('en') source table in collection.");
                return;
            }

            string pythonExe = ResolvePythonExecutable();
            if (string.IsNullOrEmpty(pythonExe))
            {
                Debug.LogError(
                    "[Argos Localization] Could not resolve Python executable. " +
                    "Ensure '.argos_venv' is created in the project root or Python 3.10+ is installed."
                );
                return;
            }

            string bridgeScriptPath = ResolveBridgeScriptPath();
            if (string.IsNullOrEmpty(bridgeScriptPath) || !File.Exists(bridgeScriptPath))
            {
                Debug.LogError($"[Argos Localization] Python bridge script not found at: {bridgeScriptPath}");
                return;
            }

            // Collect ordered keys & source English strings
            var entries = collection.SharedData.Entries;
            var entryIds = new List<long>(entries.Count);
            var entryKeys = new List<string>(entries.Count);
            var sourceTexts = new List<string>(entries.Count);

            foreach (var sharedEntry in entries)
            {
                StringTableEntry enCell = englishTable.GetEntry(sharedEntry.Id);
                string enValue = enCell != null && !string.IsNullOrEmpty(enCell.Value)
                    ? enCell.Value
                    : sharedEntry.Key;

                entryIds.Add(sharedEntry.Id);
                entryKeys.Add(sharedEntry.Key);
                sourceTexts.Add(enValue);
            }

            if (sourceTexts.Count == 0)
            {
                Debug.LogWarning("[Argos Localization] English source table has 0 entries to translate.");
                return;
            }

            // Gather all non-English target StringTables in the collection
            var targetTables = new List<StringTable>();
            foreach (var table in collection.StringTables)
            {
                if (table == null) continue;
                string code = table.LocaleIdentifier.Code;
                if (!code.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    targetTables.Add(table);
                }
            }

            try
            {
                for (int i = 0; i < targetTables.Count; i++)
                {
                    StringTable targetTable = targetTables[i];
                    string localeCode = targetTable.LocaleIdentifier.Code;

                    float progress = (float)i / targetTables.Count;
                    EditorUtility.DisplayProgressBar(
                        "Argos Offline Localization",
                        $"Translating {sourceTexts.Count} strings: en -> {localeCode}...",
                        progress
                    );

                    // Sequential per-locale call across OS process boundary
                    if (TryRunPythonTranslation(
                        pythonExe,
                        bridgeScriptPath,
                        "en",
                        localeCode,
                        sourceTexts,
                        out List<string> translatedTexts,
                        out string errorMsg))
                    {
                        // Write each translated string into its exact (Row Key x Target Locale Column) cell
                        int count = Mathf.Min(entryIds.Count, translatedTexts.Count);
                        for (int row = 0; row < count; row++)
                        {
                            long keyId = entryIds[row];
                            string keyName = entryKeys[row];
                            string translatedValue = translatedTexts[row];

                            StringTableEntry targetCell = targetTable.GetEntry(keyId);
                            if (targetCell == null)
                            {
                                targetTable.AddEntry(keyName, translatedValue);
                            }
                            else
                            {
                                targetCell.Value = translatedValue;
                            }
                        }

                        MarkAssetDirtyVersionGuarded(targetTable);
                        Debug.Log($"[Argos Localization] Populated {count} cell(s) for locale '{localeCode}'.");
                    }
                    else
                    {
                        Debug.LogError($"[Argos Localization] Failed translating locale '{localeCode}': {errorMsg}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            MarkAssetDirtyVersionGuarded(collection.SharedData);
            MarkAssetDirtyVersionGuarded(collection);
            SaveAndRefreshLocalizationWindows(collection);
        }

        private static bool TryRunPythonTranslation(
            string pythonExe,
            string scriptPath,
            string sourceLocale,
            string targetLocale,
            List<string> texts,
            out List<string> translations,
            out string error)
        {
            translations = null;
            error = null;

            string jsonRequest = DependencyFreeJsonCodec.BuildRequestJson(sourceLocale, targetLocale, texts);

            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

            using (var process = new Process { StartInfo = psi })
            {
                var stdoutBuilder = new StringBuilder();
                var stderrBuilder = new StringBuilder();

                process.OutputDataReceived += (_, e) => { if (e.Data != null) stdoutBuilder.AppendLine(e.Data); };
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderrBuilder.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using (var stdinWriter = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false)))
                {
                    stdinWriter.Write(jsonRequest);
                    stdinWriter.Flush();
                }

                // Allow up to 5 minutes on first run in case Argos downloads a language model
                if (!process.WaitForExit(300_000))
                {
                    try { process.Kill(); } catch { }
                    error = "Python subprocess timed out after 300 seconds.";
                    return false;
                }

                string stdout = stdoutBuilder.ToString().Trim();
                string stderr = stderrBuilder.ToString().Trim();

                if (string.IsNullOrEmpty(stdout))
                {
                    error = $"Empty stdout from Python subprocess. ExitCode={process.ExitCode}, Stderr={stderr}";
                    return false;
                }

                return DependencyFreeJsonCodec.TryParseResponseJson(stdout, out translations, out error);
            }
        }

        // =====================================================================
        // 5. PYTHON EXECUTABLE RESOLUTION (WINDOWS & MACOS)
        // =====================================================================

        private static string ResolvePythonExecutable()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

            // 1. Prefer project-local .argos_venv
#if UNITY_EDITOR_WIN
            string venvPython = Path.Combine(projectRoot, ".argos_venv", "Scripts", "python.exe");
#else
            string venvPython = Path.Combine(projectRoot, ".argos_venv", "bin", "python3");
#endif
            if (File.Exists(venvPython))
                return venvPython;

            // 2. Fallback to common system install locations on Windows & macOS
            var candidates = new List<string>();
#if UNITY_EDITOR_WIN
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            candidates.Add(Path.Combine(localAppData, "Programs", "Python", "Python312", "python.exe"));
            candidates.Add(Path.Combine(localAppData, "Programs", "Python", "Python311", "python.exe"));
            candidates.Add(Path.Combine(localAppData, "Programs", "Python", "Python310", "python.exe"));
            candidates.Add("python.exe");
#else
            candidates.Add("/opt/homebrew/bin/python3");
            candidates.Add("/usr/local/bin/python3");
            candidates.Add("/Library/Frameworks/Python.framework/Versions/3.11/bin/python3");
            candidates.Add("/usr/bin/python3");
#endif
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private static string ResolveBridgeScriptPath()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string[] possiblePaths =
            {
                Path.Combine(Application.dataPath, "Editor", "ArgosLocalization", "Python~", "argos_bridge.py"),
                Path.Combine(projectRoot, "Tools", "ArgosBridge", "argos_bridge.py")
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path)) return path;
            }

            return possiblePaths[0];
        }

        // =====================================================================
        // 6. DEPENDENCY-FREE JSON CODEC (NO EXTERNAL PACKAGES REQUIRED)
        // =====================================================================

        private static class DependencyFreeJsonCodec
        {
            public static string BuildRequestJson(string sourceLocale, string targetLocale, List<string> texts)
            {
                var sb = new StringBuilder(256 + texts.Count * 32);
                sb.Append("{\"source_locale\":\"");
                AppendEscaped(sb, sourceLocale);
                sb.Append("\",\"target_locale\":\"");
                AppendEscaped(sb, targetLocale);
                sb.Append("\",\"texts\":[");

                for (int i = 0; i < texts.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"');
                    AppendEscaped(sb, texts[i] ?? string.Empty);
                    sb.Append('"');
                }

                sb.Append("]}");
                return sb.ToString();
            }

            public static bool TryParseResponseJson(string json, out List<string> translations, out string error)
            {
                translations = null;
                error = null;

                // Check "ok": true
                if (Regex.IsMatch(json, "\"ok\"\\s*:\\s*false"))
                {
                    error = ExtractSingleStringField(json, "error") ?? "Unknown error from Python bridge.";
                    return false;
                }

                int arrayStart = FindTranslationsArrayStart(json);
                if (arrayStart < 0)
                {
                    error = $"Could not locate 'translations' array in JSON response: {json}";
                    return false;
                }

                translations = ParseJsonStringArray(json, arrayStart);
                return true;
            }

            private static int FindTranslationsArrayStart(string json)
            {
                Match m = Regex.Match(json, "\"translations\"\\s*:\\s*\\[");
                return m.Success ? m.Index + m.Length - 1 : -1;
            }

            private static List<string> ParseJsonStringArray(string json, int openBracketIndex)
            {
                var list = new List<string>();
                int i = openBracketIndex + 1;
                int n = json.Length;

                while (i < n)
                {
                    while (i < n && (char.IsWhiteSpace(json[i]) || json[i] == ',')) i++;
                    if (i >= n || json[i] == ']') break;

                    if (json[i] == '"')
                    {
                        string parsed = ReadJsonStringLiteral(json, ref i);
                        list.Add(parsed);
                    }
                    else
                    {
                        i++;
                    }
                }

                return list;
            }

            private static string ReadJsonStringLiteral(string json, ref int index)
            {
                // index points at opening '"'
                index++;
                var sb = new StringBuilder();
                int n = json.Length;

                while (index < n)
                {
                    char c = json[index++];
                    if (c == '"')
                        return sb.ToString();

                    if (c == '\\' && index < n)
                    {
                        char esc = json[index++];
                        switch (esc)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (index + 4 <= n)
                                {
                                    string hex = json.Substring(index, 4);
                                    if (ushort.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out ushort codeUnit))
                                    {
                                        sb.Append((char)codeUnit);
                                    }
                                    index += 4;
                                }
                                break;
                            default:
                                sb.Append(esc);
                                break;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                return sb.ToString();
            }

            private static string ExtractSingleStringField(string json, string fieldName)
            {
                Match m = Regex.Match(json, $"\"{Regex.Escape(fieldName)}\"\\s*:\\s*\"");
                if (!m.Success) return null;
                int quoteIndex = m.Index + m.Length - 1;
                return ReadJsonStringLiteral(json, ref quoteIndex);
            }

            private static void AppendEscaped(StringBuilder sb, string value)
            {
                if (string.IsNullOrEmpty(value)) return;
                foreach (char c in value)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < 0x20)
                                sb.AppendFormat("\\u{0:x4}", (int)c);
                            else
                                sb.Append(c);
                            break;
                    }
                }
            }
        }

        // =====================================================================
        // 7. VERSION-GUARDED API USAGE & EDITOR WINDOW REFRESH VIA REFLECTION
        // =====================================================================

        private static StringTable GetEnglishTable(StringTableCollection collection)
        {
            foreach (var table in collection.StringTables)
            {
                if (table != null && table.LocaleIdentifier.Code.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    return table;
                }
            }
            return null;
        }

        private static void MarkAssetDirtyVersionGuarded(UnityEngine.Object asset)
        {
            if (asset == null) return;
            EditorUtility.SetDirty(asset);
#if UNITY_2021_2_OR_NEWER
            AssetDatabase.SaveAssetIfDirty(asset);
#endif
        }

        private static void SaveAndRefreshLocalizationWindows(StringTableCollection collection)
        {
            AssetDatabase.SaveAssets();
            if (!Application.isBatchMode)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            // Refresh open Localization Tables EditorWindow via reflection so updated cells appear immediately
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null) continue;
                Type winType = window.GetType();
                if (winType.FullName != null && winType.FullName.Contains("LocalizationTablesWindow"))
                {
                    MethodInfo reloadMethod = winType.GetMethod(
                        "ReloadTables",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                    reloadMethod?.Invoke(window, null);
                }
                window.Repaint();
            }
        }

        private static void EnsureFolderExists(string parentFolder, string newFolderName)
        {
            string combined = $"{parentFolder}/{newFolderName}";
            if (!AssetDatabase.IsValidFolder(combined))
            {
                AssetDatabase.CreateFolder(parentFolder, newFolderName);
            }
        }
    }
}
#endif
```

---

## 5. Phase 5 — Execution & Verification Checklist for the AI Agent

When you (the AI Agent) execute this `.md` file inside a Unity project, perform these steps in order:

1. **Provision Python & Argos Translate:**
   - Run the Phase 1 commands for the host OS (Windows or macOS) to create `.argos_venv` and install `argostranslate` plus the offline language models (`en` $\rightarrow$ `es`, `ja`, `hi`, `fr`, `de`).
   - Add `.argos_venv/` to `.gitignore` if a `.gitignore` exists.
2. **Verify Unity Packages:**
   - Check `Packages/manifest.json` and ensure `com.unity.localization` and `com.unity.ugui` / `com.unity.textmeshpro` are installed.
3. **Write Bridge & Editor Scripts:**
   - Write `Assets/Editor/ArgosLocalization/Python~/argos_bridge.py` (Note: the trailing `~` on `Python~` tells Unity's AssetDatabase to ignore `.py` files and any `__pycache__` inside it).
   - Write `Assets/Editor/ArgosLocalization/ArgosTranslateTool.cs`.
4. **Smoke-Test the Python Bridge from CLI:**
   - Run a quick terminal check to verify the offline translation bridge before triggering Unity:
     ```bash
     echo '{"source_locale":"en","target_locale":"es","texts":["Play Game","Settings","High Score"]}' | ./.argos_venv/bin/python3 Assets/Editor/ArgosLocalization/Python~/argos_bridge.py
     ```
     *(On Windows PowerShell, pipe into `.\.argos_venv\Scripts\python.exe Assets\Editor\ArgosLocalization\Python~\argos_bridge.py`)*
   - Confirm it returns `"ok": true` with the translated Spanish strings.
5. **Run in Unity Editor:**
   - In Unity, click **`Tools -> Argos Localization -> 1. Harvest Scene TMP Texts & Translate All Locales`** (or right-click any `StringTableCollection` and choose **`Translate with Argos (Offline)...`**).
   - Open **`Window -> Asset Management -> Localization Tables`** and verify that:
     - Every `TMP_Text` from the Hierarchy is listed as a Row Key with its original text in the **English (`en`)** column.
     - Each target locale column (**Spanish (`es`)**, **Japanese (`ja`)**, **Hindi (`hi`)**, **French (`fr`)**, **German (`de`)**) has its corresponding `(Row × Column)` cell populated with the offline Argos translation.
