// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

using NUnit.Framework;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    internal sealed class ComponentTest
    {
        [Test]
        public void Author()
        {
            var go = new GameObject();
            var component = go.AddComponent<NdmfVrmExporterComponent>();
            Assert.That(component.HasAuthor, Is.False);
            component.authors.Add(string.Empty);
            Assert.That(component.HasAuthor, Is.False);
            component.authors[0] = "    ";
            Assert.That(component.HasAuthor, Is.False);
            component.authors.Add("bar");
            Assert.That(component.HasAuthor, Is.False);
            component.authors[0] = "foo";
            Assert.That(component.HasAuthor, Is.True);
        }

        [Test]
        public void LicenseURL()
        {
            var go = new GameObject();
            var component = go.AddComponent<NdmfVrmExporterComponent>();
            Assert.That(component.HasLicenseUrl, Is.True);
            Assert.That(component.licenseUrl, Is.EqualTo(vrm.core.Meta.DefaultLicenseUrl));
            component.licenseUrl = string.Empty;
            Assert.That(component.HasLicenseUrl, Is.False);
            component.licenseUrl = "no-such-license-url";
            Assert.That(component.HasLicenseUrl, Is.False);
        }

        [Test]
        public void Permissions()
        {
            var go = new GameObject();
            var component = go.AddComponent<NdmfVrmExporterComponent>();
            Assert.That(component.allowAntisocialOrHateUsage, Is.EqualTo(VrmUsagePermission.Disallow));
            Assert.That(component.allowExcessivelySexualUsage, Is.EqualTo(VrmUsagePermission.Disallow));
            Assert.That(component.allowExcessivelyViolentUsage, Is.EqualTo(VrmUsagePermission.Disallow));
            Assert.That(component.allowPoliticalOrReligiousUsage, Is.EqualTo(VrmUsagePermission.Disallow));
            Assert.That(component.allowRedistribution, Is.EqualTo(VrmUsagePermission.Disallow));
        }
    }
}
