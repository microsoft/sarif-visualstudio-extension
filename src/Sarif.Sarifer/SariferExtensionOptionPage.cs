// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    [Guid("BB3665D5-E661-48C0-801A-19B034F3CD5F")]
    [ComVisible(true)]
    public class SariferExtensionOptionPage : DialogPage
    {
        private const string CategoryName = "Sarifer Options";

        private const string BackgroundAnalysisEnabledDisplayName = "Enable background analysis automatically";
        private const string BackgroundAnalysisEnabledDescription = "If enabled, the background analysis would trigger if a file is opened or edited";

        private const string AnalyzeSarifFileDisplayName = "Enable analysis for .sarif files";
        private const string AnalyzeSarifFileDescription = "If enabled, .sarif files will be analyzed by default.";

        private const string MonitorSarifFolderDisplayName = "Enable loading sarif results in .sarif folder automatically";
        private const string MonitorSarifFolderDescription = "If enabled, .sarif files under .sarif folder will be loaded to error list automatically.";

        [Category(CategoryName)]
        [DisplayName(BackgroundAnalysisEnabledDisplayName)]
        [Description(BackgroundAnalysisEnabledDescription)]
        public bool BackgroundAnalysisEnabled { get; set; } = true;

        [Category(CategoryName)]
        [DisplayName(AnalyzeSarifFileDisplayName)]
        [Description(AnalyzeSarifFileDescription)]
        public bool AnalyzeSarifFile { get; set; }

        [Category(CategoryName)]
        [DisplayName(MonitorSarifFolderDisplayName)]
        [Description(MonitorSarifFolderDescription)]
        public bool MonitorSarifFolder { get; set; } = true;
    }
}
