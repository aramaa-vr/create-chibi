---
title: 更新履歴（CHANGELOG）
description: おちびちゃんズ化ツールの更新履歴。
nav_order: 9
---

# 更新履歴 (CHANGELOG)

> このページはリポジトリ直下の `CHANGELOG.md` を元にしています。

# Changelog

## [0.5.1]（2026-02-06）
### Fixed
- ドキュメント内の「Discordへ問い合わせ」導線表示を修正

### Changed
- 日本語向けのツール名表記を公式名称に統一
- バージョンを 0.5.1 に更新

## [0.5.0]（2026-02-06）
### Changed
- テストデータ環境の引っ越し
- バージョンアップ処理の自動化
- Webサイト作成

## [0.3.0]（2026-01-31）
顔メッシュ判定の安定性改善／Prefab検索キャッシュの保存先変更

### Added
- 顔メッシュの識別子を **FaceMeshSignature** として拡張（Mesh GUID/LocalId に加えて、Prefab/FBX/AssetPath 情報も保持）
- FaceMeshSignature の一致判定を追加（MeshId一致に加え、Prefab GUID/Name・FBX GUID/Name・AssetPath でも一致判定できる）

### Changed
- Prefab検索キャッシュの保存先を **EditorUserSettings** から **Library/Aramaa/OchibiChansConverterTool/FaceMeshCache.v7.json** に変更（VCS非対象のローカルキャッシュ）
- キャッシュ互換バージョンを **v1 → v7** に更新（保存項目に PrefabGuid/PrefabName/FbxGuid/FbxName/FaceMeshAssetPath を追加）
- 顔メッシュ取得処理を MeshId ベースから Signature ベースへ変更（TryGetFaceMeshId → TryGetFaceMeshSignature）

---

## [0.2.9] - 2026-01-28
### Added
- VPMパッケージ化対応 (`package.json` の追加)
- VCC配布用ZIP生成スクリプトを追加
- アバターのルートにMA Mesh Settingsがアタッチされている場合に不具合が起きないように修正（ピューマのぷまちゃん等）
- キャッシュデータの保存形式をEditorUserSettingsに変更
- 最新Verの取得ロジックをpackage.jsonから取得するように変更

---

## [0.2.8] - 2026-01-27
> **髪や小物がずれるケース向けの改善 ＋ UI整理**

### Added
- 「髪／小物がずれる場合はON」オプションを追加 (MA Bone Proxy 対応)
- 「困ったらDiscordへ（不具合報告・質問）」ボタンを追加
- ログ出力項目に「ツールver」「VRCSDK ver」を追加

### Fixed
- 衣装スケール調整時のボーン探索アルゴリズムを改善

### Changed
- 画面上の操作手順ガイドと文言をより分かりやすく整理

---

## [0.2.7] - 2026-01-25
### Added
- プルダウンから選択できない場合におちびちゃんPrefabを手動指定できる機能を追加

### Fixed
- 複製されたアバターの `PipelineManager` ID が残ってしまう問題を修正 (空に設定)

### Removed
- プラム胸のPhysBone無効化処理を廃止 (仕様変更)

---

## [0.2.6] - 2026-01-24
### Added
- テスター募集開始
