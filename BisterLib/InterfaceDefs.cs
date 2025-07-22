using System;
using System.IO;

namespace BisterLib
{
    public interface IBister
    {
        bool IsDebug { get; }
        string DebugPath { get; set; }

        byte[] Serialize<T>(T instance);

        byte[] Serialize(object instance,Type objType);

        void Serialize(object instance, Type objType,BinaryWriter bw);

        T Deserialize<T>(byte[] blob);

        object Deserialize(byte[] blob,Type objType);

        object Deserialize(BinaryReader br, Type objType);

        /// <summary>
        /// Provide the user with ability to register it's own serializer types.
        /// This is useful for cases where the user need full control over what and how to serialize
        /// </summary>
        /// <param name="objType"></param>
        /// <param name="serializer"></param>
        void RegisterSerializer(Type objType, IBisterTypeSerializer serializer);

        bool IsKnownType(Type objType);
    }

    public interface IBisterTypeSerializer
    {
        void Serialize(object instanceX, BinaryWriter bw);
        object Deserialize(BinaryReader br);
    }
}
