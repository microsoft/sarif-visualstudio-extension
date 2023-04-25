// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Sarif.Viewer.Options
{
    public class LocationTextDecorationCollection
    {
        public List<ColorOption> ColorOptions { get; }

        public ObservableCollection<LocationTextDecoration> Decorations { get; }

        /// <summary>
        /// This event is triggered whenever the selected color changes.
        /// </summary>
        public event SelectedColorChangedEventHandler SelectedColorChanged;

        public delegate void SelectedColorChangedEventHandler(SelectedColorChangedEventArgs e);

        public string TestProperty => "Test";

        public LocationTextDecorationCollection(List<ColorOption> colorOptions)
        {
            this.Decorations = new ObservableCollection<LocationTextDecoration>();
            this.ColorOptions = colorOptions;
        }

        public void Add(LocationTextDecoration item)
        {
            if (item != null)
            {
                item.SetColorOptions(this.ColorOptions);
                this.Decorations.Add(item);
                item.SelectedColorChanged += this.SelectedDecorationColorChanged;
            }
        }

        private void SelectedDecorationColorChanged(SelectedColorChangedEventArgs e)
        {
            SelectedColorChanged?.Invoke(e);
        }
    }
}
