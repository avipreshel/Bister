using System;
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
    internal class ___SERIALIZER_TYPE_NAME___ : IBisterGenerated
    {
        public void Serialize(object instanceX, BinaryWriter bw)
        {
            if (instanceX == null)
            {
                bw.Write(true);
                return;
            }
            
            var instance = (___TYPE_NAME___)instanceX;
            ___SERIALIZER_BODY___
        }

        public object Deserialize(BinaryReader br)
        {
___DESERIALIZER_BODY___
        }

    }
}
