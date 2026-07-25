// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Scripting.APIUpdating;

using nadena.dev.ndmf;
using nadena.dev.ndmf.runtime;

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    [AddComponentMenu("NDMF VRM Exporter/VRM Export Description")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/hkrn/ndmf-vrm-exporter")]
    [MovedFrom(true, sourceNamespace: "com.github.hkrn", sourceAssembly: "NDMFVRMExporter", sourceClassName: "NdmfVrmExporterComponent")]
    public sealed class NdmfVrmExporterComponent : MonoBehaviour, INDMFEditorOnly
    {
        [NotKeyable] [SerializeField] public bool metadataFoldout = true;

        [NotKeyable] [SerializeField] public List<string> authors = new();

        [NotKeyable] [SerializeField] public string? version;

        [NotKeyable] [SerializeField] public string? copyrightInformation;

        [NotKeyable] [SerializeField] public string? contactInformation;

        [NotKeyable] [SerializeField] public List<string> references = new();

        [NotKeyable] [SerializeField] public bool enableContactInformationOnVRChatAutofill = true;

        [NotKeyable] [SerializeField] public string licenseUrl = vrm.core.Meta.DefaultLicenseUrl;

        [NotKeyable] [SerializeField] public string? thirdPartyLicenses;

        [NotKeyable] [SerializeField] public string? otherLicenseUrl;

        [NotKeyable] [SerializeField] public vrm.core.AvatarPermission avatarPermission;

        [NotKeyable] [SerializeField] public vrm.core.CommercialUsage commercialUsage;

        [NotKeyable] [SerializeField] public vrm.core.CreditNotation creditNotation;

        [NotKeyable] [SerializeField] public vrm.core.Modification modification;

        [NotKeyable] [SerializeField] public bool metadataAllowFoldout;

        [NotKeyable] [SerializeField] public VrmUsagePermission allowExcessivelyViolentUsage;

        [NotKeyable] [SerializeField] public VrmUsagePermission allowExcessivelySexualUsage;

        [NotKeyable] [SerializeField] public VrmUsagePermission allowPoliticalOrReligiousUsage;

        [NotKeyable] [SerializeField] public VrmUsagePermission allowAntisocialOrHateUsage;

        [NotKeyable] [SerializeField] public VrmUsagePermission allowRedistribution;

        [NotKeyable] [SerializeField] public Texture2D? thumbnail;

        [NotKeyable] [SerializeField] public bool expressionFoldout = true;

        [NotKeyable] [SerializeField]
        public VrmExpressionProperty expressionPresetHappyBlendShape = VrmExpressionProperty.Happy;

        [NotKeyable] [SerializeField]
        public VrmExpressionProperty expressionPresetAngryBlendShape = VrmExpressionProperty.Angry;

        [NotKeyable] [SerializeField]
        public VrmExpressionProperty expressionPresetSadBlendShape = VrmExpressionProperty.Sad;

        [NotKeyable] [SerializeField]
        public VrmExpressionProperty expressionPresetRelaxedBlendShape = VrmExpressionProperty.Relaxed;

        [NotKeyable] [SerializeField]
        public VrmExpressionProperty expressionPresetSurprisedBlendShape = VrmExpressionProperty.Surprised;

        [NotKeyable] [SerializeField] public bool expressionCustomBlendShapeNameFoldout;

        [NotKeyable] [SerializeField] public List<VrmExpressionProperty> expressionCustomBlendShapes = new();

        [NotKeyable] [SerializeField] public bool springBoneFoldout;

        [NotKeyable] [SerializeField] public List<Transform> excludedSpringBoneColliderTransforms = new();

        [NotKeyable] [SerializeField] public List<Transform> excludedSpringBoneTransforms = new();

        [NotKeyable] [SerializeField] public bool constraintFoldout;

        [NotKeyable] [SerializeField] public List<Transform> excludedConstraintTransforms = new();

        [NotKeyable] [SerializeField] public bool mtoonFoldout;

        [NotKeyable] [SerializeField] public bool enableMToonRimLight;

        [NotKeyable] [SerializeField] public bool enableMToonMatCap;

        [NotKeyable] [SerializeField] public bool enableMToonOutline = true;

        [NotKeyable] [SerializeField] public bool enableBakingAlphaMaskTexture = true;

        [NotKeyable] [SerializeField] public bool debugFoldout;

        [NotKeyable] [SerializeField] public bool makeAllNodeNamesUnique = true;

        [NotKeyable] [SerializeField] public bool enableVertexColorOutput = true;

        [NotKeyable] [SerializeField] public bool disableVertexColorOnLiltoon = true;

        [NotKeyable] [SerializeField] public bool enableGenerateJsonFile;

        [NotKeyable] [SerializeField] public bool deleteTemporaryObjects = true;

        [NotKeyable] [SerializeField] public string? ktxToolPath;

        [NotKeyable] [SerializeField] public int metadataModeSelection;

        [NotKeyable] [SerializeField] public int expressionModeSelection;

        // from 1.1.0
        [NotKeyable] [SerializeField] public bool extensionFoldout;

        [NotKeyable] [SerializeField] public bool enableKhrMaterialsVariants = true;

        // from 1.3.0
        [NotKeyable] [SerializeField]
        public VrmExpressionProperty expressionPresetBlinkLeftBlendShape = VrmExpressionProperty.BlinkLeft;

        [NotKeyable] [SerializeField]
        public VrmExpressionProperty expressionPresetBlinkRightBlendShape = VrmExpressionProperty.BlinkRight;

        [NotKeyable] [SerializeField] public bool animationFoldout;

        [NotKeyable] [SerializeField] public List<AnimationClip> humanoidAnimations = new();

        [NotKeyable] [SerializeField] public bool experimentalEnableSpringBoneLimit;

        // from 1.4.0
        [NotKeyable] [SerializeField] public bool previewFoldout;
        [NotKeyable] [SerializeField] public bool enablePbrCompatibleConversion = true;

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

    public enum VrmUsagePermission
    {
        Disallow,
        Allow,
    }

    [MovedFrom(true, sourceAssembly: "NDMFVRMExporter")]
    [Serializable]
    public class VrmExpressionProperty
    {
        public enum BaseType
        {
            BlendShape,
            AnimationClip,
        };

        public static VrmExpressionProperty Happy => new()
        {
            expressionName = "Happy",
            isPreset = true,
        };

        public static VrmExpressionProperty Angry => new()
        {
            expressionName = "Angry",
            isPreset = true,
        };

        public static VrmExpressionProperty Sad => new()
        {
            expressionName = "Sad",
            isPreset = true,
        };

        public static VrmExpressionProperty Relaxed => new()
        {
            expressionName = "Relaxed",
            isPreset = true,
        };

        public static VrmExpressionProperty Surprised => new()
        {
            expressionName = "Surprised",
            isPreset = true,
        };

        public static VrmExpressionProperty BlinkLeft => new()
        {
            expressionName = "Blink (Left)",
            isPreset = true,
        };

        public static VrmExpressionProperty BlinkRight => new()
        {
            expressionName = "Blink (Right)",
            isPreset = true,
        };

        [NotKeyable] [SerializeField] public string? expressionName;
        [NotKeyable] [SerializeField] public BaseType baseType;
        [NotKeyable] [SerializeField] public GameObject? gameObject;
        [NotKeyable] [SerializeField] public string? blendShapeName;
        [NotKeyable] [SerializeField] public AnimationClip? blendShapeAnimationClip;
        [NotKeyable] [SerializeField] public bool optionsFoldout;
        [NotKeyable] [SerializeField] public vrm.core.ExpressionOverrideType overrideBlink;
        [NotKeyable] [SerializeField] public vrm.core.ExpressionOverrideType overrideLookAt;
        [NotKeyable] [SerializeField] public vrm.core.ExpressionOverrideType overrideMouth;
        [NotKeyable] [SerializeField] public bool isBinary;
        [NotKeyable] [SerializeField] public bool isPreset;

        // from 1.3.0
        [NotKeyable] [SerializeField] public SkinnedMeshRenderer? skinnedMeshRenderer;

        public const string BlendShapeNamePrefix = "blendShape.";

        public string CanonicalExpressionName
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

        public bool IsValid => baseType switch
        {
            BaseType.AnimationClip => blendShapeAnimationClip,
            BaseType.BlendShape => !string.IsNullOrEmpty(blendShapeName),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
