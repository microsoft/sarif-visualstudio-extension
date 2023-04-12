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

using Run = Microsoft.CodeAnalysis.Sarif.Run;
using XamlDoc = System.Windows.Documents;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// This holds the properties of the <see cref="SarifErrorListItem"/> class.
    /// </summary>
    internal partial class SarifErrorListItem : NotifyPropertyChangedObject, IDisposable
    {
        /// <summary>
        /// Max length of concise text, 0 indexed.
        /// </summary>
        internal static int MaxConcisedTextLength = 150;

        /// <summary>
        /// Gets the result ID that uniquely identifies this result for this Visual Studio session.
        /// </summary>
        /// <remarks>
        /// This property is used by the tagger to perform queries over the SARIF errors whereas the
        /// <see cref="RunIndex"/> property is not yet used.
        /// </remarks>
        public int ResultId { get; }

        /// <summary>
        /// Gets the Sarif result's guid. Can be null.
        /// </summary>
        /// <remarks>
        /// In Key Event scenario, it is used to identify each unique warning and log to telemetry.
        /// </remarks>
        public string ResultGuid { get; }

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

        /// <summary>
        /// Gets an ordered list of the different text to render and the format it should be rendered in. Sorted by most preferred format to render to least.
        /// </summary>
        [Browsable(false)]
        public List<(string strContent, TextRenderType renderType)> Content => this.CreateContent();

        private List<(string strContent, TextRenderType renderType)> _content;

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
        public int LocationsCount => this.Locations.Count + this.RelatedLocations.DeepCount;

        [Browsable(false)]
        public bool HasMultipleLocations => this.LocationsCount > 1;

        /// <summary>
        /// Contains the result Id that will be incremented and assigned to new instances of <see cref="SarifErrorListItem"/>.
        /// </summary>
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
    }
}
