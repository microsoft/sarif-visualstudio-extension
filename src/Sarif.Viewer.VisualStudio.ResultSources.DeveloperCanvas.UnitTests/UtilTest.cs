// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.UnitTests
{
    [TestClass]
    public class UtilTest
    {
        /// <summary>
        /// Validates that we properly get the right form of a string for a particular number of that string ocurring.
        /// </summary>
        /// <param name="text">Original string to pluralize</param>
        /// <param name="count">Number of instances there are of <paramref name="text"/></param>
        /// <param name="expected">Expected output.</param>
        [TestMethod]
        [DataRow("Test", 1, "1 Test")]
        [DataRow("Test", 2, "2 Tests")]
        [DataRow("Test", 100, "100 Tests")]
        [DataRow("branch", 1, "1 branch")]
        [DataRow("branch", 2, "2 branches")]
        [DataRow("branch", 0, "0 branches")]
        public void STest(string text, int count, string expected)
        {
            string output = Util.S(text, count);
            output.Should().Be(expected);
        }

        /// <summary>
        /// Tests that we can properly parse git urls into the server-project-repo format we use.
        /// </summary>
        /// <param name="repoUrl">The url of the repo we need to parse.</param>
        /// <param name="expectedServer">The server we expect to get out.</param>
        /// <param name="expectedProject">The project we expect to get out.</param>
        /// <param name="expectedRepo">The repo we expect to get out.</param>
        [TestMethod]
        [DataRow("https://dev.azure.com/serverName/projectName/_git/repoName", "dev.azure.com/serverName", "projectName", "repoName")]
        [DataRow("https://serverName.visualstudio.com/projectName/_git/repoName", "dev.azure.com/serverName", "projectName", "repoName")]
        public void ParseGitUrl(string repoUrl, string expectedServer, string expectedProject, string expectedRepo)
        {
            Util.ParseGitUrl(repoUrl, out string serverName, out string projectName, out string repoName);
            serverName.Should().Be(expectedServer);
            projectName.Should().Be(expectedProject);
            repoName.Should().Be(expectedRepo);
        }
    }
}
