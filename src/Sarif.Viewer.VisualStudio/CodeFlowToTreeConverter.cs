// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.VisualStudio
{
    internal static class CodeFlowToTreeConverter
    {
        internal static List<CallTreeNode> Convert(CodeFlow codeFlow)
        {
            var root = new CallTreeNode { Children = new List<CallTreeNode>() };
            ThreadFlow threadFlow = codeFlow.ThreadFlows?[0];

            if (threadFlow != null)
            {
                int lastNestingLevel = 0;
                CallTreeNode lastParent = root;

                foreach (CodeFlowLocation location in threadFlow.Locations)
                {
                    var newNode = new CallTreeNode
                    {
                        Location = location,
                        Children = new List<CallTreeNode>()
                    };

                    if (location.NestingLevel > lastNestingLevel)
                    {
                        // Previous node was a call
                        lastParent = lastParent.Children.Last();
                        lastParent.Kind = CallTreeNodeKind.Call;
                    }
                    else if (location.NestingLevel < lastNestingLevel)
                    {
                        // Previous node was a return
                        CallTreeNode node = lastParent.Children.Last(); // Get the last node we created
                        node.Kind = CallTreeNodeKind.Call;
                        lastParent = node.Parent.Parent;
                    }

                    newNode.Parent = lastParent;
                    lastParent.Children.Add(newNode);
                    lastNestingLevel = location.NestingLevel;
                }

                root.Children.ForEach(n => n.Parent = null);
            }

            return root.Children;
            //return GetChildren(codeFlow, ref currentCodeFlowIndex, ref nestingLevel, null);
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
                            Parent = parent,
                            Kind = CallTreeNodeKind.Return
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
                            Parent = parent,
                            Kind = CallTreeNodeKind.Call
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
