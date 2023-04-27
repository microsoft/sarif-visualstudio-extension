using System.Collections.Generic;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.CodeFinder
{
    public class CppTests : CodeFinderUnitTestBase
    {
        public CppTests() : base(@"TestFiles\Cpp1.cpp")
        {
        }

        [Fact]
        public void TestMatch1()
        {
            List<MatchResult> matches = GetMatches("sum += Numbers[i]", 40, "TextMatcherTest::Test1::AddMore");
            ValidateMatch(matches, 40, expectedScopeMatchDiff: 1);
        }

        [Fact]
        public void TestMatch2()
        {
            List<MatchResult> matches = GetMatches("return a + b", 15, "TextMatcherTest::Test1::Add2");
            ValidateMatch(matches, 19, 4, expectedScopeMatchDiff: 1);
        }

        [Fact]
        public void TestMatchWithNoNamespaceInFunctionSignature()
        {
            List<MatchResult> matches = GetMatches("return a + b + c", 28, "Test1::Add3");
            ValidateMatch(matches, 28);
        }

        [Fact]
        public void TestNoMatch()
        {
            List<MatchResult> matches = GetMatches("return a + b + c", 28, "TextMatcherTest::Test1::Add2");
            ValidateNoMatches(matches);
        }

        [Fact]
        public void TestNoScopeMatch1()
        {
            List<MatchResult> matches = GetMatches("return a + b + c", 28, "");
            ValidateMatch(matches, 28, 0, expectedScopeMatchDiff: -2);
        }

        [Fact]
        public void TestNoScopeMatch2()
        {
            var expectedMatches = new List<MatchResult>
            {
                new MatchResult(
                "0", null, 19, 1, true, -2
            ),
                new MatchResult(
                "0", null, 28, 8, true, -2
            ),
                new MatchResult(
                "0", null, 134, 114, true, -3
            )};

            List<MatchResult> matches = GetMatches("return a + b", 20, "");
            ValidateMatches(matches, expectedMatches);
        }

        [Fact]
        public void TestNoScopeMatch3()
        {
            var expectedMatches = new List<MatchResult>
            {
                new MatchResult(
                "0", null, 19, 1, true, -2
            ),
                new MatchResult(
                "0", null, 134, 114, true, -3
            )};

            List<MatchResult> matches = GetMatches("return a + b;", 20, "");
            ValidateMatches(matches, expectedMatches);
        }

        [Fact]
        public void TestNoScopeMatch4()
        {
            List<MatchResult> matches = GetMatches("return a + b + c", 28, "Add3");
            ValidateMatch(matches, 28, 0, expectedScopeMatchDiff: -1);
        }

        [Fact]
        public void TestMatchInConstructor1()
        {
            List<MatchResult> matches = GetMatches("this.Name", 11, "TextMatcherTest::Test1::Test1");
            ValidateMatch(matches, 11, expectedScopeMatchDiff: 1);
        }

        [Fact]
        public void TestMatchInConstructor2()
        {
            List<MatchResult> matches = GetMatches("this.Name", 11, "TextMatcherTest::Test1::ctor");
            ValidateMatch(matches, 11, expectedScopeMatchDiff: 1);
        }

        [Fact]
        public void TestMatchInConstructor3()
        {
            List<MatchResult> matches = GetMatches("this.Name", 11, "TextMatcherTest::Test1::{ctor}");
            ValidateMatch(matches, 11, expectedScopeMatchDiff: 1);
        }

        [Fact]
        public void TestMatchInConstructor4()
        {
            List<MatchResult> matches = GetMatches("this.Name", 11, "TextMatcherTest::Test1..ctor");
            ValidateMatch(matches, 11, expectedScopeMatchDiff: 1);
        }

        [Fact]
        public void TestMatchInConstructor5()
        {
            List<MatchResult> matches = GetMatches("this.Name", 11, "TextMatcherTest::Test1..cctor");
            ValidateMatch(matches, 11, expectedScopeMatchDiff: 1);
        }

        [Fact]
        public void TestMatchInConstructor6()
        {
            List<MatchResult> matches = GetMatches("this.name = name;", 201, "Dispatcher::Dispatcher");
            ValidateMatch(matches, 201);
        }

        [Fact]
        public void TestMatchInDestructor1()
        {
            List<MatchResult> matches = GetMatches("free(this.Name)", 87, "CppTest::dtor");
            ValidateMatch(matches, 87);
        }

        [Fact]
        public void TestMatchInDestructor2()
        {
            List<MatchResult> matches = GetMatches("free(this.Name)", 87, "CppTest::{dtor}");
            ValidateMatch(matches, 87);
        }

        [Fact]
        public void TestMatchInDestructor3()
        {
            List<MatchResult> matches = GetMatches("free(this.Name)", 87, "CppTest::_CppTest");
            ValidateMatch(matches, 87);
        }

        [Fact]
        public void TestMatchInSameFunctionNameDifferentSignature()
        {
            List<MatchResult> matches = GetMatches("return a - b - c", 60, "TextMatcherTest::Test1::Sub");
            ValidateMatch(matches, 60, expectedScopeMatchDiff: 1);
        }

        [Fact]
        public void TestMatchInClassImplementation1()
        {
            List<MatchResult> matches = GetMatches("this.Name = name", 76, "CppTest::CppTest");
            ValidateMatch(matches, 83, 7);
        }

        [Fact]
        public void TestMatchInClassImplementation2()
        {
            List<MatchResult> matches = GetMatches("return a * b * c", 95, "CppTest::Multiply3");
            ValidateMatch(matches, 95);
        }

        [Fact]
        public void TestMatchInClassDeclaration()
        {
            List<MatchResult> matches = GetMatches("this.Name = name", 77, "CppTest::Rename");
            ValidateMatch(matches, 77);
        }

        [Fact]
        public void TestFunctionSignatureFunctionPrototype()
        {
            List<MatchResult> matches = GetMatches("sum += Numbers[i]", 40, "int Test1::AddMore(int[] Numbers, int Count)");
            ValidateMatch(matches, 40);
        }

        [Fact]
        public void TestFunctionSignatureWithDots()
        {
            List<MatchResult> matches = GetMatches("return a * b * c", 95, "CppTest.CppTest.Multiply3");
            ValidateMatch(matches, 95, expectedScopeMatchDiff: 1);
        }

        [Fact]
        public void TestFunctionSignatureWithCatch1()
        {
            List<MatchResult> matches = GetMatches("product = -1", 107, "`CppTest::Multiply'::`1'::catch$");
            ValidateMatch(matches, 107);
        }

        [Fact]
        public void TestFunctionSignatureWithCatch2()
        {
            List<MatchResult> matches = GetMatches("product = -1", 107, "CppTest::Multiply$catch$0");
            ValidateMatch(matches, 107);
        }

        [Fact]
        public void TestFunctionSignatureWithCatch3()
        {
            List<MatchResult> matches = GetMatches("product = -1", 107, "_CppTest::Multiply_::_1_::catch$0");
            ValidateMatch(matches, 107);
        }

        [Fact]
        public void TestFunctionSignatureLambda1()
        {
            List<MatchResult> matches = GetMatches("func(batch)", 121, "BatchManager::EnumerateBatchesAndExecute__lambda_1b1f5ee28e310718866d896377259c1c___");
            ValidateMatch(matches, 121);
        }

        [Fact]
        public void TestFunctionSignatureLambda2()
        {
            List<MatchResult> matches = GetMatches("func(batch)", 121, "BatchManager::EnumerateBatchesAndExecute::<lambda>");
            ValidateMatch(matches, 121);
        }

        [Fact]
        public void TestFunctionSignatureLambda3()
        {
            List<MatchResult> matches = GetMatches("func(batch)", 121, "BatchManager::EnumerateBatchesAndExecute::__l2::<lambda>");
            ValidateMatch(matches, 121);
        }

        [Fact]
        public void TestFunctionSignatureLambda4()
        {
            List<MatchResult> matches = GetMatches("func(batch)", 121, "BatchManager::EnumerateBatchesAndExecute::_1_::__lambda_1b1f5ee28e310718866d896377259c1c___");
            ValidateMatch(matches, 121);
        }

        [Fact]
        public void TestFunctionSignatureLambda5()
        {
            List<MatchResult> matches = GetMatches("func(batch)", 121, "__lambda_0106e450a10813615e1ed095ab5424f8_::operator");
            ValidateMatch(matches, 121, expectedScopeMatchDiff: -2);
        }

        [Fact]
        public void TestFunctionSignatureLambda6()
        {
            List<MatchResult> matches = GetMatches("func(batch)", 121, "<lambda>");
            ValidateMatch(matches, 121, expectedScopeMatchDiff: -2);
        }

        [Fact]
        public void TestFunctionSignatureWithBrackets()
        {
            List<MatchResult> matches = GetMatches("sum += numbers[i];", 140, "System::Math::Adder::[System::Math::__IAdderPotectedNonVirtuals]::Add");
            ValidateMatch(matches, 140);
        }

        [Fact]
        public void TestInvalidFunctionSignature()
        {
            List<MatchResult> matches = GetMatches("sum += Numbers[i]", 40, "??@0a21757b16f2b53fd81e49741b88b3e9");
            ValidateMatch(matches, 40, expectedScopeMatchDiff: -2);
        }

        [Fact]
        public void TestFunctionSignatureWithAnonymousNamespace1()
        {
            List<MatchResult> matches = GetMatches("return a * b * c", 95, "_anonymous_namespace_::CppTest::CppTest::Multiply3");
            ValidateMatch(matches, 95, expectedScopeMatchDiff: 1);
        }

        [Fact]
        public void TestFunctionSignatureWithAnonymousNamespace2()
        {
            List<MatchResult> matches = GetMatches("return (number % 2 == 0);", 232, "_anonymous_namespace_::IsEven");
            ValidateMatch(matches, 232);
        }

        [Fact]
        public void TestMatchFunctionDefinition1()
        {
            List<MatchResult> matches = GetMatches("AddMore", 32, "Test1::AddMore", MatchQuery.MatchTypeHint.Function);
            ValidateMatch(matches, 32);
        }

        [Fact]
        public void TestMatchFunctionDefinition2()
        {
            List<MatchResult> matches = GetMatches("AddMore", 32, "Test1", MatchQuery.MatchTypeHint.Function);
            ValidateMatch(matches, 32);
        }

        [Fact]
        public void TestMatchFunctionDefinition3()
        {
            List<MatchResult> matches = GetMatches("AddMore", 32, "", MatchQuery.MatchTypeHint.Function);
            ValidateMatch(matches, 32, expectedScopeMatchDiff: -1);
        }

        [Fact]
        public void TestMatchFunctionDefinition4()
        {
            // There are 2 instances of Test1::Sub (with different signatures).

            var expectedMatches = new List<MatchResult>
            {
                new MatchResult(
                "0", null, 46, 0, true, 0
            ),
                new MatchResult(
                "0", null, 54, 8, true, 0
            )};

            List<MatchResult> matches = GetMatches("Sub", 46, "Test1::Sub", MatchQuery.MatchTypeHint.Function);
            ValidateMatches(matches, expectedMatches);
        }

        [Fact]
        public void TestMatchNestedNamespace()
        {
            List<MatchResult> matches = GetMatches("sum += numbers[i];", 140, "System::Math::Adder::Add");
            ValidateMatch(matches, 140);
        }

        [Fact]
        public void TestMatchWithTryInFunctionDefinition()
        {
            List<MatchResult> matches = GetMatches("*quotient = a / b", 153, "System::Math::Divider::Divide");
            ValidateMatch(matches, 153);
        }

        [Fact]
        public void TestMatchInClassWithComplexInheritance()
        {
            List<MatchResult> matches = GetMatches("reinterpret_cast<TimerBase>(Context)->DoStuff()", 173, "TimerBase::TimerCallback");
            ValidateMatch(matches, 173);
        }

        [Fact]
        public void TestMatchInInitializationList()
        {
            List<MatchResult> matches = GetMatches("Callback()", 164, "TimerBase::TimerBase");
            ValidateMatch(matches, 164, expectedScopeMatchDiff: 1);
        }

        [Fact]
        public void TestMatchInStruct1()
        {
            List<MatchResult> matches = GetMatches("dispatcherHandle = handle;", 187, "ThreadDispatcher::Initialize");
            ValidateMatch(matches, 187);
        }

        [Fact]
        public void TestMatchInStruct2()
        {
            List<MatchResult> matches = GetMatches("dispatcherHandle = nullptr;", 216, "Dispatcher::SensorDispatcher::Cleanup");
            ValidateMatch(matches, 216);
        }

        [Fact]
        public void TestMatchInClass()
        {
            List<MatchResult> matches = GetMatches("std::string name = DISPATCHER_NAME_DEFAULT;", 220, "Dispatcher");
            ValidateMatch(matches, 220);
        }

        [Fact]
        public void TestMatchInFunctionReturningPointer()
        {
            List<MatchResult> matches = GetMatches("return new Dispatcher();", 225, "DispatcherFactory::CreateDispatcher");
            ValidateMatch(matches, 225);
        }

        // TODO: Currently fails because we're not sure how to tell when a function signature has a single
        // template type embedded vs. a name that includes underscores.
        // Tracked by https://dev.azure.com/microsoft/OS/_workitems/edit/24051839
        //[Fact]
        public void TestMatchInTemplateFunction1()
        {
            List<MatchResult> matches = GetMatches("if (items[i] == toFind)", 245, "TemplateTest::Test1::Find_int_");
            ValidateMatch(matches, 245);
        }

        [Fact]
        public void TestMatchInTemplateFunction2()
        {
            List<MatchResult> matches = GetMatches("if (items[i] == toFind)", 259, "TemplateTest::Test1::FindAndCallback_int,IntCallback_");
            ValidateMatch(matches, 259);
        }
    }
}
