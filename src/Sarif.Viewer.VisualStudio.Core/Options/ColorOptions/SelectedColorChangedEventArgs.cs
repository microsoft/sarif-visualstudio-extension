// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Sarif.Viewer.Options
{
    public class SelectedColorChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The new index to use in the list of highlight colors.
        /// </summary>
        public int NewIndex;

        /// <summary>
        /// The error type which is having its' color changed.
        /// </summary>
        public string ErrorType;
    }
}
