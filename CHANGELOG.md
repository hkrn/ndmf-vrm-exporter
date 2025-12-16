# Changelog

## [Unreleased]

## [1.2.2] - 2025-12-16

- [exporter] Fixed a bug with unintended dependency on VRChat SDK extension method ([#99](https://github.com/hkrn/ndmf-vrm-exporter/pull/99))

## [1.2.1] - 2025-12-06

- Revert "[chore] split NDMFVRMExporter into several multiple classes" ([#95](https://github.com/hkrn/ndmf-vrm-exporter/pull/95))

## [1.2.0] - 2025-12-05

- [exporter] adds support of NDMF platform API ([#92](https://github.com/hkrn/ndmf-vrm-exporter/pull/92))
- [chore] updates README to be more descriptive ([#89](https://github.com/hkrn/ndmf-vrm-exporter/pull/89))

## [1.1.0] - 2025-11-05

- [exporter] implements `KHR_materials_variants` ([#86](https://github.com/hkrn/ndmf-vrm-exporter/pull/86))

## [1.0.16] - 2025-10-15

### Fixed

- [exporter] Fixed to apply original values when AnimationCurve is null ([#76](https://github.com/hkrn/ndmf-vrm-exporter/pull/76))
- [exporter] Fixed to exclude root influence from offsetFromHeadBone ([#74](https://github.com/hkrn/ndmf-vrm-exporter/pull/74))

## [1.0.15] - 2025-09-21

### Fixed

- [exporter] Fixed a bug where NaN is output ([#71](https://github.com/hkrn/ndmf-vrm-exporter/pull/71))

## [1.0.14] - 2025-09-06

### Fixed

- [exporter] Fixed a bug where hidden bones are referenced ([#69](https://github.com/hkrn/ndmf-vrm-exporter/pull/69))

## [1.0.13] - 2025-08-26

### Fixed

- [exporter] Skip when mesh primitives do not exist ([#67](hhttps://github.com/hkrn/ndmf-vrm-exporter/pull/67))
- [exporter] Stricter mesh corruption processing ([#66](https://github.com/hkrn/ndmf-vrm-exporter/pull/66))
- [exporter] Improves matcap processing ([#65](https://github.com/hkrn/ndmf-vrm-exporter/pull/65))

## [1.0.12] - 2025-08-03

### Fixed

- [exporter] Fixes to convert lilToon materials without textures to MToon ([#63](https://github.com/hkrn/ndmf-vrm-exporter/pull/63))
- [asmdef] removes upper version constraint ([#62](https://github.com/hkrn/ndmf-vrm-exporter/pull/62))

## [1.0.11] - 2025-06-14

### Fixed

- [exporter] Fixed a bug where MeshRenderer conversion fails with NRE ([#59](https://github.com/hkrn/ndmf-vrm-exporter/pull/59))

## [1.0.10] - 2025-06-02

### Fixed

- [exporter] Fixes multiple bugs in material output ([#56](https://github.com/hkrn/ndmf-vrm-exporter/pull/56))

### Changed

- [exporter] Remove workaround for `ShadeToony` becoming `NaN` ([#57](https://github.com/hkrn/ndmf-vrm-exporter/pull/57))

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

[unreleased]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.2.2...HEAD
[1.2.2]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.2.1...1.2.2
[1.2.1]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.2.0...1.2.1
[1.2.0]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.1.0...1.2.0
[1.1.0]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.15...1.1.0
[1.0.16]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.15...1.0.16
[1.0.15]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.14...1.0.15
[1.0.14]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.13...1.0.14
[1.0.13]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.12...1.0.13
[1.0.12]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.11...1.0.12
[1.0.11]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.10...1.0.11
[1.0.10]: https://github.com/hkrn/ndmf-vrm-exporter/compare/1.0.9...1.0.10
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
