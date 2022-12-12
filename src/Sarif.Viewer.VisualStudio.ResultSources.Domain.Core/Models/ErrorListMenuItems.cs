// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    public class ErrorListMenuItems
    {
        public IList<ErrorListMenuFlyout> Flyouts { get; set; } = new List<ErrorListMenuFlyout>();

        public IList<ErrorListMenuCommand> Commands { get; set; } = new List<ErrorListMenuCommand>();
    }
}
