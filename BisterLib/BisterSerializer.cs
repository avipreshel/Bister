using GeneratedNS;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;

namespace BisterLib
{
    public static class BisterSerializer
    {
        public static void DuplicateIfForAllPrimitives(StringBuilderVerbose sb, string indentation, string pattern)
        {
            var primitives = new List<string>() { "string", "sbyte", "byte", "ushort", "short", "uint", "int", "ulong", "long", "decimal", "bool", "char" };

            for (int i = 0; i < primitives.Count; i++)
            {
                if (i == 0)
                {
                    sb.AppendLine(indentation + "if " + pattern.Replace("__primitive__", primitives[i]));
                }
                else
                {
                    sb.AppendLine(indentation + "else if " + pattern.Replace("__primitive__", primitives[i]));
                }
            }
            sb.AppendLine(indentation + "else throw new NotImplementedException();");
        }

        public static void DuplicateIfForAllEnumPrimitives(StringBuilderVerbose sb, string indentation, string pattern)
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


        public static void SerializeGenericDictionary(string instanceName, string indentation, StringBuilderVerbose sb, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);

            Type keyType = objType.GenericTypeArguments[0];
            Type valType = objType.GenericTypeArguments[1];
            sb.AppendLine(indentation + $"if ({instanceName} == null) bw.Write(true);");
            sb.AppendLine(indentation + $"else");
            sb.AppendLine(indentation + "{");
            indentation += "\t";
            sb.AppendLine(indentation + $"bw.Write(false);");
            sb.AppendLine(indentation + $"bw.Write((int){instanceName}.Count);");
            sb.AppendLine(indentation + $"foreach (var item in {instanceName})");
            sb.AppendLine(indentation + "{");
            indentation += "\t";

            // Serialize key
            if ((keyType == typeof(string) || Bister.IsPrimitive(keyType)))
            {
                sb.AppendLine(indentation + $"// Key is primitive");
                sb.AppendLine(indentation + $"bw.Write(item.Key);");
            }
            else
            {
                sb.AppendLine(indentation + $"// Key is non-primitive");
                SerializeAnyType(sb, indentation, $"item.Key", keyType);
            }

            // Serialize value
            if ((valType == typeof(string) || Bister.IsPrimitive(valType)))
            {
                sb.AppendLine(indentation + $"// Value is primitive");
                sb.AppendLine(indentation + $"bw.Write(item.Value);");
            }
            else
            {
                sb.AppendLine(indentation + $"// Value is non-primitive");
                SerializeAnyType(sb, indentation, $"item.Value", valType);
            }

            indentation = indentation.Substring(0, indentation.Length-1);
            sb.AppendLine(indentation + "}");
            indentation = indentation.Substring(0, indentation.Length - 1);
            sb.AppendLine(indentation + "}");
        }

        public static void SerializeGenericList(string instanceName, string indentation, StringBuilderVerbose sb, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);

            sb.AppendLine(indentation + $"if ({instanceName} == null)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tbw.Write(true);");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tbw.Write(false);");
            sb.AppendLine(indentation + $"\tbw.Write((int){instanceName}.Count);");
            sb.AppendLine(indentation + $"\tforeach (var item in {instanceName})");
            sb.AppendLine(indentation + "\t{");

