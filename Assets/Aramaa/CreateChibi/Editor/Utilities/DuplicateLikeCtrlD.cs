#if UNITY_EDITOR
// Assets/Aramaa/CreateChibi/Editor/Utilities/DuplicateLikeCtrlD.cs
//
// ============================================================================
// 概要
// ============================================================================
// - Ctrl+D（Edit/Duplicate）と同じ経路で、Scene 上の GameObject を複製します
// - Unity 標準の挙動に合わせ、複製物が選択された状態を返します
//
// ============================================================================
// 重要メモ（初心者向け）
// ============================================================================
// - Project 上の Prefab アセット（Persistent）は対象外です
// - 複製失敗時に元オブジェクトへ処理が走らないよう検出します
//
// ============================================================================
// チーム開発向けルール
// ============================================================================
// - 複製経路を変更する場合は「失敗検出の基準」も合わせて更新する
// - 選択状態を変更する実装は、元の選択の復帰可否を明記する
//
// ============================================================================
//
// ユーザー提示の「複製だけで成功している」実装をベースにしています。

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Aramaa.CreateChibi.Editor.Utilities
{
    /// <summary>
    /// Edit/Duplicate と同じ挙動で Scene 上の GameObject を複製します。
    /// </summary>
    internal static class DuplicateLikeCtrlD
    {
        private const string UnityDuplicateMenuPath = "Edit/Duplicate";

        /// <summary>
        /// 指定した GameObject 群を Ctrl+D（Edit/Duplicate）と同じ経路で複製します。
        /// 返り値は「複製後に Selection になったオブジェクト」（通常は複製物）です。
        /// </summary>
        /// <param name="targets">複製したい GameObject 群（通常は Selection.gameObjects）。</param>
        /// <param name="restorePreviousSelection">
        /// true の場合、複製後に Selection を元に戻します。
        /// false の場合、Ctrl+D と同様に複製物を選択したままにします。
        /// </param>
        public static GameObject[] Duplicate(GameObject[] targets, bool restorePreviousSelection = false)
        {
            // ------------------------------------------------------------
            // 目的：
            // - Ctrl+D（Edit/Duplicate）と同じ “選択→複製→複製物が選択される” 挙動で複製したい。
            //
            // 重要な落とし穴：
            // - Unity の状況（Prefab Stage / Undo 状況 / 選択が不正 など）によっては、
            //   Edit/Duplicate が「実行成功（true）」を返しても “実際には複製されない” ことがあります。
            // - このとき Selection が “複製物に切り替わらず” 元の選択のままになる場合があります。
            //
            // そこで：
            // - 「本当に新規インスタンスが生成された」ことを InstanceID の差分で検出し、
            // - 新規が 0 件なら “複製失敗” として空配列を返します。
            //
            // ※ これにより「複製できていないのに、元オブジェクトへ変換を適用してしまう」
            //    という事故を確実に防ぎます。
            // ------------------------------------------------------------

            if (targets == null || targets.Length == 0)
            {
                return Array.Empty<GameObject>();
            }

            // Ctrl+D は Hierarchy（Scene）上の GameObject を対象にする操作です。
            // Project の Prefab アセットなど（Persistent）はここで除外します。
            var sceneTargets = targets
                .Where(go => go != null && !EditorUtility.IsPersistent(go))
                .Distinct()
                .ToArray();

            if (sceneTargets.Length == 0)
            {
                return Array.Empty<GameObject>();
            }

            // Ctrl+D は「選択対象」を複製するため、Selection を一時的に差し替えます。
            var previousSelection = Selection.objects;

            // 複製元が混ざった場合に除外できるよう、複製元の InstanceID を記録しておきます。
            var sourceIds = new HashSet<int>(sceneTargets.Select(go => go.GetInstanceID()));

            try
            {
                Selection.objects = sceneTargets;

                // --------------------------------------------------------
                // 1) まずはメニュー経由で Ctrl+D 相当を実行
                // --------------------------------------------------------
                bool ok = EditorApplication.ExecuteMenuItem(UnityDuplicateMenuPath);
                if (!ok)
                {
                    return Array.Empty<GameObject>();
                }

                // Unity 標準挙動：複製物が選択状態になる（…はずだが、状況によりそうならないことがある）
                var afterSelection = Selection.gameObjects ?? Array.Empty<GameObject>();
                var created = ExtractNewInstances(afterSelection, sourceIds);

                // --------------------------------------------------------
                // 2) もし “新規インスタンスが 1 つも取れない” 場合は、
                //    代替経路として Pasteboard 方式の複製も試します。
                //
                // - Unsupported.DuplicateGameObjectsUsingPasteboard は、Unity の内部経路で
                //   選択オブジェクトを複製するための API です（Editor 限定）。
                // - ここでも新規が取れなければ、本当に複製されていないので失敗と判断します。
                // --------------------------------------------------------
                if (created.Length == 0)
                {
                    Unsupported.DuplicateGameObjectsUsingPasteboard();
                    afterSelection = Selection.gameObjects ?? Array.Empty<GameObject>();
                    created = ExtractNewInstances(afterSelection, sourceIds);
                }

                // 新規が 0 件なら “複製失敗”
                return created.Length > 0 ? created : Array.Empty<GameObject>();

                // --------------------------------------------------------
                // ローカル関数：Selection の中から “複製元ではない（=新規）” だけを取り出す
                // --------------------------------------------------------
                static GameObject[] ExtractNewInstances(GameObject[] selection, HashSet<int> originalIds)
                {
                    if (selection == null || selection.Length == 0)
                    {
                        return Array.Empty<GameObject>();
                    }

                    // 念のため：Persistent（Projectアセット）が混ざっていたら除外
                    // 念のため：複製元（InstanceID一致）は除外
                    return selection
                        .Where(go => go != null && !EditorUtility.IsPersistent(go) && !originalIds.Contains(go.GetInstanceID()))
                        .ToArray();
                }
            }
            finally
            {
                if (restorePreviousSelection)
                {
                    Selection.objects = previousSelection;
                }
            }
        }
    }
}
#endif
