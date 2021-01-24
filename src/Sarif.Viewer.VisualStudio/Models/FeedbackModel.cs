// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Models
{
    // View model for the feedback dialog
    public class FeedbackModel
    {
        private Microsoft.VisualStudio.PlatformUI.DelegateCommand sendFeedbackCommand;

        public FeedbackModel(string ruleId, string toolName, string toolVersion, IEnumerable<string> snippets, FeedbackType feedbackType, string summary, SarifLog log)
        {
            this.RuleId = ruleId;
            this.ToolName = toolName;
            this.ToolVersion = toolVersion;
            this.SendSnippet = true;
            this.Snippets = snippets;
            this.Comment = string.Empty;
            this.FeedbackType = feedbackType;
            this.Summary = summary;
            this.SarifLog = log;
        }

        public string RuleId { get; }

        public string ToolName { get; }

        public string ToolVersion { get; }

        public bool SendSnippet { get; set; }

        public IEnumerable<string> Snippets { get; set; }

        public string Comment { get; set; }

        public FeedbackType FeedbackType { get; }

        public string Summary { get; set; }

        internal SarifLog SarifLog { get; set; }

        public Microsoft.VisualStudio.PlatformUI.DelegateCommand SendFeedbackCommand
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (this.sendFeedbackCommand == null)
                {
                    this.sendFeedbackCommand = new Microsoft.VisualStudio.PlatformUI.DelegateCommand((param) =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();

                        ErrorListService.SendFeedback(this);

                        DialogWindow dialogWindow = param as DialogWindow;
                        dialogWindow.Close();
                    });
                }

                return this.sendFeedbackCommand;
            }
        }
    }
}
