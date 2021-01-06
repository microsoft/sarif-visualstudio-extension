// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Telemetry;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer.Models
{
    // View model for the feedback dialog
    public class FeedbackModel
    {
        private Microsoft.VisualStudio.PlatformUI.DelegateCommand sendFeedbackCommand;

        public FeedbackModel(string ruleId, string toolName, string toolVersion, string snippet, FeedbackType feedbackType, string summary)
        {
            this.RuleId = ruleId;
            this.FeedbackType = feedbackType;
            this.ToolName = toolName;
            this.ToolVersion = toolVersion;
            this.Snippet = snippet;
            this.SendSnippet = true;
            this.Comment = string.Empty;
            this.Summary = summary;
        }

        public string RuleId { get; }
        public FeedbackType FeedbackType { get; }
        public string ToolName { get; }
        public string ToolVersion { get; }
        public bool SendSnippet { get; set; }
        public string Comment { get; set; }
        public string Summary { get; set; }
        public string Snippet { get; set; }

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
