// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using nadena.dev.ndmf;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

#if NVE_HAS_VRCHAT_AVATAR_SDK
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    internal sealed class VrmRootExporter
    {
        private static readonly string VrmcSpringBoneExtendedCollider = "VRMC_springBone_extended_collider";
        private static readonly string VrmcSpringBoneLimit = "VRMC_springBone_limit";
        private static readonly int PropertyMainTexScrollRotate = Shader.PropertyToID("_MainTex_ScrollRotate");
        private static readonly int PropertyUseShadow = Shader.PropertyToID("_UseShadow");
        private static readonly int PropertyShadowBorder = Shader.PropertyToID("_ShadowBorder");
        private static readonly int PropertyShadowBlur = Shader.PropertyToID("_ShadowBlur");
        private static readonly int PropertyShadowMainStrength = Shader.PropertyToID("_ShadowMainStrength");
        private static readonly int PropertyShadowColor = Shader.PropertyToID("_ShadowColor");
        private static readonly int PropertyShadowStrength = Shader.PropertyToID("_ShadowStrength");
        private static readonly int PropertyUseRim = Shader.PropertyToID("_UseRim");
        private static readonly int PropertyRimBorder = Shader.PropertyToID("_RimBorder");
        private static readonly int PropertyRimBlur = Shader.PropertyToID("_RimBlur");
        private static readonly int PropertyRimFresnelPower = Shader.PropertyToID("_RimFresnelPower");
        private static readonly int PropertyRimColor = Shader.PropertyToID("_RimColor");
        private static readonly int PropertyRimBlendMode = Shader.PropertyToID("_RimBlendMode");
        private static readonly int PropertyUseMatCap = Shader.PropertyToID("_UseMatCap");
        private static readonly int PropertyMatCapBlendMode = Shader.PropertyToID("_MatCapBlendMode");
        private static readonly int PropertyOutlineWidth = Shader.PropertyToID("_OutlineWidth");
        private static readonly int PropertyOutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int PropertyZWrite = Shader.PropertyToID("_ZWrite");
        private const float FarDistance = 10000.0f;

        public VrmRootExporter(GameObject gameObject, IAssetSaver assetSaver,
            IDictionary<Transform, gltf.ObjectID> transformNodeIDs,
            IDictionary<string, (gltf.ObjectID, int)> allMorphTargets, ISet<string> extensionUsed)
        {
            _gameObject = gameObject;
            _assetSaver = assetSaver;
            _allMorphTargets = ImmutableDictionary.CreateRange(allMorphTargets);
            _transformNodeIDs = ImmutableDictionary.CreateRange(transformNodeIDs);
            _extensionUsed = extensionUsed;
        }

        public vrm.core.Core ExportCore(gltf.ObjectID thumbnailImage)
        {
            var vrmRoot = new vrm.core.Core();
            ExportHumanoidBone(ref vrmRoot);
            ExportMeta(thumbnailImage, ref vrmRoot);
            ExportExpression(ref vrmRoot);
            ExportLookAt(ref vrmRoot);
            return vrmRoot;
        }

        public vrm.sb.SpringBone ExportSpringBone()
        {
            var component = _gameObject.GetComponent<NdmfVrmExporterComponent>();
            IList<vrm.sb.Collider> colliders = new List<vrm.sb.Collider>();
            IList<vrm.sb.ColliderGroup> colliderGroups = new List<vrm.sb.ColliderGroup>();
            IList<vrm.sb.Spring> springs = new List<vrm.sb.Spring>();

#if NVE_HAS_VRCHAT_AVATAR_SDK
            IList<VRCPhysBoneColliderBase> pbColliders = new List<VRCPhysBoneColliderBase>();
            foreach (var (transform, _) in _transformNodeIDs)
            {
                if (!transform.gameObject.activeInHierarchy ||
                    component.excludedSpringBoneColliderTransforms.Contains(transform) ||
                    !transform.TryGetComponent<VRCPhysBoneCollider>(out _))
                {
                    continue;
                }

                var innerColliders = transform.GetComponents<VRCPhysBoneCollider>();
                foreach (var innerCollider in innerColliders!)
                {
                    ConvertBoneCollider(innerCollider, ref pbColliders, ref colliders);
                }
            }

            var immutablePbColliders = ImmutableList.CreateRange(pbColliders);
            foreach (var (transform, _) in _transformNodeIDs)
            {
                if (!transform.gameObject.activeInHierarchy ||
                    component.excludedSpringBoneTransforms.Contains(transform) ||
                    !transform.TryGetComponent<VRCPhysBone>(out _))
                {
                    continue;
                }

                var bones = transform.GetComponents<VRCPhysBone>();
                foreach (var bone in bones!)
                {
                    ConvertColliderGroup(bone, immutablePbColliders, ref colliderGroups);
                }
            }

            var immutableColliderGroups = ImmutableList.CreateRange(colliderGroups);
            foreach (var (transform, _) in _transformNodeIDs)
            {
                if (!transform.gameObject.activeInHierarchy ||
                    component.excludedSpringBoneTransforms.Contains(transform) ||
                    !transform.TryGetComponent<VRCPhysBone>(out _))
                {
                    continue;
                }

                var bones = transform.GetComponents<VRCPhysBone>();
                foreach (var bone in bones!)
                {
                    ConvertSpringBone(bone, component, immutablePbColliders, immutableColliderGroups, ref springs);
                }
            }
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

            var vrmSpringBone = new vrm.sb.SpringBone
            {
                Colliders = colliders.Count > 0 ? colliders : null,
                ColliderGroups = colliderGroups.Count > 0 ? colliderGroups : null,
                Springs = springs.Count > 0 ? springs : null,
            };
            return vrmSpringBone;
        }

        public vrm.constraint.NodeConstraint? ExportNodeConstraint(Transform node, gltf.ObjectID immobileNodeID)
        {
            vrm.constraint.NodeConstraint? vrmNodeConstraint = null;
            var sourceID = gltf.ObjectID.Null;
            float weight;
            if (node.TryGetComponent<VRCAimConstraint>(out var vrcAimConstraint))
            {
                if (vrcAimConstraint.IsActive)
                {
                    var numSources = vrcAimConstraint.Sources.Count;
                    if (numSources >= 1)
                    {
                        if (numSources > 1)
                        {
                            Debug.LogWarning($"Constraint with multiple sources is not supported in {node.name}");
                        }

                        var constraintSource = vrcAimConstraint.Sources.First();
                        var nodeID = FindTransformNodeID(constraintSource.SourceTransform);
                        if (nodeID.HasValue)
                        {
                            sourceID = nodeID.Value;
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"Constraint source {constraintSource.SourceTransform} not found due to inactive");
                        }

                        weight = vrcAimConstraint.GlobalWeight * constraintSource.Weight;
                    }
                    else
                    {
                        sourceID = immobileNodeID;
                        weight = vrcAimConstraint.GlobalWeight;
                    }

                    if (sourceID.IsNull)
                    {
                        return null;
                    }

                    if (TryParseAimAxis(vrcAimConstraint.AimAxis, out var aimAxis))
                    {
                        vrmNodeConstraint = new vrm.constraint.NodeConstraint
                        {
                            Constraint = new vrm.constraint.Constraint
                            {
                                Aim = new vrm.constraint.AimConstraint
                                {
                                    AimAxis = aimAxis,
                                    Source = sourceID,
                                    Weight = weight,
                                }
                            }
                        };
                    }
                    else
                    {
                        Debug.LogWarning($"Aim axis cannot be exported due to unsupported axis: {node.name}");
                    }
                }
                else
                {
                    Debug.Log($"VRCAimConstraint {node.name} is not active");
                }
            }
            else if (node.TryGetComponent<AimConstraint>(out var aimConstraint) && aimConstraint.constraintActive)
            {
                if (aimConstraint.constraintActive)
                {
                    var numSources = aimConstraint.sourceCount;
                    if (numSources >= 1)
                    {
                        if (numSources > 1)
                        {
                            Debug.LogWarning($"Constraint with multiple sources is not supported in {node.name}");
                        }

                        var constraintSource = aimConstraint.GetSource(0);
                        var nodeID = FindTransformNodeID(constraintSource.sourceTransform);
                        if (nodeID.HasValue)
                        {
                            sourceID = nodeID.Value;
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"Constraint source {constraintSource.sourceTransform} not found due to inactive");
                            return null;
                        }

                        weight = aimConstraint.weight * constraintSource.weight;
                    }
                    else
                    {
                        sourceID = immobileNodeID;
                        weight = aimConstraint.weight;
                    }

                    if (sourceID.IsNull)
                    {
                        return null;
                    }

                    if (TryParseAimAxis(aimConstraint.aimVector, out var aimAxis))
                    {
                        vrmNodeConstraint = new vrm.constraint.NodeConstraint
                        {
                            Constraint = new vrm.constraint.Constraint
                            {
                                Aim = new vrm.constraint.AimConstraint
                                {
                                    AimAxis = aimAxis,
                                    Source = sourceID,
                                    Weight = weight,
                                }
                            }
                        };
                    }
                    else
                    {
                        Debug.LogWarning($"Aim axis cannot be exported due to unsupported axis: {node.name}");
                    }
                }
                else
                {
                    Debug.Log($"AimConstraint {node.name} is not active");
                }
            }
