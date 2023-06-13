// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Sarif.Viewer.VisualStudio.ResultSources.Domain.Core.Models
{
    /// <summary>
    /// Event args that get fired when a file is opened by the user within the VS editor.
    /// </summary>
    public class FilesOpenedEventArgs : EventArgs
    {
        /// <summary>
        /// The absolute file path of the file that was opened.
        /// </summary>
        public string FileOpened;
    }
}
