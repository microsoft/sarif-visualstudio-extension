// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Sarif.Viewer.ErrorList;
using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class ErrorListServiceTests
    {
        private struct TestCase
        {
            public string Title { get; set; }
            public string Input { get; set; }
            public bool ExpectedMatchSuccess { get; set; }
            public string ExpectedVersion { get; set; }
        }

        private static readonly TestCase[] s_testCases = new TestCase[]
        {
            new TestCase
            {
                Title = "Simplest case",
                Input = @"""version"": ""2.1.0""",
                ExpectedMatchSuccess = true,
                ExpectedVersion = "2.1.0"
            },
            new TestCase
            {
                Title = "White space before colon",
                Input = @"""version"" : ""2.1.0""",
                ExpectedMatchSuccess = true,
                ExpectedVersion = "2.1.0"
            },
            new TestCase
            {
                Title = "Version near start of file",
                Input = @"01234567890123456789 ""version"" : ""2.1.0""",
                ExpectedMatchSuccess = true,
                ExpectedVersion = "2.1.0"
            },
            new TestCase
            {
                Title = "Version not near start of file",
                Input = @"01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789 ""version"" : ""2.1.0""",
                ExpectedMatchSuccess = false
            },
            new TestCase
            {
                Title = "Invalid version",
                Input = @"""version"": ""a.b.c""",
                ExpectedMatchSuccess = false
            }
        };

        [Fact]
        [Trait(TestTraits.Bug, "98")]
        public void ErrorListService_MatchesVersionProperty()
        {
            var failedTestCases = new List<TestCase>();

            foreach (TestCase testCase in s_testCases)
            {
                Match match = ErrorListService.MatchVersionProperty(testCase.Input);

                bool failed = false;
                if (match.Success != testCase.ExpectedMatchSuccess) { failed = true; }

                if (!failed && testCase.ExpectedMatchSuccess)
                {
                    if (match.Groups["version"].Value != testCase.ExpectedVersion) { failed = true; }
                }

                if (failed) { failedTestCases.Add(testCase); }
            }

            failedTestCases.Should().BeEmpty();
        }
    }
}
