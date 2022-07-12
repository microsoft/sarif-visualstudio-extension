// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Sarif.Viewer.Shell
{
    /// <summary>
    /// Provides a service to manage the Visual Studio status bar.
    /// </summary>
    public interface IStatusBarService
    {
        /// <summary>
        /// Displays the specified text in the status bar texdt area.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SetStatusTextAsync(string text);

        /// <summary>
        /// Animates the status bar text.
        /// </summary>
        /// <param name="textFormat">A string specifying the format of the text. Only one token ({0}) is supported.</param>
        /// <param name="frames">An array of strings that defines the dynamic portion of the text.</param>
        /// <param name="millisecondsInterval">The interval between frames, in milliseconds.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the animation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AnimateStatusTextAsync(
            string textFormat,
            string[] frames,
            int millisecondsInterval,
            CancellationToken cancellationToken);

        /// <summary>
        /// Clears the status bar text area.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ClearStatusTextAsync();
    }
}
