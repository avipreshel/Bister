using BisterUnitTestTypes;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace BisterLib.UnitTest
{
    public class SomeClassWithArrayAndDictionary
    {
        public Vector<int>[] IntArrayProp { get; set; } = Array.Empty<Vector<int>>();
        public Dictionary<string, Vector<int>[]> StringToInt1DDictProp { get; set; } = new Dictionary<string, Vector<int>[]>();
    }

    public abstract class SomeAbstractClass
    {
        public string[] StringArrayProp { get; set; } = Array.Empty<string>();
    }

    public class InheritingFromSomeAbstractClass : SomeAbstractClass
    { 
        public int SomeIntProp { get; set; }
    }

    public class ClassWithAbstractProp
    {
        public SomeAbstractClass? SomeAbstractProp { get; set; } = null;
    }

    public interface ISimpleTestClass
    {
    }

    public class SimpleTestClass : ISimpleTestClass
    {
        public string SomeString { get; set; } = string.Empty;
    }

    public interface ISomeInterace
    { 
        string SomeProp { get; set; }
    }

    public class SomeClass : ISomeInterace
    {
        public string SomeProp { get; set; } = string.Empty;

        public KeyValuePair<object, Enum> KVPProp { get; set; }
    }

    public class SomeClassWithInterfaceProp
    {
        public ISomeInterace? SomeProp1 { get; set; } = null;
        public ISomeInterace? SomeProp2 { get; set; } = null;
        public ISomeInterace? SomePropAlwaysNull { get; set; } = null;
    }

    public class SomeInheritedFromGeneric : SomeGenericClass<ClassWithVector>
    { 
        public string SomeField {  get; set; } = string.Empty;
    }

    public class InheritedFromList<T> : List<T> 
    {
        public T? SomeProp { get; set; }
    }

    public class InheritedFromDictionary<K,V> : Dictionary<K, V> where K : notnull
    {
        public K? SomeKey { get; set; }
        public V? SomeVal { get; set; }
    }

    public class InheritedFromDictionaryStringToInt : Dictionary<string,int>
    {
        public string SomeVal { get; set; } = string.Empty;
    }

    public class InheritedFromListOfString : List<string>
    {
        public int SomeVal { get; set; }
    }

    public class ClassWithGenericListOfClass
    {
        public int SomeVal { get; set; }

        public List<SimpleTestClass> ListOfClass { get; set; } = new();
    }

    public class InheritedFromEnumerableString : IEnumerable<string>
    {
        public int SomeProp { get; set; }

        private readonly List<string> _items = new List<string>();

        // Method to add items to the collection
        public void Add(string item)
        {
            _items.Add(item);
        }

        // Implement IEnumerable<T>.GetEnumerator
        public IEnumerator<string> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        // Implement non-generic IEnumerable.GetEnumerator
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator(); // Reuse the generic enumerator
        }
    }

    public class InheritedFromEnumerable<T> : IEnumerable<T>
    {
        public T? SomeProp { get; set; }

        private readonly List<T> _items = new List<T>();

        // Method to add items to the collection
        public void Add(T item)
        {
            _items.Add(item);
        }

        // Implement IEnumerable<T>.GetEnumerator
        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        // Implement non-generic IEnumerable.GetEnumerator
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator(); // Reuse the generic enumerator
        }
    }

    public class ClassWithObject
    { 
        public object? TheObject { get; set; } = null;
    }


    public class ClassWithPrimitivesOnly
    {
        public TestEnum PropTestEnum { get; set; }

        public int Prop_int { get; set; }

        public uint Prop_uint { get; set; }

        public byte Prop_byte { get; set; }

        public sbyte Prop_sbyte { get; set; }

        public string Prop_string { get; set; } = string.Empty;

        public short Prop_short { get; set; }
        public ushort Prop_ushort { get; set; }

        public long Prop_long { get; set; }
        public ulong Prop_ulong { get; set; }
        public decimal Prop_decimal { get; set; }
        public char Prop_char { get; set; }
        public bool Prop_bool { get; set; }
        public float Prop_float { get; set; }
        public double Prop_double { get; set; }
        public DateTime Prop_DateTime { get; set; }
        public TimeSpan Prop_TimeSpan { get; set; }
    }

    public class ClassWithLegacyArray
    {
        public ArrayList LegacyArray { get; set; } = new ArrayList();
    }

    public class ClassWithArrays
    {
        public sbyte[] ArrayPropSByte { get; set; } = new sbyte[10];
        public double[] ArrayPropDouble { get; set; } = new double[10];
        public object[] ArraySystemObjects { get; set; } = new object[10];
        public int[] ArrayPropInt { get; set; } = new int[10];
        public string[] ArrayPropString { get; set; } = new string[10];
        public TestEnum[] ArrayPropTestEnum { get; set; } = new TestEnum[10];
        public Enum[] ArrayPropSystemEnum { get; set; } = new Enum[10];
        public DateTime[] ArrayPropDateTime { get; set; } = new DateTime[10];
        public TimeSpan[] ArrayPropTimeSpan { get; set; } = new TimeSpan[10];
        public ClassWithPrimitivesOnly[] ArrayOfClasses { get; set; } = new ClassWithPrimitivesOnly[1];

        public Vector<double>[] ArrayOfStructs { get; set; } = Array.Empty<Vector<double>>();
    }


    public class ClassWithGenericLists
    {
        public List<Enum> PropSystemEnum { get; set; } = new();
        public List<TestEnum> PropEnum { get; set; } = new();
        public List<float> PropFloat { get; set; } = new();
        public List<int> PropInt { get; set; } = new();
        public List<sbyte> PropSByte { get; set; } = new();
        public List<string> PropStrings { get; set; } = new();
        public List<uint> PropUint { get; set; } = new();
    }

    public class ClassWithADictStringToInt
    {
        public Dictionary<string,int> Prop { get; set; }

        public ClassWithADictStringToInt()
        {
            Prop = new Dictionary<string, int>();
        }
    }

    public class CustomSerializer : IBisterTypeSerializer
    {
        public static CustomSerializer Instance { get; } = new CustomSerializer();

        public class CustomClass
        { 
            public int SomeField { get; set; }
            public byte[] Blob { get; set; } = Array.Empty<byte>();

            public CustomClass(byte[] blob)
            {
                Blob = blob;
                SomeField = Blob.Length;
            }
        }

        public object? Deserialize(BinaryReader br)
        {
            int length = br.ReadInt32();
            byte[] blob = br.ReadBytes(length);
            CustomClass instance = new CustomClass(blob);
            return instance;
        }

        public void Serialize(object instanceX, BinaryWriter bw)
        {
            CustomClass instance = (CustomClass)instanceX;

            bw.Write(instance.SomeField);
            bw.Write(instance.Blob);
        }
    }

}
