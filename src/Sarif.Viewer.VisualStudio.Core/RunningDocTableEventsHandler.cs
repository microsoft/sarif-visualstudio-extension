// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using EnvDTE;

using EnvDTE80;

using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Workspace.Indexing;

namespace Sarif.Viewer.VisualStudio.Core
{
    /// <summary>
    /// The class handles and listens to files being opened and fires events for result plugins to respond to.
    /// </summary>
    public class RunningDocTableEventsHandler : IVsRunningDocTableEvents
    {
        private const int PollPeriodInMS = 10000;

        /// <summary>
        /// Event that gets fired when files are opened in the VS editor window.
        /// </summary>
        public event EventHandler<FilesOpenedEventArgs> ServiceEvent;

        private readonly IVsRunningDocumentTable ivsRunningDocTable;

        private readonly DTE dte;

        private Timer pollTimer;

        public RunningDocTableEventsHandler(IVsRunningDocumentTable ivsRunningDocTable)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.ivsRunningDocTable = ivsRunningDocTable;
            this.dte = (DTE)Package.GetGlobalService(typeof(DTE));
            pollTimer = new Timer(OnPollTimerFired, null, PollPeriodInMS, Timeout.Infinite);
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string docMoniker = (string)pFrame.GetType().GetProperty("DocumentMoniker").GetValue(pFrame, null);
            ThreadPool.QueueUserWorkItem((a) =>
                DocHandler_Service_Event(docMoniker));
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        private void DocHandler_Service_Event(string filePath)
        {
            DocHandler_Service_Event(new FilesOpenedEventArgs() { FileOpened = new List<string>() { filePath } });
        }

        private void DocHandler_Service_Event(List<string> filePaths)
        {
            DocHandler_Service_Event(new FilesOpenedEventArgs() { FileOpened = filePaths });
        }

        private void DocHandler_Service_Event(FilesOpenedEventArgs e)
        {
            ServiceEvent?.Invoke(this, e);
        }

        /// <summary>
        /// Called when the poll timer fires and sees if VS is done scanning the files in the workspace.
        /// If so, the collection is updated and <see cref="FilesAdded"/> may be fired to report any new files.
        /// For a solution:
        /// We wait one poll period to ensure VS has fully loaded the solution. In practice, we've seen that VS
        /// doesn't return any files for the solution if we ask for them right after the solution is opened.
        /// For "Open Folder":
        /// We wait until the file indexer service reaches a final state before we scan the workspace for files.
        /// Depending on the size of the workspace we may have to restart the poll timer to keep checking the
        /// file indexer service.
        /// </summary>
        /// <param name="state">State when the timer is fired.</param>
#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void OnPollTimerFired(object state)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            pollTimer = null;
            List<string> fileNamesOpen = new List<string>();
            DTE2 dte2 = (DTE2)this.dte;
            foreach (Window window in dte2.Windows)
            {
                try
                {
                    if (window.Document != null)
                    {
                        string fileName = window.Document.FullName;
                        fileNamesOpen.Add(fileName);
                    }
                }
                catch (Exception)
                {
                    // swallow, sometimes grabbing the doc from a window fails ex: it was a temp file that has been removed since last time this was opened.
                }
            }

            // move off of the UI thread as soon as we have the data neeed
            ThreadPool.QueueUserWorkItem(a => FinishPollTask(fileNamesOpen));
        }

        private void FinishPollTask(List<string> filePathList)
        {
            DocHandler_Service_Event(filePathList);
        }
    }
}
