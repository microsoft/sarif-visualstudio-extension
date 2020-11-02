// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer.Tags
{
    [Export(typeof(ISarifLocationTaggerService))]
    internal class SarifLocationTaggerService : ISarifLocationTaggerService, IDisposable
    {
        private bool isDisposed;

        /// <summary>
        /// Protects access to the <see cref="sarifTaggers"/> list.
        /// </summary>
        private readonly ReaderWriterLockSlimWrapper sarifTaggersLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());

        /// <summary>
        /// This list of running taggers.
        /// </summary>
        /// <remarks>
        /// This static list is used to easily notify all running taggers that there tags need to be refreshed.
        /// </remarks>
        private readonly List<ISarifLocationTagger> sarifTaggers = new List<ISarifLocationTagger>();

        /// <inheritdoc/>
        public void RefreshTags(ITextBuffer textBuffer = null)
        {
            IEnumerable<ISarifLocationTagger> taggers;
            using (this.sarifTaggersLock.EnterReadLock())
            {
                taggers = sarifTaggers.ToList();
            }

            if (textBuffer != null)
            {
                taggers = taggers.Where(t => t.TextBuffer == textBuffer);
            }

            foreach (ISarifLocationTagger tagger in taggers)
            {
                tagger.RefreshTags();
            }
        }

        /// <inheritdoc/>
        public void AddTagger(ISarifLocationTagger tagger)
        {
            using (this.sarifTaggersLock.EnterWriteLock())
            {
                if (!this.sarifTaggers.Contains(tagger))
                {
                    this.sarifTaggers.Add(tagger);
                    tagger.Disposed += this.Tagger_Disposed;
                }
            }
        }

        private void Tagger_Disposed(object sender, EventArgs e)
        {
            if (sender is ISarifLocationTagger tagger)
            {
                using (this.sarifTaggersLock.EnterWriteLock())
                {
                    if (this.sarifTaggers.Remove(tagger))
                    {
                        tagger.Disposed -= this.Tagger_Disposed;
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            if (disposing)
            {
                this.sarifTaggersLock.InnerLock.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
