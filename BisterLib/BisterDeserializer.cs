using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;

namespace BisterLib
{
    public  static class BisterDeserializer
    {
        public static void DeserializeGenericDictionary(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);

            string usefulVariableName = Bister.GetUsefulName(instanceName);
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
            else if (Bister.IsPrimitive(keyType)) // key is primitive, value is not
            {
                sb.AppendLine(indentation + $"int count{usefulVariableName} = br.ReadInt32();");
                sb.AppendLine(indentation + $"for (int i =0; i<count{usefulVariableName};i++)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tvar key = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(keyType))};");
                sb.AppendLine(indentation + $"\t// *****************************************************");
                DeserializeAnyType(sb, indentation + "\t", "val", valType);
                sb.AppendLine(indentation + $"\t// *****************************************************");
                sb.AppendLine(indentation + $"\t{instanceName}.Add(key,val);");
                sb.AppendLine(indentation + "}");
            }
            else if (Bister.IsPrimitive(valType)) // key is non-primitive, value is primitive
            {
                throw new NotImplementedException();
            }
            else // Both non-primitive
            {
                throw new NotImplementedException();
            }
        }

        public static void DeserializeGenericList(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);

            string usefulVariableName = Bister.GetUsefulName(instanceName);
            Type valType = objType.GenericTypeArguments[0];
            sb.AppendLine(indentation + $"int count{usefulVariableName} = br.ReadInt32();");
            sb.AppendLine(indentation + $"{instanceName}.Capacity = count{usefulVariableName};");
            if (valType == typeof(Enum))
            {
                sb.AppendLine(indentation + $"for (int i = 0; i < count{usefulVariableName}; i++)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tstring itemTypeName = br.ReadString();");
                sb.AppendLine(indentation + $"\tType enumType{usefulVariableName} =  Type.GetType(itemTypeName);");
                sb.AppendLine(indentation + $"\tTypeCode enumTypeCode{usefulVariableName} =  Type.GetTypeCode(enumType{usefulVariableName});");
                sb.AppendLine(indentation + $"\tif (enumTypeCode{usefulVariableName} == TypeCode.Int32) {instanceName}.Add(({valType.Name})Enum.ToObject(enumType{usefulVariableName},br.ReadInt32()));");
                sb.AppendLine(indentation + $"\telse if (enumTypeCode{usefulVariableName} == TypeCode.UInt32) {instanceName}.Add(({valType.Name})Enum.ToObject(enumType{usefulVariableName},br.ReadUInt32()));");
                sb.AppendLine(indentation + $"\telse if (enumTypeCode{usefulVariableName} == TypeCode.Int16) {instanceName}.Add(({valType.Name})Enum.ToObject(enumType{usefulVariableName},br.ReadInt16()));");
                sb.AppendLine(indentation + $"\telse if (enumTypeCode{usefulVariableName} == TypeCode.UInt16) {instanceName}.Add(({valType.Name})Enum.ToObject(enumType{usefulVariableName},br.ReadUInt16()));");
                sb.AppendLine(indentation + $"\telse if (enumTypeCode{usefulVariableName} == TypeCode.Int64) {instanceName}.Add(({valType.Name})Enum.ToObject(enumType{usefulVariableName},br.ReadInt64()));");
                sb.AppendLine(indentation + $"\telse if (enumTypeCode{usefulVariableName} == TypeCode.UInt64) {instanceName}.Add(({valType.Name})Enum.ToObject(enumType{usefulVariableName},br.ReadUInt64()));");
                sb.AppendLine(indentation + $"\telse throw new NotImplementedException();");
                sb.AppendLine(indentation + "}");
            }
            else if (valType == typeof(object))
            {
                throw new NotImplementedException();
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
       
        public static void DeserializerSystemEnum(StringBuilderVerbose sb, string indentation, string instanceName)
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

        public static void DeserializeArrayList(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            if (!instanceName.Contains("."))
            {
                sb.AppendLine(indentation + $"ArrayList {instanceName};");
            }
            sb.AppendLine(indentation + $"if (br.ReadBoolean())");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\t{instanceName} = null;");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\tint count = br.ReadInt32();");
            sb.AppendLine(indentation + $"\t{instanceName} = new ArrayList(count);");
            sb.AppendLine(indentation + $"\tDeserializeArrayList({instanceName},count,br);");
            sb.AppendLine(indentation + "}");
        }

        private static void DeserializeSystemArray(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            int dims = objType.GetArrayRank();

            Type itemType = objType.GetElementType();
            if (dims == 1) // 1D array
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
                throw new NotImplementedException("Only supporting 1D arrays, not support for 2D arrays yet");
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
            if (!instanceName.Contains("."))
            {
                sb.AppendLine(indentation + $"object {instanceName};");
            }
            sb.AppendLine(indentation + $"if (br.ReadBoolean() == true)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\t{instanceName} = null;");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\tstring objTypeName = br.ReadString();");
            sb.AppendLine(indentation + $"\tType objType = Type.GetType(objTypeName);");
            sb.AppendLine(indentation + $"\tif (IsPrimitive(objType))");
            sb.AppendLine(indentation + "\t{");
            sb.AppendLine(indentation + $"\t\t{instanceName} = DeserializePrimitive(br,objType);");
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
            else if (objType.IsClass)
            {
                DeserializeClass(sb, indentation, instanceName, objType);
            }
            else
            {
                throw new NotImplementedException();
            }
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
            sb.AppendLine(indentation + $"\t{instanceName} = ({objType.FullName})CreateException(exType,source,message,errorCode,stackTrack);");
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
            sb.AppendLine(indentation + $"{objType} {instanceName} = new {objType}();");
            var props = Bister.GetRelevantProperties(objType);
            foreach (var prop in props)
            {
                sb.AppendLine(indentation + $"// For each property...");
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
                sb.AppendLine(indentation + $"// For each property...");
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
            
            if (!instanceName.Contains(".")) // top level instance decleration
            {
                sb.AppendLine(indentation + $"{friendlyTypeName} {instanceName};"); 
            }
            
            sb.AppendLine(indentation + $"bool isNull_{usefulName} = br.ReadBoolean();");
            sb.AppendLine(indentation + $"if (isNull_{usefulName})");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\t{instanceName} = null;");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + $"else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\t{instanceName} = new {friendlyTypeName}();");
            if (objType == typeof(ArrayList))
            {
                DeserializeArrayList(sb, indentation, instanceName);
            }
            if (objType.IsGenericType)
            {
                DeserializeGenericClass(sb, indentation + "\t", instanceName, objType);
            }
            else
            {
                DeserializeNonGenericClass(sb, indentation + "\t", instanceName, objType);
            }
            sb.AppendLine(indentation + "}");
        }

        private static void DeserializeNonGenericClass(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            var props = Bister.GetRelevantProperties(objType);
            foreach (var prop in props)
            {
                sb.AppendLine(indentation + $"// For each property...");
                DeserializeAnyType(sb, indentation, $"{instanceName}.{prop.Name}", prop.PropertyType);
            }
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
