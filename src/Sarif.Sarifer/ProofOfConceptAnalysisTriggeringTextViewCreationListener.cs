// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// An <see cref="ITextViewCreationListener"/> that triggers a background analyzer that streams
    /// its results as a SARIF log to the SARIF Viewer extension.
    /// </summary>
    /// <remarks>
    /// The purpose of this class is to demonstrate that it's possible to write a VS extension that
    /// wakes up whenever a file is opened, analyzes it in the background, and sends its results as
    /// SARIF to the viewer. There is no attempt here to make the code efficient, to use an analysis
    /// tool framework such as the SARIF SDK Driver framework, to save memory, or even to separate
    /// concerns such as buffer management, analysis, SARIF creation, and communication with the
    /// viewer. All that can happen once we have the analysis pipeline working end to end, with a
    /// UI level test to validate it.
    /// </remarks>
    [ContentType(AnyContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Export(typeof(ITextViewCreationListener))]
    public class ProofOfConceptAnalysisTriggeringTextViewCreationListener : ITextViewCreationListener
    {
        private const string AnyContentType = "any";

        /// <inheritdoc/>
        public void TextViewCreated(ITextView textView)
        {
            textView = textView ?? throw new ArgumentNullException(nameof(textView));

            // For now, pretend that there is only one analyzer, and it will analyze any
            // file type.
            ProofOfConceptBackgroundAnalyzer.AnalyzeAsync(textView.TextBuffer.CurrentSnapshot.GetText())
                .FileAndForget(FileAndForgetEventName.SendDataToViewerFailure);
        }
    }
}
