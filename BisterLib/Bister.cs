﻿using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.VisualBasic;

namespace EasyCFLib
{
    public interface IBister
    {
        string DebugPath { get; set; }
        /// <summary>
        /// Generated an optimized binary serializer on the fly or take a cached instance of one
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IBisterGenerated<T> GetSerializer<T>() where T : new ();
    }

    /// <summary>
    /// This interface is needed just so we can cache all the generated serializers
    /// </summary>
    public interface IBisterGenerated
    { }

    /// <summary>
    /// This interface will be implemented by the generated serializers
    /// </summary>
    public interface IBisterGenerated<T> : IBisterGenerated where T : new()
    {
        byte[] Serialize(T obj);

        T Deserialize(byte[] buffer);

        byte[] Serialize(T obj,MemoryStream ms,BinaryWriter bw);

        T Deserialize(BinaryReader br);
    }

    public class Bister : IBister
    {
        static Lazy<IBister> _lazy = new Lazy<IBister>(()=> new Bister());
        public static IBister Instance => _lazy.Value;

        Dictionary<Type, IBisterGenerated> _typeToSerializer = new();

        /// <summary>
        /// If not empty, will dump the latest serialization class into the path, e.g. c:\temp\gen.cs
        /// </summary>
        public string DebugPath { get; set; } = string.Empty;

        private Bister() { }

        public T Deserialize<T>(byte[] buffer) where T : new()
        {
            IBisterGenerated<T> serializer = GenerateSerializer<T>();
            return serializer.Deserialize(buffer);
        }

        public byte[] Serialize<T>(T obj) where T : new()
        {
            IBisterGenerated<T> serializer = GenerateSerializer<T>();
            return serializer.Serialize(obj);
        }

