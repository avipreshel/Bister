using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisterLib.UnitTest
{
    public enum TestEnum
    {
        One,Two, Three
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

        public ClassWithADictStringToInt(int size)
        {
            Prop = Enumerable.Range(1, size)
                .Select(i => (i.ToString(), i)).ToDictionary();
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
