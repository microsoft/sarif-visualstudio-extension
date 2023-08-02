// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.Sarif.Viewer.Options
{
    [ComVisible(true)]
    public class SarifViewerColorOptionsPage : UIElementDialogPage
    {
        private readonly Lazy<SarifViewerColorOptionsControl> _sarifViewerColorOptionsControl;

        private readonly List<ColorOption> colorOptions = new List<ColorOption>
        {
            new ColorOption("Purple", "Purple squiggle", PredefinedErrorTypeNames.OtherError),
            new ColorOption("Green", "Green squiggle", PredefinedErrorTypeNames.Warning),
            new ColorOption("Gray", "Gray ellipsis (...)", PredefinedErrorTypeNames.HintedSuggestion),
            new ColorOption("Red", "Red squiggle", PredefinedErrorTypeNames.SyntaxError),
            new ColorOption("Teal", "Blue squiggle", PredefinedErrorTypeNames.CompilerError),
            new ColorOption("Transpasrent", "Nothing", PredefinedErrorTypeNames.Suggestion),
        };

        public const string ErrorUnderlineString = "ErrorUnderline";
        public const string WarningUnderlineString = "WarningUnderline";
        public const string NoteUnderlineString = "NoteUnderline";

        private LocationTextDecorationCollection locationTextDecorations;

        public LocationTextDecorationCollection LocationTextDecorations => this.locationTextDecorations;

        public ObservableCollection<LocationTextDecoration> Decorations => this.locationTextDecorations.Decorations;

        /// <summary>
        /// This event is triggered whenever the rank filter value or the Insights formatting changes.
        /// </summary>
        public event InsightSettingsChangedEventHandler InsightSettingsChanged;

        public delegate void InsightSettingsChangedEventHandler(EventArgs e);

        public int ErrorUnderlineColorIndex { get; set; } = 0;

        public int WarningUnderlineColorIndex { get; set; } = 1;

        public int NoteUnderlineColorIndex { get; set; } = 2;

        public SarifViewerColorOptionsPage()
        {
            _sarifViewerColorOptionsControl = new Lazy<SarifViewerColorOptionsControl>(() => new SarifViewerColorOptionsControl(this));
        }

        public override void LoadSettingsFromStorage()
        {
            base.LoadSettingsFromStorage();
            SetLocationTextDecorations();
        }

        /// <summary>
        /// Resets the location text decorations.
        /// </summary>
        private void SetLocationTextDecorations()
        {
            if (this.locationTextDecorations == null)
            {
                this.locationTextDecorations = new LocationTextDecorationCollection(this.colorOptions);
                this.locationTextDecorations.SelectedColorChanged += this.SelectedDecorationColorChanged;

                this.locationTextDecorations.Add(new LocationTextDecoration(ErrorUnderlineString, ErrorUnderlineColorIndex));
                this.locationTextDecorations.Add(new LocationTextDecoration(WarningUnderlineString, WarningUnderlineColorIndex));
                this.locationTextDecorations.Add(new LocationTextDecoration(NoteUnderlineString, NoteUnderlineColorIndex));
            }
        }

        public ColorOption GetSelectedColorOption(string decorationName)
        {
            return this.LocationTextDecorations.Decorations.Where(d => d.Key == decorationName).First().SelectedColorOption;
        }

        private void SelectedDecorationColorChanged(SelectedColorChangedEventArgs e)
        {
            switch (e.ErrorType)
            {
                case ErrorUnderlineString:
                    this.ErrorUnderlineColorIndex = e.NewIndex;
                    break;
                case WarningUnderlineString:
                    this.WarningUnderlineColorIndex = e.NewIndex;
                    break;
                case NoteUnderlineString:
                    this.NoteUnderlineColorIndex = e.NewIndex;
                    break;
                default:
                    throw new Exception($"Unknown error type {e.ErrorType} seen");
            }

            InsightSettingsChanged?.Invoke(e);
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
