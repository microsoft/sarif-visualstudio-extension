// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

using Newtonsoft.Json;

using XamlDoc = System.Windows.Documents;

namespace Microsoft.Sarif.Viewer
{
    public static class SdkUIUtilities
    {
        // Embedded link format: [link text](n|uri) where n is a non-negative integer, or uri is an absolute URL
        private const string EmbeddedLinkPattern =
@"
\[
    (?<text>
        [^\]]+
    )
\]
\(
    (?<target>
        [^)]+
    )
\)";

        /// <summary>
        /// Gets the requested service of type S from the service provider.
        /// </summary>
        /// <typeparam name="S">The service interface to retrieve.</typeparam>
        /// <param name="provider">The IServiceProvider implementation.</param>
        /// <returns>A reference to the service.</returns>
        internal static S GetService<S>(IServiceProvider provider)
            where S : class
        {
            return GetService<S, S>(provider);
        }

        /// <summary>
        /// Gets the requested service of type S and cast to type T from the service provider.
        /// </summary>
        /// <typeparam name="S">The service to retrieve.</typeparam>
        /// <typeparam name="T">The interface to cast to.</typeparam>
        /// <param name="provider">The IServiceProvider implementation.</param>
        /// <returns>A reference to the service.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "By design")]
        internal static T GetService<S, T>(IServiceProvider provider)
            where S : class
            where T : class
        {
            try
            {
                return (T)provider.GetService(typeof(S));
            }
            catch (Exception)
            {
                // If anything went wrong, just ignore it
            }

            return null;
        }

        /// <summary>
        /// Reads the contents of an isolated storage file and deserializes it to an object.
        /// </summary>
        /// <typeparam name="T">The type of the deserialized object.</typeparam>
        /// <param name="storageFileName">The isolated storage file.</param>
        /// <returns>Object deserialized from isolated storage file.</returns>
        internal static T GetStoredObject<T>(string storageFileName)
            where T : class
        {
            var store = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

            if (store.FileExists(storageFileName))
            {
                using (var stream = new IsolatedStorageFileStream(storageFileName, FileMode.Open, store))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Serializes an object and writes it to an isolated storage file.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="t">The object to serialize.</param>
        /// <param name="storageFileName">The isolated storage file.</param>
        internal static void StoreObject<T>(T t, string storageFileName)
        {
            var store = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

            using (var stream = new IsolatedStorageFileStream(storageFileName, FileMode.Create, store))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(JsonConvert.SerializeObject(t, Formatting.Indented));
                }
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "By design")]
        internal static IVsWindowFrame OpenDocument(IServiceProvider provider, string file, bool usePreviewPane)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (string.IsNullOrEmpty(file))
            {
                // No place to go
                return null;
            }

            // We should not throw exceptions if we cannot find the file
            if (!File.Exists(file))
            {
                return null;
            }

            try
            {
                if (usePreviewPane)
                {
                    // The scope below ensures that if a document is not yet open, it is opened in the preview pane.
                    // For documents that are already open, they will remain in their current pane, which may be the preview
                    // pane or the full editor pane.
                    using (new NewDocumentStateScope(__VSNEWDOCUMENTSTATE.NDS_Provisional
                        | __VSNEWDOCUMENTSTATE.NDS_NoActivate, VSConstants.NewDocumentStateReason.Navigation))
                    {
                        return OpenDocumentInCurrentScope(provider, file);
                    }
                }
                else
                {
                    return OpenDocumentInCurrentScope(provider, file);
                }
            }
            catch (COMException)
            {
                string fname = Path.GetFileName(file);
                if (System.Windows.Forms.MessageBox.Show(string.Format(Resources.FileOpenFail_DialogMessage, fname),
                                                                       Resources.FileOpenFail_DialogCaption,
                                                                       MessageBoxButtons.YesNo,
                                                                       MessageBoxIcon.Exclamation) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(Path.GetDirectoryName(file))?.Dispose();
                }

                return null;
            }
        }

        /// <summary>
        /// Open the file using the current document state scope.
        /// </summary>
        private static IVsWindowFrame OpenDocumentInCurrentScope(IServiceProvider provider, string file)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsUIShellOpenDocument openDoc = SdkUIUtilities.GetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>(provider);
            IVsRunningDocumentTable runningDocTable = SdkUIUtilities.GetService<SVsRunningDocumentTable, IVsRunningDocumentTable>(provider);
            if (openDoc == null || runningDocTable == null)
            {
                throw Marshal.GetExceptionForHR(VSConstants.E_FAIL);
            }

            uint cookieDocLock = FindDocument(runningDocTable, file);

            IVsWindowFrame windowFrame;
            Guid textViewGuid = VSConstants.LOGVIEWID_TextView;

            // Unused variables
            IVsUIHierarchy uiHierarchy;
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider;
            uint itemId;
            int hr = openDoc.OpenDocumentViaProject(file, ref textViewGuid, out serviceProvider, out uiHierarchy, out itemId, out windowFrame);
            if (ErrorHandler.Failed(hr))
            {
                throw Marshal.GetExceptionForHR(hr);
            }

            if (cookieDocLock == 0)
            {
                // Document was not open earlier, and should be open now.
                cookieDocLock = FindDocument(runningDocTable, file);
            }

            // This will make the document visible to the user and switch focus to it. ShowNoActivate doesn't help because for tabbed documents they
            // are not brought to the front if they are already opened.
            windowFrame?.Show();

            return windowFrame;
        }

        /// <summary>
        /// Find the document and return its cookie to the lock to the document.
        /// </summary>
        /// <param name="runningDocTable">The object having a table of all running documents.</param>
        /// <param name="file">The file to be looked in the documents table.</param>
        /// <returns>The cookie to the document lock.</returns>
        internal static uint FindDocument(IVsRunningDocumentTable runningDocTable, string file)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Unused variables
            IVsHierarchy hierarchy;
            uint itemId;
            IntPtr docData = IntPtr.Zero;

            uint cookieDocLock;
            int hr = runningDocTable.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, file, out hierarchy, out itemId, out docData, out cookieDocLock);

            // Although we don't use it, we still need to release the it
            if (docData != IntPtr.Zero)
            {
                Marshal.Release(docData);
                docData = IntPtr.Zero;
            }

            if (ErrorHandler.Failed(hr))
            {
                throw Marshal.GetExceptionForHR(hr);
            }

            if (cookieDocLock > 0)
            {
                // Document is already open
                uint rdtFlags;

                // Unused variables
                uint readLocks;
                uint editLocks;
                string documentName;

                hr = runningDocTable.GetDocumentInfo(cookieDocLock, out rdtFlags, out readLocks, out editLocks, out documentName, out hierarchy, out itemId, out docData);

                // Although we don't use it, we still need to release the it
                if (docData != IntPtr.Zero)
                {
                    Marshal.Release(docData);
                    docData = IntPtr.Zero;
                }

                if (ErrorHandler.Failed(hr))
                {
                    throw Marshal.GetExceptionForHR(hr);
                }

                if ((rdtFlags & ((uint)_VSRDTFLAGS.RDT_ProjSlnDocument)) > 0)
                {
                    throw Marshal.GetExceptionForHR(VSConstants.E_FAIL);
                }
            }

            return cookieDocLock;
        }

        /// <summary>
        /// Helper method for getting a IWpfTextView from a IVsTextView object.
        /// </summary>
        /// <param name="textView">a IVsTextView object.</param>
        /// <param name="wpfTextView">a IWpfTextView object.</param>
        /// <returns>If successfully gets IWpfTextView.</returns>
        public static bool TryGetWpfTextView(IVsTextView textView, out IWpfTextView wpfTextView)
        {
            wpfTextView = null;

            if (!(textView is IVsUserData userData))
            {
                return false;
            }

            Guid guid = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
            if (userData.GetData(ref guid, out object wpfTextViewHost) != VSConstants.S_OK)
            {
                return false;
            }

            if (!(wpfTextViewHost is IWpfTextViewHost textViewHost))
            {
                return false;
            }

            wpfTextView = textViewHost.TextView;

            return true;
        }

        public static bool TryGetTextViewFromFrame(IVsWindowFrame frame, out ITextView textView)
        {
            IVsTextView vsTextView = VsShellUtilities.GetTextView(frame);

            if (vsTextView == null)
            {
                textView = null;
                return false;
            }

            object textViewHost;
            Guid guidTextViewHost = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
            if (ErrorHandler.Succeeded(((IVsUserData)vsTextView).GetData(ref guidTextViewHost, out textViewHost)) &&
                textViewHost != null)
            {
                textView = ((IWpfTextViewHost)textViewHost).TextView;
                return true;
            }

            textView = null;

            return false;
        }

        public static bool TryGetFileNameFromTextBuffer(ITextBuffer textBuffer, out string filename)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            filename = null;

            if (textBuffer == null)
            {
                return false;
            }

            if (!textBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer vsTextBuffer))
            {
                return false;
            }

            if (!(vsTextBuffer is IPersistFileFormat persistFileFormat))
            {
                return false;
            }

            // IPersistFileFormat::GetCurFile clearly documents that the filename can be null (with an S_OK return value) if the document is in the "untitled" state.
            // For our uses, we require a non-empty, non-null file name.
            return persistFileFormat.GetCurFile(out filename, out uint formatIndex) == VSConstants.S_OK && !string.IsNullOrWhiteSpace(filename);
        }

        /// <summary>
        /// Attempts to locate an active "code window" view for the given text buffer.
        /// </summary>
        /// <param name="textBuffer">The text buffer for which to locate a view.</param>
        /// <param name="wpfTextView">On successful return, contains an instance of <see cref="IWpfTextView"/> that is being used to display the text buffer contents.</param>
        /// <returns>Returns true if a view can be located.</returns>
        public static bool TryGetActiveViewForTextBuffer(ITextBuffer textBuffer, out IWpfTextView wpfTextView)
        {
            wpfTextView = null;

            if (!textBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer vsTextBuffer))
            {
                return false;
            }

            if (!(Package.GetGlobalService(typeof(SVsTextManager)) is IVsTextManager2 textManager2))
            {
                return false;
            }

            if (textManager2.GetActiveView2(fMustHaveFocus: 0, pBuffer: vsTextBuffer, grfIncludeViewFrameType: (uint)_VIEWFRAMETYPE.vftCodeWindow, ppView: out IVsTextView vsTextView) != VSConstants.S_OK)
            {
                return false;
            }

            if (!SdkUIUtilities.TryGetWpfTextView(vsTextView, out wpfTextView))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Locates a tool window and request that it be shown.
        /// </summary>
        /// <param name="toolWindowGuid">A tool window identifier, typically one from <see cref="ToolWindowGuids80"/> such as the error list.</param>
        /// <param name="activate">Indicates whether to activate (take keyboard focus) when showing the tool window.</param>
        /// <returns>Returns a task that when completed indicates if the tool window was shown.</returns>
        public static async System.Threading.Tasks.Task<bool> ShowToolWindowAsync(Guid toolWindowGuid, bool activate)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (Package.GetGlobalService(typeof(SVsUIShell)) is IVsUIShell uiShell)
            {
                if (uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref toolWindowGuid, out IVsWindowFrame toolWindowFrame) == VSConstants.S_OK &&
                    toolWindowFrame != null)
                {
                    if (activate)
                    {
                        toolWindowFrame.Show();
                    }
                    else
                    {
                        toolWindowFrame.ShowNoActivate();
                    }

                    return true;
                }
            }

            return false;
        }

        private static readonly char[] s_directorySeparatorArray = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        /// <summary>
        /// Creates a relative path from one directory to another directory or file.
        /// </summary>
        /// <param name="fromDirectory">The directory that defines the start of the relative path.</param>
        /// <param name="toPath">The path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        internal static string GetRelativePath(string fromDirectory, string toPath)
        {
            // Both paths need to be rooted to calculate a relative path
            if (!Path.IsPathRooted(fromDirectory) ||
                !Path.IsPathRooted(toPath))
            {
                return toPath;
            }

            // If toPath is on a different drive then there is no relative path
            if (!string.Equals(Path.GetPathRoot(fromDirectory),
                                    Path.GetPathRoot(toPath),
                                    StringComparison.OrdinalIgnoreCase))
            {
                return toPath;
            }

            // Get the canonical path. This resolves directory names like "\.\" and "\..\".
            fromDirectory = Path.GetFullPath(fromDirectory);
            toPath = Path.GetFullPath(toPath);

            string[] fromDirectories = fromDirectory.Split(s_directorySeparatorArray, StringSplitOptions.RemoveEmptyEntries);
            string[] toDirectories = toPath.Split(s_directorySeparatorArray, StringSplitOptions.RemoveEmptyEntries);

            int length = Math.Min(fromDirectories.Length, toDirectories.Length);

            // We know at least the drive letter matches so start at index 1
            int firstDifference = 1;

            // Find the common root
            for (; firstDifference < length; firstDifference++)
            {
                if (!string.Equals(fromDirectories[firstDifference],
                                        toDirectories[firstDifference],
                                        StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            var relativePath = new StringCollection();

            // Add relative paths to get from fromDirectory to the common root
            for (int i = firstDifference; i < fromDirectories.Length; i++)
            {
                relativePath.Add("..");
            }

            // Add the relative paths from toPath
            for (int i = firstDifference; i < toDirectories.Length; i++)
            {
                relativePath.Add(toDirectories[i]);
            }

            // Create the relative path
            string[] relativeParts = new string[relativePath.Count];
            relativePath.CopyTo(relativeParts, 0);
            return string.Join(Path.DirectorySeparatorChar.ToString(), relativeParts);
        }

        /// <summary>
        /// Creates the shortest path with the greedy approach. That is, it makes the preference based on this order
        /// 1) If it's in the search paths, then just return the file name
        /// 2) If it shares the same common root with the relativePathBase, then it returns a relative path
        /// 3) Returns the full path as given.
        /// </summary>
        /// <param name="fullPath">The full path to the file.</param>
        /// <param name="searchPaths">The collection of search paths to use.</param>
        /// <param name="relativePathBase">The base path for constructing the relative paths..</param>
        /// <returns>The shortest path.</returns>
        internal static string MakeShortestPath(string fullPath, IEnumerable<string> searchPaths, string relativePathBase)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return string.Empty;
            }

            string directory = Path.GetDirectoryName(fullPath);

            if (searchPaths != null)
            {
                foreach (string searchPath in searchPaths)
                {
                    if (directory.Equals(searchPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return Path.GetFileName(fullPath);
                    }
                }
            }

            if (!string.IsNullOrEmpty(relativePathBase))
            {
                return GetRelativePath(relativePathBase, fullPath);
            }

            return fullPath;
        }

        /// <summary>
        /// This is the inverse of MakeShortestPath, it is taking a path, it may be full, relative, or just a file name, then
        /// make a full path out of it.
        /// </summary>
        /// <param name="fileName">The file, can be a full, relative, or just a file name.</param>
        /// <param name="searchPaths">The collection of search paths to use.</param>
        /// <param name="relativePathBase">The base path for resolving the path.</param>
        /// <returns>The full path.</returns>
        internal static string MakeFullPath(string fileName, IEnumerable<string> searchPaths, string relativePathBase)
        {
            // Simple case, if it's rooted, just return it
            if (Path.IsPathRooted(fileName))
            {
                return fileName;
            }

            // Check if it is in the relative directory
            if (!string.IsNullOrEmpty(relativePathBase))
            {
                // Using GetFullPath to remove any \..\ in the path
                string path = Path.GetFullPath(Path.Combine(relativePathBase, fileName));
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // If it's just a file name, search the search paths
            if (searchPaths != null && fileName == Path.GetFileName(fileName))
            {
                foreach (string searchPath in searchPaths)
                {
                    string path = Path.Combine(searchPath, fileName);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Builds a set of Inline elements from the specified message, without embedded hyperlinks.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <returns>A collection of Inline elements that represent the specified message.</returns>
        internal static List<XamlDoc.Inline> GetInlinesForErrorMessage(string message)
        {
            return GetMessageInlines(message, null);
        }

        /// <summary>
        /// Builds a set of Inline elements from the specified message, optionally with embedded hyperlinks.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <param name="clickHandler">A delegate for the Hyperlink.Click event.</param>
        /// <returns>A collection of Inline elements that represent the specified message.</returns>
        internal static List<XamlDoc.Inline> GetMessageInlines(string message, RoutedEventHandler clickHandler)
        {
            List<XamlDoc.Inline> inlines = null;
            if (!ThreadHelper.CheckAccess() && !SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD001
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    inlines = GetMessageInlinesHelper(message, clickHandler);
                });
#pragma warning disable VSTHRD001
            }
            else
            {
                inlines = GetMessageInlinesHelper(message, clickHandler);
            }

            return inlines;
        }

        private static List<XamlDoc.Inline> GetMessageInlinesHelper(string message, RoutedEventHandler clickHandler)
        {
            var inlines = new List<XamlDoc.Inline>();

            MatchCollection matches = Regex.Matches(message, EmbeddedLinkPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace);
            int start = 0;

            if (matches.Count > 0)
            {
                Group group = null;

                foreach (Match match in matches)
                {
                    group = match.Groups["text"];

                    // Add the plain text segment between the end of the last group and the current link.
                    inlines.Add(new XamlDoc.Run(UnescapeBrackets(message.Substring(start, group.Index - 1 - start))));
                    object target = null;

                    if (clickHandler != null)
                    {
                        string targetText = match.Groups["target"].Value;
                        int id;

                        if (int.TryParse(targetText, out id))
                        {
                            target = id;
                        }
                        else if (Uri.TryCreate(targetText, UriKind.Absolute, out Uri uri))
                        {
                            // This is super dangerous! We are launching URIs for SARIF logs
                            // that can point to anything.
                            // https://github.com/microsoft/sarif-visualstudio-extension/issues/171
                            target = uri;
                        }

                        if (target != null)
                        {
                            var link = new XamlDoc.Hyperlink
                            {
                                // Stash the id of the target location. This is used in SarifSnapshot.ErrorListInlineLink_Click.
                                Tag = target,
                            };

                            // Set the hyperlink text
                            link.Inlines.Add(new XamlDoc.Run($"{group.Value}"));
                            link.Click += clickHandler;

                            inlines.Add(link);
                        }
                    }

                    if (target == null)
                    {
                        // Either we don't have a click handler, or the target text wasn't a valid int or Uri.
                        // Add the link text as plain text.
                        inlines.Add(new XamlDoc.Run($"{group.Value}"));
                    }

                    start = match.Index + match.Length;
                }
            }

            if (inlines.Count > 0 && start < message.Length)
            {
                // Add the plain text segment after the last link
                inlines.Add(new XamlDoc.Run(UnescapeBrackets(message.Substring(start))));
            }

            return inlines;
        }

        /// <summary>
        /// Convert a collection of Inline elements into plain text.
        /// </summary>
        /// <param name="inlines">A collection of Inline elements that represent the message.</param>
        /// <returns>A plaint text of the message.</returns>
        internal static string GetPlainText(IEnumerable<XamlDoc.Inline> inlines)
        {
            if (inlines == null || !inlines.Any())
            {
                return null;
            }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (XamlDoc.Inline inline in inlines)
            {
                stringBuilder.Append(new XamlDoc.TextRange(inline.ContentStart, inline.ContentEnd).Text);
            }

            return stringBuilder.ToString();
        }

        internal static (string, string) SplitResultMessage(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return (input, input);
            }

            const string ellipsis = "\u2026";
            const string hyperlinkGroup = "link_text";
            string pattern = string.Format(@"\[(?<{0}>[\w \.]+)\]\(([\w\.:\/ ]*)\)", hyperlinkGroup);

            MatchCollection matches = Regex.Matches(input, pattern, RegexOptions.Multiline);
            var sb = new StringBuilder(input);

            // Replace the hyperlinks with only their text
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];

                // match.Groups.TryGetValue only available in .net 5/.net core 3.0 or above
                Group group = match.Groups[hyperlinkGroup];
                if (group != null && group.Success)
                {
                    string text = group.Value;
                    sb = sb.Remove(match.Index, match.Length);
                    sb = sb.Insert(match.Index, text);
                }
            }

            // whole string the hyperlinks are replaced with plain texts
            string plainText = AppendEndPunctuation(sb.ToString());

            string firstSentence = ExtensionMethods.GetFirstSentence(sb.ToString());

            // ExtensionMethods.GetFirstSentence has an issue it appends '.' even the string ends with other punctuations '!' or '?'
            firstSentence = AppendEndPunctuation(firstSentence.TrimEnd('.'));
            sb = new StringBuilder(firstSentence);

            bool addEllipsis = maxLength > 0 && sb.Length > maxLength;

            if (addEllipsis)
            {
                // Truncate the string
                sb.Length = maxLength;
            }

            // Replace the remaining intact links
            foreach (Match match in matches)
            {
                Group group = match.Groups[hyperlinkGroup];
                if (group != null && group.Success)
                {
                    if (maxLength > 0 && match.Index + group.Length <= sb.Length)
                    {
                        string text = group.Value;
                        sb = sb.Remove(match.Index, group.Length);
                        sb = sb.Insert(match.Index, match.Value);
                        maxLength += match.Value.Length - group.Length;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (addEllipsis)
            {
                sb.Append(ellipsis);
            }

            return (sb.ToString(), plainText);
        }

        internal static string AppendEndPunctuation(string input)
        {
            char[] endChars = { '\r', '\n', ' ', };
            char[] endPunctuations = { '.', '?', '!' };

            input.TrimEnd(endChars);
            return endPunctuations.Contains(input.Last()) ? input : input + ".";
        }

        /// <summary>
        /// Removes escape backslashes that were used to suppress embedded linking.
        /// </summary>
        /// <param name="s">The string to be processed.</param>
        /// <returns>The string has been proccessed.</returns>
        internal static string UnescapeBrackets(string s)
        {
            return s.Replace(@"\[", "[").Replace(@"\]", "]");
        }

        internal static string GetFileLocationPath(ArtifactLocation artifactLocation, int runId)
        {
            string path = null;

            if (artifactLocation?.Uri != null)
            {
                RunDataCache dataCache = CodeAnalysisResultManager.Instance.RunIndexToRunDataCache[runId];

                Uri uri = artifactLocation.Uri;

                // try to resolve path using OriginalUriBasePaths
                string uriBaseId = artifactLocation.UriBaseId;
                if (!string.IsNullOrEmpty(uriBaseId) && dataCache.OriginalUriBasePaths.ContainsKey(uriBaseId))
                {
                    Uri baseUri = dataCache.OriginalUriBasePaths[uriBaseId];
                    uri = new Uri(baseUri, uri);
                }

                try
                {
                    path = uri.LocalPath;
                }
                catch (InvalidOperationException)
                {
                    // if cannot resolve local path return original uri string
                    // it will try to resolve the path when user navigates to the error list item
                    path = uri.ToPath();
                }
            }

            return path;
        }

        internal static bool OpenExternalUrl(string uriString, int maxLength = 150)
        {
            if (!string.IsNullOrEmpty(uriString) && Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out Uri result))
            {
                uriString = uriString.Length > maxLength ? uriString.Substring(0, maxLength) + " \u2026" : uriString;
                if (System.Windows.Forms.MessageBox.Show(
                                            string.Format(Resources.OpenExternalUri_DialogMessage, Environment.NewLine + uriString),
                                            Resources.OpenExternalUri_DialogTitle,
                                            MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(uriString)?.Dispose();
                    return true;
                }
            }

            return false;
        }
    }
}
