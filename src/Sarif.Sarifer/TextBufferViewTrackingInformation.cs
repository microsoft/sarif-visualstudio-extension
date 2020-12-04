// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Contains information used to track the status of each <see cref="ITextBuffer"/> for which
    /// background analysis has been performed.
    /// </summary>
    internal class TextBufferViewTrackingInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextBufferViewTrackingInformation"/> class.
        /// </summary>
        internal TextBufferViewTrackingInformation(string filePath)
        {
            this.Path = filePath;
            this.LogId = Guid.NewGuid().ToString();
            this.Views = new List<ITextView>();
        }

        /// <summary>
        /// Get the path of the file.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Get the list of views currently open on the tracked text buffer.
        /// </summary>
        internal List<ITextView> Views { get; }

        /// <summary>
        /// Gets a unique id for the SARIF log associated with this text buffer.
        /// </summary>
        /// <remarks>
        /// This id will be used to remove the log's results from the error list when the user
        /// closes the last view on this text buffer.
        /// </remarks>
        internal string LogId { get; }

        /// <summary>
        /// Add a view to the list of views on the tracked text buffer.
        /// </summary>
        /// <param name="textView">
        /// The view being added.
        /// </param>
        internal void Add(ITextView textView)
        {
            this.Views.Add(textView);
        }

        /// <summary>
        /// Remove a view from the list of views on the tracked text buffer.
        /// </summary>
        /// <param name="textView">
        /// The view being removed.
        /// </param>
        internal void Remove(ITextView textView)
        {
            this.Views.Remove(textView);
        }
    }
}
