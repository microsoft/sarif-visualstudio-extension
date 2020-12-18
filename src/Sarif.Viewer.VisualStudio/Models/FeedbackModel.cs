// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer.Models
{
    // View model for the feedback dialog
    public class FeedbackModel
    {
        private DelegateCommand sendFeedbackCommand;

        public FeedbackModel(string ruleId, FeedbackType feedbackType)
        {
            this.RuleId = ruleId;
            this.FeedbackType = feedbackType;
            this.SendSnippet = true;
            this.Comment = string.Empty;
        }

        public string RuleId { get; }
        public FeedbackType FeedbackType { get; }
        public bool SendSnippet { get; set; }
        public string Comment { get; set; }

        public DelegateCommand SendFeedbackCommand
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (this.sendFeedbackCommand == null)
                {
                    this.sendFeedbackCommand = new DelegateCommand(() =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();

                        VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
                                   $"FeedbackType: {this.FeedbackType}, Comment: {this.Comment}",
                                   null, // title
                                   OLEMSGICON.OLEMSGICON_INFO,
                                   OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                   OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    });
                }

                return this.sendFeedbackCommand;
            }
        }
    }
}
