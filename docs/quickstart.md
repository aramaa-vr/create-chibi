---
title: クイックスタート
nav_order: 2
---

# クイックスタート

「最短で変換まで」だけに絞った手順です。  
より丁寧な説明は [導入]({{ "/install/" | relative_url }}) と [使い方]({{ "/usage/" | relative_url }}) を参照してください。

---

## 前提

- Unity: **2022.3 系**（package.json: `unity: 2022.3`, `unityRelease: 22f1`）
- VRC SDK: **3.10.1 以上**（package.json の `vpmDependencies`）
- 変換先（おちびちゃんズ）が Project 内に導入済み

> **候補（プルダウン）を自動検出する前提パス**は `Assets/夕時茶屋` です（ツール定数 `BaseFolder`）。  
> ここに入っていない場合は、手動で Prefab を指定して進められます。
{: .warning }

---

## 1) VCCに追加してインストール

1. **Add to VCC** を開く: <https://aramaa-vr.github.io/vpm-repos/redirect.html>
2. VCC で **おちびちゃんズ化ツール - OchibiChansConverterTool** を **Install**

---

## 2) Unityで変換する

1. Unity で変換したいアバターを Scene に配置
2. **Hierarchy** でアバター（ルート）を 1 つ選択
3. 右クリック → **Aramaa → おちびちゃんズ化ツール - OchibiChansConverterTool**
4. ウィンドウで、変換先のおちびちゃんズ Prefab を選択
   - 下のプルダウン候補から選ぶ（自動検出）
   - 見つからない場合は Project から Prefab をドラッグ＆ドロップ（手動指定）
5. 必要なら「MA Bone Proxyで…合わせる」を **ON**
6. **③ 実行（コピー→変換）** をクリック

> 変換は **Ctrl+D 相当で複製**したオブジェクトに適用されます。元アバター（選択元）は破壊しません。
{: .note }

---

## 3) 変換後に必ず確認

- Console にエラーが出ていないか
- 見た目が崩れていないか（特に髪・小物・眼鏡など）
- 変換後オブジェクトが増えているか

> 変換後は **Blueprint ID をクリア**します。アップロード時に新しい Blueprint が割り当てられる挙動は正常です。
{: .tip }

---

## 次に読む

- [導入]({{ "/install/" | relative_url }})（前提・依存関係・確認ポイント）
- [使い方]({{ "/usage/" | relative_url }})（各項目の意味・おすすめ手順）
- [トラブルシューティング]({{ "/troubleshooting/" | relative_url }})（症状別）
