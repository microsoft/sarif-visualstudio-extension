// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;

using Moq;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.Sarif
{
    public class ReplacementExtensionsTests
    {
        private const string FilePath = @"c:\file.cs";
        private const string RelativeFilePath = @"\\file.cs";
        private const string ReplacementText = "public class";
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
        public void ToReplacementModel_NullReplacement_ReturnNull()
        {
            var mock = new Mock<IFileSystem>();
            mock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
            mock.Setup(fs => fs.FileReadAllText(It.IsAny<string>())).Returns(CodeSample);
            var regionCache = new FileRegionsCache(fileSystem: mock.Object);

            Uri uri = new Uri(FilePath);

            Replacement replacement = null;
            ReplacementModel model = replacement.ToReplacementModel(regionCache, uri);
            model.Should().BeNull();
        }

        [Fact]
        public void ToReplacementModel_RelativeUri_BinaryReplacement()
        {
            var mock = new Mock<IFileSystem>();
            mock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
            mock.Setup(fs => fs.FileReadAllText(It.IsAny<string>())).Returns(CodeSample);
            var regionCache = new FileRegionsCache(fileSystem: mock.Object);

            Uri uri = new Uri(RelativeFilePath, UriKind.Relative);
            byte[] bytes = Encoding.UTF8.GetBytes(ReplacementText);

            Replacement replacement = new Replacement
            {
                DeletedRegion = new Region
                {
                    ByteOffset = 210,
                },
                InsertedContent = new ArtifactContent
                {
                    Binary = Convert.ToBase64String(bytes),
                },
            };
            ReplacementModel model = replacement.ToReplacementModel(regionCache, uri);
            model.Should().NotBeNull();
            model.InsertedString.Should().BeNull();
            model.InsertedBytes.Should().BeEquivalentTo(bytes);
            model.Region.Should().NotBeNull();
            model.Region.IsBinaryRegion.Should().BeTrue();
            model.Region.CharOffset.Should().Be(-1);
            model.Region.ByteOffset.Should().Be(210);
        }

        [Fact]
        public void ToReplacementModel_RelativeUri_TextReplacement()
        {
            var mock = new Mock<IFileSystem>();
            mock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
            mock.Setup(fs => fs.FileReadAllText(It.IsAny<string>())).Returns(CodeSample);
            var regionCache = new FileRegionsCache(fileSystem: mock.Object);

            Uri uri = new Uri(RelativeFilePath, UriKind.Relative);

            Replacement replacement = new Replacement
            {
                DeletedRegion = new Region
                {
                    CharOffset = 196,
                    CharLength = 14,
                },
                InsertedContent = new ArtifactContent
                {
                    Text = ReplacementText,
                },
            };
            ReplacementModel model = replacement.ToReplacementModel(regionCache, uri);
            model.Should().NotBeNull();
            model.InsertedBytes.Should().BeNull();
            model.InsertedString.Should().BeEquivalentTo(ReplacementText);
            model.Region.Should().NotBeNull();
            model.Region.CharLength.Should().Be(14);
            model.Region.CharOffset.Should().Be(196);
            model.Region.IsBinaryRegion.Should().BeFalse();
            model.Region.ByteOffset.Should().Be(-1);
        }
    }
}
