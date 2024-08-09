using BisterLib;

namespace BisterLib.UnitTest
{
    [TestClass]
    public class BisterUnitTests
    {
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