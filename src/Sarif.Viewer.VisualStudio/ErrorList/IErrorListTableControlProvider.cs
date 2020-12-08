// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    internal interface IErrorListTableControlProvider
    {
        IWpfTableControl GetErrorListTableControl();
    }
}
