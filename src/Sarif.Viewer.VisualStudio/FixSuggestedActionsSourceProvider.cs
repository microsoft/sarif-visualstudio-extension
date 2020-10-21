// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// Provides a <see cref="FixSuggestedActionsSource"/> for a specified <see cref="ITextView"/>
    /// and <see cref="ITextBuffer"/>
    /// </summary>
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name(FixActionCategoryName)]
    [ContentType("text")]
    internal class FixSuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        private const string FixActionCategoryName = "SARIF fix suggestion";

        [Import(typeof(ITextStructureNavigatorSelectorService))]
        private ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        /// <inheritdoc/>
        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            if (textBuffer == null && textView == null)
            {
                return null;
            }

            return new FixSuggestedActionsSource(this, textView, textBuffer);
        }
    }
}
