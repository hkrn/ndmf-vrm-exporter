# NDMF VRM Exporter

VRChat のアバターを VRM 1.0 形式で変換し出力する [Modular Avatar](https://modular-avatar.nadena.dev/) で使われている基盤フレームワークである [NDMF](https://ndmf.nadena.dev) に基づくプラグインです。

> [!IMPORTANT]
> NDMF VRM Exporter は [Modular Avatar](https://modular-avatar.nadena.dev/) と [lilToon](https://lilxyzw.github.io/liltoon/) の組み合わせが最も効果的になるように設計されています。また VRChat アバターにおけるブレンドシェイプの多さが VRM に変換して利用する時に処理負荷の悪影響を受けやすいので [Avatar Optimizer](https://vpm.anatawa12.com/avatar-optimizer/ja/) と併用する形での最適化を強く推奨します

NDMF VRM Exporter には以下の特徴を持っています。

* コンポーネントをつけるだけ
* VRM 1.0 形式で出力するため 0.x より互換性のある出力が可能
  * [VRC PhysBone](https://creators.vrchat.com/avatars/avatar-dynamics/physbones/) を [VRM Spring Bone](https://vrm.dev/vrm1/springbone/) に変換
    * 内部コライダーとプレーンコライダーが利用可能な [VRMC_springBone_extended_collider](https://vrm.dev/vrm1/springbone/extended_collider/) 拡張にも対応しています
  * [Unity Constraint](https://docs.unity3d.com/ja/2022.3/Manual/Constraints.html) / [VRC Constraint](https://creators.vrchat.com/avatars/avatar-dynamics/constraints/) を [VRM Constraint](https://vrm.dev/vrm1/constraint/) に変換
* [Modular Avatar](https://modular-avatar.nadena.dev/) 導入済みならそれ以外に必要なものはなし
* [lilToon](https://lilxyzw.github.io/lilToon/ja_JP/) の設定を MToon の互換設定に自動的に変換
  * テクスチャの焼き込みも自動的に行います

## 導入方法

まず [VRChat Creator Companion](https://vcc.docs.vrchat.com) または [ALCOM](https://vrc-get.anatawa12.com/alcom/) を事前にインストールします。その後 [レポジトリ追加のリンク](https://hkrn.github.io/vpm.html) をクリックしてレポジトリを導入します。手動で登録する場合は `https://hkrn.github.io/vpm.json` を指定します。

レポジトリ導入後は `NDMF VRM Exporter` を検索してインストールすることで利用可能になります。

## 使い方

1. インスペクタ画面から `VRC Avatar Descriptor` があるところで `Add Component` から `VRM Export Description` コンポーネントを検索し設定
2. `VRM Export Description` コンポーネント内にある `Retrieve Metadata via VRChat API` で自動設定
  * アバターが未アップロードなどの理由で手動設定する場合は `Authors` の左横の ▶️ をクリックして 🔽 にしたのち、➕ ボタンで作者名を設定
3. 再生開始
4. `Assets/NDMF VRM Exporter/${シーン名}` 内にアバター名のついた VRM ファイルが出力されていることを確認
  * シーンが未保存の状態で実行した場合はシーン名が `Untitled` になります

NDMF VRM Exporter は出力した VRM ファイルを閲覧する機能を持っていません。そのため出力された VRM ファイルを手元環境で確認する場合は [VRMファイルが使えるアプリケーションは？](https://vrm.dev/showcase) から「ビューワー」を選択して適宜アプリケーションを導入して読み込んでください。その際は必ず VRM 1.0 対応のものを利用してください（VRM 0.x のみ対応の場合は読み込めません）。

アップロードして確認する場合は [VRoid Hub](https://hub.vroid.com) の利用を推奨します。

## コンポーネントの説明

NDMF VRM Exporter が提供するコンポーネントは `VRM Export Description` のひとつのみです。コンポーネントを有効にした状態（コンポーネント名の横にチェックボックス ✅ がついてる状態）で再生すると VRM ファイルが生成される仕組みとなっています。

> [!TIP]
> 処理の性質上 VRM ファイルの生成は最低でも数秒、場合によっては数分と時間がかかるため、[AV3 Emulator](https://github.com/lyuma/Av3Emulator) などを使ってアバターをデバッグする際はコンポーネントを無効にすることを推奨します

### Metadata

VRM のメタデータに直接対応しています。詳細な情報は [モデル情報](https://vrm.dev/univrm/meta/univrm_meta/) および [VRoid Hubの利用条件とVRMライセンスについて](https://vroid.pixiv.help/hc/ja/articles/360016417013-VRoid-Hub%E3%81%AE%E5%88%A9%E7%94%A8%E6%9D%A1%E4%BB%B6%E3%81%A8VRM%E3%83%A9%E3%82%A4%E3%82%BB%E3%83%B3%E3%82%B9%E3%81%AB%E3%81%A4%E3%81%84%E3%81%A6) を参照してください。

Metadata のうち Authors と License URL が必須項目となっています。また Author は最初の要素が空文字のみで構成されないように、License URL は URL として正しい形式で設定する必要があります。

#### Information

メタデータのうち基本情報を設定します。

> [!WARNING]
> アバターのサムネイルが正方形でない場合はサムネイル画像が入りません（VRM の出力自体は可能です）

|名前|説明|必須|備考|
|---|---|---|---|
|Avatar Thumbnail|アバターのサムネイル画像||仕様上 [正方形であることが必須で、大きさが 1024x1024 であることを推奨](https://github.com/vrm-c/vrm-specification/blob/master/specification/VRMC_vrm-1.0/meta.ja.md#metathumbnailimage) している|
|Authors|作者名|✅|共同制作を想定して複数指定可能|
|Version|アバターのバージョン||記法として特に定まったものはないが、自動入力では [セマンティックバージョニング](https://semver.org) をベースにした擬似 [カレンダーバージョニング](https://calver.org) を採用している|
|Copyright Information|著作権表示||[著作権表示](https://ja.wikipedia.org/wiki/%E8%91%97%E4%BD%9C%E6%A8%A9%E8%A1%A8%E7%A4%BA) を想定|
|Contact Information|連絡先情報||問い合わせ先もしくはそれが容易に確認できる URL が望ましい。なおメールアドレスは個人情報であるとともに悪用リスクがあるので使用は極力避けるべき[^1]|
|References|参照元情報||例えば [Booth](https://booth.pm) で購入した商品を利用しているならその商品 URL の列挙が望ましい|

> [!WARNING]
> `Retrieve Metadata via VRChat API` はアバターをアップロードしないと表示されません。[^2] 使用時に値が設定されていた場合は一部を除いて上書きされるのでご注意ください

`Retrieve Metadata via VRChat API` は VRChat SDK 経由で VRChat API を利用してアップロード済みのアバターの情報からメタデータの基本情報を自動的に設定します。実行時に以下の設定を行います。

* `Avatar Thumbnail` は VRChat のアバターのサムネイルを中央揃えで切り取り 1024x1024 にリサイズして設定
  * `NDMF VRM Exporter/VRChatSDKAvatarThumbnails` にオリジナル版と加工版のふたつが保存されます
  * サムネイル加工の過程は変更することができません
* `Authors` はユーザ名を設定
  * すでに設定済みの場合は最初の要素のみ上書き
* `Version` は `{YYYY}.{MM}.{DD}+{version}` 形式
  * `{YYYY}.{MM}.{DD}` は VRChat にアバターを更新した日付が入る
  * `{version}` は VRChat 側のアバターの現在のバージョンを設定
* `Copyright Information` は `Copyright ©️ {最初にアバターをアップロードした年} {ユーザ名}` を設定
* `Contact Information` はユーザ名に対応する VRChat のリンク
  * `Enable VRChat User Link as Contact Information` のチェックを外すと自動入力されなくなります（その場合は上書きもしません）

処理の取り消しが可能でその場合は設定されません。また処理の過程でエラーが発生した場合も同様に設定されません。エラーが発生した場合はコンソールに詳細なエラーメッセージが表示されます。ただし VRChat の認証失敗のみボタンの下にメッセージが表示されます。

#### Licenses

メタデータのうちアバターに対するライセンス部分を設定します。

> [!NOTE]
> 初期設定として [VRM Public License 1.0](https://vrm.dev/licenses/1.0/) が適用されます。独自のライセンスを使用したいケース [^3] を除いて設定する必要はありません

|名前|説明|必須|備考|
|---|---|---|---|
|License URL|ライセンスのURL|✅|初期値は https://vrm.dev/licenses/1.0/|
|ThirdParty Licenses|第三者のライセンス情報|||
|Other License URL|その他のライセンスのURL|||

#### Permissions

メタデータのうちアバターに対する利用許諾の部分を設定します。

> [!WARNING]
> 初期設定として最も厳格な設定になっているため原則として設定を変更する必要はありません。これらの項目をひとつでも変更する場合は想定外のリスクが発生する可能性があります

|名前|説明|必須|
|---|---|---|
|Avatar Permission|アバター使用の許諾設定||
|Commercial Usage|商用利用の許可設定||
|Credit Notation|クレジット表記の必須設定||
|Modification|改変の許可設定||
|Allow Redistribution|再配布の許可設定||
|Allow Excessively Violent Usage|過度な暴力表現の許可設定||
|Allow Excessively Sexual Usage|過度な性的表現の許可設定||
|Allow Political or Religious Usage|政治または宗教用途に対する利用の許可設定||
|Allow Antisocial or Hate Usage|反社会もしくはヘイトに対する利用の許可設定||

## Expressions

VRM の表情設定を行います。

> [!IMPORTANT]
> 表情はブレンドシェイプと機能的によく似ていますが、同一ではありません [^4]

> [!NOTE]
> まばたきとリップシンクの表情は VRC Avatar Descriptor コンポーネントから情報を取得して自動的に設定されます。また [Avatar Optimizer](https://vpm.anatawa12.com/avatar-optimizer/ja/) を利用している場合は表情に指定されたブレンドシェイプが最適化対象から外す処理を行う関係で除去されずに残ります

### Preset

VRM のプリセットである以下の項目を設定することが可能です。

* Happy
* Angry
* Sad
* Relaxed
* Surprised

表情設定については以下の二種類が選択可能です。

* ブレンドシェイプ (`BlendShape`)
  * アバターにあるブレンドシェイプ名を指定します
  * ウェイトは 100% 固定になります
* アニメーションクリップ (`AnimationClip`)
  * アニメーションクリップを用いて最初 (0秒) のキーフレームに存在するブレンドシェイプ名とウェイト値を利用します
  * 存在しないブレンドシェイプ名を指定あるいは最初以外のキーフレームの情報は単に無視されます

> [!TIP]
> [Avatar Optimizer](https://vpm.anatawa12.com/avatar-optimizer/ja/) を利用する場合は可能な限りアニメーションクリップよりブレンドシェイプを利用するようにしてください。アニメーションクリップを用いる場合 Avatar Optimizer のブレンドシェイプに対する最適化がしにくくなります [^5]

表情の組み合わせおよび自動的に設定されることも相まって意図せずメッシュの破綻を起こす可能性があるため、その対策として VRM では表情の制御方法を提供しています。詳しい仕様については [プロシージャルのオーバーライド](https://github.com/vrm-c/vrm-specification/blob/master/specification/VRMC_vrm-1.0/expressions.ja.md#%E3%83%97%E3%83%AD%E3%82%B7%E3%83%BC%E3%82%B8%E3%83%A3%E3%83%AB%E3%81%AE%E3%82%AA%E3%83%BC%E3%83%90%E3%83%BC%E3%83%A9%E3%82%A4%E3%83%89) を確認してください。

NDMF VRM Exporter では VRM の仕様と直接対応する形で以下の表情の制御方法を提供しています。設定可能な値は `None`/`Block`/`Blend` の３つで、いずれも初期値は `None` です。

* まばたき (`Blink`)
* 視線 (`LookAt`)
* リップシンク (`Mouth`)

MMD 互換のブレンドシェイプが存在する場合は `Set Preset Expression from MMD Compatible` を利用して設定することが可能です。その場合は以下の表に基づいて設定されます。加えて表情の制御方法が全て `Block` に設定されます。

|表情名|設定先ブレンドシェイプ名|
|---|---|
|Happy|笑い|
|Angry|怒り|
|Sad|困る|
|Relaxed|なごみ|
|Surprised|びっくり|

プリセット表情の設定を全てリセットする場合は `Reset All Preset Expressions` でリセットすることができます。

### Custom

ユーザ独自の表情を設定します。

> [!WARNING]
> 表情名に非 ASCII 文字を使うと出力時に文字化けする既知の問題があるため ASCII 文字のみを使うようにしてください

> [!NOTE]
> VRM の仕様では `Custom` の表情数に制限はありませんが、VRM アニメーションを使う場合は [UniVRM の実装制約](https://github.com/vrm-c/UniVRM/blob/v0.128.1/Assets/VRM10/Runtime/Components/VrmAnimationInstance/Vrm10AnimationInstance.cs#L64-L172) により 100 個の上限があります

複数個指定可能なため追加削除と表情名の指定を行う必要はありますが、基本的な設定方法はプリセット表情と同じです。表情名をプリセット名と同じ名前で定義することも可能ですが仕様上許容されておらず、実装依存ではあるものの原則としてプリセット表情が優先されます。

## MToon Options

以下の設定が可能です。lilToon シェーダからの変換時に利用されます。

* `Enable RimLight`
* `Enable MatCap`
* `Enable Outline`
* `Enable Baking Alpha Mask Texture`

> [!WARNING]
> [TexTransTool](https://ttt.rs64.net) (TTT) の [AtlasTexture コンポーネント](https://ttt.rs64.net/docs/Reference/AtlasTexture) 使用時にマテリアルの組み合わせ次第では `Enable Baking Alpha Mask Texture` と TTT のプロパティベイクの二重焼き込みの影響で表示上の問題が発生する場合があります。その場合は `Enable Baking Alpha Mask Texture` か TTT のプロパティベイクのどちらかを無効にしてください

リムライト、マットキャップ、アウトラインのうち表示上の互換性の問題からアウトラインのみ有効となっています。

## Spring Bone Options

ビルド時にのみ除外する VRC PhysBone Collider コンポーネント及び VRC PhysBone コンポーネントを設定します。

* `Excluded Spring Bone Colliders`
* `Excluded Spring Bones`

ビルド時に非表示のゲームオブジェクトがある場合は出力から除外しますが、この項目を使うことによってゲームオブジェクトを非表示に設定しなくても出力を除外することが可能になります。

## Constraint Options

ビルド時にのみ除外する VRC PhysBone Constraint コンポーネントを設定します。

* `Excluded Constraints`

利用目的は `Spring Bone Options` の `Excluded Spring Bone Colliders` および `Excluded Spring Bones` と同じです。

## Debug Options

以下の設定が可能です。

* `Make All Node Names Unique`
  * ノード名が一意になるように名前を設定するかを指定します [^6]
* `Enable Vertex Color Output`
  * メッシュに頂点色を出力するかを設定します
  * 利用元のシェーダによっては頂点色を本来の目的とは異なる形で利用する場合があり、それが原因で意図しない色になってしまうことがあるためその場合は無効にします
* `Disable Vertex Color on lilToon`
  * lilToon からの変換時に頂点色を無効（頂点色を白色に設定）にするかを設定します [^7]
  * 頂点色が存在しない、`Enable Vertex Color Output` が無効、シェーダが lilToon ではないのいずれかに該当する場合は何もしません
* `Enable Generating glTF JSON File`
  * デバッグ目的で VRM ファイルと同じフォルダに JSON ファイルを出力するかを設定します
* `Delete Temporary Object Files`
  * 一時ファイルを出力した後に削除するかを設定します
* `KTX Tool Path`
  * テクスチャ圧縮を利用できるようにするための拡張である [KHR_texture_basisu](https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_texture_basisu/README.md) に対応したテクスチャへ変換のために使用する [KTX 変換ツール](https://github.com/KhronosGroup/KTX-Software) のパスを指定します [^8]
  * 指定して変換に成功した場合は `KHR_texture_basisu` が付与されます

## 出力互換性の情報

名前はビルド対象のアバター（ゲームオブジェクト）につけられた名前がそのまま利用されます。

ビルド時に非表示のゲームオブジェクトが存在する場合はそのノードがなかったものとして扱われます。またそのノードの子孫が存在する場合も同様に扱われます。

> [!NOTE]
> VRC PhysBone が登場する前に使われていた [Dynamic Bone](https://assetstore.unity.com/packages/tools/animation/dynamic-bone-16743) からの変換には対応していません

VRC PhysBone については VRM Spring Bone のジョイントに変換されます。ただし Immobile および Limit については VRM Spring Bone に対応する仕様が存在しないため、「動きにくくする措置」として以下で計算されます。

* Limit の場合は角度を 180 で割り、その係数を以って乗算
  * 0 の場合は Stiffness と DragForce を無効化
* Immobile の場合は Stiffness と DragForce に 1:1 の割合で加算
  * Limit がある場合は先の係数を以って乗算

VRM Spring Bone と VRC PhysBone は計算方法が異なるため結果は同一になりません。また VRM Spring Bone の仕様に存在しない以下の項目には変換に対応していません。

* `Ignore Transforms`
* `Endpoint Position`
* `Multi Child Type`
  * `Ignore` と同じ
* `Grab & Pose`

VRC PhysBone の子孫に枝分かれが存在する場合はそれぞれが独立した VRM Spring Bone として作られます[^9]。ただし VRM の仕様では枝分かれした Spring Bone の動作は [未定義で実装依存](https://github.com/vrm-c/vrm-specification/blob/master/specification/VRMC_springBone-1.0/README.ja.md#%E5%88%86%E5%B2%90%E3%81%99%E3%82%8B-springchain-%E6%9C%AA%E5%AE%9A%E7%BE%A9) となるため、動作の一貫性を重視する場合は枝分かれをしないように VRC PhysBone の再設定が必要になる場合があります。

VRC PhysBone のコライダーは以下の三種類に対応しています。

* `Capsule` (カプセル)
* `Plain` (平面)
* `Sphere` (球)

`Inside Bounds` が有効もしくは `Plain` の場合は `VRMC_springBone_extended_collider` 拡張に対応しているアプリケーションが必要となります。対応していないアプリケーションを利用した場合は前者が存在しないものとして、後者の場合は半径 10km の巨大スフィアコライダーを設定する形でそれぞれ処理されます。

Constraint または VRM Constraint が使われている場合は VRM Constraint に変換されます。またその場合は以下の三種類に対応しています。[^10]

* `AimConstraint`
  * VRM の仕様上 X/Y/Z の単一方向ベクトルのみに制約されるため、例えば斜め方向の場合変換できません
  * またアップベクトル設定の変換に対応していません
* `RotationConstraint`
* `ParentConstraint`
  * ソースノードが存在しない場合のみ
  * VRM Constraint の仕様の整合性のため専用のノードが追加され、それを参照先として利用します。

いずれも複数ソースノードを持つものについては VRM Constraint の仕様上対応できないため、最初のソースノードのみ利用されます。

`RotationConstraint` は `Freeze Rotation Axes` の結果によって変換先が変わります。

|X軸|Y軸|Z軸|変換先|
|---|---|---|---|
|✅|✅|✅|`RotationConstraint`|
|✅|❌|❌|`RollConstraint` (X軸)|
|❌|✅|❌|`RollConstraint` (Y軸)|
|❌|❌|✅|`RollConstraint` (Z軸)|
|✅|✅|❌|（変換しない）|
|❌|✅|✅|（変換しない）|
|❌|❌|❌|（変換しない）|

Unity Constraint での `Constraint Settings` および VRC Constraint での `Freeze Rotation Axes` **以外の** `Constraint Settings` と `Advanced Settings` の設定の変換は VRM に仕様に対応する機能が存在しないため対応していません。

スプリングボーンと同様に VRM Constraint と Unity/VRC Constraint は計算方法が異なるため結果は同一になりません。

lilToon シェーダが使われている場合は MToon 互換設定に変換します（MToon 未対応の環境のために `KHR_materials_unlit` も付与します）。その場合は以下の処理を行います。

* lilToon の MToon 変換と同じ方法で再設定
* 以下のテクスチャがある場合は焼き込みした上で再設定
  * メイン
  * アルファマスク
    * `MToon Options` の `Enable Baking Alpha Mask Texture` が有効の時のみ
  * 影
  * リム
    * `MToon Options` の `Enable Rim` が有効かつ乗算モードの時のみ
  * マットキャップ
    * `MToon Options` の `Enable MatCap` が有効かつ加算モードの時のみ
  * アウトライン
    * `MToon Options` の `Enable Outline` が有効の時のみ

lilToon 以外のシェーダが使われている場合は MToon の変換は行われず、glTF 準拠の最低限の設定で変換します。

## よくある質問

### VRM が出力されていない

以下のいずれかに当てはまっていないかどうかを確認してください。これらは全てコンポーネント上に事前に警告またはエラーメッセージが入ります。特に１番目は起こりやすいです。

* `VRM Export Description` コンポーネントにチェックが入ってない
* `VRM Export Description` コンポーネントが `VRC Avatar Descriptor` コンポーネントと同じ場所にいない
* `Authors` 設定が入っていない
* `License URL` 設定が URL 形式として不正

ただしごく稀に Skinned Mesh Renderer の実行時破損検出処理で NDMF のコンソールにエラーとして表示されることがあります。これは本来起こり得るエラーではないため VRM 書き出しを諦めるかプロジェクトの作り直しが必要になります。

### 出力した VRM 1.0 アバターを 0.x にダウングレードできますか？

このツールではそのような機能を持っていませんが、[VrmDowngrader](https://github.com/saturday06/VrmDowngrader) と [VRMRemaker](https://fujisunflower.fanbox.cc/posts/7313957) がありますのでそちらを使いください。ただし 0.x に変換して 1.0 に戻す形の再変換を行った場合は原則としてサポート対象外となりますのでご注意ください。

### VRM 1.0 アバターを VRChat アバターとして変換してアップロードする機能はありますか？

ありません。また実装予定もありません。

### VRM Converter for VRChat の違いはなんですか？

VRChat アバターを VRM に変換（またはその逆）するツールとして定番である [VRM Converter for VRChat](https://github.com/esperecyan/VRMConverterForVRChat) は VRM 0.x の仕様準拠で変換するのに対して NDMF VRM Exporter は VRM 1.0 の仕様準拠で変換するという点にあります。

そのため VRM Converter for VRChat では対応する VRM 0.x の仕様上どうしても変換できないカプセルコライダーおよび拡張コライダーとコンストレイントが NDMF VRM Exporter では変換することができます。その他の違いとして以下の表にまとめています[^11]。

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

### VRoid Studio でも着せ替え機能を通じて VRM 1.0 出力を扱えますが、それとどう違いますか？

VRM 1.0 を出力できる点は同じですが、出力するまでの過程が異なります。

* [VRoid Studio](https://vroid.com/studio)
  * VPM 経由で [XWear Packager](https://vroid.pixiv.help/hc/ja/articles/38903414455449) を導入
  * Unity から XAvatar 形式で出力
  * VRoid Studio で XAvatar を取り込み
  * VRoid Studio から VRM 1.0 で出力
* NDMF VRM Exporter
  * VPM 経由で NDMF VRM Exporter を導入
  * Modular Avatar で着せ替えしたアバターにコンポーネントを付与
  * Unity から再生し、出力したファイルを取得

VRoid Studio の場合は VRM の出力に XAvatar を利用する関係で VRoid Studio の導入が別途必要で、作業自体は Unity 単体で完結しません。くわえて Modular Avatar を使って着せ替えを行なっている場合 VRoid Studio 向けに XAvatar の作業行程を新たに構築する必要があるため、その構築を NDMF VRM Exporter では不要とする点が強みとなります。

XWear Packager と NDMF VRM Exporter は一緒に入れることができるため、XWear/XAvatar が必要な場合は XWear Package 経由で VRoid Studio を、Modular Avatar を使っている場合は NDMF VRM Exporter を使い分けることが可能です。

### NDMF VRM Exporter で生成した VRM ファイルを利用しようとしたらエラーになります

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

[^1]: VRM の仕様でも個人情報を含めることについては [意図していません](https://github.com/vrm-c/vrm-specification/blob/master/specification/VRMC_vrm-1.0/meta.ja.md#metacontactinformation)
[^2]: 厳密には [VRCPipelineManager](https://creators.vrchat.com/sdk/vrcpipelinemanager/) コンポーネントで管理されているブループリント ID が発行されている必要があります。これはアバターの初回アップロード後に自動的に発行されます
[^3]: これは [VN3](https://www.vn3.org) のような法務的監修を受けたライセンスとは別の完全に独自運用のライセンスを想定しています。ただし独自ライセンスの運用は法務上の相談ができる環境でなければ原則として避けるべきです
[^4]: ブレンドシェイプに直接対応するものは表情ではなく VRM の派生元である glTF におけるモーフターゲットです
[^5]: これは Avatar Optimizer に対してアニメーションクリップに含まれる変形対象のすべてのブレンドシェイプの最適化を無効にする必要があるためです。変形対象となるブレンドシェイプの数が多いほどその影響が大きくなります
[^6]: VRM の派生元である glTF の仕様として [ノード名が一意であることを求めていません](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#_node_name)。その一方で利用先アプリケーションによってはノード名が一意であることが求められる場合があるため、開発用途でなければ有効のままにしてください
[^7]: lilToon において頂点色は [輪郭線設定](https://lilxyzw.github.io/lilToon/ja_JP/advanced/outline.html) または [ファー設定](https://lilxyzw.github.io/lilToon/ja_JP/advanced/fur.html) として転用されますが、VRM の派生元である glTF では本来の目的である頂点色として使われるため、設定を解除すると意図しない色出力が発生することがあります
[^8]: VRM をサーバにアップロードして利用する場合はテクスチャ圧縮をサーバ側で実施するため NDMF VRM Exporter 側で実施する必要はありません。一方でサーバにアップロードしないかつ `KHR_textures_basisu` に対応しているアプリケーションを利用するか、開発用途の場合でこのオプションが有用になることがあります
[^9]: 1.0.5 以前は枝分かれに考慮されていなかったため、枝分かれも繋がったひとつのスプリングボーンとして出力されていました
[^10]: Position Constraint (実質的に Parent Constraint も同様) は未対応ですが https://github.com/vrm-c/vrm-specification/issues/468 で要望があがっています
[^11]: 元々の開発の動機は VRM Converter for VRChat の VRM 1.0 への未対応によるものでした。しかし仮に対応できたとしても毎回手作業が必要になるのに対して極力自動化したい動機が別にあったのと VRChat のアバター着せ替えにおける一大勢力である Modular Avatar を中心とする NDMF 圏の恩恵を最大限受けられるようにするため NDMF プラグインとして実装した経緯があります。開発にあたって [lilycalInventory](https://lilxyzw.github.io/lilycalInventory/) の思想を設計上の参考にしています
