// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// Tagger provider for highlighting the 'any' ContentType.
    /// </summary>
    /// <remarks>
    /// This is similar to the TextMarkerProviderFactory, except it applies to the 'any' ContentType.
    /// We can't use TextMarkerProviderFactory because it only applies to 'text' ContentTypes.
    /// HTML files are 'projection' types, which doesn't inherit from 'text'. So TextMarkerProviderFactory
    /// cannot highlight HTML file contents.
    /// </remarks>
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(SarifLocationTextMarkerTag))]
    [TagType(typeof(SarifLocationErrorTag))]
    [ContentType("any")]
    internal class SarifLocationTaggerProvider : IViewTaggerProvider
    {
#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF
        [Import]
        private IPersistentSpanFactory PersistentSpanFactory;
#pragma warning restore IDE0044
#pragma warning restore CS0649

        /// <summary>
        /// Protects access to the <see cref="SarifTaggers"/> list.
        /// </summary>
        private static readonly ReaderWriterLockSlimWrapper SarifTaggersLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());

        /// <summary>
        /// This list of running taggers.
        /// </summary>
        /// <remarks>
        /// This static list is used to easily notify all running taggers that there tags need to be refreshed.
        /// </remarks>
        private static readonly List<ISarifLocationTagger2> SarifTaggers = new List<ISarifLocationTagger2>();

        /// <inheritdoc/>
        /// <summary>
        /// Note that Visual Studio's tagger aggregation expects and correctly handles null
        /// if a tagger provider does not want to provide tags
        /// </summary>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer textBuffer) where T : ITag
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            // The SARIF viewer needs a text buffer to have a file name in order to be able to associate a SARIF
            // result location with the file. Visual Studio allows text buffers to be created at any time with our without a filename.
            // So, if there is no file name, then do not create a tagger for this buffer.
            if (!SdkUIUtilities.TryGetFileNameFromTextBuffer(textBuffer, out _))
            {
                return null;
            }

            ISarifLocationTagger2 newTagger = null;

            if (typeof(T) == typeof(IErrorTag))
            {
                newTagger = new SarifLocationErrorTagger(textView, textBuffer, this.PersistentSpanFactory);
            }

            if (typeof(T) == typeof(ITextMarkerTag))
            {
                newTagger = new SarifLocationTextMarkerTagger(textView, textBuffer, this.PersistentSpanFactory);
            }

            if (newTagger != null)
            {
                newTagger.Disposed += this.TaggerDisposed;

                using (SarifTaggersLock.EnterWriteLock())
                {
                    SarifTaggers.Add(newTagger);
                }
            }

            return newTagger as ITagger<T>;
        }

        /// <summary>
        /// Causes a tags changed notification to be sent out from all known taggers.
        /// </summary>
        /// <remarks>
        /// The primary use of this is to send a tags changed notification when a "text view" is already open and visible
        /// and a tagger is active for that "text view" and a SARIF log is loaded via an API.
        /// </remarks>
        public static void MarkAllTagsAsDirty()
        {
            IEnumerable<ISarifLocationTagger2> taggers;
            using (SarifTaggersLock.EnterReadLock())
            {
                taggers = SarifTaggers.ToList();
            }

            foreach (ISarifLocationTagger2 tagger in taggers)
            {
                tagger.MarkTagsDirty();
            }
        }

        private void TaggerDisposed(object sender, EventArgs e)
        {
            if (sender is ISarifLocationTagger2 tagger)
            {
                tagger.Disposed -= this.TaggerDisposed;

                using (SarifTaggersLock.EnterWriteLock())
                {
                    SarifTaggers.Remove(tagger);
                }
            }
        }
    }
}