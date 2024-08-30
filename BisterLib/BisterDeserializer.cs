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

     

        public static void DeserializeGenericDictionary(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
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
            else
            {
                throw new NotImplementedException();
            }
        }

        public static void DeserializeGenericList(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            string usefulVariableName = Bister.GetUsefulName(instanceName);
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

        //public static void PropertyDeserializer(string indentation, PropertyInfo prop, StringBuilderVerbose sb)
        //{
        //    Bister.PrintMethodName(sb, indentation, prop.PropertyType);

        //    sb.AppendLine(indentation + $"// Deserializing {prop.DeclaringType.Name}.{prop.Name}");

        //    // we avoid c# 7 syntax since we want it to be porable for dotnet framework 4.8
        //    if (Bister.IsPrimitive(prop.PropertyType))
        //    {
        //        sb.AppendLine(indentation + $"instance.{prop.Name} = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(prop.PropertyType))};");
        //    }
        //    else if (prop.PropertyType == typeof(Enum)) // Unspecific enum
        //    {
        //        // Since we can't know the actual type, we need to assume the worst (64bit enum)
        //        DeserializerSystemEnum(sb, indentation, $"instance.{prop.Name}");
        //    }
        //    else if (prop.PropertyType.IsEnum)
        //    {
        //        Type underlytingType = Enum.GetUnderlyingType(prop.PropertyType);
        //        string propType = prop.PropertyType.FullName.Replace("+", ".");
        //        sb.AppendLine(indentation + $"instance.{prop.Name} = ({propType})br.{Bister.BinaryReaderMethod(Type.GetTypeCode(underlytingType))};");
        //    }
        //    else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
        //    {
        //        PropertyDeserializerList(indentation, prop, sb);
        //    }
        //    else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        //    {
        //        PropertyDeserializerDictionary(indentation, prop, sb);
        //    }
        //    else if (prop.PropertyType.IsArray)
        //    {
        //        BisterDeserializer.DeserializeArray(sb, indentation, $"instance.{prop.Name}", prop.PropertyType);
        //    }
        //    else if (prop.PropertyType == typeof(ArrayList))
        //    {
        //        BisterDeserializer.DeserializeArrayList(sb, indentation, $"instance.{prop.Name}");
        //    }
        //    else if (prop.PropertyType.IsClass)
        //    {
        //        sb.AppendLine(indentation + $"instance.{prop.Name} = Bister.Instance.GetSerializer<{prop.PropertyType}>().Deserialize(br);");
        //    }
        //    else
        //    {
        //        throw new Exception($"Property {prop.Name} type {prop.PropertyType} is not supported");
        //    }
        //}

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

            sb.AppendLine(indentation + $"int capacity = br.ReadInt32();");
            sb.AppendLine(indentation + $"{instanceName} = new ArrayList(capacity);");
            sb.AppendLine(indentation + $"for (int i = 0; i< capacity;i++)");
            sb.AppendLine(indentation + "{");
            BisterDeserializer.DeserializeArrayObjectItem(indentation + "\t", sb, instanceName);
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


        public static void DeserializeAnyType(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            if (objType == typeof(Enum)) // Unknown enum
            {
                DeserializerSystemEnum(sb, indentation, instanceName);
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
            else if (objType.IsClass)
            {
                DeserializeClass(sb, indentation, instanceName, objType);
            }
            else
            {
                throw new NotImplementedException();
            }
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
                DeserializeAnyType(sb, indentation, $"{instanceName}.{prop.Name}", prop.PropertyType);
            }
        }

        private static void DeserializePrimitive(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            TypeCode typeCode = Type.GetTypeCode(objType);
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadBoolean();");
                    break;
                case TypeCode.Char:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadChar();");
                    break;
                case TypeCode.Byte:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadByte();");
                    break;
                case TypeCode.SByte:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadSByte();");
                    break;
                case TypeCode.Int16:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadInt16();");
                    break;
                case TypeCode.UInt16:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadUInt16();");
                    break;
                case TypeCode.Int32:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadInt32();");
                    break;
                case TypeCode.UInt32:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadUInt32();");
                    break;
                case TypeCode.Int64:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadInt64();");
                    break;
                case TypeCode.UInt64:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadUInt64();");
                    break;
                case TypeCode.Single:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadSingle();");
                    break;
                case TypeCode.Double:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadDouble();");
                    break;
                case TypeCode.Decimal:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadDecimal();");
                    break;
                case TypeCode.String:
                    sb.AppendLine(indentation + $"{instanceName} = br.ReadString();");
                    break;
                case TypeCode.DateTime:
                    sb.AppendLine(indentation + "{");
                    sb.AppendLine(indentation + $"\tlong dt = br.ReadInt64();");
                    sb.AppendLine(indentation + $"\t{instanceName} = (dt == 0? new DateTime() : DateTime.FromFileTime(dt));");
                    sb.AppendLine(indentation + "}");
                    break;
                default:
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
