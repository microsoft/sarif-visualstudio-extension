// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Windows.Controls;

using FluentAssertions;

using Microsoft.Sarif.Viewer.Options;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

using Moq;

using Sarif.Viewer.VisualStudio.Core.Models;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.Models
{
    public class ScrollViewerWrapperTests
    {
        private readonly IErrorTag lowPriorityTag;
        private readonly IErrorTag highPriorityTag;

        public ScrollViewerWrapperTests()
        {
            Mock<IErrorTag> lowPriorityTagMock = new Mock<IErrorTag>();
            lowPriorityTagMock.Setup(x => x.ErrorType).Returns(PredefinedErrorTypeNames.HintedSuggestion);
            lowPriorityTag = lowPriorityTagMock.Object;

            Mock<IErrorTag> highPriorityTagMock = new Mock<IErrorTag>();
            highPriorityTagMock.Setup(x => x.ErrorType).Returns(PredefinedErrorTypeNames.OtherError);
            highPriorityTag = highPriorityTagMock.Object;
        }

        /// <summary>
        /// Tests to determine if the scroll viewer wrapper is properly taking the highest priority error type when given multiple possible.
        /// </summary>
        [Fact]
        public void ErrorTypeSortingTest()
        {
            Mock<ISarifViewerColorOptions> sarifViewerOptionsMock = new Mock<ISarifViewerColorOptions>();
            sarifViewerOptionsMock.Setup(x => x.ErrorUnderlineColor).Returns(PredefinedErrorTypeNames.OtherError);
            sarifViewerOptionsMock.Setup(x => x.WarningUnderlineColor).Returns(PredefinedErrorTypeNames.Warning);
            sarifViewerOptionsMock.Setup(x => x.NoteUnderlineColor).Returns(PredefinedErrorTypeNames.HintedSuggestion);

            ScrollViewerWrapper wrapper = new ScrollViewerWrapper(new List<IErrorTag>() { lowPriorityTag, highPriorityTag }, sarifViewerOptionsMock.Object);
            wrapper.ErrorType.Should().Be(PredefinedErrorTypeNames.OtherError);

            ScrollViewerWrapper wrapperOtherOrder = new ScrollViewerWrapper(new List<IErrorTag>() { lowPriorityTag, highPriorityTag }, sarifViewerOptionsMock.Object);
            wrapperOtherOrder.ErrorType.Should().Be(PredefinedErrorTypeNames.OtherError);
        }

        /// <summary>
        /// Tests to make sure we properly process when we pass an empty error tag in
        /// </summary>
        [Fact]
        public void ProperHandlingOfEmpty()
        {
            ScrollViewerWrapper wrapper = new ScrollViewerWrapper(new List<IErrorTag>(), null);
            wrapper.ErrorType.Should().Be(string.Empty);
        }

    }
}
