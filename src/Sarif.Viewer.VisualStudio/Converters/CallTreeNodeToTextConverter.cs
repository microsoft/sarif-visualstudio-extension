// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Converters
{
    public class CallTreeNodeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var node = value as CallTreeNode;
            return node != null ?
                MakeDisplayString(node) :
                string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

        private static string MakeDisplayString(CallTreeNode node)
        {
            // Use the following preferences for the CallTreeNode text.
            // 1. AnnotatedCodeLocation.Message
            // 2. AnnotatedCodeLocation.Snippet
            // 3. Callee for calls
            // 4. "Return" for returns
            // 5. AnnotatedCodeLocation.Kind
            string text = string.Empty;

            CodeFlowLocation codeFlowLocation = node.Location;
            if (codeFlowLocation != null)
            {
                if (!String.IsNullOrEmpty(codeFlowLocation.Location?.Message?.Text))
                {
                    text = codeFlowLocation.Location.Message.Text;
                }
                else if (!String.IsNullOrEmpty(codeFlowLocation.Location?.PhysicalLocation?.Region?.Snippet?.Text))
                {
                    text = codeFlowLocation.Location.PhysicalLocation.Region.Snippet.Text.Trim();
                }
                else
                {
                    switch (node.Kind)
                    {
                        case CallTreeNodeKind.Call:
                            string callee;
                            
                            text = codeFlowLocation.TryGetProperty("target", out callee) ?
                                        callee :
                                        Resources.UnknownCalleeMessage;
                            break;

                        case CallTreeNodeKind.Return:
                            text = Resources.ReturnMessage;
                            break;
                    }
                }
            }

            return text;
        }
    }
}
