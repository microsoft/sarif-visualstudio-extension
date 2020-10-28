// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// This class is used only to expose the list of Guids used by this package.
    /// This list of guids must match the set of Guids used inside the VSCT file.
    /// </summary>
    internal static class GuidsList
    {
        // Now define the list of guids as public static members.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Guid guidMenuAndCommandsPkg = new Guid("{F70132AB-4095-477F-AAD2-81D3D581113B}");
        public const string guidMenuAndCommandsPkg_string = "F70132AB-4095-477F-AAD2-81D3D581113B";

        public static readonly Guid guidMenuAndCommandsCmdSet = new Guid("{CD8EE607-A630-4652-B2BA-748F534235C1}");
    }
}