        string ReadServerTemplateFromResource()
        {
            string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            string clientProxyResourceName = resourceNames.First(res => res.Contains("BisterTemplate"));
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(clientProxyResourceName)!)
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        IBisterGenerated<T> GenerateSerializer<T>() where T : new()
        {
            IBisterGenerated<T> serializer;
            Type objType = typeof(T);
            if (_typeToSerializer.TryGetValue(objType, out IBisterGenerated? serTmp))
            {
                return (IBisterGenerated <T>)serTmp;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(ReadServerTemplateFromResource());

            
            string serializerTypeName = $"Serializer{objType.Name}";

            sb.Replace("<<<SERIALIZER_TYPE_NAME>>>", serializerTypeName);
            sb.Replace("<<<TYPE_NAME>>>", objType.Name);
            sb.Replace("<<<USINGS>>>", $"using {objType.Namespace};");

            GenerateSerializerBody(sb, objType);

            GenerateDeSerializerBody(sb, objType);

            if (!string.IsNullOrEmpty(DebugPath))
            {
                File.WriteAllText(@"c:\temp\serialize.cs", sb.ToString());
            }

            Type genType = GenerateType(sb.ToString(), serializerTypeName, new() { objType, typeof(IBisterGenerated) });

            serializer = (IBisterGenerated<T>)Activator.CreateInstance(genType)!;
            _typeToSerializer[objType] = serializer;
            return serializer;
        }

        private static void GenerateDeSerializerBody(StringBuilder sb, Type objType)
        {
            StringBuilder sbSerializer = new StringBuilder();
            var props = objType.GetProperties();
            string indentation = "\t\t\t\t";
            foreach (var prop in props)
            {
                var propAccessors = prop.GetAccessors();
                // only deal with properties that have get/set accessors
                if (propAccessors.Length > 0 && propAccessors.All(acc => acc.IsPublic))
                {
                    PropertyDeserializer(indentation, prop, sbSerializer);
                }
            }

            sb.Replace("<<<DESERIALIZER_BODY>>>", sbSerializer.ToString());
        }

        private static void PropertyDeserializer(string indentation, PropertyInfo prop, StringBuilder sb)
        {
            sb.AppendLine(indentation + $"// Deserializing {prop.DeclaringType!.Name}.{prop.Name}");

            // we avoid c# 7 syntax since we want it to be porable for dotnet framework 4.8
            if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string))
            {
                sb.AppendLine(indentation + $"instance.{prop.Name} = br.{BinaryReaderMethod(Type.GetTypeCode(prop.PropertyType))};");
            }
            else if (prop.PropertyType == typeof(Enum)) // Unspecific enum
            {
                // Since we can't know the actual type, we need to assume the worst (64bit enum)
                PropertyDeserializerEnum(indentation, prop, sb);
            }
            else if (prop.PropertyType.IsEnum)
            {
                Type underlytingType = Enum.GetUnderlyingType(prop.PropertyType);
                string propType = prop.PropertyType.FullName!.Replace("+", ".");
                sb.AppendLine(indentation + $"instance.{prop.Name} = ({propType})br.{BinaryReaderMethod(Type.GetTypeCode(underlytingType))};");
            }
            else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                PropertyDeserializerList(indentation, prop, sb);
            }
            else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                PropertyDeserializerDictionary(indentation, prop, sb);
            }
            else if (prop.PropertyType.IsClass)
            {
                sb.AppendLine(indentation + $"instance.{prop.Name} = Bister.Instance.GetSerializer<{prop.PropertyType}>().Deserialize(br);");
            }
            else
            {
                throw new Exception($"Property {prop.Name} type {prop.PropertyType} is not supported");
            }
        }

        private static void PropertyDeserializerDictionary(string indentation, PropertyInfo prop, StringBuilder sb)
        {
            sb.AppendLine(indentation + $"int count{prop.Name} = br.ReadInt32();");
            Type keyType = prop.PropertyType.GenericTypeArguments[0];
            Type valType = prop.PropertyType.GenericTypeArguments[1];
            sb.AppendLine(indentation + $"instance.{prop.Name} = new Dictionary<{keyType.Name},{valType.Name}>(count{prop.Name});");
            sb.AppendLine(indentation + $"for (int i =0; i< count{prop.Name}; i++)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\tinstance.{prop.Name}[br.{BinaryReaderMethod(Type.GetTypeCode(keyType))}] = br.{BinaryReaderMethod(Type.GetTypeCode(valType))};");
            sb.AppendLine(indentation + "}");
        }

        private static void PropertyDeserializerList(string indentation, PropertyInfo prop, StringBuilder sb)
        {
            Type valType = prop.PropertyType.GenericTypeArguments[0];
            sb.AppendLine(indentation + $"int count{prop.Name} = br.ReadInt32();");
            sb.AppendLine(indentation + $"instance.{prop.Name} = new List<{valType.Name}>(count{prop.Name});");
            if (valType == typeof(Enum))
            {
                sb.AppendLine(indentation + $"Type? enumType{prop.Name} = count{prop.Name} > 0? Type.GetType(br.ReadString()) : null;");
                sb.AppendLine(indentation + $"for (int i = 0; i < count{prop.Name}; i++)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tinstance.{prop.Name}.Add((Enum)Enum.ToObject(enumType{prop.Name}!,br.ReadInt64()));");
                sb.AppendLine(indentation + "}");
            }
            else
            {
                sb.AppendLine(indentation + $"for (int i = 0; i < count{prop.Name}; i++)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tinstance.{prop.Name}.Add(br.{BinaryReaderMethod(Type.GetTypeCode(valType))});");
                sb.AppendLine(indentation + "}");
            }
            
            
        }

        private static void PropertyDeserializerEnum(string indentation, PropertyInfo prop, StringBuilder sb)
        {
            sb.AppendLine(indentation + $"string enumTypeStr = br.ReadString();");
            sb.AppendLine(indentation + $"Type enumType =Type.GetType(enumTypeStr);");
            sb.AppendLine(indentation + $"instance.{prop.Name} = (Enum)Activator.CreateInstance(enumType)!;");
            sb.AppendLine(indentation + $"var enumInstanceType = enumType.GetEnumUnderlyingType();");
            sb.AppendLine(indentation + $"object enumVal;");
            sb.AppendLine(indentation + $"if (enumInstanceType == typeof(byte)) enumVal = br.ReadByte();");
            sb.AppendLine(indentation + $"else if (enumInstanceType == typeof(short)) enumVal = br.ReadInt16();");
            sb.AppendLine(indentation + $"else if (enumInstanceType == typeof(int)) enumVal = br.ReadInt32();");
            sb.AppendLine(indentation + $"else if (enumInstanceType == typeof(long)) enumVal = br.ReadInt64();");
            sb.AppendLine(indentation + $"else throw new Exception(\"Failed to read {prop.DeclaringType!.Name}.{prop.PropertyType.Name} \");");
            sb.AppendLine(indentation + $"enumType.GetField(\"value__\")!.SetValue(instance.{prop.Name}, enumVal);");
        }

        static string BinaryReaderMethod(TypeCode typeCode)
        {
            BinaryReader br = new BinaryReader(new MemoryStream());
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return "ReadBoolean()";
                case TypeCode.Char:
                    return "ReadChar()";
                case TypeCode.Byte:
                    return "ReadByte()";
                case TypeCode.SByte:
                    return "ReadSByte()";
                case TypeCode.Int16:
                    return "ReadInt16()";
                case TypeCode.Int32:
                    return "ReadInt32()";
                case TypeCode.Int64:
                    return "ReadInt64()";
                case TypeCode.UInt16:
                    return "ReadUInt16()";
                case TypeCode.UInt32:
                    return "ReadUInt32()";
                case TypeCode.UInt64:
                    return "ReadUInt64()";
                case TypeCode.Single:
                    return "ReadSingle()";
                case TypeCode.Double:
                    return "ReadDouble()";
                case TypeCode.Decimal:
                    return "ReadDecimal()";
                case TypeCode.String:
                    return "ReadString()";
                case TypeCode.DateTime:
                    return "ReadInt64()";
                default:
                    throw new NotImplementedException();
            }
        }

        private static void GenerateSerializerBody(StringBuilder sb, Type objType)
        {
            StringBuilder sbSerializer = new StringBuilder();
            var props = objType.GetProperties();

            string indentation = "\t\t\t";

            // Estimate 8 bytes per property...simple huristic
            StringBuilder sbBinaryWriter = new StringBuilder();
            sbBinaryWriter.AppendLine(indentation+$"using var ms = new MemoryStream({props.Length * 8});");
            sbBinaryWriter.AppendLine(indentation+"using var bw = new BinaryWriter(ms);");
            sb.Replace("<<<CREATE_BINARY_WRITER>>>", sbBinaryWriter.ToString());

            foreach (var prop in props)
            {
                var propAccessors = prop.GetAccessors();
                // only deal with properties that have get/set accessors
                if (propAccessors.Length > 0 && propAccessors.All(acc => acc.IsPublic))
                {
                    PropertySerializer(indentation + "\t\t",prop, sbSerializer);
                    
                }
            }

            sb.Replace("<<<SERIALIZER_BODY>>>", sbSerializer.ToString());
        }

        static void PropertySerializerEnum(string indentation, PropertyInfo prop, StringBuilder sb)
        {
            // Since we can't know the actual Enum during run time, so we need to do magic
            sb.AppendLine(indentation + $"Type enumType = instance.{prop.Name}.GetType();");
            sb.AppendLine(indentation + $"Type enumNativeType = instance.{prop.Name}.GetType()!.GetEnumUnderlyingType()!;");
            sb.AppendLine(indentation + $"object numericVal = enumType!.GetField(\"value__\")!.GetValue(instance.{prop.Name})!;");
            sb.AppendLine(indentation + $"bw.Write(enumType!.AssemblyQualifiedName!);");
            sb.AppendLine(indentation + $"if (enumNativeType == typeof(byte)) bw.Write((byte)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(sbyte)) bw.Write((sbyte)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(short)) bw.Write((short)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(int)) bw.Write((int)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(long)) bw.Write((long)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(ushort)) bw.Write((short)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(uint)) bw.Write((int)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(ulong)) bw.Write((long)numericVal);");
            sb.AppendLine(indentation + $"else throw new Exception(\"Failed to serialize {prop.DeclaringType!.Name}.{prop.Name}\");");
        }

        static void PropertySerializer(string indentation,PropertyInfo prop, StringBuilder sb)
        {
            sb.AppendLine(indentation + $"// Serializing {prop.DeclaringType!.Name}.{prop.Name}");
            if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string))
            {
                sb.AppendLine(indentation + $"bw.Write(instance.{prop.Name});");
            }
            else if (prop.PropertyType == typeof(Enum)) // Unspecific enum
            {
                PropertySerializerEnum(indentation, prop, sb);
            }
            else if (prop.PropertyType.IsEnum) // Specific enum
            {
                Type underlytingType = Enum.GetUnderlyingType(prop.PropertyType);
                sb.AppendLine(indentation + $"bw.Write(({underlytingType.FullName})instance.{prop.Name});");
            }
            else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                PropertySerializerList(indentation, prop, sb);
            }
            else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                PropertySerializerDictionary(indentation, prop, sb);
            }
            else if (prop.PropertyType.IsClass)
            {
                sb.AppendLine(indentation + $"Bister.Instance.GetSerializer<{prop.PropertyType}>().Serialize(instance.{prop.Name},ms,bw);");
            }
            else
            {
                throw new Exception($"Property {prop.Name} type {prop.PropertyType} is not supported");
            }
        }

        private static void PropertySerializerDictionary(string indentation, PropertyInfo prop, StringBuilder sb)
        {
            Type keyType = prop.PropertyType.GenericTypeArguments[0];
            Type valType = prop.PropertyType.GenericTypeArguments[1];
            sb.AppendLine(indentation + $"bw.Write((int)instance.{prop.Name}.Count);");
            sb.AppendLine(indentation + $"foreach (var item in instance.{prop.Name})");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\tbw.Write(item.Key);");
            sb.AppendLine(indentation + $"\tbw.Write(item.Value);");
            sb.AppendLine(indentation + "}");
        }

        private static void PropertySerializerList(string indentation, PropertyInfo prop, StringBuilder sb)
        {
            sb.AppendLine(indentation + $"bw.Write((int)instance.{prop.Name}.Count);");

            Type keyType = prop.PropertyType.GenericTypeArguments[0];
            if (keyType == typeof(Enum))
            {
                sb.AppendLine(indentation + $"if (instance.{prop.Name}.Count>0)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(instance.{prop.Name}[0].GetType().AssemblyQualifiedName!);");
                sb.AppendLine(indentation + "}");
                sb.AppendLine(indentation + $"foreach (var item in instance.{prop.Name})");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(Convert.ToInt64(item));");
                sb.AppendLine(indentation + "}");
            }
            else
            {
                sb.AppendLine(indentation + $"foreach (var item in instance.{prop.Name})");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(item);");
                sb.AppendLine(indentation + "}");
            }
            
        }


        /// <summary>
        /// Takes in text, and output a type.
        /// </summary>
        /// <param name="theCode"></param>
        /// <param name="expectedTypeName"></param>
        /// <param name="domainDependencies">If the type has any dependencies which are NOT part of the dotnet runtime, need to mention it here</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static Type GenerateType(string theCode, string expectedTypeName,List<Type> domainDependencies)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(theCode);
            
            if (domainDependencies==null)
            {
                domainDependencies = new List<Type>();
            }

            var dependencyFiles = domainDependencies.Select(t => MetadataReference.CreateFromFile(t.Assembly.Location)).ToList();

            // Compile the syntax tree into an assembly
            CSharpCompilation compilation = CSharpCompilation.Create(
                $"{expectedTypeName}.asm",
                new[] { syntaxTree },
                new[] {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(Path.GetDirectoryName(typeof(object).Assembly.Location) + @"\System.Runtime.dll"),
                    MetadataReference.CreateFromFile(Path.GetDirectoryName(typeof(object).Assembly.Location) + @"\System.Collections.dll"),
                    MetadataReference.CreateFromFile(typeof(Enum).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ICollection<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Collections.Hashtable).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Collection).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IEnumerable<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IDictionary<,>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(MemoryStream).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(BinaryReader).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(BinaryWriter).Assembly.Location)
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(dependencyFiles);

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    throw new Exception(result.Diagnostics.First().ToString());
                }

                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());

                // Instantiate the class using reflection

                Type myClassType = assembly.GetTypes().First(t => t.Name.Contains(expectedTypeName));

                return myClassType;
            }
        }

        public IBisterGenerated<T> GetSerializer<T>() where T : new()
        {
            return GenerateSerializer<T>();
        }
    }
}