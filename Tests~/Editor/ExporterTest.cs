// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using Activator = System.Activator;

#if NVE_HAS_VRCHAT_AVATAR_SDK
using System;
using UnityEditor;
using VRC.Dynamics;
using VRC.SDKBase;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

#if NVE_HAS_MODULAR_AVATAR
using nadena.dev.modular_avatar.core;
#endif // NVE_HAS_MODULAR_AVATAR

#if NVE_HAS_LILYCAL_INVENTORY
using System.Reflection;
using jp.lilxyzw.lilycalinventory.runtime;
#endif // NVE_HAS_LILYCAL_INVENTORY

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    internal sealed class ExporterTest
    {
        private static readonly List<(HumanBodyBones, HumanBodyBones?)> HumanBodyBoneNames = new()
        {
            (HumanBodyBones.Hips, null),
            (HumanBodyBones.LeftUpperLeg, HumanBodyBones.Hips),
            (HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips),
            (HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftUpperLeg),
            (HumanBodyBones.RightLowerLeg, HumanBodyBones.RightUpperLeg),
            (HumanBodyBones.LeftFoot, HumanBodyBones.LeftLowerLeg),
            (HumanBodyBones.RightFoot, HumanBodyBones.RightLowerLeg),
            (HumanBodyBones.LeftToes, HumanBodyBones.LeftFoot),
            (HumanBodyBones.RightToes, HumanBodyBones.RightFoot),
            (HumanBodyBones.Spine, HumanBodyBones.Hips),
            (HumanBodyBones.Chest, HumanBodyBones.Spine),
            (HumanBodyBones.UpperChest, HumanBodyBones.Chest),
            (HumanBodyBones.Neck, HumanBodyBones.UpperChest),
            (HumanBodyBones.Head, HumanBodyBones.Neck),
            (HumanBodyBones.Jaw, HumanBodyBones.Head),
            (HumanBodyBones.LeftEye, HumanBodyBones.Head),
            (HumanBodyBones.RightEye, HumanBodyBones.Head),
            (HumanBodyBones.LeftUpperArm, HumanBodyBones.Spine),
            (HumanBodyBones.RightUpperArm, HumanBodyBones.Spine),
            (HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftUpperArm),
            (HumanBodyBones.RightLowerArm, HumanBodyBones.RightUpperArm),
            (HumanBodyBones.LeftHand, HumanBodyBones.LeftLowerArm),
            (HumanBodyBones.RightHand, HumanBodyBones.RightLowerArm),
            (HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftHand),
            (HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbProximal),
            (HumanBodyBones.LeftThumbDistal, HumanBodyBones.LeftThumbIntermediate),
            (HumanBodyBones.RightThumbProximal, HumanBodyBones.RightHand),
            (HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbProximal),
            (HumanBodyBones.RightThumbDistal, HumanBodyBones.RightThumbIntermediate),
            (HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftHand),
            (HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexProximal),
            (HumanBodyBones.LeftIndexDistal, HumanBodyBones.LeftIndexIntermediate),
            (HumanBodyBones.RightIndexProximal, HumanBodyBones.RightHand),
            (HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexProximal),
            (HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate),
            (HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftHand),
            (HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleProximal),
            (HumanBodyBones.LeftMiddleDistal, HumanBodyBones.LeftMiddleIntermediate),
            (HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightHand),
            (HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleProximal),
            (HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleIntermediate),
            (HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftHand),
            (HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingProximal),
            (HumanBodyBones.LeftRingDistal, HumanBodyBones.LeftRingIntermediate),
            (HumanBodyBones.RightRingProximal, HumanBodyBones.RightHand),
            (HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingProximal),
            (HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingIntermediate),
            (HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftHand),
            (HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleProximal),
            (HumanBodyBones.LeftLittleDistal, HumanBodyBones.LeftLittleIntermediate),
            (HumanBodyBones.RightLittleProximal, HumanBodyBones.RightHand),
            (HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleProximal),
            (HumanBodyBones.RightLittleDistal, HumanBodyBones.RightLittleIntermediate),
        };

        private static INDMFPlatformProvider[] SourceProviders => new[]
        {
            PlatformRegistry.PlatformProviders[WellKnownPlatforms.VRChatAvatar30],
            // PlatformRegistry.PlatformProviders[NdmfVrmExporterPlatform.Instance.QualifiedName],
        };

        private static gltf.ObjectID FindHumanBodyNodeIndex(uint hipNodeId, HumanBodyBones bone)
        {
            var index = (uint)HumanBodyBoneNames.FindIndex((name) => name.Item1 == bone);
            return new gltf.ObjectID(hipNodeId + index);
        }

        private static GameObject CreateRootGameObject([CallerMemberName] string name = "")
        {
            return new GameObject
            {
                name = name
            };
        }

        private static void SetupDummyHumanoidAvatar(GameObject root, uint rootNodeId,
            IDictionary<Transform, gltf.ObjectID> nodes)
        {
            var human = new List<HumanBone>();
            var skeleton = new List<SkeletonBone>()
            {
                new()
                {
                    name = root.name,
                    position = root.transform.position,
                    rotation = root.transform.rotation,
                    scale = root.transform.lossyScale,
                }
            };
            var humanBoneMappings = new Dictionary<HumanBodyBones, string>();
            for (var i = 0; i < HumanTrait.BoneCount; i++)
            {
                humanBoneMappings.Add((HumanBodyBones)i, HumanTrait.BoneName[i]);
            }

            var parents = new Dictionary<HumanBodyBones, Transform>();
            foreach (var (boneName, parentBone) in HumanBodyBoneNames)
            {
                var parent = root.transform;
                if (parentBone.HasValue)
                {
                    parents.TryGetValue(parentBone.Value, out parent);
                }

                var child = new GameObject
                {
                    name = boneName.ToString(),
                    transform =
                    {
                        parent = parent,
                        localPosition = Vector3.zero,
                        localRotation = Quaternion.identity,
                        localScale = Vector3.one,
                    }
                };
                human.Add(new HumanBone
                {
                    boneName = child.name,
                    humanName = humanBoneMappings[boneName],
                    limit = new HumanLimit
                    {
                        useDefaultValues = true
                    }
                });
                skeleton.Add(new SkeletonBone
                {
                    name = child.name,
                    position = child.transform.localPosition,
                    rotation = child.transform.localRotation,
                    scale = child.transform.localScale,
                });
                nodes.Add(child.transform, new gltf.ObjectID(rootNodeId++));
                parents.Add(boneName, child.transform);
            }

            var animator = root.AddComponent<Animator>();
            var humanoid = new HumanDescription
            {
                armStretch = 0.05f,
                feetSpacing = 0,
                human = human.ToArray(),
                hasTranslationDoF = false,
                legStretch = 0.05f,
                lowerArmTwist = 0.5f,
                lowerLegTwist = 0.5f,
                skeleton = skeleton.ToArray(),
                upperArmTwist = 0.5f,
                upperLegTwist = 0.5f,
            };
            animator.avatar = AvatarBuilder.BuildHumanAvatar(root, humanoid);
        }

        private static gltf.Root NewEmptyGltfRoot()
        {
            return new gltf.Root
            {
                Accessors = new List<gltf.accessor.Accessor>(),
                Asset = new gltf.asset.Asset
                {
                    Version = "2.0",
                },
                Buffers = new List<gltf.buffer.Buffer>(),
                BufferViews = new List<gltf.buffer.BufferView>(),
                Extensions = new Dictionary<string, JToken>(),
                ExtensionsUsed = new List<string>(),
                Images = new List<gltf.buffer.Image>(),
                Materials = new List<gltf.material.Material>(),
                Meshes = new List<gltf.mesh.Mesh>(),
                Nodes = new List<gltf.node.Node>(),
                Samplers = new List<gltf.material.Sampler>(),
                Scenes = new List<gltf.scene.Scene>(),
                Scene = new gltf.ObjectID(0),
                Skins = new List<gltf.node.Skin>(),
                Textures = new List<gltf.material.Texture>(),
            };
        }

        [TestCase(null, gltf.material.AlphaMode.Opaque, null)]
        [TestCase("Transparent", gltf.material.AlphaMode.Blend, null)]
        [TestCase("TransparentCutout", gltf.material.AlphaMode.Mask, 0.5f)]
        public void ExportGltfMaterial(string tag, gltf.material.AlphaMode alphaMode, float? alphaCutoff)
        {
            var root = CreateRootGameObject();
            root.AddComponent<NdmfVrmExporterComponent>();
            var extensionUsed = new HashSet<string>();
            var shader = Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = "Material(Clone)"
            };
            if (tag != null)
            {
                material.SetOverrideTag("RenderType", tag);
            }

            material.SetFloat($"_BumpScale", 0.5f);
            if (alphaCutoff.HasValue)
            {
                material.SetFloat($"_Cutoff", alphaCutoff.Value);
            }

            material.SetInt($"_CullMode", 0);
            material.SetColor($"_Color", Color.magenta);
            material.SetColor($"_EmissionColor", Color.white);
            material.SetTexture($"_MainTex", Texture2D.whiteTexture);
            material.SetTexture($"_EmissionMap", Texture2D.blackTexture);
            material.SetTexture($"_BumpMap", Texture2D.normalTexture);
            material.SetTexture($"_OcclusionMap", Texture2D.grayTexture);
            material.SetTexture($"_MetallicGlossMap", Texture2D.redTexture);
            var gltfRoot = NewEmptyGltfRoot();
            var gltfExporter = new gltf.exporter.Exporter();
            var materialExporter = new NdmfVrmExporter.GltfMaterialExporter(gltfRoot, gltfExporter, extensionUsed);
            var overrides = new NdmfVrmExporter.GltfMaterialExporter.ExportOverrides
            {
                EnableNormalMap = true,
                EmissiveStrength = 1,
            };
            var m = materialExporter.Export(material, overrides);
            Assert.That(JsonConvert.SerializeObject(m), Is.EqualTo(JsonConvert.SerializeObject(
                new gltf.material.Material
                {
                    AlphaMode = alphaMode,
                    AlphaCutoff = alphaCutoff,
                    DoubleSided = true,
                    EmissiveFactor = System.Numerics.Vector3.One,
                    EmissiveTexture = new gltf.material.TextureInfo
                    {
                        Index = new gltf.ObjectID(1),
                        Extensions = new Dictionary<string, JToken>(),
                    },
                    NormalTexture = new gltf.material.NormalTextureInfo
                    {
                        Index = new gltf.ObjectID(2),
                        Scale = 0.5f,
                        Extensions = new Dictionary<string, JToken>(),
                    },
                    OcclusionTexture = new gltf.material.OcclusionTextureInfo
                    {
                        Index = new gltf.ObjectID(3),
                        Strength = 1,
                        Extensions = new Dictionary<string, JToken>(),
                    },
                    PbrMetallicRoughness = new gltf.material.PbrMetallicRoughness
                    {
                        BaseColorFactor = new System.Numerics.Vector4(1, 0, 1, 1),
                        BaseColorTexture = new gltf.material.TextureInfo
                        {
                            Index = new gltf.ObjectID(0),
                            Extensions = new Dictionary<string, JToken>(),
                        },
                        MetallicRoughnessTexture = new gltf.material.TextureInfo
                        {
                            Index = new gltf.ObjectID(4),
                            Extensions = new Dictionary<string, JToken>(),
                        },
                        MetallicFactor = 1,
                        RoughnessFactor = 1,
                    },
                    Name = new gltf.UnicodeString("Material"),
                })));
        }

        [Test]
        public void EmissiveStrengthExtension()
        {
            var root = CreateRootGameObject();
            root.AddComponent<NdmfVrmExporterComponent>();
            var extensionUsed = new HashSet<string>();
            var shader = Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = "Material(Clone)"
            };
            var color = new Color(1.5f, 1.5f, 1.5f);
            var emissiveStrength = color.linear.r;
            material.SetColor($"_EmissionColor", color);
            material.SetFloat($"_Metallic", 0.5f);
            material.SetFloat($"_Glossiness", 0.5f);
            material.SetKeyword(new LocalKeyword(shader, "_EMISSION"), true);

            var gltfRoot = NewEmptyGltfRoot();
            var gltfExporter = new gltf.exporter.Exporter();
            var materialExporter = new NdmfVrmExporter.GltfMaterialExporter(gltfRoot, gltfExporter, extensionUsed);
            var overrides = new NdmfVrmExporter.GltfMaterialExporter.ExportOverrides()
            {
                CullMode = 1,
            };
            var m = materialExporter.Export(material, overrides);
            Assert.That(JsonConvert.SerializeObject(m), Is.EqualTo(JsonConvert.SerializeObject(
                new gltf.material.Material
                {
                    AlphaMode = gltf.material.AlphaMode.Opaque,
                    DoubleSided = false,
                    EmissiveFactor = System.Numerics.Vector3.One,
                    PbrMetallicRoughness = new gltf.material.PbrMetallicRoughness
                    {
                        BaseColorFactor = new System.Numerics.Vector4(1, 1, 1, 1),
                    },
                    Name = new gltf.UnicodeString("Material"),
                    Extensions = new Dictionary<string, JToken>()
                    {
                        {
                            gltf.extensions.KhrMaterialsEmissiveStrength.Name,
                            gltf.Document.SaveAsNode(new gltf.extensions.KhrMaterialsEmissiveStrength
                            {
                                EmissiveStrength = emissiveStrength,
                            })
                        },
                    }
                })));
        }

        [TestCase(-1, 0, 0, "PositiveX")]
        [TestCase(1, 0, 0, "NegativeX")]
        [TestCase(0, 1, 0, "PositiveY")]
        [TestCase(0, -1, 0, "NegativeY")]
        [TestCase(0, 0, 1, "PositiveZ")]
        [TestCase(0, 0, -1, "NegativeZ")]
        public void ExportNodeConstraintFromAimConstraint(float x, float y, float z, string expectedAimAxis)
        {
            var root = CreateRootGameObject();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var sourceNode = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var sourceNodeIgnored = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var nodeId = new gltf.ObjectID(42);
            nodes.Add(sourceNode.transform, nodeId);
            nodes.Add(sourceNodeIgnored.transform, new gltf.ObjectID(43));
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var node = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var constraint = node.AddComponent<AimConstraint>();
            constraint.constraintActive = true;
            constraint.weight = 0.5f;
            constraint.aimVector = new Vector3(x, y, z);
            constraint.SetSources(new List<ConstraintSource>
            {
                new()
                {
                    sourceTransform = sourceNode.transform,
                    weight = 0.5f,
                },
                new()
                {
                    sourceTransform = sourceNodeIgnored.transform,
                    weight = 1.0f,
                },
            });
            var nodeConstraint = exporter.ExportNodeConstraint(node.transform, gltf.ObjectID.Null);
            Assert.That(JsonConvert.SerializeObject(nodeConstraint), Is.EqualTo(JsonConvert.SerializeObject(
                new vrm.constraint.NodeConstraint
                {
                    SpecVersion = "1.0",
                    Constraint = new vrm.constraint.Constraint
                    {
                        Aim = new vrm.constraint.AimConstraint
                        {
                            Source = nodeId,
                            AimAxis = expectedAimAxis,
                            Weight = 0.25f,
                        }
                    }
                })));
        }

        [Test]
        public void ExportNodeConstraintFromRotationConstraint()
        {
            var root = CreateRootGameObject();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var sourceNode = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var sourceNodeIgnored = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var nodeId = new gltf.ObjectID(42);
            nodes.Add(sourceNode.transform, nodeId);
            nodes.Add(sourceNodeIgnored.transform, new gltf.ObjectID(43));
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var node = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var constraint = node.AddComponent<RotationConstraint>();
            constraint.constraintActive = true;
            constraint.weight = 0.5f;
            constraint.rotationAxis = Axis.X | Axis.Y | Axis.Z;
            constraint.SetSources(new List<ConstraintSource>
            {
                new()
                {
                    sourceTransform = sourceNode.transform,
                    weight = 0.5f,
                },
                new()
                {
                    sourceTransform = sourceNodeIgnored.transform,
                    weight = 1.0f,
                },
            });
            var nodeConstraint = exporter.ExportNodeConstraint(node.transform, gltf.ObjectID.Null);
            Assert.That(JsonConvert.SerializeObject(nodeConstraint), Is.EqualTo(JsonConvert.SerializeObject(
                new vrm.constraint.NodeConstraint
                {
                    SpecVersion = "1.0",
                    Constraint = new vrm.constraint.Constraint
                    {
                        Rotation = new vrm.constraint.RotationConstraint
                        {
                            Source = nodeId,
                            Weight = 0.25f,
                        }
                    }
                })));
        }

        [TestCase(Axis.X, "X")]
        [TestCase(Axis.Y, "Y")]
        [TestCase(Axis.Z, "Z")]
        public void ExportNodeConstraintFromRollConstraint(Axis axis, string rollAxis)
        {
            var root = CreateRootGameObject();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var sourceNode = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var sourceNodeIgnored = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var nodeId = new gltf.ObjectID(42);
            nodes.Add(sourceNode.transform, nodeId);
            nodes.Add(sourceNodeIgnored.transform, new gltf.ObjectID(43));
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var node = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var constraint = node.AddComponent<RotationConstraint>();
            constraint.constraintActive = true;
            constraint.weight = 0.5f;
            constraint.rotationAxis = axis;
            constraint.SetSources(new List<ConstraintSource>
            {
                new()
                {
                    sourceTransform = sourceNode.transform,
                    weight = 0.5f,
                },
                new()
                {
                    sourceTransform = sourceNodeIgnored.transform,
                    weight = 1.0f,
                },
            });
            var nodeConstraint = exporter.ExportNodeConstraint(node.transform, gltf.ObjectID.Null);
            Assert.That(JsonConvert.SerializeObject(nodeConstraint), Is.EqualTo(JsonConvert.SerializeObject(
                new vrm.constraint.NodeConstraint
                {
                    SpecVersion = "1.0",
                    Constraint = new vrm.constraint.Constraint
                    {
                        Roll = new vrm.constraint.RollConstraint
                        {
                            RollAxis = rollAxis,
                            Source = nodeId,
                            Weight = 0.25f,
                        }
                    }
                })));
        }

        [Test]
        public void ExportNodeConstraintFromParentConstraint()
        {
            var root = CreateRootGameObject();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var nodeId = new gltf.ObjectID(42);
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var node = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var constraint = node.AddComponent<ParentConstraint>();
            constraint.constraintActive = true;
            var nodeConstraint = exporter.ExportNodeConstraint(node.transform, nodeId);
            Assert.That(JsonConvert.SerializeObject(nodeConstraint), Is.EqualTo(JsonConvert.SerializeObject(
                new vrm.constraint.NodeConstraint
                {
                    SpecVersion = "1.0",
                    Constraint = new vrm.constraint.Constraint
                    {
                        Rotation = new vrm.constraint.RotationConstraint
                        {
                            Source = nodeId,
                        }
                    }
                })));
        }

