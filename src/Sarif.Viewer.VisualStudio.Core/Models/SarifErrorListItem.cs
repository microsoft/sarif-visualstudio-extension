// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using CSharpFunctionalExtensions;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Options;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Newtonsoft.Json;
using Run = Microsoft.CodeAnalysis.Sarif.Run;
using XamlDoc = System.Windows.Documents;

namespace Microsoft.Sarif.Viewer
{
    internal enum TextRenderType
    {
        /// <summary>
        /// Denotes that a string should be rendered as plaintext
        /// </summary>
        Text,

        /// <summary>
        /// Denotes that a string should be rendered as markdown
        /// </summary>
        Markdown,
    }

    /// <summary>
    /// This holds the functionality of the <see cref="SarifErrorListItem"/> class.
    /// </summary>
    internal partial class SarifErrorListItem : NotifyPropertyChangedObject, IDisposable
    {
        internal SarifErrorListItem(CodeAnalysis.Sarif.Result result)
            : this()
        {
            this.SarifResult = result;
            this.RawMessage = result.Message?.Text;
        }

        internal SarifErrorListItem()
        {
            this.Locations = new LocationCollection(string.Empty);
            this.RelatedLocations = new LocationCollection(string.Empty);
            this.AnalysisSteps = new AnalysisStepCollection();
            this.Stacks = new ObservableCollection<StackCollection>();
            this.Fixes = new ObservableCollection<FixModel>();
            this.Properties = new ObservableCollection<KeyValuePair<string, string>>();
        }

        public SarifErrorListItem(Run run, int runIndex, CodeAnalysis.Sarif.Result result, string logFilePath, ProjectNameCache projectNameCache)
            : this()
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            bool runHasSuppressions = run.HasSuppressedResults();
            bool runHasAbsentResults = run.HasAbsentResults();

            this.RunIndex = runIndex;
            this.ResultId = Interlocked.Increment(ref currentResultId);
            this.ResultGuid = result.Guid;
            this.SarifResult = result;
            ReportingDescriptor rule = result.GetRule(run);
            this.Tool = run.Tool.ToToolModel();
            this.Rule = rule.ToRuleModel(result.RuleId);
            this.Invocation = run.Invocations?[0]?.ToInvocationModel();
            this.WorkingDirectory = Path.Combine(Path.GetTempPath(), this.RunIndex.ToString());
            this.HelpLink = this.Rule?.HelpUri;

            this.RawMessage = result.GetMessageText(rule, concise: false).Trim();
            (this.ShortMessage, this.Message) = SdkUIUtilities.SplitResultMessage(this.RawMessage, MaxConcisedTextLength);

            this.FileName = result.GetPrimaryTargetFile(run);
            this.ProjectName = projectNameCache.GetName(this.FileName);
            this.Category = runHasAbsentResults ? result.GetCategory() : nameof(BaselineState.None);
            this.Region = result.GetPrimaryTargetRegion();
            this.Level = this.GetEffectiveLevel(result);

            this.VSSuppressionState = runHasSuppressions && result.IsSuppressed() ? VSSuppressionState.Suppressed : VSSuppressionState.Active;

            this.LogFilePath = logFilePath;

