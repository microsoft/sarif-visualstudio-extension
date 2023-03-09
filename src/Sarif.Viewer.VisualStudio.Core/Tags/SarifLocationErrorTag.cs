﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using Markdig.Wpf;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// Contains the data necessary to display a error tag (a underlined squiggle with a tool tip)
    /// inside Visual Studio's text views.
    /// </summary>
    internal class SarifLocationErrorTag : SarifLocationTagBase, IErrorTag
    {
        /// <summary>
        /// The default fontsize for text components.
        /// </summary>
        private const int FontSize = 16;

        /// <summary>
        /// The color for text. Changes based on user theme.
        /// </summary>
        private static readonly SolidColorBrush TextBrush = GetBrushFromThemeColor(EnvironmentColors.ToolWindowTextColorKey);

        /// <summary>
        /// Initializes a new instance of the <see cref="SarifLocationErrorTag"/> class.
        /// </summary>
        /// <param name="persistentSpan">The persistent span for the tag within a document.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="resultId">the result ID associated with this tag.</param>
        /// <param name="errorType">The Visual Studio error type to display.</param>
        /// <param name="content">The content to use when displaying a tool tip for this error. This parameter may be null.</param>
        /// <param name="context">Gets the data context for this tag.</param>
        public SarifLocationErrorTag(IPersistentSpan persistentSpan, int runIndex, int resultId, string errorType, List<(string strContent, TextRenderType renderType)> content, object context)
            : base(persistentSpan, runIndex: runIndex, resultId: resultId, context: context)
        {
            this.ErrorType = errorType;
            this.content = content;
        }

        /// <summary>
        /// Gets the Visual Studio error type to display.
        /// </summary>
        /// <remarks>
        /// The "error type" is basically the squiggle color. This may be null.
        /// </remarks>
        public string ErrorType { get; }

        /// <summary>
        /// Gets the content to use when displaying a tool tip for this error.
        /// </summary>
        /// <remarks>
        /// This may be null.
        /// </remarks>
        private readonly List<(string strContent, TextRenderType renderType)> content;

        public object ToolTipContent
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                foreach ((string strContent, TextRenderType renderType) item in content)
                {
                    if (item.renderType == TextRenderType.Markdown)
                    {
                        try
                        {
                            MarkdownViewer viewer = new MarkdownViewer();
                            viewer.Markdown = item.strContent;
                            ParseBlocks(viewer.Document.Blocks);

                            viewer.Margin = new Thickness(-15, -15, 0, 0); // There is a small amount of padding that MarkdownViewer comes with that makes it awkward when a textfield is put alongside it.
                            return viewer;
                        }
                        catch (Exception)
                        {
                            // catch and swallow silently
                        }
                    }
                    else if (item.renderType == TextRenderType.Text)
                    {
                        TextBlock textblock = new TextBlock() { Text = item.strContent };
                        textblock.FontSize = FontSize;
                        textblock.Foreground = TextBrush;
                        return textblock;
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown text render type {item.renderType}");
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Parses blocks recursively, applying style and allowing for hyperlink redirects.
        /// </summary>
        /// <param name="blocks">A list of blocks to parse.</param>
        private void ParseBlocks(BlockCollection blocks)
        {
            foreach (Block block in blocks)
            {
                foreach (object blockChild in LogicalTreeHelper.GetChildren(block))
                {
                    if (blockChild is Hyperlink hyperlink)
                    {
                        SolidColorBrush hyperlinkBrush = GetBrushFromThemeColor(EnvironmentColors.PanelHyperlinkColorKey);
                        hyperlink.Foreground = hyperlinkBrush;
                        hyperlink.MouseDown += Block_MouseDown;
                    }
                    else if (blockChild is Run runBlock)
                    {
                        runBlock.Foreground = TextBrush;
                    }
                    else if (blockChild is ListItem listBlock)
                    {
                        ParseBlocks(listBlock.Blocks);
                    }
                }

                block.Foreground = TextBrush;
            }
        }

        /// <summary>
        /// Called when a hyperlink in the markdown is clicked.
        /// </summary>
        /// <param name="sender">The object that was clicked.</param>
        /// <param name="e">The event of being clicked.</param>
        private void Block_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Hyperlink hyperlink)
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo(hyperlink.NavigateUri.AbsoluteUri));
                e.Handled = true;
            }
        }

        /// <summary>
        /// Gets the brush color for a parcitcular resource from the VS color theme.
        /// </summary>
        /// <param name="themeResourceKey">The key to use to lookup from the currently set VS color theme.</param>
        /// <returns>A <see cref="SolidColorBrush"/> holding the required color.</returns>
        private static SolidColorBrush GetBrushFromThemeColor(ThemeResourceKey themeResourceKey)
        {
            System.Drawing.Color color = VSColorTheme.GetThemedColor(themeResourceKey);
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }
}
