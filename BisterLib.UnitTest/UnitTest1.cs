using EasyCFLib;

namespace BisterLib.UnitTest
{
    [TestClass]
    public class BisterUnitTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var obj = Enumerable.Range(1,1000).Select(i => (float)i).ToList();
            Bister.Instance.DebugPath = @"C:\temp\serialize.cs";
            var bister = Bister.Instance.GetSerializer<List<float>>();
            byte[] buf = bister.Serialize(obj);
            List<float> newObj = bister.Deserialize(buf);
        }
    }
}