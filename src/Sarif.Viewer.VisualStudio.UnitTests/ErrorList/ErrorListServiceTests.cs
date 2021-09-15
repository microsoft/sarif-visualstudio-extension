// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using FluentAssertions;

using Microsoft.Sarif.Viewer.ErrorList;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class ErrorListServiceTests
    {
        private static readonly TestCase[] s_testCases = new TestCase[]
        {
            new TestCase
            {
                Title = "Simplest case",
                Input = @"""version"": ""2.1.0""",
                ExpectedMatchSuccess = true,
                ExpectedVersion = "2.1.0",
            },
            new TestCase
            {
                Title = "White space before colon",
                Input = @"""version"" : ""2.1.0""",
                ExpectedMatchSuccess = true,
                ExpectedVersion = "2.1.0",
            },
            new TestCase
            {
                Title = "Version near start of file",
                Input = @"01234567890123456789 ""version"" : ""2.1.0""",
                ExpectedMatchSuccess = true,
                ExpectedVersion = "2.1.0",
            },
            new TestCase
            {
                Title = "Version not near start of file",
                Input = @"01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789 ""version"" : ""2.1.0""",
                ExpectedMatchSuccess = false,
            },
            new TestCase
            {
                Title = "Invalid version",
                Input = @"""version"": ""a.b.c""",
                ExpectedMatchSuccess = false,
            },
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

        [Fact]
        public void ErrorListService_ProcessSarifLogAsync_InvalidJson()
        {
            // unhook original event handler and hook test event
            ErrorListService.LogProcessed -= ErrorListService.ErrorListService_LogProcessed;
            ErrorListService.LogProcessed += ErrorListServiceTest_LogProcessed;

            // invalid Json syntax, a separate comma in line 4
            // {"Invalid property identifier character: ,. Path '$schema', line 4, position 2."}
            string invalidJson =
@"
{
  ""$schema"": ""https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0-rtm.4.json"",
  ,
  ""version"": ""2.1.0"",
  ""runs"": [
    {
      ""tool"": {
        ""name"": ""CodeScanner""
      },
      ""results"": [
      ]
    }
  ]
}
";
            int numberOfException = numberOfExceptionLogged;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));
            _= ErrorListService.ProcessSarifLogAsync(stream, "logId", false, false).ConfigureAwait(false);
            // 1 exception logged
            numberOfExceptionLogged.Should().Be(numberOfException + 1);
        }

        [Fact]
        public void ErrorListService_ProcessSarifLogAsync_JsonNotCompatibleWithSarifShema()
        {
            // unhook original event handler and hook test event
            ErrorListService.LogProcessed -= ErrorListService.ErrorListService_LogProcessed;
            ErrorListService.LogProcessed += ErrorListServiceTest_LogProcessed;

            // valid Json syntax, but not satisfy schema, missing required property driver
            // {"Required property 'text' not found in JSON. Path 'runs[0].tool.driver.rules[0].fullDescription', line 14, position 36."}
            string jsonNotCompatible =
@"
{
  ""$schema"": ""https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0-rtm.4.json"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {
      ""tool"": {
        ""driver"": {
          ""name"": ""CodeScanner"",
          ""rules"": [
            {
              ""id"": ""Intrafile1001"",
              ""name"": ""IntrafileRule"",
              ""fullDescription"": { },
              ""helpUri"": ""https://github.com/microsoft/sarif-pattern-matcher""
            }
          ]
        },
      ""results"": [
      ]
    }
  ]
}
";
            int numberOfException = this.numberOfExceptionLogged;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonNotCompatible));
            _ = ErrorListService.ProcessSarifLogAsync(stream, "logId", false, false).ConfigureAwait(false);
            // 1 exception logged
            this.numberOfExceptionLogged.Should().Be(numberOfException + 1);
        }

        private struct TestCase
        {
            public string Title { get; set; }

            public string Input { get; set; }

            public bool ExpectedMatchSuccess { get; set; }

            public string ExpectedVersion { get; set; }
        }

        private int numberOfExceptionLogged = 0;
        private void ErrorListServiceTest_LogProcessed(object sender, LogProcessedEventArgs e)
        {
            this.numberOfExceptionLogged++;
        }
    }
}
