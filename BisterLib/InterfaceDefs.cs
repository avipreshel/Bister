using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BisterLib
{
    public interface IBister
    {
        bool IsDebug { get; }
        string DebugPath { get; set; }

        byte[] Serialize<T>(T instance);

        byte[] Serialize(object instance);
        void Serialize(object instance,BinaryWriter bw);

        T Deserialize<T>(byte[] blob);

        object Deserialize(byte[] blob,Type objType);

        object Deserialize(BinaryReader br, Type objType);
    }

    public interface IBisterGenerated
    {
        void Serialize(object instanceX, BinaryWriter bw);
        object Deserialize(BinaryReader br);
    }
}
