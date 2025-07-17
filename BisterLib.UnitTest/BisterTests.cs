using BisterLib;
using BisterUnitTestTypes;
using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace BisterLib.UnitTest
{
    [TestClass]
    public class BisterTests
    {
        public BisterTests() 
        {
            
        }

        //  
        [TestMethod]
        public void Test_InheritedFromEnumerableString()
        {
            var instance = new InheritedFromEnumerableString() { "1", "2", "3" };
            instance.SomeProp = 4;

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_InheritedFromListOfString()
        {
            var instance = new InheritedFromListOfString() { "1","2","3" };
            instance.SomeVal = 4;

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_InheritedFromDictionaryStringToInt()
        {
            var instance = new InheritedFromDictionaryStringToInt() { {"one",1 }, {"2",2 },{ "3", 3 } };
            instance.SomeVal = "four";

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_Array1D()
        {
            var instanceInt = new int[] {1, 2, 3};

            ValidateLogic(instanceInt);

            //Bister.Instance.DebugPath = @"C:\temp\serialize.cs";
            var instanceEnum = new Enum[] { TestEnum.One , TestEnum2.Five, TestEnum2.Six };

            ValidateLogic(instanceEnum);

            var instanceObject = new object[] { "one",TestEnum.Two,3,4f,5.0, TestEnum2.Six  };

            ValidateLogic(instanceObject);

            var instanceDateTime = new DateTime[] { DateTime.Now, DateTime.UtcNow, DateTime.MinValue, DateTime.MaxValue };

            ValidateLogic(instanceDateTime);

            var instanceClass = new SimpleTestClass[2] { new SimpleTestClass() { SomeString = "1" }, new SimpleTestClass() { SomeString = "2" } };

            ValidateLogic(instanceDateTime);

            var instanceInterface = new ISimpleTestClass[2] { new SimpleTestClass() { SomeString = "1" }, new SimpleTestClass() { SomeString = "2" } };

            ValidateLogic(instanceInterface);
        }

        [TestMethod]
        public void Test_SomeClassWithInterfaceProp()
        {
            
            SomeClassWithInterfaceProp instance = new SomeClassWithInterfaceProp()
            { 
                SomeProp1 = new SomeClass() { KVPProp = new KeyValuePair<object, Enum>("1", TestEnum.One), SomeProp = "one" },
                SomeProp2 = new SomeClass() { KVPProp = new KeyValuePair<object, Enum>("2", TestEnum.Two), SomeProp = "2" },
                SomePropAlwaysNull = null
            };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_ISomeInterace()
        {
            ISomeInterace instance = new SomeClass() { KVPProp = new KeyValuePair<object, Enum>("wow", TestEnum.Three), SomeProp = "hi" };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_InheritedFromEnumerable()
        {
            Bister.Instance.DebugPath = @"C:\temp\serialize.cs";
            var instance = new InheritedFromEnumerable<int>() { 1, 2, 3 };
            instance.SomeProp = 4;


            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_InheritedFromList()
        {
            var instance = new InheritedFromList<int>() { 1,2,3};
            instance.SomeProp = 4;

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_InheritedFromDictionary()
        {
            var instance = new InheritedFromDictionary<string,int>() { { "1", 1 }, { "2", 2 },{ "3", 3 } };
            instance.SomeKey = "4";
            instance.SomeVal = 4;

            ValidateLogic(instance);
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
               {new ClassWithArrays(),"furth" },
               {1,string.Empty }
           };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_Exception()
        {
            Exception instance;
            try
            {
                throw new TestException();
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
        public void Test_KeyValuePair()
        {
            var instance = new KeyValuePair<Enum,object>(TestEnum.One, TestEnum.Two);

            ValidateLogic(instance);

            instance = new KeyValuePair<Enum, object>(TestEnum.Two, "Hellow");

            ValidateLogic(instance);

            var instance2 = new KeyValuePair<Enum, Dictionary<string,int>>(TestEnum.Two, new Dictionary<string, int>() { { "one", 1 }, { "two", 2 } });

            ValidateLogic(instance2);
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

            Bister.Instance.DebugPath = @"c:\temp";
            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_WrongTypeDeserialization()
        {
            var instance = new Vector<float>() {  X  = 1, Y = 2};

            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            Assert.ThrowsException<EndOfStreamException>(() => Bister.Instance.Deserialize<ClassWithArrays>(blob));
        }

        [TestMethod]
        public void Test_DynamicTypeDictionary()
        {
            var instance = new Dictionary<Enum, object>
            {
                { TestEnum.One, 1 },
                { TestEnum.Two, "2" },
                { TestEnum2.Five, 5f }
            };

            var blob = Bister.Instance.Serialize(instance);
            Assert.IsNotNull(blob);
            Assert.ThrowsException<EndOfStreamException>(() => Bister.Instance.Deserialize<ClassWithArrays>(blob));
        }

        [TestMethod]
        public void Test_ClassWithArrays()
        {
            var instance = new ClassWithArrays()
            {
                ArrayPropSystemEnum = [TestEnum.Three, TestEnum.Two, TestEnum.One],
                ArrayPropInt = [1, 2, 3, 4, 5],
                ArrayPropString = ["wow", "this", "is", "cool"],
                ArrayPropTestEnum = [TestEnum.One, TestEnum.Two, TestEnum.Three],
                ArrayPropDateTime = [new DateTime(), DateTime.Now, DateTime.UtcNow, DateTime.MinValue, DateTime.MaxValue, DateTime.FromOADate(0), DateTime.FromFileTime(0), DateTime.FromBinary(0), DateTime.FromBinary(123)],
                ArrayPropTimeSpan = [new TimeSpan(), TimeSpan.Zero, TimeSpan.MinValue, TimeSpan.MaxValue, DateTime.Now.TimeOfDay],
                ArrayPropSByte = [1, 2, 3, 4],
                ArrayPropDouble = [6.4, 9.4, 1.1, -99.223, Math.E, Math.PI],
                ArraySystemObjects = [1, 2.2f, DateTime.UtcNow, TimeSpan.Zero, TestEnum.One, TestEnum.Three],
                ArrayOfClasses = [ new ClassWithPrimitivesOnly() { PropTestEnum = TestEnum.One, Prop_bool = false, Prop_double = 1.0 }  ]
            };

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_SystemType()
        {
            Type instance = typeof(Type);

            ValidateLogic(instance);
        }

        [TestMethod]
        public void Test_SomeInheritedFromGeneric()
        {
            var instance = new SomeInheritedFromGeneric() {  SomeField = "wow", GenericVal = new ClassWithVector() {  Vec = new Vector<float>() {  X = 1, Y = 2} } };
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
        public void Test_ClassWithGenericLists()
        {
            var instance = new ClassWithGenericLists()
            {
                PropSystemEnum = [TestEnum.One, TestEnum.Two, TestEnum.Three],
                PropEnum = [TestEnum.One, TestEnum.Two, TestEnum.Three],
                PropFloat = [1f, 2f, 3f],
                PropStrings = ["1", "2", "3", string.Empty, null],
                PropInt = [1, 2, 3],
                PropSByte = [1, 2, 3],
                PropUint = [1,2,3]
            };
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
                TheObject = new ClassWithADictStringToInt() { Prop = new Dictionary<string, int>() { { "a", 1 }, { "b", 2 }, { "c", 3 } } }
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
        public void Test_GUID()
        {
            Guid instance = Guid.NewGuid();

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
        public void ValidateLogic<T>(T instance, bool isExpectingNull = false, bool compareJson = true)
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

                if (compareJson)
                {
                    string jsonOriginal = Newtonsoft.Json.JsonConvert.SerializeObject(instance);
                    string jsonCopy = Newtonsoft.Json.JsonConvert.SerializeObject(copyOfinstance);
                    
                    Assert.AreEqual(jsonOriginal, jsonCopy,"Json comparison failed");
                }
            }
            
        }

    }
}