// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

using Newtonsoft.Json;
using NUnit.Framework;

namespace com.github.hkrn
{
    internal sealed class VrmMToonTest
    {
        [TestCase(vrm.mtoon.OutlineWidthMode.None)]
        [TestCase(vrm.mtoon.OutlineWidthMode.ScreenCoordinates)]
        [TestCase(vrm.mtoon.OutlineWidthMode.WorldCoordinates)]
        public void OutlineWidthModeConverter(vrm.mtoon.OutlineWidthMode expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<vrm.mtoon.OutlineWidthMode>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
