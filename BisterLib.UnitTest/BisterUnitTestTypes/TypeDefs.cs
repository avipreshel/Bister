namespace BisterUnitTestTypes
{
    public enum TestEnum : ushort
    {
        One, Two, Three
    }

    public enum TestEnum2 : ushort
    {
        Four, Five, Six
    }

    public struct Vector<T> where T : struct
    {
        public T X { get; set; }
        public T Y { get; set; }
    }

    public struct Vector
    {
        public float X { get; set; }
        public double Y { get; set; }
        public byte Z { get; set; }
    }

    public class SomeGenericClass<T> where T : class
    {
        public T? GenericVal { get; set; } = null;
    }

    public class ClassWithVector
    {
        public Vector Vec { get; set; }
    }
}
