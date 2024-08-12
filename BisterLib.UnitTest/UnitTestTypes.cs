using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisterLib.UnitTest
{
    public enum TestEnum
    {
        One,Two, Three
    }

    public class ClassWithPrimitivesOnly
    {
        public TestEnum Prop { get; set; }

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
    }

    public class ClassWithAListOfEnum
    {
        public List<Enum> Prop { get; set; } = new List<Enum>();
    }

    public class ClassWithAListOfEnumExplicit
    {
        public List<TestEnum> Prop { get; set; } = new List<TestEnum>();
    }

    public class ClassWithADictStringToInt
    {
        public Dictionary<string,int> Prop { get; set; }

        public ClassWithADictStringToInt()
        {
            Prop = new Dictionary<string, int>();
        }
    }

    public class ClassWithAListOfFloats
    {
        public List<float> Prop { get; set; }

        public ClassWithAListOfFloats()
        {
            Prop = new List<float>();
        }

        public ClassWithAListOfFloats(int size)
        {
            Prop = Enumerable.Range(1,size).Select(i=>(float)i).ToList();
        }
    }

    public class ClassWithAListOfStrings
    {

        public List<string> Prop { get; set; }

        public ClassWithAListOfStrings()
        {
            Prop = new List<string>();
        }

        public ClassWithAListOfStrings(int size)
        {
            Prop = Enumerable.Range(1, size).Select(i => i.ToString()).ToList();
        }
    }
}
