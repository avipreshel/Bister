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

            object instance = new object();

            BisterSerializer.SerializeAnyType(sb, string.Empty, "instance", instance.GetType());
            Assert.IsTrue(sb.Length != 0);

        }
    }
}
