# よくある質問

[README.md に戻る](../README.md)

## VRChat SDK / lilToon / Avatar Optimizer は必須ですか？

いずれも利用するにあたって必須ではありませんが、入れることで変換精度及び実用性が上がるため入れることを強く推奨します。

* [VRChat SDK](https://creators.vrchat.com/sdk/)
  * VRChat API を用いてメタデータを自動入力できるようになる
  * VRChat Avatar Description に設定されている Viseme を VRM の表情設定に自動的に変換されるようになる
  * PhysBone から VRM Spring Bone および VRM Constraint への変換ができるようになる
* [lilToon](https://github.com/lilxyzw/lilToon)
  * lilToon から VRM MToon への変換ができるようになる
* [Avatar Optimizer](https://github.com/anatawa12/AvatarOptimizer/)
  * 機能的には変わらないものの、ブレンドシェイプによる VRM の肥大化を抑制できる

名前を冠している通り [NDMF](https://github.com/bdunderscore/ndmf) のみ必須で、VRChat SDK は原則 VCC 経由で導入する想定であることの関係で事実上必須扱いです。

## `Building VRM file will be skipped due to corrupted SkinnedMeshRenderer found` と出る

既知の問題でモデルによっては該当のエラーが表示されることがあります。これの直接的な原因はインデックスからの頂点あるいは頂点からのボーンに対する不正参照によるもので、そのまま出力すると読み込めないあるいは異常表示の VRM が出来上がってしまうことを防ぐための措置です。

> [!IMPORTANT]
> NDMF VRM Exporter 1.0.13 以降から破損検知処理を厳格化したので 1.0.12 以前を利用している場合はアップグレードしてください

変換元モデルに対する問題であり NDMF VRM Exporter で対処できる問題の範疇を超えるため、プロジェクトを作り直すか、エラーが出ている部分を MeshRenderer に変換するか、該当のオブジェクトを非表示にするかの対処療法でしか提示できないのが現状です。

## VRM ファイルが出力されていない

以下のいずれかに当てはまっていないかどうかを確認してください。これらは全てコンポーネント上に事前に警告またはエラーメッセージが入ります。特に１番目は起こりやすいです。

* `VRM Export Description` コンポーネントにチェックが入ってない
* `VRM Export Description` コンポーネントが `VRC Avatar Descriptor` コンポーネントと同じ場所にいない
* `Authors` 設定が入っていない
* `License URL` 設定が URL 形式として不正

ただし [Skinned Mesh Renderer](https://docs.unity3d.com/ja/2022.3/Manual/class-SkinnedMeshRenderer.html) の実行時破損検出処理で NDMF のコンソールにエラーとして表示されることがあります。詳細は「`Building VRM file will be skipped due to corrupted SkinnedMeshRenderer found` と出る」を確認してください。

## lilToon が使われているにも関わらず出力された VRM 1.0 アバターの見た目が明らかに一致しない

[VRCQuestTools](https://kurotu.github.io/VRCQuestTools) を利用して Android/iOS 向けに同時出力している場合に特有の事情によってこの問題に該当する可能性があります。お手数ですが PC 向けの方で出力をお願いします。

VRChat Android/iOS 版ではシェーダの制限により lilToon が利用できず、ToonLit もしくはその高機能版の ToonStandard が利用可能となっています。このため NDMF VRM Exporter に処理が渡される前 [^12] に VRCQuestTools によって lilToon から ToonLit または ToonStandard に自動的に変換されます。その結果 lilToon を検知できず MToon 変換が行われないため、見た目が一致しない状態で出力されます。

なお ToonLit への対応予定はなく、ToonStandard に対する変換の対応予定についても今のところありませんが、需要次第では対応する可能性があります。

## 出力した VRM 1.0 アバターを 0.x にダウングレードできますか？

NDMF VRM Exporter としてそのような機能を持っていませんが、0.x にダウングレードするツールとして [VrmDowngrader](https://github.com/saturday06/VrmDowngrader) と [VRMRemaker](https://fujisunflower.fanbox.cc/posts/7313957) がありますのでどちらかをお使いください。ただし 0.x に変換して 1.0 に戻す形の再変換を行った場合は原則としてサポート対象外となりますのでご注意ください。

## VRM 1.0 アバターを VRChat アバターとして変換してアップロードする機能はありますか？

ありません。また NDMF VRM Exporter として実装する予定もありません。

ただし方法が全くないわけでなく [VRoid Studio が VRM から XAvatar を通じて VRChat アバターへの変換に対応している](https://vroid.pixiv.help/hc/ja/articles/38728373457561-VRChat%E3%81%A7%E4%BD%BF%E3%81%86%E3%81%AB%E3%81%AF) のでそれをご利用ください。

## VRM Converter for VRChat の違いはなんですか？

VRChat アバターを VRM に変換（またはその逆）するツールとして定番である [VRM Converter for VRChat](https://github.com/esperecyan/VRMConverterForVRChat) は VRM 0.x の仕様準拠で変換するのに対して NDMF VRM Exporter は VRM 1.0 の仕様準拠で変換するという点にあります。

そのため VRM Converter for VRChat では対応する VRM 0.x の仕様上どうしても変換できないカプセルコライダーおよび拡張コライダーとコンストレイントが NDMF VRM Exporter では変換することができます。その他の違いとして以下の表にまとめています[^13]。

|項目|VRM Converter for VRChat|NDMF VRM Exporter|
|---|---|---|
|VRM 0.x への変換|✅|❌|
|VRM 0.x からの変換|✅|❌|
|VRM 1.0 への変換|❌|✅|
|VRM 1.0 からの変換|❌|❌|
|出力設定|毎回設定が必要|初回のみ|
|Modular Avatar の対応|変更のたびに [Manual bake avatar](https://modular-avatar.nadena.dev/ja/docs/manual-processing) が必要|追加設定不要|
|MToon の自動変換|❌|✅ (lilToon のみ)|
|UniVRM|必要|不要|

NDMF VRM Exporter は他ツールとの干渉を避けるように設計されているため、VRM Converter for VRChat と一緒に入れて扱うことができます。NDMF VRM Exporter は VRM 0.x を扱えず、また Modular Avatar を利用していない着せ替えには対応せず VRM から VRChat 向けアバターに変換することもできないため、その場合は VRM Converter for VRChat が出番になります。必要に応じて使い分けてください。

## VRoid Studio でも着せ替え機能を通じて VRM 1.0 出力を扱えますが、それとどう違いますか？

VRM 1.0 を出力できる点は同じですが、出力するまでの過程が異なります。

* [VRoid Studio](https://vroid.com/studio)
  * VPM 経由で [XWear Packager](https://vroid.pixiv.help/hc/ja/articles/38903414455449) を導入
  * Unity から XAvatar 形式で出力
  * VRoid Studio で XAvatar を取り込み
  * VRoid Studio から VRM 1.0 で出力
* NDMF VRM Exporter
  * VPM 経由で NDMF VRM Exporter を導入
  * Modular Avatar で着せ替えしたアバターにコンポーネントを付与
  * Unity から再生または NDMF コンソール経由で書き出し、出力したファイルを取得

VRoid Studio の場合は VRM の出力に XAvatar を利用する関係で VRoid Studio の導入が別途必要で、作業自体は Unity 単体で完結しません。一方で NDMF VRM Exporter は Unity 内で完結します。

また Modular Avatar を使って着せ替えを行なっている場合 VRoid Studio 向けに XAvatar の作業行程を新たに構築する必要がありますが、その構築を NDMF VRM Exporter では不要とする点も強みとなります。

XWear Packager と NDMF VRM Exporter は一緒に入れることができるため、XWear/XAvatar が必要な場合は XWear Package 経由で VRoid Studio を、Modular Avatar を使っている場合は NDMF VRM Exporter を使い分けることが可能です。

## NDMF VRM Exporter で生成した VRM ファイルで部分的に黒い箇所が出る

> [!IMPORTANT]
> 1.2.4 以前かつ [マテリアルバリアント](https://docs.unity3d.com/ja/2022.3/Manual/materialvariant-landingpage.html) を利用している場合に使用箇所において発生する不具合がありました。お手数ですが 1.2.5 へのアップグレードをお願いします

材質色として既定で無効に設定されているマットキャップが出力する色に依存している可能性があるため、`MToon Options` の `Enable Matcap` を有効にして再度出力すると正しく表示される可能性があります。

## NDMF VRM Exporter で生成した VRM ファイルを利用しようとしたらエラーになります

まずそのエラーメッセージが利用するにあたっての制約（例えばファイルサイズやポリゴン数上限など）によるものではないことと、ほかの VRM に対応する複数のアプリケーションで利用できるかどうかの確認が必要です。動作結果と問い合わせ先の表は以下のとおりです。

> [!NOTE]
> ほかの複数アプリで確認する場合は最低でも2種類以上で動作確認をしてください

|利用先アプリ|ほかの複数アプリ|問い合わせ先|
|---|---|---|
|✅|✅|(何もしなくてよい)|
|❌|✅|利用先アプリの問い合わせ窓口|
|✅|❌|NDMF VRM Exporter に Issue を起票（ただしこれはイレギュラーなので基本的に発生しない）|
|❌|❌|NDMF VRM Exporter に Issue を起票|

利用制約は軽量化によって解決できることがあります。軽量化についての詳細は [VRChatアバター最適化・軽量化【脱Very Poor】](https://lilxyzw.github.io/matome/ja/avatar/optimization.html) を参照してください。

## プラットフォームによる制限のためポリゴン数を減らしたいけど、どうすればよいですか？

NDMF VRM Exporter 自体はポリゴン数を減らす機能を持っていませんが、NDMF プラグインとしても提供されている [Meshia Mesh Simplification](https://github.com/RamType0/Meshia.MeshSimplification/)（詳細な使い方とコツは [こちらの記事](https://vrnavi.jp/meshia-mesh-simplification/) が詳しい）を併用することでポリゴン数を減らすことが可能です。

> [!IMPORTANT]
> ポリゴン数の削減はアバターの見た目に直接影響するため、まず Avatar Optimizer で削減を行った上でどうしてもそれだけで達成ができない場合にのみ Meshia Mesh Simplification を利用するようにしてください

具体例として [Cluster](https://cluster.mu) での VRM 1.0 は全てのメッシュで [72000 ポリゴンの上限](https://help.cluster.mu/hc/ja/articles/360029465811-%E3%82%AB%E3%82%B9%E3%82%BF%E3%83%A0%E3%82%A2%E3%83%90%E3%82%BF%E3%83%BC%E3%81%AE%E5%88%B6%E9%99%90) がありますが、Meshia Mesh Simplification の `PC-Poor-Medium-Good` プリセットを使うことによりその制約を満たすことが可能です。

## NDMF VRM Exporter に出力するときだけ表示または非表示にしたいオブジェクトがあるけどどうすればよいですか？

> [!IMPORTANT]
> Modular Avatar を導入した上でセットアップされていることが前提となります

Modular Avatar の [プラットフォームフィルター](https://modular-avatar.nadena.dev/ja/docs/reference/platform-filter) コンポーネントを表示または非表示にしたいゲームオブジェクトに付与します。このときプラットフォームを `VRM 1.0 (NDMF VRM Exporter)` にし「含める（表示）」または「除外する（非表示）」を設定します。その後に「使い方その２」の方法であるアバタープラットフォームから `VRM 1.0 (NDMF VRM Exporter)` を選択して書き出してください。

なお「使い方その１」の方法だと NDMF VRM Exporter のアバターとしてではなく、VRChat のアバターとして書き出されるためプラットフォームフィルターが機能しません。

[^12]: Avatar Optimizer 導入前提で NDMF VRM Exporter は Avatar Optimizer の「後」に実施するのに対して VRCQuestTools が Avatar Optimizer の「前」に実施するため。Avatar Optimizer 未導入の場合の扱いは未定義です
[^13]: 元々の開発の動機は VRM Converter for VRChat の VRM 1.0 への未対応によるものでした。しかし仮に対応できたとしても毎回手作業が必要になるのに対して極力自動化したい動機が別にあったのと VRChat のアバター着せ替えにおける一大勢力である Modular Avatar を中心とする NDMF 圏の恩恵を最大限受けられるようにするため NDMF プラグインとして実装した経緯があります。開発にあたって [lilycalInventory](https://lilxyzw.github.io/lilycalInventory/) の思想を設計上の参考にしています
