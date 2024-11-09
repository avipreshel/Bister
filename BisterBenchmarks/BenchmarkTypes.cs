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

    public class ClassWithArrays
    {
        public int[] ArrayPropInt { get; set; } = new int[10];
        public string[] ArrayPropString { get; set; } = new string[10];
        public TestEnum[] ArrayPropTestEnum { get; set; } = new TestEnum[10];
        public Enum[] ArrayPropSystemEnum { get; set; } = new Enum[10];
        public DateTime[] ArrayPropDateTime { get; set; } = new DateTime[10];
        public TimeSpan[] ArrayPropTimeSpan { get; set; } = new TimeSpan[10];
        public Dictionary<string, float> DicStr2Float { get; set; } = new Dictionary<string, float>();
        public List<DateTime> ListDT { get; set; } = new List<DateTime>();
    }
}
