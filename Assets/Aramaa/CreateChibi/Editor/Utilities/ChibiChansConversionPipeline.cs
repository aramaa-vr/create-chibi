#if UNITY_EDITOR
// Assets/Aramaa/CreateChibi/Editor/Utilities/ChibiChansConversionPipeline.cs
//
// ============================================================================
// 概要
// ============================================================================
// - おちびちゃんズ変換の「複製→反映」処理をまとめたパイプラインです
// - UI から処理部分を分離し、機能の見通しを良くします
//
// ============================================================================
// 重要メモ（初心者向け）
// ============================================================================
// - 変換は必ず複製物に対して行い、元のアバターは触りません
// - Prefab は LoadPrefabContents で展開し、元アセットは書き換えません
//
// ============================================================================
// チーム開発向けルール
// ============================================================================
// - 変換手順の順序は仕様なので、変更時はログ文言も合わせて更新する
// - Undo を必ず記録し、ユーザーが戻せる状態を維持する
//
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aramaa.CreateChibi.Editor.Utilities;
using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace Aramaa.CreateChibi.Editor
{
    /// <summary>
    /// 変換の複製・同期処理をまとめたパイプラインです。
    /// </summary>
    internal static class ChibiChansConversionPipeline
    {
        private static string L(string key) => ChibiLocalization.Get(key);
        private static string F(string key, params object[] args) => ChibiLocalization.Format(key, args);

        // --------------------------------------------------------------------
        // 処理の全体像（初心者向け）
        // --------------------------------------------------------------------
        // 1) 選択したオブジェクトを Ctrl+D 相当で複製
        // 2) 複製されたオブジェクトに対して変換を適用
        //    2-1) ルートスケール反映
        //    2-2) Armature 以下：コンポーネント追加＆パラメータ複製（存在すればスキップ）
        //    2-3) Armature 以下：Transform（位置/回転/スケール）同期
        //    2-4) SkinnedMeshRenderer：BlendShapes ウェイトのみ同期
        //    2-5) Addmenu Prefab の追加（既にあればスキップ）
        //    2-6) 指定オブジェクトの非アクティブ化
        //    2-7) VRCAvatarDescriptor：FX/Expressions/ViewPosition を同期
        //    2-8) （任意）Modular Avatar 衣装スケール調整
        // --------------------------------------------------------------------
        public static void DuplicateThenApply(
            GameObject sourceChibiPrefab,
            GameObject[] sourceTargets,
            bool applyMaboneProxyProcessing,
            List<string> logs
        )
        {
            logs ??= new List<string>();

            logs.Add(L("Log.Header.Main"));
            logs.Add(F("Log.ToolVersion", L("Tool.Name"), ChibiEditorConstants.ToolVersion));
            logs.Add(F("Log.UnityVersion", Application.unityVersion));
            logs.Add(F("Log.VrcSdkVersion", GetVrcSdkVersionInfo()));
            logs.Add(F("Log.ExecutionTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            logs.Add("");

            logs.Add(F("Log.SourcePrefab", sourceChibiPrefab?.name ?? L("Log.NullValue"), AssetDatabase.GetAssetPath(sourceChibiPrefab)));
            if (sourceTargets != null)
            {
                foreach (var t in sourceTargets.Where(x => x != null))
                {
                    logs.Add(F("Log.SourceAvatar", ChibiChansConversionLogUtility.GetHierarchyPath(t.transform)));
                }
            }

            logs.Add("");
            // ------------------------------------------------------------
            // 重要：必ず「複製物」に対して変換を適用します。
            //       複製に失敗した場合は、変換処理を一切行いません。
            // ------------------------------------------------------------

            if (sourceChibiPrefab == null || !EditorUtility.IsPersistent(sourceChibiPrefab))
            {
                EditorUtility.DisplayDialog(
                    L("Dialog.ConversionTitle"),
                    L("Dialog.InvalidSourcePrefab"),
                    L("Dialog.Ok")
                );
                return;
            }

            if (sourceTargets == null || sourceTargets.Length == 0 || sourceTargets.Any(t => t == null))
            {
                EditorUtility.DisplayDialog(
                    L("Dialog.ConversionTitle"),
                    L("Dialog.InvalidTargets"),
                    L("Dialog.Ok")
                );
                return;
            }

            // Undo をまとめる（複製 + 反映 を 1 回の Undo で戻せる方が扱いやすい）
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(L("Undo.DuplicateApply"));

            try
            {
                // --------------------------------------------------------
                // Ctrl+D と同じ経路で複製（ユーザー提供コード相当）
                // --------------------------------------------------------
                var duplicatedTargets = DuplicateLikeCtrlD.Duplicate(sourceTargets, restorePreviousSelection: false);

                if (duplicatedTargets == null || duplicatedTargets.Length == 0)
                {
                    // ここで終了：複製に失敗しているので “変換はしない”
                    Debug.LogError(L("Error.DuplicateFailed"));
                    logs.Add(L("Log.Error.DuplicateFailed"));
                    return;
                }

                // 複製物を選択しておく（Ctrl+D と同様の体験）
                Selection.objects = duplicatedTargets;

                logs.Add(L("Log.DuplicateSuccess"));
                foreach (var d in duplicatedTargets.Where(x => x != null))
                {
                    logs.Add(F("Log.DuplicateTarget", ChibiChansConversionLogUtility.GetHierarchyPath(d.transform)));
                }

                logs.Add("");

                // --------------------------------------------------------
                // 複製物に対して MABoneProxy 処理を行う（任意）
                // --------------------------------------------------------
                if (applyMaboneProxyProcessing)
                {
                    logs.Add(L("Log.MaboneProxyHeader"));
#if CHIBI_MODULAR_AVATAR
                    foreach (var duplicated in duplicatedTargets.Where(x => x != null))
                    {
                        logs.Add(F("Log.TargetEntry", ChibiChansConversionLogUtility.GetHierarchyPath(duplicated.transform)));
                        ChibiModularAvatarBoneProxyUtility.ProcessBoneProxies(duplicated, logs);
                    }
#else
                    logs.Add(L("Log.MaboneProxySkipped"));
#endif

                    logs.Add("");
                }

                // --------------------------------------------------------
                // 複製物の BlueprintID を空にする（元アバターのIDを引き継がない）
                // --------------------------------------------------------
                logs.Add(L("Log.BlueprintClearHeader"));
                foreach (var duplicated in duplicatedTargets.Where(x => x != null))
                {
                    ChibiVrcAvatarDescriptorUtility.ClearPipelineBlueprintId(duplicated, logs);
                }
                logs.Add("");

                // --------------------------------------------------------
                // 複製物へ変換を適用
                // --------------------------------------------------------
                ApplyToTargets(sourceChibiPrefab, duplicatedTargets, manageUndoGroup: false, logs: logs);
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        /// <summary>
        /// 変換（同期）処理の入口。
        /// ここでは参照の妥当性チェックと、ベースプレハブの LoadPrefabContents を行い、
        /// 各ターゲット（複製物）へ処理を適用します。
        /// </summary>
        private static void ApplyToTargets(GameObject sourceChibiPrefab, GameObject[] targets, bool manageUndoGroup, List<string> logs)
        {
            logs ??= new List<string>();
            if (sourceChibiPrefab == null || !EditorUtility.IsPersistent(sourceChibiPrefab))
            {
                EditorUtility.DisplayDialog(
                    L("Dialog.ConversionTitle"),
                    L("Dialog.InvalidSourcePrefabApply"),
                    L("Dialog.Ok")
                );
                return;
            }

            if (targets == null || targets.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    L("Dialog.ConversionTitle"),
                    L("Dialog.TargetsNotFound"),
                    L("Dialog.Ok")
                );
                return;
            }

            var basePrefabPath = AssetDatabase.GetAssetPath(sourceChibiPrefab);
            if (string.IsNullOrEmpty(basePrefabPath))
            {
                Debug.LogError(L("Error.SourcePrefabPathMissing"));

                return;
            }

            GameObject basePrefabRoot = null;

            try
            {
                logs.Add(F("Log.PrefabExpand", basePrefabPath));
                logs.Add("");
                // --------------------------------------------------------
                // Prefab を “編集用に展開” して読み取る（元 Prefab は書き換えない）
                // --------------------------------------------------------
                basePrefabRoot = PrefabUtility.LoadPrefabContents(basePrefabPath);

                // --------------------------------------------------------
                // 変換に必要な参照を sourceChibiPrefab の VRCAvatarDescriptor から抽出
                // --------------------------------------------------------
                ChibiVrcAvatarDescriptorUtility.TryGetFxPlayableLayerControllerFromBasePrefab(
                    basePrefabRoot,
                    out var fxController
                );

                ChibiVrcAvatarDescriptorUtility.TryGetExpressionsMenuAndParametersFromBasePrefab(
                    basePrefabRoot,
                    out var expressionsMenu,
                    out var expressionParameters
                );

                // Ochibichans_Addmenu は sourceChibiPrefab の内部にある想定
                TryFindExAddMenuPlacementInBasePrefab(basePrefabRoot, out var exAddMenuPlacement);

                // --------------------------------------------------------
                // 変換対象へ反映
                // --------------------------------------------------------
                foreach (var dstRoot in targets)
                {
                    logs.Add(L("Log.Separator"));
                    logs.Add(F("Log.TargetEntry", ChibiChansConversionLogUtility.GetHierarchyPath(dstRoot.transform)));

                    // 実行前の参照（FX / Menu / Parameters）を取得してログ用に保持（値は出さない）
                    ChibiVrcAvatarDescriptorUtility.TryGetFxPlayableLayerControllerFromAvatar(dstRoot, out var fxBefore);
                    ChibiVrcAvatarDescriptorUtility.TryGetExpressionsMenuAndParametersFromAvatar(dstRoot, out var menuBefore, out var paramsBefore);

                    logs.Add(F("Log.FxBefore", ChibiChansConversionLogUtility.FormatAssetRef(fxBefore)));
                    logs.Add(F("Log.MenuBefore", ChibiChansConversionLogUtility.FormatAssetRef(menuBefore)));
                    logs.Add(F("Log.ParametersBefore", ChibiChansConversionLogUtility.FormatAssetRef(paramsBefore)));
                    logs.Add("");
                    if (dstRoot == null)
                    {
                        continue;
                    }

                    // 1)～4) の基本反映（ルートスケール / Armature 下 / Transform / BlendShape）
                    ApplyCore_1to4(basePrefabRoot, dstRoot, logs);

                    // 追加メニュー Prefab を子として追加（既にあるなら追加しない）
                    if (exAddMenuPlacement.PrefabAsset != null)
                    {
                        logs.Add(L("Log.ExPrefabHeader"));
                        AddExPrefabAsChildIfMissing(dstRoot, exAddMenuPlacement, logs);
                        logs.Add("");
                    }

                    // VRCAvatarDescriptor の参照を sourceChibiPrefab 側と同じにする
                    if (fxController != null)
                    {
                        logs.Add(F("Log.FxApply", ChibiChansConversionLogUtility.FormatAssetRef(fxController)));
                        ChibiVrcAvatarDescriptorUtility.SetFxPlayableLayerController(dstRoot, fxController);
                    }
                    else
                    {
                        logs.Add(L("Log.FxApplySkipped"));
                    }

                    if (expressionsMenu != null || expressionParameters != null)
                    {
                        logs.Add(F("Log.ExpressionsApply", ChibiChansConversionLogUtility.FormatAssetRef(expressionsMenu), ChibiChansConversionLogUtility.FormatAssetRef(expressionParameters)));
                        ChibiVrcAvatarDescriptorUtility.SetExpressionsMenuAndParameters(dstRoot, expressionsMenu, expressionParameters);
                    }
                    else
                    {
                        logs.Add(L("Log.ExpressionsApplySkipped"));
                    }

                    // ViewPosition（ビューポイント）も sourceChibiPrefab と同じにする
                    {
                        var viewOk = ChibiVrcAvatarDescriptorUtility.TryCopyViewPositionFromBasePrefab(dstRoot, basePrefabRoot);
                        logs.Add(viewOk ? L("Log.ViewPositionApplied") : L("Log.ViewPositionSkipped"));
                    }

                    // 服のスケール調整（Modular Avatar が入っている場合のみ）
                    if (!ChibiModularAvatarUtility.AdjustCostumeScalesForModularAvatarMeshSettings(dstRoot, basePrefabRoot, logs))
                    {
                        logs.Add(L("Log.Error.CostumeScaleFailed"));
                        return;
                    }

                    // 実行後の参照（FX / Menu / Parameters）
                    ChibiVrcAvatarDescriptorUtility.TryGetFxPlayableLayerControllerFromAvatar(dstRoot, out var fxAfter);
                    ChibiVrcAvatarDescriptorUtility.TryGetExpressionsMenuAndParametersFromAvatar(dstRoot, out var menuAfter, out var paramsAfter);

                    logs.Add("");
                    logs.Add(F("Log.FxAfter", ChibiChansConversionLogUtility.FormatAssetRef(fxAfter)));
                    logs.Add(F("Log.MenuAfter", ChibiChansConversionLogUtility.FormatAssetRef(menuAfter)));
                    logs.Add(F("Log.ParametersAfter", ChibiChansConversionLogUtility.FormatAssetRef(paramsAfter)));

                    // 差分が分かるように補足（値は出さない）
                    if (!ReferenceEquals(fxBefore, fxAfter))
                    {
                        logs.Add(L("Log.FxChanged"));
                    }

                    if (!ReferenceEquals(menuBefore, menuAfter))
                    {
                        logs.Add(L("Log.MenuChanged"));
                    }

                    if (!ReferenceEquals(paramsBefore, paramsAfter))
                    {
                        logs.Add(L("Log.ParametersChanged"));
                    }

                    logs.Add(L("Log.Separator"));
                    logs.Add("");
                }
            }
            finally
            {
                // 展開した Prefab コンテンツは必ず解放
                if (basePrefabRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(basePrefabRoot);
                }
            }
        }

        /// <summary>
        /// sourceChibiPrefab（PrefabContentsとして読み込んだ一時ルート）から、
        /// 「Ochibichans_Addmenu.prefab を参照しているネストPrefabインスタンス」を探し、
        /// 追加に必要な情報（Prefabアセット参照 + 位置/回転/スケール + 望ましい親パス）を返します。
        /// </summary>
        private struct ExPrefabPlacement
        {
            public GameObject PrefabAsset;
            public string ParentPathFromAvatarRoot;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
        }

        private static string GetVrcSdkVersionInfo()
        {
#if VRC_SDK_VRCSDK3
            var assembly = typeof(VRCAvatarDescriptor).Assembly;
            var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var version = info?.InformationalVersion ?? assembly.GetName().Version?.ToString() ?? L("Log.UnknownVersion");
            return $"{assembly.GetName().Name} {version}";
#else
            return L("Log.NotFound");
#endif
        }

        /// <summary>
        /// sourceChibiPrefab 内のネスト Prefab 参照から、EXメニュー（Ochibichans_Addmenu）を特定します。
        /// </summary>
        private static bool TryFindExAddMenuPlacementInBasePrefab(GameObject basePrefabRoot, out ExPrefabPlacement placement)
        {
            placement = default;

            if (basePrefabRoot == null)
            {
                return false;
            }

            // ------------------------------------------------------------
            // 仕様：
            // sourceChibiPrefab（= basePrefabRoot）内には、
            // 「Ochibichans_Addmenu.prefab のネスト（Prefabインスタンス）」が含まれている想定です。
            //
            // ここで “重要” なのは、
            //  - basePrefabRoot 上の GameObject（PrefabContents の一時オブジェクト）を
            //    そのままターゲットへ移動/親変更しないこと。
            //    （Prefabインスタンス内の Transform は親変更できず、エラーになります）
            //
            // そのため、必ず「参照している Prefab アセット（Project 上の .prefab）」を見つけて、
            // そのアセットを Instantiate してターゲットに追加する方式にします。
            // ------------------------------------------------------------

            // 期待するファイル名（拡張子まで一致）
            const string targetFileName = ChibiEditorConstants.AddMenuPrefabFileName;

            // ------------------------------------------------------------
            // まずは “Prefabアセットパス” から確実に特定します。
            // ネストPrefabのルート名が変更されていても、
            // 参照しているアセットパスが一致すれば発見できます。
            // ------------------------------------------------------------
            foreach (var t in basePrefabRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t == null)
                {
                    continue;
                }

                var go = t.gameObject;
                if (go == null)
                {
                    continue;
                }

                if (go == basePrefabRoot)
                {
                    continue;
                }

                // ネストPrefabに属している（= Prefabインスタンスの一部）なら、
                // その「最も近いインスタンスルート」を取得できます。
                var instRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                if (instRoot == null)
                {
                    continue;
                }

                if (instRoot == basePrefabRoot)
                {
                    continue;
                }

                var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instRoot);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                // ファイル名で一致判定（大文字小文字は無視）
                if (!assetPath.EndsWith(targetFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (asset == null)
                {
                    continue;
                }

                // --------------------------------------------------------
                // “座標が反映されない” 問題の対策：
                // sourceChibiPrefab 内での「ローカル座標/回転/スケール」を読み取り、
                // Instantiate した後に同じ値を適用します。
                // --------------------------------------------------------
                var instTransform = instRoot.transform;
                placement = new ExPrefabPlacement
                {
                    PrefabAsset = asset,
                    ParentPathFromAvatarRoot = GetRelativePathFromRoot(basePrefabRoot.transform, instTransform.parent),
                    LocalPosition = instTransform.localPosition,
                    LocalRotation = instTransform.localRotation,
                    LocalScale = instTransform.localScale
                };
                return true;
            }

            // ------------------------------------------------------------
            // フォールバック：オブジェクト名に "Ochibichans_Addmenu" を含むものから辿ります。
            // （Prefabのファイル名が変わっている等のレアケース向け）
            // ------------------------------------------------------------
            foreach (var t in basePrefabRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t == null)
                {
                    continue;
                }

                var go = t.gameObject;
                if (go == null)
                {
                    continue;
                }

                if (go == basePrefabRoot)
                {
                    continue;
                }

                if (go.name.IndexOf(ChibiEditorConstants.AddMenuNameKeyword, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var instRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                if (instRoot == null)
                {
                    continue;
                }

                if (instRoot == basePrefabRoot)
                {
                    continue;
                }

                var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instRoot);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (asset == null)
                {
                    continue;
                }

                var instTransform = instRoot.transform;
                placement = new ExPrefabPlacement
                {
                    PrefabAsset = asset,
                    ParentPathFromAvatarRoot = GetRelativePathFromRoot(basePrefabRoot.transform, instTransform.parent),
                    LocalPosition = instTransform.localPosition,
                    LocalRotation = instTransform.localRotation,
                    LocalScale = instTransform.localScale
                };
                return true;
            }

            Debug.LogWarning(L("Warning.AddMenuPrefabMissing"));
            return false;
        }

        /// <summary>
        /// root から target までの相対パス（例: "A/B/C"）を返します。
        /// 見つからない/同一の場合は空文字を返します。
        /// </summary>
        private static string GetRelativePathFromRoot(Transform root, Transform target)
        {
            if (root == null || target == null)
            {
                return string.Empty;
            }

            if (target == root)
            {
                return string.Empty;
            }

            // root より上に行ってしまう場合は「不正」とみなして空にします。
            var names = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                names.Add(current.name);
                current = current.parent;
            }

            if (current != root)
            {
                return string.Empty;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static void ApplyCore_1to4(GameObject srcRoot, GameObject dstRoot, List<string> logs)
        {
            logs ??= new List<string>();

            logs.Add(L("Log.CoreHeader"));
            logs.Add(L("Log.RootScaleApplied"));
            Undo.RecordObject(dstRoot.transform, L("Undo.SyncRootScale"));
            dstRoot.transform.localScale = srcRoot.transform.localScale;
            EditorUtility.SetDirty(dstRoot.transform);

            var srcArmature = ChibiEditorUtility.FindAvatarMainArmature(srcRoot.transform);
            if (srcArmature == null)
            {
                return;
            }

            var dstArmature = ChibiEditorUtility.FindAvatarMainArmature(dstRoot.transform);
            if (dstArmature == null)
            {
                return;
            }

            CopyArmatureTransforms(srcArmature, dstArmature, logs);
            AddMissingComponentsUnderArmature(srcRoot, dstRoot, srcArmature, dstArmature, logs);
            ChibiSkinnedMeshUtility.CopySkinnedMeshRenderersBlendShapesOnlyWithLog(srcRoot, dstRoot, logs);
        }

        private static void CopyArmatureTransforms(Transform srcArmature, Transform dstArmature, List<string> logs)
        {
            if (srcArmature == null || dstArmature == null)
            {
                return;
            }

            logs ??= new List<string>();
            logs.Add(L("Log.ArmatureTransformApplied"));

            var srcAll = srcArmature.GetComponentsInChildren<Transform>(true);
            int updated = 0;

            foreach (var srcT in srcAll)
            {
                if (srcT == null)
                {
                    continue;
                }

                string rel = AnimationUtility.CalculateTransformPath(srcT, srcArmature);
                rel = ChibiEditorUtility.NormalizeRelPathFor(dstArmature, rel);

                var dstT = string.IsNullOrEmpty(rel) ? dstArmature : dstArmature.Find(rel);
                if (dstT == null)
                {
                    continue;
                }

                Undo.RecordObject(dstT, L("Undo.SyncArmatureTransform"));
                dstT.localPosition = srcT.localPosition;
                dstT.localRotation = srcT.localRotation;
                dstT.localScale = srcT.localScale;

                EditorUtility.SetDirty(dstT);

                updated++;
                // パスだけを出す（値は出さない）
                logs.Add(F("Log.PathEntry", ChibiChansConversionLogUtility.GetHierarchyPath(dstT)));
            }

            logs.Add(F("Log.ArmatureTransformUpdated", updated));
            logs.Add("");
        }

        private static void AddMissingComponentsUnderArmature(
            GameObject srcRoot,
            GameObject dstRoot,
            Transform srcArmature,
            Transform dstArmature,
            List<string> logs
        )
        {
            if (srcRoot == null || dstRoot == null || srcArmature == null || dstArmature == null)
            {
                return;
            }

            logs ??= new List<string>();
            logs.Add(L("Log.AddMissingComponents"));

            var srcAll = srcArmature.GetComponentsInChildren<Transform>(true);
            int addedCount = 0;

            foreach (var srcT in srcAll)
            {
                if (srcT == null)
                {
                    continue;
                }

                string rel = AnimationUtility.CalculateTransformPath(srcT, srcArmature);
                rel = ChibiEditorUtility.NormalizeRelPathFor(dstArmature, rel);

                var dstT = string.IsNullOrEmpty(rel) ? dstArmature : dstArmature.Find(rel);
                if (dstT == null)
                {
                    continue;
                }

                var srcGO = srcT.gameObject;
                var dstGO = dstT.gameObject;

                var srcComps = srcGO.GetComponents<Component>();
                var srcByType = new Dictionary<Type, List<Component>>();

                foreach (var c in srcComps)
                {
                    if (c == null)
                    {
                        continue;
                    }

                    if (c is Transform)
                    {
                        continue;
                    }

                    var type = c.GetType();
                    if (!srcByType.TryGetValue(type, out var list))
                    {
                        list = new List<Component>();
                        srcByType[type] = list;
                    }

                    list.Add(c);
                }

                foreach (var kv in srcByType)
                {
                    var type = kv.Key;
                    var srcList = kv.Value;

                    var dstExisting = dstGO.GetComponents(type);
                    int dstCount = dstExisting != null ? dstExisting.Length : 0;

                    for (int i = 0; i < srcList.Count; i++)
                    {
                        if (i < dstCount)
                        {
                            continue;
                        }

                        Component newComp = null;

                        try
                        {
                            newComp = Undo.AddComponent(dstGO, type);
                        }
                        catch
                        {
                            continue;
                        }

                        if (newComp == null)
                        {
                            continue;
                        }

                        try
                        {
                            EditorUtility.CopySerialized(srcList[i], newComp);
                        }
                        catch
                        {
                            // コピーに失敗した場合は、追加だけ残して処理続行
                        }

                        ChibiEditorUtility.RemapObjectReferencesInObject(newComp, srcRoot, dstRoot);
                        EditorUtility.SetDirty(newComp);

                        addedCount++;
                        logs.Add(F("Log.ComponentAdded", type.Name, ChibiChansConversionLogUtility.GetHierarchyPath(dstT)));
                    }
                }
            }

            logs.Add(F("Log.MissingComponentsAdded", addedCount));
            logs.Add("");
        }

        private static bool AddExPrefabAsChildIfMissing(GameObject avatarRoot, ExPrefabPlacement placement, List<string> logs)
        {
            if (avatarRoot == null)
            {
                return false;
            }

            if (placement.PrefabAsset == null)
            {
                return false;
            }

            logs ??= new List<string>();
            var prefabPath = AssetDatabase.GetAssetPath(placement.PrefabAsset);

            // 既に同じ EX プレハブ（同じPrefabアセット由来のインスタンス）が
            // avatarRoot 配下のどこかに存在するなら、重複追加しません。
            if (HasPrefabInstanceInDescendants(avatarRoot, placement.PrefabAsset))
            {
                logs.Add(F("Log.ExPrefabAlreadyExists", placement.PrefabAsset.name, prefabPath));
                return false;
            }

            var instanceObj = PrefabUtility.InstantiatePrefab(placement.PrefabAsset) as GameObject;
            if (instanceObj == null)
            {
                logs.Add(F("Log.ExPrefabCreationFailed", placement.PrefabAsset.name, prefabPath));
                return false;
            }

            // “どこにぶら下げるか” は sourceChibiPrefab の配置を優先します。
            // ただし、ターゲット側で同じパスが見つからない場合はアバタールート直下にフォールバックします。
            Transform parentTransform = avatarRoot.transform;
            if (!string.IsNullOrEmpty(placement.ParentPathFromAvatarRoot))
            {
                var foundParent = avatarRoot.transform.Find(placement.ParentPathFromAvatarRoot);
                if (foundParent != null)
                {
                    parentTransform = foundParent;
                }
            }

            // Undo で「生成」と「親付け替え」を記録（Ctrl+Z 対応）
            Undo.RegisterCreatedObjectUndo(instanceObj, L("Undo.AddExPrefab"));
            Undo.SetTransformParent(instanceObj.transform, parentTransform, L("Undo.AddExPrefab"));

            // “座標が反映されない” 問題の対策：
            // sourceChibiPrefab 内でのローカル姿勢を、そのまま複製して適用します。
            // ※ログでは値は出しません
            Undo.RecordObject(instanceObj.transform, L("Undo.AddExPrefab"));
            instanceObj.transform.localPosition = placement.LocalPosition;
            instanceObj.transform.localRotation = placement.LocalRotation;
            instanceObj.transform.localScale = placement.LocalScale;

            EditorUtility.SetDirty(instanceObj);

            logs.Add(F("Log.ExPrefabAdded", placement.PrefabAsset.name, prefabPath));
            logs.Add(F("Log.ExPrefabParent", ChibiChansConversionLogUtility.GetHierarchyPath(parentTransform)));
            logs.Add(F("Log.ExPrefabAddedTo", ChibiChansConversionLogUtility.GetHierarchyPath(instanceObj.transform)));
            logs.Add(L("Log.ExPrefabTransformApplied"));

            return true;
        }

        private static bool HasPrefabInstanceInDescendants(GameObject parent, GameObject prefabAssetRoot)
        {
            if (parent == null || prefabAssetRoot == null)
            {
                return false;
            }

            // ------------------------------------------------------------
            // 既に同じ Prefab アセット由来のインスタンスが存在するか？
            //
            // 以前は GetCorrespondingObjectFromSource の “参照一致” で判定していましたが、
            // - prefabAssetRoot が「ルートGameObject」ではなく “Prefab内の子オブジェクト” だった
            // - ネストPrefab / Variant などで参照が取りにくい
            // といったケースで誤判定が起きやすいです。
            //
            // そこで、より確実に「Prefabアセットのパス」で比較します。
            // ------------------------------------------------------------
            var targetAssetPath = AssetDatabase.GetAssetPath(prefabAssetRoot);
            if (string.IsNullOrEmpty(targetAssetPath))
            {
                // ここが空なら「Project上のアセット」ではない可能性が高い（安全のため重複判定しない）
                return false;
            }

            // parent 自身は除外して “子孫” を検索
            var descendants = parent.GetComponentsInChildren<Transform>(includeInactive: true);
            for (int i = 0; i < descendants.Length; i++)
            {
                var t = descendants[i];
                if (t == null)
                {
                    continue;
                }

                if (t == parent.transform)
                {
                    continue;
                }

                // Prefabインスタンスの “ルート” だけを対象にする（子要素は判定がブレるため）
                var instRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
                if (instRoot != t.gameObject)
                {
                    continue;
                }

                var instAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instRoot);
                if (string.IsNullOrEmpty(instAssetPath))
                {
                    continue;
                }

                if (string.Equals(instAssetPath, targetAssetPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // ログ用ユーティリティは ChibiChansConversionLogUtility に切り出し
    }
}
#endif
