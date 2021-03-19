// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Options
{
    [ComVisible(true)]
    public class SarifViewerOptionPage : DialogPage
    {
        private const string CategoryName = "Sarif Viewer Options";

        private const string MonitorSarifFolderDisplayName = "Enable loading sarif results in .sarif folder automatically";
        private const string MonitorSarifFolderDescription = "If enabled, .sarif files under .sarif folder will be loaded to error list automatically.";

        [Category(CategoryName)]
        [DisplayName(MonitorSarifFolderDisplayName)]
        [Description(MonitorSarifFolderDescription)]
        public bool MonitorSarifFolder { get; set; } = true;
    }
}
