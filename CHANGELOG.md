# Changelog

## [Unreleased]

## [1.0.5] - 2025-02-11

### Fixed

- [exporter] use `Graphics.ConvertTexture` instead ([#29](https://github.com/hkrn/ndmf-vrm-exporter/pull/29))
- [exporter] fix issue where multiple PB colliders were not considered ([#28](https://github.com/hkrn/ndmf-vrm-exporter/pull/28))
- [exporter] prevent retaining the `(Clone)` suffix ([#27](https://github.com/hkrn/ndmf-vrm-exporter/pull/27))

## [1.0.4] - 2025-02-06

### Fixed

- [exporter] comprehensive overhaul of texture handling and baking ([#25](https://github.com/hkrn/ndmf-vrm-exporter/pull/25))
- [exporter] modify constraint output based on `Freeze Rotation Axis` ([#24](https://github.com/hkrn/ndmf-vrm-exporter/pull/24))
- [exporter] disable emission when an emission mask is present in lilToon ([#23](https://github.com/hkrn/ndmf-vrm-exporter/pull/23))
- [exporter] fix a bug where WriteStream was called twice ([#21](https://github.com/hkrn/ndmf-vrm-exporter/pull/21))
- BlendShape の変形が正しく行われない問題を修正 ([#14](https://github.com/hkrn/ndmf-vrm-exporter/pull/14)) by @Shiokai

## [1.0.3] - 2025-02-02

### Fixed

- [exporter] overhaul of texture processing ([#18](https://github.com/hkrn/ndmf-vrm-exporter/pull/18))
- [exporter] fixes a bug baking shadow texture don't work properly ([#17](https://github.com/hkrn/ndmf-vrm-exporter/pull/17))

## [1.0.2] - 2025-01-30

### Fixed

- [exporter] fixes a bug [#4](https://github.com/hkrn/ndmf-vrm-exporter/pull/4) is not actually fixed ([#15](https://github.com/hkrn/ndmf-vrm-exporter/pull/15))

## [1.0.1] - 2025-01-29

### Fixed

- [exporter] fixes a bug `aixAxis` is not set properly ([#5](https://github.com/hkrn/ndmf-vrm-exporter/pull/5))
- [exporter] fixes a bug `_CullMode` cannot retrieve properly on lilToon ([#4](https://github.com/hkrn/ndmf-vrm-exporter/pull/4))
- [exporter] Inactive joint/constraint source transform should not be referred ([#3](https://github.com/hkrn/ndmf-vrm-exporter/pull/3))
- [exporter] The root transform should be at origin and rotation identity ([#2](https://github.com/hkrn/ndmf-vrm-exporter/pull/2))

## [1.0.0] - 2025-01-23

### Added

- Initial release

[unreleased]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.5...HEAD
[1.0.5]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.4...1.0.5
[1.0.4]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.3...1.0.4
[1.0.3]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.2...1.0.3
[1.0.2]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.1...1.0.2
[1.0.1]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.0...1.0.1
[1.0.0]: https://github.com/hkrn/ndmf-vrm-exporter/releases/tag/1.0.0
