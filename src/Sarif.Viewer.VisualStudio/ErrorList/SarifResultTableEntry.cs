namespace Microsoft.Sarif.Viewer.ErrorList
{
    using EnvDTE;
    using Microsoft.CodeAnalysis.Sarif;
    using Microsoft.Sarif.Viewer.Models;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Shell.TableControl;
    using Microsoft.VisualStudio.Shell.TableManager;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Documents;

    internal sealed class SarifResultTableEntry : ITableEntry
    {

        public SarifResultTableEntry(SarifErrorListItem error)
        {
            this.Identity = error.GetHashCode();
            this.Error = error;
        }

        public SarifErrorListItem Error { get; }

        public object Identity { get; }

        public bool CanSetValue(string keyName) => false;

        public bool TryGetValue(string keyName, out object content)
        {
            if (keyName == StandardTableKeyNames2.TextInlines)
            {
                string message = this.Error.Message;
                var inlines = SdkUIUtilities.GetMessageInlines(message, 0, ErrorListInlineLink_Click);

                if (inlines.Count > 0)
                {
                    content = inlines;
                    return true;
                }

                content = null;
                return false;
            }
            
            if (keyName == StandardTableKeyNames.DocumentName)
            {
                content = this.Error.FileName;
                return true;
            }

            if (keyName == StandardTableKeyNames.ErrorCategory)
            {
                content = this.Error.Category;
                return true;
            }

            if (keyName == StandardTableKeyNames.Line)
            {
                // The error list assumes the line number provided will be zero based and adds one before displaying the value.
                // i.e. if we pass 5, the error list will display 6. 
                // Subtract one from the line number so the error list displays the correct value.
                int lineNumber = this.Error.LineNumber - 1;
                content = lineNumber;
                return true;
            }

            if (keyName == StandardTableKeyNames.Column)
            {
                content = this.Error.ColumnNumber;
                return true;
            }

            if (keyName == StandardTableKeyNames.Text)
            {
                content = SdkUIUtilities.UnescapeBrackets(this.Error.ShortMessage);
                return true;
            }

            if (keyName == StandardTableKeyNames.FullText)
            {
                if (this.Error.HasDetailsContent)
                {
                    content = SdkUIUtilities.UnescapeBrackets(this.Error.Message);
                    return true;
                }

                content = null;
                return false;
            }

            if (keyName == StandardTableKeyNames.ErrorSeverity)
            {
                content = GetSeverity(this.Error.Level);
                return true;
            }

            if (keyName == StandardTableKeyNames.Priority)
            {
                content = GetSeverity(this.Error.Level) == __VSERRORCATEGORY.EC_ERROR
                    ? vsTaskPriority.vsTaskPriorityHigh
                    : vsTaskPriority.vsTaskPriorityMedium;
                return true;
            }

            if (keyName == StandardTableKeyNames.ErrorSource)
            {
                content = ErrorSource.Build;
                return true;
            }

            else if (keyName == StandardTableKeyNames.BuildTool)
            {
                content = this.Error.Tool.Name;
                return true;
            }

            if (keyName == StandardTableKeyNames.ErrorCode)
            {
                if (this.Error.Rule != null)
                {
                    content = this.Error.Rule.Id;
                    return true;
                }

                content = null;
                return false;
            }

            if (keyName == StandardTableKeyNames.ProjectName)
            {
                content = this.Error.ProjectName;
                return true;
            }

            if (keyName == StandardTableKeyNames.HelpLink)
            {
                string url = null;
                if (!string.IsNullOrEmpty(this.Error.HelpLink))
                {
                    url = this.Error.HelpLink;
                }

                if (url != null)
                {
                    content = Uri.EscapeUriString(url);
                    return true;
                }

                content = null;
                return false;
            }
            
            if (keyName == StandardTableKeyNames.ErrorCodeToolTip)
            {
                if (this.Error.Rule != null)
                {
                    content = this.Error.Rule.Id + ":" + this.Error.Rule.Name;
                    return true;
                }

                content = null;
                return false;
            }
            
            if (keyName == "suppressionstatus" ||
                     keyName == "suppressionstate" ||
                     keyName == "suppression")
            {
                content = this.Error.VSSuppressionState.ToString();
                return true;
            }

            content = null;
            return false;
        }

        public bool TrySetValue(string keyName, object content) => false;

        private __VSERRORCATEGORY GetSeverity(FailureLevel level)
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
            Hyperlink hyperLink = sender as Hyperlink;

            if (hyperLink != null)
            {
                Tuple<int, object> data = hyperLink.Tag as Tuple<int, object>;
                // data.Item1 = index of SarifErrorListItem
                // data.Item2 = id of related location to link, or absolute URL string

                SarifErrorListItem sarifResult = this.Error;

                if (data.Item2 is int id)
                {
                    // The user clicked an inline link with an integer target. Look for a Location object
                    // whose Id property matches that integer. The spec says that might be _any_ Location
                    // object under the current result. At present, we only support Location objects that
                    // occur in Result.Locations or Result.RelatedLocations. So, for example, we don't
                    // look in Result.CodeFlows or Result.Stacks.
                    LocationModel location = sarifResult.RelatedLocations.Where(l => l.Id == id).FirstOrDefault();
                    if (location == null)
                    {
                        location = sarifResult.Locations.Where(l => l.Id == id).FirstOrDefault();
                    }

                    if (location != null)
                    {
                        // Set the current sarif error in the manager so we track code locations.
                        CodeAnalysisResultManager.Instance.CurrentSarifResult = sarifResult;

                        SarifViewerPackage.SarifToolWindow.Control.DataContext = null;

                        if (sarifResult.HasDetails)
                        {
                            // Setting the DataContext to null (above) forces the TabControl to select the appropriate tab.
                            SarifViewerPackage.SarifToolWindow.Control.DataContext = sarifResult;
                        }

                        location.NavigateTo(false);
                        location.ApplyDefaultSourceFileHighlighting();
                    }
                }
                else if (data.Item2 is string)
                {
                    System.Diagnostics.Process.Start(data.Item2.ToString());
                }
            }
        }
    }
}
