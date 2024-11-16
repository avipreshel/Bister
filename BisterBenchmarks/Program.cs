using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BisterLib;
using FastSerialization;
using Microsoft.Diagnostics.Runtime;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BisterBenchmarks
{
    interface Interface<T>
    {
        T DoSomething(T val);
    }

    class IntA : Interface<int>
    {
        public int DoSomething(int val)
        {
            Console.WriteLine("IntA");
            return val;
        }
    }
    class IntB : Interface<float>
    {
        public float DoSomething(float val)
        {
            Console.WriteLine("IntB");
            return val;
        }
    }


    public class EnumTypeNameConverter : JsonConverter<Enum>
    {
        public override Enum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

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
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            var varType = value.GetType();
            writer.WriteString("$type", $"{value.GetType().FullName},{varType.Assembly.GetName().Name}");
            writer.WriteString("Value", value.ToString());
            writer.WriteEndObject();
        }
    }

    public class SystemObjectConverter : JsonConverter<object>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                var typeName = doc.RootElement.GetProperty("$type").GetString()!;
                var type = Type.GetType(typeName)!;
                return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), type, options)!;
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            var varType = value.GetType();
            writer.WriteString("$type", $"{varType.FullName},{varType.Assembly.GetName().Name}");
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
                ArrayPropString = ["wow", "this", "is", "very", "cool"],
                ArrayPropTestEnum = [TestEnum.One, TestEnum.Two, TestEnum.Three],
                ArrayPropDateTime = [new DateTime(), DateTime.Now, DateTime.UtcNow, DateTime.MinValue, DateTime.MaxValue, DateTime.FromOADate(0), DateTime.FromFileTime(0), DateTime.FromBinary(0), DateTime.FromBinary(123)],
                ArrayPropTimeSpan = [new TimeSpan(), TimeSpan.Zero, TimeSpan.MinValue, TimeSpan.MaxValue, DateTime.Now.TimeOfDay],
                DicStr2Float = Enumerable.Range(0, 1000).ToDictionary(i => i.ToString(), i => (float)i),
                ListDT = Enumerable.Range(0, 1000).Select(i=>DateTime.FromFileTime(i)).ToList()
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
            Serialize<int>(1);
            Serialize<float>(2f);

            _ = BenchmarkRunner.Run<Program>();
        }

        static void Serialize<T>(T val)
        {
            Dictionary<Type, object> dict = new Dictionary<Type, object>();
            dict.Add(typeof(Interface<int>), new IntA());
            dict.Add(typeof(Interface<float>), new IntB());

            Interface<T> serializer = (Interface<T>)dict[typeof(Interface<T>)];
            serializer.DoSomething(val);

        }
    }
}
