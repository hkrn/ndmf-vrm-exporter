# Changelog

## [Unreleased]

## [1.0.9] - 2025-05-05

### Fixed

- [exporter] Changed to use GetValueOrDefault ([#52](https://github.com/hkrn/ndmf-vrm-exporter/pull/52))

## [1.0.8] - 2025-04-23

### Aded

- [exporter] Added output of information in NDMF dialog when ShadingToony is `NaN` ([#49](https://github.com/hkrn/ndmf-vrm-exporter/pull/49))

### Fixed

- [exporter] Fixed a bug where emission textures were not being output when using lilToon ([#48](https://github.com/hkrn/ndmf-vrm-exporter/pull/48))

## [1.0.7] - 2025-03-20

### Fixed

- [exporter] Fixes a bug where source joints were included when `Multi-Child` Type was set to `Ignore` ([#43](https://github.com/hkrn/ndmf-vrm-exporter/pull/43))
- [exporter] Add vertex index corruption detection processing ([#42](https://github.com/hkrn/ndmf-vrm-exporter/pull/42))
- [exporter] Change NDMF compatible version to 1.6 or higher but less than 2.0 ([#41](https://github.com/hkrn/ndmf-vrm-exporter/pull/41))

### Fixed

## [1.0.6] - 2025-02-15

### Fixed

- [exporter] support for converting multiple PB components ([#35](https://github.com/hkrn/ndmf-vrm-exporter/pull/35))
- [exporter] process PB branches as independent segments ([#33](https://github.com/hkrn/ndmf-vrm-exporter/pull/33))
- [exporter] fix an issue here root bone was missing from VRM spring bone ([#32](https://github.com/hkrn/ndmf-vrm-exporter/pull/32))

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

[unreleased]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.9...HEAD
[1.0.9]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.8...1.0.9
[1.0.8]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.7...1.0.8
[1.0.7]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.6...1.0.7
[1.0.6]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.5...1.0.6
[1.0.5]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.4...1.0.5
[1.0.4]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.3...1.0.4
[1.0.3]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.2...1.0.3
[1.0.2]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.1...1.0.2
[1.0.1]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.0...1.0.1
[1.0.0]: https://github.com/hkrn/ndmf-vrm-exporter/releases/tag/1.0.0
