#if UNITY_EDITOR
// Assets/Aramaa/OchibiChansConverterTool/Editor/Utilities/OchibiChansConverterToolVersionUtility.cs
//
// ============================================================================
// 概要
// ============================================================================
// - ツールのバージョン比較を行い、最新かどうかを判定します。
// - SemVer 風の文字列（例: 1.2.3 / 1.2.3-beta）を安全に比較します。
//
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Aramaa.OchibiChansConverterTool.Editor.Utilities
{
    internal enum OchibiChansConverterToolVersionStatus
    {
        Unknown,
        UpToDate,
        UpdateAvailable,
        Ahead
    }

    internal static class OchibiChansConverterToolVersionUtility
    {
        public static void FetchLatestVersionAsync(string url, Action<OchibiChansConverterToolVersionFetchResult> onComplete)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                onComplete?.Invoke(OchibiChansConverterToolVersionFetchResult.Failure(OchibiChansConverterToolLocalization.Get("Version.ErrorMissingUrl")));
                return;
            }

            var requestUrl = AppendCacheBuster(url);
            var request = UnityWebRequest.Get(requestUrl);
            request.timeout = 10;

            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        onComplete?.Invoke(OchibiChansConverterToolVersionFetchResult.Failure(request.error));
                        return;
                    }

                    var latestVersion = ExtractVersionFromResponse(request.downloadHandler.text);
                    if (string.IsNullOrWhiteSpace(latestVersion))
                    {
                        onComplete?.Invoke(OchibiChansConverterToolVersionFetchResult.Failure(OchibiChansConverterToolLocalization.Get("Version.ExtractFailed")));
                        return;
                    }

                    onComplete?.Invoke(OchibiChansConverterToolVersionFetchResult.Success(latestVersion));
                }
                finally
                {
                    request.Dispose();
                }
            };
        }

        public static OchibiChansConverterToolVersionStatus GetVersionStatus(string currentVersion, string latestVersion)
        {
            if (string.IsNullOrWhiteSpace(currentVersion) || string.IsNullOrWhiteSpace(latestVersion))
            {
                return OchibiChansConverterToolVersionStatus.Unknown;
            }

            var comparison = CompareVersions(currentVersion, latestVersion);
            if (comparison == 0)
            {
                return OchibiChansConverterToolVersionStatus.UpToDate;
            }

            return comparison < 0 ? OchibiChansConverterToolVersionStatus.UpdateAvailable : OchibiChansConverterToolVersionStatus.Ahead;
        }

        private static int CompareVersions(string currentVersion, string latestVersion)
        {
            var current = ParseVersion(currentVersion);
            var latest = ParseVersion(latestVersion);

            var max = Math.Max(current.Numbers.Count, latest.Numbers.Count);
            for (var i = 0; i < max; i++)
            {
                var currentValue = i < current.Numbers.Count ? current.Numbers[i] : 0;
                var latestValue = i < latest.Numbers.Count ? latest.Numbers[i] : 0;

                if (currentValue != latestValue)
                {
                    return currentValue.CompareTo(latestValue);
                }
            }

            if (current.HasSuffix == latest.HasSuffix)
            {
                return 0;
            }

            // 数値部分が同じなら「プレリリース付き」を旧版扱い
            return current.HasSuffix ? -1 : 1;
        }

        private static VersionParts ParseVersion(string version)
        {
            var trimmed = version.Trim();
            var parts = trimmed.Split('.');
            var numbers = new List<int>(parts.Length);
            var hasSuffix = false;

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    numbers.Add(0);
                    continue;
                }

                var digitCount = 0;
                while (digitCount < part.Length && char.IsDigit(part[digitCount]))
                {
                    digitCount++;
                }

                if (digitCount == 0)
                {
                    numbers.Add(0);
                    hasSuffix = true;
                    continue;
                }

                if (digitCount < part.Length)
                {
                    hasSuffix = true;
                }

                if (!int.TryParse(part.Substring(0, digitCount), out var value))
                {
                    value = 0;
                    hasSuffix = true;
                }

                numbers.Add(value);
            }

            for (var i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];
                if (!(char.IsDigit(c) || c == '.'))
                {
                    hasSuffix = true;
                    break;
                }
            }

            return new VersionParts(numbers, hasSuffix);
        }

        private static string ExtractVersionFromResponse(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return null;
            }

            var trimmed = responseText.Trim();
            if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                try
                {
                    var payload = JsonUtility.FromJson<VersionPayload>(trimmed);
                    if (!string.IsNullOrWhiteSpace(payload?.version))
                    {
                        return payload.version.Trim();
                    }

                    return null;
                }
                catch (Exception)
                {
                    // JSON として解釈できない場合はプレーンテキスト扱いにフォールバック
                }
            }

            return trimmed;
        }

        private static string AppendCacheBuster(string url)
        {
            var separator = url.Contains("?") ? "&" : "?";
            var cacheBuster = DateTime.UtcNow.Ticks;
            return $"{url}{separator}t={cacheBuster}";
        }

        private readonly struct VersionParts
        {
            public VersionParts(List<int> numbers, bool hasSuffix)
            {
                Numbers = numbers;
                HasSuffix = hasSuffix;
            }

            public List<int> Numbers { get; }
            public bool HasSuffix { get; }
        }

        [Serializable]
        private sealed class VersionPayload
        {
            public string version;
        }
    }

    internal readonly struct OchibiChansConverterToolVersionFetchResult
    {
        private OchibiChansConverterToolVersionFetchResult(string latestVersion, string error)
        {
            LatestVersion = latestVersion;
            Error = error;
        }

        public string LatestVersion { get; }
        public string Error { get; }
        public bool Succeeded => string.IsNullOrWhiteSpace(Error);

        public static OchibiChansConverterToolVersionFetchResult Success(string latestVersion)
        {
            return new OchibiChansConverterToolVersionFetchResult(latestVersion, null);
        }

        public static OchibiChansConverterToolVersionFetchResult Failure(string error)
        {
            return new OchibiChansConverterToolVersionFetchResult(null, error);
        }
    }
}
#endif
