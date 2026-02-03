# おちびちゃんズ化ツール create-chibi 使い方ガイド

> Unity初心者のVRChatユーザー向けに、導入から実行までをやさしくまとめた説明書です。

## このツールでできること

- **ワンクリック変換**：Hierarchyでアバターを右クリックするだけ。
- **依存パッケージの自動導入**：Modular Avatar / lilToon などが自動で入ります。
- **Unity初心者でも安心**：どこをクリックするか、手順で説明します。

---

## 準備するもの

- Unity（VRChat推奨バージョン）
- VCC（VRChat Creator Companion）
- 対応アバター（対応一覧は配布ページに記載）
- おちびちゃんズ本体（BOOTHで購入・導入済み）

> 💡 先に「VRChatアバターのアップロード」を一度試しておくと安心です。

---

## インストール手順（VCC）

1. **[Add to VCC](https://aramaa-vr.github.io/vpm-repos/redirect.html)** をクリックして、VCCへリポジトリを追加します。
2. VCCの **Settings → Packages → Installed Repositories** で「aramaa」が有効になっていることを確認します。
3. アバタープロジェクトの **Manage Project** から「おちびちゃんズ化ツール create-chibi」をインストールします。

> ⚠️ 依存パッケージは自動で入るので、基本的に追加作業は不要です。

---

## 使い方（超シンプル）

1. **Hierarchyで変換元アバターを右クリック**
2. **Aramaa → Ochibi-chans おちびちゃんズ化ツール** を選択
3. **プルダウン**から変換先のおちびちゃんズを選ぶ
4. **実行（コピー→変換）** をクリック

完了したら、シーン内に「おちびちゃんズ化されたアバター」が追加されます。

---

## 初心者向けチェックリスト

- ✅ アバターはUnity上で正しく表示されている？
- ✅ VCCで依存パッケージのエラーが出ていない？
- ✅ 変換後のアバターを「Play」せずに確認している？
- ✅ 変換後は必ず保存してからアップロードしている？

---

## よくある質問

### ツールがメニューに表示されません
- VCCでcreate-chibiがプロジェクトにインストールされているか確認してください。
- Unityを再起動すると表示されることもあります。

### 変換後にマテリアルが変わったように見えます
- lilToonやModular Avatarの設定が正しく読み込まれているか確認してください。
- 依存パッケージの導入状況もチェックしましょう。

### アップロードしてもVRChatで表示が崩れます
- 変換後のアバターで「ビルド & テスト」を行い、Consoleのエラーを確認しましょう。
- エラーが残っている場合は解決してからアップロードしてください。

---

## 公式リンク

- [BOOTH配布ページ](https://aramaa.booth.pm/items/7906711)
- [Add to VCC](https://aramaa-vr.github.io/vpm-repos/redirect.html)
- [Discordサポート](https://discord.gg/BJ3BpVnMna)
