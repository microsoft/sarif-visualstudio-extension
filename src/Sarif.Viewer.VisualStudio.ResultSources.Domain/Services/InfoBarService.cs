// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services
{
    internal class InfoBarService : IVsInfoBarUIEvents
    {
        private readonly IServiceProvider serviceProvider;
        private uint cookie;

        private InfoBarService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public static InfoBarService Instance { get; private set; }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            Instance = new InfoBarService(serviceProvider);
        }

        public Result<IVsInfoBarUIElement> ShowInfoBar(InfoBarModel infoBarModel)
        {
            Result<IVsInfoBarHost> getHostResult = GetInfoBarHost();

            if (getHostResult.IsSuccess)
            {
                var factory = serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                Assumes.Present(factory);
                IVsInfoBarUIElement element = factory.CreateInfoBar(infoBarModel);
                element.Advise(this, out cookie);
                getHostResult.Value.AddInfoBar(element);
                return Result.Success(element);
            }

            return Result.Failure<IVsInfoBarUIElement>("Unable to show infobar");
        }

        public Result CloseInfoBar(IVsInfoBarUIElement element)
        {
            Result<IVsInfoBarHost> getHostResult = GetInfoBarHost();

            if (getHostResult.IsSuccess)
            {
                getHostResult.Value.RemoveInfoBar(element);
                return Result.Success();
            }

            return Result.Failure("Unable to close infobar");
        }

        public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
            infoBarUIElement.Unadvise(cookie);
        }

        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            // Action callback = actionItem.ActionContext as Action;

            if (actionItem.ActionContext is Func<Task> callback)
            {
                // callback().GetAwaiter().GetResult();
                ThreadHelper.JoinableTaskFactory.Run(async () => await callback());
            }
        }

        private Result<IVsInfoBarHost> GetInfoBarHost()
        {
            if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
            {
                // Get the main window handle to host our InfoBar
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out object obj);
                var host = (IVsInfoBarHost)obj;

                if (host != null)
                {
                    return Result.Success(host);
                }
            }

            return Result.Failure<IVsInfoBarHost>("Unable to create infobar host");
        }
    }
}