#if NVE_HAS_VRCHAT_AVATAR_SDK
            else if (node.TryGetComponent<VRCRotationConstraint>(out var vrcRotationConstraint))
            {
                if (vrcRotationConstraint.IsActive)
                {
                    var numSources = vrcRotationConstraint.Sources.Count;
                    if (numSources >= 1)
                    {
                        if (numSources > 1)
                        {
                            Debug.LogWarning($"Constraint with multiple sources is not supported in {node.name}");
                        }

                        var constraintSource = vrcRotationConstraint.Sources.First();
                        var nodeID = FindTransformNodeID(constraintSource.SourceTransform);
                        if (nodeID.HasValue)
                        {
                            sourceID = nodeID.Value;
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"Constraint source {constraintSource.SourceTransform} not found due to inactive");
                        }

                        weight = vrcRotationConstraint.GlobalWeight * constraintSource.Weight;
                    }
                    else
                    {
                        weight = vrcRotationConstraint.GlobalWeight;
                    }

                    if (sourceID.IsNull)
                    {
                        Debug.LogWarning($"VRCRotationConstraint {node.name} has no source");
                        return null;
                    }

                    vrm.constraint.Constraint constraint;
                    switch (vrcRotationConstraint.AffectsRotationX, vrcRotationConstraint.AffectsRotationY,
                        vrcRotationConstraint.AffectsRotationZ)
                    {
                        case (true, true, true):
                        {
                            constraint = new vrm.constraint.Constraint
                            {
                                Rotation = new vrm.constraint.RotationConstraint
                                {
                                    Source = sourceID,
                                    Weight = weight,
                                }
                            };
                            break;
                        }
                        case (true, false, false):
                        case (false, true, false):
                        case (false, false, true):
                        {
                            var rollAxis = (vrcRotationConstraint.AffectsRotationX,
                                    vrcRotationConstraint.AffectsRotationY,
                                    vrcRotationConstraint.AffectsRotationZ) switch
                                {
                                    (true, false, false) => "X",
                                    (false, true, false) => "Y",
                                    (false, false, true) => "Z",
                                    _ => throw new ArgumentOutOfRangeException(),
                                };
                            constraint = new vrm.constraint.Constraint
                            {
                                Roll = new vrm.constraint.RollConstraint
                                {
                                    RollAxis = rollAxis,
                                    Source = sourceID,
                                    Weight = weight,
                                }
                            };
                            break;
                        }
                        default:
                            Debug.LogWarning(
                                $"VRCRotationConstraint {node.name} is not converted due to unsupported freeze axes pattern");
                            return null;
                    }

                    vrmNodeConstraint = new vrm.constraint.NodeConstraint
                    {
                        Constraint = constraint,
                    };
                }
                else
                {
                    Debug.Log($"VRCRotationConstraint {node.name} is not active");
                }
            }
#endif // NVE_HAS_VRCHAT_AVATAR_SDK
            else if (node.TryGetComponent<RotationConstraint>(out var rotationConstraint) &&
                     rotationConstraint.constraintActive)
            {
                if (rotationConstraint.constraintActive)
                {
                    var numSources = rotationConstraint.sourceCount;
                    if (numSources >= 1)
                    {
                        if (numSources > 1)
                        {
                            Debug.LogWarning($"Constraint with multiple sources is not supported in {node.name}");
                        }

                        var constraintSource = rotationConstraint.GetSource(0);
                        var nodeID = FindTransformNodeID(constraintSource.sourceTransform);
                        if (nodeID.HasValue)
                        {
                            sourceID = nodeID.Value;
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"Constraint source {constraintSource.sourceTransform} not found due to inactive");
                        }

                        weight = rotationConstraint.weight * constraintSource.weight;
                    }
                    else
                    {
                        weight = rotationConstraint.weight;
                    }

                    if (sourceID.IsNull)
                    {
                        Debug.LogWarning($"RotationConstraint {node.name} has no source");
                        return null;
                    }

                    vrm.constraint.Constraint constraint;
                    switch (rotationConstraint.rotationAxis)
                    {
                        case Axis.X | Axis.Y | Axis.Z:
                        {
                            constraint = new vrm.constraint.Constraint
                            {
                                Rotation = new vrm.constraint.RotationConstraint
                                {
                                    Source = sourceID,
                                    Weight = weight,
                                }
                            };
                            break;
                        }
                        case Axis.X:
                        case Axis.Y:
                        case Axis.Z:
                        {
                            var rollAxis = rotationConstraint.rotationAxis switch
                            {
                                Axis.X => "X",
                                Axis.Y => "Y",
                                Axis.Z => "Z",
                                _ => throw new ArgumentOutOfRangeException(),
                            };
                            constraint = new vrm.constraint.Constraint
                            {
                                Roll = new vrm.constraint.RollConstraint
                                {
                                    RollAxis = rollAxis,
                                    Source = sourceID,
                                    Weight = weight,
                                }
                            };
                            break;
                        }
                        case Axis.None:
                        default:
                            Debug.LogWarning(
                                $"RotationConstraint {node.name} is not converted due to unsupported freeze axes pattern");
                            return null;
                    }

                    vrmNodeConstraint = new vrm.constraint.NodeConstraint
                    {
                        Constraint = constraint,
                    };
                }
                else
                {
                    Debug.Log($"RotationConstraint {node.name} is not active");
                }
            }
#if NVE_HAS_VRCHAT_AVATAR_SDK
            else if ((node.TryGetComponent<VRCParentConstraint>(out var vrcParentConstraint) &&
                      vrcParentConstraint.Sources.Count == 0) ||
                     (node.TryGetComponent<ParentConstraint>(out var parentConstraint) &&
                      parentConstraint.sourceCount == 0))
            {
                vrmNodeConstraint = new vrm.constraint.NodeConstraint
                {
                    Constraint = new vrm.constraint.Constraint
                    {
                        Rotation = new vrm.constraint.RotationConstraint
                        {
                            Source = immobileNodeID,
                        }
                    }
                };
            }
            else if (node.TryGetComponent<VRCConstraintBase>(out _))
            {
                Debug.LogWarning($"VRC constraint is not supported in {node.name}");
            }
