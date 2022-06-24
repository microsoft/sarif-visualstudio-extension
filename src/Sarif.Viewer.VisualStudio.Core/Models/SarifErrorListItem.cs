// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

using EnvDTE;

using EnvDTE80;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

using Newtonsoft.Json;

using XamlDoc = System.Windows.Documents;

namespace Microsoft.Sarif.Viewer
{
    internal class SarifErrorListItem : NotifyPropertyChangedObject, IDisposable
    {
        // max length of concise text, 0 indexed
        internal static int MaxConcisedTextLength = 150;
        internal static string XamlPropertyName = "Xaml";

        // Contains the result Id that will be incremented and assigned to new instances of <see cref="SarifErrorListItem"/>.
        private static int currentResultId;

        private string _fileName;
        private ToolModel _tool;
        private RuleModel _rule;
        private InvocationModel _invocation;
        private string _selectedTab;
        private DelegateCommand _openLogFileCommand;
        private ObservableCollection<XamlDoc.Inline> _messageInlines;
        private ResultTextMarker _lineMarker;
        private bool isDisposed;

        /// <summary>
        /// This dictionary is used to map the SARIF failure level to the color of the "squiggle" shown
        /// in Visual Studio's editor.
        /// </summary>
        private static readonly Dictionary<FailureLevel, string> FailureLevelToPredefinedErrorTypes = new Dictionary<FailureLevel, string>
        {
            { FailureLevel.Error, PredefinedErrorTypeNames.OtherError },
            { FailureLevel.Warning, PredefinedErrorTypeNames.Warning },
            { FailureLevel.Note, PredefinedErrorTypeNames.HintedSuggestion },
        };

        internal SarifErrorListItem()
        {
            this.Locations = new LocationCollection(string.Empty);
            this.RelatedLocations = new LocationCollection(string.Empty);
            this.AnalysisSteps = new AnalysisStepCollection();
            this.Stacks = new ObservableCollection<StackCollection>();
            this.Fixes = new ObservableCollection<FixModel>();
            this.Properties = new ObservableCollection<KeyValuePair<string, string>>();
        }

        public SarifErrorListItem(Run run, int runIndex, Result result, string logFilePath, ProjectNameCache projectNameCache)
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
            this.SarifResult = result;
            ReportingDescriptor rule = result.GetRule(run);
            this.Tool = run.Tool.ToToolModel();
            this.Rule = rule.ToRuleModel(result.RuleId);
            this.Invocation = run.Invocations?[0]?.ToInvocationModel();
            this.WorkingDirectory = Path.Combine(Path.GetTempPath(), this.RunIndex.ToString());
            this.HelpLink = this.Rule?.HelpUri;

            this.RawMessage = result.GetMessageText(rule, concise: false).Trim();
            (this.ShortMessage, this.Message) = SdkUIUtilities.SplitResultMessage(this.RawMessage, MaxConcisedTextLength);

            string xamlContent = null;
            if (this.SarifResult?.Message?.TryGetProperty(XamlPropertyName, out xamlContent) == true)
            {
                this.XamlMessage = Regex.Unescape(xamlContent);
            }

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

        /// <summary>
        /// Fired when this error list item is disposed.
        /// </summary>
        /// <remarks>
        /// An example of the usage of this is making sure that the SARIF explorer window
        /// doesn't hold on to a disposed object when the error list is cleared.
        /// </remarks>
        public event EventHandler Disposed;

        private FailureLevel GetEffectiveLevel(Result result)
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

        public SarifErrorListItem(Run run, int runIndex, Notification notification, string logFilePath, ProjectNameCache projectNameCache)
            : this()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
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
            this.RawMessage = notification.Message.Text?.Trim() ?? string.Empty;
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
        /// Gets the result ID that uniquely identifies this result for this Visual Studio session.
        /// </summary>
        /// <remarks>
        /// This property is used by the tagger to perform queries over the SARIF errors whereas the
        /// <see cref="RunIndex"/> property is not yet used.
        /// </remarks>
        public int ResultId { get; }

        /// <summary>
        /// Gets reference to corresponding <see cref="SarifLog.Result" /> object.
        /// </summary>
        public Result SarifResult { get; }

        public int RunIndex { get; }

        [Browsable(false)]
        public string MimeType { get; set; }

        [Browsable(false)]
        public Region Region { get; set; }

        [Browsable(false)]
        public string FileName
        {
            get => this._fileName;

            set
            {
                if (value == this._fileName) { return; }
                this._fileName = value;
                this.NotifyPropertyChanged();
            }
        }

        [Browsable(false)]
        public string ProjectName { get; set; }

        [Browsable(false)]
        public bool RegionPopulated { get; set; }

        [Browsable(false)]
        public string WorkingDirectory { get; set; }

        [Browsable(false)]
        public string ShortMessage { get; set; }

        [Browsable(false)]
        public string Message { get; set; }

        [Browsable(false)]
        public string RawMessage { get; set; }

        [Browsable(false)]
        public string PlainMessage => !string.IsNullOrWhiteSpace(this.RawMessage) && this.HasEmbeddedLinks ?
                                      SdkUIUtilities.GetPlainText(this.MessageInlines) :
                                      this.RawMessage;

