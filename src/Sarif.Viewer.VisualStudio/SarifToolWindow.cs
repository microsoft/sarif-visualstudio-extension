// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Runtime.InteropServices;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.Sarif.Viewer.Views;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.Sarif.Viewer.Models;
using System.Windows;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("ab561bcc-e01d-4781-8c2e-95a9170bfdd5")]
    public class SarifToolWindow : ToolWindowPane, IToolWindow
    {
        private ITrackSelection _trackSelection;
        private SelectionContainer _selectionContainer;
        private ISarifErrorListEventSelectionService sarifErrorListEventSelectionService;
        private ITextViewCaretListenerService<ITextMarkerTag> textViewCaretListenerService;

        internal SarifToolWindowControl Control
        {
            get
            {
                return Content as SarifToolWindowControl;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SarifToolWindow"/> class.
        /// </summary>
        public SarifToolWindow() : base(null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.Caption = "SARIF Explorer";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            Content = new SarifToolWindowControl();
            Control.Loaded += this.Control_Loaded;
            Control.Unloaded += this.Control_Unloaded;

            IComponentModel componentModel = (IComponentModel)AsyncPackage.GetGlobalService(typeof(SComponentModel));
            if (componentModel != null)
            {
                this.sarifErrorListEventSelectionService = componentModel.GetService<ISarifErrorListEventSelectionService>();
                this.textViewCaretListenerService = componentModel.GetService<ITextViewCaretListenerService<ITextMarkerTag>>();
            }
        }

        private void Control_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            // Subscribe to the error list event service so we
            // can properly update the data context of this control when the selection changes.
            if (this.sarifErrorListEventSelectionService != null)
            {
                this.sarifErrorListEventSelectionService.NavigatedItemChanged += this.SarifListErrorItemNavigated;
                this.UpdateDataContext(this.sarifErrorListEventSelectionService.NavigatedItem);
            }

            // Subscribe to the caret listener service so we can ensure the proper call tree node
            // is selected when the caret enters a tag representing a call tree node.
            if (this.textViewCaretListenerService != null)
            {
                this.textViewCaretListenerService.CaretEnteredTag += this.TextViewCaretListenerService_CaretEnteredTag;
            }
        }

        private void Control_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.sarifErrorListEventSelectionService != null)
            {
                this.sarifErrorListEventSelectionService.NavigatedItemChanged -= this.SarifListErrorItemNavigated;
            }

            if (this.textViewCaretListenerService != null)
            {
                this.textViewCaretListenerService.CaretEnteredTag -= this.TextViewCaretListenerService_CaretEnteredTag;
            }
        }

        private void TextViewCaretListenerService_CaretEnteredTag(object sender, CaretEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (e.Tag.Context is CallTreeNode node)
            {
                // If the node is visible in the explorer pane's UI (controlled by verbosity slider)
                // and have a parent call tree node, then mark it as selected.
                if (node.Visibility == Visibility.Visible && node.CallTree != null)
                {
                    node.CallTree.SelectedItem = node;
                    this.UpdateSelectionList(node.TypeDescriptor);
                }
            }
        }

        private void SarifListErrorItemNavigated(object sender, SarifErrorListSelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.UpdateDataContext(e.NewItem);
        }

        public void Show()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ((IVsWindowFrame)Frame).Show();
        }

        private ITrackSelection TrackSelection
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (_trackSelection == null)
                {
                    _trackSelection = GetService(typeof(STrackSelection)) as ITrackSelection;
                }

                return _trackSelection;
            }
        }

        /// <summary>
        /// Updates the Properties window with the public properties of the selection objects.
        /// </summary>
        public void ApplySelection()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ITrackSelection track = TrackSelection;
            if (track != null)
            {
                track.OnSelectChange((ISelectionContainer)_selectionContainer);
            }
        }

        /// <summary>
        /// Replaces the collection of objects whose values are displayed in the Properties window.
        /// </summary>
        /// <param name="items"></param>
        public void UpdateSelectionList(params object[] items)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _selectionContainer = new SelectionContainer(true, false);
            _selectionContainer.SelectableObjects = items;
            _selectionContainer.SelectedObjects = items;
            ApplySelection();
        }

        /// <summary>
        /// Reset the Properties window to display the properties of the selected Error List item.
        /// </summary>
        public void ResetSelection()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            UpdateSelectionList(Control?.DataContext);
        }

        private void UpdateDataContext(SarifErrorListItem sarifErrorListItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Resetting the data context to null causes the correct tab in the SAIRF explorer
            // window to be selected when the data context is set back to a non-null value.
            this.Control.DataContext = null;
            this.Control.DataContext = sarifErrorListItem;
            this.ResetSelection();
        }
    }
}
