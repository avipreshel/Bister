using Microsoft.CodeAnalysis.CSharp;
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
using System.IO;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;
using System.Runtime.InteropServices;

namespace BisterLib
{
    public class Bister : IBister
    {
        static Lazy<IBister> _lazy = new Lazy<IBister>(()=> new Bister());
        public static IBister Instance => _lazy.Value;

        Dictionary<Type, IBisterGenerated> _typeToSerializer = new Dictionary<Type, IBisterGenerated>();

        /// <summary>
        /// If not empty, will dump the latest serialization class into the path, e.g. c:\temp\gen.cs
        /// </summary>
        public string DebugPath { get; set; } = string.Empty;

        private Bister() { }

        Type ReadTypeFromBlob(byte[] buffer)
        {
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    string typeName = reader.ReadString();
                    return Type.GetType(typeName);
                }
            }
        }

        #region IBister
        public T Deserialize<T>(byte[] buffer)
        {
            if (typeof(T) == typeof(object))
            {
                Type realType = ReadTypeFromBlob(buffer);
                IBisterGenerated serializer = GenerateSerializer(realType);
                return (T)serializer.DeserializeObj(buffer);
            }
            else
            {
                IBisterGenerated<T> serializer = GenerateSerializer<T>();
                return serializer.Deserialize(buffer);
            }
        }

        public byte[] Serialize<T>(T obj)
        {
            var serializer = GenerateSerializer<T>();
            return serializer.Serialize(obj);
        }

        #endregion

        string ReadServerTemplateFromResource()
        {
            string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            string clientProxyResourceName = resourceNames.First(res => res.Contains("BisterTemplate"));
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(clientProxyResourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static string GetFriendlyGenericTypeName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.FullName;
            }

            string typeName = type.Name;
            int backtickIndex = typeName.IndexOf('`');
            if (backtickIndex > 0)
            {
                typeName = typeName.Remove(backtickIndex);
            }

            Type[] genericArguments = type.GetGenericArguments();
            string[] genericArgumentNames = new string[genericArguments.Length];
            for (int i = 0; i < genericArguments.Length; i++)
            {
                genericArgumentNames[i] = GetFriendlyGenericTypeName(genericArguments[i]);
            }

            return $"{typeName}<{string.Join(", ", genericArgumentNames)}>";
        }

        IBisterGenerated GenerateSerializer(Type objType)
        {
            if (_typeToSerializer.TryGetValue(objType, out IBisterGenerated serTmp))
            {
                return serTmp;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(ReadServerTemplateFromResource());

            string friendlyTypeName = GetFriendlyGenericTypeName(objType);
            sb.Replace("___TYPE_NAME___", friendlyTypeName);

            string serializerTypeName = $"Serializer_{friendlyTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_')}";

            sb.Replace("___SERIALIZER_TYPE_NAME___", serializerTypeName);

            // If the user is trying to generate serializer for generic type, no need to have any "using"
            if (objType.Namespace.StartsWith("System"))
            {
                sb.Replace("<<<USINGS>>>", string.Empty);
            }
            else
            {
                sb.Replace("<<<USINGS>>>", $"using {objType.Namespace};");
            }

            GenerateSerializerBody(sb, objType);

            GenerateDeSerializerBody(sb, objType);

            if (!string.IsNullOrEmpty(DebugPath))
            {
                File.WriteAllText(@"c:\temp\serialize.cs", sb.ToString());
            }

            Type genType = GenerateType(sb.ToString(), serializerTypeName, new List<Type>() { objType, typeof(IBisterGenerated) });

            var serializer = (IBisterGenerated)Activator.CreateInstance(genType);
            _typeToSerializer[objType] = serializer;
            return serializer;
        }

        IBisterGenerated<T> GenerateSerializer<T>()
        {
            return (IBisterGenerated<T>)GenerateSerializer(typeof(T));
        }

        static void DeSerializeGeneric(string indentation,StringBuilder sb, Type objType,string instanceName)
        {
            string usefulVariableName = instanceName.Replace(".", "");
            if (objType.GetGenericTypeDefinition() == typeof(List<>))
            {
                DeSerializeGenericList(indentation, sb, objType, instanceName, usefulVariableName);
            }
            else if (objType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                DeSerializeGenericDictionary(indentation, sb, objType, instanceName,usefulVariableName);
            }
            else
            {
                throw new NotImplementedException($"Unable to create serializer code for {instanceName} since type {objType} is unsupported");
            }
        }
        private static void DeSerializeGenericDictionary(string indentation, StringBuilder sb, Type objType, string instanceName, string usefulVariableName)
        {
            Type keyType = objType.GenericTypeArguments[0];
            Type valType = objType.GenericTypeArguments[1];
            if (IsPrimitive(keyType) && IsPrimitive(valType))
            {
                sb.AppendLine(indentation + $"int count{usefulVariableName} = br.ReadInt32();");
                sb.AppendLine(indentation + $"for (int i =0; i<count{usefulVariableName};i++)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tvar key = br.{BinaryReaderMethod(Type.GetTypeCode(keyType))};");
                sb.AppendLine(indentation + $"\tvar val = br.{BinaryReaderMethod(Type.GetTypeCode(valType))};");
                sb.AppendLine(indentation + $"\t{instanceName}.Add(key,val);");
                sb.AppendLine(indentation + "}");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void DeSerializeGenericList(string indentation, StringBuilder sb, Type objType, string instanceName, string usefulVariableName)
        {
            Type valType = objType.GenericTypeArguments[0];
            sb.AppendLine(indentation + $"int count{usefulVariableName} = br.ReadInt32();");
            sb.AppendLine(indentation + $"{instanceName}.Capacity = count{usefulVariableName};");
            if (valType == typeof(Enum))
            {


                sb.AppendLine(indentation + $"Type enumType{usefulVariableName} = count{usefulVariableName} > 0 ? Type.GetType(br.ReadString()) : null;");
                sb.AppendLine(indentation + $"TypeCode enumTypeCode{usefulVariableName} = count{usefulVariableName} > 0 ? Type.GetTypeCode(enumType{usefulVariableName}) : TypeCode.Empty;");
                sb.AppendLine(indentation + $"for (int i = 0; i < count{usefulVariableName}; i++)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tif (enumTypeCode{usefulVariableName} == TypeCode.Int32) {instanceName}.Add(({valType.Name})Enum.ToObject(enumType{usefulVariableName},br.ReadInt32()));");
                sb.AppendLine(indentation + $"\telse if (enumTypeCode{usefulVariableName} == TypeCode.Int64) {instanceName}.Add(({valType.Name})Enum.ToObject(enumType{usefulVariableName},br.ReadInt64()));");
                sb.AppendLine(indentation + $"\telse if (enumTypeCode{usefulVariableName} == TypeCode.Int16) {instanceName}.Add(({valType.Name})Enum.ToObject(enumType{usefulVariableName},br.ReadInt16()));");
                sb.AppendLine(indentation + "}");
            }
            else
            {
                Type enumPrimitiveType = valType.IsEnum ? valType.GetEnumUnderlyingType() : valType;
                sb.AppendLine(indentation + $"for (int i = 0; i < count{usefulVariableName}; i++)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\t{instanceName}.Add(({valType})br.{BinaryReaderMethod(Type.GetTypeCode(enumPrimitiveType))});");
                sb.AppendLine(indentation + "}");
            }
        }

        private static void GenerateDeSerializerBody(StringBuilder sb, Type objType)
        {
            string indentation = "\t\t\t\t";
            StringBuilder sbSerializer = new StringBuilder();
            if (objType.IsGenericType)
            {
                DeSerializeGeneric(indentation, sbSerializer, objType, "instance");
            }
            else
            {
                var props = objType.GetProperties();
                foreach (var prop in props)
                {
                    var propAccessors = prop.GetAccessors();
                    // only deal with properties that have get/set accessors
                    if (propAccessors.Length > 0 && propAccessors.All(acc => acc.IsPublic))
                    {
                        if (prop.PropertyType.IsGenericType)
                        {
                            DeSerializeGeneric(indentation, sbSerializer, prop.PropertyType, $"instance.{prop.Name}");
                        }
                        else
                        {
                            PropertyDeserializer(indentation, prop, sbSerializer);
                        }
                            
                    }
                }
            }

            sb.Replace("___DESERIALIZER_BODY___", sbSerializer.ToString());
        }

        private static void PropertyDeserializer(string indentation, PropertyInfo prop, StringBuilder sb)
        {
            sb.AppendLine(indentation + $"// Deserializing {prop.DeclaringType.Name}.{prop.Name}");

            // we avoid c# 7 syntax since we want it to be porable for dotnet framework 4.8
            if (IsPrimitive(prop.PropertyType))
            {
                sb.AppendLine(indentation + $"instance.{prop.Name} = br.{BinaryReaderMethod(Type.GetTypeCode(prop.PropertyType))};");
            }
            else if (prop.PropertyType == typeof(Enum)) // Unspecific enum
            {
                // Since we can't know the actual type, we need to assume the worst (64bit enum)
                DeserializerEnum(indentation, $"instance.{prop.Name}", sb);
            }
            else if (prop.PropertyType.IsEnum)
            {
                Type underlytingType = Enum.GetUnderlyingType(prop.PropertyType);
                string propType = prop.PropertyType.FullName.Replace("+", ".");
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
            if (valType.IsEnum || valType == typeof(Enum))
            {
                sb.AppendLine(indentation + $"if (count{prop.Name} > 0)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tType enumType{prop.Name} = count{prop.Name} > 0? Type.GetType(br.ReadString()) : null;");
                sb.AppendLine(indentation + $"\tType enumNativeType{prop.Name} = enumType{prop.Name}.GetEnumUnderlyingType();");
                sb.AppendLine(indentation + $"\tfor (int i = 0; i < count{prop.Name}; i++)");
                sb.AppendLine(indentation + "\t{");
                sb.AppendLine(indentation + $"\t\tif (enumNativeType{prop.Name} == typeof(byte)) instance.{prop.Name}.Add(({valType.Name})Enum.ToObject(enumType{prop.Name}!,br.ReadByte()));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType{prop.Name} == typeof(sbyte)) instance.{prop.Name}.Add(({valType.Name})Enum.ToObject(enumType{prop.Name}!,br.ReadSByte()));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType{prop.Name} == typeof(short)) instance.{prop.Name}.Add(({valType.Name})Enum.ToObject(enumType{prop.Name}!,br.ReadInt16()));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType{prop.Name} == typeof(ushort)) instance.{prop.Name}.Add(({valType.Name})Enum.ToObject(enumType{prop.Name}!,br.ReadUInt16()));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType{prop.Name} == typeof(int)) instance.{prop.Name}.Add(({valType.Name})Enum.ToObject(enumType{prop.Name}!,br.ReadInt32()));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType{prop.Name} == typeof(uint)) instance.{prop.Name}.Add(({valType.Name})Enum.ToObject(enumType{prop.Name}!,br.ReadUInt32()));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType{prop.Name} == typeof(long)) instance.{prop.Name}.Add(({valType.Name})Enum.ToObject(enumType{prop.Name}!,br.ReadInt64()));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType{prop.Name} == typeof(ulong)) instance.{prop.Name}.Add(({valType.Name})Enum.ToObject(enumType{prop.Name}!,br.ReadUInt64()));");
                sb.AppendLine(indentation + $"\t\telse throw new Exception(\"Failed to deserialize {prop.Name}. Unknown enum underlying type\");");
                sb.AppendLine(indentation + "\t}");
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

        private static void DeserializerEnum(string indentation, string instanceName, StringBuilder sb)
        {
            sb.AppendLine(indentation + $"string enumTypeStr = br.ReadString();");
            sb.AppendLine(indentation + $"Type enumType =Type.GetType(enumTypeStr);");
            sb.AppendLine(indentation + $"{instanceName} = (Enum)Activator.CreateInstance(enumType)!;");
            sb.AppendLine(indentation + $"var enumInstanceType = enumType.GetEnumUnderlyingType();");
            sb.AppendLine(indentation + $"object enumVal;");
            sb.AppendLine(indentation + $"if (enumInstanceType == typeof(byte)) enumVal = br.ReadByte();");
            sb.AppendLine(indentation + $"else if (enumInstanceType == typeof(short)) enumVal = br.ReadInt16();");
            sb.AppendLine(indentation + $"else if (enumInstanceType == typeof(int)) enumVal = br.ReadInt32();");
            sb.AppendLine(indentation + $"else if (enumInstanceType == typeof(long)) enumVal = br.ReadInt64();");
            sb.AppendLine(indentation + $"else throw new Exception(\"Failed to read {instanceName} \");");
            sb.AppendLine(indentation + $"enumType.GetField(\"value__\")!.SetValue({instanceName}, enumVal);");
        }

        static string BinaryReaderMethod(TypeCode typeCode)
        {
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
                case TypeCode.UInt16:
                    return "ReadUInt16()";
                case TypeCode.Int32:
                    return "ReadInt32()";
                case TypeCode.UInt32:
                    return "ReadUInt32()";
                case TypeCode.Int64:
                    return "ReadInt64()";
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

            var props = objType.GetProperties();

            string indentation = "\t\t\t";
            
            StringBuilder sbSizeOfObject = new StringBuilder();
            sbSizeOfObject.Append($"0");
            foreach (var prop in props)
            {
                if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(Decimal))
                {
                    sbSizeOfObject.Append($"+{Marshal.SizeOf(prop.PropertyType)}");
                }
                else if (prop.PropertyType.IsEnum)
                {
                    sbSizeOfObject.Append($"+8");
                }
                else if (prop.PropertyType == typeof(string))
                {
                    sbSizeOfObject.Append($"+instance.{prop.Name}.Length");
                }
                else if (prop.PropertyType.IsArray)
                {
                    sbSizeOfObject.Append($"+{Marshal.SizeOf(prop.PropertyType.GetElementType())}");
                }
                else if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                {
                    int size = prop.PropertyType.GenericTypeArguments.Sum(t => t.IsPrimitive? Marshal.SizeOf(t) : 8);
                    sbSizeOfObject.Append($"+{size}");
                }
                else
                {
                    throw new NotImplementedException();
                }
                // Estimate 8 bytes per property...simple huristic

            }

            sb.Replace("___BINARY_WRITER_BUFFER_SIZE___", $"(int)Math.Ceiling(1.5f * ({sbSizeOfObject.ToString()}))");

            StringBuilder sbSerializerBody = new StringBuilder();
            if (objType.IsGenericType)
            {
                SerializeGeneric("instance",indentation + '\t', sbSerializerBody, objType);
            }
            else
            {
                foreach (var prop in props)
                {

                    var propAccessors = prop.GetAccessors();
                    // only deal with properties that have get/set accessors
                    if (propAccessors.Length > 0 && propAccessors.All(acc => acc.IsPublic))
                    {
                        if (prop.PropertyType.IsGenericType)
                        {
                            SerializeGeneric($"instance.{prop.Name}", indentation + "\t", sbSerializerBody, prop.PropertyType);
                        }
                        else
                        {
                            PropertySerializer(indentation + "\t", prop, sbSerializerBody);
                        }

                    }
                }
            }
            

            sb.Replace("___SERIALIZER_BODY___", sbSerializerBody.ToString());
        }

        static void SerializeGeneric(string instanceName,string indentation,StringBuilder sb, Type objType)
        {
            sb.AppendLine(indentation+$"// Serializing {instanceName}");
            if (objType.GetGenericTypeDefinition() == typeof(List<>))
            {
                SerializeGenericList(instanceName, indentation, sb, objType);
            }
            else if (objType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                SerializeGenericDictionary(instanceName, indentation, sb, objType);
            }
            else
            {
                throw new NotImplementedException($"Unable to create serializer code for {instanceName} since type {objType} is unsupported");
            }
        }

        static bool IsPrimitive(Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal);
        }

        private static void SerializeGenericDictionary(string instanceName, string indentation, StringBuilder sb, Type objType)
        {
            Type keyType = objType.GenericTypeArguments[0];
            Type valType = objType.GenericTypeArguments[1];
            if (IsPrimitive(keyType) && IsPrimitive(valType))
            {
                sb.AppendLine(indentation + $"bw.Write((int){instanceName}.Count);");
                sb.AppendLine(indentation + $"foreach (var item in {instanceName})");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(item.Key);");
                sb.AppendLine(indentation + $"\tbw.Write(item.Value);");
                sb.AppendLine(indentation + "}");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void SerializeGenericList(string instanceName, string indentation, StringBuilder sb, Type objType)
        {
            Type valType = objType.GenericTypeArguments[0];
            if (valType == typeof(Enum))
            {
                // Write the enum type
                sb.AppendLine(indentation + $"bw.Write((int){instanceName}.Count);");
                sb.AppendLine(indentation + $"if ({instanceName}.Count > 0)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tType enumType = {instanceName}[0].GetType();");
                sb.AppendLine(indentation + $"\tbw.Write(enumType.AssemblyQualifiedName);");
                sb.AppendLine(indentation + $"\tType enumNativeType = enumType.GetEnumUnderlyingType();");
                sb.AppendLine(indentation + $"\tvar enumValueField = enumType.GetField(\"value__\");");
                sb.AppendLine(indentation + $"\tforeach (var item in {instanceName})");
                sb.AppendLine(indentation + "\t{");
                sb.AppendLine(indentation + $"\t\tobject numericVal = enumValueField.GetValue(item);");
                sb.AppendLine(indentation + $"\t\tif (enumNativeType == typeof(byte)) bw.Write((byte)numericVal);");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(sbyte)) bw.Write((sbyte)numericVal);");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(short)) bw.Write((short)numericVal);");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(int)) bw.Write((int)numericVal);");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(long)) bw.Write((long)numericVal);");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(ushort)) bw.Write((short)numericVal);");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(uint)) bw.Write((int)numericVal);");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(ulong)) bw.Write((long)numericVal);");
                sb.AppendLine(indentation + $"\t\telse throw new Exception(\"Failed to serialize {instanceName}\");");
                sb.AppendLine(indentation + "\t}");
                sb.AppendLine(indentation + "}");
            }
            else
            {
                sb.AppendLine(indentation + $"bw.Write((int){instanceName}.Count);");
                sb.AppendLine(indentation + $"foreach (var item in {instanceName})");
                sb.AppendLine(indentation + "{");
                SerializeGenericItem("item", indentation + '\t', valType, sb);
                sb.AppendLine(indentation + "}");
            }
        }

        static void SerializeGenericItem(string instanceName,string indentation, Type objType, StringBuilder sb)
        {
            if (IsPrimitive(objType))
            {
                sb.AppendLine(indentation + $"bw.Write(({objType}){instanceName});");
            }
            else if (objType.IsEnum && objType != typeof(Enum))
            {
                Type enumNativeType = objType.GetEnumUnderlyingType();
                SerializeGenericItem(instanceName, indentation, enumNativeType, sb);
            }
            else
            {
                throw new NotImplementedException($"Unable to create serialize code for {instanceName} of type {objType}");
            }
        }

        static void SerializerEnum(string indentation, string instanceName, StringBuilder sb)
        {
            // Since we can't know the actual Enum during run time, so we need to do magic
            sb.AppendLine(indentation + $"Type enumType = {instanceName}.GetType();");
            sb.AppendLine(indentation + $"Type enumNativeType = {instanceName}.GetType()!.GetEnumUnderlyingType()!;");
            sb.AppendLine(indentation + $"object numericVal = enumType!.GetField(\"value__\")!.GetValue({instanceName})!;");
            sb.AppendLine(indentation + $"bw.Write(enumType!.AssemblyQualifiedName!);");
            sb.AppendLine(indentation + $"if (enumNativeType == typeof(byte)) bw.Write((byte)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(sbyte)) bw.Write((sbyte)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(short)) bw.Write((short)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(int)) bw.Write((int)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(long)) bw.Write((long)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(ushort)) bw.Write((short)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(uint)) bw.Write((int)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(ulong)) bw.Write((long)numericVal);");
            sb.AppendLine(indentation + $"else throw new Exception(\"Failed to serialize {instanceName}\");");
        }

        static void PropertySerializer(string indentation,PropertyInfo prop, StringBuilder sb)
        {
            sb.AppendLine(indentation + $"// Serializing {prop.DeclaringType.Name}.{prop.Name}");
            if (IsPrimitive(prop.PropertyType))
            {
                sb.AppendLine(indentation + $"bw.Write(instance.{prop.Name});");
            }
            else if (prop.PropertyType == typeof(Enum)) // Unspecific enum
            {
                SerializerEnum(indentation, $"instance.{prop.Name}", sb);
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
            if (keyType.IsEnum || keyType == typeof(Enum))
            {
                sb.AppendLine(indentation + $"if (instance.{prop.Name}.Count>0)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(instance.{prop.Name}[0].GetType().AssemblyQualifiedName);");
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
                    MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51").Location),
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(Path.GetDirectoryName(typeof(object).Assembly.Location) + @"\System.Runtime.dll"),
                    MetadataReference.CreateFromFile(Path.GetDirectoryName(typeof(object).Assembly.Location) + @"\System.Collections.dll"),
                    MetadataReference.CreateFromFile(typeof(Enum).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ICollection<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Collections.Hashtable).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Collection<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IEnumerable<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IDictionary<,>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(MemoryStream).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(BinaryReader).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(BinaryWriter).Assembly.Location)
                    //Marshal
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
    }
}
