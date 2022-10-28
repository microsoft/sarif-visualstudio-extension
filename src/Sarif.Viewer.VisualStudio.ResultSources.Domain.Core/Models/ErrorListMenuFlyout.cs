// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    public class ErrorListMenuFlyout : ErrorListMenuItem
    {
        public ErrorListMenuFlyout(string text)
            : base(text)
        {
        }

        public IList<ErrorListMenuFlyout> Flyouts { get; set; } = new List<ErrorListMenuFlyout>();

        public IList<ErrorListMenuCommand> Commands { get; set;  } = new List<ErrorListMenuCommand>();

        public int DescendantCommandsCount
        {
            get
            {
                int count = 0;
                foreach (ErrorListMenuFlyout flyout in Flyouts)
                {
                    count += flyout.DescendantCommandsCount;
                }

                count += this.Commands.Count;
                return count;
            }
        }
    }
}
