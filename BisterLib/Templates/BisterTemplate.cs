using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BisterLib;
using System.CodeDom;
using System.Reflection;
<<<USINGS>>>

namespace GeneratedNS
{
    internal class ___SERIALIZER_TYPE_NAME___ : IBisterGenerated<___TYPE_NAME___>
    {
        public void Serialize(___TYPE_NAME___ instance, BinaryWriter bw)
        {
___SERIALIZER_BODY___
        }

        public ___TYPE_NAME___ Deserialize(BinaryReader br)
        {
___DESERIALIZER_BODY___
        }

        public byte[] Serialize(___TYPE_NAME___ instance)
        {
            using (var ms = new MemoryStream(___BINARY_WRITER_BUFFER_SIZE___))
            {
                using (var bw = new BinaryWriter(ms))
                {
                    Serialize(instance, bw);
                    bw.Flush();
                    return ms.ToArray();
                }
            }
        }

        public void SerializeObj(object instance, BinaryWriter bw)
        {
            Serialize((___TYPE_NAME___)instance, bw);
        }

        public object DeserializeObj(BinaryReader br)
        {
            return (object)Deserialize(br);
        }

        public ___TYPE_NAME___ Deserialize(byte[] buffer)
        {
            using (var ms = new MemoryStream(buffer))
            {
                using (var br = new BinaryReader(ms))
                {
                    return Deserialize(br);
                }
            }
        }

        public byte[] SerializeObj(object instance)
        {
            return Serialize((___TYPE_NAME___)instance);
        }

        public object DeserializeObj(byte[] buffer)
        {
            return (object)Deserialize(buffer);
        }

        #region Helper method

        Exception CreateException(Type exType,string source, string message,int errorCode,string stackTrace)
        {
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

        bool IsPrimitive(Type objType)
        {
            return objType.IsPrimitive || objType == typeof(string) || objType == typeof(DateTime) || objType == typeof(decimal);
        }

        object DeserializePrimitive(BinaryReader br,Type objType)
        {
            if (objType == typeof(string))
            {
                return br.ReadString();
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

        void SerializePrimitive(object instance,Type objType, BinaryWriter bw)
        {
            if (objType == typeof(string))
            {
                bw.Write((string)instance);
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

        void DeserializeArrayList(ArrayList instance, int count,BinaryReader br)
        {
            for (int i = 0; i < count; i++)
            {
                bool isNull = br.ReadBoolean();
                if (isNull)
                {
                    instance.Add(null);
                }
                else
                {
                    string itemTypeName = br.ReadString();
                    Type itemType = Type.GetType(itemTypeName);
                    if (IsPrimitive(itemType))
                    {
                        instance.Add(DeserializePrimitive(br, itemType));
                    }
                    else if (itemType.IsEnum)
                    {
                        Type enumPrimitiveType = itemType.GetEnumUnderlyingType();
                        instance.Add(Enum.ToObject(itemType, DeserializePrimitive(br, enumPrimitiveType)));
                    }
                    else if (itemType == typeof(object))
                    {
                        instance.Add(new object());
                    }
                    else if (itemType == typeof(TimeSpan))
                    {
                        instance.Add(TimeSpan.FromTicks(br.ReadInt64()));
                    }
                    else if (itemType.IsClass || itemType.IsValueType)
                    {
                        instance.Add(Bister.Instance.Deserialize(br, itemType));
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        void SerializeArrayList(ArrayList instance,BinaryWriter bw)
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
                        bw.Write(itemType.AssemblyQualifiedName);
                        if (IsPrimitive(itemType))
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
        #endregion
    }
}
