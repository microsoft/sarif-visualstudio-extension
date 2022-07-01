// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CSharpFunctionalExtensions;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer.Shell
{
    /// <summary>
    /// Provides a service to manage Visual Studio info bars.
    /// </summary>
    public interface IInfoBarService
    {
        /// <summary>
        /// Displays an info bar.
        /// </summary>
        /// <param name="infoBarModel">The <see cref="InfoBarModel"/> that specifies the settings.</param>
        /// <returns>The <see cref="IVsInfoBarUIElement"/>.</returns>
        IVsInfoBarUIElement ShowInfoBar(InfoBarModel infoBarModel);

        /// <summary>
        /// Closes the specified info bar.
        /// </summary>
        /// <param name="element">The <see cref="IVsInfoBarUIElement"/> to be closed.</param>
        /// <returns>The <see cref="Result"/> of the operation.</returns>
        Result CloseInfoBar(IVsInfoBarUIElement element);
    }
}
