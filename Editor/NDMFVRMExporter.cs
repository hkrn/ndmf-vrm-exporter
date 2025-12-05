// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

#if NVE_HAS_VRCHAT_AVATAR_SDK
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

using nadena.dev.ndmf;
using nadena.dev.ndmf.localization;

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    internal sealed class MaterialVariantMapping
    {
        public Renderer Renderer { get; init; } = null!;
        public Material[] Materials { get; init; } = { };
    }

    internal sealed class MaterialVariant
    {
        public string? Name { get; init; }
        public MaterialVariantMapping[] Mappings { get; init; } = { };
    }

    internal sealed class NdmfVrmExporter : IDisposable
    {
        private static readonly string VrmcVrm = "VRMC_vrm";
        private static readonly string VrmcSpringBone = "VRMC_springBone";
        private static readonly string VrmcSpringBoneExtendedCollider = "VRMC_springBone_extended_collider";
        private static readonly string VrmcNodeConstraint = "VRMC_node_constraint";
        private static readonly string VrmcMaterialsMtoon = "VRMC_materials_mtoon";
        private static readonly int PropertyCull = Shader.PropertyToID("_Cull");
        private static readonly int PropertyUseEmission = Shader.PropertyToID("_UseEmission");
        private static readonly int PropertyEmissionMainStrength = Shader.PropertyToID("_EmissionMainStrength");
        private static readonly int PropertyEmissionBlendMask = Shader.PropertyToID("_EmissionBlendMask");
        private static readonly int PropertyUseBumpMap = Shader.PropertyToID("_UseBumpMap");
        private static readonly int PropertyAlphaMaskMode = Shader.PropertyToID("_AlphaMaskMode");

        public sealed class PackageJson
        {
            public const string Name = "com.github.hkrn.ndmf-vrm-exporter";
            public string DisplayName { get; set; } = null!;
            public string Version { get; set; } = null!;

            public static PackageJson LoadFromString(string json)
            {
                return JsonConvert.DeserializeObject<PackageJson>(json, new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    },
                    DefaultValueHandling = DefaultValueHandling.Include,
                    NullValueHandling = NullValueHandling.Ignore,
                })!;
            }
        }

        private sealed class ScopedProfile : IDisposable
        {
            public ScopedProfile(string name)
            {
                _name = $"NDMFVRMExporter.{name}";
                _sw = new Stopwatch();
                _sw.Start();
            }

            public void Dispose()
            {
                _sw.Stop();
                WriteResult();
            }

            [Conditional("DEBUG")]
            private void WriteResult()
            {
                Debug.Log($"{_name}: {_sw.ElapsedMilliseconds}ms");
            }

            private readonly Stopwatch _sw;
            private readonly string _name;
        }

        private sealed class MToonTexture
        {
            public Texture? MainTexture { get; set; }
            public gltf.material.TextureInfo? MainTextureInfo { get; set; }
        }

        private sealed class KtxConverterBuilder
        {
            public KtxConverterBuilder(string ktxToolPath)
            {
                _ktxToolPath = ktxToolPath;
            }

            public string InternalFormat { set; private get; } = null!;

            public string Oetf { set; private get; } = null!;

            public string Primaries { set; private get; } = null!;

            public KtxConverter Build()
            {
                var info = new ProcessStartInfo(_ktxToolPath);
                info.ArgumentList.Add("create");
                info.ArgumentList.Add("--format");
                info.ArgumentList.Add(InternalFormat);
                info.ArgumentList.Add("--assign-oetf");
                info.ArgumentList.Add(Oetf);
                info.ArgumentList.Add("--assign-primaries");
                info.ArgumentList.Add(Primaries);
                info.ArgumentList.Add("--encode");
                info.ArgumentList.Add("uastc");
                info.ArgumentList.Add("--zstd");
                info.ArgumentList.Add("22");
                info.ArgumentList.Add("--generate-mipmap");
                info.ArgumentList.Add("--stdin");
                info.ArgumentList.Add("--stdout");
                info.CreateNoWindow = true;
                info.UseShellExecute = false;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                return new KtxConverter(info);
            }

            private readonly string _ktxToolPath;
        }

        private sealed class KtxConverter
        {
            internal KtxConverter(ProcessStartInfo info)
            {
                _info = info;
            }

            public byte[]? Run(byte[] inputData)
            {
                using var process = Process.Start(_info);
                if (process is null)
                {
                    return null;
                }

                {
                    using var writer = new BinaryWriter(process.StandardInput.BaseStream);
                    writer.Write(inputData);
                }
                using var reader = new MemoryStream();
                process.StandardOutput.BaseStream.CopyTo(reader);
                process.WaitForExit(30000);
                return reader.GetBuffer();
            }

            private readonly ProcessStartInfo _info;
        }

        public NdmfVrmExporter(GameObject gameObject, IAssetSaver assetSaver,
            IReadOnlyList<MaterialVariant> materialVariants)
        {
            var packageJsonFile = File.ReadAllText($"Packages/{PackageJson.Name}/package.json");
            var packageJson = PackageJson.LoadFromString(packageJsonFile);
            _gameObject = gameObject;
            _assetSaver = assetSaver;
            _materialIDs = new Dictionary<Material, gltf.ObjectID>();
            _materialMToonTextures = new Dictionary<Material, MToonTexture>();
            _transformNodeIDs = new Dictionary<Transform, gltf.ObjectID>();
            _transformNodeNames = new HashSet<string>();
            _materialVariants = materialVariants;
            _exporter = new gltf.exporter.Exporter();
            _root = new gltf.Root
            {
                Accessors = new List<gltf.accessor.Accessor>(),
                Asset = new gltf.asset.Asset
                {
                    Version = "2.0",
                    Generator = $"{packageJson.DisplayName} {packageJson.Version}",
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
            _root.Scenes.Add(new gltf.scene.Scene()
            {
                Nodes = new List<gltf.ObjectID> { new(0) },
            });
            _extensionsUsed = new SortedSet<string>();
            _materialExporter = new GltfMaterialExporter(_root, _exporter, _extensionsUsed);
        }

        public void Dispose()
        {
            _exporter.Dispose();
        }

        public string Export(Stream stream)
        {
            var rootTransform = _gameObject.transform;
            var translation = System.Numerics.Vector3.Zero;
            var rotation = System.Numerics.Quaternion.Identity;
            var scale = rootTransform.localScale.ToVector3();
            var rootNode = new gltf.node.Node
            {
                Name = new gltf.UnicodeString(AssetPathUtils.TrimCloneSuffix(rootTransform.name)),
                Children = new List<gltf.ObjectID>(),
                Translation = translation,
                Rotation = rotation,
                Scale = scale,
            };
            var nodes = _root.Nodes!;
            var nodeID = new gltf.ObjectID((uint)nodes.Count);
            _transformNodeIDs.Add(rootTransform, nodeID);
            _transformNodeNames.Add(rootTransform.name);
            nodes.Add(rootNode);
            var component = _gameObject.GetComponent<NdmfVrmExporterComponent>();
            using (var _ = new ScopedProfile(nameof(RetrieveAllTransforms)))
            {
                RetrieveAllTransforms(rootTransform, component.makeAllNodeNamesUnique);
            }

            using (var _ = new ScopedProfile(nameof(RetrieveAllNodes)))
            {
                RetrieveAllNodes(rootTransform);
            }

            using (var _ = new ScopedProfile(nameof(RetrieveAllMeshRenderers)))
            {
                RetrieveAllMeshRenderers(rootTransform, component);
            }

            using (var _ = new ScopedProfile(nameof(ConvertAllMaterialVariants)))
            {
                ConvertAllMaterialVariants(component);
            }

            if (!string.IsNullOrEmpty(component.ktxToolPath) && File.Exists(component.ktxToolPath))
            {
                using var _ = new ScopedProfile(nameof(ConvertAllTexturesToKtx));
                ConvertAllTexturesToKtx(component.ktxToolPath!);
            }

            using (var _ = new ScopedProfile(nameof(ExportAllVrmExtensions)))
            {
                ExportAllVrmExtensions();
            }

            _root.Buffers!.Add(new gltf.buffer.Buffer
            {
                ByteLength = _exporter.Length,
            });
            _root.ExtensionsUsed = _extensionsUsed.ToList();
            _root.Normalize();
            var json = gltf.Document.SaveAsString(_root);
            _exporter.Export(json, stream);
            return json;
        }

        private void RetrieveAllTransforms(Transform parent, bool uniqueNodeName)
        {
            foreach (Transform child in parent)
            {
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var nodeName = AssetPathUtils.TrimCloneSuffix(child.name);
                if (uniqueNodeName && _transformNodeNames.Contains(nodeName))
                {
                    var renamedNodeName = nodeName;
                    var i = 1;
                    while (_transformNodeNames.Contains(renamedNodeName))
                    {
                        renamedNodeName = $"{nodeName}_{i}";
                        i++;
                    }

                    nodeName = renamedNodeName;
                }

                var translation = child.localPosition.ToVector3WithCoordinateSpace();
                var rotation = child.localRotation.ToQuaternionWithCoordinateSpace();
                var scale = child.localScale.ToVector3();
                var node = new gltf.node.Node
                {
                    Name = new gltf.UnicodeString(nodeName),
                    Children = new List<gltf.ObjectID>(),
                    Translation = translation,
                    Rotation = rotation,
                    Scale = scale,
                };
                var nodes = _root.Nodes!;
                var nodeID = new gltf.ObjectID((uint)nodes.Count);
                nodes.Add(node);
                _transformNodeIDs.Add(child, nodeID);
                _transformNodeNames.Add(nodeName);
                RetrieveAllTransforms(child, uniqueNodeName);
            }
        }

        private void RetrieveAllMeshRenderers(Transform parent, NdmfVrmExporterComponent component)
        {
            foreach (Transform child in parent)
            {
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var nodeID = _transformNodeIDs[child];
                var node = _root.Nodes![(int)nodeID.ID];
                if (child.gameObject.TryGetComponent<SkinnedMeshRenderer>(out var smr) && smr.sharedMesh)
                {
                    RetrieveMesh(smr.sharedMesh, smr.sharedMaterials, smr.sharedMaterial, component, child, smr,
                        ref node);
                }
                else if (child.gameObject.TryGetComponent<MeshRenderer>(out var mr) &&
                         mr.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh)
                {
                    RetrieveMesh(filter.sharedMesh, mr.sharedMaterials, mr.sharedMaterial, component, child, null,
                        ref node);
                }

                RetrieveAllMeshRenderers(child, component);
            }
        }

        private void ConvertAllMaterialVariants(NdmfVrmExporterComponent component)
        {
            var enableBakingAlphaMaskTexture = component.enableBakingAlphaMaskTexture;
            var materialVariants = new gltf.extensions.KhrMaterialsVariants();
            var variantIndex = 0u;
            foreach (var variant in _materialVariants)
            {
                materialVariants.Variants.Add(new gltf.extensions.KhrMaterialsVariantsItem
                {
                    Name = new gltf.UnicodeString(variant.Name ?? $"Variant{variantIndex}")
                });
            }

            var allMeshMaterialMappings =
                new Dictionary<(gltf.ObjectID, int), gltf.extensions.KhrMaterialsVariantsPrimitive>();
            variantIndex = 0u;
            foreach (var variant in _materialVariants)
            {
                var mappingIndex = 0u;
                foreach (var mapping in variant.Mappings)
                {
                    var renderer = mapping.Renderer;
                    if (!renderer)
                    {
                        Debug.LogWarning(
                            $"Cannot convert material variant {variant.Name}:{mappingIndex} due to renderer is null");
                        continue;
                    }

                    var nodeID = _transformNodeIDs[renderer.transform];
                    var node = _root.Nodes![(int)nodeID.ID];
                    if (!node.Mesh.HasValue)
                    {
                        Debug.LogWarning(
                            $"Cannot convert material variant {variant.Name}:{mappingIndex} due to mesh is none");
                        continue;
                    }

                    var meshID = node.Mesh!.Value;
                    var mesh = _root.Meshes![(int)meshID.ID];
                    var primitives = mesh.Primitives!;
                    var primitiveIndex = 0;
                    foreach (var material in mapping.Materials)
                    {
                        if (primitiveIndex >= primitives.Count)
                        {
                            break;
                        }

                        gltf.ObjectID materialID;
                        if (material)
                        {
                            if (!_materialIDs.TryGetValue(material, out materialID))
                            {
                                materialID = ConvertMaterial(material, enableBakingAlphaMaskTexture, out _);
                            }
                        }
                        else
                        {
                            materialID = primitives[primitiveIndex].Material ?? gltf.ObjectID.Null;
                        }

                        if (allMeshMaterialMappings.TryGetValue((meshID, primitiveIndex), out var materialMappings))
                        {
                            var foundMaterialMapping =
                                materialMappings.Mappings.FirstOrDefault(item => item.Material.Equals(materialID));
                            if (foundMaterialMapping != null)
                            {
                                foundMaterialMapping.Variants.Add(new gltf.ObjectID(variantIndex));
                            }
                            else
                            {
                                materialMappings.Mappings.Add(new gltf.extensions.KhrMaterialsVariantsPrimitiveMapping
                                {
                                    Material = materialID,
                                    Variants = new List<gltf.ObjectID> { new(variantIndex) },
                                });
                            }
                        }
                        else
                        {
                            var materialMapping = new gltf.extensions.KhrMaterialsVariantsPrimitiveMapping
                            {
                                Material = materialID,
                                Variants = new List<gltf.ObjectID> { new(variantIndex) },
                            };
                            allMeshMaterialMappings.Add((meshID, primitiveIndex),
                                new gltf.extensions.KhrMaterialsVariantsPrimitive
                                {
                                    Mappings = new List<gltf.extensions.KhrMaterialsVariantsPrimitiveMapping>
                                        { materialMapping }
                                });
                        }

                        primitiveIndex++;
                    }

                    mappingIndex++;
                }

                variantIndex++;
            }

            if (allMeshMaterialMappings.Count <= 0)
            {
                return;
            }

            _root.Extensions!.Add(gltf.extensions.KhrMaterialsVariants.Name,
                gltf.Document.SaveAsNode(materialVariants));
            _extensionsUsed.Add(gltf.extensions.KhrMaterialsVariants.Name);
            foreach (var ((meshID, primitiveIndex), variantPrimitive) in allMeshMaterialMappings)
            {
                var mesh = _root.Meshes![(int)meshID.ID];
                var primitive = mesh.Primitives![primitiveIndex];
                primitive.Extensions ??= new Dictionary<string, JToken>();
                primitive.Extensions.Add(gltf.extensions.KhrMaterialsVariants.Name,
                    gltf.Document.SaveAsNode(variantPrimitive));
            }
        }

        private bool HasEmptySourceConstraint()
        {
            foreach (var (transform, _) in _transformNodeIDs)
            {
#if NVE_HAS_VRCHAT_AVATAR_SDK
                if (transform.TryGetComponent<VRCConstraintBase>(out var vcb) && vcb.Sources.Count == 0)
                {
                    return true;
                }
#endif // NVE_HAS_VRCHAT_AVATAR_SDK
                if (transform.TryGetComponent<IConstraint>(out var cb) && cb.sourceCount == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void ExportAllVrmExtensions()
        {
            var allMorphTargets = new Dictionary<string, (gltf.ObjectID, int)>();
            foreach (var (_, nodeID) in _transformNodeIDs)
            {
                var node = _root.Nodes![(int)nodeID.ID];
                var meshID = node.Mesh;
                if (!meshID.HasValue)
                    continue;
                var mesh = _root.Meshes![(int)meshID.Value.ID];
                if (mesh.Extras is null)
                    continue;
                var names = (JArray)mesh.Extras["targetNames"]!;
                var index = 0;
                foreach (var item in names)
                {
                    var key = Regex.Unescape(item.ToString()).Trim('"');
                    if (!allMorphTargets.TryAdd(key, (nodeID, index)))
                    {
                        Debug.LogWarning($"Morph target {key} is duplicated and ignored");
                    }

                    index++;
                }
            }

            var vrmExporter =
                new VrmRootExporter(_gameObject, _assetSaver, _transformNodeIDs, allMorphTargets, _extensionsUsed);
            var thumbnailImage = gltf.ObjectID.Null;
            var component = _gameObject.GetComponent<NdmfVrmExporterComponent>();
            if (component.thumbnail)
            {
                var thumbnail = component.thumbnail!;
                if (thumbnail.width == thumbnail.height)
                {
                    var name = $"VRM_Core_Meta_Thumbnail_{AssetPathUtils.TrimCloneSuffix(component.name)}";
                    var textureUnit =
                        ExportTextureUnit(thumbnail, name, TextureFormat.RGB24, ColorSpace.Gamma, null, true);
                    thumbnailImage = _exporter.CreateSampledTexture(_root, textureUnit);
                }
            }

            if (!string.IsNullOrWhiteSpace(component.copyrightInformation))
            {
                _root.Asset.Copyright = component.copyrightInformation;
            }

            _root.Extensions!.Add(VrmcVrm, vrm.Document.SaveAsNode(vrmExporter.ExportCore(thumbnailImage)));
            _root.Extensions!.Add(VrmcSpringBone, vrm.Document.SaveAsNode(vrmExporter.ExportSpringBone()));
            _extensionsUsed.Add(VrmcVrm);
            _extensionsUsed.Add(VrmcSpringBone);
            var immobileNodeID = gltf.ObjectID.Null;
            if (HasEmptySourceConstraint())
            {
                immobileNodeID = new gltf.ObjectID((uint)_root.Nodes!.Count);
                var name = AssetPathUtils.TrimCloneSuffix(_gameObject.name);
                _root.Nodes!.Add(new gltf.node.Node
                {
                    Name = new gltf.UnicodeString($"{name}_Constraint_ImmobileConstraintRootNode")
                });
                _root.Nodes.First().Children!.Add(immobileNodeID);
            }

            foreach (var (transform, nodeID) in _transformNodeIDs)
            {
                if (component.excludedConstraintTransforms.Contains(transform))
                    continue;
                var node = _root.Nodes![(int)nodeID.ID];
                var constraint = vrmExporter.ExportNodeConstraint(transform, immobileNodeID);
                if (constraint == null)
                    continue;
                node.Extensions ??= new Dictionary<string, JToken>();
                node.Extensions.Add(VrmcNodeConstraint, vrm.Document.SaveAsNode(constraint));
                _extensionsUsed.Add(VrmcNodeConstraint);
            }

            var materialIDs = new List<Material>(_materialIDs.Count);
            foreach (var (material, nodeID) in _materialIDs)
            {
                materialIDs.Insert((int)nodeID.ID, material);
            }

            var materialID = 0;
            foreach (var gltfMaterial in _root.Materials!)
            {
                var material = materialIDs[materialID];
                if (_materialMToonTextures.TryGetValue(material, out var mToonTexture))
                {
                    var mtoon = vrmExporter.ExportMToon(material, mToonTexture, _materialExporter);
                    gltfMaterial.Extensions ??= new Dictionary<string, JToken>();
                    gltfMaterial.Extensions.Add(gltf.extensions.KhrMaterialsUnlit.Name, new JObject());
                    gltfMaterial.Extensions.Add(VrmcMaterialsMtoon, vrm.Document.SaveAsNode(mtoon));
                    _extensionsUsed.Add(gltf.extensions.KhrMaterialsUnlit.Name);
                    _extensionsUsed.Add(VrmcMaterialsMtoon);
                }

                materialID++;
            }
        }

        private void ConvertAllTexturesToKtx(string ktxToolPath)
        {
            using var _ = new ScopedProfile($"{nameof(ConvertAllTexturesToKtx)}");
            var builder = new KtxConverterBuilder(ktxToolPath);
            var basePath = AssetPathUtils.GetTempPath(_gameObject);
            var textureIndex = 0;
            Directory.CreateDirectory(basePath);
            foreach (var texture in _root.Textures!)
            {
                var textureID = new gltf.ObjectID((uint)textureIndex);
                if (!_materialExporter.TextureMetadata.TryGetValue(textureID, out var metadata))
                {
                    continue;
                }

                var internalFormat = metadata.KtxImageDataFormat;
                if (internalFormat != null)
                {
                    var source = texture.Source!.Value;
                    var (oetf, primaries) = metadata.ColorSpace switch
                    {
                        ColorSpace.Gamma => ("srgb", "bt709"),
                        ColorSpace.Linear => ("linear", "none"),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    var image = _root.Images![(int)source.ID];
                    var bufferView = _root.BufferViews![(int)image.BufferView!.Value.ID];
                    var inputData = _exporter.GetData(bufferView);
                    try
                    {
                        builder.InternalFormat = internalFormat;
                        builder.Oetf = oetf;
                        builder.Primaries = primaries;
                        var converter = builder.Build();
                        var outputData = converter.Run(inputData);
                        if (outputData is null)
                        {
                            continue;
                        }

                        var sourceID = _exporter.CreateTextureSource(_root, outputData, texture.Name, "image/ktx2");
                        texture.Extensions ??= new Dictionary<string, JToken>();
                        texture.Extensions.Add(gltf.extensions.KhrTextureBasisu.Name, gltf.Document.SaveAsNode(
                            new gltf.extensions.KhrTextureBasisu
                            {
                                Source = sourceID,
                            }));
                        _extensionsUsed.Add(gltf.extensions.KhrTextureBasisu.Name);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(
                            $"Failed to convert KTX file (name={texture.Name}, sourceID={source.ID}): {e}");
                    }
                }

                textureIndex++;
            }
        }

        private void RetrieveAllNodes(Transform parent)
        {
            if (_transformNodeIDs.TryGetValue(parent, out var parentID))
            {
                foreach (Transform child in parent)
                {
                    RetrieveAllNodes(child);
                    if (_transformNodeIDs.TryGetValue(child, out var childID))
                    {
                        _root.Nodes![(int)parentID.ID].Children?.Add(childID);
                    }
                    else if (child.gameObject.activeInHierarchy)
                    {
                        Debug.LogWarning($"Cannot find {child} of transform");
                    }
                }
            }
            else if (parent.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"Cannot find {parent} of transform");
            }
        }

        private sealed class BoneResolver
        {
            public IList<Transform> UniqueTransforms { get; } = new List<Transform>();

            public IList<System.Numerics.Matrix4x4> InverseBindMatrices { get; } =
                new List<System.Numerics.Matrix4x4>();

            private readonly Dictionary<Transform, int> _transformMap = new();
            private readonly Matrix4x4 _inverseParentTransformMatrix;
            private readonly Transform[] _boneTransforms;
            private readonly Vector3[] _originPositions;
            private readonly Vector3[] _originNormals;
            private readonly Vector3[] _deltaPositions;
            private readonly Vector3[] _deltaNormals;
            private readonly Matrix4x4[] _boneMatrices;
            private readonly Matrix4x4[] _bindPoseMatrices;

            public BoneResolver(Transform parentTransform, SkinnedMeshRenderer skinnedMeshRenderer)
            {
                var mesh = skinnedMeshRenderer.sharedMesh;
                var bones = skinnedMeshRenderer.bones;
                var numBlendShapes = mesh.blendShapeCount;
                var numPositions = mesh.vertexCount;
                var blendShapeVertices = new Vector3[numPositions];
                var blendShapeNormals = new Vector3[numPositions];
                _originPositions = mesh.vertices.ToArray();
                _originNormals = mesh.normals.ToArray();
                _boneMatrices = bones.Select(bone => bone ? bone.localToWorldMatrix : Matrix4x4.zero).ToArray();
                _bindPoseMatrices = mesh.bindposes.ToArray();
                _deltaPositions = new Vector3[numPositions];
                _deltaNormals = new Vector3[numPositions];
                _boneTransforms = bones;
                _inverseParentTransformMatrix = parentTransform.worldToLocalMatrix;

                for (var blendShapeIndex = 0; blendShapeIndex < numBlendShapes; blendShapeIndex++)
                {
                    var weight = skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex) * 0.01f;
                    if (!(weight > 0.0))
                        continue;
                    mesh.GetBlendShapeFrameVertices(blendShapeIndex, 0, blendShapeVertices, blendShapeNormals, null);
                    for (var i = 0; i < numPositions; i++)
                    {
                        _deltaPositions[i] += blendShapeVertices[i] * weight;
                    }

                    for (var i = 0; i < numPositions; i++)
                    {
                        _deltaNormals[i] += blendShapeNormals[i] * weight;
                    }
                }

                foreach (var transform in bones)
                {
                    if (!transform || !transform.gameObject.activeInHierarchy ||
                        _transformMap.TryGetValue(transform, out var offset))
                    {
                        continue;
                    }

                    offset = UniqueTransforms.Count;
                    UniqueTransforms.Add(transform);
                    _transformMap.Add(transform, offset);
                    var inverseBindMatrix = transform.worldToLocalMatrix * parentTransform.localToWorldMatrix;
                    InverseBindMatrices.Add(inverseBindMatrix.ToNormalizedMatrix());
                }
            }

            public (ushort, float) Resolve(int index, float weight)
            {
                if (index < 0 || weight == 0.0)
                    return (0, 0);
                var transform = _boneTransforms[index];
                if (!transform)
                    return (0, 0);
                var offset = _transformMap[transform];
                return ((ushort)offset, weight);
            }

            public System.Numerics.Vector3 ConvertPosition(int index, BoneWeight item)
            {
                var originPosition = _originPositions[index] + _deltaPositions[index];
                var newPosition = Vector3.zero;
                foreach (var (sourceBoneIndex, weight) in new List<(int, float)>
                         {
                             (item.boneIndex0, item.weight0),
                             (item.boneIndex1, item.weight1),
                             (item.boneIndex2, item.weight2),
                             (item.boneIndex3, item.weight3)
                         })
                {
                    if (weight == 0)
                        continue;
                    var sourceMatrix = GetSourceMatrix(sourceBoneIndex);
                    newPosition += sourceMatrix.MultiplyPoint(originPosition) * weight;
                }

                return newPosition.ToVector3WithCoordinateSpace();
            }

            public System.Numerics.Vector3 ConvertNormal(int index, BoneWeight item)
            {
                var originNormal = _originNormals[index] + _deltaNormals[index];
                var newNormal = Vector3.zero;
                foreach (var (sourceBoneIndex, weight) in new List<(int, float)>
                         {
                             (item.boneIndex0, item.weight0),
                             (item.boneIndex1, item.weight1),
                             (item.boneIndex2, item.weight2),
                             (item.boneIndex3, item.weight3)
                         })
                {
                    if (weight == 0)
                        continue;
                    var sourceMatrix = GetSourceMatrix(sourceBoneIndex);
                    newNormal += sourceMatrix.MultiplyVector(originNormal) * weight;
                }

                return newNormal.normalized.ToVector3WithCoordinateSpace();
            }

            private Matrix4x4 GetSourceMatrix(int sourceBoneIndex)
            {
                var sourceMatrix = _inverseParentTransformMatrix * _boneMatrices[sourceBoneIndex] *
                                   _bindPoseMatrices[sourceBoneIndex];
                return sourceMatrix;
            }
        }

        private void RetrieveMesh(Mesh mesh, Material[] materials, Material fallbackMaterial,
            NdmfVrmExporterComponent component, Transform parentTransform, SkinnedMeshRenderer? smr,
            ref gltf.node.Node node)
        {
            using var _ = new ScopedProfile($"{nameof(RetrieveMesh)}({mesh.name})");
            System.Numerics.Vector3[] positions, normals;
            gltf.exporter.JointUnit[] jointUnits;
            System.Numerics.Vector4[] weights;
            if (smr)
            {
                var resolver = new BoneResolver(parentTransform, smr!);
                var boneWeights = smr!.sharedMesh.boneWeights;
                var index = 0;
                jointUnits = new gltf.exporter.JointUnit[boneWeights.Length];
                weights = new System.Numerics.Vector4[boneWeights.Length];
                foreach (var item in boneWeights)
                {
                    var (b0, w0) = resolver.Resolve(item.boneIndex0, item.weight0);
                    var (b1, w1) = resolver.Resolve(item.boneIndex1, item.weight1);
                    var (b2, w2) = resolver.Resolve(item.boneIndex2, item.weight2);
                    var (b3, w3) = resolver.Resolve(item.boneIndex3, item.weight3);
                    var jointUnit = new gltf.exporter.JointUnit
                    {
                        X = b0,
                        Y = b1,
                        Z = b2,
                        W = b3,
                    };
                    jointUnits[index] = jointUnit;
                    weights[index] = new System.Numerics.Vector4(w0, w1, w2, w3);
                    index++;
                }

                positions = new System.Numerics.Vector3[boneWeights.Length];
                normals = new System.Numerics.Vector3[boneWeights.Length];
                index = 0;
                foreach (var item in boneWeights)
                {
                    positions[index] = resolver.ConvertPosition(index, item);
                    normals[index] = resolver.ConvertNormal(index, item);
                    index++;
                }

                var skinID = new gltf.ObjectID((uint)_root.Skins!.Count);
                var joints = resolver.UniqueTransforms.Select(bone => _transformNodeIDs[bone]).ToList();
                var inverseBindMatricesAccessor =
                    _exporter.CreateMatrix4Accessor(_root, $"{smr.name}_IBM", resolver.InverseBindMatrices.ToArray());
                _root.Skins.Add(new gltf.node.Skin
                {
                    InverseBindMatrices = inverseBindMatricesAccessor,
                    Joints = joints,
                });
                node.Skin = skinID;
            }
            else
            {
                positions = mesh.vertices.Select(item => item.ToVector3WithCoordinateSpace()).ToArray();
                normals = mesh.normals.Select(item => item.normalized.ToVector3WithCoordinateSpace()).ToArray();
                jointUnits = Array.Empty<gltf.exporter.JointUnit>();
                weights = Array.Empty<System.Numerics.Vector4>();
            }

            var meshUnit = new gltf.exporter.MeshUnit
            {
                Name = new gltf.UnicodeString(AssetPathUtils.TrimCloneSuffix(mesh.name)),
                Positions = positions,
                Normals = normals,
                Colors = component.enableVertexColorOutput
                    ? mesh.colors32.Select(item => ((Color)item).ToVector4(ColorSpace.Gamma, ColorSpace.Linear))
                        .ToArray()
                    : Array.Empty<System.Numerics.Vector4>(),
                TexCoords0 = mesh.uv.Select(item => item.ToVector2WithCoordinateSpace()).ToArray(),
                TexCoords1 = mesh.uv2.Select(item => item.ToVector2WithCoordinateSpace()).ToArray(),
                Joints = jointUnits,
                Weights = weights,
                Tangents = mesh.tangents.Select(item => item.ToVector4WithTangentSpace())
                    .ToArray(),
            };
            var enableBakingAlphaMaskTexture = component.enableBakingAlphaMaskTexture;

            var numMaterials = materials.Length;
            if (numMaterials < mesh.subMeshCount)
            {
                ErrorReport.ReportError(Translator.Instance, ErrorSeverity.NonFatal,
                    "component.runtime.error.mesh.oob-materials", parentTransform.gameObject);
            }

            var noMaterialReferenceHasBeenReported = false;
            for (int i = 0, numSubMeshes = mesh.subMeshCount; i < numSubMeshes; i++)
            {
                var subMesh = mesh.GetSubMesh(i);
                var indices = mesh.GetIndices(i);
                IList<uint> newIndices;
                if (subMesh.topology == MeshTopology.Triangles)
                {
                    newIndices = new List<uint>();
                    var numIndices = indices.Length;
                    for (var j = 0; j < numIndices; j += 3)
                    {
                        newIndices.Add((uint)indices[j + 2]);
                        newIndices.Add((uint)indices[j + 1]);
                        newIndices.Add((uint)indices[j + 0]);
                    }
                }
                else
                {
                    newIndices = indices.Select(index => (uint)index).ToList();
                }

                var primitiveMode = subMesh.topology switch
                {
                    MeshTopology.Lines => gltf.mesh.PrimitiveMode.Lines,
                    MeshTopology.LineStrip => gltf.mesh.PrimitiveMode.LineStrip,
                    MeshTopology.Points => gltf.mesh.PrimitiveMode.Point,
                    MeshTopology.Triangles => gltf.mesh.PrimitiveMode.Triangles,
                    _ => throw new ArgumentOutOfRangeException(),
                };
                var subMeshMaterial = i < numMaterials ? materials[i] : fallbackMaterial;
                if (!subMeshMaterial)
                {
                    if (!noMaterialReferenceHasBeenReported)
                    {
                        ErrorReport.ReportError(Translator.Instance, ErrorSeverity.NonFatal,
                            "component.runtime.error.mesh.no-material", parentTransform.gameObject);
                        noMaterialReferenceHasBeenReported = true;
                    }

                    continue;
                }

                var materialID =
                    ConvertMaterial(subMeshMaterial, enableBakingAlphaMaskTexture, out var isShaderLiltoon);
                if (isShaderLiltoon && component.disableVertexColorOnLiltoon)
                {
                    var numColors = (uint)meshUnit.Colors.Length;
                    foreach (var index in newIndices)
                    {
                        if (index < numColors)
                        {
                            meshUnit.Colors[index] = System.Numerics.Vector4.One;
                        }
                    }
                }

                var primitiveUnit = new gltf.exporter.PrimitiveUnit
                {
                    Indices = newIndices.ToArray(),
                    Material = materialID,
                    PrimitiveMode = primitiveMode
                };
                meshUnit.Primitives.Add(primitiveUnit);
            }

            var numBlendShapes = mesh.blendShapeCount;
            var blendShapeVertices = new Vector3[mesh.vertexCount];
            var blendShapeNormals = new Vector3[mesh.vertexCount];
            for (var i = 0; i < numBlendShapes; i++)
            {
                var name = mesh.GetBlendShapeName(i);
                mesh.GetBlendShapeFrameVertices(i, 0, blendShapeVertices, blendShapeNormals, null);
                meshUnit.MorphTargets.Add(new gltf.exporter.MorphTarget
                {
                    Name = name,
                    Positions = blendShapeVertices.Select(item => item.ToVector3WithCoordinateSpace()).ToArray(),
                    Normals = blendShapeNormals.Select(item => item.ToVector3WithCoordinateSpace()).ToArray(),
                });
            }

            if (meshUnit.Primitives.Count > 0)
            {
                node.Mesh = _exporter.CreateMesh(_root, meshUnit);
            }
            else
            {
                ErrorReport.ReportError(Translator.Instance, ErrorSeverity.Information,
                    "component.runtime.error.mesh.no-primitive", parentTransform.gameObject);
            }
        }

        private gltf.ObjectID ConvertMaterial(Material subMeshMaterial, bool enableBakingAlphaMaskTexture,
            out bool isShaderLiltoon)
        {
            var shaderName = subMeshMaterial.shader.name;
            isShaderLiltoon = shaderName == "lilToon" ||
                              shaderName.StartsWith("Hidden/lilToon", StringComparison.Ordinal);
            if (_materialIDs.TryGetValue(subMeshMaterial, out var materialID))
            {
                return materialID;
            }

            var config = new GltfMaterialExporter.ExportOverrides();
#if NVE_HAS_LILTOON
            if (isShaderLiltoon)
            {
                if (shaderName.Contains("Cutout"))
                {
                    config.AlphaMode = gltf.material.AlphaMode.Mask;
                    config.MainTexture = MaterialBaker.AutoBakeMainTexture(_assetSaver, subMeshMaterial);
                }
                else if (shaderName.Contains("Transparent") || shaderName.Contains("Overlay"))
                {
                    config.AlphaMode = gltf.material.AlphaMode.Blend;
                    config.MainTexture = enableBakingAlphaMaskTexture &&
                                         Mathf.Approximately(subMeshMaterial.GetFloat(PropertyAlphaMaskMode),
                                             1.0f)
                        ? MaterialBaker.AutoBakeAlphaMask(_assetSaver, subMeshMaterial)
                        : MaterialBaker.AutoBakeMainTexture(_assetSaver, subMeshMaterial);
                }
                else
                {
                    config.AlphaMode = gltf.material.AlphaMode.Opaque;
                    config.MainTexture = MaterialBaker.AutoBakeMainTexture(_assetSaver, subMeshMaterial);
                }

                config.CullMode = subMeshMaterial.HasProperty(PropertyCull)
                    ? (int)subMeshMaterial.GetFloat(PropertyCull)
                    : (int)CullMode.Back;
                if (Mathf.Approximately(subMeshMaterial.GetFloat(PropertyUseEmission), 1.0f))
                {
                    config.EmissiveStrength = !subMeshMaterial.GetTexture(PropertyEmissionBlendMask)
                        ? 1.0f - Mathf.Clamp01(subMeshMaterial.GetFloat(PropertyEmissionMainStrength))
                        : 0.0f;
                }

                config.EnableNormalMap =
                    Mathf.Approximately(subMeshMaterial.GetFloat(PropertyUseBumpMap), 1.0f);
            }
#endif // NVE_HAS_LILTOON
            materialID = new gltf.ObjectID((uint)_root.Materials!.Count);
            var material = _materialExporter.Export(subMeshMaterial, config);
            _root.Materials!.Add(material);
            _materialIDs.Add(subMeshMaterial, materialID);

#if NVE_HAS_LILTOON
            if (!isShaderLiltoon)
                return materialID;

            var bakedMainTexture =
                _materialExporter.ResolveTexture(material.PbrMetallicRoughness!.BaseColorTexture);
            var mToonTexture = new MToonTexture();
            if (bakedMainTexture)
            {
                mToonTexture.MainTexture = bakedMainTexture;
                mToonTexture.MainTextureInfo = material.PbrMetallicRoughness!.BaseColorTexture;
            }

            _materialMToonTextures.Add(subMeshMaterial, mToonTexture);
#endif // NVE_HAS_LILTOON

            return materialID;
        }

        private static gltf.exporter.SampledTextureUnit ExportTextureUnit(Texture texture, string name,
            TextureFormat textureFormat, ColorSpace cs, Material? blitMaterial, bool needsBlit)
        {
            using var _ = new ScopedProfile($"ExportTextureUnit({name})");
            byte[] bytes;
            if (needsBlit)
            {
                var destTexture = texture.Blit(textureFormat, cs, blitMaterial);
                bytes = destTexture.EncodeToPNG();
                Object.DestroyImmediate(destTexture);
            }
            else if (texture.isReadable && texture is Texture2D tex)
            {
                bytes = tex.EncodeToPNG();
            }
            else
            {
                bytes = Array.Empty<byte>();
            }

            var textureUnit = new gltf.exporter.SampledTextureUnit
            {
                Name = new gltf.UnicodeString(name),
                MimeType = "image/png",
                Data = bytes,
                MagFilter = texture.filterMode.ToTextureFilterMode(),
                MinFilter = texture.filterMode.ToTextureFilterMode(),
                WrapS = texture.wrapModeU.ToTextureWrapMode(),
                WrapT = texture.wrapModeV.ToTextureWrapMode(),
            };
            return textureUnit;
        }

        private sealed class VrmRootExporter
        {
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
                        ConvertSpringBone(bone, immutablePbColliders, immutableColliderGroups, ref springs);
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

            public vrm.mtoon.MToon ExportMToon(Material material, MToonTexture mToonTexture,
                GltfMaterialExporter exporter)
            {
                var mtoon = new vrm.mtoon.MToon
                {
                    GIEqualizationFactor = 1.0f,
                    MatcapFactor = System.Numerics.Vector3.Zero
                };
#if NVE_HAS_LILTOON
                var so = new SerializedObject(material);
                so.Update();
                var props = so.FindProperty("m_SavedProperties");
                var textures = MaterialBaker.GetProps(props.FindPropertyRelative("m_TexEnvs"));
                var floats = MaterialBaker.GetProps(props.FindPropertyRelative("m_Floats"))
                    .ToDictionary(kv => kv.Key, kv => kv.Value.floatValue);
                var colors = MaterialBaker.GetProps(props.FindPropertyRelative("m_Colors"))
                    .ToDictionary(kv => kv.Key, kv => kv.Value.colorValue);
                var scrollRotate = colors.GetValueOrDefault("_MainTex_ScrollRotate", Color.clear);

                Texture2D? LocalRetrieveTexture2D(string name)
                {
                    var prop = textures!.GetValueOrDefault(name, null);
                    return prop?.FindPropertyRelative("m_Texture").objectReferenceValue as Texture2D;
                }

                if (Mathf.Approximately(floats.GetValueOrDefault("_UseShadow", 0.0f), 1.0f))
                {
                    var shadowBorder = floats.GetValueOrDefault("_ShadowBorder", 0.5f);
                    var shadowBlur = floats.GetValueOrDefault("_ShadowBlur", 0.1f);
                    var shadeShift = Mathf.Clamp01(shadowBorder - (shadowBlur * 0.5f)) * 2.0f - 1.0f;
                    var shadeToony = Mathf.Approximately(shadeShift, 1.0f)
                        ? 1.0f
                        : (2.0f - Mathf.Clamp01(shadowBorder + shadowBlur * 0.5f) * 2.0f) /
                          (1.0f - shadeShift);
                    if (LocalRetrieveTexture2D("_ShadowStrengthMask") ||
                        !Mathf.Approximately(floats.GetValueOrDefault("_ShadowMainStrength", 0.0f), 0.0f))
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
                        var shadowColor = colors.GetValueOrDefault("_ShadowColor", new Color(0.82f, 0.76f, 0.85f));
                        var shadowStrength = floats.GetValueOrDefault("_ShadowStrength", 1.0f);
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

                    var texture = LocalRetrieveTexture2D("_ShadowBorderTex");
                    var info = exporter.ExportTextureInfoMToon(material, texture, ColorSpace.Gamma, needsBlit: true);
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
                    Mathf.Approximately(floats.GetValueOrDefault("_UseRim", 0.0f), 1.0f))
                {
                    var rimColorTexture = LocalRetrieveTexture2D("_RimColorTex");
                    var rimBorder = floats.GetValueOrDefault("_RimBorder", 0.5f);
                    var rimBlur = floats.GetValueOrDefault("_RimBlur", 0.65f);
                    var rimFresnelPower = floats.GetValueOrDefault("_RimFresnelPower", 3.5f);
                    var rimFp = rimFresnelPower / Mathf.Max(0.001f, rimBlur);
                    var rimLift = Mathf.Pow(1.0f - rimBorder, rimFresnelPower) * (1.0f - rimBlur);
                    mtoon.RimLightingMixFactor = 1.0f;
                    mtoon.ParametricRimColorFactor =
                        colors.GetValueOrDefault("_RimColor", new Color(0.66f, 0.5f, 0.48f)).ToVector3();
                    mtoon.ParametricRimLiftFactor = rimLift;
                    mtoon.ParametricRimFresnelPowerFactor = rimFp;
                    if (Mathf.Approximately(floats.GetValueOrDefault("_RimBlendMode", 1.0f), 3.0f))
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
                    Mathf.Approximately(floats.GetValueOrDefault("_UseMatCap", 0.0f), 1.0f) &&
                    !Mathf.Approximately(floats.GetValueOrDefault("_MatCapBlendMode", 1.0f), 3.0f))
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
                            ColorSpace.Linear, needsBlit: true);
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
                    mtoon.OutlineWidthFactor = floats.GetValueOrDefault("_OutlineWidth", 0.08f) * 0.01f;
                    mtoon.OutlineColorFactor = colors.GetValueOrDefault("_OutlineColor", new Color(0.6f, 0.56f, 0.73f))
                        .ToVector3();
                    mtoon.OutlineWidthMultiplyTexture =
                        exporter.ExportTextureInfoMToon(material, outlineWidthTexture, ColorSpace.Gamma,
                            needsBlit: true);
                }

                var isCutout = shaderName.Contains("Cutout");
                var isTransparent = shaderName.Contains("Transparent") || shaderName.Contains("Overlay");
                mtoon.TransparentWithZWrite =
                    isCutout || (isTransparent &&
                                 !Mathf.Approximately(floats.GetValueOrDefault("_ZWrite", 1.0f), 0.0f));

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

            private static (int, int) FindTransformDepth(Transform? transform, Transform? root)
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

                return (upperDepth, lowerDepth);
            }

            private void ConvertSpringBone(VRCPhysBone pb, IImmutableList<VRCPhysBoneColliderBase> pbColliders,
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
                            ConvertSpringBoneInner(pb, name, transforms.Skip(1).ToImmutableList(), pbColliders,
                                colliderGroups, ref springs);
                            break;
                        }
                        case true:
                        {
                            var name = $"{pb.name}.{index}";
                            ConvertSpringBoneInner(pb, name, transforms, pbColliders, colliderGroups, ref springs);
                            break;
                        }
                        default:
                        {
                            var name = pb.name;
                            ConvertSpringBoneInner(pb, name, transforms, pbColliders, colliderGroups, ref springs);
                            break;
                        }
                    }

                    index++;
                }
            }

            private void ConvertSpringBoneInner(VRCPhysBone pb, string name, IImmutableList<Transform> transforms,
                IImmutableList<VRCPhysBoneColliderBase> pbColliders,
                IImmutableList<vrm.sb.ColliderGroup> colliderGroups, ref IList<vrm.sb.Spring> springs)
            {
                var rootTransform = pb.GetRootTransform();
                var joints = transforms.Select(transform =>
                    {
                        var nodeID = FindTransformNodeID(transform);
                        if (!nodeID.HasValue)
                        {
                            Debug.LogWarning($"Joint transform {transform} not found due to inactive");
                            return null;
                        }

                        var (upperDepth, lowerDepth) = FindTransformDepth(transform, rootTransform);
                        var totalDepth = upperDepth + lowerDepth;
                        var depthRatio = totalDepth != 0 ? upperDepth / (float)totalDepth : 0;
                        var evaluate = new Func<float, AnimationCurve, float>((value, curve) =>
                            curve is { length: > 0 }
                                ? curve.Evaluate(depthRatio) * value
                                : value);
                        var gravity = evaluate(pb.gravity, pb.gravityCurve);
                        var stiffness = evaluate(pb.stiffness, pb.stiffnessCurve);
                        var hitRadius = evaluate(pb.radius, pb.radiusCurve);
                        var pull = evaluate(pb.pull, pb.pullCurve);
                        var immobile = evaluate(pb.immobile, pb.immobileCurve) * 0.5f;
                        float stiffnessFactor, pullFactor;
                        if (pb.limitType != VRCPhysBoneBase.LimitType.None)
                        {
                            var maxAngleX = evaluate(pb.maxAngleX, pb.maxAngleXCurve);
                            stiffnessFactor = maxAngleX > 0.0f ? 1.0f / Mathf.Clamp01(maxAngleX / 180.0f) : 0.0f;
                            pullFactor = stiffnessFactor * 0.5f;
                        }
                        else
                        {
                            stiffnessFactor = 1.0f;
                            pullFactor = 1.0f;
                        }

                        return new vrm.sb.Joint
                        {
                            Node = nodeID.Value,
                            HitRadius = hitRadius,
                            Stiffness = immobile + stiffness * stiffnessFactor,
                            GravityPower = gravity,
                            GravityDir = -System.Numerics.Vector3.UnitY,
                            DragForce = Mathf.Clamp01(immobile + pull * pullFactor),
                        };
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
                    Center = null,
                    ColliderGroups = newColliderGroups.Count > 0 ? newColliderGroups.ToList() : null,
                    Joints = joints.Where(joint => joint != null).Select(joint => joint!).ToList(),
                };
                springs.Add(spring);
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

        private sealed class GltfMaterialExporter
        {
            public sealed class ExportOverrides
            {
                public Texture? MainTexture { get; set; }
                public gltf.material.AlphaMode? AlphaMode { get; set; }
                public int? CullMode { get; set; }
                public float? EmissiveStrength { get; set; }
                public bool EnableNormalMap { get; set; } = true;
            }

            // ReSharper disable once MemberCanBePrivate.Local
            public sealed class TextureItemMetadata
            {
                public TextureFormat TextureFormat { get; init; }
                public ColorSpace ColorSpace { get; init; }
                public int Width { get; init; }
                public int Height { get; init; }

                public string? KtxImageDataFormat
                {
                    get
                    {
                        if (Width > 0 && Height > 0 && Width % 4 == 0 && Height % 4 == 0)
                        {
                            return (TextureFormat, ColorSpace) switch
                            {
                                (TextureFormat.RGB24, ColorSpace.Gamma) => "R8G8B8_SRGB",
                                (TextureFormat.RGB24, ColorSpace.Linear) => "R8G8B8_UNORM",
                                (TextureFormat.ARGB32, ColorSpace.Gamma) => "R8G8B8A8_SRGB",
                                (TextureFormat.ARGB32, ColorSpace.Linear) => "R8G8B8A8_UNORM",
                                _ => null,
                            };
                        }

                        return null;
                    }
                }
            }

            public GltfMaterialExporter(gltf.Root root, gltf.exporter.Exporter exporter,
                ISet<string> extensionsUsed)
            {
                var metalGlossChannelSwapShader = Resources.Load("MetalGlossChannelSwap", typeof(Shader)) as Shader;
                var metalGlossOcclusionChannelSwapShader =
                    Resources.Load("MetalGlossOcclusionChannelSwap", typeof(Shader)) as Shader;
                var normalChannelShader = Resources.Load("NormalChannel", typeof(Shader)) as Shader;
                _root = root;
                _exporter = exporter;
                _textureIDs = new Dictionary<Texture, gltf.ObjectID>();
                TextureMetadata = new Dictionary<gltf.ObjectID, TextureItemMetadata>();
                _extensionsUsed = extensionsUsed;
                _metalGlossChannelSwapMaterial = new Material(metalGlossChannelSwapShader);
                _metalGlossOcclusionChannelSwapMaterial = new Material(metalGlossOcclusionChannelSwapShader);
                _normalChannelMaterial = new Material(normalChannelShader);
            }

            public gltf.material.Material Export(Material source, ExportOverrides overrides)
            {
                using var _ = new ScopedProfile($"GltfMaterialExporter.Export({source.name})");
                var alphaMode = overrides.AlphaMode;
                if (alphaMode == null)
                {
                    var renderType = source.GetTag("RenderType", true);
                    alphaMode = renderType switch
                    {
                        "Transparent" => gltf.material.AlphaMode.Blend,
                        "TransparentCutout" => gltf.material.AlphaMode.Mask,
                        _ => gltf.material.AlphaMode.Opaque,
                    };
                }

                var material = new gltf.material.Material
                {
                    Name = new gltf.UnicodeString(AssetPathUtils.TrimCloneSuffix(source.name)),
                    PbrMetallicRoughness = new gltf.material.PbrMetallicRoughness(),
                    AlphaMode = alphaMode,
                };

                if (source.HasProperty(PropertyColor))
                {
                    material.PbrMetallicRoughness.BaseColorFactor =
                        source.GetColor(PropertyColor).ToVector4(ColorSpace.Gamma, ColorSpace.Linear);
                }

                if (overrides.MainTexture)
                {
                    material.PbrMetallicRoughness.BaseColorTexture =
                        ExportTextureInfo(source, overrides.MainTexture, ColorSpace.Gamma, blitMaterial: null,
                            needsBlit: false);
                }
                else if (source.HasProperty(PropertyMainTex))
                {
                    var texture = source.GetTexture(PropertyMainTex);
                    material.PbrMetallicRoughness.BaseColorTexture =
                        ExportTextureInfo(source, texture, ColorSpace.Gamma, blitMaterial: null, needsBlit: true);
                    DecorateTextureTransform(source, PropertyMainTex, material.PbrMetallicRoughness.BaseColorTexture);
                }

                if (overrides.EmissiveStrength.HasValue)
                {
                    var emissiveStrength = overrides.EmissiveStrength.Value;
                    if (emissiveStrength > 0.0f)
                    {
                        material.EmissiveFactor = System.Numerics.Vector3.Clamp(GetEmissionColor(source),
                            System.Numerics.Vector3.Zero, System.Numerics.Vector3.One);
                        if (emissiveStrength < 1.0f)
                        {
                            material.EmissiveFactor *= emissiveStrength;
                        }
                    }

                    ExportEmissionTexture(source, material);
                }
                else if (source.IsKeywordEnabled("_EMISSION"))
                {
                    var emissiveFactor = GetEmissionColor(source);
                    material.EmissiveFactor = System.Numerics.Vector3.Clamp(emissiveFactor,
                        System.Numerics.Vector3.Zero, System.Numerics.Vector3.One);
                    var emissiveStrength = Mathf.Max(emissiveFactor.X, emissiveFactor.Y, emissiveFactor.Z);
                    if (emissiveStrength > 1.0f)
                    {
                        AddEmissiveStrengthExtension(emissiveStrength, material);
                    }

                    ExportEmissionTexture(source, material);
                }

                if (overrides.EnableNormalMap && source.HasProperty(PropertyBumpMap))
                {
                    var texture = source.GetTexture(PropertyBumpMap);
                    if (texture)
                    {
                        var info = ExportTextureInfo(source, texture, ColorSpace.Linear, _normalChannelMaterial,
                            needsBlit: true);
                        material.NormalTexture = new gltf.material.NormalTextureInfo
                        {
                            Index = info!.Index,
                            TexCoord = info.TexCoord,
                        };
                        if (source.HasProperty(PropertyBumpScale))
                        {
                            material.NormalTexture.Scale = source.GetFloat(PropertyBumpScale);
                        }

                        material.NormalTexture.Extensions ??= new Dictionary<string, JToken>();
                        DecorateTextureTransform(source, PropertyBumpMap, material.NormalTexture.Extensions);
                    }
                }

                if (source.HasProperty(PropertyOcclusionMap))
                {
                    var texture = source.GetTexture(PropertyOcclusionMap);
                    if (texture)
                    {
                        var info = ExportTextureInfo(source, texture, ColorSpace.Linear,
                            _metalGlossOcclusionChannelSwapMaterial, needsBlit: true);
                        material.OcclusionTexture = new gltf.material.OcclusionTextureInfo
                        {
                            Index = info!.Index,
                            TexCoord = info.TexCoord,
                        };
                        if (source.HasProperty(PropertyOcclusionStrength))
                        {
                            material.OcclusionTexture.Strength =
                                Mathf.Clamp01(source.GetFloat(PropertyOcclusionStrength));
                        }

                        material.OcclusionTexture.Extensions ??= new Dictionary<string, JToken>();
                        DecorateTextureTransform(source, PropertyOcclusionMap, material.OcclusionTexture.Extensions);
                    }
                }

                if (source.HasProperty(PropertyMetallicGlossMap))
                {
                    var texture = source.GetTexture(PropertyMetallicGlossMap);
                    if (texture)
                    {
                        material.PbrMetallicRoughness.MetallicRoughnessTexture =
                            ExportTextureInfo(source, texture, ColorSpace.Linear, _metalGlossChannelSwapMaterial,
                                needsBlit: true);
                        material.PbrMetallicRoughness.MetallicFactor = 1.0f;
                        material.PbrMetallicRoughness.RoughnessFactor = 1.0f;
                        DecorateTextureTransform(source, PropertyMetallicGlossMap,
                            material.PbrMetallicRoughness.MetallicRoughnessTexture);
                    }
                }
                else
                {
                    if (source.HasProperty(PropertyMetallic))
                    {
                        material.PbrMetallicRoughness.MetallicFactor = Mathf.Clamp01(source.GetFloat(PropertyMetallic));
                    }

                    if (source.HasProperty(PropertyGlossiness))
                    {
                        material.PbrMetallicRoughness.RoughnessFactor =
                            Mathf.Clamp01(source.GetFloat(PropertyGlossiness));
                    }
                }

                if (material.AlphaMode == gltf.material.AlphaMode.Mask && source.HasProperty(PropertyCutoff))
                {
                    material.AlphaCutoff = Mathf.Max(source.GetFloat(PropertyCutoff), 0.0f);
                }

                if (overrides.CullMode != null)
                {
                    material.DoubleSided = overrides.CullMode == 0;
                }
                else if (source.HasProperty(PropertyCullMode))
                {
                    material.DoubleSided = source.GetInt(PropertyCullMode) == 0;
                }

                return material;
            }

            internal Texture? ResolveTexture(gltf.material.TextureInfo? info)
            {
                return info == null
                    ? null
                    : (from item in _textureIDs where item.Value.Equals(info.Index) select item.Key)
                    .FirstOrDefault();
            }

            internal gltf.material.TextureInfo? ExportTextureInfoMToon(Material material, Texture? texture,
                ColorSpace cs, bool needsBlit)
            {
                return ExportTextureInfoInner(material, texture, cs, blitMaterial: null, needsBlit, mtoon: true);
            }

            private gltf.material.TextureInfo? ExportTextureInfo(Material material, Texture? texture,
                ColorSpace cs, Material? blitMaterial, bool needsBlit)
            {
                return ExportTextureInfoInner(material, texture, cs, blitMaterial, needsBlit, mtoon: false);
            }

            private gltf.material.TextureInfo? ExportTextureInfoInner(Material material, Texture? texture,
                ColorSpace cs, Material? blitMaterial, bool needsBlit, bool mtoon)
            {
                if (!texture || texture is null)
                {
                    return null;
                }

                if (!_textureIDs.TryGetValue(texture, out var textureID))
                {
                    const TextureFormat textureFormat = TextureFormat.RGBA32;
                    var name = $"{AssetPathUtils.TrimCloneSuffix(material.name)}_{texture.name}_{textureFormat}";
                    if (mtoon)
                    {
                        name = $"VRM_MToon_{name}";
                    }

                    var textureUnit = ExportTextureUnit(texture, name, textureFormat, cs, blitMaterial, needsBlit);
                    textureID = _exporter.CreateSampledTexture(_root, textureUnit);
                    _textureIDs.Add(texture, textureID);
                    TextureMetadata.Add(textureID, new TextureItemMetadata
                    {
                        TextureFormat = textureFormat,
                        ColorSpace = cs,
                        Width = texture.width,
                        Height = texture.height,
                    });
                }

                var textureInfo = new gltf.material.TextureInfo
                {
                    Index = textureID,
                };
                return textureInfo;
            }

            private void DecorateTextureTransform(Material material, int propertyID,
                gltf.material.TextureInfo? info)
            {
                if (info == null)
                {
                    return;
                }

                info.Extensions ??= new Dictionary<string, JToken>();
                DecorateTextureTransform(material, propertyID, info.Extensions);
            }

            private static System.Numerics.Vector3 GetEmissionColor(Material source)
            {
                if (!source.HasProperty(PropertyEmissionColor))
                    return System.Numerics.Vector3.Zero;
                return source.GetColor(PropertyEmissionColor)
                    .ToVector3(ColorSpace.Gamma, ColorSpace.Linear);
            }

            private void ExportEmissionTexture(Material source, gltf.material.Material material)
            {
                if (!source.HasProperty(PropertyEmissionMap))
                {
                    return;
                }

                var texture = source.GetTexture(PropertyEmissionMap);
                material.EmissiveTexture =
                    ExportTextureInfo(source, texture, ColorSpace.Gamma, blitMaterial: null, needsBlit: true);
                DecorateTextureTransform(source, PropertyEmissionMap, material.EmissiveTexture);
            }

            private void AddEmissiveStrengthExtension(float emissiveStrength, gltf.material.Material material)
            {
                material.Extensions ??= new Dictionary<string, JToken>();
                material.Extensions.Add(gltf.extensions.KhrMaterialsEmissiveStrength.Name,
                    gltf.Document.SaveAsNode(
                        new gltf.extensions.KhrMaterialsEmissiveStrength
                        {
                            EmissiveStrength = emissiveStrength,
                        }));
                _extensionsUsed.Add(gltf.extensions.KhrMaterialsEmissiveStrength.Name);
            }

            private void DecorateTextureTransform(Material material, int propertyID,
                IDictionary<string, JToken> extensions)
            {
                gltf.extensions.KhrTextureTransform? transform = null;
                var offset = material.GetTextureOffset(propertyID);
                if (offset != Vector2.zero)
                {
                    transform ??= new gltf.extensions.KhrTextureTransform();
                    transform.Offset = offset.ToVector2WithCoordinateSpace();
                }

                var scale = material.GetTextureScale(PropertyMainTex);
                if (scale != Vector2.one)
                {
                    transform ??= new gltf.extensions.KhrTextureTransform();
                    transform.Scale = offset.ToVector2();
                }

                if (transform == null)
                {
                    return;
                }

                extensions.Add(gltf.extensions.KhrTextureTransform.Name,
                    gltf.Document.SaveAsNode(transform));
                _extensionsUsed.Add(gltf.extensions.KhrTextureTransform.Name);
            }

            private static readonly int PropertyCullMode = Shader.PropertyToID("_CullMode");
            private static readonly int PropertyColor = Shader.PropertyToID("_Color");
            private static readonly int PropertyMainTex = Shader.PropertyToID("_MainTex");
            private static readonly int PropertyEmissionColor = Shader.PropertyToID("_EmissionColor");
            private static readonly int PropertyEmissionMap = Shader.PropertyToID("_EmissionMap");
            private static readonly int PropertyBumpScale = Shader.PropertyToID("_BumpScale");
            private static readonly int PropertyBumpMap = Shader.PropertyToID("_BumpMap");
            private static readonly int PropertyMetallic = Shader.PropertyToID("_Metallic");
            private static readonly int PropertyGlossiness = Shader.PropertyToID("_Glossiness");
            private static readonly int PropertyMetallicGlossMap = Shader.PropertyToID("_MetallicGlossMap");
            private static readonly int PropertyOcclusionStrength = Shader.PropertyToID("_OcclusionStrength");
            private static readonly int PropertyOcclusionMap = Shader.PropertyToID("_OcclusionMap");
            private static readonly int PropertyCutoff = Shader.PropertyToID("_Cutoff");

            public IDictionary<gltf.ObjectID, TextureItemMetadata> TextureMetadata { get; }
            private readonly gltf.Root _root;
            private readonly gltf.exporter.Exporter _exporter;
            private readonly IDictionary<Texture, gltf.ObjectID> _textureIDs;
            private readonly ISet<string> _extensionsUsed;
            private readonly Material _metalGlossChannelSwapMaterial;
            private readonly Material _metalGlossOcclusionChannelSwapMaterial;
            private readonly Material _normalChannelMaterial;
        }

        private readonly GameObject _gameObject;
        private readonly IAssetSaver _assetSaver;
        private readonly gltf.exporter.Exporter _exporter;
        private readonly gltf.Root _root;
        private readonly IDictionary<Material, gltf.ObjectID> _materialIDs;
        private readonly IDictionary<Material, MToonTexture> _materialMToonTextures;
        private readonly IDictionary<Transform, gltf.ObjectID> _transformNodeIDs;
        private readonly ISet<string> _transformNodeNames;
        private readonly ISet<string> _extensionsUsed;
        private readonly IReadOnlyList<MaterialVariant> _materialVariants;
        private readonly GltfMaterialExporter _materialExporter;
    }
}
