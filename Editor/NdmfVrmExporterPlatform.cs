// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#if NVE_HAS_NDMF_PLATFORM_SUPPORT
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    [NDMFPlatformProvider]
    internal class NdmfVrmExporterPlatform : INDMFPlatformProvider
    {
        public string QualifiedName => NdmfVrmExporterPlugin.Instance.QualifiedName;
        public string DisplayName => "VRM 1.0 (NDMF VRM Exporter)";

        public BuildUIElement CreateBuildUI() => new ui.NdmfVrmExporterBuildUI();

        internal static readonly NdmfVrmExporterPlatform Instance = new();
        internal string LastBuildDirectory { get; set; } = string.Empty;
        internal string LastBuildFileNameWithoutExtension { get; set; } = string.Empty;

        public void InitBuildFromCommonAvatarInfo(BuildContext context, CommonAvatarInfo info)
        {
            context.GetState<NdmfVrmExporterBuildState>().CommonAvatarInfo = info;
        }
    }
}
#endif // NVE_HAS_NDMF_PLATFORM_SUPPORT
