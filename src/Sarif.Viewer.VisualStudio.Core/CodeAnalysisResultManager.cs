// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;

using EnvDTE;

using EnvDTE80;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Visitors;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.Sarif.Viewer.Views;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Workspace.VSIntegration.Contracts;
using Microsoft.Win32;

using Newtonsoft.Json;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// This class is responsible for coordinating Code Analysis results end-to-end from the underlying
    /// implementation to the user interface activities.
    /// </summary>
    [Guid("4494F79A-6E9F-45EA-895B-7AE959B94D6A")]
    internal sealed class CodeAnalysisResultManager : IVsSolutionEvents
    {
        internal const int E_FAIL = unchecked((int)0x80004005);
        internal const uint VSCOOKIE_NIL = 0;
        internal const int S_OK = 0;
        private const string AllowedDownloadHostsFileName = "AllowedDownloadHosts.json";
        private const string AllowedFileExtensionsFileName = "AllowedFileExtensions.json";
        private const string TemporaryFileDirectoryName = "SarifViewer";
        private readonly string temporaryFilePath;

        // Cookie for registration and unregistration
        private uint m_solutionEventsCookie;
        private readonly List<string> _allowedDownloadHosts;
        private readonly HashSet<string> _allowedFileExtensions;

        private readonly IFileSystem _fileSystem;

        private readonly ConcurrentDictionary<string, ResolveEmbeddedFileDialogResult> userDialogPreference;

        internal delegate string PromptForResolvedPathDelegate(SarifErrorListItem sarifErrorListItem, string pathFromLogFile);

        internal delegate ResolveEmbeddedFileDialogResult PromptForEmbeddedFileDelegate(string sarifLogFilePath, bool hasEmbeddedContent, ConcurrentDictionary<string, ResolveEmbeddedFileDialogResult> preference);

        private readonly PromptForResolvedPathDelegate _promptForResolvedPathDelegate;

        private readonly PromptForEmbeddedFileDelegate _promptForEmbeddedFileDelegate;

        private static readonly HttpClient s_httpClient = new HttpClient();

        // This ctor is internal rather than private for unit test purposes.
        internal CodeAnalysisResultManager(
            IFileSystem fileSystem,
            PromptForResolvedPathDelegate promptForResolvedPathDelegate = null,
            PromptForEmbeddedFileDelegate promptForEmbeddedFileDelegate = null)
        {
            this._fileSystem = fileSystem;
            this._promptForResolvedPathDelegate = promptForResolvedPathDelegate ?? this.PromptForResolvedPath;
            this._promptForEmbeddedFileDelegate = promptForEmbeddedFileDelegate ?? this.PromptForEmbeddedFile;

            this._allowedDownloadHosts = SdkUIUtilities.GetStoredObject<List<string>>(AllowedDownloadHostsFileName) ?? new List<string>();
            this._allowedFileExtensions = SdkUIUtilities.GetStoredObject<HashSet<string>>(AllowedFileExtensionsFileName) ?? new HashSet<string>();

            // Get temporary path for embedded files.
            this.temporaryFilePath = Path.GetTempPath();
            this.temporaryFilePath = Path.Combine(this.temporaryFilePath, TemporaryFileDirectoryName);

            this.userDialogPreference = new ConcurrentDictionary<string, ResolveEmbeddedFileDialogResult>();
        }

        public string TempDirectoryPath => this.temporaryFilePath;

        public static CodeAnalysisResultManager Instance = new CodeAnalysisResultManager(new FileSystem());

        public IDictionary<int, RunDataCache> RunIndexToRunDataCache { get; } = new Dictionary<int, RunDataCache>();

        /// <summary>
        /// Returns the last index given out by <see cref="GetNextRunIndex"/>.
        /// </summary>
        /// <remarks>
        /// The internal reference is for test code.
        /// </remarks>
        internal int CurrentRunIndex;

        public int GetNextRunIndex()
        {
            return Interlocked.Increment(ref this.CurrentRunIndex);
        }

        public RunDataCache CurrentRunDataCache
        {
            get
            {
                this.RunIndexToRunDataCache.TryGetValue(this.CurrentRunIndex, out RunDataCache dataCache);
                return dataCache;
            }
        }

        internal void Register()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Register this object to listen for IVsSolutionEvents
            if (!(ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) is IVsSolution solution))
            {
                throw Marshal.GetExceptionForHR(E_FAIL);
            }

            solution.AdviseSolutionEvents(this, out this.m_solutionEventsCookie);
        }

        /// <summary>
        /// Unregister this provider from VS.
        /// </summary>
        internal void Unregister()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Unregister this object from IVsSolutionEvents events
            if (this.m_solutionEventsCookie != VSCOOKIE_NIL)
            {
                if (ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) is IVsSolution solution)
                {
                    solution.UnadviseSolutionEvents(this.m_solutionEventsCookie);
                    this.m_solutionEventsCookie = VSCOOKIE_NIL;
                }
            }
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => S_OK;

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => S_OK;

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => S_OK;

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => S_OK;

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => S_OK;

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => S_OK;

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) => S_OK;

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => S_OK;

        public int OnBeforeCloseSolution(object pUnkReserved) => S_OK;

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            // When closing solution (or closing VS), remove the temporary folder.
            this.RemoveTemporaryFiles();

            return S_OK;
        }

        public void CacheUriBasePaths(Run run)
        {
            if (run.OriginalUriBaseIds is Dictionary<string, ArtifactLocation> source)
            {
                var target = this.CurrentRunDataCache.OriginalUriBasePaths as Dictionary<string, Uri>;

                // This line assumes an empty dictionary
                source.ToList().ForEach(x =>
                {
                    if (x.Value.Uri != null)
                    {
                        // The URI is not required.
                        // The SARIF producer has chosen not to specify a URI. See §3.14.14, NOTE 1, for an explanation.
                        target.Add(x.Key, x.Value.Uri.WithTrailingSlash());
                    }
                });
            }

            if (run.VersionControlProvenance != null && run.VersionControlProvenance.Any())
            {
                var sc = this.CurrentRunDataCache.SourceControlDetails as List<VersionControlDetails>;
                sc.AddRange(run.VersionControlProvenance);
            }
        }

        public bool TryResolveFilePath(int resultId, int runIndex, string uriBaseId, string relativePath, out string resolvedPath)
        {
            resolvedPath = null;

            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            if (!this.RunIndexToRunDataCache.TryGetValue(runIndex, out RunDataCache dataCache))
            {
                return false;
            }

            SarifErrorListItem sarifErrorListItem = dataCache.SarifErrors.FirstOrDefault(sarifResult => sarifResult.ResultId == resultId);
            if (sarifErrorListItem == null)
            {
                return false;
            }

            string solutionPath = GetSolutionPath(
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

        // Contents are embedded in SARIF. Create a file from these contents.
        internal string CreateFileFromContents(int runId, string fileName)
        {
            return this.CreateFileFromContents(this.RunIndexToRunDataCache[runId].FileDetails, fileName);
        }

        // Contents are embedded in SARIF. Create a file from these contents.
        internal string CreateFileFromContents(IDictionary<string, ArtifactDetailsModel> fileDetailsDictionary, string fileName)
        {
            if (!fileDetailsDictionary.TryGetValue(fileName, out ArtifactDetailsModel fileData))
            {
                return null;
            }

            if (!fileData.HasContent)
            {
                return null;
            }

            string contents = fileData.GetContents();
            if (string.IsNullOrEmpty(contents))
            {
                // artifact doesn't have embedded contents
                return null;
            }

            string finalPath = this.temporaryFilePath;

            // If the file path already starts with the temporary location,
            // that means we've already built the temporary file, so we can
            // just open it.
            if (fileName.StartsWith(finalPath))
            {
                finalPath = fileName;
            }
            else
            {
                // Else we have to create a location under the temp path.
                // Strip off the leading drive letter and backslash (e.g., "C:\"), if present.
                if (Path.IsPathRooted(fileName))
                {
                    string pathRoot = Path.GetPathRoot(fileName);
                    fileName = fileName.Substring(pathRoot.Length);
                }

                if (fileName.StartsWith("/") || fileName.StartsWith("\\"))
                {
                    fileName = fileName.Substring(1);
                }

                // In this code path, we are working with a non-file system URI
                // where the log file contains embedded SARIF content
                if (Uri.TryCreate(fileName, UriKind.RelativeOrAbsolute, out Uri uri) &&
                    uri.IsAbsoluteUri &&
                    !uri.IsFile)
                {
                    fileName = Guid.NewGuid().ToString();
                }

                // Combine all paths into the final.
                // Sha256Hash is guaranteed to exist. When SARIF file is read, only files
                // with Sha256 hashes are added to the FileDetails dictionary.
                finalPath = Path.Combine(finalPath, fileData.Sha256Hash, fileName);
            }

            string directory = Path.GetDirectoryName(finalPath);
            Directory.CreateDirectory(directory);

            if (!this._fileSystem.FileExists(finalPath))
            {
                this._fileSystem.FileWriteAllText(finalPath, contents);

                // File should be readonly, because it is embedded.
                this._fileSystem.FileSetAttributes(finalPath, FileAttributes.ReadOnly);
            }

            if (!fileDetailsDictionary.ContainsKey(finalPath))
            {
                // Add another key to our file data object, so that we can
                // find it if the user closes the window and reopens it.
                fileDetailsDictionary.Add(finalPath, fileData);
            }

            return finalPath;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "need to wait http request/file download to be completed before exit the function.")]
        internal string DownloadFile(string workingDirectory, string fileUrl, string localRelativeFilePath)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return fileUrl;
            }

            var sourceUri = new Uri(fileUrl);
            string relativeLocalPath = localRelativeFilePath ?? sourceUri.LocalPath;
            relativeLocalPath = NormalizeFilePath(relativeLocalPath).TrimStart('\\');

            string destinationFile = Path.Combine(workingDirectory, relativeLocalPath);
            string destinationDirectory = Path.GetDirectoryName(destinationFile);
            this._fileSystem.DirectoryCreateDirectory(destinationDirectory);

            if (!this._fileSystem.FileExists(destinationFile))
            {
                // have to use synchronous http request/file write so that
                // when this function exits the file is already created.
                using HttpResponseMessage response = s_httpClient.GetAsync(sourceUri).Result;

                // if the status code is other than 200 (OK), e.g. 401 (Unathorized) don't download the file
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream stream = response.Content.ReadAsStreamAsync().Result;
                    using FileStream fs = File.Create(destinationFile);
                    stream.CopyTo(fs);
                }
                else
                {
                    throw new Exception($"Not able to download file from Url {sourceUri}. Http status code: {response.StatusCode}");
                }
            }

            return destinationFile;
        }

        // Internal rather than private for unit testability.
        internal string GetRebaselinedFileName(string uriBaseId, string pathFromLogFile, RunDataCache dataCache, string workingDirectory = null, string solutionFullPath = null)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            string originalPath = pathFromLogFile;
            Uri relativeUri = null;
            string resolvedPath = null;

            if (this.TryResolveFilePathFromUriBasePaths(uriBaseId, pathFromLogFile, dataCache, out relativeUri, out resolvedPath))
            {
                return resolvedPath;
            }

            if (this.TryResolveFilePathFromRemappings(pathFromLogFile, dataCache, out resolvedPath))
            {
                return resolvedPath;
            }

            if (this.TryResolveFilePathFromSolution(
                solutionPath: solutionFullPath,
                pathFromLogFile: originalPath,
                fileSystem: this._fileSystem,
                resolvedPath: out resolvedPath))
            {
                return resolvedPath;
            }

            // try to resolve using VersionControlProvenance
            if (this.TryResolveFilePathFromSourceControl(dataCache.SourceControlDetails, pathFromLogFile, workingDirectory, this._fileSystem, out resolvedPath))
            {
                return resolvedPath;
            }

            return null;
        }

        internal void RemapFilePaths(IList<SarifErrorListItem> sarifErrors, string originalPath, string remappedPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (SarifErrorListItem sarifError in sarifErrors)
            {
                sarifError.RemapFilePath(originalPath, remappedPath);
            }
        }

        private string PromptForResolvedPath(SarifErrorListItem sarifErrorListItem, string pathFromLogFile)
        {
            // Opening the OpenFileDialog causes the TreeView to lose focus,
            // which in turn causes the TreeViewItem selection to be unpredictable
            // (because the selection event relies on the TreeViewItem focus.)
            // We'll save the element which currently has focus and then restore
            // focus after the OpenFileDialog is closed.
            var elementWithFocus = Keyboard.FocusedElement as UIElement;

            string fileName = Path.GetFileName(pathFromLogFile);
            var openFileDialog = new OpenFileDialog
            {
                Title = string.Format(Resources.PromptForResolvedPathDialogTitle, pathFromLogFile),
                InitialDirectory = sarifErrorListItem != null ? Path.GetDirectoryName(sarifErrorListItem.LogFilePath) : null,
                Filter = $"{fileName}|{fileName}",
                RestoreDirectory = true,
            };

            try
            {
                bool? dialogResult = openFileDialog.ShowDialog();

                return dialogResult == true ? openFileDialog.FileName : null;
            }
            finally
            {
                elementWithFocus?.Focus();
            }
        }

        private ResolveEmbeddedFileDialogResult PromptForEmbeddedFile(string sarifLogFilePath, bool hasEmbeddedContent, ConcurrentDictionary<string, ResolveEmbeddedFileDialogResult> userPreference)
        {
            // Opening the OpenFileDialog causes the TreeView to lose focus,
            // which in turn causes the TreeViewItem selection to be unpredictable
            // (because the selection event relies on the TreeViewItem focus.)
            // We'll save the element which currently has focus and then restore
            // focus after the OpenFileDialog is closed.
            var elementWithFocus = Keyboard.FocusedElement as UIElement;

            try
            {
                ResolveEmbeddedFileDialogResult dialogResult;
                if (userPreference.TryGetValue(sarifLogFilePath, out ResolveEmbeddedFileDialogResult preference) &&
                    preference != ResolveEmbeddedFileDialogResult.None &&

                    // if preference is OpenEmbeddedFileContent but this result has no embedded content, should ignore this preference
                    !(!hasEmbeddedContent && preference == ResolveEmbeddedFileDialogResult.OpenEmbeddedFileContent))
                {
                    dialogResult = preference;
                }
                else
                {
                    var dialog = new ResolveEmbeddedFileDialog(hasEmbeddedContent);
                    dialog.ShowModal();
                    dialogResult = dialog.Result;
                    if (dialog.ApplyUserPreference)
                    {
                        userPreference.AddOrUpdate(sarifLogFilePath, dialogResult, (key, value) => dialogResult);
                    }
                }

                return dialogResult;
            }
            finally
            {
                elementWithFocus?.Focus();
            }
        }

        // Find the common suffix between two paths by walking both paths backwards
        // until they differ or until we reach the beginning.
        private static string GetCommonSuffix(string firstPath, string secondPath)
        {
            string commonSuffix = null;

            int firstSuffixOffset = firstPath.Length;
            int secondSuffixOffset = secondPath.Length;

            while (firstSuffixOffset > 0 && secondSuffixOffset > 0)
            {
                firstSuffixOffset = firstPath.LastIndexOf('\\', firstSuffixOffset - 1);
                secondSuffixOffset = secondPath.LastIndexOf('\\', secondSuffixOffset - 1);

                if (firstSuffixOffset == -1 || secondSuffixOffset == -1)
                {
                    break;
                }

                string firstSuffix = firstPath.Substring(firstSuffixOffset);
                string secondSuffix = secondPath.Substring(secondSuffixOffset);

                if (!secondSuffix.Equals(firstSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                commonSuffix = firstSuffix;
            }

            return commonSuffix;
        }

        private void RemoveTemporaryFiles()
        {
            // User is closing the solution (or VS), so remove temporary directory.
            try
            {
                if (Directory.Exists(this.temporaryFilePath))
                {
                    var dir = new DirectoryInfo(this.temporaryFilePath) { Attributes = FileAttributes.Normal };

                    foreach (FileSystemInfo info in dir.GetFileSystemInfos("*", SearchOption.AllDirectories))
                    {
                        // Clear any read-only attributes
                        info.Attributes = FileAttributes.Normal;
                    }

                    dir.Refresh();
                    dir.Delete(true);
                }
            }

            // Delete failed, no harm in leaving it this way and continuing
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }

        // Expose the path prefix remapping to unit tests.
        internal Tuple<string, string>[] GetRemappedPathPrefixes()
        {
            // Unit tests will only create one cache.
            return this.RunIndexToRunDataCache.Values.First().RemappedPathPrefixes.ToArray();
        }

        // Extract selected results from original SarifLog.
        internal SarifLog GetPartitionedLog(IEnumerable<SarifErrorListItem> listItems)
        {
            int runIndex = -1;
            string guid = Guid.NewGuid().ToString();
            foreach (SarifErrorListItem item in listItems)
            {
                if (item.SarifResult != null)
                {
                    item.SarifResult.Guid = guid;
                    if (runIndex == -1)
                    {
                        runIndex = item.RunIndex;
                    }
                }
            }

            if (runIndex == -1 || !this.RunIndexToRunDataCache.TryGetValue(runIndex, out RunDataCache dataCache) || dataCache.SarifLog == null)
            {
                return null;
            }

            // parition results in log
            PartitionFunction<string> partitionFunction = (result) => result.Guid ?? null;
            var partitioningVisitor = new PartitioningVisitor<string>(partitionFunction, deepClone: false);
            partitioningVisitor.VisitSarifLog(dataCache.SarifLog);
            Dictionary<string, SarifLog> partitions = partitioningVisitor.GetPartitionLogs();
            return partitions[guid];
        }

        internal void AddSuppressionToSarifLog(SuppressionModel suppressionModel)
        {
            if (suppressionModel?.SelectedErrorListItems?.Any() != true)
            {
                return;
            }

            int runIndex = -1;
            bool suppressionAdded = false;

            foreach (SarifErrorListItem item in suppressionModel.SelectedErrorListItems)
            {
                runIndex = runIndex == -1 ? item.RunIndex : runIndex;
                if (item.SarifResult != null)
                {
                    if (item.SarifResult.Suppressions == null)
                    {
                        item.SarifResult.Suppressions = new List<Suppression>();
                    }

                    var suppression = new Suppression
                    {
                        Status = suppressionModel.Status,
                        Kind = SuppressionKind.External,
                    };

                    item.SarifResult.Suppressions.Add(suppression);
                    suppressionAdded = true;
                }
            }

            if (runIndex == -1 ||
                !this.RunIndexToRunDataCache.TryGetValue(runIndex, out RunDataCache dataCache) ||
                dataCache.SarifLog == null)
            {
                return;
            }

            if (suppressionAdded)
            {
                // add empty suppression for results don't have suppression
                // this is to satisfy sarif spec: either all results have non-null suppressions or have no suppressions
                // spec link: https://docs.oasis-open.org/sarif/sarif/v2.1.0/os/sarif-v2.1.0-os.html#_Toc34317661
                // "The suppressions values for all result objects in theRun SHALL be either all null or all non-null.
                // "NOTE: The rationale is that an engineering system will generally evaluate all results for suppression, or none of them.Requiring that the suppressions values be either all null or all non - null enables a consumer to determine whether suppression information is available for the run by examining a single result object."
                foreach (SarifErrorListItem errorListItem in dataCache.SarifErrors)
                {
                    if (errorListItem.SarifResult.Suppressions == null)
                    {
                        errorListItem.SarifResult.Suppressions = Array.Empty<Suppression>();
                    }
                }

                var serializer = new JsonSerializer()
                {
                    Formatting = Formatting.Indented,
                };

                using (var writer = new JsonTextWriter(
                    new StreamWriter(this._fileSystem.FileCreate(dataCache.LogFilePath))))
                {
                    serializer.Serialize(writer, dataCache.SarifLog);
                }
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

        internal bool TryResolveFilePathFromSolution(string solutionPath, string pathFromLogFile, IFileSystem fileSystem, out string resolvedPath)
        {
            resolvedPath = null;
            if (string.IsNullOrWhiteSpace(solutionPath))
            {
                return false;
            }

            try
            {
                solutionPath = fileSystem.FileExists(solutionPath) ? Path.GetDirectoryName(solutionPath) : solutionPath;
                if (!fileSystem.DirectoryExists(solutionPath))
                {
                    return false;
                }

                pathFromLogFile = NormalizeFilePath(pathFromLogFile);
                string fileToSearch = Path.GetFileName(pathFromLogFile);
                IEnumerable<string> searchResults = fileSystem.DirectoryEnumerateFiles(solutionPath, fileToSearch, SearchOption.AllDirectories);
                searchResults = searchResults.Where(path => path.EndsWith(pathFromLogFile, StringComparison.OrdinalIgnoreCase));

                // if path like "\AssemblyInfo.cs" it may exists in many projects.
                // Here try to find a unique file matching the path,
                // if more than 1 files match the path, we cannot decide which file to select, need manual intervention
                IEnumerator<string> searchResultsEnumerator = searchResults.GetEnumerator();
                if (searchResultsEnumerator.MoveNext())
                {
                    string currentResolvedPath = searchResultsEnumerator.Current;

                    // If there is another entry, the user must pick one.
                    if (searchResultsEnumerator.MoveNext())
                    {
                        return false;
                    }

                    resolvedPath = currentResolvedPath;
                    return true;
                }
            }
            catch (Exception ex) when (ex is ArgumentException ||
                                       ex is IOException ||
                                       ex is UnauthorizedAccessException)
            {
                // do not throw exception so that it keeps trying to resolve path the next
                Trace.WriteLine($"{nameof(TryResolveFilePathFromSolution)} threw exception: {ex}");
            }

            return false;
        }

        internal bool TryResolveFilePathFromUriBasePaths(string uriBaseId, string pathFromLogFile, RunDataCache dataCache, out Uri relativeUri, out string resolvedPath)
        {
            relativeUri = null;
            resolvedPath = null;
            if (!string.IsNullOrEmpty(uriBaseId) && Uri.TryCreate(pathFromLogFile, UriKind.Relative, out relativeUri))
            {
                // If the relative file path is relative to an unknown root,
                // we need to strip the leading slash, so that we can relate
                // the file path to an arbitrary remapped disk location.
                if (pathFromLogFile.StartsWith("/"))
                {
                    pathFromLogFile = pathFromLogFile.Substring(1);
                }

                if (dataCache.RemappedUriBasePaths.TryGetValue(uriBaseId, out Uri baseUri))
                {
                    pathFromLogFile = new Uri(baseUri, pathFromLogFile).LocalPath;
                }
                else if (dataCache.OriginalUriBasePaths.TryGetValue(uriBaseId, out Uri originalBaseUri))
                {
                    pathFromLogFile = new Uri(originalBaseUri, pathFromLogFile).LocalPath;
                }

                if (this._fileSystem.FileExists(pathFromLogFile))
                {
                    resolvedPath = pathFromLogFile;
                    return true;
                }
            }

            return false;
        }

        internal bool TryResolveFilePathFromRemappings(string pathFromLogFile, RunDataCache dataCache, out string resolvedPath)
        {
            resolvedPath = null;

            // Traverse our remappings and see if we can
            // make rebaseline from existing data
            pathFromLogFile = NormalizeFilePath(pathFromLogFile);
            foreach (Tuple<string, string> remapping in dataCache.RemappedPathPrefixes)
            {
                string remapped;
                if (!string.IsNullOrEmpty(remapping.Item1))
                {
                    remapped = pathFromLogFile.Replace(remapping.Item1, remapping.Item2);
                }
                else
                {
                    remapped = Path.Combine(remapping.Item2, pathFromLogFile);
                }

                if (this._fileSystem.FileExists(remapped))
                {
                    resolvedPath = remapped;
                    return true;
                }
            }

            return false;
        }

        internal bool TryResolveFilePathFromSourceControl(IList<VersionControlDetails> sources, string pathFromLogFile, string workingDirectory, IFileSystem fileSystem, out string resolvedPath)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            resolvedPath = null;
            if (sources == null || !sources.Any())
            {
                return false;
            }

            foreach (VersionControlDetails versionControl in sources)
            {
                string localFilePath;

                // check if file exists in mapped location
                Uri mapToPath = versionControl.MappedTo?.Uri;
                if (mapToPath != null)
                {
                    localFilePath = new Uri(mapToPath, pathFromLogFile).LocalPath;
                    if (fileSystem.FileExists(localFilePath))
                    {
                        resolvedPath = localFilePath;
                        return true;
                    }
                }

                // try to read from remote repo
                Uri soureFileFromRepo = null;
                string localRelativePath = null;
                if (VersionControlParserFactory.TryGetVersionControlParser(versionControl, out IVersionControlParser parser))
                {
                    soureFileFromRepo = parser.GetSourceFileUri(pathFromLogFile);
                    localRelativePath = parser.GetLocalRelativePath(soureFileFromRepo, pathFromLogFile);
                }

                if (soureFileFromRepo != null)
                {
                    localFilePath = this.HandleHttpFileDownloadRequest(soureFileFromRepo, workingDirectory, localRelativePath);
                    if (fileSystem.FileExists(localFilePath))
                    {
                        resolvedPath = localFilePath;
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool SaveResolvedPathToUriBaseMapping(string uriBaseId, string originalPath, string pathFromLogFile, string resolvedPath, RunDataCache dataCache)
        {
            Uri.TryCreate(pathFromLogFile, UriKind.Relative, out Uri relativeUri);
            if (Uri.TryCreate(pathFromLogFile, UriKind.Absolute, out Uri absoluteUri))
            {
                if (absoluteUri.IsHttpScheme())
                {
                    // since result's path is full url path, it has no common part of local files
                    return true;
                }
            }
            else
            {
                // if path is relative path, add '/' at beginning
                if (!pathFromLogFile.StartsWith("/"))
                {
                    pathFromLogFile = "/" + pathFromLogFile;
                }
            }

            string commonSuffix = GetCommonSuffix(NormalizeFilePath(pathFromLogFile), resolvedPath);
            if (commonSuffix == null)
            {
                return false;
            }

            // Trim the common suffix from both paths, and add a remapping that converts
            // one prefix to the other.
            string originalPrefix = pathFromLogFile.Substring(0, pathFromLogFile.Length - commonSuffix.Length);
            string resolvedPrefix = resolvedPath.Substring(0, resolvedPath.Length - commonSuffix.Length);

            int uriBaseIdEndIndex = resolvedPath.IndexOf(NormalizeFilePath(originalPath));

            if (!string.IsNullOrEmpty(uriBaseId) && relativeUri != null && uriBaseIdEndIndex >= 0)
            {
                // If we could determine the uriBaseId substitution value, then add it to the map.
                dataCache.RemappedUriBasePaths[uriBaseId] = new Uri(resolvedPath.Substring(0, uriBaseIdEndIndex), UriKind.Absolute);
            }
            else
            {
                // If there's no relativeUri/uriBaseId pair or we couldn't determine the uriBaseId value,
                // map the original prefix to the new prefix.
                dataCache.RemappedPathPrefixes.Add(new Tuple<string, string>(originalPrefix, resolvedPrefix));
            }

            return true;
        }

        // return false means cannot resolve local file and will use embedded file.
        internal bool VerifyFileWithArtifactHash(SarifErrorListItem sarifErrorListItem, string pathFromLogFile, RunDataCache dataCache, string resolvedPath, string embeddedTempFilePath, out string newResolvedPath)
        {
            newResolvedPath = null;

            if (string.IsNullOrEmpty(resolvedPath))
            {
                // cannot find corresponding file in local, then use embedded file
                newResolvedPath = embeddedTempFilePath;
                return true;
            }

            if (!dataCache.FileDetails.TryGetValue(pathFromLogFile, out ArtifactDetailsModel fileData))
            {
                // has no embedded file, return the path resolved till now
                newResolvedPath = resolvedPath;
                return true;
            }

            string fileHash = this.GetFileHash(this._fileSystem, resolvedPath);

            if (fileHash.Equals(fileData.Sha256Hash, StringComparison.OrdinalIgnoreCase))
            {
                // found a file in file system which has same hashcode as embeded content.
                newResolvedPath = resolvedPath;
                return true;
            }

            bool hasEmbeddedContent = !string.IsNullOrEmpty(embeddedTempFilePath);
            ResolveEmbeddedFileDialogResult dialogResult = this._promptForEmbeddedFileDelegate(sarifErrorListItem.LogFilePath, hasEmbeddedContent, this.userDialogPreference);

            switch (dialogResult)
            {
                case ResolveEmbeddedFileDialogResult.None:
                    // dialog is cancelled.
                    newResolvedPath = null;
                    return false;
                case ResolveEmbeddedFileDialogResult.OpenEmbeddedFileContent:
                    newResolvedPath = embeddedTempFilePath;
                    return true;
                case ResolveEmbeddedFileDialogResult.OpenLocalFileFromSolution:
                    newResolvedPath = resolvedPath;
                    return true;
                case ResolveEmbeddedFileDialogResult.BrowseAlternateLocation:
                    // if returns null means user cancelled the open file dialog.
                    newResolvedPath = this._promptForResolvedPathDelegate(sarifErrorListItem, pathFromLogFile);
                    return !string.IsNullOrEmpty(newResolvedPath);
                default:
                    return false;
            }
        }

        private string GetFileHash(IFileSystem fileSystem, string filePath)
        {
            if (!fileSystem.FileExists(filePath))
            {
                return null;
            }

            using (Stream stream = fileSystem.FileOpenRead(filePath))
            {
                return HashHelper.GenerateHash(stream);
            }
        }

        private static string NormalizeFilePath(string path)
        {
            return path?.Replace('/', Path.DirectorySeparatorChar);
        }

        internal static string GetSolutionPath(DTE2 dte, IVsFolderWorkspaceService workspaceService)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            // Check to see if this is an "Open Folder" scenario where there is no ".sln" file.
            string solutionPath = workspaceService?.CurrentWorkspace?.Location;

            if (!string.IsNullOrEmpty(solutionPath))
            {
                return solutionPath;
            }

            // If we don't have an open folder situation, then we assume there is a ".sln" file.
            // When VS opens a file instead of a solution/folder, it creates a temporary solution "Solution1"
            // and its opened but FullName is empty
            if (dte?.Solution != null && dte.Solution.IsOpen && !string.IsNullOrWhiteSpace(dte.Solution.FullName))
            {
                solutionPath = Path.GetDirectoryName(dte.Solution.FullName);
            }

            return solutionPath;
        }

        internal void AddAllowedFileExtension(string fileExtension)
        {
            if (!this._allowedFileExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
            {
                this._allowedFileExtensions.Add(fileExtension);
                SdkUIUtilities.StoreObject<HashSet<string>>(this._allowedFileExtensions, AllowedFileExtensionsFileName);
            }
        }

        internal HashSet<string> GetAllowedFileExtensions()
        {
            return this._allowedFileExtensions;
        }
    }
}
