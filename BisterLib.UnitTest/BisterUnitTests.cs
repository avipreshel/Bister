using BisterLib;

namespace BisterLib.UnitTest
{
    [TestClass]
    public class BisterUnitTests
    {
        [TestMethod]
        [DataRow(1)]
        [DataRow(1000)]
        public void Test_ClassWithAListOfPrimitives(int size)
        {
            var instance = new ClassWithAListOfFloats(size);

            Bister.Instance.DebugPath = @"C:\temp\serialize.cs";
            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            object copyOfinstance = Bister.Instance.Deserialize<object>(blob);
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
            object copyOfinstance = Bister.Instance.Deserialize<object>(blob);
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
            object copyOfinstance = Bister.Instance.Deserialize<object>(blob);
            Assert.IsNotNull(copyOfinstance);
            CollectionAssert.AreEqual(instance.Prop, instance.Prop);
        }
    }
}