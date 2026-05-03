// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
using UnityEditor;
using UnityEngine;
using nadena.dev.ndmf;

#if NVE_HAS_LILTOON
using lilToon;
#endif // NVE_HAS_LILTOON

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
#if NVE_HAS_LILTOON
    // Based on lilMaterialBaker
    // https://github.com/lilxyzw/lilToon/blob/f80e1f13f385c1774a6626cb585a4ab6f5728caa/Assets/lilToon/Editor/lilMaterialBaker.cs
    internal static class MaterialBaker
    {
        public static Texture? AutoBakeMainTexture(IAssetSaver assetSaver, Material material)
        {
            var mainColor = material.GetColor(PropertyColor);
            var mainTexHsvg = material.GetVector(PropertyMainTexHsvg);
            var mainGradationStrength = material.GetFloat(PropertyMainGradationStrength);
            var useMainSecondTex = material.GetFloat(PropertyUseMainSecondTex);
            var useMainThirdTex = material.GetFloat(PropertyUseMainThirdTex);
            var mainTex = material.GetTexture(PropertyMainTex);
            var shouldNotBakeAll = mainColor == Color.white && mainTexHsvg == lilConstants.defaultHSVG &&
                                   mainGradationStrength == 0.0 && useMainSecondTex == 0.0 && useMainThirdTex == 0.0;
            if (shouldNotBakeAll)
                return null;
            var mainSecondUVMode = material.HasFloat(PropertyMainSecondUVMode)
                ? material.GetFloat(PropertyMainSecondUVMode)
                : 0.0;
            var mainThirdUVMode = material.HasFloat(PropertyMainThirdUVMode)
                ? material.GetFloat(PropertyMainThirdUVMode)
                : 0.0;
            var bakeSecond = useMainSecondTex != 0.0 && mainSecondUVMode == 0.0;
            var bakeThird = useMainThirdTex != 0.0 && mainThirdUVMode == 0.0;
            // run bake
            var hsvgMaterial = new Material(lilShaderManager.ltsbaker);

            var mainTexName = mainTex ? mainTex!.name : AssetPathUtils.TrimCloneSuffix(material.name);
            var srcTexture = new Texture2D(2, 2);
            var srcMain2 = new Texture2D(2, 2);
            var srcMain3 = new Texture2D(2, 2);
            var srcMask2 = new Texture2D(2, 2);
            var srcMask3 = new Texture2D(2, 2);

            hsvgMaterial.SetColor(PropertyColor, Color.white);
            hsvgMaterial.SetVector(PropertyMainTexHsvg, mainTexHsvg);
            hsvgMaterial.SetFloat(PropertyMainGradationStrength, mainGradationStrength);
            CopyTexture(hsvgMaterial, material, PropertyMainGradationTex);
            CopyTexture(hsvgMaterial, material, PropertyMainColorAdjustMask);

            AssignMaterialTexture(assetSaver, mainTex, hsvgMaterial, PropertyMainTex, ref srcTexture);
            if (bakeSecond)
            {
                CopyFloat(hsvgMaterial, material, PropertyUseMainSecondTex);
                CopyColor(hsvgMaterial, material, PropertyMainColorSecond);
                CopyFloat(hsvgMaterial, material, PropertyMainSecondTexAngle);
                CopyFloat(hsvgMaterial, material, PropertyMainSecondTexIsDecal);
                CopyFloat(hsvgMaterial, material, PropertyMainSecondTexIsLeftOnly);
                CopyFloat(hsvgMaterial, material, PropertyMainSecondTexIsRightOnly);
                CopyFloat(hsvgMaterial, material, PropertyMainSecondTexShouldCopy);
                CopyFloat(hsvgMaterial, material, PropertyMainSecondTexShouldFlipMirror);
                CopyFloat(hsvgMaterial, material, PropertyMainSecondTexShouldFlipCopy);
                CopyFloat(hsvgMaterial, material, PropertyMainSecondTexIsMsdf);
                CopyFloat(hsvgMaterial, material, PropertyMainSecondTexBlendMode);
                CopyFloat(hsvgMaterial, material, PropertyMainSecondTexAlphaMode);
                CopyTextureOffset(hsvgMaterial, material, PropertyMainSecondTex);
                CopyTextureScale(hsvgMaterial, material, PropertyMainSecondTex);
                CopyTextureOffset(hsvgMaterial, material, PropertyMainSecondBlendMask);
                CopyTextureScale(hsvgMaterial, material, PropertyMainSecondBlendMask);

                AssignMaterialTexture(assetSaver, material.GetTexture(PropertyMainSecondTex), hsvgMaterial,
                    PropertyMainSecondTex, ref srcMain2);
                AssignMaterialTexture(assetSaver, material.GetTexture(PropertyMainSecondBlendMask), hsvgMaterial,
                    PropertyMainSecondBlendMask, ref srcMask2);
            }

            if (bakeThird)
            {
                CopyFloat(hsvgMaterial, material, PropertyUseMainThirdTex);
                CopyColor(hsvgMaterial, material, PropertyMainColorThird);
                CopyFloat(hsvgMaterial, material, PropertyMainThirdTexAngle);
                CopyFloat(hsvgMaterial, material, PropertyMainThirdTexIsDecal);
                CopyFloat(hsvgMaterial, material, PropertyMainThirdTexIsLeftOnly);
                CopyFloat(hsvgMaterial, material, PropertyMainThirdTexIsRightOnly);
                CopyFloat(hsvgMaterial, material, PropertyMainThirdTexShouldCopy);
                CopyFloat(hsvgMaterial, material, PropertyMainThirdTexShouldFlipMirror);
                CopyFloat(hsvgMaterial, material, PropertyMainThirdTexShouldFlipCopy);
                CopyFloat(hsvgMaterial, material, PropertyMainThirdTexIsMsdf);
                CopyFloat(hsvgMaterial, material, PropertyMainThirdTexBlendMode);
                CopyFloat(hsvgMaterial, material, PropertyMainThirdTexAlphaMode);
                CopyTextureOffset(hsvgMaterial, material, PropertyMainThirdTex);
                CopyTextureScale(hsvgMaterial, material, PropertyMainThirdTex);
                CopyTextureOffset(hsvgMaterial, material, PropertyMainThirdBlendMask);
                CopyTextureScale(hsvgMaterial, material, PropertyMainThirdBlendMask);

                AssignMaterialTexture(assetSaver, material.GetTexture(PropertyMainThirdTex), hsvgMaterial,
                    PropertyMainThirdTex, ref srcMain3);
                AssignMaterialTexture(assetSaver, material.GetTexture(PropertyMainThirdBlendMask), hsvgMaterial,
                    PropertyMainThirdBlendMask, ref srcMask3);
            }

            var outTexture = RunBake(srcTexture, hsvgMaterial);
            outTexture.name = $"{mainTexName}_{nameof(AutoBakeMainTexture)}";
            assetSaver.SaveAsset(outTexture);

            Object.DestroyImmediate(hsvgMaterial);
            Object.DestroyImmediate(srcTexture);
            Object.DestroyImmediate(srcMain2);
            Object.DestroyImmediate(srcMain3);
            Object.DestroyImmediate(srcMask2);
            Object.DestroyImmediate(srcMask3);

            return outTexture;
        }

        public static Texture? AutoBakeShadowTexture(IAssetSaver assetSaver, Material material,
            Texture? baseMainTexture, int shadowType = 0)
        {
            var useShadow = material.GetFloat(PropertyUseShadow);
            var shadowColor = material.GetColor(PropertyShadowColor);
            var shadowColorTex = material.GetTexture(PropertyShadowColorTex);
            var shadowStrength = material.GetFloat(PropertyShadowStrength);
            var shadowStrengthMask = material.GetTexture(PropertyShadowStrengthMask);
            var mainTex = material.GetTexture(PropertyMainTex);

            var shouldNotBakeAll = useShadow == 0.0 && shadowColor == Color.white && !shadowColorTex &&
                                   !shadowStrengthMask;
            if (shouldNotBakeAll)
                return null;
            // run bake
            var hsvgMaterial = new Material(lilShaderManager.ltsbaker);

            var mainTexName = mainTex ? mainTex!.name : AssetPathUtils.TrimCloneSuffix(material.name);
            var srcTexture = new Texture2D(2, 2);
            var srcMain2 = new Texture2D(2, 2);
            var srcMask2 = new Texture2D(2, 2);

            hsvgMaterial.SetColor(PropertyColor, Color.white);
            hsvgMaterial.SetVector(PropertyMainTexHsvg, lilConstants.defaultHSVG);
            hsvgMaterial.SetFloat(PropertyUseMainSecondTex, 1.0f);
            hsvgMaterial.SetFloat(PropertyUseMainThirdTex, 1.0f);
            hsvgMaterial.SetColor(PropertyMainColorThird,
                new Color(1.0f, 1.0f, 1.0f, material.GetFloat(PropertyShadowMainStrength)));
            hsvgMaterial.SetFloat(PropertyMainThirdTexBlendMode, 3.0f);

            Texture? shadowTex;
            switch (shadowType)
            {
                case 2:
                {
                    var shadowSecondColor = material.GetColor(PropertyShadowSecondColor);
                    hsvgMaterial.SetColor(PropertyMainColorSecond,
                        new Color(shadowSecondColor.r, shadowSecondColor.g, shadowSecondColor.b,
                            shadowSecondColor.a * shadowStrength));
                    hsvgMaterial.SetFloat(PropertyMainSecondTexBlendMode, 0.0f);
                    hsvgMaterial.SetFloat(PropertyMainSecondTexAlphaMode, 0.0f);
                    shadowTex = material.GetTexture(PropertyShadowSecondColorTex);
                    break;
                }
                case 3:
                {
                    var shadowThirdColor = material.GetColor(PropertyShadowThirdColor);
                    hsvgMaterial.SetColor(PropertyMainColorThird,
                        new Color(shadowThirdColor.r, shadowThirdColor.g, shadowThirdColor.b,
                            shadowThirdColor.a * shadowStrength));
                    hsvgMaterial.SetFloat(PropertyMainThirdTexBlendMode, 0.0f);
                    hsvgMaterial.SetFloat(PropertyMainThirdTexAlphaMode, 0.0f);
                    shadowTex = material.GetTexture(PropertyShadowThirdColorTex);
                    break;
                }
                default:
                    hsvgMaterial.SetColor(PropertyMainColorSecond,
                        new Color(shadowColor.r, shadowColor.g, shadowColor.b, shadowStrength));
                    hsvgMaterial.SetFloat(PropertyMainSecondTexBlendMode, 0.0f);
                    hsvgMaterial.SetFloat(PropertyMainSecondTexAlphaMode, 0.0f);
                    shadowTex = material.GetTexture(PropertyShadowColorTex);
                    break;
            }

            var referenceMainTexture =
                LoadTexture(assetSaver, baseMainTexture, ref srcTexture) ? srcTexture : Texture2D.whiteTexture;
            var referenceMainSecondTexture =
                LoadTexture(assetSaver, shadowTex, ref srcMain2) ? srcMain2 : referenceMainTexture;
            hsvgMaterial.SetTexture(PropertyMainTex, referenceMainTexture);
            hsvgMaterial.SetTexture(PropertyMainSecondTex, referenceMainSecondTexture);
            hsvgMaterial.SetTexture(PropertyMainThirdTex, referenceMainTexture);

            var referenceMaskTexture =
                LoadTexture(assetSaver, shadowStrengthMask, ref srcMask2) ? srcMask2 : Texture2D.whiteTexture;
            hsvgMaterial.SetTexture(PropertyMainSecondBlendMask, referenceMaskTexture);
            hsvgMaterial.SetTexture(PropertyMainThirdBlendMask, referenceMaskTexture);

            var outTexture = RunBake(srcTexture, hsvgMaterial);
            outTexture.name = $"{mainTexName}_{nameof(AutoBakeShadowTexture)}";
            assetSaver.SaveAsset(outTexture);

            Object.DestroyImmediate(hsvgMaterial);
            Object.DestroyImmediate(srcTexture);
            Object.DestroyImmediate(srcMain2);
            Object.DestroyImmediate(srcMask2);

            return outTexture;
        }

        public static Texture? AutoBakeMatCap(IAssetSaver assetSaver, Material material)
        {
            var matcapColor = material.GetColor(PropertyMatCapColor);
            var matcapTex = material.GetTexture(PropertyMatCapTex);
            var shouldNotBakeAll = matcapColor == Color.white;
            if (shouldNotBakeAll)
                return matcapTex;
            // run bake
            var bufMainTexture = matcapTex as Texture2D;
            var hsvgMaterial = new Material(lilShaderManager.ltsbaker);

            var matcapTexName = matcapTex ? matcapTex!.name : AssetPathUtils.TrimCloneSuffix(material.name);
            var srcTexture = new Texture2D(2, 2);

            hsvgMaterial.SetColor(PropertyColor, matcapColor);
            hsvgMaterial.SetVector(PropertyMainTexHsvg, lilConstants.defaultHSVG);
            AssignMaterialTexture(assetSaver, bufMainTexture, hsvgMaterial, PropertyMainTex, ref srcTexture);

            var outTexture = RunBake(srcTexture, hsvgMaterial);
            outTexture.name = $"{matcapTexName}_{nameof(AutoBakeMatCap)}";
            assetSaver.SaveAsset(outTexture);

            Object.DestroyImmediate(hsvgMaterial);
            Object.DestroyImmediate(srcTexture);

            return outTexture;
        }

        public static Texture? AutoBakeAlphaMask(IAssetSaver assetSaver, Material material)
        {
            var mainTex = material.GetTexture(PropertyMainTex);
            // run bake
            var bufMainTexture = mainTex as Texture2D;
            var hsvgMaterial = new Material(lilShaderManager.ltsbaker);

            var mainTexName = mainTex ? mainTex!.name : AssetPathUtils.TrimCloneSuffix(material.name);
            var srcTexture = new Texture2D(2, 2);
            var srcAlphaMask = new Texture2D(2, 2);

            hsvgMaterial.EnableKeyword("_ALPHAMASK");
            hsvgMaterial.SetColor(PropertyColor, Color.white);
            hsvgMaterial.SetVector(PropertyMainTexHsvg, lilConstants.defaultHSVG);
            CopyFloat(hsvgMaterial, material, PropertyAlphaMaskMode);
            CopyFloat(hsvgMaterial, material, PropertyAlphaMaskScale);
            CopyFloat(hsvgMaterial, material, PropertyAlphaMaskValue);

            var baseTex = material.GetTexture(PropertyAlphaMask);
            Texture2D? outTexture = null;
            if (LoadTexture(assetSaver, baseTex, ref srcAlphaMask))
            {
                hsvgMaterial.SetTexture(PropertyAlphaMask, srcAlphaMask);
                AssignMaterialTexture(assetSaver, bufMainTexture, hsvgMaterial, PropertyMainTex, ref srcTexture);
                outTexture = RunBake(srcTexture, hsvgMaterial);
                outTexture.name = $"{mainTexName}_{nameof(AutoBakeAlphaMask)}";
                assetSaver.SaveAsset(outTexture);
            }

            Object.DestroyImmediate(hsvgMaterial);
            Object.DestroyImmediate(srcTexture);

            return outTexture;
        }

        private static Texture2D RunBake(Texture2D srcTexture, Material material,
            Texture2D? referenceTexture = null)
        {
            int width = 4096, height = 4096;
            if (referenceTexture)
            {
                width = referenceTexture!.width;
                height = referenceTexture.height;
            }
            else if (srcTexture)
            {
                width = srcTexture.width;
                height = srcTexture.height;
            }

            var outTexture = new Texture2D(width, height);
            var bufRT = RenderTexture.active;
            var dstTexture = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(srcTexture, dstTexture, material);
            RenderTexture.active = dstTexture;
            outTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = bufRT;
            RenderTexture.ReleaseTemporary(dstTexture);
            return outTexture;
        }

        private static void AssignMaterialTexture(IAssetSaver assetSaver, Texture? baseTex, Material material,
            int propertyID, ref Texture2D srcTexture)
        {
            var result = LoadTexture(assetSaver, baseTex, ref srcTexture);
            material.SetTexture(propertyID, result ? srcTexture : Texture2D.whiteTexture);
        }

        private static bool LoadTexture(IAssetSaver assetSaver, Texture? baseTexture, ref Texture2D srcTexture)
        {
            if (baseTexture is Texture2D { isReadable: true } readableTexture)
            {
                srcTexture.LoadImage(readableTexture.EncodeToPNG());
            }
            else if (baseTexture && assetSaver.IsTemporaryAsset(baseTexture))
            {
                Object.Destroy(srcTexture);
                var innerBaseTexture = baseTexture!;
                srcTexture = new Texture2D(innerBaseTexture.width, innerBaseTexture.height);
                Graphics.ConvertTexture(innerBaseTexture, srcTexture);
            }
            else
            {
                var path = AssetDatabase.GetAssetPath(baseTexture);
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                lilTextureUtils.LoadTexture(ref srcTexture, path);
            }

            return true;
        }

        private static void CopyFloat(Material dest, Material source, int name)
        {
            if (source.HasProperty(name))
            {
                dest.SetFloat(name, source.GetFloat(name));
            }
        }

        private static void CopyColor(Material dest, Material source, int name)
        {
            if (source.HasProperty(name))
            {
                dest.SetColor(name, source.GetColor(name));
            }
        }

        private static void CopyTexture(Material dest, Material source, int name)
        {
            if (source.HasProperty(name))
            {
                dest.SetTexture(name, source.GetTexture(name));
            }
        }

        private static void CopyTextureOffset(Material dest, Material source, int name)
        {
            if (source.HasProperty(name))
            {
                dest.SetTextureOffset(name, source.GetTextureOffset(name));
            }
        }

        private static void CopyTextureScale(Material dest, Material source, int name)
        {
            if (source.HasProperty(name))
            {
                dest.SetTextureScale(name, source.GetTextureScale(name));
            }
        }

        private static readonly int PropertyColor = Shader.PropertyToID("_Color");
        private static readonly int PropertyMainTexHsvg = Shader.PropertyToID("_MainTexHSVG");
        private static readonly int PropertyMainGradationStrength = Shader.PropertyToID("_MainGradationStrength");
        private static readonly int PropertyMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int PropertyMainSecondTex = Shader.PropertyToID("_Main2ndTex");
        private static readonly int PropertyMainSecondBlendMask = Shader.PropertyToID("_Main2ndBlendMask");
        private static readonly int PropertyMainThirdTex = Shader.PropertyToID("_Main3rdTex");
        private static readonly int PropertyMainThirdBlendMask = Shader.PropertyToID("_Main3rdBlendMask");
        private static readonly int PropertyUseMainSecondTex = Shader.PropertyToID("_UseMain2ndTex");
        private static readonly int PropertyUseMainThirdTex = Shader.PropertyToID("_UseMain3rdTex");
        private static readonly int PropertyMainColorThird = Shader.PropertyToID("_MainColor3rd");
        private static readonly int PropertyMainThirdTexBlendMode = Shader.PropertyToID("_Main3rdTexBlendMode");
        private static readonly int PropertyMainColorSecond = Shader.PropertyToID("_MainColor2nd");
        private static readonly int PropertyMainSecondTexBlendMode = Shader.PropertyToID("_Main2ndTexBlendMode");
        private static readonly int PropertyMainSecondTexAlphaMode = Shader.PropertyToID("_Main2ndTexAlphaMode");
        private static readonly int PropertyMainThirdTexAlphaMode = Shader.PropertyToID("_Main3rdTexAlphaMode");
        private static readonly int PropertyAlphaMask = Shader.PropertyToID("_AlphaMask");
        private static readonly int PropertyMatCapColor = Shader.PropertyToID("_MatCapColor");
        private static readonly int PropertyMatCapTex = Shader.PropertyToID("_MatCapTex");
        private static readonly int PropertyUseShadow = Shader.PropertyToID("_UseShadow");
        private static readonly int PropertyShadowColor = Shader.PropertyToID("_ShadowColor");
        private static readonly int PropertyShadowColorTex = Shader.PropertyToID("_ShadowColorTex");
        private static readonly int PropertyShadowStrength = Shader.PropertyToID("_ShadowStrength");
        private static readonly int PropertyShadowStrengthMask = Shader.PropertyToID("_ShadowStrengthMask");
        private static readonly int PropertyShadowMainStrength = Shader.PropertyToID("_ShadowMainStrength");
        private static readonly int PropertyShadowSecondColor = Shader.PropertyToID("_Shadow2ndColor");
        private static readonly int PropertyShadowSecondColorTex = Shader.PropertyToID("_Shadow2ndColorTex");
        private static readonly int PropertyShadowThirdColor = Shader.PropertyToID("_Shadow3rdColor");
        private static readonly int PropertyShadowThirdColorTex = Shader.PropertyToID("_Shadow3rdColorTex");
        private static readonly int PropertyMainGradationTex = Shader.PropertyToID("_MainGradationTex");
        private static readonly int PropertyMainColorAdjustMask = Shader.PropertyToID("_MainColorAdjustMask");
        private static readonly int PropertyMainSecondTexAngle = Shader.PropertyToID("_Main2ndTexAngle");
        private static readonly int PropertyMainSecondTexIsDecal = Shader.PropertyToID("_Main2ndTexIsDecal");
        private static readonly int PropertyMainSecondTexIsLeftOnly = Shader.PropertyToID("_Main2ndTexIsLeftOnly");
        private static readonly int PropertyMainSecondTexIsRightOnly = Shader.PropertyToID("_Main2ndTexIsRightOnly");
        private static readonly int PropertyMainSecondTexShouldCopy = Shader.PropertyToID("_Main2ndTexShouldCopy");
        private static readonly int PropertyMainSecondUVMode = Shader.PropertyToID("_Main2ndTex_UVMode");

        private static readonly int PropertyMainSecondTexShouldFlipMirror =
            Shader.PropertyToID("_Main2ndTexShouldFlipMirror");

        private static readonly int PropertyMainSecondTexShouldFlipCopy =
            Shader.PropertyToID("_Main2ndTexShouldFlipCopy");

        private static readonly int PropertyMainSecondTexIsMsdf = Shader.PropertyToID("_Main2ndTexIsMSDF");
        private static readonly int PropertyMainThirdTexAngle = Shader.PropertyToID("_Main3rdTexAngle");
        private static readonly int PropertyMainThirdTexIsDecal = Shader.PropertyToID("_Main3rdTexIsDecal");
        private static readonly int PropertyMainThirdTexIsLeftOnly = Shader.PropertyToID("_Main3rdTexIsLeftOnly");
        private static readonly int PropertyMainThirdTexIsRightOnly = Shader.PropertyToID("_Main3rdTexIsRightOnly");
        private static readonly int PropertyMainThirdTexShouldCopy = Shader.PropertyToID("_Main3rdTexShouldCopy");
        private static readonly int PropertyMainThirdUVMode = Shader.PropertyToID("_Main3rdTex_UVMode");

        private static readonly int PropertyMainThirdTexShouldFlipMirror =
            Shader.PropertyToID("_Main3rdTexShouldFlipMirror");

        private static readonly int PropertyMainThirdTexShouldFlipCopy =
            Shader.PropertyToID("_Main3rdTexShouldFlipCopy");

        private static readonly int PropertyMainThirdTexIsMsdf = Shader.PropertyToID("_Main3rdTexIsMSDF");
        private static readonly int PropertyAlphaMaskMode = Shader.PropertyToID("_AlphaMaskMode");
        private static readonly int PropertyAlphaMaskScale = Shader.PropertyToID("_AlphaMaskScale");
        private static readonly int PropertyAlphaMaskValue = Shader.PropertyToID("_AlphaMaskValue");
    }
#endif // NVE_HAS_LILTOON
}
