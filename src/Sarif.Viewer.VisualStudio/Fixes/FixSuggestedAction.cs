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
using Microsoft.VisualStudio.Text;

namespace Microsoft.Sarif.Viewer.Fixes
{
    /// <summary>
    /// An suggested action to fix a result in a SARIF log file.
    /// </summary>
    internal class FixSuggestedAction : ISuggestedAction
    {
        private readonly FixModel fix;
        private readonly ITextBuffer textBuffer;
        private readonly IPreviewProvider previewProvider;

        /// <summary>
        /// Creates a new instance of <see cref="FixSuggestedAction"/>.
        /// </summary>
        /// <param name="fix">
        /// The SARIF <see cref="Fix"/> object that describes the action.
        /// </param>
        /// <param name="textBuffer">
        /// The text buffer to which the fix will be applied.
        /// </param>
        /// <param name="previewProvider">
        /// Creates the XAML UIControl that displays the preview.
        /// </param>
        public FixSuggestedAction(
            FixModel fix,
            ITextBuffer textBuffer,
            IPreviewProvider previewProvider)
        {
            this.fix = fix;
            this.textBuffer = textBuffer;
            this.previewProvider = previewProvider;
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
