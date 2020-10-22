// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Sarif.Converters;
using Microsoft.Sarif.Viewer.ContentTypes;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// Factory for creating our editors.
    /// </summary>
    [ContentType(ContentTypes.ContentTypes.Sarif)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Export(typeof(ITextViewCreationListener))]
    public class SarifTextViewCreationListener : ITextViewCreationListener
    {
        #region Fields
#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF
        [Import]
        private Lazy<IVsEditorAdaptersFactoryService> vsEditorAdaptersFactoryService;
#pragma warning restore IDE0044
#pragma warning restore CS0649
        #endregion

        #region Constructors
        /// <summary>
        /// Explicitly defined default constructor.
        /// Initialize new instance of the EditorFactory object.
        /// </summary>
        public SarifTextViewCreationListener()
        {
        }

        #endregion Constructors

        #region Methods

        #region ITextViewCreationListener Members

        public void TextViewCreated(ITextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            textView.Closed += this.TextView_Closed;

            if (this.TryGetFileNameFromTextView(textView, out var filename))
            {
                ErrorListService.ProcessLogFile(filename, ToolFormat.None, promptOnLogConversions: true, cleanErrors: false, openInEditor: false);
            }
        }

        #endregion

        private void TextView_Closed(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is ITextView textView)
            {
                textView.Closed -= this.TextView_Closed;

                if (this.TryGetFileNameFromTextView(textView, out var filename))
                {
                    ErrorListService.CloseSarifLogs(new[] { filename });
                }
            }
        }

        private bool TryGetFileNameFromTextView(ITextView textView, out string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            fileName = null;

            var vsTextView = this.vsEditorAdaptersFactoryService.Value.GetViewAdapter(textView);
            if (vsTextView == null)
            {
                return false;
            }

            if (vsTextView.GetBuffer(out var vsTextLines) != VSConstants.S_OK)
            {
                return false;
            }

            if (!(vsTextLines is IPersistFileFormat persistFile))
            {
                return false;
            }

            return persistFile.GetCurFile(out fileName, out _) == VSConstants.S_OK;
        }

        #endregion
    }
}
