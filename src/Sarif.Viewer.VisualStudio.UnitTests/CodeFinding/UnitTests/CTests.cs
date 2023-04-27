using System.Collections.Generic;

using Microsoft.Sarif.Viewer.CodeFinding;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.CodeFinding
{
    public class CTests : CodeFinderUnitTestBase
    {
        public CTests() : base(@"TestFiles\C1.c")
        {
        }

        [Fact]
        public void TestMatch1()
        {
            List<MatchResult> matches = GetMatches("return a + b", 6, "Add2");
            ValidateMatch(matches, 6);
        }

        [Fact]
        public void TestMatch2()
        {
            List<MatchResult> matches = GetMatches("sum += numbers[i]", 14, "AddAll");
            ValidateMatch(matches, 17, 3);
        }

        [Fact]
        public void TestNoScopeMatch1()
        {
            List<MatchResult> matches = GetMatches("return a + b + c", 10, "");
            ValidateMatch(matches, 10, 0, expectedScopeMatchDiff: -1);
        }

        [Fact]
        public void TestNoScopeMatch2()
        {
            List<MatchResult> matches = GetMatches("return a / b", 38, "");

            ValidateMatch(matches, 40, 2, expectedScopeMatchDiff: -1);
        }

        [Fact]
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

            List<MatchResult> matches = GetMatches("return a + b", 10, "");
            ValidateMatches(matches, expectedMatches);
        }

        [Fact]
        public void TestPragmasAreIgnored()
        {
            List<MatchResult> matches = GetMatches("return a * b", 28, "Multiply");
            ValidateMatch(matches, 28);
        }

        [Fact]
        public void TestMatchFunctionDefinition1()
        {
            List<MatchResult> matches = GetMatches("AddAll", 13, "", MatchQuery.MatchTypeHint.Function);
            ValidateMatch(matches, 13);
        }

        [Fact]
        public void TestMatchFunctionDefinition2()
        {
            List<MatchResult> matches = GetMatches("Add2", 5, "", MatchQuery.MatchTypeHint.Function);
            ValidateMatch(matches, 5);
        }

        [Fact]
        public void TestMatchWithStringLiteral1()
        {
            List<MatchResult> matches = GetMatches("return GetSetting(\"MySettings.ThingEnabled\");", 62, "IsThingEnabled");
            ValidateMatch(matches, 62);
        }

        [Fact]
        public void TestMatchWithStringLiteral2()
        {
            List<MatchResult> matches = GetMatches("return GetSetting(\"MySettings.ThingEnabled\");", 62, "");
            ValidateMatch(matches, 62, expectedScopeMatchDiff: -1);
        }

        [Fact]
        public void TestMatchWithStringLiteral3()
        {
            List<MatchResult> matches = GetMatches("MySettings.ThingEnabled", 62, "IsThingEnabled");
            ValidateMatch(matches, 62);
        }

        [Fact]
        public void TestMatchWithStringLiteral4()
        {
            List<MatchResult> matches = GetMatches("MySettings.ThingEnabled", 62, "");
            ValidateMatch(matches, 62, expectedScopeMatchDiff: -1);
        }
    }
}
