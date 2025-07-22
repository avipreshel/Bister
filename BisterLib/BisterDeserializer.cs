using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection.PortableExecutable;

namespace BisterLib
{
    public static class BisterDeserializer
    {
        public static void DeserializeAnyType(StringBuilderVerbose sb, string indentation, string instanceName, Type objType, bool isStructField = false)
        {
            Bister.PrintMethodName(sb, indentation, objType);

            if (Bister.Instance.IsKnownType(objType))
            {
                sb.AppendLine(indentation + $"{instanceName} = ({objType})Bister.Instance.Deserialize(br,typeof({objType}));");
                return;
            }

            switch (objType)
            {
                case Type t when t == typeof(string):
                    DeserializeString(sb, indentation, instanceName);
                    break;
                case Type t when BisterHelpers.IsPrimitive(t):
                    DeserializePrimitive(sb, indentation, instanceName, objType, isStructField);
                    break;
                case Type t when t == typeof(DateTime):
                    DeserializeDateTime(sb, indentation, instanceName);
                    break;
                case Type t when t == typeof(TimeSpan):
                    DeserializeTimeSpan(sb, indentation, instanceName);
                    break;
                case Type t when t == typeof(Enum):
                    DeserializerSystemEnum(sb, indentation, instanceName);
                    break;
                case Type t when t == typeof(object):
                    DeserializeSystemObject(sb, indentation, instanceName);
                    break;
                case Type t when t == typeof(Guid):
                    DeserializeGuid(sb, indentation, instanceName);
                    break;
                case Type t when t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>):
                    DeserializeKeyValuePair(sb, indentation, instanceName, objType);
                    break;
                case Type t when t == typeof(Type):
                    DeserializeSystemType(sb, indentation, instanceName, objType);
                    break;
                case Type t when t.IsEnum:
                    DeserializerUserEnum(sb, indentation, instanceName, objType);
                    break;
                case Type t when t == typeof(ArrayList):
                    DeserializeArrayList(sb, indentation, instanceName);
                    break;
                case Type t when t.IsArray:
                    DeserializeSystemArray(sb, indentation, instanceName, objType);
                    break;
                case Type t when t.IsValueType:
                    DeserializeStruct(sb, indentation, instanceName, objType);
                    break;
                case Type t when typeof(Exception).IsAssignableFrom(t):
                    DeserializeException(sb, indentation, instanceName, objType);
                    break;
                //case Type t when t.FullName == "System.Drawing.Bitmap":
                //    DeserializeSystemDrawingBitmap(sb, indentation, instanceName, objType);
                //    break;
                case Type t when t.IsClass:
                    DeserializeClass(sb, indentation, instanceName, objType);
                    break;
                case Type t when t.IsInterface:
                    DeserializeInterface(sb, indentation, instanceName, objType);
                    break;
                default:
                    throw new NotImplementedException($"No support for {objType}");

            }
        }

        private static void DeserializeSystemDrawingBitmap(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            sb.AppendLine(indentation + "if (br.ReadBoolean())");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\t{instanceName} = null;");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + "\tusing (var ms = new MemoryStream(br.ReadBytes(br.ReadInt32())))");
            sb.AppendLine(indentation + "\t{");
            sb.AppendLine(indentation + $"\t\t{instanceName} = new Bitmap(ms);");
            sb.AppendLine(indentation + "\t}");
            sb.AppendLine(indentation + "}");
        }

        private static void DeserializeInterface(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            
            sb.AppendLine(indentation + $"{instanceName} = br.ReadBoolean()? null : ({BisterHelpers.GetFriendlyGenericTypeName(objType)})Bister.Instance.Deserialize(br,Type.GetType(br.ReadString()));");
        }

