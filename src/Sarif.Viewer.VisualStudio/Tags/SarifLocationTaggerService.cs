// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.VisualStudio.Utilities;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Threading;

    [Export(typeof(ISarifLocationTaggerService))]
    internal class SarifLocationTaggerService: ISarifLocationTaggerService, IDisposable
    {
        private bool isDisposed;

        /// <summary>
        /// Protects access to the <see cref="SarifTaggers"/> list.
        /// </summary>
        private readonly ReaderWriterLockSlimWrapper SarifTaggersLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());

        /// <summary>
        /// This list of running taggers.
        /// </summary>
        /// <remarks>
        /// This static list is used to easily notify all running taggers that there tags need to be refreshed.
        /// </remarks>
        private readonly List<ISarifLocationTagger> SarifTaggers = new List<ISarifLocationTagger>();

        /// <inheritdoc/>
        public void RefreshAllTags()
        {
            IEnumerable<ISarifLocationTagger> taggers;
            using (this.SarifTaggersLock.EnterReadLock())
            {
                taggers = SarifTaggers.ToList();
            }

            foreach (ISarifLocationTagger tagger in taggers)
            {
                tagger.RefreshTags();
            }
        }

        /// <inheritdoc/>
        public void NotifyTaggerCreated(ISarifLocationTagger tagger)
        {
            using (this.SarifTaggersLock.EnterWriteLock())
            {
                if (!this.SarifTaggers.Contains(tagger))
                {
                    this.SarifTaggers.Add(tagger);
                    tagger.Disposed += this.Tagger_Disposed;
                }
            }
        }

        private void Tagger_Disposed(object sender, EventArgs e)
        {
            if (sender is ISarifLocationTagger tagger)
            {
                using (this.SarifTaggersLock.EnterWriteLock())
                {
                    if (!this.SarifTaggers.Remove(tagger))
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
                this.SarifTaggersLock.InnerLock.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
