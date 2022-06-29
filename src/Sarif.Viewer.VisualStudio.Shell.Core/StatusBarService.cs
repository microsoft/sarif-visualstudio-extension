// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Task = System.Threading.Tasks.Task;

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
                    Assumes.Present(statusBar);
                }

                return statusBar;
            }
        }

        /// <summary>
        /// Displays the specified text in the status bar texdt area.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SetStatusTextAsync(string text)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.StatusBar.IsFrozen(out int frozen);

            if (frozen != 0)
            {
                this.StatusBar.FreezeOutput(0);
            }

            this.StatusBar.SetText(text);
        }

        /// <summary>
        /// Animates the specified text using the specified frames.
        /// </summary>
        /// <param name="textFormat">The text format string. Only one token ({0}) is supported.</param>
        /// <param name="frames">The array of animation frames.</param>
        /// <param name="millisecondsInterval">The interval between frames, in milliseconds.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AnimateStatusTextAsync(
            string textFormat,
            string[] frames,
            int millisecondsInterval,
            CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            for (int i = 0; !cancellationToken.IsCancellationRequested; i++)
            {
                string text = string.Format(textFormat, frames[i % frames.Length]);
                await this.SetStatusTextAsync(text);

                await Task.Delay(millisecondsInterval);
            }

            await this.ClearStatusTextAsync();
        }

        /// <summary>
        /// Clears the status bar text area.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ClearStatusTextAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            this.StatusBar.IsFrozen(out int frozen);

            if (frozen != 0)
            {
                this.StatusBar.FreezeOutput(0);
            }

            // This call succeeds but the text remains
            // this.StatusBar.Clear();

            // Workaround
            statusBar.SetText(string.Empty);
        }
    }
}
