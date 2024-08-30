using BisterLib;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace BisterLib.UnitTest
{
    [TestClass]
    public class BisterTests
    {
        public BisterTests() 
        {
            Bister.Instance.DebugPath = @"C:\temp\serialize.cs";
        }

        [TestMethod]
        public void Test_VectorT()
        {
            var instance = new Vector<float>() { X = float.E, Y = float.Pi };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_ClassWithLegacyArray()
        {
            var instance = new ClassWithLegacyArray()
            {
                ArrayDotNet2 = [10, 11, 12]
            };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_WrongTypeDeserialization()
        {
            var instance = new ClassWithLegacyArray()
            {
                ArrayDotNet2 = [10, 11, 12]
            };

            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            Assert.ThrowsException<Exception>(() => Bister.Instance.Deserialize<ClassWithArrays>(blob));
        }

        [TestMethod]
        public void Test_ClassWithArrays()
        {
            var instance = new ClassWithArrays()
            { 
                ArrayPropEnum = [TestEnum.Three, TestEnum.Two, TestEnum.One],
                ArrayPropInt = [1,2,3,4,5],
                ArrayPropString = ["wow","this","is","cool"],
                ArrayPropTestEnum = [TestEnum.One, TestEnum.Two, TestEnum.Three]
            };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_GenericListOfPrimitives()
        {
            var instance = new List<float>() {  1f,2f,3f,10f};
            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_NullObject()
        {
            object? instance = null;
            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_ClassWithPrimitivesOnly()
        {
            var instance = new ClassWithPrimitivesOnly()
            {
                PropTestEnum = TestEnum.Two,
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
                Prop_float = 14.44f,
                Prop_DateTime = DateTime.Now,
            };

            ValidateLogic(instance);
        }

       
        [TestMethod]
        [DataRow(1)]
        [DataRow(10)]
        public void Test_ClassWithAListOfEnum(int size)
        {
            var instance = new ClassWithAListOfEnum();
            Random random = new Random(42);
            
            instance.Prop = Enumerable.Range(0, size).Select(i => (Enum)Enum.ToObject(typeof(TestEnum),random.Next((int)TestEnum.One, (int)TestEnum.Three + 1))).ToList();


            ValidateLogic(instance);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(1000)]
        public void Test_ClassWithAListOfEnumExplicit(int size)
        {
            var instance = new ClassWithAListOfEnumExplicit();
            Random random = new Random(42);
            instance.Prop = Enumerable.Range(0, size).Select(i => (TestEnum)random.Next((int)TestEnum.One, (int)TestEnum.Three + 1)).ToList();

            ValidateLogic(instance);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(1000)]
        public void Test_ClassWithAListOfPrimitives(int size)
        {
            var instance = new ClassWithAListOfFloats(size);
            
            ValidateLogic(instance);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(1000)]
        public void Test_ClassWithAListOfStrings(int size)
        {
            var instance = new ClassWithAListOfStrings(size);

            ValidateLogic(instance);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(1000)]
        public void Test_ClassWithADictStringToInt(int size)
        {
            var instance = new ClassWithADictStringToInt();

            instance.Prop = Enumerable.Range(1, size).Select(i => (i.ToString(), i)).ToDictionary();

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_SimpleStruct()
        {
            var instance = new Vector() {  X = 1, Y = 2, Z = 3 };

            ValidateLogic(instance);
        }


        [TestMethod]
        public void Test_ClassWithObject()
        {
            ClassWithObject instance = new ClassWithObject()
            {
                TheObject = "hello I am object"
            };

            ValidateLogic(instance);

        }

        public void ValidateLogic<T>(T instance)
        {
            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);

            var copyOfinstance = Bister.Instance.Deserialize<T>(blob);
            Assert.IsNotNull(copyOfinstance);

            var copyOfBlob = Bister.Instance.Serialize(copyOfinstance);
            CollectionAssert.AreEqual(blob, copyOfBlob);
        }

    }
}