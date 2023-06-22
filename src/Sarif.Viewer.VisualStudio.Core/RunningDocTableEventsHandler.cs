// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using EnvDTE;

using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Workspace.Indexing;

namespace Sarif.Viewer.VisualStudio.Core
{
    /// <summary>
    /// The class handles and listens to files being opened and fires events.
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

        private readonly Timer pollTimer;

        public RunningDocTableEventsHandler(IVsRunningDocumentTable ivsRunningDocTable, DTE dte)
        {
            this.ivsRunningDocTable = ivsRunningDocTable;
            this.dte = dte;
            pollTimer = new Timer(OnPollTimerFired, null, PollPeriodInMS, Timeout.Infinite);
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            /*ThreadHelper.ThrowIfNotOnUIThread();
            if (runningDocTable != null)
            {
                IVsHierarchy hierarchy;
                uint itemId;
                IntPtr docData;
                string fileName = null;

                int hr = runningDocTable.GetDocumentInfo(docCookie, out uint a, out uint b, out uint c, out string pbstrMkDocument, out hierarchy, out itemId, out docData);
                if (hr == VSConstants.S_OK && hierarchy != null)
                {
                    hierarchy.GetCanonicalName(itemId, out fileName);
                    DocHandler_Service_Event(fileName);
                }

                // Now you have the file name of the document that was opened
            }
*/
            // This method is called when a document is opened in the editor
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
            if (ivsRunningDocTable != null)
            {
                IVsHierarchy hierarchy;
                uint itemId;
                IntPtr docData;
                string fileName = null;

                int hr = ivsRunningDocTable.GetDocumentInfo(docCookie, out uint a, out uint b, out uint c, out string pbstrMkDocument, out hierarchy, out itemId, out docData);
                if (hr == VSConstants.S_OK && hierarchy != null)
                {
                    hierarchy.GetCanonicalName(itemId, out fileName);
                    DocHandler_Service_Event(fileName);
                }

                // Now you have the file name of the document that was opened
            }

            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        private void DocHandler_Service_Event(string filePath)
        {
            DocHandler_Service_Event(new FilesOpenedEventArgs() { FileOpened = filePath });
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
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async void OnPollTimerFired(object state)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                IVsUIShell uiShell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));
                int outputCode = uiShell.GetDocumentWindowEnum(out IEnumWindowFrames windowEnumerator);

                // IntPtr hwnd;
                IVsWindowFrame[] windowFrames = new IVsWindowFrame[1];
                while (windowEnumerator.Next(1, windowFrames, out uint fetched) == 0 && fetched == 1)
                {
                    Console.WriteLine(windowFrames);
                    /*windowFrames[0].GetProperty((int)__VSFPROPID.VSFPROPID_Hwnd, out hwnd);
                    if (hwnd != IntPtr.Zero)
                    {
                        // Do something with the open document
                    }*/
                }
            }
            catch (Exception)
            {
                // Swallow to prevent the extension from crashing.
            }
        }
    }
}
