using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace BisterLib
{
    public  static class BisterDeserializer
    {
        public static void DeserializeObject(string indentation, StringBuilderVerbose sb, string instanceName,Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            if (Bister.IsPrimitive(objType))
            {
                sb.AppendLine(indentation + $"{instanceName} = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(objType))};");
            }
        }


        public static void DeSerializeGeneric(string indentation, StringBuilderVerbose sb, Type objType, string instanceName)
        {
            string usefulVariableName = instanceName.Replace(".", "");
            if (objType.GetGenericTypeDefinition() == typeof(List<>))
            {
                DeSerializeGenericList(indentation, sb, objType, instanceName, usefulVariableName);
            }
            else if (objType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                DeSerializeGenericDictionary(indentation, sb, objType, instanceName, usefulVariableName);
            }
            else if (objType.Namespace.StartsWith("System.Collections.Generic"))
            {
                throw new NotImplementedException("Unsupported System.Collections.Generic type");
            }
            else
            {
                var props = Bister.GetRelevantProperties(objType);
                foreach (var prop in props)
                {
                    BisterDeserializer.DeserializeObject(indentation, sb, $"{instanceName}.{prop.Name}", prop.PropertyType);
                }
            }
        }

        public static void DeSerializeGenericDictionary(string indentation, StringBuilderVerbose sb, Type objType, string instanceName, string usefulVariableName)
        {
            Type keyType = objType.GenericTypeArguments[0];
            Type valType = objType.GenericTypeArguments[1];
            if (Bister.IsPrimitive(keyType) && Bister.IsPrimitive(valType))
            {
                sb.AppendLine(indentation + $"int count{usefulVariableName} = br.ReadInt32();");
                sb.AppendLine(indentation + $"for (int i =0; i<count{usefulVariableName};i++)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tvar key = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(keyType))};");
                sb.AppendLine(indentation + $"\tvar val = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(valType))};");
                sb.AppendLine(indentation + $"\t{instanceName}.Add(key,val);");
                sb.AppendLine(indentation + "}");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static void DeSerializeGenericList(string indentation, StringBuilderVerbose sb, Type objType, string instanceName, string usefulVariableName)
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
                sb.AppendLine(indentation + $"\t{instanceName}.Add(({valType})br.{Bister.BinaryReaderMethod(Type.GetTypeCode(enumPrimitiveType))});");
                sb.AppendLine(indentation + "}");
            }
        }

        public static void PropertyDeserializer(string indentation, PropertyInfo prop, StringBuilderVerbose sb)
        {
            Bister.PrintMethodName(sb, indentation, prop.PropertyType);

            sb.AppendLine(indentation + $"// Deserializing {prop.DeclaringType.Name}.{prop.Name}");

            // we avoid c# 7 syntax since we want it to be porable for dotnet framework 4.8
            if (Bister.IsPrimitive(prop.PropertyType))
            {
                sb.AppendLine(indentation + $"instance.{prop.Name} = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(prop.PropertyType))};");
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
                sb.AppendLine(indentation + $"instance.{prop.Name} = ({propType})br.{Bister.BinaryReaderMethod(Type.GetTypeCode(underlytingType))};");
            }
            else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                PropertyDeserializerList(indentation, prop, sb);
            }
            else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                PropertyDeserializerDictionary(indentation, prop, sb);
            }
            else if (prop.PropertyType.IsArray)
            {
                BisterDeserializer.DeserializeArray(indentation, sb, $"instance.{prop.Name}", prop.PropertyType);
            }
            else if (prop.PropertyType == typeof(ArrayList))
            {
                BisterDeserializer.DeserializeArrayList(indentation, sb, $"instance.{prop.Name}");
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

        public static void PropertyDeserializerDictionary(string indentation, PropertyInfo prop, StringBuilderVerbose sb)
        {
            sb.AppendLine(indentation + $"int count{prop.Name} = br.ReadInt32();");
            Type keyType = prop.PropertyType.GenericTypeArguments[0];
            Type valType = prop.PropertyType.GenericTypeArguments[1];
            sb.AppendLine(indentation + $"instance.{prop.Name} = new Dictionary<{keyType.Name},{valType.Name}>(count{prop.Name});");
            sb.AppendLine(indentation + $"for (int i =0; i< count{prop.Name}; i++)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\tinstance.{prop.Name}[br.{Bister.BinaryReaderMethod(Type.GetTypeCode(keyType))}] = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(valType))};");
            sb.AppendLine(indentation + "}");
        }

        public static void PropertyDeserializerList(string indentation, PropertyInfo prop, StringBuilderVerbose sb)
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
                sb.AppendLine(indentation + $"\tinstance.{prop.Name}.Add(br.{Bister.BinaryReaderMethod(Type.GetTypeCode(valType))});");
                sb.AppendLine(indentation + "}");
            }


        }

        public static void DeserializerEnum(string indentation, string instanceName, StringBuilderVerbose sb)
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

        public static void DeserializeArrayList(string indentation, StringBuilderVerbose sb, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);

            sb.AppendLine(indentation + $"int capacity = br.ReadInt32();");
            sb.AppendLine(indentation + $"{instanceName} = new ArrayList(capacity);");
            sb.AppendLine(indentation + $"for (int i = 0; i< capacity;i++)");
            sb.AppendLine(indentation + "{");
            BisterDeserializer.DeserializeArrayObjectItem(indentation + "\t", sb, instanceName);
            sb.AppendLine(indentation + "}");
        }

        public static void DeserializeArray(string indentation, StringBuilderVerbose sb, string instanceName, Type arrayType)
        {
            Bister.PrintMethodName(sb, indentation, arrayType);

            var itemType = arrayType.GetElementType();
            if (arrayType.IsArray && itemType.IsArray)
            {
                throw new NotImplementedException("No support yet for multi-dim arrays");
            }
            else if (arrayType.IsArray) // 1D array
            {
                if (itemType == typeof(Enum))
                {
                    
                    sb.AppendLine(indentation + "{");
                    sb.AppendLine(indentation + $"\tint count = br.ReadInt32();");
                    sb.AppendLine(indentation + $"\t{instanceName} = new Enum[count];");
                    sb.AppendLine(indentation + $"\tfor (int i = 0;i < count;i++)");
                    sb.AppendLine(indentation + "\t{");
                    sb.AppendLine(indentation + $"\tstring itemTypeStr = br.ReadString();");
                    sb.AppendLine(indentation + $"\tType itemType = Type.GetType(itemTypeStr);");
                    sb.AppendLine(indentation + $"\tType itemTypeNative = Enum.GetUnderlyingType(itemType);");
                    sb.AppendLine(indentation + $"\t\tif (itemTypeNative == typeof(int)) {instanceName}[i] = (Enum)Enum.ToObject(itemType, br.ReadInt32());");
                    sb.AppendLine(indentation + $"\t\telse if (itemTypeNative == typeof(uint)) {instanceName}[i] = (Enum)Enum.ToObject(itemType, br.ReadUInt32());");
                    sb.AppendLine(indentation + $"\t\telse if (itemTypeNative == typeof(short)) {instanceName}[i] = (Enum)Enum.ToObject(itemType, br.ReadInt16());");
                    sb.AppendLine(indentation + $"\t\telse if (itemTypeNative == typeof(ushort)) {instanceName}[i] = (Enum)Enum.ToObject(itemType, br.ReadUInt16());");
                    sb.AppendLine(indentation + $"\t\telse if (itemTypeNative == typeof(byte)) {instanceName}[i] = (Enum)Enum.ToObject(itemType, br.ReadByte());");
                    sb.AppendLine(indentation + $"\t\telse if (itemTypeNative == typeof(sbyte)) {instanceName}[i] = (Enum)Enum.ToObject(itemType, br.ReadSByte());");
                    sb.AppendLine(indentation + $"\t\telse if (itemTypeNative == typeof(long)) {instanceName}[i] = (Enum)Enum.ToObject(itemType, br.ReadInt64());");
                    sb.AppendLine(indentation + $"\t\telse if (itemTypeNative == typeof(ulong)) {instanceName}[i] = (Enum)Enum.ToObject(itemType, br.ReadUInt64());");
                    sb.AppendLine(indentation + $"\t\telse throw new NotImplementedException();");
                    sb.AppendLine(indentation + "\t}");
                    sb.AppendLine(indentation + "}");
                }
                else if (itemType.IsEnum)
                {
                    sb.AppendLine(indentation + "{");
                    sb.AppendLine(indentation + $"\tint count = br.ReadInt32();");
                    sb.AppendLine(indentation + $"\t{instanceName} = new {itemType}[count];");
                    sb.AppendLine(indentation + $"\tfor (int i = 0;i < count;i++)");
                    sb.AppendLine(indentation + "\t{");
                    sb.AppendLine(indentation + $"\t\t{instanceName}[i] = ({itemType})br.{Bister.BinaryReaderMethod(Type.GetTypeCode(itemType))};");
                    sb.AppendLine(indentation + "\t}");
                    sb.AppendLine(indentation + "}");
                }
                else if (itemType.IsPrimitive || itemType == typeof(string) || itemType == typeof(decimal))
                {
                    sb.AppendLine(indentation + "{");
                    sb.AppendLine(indentation + $"\tint count = br.ReadInt32();");
                    sb.AppendLine(indentation + $"\t{instanceName} = new {itemType}[count];");
                    sb.AppendLine(indentation + $"\tfor (int i = 0;i < count;i++)");
                    sb.AppendLine(indentation + "\t{");
                    sb.AppendLine(indentation + $"\t\t{instanceName}[i] = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(itemType))};");
                    sb.AppendLine(indentation + "\t}");
                    sb.AppendLine(indentation + "}");
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static void DeserializeArrayObjectItem(string indentation, StringBuilderVerbose sb, string instanceName)
        {
            sb.AppendLine(indentation + $"string itemTypeStr = br.ReadString();");
            sb.AppendLine(indentation + $"Type itemType = Type.GetType(itemTypeStr);");
            sb.AppendLine(indentation + $"if (itemType == typeof(string)) {instanceName}.Add(br.ReadString());");
            sb.AppendLine(indentation + $"else if (itemType == typeof(decimal)) {instanceName}.Add(br.ReadDecimal());");
            sb.AppendLine(indentation + $"else if (itemType == typeof(bool)) {instanceName}.Add(br.ReadBoolean());");
            sb.AppendLine(indentation + $"else if (itemType == typeof(short)) {instanceName}.Add(br.ReadInt16());");
            sb.AppendLine(indentation + $"else if (itemType == typeof(ushort)) {instanceName}.Add(br.ReadUInt16());");
            sb.AppendLine(indentation + $"else if (itemType == typeof(int)) {instanceName}.Add(br.ReadInt32());");
            sb.AppendLine(indentation + $"else if (itemType == typeof(uint)) {instanceName}.Add(br.ReadUInt32());");
            sb.AppendLine(indentation + $"else if (itemType == typeof(long)) {instanceName}.Add(br.ReadInt64());");
            sb.AppendLine(indentation + $"else if (itemType == typeof(ulong)) {instanceName}.Add(br.ReadUInt64());");
            sb.AppendLine(indentation + $"else if (itemType == typeof(char)) {instanceName}.Add(br.ReadChar());");
            sb.AppendLine(indentation + $"else if (itemType == typeof(byte)) {instanceName}.Add(br.ReadByte());");
            sb.AppendLine(indentation + $"else if (itemType == typeof(sbyte)) {instanceName}.Add(br.ReadSByte());");
            sb.AppendLine(indentation + $"else throw new NotImplementedException();");
        }
    }
}
