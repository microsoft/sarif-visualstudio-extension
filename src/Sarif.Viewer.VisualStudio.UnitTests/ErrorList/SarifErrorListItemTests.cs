// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.CodeAnalysis.Sarif;
using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class SarifErrorListItemTests
    {
        public SarifErrorListItemTests()
        {
            TestUtilities.InitializeTestEnvironment();
        }

        [Fact]
        public void SarifErrorListItem_WhenRegionHasStartLine_HasLineMarker()
        {
            SarifErrorListItem item = new SarifErrorListItem
            {
                FileName = "file.ext",
                Region = new Region
                {
                    StartLine = 5
                }
            };

            var lineMarker = item.LineMarker;

            lineMarker.Should().NotBe(null);
        }

        [Fact]
        public void SarifErrorListItem_WhenRegionHasNoStartLine_HasNoLineMarker()
        {
            SarifErrorListItem item = new SarifErrorListItem
            {
                FileName = "file.ext",
                Region = new Region
                {
                    ByteOffset = 20
                }
            };

            var lineMarker = item.LineMarker;

            lineMarker.Should().Be(null);
        }

        [Fact]
        public void SarifErrorListItem_WhenRegionIsAbsent_HasNoLineMarker()
        {
            SarifErrorListItem item = new SarifErrorListItem
            {
                FileName = "file.ext"
            };

            var lineMarker = item.LineMarker;

            lineMarker.Should().Be(null);
        }

        [Fact]
        public void SarifErrorListItem_WhenMessageIsAbsent_ContainsBlankMessage()
        {
            var result = new Result
            {
            };

            SarifErrorListItem item = MakeErrorListItem(result);

            item.Message.Should().Be(string.Empty);
        }

        [Fact]
        public void SarifErrorListItem_WhenResultRefersToNonExistentRule_ContainsBlankMessage()
        {
            var result = new Result
            {
                Message = new Message
                {
                    Id = "nonExistentMessageId"
                },
                RuleId = "TST0001"
            };

            var run = new Run
            {
                Tool = new Tool()
            };

            SarifErrorListItem item = MakeErrorListItem(run, result);

            item.Message.Should().Be(string.Empty);
        }

        [Fact]
        public void SarifErrorListItem_WhenResultRefersToRuleWithNoMessageStrings_ContainsBlankMessage()
        {
            var result = new Result
            {
                Message = new Message
                {
                    Id = "nonExistentMessageId"
                },
                RuleId = "TST0001"
            };

            var run = new Run
            {
                Tool = new Tool
                {
                    Driver = new ToolComponent
                    {
                        Rules = new List<ReportingDescriptor>
                    {
                        new ReportingDescriptor
                        {
                            Id = "TST0001"
                        }
                    }
                    }
                }
            };

            SarifErrorListItem item = MakeErrorListItem(run, result);

            item.Message.Should().Be(string.Empty);
        }

        [Fact]
        public void SarifErrorListItem_WhenResultRefersToNonExistentMessageFormat_ContainsBlankMessage()
        {
            var result = new Result
            {
                Message = new Message
                {
                    Id = "nonExistentFormatId"
                },
                RuleId = "TST0001"
            };

            var run = new Run
            {
                Tool = new Tool
                {
                    Driver = new ToolComponent
                    {
                        Rules = new List<ReportingDescriptor>
                        {
                            new ReportingDescriptor
                            {
                                Id = "TST0001",
                                MessageStrings = new Dictionary<string, MultiformatMessageString>
                                {
                                    { "realFormatId", new MultiformatMessageString { Text = "The message" } }
                                }
                            }
                        }
                    }
                }
            };

            SarifErrorListItem item = MakeErrorListItem(run, result);

            item.Message.Should().Be(string.Empty);
        }
        
        [Fact]
        public void SarifErrorListItem_WhenResultRefersToExistingMessageString_ContainsExpectedMessage()
        {
            var result = new Result
            {
                RuleId = "TST0001", 
                Message = new Message()
                {
                    Arguments = new string[]
                    {
                        "Mary"
                    },
                    Id = "greeting"
                }
            };

            var run = new Run
            {
                Tool = new Tool
                {
                    Driver = new ToolComponent
                    {
                        Rules = new List<ReportingDescriptor>
                        {
                            new ReportingDescriptor
                            {
                                Id = "TST0001",
                                MessageStrings = new Dictionary<string, MultiformatMessageString>
                                {
                                    { "greeting", new MultiformatMessageString { Text = "Hello, {0}!" } }
                                }
                            }
                        }
                    }
                }
            };

            SarifErrorListItem item = MakeErrorListItem(run, result);

            item.Message.Should().Be("Hello, Mary!");
        }

        [Fact]
        public void SarifErrorListItem_WhenFixHasRelativePath_UsesThatPath()
        {
            var result = new Result
            {
                Fixes = new[]
                {
                    new Fix
                    {
                        ArtifactChanges = new[]
                        {
                            new ArtifactChange
                            {
                                ArtifactLocation = new ArtifactLocation
                                {
                                    Uri = new Uri("path/to/file.html", UriKind.Relative)
                                },
                                Replacements = new[]
                                {
                                    new Replacement()
                                    {
                                        DeletedRegion = new Region
                                        {
                                            ByteLength = 5,
                                            ByteOffset = 10
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            SarifErrorListItem item = MakeErrorListItem(result);

            item.Fixes[0].ArtifactChanges[0].FilePath.Should().Be("path/to/file.html");
        }

        [Fact]
        public void SarifErrorListItem_WhenRuleMetadataIsPresent_PopulatesRuleModelFromSarifRule()
        {
            var result = new Result
            {
                RuleId = "TST0001"
            };

            var run = new Run
            {
                Tool = new Tool
                {
                    Driver = new ToolComponent
                    {
                        Rules = new List<ReportingDescriptor>
                        {
                            new ReportingDescriptor
                            {
                                Id = "TST0001"
                            }
                        }
                    }
                }
            };

            SarifErrorListItem item = MakeErrorListItem(run, result);

            item.Rule.Id.Should().Be("TST0001");
        }

        [Fact]
        public void SarifErrorListItem_WhenRuleMetadataIsAbsent_SynthesizesRuleModelFromResultRuleId()
        {
            var result = new Result
            {
                RuleId = "TST0001"
            };

            var run = new Run
            {
                Tool = new Tool
                {
                    Driver = new ToolComponent
                    {
                        Rules = new List<ReportingDescriptor>
                        {
                            // No metadata for rule TST0001.
                        }
                    }
                }
            };

            SarifErrorListItem item = MakeErrorListItem(run, result);

            item.Rule.Id.Should().Be("TST0001");
        }

        [Fact]
        public void SarifErrorListItem_WhenMessageAndFormattedRuleMessageAreAbsentButRuleMetadataIsPresent_ContainsBlankMessage()
        {
            // This test prevents regression of #647,
            // "Viewer NRE when result lacks message/formattedRuleMessage but rule metadata is present"
            var result = new Result
            {
                RuleId = "TST0001"
            };

            var run = new Run
            {
                Tool = new Tool
                {
                    Driver = new ToolComponent
                    {
                        Rules = new List<ReportingDescriptor>
                        {
                            new ReportingDescriptor
                            {
                                Id = "TST0001"
                            }
                        }
                    }
                }
            };

            SarifErrorListItem item = MakeErrorListItem(run, result);

            item.Message.Should().Be(string.Empty);
        }

        [Fact]
        public void SarifErrorListItem_ResultMessageFormat_MultipleSentences()
        {
            string s1 = "The quick brown fox.";
            string s2 = "Jumps over the lazy dog.";
            var result = new Result
            {
                Message = new Message()
                {
                    Text = $"{s1} {s2}"
                }
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Message.Should().Be($"{s1} {s2}");
            item.ShortMessage.Should().Be(s1);
        }

        [Fact]
        public void SarifErrorListItem_ResultMessageFormat_NoTrailingPeriod()
        {
            string s1 = "The quick brown fox";
            var result = new Result
            {
                Message = new Message()
                {
                    Text = s1
                }
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Message.Should().Be(s1);
            item.ShortMessage.Should().Be(s1);
        }

        [Fact]
        public void SarifErrorListItem_HasEmbeddedLinks_MultipleSentencesWithEmbeddedLinks()
        {
            string s1 = "The quick [brown](1) fox.";
            string s2 = "Jumps over the [lazy](2) dog.";
            var result = new Result
            {
                Message = new Message()
                {
                    Text = $"{s1} {s2}"
                }
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.HasEmbeddedLinks.Should().BeTrue();
        }

        [Fact]
        public void SarifErrorListItem_TreatsInformationalResultAsNote()
        {
            var result = new Result
            {
                Kind = ResultKind.Informational,
                Level = FailureLevel.None
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Level.Should().Be(FailureLevel.Note);
        }

        [Fact]
        public void SarifErrorListItem_TreatsNotApplicableResultAsNote()
        {
            var result = new Result
            {
                Kind = ResultKind.NotApplicable,
                Level = FailureLevel.None
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Level.Should().Be(FailureLevel.Note);
        }

        [Fact]
        public void SarifErrorListItem_TreatsOpenResultAsWarning()
        {
            var result = new Result
            {
                Kind = ResultKind.Open,
                Level = FailureLevel.None
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Level.Should().Be(FailureLevel.Warning);
        }

        [Fact]
        public void SarifErrorListItem_TreatsReviewResultAsWarning()
        {
            var result = new Result
            {
                Kind = ResultKind.Review,
                Level = FailureLevel.None
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Level.Should().Be(FailureLevel.Warning);
        }

        [Fact]
        public void SarifErrorListItem_TreatsFailResultAccordingToLevel()
        {
            var result = new Result
            {
                Level = FailureLevel.Error,
                Kind = ResultKind.Fail
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Level.Should().Be(FailureLevel.Error);
        }

        // Run object used in tests that don't require a populated run object.
        private static readonly Run EmptyRun = new Run();

        private static SarifErrorListItem MakeErrorListItem(Result result)
        {
            return MakeErrorListItem(EmptyRun, result);
        }

        private static SarifErrorListItem MakeErrorListItem(Run run, Result result)
        {
            result.Run = run;
            return new SarifErrorListItem(
                run,
                result,
                "log.sarif",
                new ProjectNameCache(solution: null));
        }
    }
}