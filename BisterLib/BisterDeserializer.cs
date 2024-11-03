using System;
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
                sb.AppendLine(indentation + $"\t\t{instanceName} = GeneratedHelper.DeserializeListOfEnums(br);");
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
            else
            {
                throw new NotImplementedException($"No support for deserialization of {Bister.GetFriendlyGenericTypeName(objType)}");
            }
            sb.AppendLine(indentation + "\t}");
            sb.AppendLine(indentation + "}");
        }
       
        public static void DeserializerSystemEnum(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            sb.AppendLine(indentation + $"{instanceName} = GeneratedHelper.DeserializeSystemEnum(br);");
        }

        public static void DeserializeArrayList(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            
            sb.AppendLine(indentation + $"{instanceName} = GeneratedHelper.DeserializeArrayList(br);");
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
                sb.AppendLine(indentation + $"{instanceName} = GeneratedHelper.DeserializeSystemStringArray(br);");
            }
            else if (Bister.IsPrimitive(arrayItemType))
            {
                TypeCode arrayItemTypeCode = Type.GetTypeCode(arrayItemType);
                sb.AppendLine(indentation + $"if (br.ReadBoolean() == false)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\t{instanceName} = new {arrayItemType.FullName}[br.ReadInt32()];");
                switch (arrayItemTypeCode)
                { 
                    case TypeCode.Int32:
                        sb.AppendLine(indentation + $"\tfor (int i = 0; i< {instanceName}.Length;i++) {instanceName}[i] = br.ReadInt32();");
                        break;
                    case TypeCode.UInt32:
                        sb.AppendLine(indentation + $"\tfor (int i = 0; i< {instanceName}.Length;i++) {instanceName}[i] = br.ReadUInt32();");
                        break;
                    case TypeCode.Int16:
                        sb.AppendLine(indentation + $"\tfor (int i = 0; i< {instanceName}.Length;i++) {instanceName}[i] = br.ReadInt16();");
                        break;
                    case TypeCode.UInt16:
                        sb.AppendLine(indentation + $"\tfor (int i = 0; i< {instanceName}.Length;i++) {instanceName}[i] = br.ReadUInt16();");
                        break;
                    case TypeCode.Int64:
                        sb.AppendLine(indentation + $"\tfor (int i = 0; i< {instanceName}.Length;i++) {instanceName}[i] = br.ReadInt64();");
                        break;
                    case TypeCode.UInt64:
                        sb.AppendLine(indentation + $"\tfor (int i = 0; i< {instanceName}.Length;i++) {instanceName}[i] = br.ReadUInt64();");
                        break;
                    case TypeCode.Boolean:
                        sb.AppendLine(indentation + $"\tfor (int i = 0; i< {instanceName}.Length;i++) {instanceName}[i] = br.ReadBoolean();");
                        break;
                    case TypeCode.Byte:
                        sb.AppendLine(indentation + $"\tfor (int i = 0; i< {instanceName}.Length;i++) {instanceName}[i] = br.ReadByte();");
                        break;
                    case TypeCode.SByte:
                        sb.AppendLine(indentation + $"\tfor (int i = 0; i< {instanceName}.Length;i++) {instanceName}[i] = br.ReadSByte();");
                        break;
                    case TypeCode.Decimal:
                        sb.AppendLine(indentation + $"\tfor (int i = 0; i< {instanceName}.Length;i++) {instanceName}[i] = br.ReadDecimal();");
                        break;
                    default:
                        throw new NotImplementedException($"Unsupported array type {arrayItemType.FullName}");
                }

                sb.AppendLine(indentation + "}");
            }
            else if (arrayItemType == typeof(Enum))
            {
                sb.AppendLine(indentation + $"{instanceName} = GeneratedHelper.DeserializeSystemArrayOfEnums(br);");
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
            else
            {
                throw new NotImplementedException($"No support for serializing {arrayItemType.FullName}[]");
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
            sb.AppendLine(indentation + $"if (br.ReadBoolean() == true)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\t{instanceName} = null;");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\tstring objTypeName = br.ReadString();");
            sb.AppendLine(indentation + $"\tType objType = Type.GetType(objTypeName);");
            sb.AppendLine(indentation + $"\tif (GeneratedHelper.IsPrimitive(objType))");
            sb.AppendLine(indentation + "\t{");
            sb.AppendLine(indentation + $"\t\t{instanceName} = GeneratedHelper.DeserializePrimitive(br,objType);");
            sb.AppendLine(indentation + "\t}");
            sb.AppendLine(indentation + "\telse if (objType == typeof(object))");
            sb.AppendLine(indentation + "\t{");
            sb.AppendLine(indentation + $"\t\t{instanceName} = new object();");
            sb.AppendLine(indentation + "\t}");
            sb.AppendLine(indentation + "\telse");
            sb.AppendLine(indentation + "\t{");
            sb.AppendLine(indentation + $"\t\t{instanceName} = Bister.Instance.Deserialize(br,objType);");
            sb.AppendLine(indentation + "\t}");
            sb.AppendLine(indentation + "}");
        }

        public static void DeserializeAnyType(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
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
            else if (objType.IsEnum) // Strongly-defined enum
            {
                DeserializerUserEnum(sb, indentation, instanceName, objType);
            }
            else if (Bister.IsPrimitive(objType)) // Native type
            {
                DeserializePrimitive(sb, indentation, instanceName, objType);
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

        private static void DeserializeTimeSpan(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"{instanceName} = TimeSpan.FromTicks(br.ReadInt64());");
        }

        private static void DeserializeDateTime(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"{instanceName} = GeneratedHelper.DeserializeDateTime(br);");
        }

        private static void DeserializeString(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"{instanceName} = GeneratedHelper.DeserializeString(br);");
        }

        private static void DeserializeException(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation);
            if (!instanceName.Contains("."))
            {
                sb.AppendLine(indentation + $"{objType.FullName} {instanceName};");
            }
            sb.AppendLine(indentation + $"if (br.ReadBoolean() == true)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\t{instanceName} = null;");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tstring exTypeName = br.ReadString();");
            sb.AppendLine(indentation + "\tType exType = Type.GetType(exTypeName);");
            sb.AppendLine(indentation + "\tstring source = br.ReadString();");
            sb.AppendLine(indentation + "\tstring message = br.ReadString();");
            sb.AppendLine(indentation + "\tstring stackTrack = br.ReadString();");
            sb.AppendLine(indentation + "\tint errorCode = br.ReadInt32();");
            sb.AppendLine(indentation + $"\t{instanceName} = ({objType.FullName})GeneratedHelper.CreateException(exType,source,message,errorCode,stackTrack);");
            sb.AppendLine(indentation + "}");
            // CreateException(Type exType,string source, string message,int errorCode,string stackTrace)
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
            sb.AppendLine(indentation + $"{instanceName} = new {objType}();");
            var props = Bister.GetRelevantProperties(objType);
            foreach (var prop in props)
            {
                sb.AppendLine(indentation + $"// For each property...{prop.Name}");
                DeserializeAnyType(sb, indentation, $"{instanceName}.{prop.Name}", prop.PropertyType);
            }
        }

        private static void DeserializeGenericStruct(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            string friendlyTypename = Bister.GetFriendlyGenericTypeName(objType);
            sb.AppendLine(indentation + $"{friendlyTypename} {instanceName} = new {friendlyTypename}();");
            var props = Bister.GetRelevantProperties(objType);
            foreach (var prop in props)
            {
                sb.AppendLine(indentation + $"// For each property...{prop.Name}");
                DeserializeAnyType(sb, indentation, $"{instanceName}.{prop.Name}", prop.PropertyType);
            }
        }

        private static void DeserializePrimitive(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            if (objType.IsPrimitive || objType == typeof(string) || objType == typeof(decimal))
            {
                sb.AppendLine(indentation + $"{instanceName} = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(objType))};");
            }
            else if (objType == typeof(DateTime))
            {
                
                sb.AppendLine(indentation + $"{instanceName} = DateTime.FromFileTime(br.ReadInt64());");
            }
            else if (objType == typeof(TimeSpan))
            {
                
                sb.AppendLine(indentation + $"{instanceName} = TimeSpan.FromTicks(br.ReadInt64());");
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
