﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BisterLib;
using System.CodeDom;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
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

    }

    

}
