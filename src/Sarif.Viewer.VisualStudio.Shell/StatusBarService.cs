// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer.Shell
{
    public class StatusBarService
    {
        private readonly IServiceProvider serviceProvider;
        private IVsStatusbar statusBar;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusBarService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public StatusBarService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets the status bar.
        /// </summary>
        /// <value>The status bar.</value>
        protected IVsStatusbar StatusBar
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (statusBar == null)
                {
                    statusBar = serviceProvider.GetService(typeof(SVsStatusbar)) as IVsStatusbar;
                }

                return statusBar;
            }
        }

        /// <summary>
        /// Displays the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void ShowStatusText(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            StatusBar.IsFrozen(out int frozen);

            if (frozen == 0)
            {
                StatusBar.SetText(message);
            }
        }
    }
}
