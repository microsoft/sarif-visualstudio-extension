// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class RunExtensionsTests
    {
        private static readonly ReadOnlyCollection<HasAbsentResultsTestCase> s_hasAbsentResultsTestCases = new List<HasAbsentResultsTestCase>
        {
            new HasAbsentResultsTestCase
            {
                Name = "Null results",
                Run = new Run(),
                ExpectedResult = false,
            },

            new HasAbsentResultsTestCase
            {
                Name = "Empty results",
                Run = new Run
                {
                    Results = new List<Result>(),
                },
                ExpectedResult = false,
            },

            new HasAbsentResultsTestCase
            {
                Name = "No baseline",
                Run = new Run
                {
                    Results = new List<Result>
                    {
                        new Result(),
                    },
                },
                ExpectedResult = false,
            },

            new HasAbsentResultsTestCase
            {
                Name = "New result",
                Run = new Run
                {
                    Results = new List<Result>
                    {
                        new Result
                        {
                            BaselineState = BaselineState.New,
                        },
                    },
                },
                ExpectedResult = false,
            },

            new HasAbsentResultsTestCase
            {
                Name = "Absent result",
                Run = new Run
                {
                    Results = new List<Result>
                    {
                        new Result
                        {
                            BaselineState = BaselineState.Absent,
                        },
                    },
                },
                ExpectedResult = true,
            },

            new HasAbsentResultsTestCase
            {
                Name = "Absent result after new result",
                Run = new Run
                {
                    Results = new List<Result>
                    {
                        new Result
                        {
                            BaselineState = BaselineState.New,
                        },

                        new Result
                        {
                            BaselineState = BaselineState.Absent,
                        },
                    },
                },
                ExpectedResult = true,
            },

            new HasAbsentResultsTestCase
            {
                Name = "Absent result after result with no BaselineState",
                Run = new Run
                {
                    Results = new List<Result>
                    {
                        new Result(),

                        new Result
                        {
                            BaselineState = BaselineState.Absent,
                        },
                    },
                },
                ExpectedResult = false,
            },
        }.AsReadOnly();

        private static readonly ReadOnlyCollection<HasSuppressedResultsTestCase> s_hasSuppressedResultsTestCases = new List<HasSuppressedResultsTestCase>
        {
            new HasSuppressedResultsTestCase
            {
                Name = "Null results",
                Run = new Run(),
                ExpectedResult = false,
            },

            new HasSuppressedResultsTestCase
            {
                Name = "Empty results",
                Run = new Run
                {
                    Results = new List<Result>(),
                },
                ExpectedResult = false,
            },

            new HasSuppressedResultsTestCase
            {
                Name = "Null suppressions array",
                Run = new Run
                {
                    Results = new List<Result>
                    {
                        new Result(),
                    },
                },
                ExpectedResult = false,
            },

            new HasSuppressedResultsTestCase
            {
                Name = "Empty suppressions array",
                Run = new Run
                {
                    Results = new List<Result>
                    {
                        new Result
                        {
                            Suppressions = new List<Suppression>(),
                        },
                    },
                },
                ExpectedResult = false,
            },

            new HasSuppressedResultsTestCase
            {
                Name = "Accepted suppression",
                Run = new Run
                {
                    Results = new List<Result>
                    {
                        new Result
                        {
                            Suppressions = new List<Suppression>
                            {
                                new Suppression
                                {
                                    Status = SuppressionStatus.Accepted,
                                },
                            },
                        },
                    },
                },
                ExpectedResult = true,
            },

            new HasSuppressedResultsTestCase
            {
                Name = "Suppression under review",
                Run = new Run
                {
                    Results = new List<Result>
                    {
                        new Result
                        {
                            Suppressions = new List<Suppression>
                            {
                                new Suppression
                                {
                                    Status = SuppressionStatus.UnderReview,
                                },
                            },
                        },
                    },
                },
                ExpectedResult = false,
            },

            new HasSuppressedResultsTestCase
            {
                Name = "Rejected suppression",
                Run = new Run
                {
                    Results = new List<Result>
                    {
                        new Result
                        {
                            Suppressions = new List<Suppression>
                            {
                                new Suppression
                                {
                                    Status = SuppressionStatus.Rejected,
                                },
                            },
                        },
                    },
                },
                ExpectedResult = false,
            },

            new HasSuppressedResultsTestCase
            {
                Name = "Suppressed result after result with rejected suppression",
                Run = new Run
                {
                    Results = new List<Result>
                    {
                        new Result
                        {
                            Suppressions = new List<Suppression>
                            {
                                new Suppression
                                {
                                    Status = SuppressionStatus.Rejected,
                                },
                            },
                        },

                        new Result
                        {
                            Suppressions = new List<Suppression>
                            {
                                new Suppression
                                {
                                    Status = SuppressionStatus.Accepted,
                                },
                            },
                        },
                    },
                },
                ExpectedResult = true,
            },

            new HasSuppressedResultsTestCase
            {
                Name = "Suppressed result after result with null suppressions",
                Run = new Run
                {
                    Results = new List<Result>
                    {
                        new Result(),

                        new Result
                        {
                            Suppressions = new List<Suppression>
                            {
                                new Suppression
                                {
                                    Status = SuppressionStatus.Accepted,
                                },
                            },
                        },
                    },
                },
                ExpectedResult = false,
            },

            new HasSuppressedResultsTestCase
            {
                Name = "Suppression both rejected and accepted",
                Run = new Run
                {
                    Results = new List<Result>
                    {
                        new Result(),

                        new Result
                        {
                            Suppressions = new List<Suppression>
                            {
                                new Suppression
                                {
                                    Status = SuppressionStatus.Accepted,
                                },
                                new Suppression
                                {
                                    Status = SuppressionStatus.Rejected,
                                },
                            },
                        },
                    },
                },
                ExpectedResult = false,
            },
        }.AsReadOnly();

        [Fact]
        public void HasAbsentResults_ReturnsExpectedValue()
        {
            var sb = new StringBuilder();

            foreach (HasAbsentResultsTestCase testCase in s_hasAbsentResultsTestCases)
            {
                bool actualResult = testCase.Run.HasAbsentResults();
                if (actualResult != testCase.ExpectedResult)
                {
                    sb.AppendLine($"    {testCase.Name}: expected {testCase.ExpectedResult} but got {actualResult}");
                }
                
                foreach(Result result in testCase.Run.Results?? Enumerable.Empty<Result>())
                {
                    _ = result.TryIsSuppressed(out bool suppressed);
                    result.IsSuppressed().Should().Be(suppressed);
                }
            }

            sb.Length.Should().Be(0, "failed test cases:\n" + sb.ToString());
        }

        [Fact]
        public void HasSuppressedResults_ReturnsExpectedValue()
        {
            var sb = new StringBuilder();

            foreach (HasSuppressedResultsTestCase testCase in s_hasSuppressedResultsTestCases)
            {
                bool actualResult = testCase.Run.HasSuppressedResults();
                if (actualResult != testCase.ExpectedResult)
                {
                    sb.AppendLine($"    {testCase.Name}: expected {testCase.ExpectedResult} but got {actualResult}");
                }
            }

            sb.Length.Should().Be(0, "failed test cases:\n" + sb.ToString());
        }

        private struct HasAbsentResultsTestCase
        {
            internal string Name;
            internal Run Run;
            internal bool ExpectedResult;
        }

        private struct HasSuppressedResultsTestCase
        {
            internal string Name;
            internal Run Run;
            internal bool ExpectedResult;
        }
    }
}
