using Microsoft.Sarif.Viewer.CodeFinding.Internal.CStyle;
using Microsoft.Sarif.Viewer.CodeFinding;

using Xunit;
using FluentAssertions;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.CodeFinding
{
    public class IgnoredRegionsTests : CodeFinderUnitTestBase
    {
        public IgnoredRegionsTests() : base(@"CodeFinding\TestFiles\CSharp2.cs")
        {
        }

        [Fact]
        public void TestMatch1()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("return Name", 10, "MoreTests.Test1.Test.GetName");
            ValidateMatch(matches, 17, 7);
        }

        [Fact]
        public void TestMatch2()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("return Name", 97, "MoreTests.Test5.Test.GetName");
            ValidateMatch(matches, 97);
        }

        [Fact]
        public void TestMatch3()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("name = test.GetName()", 133, "MoreTests.Test.TestCommentsAndStrings");
            ValidateMatch(matches, 133);
        }

        [Fact]
        public void TestMatch4()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("test.SetName(name)", 135, "MoreTests.Test.TestCommentsAndStrings");
            ValidateMatch(matches, 135);
        }

        [Fact]
        public void TestMatch5()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("test.SetName(name)", 153, "MoreTests.Test.TestDoubleEscape");
            ValidateMatch(matches, 153);
        }

        [Fact]
        public void TestMatchInLineComment1()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("return a + b + c", 32, "MoreTests.Test1.Test.Add");
            ValidateNoMatches(matches);
        }

        [Fact]
        public void TestMatchInLineComment2()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("return Name4", 80, "MoreTests.Test4.Test.GetName");
            ValidateNoMatches(matches);
        }

        [Fact]
        public void TestMatchInBlockComment()
        {
            System.Collections.Generic.List<MatchResult> matches = GetMatches("return Name3", 66, "MoreTests.Test3.Test.GetName");
            ValidateNoMatches(matches);
        }

        [Fact]
        public void TestSubstring1()
        {
            string str = @"foo(var1, var2, /* var 3 */);";
            var finder = new CppFinder(str);

            // Comment in the middle of the substring.
            string substr = finder.Substring(0);
            "foo(var1, var2, );".Should().Be(substr);

            // Substring ends in middle of comment.
            substr = finder.Substring(0, 20);
            "foo(var1, var2, ".Should().Be(substr);

            // Substring starts at comment start.
            substr = finder.Substring(16);
            ");".Should().Be(substr);

            // Substring starts at comment end.
            substr = finder.Substring(27);
            ");".Should().Be(substr);

            // Substring starts within comment.
            substr = finder.Substring(20);
            ");".Should().Be(substr);

            // Substring completely contains comment.
            substr = finder.Substring(16, 11);
            "".Should().Be(substr);

            // Comment completely contains substring.
            substr = finder.Substring(18, 4);
            "".Should().Be(substr);

            // Substring does not intersect with comment.
            substr = finder.Substring(4, 4);
            "var1".Should().Be(substr);
        }

        [Fact]
        public void TestSubstring2()
        {
            string str =
@"int Multiply(var1, var2)
{
    // Add var1 var2 times.
    int product = 0;
    for (int i = 0; i < var2; i++)
    {
        product += var1;
    }

    // Return the product.
    return product;
}";
            var finder = new CppFinder(str);

            // Substring after comment.
            string substr = finder.Substring(62, 16);
            "int product = 0;".Should().Be(substr);

            // Comment in the middle of the substring.
            substr = finder.Substring(62);
            @"int product = 0;
    for (int i = 0; i < var2; i++)
    {
        product += var1;
    }

        return product;
}".Should().Be(substr);

            // 2 comments in the middle of the substring.
            substr = finder.Substring(0);
            @"int Multiply(var1, var2)
{
        int product = 0;
    for (int i = 0; i < var2; i++)
    {
        product += var1;
    }

        return product;
}".Should().Be(substr);

        }

        // Verifies our ability to detect a character literal definition as an ignored region.
        [Fact]
        public void TestCharLiteral()
        {
            string str =
@"namespace MyNamespace
{
    wstring MyClass::Bracketize(wstring id)
    {
        wstring newId;
        if (id.front() != L'{')
        {
            newId = L""{"" + id + ""}"";
        }
        else
        {
            newId = id;
        }

        return newId;
    }

    int MyClass::Foo(int a, int b)
    {
        if (a > b)
        {
            return a - b;
        }
        else
        {
            return b - a;
        }
    }
}";
            var finder = new CppFinder(str);

            // If we aren't detecting character literals correctly then the ScopeMatchDiff returned will be incorrect.
            // The curly brace character (L'{') in the code above will appear to be a legitimate curly brace and this
            // will confuse the scope detection logic.

            System.Collections.Generic.List<MatchResult> matches = finder.FindMatchesWithFunction(new MatchQuery("return a - b", 22));
            1.Should().Be(matches.Count);
            22.Should().Be(matches[0].LineNumber);
            true.Should().Be(matches[0].ScopeChecked);
            (-3).Should().Be(matches[0].ScopeMatchDiff);

            matches = finder.FindMatchesWithFunction(new MatchQuery("return a - b", 22, "Foo"));
            1.Should().Be(matches.Count);
            22.Should().Be(matches[0].LineNumber);
            true.Should().Be(matches[0].ScopeChecked);
            (-2).Should().Be(matches[0].ScopeMatchDiff);

            matches = finder.FindMatchesWithFunction(new MatchQuery("return a - b", 22, "MyClass::Foo"));
            1.Should().Be(matches.Count);
            22.Should().Be(matches[0].LineNumber);
            true.Should().Be(matches[0].ScopeChecked);
            (-1).Should().Be(matches[0].ScopeMatchDiff);

            matches = finder.FindMatchesWithFunction(new MatchQuery("return a - b", 22, "MyNamespace::MyClass::Foo"));
            1.Should().Be(matches.Count);
            22.Should().Be(matches[0].LineNumber);
            true.Should().Be(matches[0].ScopeChecked);
            0.Should().Be(matches[0].ScopeMatchDiff);
        }
    }
}
