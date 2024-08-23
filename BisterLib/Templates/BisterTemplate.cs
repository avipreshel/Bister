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
        public byte[] Serialize(___TYPE_NAME___ instance, MemoryStream ms, BinaryWriter bw)
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
