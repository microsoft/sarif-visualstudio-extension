// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;
using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class ResultExtensionsTests
    {
        [Fact]
        public void ResultExtensions_GetPrimaryTargetFile_HasOriginalUriBasePath()
        {
            // Arrange.
            Uri originalUriBasePath = new Uri(@"C:\Code\sarif-sdk\");
            Uri fileUriInLogFile = new Uri(@"src\Sarif\Notes.cs", UriKind.Relative);
            const string UriBaseId = "%SDXROOT%";
            const string ResolvedFilePath = @"C:\Code\sarif-sdk\src\Sarif\Notes.cs";			
			const string RunId = "faf71237-d00d-456c-855e-f179759f5f21";

            Result result = new Result
            {
                Locations = new List<Location>
				{
					new Location
					{
                        PhysicalLocation = new PhysicalLocation
                        {
							FileLocation = new FileLocation
                            {
                                UriBaseId = UriBaseId,
								Uri = fileUriInLogFile
                            }
                        }
                    }
				}
            };

            CodeAnalysisResultManager.Instance.CurrentRunId = RunId;
            CodeAnalysisResultManager.Instance.RunDataCaches.Add(RunId, new RunDataCache());
            CodeAnalysisResultManager.Instance.CurrentRunDataCache.OriginalUriBasePaths.Add(UriBaseId, originalUriBasePath);

            // Act.
            string actualResolvedFilePath = result.GetPrimaryTargetFile();

            // Assert.
            actualResolvedFilePath.Should().Be(ResolvedFilePath);
        }
    }
}
