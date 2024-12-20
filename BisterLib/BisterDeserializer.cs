﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;

namespace BisterLib
{
    public static class BisterDeserializer
    {
        public static void DeserializeGenericDictionary(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            string niceTypeName = Bister.GetFriendlyGenericTypeName(objType);
            string usefulVariableName = Bister.GetUsefulName(instanceName);
            Type keyType = objType.GenericTypeArguments[0];
            Type valType = objType.GenericTypeArguments[1];

            sb.AppendLine(indentation + $"if (br.ReadBoolean()) {instanceName} = null;");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");
            indentation += "\t";
            sb.AppendLine(indentation + $"{instanceName} = new {niceTypeName}();");
            sb.AppendLine(indentation + $"int count{usefulVariableName} = br.ReadInt32();");
            sb.AppendLine(indentation + $"for (int i =0; i<count{usefulVariableName};i++)");
            sb.AppendLine(indentation + "{");
            indentation += "\t";

            if ((keyType == typeof(string) || Bister.IsPrimitive(keyType)))
            {
                sb.AppendLine(indentation + $"// Key is primitive");
                sb.AppendLine(indentation + $"var key = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(keyType))};");
            }
            else
            {
                string niceKeyTypeName = Bister.GetFriendlyGenericTypeName(keyType);
                sb.AppendLine(indentation + $"{niceKeyTypeName} key;");
                DeserializeAnyType(sb, indentation, "key", keyType);
            }

            if ((valType == typeof(string) || Bister.IsPrimitive(valType)))
            {
                sb.AppendLine(indentation + $"// Val is primitive");
                sb.AppendLine(indentation + $"var val = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(valType))};");
            }
            else
            {
                sb.AppendLine(indentation + $"// Val is non-primitive");
                string niceValTypeName = Bister.GetFriendlyGenericTypeName(valType);
                sb.AppendLine(indentation + $"{niceValTypeName} val;");
                DeserializeAnyType(sb, indentation, "val", valType);
            }
            sb.AppendLine(indentation + $"{instanceName}.Add(key,val);");

            indentation = indentation.Substring(0, indentation.Length - 1);
            sb.AppendLine(indentation + "}");
            indentation = indentation.Substring(0, indentation.Length - 1);
            sb.AppendLine(indentation + "}");

        }

        public static void DeserializeGenericList(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);

            sb.AppendLine(indentation + "if (br.ReadBoolean() == true)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\t{instanceName} = null;");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");
            Type valType = objType.GenericTypeArguments[0];
            sb.AppendLine(indentation + $"\t{instanceName} = new List<{valType.FullName}>(br.ReadInt32());");
            sb.AppendLine(indentation + $"\tfor (int i = 0; i < {instanceName}.Capacity; i++)");
            sb.AppendLine(indentation + "\t{");
            if (valType == typeof(Enum))
            {
                sb.AppendLine(indentation + $"\t\t{instanceName} = StaticHelper.DeserializeListOfEnums(br);");
            }
            else if (valType == typeof(object))
            {
                throw new NotImplementedException();
            }
            else if (valType == typeof(string))
            {
                sb.AppendLine(indentation + $"\t\t{instanceName}.Add(br.ReadString());");
            }
            else if (valType.IsEnum)
            {
                Type enumPrimitiveType = valType.GetEnumUnderlyingType();

                sb.AppendLine(indentation + $"\t\t{instanceName}.Add(({valType})br.{Bister.BinaryReaderMethod(Type.GetTypeCode(enumPrimitiveType))});");
            }
            else if (valType.IsPrimitive)
            {
                sb.AppendLine(indentation + $"\t\t{instanceName}.Add(br.{Bister.BinaryReaderMethod(Type.GetTypeCode(valType))});");
            }
            else if (valType == typeof(DateTime))
            {
                
                sb.AppendLine(indentation + $"\t\t{instanceName}.Add(DateTime.FromBinary(br.ReadInt64()));");
            }
            else
            {
                throw new NotImplementedException($"No support for deserialization of {Bister.GetFriendlyGenericTypeName(objType)}");
            }
            sb.AppendLine(indentation + "\t}");
            sb.AppendLine(indentation + "}");
        }
       
