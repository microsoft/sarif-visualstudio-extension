// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio.Shell.TableManager;

using Moq;

using Xunit;

using Match = System.Text.RegularExpressions.Match;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class ErrorListServiceTests : SarifViewerPackageUnitTests
    {
        private static readonly TestCase[] s_testCases = new TestCase[]
        {
            new TestCase
            {
                Title = "Simplest case",
                Input = @"""version"": ""2.1.0""",
                ExpectedMatchSuccess = true,
                ExpectedVersion = "2.1.0",
            },
            new TestCase
            {
                Title = "White space before colon",
                Input = @"""version"" : ""2.1.0""",
                ExpectedMatchSuccess = true,
                ExpectedVersion = "2.1.0",
            },
            new TestCase
            {
                Title = "Version near start of file",
                Input = @"01234567890123456789 ""version"" : ""2.1.0""",
                ExpectedMatchSuccess = true,
                ExpectedVersion = "2.1.0",
            },
            new TestCase
            {
                Title = "Version not near start of file",
                Input = @"01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789 ""version"" : ""2.1.0""",
                ExpectedMatchSuccess = false,
            },
            new TestCase
            {
                Title = "Invalid version",
                Input = @"""version"": ""a.b.c""",
                ExpectedMatchSuccess = false,
            },
        };

        [Fact]
        [Trait(TestTraits.Bug, "98")]
        public void ErrorListService_MatchesVersionProperty()
        {
            var failedTestCases = new List<TestCase>();

            foreach (TestCase testCase in s_testCases)
            {
                Match match = ErrorListService.MatchVersionProperty(testCase.Input);

                bool failed = false;
                if (match.Success != testCase.ExpectedMatchSuccess) { failed = true; }

                if (!failed && testCase.ExpectedMatchSuccess)
                {
                    if (match.Groups["version"].Value != testCase.ExpectedVersion) { failed = true; }
                }

                if (failed) { failedTestCases.Add(testCase); }
            }

            failedTestCases.Should().BeEmpty();
        }

        [Fact]
        public void ErrorListService_ProcessSarifLogAsync_InvalidJson()
        {
            // unhook original event handler and hook test event
            ErrorListService.LogProcessed -= ErrorListService.ErrorListService_LogProcessed;
            ErrorListService.LogProcessed += ErrorListServiceTest_LogProcessed;

            // invalid Json syntax, a separate comma in line 4
            // {"Invalid property identifier character: ,. Path '$schema', line 4, position 2."}
            string invalidJson =
@"
{
  ""$schema"": ""https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0-rtm.4.json"",
  ,
  ""version"": ""2.1.0"",
  ""runs"": [
    {
      ""tool"": {
        ""name"": ""CodeScanner""
      },
      ""results"": [
      ]
    }
  ]
}
";
            int numberOfException = numberOfExceptionLogged;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));
            ErrorListService.ProcessSarifLogAsync(stream, "logId", false, false).ConfigureAwait(false);
            // 1 exception logged
            numberOfExceptionLogged.Should().Be(numberOfException + 1);
        }

        [Fact]
        public void ErrorListService_ProcessSarifLogAsync_JsonNotCompatibleWithSarifShema()
        {
            // unhook original event handler and hook test event
            ErrorListService.LogProcessed -= ErrorListService.ErrorListService_LogProcessed;
            ErrorListService.LogProcessed += ErrorListServiceTest_LogProcessed;

            // valid Json syntax, but not satisfy schema, missing required property driver
            // {"Required property 'text' not found in JSON. Path 'runs[0].tool.driver.rules[0].fullDescription', line 14, position 36."}
            string jsonNotCompatible =
@"
{
  ""$schema"": ""https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0-rtm.4.json"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {
      ""tool"": {
        ""driver"": {
          ""name"": ""CodeScanner"",
          ""rules"": [
            {
              ""id"": ""Intrafile1001"",
              ""name"": ""IntrafileRule"",
              ""fullDescription"": { },
              ""helpUri"": ""https://github.com/microsoft/sarif-pattern-matcher""
            }
          ]
        },
      ""results"": [
      ]
    }
  ]
}
";
            int numberOfException = this.numberOfExceptionLogged;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonNotCompatible));
            ErrorListService.ProcessSarifLogAsync(stream, "logId", false, false).ConfigureAwait(false);
            // 1 exception logged
            this.numberOfExceptionLogged.Should().Be(numberOfException + 1);
        }

        [Fact]
        public void ProcessSarifLogAsync_ResultsFiltered_ShouldShowNotification()
        {
            // unhook original event handler and hook test event
            ErrorListService.LogProcessed -= ErrorListService.ErrorListService_LogProcessed;
            ErrorListService.LogProcessed += ErrorListServiceTest_LogProcessed;

            var testLog = new SarifLog
            {
                Runs = new List<Run>
                {
                    new Run
                    {
                        Tool = new Tool
                        {
                            Driver = new ToolComponent
                            {
                                Name = "Test",
                                SemanticVersion = "1.0",
                            },
                        },
                        Results = new List<Result>
                        {
                            new Result
                            {
                                AnalysisTarget = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///item.cpp"),
                                },
                                RuleId = "E0001",
                                Message = new Message { Text = "Error description" },
                                Level = FailureLevel.Error,
                                Locations = new List<Location>
                                {
                                    new Location(),
                                },
                            },
                            new Result
                            {
                                AnalysisTarget = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///item.cpp"),
                                },
                                RuleId = "C0001",
                                Message = new Message { Text = "Warning number 1" },
                                Level = FailureLevel.Warning,
                                Locations = new List<Location>
                                {
                                    new Location(),
                                },
                            },
                        },
                    },
                },
            };

            var mockColumnFilter = new Mock<IColumnFilterer>();
            mockColumnFilter.Setup(x => x.GetFilteredValues(StandardTableKeyNames.ErrorSeverity)).Returns(new[] { "warning", "note" });

            ErrorListService.Instance.ColumnFilterer = mockColumnFilter.Object;

            ErrorListService.ProcessSarifLogAsync(testLog, "logId", false, false).ConfigureAwait(false);
            
            this.logExceptionalConditions.HasFlag(ExceptionalConditions.ResultsFiltered).Should().BeTrue();
        }

        private struct TestCase
        {
            public string Title { get; set; }

            public string Input { get; set; }

            public bool ExpectedMatchSuccess { get; set; }

            public string ExpectedVersion { get; set; }
        }

        private int numberOfExceptionLogged = 0;

        private ExceptionalConditions logExceptionalConditions;

        private void ErrorListServiceTest_LogProcessed(object sender, LogProcessedEventArgs e)
        {
            this.numberOfExceptionLogged++;
            this.logExceptionalConditions = e.ExceptionalConditions;
        }
    }
}
