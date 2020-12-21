// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer.Fixes
{
    /// <summary>
    /// Provides a <see cref="FixSuggestedActionsSource"/> for a specified <see cref="ITextView"/>
    /// and <see cref="ITextBuffer"/>.
    /// </summary>
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name(FixActionCategoryName)]
    [ContentType(ContentTypes.Any)]
    internal class FixSuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        private const string FixActionCategoryName = "SARIF fix suggestion";

#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF
        [Import]
        private IPersistentSpanFactory persistentSpanFactory;

        [Import]
        private IPreviewProvider previewProvider;
#pragma warning restore IDE0044
#pragma warning restore CS0649

        /// <inheritdoc/>
        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            if (textBuffer == null && textView == null)
            {
                return null;
            }

            return new FixSuggestedActionsSource(textView, textBuffer, this.persistentSpanFactory, this.previewProvider);
        }
    }
}
