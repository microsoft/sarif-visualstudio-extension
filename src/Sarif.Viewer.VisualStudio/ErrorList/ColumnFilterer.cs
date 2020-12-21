// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    // This class allows the caller to filter specified values from specified columns.
    // It keeps track of which column/value pairs have been filtered, so that it only
    // does so once per column/value pair. The idea is that if the user ever checks the
    // box in the filter UI to show a certain value in a certain column, we should never
    // hide that column again.
    internal class ColumnFilterer
    {
        // The set of column/value pairs that have been filtered so far.
        private readonly HashSet<FilteredColumnValue> filteredColumnValues = new HashSet<FilteredColumnValue>();

        private IWpfTableControl errorListTableControl;

        private IWpfTableControl ErrorListTableControl
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (this.errorListTableControl == null)
                {
                    var errorList = ServiceProvider.GlobalProvider.GetService(typeof(SVsErrorList)) as IErrorList;
                    if (errorList != null)
                    {
                        this.errorListTableControl = errorList.TableControl;
                    }
                }

                return this.errorListTableControl;
            }
        }

        public void FilterOut(string columnName, string filteredValue)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var filteredColumnValue = new FilteredColumnValue(columnName, filteredValue);
            if (!this.filteredColumnValues.Contains(filteredColumnValue))
            {
                ITableColumnDefinition categoryColumnDefinition =
                    this.ErrorListTableControl.ColumnDefinitionManager.GetColumnDefinition(columnName);

                this.ErrorListTableControl.SetFilter(
                    columnName,
                    new ColumnHashSetFilter(
                        categoryColumnDefinition,
                        filteredValue));

                this.filteredColumnValues.Add(filteredColumnValue);
            }
        }
    }
}