        public static void DeserializerSystemEnum(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeSystemEnum(br);");
        }

        public static void DeserializeArrayList(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            
            sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeArrayList(br);");
        }

        private static void DeserializeSystemArray(StringBuilderVerbose sb, string indentation, string instanceName, Type arrayType)
        {
            Bister.PrintMethodName(sb, indentation, arrayType);
            
            int dims = arrayType.GetArrayRank();

            if (dims != 1)
            {
                throw new NotImplementedException("Only supporting 1D arrays, not support for 2D arrays yet");
            }

            Type arrayItemType = arrayType.GetElementType();
            if (arrayItemType == typeof(string))
            {
                sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeSystemStringArray(br);");
            }
            else if (Bister.IsPrimitive(arrayItemType))
            {
                TypeCode arrayItemTypeCode = Type.GetTypeCode(arrayItemType);
                sb.AppendLine(indentation + $"if (br.ReadBoolean() == false)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + "\tint length = br.ReadInt32();");
                sb.AppendLine(indentation + "\tbyte[] blob = br.ReadBytes(length);");
                sb.AppendLine(indentation + $"\t{instanceName} = MemoryMarshal.Cast<byte, {arrayItemType.FullName}>(blob).ToArray();");
                sb.AppendLine(indentation + "}");
            }
            else if (arrayItemType == typeof(Enum))
            {
                sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeSystemArrayOfEnums(br);");
            }
            else if (arrayItemType.IsEnum)
            {
                sb.AppendLine(indentation + "if (br.ReadBoolean() == true)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\t{instanceName} = null;");
                sb.AppendLine(indentation + "}");
                sb.AppendLine(indentation + "else");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\t{instanceName} = new {arrayItemType}[br.ReadInt32()];");
                sb.AppendLine(indentation + $"\tfor (int i = 0;i < {instanceName}.Length;i++)");
                sb.AppendLine(indentation + "\t{");
                sb.AppendLine(indentation + $"\t\t{instanceName}[i] = ({arrayItemType})br.{Bister.BinaryReaderMethod(Type.GetTypeCode(arrayItemType))};");
                sb.AppendLine(indentation + "\t}");
                sb.AppendLine(indentation + "}");
            }
            else if (arrayItemType == typeof(DateTime))
            {
                sb.AppendLine(indentation + "if (br.ReadBoolean() == true)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\t{instanceName} = null;");
                sb.AppendLine(indentation + "}");
                sb.AppendLine(indentation + "else");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\t{instanceName} = new {arrayItemType}[br.ReadInt32()];");
                sb.AppendLine(indentation + $"\tfor (int i = 0;i < {instanceName}.Length;i++)");
                sb.AppendLine(indentation + "\t{");
                sb.AppendLine(indentation + $"\t\t{instanceName}[i] = DateTime.FromBinary(br.ReadInt64());");
                sb.AppendLine(indentation + "\t}");
                sb.AppendLine(indentation + "}");
            }
            else if (arrayItemType == typeof(TimeSpan))
            {
                sb.AppendLine(indentation + "if (br.ReadBoolean() == true)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\t{instanceName} = null;");
                sb.AppendLine(indentation + "}");
                sb.AppendLine(indentation + "else");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\t{instanceName} = new {arrayItemType}[br.ReadInt32()];");
                sb.AppendLine(indentation + $"\tfor (int i = 0;i < {instanceName}.Length;i++)");
                sb.AppendLine(indentation + "\t{");
                sb.AppendLine(indentation + $"\t\t{instanceName}[i] = TimeSpan.FromTicks(br.ReadInt64());");
                sb.AppendLine(indentation + "\t}");
                sb.AppendLine(indentation + "}");
            }
            else if (arrayItemType == typeof(object))
            {
                sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeSystemArrayOfObjects(br);");
            }
            else
            {
                throw new NotImplementedException($"No support for deserializing {arrayItemType.FullName}[]");
            }
        }

        public static void DeserializeArrayObjectItem(string indentation, StringBuilderVerbose sb, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"if (br.ReadBoolean())"); // is null?
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\t{instanceName}.Add(null);");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\tstring itemTypeStr = br.ReadString();");
            sb.AppendLine(indentation + $"\tType itemType = Type.GetType(itemTypeStr);");
            sb.AppendLine(indentation + $"\tif (itemType == typeof(string)) {instanceName}.Add(br.ReadString());");
            sb.AppendLine(indentation + $"\telse if (itemType == typeof(decimal)) {instanceName}.Add(br.ReadDecimal());");
            sb.AppendLine(indentation + $"\telse if (itemType == typeof(bool)) {instanceName}.Add(br.ReadBoolean());");
            sb.AppendLine(indentation + $"\telse if (itemType == typeof(short)) {instanceName}.Add(br.ReadInt16());");
            sb.AppendLine(indentation + $"\telse if (itemType == typeof(ushort)) {instanceName}.Add(br.ReadUInt16());");
            sb.AppendLine(indentation + $"\telse if (itemType == typeof(int)) {instanceName}.Add(br.ReadInt32());");
            sb.AppendLine(indentation + $"\telse if (itemType == typeof(uint)) {instanceName}.Add(br.ReadUInt32());");
            sb.AppendLine(indentation + $"\telse if (itemType == typeof(long)) {instanceName}.Add(br.ReadInt64());");
            sb.AppendLine(indentation + $"\telse if (itemType == typeof(ulong)) {instanceName}.Add(br.ReadUInt64());");
            sb.AppendLine(indentation + $"\telse if (itemType == typeof(char)) {instanceName}.Add(br.ReadChar());");
            sb.AppendLine(indentation + $"\telse if (itemType == typeof(byte)) {instanceName}.Add(br.ReadByte());");
            sb.AppendLine(indentation + $"\telse if (itemType == typeof(sbyte)) {instanceName}.Add(br.ReadSByte());");
            sb.AppendLine(indentation + $"\telse if (itemType == typeof(object)) {instanceName}.Add(new object());");
            sb.AppendLine(indentation + $"\telse throw new NotImplementedException();");
            sb.AppendLine(indentation + "}");
        }

        public static void DeserializeSystemObject(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeSystemObject(br);");
        }

        public static void DeserializeAnyType(StringBuilderVerbose sb, string indentation, string instanceName, Type objType,bool isStructField = false)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            if (objType == typeof(Enum)) // Unknown enum
            {
                DeserializerSystemEnum(sb, indentation, instanceName);
            }
            else if (objType == typeof(object))
            {
                DeserializeSystemObject(sb, indentation, instanceName);
            }
            else if (objType == typeof(DateTime))
            {
                DeserializeDateTime(sb, indentation, instanceName);
            }
            else if (objType == typeof(TimeSpan))
            {
                DeserializeTimeSpan(sb, indentation, instanceName);
            }
            else if (objType == typeof(Type))
            {
                DeserializeSystemType(sb, indentation, instanceName, objType);
            }
            else if (objType.IsEnum) // Strongly-defined enum
            {
                DeserializerUserEnum(sb, indentation, instanceName, objType);
            }
            else if (Bister.IsPrimitive(objType)) // Native type
            {
                DeserializePrimitive(sb, indentation, instanceName, objType, isStructField);
            }
            else if (objType == typeof(ArrayList))
            {
                DeserializeArrayList(sb,indentation,instanceName);
            }
            else if (objType.IsArray) // such as int[5] or string[,]
            {
                DeserializeSystemArray(sb, indentation, instanceName, objType);
            }
            else if (objType.IsValueType) // a struct
            {
                DeserializeStruct(sb, indentation, instanceName, objType);
            }
            else if (typeof(Exception).IsAssignableFrom(objType)) // is it some kind of Exception?
            {
                DeserializeException(sb, indentation, instanceName, objType);
            }
            else if (objType == typeof(string))
            {
                DeserializeString(sb, indentation, instanceName);
            }
            else if (objType.IsClass)
            {
                DeserializeClass(sb, indentation, instanceName, objType);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void DeserializeSystemType(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeSystemType(br);");
        }

        private static void DeserializeTimeSpan(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"{instanceName} = TimeSpan.FromTicks(br.ReadInt64());");
        }

        private static void DeserializeDateTime(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"{instanceName} = DateTime.FromBinary(br.ReadInt64());");
        }

        private static void DeserializeString(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeString(br);");
        }

        private static void DeserializeException(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"{instanceName} = ({objType.FullName})StaticHelper.DeserializeException(br);");
        }

        private static void DeserializeStruct(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            if (objType.IsGenericType)
            {
                DeserializeGenericStruct(sb, indentation, instanceName, objType);
            }
            else
            {
                DeserializeNonGenericStruct(sb, indentation, instanceName, objType);
            }


        }

        private static void DeserializeNonGenericStruct(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            sb.AppendLine(indentation + $"{instanceName} = new {objType}()");
            sb.AppendLine(indentation + "{");
            var props = Bister.GetRelevantProperties(objType);
            foreach (var prop in props)
            {
                sb.AppendLine(indentation + $"\t// For each property of {prop.Name}");
                DeserializeAnyType(sb, indentation + "\t", $"{prop.Name}", prop.PropertyType, isStructField : true);
            }
            sb.AppendLine(indentation + "};");
        }

        private static void DeserializeGenericStruct(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            string friendlyTypename = Bister.GetFriendlyGenericTypeName(objType);
            sb.AppendLine(indentation + $"{instanceName} = new {friendlyTypename}()");
            sb.AppendLine(indentation + "{");
            var props = Bister.GetRelevantProperties(objType);
            foreach (var prop in props)
            {
                sb.AppendLine(indentation + $"\t// For each property...{prop.Name}");
                DeserializeAnyType(sb, indentation + "\t", $"{prop.Name}", prop.PropertyType, isStructField: true);
            }
            sb.AppendLine(indentation + "};");
        }

        private static void DeserializePrimitive(StringBuilderVerbose sb, string indentation, string instanceName, Type objType, bool isStructField = false)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            char lineEnd = isStructField ? ',' : ';';
            if (objType.IsPrimitive || objType == typeof(string) || objType == typeof(decimal))
            {
                sb.AppendLine(indentation + $"{instanceName} = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(objType))}{lineEnd}");
            }
            else if (objType == typeof(DateTime))
            {
                
                sb.AppendLine(indentation + $"{instanceName} = DateTime.FromFileTime(br.ReadInt64()){lineEnd}");
            }
            else if (objType == typeof(TimeSpan))
            {
                
                sb.AppendLine(indentation + $"{instanceName} = TimeSpan.FromTicks(br.ReadInt64()){lineEnd}");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void DeserializerUserEnum(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            Type underlytingType = Enum.GetUnderlyingType(objType);
            string propType = objType.FullName.Replace("+", ".");
            sb.AppendLine(indentation + $"{instanceName} = ({propType})br.{Bister.BinaryReaderMethod(Type.GetTypeCode(underlytingType))};");
        }

        public static void DeserializeClass(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            string usefulName = Bister.GetUsefulName(instanceName);
            string friendlyTypeName = Bister.GetFriendlyGenericTypeName(objType);
            
            if (objType == typeof(ArrayList))
            {
                DeserializeArrayList(sb, indentation, instanceName);
            }
            if (objType.IsGenericType)
            {
                DeserializeGenericClass(sb, indentation , instanceName, objType);
            }
            else
            {
                DeserializeNonGenericClass(sb, indentation , instanceName, objType);
            }
        }

        private static void DeserializeNonGenericClass(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            sb.AppendLine(indentation + $"if (br.ReadBoolean() == true)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\t{instanceName} = null;");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\t{instanceName} = new {objType.FullName}();");
            var props = Bister.GetRelevantProperties(objType);
            foreach (var prop in props)
            {
                sb.AppendLine(indentation + $"\t// For each property...{prop.Name}");
                DeserializeAnyType(sb, indentation + "\t", $"{instanceName}.{prop.Name}", prop.PropertyType);
            }
            sb.AppendLine(indentation + "}");
        }

        private static void DeserializeGenericClass(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            if (objType.GetGenericTypeDefinition() == typeof(List<>))
            {
                DeserializeGenericList(sb, indentation, instanceName, objType);
            }
            else if (objType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                DeserializeGenericDictionary(sb, indentation, instanceName, objType);
            }
            else if (objType.Namespace.StartsWith("System.Collections.Generic"))
            {
                throw new NotImplementedException("Unsupported System.Collections.Generic type");
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
