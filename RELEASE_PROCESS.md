# バージョン更新作業

---

## 1. ブランチの作成

* master から develop/x.y.z ブランチを切る
* develop/x.y.z から feature/xxxxx ブランチを切る

## 2. バージョン情報の更新

* package.json の 「"version": "x.y.z",」 を変更
* package.json の 「"url": ".../x.y.z/jp.aramaa.create-chibi-x.y.z.zip?」 パスを変更
* ChibiEditorConstants.csの 「ToolVersion = "x.y.z"」 を変更
* Tools/create_vpm_zip.shの「readonly DEFAULT_VERSION="x.y.z"」を変更

## 3. コミットとプッシュ

* 修正をコミット
* ブランチをプッシュ

## 4. プルリクエストとマージ (develop/x.y.zブランチ向け)

* develop/x.y.z に対して feature/xxxxx のプルリクエストを作成
* プルリクエストのタイトルに [x.y.z] を記載
* マージ後、x.y.z のリリースと ZIP ファイル、タグを作成
    * ZIP はコマンドで作成します

## 5. VPMリポジトリの更新 (開発用)

* 以下のファイルを作成し
    * develop/redirect-ver-x.y.z-develop.html
    * develop/vpm-ver-x.y.z-develop.json
* プルリクエストを作成し、マージ
* 公開して何人かのユーザーに見てもらう

## 6. VCCでの動作確認

* VCCで更新後の動作を確認

## 7. VPMリポジトリの更新 (本番用)

* vpm.json に新しいパッケージ情報を追加
* プルリクエストを作成し、マージ

## 8. Masterブランチへのマージと告知

* master に develop/x.y.z のプルリクエストを作成し、マージ（これによりツール側でバージョンアップ告知が表示されます）
* Twitterでアップデートを告知

---

### パッケージ版に関する注意点
* VRChat SDK を導入／更新した直後に、SDK が Unity の「Audio Spatializer（音の空間化プラグイン）」設定が不正だと判定して自動で修正したため、その変更を反映するために Unity再起動が必要になって通知が出るので一旦、VPMPackageAutoInstallerの利用はしない
