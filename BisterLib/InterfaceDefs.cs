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


        /// <summary>
        /// This API should be used when the type of the object is unknown during compile time.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        byte[] Serialize(object instance);
        T Deserialize<T>(byte[] blob);

        object Deserialize(byte[] blob,Type objType);
    }

    /// <summary>
    /// This interface is needed just so we can cache all the generated serializers
    /// </summary>
    public interface IBisterGenerated
    {
        byte[] SerializeObj(object obj);

        object DeserializeObj(byte[] buffer);
    }

    /// <summary>
    /// This interface will be implemented by the generated serializers
    /// </summary>
    public interface IBisterGenerated<T> : IBisterGenerated
    {
        byte[] Serialize(T obj);

        T Deserialize(byte[] buffer);
    }
}
