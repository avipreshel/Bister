using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisterBenchmarks
{
    public enum TestEnum : ushort
    {
        One, Two, Three
    }

    [MessagePackObject]
    public class ClassWithArrays
    {
        [Key(0)]
        public int[] ArrayPropInt { get; set; } = new int[10];

        [Key(1)]
        public string[] ArrayPropString { get; set; } = new string[10];

        [Key(2)]
        public TestEnum[] ArrayPropTestEnum { get; set; } = new TestEnum[10];

        [Key(3)]
        public DateTime[] ArrayPropDateTime { get; set; } = new DateTime[10];

        [Key(4)]
        public TimeSpan[] ArrayPropTimeSpan { get; set; } = new TimeSpan[10];

        [Key(5)]
        public Dictionary<string, float> DicStr2Float { get; set; } = new Dictionary<string, float>();

        [Key(6)]
        public List<string> ListDT { get; set; } = new();

    }

    [MessagePackObject]
    public class SimpleClass
    {
        [Key(0)]
        public int[] ArrayPropInt { get; set; } = new int[10];

        [Key(1)]
        public string[] ArrayPropString { get; set; } = new string[10];

        [Key(2)]
        public Dictionary<string, float> DicStr2Float { get; set; } = new Dictionary<string, float>();

        [Key(3)]
        public List<string> ListOfStrings { get; set; } = new();

    }
}
