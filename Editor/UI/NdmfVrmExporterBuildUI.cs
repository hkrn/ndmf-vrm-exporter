// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#if NVE_HAS_NDMF_PLATFORM_SUPPORT
using System.IO;
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace com.github.hkrn.ui
{
    internal class NdmfVrmExporterBuildUI : BuildUIElement
    {
        public NdmfVrmExporterBuildUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"Packages/{NdmfVrmExporter.PackageJson.Name}/Editor/UI/Resources/NDMFVRMExporter.uxml");
            var rootContainer = visualTree.CloneTree();
            Add(rootContainer);
            _exportButton = rootContainer.Q<Button>("export");
            _messageLabel = rootContainer.Q<Label>("message");
            _exportButton.clicked += () =>
            {
                _exportButton.SetEnabled(false);
                _messageLabel.style.display = DisplayStyle.None;
                var buildLocationKey = GetBuildLocationKey();
                var platformInstance = NdmfVrmExporterPlatform.Instance;
                if (!platformInstance.LastBuildLocations.TryGetValue(buildLocationKey, out var buildLocationValue))
                {
                    buildLocationValue = new NdmfVrmExporterPlatform.BuildLocationValue()
                    {
                        Filename = AvatarRoot.name
                    };
                }

                var path = EditorUtility.SaveFilePanel("Export VRM File", buildLocationValue.Directory,
                    buildLocationValue.Filename, "vrm");
                if (string.IsNullOrEmpty(path))
                {
                    Debug.Log("Exporting VRM has been cancelled");
                    _exportButton.SetEnabled(true);
                    return;
                }

                Build(platformInstance, path);
            };
        }

        public override GameObject AvatarRoot
        {
            get => _avatarRoot;
            set
            {
                NdmfVrmExporterComponent component = null;
                var enabled = value && value.TryGetComponent(out component)
                                    && component.HasAuthor && component.HasLicenseUrl;
                _exportButton.SetEnabled(enabled);
                _messageLabel.style.display = enabled ? DisplayStyle.None : DisplayStyle.Flex;
                if (!enabled)
                {
                    if (component)
                    {
                        if (!component.HasAuthor)
                        {
                            _messageLabel.text = Translator._("component.validation.author");
                        }
                        else if (!component.HasLicenseUrl)
                        {
                            _messageLabel.text = Translator._("component.validation.license-url");
                        }
                    }
                    else
                    {
                        _messageLabel.text = Translator._("component.platform-build.disabled");
                    }
                }

                _avatarRoot = value;
            }
        }

        private void Build(NdmfVrmExporterPlatform platform, string path)
        {
            GameObject avatarRoot = null;
            try
            {
                avatarRoot = Object.Instantiate(AvatarRoot);
                avatarRoot.name = avatarRoot.name[..^"(clone)".Length];
                using var scope = new AmbientPlatform.Scope(platform);
                using var scope2 = new OverrideTemporaryDirectoryScope(null);
                var buildContext = AvatarProcessor.ProcessAvatar(avatarRoot, platform);
                if (!avatarRoot.TryGetComponent<NdmfVrmExporterComponent>(out var component))
                {
                    ErrorReport.ReportError(Translator.Instance, ErrorSeverity.NonFatal,
                        "component.runtime.error.validation.not-attached");
                    return;
                }

                var outputDirectory = Path.GetDirectoryName(path) ?? string.Empty;
                var outputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
                var baseOutputPath = Path.Join(outputDirectory, outputFileNameWithoutExtension);
                var workingDirectoryPath = AssetPathUtils.GetTempPath(avatarRoot);
                try
                {
                    NdmfVrmExporterPlugin.ExportVrmFile(component, buildContext, baseOutputPath,
                        workingDirectoryPath);
                    var buildLocationKey = GetBuildLocationKey();
                    platform.LastBuildLocations[buildLocationKey] = new NdmfVrmExporterPlatform.BuildLocationValue
                    {
                        Directory = outputDirectory,
                        Filename = outputFileNameWithoutExtension,
                    };
                    EditorUtility.DisplayDialog(NdmfVrmExporterPlugin.Instance.DisplayName,
                        Translator._("component.platform-build.success"), "OK");
                }
                catch (NdmfVrmExporterPlugin.ValidationException e)
                {
                    if (e.Extras != null)
                    {
                        ErrorReport.ReportError(Translator.Instance, ErrorSeverity.NonFatal,
                            e.Key, e.Extras);
                    }
                    else
                    {
                        _messageLabel.style.display = DisplayStyle.Flex;
                        _messageLabel.text = e.Message;
                    }
                }
            }
            finally
            {
                if (avatarRoot)
                {
                    Object.DestroyImmediate(avatarRoot);
                }

                _exportButton.SetEnabled(true);
            }
        }

        private NdmfVrmExporterPlatform.BuildLocationKey GetBuildLocationKey()
        {
            return new NdmfVrmExporterPlatform.BuildLocationKey
            {
                AvatarId = AvatarRoot.GetInstanceID(),
                SceneBuildId = SceneManager.GetActiveScene().buildIndex,
            };
        }

        private readonly Button _exportButton;
        private readonly Label _messageLabel;
        private GameObject _avatarRoot;
    }
}
#endif // NVE_HAS_NDMF_PLATFORM_SUPPORT
