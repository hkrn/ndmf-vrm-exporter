# 導入と使い方

[README.md に戻る](../README.md)

まず [VRChat Creator Companion](https://vcc.docs.vrchat.com) または [ALCOM](https://vrc-get.anatawa12.com/alcom/) を事前にインストールします。その後 [レポジトリ追加のリンク](https://hkrn.github.io/vpm.html) をクリックしてレポジトリを導入します。手動で登録する場合は `https://hkrn.github.io/vpm.json` を指定します。

レポジトリ導入後は `NDMF VRM Exporter` を検索してインストールすることで利用可能になります。

## 使い方その１

Unity を再生あるいは VRChat にアップロードする際にビルドと共に書き出す方法です。

1. インスペクタ画面から `VRC Avatar Descriptor` があるところで `Add Component` から `VRM Export Description` コンポーネントを検索し設定
2. `VRM Export Description` コンポーネント内にある `Retrieve Metadata via VRChat API` で自動設定
  * 詳細は「[コンポーネントの説明](./component.md)」を参照
  * アバターが未アップロードなどの理由で手動設定する場合は `Authors` の左横の ▶️ をクリックして 🔽 にしたのち、➕ ボタンで作者名を設定
3. 再生開始
4. `Assets/NDMF VRM Exporter/${シーン名}` 内にアバター名のついた VRM ファイルが出力されていることを確認
  * シーンが未保存の状態で実行した場合はシーン名が `Untitled` になります

NDMF VRM Exporter は出力した VRM ファイルを閲覧する機能を持っていません。そのため出力された VRM ファイルを手元環境で確認する場合は [VRoid Playground](https://hub.vroid.com/playground)（要 Pixiv アカウント）を利用するか、[VRMファイルが使えるアプリケーションは？](https://vrm.dev/showcase) から「ビューワー」を選択して適宜アプリケーションを導入して読み込んでください。その際は必ず VRM 1.0 対応のものを利用してください（VRM 0.x のみ対応の場合は読み込めません）。

アップロードして確認する場合は [VRoid Hub](https://hub.vroid.com) の利用を推奨します。

## 使い方その２

> [!IMPORTANT]
> NDMF 1.8 以上 (Modular Avatar では 1.13 以上が対応) が導入されている必要があります

NDMF に組み込まれているアバタープラットフォーム機能を利用する方法です。

1. インスペクタ画面から `VRC Avatar Descriptor` があるところで `Add Component` から `VRM Export Description` コンポーネントを検索し設定
2. `VRM Export Description` コンポーネント内にある `Retrieve Metadata via VRChat API` で自動設定
  * 詳細は「[コンポーネントの説明](./component.md)」を参照
  * アバターが未アップロードなどの理由で手動設定する場合は `Authors` の左横の ▶️ をクリックして 🔽 にしたのち、➕ ボタンで作者名を設定
3. `VRM Export Description` 左横のチェックボタンを外して無効化
  * 有効化したままの場合はメニューの `Tools > NDM Framework > Show NDMF Console` でウィンドウを開く必要があります
4. 無効にした際に現れる `Open NDMF Console to export VRM file` ボタンを押す
5. `Avatar platform` から `VRM 1.0 (NDMF VRM Exporter)` を選択
6. `Avatar platform` の項目のすぐ下に出てくる `Export` ボタンを押す
7. ファイルダイアログが開くので出力先を指定

こちらの方法を利用する際に「使い方その１」にないメリットとして以下が挙げられます

* コンポーネントを無効にしても機能するため「その１」と比較してビルド時間を短縮できる
* 必要なタイミングで任意の場所に対して VRM ファイルの出力が可能
* Modular Avatar を利用している場合は [プラットフォームフィルター](https://modular-avatar.nadena.dev/ja/docs/reference/platform-filter) コンポーネントによる表示の切り替えが利用可能
