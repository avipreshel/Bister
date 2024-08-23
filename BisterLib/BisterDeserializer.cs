using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace BisterLib
{
    internal static class BisterDeserializer
    {
        public static void DeserializeObject(string indentation, StringBuilder sb, string instanceName,Type objType)
        {
            Bister.PrintMethodName(indentation,sb, objType);
            if (Bister.IsPrimitive(objType))
            {
                sb.AppendLine(indentation + $"{instanceName} = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(objType))};");
            }
        }

        public static void DeserializeArrayList(string indentation, StringBuilder sb, string instanceName)
        {
            sb.AppendLine(indentation + $"// {MethodBase.GetCurrentMethod().Name}");
            sb.AppendLine(indentation + $"int capacity = br.ReadInt32();");
            sb.AppendLine(indentation + $"{instanceName} = new ArrayList(capacity);");
            sb.AppendLine(indentation + $"for (int i = 0; i< capacity;i++)");
            sb.AppendLine(indentation + "{");
            BisterDeserializer.DeserializeArrayObjectItem(indentation + "\t", sb, instanceName);
            sb.AppendLine(indentation + "}");
        }

        public static void DeserializeArray(string indentation, StringBuilder sb, string instanceName, Type arrayType)
        {
            sb.AppendLine(indentation + $"// {MethodBase.GetCurrentMethod().Name}");

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

        public static void DeserializeArrayObjectItem(string indentation, StringBuilder sb, string instanceName)
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
