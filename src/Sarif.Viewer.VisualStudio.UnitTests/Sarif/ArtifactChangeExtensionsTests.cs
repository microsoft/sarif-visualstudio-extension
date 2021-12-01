// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.Sarif
{
    public class ArtifactChangeExtensionsTests
    {
        [Fact]
        public void ToArtifactChangeModel_ArtifactChangeIsNull()
        {
            ArtifactChange artifactChange = null;

            var model = artifactChange.ToArtifactChangeModel(new Dictionary<string, ArtifactLocation>(), new FileRegionsCache());
            model.Should().BeNull();

            model = artifactChange.ToArtifactChangeModel(new Dictionary<string, Uri>(), new FileRegionsCache());
            model.Should().BeNull();
        }

        [Fact]
        public void ToArtifactChangeModel_ArtifactLocationAbsolutePath()
        {
            string absolutePath = "file:///etc/path/to/file1";
            ArtifactChange artifactChange = new ArtifactChange
            {
                ArtifactLocation = new ArtifactLocation
                {
                    Uri = new Uri(absolutePath, UriKind.Absolute),
                }
            };

            var model = artifactChange.ToArtifactChangeModel(new Dictionary<string, ArtifactLocation>(), new FileRegionsCache());
            model.Should().NotBeNull();
            model.FilePath.Should().BeEquivalentTo("/etc/path/to/file1");
            model.FileName.Should().BeEquivalentTo("file1");
        }

        [Fact]
        public void ToArtifactChangeModel_ArtifactLocationRelativePath()
        {
            string relativePath = "path/to/file1";
            ArtifactChange artifactChange = new ArtifactChange
            {
                ArtifactLocation = new ArtifactLocation
                {
                    Uri = new Uri(relativePath, UriKind.Relative),
                }
            };

            var model = artifactChange.ToArtifactChangeModel(new Dictionary<string, ArtifactLocation>(), new FileRegionsCache());
            model.Should().NotBeNull();
            model.FilePath.Should().BeEquivalentTo(relativePath);
            model.FileName.Should().BeEquivalentTo("file1");
        }

        [Fact]
        public void ToArtifactChangeModel_WithReplacement()
        {
            string uriId = "SRCROOT";
            string relativePath = "path/to/file1";
            ArtifactChange artifactChange = new ArtifactChange
            {
                ArtifactLocation = new ArtifactLocation
                {
                    Uri = new Uri(relativePath, UriKind.Relative),
                    UriBaseId = uriId,
                },
                Replacements = new[]
                {
                    new Replacement { DeletedRegion = new Region { CharOffset = 0, CharLength = 10 }},
                }
            };

            var uriBaseIds = new Dictionary<string, ArtifactLocation>
            {
                { uriId, new ArtifactLocation { Uri = new Uri("file:///etc/", UriKind.Absolute) } }
            };

            var model = artifactChange.ToArtifactChangeModel(uriBaseIds, new FileRegionsCache());
            model.Should().NotBeNull();
            model.FilePath.Should().BeEquivalentTo(relativePath);
            model.FileName.Should().BeEquivalentTo("file1");
            model.Replacements.Should().NotBeEmpty();
            model.Replacements.Count.Should().Be(1);
        }
    }
}
