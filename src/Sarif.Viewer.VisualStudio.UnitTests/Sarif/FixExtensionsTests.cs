// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;

using Moq;

using Xunit;


namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.Sarif
{
    public class FixExtensionsTests
    {
        private const string FilePath = @"c:\file.cs";
        private const string AnotherFilePath = @"c:\anotherfile.cs";

        private const string CodeSample =
@"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace AnalysisTestProject2
{
    internal class Class4
    {
    }
}";
        [Fact]
        public void ToFixModel_FileExists_FixIsApplyable()
        {
            var mock = new Mock<IFileSystem>();
            mock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            mock.Setup(fs => fs.FileReadAllText(It.IsAny<string>())).Returns(CodeSample);
            var regionCache = new FileRegionsCache(fileSystem: mock.Object);

            string descriptionString = "test description";
            string replacementText = "public class";

            var fix = new Fix
            {
                Description = new Message { Text = descriptionString },
                ArtifactChanges = new List<ArtifactChange>
                {
                    new ArtifactChange
                    {
                        ArtifactLocation = new ArtifactLocation
                        {
                            Uri = new Uri(FilePath, UriKind.Absolute),
                        },
                        Replacements = new List<Replacement>
                        {
                            new Replacement
                            {
                                DeletedRegion = new Region
                                {
                                    CharOffset = 196,
                                    CharLength = 14,
                                },
                                InsertedContent = new ArtifactContent
                                {
                                    Text = replacementText
                                },
                            },
                        },
                    },
                },
            }.ToFixModel(originalUriBaseIds: null, regionCache);

            fix.Description.Should().BeEquivalentTo(descriptionString);
            fix.ArtifactChanges.Count.Should().Be(1);
            fix.CanBeAppliedToFile(FilePath).Should().BeTrue();
            ObservableCollection<ReplacementModel> replacements = fix.ArtifactChanges.FirstOrDefault().Replacements;

            // input replacement is charoffset based. output replacement should have start/end line/column info.
            replacements.Count.Should().Be(1);
            ReplacementModel replacement = replacements.FirstOrDefault();
            replacement.InsertedString.Should().BeEquivalentTo(replacementText);
            replacement.Region.StartLine.Should().Be(6);
            replacement.Region.EndLine.Should().Be(6);
            replacement.Region.StartColumn.Should().Be(7);
            replacement.Region.EndColumn.Should().Be(21);
        }

        [Fact]
        public void ToFixModel_FileDoesNotExists_FixIsNotApplyable()
        {
            var mock = new Mock<IFileSystem>();
            mock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
            mock.Setup(fs => fs.FileReadAllText(It.IsAny<string>())).Returns(string.Empty);
            var regionCache = new FileRegionsCache(fileSystem: mock.Object);

            string descriptionString = "test description";
            string replacementText = "public class";

            var fix = new Fix
            {
                Description = new Message { Text = descriptionString },
                ArtifactChanges = new List<ArtifactChange>
                {
                    new ArtifactChange
                    {
                        ArtifactLocation = new ArtifactLocation
                        {
                            Uri = new Uri(AnotherFilePath, UriKind.Absolute),
                        },
                        Replacements = new List<Replacement>
                        {
                            new Replacement
                            {
                                DeletedRegion = new Region
                                {
                                    CharOffset = 196,
                                    CharLength = 14,
                                },
                                InsertedContent = new ArtifactContent
                                {
                                    Text = replacementText
                                },
                            },
                        },
                    },
                },
            }.ToFixModel(originalUriBaseIds: null, regionCache);

            fix.Description.Should().BeEquivalentTo(descriptionString);
            fix.ArtifactChanges.Count.Should().Be(1);
            fix.CanBeAppliedToFile(FilePath).Should().BeFalse();
            ObservableCollection<ReplacementModel> replacements = fix.ArtifactChanges.FirstOrDefault().Replacements;
            // file doesn't exist, output replacement should have be same as input region.
            replacements.Count.Should().Be(1);
            ReplacementModel replacement = replacements.FirstOrDefault();
            replacement.InsertedString.Should().BeEquivalentTo(replacementText);
            replacement.Region.CharOffset.Should().Be(196);
            replacement.Region.CharLength.Should().Be(14);
            replacement.Region.StartLine.Should().Be(0);
            replacement.Region.EndLine.Should().Be(0);
            replacement.Region.StartColumn.Should().Be(0);
            replacement.Region.EndColumn.Should().Be(0);
        }
    }
}
