// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class SarifTextViewCreationListenerTests
    {
        [Fact]
        public void SarifTextViewCreationListener_IsSarifLog()
        {
            var testcases = new[]
            {
                new { input = (string)null, expected = false },
                new { input = "", expected = false },
                new { input = "    ", expected = false },
                new { input = @"F:\users\david\repo\", expected = false },
                new { input = @"F:\users\david\repo\readme.txt", expected = false },
                new { input = @"D:\sources\repo\AssemblyInfo.cs", expected = false },
                new { input = @"D:\sources\repo\data\rules.json", expected = false },
                new { input = @"\\fileserver\shares\reports\scan.sarif", expected = true },
                new { input = @"C:\static analysis results\github.com\2021\04\16\nightly.sarif", expected = true },
            };

            var target = new SarifTextViewCreationListener();

            foreach (var testcase in testcases)
            {
                target.IsSarifLogFile(testcase.input).Should().Be(testcase.expected);
            }
        }

        [Fact]
        public void SarifTextViewCreationListener_IsSarifContentType()
        {
            var testcases = new[]
            {
                new { input = (string)null, expected = false },
                new { input = "", expected = false },
                new { input = "    ", expected = false },
                new { input = "JSON", expected = false },
                new { input = "XML", expected = false },
                new { input = "code", expected = false },
                new { input = "XSARIF", expected = false },
                new { input = "SARIFX", expected = false },
                new { input = "sarif", expected = true },
                new { input = "SARIF", expected = true },
            };

            var target = new SarifTextViewCreationListener();

            foreach (var testcase in testcases)
            {
                target.IsSarifLogFile(testcase.input).Should().Be(testcase.expected);
            }
        }
    }
}
