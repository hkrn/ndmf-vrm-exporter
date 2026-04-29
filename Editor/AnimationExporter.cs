// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    internal static class AnimationExporter
    {
        private static readonly string VrmcAnimation = "VRMC_vrm_animation";

        private class AnimationUnit
        {
            public gltf.exporter.KeyframeAccessorBundle Bundle { get; } = new();
            public Transform Transform { get; init; } = null!;
            public gltf.ObjectID NodeID;
        }

        private class AnimationModeScope : IDisposable
        {
            public AnimationModeScope()
            {
                _hasAlreadyInAnimationMode = AnimationMode.InAnimationMode();
                if (!_hasAlreadyInAnimationMode)
                {
                    AnimationMode.StartAnimationMode();
                }
            }

            public void Dispose()
            {
                if (!_hasAlreadyInAnimationMode)
                {
                    AnimationMode.StopAnimationMode();
                }
            }

            private readonly bool _hasAlreadyInAnimationMode;
        }

        private struct HumanBodyNode
        {
            public Vector3 Translation { get; init; }
            public Quaternion Rotation { get; init; }
            public Vector3 Scale { get; init; }
            public HumanBodyBones ParentHumanBodyBones { get; init; }
        }

        public static void Export(GameObject avatar, AnimationClip clip, string outputDirectoryPath)
        {
            var avatarRoot = Object.Instantiate(avatar);
            try
            {
                ExportAnimation(avatarRoot, clip, outputDirectoryPath);
            }
            finally
            {
                Object.DestroyImmediate(avatarRoot);
                // ReSharper disable once RedundantAssignment
                avatarRoot = null;
            }
        }

        private static void ExportAnimation(GameObject avatarRoot, AnimationClip humanoidAnimationClip, string outputDirectoryPath)
        {
            var packageJsonFile = File.ReadAllText($"Packages/{NdmfVrmExporter.PackageJson.Name}/package.json");
            var packageJson = NdmfVrmExporter.PackageJson.LoadFromString(packageJsonFile);
            var root = new gltf.Root
            {
                Asset = new gltf.asset.Asset()
                {
                    Version = "2.0",
                    Generator = $"{packageJson.DisplayName} {packageJson.Version}",
                },
                Accessors = new List<gltf.accessor.Accessor>(),
                Animations = new List<gltf.animation.Animation>(),
                Buffers = new List<gltf.buffer.Buffer>(),
                BufferViews = new List<gltf.buffer.BufferView>(),
                Extensions = new Dictionary<string, JToken>(),
                ExtensionsUsed = new List<string>
                {
                    VrmcAnimation,
                },
                Nodes = new List<gltf.node.Node>()
                {
                    new()
                    {
                        Translation = System.Numerics.Vector3.Zero,
                        Rotation = System.Numerics.Quaternion.Identity,
                        Scale = System.Numerics.Vector3.One,
                        Children = new List<gltf.ObjectID> { new(1) },
                    }
                },
                Scenes = new List<gltf.scene.Scene>
                {
                    new()
                    {
                        Name = new gltf.UnicodeString("root"),
                        Nodes = new List<gltf.ObjectID>
                        {
                            new(0),
                        }
                    }
                },
                Scene = new gltf.ObjectID(0),
            };

            var units = new Dictionary<HumanBodyBones, AnimationUnit>();
            var animator = avatarRoot.GetComponent<Animator>();
            for (HumanBodyBones humanBodyBoneId = 0; humanBodyBoneId < HumanBodyBones.LastBone; humanBodyBoneId++)
            {
                var transform = animator.GetBoneTransform(humanBodyBoneId);
                if (transform == null)
                {
                    continue;
                }

                var unit = new AnimationUnit { Transform = transform };
                var humanBodyNode = GetHumanBodyNode(transform, humanBodyBoneId, units);
                var nodeID = new gltf.ObjectID((uint)root.Nodes.Count);
                unit.NodeID = nodeID;
                root.Nodes.Add(new gltf.node.Node
                {
                    Name = new gltf.UnicodeString(transform.name),
                    Translation = humanBodyNode.Translation.ToVector3WithCoordinateSpace(),
                    Rotation = humanBodyNode.Rotation.ToQuaternionWithCoordinateSpace(),
                    Scale = humanBodyNode.Scale.ToVector3(),
                    Children = new List<gltf.ObjectID>(),
                });
                if (units.TryGetValue(humanBodyNode.ParentHumanBodyBones, out var parentUnit))
                {
                    root.Nodes[(int)parentUnit.NodeID.ID].Children?.Add(nodeID);
                }

                units.Add(humanBodyBoneId, unit);
            }

            using var _ = new AnimationModeScope();
            var times = new HashSet<float>();
            foreach (var binding in AnimationUtility.GetCurveBindings(humanoidAnimationClip))
            {
                var curve = AnimationUtility.GetEditorCurve(humanoidAnimationClip, binding);
                foreach (var key in curve.keys)
                {
                    times.Add(key.time);
                }
            }

            var sortedTimes = times.OrderBy(time => time).ToArray();
            foreach (var time in sortedTimes)
            {
                humanoidAnimationClip.SampleAnimation(avatarRoot, time);
                for (HumanBodyBones humanBodyBoneId = 0; humanBodyBoneId < HumanBodyBones.LastBone; humanBodyBoneId++)
                {
                    if (!units.TryGetValue(humanBodyBoneId, out var unit))
                    {
                        continue;
                    }

                    var transform = unit.Transform;
                    var humanBodyNode = GetHumanBodyNode(transform, humanBodyBoneId, units);
                    unit.Bundle.Rotations.Keyframes.Add(new gltf.exporter.KeyframeUnit(time,
                        humanBodyNode.Rotation.ToQuaternionWithCoordinateSpace()));
                    if (humanBodyBoneId != HumanBodyBones.Hips)
                    {
                        continue;
                    }

                    unit.Bundle.Translations.Keyframes.Add(new gltf.exporter.KeyframeUnit(time,
                        humanBodyNode.Translation.ToVector3WithCoordinateSpace()));
                }
            }

            var exporter = new gltf.exporter.Exporter();
            var animation = new gltf.animation.Animation
            {
                Channels = new List<gltf.animation.Channel>(),
                Samplers = new List<gltf.animation.AnimationSampler>(),
            };
            for (HumanBodyBones humanBodyBoneId = 0; humanBodyBoneId < HumanBodyBones.LastBone; humanBodyBoneId++)
            {
                if (!units.TryGetValue(humanBodyBoneId, out var unit))
                {
                    continue;
                }

                var transform = unit.Transform;
                var name = new gltf.UnicodeString(transform.name);
                var output = exporter.SerializeAnimationSamplerBundleOutput(root, name, unit.Bundle);
                if (output.Rotations == null)
                {
                    continue;
                }

                var nodeID = unit.NodeID;
                var rotationAnimationSamplerId = new gltf.ObjectID((uint)animation.Samplers.Count);
                animation.Samplers.Add(output.Rotations);
                animation.Channels.Add(new gltf.animation.Channel
                {
                    Sampler = rotationAnimationSamplerId,
                    Target = new gltf.animation.ChannelTarget
                    {
                        Node = nodeID,
                        Path = gltf.animation.Path.Rotation,
                    }
                });
                if (output.Translations == null)
                {
                    continue;
                }

                var translationAnimationSamplerId = new gltf.ObjectID((uint)animation.Samplers.Count);
                animation.Samplers.Add(output.Translations);
                animation.Channels.Add(new gltf.animation.Channel
                {
                    Sampler = translationAnimationSamplerId,
                    Target = new gltf.animation.ChannelTarget
                    {
                        Node = nodeID,
                        Path = gltf.animation.Path.Translation,
                    }
                });
            }

            root.Animations.Add(animation);
            root.Buffers.Add(new gltf.buffer.Buffer
            {
                ByteLength = exporter.Length,
            });
            var metadata = new Dictionary<string, gltf.UnicodeString>
            {
                {"origin.name", new gltf.UnicodeString(humanoidAnimationClip.name) },
                {"origin.uuid", new gltf.UnicodeString(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(humanoidAnimationClip)))},
            };
            var extras = new Dictionary<string, object>
            {
                { NdmfVrmExporter.PackageJson.Name, metadata }
            };
            var vrma = new vrm.animation.Animation
            {
                Humanoid = new vrm.animation.Humanoid
                {
                    HumanBones = new vrm.animation.HumanBones
                    {
                        Hips = FindHumanBoneNode(HumanBodyBones.Hips, units)!,
                        Spine = FindHumanBoneNode(HumanBodyBones.Spine, units)!,
                        Chest = FindHumanBoneNode(HumanBodyBones.Chest, units),
                        UpperChest = FindHumanBoneNode(HumanBodyBones.UpperChest, units),
                        Neck = FindHumanBoneNode(HumanBodyBones.Neck, units),
                        Head = FindHumanBoneNode(HumanBodyBones.Head, units)!,
                        LeftEye = FindHumanBoneNode(HumanBodyBones.LeftEye, units),
                        RightEye = FindHumanBoneNode(HumanBodyBones.RightEye, units),
                        Jaw = FindHumanBoneNode(HumanBodyBones.Jaw, units),
                        LeftUpperLeg = FindHumanBoneNode(HumanBodyBones.LeftUpperLeg, units)!,
                        LeftLowerLeg = FindHumanBoneNode(HumanBodyBones.LeftLowerLeg, units)!,
                        LeftFoot = FindHumanBoneNode(HumanBodyBones.LeftFoot, units)!,
                        LeftToes = FindHumanBoneNode(HumanBodyBones.LeftToes, units),
                        RightUpperLeg = FindHumanBoneNode(HumanBodyBones.RightUpperLeg, units)!,
                        RightLowerLeg = FindHumanBoneNode(HumanBodyBones.RightLowerLeg, units)!,
                        RightFoot = FindHumanBoneNode(HumanBodyBones.RightFoot, units)!,
                        RightToes = FindHumanBoneNode(HumanBodyBones.RightToes, units),
                        LeftShoulder = FindHumanBoneNode(HumanBodyBones.LeftShoulder, units),
                        LeftUpperArm = FindHumanBoneNode(HumanBodyBones.LeftUpperArm, units)!,
                        LeftLowerArm = FindHumanBoneNode(HumanBodyBones.LeftLowerArm, units)!,
                        LeftHand = FindHumanBoneNode(HumanBodyBones.LeftHand, units)!,
                        RightShoulder = FindHumanBoneNode(HumanBodyBones.RightShoulder, units),
                        RightUpperArm = FindHumanBoneNode(HumanBodyBones.RightUpperArm, units)!,
                        RightLowerArm = FindHumanBoneNode(HumanBodyBones.RightLowerArm, units)!,
                        RightHand = FindHumanBoneNode(HumanBodyBones.RightHand, units)!,
                        LeftThumbMetacarpal = FindHumanBoneNode(HumanBodyBones.LeftThumbProximal, units),
                        LeftThumbProximal = FindHumanBoneNode(HumanBodyBones.LeftThumbIntermediate, units),
                        LeftThumbDistal = FindHumanBoneNode(HumanBodyBones.LeftThumbDistal, units),
                        LeftIndexProximal = FindHumanBoneNode(HumanBodyBones.LeftIndexProximal, units),
                        LeftIndexIntermediate = FindHumanBoneNode(HumanBodyBones.LeftIndexIntermediate, units),
                        LeftIndexDistal = FindHumanBoneNode(HumanBodyBones.LeftIndexDistal, units),
                        LeftMiddleProximal = FindHumanBoneNode(HumanBodyBones.LeftMiddleProximal, units),
                        LeftMiddleIntermediate = FindHumanBoneNode(HumanBodyBones.LeftMiddleIntermediate, units),
                        LeftMiddleDistal = FindHumanBoneNode(HumanBodyBones.LeftMiddleDistal, units),
                        LeftRingProximal = FindHumanBoneNode(HumanBodyBones.LeftRingProximal, units),
                        LeftRingIntermediate = FindHumanBoneNode(HumanBodyBones.LeftRingIntermediate, units),
                        LeftRingDistal = FindHumanBoneNode(HumanBodyBones.LeftRingDistal, units),
                        LeftLittleProximal = FindHumanBoneNode(HumanBodyBones.LeftLittleProximal, units),
                        LeftLittleIntermediate = FindHumanBoneNode(HumanBodyBones.LeftLittleIntermediate, units),
                        LeftLittleDistal = FindHumanBoneNode(HumanBodyBones.LeftLittleDistal, units),
                        RightThumbMetacarpal = FindHumanBoneNode(HumanBodyBones.RightThumbProximal, units),
                        RightThumbProximal = FindHumanBoneNode(HumanBodyBones.RightThumbIntermediate, units),
                        RightThumbDistal = FindHumanBoneNode(HumanBodyBones.RightThumbDistal, units),
                        RightIndexProximal = FindHumanBoneNode(HumanBodyBones.RightIndexProximal, units),
                        RightIndexIntermediate = FindHumanBoneNode(HumanBodyBones.RightIndexIntermediate, units),
                        RightIndexDistal = FindHumanBoneNode(HumanBodyBones.RightIndexDistal, units),
                        RightMiddleProximal = FindHumanBoneNode(HumanBodyBones.RightMiddleProximal, units),
                        RightMiddleIntermediate = FindHumanBoneNode(HumanBodyBones.RightMiddleIntermediate, units),
                        RightMiddleDistal = FindHumanBoneNode(HumanBodyBones.RightMiddleDistal, units),
                        RightRingProximal = FindHumanBoneNode(HumanBodyBones.RightRingProximal, units),
                        RightRingIntermediate = FindHumanBoneNode(HumanBodyBones.RightRingIntermediate, units),
                        RightRingDistal = FindHumanBoneNode(HumanBodyBones.RightRingDistal, units),
                        RightLittleProximal = FindHumanBoneNode(HumanBodyBones.RightLittleProximal, units),
                        RightLittleIntermediate = FindHumanBoneNode(HumanBodyBones.RightLittleIntermediate, units),
                        RightLittleDistal = FindHumanBoneNode(HumanBodyBones.RightLittleDistal, units),
                    }
                },
                Extras = JToken.FromObject(extras, JsonSerializer.Create(gltf.Document.SerializerOptions)),
            };
            root.Extensions.Add(VrmcAnimation, vrm.Document.SaveAsNode(vrma));
            root.Normalize();
            var json = gltf.Document.SaveAsString(root);
            using var stream = new MemoryStream();
            exporter.Export(json, stream);
            var outputPath = $"{outputDirectoryPath}/{AssetPathUtils.TrimCloneSuffix(humanoidAnimationClip.name)}.vrma";
            File.WriteAllBytes(outputPath, stream.GetBuffer());
        }


        private static HumanBodyNode GetHumanBodyNode(Transform transform, HumanBodyBones humanBodyBoneId,
            Dictionary<HumanBodyBones, AnimationUnit> units)
        {
            var parentId = HumanTrait.GetParentBone((int)humanBodyBoneId);
            if (parentId == -1)
            {
                return new HumanBodyNode
                {
                    Translation = transform.position,
                    Rotation = transform.rotation,
                    Scale = transform.lossyScale,
                    ParentHumanBodyBones = HumanBodyBones.LastBone,
                };
            }

            var parentHumanBodyBoneId = (HumanBodyBones)parentId;
            var parentTransform = units.TryGetValue(parentHumanBodyBoneId, out var unit) ? unit.Transform : null;
            while (parentTransform == null)
            {
                parentId = HumanTrait.GetParentBone(parentId);
                if (parentId == -1)
                {
                    break;
                }

                parentHumanBodyBoneId = (HumanBodyBones)parentId;
                parentTransform = units.TryGetValue(parentHumanBodyBoneId, out unit) ? unit.Transform : null;
            }

            var transformMatrix = parentTransform!.worldToLocalMatrix * transform.localToWorldMatrix;
            return new HumanBodyNode
            {
                Translation = transformMatrix.GetPosition(),
                Rotation = transformMatrix.rotation,
                Scale = transformMatrix.lossyScale,
                ParentHumanBodyBones = parentHumanBodyBoneId
            };
        }

        private static vrm.animation.HumanBone? FindHumanBoneNode(HumanBodyBones humanBodyBoneId,
            Dictionary<HumanBodyBones, AnimationUnit> units)
        {
            return units.TryGetValue(humanBodyBoneId, out var unit)
                ? new vrm.animation.HumanBone { Node = unit.NodeID }
                : null;
        }
    }
}