        public static void DeserializeGenericDictionary(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            string friendlyTypeName = BisterHelpers.GetFriendlyGenericTypeName(objType);
            string usefulVariableName = BisterHelpers.GetUsefulName(instanceName);

            Type genericType = BisterHelpers.GetGenericInterface(objType,typeof(IDictionary<,>));
            Type keyType = genericType.GenericTypeArguments[0];
            Type valType = genericType.GenericTypeArguments[1];

            string niceKeyTypeName = BisterHelpers.GetFriendlyGenericTypeName(keyType);
            string niceValueTypeName = BisterHelpers.GetFriendlyGenericTypeName(valType);

            sb.AppendLine(indentation + $"int {usefulVariableName}_count = br.ReadInt32();");
            if (objType.FullName.StartsWith("System"))
            {
                sb.AppendLine(indentation + $"{instanceName} = new {friendlyTypeName}({usefulVariableName}_count);");
            }
            else
            {
                sb.AppendLine(indentation + $"{instanceName} = new {friendlyTypeName}();");
            }

            sb.AppendLine(indentation + $"for (int i =0; i<{usefulVariableName}_count;i++)");
            sb.AppendLine(indentation + "{");

            Bister.IncreaseIndent(ref indentation);
            sb.AppendLine(indentation + $"{niceKeyTypeName} key;");
            sb.AppendLine(indentation + $"{niceValueTypeName} val;");
            DeserializeAnyType(sb, indentation, "key", keyType);
            DeserializeAnyType(sb, indentation, "val", valType);

            sb.AppendLine(indentation + $"{instanceName}.Add(key,val);");

            Bister.DecreaseIndent(ref indentation);

            sb.AppendLine(indentation + "}");

        }

        public static void DeserializeGenericList(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);

            string friendlyTypeName = BisterHelpers.GetFriendlyGenericTypeName(objType);
            string usefulName = BisterHelpers.GetUsefulName(instanceName);
            var genericType = BisterHelpers.GetGenericInterface(objType, typeof(IList<>));
            Type valType = genericType.GenericTypeArguments[0];

            
            sb.AppendLine(indentation + $"int {usefulName}_count = br.ReadInt32();");
            if (objType.FullName.StartsWith("System"))
            {
                sb.AppendLine(indentation + $"{instanceName} = new {friendlyTypeName}({usefulName}_count);");
            }
            else
            {
                sb.AppendLine(indentation + $"{instanceName} = new {friendlyTypeName}();");
            }
            