#if NVE_HAS_VRCHAT_AVATAR_SDK
        [Test]
        public void ExportHumanoid()
        {
            const uint rootNodeId = 42u;
            var thumbnailNodeId = new gltf.ObjectID(1023);
            var visemeNodeId = new gltf.ObjectID(rootNodeId);
            var root = new GameObject
            {
                name = "Humanoid(Clone)",
                transform =
                {
                    // verify root position is expected to be zero for https://github.com/hkrn/ndmf-vrm-exporter/issues/73
                    position = new Vector3(10, 10, 10)
                }
            };
            var nodes = new Dictionary<Transform, gltf.ObjectID>
            {
                { root.transform, new gltf.ObjectID(rootNodeId) }
            };
            const uint hipNodeId = rootNodeId + 1;
            SetupDummyHumanoidAvatar(root, hipNodeId, nodes);
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var blendShapeIndex = 0;
            var vrmComponent = root.AddComponent<NdmfVrmExporterComponent>();
            vrmComponent.version = "1.0";
            vrmComponent.authors = new List<string>
            {
                "foo",
                string.Empty,
                "bar",
                "   ",
                "baz",
            };
            vrmComponent.references = new List<string>()
            {
                "baz",
                "   ",
                "bar",
                string.Empty,
                "foo",
            };
            vrmComponent.copyrightInformation = "(c) ndmf-vrm-exporter";
            vrmComponent.contactInformation = "https://github.com/hkrn/ndmf-vrm-exporter";
            vrmComponent.thirdPartyLicenses = "MPL";
            vrmComponent.otherLicenseUrl = "https://www.mozilla.org/en-US/MPL/2.0/";
            vrmComponent.expressionPresetAngryBlendShape.baseType = VrmExpressionProperty.BaseType.BlendShape;
            vrmComponent.expressionPresetAngryBlendShape.blendShapeName = nameof(vrm.core.Preset.Angry);
            vrmComponent.expressionPresetAngryBlendShape.overrideBlink = vrm.core.ExpressionOverrideType.None;
            vrmComponent.expressionPresetAngryBlendShape.overrideLookAt = vrm.core.ExpressionOverrideType.None;
            vrmComponent.expressionPresetAngryBlendShape.overrideMouth = vrm.core.ExpressionOverrideType.None;
            vrmComponent.expressionPresetHappyBlendShape.baseType = VrmExpressionProperty.BaseType.BlendShape;
            vrmComponent.expressionPresetHappyBlendShape.blendShapeName = nameof(vrm.core.Preset.Happy);
            vrmComponent.expressionPresetHappyBlendShape.overrideBlink = vrm.core.ExpressionOverrideType.Block;
            vrmComponent.expressionPresetHappyBlendShape.overrideLookAt = vrm.core.ExpressionOverrideType.Block;
            vrmComponent.expressionPresetHappyBlendShape.overrideMouth = vrm.core.ExpressionOverrideType.Block;
            vrmComponent.expressionPresetRelaxedBlendShape.baseType = VrmExpressionProperty.BaseType.BlendShape;
            vrmComponent.expressionPresetRelaxedBlendShape.blendShapeName = nameof(vrm.core.Preset.Relaxed);
            vrmComponent.expressionPresetRelaxedBlendShape.overrideBlink = vrm.core.ExpressionOverrideType.Blend;
            vrmComponent.expressionPresetRelaxedBlendShape.overrideLookAt = vrm.core.ExpressionOverrideType.Blend;
            vrmComponent.expressionPresetRelaxedBlendShape.overrideMouth = vrm.core.ExpressionOverrideType.Blend;
            vrmComponent.expressionPresetSadBlendShape.baseType = VrmExpressionProperty.BaseType.AnimationClip;
            var animationClip = new AnimationClip();
            var curve = new AnimationCurve(new Keyframe[]
            {
                new(0, 50),
                new(1, 100)
            });
            var binding = new EditorCurveBinding
            {
                path = root.name,
                type = typeof(SkinnedMeshRenderer),
                propertyName = $"blendShape.{nameof(vrm.core.Preset.Sad)}"
            };
            AnimationUtility.SetEditorCurve(animationClip, binding, curve);
            vrmComponent.expressionPresetSadBlendShape.blendShapeAnimationClip = animationClip;
            vrmComponent.expressionPresetSadBlendShape.blendShapeName = nameof(vrm.core.Preset.Sad);
            vrmComponent.expressionPresetSurprisedBlendShape.baseType = VrmExpressionProperty.BaseType.BlendShape;
            vrmComponent.expressionPresetSurprisedBlendShape.blendShapeName = nameof(vrm.core.Preset.Surprised);
            vrmComponent.expressionCustomBlendShapes.Add(new VrmExpressionProperty
            {
                baseType = VrmExpressionProperty.BaseType.BlendShape,
                blendShapeName = "Custom",
                expressionName = "Custom",
            });
            var vrchatComponent = root.AddComponent<VRCAvatarDescriptor>();
            vrchatComponent.lipSync = VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;
            vrchatComponent.VisemeBlendShapes = new string[(uint)VRC_AvatarDescriptor.Viseme.Count];
            vrchatComponent.VisemeBlendShapes[(uint)VRC_AvatarDescriptor.Viseme.aa] =
                nameof(vrm.core.Preset.Aa);
            vrchatComponent.VisemeBlendShapes[(uint)VRC_AvatarDescriptor.Viseme.ih] =
                nameof(vrm.core.Preset.Ih);
            vrchatComponent.VisemeBlendShapes[(uint)VRC_AvatarDescriptor.Viseme.ou] =
                nameof(vrm.core.Preset.Ou);
            vrchatComponent.VisemeBlendShapes[(uint)VRC_AvatarDescriptor.Viseme.E] =
                nameof(vrm.core.Preset.Ee);
            vrchatComponent.VisemeBlendShapes[(uint)VRC_AvatarDescriptor.Viseme.oh] =
                nameof(vrm.core.Preset.Oh);
            var skinnedMeshRenderer = root.AddComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.sharedMesh = new Mesh();
            vrchatComponent.VisemeSkinnedMesh = skinnedMeshRenderer;
            var presetBlendShapeNames = new[]
            {
                nameof(vrm.core.Preset.Aa),
                nameof(vrm.core.Preset.Ih),
                nameof(vrm.core.Preset.Ou),
                nameof(vrm.core.Preset.Ee),
                nameof(vrm.core.Preset.Oh),
                nameof(vrm.core.Preset.Angry),
                nameof(vrm.core.Preset.Happy),
                nameof(vrm.core.Preset.Relaxed),
                nameof(vrm.core.Preset.Sad),
                nameof(vrm.core.Preset.Surprised),
                nameof(vrm.core.Preset.Blink),
                nameof(vrm.core.Preset.LookUp),
                nameof(vrm.core.Preset.LookDown),
                "Custom",
            };
            foreach (var blendShapeName in presetBlendShapeNames)
            {
                skinnedMeshRenderer.sharedMesh.AddBlendShapeFrame(blendShapeName, 0, new Vector3[] { },
                    new Vector3[] { }, new Vector3[] { });
                allMorphTargets.Add(blendShapeName, (visemeNodeId, blendShapeIndex++));
            }

            vrchatComponent.enableEyeLook = true;
            vrchatComponent.customEyeLookSettings = new VRCAvatarDescriptor.CustomEyeLookSettings()
            {
                eyelidType = VRCAvatarDescriptor.EyelidType.Blendshapes,
                eyelidsBlendshapes = new[]
                {
                    Array.IndexOf(presetBlendShapeNames, nameof(vrm.core.Preset.Blink)),
                    Array.IndexOf(presetBlendShapeNames, nameof(vrm.core.Preset.LookUp)),
                    Array.IndexOf(presetBlendShapeNames, nameof(vrm.core.Preset.LookDown)),
                },
                eyesLookingUp = new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations
                {
                    left = Quaternion.Euler(8, 0, 0),
                    right = Quaternion.Euler(4, 0, 0),
                },
                eyesLookingDown = new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations
                {
                    left = Quaternion.Euler(12, 0, 0),
                    right = Quaternion.Euler(6, 0, 0),
                },
                eyesLookingLeft = new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations
                {
                    left = Quaternion.Euler(0, 16, 0),
                    right = Quaternion.Euler(0, 8, 0),
                },
                eyesLookingRight = new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations
                {
                    left = Quaternion.Euler(0, 24, 0),
                    right = Quaternion.Euler(0, 12, 0),
                },
            };

            var assetSaver = new NullAssetSaver();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var core = exporter.ExportCore(thumbnailNodeId);
            Assert.That(core, Is.Not.Null);
            Assert.That(JsonConvert.SerializeObject(core.Humanoid), Is.EqualTo(JsonConvert.SerializeObject(
                new vrm.core.Humanoid
                {
                    HumanBones = new vrm.core.HumanBones
                    {
                        Hips = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.Hips)
                        },
                        LeftUpperLeg = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftUpperLeg)
                        },
                        RightUpperLeg = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightUpperLeg)
                        },
                        LeftLowerLeg = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftLowerLeg)
                        },
                        RightLowerLeg = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightLowerLeg)
                        },
                        LeftFoot = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftFoot)
                        },
                        RightFoot = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightFoot)
                        },
                        LeftToes = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftToes)
                        },
                        RightToes = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightToes)
                        },
                        Spine = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.Spine)
                        },
                        Chest = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.Chest)
                        },
                        UpperChest = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.UpperChest)
                        },
                        Neck = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.Neck)
                        },
                        Head = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.Head)
                        },
                        Jaw = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.Jaw)
                        },
                        LeftEye = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftEye)
                        },
                        RightEye = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightEye)
                        },
                        LeftUpperArm = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftUpperArm)
                        },
                        RightUpperArm = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightUpperArm)
                        },
                        LeftLowerArm = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftLowerArm)
                        },
                        RightLowerArm = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightLowerArm)
                        },
                        LeftHand = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftHand)
                        },
                        RightHand = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightHand)
                        },
                        LeftThumbMetacarpal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftThumbProximal)
                        },
                        RightThumbMetacarpal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightThumbProximal)
                        },
                        LeftThumbProximal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftThumbIntermediate)
                        },
                        RightThumbProximal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightThumbIntermediate)
                        },
                        LeftThumbDistal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftThumbDistal)
                        },
                        RightThumbDistal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightThumbDistal)
                        },
                        LeftIndexProximal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftIndexProximal)
                        },
                        RightIndexProximal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightIndexProximal)
                        },
                        LeftIndexIntermediate = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftIndexIntermediate)
                        },
                        RightIndexIntermediate = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightIndexIntermediate)
                        },
                        LeftIndexDistal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftIndexDistal)
                        },
                        RightIndexDistal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightIndexDistal)
                        },
                        LeftMiddleProximal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftMiddleProximal)
                        },
                        RightMiddleProximal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightMiddleProximal)
                        },
                        LeftMiddleIntermediate = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftMiddleIntermediate)
                        },
                        RightMiddleIntermediate = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightMiddleIntermediate)
                        },
                        LeftMiddleDistal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftMiddleDistal)
                        },
                        RightMiddleDistal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightMiddleDistal)
                        },
                        LeftRingProximal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftRingProximal)
                        },
                        RightRingProximal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightRingProximal)
                        },
                        LeftRingIntermediate = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftRingIntermediate)
                        },
                        RightRingIntermediate = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightRingIntermediate)
                        },
                        LeftRingDistal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftRingDistal)
                        },
                        RightRingDistal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightRingDistal)
                        },
                        LeftLittleProximal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftLittleProximal)
                        },
                        RightLittleProximal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightLittleProximal)
                        },
                        LeftLittleIntermediate = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftLittleIntermediate)
                        },
                        RightLittleIntermediate = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightLittleIntermediate)
                        },
                        LeftLittleDistal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.LeftLittleDistal)
                        },
                        RightLittleDistal = new vrm.core.HumanBone
                        {
                            Node = FindHumanBodyNodeIndex(hipNodeId, HumanBodyBones.RightLittleDistal)
                        },
                    }
                })));
            Assert.That(JsonConvert.SerializeObject(core.Meta), Is.EqualTo(JsonConvert.SerializeObject(
                new vrm.core.Meta
                {
                    Name = "Humanoid",
                    Version = "1.0",
                    Authors = new List<string>
                    {
                        "foo",
                        "bar",
                        "baz"
                    },
                    References = new List<string>
                    {
                        "baz",
                        "bar",
                        "foo",
                    },
                    ThumbnailImage = thumbnailNodeId,
                    CopyrightInformation = "(c) ndmf-vrm-exporter",
                    ContactInformation = "https://github.com/hkrn/ndmf-vrm-exporter",
                    LicenseUrl = vrm.core.Meta.DefaultLicenseUrl,
                    ThirdPartyLicenses = "MPL",
                    OtherLicenseUrl = "https://www.mozilla.org/en-US/MPL/2.0/",
                    AvatarPermission = vrm.core.AvatarPermission.OnlyAuthor,
                    AllowAntisocialOrHateUsage = false,
                    AllowExcessivelySexualUsage = false,
                    AllowExcessivelyViolentUsage = false,
                    AllowPoliticalOrReligiousUsage = false,
                    AllowRedistribution = false,
                    CommercialUsage = vrm.core.CommercialUsage.PersonalNonProfit,
                    CreditNotation = vrm.core.CreditNotation.Required,
                    Modification = vrm.core.Modification.Prohibited,
                })));
            Assert.That(JsonConvert.SerializeObject(core.Expressions), Is.EqualTo(JsonConvert.SerializeObject(
                new vrm.core.Expressions
                {
                    Preset = new vrm.core.Preset
                    {
                        Aa = new vrm.core.ExpressionItem
                        {
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = visemeNodeId,
                                    Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames,
                                        nameof(vrm.core.Preset.Aa))),
                                    Weight = 1,
                                }
                            },
                        },
                        Ih = new vrm.core.ExpressionItem
                        {
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = visemeNodeId,
                                    Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames,
                                        nameof(vrm.core.Preset.Ih))),
                                    Weight = 1,
                                }
                            },
                        },
                        Ou = new vrm.core.ExpressionItem
                        {
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = visemeNodeId,
                                    Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames,
                                        nameof(vrm.core.Preset.Ou))),
                                    Weight = 1,
                                }
                            },
                        },
                        Ee = new vrm.core.ExpressionItem
                        {
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = visemeNodeId,
                                    Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames,
                                        nameof(vrm.core.Preset.Ee))),
                                    Weight = 1,
                                }
                            },
                        },
                        Oh = new vrm.core.ExpressionItem
                        {
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = visemeNodeId,
                                    Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames,
                                        nameof(vrm.core.Preset.Oh))),
                                    Weight = 1,
                                }
                            },
                        },
                        Angry = new vrm.core.ExpressionItem
                        {
                            OverrideBlink = vrm.core.ExpressionOverrideType.None,
                            OverrideLookAt = vrm.core.ExpressionOverrideType.None,
                            OverrideMouth = vrm.core.ExpressionOverrideType.None,
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = visemeNodeId,
                                    Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames,
                                        nameof(vrm.core.Preset.Angry))),
                                    Weight = 1,
                                }
                            },
                        },
                        Happy = new vrm.core.ExpressionItem
                        {
                            OverrideBlink = vrm.core.ExpressionOverrideType.Block,
                            OverrideLookAt = vrm.core.ExpressionOverrideType.Block,
                            OverrideMouth = vrm.core.ExpressionOverrideType.Block,
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = visemeNodeId,
                                    Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames,
                                        nameof(vrm.core.Preset.Happy))),
                                    Weight = 1,
                                }
                            },
                        },
                        Relaxed = new vrm.core.ExpressionItem
                        {
                            OverrideBlink = vrm.core.ExpressionOverrideType.Blend,
                            OverrideLookAt = vrm.core.ExpressionOverrideType.Blend,
                            OverrideMouth = vrm.core.ExpressionOverrideType.Blend,
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = visemeNodeId,
                                    Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames,
                                        nameof(vrm.core.Preset.Relaxed))),
                                    Weight = 1,
                                }
                            },
                        },
                        Sad = new vrm.core.ExpressionItem
                        {
                            OverrideBlink = vrm.core.ExpressionOverrideType.None,
                            OverrideLookAt = vrm.core.ExpressionOverrideType.None,
                            OverrideMouth = vrm.core.ExpressionOverrideType.None,
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = visemeNodeId,
                                    Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames,
                                        nameof(vrm.core.Preset.Sad))),
                                    Weight = 0.5f,
                                }
                            },
                        },
                        Surprised = new vrm.core.ExpressionItem
                        {
                            OverrideBlink = vrm.core.ExpressionOverrideType.None,
                            OverrideLookAt = vrm.core.ExpressionOverrideType.None,
                            OverrideMouth = vrm.core.ExpressionOverrideType.None,
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = visemeNodeId,
                                    Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames,
                                        nameof(vrm.core.Preset.Surprised))),
                                    Weight = 1,
                                }
                            },
                        },
                        Blink = new vrm.core.ExpressionItem
                        {
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = visemeNodeId,
                                    Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames,
                                        nameof(vrm.core.Preset.Blink))),
                                    Weight = 1,
                                }
                            },
                        },
                        LookUp = new vrm.core.ExpressionItem
                        {
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = visemeNodeId,
                                    Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames,
                                        nameof(vrm.core.Preset.LookUp))),
                                    Weight = 1,
                                }
                            },
                        },
                        LookDown = new vrm.core.ExpressionItem
                        {
                            MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = visemeNodeId,
                                    Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames,
                                        nameof(vrm.core.Preset.LookDown))),
                                    Weight = 1,
                                }
                            },
                        }
                    },
                    Custom = new Dictionary<gltf.UnicodeString, vrm.core.ExpressionItem>()
                    {
                        {
                            new gltf.UnicodeString("Custom"),
                            new vrm.core.ExpressionItem
                            {
                                OverrideBlink = vrm.core.ExpressionOverrideType.None,
                                OverrideLookAt = vrm.core.ExpressionOverrideType.None,
                                OverrideMouth = vrm.core.ExpressionOverrideType.None,
                                MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                                {
                                    new()
                                    {
                                        Node = visemeNodeId,
                                        Index = new gltf.ObjectID((uint)Array.IndexOf(presetBlendShapeNames, "Custom")),
                                        Weight = 1,
                                    }
                                },
                            }
                        }
                    }
                })));
            Assert.That(core.LookAt, Is.Not.Null);
            Assert.That(core.LookAt.Type, Is.EqualTo(vrm.core.LookAtType.Bone));
            Assert.That(core.LookAt.OffsetFromHeadBone, Is.EqualTo(new System.Numerics.Vector3(vrchatComponent.ViewPosition.x,
                vrchatComponent.ViewPosition.y, vrchatComponent.ViewPosition.z)));
            Assert.That(core.LookAt.RangeMapVerticalUp, Is.Not.Null);
            Assert.That(core.LookAt.RangeMapVerticalUp.InputMaxValue, Is.EqualTo(4.0f).Within(3).Ulps);
            Assert.That(core.LookAt.RangeMapVerticalUp.OutputScale, Is.EqualTo(1));
            Assert.That(core.LookAt.RangeMapVerticalDown, Is.Not.Null);
            Assert.That(core.LookAt.RangeMapVerticalDown.InputMaxValue, Is.EqualTo(6.0f).Within(3).Ulps);
            Assert.That(core.LookAt.RangeMapVerticalDown.OutputScale, Is.EqualTo(1));
            Assert.That(core.LookAt.RangeMapHorizontalInner, Is.Not.Null);
            Assert.That(core.LookAt.RangeMapHorizontalInner.InputMaxValue, Is.EqualTo(8.0f).Within(3).Ulps);
            Assert.That(core.LookAt.RangeMapHorizontalInner.OutputScale, Is.EqualTo(1));
            Assert.That(core.LookAt.RangeMapHorizontalOuter, Is.Not.Null);
            Assert.That(core.LookAt.RangeMapHorizontalOuter.InputMaxValue, Is.EqualTo(12.0f).Within(3).Ulps);
            Assert.That(core.LookAt.RangeMapHorizontalOuter.OutputScale, Is.EqualTo(1));
        }

        [TestCase(-1, 0, 0, "PositiveX")]
        [TestCase(1, 0, 0, "NegativeX")]
        [TestCase(0, 1, 0, "PositiveY")]
        [TestCase(0, -1, 0, "NegativeY")]
        [TestCase(0, 0, 1, "PositiveZ")]
        [TestCase(0, 0, -1, "NegativeZ")]
        public void ExportNodeConstraintFromVrcAimConstraint(float x, float y, float z, string expectedAimAxis)
        {
            var root = CreateRootGameObject();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var sourceNode = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var sourceNodeIgnored = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var nodeId = new gltf.ObjectID(42);
            nodes.Add(sourceNode.transform, nodeId);
            nodes.Add(sourceNodeIgnored.transform, new gltf.ObjectID(43));
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var node = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var constraint = node.AddComponent<VRCAimConstraint>();
            constraint.ActivateConstraint();
            constraint.GlobalWeight = 0.5f;
            constraint.AimAxis = new Vector3(x, y, z);
            constraint.Sources.Add(new VRCConstraintSource(sourceNode.transform, 0.5f));
            constraint.Sources.Add(new VRCConstraintSource(sourceNodeIgnored.transform, 1.0f));
            var nodeConstraint = exporter.ExportNodeConstraint(node.transform, gltf.ObjectID.Null);
            Assert.That(JsonConvert.SerializeObject(nodeConstraint), Is.EqualTo(JsonConvert.SerializeObject(
                new vrm.constraint.NodeConstraint
                {
                    SpecVersion = "1.0",
                    Constraint = new vrm.constraint.Constraint
                    {
                        Aim = new vrm.constraint.AimConstraint
                        {
                            Source = nodeId,
                            AimAxis = expectedAimAxis,
                            Weight = 0.25f,
                        }
                    }
                })));
        }

        [Test]
        public void ExportNodeConstraintFromVrcRotationConstraint()
        {
            var root = CreateRootGameObject();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var sourceNode = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var sourceNodeIgnored = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var nodeId = new gltf.ObjectID(42);
            nodes.Add(sourceNode.transform, nodeId);
            nodes.Add(sourceNodeIgnored.transform, new gltf.ObjectID(43));
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var node = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var constraint = node.AddComponent<VRCRotationConstraint>();
            constraint.ActivateConstraint();
            constraint.AffectsRotationX = true;
            constraint.AffectsRotationY = true;
            constraint.AffectsRotationZ = true;
            constraint.GlobalWeight = 0.5f;
            constraint.Sources.Add(new VRCConstraintSource(sourceNode.transform, 0.5f));
            constraint.Sources.Add(new VRCConstraintSource(sourceNodeIgnored.transform, 1.0f));
            var nodeConstraint = exporter.ExportNodeConstraint(node.transform, gltf.ObjectID.Null);
            Assert.That(JsonConvert.SerializeObject(nodeConstraint), Is.EqualTo(JsonConvert.SerializeObject(
                new vrm.constraint.NodeConstraint
                {
                    SpecVersion = "1.0",
                    Constraint = new vrm.constraint.Constraint
                    {
                        Rotation = new vrm.constraint.RotationConstraint
                        {
                            Source = nodeId,
                            Weight = 0.25f,
                        }
                    }
                })));
        }

        [TestCase(true, false, false, "X")]
        [TestCase(false, true, false, "Y")]
        [TestCase(false, false, true, "Z")]
        public void ExportNodeConstraintFromVrcRollConstraint(bool affectX, bool affectY, bool affectZ, string rollAxis)
        {
            var root = CreateRootGameObject();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var sourceNode = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var sourceNodeIgnored = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var nodeId = new gltf.ObjectID(42);
            nodes.Add(sourceNode.transform, nodeId);
            nodes.Add(sourceNodeIgnored.transform, new gltf.ObjectID(43));
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var node = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var constraint = node.AddComponent<VRCRotationConstraint>();
            constraint.ActivateConstraint();
            constraint.GlobalWeight = 0.5f;
            constraint.AffectsRotationX = affectX;
            constraint.AffectsRotationY = affectY;
            constraint.AffectsRotationZ = affectZ;
            constraint.Sources.Add(new VRCConstraintSource(sourceNode.transform, 0.5f));
            constraint.Sources.Add(new VRCConstraintSource(sourceNodeIgnored.transform, 1.0f));
            var nodeConstraint = exporter.ExportNodeConstraint(node.transform, gltf.ObjectID.Null);
            Assert.That(JsonConvert.SerializeObject(nodeConstraint), Is.EqualTo(JsonConvert.SerializeObject(
                new vrm.constraint.NodeConstraint
                {
                    SpecVersion = "1.0",
                    Constraint = new vrm.constraint.Constraint
                    {
                        Roll = new vrm.constraint.RollConstraint
                        {
                            RollAxis = rollAxis,
                            Source = nodeId,
                            Weight = 0.25f,
                        }
                    }
                })));
        }

        [Test]
        public void ExportNodeConstraintFromVrcParentConstraint()
        {
            var root = CreateRootGameObject();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var nodeId = new gltf.ObjectID(42);
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var node = new GameObject
            {
                transform =
                {
                    parent = root.transform
                }
            };
            var constraint = node.AddComponent<VRCParentConstraint>();
            constraint.ActivateConstraint();
            var nodeConstraint = exporter.ExportNodeConstraint(node.transform, nodeId);
            Assert.That(JsonConvert.SerializeObject(nodeConstraint), Is.EqualTo(JsonConvert.SerializeObject(
                new vrm.constraint.NodeConstraint
                {
                    SpecVersion = "1.0",
                    Constraint = new vrm.constraint.Constraint
                    {
                        Rotation = new vrm.constraint.RotationConstraint
                        {
                            Source = nodeId,
                        }
                    }
                })));
        }

        [Test]
        public void ExportPhysBoneCollidersExcluded()
        {
            var root = CreateRootGameObject();
            var component = root.AddComponent<NdmfVrmExporterComponent>();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            {
                var invisible = new GameObject
                {
                    transform =
                    {
                        parent = root.transform
                    }
                };
                invisible.SetActive(false);
                invisible.AddComponent<VRCPhysBoneCollider>();
                nodes.Add(invisible.transform, new gltf.ObjectID(1));
            }
            {
                var excluded = new GameObject
                {
                    transform =
                    {
                        parent = root.transform
                    }
                };
                excluded.AddComponent<VRCPhysBoneCollider>();
                nodes.Add(excluded.transform, new gltf.ObjectID(2));
                component.excludedSpringBoneColliderTransforms.Add(excluded.transform);
            }
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var sb = exporter.ExportSpringBone();
            Assert.That(sb.Colliders, Is.Null);
            Assert.That(extensionUsed, Is.Empty);
        }

        [Test]
        public void ExportPhysBoneColliderCapsule()
        {
            var root = CreateRootGameObject();
            root.AddComponent<NdmfVrmExporterComponent>();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var nodeId = new gltf.ObjectID(42);
            {
                var capsule = new GameObject
                {
                    transform =
                    {
                        parent = root.transform
                    }
                };
                var collider = capsule.AddComponent<VRCPhysBoneCollider>();
                collider.shapeType = VRCPhysBoneColliderBase.ShapeType.Capsule;
                collider.position = new Vector3(0, 1, 0);
                collider.height = 1;
                collider.radius = 0.5f;
                nodes.Add(capsule.transform, nodeId);
            }
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var sb = exporter.ExportSpringBone();
            Assert.That(sb.Colliders, Is.Not.Null);
            Assert.That(JsonConvert.SerializeObject(sb.Colliders), Is.EqualTo(JsonConvert.SerializeObject(
                new List<vrm.sb.Collider>
                {
                    new()
                    {
                        Node = nodeId,
                        Shape = new vrm.sb.Shape
                        {
                            Capsule = new vrm.sb.Capsule
                            {
                                Offset = new System.Numerics.Vector3(0, 1, 0),
                                Tail = new System.Numerics.Vector3(0, 1, 0),
                                Radius = 0.5f,
                            }
                        }
                    }
                })));
            Assert.That(extensionUsed, Is.Empty);
        }

        [Test]
        public void ExportPhysBoneColliderSphere()
        {
            var root = CreateRootGameObject();
            root.AddComponent<NdmfVrmExporterComponent>();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var nodeId = new gltf.ObjectID(42);
            {
                var sphere = new GameObject
                {
                    transform =
                    {
                        parent = root.transform
                    }
                };
                var collider = sphere.AddComponent<VRCPhysBoneCollider>();
                collider.shapeType = VRCPhysBoneColliderBase.ShapeType.Sphere;
                collider.radius = 0.5f;
                nodes.Add(sphere.transform, nodeId);
            }
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var sb = exporter.ExportSpringBone();
            Assert.That(sb.Colliders, Is.Not.Null);
            Assert.That(JsonConvert.SerializeObject(sb.Colliders), Is.EqualTo(JsonConvert.SerializeObject(
                new List<vrm.sb.Collider>
                {
                    new()
                    {
                        Node = nodeId,
                        Shape = new vrm.sb.Shape
                        {
                            Sphere = new vrm.sb.Sphere
                            {
                                Offset = new System.Numerics.Vector3(0),
                                Radius = 0.5f,
                            }
                        }
                    }
                })));
            Assert.That(extensionUsed, Is.Empty);
        }

        [Test]
        public void ExportPhysBoneColliderInsideCapsule()
        {
            var root = CreateRootGameObject();
            root.AddComponent<NdmfVrmExporterComponent>();
            var assetSaver = new NullAssetSaver();
            var nodeId = new gltf.ObjectID(42);
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            {
                var capsule = new GameObject
                {
                    transform =
                    {
                        parent = root.transform
                    }
                };
                var collider = capsule.AddComponent<VRCPhysBoneCollider>();
                collider.shapeType = VRCPhysBoneColliderBase.ShapeType.Capsule;
                collider.position = new Vector3(0, 1, 0);
                collider.height = 1;
                collider.radius = 0.5f;
                collider.insideBounds = true;
                nodes.Add(capsule.transform, nodeId);
            }
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var sb = exporter.ExportSpringBone();
            Assert.That(sb.Colliders, Is.Not.Null);
            Assert.That(JsonConvert.SerializeObject(sb.Colliders), Is.EqualTo(JsonConvert.SerializeObject(
                new List<vrm.sb.Collider>
                {
                    new()
                    {
                        Node = nodeId,
                        Shape = new vrm.sb.Shape
                        {
                            Capsule = new vrm.sb.Capsule
                            {
                                Offset = new System.Numerics.Vector3(-10000),
                                Tail = new System.Numerics.Vector3(-10000),
                                Radius = 0.0f,
                            }
                        },
                        Extensions = new Dictionary<string, JToken>
                        {
                            {
                                "VRMC_springBone_extended_collider", vrm.Document.SaveAsNode(new vrm.sb.ExtendedCollider
                                {
                                    Shape = new vrm.sb.ExtendedShape
                                    {
                                        Capsule = new vrm.sb.ShapeCapsule
                                        {
                                            Offset = new System.Numerics.Vector3(0, 1, 0),
                                            Tail = new System.Numerics.Vector3(0, 1, 0),
                                            Radius = 0.5f,
                                            Inside = true
                                        }
                                    }
                                })
                            }
                        }
                    }
                })));
            Assert.That(extensionUsed, Does.Contain("VRMC_springBone_extended_collider"));
        }

        [Test]
        public void ExportPhysBoneColliderInsideSphere()
        {
            var root = CreateRootGameObject();
            root.AddComponent<NdmfVrmExporterComponent>();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var nodeId = new gltf.ObjectID(42);
            {
                var capsule = new GameObject
                {
                    transform =
                    {
                        parent = root.transform
                    }
                };
                var collider = capsule.AddComponent<VRCPhysBoneCollider>();
                collider.shapeType = VRCPhysBoneColliderBase.ShapeType.Sphere;
                collider.radius = 0.5f;
                collider.insideBounds = true;
                nodes.Add(capsule.transform, nodeId);
            }
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var sb = exporter.ExportSpringBone();
            Assert.That(sb.Colliders, Is.Not.Null);
            Assert.That(JsonConvert.SerializeObject(sb.Colliders), Is.EqualTo(JsonConvert.SerializeObject(
                new List<vrm.sb.Collider>
                {
                    new()
                    {
                        Node = nodeId,
                        Shape = new vrm.sb.Shape()
                        {
                            Sphere = new vrm.sb.Sphere
                            {
                                Offset = new System.Numerics.Vector3(-10000),
                            }
                        },
                        Extensions = new Dictionary<string, JToken>
                        {
                            {
                                "VRMC_springBone_extended_collider", vrm.Document.SaveAsNode(new vrm.sb.ExtendedCollider
                                {
                                    Shape = new vrm.sb.ExtendedShape
                                    {
                                        Sphere = new vrm.sb.ShapeSphere
                                        {
                                            Offset = new System.Numerics.Vector3(0, 0, 0),
                                            Radius = 0.5f,
                                            Inside = true
                                        }
                                    }
                                })
                            }
                        }
                    }
                })));
            Assert.That(extensionUsed, Does.Contain("VRMC_springBone_extended_collider"));
        }

        [Test]
        public void ExportPhysBoneColliderPlane()
        {
            var root = CreateRootGameObject();
            root.AddComponent<NdmfVrmExporterComponent>();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var nodeId = new gltf.ObjectID(42);
            {
                var capsule = new GameObject
                {
                    transform =
                    {
                        parent = root.transform
                    }
                };
                var collider = capsule.AddComponent<VRCPhysBoneCollider>();
                collider.shapeType = VRCPhysBoneColliderBase.ShapeType.Plane;
                nodes.Add(capsule.transform, nodeId);
            }
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var sb = exporter.ExportSpringBone();
            Assert.That(sb.Colliders, Is.Not.Null);
            Assert.That(JsonConvert.SerializeObject(sb.Colliders), Is.EqualTo(JsonConvert.SerializeObject(
                new List<vrm.sb.Collider>
                {
                    new()
                    {
                        Node = nodeId,
                        Shape = new vrm.sb.Shape()
                        {
                            Sphere = new vrm.sb.Sphere
                            {
                                Offset = new System.Numerics.Vector3(0, -10000, 0),
                                Radius = 10000,
                            }
                        },
                        Extensions = new Dictionary<string, JToken>
                        {
                            {
                                "VRMC_springBone_extended_collider", vrm.Document.SaveAsNode(new vrm.sb.ExtendedCollider
                                {
                                    Shape = new vrm.sb.ExtendedShape
                                    {
                                        Plane = new vrm.sb.ShapePlane
                                        {
                                            Normal = new System.Numerics.Vector3(0, 1, 0),
                                        }
                                    }
                                })
                            }
                        }
                    }
                })));
            Assert.That(extensionUsed, Does.Contain("VRMC_springBone_extended_collider"));
        }

        [Test]
        public void ExportPhysBoneSingle()
        {
            var root = CreateRootGameObject();
            root.AddComponent<NdmfVrmExporterComponent>();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var nodeId = new gltf.ObjectID(42);
            {
                var rootNode = new GameObject
                {
                    name = $"Node{nodeId.ID}",
                    transform =
                    {
                        parent = root.transform
                    }
                };
                var physBone = rootNode.AddComponent<VRCPhysBone>();
                physBone.gravity = 0.8f;
                physBone.stiffness = 0.63f;
                physBone.radius = 0.5f;
                physBone.pull = 0.42f;
                nodes.Add(rootNode.transform, nodeId);
            }
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var sb = exporter.ExportSpringBone();
            Assert.That(sb.Springs, Is.Not.Null);
            Assert.That(JsonConvert.SerializeObject(sb.Springs), Is.EqualTo(JsonConvert.SerializeObject(
                new List<vrm.sb.Spring>
                {
                    new()
                    {
                        Name = new gltf.UnicodeString($"Node{nodeId.ID}"),
                        Joints = new List<vrm.sb.Joint>
                        {
                            new()
                            {
                                Node = nodeId,
                                HitRadius = 0.5f,
                                Stiffness = 0.63f,
                                DragForce = 0.42f,
                                GravityPower = 0.8f,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            }
                        }
                    }
                })));
            Assert.That(extensionUsed, Is.Empty);
        }

        [Test]
        public void ExportPhysBoneMultiple()
        {
            var root = CreateRootGameObject();
            root.AddComponent<NdmfVrmExporterComponent>();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var nodeId = new gltf.ObjectID(42);
            {
                var rootNode = new GameObject
                {
                    name = $"Node{nodeId.ID}",
                    transform =
                    {
                        parent = root.transform
                    }
                };
                var physBone = rootNode.AddComponent<VRCPhysBone>();
                var linearCurve = AnimationCurve.Linear(0, 0, 1, 1);
                physBone.gravity = 1;
                physBone.gravityCurve = linearCurve;
                physBone.stiffness = 1;
                physBone.stiffnessCurve = linearCurve;
                physBone.radius = 1;
                physBone.radiusCurve = linearCurve;
                physBone.pull = 1;
                physBone.pullCurve = linearCurve;
                nodes.Add(rootNode.transform, nodeId);
                var parentNode = rootNode;
                for (var depth = 1u; depth <= 4u; depth++)
                {
                    var childNode = new GameObject
                    {
                        transform =
                        {
                            parent = parentNode.transform
                        }
                    };
                    nodes.Add(childNode.transform, new gltf.ObjectID(nodeId.ID + depth));
                    parentNode = childNode;
                }
            }
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var sb = exporter.ExportSpringBone();
            Assert.That(sb.Springs, Is.Not.Null);
            Assert.That(JsonConvert.SerializeObject(sb.Springs), Is.EqualTo(JsonConvert.SerializeObject(
                new List<vrm.sb.Spring>
                {
                    new()
                    {
                        Name = new gltf.UnicodeString($"Node{nodeId.ID}"),
                        Joints = new List<vrm.sb.Joint>
                        {
                            new()
                            {
                                Node = nodeId,
                                HitRadius = 0,
                                Stiffness = 0,
                                DragForce = 0,
                                GravityPower = 0,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            },
                            new()
                            {
                                Node = new gltf.ObjectID(nodeId.ID + 1),
                                HitRadius = 0.25f,
                                Stiffness = 0.25f,
                                DragForce = 0.25f,
                                GravityPower = 0.25f,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            },
                            new()
                            {
                                Node = new gltf.ObjectID(nodeId.ID + 2),
                                HitRadius = 0.5f,
                                Stiffness = 0.5f,
                                DragForce = 0.5f,
                                GravityPower = 0.5f,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            },
                            new()
                            {
                                Node = new gltf.ObjectID(nodeId.ID + 3),
                                HitRadius = 0.75f,
                                Stiffness = 0.75f,
                                DragForce = 0.75f,
                                GravityPower = 0.75f,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            },
                            new()
                            {
                                Node = new gltf.ObjectID(nodeId.ID + 4),
                                HitRadius = 1,
                                Stiffness = 1,
                                DragForce = 1,
                                GravityPower = 1,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            }
                        }
                    }
                })));
            Assert.That(extensionUsed, Is.Empty);
        }

        [Test]
        public void ExportPhysBoneChainedIgnore()
        {
            var root = CreateRootGameObject();
            root.AddComponent<NdmfVrmExporterComponent>();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var nodeId = new gltf.ObjectID(42);
            {
                var rootNode = new GameObject
                {
                    name = $"Node{nodeId.ID}",
                    transform =
                    {
                        parent = root.transform
                    }
                };
                var physBone = rootNode.AddComponent<VRCPhysBone>();
                physBone.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
                physBone.gravity = 1;
                physBone.stiffness = 1;
                physBone.radius = 1;
                physBone.pull = 1;
                nodes.Add(rootNode.transform, nodeId);
                {
                    var firstChildNode = new GameObject
                    {
                        transform =
                        {
                            parent = rootNode.transform
                        }
                    };
                    nodes.Add(firstChildNode.transform, new gltf.ObjectID(nodeId.ID + 1));
                    var firstChildNode2 = new GameObject
                    {
                        transform =
                        {
                            parent = firstChildNode.transform
                        }
                    };
                    nodes.Add(firstChildNode2.transform, new gltf.ObjectID(nodeId.ID + 2));
                    var secondChildNode = new GameObject
                    {
                        transform =
                        {
                            parent = rootNode.transform
                        }
                    };
                    nodes.Add(secondChildNode.transform, new gltf.ObjectID(nodeId.ID + 3));
                    var secondChildNode2 = new GameObject
                    {
                        transform =
                        {
                            parent = secondChildNode.transform
                        }
                    };
                    nodes.Add(secondChildNode2.transform, new gltf.ObjectID(nodeId.ID + 4));
                }
            }
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var sb = exporter.ExportSpringBone();
            Assert.That(sb.Springs, Is.Not.Null);
            Assert.That(JsonConvert.SerializeObject(sb.Springs), Is.EqualTo(JsonConvert.SerializeObject(
                new List<vrm.sb.Spring>
                {
                    new()
                    {
                        Name = new gltf.UnicodeString($"Node{nodeId.ID}.1"),
                        Joints = new List<vrm.sb.Joint>
                        {
                            new()
                            {
                                Node = new gltf.ObjectID(nodeId.ID + 1),
                                HitRadius = 1,
                                Stiffness = 1,
                                DragForce = 1,
                                GravityPower = 1,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            },
                            new()
                            {
                                Node = new gltf.ObjectID(nodeId.ID + 2),
                                HitRadius = 1,
                                Stiffness = 1,
                                DragForce = 1,
                                GravityPower = 1,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            }
                        }
                    },
                    new()
                    {
                        Name = new gltf.UnicodeString($"Node{nodeId.ID}.2"),
                        Joints = new List<vrm.sb.Joint>
                        {
                            new()
                            {
                                Node = new gltf.ObjectID(nodeId.ID + 3),
                                HitRadius = 1,
                                Stiffness = 1,
                                DragForce = 1,
                                GravityPower = 1,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            },
                            new()
                            {
                                Node = new gltf.ObjectID(nodeId.ID + 4),
                                HitRadius = 1,
                                Stiffness = 1,
                                DragForce = 1,
                                GravityPower = 1,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            }
                        }
                    }
                })));
            Assert.That(extensionUsed, Is.Empty);
        }

        [TestCase(VRCPhysBoneBase.MultiChildType.First)]
        [TestCase(VRCPhysBoneBase.MultiChildType.Average)]
        public void ExportPhysBoneChained(VRCPhysBoneBase.MultiChildType multiChildType)
        {
            var root = CreateRootGameObject();
            root.AddComponent<NdmfVrmExporterComponent>();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var nodeId = new gltf.ObjectID(42);
            {
                var rootNode = new GameObject
                {
                    name = $"Node{nodeId.ID}",
                    transform =
                    {
                        parent = root.transform
                    }
                };
                var physBone = rootNode.AddComponent<VRCPhysBone>();
                physBone.multiChildType = multiChildType;
                physBone.gravity = 1;
                physBone.stiffness = 1;
                physBone.radius = 1;
                physBone.pull = 1;
                nodes.Add(rootNode.transform, nodeId);
                {
                    var firstChildNode = new GameObject
                    {
                        transform =
                        {
                            parent = rootNode.transform
                        }
                    };
                    nodes.Add(firstChildNode.transform, new gltf.ObjectID(nodeId.ID + 1));
                    var firstChildNode2 = new GameObject
                    {
                        transform =
                        {
                            parent = firstChildNode.transform
                        }
                    };
                    nodes.Add(firstChildNode2.transform, new gltf.ObjectID(nodeId.ID + 2));
                    var secondChildNode = new GameObject
                    {
                        transform =
                        {
                            parent = rootNode.transform
                        }
                    };
                    nodes.Add(secondChildNode.transform, new gltf.ObjectID(nodeId.ID + 3));
                    var secondChildNode2 = new GameObject
                    {
                        transform =
                        {
                            parent = secondChildNode.transform
                        }
                    };
                    nodes.Add(secondChildNode2.transform, new gltf.ObjectID(nodeId.ID + 4));
                }
            }
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var sb = exporter.ExportSpringBone();
            Assert.That(sb.Springs, Is.Not.Null);
            Assert.That(JsonConvert.SerializeObject(sb.Springs), Is.EqualTo(JsonConvert.SerializeObject(
                new List<vrm.sb.Spring>
                {
                    new()
                    {
                        Name = new gltf.UnicodeString($"Node{nodeId.ID}.1"),
                        Joints = new List<vrm.sb.Joint>
                        {
                            new()
                            {
                                Node = new gltf.ObjectID(nodeId.ID),
                                HitRadius = 1,
                                Stiffness = 1,
                                DragForce = 1,
                                GravityPower = 1,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            },
                            new()
                            {
                                Node = new gltf.ObjectID(nodeId.ID + 1),
                                HitRadius = 1,
                                Stiffness = 1,
                                DragForce = 1,
                                GravityPower = 1,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            },
                            new()
                            {
                                Node = new gltf.ObjectID(nodeId.ID + 2),
                                HitRadius = 1,
                                Stiffness = 1,
                                DragForce = 1,
                                GravityPower = 1,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            }
                        }
                    },
                    new()
                    {
                        Name = new gltf.UnicodeString($"Node{nodeId.ID}.2"),
                        Joints = new List<vrm.sb.Joint>
                        {
                            new()
                            {
                                Node = new gltf.ObjectID(nodeId.ID + 3),
                                HitRadius = 1,
                                Stiffness = 1,
                                DragForce = 1,
                                GravityPower = 1,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            },
                            new()
                            {
                                Node = new gltf.ObjectID(nodeId.ID + 4),
                                HitRadius = 1,
                                Stiffness = 1,
                                DragForce = 1,
                                GravityPower = 1,
                                GravityDir = -System.Numerics.Vector3.UnitY
                            }
                        }
                    }
                })));
            Assert.That(extensionUsed, Is.Empty);
        }
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

