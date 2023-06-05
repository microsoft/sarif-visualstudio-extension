// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// Helper class that builds spans to be used in code highlighting.
    /// </summary>
    internal static class SpanHelper
    {
        /// <summary>
        /// Tries to create a persistent span for highlighting matching the provided <see cref="Region"/>.
        /// </summary>
        /// <param name="fullyPopulatedRegion">The region to make a span for.</param>
        /// <param name="textBuffer">The textbuffer to use to make the span.</param>
        /// <param name="persistentSpanFactory">The span factory to use.</param>
        /// <param name="persistentSpan">The persistent span to output if available.</param>
        /// <returns>True if it successfully created the <paramref name="persistentSpan"/> object.</returns>
        internal static bool TryCreatePersistentSpan(Region fullyPopulatedRegion, ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory, out IPersistentSpan persistentSpan)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            persistentSpan = null;

            if (!TryCreateTextSpanWithinDocumentFromSourceRegion(fullyPopulatedRegion, textBuffer, out TextSpan documentSpan))
            {
                return false;
            }

            if (!persistentSpanFactory.CanCreate(textBuffer))
            {
                return false;
            }

            // Creates an IPersistentSpan for a snapshot span on a document that is currently open.
            persistentSpan = persistentSpanFactory.Create(
                        textBuffer.CurrentSnapshot,
                        startLine: documentSpan.iStartLine,
                        startIndex: documentSpan.iStartIndex,
                        endLine: documentSpan.iEndLine,
                        endIndex: documentSpan.iEndIndex,
                        trackingMode: SpanTrackingMode.EdgeInclusive);

            return true;
        }

        /// <summary>
        /// Produces a <paramref name="textSpan"/> representing the <paramref name="region"/> and <paramref name="textBuffer"/> if available.
        /// </summary>
        /// <param name="region">The region to make a textspan from.</param>
        /// <param name="textBuffer">The textbuffer to make the textspan from.</param>
        /// <param name="textSpan">The textspan that is output.</param>
        /// <returns>True if it succesfully found a valid textspan.</returns>
        internal static bool TryCreateTextSpanWithinDocumentFromSourceRegion(Region region, ITextBuffer textBuffer, out TextSpan textSpan)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            // SARIF regions are 1 based, VS is zero based.
            textSpan.iStartLine = Math.Max(region.StartLine - 1, 0);
            textSpan.iEndLine = Math.Max(region.EndLine - 1, 0);
            textSpan.iStartIndex = Math.Max(region.StartColumn - 1, 0);
            textSpan.iEndIndex = Math.Max(region.EndColumn - 1, 0);

            ITextSnapshot currentSnapshot = textBuffer.CurrentSnapshot;
            int lastLine = currentSnapshot.LineCount;

            // If the start and end indexes are outside the scope of the text, skip tagging.
            if (textSpan.iStartLine > lastLine ||
                textSpan.iEndLine > lastLine ||
                textSpan.iEndLine < textSpan.iStartLine)
            {
                return false;
            }

            try
            {
                ITextSnapshotLine startTextLine = currentSnapshot.GetLineFromLineNumber(textSpan.iStartLine);
                ITextSnapshotLine endTextLine = currentSnapshot.GetLineFromLineNumber(textSpan.iEndLine);

                if (textSpan.iStartIndex > startTextLine.Length)
                {
                    return false;
                }

                // If we are highlighting just one line and the end column of the end line is out of scope
                // or we are highlighting just one line and we reset the start column above, then highlight the entire line.
                if (textSpan.iEndLine == textSpan.iStartLine && textSpan.iStartIndex > textSpan.iEndIndex)
                {
                    textSpan.iStartIndex = 0;
                    textSpan.iEndIndex = endTextLine.Length - 1;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // This exception can be thrown even though the range check above has passed.
                // Unknown cause. Net effect is no text highlighting.
                return false;
            }

            return true;
        }
    }
}
