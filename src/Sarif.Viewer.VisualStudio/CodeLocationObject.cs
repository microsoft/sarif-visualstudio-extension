// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.ComponentModel;
using System.IO;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
                if (_lineMarker == null && Region != null)
                {
                    _lineMarker = new ResultTextMarker(RunId, Region, FilePath);
                }

                return _lineMarker;
            }
        }

        public Region Region
        {
            get
            {
                return _region;
            }
            set
            {
                if (value != _region)
                {
                    _region = value;

                    if (LineMarker != null)
                    {
                        LineMarker.Region = _region;
                    }

                    NotifyPropertyChanged("Region");
                    NotifyPropertyChanged("RegionDisplayString");
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
                return _filePath;
            }
            set
            {
                if (value != _filePath)
                {
                    _filePath = value;

                    if (this.LineMarker != null)
                    {
                        this.LineMarker.FullFilePath = _filePath;
                    }

                    NotifyPropertyChanged("FilePath");
                }
            }
        }

        internal virtual string UriBaseId
        {
            get
            {
                return _uriBaseId;
            }
            set
            {
                if (value != _uriBaseId)
                {
                    _uriBaseId = value;

                    if (this.LineMarker != null)
                    {
                        this.LineMarker.UriBaseId = _uriBaseId;
                    }

                    NotifyPropertyChanged("UriBaseId");
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
            RunId = CodeAnalysisResultManager.Instance.CurrentRunId;
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
                LineMarker?.NavigateTo(usePreviewPane);
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
            // Remove hover marker
            LineMarker?.RemoveHighlightMarker();

            // Add default marker instead
            LineMarker?.AddHighlightMarker(DefaultSourceHighlightColor);
        }

        /// <summary>
        /// A method for handling the event when this object is selected
        /// </summary>
        public void ApplySelectionSourceFileHighlighting()
        {
            // Remove previous highlighting and replace with hover color
            LineMarker?.RemoveHighlightMarker();
            LineMarker?.AddHighlightMarker(SelectedSourceHighlightColor);
        }

        /// <summary>
        /// An overridden method for reacting to the event of a document window
        /// being opened
        /// </summary>
        internal void AttachToDocument(string documentName, long docCookie, IVsWindowFrame frame)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            LineMarker.TryAttachToDocument(documentName, frame);
        }
    }
}