#if NVE_HAS_LILTOON
        [Test]
        public void ExportMToonEmpty()
        {
            var root = CreateRootGameObject();
            root.AddComponent<NdmfVrmExporterComponent>();
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var material = new Material(Shader.Find("Hidden/lilToonTransparent"));
            var mtoonTexture = new NdmfVrmExporter.MToonTexture();
            var gltfRoot = NewEmptyGltfRoot();
            var gltfExporter = new gltf.exporter.Exporter();
            var materialExporter = new NdmfVrmExporter.GltfMaterialExporter(gltfRoot, gltfExporter, extensionUsed);
            var mtoon = exporter.ExportMToon(material, mtoonTexture, materialExporter);
            Assert.That(JsonConvert.SerializeObject(mtoon), Is.EqualTo(JsonConvert.SerializeObject(new vrm.mtoon.MToon
            {
                GIEqualizationFactor = 1,
                MatcapFactor = System.Numerics.Vector3.Zero,
                ShadeColorFactor = System.Numerics.Vector3.One,
                TransparentWithZWrite = true,
                RimLightingMixFactor = 0,
            })));
        }

        [Test]
        public void ExportMToonShadowWithMaskTexture()
        {
            var root = CreateRootGameObject();
            var component = root.AddComponent<NdmfVrmExporterComponent>();
            component.enableMToonRimLight = true;
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var material = new Material(Shader.Find("Hidden/lilToonOutline"));
            material.SetFloat($"_UseShadow", 1);
            material.SetFloat($"_ShadowBorder", 0.5f);
            material.SetFloat($"_ShadowBlur", 0.5f);
            material.SetFloat($"_ShadowMainStrength", 0.5f);
            material.SetTexture($"_ShadowStrengthMask", Texture2D.whiteTexture);
            material.SetTexture($"_ShadowBorderMask", Texture2D.blackTexture);
            var mtoonTexture = new NdmfVrmExporter.MToonTexture();
            var gltfRoot = NewEmptyGltfRoot();
            var gltfExporter = new gltf.exporter.Exporter();
            var materialExporter = new NdmfVrmExporter.GltfMaterialExporter(gltfRoot, gltfExporter, extensionUsed);
            var mtoon = exporter.ExportMToon(material, mtoonTexture, materialExporter);
            Assert.That(JsonConvert.SerializeObject(mtoon), Is.EqualTo(JsonConvert.SerializeObject(new vrm.mtoon.MToon
            {
                ShadeColorFactor = System.Numerics.Vector3.One,
                MatcapFactor = System.Numerics.Vector3.Zero,
                OutlineWidthMode = vrm.mtoon.OutlineWidthMode.WorldCoordinates,
                OutlineWidthFactor = 0.0008f,
                OutlineColorFactor = new System.Numerics.Vector3(0.6f, 0.56f, 0.73f),
                ShadeMultiplyTexture = new gltf.material.TextureInfo
                {
                    Index = new gltf.ObjectID(0),
                },
                ShadingShiftTexture = new vrm.mtoon.ShadingShiftTexture
                {
                    Index = new gltf.ObjectID(1),
                },
                ShadingShiftFactor = 0,
                ShadingToonyFactor = 0.5f,
                RimLightingMixFactor = 0,
                GIEqualizationFactor = 1,
                TransparentWithZWrite = false,
            })));
        }

        [Test]
        public void ExportMToonShadowWithoutMaskTexture()
        {
            var root = CreateRootGameObject();
            var component = root.AddComponent<NdmfVrmExporterComponent>();
            component.enableMToonRimLight = true;
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var material = new Material(Shader.Find("Hidden/lilToonCutout"));
            material.SetFloat($"_UseShadow", 1);
            material.SetFloat($"_ShadowBorder", 0.5f);
            material.SetFloat($"_ShadowBlur", 0.5f);
            material.SetFloat($"_ShadowMainStrength", 0.0f);
            material.SetTexture($"_ShadowColorTex", Texture2D.whiteTexture);
            var mtoonTexture = new NdmfVrmExporter.MToonTexture();
            var gltfRoot = NewEmptyGltfRoot();
            var gltfExporter = new gltf.exporter.Exporter();
            var materialExporter = new NdmfVrmExporter.GltfMaterialExporter(gltfRoot, gltfExporter, extensionUsed);
            var mtoon = exporter.ExportMToon(material, mtoonTexture, materialExporter);
            Assert.That(JsonConvert.SerializeObject(mtoon), Is.EqualTo(JsonConvert.SerializeObject(new vrm.mtoon.MToon
            {
                ShadeColorFactor = new System.Numerics.Vector3(0.82f, 0.76f, 0.85f),
                MatcapFactor = System.Numerics.Vector3.Zero,
                ShadeMultiplyTexture = new gltf.material.TextureInfo
                {
                    Index = new gltf.ObjectID(0),
                },
                ShadingShiftFactor = 0,
                ShadingToonyFactor = 0.5f,
                RimLightingMixFactor = 0,
                GIEqualizationFactor = 1,
                TransparentWithZWrite = true,
            })));
        }

        [Test]
        public void ExportMToonRimLight()
        {
            var root = CreateRootGameObject();
            var component = root.AddComponent<NdmfVrmExporterComponent>();
            component.enableMToonRimLight = true;
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var material = new Material(Shader.Find("Hidden/lilToonOutline"));
            material.SetFloat($"_UseRim", 1);
            material.SetFloat($"_RimBorder", 0);
            material.SetFloat($"_RimBlur", 0.5f);
            material.SetFloat($"_RimFresnelPower", 0.5f);
            material.SetColor($"_RimColor", Color.magenta);
            material.SetFloat($"_RimBlendMode", 3);
            material.SetTexture($"_RimColorTex", Texture2D.whiteTexture);
            var mtoonTexture = new NdmfVrmExporter.MToonTexture();
            var gltfRoot = NewEmptyGltfRoot();
            var gltfExporter = new gltf.exporter.Exporter();
            var materialExporter = new NdmfVrmExporter.GltfMaterialExporter(gltfRoot, gltfExporter, extensionUsed);
            var mtoon = exporter.ExportMToon(material, mtoonTexture, materialExporter);
            Assert.That(JsonConvert.SerializeObject(mtoon), Is.EqualTo(JsonConvert.SerializeObject(new vrm.mtoon.MToon
            {
                ShadeColorFactor = System.Numerics.Vector3.One,
                MatcapFactor = System.Numerics.Vector3.Zero,
                OutlineWidthMode = vrm.mtoon.OutlineWidthMode.WorldCoordinates,
                OutlineWidthFactor = 0.0008f,
                OutlineColorFactor = new System.Numerics.Vector3(0.6f, 0.56f, 0.73f),
                RimLightingMixFactor = 1,
                ParametricRimColorFactor = new System.Numerics.Vector3(1, 0, 1),
                ParametricRimFresnelPowerFactor = 1,
                ParametricRimLiftFactor = 0.5f,
                RimMultiplyTexture = new gltf.material.TextureInfo
                {
                    Index = new gltf.ObjectID(0),
                },
                GIEqualizationFactor = 1,
                TransparentWithZWrite = false,
            })));
        }

        [Test]
        public void ExportMToonMatCap()
        {
            var root = CreateRootGameObject();
            var component = root.AddComponent<NdmfVrmExporterComponent>();
            component.enableMToonMatCap = true;
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var material = new Material(Shader.Find("Hidden/lilToonOutline"));
            material.SetFloat($"_UseMatCap", 1);
            material.SetTexture($"_MatCapTex", Texture2D.whiteTexture);
            material.SetTexture($"_MatCapBlendMask", Texture2D.blackTexture);
            var mtoonTexture = new NdmfVrmExporter.MToonTexture();
            var gltfRoot = NewEmptyGltfRoot();
            var gltfExporter = new gltf.exporter.Exporter();
            var materialExporter = new NdmfVrmExporter.GltfMaterialExporter(gltfRoot, gltfExporter, extensionUsed);
            var mtoon = exporter.ExportMToon(material, mtoonTexture, materialExporter);
            Assert.That(JsonConvert.SerializeObject(mtoon), Is.EqualTo(JsonConvert.SerializeObject(new vrm.mtoon.MToon
            {
                ShadeColorFactor = System.Numerics.Vector3.One,
                MatcapFactor = System.Numerics.Vector3.One,
                OutlineWidthMode = vrm.mtoon.OutlineWidthMode.WorldCoordinates,
                OutlineWidthFactor = 0.0008f,
                OutlineColorFactor = new System.Numerics.Vector3(0.6f, 0.56f, 0.73f),
                MatcapTexture = new gltf.material.TextureInfo
                {
                    Index = new gltf.ObjectID(0),
                },
                RimMultiplyTexture = new gltf.material.TextureInfo
                {
                    Index = new gltf.ObjectID(1),
                },
                RimLightingMixFactor = 1,
                GIEqualizationFactor = 1,
                TransparentWithZWrite = false,
            })));
        }

        [Test]
        public void ExportMToonOutline()
        {
            var root = CreateRootGameObject();
            var component = root.AddComponent<NdmfVrmExporterComponent>();
            component.enableMToonOutline = true;
            var assetSaver = new NullAssetSaver();
            var nodes = new Dictionary<Transform, gltf.ObjectID>();
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            var extensionUsed = new HashSet<string>();
            var exporter = new NdmfVrmExporter.VrmRootExporter(root, assetSaver, nodes, allMorphTargets, extensionUsed);
            var material = new Material(Shader.Find("Hidden/lilToonOutline"));
            material.SetFloat($"_OutlineWidth", 1);
            material.SetColor($"_OutlineColor", Color.cyan);
            material.SetTexture($"_OutlineWidthMask", Texture2D.whiteTexture);
            var mtoonTexture = new NdmfVrmExporter.MToonTexture();
            var gltfRoot = NewEmptyGltfRoot();
            var gltfExporter = new gltf.exporter.Exporter();
            var materialExporter = new NdmfVrmExporter.GltfMaterialExporter(gltfRoot, gltfExporter, extensionUsed);
            var mtoon = exporter.ExportMToon(material, mtoonTexture, materialExporter);
            Assert.That(JsonConvert.SerializeObject(mtoon), Is.EqualTo(JsonConvert.SerializeObject(new vrm.mtoon.MToon
            {
                ShadeColorFactor = System.Numerics.Vector3.One,
                MatcapFactor = System.Numerics.Vector3.Zero,
                OutlineWidthMode = vrm.mtoon.OutlineWidthMode.WorldCoordinates,
                OutlineLightingMixFactor = 1,
                OutlineWidthFactor = 0.01f,
                OutlineColorFactor = new System.Numerics.Vector3(0, 1, 1),
                OutlineWidthMultiplyTexture = new gltf.material.TextureInfo
                {
                    Index = new gltf.ObjectID(0),
                },
                GIEqualizationFactor = 1,
                RimLightingMixFactor = 0,
                TransparentWithZWrite = false
            })));
        }
