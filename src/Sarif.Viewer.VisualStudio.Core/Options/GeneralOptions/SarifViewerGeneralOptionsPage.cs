﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Options
{
    [ComVisible(true)]
    public class SarifViewerGeneralOptionsPage : UIElementDialogPage
    {
        private readonly Lazy<SarifViewerGeneralOptionsControl> _sarifViewerOptionsControl;

        public SarifViewerGeneralOptionsPage()
        {
            _sarifViewerOptionsControl = new Lazy<SarifViewerGeneralOptionsControl>(() => new SarifViewerGeneralOptionsControl(this));
        }

        public bool MonitorSarifFolder { get; set; } = true;

        public bool EnableGitHubAdvancedSecurity { get; set; } = false;

        public bool EnableKeyEventAdornment { get; set; } = true;

        public int SelectedSlot { get; set; } = 0;

        /// <summary>
        /// Gets the Windows Presentation Foundation (WPF) child element to be hosted inside the Options dialog page.
        /// </summary>
        /// <returns>The WPF child element.</returns>
        protected override UIElement Child
        {
            get
            {
                return _sarifViewerOptionsControl.Value;
            }
        }

        /// <summary>
        /// This occurs when the User selecting 'Ok' and right before the dialog page UI closes entirely.
        /// This override handles the case when the user types inside an editable combobox and
        /// immediately hits enter causing the window to close without firing the combobox LostFocus event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e); // Saves the user's changes.
        }

        /// <summary>
        /// This page is called when VS wants to activate this page.
        /// ie. when the user opens the tools options page.
        /// </summary>
        /// <param name="e">Cancellation event arguments.</param>
        protected override void OnActivate(CancelEventArgs e)
        {
            // The UI caches the settings even though the tools options page is closed.
            // This load call ensures we display data that was saved. This is to handle
            // the case when the user hits the cancel button and reloads the page.
            LoadSettingsFromStorage();

            base.OnActivate(e);
        }
    }
}
