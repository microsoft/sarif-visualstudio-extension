// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core;

using Xunit;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.UnitTests
{
    /// <summary>
    /// Tests the <see cref="Util"/> class.
    /// </summary>
    public class UtilTest
    {
        /// <summary>
        /// Validates that we properly get the right form of a string for a particular number of that string ocurring.
        /// </summary>
        /// <param name="text">Original string to pluralize</param>
        /// <param name="count">Number of instances there are of <paramref name="text"/></param>
        /// <param name="expected">Expected output.</param>
        [Theory]
        [InlineData("Test", 1, "1 Test")]
        [InlineData("Test", 2, "2 Tests")]
        [InlineData("Test", 100, "100 Tests")]
        [InlineData("branch", 1, "1 branch")]
        [InlineData("branch", 2, "2 branches")]
        [InlineData("branch", 0, "0 branches")]
        public void STest(string text, int count, string expected)
        {
            string output = Util.S(text, count);
            output.Should().Be(expected);
        }
    }
}
