// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#if NVE_HAS_NDMF_PLATFORM_SUPPORT
using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    [NDMFPlatformProvider]
    internal class NdmfVrmExporterPlatform : INDMFPlatformProvider
    {
        public struct BuildLocationKey : IEquatable<BuildLocationKey>
        {
            public int SceneBuildId { get; set; }
            public int AvatarId { get; set; }

            public bool Equals(BuildLocationKey other)
            {
                return SceneBuildId == other.SceneBuildId && AvatarId == other.AvatarId;
            }

            public override bool Equals(object obj)
            {
                return obj is BuildLocationKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(SceneBuildId, AvatarId);
            }
        }

        public struct BuildLocationValue
        {
            public string Directory { get; set; }
            public string Filename { get; set; }
        }

        public string QualifiedName => NdmfVrmExporterPlugin.Instance.QualifiedName;
        public string DisplayName => "VRM 1.0 (NDMF VRM Exporter)";

        public BuildUIElement CreateBuildUI() => new ui.NdmfVrmExporterBuildUI();

        internal static readonly NdmfVrmExporterPlatform Instance = new();
        internal Dictionary<BuildLocationKey, BuildLocationValue> LastBuildLocations { get; set; } = new();

        public void InitBuildFromCommonAvatarInfo(BuildContext context, CommonAvatarInfo info)
        {
            context.GetState<NdmfVrmExporterBuildState>().CommonAvatarInfo = info;
        }
    }
}
#endif // NVE_HAS_NDMF_PLATFORM_SUPPORT
