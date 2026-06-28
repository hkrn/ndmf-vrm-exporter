// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using nadena.dev.ndmf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.Animations;

#if NVE_HAS_VRCHAT_AVATAR_SDK
using VRC.Dynamics;
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

using Debug = UnityEngine.Debug;
using Material = UnityEngine.Material;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Mesh = UnityEngine.Mesh;
using Quaternion = System.Numerics.Quaternion;
using Texture = UnityEngine.Texture;
using Vector4 = System.Numerics.Vector4;

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    internal sealed class NdmfVrmExporter : IDisposable
    {
        private static readonly string VrmcVrm = "VRMC_vrm";
        private static readonly string VrmcSpringBone = "VRMC_springBone";
        private static readonly string VrmcNodeConstraint = "VRMC_node_constraint";
        private static readonly string VrmcMaterialsMtoon = "VRMC_materials_mtoon";

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

        internal sealed class ScopedProfile : IDisposable
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

        public sealed class MToonTexture
        {
            public Texture? MainTexture { get; set; }
            public gltf.material.TextureInfo? MainTextureInfo { get; set; }
        }

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
                if (!process.WaitForExit(30000) || process.ExitCode != 0)
                {
                    return null;
                }
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
            var rotation = Quaternion.Identity;
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

        internal static void ReplaceFile(string filePath, byte[] data)
        {
            bool IsFileAlreadyExists(IOException ex)
            {
                return (uint)ex.HResult is 0x80070050 or 0x800700B7;
            }

            var randomFilename = Path.GetRandomFileName();
            var tempFilePath = $"{filePath}.{randomFilename}.tmp";
            var backupFilePath = $"{filePath}.{randomFilename}.bak";
            try
            {
                try
                {
                    using var _ = File.Open(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                }
                catch (IOException ex) when (IsFileAlreadyExists(ex))
                {
                    /* ignore only when file is already exists */
                }

                File.WriteAllBytes(tempFilePath, data);
                File.Replace(tempFilePath, filePath, backupFilePath);
            }
            finally
            {
                if (File.Exists(backupFilePath))
                {
                    File.Delete(backupFilePath);
                }

                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
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
                        GltfMaterialExporter.ExportTextureUnit(thumbnail, name, TextureFormat.RGB24, ColorSpace.Gamma,
                            null, true);
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

            {
                var detector = new CircularDependentNodeConstraintDetector(_transformNodeIDs);
                detector.Visit();
                foreach (var transform in detector.FoundAllTransforms)
                {
                    if (transform.TryGetComponent<AimConstraint>(out var aimConstraint))
                    {
                        ErrorReport.ReportError(Translator.Instance, ErrorSeverity.NonFatal,
                            "component.runtime.error.constraint.circular", transform.gameObject);
                        aimConstraint.SetSources(new List<ConstraintSource>());
                    }
                    else if (transform.TryGetComponent<RotationConstraint>(out var rotationConstraint))
                    {
                        ErrorReport.ReportError(Translator.Instance, ErrorSeverity.NonFatal,
                            "component.runtime.error.constraint.circular", transform.gameObject);
                        rotationConstraint.SetSources(new List<ConstraintSource>());
                    }
                }
            }
#if NVE_HAS_VRCHAT_AVATAR_SDK
            {
                var detector = new VrcCircularDependentNodeConstraintDetector(_transformNodeIDs);
                detector.Visit();
                foreach (var transform in detector.FoundAllTransforms)
                {
                    if (!transform.TryGetComponent<VRCConstraintBase>(out var vcb))
                    {
                        continue;
                    }

                    ErrorReport.ReportError(Translator.Instance, ErrorSeverity.NonFatal,
                        "component.runtime.error.constraint.circular", transform.gameObject);
                    vcb.Sources.Clear();
                }
            }
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

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

            public IList<Matrix4x4> InverseBindMatrices { get; } =
                new List<Matrix4x4>();

            private readonly Dictionary<Transform, int> _transformMap = new();
            private readonly UnityEngine.Matrix4x4 _inverseParentTransformMatrix;
            private readonly Transform[] _boneTransforms;
            private readonly Vector3[] _originPositions;
            private readonly Vector3[] _originNormals;
            private readonly Vector3[] _deltaPositions;
            private readonly Vector3[] _deltaNormals;
            private readonly UnityEngine.Matrix4x4[] _boneMatrices;
            private readonly UnityEngine.Matrix4x4[] _bindPoseMatrices;

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
                _boneMatrices = bones.Select(bone => bone ? bone.localToWorldMatrix : UnityEngine.Matrix4x4.zero)
                    .ToArray();
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

            private UnityEngine.Matrix4x4 GetSourceMatrix(int sourceBoneIndex)
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
            Vector4[] weights;
            if (smr)
            {
                var resolver = new BoneResolver(parentTransform, smr!);
                var boneWeights = smr!.sharedMesh.boneWeights;
                var index = 0;
                jointUnits = new gltf.exporter.JointUnit[boneWeights.Length];
                weights = new Vector4[boneWeights.Length];
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
                    weights[index] = new Vector4(w0, w1, w2, w3);
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
                weights = Array.Empty<Vector4>();
            }

            var meshUnit = new gltf.exporter.MeshUnit
            {
                Name = new gltf.UnicodeString(AssetPathUtils.TrimCloneSuffix(mesh.name)),
                Positions = positions,
                Normals = normals,
                Colors = component.enableVertexColorOutput
                    ? mesh.colors32.Select(item => ((Color)item).ToVector4(ColorSpace.Gamma, ColorSpace.Linear))
                        .ToArray()
                    : Array.Empty<Vector4>(),
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
                            meshUnit.Colors[index] = Vector4.One;
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
            var isMToon = shaderName.StartsWith("VRM10/", StringComparison.Ordinal) &&
                          shaderName.EndsWith("/MToon10", StringComparison.Ordinal);
            if (_materialIDs.TryGetValue(subMeshMaterial, out var materialID))
            {
                return materialID;
            }

            materialID = new gltf.ObjectID((uint)_root.Materials!.Count);
            var config = GltfMaterialExporter.CreateExportOverrides(_assetSaver, subMeshMaterial, shaderName,
                enableBakingAlphaMaskTexture, ref isShaderLiltoon);
            var material = _materialExporter.Export(subMeshMaterial, config);
            if (isMToon)
            {
                WrapMToonMaterial(subMeshMaterial, material);
            }

            _root.Materials!.Add(material);
            _materialIDs.Add(subMeshMaterial, materialID);

#if NVE_HAS_LILTOON
            if (!isShaderLiltoon)
            {
                return materialID;
            }

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

        private void WrapMToonMaterial(Material subMeshMaterial, gltf.material.Material material)
        {
            // Parameters from `vrmc_materials_mtoon.shader`
            // https://github.com/vrm-c/UniVRM/blob/9b129cf788a00232b62cc7227e732a39158ea883/Packages/VRM10/MToon10/Shaders/vrmc_materials_mtoon.shader
            var shadeMultiplyTextureInner = subMeshMaterial.GetTexture(Shader.PropertyToID("_ShadeTex"));
            var shadeMultiplyTexture = shadeMultiplyTextureInner
                ? _materialExporter.ExportTextureInfoMToon(subMeshMaterial, shadeMultiplyTextureInner,
                    ColorSpace.Gamma, false)
                : null;
            var shadingShiftTextureInner = subMeshMaterial.GetTexture(Shader.PropertyToID("_ShadingShiftTex"));
            vrm.mtoon.ShadingShiftTexture? shadingShiftTexture = null;
            if (shadingShiftTextureInner)
            {
                var info = _materialExporter.ExportTextureInfoMToon(subMeshMaterial, shadingShiftTextureInner,
                    ColorSpace.Linear, false)!;
                shadingShiftTexture = new vrm.mtoon.ShadingShiftTexture
                {
                    Index = info.Index,
                    Scale = 1.0f,
                    TexCoord = info.TexCoord,
                };
            }

            var matcapTextureInner = subMeshMaterial.GetTexture(Shader.PropertyToID("_MatcapTex"));
            var matcapTexture = matcapTextureInner
                ? _materialExporter.ExportTextureInfoMToon(subMeshMaterial, matcapTextureInner, ColorSpace.Gamma,
                    false)
                : null;
            var rimMultiplyTextureInner = subMeshMaterial.GetTexture(Shader.PropertyToID("_RimTex"));
            var rimMultiplyTexture = rimMultiplyTextureInner
                ? _materialExporter.ExportTextureInfoMToon(subMeshMaterial, rimMultiplyTextureInner,
                    ColorSpace.Gamma, false)
                : null;
            var outlineWidthMultiplyTextureInner =
                subMeshMaterial.GetTexture(Shader.PropertyToID("_OutlineWidthTex"));
            var outlineWidthMultiplyTexture = outlineWidthMultiplyTextureInner
                ? _materialExporter.ExportTextureInfoMToon(subMeshMaterial, outlineWidthMultiplyTextureInner,
                    ColorSpace.Linear, false)
                : null;
            var uvAnimationMaskTextureInner = subMeshMaterial.GetTexture(Shader.PropertyToID("_UvAnimMaskTex"));
            var uvAnimationMaskTexture = uvAnimationMaskTextureInner
                ? _materialExporter.ExportTextureInfoMToon(subMeshMaterial, uvAnimationMaskTextureInner,
                    ColorSpace.Linear, false)
                : null;
            var mtoon = new vrm.mtoon.MToon
            {
                TransparentWithZWrite = subMeshMaterial.GetInt(Shader.PropertyToID("_TransparentWithZWrite")) != 0,
                RenderQueueOffsetNumber = subMeshMaterial.GetInt(Shader.PropertyToID("_RenderQueueOffset")),
                ShadeColorFactor = subMeshMaterial.GetColor(Shader.PropertyToID("_ShadeColor")).ToVector3(),
                ShadeMultiplyTexture = shadeMultiplyTexture,
                ShadingShiftFactor = subMeshMaterial.GetFloat(Shader.PropertyToID("_ShadingShiftFactor")),
                ShadingShiftTexture = shadingShiftTexture,
                ShadingToonyFactor = subMeshMaterial.GetFloat(Shader.PropertyToID("_ShadingToonyFactor")),
                GIEqualizationFactor = subMeshMaterial.GetFloat(Shader.PropertyToID("_GiEqualization")),
                MatcapFactor = subMeshMaterial.GetColor(Shader.PropertyToID("_MatcapColor")).ToVector3(),
                MatcapTexture = matcapTexture,
                ParametricRimColorFactor = subMeshMaterial.GetColor(Shader.PropertyToID("_RimColor")).ToVector3(),
                RimMultiplyTexture = rimMultiplyTexture,
                RimLightingMixFactor = subMeshMaterial.GetFloat(Shader.PropertyToID("_RimLightingMix")),
                ParametricRimFresnelPowerFactor = subMeshMaterial.GetFloat(Shader.PropertyToID("_RimFresnelPower")),
                ParametricRimLiftFactor = subMeshMaterial.GetFloat(Shader.PropertyToID("_RimLift")),
                OutlineWidthFactor = subMeshMaterial.GetFloat(Shader.PropertyToID("_OutlineWidth")),
                OutlineWidthMode =
                    (vrm.mtoon.OutlineWidthMode)subMeshMaterial.GetInt(Shader.PropertyToID("_OutlineWidthMode")),
                OutlineColorFactor = subMeshMaterial.GetColor(Shader.PropertyToID("_OutlineColor")).ToVector3(),
                OutlineWidthMultiplyTexture = outlineWidthMultiplyTexture,
                UVAnimationMaskTexture = uvAnimationMaskTexture,
                UVAnimationScrollXSpeedFactor =
                    subMeshMaterial.GetFloat(Shader.PropertyToID("_UvAnimScrollXSpeed")),
                UVAnimationScrollYSpeedFactor =
                    subMeshMaterial.GetFloat(Shader.PropertyToID("_UvAnimScrollYSpeed")),
                UVAnimationRotationSpeedFactor =
                    subMeshMaterial.GetFloat(Shader.PropertyToID("_UvAnimRotationSpeed")),
            };
            material.Extensions ??= new Dictionary<string, JToken>();
            material.Extensions.Add(gltf.extensions.KhrMaterialsUnlit.Name, new JObject());
            material.Extensions.Add(VrmcMaterialsMtoon, vrm.Document.SaveAsNode(mtoon));
            _extensionsUsed.Add(gltf.extensions.KhrMaterialsUnlit.Name);
            _extensionsUsed.Add(VrmcMaterialsMtoon);
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
