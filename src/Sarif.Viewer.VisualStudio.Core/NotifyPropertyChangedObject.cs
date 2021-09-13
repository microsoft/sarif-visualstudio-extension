// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Microsoft.Sarif.Viewer
{
    public abstract class NotifyPropertyChangedObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string info = null)
        {
            if (string.IsNullOrEmpty(info))
            {
                throw new ArgumentNullException(nameof(info));
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
