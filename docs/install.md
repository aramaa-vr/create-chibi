---
title: 導入
nav_order: 3
---

# 導入

このページでは **前提条件 → インストール → 動作確認** までをまとめます。

---

## 前提条件（ツールが想定している環境）

以下はツールの `Assets/Aramaa/OchibiChansConverterTool/package.json` に基づく目安です。

- Unity: **2022.3 系**（`unity: 2022.3` / `unityRelease: 22f1`）
- VRChat SDK: **3.10.1 以上**
- 依存パッケージ（最小バージョン）
  - `nadena.dev.modular-avatar` >= 1.15.1
  - `net.narazaka.vrchat.floor-adjuster` >= 1.1.2
  - `jp.lilxyzw.liltoon` >= 2.3.2
  - `jp.aramaa.dakochite-gimmick` >= 1.1.2

---

## 1) VCCにリポジトリを追加

1. **Add to VCC** を開きます: <https://aramaa-vr.github.io/vpm-repos/redirect.html>
2. 表示される案内に従って VCC にリポジトリを追加します

---

## 2) VCCでインストール

1. VCC で対象プロジェクトを開きます
2. Packages から **おちびちゃんズ化ツール（Ochibi-chans Converter Tool）** を **Install** します

> インストール後、Unity を開いてしばらく待つと、初回の読み込みが完了します。
{: .note }

---

## 3) 変換先（おちびちゃんズ）の配置について

候補（プルダウン）自動検出は、Project 内の **`Assets/夕時茶屋` 配下**を走査します（ツール定数 `BaseFolder`）。

> `Assets/夕時茶屋` 配下に置けない場合でも、Project から Prefab をドラッグ＆ドロップする **手動指定**で進められます。
{: .tip }

---

## 4) メニューが出るか確認

Unity のメニューから、次のいずれかが見えれば導入完了です（ツール定数 `ToolsMenuPath` / `GameObjectMenuPath`）。

- **Tools → Aramaa → おちびちゃんズ化ツール（Ochibi-chans Converter Tool）**
- **GameObject → Aramaa → おちびちゃんズ化ツール（Ochibi-chans Converter Tool）**

> メニューが出ない場合は、まず **Unity 再起動** → **Console エラー解消** → **VCCで Installed を再確認** の順で見てください。
{: .warning }

---

## 次に読む

- [クイックスタート]({{ "/quickstart/" | relative_url }})
- [使い方]({{ "/usage/" | relative_url }})
- [困ったとき]({{ "/troubleshooting/" | relative_url }})
