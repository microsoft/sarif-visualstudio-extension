// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using FluentAssertions;

using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio.Text.Adornments;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class SarifLocationErrorTagTests
    {
        private const string ResultMessageString = "This is result message.";

        public SarifLocationErrorTagTests()
        {
            TestUtilities.InitializeTestEnvironment();
        }

        // XamlReader.Load(...) has to be called from STA thread.
        // Use [StaFact] instead of [Fact] to run the test on STA thread.
        [StaFact]
        public void WithoutXamlString_Tooltip_ShouldBeMessageString()
        {
            var tag = new SarifLocationErrorTag(
                persistentSpan: null, // not touch span in the tests
                runIndex: 0,
                resultId: 0,
                errorType: PredefinedErrorTypeNames.OtherError,
                toolTipContent: ResultMessageString,
                toolTipXamlString: null,
                context: null);

            tag.Should().NotBeNull();
            tag.ToolTipContent.Should().Be(ResultMessageString);

            tag = new SarifLocationErrorTag(
                persistentSpan: null, // not touch span in the tests
                runIndex: 0,
                resultId: 0,
                errorType: PredefinedErrorTypeNames.OtherError,
                toolTipContent: ResultMessageString,
                toolTipXamlString: " ",
                context: null);

            tag.Should().NotBeNull();
            tag.ToolTipContent.Should().Be(ResultMessageString);
        }

        [StaFact]
        public void WithInvalidXamlString_Tooltip_ShouldBeMessageString()
        {
            var tag = new SarifLocationErrorTag(
                persistentSpan: null, // not touch span in the tests
                runIndex: 0,
                resultId: 0,
                errorType: PredefinedErrorTypeNames.OtherError,
                toolTipContent: ResultMessageString,
                toolTipXamlString: XamlUtilitiesTests.InvalidXaml,
                context: null);

            tag.Should().NotBeNull();
            tag.ToolTipContent.Should().Be(ResultMessageString);
        }

        [StaFact]
        public void WithValidXamlString_Tooltip_ShouldBeXamlElement()
        {
            var tag = new SarifLocationErrorTag(
                persistentSpan: null, // not touch span in the tests
                runIndex: 0,
                resultId: 0,
                errorType: PredefinedErrorTypeNames.OtherError,
                toolTipContent: ResultMessageString,
                toolTipXamlString: XamlUtilitiesTests.ValidXamlWithHyperlink,
                context: null);

            tag.Should().NotBeNull();
            tag.ToolTipContent.Should().BeOfType(typeof(ScrollViewer));
        }
    }
}
