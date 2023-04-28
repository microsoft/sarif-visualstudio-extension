// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

using Moq;

using Newtonsoft.Json;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "No point in naming test methods \"Async\".")]
    public class SuppressionTests
    {
        public SuppressionTests()
        {
            TestUtilities.SetCodeAnalysisResultManager();
        }

        [Fact]
        public async Task CodeAnalysisResultManager_AddSuppressionToSarifLog_Tests()
        {
            var testCases = new[]
            {
                new
                {
                    Model = new SuppressionModel((IEnumerable<SarifErrorListItem>)null)
                    {
                        Status = SuppressionStatus.Accepted,
                        Kind = SuppressionKind.External,
                    },
                    ResultToBeSuppressed = new string[] { "Test0001" },
                    Expected = new
                    {
                        SuppressionAdded = true,
                        RunHasSuppressions = true,
                        ResultSuppressState = VSSuppressionState.Suppressed,
                    }
                },
                new
                {
                    Model = new SuppressionModel((IEnumerable<SarifErrorListItem>)null)
                    {
                        Status = SuppressionStatus.UnderReview,
                        Kind = SuppressionKind.External,
                    },
                    ResultToBeSuppressed = new string[] { "Test0002" },
                    Expected = new
                    {
                        SuppressionAdded = true,
                        RunHasSuppressions = false,
                        ResultSuppressState = VSSuppressionState.Active,
                    }
                },
                new
                {
                    Model = new SuppressionModel((IEnumerable<SarifErrorListItem>)null)
                    {
                        Status = SuppressionStatus.Rejected,
                        Kind = SuppressionKind.External,
                    },
                    ResultToBeSuppressed = new string[] { "Test0001" },
                    Expected = new
                    {
                        SuppressionAdded = true,
                        RunHasSuppressions = false,
                        ResultSuppressState = VSSuppressionState.Active,
                    }
                },
                new
                {
                    Model = new SuppressionModel((IEnumerable<SarifErrorListItem>)null)
                    {
                        Status = SuppressionStatus.Accepted,
                        Kind = SuppressionKind.External,
                    },
                    ResultToBeSuppressed = new string[] { "Test0002", "Test0004" },
                    Expected = new
                    {
                        SuppressionAdded = true,
                        RunHasSuppressions = true,
                        ResultSuppressState = VSSuppressionState.Active,
                    }
                },
                new
                {
                    Model = new SuppressionModel((IEnumerable<SarifErrorListItem>)null)
                    {
                        Status = SuppressionStatus.Accepted,
                        Kind = SuppressionKind.External,
                    },
                    ResultToBeSuppressed = new string[] { "Test0001", "Test0002" , "Test0003", "Test0004", "Test0005"},
                    Expected = new
                    {
                        SuppressionAdded = true,
                        RunHasSuppressions = true,
                        ResultSuppressState = VSSuppressionState.Active,
                    }
                },
                new
                {
                    Model = new SuppressionModel((IEnumerable<SarifErrorListItem>)null)
                    {
                        Status = SuppressionStatus.Accepted,
                    },
                    ResultToBeSuppressed = new string[] { "Test0001-not-exists" },
                    Expected = new
                    {
                        SuppressionAdded = false,
                        RunHasSuppressions = false,
                        ResultSuppressState = VSSuppressionState.Active,
                    }
                }
            };

            foreach (var testCase in testCases)
            {
                await VerifySuppressionTests(testCase);
            }
        }

        public async Task<SarifLog> VerifySuppressionTests(dynamic testCase)
        {
            DateTime now = DateTime.UtcNow;
            string[] resultToBeSuppressed = testCase.ResultToBeSuppressed;
            string sarifLogFilePath = @"E:\Users\AnUser\Sarif\Logs\ResultsToSuppress.sarif";
            var transformedContents = new StringBuilder();
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(x => x.FileCreate(sarifLogFilePath))
                .Returns(() => new StringBuilderFileStreamMock(transformedContents));
            CodeAnalysisResultManager.Instance = new CodeAnalysisResultManager(mockFileSystem.Object);

            SarifLog sarifLog = CreateTestSarifLog();
            await TestUtilities.InitializeTestEnvironmentAsync(sarifLog, sarifLogFilePath);

            IEnumerable<SarifErrorListItem> itemToBeSuppressed = CodeAnalysisResultManager.Instance
                                                    .RunIndexToRunDataCache[CodeAnalysisResultManager.Instance.CurrentRunIndex]
                                                    .SarifErrors.Where(i => resultToBeSuppressed.Contains(i.Rule.Id));

            SuppressionModel model = testCase.Model;
            if (model != null && itemToBeSuppressed != null)
            {
                model.SelectedErrorListItems = itemToBeSuppressed;
            }

            sarifLog.Runs.First().HasSuppressedResults().Should().Be(false);

            CodeAnalysisResultManager.Instance.AddSuppressionToSarifLog(model);
            SarifLog suppressedLog = JsonConvert.DeserializeObject<SarifLog>(transformedContents.ToString());

            if (!testCase.Expected.SuppressionAdded)
            {
                suppressedLog.Should().BeNull();
                return suppressedLog;
            }

            Run run = suppressedLog.Runs.First();
            run.HasSuppressedResults().Should().Be(testCase.Expected.RunHasSuppressions);
            foreach (Result result in run.Results)
            {
                if (resultToBeSuppressed.Contains(result.RuleId))
                {
                    // suppressed result
                    result.Suppressions.Should().NotBeNullOrEmpty();
                    result.Suppressions.Count.Should().Be(1);
                    result.TryIsSuppressed(out bool isSuppressed).Should().Be(true);
                    isSuppressed.Should().Be(model.Status == SuppressionStatus.Accepted);
                    Suppression suppression = result.Suppressions.First();
                    suppression.Status.Should().Be(model.Status);
                    suppression.Kind.Should().Be(model.Kind);
                }
                else
                {
                    if (testCase.Expected.RunHasSuppressions)
                    {
                        // not suppressed result should have empty suppressions
                        result.Suppressions.Should().NotBeNull();
                        result.Suppressions.Should().BeEmpty();
                    }
                }
            }

            return suppressedLog;
        }

        private SarifLog CreateTestSarifLog()
        {
            return new SarifLog
            {
                Runs = new List<Run>
                {
                    new Run
                    {
                        Results = new List<Result>
                        {
                            new Result
                            {
                                RuleId = "Test0001"
                            },
                            new Result
                            {
                                RuleId = "Test0002"
                            },
                            new Result
                            {
                                RuleId = "Test0003"
                            },
                            new Result
                            {
                                RuleId = "Test0004"
                            },
                            new Result
                            {
                                RuleId = "Test0005"
                            }
                        }
                    }
                }
            };
        }
    }
}
