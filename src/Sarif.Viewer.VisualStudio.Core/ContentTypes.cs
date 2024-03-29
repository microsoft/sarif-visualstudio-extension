﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer
{
    public static class ContentTypes
    {
        /// <summary>
        /// The content type name for SARIF log files.
        /// </summary>
        public const string Sarif = "SARIF";

        /// <summary>
        /// The content type name that accepts any file.
        /// </summary>
        public const string Any = "any";

        /// <summary>
        /// The content type name for text files.
        /// </summary>
        public const string Text = "text";

        /// <summary>
        /// Gets the base content type definition for SARIF log files..
        /// </summary>
        [Export]
        [BaseDefinition("json")]
        [Name(Sarif)]
        internal static ContentTypeDefinition SarifBaseContentType { get; } = null;

        /// <summary>
        /// Gets the ".sarif" file extension mapping to "SARIF" content type.
        /// </summary>
        [Export]
        [FileExtension(".sarif")]
        [ContentType(Sarif)]
        internal static FileExtensionToContentTypeDefinition SarifFileExtensionContentType { get; } = null;
    }
}
