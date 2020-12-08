// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    // Associates an error list column with a value that has been filtered from it.
    internal struct FilteredColumnValue
    {
        public string ColumnName { get; }
        public string FilteredValue { get; }

        public FilteredColumnValue(string columnName, string filteredValue)
        {
            this.ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            this.FilteredValue = filteredValue ?? throw new ArgumentNullException(nameof(filteredValue));
        }

        public override bool Equals(object obj) =>
            obj is FilteredColumnValue other &&
            other.ColumnName.Equals(this.ColumnName, StringComparison.Ordinal) &&
            other.FilteredValue.Equals(this.FilteredValue, StringComparison.Ordinal);

        public override int GetHashCode()
        {
            int result = 17;
            unchecked
            {
                result = (result * 31) + this.ColumnName.GetHashCode();
                result = (result * 31) + this.FilteredValue.GetHashCode();
            }

            return result;
        }
    }
}
