// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Sarif.Viewer.VisualStudio.GitHelper.Core
{
    public enum GitEventType
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// Push.
        /// </summary>
        Push = 1,

        /// <summary>
        /// Pull.
        /// </summary>
        Pull = 2,

        /// <summary>
        /// Branch change.
        /// </summary>
        BranchChange = 3,
    }
}
