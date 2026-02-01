#if UNITY_EDITOR
// Assets/Aramaa/CreateChibi/Editor/Utilities/ChibiLocalization.cs
//
// =====================================================================
// 概要
// =====================================================================
// - CreateChibi Editor 拡張の文字列を JSON から読み込むローカライザです。
// - OS 言語を初期値にしつつ、EditorPrefs で上書きできます。
//
// =====================================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Aramaa.CreateChibi.Editor.Utilities
{
    internal static class ChibiLocalization
    {
        private const string EditorPrefsKey = "Aramaa.CreateChibi.Language";
        private const string LanguageJapanese = "ja";
        private const string LanguageEnglish = "en";

        private static string _currentLanguageCode;
        private static string _loadedLanguageCode;
        private static Dictionary<string, string> _strings;
        private static string _cachedDisplayNamesLanguageCode;
        private static string[] _cachedDisplayNames;

        public static string CurrentLanguageCode
        {
            get
            {
                EnsureLanguageCode();
                return _currentLanguageCode;
            }
        }

        public static void SetLanguage(string languageCode)
        {
            EnsureLanguageCode();
            var normalized = NormalizeLanguage(languageCode);
            if (_currentLanguageCode == normalized)
            {
                return;
            }

            _currentLanguageCode = normalized;
            EditorPrefs.SetString(EditorPrefsKey, _currentLanguageCode);
            LoadStrings();
        }

        public static int GetLanguageIndex()
        {
            return CurrentLanguageCode == LanguageJapanese ? 0 : 1;
        }

        public static string GetLanguageCodeFromIndex(int index)
        {
            return index == 0 ? LanguageJapanese : LanguageEnglish;
        }

        public static string[] GetLanguageDisplayNames()
        {
            EnsureStrings();
            if (_cachedDisplayNames == null || _cachedDisplayNamesLanguageCode != _loadedLanguageCode)
            {
                _cachedDisplayNamesLanguageCode = _loadedLanguageCode;
                _cachedDisplayNames = new[]
                {
                    Get("Language.OptionJapanese"),
                    Get("Language.OptionEnglish")
                };
            }

            return _cachedDisplayNames;
        }

        public static string Get(string key)
        {
            EnsureStrings();
            if (_strings != null && _strings.TryGetValue(key, out var value))
            {
                return value;
            }

            return key;
        }

        public static string Format(string key, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return Get(key);
            }

            return string.Format(Get(key), args);
        }

        private static void EnsureLanguageCode()
        {
            if (!string.IsNullOrEmpty(_currentLanguageCode))
            {
                return;
            }

            var stored = EditorPrefs.GetString(EditorPrefsKey, string.Empty);
            _currentLanguageCode = NormalizeLanguage(string.IsNullOrEmpty(stored) ? GetSystemLanguageCode() : stored);
            EditorPrefs.SetString(EditorPrefsKey, _currentLanguageCode);
        }

        private static void EnsureStrings()
        {
            EnsureLanguageCode();
            if (_strings != null && _loadedLanguageCode == _currentLanguageCode)
            {
                return;
            }

            LoadStrings();
        }

        private static void LoadStrings()
        {
            _loadedLanguageCode = _currentLanguageCode;
            _strings = new Dictionary<string, string>(StringComparer.Ordinal);

            LoadStringsFromLanguage(_currentLanguageCode);
        }

        private static void LoadStringsFromLanguage(string languageCode)
        {
            var jsonPath = Path.Combine(
                Application.dataPath,
                "Aramaa",
                "CreateChibi",
                "Editor",
                "Localization",
                $"strings.{languageCode}.json");

            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"[CreateChibi] Localization file missing: {jsonPath}");
                if (!string.Equals(languageCode, LanguageEnglish, StringComparison.OrdinalIgnoreCase))
                {
                    LoadStringsFromLanguage(LanguageEnglish);
                }
                return;
            }

            var json = File.ReadAllText(jsonPath);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            var data = JsonUtility.FromJson<LocalizationData>(json);
            if (data?.entries == null)
            {
                return;
            }

            foreach (var entry in data.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key))
                {
                    continue;
                }

                _strings[entry.key] = entry.value ?? string.Empty;
            }
        }

        private static string GetSystemLanguageCode()
        {
            return Application.systemLanguage == SystemLanguage.Japanese ? LanguageJapanese : LanguageEnglish;
        }

        private static string NormalizeLanguage(string languageCode)
        {
            return string.Equals(languageCode, LanguageJapanese, StringComparison.OrdinalIgnoreCase)
                ? LanguageJapanese
                : LanguageEnglish;
        }

        [Serializable]
        private sealed class LocalizationData
        {
            public string language;
            public LocalizationEntry[] entries;
        }

        [Serializable]
        private sealed class LocalizationEntry
        {
            public string key;
            public string value;
        }
    }
}
#endif
