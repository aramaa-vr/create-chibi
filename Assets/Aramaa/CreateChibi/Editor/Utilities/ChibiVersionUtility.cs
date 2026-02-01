#if UNITY_EDITOR
// Assets/Aramaa/CreateChibi/Editor/Utilities/ChibiVersionUtility.cs
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

namespace Aramaa.CreateChibi.Editor.Utilities
{
    internal enum ChibiVersionStatus
    {
        Unknown,
        UpToDate,
        UpdateAvailable,
        Ahead
    }

    internal static class ChibiVersionUtility
    {
        public static void FetchLatestVersionAsync(string url, Action<ChibiVersionFetchResult> onComplete)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                onComplete?.Invoke(ChibiVersionFetchResult.Failure(ChibiLocalization.Get("Version.ErrorMissingUrl")));
                return;
            }

            var request = UnityWebRequest.Get(url);
            request.timeout = 10;

            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        onComplete?.Invoke(ChibiVersionFetchResult.Failure(request.error));
                        return;
                    }

                    var latestVersion = ExtractVersionFromResponse(request.downloadHandler.text);
                    if (string.IsNullOrWhiteSpace(latestVersion))
                    {
                        onComplete?.Invoke(ChibiVersionFetchResult.Failure(ChibiLocalization.Get("Version.ExtractFailed")));
                        return;
                    }

                    onComplete?.Invoke(ChibiVersionFetchResult.Success(latestVersion));
                }
                finally
                {
                    request.Dispose();
                }
            };
        }

        public static ChibiVersionStatus GetVersionStatus(string currentVersion, string latestVersion)
        {
            if (string.IsNullOrWhiteSpace(currentVersion) || string.IsNullOrWhiteSpace(latestVersion))
            {
                return ChibiVersionStatus.Unknown;
            }

            var comparison = CompareVersions(currentVersion, latestVersion);
            if (comparison == 0)
            {
                return ChibiVersionStatus.UpToDate;
            }

            return comparison < 0 ? ChibiVersionStatus.UpdateAvailable : ChibiVersionStatus.Ahead;
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

    internal readonly struct ChibiVersionFetchResult
    {
        private ChibiVersionFetchResult(string latestVersion, string error)
        {
            LatestVersion = latestVersion;
            Error = error;
        }

        public string LatestVersion { get; }
        public string Error { get; }
        public bool Succeeded => string.IsNullOrWhiteSpace(Error);

        public static ChibiVersionFetchResult Success(string latestVersion)
        {
            return new ChibiVersionFetchResult(latestVersion, null);
        }

        public static ChibiVersionFetchResult Failure(string error)
        {
            return new ChibiVersionFetchResult(null, error);
        }
    }
}
#endif
