# コンポーネントの説明

[README.md に戻る](../README.md)

NDMF VRM Exporter が提供するコンポーネントは `VRM Export Description` のひとつのみです。

* コンポーネントを有効にした状態（コンポーネント名の横にチェックボックス ✅ がついてる状態）で再生
* コンポーネントの有効無効にかかわらず NDMF Console からプラットフォーム選択で `NDMF VRM Exporter` を選び Export した場合

上記のいずれかによって VRM ファイルが生成される仕組みとなっています。

> [!TIP]
> 処理の性質上 VRM ファイルの生成は最低でも数秒、場合によっては数分と時間がかかるため、時間がかかる場合はコンポーネントを無効化して NDMF Console 経由でファイル書き出しをしてください

## Metadata

VRM のメタデータに直接対応しています。詳細な情報は [モデル情報](https://vrm.dev/univrm/meta/univrm_meta/) および [VRoid Hubの利用条件とVRMライセンスについて](https://vroid.pixiv.help/hc/ja/articles/360016417013-VRoid-Hub%E3%81%AE%E5%88%A9%E7%94%A8%E6%9D%A1%E4%BB%B6%E3%81%A8VRM%E3%83%A9%E3%82%A4%E3%82%BB%E3%83%B3%E3%82%B9%E3%81%AB%E3%81%A4%E3%81%84%E3%81%A6) を参照してください。

Metadata のうち Authors と License URL が必須項目となっています。また Author は最初の要素が空文字のみで構成されないように、License URL は URL として正しい形式で設定する必要があります。

### Information

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

### Licenses

メタデータのうちアバターに対するライセンス部分を設定します。

> [!NOTE]
> 初期設定として [VRM Public License 1.0](https://vrm.dev/licenses/1.0/) が適用されます。独自のライセンスを使用したいケース [^3] を除いて設定する必要はありません

|名前|説明|必須|備考|
|---|---|---|---|
|License URL|ライセンスのURL|✅|初期値は https://vrm.dev/licenses/1.0/|
|ThirdParty Licenses|第三者のライセンス情報|||
|Other License URL|その他のライセンスのURL|||

`Uses VRM Public License` ボタンを押すと `License URL` を初期設定である VRM Public License 1.0 の URL に戻すことができます。それ以外の値は変更されません。

### Permissions

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

表示上の互換性の問題の背景として MToon においてリムライトのブレンドモードが乗算のみ、マットキャップのブレンドモードが加算のみしか利用できない制約によるものです。後者については以下で起票されています。

* [MtoonのMatcap機能拡張 #2328](https://github.com/vrm-c/UniVRM/issues/2328)

> [!TIP]
> NDMF VRM Exporter においてリムライトを無効にしていることが条件ですが、マットキャップのマスクテクスチャを使う形でマットキャップの乗算モードを擬似的に実現可能です

ただしマットキャップが「有効」かつリムライトが「無効」の場合は MToon の実装の関係でリムライトのパラメータを上書きします。詳細は「出力互換性の情報」の「材質（マテリアル）の変換」を確認してください。

## Spring Bone Options

ビルド時にのみ除外する VRC PhysBone Collider コンポーネント及び VRC PhysBone コンポーネントを設定します。

* `Excluded Spring Bone Colliders`
* `Excluded Spring Bones`

ビルド時に非表示のゲームオブジェクトがある場合は出力から除外しますが、この項目を使うことによってゲームオブジェクトを非表示に設定しなくても出力を除外することが可能になります。

## Constraint Options

ビルド時にのみ除外する VRC PhysBone Constraint コンポーネントを設定します。

* `Excluded Constraints`

利用目的は `Spring Bone Options` の `Excluded Spring Bone Colliders` および `Excluded Spring Bones` と同じです。

## glTF Extension Options

以下の設定が可能です。設定自体は 1.1.0 から導入されました。

* `Enable KHR_materials_variants`
  * マテリアルを動的に切り替えられるようにする公式の拡張である [KHR_materials_variants](https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_variants/README.md) 拡張を有効にするかどうかを設定します
  * 詳細は「材質（マテリアル）の切り替え」の章を確認してください

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

[^1]: VRM の仕様でも個人情報を含めることについては [意図していません](https://github.com/vrm-c/vrm-specification/blob/master/specification/VRMC_vrm-1.0/meta.ja.md#metacontactinformation)
[^2]: 厳密には [VRCPipelineManager](https://creators.vrchat.com/sdk/vrcpipelinemanager/) コンポーネントで管理されているブループリント ID が発行されている必要があります。これはアバターの初回アップロード後に自動的に発行されます
[^3]: これは [VN3](https://www.vn3.org) のような法務的監修を受けたライセンスとは別の完全に独自運用のライセンスを想定しています。ただし独自ライセンスの運用は法務上の相談ができる環境でなければ原則として避けるべきです
[^4]: ブレンドシェイプに直接対応するものは表情ではなく VRM の派生元である glTF におけるモーフターゲットです
[^5]: これは Avatar Optimizer に対してアニメーションクリップに含まれる変形対象のすべてのブレンドシェイプの最適化を無効にする必要があるためです。変形対象となるブレンドシェイプの数が多いほどその影響が大きくなります
[^6]: VRM の派生元である glTF の仕様として [ノード名が一意であることを求めていません](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#_node_name)。その一方で利用先アプリケーションによってはノード名が一意であることが求められる場合があるため、開発用途でなければ有効のままにしてください
[^7]: lilToon において頂点色は [輪郭線設定](https://lilxyzw.github.io/lilToon/ja_JP/advanced/outline.html) または [ファー設定](https://lilxyzw.github.io/lilToon/ja_JP/advanced/fur.html) として転用されますが、VRM の派生元である glTF では本来の目的である頂点色として使われるため、設定を解除すると意図しない色出力が発生することがあります
[^8]: VRM をサーバにアップロードして利用する場合はテクスチャ圧縮をサーバ側で実施するため NDMF VRM Exporter 側で実施する必要はありません。一方でサーバにアップロードしないかつ `KHR_textures_basisu` に対応しているアプリケーションを利用するか、開発用途の場合でこのオプションが有用になることがあります
