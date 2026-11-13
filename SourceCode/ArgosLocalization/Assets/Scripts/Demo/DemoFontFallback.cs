using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ArgosLocalization.Demo
{
    /// <summary>
    /// Demo-only helper: TextMeshPro's default font has no Devanagari or Japanese glyphs, so the Hindi and
    /// Japanese columns would render as empty boxes. This builds dynamic fallback fonts from fonts installed
    /// on the OS and appends them to the default font's fallback list at startup.
    /// For a shipping game, import real fonts (e.g. Noto Sans JP / Noto Sans Devanagari) and set up
    /// fallback TMP_FontAssets instead of relying on OS fonts.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class DemoFontFallback : MonoBehaviour
    {
        private static readonly string[] OsFontFamilies =
        {
            // Windows
            "Nirmala UI", "Yu Gothic", "Meiryo", "MS Gothic",
            // macOS
            "Kohinoor Devanagari", "Devanagari Sangam MN", "Hiragino Sans", "Hiragino Kaku Gothic ProN",
            // Linux / generic
            "Noto Sans CJK JP", "Noto Sans Devanagari", "Arial Unicode MS"
        };

        private static bool s_Applied;
        private static readonly List<TMP_FontAsset> s_AddedFallbacks = new List<TMP_FontAsset>();

        private void Awake()
        {
            if (s_Applied) return;
            s_Applied = true;

            TMP_FontAsset baseFont = TMP_Settings.defaultFontAsset;
            if (baseFont == null)
            {
                Debug.LogWarning("[DemoFontFallback] No default TMP font; import TMP Essential Resources first.");
                return;
            }

            if (baseFont.fallbackFontAssetTable == null)
                baseFont.fallbackFontAssetTable = new List<TMP_FontAsset>();

            int added = 0;
            foreach (string family in OsFontFamilies)
            {
                TMP_FontAsset fallback = TMP_FontAsset.CreateFontAsset(family, "Regular");
                if (fallback == null) continue;

                fallback.name = family + " (OS fallback)";
                baseFont.fallbackFontAssetTable.Add(fallback);
                s_AddedFallbacks.Add(fallback);
                added++;
            }

            Debug.Log($"[DemoFontFallback] Added {added} OS fallback font(s) for Devanagari/Japanese glyphs.");
        }

        private void OnDestroy()
        {
            // Undo the runtime-only changes so the font asset never serializes references to in-memory fonts,
            // and so the next Play session (domain reload may be disabled) starts clean.
            TMP_FontAsset baseFont = TMP_Settings.defaultFontAsset;
            if (baseFont != null && baseFont.fallbackFontAssetTable != null)
            {
                foreach (TMP_FontAsset added in s_AddedFallbacks)
                    baseFont.fallbackFontAssetTable.Remove(added);
            }

            s_AddedFallbacks.Clear();
            s_Applied = false;
        }
    }
}
