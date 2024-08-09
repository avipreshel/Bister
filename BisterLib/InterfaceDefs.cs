using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BisterLib
{
    public interface IBister
    {
        string DebugPath { get; set; }

        byte[] Serialize<T>(T instance);
        T Deserialize<T>(byte[] blob);
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
