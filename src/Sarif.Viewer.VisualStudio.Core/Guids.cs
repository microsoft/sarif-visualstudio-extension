// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// Defines GUIDs used by this package. The values must match the values used in the VSCT file.
    /// </summary>
    internal sealed partial class Guids
    {
        public const string GuidVSPackageString = "b97edb99-282e-444c-8f53-7de237f2ec5e";

        public static readonly Guid GuidVSPackage = new Guid(GuidVSPackageString);
    }
}
