#if UNITY_EDITOR
using ArgosLocalization.Demo;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArgosLocalization.Editor
{
    /// <summary>
    /// Builds Assets/Scenes/ArgosDemo.unity: a Canvas with one TMP label per demo string plus language buttons.
    /// Every label is bound to the UI_Strings table so the language buttons switch text live in Play mode.
    /// </summary>
    public static class ArgosDemoSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ArgosDemo.unity";

        private static readonly (string Code, string Label)[] Languages =
        {
            ("en", "English"), ("es", "Español"), ("ja", "日本語"),
            ("hi", "हिन्दी"), ("fr", "Français"), ("de", "Deutsch")
        };

        /// <summary>Headless entry point: unity -batchmode -executeMethod ArgosLocalization.Editor.ArgosDemoSceneBuilder.BuildDemoSceneBatch</summary>
        public static void BuildDemoSceneBatch()
        {
            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                AssetDatabase.Refresh();
            }

            BuildDemoScene();
        }

        [MenuItem("Tools/Argos Localization/0. Build Demo Scene (18 demo strings)", priority = 0)]
        public static void BuildDemoScene()
        {
            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                EditorApplication.delayCall += BuildDemoScene;
                Debug.Log("[Argos Localization] Importing TMP Essential Resources; the demo scene will build when it finishes.");
                return;
            }

            var collection = ArgosTranslateTool.EnsureStringTableCollectionAndLocales(
                ArgosTranslateTool.DefaultTableName, ArgosTranslateTool.DefaultLocaleCodes);
            if (collection == null) return;
            RemoveLanguageButtonRows(collection);
            ArgosTranslateTool.EnsureDemoSeedEntries(collection);

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.12f, 0.16f);

            // Canvas + EventSystem
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var systems = new GameObject("Localization Demo", typeof(LanguageSwitcher), typeof(DemoFontFallback));
            var switcher = systems.GetComponent<LanguageSwitcher>();

            // Language button row
            var buttonRow = CreateRect("Language Buttons", canvasGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(1840, 100));
            var rowLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 16;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            foreach (var (code, label) in Languages)
                CreateLanguageButton(buttonRow, switcher, code, label);

            // Label grid: one TMP_Text per demo string
            var grid = CreateRect("Labels", canvasGo.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(1840, 800));
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(890, 84);
            gridLayout.spacing = new Vector2(60, 8);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            gridLayout.childAlignment = TextAnchor.UpperCenter;

            foreach (var (key, english) in ArgosDemoData.Entries)
            {
                var label = CreateTmpLabel(key, grid, english, 30, TextAlignmentOptions.MidlineLeft, wrap: true);
                ArgosTranslateTool.BindLocalizeStringEvent(label, collection.TableCollectionName, key);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Argos Localization] Demo scene saved to {ScenePath}. " +
                      "Run 'Tools/Argos Localization/1. Harvest ... & Translate All Locales' to fill the other languages, " +
                      "then press Play and click the language buttons.");
        }

        // Earlier builds let the harvester pick up the language-button captions as rows; drop those leftovers.
        private static void RemoveLanguageButtonRows(UnityEditor.Localization.StringTableCollection collection)
        {
            var english = System.Linq.Enumerable.FirstOrDefault(collection.StringTables, t => t != null && t.LocaleIdentifier.Code == "en");
            if (english == null) return;

            var captions = new System.Collections.Generic.HashSet<string>();
            foreach (var (_, caption) in Languages) captions.Add(caption);

            var demoKeys = new System.Collections.Generic.HashSet<string>();
            foreach (var (key, _) in ArgosDemoData.Entries) demoKeys.Add(key);

            var stale = new System.Collections.Generic.List<string>();
            foreach (var entry in collection.SharedData.Entries)
            {
                if (demoKeys.Contains(entry.Key)) continue;
                var cell = english.GetEntry(entry.Id);
                if (cell != null && captions.Contains(cell.Value)) stale.Add(entry.Key);
            }

            foreach (string key in stale)
                collection.RemoveEntry(key);

            if (stale.Count > 0)
                Debug.Log($"[Argos Localization] Removed {stale.Count} stale language-button row(s) from the table.");
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, anchorMin.y);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        private static TextMeshProUGUI CreateTmpLabel(string name, Transform parent, string text, float fontSize,
            TextAlignmentOptions alignment, bool wrap = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        private static void CreateLanguageButton(Transform parent, LanguageSwitcher switcher, string code, string label)
        {
            var go = new GameObject("Button_" + code, typeof(RectTransform), typeof(Image), typeof(Button), typeof(ArgosIgnoreLocalization));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.20f, 0.45f, 0.85f);

            var text = CreateTmpLabel("Label", go.transform, label, 34, TextAlignmentOptions.Center);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;

            UnityEventTools.AddStringPersistentListener(go.GetComponent<Button>().onClick, switcher.SetLocale, code);
        }
    }
}
#endif
