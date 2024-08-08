using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using EasyCFLib;
<<<USINGS>>>

namespace GeneratedNS
{
    internal class <<<SERIALIZER_TYPE_NAME>>> : IBisterGenerated<<<<TYPE_NAME>>>>
    {
            public byte[] Serialize(<<<TYPE_NAME>>> instance, MemoryStream ms, BinaryWriter bw)
            {
                if (instance == null)
                {
                    bw.Write((byte)0);
                }
                else
                {
                    bw.Write((byte)1);
<<<SERIALIZER_BODY>>>
                }
                bw.Flush();
                return ms.ToArray();
            }

        public <<<TYPE_NAME>>> Deserialize(BinaryReader br)
        {
            byte isNull = br.ReadByte();
            if (isNull == 0)
            {
                return null;
            }
            else
            {
                <<<TYPE_NAME>>> instance = new <<<TYPE_NAME>>>();
<<<DESERIALIZER_BODY>>>
                return instance;
            }
        }

        public byte[] Serialize(<<<TYPE_NAME>>> instance)
        {
            using var ms = new MemoryStream(<<<BINARY_WRITER_BUFFER_SIZE>>>);
            using var bw = new BinaryWriter(ms);
            return Serialize(instance,ms, bw);
        }

        public <<<TYPE_NAME>>> Deserialize(byte[] buffer)
        {
            using var ms = new MemoryStream(buffer);
            using var br = new BinaryReader(ms);
            return Deserialize(br);
        }
    }
}
