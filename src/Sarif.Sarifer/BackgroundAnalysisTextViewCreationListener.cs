// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// An <see cref="ITextViewCreationListener"/> that triggers background analysis that streams
    /// its results as a SARIF log to the SARIF Viewer extension.
    /// </summary>
    [ContentType(AnyContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Export(typeof(ITextViewCreationListener))]
    public class BackgroundAnalysisTextViewCreationListener : ITextViewCreationListener
    {
        private const string AnyContentType = "any";

#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF

        [Import]
        private IBackgroundAnalysisService backgroundAnalysisService;

#pragma warning restore IDE0044
#pragma warning restore CS0649

        /// <inheritdoc/>
        public void TextViewCreated(ITextView textView)
        {
            textView = textView ?? throw new ArgumentNullException(nameof(textView));
            string text = textView.TextBuffer.CurrentSnapshot.GetText();

            this.backgroundAnalysisService.StartAnalysis(text);

        }
    }
}
