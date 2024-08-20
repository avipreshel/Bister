using System;
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
    }
}
