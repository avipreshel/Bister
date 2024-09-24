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
        public void Test_GenericListOfInt()
        {
            var instance = new List<int>() { 1, 2, 3 };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_GenericListOfFloats()
        {
            var instance = new List<float>() { 1f, 2f, 3f, 10f };
            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_EmptyGenericList()
        {
            var instance = new List<float>();
            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_NullGenericList()
        {
            List<float>? instance = null;
            ValidateLogic(instance, isExpectingNull : true);
        }



        [TestMethod]
        public void Test_ClassWithLegacyArray()
        {
            var instance = new ClassWithLegacyArray()
            {
                LegacyArray = [10, null, new object(),"hi", new List<int>() { 5, 6 }]
            };

            

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_Dictionary_object_to_string()
        {
            Dictionary<object, string> instance = new Dictionary<object, string>()
           {
               {new object(),"first" },
               {new List<int> {1,2 },"third" },
               {new ClassWithArrays(),"furth" }
           };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_Exception()
        {
            Exception instance;
            try
            {
                throw new NotImplementedException("I am a test exception");
            }
            catch (Exception ex)
            {
                instance = ex;
            }

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_ExceptionWithNullSourceValue()
        {
            var instance =  new NotImplementedException("I am a test exception");

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_ClassWithArrays_allempty()
        {
            var instance = new ClassWithArrays();

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_Dictionary_string_to_object()
        {
            var instance = new Dictionary<string,object>()
           {
               {"first",new object() },
               {"third", new List<int> {1,2 } },
               {"furth",new ClassWithArrays() }
           };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_WrongTypeDeserialization()
        {
            var instance = new ClassWithLegacyArray()
            {
                LegacyArray = [10, null, new object(),new List<int>() { 5,6 },"hello", TestEnum.Two, TestEnum.Three]
            };

            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            Assert.ThrowsException<Exception>(() => Bister.Instance.Deserialize<ClassWithArrays>(blob));
        }

        [TestMethod]
        public void Test_DynamicTypeDictionary()
        {
            var instance = new Dictionary<Enum, object>();
            instance.Add(TestEnum.One, 1);
            instance.Add(TestEnum.Two, "2");
            instance.Add(TestEnum2.Five, 5f);

            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            Assert.ThrowsException<Exception>(() => Bister.Instance.Deserialize<ClassWithArrays>(blob));
        }

        [TestMethod]
        public void Test_ClassWithArrays()
        {
            var instance = new ClassWithArrays()
            {
                ArrayPropSystemEnum = [TestEnum.Three, TestEnum.Two, TestEnum.One],
                ArrayPropInt = [1,2,3,4,5],
                ArrayPropString = ["wow","this","is","cool"],
                ArrayPropTestEnum = [TestEnum.One, TestEnum.Two, TestEnum.Three]
            };

            ValidateLogic(instance);
        }

       

        [TestMethod]
        public void Test_NullObject()
        {
            object? instance = null;
            ValidateLogic(instance, isExpectingNull : true);
        }

        [TestMethod]
        public void Test_Null()
        {
            ValidateLogic<List<int>?>(null, isExpectingNull: true);
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
                Prop_TimeSpan = DateTime.Now.TimeOfDay
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
        public void Test_ClassWithObjectString()
        {
            ClassWithObject instance = new ClassWithObject()
            {
                TheObject = "hello I am object"
            };

            ValidateLogic(instance);

        }

        [TestMethod]
        public void Test_ClassWithObjectInt()
        {
            ClassWithObject instance = new ClassWithObject()
            {
                TheObject = 5
            };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_ClassWithObjectFloat()
        {
            ClassWithObject instance = new ClassWithObject()
            {
                TheObject = 6f
            };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_ClassWithObjectNull()
        {
            ClassWithObject instance = new ClassWithObject()
            {
                TheObject = null
            };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_ClassWithObjectAsObject()
        {
            ClassWithObject instance = new ClassWithObject()
            {
                TheObject = new object()
            };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_ClassWithObjectAsAnotherClass()
        {
            ClassWithObject instance = new ClassWithObject()
            {
                TheObject = new ClassWithADictStringToInt() {  Prop = Enumerable.Range(1, 1000).Select(i => (i.ToString(), i)).ToDictionary() }
            };

            ValidateLogic(instance);
        }


        /// <summary>
        /// Helper method. This method takes an instance, serialize it to a blob, then de-serialize the blob, which effectively creates a copy of the original
        /// instance. It then serialize it again, just so that we can do a simple blob-to-blob comparison, instead of coding specific comparison code for each class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        public void ValidateLogic<T>(T instance, bool isExpectingNull = false)
        {
            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);

            T copyOfinstance = Bister.Instance.Deserialize<T>(blob);

            if (isExpectingNull)
            {
                Assert.IsNull(copyOfinstance);
            }
            else
            {
                Assert.IsNotNull(copyOfinstance);

                var copyOfBlob = Bister.Instance.Serialize(copyOfinstance);
                Assert.IsNotNull(copyOfBlob);

                CollectionAssert.AreEqual(blob, copyOfBlob);
            }
            
        }

    }
}