using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.CodeFinder
{
    public class CSharpTests : CodeFinderUnitTestBase
    {
        public CSharpTests() : base(@"TestFiles\CSharp1.cs")
        {

        }

        [Fact]
        public void TestMatch1()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("sum = Add(sum, number)", 41, "Tests.Test1.Sum");
            ValidateMatch(matches, 41);
        }

        [Fact]
        public void TestMatch2()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("var diff = Sub(5, 3)", 50, "Tests.Test1.SubTests");
            ValidateMatch(matches, 53, 3);
        }

        [Fact]
        public void TestNoMatch()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("diff = Sub(3, 5)", 53, "Tests.Test1.SubTests");
            ValidateNoMatches(matches);
        }

        [Fact]
        public void TestMatchInConstructor1()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("Name = name", 65, "Tests.Test2.ctor");
            ValidateMatch(matches, 65);
        }

        [Fact]
        public void TestMatchInConstructor2()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("Name = name", 65, "Tests.Test2.Test2");
            ValidateMatch(matches, 65);
        }

        [Fact]
        public void TestMatchInConstructor3()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("Name = name", 74, "Tests.Test22.Test22");
            ValidateMatch(matches, 74);
        }

        [Fact]
        public void TestMatchInClass()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("string Name = new string(\"Test2\");", 61, "Tests.Test2.cctor");
            ValidateMatch(matches, 61);
        }

        [Fact]
        public void TestIgnoreTemplateTypes()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("flatList.AddRange(list)", 86, "Tests.Utils.FlattenList");
            ValidateMatch(matches, 86);
        }

        [Fact]
        public void TestIEnumerableMethod()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("yield return list[next]", 108, "Tests.Utils+_Randomize_d__24.MoveNext");
            ValidateMatch(matches, 108);
        }
    }
}
