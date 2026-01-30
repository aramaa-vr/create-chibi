#if UNITY_EDITOR
// Assets/Aramaa/CreateChibi/Editor/Utilities/ChibiChansPrefabDropdownCache.cs
//
// ============================================================================
// 概要
// ============================================================================
// - アバターの顔メッシュに一致するおちびちゃんズ Prefab を高速に検索するキャッシュです
// - Prefab の依存ハッシュと紐づけて、再走査の回数を減らします
//
// ============================================================================
// 重要メモ（初心者向け）
// ============================================================================
// - VRChat SDK が無い環境では検索できないため、安全にスキップします
// - Project 側の Prefab を読み取るだけで、Prefab 自体の内容は変更しません
//
// ============================================================================
// チーム開発向けルール
// ============================================================================
// - キャッシュは EditorUserSettings に保存し、VCS には含めません（各環境ローカル）
// - Prefab 判定ロジックを変える際は、候補優先順位の理由もコメントに残す
//
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Aramaa.CreateChibi.Editor.Utilities
{
    /// <summary>
    /// アバターの顔メッシュ情報を基に、候補となるおちびちゃんズ Prefab の一覧を作るキャッシュです。
    /// </summary>
    internal sealed class ChibiChansPrefabDropdownCache
    {
        private const string BaseFolder = ChibiEditorConstants.BaseFolder;

        // EditorUserSettings に保存するキー（プロジェクト単位・ユーザー単位）。
        // 末尾の v1 は「キャッシュ互換性（このキャッシュを再利用して良いか）」のバージョン。
        // 互換が壊れる変更を入れたら v2 に上げる（JSON構造が同じでも上げてよい）。
        private const string FaceMeshCacheConfigKey = "Aramaa.CreateChibi.FaceMeshCache.v1";

        private static readonly Dictionary<string, CachedFaceMesh> CachedFaceMeshByPrefab =
            new Dictionary<string, CachedFaceMesh>();
        private static bool _faceMeshCacheLoaded;
        private static bool _faceMeshCacheDirty;

        private int _cachedTargetInstanceId;
        private bool _needsRefreshPrefabs = true;
        private int _selectedPrefabIndex;
        private GameObject _sourcePrefabAsset;
        private readonly List<string> _candidatePrefabPaths = new List<string>();
        private readonly List<string> _candidateDisplayNames = new List<string>();

        public IReadOnlyList<string> CandidateDisplayNames => _candidateDisplayNames;
        public int SelectedIndex => _selectedPrefabIndex;
        public GameObject SourcePrefabAsset => _sourcePrefabAsset;

        public static void SaveCacheToDisk()
        {
            SaveFaceMeshCacheToEditorUserSettings();
        }

        /// <summary>
        /// 対象アバターの変更に備えて、次回の候補再構築を予約します。
        /// </summary>
        public void MarkNeedsRefresh()
        {
            _needsRefreshPrefabs = true;
        }

        /// <summary>
        /// ドロップダウンで選ばれたインデックスを反映し、選択中 Prefab を更新します。
        /// </summary>
        public void ApplySelection(int nextIndex)
        {
            if (_candidatePrefabPaths.Count == 0)
            {
                _sourcePrefabAsset = null;
                return;
            }

            _selectedPrefabIndex = Mathf.Clamp(nextIndex, 0, _candidatePrefabPaths.Count - 1);
            var selectedPath = _candidatePrefabPaths[_selectedPrefabIndex];
            _sourcePrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(selectedPath);
        }

        /// <summary>
        /// 対象アバターが変わったときのみ候補を再計算します。
        /// </summary>
        public void RefreshIfNeeded(GameObject sourceTarget)
        {
            if (sourceTarget == null)
            {
                ClearState();
                return;
            }

            var instanceId = sourceTarget.GetInstanceID();
            if (!_needsRefreshPrefabs && instanceId == _cachedTargetInstanceId) return;

            _needsRefreshPrefabs = false;
            _cachedTargetInstanceId = instanceId;

            _candidatePrefabPaths.Clear();
            _candidateDisplayNames.Clear();
            _selectedPrefabIndex = 0;
            _sourcePrefabAsset = null;

            if (!TryGetFaceMeshId(sourceTarget, out var avatarFaceMeshId)) return;

            var subFolders = AssetDatabase.GetSubFolders(BaseFolder);
            foreach (var folder in subFolders)
            {
                var prefabPath = FindPreferredPrefabPathUnder(folder);
                if (string.IsNullOrEmpty(prefabPath)) continue;

                if (!PrefabHasMatchingFaceMesh(prefabPath, avatarFaceMeshId)) continue;

                _candidatePrefabPaths.Add(prefabPath);
                _candidateDisplayNames.Add(Path.GetFileName(folder));
            }

            if (_candidatePrefabPaths.Count > 0)
            {
                ApplySelection(_selectedPrefabIndex);
            }
        }

        private void ClearState()
        {
            _candidatePrefabPaths.Clear();
            _candidateDisplayNames.Clear();
            _sourcePrefabAsset = null;
            _cachedTargetInstanceId = 0;
            _selectedPrefabIndex = 0;
            _needsRefreshPrefabs = false;
        }

        /// <summary>
        /// 指定フォルダ配下の Prefab から、優先順位に従って候補を1つ選びます。
        /// </summary>
        private static string FindPreferredPrefabPathUnder(string folder)
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            if (prefabGuids == null || prefabGuids.Length == 0) return null;

            var candidates = new List<string>();
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;

                candidates.Add(path);
            }

            if (candidates.Count == 0) return null;

            var preferred = PickPrefabByFilenamePattern(candidates, "Kisekae Variant");
            if (!string.IsNullOrEmpty(preferred)) return preferred;

            preferred = PickPrefabByFilenamePattern(candidates, "Kaihen_Kisekae");
            if (!string.IsNullOrEmpty(preferred)) return preferred;

            preferred = PickPrefabByFilenamePattern(candidates, "Kisekae");
            if (!string.IsNullOrEmpty(preferred)) return preferred;

            return candidates[0];
        }

        private static string PickPrefabByFilenamePattern(IEnumerable<string> paths, string pattern)
        {
            if (paths == null) return null;
            if (string.IsNullOrEmpty(pattern)) return null;

            var match = paths.FirstOrDefault(path =>
                Path.GetFileNameWithoutExtension(path)
                    .IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0);

            return match;
        }

        private static bool PrefabHasMatchingFaceMesh(string prefabPath, MeshId targetFaceMeshId)
        {
            if (string.IsNullOrEmpty(prefabPath)) return false;
            if (string.IsNullOrEmpty(targetFaceMeshId.Guid)) return false;

            return TryGetCachedFaceMeshId(prefabPath, out var prefabFaceMeshId) &&
                   FaceMeshIdMatches(targetFaceMeshId, prefabFaceMeshId);
        }

        /// <summary>
        /// アバターの Viseme 用メッシュから、GUID/LocalId を抽出します。
        /// </summary>
        private static bool TryGetFaceMeshId(GameObject root, out MeshId meshId)
        {
            meshId = default;
            if (root == null) return false;

#if VRC_SDK_VRCSDK3
            var descriptor = root.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            if (!TryGetVisemeRendererFromDescriptor(descriptor, out var faceRenderer)) return false;
            if (faceRenderer == null || faceRenderer.sharedMesh == null) return false;

            return TryBuildMeshId(faceRenderer.sharedMesh, out meshId);
#else
            return false;
#endif
        }

        private static bool TryGetFaceMeshIdFromPrefabPath(string prefabPath, out MeshId meshId)
        {
            meshId = default;
            if (string.IsNullOrEmpty(prefabPath)) return false;

            var prefab = AssetDatabase.LoadMainAssetAtPath(prefabPath) as GameObject;
            if (prefab == null) return false;

            return TryGetFaceMeshId(prefab, out meshId);
        }

        /// <summary>
        /// Prefab の依存ハッシュを使って、顔メッシュIDのキャッシュを再利用します。
        /// </summary>
        private static bool TryGetCachedFaceMeshId(string prefabPath, out MeshId meshId)
        {
            meshId = default;
            if (string.IsNullOrEmpty(prefabPath)) return false;

            EnsureFaceMeshCacheLoaded();

            var hash = AssetDatabase.GetAssetDependencyHash(prefabPath);
            if (CachedFaceMeshByPrefab.TryGetValue(prefabPath, out var cached) &&
                cached.DependencyHash == hash)
            {
                if (cached.HasFaceMesh)
                {
                    meshId = cached.FaceMeshId;
                    return true;
                }

                return false;
            }

            var hasFaceMesh = TryGetFaceMeshIdFromPrefabPath(prefabPath, out var cachedMeshId);
            CachedFaceMeshByPrefab[prefabPath] = new CachedFaceMesh(hash, cachedMeshId, hasFaceMesh);
            MarkFaceMeshCacheDirty();
            // ここでは即時保存しません。
            // - OnDisable でまとめて保存される
            // - 検索中に毎回ディスク/設定を書き換える回数を減らし、Editor の負荷を抑える
            if (hasFaceMesh)
            {
                meshId = cachedMeshId;
            }

            return hasFaceMesh;
        }

        private static bool FaceMeshIdMatches(MeshId a, MeshId b)
        {
            if (string.IsNullOrEmpty(a.Guid) || string.IsNullOrEmpty(b.Guid)) return false;
            if (!string.Equals(a.Guid, b.Guid, StringComparison.Ordinal)) return false;

            if (a.HasLocalId && b.HasLocalId)
            {
                return a.LocalId == b.LocalId;
            }

            return true;
        }

        private static bool TryBuildMeshId(Mesh mesh, out MeshId meshId)
        {
            meshId = default;
            if (mesh == null) return false;

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out var guid, out long localId))
            {
                meshId = new MeshId(guid, localId, hasLocalId: true);
                return true;
            }

            var meshPath = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(meshPath)) return false;

            var fallbackGuid = AssetDatabase.AssetPathToGUID(meshPath);
            if (string.IsNullOrEmpty(fallbackGuid)) return false;

            meshId = new MeshId(fallbackGuid, 0, hasLocalId: false);
            return true;
        }

