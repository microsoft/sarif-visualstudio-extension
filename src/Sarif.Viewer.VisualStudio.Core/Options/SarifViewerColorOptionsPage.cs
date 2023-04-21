// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Options
{
    [ComVisible(true)]
    public class SarifViewerColorOptionsPage : UIElementDialogPage
    {
        private readonly Lazy<SarifViewerColorOptionsControl> _sarifViewerColorOptionsControl;

        private readonly List<ColorOption> colorOptions = new List<ColorOption>
        {
            new ColorOption("Purple", "Purple squiggle"),
            new ColorOption("Green", "Green squiggle"),
            new ColorOption("Gray", "Gray ellipsis (...)"),
            new ColorOption("Red", "Red squiggle"),
            new ColorOption("Teal", "Blue squiggle"),
            new ColorOption("Transpasrent", "Nothing"),
        };

        private readonly LocationTextDecorationCollection locationTextDecorations;

        public LocationTextDecorationCollection LocationTextDecorations => this.locationTextDecorations;

        /// <summary>
        /// This event is triggered whenever the rank filter value or the Insights formatting changes.
        /// </summary>
        public event InsightSettingsChangedEventHandler InsightSettingsChanged;

        public delegate void InsightSettingsChangedEventHandler(EventArgs e);

        public SarifViewerColorOptionsPage()
        {
            this.locationTextDecorations = new LocationTextDecorationCollection(this.colorOptions);
            this.locationTextDecorations.SelectedColorChanged += this.SelectedDecorationColorChanged;

            this.locationTextDecorations.Add(new LocationTextDecoration("ErrorUnderline", 0));
            this.locationTextDecorations.Add(new LocationTextDecoration("WarningUnderline", 1));
            this.locationTextDecorations.Add(new LocationTextDecoration("NoteUnderline", 2));

            _sarifViewerColorOptionsControl = new Lazy<SarifViewerColorOptionsControl>(() => new SarifViewerColorOptionsControl(this));
        }

        public ColorOption GetSelectedColorOption(string decorationName)
        {
            return this.LocationTextDecorations.Decorations.Where(d => d.Key == decorationName).First().SelectedColorOption;
        }

        private void SelectedDecorationColorChanged(SelectedColorChangedEventArgs e)
        {
            InsightSettingsChanged?.Invoke(new EventArgs());
        }

        /// <summary>
        /// Gets the Windows Presentation Foundation (WPF) child element to be hosted inside the Options dialog page.
        /// </summary>
        /// <returns>The WPF child element.</returns>
        protected override UIElement Child => _sarifViewerColorOptionsControl.Value;

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
