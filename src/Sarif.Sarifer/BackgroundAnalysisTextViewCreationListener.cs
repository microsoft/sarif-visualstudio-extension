// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// An <see cref="ITextViewCreationListener"/> that triggers background analysis.
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
        private Lazy<IBackgroundAnalysisService> backgroundAnalysisService;

        [Import]
        private Lazy<IVsEditorAdaptersFactoryService> vsEditorAdaptersFactoryService;
#pragma warning restore IDE0044
#pragma warning restore CS0649

        /// <inheritdoc/>
        public void TextViewCreated(ITextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            textView = textView ?? throw new ArgumentNullException(nameof(textView));

            string text = textView.TextBuffer.CurrentSnapshot.GetText();
            string path = GetPathFromTextView(textView);

            this.backgroundAnalysisService.Value.StartAnalysis(path, text);

        }

        private string GetPathFromTextView(ITextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsTextView vsTextView = this.vsEditorAdaptersFactoryService.Value.GetViewAdapter(textView);
            if (vsTextView == null)
            {
                return null;
            }

            if (vsTextView.GetBuffer(out IVsTextLines vsTextLines) != VSConstants.S_OK)
            {
                return null;
            }

            if (!(vsTextLines is IPersistFileFormat persistFile))
            {
                return null;
            }

            return persistFile.GetCurFile(out string path, out _) == VSConstants.S_OK
                ? path
                : null;
        }
    }
}
