// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using nadena.dev.ndmf;

#if NVE_HAS_LILYCAL_INVENTORY
using jp.lilxyzw.lilycalinventory.runtime;
#endif // NVE_HAS_LILYCAL_INVENTORY

#if NVE_HAS_MODULAR_AVATAR
using nadena.dev.modular_avatar.core;
#endif // NVE_HAS_MODULAR_AVATAR

[assembly: ExportsPlugin(typeof(com.github.hkrn.NdmfVrmExporterPlugin))]
// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    internal sealed class NdmfVrmExporterPlugin : Plugin<NdmfVrmExporterPlugin>
    {
        public override string QualifiedName => NdmfVrmExporter.PackageJson.Name;

        public override string DisplayName => "NDMF VRM Exporter";

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .BeforePlugin("jp.lilxyzw.lilycalinventory")
                .Run("Retrieve all LI CostumeChanger components to be converted to KHR_materials_variants",
                    RetrieveAllLiCostumeChangerComponentsPass)
                .Then
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Retrieve all MA reactive components to be converted to KHR_materials_variants",
                    RetrieveAllModularAvatarReactiveComponentsPass);
            InPhase(BuildPhase.Optimizing)
                .AfterPlugin("com.anatawa12.avatar-optimizer")
                .AfterPlugin("nadena.dev.modular-avatar")
                .AfterPlugin("net.rs64.tex-trans-tool")
                .Run("Export VRM 1.0 file with NDMF VRM Exporter", ExportVrmFilePass);
        }

        private static void ExportVrmFilePass(BuildContext buildContext)
        {
            var avatarRootObject = buildContext.AvatarRootObject;
            if (!avatarRootObject.TryGetComponent<NdmfVrmExporterComponent>(out var component))
            {
                Debug.LogWarning(Translator._("component.runtime.error.validation.not-attached"));
                return;
            }

            if (!component.enabled)
            {
                Debug.LogWarning(Translator._("component.runtime.error.validation.not-enabled"));
                return;
            }

            var basePath = AssetPathUtils.GetOutputPath(avatarRootObject);
            Directory.CreateDirectory(basePath);
            try
            {
                ExportVrmFile(component, buildContext, basePath, basePath);
            }
            catch (ValidationException e)
            {
                if (e.Extras != null)
                {
                    ErrorReport.ReportError(Translator.Instance, ErrorSeverity.NonFatal,
                        e.Key, e.Extras);
                }
                else
                {
                    Debug.Log(e.Message);
                }
            }
        }

        private static void RetrieveAllModularAvatarReactiveComponentsPass(BuildContext buildContext)
        {
#if NVE_HAS_MODULAR_AVATAR
            if (buildContext.AvatarRootObject.TryGetComponent<NdmfVrmExporterComponent>(out var ndmfVrmExporterComponent) &&
                !ndmfVrmExporterComponent.enableKhrMaterialsVariants)
            {
                Debug.Log("KHR_materials_variants will not be exported due to be disabled in VRM Exporter Description");
                return;
            }

            var variants = new List<MaterialVariant>();
            var installers = buildContext.AvatarRootObject.GetComponentsInChildren<ModularAvatarMenuInstaller>();
            foreach (var installer in installers)
            {
                if (!installer.enabled || !installer.TryGetComponent<ModularAvatarMenuItem>(out var menuItem))
                {
                    Debug.Log($"MA menu installer {installer.name} is not enabled or MA menu item is not attached");
                    continue;
                }

                if (!menuItem.enabled)
                {
                    Debug.Log($"MA menu item {menuItem.name} is not enabled");
                    continue;
                }

                if (menuItem.PortableControl.Type == PortableControlType.SubMenu)
                {
                    var name = menuItem.name;
                    foreach (Transform child in menuItem.transform)
                    {
                        if (child.TryGetComponent<ModularAvatarMaterialSwap>(out var materialSwap))
                        {
                            MapMaterialSwapToVariants(buildContext, materialSwap, name, ref variants);
                        }
                        else if (child.TryGetComponent<ModularAvatarMaterialSetter>(out var materialSetter))
                        {
                            MapMaterialSetterToVariants(materialSetter, name, ref variants);
                        }
                    }
                }
                else if (menuItem.TryGetComponent<ModularAvatarMaterialSwap>(out var materialSwap))
                {
                    MapMaterialSwapToVariants(buildContext, materialSwap, null, ref variants);
                }
                else if (menuItem.TryGetComponent<ModularAvatarMaterialSetter>(out var materialSetter))
                {
                    MapMaterialSetterToVariants(materialSetter, null, ref variants);
                }
            }

            buildContext.GetState<List<MaterialVariant>>().AddRange(variants);
        }

        private static void MapMaterialSwapToVariants(BuildContext buildContext, ModularAvatarMaterialSwap materialSwap,
            string? name,
            ref List<MaterialVariant> variants)
        {
            var reference = materialSwap.Root.Get(materialSwap);
            var renderers = (reference ?? buildContext.AvatarRootObject).GetComponentsInChildren<Renderer>();
            var sourceMappings = renderers.ToDictionary(renderer => renderer, renderer => renderer.sharedMaterials);
            var mappings = new Dictionary<Renderer, Material[]>();
            var variantName = materialSwap.gameObject.name;
            if (materialSwap.gameObject.TryGetComponent<ModularAvatarMenuItem>(out var menuItem))
            {
                variantName = menuItem.name;
            }

            foreach (var swap in materialSwap.Swaps)
            {
                var swapFrom = ObjectRegistry.GetReference(swap.From);
                foreach (var (sourceRenderer, sourceMaterials) in sourceMappings)
                {
                    var materialIndex = 0;
                    foreach (var sourceMaterial in sourceMaterials)
                    {
                        var sourceMaterialReference = ObjectRegistry.GetReference(sourceMaterial);
                        if (swapFrom?.Object == sourceMaterialReference?.Object)
                        {
                            if (mappings.TryGetValue(sourceRenderer, out var materials))
                            {
                                materials[materialIndex] = swap.To;
                            }
                            else
                            {
                                var newMaterials = new Material[sourceMaterials.Length];
                                newMaterials[materialIndex] = swap.To;
                                mappings.Add(sourceRenderer, newMaterials);
                            }
                        }

                        materialIndex++;
                    }
                }
            }

            variants.Add(new MaterialVariant
            {
                Name = !string.IsNullOrEmpty(name) ? $"{name}/{variantName}" : variantName,
                Mappings = mappings.Select(item => new MaterialVariantMapping
                    { Renderer = item.Key, Materials = item.Value.ToArray(), }).ToArray(),
            });
        }

        private static void MapMaterialSetterToVariants(ModularAvatarMaterialSetter materialSetter, string? name,
            ref List<MaterialVariant> variants)
        {
            var mappings = new Dictionary<Renderer, Material[]>();
            var variantName = materialSetter.gameObject.name;
            if (materialSetter.gameObject.TryGetComponent<ModularAvatarMenuItem>(out var menuItem))
            {
                variantName = menuItem.name;
            }

            foreach (var item in materialSetter.Objects)
            {
                var go = item.Object.Get(materialSetter);
                if (!go.TryGetComponent<Renderer>(out var renderer))
                {
                    Debug.Log($"{go} in MaterialSwitchObject has no renderer");
                    continue;
                }

                if (mappings.TryGetValue(renderer, out var materials))
                {
                    if (item.MaterialIndex < materials.Length)
                    {
                        materials[item.MaterialIndex] = item.Material;
                    }
                }
                else if (item.MaterialIndex < renderer.sharedMaterials.Length)
                {
                    var newMaterials = new Material[renderer.sharedMaterials.Length];
                    newMaterials[item.MaterialIndex] = item.Material;
                    mappings.Add(renderer, newMaterials);
                }
            }

            variants.Add(new MaterialVariant
            {
                Name = !string.IsNullOrEmpty(name) ? $"{name}/{variantName}" : variantName,
                Mappings = mappings.Select(item => new MaterialVariantMapping
                    { Renderer = item.Key, Materials = item.Value.ToArray(), }).ToArray(),
            });
#else
            Debug.Log("Modular Avatar (>= 1.13) is not installed and will be skipped");
#endif // NVE_HAS_MODULAR_AVATAR
        }

        private static void RetrieveAllLiCostumeChangerComponentsPass(BuildContext buildContext)
        {
#if NVE_HAS_LILYCAL_INVENTORY
            if (buildContext.AvatarRootObject.TryGetComponent<NdmfVrmExporterComponent>(out var ndmfVrmExporterComponent) &&
                !ndmfVrmExporterComponent.enableKhrMaterialsVariants)
            {
                Debug.Log("KHR_materials_variants will not be exported due to be disabled in VRM Exporter Description");
                return;
            }

            var costumeChangers = buildContext.AvatarRootObject.GetComponentsInChildren<CostumeChanger>();
            var costumeChangerType = typeof(CostumeChanger);
            var menuFolderType = typeof(MenuFolder);
            var menuItemNameChain = new List<string>();
            const BindingFlags bindingAttrPrivate = BindingFlags.NonPublic | BindingFlags.Instance;
            const BindingFlags bindingAttrPublic = BindingFlags.Public | BindingFlags.Instance;
            Type? costumeType = null;
            Type? materialReplacerType = null;
            Type? parametersPerMenuType = null;
            var variants = new List<MaterialVariant>();
            foreach (var costumeChanger in costumeChangers)
            {
                if (!costumeChanger.enabled)
                {
                    Debug.Log($"LI CostumeChanger {costumeChanger.name} is disabled and will be skipped");
                    continue;
                }

                var baseMenuName =
                    (string)costumeChangerType.GetField("menuName", bindingAttrPrivate)!
                        .GetValue(costumeChanger);
                var baseParentOverride =
                    (MenuFolder)costumeChangerType.GetField("parentOverride", bindingAttrPrivate)!.GetValue(
                        costumeChanger);
                var costumes =
                    (object[])costumeChangerType.GetField("costumes", bindingAttrPrivate)!.GetValue(
                        costumeChanger);
                foreach (var costume in costumes)
                {
                    costumeType ??= costume.GetType();
                    var parentOverride =
                        (MenuFolder)costumeType.GetField("parentOverride", bindingAttrPublic)!
                            .GetValue(costume);
                    if (!parentOverride)
                    {
                        parentOverride = baseParentOverride;
                    }

                    var menuIsEnabled = true;
                    menuItemNameChain.Clear();
                    while (parentOverride)
                    {
                        if (!parentOverride.enabled)
                        {
                            menuIsEnabled = false;
                            break;
                        }

                        var menuItemNameInner =
                            (string)menuFolderType.GetField("menuName", bindingAttrPrivate)!.GetValue(
                                parentOverride);
                        menuItemNameChain.Add(menuItemNameInner);
                        parentOverride =
                            (MenuFolder)menuFolderType.GetField("parentOverride", bindingAttrPrivate)!.GetValue(
                                parentOverride);
                    }

                    if (!menuIsEnabled)
                    {
                        Debug.Log($"LI MenuFolder {parentOverride.name} is disabled and will be skipped");
                        continue;
                    }

                    menuItemNameChain.Reverse();
                    var menuItemName =
                        (string)costumeType.GetField("menuName", bindingAttrPublic)!.GetValue(costume);
                    var parametersPerMenu =
                        costumeType.GetField("parametersPerMenu", bindingAttrPublic)!.GetValue(costume);
                    parametersPerMenuType ??= parametersPerMenu.GetType();
                    var materialReplaces =
                        (object[])parametersPerMenuType.GetField("materialReplacers", bindingAttrPublic)!
                            .GetValue(parametersPerMenu);
                    var mappings = new List<MaterialVariantMapping>();
                    foreach (var materialReplace in materialReplaces)
                    {
                        materialReplacerType ??= materialReplace.GetType();
                        var fields = materialReplacerType.GetFields();
                        var renderer = (Renderer)fields.First(item => item.Name == "renderer")
                            .GetValue(materialReplace);
                        var replaceTo = (Material[])fields.First(item => item.Name == "replaceTo")
                            .GetValue(materialReplace);
                        mappings.Add(new MaterialVariantMapping
                        {
                            Renderer = renderer,
                            Materials = replaceTo,
                        });
                    }

                    if (mappings.Count <= 0)
                    {
                        continue;
                    }

                    var menuItemNameChainInner = new List<string>(menuItemNameChain.AsReadOnly())
                    {
                        baseMenuName,
                        menuItemName
                    };
                    variants.Add(new MaterialVariant
                    {
                        Name = string.Join("/", menuItemNameChainInner),
                        Mappings = mappings.ToArray(),
                    });
                }
            }

            buildContext.GetState<List<MaterialVariant>>().AddRange(variants);
#else
            Debug.Log("lilycalInventory is not installed and will be skipped");
#endif // NVE_HAS_LILYCAL_INVENTORY
        }

        internal class ValidationException : Exception
        {
            public string Key { get; init; } = null!;
            public object? Extras { get; init; }

            public override string Message => Translator._(Key);
        }

        internal static void ExportVrmFile(NdmfVrmExporterComponent component, BuildContext buildContext,
            string baseOutputPath, string workingDirectoryPath)
        {
            if (!component.HasAuthor)
            {
                throw new ValidationException
                {
                    Key = "component.runtime.error.validation.author"
                };
            }

            if (!component.HasLicenseUrl)
            {
                throw new ValidationException
                {
                    Key = "component.runtime.error.validation.license-url"
                };
            }

            var corrupted = new List<SkinnedMeshRenderer>();
            CheckAllSkinnedMeshRenderers(buildContext.AvatarRootTransform, ref corrupted);
            if (corrupted.Count > 0)
            {
                throw new ValidationException
                {
                    Key = "component.runtime.error.validation.smr",
                    Extras = corrupted,
                };
            }

            var ro = buildContext.AvatarRootObject;
            var variants = buildContext.GetState<List<MaterialVariant>>().AsReadOnly();
            using var stream = new MemoryStream();
            using var exporter = new NdmfVrmExporter(ro, buildContext.AssetSaver, variants);
            var json = exporter.Export(stream);
            var bytes = stream.GetBuffer();
            File.WriteAllBytes($"{baseOutputPath}.vrm", bytes);
            if (component.enableGenerateJsonFile)
            {
                File.WriteAllText($"{baseOutputPath}.json", json);
            }

            if (!component.deleteTemporaryObjects)
            {
                return;
            }

            foreach (var item in new[] { AssetPathUtils.GetTempPath(ro), workingDirectoryPath })
            {
                try
                {
                    var info = new DirectoryInfo(item);
                    info.Delete(true);
                }
                catch (DirectoryNotFoundException)
                {
                    /* this is ignorable */
                }
            }
        }

        private static void CheckAllSkinnedMeshRenderers(Transform parent, ref List<SkinnedMeshRenderer> corrupted)
        {
            foreach (Transform child in parent)
            {
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (child.gameObject.TryGetComponent<SkinnedMeshRenderer>(out var smr) &&
                    smr.sharedMesh && IsSharedMeshCorrupted(smr))
                {
                    corrupted.Add(smr);
                }

                CheckAllSkinnedMeshRenderers(child, ref corrupted);
            }
        }

        private static bool IsSharedMeshCorrupted(SkinnedMeshRenderer smr)
        {
            var mesh = smr.sharedMesh;
            var bones = smr.bones;
            var numPositions = mesh.boneWeights.Length;
            var numSubMeshes = mesh.subMeshCount;
            var indexMapping = new Dictionary<uint, uint>();
            for (var meshIndex = 0; meshIndex < numSubMeshes; meshIndex++)
            {
                var indices = mesh.GetIndices(meshIndex).Select(index => (uint)index).ToArray();
                var indexSet = new HashSet<uint>(indices);
                indexMapping.Clear();
                for (uint i = 0; i < numPositions; i++)
                {
                    if (!indexSet.Contains(i))
                    {
                        continue;
                    }

                    var newIndex = (uint)indexMapping.Count;
                    indexMapping.Add(i, newIndex);
                }

                if (indices.Any(index => !indexMapping.TryGetValue(index, out _)))
                {
                    return true;
                }

                var boneWeights = mesh.boneWeights;
                if (boneWeights.Any(boneWeight => (boneWeight.weight0 > 0.0 && !bones[boneWeight.boneIndex0]) ||
                                                  (boneWeight.weight1 > 0.0 && !bones[boneWeight.boneIndex1]) ||
                                                  (boneWeight.weight2 > 0.0 && !bones[boneWeight.boneIndex2]) ||
                                                  (boneWeight.weight3 > 0.0 && !bones[boneWeight.boneIndex3])))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#endif // NVE_HAS_NDMF
