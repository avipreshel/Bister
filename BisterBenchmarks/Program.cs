using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BisterLib;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BisterBenchmarks
{
    public class EnumTypeNameConverter : JsonConverter<Enum>
    {
        public override Enum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                var typeName = doc.RootElement.GetProperty("$type").GetString()!;
                var type = Type.GetType(typeName)!;
                var value = doc.RootElement.GetProperty("Value").GetString()!;
                return (Enum)Enum.Parse(type, value);
            }
        }

        public override void Write(Utf8JsonWriter writer, Enum value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("$type", value.GetType().AssemblyQualifiedName);
            writer.WriteString("Value", value.ToString());
            writer.WriteEndObject();
        }
    }

    public class SystemObjectConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                var typeName = doc.RootElement.GetProperty("$type").GetString()!;
                var type = Type.GetType(typeName)!;
                return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), type, options)!;
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            var varType = value.GetType();
            writer.WriteString("$type", $"{value.GetType().FullName},{varType.Assembly.GetName().Name}");
            foreach (var property in value.GetType().GetProperties())
            {
                writer.WritePropertyName(property.Name);
                JsonSerializer.Serialize(writer, property.GetValue(value), options);
            }
            writer.WriteEndObject();
        }
    }

    [MemoryDiagnoser]
    public class Program
    {
        ClassWithArrays _instance;
        string _instanceAsJson;
        byte[] _instanceAsBister;
        JsonSerializerOptions _jsonSettings;
        public Program()
        {
            _jsonSettings = new JsonSerializerOptions
            {
                 Converters = { new SystemObjectConverter(), new EnumTypeNameConverter() }
            };

            _instance = new ClassWithArrays()
            {
                ArrayPropSystemEnum = [TestEnum.Three, TestEnum.Two, TestEnum.One],
                ArrayPropInt = [1, 2, 3, 4, 5],
                ArrayPropString = ["wow", "this", "is","very", "cool"],
                ArrayPropTestEnum = [TestEnum.One, TestEnum.Two, TestEnum.Three],
                ArrayPropDateTime = [new DateTime(), DateTime.Now, DateTime.UtcNow, DateTime.MinValue, DateTime.MaxValue, DateTime.FromOADate(0), DateTime.FromFileTime(0), DateTime.FromBinary(0), DateTime.FromBinary(123)],
                ArrayPropTimeSpan = [new TimeSpan(), TimeSpan.Zero, TimeSpan.MinValue, TimeSpan.MaxValue, DateTime.Now.TimeOfDay]
            };
            
            _instanceAsJson = System.Text.Json.JsonSerializer.Serialize(_instance, _jsonSettings);
            _instanceAsBister = Bister.Instance.Serialize(_instance);
        }

        [Benchmark]
        public string SystemTextJsonSerialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(_instance, _jsonSettings);
        }

        [Benchmark]
        public ClassWithArrays? SystemTextJsonDeserialize()
        {
            return System.Text.Json.JsonSerializer.Deserialize<ClassWithArrays>(_instanceAsJson, _jsonSettings);
        }

        [Benchmark]
        public byte[] BisterSerialize()
        {
            return Bister.Instance.Serialize(_instance);
        }

        [Benchmark]
        public ClassWithArrays? BisterDeserialize()
        {
            return Bister.Instance.Deserialize<ClassWithArrays>(_instanceAsBister);
        }

        static void Main(string[] args)
        {
            var _instance = new ClassWithArrays()
            {
                ArrayPropSystemEnum = [TestEnum.Three, TestEnum.Two, TestEnum.One],
                ArrayPropInt = [1, 2, 3, 4, 5],
                ArrayPropString = ["wow", "this", "is", "very", "cool"],
                ArrayPropTestEnum = [TestEnum.One, TestEnum.Two, TestEnum.Three],
                ArrayPropDateTime = [new DateTime(), DateTime.Now, DateTime.UtcNow, DateTime.MinValue, DateTime.MaxValue, DateTime.FromOADate(0), DateTime.FromFileTime(0), DateTime.FromBinary(0), DateTime.FromBinary(123)],
                ArrayPropTimeSpan = [new TimeSpan(), TimeSpan.Zero, TimeSpan.MinValue, TimeSpan.MaxValue, DateTime.Now.TimeOfDay]
            };

            var _instanceAsJson = System.Text.Json.JsonSerializer.Serialize(_instance);

            _ = BenchmarkRunner.Run<Program>();
        }
    }
}
