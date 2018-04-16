// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.VisualStudio
{
    internal static class CodeFlowToTreeConverter
    {
        internal static List<CallTreeNode> Convert(CodeFlow codeFlow)
        {
            int currentCodeFlowIndex = -1;
            int nestingLevel = 0;

            return GetChildren(codeFlow, ref currentCodeFlowIndex, ref nestingLevel, null);
        }

        private static List<CallTreeNode> GetChildren(CodeFlow codeFlow, ref int currentCodeFlowIndex, ref int nestingLevel, CallTreeNode parent)
        {
            currentCodeFlowIndex++;
            List<CallTreeNode> children = new List<CallTreeNode>();
            bool foundCallReturn = false;
            ThreadFlow threadFlow = codeFlow.ThreadFlows?[0];

            if (threadFlow != null)
            {
                while (currentCodeFlowIndex < threadFlow.Locations.Count && !foundCallReturn)
                {
                    CodeFlowLocation codeFlowLocation = threadFlow.Locations[currentCodeFlowIndex];

                    if (codeFlowLocation.NestingLevel < nestingLevel)
                    {
                        // Call return
                        children.Add(new CallTreeNode
                        {
                            Location = codeFlowLocation,
                            Children = new List<CallTreeNode>(),
                            Parent = parent
                        });
                        foundCallReturn = true;
                        nestingLevel--;
                    }
                    else if (codeFlowLocation.NestingLevel > 0)
                    {
                        // Call
                        var newNode = new CallTreeNode
                        {
                            Location = codeFlowLocation,
                            Parent = parent
                        };
                        newNode.Children = GetChildren(codeFlow, ref currentCodeFlowIndex, ref nestingLevel, newNode);
                        children.Add(newNode);
                        nestingLevel++;
                    }
                    else
                    {
                        children.Add(new CallTreeNode
                        {
                            Location = codeFlowLocation,
                            Children = new List<CallTreeNode>(),
                            Parent = parent
                        });
                        currentCodeFlowIndex++;
                    }
                    //switch (threadFlow.Locations[currentCodeFlowIndex].Kind)
                    //{
                    //    case CodeFlowLocationKind.Call:
                    //        var newNode = new CallTreeNode
                    //        {
                    //            Location = codeFlow.Locations[currentCodeFlowIndex],
                    //            Parent = parent
                    //        };
                    //        newNode.Children = GetChildren(codeFlow, ref currentCodeFlowIndex, newNode);
                    //        children.Add(newNode);
                    //        break;

                    //    case CodeFlowLocationKind.CallReturn:
                    //        children.Add(new CallTreeNode
                    //        {
                    //            Location = codeFlow.Locations[currentCodeFlowIndex],
                    //            Children = new List<CallTreeNode>(),
                    //            Parent = parent
                    //        });
                    //        foundCallReturn = true;
                    //        break;

                    //    default:
                    //        children.Add(new CallTreeNode
                    //        {
                    //            Location = codeFlow.Locations[currentCodeFlowIndex],
                    //            Children = new List<CallTreeNode>(),
                    //            Parent = parent
                    //        });
                    //        currentCodeFlowIndex++;
                    //        break;
                    //}
                }
            }

            currentCodeFlowIndex++;
            return children;
        }
    }
}
