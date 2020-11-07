// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// An <see cref="ITextViewCreationListener"/> that triggers a background analyzer that streams
    /// its results as a SARIF log to the SARIF Viewer extension.
    /// </summary>
    [ContentType(AnyContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Export(typeof(ITextViewCreationListener))]
    public class AnalysisTriggeringTextViewCreationListener : ITextViewCreationListener
    {
        private const string AnyContentType = "any";

        /// <inheritdoc/>
        public void TextViewCreated(ITextView textView)
        {
            textView = textView ?? throw new ArgumentNullException(nameof(textView));

            // For now, pretend that there is only one analyzer, and it will analyze any
            // file type.
            new FakeBackgroundAnalyzer().Analyze(textView.TextBuffer);
        }
    }
}
