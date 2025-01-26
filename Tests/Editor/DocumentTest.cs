using com.github.hkrn.gltf.asset;
using NUnit.Framework;

namespace com.github.hkrn
{
    internal class DocumentTest
    {
        [Test]
        public void SaveAndLoad()
        {
            var expected = new gltf.Root
            {
                Asset = new Asset
                {
                    Version = "2.0",
                }
            };
            var json = gltf.Document.SaveAsString(expected);
            var actual = gltf.Document.LoadFromString(json);
            Assert.IsNotNull(actual);
            Assert.That(actual.Asset.Version, Is.EqualTo("2.0"));
        }
    }
}
