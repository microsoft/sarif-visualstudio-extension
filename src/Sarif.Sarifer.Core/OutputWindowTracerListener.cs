// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class OutputWindowTracerListener : TraceListener
    {
        private readonly IVsOutputWindow _outputWindowService;
        private readonly string _name;
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
                    _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                      {
                          await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                          this.pane.OutputStringThreadSafe(message);
                      });
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                // Do nothing
            }
        }

        public override void WriteLine(string message)
        {
#pragma warning disable VSTHRD010
            this.Write(Environment.NewLine + message);
#pragma warning restore VSTHRD010
        }

        private bool EnsurePane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (this.pane == null)
            {
                var guid = Guid.NewGuid();
                this._outputWindowService.CreatePane(ref guid, this._name, 1, 1);
                this._outputWindowService.GetPane(ref guid, out this.pane);
            }

            return this.pane != null;
        }
    }
}
