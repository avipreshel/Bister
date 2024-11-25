#pragma warning disable CS8321 // StaticHelper will be used only by the generated code, so the compiler gives false-error which we are supressing 
using BisterLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace GeneratedNS
{
    public static class StaticHelper
    {
        public static string GetFQTypeName(Type type)
        {
            return $"{type.FullName},{type.Assembly.GetName().Name}";
        }
        public static DateTime DeserializeDateTime(BinaryReader br)
        {
            long fileTime = br.ReadInt64();
            return fileTime == 0 ? new DateTime(0) : DateTime.FromFileTime(fileTime);
        }

        public static void Serialize(Exception ex, BinaryWriter bw)
        {
            if (ex == null)
            {
                bw.Write(true);
            }
            else
            {
                bw.Write(false);
                bw.Write(GetFQTypeName(ex.GetType()));
                bw.Write(ex.Source == null ? BisterConsts.NullStr : ex.Source);
                bw.Write(ex.Message);
                bw.Write(ex.StackTrace == null ? BisterConsts.NullStr : ex.StackTrace);
                bw.Write(ex.HResult);
            }
        }

        public static Exception DeserializeException(BinaryReader br)
        {
            if (br.ReadBoolean() == true)
            {
                return null;
            }
            
            string exTypeName = br.ReadString();
            Type exType = Type.GetType(exTypeName);
            string source = br.ReadString();
            string message = br.ReadString();
            string stackTrace = br.ReadString();
            int errorCode = br.ReadInt32();
            
            Exception ex = (Exception)Activator.CreateInstance(exType);
            BisterConsts.ExceptionHResult.SetValue(ex, unchecked(errorCode));
            if (source != BisterConsts.NullStr)
            {
                BisterConsts.ExceptionSource.SetValue(ex, unchecked(source));
            }
            BisterConsts.ExceptionMessage.SetValue(ex, unchecked(message));
            if (stackTrace != BisterConsts.NullStr)
            {
                BisterConsts.ExceptionStackTrace.SetValue(ex, unchecked(stackTrace));
            }

            return ex;
        }

        public static void Serialize(Type type, BinaryWriter bw)
        {
            bw.Write(GetFQTypeName(type));
        }

        public static Type DeserializeSystemType(BinaryReader br)
        {
            return Type.GetType(br.ReadString());
        }

        public static void Serialize(IEnumerable<Enum> arr, BinaryWriter bw)
        {
            if (arr == null)
            {
                bw.Write(true);
            }
            else
            {
                bw.Write(false);
                bw.Write(arr.Count());
                foreach (var item in arr)
                {
                    Serialize(item, bw);
                }
            }
        }

        public static void Serialize(IEnumerable<object> arr, BinaryWriter bw)
        {
            if (arr == null)
            {
                bw.Write(true);
            }
            else
            {
                bw.Write(false);
                bw.Write(arr.Count());
                foreach (var item in arr)
                {
                    Serialize(item, bw);
                }
            }
        }

        public static object[] DeserializeSystemArrayOfObjects(BinaryReader br)
        {
            if (br.ReadBoolean() == true)
            {
                return null;
            }
            else
            {
                int count = br.ReadInt32();
                var arr = new object[count];
                for (int i = 0; i < count; i++)
                {
                    arr[i] = DeserializeSystemObject(br);

                }
                return arr;
            }
        }

        public static Enum[] DeserializeSystemArrayOfEnums(BinaryReader br)
        {
            if (br.ReadBoolean() == true)
            {
                return null;
            }
            else
            {
                int count = br.ReadInt32();
                Enum[] arr = new Enum[count];
                for (int i = 0; i < count; i++)
                {
                    arr[i] = DeserializeSystemEnum(br);

                }
                return arr;
            }
        }

        public static List<Enum> DeserializeListOfEnums(BinaryReader br)
        {
            if (br.ReadBoolean() == true)
            {
                return null;
            }
            else
            {
                int count = br.ReadInt32();
                var arr = new List<Enum>(count);
                for (int i = 0; i < count; i++)
                {
                    arr.Add(DeserializeSystemEnum(br));
                }
                return arr;
            }
        }

        public static void Serialize(string[] arr, BinaryWriter bw)
        {
            if (arr == null)
            {
                bw.Write(true);
            }
            else
            {
                bw.Write(false);
                bw.Write(arr.Length);
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i] == null)
                    {
                        bw.Write(true);
                    }
                    else
                    {
                        bw.Write(false);
                        bw.Write(arr[i]);
                    }
                }
            }
        }

        public static string[] DeserializeSystemStringArray(BinaryReader br)
        {
            if (br.ReadBoolean() == true)
            {
                return null;
            }
            else
            {
                int count = br.ReadInt32();
                string[] strArr = new string[count];
                for (int i = 0; i < count; i++)
                {
                    if (br.ReadBoolean() == false)
                    {
                        strArr[i] = br.ReadString();
                    }
                }
                return strArr;
            }
        }

        public static Enum DeserializeSystemEnum(BinaryReader br)
        {
            if (br.ReadBoolean() == false)
            {
                string enumTypeName = br.ReadString();
                Type enumType = Type.GetType(enumTypeName);
                TypeCode enumTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(enumType));
                switch (enumTypeCode)
                {
                    case TypeCode.Int32:
                        return (Enum)Enum.ToObject(enumType, br.ReadInt32());
                    case TypeCode.UInt32:
                        return (Enum)Enum.ToObject(enumType, br.ReadUInt32());
                    case TypeCode.Int16:
                        return (Enum)Enum.ToObject(enumType, br.ReadInt16());
                    case TypeCode.UInt16:
                        return (Enum)Enum.ToObject(enumType, br.ReadUInt16());
                    case TypeCode.Int64:
                        return (Enum)Enum.ToObject(enumType, br.ReadInt64());
                    case TypeCode.UInt64:
                        return (Enum)Enum.ToObject(enumType, br.ReadUInt64());
                    case TypeCode.Byte:
                        return (Enum)Enum.ToObject(enumType, br.ReadByte());
                    case TypeCode.SByte:
                        return (Enum)Enum.ToObject(enumType, br.ReadSByte());
                    default:
                        throw new NotImplementedException($"No support for enum type {enumType.FullName}");
                }
            }
            else
            {
                return null;
            }
        }

        public static void Serialize(object item, BinaryWriter bw)
        {
            if (item == null)
            {
                bw.Write(true);
            }
            else
            {
                bw.Write(false);
                Type objType = item.GetType();
                bw.Write(StaticHelper.GetFQTypeName(objType));
                if (StaticHelper.IsPrimitive(objType))
                {
                    StaticHelper.SerializePrimitive(item, objType, bw);
                }
                else if (objType == typeof(object))
                {
                    // Do nothing
                }
                else
                {
                    Bister.Instance.Serialize(item, bw);
                }
            }
        }

        public static object DeserializeSystemObject(BinaryReader br)
        {
            if (br.ReadBoolean() == true)
            {
                return null;
            }
            else
            {
                string objTypeName = br.ReadString();
                Type objType = Type.GetType(objTypeName);
                if (StaticHelper.IsPrimitive(objType))
                {
                    return StaticHelper.DeserializePrimitive(br,objType);
                }
                else if (objType == typeof(object))
                {
                    return new object();
                }
                else
                {
                    return Bister.Instance.Deserialize(br, objType);
                }
            }
          
        }

        public static void Serialize(Enum item, BinaryWriter bw)
        {
            if (item == null)
            {
                bw.Write(true);
            }
            else
            {
                bw.Write(false);
                Type enumType = item.GetType();
                bw.Write(GetFQTypeName(enumType));

                TypeCode enumTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(enumType));
                switch (enumTypeCode)
                {
                    case TypeCode.Int32:
                        bw.Write(Convert.ToInt32(item));
                        break;
                    case TypeCode.UInt32:
                        bw.Write(Convert.ToUInt32(item));
                        break;
                    case TypeCode.Int16:
                        bw.Write(Convert.ToInt16(item));
                        break;
                    case TypeCode.UInt16:
                        bw.Write(Convert.ToUInt16(item));
                        break;
                    case TypeCode.Int64:
                        bw.Write(Convert.ToInt64(item));
                        break;
                    case TypeCode.UInt64:
                        bw.Write(Convert.ToUInt64(item));
                        break;
                    case TypeCode.Byte:
                        bw.Write(Convert.ToByte(item));
                        break;
                    case TypeCode.SByte:
                        bw.Write(Convert.ToSByte(item));
                        break;
                    default:
                        throw new NotImplementedException($"Unknown enum typecode {enumTypeCode}");
                }
            }
        }

        public static void Serialize(IEnumerable<string> arr, BinaryWriter bw)
        {
            if (arr == null)
            {
                bw.Write(true);
            }
            else
            {
                bw.Write(false);
                bw.Write(arr.Count());
                foreach (var item in arr)
                {
                   Serialize(item, bw);
                }
            }
        }

        public static void Serialize(IEnumerable<int> arr, BinaryWriter bw)
        {
            if (arr == null)
            {
                bw.Write(true);
            }
            else
            {
                bw.Write(false);
                bw.Write(arr.Count());
                foreach (var item in arr)
                {
                    bw.Write(item);
                }
            }
        }

        public static void Serialize(string item, BinaryWriter bw)
        {
            if (item == null)
            {
                bw.Write(true);
            }
            else
            {
                bw.Write(false);
                bw.Write(item);
            }
        }

        public static string DeserializeString(BinaryReader br)
        {
            if (br.ReadBoolean() == true)
            {
                return null;
            }
            else
            {
                return br.ReadString();
            }
        }

        public static bool IsPrimitive(Type objType)
        {
            return objType.IsPrimitive || objType == typeof(DateTime) || objType == typeof(decimal) || objType == typeof(string);
        }

        public static object DeserializePrimitive(BinaryReader br, Type objType)
        {
            if (objType == typeof(string))
            {
                if (br.ReadBoolean() == false)
                {
                    return br.ReadString();
                }
                else
                {
                    return null;
                }
            }
            else if (objType == typeof(DateTime))
            {
                long dt = br.ReadInt64();
                return DateTime.FromFileTime(dt);
            }
            else if (objType == typeof(decimal))
            {
                return br.ReadDecimal();
            }
            else if (objType == typeof(int))
            {
                return br.ReadInt32();
            }
            else if (objType == typeof(uint))
            {
                return br.ReadUInt32();
            }
            else if (objType == typeof(short))
            {
                return br.ReadInt16();
            }
            else if (objType == typeof(ushort))
            {
                return br.ReadUInt16();
            }
            else if (objType == typeof(long))
            {
                return br.ReadInt64();
            }
            else if (objType == typeof(ulong))
            {
                return br.ReadUInt64();
            }
            else if (objType == typeof(byte))
            {
                return br.ReadByte();
            }
            else if (objType == typeof(sbyte))
            {
                return br.ReadSByte();
            }
            else if (objType == typeof(char))
            {
                return br.ReadChar();
            }
            else if (objType == typeof(float))
            {
                return br.ReadSingle();
            }
            else if (objType == typeof(double))
            {
                return br.ReadDouble();
            }
            else if (objType == typeof(bool))
            {
                return br.ReadBoolean();
            }
            else
            {
                throw new NotImplementedException($"Unknown type {objType.FullName}");
            }
        }

        
        public static void SerializeUserEnum(object instance, TypeCode typeCode, BinaryWriter bw)
        {
            switch (typeCode)
            {
                case TypeCode.Int32:
                    bw.Write((int)instance);
                    break;
                case TypeCode.UInt32:
                    bw.Write((uint)instance);
                    break;
                case TypeCode.Int16:
                    bw.Write((short)instance);
                    break;
                case TypeCode.UInt16:
                    bw.Write((ushort)instance);
                    break;
                case TypeCode.Int64:
                    bw.Write((long)instance);
                    break;
                case TypeCode.UInt64:
                    bw.Write((ulong)instance);
                    break;
                case TypeCode.Byte:
                    bw.Write((byte)instance);
                    break;
                case TypeCode.SByte:
                    bw.Write((sbyte)instance);
                    break;
                default:
                    throw new NotImplementedException($"Unknown enum type {typeCode} for {instance}");

            }
        }

        public static void SerializePrimitive(object instance, Type objType, BinaryWriter bw)
        {
            if (objType == typeof(string))
            {
                if (instance == null)
                {
                    bw.Write(true);
                }
                else
                {
                    bw.Write(false);
                    bw.Write((string)instance);
                }
                
            }
            else if (objType == typeof(DateTime))
            {
                bw.Write(((DateTime)instance).ToFileTime());
            }
            else if (objType == typeof(decimal))
            {
                bw.Write((decimal)instance);
            }
            else if (objType == typeof(int))
            {
                bw.Write((int)instance);
            }
            else if (objType == typeof(uint))
            {
                bw.Write((uint)instance);
            }
            else if (objType == typeof(short))
            {
                bw.Write((short)instance);
            }
            else if (objType == typeof(ushort))
            {
                bw.Write((ushort)instance);
            }
            else if (objType == typeof(long))
            {
                bw.Write((long)instance);
            }
            else if (objType == typeof(ulong))
            {
                bw.Write((ulong)instance);
            }
            else if (objType == typeof(byte))
            {
                bw.Write((byte)instance);
            }
            else if (objType == typeof(sbyte))
            {
                bw.Write((sbyte)instance);
            }
            else if (objType == typeof(char))
            {
                bw.Write((char)instance);
            }
            else if (objType == typeof(float))
            {
                bw.Write((float)instance);
            }
            else if (objType == typeof(double))
            {
                bw.Write((double)instance);
            }
            else if (objType == typeof(bool))
            {
                bw.Write((bool)instance);
            }
            else
            {
                throw new NotImplementedException($"Unknown type {objType.FullName}");
            }
        }

        public static ArrayList DeserializeArrayList(BinaryReader br)
        {
            if (br.ReadBoolean() == true)
                return null;

            int count = br.ReadInt32();
            ArrayList array = new ArrayList(count);
            for (int i = 0; i < count; i++)
            {
                bool isNull = br.ReadBoolean();
                if (isNull)
                {
                    array.Add(null);
                }
                else
                {
                    string itemTypeName = br.ReadString();
                    Type itemType = Type.GetType(itemTypeName);
                    if (StaticHelper.IsPrimitive(itemType))
                    {
                        array.Add(DeserializePrimitive(br, itemType));
                    }
                    else if (itemType == typeof(string))
                    {
                        array.Add(br.ReadString());
                    }
                    else if (itemType.IsEnum)
                    {
                        Type enumPrimitiveType = itemType.GetEnumUnderlyingType();
                        array.Add(Enum.ToObject(itemType, DeserializePrimitive(br, enumPrimitiveType)));
                    }
                    else if (itemType == typeof(object))
                    {
                        array.Add(new object());
                    }
                    else if (itemType == typeof(TimeSpan))
                    {
                        array.Add(TimeSpan.FromTicks(br.ReadInt64()));
                    }
                    else if (itemType.IsClass || itemType.IsValueType)
                    {
                        array.Add(Bister.Instance.Deserialize(br, itemType));
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            return array;
        }


        public static void SerializeArrayList(ArrayList instance, BinaryWriter bw)
        {
            if (instance == null)
            {
                bw.Write(true);
            }
            else
            {
                bw.Write(false);
                bw.Write(instance.Count);
                foreach (object item in instance)
                {
                    if (item == null)
                    {
                        bw.Write(true);
                    }
                    else
                    {
                        bw.Write(false);
                        Type itemType = item.GetType();
                        bw.Write(GetFQTypeName(itemType));
                        if (StaticHelper.IsPrimitive(itemType))
                        {
                            SerializePrimitive(item, itemType, bw);
                        }
                        else if (itemType.IsEnum)
                        {
                            Type enumPrimitiveType = itemType.GetEnumUnderlyingType();
                            SerializePrimitive(item, enumPrimitiveType, bw);
                        }
                        else if (itemType == typeof(object))
                        {
                            // nothing to serialize
                        }
                        else if (itemType == typeof(string))
                        {
                            bw.Write((string)item);
                        }
                        else if (itemType == typeof(TimeSpan))
                        {
                            bw.Write(((TimeSpan)item).Ticks);
                        }
                        else
                        {
                            Bister.Instance.Serialize(item, bw); // note: universe might collapse here
                        }
                    }
                }
            }
        }

    }
}
#pragma warning restore CS8321 // Local function is declared but never used