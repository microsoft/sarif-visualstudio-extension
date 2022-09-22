﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer
{
    public class OutputWindowTracerListener : TraceListener
    {
        private readonly string _name;
        private readonly IVsOutputWindow _outputWindowService;

        private IVsOutputWindowPane pane;

        public OutputWindowTracerListener(IVsOutputWindow outputWindowService, string name)
        {
            this._outputWindowService = outputWindowService;
            this._name = name;
            Trace.Listeners.Add(this);
        }

        public override void Write(string message)
        {
            try
            {
                if (this.EnsurePane())
                {
                    if (!SarifViewerPackage.IsUnitTesting)
                    {
                        ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            this.pane.OutputStringThreadSafe(message);
                        }).FileAndForget(Constants.FileAndForgetFaultEventNames.OutputWindowEvent);
                    }
                    else
                    {
#pragma warning disable VSTHRD010 // Only for unit tests.
                        this.pane.OutputStringThreadSafe(message);
#pragma warning restore VSTHRD010
                    }
                }
            }
            catch (COMException)
            {
                // COMException is thrown if called from non UI thread.
            }
        }

        public override void WriteLine(string message)
        {
#pragma warning disable VSTHRD010 // Thread check inside this.Write().
            this.Write(Environment.NewLine + message);
#pragma warning restore VSTHRD010
        }

        private bool EnsurePane()
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally.
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            if (this.pane == null)
            {
                var guid = Guid.NewGuid();
                this._outputWindowService.CreatePane(ref guid, this._name, fInitVisible: 1, fClearWithSolution: 1);
                this._outputWindowService.GetPane(ref guid, out this.pane);
            }

            return this.pane != null;
        }
    }
}
