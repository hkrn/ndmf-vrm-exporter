# 出力の互換性

[README.md に戻る](../README.md)

名前はビルド対象のアバター（ゲームオブジェクト）につけられた名前がそのまま利用されます。

ビルド時に非表示のゲームオブジェクトが存在する場合はそのノードがなかったものとして扱われます。またそのノードの子孫が存在する場合も同様に扱われます。

## Spring Bone の変換

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
* `Grab & Pose`

> [!TIP]
> 枝分かれが生成される場合スプリングボーン名に `.${番号}` が末尾に付与されます。番号は 1 からはじまり、たとえばスプリングボーン名が `SB` で 2 つ存在する場合は `SB.1` と `SB.2` になります。

VRC PhysBone の子孫に枝分かれが存在する場合は `Multi-Child Type` に基づき以下の対応が行われます。

* `Ignore`
  * 分岐元が含まれないそれぞれ独立した VRM Spring Bone のジョイント集合が作られます [^9]
* `First`
  * 分岐元が含まれる VRM Spring Bone のジョイント集合として作られます
  * VRM の仕様では分岐が含まれる Spring Bone の動作は [未定義で実装依存](https://github.com/vrm-c/vrm-specification/blob/master/specification/VRMC_springBone-1.0/README.ja.md#%E5%88%86%E5%B2%90%E3%81%99%E3%82%8B-springchain-%E6%9C%AA%E5%AE%9A%E7%BE%A9) のため動作の一貫性が取れない可能性があります
* `Average`
  * 同等の実装ができないため First と同じ扱いで処理します

VRC PhysBone のコライダーは以下の三種類に対応しています。

* `Capsule` (カプセル)
* `Plain` (平面)
* `Sphere` (球)

`Inside Bounds` が有効もしくは `Plain` の場合は `VRMC_springBone_extended_collider` 拡張に対応しているアプリケーションが必要となります。対応していないアプリケーションを利用した場合は前者が存在しないものとして、後者の場合は半径 10km の巨大スフィアコライダーを設定する形でそれぞれ処理されます。

## Constraint の変換

Constraint または VRC Constraint が使われている場合は VRM Constraint に変換されます。またその場合は以下の三種類に対応しています。[^10] 変換元に複数の Constraint または VRC Constraint が存在する場合は最初のひとつのみが変換されます。[^11]

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

## 材質（マテリアル）の変換

lilToon シェーダが使われている場合は MToon 互換設定に変換します（MToon 未対応の環境のために `KHR_materials_unlit` も付与します）。その場合は以下の処理を行います。

* lilToon の MToon 変換と基本的に同じ方法で再設定
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

`Enable MatCap` が有効かつ `Enable Rim` が **無効** の場合（有効の場合はリムの設定を優先するため行わない）は MToon のマットキャップの計算がリムライトに依存している都合上、以下の追加設定が行われます。

* リムライトの係数を 1.0 に設定
* リムライトの乗算テクスチャにマットキャップのマスクテクスチャを割り当て

lilToon 以外のシェーダが使われている場合は MToon の変換は行われず、glTF 準拠の最低限の設定で変換します。

## 材質（マテリアル）の切り替え

材質（マテリアル）の切り替えについては [lilycalInventory](https://lilxyzw.github.io/lilycalInventory/) と [Modular Avatar](https://modular-avatar.nadena.dev) のコンポーネント設定に対応しており、両方設定している場合は両方とも適用されます。いずれの場合も公式の glTF 拡張である [KHR_materials_variants](https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_variants) に変換します。

> [!WARNING]
> 切り替えに必要なマテリアル数及びテクスチャ数が増加する関係でその分出力される VRM のファイルサイズが大きくなるため、切り替えが不要であればコンポーネント設定の `glTF Extension Options` 内にある `Enable KHR_materials_variants` を無効にしてください。また `KHR_materials_variants` に対応しているかどうかは読み込み先アプリケーションに依存します

### lilycalInventory を利用している場合

lilycalInventory の [LI CostumeChanger](https://lilxyzw.github.io/lilycalInventory/ja/docs/components/costumechanger.html) コンポーネントのうち「マテリアルの置き換え」を使っている場合に適用されます。

* 名前は「メニュー・パラメーター名」とコスチューム側の「メニュー名」を利用
  * 「メニューの親フォルダ」が設定されている場合はその名前を再帰的にたどります
    * 「コスチューム」にある方と両方設定されている場合は「コスチューム」の方が優先されます
    * 「メニューのオーバーライド」は使用されません
    * 親フォルダがひとつでも無効化されている場合はスキップされます
  * `${メニューの親フォルダ名}/${メニュー・パラメーター名}/${メニュー名}` の法則で名前が設定されます
    * メニューの親フォルダ名は親フォルダの数だけ `/` が追加されます
  * 例えば「メニューの親フォルダ」の設定なしで「メニュー・パラメーター名」が「衣装」で「メニュー名」が「派生」の場合は `衣装/派生` になります
* 「メッシュ」と「置き換え先」は VRM に対応するものと直接マッピング
  * 置き換え先が `None` の場合は元のマテリアルを参照します
* それ以外の項目は全て無視される

> [!NOTE]
> [LI AutoDresser](https://lilxyzw.github.io/lilycalInventory/ja/docs/components/autodresser.html) コンポーネントは内部的に LI CostumeChanger に変換されますが、こちらは NDMF VRM Exporter としてサポート対象外です

`LI CostumeChanger` がもつ機能であるオブジェクトのオンオフ、材質のプロパティ操作、アニメーション再生、ブレンドシェイプの切り替えその他諸々には対応していません。それらが設定されている場合でも全て無視されます。

LI CostumeChanger コンポーネントをを無効化すると生成されなくなります。

### Modular Avatar を利用している場合

以下の条件を **全て** 満たしている時に変換されます。もし全て満たしているにもかかわらず変換されていない場合は非対応条件に当てはまってないかと Unity の Console 画面を確認してください。

* Modular Avatar 1.13 以上がインストールされていること
  * これは Modular Avatar 1.13 に導入された API に依存しているためです
* [Material Setter](https://modular-avatar.nadena.dev/dev/ja/docs/reference/reaction/material-setter) または [Material Swap](https://modular-avatar.nadena.dev/dev/ja/docs/reference/reaction/material-swap) コンポーネントを利用していること
* [Menu Installer](https://modular-avatar.nadena.dev/dev/ja/docs/reference/menu-installer) と [Menu Item](https://modular-avatar.nadena.dev/dev/ja/docs/reference/menu-item) が両方とも有効かつ同じコンポーネント内に設定されていること
* Menu Item コンポーネントに Material Setter または Material Swap コンポーネントが紐づけられていること
  * サブメニューに対応していますが、2階層以上のネストされたサブメニューには対応していません

名前はサブメニューが設定されている場合に lilycalInventory と同じよう `/` で区切られます。

Menu Installer または Menu Item コンポーネントを無効化することで生成されなくなります。

[^9]: 1.0.5 以前は枝分かれに考慮されていなかったため、枝分かれも繋がったひとつのスプリングボーンとして出力されていました
[^10]: Position Constraint (実質的に Parent Constraint も同様) は未対応ですが https://github.com/vrm-c/vrm-specification/issues/468 で要望があがっています
[^11]: VRM Constraint が glTF のノードの拡張として実装されており、ひとつのノード（＝ゲームオブジェクト）につきひとつの VRM Constraint しか持つことができないためです
