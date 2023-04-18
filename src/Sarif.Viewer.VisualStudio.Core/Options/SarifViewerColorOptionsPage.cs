// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Options
{
    [ComVisible(true)]
    public class SarifViewerColorOptionsPage : UIElementDialogPage
    {
        private int _errorUnderlineColorIndex = 0;
        private int _warningUnderlineColorIndex = 1;
        private int _noteUnderlineColorIndex = 2;

        private readonly Lazy<SarifViewerColorOptionsControl> _sarifViewerColorOptionsControl;

        public SarifViewerColorOptionsPage()
        {
            _sarifViewerColorOptionsControl = new Lazy<SarifViewerColorOptionsControl>(() => new SarifViewerColorOptionsControl(this));
        }

        /// <summary>
        /// This event is triggered whenever the rank filter value or the Insights formatting changes.
        /// </summary>
        public event InsightSettingsChangedEventHandler InsightSettingsChanged;

        public delegate void InsightSettingsChangedEventHandler(string setting, object oldValue, object newValue);

        /// <summary>
        /// Gets or sets the index representing what an error needs to be underlined as.
        /// </summary>
        public int ErrorUnderlineColorIndex
        {
            get
            {
                return _errorUnderlineColorIndex;
            }

            set
            {
                if (value != _errorUnderlineColorIndex)
                {
                    int oldValue = _errorUnderlineColorIndex;
                    _errorUnderlineColorIndex = value;
                    InsightSettingsChanged?.Invoke(nameof(ErrorUnderlineColorIndex), oldValue, _errorUnderlineColorIndex);
                }
            }
        }

        /// <summary>
        /// Gets or sets the index representing what a warning needs to be underlined as.
        /// </summary>
        public int WarningUnderlineColorIndex
        {
            get
            {
                return _warningUnderlineColorIndex;
            }

            set
            {
                if (value != _warningUnderlineColorIndex)
                {
                    int oldValue = _warningUnderlineColorIndex;
                    _warningUnderlineColorIndex = value;
                    InsightSettingsChanged?.Invoke(nameof(WarningUnderlineColorIndex), oldValue, _warningUnderlineColorIndex);
                }
            }
        }

        /// <summary>
        /// Gets or sets the index representing what a note needs to be underlined as.
        /// </summary>
        public int NoteUnderlineColorIndex
        {
            get
            {
                return _noteUnderlineColorIndex;
            }

            set
            {
                if (value != _noteUnderlineColorIndex)
                {
                    int oldValue = _noteUnderlineColorIndex;
                    _noteUnderlineColorIndex = value;
                    InsightSettingsChanged?.Invoke(nameof(NoteUnderlineColorIndex), oldValue, _noteUnderlineColorIndex);
                }
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
                return _sarifViewerColorOptionsControl.Value;
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
