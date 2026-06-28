// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

#if NVE_HAS_VRCHAT_AVATAR_SDK
using System.Net.Http;
using System.Threading;
using VRC.Core;
using VRC.SDKBase.Editor.Api;
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

#if NVE_HAS_LILTOON
#endif // NVE_HAS_LILTOON

#if NVE_HAS_NDMF
using nadena.dev.ndmf;
using nadena.dev.ndmf.localization;
using nadena.dev.ndmf.runtime;
#if NVE_HAS_NDMF_PLATFORM_SUPPORT
using nadena.dev.ndmf.ui;
#endif // NVE_HAS_NDMF_PLATFORM_SUPPORT

[assembly: ExportsPlugin(typeof(com.github.hkrn.NdmfVrmExporterPlugin))]
[assembly: InternalsVisibleTo("com.github.hkrn.NDMFVRMExporterTests")]
#endif
// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    [AddComponentMenu("NDMF VRM Exporter/VRM Export Description")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/hkrn/ndmf-vrm-exporter")]
    public sealed class NdmfVrmExporterComponent : MonoBehaviour, INDMFEditorOnly
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

        // from 1.1.0
        [NotKeyable] [SerializeField] internal bool extensionFoldout;

        [NotKeyable] [SerializeField] internal bool enableKhrMaterialsVariants = true;

        // from 1.3.0
        [NotKeyable] [SerializeField]
        internal VrmExpressionProperty expressionPresetBlinkLeftBlendShape = VrmExpressionProperty.BlinkLeft;

        [NotKeyable] [SerializeField]
        internal VrmExpressionProperty expressionPresetBlinkRightBlendShape = VrmExpressionProperty.BlinkRight;

        [NotKeyable] [SerializeField] internal bool animationFoldout;

        [NotKeyable] [SerializeField] internal List<AnimationClip> humanoidAnimations = new();

        [NotKeyable] [SerializeField] internal bool experimentalEnableSpringBoneLimit;

        public bool HasAuthor => authors.Count > 0 && !string.IsNullOrWhiteSpace(authors.First());

        public bool HasLicenseUrl =>
            !string.IsNullOrWhiteSpace(licenseUrl) && Uri.TryCreate(licenseUrl, UriKind.Absolute, out _);

        public bool HasAvatarRoot => RuntimeUtil.IsAvatarRoot(gameObject.transform);

        public bool IsSpringBoneLimitEnabled => experimentalEnableSpringBoneLimit;

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
        private SerializedProperty _expressionPresetBlinkLeftBlendShape = null!;
        private SerializedProperty _expressionPresetBlinkRightBlendShape = null!;
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

        // from 1.1.0
        private SerializedProperty _extensionFoldoutProp = null!;
        private SerializedProperty _enableKhrMaterialsVariantsProp = null!;

        // from 1.3.0
        private SerializedProperty _animationFoldoutProp = null!;
        private SerializedProperty _humanoidAnimationsProp = null!;
        private SerializedProperty _experimentalEnableSpringBoneLimit = null!;

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
            _expressionPresetBlinkLeftBlendShape =
                serializedObject.FindProperty(
                    nameof(NdmfVrmExporterComponent.expressionPresetBlinkLeftBlendShape));
            _expressionPresetBlinkRightBlendShape =
                serializedObject.FindProperty(
                    nameof(NdmfVrmExporterComponent.expressionPresetBlinkRightBlendShape));
            _expressionCustomBlendShapes =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.expressionCustomBlendShapes));
            _springBoneFoldoutProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.springBoneFoldout));
            _excludedSpringBoneColliderTransformsProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.excludedSpringBoneColliderTransforms));
            _excludedSpringBoneTransformsProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.excludedSpringBoneTransforms));
            _experimentalEnableSpringBoneLimit =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.experimentalEnableSpringBoneLimit));
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
            _animationFoldoutProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.animationFoldout));
            _humanoidAnimationsProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.humanoidAnimations));
            _extensionFoldoutProp = serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.extensionFoldout));
            _enableKhrMaterialsVariantsProp =
                serializedObject.FindProperty(nameof(NdmfVrmExporterComponent.enableKhrMaterialsVariants));
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
            component.expressionPresetBlinkLeftBlendShape.gameObject = component.gameObject;
            component.expressionPresetBlinkRightBlendShape.gameObject = component.gameObject;
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
#if NVE_HAS_NDMF_PLATFORM_SUPPORT
                EditorGUILayout.HelpBox(Translator._("component.platform-build.suggestion"),
                    MessageType.Info);
                if (GUILayout.Button(Translator._("component.platform-build.open")))
                {
                    ErrorReportWindow.ShowReport(component.gameObject);
                }
