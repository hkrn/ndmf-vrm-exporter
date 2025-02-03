// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal sealed class IsExternalInit
    {
    }
}
#endif
/* NET5_0_OR_GREATER */

namespace com.github.hkrn.gltf
{
    using System.Numerics;
    using IExtensions = IDictionary<string, JToken>;

    [DebuggerDisplay("{ID}")]
    public readonly struct ObjectID : IEquatable<ObjectID>, IComparable<ObjectID>
    {
        public static readonly ObjectID Null = new(uint.MaxValue);

        public uint ID { get; }

        public ObjectID(uint value)
        {
            ID = value;
        }

        public bool IsNull => ID == uint.MaxValue;

        public bool Equals(ObjectID other)
        {
            return ID == other.ID;
        }

        public override bool Equals(object? obj)
        {
            return obj is ObjectID other && Equals(other);
        }

        public override string ToString()
        {
            return $"ObjectID({ID})";
        }

        public override int GetHashCode()
        {
            return (int)ID;
        }

        public int CompareTo(ObjectID other)
        {
            return ID.CompareTo(other.ID);
        }
    }

    internal sealed class ObjectIDConverter : JsonConverter<ObjectID?>
    {
        public override void WriteJson(JsonWriter writer, ObjectID? value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ID);
        }

        public override ObjectID? ReadJson(JsonReader reader, Type objectType, ObjectID? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.Integer)
                return null;
            var id = ((long?)reader.Value).GetValueOrDefault();
            return new ObjectID((uint)id);
        }
    }

    public class UnicodeString : IEquatable<UnicodeString>
    {
        public UnicodeString(string v)
        {
            var sb = new StringBuilder();
            foreach (var c in v)
            {
                if (c >= 0 && c <= 0x7f)
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(@"\u");
                    sb.Append($"{(int)c:x4}");
                }
            }

            Value = sb.ToString();
        }

        public string Value { get; init; }

        public bool Equals(UnicodeString? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value;
        }

        public override string ToString()
        {
            return Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public class UnicodeStringConverter : JsonConverter<UnicodeString?>
    {
        public override void WriteJson(JsonWriter writer, UnicodeString? value, JsonSerializer serializer)
        {
            writer.WriteRawValue($"\"{value!.Value}\"");
        }

        public override UnicodeString? ReadJson(JsonReader reader, Type objectType,
            UnicodeString? existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                return null;
            var value = (string?)reader.Value;
            return new UnicodeString(value ?? "");
        }
    }

    internal sealed class Vector3Converter : JsonConverter<Vector3?>
    {
        public override void WriteJson(JsonWriter writer, Vector3? value, JsonSerializer serializer)
        {
            if (!value.HasValue)
                return;
            var v = value.Value;
            writer.WriteStartArray();
            writer.WriteValue(v.X);
            writer.WriteValue(v.Y);
            writer.WriteValue(v.Z);
            writer.WriteEndArray();
        }

        public override Vector3? ReadJson(JsonReader reader, Type objectType, Vector3? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
                return null;
            var value = new Vector3
            {
                X = (float)reader.ReadAsDouble().GetValueOrDefault(3.0),
                Y = (float)reader.ReadAsDouble().GetValueOrDefault(3.0),
                Z = (float)reader.ReadAsDouble().GetValueOrDefault(3.0),
            };
            reader.Read();
            return value;
        }
    }

    internal sealed class Vector4Converter : JsonConverter<Vector4?>
    {
        public override void WriteJson(JsonWriter writer, Vector4? value, JsonSerializer serializer)
        {
            if (!value.HasValue)
                return;
            var v = value.Value;
            writer.WriteStartArray();
            writer.WriteValue(v.X);
            writer.WriteValue(v.Y);
            writer.WriteValue(v.Z);
            writer.WriteValue(v.W);
            writer.WriteEndArray();
        }

        public override Vector4? ReadJson(JsonReader reader, Type objectType, Vector4? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
                return null;
            var value = new Vector4
            {
                X = (float)reader.ReadAsDouble().GetValueOrDefault(),
                Y = (float)reader.ReadAsDouble().GetValueOrDefault(),
                Z = (float)reader.ReadAsDouble().GetValueOrDefault(),
                W = (float)reader.ReadAsDouble().GetValueOrDefault(),
            };
            reader.Read();
            return value;
        }
    }

    internal sealed class QuaternionConverter : JsonConverter<Quaternion?>
    {
        public override void WriteJson(JsonWriter writer, Quaternion? value, JsonSerializer serializer)
        {
            if (!value.HasValue)
                return;
            var v = value.Value;
            writer.WriteStartArray();
            writer.WriteValue(v.X);
            writer.WriteValue(v.Y);
            writer.WriteValue(v.Z);
            writer.WriteValue(v.W);
            writer.WriteEndArray();
        }

        public override Quaternion? ReadJson(JsonReader reader, Type objectType, Quaternion? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
                return null;
            var value = new Quaternion
            {
                X = (float)reader.ReadAsDouble().GetValueOrDefault(),
                Y = (float)reader.ReadAsDouble().GetValueOrDefault(),
                Z = (float)reader.ReadAsDouble().GetValueOrDefault(),
                W = (float)reader.ReadAsDouble().GetValueOrDefault(1.0),
            };
            reader.Read();
            return value;
        }
    }

    internal sealed class Matrix4Converter : JsonConverter<Matrix4x4?>
    {
        public override void WriteJson(JsonWriter writer, Matrix4x4? value, JsonSerializer serializer)
        {
            if (!value.HasValue)
                return;
            var v = value.Value;
            writer.WriteStartArray();
            writer.WriteValue(v.M11);
            writer.WriteValue(v.M21);
            writer.WriteValue(v.M31);
            writer.WriteValue(v.M41);
            writer.WriteValue(v.M12);
            writer.WriteValue(v.M22);
            writer.WriteValue(v.M32);
            writer.WriteValue(v.M42);
            writer.WriteValue(v.M13);
            writer.WriteValue(v.M23);
            writer.WriteValue(v.M33);
            writer.WriteValue(v.M43);
            writer.WriteValue(v.M14);
            writer.WriteValue(v.M24);
            writer.WriteValue(v.M34);
            writer.WriteValue(v.M44);
            writer.WriteEndArray();
        }

        public override Matrix4x4? ReadJson(JsonReader reader, Type objectType, Matrix4x4? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
                return null;
            var value = new Matrix4x4
            {
                M11 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M21 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M31 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M41 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M12 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M22 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M32 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M42 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M13 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M23 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M33 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M43 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M14 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M24 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M34 = (float)reader.ReadAsDouble().GetValueOrDefault(),
                M44 = (float)reader.ReadAsDouble().GetValueOrDefault()
            };
            reader.Read();
            return value;
        }
    }

    internal sealed class ListUtils<T> where T : ICloneable
    {
        public static IList<T>? DeepClone(IList<T>? values)
        {
            if (values == null)
            {
                return null;
            }

            var list = new List<T>(values.Count);
            list.AddRange(values.Select(item => (T)item.Clone()));
            return list;
        }
    }

    internal sealed class ExtensionsUtils
    {
        public static IExtensions? DeepClone(IExtensions? extensions)
        {
            return extensions?.ToDictionary(entry => entry.Key, entry => entry.Value.DeepClone());
        }
    }

    namespace accessor
    {
        public sealed class SparseIndices : ICloneable
        {
            public ObjectID BufferView { get; set; } = ObjectID.Null;
            public uint? ByteOffset { get; set; }
            public ComponentType ComponentType { get; set; } = ComponentType.Unknown;
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new SparseIndices
                {
                    BufferView = BufferView,
                    ByteOffset = ByteOffset,
                    ComponentType = ComponentType,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        public sealed class SparseValues : ICloneable
        {
            public ObjectID BufferView { get; set; } = ObjectID.Null;
            public uint? ByteOffset { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new SparseValues
                {
                    BufferView = BufferView,
                    ByteOffset = ByteOffset,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        public sealed class Sparse : ICloneable
        {
            public uint Count { get; set; }
            public SparseIndices Indices { get; init; } = new();
            public SparseValues Values { get; init; } = new();
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Sparse
                {
                    Count = Count,
                    Indices = (SparseIndices)Indices.Clone(),
                    Values = (SparseValues)Values.Clone(),
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        public enum ComponentType
        {
            Unknown,
            Byte,
            UnsignedByte,
            Short,
            UnsignedShort,
            UnsignedInt,
            Float,
        }

        internal sealed class ComponentTypeConverter : JsonConverter<ComponentType?>
        {
            public override void WriteJson(JsonWriter writer, ComponentType? value, JsonSerializer serializer)
            {
                if (!value.HasValue)
                    return;
                var v = value switch
                {
                    ComponentType.Byte => 5120,
                    ComponentType.UnsignedByte => 5121,
                    ComponentType.Short => 5122,
                    ComponentType.UnsignedShort => 5123,
                    ComponentType.UnsignedInt => 5125,
                    ComponentType.Float => 5126,
                    _ => throw new JsonException(),
                };
                writer.WriteValue(v);
            }

            public override ComponentType? ReadJson(JsonReader reader, System.Type objectType,
                ComponentType? existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.Integer)
                    return null;
                return reader.Value switch
                {
                    5120L => ComponentType.Byte,
                    5121L => ComponentType.UnsignedByte,
                    5122L => ComponentType.Short,
                    5123L => ComponentType.UnsignedShort,
                    5125L => ComponentType.UnsignedInt,
                    5126L => ComponentType.Float,
                    _ => throw new JsonException(),
                };
            }
        }


        public enum Type
        {
            Unknown,
            Scalar,
            Vec2,
            Vec3,
            Vec4,
            Mat2,
            Mat3,
            Mat4,
        }

        internal sealed class TypeConverter : JsonConverter<Type?>
        {
            public override void WriteJson(JsonWriter writer, Type? value, JsonSerializer serializer)
            {
                if (!value.HasValue)
                    return;
                var v = value switch
                {
                    Type.Scalar => "SCALAR",
                    Type.Vec2 => "VEC2",
                    Type.Vec3 => "VEC3",
                    Type.Vec4 => "VEC4",
                    Type.Mat2 => "MAT2",
                    Type.Mat3 => "MAT3",
                    Type.Mat4 => "MAT4",
                    _ => throw new JsonException(),
                };
                writer.WriteValue(v);
            }

            public override Type? ReadJson(JsonReader reader, System.Type objectType, Type? existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.String)
                    return null;
                return reader.Value switch
                {
                    "SCALAR" => Type.Scalar,
                    "VEC2" => Type.Vec2,
                    "VEC3" => Type.Vec3,
                    "VEC4" => Type.Vec4,
                    "MAT2" => Type.Mat2,
                    "MAT3" => Type.Mat3,
                    "MAT4" => Type.Mat4,
                    _ => throw new JsonException(),
                };
            }
        }

        [DebuggerDisplay("{Name}")]
        public sealed class Accessor : ICloneable
        {
            public ObjectID? BufferView { get; set; }
            public uint? ByteOffset { get; set; }
            public ComponentType? ComponentType { get; set; }
            public bool? Normalized { get; set; }
            public long Count { get; set; }
            public Type Type { get; set; } = Type.Unknown;
            public IList<float>? Max { get; set; }
            public IList<float>? Min { get; set; }
            public Sparse? Sparse { get; set; }
            public UnicodeString? Name { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Accessor
                {
                    BufferView = BufferView,
                    ByteOffset = ByteOffset,
                    ComponentType = ComponentType,
                    Normalized = Normalized,
                    Count = Count,
                    Type = Type,
                    Max = Max?.ToList(),
                    Min = Min?.ToList(),
                    Sparse = (Sparse?)Sparse?.Clone(),
                    Name = Name,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }
    }

    namespace animation
    {
        public enum Interpolation
        {
            Step,
            Linear,
            CubicSpline
        }

        internal sealed class InterpolationConverter : JsonConverter<Interpolation?>
        {
            public override void WriteJson(JsonWriter writer, Interpolation? value, JsonSerializer serializer)
            {
                if (!value.HasValue)
                    return;
                var v = value switch
                {
                    Interpolation.CubicSpline => "CUBICSPLINE",
                    Interpolation.Linear => "LINEAR",
                    Interpolation.Step => "STEP",
                    _ => throw new JsonException(),
                };
                writer.WriteValue(v);
            }

            public override Interpolation? ReadJson(JsonReader reader, Type objectType,
                Interpolation? existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.String)
                    return null;
                return reader.Value switch
                {
                    "CUBICSPLINE" => Interpolation.CubicSpline,
                    "LINEAR" => Interpolation.Linear,
                    "STEP" => Interpolation.Step,
                    _ => throw new JsonException(),
                };
            }
        }

        public enum Path
        {
            Unknown,
            Translation,
            Rotation,
            Scale,
            Weights,
            Pointer,
        }

        internal sealed class PathConverter : JsonConverter<Path?>
        {
            public override void WriteJson(JsonWriter writer, Path? value, JsonSerializer serializer)
            {
                if (!value.HasValue)
                    return;
                var v = value switch
                {
                    Path.Rotation => "rotation",
                    Path.Scale => "scale",
                    Path.Translation => "translation",
                    Path.Weights => "weights",
                    Path.Pointer => "pointer",
                    _ => throw new JsonException(),
                };
                writer.WriteValue(v);
            }

            public override Path? ReadJson(JsonReader reader, Type objectType, Path? existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.String)
                    return null;
                return reader.Value switch
                {
                    "rotation" => Path.Rotation,
                    "scale" => Path.Scale,
                    "translation" => Path.Translation,
                    "weights" => Path.Weights,
                    "pointer" => Path.Pointer,
                    _ => throw new JsonException(),
                };
            }
        }

        public sealed class AnimationSampler : ICloneable
        {
            public ObjectID Input { get; set; } = ObjectID.Null;
            public Interpolation? Interpolation { get; set; }
            public ObjectID Output { get; set; } = ObjectID.Null;
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new AnimationSampler
                {
                    Input = Input,
                    Interpolation = Interpolation,
                    Output = Output,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        public sealed class ChannelTarget : ICloneable
        {
            public ObjectID Node { get; set; } = ObjectID.Null;
            public Path Path { get; set; } = Path.Unknown;
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new ChannelTarget
                {
                    Node = Node,
                    Path = Path,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        public sealed class Channel : ICloneable
        {
            public ObjectID Sampler { get; set; } = ObjectID.Null;
            public ChannelTarget Target { get; init; } = new();
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Channel
                {
                    Sampler = Sampler,
                    Target = Target,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        [DebuggerDisplay("{Name}")]
        public sealed class Animation : ICloneable
        {
            public IList<Channel>? Channels { get; set; }
            public IList<AnimationSampler>? Samplers { get; set; }
            public UnicodeString? Name { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Animation
                {
                    Channels = ListUtils<Channel>.DeepClone(Channels),
                    Samplers = ListUtils<AnimationSampler>.DeepClone(Samplers),
                    Name = Name,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }
    }

    namespace buffer
    {
        public enum Target
        {
            ArrayBuffer,
            ElementArrayBuffer,
        }

        internal sealed class TargetConverter : JsonConverter<Target?>
        {
            public override void WriteJson(JsonWriter writer, Target? value, JsonSerializer serializer)
            {
                if (!value.HasValue)
                    return;
                var v = value switch
                {
                    Target.ArrayBuffer => 34962,
                    Target.ElementArrayBuffer => 34963,
                    _ => throw new JsonException(),
                };
                writer.WriteValue(v);
            }

            public override Target? ReadJson(JsonReader reader, Type objectType, Target? existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.Integer)
                    return null;
                return reader.Value switch
                {
                    34962L => Target.ArrayBuffer,
                    34963L => Target.ElementArrayBuffer,
                    _ => throw new JsonException(),
                };
            }
        }

        [DebuggerDisplay("{Name}")]
        public sealed class Buffer : ICloneable
        {
            public string? Uri { get; set; }
            public long ByteLength { get; set; }
            public UnicodeString? Name { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Buffer
                {
                    Uri = Uri,
                    ByteLength = ByteLength,
                    Name = Name,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        [DebuggerDisplay("{Name}")]
        public sealed class BufferView : ICloneable
        {
            public ObjectID Buffer { get; set; } = ObjectID.Null;
            public long? ByteOffset { get; set; }
            public long ByteLength { get; set; }
            public long? ByteStride { get; set; }
            public UnicodeString? Name { get; set; }
            public Target? Target { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new BufferView
                {
                    Buffer = Buffer,
                    ByteOffset = ByteOffset,
                    ByteLength = ByteLength,
                    ByteStride = ByteStride,
                    Name = Name,
                    Target = Target,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        [DebuggerDisplay("{Name}")]
        public sealed class Image : ICloneable
        {
            public string? Uri { get; set; }
            public string? MimeType { get; set; }
            public ObjectID? BufferView { get; set; }
            public UnicodeString? Name { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Image
                {
                    Uri = Uri,
                    MimeType = MimeType,
                    BufferView = BufferView,
                    Name = Name,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }
    }

    namespace asset
    {
        public sealed class Asset : ICloneable
        {
            public string Version { get; set; } = "";
            public string? Generator { get; set; }
            public string? Copyright { get; set; }
            public string? MinVersion { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Asset
                {
                    Version = Version,
                    Generator = Generator,
                    Copyright = Copyright,
                    MinVersion = MinVersion,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }
    }

    namespace camera
    {
        public sealed class Orthographic : ICloneable
        {
            public float Xmag { get; set; }
            public float Ymag { get; set; }
            public float Zfar { get; set; }
            public float Znear { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Orthographic
                {
                    Xmag = Xmag,
                    Ymag = Ymag,
                    Zfar = Zfar,
                    Znear = Znear,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        public sealed class Perspective : ICloneable
        {
            public float? AspectRatio { get; set; }
            public float Yfov { get; set; }
            public float Zfar { get; set; }
            public float Znear { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Perspective
                {
                    AspectRatio = AspectRatio,
                    Yfov = Yfov,
                    Zfar = Zfar,
                    Znear = Znear,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        public sealed class Camera : ICloneable
        {
            public Orthographic? Orthographic { get; set; }
            public Perspective? Perspective { get; set; }
            public string? Type { get; set; }
            public UnicodeString? Name { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Camera
                {
                    Orthographic = (Orthographic?)Orthographic?.Clone(),
                    Perspective = (Perspective?)Perspective?.Clone(),
                    Type = Type,
                    Name = Name,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }
    }

    namespace material
    {
        public enum AlphaMode
        {
            Blend,
            Mask,
            Opaque,
        }

        internal sealed class AlphaModeConverter : JsonConverter<AlphaMode?>
        {
            public override void WriteJson(JsonWriter writer, AlphaMode? value, JsonSerializer serializer)
            {
                if (!value.HasValue)
                    return;
                var v = value switch
                {
                    AlphaMode.Blend => "BLEND",
                    AlphaMode.Mask => "MASK",
                    AlphaMode.Opaque => "OPAQUE",
                    _ => throw new JsonException(),
                };
                writer.WriteValue(v);
            }

            public override AlphaMode? ReadJson(JsonReader reader, Type objectType,
                AlphaMode? existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.String)
                    return null;
                return reader.Value switch
                {
                    "BLEND" => AlphaMode.Blend,
                    "MASK" => AlphaMode.Mask,
                    "OPAQUE" => AlphaMode.Opaque,
                    _ => throw new JsonException(),
                };
            }
        }

        public enum TextureFilterMode
        {
            Nearest = 9728,
            Linear = 9729,
            NearestMipmapNearest = 9984,
            LinearMipmapNearest = 9985,
            NearestMipmapLinear = 9986,
            LinearMipmapLinear = 9987,
        }

        internal sealed class TextureFilterModeConverter : JsonConverter<TextureFilterMode?>
        {
            public override void WriteJson(JsonWriter writer, TextureFilterMode? value,
                JsonSerializer serializer)
            {
                if (!value.HasValue)
                    return;
                var v = value switch
                {
                    TextureFilterMode.Nearest => 9728,
                    TextureFilterMode.Linear => 9729,
                    TextureFilterMode.NearestMipmapNearest => 9984,
                    TextureFilterMode.LinearMipmapNearest => 9985,
                    TextureFilterMode.NearestMipmapLinear => 9986,
                    TextureFilterMode.LinearMipmapLinear => 9987,
                    _ => throw new JsonException(),
                };
                writer.WriteValue(v);
            }

            public override TextureFilterMode? ReadJson(JsonReader reader, Type objectType,
                TextureFilterMode? existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.Integer)
                    return null;
                return reader.Value switch
                {
                    9728L => TextureFilterMode.Nearest,
                    9729L => TextureFilterMode.Linear,
                    9984L => TextureFilterMode.NearestMipmapNearest,
                    9985L => TextureFilterMode.LinearMipmapNearest,
                    9986L => TextureFilterMode.NearestMipmapLinear,
                    9987L => TextureFilterMode.LinearMipmapLinear,
                    _ => throw new JsonException(),
                };
            }
        }

        public enum TextureWrapMode
        {
            ClampToEdge = 33071,
            MirroredRepeat = 33648,
            Repeat = 10497,
        }

        internal sealed class TextureWrapModeConverter : JsonConverter<TextureWrapMode?>
        {
            public override void WriteJson(JsonWriter writer, TextureWrapMode? value,
                JsonSerializer serializer)
            {
                if (!value.HasValue)
                    return;
                var v = value switch
                {
                    TextureWrapMode.ClampToEdge => 33071,
                    TextureWrapMode.MirroredRepeat => 33648,
                    TextureWrapMode.Repeat => 10497,
                    _ => throw new JsonException(),
                };
                writer.WriteValue(v);
            }

            public override TextureWrapMode? ReadJson(JsonReader reader, Type objectType,
                TextureWrapMode? existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.Integer)
                    return null;
                return reader.Value switch
                {
                    33071L => TextureWrapMode.ClampToEdge,
                    33648L => TextureWrapMode.MirroredRepeat,
                    10497L => TextureWrapMode.Repeat,
                    _ => throw new JsonException(),
                };
            }
        }

        public sealed class NormalTextureInfo : ICloneable
        {
            public ObjectID Index { get; set; } = ObjectID.Null;
            public uint? TexCoord { get; set; }
            public float? Scale { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new NormalTextureInfo
                {
                    Index = Index,
                    TexCoord = TexCoord,
                    Scale = Scale,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        public sealed class OcclusionTextureInfo : ICloneable
        {
            public ObjectID Index { get; set; } = ObjectID.Null;
            public uint? TexCoord { get; set; }
            public float? Strength { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new OcclusionTextureInfo
                {
                    Index = Index,
                    TexCoord = TexCoord,
                    Strength = Strength,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        public sealed class PbrMetallicRoughness : ICloneable
        {
            public Vector4? BaseColorFactor { get; set; }
            public TextureInfo? BaseColorTexture { get; set; }
            public float? MetallicFactor { get; set; }
            public float? RoughnessFactor { get; set; }
            public TextureInfo? MetallicRoughnessTexture { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new PbrMetallicRoughness
                {
                    BaseColorFactor = BaseColorFactor,
                    BaseColorTexture = (TextureInfo?)BaseColorTexture?.Clone(),
                    MetallicFactor = MetallicFactor,
                    RoughnessFactor = RoughnessFactor,
                    MetallicRoughnessTexture = (TextureInfo?)MetallicRoughnessTexture?.Clone(),
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        [DebuggerDisplay("{Name}")]
        public sealed class Material : ICloneable
        {
            public UnicodeString? Name { get; set; }
            public PbrMetallicRoughness? PbrMetallicRoughness { get; set; }
            public NormalTextureInfo? NormalTexture { get; set; }
            public OcclusionTextureInfo? OcclusionTexture { get; set; }
            public TextureInfo? EmissiveTexture { get; set; }
            public Vector3? EmissiveFactor { get; set; }
            public AlphaMode? AlphaMode { get; set; }
            public float? AlphaCutoff { get; set; }
            public bool? DoubleSided { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Material
                {
                    Name = Name,
                    PbrMetallicRoughness = (PbrMetallicRoughness?)PbrMetallicRoughness?.Clone(),
                    NormalTexture = (NormalTextureInfo?)NormalTexture?.Clone(),
                    OcclusionTexture = (OcclusionTextureInfo?)OcclusionTexture?.Clone(),
                    EmissiveTexture = (TextureInfo?)EmissiveTexture?.Clone(),
                    EmissiveFactor = EmissiveFactor,
                    AlphaMode = AlphaMode,
                    AlphaCutoff = AlphaCutoff,
                    DoubleSided = DoubleSided,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        [DebuggerDisplay("{Name}")]
        public sealed class Sampler : ICloneable
        {
            public TextureFilterMode? MagFilter { get; set; }
            public TextureFilterMode? MinFilter { get; set; }
            public TextureWrapMode? WrapS { get; set; }
            public TextureWrapMode? WrapT { get; set; }
            public UnicodeString? Name { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Sampler
                {
                    MagFilter = MagFilter,
                    MinFilter = MinFilter,
                    WrapS = WrapS,
                    WrapT = WrapT,
                    Name = Name,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        [DebuggerDisplay("{Name}")]
        public sealed class Texture : ICloneable
        {
            public ObjectID? Sampler { get; set; }
            public ObjectID? Source { get; set; }
            public UnicodeString? Name { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Texture
                {
                    Sampler = Sampler,
                    Source = Source,
                    Name = Name,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        public sealed class TextureInfo : ICloneable
        {
            public ObjectID Index { get; set; } = ObjectID.Null;
            public uint? TexCoord { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new TextureInfo
                {
                    Index = Index,
                    TexCoord = TexCoord,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }
    }

    namespace mesh
    {
        public enum PrimitiveMode
        {
            Point,
            Lines,
            LineLoop,
            LineStrip,
            Triangles,
            TriangleStrip,
            TriangleFan,
        }

        internal sealed class PrimitiveModeConverter : JsonConverter<PrimitiveMode?>
        {
            public override void WriteJson(JsonWriter writer, PrimitiveMode? value, JsonSerializer serializer)
            {
                if (!value.HasValue)
                    return;
                var v = value switch
                {
                    PrimitiveMode.Point => 0,
                    PrimitiveMode.Lines => 1,
                    PrimitiveMode.LineLoop => 2,
                    PrimitiveMode.LineStrip => 3,
                    PrimitiveMode.Triangles => 4,
                    PrimitiveMode.TriangleStrip => 5,
                    PrimitiveMode.TriangleFan => 6,
                    _ => throw new JsonException(),
                };
                writer.WriteValue(v);
            }

            public override PrimitiveMode? ReadJson(JsonReader reader, Type objectType,
                PrimitiveMode? existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.Integer)
                    return null;
                return reader.Value switch
                {
                    0L => PrimitiveMode.Point,
                    1L => PrimitiveMode.Lines,
                    2L => PrimitiveMode.LineLoop,
                    3L => PrimitiveMode.LineStrip,
                    4L => PrimitiveMode.Triangles,
                    5L => PrimitiveMode.TriangleStrip,
                    6L => PrimitiveMode.TriangleFan,
                    _ => throw new JsonException(),
                };
            }
        }

        public sealed class Primitive : ICloneable
        {
            public IDictionary<string, ObjectID> Attributes { get; init; } = new Dictionary<string, ObjectID>();
            public ObjectID? Indices { get; set; }
            public ObjectID? Material { get; set; }
            public PrimitiveMode? Mode { get; set; }
            public IList<IDictionary<string, ObjectID>>? Targets { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Primitive
                {
                    Attributes = Attributes.ToDictionary(entry => entry.Key, entry => entry.Value),
                    Indices = Indices,
                    Material = Material,
                    Mode = Mode,
                    Targets = Targets?.ToList(),
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        [DebuggerDisplay("{Name}")]
        public sealed class Mesh : ICloneable
        {
            public IList<Primitive>? Primitives { get; set; }
            public IList<float>? Weights { get; set; }
            public UnicodeString? Name { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Mesh
                {
                    Primitives = ListUtils<Primitive>.DeepClone(Primitives),
                    Weights = Weights?.ToList(),
                    Name = Name,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }

            public void Normalize()
            {
                if (Primitives?.Count == 0)
                {
                    Primitives = null;
                }

                if (Weights?.Count == 0)
                {
                    Weights = null;
                }

                if (Extensions?.Count == 0)
                {
                    Extensions = null;
                }
            }
        }
    }

    namespace node
    {
        [DebuggerDisplay("{Name}")]
        public sealed class Node : ICloneable
        {
            public ObjectID? Camera { get; set; }
            public IList<ObjectID>? Children { get; set; }
            public ObjectID? Skin { get; set; }
            public Matrix4x4? Matrix { get; set; }
            public ObjectID? Mesh { get; set; }
            public Quaternion? Rotation { get; set; }
            public Vector3? Scale { get; set; }
            public Vector3? Translation { get; set; }
            public IList<float>? Weights { get; set; }
            public UnicodeString? Name { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Node
                {
                    Camera = Camera,
                    Children = Children?.ToList(),
                    Skin = Skin,
                    Matrix = Matrix,
                    Mesh = Mesh,
                    Rotation = Rotation,
                    Scale = Scale,
                    Translation = Translation,
                    Weights = Weights?.ToList(),
                    Name = Name,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }

            public void Normalize()
            {
                if (Children?.Count == 0)
                {
                    Children = null;
                }

                if (Weights?.Count == 0)
                {
                    Weights = null;
                }

                if (Extensions?.Count == 0)
                {
                    Extensions = null;
                }
            }
        }

        [DebuggerDisplay("{Name}")]
        public sealed class Skin : ICloneable
        {
            public ObjectID InverseBindMatrices { get; set; } = ObjectID.Null;
            public ObjectID? Skeleton { get; set; }
            public IList<ObjectID>? Joints { get; set; }
            public UnicodeString? Name { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Skin
                {
                    InverseBindMatrices = InverseBindMatrices,
                    Skeleton = Skeleton,
                    Joints = Joints?.ToList(),
                    Name = Name,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }
    }

    namespace scene
    {
        [DebuggerDisplay("{Name}")]
        public sealed class Scene : ICloneable
        {
            public IList<ObjectID>? Nodes { get; set; }
            public UnicodeString? Name { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new Scene
                {
                    Nodes = Nodes?.ToList(),
                    Name = Name,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }
    }

    namespace extensions
    {
        public sealed class KhrMaterialsEmissiveStrength : ICloneable
        {
            public static readonly string Name = "KHR_materials_emissive_strength";
            public float EmissiveStrength { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new KhrMaterialsEmissiveStrength
                {
                    EmissiveStrength = EmissiveStrength,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        public sealed class KhrMaterialsUnlit
        {
            public static readonly string Name = "KHR_materials_unlit";
        }

        public sealed class KhrTextureTransform : ICloneable
        {
            public static readonly string Name = "KHR_texture_transform";
            public Vector2 Offset { get; set; } = Vector2.Zero;
            public float Rotation { get; set; }
            public Vector2 Scale { get; set; } = Vector2.One;
            public uint? TexCoord { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new KhrTextureTransform
                {
                    Offset = Offset,
                    Rotation = Rotation,
                    Scale = Scale,
                    TexCoord = TexCoord,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }

        public sealed class KhrTextureBasisu : ICloneable
        {
            public static readonly string Name = "KHR_texture_basisu";
            public ObjectID Source { get; set; }
            public IExtensions? Extensions { get; set; }
            public JToken? Extras { get; set; }

            public object Clone()
            {
                return new KhrTextureBasisu
                {
                    Source = Source,
                    Extensions = ExtensionsUtils.DeepClone(Extensions),
                    Extras = Extras?.DeepClone(),
                };
            }
        }
    }

    namespace exporter
    {
        public readonly struct KeyframeUnit
        {
            public readonly float Seconds;
            public readonly float[] InputTangent;
            public readonly float[] Value;
            public readonly float[] OutputTangent;

            public KeyframeUnit(float sec, float value)
            {
                Seconds = sec;
                InputTangent = Array.Empty<float>();
                Value = new[] { value };
                OutputTangent = Array.Empty<float>();
            }

            public KeyframeUnit(float sec, float inputTangent, float value, float outputTangent)
            {
                Seconds = sec;
                InputTangent = new[] { inputTangent };
                Value = new[] { value };
                OutputTangent = new[] { outputTangent };
            }

            public KeyframeUnit(float sec, Vector3 value)
            {
                Seconds = sec;
                InputTangent = Array.Empty<float>();
                Value = new[] { value.X, value.Y, value.Z };
                OutputTangent = Array.Empty<float>();
            }

            public KeyframeUnit(float sec, Vector3 inputTangent, Vector3 value, Vector3 outputTangent)
            {
                Seconds = sec;
                InputTangent = new[] { inputTangent.X, inputTangent.Y, inputTangent.Z };
                Value = new[] { value.X, value.Y, value.Z };
                OutputTangent = new[] { outputTangent.X, outputTangent.Y, outputTangent.Z };
            }

            public KeyframeUnit(float sec, Quaternion value)
            {
                Seconds = sec;
                InputTangent = Array.Empty<float>();
                Value = new[] { value.X, value.Y, value.Z, value.W };
                OutputTangent = Array.Empty<float>();
            }

            public KeyframeUnit(float sec, Quaternion inputTangent, Quaternion value, Quaternion outputTangent)
            {
                Seconds = sec;
                InputTangent = new[] { inputTangent.X, inputTangent.Y, inputTangent.Z, inputTangent.W };
                Value = new[] { value.X, value.Y, value.Z, value.W };
                OutputTangent = new[] { outputTangent.X, outputTangent.Y, outputTangent.Z, outputTangent.W };
            }
        }

        public class KeyframeAccessorUnit
        {
            public IList<KeyframeUnit> Keyframes { get; private init; } = new List<KeyframeUnit>();
            public ObjectID InputAccessor { get; set; } = ObjectID.Null;
            public ObjectID OutputAccessor { get; set; } = ObjectID.Null;
            public bool HasTangent { get; }
        }

        public sealed class KeyframeAccessorBundle
        {
            public KeyframeAccessorUnit Scales { get; private init; } = new();
            public KeyframeAccessorUnit Rotations { get; private init; } = new();
            public KeyframeAccessorUnit Translations { get; private init; } = new();
            public KeyframeAccessorUnit Weights { get; private init; } = new();
        }

        public sealed class MorphTarget
        {
            public string? Name { get; set; }
            public Vector3[] Positions { get; init; } = Array.Empty<Vector3>();
            public Vector3[] Normals { get; init; } = Array.Empty<Vector3>();
            public float? Weight { get; set; }
        }

        public sealed class MeshUnit
        {
            public UnicodeString? Name { get; set; }
            public IList<PrimitiveUnit> Primitives { get; } = new List<PrimitiveUnit>();
            public Vector3[] Positions { get; init; } = Array.Empty<Vector3>();
            public Vector3[] Normals { get; init; } = Array.Empty<Vector3>();
            public Vector4[] Colors { get; init; } = Array.Empty<Vector4>();
            public Vector2[] TexCoords0 { get; init; } = Array.Empty<Vector2>();
            public Vector2[] TexCoords1 { get; init; } = Array.Empty<Vector2>();
            public JointUnit[] Joints { get; init; } = Array.Empty<JointUnit>();
            public Vector4[] Weights { get; init; } = Array.Empty<Vector4>();
            public Vector4[] Tangents { get; init; } = Array.Empty<Vector4>();
            public IList<MorphTarget> MorphTargets { get; } = new List<MorphTarget>();
        }

        public struct JointUnit
        {
            public ushort X;
            public ushort Y;
            public ushort Z;
            public ushort W;
        }

        public sealed class PrimitiveUnit
        {
            public uint[] Indices { get; init; } = Array.Empty<uint>();
            public ObjectID? Material { get; set; }
            public mesh.PrimitiveMode PrimitiveMode { get; set; }
        }

        public sealed class SampledTextureUnit
        {
            public UnicodeString? Name { get; set; }
            public string? MimeType { get; set; }
            public byte[] Data { get; set; } = Array.Empty<byte>();
            public material.TextureFilterMode? MagFilter { get; set; }
            public material.TextureFilterMode? MinFilter { get; set; }
            public material.TextureWrapMode? WrapS { get; set; }
            public material.TextureWrapMode? WrapT { get; set; }
        }

        public sealed class Exporter : IDisposable
        {
            public const int Alignment = sizeof(uint);
            public long Length => InnerStream.Position;

            public class SparseBuilder
            {
                public Vector3 Max { get; init; }
                public Vector3 Min { get; init; }
                public long BaseAccessorCount { get; init; }
                public long SparseCount => _values.Count;
                private readonly IDictionary<uint, Vector3> _values = new Dictionary<uint, Vector3>();

                public SparseBuilder(IImmutableList<Vector3> values)
                {
                    Vector3 min = new(float.MaxValue), max = new(-float.MaxValue);
                    foreach (var value in values)
                    {
                        min = Vector3.Min(min, value);
                        max = Vector3.Max(max, value);
                    }

                    Min = min;
                    Max = max;
                    BaseAccessorCount = values.Count;
                }

                public void Clear()
                {
                    _values.Clear();
                }

                public void Add(Vector3 value, uint index)
                {
                    _values.Add(index, value);
                }

                public (IList<uint>, IList<Vector3>) Build()
                {
                    var indices = _values.Keys.ToList();
                    indices.Sort();
                    var values = indices.Select(index => _values[index]).ToList();
                    return (indices, values);
                }
            }

            public Exporter()
            {
                InnerStream = new MemoryStream();
                InnerStreamWriter = new BinaryWriter(InnerStream);
            }

            public void Dispose()
            {
                InnerStreamWriter.Dispose();
                InnerStream.Dispose();
            }

            public byte[] GetData(buffer.BufferView bufferView)
            {
                var from = (int)bufferView.ByteOffset.GetValueOrDefault();
                var to = from + (int)bufferView.ByteLength;
                return InnerStream.GetBuffer()[from..to];
            }

            public void Export(string json, Stream stream)
            {
                var paddingLength = InnerStream.Position % Alignment;
                if (paddingLength > 0)
                {
                    var padding = new byte[Alignment - paddingLength];
                    InnerStreamWriter.Write(padding);
                    InnerStreamWriter.Flush();
                }

                var bytesJson = GetAlignedJson(json);
                stream.Write(BitConverter.GetBytes(0x46546C67)); // "glTF"
                stream.Write(BitConverter.GetBytes(2));
                stream.Write(BitConverter.GetBytes((uint)(bytesJson.Length + InnerStream.Position + 28)));
                stream.Write(BitConverter.GetBytes(bytesJson.Length));
                stream.Write(BitConverter.GetBytes(0x4E4F534A)); // "JSON"
                stream.Write(bytesJson);
                stream.Write(BitConverter.GetBytes((uint)InnerStream.Position));
                stream.Write(BitConverter.GetBytes(0x004E4942)); // "BIN "
                stream.Write(InnerStream.GetBuffer().Take((int)InnerStream.Position).ToArray());
            }

            public ObjectID CreateSampledTexture(Root root, SampledTextureUnit sampledTextureUnit)
            {
                var sourceID = CreateTextureSource(root, sampledTextureUnit.Data, sampledTextureUnit.Name,
                    sampledTextureUnit.MimeType);
                var sampler = new material.Sampler
                {
                    MagFilter = sampledTextureUnit.MagFilter,
                    MinFilter = sampledTextureUnit.MinFilter,
                    WrapS = sampledTextureUnit.WrapS,
                    WrapT = sampledTextureUnit.WrapT,
                };
                var texture = new material.Texture
                {
                    Sampler = new ObjectID((uint)root.Samplers!.Count),
                    Source = sourceID,
                    Name = sampledTextureUnit.Name,
                };
                var textureID = new ObjectID((uint)root.Textures!.Count);
                root.Samplers!.Add(sampler);
                root.Textures!.Add(texture);
                return textureID;
            }

            public ObjectID CreateTextureSource(Root root, byte[] data, UnicodeString? name, string? mimeType)
            {
                var bufferViews = root.BufferViews!;
                var bufferView = new buffer.BufferView
                {
                    Buffer = new ObjectID(0),
                    ByteOffset = InnerStream.Position,
                    ByteLength = data.Length,
                    Name = name,
                };
                var image = new buffer.Image
                {
                    BufferView = new ObjectID((uint)bufferViews.Count),
                    MimeType = mimeType,
                    Name = name,
                };
                WriteStream(data);
                var sourceID = new ObjectID((uint)root.Images!.Count);
                bufferViews.Add(bufferView);
                root.Images!.Add(image);
                return sourceID;
            }

            public ObjectID CreateMesh(Root root, MeshUnit meshUnit)
            {
                var name = meshUnit.Name;
                var mesh = new mesh.Mesh
                {
                    Name = name,
                    Primitives = new List<mesh.Primitive>(),
                    Weights = meshUnit.MorphTargets.Count > 0 && meshUnit.MorphTargets.Any(item => item.Weight.HasValue)
                        ? meshUnit.MorphTargets.Select(item => item.Weight.GetValueOrDefault()).ToList()
                        : null,
                };
                var positions = new List<Vector3>();
                var normals = new List<Vector3>();
                var colors = new List<Vector4>();
                var texCoords0 = new List<Vector2>();
                var texCoords1 = new List<Vector2>();
                var joints = new List<JointUnit>();
                var weights = new List<Vector4>();
                var orderedIndices = new List<uint>();
                var indexMapping = new Dictionary<uint, uint>();
                var numPositions = (uint)meshUnit.Positions.Length;
                var numNormals = (uint)meshUnit.Normals.Length;
                var numColors = (uint)meshUnit.Colors.Length;
                var numTexCoords0Coords = (uint)meshUnit.TexCoords0.Length;
                var numTexCoords1Coords = (uint)meshUnit.TexCoords1.Length;
                var numJoints = (uint)meshUnit.Joints.Length;
                var numWeights = (uint)meshUnit.Weights.Length;
                foreach (var item in meshUnit.Primitives)
                {
                    var indexSet = new HashSet<uint>(item.Indices);
                    positions.Clear();
                    normals.Clear();
                    colors.Clear();
                    texCoords0.Clear();
                    texCoords1.Clear();
                    joints.Clear();
                    weights.Clear();
                    orderedIndices.Clear();
                    indexMapping.Clear();
                    for (uint i = 0; i < numPositions; i++)
                    {
                        if (!indexSet.Contains(i))
                            continue;
                        orderedIndices.Add(i);
                        positions.Add(meshUnit.Positions[i]);
                        if (i < numNormals)
                        {
                            normals.Add(meshUnit.Normals[i]);
                        }

                        if (i < numColors)
                        {
                            colors.Add(meshUnit.Colors[i]);
                        }

                        if (i < numTexCoords0Coords)
                        {
                            texCoords0.Add(meshUnit.TexCoords0[i]);
                        }

                        if (i < numTexCoords1Coords)
                        {
                            texCoords1.Add(meshUnit.TexCoords1[i]);
                        }

                        if (i < numJoints)
                        {
                            joints.Add(meshUnit.Joints[i]);
                        }

                        if (i < numWeights)
                        {
                            weights.Add(meshUnit.Weights[i]);
                        }

                        var newIndex = (uint)indexMapping.Count;
                        indexMapping.Add(i, newIndex);
                    }

                    var positionsAccessor = CreateAccessorVector3(root, new UnicodeString($"{name}_POSITION"),
                        positions.ToArray());
                    var normalsAccessor =
                        CreateAccessorVector3(root, new UnicodeString($"{name}_NORMAL"), normals.ToArray());
                    var colorsAccessor =
                        CreateAccessorVector4(root, new UnicodeString($"{name}_COLORS"), colors.ToArray());
                    var texCoords0Accessor = CreateAccessorVector2(root, new UnicodeString($"{name}_TEXCOORDS0"),
                        texCoords0.ToArray());
                    var texCoords1Accessor = CreateAccessorVector2(root, new UnicodeString($"{name}_TEXCOORDS1"),
                        texCoords1.ToArray());
                    var jointsAccessor =
                        CreateAccessorJoints(root, new UnicodeString($"{name}_JOINTS"), joints.ToArray());
                    var weightsAccessor =
                        CreateAccessorVector4(root, new UnicodeString($"{name}_WEIGHTS"), weights.ToArray());
                    var newIndices = item.Indices.Select(i => indexMapping[i]).ToList();
                    var primitive = new mesh.Primitive
                    {
                        Indices = CreateMeshPrimitiveAccessorIndices(root, name, newIndices.ToArray()),
                        Material = item.Material,
                        Mode = item.PrimitiveMode,
                    };
                    primitive.Attributes.Add("POSITION", positionsAccessor!.Value);
                    if (normalsAccessor.HasValue)
                    {
                        primitive.Attributes.Add("NORMAL", normalsAccessor.Value);
                    }

                    if (colorsAccessor.HasValue)
                    {
                        primitive.Attributes.Add("COLOR_0", colorsAccessor.Value);
                    }

                    if (texCoords0Accessor.HasValue)
                    {
                        primitive.Attributes.Add("TEXCOORD_0", texCoords0Accessor.Value);
                    }

                    if (texCoords1Accessor.HasValue)
                    {
                        primitive.Attributes.Add("TEXCOORD_1", texCoords1Accessor.Value);
                    }

                    if (jointsAccessor.HasValue)
                    {
                        primitive.Attributes.Add("JOINTS_0", jointsAccessor.Value);
                    }

                    if (weightsAccessor.HasValue)
                    {
                        primitive.Attributes.Add("WEIGHTS_0", weightsAccessor.Value);
                    }

                    var builder = new MorphTargetUnit
                    {
                        OrderedIndices = ImmutableList.CreateRange(orderedIndices),
                        IndexMappings = ImmutableDictionary.CreateRange(indexMapping),
                        Positions = ImmutableList.CreateRange(positions),
                        Normals = ImmutableList.CreateRange(normals),
                    };
                    BuildMorphTarget(root, meshUnit, builder, ref primitive);
                    mesh.Primitives.Add(primitive);
                }

                if (meshUnit.MorphTargets.Count > 0)
                {
                    var targetNames = new JArray();
                    var serializer = JsonSerializer.Create(new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter>
                        {
                            new UnicodeStringConverter()
                        }
                    });
                    foreach (var target in meshUnit.MorphTargets)
                    {
                        var value = new UnicodeString(target.Name!);
                        targetNames.Add(JToken.FromObject(value, serializer));
                    }

                    mesh.Extras = new JObject()
                    {
                        { "targetNames", targetNames }
                    };
                }

                var meshID = new ObjectID((uint)root.Meshes!.Count);
                root.Meshes!.Add(mesh);
                return meshID;
            }

            public ObjectID CreateMatrix4Accessor(Root root, string? name, Matrix4x4[] matrices)
            {
                var accessorObjectID = ObjectID.Null;
                var (accessorObject, bufferView) = CreateAccessor(root, name != null ? new UnicodeString(name) : null,
                    ref accessorObjectID);
                using var memoryStream = new MemoryStream();
                using var byteArray = new BinaryWriter(memoryStream);
                foreach (var item in matrices)
                {
                    byteArray.Write(BitConverter.GetBytes(item.M11));
                    byteArray.Write(BitConverter.GetBytes(item.M21));
                    byteArray.Write(BitConverter.GetBytes(item.M31));
                    byteArray.Write(BitConverter.GetBytes(item.M41));
                    byteArray.Write(BitConverter.GetBytes(item.M12));
                    byteArray.Write(BitConverter.GetBytes(item.M22));
                    byteArray.Write(BitConverter.GetBytes(item.M32));
                    byteArray.Write(BitConverter.GetBytes(item.M42));
                    byteArray.Write(BitConverter.GetBytes(item.M13));
                    byteArray.Write(BitConverter.GetBytes(item.M23));
                    byteArray.Write(BitConverter.GetBytes(item.M33));
                    byteArray.Write(BitConverter.GetBytes(item.M43));
                    byteArray.Write(BitConverter.GetBytes(item.M14));
                    byteArray.Write(BitConverter.GetBytes(item.M24));
                    byteArray.Write(BitConverter.GetBytes(item.M34));
                    byteArray.Write(BitConverter.GetBytes(item.M44));
                }

                accessorObject.Count = matrices.Length;
                accessorObject.ComponentType = accessor.ComponentType.Float;
                accessorObject.Type = accessor.Type.Mat4;
                bufferView.ByteOffset = InnerStream.Position;
                bufferView.ByteLength = memoryStream.Length;
                WriteStream(memoryStream);
                return accessorObjectID;
            }

            public void SerializeAllAnimationSamplerBundle(Root root, IReadOnlyList<KeyframeAccessorBundle> bundles)
            {
                foreach (var bundle in bundles)
                {
                    SerializeAnimationSamplerBundle(root, bundle);
                }
            }

            public void SerializeAnimationSamplerBundle(Root root, KeyframeAccessorBundle bundle)
            {
                SerializeAnimationSamplerAccessor(root, bundle.Translations, accessor.Type.Vec3,
                    accessor.ComponentType.Float);
                SerializeAnimationSamplerAccessor(root, bundle.Rotations, accessor.Type.Vec4,
                    accessor.ComponentType.Float);
                SerializeAnimationSamplerAccessor(root, bundle.Scales, accessor.Type.Vec3,
                    accessor.ComponentType.Float);
                SerializeAnimationSamplerAccessor(root, bundle.Weights, accessor.Type.Scalar,
                    accessor.ComponentType.Float);
            }

            public ObjectID? CreateAccessorJoints(Root root, UnicodeString? name, JointUnit[] joints)
            {
                if (joints.Length == 0)
                {
                    return null;
                }

                var accessorObjectID = ObjectID.Null;
                var (accessorObject, bufferView) = CreateAccessor(root, name,
                    ref accessorObjectID);
                using var memoryStream = new MemoryStream();
                using var byteArray = new BinaryWriter(memoryStream);
                foreach (var joint in joints)
                {
                    byteArray.Write(BitConverter.GetBytes(joint.X));
                    byteArray.Write(BitConverter.GetBytes(joint.Y));
                    byteArray.Write(BitConverter.GetBytes(joint.Z));
                    byteArray.Write(BitConverter.GetBytes(joint.W));
                }

                accessorObject.Count = joints.LongLength;
                accessorObject.ComponentType = accessor.ComponentType.UnsignedShort;
                accessorObject.Type = accessor.Type.Vec4;
                bufferView.ByteOffset = InnerStream.Position;
                bufferView.ByteLength = memoryStream.Length;
                bufferView.ByteStride = memoryStream.Length / accessorObject.Count;
                bufferView.Target = buffer.Target.ArrayBuffer;
                WriteStream(memoryStream);
                return accessorObjectID;
            }

            private ObjectID? CreateMeshPrimitiveAccessorIndices(Root root, UnicodeString? name, uint[] indices)
            {
                if (indices.Length == 0)
                {
                    return null;
                }

                var accessorObjectID = ObjectID.Null;
                var (accessorObject, bufferView) = CreateAccessor(root, name, ref accessorObjectID);
                using var memoryStream = new MemoryStream();
                using var byteArray = new BinaryWriter(memoryStream);
                foreach (var index in indices)
                {
                    byteArray.Write(BitConverter.GetBytes(index));
                }

                accessorObject.Count = indices.LongLength;
                accessorObject.ComponentType = accessor.ComponentType.UnsignedInt;
                accessorObject.Type = accessor.Type.Scalar;
                bufferView.ByteOffset = InnerStream.Position;
                bufferView.ByteLength = memoryStream.Length;
                bufferView.Target = buffer.Target.ElementArrayBuffer;
                WriteStream(memoryStream);
                return accessorObjectID;
            }

            public ObjectID? CreateAccessorVector2(Root root, UnicodeString? name, Vector2[] values)
            {
                if (values.Length == 0)
                {
                    return null;
                }

                var accessorObjectID = ObjectID.Null;
                var (accessorObject, bufferView) = CreateAccessor(root, name,
                    ref accessorObjectID);
                using MemoryStream memoryStream = new();
                using BinaryWriter byteArray = new(memoryStream);
                Vector2 min = new(float.MaxValue), max = new(-float.MaxValue);
                foreach (var value in values)
                {
                    byteArray.Write(BitConverter.GetBytes(value.X));
                    byteArray.Write(BitConverter.GetBytes(value.Y));
                    min = Vector2.Min(min, value);
                    max = Vector2.Max(max, value);
                }

                accessorObject.Min = new[] { min.X, min.Y };
                accessorObject.Max = new[] { max.X, max.Y };
                accessorObject.Count = values.LongLength;
                accessorObject.ComponentType = accessor.ComponentType.Float;
                accessorObject.Type = accessor.Type.Vec2;
                bufferView.ByteOffset = InnerStream.Position;
                bufferView.ByteLength = memoryStream.Length;
                bufferView.ByteStride = memoryStream.Length / accessorObject.Count;
                bufferView.Target = buffer.Target.ArrayBuffer;
                WriteStream(memoryStream);
                return accessorObjectID;
            }

            public ObjectID? CreateAccessorVector3(Root root, UnicodeString? name, Vector3[] values)
            {
                if (values.Length == 0)
                {
                    return null;
                }

                var accessorObjectID = ObjectID.Null;
                var (accessorObject, bufferView) = CreateAccessor(root, name,
                    ref accessorObjectID);
                using MemoryStream memoryStream = new();
                using BinaryWriter byteArray = new(memoryStream);
                Vector3 min = new(float.MaxValue), max = new(-float.MaxValue);
                foreach (var value in values)
                {
                    byteArray.Write(BitConverter.GetBytes(value.X));
                    byteArray.Write(BitConverter.GetBytes(value.Y));
                    byteArray.Write(BitConverter.GetBytes(value.Z));
                    min = Vector3.Min(min, value);
                    max = Vector3.Max(max, value);
                }

                accessorObject.Min = new[] { min.X, min.Y, min.Z };
                accessorObject.Max = new[] { max.X, max.Y, max.Z };
                accessorObject.Count = values.LongLength;
                accessorObject.ComponentType = accessor.ComponentType.Float;
                accessorObject.Type = accessor.Type.Vec3;
                bufferView.ByteOffset = InnerStream.Position;
                bufferView.ByteLength = memoryStream.Length;
                bufferView.ByteStride = memoryStream.Length / accessorObject.Count;
                bufferView.Target = buffer.Target.ArrayBuffer;
                WriteStream(memoryStream);
                return accessorObjectID;
            }

            public ObjectID? CreateAccessorVector4(Root root, UnicodeString? name, Vector4[] values)
            {
                if (values.Length == 0)
                {
                    return null;
                }

                var accessorObjectID = ObjectID.Null;
                var (accessorObject, bufferView) = CreateAccessor(root, name,
                    ref accessorObjectID);
                using MemoryStream memoryStream = new();
                using BinaryWriter byteArray = new(memoryStream);
                Vector4 min = new(float.MaxValue), max = new(-float.MaxValue);
                foreach (var value in values)
                {
                    byteArray.Write(BitConverter.GetBytes(value.X));
                    byteArray.Write(BitConverter.GetBytes(value.Y));
                    byteArray.Write(BitConverter.GetBytes(value.Z));
                    byteArray.Write(BitConverter.GetBytes(value.W));
                    min = Vector4.Min(min, value);
                    max = Vector4.Max(max, value);
                }

                accessorObject.Min = new[] { min.X, min.Y, min.Z, min.W };
                accessorObject.Max = new[] { max.X, max.Y, max.Z, max.W };
                accessorObject.Count = values.LongLength;
                accessorObject.ComponentType = accessor.ComponentType.Float;
                accessorObject.Type = accessor.Type.Vec4;
                bufferView.ByteOffset = InnerStream.Position;
                bufferView.ByteLength = memoryStream.Length;
                bufferView.ByteStride = memoryStream.Length / accessorObject.Count;
                bufferView.Target = buffer.Target.ArrayBuffer;
                WriteStream(memoryStream);
                return accessorObjectID;
            }

            public ObjectID? CreateSparseAccessorVector3(Root root, UnicodeString? name, SparseBuilder builder)
            {
                if (builder.SparseCount == 0)
                {
                    return null;
                }

                ObjectID accessorObjectID = new((uint)root.Accessors!.Count),
                    indexBufferViewID = new((uint)root.BufferViews!.Count),
                    valueBufferViewID = new((uint)root.BufferViews!.Count + 1);
                var indexBufferViewSuffix = $"BufferView{indexBufferViewID.ID}_SparseIndex";
                var indexBufferView = new buffer.BufferView
                {
                    Buffer = new ObjectID(0),
                    ByteOffset = InnerStream.Position,
                    Name = new UnicodeString(name != null
                        ? $"{name.Value}_{indexBufferViewSuffix}"
                        : indexBufferViewSuffix),
                };
                var (indices, values) = builder.Build();
                var componentType = indices.Last() switch
                {
                    > ushort.MaxValue => accessor.ComponentType.UnsignedInt,
                    > byte.MaxValue => accessor.ComponentType.UnsignedShort,
                    _ => accessor.ComponentType.UnsignedByte
                };
                {
                    using MemoryStream memoryStream = new();
                    using BinaryWriter byteArray = new(memoryStream);
                    foreach (var index in indices)
                    {
                        switch (componentType)
                        {
                            case accessor.ComponentType.UnsignedByte:
                            {
                                byteArray.Write((byte)index);
                                break;
                            }
                            case accessor.ComponentType.UnsignedShort:
                            {
                                byteArray.Write(BitConverter.GetBytes((ushort)index));
                                break;
                            }
                            case accessor.ComponentType.UnsignedInt:
                            {
                                byteArray.Write(BitConverter.GetBytes(index));
                                break;
                            }
                            case accessor.ComponentType.Byte:
                            case accessor.ComponentType.Short:
                            case accessor.ComponentType.Float:
                            default:
                                throw new InvalidOperationException();
                        }
                    }

                    indexBufferView.ByteLength = memoryStream.Length;
                    WriteStream(memoryStream);
                }
                var valueBufferViewSuffix = $"BufferView{indexBufferViewID.ID}_SparseValue";
                var valueBufferView = new buffer.BufferView
                {
                    Buffer = new ObjectID(0),
                    ByteOffset = InnerStream.Position,
                    Name = new UnicodeString(name != null
                        ? $"{name.Value}_{valueBufferViewSuffix}"
                        : valueBufferViewSuffix),
                };
                Vector3 min = new(float.MaxValue), max = new Vector3(-float.MaxValue);
                {
                    using MemoryStream memoryStream = new();
                    using BinaryWriter byteArray = new(memoryStream);
                    foreach (var value in values)
                    {
                        byteArray.Write(BitConverter.GetBytes(value.X));
                        byteArray.Write(BitConverter.GetBytes(value.Y));
                        byteArray.Write(BitConverter.GetBytes(value.Z));
                        min = Vector3.Min(min, value);
                        max = Vector3.Max(max, value);
                    }

                    valueBufferView.ByteLength = memoryStream.Length;
                    WriteStream(memoryStream);
                }
                if (builder.SparseCount < builder.BaseAccessorCount)
                {
                    min = Vector3.Min(min, Vector3.Zero);
                    max = Vector3.Max(max, Vector3.Zero);
                }

                var accessorNameSuffix = $"Accessor{accessorObjectID.ID}";
                var accessorObject = new accessor.Accessor
                {
                    Name = new UnicodeString(name != null ? $"{name.Value}_{accessorNameSuffix}" : accessorNameSuffix),
                    Min = new[] { min.X, min.Y, min.Z },
                    Max = new[] { max.X, max.Y, max.Z },
                    Count = builder.BaseAccessorCount,
                    ComponentType = accessor.ComponentType.Float,
                    Type = accessor.Type.Vec3,
                    Sparse = new accessor.Sparse
                    {
                        Count = (uint)builder.SparseCount,
                        Indices = new accessor.SparseIndices
                        {
                            BufferView = indexBufferViewID,
                            ComponentType = componentType,
                        },
                        Values = new accessor.SparseValues
                        {
                            BufferView = valueBufferViewID,
                        }
                    },
                };
                root.Accessors.Add(accessorObject);
                root.BufferViews.Add(indexBufferView);
                root.BufferViews.Add(valueBufferView);
                return accessorObjectID;
            }

            private static byte[] GetAlignedJson(string json)
            {
                using var jsonStream = new MemoryStream();
                using var jsonWriter = new BinaryWriter(jsonStream);
                jsonWriter.Write(Encoding.ASCII.GetBytes(json));
                while (jsonStream.Position % Alignment != 0)
                {
                    jsonWriter.Write((byte)0x20);
                }

                return jsonStream.GetBuffer().Take((int)jsonStream.Position).ToArray();
            }

            private void BuildMorphTarget(Root root, MeshUnit meshUnit, MorphTargetUnit morphTargetUnit,
                ref mesh.Primitive primitive)
            {
                var morphTargets = new List<IDictionary<string, ObjectID>>();
                var positions = new SparseBuilder(morphTargetUnit.Positions);
                var normals = new SparseBuilder(morphTargetUnit.Normals);
                foreach (var item in meshUnit.MorphTargets)
                {
                    positions.Clear();
                    normals.Clear();
                    foreach (var index in morphTargetUnit.OrderedIndices)
                    {
                        var position = item.Positions[index];
                        var remappedIndex = morphTargetUnit.IndexMappings[index];
                        if (Math.Abs(Vector3.Distance(position, Vector3.Zero)) > 0)
                        {
                            positions.Add(position, remappedIndex);
                        }

                        if (index >= item.Normals.Length)
                            continue;
                        var normal = item.Normals[index];
                        if (Math.Abs(Vector3.Distance(normal, Vector3.Zero)) > 0)
                        {
                            normals.Add(normal, remappedIndex);
                        }
                    }

                    var attributes = new Dictionary<string, ObjectID>();
                    var name = item.Name ?? $"MorphTarget{morphTargets.Count}";
                    var positionsAccessor =
                        CreateSparseAccessorVector3(root, new UnicodeString($"{name}_POSITION"), positions);
                    if (positionsAccessor.HasValue)
                    {
                        attributes.Add("POSITION", positionsAccessor.Value);
                        var normalsAccessor =
                            CreateSparseAccessorVector3(root, new UnicodeString($"{name}_NORMAL"), normals);
                        if (normalsAccessor.HasValue)
                        {
                            attributes.Add("NORMAL", normalsAccessor.Value);
                        }
                    }
                    else
                    {
                        positions.Clear();
                        positions.Add(Vector3.Zero, 0);
                        positionsAccessor =
                            CreateSparseAccessorVector3(root, new UnicodeString($"{name}_POSITION"), positions);
                        attributes.Add("POSITION", positionsAccessor!.Value);
                        if (normals.BaseAccessorCount > 0)
                        {
                            normals.Clear();
                            normals.Add(Vector3.Zero, 0);
                            var normalsAccessor =
                                CreateSparseAccessorVector3(root, new UnicodeString($"{name}_NORMAL"), normals);
                            attributes.Add("NORMAL", normalsAccessor!.Value);
                        }
                    }

                    morphTargets.Add(attributes);
                }

                if (morphTargets.Count > 0)
                {
                    primitive.Targets = morphTargets;
                }
            }

            private void SerializeAnimationSamplerAccessor(Root root, KeyframeAccessorUnit sa, accessor.Type ty,
                accessor.ComponentType componentTy)
            {
                if (sa.Keyframes.Count == 0)
                {
                    return;
                }

                var inputAccessorID = sa.InputAccessor;
                var (inputAccessor, inputBufferView) = CreateAccessor(root, null, ref inputAccessorID);
                sa.InputAccessor = inputAccessorID;
                var outputAccessorID = sa.OutputAccessor;
                var (outputAccessor, outputBufferView) = CreateAccessor(root, null, ref outputAccessorID);
                sa.OutputAccessor = outputAccessorID;
                using var inputMemoryStream = new MemoryStream();
                using var inputByteArray = new BinaryWriter(inputMemoryStream);
                using var outputMemoryStream = new MemoryStream();
                using var outputByteArray = new BinaryWriter(outputMemoryStream);
                var min = float.MaxValue;
                var max = -float.MaxValue;
                if (sa.HasTangent)
                {
                    foreach (var i in sa.Keyframes)
                    {
                        inputByteArray.Write(i.Seconds);
                        foreach (var j in i.InputTangent)
                        {
                            outputByteArray.Write(j);
                        }

                        foreach (var j in i.Value)
                        {
                            outputByteArray.Write(j);
                        }

                        foreach (var j in i.OutputTangent)
                        {
                            outputByteArray.Write(j);
                        }

                        min = Math.Min(min, i.Seconds);
                        max = Math.Max(max, i.Seconds);
                    }
                }
                else
                {
                    foreach (var i in sa.Keyframes)
                    {
                        inputByteArray.Write(i.Seconds);
                        foreach (var j in i.Value)
                        {
                            outputByteArray.Write(j);
                        }

                        min = Math.Min(min, i.Seconds);
                        max = Math.Max(max, i.Seconds);
                    }
                }

                inputAccessor.Count = sa.Keyframes.Count;
                inputAccessor.ComponentType = accessor.ComponentType.Float;
                inputAccessor.Type = accessor.Type.Scalar;
                inputAccessor.Min = new[] { min };
                inputAccessor.Max = new[] { max };
                inputBufferView.ByteOffset = InnerStream.Position;
                inputBufferView.ByteLength = inputMemoryStream.Length;
                inputBufferView.ByteStride = inputMemoryStream.Length / inputAccessor.Count;
                WriteStream(inputMemoryStream);
                outputAccessor.Count = sa.Keyframes.Count;
                outputAccessor.ComponentType = componentTy;
                outputAccessor.Type = ty;
                outputBufferView.ByteOffset = InnerStream.Position;
                outputBufferView.ByteLength = outputMemoryStream.Length;
                outputBufferView.ByteStride = outputMemoryStream.Length / outputAccessor.Count;
                WriteStream(outputMemoryStream);
            }

            private static (accessor.Accessor, buffer.BufferView) CreateAccessor(Root root, UnicodeString? name,
                ref ObjectID accessorID)
            {
                var accessors = root.Accessors!;
                var bufferViews = root.BufferViews!;
                accessor.Accessor accessor;
                buffer.BufferView bufferView;
                if (accessorID.IsNull)
                {
                    ObjectID bufferViewID = new((uint)bufferViews.Count);
                    accessorID = new((uint)accessors.Count);
                    var accessorNameSuffix = $"Accessor{accessorID.ID}";
                    accessor = new accessor.Accessor
                    {
                        Name = new UnicodeString(name != null
                            ? $"{name.Value}_{accessorNameSuffix}"
                            : accessorNameSuffix),
                    };
                    var bufferViewNameSuffix = $"Accessor{accessorID.ID}";
                    bufferView = new buffer.BufferView
                    {
                        Buffer = new ObjectID(0),
                        Name =
                            new UnicodeString(name != null
                                ? $"{name.Value}_{bufferViewNameSuffix}"
                                : bufferViewNameSuffix),
                    };
                    accessor.BufferView = bufferViewID;
                    accessors.Add(accessor);
                    bufferViews.Add(bufferView);
                }
                else
                {
                    accessor = accessors[(int)accessorID.ID];
                    bufferView = bufferViews[(int)accessor.BufferView!.Value.ID];
                }

                return (accessor, bufferView);
            }

            private void WriteStream(MemoryStream stream)
            {
                WriteStream(stream.GetBuffer().Take((int)stream.Position).ToArray());
            }

            private void WriteStream(byte[] bytes)
            {
                InnerStreamWriter.Write(bytes);
                var length = InnerStream.Position;
                var paddingLength = length % Alignment;
                if (paddingLength <= 0)
                {
                    InnerStreamWriter.Flush();
                    return;
                }

                var padding = new byte[Alignment - paddingLength];
                InnerStreamWriter.Write(padding);
                InnerStreamWriter.Flush();
            }

            private struct MorphTargetUnit
            {
                public IImmutableList<uint> OrderedIndices { get; init; }
                public IImmutableDictionary<uint, uint> IndexMappings { get; init; }
                public IImmutableList<Vector3> Positions { get; init; }
                public IImmutableList<Vector3> Normals { get; init; }
            }

            private MemoryStream InnerStream { get; }
            private BinaryWriter InnerStreamWriter { get; }
        }
    }

    public sealed class Root : ICloneable
    {
        public IList<string>? ExtensionsUsed { get; set; }
        public IList<string>? ExtensionsRequired { get; set; }
        public IList<accessor.Accessor>? Accessors { get; set; }
        public IList<animation.Animation>? Animations { get; set; }
        public asset.Asset Asset { get; init; } = new();
        public IList<buffer.Buffer>? Buffers { get; set; }
        public IList<buffer.BufferView>? BufferViews { get; set; }
        public IList<camera.Camera>? Cameras { get; set; }
        public IList<buffer.Image>? Images { get; set; }
        public IList<material.Material>? Materials { get; set; }
        public IList<mesh.Mesh>? Meshes { get; set; }
        public IList<node.Node>? Nodes { get; set; }
        public IList<material.Sampler>? Samplers { get; set; }
        public ObjectID? Scene { get; set; }
        public IList<scene.Scene>? Scenes { get; set; }
        public IList<node.Skin>? Skins { get; set; }
        public IList<material.Texture>? Textures { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }

        public object Clone()
        {
            return new Root
            {
                ExtensionsUsed = ListUtils<string>.DeepClone(ExtensionsUsed),
                ExtensionsRequired = ListUtils<string>.DeepClone(ExtensionsRequired),
                Asset = (asset.Asset)Asset.Clone(),
                Accessors = ListUtils<accessor.Accessor>.DeepClone(Accessors),
                Animations = ListUtils<animation.Animation>.DeepClone(Animations),
                Buffers = ListUtils<buffer.Buffer>.DeepClone(Buffers),
                BufferViews = ListUtils<buffer.BufferView>.DeepClone(BufferViews),
                Cameras = ListUtils<camera.Camera>.DeepClone(Cameras),
                Images = ListUtils<buffer.Image>.DeepClone(Images),
                Materials = ListUtils<material.Material>.DeepClone(Materials),
                Meshes = ListUtils<mesh.Mesh>.DeepClone(Meshes),
                Nodes = ListUtils<node.Node>.DeepClone(Nodes),
                Samplers = ListUtils<material.Sampler>.DeepClone(Samplers),
                Scene = Scene,
                Scenes = ListUtils<scene.Scene>.DeepClone(Scenes),
                Skins = ListUtils<node.Skin>.DeepClone(Skins),
                Textures = ListUtils<material.Texture>.DeepClone(Textures),
                Extensions = ExtensionsUtils.DeepClone(Extensions),
                Extras = Extras?.DeepClone(),
            };
        }

        public void Normalize()
        {
            if (ExtensionsUsed?.Count == 0)
            {
                ExtensionsUsed = null;
            }

            if (ExtensionsRequired?.Count == 0)
            {
                ExtensionsRequired = null;
            }

            if (Accessors?.Count == 0)
            {
                Accessors = null;
            }

            if (Buffers?.Count == 0)
            {
                Buffers = null;
            }

            if (BufferViews?.Count == 0)
            {
                BufferViews = null;
            }

            if (Cameras?.Count == 0)
            {
                Cameras = null;
            }

            if (Images?.Count == 0)
            {
                Images = null;
            }

            if (Materials?.Count == 0)
            {
                Materials = null;
            }

            if (Meshes?.Count > 0)
            {
                foreach (var item in Meshes)
                {
                    item.Normalize();
                }
            }
            else
            {
                Meshes = null;
            }

            if (Nodes?.Count > 0)
            {
                foreach (var item in Nodes)
                {
                    item.Normalize();
                }
            }
            else
            {
                Nodes = null;
            }

            if (Samplers?.Count == 0)
            {
                Samplers = null;
            }

            if (Scenes?.Count == 0)
            {
                Scenes = null;
            }

            if (Skins?.Count == 0)
            {
                Skins = null;
            }

            if (Textures?.Count == 0)
            {
                Textures = null;
            }

            if (Extensions?.Count == 0)
            {
                Extensions = null;
            }
        }
    }

    public class Document
    {
        public static Root? LoadFromString(string json)
        {
            return JsonConvert.DeserializeObject<Root>(json, SerializerOptions);
        }

        public static string SaveAsString(Root root)
        {
            return SaveAsNode(root).ToString(Formatting.None);
        }

        public static JToken SaveAsNode(Root root)
        {
            return JToken.FromObject(root, JsonSerializer.Create(SerializerOptions));
        }

        public static JToken SaveAsNode(extensions.KhrMaterialsEmissiveStrength value)
        {
            return JToken.FromObject(value, JsonSerializer.Create(SerializerOptions));
        }

        public static JToken SaveAsNode(extensions.KhrTextureTransform value)
        {
            return JToken.FromObject(value, JsonSerializer.Create(SerializerOptions));
        }

        public static JToken SaveAsNode(extensions.KhrTextureBasisu value)
        {
            return JToken.FromObject(value, JsonSerializer.Create(SerializerOptions));
        }

        public static JsonSerializerSettings SerializerOptions
        {
            get
            {
                var options = BaseSerializerOptions;
                options.Converters = new List<JsonConverter>
                {
                    new ObjectIDConverter(),
                    new Vector3Converter(),
                    new Vector4Converter(),
                    new Matrix4Converter(),
                    new QuaternionConverter(),
                    new UnicodeStringConverter(),
                    new accessor.ComponentTypeConverter(),
                    new accessor.TypeConverter(),
                    new animation.InterpolationConverter(),
                    new animation.PathConverter(),
                    new buffer.TargetConverter(),
                    new material.AlphaModeConverter(),
                    new material.TextureFilterModeConverter(),
                    new material.TextureWrapModeConverter(),
                    new mesh.PrimitiveModeConverter(),
                };
                return options;
            }
        }

        internal static JsonSerializerSettings BaseSerializerOptions =>
            new()
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore,
                StringEscapeHandling = StringEscapeHandling.Default,
            };
    }
}

namespace com.github.hkrn.vrm.core
{
    using IExtensions = IDictionary<string, JValue>;

    public enum AvatarPermission
    {
        OnlyAuthor,
        OnlySeparatelyLicensedPerson,
        Everyone,
    }

    internal sealed class AvatarPermissionConverter : JsonConverter<AvatarPermission?>
    {
        public override void WriteJson(JsonWriter writer, AvatarPermission? value, JsonSerializer serializer)
        {
            if (!value.HasValue)
                return;
            var v = value switch
            {
                AvatarPermission.OnlyAuthor => "onlyAuthor",
                AvatarPermission.OnlySeparatelyLicensedPerson => "onlySeparatelyLicensedPerson",
                AvatarPermission.Everyone => "everyone",
                _ => throw new JsonException(),
            };
            writer.WriteValue(v);
        }

        public override AvatarPermission? ReadJson(JsonReader reader, Type objectType, AvatarPermission? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                return null;
            return reader.Value switch
            {
                "onlyAuthor" => AvatarPermission.OnlyAuthor,
                "onlySeparatelyLicensedPerson" => AvatarPermission.OnlySeparatelyLicensedPerson,
                "everyone" => AvatarPermission.Everyone,
                _ => throw new JsonException(),
            };
        }
    }

    public enum CommercialUsage
    {
        PersonalNonProfit,
        PersonalProfit,
        Corporation,
    }

    internal sealed class CommercialUsageConverter : JsonConverter<CommercialUsage?>
    {
        public override void WriteJson(JsonWriter writer, CommercialUsage? value, JsonSerializer serializer)
        {
            if (!value.HasValue)
                return;
            var v = value switch
            {
                CommercialUsage.PersonalNonProfit => "personalNonProfit",
                CommercialUsage.PersonalProfit => "personalProfit",
                CommercialUsage.Corporation => "corporation",
                _ => throw new JsonException(),
            };
            writer.WriteValue(v);
        }

        public override CommercialUsage? ReadJson(JsonReader reader, Type objectType, CommercialUsage? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                return null;
            return reader.Value switch
            {
                "personalNonProfit" => CommercialUsage.PersonalNonProfit,
                "personalProfit" => CommercialUsage.PersonalProfit,
                "corporation" => CommercialUsage.Corporation,
                _ => throw new JsonException(),
            };
        }
    }

    public enum Modification
    {
        Prohibited,
        AllowModification,
        AllowModificationRedistribution,
    }

    internal sealed class ModificationConverter : JsonConverter<Modification?>
    {
        public override void WriteJson(JsonWriter writer, Modification? value, JsonSerializer serializer)
        {
            if (!value.HasValue)
                return;
            var v = value switch
            {
                Modification.Prohibited => "prohibited",
                Modification.AllowModification => "allowModification",
                Modification.AllowModificationRedistribution => "allowModificationRedistribution",
                _ => throw new JsonException(),
            };
            writer.WriteValue(v);
        }

        public override Modification? ReadJson(JsonReader reader, Type objectType, Modification? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                return null;
            return reader.Value switch
            {
                "prohibited" => Modification.Prohibited,
                "allowModification" => Modification.AllowModification,
                "allowModificationRedistribution" => Modification.AllowModificationRedistribution,
                _ => throw new JsonException(),
            };
        }
    }

    public enum CreditNotation
    {
        Required,
        Unnecessary,
    }

    internal sealed class CreditNotationConverter : JsonConverter<CreditNotation?>
    {
        public override void WriteJson(JsonWriter writer, CreditNotation? value, JsonSerializer serializer)
        {
            if (!value.HasValue)
                return;
            var v = value switch
            {
                CreditNotation.Required => "required",
                CreditNotation.Unnecessary => "unnecessary",
                _ => throw new JsonException(),
            };
            writer.WriteValue(v);
        }

        public override CreditNotation? ReadJson(JsonReader reader, Type objectType, CreditNotation? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                return null;
            return reader.Value switch
            {
                "required" => CreditNotation.Required,
                "unnecessary" => CreditNotation.Unnecessary,
                _ => throw new JsonException(),
            };
        }
    }

    public enum FirstPersonType
    {
        Unknown,
        Auto,
        Both,
        ThirdPersonOnly,
        FirstPersonOnly,
    }

    internal sealed class FirstPersonTypeConverter : JsonConverter<FirstPersonType?>
    {
        public override void WriteJson(JsonWriter writer, FirstPersonType? value, JsonSerializer serializer)
        {
            if (!value.HasValue)
                return;
            var v = value switch
            {
                FirstPersonType.Auto => "auto",
                FirstPersonType.Both => "both",
                FirstPersonType.ThirdPersonOnly => "thirdPersonOnly",
                FirstPersonType.FirstPersonOnly => "firstPersonOnly",
                _ => throw new JsonException(),
            };
            writer.WriteValue(v);
        }

        public override FirstPersonType? ReadJson(JsonReader reader, Type objectType, FirstPersonType? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                return null;
            return reader.Value switch
            {
                "auto" => FirstPersonType.Auto,
                "both" => FirstPersonType.Both,
                "thirdPersonOnly" => FirstPersonType.ThirdPersonOnly,
                "firstPersonOnly" => FirstPersonType.FirstPersonOnly,
                _ => throw new JsonException(),
            };
        }
    }

    public enum ExpressionOverrideType
    {
        None,
        Block,
        Blend,
    }

    internal sealed class ExpressionOverrideTypeConverter : JsonConverter<ExpressionOverrideType?>
    {
        public override void WriteJson(JsonWriter writer, ExpressionOverrideType? value, JsonSerializer serializer)
        {
            if (!value.HasValue)
                return;
            var v = value switch
            {
                ExpressionOverrideType.None => "none",
                ExpressionOverrideType.Block => "block",
                ExpressionOverrideType.Blend => "blend",
                _ => throw new JsonException(),
            };
            writer.WriteValue(v);
        }

        public override ExpressionOverrideType? ReadJson(JsonReader reader, Type objectType,
            ExpressionOverrideType? existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                return null;
            return reader.Value switch
            {
                "none" => ExpressionOverrideType.None,
                "block" => ExpressionOverrideType.Block,
                "blend" => ExpressionOverrideType.Blend,
                _ => throw new JsonException(),
            };
        }
    }

    public enum MaterialColorType
    {
        Color,
        EmissionColor,
        ShadeColor,
        RimColor,
        OutlineColor,
    }

    internal sealed class MaterialColorTypeConverter : JsonConverter<MaterialColorType?>
    {
        public override void WriteJson(JsonWriter writer, MaterialColorType? value, JsonSerializer serializer)
        {
            if (!value.HasValue)
                return;
            var v = value switch
            {
                MaterialColorType.Color => "color",
                MaterialColorType.EmissionColor => "emissionColor",
                MaterialColorType.ShadeColor => "shadeColor",
                MaterialColorType.RimColor => "rimColor",
                MaterialColorType.OutlineColor => "outlineColor",
                _ => throw new JsonException(),
            };
            writer.WriteValue(v);
        }

        public override MaterialColorType? ReadJson(JsonReader reader, Type objectType,
            MaterialColorType? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                return null;
            return reader.Value switch
            {
                "color" => MaterialColorType.Color,
                "emissionColor" => MaterialColorType.EmissionColor,
                "shadeColor" => MaterialColorType.ShadeColor,
                "rimColor" => MaterialColorType.RimColor,
                "outlineColor" => MaterialColorType.OutlineColor,
                _ => throw new JsonException(),
            };
        }
    }

    public enum LookAtType
    {
        Unknown,
        Bone,
        Expression,
    }

    internal sealed class LookAtTypeConverter : JsonConverter<LookAtType?>
    {
        public override void WriteJson(JsonWriter writer, LookAtType? value, JsonSerializer serializer)
        {
            if (!value.HasValue)
                return;
            var v = value switch
            {
                LookAtType.Bone => "bone",
                LookAtType.Expression => "expression",
                _ => throw new JsonException(),
            };
            writer.WriteValue(v);
        }

        public override LookAtType? ReadJson(JsonReader reader, Type objectType, LookAtType? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                return null;
            return reader.Value switch
            {
                "bone" => LookAtType.Bone,
                "expression" => LookAtType.Expression,
                _ => throw new JsonException(),
            };
        }
    }

    public sealed class HumanBone
    {
        public gltf.ObjectID Node { get; set; } = gltf.ObjectID.Null;
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class HumanBones
    {
        public HumanBone Hips { get; init; } = new();
        public HumanBone Spine { get; init; } = new();
        public HumanBone? Chest { get; set; }
        public HumanBone? UpperChest { get; set; }
        public HumanBone? Neck { get; set; }
        public HumanBone Head { get; init; } = new();
        public HumanBone? LeftEye { get; set; }
        public HumanBone? RightEye { get; set; }
        public HumanBone? Jaw { get; set; }
        public HumanBone LeftUpperLeg { get; init; } = new();
        public HumanBone LeftLowerLeg { get; init; } = new();
        public HumanBone LeftFoot { get; init; } = new();
        public HumanBone? LeftToes { get; set; }
        public HumanBone RightUpperLeg { get; init; } = new();
        public HumanBone RightLowerLeg { get; init; } = new();
        public HumanBone RightFoot { get; init; } = new();
        public HumanBone? RightToes { get; set; }
        public HumanBone? LeftShoulder { get; set; }
        public HumanBone LeftUpperArm { get; init; } = new();
        public HumanBone LeftLowerArm { get; init; } = new();
        public HumanBone LeftHand { get; init; } = new();
        public HumanBone? RightShoulder { get; set; }
        public HumanBone RightUpperArm { get; init; } = new();
        public HumanBone RightLowerArm { get; init; } = new();
        public HumanBone RightHand { get; init; } = new();
        public HumanBone? LeftThumbMetacarpal { get; set; }
        public HumanBone? LeftThumbProximal { get; set; }
        public HumanBone? LeftThumbDistal { get; set; }
        public HumanBone? LeftIndexProximal { get; set; }
        public HumanBone? LeftIndexIntermediate { get; set; }
        public HumanBone? LeftIndexDistal { get; set; }
        public HumanBone? LeftMiddleProximal { get; set; }
        public HumanBone? LeftMiddleIntermediate { get; set; }
        public HumanBone? LeftMiddleDistal { get; set; }
        public HumanBone? LeftRingProximal { get; set; }
        public HumanBone? LeftRingIntermediate { get; set; }
        public HumanBone? LeftRingDistal { get; set; }
        public HumanBone? LeftLittleProximal { get; set; }
        public HumanBone? LeftLittleIntermediate { get; set; }
        public HumanBone? LeftLittleDistal { get; set; }
        public HumanBone? RightThumbMetacarpal { get; set; }
        public HumanBone? RightThumbProximal { get; set; }
        public HumanBone? RightThumbDistal { get; set; }
        public HumanBone? RightIndexProximal { get; set; }
        public HumanBone? RightIndexIntermediate { get; set; }
        public HumanBone? RightIndexDistal { get; set; }
        public HumanBone? RightMiddleProximal { get; set; }
        public HumanBone? RightMiddleIntermediate { get; set; }
        public HumanBone? RightMiddleDistal { get; set; }
        public HumanBone? RightRingProximal { get; set; }
        public HumanBone? RightRingIntermediate { get; set; }
        public HumanBone? RightRingDistal { get; set; }
        public HumanBone? RightLittleProximal { get; set; }
        public HumanBone? RightLittleIntermediate { get; set; }
        public HumanBone? RightLittleDistal { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class Humanoid
    {
        public HumanBones HumanBones { get; set; } = new();
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class Meta
    {
        public static readonly string DefaultLicenseUrl = "https://vrm.dev/licenses/1.0/";
        public string Name { get; set; } = "(null)";
        public string? Version { get; set; }
        public IList<string> Authors { get; set; } = new List<string>();
        public string? CopyrightInformation { get; set; }
        public string? ContactInformation { get; set; }
        public IList<string>? References { get; set; }
        public string? ThirdPartyLicenses { get; set; }
        public string LicenseUrl { get; set; } = "";
        public gltf.ObjectID? ThumbnailImage { get; set; }
        public AvatarPermission? AvatarPermission { get; set; }
        public bool? AllowExcessivelyViolentUsage { get; set; }
        public bool? AllowExcessivelySexualUsage { get; set; }
        public CommercialUsage? CommercialUsage { get; set; }
        public bool? AllowPoliticalOrReligiousUsage { get; set; }
        public bool? AllowAntisocialOrHateUsage { get; set; }
        public CreditNotation? CreditNotation { get; set; }
        public bool? AllowRedistribution { get; set; }
        public Modification? Modification { get; set; }
        public string? OtherLicenseUrl { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class MeshAnnotation
    {
        public gltf.ObjectID Node { get; set; } = gltf.ObjectID.Null;
        public FirstPersonType Type { get; set; } = FirstPersonType.Unknown;
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class FirstPerson
    {
        public IList<MeshAnnotation>? MeshAnnotations { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class MorphTargetBind
    {
        public gltf.ObjectID Node { get; set; } = gltf.ObjectID.Null;
        public gltf.ObjectID Index { get; set; } = gltf.ObjectID.Null;
        public float Weight { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class MaterialColorBind
    {
        public gltf.ObjectID Material { get; set; } = gltf.ObjectID.Null;
        public Vector4 TargetValue { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class TextureTransformBind
    {
        public gltf.ObjectID Material { get; set; } = gltf.ObjectID.Null;
        public Vector2 Scale { get; set; }
        public Vector2 Offset { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class ExpressionItem
    {
        public IList<MorphTargetBind>? MorphTargetBinds { get; set; }
        public IList<MaterialColorBind>? MaterialColorBinds { get; set; }
        public IList<TextureTransformBind>? TextureTransformBinds { get; set; }
        public bool IsBinary { get; set; }
        public ExpressionOverrideType? OverrideBlink { get; set; }
        public ExpressionOverrideType? OverrideLookAt { get; set; }
        public ExpressionOverrideType? OverrideMouth { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class Preset
    {
        public ExpressionItem? Happy { get; set; }
        public ExpressionItem? Angry { get; set; }
        public ExpressionItem? Sad { get; set; }
        public ExpressionItem? Relaxed { get; set; }
        public ExpressionItem? Surprised { get; set; }
        public ExpressionItem? Aa { get; set; }
        public ExpressionItem? Ih { get; set; }
        public ExpressionItem? Ou { get; set; }
        public ExpressionItem? Ee { get; set; }
        public ExpressionItem? Oh { get; set; }
        public ExpressionItem? Blink { get; set; }
        public ExpressionItem? BlinkLeft { get; set; }
        public ExpressionItem? BlinkRight { get; set; }
        public ExpressionItem? LookUp { get; set; }
        public ExpressionItem? LookDown { get; set; }
        public ExpressionItem? LookLeft { get; set; }
        public ExpressionItem? LookRight { get; set; }
        public ExpressionItem? Neutral { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class Expressions
    {
        public Preset Preset { get; init; } = new();
        public IDictionary<gltf.UnicodeString, ExpressionItem>? Custom { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class RangeMap
    {
        public float? InputMaxValue { get; set; }
        public float? OutputScale { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class LookAt
    {
        public LookAtType Type { get; set; } = LookAtType.Unknown;
        public Vector3? OffsetFromHeadBone { get; set; }
        public RangeMap? RangeMapHorizontalInner { get; set; }
        public RangeMap? RangeMapHorizontalOuter { get; set; }
        public RangeMap? RangeMapVerticalDown { get; set; }
        public RangeMap? RangeMapVerticalUp { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class Core
    {
        public string SpecVersion { get; set; } = "1.0";
        public Humanoid Humanoid { get; init; } = new();
        public Meta Meta { get; init; } = new();
        public FirstPerson? FirstPerson { get; set; }
        public Expressions? Expressions { get; set; }
        public LookAt? LookAt { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class MaterialsHdrEmissiveMultiplier
    {
        public float EmissiveMultiplier { get; set; } = 1.0f;
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }
}

namespace com.github.hkrn.vrm.sb
{
    using IExtensions = IDictionary<string, JToken>;

    public sealed class Capsule
    {
        public Vector3 Offset { get; set; }
        public float Radius { get; set; }
        public Vector3 Tail { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class Sphere
    {
        public Vector3 Offset { get; set; }
        public float Radius { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class Shape
    {
        public Capsule? Capsule { get; set; }
        public Sphere? Sphere { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class Collider
    {
        public gltf.ObjectID Node { get; set; } = gltf.ObjectID.Null;
        public Shape Shape { get; init; } = new();
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class ShapeCapsule
    {
        public Vector3 Offset { get; set; }
        public float Radius { get; set; }
        public Vector3 Tail { get; set; }
        public bool Inside { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class ShapeSphere
    {
        public Vector3 Offset { get; set; }
        public float Radius { get; set; }
        public bool Inside { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class ShapePlane
    {
        public Vector3 Offset { get; set; }
        public Vector3 Normal { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }


    public sealed class ExtendedShape
    {
        public ShapeCapsule? Capsule { get; set; }
        public ShapeSphere? Sphere { get; set; }
        public ShapePlane? Plane { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class ExtendedCollider
    {
        public string Spec { get; set; } = "1.0";
        public ExtendedShape Shape { get; init; } = new();
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class ColliderGroup
    {
        public gltf.UnicodeString? Name { get; set; }
        public IList<gltf.ObjectID> Colliders { get; init; } = new List<gltf.ObjectID>();
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class Joint
    {
        public gltf.ObjectID Node { get; set; } = gltf.ObjectID.Null;
        public float HitRadius { get; set; }
        public float Stiffness { get; set; } = 1.0f;
        public float GravityPower { get; set; } = 0.0f;
        public Vector3 GravityDir { get; set; } = -Vector3.UnitY;
        public float DragForce { get; set; } = 0.5f;
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class Spring
    {
        public gltf.ObjectID? Center { get; set; }
        public gltf.UnicodeString? Name { get; set; }
        public IList<Joint> Joints { get; init; } = new List<Joint>();
        public IList<gltf.ObjectID>? ColliderGroups { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class SpringBone
    {
        public string SpecVersion { get; set; } = "1.0";
        public IList<Collider>? Colliders { get; set; }
        public IList<ColliderGroup>? ColliderGroups { get; set; }
        public IList<Spring>? Springs { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }
}

namespace com.github.hkrn.vrm.constraint
{
    using IExtensions = IDictionary<string, JToken>;

    public sealed class AimConstraint
    {
        public gltf.ObjectID Source { get; set; } = gltf.ObjectID.Null;
        public string AimAxis { get; set; } = "";
        public float Weight { get; set; } = 1.0f;
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class RollConstraint
    {
        public gltf.ObjectID Source { get; set; } = gltf.ObjectID.Null;
        public string RollAxis { get; set; } = "";
        public float Weight { get; set; } = 1.0f;
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class RotationConstraint
    {
        public gltf.ObjectID Source { get; set; } = gltf.ObjectID.Null;
        public float Weight { get; set; } = 1.0f;
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class Constraint
    {
        public RollConstraint? Roll { get; set; }
        public AimConstraint? Aim { get; set; }
        public RotationConstraint? Rotation { get; set; }
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class NodeConstraint
    {
        public string SpecVersion { get; set; } = "1.0";
        public Constraint Constraint { get; init; } = new();
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }
}

namespace com.github.hkrn.vrm.mtoon
{
    using IExtensions = IDictionary<string, JToken>;

    public enum OutlineWidthMode
    {
        None,
        WorldCoordinates,
        ScreenCoordinates,
    }

    internal sealed class OutlineWidthModeConverter : JsonConverter<OutlineWidthMode>
    {
        public override void WriteJson(JsonWriter writer, OutlineWidthMode value, JsonSerializer serializer)
        {
            var v = value switch
            {
                OutlineWidthMode.None => "none",
                OutlineWidthMode.WorldCoordinates => "worldCoordinates",
                OutlineWidthMode.ScreenCoordinates => "screenCoordinates",
                _ => throw new JsonException(),
            };
            writer.WriteValue(v);
        }

        public override OutlineWidthMode ReadJson(JsonReader reader, Type objectType, OutlineWidthMode existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            return reader.ReadAsString() switch
            {
                "none" => OutlineWidthMode.None,
                "worldCoordinates" => OutlineWidthMode.WorldCoordinates,
                "screenCoordinates" => OutlineWidthMode.ScreenCoordinates,
                _ => throw new JsonException(),
            };
        }
    }

    public sealed class ShadingShiftTexture
    {
        public gltf.ObjectID Index { get; set; } = gltf.ObjectID.Null;
        public uint? TexCoord { get; set; }
        public float Scale { get; set; } = 1.0f;
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }

    public sealed class MToon
    {
        public string SpecVersion { get; set; } = "1.0";
        public bool TransparentWithZWrite { get; set; } = false;
        public int RenderQueueOffsetNumber { get; set; } = 0;
        public Vector3 ShadeColorFactor { get; set; } = Vector3.Zero;
        public gltf.material.TextureInfo? ShadeMultiplyTexture { get; set; }
        public float ShadingShiftFactor { get; set; } = 0.0f;
        public ShadingShiftTexture? ShadingShiftTexture { get; set; }
        public float ShadingToonyFactor { get; set; } = 0.9f;
        public float GIEqualizationFactor { get; set; } = 0.9f;
        public Vector3 MatcapFactor { get; set; } = Vector3.One;
        public gltf.material.TextureInfo? MatcapTexture { get; set; }
        public Vector3 ParametricRimColorFactor { get; set; } = Vector3.Zero;
        public gltf.material.TextureInfo? RimMultiplyTexture { get; set; }
        public float RimLightingMixFactor { get; set; } = 1.0f;
        public float ParametricRimFresnelPowerFactor { get; set; } = 5.0f;
        public float ParametricRimLiftFactor { get; set; } = 0.0f;
        public OutlineWidthMode OutlineWidthMode { get; set; } = OutlineWidthMode.None;
        public float OutlineWidthFactor { get; set; } = 0.0f;
        public gltf.material.TextureInfo? OutlineWidthMultiplyTexture { get; set; }
        public Vector3 OutlineColorFactor { get; set; } = Vector3.Zero;
        public float OutlineLightingMixFactor { get; set; } = 1.0f;
        public gltf.material.TextureInfo? UVAnimationMaskTexture { get; set; }
        public float UVAnimationScrollXSpeedFactor { get; set; } = 0.0f;
        public float UVAnimationScrollYSpeedFactor { get; set; } = 0.0f;
        public float UVAnimationRotationSpeedFactor { get; set; } = 0.0f;
        public IExtensions? Extensions { get; set; }
        public JToken? Extras { get; set; }
    }
}

namespace com.github.hkrn.vrm
{
    public class Document
    {
        public static JToken SaveAsNode(core.Core core)
        {
            return JToken.FromObject(core, JsonSerializer.Create(SerializerOptions));
        }

        public static JToken SaveAsNode(core.MaterialsHdrEmissiveMultiplier emissive)
        {
            return JToken.FromObject(emissive, JsonSerializer.Create(SerializerOptions));
        }

        public static JToken SaveAsNode(sb.SpringBone springBone)
        {
            return JToken.FromObject(springBone, JsonSerializer.Create(SerializerOptions));
        }

        public static JToken SaveAsNode(sb.ExtendedCollider collider)
        {
            return JToken.FromObject(collider, JsonSerializer.Create(SerializerOptions));
        }

        public static JToken SaveAsNode(constraint.NodeConstraint nodeConstraint)
        {
            return JToken.FromObject(nodeConstraint, JsonSerializer.Create(SerializerOptions));
        }

        public static JToken SaveAsNode(mtoon.MToon mtoon)
        {
            return JToken.FromObject(mtoon, JsonSerializer.Create(SerializerOptions));
        }

        public static JsonSerializerSettings SerializerOptions
        {
            get
            {
                var options = gltf.Document.BaseSerializerOptions;
                options.Converters = new List<JsonConverter>
                {
                    new gltf.ObjectIDConverter(),
                    new gltf.Vector3Converter(),
                    new gltf.Vector4Converter(),
                    new gltf.Matrix4Converter(),
                    new gltf.QuaternionConverter(),
                    new gltf.UnicodeStringConverter(),
                    new core.AvatarPermissionConverter(),
                    new core.CommercialUsageConverter(),
                    new core.ModificationConverter(),
                    new core.CreditNotationConverter(),
                    new core.FirstPersonTypeConverter(),
                    new core.ExpressionOverrideTypeConverter(),
                    new core.MaterialColorTypeConverter(),
                    new core.LookAtTypeConverter(),
                    new mtoon.OutlineWidthModeConverter(),
                };
                return options;
            }
        }
    }
}
