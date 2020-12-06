// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

using EnvDTE;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    internal sealed class SarifResultTableEntry : ITableEntry, IDisposable
    {
        private bool isDisposed;

        private readonly Dictionary<string, object> columnKeyToContent = new Dictionary<string, object>(StringComparer.InvariantCulture);

        // The set of columns we display depends on the contents of the displayed log file.
        // Per https://github.com/microsoft/sarif-visualstudio-extension/issues/114, we do not
        // display "suppressed" or "absent" issues by default.
        //
        // If we open a log file that contains a suppressed issue, we display the "Suppression
        // state" column, setting its filter to NOT display the suppressed items. The appearance
        // of the column is a hint to the user that there are suppressed results available to examine.
        //
        // Similarly, if we open a log file that contains absent issues, we display the "Category"
        // column, setting its filter to NOT display the absent items. The appearance of the column
        // is a hint to the user that there are absent results available to examine.
        //
        // There is not a direct mechanism to add or remove columns based on an individual entry.
        // You can, however, add a new ITableDataSource (ITableManager.AddSource(source[, columns])
        // where you can specify the columns you want to appear in the table (at which point VS adds
        // the column to the table, though it won’t be shown to the user unless it is visible).
        // Removing the data source removes the column (although we don't have a requirement to do
        // that), unless some other source also added the same column.

        // The "basic" columns are always displayed.
        public static readonly ReadOnlyCollection<string> BasicColumns = new ReadOnlyCollection<string>(new[] {
            StandardTableKeyNames2.TextInlines,
            StandardTableKeyNames.DocumentName,
            StandardTableKeyNames.Line,
            StandardTableKeyNames.Column,
            StandardTableKeyNames.Text,
            StandardTableKeyNames.FullText,
            StandardTableKeyNames.ErrorSeverity,
            StandardTableKeyNames.Priority,
            StandardTableKeyNames.ErrorSource,
            StandardTableKeyNames.BuildTool,
            StandardTableKeyNames.ErrorCode,
            StandardTableKeyNames.ProjectName,
            StandardTableKeyNames.HelpLink,
            StandardTableKeyNames.ErrorCodeToolTip
        });

        public static readonly ReadOnlyCollection<string> SuppressedResultColumns = new ReadOnlyCollection<string>(new[]
        {
            "suppressionstatus",
            "suppressionstate",
            "suppression"
        });

        public static readonly ReadOnlyCollection<string> AbsentResultColumns = new ReadOnlyCollection<string>(new[]
        {
            StandardTableKeyNames.ErrorCategory
        });


        public SarifResultTableEntry(SarifErrorListItem error)
        {
            this.Identity = error.GetHashCode();
            this.Error = error;

            // Set the data that's fast to retrieve into the dictionary of values.
            this.columnKeyToContent[StandardTableKeyNames.DocumentName] = this.Error.FileName;
            this.columnKeyToContent[StandardTableKeyNames.ErrorCategory] = this.Error.Category;

            // The error list assumes the line number provided will be zero based and adds one before displaying the value.
            // i.e. if we pass 5, the error list will display 6. 
            // Subtract one from the line number so the error list displays the correct value.
            this.columnKeyToContent[StandardTableKeyNames.Line] = this.Error.LineNumber - 1;

            this.columnKeyToContent[StandardTableKeyNames.Column] = this.Error.ColumnNumber;
            this.columnKeyToContent[StandardTableKeyNames.ErrorSeverity] = GetSeverity(this.Error.Level);
            this.columnKeyToContent[StandardTableKeyNames.Priority] = GetSeverity(this.Error.Level) == __VSERRORCATEGORY.EC_ERROR
                    ? vsTaskPriority.vsTaskPriorityHigh
                    : vsTaskPriority.vsTaskPriorityMedium;
            this.columnKeyToContent[StandardTableKeyNames.ErrorSource] = ErrorSource.Build;
            this.columnKeyToContent[StandardTableKeyNames.BuildTool] = this.Error.Tool?.Name;

            if (this.Error.Rule != null)
            {
                this.columnKeyToContent[StandardTableKeyNames.ErrorCode] = this.Error.Rule.Id;
                this.columnKeyToContent[StandardTableKeyNames.ErrorCodeToolTip] = string.IsNullOrEmpty(this.Error.Rule.Name) ?
                    this.Error.Rule.Id : this.Error.Rule.Id + "." + this.Error.Rule.Name;
            }

            this.columnKeyToContent[StandardTableKeyNames.ProjectName] = this.Error.ProjectName;

            string supressionState = this.Error.VSSuppressionState.ToString();
            this.columnKeyToContent["suppressionstatus"] = supressionState;
            this.columnKeyToContent["suppressionstate"] = supressionState;
            this.columnKeyToContent["suppression"] = supressionState;

            // Anything that's a bit more complex, we will make a "lazy" value and evaluate
            // it when it's asked for.
            this.columnKeyToContent[StandardTableKeyNames2.TextInlines] = new Lazy<object>(() =>
            {
                string message = this.Error.Message;
                List<Inline> inlines = SdkUIUtilities.GetMessageInlines(message, this.ErrorListInlineLink_Click);

                if (inlines.Count > 0)
                {
                    return inlines;
                }

                return null;
            });

            this.columnKeyToContent[StandardTableKeyNames.Text] = new Lazy<object>(() =>
            {
                return SdkUIUtilities.UnescapeBrackets(this.Error.ShortMessage);
            });

            this.columnKeyToContent[StandardTableKeyNames.FullText] = new Lazy<object>(() =>
            {
                if (this.Error.HasDetailsContent)
                {
                    return SdkUIUtilities.UnescapeBrackets(this.Error.Message);
                }

                return null;
            });

            this.columnKeyToContent[StandardTableKeyNames.HelpLink] = new Lazy<object>(() =>
            {
                return !string.IsNullOrEmpty(this.Error.HelpLink) ? Uri.EscapeUriString(this.Error.HelpLink) : null;
            });
        }

        public SarifErrorListItem Error { get; }

        public object Identity { get; }

        public bool CanSetValue(string keyName) => false;

        public bool TryGetValue(string keyName, out object content)
        {
            if (this.columnKeyToContent.TryGetValue(keyName, out content))
            {
                if (content is Lazy<object> lazyContent)
                {
                    content = lazyContent.Value;
                }

                return true;
            }

            content = null;
            return false;
        }

        public bool TrySetValue(string keyName, object content) => false;

        private static __VSERRORCATEGORY GetSeverity(FailureLevel level)
        {
            switch (level)
            {
                case FailureLevel.Error:
                {
                    return __VSERRORCATEGORY.EC_ERROR;
                }
                case FailureLevel.Warning:
                {
                    return __VSERRORCATEGORY.EC_WARNING;
                }
                case FailureLevel.None:
                case FailureLevel.Note:
                {
                    return __VSERRORCATEGORY.EC_MESSAGE;
                }
            }
            return __VSERRORCATEGORY.EC_WARNING;
        }

        private void ErrorListInlineLink_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(sender is Hyperlink hyperLink))
            {
                return;
            }

            if (hyperLink.Tag is int id)
            {
                // The user clicked an in-line link with an integer target. Look for a Location object
                // whose Id property matches that integer. The spec says that might be _any_ Location
                // object under the current result. At present, we only support Location objects that
                // occur in Result.Locations or Result.RelatedLocations. So, for example, we don't
                // look in Result.CodeFlows or Result.Stacks.
                LocationModel location =
                    this.Error.RelatedLocations.
                    Concat(this.Error.Locations).
                    FirstOrDefault(l => l.Id == id);

                if (location == null)
                {
                    return;
                }

                // If a location is found, then we will show this error in the explorer window
                // by setting the navigated item to the error related to this error entry,
                // but... we will navigate the editor to the found location, which for example
                // may be a related location.
                if (this.Error.HasDetails)
                {
                    IComponentModel componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                    if (componentModel != null)
                    {
                        ISarifErrorListEventSelectionService sarifSelectionService = componentModel.GetService<ISarifErrorListEventSelectionService>();
                        if (sarifSelectionService != null)
                        {
                            sarifSelectionService.NavigatedItem = this.Error;
                        }
                    }
                }

                location.NavigateTo(usePreviewPane: false, moveFocusToCaretLocation: true);
            }
            // This is super dangerous! We are launching URIs for SARIF logs
            // that can point to anything.
            // https://github.com/microsoft/sarif-visualstudio-extension/issues/171
            else if (hyperLink.Tag is string uriAsString)
            {
                System.Diagnostics.Process.Start(uriAsString);
            }
            else if (hyperLink.Tag is Uri uri)
            {
                System.Diagnostics.Process.Start(uri.ToString());
            }
        }

        private void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            if (disposing)
            {
                this.Error.Dispose();
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
