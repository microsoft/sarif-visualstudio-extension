using FluentAssertions;

using Microsoft.Sarif.Viewer.CodeFinding;
using Microsoft.Sarif.Viewer.CodeFinding.Internal.CStyle;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.CodeFinding
{
    public class CTests2
    {
        private const string code1 =
            @"void foo (int a)
            {
                if (a > 0)
                {
                    return true;
                }
                return false;
            }";

        [Fact]
        public void TestGetScopeSpan1()
        {
            var finder = new CppFinder(code1);
            FileSpan span = finder.GetScopeSpan(100);
            77.Should().Be(span.Start);
            130.Should().Be(span.End);
        }

        [Fact]
        public void TestGetScopeSpan2()
        {
            var finder = new CppFinder(code1);
            FileSpan span = finder.GetScopeSpan(49);
            30.Should().Be(span.Start);
            176.Should().Be(span.End);
        }

        [Fact]
        public void TestGetScopeSpan3()
        {
            var finder = new CppFinder(code1);
            FileSpan span = finder.GetScopeSpan(30);
            30.Should().Be(span.Start);
            176.Should().Be(span.End);
        }

        [Fact]
        public void TestGetScopeSpan4()
        {
            var finder = new CppFinder(code1);
            FileSpan span = finder.GetScopeSpan(176);
            30.Should().Be(span.Start);
            176.Should().Be(span.End);
        }

        [Fact]
        public void TestGetScopeSpanNull1()
        {
            var finder = new CppFinder(code1);
            FileSpan span = finder.GetScopeSpan(20);
            span.Should().Be(null);
        }

        [Fact]
        public void TestGetScopeSpanNull2()
        {
            var finder = new CppFinder(code1);
            FileSpan span = finder.GetScopeSpan(177);
            span.Should().Be(null);
        }

        [Fact]
        public void TestGetScopeSpanAtLine1()
        {
            var finder = new CppFinder(code1);
            FileSpan span = finder.GetScopeSpanAtLine(3);
            30.Should().Be(span.Start);
            176.Should().Be(span.End);
        }

        [Fact]
        public void TestGetScopeSpanAtLine2()
        {
            var finder = new CppFinder(code1);
            FileSpan span = finder.GetScopeSpanAtLine(5);
            77.Should().Be(span.Start);
            130.Should().Be(span.End);
        }

        [Fact]
        public void TestGetScopeIdentifiers1()
        {
            var finder = new CppFinder(code1);
            FileSpan span = finder.GetScopeSpanAtLine(3);
            System.Collections.Generic.List<string> identifiers = finder.GetScopeIdentifiers(span.Start, out bool isFunction);
            1.Should().Be(identifiers.Count);
            "foo".Should().Be(identifiers[0]);
            true.Should().Be(isFunction);
        }

        [Fact]
        public void TestGetScopeIdentifiers2()
        {
            var finder = new CppFinder(code1);
            FileSpan span = finder.GetScopeSpanAtLine(5);
            System.Collections.Generic.List<string> identifiers = finder.GetScopeIdentifiers(span.Start, out _);
            0.Should().Be(identifiers.Count);
        }

        [Fact]
        public void TestFindCode1()
        {
            var finder = new CppFinder(code1);
            var query = new MatchQuery("return true", 4, "foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            1.Should().Be(results.Count);
            5.Should().Be(results[0].LineNumber);
            true.Should().Be(results[0].ScopeChecked);
            0.Should().Be(results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeNoScope1()
        {
            var finder = new CppFinder(code1);
            var query = new MatchQuery("return true", 4, "", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            1.Should().Be(results.Count);
            5.Should().Be(results[0].LineNumber);
            true.Should().Be(results[0].ScopeChecked);
            (-1).Should().Be(results[0].ScopeMatchDiff);
        }

        private const string code2 =
            @"void foo (int a)
            {
                if (a > 0) { return true; }
                return false;
            }";

        [Fact]
        public void TestGetScopeSpan5()
        {
            var finder = new CppFinder(code2);
            FileSpan span = finder.GetScopeSpan(62);
            60.Should().Be(span.Start);
            75.Should().Be(span.End);
        }

        [Fact]
        public void TestGetScopeSpan6()
        {
            var finder = new CppFinder(code2);
            FileSpan span = finder.GetScopeSpan(60);
            60.Should().Be(span.Start);
            75.Should().Be(span.End);
        }

        [Fact]
        public void TestGetScopeSpan7()
        {
            var finder = new CppFinder(code2);
            FileSpan span = finder.GetScopeSpan(76);
            30.Should().Be(span.Start);
            121.Should().Be(span.End);
        }

        [Fact]
        public void TestGetScopeSpanAtLine3()
        {
            var finder = new CppFinder(code2);
            FileSpan span = finder.GetScopeSpanAtLine(3);
            30.Should().Be(span.Start);
            121.Should().Be(span.End);
        }

        [Fact]
        public void TestFindCode2()
        {
            var finder = new CppFinder(code2);
            var query = new MatchQuery("return true", 3, "foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            1.Should().Be(results.Count);
            3.Should().Be(results[0].LineNumber);
            true.Should().Be(results[0].ScopeChecked);
            0.Should().Be(results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeNoScope2()
        {
            var finder = new CppFinder(code2);
            var query = new MatchQuery("return true", 3, "", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            1.Should().Be(results.Count);
            3.Should().Be(results[0].LineNumber);
            true.Should().Be(results[0].ScopeChecked);
            (-1).Should().Be(results[0].ScopeMatchDiff);
        }

        private const string code3 =
            @"int foo (int a, int b)
            {
                ASSERT(a > 0);

                do
                {
                    a -= b;
                } while (a > 0);

                return b;
            }";

        [Fact]
        public void TestFindCode3()
        {
            var finder = new CppFinder(code3);
            var query = new MatchQuery("a -= b", 7, "foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            1.Should().Be(results.Count);
            7.Should().Be(results[0].LineNumber);
            true.Should().Be(results[0].ScopeChecked);
            0.Should().Be(results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeNoScope3()
        {
            var finder = new CppFinder(code3);
            var query = new MatchQuery("a -= b", 7, "", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            1.Should().Be(results.Count);
            7.Should().Be(results[0].LineNumber);
            true.Should().Be(results[0].ScopeChecked);
            (-1).Should().Be(results[0].ScopeMatchDiff);
        }

        private const string code4 =
            @"int foo (int a)
            {
                ASSERT(a > 0);

                int b = 0;
                for (; a > 0; a--)
                {
                    b += a;
                }

                return b;
            }";

        [Fact]
        public void TestFindCode4()
        {
            var finder = new CppFinder(code4);
            var query = new MatchQuery("b += a", 8, "foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            1.Should().Be(results.Count);
            8.Should().Be(results[0].LineNumber);
            true.Should().Be(results[0].ScopeChecked);
            0.Should().Be(results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeNoScope4()
        {
            var finder = new CppFinder(code4);
            var query = new MatchQuery("b += a", 8, "", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            1.Should().Be(results.Count);
            8.Should().Be(results[0].LineNumber);
            true.Should().Be(results[0].ScopeChecked);
            (-1).Should().Be(results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCode5()
        {
            var finder = new CppFinder(code4);
            var query = new MatchQuery("a > 0", 3, "foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            2.Should().Be(results.Count);
            3.Should().Be(results[0].LineNumber);
            6.Should().Be(results[1].LineNumber);
            true.Should().Be(results[0].ScopeChecked);
            true.Should().Be(results[1].ScopeChecked);
            0.Should().Be(results[0].ScopeMatchDiff);
            0.Should().Be(results[1].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeNoScope5()
        {
            var finder = new CppFinder(code4);
            var query = new MatchQuery("a > 0", 3, "", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            2.Should().Be(results.Count);
            3.Should().Be(results[0].LineNumber);
            6.Should().Be(results[1].LineNumber);
            true.Should().Be(results[0].ScopeChecked);
            true.Should().Be(results[1].ScopeChecked);
            (-1).Should().Be(results[0].ScopeMatchDiff);
            (-1).Should().Be(results[1].ScopeMatchDiff);
        }

        private const string code5 =
            @"int foo2(int a)
            //int foo(int a, int c)
            {
                ASSERT(a > 0);

                int b = 0;
                for (; a > 0 /* && c > 0 */; a--)
                //while (c > 0)
                {
                    b += a;
                    //c--
                }

                return b;
            }";

        [Fact]
        public void TestFindCode6()
        {
            var finder = new CppFinder(code5);
            var query = new MatchQuery("b += a", 9, "foo2", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            results.Count.Should().Be(1);
            10.Should().Be(results[0].LineNumber);
            true.Should().Be(results[0].ScopeChecked);
            0.Should().Be(results[0].ScopeMatchDiff);
        }
    }
}
