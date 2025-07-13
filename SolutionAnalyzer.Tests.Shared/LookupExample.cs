namespace SolutionAnalyzer.Tests.Shared
{
    public class LookupExample
    {
        // non instance fields
        public const string ConstName = "NameConst";
        public static readonly bool StaticBool = true;

        public LookupExample()
        {
            // just to have real references
            MyPrivateMethod();
            MyStaticMethod();
        }

        // props
        public int Index { get; set; }
        public float X { get; set; }

        // basic methods
        private void MyPrivateMethod() { }
        public void MyMethod() { }
        public static void MyStaticMethod() { }
    }
}
