// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.Sarif.Viewer.Interop;
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
    public class BackgroundAnalysisTextViewCreationListener : ITextViewCreationListener, IDisposable
    {
        private const string AnyContentType = "any";
        private SarifViewerInterop sarifViewerInterop;
        private TextEditIdleAssistant inputAssistant;
        private bool subscribed;
        private bool disposed;
        private int isRunning;

#pragma warning disable IDE0044, CS0649 // Provided by MEF
        [Import]
        private Lazy<IBackgroundAnalysisService> backgroundAnalysisService;

        [Import]
        private Lazy<IVsEditorAdaptersFactoryService> vsEditorAdaptersFactoryService;

        [Import]
        private ITextBufferViewTracker textBufferViewTracker;
#pragma warning restore IDE0044, CS0649

        /// <inheritdoc/>
        public void TextViewCreated(ITextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.sarifViewerInterop == null)
            {
                if (Package.GetGlobalService(typeof(SVsShell)) is IVsShell vsShell)
                {
                    this.sarifViewerInterop = new SarifViewerInterop(vsShell);
                }

                if (!this.sarifViewerInterop.IsSariferExtensionLoaded)
                {
                    // need to load package to get extension option setting values
                    this.sarifViewerInterop.LoadSariferExtension();
                }
            }

            if (!SariferOption.Instance.IsBackgroundAnalysisEnabled)
            {
                return;
            }

            if (!this.subscribed)
            {
                // ITextViewCreationListener is not IDisposable, so the ITextBufferManager will
                // never be removed from memory. This isn't a problem because the listener will
                // never be removed from memory either; we want it to live as long as the extension
                // is loaded.
                this.textBufferViewTracker.FirstViewAdded += this.TextBufferViewTracker_FirstViewAdded;
                this.textBufferViewTracker.LastViewRemoved += this.TextBufferViewTracker_LastViewRemoved;
                this.textBufferViewTracker.ViewUpdated += this.TextBufferViewTracker_ViewUpdated;
                this.subscribed = true;
            }

            textView = textView ?? throw new ArgumentNullException(nameof(textView));

            string text = textView.TextBuffer.CurrentSnapshot.GetText();
            string path = this.GetPathFromTextView(textView);

            textView.Closed += (object sender, EventArgs e) => this.TextView_Closed(textView);

            // TextBuffer.Changed event is a performance critical event, whose handlers directly affect typing responsiveness.
            // Unless it's required to handle this event synchronously on the UI thread.
            // As suggested listen to Microsoft.VisualStudio.Text.ITextBuffer2.ChangedOnBackground event instead.
            if (textView.TextBuffer is VisualStudio.Text.ITextBuffer2 oldBuffer)
            {
                oldBuffer.ChangedOnBackground += (object sender, VisualStudio.Text.TextContentChangedEventArgs e) => this.TextBuffer_ChangedOnBackground(textView);
            }

            this.textBufferViewTracker.AddTextView(textView, path, text);

            this.inputAssistant = new TextEditIdleAssistant();
            this.inputAssistant.Idled += this.InputAssistant_Idled;
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.textBufferViewTracker.Clear();
                    this.inputAssistant.Dispose();
                }

                this.disposed = true;
            }
        }

        private void InputAssistant_Idled(object sender, TextEditIdledEventArgs e)
        {
            ITextView textView = e.TextView;
            string text = textView.TextBuffer.CurrentSnapshot.GetText();
            this.textBufferViewTracker.UpdateTextView(textView, text);
        }

        private void TextBufferViewTracker_ViewUpdated(object sender, ViewUpdatedEventArgs e)
        {
            this.BackgroundAnalyzeAsync(e.Path, e.Text, e.CancellationToken)
                .FileAndForget(FileAndForgetEventName.BackgroundAnalysisFailure);
        }

        private void TextBuffer_ChangedOnBackground(ITextView textView)
        {
            this.inputAssistant.TextChanged(new TextEditIdledEventArgs(textView));
        }

        private void TextBufferViewTracker_FirstViewAdded(object sender, FirstViewAddedEventArgs e)
        {
            this.BackgroundAnalyzeAsync(e.Path, e.Text, e.CancellationToken)
                .FileAndForget(FileAndForgetEventName.BackgroundAnalysisFailure);
        }

        private async System.Threading.Tasks.Task BackgroundAnalyzeAsync(string path, string text, CancellationToken cancellationToken)
        {
            if (!this.IsRunning())
            {
                this.SetRun(true);
                try
                {
                    await this.backgroundAnalysisService.Value.AnalyzeAsync(path, text, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    this.SetRun(false);
                }
            }
        }

        private void TextView_Closed(ITextView textView)
        {
            this.textBufferViewTracker.RemoveTextView(textView);
        }

        // If this is the last view on a buffer, remove any results for this buffer from the
        // error list.
        private void TextBufferViewTracker_LastViewRemoved(object sender, LastViewRemovedEventArgs e)
        {
            this.sarifViewerInterop.CloseSarifLogAsync(new string[] { e.Path })
                .FileAndForget(FileAndForgetEventName.CloseSarifLogsFailure);
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

        private bool IsRunning() => Interlocked.CompareExchange(ref this.isRunning, 1, 1) == 1;

        private bool SetRun(bool run) => (run ?
            Interlocked.CompareExchange(ref this.isRunning, 1, 0) : Interlocked.CompareExchange(ref this.isRunning, 0, 1)) == 1;
    }
}
