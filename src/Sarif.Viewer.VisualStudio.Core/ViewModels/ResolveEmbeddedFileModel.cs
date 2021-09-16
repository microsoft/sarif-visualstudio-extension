// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Sarif.Viewer.ViewModels
{
    public class ResolveEmbeddedFileModel
    {
        private readonly DelegateCommand openEmbeddedFileCommand;
        private readonly DelegateCommand openLocalFileCommand;
        private readonly DelegateCommand browseFileCommand;

        public ResolveEmbeddedFileModel()
        {
            openEmbeddedFileCommand = new DelegateCommand(this.OnOpenEmbeddedFileClicked);
            openLocalFileCommand = new DelegateCommand(OnOpenLocalFileClicked);
            browseFileCommand = new DelegateCommand(OnBrowseFileClicked);
        }

        public bool HasEmbeddedContent { get; set; }

        public bool ApplyUserPreference { get; set; }

        public string MessageText => this.HasEmbeddedContent ?
                                     Resources.ConfirmSourceFileDialog_Message :
                                     Resources.ConfirmSourceFileDialog_Message_NoEmbedded;

        public ResolveEmbeddedFileDialogResult Result { get; set; }

        public DelegateCommand OpenEmbeddedFileCommand => openEmbeddedFileCommand;

        public DelegateCommand OpenLocalFileCommand => openLocalFileCommand;

        public DelegateCommand BrowseFileCommand => browseFileCommand;

        private void OnOpenEmbeddedFileClicked()
        {
            this.Result = ResolveEmbeddedFileDialogResult.OpenEmbeddedFileContent;
        }

        private void OnOpenLocalFileClicked()
        {
            this.Result = ResolveEmbeddedFileDialogResult.OpenLocalFileFromSolution;
        }

        private void OnBrowseFileClicked()
        {
            this.Result = ResolveEmbeddedFileDialogResult.BrowseAlternateLocation;
        }
    }

    public enum ResolveEmbeddedFileDialogResult
    {
        /// <summary>
        /// Default value, no result selected when dialog closed.
        /// </summary>
        None,

        /// <summary>
        /// Result of open embedded file content.
        /// </summary>
        OpenEmbeddedFileContent,

        /// <summary>
        /// Result of open local file.
        /// </summary>
        OpenLocalFileFromSolution,

        /// <summary>
        /// Result of browse alternate folder.
        /// </summary>
        BrowseAlternateLocation,
    }
}
