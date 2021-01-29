// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    public class TextEditIdledEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextEditIdledEventArgs"/> class.
        /// </summary>
        /// <param name="textView">
        /// Instance of <see cref="ITextView"/> class.
        /// </param>
        public TextEditIdledEventArgs(ITextView textView)
        {
            this.TextView = textView;
        }

        /// <summary>
        /// Gets the path to the file whose contents are being viewed, or <c>null</c> if
        /// <see cref="TextBuffer"/> is not associated with a file.
        /// </summary>
        public ITextView TextView { get; }
    }
}
