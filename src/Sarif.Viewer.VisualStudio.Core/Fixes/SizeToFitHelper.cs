// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.Sarif.Viewer.Fixes
{
    internal class SizeToFitHelper
    {
        private readonly IWpfDifferenceViewer diffViewer;
        private readonly TaskCompletionSource<object> taskCompletion;
        private readonly double minWidth;

        private int calculationStarted;
        private double width;
        private double height;

        public SizeToFitHelper(IWpfDifferenceViewer diffViewer, double minWidth)
        {
            this.calculationStarted = 0;
            this.diffViewer = diffViewer;
            this.minWidth = minWidth;
            this.taskCompletion = new TaskCompletionSource<object>();
        }

        public async Task SizeToFitAsync()
        {
            // We won't know how many lines there will be in the inline diff or how
            // wide the widest line in the inline diff will be until the inline diff
            // snapshot has been computed. We register an event handler here that will
            // allow us to calculate the required width and height once the inline diff
            // snapshot has been computed.
            this.diffViewer.DifferenceBuffer.SnapshotDifferenceChanged += this.SnapshotDifferenceChanged;

            // The inline diff snapshot may already have been computed before we registered the
            // above event handler. In this case, we can go ahead and calculate the required width
            // and height.
            this.CalculateSize();

            // IDifferenceBuffer calculates the inline diff snapshot on the UI thread (on idle).
            // Since we are already on the UI thread, we need to yield control so that the
            // inline diff snapshot computation (and the event handler we registered above to
            // calculate required width and height) get a chance to run and we need to wait until
            // this computation is complete.
            await this.taskCompletion.Task;

            // We have the height and width required to display the inline diff snapshot now.
            // Set the height and width of the IWpfDifferenceViewer accordingly.
            this.diffViewer.VisualElement.Width = this.width;
            this.diffViewer.VisualElement.Height = this.height;
        }

        private void SnapshotDifferenceChanged(object sender, SnapshotDifferenceChangeEventArgs args)
        {
            this.CalculateSize();
        }

        private void CalculateSize()
        {
            if ((this.diffViewer.DifferenceBuffer.CurrentInlineBufferSnapshot == null) ||
                (Interlocked.CompareExchange(ref this.calculationStarted, 1, 0) == 1))
            {
                // Return if inline diff snapshot is not yet ready or
                // if the size calculation is already in progress.
                return;
            }

            // Unregister the event handler - we don't need it anymore since the inline diff
            // snapshot is available at this point.
            this.diffViewer.DifferenceBuffer.SnapshotDifferenceChanged -= this.SnapshotDifferenceChanged;

            IWpfTextView textView;
            ITextSnapshot snapshot;
            if (this.diffViewer.ViewMode == DifferenceViewMode.RightViewOnly)
            {
                textView = this.diffViewer.RightView;
                snapshot = this.diffViewer.DifferenceBuffer.RightBuffer.CurrentSnapshot;
            }
            else if (this.diffViewer.ViewMode == DifferenceViewMode.LeftViewOnly)
            {
                textView = this.diffViewer.LeftView;
                snapshot = this.diffViewer.DifferenceBuffer.LeftBuffer.CurrentSnapshot;
            }
            else
            {
                textView = this.diffViewer.InlineView;
                snapshot = this.diffViewer.DifferenceBuffer.CurrentInlineBufferSnapshot;
            }

            // Perform a layout without actually rendering the content on the screen so that
            // we can calculate the exact height and width required to render the content on
            // the screen before actually rendering it. This helps us avoiding the flickering
            // effect that would be caused otherwise when the UI is rendered twice with
            // different sizes.
            textView.DisplayTextLineContainingBufferPosition(
                new SnapshotPoint(snapshot, 0),
                0.0,
                ViewRelativePosition.Top,
                double.MaxValue,
                double.MaxValue);

            this.width = Math.Max(textView.MaxTextRightCoordinate * (textView.ZoomLevel / 100), this.minWidth); // Width of the widest line.

            this.height = textView.LineHeight * (textView.ZoomLevel / 100) * // Height of each line.
                     snapshot.LineCount;                                // Number of lines.

            // Calculation of required height and width is now complete.
            this.taskCompletion.SetResult(null);
        }
    }
}
