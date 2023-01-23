// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal class KeyEventAdornment : TextBlock
    {
        private const char PrefixChar = '-';
        private const string Ellipsis = "\u2026";
        private const int MaxLength = 100;

        private readonly int prefixLength;
        private readonly RoutedEventHandler routedEventHandler;

        public KeyEventAdornment(IList<ITextMarkerTag> tags, int prefixLength, double fontSize, FontFamily fontFamily, RoutedEventHandler clickHandler)
        {
            this.prefixLength = prefixLength;
            this.routedEventHandler = clickHandler;

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

            this.FormatText(nodes, prefixLength, out string fullText, out string shortText, out string tooltipText, out List<Inline> inlines);

            // prefixLength is calculated by (longest length of lines have tag) - (current line lenght)
            // the prefix let the all key event text start locations align at same column
            string prefix = new string(PrefixChar, prefixLength);

            // List<Inline> inlines = SdkUIUtilities.GetMessageInlines(fullText, clickHandler, this.ToDict(nodes.First().State));
            if (inlines?.Any() == true)
            {
                this.Inlines.AddRange(inlines);
            }
            else
            {
                this.Inlines.Add(fullText);
            }

            /*
            this.Inlines.Add($" {prefix}{shortText}");

            Hyperlink hyperLink = new Hyperlink()
            {
                Tag = nodes.Max(n => n.Index) + 1,
            };
            hyperLink.Inlines.Add("▶");

            if (clickHandler != null)
            {
                hyperLink.Click += clickHandler;
            }

            this.Inlines.Add(hyperLink);
            */

            this.FontFamily = fontFamily;
            this.FontSize = fontSize;
            this.FontStyle = FontStyles.Italic;
            this.SetResourceReference(
                TextBlock.ForegroundProperty,
                EnvironmentColors.ExtensionManagerStarHighlight2BrushKey);

            // this.ToolTip = SdkUIUtilities.EscapeHyperlinks(tooltipText);
            this.Cursor = Cursors.Arrow;
        }

        internal void Update(IList<ITextMarkerTag> textMarkerTag)
        {
            if (textMarkerTag is null)
            {
                throw new ArgumentNullException(nameof(textMarkerTag));
            }

            // rect.Fill = MakeBrush(colorTag.Color);
        }

        private void FormatText(IList<AnalysisStepNode> nodes, int prefixLength, out string fullText, out string conciseText, out string tooltipText, out List<Inline> inlines)
        {
            var displayText = new StringBuilder();
            var hintText = new StringBuilder();

            inlines = new List<Inline>();

            // used to separate multiple key events, e.g.
            // --- Key Event 1 --- Key Event 2: message
            string separator = new string(PrefixChar, 3);

            foreach (AnalysisStepNode node in nodes)
            {
                string nodeString = CreateKeyEventText(node.Index, node.Message, prefixLength + 3) + Environment.NewLine;
                displayText.Append(nodeString);
                hintText.Append(CreateKeyEventText(node.Index, node.Message, 0));
                inlines.AddRange(SdkUIUtilities.GetMessageInlines(nodeString, this.routedEventHandler, node));
            }

            tooltipText = hintText.ToString();
            fullText = displayText.ToString(); // ReplaceLineBreaker(displayText.ToString(), " ");
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

        private static string CreateKeyEventText(int index, string message, int prefixLength)
        {
            string prefix = prefixLength > 0 ?
                $" {new string(PrefixChar, prefixLength)} Step {index} : " :
                string.Empty;
            prefixLength = prefix.Length;

            message ??= string.Empty;
            if (!string.IsNullOrEmpty(message))
            {
                // prefixLength += 3;
            }

            string[] lines = message.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                builder.Append(i == 0 ? prefix : new string(' ', prefixLength));
                builder.Append(lines[i]);
                builder.Append(i == lines.Length - 1 ? string.Empty : Environment.NewLine);
            }

            return builder.ToString();
        }
    }

    public class LinkTag
    {
        public int Index { get; set; }

        public string StateKey { get; set; }

        public bool Forward { get; set; }
    }
}
