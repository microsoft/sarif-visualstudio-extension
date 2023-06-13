// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Sarif.Viewer.VisualStudio.ResultSources.Domain.Core.Models;

namespace Sarif.Viewer.VisualStudio.Core
{
    public class RunningDocTableEventsHandler : IVsRunningDocTableEvents
    {
        public event EventHandler<FilesOpenedEventArgs> ServiceEvent;

        private readonly IVsRunningDocumentTable runningDocTable;

        public RunningDocTableEventsHandler(IVsRunningDocumentTable runningDocTable)
        {
            this.runningDocTable = runningDocTable;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
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
            // DocHandler_Service_Event(this, new FilesOpenedEventArgs() { FileOpened = pFrame.});
/*            var x = pFrame.
            if (pFrame is Microsoft.VisualStudio.PlatformUI.WindowManagment.WindowFrame.MarshalingWindowFrame windowFrame)
            {
                var x = windowFrame.DocumentMoniker;
            }*/
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
    }
}
