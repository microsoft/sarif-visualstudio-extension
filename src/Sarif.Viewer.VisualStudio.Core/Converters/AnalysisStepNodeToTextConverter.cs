// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows.Data;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Converters
{
    internal class AnalysisStepNodeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var node = value as AnalysisStepNode;
            return node != null ?
                MakeDisplayString(node) :
                string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

        public static string MakeDisplayString(AnalysisStepNode node)
        {
            // Use the following preferences for the AnalysisStepNode text.
            // 1. ThreadFlowLocation.Location.Message.Text
            // 2. ThreadFlowLocation.Location.PhysicalLocation.Region.Snippet.Text
            // 3. "Continuing"
            string text = string.Empty;

            ThreadFlowLocation threadFlowLocation = node.Location;
            if (threadFlowLocation != null)
            {
                if (!string.IsNullOrWhiteSpace(threadFlowLocation.Location?.Message?.Text))
                {
                    text = threadFlowLocation.Location.Message.Text;
                }
                else if (!string.IsNullOrWhiteSpace(threadFlowLocation.Location?.PhysicalLocation?.Region?.Snippet?.Text))
                {
                    text = threadFlowLocation.Location.PhysicalLocation.Region.Snippet.Text.Trim();
                }
                else
                {
                    text = Resources.ContinuingAnalysisStepNodeMessage;
                }
            }

            return text;
        }
    }
}
