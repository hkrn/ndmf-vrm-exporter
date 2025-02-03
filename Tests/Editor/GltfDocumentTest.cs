// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    internal sealed class GltfDocumentTest
    {
        [TestCase(0u)]
        [TestCase(42u)]
        public void ObjectID(uint value)
        {
            var id = new gltf.ObjectID(value);
            Assert.That(id.IsNull, Is.False);
            Assert.That(id.ID, Is.EqualTo(value));
        }

        [Test]
        public void ObjectID_Null()
        {
            var id = gltf.ObjectID.Null;
            Assert.That(id.IsNull, Is.True);
            Assert.That(id.ID, Is.EqualTo(uint.MaxValue));
        }

        [TestCase(42u)]
        public void ObjectIDConverter(uint value)
        {
            var expected = new gltf.ObjectID(value);
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<gltf.ObjectID>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("glTF")]
        public void UnicodeStringConverter(string value)
        {
            var expected = new gltf.UnicodeString(value);
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<gltf.UnicodeString>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(1.0f, 0.0f, 0.0f)]
        [TestCase(0.0f, 1.0f, 0.0f)]
        [TestCase(0.0f, 0.0f, 1.0f)]
        public void Vector3Converter(float x, float y, float z)
        {
            var expected = new Vector3(x, y, z);
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<Vector3>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(1.0f, 0.0f, 0.0f, 0.0f)]
        [TestCase(0.0f, 1.0f, 0.0f, 0.0f)]
        [TestCase(0.0f, 0.0f, 1.0f, 0.0f)]
        [TestCase(0.0f, 0.0f, 0.0f, 1.0f)]
        public void Vector4Converter(float x, float y, float z, float w)
        {
            var expected = new Vector4(x, y, z, w);
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<Vector4>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(1.0f, 0.0f, 0.0f, 0.0f)]
        [TestCase(0.0f, 1.0f, 0.0f, 0.0f)]
        [TestCase(0.0f, 0.0f, 1.0f, 0.0f)]
        [TestCase(0.0f, 0.0f, 0.0f, 1.0f)]
        public void QuaternionConverter(float x, float y, float z, float w)
        {
            var expected = new Quaternion(x, y, z, w);
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result =
                JsonConvert.DeserializeObject<Quaternion>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Matrix4Converter()
        {
            var expected = Matrix4x4.Identity;
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result =
                JsonConvert.DeserializeObject<Matrix4x4>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(gltf.accessor.ComponentType.Byte)]
        [TestCase(gltf.accessor.ComponentType.Float)]
        [TestCase(gltf.accessor.ComponentType.Short)]
        [TestCase(gltf.accessor.ComponentType.UnsignedByte)]
        [TestCase(gltf.accessor.ComponentType.UnsignedInt)]
        [TestCase(gltf.accessor.ComponentType.UnsignedShort)]
        public void AccessorComponentTypeConverter(gltf.accessor.ComponentType expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result =
                JsonConvert.DeserializeObject<gltf.accessor.ComponentType>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(gltf.accessor.Type.Mat2)]
        [TestCase(gltf.accessor.Type.Mat3)]
        [TestCase(gltf.accessor.Type.Mat4)]
        [TestCase(gltf.accessor.Type.Scalar)]
        [TestCase(gltf.accessor.Type.Vec2)]
        [TestCase(gltf.accessor.Type.Vec3)]
        [TestCase(gltf.accessor.Type.Vec4)]
        public void AccessorTypeConverter(gltf.accessor.Type expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<gltf.accessor.Type>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(gltf.animation.Interpolation.Linear)]
        [TestCase(gltf.animation.Interpolation.Step)]
        [TestCase(gltf.animation.Interpolation.CubicSpline)]
        public void AnimationInterpolationConverter(gltf.animation.Interpolation expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result =
                JsonConvert.DeserializeObject<gltf.animation.Interpolation>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(gltf.animation.Path.Pointer)]
        [TestCase(gltf.animation.Path.Rotation)]
        [TestCase(gltf.animation.Path.Scale)]
        [TestCase(gltf.animation.Path.Translation)]
        [TestCase(gltf.animation.Path.Weights)]
        public void AnimationPathConverter(gltf.animation.Path expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<gltf.animation.Path>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(gltf.buffer.Target.ArrayBuffer)]
        [TestCase(gltf.buffer.Target.ElementArrayBuffer)]
        public void BufferTargetConverter(gltf.buffer.Target expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<gltf.buffer.Target>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(gltf.material.AlphaMode.Blend)]
        [TestCase(gltf.material.AlphaMode.Mask)]
        [TestCase(gltf.material.AlphaMode.Opaque)]
        public void MaterialAlphaModeConverter(gltf.material.AlphaMode expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<gltf.material.AlphaMode>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(gltf.material.TextureFilterMode.Linear)]
        [TestCase(gltf.material.TextureFilterMode.Nearest)]
        [TestCase(gltf.material.TextureFilterMode.LinearMipmapLinear)]
        [TestCase(gltf.material.TextureFilterMode.LinearMipmapNearest)]
        [TestCase(gltf.material.TextureFilterMode.NearestMipmapLinear)]
        [TestCase(gltf.material.TextureFilterMode.NearestMipmapNearest)]
        public void MaterialTextureFilterModeConverter(gltf.material.TextureFilterMode expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result =
                JsonConvert.DeserializeObject<gltf.material.TextureFilterMode>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(gltf.material.TextureWrapMode.Repeat)]
        [TestCase(gltf.material.TextureWrapMode.MirroredRepeat)]
        [TestCase(gltf.material.TextureWrapMode.ClampToEdge)]
        public void MaterialTextureWrapModeConverter(gltf.material.TextureWrapMode expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result =
                JsonConvert.DeserializeObject<gltf.material.TextureWrapMode>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(gltf.mesh.PrimitiveMode.Lines)]
        [TestCase(gltf.mesh.PrimitiveMode.Point)]
        [TestCase(gltf.mesh.PrimitiveMode.Triangles)]
        [TestCase(gltf.mesh.PrimitiveMode.LineLoop)]
        [TestCase(gltf.mesh.PrimitiveMode.LineStrip)]
        [TestCase(gltf.mesh.PrimitiveMode.TriangleFan)]
        [TestCase(gltf.mesh.PrimitiveMode.TriangleStrip)]
        public void MeshPrimitiveModeConverter(gltf.mesh.PrimitiveMode expected)
        {
            var json = JsonConvert.SerializeObject(expected, gltf.Document.SerializerOptions);
            var result = JsonConvert.DeserializeObject<gltf.mesh.PrimitiveMode>(json, gltf.Document.SerializerOptions);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ExporterSparseBuilder()
        {
            var builder = new gltf.exporter.Exporter.SparseBuilder(ImmutableList.CreateRange(new[]
            {
                -Vector3.UnitX,
                -Vector3.UnitY,
                -Vector3.UnitZ,
                Vector3.Zero,
                Vector3.UnitX,
                Vector3.UnitY,
                Vector3.UnitZ,
            }));
            Assert.That(builder.Min, Is.EqualTo(-Vector3.One));
            Assert.That(builder.Max, Is.EqualTo(Vector3.One));
            Assert.That(builder.BaseAccessorCount, Is.EqualTo(7));
            builder.Add(Vector3.One, 5);
            builder.Add(-Vector3.One, 1);
            builder.Add(Vector3.Zero, 3);
            var (indices, values) = builder.Build();
            Assert.That(indices, Is.EquivalentTo(new[] { 1, 3, 5 }));
            Assert.That(values, Is.EquivalentTo(new[]
            {
                -Vector3.One,
                Vector3.Zero,
                Vector3.One,
            }));
        }

        [TestCase("", "", 0)]
        [TestCase("0", "0   ", 4)]
        [TestCase("{}", "{}  ", 4)]
        [TestCase("012", "012 ", 4)]
        [TestCase("true", "true", 4)]
        [TestCase("false", "false   ", 8)]
        public void ExporterExport_JSON(string json, string expectedJson, int expectedJsonSize)
        {
            using var exporter = new gltf.exporter.Exporter();
            using var outputStream = new MemoryStream();
            exporter.Export(json, outputStream);
            var bytes = outputStream.GetBuffer();
            var inputStream = new MemoryStream(bytes);
            using var reader = new BinaryReader(inputStream);
            Assert.That(reader.ReadBytes(4), Is.EqualTo("glTF"));
            Assert.That(reader.ReadInt32(), Is.EqualTo(2));
            Assert.That(reader.ReadInt32(), Is.EqualTo(28 + expectedJsonSize));
            Assert.That(reader.ReadInt32(), Is.EqualTo(expectedJsonSize));
            Assert.That(reader.ReadBytes(4), Is.EqualTo("JSON"));
            Assert.That(reader.ReadBytes(expectedJsonSize), Is.EqualTo(expectedJson));
            Assert.That(reader.ReadInt32(), Is.EqualTo(0));
            Assert.That(reader.ReadBytes(4), Is.EqualTo("BIN\0"));
        }

        [TestCase("",  "", 0)]
        [TestCase("0",  "0\0\0\0", 4)]
        [TestCase("01",  "01\0\0", 4)]
        [TestCase("012",  "012\0", 4)]
        [TestCase("0123",  "0123", 4)]
        [TestCase("01234",  "01234\0\0\0", 8)]
        public void ExporterExport_BIN(string bin, string expectedBin, int expectedBinSize)
        {
            var root = new gltf.Root
            {
                BufferViews = new List<gltf.buffer.BufferView>(),
                Images = new List<gltf.buffer.Image>(),
                Samplers = new List<gltf.material.Sampler>(),
                Textures = new List<gltf.material.Texture>(),
            };
            using var exporter = new gltf.exporter.Exporter();
            exporter.CreateSampledTexture(root, new gltf.exporter.SampledTextureUnit
            {
                Data = Encoding.ASCII.GetBytes(bin),
            });
            using var outputStream = new MemoryStream();
            exporter.Export("{}", outputStream);
            var bytes = outputStream.GetBuffer();
            var inputStream = new MemoryStream(bytes);
            using var reader = new BinaryReader(inputStream);
            Assert.That(reader.ReadBytes(4), Is.EqualTo("glTF"));
            Assert.That(reader.ReadInt32(), Is.EqualTo(2));
            Assert.That(reader.ReadInt32(), Is.EqualTo(32 + expectedBinSize));
            Assert.That(reader.ReadInt32(), Is.EqualTo(4));
            Assert.That(reader.ReadBytes(4), Is.EqualTo("JSON"));
            Assert.That(reader.ReadBytes(4), Is.EqualTo("{}  "));
            Assert.That(reader.ReadInt32(), Is.EqualTo(expectedBinSize));
            Assert.That(reader.ReadBytes(4), Is.EqualTo("BIN\0"));
            Assert.That(reader.ReadBytes(expectedBinSize), Is.EqualTo(expectedBin));
        }

        [Test]
        public void ExporterCreateSampledTexture()
        {
            var root = new gltf.Root
            {
                BufferViews = new List<gltf.buffer.BufferView>(),
                Images = new List<gltf.buffer.Image>(),
                Samplers = new List<gltf.material.Sampler>(),
                Textures = new List<gltf.material.Texture>(),
            };
            using var exporter = new gltf.exporter.Exporter();
            var inputData = new byte[] { 0, 0, 0, 0 };
            var textureId = exporter.CreateSampledTexture(root, new gltf.exporter.SampledTextureUnit
            {
                Name = new gltf.UnicodeString("sampledTexture"),
                MimeType = "image/png",
                Data = inputData,
                MagFilter = gltf.material.TextureFilterMode.Linear,
                MinFilter = gltf.material.TextureFilterMode.Linear,
                WrapS = gltf.material.TextureWrapMode.Repeat,
                WrapT = gltf.material.TextureWrapMode.Repeat,
            });
            Assert.That(textureId.ID, Is.Zero);
            Assert.That(root.BufferViews.Count, Is.EqualTo(1));
            var bufferView = root.BufferViews.First();
            Assert.That(bufferView.Buffer.ID, Is.Zero);
            Assert.That(bufferView.ByteOffset, Is.Zero);
            Assert.That(bufferView.ByteLength, Is.EqualTo(4));
            Assert.That(bufferView.Name!.Value, Is.EqualTo("sampledTexture"));
            Assert.That(root.Images.Count, Is.EqualTo(1));
            var image = root.Images.First();
            Assert.That(image.BufferView!.Value.ID, Is.Zero);
            Assert.That(image.Name!.Value, Is.EqualTo("sampledTexture"));
            Assert.That(image.MimeType, Is.EqualTo("image/png"));
            Assert.That(root.Samplers.Count, Is.EqualTo(1));
            var sampler = root.Samplers.First();
            Assert.That(sampler.MagFilter, Is.EqualTo(gltf.material.TextureFilterMode.Linear));
            Assert.That(sampler.MinFilter, Is.EqualTo(gltf.material.TextureFilterMode.Linear));
            Assert.That(sampler.WrapS, Is.EqualTo(gltf.material.TextureWrapMode.Repeat));
            Assert.That(sampler.WrapT, Is.EqualTo(gltf.material.TextureWrapMode.Repeat));
            Assert.That(root.Samplers.Count, Is.EqualTo(1));
            var texture = root.Textures.First();
            Assert.That(texture.Sampler!.Value.ID, Is.Zero);
            Assert.That(texture.Source!.Value.ID, Is.Zero);
            Assert.That(texture.Name!.Value, Is.EqualTo("sampledTexture"));
            Assert.That(exporter.Length, Is.EqualTo(inputData.Length));
        }

        [Test]
        public void ExporterCreateMatrixAccessor()
        {
            var root = new gltf.Root
            {
                Accessors = new List<gltf.accessor.Accessor>(),
                BufferViews = new List<gltf.buffer.BufferView>(),
            };
            using var exporter = new gltf.exporter.Exporter();
            var accessorId = exporter.CreateMatrix4Accessor(root, "mat4", new[] { Matrix4x4.Identity });
            Assert.That(accessorId.ID, Is.Zero);
            var accessor = root.Accessors.First();
            Assert.That(accessor.Count, Is.EqualTo(1));
            Assert.That(accessor.ComponentType, Is.EqualTo(gltf.accessor.ComponentType.Float));
            Assert.That(accessor.Type, Is.EqualTo(gltf.accessor.Type.Mat4));
            Assert.That(accessor.Name!.Value, Is.EqualTo("mat4_Accessor0"));
            var bufferView = root.BufferViews.First();
            Assert.That(bufferView.Buffer.ID, Is.Zero);
            Assert.That(bufferView.ByteLength, Is.EqualTo(64));
            Assert.That(bufferView.ByteStride, Is.Null);
            Assert.That(bufferView.Name!.Value, Is.EqualTo("mat4_Accessor0"));
            Assert.That(exporter.Length, Is.EqualTo(64));
        }

        [Test]
        public void ExporterCreateAccessorIndices()
        {
            var root = new gltf.Root
            {
                Accessors = new List<gltf.accessor.Accessor>(),
                BufferViews = new List<gltf.buffer.BufferView>(),
            };
            using var exporter = new gltf.exporter.Exporter();
            Assert.That(exporter.CreateAccessorJoints(root, new gltf.UnicodeString("empty"),
                new gltf.exporter.JointUnit[] { }), Is.Null);
            var accessorId = exporter.CreateAccessorJoints(root, new gltf.UnicodeString("joints"), new[]
            {
                new gltf.exporter.JointUnit
                {
                    X = 1,
                    Y = 2,
                    Z = 3,
                    W = 4,
                }
            });
            Assert.That(accessorId, Is.Not.Null);
            Assert.That(accessorId!.Value.ID, Is.Zero);
            var accessor = root.Accessors.First();
            Assert.That(accessor.Count, Is.EqualTo(1));
            Assert.That(accessor.ComponentType, Is.EqualTo(gltf.accessor.ComponentType.UnsignedShort));
            Assert.That(accessor.Type, Is.EqualTo(gltf.accessor.Type.Vec4));
            Assert.That(accessor.Name!.Value, Is.EqualTo("joints_Accessor0"));
            var bufferView = root.BufferViews.First();
            Assert.That(bufferView.Buffer.ID, Is.Zero);
            Assert.That(bufferView.ByteLength, Is.EqualTo(8));
            Assert.That(bufferView.ByteStride, Is.EqualTo(8));
            Assert.That(bufferView.Name!.Value, Is.EqualTo("joints_Accessor0"));
            Assert.That(exporter.Length, Is.EqualTo(8));
        }

        [Test]
        public void ExporterCreateMesh()
        {
            var root = new gltf.Root
            {
                Accessors = new List<gltf.accessor.Accessor>(),
                BufferViews = new List<gltf.buffer.BufferView>(),
                Meshes = new List<gltf.mesh.Mesh>(),
            };
            using var exporter = new gltf.exporter.Exporter();
            var accessorId = exporter.CreateMesh(root, new gltf.exporter.MeshUnit
            {
                Name = new gltf.UnicodeString("mesh"),
                Primitives =
                {
                    new gltf.exporter.PrimitiveUnit
                    {
                        Indices = new[] { 0u, 1u, 2u }, // 12
                        Material = new gltf.ObjectID(0),
                        PrimitiveMode = gltf.mesh.PrimitiveMode.Triangles,
                    }
                },
                Positions = new[] { Vector3.Zero, Vector3.Zero, Vector3.Zero }, // 36
                Normals = new[] { Vector3.Zero, Vector3.Zero, Vector3.Zero }, // 36
                Colors = new[] { Vector4.One, Vector4.One, Vector4.One }, // 48
                TexCoords0 = new[] { Vector2.Zero, Vector2.Zero, Vector2.Zero }, // 24
                TexCoords1 = new[] { Vector2.Zero, Vector2.Zero, Vector2.Zero }, // 24
                Joints = new[] // 24
                {
                    new gltf.exporter.JointUnit(),
                    new gltf.exporter.JointUnit(),
                    new gltf.exporter.JointUnit(),
                },
                Weights = new[] { Vector4.Zero, Vector4.Zero, Vector4.Zero }, // 48
            });
            Assert.That(accessorId.ID, Is.Zero);
            Assert.That(root.Accessors.Count, Is.EqualTo(8));
            Assert.That(root.BufferViews.Count, Is.EqualTo(8));
            Assert.That(exporter.Length, Is.EqualTo(252));
            var mesh = root.Meshes.First();
            var primitive = mesh.Primitives!.First();
            Assert.That(primitive.Attributes, Is.EquivalentTo(new Dictionary<string, gltf.ObjectID>
            {
                { "POSITION", new gltf.ObjectID(0) },
                { "NORMAL", new gltf.ObjectID(1) },
                { "COLOR_0", new gltf.ObjectID(2) },
                { "TEXCOORD_0", new gltf.ObjectID(3) },
                { "TEXCOORD_1", new gltf.ObjectID(4) },
                { "JOINTS_0", new gltf.ObjectID(5) },
                { "WEIGHTS_0", new gltf.ObjectID(6) },
            }));
            Assert.That(primitive.Indices!.Value.ID, Is.EqualTo(7));
            Assert.That(primitive.Material!.Value.ID, Is.Zero);
            Assert.That(primitive.Mode, Is.EqualTo(gltf.mesh.PrimitiveMode.Triangles));
        }

        [Test]
        public void ExporterCreateAccessorVector2()
        {
            var root = new gltf.Root
            {
                Accessors = new List<gltf.accessor.Accessor>(),
                BufferViews = new List<gltf.buffer.BufferView>(),
            };
            using var exporter = new gltf.exporter.Exporter();
            Assert.That(exporter.CreateAccessorVector2(root, new gltf.UnicodeString("empty"), new Vector2[] { }),
                Is.Null);
            var accessorId =
                exporter.CreateAccessorVector2(root, new gltf.UnicodeString("vec2"), new[] { Vector2.Zero });
            Assert.That(accessorId, Is.Not.Null);
            Assert.That(accessorId!.Value.ID, Is.Zero);
            var accessor = root.Accessors.First();
            Assert.That(accessor.Count, Is.EqualTo(1));
            Assert.That(accessor.ComponentType, Is.EqualTo(gltf.accessor.ComponentType.Float));
            Assert.That(accessor.Type, Is.EqualTo(gltf.accessor.Type.Vec2));
            Assert.That(accessor.Name!.Value, Is.EqualTo("vec2_Accessor0"));
            var bufferView = root.BufferViews.First();
            Assert.That(bufferView.Buffer.ID, Is.Zero);
            Assert.That(bufferView.ByteLength, Is.EqualTo(8));
            Assert.That(bufferView.ByteStride, Is.EqualTo(8));
            Assert.That(bufferView.Name!.Value, Is.EqualTo("vec2_Accessor0"));
            Assert.That(exporter.Length, Is.EqualTo(8));
        }

        [Test]
        public void ExporterCreateAccessorVector3()
        {
            var root = new gltf.Root
            {
                Accessors = new List<gltf.accessor.Accessor>(),
                BufferViews = new List<gltf.buffer.BufferView>(),
            };
            using var exporter = new gltf.exporter.Exporter();
            Assert.That(exporter.CreateAccessorVector3(root, new gltf.UnicodeString("empty"), new Vector3[] { }),
                Is.Null);
            var accessorId =
                exporter.CreateAccessorVector3(root, new gltf.UnicodeString("vec3"), new[] { Vector3.Zero });
            Assert.That(accessorId, Is.Not.Null);
            Assert.That(accessorId!.Value.ID, Is.Zero);
            var accessor = root.Accessors.First();
            Assert.That(accessor.Count, Is.EqualTo(1));
            Assert.That(accessor.ComponentType, Is.EqualTo(gltf.accessor.ComponentType.Float));
            Assert.That(accessor.Type, Is.EqualTo(gltf.accessor.Type.Vec3));
            Assert.That(accessor.Name!.Value, Is.EqualTo("vec3_Accessor0"));
            var bufferView = root.BufferViews.First();
            Assert.That(bufferView.Buffer.ID, Is.Zero);
            Assert.That(bufferView.ByteLength, Is.EqualTo(12));
            Assert.That(bufferView.ByteStride, Is.EqualTo(12));
            Assert.That(bufferView.Name!.Value, Is.EqualTo("vec3_Accessor0"));
            Assert.That(exporter.Length, Is.EqualTo(12));
        }

        [Test]
        public void ExporterCreateAccessorVector4()
        {
            var root = new gltf.Root
            {
                Accessors = new List<gltf.accessor.Accessor>(),
                BufferViews = new List<gltf.buffer.BufferView>(),
            };
            using var exporter = new gltf.exporter.Exporter();
            Assert.That(exporter.CreateAccessorVector4(root, new gltf.UnicodeString("empty"), new Vector4[] { }),
                Is.Null);
            var accessorId =
                exporter.CreateAccessorVector4(root, new gltf.UnicodeString("vec4"), new[] { Vector4.Zero });
            Assert.That(accessorId, Is.Not.Null);
            Assert.That(accessorId!.Value.ID, Is.Zero);
            var accessor = root.Accessors.First();
            Assert.That(accessor.Count, Is.EqualTo(1));
            Assert.That(accessor.ComponentType, Is.EqualTo(gltf.accessor.ComponentType.Float));
            Assert.That(accessor.Type, Is.EqualTo(gltf.accessor.Type.Vec4));
            Assert.That(accessor.Name!.Value, Is.EqualTo("vec4_Accessor0"));
            var bufferView = root.BufferViews.First();
            Assert.That(bufferView.Buffer.ID, Is.Zero);
            Assert.That(bufferView.ByteLength, Is.EqualTo(16));
            Assert.That(bufferView.ByteStride, Is.EqualTo(16));
            Assert.That(bufferView.Name!.Value, Is.EqualTo("vec4_Accessor0"));
            Assert.That(exporter.Length, Is.EqualTo(16));
        }

        [Test]
        public void ExporterCreateSparseAccessorVector3()
        {
            var root = new gltf.Root
            {
                Accessors = new List<gltf.accessor.Accessor>(),
                BufferViews = new List<gltf.buffer.BufferView>(),
            };
            using var exporter = new gltf.exporter.Exporter();
            Assert.That(
                exporter.CreateSparseAccessorVector3(root, new gltf.UnicodeString("empty"),
                    new gltf.exporter.Exporter.SparseBuilder(ImmutableArray<Vector3>.Empty)), Is.Null);
            var builder = new gltf.exporter.Exporter.SparseBuilder(ImmutableArray.CreateRange(new[]
            {
                Vector3.Zero,
                Vector3.Zero,
                Vector3.Zero,
            }));
            builder.Add(Vector3.One, 0);
            var accessorId = exporter.CreateSparseAccessorVector3(root, new gltf.UnicodeString("sparse"), builder);
            Assert.That(accessorId!.Value.ID, Is.Zero);
            var accessor = root.Accessors.First();
            Assert.That(accessor.Count, Is.EqualTo(3));
            Assert.That(accessor.ComponentType, Is.EqualTo(gltf.accessor.ComponentType.Float));
            Assert.That(accessor.Type, Is.EqualTo(gltf.accessor.Type.Vec3));
            Assert.That(accessor.Name!.Value, Is.EqualTo("sparse_Accessor0"));
            Assert.That(accessor.Sparse, Is.Not.Null);
            var sparse = accessor.Sparse!;
            Assert.That(sparse.Count, Is.EqualTo(1));
            Assert.That(sparse.Indices.BufferView.ID, Is.EqualTo(0));
            Assert.That(sparse.Indices.ComponentType, Is.EqualTo( gltf.accessor.ComponentType.UnsignedByte));
            Assert.That(sparse.Values.BufferView.ID, Is.EqualTo(1));
        }

        [Test]
        public void ExporterCreateSparseAccessorVector3_MinMax()
        {
            var root = new gltf.Root
            {
                Accessors = new List<gltf.accessor.Accessor>(),
                BufferViews = new List<gltf.buffer.BufferView>(),
            };
            using var exporter = new gltf.exporter.Exporter();
            var builder = new gltf.exporter.Exporter.SparseBuilder(ImmutableArray.CreateRange(new[]
            {
                new Vector3(-1),
                new Vector3(-2),
                new Vector3(-3),
            }));
            builder.Add(new Vector3(-1), 0);
            builder.Add(new Vector3(-2), 1);
            {
                var accessorId = exporter.CreateSparseAccessorVector3(root, new gltf.UnicodeString("sparse"), builder);
                var accessor = root.Accessors[(int)accessorId!.Value.ID];
                Assert.That(accessor.Min, Is.EquivalentTo(new []{ -2.0f, -2.0f, -2.0f }));
                Assert.That(accessor.Max, Is.EquivalentTo(new []{ 0.0f, 0.0f, 0.0f }));
            }
            builder.Add(new Vector3(-3), 2);
            {
                var accessorId = exporter.CreateSparseAccessorVector3(root, new gltf.UnicodeString("sparse"), builder);
                var accessor = root.Accessors[(int)accessorId!.Value.ID];
                Assert.That(accessor.Count, Is.EqualTo(accessor.Sparse!.Count));
                Assert.That(accessor.Min, Is.EquivalentTo(new []{ -3.0f, -3.0f, -3.0f }));
                Assert.That(accessor.Max, Is.EquivalentTo(new []{ -1.0f, -1.0f, -1.0f }));
            }
        }

        [TestCase(0, gltf.accessor.ComponentType.UnsignedByte, 1)]
        [TestCase(0xff, gltf.accessor.ComponentType.UnsignedByte, 1)]
        [TestCase(0x100, gltf.accessor.ComponentType.UnsignedShort, 2)]
        [TestCase(0xffff, gltf.accessor.ComponentType.UnsignedShort, 2)]
        [TestCase(0x10000, gltf.accessor.ComponentType.UnsignedInt, 4)]
        public void ExporterCreateSparseAccessorVector3_Indices(int index, gltf.accessor.ComponentType expectedType,
            int expectedSize)
        {
            var root = new gltf.Root
            {
                Accessors = new List<gltf.accessor.Accessor>(),
                BufferViews = new List<gltf.buffer.BufferView>(),
            };
            using var exporter = new gltf.exporter.Exporter();
            var builder = new gltf.exporter.Exporter.SparseBuilder(ImmutableArray.CreateRange(new[]
            {
                -Vector3.One,
                Vector3.Zero,
                Vector3.One,
            }));
            builder.Add(Vector3.Zero, (uint)index);
            var accessorId = exporter.CreateSparseAccessorVector3(root, new gltf.UnicodeString("sparse"), builder);
            var accessor = root.Accessors[(int)accessorId!.Value.ID];
            var sparse = accessor.Sparse!;
            Assert.That(sparse.Indices.ComponentType, Is.EqualTo(expectedType));
            var bufferView = root.BufferViews[(int)sparse.Indices.BufferView.ID];
            Assert.That(bufferView.ByteLength, Is.EqualTo(expectedSize));
        }

        [Test]
        public void Normalize()
        {
            var root = new gltf.Root
            {
                Extensions = new Dictionary<string, JToken>(),
                ExtensionsUsed = new List<string>(),
                ExtensionsRequired = new List<string>(),
                Accessors = new List<gltf.accessor.Accessor>(),
                Buffers = new List<gltf.buffer.Buffer>(),
                BufferViews = new List<gltf.buffer.BufferView>(),
                Cameras = new List<gltf.camera.Camera>(),
                Images = new List<gltf.buffer.Image>(),
                Materials = new List<gltf.material.Material>(),
                Meshes = new List<gltf.mesh.Mesh>(),
                Nodes = new List<gltf.node.Node>(),
                Samplers = new List<gltf.material.Sampler>(),
                Scenes = new List<gltf.scene.Scene>(),
                Skins = new List<gltf.node.Skin>(),
                Textures = new List<gltf.material.Texture>(),
            };
            root.Normalize();
            Assert.That(root.Extensions, Is.Null);
            Assert.That(root.ExtensionsUsed, Is.Null);
            Assert.That(root.ExtensionsRequired, Is.Null);
            Assert.That(root.Accessors, Is.Null);
            Assert.That(root.Buffers, Is.Null);
            Assert.That(root.BufferViews, Is.Null);
            Assert.That(root.Cameras, Is.Null);
            Assert.That(root.Images, Is.Null);
            Assert.That(root.Materials, Is.Null);
            Assert.That(root.Meshes, Is.Null);
            Assert.That(root.Nodes, Is.Null);
            Assert.That(root.Samplers, Is.Null);
            Assert.That(root.Scenes, Is.Null);
            Assert.That(root.Skins, Is.Null);
            Assert.That(root.Textures, Is.Null);
        }

        [Test]
        public void SaveAndLoad()
        {
            var expected = new gltf.Root
            {
                Asset = new gltf.asset.Asset
                {
                    Version = "2.0",
                }
            };
            var json = gltf.Document.SaveAsString(expected);
            var actual = gltf.Document.LoadFromString(json);
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.Asset.Version, Is.EqualTo("2.0"));
        }
    }
}
