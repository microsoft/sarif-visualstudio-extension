/***************************************************************************
 
Copyright (c) Microsoft Corporation. All rights reserved.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;

namespace Microsoft.Samples.VisualStudio.MenuCommands
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Guid guidGenericCmdBmp = new Guid("{9749197A-F29F-4753-85CE-FD6B9C200223}");
    }
}
