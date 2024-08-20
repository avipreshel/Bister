using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace BisterLib
{
    internal static class BisterDeserializer
    {
        public static void DeserializeArrayList(string indentation, StringBuilder sb, string instanceName)
        {
            sb.AppendLine(indentation + $"// {MethodBase.GetCurrentMethod().Name}");
            sb.AppendLine(indentation + $"int capacity = br.ReadInt32();");
            sb.AppendLine(indentation + $"{instanceName} = new ArrayList(capacity);");
            sb.AppendLine(indentation + $"for (int i = 0; i<= capacity;i++)");
            sb.AppendLine(indentation + "{");
            BisterDeserializer.DeserializeArrayObjectItem(indentation + "\t", sb, instanceName);
            sb.AppendLine(indentation + "}");
        }

        public static void DeserializeArrayObjectItem(string indentation, StringBuilder sb, string instanceName)
        {
            sb.AppendLine(indentation + $"string itemTypeStr = br.ReadString();");
            sb.AppendLine(indentation + $"Type itemType = Type.GetType(itemTypeStr);");
            sb.AppendLine(indentation + $"if (itemTypeStr == \"System.String\") {instanceName}.Add(br.ReadString());");
            sb.AppendLine(indentation + $"else if (itemType is decimal) {instanceName}.Add(br.ReadDecimal());");
            sb.AppendLine(indentation + $"else if (itemType is bool) {instanceName}.Add(br.ReadBoolean());");
            sb.AppendLine(indentation + $"else if (itemType is short) {instanceName}.Add(br.ReadInt16());");
            sb.AppendLine(indentation + $"else if (itemType is ushort) {instanceName}.Add(br.ReadUInt16());");
            sb.AppendLine(indentation + $"else if (itemTypeStr == \"System.Int32\") {instanceName}.Add(br.ReadInt32());");
            sb.AppendLine(indentation + $"else if (itemType is uint) {instanceName}.Add(br.ReadUInt32());");
            sb.AppendLine(indentation + $"else if (itemType is long) {instanceName}.Add(br.ReadInt64());");
            sb.AppendLine(indentation + $"else if (itemType is ulong) {instanceName}.Add(br.ReadUInt64());");
            sb.AppendLine(indentation + $"else if (itemType is char) {instanceName}.Add(br.ReadChar());");
            sb.AppendLine(indentation + $"else if (itemType is byte) {instanceName}.Add(br.ReadByte());");
            sb.AppendLine(indentation + $"else if (itemType is sbyte) {instanceName}.Add(br.ReadSByte());");
            sb.AppendLine(indentation + $"else throw new NotImplementedException();");
        }
    }
}
