// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using XamlDoc = System.Windows.Documents;

namespace Microsoft.Sarif.Viewer
{
    public class SarifErrorListItem : NotifyPropertyChangedObject
    {
        private int _runId;
        private string _fileName;
        private ToolModel _tool;
        private RuleModel _rule;
        private InvocationModel _invocation;
        private string _selectedTab;
        private DelegateCommand _openLogFileCommand;
        private ObservableCollection<XamlDoc.Inline> _messageInlines;
        private ResultTextMarker _lineMarker;

        internal SarifErrorListItem()
        {
            Locations = new LocationCollection(string.Empty);
            RelatedLocations = new LocationCollection(string.Empty);
            CallTrees = new CallTreeCollection();
            Stacks = new ObservableCollection<StackCollection>();
            Fixes = new ObservableCollection<FixModel>();
        }

        public SarifErrorListItem(Run run, Result result, string logFilePath, ProjectNameCache projectNameCache) : this()
        {
            _runId = CodeAnalysisResultManager.Instance.CurrentRunId;
            IRule rule;
            run.TryGetRule(result.RuleId, out rule);
            Tool = run.Tool.ToToolModel();
            Rule = rule.ToRuleModel(result.RuleId);
            Invocation = run.Invocations?[0]?.ToInvocationModel();
            Message = result.GetMessageText(rule, concise: false).Trim();
            ShortMessage = result.GetMessageText(rule, concise: true).Trim();
            if (!Message.EndsWith("."))
            {
                ShortMessage = ShortMessage.TrimEnd('.');
            }
            FileName = result.GetPrimaryTargetFile();
            ProjectName = projectNameCache.GetName(FileName);
            Category = result.GetCategory();
            Region = result.GetPrimaryTargetRegion();
            Level = result.Level != ResultLevel.Default ? result.Level : Rule.ResultLevel;
            SuppressionStates = result.SuppressionStates;
            LogFilePath = logFilePath;

            if (Region != null)
            {
                LineNumber = Region.StartLine;
                ColumnNumber = Region.StartColumn;
            }

            Tool = run.Tool.ToToolModel();
            Rule = rule.ToRuleModel(result.RuleId);
            Invocation = run.Invocations?[0]?.ToInvocationModel();
            WorkingDirectory = Path.Combine(Path.GetTempPath(), _runId.ToString());

            if (result.Locations != null)
            {
                foreach (Location location in result.Locations)
                {
                    Locations.Add(location.ToLocationModel());
                }
            }

            if (result.RelatedLocations != null)
            {
                foreach (Location location in result.RelatedLocations)
                {
                    RelatedLocations.Add(location.ToLocationModel());
                }
            }

            if (result.CodeFlows != null)
            {
                foreach (CodeFlow codeFlow in result.CodeFlows)
                {
                    CallTrees.Add(codeFlow.ToCallTree());
                }

                CallTrees.Verbosity = 100;
                CallTrees.IntelligentExpand();
            }

            if (result.Stacks != null)
            {
                foreach (Stack stack in result.Stacks)
                {
                    Stacks.Add(stack.ToStackCollection());
                }
            }

            if (result.Fixes != null)
            {
                foreach (Fix fix in result.Fixes)
                {
                    Fixes.Add(fix.ToFixModel());
                }
            }
        }

        public SarifErrorListItem(Run run, Notification notification, string logFilePath, ProjectNameCache projectNameCache) : this()
        {
            _runId = CodeAnalysisResultManager.Instance.CurrentRunId;
            IRule rule;
            string ruleId = notification.RuleId ?? notification.Id;
            run.TryGetRule(ruleId, out rule);
            Message = notification.Message.Text.Trim();
            ShortMessage = ExtensionMethods.GetFirstSentence(notification.Message.Text);
            if (!Message.EndsWith("."))
            {
                ShortMessage = ShortMessage.TrimEnd('.');
            }
            LogFilePath = logFilePath;
            FileName = SdkUIUtilities.GetFileLocationPath(notification.PhysicalLocation?.FileLocation, _runId) ?? run.Tool.FullName;
            ProjectName = projectNameCache.GetName(FileName);
            Locations.Add(new LocationModel() { FilePath = FileName });

            Tool = run.Tool.ToToolModel();
            Rule = rule.ToRuleModel(ruleId);
            Rule.DefaultLevel = (RuleConfigurationDefaultLevel)Enum.Parse(typeof(ResultLevel), notification.Level.ToString());
            Invocation = run.Invocations?[0]?.ToInvocationModel();
            WorkingDirectory = Path.Combine(Path.GetTempPath(), _runId.ToString());
        }

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
                NotifyPropertyChanged("FileName");
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
        public ResultLevel Level { get; set; }

