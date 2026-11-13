using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ArgosLocalization.Demo
{
    /// <summary>
    /// Minimal runtime language switcher for the Argos demo scene. Hook SetLocale(code) to buttons,
    /// or call Next() to cycle through every locale in the Localization Settings.
    /// </summary>
    public class LanguageSwitcher : MonoBehaviour
    {
        public void SetLocale(string code)
        {
            StartCoroutine(SetLocaleRoutine(code));
        }

        public void Next()
        {
            StartCoroutine(NextRoutine());
        }

        private static IEnumerator SetLocaleRoutine(string code)
        {
            yield return LocalizationSettings.InitializationOperation;

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
            if (locale == null)
            {
                Debug.LogWarning($"[LanguageSwitcher] Locale '{code}' is not in the Localization Settings.");
                yield break;
            }

            LocalizationSettings.SelectedLocale = locale;
        }

        private static IEnumerator NextRoutine()
        {
            yield return LocalizationSettings.InitializationOperation;

            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (locales.Count == 0) yield break;

            int current = locales.IndexOf(LocalizationSettings.SelectedLocale);
            LocalizationSettings.SelectedLocale = locales[(current + 1) % locales.Count];
        }
    }
}
