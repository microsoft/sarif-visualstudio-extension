// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Define the IDs of the commands implemented by this package. The values must match the
    /// values defined in the Buttons section of the VSCT file.
    /// </summary>
    internal static class SariferPackageCommandIds
    {
        public const int GenerateTestData = 0x2010;
        public const int AnalyzeSolution = 0x2020;
        public const int AnalyzeProject = 0x2030;
        public const int AnalyzeFile = 0x2040;
    }
}