        [Browsable(false)]
        public ObservableCollection<XamlDoc.Inline> MessageInlines => this._messageInlines ??=
            new ObservableCollection<XamlDoc.Inline>(SdkUIUtilities.GetMessageInlines(this.RawMessage, this.MessageInlineLink_Click));

        [Browsable(false)]
        public bool HasEmbeddedLinks => this.MessageInlines.Any();

        [Browsable(false)]
        public bool HasDetailsContent =>
            !string.IsNullOrWhiteSpace(this.Message)
            && this.Message != this.ShortMessage;

        [Browsable(false)]
        public string XamlMessage { get; }

        [Browsable(false)]
        public SnapshotSpan Span { get; set; }

        [Browsable(false)]
        public int LineNumber { get; set; }

        [Browsable(false)]
        public int ColumnNumber { get; set; }

        [Browsable(false)]
        public string Category { get; set; }

        [ReadOnly(true)]
        public FailureLevel Level { get; set; }

        [Browsable(false)]
        public string HelpLink { get; set; }

        [DisplayName("Suppression status")]
        [ReadOnly(true)]
        public VSSuppressionState VSSuppressionState { get; set; }

        [DisplayName("Log file")]
        [ReadOnly(true)]
        public string LogFilePath { get; set; }

        [Browsable(false)]
        public ToolModel Tool
        {
            get => this._tool;

            set
            {
                this._tool = value;
                this.NotifyPropertyChanged();
            }
        }

        [Browsable(false)]
        public RuleModel Rule
        {
            get => this._rule;

            set
            {
                this._rule = value;
                this.NotifyPropertyChanged();
            }
        }

        [Browsable(false)]
        public InvocationModel Invocation
        {
            get => this._invocation;

            set
            {
                this._invocation = value;
                this.NotifyPropertyChanged();
            }
        }

        [Browsable(false)]
        public string SelectedTab
        {
            get => this._selectedTab;

            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this._selectedTab = value;

                // If a new tab is selected, reset the Properties window.
                SarifExplorerWindow.Find()?.ResetSelection();
            }
        }

        [Browsable(false)]
        public LocationCollection Locations { get; }

        [Browsable(false)]
        public LocationCollection RelatedLocations { get; }

        [Browsable(false)]
        public AnalysisStepCollection AnalysisSteps { get; }

        [Browsable(false)]
        public ObservableCollection<StackCollection> Stacks { get; }

        [Browsable(false)]
        public ObservableCollection<FixModel> Fixes { get; }

        [Browsable(false)]
        public ObservableCollection<KeyValuePair<string, string>> Properties { get; }

        [Browsable(false)]
        public bool HasDetails => this.SarifResult.Fixes?.Any() == true ||
                                  this.SarifResult.Stacks?.Any() == true ||
                                  this.SarifResult.CodeFlows?.Any() == true ||
                                  this.SarifResult.Locations?.Any() == true ||
                                  this.SarifResult.RelatedLocations?.Any() == true;

        [Browsable(false)]
        public int LocationsCount => this.Locations.Count + this.RelatedLocations.Count;

        [Browsable(false)]
        public bool HasMultipleLocations => this.LocationsCount > 1;

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
                            MaxDepth = 64,
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

        [Browsable(false)]
        public ResultTextMarker LineMarker
        {
            get
            {
                if (this._lineMarker == null && this.Region?.StartLine > 0)
                {
                    FailureLevelToPredefinedErrorTypes.TryGetValue(this.Level, out string predefinedErrorType);

                    this._lineMarker = new ResultTextMarker(
                        runIndex: this.RunIndex,
                        resultId: this.ResultId,
                        uriBaseId: this.Locations?.FirstOrDefault()?.UriBaseId,
                        region: this.Region,
                        fullFilePath: this.FileName,
                        nonHighlightedColor: ResultTextMarker.DEFAULT_SELECTION_COLOR,
                        highlightedColor: ResultTextMarker.HOVER_SELECTION_COLOR,
                        errorType: predefinedErrorType,
                        tooltipContent: this.PlainMessage,
                        tooltipXamlString: this.XamlMessage,
                        context: this);
                }

                return this._lineMarker;
            }

            set => this._lineMarker = value;
        }

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
                    location.Region = regionsCache.PopulateTextRegionProperties(location.Region, uri, true);
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

            // After the file-paths have been remapped, we need to refresh the tags
            // as it may now be possible to create the persistent spans (since the file paths are now potentially valid)
            // or their file paths may have moved from one valid location to a different valid location.
            SarifLocationTagHelpers.RefreshTags();
        }

        internal void PopulateFixModelsIfNot()
        {
            // populate FixModels if they are not
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
                for (int i = this.SarifResult.RelatedLocations.Count - 1; i >= 0; --i)
                {
                    this.RelatedLocations.Add(this.SarifResult.RelatedLocations[i].ToLocationModel(this.SarifResult.Run, resultId: this.ResultId, runIndex: this.RunIndex));
                }
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
                    var analysisStep = codeFlow.ToAnalysisStep(this.SarifResult.Run, resultId: this.ResultId, runIndex: this.RunIndex);
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

        public IEnumerable<ISarifLocationTag> GetTags<T>(ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory, bool includeChildTags, bool includeResultTag)
            where T : ITag
        {
            IEnumerable<ISarifLocationTag> tags = Enumerable.Empty<ISarifLocationTag>();
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
    }
}
