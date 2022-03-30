// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using CSharpFunctionalExtensions;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.Shell
{
    public class InfoBarService : IVsInfoBarUIEvents
    {
        private readonly IServiceProvider serviceProvider;
        private uint cookie;

        public InfoBarService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IVsInfoBarUIElement ShowInfoBar(InfoBarModel infoBarModel)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Result<InfoBarHostControl> getHostResult = GetInfoBarHost();

            if (getHostResult.IsSuccess)
            {
                var factory = serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                Assumes.Present(factory);
                IVsInfoBarUIElement element = factory.CreateInfoBar(infoBarModel);
                element.Advise(this, out cookie);
                getHostResult.Value.AddInfoBar(element);
                return element;
            }

            return null;
        }

        public Result CloseInfoBar(IVsInfoBarUIElement element)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Result<InfoBarHostControl> getHostResult = GetInfoBarHost();

            if (getHostResult.IsSuccess)
            {
                getHostResult.Value.RemoveInfoBar(element);
                return Result.Success();
            }

            return Result.Failure("Unable to close infobar");
        }

        public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            infoBarUIElement.Unadvise(cookie);
        }

        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (actionItem.ActionContext is Func<Task> callback)
            {
                ThreadHelper.JoinableTaskFactory.Run(async () => await callback());
            }
        }

        private Result<InfoBarHostControl> GetInfoBarHost()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
            {
                // Get the main window handle to host our InfoBar
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out object obj);

                if (obj is InfoBarHostControl host)
                {
                    return Result.Success(host);
                }
            }

            return Result.Failure<InfoBarHostControl>("Unable to create infobar host");
        }
    }
}
