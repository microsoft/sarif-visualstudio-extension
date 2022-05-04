using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeFinderUnitTests
{
    [TestClass]
    public class CSharpTests : CodeFinderUnitTestBase
    {
        public CSharpTests() : base(@"TestFiles\CSharp1.cs")
        {

        }

        [TestMethod]
        public void TestMatch1()
        {
            var matches = GetMatches("sum = Add(sum, number)", 41, "Tests.Test1.Sum");
            ValidateMatch(matches, 41);
        }

        [TestMethod]
        public void TestMatch2()
        {
            var matches = GetMatches("var diff = Sub(5, 3)", 50, "Tests.Test1.SubTests");
            ValidateMatch(matches, 53, 3);
        }

        [TestMethod]
        public void TestNoMatch()
        {
            var matches = GetMatches("diff = Sub(3, 5)", 53, "Tests.Test1.SubTests");
            ValidateNoMatches(matches);
        }

        [TestMethod]
        public void TestMatchInConstructor1()
        {
            var matches = GetMatches("Name = name", 65, "Tests.Test2.ctor");
            ValidateMatch(matches, 65);
        }

        [TestMethod]
        public void TestMatchInConstructor2()
        {
            var matches = GetMatches("Name = name", 65, "Tests.Test2.Test2");
            ValidateMatch(matches, 65);
        }

        [TestMethod]
        public void TestMatchInConstructor3()
        {
            var matches = GetMatches("Name = name", 74, "Tests.Test22.Test22");
            ValidateMatch(matches, 74);
        }

        [TestMethod]
        public void TestMatchInClass()
        {
            var matches = GetMatches("string Name = new string(\"Test2\");", 61, "Tests.Test2.cctor");
            ValidateMatch(matches, 61);
        }

        [TestMethod]
        public void TestIgnoreTemplateTypes()
        {
            var matches = GetMatches("flatList.AddRange(list)", 86, "Tests.Utils.FlattenList");
            ValidateMatch(matches, 86);
        }

        [TestMethod]
        public void TestIEnumerableMethod()
        {
            var matches = GetMatches("yield return list[next]", 108, "Tests.Utils+_Randomize_d__24.MoveNext");
            ValidateMatch(matches, 108);
        }
    }
}
