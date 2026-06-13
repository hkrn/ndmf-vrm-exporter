// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
#if NVE_HAS_UNIVRM
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using nadena.dev.ndmf;
using nadena.dev.ndmf.preview;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    public sealed class VrmPreview : IRenderFilter
    {
        private static readonly TogglablePreviewNode EnableNode =
            TogglablePreviewNode.Create(() => "VRM Preview", $"{NdmfVrmExporter.PackageJson.Name}/VrmPreview", false);

        private static readonly int PropertyAlphaMode = Shader.PropertyToID("_AlphaMode");
        private static readonly int PropertyCutoff = Shader.PropertyToID("_Cutoff");
        private static readonly int PropertyDoubleSided = Shader.PropertyToID("_DoubleSided");
        private static readonly int PropertyColor = Shader.PropertyToID("_Color");
        private static readonly int PropertyMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int PropertyBumpMap = Shader.PropertyToID("_BumpMap");
        private static readonly int PropertyBumpScale = Shader.PropertyToID("_BumpScale");
        private static readonly int PropertyEmissionMap = Shader.PropertyToID("_EmissionMap");
        private static readonly int PropertyEmissionColor = Shader.PropertyToID("_EmissionColor");

        private sealed class Node : IRenderFilterNode
        {
            public Node(Dictionary<Material, Material> materials, IAssetSaver assetSaver)
            {
                SwappedMaterials = materials;
                AssetSaver = assetSaver;
            }

            public void Dispose()
            {
                AssetSaver.Dispose();
            }

            public void OnFrame(Renderer original, Renderer proxy)
            {
                var materials = proxy.sharedMaterials;
                var changed = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (!materials[i] || !SwappedMaterials.TryGetValue(materials[i], out var swapped))
                    {
                        continue;
                    }

                    materials[i] = swapped;
                    changed = true;
                }

                if (changed)
                {
                    proxy.sharedMaterials = materials;
                }
            }

            public RenderAspects WhatChanged => RenderAspects.Material;

            private Dictionary<Material, Material> SwappedMaterials { get; }
            private IAssetSaver AssetSaver { get; }
        }

        internal sealed class PreviewAssetSaver : IAssetSaver
        {
            internal sealed class PreviewAssetContainer : ScriptableObject
            {
                [NonSerialized] internal PreviewAssetSaver? Owner;
            }

            private readonly PreviewAssetContainer _container;
            private readonly HashSet<Object> _saved = new();
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                foreach (var asset in _saved.Where(asset => asset))
                {
                    Object.DestroyImmediate(asset);
                }

                _saved.Clear();
                if (_container)
                {
                    Object.DestroyImmediate(_container);
                }
            }

            public PreviewAssetSaver()
            {
                _container = ScriptableObject.CreateInstance<PreviewAssetContainer>();
                _container.name = nameof(PreviewAssetContainer);
                _container.hideFlags = HideFlags.HideAndDontSave;
                _container.Owner = this;
            }

            public void SaveAsset(Object? asset)
            {
                ThrowIfDisposed();
                if (!asset || EditorUtility.IsPersistent(asset))
                {
                    return;
                }

                asset!.hideFlags = HideFlags.HideAndDontSave;
                _saved.Add(asset);
            }

            public bool IsTemporaryAsset(Object? asset)
            {
                if (!asset || _saved.Contains(asset!))
                {
                    return true;
                }

                return !EditorUtility.IsPersistent(asset);
            }

            public Object CurrentContainer => _container;

            public IEnumerable<Object> GetPersistedAssets()
            {
                ThrowIfDisposed();
                return _saved.Where(asset => !asset);
            }

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(PreviewAssetSaver));
                }
            }
        }

        private class NdmfVrmExporterComponentComparer : IEqualityComparer<NdmfVrmExporterComponent>
        {
            public static readonly NdmfVrmExporterComponentComparer Instance = new();

            public bool Equals(NdmfVrmExporterComponent? x, NdmfVrmExporterComponent? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null) return false;
                if (y is null) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.name == y.name && x.authors.Equals(y.authors) && x.version == y.version &&
                       x.copyrightInformation == y.copyrightInformation &&
                       x.contactInformation == y.contactInformation && x.references.Equals(y.references) &&
                       x.enableContactInformationOnVRChatAutofill == y.enableContactInformationOnVRChatAutofill &&
                       x.licenseUrl == y.licenseUrl && x.thirdPartyLicenses == y.thirdPartyLicenses &&
                       x.otherLicenseUrl == y.otherLicenseUrl && x.avatarPermission == y.avatarPermission &&
                       x.commercialUsage == y.commercialUsage && x.creditNotation == y.creditNotation &&
                       x.modification == y.modification && x.metadataAllowFoldout == y.metadataAllowFoldout &&
                       x.allowExcessivelyViolentUsage == y.allowExcessivelyViolentUsage &&
                       x.allowExcessivelySexualUsage == y.allowExcessivelySexualUsage &&
                       x.allowPoliticalOrReligiousUsage == y.allowPoliticalOrReligiousUsage &&
                       x.allowAntisocialOrHateUsage == y.allowAntisocialOrHateUsage &&
                       x.allowRedistribution == y.allowRedistribution;
            }

            public int GetHashCode(NdmfVrmExporterComponent obj)
            {
                var hashCode = new HashCode();
                hashCode.Add(obj.name);
                hashCode.Add(obj.authors);
                hashCode.Add(obj.version);
                hashCode.Add(obj.copyrightInformation);
                hashCode.Add(obj.contactInformation);
                hashCode.Add(obj.references);
                hashCode.Add(obj.enableContactInformationOnVRChatAutofill);
                hashCode.Add(obj.licenseUrl);
                hashCode.Add(obj.thirdPartyLicenses);
                hashCode.Add(obj.otherLicenseUrl);
                hashCode.Add((int)obj.avatarPermission);
                hashCode.Add((int)obj.commercialUsage);
                hashCode.Add((int)obj.creditNotation);
                hashCode.Add((int)obj.modification);
                hashCode.Add(obj.metadataAllowFoldout);
                hashCode.Add((int)obj.allowExcessivelyViolentUsage);
                hashCode.Add((int)obj.allowExcessivelySexualUsage);
                hashCode.Add((int)obj.allowPoliticalOrReligiousUsage);
                hashCode.Add((int)obj.allowAntisocialOrHateUsage);
                hashCode.Add((int)obj.allowRedistribution);
                return hashCode.ToHashCode();
            }
        }

        public IEnumerable<TogglablePreviewNode> GetPreviewControlNodes()
        {
            yield return EnableNode;
        }

        public bool IsEnabled(ComputeContext context)
        {
            return context.Observe(EnableNode.IsEnabled);
        }

        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext context)
        {
            var groups = ImmutableList.CreateBuilder<RenderGroup>();
            foreach (var avatar in context.GetAvatarRoots())
            {
                if (!avatar.TryGetComponent<NdmfVrmExporterComponent>(out var component))
                {
                    continue;
                }

                context.Observe(component, c => (c.enableMToonMatCap, c.enableMToonOutline, c.enableMToonRimLight));

                foreach (var renderer in context.GetComponentsInChildren<Renderer>(avatar, true)
                             .Where(renderer => renderer is SkinnedMeshRenderer or MeshRenderer))
                {
                    groups.Add(RenderGroup.For(renderer)
                        .WithData(component, NdmfVrmExporterComponentComparer.Instance));
                }
            }

            return groups.ToImmutableList();
        }

        public Task<IRenderFilterNode> Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs,
            ComputeContext context)
        {
            var component = group.GetData<NdmfVrmExporterComponent>();
            var swappingMaterials = new Dictionary<Material, Material>();
            var assetSaver = new PreviewAssetSaver();
            var root = new gltf.Root
            {
                Accessors = new List<gltf.accessor.Accessor>(),
                Asset = new gltf.asset.Asset(),
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
            var exporter = new gltf.exporter.Exporter();
            var extensionsUsed = new HashSet<string>();
            var materialExporter = new GltfMaterialExporter(root, exporter, extensionsUsed);
            foreach (var (_, proxy) in proxyPairs)
            {
                var materials = proxy.sharedMaterials;
                var numMaterials = materials.Length;
                for (var i = 0; i < numMaterials; i++)
                {
                    var originalMaterial = materials[i];
                    if (swappingMaterials.ContainsKey(originalMaterial))
                    {
                        continue;
                    }

                    Material swappedMaterial;
                    var isLiltoonPbr = GltfMaterialExporter.IsLilToonPbr(originalMaterial);
                    if (GltfMaterialExporter.IsLilToon(originalMaterial) && !isLiltoonPbr)
                    {
                        swappedMaterial =
                            CreateMToonMaterial(assetSaver, component, originalMaterial, materialExporter);
                    }
#if NVE_HAS_UNITY_GLTF
                    else if (isLiltoonPbr)
                    {
                        swappedMaterial = CreateGltfPbrMaterial(assetSaver, component, originalMaterial);
                    }
#endif // NVE_HAS_UNITY_GLTF
                    else
                    {
                        swappedMaterial = CreateStandardMaterial(originalMaterial);
                    }

                    swappingMaterials[originalMaterial] = swappedMaterial;
                    ObjectRegistry.RegisterReplacedObject(originalMaterial, swappedMaterial);
                }
            }

            return Task.FromResult<IRenderFilterNode>(new Node(swappingMaterials, assetSaver));
        }

        private static Material CreateMToonMaterial(IAssetSaver assetSaver, NdmfVrmExporterComponent component,
            Material originalMaterial, GltfMaterialExporter materialExporter)
        {
            var swappedMaterial = new Material(Shader.Find("VRM10/MToon10"));
            var mainTexture = originalMaterial.HasProperty(PropertyMainTex)
                ? originalMaterial.GetTexture(PropertyMainTex)
                : null;
            var mtoonTexture = new NdmfVrmExporter.MToonTexture
            {
                MainTexture = mainTexture,
                MainTextureInfo =
                    materialExporter.ExportTextureInfoMToon(originalMaterial, mainTexture, ColorSpace.Gamma, false),
            };
            var mtoon = VrmRootExporter.ExportMToon(component, assetSaver, originalMaterial, mtoonTexture,
                materialExporter);
            var mtoonContext = new VRM10.MToon10.MToon10Context(swappedMaterial)
            {
                AlphaMode = (VRM10.MToon10.MToon10AlphaMode)originalMaterial.GetIntOrDefault(PropertyAlphaMode, 0),
                AlphaCutoff = originalMaterial.GetFloatOrDefault(PropertyCutoff, 0.0f),
                DoubleSidedMode =
                    (VRM10.MToon10.MToon10DoubleSidedMode)originalMaterial.GetIntOrDefault(PropertyDoubleSided, 0),
                BaseColorFactorSrgb = originalMaterial.GetColorOrDefault(PropertyColor, Color.white),
                BaseColorTexture = mainTexture,
                NormalTexture = originalMaterial.HasProperty(PropertyBumpMap)
                    ? originalMaterial.GetTexture(PropertyBumpMap)
                    : null,
                NormalTextureScale = originalMaterial.GetFloatOrDefault(PropertyBumpScale, 1.0f),
                EmissiveTexture = originalMaterial.HasProperty(PropertyEmissionMap)
                    ? originalMaterial.GetTexture(PropertyEmissionMap)
                    : null,
                EmissiveFactorLinear = originalMaterial.GetColorOrDefault(PropertyEmissionColor, Color.black),
                TransparentWithZWriteMode = mtoon.TransparentWithZWrite
                    ? VRM10.MToon10.MToon10TransparentWithZWriteMode.On
                    : VRM10.MToon10.MToon10TransparentWithZWriteMode.Off,
                ShadeColorFactorSrgb = ToColor(mtoon.ShadeColorFactor),
                ShadeColorTexture = materialExporter.ResolveTexture(mtoon.ShadeMultiplyTexture),
                ShadingShiftFactor = mtoon.ShadingShiftFactor,
                ShadingShiftTexture = materialExporter.ResolveTexture(mtoon.ShadingShiftTexture),
                ShadingToonyFactor = mtoon.ShadingToonyFactor,
                GiEqualizationFactor = mtoon.GIEqualizationFactor,
                MatcapColorFactorSrgb = ToColor(mtoon.MatcapFactor),
                MatcapTexture = materialExporter.ResolveTexture(mtoon.MatcapTexture),
                ParametricRimColorFactorSrgb = ToColor(mtoon.ParametricRimColorFactor),
                RimMultiplyTexture = materialExporter.ResolveTexture(mtoon.RimMultiplyTexture),
                RimLightingMixFactor = mtoon.RimLightingMixFactor,
                ParametricRimFresnelPowerFactor = mtoon.ParametricRimFresnelPowerFactor,
                ParametricRimLiftFactor = mtoon.ParametricRimLiftFactor,
                OutlineWidthMode = mtoon.OutlineWidthMode switch
                {
                    vrm.mtoon.OutlineWidthMode.None => VRM10.MToon10.MToon10OutlineMode.None,
                    vrm.mtoon.OutlineWidthMode.ScreenCoordinates => VRM10.MToon10.MToon10OutlineMode.Screen,
                    vrm.mtoon.OutlineWidthMode.WorldCoordinates => VRM10.MToon10.MToon10OutlineMode.World,
                    _ => throw new ArgumentOutOfRangeException()
                },
                OutlineWidthFactor = mtoon.OutlineWidthFactor,
                OutlineWidthMultiplyTexture = materialExporter.ResolveTexture(mtoon.OutlineWidthMultiplyTexture),
                OutlineColorFactorSrgb = ToColor(mtoon.OutlineColorFactor),
                OutlineLightingMixFactor = mtoon.OutlineLightingMixFactor,
                UvAnimationMaskTexture = materialExporter.ResolveTexture(mtoon.UVAnimationMaskTexture),
                UvAnimationRotationSpeedFactor = mtoon.UVAnimationRotationSpeedFactor,
                UvAnimationScrollXSpeedFactor = mtoon.UVAnimationScrollXSpeedFactor,
                UvAnimationScrollYSpeedFactor = mtoon.UVAnimationScrollYSpeedFactor,
            };
            mtoonContext.Validate();
            return swappedMaterial;
        }

#if NVE_HAS_UNITY_GLTF
        private static Material CreateGltfPbrMaterial(IAssetSaver assetSaver, NdmfVrmExporterComponent component,
            Material originalMaterial)
        {
            var shaderName = originalMaterial.shader.name;
            var enableBakingAlphaMaskTexture = component.enableBakingAlphaMaskTexture;
            var isShaderLiltoon = true;
            var overrides = GltfMaterialExporter.CreateExportOverrides(assetSaver, originalMaterial, shaderName,
                enableBakingAlphaMaskTexture, ref isShaderLiltoon);
            var normalTexture = originalMaterial.HasProperty(PropertyBumpMap)
                ? originalMaterial.GetTexture(PropertyBumpMap)
                : null;
            var normalTextureScale = originalMaterial.GetFloatOrDefault(PropertyBumpScale, 1.0f);
            var emissiveTexture = originalMaterial.HasProperty(PropertyEmissionMap)
                ? originalMaterial.GetTexture(PropertyEmissionMap)
                : null;
            var emissiveFactor = originalMaterial.GetColorOrDefault(PropertyEmissionColor, Color.black);
            var mainTexture = overrides.MainTexture;
            if (!mainTexture)
            {
                mainTexture = originalMaterial.GetTexture(PropertyMainTex);
            }
            var mapper = new UnityGLTF.PBRGraphMap
            {
                AlphaMode = overrides.AlphaMode switch
                {
                    gltf.material.AlphaMode.Blend => GLTF.Schema.AlphaMode.BLEND,
                    gltf.material.AlphaMode.Mask => GLTF.Schema.AlphaMode.MASK,
                    gltf.material.AlphaMode.Opaque => GLTF.Schema.AlphaMode.OPAQUE,
                    _ => throw new ArgumentOutOfRangeException()
                },
                AlphaCutoff = overrides.AlphaCutoff.GetValueOrDefault(0.5f),
                BaseColorTexture = mainTexture,
                BaseColorFactor = originalMaterial.GetColorOrDefault(PropertyColor, Color.white),
                NormalTexture = normalTexture,
                NormalTexScale = normalTextureScale,
                EmissiveTexture = emissiveTexture,
                EmissiveFactor = emissiveFactor,
                MetallicRoughnessTexture = overrides.MetallicRoughnessTexture,
                MetallicFactor = overrides.MetallicFactor.GetValueOrDefault(0),
                RoughnessFactor = overrides.RoughnessFactor.GetValueOrDefault(0),
                SpecularFactor = overrides.SpecularFactor.GetValueOrDefault(0),
                SpecularTexture = overrides.SpecularTexture,
                SpecularColorFactor = overrides.SpecularColorFactor.GetValueOrDefault(Color.black),
                SpecularColorTexture = overrides.SpecularColorTexture,
                TransmissionFactor = overrides.TransmissionFactor.GetValueOrDefault(0),
                TransmissionTexture = overrides.TransmissionTexture,
                ThicknessFactor = overrides.ThicknessFactor.GetValueOrDefault(0),
                AttenuationColor = overrides.AttenuationColor.GetValueOrDefault(Color.black),
                AttenuationDistance = overrides.AttenuationDistance.GetValueOrDefault(0),
                IOR = overrides.Ior.GetValueOrDefault(1.5f),
            };
            if (overrides.SpecularFactor.HasValue)
            {
                mapper.Material.EnableKeyword("_SPECULAR");
                mapper.Material.EnableKeyword("_SPECULAR_ON");
            }
            if (overrides.TransmissionFactor.HasValue)
            {
                mapper.Material.EnableKeyword("_VOLUME_TRANSMISSION_ON");
                mapper.Material.SetFloat($"_VOLUME_TRANSMISSION", 1);

            }
            if (overrides.ThicknessFactor.HasValue)
            {
                mapper.Material.SetFloat($"_VOLUME_ON", 1);
            }

            UnityGLTF.GLTFMaterialHelper.ValidateMaterialKeywords(mapper.Material);
            return Object.Instantiate(mapper.Material);
        }
#endif // NVE_HAS_UNITY_GLTF

        private static Material CreateStandardMaterial(Material originalMaterial)
        {
            var swappedMaterial = new Material(Shader.Find("Standard"));
            swappedMaterial.CopyPropertiesFromMaterial(originalMaterial);
            return swappedMaterial;
        }

        private static Color ToColor(System.Numerics.Vector3 value)
        {
            return new Color(value.X, value.Y, value.Z);
        }
    }
}

#endif // NVE_HAS_UNIVRM
