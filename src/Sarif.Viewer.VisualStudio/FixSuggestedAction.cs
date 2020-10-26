// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// An suggested action to resolve a result in a SARIF log file.
    /// </summary>
    internal class FixSuggestedAction : ISuggestedAction
    {
        private readonly FixModel fix;

        public FixSuggestedAction(FixModel fix)
        {
            this.fix = fix;
            DisplayText = fix.Description;
        }

        /// <inheritdoc/>
        public bool HasActionSets => false;

        /// <inheritdoc/>
        public string DisplayText { get; }

        /// <inheritdoc/>
        public ImageMoniker IconMoniker => default(ImageMoniker);

        /// <inheritdoc/>
        public string IconAutomationText => null;

        /// <inheritdoc/>
        public string InputGestureText => null;

        /// <inheritdoc/>
        public bool HasPreview => false;

        /// <inheritdoc/>
        public void Dispose() { }

        /// <inheritdoc/>
        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<object> GetPreviewAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        /// <inheritdoc/>
        public void Invoke(CancellationToken cancellationToken)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            fix.Apply();
        }

        /// <inheritdoc/>
        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
