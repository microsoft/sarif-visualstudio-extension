﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Sarif.Viewer.Converters
{
    public class CollectionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var collection = value as ICollection;
            int val;

            if (collection != null && int.TryParse(parameter.ToString(), out val))
            {
                return collection.Count > val ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CollectionToInvertedVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var collection = value as ICollection;
            int val;

            if (collection != null && int.TryParse(parameter.ToString(), out val))
            {
                return collection.Count > val ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
