using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace BisterLib
{
    

    public static class BisterSerializer
    {
        public static void SerializeAnyType(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);


            switch (objType)
            {
                case Type t when t == typeof(string):
                    SerializeString(sb, indentation, instanceName);
                    break;
                case Type t when t.IsEnum:
                    SerializeEnum(sb, indentation, instanceName, objType);
                    break;
                case Type t when BisterHelpers.IsPrimitive(t):
                    SerializePrimitive(sb, indentation, instanceName, objType);
                    break;
                case Type t when t == typeof(ArrayList):
                    SerializeArrayList(sb, indentation, instanceName, objType);
                    break;
                case Type t when t == typeof(Enum):
                    SerializeSystemEnum(sb, indentation, instanceName);
                    break;
                case Type t when t == typeof(object):
                    SerializeSystemObject(sb, indentation, instanceName);
                    break;
                case Type t when t == typeof(DateTime):
                    SerializeDateTime(sb, indentation, instanceName);
                    break;
                case Type t when t == typeof(TimeSpan):
                    SerializeTimeSpan(sb, indentation, instanceName);
                    break;
                case Type t when t == typeof(Type):
                    SerializeSystemType(sb, indentation, instanceName, objType);
                    break;
                case Type t when t == typeof(Guid):
                    SerializeGuid(sb, indentation, instanceName);
                    break;
                case Type t when t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>):
                    SerializeKeyValuePair(sb, indentation, instanceName, objType.GenericTypeArguments[0], objType.GenericTypeArguments[1]);
                    break;
                case Type t when t.IsArray:
                    SerializeSystemArray(sb, indentation, instanceName, objType);
                    break;
                case Type t when t.IsValueType:
                    SerializeStruct(sb, indentation, instanceName, objType);
                    break;
                case Type t when typeof(Exception).IsAssignableFrom(t):
                    SerializeException(sb, indentation, instanceName, objType);
                    break;
                case Type t when t.FullName == "System.Drawing.Bitmap":
                    SerializeSystemDrawingBitmap(sb, indentation, instanceName, objType);
                    break;
                case Type t when t.IsClass:
                    SerializeClass(sb, indentation, instanceName, objType);
                    break;
                case Type t when t.IsInterface:
                    SerializeInterface(sb, indentation, instanceName, objType);
                    break;
                default:
                    throw new Exception($"Cannot serialize {objType}");
            }
        }

        private static void SerializeSystemDrawingBitmap(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);

            string usefulName = BisterHelpers.GetUsefulName(instanceName);

            sb.AppendLine(indentation + $"if ({instanceName} == null)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tbw.Write(true);");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tbw.Write(false);");
            sb.AppendLine(indentation + $"\tusing (MemoryStream memoryStream = new MemoryStream())");
            sb.AppendLine(indentation + "\t{");
            sb.AppendLine(indentation + $"\t\t{instanceName}.Save(memoryStream, ImageFormat.Tiff);");
            sb.AppendLine(indentation + $"\t\tvar {usefulName}_blob = memoryStream.ToArray();");
            sb.AppendLine(indentation + $"\t\tbw.Write({usefulName}_blob.Length);");
            sb.AppendLine(indentation + $"\t\tbw.Write({usefulName}_blob);");
            sb.AppendLine(indentation + "\t}");
            sb.AppendLine(indentation + "}");

        }

        private static void SerializeInterface(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            // public void Serialize(object instance, Type objType, BinaryWriter bw)
            sb.AppendLine(indentation + $"if ({instanceName} == null)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tbw.Write(true);");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tbw.Write(false);");
            sb.AppendLine(indentation + $"\tbw.Write(StaticHelper.GetFQTypeName({instanceName}.GetType()));");
            sb.AppendLine(indentation + $"\tBister.Instance.Serialize({instanceName},{instanceName}.GetType(),bw);");
            sb.AppendLine(indentation + "}");
            
            
        }

        private static void SerializeKeyValuePair(StringBuilderVerbose sb, string indentation, string instanceName, Type keyType, Type valType)
        {
            Bister.PrintMethodName(sb, indentation, keyType);
            Bister.PrintMethodName(sb, indentation, valType);

            SerializeAnyType(sb, indentation, $"{instanceName}.Key", keyType);
            SerializeAnyType(sb, indentation, $"{instanceName}.Value", valType);
        }

        public static void SerializeGenericDictionary(string instanceName, string indentation, StringBuilderVerbose sb, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);

            var genericType = BisterHelpers.GetGenericInterface(objType, typeof(IDictionary<,>));
            Type keyType = genericType.GenericTypeArguments[0];
            Type valType = genericType.GenericTypeArguments[1];

            sb.AppendLine(indentation + $"bw.Write((int){instanceName}.Count);");
            sb.AppendLine(indentation + $"foreach (var item in {instanceName})");
            sb.AppendLine(indentation + "{");
            SerializeKeyValuePair(sb, indentation + "\t", "item", keyType,valType);
            sb.AppendLine(indentation + "}");
        }

        public static void SerializeGenericList(string instanceName, string indentation, StringBuilderVerbose sb, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);

            sb.AppendLine(indentation + $"bw.Write((int){instanceName}.Count);");
            sb.AppendLine(indentation + $"foreach (var item in {instanceName})");
            sb.AppendLine(indentation + "{");
            var genericType = BisterHelpers.GetGenericInterface(objType,typeof(IList<>));
            Type valType = genericType.GenericTypeArguments[0];
            SerializeAnyType(sb,indentation + "\t","item",valType);
            sb.AppendLine(indentation + "}");
        }

        public static void SerializeSystemEnum(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            // Since we can't know the actual Enum during run time, so we need to do magic
            sb.AppendLine(indentation + $"StaticHelper.Serialize({instanceName},bw);");
        }

        public static void SerializeClass(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            SerializeNullCheckStart(sb, indentation, instanceName, objType);
            Bister.IncreaseIndent(ref indentation);

            if (BisterHelpers.IsImplementingIEnumerable(objType))
            {
                if (BisterHelpers.TestGenericType(objType, typeof(IList<>)))
                {
                    SerializeGenericList(instanceName,indentation,sb, objType);
                }
                else if (BisterHelpers.TestGenericType(objType, typeof(IDictionary<,>)))
                {
                    SerializeGenericDictionary(instanceName,indentation,sb,objType);
                }
                else if (BisterHelpers.TestGenericType(objType, typeof(ISet<>)))
                {
                    SerializeGenericHashSet(instanceName, indentation, sb, objType);
                }
                else if (BisterHelpers.TestGenericType(objType, typeof(IEnumerable<>)))
                {
                    SerializeIEnumerable(instanceName, indentation, sb, objType);
                }
                else
                {
                    throw new NotImplementedException($"Unknown IEnumerable type {objType}");
                }
            }

            SerializePublicProperties(sb, indentation, instanceName, objType);

            Bister.DecreaseIndent(ref indentation);
            SerializeNullCheckEnd(sb, indentation);
        }

        private static void SerializeGenericHashSet(string instanceName, string indentation, StringBuilderVerbose sb, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            var genericType = BisterHelpers.GetGenericInterface(objType, typeof(ISet<>));
            Type valType = genericType.GenericTypeArguments[0];
            sb.AppendLine(indentation + $"bw.Write({instanceName}.Count);");
            sb.AppendLine(indentation + $"foreach (var item in {instanceName})");
            sb.AppendLine(indentation + "{");
            SerializeAnyType(sb, indentation + "\t", "item", valType);
            sb.AppendLine(indentation + "}");
        }

        private static void SerializeIEnumerable(string instanceName, string indentation, StringBuilderVerbose sb, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            var genericType = BisterHelpers.GetGenericInterface(objType, typeof(IEnumerable<>));
            Type valType = genericType.GenericTypeArguments[0];
            
            sb.AppendLine(indentation + $"foreach (var item in {instanceName})");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tbw.Write(true);"); // true will mark that we have an item to read
            SerializeAnyType(sb, indentation + "\t", "item", valType);
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "bw.Write(false);"); // false will mark that we don't have anymore to read
        }

        private static void SerializeNullCheckEnd(StringBuilderVerbose sb, string indentation)
        {
            
            sb.AppendLine(indentation + "}");
        }

        private static void SerializePublicProperties(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            var props = BisterHelpers.GetRelevantProperties(objType);
            foreach (var prop in props)
            {
                sb.AppendLine(indentation + $"// For each property...");
                SerializeAnyType(sb, indentation, $"{instanceName}.{prop.Name}", prop.PropertyType);
            }
        }

        static void SerializeNullCheckStart(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            sb.AppendLine(indentation + $"if ({instanceName} == null)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tbw.Write(true);");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + $"else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tbw.Write(false);");
        }

        public static void SerializeStruct(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            SerializePublicProperties(sb,indentation, instanceName, objType);
        }

        public static void SerializePrimitive(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);

            sb.AppendLine(indentation + $"bw.Write({instanceName});");
        }

        public static void SerializeEnum(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            Type enumPrimitiveType = objType.GetEnumUnderlyingType();
            sb.AppendLine(indentation + $"{Bister.BinaryWriterMethod(Type.GetTypeCode(enumPrimitiveType), instanceName)};");
        }

        private static void SerializeGuid(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"bw.Write({instanceName}.ToByteArray());");
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
            SerializePublicProperties(sb,indentation,instanceName,objType);
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
            else if (BisterHelpers.IsPrimitive(arrayItemType))
            {
                sb.AppendLine(indentation + $"if ({instanceName} == null)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(true);");
                sb.AppendLine(indentation + "}");
                sb.AppendLine(indentation + "else");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + $"\tbw.Write(false);");
                sb.AppendLine(indentation + $"\tvar byteSpan = MemoryMarshal.AsBytes<{arrayItemType.FullName}>({instanceName});");
                sb.AppendLine(indentation + $"\tbw.Write((int)byteSpan.Length);");
                sb.AppendLine(indentation + $"\tbw.Write(byteSpan.ToArray());");
                sb.AppendLine(indentation + "}");
            }
            else if (arrayItemType == typeof(Enum) || arrayItemType == typeof(object) || arrayItemType == typeof(DateTime) || arrayItemType == typeof(TimeSpan))
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
            else if (arrayItemType.IsClass)
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
                sb.AppendLine(indentation + $"\t\tBister.Instance.Serialize({instanceName}[i],typeof({BisterHelpers.GetFriendlyGenericTypeName(arrayItemType)}),bw);");
                sb.AppendLine(indentation + "\t}");
                sb.AppendLine(indentation + "}");
            }
            else
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
                sb.AppendLine(indentation + $"\t\tif({instanceName}[i] == null)");
                sb.AppendLine(indentation + "\t\t{");
                sb.AppendLine(indentation + "\t\t\tbw.Write(true);");
                sb.AppendLine(indentation + "\t\t}");
                sb.AppendLine(indentation + "\t\telse");
                sb.AppendLine(indentation + "\t\t{");
                sb.AppendLine(indentation + "\t\t\tbw.Write(false);");
                sb.AppendLine(indentation + $"\t\t\tbw.Write(StaticHelper.GetFQTypeName({instanceName}[i].GetType()));");
                sb.AppendLine(indentation + $"\t\t\tBister.Instance.Serialize({instanceName}[i],{instanceName}[i].GetType(),bw);");
                sb.AppendLine(indentation + "\t\t}");
                sb.AppendLine(indentation + "\t}");
                sb.AppendLine(indentation + "}");
            }
        }
    }
}
