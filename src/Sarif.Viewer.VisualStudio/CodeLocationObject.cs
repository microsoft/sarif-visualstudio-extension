// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.IO;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer
{
    internal abstract class CodeLocationObject : NotifyPropertyChangedObject
    {
        private Region _region;
        protected ResultTextMarker _lineMarker;
        protected string _filePath;
        protected string _uriBaseId;

        public CodeLocationObject(int resultId, int runIndex)
        {
            this.ResultId = resultId;
            this.RunIndex = runIndex;
            this.TypeDescriptor = new CodeLocationObjectTypeDescriptor(this);
        }

        /// <summary>
        /// Gets the result ID that uniquely identifies this result for this Visual Studio session.
        /// </summary>
        public int ResultId { get; }

        /// <summary>
        /// Gets the run index known to <see cref="CodeAnalysisResultManager"/>.
        /// </summary>
        public int RunIndex { get; }

        internal virtual ResultTextMarker LineMarker
        {
            get
            {
                if (this._lineMarker == null)
                {
                    this.RecreateLineMarker();
                }

                return this._lineMarker;
            }
        }

        public Region Region
        {
            get => this._region;

            set
            {
                if (value != this._region)
                {
                    this._region = value;

                    if (this._lineMarker != null)
                    {
                        this.RecreateLineMarker();
                    }

                    this.NotifyPropertyChanged();
                    this.NotifyPropertyChanged(nameof(this.RegionDisplayString));
                }
            }
        }

        public string RegionDisplayString =>

            // If startLine is zero, we haven't populated the region yet.
            // Since startLine is always part of this string, we avoid invalid strings like "(0)".
            this.Region?.StartLine > 0 ? this.Region.FormatForVisualStudio() : null;

        public virtual string FilePath
        {
            get => this._filePath;

            set
            {
                if (!string.Equals(value, this._filePath))
                {
                    this._filePath = value;

                    if (this._lineMarker != null)
                    {
                        this.RecreateLineMarker();
                    }

                    this.NotifyPropertyChanged();
                }
            }
        }

        internal virtual string UriBaseId
        {
            get => this._uriBaseId;

            set
            {
                if (!string.Equals(value, this._uriBaseId))
                {
                    this._uriBaseId = value;

                    if (this._lineMarker != null)
                    {
                        this.RecreateLineMarker();
                    }

                    this.NotifyPropertyChanged();
                }
            }
        }

        public virtual string DefaultSourceHighlightColor => ResultTextMarker.DEFAULT_SELECTION_COLOR;

        public virtual string SelectedSourceHighlightColor => ResultTextMarker.DEFAULT_SELECTION_COLOR;

        // This is a custom type descriptor which enables the SARIF properties
        // to be displayed in the Properties window.
        internal ICustomTypeDescriptor TypeDescriptor { get; }

        /// <summary>
        /// Attempts to navigate a VS editor to the text marker.
        /// </summary>
        /// <param name="usePreviewPane">Indicates whether to use VS's preview pane.</param>
        /// <param name="moveFocusToCaretLocation">Indicates whether to move focus to the caret location.</param>
        /// <returns>Returns true if a VS editor was opened.</returns>
        /// <remarks>
        /// The <paramref name="usePreviewPane"/> indicates whether Visual Studio opens the document as a preview (tab to the right)
        /// rather than as an "open code editor" (tab attached to other open documents on the left).
        /// </remarks>
        public bool NavigateTo(bool usePreviewPane, bool moveFocusToCaretLocation)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            if (this.LineMarker != null)
            {
                return this.LineMarker.NavigateTo(usePreviewPane, moveFocusToCaretLocation);
            }
            else
            {
                // The user clicked an in-line link with an integer target, which points to
                // a Location object that does NOT have a region associated with it.

                // Before anything else, see if this is an external link we should open in the browser.
                if (Uri.TryCreate(this.FilePath, UriKind.Absolute, out Uri uri))
                {
                    if (!uri.IsFile)
                    {
                        SdkUIUtilities.OpenExternalUrl(uri.OriginalString);
                        return true;
                    }
                }

                if (File.Exists(this.FilePath))
                {
                    return SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, this.FilePath, usePreviewPane) != null;
                }
                else if (CodeAnalysisResultManager.Instance.TryResolveFilePath(resultId: this.ResultId, runIndex: this.RunIndex, uriBaseId: this.UriBaseId, relativePath: this.FilePath, resolvedPath: out string resolvedFilePath))
                {
                    return SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, resolvedFilePath, usePreviewPane) != null;
                }
            }

            return false;
        }

        private void RecreateLineMarker()
        {
            if (this._lineMarker != null)
            {
                this._lineMarker.Dispose();
                this._lineMarker = null;
            }

            // Not all locations have regions. Don't try to mark the locations that don't.
            // PROBLEM: This means we can't double-click to open a file containing a result
            // without a region.
            if (this.Region != null)
            {
                this._lineMarker = new ResultTextMarker(
                    runIndex: this.RunIndex,
                    resultId: this.ResultId,
                    uriBaseId: this.UriBaseId,
                    region: this.Region,
                    fullFilePath: this.FilePath,
                    nonHghlightedColor: this.DefaultSourceHighlightColor,
                    highlightedColor: this.SelectedSourceHighlightColor,
                    context: this);
            }

            this.NotifyPropertyChanged(nameof(this.LineMarker));
        }
    }
}