#if VRC_SDK_VRCSDK3
        private static bool TryGetVisemeRendererFromDescriptor(
            VRC.SDK3.Avatars.Components.VRCAvatarDescriptor descriptor,
            out SkinnedMeshRenderer renderer)
        {
            renderer = null;
            if (descriptor == null) return false;

            using (var so = new SerializedObject(descriptor))
            {
                var prop = so.FindProperty("VisemeSkinnedMesh");
                if (prop != null)
                {
                    renderer = prop.objectReferenceValue as SkinnedMeshRenderer;
                }
            }

            if (renderer != null) return true;

            renderer = descriptor.VisemeSkinnedMesh;
            return renderer != null;
        }
#endif

        private static void EnsureFaceMeshCacheLoaded()
        {
            if (_faceMeshCacheLoaded) return;
            _faceMeshCacheLoaded = true;
            LoadFaceMeshCacheFromEditorUserSettings();
        }

        private static void MarkFaceMeshCacheDirty()
        {
            _faceMeshCacheDirty = true;
        }

        private static void LoadFaceMeshCacheFromEditorUserSettings()
        {
            try
            {
                var json = EditorUserSettings.GetConfigValue(FaceMeshCacheConfigKey);
                if (string.IsNullOrEmpty(json)) return;

                var cacheFile = JsonUtility.FromJson<FaceMeshCacheFile>(json);
                if (cacheFile == null) return;
                if (cacheFile.Entries == null) return;

                foreach (var entry in cacheFile.Entries)
                {
                    if (entry == null) continue;
                    if (string.IsNullOrEmpty(entry.PrefabPath)) continue;
                    if (string.IsNullOrEmpty(entry.DependencyHash)) continue;

                    if (!TryParseHash128(entry.DependencyHash, out var hash)) continue;

                    var meshId = new MeshId(entry.FaceMeshGuid ?? string.Empty, entry.FaceMeshLocalId, entry.HasLocalId);
                    CachedFaceMeshByPrefab[entry.PrefabPath] = new CachedFaceMesh(hash, meshId, entry.HasFaceMesh);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CreateChibi] フェイスメッシュキャッシュの読み込みに失敗しました: {e.Message}");
            }
        }

        private static void SaveFaceMeshCacheToEditorUserSettings()
        {
            if (!_faceMeshCacheDirty) return;

            try
            {
                var cacheFile = new FaceMeshCacheFile();
                foreach (var pair in CachedFaceMeshByPrefab)
                {
                    var cached = pair.Value;
                    cacheFile.Entries.Add(new FaceMeshCacheEntry
                    {
                        PrefabPath = pair.Key,
                        DependencyHash = cached.DependencyHash.ToString(),
                        FaceMeshGuid = cached.FaceMeshId.Guid,
                        FaceMeshLocalId = cached.FaceMeshId.LocalId,
                        HasLocalId = cached.FaceMeshId.HasLocalId,
                        HasFaceMesh = cached.HasFaceMesh
                    });
                }

                var json = JsonUtility.ToJson(cacheFile, true);
                EditorUserSettings.SetConfigValue(FaceMeshCacheConfigKey, json);
                _faceMeshCacheDirty = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CreateChibi] フェイスメッシュキャッシュの保存に失敗しました: {e.Message}");
            }
        }

        private static bool TryParseHash128(string value, out Hash128 hash)
        {
            hash = default;
            if (string.IsNullOrEmpty(value)) return false;

            try
            {
                hash = Hash128.Parse(value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Serializable]
        private sealed class FaceMeshCacheFile
        {
            public List<FaceMeshCacheEntry> Entries = new List<FaceMeshCacheEntry>();
        }

        /// <summary>
        /// フェイスメッシュキャッシュのシリアライズ用エントリです。
        /// </summary>
        [Serializable]
        private sealed class FaceMeshCacheEntry
        {
            public string PrefabPath;
            public string DependencyHash;
            public string FaceMeshGuid;
            public long FaceMeshLocalId;
            public bool HasLocalId;
            public bool HasFaceMesh;
        }

        /// <summary>
        /// メッシュ識別子（GUID と LocalId の組）です。
        /// </summary>
        private readonly struct MeshId
        {
            public MeshId(string guid, long localId, bool hasLocalId)
            {
                Guid = guid;
                LocalId = localId;
                HasLocalId = hasLocalId;
            }

            public string Guid { get; }
            public long LocalId { get; }
            public bool HasLocalId { get; }
        }

        /// <summary>
        /// Prefab の依存ハッシュと顔メッシュIDをまとめたキャッシュ情報です。
        /// </summary>
        private readonly struct CachedFaceMesh
        {
            public CachedFaceMesh(Hash128 dependencyHash, MeshId faceMeshId, bool hasFaceMesh)
            {
                DependencyHash = dependencyHash;
                FaceMeshId = faceMeshId;
                HasFaceMesh = hasFaceMesh;
            }

            public Hash128 DependencyHash { get; }
            public MeshId FaceMeshId { get; }
            public bool HasFaceMesh { get; }
        }
    }
}
#endif