#endif // NVE_HAS_LILTOON

#if NVE_HAS_MODULAR_AVATAR
        [TestCaseSource(nameof(SourceProviders))]
        public void ExportMaterialVariantWithMaMaterialSetter(INDMFPlatformProvider provider)
        {
            const uint rootNodeId = 42u;
            var root = new GameObject
            {
                name = "Humanoid(Clone)"
            };
            var nodes = new Dictionary<Transform, gltf.ObjectID>
            {
                { root.transform, new gltf.ObjectID(rootNodeId) }
            };
            const uint hipNodeId = rootNodeId + 1;
            SetupDummyHumanoidAvatar(root, hipNodeId, nodes);
            var hipTransform = root.transform.GetChild(0);
            var mesh = new Mesh();
            var skinnedMeshRenderer = hipTransform.gameObject.AddComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.sharedMesh = mesh;
            root.AddComponent<NdmfVrmExporterComponent>();
            root.AddComponent<VRCAvatarDescriptor>();
            var shader = Shader.Find("Standard");
            var fooMaterial = new Material(shader)
            {
                name = "foo"
            };
            var barMaterial = new Material(shader)
            {
                name = "bar"
            };
            var bazMaterial = new Material(shader)
            {
                name = "baz"
            };
            foreach (var item in new[] { fooMaterial, barMaterial, bazMaterial })
            {
                var child = new GameObject
                {
                    name = $"MA-{item.name}",
                    transform =
                    {
                        parent = root.transform,
                    }
                };
                child.AddComponent<ModularAvatarMenuInstaller>();
                child.AddComponent<ModularAvatarMenuItem>();
                var setter = child.AddComponent<ModularAvatarMaterialSetter>();
                setter.Objects = new List<MaterialSwitchObject>
                {
                    new()
                    {
                        Object = new AvatarObjectReference(skinnedMeshRenderer.gameObject),
                        Material = item,
                        MaterialIndex = 0,
                    }
                };
                child.transform.parent = hipTransform;
            }
            var quuxMaterial = new Material(shader)
            {
                name = "quux"
            };
            {
                var child2 = new GameObject
                {
                    name = "child2",
                    transform =
                    {
                        parent = root.transform,
                    }
                };
                child2.AddComponent<ModularAvatarMenuItem>();
                var setter = child2.AddComponent<ModularAvatarMaterialSetter>();
                setter.Objects = new List<MaterialSwitchObject>
                {
                    new()
                    {
                        Object = new AvatarObjectReference(skinnedMeshRenderer.gameObject),
                        Material = quuxMaterial,
                        MaterialIndex = 0,
                    }
                };
                var child1 = new GameObject
                {
                    name = "child1",
                    transform =
                    {
                        parent = root.transform,
                    }
                };
                child1.AddComponent<ModularAvatarMenuInstaller>();
                var item = child1.AddComponent<ModularAvatarMenuItem>();
                item.PortableControl.Type = PortableControlType.SubMenu;
                child2.transform.parent = child1.transform;
                child1.transform.parent = hipTransform;
            }
            var context = AvatarProcessor.ProcessAvatar(root, provider);
            var variants = context.GetState<List<MaterialVariant>>().AsReadOnly();
            Assert.That(variants, Is.Not.Empty);
            Assert.That(variants.Count, Is.EqualTo(4));
            Assert.That(variants[0].Name, Is.EqualTo("MA-foo"));
            Assert.That(variants[0].Mappings.Length, Is.EqualTo(1));
            Assert.That(variants[0].Mappings[0].Renderer, Is.EqualTo(skinnedMeshRenderer));
            Assert.That(variants[0].Mappings[0].Materials.Length, Is.EqualTo(1));
            Assert.That(variants[0].Mappings[0].Materials[0], Is.EqualTo(fooMaterial));
            Assert.That(variants[1].Name, Is.EqualTo("MA-bar"));
            Assert.That(variants[1].Mappings.Length, Is.EqualTo(1));
            Assert.That(variants[1].Mappings[0].Renderer, Is.EqualTo(skinnedMeshRenderer));
            Assert.That(variants[1].Mappings[0].Materials.Length, Is.EqualTo(1));
            Assert.That(variants[1].Mappings[0].Materials[0], Is.EqualTo(barMaterial));
            Assert.That(variants[2].Name, Is.EqualTo("MA-baz"));
            Assert.That(variants[2].Mappings.Length, Is.EqualTo(1));
            Assert.That(variants[2].Mappings[0].Renderer, Is.EqualTo(skinnedMeshRenderer));
            Assert.That(variants[2].Mappings[0].Materials.Length, Is.EqualTo(1));
            Assert.That(variants[2].Mappings[0].Materials[0], Is.EqualTo(bazMaterial));
            Assert.That(variants[3].Name, Is.EqualTo("child1/child2"));
            Assert.That(variants[3].Mappings.Length, Is.EqualTo(1));
            Assert.That(variants[3].Mappings[0].Renderer, Is.EqualTo(skinnedMeshRenderer));
            Assert.That(variants[3].Mappings[0].Materials.Length, Is.EqualTo(1));
            Assert.That(variants[3].Mappings[0].Materials[0], Is.EqualTo(quuxMaterial));
        }

        [TestCaseSource(nameof(SourceProviders))]
        public void ExportMaterialVariantWithMaMaterialSwap(INDMFPlatformProvider provider)
        {
            const uint rootNodeId = 42u;
            var root = new GameObject
            {
                name = "Humanoid(Clone)"
            };
            var nodes = new Dictionary<Transform, gltf.ObjectID>
            {
                { root.transform, new gltf.ObjectID(rootNodeId) }
            };
            const uint hipNodeId = rootNodeId + 1;
            SetupDummyHumanoidAvatar(root, hipNodeId, nodes);
            var hipTransform = root.transform.GetChild(0);
            var mesh = new Mesh();
            var skinnedMeshRenderer = hipTransform.gameObject.AddComponent<SkinnedMeshRenderer>();
            var shader = Shader.Find("Standard");
            var baseMaterial = new Material(shader);
            skinnedMeshRenderer.SetMaterials(new List<Material> { baseMaterial });
            skinnedMeshRenderer.sharedMesh = mesh;
            root.AddComponent<NdmfVrmExporterComponent>();
            root.AddComponent<VRCAvatarDescriptor>();
            var fooMaterial = new Material(shader)
            {
                name = "foo"
            };
            var barMaterial = new Material(shader)
            {
                name = "bar"
            };
            var bazMaterial = new Material(shader)
            {
                name = "baz"
            };
            foreach (var (source, target) in new[]
                     {
                         (baseMaterial, fooMaterial),
                         (baseMaterial, barMaterial),
                         (baseMaterial, bazMaterial),
                     })
            {
                var child = new GameObject
                {
                    name = $"MA-{target.name}",
                    transform =
                    {
                        parent = root.transform
                    }
                };
                child.AddComponent<ModularAvatarMenuInstaller>();
                child.AddComponent<ModularAvatarMenuItem>();
                var setter = child.AddComponent<ModularAvatarMaterialSwap>();
                setter.Swaps = new List<MatSwap>()
                {
                    new()
                    {
                        From = source,
                        To = target
                    }
                };
                child.transform.parent = hipTransform;
            }
            var quuxMaterial = new Material(shader)
            {
                name = "quux"
            };
            {
                var child2 = new GameObject
                {
                    name = "child2",
                    transform =
                    {
                        parent = root.transform,
                    }
                };
                child2.AddComponent<ModularAvatarMenuItem>();
                var setter = child2.AddComponent<ModularAvatarMaterialSwap>();
                setter.Swaps = new List<MatSwap>()
                {
                    new()
                    {
                        From = baseMaterial,
                        To = quuxMaterial
                    }
                };
                var child1 = new GameObject
                {
                    name = "child1",
                    transform =
                    {
                        parent = root.transform,
                    }
                };
                child1.AddComponent<ModularAvatarMenuInstaller>();
                var item = child1.AddComponent<ModularAvatarMenuItem>();
                item.PortableControl.Type = PortableControlType.SubMenu;
                child2.transform.parent = child1.transform;
                child1.transform.parent = hipTransform;
            }
            var context = AvatarProcessor.ProcessAvatar(root, provider);
            var variants = context.GetState<List<MaterialVariant>>().AsReadOnly();
            Assert.That(variants, Is.Not.Empty);
            Assert.That(variants.Count, Is.EqualTo(4));
            Assert.That(variants[0].Name, Is.EqualTo("MA-foo"));
            Assert.That(variants[0].Mappings.Length, Is.EqualTo(1));
            Assert.That(variants[0].Mappings[0].Renderer, Is.EqualTo(skinnedMeshRenderer));
            Assert.That(variants[0].Mappings[0].Materials.Length, Is.EqualTo(1));
            Assert.That(variants[0].Mappings[0].Materials[0], Is.EqualTo(fooMaterial));
            Assert.That(variants[1].Name, Is.EqualTo("MA-bar"));
            Assert.That(variants[1].Mappings.Length, Is.EqualTo(1));
            Assert.That(variants[1].Mappings[0].Renderer, Is.EqualTo(skinnedMeshRenderer));
            Assert.That(variants[1].Mappings[0].Materials.Length, Is.EqualTo(1));
            Assert.That(variants[1].Mappings[0].Materials[0], Is.EqualTo(barMaterial));
            Assert.That(variants[2].Name, Is.EqualTo("MA-baz"));
            Assert.That(variants[2].Mappings.Length, Is.EqualTo(1));
            Assert.That(variants[2].Mappings[0].Renderer, Is.EqualTo(skinnedMeshRenderer));
            Assert.That(variants[2].Mappings[0].Materials.Length, Is.EqualTo(1));
            Assert.That(variants[2].Mappings[0].Materials[0], Is.EqualTo(bazMaterial));
            Assert.That(variants[3].Name, Is.EqualTo("child1/child2"));
            Assert.That(variants[3].Mappings.Length, Is.EqualTo(1));
            Assert.That(variants[3].Mappings[0].Renderer, Is.EqualTo(skinnedMeshRenderer));
            Assert.That(variants[3].Mappings[0].Materials.Length, Is.EqualTo(1));
            Assert.That(variants[3].Mappings[0].Materials[0], Is.EqualTo(quuxMaterial));
        }
