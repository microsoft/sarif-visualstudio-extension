// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.ComponentModel;
using System.IO;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace Microsoft.Sarif.Viewer
{
    public abstract class CodeLocationObject : NotifyPropertyChangedObject
    {
        private Region _region;
        protected ResultTextMarker _lineMarker;
        protected string _filePath;
        protected string _uriBaseId;

        internal virtual ResultTextMarker LineMarker
        {
            get
            {
                // Not all locations have regions. Don't try to mark the locations that don't.
                //
                // PROBLEM: This means we can't double-click to open a file containing a result
                // without a region.
                if (_lineMarker == null && Region != null)
                {
                    _lineMarker = new ResultTextMarker(RunId, Region, FilePath);
                }

                // If the UriBaseId was populated before the marker was available, set the
                // marker's UriBaseId property now. The marker's UriBaseId is used to resolve
                // relative paths to absolute paths when the user double-clicks a result in
                // the Error List window.
                if (_lineMarker != null && UriBaseId != null)
                {
                    _lineMarker.UriBaseId = UriBaseId;
                }

                return _lineMarker;
            }
        }

        public Region Region
        {
            get
            {
                return this._region;
            }
            set
            {
                if (value != this._region)
                {
                    _region = value;

                    if (this._lineMarker != null)
                    {
                        this._lineMarker.Region = _region;
                    }

                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(this.RegionDisplayString));
                }
            }
        }

        public string RegionDisplayString
        {
            get
            {
                // If startLine is zero, we haven't populated the region yet.
                // Since startLine is always part of this string, we avoid invalid strings like "(0)".
                return Region != null && Region.StartLine > 0 ? Region.FormatForVisualStudio() : null;
            }
        }

        public virtual string FilePath
        {
            get
            {
                return this._filePath;
            }
            set
            {
                if (!string.Equals(value, this._filePath))
                {
                    this._filePath = value;

                    if (this._lineMarker != null)
                    {
                        this._lineMarker.FullFilePath = this._filePath;
                    }

                    NotifyPropertyChanged();
                }
            }
        }

        internal virtual string UriBaseId
        {
            get
            {
                return this._uriBaseId;
            }
            set
            {
                if (!string.Equals(value, this._uriBaseId))
                {
                    this._uriBaseId = value;

                    if (this._lineMarker != null)
                    {
                        this._lineMarker.UriBaseId = this._uriBaseId;
                    }

                    NotifyPropertyChanged();
                }
            }
        }

        public virtual string DefaultSourceHighlightColor
        {
            get
            {
                return ResultTextMarker.DEFAULT_SELECTION_COLOR;
            }
        }

        public virtual string SelectedSourceHighlightColor
        {
            get
            {
                return ResultTextMarker.DEFAULT_SELECTION_COLOR;
            }
        }

        // This is a custom type descriptor which enables the SARIF properties
        // to be displayed in the Properties window.
        internal ICustomTypeDescriptor TypeDescriptor
        {
            get
            {
                return new CodeLocationObjectTypeDescriptor(this);
            }
        }

        internal int RunId { get; }

        public CodeLocationObject()
        {
            RunId = CodeAnalysisResultManager.Instance.CurrentRunIndex;
        }

        public void NavigateTo(bool usePreviewPane = true)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally
            }

            if (LineMarker != null)
            {
                LineMarker.TryNavigateTo(usePreviewPane);
            }
            else
            {
                // The user clicked an inline link with an integer target, which points to
                // a Location object that does NOT have a region associated with it.

                // Before anything else, see if this is an external link we should open in the browser.
                if (Uri.TryCreate(this.FilePath, UriKind.Absolute, out Uri uri))
                {
                    if (!uri.IsFile)
                    {
                        System.Diagnostics.Process.Start(uri.OriginalString);
                        return;
                    }
                }

                if (!File.Exists(this.FilePath))
                {
                    CodeAnalysisResultManager.Instance.TryRebaselineAllSarifErrors(RunId, this.UriBaseId, this.FilePath);
                }

                if (File.Exists(this.FilePath))
                {
                    SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, this.FilePath, usePreviewPane);
                }
            }
        }

        public void ApplyDefaultSourceFileHighlighting()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Remove hover marker
            LineMarker?.RemoveTagHighlight();

            // Add default marker instead
            LineMarker?.AddTagHighlight(DefaultSourceHighlightColor);
        }

        /// <summary>
        /// A method for handling the event when this object is selected
        /// </summary>
        public void ApplySelectionSourceFileHighlighting()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Remove previous highlighting and replace with hover color
            LineMarker?.RemoveTagHighlight();
            LineMarker?.AddTagHighlight(SelectedSourceHighlightColor);
        }

        /// <summary>
        /// An overridden method for reacting to the event of a document window
        /// being opened
        /// </summary>
        internal void AttachToDocument(ITextBuffer textBuffer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            LineMarker?.TryTagDocument(textBuffer);
        }
    }
}
