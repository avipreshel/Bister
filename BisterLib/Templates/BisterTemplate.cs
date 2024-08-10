using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using BisterLib;
<<<USINGS>>>

namespace GeneratedNS
{
    internal class ___SERIALIZER_TYPE_NAME___ : IBisterGenerated<___TYPE_NAME___>
    {
        public byte[] Serialize(___TYPE_NAME___ instance, MemoryStream ms, BinaryWriter bw)
        {
            bw.Write(instance.GetType().AssemblyQualifiedName);
            if (instance == null)
            {
                bw.Write((byte)0);
            }
            else
            {
                bw.Write((byte)1);
___SERIALIZER_BODY___
            }
            bw.Flush();
            return ms.ToArray();
        }

        public ___TYPE_NAME___ Deserialize(BinaryReader br)
        {
            _ = br.ReadString(); // Reads the type name
            byte isNull = br.ReadByte();
            if (isNull == 0)
            {
                return null;
            }
            else
            {
                ___TYPE_NAME___ instance = new ___TYPE_NAME___();
___DESERIALIZER_BODY___
                return instance;
            }
        }

        public byte[] Serialize(___TYPE_NAME___ instance)
        {
            using (var ms = new MemoryStream(___BINARY_WRITER_BUFFER_SIZE___))
            {
                using (var bw = new BinaryWriter(ms))
                {
                    return Serialize(instance, ms, bw);
                }
            }
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

        public byte[] SerializeObj(object obj)
        {
            return Serialize((___TYPE_NAME___)obj);
        }

        public object DeserializeObj(byte[] buffer)
        {
            return (object)Deserialize(buffer);
        }
    }
}
