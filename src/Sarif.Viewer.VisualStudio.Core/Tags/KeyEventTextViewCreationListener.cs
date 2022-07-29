// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using System.Windows.Media;

using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports
    /// the <see cref="IWpfTextViewCreationListener"/> that instantiates the adornment on
    /// the event of a <see cref="IWpfTextView"/>'s creation.
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class KeyEventTextViewCreationListener : IWpfTextViewCreationListener
    {
#pragma warning disable CS0169 // Variable is never used
#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF
        /// <summary>
        /// Defines the adornment layer for the adornment. This layer is ordered
        /// after the selection layer in the Z-order.
        /// </summary>
        [Export(typeof(AdornmentLayerDefinition))]
        [Name(nameof(KeyEventAdornmentManager))]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        private AdornmentLayerDefinition editorAdornmentLayer;

        [Import]
        public IViewTagAggregatorFactoryService TagAggregatorFactoryService;

        [Import]
        private ISarifErrorListEventSelectionService sarifErrorListEventSelectionService;
#pragma warning restore CS0169 // Variable is never used
#pragma warning restore CS0649 // Filled in by MEF
#pragma warning restore IDE0044 // Assigned by MEF

        /// <summary>
        /// Called when a text view having matching roles is created over a text data model having a matching content type.
        /// Instantiates a ColumnGuideTextAdornment manager when the textView is created.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed.</param>
        public void TextViewCreated(IWpfTextView textView)
        {
            // The adornment will listen to any event that changes the layout (text changes, scrolling, etc)
            new KeyEventAdornmentManager(textView, sarifErrorListEventSelectionService, TagAggregatorFactoryService);
        }
    }
}
