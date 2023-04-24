// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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

        /// <summary>
        /// This event is triggered whenever the rank filter value or the Insights formatting changes.
        /// </summary>
        public event InsightSettingsChangedEventHandler InsightSettingsChanged;

        public delegate void InsightSettingsChangedEventHandler(EventArgs e);

        private int _errorUnderlineColorIndex = 0;

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
                    _errorUnderlineColorIndex = value;

                    // We need to reset the location text decorations due to the index settings being loaded in at aribtrary time, meaning it can be loaded in after the location text decorations are initialized.
                    // SetLocationTextDecorations();
                }
            }
        }

        private int _warningUnderlineColorIndex = 1;

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
                    _warningUnderlineColorIndex = value;

                    // We need to reset the location text decorations due to the index settings being loaded in at aribtrary time, meaning it can be loaded in after the location text decorations are initialized.
                    // SetLocationTextDecorations();
                }
            }
        }

        private int _noteUnderlineColorIndex = 2;

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
                    _noteUnderlineColorIndex = value;

                    // We need to reset the location text decorations due to the index settings being loaded in at aribtrary time, meaning it can be loaded in after the location text decorations are initialized.
                    // SetLocationTextDecorations();
                }
            }
        }

        public SarifViewerColorOptionsPage()
        {
            LoadSettingsFromStorage();
            SetLocationTextDecorations();
            _sarifViewerColorOptionsControl = new Lazy<SarifViewerColorOptionsControl>(() => new SarifViewerColorOptionsControl(this));
        }

        /// <summary>
        /// Resets the location text decorations.
        /// </summary>
        private void SetLocationTextDecorations()
        {
            if (this.locationTextDecorations != null)
            {
                this.locationTextDecorations.SelectedColorChanged -= this.SelectedDecorationColorChanged;
            }

            this.locationTextDecorations = new LocationTextDecorationCollection(this.colorOptions);
            this.locationTextDecorations.SelectedColorChanged += this.SelectedDecorationColorChanged;

            this.locationTextDecorations.Add(new LocationTextDecoration(ErrorUnderlineString, ErrorUnderlineColorIndex));
            this.locationTextDecorations.Add(new LocationTextDecoration(WarningUnderlineString, WarningUnderlineColorIndex));
            this.locationTextDecorations.Add(new LocationTextDecoration(NoteUnderlineString, NoteUnderlineColorIndex));
        }

        public ColorOption GetSelectedColorOption(string decorationName)
        {
            return this.LocationTextDecorations.Decorations.Where(d => d.Key == decorationName).First().SelectedColorOption;
        }

        private void SelectedDecorationColorChanged(SelectedColorChangedEventArgs e)
        {
            if (e.ErrorType == ErrorUnderlineString)
            {
                this.ErrorUnderlineColorIndex = e.NewIndex;
            }
            else if (e.ErrorType == WarningUnderlineString)
            {
                this.WarningUnderlineColorIndex = e.NewIndex;
            }
            else if (e.ErrorType == NoteUnderlineString)
            {
                this.NoteUnderlineColorIndex = e.NewIndex;
            }
            else
            {
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

            // SetLocationTextDecorations();

            base.OnActivate(e);
        }
    }
}
