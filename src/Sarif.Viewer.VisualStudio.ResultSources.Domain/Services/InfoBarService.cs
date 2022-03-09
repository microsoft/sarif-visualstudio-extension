// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
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

        public Result ShowInfoBar(InfoBarModel infoBarModel)
        {
            if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
            {
                // Get the main window handle to host our InfoBar
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out object obj);
                var host = (IVsInfoBarHost)obj;

                if (host != null)
                {
                    var factory = serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                    Assumes.Present(factory);
                    IVsInfoBarUIElement element = factory.CreateInfoBar(infoBarModel);
                    element.Advise(this, out cookie);
                    host.AddInfoBar(element);
                }
            }

            return Result.Failure("Unable to show infobar");
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
    }
}