#endif // NVE_HAS_MODULAR_AVATAR

#if NVE_HAS_LILYCAL_INVENTORY
        [TestCaseSource(nameof(SourceProviders))]
        public void ExportMaterialVariantWithLilycalInventory(INDMFPlatformProvider provider)
        {
            const uint rootNodeId = 42u;
            var root = new GameObject
            {
                name = "Humanoid(Clone)"
            };
            var nodes = new Dictionary<Transform, gltf.ObjectID>
            {
                { root.transform, new gltf.ObjectID(rootNodeId) }
            };
            const uint hipNodeId = rootNodeId + 1;
            SetupDummyHumanoidAvatar(root, hipNodeId, nodes);
            var hipTransform = root.transform.GetChild(0);
            var mesh = new Mesh();
            var skinnedMeshRenderer = hipTransform.gameObject.AddComponent<SkinnedMeshRenderer>();
            var baseMaterial = new Material(Shader.Find("Standard"));
            skinnedMeshRenderer.SetMaterials(new List<Material> { baseMaterial });
            skinnedMeshRenderer.sharedMesh = mesh;
            root.AddComponent<NdmfVrmExporterComponent>();
            root.AddComponent<VRCAvatarDescriptor>();
            var shader = Shader.Find("Standard");
            var fooMaterial = new Material(shader)
            {
                name = "foo"
            };
            var barMaterial = new Material(shader)
            {
                name = "bar"
            };
            var bazMaterial = new Material(shader)
            {
                name = "baz"
            };
            var quuxMaterial = new Material(shader)
            {
                name = "quux"
            };
            {
                const BindingFlags bindingAttrPublic = BindingFlags.Public | BindingFlags.Instance;
                const BindingFlags bindingAttrPrivate = BindingFlags.NonPublic | BindingFlags.Instance;
                var assembly = Assembly.Load("jp.lilxyzw.lilycalinventory.runtime");
                var costumeType = assembly.GetType("jp.lilxyzw.lilycalinventory.runtime.Costume");
                var parametersPerMenuType = assembly.GetType("jp.lilxyzw.lilycalinventory.runtime.ParametersPerMenu");
                var materialReplacerType = assembly.GetType("jp.lilxyzw.lilycalinventory.runtime.MaterialReplacer");
                var costumeChangerType = typeof(CostumeChanger);
                var costumeChanger = root.AddComponent<CostumeChanger>();
                var materials = new[] { fooMaterial, barMaterial, bazMaterial };
                var costumes = Array.CreateInstance(costumeType, materials.Length + 1);
                var index = 0;
                costumeChangerType.GetField("menuName", bindingAttrPrivate)!.SetValue(costumeChanger, "LI");
                foreach (var item in materials)
                {
                    var costume = Activator.CreateInstance(costumeType);
                    var parametersPerMenu = Activator.CreateInstance(parametersPerMenuType);
                    var materialReplacer = Activator.CreateInstance(materialReplacerType);
                    var materialReplacerArray = Array.CreateInstance(materialReplacerType, 1);
                    materialReplacerArray.SetValue(materialReplacer, 0);
                    materialReplacerType.GetField("renderer", bindingAttrPublic)!.SetValue(materialReplacer, skinnedMeshRenderer);
                    materialReplacerType.GetField("replaceTo", bindingAttrPublic)!.SetValue(materialReplacer, new[] { item });
                    parametersPerMenuType.GetField("materialReplacers", bindingAttrPublic)!.SetValue(parametersPerMenu, materialReplacerArray);
                    costumeType.GetField("menuName", bindingAttrPublic)!.SetValue(costume, item.name);
                    costumeType.GetField("parametersPerMenu", bindingAttrPublic)!.SetValue(costume, parametersPerMenu);
                    costumes.SetValue(costume, index++);
                }
                {
                    var costume = Activator.CreateInstance(costumeType);
                    var parametersPerMenu = Activator.CreateInstance(parametersPerMenuType);
                    var materialReplacer = Activator.CreateInstance(materialReplacerType);
                    var materialReplacerArray = Array.CreateInstance(materialReplacerType, 1);
                    materialReplacerArray.SetValue(materialReplacer, 0);
                    materialReplacerType.GetField("renderer", bindingAttrPublic)!.SetValue(materialReplacer, skinnedMeshRenderer);
                    materialReplacerType.GetField("replaceTo", bindingAttrPublic)!.SetValue(materialReplacer, new[] { quuxMaterial });
                    parametersPerMenuType.GetField("materialReplacers", bindingAttrPublic)!.SetValue(parametersPerMenu, materialReplacerArray);
                    costumeType.GetField("menuName", bindingAttrPublic)!.SetValue(costume, quuxMaterial.name);
                    costumeType.GetField("parametersPerMenu", bindingAttrPublic)!.SetValue(costume, parametersPerMenu);
                    {
                        var menuFolderType = typeof(MenuFolder);
                        var menuNodeBar = new GameObject
                        {
                            name = "bar",
                            transform =
                            {
                                parent = root.transform
                            }
                        };
                        var menuFolderBar = menuNodeBar.AddComponent<MenuFolder>();
                        menuFolderType.GetField("menuName", bindingAttrPrivate)!.SetValue(menuFolderBar, menuNodeBar.name);
                        var menuNodeFoo = new GameObject
                        {
                            name = "foo",
                            transform =
                            {
                                parent = root.transform
                            }
                        };
                        var menuFolderFoo = menuNodeFoo.AddComponent<MenuFolder>();
                        menuFolderType.GetField("menuName", bindingAttrPrivate)!.SetValue(menuFolderFoo, menuFolderFoo.name);
                        menuFolderType.GetField("parentOverride", bindingAttrPrivate)!.SetValue(menuFolderBar, menuFolderFoo);
                        costumeType.GetField("parentOverride", bindingAttrPublic)!.SetValue(costume, menuFolderBar);
                    }
                    costumes.SetValue(costume, index);
                }
                costumeChangerType.GetField("costumes", bindingAttrPrivate)!.SetValue(costumeChanger, costumes);
            }
            var context = AvatarProcessor.ProcessAvatar(root, provider);
            var variants = context.GetState<List<MaterialVariant>>().AsReadOnly();
            Assert.That(variants, Is.Not.Empty);
            Assert.That(variants.Count, Is.EqualTo(4));
            Assert.That(variants[0].Name, Is.EqualTo("LI/foo"));
            Assert.That(variants[0].Mappings.Length, Is.EqualTo(1));
            Assert.That(variants[0].Mappings[0].Renderer, Is.EqualTo(skinnedMeshRenderer));
            Assert.That(variants[0].Mappings[0].Materials.Length, Is.EqualTo(1));
            Assert.That(variants[0].Mappings[0].Materials[0], Is.EqualTo(fooMaterial));
            Assert.That(variants[1].Name, Is.EqualTo("LI/bar"));
            Assert.That(variants[1].Mappings.Length, Is.EqualTo(1));
            Assert.That(variants[1].Mappings[0].Renderer, Is.EqualTo(skinnedMeshRenderer));
            Assert.That(variants[1].Mappings[0].Materials.Length, Is.EqualTo(1));
            Assert.That(variants[1].Mappings[0].Materials[0], Is.EqualTo(barMaterial));
            Assert.That(variants[2].Name, Is.EqualTo("LI/baz"));
            Assert.That(variants[2].Mappings.Length, Is.EqualTo(1));
            Assert.That(variants[2].Mappings[0].Renderer, Is.EqualTo(skinnedMeshRenderer));
            Assert.That(variants[2].Mappings[0].Materials.Length, Is.EqualTo(1));
            Assert.That(variants[2].Mappings[0].Materials[0], Is.EqualTo(bazMaterial));
            Assert.That(variants[3].Name, Is.EqualTo("foo/bar/LI/quux"));
            Assert.That(variants[3].Mappings.Length, Is.EqualTo(1));
            Assert.That(variants[3].Mappings[0].Renderer, Is.EqualTo(skinnedMeshRenderer));
            Assert.That(variants[3].Mappings[0].Materials.Length, Is.EqualTo(1));
            Assert.That(variants[3].Mappings[0].Materials[0], Is.EqualTo(quuxMaterial));
        }
