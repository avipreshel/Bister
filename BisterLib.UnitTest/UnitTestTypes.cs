using BisterUnitTestTypes;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisterLib.UnitTest
{

    

    public class SomeInheritedFromGeneric : SomeGenericClass<ClassWithVector>
    { 
        public string SomeField {  get; set; } = string.Empty;
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
    }


    public class ClassWithListsOfEnum
    {
        public List<Enum> PropSystemEnum { get; set; } = new();
        public List<TestEnum> PropEnum { get; set; } = new();
        public List<float> PropFloat { get; set; } = new();
        public List<string> PropStrings { get; set; } = new();
    }

    public class ClassWithADictStringToInt
    {
        public Dictionary<string,int> Prop { get; set; }

        public ClassWithADictStringToInt()
        {
            Prop = new Dictionary<string, int>();
        }
    }
}
