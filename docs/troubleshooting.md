---
title: トラブルシューティング
nav_order: 5
---

# トラブルシューティング

具体的な手順や操作方法はこちらにまとめています。  
まずは [よくある質問（FAQ）]({{ site.baseurl }}{% link faq.md %}) の短い回答を確認してから進めると早いです。

---

## まず確認すること（共通）

- VCCで CreateChibi が **Installed** になっているか
- UnityのConsoleにエラーが出ていないか
- Scene上のアバターを選んでいるか（Project上のPrefabではない）

---

## ダイアログが出た場合（文言そのまま）

### 「Hierarchy で元のアバターを 1 つ選んでください。」

- Hierarchyで **アバタールート** を1つ選択してから、もう一度ツールを開いてください。

### 「Project でおちびちゃんズの Prefab を 1 つ選んでください。」

- Projectで **おちびちゃんズ側のPrefab** を1つ指定してください。
- 自動候補が出ない場合は、手動指定で進められます（[使い方]({{ site.baseurl }}{% link usage.md %})）。

---

## おちびちゃんズPrefabの候補（プルダウン）が出ない

よくある原因:

1) おちびちゃんズ側が `Assets/夕時茶屋` 配下に入っていない  
→ `Assets/夕時茶屋` のサブフォルダ配下に置くと自動候補に出やすいです。

2) 顔メッシュの参照が特殊（ギミックで差し替え等）  
→ 手動でPrefabを指定してください。

3) キャッシュが古い / 壊れている  
→ Unityを閉じて、次のキャッシュファイルを削除してから開き直してください。  
`Library/Aramaa/CreateChibi/FaceMeshCache.v7.json`

---

## 髪・小物・眼鏡がズレる（MA Bone Proxy系）

- オプション「MA Bone Proxyで設定している髪・小物がずれる場合に合わせる」を **ON** にして再実行してください。

既知の事例が `KNOWN_ISSUES.md` にまとまっています（テスター報告ベース）。

---

## それでも直らないとき

Discordで状況を共有してください（Unityバージョン / CreateChibiバージョン / 対象アバター / Consoleログ）。  
https://discord.gg/BJ3BpVnMna
