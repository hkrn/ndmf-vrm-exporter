// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    internal sealed class GltfMaterialExporter
    {
        internal sealed class ExportOverrides
        {
            public Texture? MainTexture { get; set; }
            public Texture? MetallicRoughnessTexture { get; set; } // from lilToon PBR
            public Texture? SpecularTexture { get; set; } // from lilToon PBR
            public Texture? SpecularColorTexture { get; set; } // from lilToon PBR
            public Texture? TransmissionTexture { get; set; } // from lilToon PBR
            public Color? SpecularColorFactor { get; set; } // from lilToon PBR
            public Color? DiffuseTransmissionColorFactor { get; set; } // from lilToon PBR
            public Color? AttenuationColor { get; set; } // from lilToon PBR
            public gltf.material.AlphaMode? AlphaMode { get; set; }
            public float? AlphaCutoff { get; set; }
            public int? CullMode { get; set; }
            public float? EmissiveStrength { get; set; }
            public float? MetallicFactor { get; set; } // from lilToon PBR
            public float? RoughnessFactor { get; set; } // from lilToon PBR
            public float? SpecularFactor { get; set; } // from lilToon PBR
            public float? DiffuseTransmissionFactor { get; set; } // from lilToon PBR
            public float? TransmissionFactor { get; set; } // from lilToon PBR
            public float? ThicknessFactor { get; set; } // from lilToon PBR
            public float? AttenuationDistance { get; set; } // from lilToon PBR
            public float? Ior { get; set; } // from lilToon PBR
            public bool EnableNormalMap { get; set; } = true;
        }

        // ReSharper disable once MemberCanBePrivate.Local
        internal sealed class TextureItemMetadata
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

        private const int CullModeNone = 0;
        private const int CullModeFront = 1;
        private const int ReflectionBlendModeAdd = 1;
        private const int SpecularToonReal = 0;
        private const int TransparentModeRefraction = 3;

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
            using var _ = new NdmfVrmExporter.ScopedProfile($"GltfMaterialExporter.Export({source.name})");
            var alphaMode = overrides.AlphaMode;
            var alphaCutoff = overrides.AlphaCutoff;
            if (alphaMode == null)
            {
                var renderType = source.GetTag("RenderType", true);
                alphaMode = renderType switch
                {
                    "Transparent" => gltf.material.AlphaMode.Blend,
                    "TransparentCutout" => gltf.material.AlphaMode.Mask,
                    _ => gltf.material.AlphaMode.Opaque,
                };
                if (alphaMode == gltf.material.AlphaMode.Mask)
                {
                    alphaCutoff = source.GetFloatOrDefault(PropertyCutoff, 0.0f);
                }
            }

            var material = new gltf.material.Material
            {
                Name = new gltf.UnicodeString(AssetPathUtils.TrimCloneSuffix(source.name)),
                PbrMetallicRoughness = new gltf.material.PbrMetallicRoughness(),
                AlphaMode = alphaMode,
                AlphaCutoff = alphaCutoff
            };

            material.PbrMetallicRoughness.BaseColorFactor =
                source.GetColorOrDefault(PropertyColor, Color.white).ToVector4(ColorSpace.Gamma, ColorSpace.Linear);

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
                        Scale = source.GetFloatOrDefault(PropertyBumpScale, 1.0f),
                    };

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
                        Strength = Mathf.Clamp01(source.GetFloatOrDefault(PropertyOcclusionStrength, 0.0f)),
                    };

                    material.OcclusionTexture.Extensions ??= new Dictionary<string, JToken>();
                    DecorateTextureTransform(source, PropertyOcclusionMap, material.OcclusionTexture.Extensions);
                }
            }

            if (overrides.MetallicRoughnessTexture)
            {
                material.PbrMetallicRoughness.MetallicRoughnessTexture = ExportTextureInfo(source,
                    overrides.MetallicRoughnessTexture,
                    ColorSpace.Linear, _metalGlossChannelSwapMaterial, needsBlit: false);
                material.PbrMetallicRoughness.RoughnessFactor = overrides.RoughnessFactor!.Value;
                material.PbrMetallicRoughness.MetallicFactor = overrides.MetallicFactor!.Value;
            }
            else if (source.HasProperty(PropertyMetallicGlossMap))
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
                material.PbrMetallicRoughness.MetallicFactor = overrides.MetallicFactor.GetValueOrDefault(
                    Mathf.Clamp01(source.GetFloatOrDefault(PropertyMetallic, 0.0f)));
                material.PbrMetallicRoughness.RoughnessFactor = overrides.RoughnessFactor.GetValueOrDefault(
                    Mathf.Clamp01(source.GetFloatOrDefault(PropertyGlossiness, 0.0f)));
            }

            if (overrides.SpecularFactor.HasValue)
            {
                var colorFactor = overrides.SpecularColorFactor!.Value;
                gltf.material.TextureInfo? specularTexture = null;
                if (overrides.SpecularTexture)
                {
                    specularTexture = ExportTextureInfo(source, overrides.SpecularTexture, ColorSpace.Linear,
                        _metalGlossChannelSwapMaterial, needsBlit: true);
                }

                gltf.material.TextureInfo? specularColorTexture = null;
                if (overrides.SpecularColorTexture)
                {
                    specularColorTexture = ExportTextureInfo(source, overrides.SpecularColorTexture, ColorSpace.Gamma,
                        _metalGlossChannelSwapMaterial, needsBlit: true);
                }

                var specular = new gltf.extensions.KhrMaterialsSpecular
                {
                    SpecularFactor = overrides.SpecularFactor.Value,
                    SpecularTexture = specularTexture,
                    SpecularColorFactor = new System.Numerics.Vector3(colorFactor.r, colorFactor.g, colorFactor.b),
                    SpecularColorTexture = specularColorTexture,
                };
                material.Extensions ??= new Dictionary<string, JToken>();
                material.Extensions.Add(gltf.extensions.KhrMaterialsSpecular.Name, gltf.Document.SaveAsNode(specular));
                _extensionsUsed.Add(gltf.extensions.KhrMaterialsSpecular.Name);
            }

            if (overrides.DiffuseTransmissionFactor.HasValue)
            {
                var transmissionColorFactor = overrides.DiffuseTransmissionColorFactor!.Value;
                var specular = new gltf.extensions.KhrMaterialsDiffuseTransmission
                {
                    DiffuseTransmissionFactor = overrides.DiffuseTransmissionFactor.Value,
                    DiffuseTransmissionColorFactor = new System.Numerics.Vector3(transmissionColorFactor.r,
                        transmissionColorFactor.g, transmissionColorFactor.b),
                };
                material.Extensions ??= new Dictionary<string, JToken>();
                material.Extensions.Add(gltf.extensions.KhrMaterialsDiffuseTransmission.Name,
                    gltf.Document.SaveAsNode(specular));
                _extensionsUsed.Add(gltf.extensions.KhrMaterialsDiffuseTransmission.Name);
            }

            if (overrides.TransmissionFactor.HasValue)
            {
                var transmission = new gltf.extensions.KhrMaterialsTransmission
                {
                    TransmissionFactor = overrides.TransmissionFactor,
                    TransmissionTexture = ExportTextureInfo(source, overrides.TransmissionTexture, ColorSpace.Linear,
                        _metalGlossChannelSwapMaterial, needsBlit: false),
                };
                material.Extensions ??= new Dictionary<string, JToken>();
                material.Extensions.Add(gltf.extensions.KhrMaterialsTransmission.Name,
                    gltf.Document.SaveAsNode(transmission));
                _extensionsUsed.Add(gltf.extensions.KhrMaterialsTransmission.Name);
            }

            if (overrides.ThicknessFactor.HasValue)
            {
                var volume = new gltf.extensions.KhrMaterialsVolume
                {
                    ThicknessFactor = overrides.ThicknessFactor.Value,
                    AttenuationColor = overrides.AttenuationColor?.ToVector3(),
                    AttenuationDistance = overrides.AttenuationDistance,
                };
                material.Extensions ??= new Dictionary<string, JToken>();
                material.Extensions.Add(gltf.extensions.KhrMaterialsVolume.Name, gltf.Document.SaveAsNode(volume));
                _extensionsUsed.Add(gltf.extensions.KhrMaterialsVolume.Name);
            }

            if (overrides.Ior.HasValue)
            {
                var ior = new gltf.extensions.KhrMaterialsIor
                {
                    Ior = overrides.Ior
                };
                material.Extensions ??= new Dictionary<string, JToken>();
                material.Extensions.Add(gltf.extensions.KhrMaterialsIor.Name, gltf.Document.SaveAsNode(ior));
                _extensionsUsed.Add(gltf.extensions.KhrMaterialsIor.Name);
            }

            var cull = overrides.CullMode.GetValueOrDefault(source.GetIntOrDefault(PropertyCullMode, 0));
            if (cull == CullModeFront)
            {
                Debug.LogWarning($"Cull mode with Front is not supported due to glTF specification limit: {source}");
            }

            material.DoubleSided = cull == CullModeNone;

            return material;
        }

        internal static ExportOverrides CreateExportOverrides(IAssetSaver assetSaver, Material subMeshMaterial,
            string shaderName, bool enableBakingAlphaMaskTexture, ref bool isShaderLiltoon)
        {
            var config = new ExportOverrides();
#if NVE_HAS_LILTOON
            if (!isShaderLiltoon)
            {
                return config;
            }

            // lilToon PBR should be set as glTF PBR
            var specularToon = subMeshMaterial.GetIntOrDefault(PropertySpecularToon, 0);
            var reflectionBlendMode = subMeshMaterial.GetIntOrDefault(PropertyReflectionBlendMode, 1);
            if (specularToon == SpecularToonReal && reflectionBlendMode == ReflectionBlendModeAdd)
            {
                ConvertToGltfMetallicRoughness(subMeshMaterial, config);
                ConvertToGltfMaterialSpecular(subMeshMaterial, config);
                ConvertToGltfMaterialDiffuseTransmission(subMeshMaterial, config);
                if (shaderName.Contains("Refraction", StringComparison.Ordinal) ||
                    subMeshMaterial.GetIntOrDefault(PropertyTransparentMode, 0) == TransparentModeRefraction)
                {
                    ConvertToGltfMaterialTransmission(subMeshMaterial, config);
                    ConvertToGltfMaterialVolume(subMeshMaterial, config);
                    ConvertToGltfMaterialIor(config);
                }

                isShaderLiltoon = false;
                return config;
            }

            if (shaderName.Contains("Cutout", StringComparison.Ordinal))
            {
                config.AlphaMode = gltf.material.AlphaMode.Mask;
                config.AlphaCutoff = subMeshMaterial.GetFloatOrDefault(PropertyCutoff, 0.0f);
                config.MainTexture = MaterialBaker.AutoBakeMainTexture(assetSaver, subMeshMaterial);
            }
            else if (shaderName.Contains("Transparent", StringComparison.Ordinal) ||
                     shaderName.Contains("Overlay", StringComparison.Ordinal))
            {
                config.AlphaMode = gltf.material.AlphaMode.Blend;
                config.MainTexture = enableBakingAlphaMaskTexture &&
                                     Mathf.Approximately(subMeshMaterial.GetFloatOrDefault(PropertyAlphaMaskMode, 0.0f),
                                         1.0f)
                    ? MaterialBaker.AutoBakeAlphaMask(assetSaver, subMeshMaterial)
                    : MaterialBaker.AutoBakeMainTexture(assetSaver, subMeshMaterial);
            }
            else
            {
                switch (subMeshMaterial.GetIntOrDefault(PropertyTransparentMode, 0))
                {
                    case 0:
                        config.AlphaMode = gltf.material.AlphaMode.Opaque;
                        break;
                    case 1:
                        config.AlphaMode = gltf.material.AlphaMode.Mask;
                        config.AlphaCutoff = subMeshMaterial.GetFloatOrDefault(PropertyCutoff, 0.0f);
                        break;
                    case 2:
                        config.AlphaMode = gltf.material.AlphaMode.Blend;
                        break;
                    default:
                        config.AlphaMode = gltf.material.AlphaMode.Opaque;
                        break;
                }

                config.MainTexture = MaterialBaker.AutoBakeMainTexture(assetSaver, subMeshMaterial);
            }

            config.CullMode = subMeshMaterial.GetIntOrDefault(PropertyCull, (int)CullMode.Back);
            if (Mathf.Approximately(subMeshMaterial.GetFloatOrDefault(PropertyUseEmission, 0.0f), 1.0f))
            {
                config.EmissiveStrength = !subMeshMaterial.GetTexture(PropertyEmissionBlendMask)
                    ? 1.0f - Mathf.Clamp01(subMeshMaterial.GetFloatOrDefault(PropertyEmissionMainStrength, 0.0f))
                    : 0.0f;
            }

            config.EnableNormalMap =
                Mathf.Approximately(subMeshMaterial.GetFloatOrDefault(PropertyUseBumpMap, 0.0f), 1.0f);
#endif // NVE_HAS_LILTOON
            return config;
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

        internal static gltf.exporter.SampledTextureUnit ExportTextureUnit(Texture texture, string name,
            TextureFormat textureFormat, ColorSpace cs, Material? blitMaterial, bool needsBlit)
        {
            using var _ = new NdmfVrmExporter.ScopedProfile($"ExportTextureUnit({name})");
            byte[] bytes;
            if (needsBlit || GraphicsFormatUtility.IsCompressedFormat(texture.graphicsFormat))
            {
                var destTexture = texture.Blit(textureFormat, cs, blitMaterial);
                bytes = destTexture.EncodeToPNG();
                Object.DestroyImmediate(destTexture);
            }
            else if (texture.isReadable && texture is Texture2D texture2D)
            {
                bytes = texture2D.EncodeToPNG();
            }
            else
            {
                bytes = Texture2D.whiteTexture.EncodeToPNG();
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

        private static void ConvertToGltfMetallicRoughness(Material subMeshMaterial, ExportOverrides config)
        {
            var metallicFactor = Mathf.GammaToLinearSpace(subMeshMaterial.GetFloatOrDefault(PropertyMetallic, 0.0f));
            var smoothnessFactor = subMeshMaterial.GetFloatOrDefault(PropertySmoothness, 0.0f);
            var smoothnessTexture = subMeshMaterial.GetTexture(PropertySmoothnessTex) as Texture2D;
            var metallicGlossinessTexture = subMeshMaterial.GetTexture(PropertyMetallicGlossMap) as Texture2D;
            var roughnessFactor = 1.0f - smoothnessFactor;
            if (smoothnessTexture && metallicGlossinessTexture)
            {
                var width = smoothnessTexture!.width;
                var height = smoothnessTexture.height;
                var metallicRoughnessTexturePixels = new Color[width * height];
                var smoothnessTexturePixels = smoothnessTexture.GetPixels();
                var metallicGlossinessTexturePixels = metallicGlossinessTexture!.GetPixels();
                for (var j = 0; j < height; j++)
                {
                    var stride = j * width;
                    for (var i = 0; i < width; i++)
                    {
                        var offset = stride + i;
                        var roughness = 1.0f - (smoothnessTexturePixels[offset].linear.r * smoothnessFactor);
                        var metallic = metallicGlossinessTexturePixels[offset].linear.r * metallicFactor;
                        metallicRoughnessTexturePixels[offset] = new Color(1.0f, roughness, metallic, 1.0f);
                    }
                }

                var metallicRoughnessTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                metallicRoughnessTexture.SetPixels(metallicRoughnessTexturePixels);
                config.MetallicRoughnessTexture = metallicRoughnessTexture;
                config.MetallicFactor = 1.0f;
                config.RoughnessFactor = 1.0f;
            }
            else if (metallicGlossinessTexture)
            {
                var width = metallicGlossinessTexture!.width;
                var height = metallicGlossinessTexture.height;
                var metallicRoughnessTexturePixels = new Color[width * height];
                var metallicGlossinessTexturePixels = metallicGlossinessTexture.GetPixels();
                for (var j = 0; j < height; j++)
                {
                    var stride = j * width;
                    for (var i = 0; i < width; i++)
                    {
                        var offset = stride + i;
                        var metallic = metallicGlossinessTexturePixels[offset].linear.r;
                        metallicRoughnessTexturePixels[offset] = new Color(1.0f, roughnessFactor, metallic, 1.0f);
                    }
                }

                var metallicRoughnessTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                metallicRoughnessTexture.SetPixels(metallicRoughnessTexturePixels);
                config.MetallicRoughnessTexture = metallicRoughnessTexture;
                config.MetallicFactor = 1.0f;
                config.RoughnessFactor = roughnessFactor;
            }
            else if (smoothnessTexture)
            {
                var width = smoothnessTexture!.width;
                var height = smoothnessTexture.height;
                var metallicRoughnessTexturePixels = new Color[width * height];
                var smoothnessTexturePixels = smoothnessTexture.GetPixels();
                for (var j = 0; j < height; j++)
                {
                    var stride = j * width;
                    for (var i = 0; i < width; i++)
                    {
                        var offset = stride + i;
                        var roughness = 1.0f - smoothnessTexturePixels[offset].linear.r;
                        metallicRoughnessTexturePixels[offset] = new Color(1.0f, roughness, metallicFactor, 1.0f);
                    }
                }

                var metallicRoughnessTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                metallicRoughnessTexture.SetPixels(metallicRoughnessTexturePixels);
                config.MetallicRoughnessTexture = metallicRoughnessTexture;
                config.MetallicFactor = metallicFactor;
                config.RoughnessFactor = 1.0f;
            }
            else
            {
                config.MetallicFactor = Mathf.GammaToLinearSpace(metallicFactor);
                config.RoughnessFactor = roughnessFactor;
            }
        }

        private static void ConvertToGltfMaterialSpecular(Material subMeshMaterial, ExportOverrides config)
        {
            var reflectance = Mathf.GammaToLinearSpace(subMeshMaterial.GetFloatOrDefault(PropertyReflectance, 0.04f));
            config.SpecularColorFactor = new Color(reflectance / 0.04f, reflectance / 0.04f, reflectance / 0.04f);
            config.SpecularFactor = 1.0f;
        }

        private static void ConvertToGltfMaterialDiffuseTransmission(Material subMeshMaterial, ExportOverrides config)
        {
            var backlightColor = subMeshMaterial.GetColor(PropertyBacklightColor);
            config.DiffuseTransmissionColorFactor = new Color(backlightColor.r, backlightColor.g, backlightColor.b);
            config.DiffuseTransmissionFactor = backlightColor.a;
        }

        private static void ConvertToGltfMaterialTransmission(Material subMeshMaterial, ExportOverrides config)
        {
            var alphaColor = subMeshMaterial.GetColorOrDefault(PropertyColor, Color.white).linear.a;
            config.TransmissionFactor = 1.0f - alphaColor;
            var mainTexture = subMeshMaterial.GetTexture(PropertyMainTex) as Texture2D;
            if (!mainTexture)
            {
                return;
            }

            var width = mainTexture!.width;
            var height = mainTexture.height;
            var transmissionTexturePixels = new Color[width * height];
            var mainTexturePixels = mainTexture.GetPixels();
            for (var j = 0; j < height; j++)
            {
                var stride = j * width;
                for (var i = 0; i < width; i++)
                {
                    var offset = stride + i;
                    var alpha = 1.0f - mainTexturePixels[offset].linear.a;
                    transmissionTexturePixels[offset] = new Color(1.0f, 1.0f, 1.0f, alpha);
                }
            }

            var transmissionTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            transmissionTexture.SetPixels(transmissionTexturePixels);
            config.TransmissionTexture = transmissionTexture;
        }

        private static void ConvertToGltfMaterialVolume(Material subMeshMaterial, ExportOverrides config)
        {
            config.ThicknessFactor = 0;
            config.AttenuationColor = subMeshMaterial.GetColor(PropertyRefractionColor);
            config.AttenuationDistance = float.MaxValue;
        }

        private static void ConvertToGltfMaterialIor(ExportOverrides config)
        {
            config.Ior = 1.5f;
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
            return source.GetColorOrDefault(PropertyEmissionColor, Color.black)
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
        private static readonly int PropertyCull = Shader.PropertyToID("_Cull");
        private static readonly int PropertyUseEmission = Shader.PropertyToID("_UseEmission");
        private static readonly int PropertyEmissionMainStrength = Shader.PropertyToID("_EmissionMainStrength");
        private static readonly int PropertyEmissionBlendMask = Shader.PropertyToID("_EmissionBlendMask");
        private static readonly int PropertyUseBumpMap = Shader.PropertyToID("_UseBumpMap");
        private static readonly int PropertyAlphaMaskMode = Shader.PropertyToID("_AlphaMaskMode");

        // lilToon
        private static readonly int PropertySpecularToon = Shader.PropertyToID("_SpecularToon");
        private static readonly int PropertyReflectionBlendMode = Shader.PropertyToID("_ReflectionBlendMode");
        private static readonly int PropertySmoothness = Shader.PropertyToID("_Smoothness");
        private static readonly int PropertySmoothnessTex = Shader.PropertyToID("_SmoothnessTex");
        private static readonly int PropertyReflectance = Shader.PropertyToID("_Reflectance");
        private static readonly int PropertyReflectionColor = Shader.PropertyToID("_ReflectionColor");
        private static readonly int PropertyReflectionColorTex = Shader.PropertyToID("_ReflectionColorTex");
        private static readonly int PropertyBacklightColor = Shader.PropertyToID("_BacklightColor");
        private static readonly int PropertyTransparentMode = Shader.PropertyToID("_TransparentMode");
        private static readonly int PropertyRefractionColor = Shader.PropertyToID("_RefractionColor");

        public IDictionary<gltf.ObjectID, TextureItemMetadata> TextureMetadata { get; }
        private readonly gltf.Root _root;
        private readonly gltf.exporter.Exporter _exporter;
        private readonly IDictionary<Texture, gltf.ObjectID> _textureIDs;
        private readonly ISet<string> _extensionsUsed;
        private readonly Material _metalGlossChannelSwapMaterial;
        private readonly Material _metalGlossOcclusionChannelSwapMaterial;
        private readonly Material _normalChannelMaterial;
    }
}
