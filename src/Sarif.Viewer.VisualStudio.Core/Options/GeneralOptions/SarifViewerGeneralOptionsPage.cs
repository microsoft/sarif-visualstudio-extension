// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;

using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;

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

        public int DevCanvasServerIndex { get; set; } = 0;

        public bool? DevCanvasLoggedIn { get; set; } = null;

        /// <summary>
        /// Gets the message that the login button shows.
        /// Empty when undecided, "Log out" when logged in, "Log in" when logged out.
        /// </summary>
        public string DevCanvasLoginButtonMessage
        {
            get
            {
                if (DevCanvasLoggedIn == null)
                {
                    return string.Empty;
                }

                bool devCanvasLoggedInNonNull = (bool)DevCanvasLoggedIn;
                if (devCanvasLoggedInNonNull)
                {
                    return "Log out of DevCanvas";
                }
                else
                {
                    return "Log into DevCanvas";
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the user should be able to see the devcanvs settings. Not done for security reasons but for UI/UX reasons.
        /// </summary>
        public string CanSeeDevCanvas
        {
            get
            {
                string userName = (string)Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\VSCommon\\ConnectedUser\\IdeUserV4\\Cache", "EmailAddress", null);
                if (userName == null || !userName.EndsWith("@microsoft.com"))
                {
                    return "Hidden";
                }

                return "Visible";
            }
        }

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
