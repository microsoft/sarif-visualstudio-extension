// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core;
using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Services;

using Xunit;

namespace Microsoft.Sarif.Viewer.ResultSources.DeveloperCanvas.UnitTests
{
    /// <summary>
    /// Set of tests for the <see cref="DevCanvasResultSourceService"/>
    /// </summary>
    public class DevCanvasResultServiceTest
    {
        /// <summary>
        /// Tests that we can properly parse git urls into the server-project-repo format we use.
        /// </summary>
        /// <param name="repoUrl">The url of the repo we need to parse.</param>
        /// <param name="expectedServer">The server we expect to get out.</param>
        /// <param name="expectedProject">The project we expect to get out.</param>
        /// <param name="expectedRepo">The repo we expect to get out.</param>
        [Theory]
        [InlineData("https://dev.azure.com/serverName/projectName/_git/repoName", "dev.azure.com/serverName", "projectName", "repoName")]
        [InlineData("https://serverName.visualstudio.com/projectName/_git/repoName", "serverName.visualstudio.com", "projectName", "repoName")]
        public void ParseGitUrl(string repoUrl, string expectedServer, string expectedProject, string expectedRepo)
        {
            DevCanvasResultSourceService.ParseGitUrl(repoUrl, out string serverName, out string projectName, out string repoName);
            serverName.Should().Be(expectedServer);
            projectName.Should().Be(expectedProject);
            repoName.Should().Be(expectedRepo);
        }
    }
}
