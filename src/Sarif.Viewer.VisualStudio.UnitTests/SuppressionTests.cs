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
        [Fact]
        public async Task CodeAnalysisResultManager_AddSuppressionToSarifLog_Tests()
        {
            var testCases = new[]
            {
                new
                {
                    Model = new SuppressionModel(null)
                    {
                        Status = SuppressionStatus.Accepted,
                        Justification = "Suppress this result for 5 days",
                        UserAlias = "someone",
                        ExpiryInDays = 5,
                        Timestamp = DateTime.UtcNow
                    },
                    ResultToBeSuppressed = "Test0001",
                    Expected = new
                    {
                        SuppressionAdded = true,
                        RunHasSuppressions = true,
                    }
                },
                new
                {
                    Model = new SuppressionModel(null)
                    {
                        Status = SuppressionStatus.UnderReview,
                        Justification = "Suppress this result for 5 days",
                        UserAlias = "Someone",
                        ExpiryInDays = -5,
                        Timestamp = DateTime.UtcNow
                    },
                    ResultToBeSuppressed = "Test0002",
                    Expected = new
                    {
                        SuppressionAdded = true,
                        RunHasSuppressions = false,
                    }
                },
                new
                {
                    Model = new SuppressionModel(null)
                    {
                        Status = SuppressionStatus.Accepted,
                        UserAlias = "Someone",
                        ExpiryInDays = 0,
                    },
                    ResultToBeSuppressed = "Test0003",
                    Expected = new
                    {
                        SuppressionAdded = true,
                        RunHasSuppressions = true,
                    }
                },
                new
                {
                    Model = new SuppressionModel(null)
                    {
                        Status = SuppressionStatus.Rejected,
                    },
                    ResultToBeSuppressed = "Test0001",
                    Expected = new
                    {
                        SuppressionAdded = true,
                        RunHasSuppressions = false,
                    }
                },
                new
                {
                    Model = new SuppressionModel(null)
                    {
                        Status = SuppressionStatus.Accepted,
                        Justification = "!@#%^ \r\n+_))(^&^%&*",
                        UserAlias = "",
                    },
                    ResultToBeSuppressed = "Test0001",
                    Expected = new
                    {
                        SuppressionAdded = true,
                        RunHasSuppressions = true,
                    }
                },
                new
                {
                    Model = (SuppressionModel)null,
                    ResultToBeSuppressed = "Test0001",
                    Expected = new
                    {
                        SuppressionAdded = false,
                        RunHasSuppressions = false,
                    }
                },
                new
                {
                    Model = new SuppressionModel(null)
                    {
                        Status = SuppressionStatus.Accepted,
                    },
                    ResultToBeSuppressed = "Test0001-not-exists",
                    Expected = new
                    {
                        SuppressionAdded = false,
                        RunHasSuppressions = false,
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
            string resultToBeSuppressed = testCase.ResultToBeSuppressed;
            string sarifLogFilePath = @"E:\Users\Yong\Sarif\Logs\ResultsToSuppress.sarif";
            var transformedContents = new StringBuilder();
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(x => x.FileCreate(sarifLogFilePath))
                .Returns(() => new StringBuilderFileStreamMock(transformedContents));
            CodeAnalysisResultManager.Instance = new CodeAnalysisResultManager(mockFileSystem.Object);

            SarifLog sarifLog = CreateTestSarifLog();
            await TestUtilities.InitializeTestEnvironmentAsync(sarifLog, sarifLogFilePath);

            SarifErrorListItem itemToBeSuppressed = CodeAnalysisResultManager.Instance
                                                    .RunIndexToRunDataCache[CodeAnalysisResultManager.Instance.CurrentRunIndex]
                                                    .SarifErrors.FirstOrDefault(i => i.Rule.Id == resultToBeSuppressed);

            SuppressionModel model = testCase.Model;
            if (model != null && itemToBeSuppressed != null)
            {
                model.SelectedErrorListItems = new[] { itemToBeSuppressed };
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
                if (result.RuleId == resultToBeSuppressed)
                {
                    // suppressed result
                    result.Suppressions.Should().NotBeNullOrEmpty();
                    result.Suppressions.Count.Should().Be(1);
                    result.TryIsSuppressed(out bool isSuppressed).Should().Be(true);
                    isSuppressed.Should().Be(model.Status == SuppressionStatus.Accepted);
                    Suppression suppression = result.Suppressions.First();
                    suppression.Status.Should().Be(model.Status);

                    if (model.Justification == null)
                    {
                        suppression.Justification.Should().BeNull();
                    }
                    else
                    {
                        suppression.Justification.Should().BeEquivalentTo(model.Justification);
                    }
                    suppression.Guid.Should().NotBeNull();
                    if (string.IsNullOrWhiteSpace(model.UserAlias))
                    {
                        suppression.TryGetProperty("alias", out _).Should().Be(false);
                    }
                    else
                    {
                        suppression.GetProperty("alias").Should().BeEquivalentTo(model.UserAlias);
                    }
                    DateTime timestamp = suppression.GetProperty<DateTime>("timeUtc");
                    if (model.Timestamp.HasValue)
                    {
                        timestamp.Should().Be(model.Timestamp.Value);
                    }
                    else
                    {
                        // if timestamp not specific, it will be set as current time
                        timestamp.Should().BeAfter(now);
                    }
                    if (model.ExpiryInDays > 0)
                    {
                        suppression.GetProperty<DateTime>("expiryUtc").Should().Be(timestamp.AddDays(model.ExpiryInDays));
                    }
                    else
                    {
                        // if expiry in days not set, it will not add property expiryUtc
                        suppression.TryGetProperty("expiryUtc", out _).Should().Be(false);
                    }
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
                            }
                        }
                    }
                }
            };
        }
    }
}
