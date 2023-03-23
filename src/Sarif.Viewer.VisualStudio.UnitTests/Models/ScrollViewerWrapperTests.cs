// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FluentAssertions;

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
            highPriorityTagMock.Setup(x => x.ErrorType).Returns(PredefinedErrorTypeNames.SyntaxError);
            highPriorityTag = highPriorityTagMock.Object;
        }

        /// <summary>
        /// Tests to determine if the scroll viewer wrapper is properly taking the highest priority error type when given multiple possible.
        /// </summary>
        [Fact]
        public void ErrorTypeSortingTest()
        {
            ScrollViewerWrapper wrapper = new ScrollViewerWrapper(new List<IErrorTag>() { lowPriorityTag, highPriorityTag });
            wrapper.ErrorType.Should().Be(PredefinedErrorTypeNames.SyntaxError);

            ScrollViewerWrapper wrapperOtherOrder = new ScrollViewerWrapper(new List<IErrorTag>() { lowPriorityTag, highPriorityTag });
            wrapperOtherOrder.ErrorType.Should().Be(PredefinedErrorTypeNames.SyntaxError);
        }

        /// <summary>
        /// Tests to make sure we can properly handle and sort unknown type strings
        /// </summary>
        [Fact]
        public void ErrorTypeSortingWhenUnexpectedType()
        {
            Mock<IErrorTag> unknownPriorityTag = new Mock<IErrorTag>();
            unknownPriorityTag.Setup(x => x.ErrorType).Returns("Random error type string");

            ScrollViewerWrapper wrapper = new ScrollViewerWrapper(new List<IErrorTag>() { lowPriorityTag, unknownPriorityTag.Object });

            wrapper.ErrorType.Should().Be("Random error type string");

            wrapper = new ScrollViewerWrapper(new List<IErrorTag>() { highPriorityTag, unknownPriorityTag.Object });
            wrapper.ErrorType.Should().Be(PredefinedErrorTypeNames.SyntaxError);
        }

    }
}
