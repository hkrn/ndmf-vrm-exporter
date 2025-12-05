// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
            var dictionaries = GetProps(material);
            var mainColor = dictionaries.GetColor("_Color");
            var mainTexHsvg = dictionaries.GetVector("_MainTexHSVG");
            var mainGradationStrength = dictionaries.GetFloat("_MainGradationStrength");
            var useMainSecondTex = dictionaries.GetFloat("_UseMain2ndTex");
            var useMainThirdTex = dictionaries.GetFloat("_UseMain3rdTex");
            var mainTex = dictionaries.GetTexture("_MainTex");
            var shouldNotBakeAll = mainColor == Color.white && mainTexHsvg == lilConstants.defaultHSVG &&
                                   mainGradationStrength == 0.0 && useMainSecondTex == 0.0 && useMainThirdTex == 0.0;
            if (shouldNotBakeAll)
                return null;
            var bakeSecond = useMainSecondTex != 0.0;
            var bakeThird = useMainThirdTex != 0.0;
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
            hsvgMaterial.CopyTexture(dictionaries, "_MainGradationTex");
            hsvgMaterial.CopyTexture(dictionaries, "_MainColorAdjustMask");

            AssignMaterialTexture(assetSaver, mainTex, hsvgMaterial, PropertyMainTex, ref srcTexture);
            if (bakeSecond)
            {
                hsvgMaterial.CopyFloat(dictionaries, "_UseMain2ndTex");
                hsvgMaterial.CopyColor(dictionaries, "_MainColor2nd");
                hsvgMaterial.CopyFloat(dictionaries, "_Main2ndTexAngle");
                hsvgMaterial.CopyFloat(dictionaries, "_Main2ndTexIsDecal");
                hsvgMaterial.CopyFloat(dictionaries, "_Main2ndTexIsLeftOnly");
                hsvgMaterial.CopyFloat(dictionaries, "_Main2ndTexIsRightOnly");
                hsvgMaterial.CopyFloat(dictionaries, "_Main2ndTexShouldCopy");
                hsvgMaterial.CopyFloat(dictionaries, "_Main2ndTexShouldFlipMirror");
                hsvgMaterial.CopyFloat(dictionaries, "_Main2ndTexShouldFlipCopy");
                hsvgMaterial.CopyFloat(dictionaries, "_Main2ndTexIsMSDF");
                hsvgMaterial.CopyFloat(dictionaries, "_Main2ndTexBlendMode");
                hsvgMaterial.CopyFloat(dictionaries, "_Main2ndTexAlphaMode");
                hsvgMaterial.CopyTextureOffset(dictionaries, "_Main2ndTex");
                hsvgMaterial.CopyTextureScale(dictionaries, "_Main2ndTex");
                hsvgMaterial.CopyTextureOffset(dictionaries, "_Main2ndBlendMask");
                hsvgMaterial.CopyTextureScale(dictionaries, "_Main2ndBlendMask");

                AssignMaterialTexture(assetSaver, dictionaries.GetTexture("_Main2ndTex"), hsvgMaterial,
                    PropertyMainSecondTex, ref srcMain2);
                AssignMaterialTexture(assetSaver, dictionaries.GetTexture("_Main2ndBlendMask"), hsvgMaterial,
                    PropertyMainSecondBlendMask, ref srcMask2);
            }

            if (bakeThird)
            {
                hsvgMaterial.CopyFloat(dictionaries, "_UseMain3rdTex");
                hsvgMaterial.CopyColor(dictionaries, "_MainColor3rd");
                hsvgMaterial.CopyFloat(dictionaries, "_Main3rdTexAngle");
                hsvgMaterial.CopyFloat(dictionaries, "_Main3rdTexIsDecal");
                hsvgMaterial.CopyFloat(dictionaries, "_Main3rdTexIsLeftOnly");
                hsvgMaterial.CopyFloat(dictionaries, "_Main3rdTexIsRightOnly");
                hsvgMaterial.CopyFloat(dictionaries, "_Main3rdTexShouldCopy");
                hsvgMaterial.CopyFloat(dictionaries, "_Main3rdTexShouldFlipMirror");
                hsvgMaterial.CopyFloat(dictionaries, "_Main3rdTexShouldFlipCopy");
                hsvgMaterial.CopyFloat(dictionaries, "_Main3rdTexIsMSDF");
                hsvgMaterial.CopyFloat(dictionaries, "_Main3rdTexBlendMode");
                hsvgMaterial.CopyFloat(dictionaries, "_Main3rdTexAlphaMode");
                hsvgMaterial.CopyTextureOffset(dictionaries, "_Main3rdTex");
                hsvgMaterial.CopyTextureScale(dictionaries, "_Main3rdTex");
                hsvgMaterial.CopyTextureOffset(dictionaries, "_Main3rdBlendMask");
                hsvgMaterial.CopyTextureScale(dictionaries, "_Main3rdBlendMask");

                AssignMaterialTexture(assetSaver, dictionaries.GetTexture("_Main3rdTex"), hsvgMaterial,
                    PropertyMainThirdTex, ref srcMain3);
                AssignMaterialTexture(assetSaver, dictionaries.GetTexture("_Main3rdBlendMask"), hsvgMaterial,
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
            var dictionaries = GetProps(material);
            var useShadow = dictionaries.GetFloat("_UseShadow");
            var shadowColor = dictionaries.GetColor("_ShadowColor");
            var shadowColorTex = dictionaries.GetTexture("_ShadowColorTex");
            var shadowStrength = dictionaries.GetFloat("_ShadowStrength");
            var shadowStrengthMask = dictionaries.GetTexture("_ShadowStrengthMask");
            var mainTex = dictionaries.GetTexture("_MainTex");

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
                new Color(1.0f, 1.0f, 1.0f, dictionaries.GetFloat("_ShadowMainStrength")));
            hsvgMaterial.SetFloat(PropertyMainThirdTexBlendMode, 3.0f);

            Texture? shadowTex;
            switch (shadowType)
            {
                case 2:
                {
                    var shadowSecondColor = dictionaries.GetColor("_Shadow2ndColor");
                    hsvgMaterial.SetColor(PropertyMainColorSecond,
                        new Color(shadowSecondColor.r, shadowSecondColor.g, shadowSecondColor.b,
                            shadowSecondColor.a * shadowStrength));
                    hsvgMaterial.SetFloat(PropertyMainSecondTexBlendMode, 0.0f);
                    hsvgMaterial.SetFloat(PropertyMainSecondTexAlphaMode, 0.0f);
                    shadowTex = dictionaries.GetTexture("_Shadow2ndColorTex");
                    break;
                }
                case 3:
                {
                    var shadowThirdColor = dictionaries.GetColor("_Shadow3rdColor");
                    hsvgMaterial.SetColor(PropertyMainColorThird,
                        new Color(shadowThirdColor.r, shadowThirdColor.g, shadowThirdColor.b,
                            shadowThirdColor.a * shadowStrength));
                    hsvgMaterial.SetFloat(PropertyMainThirdTexBlendMode, 0.0f);
                    hsvgMaterial.SetFloat(PropertyMainThirdTexAlphaMode, 0.0f);
                    shadowTex = dictionaries.GetTexture("_Shadow3rdColorTex");
                    break;
                }
                default:
                    hsvgMaterial.SetColor(PropertyMainColorSecond,
                        new Color(shadowColor.r, shadowColor.g, shadowColor.b, shadowStrength));
                    hsvgMaterial.SetFloat(PropertyMainSecondTexBlendMode, 0.0f);
                    hsvgMaterial.SetFloat(PropertyMainSecondTexAlphaMode, 0.0f);
                    shadowTex = dictionaries.GetTexture("_ShadowColorTex");
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
            var dictionaries = GetProps(material);
            var matcapColor = dictionaries.GetColor("_MatCapColor");
            var matcapTex = dictionaries.GetTexture("_MatCapTex");
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
            var dictionaries = GetProps(material);
            var mainTex = dictionaries.GetTexture("_MainTex");
            // run bake
            var bufMainTexture = mainTex as Texture2D;
            var hsvgMaterial = new Material(lilShaderManager.ltsbaker);

            var mainTexName = mainTex ? mainTex!.name : AssetPathUtils.TrimCloneSuffix(material.name);
            var srcTexture = new Texture2D(2, 2);
            var srcAlphaMask = new Texture2D(2, 2);

            hsvgMaterial.EnableKeyword("_ALPHAMASK");
            hsvgMaterial.SetColor(PropertyColor, Color.white);
            hsvgMaterial.SetVector(PropertyMainTexHsvg, lilConstants.defaultHSVG);
            hsvgMaterial.CopyFloat(dictionaries, "_AlphaMaskMode");
            hsvgMaterial.CopyFloat(dictionaries, "_AlphaMaskScale");
            hsvgMaterial.CopyFloat(dictionaries, "_AlphaMaskValue");

            var baseTex = dictionaries.GetTexture("_AlphaMask");
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

        private static (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs,
            Dictionary<string, SerializedProperty> cs) GetProps(Material material)
        {
            var so = new SerializedObject(material);
            so.Update();
            var props = so.FindProperty("m_SavedProperties");
            return (GetProps(props.FindPropertyRelative("m_TexEnvs")),
                GetProps(props.FindPropertyRelative("m_Floats")),
                GetProps(props.FindPropertyRelative("m_Colors")));
        }

        public static (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs,
            Dictionary<string, SerializedProperty> cs) GetProps(SerializedObject so)
        {
            var props = so.FindProperty("m_SavedProperties");
            return (GetProps(props.FindPropertyRelative("m_TexEnvs")),
                GetProps(props.FindPropertyRelative("m_Floats")),
                GetProps(props.FindPropertyRelative("m_Colors")));
        }

        public static Dictionary<string, SerializedProperty> GetProps(SerializedProperty props)
        {
            var dic = new Dictionary<string, SerializedProperty>();
            props.DoAllElements((p, _) =>
            {
                dic[p.FindPropertyRelative("first").stringValue] = p.FindPropertyRelative("second");
            });
            return dic;
        }

        private static void DoAllElements(this SerializedProperty property, Action<SerializedProperty, int> function)
        {
            if (property.arraySize == 0)
                return;
            var i = 0;
            var prop = property.GetArrayElementAtIndex(0);
            var end = property.GetEndProperty();
            function.Invoke(prop, i);
            while (prop.NextVisible(false) && !SerializedProperty.EqualContents(prop, end))
            {
                i++;
                function.Invoke(prop, i);
            }
        }

        private static float GetFloat(
            this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs,
                Dictionary<string, SerializedProperty> cs) dictionaries, string key)
        {
            return dictionaries.fs.TryGetValue(key, out var f) ? f.floatValue : 0f;
        }

        private static Color GetColor(
            this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs,
                Dictionary<string, SerializedProperty> cs) dictionaries, string key)
        {
            return dictionaries.cs.TryGetValue(key, out var c) ? c.colorValue : Color.black;
        }

        private static Vector4 GetVector(
            this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs,
                Dictionary<string, SerializedProperty> cs) dictionaries, string key)
        {
            return dictionaries.GetColor(key);
        }

        private static Texture? GetTexture(
            this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs,
                Dictionary<string, SerializedProperty> cs) dictionaries, string key)
        {
            if (dictionaries.ts.ContainsKey(key))
                return dictionaries.ts[key].FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
            else return null;
        }

        private static Vector2 GetTextureScale(
            this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs,
                Dictionary<string, SerializedProperty> cs) dictionaries, string key)
        {
            return dictionaries.ts.ContainsKey(key)
                ? dictionaries.ts[key].FindPropertyRelative("m_Scale").vector2Value
                : Vector2.one;
        }

        private static Vector2 GetTextureOffset(
            this (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs,
                Dictionary<string, SerializedProperty> cs) dictionaries, string key)
        {
            return dictionaries.ts.ContainsKey(key)
                ? dictionaries.ts[key].FindPropertyRelative("m_Offset").vector2Value
                : Vector2.zero;
        }

        private static void CopyFloat(this Material material,
            (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs,
                Dictionary<string, SerializedProperty> cs) dictionaries, string key)
        {
            material.SetFloat(key, dictionaries.GetFloat(key));
        }

        private static void CopyColor(this Material material,
            (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs,
                Dictionary<string, SerializedProperty> cs) dictionaries, string key)
        {
            material.SetColor(key, dictionaries.GetColor(key));
        }

        private static void CopyTexture(this Material material,
            (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs,
                Dictionary<string, SerializedProperty> cs) dictionaries, string key)
        {
            material.SetTexture(key, dictionaries.GetTexture(key));
        }

        private static void CopyTextureScale(this Material material,
            (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs,
                Dictionary<string, SerializedProperty> cs) dictionaries, string key)
        {
            material.SetTextureScale(key, dictionaries.GetTextureScale(key));
        }

        private static void CopyTextureOffset(this Material material,
            (Dictionary<string, SerializedProperty> ts, Dictionary<string, SerializedProperty> fs,
                Dictionary<string, SerializedProperty> cs) dictionaries, string key)
        {
            material.SetTextureOffset(key, dictionaries.GetTextureOffset(key));
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
    }
#endif // NVE_HAS_LILTOON
}
