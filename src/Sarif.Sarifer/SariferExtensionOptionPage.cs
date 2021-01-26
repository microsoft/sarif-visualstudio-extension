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
        private const string DisplayName = "Enable Background Analysis";
        private const string Description = "If enable background analyzer to analyze content in editor while editing.";


        [Category(CategoryName)]
        [DisplayName(DisplayName)]
        [Description(Description)]
        // default value: false
        public bool BackgroundAnalysisEnabled { get; set; } = false;
    }
}
