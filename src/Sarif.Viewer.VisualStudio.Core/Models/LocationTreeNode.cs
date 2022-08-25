// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Sarif.Viewer.Models
{
    internal class LocationTreeNode
    {
        public LocationModel Location { get; }

        public IList<LocationTreeNode> Children { get; } = new List<LocationTreeNode>();

        public int Level => this.Location?.NestingLevel ?? -1;

        public LocationTreeNode Parent { get; }

        public LocationTreeNode(LocationModel locationModel, LocationTreeNode parent)
        {
            this.Location = locationModel;
            this.Parent = parent;
        }
    }
}
