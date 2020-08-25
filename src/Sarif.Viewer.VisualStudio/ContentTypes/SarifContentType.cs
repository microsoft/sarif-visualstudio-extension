namespace Microsoft.Sarif.Viewer.ContentTypes
{
    using Microsoft.VisualStudio.Utilities;
    using System.ComponentModel.Composition;

    public static class SarifContentType
    {
        /// <summary>
        /// The content type name for SARIF log files..
        /// </summary>
        public const string ContentTypeName = "SARIF";

        /// <summary>
        /// Gets the base content type definition for SARIF log files..
        /// </summary>
        [Export]
        [BaseDefinition("json")]
        [Name(ContentTypeName)]
        internal static ContentTypeDefinition SarifBaseContentType { get; } = null;

        /// <summary>
        /// Gets the ".sarif" file extension mapping to "SARIF" content type.
        /// </summary>
        [Export]
        [FileExtension(".sarif")]
        [ContentType(ContentTypeName)]
        internal static FileExtensionToContentTypeDefinition SarifFileExtensionContentType { get; } = null;
    }
}
