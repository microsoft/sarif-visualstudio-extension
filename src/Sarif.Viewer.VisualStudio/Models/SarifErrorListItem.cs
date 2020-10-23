// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using XamlDoc = System.Windows.Documents;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.Sarif.Viewer
{
    internal class SarifErrorListItem : NotifyPropertyChangedObject, IDisposable
    {
        /// <summary>
        /// Contains the result Id that will be incremented and assigned to new instances of <see cref="SarifErrorListItem"/>.
        /// </summary>
        private static int CurrentResultId;

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
            Locations = new LocationCollection(string.Empty);
            RelatedLocations = new LocationCollection(string.Empty);
            CallTrees = new CallTreeCollection();
            Stacks = new ObservableCollection<StackCollection>();
            Fixes = new ObservableCollection<FixModel>();
        }

        public SarifErrorListItem(Run run, int runIndex, Result result, string logFilePath, ProjectNameCache projectNameCache) : this()
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally
            }

            RunIndex = runIndex;
            ResultId = Interlocked.Increment(ref CurrentResultId);
            ReportingDescriptor rule = result.GetRule(run);
            Tool = run.Tool.ToToolModel();
            Rule = rule.ToRuleModel(result.RuleId);
            Invocation = run.Invocations?[0]?.ToInvocationModel();
            Message = result.GetMessageText(rule, concise: false).Trim();
            ShortMessage = result.GetMessageText(rule, concise: true).Trim();
            if (!Message.EndsWith("."))
            {
                ShortMessage = ShortMessage.TrimEnd('.');
            }
            FileName = result.GetPrimaryTargetFile(run);
            ProjectName = projectNameCache.GetName(FileName);
            Category = result.GetCategory();
            Region = result.GetPrimaryTargetRegion();
            Level = GetEffectiveLevel(result);

            if (result.Suppressions?.Count > 0)
            {
                VSSuppressionState = VSSuppressionState.Suppressed;
            }
            
            LogFilePath = logFilePath;

            if (Region != null)
            {
                LineNumber = Region.StartLine;
                ColumnNumber = Region.StartColumn;
            }

            Tool = run.Tool.ToToolModel();
            Rule = rule.ToRuleModel(result.RuleId);
            Invocation = run.Invocations?[0]?.ToInvocationModel();
            WorkingDirectory = Path.Combine(Path.GetTempPath(), RunIndex.ToString());

            if (result.Locations?.Any() == true)
            {
                // Adding in reverse order will make them display in the correct order in the UI.
                for (int i = result.Locations.Count - 1; i >= 0; --i)
                {
                    Locations.Add(result.Locations[i].ToLocationModel(run, resultId: ResultId, runIndex: RunIndex));
                }
            }

            if (result.RelatedLocations?.Any() == true)
            {
                for (int i = result.RelatedLocations.Count - 1; i >= 0; --i)
                {
                    RelatedLocations.Add(result.RelatedLocations[i].ToLocationModel(run, resultId: ResultId, runIndex: RunIndex));
                }

            }

            if (result.CodeFlows != null)
            {
                foreach (CodeFlow codeFlow in result.CodeFlows)
                {
                    CallTree callTree = codeFlow.ToCallTree(run, resultId: ResultId, runIndex: RunIndex);
                    if (callTree != null)
                    {
                        CallTrees.Add(callTree);
                    }
                }

                CallTrees.Verbosity = 100;
                CallTrees.IntelligentExpand();
            }

            if (result.Stacks != null)
            {
                foreach (Stack stack in result.Stacks)
                {
                    Stacks.Add(stack.ToStackCollection(resultId: ResultId, runIndex: RunIndex));
                }
            }

            if (result.Fixes != null)
            {
                FileRegionsCache regionsCache = CodeAnalysisResultManager.Instance.RunIndexToRunDataCache[RunIndex].FileRegionsCache;
                foreach (Fix fix in result.Fixes)
                {
                    Fixes.Add(fix.ToFixModel(run.OriginalUriBaseIds, regionsCache));
                }
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
            FailureLevel effectiveLevel;

            switch (result.Kind)
            {
                case ResultKind.Review:
                case ResultKind.Open:
                    effectiveLevel = FailureLevel.Warning;
                    break;

                case ResultKind.NotApplicable:
                case ResultKind.Informational:
                case ResultKind.Pass:
                    effectiveLevel = FailureLevel.Note;
                    break;

                case ResultKind.Fail:
                case ResultKind.None:   // Should never happen.
                default:                // Should never happen.
                    effectiveLevel = result.Level != FailureLevel.Warning ? result.Level : Rule.FailureLevel;
                    break;
            }

            return effectiveLevel;
        }

        public SarifErrorListItem(Run run, int runIndex, Notification notification, string logFilePath, ProjectNameCache projectNameCache) : this()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            RunIndex = runIndex;
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
            Message = notification.Message.Text?.Trim() ?? string.Empty;
            ShortMessage = ExtensionMethods.GetFirstSentence(Message);

            // This is not locale friendly.
            if (!Message.EndsWith("."))
            {
                ShortMessage = ShortMessage.TrimEnd('.');
            }

            Level = notification.Level;
            LogFilePath = logFilePath;
            FileName = SdkUIUtilities.GetFileLocationPath(notification.Locations?[0]?.PhysicalLocation?.ArtifactLocation, RunIndex) ?? string.Empty;
            ProjectName = projectNameCache.GetName(FileName);
            Locations.Add(new LocationModel(resultId: ResultId, runIndex: RunIndex) { FilePath = FileName });

            Tool = run.Tool.ToToolModel();
            Rule = rule.ToRuleModel(ruleId);
            Invocation = run.Invocations?[0]?.ToInvocationModel();
            WorkingDirectory = Path.Combine(Path.GetTempPath(), RunIndex.ToString());
        }

        /// <summary>
        /// Gets the result ID that uniquely identifies this result for this Visual Studio session.
        /// </summary>
        /// <remarks>
        /// This property is used by the tagger to perform queries over the SARIF errors whereas the
        /// <see cref="RunIndex"/> property is not yet used.
        /// </remarks>
        public int ResultId { get; }

        private int RunIndex { get; }

        [Browsable(false)]
        public string MimeType { get; set; }

        [Browsable(false)]
        public Region Region { get; set; }

        [Browsable(false)]
        public string FileName
        {
            get
            {
                return _fileName;
            }

            set
            {
                if (value == _fileName) { return; }
                _fileName = value;
                NotifyPropertyChanged();
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
        public ObservableCollection<XamlDoc.Inline> MessageInlines
        {
            get
            {
                if (_messageInlines == null)
                {
                    _messageInlines = new ObservableCollection<XamlDoc.Inline>(SdkUIUtilities.GetInlinesForErrorMessage(Message));
                }

                return _messageInlines;
            }
        }

        [Browsable(false)]
        public bool HasEmbeddedLinks
        {
            get
            {
                return MessageInlines.Any();
            }
        }

        [Browsable(false)]
        public bool HasDetailsContent
        {
            get
            {
                return !HasEmbeddedLinks &&
                    !string.IsNullOrWhiteSpace(Message) &&
                    Message != ShortMessage;
            }
        }

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
            get
            {
                return _tool;
            }
            set
            {
                _tool = value;
                NotifyPropertyChanged();
            }
        }

        [Browsable(false)]
        public RuleModel Rule
        {
            get
            {
                return _rule;
            }
            set
            {
                _rule = value;
                NotifyPropertyChanged();
            }
        }

        [Browsable(false)]
        public InvocationModel Invocation
        {
            get
            {
                return _invocation;
            }
            set
            {
                _invocation = value;
                NotifyPropertyChanged();
            }
        }

        [Browsable(false)]
        public string SelectedTab
        {
            get
            {
                return _selectedTab;
            }
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _selectedTab = value;

                // If a new tab is selected, reset the Properties window.
                SarifExplorerWindow.Find()?.ResetSelection();
            }
        }

        [Browsable(false)]
        public LocationCollection Locations { get; }

        [Browsable(false)]
        public LocationCollection RelatedLocations { get; }

        [Browsable(false)]
        public CallTreeCollection CallTrees { get; }

        [Browsable(false)]
        public ObservableCollection<StackCollection> Stacks { get; }

        [Browsable(false)]
        public ObservableCollection<FixModel> Fixes { get; }

        [Browsable(false)]
        public bool HasDetails
        {
            get
            {
                return Locations.Any() || RelatedLocations.Any() || CallTrees.Any() || Stacks.Any() || Fixes.Any();
            }
        }

        [Browsable(false)]
        public int LocationsCount
        {
            get
            {
                return Locations.Count + RelatedLocations.Count;
            }
        }

        [Browsable(false)]
        public bool HasMultipleLocations
        {
            get
            {
                return LocationsCount > 1;
            }
        }

        [Browsable(false)]
        public DelegateCommand OpenLogFileCommand
        {
            get
            {
                if (_openLogFileCommand == null)
                {
                    _openLogFileCommand = new DelegateCommand(() =>
                    {
                        // For now this is being done on the UI thread
                        // and is only required due to the message box being shown below.
                        // This will be addressed when https://github.com/microsoft/sarif-visualstudio-extension/issues/160
                        // is fixed.
                        ThreadHelper.ThrowIfNotOnUIThread();

                        OpenLogFile();
                    });
                }

                return _openLogFileCommand;
            }
        }

        internal void OpenLogFile()
        {
            // For now this is being done on the UI thread
            // and is only required due to the message box being shown below.
            // This will be addressed when https://github.com/microsoft/sarif-visualstudio-extension/issues/160
            // is fixed.
            ThreadHelper.ThrowIfNotOnUIThread();

            if (LogFilePath != null)
            {
                if (File.Exists(LogFilePath))
                {
                    var dte = AsyncPackage.GetGlobalService(typeof(DTE)) as DTE2;
                    dte.ExecuteCommand("File.OpenFile", $@"""{LogFilePath}"" /e:""JSON Editor""");
                }
                else
                {
                    VsShellUtilities.ShowMessageBox(Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider,
                                                    string.Format(Resources.OpenLogFileFail_DilogMessage, LogFilePath),
                                                    null, // title
                                                    OLEMSGICON.OLEMSGICON_CRITICAL,
                                                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            }
        }

        public override string ToString()
        {
            return Message;
        }

        [Browsable(false)]
        public ResultTextMarker LineMarker
        {
            get
            {
                if (_lineMarker == null && Region != null && Region.StartLine > 0)
                {
                    FailureLevelToPredefinedErrorTypes.TryGetValue(this.Level, out string predefinedErrorType);

                    _lineMarker = new ResultTextMarker(
                        resultId: ResultId,
                        runIndex: RunIndex,
                        uriBaseId: Locations?.FirstOrDefault()?.UriBaseId,
                        region: Region,
                        fullFilePath: FileName,
                        nonHighlightedColor: ResultTextMarker.DEFAULT_SELECTION_COLOR,
                        highlightedColor: ResultTextMarker.HOVER_SELECTION_COLOR,
                        errorType: predefinedErrorType,
                        tooltipContent: this.Message,
                        context: this);
                }

                return _lineMarker;
            }
            set
            {
                _lineMarker = value;
            }
        }

        internal void RemapFilePath(string originalPath, string remappedPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var uri = new Uri(remappedPath, UriKind.Absolute);
            FileRegionsCache regionsCache = CodeAnalysisResultManager.Instance.RunIndexToRunDataCache[RunIndex].FileRegionsCache;

            if (FileName.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
            {
                FileName = remappedPath;
            }

            foreach (LocationModel location in Locations)
            {
                if (location.FilePath.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
                {
                    location.FilePath = remappedPath;
                    location.Region = regionsCache.PopulateTextRegionProperties(location.Region, uri, true);
                }
            }

            foreach (LocationModel location in RelatedLocations)
            {
                if (location.FilePath.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
                {
                    location.FilePath = remappedPath;
                    location.Region = regionsCache.PopulateTextRegionProperties(location.Region, uri, true);
                }
            }

            foreach (CallTree callTree in CallTrees)
            {
                Stack<CallTreeNode> nodesToProcess = new Stack<CallTreeNode>();

                foreach (CallTreeNode topLevelNode in callTree.TopLevelNodes)
                {
                    nodesToProcess.Push(topLevelNode);
                }

                while (nodesToProcess.Count > 0)
                {
                    CallTreeNode current = nodesToProcess.Pop();
                    try
                    {
                        if (current.FilePath != null &&
                            current.FilePath.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
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

                    foreach (CallTreeNode childNode in current.Children)
                    {
                        nodesToProcess.Push(childNode);
                    }
                }
            }

            foreach (StackCollection stackCollection in Stacks)
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

            foreach (FixModel fixModel in Fixes)
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
            SarifLocationTagHelpers.RefreshAllTags();
        }

        public IEnumerable<ISarifLocationTag> GetTags<T>(ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory, bool includeChildTags, bool includeResultTag)
            where T: ITag
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

            Disposed?.Invoke(this, new EventArgs());
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
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
                resultTextMarkers = resultTextMarkers.Concat(Locations.Select(location => location.LineMarker));
                resultTextMarkers = resultTextMarkers.Concat(RelatedLocations.Select(relatedLocation => relatedLocation.LineMarker));

                foreach (CallTree callTree in CallTrees)
                {
                    Stack<CallTreeNode> nodesToProcess = new Stack<CallTreeNode>();
                    List<CallTreeNode> allCallTreeNodes = new List<CallTreeNode>();

                    foreach (CallTreeNode topLevelNode in callTree.TopLevelNodes)
                    {
                        nodesToProcess.Push(topLevelNode);
                    }

                    while (nodesToProcess.Count > 0)
                    {
                        CallTreeNode current = nodesToProcess.Pop();

                        allCallTreeNodes.Add(current);

                        foreach (CallTreeNode childNode in current.Children)
                        {
                            nodesToProcess.Push(childNode);
                        }
                    }

                    resultTextMarkers = resultTextMarkers.Concat(allCallTreeNodes.Select(callTreeNode => callTreeNode.LineMarker));
                }

                foreach (StackCollection stackCollection in Stacks)
                {
                    resultTextMarkers = resultTextMarkers.Concat(stackCollection.Select(stack => stack.LineMarker));
                }
            }

            // Some of the data models will return null markers if there aren't valid properties like
            // "region" being set. So let's filter out the nulls from the callers.
            return resultTextMarkers.Where(resultTextMarker => resultTextMarker != null);
        }
    }
}