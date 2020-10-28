// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// This class is used only to expose the list of Guids used by this package.
    /// This list of guids must match the set of Guids used inside the VSCT file.
    /// </summary>
    internal static class Guids
    {
        // Now define the list of guids as public static members.
        public static readonly Guid guidMenuAndCommandsCmdSet = new Guid("{CD8EE607-A630-4652-B2BA-748F534235C1}");
    }
}
