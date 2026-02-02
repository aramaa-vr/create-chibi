#if UNITY_EDITOR && CHIBI_MODULAR_AVATAR
// Assets/Aramaa/CreateChibi/Editor/Utilities/ChibiModularAvatarBoneProxyUtility.cs
//
// ============================================================================
// 概要
// ============================================================================
// - MABoneProxy（Modular Avatar）を複製物に対して実行します
// - Modular Avatar の BoneProxyProcessor に近い処理を行います
//
// ============================================================================

using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;

namespace Aramaa.CreateChibi.Editor.Utilities
{
    /// <summary>
    /// Modular Avatar の MABoneProxy を複製物に適用するユーティリティです。
    /// </summary>
    internal static class ChibiModularAvatarBoneProxyUtility
    {
        private enum ValidationResult
        {
            Ok,
            MovingTarget,
            NotInAvatar
        }

        public static void ProcessBoneProxies(GameObject avatarRoot, List<string> logs = null)
        {
            if (avatarRoot == null)
            {
                return;
            }

            var proxies = avatarRoot.GetComponentsInChildren<ModularAvatarBoneProxy>(true);
            if (proxies == null || proxies.Length == 0)
            {
                logs?.Add(ChibiLocalization.Get("Log.MaboneProxyNone"));
                return;
            }

            logs?.Add(ChibiLocalization.Format("Log.MaboneProxyCount", proxies.Length));

            var unpackedPrefabRoots = new HashSet<GameObject>();

            foreach (var proxy in proxies)
            {
                if (proxy == null)
                {
                    continue;
                }

                ProcessProxy(avatarRoot, proxy, unpackedPrefabRoots, logs);
            }
        }

        private static void ProcessProxy(
            GameObject avatarRoot,
            ModularAvatarBoneProxy proxy,
            HashSet<GameObject> unpackedPrefabRoots,
            List<string> logs
        )
        {
            var target = proxy.target;
            var validation = target != null ? ValidateTarget(avatarRoot, target) : ValidationResult.NotInAvatar;

            if (target != null && validation == ValidationResult.Ok)
            {
                UnpackPrefabIfNeeded(proxy.gameObject, unpackedPrefabRoots, logs);

                string suffix = string.Empty;
                int i = 1;
                while (target.Find(proxy.gameObject.name + suffix) != null)
                {
                    suffix = $" ({i++})";
                }

                var proxyTransform = proxy.transform;
                proxy.gameObject.name += suffix;

                // ------------------------------------------------------------
                // 重要：
                // - attachmentMode ごとに “どの要素をワールドで保持するか” が異なります。
                // - SetParent(worldPositionStays=true) はローカルスケールに補正が入りやすく、
                //   その後に Armature のスケールを変更する処理と組み合わさると破綻しやすいです。
                //
                // ここでは、KeepWorldPose のみ worldPositionStays=true を使い、
                // KeepPosition / KeepRotation / AtRoot は worldPositionStays=false + 必要な要素のみ復元します。
                // ------------------------------------------------------------
                var worldPos = proxyTransform.position;
                var worldRot = proxyTransform.rotation;

                switch (proxy.attachmentMode)
                {
                    default:
                    case BoneProxyAttachmentMode.Unset:
                    case BoneProxyAttachmentMode.AsChildAtRoot:
                        // ローカルを基準に “親の原点” へ
                        proxyTransform.SetParent(target, worldPositionStays: false);
                        proxyTransform.localPosition = Vector3.zero;
                        proxyTransform.localRotation = Quaternion.identity;
                        break;
                    case BoneProxyAttachmentMode.AsChildKeepWorldPose:
                        // ワールド姿勢（位置・回転・スケール）を維持
                        proxyTransform.SetParent(target, worldPositionStays: true);
                        break;
                    case BoneProxyAttachmentMode.AsChildKeepPosition:
                        // ワールド位置は維持、回転は親基準
                        proxyTransform.SetParent(target, worldPositionStays: false);
                        proxyTransform.position = worldPos;
                        proxyTransform.localRotation = Quaternion.identity;
                        break;
                    case BoneProxyAttachmentMode.AsChildKeepRotation:
                        // ワールド回転は維持、位置は親の原点
                        proxyTransform.SetParent(target, worldPositionStays: false);
                        proxyTransform.localPosition = Vector3.zero;
                        proxyTransform.rotation = worldRot;
                        break;
                }

                logs?.Add(ChibiLocalization.Format("Log.MaboneProxyProcessed", ChibiChansConversionLogUtility.GetHierarchyPath(proxyTransform)));
            }
            else
            {
                logs?.Add(ChibiLocalization.Format("Log.MaboneProxySkipDetail", ChibiChansConversionLogUtility.GetHierarchyPath(proxy.transform), validation));
            }

            Object.DestroyImmediate(proxy);
        }

        private static ValidationResult ValidateTarget(GameObject avatarRoot, Transform proxyTarget)
        {
            if (avatarRoot == null || proxyTarget == null)
            {
                return ValidationResult.NotInAvatar;
            }

            var avatar = avatarRoot.transform;
            var node = proxyTarget;

            while (node != null && node != avatar)
            {
                if (node.GetComponent<ModularAvatarMergeArmature>() != null ||
                    node.GetComponent<ModularAvatarBoneProxy>() != null)
                {
                    return ValidationResult.MovingTarget;
                }

                node = node.parent;
            }

            return node == null ? ValidationResult.NotInAvatar : ValidationResult.Ok;
        }

        private static void UnpackPrefabIfNeeded(
            GameObject proxyObject,
            HashSet<GameObject> unpackedPrefabRoots,
            List<string> logs
        )
        {
            if (proxyObject == null)
            {
                return;
            }

            if (!PrefabUtility.IsPartOfPrefabInstance(proxyObject))
            {
                return;
            }

            var instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(proxyObject);
            if (instanceRoot == null)
            {
                return;
            }

            if (!unpackedPrefabRoots.Add(instanceRoot))
            {
                return;
            }

            logs?.Add(ChibiLocalization.Format("Log.MaboneProxyPrefabUnpacked", ChibiChansConversionLogUtility.GetHierarchyPath(instanceRoot.transform)));
            PrefabUtility.UnpackPrefabInstance(instanceRoot, PrefabUnpackMode.Completely, InteractionMode.UserAction);
        }
    }
}
#endif