#else
                EditorGUILayout.HelpBox(Translator._("component.validation.not-enabled"),
                    MessageType.Warning);
#endif // NVE_HAS_NDMF_PLATFORM_SUPPORT
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
                        var destTexture =
                            intermediateTexture.Blit(1024, 1024, TextureFormat.ARGB32, ColorSpace.Gamma, null);
                        var bytes = destTexture.EncodeToPNG();
                        DestroyImmediate(destTexture);
                        NdmfVrmExporter.ReplaceFile(filePath, bytes);
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
            var animationFoldout = EditorGUILayout.Foldout(_animationFoldoutProp.boolValue,
                Translator._("component.category.animation"));
            if (animationFoldout)
            {
                DrawAnimationOptions();
            }

            EditorGUILayout.Separator();
            var extensionFoldout =
                EditorGUILayout.Foldout(_extensionFoldoutProp.boolValue, Translator._("component.category.extension"));
            if (extensionFoldout)
            {
                DrawExtensionOptions();
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
            _animationFoldoutProp.boolValue = animationFoldout;
            _extensionFoldoutProp.boolValue = extensionFoldout;
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
            DrawToggleLeft(Translator._("component.metadata.information.vrchat.enable-contact-information"),
                _enableContactInformationOnVRChatAutofillProp);

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
            EditorGUILayout.Space();
            if (GUILayout.Button("Uses VRM Public License"))
            {
                _licenseUrlProp.stringValue = vrm.core.Meta.DefaultLicenseUrl;
            }
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
            {
                using var _ = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
                DrawPropertyField(Translator._("component.expression.preset.blink.left"),
                    _expressionPresetBlinkLeftBlendShape);
            }
            {
                using var _ = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
                DrawPropertyField(Translator._("component.expression.preset.blink.right"),
                    _expressionPresetBlinkRightBlendShape);
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
                _expressionPresetBlinkLeftBlendShape.boxedValue = WrapVrmExpression(VrmExpressionProperty.BlinkLeft);
                _expressionPresetBlinkRightBlendShape.boxedValue = WrapVrmExpression(VrmExpressionProperty.BlinkRight);
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
            DrawToggleLeft(Translator._("component.spring-bone.experimental-enable-limit"),
                _experimentalEnableSpringBoneLimit);
        }

        private void DrawConstraintOptions()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(
                _excludedConstraintTransformsProp,
                new GUIContent(Translator._("component.constraint.exclude.constraints")));
            EditorGUI.indentLevel--;
        }


        private void DrawAnimationOptions()
        {
            EditorGUI.indentLevel++;
            var component = (NdmfVrmExporterComponent)target;
            if (component.humanoidAnimations.Any(clip => clip && !clip.humanMotion))
            {
                EditorGUILayout.HelpBox(Translator._("component.validation.animation.non-humanoid"),
                    MessageType.Warning);
            }

            EditorGUILayout.PropertyField(
                _humanoidAnimationsProp,
                new GUIContent(Translator._("component.animation.title")));
            if (GUILayout.Button(Translator._("component.animation.export.button")))
            {
                var outputDirectoryPath =
                    EditorUtility.SaveFolderPanel(Translator._("component.animation.export.dialog.title"), "", "");
                if (!string.IsNullOrEmpty(outputDirectoryPath))
                {
                    ExportAllAnimations(component, outputDirectoryPath);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawExtensionOptions()
        {
            DrawToggleLeft(Translator._("component.extension.enable-khr-materials-variants"),
                _enableKhrMaterialsVariantsProp);
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

        private static void ExportAllAnimations(NdmfVrmExporterComponent component, string outputDirectoryPath)
        {
            var platform = NdmfVrmExporterPlatform.Instance;
            var instance = Instantiate(component.gameObject);
            try
            {
                platform.SkipExportingVrmFile = true;
                using var scope = new OverrideTemporaryDirectoryScope(null);
                var context = AvatarProcessor.ProcessAvatar(instance, platform);
                var numProceededAnimationClips = 0;
                var numTotalAnimationClips = component.humanoidAnimations.Count;
                var progressTitle = Translator._("component.animation.export.progress.title");
                var progressMessage = Translator._("component.animation.export.progress.info");
                var cancelled = false;
                foreach (var animation in component.humanoidAnimations)
                {
                    if (!animation)
                    {
                        numProceededAnimationClips++;
                        continue;
                    }

                    if (!animation.humanMotion)
                    {
                        numProceededAnimationClips++;
                        Debug.LogWarning(
                            $"The animation clip {animation.name} is not human motion and will be skipped");
                        continue;
                    }

                    var progress = numProceededAnimationClips / (float)numTotalAnimationClips;
                    var message = $"{progressMessage} ({numProceededAnimationClips}/{numTotalAnimationClips})";
                    AnimationExporter.Export(context.AvatarRootObject, animation, outputDirectoryPath);
                    if (EditorUtility.DisplayCancelableProgressBar(progressTitle, message, progress))
                    {
                        cancelled = true;
                        break;
                    }

                    numProceededAnimationClips++;
                }

                EditorUtility.ClearProgressBar();
                if (!cancelled)
                {
                    EditorUtility.DisplayDialog(NdmfVrmExporterPlugin.Instance.DisplayName,
                        Translator._("component.animation.export.success"), "OK");
                }
            }
            finally
            {
                platform.SkipExportingVrmFile = false;
                EditorUtility.ClearProgressBar();
                DestroyImmediate(instance);
                // ReSharper disable once RedundantAssignment
                instance = null;
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

        internal static VrmExpressionProperty BlinkLeft => new()
        {
            expressionName = "Blink (Left)",
            isPreset = true,
        };

        internal static VrmExpressionProperty BlinkRight => new()
        {
            expressionName = "Blink (Right)",
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

        // from 1.3.0
        [NotKeyable] [SerializeField] internal SkinnedMeshRenderer? skinnedMeshRenderer;

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
            BaseType.AnimationClip => blendShapeAnimationClip,
            BaseType.BlendShape => !string.IsNullOrEmpty(blendShapeName),
            _ => throw new ArgumentOutOfRangeException(),
        };

        internal void SetFromMmdExpression(string targetName)
        {
            if (!gameObject || !gameObject!.transform)
            {
                return;
            }

            var blendShapeNames = new List<string>();
            SkinnedMeshRenderer? foundSkinnedMeshRenderer = null;
            RetrieveAllBlendShapes(gameObject.transform, (name, innerSkinnedMeshRenderer, _) =>
                {
                    if (name == targetName)
                    {
                        foundSkinnedMeshRenderer = innerSkinnedMeshRenderer;
                    }
                },
                ref blendShapeNames);
            if (!foundSkinnedMeshRenderer)
            {
                return;
            }

            skinnedMeshRenderer = foundSkinnedMeshRenderer;
            blendShapeName = targetName;
            baseType = BaseType.BlendShape;
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
            if (!transform)
            {
                return;
            }

            if (transform.TryGetComponent<SkinnedMeshRenderer>(out var parentSmr))
            {
                var parentMesh = parentSmr.sharedMesh;
                if (parentMesh)
                {
                    var numBlendShapes = parentMesh.blendShapeCount;
                    for (var j = 0; j < numBlendShapes; j++)
                    {
                        callback(parentMesh.GetBlendShapeName(j), parentSmr, blendShapeNames);
                    }
                }
            }

            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                RetrieveAllBlendShapes(child, callback, ref blendShapeNames);
            }
        }

        private List<string?> ExtractAllBlendShapeNamesFromAnimationClip()
        {
            if (!blendShapeAnimationClip)
                return new List<string?>();
            return (from binding in AnimationUtility.GetCurveBindings(blendShapeAnimationClip)
                where binding.propertyName.StartsWith(BlendShapeNamePrefix, StringComparison.Ordinal)
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
                    var skinnedMeshRendererProp =
                        property.FindPropertyRelative(nameof(VrmExpressionProperty.skinnedMeshRenderer));
                    var skinnedMeshRenderer = (SkinnedMeshRenderer)skinnedMeshRendererProp.objectReferenceValue;
                    var transform = skinnedMeshRenderer ? skinnedMeshRenderer.transform : gameObject.transform;
                    var blendShapeName = property.FindPropertyRelative(nameof(VrmExpressionProperty.blendShapeName));

                    StaleAllBlendShapeNamesIfChanged(transform);
                    if (_blendShapeNames.Count == 0 && transform)
                    {
                        VrmExpressionProperty.RetrieveAllBlendShapes(transform, ref _blendShapeNames);
                    }

                    EditorGUI.ObjectField(fieldRect, skinnedMeshRendererProp);
                    fieldRect.y += fieldRect.height;

                    var selectedIndex = blendShapeName.stringValue != null
                        ? _blendShapeNames.IndexOf(blendShapeName.stringValue)
                        : -1;
                    var newIndex = EditorGUI.Popup(fieldRect, selectedIndex, _blendShapeNames.ToArray());
                    fieldRect.y += fieldRect.height;

                    if (newIndex >= 0 && newIndex < _blendShapeNames.Count)
                    {
                        var targetBlendShapeName = _blendShapeNames[newIndex];
                        blendShapeName.stringValue = targetBlendShapeName;
                    }
#if NVE_HAS_MODULAR_AVATAR
                    if (GUI.Button(fieldRect, "Select"))
                    {
                        if (_window)
                        {
                            Object.DestroyImmediate(_window);
                            _window = null;
                        }

                        var window = ScriptableObject.CreateInstance<ui.BlendshapeSelectWindow>();
                        window.AvatarRoot = gameObject;
                        window.OfferBinding = OfferBinding;
                        window.Show();
                        _window = window;

                        void OfferBinding(nadena.dev.modular_avatar.core.BlendshapeBinding binding)
                        {
                            var component = gameObject.GetComponent<NdmfVrmExporterComponent>();
                            var referenceObject = binding.ReferenceMesh.Get(component);
                            skinnedMeshRendererProp.objectReferenceValue =
                                referenceObject.GetComponent<SkinnedMeshRenderer>();
                            blendShapeName.stringValue = binding.Blendshape;
                            property.serializedObject.ApplyModifiedProperties();
                        }
                    }
#endif // NVE_HAS_MODULAR_AVATAR
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
            var height = LineHeight;
            if (!property.FindPropertyRelative(nameof(VrmExpressionProperty.isPreset)).boolValue)
            {
                height += LineHeight;
            }

            switch ((VrmExpressionProperty.BaseType)property
                        .FindPropertyRelative(nameof(VrmExpressionProperty.baseType)).intValue)
            {
                case VrmExpressionProperty.BaseType.BlendShape:
                {
#if NVE_HAS_MODULAR_AVATAR
                    height += LineHeight * 4;
#else
                    height += LineHeight * 3;
#endif // NVE_HAS_MODULAR_AVATAR
                    break;
                }
                case VrmExpressionProperty.BaseType.AnimationClip:
                {
                    height += LineHeight * 2;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (property.FindPropertyRelative(nameof(VrmExpressionProperty.optionsFoldout)).boolValue)
            {
                height += LineHeight * 4;
            }

            return height;
        }

        private void StaleAllBlendShapeNamesIfChanged(Transform transform)
        {
            var sourceId = transform ? transform.GetInstanceID() : 0;
            if (sourceId == _lastTransformInstanceId)
            {
                return;
            }

            _blendShapeNames.Clear();
            _lastTransformInstanceId = sourceId;
        }

        private static float LineHeight => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        private List<string> _blendShapeNames = new();
        private ScriptableObject? _window;
        private int _lastTransformInstanceId;
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
}

#endif // NVE_HAS_NDMF
