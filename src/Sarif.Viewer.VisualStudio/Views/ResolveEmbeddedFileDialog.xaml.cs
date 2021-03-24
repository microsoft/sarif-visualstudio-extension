// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Sarif.Viewer.Views
{
    /// <summary>
    /// Interaction logic for ResolveEmbeddedFileDialog.xaml.
    /// </summary>
    public partial class ResolveEmbeddedFileDialog : DialogWindow
    {
        public ResolveEmbeddedFileDialog(bool hasEmbeddedContent)
        {
            InitializeComponent();
            Loaded += OnLoaded;
            this.HasEmbeddedFileContent = hasEmbeddedContent;
        }

        public bool HasEmbeddedFileContent { get; set; }

        public ResolveEmbeddedFileDialogResult Result;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.OpenEmbeddedFileButton.Visibility = this.HasEmbeddedFileContent ? Visibility.Visible : Visibility.Hidden;
            this.Message.Text = this.HasEmbeddedFileContent ? Viewer.Resources.ConfirmSourceFileDialog_Message : Viewer.Resources.ConfirmSourceFileDialog_Message_NoEmbedded;
        }

        private void OpenEmbeddedFileButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Result = ResolveEmbeddedFileDialogResult.OpenEmbeddedFileContent;
            this.DialogResult = true;
            this.Close();
        }

        private void OpenLocalFileButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Result = ResolveEmbeddedFileDialogResult.OpenLocalFileFromSolution;
            this.DialogResult = true;
            this.Close();
        }

        private void BrowseFileButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Result = ResolveEmbeddedFileDialogResult.BrowseAlternateLocation;
            this.DialogResult = true;
            this.Close();
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