        [Browsable(false)]
        public string HelpLink { get; set; }

        [DisplayName("Suppression states")]
        [ReadOnly(true)]
        public SuppressionStates SuppressionStates { get; set; }

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
                NotifyPropertyChanged("Tool");
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
                NotifyPropertyChanged("Rule");
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
                NotifyPropertyChanged("Invocation");
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
                _selectedTab = value;

                // If a new tab is selected, remove all the the markers for the
                // previous tab.
                RemoveMarkers();

                // If a new tab is selected, reset the Properties window.
                SarifViewerPackage.SarifToolWindow.ResetSelection();
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
                return (Locations.Count + RelatedLocations.Count) > 0 || CallTrees.Count > 0 || Stacks.Count > 0 || Fixes.Count > 0;
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
                    _openLogFileCommand = new DelegateCommand(() => OpenLogFile());
                }

                return _openLogFileCommand;
            }
        }

        internal void RemoveMarkers()
        {
            LineMarker?.RemoveHighlightMarker();

            foreach (LocationModel location in Locations)
            {
                location.LineMarker?.RemoveHighlightMarker();
            }

            foreach (LocationModel location in RelatedLocations)
            {
                location.LineMarker?.RemoveHighlightMarker();
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
                        current.LineMarker?.RemoveHighlightMarker();
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
                    stackFrame.LineMarker?.RemoveHighlightMarker();
                }
            }
        }

        internal void OpenLogFile()
        {
            if (LogFilePath != null)
            {
                if (File.Exists(LogFilePath))
                {
                    SarifViewerPackage.Dte.ExecuteCommand("File.OpenFile", $@"""{LogFilePath}"" /e:""JSON Editor""");
                }
                else
                {
                    VsShellUtilities.ShowMessageBox(SarifViewerPackage.ServiceProvider,
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
                    _lineMarker = new ResultTextMarker(SarifViewerPackage.ServiceProvider, _runId, Region, FileName);
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
            if (FileName.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
            {
                FileName = remappedPath;
            }

            foreach (LocationModel location in Locations)
            {
                if (location.FilePath.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
                {
                    location.FilePath = remappedPath;
                }
            }

            foreach (LocationModel location in RelatedLocations)
            {
                if (location.FilePath.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
                {
                    location.FilePath = remappedPath;
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
                    }
                }
            }

            foreach (FixModel fixModel in Fixes)
            {
                foreach (FileChangeModel fileChangeModel in fixModel.FileChanges)
                {
                    if (fileChangeModel.FilePath.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
                    {
                        fileChangeModel.FilePath = remappedPath;
                    }
                }
            }
        }

        internal void AttachToDocument(string documentName, long docCookie, IVsWindowFrame pFrame)
        {
            LineMarker?.AttachToDocument(documentName, docCookie, pFrame);

            foreach (LocationModel location in Locations)
            {
                location.LineMarker?.AttachToDocument(documentName, docCookie, pFrame);
            }

            foreach (LocationModel location in RelatedLocations)
            {
                location.LineMarker?.AttachToDocument(documentName, docCookie, pFrame);
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

                    if (current.LineMarker?.CanAttachToDocument(documentName, docCookie, pFrame) == true)
                    {
                        current.LineMarker?.AttachToDocument(documentName, (long)docCookie, pFrame);
                        current.ApplyDefaultSourceFileHighlighting();
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
                    stackFrame.LineMarker?.AttachToDocument(documentName, docCookie, pFrame);
                }
            }
        }

        internal void DetachFromDocument(long docCookie)
        {
            LineMarker?.DetachFromDocument(docCookie);

            foreach (LocationModel location in Locations)
            {
                location.LineMarker?.DetachFromDocument(docCookie);
            }

            foreach (LocationModel location in RelatedLocations)
            {
                location.LineMarker?.DetachFromDocument(docCookie);
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
                    current.LineMarker?.DetachFromDocument((long)docCookie);

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
                    stackFrame.LineMarker?.DetachFromDocument(docCookie);
                }
            }
        }
    }
}