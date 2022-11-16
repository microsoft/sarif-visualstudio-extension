// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

using Moq;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class SpanHelperTests : SarifViewerPackageUnitTests
    {
        [Fact]
        public void TryCreateTextSpanWithinDocumentFromSourceRegion_ValidRegion()
        {
            /*
            line 1: ...
            ...
            line 5: -----
            line 6: ---------
            line 7: ---------------
            ...
            line 100
             */
            int totalLines = 100;
            int startLine = 5, endLine = 7, startColumn = 1, endColumn = 15;
            var region = new Region { StartLine = startLine, EndLine = endLine, StartColumn = startColumn, EndColumn = endColumn };

            var lineLengthMap = new Dictionary<int, int>
            {
                { startLine - 1, 20 },
                { endLine - 1, 15 }
            };
            ITextBuffer textBuffer = this.SetupTextBuffer(totalLines, lineLengthMap);

            bool result = SpanHelper.TryCreateTextSpanWithinDocumentFromSourceRegion(region, textBuffer, out TextSpan textSpan);

            result.Should().Be(true);
            textSpan.iStartLine.Should().Be(startLine - 1);
            textSpan.iEndLine.Should().Be(endLine - 1);
            textSpan.iStartIndex.Should().Be(startColumn - 1);
            textSpan.iEndIndex.Should().Be(endColumn - 1);
        }

        [Fact]
        public void TryCreateTextSpanWithinDocumentFromSourceRegion_ZeroLength()
        {
            /*
            line 1: ...
            ...
            line 4: (empty line)
            ...
            line 21
             */
            int totalLines = 21;
            int startLine = 4, endLine = 4, startColumn = 1, endColumn = 1;
            var region = new Region { StartLine = startLine, EndLine = endLine, StartColumn = startColumn, EndColumn = endColumn };

            var lineLengthMap = new Dictionary<int, int>
            {
                { startLine - 1, 0 }
            };
            ITextBuffer textBuffer = this.SetupTextBuffer(totalLines, lineLengthMap);

            bool result = SpanHelper.TryCreateTextSpanWithinDocumentFromSourceRegion(region, textBuffer, out TextSpan textSpan);

            result.Should().Be(true);
            textSpan.iStartLine.Should().Be(startLine - 1);
            textSpan.iEndLine.Should().Be(endLine - 1);
            textSpan.iStartIndex.Should().Be(startColumn - 1);
            textSpan.iEndIndex.Should().Be(endColumn - 1);
        }

        [Fact]
        public void TryCreateTextSpanWithinDocumentFromSourceRegion_ContainsEmptyLines()
        {
            /*
            line 1: ...
            ...
            line 9: (empty line)
            line 10: ----------
            line 11: (empty line)
            ...
            line 42
             */
            int totalLines = 42;
            int startLine = 9, endLine = 11, startColumn = 1, endColumn = 1;
            var region = new Region { StartLine = startLine, EndLine = endLine, StartColumn = startColumn, EndColumn = endColumn };

            var lineLengthMap = new Dictionary<int, int>
            {
                { startLine - 1, 0 },
                { endLine - 1, 0 },
            };
            ITextBuffer textBuffer = this.SetupTextBuffer(totalLines, lineLengthMap);

            bool result = SpanHelper.TryCreateTextSpanWithinDocumentFromSourceRegion(region, textBuffer, out TextSpan textSpan);

            result.Should().Be(true);
            textSpan.iStartLine.Should().Be(startLine - 1);
            textSpan.iEndLine.Should().Be(endLine - 1);
            textSpan.iStartIndex.Should().Be(startColumn - 1);
            textSpan.iEndIndex.Should().Be(endColumn - 1);
        }

        [Fact]
        public void TryCreateTextSpanWithinDocumentFromSourceRegion_StartLineLessThanEndLine()
        {
            int totalLines = 34;
            int startLine = 9, endLine = 8, startColumn = 1, endColumn = 10;
            var region = new Region { StartLine = startLine, EndLine = endLine, StartColumn = startColumn, EndColumn = endColumn };

            var lineLengthMap = new Dictionary<int, int>
            {
                { startLine - 1, 0 },
                { endLine - 1, 10 },
            };
            ITextBuffer textBuffer = this.SetupTextBuffer(totalLines, lineLengthMap);

            bool result = SpanHelper.TryCreateTextSpanWithinDocumentFromSourceRegion(region, textBuffer, out TextSpan textSpan);

            result.Should().Be(false);
        }

        [Fact]
        public void TryCreateTextSpanWithinDocumentFromSourceRegion_EndLineLessThanTotalLines()
        {
            int totalLines = 34;
            int startLine = 29, endLine = 36, startColumn = 1, endColumn = 53;
            var region = new Region { StartLine = startLine, EndLine = endLine, StartColumn = startColumn, EndColumn = endColumn };

            var lineLengthMap = new Dictionary<int, int>
            {
                { startLine - 1, 10 },
                { endLine - 1, 53 },
            };
            ITextBuffer textBuffer = this.SetupTextBuffer(totalLines, lineLengthMap);

            bool result = SpanHelper.TryCreateTextSpanWithinDocumentFromSourceRegion(region, textBuffer, out TextSpan textSpan);

            result.Should().Be(false);
        }

        [Fact]
        public void TryCreateTextSpanWithinDocumentFromSourceRegion_StartColumnGreaterThanLineLength()
        {
            int totalLines = 76;
            int startLine = 50, endLine = 50, startColumn = 111, endColumn = 160;
            var region = new Region { StartLine = startLine, EndLine = endLine, StartColumn = startColumn, EndColumn = endColumn };

            var lineLengthMap = new Dictionary<int, int>
            {
                { startLine - 1, 100 },
            };
            ITextBuffer textBuffer = this.SetupTextBuffer(totalLines, lineLengthMap);

            bool result = SpanHelper.TryCreateTextSpanWithinDocumentFromSourceRegion(region, textBuffer, out TextSpan textSpan);

            result.Should().Be(false);
        }

        [Fact]
        public void TryCreateTextSpanWithinDocumentFromSourceRegion_StartColumnGreaterThanEndColumn()
        {
            int totalLines = 81;
            int startLine = 81, endLine = 81, startColumn = 66, endColumn = 38;
            var region = new Region { StartLine = startLine, EndLine = endLine, StartColumn = startColumn, EndColumn = endColumn };

            var lineLengthMap = new Dictionary<int, int>
            {
                { startLine - 1, 100 },
            };
            ITextBuffer textBuffer = this.SetupTextBuffer(totalLines, lineLengthMap);

            bool result = SpanHelper.TryCreateTextSpanWithinDocumentFromSourceRegion(region, textBuffer, out TextSpan textSpan);

            result.Should().Be(true);
            textSpan.iStartLine.Should().Be(startLine - 1);
            textSpan.iEndLine.Should().Be(endLine - 1);
            textSpan.iStartIndex.Should().Be(0);
            textSpan.iEndIndex.Should().Be(100 - 1);
        }

        private ITextBuffer SetupTextBuffer(int totalLines, IDictionary<int, int> linesMap)
        {
            Mock<ITextBuffer> mockTextBuffer = new Mock<ITextBuffer>();

            mockTextBuffer.Setup(b => b.CurrentSnapshot).Returns(SetupTextSnapshot(totalLines, linesMap));

            return mockTextBuffer.Object;
        }

        private ITextSnapshot SetupTextSnapshot(int totalLines, IDictionary<int, int> linesMap)
        {
            Mock<ITextSnapshot> mockTextSnapshot = new Mock<ITextSnapshot>();

            mockTextSnapshot.Setup(s => s.LineCount).Returns(totalLines);

            mockTextSnapshot
                .Setup(s => s.GetLineFromLineNumber(It.IsAny<int>()))
                .Returns((int lineNumber) => SetupTextSnapshotLine(linesMap[lineNumber]));

            return mockTextSnapshot.Object;
        }

        private ITextSnapshotLine SetupTextSnapshotLine(int lineLength)
        {
            Mock<ITextSnapshotLine> mockTextSnapshotLine = new Mock<ITextSnapshotLine>();

            mockTextSnapshotLine.Setup(l => l.Length).Returns(lineLength);

            return mockTextSnapshotLine.Object;
        }
    }
}
