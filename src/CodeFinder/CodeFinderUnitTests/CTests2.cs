using System;
using System.Collections.Generic;

using CodeFinder;
using CodeFinder.Internal.CStyle;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeFinderUnitTests
{
    [TestClass]
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

        [TestMethod]
        public void TestGetScopeSpan1()
        {
            var finder = new CppFinder(code1);
            var span = finder.GetScopeSpan(100);
            Assert.AreEqual(77, span.Start);
            Assert.AreEqual(130, span.End);
        }

        [TestMethod]
        public void TestGetScopeSpan2()
        {
            var finder = new CppFinder(code1);
            var span = finder.GetScopeSpan(49);
            Assert.AreEqual(30, span.Start);
            Assert.AreEqual(176, span.End);
        }

        [TestMethod]
        public void TestGetScopeSpan3()
        {
            var finder = new CppFinder(code1);
            var span = finder.GetScopeSpan(30);
            Assert.AreEqual(30, span.Start);
            Assert.AreEqual(176, span.End);
        }

        [TestMethod]
        public void TestGetScopeSpan4()
        {
            var finder = new CppFinder(code1);
            var span = finder.GetScopeSpan(176);
            Assert.AreEqual(30, span.Start);
            Assert.AreEqual(176, span.End);
        }

        [TestMethod]
        public void TestGetScopeSpanNull1()
        {
            var finder = new CppFinder(code1);
            var span = finder.GetScopeSpan(20);
            Assert.AreEqual(null, span);
        }

        [TestMethod]
        public void TestGetScopeSpanNull2()
        {
            var finder = new CppFinder(code1);
            var span = finder.GetScopeSpan(177);
            Assert.AreEqual(null, span);
        }

        [TestMethod]
        public void TestGetScopeSpanAtLine1()
        {
            var finder = new CppFinder(code1);
            var span = finder.GetScopeSpanAtLine(3);
            Assert.AreEqual(30, span.Start);
            Assert.AreEqual(176, span.End);
        }

        [TestMethod]
        public void TestGetScopeSpanAtLine2()
        {
            var finder = new CppFinder(code1);
            var span = finder.GetScopeSpanAtLine(5);
            Assert.AreEqual(77, span.Start);
            Assert.AreEqual(130, span.End);
        }

        [TestMethod]
        public void TestGetScopeIdentifiers1()
        {
            var finder = new CppFinder(code1);
            var span = finder.GetScopeSpanAtLine(3);
            var identifiers = finder.GetScopeIdentifiers(span.Start, out var isFunction);
            Assert.AreEqual(1, identifiers.Count);
            Assert.AreEqual("foo", identifiers[0]);
            Assert.AreEqual(true, isFunction);
        }

        [TestMethod]
        public void TestGetScopeIdentifiers2()
        {
            var finder = new CppFinder(code1);
            var span = finder.GetScopeSpanAtLine(5);
            var identifiers = finder.GetScopeIdentifiers(span.Start, out _);
            Assert.AreEqual(0, identifiers.Count);
        }

        [TestMethod]
        public void TestFindCode1()
        {
            var finder = new CppFinder(code1);
            var query = new MatchQuery("return true", 4, "foo", "0");
            var results = finder.FindMatches2(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(5, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [TestMethod]
        public void TestFindCodeNoScope1()
        {
            var finder = new CppFinder(code1);
            var query = new MatchQuery("return true", 4, "", "0");
            var results = finder.FindMatches2(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(5, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(-1, results[0].ScopeMatchDiff);
        }

        private const string code2 =
            @"void foo (int a)
            {
                if (a > 0) { return true; }
                return false;
            }";

        [TestMethod]
        public void TestGetScopeSpan5()
        {
            var finder = new CppFinder(code2);
            var span = finder.GetScopeSpan(62);
            Assert.AreEqual(60, span.Start);
            Assert.AreEqual(75, span.End);
        }

        [TestMethod]
        public void TestGetScopeSpan6()
        {
            var finder = new CppFinder(code2);
            var span = finder.GetScopeSpan(60);
            Assert.AreEqual(60, span.Start);
            Assert.AreEqual(75, span.End);
        }

        [TestMethod]
        public void TestGetScopeSpan7()
        {
            var finder = new CppFinder(code2);
            var span = finder.GetScopeSpan(76);
            Assert.AreEqual(30, span.Start);
            Assert.AreEqual(121, span.End);
        }

        [TestMethod]
        public void TestGetScopeSpanAtLine3()
        {
            var finder = new CppFinder(code2);
            var span = finder.GetScopeSpanAtLine(3);
            Assert.AreEqual(30, span.Start);
            Assert.AreEqual(121, span.End);
        }

        [TestMethod]
        public void TestFindCode2()
        {
            var finder = new CppFinder(code2);
            var query = new MatchQuery("return true", 3, "foo", "0");
            var results = finder.FindMatches2(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [TestMethod]
        public void TestFindCodeNoScope2()
        {
            var finder = new CppFinder(code2);
            var query = new MatchQuery("return true", 3, "", "0");
            var results = finder.FindMatches2(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(-1, results[0].ScopeMatchDiff);
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

        [TestMethod]
        public void TestFindCode3()
        {
            var finder = new CppFinder(code3);
            var query = new MatchQuery("a -= b", 7, "foo", "0");
            var results = finder.FindMatches2(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(7, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [TestMethod]
        public void TestFindCodeNoScope3()
        {
            var finder = new CppFinder(code3);
            var query = new MatchQuery("a -= b", 7, "", "0");
            var results = finder.FindMatches2(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(7, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(-1, results[0].ScopeMatchDiff);
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

        [TestMethod]
        public void TestFindCode4()
        {
            var finder = new CppFinder(code4);
            var query = new MatchQuery("b += a", 8, "foo", "0");
            var results = finder.FindMatches2(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(8, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [TestMethod]
        public void TestFindCodeNoScope4()
        {
            var finder = new CppFinder(code4);
            var query = new MatchQuery("b += a", 8, "", "0");
            var results = finder.FindMatches2(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(8, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(-1, results[0].ScopeMatchDiff);
        }

        [TestMethod]
        public void TestFindCode5()
        {
            var finder = new CppFinder(code4);
            var query = new MatchQuery("a > 0", 3, "foo", "0");
            var results = finder.FindMatches2(query);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);
            Assert.AreEqual(6, results[1].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(true, results[1].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
            Assert.AreEqual(0, results[1].ScopeMatchDiff);
        }

        [TestMethod]
        public void TestFindCodeNoScope5()
        {
            var finder = new CppFinder(code4);
            var query = new MatchQuery("a > 0", 3, "", "0");
            var results = finder.FindMatches2(query);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);
            Assert.AreEqual(6, results[1].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(true, results[1].ScopeChecked);
            Assert.AreEqual(-1, results[0].ScopeMatchDiff);
            Assert.AreEqual(-1, results[1].ScopeMatchDiff);
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

        [TestMethod]
        public void TestFindCode6()
        {
            var finder = new CppFinder(code5);
            var query = new MatchQuery("b += a", 9, "foo2", "0");
            var results = finder.FindMatches2(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(10, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }
    }
}
