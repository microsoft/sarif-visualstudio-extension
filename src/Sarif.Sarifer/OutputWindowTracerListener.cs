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
        private IVsOutputWindowPane pane;
        private IVsOutputWindow _outputWindowService;
        private string _name;

        public OutputWindowTracerListener(IVsOutputWindow outputWindowService, string name)
        {
            _outputWindowService = outputWindowService;
            _name = name;
            Trace.Listeners.Add(this);
        }

        private bool EnsurePane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (pane == null)
            {
                Guid guid = Guid.NewGuid();
                _outputWindowService.CreatePane(ref guid, _name, 1, 1);
                _outputWindowService.GetPane(ref guid, out pane);
            }

            return pane != null;
        }

        public override void Write(string message)
        {
            try
            {
                if (EnsurePane())
                {
                    ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        pane.OutputString(message);
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
            Write(Environment.NewLine + message);
#pragma warning restore VSTHRD010
        }
    }
}
