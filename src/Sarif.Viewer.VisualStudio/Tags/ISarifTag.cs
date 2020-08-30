// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.TextManager.Interop;
    using System.ComponentModel;

    internal interface ISarifTag: INotifyPropertyChanged
    {
        IPersistentSpan PersistentSpan { get; }

        TextSpan InitialSpan { get; }

        TextMarkerTag Tag { get; set; }
    }
}
