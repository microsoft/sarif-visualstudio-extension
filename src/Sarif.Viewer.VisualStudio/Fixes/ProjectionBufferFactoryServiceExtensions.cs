// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.Sarif.Viewer.Fixes
{
    internal static class ProjectionBufferFactoryServiceExtensions
    {
        // Code taken from https://github.com/dotnet/roslyn/blob/master/src/EditorFeatures/Core/Shared/Extensions/IProjectionBufferFactoryServiceExtensions.cs
        public static IProjectionBuffer CreateProjectionBufferWithoutIndentation(
            this IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptions editorOptions,
            ITextSnapshot textSnapshot,
            string separator,
            params LineSpan[] exposedLineSpans)
        {
            var spans = new List<object>();
            if (exposedLineSpans.Length > 0)
            {
                if (exposedLineSpans[0].Start > 0 && !string.IsNullOrEmpty(separator))
                {
                    spans.Add(separator);
                    spans.Add(editorOptions.GetNewLineCharacter());
                }

                var snapshotSpanRanges = CreateSnapshotSpanRanges(textSnapshot, exposedLineSpans);
                var indentColumn = DetermineIndentationColumn(editorOptions, snapshotSpanRanges.SelectMany(s => s));

                foreach (var snapshotSpanRange in snapshotSpanRanges)
                {
                    foreach (var snapshotSpan in snapshotSpanRange)
                    {
                        var line = snapshotSpan.Snapshot.GetLineFromPosition(snapshotSpan.Start);
                        var indentPosition = line.GetText().GetLineOffsetFromColumn(indentColumn, editorOptions.GetTabSize()) + line.Start;
                        var mappedSpan = new SnapshotSpan(snapshotSpan.Snapshot, Span.FromBounds(indentPosition, snapshotSpan.End));

                        var trackingSpan = mappedSpan.Snapshot.CreateTrackingSpan(mappedSpan, SpanTrackingMode.EdgeExclusive);

                        spans.Add(trackingSpan);

                        // Add a newline between every line.
                        if (snapshotSpan != snapshotSpanRange.Last())
                        {
                            spans.Add(editorOptions.GetNewLineCharacter());
                        }
                    }

                    // Add a separator between every set of lines.
                    if (snapshotSpanRange != snapshotSpanRanges.Last())
                    {
                        spans.Add(editorOptions.GetNewLineCharacter());
                        spans.Add(separator);
                        spans.Add(editorOptions.GetNewLineCharacter());
                    }
                }

                if (textSnapshot.GetLineNumberFromPosition(snapshotSpanRanges.Last().Last().End) < textSnapshot.LineCount - 1)
                {
                    spans.Add(editorOptions.GetNewLineCharacter());
                    spans.Add(separator);
                }
            }

            return projectionBufferFactoryService.CreateProjectionBuffer(
                projectionEditResolver: null,
                sourceSpans: spans,
                options: ProjectionBufferOptions.None,
                contentType: textSnapshot.ContentType);
        }

        public static int GetLineOffsetFromColumn(this string line, int column, int tabSize)
        {
            var currentColumn = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (currentColumn >= column)
                {
                    return i;
                }

                if (line[i] == '\t')
                {
                    currentColumn += tabSize - (currentColumn % tabSize);
                }
                else
                {
                    currentColumn++;
                }
            }

            // We're asking for a column past the end of the line, so just go to the end.
            return line.Length;
        }

        public static int? GetFirstNonWhitespacePosition(this ITextSnapshotLine line)
        {
            var text = line.GetText();

            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return line.Start + i;
                }
            }

            return null;
        }

        public static int GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(this string line, int tabSize)
        {
            var firstNonWhitespaceChar = line.GetFirstNonWhitespaceOffset();

            if (firstNonWhitespaceChar.HasValue)
            {
                return line.GetColumnFromLineOffset(firstNonWhitespaceChar.Value, tabSize);
            }
            else
            {
                // It's all whitespace, so go to the end
                return line.GetColumnFromLineOffset(line.Length, tabSize);
            }
        }

        public static int? GetFirstNonWhitespaceOffset(this string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (!char.IsWhiteSpace(line[i]))
                {
                    return i;
                }
            }

            return null;
        }

        public static int GetColumnFromLineOffset(this string line, int endPosition, int tabSize)
        {
            return ConvertTabToSpace(line, tabSize, 0, endPosition);
        }

        public static int ConvertTabToSpace(this string textSnippet, int tabSize, int initialColumn, int endPosition)
        {
            int column = initialColumn;

            // now this will calculate indentation regardless of actual content on the buffer except TAB
            for (int i = 0; i < endPosition; i++)
            {
                if (textSnippet[i] == '\t')
                {
                    column += tabSize - (column % tabSize);
                }
                else
                {
                    column++;
                }
            }

            return column - initialColumn;
        }

        private static IList<IList<SnapshotSpan>> CreateSnapshotSpanRanges(ITextSnapshot snapshot, LineSpan[] exposedLineSpans)
        {
            var result = new List<IList<SnapshotSpan>>();
            foreach (var lineSpan in exposedLineSpans)
            {
                var snapshotSpans = CreateSnapshotSpans(snapshot, lineSpan);
                if (snapshotSpans.Count > 0)
                {
                    result.Add(snapshotSpans);
                }
            }

            return result;
        }

        private static IList<SnapshotSpan> CreateSnapshotSpans(ITextSnapshot snapshot, LineSpan lineSpan)
        {
            var result = new List<SnapshotSpan>();
            for (int i = lineSpan.Start; i < lineSpan.End; i++)
            {
                var line = snapshot.GetLineFromLineNumber(i);
                result.Add(line.Extent);
            }

            return result;
        }

        private static int DetermineIndentationColumn(
            IEditorOptions editorOptions,
            IEnumerable<SnapshotSpan> spans)
        {
            int? indentationColumn = null;
            foreach (var span in spans)
            {
                var snapshot = span.Snapshot;
                var startLineNumber = snapshot.GetLineNumberFromPosition(span.Start);
                var endLineNumber = snapshot.GetLineNumberFromPosition(span.End);

                // If the span starts after the first non-whitespace of the first line, we'll
                // exclude that line to avoid throwing off the calculation. Otherwise, the
                // incorrect indentation will be returned for lambda cases like so:
                //
                // void M()
                // {
                //     Func<int> f = () =>
                //         {
                //             return 1;
                //         };
                // }
                //
                // Without throwing out the first line in the example above, the indentation column
                // used will be 4, rather than 8.
                var startLineFirstNonWhitespace = snapshot.GetLineFromLineNumber(startLineNumber).GetFirstNonWhitespacePosition();
                if (startLineFirstNonWhitespace.HasValue && startLineFirstNonWhitespace.Value < span.Start)
                {
                    startLineNumber++;
                }

                for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
                {
                    var line = snapshot.GetLineFromLineNumber(lineNumber);
                    if (string.IsNullOrWhiteSpace(line.GetText()))
                    {
                        continue;
                    }

                    indentationColumn = indentationColumn.HasValue
                        ? Math.Min(indentationColumn.Value, line.GetText().GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(editorOptions.GetTabSize()))
                        : line.GetText().GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(editorOptions.GetTabSize());
                }
            }

            return indentationColumn ?? 0;
        }
    }
}
