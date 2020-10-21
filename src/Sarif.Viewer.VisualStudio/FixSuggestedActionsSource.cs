// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.Sarif.Viewer
{
    internal class FixSuggestedActionsSource : ISuggestedActionsSource
    {
        public FixSuggestedActionsSource(FixSuggestedActionsSourceProvider fixSuggestedActionsSourceProvider, ITextView textView, ITextBuffer textBuffer)
        {
            FixSuggestedActionsSourceProvider = fixSuggestedActionsSourceProvider;
            TextView = textView;
            TextBuffer = textBuffer;
        }

        public FixSuggestedActionsSourceProvider FixSuggestedActionsSourceProvider { get; }
        public ITextView TextView { get; }
        public ITextBuffer TextBuffer { get; }

#pragma warning disable 0067
        public event EventHandler<EventArgs> SuggestedActionsChanged;
#pragma warning restore 0067

        public void Dispose()
        {
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            yield return new SuggestedActionSet(new FixSuggestedAction[0]);
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
