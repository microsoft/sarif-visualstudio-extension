// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    [Guid("BB3665D5-E661-48C0-801A-19B034F3CD5F")]
    public class SariferExtensionOptionPage : DialogPage
    {
        private const string CategoryName = "Sarifer Options";

        private const string DisplayName_BackgroundAnalysisEnabled = "Enable background analysis automatically";
        private const string Description_BackgroundAnalysisEnabled = "If enabled, the background analysis would trigger if a file is opened or edited";

        private const string DisplayName_AnalyzeSarifFile = "Enable analysis for .sarif files";
        private const string Description_AnalyzeSarifFile = "If enabled, .sarif files will be analyzed by default.";

        [Category(CategoryName)]
        [DisplayName(DisplayName_BackgroundAnalysisEnabled)]
        [Description(Description_BackgroundAnalysisEnabled)]
        // default value: true
        public bool BackgroundAnalysisEnabled { get; set; } = true;

        [Category(CategoryName)]
        [DisplayName(DisplayName_AnalyzeSarifFile)]
        [Description(Description_AnalyzeSarifFile)]
        // default value: false
        public bool AnalyzeSarifFile { get; set; } = false;
    }
}
