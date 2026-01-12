// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
#if NVE_HAS_VRCHAT_AVATAR_SDK
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
using Progress = UnityEditor.Progress;

namespace com.github.hkrn.ui
{
    internal static class NdmfVrmExportMenu
    {
        [MenuItem("Tools/NDMF VRM Exporter/Batch Generate Components")]
        private static void GenerateAll()
        {
            GenerateAllAsync(false).Forget();
        }

        [MenuItem("Tools/NDMF VRM Exporter/Batch Delete and Generate Components")]
        private static void RegenerateAll()
        {
            GenerateAllAsync(true).Forget();
        }

        [MenuItem("Tools/NDMF VRM Exporter/Batch Export")]
        private static void ExportAll()
        {
            var path = EditorUtility.SaveFolderPanel("Export VRM Directory", string.Empty, string.Empty);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Directory.CreateDirectory(path);
            ExportAllAsync(path);
        }

        private static async UniTaskVoid GenerateAllAsync(bool delete)
        {
            var source = new CancellationTokenSource();
            var currentScenePath = SceneManager.GetActiveScene().path;
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            var progressId = Progress.Start("Batch Generate VRM Export Description");
            var numScenes = 0;
            var totalScenes = sceneGuids.Length;
            foreach (var sceneGuid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                var scene = EditorSceneManager.OpenScene(scenePath);
                var avatars = Resources.FindObjectsOfTypeAll<VRCAvatarDescriptor>();
                foreach (var avatar in avatars)
                {
                    var go = avatar.gameObject;
                    if (!go.activeSelf)
                    {
                        Debug.Log($"Object is not active: name={go}");
                        continue;
                    }

                    if (!go.TryGetComponent<PipelineManager>(out var pipeline) ||
                        string.IsNullOrEmpty(pipeline.blueprintId))
                    {
                        Debug.Log($"Blueprint ID is not set and will be skipped: name={go}");
                        continue;
                    }

                    if (go.TryGetComponent<NdmfVrmExporterComponent>(out var innerComponent) && !delete)
                    {
                        Debug.Log($"NdmfVrmExporterComponent is already set: name={go}");
                        continue;
                    }
                    if (delete && innerComponent)
                    {
                        UnityEngine.Object.DestroyImmediate(innerComponent);
                    }

                    try
                    {
                        var component = go.AddComponent<NdmfVrmExporterComponent>();
                        var metadata = await NdmfVrmExporterComponentEditor.RetrieveAvatar(pipeline.blueprintId, source.Token);
                        component.enabled = false;
                        if (component.HasAuthor)
                        {
                            component.authors[0] = metadata.Author;
                        }
                        else
                        {
                            component.authors = new List<string> { metadata.Author };
                        }

                        component.copyrightInformation = metadata.CopyrightInformation;
                        component.contactInformation = metadata.ContactInformation;
                        component.version = metadata.Version;
                        if (!string.IsNullOrEmpty(metadata.OriginThumbnailPath))
                        {
                            component.thumbnail = NdmfVrmExporterComponentEditor.CreateSquareTrimmedThumbnail(metadata.OriginThumbnailPath);
                        }

                        void SetMmdExpression(VrmExpressionProperty prop, string expressionName, string blendShapeName)
                        {
                            prop.gameObject = go;
                            prop.expressionName = expressionName;
                            prop.blendShapeName = blendShapeName;
                            prop.overrideBlink = vrm.core.ExpressionOverrideType.Block;
                            prop.overrideLookAt = vrm.core.ExpressionOverrideType.Block;
                            prop.overrideMouth = vrm.core.ExpressionOverrideType.Block;
                            prop.isPreset = true;
                        }

                        var happy = new VrmExpressionProperty();
                        SetMmdExpression(happy, "Happy", "笑い");
                        component.expressionPresetHappyBlendShape = happy;
                        var angry = new VrmExpressionProperty();
                        SetMmdExpression(angry, "Angry", "怒り");
                        component.expressionPresetAngryBlendShape = angry;
                        var sad = new VrmExpressionProperty();
                        SetMmdExpression(sad, "Sad", "困る");
                        component.expressionPresetSadBlendShape = sad;
                        var relaxed = new VrmExpressionProperty();
                        SetMmdExpression(relaxed, "Relaxed", "なごみ");
                        component.expressionPresetRelaxedBlendShape = relaxed;
                        var surprised = new VrmExpressionProperty();
                        SetMmdExpression(surprised, "Surprised", "びっくり");
                        component.expressionPresetSurprisedBlendShape = surprised;

                        EditorUtility.SetDirty(go);
                        AssetDatabase.SaveAssetIfDirty(go);
                        Debug.Log($"NdmfVrmExporterComponent is added: name={go}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to set NdmfVrmExporterComponent: name={go}, exception={e}");
                    }
                }

                EditorSceneManager.SaveScene(scene);
                Progress.Report(progressId, numScenes / (float)totalScenes);
                numScenes++;
            }
            Progress.Finish(progressId);
            EditorSceneManager.OpenScene(currentScenePath);
        }

        private static void ExportAllAsync(string outputDirectoryPath)
        {
            var currentScenePath = SceneManager.GetActiveScene().path;
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            var progressId = Progress.Start("Batch Export VRM");
            var numScenes = 0;
            var totalScenes = sceneGuids.Length;
            foreach (var sceneGuid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                EditorSceneManager.OpenScene(scenePath);
                var avatars = Resources.FindObjectsOfTypeAll<NdmfVrmExporterComponent>();
                foreach (var avatar in avatars)
                {
                    var go = avatar.gameObject;
                    if (!go.activeSelf)
                    {
                        Debug.Log($"Object is not active: name={go}");
                        continue;
                    }

                    try
                    {
                        var platform = NdmfVrmExporterPlatform.Instance;
                        var avatarRoot = UnityEngine.Object.Instantiate(go);
                        avatarRoot.name = avatarRoot.name[..^"(clone)".Length];
                        using var scope = new AmbientPlatform.Scope(platform);
                        using var scope2 = new OverrideTemporaryDirectoryScope(null);
                        var buildContext = AvatarProcessor.ProcessAvatar(avatarRoot, platform);
                        var baseOutputPath = Path.Join( outputDirectoryPath, avatarRoot.name);
                        var workingDirectoryPath = AssetPathUtils.GetTempPath(avatarRoot);
                        NdmfVrmExporterPlugin.ExportVrmFile(avatar, buildContext, baseOutputPath,
                            workingDirectoryPath);
                        Debug.Log($"Exporting VRM file is succeeded: name={go}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to export: name={go}, exception={e}");
                    }
                    Progress.Report(progressId, numScenes / (float)totalScenes);
                    numScenes++;
                }
            }
            Progress.Finish(progressId);
            EditorSceneManager.OpenScene(currentScenePath);
        }
    }
}
#endif // NVE_HAS_VRCHAT_AVATAR_SDK
