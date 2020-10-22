namespace Microsoft.Sarif.Viewer.ContentTypes
{
    using Microsoft.VisualStudio.Utilities;
    using System.ComponentModel.Composition;

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
