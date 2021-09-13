// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Fixes
{
    internal enum FixScope
    {
        /// <summary>
        /// Scope of fixes to be applied is in current document.
        /// </summary>
        Document,

        /// <summary>
        /// Scope of fixes to be applied is in current project.
        /// </summary>
        Project,

        /// <summary>
        /// Scope of fixes to be applied is in current solution.
        /// </summary>
        Solution,
    }

    internal class ApplyFixEventArgs
    {
        internal ApplyFixEventArgs(FixScope scope, SarifErrorListItem errorItem)
        {
            this.Scope = scope;
            this.ErrorItem = errorItem;
        }

        public FixScope Scope { get; }

        public SarifErrorListItem ErrorItem { get; }
    }
}
