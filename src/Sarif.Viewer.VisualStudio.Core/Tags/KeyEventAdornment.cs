// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal class KeyEventAdornment : TextBlock
    {
        private const char PrefixChar = '-';
        private const string Ellipsis = "\u2026";
        private const int MaxLength = 100;

        public KeyEventAdornment(IList<ITextMarkerTag> tags, int prefixLength, double fontSize, FontFamily fontFamily, RoutedEventHandler clickHandler)
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

            FormatText(nodes, prefixLength, out string fullText, out string shortText, out string tooltipText);

            // prefixLength is calculated by (longest length of lines have tag) - (current line lenght)
            // the prefix let the all key event text start locations align at same column
            string prefix = new string(PrefixChar, prefixLength);

            List<Inline> inlines = SdkUIUtilities.GetMessageInlines(fullText, clickHandler, this.ToDict(nodes.First().State));
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

            this.ToolTip = SdkUIUtilities.EscapeHyperlinks(tooltipText);
            this.Cursor = Cursors.Arrow;
        }

        internal void Update(ITextMarkerTag textMarkerTag)
        {
            if (textMarkerTag is null)
            {
                throw new ArgumentNullException(nameof(textMarkerTag));
            }

            // rect.Fill = MakeBrush(colorTag.Color);
        }

        internal IDictionary<string, string> ToDict(IList<AnalysisStepState> list)
        {
            var result = new Dictionary<string, string>();
            foreach (AnalysisStepState state in list)
            {
                result[state.Expression] = state.Value;
            }

            return result;
        }

        private static void FormatText(IList<AnalysisStepNode> nodes, int prefixLength, out string fullText, out string conciseText, out string tooltipText)
        {
            var displayText = new StringBuilder();
            var hintText = new StringBuilder();

            // used to separate multiple key events, e.g.
            // --- Key Event 1 --- Key Event 2: message
            string separator = new string(PrefixChar, 3);

            foreach (AnalysisStepNode node in nodes)
            {
                displayText.Append(CreateKeyEventText(node.Index, node.Message, prefixLength + 3));
                hintText.Append(CreateKeyEventText(node.Index, node.Message, 0));
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
}
