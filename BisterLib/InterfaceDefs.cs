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

    /// <summary>
    /// This interface is needed just so we can cache all the generated serializers
    /// </summary>
    public interface IBisterGenerated
    {
        byte[] SerializeObj(object instance);

        void SerializeObj(object instance, BinaryWriter bw);

        object DeserializeObj(byte[] buffer);

        object DeserializeObj(BinaryReader br);
    }

    /// <summary>
    /// This interface will be implemented by the generated serializers
    /// </summary>
    public interface IBisterGenerated<T> : IBisterGenerated
    {
        byte[] Serialize(T instance);

        T Deserialize(byte[] buffer);
    }
}