            if (this.Region != null)
            {
                this.LineNumber = this.Region.StartLine;
                this.ColumnNumber = this.Region.StartColumn;
            }
        }

/*        /// <summary>
        ///  Gets the queries that can be used to do codefinding for this error list item.
        /// </summary>
        /// <returns>A list of queries for each location. If a location does not require a query, it will be inserted as null. If this item does not require a query for any, this list will be null or empty.</returns>
        public List<(Uri filePath, CodeFinder.MatchQuery query)?> GetMatchQueries()
        {
            List<(Uri filePath, CodeFinder.MatchQuery query)?> queries = new List<(Uri filePath, MatchQuery query)?>();

            // If the physical location has a start line and end line tag, we should try to do codefinder searching to find the line to highlight even in cases of code drift
            if (this.SarifResult.Locations?[0].PhysicalLocation != null
                && this.SarifResult.Locations[0].PhysicalLocation.PropertyNames.Contains("StartLine")
                && this.SarifResult.Locations[0].PhysicalLocation.PropertyNames.Contains("EndLine"))
            {
                foreach (Location l in this.SarifResult.Locations)
                {
                    if (l != null)
                    {
                        PhysicalLocation currentPhysicalLocation = l.PhysicalLocation;
                        LogicalLocation currentLogicalLocation = l.LogicalLocation;
                        if (currentPhysicalLocation.PropertyNames.Contains("StartLine") && currentPhysicalLocation.PropertyNames.Contains("EndLine")
                            && currentPhysicalLocation.Region?.Snippet?.Text != null && currentPhysicalLocation.ArtifactLocation?.Uri != null && this.SarifResult.Guid != null)
                        {
                            MatchQuery.MatchTypeHint typeHint = MatchQuery.MatchTypeHint.Code;
                            if (currentPhysicalLocation.Region.Snippet.Text == currentLogicalLocation.FullyQualifiedName)
                            {
                                typeHint = MatchQuery.MatchTypeHint.Function;
                            }

                            MatchQuery query = new MatchQuery(textToFind: currentPhysicalLocation.Region.Snippet.Text,
                                lineNumberHint: this.LineNumber,
                                callingSignature: currentLogicalLocation?.FullyQualifiedName,
                                id: this.SarifResult.Guid,
                                typeHint: typeHint);
                            queries.Add((currentPhysicalLocation.ArtifactLocation?.Uri, query));
                        }
                    }
                    else
                    {
                        queries.Add(null);
                    }
                }
            }

            return queries;
        }*/

        public SarifErrorListItem(Run run, int runIndex, Notification notification, string logFilePath, ProjectNameCache projectNameCache)
            : this()
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            this.RunIndex = runIndex;
            string ruleId = null;

            if (notification.AssociatedRule != null)
            {
                ruleId = notification.AssociatedRule.Id;
            }
            else if (notification.Descriptor != null)
            {
                ruleId = notification.Descriptor.Id;
            }

            run.TryGetRule(ruleId, out ReportingDescriptor rule);
            this.RawMessage = FormatNotficationText(notification);
            (this.ShortMessage, this.Message) = SdkUIUtilities.SplitResultMessage(this.RawMessage, MaxConcisedTextLength);

            this.Level = notification.Level;
            this.LogFilePath = logFilePath;
            this.FileName = SdkUIUtilities.GetFileLocationPath(notification.Locations?[0]?.PhysicalLocation?.ArtifactLocation, this.RunIndex) ?? string.Empty;
            this.ProjectName = projectNameCache.GetName(this.FileName);
            this.Locations.Add(new LocationModel(resultId: this.ResultId, runIndex: this.RunIndex) { FilePath = FileName });

