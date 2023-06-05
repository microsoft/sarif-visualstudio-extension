// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    internal interface IColumnFilterer
    {
        void FilterOut(string columnName, string filteredValue);

        IEnumerable<string> GetFilteredValues(string columnName);
    }
}
