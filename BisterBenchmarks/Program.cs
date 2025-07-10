using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BinaryPack;
using BisterLib;
using FastSerialization;
using MessagePack;
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
        SimpleClass _instance;
        string _instanceAsJson;
        byte[] _instanceAsBister;
        byte[] _instanceAsBinaryPack;
        byte[] _instanceAsMessagePack;
        JsonSerializerOptions _jsonSettings;
        public Program()
        {
            _jsonSettings = new JsonSerializerOptions
            {
                 Converters = { new SystemObjectConverter(), new EnumTypeNameConverter() }
            };

            //_instance = new ClassWithArrays()
            //{
            //    ArrayPropInt = Enumerable.Range(0, 1000).Select(i=>i).ToArray(),
            //    ArrayPropString = Enumerable.Range(0, 1000).Select(i => $"Number{i}").ToArray(),
            //    ArrayPropTestEnum = Enumerable.Range(0, 1000).Select(i => (TestEnum)(i % 3)).ToArray(),
            //    ArrayPropDateTime = Enumerable.Range(0, 1000).Select(i => DateTime.Now.AddMinutes(i)).ToArray(),
            //    ArrayPropTimeSpan = Enumerable.Range(0, 1000).Select(i => TimeSpan.FromDays(i)).ToArray(),
            //    DicStr2Float = Enumerable.Range(0, 1000).ToDictionary(i => i.ToString(), i => (float)i),
            //    ListDT = Enumerable.Range(0, 1000).Select(i=>DateTime.FromFileTime(i).ToString()).ToList()
            //};

            _instance = new SimpleClass()
            {
                ArrayPropInt = Enumerable.Range(0, 100).Select(i => i).ToArray(),
                ArrayPropString = Enumerable.Range(0, 100).Select(i => $"Number{i}").ToArray(),
                DicStr2Float = Enumerable.Range(0, 100).ToDictionary(i => i.ToString(), i => (float)i),
                ListOfStrings = Enumerable.Range(0, 100).Select(i => i.ToString()).ToList(),
            };

            _instanceAsJson = System.Text.Json.JsonSerializer.Serialize(_instance, _jsonSettings);
            _instanceAsBister = Bister.Instance.Serialize(_instance);
            _instanceAsBinaryPack = BinaryConverter.Serialize(_instance);
            _instanceAsMessagePack = MessagePackSerializer.Serialize(_instance);
        }

        [Benchmark]
        public byte[] MessagePack_Serialize()
        {
            return MessagePackSerializer.Serialize(_instance);
        }

        [Benchmark]
        public SimpleClass MessagePack_Deserialize()
        {
            return MessagePackSerializer.Deserialize<SimpleClass>(_instanceAsMessagePack);
        }

        [Benchmark]
        public byte[] BinaryPack_Serialize()
        {
            return BinaryConverter.Serialize(_instance);
        }

        [Benchmark]
        public SimpleClass BinaryPack_Deserialize()
        {
            return BinaryConverter.Deserialize<SimpleClass>(_instanceAsBinaryPack);
        }

        [Benchmark]
        public string SystemTextJsonSerialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(_instance, _jsonSettings);
        }

        [Benchmark]
        public SimpleClass? SystemTextJsonDeserialize()
        {
            return System.Text.Json.JsonSerializer.Deserialize<SimpleClass>(_instanceAsJson, _jsonSettings);
        }

        [Benchmark]
        public byte[] BisterSerialize()
        {
            return Bister.Instance.Serialize(_instance);
        }

        [Benchmark]
        public SimpleClass? BisterDeserialize()
        {
            return Bister.Instance.Deserialize<SimpleClass>(_instanceAsBister);
        }

        static void Main(string[] args)
        {
            var _instance = new SimpleClass()
            {
                ArrayPropInt = Enumerable.Range(0, 100).Select(i => i).ToArray(),
                ArrayPropString = Enumerable.Range(0, 100).Select(i => $"Number{i}").ToArray(),
                DicStr2Float = Enumerable.Range(0, 100).ToDictionary(i => i.ToString(), i => (float)i),
                ListOfStrings = Enumerable.Range(0, 100).Select(i => i.ToString()).ToList(),
            };

            do
            {
                var blob = Bister.Instance.Serialize(_instance);
                _ = Bister.Instance.Deserialize<SimpleClass>(blob);
            } while (true);

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
;