            Type valType = objType.GenericTypeArguments[0];
            if (valType == typeof(Enum) || valType == typeof(object)) 
            {
                sb.AppendLine(indentation + $"StaticHelper.Serialize({instanceName},bw);");
            }
            else
            {
                SerializeGenericItem("item", indentation + "\t\t", valType, sb);
            }
            sb.AppendLine(indentation + "\t}");
            sb.AppendLine(indentation + "}");
        }

        public static void SerializeGenericItem(string instanceName, string indentation, Type objType, StringBuilderVerbose sb)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            if (Bister.IsPrimitive(objType))
            {
                sb.AppendLine(indentation + $"bw.Write(({objType}){instanceName});"); // TBD can remove cast?
            }
            else if (objType == typeof(string))
            {
                sb.AppendLine(indentation + $"bw.Write({instanceName});"); // TBD can remove cast?
            }
            else if (objType.IsEnum && objType != typeof(Enum))
            {
                Type enumNativeType = objType.GetEnumUnderlyingType();
                SerializeGenericItem(instanceName, indentation, enumNativeType, sb);
            }
            else if (objType == typeof(DateTime))
            {
                sb.AppendLine(indentation + $"bw.Write({instanceName}.ToBinary());"); // TBD can remove cast?
            }
            else
            {
                throw new NotImplementedException($"Unable to create serialize code for {instanceName} of type {objType}");
            }
        }

        public static void SerializerSystemEnum(string indentation, string instanceName, StringBuilderVerbose sb)
        {
            // Since we can't know the actual Enum during run time, so we need to do magic
            sb.AppendLine(indentation + $"StaticHelper.Serialize({instanceName},bw);");
        }


        public static void SerializeGenericClass(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            if (objType.GetGenericTypeDefinition() == typeof(List<>))
            {
                SerializeGenericList(instanceName, indentation, sb, objType);
            }
            else if (objType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                SerializeGenericDictionary(instanceName, indentation, sb, objType);
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
                    SerializeGenericItem($"{instanceName}.{prop.Name}", indentation, prop.PropertyType, sb);
                }
            }
        }

        public static void SerializeClass(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            if (objType.IsGenericType)
            {
                SerializeGenericClass(sb, indentation, instanceName, objType);
            }
            else
            {
                SerializeNonGenericClass(sb, indentation, instanceName, objType);
            }
        }

        public static void SerializeNonGenericClass(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            if (objType == typeof(object))
            {
                sb.AppendLine(indentation + $"if ({instanceName}.GetType() == typeof(object))");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(\"{objType.FullName}\");");
                sb.AppendLine(indentation + "}");
                sb.AppendLine(indentation + "else");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbyte[] blob = Bister.Instance.Serialize({instanceName});");
                sb.AppendLine(indentation + $"\tbw.Write(blob);");
                sb.AppendLine(indentation + "}");
            }
            else
            {
                sb.AppendLine(indentation + $"if ({instanceName} == null)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + "\tbw.Write(true);");
                sb.AppendLine(indentation + "}");
                sb.AppendLine(indentation + $"else");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + "\tbw.Write(false);");
                var props = Bister.GetRelevantProperties(objType);
                foreach (var prop in props)
                {
                    sb.AppendLine(indentation + $"\t// For each property...");
                    SerializeAnyType(sb, indentation + "\t", $"{instanceName}.{prop.Name}", prop.PropertyType);
                }
                sb.AppendLine(indentation + "}");
            }
        }

        public static void SerializeStruct(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            var props = Bister.GetRelevantProperties(objType);
            foreach (var prop in props)
            {
                sb.AppendLine(indentation + $"// For each property...");
                SerializeAnyType(sb, indentation, $"{instanceName}.{prop.Name}", prop.PropertyType);
            }
        }

        public static void SerializePrimitive(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            if (objType.IsPrimitive || objType == typeof(string) || objType == typeof(decimal))
            {
                sb.AppendLine(indentation + $"bw.Write({instanceName});");
            }
            else if (objType == typeof(DateTime))
            {
                sb.AppendLine(indentation + $"bw.Write({instanceName}.ToFileTime());");
            }
            else if (objType == typeof(TimeSpan))
            {
                sb.AppendLine(indentation + $"bw.Write({instanceName}.Ticks);");
            }
            else
            {
                throw new NotImplementedException();
            }

        }

        public static void SerializeEnum(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            Type enumPrimitiveType = objType.GetEnumUnderlyingType();
            sb.AppendLine(indentation + $"{Bister.BinaryWriterMethod(Type.GetTypeCode(enumPrimitiveType), instanceName)};");
        }

        public static void SerializeAnyType(StringBuilderVerbose sb, string indentation, string instanceName,Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            if (objType == typeof(Enum)) // Unknown enum
            {
                SerializerSystemEnum(indentation,instanceName,sb);
            }
            else if (objType == typeof(object))
            {
                SerializeSystemObject(sb, indentation, instanceName);
            }
            else if (objType == typeof(DateTime))
            {
                SerializeDateTime(sb, indentation, instanceName);
            }
            else if (objType == typeof(TimeSpan))
            {
                SerializeTimeSpan(sb, indentation, instanceName);
            }
            else if (objType == typeof(Type))
            {
                SerializeSystemType(sb, indentation, instanceName, objType);
            }
            else if (objType.IsEnum) // Strongly-defined enum
            {
                SerializeEnum(sb, indentation,instanceName,objType);
            }
            else if (Bister.IsPrimitive(objType)) // Native type
            {
                SerializePrimitive(sb, indentation, instanceName, objType);
            }
            else if (objType == typeof(ArrayList))
            {
                SerializeArrayList(sb,indentation,instanceName, objType);
            }
            else if (objType.IsArray) // types such as int[] or string[]
            {
                SerializeSystemArray(sb,indentation,instanceName, objType);
            }
            else if (objType.IsValueType) // a struct
            {
                SerializeStruct(sb, indentation,instanceName, objType);
            }
            else if (typeof(Exception).IsAssignableFrom(objType)) // is it some kind of Exception?
            {
                SerializeException(sb, indentation, instanceName, objType);
            }
            else if (objType == typeof(string))
            {
                SerializeString(sb, indentation, instanceName);
            }
            else if (objType.IsClass)
            {
                SerializeClass(sb, indentation, instanceName, objType);
            }
            else 
            {
                throw new NotImplementedException();
            }
        }

        private static void SerializeSystemType(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            sb.AppendLine(indentation + $"StaticHelper.Serialize({instanceName},bw);");
        }

        private static void SerializeTimeSpan(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"bw.Write({instanceName}.Ticks);");
        }

        private static void SerializeDateTime(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"bw.Write({instanceName}.ToBinary());");
        }

        private static void SerializeString(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation, typeof(string));
            sb.AppendLine(indentation + $"StaticHelper.Serialize({instanceName},bw);");
        }

        public static void SerializeException(StringBuilderVerbose sb, string indentation, string instanceName,Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            sb.AppendLine(indentation + $"StaticHelper.Serialize({instanceName},bw);");
        }

        /// <summary>
        /// Serialize System.Object
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="indentation"></param>
        /// <param name="instanceName"></param>
        public static void SerializeSystemObject(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation, typeof(object));
            sb.AppendLine(indentation + $"StaticHelper.Serialize({instanceName},bw);");
        }

        public static void SerializeArrayList(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            sb.AppendLine(indentation + $"StaticHelper.SerializeArrayList({instanceName},bw);");
        }

        static void SerializeSystemEnum(StringBuilderVerbose sb,string indentation,string instanceName)
        {
            sb.AppendLine(indentation + $"StaticHelper.SerializeSystemEnum({instanceName},bw);");
        }

        private static void SerializeSystemArray(StringBuilderVerbose sb, string indentation, string instanceName, Type arrayType)
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
                sb.AppendLine(indentation + $"StaticHelper.Serialize({instanceName},bw);");
            }
            else if (Bister.IsPrimitive(arrayItemType))
            {
                sb.AppendLine(indentation + $"if ({instanceName} == null)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(true);");
                sb.AppendLine(indentation + "}");
                sb.AppendLine(indentation + "else");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(false);");
                sb.AppendLine(indentation + $"\tReadOnlySpan<byte> byteSpan = MemoryMarshal.AsBytes<{arrayItemType.FullName}>({instanceName});");
                sb.AppendLine(indentation + $"\tbw.Write((int)byteSpan.Length);");
                sb.AppendLine(indentation + $"\tbw.Write(byteSpan);");
                sb.AppendLine(indentation + "}");
            }
            else if (arrayItemType == typeof(Enum) || arrayItemType == typeof(object))
            {
                sb.AppendLine(indentation + $"StaticHelper.Serialize({instanceName},bw);");
            }
            else if (arrayItemType.IsEnum)
            {
                TypeCode arrayItemTypeCode = Type.GetTypeCode(arrayItemType);
                sb.AppendLine(indentation + $"if ({instanceName} == null)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(true);");
                sb.AppendLine(indentation + "}");
                sb.AppendLine(indentation + "else");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(false);");
                sb.AppendLine(indentation + $"\tbw.Write((int){instanceName}.Length);");
                sb.AppendLine(indentation + $"\tfor (int i = 0 ; i < {instanceName}.Length ; i++)");
                sb.AppendLine(indentation + "\t{");
                sb.AppendLine(indentation + "\t\t" + Bister.BinaryWriterMethod(arrayItemTypeCode, $"{instanceName}[i]") + ";");
                sb.AppendLine(indentation + "\t}");
                sb.AppendLine(indentation + "}");
            }
            else if (arrayItemType == typeof(DateTime))
            {
                sb.AppendLine(indentation + $"if ({instanceName} == null)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(true);");
                sb.AppendLine(indentation + "}");
                sb.AppendLine(indentation + "else");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(false);");
                sb.AppendLine(indentation + $"\tbw.Write((int){instanceName}.Length);");
                sb.AppendLine(indentation + $"\tfor (int i = 0 ; i < {instanceName}.Length ; i++)");
                sb.AppendLine(indentation + "\t{");
                sb.AppendLine(indentation + "\t\t" + $"bw.Write({instanceName}[i].ToBinary());");
                sb.AppendLine(indentation + "\t}");
                sb.AppendLine(indentation + "}");
            }
            else if (arrayItemType == typeof(TimeSpan))
            {
                sb.AppendLine(indentation + $"if ({instanceName} == null)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(true);");
                sb.AppendLine(indentation + "}");
                sb.AppendLine(indentation + "else");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(false);");
                sb.AppendLine(indentation + $"\tbw.Write((int){instanceName}.Length);");
                sb.AppendLine(indentation + $"\tfor (int i = 0 ; i < {instanceName}.Length ; i++)");
                sb.AppendLine(indentation + "\t{");
                sb.AppendLine(indentation + "\t\t" + $"bw.Write({instanceName}[i].Ticks);");
                sb.AppendLine(indentation + "\t}");
                sb.AppendLine(indentation + "}");
            }
            else 
            {
                throw new NotImplementedException($"No support yet for serializing {arrayItemType.FullName}[]");
            }
        }
    }
}
