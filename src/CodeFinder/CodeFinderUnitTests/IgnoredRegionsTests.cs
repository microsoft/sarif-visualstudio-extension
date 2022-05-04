﻿using CodeFinder.Internal.CStyle;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeFinderUnitTests
{
    [TestClass]
    public class IgnoredRegionsTests : CodeFinderUnitTestBase
    {
        public IgnoredRegionsTests() : base(@"TestFiles\CSharp2.cs")
        {
        }

        [TestMethod]
        public void TestMatch1()
        {
            var matches = GetMatches("return Name", 10, "MoreTests.Test1.Test.GetName");
            ValidateMatch(matches, 17, 7);
        }

        [TestMethod]
        public void TestMatch2()
        {
            var matches = GetMatches("return Name", 97, "MoreTests.Test5.Test.GetName");
            ValidateMatch(matches, 97);
        }

        [TestMethod]
        public void TestMatch3()
        {
            var matches = GetMatches("name = test.GetName()", 133, "MoreTests.Test.TestCommentsAndStrings");
            ValidateMatch(matches, 133);
        }

        [TestMethod]
        public void TestMatch4()
        {
            var matches = GetMatches("test.SetName(name)", 135, "MoreTests.Test.TestCommentsAndStrings");
            ValidateMatch(matches, 135);
        }

        [TestMethod]
        public void TestMatch5()
        {
            var matches = GetMatches("test.SetName(name)", 153, "MoreTests.Test.TestDoubleEscape");
            ValidateMatch(matches, 153);
        }

        [TestMethod]
        public void TestMatchInLineComment1()
        {
            var matches = GetMatches("return a + b + c", 32, "MoreTests.Test1.Test.Add");
            ValidateNoMatches(matches);
        }

        [TestMethod]
        public void TestMatchInLineComment2()
        {
            var matches = GetMatches("return Name4", 80, "MoreTests.Test4.Test.GetName");
            ValidateNoMatches(matches);
        }

        [TestMethod]
        public void TestMatchInBlockComment()
        {
            var matches = GetMatches("return Name3", 66, "MoreTests.Test3.Test.GetName");
            ValidateNoMatches(matches);
        }

        [TestMethod]
        public void TestSubstring1()
        {            
            var str = @"foo(var1, var2, /* var 3 */);";
            var finder = new CppFinder(str);

            // Comment in the middle of the substring.
            var substr = finder.Substring(0);
            Assert.AreEqual("foo(var1, var2, );", substr);

            // Substring ends in middle of comment.
            substr = finder.Substring(0, 20);
            Assert.AreEqual("foo(var1, var2, ", substr);

            // Substring starts at comment start.
            substr = finder.Substring(16);
            Assert.AreEqual(");", substr);

            // Substring starts at comment end.
            substr = finder.Substring(27);
            Assert.AreEqual(");", substr);

            // Substring starts within comment.
            substr = finder.Substring(20);
            Assert.AreEqual(");", substr);

            // Substring completely contains comment.
            substr = finder.Substring(16, 11);
            Assert.AreEqual("", substr);

            // Comment completely contains substring.
            substr = finder.Substring(18, 4);
            Assert.AreEqual("", substr);

            // Substring does not intersect with comment.
            substr = finder.Substring(4, 4);
            Assert.AreEqual("var1", substr);
        }

        [TestMethod]
        public void TestSubstring2()
        {
            var str =
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
            var substr = finder.Substring(62, 16);
            Assert.AreEqual("int product = 0;", substr);

            // Comment in the middle of the substring.
            substr = finder.Substring(62);
            Assert.AreEqual(
@"int product = 0;
    for (int i = 0; i < var2; i++)
    {
        product += var1;
    }

        return product;
}", substr);

            // 2 comments in the middle of the substring.
            substr = finder.Substring(0);
            Assert.AreEqual(
@"int Multiply(var1, var2)
{
        int product = 0;
    for (int i = 0; i < var2; i++)
    {
        product += var1;
    }

        return product;
}", substr);

        }

        // Verifies our ability to detect a character literal definition as an ignored region.
        [TestMethod]
        public void TestCharLiteral()
        {
            var str =
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

            var matches = finder.FindMatches2(new CodeFinder.MatchQuery("return a - b", 22));
            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual(22, matches[0].LineNumber);
            Assert.AreEqual(true, matches[0].ScopeChecked);
            Assert.AreEqual(-3, matches[0].ScopeMatchDiff);

            matches = finder.FindMatches2(new CodeFinder.MatchQuery("return a - b", 22, "Foo"));
            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual(22, matches[0].LineNumber);
            Assert.AreEqual(true, matches[0].ScopeChecked);
            Assert.AreEqual(-2, matches[0].ScopeMatchDiff);

            matches = finder.FindMatches2(new CodeFinder.MatchQuery("return a - b", 22, "MyClass::Foo"));
            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual(22, matches[0].LineNumber);
            Assert.AreEqual(true, matches[0].ScopeChecked);
            Assert.AreEqual(-1, matches[0].ScopeMatchDiff);

            matches = finder.FindMatches2(new CodeFinder.MatchQuery("return a - b", 22, "MyNamespace::MyClass::Foo"));
            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual(22, matches[0].LineNumber);
            Assert.AreEqual(true, matches[0].ScopeChecked);
            Assert.AreEqual(0, matches[0].ScopeMatchDiff);
        }
    }
}
