// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Sarif.Viewer.ViewModels;

namespace Microsoft.Sarif.Viewer.Views
{
    /// <summary>
    /// Interaction logic for ResolveEmbeddedFileDialog.xaml.
    /// </summary>
    public partial class ResolveEmbeddedFileDialogControl : UserControl, IDisposable
    {
        private Window window;

        public ResolveEmbeddedFileDialogControl(ResolveEmbeddedFileModel model)
        {
            this.InitializeComponent();
            this.DataContext = model;
            this.Loaded += OnLoaded;
        }

        public ResolveEmbeddedFileDialogResult Result;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.window = Window.GetWindow(this);
        }

        private void OpenEmbeddedFileButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Result = ResolveEmbeddedFileDialogResult.OpenEmbeddedFileContent;
            this.window.DialogResult = true;
            this.CloseWindow();
        }

        private void OpenLocalFileButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Result = ResolveEmbeddedFileDialogResult.OpenLocalFileFromSolution;
            this.window.DialogResult = true;
            this.CloseWindow();
        }

        private void BrowseFileButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Result = ResolveEmbeddedFileDialogResult.BrowseAlternateLocation;
            this.window.DialogResult = true;
            this.CloseWindow();
        }

        private void CloseWindow()
        {
            this.window?.Close();
        }

        public void Dispose()
        {
            this.Loaded -= this.OnLoaded;
        }
    }
}
