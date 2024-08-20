using BisterLib;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace BisterLib.UnitTest
{
    [TestClass]
    public class BisterUnitTests
    {
        public BisterUnitTests() 
        {
            Bister.Instance.DebugPath = @"C:\temp\serialize.cs";
        }

        [TestMethod]
        public void Test_VectorT()
        {
            Vector<float> instance = new Vector<float>();
            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            var copyOfinstance = Bister.Instance.Deserialize<Vector<float>>(blob);
            Assert.IsNotNull(copyOfinstance);
            Assert.AreEqual(instance.X, copyOfinstance.X);
            Assert.AreEqual(instance.Y, copyOfinstance.Y);
        }

        [TestMethod]
        public void Test_ClassWithArrays()
        {
            var instance = new ClassWithArrays()
            { 
                ArrayPropEnum = [TestEnum.Three, TestEnum.Two, TestEnum.One],
                ArrayPropInt = [1,2,3,4,5],
                ArrayPropString = ["wow","this","is","cool"],
                ArrayPropTestEnum = [TestEnum.One, TestEnum.Two, TestEnum.Three],
                ArrayDotNet2 = [10,11,12]
            };

            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            var copyOfinstance = Bister.Instance.Deserialize<ClassWithArrays>(blob);
            Assert.IsNotNull(copyOfinstance);
            
            CollectionAssert.AreEqual(instance.ArrayPropEnum, copyOfinstance.ArrayPropEnum);
            CollectionAssert.AreEqual(instance.ArrayPropInt, copyOfinstance.ArrayPropInt);
            CollectionAssert.AreEqual(instance.ArrayPropString, copyOfinstance.ArrayPropString);
            CollectionAssert.AreEqual(instance.ArrayPropTestEnum, copyOfinstance.ArrayPropTestEnum);
            CollectionAssert.AreEqual(instance.ArrayDotNet2, copyOfinstance.ArrayDotNet2);
        }

        [TestMethod]
        public void Test_GenericListOfPrimitives()
        {
            List<float> instance = new List<float>();
            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            var copyOfinstance = Bister.Instance.Deserialize<List<float>>(blob);
            Assert.IsNotNull(copyOfinstance);
            CollectionAssert.AreEqual(instance, copyOfinstance);
        }

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
        [DataRow(1)]
        [DataRow(1000)]
        public void Test_ClassWithAListOfEnum(int size)
        {
            var instance = new ClassWithAListOfEnum();
            Random random = new Random(42);
            
            instance.Prop = Enumerable.Range(0, size).Select(i => (Enum)Enum.ToObject(typeof(TestEnum),random.Next((int)TestEnum.One, (int)TestEnum.Three + 1))).ToList();


            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            var copyOfinstance = Bister.Instance.Deserialize<ClassWithAListOfEnum>(blob);
            Assert.IsNotNull(copyOfinstance);
            CollectionAssert.AreEqual(instance.Prop, copyOfinstance.Prop);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(1000)]
        public void Test_ClassWithAListOfEnumExplicit(int size)
        {
            var instance = new ClassWithAListOfEnumExplicit();
            Random random = new Random(42);
            instance.Prop = Enumerable.Range(0, size).Select(i => (TestEnum)random.Next((int)TestEnum.One, (int)TestEnum.Three + 1)).ToList();

            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            var copyOfinstance = Bister.Instance.Deserialize<ClassWithAListOfEnumExplicit>(blob);
            Assert.IsNotNull(copyOfinstance);
            CollectionAssert.AreEqual(instance.Prop, copyOfinstance.Prop);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(1000)]
        public void Test_ClassWithAListOfPrimitives(int size)
        {
            var instance = new ClassWithAListOfFloats(size);

            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            var copyOfinstance = Bister.Instance.Deserialize<ClassWithAListOfFloats>(blob);
            Assert.IsNotNull(copyOfinstance);
            CollectionAssert.AreEqual(instance.Prop, copyOfinstance.Prop);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(1000)]
        public void Test_ClassWithAListOfStrings(int size)
        {
            var instance = new ClassWithAListOfStrings(size);

            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            var copyOfinstance = Bister.Instance.Deserialize<ClassWithAListOfStrings>(blob);
            Assert.IsNotNull(copyOfinstance);
            CollectionAssert.AreEqual(instance.Prop, copyOfinstance.Prop);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(1000)]
        public void Test_ClassWithADictStringToInt(int size)
        {
            var instance = new ClassWithADictStringToInt();

            instance.Prop = Enumerable.Range(1, size).Select(i => (i.ToString(), i)).ToDictionary();

            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            var copyOfinstance = Bister.Instance.Deserialize<ClassWithADictStringToInt>(blob);
            Assert.IsNotNull(copyOfinstance);
            CollectionAssert.AreEqual(instance.Prop, copyOfinstance.Prop);
        }
    }
}