            this.Tool = run.Tool.ToToolModel();
            this.Rule = rule.ToRuleModel(ruleId);
            this.Invocation = run.Invocations?[0]?.ToInvocationModel();
            this.WorkingDirectory = Path.Combine(Path.GetTempPath(), this.RunIndex.ToString());
            this.HelpLink = this.Rule?.HelpUri;
        }

        /// <summary>
        /// Fired when this error list item is disposed.
        /// </summary>
        /// <remarks>
        /// An example of the usage of this is making sure that the SARIF explorer window
        /// doesn't hold on to a disposed object when the error list is cleared.
        /// </remarks>
        public event EventHandler Disposed;

        private FailureLevel GetEffectiveLevel(CodeAnalysis.Sarif.Result result)
        {
            switch (result.Kind)
            {
                case ResultKind.Review:
                case ResultKind.Open:
                    return FailureLevel.Warning;

                case ResultKind.NotApplicable:
                case ResultKind.Informational:
                case ResultKind.Pass:
                    return FailureLevel.Note;

                case ResultKind.Fail:
                case ResultKind.None: // Should never happen.
                default: // Should never happen.
                    return result.Level != FailureLevel.Warning ? result.Level : this.Rule.FailureLevel;
            }
        }

        [Browsable(false)]
        public DelegateCommand OpenLogFileCommand => this._openLogFileCommand ??= new DelegateCommand(() =>
        {
            // For now this is being done on the UI thread
            // and is only required due to the message box being shown below.
            // This will be addressed when https://github.com/microsoft/sarif-visualstudio-extension/issues/160
            // is fixed.
            ThreadHelper.ThrowIfNotOnUIThread();

            this.OpenLogFile();
        });

        internal void OpenLogFile()
        {
            // For now this is being done on the UI thread
            // and is only required due to the message box being shown below.
            // This will be addressed when https://github.com/microsoft/sarif-visualstudio-extension/issues/160
            // is fixed.
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.LogFilePath != null)
            {
                if (File.Exists(this.LogFilePath))
                {
                    var dte = AsyncPackage.GetGlobalService(typeof(DTE)) as DTE2;
                    dte.ExecuteCommand("File.OpenFile", $@"""{this.LogFilePath}"" /e:""JSON Editor""");
                }
                else if (CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.TryGetValue(this.RunIndex, out RunDataCache cache))
                {
                    // LogFilePath doesn't exist then its a background analyzer result in memeory
                    // load sarif log from memory cache
                    string sarifLogFilePath = Path.Combine(CodeAnalysisResultManager.Instance.TempDirectoryPath, $"{cache.SarifLog.GetHashCode()}");
                    if (!Directory.Exists(CodeAnalysisResultManager.Instance.TempDirectoryPath))
                    {
                        Directory.CreateDirectory(CodeAnalysisResultManager.Instance.TempDirectoryPath);
                    }

                    if (!File.Exists(sarifLogFilePath))
                    {
                        // serialize memory cached SarifLog into a temp file
                        var serializer = new JsonSerializer
                        {
                            Formatting = Formatting.Indented,
                        };
                        using var sw = new StreamWriter(sarifLogFilePath);
                        using JsonWriter writer = new JsonTextWriter(sw);
                        serializer.Serialize(writer, cache.SarifLog);
                    }

                    // open temp sarif log file
                    SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, sarifLogFilePath, usePreviewPane: false);
                }
                else
                {
                    VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
                                                    string.Format(Resources.OpenLogFileFail_DialogMessage, this.LogFilePath),
                                                    null, // title
                                                    OLEMSGICON.OLEMSGICON_CRITICAL,
                                                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            }
        }

        public override string ToString()
        {
            return this.Message;
        }

        /// <summary>
        /// Gets or sets the line marker for highlighting that represents the primary region of this item.
        /// </summary>
        [Browsable(false)]
        public ResultTextMarker LineMarker
        {
            get
            {
                if (this._lineMarker == null && this.Region?.StartLine > 0)
                {
                    this._lineMarker = new ResultTextMarker(
                        runIndex: this.RunIndex,
                        resultId: this.ResultId,
                        uriBaseId: this.Locations?.FirstOrDefault()?.UriBaseId,
                        region: this.Region,
                        fullFilePath: this.FileName,
                        nonHighlightedColor: ResultTextMarker.DEFAULT_SELECTION_COLOR,
                        highlightedColor: ResultTextMarker.HOVER_SELECTION_COLOR,
                        failureLevel: this.Level,
                        tooltipContent: this.Content,
                        context: this);
                }

                return this._lineMarker;
            }

            set => this._lineMarker = value;
        }

        /// <summary>
        /// Remaps the file paths of this object's File name, locations, analysis steps, stack, and fixes.
        /// </summary>
        /// <param name="originalPath">The original path.</param>
        /// <param name="remappedPath">The path it got re-mapped to.</param>
        internal void RemapFilePath(string originalPath, string remappedPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var uri = new Uri(remappedPath, UriKind.Absolute);
            FileRegionsCache regionsCache = CodeAnalysisResultManager.Instance.RunIndexToRunDataCache[this.RunIndex].FileRegionsCache;

            if (this.FileName.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
            {
                this.FileName = remappedPath;
            }

            foreach (LocationModel location in this.Locations)
            {
                if (location.FilePath.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
                {
                    location.FilePath = remappedPath;

                    if (location.Region != null)
                    {
                        location.Region = regionsCache.PopulateTextRegionProperties(location.Region, uri, true);

                        if (this.LineNumber != location.Region.StartLine)
                        {
                            this.LineNumber = location.Region.StartLine;
                        }

                        if (this.ColumnNumber != location.Region.StartColumn)
                        {
                            this.ColumnNumber = location.Region.StartColumn;
                        }
                    }
                }
            }

            foreach (LocationModel location in this.RelatedLocations)
            {
                if (location.FilePath.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
                {
                    location.FilePath = remappedPath;
                    location.Region = regionsCache.PopulateTextRegionProperties(location.Region, uri, true);
                }
            }

            foreach (AnalysisStep analysisStep in this.AnalysisSteps)
            {
                var nodesToProcess = new Stack<AnalysisStepNode>();

                foreach (AnalysisStepNode topLevelNode in analysisStep.TopLevelNodes)
                {
                    nodesToProcess.Push(topLevelNode);
                }

                while (nodesToProcess.Count > 0)
                {
                    AnalysisStepNode current = nodesToProcess.Pop();
                    try
                    {
                        if (current.FilePath?.Equals(originalPath, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            current.FilePath = remappedPath;
                            current.Region = regionsCache.PopulateTextRegionProperties(current.Region, uri, true);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // An argument exception is thrown if the node does not have a region.
                        // Since there's no region, there's no document to attach to.
                        // Just move on with processing the child nodes.
                    }

                    foreach (AnalysisStepNode childNode in current.Children)
                    {
                        nodesToProcess.Push(childNode);
                    }
                }
            }

            foreach (StackCollection stackCollection in this.Stacks)
            {
                foreach (StackFrameModel stackFrame in stackCollection)
                {
                    if (stackFrame.FilePath.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
                    {
                        stackFrame.FilePath = remappedPath;
                        stackFrame.Region = regionsCache.PopulateTextRegionProperties(stackFrame.Region, uri, true);
                    }
                }
            }

            foreach (FixModel fixModel in this.Fixes)
            {
                foreach (ArtifactChangeModel fileChangeModel in fixModel.ArtifactChanges)
                {
                    if (fileChangeModel.FilePath.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
                    {
                        fileChangeModel.FilePath = remappedPath;
                    }
                }
            }

            foreach (Location location in this.SarifResult.Locations)
            {
                if (location?.PhysicalLocation?.ArtifactLocation?.Uri.ToString() == originalPath)
                {
                    location.PhysicalLocation.ArtifactLocation.Uri = new Uri(remappedPath);
                }
            }

            // After the file-paths have been remapped, we need to refresh the tags
            // as it may now be possible to create the persistent spans (since the file paths are now potentially valid)
            // or their file paths may have moved from one valid location to a different valid location.
            SarifLocationTagHelpers.RefreshTags();
        }

        /// <summary>
        /// Populates the <see cref="Fixes"/> field from the <see cref="SarifResult.Fixes"/>.
        /// </summary>
        internal void PopulateFixModelsIfNot()
        {
            // Populate FixModels if they are not populated
            if (this.SarifResult?.Fixes?.Any() == true && this.Fixes?.Any() == false)
            {
                if (CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.TryGetValue(this.RunIndex, out RunDataCache runDataCache))
                {
                    foreach (Fix fix in this.SarifResult.Fixes)
                    {
                        var fixModel = fix.ToFixModel(runDataCache.OriginalUriBasePaths, FileRegionsCache.Instance);
                        foreach (ArtifactChangeModel fileChangeModel in fixModel.ArtifactChanges)
                        {
                            fileChangeModel.FilePath = this.FileName;
                        }

                        this.Fixes.Add(fixModel);
                    }
                }
                else
                {
                    foreach (Fix fix in this.SarifResult.Fixes)
                    {
                        this.Fixes.Add(fix.ToFixModel(this.SarifResult.Run.OriginalUriBaseIds, FileRegionsCache.Instance));
                    }
                }
            }
        }

        /// <summary>
        /// Populates the <see cref="Locations"/> , <see cref="RelatedLocations"/>, <see cref="CodeFlows"/>, <see cref="Stacks"/>, and <see cref="Properties"/> fields from <see cref="SarifResult"/>.
        /// Only callable from the UI thread.
        /// </summary>
        internal void PopulateAdditionalPropertiesIfNot()
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            if (this.SarifResult.Locations?.Any() == true && this.Locations?.Any() == false)
            {
                // Adding in reverse order will make them display in the correct order in the UI.
                for (int i = this.SarifResult.Locations.Count - 1; i >= 0; --i)
                {
                    this.Locations.Add(this.SarifResult.Locations[i].ToLocationModel(this.SarifResult.Run, resultId: this.ResultId, runIndex: this.RunIndex));
                }
            }

            if (this.SarifResult.RelatedLocations?.Any() == true && this.RelatedLocations?.Any() == false)
            {
                this.BuildRelatedLocationsTree();
            }

            if (this.SarifResult.Stacks?.Any() == true && this.Stacks?.Any() == false)
            {
                foreach (Stack stack in this.SarifResult.Stacks)
                {
                    this.Stacks.Add(stack.ToStackCollection(resultId: this.ResultId, runIndex: this.RunIndex));
                }
            }

            if (this.SarifResult.CodeFlows?.Any() == true && this.AnalysisSteps?.Any() == false)
            {
                foreach (CodeFlow codeFlow in this.SarifResult.CodeFlows)
                {
                    var analysisStep = codeFlow.ToAnalysisStep(this.SarifResult.Run, sarifErrorListItem: this, runIndex: this.RunIndex);
                    if (analysisStep != null)
                    {
                        this.AnalysisSteps.Add(analysisStep);
                    }
                }

                this.AnalysisSteps.Verbosity = 100;
                this.AnalysisSteps.IntelligentExpand();
            }

            if (this.SarifResult.PropertyNames?.Any() == true && this.Properties?.Any() == false)
            {
                foreach (string propertyName in this.SarifResult.PropertyNames)
                {
                    this.Properties.Add(
                        new KeyValuePair<string, string>(
                            propertyName,
                            this.SarifResult.GetSerializedPropertyValue(propertyName)));
                }
            }
        }

        internal void BuildRelatedLocationsTree()
        {
            LocationModel lastNode = null;
            int lastLevel = -1;

            foreach (Location location in this.SarifResult.RelatedLocations)
            {
                var locationModel = location.ToLocationModel(this.SarifResult.Run, resultId: this.ResultId, runIndex: this.RunIndex);
                int levelChange = locationModel.NestingLevel - lastLevel;

                while (levelChange++ <= 0)
                {
                    lastNode = lastNode?.Parent;
                }

                if (locationModel.NestingLevel > 0)
                {
                    locationModel.Parent = lastNode;
                    lastNode?.Children.Add(locationModel);
                }
                else
                {
                    this.RelatedLocations.Add(locationModel);
                }

                lastLevel = locationModel.NestingLevel;
                lastNode = locationModel;
            }
        }

        /// <summary>
        /// Returns a value indicating whether this error is fixable.
        /// </summary>
        /// <remarks>
        /// An error is fixable if it provides at list one fix with enough information to be
        /// applied, and it is not already fixed.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the error is fixable; otherwise <c>false</c>.
        /// </returns>
        public bool IsFixable() =>
            !this.IsFixed && this.Fixes.Any(fix => fix.CanBeAppliedToFile(this.FileName));

        /// <summary>
        /// Gets or sets a value indicating whether this error has been fixed.
        /// </summary>
        public bool IsFixed { get; set; }

        /// <summary>
        /// Builds the tags that are to be used in code highglighting and popups.
        /// </summary>
        /// <typeparam name="T">Type that the result text marker should use.</typeparam>
        /// <param name="textBuffer">The textbuffer representing the file the tags are being built for.</param>
        /// <param name="persistentSpanFactory">The span factory that is to actually create the spans.</param>
        /// <param name="includeChildTags">True will set it to include child tags in the location tags returned.</param>
        /// <param name="includeResultTag">True will set it to include result tags in the location tags returned.</param>
        /// <returns>The list of <see cref="ISarifLocationTag"/> that are used to display highlights and popups in the VS editor window.</returns>
        public IEnumerable<ISarifLocationTag> GetTags<T>(ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory, bool includeChildTags, bool includeResultTag)
            where T : ITag
        {
            IEnumerable<ResultTextMarker> resultTextMarkers = this.CollectResultTextMarkers(includeChildTags: includeChildTags, includeResultTag: includeResultTag);

            return resultTextMarkers.SelectMany(resultTextMarker => resultTextMarker.GetTags<T>(textBuffer, persistentSpanFactory));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            if (disposing)
            {
                IEnumerable<ResultTextMarker> resultTextMarkers = this.CollectResultTextMarkers(includeChildTags: true, includeResultTag: true);
                foreach (ResultTextMarker resultTextMarker in resultTextMarkers)
                {
                    resultTextMarker.Dispose();
                }
            }

            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private IEnumerable<ResultTextMarker> CollectResultTextMarkers(bool includeChildTags, bool includeResultTag)
        {
            IEnumerable<ResultTextMarker> resultTextMarkers = Enumerable.Empty<ResultTextMarker>();

            // The "line marker" springs into existence when it is asked for.
            if (includeResultTag)
            {
                resultTextMarkers = resultTextMarkers.Concat(Enumerable.Repeat(this.LineMarker, 1));
            }

            if (includeChildTags)
            {
                resultTextMarkers = resultTextMarkers.Concat(this.Locations.Select(location => location.LineMarker));
                resultTextMarkers = resultTextMarkers.Concat(this.RelatedLocations.Select(relatedLocation => relatedLocation.LineMarker));

                foreach (AnalysisStep analysisStep in this.AnalysisSteps)
                {
                    var nodesToProcess = new Stack<AnalysisStepNode>();
                    var allAnalysisStepNodes = new List<AnalysisStepNode>();

                    foreach (AnalysisStepNode topLevelNode in analysisStep.TopLevelNodes)
                    {
                        nodesToProcess.Push(topLevelNode);
                    }

                    while (nodesToProcess.Count > 0)
                    {
                        AnalysisStepNode current = nodesToProcess.Pop();

                        allAnalysisStepNodes.Add(current);

                        foreach (AnalysisStepNode childNode in current.Children)
                        {
                            nodesToProcess.Push(childNode);
                        }
                    }

                    resultTextMarkers = resultTextMarkers.Concat(allAnalysisStepNodes.Select(analysisStepNode => analysisStepNode.LineMarker));
                }

                foreach (StackCollection stackCollection in this.Stacks)
                {
                    resultTextMarkers = resultTextMarkers.Concat(stackCollection.Select(stack => stack.LineMarker));
                }
            }

            // Some of the data models will return null markers if there aren't valid properties like
            // "region" being set. So let's filter out the nulls from the callers.
            return resultTextMarkers.Where(resultTextMarker => resultTextMarker != null);
        }

        internal IEnumerable<string> GetCodeSnippets()
        {
            IDictionary<int, RunDataCache> runIndexToRunDataCache = CodeAnalysisResultManager.Instance.RunIndexToRunDataCache;
            if (!runIndexToRunDataCache.TryGetValue(this.RunIndex, out RunDataCache runDataCache))
            {
                runDataCache = null;
            }

            FileRegionsCache regionsCache = runDataCache?.FileRegionsCache;
            return this.Locations.Select(location => location.ExtractSnippet(regionsCache));
        }

        internal (string first, string second) SplitMessageText(string fullText, int maxLength = 165)
        {
            // remove line breakers
            fullText = fullText.Replace(Environment.NewLine, " ").Replace("\r", " ").Replace("\n", " ");

            string text = fullText;
            string restText = fullText;

            char[] endChars = new char[] { '\r', '\n', ' ', };
            if (text.Length > maxLength)
            {
                int endPosition = maxLength;

                // if need to split text longer than maxLength, make sure not split in middle of a word.
                if (!endChars.Contains(text[maxLength]))
                {
                    // find nearest whitespace before max length to separate the string
                    endPosition = text.LastIndexOfAny(endChars, maxLength);

                    // if not found, set end position to max length
                    endPosition = (endPosition == -1) ? maxLength : endPosition;
                }

                text = text.Substring(0, endPosition) + " \u2026"; // u2026 is Unicode "horizontal ellipsis";
                restText = restText.Substring(endPosition).TrimStart(endChars);
            }

            return (text.TrimEnd(endChars), restText.TrimEnd(endChars));
        }

        internal void MessageInlineLink_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(sender is XamlDoc.Hyperlink hyperLink))
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
                LocationModel location = this.RelatedLocations.Concat(this.Locations)
                                                              .FirstOrDefault(l => l.Id == id);

                if (location == null)
                {
                    return;
                }

                // If a location is found, then we will show this error in the explorer window
                // by setting the navigated item to the error related to this error entry,
                // but... we will navigate the editor to the found location, which for example
                // may be a related location.
                if (this.HasDetails)
                {
                    var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                    if (componentModel != null)
                    {
                        ISarifErrorListEventSelectionService sarifSelectionService = componentModel.GetService<ISarifErrorListEventSelectionService>();
                        if (sarifSelectionService != null)
                        {
                            sarifSelectionService.NavigatedItem = this;
                        }
                    }
                }

                location.NavigateTo(usePreviewPane: false, moveFocusToCaretLocation: true);
            }

            // This is super dangerous! We are launching URIs for SARIF logs
            // that can point to anything.
            // https://github.com/microsoft/sarif-visualstudio-extension/issues/171
            else
            {
                string uriString = null;
                if (hyperLink.Tag is string uriAsString)
                {
                    uriString = uriAsString;
                }
                else if (hyperLink.Tag is Uri uri)
                {
                    uriString = uri.ToString();
                }

                SdkUIUtilities.OpenExternalUrl(uriString);
            }
        }

        /// <summary>
        /// Generates an unique hash value using the properties affect content of error list item.
        /// If any of these properties changes, needs to refresh error list item.
        /// </summary>
        /// <returns>The hash of the error list item object.</returns>
        internal int GetIdentity()
        {
            int hashCode = -509415362;
            int hashFactor = -1521134295;

            // ignore FileName because it usally updated to a physical file path during file resolving
            // but error list only shows file name not the full path
            // hashCode = (hashCode * hashFactor) + EqualityComparer<string>.Default.GetIdentity(this.FileName);

            hashCode = (hashCode * hashFactor) + EqualityComparer<string>.Default.GetHashCode(this.Category);
            hashCode = (hashCode * hashFactor) + this.LineNumber.GetHashCode();
            hashCode = (hashCode * hashFactor) + this.ColumnNumber.GetHashCode();
            hashCode = (hashCode * hashFactor) + this.Level.GetHashCode();
            hashCode = (hashCode * hashFactor) + EqualityComparer<string>.Default.GetHashCode(this.ProjectName);
            hashCode = (hashCode * hashFactor) + this.VSSuppressionState.GetHashCode();
            hashCode = (hashCode * hashFactor) + EqualityComparer<string>.Default.GetHashCode(this.Message);
            hashCode = (hashCode * hashFactor) + EqualityComparer<string>.Default.GetHashCode(this.RawMessage);
            hashCode = (hashCode * hashFactor) + EqualityComparer<string>.Default.GetHashCode(this.ShortMessage);
            hashCode = (hashCode * hashFactor) + this.HasDetailsContent.GetHashCode();
            hashCode = (hashCode * hashFactor) + EqualityComparer<string>.Default.GetHashCode(this.HelpLink);
            hashCode = (hashCode * hashFactor) + EqualityComparer<ToolModel>.Default.GetHashCode(this.Tool);
            hashCode = (hashCode * hashFactor) + EqualityComparer<RuleModel>.Default.GetHashCode(this.Rule);

            return hashCode;
        }

        private static string FormatNotficationText(Notification notification)
        {
            string message = notification.Message.Text?.Trim() ?? string.Empty;

            string kind = notification.Exception?.Kind?.Trim();
            if (!string.IsNullOrWhiteSpace(kind))
            {
                message += Environment.NewLine + $"[Exception type: {kind}]";
            }

            string exceptionMessage = notification.Exception?.Message?.Trim();
            if (!string.IsNullOrWhiteSpace(exceptionMessage))
            {
                message += Environment.NewLine + $"[Exception message: {exceptionMessage}]";
            }

            return message;
        }

        /// <summary>
        /// Creates the content that goes into the ToolTipContent that gets rendered on hover.
        /// Will attempt to render markdown first, however if it fails it will fall back to plaintext.
        /// </summary>
        /// <returns>ToolTipContent to render.</returns>
        private List<(string strContent, TextRenderType renderType)> CreateContent()
        {
            if (_content == null)
            {
                _content = new List<(string strContent, TextRenderType renderType)>();
                if (!string.IsNullOrWhiteSpace(this.SarifResult?.Message?.Markdown))
                {
                    _content.Add((this.SarifResult.Message.Markdown, TextRenderType.Markdown));
                }

                string textContent = !string.IsNullOrWhiteSpace(this.RawMessage) && this.HasEmbeddedLinks ?
                                          SdkUIUtilities.GetPlainText(this.MessageInlines) :
                                          this.RawMessage;
                _content.Add((textContent, TextRenderType.Text));
                return _content;
            }
            else
            {
                return _content;
            }
        }
    }
}
