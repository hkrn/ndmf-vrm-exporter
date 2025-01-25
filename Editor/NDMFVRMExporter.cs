// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

#if NVE_HAS_VRCHAT_AVATAR_SDK
using System.Net.Http;
using System.Threading;
using VRC.Core;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;
using VRC.SDKBase.Editor.Api;
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

#if NVE_HAS_AVATAR_OPTIMIZER
using Anatawa12.AvatarOptimizer.API;
#endif // NVE_HAS_AVATAR_OPTIMIZER

#if NVE_HAS_LILTOON
using lilToon;
#endif // NVE_HAS_LILTOON

#if NVE_HAS_NDMF
using nadena.dev.ndmf;
using nadena.dev.ndmf.localization;
using nadena.dev.ndmf.runtime;

[assembly: ExportsPlugin(typeof(com.github.hkrn.NdmfVrmExporterPlugin))]
#endif
// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    [AddComponentMenu("NDMF VRM Exporter/VRM Export Description")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/hkrn/ndmf-vrm-exporter")]
    public sealed class NdmfVrmExporterComponent : MonoBehaviour
#if NVE_HAS_VRCHAT_AVATAR_SDK
        , IEditorOnly
#endif // NVE_HAS_VRCHAT_AVATAR_SDK
    {
        [NotKeyable] [SerializeField] internal bool metadataFoldout = true;

        [NotKeyable] [SerializeField] internal List<string> authors = new();

        [NotKeyable] [SerializeField] internal string? version;

        [NotKeyable] [SerializeField] internal string? copyrightInformation;

        [NotKeyable] [SerializeField] internal string? contactInformation;

        [NotKeyable] [SerializeField] internal List<string> references = new();

        [NotKeyable] [SerializeField] internal bool enableContactInformationOnVRChatAutofill = true;

        [NotKeyable] [SerializeField] internal string licenseUrl = vrm.core.Meta.DefaultLicenseUrl;

        [NotKeyable] [SerializeField] internal string? thirdPartyLicenses;

        [NotKeyable] [SerializeField] internal string? otherLicenseUrl;

        [NotKeyable] [SerializeField] internal vrm.core.AvatarPermission avatarPermission;

        [NotKeyable] [SerializeField] internal vrm.core.CommercialUsage commercialUsage;

        [NotKeyable] [SerializeField] internal vrm.core.CreditNotation creditNotation;

        [NotKeyable] [SerializeField] internal vrm.core.Modification modification;

        [NotKeyable] [SerializeField] internal bool metadataAllowFoldout;

        [NotKeyable] [SerializeField] internal VrmUsagePermission allowExcessivelyViolentUsage;

        [NotKeyable] [SerializeField] internal VrmUsagePermission allowExcessivelySexualUsage;

        [NotKeyable] [SerializeField] internal VrmUsagePermission allowPoliticalOrReligiousUsage;

        [NotKeyable] [SerializeField] internal VrmUsagePermission allowAntisocialOrHateUsage;

        [NotKeyable] [SerializeField] internal VrmUsagePermission allowRedistribution;

        [NotKeyable] [SerializeField] internal Texture2D? thumbnail;

        [NotKeyable] [SerializeField] internal bool expressionFoldout = true;

        [NotKeyable] [SerializeField]
        internal VrmExpressionProperty expressionPresetHappyBlendShape = VrmExpressionProperty.Happy;

        [NotKeyable] [SerializeField]
        internal VrmExpressionProperty expressionPresetAngryBlendShape = VrmExpressionProperty.Angry;

        [NotKeyable] [SerializeField]
        internal VrmExpressionProperty expressionPresetSadBlendShape = VrmExpressionProperty.Sad;

        [NotKeyable] [SerializeField]
        internal VrmExpressionProperty expressionPresetRelaxedBlendShape = VrmExpressionProperty.Relaxed;

        [NotKeyable] [SerializeField]
        internal VrmExpressionProperty expressionPresetSurprisedBlendShape = VrmExpressionProperty.Surprised;

        [NotKeyable] [SerializeField] internal bool expressionCustomBlendShapeNameFoldout;

        [NotKeyable] [SerializeField] internal List<VrmExpressionProperty> expressionCustomBlendShapes = new();

        [NotKeyable] [SerializeField] internal bool springBoneFoldout;

        [NotKeyable] [SerializeField] internal List<Transform> excludedSpringBoneColliderTransforms = new();

        [NotKeyable] [SerializeField] internal List<Transform> excludedSpringBoneTransforms = new();

        [NotKeyable] [SerializeField] internal bool constraintFoldout;

        [NotKeyable] [SerializeField] internal List<Transform> excludedConstraintTransforms = new();

        [NotKeyable] [SerializeField] internal bool mtoonFoldout;

        [NotKeyable] [SerializeField] internal bool enableMToonRimLight;

        [NotKeyable] [SerializeField] internal bool enableMToonMatCap;

        [NotKeyable] [SerializeField] internal bool enableMToonOutline = true;

        [NotKeyable] [SerializeField] internal bool enableBakingAlphaMaskTexture = true;

        [NotKeyable] [SerializeField] internal bool debugFoldout;

        [NotKeyable] [SerializeField] internal bool makeAllNodeNamesUnique = true;

        [NotKeyable] [SerializeField] internal bool enableVertexColorOutput = true;

        [NotKeyable] [SerializeField] internal bool disableVertexColorOnLiltoon = true;

        [NotKeyable] [SerializeField] internal bool enableGenerateJsonFile;

        [NotKeyable] [SerializeField] internal bool deleteTemporaryObjects = true;

        [NotKeyable] [SerializeField] internal string? ktxToolPath;

        [NotKeyable] [SerializeField] internal int metadataModeSelection;

        [NotKeyable] [SerializeField] internal int expressionModeSelection;

        public bool HasAuthor => authors.Count > 0 && !string.IsNullOrWhiteSpace(authors.First());

        public bool HasLicenseUrl =>
            !string.IsNullOrWhiteSpace(licenseUrl) && Uri.TryCreate(licenseUrl, UriKind.Absolute, out _);

        public bool HasAvatarRoot => RuntimeUtil.IsAvatarRoot(gameObject.transform);

        // ReSharper disable once Unity.RedundantEventFunction
        private void Start()
        {
            /*  do nothing to show checkbox */
        }
    }

    [CustomEditor(typeof(NdmfVrmExporterComponent))]
    public sealed class NdmfVrmExporterComponentEditor : Editor
    {
        private SerializedProperty _metadataFoldoutProp = null!;
        private SerializedProperty _authorsProp = null!;
        private SerializedProperty _versionProp = null!;
        private SerializedProperty _copyrightInformationProp = null!;
        private SerializedProperty _contactInformationProp = null!;
        private SerializedProperty _referencesProp = null!;
        private SerializedProperty _enableContactInformationOnVRChatAutofillProp = null!;
        private SerializedProperty _licenseUrlProp = null!;
        private SerializedProperty _thirdPartyLicensesProp = null!;
        private SerializedProperty _otherLicenseUrlProp = null!;
        private SerializedProperty _avatarPermissionProp = null!;
        private SerializedProperty _commercialUsageProp = null!;
        private SerializedProperty _creditNotationProp = null!;
        private SerializedProperty _modificationProp = null!;
        private SerializedProperty _metadataAllowFoldoutProp = null!;
        private SerializedProperty _allowExcessivelyViolentUsageProp = null!;
        private SerializedProperty _allowExcessivelySexualUsageProp = null!;
        private SerializedProperty _allowPoliticalOrReligiousUsageProp = null!;
        private SerializedProperty _allowAntisocialOrHateUsageProp = null!;
        private SerializedProperty _allowRedistributionProp = null!;
        private SerializedProperty _thumbnailProp = null!;
        private SerializedProperty _expressionFoldoutProp = null!;
        private SerializedProperty _expressionPresetHappyBlendShape = null!;
        private SerializedProperty _expressionPresetAngryBlendShape = null!;
        private SerializedProperty _expressionPresetSadBlendShape = null!;
        private SerializedProperty _expressionPresetRelaxedBlendShape = null!;
        private SerializedProperty _expressionPresetSurprisedBlendShape = null!;
        private SerializedProperty _expressionCustomBlendShapes = null!;
        private SerializedProperty _springBoneFoldoutProp = null!;
        private SerializedProperty _excludedSpringBoneColliderTransformsProp = null!;
        private SerializedProperty _excludedSpringBoneTransformsProp = null!;
        private SerializedProperty _constraintFoldoutProp = null!;
        private SerializedProperty _excludedConstraintTransformsProp = null!;
        private SerializedProperty _mtoonFoldoutProp = null!;
        private SerializedProperty _enableMToonRimLightProp = null!;
        private SerializedProperty _enableMToonMatCapProp = null!;
        private SerializedProperty _enableMToonOutlineProp = null!;
        private SerializedProperty _enableBakingAlphaMaskProp = null!;
        private SerializedProperty _debugFoldoutProp = null!;
        private SerializedProperty _makeAllNodeNamesUniqueProp = null!;
        private SerializedProperty _enableVertexColorOutputProp = null!;
        private SerializedProperty _disableVertexColorOnLiltoonProp = null!;
        private SerializedProperty _enableGenerateJsonFileProp = null!;
        private SerializedProperty _deleteTemporaryObjectsProp = null!;
        private SerializedProperty _ktxToolPathProp = null!;
        private SerializedProperty _metadataModeSelection = null!;
        private SerializedProperty _expressionModeSelection = null!;
#if NVE_HAS_VRCHAT_AVATAR_SDK
        private struct VRChatAvatarToMetadata
        {
            public string Author { get; init; }
            public string CopyrightInformation { get; init; }
            public string ContactInformation { get; init; }
            public string Version { get; init; }
            public string OriginThumbnailPath { get; init; }
        };

        private Task<VRChatAvatarToMetadata>? _retrieveAvatarTask;
        private CancellationTokenSource _cancellationTokenSource = new();
        private string? _retrieveAvatarTaskErrorMessage;
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

        private void OnEnable()
        {
            _metadataFoldoutProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.metadataFoldout));
            _authorsProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.authors));
            _versionProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.version));
            _copyrightInformationProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.copyrightInformation));
            _contactInformationProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.contactInformation));
            _referencesProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.references));
            _enableContactInformationOnVRChatAutofillProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent
                    .enableContactInformationOnVRChatAutofill));
            _licenseUrlProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.licenseUrl));
            _thirdPartyLicensesProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.thirdPartyLicenses));
            _otherLicenseUrlProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.otherLicenseUrl));
            _avatarPermissionProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.avatarPermission));
            _commercialUsageProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.commercialUsage));
            _creditNotationProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.creditNotation));
            _modificationProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.modification));
            _metadataAllowFoldoutProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.metadataAllowFoldout));
            _allowExcessivelyViolentUsageProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.allowExcessivelyViolentUsage));
            _allowExcessivelySexualUsageProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.allowExcessivelySexualUsage));
            _allowPoliticalOrReligiousUsageProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.allowPoliticalOrReligiousUsage));
            _allowAntisocialOrHateUsageProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.allowAntisocialOrHateUsage));
            _allowRedistributionProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.allowRedistribution));
            _thumbnailProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.thumbnail));
            _expressionFoldoutProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.expressionFoldout));
            _expressionPresetHappyBlendShape =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.expressionPresetHappyBlendShape));
            _expressionPresetAngryBlendShape =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.expressionPresetAngryBlendShape));
            _expressionPresetSadBlendShape =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.expressionPresetSadBlendShape));
            _expressionPresetRelaxedBlendShape =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.expressionPresetRelaxedBlendShape));
            _expressionPresetSurprisedBlendShape =
                serializedObject.FindProperty(
                    nameof(NdmfVrmExporterComponent.expressionPresetSurprisedBlendShape));
            _expressionCustomBlendShapes =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.expressionCustomBlendShapes));
            _springBoneFoldoutProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.springBoneFoldout));
            _excludedSpringBoneColliderTransformsProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.excludedSpringBoneColliderTransforms));
            _excludedSpringBoneTransformsProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.excludedSpringBoneTransforms));
            _constraintFoldoutProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.constraintFoldout));
            _excludedConstraintTransformsProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.excludedConstraintTransforms));
            _mtoonFoldoutProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.mtoonFoldout));
            _enableMToonRimLightProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.enableMToonRimLight));
            _enableMToonMatCapProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.enableMToonMatCap));
            _enableMToonOutlineProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.enableMToonOutline));
            _enableBakingAlphaMaskProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.enableBakingAlphaMaskTexture));
            _debugFoldoutProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.debugFoldout));
            _makeAllNodeNamesUniqueProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.makeAllNodeNamesUnique));
            _enableVertexColorOutputProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.enableVertexColorOutput));
            _disableVertexColorOnLiltoonProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.disableVertexColorOnLiltoon));
            _enableGenerateJsonFileProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.enableGenerateJsonFile));
            _deleteTemporaryObjectsProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.deleteTemporaryObjects));
            _ktxToolPathProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.ktxToolPath));
            _metadataModeSelection =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.metadataModeSelection));
            _expressionModeSelection =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.expressionModeSelection));
            var component = (NdmfVrmExporterComponent)target;
            component.expressionPresetHappyBlendShape.gameObject = component.gameObject;
            component.expressionPresetAngryBlendShape.gameObject = component.gameObject;
            component.expressionPresetSadBlendShape.gameObject = component.gameObject;
            component.expressionPresetRelaxedBlendShape.gameObject = component.gameObject;
            component.expressionPresetSurprisedBlendShape.gameObject = component.gameObject;
            foreach (var property in component.expressionCustomBlendShapes)
            {
                property.gameObject = component.gameObject;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            var component = (NdmfVrmExporterComponent)target;
            if (component.enabled)
            {
                if (!component.HasAvatarRoot)
                {
                    EditorGUILayout.HelpBox(Translator._("component.validation.avatar-root"),
                        MessageType.Error);
                }
                else if (!component.HasAuthor)
                {
                    EditorGUILayout.HelpBox(Translator._("component.validation.author"),
                        MessageType.Error);
                }
                else if (!component.HasLicenseUrl)
                {
                    EditorGUILayout.HelpBox(Translator._("component.validation.license-url"),
                        MessageType.Error);
                }
                else
                {
                    var outputPath = $"{AssetPathUtils.GetOutputPath(component.gameObject)}.vrm";
                    EditorGUILayout.HelpBox(string.Format(Translator._("component.output.path"), outputPath),
                        MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(Translator._("component.validation.not-enabled"),
                    MessageType.Warning);
            }

#if NVE_HAS_VRCHAT_AVATAR_SDK
            if (_retrieveAvatarTask != null && _retrieveAvatarTask!.IsCompleted)
            {
                if (_retrieveAvatarTask.Exception != null)
                {
                    foreach (var exception in _retrieveAvatarTask.Exception.InnerExceptions)
                    {
                        if (exception is not ApiErrorException ae)
                        {
                            continue;
                        }

                        if (ae.StatusCode != HttpStatusCode.Unauthorized)
                        {
                            continue;
                        }

                        _retrieveAvatarTaskErrorMessage =
                            Translator._("component.metadata.information.vrchat.error.authorization");
                        break;
                    }

                    Debug.LogError($"Failed to retrieve metadata via VRChat: {_retrieveAvatarTask.Exception}");
                }
                else if (_retrieveAvatarTask.IsCanceled)
                {
                    Debug.Log("Cancelled to retrieve metadata via VRChat");
                }
                else
                {
                    var result = _retrieveAvatarTask.Result;
                    if (component.HasAuthor)
                    {
                        component.authors[0] = result.Author;
                    }
                    else
                    {
                        component.authors = new List<string> { result.Author };
                    }

                    component.copyrightInformation = result.CopyrightInformation;
                    if (_enableContactInformationOnVRChatAutofillProp.boolValue)
                    {
                        component.contactInformation = result.ContactInformation;
                    }
                    component.version = result.Version;
                    if (!string.IsNullOrEmpty(result.OriginThumbnailPath))
                    {
                        var baseTexture = new Texture2D(2, 2);
                        baseTexture.LoadImage(File.ReadAllBytes(result.OriginThumbnailPath));
                        var lowerSize = Mathf.Min(baseTexture.width, baseTexture.height);
                        var intermediateTexture = new Texture2D(lowerSize, lowerSize, baseTexture.format, false);
                        var srcX = Math.Max(baseTexture.width - lowerSize, 0) / 2;
                        var srcY = Math.Max(baseTexture.height - lowerSize, 0) / 2;
                        Graphics.CopyTexture(baseTexture, 0, 0, srcX, srcY, lowerSize, lowerSize, intermediateTexture,
                            0, 0, 0, 0);
                        var filePath = result.OriginThumbnailPath.Replace(".origin.png", ".png");
                        var destTexture = intermediateTexture.Blit(1024, 1024, TextureFormat.ARGB32, ColorSpace.Gamma);
                        var bytes = destTexture.EncodeToPNG();
                        DestroyImmediate(destTexture);
                        File.WriteAllBytes(filePath, bytes);
                        AssetDatabase.ImportAsset(filePath);
                        component.thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
                    }

                    Debug.Log("Succeeded to retrieve metadata via VRChat");
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _retrieveAvatarTask = null;
            }
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

            var thumbnail = _thumbnailProp.objectReferenceValue as Texture2D;
            if (thumbnail && thumbnail is not null && thumbnail.width != thumbnail.height)
            {
                EditorGUILayout.HelpBox(Translator._("component.validation.avatar-thumbnail"), MessageType.Warning);
            }

            var metadataFoldout = EditorGUILayout.Foldout(_metadataFoldoutProp.boolValue,
                Translator._("component.category.metadata"));
            if (metadataFoldout)
            {
                DrawMetadata();
            }

            EditorGUILayout.Separator();
            var expressionsFoldout = EditorGUILayout.Foldout(_expressionFoldoutProp.boolValue,
                Translator._("component.category.expressions"));
            if (expressionsFoldout)
            {
                DrawExpressions();
            }

            EditorGUILayout.Separator();
            var mtoonFoldout =
                EditorGUILayout.Foldout(_mtoonFoldoutProp.boolValue, Translator._("component.category.mtoon"));
            if (mtoonFoldout)
            {
                DrawMToonOptions();
            }

            EditorGUILayout.Separator();
            var springBoneFoldout = EditorGUILayout.Foldout(_springBoneFoldoutProp.boolValue,
                Translator._("component.category.spring-bone"));
            if (springBoneFoldout)
            {
                DrawSpringBoneOptions();
            }

            EditorGUILayout.Separator();
            var constraintFoldout = EditorGUILayout.Foldout(_constraintFoldoutProp.boolValue,
                Translator._("component.category.constraint"));
            if (constraintFoldout)
            {
                DrawConstraintOptions();
            }

            EditorGUILayout.Separator();
            var debugFoldout =
                EditorGUILayout.Foldout(_debugFoldoutProp.boolValue, Translator._("component.category.debug"));
            if (debugFoldout)
            {
                DrawDebugOptions();
            }

            _metadataFoldoutProp.boolValue = metadataFoldout;
            _expressionFoldoutProp.boolValue = expressionsFoldout;
            _mtoonFoldoutProp.boolValue = mtoonFoldout;
            _springBoneFoldoutProp.boolValue = springBoneFoldout;
            _constraintFoldoutProp.boolValue = constraintFoldout;
            _debugFoldoutProp.boolValue = debugFoldout;
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMetadata()
        {
            var value = GUILayout.Toolbar(_metadataModeSelection.intValue, new[]
            {
                Translator._("component.metadata.information"),
                Translator._("component.metadata.licenses"),
                Translator._("component.metadata.permissions"),
            });
            switch (value)
            {
                case 0:
                {
                    DrawInformation();
                    break;
                }
                case 1:
                {
                    DrawLicenses();
                    break;
                }
                case 2:
                {
                    DrawPermissions();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _metadataModeSelection.intValue = value;
        }

        private void DrawInformation()
        {
            DrawPropertyField(Translator._("component.metadata.information.avatar-thumbnail"), _thumbnailProp);
            EditorGUILayout.PropertyField(_authorsProp,
                new GUIContent(Translator._("component.metadata.information.authors")));
            DrawPropertyField(Translator._("component.metadata.information.version"), _versionProp);
            DrawPropertyField(Translator._("component.metadata.information.copyright-information"),
                _copyrightInformationProp);
            DrawPropertyField(Translator._("component.metadata.information.contact-information"),
                _contactInformationProp);
            EditorGUILayout.PropertyField(_referencesProp,
                new GUIContent(Translator._("component.metadata.information.references")));
#if NVE_HAS_VRCHAT_AVATAR_SDK
            var component = (NdmfVrmExporterComponent)target;
            if (!component.transform.TryGetComponent<PipelineManager>(out var pipelineManager) ||
                string.IsNullOrEmpty(pipelineManager.blueprintId))
                return;
            EditorGUILayout.Separator();
            if (_retrieveAvatarTask != null)
            {
                if (GUILayout.Button(Translator._("component.metadata.information.vrchat.cancel")))
                {
                    _cancellationTokenSource.Cancel();
                    Debug.Log("Requested to cancel retrieving metadata task");
                }
            }
            else if (GUILayout.Button(Translator._("component.metadata.information.vrchat.retrieve")))
            {
                async Task<VRChatAvatarToMetadata> RetrieveAvatarTask(string blueprintId, CancellationToken token)
                {
                    var packageJsonFile =
                        await File.ReadAllTextAsync($"Packages/{NdmfVrmExporter.PackageJson.Name}/package.json", token);
                    var packageJson = NdmfVrmExporter.PackageJson.LoadFromString(packageJsonFile);
                    var avatar = await VRCApi.GetAvatar(blueprintId, cancellationToken: token);
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd($"{packageJson.DisplayName}/{packageJson.Version}");
                    var result = await client.GetAsync(avatar.ImageUrl, token);
                    var filePath = string.Empty;
                    if (result.IsSuccessStatusCode)
                    {
                        var bytes = await result.Content.ReadAsByteArrayAsync();
                        var basePath = $"{AssetPathUtils.BasePath}/VRChatSDKAvatarThumbnails";
                        Directory.CreateDirectory(basePath);
                        filePath = $"{basePath}/{avatar.ID}.origin.png";
                        await File.WriteAllBytesAsync(filePath, bytes, token);
                    }
                    else
                    {
                        Debug.LogWarning($"Cannot retrieve thumbnail: {result.ReasonPhrase}");
                    }

                    return new VRChatAvatarToMetadata
                    {
                        Author = avatar.AuthorName,
                        CopyrightInformation = $"Copyright \u00a9 {avatar.CreatedAt.Year} {avatar.AuthorName}",
                        ContactInformation = $"https://vrchat.com/home/user/{avatar.AuthorId}",
                        Version =
                            $"{avatar.UpdatedAt.Year}.{avatar.UpdatedAt.Month}.{avatar.UpdatedAt.Day}+{avatar.Version}",
                        OriginThumbnailPath = filePath,
                    };
                }

                _retrieveAvatarTaskErrorMessage = null;
                _retrieveAvatarTask = RetrieveAvatarTask(pipelineManager.blueprintId, _cancellationTokenSource.Token);
                Debug.Log("Start to retrieve metadata via VRChat API");
            }
            EditorGUILayout.Space();
            DrawToggleLeft(Translator._("component.metadata.information.vrchat.enable-contact-information"), _enableContactInformationOnVRChatAutofillProp);

            if (!string.IsNullOrEmpty(_retrieveAvatarTaskErrorMessage))
            {
                EditorGUILayout.HelpBox(_retrieveAvatarTaskErrorMessage, MessageType.Error);
            }

#endif // NVE_HAS_VRCHAT_AVATAR_SDK
        }

        private void DrawLicenses()
        {
            DrawPropertyField(Translator._("component.metadata.licenses.license-url"), _licenseUrlProp);
            DrawPropertyField(Translator._("component.metadata.licenses.third-party-license"), _thirdPartyLicensesProp);
            DrawPropertyField(Translator._("component.metadata.licenses.other-license-url"), _otherLicenseUrlProp);
        }

        private void DrawPermissions()
        {
            DrawPropertyField(Translator._("component.metadata.permissions.avatar-permission"), _avatarPermissionProp);
            DrawPropertyField(Translator._("component.metadata.permissions.commercial-usage"), _commercialUsageProp);
            DrawPropertyField(Translator._("component.metadata.permissions.credit-notation"), _creditNotationProp);
            DrawPropertyField(Translator._("component.metadata.permissions.modification"), _modificationProp);
            var foldout = EditorGUILayout.Foldout(_metadataAllowFoldoutProp.boolValue,
                Translator._("component.metadata.permissions.allow.header"));
            if (foldout)
            {
                DrawPropertyField(Translator._("component.metadata.permissions.allow.redistribution"),
                    _allowRedistributionProp);
                DrawPropertyField(Translator._("component.metadata.permissions.allow.excessively-violent-usage"),
                    _allowExcessivelyViolentUsageProp);
                DrawPropertyField(Translator._("component.metadata.permissions.allow.excessively-sexual-usage"),
                    _allowExcessivelySexualUsageProp);
                DrawPropertyField(Translator._("component.metadata.permissions.allow.political-or-religious-usage"),
                    _allowPoliticalOrReligiousUsageProp);
                DrawPropertyField(Translator._("component.metadata.permissions.allow.antisocial-or-hate-usage"),
                    _allowAntisocialOrHateUsageProp);
            }

            _metadataAllowFoldoutProp.boolValue = foldout;
        }

        private void DrawExpressions()
        {
            var value = GUILayout.Toolbar(_expressionModeSelection.intValue, new[]
            {
                Translator._("component.expression.preset"),
                Translator._("component.expression.custom"),
            });
            switch (value)
            {
                case 0:
                {
                    DrawPresetExpressions();
                    break;
                }
                case 1:
                {
                    DrawCustomExpression();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _expressionModeSelection.intValue = value;
        }

        private void DrawPresetExpressions()
        {
            {
                using var _ = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
                DrawPropertyField(Translator._("component.expression.preset.happy"), _expressionPresetHappyBlendShape);
            }
            {
                using var _ = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
                DrawPropertyField(Translator._("component.expression.preset.angry"), _expressionPresetAngryBlendShape);
            }
            {
                using var _ = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
                DrawPropertyField(Translator._("component.expression.preset.sad"), _expressionPresetSadBlendShape);
            }
            {
                using var _ = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
                DrawPropertyField(Translator._("component.expression.preset.relaxed"),
                    _expressionPresetRelaxedBlendShape);
            }
            {
                using var _ = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
                DrawPropertyField(Translator._("component.expression.preset.surprised"),
                    _expressionPresetSurprisedBlendShape);
            }
            EditorGUILayout.Separator();
            if (GUILayout.Button(Translator._("component.expression.preset.from-mmd")))
            {
                void SetFromMmdExpression(string targetBlendShapeName, SerializedProperty prop)
                {
                    var option = (VrmExpressionProperty)prop.boxedValue;
                    option.SetFromMmdExpression(targetBlendShapeName);
                    prop.boxedValue = option;
                }

                SetFromMmdExpression("笑い", _expressionPresetHappyBlendShape);
                SetFromMmdExpression("怒り", _expressionPresetAngryBlendShape);
                SetFromMmdExpression("困る", _expressionPresetSadBlendShape);
                SetFromMmdExpression("なごみ", _expressionPresetRelaxedBlendShape);
                SetFromMmdExpression("びっくり", _expressionPresetSurprisedBlendShape);
            }

            EditorGUILayout.Separator();
            if (GUILayout.Button(Translator._("component.expression.preset.reset-all")))
            {
                var gameObject = ((NdmfVrmExporterComponent)target).gameObject;

                VrmExpressionProperty WrapVrmExpression(VrmExpressionProperty property)
                {
                    property.gameObject = gameObject;
                    return property;
                }

                _expressionPresetHappyBlendShape.boxedValue = WrapVrmExpression(VrmExpressionProperty.Happy);
                _expressionPresetAngryBlendShape.boxedValue = WrapVrmExpression(VrmExpressionProperty.Angry);
                _expressionPresetSadBlendShape.boxedValue = WrapVrmExpression(VrmExpressionProperty.Sad);
                _expressionPresetRelaxedBlendShape.boxedValue = WrapVrmExpression(VrmExpressionProperty.Relaxed);
                _expressionPresetSurprisedBlendShape.boxedValue = WrapVrmExpression(VrmExpressionProperty.Surprised);
            }
        }

        private void DrawCustomExpression()
        {
            var go = (NdmfVrmExporterComponent)target;
            foreach (var property in go.expressionCustomBlendShapes)
            {
                property.gameObject = go.gameObject;
            }

            EditorGUILayout.PropertyField(_expressionCustomBlendShapes, GUIContent.none);
        }

        private void DrawMToonOptions()
        {
            DrawToggleLeft(Translator._("component.mtoon.enable.rim-light"), _enableMToonRimLightProp);
            DrawToggleLeft(Translator._("component.mtoon.enable.mat-cap"), _enableMToonMatCapProp);
            DrawToggleLeft(Translator._("component.mtoon.enable.outline"), _enableMToonOutlineProp);
            DrawToggleLeft(Translator._("component.mtoon.enable.bake-alpha-mask"), _enableBakingAlphaMaskProp);
        }

        private void DrawSpringBoneOptions()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(
                _excludedSpringBoneColliderTransformsProp,
                new GUIContent(Translator._("component.spring-bone.exclude.colliders")));
            EditorGUILayout.PropertyField(_excludedSpringBoneTransformsProp,
                new GUIContent(Translator._("component.spring-bone.exclude.bones")));
            EditorGUI.indentLevel--;
        }

        private void DrawConstraintOptions()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(
                _excludedConstraintTransformsProp,
                new GUIContent(Translator._("component.constraint.exclude.constraints")));
            EditorGUI.indentLevel--;
        }

        private void DrawDebugOptions()
        {
            DrawToggleLeft(Translator._("component.debug.make-all-node-names-unique"), _makeAllNodeNamesUniqueProp);
            DrawToggleLeft(Translator._("component.debug.enable-vertex-color-output"), _enableVertexColorOutputProp);
            DrawToggleLeft(Translator._("component.debug.disable-vertex-color-on-liltoon"),
                _disableVertexColorOnLiltoonProp);
            DrawToggleLeft(Translator._("component.debug.enable-generate-json"), _enableGenerateJsonFileProp);
            DrawToggleLeft(Translator._("component.debug.delete-temporary-files"), _deleteTemporaryObjectsProp);
            DrawPropertyField(Translator._("component.debug.ktx-tool-path"), _ktxToolPathProp);
        }

        private static void DrawPropertyField(string label, SerializedProperty prop)
        {
            GUILayout.Label(label);
            EditorGUILayout.PropertyField(prop, GUIContent.none);
        }

        private static void DrawToggleLeft(string label, SerializedProperty prop)
        {
            EditorGUI.BeginChangeCheck();
            var value = EditorGUILayout.ToggleLeft(label, prop.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                prop.boolValue = value;
            }
        }
    }

    public enum VrmUsagePermission
    {
        Disallow,
        Allow,
    }

    [Serializable]
    public sealed class VrmExpressionProperty
    {
        internal static VrmExpressionProperty Happy => new()
        {
            expressionName = "Happy",
            isPreset = true,
        };

        internal static VrmExpressionProperty Angry => new()
        {
            expressionName = "Angry",
            isPreset = true,
        };

        internal static VrmExpressionProperty Sad => new()
        {
            expressionName = "Sad",
            isPreset = true,
        };

        internal static VrmExpressionProperty Relaxed => new()
        {
            expressionName = "Relaxed",
            isPreset = true,
        };

        internal static VrmExpressionProperty Surprised => new()
        {
            expressionName = "Surprised",
            isPreset = true,
        };

        [NotKeyable] [SerializeField] internal string? expressionName;
        [NotKeyable] [SerializeField] internal BaseType baseType;
        [NotKeyable] [SerializeField] internal GameObject? gameObject;
        [NotKeyable] [SerializeField] internal string? blendShapeName;
        [NotKeyable] [SerializeField] internal AnimationClip? blendShapeAnimationClip;
        [NotKeyable] [SerializeField] internal bool optionsFoldout;
        [NotKeyable] [SerializeField] internal vrm.core.ExpressionOverrideType overrideBlink;
        [NotKeyable] [SerializeField] internal vrm.core.ExpressionOverrideType overrideLookAt;
        [NotKeyable] [SerializeField] internal vrm.core.ExpressionOverrideType overrideMouth;
        [NotKeyable] [SerializeField] internal bool isBinary;
        [NotKeyable] [SerializeField] internal bool isPreset;

        internal const string BlendShapeNamePrefix = "blendShape.";

        internal enum BaseType
        {
            BlendShape,
            AnimationClip,
        };

        internal List<string?> BlendShapeNames => baseType switch
        {
            BaseType.AnimationClip => ExtractAllBlendShapeNamesFromAnimationClip(),
            BaseType.BlendShape => new List<string?> { blendShapeName },
            _ => throw new ArgumentOutOfRangeException(),
        };

        internal string CanonicalExpressionName
        {
            get
            {
                if (!string.IsNullOrEmpty(expressionName))
                {
                    return expressionName!;
                }

                return baseType switch
                {
                    BaseType.AnimationClip => blendShapeAnimationClip!.name,
                    BaseType.BlendShape => blendShapeName!,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
        }

        internal bool IsValid => baseType switch
        {
            BaseType.AnimationClip => blendShapeAnimationClip != null,
            BaseType.BlendShape => !string.IsNullOrEmpty(blendShapeName),
            _ => throw new ArgumentOutOfRangeException(),
        };

        internal void SetFromMmdExpression(string targetName)
        {
            var blendShapeNames = new List<string>();
            if (!gameObject || !gameObject!.transform)
            {
                return;
            }

            RetrieveAllBlendShapes(gameObject.transform, ref blendShapeNames);
            var index = blendShapeNames.IndexOf(targetName);
            if (index == -1)
                return;
            baseType = BaseType.BlendShape;
            blendShapeName = blendShapeNames[index];
            overrideBlink = vrm.core.ExpressionOverrideType.Block;
            overrideLookAt = vrm.core.ExpressionOverrideType.Block;
            overrideMouth = vrm.core.ExpressionOverrideType.Block;
        }

        internal static void RetrieveAllBlendShapes(Transform transform, ref List<string> blendShapeNames)
        {
            RetrieveAllBlendShapes(transform, (name, _, blendShapeNames) => { blendShapeNames.Add(name); },
                ref blendShapeNames);
        }

        internal static void RetrieveAllBlendShapes<T>(Transform transform,
            Action<string, SkinnedMeshRenderer, T> callback, ref T blendShapeNames)
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (!child || child is null || !child.TryGetComponent<SkinnedMeshRenderer>(out var smr))
                    continue;
                var mesh = smr.sharedMesh;
                if (mesh)
                {
                    var numBlendShapes = mesh.blendShapeCount;
                    for (var j = 0; j < numBlendShapes; j++)
                    {
                        callback(mesh.GetBlendShapeName(j), smr, blendShapeNames);
                    }
                }

                RetrieveAllBlendShapes(child, callback, ref blendShapeNames);
            }
        }

        private List<string?> ExtractAllBlendShapeNamesFromAnimationClip()
        {
            if (!blendShapeAnimationClip)
                return new List<string?>();
            return (from binding in AnimationUtility.GetCurveBindings(blendShapeAnimationClip)
                where binding.propertyName.StartsWith(BlendShapeNamePrefix)
                let name = binding.propertyName[BlendShapeNamePrefix.Length..]
                let curve = AnimationUtility.GetEditorCurve(blendShapeAnimationClip, binding)
                from keyframe in curve.keys
                where !(keyframe.time > 0.0f) && !Mathf.Approximately(keyframe.value, 0.0f)
                select name).ToList();
        }
    }

    [CustomPropertyDrawer(typeof(VrmExpressionProperty))]
    public sealed class VrmExpressionPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var fieldRect = position;
            fieldRect.height = LineHeight;

            var expressionName = property.FindPropertyRelative(nameof(VrmExpressionProperty.expressionName));
            if (!property.FindPropertyRelative(nameof(VrmExpressionProperty.isPreset)).boolValue)
            {
                expressionName.stringValue = EditorGUI.TextField(fieldRect, Translator._("component.expression.name"),
                    expressionName.stringValue);
                fieldRect.y += fieldRect.height;
            }

            var baseType = property.FindPropertyRelative(nameof(VrmExpressionProperty.baseType));
            baseType.intValue = EditorGUI.Popup(
                fieldRect,
                Translator._("component.expression.type"),
                baseType.intValue,
                new[]
                {
                    Translator._("component.expression.type.blend-shape"),
                    Translator._("component.expression.type.animation-clip")
                });
            fieldRect.y += fieldRect.height;

            switch ((VrmExpressionProperty.BaseType)baseType.intValue)
            {
                case VrmExpressionProperty.BaseType.BlendShape:
                {
                    var gameObjectProp = property.FindPropertyRelative(nameof(VrmExpressionProperty.gameObject));
                    var gameObject = (GameObject)gameObjectProp.objectReferenceValue;
                    if (_blendShapeNames.Count == 0 && gameObject)
                    {
                        VrmExpressionProperty.RetrieveAllBlendShapes(gameObject.transform, ref _blendShapeNames);
                    }

                    var blendShapeName = property.FindPropertyRelative(nameof(VrmExpressionProperty.blendShapeName));
                    var selectedIndex = blendShapeName.stringValue != null
                        ? _blendShapeNames.IndexOf(blendShapeName.stringValue)
                        : -1;
                    var newIndex = EditorGUI.Popup(fieldRect, selectedIndex, _blendShapeNames.ToArray());
                    if (newIndex >= 0 && newIndex < _blendShapeNames.Count)
                    {
                        var targetBlendShapeName = _blendShapeNames[newIndex];
                        blendShapeName.stringValue = targetBlendShapeName;
                    }

                    break;
                }
                case VrmExpressionProperty.BaseType.AnimationClip:
                {
                    var animationClipProp =
                        property.FindPropertyRelative(nameof(VrmExpressionProperty.blendShapeAnimationClip));
                    var animationClip = (AnimationClip)animationClipProp.objectReferenceValue;
                    animationClipProp.objectReferenceValue =
                        (AnimationClip)EditorGUI.ObjectField(fieldRect, animationClip, typeof(AnimationClip), false);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            fieldRect.y += fieldRect.height;

            EditorGUI.indentLevel++;
            var optionsFoldoutProp = property.FindPropertyRelative(nameof(VrmExpressionProperty.optionsFoldout));
            var foldout = EditorGUI.Foldout(fieldRect, optionsFoldoutProp.boolValue,
                Translator._("component.expression.property.header"));
            fieldRect.y += fieldRect.height;
            if (foldout)
            {
                var selections = new[]
                {
                    Translator._("component.expression.property.type.none"),
                    Translator._("component.expression.property.type.block"),
                    Translator._("component.expression.property.type.blend")
                };
                var overrideBlinkProp = property.FindPropertyRelative(nameof(VrmExpressionProperty.overrideBlink));
                overrideBlinkProp.intValue = EditorGUI.Popup(fieldRect,
                    Translator._("component.expression.property.override.blink"),
                    overrideBlinkProp.intValue,
                    selections);
                fieldRect.y += fieldRect.height;
                var overrideLookAtProp = property.FindPropertyRelative(nameof(VrmExpressionProperty.overrideLookAt));
                overrideLookAtProp.intValue = EditorGUI.Popup(fieldRect,
                    Translator._("component.expression.property.override.look-at"),
                    overrideLookAtProp.intValue, selections);
                fieldRect.y += fieldRect.height;
                var overrideMouthProp = property.FindPropertyRelative(nameof(VrmExpressionProperty.overrideMouth));
                overrideMouthProp.intValue = EditorGUI.Popup(fieldRect,
                    Translator._("component.expression.property.override.mouth"),
                    overrideMouthProp.intValue,
                    selections);
                fieldRect.y += fieldRect.height;
                var isBinaryProp = property.FindPropertyRelative(nameof(VrmExpressionProperty.isBinary));
                isBinaryProp.boolValue =
                    EditorGUI.Toggle(fieldRect, Translator._("component.expression.property.is-binary"),
                        isBinaryProp.boolValue);
            }

            EditorGUI.indentLevel--;

            optionsFoldoutProp.boolValue = foldout;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = LineHeight * 3;
            if (!property.FindPropertyRelative(nameof(VrmExpressionProperty.isPreset)).boolValue)
            {
                height += LineHeight;
            }

            if (property.FindPropertyRelative(nameof(VrmExpressionProperty.optionsFoldout)).boolValue)
            {
                height += LineHeight * 4;
            }

            return height;
        }

        private float LineHeight => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        private List<string> _blendShapeNames = new();
    }


    internal static class UnityExtensions
    {
        public static System.Numerics.Vector2 ToVector2(this Vector2 self)
        {
            return new System.Numerics.Vector2(self.x, self.y);
        }

        public static System.Numerics.Vector2 ToVector2WithCoordinateSpace(this Vector2 self)
        {
            return new System.Numerics.Vector2(self.x, 1.0f - self.y);
        }

        public static System.Numerics.Vector3 ToVector3(this Vector3 self)
        {
            return new System.Numerics.Vector3(self.x, self.y, self.z);
        }

        public static System.Numerics.Vector3 ToVector3WithCoordinateSpace(this Vector3 self)
        {
            return new System.Numerics.Vector3(-self.x, self.y, self.z);
        }

        public static System.Numerics.Vector4 ToVector4WithTangentSpace(this Vector4 self)
        {
            var v = (new Vector3(-self.x, self.y, self.z)).normalized;
            var w = Mathf.Sign(-self.w);
            return new System.Numerics.Vector4(v.x, v.y, v.z, w);
        }

        public static System.Numerics.Vector3 ToVector3(this Color self)
        {
            return new System.Numerics.Vector3(self.r, self.g, self.b);
        }

        public static System.Numerics.Vector3 ToVector3(this Color self, ColorSpace src, ColorSpace dst)
        {
            var color = ConvertColor(self, src, dst);
            return new System.Numerics.Vector3(color.r, color.g, color.b);
        }

        public static System.Numerics.Vector4 ToVector4(this Color self, ColorSpace src, ColorSpace dst)
        {
            var color = ConvertColor(self, src, dst);
            return new System.Numerics.Vector4(color.r, color.g, color.b, color.a);
        }

        public static System.Numerics.Quaternion ToQuaternionWithCoordinateSpace(this Quaternion self)
        {
            self.ToAngleAxis(out var angle, out var axis);
            var newAxis = axis.ToVector3WithCoordinateSpace();
            return System.Numerics.Quaternion.CreateFromAxisAngle(newAxis, -angle * Mathf.Deg2Rad);
        }

        private static System.Numerics.Matrix4x4 ToMatrix4(this Matrix4x4 self)
        {
            return new System.Numerics.Matrix4x4(
                self.m00, self.m01, self.m02, self.m03,
                self.m10, self.m11, self.m12, self.m13,
                self.m20, self.m21, self.m22, self.m23,
                self.m30, self.m31, self.m32, self.m33);
        }

        public static System.Numerics.Matrix4x4 ToNormalizedMatrix(this Matrix4x4 self)
        {
            var t = new Vector3(-self.m03, self.m13, self.m23);
            self.rotation.ToAngleAxis(out var angle, out var axis);
            var r = Quaternion.AngleAxis(-angle, new Vector3(-axis.x, axis.y, axis.z));
            var s = new Vector3
            {
                x = new Vector4(self.m00, self.m10, self.m20, self.m30).magnitude,
                y = new Vector4(self.m01, self.m11, self.m21, self.m31).magnitude,
                z = new Vector4(self.m02, self.m12, self.m22, self.m32).magnitude,
            };
            return Matrix4x4.TRS(t, r, s).ToMatrix4();
        }

        public static gltf.material.TextureFilterMode ToTextureFilterMode(this FilterMode self)
        {
            return self switch
            {
                FilterMode.Point => gltf.material.TextureFilterMode.Nearest,
                FilterMode.Bilinear => gltf.material.TextureFilterMode.Linear,
                FilterMode.Trilinear => gltf.material.TextureFilterMode.LinearMipmapLinear,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static gltf.material.TextureWrapMode ToTextureWrapMode(this TextureWrapMode self)
        {
            return self switch
            {
                TextureWrapMode.Clamp => gltf.material.TextureWrapMode.ClampToEdge,
                TextureWrapMode.Mirror => gltf.material.TextureWrapMode.MirroredRepeat,
                TextureWrapMode.Repeat => gltf.material.TextureWrapMode.Repeat,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        internal static Texture2D? Clone(this Texture sourceTexture)
        {
            if (sourceTexture is not Texture2D texture)
            {
                return null;
            }
            var newTexture = new Texture2D(texture.width, texture.height, texture.format, true);
            Graphics.CopyTexture(texture, newTexture);
            return newTexture;
        }

        internal static Texture2D Blit(this Texture sourceTexture, TextureFormat textureFormat, ColorSpace cs)
        {
            return Blit(sourceTexture, sourceTexture.width, sourceTexture.height, textureFormat, cs);
        }

        internal static Texture2D Blit(this Texture sourceTexture, int width, int height, TextureFormat textureFormat,
            ColorSpace cs)
        {
            var rw = cs == ColorSpace.Gamma ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;
            var activeRenderTexture = RenderTexture.active;
            var destRenderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, rw);
            Graphics.Blit(sourceTexture, destRenderTexture);
            RenderTexture.active = destRenderTexture;
            var outputTexture = new Texture2D(width, height, textureFormat, false, cs == ColorSpace.Linear)
            {
                name = sourceTexture.name
            };
            outputTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = activeRenderTexture;
            RenderTexture.ReleaseTemporary(destRenderTexture);
            return outputTexture;
        }

        private static Color ConvertColor(Color self, ColorSpace src, ColorSpace dst)
        {
            var color = (src, dst) switch
            {
                (ColorSpace.Gamma, ColorSpace.Linear) => self.linear,
                (ColorSpace.Linear, ColorSpace.Gamma) => self.gamma,
                _ => self
            };
            return color;
        }
    }

    internal static class Translator
    {
        internal static readonly Localizer Instance = new("en-us", () =>
        {
            var path = AssetDatabase.GUIDToAssetPath("9d1da1be88b74198955d08e38cddf4b4");
            var english = AssetDatabase.LoadAssetAtPath<LocalizationAsset>($"{path}/en-us.po");
            return new List<LocalizationAsset> { english };
        });

        public static string _(string key) => Instance.GetLocalizedString(key);
    }

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

            SetMaterialTexture(assetSaver, mainTex, hsvgMaterial, PropertyMainTex, ref srcTexture);
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

                SetMaterialTexture(assetSaver, dictionaries.GetTexture("_Main2ndTex"), hsvgMaterial,
                    PropertyMainSecondTex, ref srcMain2);
                SetMaterialTexture(assetSaver, dictionaries.GetTexture("_Main2ndBlendMask"), hsvgMaterial,
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

                SetMaterialTexture(assetSaver, dictionaries.GetTexture("_Main3rdTex"), hsvgMaterial,
                    PropertyMainThirdTex, ref srcMain3);
                SetMaterialTexture(assetSaver, dictionaries.GetTexture("_Main3rdBlendMask"), hsvgMaterial,
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
            Texture bakedMainTex, int shadowType = 0)
        {
            var dictionaries = GetProps(material);
            var useShadow = dictionaries.GetFloat("_UseShadow");
            var shadowColor = dictionaries.GetColor("_ShadowColor");
            var shadowColorTex = dictionaries.GetTexture("_ShadowColorTex");
            var shadowStrength = dictionaries.GetFloat("_ShadowStrength");
            var shadowStrengthMask = dictionaries.GetTexture("_ShadowStrengthMask");
            var mainTex = dictionaries.GetTexture("_MainTex");

            var shouldNotBakeAll = useShadow == 0.0 && shadowColor == Color.white && shadowColorTex == null &&
                                   shadowStrengthMask == null;
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

            bool existsShadowTex;
            if (shadowTex && assetSaver.IsTemporaryAsset(shadowTex))
            {
                Object.DestroyImmediate(srcMain2);
                srcMain2 = shadowTex!.Clone();
                hsvgMaterial.SetTexture(PropertyMainSecondTex, srcMain2);
                existsShadowTex = true;
            }
            else
            {
                var path = AssetDatabase.GetAssetPath(shadowTex);
                existsShadowTex = !string.IsNullOrEmpty(path);
                if (existsShadowTex)
                {
                    lilTextureUtils.LoadTexture(ref srcMain2, path);
                    hsvgMaterial.SetTexture(PropertyMainSecondTex, srcMain2);
                }
            }

            Texture2D? innerMainTexture;
            if (bakedMainTex && assetSaver.IsTemporaryAsset(bakedMainTex))
            {
                Object.DestroyImmediate(srcTexture);
                srcTexture = bakedMainTex.Clone()!;
                innerMainTexture = srcTexture;
            }
            else
            {
                var path = AssetDatabase.GetAssetPath(bakedMainTex);
                if (!string.IsNullOrEmpty(path))
                {
                    lilTextureUtils.LoadTexture(ref srcTexture, path);
                    innerMainTexture = srcTexture;
                }
                else
                {
                    innerMainTexture = Texture2D.whiteTexture;
                }
            }

            hsvgMaterial.SetTexture(PropertyMainTex, innerMainTexture);
            hsvgMaterial.SetTexture(PropertyMainThirdTex, innerMainTexture);
            if (!existsShadowTex)
            {
                hsvgMaterial.SetTexture(PropertyMainSecondTex, innerMainTexture);
            }

            Texture2D? innerMaskTexture;
            if (shadowStrengthMask && assetSaver.IsTemporaryAsset(shadowStrengthMask))
            {
                Object.DestroyImmediate(srcMask2);
                srcMask2 = shadowStrengthMask!.Clone();
                innerMaskTexture = srcMask2;
            }
            else
            {
                var path = AssetDatabase.GetAssetPath(shadowStrengthMask);
                if (!string.IsNullOrEmpty(path))
                {
                    lilTextureUtils.LoadTexture(ref srcMask2, path);
                    innerMaskTexture = srcMask2;
                }
                else
                {
                    innerMaskTexture = Texture2D.whiteTexture;
                }
            }

            hsvgMaterial.SetTexture(PropertyMainSecondBlendMask, innerMaskTexture);
            hsvgMaterial.SetTexture(PropertyMainThirdBlendMask, innerMaskTexture);

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
                return null;
            // run bake
            var bufMainTexture = matcapTex as Texture2D;
            var hsvgMaterial = new Material(lilShaderManager.ltsbaker);

            var matcapTexName = matcapTex ? matcapTex!.name : AssetPathUtils.TrimCloneSuffix(material.name);
            var srcTexture = new Texture2D(2, 2);

            hsvgMaterial.SetColor(PropertyColor, matcapColor);
            hsvgMaterial.SetVector(PropertyMainTexHsvg, lilConstants.defaultHSVG);
            SetMaterialTexture(assetSaver, bufMainTexture, hsvgMaterial, PropertyMainTex, ref srcTexture);

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
            if (baseTex && assetSaver.IsTemporaryAsset(baseTex))
            {
                Object.DestroyImmediate(srcAlphaMask);
                srcAlphaMask = baseTex!.Clone();
                hsvgMaterial.SetTexture(PropertyAlphaMask, srcAlphaMask);
            }
            else
            {
                var path = AssetDatabase.GetAssetPath(baseTex);
                if (!string.IsNullOrEmpty(path))
                {
                    lilTextureUtils.LoadTexture(ref srcAlphaMask, path);
                    hsvgMaterial.SetTexture(PropertyAlphaMask, srcAlphaMask);
                }
                else
                {
                    return null;
                }
            }

            SetMaterialTexture(assetSaver, bufMainTexture, hsvgMaterial, PropertyMainTex, ref srcTexture);

            var outTexture = RunBake(srcTexture, hsvgMaterial);
            outTexture.name = $"{mainTexName}_{nameof(AutoBakeAlphaMask)}";
            assetSaver.SaveAsset(outTexture);

            Object.DestroyImmediate(hsvgMaterial);
            Object.DestroyImmediate(srcTexture);

            return outTexture;
        }

        private static Texture2D RunBake(Texture2D srcTexture, Material material,
            Texture2D? referenceTexture = null)
        {
            int width = 4096, height = 4096;
            if (referenceTexture != null)
            {
                width = referenceTexture.width;
                height = referenceTexture.height;
            }
            else if (srcTexture != null)
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

        private static void SetMaterialTexture(IAssetSaver assetSaver, Texture? baseTex, Material hsvgMaterial,
            int propertyID, ref Texture2D srcTexture)
        {
            if (baseTex && assetSaver.IsTemporaryAsset(baseTex))
            {
                Object.DestroyImmediate(srcTexture);
                srcTexture = baseTex!.Clone()!;
                hsvgMaterial.SetTexture(propertyID, srcTexture);
            }
            else
            {
                var path = AssetDatabase.GetAssetPath(baseTex);
                if (!string.IsNullOrEmpty(path))
                {
                    lilTextureUtils.LoadTexture(ref srcTexture, path);
                    hsvgMaterial.SetTexture(propertyID, srcTexture);
                }
                else
                {
                    hsvgMaterial.SetTexture(propertyID, Texture2D.whiteTexture);
                }
            }
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

    internal static class AssetPathUtils
    {
        private const string CloneSuffix = "(Clone)";

        public static string BasePath => "Assets/NDMF VRM Exporter";

        public static string GetOutputPath(GameObject gameObject)
        {
            return GetBasePath(gameObject, BasePath);
        }

        public static string GetTempPath(GameObject gameObject)
        {
            return GetBasePath(gameObject, FileUtil.GetUniqueTempPathInProject());
        }

        public static string TrimCloneSuffix(string name)
        {
            if (name.EndsWith(CloneSuffix))
            {
                name = name[..^CloneSuffix.Length];
            }

            return name;
        }

        private static string GetBasePath(GameObject gameObject, string basePath)
        {
            var sceneName = !string.IsNullOrEmpty(gameObject.scene.name)
                ? StripInvalidFileNameCharacters(gameObject.scene.name)
                : "Untitled";
            var gameObjectName = TrimCloneSuffix(StripInvalidFileNameCharacters(gameObject.name));
            return $"{basePath}/{sceneName}/{gameObjectName}";
        }

        private static string StripInvalidFileNameCharacters(string name)
        {
            return string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        }
    }

    internal sealed class NdmfVrmExporter : IDisposable
    {
        private static readonly string VrmcVrm = "VRMC_vrm";
        private static readonly string VrmcSpringBone = "VRMC_springBone";
        private static readonly string VrmcSpringBoneExtendedCollider = "VRMC_springBone_extended_collider";
        private static readonly string VrmcNodeConstraint = "VRMC_node_constraint";
        private static readonly string VrmcMaterialsMtoon = "VRMC_materials_mtoon";
        private static readonly int PropertyUseEmission = Shader.PropertyToID("_UseEmission");
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

        public NdmfVrmExporter(GameObject gameObject, IAssetSaver assetSaver)
        {
            var packageJsonFile = File.ReadAllText($"Packages/{PackageJson.Name}/package.json");
            var packageJson = PackageJson.LoadFromString(packageJsonFile);
            _gameObject = gameObject;
            _assetSaver = assetSaver;
            _materialIDs = new Dictionary<Material, gltf.ObjectID>();
            _bakedMaterialMainTextures = new Dictionary<Material, Texture?>();
            _transformNodeIDs = new Dictionary<Transform, gltf.ObjectID>();
            _transformNodeNames = new HashSet<string>();
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
                    RetrieveMesh(smr.sharedMesh, smr.sharedMaterials, component, smr, child, ref node);
                }
                else if (child.gameObject.TryGetComponent<MeshRenderer>(out var mr) &&
                         mr.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh)
                {
                    RetrieveMesh(filter.sharedMesh, mr.sharedMaterials, component, null, null, ref node);
                }

                RetrieveAllMeshRenderers(child, component);
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
                    var textureUnit =
                        ExportTextureUnit(thumbnail, component.name, TextureFormat.RGB24, ColorSpace.Gamma, true);
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
                if (_bakedMaterialMainTextures.TryGetValue(material, out var texture))
                {
                    var mtoon = vrmExporter.ExportMToon(material, texture, _materialExporter);
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
            var basePath = AssetPathUtils.GetTempPath(_gameObject);
            var textureIndex = 0;
            Directory.CreateDirectory(basePath);
            foreach (var texture in _root.Textures!)
            {
                var textureID = new gltf.ObjectID((uint)textureIndex);
                var metadata = _materialExporter.TextureMetadata[textureID];
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
                    var sourcePath = $"{basePath}/{source.ID}.bin";
                    var destPath = $"{basePath}/{source.ID}.ktx";
                    File.WriteAllBytes(sourcePath, inputData);
                    var info = new ProcessStartInfo(ktxToolPath);
                    info.ArgumentList.Add("create");
                    info.ArgumentList.Add("--format");
                    info.ArgumentList.Add(internalFormat);
                    info.ArgumentList.Add("--assign-oetf");
                    info.ArgumentList.Add(oetf);
                    info.ArgumentList.Add("--assign-primaries");
                    info.ArgumentList.Add(primaries);
                    info.ArgumentList.Add("--encode");
                    info.ArgumentList.Add("uastc");
                    info.ArgumentList.Add("--zstd");
                    info.ArgumentList.Add("22");
                    info.ArgumentList.Add("--generate-mipmap");
                    info.ArgumentList.Add(sourcePath);
                    info.ArgumentList.Add(destPath);
                    info.CreateNoWindow = true;
                    info.UseShellExecute = false;
                    try
                    {
                        using var process = Process.Start(info);
                        if (process is null)
                            continue;
                        process.WaitForExit();
                        var outputData = File.ReadAllBytes(destPath);
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
                _boneMatrices = bones.Select(bone => bone.localToWorldMatrix).ToArray();
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
                    if (!transform || _transformMap.TryGetValue(transform, out var offset))
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

        private void RetrieveMesh(Mesh mesh, Material[] materials, NdmfVrmExporterComponent component,
            SkinnedMeshRenderer? smr, Transform? parentTransform,
            ref gltf.node.Node node)
        {
            using var _ = new ScopedProfile($"{nameof(RetrieveMesh)}({mesh.name})");
            System.Numerics.Vector3[] positions, normals;
            gltf.exporter.JointUnit[] jointUnits;
            System.Numerics.Vector4[] weights;
            if (smr && parentTransform)
            {
                var resolver = new BoneResolver(parentTransform!, smr!);
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
                Name = new gltf.UnicodeString(mesh.name),
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
                var m = materials[i];
                if (!_materialIDs.TryGetValue(m, out var materialID))
                {
                    var shaderName = m.shader.name;
                    var config = new GltfMaterialExporter.ExportOverrides();
#if NVE_HAS_LILTOON
                    if (shaderName == "lilToon" || shaderName.StartsWith("Hidden/lilToon"))
                    {
                        if (shaderName.Contains("Cutout"))
                        {
                            config.AlphaMode = gltf.material.AlphaMode.Mask;
                            config.MainTexture = MaterialBaker.AutoBakeMainTexture(_assetSaver, m);
                        }
                        else if (shaderName.Contains("Transparent") || shaderName.Contains("Overlay"))
                        {
                            config.AlphaMode = gltf.material.AlphaMode.Blend;
                            config.MainTexture = enableBakingAlphaMaskTexture &&
                                                 Mathf.Approximately(m.GetFloat(PropertyAlphaMaskMode), 1.0f)
                                ? MaterialBaker.AutoBakeAlphaMask(_assetSaver, m)
                                : MaterialBaker.AutoBakeMainTexture(_assetSaver, m);
                        }
                        else
                        {
                            config.AlphaMode = gltf.material.AlphaMode.Opaque;
                            config.MainTexture = MaterialBaker.AutoBakeMainTexture(_assetSaver, m);
                        }

                        config.EnableEmission = Mathf.Approximately(m.GetFloat(PropertyUseEmission), 1.0f);
                        config.EnableNormalMap = Mathf.Approximately(m.GetFloat(PropertyUseBumpMap), 1.0f);
                        _bakedMaterialMainTextures.Add(m, config.MainTexture);

                        if (component.disableVertexColorOnLiltoon)
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
                    }
#endif // NVE_HAS_LILTOON
                    materialID = new gltf.ObjectID((uint)_root.Materials!.Count);
                    var material = _materialExporter.Export(m, config);
                    _root.Materials!.Add(material);
                    _materialIDs.Add(m, materialID);
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

            node.Mesh = _exporter.CreateMesh(_root, meshUnit);
        }

        private static gltf.exporter.SampledTextureUnit ExportTextureUnit(Texture texture, string name,
            TextureFormat textureFormat, ColorSpace cs, bool blit)
        {
            using var _ = new ScopedProfile($"ExportTextureUnit({name})");
            byte[] bytes;
            if (blit)
            {
                var destTexture = texture.Blit(textureFormat, cs);
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
                        !transform.TryGetComponent<VRCPhysBoneCollider>(out var collider))
                        continue;
                    ConvertBoneCollider(collider, ref pbColliders, ref colliders);
                }

                foreach (var (transform, _) in _transformNodeIDs)
                {
                    if (!transform.gameObject.activeInHierarchy ||
                        component.excludedSpringBoneTransforms.Contains(transform) ||
                        !transform.TryGetComponent<VRCPhysBone>(out var pb))
                        continue;
                    ConvertColliderGroup(pb, pbColliders, ref colliderGroups);
                }

                foreach (var (transform, _) in _transformNodeIDs)
                {
                    if (!transform.gameObject.activeInHierarchy ||
                        component.excludedSpringBoneTransforms.Contains(transform) ||
                        !transform.TryGetComponent<VRCPhysBone>(out var pb))
                        continue;
                    ConvertSpringBone(pb, pbColliders, colliderGroups, ref springs);
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
                    var numSources = vrcAimConstraint.Sources.Count;
                    if (numSources >= 1)
                    {
                        if (numSources > 1)
                        {
                            Debug.LogWarning($"Constraint with multiple sources is not supported in {node.name}");
                        }

                        var constraintSource = vrcAimConstraint.Sources.First();
                        sourceID = _transformNodeIDs[constraintSource.SourceTransform];
                        weight = vrcAimConstraint.GlobalWeight * constraintSource.Weight;
                    }
                    else
                    {
                        sourceID = immobileNodeID;
                        weight = vrcAimConstraint.GlobalWeight;
                    }

                    vrmNodeConstraint = new vrm.constraint.NodeConstraint
                    {
                        Constraint = new vrm.constraint.Constraint
                        {
                            Aim = new vrm.constraint.AimConstraint
                            {
                                Source = sourceID,
                                Weight = weight,
                            }
                        }
                    };
                }
                else if (node.TryGetComponent<AimConstraint>(out var aimConstraint))
                {
                    var numSources = aimConstraint.sourceCount;
                    if (numSources >= 1)
                    {
                        if (numSources > 1)
                        {
                            Debug.LogWarning($"Constraint with multiple sources is not supported in {node.name}");
                        }

                        var constraintSource = aimConstraint.GetSource(0);
                        sourceID = _transformNodeIDs[constraintSource.sourceTransform];
                        weight = aimConstraint.weight * constraintSource.weight;
                    }
                    else
                    {
                        sourceID = immobileNodeID;
                        weight = aimConstraint.weight;
                    }

                    vrmNodeConstraint = new vrm.constraint.NodeConstraint
                    {
                        Constraint = new vrm.constraint.Constraint
                        {
                            Aim = new vrm.constraint.AimConstraint
                            {
                                Source = sourceID,
                                Weight = weight,
                            }
                        }
                    };
                }
#if NVE_HAS_VRCHAT_AVATAR_SDK
                else if (node.TryGetComponent<VRCRotationConstraint>(out var vrcRotationConstraint))
                {
                    var numSources = vrcRotationConstraint.Sources.Count;
                    if (numSources >= 1)
                    {
                        if (numSources > 1)
                        {
                            Debug.LogWarning($"Constraint with multiple sources is not supported in {node.name}");
                        }

                        var constraintSource = vrcRotationConstraint.Sources.First();
                        sourceID = _transformNodeIDs[constraintSource.SourceTransform];
                        weight = vrcRotationConstraint.GlobalWeight * constraintSource.Weight;
                    }
                    else
                    {
                        weight = vrcRotationConstraint.GlobalWeight;
                    }

                    vrmNodeConstraint = new vrm.constraint.NodeConstraint
                    {
                        Constraint = new vrm.constraint.Constraint
                        {
                            Rotation = new vrm.constraint.RotationConstraint
                            {
                                Source = sourceID,
                                Weight = weight,
                            }
                        }
                    };
                }
#endif // NVE_HAS_VRCHAT_AVATAR_SDK
                else if (node.TryGetComponent<RotationConstraint>(out var rotationConstraint))
                {
                    var numSources = rotationConstraint.sourceCount;
                    if (numSources >= 1)
                    {
                        if (numSources > 1)
                        {
                            Debug.LogWarning($"Constraint with multiple sources is not supported in {node.name}");
                        }

                        var constraintSource = rotationConstraint.GetSource(0);
                        sourceID = _transformNodeIDs[constraintSource.sourceTransform];
                        weight = rotationConstraint.weight * constraintSource.weight;
                    }
                    else
                    {
                        weight = rotationConstraint.weight;
                    }

                    vrmNodeConstraint = new vrm.constraint.NodeConstraint
                    {
                        Constraint = new vrm.constraint.Constraint
                        {
                            Rotation = new vrm.constraint.RotationConstraint
                            {
                                Source = sourceID,
                                Weight = weight,
                            }
                        }
                    };
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

            public vrm.mtoon.MToon ExportMToon(Material material, Texture? sourceMainTexture,
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
                var floats = MaterialBaker.GetProps(props.FindPropertyRelative("m_Floats"));
                var colors = MaterialBaker.GetProps(props.FindPropertyRelative("m_Colors"));
                var scrollRotate = colors["_MainTex_ScrollRotate"].colorValue;
                var mainTexture = exporter.ExportTextureInfoMToon(material, sourceMainTexture,
                    ColorSpace.Gamma, enableAlphaChannel: false, blit: false);

                Texture2D? LocalRetrieveTexture2D(string name)
                {
                    var prop = textures!.GetValueOrDefault(name, null);
                    return prop?.FindPropertyRelative("m_Texture").objectReferenceValue as Texture2D;
                }

                if (Mathf.Approximately(floats["_UseShadow"].floatValue, 1.0f))
                {
                    var shadowBorder = floats["_ShadowBorder"].floatValue;
                    var shadowBlur = floats["_ShadowBlur"].floatValue;
                    var shadeShift = Mathf.Clamp01(shadowBorder - (shadowBlur * 0.5f)) * 2.0f - 1.0f;
                    var shadeToony = (2.0f - Mathf.Clamp01(shadowBorder + shadowBlur * 0.5f) * 2.0f) /
                                     (1.0f - shadeShift);
                    if (LocalRetrieveTexture2D("_ShadowStrengthMask") ||
                        !Mathf.Approximately(floats["_ShadowMainStrength"].floatValue, 0.0f))
                    {
                        var bakedShadowTex =
                            MaterialBaker.AutoBakeShadowTexture(_assetSaver, material, sourceMainTexture!);
                        mtoon.ShadeColorFactor = Color.white.ToVector3();
                        mtoon.ShadeMultiplyTexture =
                            exporter.ExportTextureInfoMToon(material, bakedShadowTex, ColorSpace.Gamma,
                                enableAlphaChannel: false, blit: bakedShadowTex != null);
                    }
                    else
                    {
                        var shadowColor = colors["_ShadowColor"].colorValue;
                        var shadowStrength = floats["_ShadowStrength"].floatValue;
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
                                enableAlphaChannel: false, blit: true)
                            : mainTexture;
                    }

                    var texture = LocalRetrieveTexture2D("_ShadowBorderTex");
                    var info = exporter.ExportTextureInfoMToon(material, texture, ColorSpace.Gamma,
                        enableAlphaChannel: false, blit: true);
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
                    mtoon.ShadeMultiplyTexture = mainTexture;
                }

                var component = _gameObject.GetComponent<NdmfVrmExporterComponent>();
                if (component.enableMToonRimLight && Mathf.Approximately(floats["_UseRim"].floatValue, 1.0f))
                {
                    var rimColorTexture = LocalRetrieveTexture2D("_RimColorTex");
                    var rimBorder = floats["_RimBorder"].floatValue;
                    var rimBlur = floats["_RimBlur"].floatValue;
                    var rimFresnelPower = floats["_RimFresnelPower"].floatValue;
                    var rimFp = rimFresnelPower / Mathf.Max(0.001f, rimBlur);
                    var rimLift = Mathf.Pow(1.0f - rimBorder, rimFresnelPower) * (1.0f - rimBlur);
                    mtoon.RimLightingMixFactor = 1.0f;
                    mtoon.ParametricRimColorFactor = colors["_RimColor"].colorValue.ToVector3();
                    mtoon.ParametricRimLiftFactor = rimLift;
                    mtoon.ParametricRimFresnelPowerFactor = rimFp;
                    if (Mathf.Approximately(floats["_RimBlendMode"].floatValue, 3.0f))
                    {
                        mtoon.RimMultiplyTexture =
                            exporter.ExportTextureInfoMToon(material, rimColorTexture, ColorSpace.Gamma,
                                enableAlphaChannel: false, blit: true);
                    }
                }
                else
                {
                    mtoon.RimLightingMixFactor = 0.0f;
                }

                if (component.enableMToonMatCap && Mathf.Approximately(floats["_UseMatCap"].floatValue, 1.0f) &&
                    !Mathf.Approximately(floats["_MatCapBlendMode"].floatValue, 3.0f))
                {
                    var matcapTexture = LocalRetrieveTexture2D("_MatCapTex");
                    if (matcapTexture)
                    {
                        var bakedMatCap = MaterialBaker.AutoBakeMatCap(_assetSaver, material);
                        mtoon.MatcapTexture =
                            exporter.ExportTextureInfoMToon(material, bakedMatCap, ColorSpace.Gamma,
                                enableAlphaChannel: false, blit: bakedMatCap != null);
                        mtoon.MatcapFactor = System.Numerics.Vector3.One;
                    }
                }

                var shaderName = material.shader.name;
                var isOutline = shaderName.Contains("Outline");
                if (component.enableMToonOutline && isOutline)
                {
                    var outlineWidthTexture = LocalRetrieveTexture2D("_OutlineWidthMask");
                    mtoon.OutlineWidthMode = vrm.mtoon.OutlineWidthMode.WorldCoordinates;
                    mtoon.OutlineLightingMixFactor = 1.0f;
                    mtoon.OutlineWidthFactor = floats["_OutlineWidth"].floatValue * 0.01f;
                    mtoon.OutlineColorFactor = colors["_OutlineColor"].colorValue.ToVector3();
                    mtoon.OutlineWidthMultiplyTexture =
                        exporter.ExportTextureInfoMToon(material, outlineWidthTexture, ColorSpace.Gamma,
                            enableAlphaChannel: false, blit: true);
                }

                var isCutout = shaderName.Contains("Cutout");
                var isTransparent = shaderName.Contains("Transparent") || shaderName.Contains("Overlay");
                mtoon.TransparentWithZWrite =
                    isCutout || (isTransparent && !Mathf.Approximately(floats["_ZWrite"].floatValue, 0.0f));

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
                var offsetFromHeadBone = descriptor.ViewPosition - head.position;
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
                    return;
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
                    return null;
                var blendShapeName = descriptor.VisemeBlendShapes[(int)viseme];
                var offset = descriptor.VisemeSkinnedMesh.sharedMesh.GetBlendShapeIndex(blendShapeName);
                var nodeID = _transformNodeIDs[descriptor.VisemeSkinnedMesh.transform];
                var item = new vrm.core.ExpressionItem
                {
                    MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                    {
                        new()
                        {
                            Node = nodeID,
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
                    return null;
                var settings = descriptor.customEyeLookSettings;
                if (settings.eyelidType != VRCAvatarDescriptor.EyelidType.Blendshapes)
                    return null;
                var blendShapeIndex = settings.eyelidsBlendshapes[offset];
                if (blendShapeIndex == -1)
                    return null;
                var nodeID = _transformNodeIDs[settings.eyelidsSkinnedMesh.transform];
                var item = new vrm.core.ExpressionItem
                {
                    MorphTargetBinds = new List<vrm.core.MorphTargetBind>
                    {
                        new()
                        {
                            Node = nodeID,
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
                        return null;
                    }
                    case VrmExpressionProperty.BaseType.AnimationClip:
                    {
                        var morphTargetBinds = new List<vrm.core.MorphTargetBind>();
                        foreach (var binding in AnimationUtility.GetCurveBindings(property.blendShapeAnimationClip))
                        {
                            if (!binding.propertyName.StartsWith(VrmExpressionProperty.BlendShapeNamePrefix))
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
                var node = _transformNodeIDs[rootTransform];
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
                            Node = node,
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
                            Node = node,
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
                            Node = node,
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

            private static void ConvertColliderGroup(VRCPhysBone pb, IList<VRCPhysBoneColliderBase> pbColliders,
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

            private static void RetrieveTransforms(Transform transform, ref List<Transform> result)
            {
                for (var i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (!child || child is null)
                        continue;
                    result.Add(child);
                    RetrieveTransforms(child, ref result);
                }
            }

            private static void CalcTransformDepth(Transform? transform, ref int depth)
            {
                for (var i = 0; i < transform?.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (!child || child is null)
                        continue;
                    depth++;
                    CalcTransformDepth(child, ref depth);
                }
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
                CalcTransformDepth(transform, ref lowerDepth);
                return (upperDepth, lowerDepth);
            }

            private void ConvertSpringBone(VRCPhysBone pb, IList<VRCPhysBoneColliderBase> pbColliders,
                IList<vrm.sb.ColliderGroup> colliderGroups,
                ref IList<vrm.sb.Spring> springs)
            {
                var transforms = new List<Transform>();
                RetrieveTransforms(pb.GetRootTransform(), ref transforms);
                var joints = transforms.Select(transform =>
                    {
                        var (upperDepth, lowerDepth) = FindTransformDepth(transform, pb.GetRootTransform());
                        var depthRatio = upperDepth / (float)(upperDepth + lowerDepth);
                        var evaluate = new Func<float, AnimationCurve, float>(
                            (value, curve) =>
                                curve.length > 0
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
                            Node = _transformNodeIDs[transform],
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
                    Name = new gltf.UnicodeString(pb.name),
                    Center = null,
                    ColliderGroups = newColliderGroups.Count > 0 ? newColliderGroups.ToList() : null,
                    Joints = joints,
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
                var transform = animator.GetBoneTransform(bone);
                return transform ? _transformNodeIDs[transform] : null;
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
                public bool EnableEmission { get; set; }
                public bool EnableNormalMap { get; set; } = true;
            }

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
                _root = root;
                _exporter = exporter;
                _textureIDs = new Dictionary<(Texture, bool), gltf.ObjectID>();
                TextureMetadata = new Dictionary<gltf.ObjectID, TextureItemMetadata>();
                _extensionsUsed = extensionsUsed;
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
                    Name = new gltf.UnicodeString(source.name),
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
                        ExportTextureInfo(source, overrides.MainTexture, ColorSpace.Gamma, enableAlphaChannel: true,
                            blit: false);
                }
                else if (source.HasProperty(PropertyMainTex))
                {
                    var texture = source.GetTexture(PropertyMainTex);
                    material.PbrMetallicRoughness.BaseColorTexture =
                        ExportTextureInfo(source, texture, ColorSpace.Gamma, enableAlphaChannel: false, blit: true);
                    DecorateTextureTransform(source, PropertyMainTex, material.PbrMetallicRoughness.BaseColorTexture);
                }

                if (overrides.EnableEmission || source.IsKeywordEnabled("_EMISSION"))
                {
                    if (source.HasProperty(PropertyEmissionColor))
                    {
                        var emissiveFactor = source.GetColor(PropertyEmissionColor)
                            .ToVector3(ColorSpace.Gamma, ColorSpace.Linear);
                        material.EmissiveFactor = System.Numerics.Vector3.Clamp(emissiveFactor,
                            System.Numerics.Vector3.Zero, System.Numerics.Vector3.One);
                        var emissiveStrength = Mathf.Max(emissiveFactor.X, emissiveFactor.Y, emissiveFactor.Z);
                        if (emissiveStrength > 1.0f)
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
                    }

                    if (source.HasProperty(PropertyEmissionMap))
                    {
                        var texture = source.GetTexture(PropertyEmissionMap);
                        material.EmissiveTexture =
                            ExportTextureInfo(source, texture, ColorSpace.Gamma, enableAlphaChannel: false, blit: true);
                        DecorateTextureTransform(source, PropertyEmissionMap, material.EmissiveTexture);
                    }
                }

                if (overrides.EnableNormalMap && source.HasProperty(PropertyBumpMap))
                {
                    var texture = source.GetTexture(PropertyBumpMap);
                    if (texture)
                    {
                        var info = ExportTextureInfo(source, texture, ColorSpace.Linear, enableAlphaChannel: false,
                            blit: true);
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
                        var info = ExportTextureInfo(source, texture, ColorSpace.Linear, enableAlphaChannel: false,
                            blit: true);
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
                            ExportTextureInfo(source, texture, ColorSpace.Linear, enableAlphaChannel: false,
                                blit: true);
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

                if (source.HasProperty(PropertyCullMode))
                {
                    material.DoubleSided = source.GetInt(PropertyCullMode) == 0;
                }

                return material;
            }

            internal gltf.material.TextureInfo? ExportTextureInfoMToon(Material material, Texture? texture,
                ColorSpace cs, bool enableAlphaChannel, bool blit)
            {
                return ExportTextureInfoInner(material, texture, cs, enableAlphaChannel, blit, true);
            }

            private gltf.material.TextureInfo? ExportTextureInfo(Material material, Texture? texture,
                ColorSpace cs, bool enableAlphaChannel, bool blit)
            {
                return ExportTextureInfoInner(material, texture, cs, enableAlphaChannel, blit, false);
            }

            private gltf.material.TextureInfo? ExportTextureInfoInner(Material material, Texture? texture,
                ColorSpace cs, bool enableAlphaChannel, bool blit, bool mtoon)
            {
                if (!texture || texture is null)
                {
                    return null;
                }

                if (!_textureIDs.TryGetValue((texture, enableAlphaChannel), out var textureID))
                {
                    var textureFormat = TextureFormat.RGB24;
                    var hasTransparent = material.GetTag("RenderType", true) switch
                    {
                        "Transparent" => true,
                        "TransparentCutout" => true,
                        _ => false,
                    };
                    if (hasTransparent && enableAlphaChannel)
                    {
                        textureFormat = TextureFormat.RGBA32;
                    }

                    var name = $"{AssetPathUtils.TrimCloneSuffix(material.name)}_{texture.name}_{textureFormat}";
                    if (mtoon)
                    {
                        name = $"MToon_{name}";
                    }

                    var textureUnit = ExportTextureUnit(texture, name, textureFormat, cs, blit);
                    textureID = _exporter.CreateSampledTexture(_root, textureUnit);
                    _textureIDs.Add((texture, enableAlphaChannel), textureID);
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
            private static readonly int PropertyCullMode = Shader.PropertyToID("_CullMode");

            public IDictionary<gltf.ObjectID, TextureItemMetadata> TextureMetadata { get; }
            private readonly gltf.Root _root;
            private readonly gltf.exporter.Exporter _exporter;
            private readonly IDictionary<(Texture, bool), gltf.ObjectID> _textureIDs;
            private readonly ISet<string> _extensionsUsed;
        }

        private readonly GameObject _gameObject;
        private readonly IAssetSaver _assetSaver;
        private readonly gltf.exporter.Exporter _exporter;
        private readonly gltf.Root _root;
        private readonly IDictionary<Material, gltf.ObjectID> _materialIDs;
        private readonly IDictionary<Material, Texture?> _bakedMaterialMainTextures;
        private readonly IDictionary<Transform, gltf.ObjectID> _transformNodeIDs;
        private readonly ISet<string> _transformNodeNames;
        private readonly ISet<string> _extensionsUsed;
        private readonly GltfMaterialExporter _materialExporter;
    }

    internal sealed class NdmfVrmExporterPlugin : Plugin<NdmfVrmExporterPlugin>
    {
        public override string QualifiedName => NdmfVrmExporter.PackageJson.Name;

        public override string DisplayName => "NDMF VRM Exporter";

        protected override void Configure()
        {
            InPhase(BuildPhase.Optimizing)
                .AfterPlugin("com.anatawa12.avatar-optimizer")
                .AfterPlugin("nadena.dev.modular-avatar")
                .AfterPlugin("net.rs64.tex-trans-tool")
                .Run("VrmExportPlugin", ctx =>
                {
                    if (!ctx.AvatarRootObject.TryGetComponent<NdmfVrmExporterComponent>(out var component))
                    {
                        Debug.LogWarning(Translator._("component.runtime.error.validation.not-attached"));
                        return;
                    }

                    if (!component.enabled)
                    {
                        Debug.Log(Translator._("component.runtime.error.validation..not-enabled"));
                        return;
                    }

                    if (!component.HasAuthor)
                    {
                        Debug.LogWarning(Translator._("component.runtime.error.validation.author"));
                        return;
                    }

                    if (!component.HasLicenseUrl)
                    {
                        Debug.LogWarning(Translator._("component.runtime.error.validation.license-url"));
                        return;
                    }

                    try
                    {
                        CheckAllSkinnedMeshRenderers(ctx.AvatarRootTransform);
                    }
                    catch (CorruptedSkinnedMeshRendererFoundException e)
                    {
                        ErrorReport.ReportError(Translator.Instance, ErrorSeverity.NonFatal,
                            "component.runtime.error.validation.smr", e.SkinnedMeshRenderer);
                        return;
                    }

                    var ro = ctx.AvatarRootObject;
                    var basePath = AssetPathUtils.GetOutputPath(ro);
                    Directory.CreateDirectory(basePath);
                    using var stream = new MemoryStream();
                    using var exporter = new NdmfVrmExporter(ro, ctx.AssetSaver);
                    var json = exporter.Export(stream);
                    var bytes = stream.GetBuffer();
                    File.WriteAllBytes($"{basePath}.vrm", bytes);
                    if (component.enableGenerateJsonFile)
                    {
                        File.WriteAllText($"{basePath}.json", json);
                    }

                    if (!component.deleteTemporaryObjects)
                    {
                        return;
                    }

                    foreach (var item in new[] { AssetPathUtils.GetTempPath(ro), basePath })
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
                });
        }

        private sealed class CorruptedSkinnedMeshRendererFoundException : Exception
        {
            public CorruptedSkinnedMeshRendererFoundException(SkinnedMeshRenderer smr) : base("")
            {
                SkinnedMeshRenderer = smr;
            }

            public SkinnedMeshRenderer SkinnedMeshRenderer { get; }
        }

        private static void CheckAllSkinnedMeshRenderers(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (child.gameObject.TryGetComponent<SkinnedMeshRenderer>(out var smr) && smr.sharedMesh)
                {
                    if (smr.bones.Any(bone => !bone))
                    {
                        throw new CorruptedSkinnedMeshRendererFoundException(smr);
                    }
                }

                CheckAllSkinnedMeshRenderers(child);
            }
        }
    }

#if NVE_HAS_AVATAR_OPTIMIZER
    namespace aao
    {
        [ComponentInformation(typeof(NdmfVrmExporterComponent))]
        internal sealed class NdmfVrmExporterComponentInformation : ComponentInformation<NdmfVrmExporterComponent>
        {
            protected override void CollectMutations(NdmfVrmExporterComponent component,
                ComponentMutationsCollector collector)
            {
                var blendShapeNameToSmr = new Dictionary<string, SkinnedMeshRenderer>();
                VrmExpressionProperty.RetrieveAllBlendShapes(component.transform, (name, smr, props) =>
                {
                    if (props.TryGetValue(name, out var foundSmr))
                    {
                        if (smr != foundSmr)
                        {
                            Debug.LogWarning($"Different SMR found: left={smr}, right={foundSmr}");
                        }
                    }
                    else
                    {
                        props.Add(name, smr);
                    }
                }, ref blendShapeNameToSmr);
                var allExpressionUsedBlendShapeNames = new List<string?>();
                allExpressionUsedBlendShapeNames.AddRange(component.expressionPresetHappyBlendShape.BlendShapeNames);
                allExpressionUsedBlendShapeNames.AddRange(component.expressionPresetAngryBlendShape.BlendShapeNames);
                allExpressionUsedBlendShapeNames.AddRange(component.expressionPresetSadBlendShape.BlendShapeNames);
                allExpressionUsedBlendShapeNames.AddRange(component.expressionPresetRelaxedBlendShape.BlendShapeNames);
                allExpressionUsedBlendShapeNames.AddRange(component.expressionPresetSurprisedBlendShape
                    .BlendShapeNames);
                foreach (var property in component.expressionCustomBlendShapes)
                {
                    allExpressionUsedBlendShapeNames.AddRange(property.BlendShapeNames);
                }

                var collectorProperties = new Dictionary<SkinnedMeshRenderer, IList<string>>();
                foreach (var name in allExpressionUsedBlendShapeNames)
                {
                    if (string.IsNullOrEmpty(name) || !blendShapeNameToSmr.TryGetValue(name!, out var smr))
                    {
                        continue;
                    }

                    if (collectorProperties.TryGetValue(smr, out var names))
                    {
                        names.Add($"blendShape.{name}");
                    }
                    else
                    {
                        collectorProperties.Add(smr, new List<string>());
                    }
                }

                foreach (var (smr, names) in collectorProperties)
                {
                    collector.ModifyProperties(smr, names);
                }
            }

            protected override void CollectDependency(NdmfVrmExporterComponent component,
                ComponentDependencyCollector collector)
            {
                collector.MarkEntrypoint();
            }
        }
    }
#endif // NVE_HAS_AVATAR_OPTIMIZER
}

#endif // NVE_HAS_NDMF