#endif // NVE_HAS_VRCHAT_AVATAR_SDK
            else if (node.TryGetComponent<IConstraint>(out _))
            {
                Debug.LogWarning($"Constraint is not supported in {node.name}");
            }

            return vrmNodeConstraint;
        }

        public vrm.mtoon.MToon ExportMToon(Material material, NdmfVrmExporter.MToonTexture mToonTexture,
            GltfMaterialExporter exporter)
        {
            var mtoon = new vrm.mtoon.MToon
            {
                GIEqualizationFactor = 1.0f,
                MatcapFactor = System.Numerics.Vector3.Zero
            };
#if NVE_HAS_LILTOON
            var scrollRotate = material.GetColor(PropertyMainTexScrollRotate);

            Texture2D? LocalRetrieveTexture2D(string name)
            {
                return material.HasProperty(name) ? material.GetTexture(name) as Texture2D : null;
            }

            if (Mathf.Approximately(material.GetFloat(PropertyUseShadow), 1.0f))
            {
                var shadowBorder = material.GetFloat(PropertyShadowBorder);
                var shadowBlur = material.GetFloat(PropertyShadowBlur);
                var shadeShift = Mathf.Clamp01(shadowBorder - (shadowBlur * 0.5f)) * 2.0f - 1.0f;
                var shadeToony = Mathf.Approximately(shadeShift, 1.0f)
                    ? 1.0f
                    : (2.0f - Mathf.Clamp01(shadowBorder + shadowBlur * 0.5f) * 2.0f) /
                      (1.0f - shadeShift);
                if (LocalRetrieveTexture2D("_ShadowStrengthMask") ||
                    !Mathf.Approximately(material.GetFloat(PropertyShadowMainStrength), 0.0f))
                {
                    var bakedShadowTex =
                        MaterialBaker.AutoBakeShadowTexture(_assetSaver, material, mToonTexture.MainTexture);
                    mtoon.ShadeColorFactor = Color.white.ToVector3();
                    mtoon.ShadeMultiplyTexture = bakedShadowTex
                        ? exporter.ExportTextureInfoMToon(material, bakedShadowTex, ColorSpace.Gamma,
                            needsBlit: false)
                        : mToonTexture.MainTextureInfo;
                }
                else
                {
                    var shadowColor = material.GetColor(PropertyShadowColor);
                    var shadowStrength = material.GetFloat(PropertyShadowStrength);
                    var shadeColorStrength = new Color(
                        1.0f - (1.0f - shadowColor.r) * shadowStrength,
                        1.0f - (1.0f - shadowColor.g) * shadowStrength,
                        1.0f - (1.0f - shadowColor.b) * shadowStrength,
                        1.0f
                    );
                    mtoon.ShadeColorFactor = shadeColorStrength.ToVector3();
                    var shadowColorTex = LocalRetrieveTexture2D("_ShadowColorTex");
                    mtoon.ShadeMultiplyTexture = shadowColorTex
                        ? exporter.ExportTextureInfoMToon(material, shadowColorTex, ColorSpace.Gamma,
                            needsBlit: true)
                        : mToonTexture.MainTextureInfo;
                }

                var texture = LocalRetrieveTexture2D("_ShadowBorderMask");
                if (!texture)
                {
                    texture = LocalRetrieveTexture2D("_ShadowBorderTex");
                }

                var info = exporter.ExportTextureInfoMToon(material, texture, ColorSpace.Linear, needsBlit: true);
                if (info != null)
                {
                    mtoon.ShadingShiftTexture = new vrm.mtoon.ShadingShiftTexture
                    {
                        Index = info.Index,
                        Scale = 1.0f,
                        TexCoord = info.TexCoord,
                    };
                }

                var rangeMin = shadeShift;
                var rangeMax = Mathf.Lerp(1.0f, shadeShift, shadeToony);
                mtoon.ShadingShiftFactor = Mathf.Clamp((rangeMin + rangeMax) * -0.5f, -1.0f, 1.0f);
                mtoon.ShadingToonyFactor = Mathf.Clamp01((2.0f - (rangeMax - rangeMin)) * 0.5f);
            }
            else
            {
                mtoon.ShadeColorFactor = System.Numerics.Vector3.One;
                mtoon.ShadeMultiplyTexture = mToonTexture.MainTextureInfo;
            }

            var component = _gameObject.GetComponent<NdmfVrmExporterComponent>();
            if (component.enableMToonRimLight &&
                Mathf.Approximately(material.GetFloat(PropertyUseRim), 1.0f))
            {
                var rimColorTexture = LocalRetrieveTexture2D("_RimColorTex");
                var rimBorder = material.GetFloat(PropertyRimBorder);
                var rimBlur = material.GetFloat(PropertyRimBlur);
                var rimFresnelPower = material.GetFloat(PropertyRimFresnelPower);
                var rimFp = rimFresnelPower / Mathf.Max(0.001f, rimBlur);
                var rimLift = Mathf.Pow(1.0f - rimBorder, rimFresnelPower) * (1.0f - rimBlur);
                mtoon.RimLightingMixFactor = 1.0f;
                mtoon.ParametricRimColorFactor =
                    material.GetColor(PropertyRimColor).ToVector3();
                mtoon.ParametricRimLiftFactor = rimLift;
                mtoon.ParametricRimFresnelPowerFactor = rimFp;
                if (Mathf.Approximately(material.GetFloat(PropertyRimBlendMode), 3.0f))
                {
                    mtoon.RimMultiplyTexture =
                        exporter.ExportTextureInfoMToon(material, rimColorTexture, ColorSpace.Gamma,
                            needsBlit: true);
                }
            }
            else
            {
                mtoon.RimLightingMixFactor = 0.0f;
            }

            if (component.enableMToonMatCap &&
                Mathf.Approximately(material.GetFloat(PropertyUseMatCap), 1.0f) &&
                !Mathf.Approximately(material.GetFloat(PropertyMatCapBlendMode), 3.0f))
            {
                var matcapTexture = LocalRetrieveTexture2D("_MatCapTex");
                if (matcapTexture)
                {
                    var bakedMatCap = MaterialBaker.AutoBakeMatCap(_assetSaver, material);
                    mtoon.MatcapTexture =
                        exporter.ExportTextureInfoMToon(material, bakedMatCap, ColorSpace.Gamma, needsBlit: true);
                    mtoon.MatcapFactor = System.Numerics.Vector3.One;
                }

                if (!component.enableMToonRimLight)
                {
                    var matcapBlendMaskTexture = LocalRetrieveTexture2D("_MatCapBlendMask");
                    mtoon.RimMultiplyTexture = exporter.ExportTextureInfoMToon(material, matcapBlendMaskTexture,
                        ColorSpace.Gamma, needsBlit: true);
                    mtoon.RimLightingMixFactor = 1.0f;
                }
            }

            var shaderName = material.shader.name;
            var isOutline = shaderName.Contains("Outline");
            if (component.enableMToonOutline && isOutline)
            {
                var outlineWidthTexture = LocalRetrieveTexture2D("_OutlineWidthMask");
                mtoon.OutlineWidthMode = vrm.mtoon.OutlineWidthMode.WorldCoordinates;
                mtoon.OutlineLightingMixFactor = 1.0f;
                mtoon.OutlineWidthFactor = material.GetFloat(PropertyOutlineWidth) * 0.01f;
                mtoon.OutlineColorFactor = material.GetColor(PropertyOutlineColor)
                    .ToVector3();
                mtoon.OutlineWidthMultiplyTexture =
                    exporter.ExportTextureInfoMToon(material, outlineWidthTexture, ColorSpace.Linear,
                        needsBlit: true);
            }

            var isCutout = shaderName.Contains("Cutout");
            var isTransparent = shaderName.Contains("Transparent") || shaderName.Contains("Overlay");
            mtoon.TransparentWithZWrite =
                isCutout || (isTransparent &&
                             !Mathf.Approximately(material.GetFloat(PropertyZWrite), 0.0f));

            mtoon.UVAnimationScrollXSpeedFactor = scrollRotate.r;
            mtoon.UVAnimationScrollYSpeedFactor = scrollRotate.g;
            mtoon.UVAnimationRotationSpeedFactor = scrollRotate.a / Mathf.PI * 0.5f;
#endif // NVE_HAS_LILTOON
            return mtoon;
        }

        private void ExportMeta(gltf.ObjectID thumbnailImage, ref vrm.core.Core core)
        {
            var mc = _gameObject.GetComponent<NdmfVrmExporterComponent>();
            var meta = core.Meta;
            meta.Name = AssetPathUtils.TrimCloneSuffix(_gameObject.name);
            meta.Version = mc.version;
            meta.Authors = mc.authors.FindAll(s => !string.IsNullOrWhiteSpace(s));
            meta.LicenseUrl = mc.licenseUrl;
            meta.AvatarPermission = mc.avatarPermission;
            meta.AllowExcessivelyViolentUsage = ToBool(mc.allowExcessivelyViolentUsage);
            meta.AllowExcessivelySexualUsage = ToBool(mc.allowExcessivelySexualUsage);
            meta.CommercialUsage = mc.commercialUsage;
            meta.AllowPoliticalOrReligiousUsage = ToBool(mc.allowPoliticalOrReligiousUsage);
            meta.AllowAntisocialOrHateUsage = ToBool(mc.allowAntisocialOrHateUsage);
            meta.CreditNotation = mc.creditNotation;
            meta.AllowRedistribution = ToBool(mc.allowRedistribution);
            meta.Modification = mc.modification;
            if (!thumbnailImage.IsNull)
            {
                meta.ThumbnailImage = thumbnailImage;
            }

            if (!string.IsNullOrWhiteSpace(mc.copyrightInformation))
            {
                meta.CopyrightInformation = mc.copyrightInformation;
            }

            if (!string.IsNullOrWhiteSpace(mc.contactInformation))
            {
                meta.ContactInformation = mc.contactInformation;
            }

            if (mc.references.Count > 0)
            {
                meta.References = mc.references.FindAll(s => !string.IsNullOrWhiteSpace(s));
            }

            if (!string.IsNullOrWhiteSpace(mc.thirdPartyLicenses))
            {
                meta.ThirdPartyLicenses = mc.thirdPartyLicenses;
            }

            if (!string.IsNullOrWhiteSpace(mc.otherLicenseUrl))
            {
                meta.OtherLicenseUrl = mc.otherLicenseUrl;
            }
        }

        private void ExportHumanoidBone(ref vrm.core.Core core)
        {
            var animator = _gameObject.GetComponent<Animator>();
            var hb = core.Humanoid.HumanBones;
            hb.Hips.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.Hips);
            hb.Spine.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.Spine);
            var chest = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.Chest);
            if (chest.HasValue)
            {
                hb.Chest = new vrm.core.HumanBone { Node = chest.Value };
            }

            var upperChest = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.UpperChest);
            if (upperChest.HasValue)
            {
                hb.UpperChest = new vrm.core.HumanBone { Node = upperChest.Value };
            }

            var neck = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.Neck);
            if (neck.HasValue)
            {
                hb.Neck = new vrm.core.HumanBone { Node = neck.Value };
            }

            hb.Head.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.Head);
            var leftEye = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftEye);
            if (leftEye.HasValue)
            {
                hb.LeftEye = new vrm.core.HumanBone { Node = leftEye.Value };
            }

            var rightEye = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightEye);
            if (rightEye.HasValue)
            {
                hb.RightEye = new vrm.core.HumanBone { Node = rightEye.Value };
            }

            var jaw = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.Jaw);
            if (jaw.HasValue)
            {
                hb.Jaw = new vrm.core.HumanBone { Node = jaw.Value };
            }

            hb.LeftUpperLeg.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.LeftUpperLeg);
            hb.LeftLowerLeg.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.LeftLowerLeg);
            hb.LeftFoot.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.LeftFoot);
            var leftToes = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftToes);
            if (leftToes.HasValue)
            {
                hb.LeftToes = new vrm.core.HumanBone { Node = leftToes.Value };
            }

            hb.RightUpperLeg.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.RightUpperLeg);
            hb.RightLowerLeg.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.RightLowerLeg);
            hb.RightFoot.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.RightFoot);
            var rightToes = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightToes);
            if (rightToes.HasValue)
            {
                hb.RightToes = new vrm.core.HumanBone { Node = rightToes.Value };
            }

            var leftShoulder = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftShoulder);
            if (leftShoulder.HasValue)
            {
                hb.LeftShoulder = new vrm.core.HumanBone { Node = leftShoulder.Value };
            }

            hb.LeftUpperArm.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.LeftUpperArm);
            hb.LeftLowerArm.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.LeftLowerArm);
            hb.LeftHand.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.LeftHand);
            var rightShoulder = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightShoulder);
            if (rightShoulder.HasValue)
            {
                hb.RightShoulder = new vrm.core.HumanBone { Node = rightShoulder.Value };
            }

            hb.RightUpperArm.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.RightUpperArm);
            hb.RightLowerArm.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.RightLowerArm);
            hb.RightHand.Node = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.RightHand);
            var leftThumbProximal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftThumbProximal);
            if (leftThumbProximal.HasValue)
            {
                hb.LeftThumbMetacarpal = new vrm.core.HumanBone { Node = leftThumbProximal.Value };
            }

            var leftThumbIntermediate = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftThumbIntermediate);
            if (leftThumbIntermediate.HasValue)
            {
                hb.LeftThumbProximal = new vrm.core.HumanBone { Node = leftThumbIntermediate.Value };
            }

            var leftThumbDistal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftThumbDistal);
            if (leftThumbDistal.HasValue)
            {
                hb.LeftThumbDistal = new vrm.core.HumanBone { Node = leftThumbDistal.Value };
            }

            var leftIndexProximal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftIndexProximal);
            if (leftIndexProximal.HasValue)
            {
                hb.LeftIndexProximal = new vrm.core.HumanBone { Node = leftIndexProximal.Value };
            }

            var leftIndexIntermediate = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftIndexIntermediate);
            if (leftIndexIntermediate.HasValue)
            {
                hb.LeftIndexIntermediate = new vrm.core.HumanBone { Node = leftIndexIntermediate.Value };
            }

            var leftIndexDistal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftIndexDistal);
            if (leftIndexDistal.HasValue)
            {
                hb.LeftIndexDistal = new vrm.core.HumanBone { Node = leftIndexDistal.Value };
            }

            var leftMiddleProximal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftMiddleProximal);
            if (leftMiddleProximal.HasValue)
            {
                hb.LeftMiddleProximal = new vrm.core.HumanBone { Node = leftMiddleProximal.Value };
            }

            var leftMiddleIntermediate =
                GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftMiddleIntermediate);
            if (leftMiddleIntermediate.HasValue)
            {
                hb.LeftMiddleIntermediate = new vrm.core.HumanBone { Node = leftMiddleIntermediate.Value };
            }

            var leftMiddleDistal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftMiddleDistal);
            if (leftMiddleDistal.HasValue)
            {
                hb.LeftMiddleDistal = new vrm.core.HumanBone { Node = leftMiddleDistal.Value };
            }

            var leftRingProximal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftRingProximal);
            if (leftRingProximal.HasValue)
            {
                hb.LeftRingProximal = new vrm.core.HumanBone { Node = leftRingProximal.Value };
            }

            var leftRingIntermediate = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftRingIntermediate);
            if (leftRingIntermediate.HasValue)
            {
                hb.LeftRingIntermediate = new vrm.core.HumanBone { Node = leftRingIntermediate.Value };
            }

            var leftRingDistal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftRingDistal);
            if (leftRingDistal.HasValue)
            {
                hb.LeftRingDistal = new vrm.core.HumanBone { Node = leftRingDistal.Value };
            }

            var leftLittleProximal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftLittleProximal);
            if (leftLittleProximal.HasValue)
            {
                hb.LeftLittleProximal = new vrm.core.HumanBone { Node = leftLittleProximal.Value };
            }

            var leftLittleIntermediate =
                GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftLittleIntermediate);
            if (leftLittleIntermediate.HasValue)
            {
                hb.LeftLittleIntermediate = new vrm.core.HumanBone { Node = leftLittleIntermediate.Value };
            }

            var leftLittleDistal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.LeftLittleDistal);
            if (leftLittleDistal.HasValue)
            {
                hb.LeftLittleDistal = new vrm.core.HumanBone { Node = leftLittleDistal.Value };
            }

            var rightThumbProximal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightThumbProximal);
            if (rightThumbProximal.HasValue)
            {
                hb.RightThumbMetacarpal = new vrm.core.HumanBone { Node = rightThumbProximal.Value };
            }

            var rightThumbIntermediate =
                GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightThumbIntermediate);
            if (rightThumbIntermediate.HasValue)
            {
                hb.RightThumbProximal = new vrm.core.HumanBone { Node = rightThumbIntermediate.Value };
            }

            var rightThumbDistal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightThumbDistal);
            if (rightThumbDistal.HasValue)
            {
                hb.RightThumbDistal = new vrm.core.HumanBone { Node = rightThumbDistal.Value };
            }

            var rightIndexProximal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightIndexProximal);
            if (rightIndexProximal.HasValue)
            {
                hb.RightIndexProximal = new vrm.core.HumanBone { Node = rightIndexProximal.Value };
            }

            var rightIndexIntermediate =
                GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightIndexIntermediate);
            if (rightIndexIntermediate.HasValue)
            {
                hb.RightIndexIntermediate = new vrm.core.HumanBone { Node = rightIndexIntermediate.Value };
            }

            var rightIndexDistal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightIndexDistal);
            if (rightIndexDistal.HasValue)
            {
                hb.RightIndexDistal = new vrm.core.HumanBone { Node = rightIndexDistal.Value };
            }

            var rightMiddleProximal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightMiddleProximal);
            if (rightMiddleProximal.HasValue)
            {
                hb.RightMiddleProximal = new vrm.core.HumanBone { Node = rightMiddleProximal.Value };
            }

            var rightMiddleIntermediate =
                GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightMiddleIntermediate);
            if (rightMiddleIntermediate.HasValue)
            {
                hb.RightMiddleIntermediate = new vrm.core.HumanBone { Node = rightMiddleIntermediate.Value };
            }

            var rightMiddleDistal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightMiddleDistal);
            if (rightMiddleDistal.HasValue)
            {
                hb.RightMiddleDistal = new vrm.core.HumanBone { Node = rightMiddleDistal.Value };
            }

            var rightRingProximal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightRingProximal);
            if (rightRingProximal.HasValue)
            {
                hb.RightRingProximal = new vrm.core.HumanBone { Node = rightRingProximal.Value };
            }

            var rightRingIntermediate = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightRingIntermediate);
            if (rightRingIntermediate.HasValue)
            {
                hb.RightRingIntermediate = new vrm.core.HumanBone { Node = rightRingIntermediate.Value };
            }

            var rightRingDistal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightRingDistal);
            if (rightRingDistal.HasValue)
            {
                hb.RightRingDistal = new vrm.core.HumanBone { Node = rightRingDistal.Value };
            }

            var rightLittleProximal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightLittleProximal);
            if (rightLittleProximal.HasValue)
            {
                hb.RightLittleProximal = new vrm.core.HumanBone { Node = rightLittleProximal.Value };
            }

            var rightLittleIntermediate =
                GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightLittleIntermediate);
            if (rightLittleIntermediate.HasValue)
            {
                hb.RightLittleIntermediate = new vrm.core.HumanBone { Node = rightLittleIntermediate.Value };
            }

            var rightLittleDistal = GetOptionalHumanBoneNodeID(animator, HumanBodyBones.RightLittleDistal);
            if (rightLittleDistal.HasValue)
            {
                hb.RightLittleDistal = new vrm.core.HumanBone { Node = rightLittleDistal.Value };
            }
        }

        private static bool TryParseAimAxis(Vector3 axis, out string value)
        {
            if (axis == Vector3.left)
            {
                value = "PositiveX";
            }
            else if (axis == Vector3.right)
            {
                value = "NegativeX";
            }
            else if (axis == Vector3.up)
            {
                value = "PositiveY";
            }
            else if (axis == Vector3.down)
            {
                value = "NegativeY";
            }
            else if (axis == Vector3.forward)
            {
                value = "PositiveZ";
            }
            else if (axis == Vector3.back)
            {
                value = "NegativeZ";
            }
            else
            {
                value = "";
            }

            return !string.IsNullOrEmpty(value);
        }

        private void ExportExpression(ref vrm.core.Core core)
        {
#if NVE_HAS_VRCHAT_AVATAR_SDK
            var avatarDescriptor = _gameObject.GetComponent<VRCAvatarDescriptor>();
            core.Expressions = new vrm.core.Expressions
            {
                Preset =
                {
                    Aa = ExportExpressionViseme(avatarDescriptor, VRC_AvatarDescriptor.Viseme.aa),
                    Ih = ExportExpressionViseme(avatarDescriptor, VRC_AvatarDescriptor.Viseme.ih),
                    Ou = ExportExpressionViseme(avatarDescriptor, VRC_AvatarDescriptor.Viseme.ou),
                    Ee = ExportExpressionViseme(avatarDescriptor, VRC_AvatarDescriptor.Viseme.E),
                    Oh = ExportExpressionViseme(avatarDescriptor, VRC_AvatarDescriptor.Viseme.oh),
                    Blink = ExportExpressionEyelids(avatarDescriptor, 0),
                    LookUp = ExportExpressionEyelids(avatarDescriptor, 1),
                    LookDown = ExportExpressionEyelids(avatarDescriptor, 2),
                }
            };
#else
                core.Expressions = new vrm.core.Expressions();
#endif // NVE_HAS_VRCHAT_AVATAR_SDK
            var component = _gameObject.GetComponent<NdmfVrmExporterComponent>();
            if (component.expressionPresetHappyBlendShape.IsValid)
            {
                core.Expressions.Preset.Happy = ExportExpressionItem(component.expressionPresetHappyBlendShape);
            }
            else
            {
                Debug.LogWarning("Preset Happy will be skipped due to expression is not set properly");
            }

            if (component.expressionPresetAngryBlendShape.IsValid)
            {
                core.Expressions.Preset.Angry = ExportExpressionItem(component.expressionPresetAngryBlendShape);
            }
            else
            {
                Debug.LogWarning("Preset Angry will be skipped due to expression is not set properly");
            }

            if (component.expressionPresetSadBlendShape.IsValid)
            {
                core.Expressions.Preset.Sad = ExportExpressionItem(component.expressionPresetSadBlendShape);
            }
            else
            {
                Debug.LogWarning("Preset Sad will be skipped due to expression is not set properly");
            }

            if (component.expressionPresetRelaxedBlendShape.IsValid)
            {
                core.Expressions.Preset.Relaxed =
                    ExportExpressionItem(component.expressionPresetRelaxedBlendShape);
            }
            else
            {
                Debug.LogWarning("Preset Relaxed will be skipped due to expression is not set properly");
            }

            if (component.expressionPresetSurprisedBlendShape.IsValid)
            {
                core.Expressions.Preset.Surprised =
                    ExportExpressionItem(component.expressionPresetSurprisedBlendShape);
            }
            else
            {
                Debug.LogWarning("Preset Surprised will be skipped due to expression is not set properly");
            }

            if (component.expressionPresetBlinkLeftBlendShape.IsValid)
            {
                core.Expressions.Preset.BlinkLeft =
                    ExportExpressionItem(component.expressionPresetBlinkLeftBlendShape);
            }

            if (component.expressionPresetBlinkRightBlendShape.IsValid)
            {
                core.Expressions.Preset.BlinkRight =
                    ExportExpressionItem(component.expressionPresetBlinkRightBlendShape);
            }

            var offset = 0;
            foreach (var property in component.expressionCustomBlendShapes)
            {
                var index = offset++;
                if (!property.IsValid)
                {
                    Debug.LogWarning(
                        $"Custom expression offset with {index} will be skipped due to expression is not set properly");
                    continue;
                }

                var item = ExportExpressionItem(property);
                if (item == null)
                    continue;
                core.Expressions.Custom ??= new Dictionary<gltf.UnicodeString, vrm.core.ExpressionItem>();
                core.Expressions.Custom.Add(new gltf.UnicodeString(property.CanonicalExpressionName), item);
            }
        }

