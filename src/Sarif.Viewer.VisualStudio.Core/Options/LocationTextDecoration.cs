// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Sarif.Viewer.Options
{
    public class LocationTextDecoration
    {
        private int selectedIndex;

        public string Key { get; }

        public string Label => Resources.ResourceManager.GetString($"OptionsDialog_Colors_DecorationLabel_{this.Key}");

        public ObservableCollection<ColorOption> ColorOptions { get; private set; }

        /// <summary>
        /// Gets or sets the index of the highlight color in the dropdown menu. 0 indexed.
        /// </summary>
        public int SelectedIndex
        {
            get => this.selectedIndex;
            set
            {
                if (value != this.selectedIndex)
                {
                    this.selectedIndex = value;
                    SelectedColorChanged?.Invoke(new SelectedColorChangedEventArgs()
                    {
                        ErrorType = Key,
                        NewIndex = this.selectedIndex,
                    });
                }
            }
        }

        public ColorOption SelectedColorOption => this.ColorOptions[this.SelectedIndex];

        /// <summary>
        /// This event is triggered whenever the selected color changes.
        /// </summary>
        public event SelectedColorChangedEventHandler SelectedColorChanged;

        public delegate void SelectedColorChangedEventHandler(SelectedColorChangedEventArgs e);

        public LocationTextDecoration(string name, int selectedIndex)
        {
            // Assume name is unique.
            this.Key = name;
            this.SelectedIndex = selectedIndex;
        }

        public void SetColorOptions(List<ColorOption> colorOptions)
        {
            this.ColorOptions = new ObservableCollection<ColorOption>(colorOptions);
        }
    }
}
