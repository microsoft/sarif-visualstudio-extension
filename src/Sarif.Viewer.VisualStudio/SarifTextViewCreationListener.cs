// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;

using Microsoft.CodeAnalysis.Sarif.Converters;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// Factory for creating our editors.
    /// </summary>
    [ContentType(ContentTypes.Sarif)]
    [ContentType(ContentTypes.Text)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Export(typeof(ITextViewCreationListener))]
    public class SarifTextViewCreationListener : ITextViewCreationListener
    {
#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF
        [Import]
        private Lazy<IVsEditorAdaptersFactoryService> vsEditorAdaptersFactoryService;
#pragma warning restore IDE0044
#pragma warning restore CS0649

        private readonly ConcurrentDictionary<ITextBuffer, int> textBufferMap = new ConcurrentDictionary<ITextBuffer, int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SarifTextViewCreationListener"/> class.
        /// Explicitly defined default constructor.
        /// </summary>
        public SarifTextViewCreationListener()
        {
        }

        public void TextViewCreated(ITextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            textView.Closed += this.TextView_Closed;

            if (this.TryGetFileNameFromTextView(textView, out string filename) &&
                this.IsSarifLogFile(filename))
            {
                // since Json (base type of sarif log) editor throws error when file size is greater than 5 MBs
                // need to listen to content type "text". Only process log if file extension is .sarif or .json.
                if (!textBufferMap.ContainsKey(textView.TextBuffer))
                {
                    textBufferMap.TryAdd(textView.TextBuffer, 0);
                    if (!ErrorListService.IsSarifLogOpened(filename))
                    {
                        ErrorListService.ProcessLogFile(filename, ToolFormat.None, promptOnLogConversions: true, cleanErrors: false, openInEditor: false);
                    }
                }

                textBufferMap[textView.TextBuffer]++;
            }
        }

        private void TextView_Closed(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is ITextView textView)
            {
                textView.Closed -= this.TextView_Closed;

                if (this.TryGetFileNameFromTextView(textView, out string filename) &&
                    this.IsSarifLogFile(filename))
                {
                    if (textBufferMap.ContainsKey(textView.TextBuffer))
                    {
                        textBufferMap[textView.TextBuffer]--;

                        if (textBufferMap[textView.TextBuffer] <= 0)
                        {
                            ErrorListService.CloseSarifLogs(new[] { filename });
                            textBufferMap.TryRemove(textView.TextBuffer, out int value);
                        }
                    }
                }
            }
        }

        private bool TryGetFileNameFromTextView(ITextView textView, out string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            fileName = null;

            IVsTextView vsTextView = this.vsEditorAdaptersFactoryService.Value.GetViewAdapter(textView);
            if (vsTextView == null)
            {
                return false;
            }

            if (vsTextView.GetBuffer(out IVsTextLines vsTextLines) != VSConstants.S_OK)
            {
                return false;
            }

            if (!(vsTextLines is IPersistFileFormat persistFile))
            {
                return false;
            }

            return persistFile.GetCurFile(out fileName, out _) == VSConstants.S_OK;
        }

        private bool IsSarifLogFile(string fileName)
        {
            return !string.IsNullOrEmpty(fileName) &&
                (fileName.EndsWith(".sarif", StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        }
    }
}
