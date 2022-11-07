// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    /// <summary>
    /// Represents event data for a requested to add new menu items.
    /// </summary>
    public class RequestAddMenuItemsEventArgs : ServiceEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestAddMenuItemsEventArgs"/> class.
        /// </summary>
        public RequestAddMenuItemsEventArgs()
        {
            this.ServiceEventType = ResultSourceServiceEventType.RequestAddMenuItems;
        }

        /// <summary>
        /// Gets or sets the first menu ID for the requesting service.
        /// </summary>
        public int FirstMenuId { get; set; }

        /// <summary>
        /// Gets or sets the first command ID for the requesting service.
        /// </summary>
        public int FirstCommandId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ErrorListMenuItem"/>s to be added.
        /// </summary>
        public ErrorListMenuItems MenuItems { get; set; } = new ErrorListMenuItems();
    }
}
