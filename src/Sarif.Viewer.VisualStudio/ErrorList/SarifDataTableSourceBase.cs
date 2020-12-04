// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ErrorList
{
    // Base class for the three "data table sources" used by the extension:
    //
    // 1. The "real" source, which adds entries to the error list.
    // 2. A "stub" data source whose sole purpose is to cause the Supression State
    //    column to be displayed.
    // 3. A "stub" data source whose sole purpose is to cause the Category column
    //    to be displayed.
    //
    // For an explanation of why these three sources are necessary, see the comment
    // near the top of SarifResultTableEntry.cs
    internal class SarifDataTableSourceBase
    {
    }
}
