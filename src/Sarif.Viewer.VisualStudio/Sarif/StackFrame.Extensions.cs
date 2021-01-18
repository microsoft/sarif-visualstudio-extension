// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class StackFrameExtensions
    {
        public static StackFrameModel ToStackFrameModel(this StackFrame stackFrame, int resultId, int runIndex)
        {
            var model = new StackFrameModel(resultId, runIndex);

            model.FullyQualifiedLogicalName = stackFrame.Location.LogicalLocation.FullyQualifiedName;
            model.Message = stackFrame.Location?.Message?.Text;
            model.Module = stackFrame.Module;

            if (stackFrame.Location?.PhysicalLocation?.Address != null)
            {
                model.Address = stackFrame.Location.PhysicalLocation.Address.AbsoluteAddress;
                model.Offset = stackFrame.Location.PhysicalLocation.Address.OffsetFromParent ?? 0;
            }

            PhysicalLocation physicalLocation = stackFrame.Location?.PhysicalLocation;
            if (physicalLocation?.ArtifactLocation != null)
            {
                model.FilePath = physicalLocation.ArtifactLocation.Uri.ToPath();
                Region region = physicalLocation.Region;
                if (region != null)
                {
                    model.Region = region;
                    model.Line = region.StartLine;
                    model.Column = region.StartColumn;
                }
            }

            return model;
        }
    }
}
