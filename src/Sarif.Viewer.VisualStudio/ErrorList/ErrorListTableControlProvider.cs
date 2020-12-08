// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    [Export(typeof(IErrorListTableControlProvider))]
    internal class ErrorListTableControlProvider : IErrorListTableControlProvider
    {
        private IWpfTableControl errorListTableControl;

        public IWpfTableControl GetErrorListTableControl()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.errorListTableControl == null)
            {
                var errorList = ServiceProvider.GlobalProvider.GetService(typeof(SVsErrorList)) as IErrorList;
                if (errorList != null)
                {
                    this.errorListTableControl = errorList.TableControl;
                }
            }

            return this.errorListTableControl;
        }
    }
}
