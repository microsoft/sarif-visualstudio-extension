// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// This class is responsible for coordinating Code Analysis results end-to-end from the underlying
    /// implementation to the user interface activities.
    /// </summary>
    [Guid("4494F79A-6E9F-45EA-895B-7AE959B94D6A")]
    internal sealed class CodeAnalysisResultManager : IVsSolutionEvents, IVsUpdateSolutionEvents2
    {
        internal const int E_FAIL = unchecked((int)0x80004005);
        internal const uint VSCOOKIE_NIL = 0;
        internal const int S_OK = 0;
        private const string AllowedDownloadHostsFileName = "AllowedDownloadHosts.json";
        private const string TemporaryFileDirectoryName = "SarifViewer";
        private readonly string TemporaryFilePath;

        // Cookies for registration and unregistration
        private uint m_updateSolutionEventsCookie;
        private uint m_solutionEventsCookie;
        private List<string> _allowedDownloadHosts;

        private readonly IFileSystem _fileSystem;

        internal delegate string PromptForResolvedPathDelegate(string pathFromLogFile);
        readonly PromptForResolvedPathDelegate _promptForResolvedPathDelegate;

        // This ctor is internal rather than private for unit test purposes.
        internal CodeAnalysisResultManager(
            IFileSystem fileSystem,
            PromptForResolvedPathDelegate promptForResolvedPathDelegate = null)
        {
            _fileSystem = fileSystem;
            _promptForResolvedPathDelegate = promptForResolvedPathDelegate ?? PromptForResolvedPath;

            _allowedDownloadHosts = SdkUIUtilities.GetStoredObject<List<string>>(AllowedDownloadHostsFileName) ?? new List<string>();

            // Get temporary path for embedded files.
            TemporaryFilePath = Path.GetTempPath();
            TemporaryFilePath = Path.Combine(TemporaryFilePath, TemporaryFileDirectoryName);
        }

        public static CodeAnalysisResultManager Instance = new CodeAnalysisResultManager(new FileSystem());

        public IDictionary<int, RunDataCache> RunIndexToRunDataCache { get; } = new Dictionary<int, RunDataCache>();

        /// <summary>
        /// Returns the last index given out by <see cref="GetNextRunIndex"/>.
        /// </summary>
        /// <remarks>
        /// Currently on referenced for testing.
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
                RunIndexToRunDataCache.TryGetValue(CurrentRunIndex, out RunDataCache dataCache);
                return dataCache;
            }
        }

        SarifErrorListItem m_currentSarifError;
        public SarifErrorListItem CurrentSarifResult
        {
            get
            {
                return m_currentSarifError;
            }
            set
            {
                m_currentSarifError = value;
            }
        }


        internal void Register()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Register this object to listen for IVsUpdateSolutionEvents
            IVsSolutionBuildManager2 buildManager = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager2;
            if (buildManager == null)
            {
                throw Marshal.GetExceptionForHR(E_FAIL);
            }
            buildManager.AdviseUpdateSolutionEvents(this, out m_updateSolutionEventsCookie);

            // Register this object to listen for IVsSolutionEvents
            IVsSolution solution = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            if (solution == null)
            {
                throw Marshal.GetExceptionForHR(E_FAIL);
            }
            solution.AdviseSolutionEvents(this, out m_solutionEventsCookie);
        }

        /// <summary>
        /// Unregister this provider from VS
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
        internal void Unregister()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Unregister this object from IVsUpdateSolutionEvents events
            if (m_updateSolutionEventsCookie != VSCOOKIE_NIL)
            {

                IVsSolutionBuildManager2 buildManager = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolutionBuildManager))  as IVsSolutionBuildManager2;
                if (buildManager != null)
                {
                    buildManager.UnadviseUpdateSolutionEvents(m_updateSolutionEventsCookie);
                    m_updateSolutionEventsCookie = VSCOOKIE_NIL;
                }
            }

            // Unregister this object from IVsSolutionEvents events
            if (m_solutionEventsCookie != VSCOOKIE_NIL)
            {
                IVsSolution solution = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) as IVsSolution;
                if (solution != null)
                {
                    solution.UnadviseSolutionEvents(m_solutionEventsCookie);
                    m_solutionEventsCookie = VSCOOKIE_NIL;
                }
            }
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            // When closing solution (or closing VS), remove the temporary folder.
            RemoveTemporaryFiles();

            return S_OK;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return S_OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            return S_OK;
        }

        public void CacheUriBasePaths(Run run)
        {
            if (run.OriginalUriBaseIds is Dictionary<string, ArtifactLocation> source)
            {
                var target = CurrentRunDataCache.OriginalUriBasePaths as Dictionary<string, Uri>;
                // This line assumes an empty dictionary
                source.ToList().ForEach(x =>
                {
                    if (x.Value.Uri != null)
                    {
                        // The URI is not required.
                        // The SARIF producer has chosen not to specify a URI. See §3.14.14, NOTE 1, for an explanation.
                        target.Add(x.Key, x.Value.Uri.WithTrailingSlash());
                    }
                }) ;
            }
        }

        public bool ResolveFilePath(int runId, string uriBaseId, string relativePath)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally
            }

            if (CurrentSarifResult == null)
            {
                return false;
            }

            RunDataCache dataCache = RunIndexToRunDataCache[runId];
            string resolvedPath = null;

            if (dataCache.FileDetails.ContainsKey(relativePath))
            {
                // File contents embedded in SARIF.
                resolvedPath = CreateFileFromContents(dataCache.FileDetails, relativePath);
            }
            else
            {

                if (uriBaseId != null
                    && dataCache.OriginalUriBasePaths.TryGetValue(uriBaseId, out Uri baseUri)
                    && Uri.TryCreate(baseUri, relativePath, out Uri uri)
                    && uri.IsHttpScheme())
                {
                    bool allow = _allowedDownloadHosts.Contains(uri.Host);

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
                                AddAllowedDownloadHost(uri.Host);
                            }
                        }
                    }

                    if (allow)
                    {
                        try
                        {
                            resolvedPath = DownloadFile(uri.ToString());
                        }
                        catch (WebException wex)
                        {
                            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
                                       Resources.DownloadFail_DialogMessage + Environment.NewLine + wex.Message,
                                       null, // title
                                       OLEMSGICON.OLEMSGICON_CRITICAL,
                                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                            return false;
                        }
                    }
                }
                else
                {
                    // User needs to locate file.
                    resolvedPath = GetRebaselinedFileName(uriBaseId, relativePath, dataCache);
                }

                if (String.IsNullOrEmpty(resolvedPath) || relativePath.Equals(resolvedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Update all the paths in this run.
            RemapFilePaths(dataCache.SarifErrors, relativePath, resolvedPath);
            return true;
        }

        // Contents are embedded in SARIF. Create a file from these contents.
        internal string CreateFileFromContents(int runId, string fileName)
        {
            return CreateFileFromContents(RunIndexToRunDataCache[runId].FileDetails, fileName);
        }

        // Contents are embedded in SARIF. Create a file from these contents.
        internal string CreateFileFromContents(IDictionary<string, ArtifactDetailsModel> fileDetailsDictionary, string fileName)
        {
            var fileData = fileDetailsDictionary[fileName];

            string finalPath = TemporaryFilePath;

            // If the file path already starts with the temporary location,
            // that means we've already built the temporary file, so we can
            // just open it.
            if (fileName.StartsWith(finalPath))
            {
                finalPath = fileName;
            }
            // Else we have to create a location under the temp path.
            else
            {
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
            
            if (!_fileSystem.FileExists(finalPath))
            {
                string contents = fileData.GetContents();
                _fileSystem.WriteAllText(finalPath, contents);
                // File should be readonly, because it is embedded.
                _fileSystem.SetAttributes(finalPath, FileAttributes.ReadOnly);
            }

            if (!fileDetailsDictionary.ContainsKey(finalPath))
            {
                // Add another key to our file data object, so that we can
                // find it if the user closes the window and reopens it.
                fileDetailsDictionary.Add(finalPath, fileData);
            }

            return finalPath;
        }

        internal void AddAllowedDownloadHost(string host)
        {
            _allowedDownloadHosts.Add(host);
            SdkUIUtilities.StoreObject<List<string>>(_allowedDownloadHosts, AllowedDownloadHostsFileName);
        }

        internal string DownloadFile(string fileUrl)
        {
            if (String.IsNullOrEmpty(fileUrl))
            {
                return fileUrl;
            }

            Uri sourceUri = new Uri(fileUrl);

            string destinationFile = Path.Combine(CurrentSarifResult.WorkingDirectory, sourceUri.LocalPath.Replace('/', '\\').TrimStart('\\'));
            string destinationDirectory = Path.GetDirectoryName(destinationFile);
            Directory.CreateDirectory(destinationDirectory);

            if (!_fileSystem.FileExists(destinationFile))
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(sourceUri, destinationFile);
                }
            }

            return destinationFile;
        }

        // Internal rather than private for unit testability.
        internal string GetRebaselinedFileName(string uriBaseId, string pathFromLogFile, RunDataCache dataCache)
        {
            string originalPath = pathFromLogFile;
            Uri relativeUri = null;

            if (!string.IsNullOrEmpty(uriBaseId) && Uri.TryCreate(pathFromLogFile, UriKind.Relative, out relativeUri))
            {
                // If the relative file path is relative to an unknown root,
                // we need to strip the leading slash, so that we can relate
                // the file path to an arbitrary remapped disk location.
                if (pathFromLogFile.StartsWith("/"))
                {
                    pathFromLogFile = pathFromLogFile.Substring(1);
                }

                if (dataCache.RemappedUriBasePaths.ContainsKey(uriBaseId))
                {
                    pathFromLogFile = new Uri(dataCache.RemappedUriBasePaths[uriBaseId], pathFromLogFile).LocalPath;
                }
                else if (dataCache.OriginalUriBasePaths.ContainsKey(uriBaseId))
                {
                    pathFromLogFile = new Uri(dataCache.OriginalUriBasePaths[uriBaseId], pathFromLogFile).LocalPath;
                }

                if (_fileSystem.FileExists(pathFromLogFile))
                {
                    return pathFromLogFile;
                }
            }

            // Traverse our remappings and see if we can
            // make rebaseline from existing data
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

                if (_fileSystem.FileExists(remapped))
                {
                    return remapped;
                }
            }

            string resolvedPath = _promptForResolvedPathDelegate(pathFromLogFile);
            if (resolvedPath == null)
            {
                return pathFromLogFile;
            }

            string fullPathFromLogFile = pathFromLogFile;
            if (Uri.TryCreate(pathFromLogFile, UriKind.Absolute, out Uri absoluteUri))
            {
                fullPathFromLogFile = Path.GetFullPath(pathFromLogFile);
            }
            else
            {
                if (!fullPathFromLogFile.StartsWith("/"))
                {
                    fullPathFromLogFile = "/" + fullPathFromLogFile;
                }
            }

            string commonSuffix = GetCommonSuffix(fullPathFromLogFile.Replace("/", @"\"), resolvedPath);
            if (commonSuffix == null)
            {
                return pathFromLogFile;
            }

            // Trim the common suffix from both paths, and add a remapping that converts
            // one prefix to the other.
            string originalPrefix = fullPathFromLogFile.Substring(0, fullPathFromLogFile.Length - commonSuffix.Length);
            string resolvedPrefix = resolvedPath.Substring(0, resolvedPath.Length - commonSuffix.Length);

            int uriBaseIdEndIndex = resolvedPath.IndexOf(originalPath.Replace("/", @"\"));

            if (relativeUri != null && uriBaseIdEndIndex >= 0)
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

            return resolvedPath;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return S_OK;
        }

        public int UpdateSolution_Cancel()
        {
            return S_OK;
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return S_OK;
        }

        public int UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            return S_OK;
        }

        internal static bool CanNavigateTo(SarifErrorListItem sarifError)
        {
            throw new NotImplementedException();
        }

        internal void RemapFilePaths(IList<SarifErrorListItem> sarifErrors, string originalPath, string remappedPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (SarifErrorListItem sarifError in sarifErrors)
            {
                sarifError.RemapFilePath(originalPath, remappedPath);
            }
        }

        public int UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            return S_OK;
        }

        private string PromptForResolvedPath(string pathFromLogFile)
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
                Title = $"Locate missing file: {pathFromLogFile}",
                InitialDirectory = Path.GetDirectoryName(CurrentSarifResult.LogFilePath),
                Filter = $"{fileName}|{fileName}",
                RestoreDirectory = true
            };

            try
            {
                bool? dialogResult = openFileDialog.ShowDialog();

                return dialogResult == true ? openFileDialog.FileName : null;
            }
            finally
            {
                if (elementWithFocus != null)
                {
                    elementWithFocus.Focus();
                }
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
                if (Directory.Exists(TemporaryFilePath))
                {
                    var dir = new DirectoryInfo(TemporaryFilePath) { Attributes = FileAttributes.Normal };

                    foreach (var info in dir.GetFileSystemInfos("*", SearchOption.AllDirectories))
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
            return RunIndexToRunDataCache.Values.First().RemappedPathPrefixes.ToArray();
        }
    }
}