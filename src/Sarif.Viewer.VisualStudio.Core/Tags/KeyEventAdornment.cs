// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal class KeyEventAdornment : TextBlock
    {
        private const char PrefixChar = '-';
        private const string Ellipsis = "\u2026";
        private const int MaxLength = 100;

        public KeyEventAdornment(IList<ITextMarkerTag> tags, int prefixLength, double fontSize, FontFamily fontFamily)
        {
            List<AnalysisStepNode> nodes = new List<AnalysisStepNode>();
            foreach (ITextMarkerTag tag in tags)
            {
                if (!(tag is SarifLocationTextMarkerTag sarifTag) || !(sarifTag.Context is AnalysisStepNode stepNode))
                {
                    // ignore if not a Sarif AnalysisStepNode tag
                    continue;
                }

                nodes.Add(stepNode);
            }

            nodes.Sort((a, b) => a.Index.CompareTo(b.Index));

            FormatText(nodes, out string fullText, out string shortText, out string tooltipText);

            // prefixLength is calculated by (longest length of lines have tag) - (current line lenght)
            // the prefix let the all key event text start locations align at same column
            string prefix = new string(PrefixChar, prefixLength);

            this.Text = $" {prefix}{shortText}";
            this.FontFamily = fontFamily;
            this.FontSize = fontSize;
            this.FontStyle = FontStyles.Italic;
            this.SetResourceReference(
                TextBlock.ForegroundProperty,
                Microsoft.VisualStudio.PlatformUI.EnvironmentColors.SmartTagFillBrushKey);

            if (fullText != shortText)
            {
                this.ToolTip = tooltipText;
                this.Cursor = Cursors.Arrow;
            }
        }

        private static void FormatText(IList<AnalysisStepNode> nodes, out string fullText, out string conciseText, out string tooltipText)
        {
            var displayText = new StringBuilder();
            var hintText = new StringBuilder();

            // used to separate multiple key events, e.g.
            // --- Key Event 1 --- Key Event 2: message
            string separator = new string(PrefixChar, 3);

            foreach (AnalysisStepNode node in nodes)
            {
                displayText.Append(CreateKeyEventText(node.Index, node.Message, separator));
                hintText.Append(CreateKeyEventText(node.Index, node.Message));
            }

            tooltipText = hintText.ToString();
            fullText = ReplaceLineBreaker(displayText.ToString(), " ");
            conciseText = GetConciseText(fullText, MaxLength);
        }

        private static string GetConciseText(string input, int maxStringLength)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            if (input.Length > maxStringLength)
            {
                input = $"{input.Substring(0, maxStringLength)} {Ellipsis}";
            }

            return input;
        }

        private static string ReplaceLineBreaker(string input, string newValue)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return input.Replace("\r\n", newValue).Replace("\n", newValue);
        }

        private static string CreateKeyEventText(int index, string message, string prefix = null)
        {
            prefix ??= string.Empty;
            if (!string.IsNullOrEmpty(prefix))
            {
                // Add a space between prefix and first char of the sentence.
                prefix += " ";
            }

            message ??= string.Empty;
            if (!string.IsNullOrEmpty(message))
            {
                message = $" : {message}";
            }

            return $"{prefix}Step {index}{message}{Environment.NewLine}";
        }
    }
}
