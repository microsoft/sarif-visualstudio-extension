// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using Microsoft.VisualStudio.Text.Differencing;

namespace Microsoft.Sarif.Viewer.Fixes
{
    /// <summary>
    /// Interaction logic for DisposableDifferenceViewerControl.xaml.
    /// </summary>
    public partial class DisposableDifferenceViewerControl : UserControl, IDisposable
    {
        private IWpfDifferenceViewer differenceViewer = null;

        private readonly SarifErrorListItem sarifErrorListItem = null;

        internal event EventHandler<ApplyFixEventArgs> ApplyFixesInDocument;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableDifferenceViewerControl"/> class.
        /// </summary>
        /// <param name="errorListItem">Corresponding SarifErrorListItem object.</param>
        /// <param name="differenceViewer"><see cref="IWpfDifferenceViewer"/> instance.</param>
        /// <param name="description">Description of the changes. Optional.</param>
        /// <param name="additionalContent">Additional content to display at the bottom. Optional.</param>
        internal DisposableDifferenceViewerControl(
            SarifErrorListItem errorListItem,
            IWpfDifferenceViewer differenceViewer,
            string description = null,
            FrameworkElement additionalContent = null)
        {
            this.InitializeComponent();

            this.sarifErrorListItem = errorListItem;
            this.differenceViewer = Requires.NotNull(differenceViewer, nameof(differenceViewer));

            if (!string.IsNullOrWhiteSpace(description))
            {
                var descriptionBlock = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(5),
                };

                descriptionBlock.Inlines.Add(new Run()
                {
                    Text = description,
                });

                this.StackPanelContent.Children.Add(descriptionBlock);
            }

            this.StackPanelContent.Children.Add(this.differenceViewer.VisualElement);

            if (additionalContent != null)
            {
                this.StackPanelContent.Children.Add(additionalContent);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.differenceViewer != null)
            {
                this.differenceViewer.Close();
                this.differenceViewer = null;
            }
        }

        private void ApplyInDocument_Click(object sender, RoutedEventArgs e)
        {
            this.StackPanelContent.Children.Clear();
            ApplyFixesInDocument?.Invoke(this, new ApplyFixEventArgs(FixScope.Document, this.sarifErrorListItem));
        }
    }
}
