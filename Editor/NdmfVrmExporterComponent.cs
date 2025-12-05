// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using nadena.dev.ndmf.runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
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
            BaseType.AnimationClip => blendShapeAnimationClip,
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
                where binding.propertyName.StartsWith(BlendShapeNamePrefix, StringComparison.Ordinal)
                let name = binding.propertyName[BlendShapeNamePrefix.Length..]
                let curve = AnimationUtility.GetEditorCurve(blendShapeAnimationClip, binding)
                from keyframe in curve.keys
                where !(keyframe.time > 0.0f) && !Mathf.Approximately(keyframe.value, 0.0f)
                select name).ToList();
        }
    }

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
}
