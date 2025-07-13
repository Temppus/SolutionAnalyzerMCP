namespace SolutionAnalyzer.Tests.Shared
{
    public class LookupExample
    {
        // non instance fields
        public const string Name = "NameConst";
        public static readonly bool IsStatic = true;

        // props
        public int Index { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        // basic methods
        private void MyPrivateMethod() { }
        public void MyMethod() { }
        public static void MyStaticMethod() { }
    }
}
