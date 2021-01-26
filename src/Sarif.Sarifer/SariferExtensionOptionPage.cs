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
        private const string DisplayName_BackgroundAnalysisEnabled = "Enable Background Analysis";
        private const string Description_BackgroundAnalysisEnabled = "If enable background analyzer to analyze content in editor while editing.";
        private const string DisplayName_AnalyzeSarifFile = "If Analyze .sarif File";
        private const string Description_AnalyzeSarifFile = "If Sarifer analyzes .sarif files";


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
