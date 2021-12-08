// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

using FluentAssertions;

using Microsoft.Sarif.Viewer.Converters;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.Converters.UnitTests
{
    public class CollectionToVisibilityConverterTests
    {
        [Fact]
        public void CollectionToVisibilityConverter_CollectionHasElements()
        {
            var testList = new List<int>();
            testList.Add(1);
            int threshold = 0;

            VerifyConversion(testList, threshold, Visibility.Visible);

            testList.Add(2);
            testList.Add(3);
            VerifyConversion(testList, threshold, Visibility.Visible);
        }

        [Fact]
        public void CollectionToVisibilityConverter_CompareToThreshold()
        {
            var testList = new List<int>() { 1, 2, 3 };
            int threshold = 5;

            VerifyConversion(testList, threshold, Visibility.Collapsed);

            threshold = 3;
            VerifyConversion(testList, threshold, Visibility.Collapsed);

            threshold = 2;
            VerifyConversion(testList, threshold, Visibility.Visible);
        }

        [Fact]
        public void CollectionToVisibilityConverter_NonEmptyCollection()
        {
            IEnumerable<int> testList = null;
            int threshold = 0;

            VerifyConversion(testList, threshold, Visibility.Collapsed);

            testList = Enumerable.Empty<int>();
            VerifyConversion(testList, threshold, Visibility.Collapsed);
        }

        [Fact]
        public void CollectionToVisibilityConverter_InvalidThreshold()
        {
            var testList = new List<int>() { 1, 2, 3 };
            bool threshold = false;

            VerifyConversion(testList, threshold, Visibility.Collapsed);
        }

        private static void VerifyConversion(IEnumerable<int> list, object threshold, Visibility expectedResult)
        {
            var converter = new CollectionToVisibilityConverter();
            var result = (Visibility)converter.Convert(list, typeof(List<int>), threshold, CultureInfo.CurrentCulture);
            result.Should().Be(expectedResult);

            Visibility revertedResult = expectedResult switch
            {
                Visibility.Visible => Visibility.Collapsed,
                Visibility.Collapsed => Visibility.Visible,
                _ => throw new Exception(),
            };
            var revertedConverter = new CollectionToInvertedVisibilityConverter();
            result = (Visibility)revertedConverter.Convert(list, typeof(List<int>), threshold, CultureInfo.CurrentCulture);
            result.Should().Be(revertedResult);
        }
    }
}