            sb.AppendLine(indentation + $"for (int i = 0; i < {usefulName}_count; i++)");
            sb.AppendLine(indentation + "{");
            Bister.IncreaseIndent(ref indentation);
            sb.AppendLine(indentation + $"{BisterHelpers.GetFriendlyGenericTypeName(valType)} item;");
            DeserializeAnyType(sb, indentation, "item", valType);
            sb.AppendLine(indentation + $"{instanceName}.Add(item);");
            Bister.DecreaseIndent(ref indentation);
            sb.AppendLine(indentation + "}");
        }
       
        public static void DeserializerSystemEnum(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeSystemEnum(br);");
        }

        public static void DeserializeArrayList(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            
            sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeArrayList(br);");
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
                sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeSystemStringArray(br);");
            }
            else if (BisterHelpers.IsPrimitive(arrayItemType))
            {
                TypeCode arrayItemTypeCode = Type.GetTypeCode(arrayItemType);
                sb.AppendLine(indentation + $"if (br.ReadBoolean() == false)");
                sb.AppendLine(indentation + "{");
                sb.AppendLine(indentation + "\tbyte[] blob = br.ReadBytes(br.ReadInt32());");
                sb.AppendLine(indentation + $"\t{instanceName} = MemoryMarshal.Cast<byte, {arrayItemType.FullName}>(blob).ToArray();");
                sb.AppendLine(indentation + "}");
                sb.AppendLine(indentation + $"else {instanceName}=null;");

            }
            else if (arrayItemType == typeof(Enum))
            {
                sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeSystemArrayOfEnums(br);");
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
            else if (arrayItemType == typeof(object))
            {
                sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeSystemArrayOfObjects(br);");
            }
            else if (arrayItemType == typeof(DateTime))
            {
                sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeSerializeDateTimeArr(br);");
            }
            else if (arrayItemType == typeof(TimeSpan))
            {
                sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeSerializeTimeSpanArr(br);");
            }
            else if (arrayItemType.IsClass)
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
                sb.AppendLine(indentation + $"\t\t{instanceName}[i] = ({BisterHelpers.GetFriendlyGenericTypeName(arrayItemType)})Bister.Instance.Deserialize(br,typeof({BisterHelpers.GetFriendlyGenericTypeName(arrayItemType)}));");
                sb.AppendLine(indentation + "\t}");
                sb.AppendLine(indentation + "}");
            }
            else
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
                sb.AppendLine(indentation + $"\t\t{instanceName}[i] = br.ReadBoolean()? null : ({BisterHelpers.GetFriendlyGenericTypeName(arrayItemType)})Bister.Instance.Deserialize(br,Type.GetType(br.ReadString()));");
                sb.AppendLine(indentation + "\t}");
                sb.AppendLine(indentation + "}");
            }
        }

        public static void DeserializeSystemObject(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeSystemObject(br);");
        }

        private static void DeserializeKeyValuePair(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            string usefulName = BisterHelpers.GetUsefulName(instanceName);
            Type keyType = objType.GenericTypeArguments[0];
            Type valType = objType.GenericTypeArguments[1];

            string niceKeyTypeName = BisterHelpers.GetFriendlyGenericTypeName(keyType);
            string niceValueTypeName = BisterHelpers.GetFriendlyGenericTypeName(valType);

            sb.AppendLine(indentation + $"{niceKeyTypeName} {usefulName}_key;");
            sb.AppendLine(indentation + $"{niceValueTypeName} {usefulName}_val;");

            DeserializeAnyType(sb, indentation, $"{usefulName}_key", keyType);
            DeserializeAnyType(sb, indentation, $"{usefulName}_val", valType);
            sb.AppendLine(indentation + $"{instanceName} = new KeyValuePair<{niceKeyTypeName},{niceValueTypeName}>({usefulName}_key,{usefulName}_val);");
        }

        private static void DeserializeGuid(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"{instanceName} = new Guid(br.ReadBytes(16));");
        }

        private static void DeserializeSystemType(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeSystemType(br);");
        }

        private static void DeserializeTimeSpan(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"{instanceName} = TimeSpan.FromTicks(br.ReadInt64());");
        }

        private static void DeserializeDateTime(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"{instanceName} = DateTime.FromBinary(br.ReadInt64());");
        }

        private static void DeserializeString(StringBuilderVerbose sb, string indentation, string instanceName)
        {
            Bister.PrintMethodName(sb, indentation);
            sb.AppendLine(indentation + $"{instanceName} = StaticHelper.DeserializeString(br);");
        }

        private static void DeserializeException(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation);
            DeserializePublicProperties(sb,indentation, instanceName,objType);
            sb.AppendLine(indentation + $"{instanceName} = ({objType.FullName})StaticHelper.DeserializeException(br);");
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
            sb.AppendLine(indentation + $"{instanceName} = new {objType}()");
            sb.AppendLine(indentation + "{");
            var props = BisterHelpers.GetRelevantProperties(objType);
            foreach (var prop in props)
            {
                sb.AppendLine(indentation + $"\t// For each property of {prop.Name}");
                DeserializeAnyType(sb, indentation + "\t", $"{prop.Name}", prop.PropertyType, isStructField : true);
            }
            sb.AppendLine(indentation + "};");
        }

        private static void DeserializeGenericStruct(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            string friendlyTypename = BisterHelpers.GetFriendlyGenericTypeName(objType);
            sb.AppendLine(indentation + $"{instanceName} = new {friendlyTypename}()");
            sb.AppendLine(indentation + "{");
            var props = BisterHelpers.GetRelevantProperties(objType);
            foreach (var prop in props)
            {
                sb.AppendLine(indentation + $"\t// For each property...{prop.Name}");
                DeserializeAnyType(sb, indentation + "\t", $"{prop.Name}", prop.PropertyType, isStructField: true);
            }
            sb.AppendLine(indentation + "};");
        }

        private static void DeserializePrimitive(StringBuilderVerbose sb, string indentation, string instanceName, Type objType, bool isStructField = false)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            char lineEnd = isStructField ? ',' : ';';
            if (objType.IsPrimitive || objType == typeof(string) || objType == typeof(decimal))
            {
                sb.AppendLine(indentation + $"{instanceName} = br.{Bister.BinaryReaderMethod(Type.GetTypeCode(objType))}{lineEnd}");
            }
            else if (objType == typeof(DateTime))
            {
                
                sb.AppendLine(indentation + $"{instanceName} = DateTime.FromFileTime(br.ReadInt64()){lineEnd}");
            }
            else if (objType == typeof(TimeSpan))
            {
                
                sb.AppendLine(indentation + $"{instanceName} = TimeSpan.FromTicks(br.ReadInt64()){lineEnd}");
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

            string friendlyTypeName = BisterHelpers.GetFriendlyGenericTypeName(objType);
            sb.AppendLine(indentation + $"if (br.ReadBoolean() == true)");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\t{instanceName} = null;");
            sb.AppendLine(indentation + "}");
            sb.AppendLine(indentation + "else");
            sb.AppendLine(indentation + "{");

            Bister.IncreaseIndent(ref indentation);
            

            if (BisterHelpers.IsImplementingIEnumerable(objType))
            {
                if (BisterHelpers.TestGenericType(objType, typeof(IList<>)))
                {
                    DeserializeGenericList(sb, indentation, instanceName, objType);
                }
                else if (BisterHelpers.TestGenericType(objType, typeof(IDictionary<,>)))
                {
                    DeserializeGenericDictionary(sb, indentation, instanceName, objType);
                }
                else if (BisterHelpers.TestGenericType(objType, typeof(ISet<>)))
                {
                    DeserializeGenericHashSet(sb, indentation, instanceName, objType);
                }
                else if (BisterHelpers.TestGenericType(objType, typeof(IEnumerable<>)))
                {
                    DeserializeIEnumerable(sb, indentation, instanceName, objType);
                }
                else
                {
                    throw new NotImplementedException($"Unknown IEnumerable type {objType}");
                }
            }
            else
            {
                sb.AppendLine(indentation + $"{instanceName} = new {friendlyTypeName}();");
            }

            DeserializePublicProperties(sb, indentation, instanceName, objType);

            Bister.DecreaseIndent(ref indentation);
            sb.AppendLine(indentation + "}");
        }

        private static void DeserializeGenericHashSet(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            string usefulName = BisterHelpers.GetUsefulName(instanceName);
            string friendlyTypeName = BisterHelpers.GetFriendlyGenericTypeName(objType);
            var genericType = BisterHelpers.GetGenericInterface(objType, typeof(ISet<>));
            Type valType = genericType.GenericTypeArguments[0];
            sb.AppendLine(indentation + $"int {usefulName}_count = br.ReadInt32();");
            sb.AppendLine(indentation + $"{instanceName} = new {friendlyTypeName}();");
            sb.AppendLine(indentation + $"for (int i =0;i<{usefulName}_count;i++)");
            sb.AppendLine(indentation + "{");
            Bister.IncreaseIndent(ref indentation);
            sb.AppendLine(indentation + $"{BisterHelpers.GetFriendlyGenericTypeName(valType)} item;");
            DeserializeAnyType(sb, indentation, "item", valType);
            sb.AppendLine(indentation + $"{instanceName}.Add(item);");
            Bister.DecreaseIndent(ref indentation);
            sb.AppendLine(indentation + "}");
        }

        private static void DeserializeIEnumerable(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            Bister.PrintMethodName(sb, indentation, objType);
            string friendlyTypeName = BisterHelpers.GetFriendlyGenericTypeName(objType);
            Type genericType = BisterHelpers.GetGenericInterface(objType, typeof(IEnumerable<>));
            Type valType = genericType.GenericTypeArguments[0];
            sb.AppendLine(indentation + $"{instanceName} = new {friendlyTypeName}();");
            sb.AppendLine(indentation + $"while (br.ReadBoolean())");
            sb.AppendLine(indentation + "{");
            sb.AppendLine(indentation + $"\t{BisterHelpers.GetFriendlyGenericTypeName(valType)} item;");
            DeserializeAnyType(sb, indentation + "\t", "item", valType);
            sb.AppendLine(indentation + $"\t{instanceName}.Add(item);");
            sb.AppendLine(indentation + "}");
        }

        private static void DeserializePublicProperties(StringBuilderVerbose sb, string indentation, string instanceName, Type objType)
        {
            var props = BisterHelpers.GetRelevantProperties(objType);
            foreach (var prop in props)
            {
                sb.AppendLine(indentation + $"// For each property: {instanceName}.{prop.Name}");
                DeserializeAnyType(sb, indentation, $"{instanceName}.{prop.Name}", prop.PropertyType);
            }
        }
    }
}
