// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Windows;

using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.Sarif.Viewer.Views;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Tagging;

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
    public class SarifExplorerWindow : ToolWindowPane, IToolWindow
    {
        /// <summary>
        /// Track selection is for development (design time) only. It has no impact on runtime.
        /// It updates VS's "properties" pane.
        /// </summary>
        private ITrackSelection _trackSelection;
        private SelectionContainer _selectionContainer;

        private readonly ISarifErrorListEventSelectionService sarifErrorListEventSelectionService;
        private readonly ITextViewCaretListenerService<ITextMarkerTag> textViewCaretListenerService;

        internal SarifToolWindowControl Control => this.Content as SarifToolWindowControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="SarifExplorerWindow"/> class.
        /// </summary>
        public SarifExplorerWindow()
            : base(null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.Caption = Resources.SarifExplorerCaption;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new SarifToolWindowControl();
            this.Control.Loaded += this.Control_Loaded;
            this.Control.Unloaded += this.Control_Unloaded;

            var componentModel = (IComponentModel)AsyncPackage.GetGlobalService(typeof(SComponentModel));
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

            // Update the selection.
            this._trackSelection = this.GetService(typeof(STrackSelection)) as ITrackSelection;
            this._trackSelection.OnSelectChange(this._selectionContainer);
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

        private void TextViewCaretListenerService_CaretEnteredTag(object sender, TagInCaretChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (e.Tag.Context is AnalysisStepNode node)
            {
                // If the node is visible in the explorer pane's UI (controlled by verbosity slider)
                // and have a parent call tree node, then mark it as selected.
                if (node.Visibility == Visibility.Visible && node.AnalysisStep != null)
                {
                    // Setting the selected item here causes the SARIF explorer window to update it's selection.
                    // The implementation here is a bit weird because we are telling the "data model" about the
                    // item that is to be selected in the UI. In a better world, the concept of "selection" would
                    // be in the UI logic, not the data model.
                    node.AnalysisStep.SelectedItem = node;
                    this.UpdateSelectionList(node.TypeDescriptor);
                }
            }

            if (e.Tag.Context is LocationModel locationModel)
            {
                // Setting the selected item here causes the SARIF explorer window to update it's selection.
                // The implementation here is a bit weird because we are telling the "data model" about the
                // item that is to be selected in the UI. In a better world, the concept of "selection" would
                // be in the UI logic, not the data model.
                locationModel.IsSelected = true;
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
            ((IVsWindowFrame)this.Frame).Show();
        }

        public void Close()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ((IVsWindowFrame)this.Frame).CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
        }

        public void UpdateSelectionList(params object[] items)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this._selectionContainer = new SelectionContainer(selectableReadOnly: true, selectedReadOnly: false)
            {
                SelectableObjects = items,
                SelectedObjects = items,
            };

            // This is null until the control is loaded.
            this._trackSelection?.OnSelectChange(this._selectionContainer);
        }

        /// <summary>
        /// Reset the Properties window to display the properties of the selected Error List item.
        /// </summary>
        public void ResetSelection()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.UpdateSelectionList(this.Control?.DataContext);
        }

        private void UpdateDataContext(SarifErrorListItem sarifErrorListItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.Control.DataContext is SarifErrorListItem previousSarifErrorListItem)
            {
                previousSarifErrorListItem.Disposed -= this.SarifErrorListItem_Disposed;
            }

            // Resetting the data context to null causes the correct tab in the SARIF explorer
            // window to be selected when the data context is set back to a non-null value.
            this.Control.DataContext = null;
            this.Control.DataContext = sarifErrorListItem;

            if (sarifErrorListItem != null)
            {
                sarifErrorListItem.Disposed += this.SarifErrorListItem_Disposed;
            }

            this.UpdateSelectionList(sarifErrorListItem);
        }

        private void SarifErrorListItem_Disposed(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.Control.DataContext == sender)
            {
                this.UpdateDataContext(sarifErrorListItem: null);
            }
        }

        /// <summary>
        /// Returns the instance of the SARIF tool window.
        /// </summary>
        /// <returns>The object of SARIF tool window.</returns>
        public static SarifExplorerWindow Find()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) is IVsShell vsShell))
            {
                return null;
            }

            IVsPackage package;
            if (vsShell.IsPackageLoaded(SarifViewerPackage.PackageGuid, out package) != VSConstants.S_OK &&
                vsShell.LoadPackage(SarifViewerPackage.PackageGuid, out package) != VSConstants.S_OK)
            {
                return null;
            }

            if (!(package is Package vsPackage))
            {
                return null;
            }

            return vsPackage.FindToolWindow(typeof(SarifExplorerWindow), 0, true) as SarifExplorerWindow;
        }
    }
}
