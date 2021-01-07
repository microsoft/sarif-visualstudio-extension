// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class ExceptionalConditionsCalculatorTests
    {
        private struct TestCase
        {
            internal string Name;
            internal SarifLog Log;
            internal ExceptionalConditions ExpectedResult;
        }

        private static readonly ReadOnlyCollection<TestCase> s_testCases = new List<TestCase>
        {
            new TestCase
            {
                Name = "Null log",
                Log = null,
                ExpectedResult = ExceptionalConditions.InvalidJson
            },

            new TestCase
            {
                Name = "Null runs",
                Log = new SarifLog(),
                ExpectedResult = ExceptionalConditions.NoResults
            },

            new TestCase
            {
                Name = "Empty runs",
                Log = new SarifLog
                {
                    Runs = new List<Run>()
                },
                ExpectedResult = ExceptionalConditions.NoResults
            },

            new TestCase
            {
                Name = "Run with null results",
                Log = new SarifLog
                {
                    Runs = new List<Run>
                    {
                        new Run()
                    }
                },
                ExpectedResult = ExceptionalConditions.NoResults
            },

            new TestCase
            {
                Name = "Run with empty results",
                Log = new SarifLog
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Results = new List<Result>()
                        }
                    }
                },
                ExpectedResult = ExceptionalConditions.NoResults
            },

            new TestCase
            {
                Name = "Run with non-empty results",
                Log = new SarifLog
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Results = new List<Result>
                            {
                                new Result()
                            }
                        }
                    }
                },
                ExpectedResult = ExceptionalConditions.None
            },

            new TestCase
            {
                Name = "Runs with empty and non-empty results",
                Log = new SarifLog
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Results = new List<Result>()
                        },

                        new Run
                        {
                            Results = new List<Result>
                            {
                                new Result()
                            }
                        }
                    }
                },
                ExpectedResult = ExceptionalConditions.None
            },

            new TestCase
            {
                Name = "Run with empty configuration notifications",
                Log = new SarifLog
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Results = new List<Result>
                            {
                                new Result()
                            },
                            Invocations = new List<Invocation>
                            {
                                new Invocation
                                {
                                    ToolConfigurationNotifications = new List<Notification>()
                                }
                            }
                        }
                    }
                },
                ExpectedResult = ExceptionalConditions.None
            },

            new TestCase
            {
                Name = "Run with warning-level configuration notifications",
                Log = new SarifLog
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Results = new List<Result>
                            {
                                new Result()
                            },
                            Invocations = new List<Invocation>
                            {
                                new Invocation
                                {
                                    ToolConfigurationNotifications = new List<Notification>
                                    {
                                        new Notification
                                        {
                                            Level = FailureLevel.Warning
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ExpectedResult = ExceptionalConditions.None
            },

            new TestCase
            {
                Name = "Run with error-level configuration notifications",
                Log = new SarifLog
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Results = new List<Result>
                            {
                                new Result()
                            },
                            Invocations = new List<Invocation>
                            {
                                new Invocation
                                {
                                    ToolConfigurationNotifications = new List<Notification>
                                    {
                                        new Notification
                                        {
                                            Level = FailureLevel.Error
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ExpectedResult = ExceptionalConditions.ConfigurationError
            },

            new TestCase
            {
                Name = "Run with error-level configuration notifications and no results",
                Log = new SarifLog
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Invocations = new List<Invocation>
                            {
                                new Invocation
                                {
                                    ToolConfigurationNotifications = new List<Notification>
                                    {
                                        new Notification
                                        {
                                            Level = FailureLevel.Error
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ExpectedResult = ExceptionalConditions.ConfigurationError
            },

            new TestCase
            {
                Name = "Run with empty execution notifications",
                Log = new SarifLog
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Results = new List<Result>
                            {
                                new Result()
                            },
                            Invocations = new List<Invocation>
                            {
                                new Invocation
                                {
                                    ToolExecutionNotifications = new List<Notification>()
                                }
                            }
                        }
                    }
                },
                ExpectedResult = ExceptionalConditions.None
            },

            new TestCase
            {
                Name = "Run with warning-level execution notifications",
                Log = new SarifLog
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Results = new List<Result>
                            {
                                new Result()
                            },
                            Invocations = new List<Invocation>
                            {
                                new Invocation
                                {
                                    ToolExecutionNotifications = new List<Notification>
                                    {
                                        new Notification
                                        {
                                            Level = FailureLevel.Warning
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ExpectedResult = ExceptionalConditions.None
            },

            new TestCase
            {
                Name = "Run with error-level execution notifications",
                Log = new SarifLog
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Results = new List<Result>
                            {
                                new Result()
                            },
                            Invocations = new List<Invocation>
                            {
                                new Invocation
                                {
                                    ToolExecutionNotifications = new List<Notification>
                                    {
                                        new Notification
                                        {
                                            Level = FailureLevel.Error
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ExpectedResult = ExceptionalConditions.ExecutionError
            },

            new TestCase
            {
                Name = "Run with error-level execution notifications and no results",
                Log = new SarifLog
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Invocations = new List<Invocation>
                            {
                                new Invocation
                                {
                                    ToolExecutionNotifications = new List<Notification>
                                    {
                                        new Notification
                                        {
                                            Level = FailureLevel.Error
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ExpectedResult = ExceptionalConditions.ExecutionError
            },

            new TestCase
            {
                Name = "Run with error-level configuration notifications, error-level execution notifications, and no results",
                Log = new SarifLog
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Invocations = new List<Invocation>
                            {
                                new Invocation
                                {
                                    ToolConfigurationNotifications = new List<Notification>
                                    {
                                        new Notification
                                        {
                                            Level = FailureLevel.Error
                                        }
                                    },
                                    ToolExecutionNotifications = new List<Notification>
                                    {
                                        new Notification
                                        {
                                            Level = FailureLevel.Error
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ExpectedResult = ExceptionalConditions.ExecutionError | ExceptionalConditions.ConfigurationError
            },
        }.AsReadOnly();

        [Fact]
        public void ExceptionalConditionsCalculator_ProducesExpectedResults()
        {
            var sb = new StringBuilder();

            foreach (TestCase testCase in s_testCases)
            {
                ExceptionalConditions actualResult = ExceptionalConditionsCalculator.Calculate(testCase.Log);
                if (actualResult != testCase.ExpectedResult)
                {
                    sb.Append("    ").Append(testCase.Name).Append(": expected ").Append(testCase.ExpectedResult)
                        .Append(" but got ").Append(actualResult).AppendLine();
                }
            }

            sb.Length.Should().Be(0, "failed test cases:\n" + sb.ToString());
        }

        [Fact]
        public void ExceptionalConditionsCalculator_OnMultipleLogs_MergesConditions()
        {
            var logs = new List<SarifLog>
            {
                new SarifLog // Run with no results.
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Results = new List<Result>()
                        }
                    }
                },
                new SarifLog // Run with both configuration and execution errors.
                {
                    Runs = new List<Run>
                    {
                        new Run
                        {
                            Results = new List<Result>
                            {
                                new Result()
                            },
                            Invocations = new List<Invocation>
                            {
                                new Invocation
                                {
                                    ToolConfigurationNotifications = new List<Notification>
                                    {
                                        new Notification
                                        {
                                            Level = FailureLevel.Error
                                        }
                                    },
                                    ToolExecutionNotifications = new List<Notification>
                                    {
                                        new Notification
                                        {
                                            Level = FailureLevel.Error
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ExceptionalConditionsCalculator.Calculate(logs).
                Should().Be(ExceptionalConditions.NoResults | ExceptionalConditions.ConfigurationError | ExceptionalConditions.ExecutionError);
        }
    }
}
