---
title: よくある質問（FAQ）
nav_order: 5
---

# よくある質問（FAQ）

「まず何を疑うか」を短くまとめます。手順が必要な場合は [トラブルシューティング]({{ "/troubleshooting/" | relative_url }}) を参照してください。

---

## ツールがメニューに表示されません

- VCC で **Ochibi-chans おちびちゃんズ化ツール** が **Installed** になっているか確認
- Unity を再起動（初回インストール直後は読み込みが入ります）
- Console の赤エラーを先に解消

> メニューは次の場所に出ます（ツール定数 `ToolsMenuPath` / `GameObjectMenuPath`）。  
> - Tools → Aramaa → Ochibi-chans おちびちゃんズ化ツール  
> - GameObject → Aramaa → Ochibi-chans おちびちゃんズ化ツール
{: .note }

---

## 候補（プルダウン）に変換先が出ません

- 自動検出は **`Assets/夕時茶屋`** 配下を走査します（ツール定数 `BaseFolder`）。
- 置き場所を変えられない場合は、**手動指定（Prefabをドラッグ＆ドロップ）**で進められます。

> 自動検出のキャッシュは `Library/Aramaa/OchibiChansConverterTool/FaceMeshCache.v7.json` に保存されます。  
> うまく更新されないときは Unity を閉じてこのファイルを削除 → 再起動を試してください。
{: .tip }

---

## 変換後、アップロードすると「新規アバター」扱いになります

仕様です。

- 変換は **複製物**に適用されます（Ctrl+D相当）
- さらに複製物の **Blueprint ID をクリア**します（元アバターのIDを引き継がない）

> 「元のアバターを壊さない・IDを汚さない」ための安全策です。
{: .note }

---

## 髪・小物・眼鏡がズレます

- 「**MA Bone Proxyで設定している髪・小物がずれる場合に合わせる**」を **ON** にして再実行してください。

> ONにすると、複製後に MA Bone Proxy の処理を行い、ずれを軽減します（ツール内ヘルプ）。
{: .tip }

---

## マテリアルがピンク（シェーダーが見つからない）になります

依存パッケージが未導入／未読み込みの可能性があります。

- ツールは `jp.lilxyzw.liltoon`（>=2.3.2）を依存に持ちます（package.json の `vpmDependencies`）。
- VCC の Packages で lilToon が導入済みか確認し、Unity を再起動してください。

---

## どのアバターが対応しているかわかりません

- [対応一覧]({{ "/support/" | relative_url }}) を確認してください。

---

## Discordに相談するとき、何を貼ればいいですか？

- Unity / VRC SDK / ツールバージョン
- 変換元アバター名 / 変換先おちびちゃんズ名
- Console のエラー
- （可能なら）ツールの **ログウィンドウ**（「ログを表示する」をON）

