// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;

using Xunit;

using XamlDoc = System.Windows.Documents;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class SarifErrorListItemTests
    {
        private const string FileName = "file.c";

        // Run object used in tests that don't require a populated run object.
        private static readonly Run EmptyRun = new Run();

        public SarifErrorListItemTests()
        {
            TestUtilities.InitializeTestEnvironment();
        }

        [Fact]
        public void SarifErrorListItem_WhenRegionHasStartLine_HasLineMarker()
        {
            var item = new SarifErrorListItem
            {
                FileName = "file.ext",
                Region = new Region
                {
                    StartLine = 5,
                },
            };

            ResultTextMarker lineMarker = item.LineMarker;

            lineMarker.Should().NotBe(null);
        }

        [Fact]
        public void SarifErrorListItem_WhenRegionHasNoStartLine_HasNoLineMarker()
        {
            var item = new SarifErrorListItem
            {
                FileName = "file.ext",
                Region = new Region
                {
                    ByteOffset = 20,
                },
            };

            ResultTextMarker lineMarker = item.LineMarker;

            lineMarker.Should().Be(null);
        }

        [Fact]
        public void SarifErrorListItem_WhenRegionIsAbsent_HasNoLineMarker()
        {
            var item = new SarifErrorListItem
            {
                FileName = "file.ext",
            };

            ResultTextMarker lineMarker = item.LineMarker;

            lineMarker.Should().Be(null);
        }

        [Fact]
        public void SarifErrorListItem_WhenMessageIsAbsent_ContainsBlankMessage()
        {
            var result = new Result();

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
                    Id = "nonExistentMessageId",
                },
                RuleId = "TST0001",
            };

            var run = new Run
            {
                Tool = new Tool(),
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
                    Id = "nonExistentMessageId",
                },
                RuleId = "TST0001",
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
                            },
                        },
                    },
                },
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
                    Id = "nonExistentFormatId",
                },
                RuleId = "TST0001",
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
                                    { "realFormatId", new MultiformatMessageString { Text = "The message" } },
                                },
                            },
                        },
                    },
                },
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
                        "Mary",
                    },
                    Id = "greeting",
                },
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
                                    { "greeting", new MultiformatMessageString { Text = "Hello, {0}!" } },
                                },
                            },
                        },
                    },
                },
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
                                    Uri = new Uri("path/to/file.html", UriKind.Relative),
                                },
                                Replacements = new[]
                                {
                                    new Replacement()
                                    {
                                        DeletedRegion = new Region
                                        {
                                            ByteLength = 5,
                                            ByteOffset = 10,
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Fixes.Should().BeEmpty();
            item.PopulateFixModelsIfNot();
            item.Fixes.Should().NotBeEmpty();
            item.Fixes[0].ArtifactChanges[0].FilePath.Should().Be("path/to/file.html");
        }

        [Fact]
        public void SarifErrorListItem_WhenRuleMetadataIsPresent_PopulatesRuleModelFromSarifRule()
        {
            var result = new Result
            {
                RuleId = "TST0001",
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
                            },
                        },
                    },
                },
            };

            SarifErrorListItem item = MakeErrorListItem(run, result);
            item.Fixes.Should().BeEmpty();
            item.PopulateFixModelsIfNot();
            item.Fixes.Should().BeEmpty();
            item.Rule.Id.Should().Be("TST0001");
        }

        [Fact]
        public void SarifErrorListItem_WhenRuleMetadataIsAbsent_SynthesizesRuleModelFromResultRuleId()
        {
            var result = new Result
            {
                RuleId = "TST0001",
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
                        },
                    },
                },
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
                RuleId = "TST0001",
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
                            },
                        },
                    },
                },
            };

            SarifErrorListItem item = MakeErrorListItem(run, result);

            item.Message.Should().Be(string.Empty);
        }

        [Fact]
        public void SarifErrorListItem_ResultMessageFormat_MultipleSentences()
        {
            const string s1 = "The quick brown fox.";
            const string s2 = "Jumps over the lazy dog.";
            var result = new Result
            {
                Message = new Message()
                {
                    Text = $"{s1} {s2}",
                },
            };

            SarifErrorListItem item = MakeErrorListItem(result);

            // with the change related to #360, message is not separated by sentence.
            item.Message.Should().Be($"{s1} {s2}");
            item.ShortMessage.Should().Be($"{s1} {s2}");
        }

        [Fact]
        public void SarifErrorListItem_ResultMessageFormat_NoTrailingPeriod()
        {
            const string s1 = "The quick brown fox";
            var result = new Result
            {
                Message = new Message()
                {
                    Text = s1,
                },
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Message.Should().Be(s1);
            item.ShortMessage.Should().Be(s1);
        }

        [Fact(Skip = "Obsoleted since in latest update extension does not truncate message anymore")]
        public void SarifErrorListItem_ResultMessageFormat_LongTextWithoutBreak()
        {
            // very long text without break like space or NewLine, longer than default max lengh 165
            const string s1 = "1234567890";
            var result = new Result
            {
                Message = new Message()
                {
                    Text = string.Concat(Enumerable.Repeat(s1, 20)), // replace s1 10 times, a string which length is 200
                },
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            int breakPoistion = 165;
            item.ShortMessage.Length.Should().Be(breakPoistion + 2);
            item.Message.Length.Should().Be(200 - breakPoistion);
        }

        [Fact(Skip = "Obsoleted since in latest update extension does not truncate message anymore")]
        public void SarifErrorListItem_ResultMessageFormat_LongTextSplitAtSpace()
        {
            // a string that index at Max Length (165) happens to be a space
            const string s1 = "In httplib2 before version 0.18.0, an attacker controlling unescaped part of uri for `httplib2.Http.request()` could change request headers and body, send additional hidden requests to same server. This vulnerability impacts software that uses httplib2 with uri constructed by string concatenation, as opposed to proper urllib building with escaping. This has been fixed in 0.18.0.";
            var result = new Result
            {
                Message = new Message()
                {
                    Text = s1,
                },
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            int breakPoistion = 165; // 0 indexed
            item.ShortMessage.Length.Should().Be(breakPoistion + 2); // 2 chars is added at end " \u2026"
            item.ShortMessage.Substring(0, breakPoistion).Should().Be(s1.Substring(0, breakPoistion));
            item.Message.Length.Should().Be(s1.Length - breakPoistion - 1);
            item.Message.Should().Be(s1.Substring(breakPoistion + 1)); // leading space trimmed
        }

        [Fact(Skip = "Obsoleted since in latest update extension does not truncate message anymore")]
        public void SarifErrorListItem_ResultMessageFormat_LongTextWitBreak()
        {
            // very long text has a space every 10 chars 
            const string s1 = "123456789 ";
            var result = new Result
            {
                Message = new Message()
                {
                    Text = string.Concat(Enumerable.Repeat(s1, 20)), // replace s1 10 times, a string which length is 200
                },
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            int breakPoistion = 159; // 0 indexed
            item.ShortMessage.Length.Should().Be(breakPoistion + 2); // the space break the text will be removed
            item.Message.Length.Should().Be(200 - breakPoistion - 2); // last space is trimmed
        }

        [Fact(Skip = "Obsoleted since in latest update extension does not truncate message anymore")]
        public void SarifErrorListItem_ResultMessageFormat_TextWitLinerBreak()
        {
            const string s0 = "The quick brown fox. Jumps over the lazy dog.";
            // text with varies style of line breakers 
            string s1 = "The\rquick brown fox." + Environment.NewLine + "Jumps over the lazy\ndog.";
            var result = new Result
            {
                Message = new Message()
                {
                    Text = s1,
                },
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Message.Should().Be(s0);
            item.ShortMessage.Should().Be(s0);
        }

        [Fact]
        public void SarifErrorListItem_HasLongEmbeddedLink()
        {
            const string s1 = "The quick brown fox. Jumps over the lazy dog. Reference to [docs](https://github.com/long/path/to/docs/1234567890/1234567890/1234567890/1234567890/1234567890/1234567890/1234567890/)";
            var result = new Result
            {
                Message = new Message()
                {
                    Text = s1,
                },
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.HasEmbeddedLinks.Should().BeTrue();

            // "The quick brown fox. Jumps over the lazy dog. Reference to ", "docs"
            item.MessageInlines.Count.Should().Be(2);
            item.MessageInlines[0].ContentStart.GetTextInRun(XamlDoc.LogicalDirection.Forward)
                .Should().BeEquivalentTo("The quick brown fox. Jumps over the lazy dog. Reference to ");

            var hyperlink = item.MessageInlines[1] as XamlDoc.Hyperlink;
            hyperlink.Inlines.First().ContentStart.GetTextInRun(XamlDoc.LogicalDirection.Forward)
                .Should().BeEquivalentTo("docs");
        }

        [Fact]
        public void SarifErrorListItem_HasEmbeddedLinks_MultipleSentencesWithEmbeddedLinks()
        {
            const string s1 = "The quick [brown](1) fox.";
            const string s2 = "Jumps over the [lazy](2) dog.";
            var result = new Result
            {
                Message = new Message()
                {
                    Text = $"{s1} {s2}",
                },
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.HasEmbeddedLinks.Should().BeTrue();

            // "The quick ", "brown", " fox. Jumps over the ", "lazy", " dog."
            item.MessageInlines.Count.Should().Be(5);
            item.MessageInlines[0].ContentStart.GetTextInRun(XamlDoc.LogicalDirection.Forward).Should().BeEquivalentTo("The quick ");
            var hyperlink = item.MessageInlines[1] as XamlDoc.Hyperlink;
            hyperlink.Inlines.First().ContentStart.GetTextInRun(XamlDoc.LogicalDirection.Forward).Should().BeEquivalentTo("brown");
            item.MessageInlines[2].ContentStart.GetTextInRun(XamlDoc.LogicalDirection.Forward).Should().BeEquivalentTo(" fox. Jumps over the ");
            hyperlink = item.MessageInlines[3] as XamlDoc.Hyperlink;
            hyperlink.Inlines.First().ContentStart.GetTextInRun(XamlDoc.LogicalDirection.Forward).Should().BeEquivalentTo("lazy");
            item.MessageInlines[4].ContentStart.GetTextInRun(XamlDoc.LogicalDirection.Forward).Should().BeEquivalentTo(" dog.");

        }

        [Fact]
        public void SarifErrorListItem_HelpLinkShouldBeSameAs_RuleHelpUri()
        {
            string link = "https://example.com/TST0001";
            var result = new Result
            {
                Message = new Message
                {
                    Text = "The quick brown fox.",
                },
                RuleId = "TST0001",
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
                                HelpUri = new Uri(link),
                            },
                        },
                    },
                },
            };

            SarifErrorListItem item = MakeErrorListItem(run, result);

            item.HelpLink.Should().NotBeNull();
            item.HelpLink.Should().BeEquivalentTo(link);

            // without help Uri
            run = new Run
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
                            },
                        },
                    },
                },
            };

            item = MakeErrorListItem(run, result);
            item.HelpLink.Should().BeNull();
        }

        [Fact]
        public void SarifErrorListItem_TreatsInformationalResultAsNote()
        {
            var result = new Result
            {
                Kind = ResultKind.Informational,
                Level = FailureLevel.None,
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
                Level = FailureLevel.None,
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Level.Should().Be(FailureLevel.Note);
        }

        [Fact]
        public void SarifErrorListItem_TreatsPassResultAsNote()
        {
            var result = new Result
            {
                Kind = ResultKind.Pass,
                Level = FailureLevel.None,
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
                Level = FailureLevel.None,
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
                Level = FailureLevel.None,
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
                Kind = ResultKind.Fail,
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Level.Should().Be(FailureLevel.Error);
        }

        [Fact]
        public void SarifErrorListItem_WhenThereAreNoFixes_IsNotFixable()
        {
            var result = new Result();

            SarifErrorListItem item = MakeErrorListItem(result);

            item.IsFixable().Should().BeFalse();
        }

        [Fact]
        public void SarifErrorListItem_WhenFixIsConfinedToOneFile_IsFixable()
        {
            var result = new Result
            {
                Fixes = new List<Fix>
                {
                    new Fix
                    {
                        ArtifactChanges = new List<ArtifactChange>
                        {
                            new ArtifactChange
                            {
                                ArtifactLocation = new ArtifactLocation
                                {
                                    Uri = new Uri(FileName, UriKind.Relative),
                                },
                                Replacements = new List<Replacement>
                                {
                                    new Replacement
                                    {
                                        DeletedRegion = new Region
                                        {
                                            CharOffset = 52,
                                            CharLength = 5,
                                        },
                                    },
                                },
                            },
                            new ArtifactChange
                            {
                                ArtifactLocation = new ArtifactLocation
                                {
                                    Uri = new Uri(FileName, UriKind.Relative),
                                },
                                Replacements = new List<Replacement>
                                {
                                    new Replacement
                                    {
                                        DeletedRegion = new Region
                                        {
                                            CharOffset = 52,
                                            CharLength = 5,
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Add(item.RunIndex, new RunDataCache());
            item.Fixes.Should().BeEmpty();
            item.PopulateFixModelsIfNot();
            item.Fixes.Should().NotBeEmpty();
            item.IsFixable().Should().BeTrue();
        }

        [Fact]
        public void SarifErrorListItem_WhenFixSpansMultipleFiles_IsNotFixable()
        {
            var result = new Result
            {
                Fixes = new List<Fix>
                {
                    new Fix
                    {
                        ArtifactChanges = new List<ArtifactChange>
                        {
                            new ArtifactChange
                            {
                                ArtifactLocation = new ArtifactLocation
                                {
                                    Uri = new Uri(FileName, UriKind.Relative),
                                },
                                Replacements = new List<Replacement>
                                {
                                    new Replacement
                                    {
                                        DeletedRegion = new Region
                                        {
                                            CharOffset = 52,
                                            CharLength = 5,
                                        },
                                    },
                                },
                            },
                            new ArtifactChange
                            {
                                ArtifactLocation = new ArtifactLocation
                                {
                                    Uri = new Uri("SomeOther" + FileName, UriKind.Relative),
                                },
                                Replacements = new List<Replacement>
                                {
                                    new Replacement
                                    {
                                        DeletedRegion = new Region
                                        {
                                            CharOffset = 52,
                                            CharLength = 5,
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            SarifErrorListItem item = MakeErrorListItem(result);

            item.IsFixable().Should().BeFalse();
        }

        [Fact]
        public void SarifErrorListItem_WithoutPropertyBags_EmptyProperties()
        {
            var result = new Result();

            SarifErrorListItem item = MakeErrorListItem(result);

            item.Properties.Should().NotBeNull();
            item.Properties.Should().BeEmpty();
        }

        [Fact]
        public void SarifErrorListItem_WithAdditionalProperties_ShouldDeferLoading()
        {
            string moduleName = "Microsoft.CodeAnalysis.Sarif.PatternMatcher.AnalyzeCommand";
            string relatedLocationString = "location related to the issue";
            var result = new Result
            {
                Locations = new List<Location>
                {
                    new Location
                    {
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation { Uri = new Uri("path/to/file", UriKind.Relative) },
                        },
                    },
                },
                RelatedLocations = new List<Location>
                {
                    new Location
                    {
                        Message = new Message { Text = relatedLocationString },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation { Description = new Message { Text = relatedLocationString } },
                        },
                    },
                    new Location
                    {
                        Message = new Message { Text = relatedLocationString },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation { Description = new Message { Text = relatedLocationString } },
                        },
                    },
                    new Location
                    {
                        Message = new Message { Text = relatedLocationString },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation { Description = new Message { Text = relatedLocationString } },
                        },
                    },
                },
                Stacks = new List<Stack>
                {
                    new Stack
                    {
                        Frames = new List<StackFrame>
                        {
                            new StackFrame { Module = moduleName },
                        },
                    },
                },
                CodeFlows = new List<CodeFlow>
                {
                    new CodeFlow
                    {
                        Message = new Message { Text = "'*Temp_value_#8086' is not initialized" },
                    },
                    new CodeFlow
                    {
                        Message = new Message { Text = "'*Temp_value_#8086' is used, but may not have been initialized" },
                    },
                },
            };

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Locations.Should().BeEmpty();
            item.RelatedLocations.Should().BeEmpty();
            item.Stacks.Should().BeEmpty();
            item.AnalysisSteps.Should().BeEmpty();

            item.PopulateAdditionalPropertiesIfNot();

            item.Locations.Count.Should().Be(1);
            item.Locations.First().FilePath.Should().BeEquivalentTo("path/to/file");
            item.RelatedLocations.Count.Should().Be(3);
            item.RelatedLocations.First().Message.Should().BeEquivalentTo(relatedLocationString);
            item.Stacks.Count.Should().Be(1);
            item.Stacks.First().First().Module.Should().BeEquivalentTo(moduleName);
            item.AnalysisSteps.Count.Should().Be(2);
            item.AnalysisSteps.Verbosity.Should().Be(100);
        }

        [Fact]
        public void SarifErrorListItem_WithPropertyBags_HasProperties()
        {
            var result = new Result();
            result.SetProperty("text", "test strings");
            result.SetProperty<bool>("validated", true);
            result.SetProperty<double>("totalAmount", 344.235d);
            result.SetProperty<int[]>("params", new int[] { -1, 2, 3 });
            result.SetProperty<object>("nullObj", null);
            result.SetProperty<dynamic>("object", new { foo = "bar" });

            SarifErrorListItem item = MakeErrorListItem(result);
            item.Properties.Should().BeEmpty();

            item.PopulateAdditionalPropertiesIfNot();

            item.Properties.Should().NotBeNull();
            item.Properties.Count.Should().Be(6);
            item.Properties.First(kv => kv.Key == "text").Value.Should().BeEquivalentTo("\"test strings\"");
            item.Properties.First(kv => kv.Key == "validated").Value.Should().BeEquivalentTo("true");
            item.Properties.First(kv => kv.Key == "totalAmount").Value.Should().BeEquivalentTo("344.235");
            item.Properties.First(kv => kv.Key == "params").Value.Should().BeEquivalentTo("[-1,2,3]");
            item.Properties.First(kv => kv.Key == "nullObj").Value.Should().BeNull();
            item.Properties.First(kv => kv.Key == "object").Value.Should().BeEquivalentTo("{\"foo\":\"bar\"}");
        }

        private static SarifErrorListItem MakeErrorListItem(Result result)
        {
            return MakeErrorListItem(EmptyRun, result);
        }

        private static SarifErrorListItem MakeErrorListItem(Run run, Result result)
        {
            result.Run = run;
            return new SarifErrorListItem(
                run,
                runIndex: 0,
                result: result,
                logFilePath: "log.sarif",
                projectNameCache: new ProjectNameCache(solution: null))
            {
                FileName = FileName,
            };
        }
    }
}
