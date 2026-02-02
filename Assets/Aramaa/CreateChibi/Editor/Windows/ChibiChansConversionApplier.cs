#if UNITY_EDITOR
// Assets/Aramaa/CreateChibi/Editor/Windows/ChibiChansConversionApplier.cs
//
// ============================================================================
// 概要
// ============================================================================
// - Hierarchy で選択したオブジェクトを「Ctrl+D と同じ方法」で複製します
// - 複製された “新しいオブジェクト” に対して、おちびちゃんズ用の同期処理を適用します
// - 変換元（おちびちゃんズ側）Prefab を指定し、VRCAvatarDescriptor から参照（FX/Menu/Parameters/ViewPosition 等）を抽出して反映します
// - sourceChibiPrefab（変換元Prefab）を 1 つ指定するだけで実行できます
//
// ============================================================================
// 重要メモ（初心者向け）
// ============================================================================
// - このスクリプトは Editor 専用です（ビルドには含まれません）
// - Prefab を直接編集しないため、誤って元アセットを壊しにくいです
// - VRChat SDK の型は環境によって無い場合があるので、一部は反射 + SerializedObject で触ります
//
// ============================================================================
// チーム開発向けルール
// ============================================================================
// - 変更前に「どのアセット/どの階層を触るか」をコメントに残す（事故防止）
// - Editor 拡張は必ず Undo を記録する（ユーザーが戻せることが最優先）
// - Prefab アセットを勝手に更新しない（Scene 上の対象だけを変更）
// - 処理順が仕様なので、並べ替える時は README とコメントも更新する
//
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Aramaa.CreateChibi.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aramaa.CreateChibi.Editor
{
    /// <summary>
    /// 選択中アバターを Ctrl+D 相当で複製し、変換元 Prefab の設定を複製先に同期するエディターツールです。
    /// </summary>
    public static class ChibiChansConversionApplier
    {
        private const string ToolVersion = ChibiEditorConstants.ToolVersion;
        private const string LatestVersionUrl = ChibiEditorConstants.LatestVersionUrl;
        private const string SupportDiscordUrl = ChibiEditorConstants.SupportDiscordUrl;
        private const string ToolsMenuPath = ChibiEditorConstants.ToolsMenuPath;
        private const string GameObjectMenuPath = ChibiEditorConstants.GameObjectMenuPath;
        private static string ToolWindowTitle => ChibiLocalization.Get("Tool.Name");
        private static string LogWindowTitle => ChibiLocalization.Get("Tool.LogWindowTitle");

        // ------------------------------------------------------------
        // MenuItem（入口）
        // ------------------------------------------------------------
        // 仕様：
        // - Tools/Aramaa からは「選択が無くても」ウィンドウを開ける
        // - GameObject/Aramaa からは「Hierarchy の単一選択（Scene上）」がある時だけ開ける

        [MenuItem(ToolsMenuPath, priority = 2000)]
        private static void OpenFromToolsMenu()
        {
            // Tools メニューは選択が無くても起動できる。
            // ただし、Scene上のオブジェクトが選択されている場合はそれをデフォルト対象にする。
            var selected = Selection.activeGameObject;
            if (selected != null && !EditorUtility.IsPersistent(selected) && selected.scene.IsValid() && selected.scene.isLoaded)
            {
                ChibiConversionSourcePrefabWindow.Show(selected);
                return;
            }

            ChibiConversionSourcePrefabWindow.Show(null);
        }

        [MenuItem(ToolsMenuPath, validate = true)]
        private static bool ValidateOpenFromToolsMenu()
        {
            // 開くだけなので常に有効。
            return true;
        }

        [MenuItem(GameObjectMenuPath, priority = 0)]
        private static void OpenFromGameObjectMenu()
        {
            // GameObject メニューは “選択オブジェクト” を対象として起動する。
            var selected = Selection.activeGameObject;
            ChibiConversionSourcePrefabWindow.Show(selected);
        }

        [MenuItem(GameObjectMenuPath, validate = true)]
        private static bool ValidateOpenFromGameObjectMenu()
        {
            // Hierarchy（Scene）上の単一選択があるときだけ表示
            var selected = Selection.activeGameObject;
            return selected != null && !EditorUtility.IsPersistent(selected) && selected.scene.IsValid() && selected.scene.isLoaded;
        }

        #region 変換元プレハブ選択ウィンドウ（EditorWindow）

        /// <summary>
        /// 変換元（おちびちゃんズ側）の Prefab アセットを参照欄で指定し、
        /// 選択中アバターを Ctrl+D 相当で複製した上で、複製物へ変換を適用するウィンドウです。
        ///
        /// 仕様（重要）
        /// - 指定された sourceChibiPrefab（おちびちゃんズ側の Prefab）に入っている
        ///   VRCAvatarDescriptor の内部設定（FX / Expressions / ViewPosition など）を読み取り、
        ///   その値を複製先へ反映します。
        /// - Ochibichans_Addmenu は sourceChibiPrefab の内部にある想定のため、
        ///   Prefab 内のネストされた Prefab インスタンスから該当アセットを探索して追加します。
        /// </summary>
        private sealed class ChibiConversionSourcePrefabWindow : EditorWindow
        {
            // ------------------------------------------------------------
            // 見た目（ウィンドウサイズ）
            // ------------------------------------------------------------
            // 最低サイズのみ固定（内容が増える場合はスクロール対応）
            private static readonly Vector2 WindowMinSize = new Vector2(430, 510);

            // 二重起動防止：既に開いているウィンドウがあればそれを使う
            private static ChibiConversionSourcePrefabWindow _opened;

            // 二重実行防止：ボタン連打で複数回の delayCall が積まれるのを防ぐ
            private bool _applyQueued;

            private bool _showLogs;
            private bool _applyMaboneProxyProcessing;
            private Vector2 _scrollPosition;
            private bool _versionCheckRequested;
            private bool _versionCheckInProgress;
            private string _latestVersion;
            private string _versionError;
            private ChibiVersionStatus _versionStatus = ChibiVersionStatus.Unknown;

            // 変換対象（Hierarchy で選択されているアバター）
            private GameObject _sourceTarget;

            // 変換元（おちびちゃんズ側）Prefab アセット（Project 上の Prefab）
            private GameObject _sourcePrefabAsset;
            private readonly ChibiChansPrefabDropdownCache _prefabDropdownCache = new ChibiChansPrefabDropdownCache();

            /// <summary>
            /// 変換ウィンドウを表示します（既に開いていればフォーカスするだけ）。
            /// </summary>
            public static void Show(GameObject sourceTarget)
            {
                // sourceTarget は null でも良い（Tools メニューから起動できるようにする）

                // 既存ウィンドウがあるならそれを使う
                if (_opened != null)
                {
                    if (sourceTarget != null)
                    {
                        _opened._sourceTarget = sourceTarget;
                    }

                    _opened.Focus();
                    return;
                }

                // なければ作成
                var titleWithVersion = ChibiLocalization.Format("Window.TitleWithVersion", ToolWindowTitle, ToolVersion);
                var w = GetWindow<ChibiConversionSourcePrefabWindow>(utility: true, title: titleWithVersion, focus: true);
                _opened = w;

                w.minSize = WindowMinSize;
                if (sourceTarget != null)
                {
                    w._sourceTarget = sourceTarget;
                }

                // ユーザーが「何をするウィンドウか」を一目で理解できるタイトル
                w.titleContent = new GUIContent(titleWithVersion);

                w.Show();
                w.Focus();
            }

            private void OnDisable()
            {
                // 閉じられたら参照を解放（次回また開けるように）
                if (_opened == this)
                {
                    _opened = null;
                }

                ChibiChansPrefabDropdownCache.SaveCacheToDisk();
            }

            private void OnEnable()
            {
                _versionCheckRequested = false;
                _versionCheckInProgress = false;
                _latestVersion = null;
                _versionError = null;
                _versionStatus = ChibiVersionStatus.Unknown;
            }

            private void OnGUI()
            {
                UpdateWindowTitle();

                // ------------------------------------------------------------
                // このウィンドウは「元のアバター」と「おちびちゃんズ側 Prefab アセット」を指定し、
                // 「実行」ボタンで変換を行うツールです。
                //
                // 注意:
                // - OnGUI（IMGUI）中に Hierarchy を編集すると Layout が崩れやすいので、
                //   実際の処理（複製→変換）は delayCall で次の Editor ループに回します。
                // ------------------------------------------------------------

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                EditorGUILayout.Space(8);

                // 言語
                DrawLanguageSelector();

                // バージョン
                EnsureVersionCheck();
                DrawVersionStatus();

                EditorGUILayout.LabelField(ChibiLocalization.Get("Window.Description"), EditorStyles.wordWrappedLabel);

                // ------------------------------------------------------------
                // 入力欄（参照指定）
                // ------------------------------------------------------------
                DrawTargetObjectField();
                EditorGUILayout.Space(6);
                DrawSourcePrefabObjectField();
                EditorGUILayout.Space(6);
                DrawMaboneProxyToggle();

                EditorGUILayout.Space(10);

                DrawExecuteButton();
                DrawLogToggle();

                EditorGUILayout.Space(6);
                OpenDiscord();

                EditorGUILayout.Space(10);
                EditorGUILayout.EndScrollView();
            }

            private void DrawVersionStatus()
            {
                var wrappedMini = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
                var message = GetVersionStatusMessage(out var color);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    ApplyStatusColor(wrappedMini, color);
                    EditorGUILayout.LabelField(message, wrappedMini);
                }
            }

            private void UpdateWindowTitle()
            {
                var titleWithVersion = ChibiLocalization.Format("Window.TitleWithVersion", ToolWindowTitle, ToolVersion);
                if (titleContent == null || titleContent.text != titleWithVersion)
                {
                    titleContent = new GUIContent(titleWithVersion);
                }
            }

            private void DrawLanguageSelector()
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(ChibiLocalization.Get("Language.Label"), GUILayout.Width(140));
                    var currentIndex = ChibiLocalization.GetLanguageIndex();
                    var nextIndex = EditorGUILayout.Popup(currentIndex, ChibiLocalization.GetLanguageDisplayNames());
                    if (nextIndex != currentIndex)
                    {
                        ChibiLocalization.SetLanguage(ChibiLocalization.GetLanguageCodeFromIndex(nextIndex));
                    }
                }
            }

            private void EnsureVersionCheck()
            {
                if (_versionCheckRequested)
                {
                    return;
                }

                _versionCheckRequested = true;

                if (string.IsNullOrWhiteSpace(LatestVersionUrl))
                {
                    _versionError = ChibiLocalization.Get("Version.ErrorMissingUrl");
                    _versionStatus = ChibiVersionStatus.Unknown;
                    return;
                }

                _versionCheckInProgress = true;
                _versionError = null;
                _latestVersion = null;
                _versionStatus = ChibiVersionStatus.Unknown;

                ChibiVersionUtility.FetchLatestVersionAsync(LatestVersionUrl, result =>
                {
                    _versionCheckInProgress = false;
                    if (!result.Succeeded)
                    {
                        _versionError = result.Error;
                        _versionStatus = ChibiVersionStatus.Unknown;
                        return;
                    }

                    _latestVersion = result.LatestVersion;
                    _versionStatus = ChibiVersionUtility.GetVersionStatus(ToolVersion, _latestVersion);
                });
            }

            private string GetVersionStatusMessage(out Color color)
            {
                if (_versionCheckInProgress)
                {
                    color = SelectStatusColor(new Color(0.2f, 0.6f, 1f), new Color(0.1f, 0.3f, 0.8f));
                    return ChibiLocalization.Format("Version.Checking", ToolVersion);
                }

                if (!string.IsNullOrWhiteSpace(_versionError))
                {
                    color = SelectStatusColor(new Color(0.95f, 0.35f, 0.35f), new Color(0.7f, 0.15f, 0.15f));
                    return ChibiLocalization.Format("Version.CheckFailed", ToolVersion, _versionError);
                }

                if (string.IsNullOrWhiteSpace(_latestVersion))
                {
                    color = SelectStatusColor(new Color(0.7f, 0.7f, 0.7f), new Color(0.45f, 0.45f, 0.45f));
                    return ChibiLocalization.Format("Version.NoInfo", ToolVersion);
                }

                switch (_versionStatus)
                {
                    case ChibiVersionStatus.UpdateAvailable:
                        color = SelectStatusColor(new Color(1f, 0.65f, 0.2f), new Color(0.8f, 0.45f, 0.1f));
                        return ChibiLocalization.Format("Version.Available", ToolVersion, _latestVersion);
                    case ChibiVersionStatus.Ahead:
                        color = SelectStatusColor(new Color(0.4f, 0.75f, 1f), new Color(0.15f, 0.5f, 0.8f));
                        return ChibiLocalization.Format("Version.Ahead", ToolVersion, _latestVersion);
                    case ChibiVersionStatus.UpToDate:
                        color = SelectStatusColor(new Color(0.35f, 0.8f, 0.4f), new Color(0.15f, 0.55f, 0.2f));
                        return ChibiLocalization.Format("Version.UpToDate", ToolVersion, _latestVersion);
                    default:
                        color = SelectStatusColor(new Color(0.7f, 0.7f, 0.7f), new Color(0.45f, 0.45f, 0.45f));
                        return ChibiLocalization.Format("Version.Unknown", ToolVersion);
                }
            }

            private static Color SelectStatusColor(Color proSkinColor, Color lightSkinColor)
            {
                return EditorGUIUtility.isProSkin ? proSkinColor : lightSkinColor;
            }

            private static void ApplyStatusColor(GUIStyle style, Color color)
            {
                style.normal.textColor = color;
                style.hover.textColor = color;
                style.active.textColor = color;
                style.focused.textColor = color;
            }

            /// <summary>
            /// 変換対象（元のアバター）の参照欄を描画します。
            /// </summary>
            private void DrawTargetObjectField()
            {
                EditorGUILayout.LabelField(ChibiLocalization.Get("Section.SourceAvatarLabel"), EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                var nextTarget = (GameObject)EditorGUILayout.ObjectField(_sourceTarget, typeof(GameObject), allowSceneObjects: true);
                if (EditorGUI.EndChangeCheck())
                {
                    _sourceTarget = nextTarget;
                    _prefabDropdownCache.MarkNeedsRefresh();
                    _sourcePrefabAsset = null;
                }

                if (_sourceTarget == null)
                {
                    EditorGUILayout.HelpBox(ChibiLocalization.Get("Help.SelectSourceAvatar"), MessageType.Warning);
                    return;
                }

                // Project 上のアセットを入れてしまった場合は対象外（実行条件を明確化）
                if (EditorUtility.IsPersistent(_sourceTarget))
                {
                    EditorGUILayout.HelpBox(ChibiLocalization.Get("Help.SourceAvatarAssetInvalid"), MessageType.Error);
                    _sourceTarget = null;
                    _prefabDropdownCache.MarkNeedsRefresh();
                }
            }

            /// <summary>
            /// 変換元（おちびちゃんズ側 Prefab アセット）の参照欄を描画します。
            /// </summary>
            private void DrawSourcePrefabObjectField()
            {
                EditorGUILayout.LabelField(ChibiLocalization.Get("Section.TargetPrefabLabel"), EditorStyles.boldLabel);

                _prefabDropdownCache.RefreshIfNeeded(_sourceTarget);

                var hasCandidates = _prefabDropdownCache.CandidateDisplayNames.Count > 0;

                if (!hasCandidates)
                {
                    EditorGUILayout.HelpBox(ChibiLocalization.Get("Help.NoPrefabCandidates"), MessageType.Info);

                    EditorGUILayout.LabelField(ChibiLocalization.Get("Section.ManualPrefabLabel"), EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    var manualPrefab = (GameObject)EditorGUILayout.ObjectField(_sourcePrefabAsset, typeof(GameObject), allowSceneObjects: false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _sourcePrefabAsset = manualPrefab;
                    }

                    if (_sourcePrefabAsset == null)
                    {
                        EditorGUILayout.HelpBox(ChibiLocalization.Get("Help.SelectPrefabFromProject"), MessageType.Info);
                        return;
                    }

                    if (!IsPrefabAsset(_sourcePrefabAsset))
                    {
                        EditorGUILayout.HelpBox(ChibiLocalization.Get("Help.NotPrefabSelected"), MessageType.Error);
                        return;
                    }

                    EditorGUILayout.HelpBox(ChibiLocalization.Get("Help.ManualPrefabWarning"), MessageType.Warning);
                    return;
                }

                var currentIndex = _prefabDropdownCache.SelectedIndex;
                var nextIndex = EditorGUILayout.Popup(ChibiLocalization.Get("Label.CandidateList"), currentIndex, _prefabDropdownCache.CandidateDisplayNames.ToArray());
                if (nextIndex != currentIndex)
                {
                    _prefabDropdownCache.ApplySelection(nextIndex);
                }

                _sourcePrefabAsset = _prefabDropdownCache.SourcePrefabAsset;
                using (new EditorGUI.DisabledScope(true))
                {
                    _sourcePrefabAsset = (GameObject)EditorGUILayout.ObjectField(_sourcePrefabAsset, typeof(GameObject), allowSceneObjects: false);
                }

                if (_sourcePrefabAsset == null)
                {
                    EditorGUILayout.HelpBox(ChibiLocalization.Get("Help.SelectPrefabFromProject"), MessageType.Info);
                    return;
                }

                if (!IsPrefabAsset(_sourcePrefabAsset))
                {
                    EditorGUILayout.HelpBox(ChibiLocalization.Get("Help.NotPrefabSelected"), MessageType.Error);
                }
            }

            /// <summary>
            /// 実行ボタンを描画し、押されたら安全に delayCall へ処理を逃がします。
            /// </summary>
            private void DrawExecuteButton()
            {
                var canExecute =
                    !_applyQueued &&
                    _sourceTarget != null &&
                    !EditorUtility.IsPersistent(_sourceTarget) &&
                    _sourcePrefabAsset != null &&
                    IsPrefabAsset(_sourcePrefabAsset);

                using (new EditorGUI.DisabledScope(!canExecute))
                {
                    if (GUILayout.Button(ChibiLocalization.Get("Button.Execute"), GUILayout.Height(32)))
                    {
                        QueueApplyFromFields();
                    }
                }

                if (_applyQueued)
                {
                    EditorGUILayout.HelpBox(ChibiLocalization.Get("Help.ExecuteQueued"), MessageType.Info);
                }
            }

            private void DrawLogToggle()
            {
                _showLogs = EditorGUILayout.ToggleLeft(ChibiLocalization.Get("Toggle.ShowLogs"), _showLogs);
            }

            private void OpenDiscord()
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    // 左側のアイコン（Unity 標準の情報アイコン）
                    var icon = EditorGUIUtility.IconContent("console.infoicon");
                    GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));

                    using (new EditorGUILayout.VerticalScope())
                    {
                        var linkStyle = new GUIStyle(EditorStyles.linkLabel)
                        {
                            wordWrap = true
                        };

                        if (GUILayout.Button(ChibiLocalization.Get("Button.DiscordHelp"), linkStyle))
                        {
                            Application.OpenURL(SupportDiscordUrl);
                        }
                    }
                }
            }

            private void DrawMaboneProxyToggle()
            {
                _applyMaboneProxyProcessing = EditorGUILayout.ToggleLeft(ChibiLocalization.Get("Toggle.MaboneProxy"), _applyMaboneProxyProcessing);
                EditorGUILayout.HelpBox(ChibiLocalization.Get("Help.MaboneProxy"), MessageType.Info);
            }

            /// <summary>
            /// 入力欄の内容で「複製→変換」を予約します。
            /// </summary>
            private void QueueApplyFromFields()
            {
                // 二重実行防止
                if (_applyQueued)
                {
                    return;
                }

                if (_sourceTarget == null || EditorUtility.IsPersistent(_sourceTarget))
                {
                    EditorUtility.DisplayDialog(
                        ChibiLocalization.Get("Dialog.ToolTitle"),
                        ChibiLocalization.Get("Dialog.SelectSourceAvatar"),
                        ChibiLocalization.Get("Dialog.Ok")
                    );
                    return;
                }

                if (_sourcePrefabAsset == null || !IsPrefabAsset(_sourcePrefabAsset))
                {
                    EditorUtility.DisplayDialog(
                        ChibiLocalization.Get("Dialog.ToolTitle"),
                        ChibiLocalization.Get("Dialog.SelectSourcePrefab"),
                        ChibiLocalization.Get("Dialog.Ok")
                    );
                    return;
                }

                _applyQueued = true;

                var capturedSourcePrefab = _sourcePrefabAsset;
                var capturedTargets = new[] { _sourceTarget };
                var capturedApplyMaboneProxyProcessing = _applyMaboneProxyProcessing;

                Debug.Log(ChibiLocalization.Format("Log.QueuedApply", capturedTargets[0].name, capturedSourcePrefab.name));

                // ウィンドウは閉じず、次の Editor ループで実行（OnGUI中の変更を避ける）
                // 次の Editor ループで実行（Ctrl+D 相当の複製もここで行う）
                EditorApplication.delayCall += () =>
                {
                    var logs = new List<string>();

                    try
                    {
                        ChibiChansConversionPipeline.DuplicateThenApply(
                            capturedSourcePrefab,
                            capturedTargets,
                            capturedApplyMaboneProxyProcessing,
                            logs
                        );
                    }
                    catch (Exception e)
                    {
                        logs.Add(ChibiLocalization.Get("Log.Error.ExceptionOccurred"));
                        Debug.LogException(e);
                    }
                    finally
                    {
                        // 次回も使えるようにフラグを戻す
                        _applyQueued = false;

                        if (_showLogs)
                        {
                            // ログウィンドウを表示（メインウィンドウ内には表示しない）
                            ChibiConversionLogWindow.ShowLogs(LogWindowTitle, logs);
                        }

                        Repaint();
                    }
                };

                // 以降の IMGUI 処理を打ち切り（レイアウト崩壊回避）
                GUIUtility.ExitGUI();
            }

            /// <summary>
            /// 「Project 上の Prefab アセット」かどうかを判定します。
            /// </summary>
            private static bool IsPrefabAsset(GameObject go)
            {
                if (go == null)
                {
                    return false;
                }

                if (!EditorUtility.IsPersistent(go))
                {
                    // Scene上オブジェクトを除外
                    return false;
                }

                var path = AssetDatabase.GetAssetPath(go);
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                return PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab;
            }
        }

        #endregion

    }
}
#endif
