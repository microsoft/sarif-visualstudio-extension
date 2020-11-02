// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.VisualStudio.Text;

namespace Microsoft.Sarif.Viewer.Fixes
{
    internal interface IPreviewProvider
    {
        Task<object> CreateChangePreviewAsync(
            ITextBuffer buffer,
            Action<ITextBuffer, ITextSnapshot> applyEdit,
            string description = null,
            FrameworkElement additionalContent = null);
    }
}
