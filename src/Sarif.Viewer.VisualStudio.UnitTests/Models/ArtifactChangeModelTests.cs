// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;
using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.Models
{
    public class ArtifactChangeModelTests
    {
        [Fact]
        public void ArtifactChangeModel_FromArtifactChange_LocalPath()
        {
            ArtifactChange change = new ArtifactChange
            {
                ArtifactLocation = new ArtifactLocation
                {
                    Uri = new Uri("file://C:/src/tools/util.cs", UriKind.RelativeOrAbsolute)
                },
                Replacements = new List<Replacement>()
            };

            ArtifactChangeModel model = change.ToArtifactChangeModel();
            model.FilePath.Should().Be(@"C:\src\tools\util.cs");
        }

        [Fact]
        public void ArtifactChangeModel_FromArtifactChange_RelativePath()
        {
            ArtifactChange change = new ArtifactChange
            {
                ArtifactLocation = new ArtifactLocation
                {
                    Uri = new Uri(@"\src\tools\util.cs", UriKind.RelativeOrAbsolute)
                },
                Replacements = new List<Replacement>()
            };

            ArtifactChangeModel model = change.ToArtifactChangeModel();
            model.FilePath.Should().Be(@"\src\tools\util.cs");
        }
    }
}
