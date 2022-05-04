using CodeFinder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CodeFinderUnitTests
{
    [TestClass]
    public class CTests : CodeFinderUnitTestBase
    {
        public CTests() : base(@"TestFiles\C1.c")
        {
        }

        [TestMethod]
        public void TestMatch1()
        {
            var matches = GetMatches("return a + b", 6, "Add2");
            ValidateMatch(matches, 6);
        }

        [TestMethod]
        public void TestMatch2()
        {
            var matches = GetMatches("sum += numbers[i]", 14, "AddAll");
            ValidateMatch(matches, 17, 3);
        }

        [TestMethod]
        public void TestNoScopeMatch1()
        {
            var matches = GetMatches("return a + b + c", 10, "");
            ValidateMatch(matches, 10, 0, expectedScopeMatchDiff: -1);
        }

        [TestMethod]
        public void TestNoScopeMatch2()
        {
            var matches = GetMatches("return a / b", 38, "");

            ValidateMatch(matches, 40, 2, expectedScopeMatchDiff: -1);
        }

        [TestMethod]
        public void TestNoScopeMatch3()
        {
            var expectedMatches = new List<MatchResult>();
            expectedMatches.Add(new MatchResult(
                "0", null, 6, 4, true, -1
            ));
            expectedMatches.Add(new MatchResult(
                "0", null, 10, 0, true, -1
            ));
            expectedMatches.Add(new MatchResult(
                "0", null, 50, 40, true, -1
            ));

            var matches = GetMatches("return a + b", 10, "");
            ValidateMatches(matches, expectedMatches);
        }

        [TestMethod]
        public void TestPragmasAreIgnored()
        {
            var matches = GetMatches("return a * b", 28, "Multiply");
            ValidateMatch(matches, 28);
        }

        [TestMethod]
        public void TestMatchFunctionDefinition1()
        {
            var matches = GetMatches("AddAll", 13, "", MatchQuery.MatchTypeHint.Function);
            ValidateMatch(matches, 13);
        }

        [TestMethod]
        public void TestMatchFunctionDefinition2()
        {
            var matches = GetMatches("Add2", 5, "", MatchQuery.MatchTypeHint.Function);
            ValidateMatch(matches, 5);
        }

        // TODO: This currently fails because we don't detect when extra, un-matched curly braces are introduced because of #if/#else blocks.
        // Tracked by https://dev.azure.com/microsoft/OS/_workitems/edit/23997257
        //[TestMethod]
        public void TestExtraCurlyBraceInIfMacro()
        {
            var matches = GetMatches("return a / b", 40, "Divide");
            ValidateMatch(matches, 40);
        }

        [TestMethod]
        public void TestMatchWithStringLiteral1()
        {
            var matches = GetMatches("return GetSetting(\"MySettings.ThingEnabled\");", 62, "IsThingEnabled");
            ValidateMatch(matches, 62);
        }

        [TestMethod]
        public void TestMatchWithStringLiteral2()
        {
            var matches = GetMatches("return GetSetting(\"MySettings.ThingEnabled\");", 62, "");
            ValidateMatch(matches, 62, expectedScopeMatchDiff: -1);
        }

        [TestMethod]
        public void TestMatchWithStringLiteral3()
        {
            var matches = GetMatches("MySettings.ThingEnabled", 62, "IsThingEnabled");
            ValidateMatch(matches, 62);
        }

        [TestMethod]
        public void TestMatchWithStringLiteral4()
        {
            var matches = GetMatches("MySettings.ThingEnabled", 62, "");
            ValidateMatch(matches, 62, expectedScopeMatchDiff: -1);
        }
    }
}
