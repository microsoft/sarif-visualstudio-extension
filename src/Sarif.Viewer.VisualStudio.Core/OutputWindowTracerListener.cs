// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer
{
    public class OutputWindowTracerListener : TraceListener
    {
        private const string OutputWindowEvent = "Microsoft/SARIF/Viewer/WriteToOutputPane/Failed";

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
            if (this.EnsurePane())
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    this.pane.OutputStringThreadSafe(message);
                }).FileAndForget(OutputWindowEvent);
            }
        }

        public override void WriteLine(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.Write(Environment.NewLine + message);
        }

        private bool EnsurePane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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
