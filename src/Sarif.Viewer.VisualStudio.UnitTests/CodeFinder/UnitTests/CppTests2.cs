using Microsoft.Sarif.Viewer.CodeFinder;
using Microsoft.Sarif.Viewer.CodeFinding.Internal.CStyle;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.CodeFinder
{
    public class CppTests2
    {
        private const string code1 =
            @"int MyClass::Foo (int a)
            {
                int sum = 0;
                for (int i = 0; i < a; i++)
                {
                    sum += i;
                }
                return sum;
            }";

        [Fact]
        public void TestGetScopeIdentifiers1()
        {
            // Verify that we can identify the containing scope for the code within a member function.
            var finder = new CppFinder(code1);
            FileSpan span = finder.GetScopeSpanAtLine(3);
            System.Collections.Generic.List<string> identifiers = finder.GetScopeIdentifiers(span.Start, out bool isFunction);
            Assert.AreEqual(2, identifiers.Count);
            Assert.AreEqual("Foo", identifiers[0]);
            Assert.AreEqual("MyClass", identifiers[1]);
            Assert.AreEqual(true, isFunction);
        }

        [Fact]
        public void TestFindCodeInMemberFunction1()
        {
            // Make sure we can find the code in a member function when the 
            // function is prefixed with the class.
            var finder = new CppFinder(code1);
            var query = new MatchQuery("sum += i", 6, "MyClass::Foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(6, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeNoScope1()
        {
            // Make sure we can find the code in a member function when the 
            // function is prefixed with the class.
            var finder = new CppFinder(code1);
            var query = new MatchQuery("sum += i", 6, "", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(6, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(-2, results[0].ScopeMatchDiff);
        }

        private const string code2 =
            @"double Class1::Foo (double a)
            {
                return a + a;
            }

            int Class2::Foo (int a)
            {
                return a + a;
            }";

        [Fact]
        public void TestGetScopeIdentifiers2()
        {
            // Verify that we can identify the containing scope for the code within a member function.
            var finder = new CppFinder(code2);
            FileSpan span = finder.GetScopeSpanAtLine(3);
            System.Collections.Generic.List<string> identifiers = finder.GetScopeIdentifiers(span.Start, out bool isFunction);
            Assert.AreEqual(2, identifiers.Count);
            Assert.AreEqual("Foo", identifiers[0]);
            Assert.AreEqual("Class1", identifiers[1]);
            Assert.AreEqual(true, isFunction);
        }

        [Fact]
        public void TestGetScopeIdentifiers3()
        {
            // Verify that we can identify the containing scope for the code within a member function.
            var finder = new CppFinder(code2);
            FileSpan span = finder.GetScopeSpanAtLine(8);
            System.Collections.Generic.List<string> identifiers = finder.GetScopeIdentifiers(span.Start, out bool isFunction);
            Assert.AreEqual(2, identifiers.Count);
            Assert.AreEqual("Foo", identifiers[0]);
            Assert.AreEqual("Class2", identifiers[1]);
            Assert.AreEqual(true, isFunction);
        }

        [Fact]
        public void TestGetScopeSpanNull1()
        {
            // Verify that we return no scope at line 5.
            var finder = new CppFinder(code2);
            FileSpan span = finder.GetScopeSpanAtLine(5);
            Assert.AreEqual(null, span);
        }

        [Fact]
        public void TestFindCodeInMemberDifferentClass1()
        {
            // When the line of code text is found multiple times, make sure
            // we find the right instance in the right class scope.
            var finder = new CppFinder(code2);
            var query = new MatchQuery("return a + a", 3, "Class1::Foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeInMemberDifferentClass2()
        {
            // When the line of code text is found multiple times, make sure
            // we find the right instance in the right class scope.
            var finder = new CppFinder(code2);
            var query = new MatchQuery("return a + a", 8, "Class2::Foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(8, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeNoScope2()
        {
            // When no scope is provided, we should get 2 results.
            var finder = new CppFinder(code2);
            var query = new MatchQuery("return a + a", 8, "", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);
            Assert.AreEqual(8, results[1].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(true, results[1].ScopeChecked);
            Assert.AreEqual(-2, results[0].ScopeMatchDiff);
            Assert.AreEqual(-2, results[1].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodePartialScope1()
        {
            // when a partial, ambiguous scope is provided, we should get 2 results.
            var finder = new CppFinder(code2);
            var query = new MatchQuery("return a + a", 8, "Foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);
            Assert.AreEqual(8, results[1].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(true, results[1].ScopeChecked);
            Assert.AreEqual(-1, results[0].ScopeMatchDiff);
            Assert.AreEqual(-1, results[1].ScopeMatchDiff);
        }

        private const string code3 =
            @"public class MyClass
            {
                int id;

                public MyClass(int id)
                {
                    this.id = id;
                }

                int Foo (int a)
                {
                    return a + a;
                }
            }";

        [Fact]
        public void TestFindCodeInMemberFunction2()
        {
            // Make sure we can find code in a member function when it is
            // implemented within the class declaration.
            var finder = new CppFinder(code3);
            var query = new MatchQuery("return a + a", 12, "MyClass::Foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(12, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeInConstructor1()
        {
            // Verify we can find a line of code within a class' constructor.
            var finder = new CppFinder(code3);
            var query = new MatchQuery("this.id = id", 7, "MyClass::MyClass", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(7, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodePartialScope2()
        {
            // Verify we can find a line of code within a class' constructor.
            var finder = new CppFinder(code3);
            var query = new MatchQuery("this.id = id", 7, "MyClass", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(7, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(-1, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeNoScope3()
        {
            // Verify we can find a line of code within a class' constructor.
            var finder = new CppFinder(code3);
            var query = new MatchQuery("this.id = id", 7, "", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(7, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(-2, results[0].ScopeMatchDiff);
        }

        private const string code4 =
            @"public class MyClass : OtherClass
            {
                int id = GetDefaultId();

                public MyClass(int id) : base()
                {
                    this.id = id;
                }

                int Foo (int a)
                {
                    return a + a;
                }
            }";

        [Fact]
        public void TestFindCodeInMemberFunction3()
        {
            // Verify that we can find code in a member function of a class that derives from some other class.
            var finder = new CppFinder(code4);
            var query = new MatchQuery("return a + a", 12, "MyClass::Foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(12, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeInConstructor2()
        {
            // Verify that we can find code in a constructor that calls the base class in its initialization list.
            var finder = new CppFinder(code4);
            var query = new MatchQuery("this.id = id", 7, "MyClass::MyClass", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(7, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeInClass1()
        {
            // Verify that we can find code within the class itself (not within a member function).
            var finder = new CppFinder(code4);
            var query = new MatchQuery("int id = GetDefaultId()", 3, "MyClass", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeInInitializationList1()
        {
            // Verify that we can find code in the constructor initialization list.
            var finder = new CppFinder(code4);
            var query = new MatchQuery("base()", 5, "MyClass::MyClass", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(5, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(1, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeNoScope4()
        {
            // Verify that we can find code in the constructor initialization list.
            var finder = new CppFinder(code4);
            var query = new MatchQuery("base()", 5, "MyClass::MyClass", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(5, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(1, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindMemberFunction1()
        {
            // Verify that we can find the definition of a member function (rather than code within it).
            var finder = new CppFinder(code4);
            var query = new MatchQuery("Foo", 10, "MyClass", "0", MatchQuery.MatchTypeHint.Function);
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(10, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        private const string code5 =
            @"public int MyClass::Foo(Thing* myThing)
            {
                // Make sure things are initialized.
                Initialize();

                try
                {
                    myThing->DoStuff();
                }
                catch (Exception* e)
                {
                    LogException(e, ""Oops!"");
                }
                finally
                {
                    // No matter what happens, make sure we clean up.
                    Cleanup();
                }
            }";

        [Fact]
        public void TestFindCodeInTry1()
        {
            var finder = new CppFinder(code5);
            var query = new MatchQuery("myThing->DoStuff()", 8, "MyClass::Foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(8, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeInCatch1()
        {
            var finder = new CppFinder(code5);
            var query = new MatchQuery("LogException", 12, "MyClass::Foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(12, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeInCatch2()
        {
            var finder = new CppFinder(code5);
            var query = new MatchQuery("LogException(e, \"Oops!\")", 12, "MyClass::Foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(12, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeInFinally1()
        {
            var finder = new CppFinder(code5);
            var query = new MatchQuery("Cleanup()", 17, "MyClass::Foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(17, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeInFinally2()
        {
            var finder = new CppFinder(code5);
            var query = new MatchQuery("Cleanup()", 17, "MyClass::Foo$fin$0", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(17, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeNoScope5()
        {
            var finder = new CppFinder(code5);
            var query = new MatchQuery("Cleanup()", 17, "", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(17, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(-2, results[0].ScopeMatchDiff);
        }

        private const string code6 =
            @"public int MyClass::Foo(Thing* myThing)
            {
                try
                {
                    myThing->DoStuff();
                }
                except (ShouldHandleException(GetExceptionCode()))
                {                    
                    LogException(GetExceptionCode(), ""Oops!"");
                }
            }";

        [Fact]
        public void TestFindCodeInTry2()
        {
            var finder = new CppFinder(code6);
            var query = new MatchQuery("myThing->DoStuff()", 5, "MyClass::Foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(5, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeInExcept1()
        {
            var finder = new CppFinder(code6);
            var query = new MatchQuery("LogException", 9, "MyClass::Foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(9, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeInExcept2()
        {
            var finder = new CppFinder(code6);
            var query = new MatchQuery("LogException", 9, "MyClass::Foo$filt$0", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(9, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodeNoScope6()
        {
            var finder = new CppFinder(code6);
            var query = new MatchQuery("LogException", 9, "", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(9, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(-2, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodePartialScope3()
        {
            var finder = new CppFinder(code6);
            var query = new MatchQuery("LogException", 9, "Foo", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(9, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(-1, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFindCodePartialScope4()
        {
            // Partial, but incorrect scope should return no matches.
            var finder = new CppFinder(code6);
            var query = new MatchQuery("LogException", 9, "MyClass", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(0, results.Count);
        }

        private const string code7 =
            @"class SettingsAppActivityTelemetry final : public wil::TraceLoggingProvider
            {

            public:
            
                TraceSettingsContentDialogLaunched
                    (_In_ Platform::String^ parentSettingId)
                {
                    m_sharedTelemetryData->FlowParentSettingId = parentSettingId;
                    TraceLoggingClassWriteMeasure(
                        ""SettingsContentDialogLaunched"",
                        SettingsTraceLoggingSessionIds(),
                        SettingsTraceLoggingCurrentPage(),
                        TraceLoggingWideString(m_sharedTelemetryData->FlowParentSettingId->Data(), ""parentSettingId""));
                }
            }";

        [Fact]
        public void TestComplexClass()
        {
            // Test when the class declaration is somewhat complex.
            var finder = new CppFinder(code7);
            var query = new MatchQuery("\"SettingsContentDialogLaunched\"", 11, "SettingsAppActivityTelemetry::TraceSettingsContentDialogLaunched", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(11, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        private const string code8 =
            @"#define TraceLoggingPartB_Ms_Content_PageAction_SettingsContent(pageName, customSessionGuid, impressionGuid, actionInputMethod, controlNameActionedOn) \
                PartB_Ms_Content_PageAction( \
                    pageName, \
                    nullptr, \
                    nullptr, \
                    nullptr, \
                    nullptr, \
                    nullptr, \
                    0, \
                    customSessionGuid, \
                    impressionGuid, \
                    actionInputMethod, \
                    0, \
                    0.0, \
                    controlNameActionedOn)

            namespace SystemSettings
            {
                namespace Telemetry
                {
                    class SettingsAppActivityTelemetry final : public wil::TraceLoggingProvider
                    {

                    public:
            
                        TraceSettingsContentDialogLaunched
                            (_In_ Platform::String^ parentSettingId)
                        {
                            m_sharedTelemetryData->FlowParentSettingId = parentSettingId;
                            TraceLoggingClassWriteMeasure(
                                ""SettingsContentDialogLaunched"",
                                SettingsTraceLoggingSessionIds(),
                                SettingsTraceLoggingCurrentPage(),
                                TraceLoggingWideString(m_sharedTelemetryData->FlowParentSettingId->Data(), ""parentSettingId""));
                        }
                    }
                }
            }";

        [Fact]
        public void TestDefineBeforeNamespace()
        {
            // Test when a namespace is preceded by a macro to ensure we don't mistakenly interpret the namespace as potentially being a function.
            var finder = new CppFinder(code8);
            var query = new MatchQuery("\"SettingsContentDialogLaunched\"", 31, "SystemSettings::Telemetry::SettingsAppActivityTelemetry::TraceSettingsContentDialogLaunched", "0");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(31, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        private const string code9 =
            @"namespace Microsoft::Bluetooth::BthCoreCx
            {
                NTSTATUS HciTransport::SendRequest(WDFREQUEST wdfRequest, unsigned int timeoutInMilliseconds)
                {
                    NTSTATUS status;

                    WDF_REQUEST_SEND_OPTIONS* sendOptionsPtr = nullptr;
                    WDF_REQUEST_SEND_OPTIONS sendOptions;
                    if (timeoutInMilliseconds != 0)
                    {
                        // Allocate a timer up-front so that we can guarantee that WdfRequestSend will fail only if
                        // the I/O target is not accepting requests. This allows us to more clearly identify the
                        // failure reason.
                        status = WdfRequestAllocateTimer(wdfRequest);
                        if (!NT_SUCCESS(status))
                        {
                            TRANSPORT_TRACE(this, LEVEL_ERROR, ""WdfRequestAllocateTimer failed - %!STATUS!, WDFREQUEST: 0x%p"", status, wdfRequest);
                            return status;
                        }

                        WDF_REQUEST_SEND_OPTIONS_INIT(&sendOptions, 0);
                        WDF_REQUEST_SEND_OPTIONS_SET_TIMEOUT(&sendOptions, WDF_REL_TIMEOUT_IN_MS(timeoutInMilliseconds));
                        sendOptionsPtr = &sendOptions;
                    }

                    BOOLEAN sendResult = WdfRequestSend(wdfRequest, m_ioTarget, sendOptionsPtr);
                    if (!sendResult)
                    {
                        status = WdfRequestGetStatus(wdfRequest);
                        WDF_IO_TARGET_STATE ioTargetState = WdfIoTargetGetState(m_ioTarget);
                        TRANSPORT_TRACE(this, LEVEL_ERROR, ""WdfRequestSend failed - %!STATUS!, WDFREQUEST: 0x%p, I/O target state: %!WDF_IO_TARGET_STATE!"",
                            status, wdfRequest, ioTargetState);

                        return status;
                    }

                    return STATUS_SUCCESS;
                }
            }";

        [Fact]
        public void TestFunctionDefinitionMatch1()
        {
            // Make sure we find the function definition when the function name is provided and not included in the calling signature.
            var finder = new CppFinder(code9);
            var query = new MatchQuery("SendRequest", 3, "Microsoft::Bluetooth::BthCoreCx::HciTransport", "0", MatchQuery.MatchTypeHint.Function);
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFunctionDefinitionMatch2()
        {
            // Make sure we find the function definition when the function name is provided and is also included in the calling signature.
            var finder = new CppFinder(code9);
            var query = new MatchQuery("SendRequest", 3, "Microsoft::Bluetooth::BthCoreCx::HciTransport::SendRequest", "0", MatchQuery.MatchTypeHint.Function);
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        [Fact]
        public void TestFunctionDefinitionMatch3()
        {
            // Make sure we find the function definition when the text to search and the calling signature are the same.
            var finder = new CppFinder(code9);
            var query = new MatchQuery("Microsoft::Bluetooth::BthCoreCx::HciTransport::SendRequest", 3, "Microsoft::Bluetooth::BthCoreCx::HciTransport::SendRequest", "0", MatchQuery.MatchTypeHint.Function);
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);
            Assert.AreEqual(true, results[0].ScopeChecked);
            Assert.AreEqual(0, results[0].ScopeMatchDiff);
        }

        private const string code10 =
            @"cHydratedFluidLink++;
            if (!pFluidOsfE2oUserLast)
            {
                pFluidOsfE2oUserLast = pFluidOsfE2oUser;
                cpLimFluidFieldLast = cpInsert + 1;
            }

            pFluidOsfE2oUser->SetFFluidIconPresent(fIsFluidIconPresent);

            static constexpr Mso::AB::Optimized::ChangeGate fIsInEditableDocumentEnabled { Mso::AB::TeamEnum::Word,
                                                                                           ""CanDocumentBeEdited""_S };
            if (fIsInEditableDocumentEnabled)
            {
                pFluidOsfE2oUser->SetCanDocumentBeEdited(!fReadMode);
            }

            if (FFluidRefactorToSupportCreateNew())
            {
                pFluidOsfE2oUser->FHideHyperlink();
            }";

        [Fact]
        public void TestWholeTokenMatch1()
        {
            var finder = new CppFinder(code10);
            var query = new MatchQuery("CanDocumentBeEdited", 3, "", "0");

            // The desired match is on line 11 ("CanDocumentBeEdited" string literal) not on line 14 (SetCanDocumentBeEdited).
            // V1 correctly finds this as the best match b/c it is closest to the line hint.
            // V2 returns the other match (on line 14) b/c the first match has a ScopeMatchDiff of -1 (the second match has 0)
            // unless whole token matching is enabled.

            System.Collections.Generic.List<MatchResult> results = finder.FindMatches(query);
            var bestMatchV1 = MatchResult.GetBestMatch(results);

            System.Collections.Generic.List<MatchResult> results2 = finder.FindMatchesWithFunction(query);
            var bestMatchV2 = MatchResult.GetBestMatch(results2);

            Assert.AreEqual(11, bestMatchV2.LineNumber);

            Assert.AreEqual(bestMatchV1.LineNumber, bestMatchV2.LineNumber);
        }

        private const string code11 =
            @"if (Foo->IsDoSomethingExEnabled() == false)
            {
                Foo->DoSomething();
            }
            else
            {
                Foo->DoSomethingEx();
            }";

        [Fact]
        public void TestWholeTokenMatch2()
        {
            var finder = new CppFinder(code11);

            // Verify we only match on line 3 when whole token match is enabled (by default).
            var query = new MatchQuery("DoSomething");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);

            // Verify we get 3 matches when whole token matching is disabled.
            query = new MatchQuery("DoSomething", matchWholeTokens: false);
            results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(1, results[0].LineNumber);
            Assert.AreEqual(3, results[1].LineNumber);
            Assert.AreEqual(7, results[2].LineNumber);

            // Verify we get a match when the text to find has multiple tokens.
            query = new MatchQuery("Foo->DoSomething");
            results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);

            // Verify we get 2 matches with multiple tokens when whole token matching is disabled.
            query = new MatchQuery("Foo->DoSomething", matchWholeTokens: false);
            results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(3, results[0].LineNumber);
            Assert.AreEqual(7, results[1].LineNumber);

            // Verify we get no matches when we ask to find a string that has no whole token matches.
            query = new MatchQuery("Something");
            results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(0, results.Count);
        }

        private const string code12 =
            @"void FluidOsfE2oUser::RemoveFluidComponent() noexcept
            {
                static constexpr Mso::AB::Optimized::ChangeGate fRemoveFluidComponent { Mso::AB::TeamEnum::Word,
                                                                                        ""RemoveFluidComponent""_S };
                if (fRemoveFluidComponent)
                {
                    const Mso::WeakPtr<FluidOsfE2oUser> pwUser(this);
                    HrExecuteOnIdleTask(
                        [pwUser] () noexcept

                        {
                        Mso::Threadpool::AssertMainThread();
                        const Mso::TCntPtr<FluidOsfE2oUser> pcUser = pwUser.GetStrongPtr();
                        if (pcUser != nullptr)
                        {
                            pcUser->RemoveFluidComponentOnMainThread();
                        }
                    });
                }
            }
            ";

        [Fact]
        public void TestPreferStringLiteral()
        {
            var finder = new CppFinder(code12);

            // Verify we only match on line 3 when whole token match is enabled (by default).
            var query = new MatchQuery("RemoveFluidComponent");
            System.Collections.Generic.List<MatchResult> results = finder.FindMatchesWithFunction(query);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(1, results[0].LineNumber);
            Assert.AreEqual(4, results[1].LineNumber);

            var bestResult = MatchResult.GetBestMatch(results, preferStringLiterals: true);
            Assert.AreEqual(4, bestResult.LineNumber);
        }

        [Fact]
        public void TestFindNamespaceMatch()
        {
            var finder = new CppFinder(code9);

            // Verify we only match on line 1 when whole token match is enabled (by default).
            var query = new MatchQuery("namespace", lineNumberHint: 1, "", "", MatchQuery.MatchTypeHint.Class);
            System.Collections.Generic.List<MatchResult> results = finder.FindMatches(query);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LineNumber);
        }
    }
}