#endif // NVE_HAS_LILYCAL_INVENTORY

#if NVE_HAS_VRCHAT_AVATAR_SDK && NVE_HAS_LILTOON
        [Test]
        public void ExportFull()
        {
            const uint rootNodeId = 42u;
            var root = new GameObject
            {
                name = "Humanoid(Clone)"
            };
            var nodes = new Dictionary<Transform, gltf.ObjectID>
            {
                { root.transform, new gltf.ObjectID(rootNodeId) }
            };
            const uint hipNodeId = rootNodeId + 1;
            SetupDummyHumanoidAvatar(root, hipNodeId, nodes);
            var hipTransform = root.transform.GetChild(0);
            var mesh = new Mesh
            {
                vertices = new Vector3[]
                {
                    new(0, 0, 0),
                    new(1, 0, 0),
                    new(0, 1, 0),
                },
                normals = new Vector3[]
                {
                    new(0, 1, 0),
                    new(0, 0, 1),
                    new(1, 0, 0),
                },
                colors = new[]
                {
                    Color.white,
                    Color.white,
                    Color.white,
                },
                uv = new[]
                {
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero,
                },
                uv2 = new[]
                {
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero,
                },
                tangents = new[]
                {
                    Vector4.zero,
                    Vector4.zero,
                    Vector4.zero,
                },
                triangles = new[]
                {
                    0, 1, 2
                },
                bindposes = new[]
                {
                    Matrix4x4.identity
                },
                boneWeights = new[]
                {
                    new BoneWeight
                    {
                        weight0 = 1,
                        boneIndex0 = 0,
                    },
                    new BoneWeight
                    {
                        weight0 = 1,
                        boneIndex0 = 0,
                    },
                    new BoneWeight
                    {
                        weight0 = 1,
                        boneIndex0 = 0,
                    }
                },
            };
            mesh.AddBlendShapeFrame("dummy", 0, new[]
                {
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.zero
                },
                new[]
                {
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.zero
                },
                null
            );
            var skinnedMeshRenderer = hipTransform.gameObject.AddComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.bones = new[]
            {
                hipTransform
            };
            var shader = Shader.Find("Hidden/lilToonCutout");
            var material = new Material(shader);
            material.SetTexture($"_MainTex", Texture2D.whiteTexture);
            skinnedMeshRenderer.SetMaterials(new List<Material> { material });
            skinnedMeshRenderer.sharedMesh = mesh;
            skinnedMeshRenderer.SetBlendShapeWeight(0, 100);
            root.AddComponent<NdmfVrmExporterComponent>();
            root.AddComponent<VRCAvatarDescriptor>();
            var assetSaver = new NullAssetSaver();
            var variants = new List<MaterialVariant>
            {
                new()
                {
                    Name = "mappings",
                    Mappings = new[]
                    {
                        new MaterialVariantMapping
                        {
                            Materials = new Material[]
                            {
                                null
                            },
                            Renderer = skinnedMeshRenderer,
                        },
                        new MaterialVariantMapping
                        {
                            Materials = new[]
                            {
                                new Material(shader)
                                {
                                    name = "variant1",
                                },
                            },
                            Renderer = skinnedMeshRenderer,
                        },
                        new MaterialVariantMapping
                        {
                            Materials = new[]
                            {
                                new Material(shader)
                                {
                                    name = "variant2",
                                },
                            },
                            Renderer = skinnedMeshRenderer,
                        }
                    },
                }
            };
            using var exporter = new NdmfVrmExporter(root, assetSaver, variants);
            using var stream = new MemoryStream();
            var json = exporter.Export(stream);
            var gltfRoot = gltf.Document.LoadFromString(json);
            var bin = stream.ToArray();
            Assert.That(gltfRoot, Is.Not.Null);
            Assert.That(gltfRoot.Asset.Version, Is.EqualTo("2.0"));
            Assert.That(gltfRoot.Asset.Generator, Does.StartWith("NDMF VRM Exporter"));
            Assert.That(bin, Is.Not.Empty);
            Assert.That(bin.Length % 4, Is.Zero);
        }
#endif // NVE_HAS_VRCHAT_AVATAR_SDK && NVE_HAS_LILTOON
    }
}
