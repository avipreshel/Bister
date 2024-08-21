using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BisterLib
{
    internal static class BisterSerializer
    {
        public static void DuplicateIfForAllPrimitives(StringBuilder sb, string indentation, string pattern)
        {
            var primitives = new List<string>() { "string", "sbyte", "byte", "ushort", "short", "uint", "int", "ulong", "long", "decimal", "bool", "char" };

            for (int i = 0; i < primitives.Count; i++)
            {
                if (i == 0)
                {
                    sb.AppendLine(indentation + "if " + pattern.Replace("__pattern__", primitives[i]));
                }
                else
                {
                    sb.AppendLine(indentation + "else if " + pattern.Replace("__pattern__", primitives[i]));
                }
            }
            sb.AppendLine(indentation + "else throw new NotImplementedException();");
        }

        public static void DuplicateIfForAllEnumPrimitives(StringBuilder sb, string indentation, string pattern)
        {
            var primitives = new List<string>() { "int", "uint", "short", "ushort", "sbyte", "byte", "ulong", "long" };

            for (int i = 0; i < primitives.Count; i++)
            {
                if (i == 0)
                {
                    sb.AppendLine(indentation + "if " + pattern.Replace("__pattern__", primitives[i]));
                }
                else
                {
                    sb.AppendLine(indentation + "else if " + pattern.Replace("__pattern__", primitives[i]));
                }
            }
            sb.AppendLine(indentation + "else throw new NotImplementedException();");
        }

        /// <summary>
        /// Serialize System.Object
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="indentation"></param>
        /// <param name="instanceName"></param>
        public static void SerializeObject(StringBuilder sb, string indentation, string instanceName)
        {
            sb.AppendLine(indentation + $"Type itemType = {instanceName}.GetType();");
            sb.AppendLine(indentation + $"bw.Write(itemType.FullName);");
            DuplicateIfForAllPrimitives(sb, indentation, $"({instanceName} is __pattern__) bw.Write((__pattern__){instanceName});");
        }

        public static void SerializeArrayList(StringBuilder sb, string indentation, string instanceName, Type arrayType)
        {
            sb.AppendLine(indentation + $"// {MethodBase.GetCurrentMethod().Name}");
            sb.AppendLine(indentation + $"bw.Write((int){instanceName}.Count);");
            sb.AppendLine(indentation + $"if ((int){instanceName}.Count > 0)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\tfor (int i =0;i<{instanceName}.Count;i++)");
            sb.AppendLine(indentation + "\t{");
            SerializeObject(sb, indentation + "\t\t", $"{instanceName}[i]");
            sb.AppendLine(indentation + "\t}");
            sb.AppendLine(indentation + "}");
        }

        static void WriteEnum(StringBuilder sb,string indentation,string instanceName)
        {
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\tType instanceType = {instanceName}.GetType();");
            sb.AppendLine(indentation + $"\tType instancePrimitiveType = Enum.GetUnderlyingType(instanceType);");
            sb.AppendLine(indentation + $"\tbw.Write(instanceType.AssemblyQualifiedName);");
            sb.AppendLine(indentation + $"\tif (instancePrimitiveType == typeof(int)) bw.Write(Convert.ToInt32({instanceName}));");
            sb.AppendLine(indentation + $"\telse if (instancePrimitiveType == typeof(uint)) bw.Write(Convert.ToUInt32({instanceName}));");
            sb.AppendLine(indentation + $"\telse if (instancePrimitiveType == typeof(long)) bw.Write(Convert.ToInt64({instanceName}));");
            sb.AppendLine(indentation + $"\telse if (instancePrimitiveType == typeof(ulong)) bw.Write(Convert.ToUInt64({instanceName}));");
            sb.AppendLine(indentation + $"\telse if (instancePrimitiveType == typeof(ushort)) bw.Write(Convert.ToUInt16({instanceName}));");
            sb.AppendLine(indentation + $"\telse if (instancePrimitiveType == typeof(short)) bw.Write(Convert.ToInt16({instanceName}));");
            sb.AppendLine(indentation + $"\telse if (instancePrimitiveType == typeof(sbyte)) bw.Write(Convert.ToSByte({instanceName}));");
            sb.AppendLine(indentation + $"\telse if (instancePrimitiveType == typeof(byte)) bw.Write(Convert.ToByte({instanceName}));");
            sb.AppendLine(indentation + $"\telse throw new NotImplementedException();");
            sb.AppendLine(indentation + "}");
        }

        public static void SerializeArray(StringBuilder sb, string indentation, string instanceName, Type arrayType)
        {
            sb.AppendLine(indentation + $"// {MethodBase.GetCurrentMethod().Name}");
            if (arrayType == typeof(ArrayList))
            {
                SerializeArrayList(sb, indentation, instanceName, arrayType);
            }
            else
            {
                sb.AppendLine(indentation + $"bw.Write((int){instanceName}.Length);");
                Type arrayItemType = arrayType.GetElementType();
                if (Bister.IsPrimitive(arrayItemType))
                {
                    sb.AppendLine(indentation + $"for (int i =0;i<{instanceName}.Length;i++)");
                    sb.AppendLine(indentation + "{");
                    sb.AppendLine(indentation + $"\tbw.Write({instanceName}[i]);");
                    sb.AppendLine(indentation + "}");
                }
                else if (arrayItemType == typeof(Enum))
                {
                    sb.AppendLine(indentation + $"for (int i =0;i<{instanceName}.Length;i++)");
                    sb.AppendLine(indentation + "{");
                    sb.AppendLine(indentation + $"\tvar item = {instanceName}[i];");
                    WriteEnum(sb, indentation + "\t", "item");
                    sb.AppendLine(indentation + "}");
                }
                else if (arrayItemType.IsEnum)
                {
                    sb.AppendLine(indentation + $"for (int i =0;i<{instanceName}.Length;i++)");
                    sb.AppendLine(indentation + "{");
                    if (Type.GetTypeCode(arrayItemType) == TypeCode.Int16 || Type.GetTypeCode(arrayItemType) == TypeCode.UInt16)
                    {
                        sb.AppendLine(indentation + $"\tbw.Write((short){instanceName}[i]);");
                    }
                    else if (Type.GetTypeCode(arrayItemType) == TypeCode.Int32 || Type.GetTypeCode(arrayItemType) == TypeCode.UInt32)
                    {
                        sb.AppendLine(indentation + $"\tbw.Write((int){instanceName}[i]);");
                    }
                    else if (Type.GetTypeCode(arrayItemType) == TypeCode.Int64 || Type.GetTypeCode(arrayItemType) == TypeCode.UInt64)
                    {
                        sb.AppendLine(indentation + $"\tbw.Write((long){instanceName}[i]);");
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                    sb.AppendLine(indentation + "}");
                }
            }
        }
    }
}
