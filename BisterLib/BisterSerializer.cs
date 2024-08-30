using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
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
            if (Bister.IsPrimitive(keyType) && Bister.IsPrimitive(valType))
            {
                sb.AppendLine(indentation + $"bw.Write((int){instanceName}.Count);");
                sb.AppendLine(indentation + $"foreach (var item in {instanceName})");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(item.Key);");
                sb.AppendLine(indentation + $"\tbw.Write(item.Value);");
                sb.AppendLine(indentation + "}");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static void SerializeGenericList(string instanceName, string indentation, StringBuilderVerbose sb, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);

            Type valType = objType.GenericTypeArguments[0];
            if (valType == typeof(Enum)) // List<Enum>
            {
                
                // Write the enum type
                sb.AppendLine(indentation + $"bw.Write((int){instanceName}.Count);");
                sb.AppendLine(indentation + $"if ({instanceName}.Count > 0)");
                sb.AppendLine(indentation + "{");
                //sb.AppendLine(indentation + $"\tType enumType = {instanceName}[0].GetType();");
                //sb.AppendLine(indentation + $"\tbw.Write(enumType.AssemblyQualifiedName);");
                //sb.AppendLine(indentation + $"\tType enumNativeType = enumType.GetEnumUnderlyingType();");
                //sb.AppendLine(indentation + $"\tvar enumValueField = enumType.GetField(\"value__\");");
                sb.AppendLine(indentation + $"\tforeach (var item in {instanceName})");
                sb.AppendLine(indentation + "\t{");
                sb.AppendLine(indentation + $"\t\tType itemType = item.GetType();");
                sb.AppendLine(indentation + $"\t\tbw.Write(itemType.AssemblyQualifiedName);");
                sb.AppendLine(indentation + $"\t\tType enumNativeType = itemType.GetEnumUnderlyingType();");
                sb.AppendLine(indentation + $"\t\tif (enumNativeType == typeof(byte)) bw.Write(Convert.ToByte(item));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(sbyte)) bw.Write(Convert.ToSByte(item));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(short)) bw.Write(Convert.ToInt16(item));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(ushort)) bw.Write(Convert.ToUInt16(item));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(int)) bw.Write(Convert.ToInt32(item));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(uint)) bw.Write(Convert.ToUInt32(item));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(long)) bw.Write(Convert.ToInt64(item));");
                sb.AppendLine(indentation + $"\t\telse if (enumNativeType == typeof(ulong)) bw.Write(Convert.ToUInt64(item));");
                sb.AppendLine(indentation + $"\t\telse throw new Exception(\"Failed to serialize {instanceName}\");");
                sb.AppendLine(indentation + "\t}");
                sb.AppendLine(indentation + "}");
            }
            else if (valType == typeof(object)) // List<Enum>
            {
                throw new NotImplementedException();
            }
            else
            {
                sb.AppendLine(indentation + $"bw.Write((int){instanceName}.Count);");
                sb.AppendLine(indentation + $"foreach (var item in {instanceName})");
                sb.AppendLine(indentation + "{");
                SerializeGenericItem("item", indentation + '\t', valType, sb);
                sb.AppendLine(indentation + "}");
            }
        }

        public static void SerializeGenericItem(string instanceName, string indentation, Type objType, StringBuilderVerbose sb)
        {
            if (Bister.IsPrimitive(objType))
            {
                sb.AppendLine(indentation + $"bw.Write(({objType}){instanceName});");
            }
            else if (objType.IsEnum && objType != typeof(Enum))
            {
                Type enumNativeType = objType.GetEnumUnderlyingType();
                SerializeGenericItem(instanceName, indentation, enumNativeType, sb);
            }
            else
            {
                throw new NotImplementedException($"Unable to create serialize code for {instanceName} of type {objType}");
            }
        }

        public static void SerializerSystemEnum(string indentation, string instanceName, StringBuilderVerbose sb)
        {
            // Since we can't know the actual Enum during run time, so we need to do magic
            sb.AppendLine(indentation + $"Type enumType = {instanceName}.GetType();");
            sb.AppendLine(indentation + $"Type enumNativeType = {instanceName}.GetType()!.GetEnumUnderlyingType()!;");
            sb.AppendLine(indentation + $"object numericVal = enumType!.GetField(\"value__\")!.GetValue({instanceName})!;");
            sb.AppendLine(indentation + $"bw.Write(enumType!.AssemblyQualifiedName!);");
            sb.AppendLine(indentation + $"if (enumNativeType == typeof(byte)) bw.Write((byte)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(sbyte)) bw.Write((sbyte)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(short)) bw.Write((short)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(int)) bw.Write((int)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(long)) bw.Write((long)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(ushort)) bw.Write((short)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(uint)) bw.Write((int)numericVal);");
            sb.AppendLine(indentation + $"else if (enumNativeType == typeof(ulong)) bw.Write((long)numericVal);");
            sb.AppendLine(indentation + $"else throw new Exception(\"Failed to serialize {instanceName}\");");
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
            sb.AppendLine(indentation + $"if ({instanceName} == null)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tbw.Write(true);");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + $"else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tbw.Write(false);");

            if (objType.IsGenericType)
            {
                SerializeGenericClass(sb, indentation + "\t", instanceName, objType);
            }
            else
            {
                SerializeNonGenericClass(sb, indentation + "\t", instanceName, objType);
            }
            sb.AppendLine(indentation + "}");
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
                var props = Bister.GetRelevantProperties(objType);
                foreach (var prop in props)
                {
                    sb.AppendLine(indentation + $"// For each property...");
                    SerializeAnyType(sb, indentation, $"{instanceName}.{prop.Name}", prop.PropertyType);
                }
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
            sb.AppendLine(indentation + $"{Bister.BinaryWriterMethod(Type.GetTypeCode(objType),instanceName)};");
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
            else // Must be some class..
            {
                SerializeClass(sb, indentation, instanceName, objType);
            }
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
            sb.AppendLine(indentation + $"if ({instanceName} == null)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tbw.Write(true);");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tbw.Write(false);");
            sb.AppendLine(indentation + $"\tType itemType = {instanceName}.GetType();");
            sb.AppendLine(indentation + $"\tbw.Write(itemType.AssemblyQualifiedName);");
            sb.AppendLine(indentation + $"\tif (IsPrimitive(itemType))");
            sb.AppendLine(indentation + "\t{");
            sb.AppendLine(indentation + $"\t\tSerializePrimitive({instanceName},itemType,bw);");
            sb.AppendLine(indentation + "\t}");
            sb.AppendLine(indentation + "\telse if (itemType == typeof(object))");
            sb.AppendLine(indentation + "\t{");
            sb.AppendLine(indentation + $"\t\t//Do Nothing :) We already know it's an object, so deserializer will just create an instance of object...");
            sb.AppendLine(indentation + "\t}");
            sb.AppendLine(indentation + "\telse");
            sb.AppendLine(indentation + "\t{");
            sb.AppendLine(indentation + $"\t\tBister.Instance.Serialize({instanceName},bw);");
            sb.AppendLine(indentation + "\t}");
            sb.AppendLine(indentation + "}");
            
        }

        public static void SerializeArrayList(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            sb.AppendLine(indentation + $"bw.Write((int){instanceName}.Count);");
            sb.AppendLine(indentation + $"for (int i =0;i<{instanceName}.Count;i++)");
            sb.AppendLine(indentation + "{");
            SerializeSystemObject(sb, indentation + "\t", $"{instanceName}[i]");
            sb.AppendLine(indentation + "}");
        }

        static void WriteEnum(StringBuilderVerbose sb,string indentation,string instanceName)
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


        private static void SerializeSystemArray(StringBuilderVerbose sb, string indentation, string instanceName, Type arrayType)
        {
            Bister.PrintMethodName(sb, indentation, arrayType);

            sb.AppendLine(indentation + $"bw.Write((int){instanceName}.Length);");
            Type arrayItemType = arrayType.GetElementType();
            if (Bister.IsPrimitive(arrayItemType))
            {
                sb.AppendLine(indentation + $"for (int i = 0; i < {instanceName}.Length ; i++)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write({instanceName}[i]);");
                sb.AppendLine(indentation + "}");
            }
            else if (arrayItemType == typeof(Enum))
            {
                sb.AppendLine(indentation + $"for (int i = 0 ;i < {instanceName}.Length ; i++)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tvar item = {instanceName}[i];");
                WriteEnum(sb, indentation + "\t", "item");
                sb.AppendLine(indentation + "}");
            }
            else if (arrayItemType.IsEnum)
            {
                sb.AppendLine(indentation + $"for (int i = 0 ; i < {instanceName}.Length ; i++)");
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
