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
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
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
        public const string DefaultTableName = "UI_Strings";
        private const string LocalizationAssetsRoot = "Assets/Localization";
        private const string LocalesFolder = "Assets/Localization/Locales";
        private const string TablesFolder = "Assets/Localization/Tables";
        private const string BootstrapSessionKey = "ArgosLocalization_DemoBootstrapped_v1";

        // English source + target columns
        public static readonly string[] DefaultLocaleCodes = { "en", "es", "ja", "hi", "fr", "de" };

        static ArgosTranslateTool()
        {
            EditorApplication.delayCall += RunIdempotentDemoBootstrapOnStartup;
        }

        // =====================================================================
        // 1. MENU COMMANDS & CONTEXT ACTIONS
        // =====================================================================

        [MenuItem("Tools/Argos Localization/1. Harvest Scene TMP Texts && Translate All Locales", priority = 10)]
        public static void HarvestAndTranslateMenuCommand()
        {
            var collection = EnsureStringTableCollectionAndLocales(DefaultTableName, DefaultLocaleCodes);
            if (collection == null)
            {
                Debug.LogError("[Argos Localization] Failed to create or locate StringTableCollection.");
                return;
            }

            if (HarvestSceneTmpTextsIntoEnglishTable(collection) == 0)
                EnsureDemoSeedEntries(collection);

            TranslateStringTableCollection(collection);
        }

        [MenuItem("Tools/Argos Localization/2. Harvest Scene TMP Texts Only (Populate English Column)", priority = 11)]
        public static void HarvestOnlyMenuCommand()
        {
            var collection = EnsureStringTableCollectionAndLocales(DefaultTableName, DefaultLocaleCodes);
            if (collection == null) return;

            if (HarvestSceneTmpTextsIntoEnglishTable(collection) == 0)
                EnsureDemoSeedEntries(collection);

            SaveAndRefreshLocalizationWindows(collection);
            Debug.Log($"[Argos Localization] English column updated in '{collection.TableCollectionName}'.");
        }

        [MenuItem("Tools/Argos Localization/3. Translate Existing Table Collection (Offline Argos)", priority = 12)]
        public static void TranslateExistingCollectionMenuCommand()
        {
            var collection = Selection.activeObject as StringTableCollection
                             ?? EnsureStringTableCollectionAndLocales(DefaultTableName, DefaultLocaleCodes);
            if (collection != null)
                TranslateStringTableCollection(collection);
        }

        // Toggle: store non-English cells as "translation (English)", e.g. "Juego (Play Game)"
        private const string AppendEnglishPrefKey = "ArgosLocalization_AppendEnglish";
        private const string AppendEnglishMenu = "Tools/Argos Localization/Append English in (brackets) after translation";

        private static bool AppendEnglish
        {
            get => EditorPrefs.GetBool(AppendEnglishPrefKey, true);
            set => EditorPrefs.SetBool(AppendEnglishPrefKey, value);
        }

        [MenuItem(AppendEnglishMenu, priority = 30)]
        private static void ToggleAppendEnglish() => AppendEnglish = !AppendEnglish;

        [MenuItem(AppendEnglishMenu, validate = true)]
        private static bool ToggleAppendEnglishValidate()
        {
            Menu.SetChecked(AppendEnglishMenu, AppendEnglish);
            return true;
        }

        private static string FormatWithEnglish(string translated, string english)
        {
            if (!AppendEnglish || string.IsNullOrWhiteSpace(english)) return translated;
            // Skip when the model returned the source unchanged ("Menu" -> "Menu"), to avoid "Menu (Menu)"
            if (string.Equals(translated?.Trim(), english.Trim(), StringComparison.OrdinalIgnoreCase)) return translated;
            return $"{translated} ({english})";
        }

        // Right-click context menu on a StringTableCollection asset in the Inspector
        [MenuItem("CONTEXT/StringTableCollection/Translate with Argos (Offline)...")]
        private static void TranslateFromContextMenu(MenuCommand command)
        {
            if (command.context is StringTableCollection collection)
                TranslateStringTableCollection(collection);
        }

        // =====================================================================
        // 2. IDEMPOTENT DEMO BOOTSTRAP & TABLE/LOCALE PROVISIONING
        // =====================================================================

        private static void RunIdempotentDemoBootstrapOnStartup()
        {
            if (SessionState.GetBool(BootstrapSessionKey, false))
                return;
            SessionState.SetBool(BootstrapSessionKey, true);

            // Only auto-fill when a collection already exists and is empty; never create assets unprompted.
            var collections = LocalizationEditorSettings.GetStringTableCollections();
            if (collections == null || collections.Count == 0) return;

            var first = collections[0];
            if (first != null && first.SharedData != null && first.SharedData.Entries.Count == 0)
            {
                EnsureDemoSeedEntries(first);
                SaveAndRefreshLocalizationWindows(first);
            }
        }

        public static StringTableCollection EnsureStringTableCollectionAndLocales(string tableName, string[] localeCodes)
        {
            EnsureFolderExists("Assets", "Localization");
            EnsureFolderExists(LocalizationAssetsRoot, "Locales");
            EnsureFolderExists(LocalizationAssetsRoot, "Tables");

            // Ensure a LocalizationSettings asset exists and is active
            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
            {
                string settingsPath = $"{LocalizationAssetsRoot}/Localization Settings.asset";
                var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(settingsPath);
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
                collection = LocalizationEditorSettings.CreateStringTableCollection(tableName, TablesFolder, ensuredLocales);
            }
            else
            {
                foreach (Locale locale in ensuredLocales)
                {
                    if (collection.GetTable(locale.Identifier) == null)
                        collection.AddNewTable(locale.Identifier);
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
                Debug.Log("[Argos Localization] No TMP_Text found in the open scene; falling back to demo seed entries.");
                return 0;
            }

            int harvested = 0;
            foreach (TMP_Text tmp in tmpComponents)
            {
                if (tmp == null) continue;
                if (tmp.GetComponentInParent<ArgosIgnoreLocalization>(true) != null) continue;
                string rawText = tmp.text != null ? tmp.text.Trim() : string.Empty;
                if (string.IsNullOrEmpty(rawText)) continue;

                // Re-running the harvest must not mint a second key for an already-bound label.
                string key = TryGetBoundKey(tmp, collection.TableCollectionName) ?? BuildDeterministicKey(tmp, rawText);

                var sharedEntry = collection.SharedData.GetEntry(key) ?? collection.SharedData.AddKey(key);

                // The scene text is the source of truth only for new rows; once bound the table owns the text.
                StringTableEntry enEntry = englishTable.GetEntry(sharedEntry.Id);
                if (enEntry == null)
                    englishTable.AddEntry(key, rawText);
                else if (string.IsNullOrEmpty(enEntry.Value))
                    enEntry.Value = rawText;

                BindLocalizeStringEvent(tmp, collection.TableCollectionName, key);
                harvested++;
            }

            MarkAssetDirtyVersionGuarded(collection.SharedData);
            MarkAssetDirtyVersionGuarded(englishTable);
            MarkAssetDirtyVersionGuarded(collection);

            Debug.Log($"[Argos Localization] Harvested {harvested} TMP_Text string(s) from the hierarchy into the English ('en') column.");
            return harvested;
        }

        public static void EnsureDemoSeedEntries(StringTableCollection collection)
        {
            var englishTable = GetEnglishTable(collection);
            if (englishTable == null) return;

            foreach (var (key, englishText) in ArgosDemoData.Entries)
            {
                var shared = collection.SharedData.GetEntry(key) ?? collection.SharedData.AddKey(key);
                var entry = englishTable.GetEntry(shared.Id);
                if (entry == null)
                    englishTable.AddEntry(key, englishText);
                else if (string.IsNullOrEmpty(entry.Value))
                    entry.Value = englishText;
            }

            MarkAssetDirtyVersionGuarded(collection.SharedData);
            MarkAssetDirtyVersionGuarded(englishTable);
            Debug.Log($"[Argos Localization] Demo seed: {ArgosDemoData.Entries.Length} English strings ensured in '{collection.TableCollectionName}'.");
        }

        private static TMP_Text[] FindAllSceneTmpTexts()
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            return UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
#endif
        }

        private static string BuildDeterministicKey(TMP_Text tmp, string text)
        {
            string goName = Regex.Replace(tmp.gameObject.name.ToUpperInvariant(), @"[^A-Z0-9]+", "_").Trim('_');
            string textSlug = Regex.Replace(text.ToUpperInvariant(), @"[^A-Z0-9]+", "_").Trim('_');
            if (textSlug.Length > 24) textSlug = textSlug.Substring(0, 24).TrimEnd('_');
            if (string.IsNullOrEmpty(goName)) goName = "TMP";
            if (string.IsNullOrEmpty(textSlug)) textSlug = "TEXT";
            return $"{goName}_{textSlug}";
        }

        private static string TryGetBoundKey(TMP_Text tmp, string tableName)
        {
            var locEvent = tmp.GetComponent<LocalizeStringEvent>();
            if (locEvent == null || locEvent.StringReference.IsEmpty) return null;
            if (locEvent.StringReference.TableReference.TableCollectionName != tableName) return null;
            string key = locEvent.StringReference.TableEntryReference.Key;
            return string.IsNullOrEmpty(key) ? null : key;
        }

        /// <summary>
        /// Adds a LocalizeStringEvent to the label, points it at the table row and wires its OnUpdateString to
        /// TMP_Text.text, so changing the selected Locale updates the label at runtime.
        /// </summary>
        public static void BindLocalizeStringEvent(TMP_Text tmp, string tableName, string entryKey)
        {
            var locEvent = tmp.GetComponent<LocalizeStringEvent>();
            if (locEvent == null)
                locEvent = Undo.AddComponent<LocalizeStringEvent>(tmp.gameObject);

            locEvent.StringReference.SetReference(tableName, entryKey);

            bool alreadyWired = false;
            for (int i = 0; i < locEvent.OnUpdateString.GetPersistentEventCount(); i++)
            {
                if (locEvent.OnUpdateString.GetPersistentTarget(i) == tmp &&
                    locEvent.OnUpdateString.GetPersistentMethodName(i) == "set_text")
                {
                    alreadyWired = true;
                    break;
                }
            }

            if (!alreadyWired)
            {
                MethodInfo setter = typeof(TMP_Text).GetProperty("text").GetSetMethod();
                var action = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), tmp, setter);
                UnityEventTools.AddPersistentListener(locEvent.OnUpdateString, action);
            }

            EditorUtility.SetDirty(locEvent);
            EditorSceneManager.MarkSceneDirty(tmp.gameObject.scene);
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
                Debug.LogError("[Argos Localization] Could not resolve a Python executable. " +
                               "Create '.argos_venv' in the project root (see README_ARGOS.md) or install Python 3.10+.");
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
                string enValue = enCell != null && !string.IsNullOrEmpty(enCell.Value) ? enCell.Value : sharedEntry.Key;

                entryIds.Add(sharedEntry.Id);
                entryKeys.Add(sharedEntry.Key);
                sourceTexts.Add(enValue);
            }

            if (sourceTexts.Count == 0)
            {
                Debug.LogWarning("[Argos Localization] English source table has 0 entries to translate.");
                return;
            }

            var targetTables = new List<StringTable>();
            foreach (var table in collection.StringTables)
            {
                if (table == null) continue;
                if (!table.LocaleIdentifier.Code.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                    targetTables.Add(table);
            }

            try
            {
                for (int i = 0; i < targetTables.Count; i++)
                {
                    StringTable targetTable = targetTables[i];
                    string localeCode = targetTable.LocaleIdentifier.Code;

                    EditorUtility.DisplayProgressBar(
                        "Argos Offline Localization",
                        $"Translating {sourceTexts.Count} strings: en -> {localeCode}...",
                        (float)i / targetTables.Count);

                    // Sequential per-locale call across the OS process boundary
                    if (TryRunPythonTranslation(pythonExe, bridgeScriptPath, "en", localeCode, sourceTexts,
                            out List<string> translatedTexts, out string errorMsg))
                    {
                        // Write each translated string into its exact (Row Key x Target Locale Column) cell
                        int count = Mathf.Min(entryIds.Count, translatedTexts.Count);
                        for (int row = 0; row < count; row++)
                        {
                            string cellValue = FormatWithEnglish(translatedTexts[row], sourceTexts[row]);
                            StringTableEntry targetCell = targetTable.GetEntry(entryIds[row]);
                            if (targetCell == null)
                                targetTable.AddEntry(entryKeys[row], cellValue);
                            else
                                targetCell.Value = cellValue;
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

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    error = $"Could not start '{pythonExe}': {ex.Message}";
                    return false;
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using (var stdinWriter = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false)))
                {
                    stdinWriter.Write(jsonRequest);
                    stdinWriter.Flush();
                }

                // Up to 5 minutes: the first run may download a language model
                if (!process.WaitForExit(300_000))
                {
                    try { process.Kill(); } catch { }
                    error = "Python subprocess timed out after 300 seconds.";
                    return false;
                }
                process.WaitForExit(); // flush async stdout/stderr handlers

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

#if UNITY_EDITOR_WIN
            string venvPython = Path.Combine(projectRoot, ".argos_venv", "Scripts", "python.exe");
#else
            string venvPython = Path.Combine(projectRoot, ".argos_venv", "bin", "python3");
#endif
            if (File.Exists(venvPython))
                return venvPython;

            var candidates = new List<string>();
#if UNITY_EDITOR_WIN
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach (string v in new[] { "Python312", "Python311", "Python310" })
                candidates.Add(Path.Combine(localAppData, "Programs", "Python", v, "python.exe"));
            candidates.Add(@"C:\Python312\python.exe");
            candidates.Add(@"C:\Python311\python.exe");
            candidates.Add(@"C:\Python313\python.exe");
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

                if (Regex.IsMatch(json, "\"ok\"\\s*:\\s*false"))
                {
                    error = ExtractSingleStringField(json, "error") ?? "Unknown error from Python bridge.";
                    return false;
                }

                Match m = Regex.Match(json, "\"translations\"\\s*:\\s*\\[");
                if (!m.Success)
                {
                    error = $"Could not locate 'translations' array in JSON response: {json}";
                    return false;
                }

                translations = ParseJsonStringArray(json, m.Index + m.Length - 1);
                return true;
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
                        list.Add(ReadJsonStringLiteral(json, ref i));
                    else
                        i++;
                }

                return list;
            }

            private static string ReadJsonStringLiteral(string json, ref int index)
            {
                index++; // skip opening quote
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
                                        sb.Append((char)codeUnit);
                                    index += 4;
                                }
                                break;
                            default: sb.Append(esc); break;
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
                            if (c < 0x20) sb.AppendFormat("\\u{0:x4}", (int)c);
                            else sb.Append(c);
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
                    return table;
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

            // Refresh open Localization Tables windows via reflection so updated cells appear immediately
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null) continue;
                Type winType = window.GetType();
                if (winType.FullName != null && winType.FullName.Contains("LocalizationTablesWindow"))
                {
                    MethodInfo reloadMethod = winType.GetMethod(
                        "ReloadTables",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    reloadMethod?.Invoke(window, null);
                }
                window.Repaint();
            }
        }

        private static void EnsureFolderExists(string parentFolder, string newFolderName)
        {
            if (!AssetDatabase.IsValidFolder($"{parentFolder}/{newFolderName}"))
                AssetDatabase.CreateFolder(parentFolder, newFolderName);
        }
    }
}
#endif
