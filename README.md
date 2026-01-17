# NDMF VRM Exporter

VRChat のアバターを VRM 1.0 形式で変換し出力する [Modular Avatar](https://modular-avatar.nadena.dev/) で使われている基盤フレームワークである [NDMF](https://ndmf.nadena.dev) に基づくプラグインです。

> [!IMPORTANT]
> NDMF VRM Exporter は [Modular Avatar](https://modular-avatar.nadena.dev/) と [lilToon](https://lilxyzw.github.io/liltoon/) の組み合わせが最も効果的になるように設計されています。また VRChat アバターにおけるブレンドシェイプの多さが VRM に変換して利用する時に処理負荷の悪影響を受けやすいので [Avatar Optimizer](https://vpm.anatawa12.com/avatar-optimizer/ja/) と併用する形での最適化を強く推奨します

* [導入と使い方](./docs~/usage.md)
* [コンポーネントの説明](./docs~/component.md)
* [出力の互換性](./docs~/compatibility.md)
* [設計思想](./docs~/design.md)
* [よくある質問](./docs~/faq.md)

NDMF VRM Exporter には以下の特徴を持っています。

* コンポーネントをつけるだけ
* VRM 1.0 形式で出力するため 0.x より互換性のある出力が可能
  * [VRC PhysBone](https://creators.vrchat.com/avatars/avatar-dynamics/physbones/) を [VRM Spring Bone](https://vrm.dev/vrm1/springbone/) に変換
    * 内部コライダーとプレーンコライダーが利用可能な [VRMC_springBone_extended_collider](https://vrm.dev/vrm1/springbone/extended_collider/) 拡張にも対応しています
  * [Unity Constraint](https://docs.unity3d.com/ja/2022.3/Manual/Constraints.html) / [VRC Constraint](https://creators.vrchat.com/avatars/avatar-dynamics/constraints/) を [VRM Constraint](https://vrm.dev/vrm1/constraint/) に変換
* [Modular Avatar](https://modular-avatar.nadena.dev/) 導入済みならそれ以外に必要なものはなし
* [lilToon](https://lilxyzw.github.io/lilToon/ja_JP/) の設定を MToon の互換設定に自動的に変換
  * テクスチャの焼き込みも自動的に行います
