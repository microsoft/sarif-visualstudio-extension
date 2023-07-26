// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Windows.Documents;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    /// <summary>
    /// Event args that get fired when a file is opened by the user within the VS editor.
    /// </summary>
    public class FilesOpenedEventArgs : ServiceEventArgs
    {
        /// <summary>
        /// The absolute file path of the file that was opened.
        /// </summary>
        public List<string> FileOpened;

        public FilesOpenedEventArgs()
        {
            ServiceEventType = ResultSourceServiceEventType.ResultsUpdated;
        }
    }
}
