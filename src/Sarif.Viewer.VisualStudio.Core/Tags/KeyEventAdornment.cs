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
        private static readonly Color FontColor = Color.FromArgb(0x70, 210, 153, 48);
        private static readonly Brush FontBrush = new SolidColorBrush(FontColor);

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

            this.FormatText(nodes, out string fullText, out string shortText, out string tooltipText);
            string prefix = new string(PrefixChar, prefixLength);

            this.Text = $" {prefix}{shortText}";
            this.Foreground = FontBrush;
            this.FontFamily = fontFamily;
            this.FontSize = fontSize;
            this.FontStyle = FontStyles.Italic;
            this.Width = 2000;

            if (fullText != shortText)
            {
                this.ToolTip = tooltipText;
                this.Cursor = Cursors.Arrow;
            }
        }

        private void FormatText(IList<AnalysisStepNode> nodes, out string fullText, out string concisedText, out string tooltipText)
        {
            var displayText = new StringBuilder();
            var hintText = new StringBuilder();

            // used to separate multiple key events, e.g.
            // --- Key Event 1 --- Key Event 2: message
            string separator = new string(PrefixChar, 3);

            foreach (AnalysisStepNode node in nodes)
            {
                displayText.Append($"{separator} Key Event {node.Index}")
                    .Append(node.Message != null ? $": {node.Message}" : string.Empty)
                    .Append(Environment.NewLine);

                hintText.Append($"Key Event {node.Index}")
                    .Append(node.Message != null ? $": {node.Message}" : string.Empty)
                    .Append(Environment.NewLine);
            }

            tooltipText = hintText.ToString();
            fullText = displayText.ToString().Replace("\r\n", " ").Replace("\n", " ");
            concisedText = fullText;
            if (concisedText.Length > MaxLength)
            {
                concisedText = concisedText.Substring(0, MaxLength) + " " + Ellipsis;
            }
        }
    }
}
