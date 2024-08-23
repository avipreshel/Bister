using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisterLib.UnitTest
{
    [TestClass]
    public class BisterSerializerTests
    {
        public BisterSerializerTests()
        {
            Bister.Instance.DebugPath = @"C:\temp\serialize.cs";
        }

        [TestMethod]
        public void Test_SerializeNonGenericClass()
        {
            StringBuilderVerbose sb = new StringBuilderVerbose(true);

            //object instance = new ClassWithPrimitivesOnly()
            //{
            //    Prop = TestEnum.Two,
            //    Prop_bool = true,
            //    Prop_byte = 1,
            //    Prop_short = 2,
            //    Prop_int = 3,
            //    Prop_long = 4,
            //    Prop_char = 'c',
            //    Prop_decimal = 5,
            //    Prop_ulong = 7,
            //    Prop_sbyte = 9,
            //    Prop_string = "10 wow nice string dude",
            //    Prop_uint = 11,
            //    Prop_ushort = 12,
            //    Prop_double = 13.33,
            //    Prop_float = 14.44f
            //};

            object instance = new object();

            BisterSerializer.SerializeClass(sb, string.Empty, "instance", instance.GetType());
            Assert.IsTrue(sb.Length != 0);

        }
    }
}
