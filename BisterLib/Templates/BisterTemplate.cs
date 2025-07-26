
#nullable disable

#pragma warning disable CS0105 // Supress warning about any redundant using statememnt
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.CodeDom;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
<<<USINGS>>>
#pragma warning restore CS0105

namespace GeneratedNS
{
    internal class ___SERIALIZER_TYPE_NAME___ : IBisterTypeSerializer
    {
        public void Serialize(object instanceX, BinaryWriter bw)
        {
            var instance = (___TYPE_NAME___)instanceX;
___SERIALIZER_BODY___
        }

        public object Deserialize(BinaryReader br)
        {
___DESERIALIZER_BODY___
        }

    }
}
