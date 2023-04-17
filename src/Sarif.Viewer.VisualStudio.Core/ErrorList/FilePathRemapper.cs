// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.Sarif.Viewer;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

using Microsoft.VisualStudio.Workspace.VSIntegration.Contracts;
using Microsoft.Sarif.Viewer.ErrorList;
using System.Linq;
using Microsoft.Sarif.Viewer.Models;
using System.IO;

namespace Sarif.Viewer.VisualStudio.Core.ErrorList
{
    /// <summary>
    /// This class handles the mapping relative file paths into their absolute form.
    /// </summary>
    internal class FilePathRemapper
    {
        private bool RemapRelativePath(string relativePath, RunDataCache dataCache, string workingDirectory)
        {
            string resolvedPath = null;

            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            if (relativePath == null)
            {
                throw new ArgumentNullException($"{nameof(RemapRelativePath)} received null {nameof(relativePath)}.");
            }

            string solutionPath = CodeAnalysisResultManager.GetSolutionPath(
                (DTE2)Package.GetGlobalService(typeof(DTE)),
                ((IComponentModel)Package.GetGlobalService(typeof(SComponentModel))).GetService<IVsFolderWorkspaceService>());

            // File contents embedded in SARIF.
            bool hasHash = dataCache.FileDetails.TryGetValue(relativePath, out ArtifactDetailsModel model) && !string.IsNullOrEmpty(model?.Sha256Hash);
            string embeddedTempFilePath = this.CreateFileFromContents(dataCache.FileDetails, relativePath);

            try
            {
                resolvedPath = this.GetFilePathFromHttp(sarifErrorListItem, uriBaseId, dataCache, relativePath);
            }
            catch (WebException)
            {
                // failed to download the file
                return false;
            }

            if (string.IsNullOrEmpty(resolvedPath))
            {
                // resolve path, existing file in local disk
                resolvedPath = this.GetRebaselinedFileName(
                    uriBaseId: uriBaseId,
                    pathFromLogFile: relativePath,
                    dataCache: dataCache,
                    workingDirectory: sarifErrorListItem.WorkingDirectory,
                    solutionFullPath: solutionPath);
            }

            // verify resolved file with artifact's Hash
            if (hasHash)
            {
                string currentResolvedPath = resolvedPath;
                if (!this.VerifyFileWithArtifactHash(sarifErrorListItem, relativePath, dataCache, currentResolvedPath, embeddedTempFilePath, out resolvedPath))
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(resolvedPath))
            {
                // User needs to locate file.
                resolvedPath = this._promptForResolvedPathDelegate(sarifErrorListItem, relativePath);
            }

            if (!string.IsNullOrEmpty(resolvedPath))
            {
                // save resolved path to mapping
                if (!this.SaveResolvedPathToUriBaseMapping(uriBaseId, relativePath, relativePath, resolvedPath, dataCache))
                {
                    resolvedPath = relativePath;
                }
            }

            if (string.IsNullOrEmpty(resolvedPath) || relativePath.Equals(resolvedPath, StringComparison.OrdinalIgnoreCase))
            {
                resolvedPath = relativePath;
                return false;
            }

            // Update all the paths in this run.
            this.RemapFilePaths(dataCache.SarifErrors, relativePath, resolvedPath);
            return true;
        }

        internal string GetFilePathFromHttp(string workingDirectory, string uriBaseId, RunDataCache dataCache, string pathFromLogFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Uri uri = null;
            if ((uriBaseId != null
                 && dataCache.OriginalUriBasePaths.TryGetValue(uriBaseId, out Uri baseUri)
                 && Uri.TryCreate(baseUri, pathFromLogFile, out uri)
                 && uri.IsHttpScheme()) ||

                 // if result location uri is an absolute http url
                 (Uri.TryCreate(pathFromLogFile, UriKind.Absolute, out uri) &&
                  uri.IsHttpScheme()))
            {
                return this.HandleHttpFileDownloadRequest(
                            VersionControlParserFactory.ConvertToRawFileLink(uri),
                            workingDirectory);
            }

            return null;
        }

        internal string HandleHttpFileDownloadRequest(Uri uri, string workingDirectory, string localRelativePath = null)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            bool allow = this._allowedDownloadHosts.Contains(uri.Host);

            // File needs to be downloaded, prompt for confirmation if host is not already allowed
            if (!allow)
            {
                MessageDialogCommand result = MessageDialog.Show(Resources.ConfirmDownloadDialog_Title,
                                                                 string.Format(Resources.ConfirmDownloadDialog_Message, uri),
                                                                 MessageDialogCommandSet.YesNo,
                                                                 string.Format(Resources.ConfirmDownloadDialog_CheckboxLabel, uri.Host),
                                                                 out bool alwaysAllow);

                if (result != MessageDialogCommand.No)
                {
                    allow = true;

                    if (alwaysAllow)
                    {
                        this.AddAllowedDownloadHost(uri.Host);
                    }
                }
            }

            if (allow)
            {
                try
                {
                    workingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ?
                                       Path.Combine(Path.GetTempPath(), this.CurrentRunIndex.ToString()) :
                                       workingDirectory;
                    return this.DownloadFile(workingDirectory, uri.ToString(), localRelativePath);
                }
                catch (Exception ex)
                {
                    VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
                               Resources.DownloadFail_DialogMessage + Environment.NewLine + ex.Message,
                               null, // title
                               OLEMSGICON.OLEMSGICON_CRITICAL,
                               OLEMSGBUTTON.OLEMSGBUTTON_OK,
                               OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    Trace.WriteLine($"DownloadFile {uri.ToString()} threw exception: {ex.Message}");
                }
            }

            return null;
        }

        internal void AddAllowedDownloadHost(string host)
        {
            if (!this._allowedDownloadHosts.Contains(host))
            {
                this._allowedDownloadHosts.Add(host);
                SdkUIUtilities.StoreObject<List<string>>(this._allowedDownloadHosts, AllowedDownloadHostsFileName);
            }
        }



        internal string GetFilePathFromHttp(SarifErrorListItem sarifErrorListItem, string uriBaseId, RunDataCache dataCache, string pathFromLogFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Uri uri = null;
            if ((uriBaseId != null
                 && dataCache.OriginalUriBasePaths.TryGetValue(uriBaseId, out Uri baseUri)
                 && Uri.TryCreate(baseUri, pathFromLogFile, out uri)
                 && uri.IsHttpScheme()) ||

                 // if result location uri is an absolute http url
                 (Uri.TryCreate(pathFromLogFile, UriKind.Absolute, out uri) &&
                  uri.IsHttpScheme()))
            {
                return this.HandleHttpFileDownloadRequest(
                            VersionControlParserFactory.ConvertToRawFileLink(uri),
                            sarifErrorListItem.WorkingDirectory);
            }

            return null;
        }
    }
}
