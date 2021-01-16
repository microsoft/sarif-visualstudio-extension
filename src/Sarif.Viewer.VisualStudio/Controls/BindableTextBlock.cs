// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Microsoft.Sarif.Viewer.Controls
{
    public class BindableTextBlock : TextBlock
    {
        public static readonly DependencyProperty InlineListProperty =
            DependencyProperty.Register("InlineList", typeof(ObservableCollection<Inline>), typeof(BindableTextBlock), new UIPropertyMetadata(null, OnPropertyChanged));

        public ObservableCollection<Inline> InlineList
        {
            get { return (ObservableCollection<Inline>)this.GetValue(InlineListProperty); }
            set { this.SetValue(InlineListProperty, value); }
        }

        private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e != null && e.NewValue != null)
            {
                var textBlock = sender as BindableTextBlock;
                var list = e.NewValue as ObservableCollection<Inline>;
                textBlock.Inlines.Clear();
                textBlock.Inlines.AddRange(list);
            }
        }
    }
}
