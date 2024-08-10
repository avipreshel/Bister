using BisterLib;
using System.ComponentModel.DataAnnotations;

namespace BisterLib.UnitTest
{
    [TestClass]
    public class BisterUnitTests
    {
        [TestMethod]
        public void Test_ClassWithPrimitivesOnly()
        {
            var instance = new ClassWithPrimitivesOnly()
            {
                Prop = TestEnum.Two,
                Prop_bool = true,
                Prop_byte = 1,
                Prop_short = 2,
                Prop_int = 3,
                Prop_long = 4,
                Prop_char = 'c',
                Prop_decimal = 5,
                Prop_ulong = 7,
                Prop_sbyte = 9,
                Prop_string = "10 wow nice string dude",
                Prop_uint = 11,
                Prop_ushort = 12,
                Prop_double = 13.33,
                Prop_float = 14.44f
            };

            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            var copyOfinstance = Bister.Instance.Deserialize<ClassWithPrimitivesOnly>(blob);
            Assert.IsNotNull(copyOfinstance);

            string strInstance = System.Text.Json.JsonSerializer.Serialize(instance);
            string strcopyOfinstance = System.Text.Json.JsonSerializer.Serialize(copyOfinstance);
            Assert.AreEqual(strInstance, strcopyOfinstance);
        }

        [TestMethod]
        public void Test_ClassWithAListOfEnum()
        {
            var instance = new ClassWithAListOfEnum();
            instance.Prop = new List<Enum>()
            {
                TestEnum.Three,TestEnum.Two,TestEnum.Three,TestEnum.One
            };

            Bister.Instance.DebugPath = @"C:\temp\serialize.cs";
            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            object copyOfinstance = Bister.Instance.Deserialize<object>(blob);
            Assert.IsNotNull(copyOfinstance);
            CollectionAssert.AreEqual(instance.Prop, instance.Prop);
        }

        [TestMethod]
        public void Test_ClassWithAListOfEnumExplicit()
        {
            var instance = new ClassWithAListOfEnumExplicit();
            instance.Prop = new List<TestEnum>()
            {
                TestEnum.Three,TestEnum.Two,TestEnum.Three,TestEnum.One
            };

            Bister.Instance.DebugPath = @"C:\temp\serialize.cs";
            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            var copyOfinstance = Bister.Instance.Deserialize<ClassWithAListOfEnumExplicit>(blob);
            Assert.IsNotNull(copyOfinstance);
            CollectionAssert.AreEqual(instance.Prop, instance.Prop);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(1000)]
        public void Test_ClassWithAListOfPrimitives(int size)
        {
            var instance = new ClassWithAListOfFloats(size);

            Bister.Instance.DebugPath = @"C:\temp\serialize.cs";
            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            var copyOfinstance = Bister.Instance.Deserialize<ClassWithAListOfFloats>(blob);
            Assert.IsNotNull(copyOfinstance);
            CollectionAssert.AreEqual(instance.Prop, instance.Prop);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(1000)]
        public void Test_ClassWithAListOfStrings(int size)
        {
            var instance = new ClassWithAListOfStrings(size);

            Bister.Instance.DebugPath = @"C:\temp\serialize.cs";
            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            var copyOfinstance = Bister.Instance.Deserialize<ClassWithAListOfStrings>(blob);
            Assert.IsNotNull(copyOfinstance);
            CollectionAssert.AreEqual(instance.Prop, instance.Prop);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(1000)]
        public void Test_ClassWithADictStringToInt(int size)
        {
            var instance = new ClassWithADictStringToInt(size);

            Bister.Instance.DebugPath = @"C:\temp\serialize.cs";
            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            var copyOfinstance = Bister.Instance.Deserialize<ClassWithADictStringToInt>(blob);
            Assert.IsNotNull(copyOfinstance);
            CollectionAssert.AreEqual(instance.Prop, instance.Prop);
        }
    }
}