#if NVE_HAS_VRCHAT_AVATAR_SDK
        private void ExportLookAt(ref vrm.core.Core core)
        {
            var descriptor = _gameObject.GetComponent<VRCAvatarDescriptor>();
            var animator = _gameObject.GetComponent<Animator>();
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var headPosition = head.position - _gameObject.transform.position;
            var offsetFromHeadBone = descriptor.ViewPosition - headPosition;
            core.LookAt = new vrm.core.LookAt
            {
                Type = vrm.core.LookAtType.Bone,
                OffsetFromHeadBone = offsetFromHeadBone.ToVector3WithCoordinateSpace(),
            };
            var down = descriptor.customEyeLookSettings.eyesLookingDown;
            if (down != null)
            {
                var leftAngles = down.left.eulerAngles;
                var rightAngles = down.right.eulerAngles;
                core.LookAt.RangeMapVerticalDown = new vrm.core.RangeMap
                {
                    InputMaxValue = Math.Min(leftAngles.x, rightAngles.x),
                    OutputScale = 1.0f,
                };
            }

            var up = descriptor.customEyeLookSettings.eyesLookingUp;
            if (up != null)
            {
                var leftAngles = up.left.eulerAngles;
                var rightAngles = up.right.eulerAngles;
                core.LookAt.RangeMapVerticalUp = new vrm.core.RangeMap
                {
                    InputMaxValue = Math.Min(leftAngles.x, rightAngles.x),
                    OutputScale = 1.0f,
                };
            }

            var left = descriptor.customEyeLookSettings.eyesLookingLeft;
            var right = descriptor.customEyeLookSettings.eyesLookingRight;
            if (left == null || right == null)
            {
                return;
            }

            var leftLeftAngles = left.left.eulerAngles;
            var leftRightAngles = left.right.eulerAngles;
            var rightLeftAngles = right.left.eulerAngles;
            var rightRightAngles = right.right.eulerAngles;
            core.LookAt.RangeMapHorizontalInner = new vrm.core.RangeMap
            {
                InputMaxValue = Math.Min(leftLeftAngles.y, leftRightAngles.y),
                OutputScale = 1.0f,
            };
            core.LookAt.RangeMapHorizontalOuter = new vrm.core.RangeMap
            {
                InputMaxValue = Math.Min(rightLeftAngles.y, rightRightAngles.y),
                OutputScale = 1.0f,
            };
        }

        private vrm.core.ExpressionItem? ExportExpressionViseme(VRCAvatarDescriptor descriptor,
            VRC_AvatarDescriptor.Viseme viseme)
        {
            if (descriptor.lipSync != VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
            {
                return null;
            }

            var nodeID = FindTransformNodeID(descriptor.VisemeSkinnedMesh.transform);
            if (!nodeID.HasValue)
            {
                Debug.LogWarning($"Viseme skinned mesh {descriptor.VisemeSkinnedMesh} not found due to inactive");
                return null;
            }

            var blendShapeName = descriptor.VisemeBlendShapes[(int)viseme];
            var offset = descriptor.VisemeSkinnedMesh.sharedMesh.GetBlendShapeIndex(blendShapeName);
            var item = new vrm.core.ExpressionItem
            {
                MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                {
                    new()
                    {
                        Node = nodeID.Value,
                        Index = new gltf.ObjectID((uint)offset),
                        Weight = 1.0f,
                    }
                }
            };
            return item;
        }

        private vrm.core.ExpressionItem? ExportExpressionEyelids(VRCAvatarDescriptor descriptor, int offset)
        {
            if (!descriptor.enableEyeLook)
            {
                return null;
            }

            var settings = descriptor.customEyeLookSettings;
            if (settings.eyelidType != VRCAvatarDescriptor.EyelidType.Blendshapes)
            {
                return null;
            }

            var nodeID = FindTransformNodeID(descriptor.VisemeSkinnedMesh.transform);
            if (!nodeID.HasValue)
            {
                Debug.LogWarning($"Viseme skinned mesh {descriptor.VisemeSkinnedMesh} not found due to inactive");
                return null;
            }

            var blendShapeIndex = settings.eyelidsBlendshapes[offset];
            if (blendShapeIndex == -1)
                return null;
            var item = new vrm.core.ExpressionItem
            {
                MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                {
                    new()
                    {
                        Node = nodeID.Value,
                        Index = new gltf.ObjectID((uint)blendShapeIndex),
                        Weight = 1.0f,
                    }
                }
            };
            return item;
        }
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

        private vrm.core.ExpressionItem? ExportExpressionItem(VrmExpressionProperty property)
        {
            switch (property.baseType)
            {
                case VrmExpressionProperty.BaseType.BlendShape:
                {
                    if (_allMorphTargets.TryGetValue(property.blendShapeName!, out var value))
                        return new vrm.core.ExpressionItem
                        {
                            OverrideBlink = property.overrideBlink,
                            OverrideLookAt = property.overrideLookAt,
                            OverrideMouth = property.overrideMouth,
                            IsBinary = property.isBinary,
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = value.Item1,
                                    Index = new gltf.ObjectID((uint)value.Item2),
                                    Weight = 1.0f,
                                }
                            }
                        };
                    Debug.LogWarning($"BlendShape {property.blendShapeName} is not found");
                    break;
                }
                case VrmExpressionProperty.BaseType.AnimationClip:
                {
                    var morphTargetBinds = new List<vrm.core.MorphTargetBind>();
                    foreach (var binding in AnimationUtility.GetCurveBindings(property.blendShapeAnimationClip))
                    {
                        if (!binding.propertyName.StartsWith(VrmExpressionProperty.BlendShapeNamePrefix,
                                StringComparison.Ordinal))
                            continue;
                        var blendShapeName =
                            binding.propertyName[VrmExpressionProperty.BlendShapeNamePrefix.Length..];
                        var curve = AnimationUtility.GetEditorCurve(property.blendShapeAnimationClip, binding);
                        foreach (var keyframe in curve.keys)
                        {
                            if (keyframe.time > 0.0f || Mathf.Approximately(keyframe.value, 0.0f))
                                continue;
                            if (!_allMorphTargets.TryGetValue(blendShapeName, out var value))
                            {
                                Debug.LogWarning($"BlendShape {blendShapeName} is not found");
                                continue;
                            }

                            morphTargetBinds.Add(new vrm.core.MorphTargetBind
                            {
                                Node = value.Item1,
                                Index = new gltf.ObjectID((uint)value.Item2),
                                Weight = keyframe.value * 0.01f,
                            });
                        }
                    }

                    if (morphTargetBinds.Count > 0)
                    {
                        return new vrm.core.ExpressionItem
                        {
                            OverrideBlink = property.overrideBlink,
                            OverrideLookAt = property.overrideLookAt,
                            OverrideMouth = property.overrideMouth,
                            IsBinary = property.isBinary,
                            MorphTargetBinds = morphTargetBinds
                        };
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return null;
        }

#if NVE_HAS_VRCHAT_AVATAR_SDK
        private void ConvertBoneCollider(VRCPhysBoneCollider collider,
            ref IList<VRCPhysBoneColliderBase> pbColliders,
            ref IList<vrm.sb.Collider> colliders)
        {
            var rootTransform = collider.GetRootTransform();
            var nodeID = FindTransformNodeID(rootTransform);
            if (!nodeID.HasValue)
            {
                Debug.LogWarning($"Collider root transform {rootTransform} not found due to inactive");
                return;
            }

            switch (collider.shapeType)
            {
                case VRCPhysBoneColliderBase.ShapeType.Capsule:
                {
                    var position = collider.position;
                    var radius = collider.radius;
                    var height = (collider.height - radius * 2.0f) * 0.5f;
                    var offset = position + collider.rotation * new Vector3(0.0f, -height, 0.0f);
                    var tail = position + collider.rotation * new Vector3(0.0f, height, 0.0f);
                    var capsuleCollider = new vrm.sb.Collider
                    {
                        Node = nodeID.Value,
                        Shape = new vrm.sb.Shape
                        {
                            Capsule = new vrm.sb.Capsule
                            {
                                Offset = offset.ToVector3WithCoordinateSpace(),
                                Radius = radius,
                                Tail = tail.ToVector3WithCoordinateSpace(),
                            }
                        }
                    };
                    if (collider.insideBounds)
                    {
                        var extendedCollider = new vrm.sb.ExtendedCollider
                        {
                            Spec = "1.0",
                            Shape = new vrm.sb.ExtendedShape
                            {
                                Capsule = new vrm.sb.ShapeCapsule
                                {
                                    Offset = offset.ToVector3WithCoordinateSpace(),
                                    Radius = radius,
                                    Tail = tail.ToVector3WithCoordinateSpace(),
                                    Inside = true,
                                }
                            }
                        };
                        capsuleCollider.Shape.Capsule.Offset = new System.Numerics.Vector3(-FarDistance);
                        capsuleCollider.Shape.Capsule.Tail = new System.Numerics.Vector3(-FarDistance);
                        capsuleCollider.Shape.Capsule.Radius = 0.0f;
                        capsuleCollider.Extensions ??= new Dictionary<string, JToken>();
                        capsuleCollider.Extensions.Add(VrmcSpringBoneExtendedCollider,
                            vrm.Document.SaveAsNode(extendedCollider));
                        _extensionUsed.Add(VrmcSpringBoneExtendedCollider);
                    }

                    colliders.Add(capsuleCollider);
                    break;
                }
                case VRCPhysBoneColliderBase.ShapeType.Plane:
                {
                    var offset = collider.position;
                    var normal = collider.axis;
                    var extendedCollider = new vrm.sb.ExtendedCollider
                    {
                        Spec = "1.0",
                        Shape = new vrm.sb.ExtendedShape
                        {
                            Plane = new vrm.sb.ShapePlane
                            {
                                Offset = offset.ToVector3WithCoordinateSpace(),
                                Normal = normal.ToVector3WithCoordinateSpace(),
                            }
                        }
                    };
                    var planeCollider = new vrm.sb.Collider
                    {
                        Node = nodeID.Value,
                        Shape = new vrm.sb.Shape
                        {
                            Sphere = new vrm.sb.Sphere
                            {
                                Offset = offset.ToVector3WithCoordinateSpace() -
                                         (normal * FarDistance).ToVector3WithCoordinateSpace(),
                                Radius = FarDistance,
                            }
                        },
                        Extensions = new Dictionary<string, JToken>(),
                    };
                    planeCollider.Extensions.Add(VrmcSpringBoneExtendedCollider,
                        vrm.Document.SaveAsNode(extendedCollider));
                    colliders.Add(planeCollider);
                    _extensionUsed.Add(VrmcSpringBoneExtendedCollider);
                    break;
                }
                case VRCPhysBoneColliderBase.ShapeType.Sphere:
                {
                    var offset = collider.position;
                    var radius = collider.radius;
                    var sphereCollider = new vrm.sb.Collider
                    {
                        Node = nodeID.Value,
                        Shape = new vrm.sb.Shape
                        {
                            Sphere = new vrm.sb.Sphere
                            {
                                Offset = offset.ToVector3WithCoordinateSpace(),
                                Radius = radius,
                            }
                        }
                    };
                    if (collider.insideBounds)
                    {
                        var extendedCollider = new vrm.sb.ExtendedCollider
                        {
                            Spec = "1.0",
                            Shape = new vrm.sb.ExtendedShape
                            {
                                Sphere = new vrm.sb.ShapeSphere
                                {
                                    Offset = offset.ToVector3WithCoordinateSpace(),
                                    Radius = radius,
                                    Inside = true,
                                }
                            }
                        };
                        sphereCollider.Shape.Sphere.Offset = new System.Numerics.Vector3(-FarDistance);
                        sphereCollider.Shape.Sphere.Radius = 0.0f;
                        sphereCollider.Extensions ??= new Dictionary<string, JToken>();
                        sphereCollider.Extensions.Add(VrmcSpringBoneExtendedCollider,
                            vrm.Document.SaveAsNode(extendedCollider));
                        _extensionUsed.Add(VrmcSpringBoneExtendedCollider);
                    }

                    colliders.Add(sphereCollider);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            pbColliders.Add(collider);
        }

        private static void ConvertColliderGroup(VRCPhysBone pb,
            IImmutableList<VRCPhysBoneColliderBase> pbColliders,
            ref IList<vrm.sb.ColliderGroup> colliderGroups)
        {
            var colliders = (from collider in pb.colliders
                select pbColliders.IndexOf(collider)
                into index
                where index != -1
                select new gltf.ObjectID((uint)index)).ToList();
            if (colliders.Count <= 0)
                return;
            var colliderGroup = new vrm.sb.ColliderGroup
            {
                Name = new gltf.UnicodeString(pb.name),
                Colliders = colliders,
            };
            colliderGroups.Add(colliderGroup);
        }

        private static bool RetrieveSpringBoneChainTransforms(Transform transform, ref List<List<Transform>> chains)
        {
            var numChildren = 0;
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (!child || child is null)
                {
                    continue;
                }

                chains.Last().Add(child);
                if (RetrieveSpringBoneChainTransforms(child, ref chains))
                {
                    chains.Add(new List<Transform>());
                }

                numChildren++;
            }

            return numChildren > 0;
        }

        private static bool CalcTransformDepth(Transform? transform, bool incrementDepth, ref int depth)
        {
            var numChildren = 0;
            var hasChildren = false;
            for (var i = 0; i < transform?.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (!child || child is null)
                {
                    continue;
                }

                hasChildren |= CalcTransformDepth(child, !hasChildren, ref depth);
                numChildren++;
            }

            if (hasChildren && incrementDepth)
            {
                depth++;
            }

            return numChildren > 0;
        }

        private readonly struct DepthRatio
        {
            public DepthRatio(float upperDepth, float lowerDepth)
            {
                var totalDepth = upperDepth + lowerDepth;
                var depthRatio = totalDepth != 0 ? upperDepth / totalDepth : 0;
                Value = depthRatio;
            }

            public float Value { get; }
        }

        private static DepthRatio FindTransformDepth(Transform? transform, Transform? root)
        {
            var upperDepth = 0;
            var upperTransform = transform;
            while (upperTransform && upperTransform != root)
            {
                upperDepth++;
                upperTransform = upperTransform?.parent;
            }

            var lowerDepth = 0;
            if (CalcTransformDepth(transform, true, ref lowerDepth))
            {
                lowerDepth++;
            }

            return new DepthRatio(upperDepth, lowerDepth);
        }

        private void ConvertSpringBone(VRCPhysBone pb, NdmfVrmExporterComponent component,
            IImmutableList<VRCPhysBoneColliderBase> pbColliders,
            IImmutableList<vrm.sb.ColliderGroup> colliderGroups, ref IList<vrm.sb.Spring> springs)
        {
            var rootTransform = pb.GetRootTransform();
            var chains = new List<List<Transform>>
            {
                new() { rootTransform }
            };
            RetrieveSpringBoneChainTransforms(rootTransform, ref chains);
            var newChains = chains.Where(chain => chain.Count != 0).Select(ImmutableList.CreateRange)
                .ToImmutableList();
            var hasChainBranch = newChains.Count > 1;
            var index = 1;
            foreach (var transforms in newChains)
            {
                switch (hasChainBranch)
                {
                    case true when pb.multiChildType == VRCPhysBoneBase.MultiChildType.Ignore && index == 1:
                    {
                        var name = $"{pb.name}.{index}";
                        ConvertSpringBoneInner(pb, component, name, transforms.Skip(1).ToImmutableList(),
                            pbColliders, colliderGroups, ref springs);
                        break;
                    }
                    case true:
                    {
                        var name = $"{pb.name}.{index}";
                        ConvertSpringBoneInner(pb, component, name, transforms, pbColliders, colliderGroups,
                            ref springs);
                        break;
                    }
                    default:
                    {
                        var name = pb.name;
                        ConvertSpringBoneInner(pb, component, name, transforms, pbColliders, colliderGroups,
                            ref springs);
                        break;
                    }
                }

                index++;
            }
        }

        private void ConvertSpringBoneInner(VRCPhysBone pb, NdmfVrmExporterComponent component, string name,
            IImmutableList<Transform> transforms,
            IImmutableList<VRCPhysBoneColliderBase> pbColliders,
            IImmutableList<vrm.sb.ColliderGroup> colliderGroups, ref IList<vrm.sb.Spring> springs)
        {
            var rootTransform = pb.GetRootTransform();
            gltf.ObjectID? centerNode = null;
            if (pb.immobileType == VRCPhysBoneBase.ImmobileType.World && Mathf.Approximately(pb.immobile, 1.0f) &&
                pb.immobileCurve is not { length: > 0 })
            {
                var animator = _gameObject.GetComponent<Animator>();
                centerNode = GetRequiredHumanBoneNodeID(animator, HumanBodyBones.Hips);
            }

            var joints = transforms.Select(transform =>
                {
                    var nodeID = FindTransformNodeID(transform);
                    if (!nodeID.HasValue)
                    {
                        Debug.LogWarning($"Joint transform {transform} not found due to inactive");
                        return null;
                    }

                    var depthRatio = FindTransformDepth(transform, rootTransform);
                    var gravity = EvaluateCurve(pb.gravityCurve, pb.gravity, depthRatio);
                    var stiffness = EvaluateCurve(pb.stiffnessCurve, pb.stiffness, depthRatio);
                    var hitRadius = EvaluateCurve(pb.radiusCurve, pb.radius, depthRatio);
                    var pull = EvaluateCurve(pb.pullCurve, pb.pull, depthRatio);
                    var immobile = EvaluateCurve(pb.immobileCurve, pb.immobile, depthRatio) * 0.5f;
                    float stiffnessFactor, pullFactor;
                    if (pb.limitType != VRCPhysBoneBase.LimitType.None && !centerNode.HasValue)
                    {
                        var maxAngleX = EvaluateCurve(pb.maxAngleXCurve, pb.maxAngleX, depthRatio);
                        stiffnessFactor = maxAngleX > 0.0f ? 1.0f / Mathf.Clamp01(maxAngleX / 180.0f) : 0.0f;
                        pullFactor = stiffnessFactor * 0.5f;
                    }
                    else
                    {
                        stiffnessFactor = 1.0f;
                        pullFactor = 1.0f;
                    }

                    var joint = new vrm.sb.Joint
                    {
                        Node = nodeID.Value,
                        HitRadius = hitRadius,
                        Stiffness = immobile + stiffness * stiffnessFactor,
                        GravityPower = gravity,
                        GravityDir = -System.Numerics.Vector3.UnitY,
                        DragForce = Mathf.Clamp01(immobile + pull * pullFactor),
                    };
                    var limit = component.IsSpringBoneLimitEnabled ? ConvertSpringLimit(pb, depthRatio) : null;
                    if (limit == null)
                    {
                        return joint;
                    }

                    joint.Extensions ??= new Dictionary<string, JToken>();
                    joint.Extensions.Add(VrmcSpringBoneLimit, vrm.Document.SaveAsNode(new vrm.sb.SpringLimit
                    {
                        SpecVersion = "1.0-draft",
                        Limit = limit,
                    }));
                    _extensionUsed.Add(VrmcSpringBoneLimit);

                    return joint;
                })
                .ToList();
            var colliders = (from pbCollider in pb.colliders
                select pbColliders.IndexOf(pbCollider)
                into index
                where index != -1
                select new gltf.ObjectID((uint)index)).ToList();

            var groupID = 0;
            var newColliderGroups = new HashSet<gltf.ObjectID>();
            foreach (var group in colliderGroups)
            {
                if (colliders.Any(id => group.Colliders.IndexOf(id) != -1))
                {
                    newColliderGroups.Add(new gltf.ObjectID((uint)groupID));
                }

                groupID++;
            }

            var spring = new vrm.sb.Spring
            {
                Name = new gltf.UnicodeString(name),
                Center = centerNode,
                ColliderGroups = newColliderGroups.Count > 0 ? newColliderGroups.ToList() : null,
                Joints = joints.Where(joint => joint != null).Select(joint => joint!).ToList(),
            };
            springs.Add(spring);
        }

        private static vrm.sb.Limit? ConvertSpringLimit(VRCPhysBone pb, DepthRatio depthRatio)
        {
            vrm.sb.Limit? limit = null;
            switch (pb.limitType)
            {
                case VRCPhysBoneBase.LimitType.Angle:
                {
                    limit = new vrm.sb.Limit
                    {
                        Cone = new vrm.sb.ConeLimit
                        {
                            Angle = Mathf.Deg2Rad * EvaluateCurve(pb.maxAngleXCurve, pb.maxAngleX, depthRatio),
                            Rotation = Quaternion.Euler(pb.limitRotation).ToQuaternionWithCoordinateSpace(),
                        }
                    };
                    break;
                }
                case VRCPhysBoneBase.LimitType.Hinge:
                {
                    limit = new vrm.sb.Limit
                    {
                        Hinge = new vrm.sb.HingeLimit
                        {
                            Angle = Mathf.Deg2Rad * EvaluateCurve(pb.maxAngleXCurve, pb.maxAngleX, depthRatio),
                            Rotation = Quaternion.Euler(pb.limitRotation).ToQuaternionWithCoordinateSpace(),
                        }
                    };
                    break;
                }
                case VRCPhysBoneBase.LimitType.Polar:
                {
                    limit = new vrm.sb.Limit
                    {
                        Spherical = new vrm.sb.SphericalLimit
                        {
                            Pitch = Mathf.Deg2Rad * EvaluateCurve(pb.maxAngleXCurve, pb.maxAngleX, depthRatio),
                            Yaw = Mathf.Deg2Rad * EvaluateCurve(pb.maxAngleZCurve, pb.maxAngleZ, depthRatio),
                            Rotation = Quaternion.Euler(pb.limitRotation).ToQuaternionWithCoordinateSpace(),
                        }
                    };
                    break;
                }
                case VRCPhysBoneBase.LimitType.None:
                default:
                {
                    break;
                }
            }

            return limit;
        }

        private static float EvaluateCurve(AnimationCurve curve, float value, DepthRatio depthRatio)
        {
            return curve is { length: > 0 } ? curve.Evaluate(depthRatio.Value) * value : value;
        }
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

        private static bool ToBool(VrmUsagePermission value)
        {
            return value switch
            {
                VrmUsagePermission.Allow => true,
                VrmUsagePermission.Disallow => false,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private gltf.ObjectID GetRequiredHumanBoneNodeID(Animator animator, HumanBodyBones bone)
        {
            return _transformNodeIDs[animator.GetBoneTransform(bone)];
        }

        private gltf.ObjectID? GetOptionalHumanBoneNodeID(Animator animator, HumanBodyBones bone)
        {
            return FindTransformNodeID(animator.GetBoneTransform(bone));
        }

        private gltf.ObjectID? FindTransformNodeID(Transform transform)
        {
            return transform && _transformNodeIDs.TryGetValue(transform, out var nodeID) ? nodeID : null;
        }

        private readonly GameObject _gameObject;
        private readonly IAssetSaver _assetSaver;
        private readonly IImmutableDictionary<string, (gltf.ObjectID, int)> _allMorphTargets;
        private readonly IImmutableDictionary<Transform, gltf.ObjectID> _transformNodeIDs;
        private readonly ISet<string> _extensionUsed;
    }
}
