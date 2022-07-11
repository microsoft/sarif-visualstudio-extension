// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Workspace.VSIntegration.UI;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer.Commands
{
    [Export(typeof(INodeExtender))]
    internal class AnalyzeSolutionFolderNodeExtender : INodeExtender
    {
        public IChildrenSource ProvideChildren(WorkspaceVisualNodeBase parentNode)
        {
            // Only used when providing child nodes.
            return null;
        }

        public IWorkspaceCommandHandler ProvideCommandHandler(WorkspaceVisualNodeBase parentNode)
        {
            if (parentNode is IFileNode fileNode)
            {
                return new AnalyzeSolutionFolderCommandHandler(fileNode);
            }

            if (parentNode is IFolderNode folderNode)
            {
                return new AnalyzeSolutionFolderCommandHandler(folderNode);
            }

            return null;
        }

        internal class AnalyzeSolutionFolderCommandHandler : IWorkspaceCommandHandler
        {
            private const uint CmdId = SariferPackageCommandIds.AnalyzeSolutionFolder;
            private static Guid commandGroup = Guids.SariferFolderViewCommandSet;

            private readonly IFileSystem fileSystem;
            private readonly IFileSystemNode attachedNode;
            private readonly IComponentModel componentModel;
            private readonly IBackgroundAnalysisService backgroundAnalysisService;
            private CancellationTokenSource cancellationTokenSource;
            private bool analysisInProgress;

            public AnalyzeSolutionFolderCommandHandler(
                IFileSystemNode attachedNode,
                IBackgroundAnalysisService backgroundAnalysisService = null,
                IFileSystem fileSystem = null)
            {
                this.attachedNode = attachedNode;
                this.fileSystem = fileSystem ?? new FileSystem();

                if (backgroundAnalysisService == null)
                {
                    this.componentModel ??= (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                    this.backgroundAnalysisService ??= this.componentModel.GetService<IBackgroundAnalysisService>();
                }
                else
                {
                    this.backgroundAnalysisService = backgroundAnalysisService;
                }

                this.backgroundAnalysisService.AnalysisCompleted += this.BackgroundAnalysisService_AnalysisCompleted;
            }

            public int Priority => 100;

            public bool IgnoreOnMultiselect => true;

            public int Exec(List<WorkspaceVisualNodeBase> selection, Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                if (pguidCmdGroup == commandGroup && nCmdID == CmdId)
                {
                    IEnumerable<IFileNode> fileNodes = selection.OfType<IFileNode>();
                    if (fileNodes.Any())
                    {
                        AnalyzeTargets(fileNodes);
                        return VSConstants.S_OK;
                    }

                    IEnumerable<IFolderNode> folderNodes = selection.OfType<IFolderNode>();
                    if (folderNodes.Any())
                    {
                        AnalyzeTargets(folderNodes);
                        return VSConstants.S_OK;
                    }

                    return VSConstants.S_OK;
                }

                return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
            }

            public bool QueryStatus(List<WorkspaceVisualNodeBase> selection, Guid pguidCmdGroup, uint nCmdID, ref uint cmdf, ref string customTitle)
            {
                if (pguidCmdGroup == commandGroup && nCmdID == CmdId)
                {
                    cmdf = selection.OfType<IFileNode>().Any() || selection.OfType<IFolderNode>().Any() || !analysisInProgress
                        ? (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED)
                        : (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE);

                    return true;
                }

                return false;
            }

            internal void AnalyzeTargets(IEnumerable<IFileNode> files)
            {
                // Always cancel before start.
                this.cancellationTokenSource?.Cancel();
                this.cancellationTokenSource = new CancellationTokenSource();

                List<string> targetFiles = GetFiles(files);

                if (targetFiles?.Any() == true)
                {
                    this.backgroundAnalysisService.AnalyzeAsync(this.attachedNode.FullPath, targetFiles, this.cancellationTokenSource.Token).GetAwaiter();
                    analysisInProgress = true;
                }
            }

            internal void AnalyzeTargets(IEnumerable<IFolderNode> folders)
            {
                // Always cancel before start.
                this.cancellationTokenSource?.Cancel();
                this.cancellationTokenSource = new CancellationTokenSource();

                List<string> targetFiles = GetFilesInDirectory(folders.Select(f => f.FullPath));

                if (targetFiles?.Any() == true)
                {
                    this.backgroundAnalysisService.AnalyzeAsync(this.attachedNode.FullPath, targetFiles, this.cancellationTokenSource.Token)
                        .FileAndForget(FileAndForgetEventName.BackgroundAnalysisFailure);
                    analysisInProgress = true;
                }
            }

            protected void BackgroundAnalysisService_AnalysisCompleted(object sender, EventArgs e)
            {
                analysisInProgress = false;
            }

            private List<string> GetFiles(IEnumerable<IFileNode> files)
            {
                var targetFiles = new List<string>();

                foreach (IFileNode file in files)
                {
                    if (this.fileSystem.FileExists(file.FullPath))
                    {
                        targetFiles.Add(file.FullPath);
                    }
                }

                return targetFiles;
            }

            // ignore hidden folders/files e.g. ".vs", ".git"
            private List<string> GetFilesInDirectory(IEnumerable<string> folders)
            {
                var targetFiles = new List<string>();

                foreach (string folder in folders)
                {
                    if (string.IsNullOrWhiteSpace(folder))
                    {
                        continue;
                    }

                    try
                    {
                        DirectoryInfo directory = new DirectoryInfo(folder);
                        FileInfo[] files = directory.GetFiles("*.*", SearchOption.TopDirectoryOnly);

                        targetFiles.AddRange(
                            files
                            .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden))
                            .Select(f => f.FullName));

                        targetFiles.AddRange(
                            GetFilesInDirectory(
                                directory
                                .GetDirectories("*.*", SearchOption.TopDirectoryOnly)
                                .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden))
                                .Select(d => d.FullName)));
                    }

                    // ignore the directory if error occured
                    catch (SecurityException) { } // cannot access
                    catch (ArgumentException) { } // invalid chars in path
                    catch (PathTooLongException) { }
                }

                return targetFiles;
            }
        }
    }
}
