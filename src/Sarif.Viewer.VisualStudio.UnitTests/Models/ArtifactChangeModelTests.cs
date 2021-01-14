// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;

using Xunit;

namespace Microsoft.Sarif.Viewer.Models
{
    public class ArtifactChangeModelTests
    {
        private static readonly IDictionary<string, ArtifactLocation> s_emptyOriginalUriBaseIds = new Dictionary<string, ArtifactLocation>();
        private static readonly FileRegionsCache s_emptyFileRegionsCache = new FileRegionsCache();

        [Fact]
        public void ArtifactChangeModel_FromArtifactChange_LocalPath()
        {
            var change = new ArtifactChange
            {
                ArtifactLocation = new ArtifactLocation
                {
                    Uri = new Uri("file://C:/src/tools/util.cs", UriKind.RelativeOrAbsolute),
                },
                Replacements = new List<Replacement>(),
            };

            var model = change.ToArtifactChangeModel(s_emptyOriginalUriBaseIds, s_emptyFileRegionsCache);
            model.FilePath.Should().Be(@"C:\src\tools\util.cs");
        }

        [Fact]
        public void ArtifactChangeModel_FromArtifactChange_RelativePath()
        {
            var change = new ArtifactChange
            {
                ArtifactLocation = new ArtifactLocation
                {
                    Uri = new Uri(@"\src\tools\util.cs", UriKind.RelativeOrAbsolute),
                },
                Replacements = new List<Replacement>(),
            };

            var model = change.ToArtifactChangeModel(s_emptyOriginalUriBaseIds, s_emptyFileRegionsCache);
            model.FilePath.Should().Be(@"\src\tools\util.cs");
        }
    }
}
