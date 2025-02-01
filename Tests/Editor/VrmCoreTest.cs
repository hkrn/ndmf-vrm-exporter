using Newtonsoft.Json;
using NUnit.Framework;

namespace com.github.hkrn
{
    internal sealed class VrmCoreTest
    {
        [Test]
        public void CoreMetaDefault()
        {
            var core = new vrm.core.Core();
            var meta = core.Meta;
            Assert.That(meta.Modification.GetValueOrDefault(), Is.EqualTo(vrm.core.Modification.Prohibited));
            Assert.That(meta.AvatarPermission.GetValueOrDefault(), Is.EqualTo(vrm.core.AvatarPermission.OnlyAuthor));
            Assert.That(meta.CommercialUsage.GetValueOrDefault(), Is.EqualTo(vrm.core.CommercialUsage.PersonalNonProfit));
            Assert.That(meta.CreditNotation.GetValueOrDefault(), Is.EqualTo(vrm.core.CreditNotation.Required));
            Assert.That(meta.AllowRedistribution.GetValueOrDefault(), Is.False);
            Assert.That(meta.AllowExcessivelySexualUsage.GetValueOrDefault(), Is.False);
            Assert.That(meta.AllowExcessivelyViolentUsage.GetValueOrDefault(), Is.False);
            Assert.That(meta.AllowAntisocialOrHateUsage.GetValueOrDefault(), Is.False);
            Assert.That(meta.AllowPoliticalOrReligiousUsage.GetValueOrDefault(), Is.False);
        }

        [TestCase(vrm.core.AvatarPermission.Everyone)]
        [TestCase(vrm.core.AvatarPermission.OnlyAuthor)]
        [TestCase(vrm.core.AvatarPermission.OnlySeparatelyLicensedPerson)]
        public void AvatarPermissionConverter(vrm.core.AvatarPermission expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<vrm.core.AvatarPermission>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(vrm.core.CommercialUsage.Corporation)]
        [TestCase(vrm.core.CommercialUsage.PersonalProfit)]
        [TestCase(vrm.core.CommercialUsage.PersonalNonProfit)]
        public void CommercialUsageConverter(vrm.core.CommercialUsage expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<vrm.core.CommercialUsage>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(vrm.core.Modification.Prohibited)]
        [TestCase(vrm.core.Modification.AllowModification)]
        [TestCase(vrm.core.Modification.AllowModificationRedistribution)]
        public void ModificationConverter(vrm.core.Modification expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<vrm.core.Modification>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(vrm.core.CreditNotation.Required)]
        [TestCase(vrm.core.CreditNotation.Unnecessary)]
        public void CreditNotationConverter(vrm.core.CreditNotation expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<vrm.core.CreditNotation>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(vrm.core.FirstPersonType.Auto)]
        [TestCase(vrm.core.FirstPersonType.Both)]
        [TestCase(vrm.core.FirstPersonType.FirstPersonOnly)]
        [TestCase(vrm.core.FirstPersonType.ThirdPersonOnly)]
        public void CreditNotationConverter(vrm.core.FirstPersonType expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<vrm.core.FirstPersonType>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(vrm.core.ExpressionOverrideType.Blend)]
        [TestCase(vrm.core.ExpressionOverrideType.Block)]
        [TestCase(vrm.core.ExpressionOverrideType.None)]
        public void CreditNotationConverter(vrm.core.ExpressionOverrideType expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<vrm.core.ExpressionOverrideType>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(vrm.core.MaterialColorType.Color)]
        [TestCase(vrm.core.MaterialColorType.EmissionColor)]
        [TestCase(vrm.core.MaterialColorType.OutlineColor)]
        [TestCase(vrm.core.MaterialColorType.RimColor)]
        [TestCase(vrm.core.MaterialColorType.ShadeColor)]
        public void CreditNotationConverter(vrm.core.MaterialColorType expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<vrm.core.MaterialColorType>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(vrm.core.LookAtType.Bone)]
        [TestCase(vrm.core.LookAtType.Expression)]
        public void CreditNotationConverter(vrm.core.LookAtType expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<vrm.core.LookAtType>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
