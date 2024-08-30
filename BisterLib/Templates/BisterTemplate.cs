using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BisterLib;
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

        #endregion
    }
}
