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
    public class FileChangeModelTests
    {
        [Fact]
        public void FileChangeModel_FromFileChange_LocalPath()
        {
            FileChange fileChange = new FileChange
            {
                FileLocation = new FileLocation
                {
                    Uri = new Uri("file://C:/src/tools/util.cs", UriKind.RelativeOrAbsolute)
                },
                Replacements = new List<Replacement>()
            };

            FileChangeModel model = fileChange.ToFileChangeModel();
            model.FilePath.Should().Be(@"C:\src\tools\util.cs");
        }

        [Fact]
        public void FileChangeModel_FromFileChange_RelativePath()
        {
            FileChange fileChange = new FileChange
            {
                FileLocation = new FileLocation
                {
                    Uri = new Uri(@"\src\tools\util.cs", UriKind.RelativeOrAbsolute)
                },
                Replacements = new List<Replacement>()
            };

            FileChangeModel model = fileChange.ToFileChangeModel();
            model.FilePath.Should().Be(@"\src\tools\util.cs");
        }
    }
}
