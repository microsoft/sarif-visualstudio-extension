// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core
{
    /// <summary>
    /// Set of useful helper functions for this project.
    /// </summary>
    internal static class Util
    {
        private const string sdIniFileName = "sd.ini";

        /// <summary>
        /// Returns a string with the given count and pluralized text.
        /// E.g. if text = "car" and count = 3 then this returns "3 cars".
        /// </summary>
        /// <param name="text">The text to pluralize.</param>
        /// <param name="count">The number of items that "text" represents.</param>
        /// <returns>The string with approrpiate plural ending</returns>
        public static string S(string text, int count)
        {
            if (count != 1)
            {
                if (text.EndsWith("ch"))
                {
                    text += "es";
                }
                else
                {
                    text += "s";
                }
            }

            return $"{count} {text}";
        }

        /// <summary>
        /// Returns the code base root along with information about if it is a source depot code base.
        /// </summary>
        /// <param name="filePath">File path of a file in the code base</param>
        /// <param name="rootDir">The root of the code base</param>
        /// <returns>True if this file is part of a source depot code base.</returns>
        public static bool IsSourceDepot(string filePath, out string rootDir)
        {
            rootDir = null;

            try
            {
                rootDir = Path.GetDirectoryName(filePath);
                while (!string.IsNullOrEmpty(rootDir))
                {
                    if (File.Exists(Path.Combine(rootDir, sdIniFileName)))
                    {
                        return true;
                    }
                    rootDir = Path.GetDirectoryName(rootDir);
                }
            }
            catch (Exception)
            {
                // Swallow any exceptions that happen here. This is most likely due to the filePath being invalid.
            }
            return false;
        }

        /// <summary>
        /// Parses the server, project, and repository out of a full git-based repository URL.
        /// E.g. if <paramref name="gitUrl"/> = "https://dev.azure.com/serverName/projectName/_git/repoName" then
        /// <paramref name="gitServer"/> = "https://dev.azure.com/serverName",
        /// <paramref name="projectName"/> = "projectName", and
        /// <paramref name="gitRepositoryName"/> = "repoName".
        /// Note that some URLs, like GitHub, may not have a <paramref name="projectName"/>.
        /// It is the caller's responsibility to check the validity of each extracted part.
        /// </summary>
        /// <param name="gitUrl">The git url to be parsed. Ex: https://dev.azure.com/serverName/projectName/_git/repoName</param>
        /// <param name="gitServer">The server of the repo Ex: dev.azure.com/serverName</param>
        /// <param name="projectName">The project name of the repo Ex: OS.Fun</param>
        /// <param name="gitRepositoryName">The name of the repo Ex: devcanvas</param>
        public static void ParseGitUrl(string gitUrl, out string gitServer, out string projectName, out string gitRepositoryName)
        {
            projectName = null;
            gitServer = null;
            gitRepositoryName = null;

            if (gitUrl.Contains("ssh")) //example url: serverName@vs-ssh.visualstudio.com:v3/serverName/projectname/repoName , git@ssh.dev.azure.com:v3/serverName/projectName/repoName
            {
                if (gitUrl.Contains("vs-ssh.visualstudio.com"))
                {
                    Regex r = new Regex(@"(^[0-9,A-z]*@vs-ssh\.visualstudio\.com:[0-9,A-z]*/)([0-9,A-z]*)/([0-9,A-z]*)/([0-9,A-z]*)$");
                    Match match = r.Match(gitUrl);
                    if (match.Success && match.Groups.Count == 5)
                    {
                        string serverName = match.Groups[2].Value;
                        gitServer = $"{serverName}.visualstudio.com";
                        projectName = match.Groups[3].Value;
                        gitRepositoryName = match.Groups[4].Value;
                    }
                }
                else if (gitUrl.Contains("ssh.dev.azure.com"))
                {
                    Regex r = new Regex(@"(^git@ssh\.dev\.azure\.com:[0-9,A-z]*)/([0-9,A-z,%]*)/([0-9,A-z,%]*)/([0-9,A-z,%]*)$");
                    Match match = r.Match(gitUrl);
                    if (match.Success && match.Groups.Count == 5)
                    {
                        string serverName = match.Groups[2].Value;
                        gitServer = $"dev.azure.com/{serverName}";
                        projectName = match.Groups[3].Value;
                        gitRepositoryName = match.Groups[4].Value;
                    }
                }
                else
                {
                    return;
                }

            }
            else if (gitUrl.Contains("git")) //example url: https://serverName.visualstudio.com/DefaultCollection/projectName/_git/repoName
            {
                // handle URLs of format:
                //      https://github.com/Microsoft/Windows-universal-samples.git 
                //      git@github.com:Microsoft/ChakraCore-Debugger.git
                //      \\\\analogfs\\private\\AnalogSX\\GT\\ObjectLock\\ForNing\\Src\\MRTK.git
                if (gitUrl.EndsWith(".git"))
                {
                    // The URL contains project names and repository names which can alphabets, number and special characters, so regex matching is tricky
                    string[] parts = gitUrl.Split(new string[] { "https://", "/", ":", ".git" }, StringSplitOptions.RemoveEmptyEntries);

                    // Nothing to parse, return.
                    if (parts.Length == 0)
                    {
                        return;
                    }

                    // handle the case when git repo is hosted on a file share
                    if (gitUrl.StartsWith(@"\\"))
                    {
                        parts[0] = Regex.Unescape(parts[0]);
                        int indx = parts[0].LastIndexOf(@"\");
                        gitServer = parts[0].Substring(0, indx);
                        gitRepositoryName = parts[0].Substring(indx + 1);
                    }
                    else
                    {
                        gitServer = parts[0];
                        if (parts.Length == 2)
                        {
                            gitRepositoryName = parts[1];
                        }
                        else if (parts.Length == 3)
                        {
                            projectName = parts[1];
                            gitRepositoryName = parts[2];
                        }
                        else
                        {
                            // ideally we should not reach this code path, but in case there is an unexpected Git repo URL,
                            // set the repository name to null
                            gitRepositoryName = null;
                        }

                        // If the server looks like git@github.com, extract the github.com part.
                        if (gitServer.Contains("@"))
                        {
                            parts = gitServer.Split('@');
                            gitServer = parts.Length == 2 ? parts[1] : gitServer;
                        }
                    }
                }
                // handle the following URL formats to project to <"server", "project", "repo">
                //      https://dev.azure.com/serverName/_git/repoName to <"dev.azure.com/serverName", "projectName", "repoName">
                //      https://dev.azure.com/serverName/projectName/_git/repoName to <"dev.azure.com/serverName", "projectName", "repoName">
                //      https://llvm.org/git/llvm to <"llvm.org", "", "llvm">
                //      https://serverName.visualstudio.com/defaultcollection/projectName/_git/repoName to <"serverName.visualstudio.com", "projectName", "repoName">
                //      https://serverName.visualstudio.com/projectName/_git/repoName to <"serverName.visualstudio.com", "projectName", "repoName">
                else if (gitUrl.Contains(".com") || gitUrl.Contains(".org"))
                {
                    // only look at server/project/repo portion of URL
                    gitUrl = gitUrl.Contains("?") ? gitUrl.Substring(0, gitUrl.IndexOf("?")) : gitUrl;

                    // Remove "defaultcollection/" 
                    var regex = new Regex("defaultcollection/", RegexOptions.IgnoreCase);
                    gitUrl = regex.Replace(gitUrl, string.Empty);

                    // dev.azure.com uses "/" in server paths
                    // Use a replacer to change and change back for server, but could be used for other parts as needed
                    List<KeyValuePair<string, string>> delimiterReplacementsForServer = new List<KeyValuePair<string, string>>()
                    {
                        { new KeyValuePair<string, string>("dev.azure.com/", "dev.azure.com_") }
                    };
                    foreach (KeyValuePair<string, string> replacer in delimiterReplacementsForServer)
                    {
                        gitUrl = gitUrl.Replace(replacer.Key, replacer.Value);
                    }

                    // Some URLs have "_git" instead of "git", modify the set of delimiters accordingly.
                    List<string> delimiters = new List<string> { "https://", "/" };
                    if (gitUrl.Contains("_git"))
                    {
                        delimiters.Add("_git");
                    }
                    else
                    {
                        delimiters.Add("git");
                    }

                    string[] parts = gitUrl.Split(delimiters.ToArray(), StringSplitOptions.RemoveEmptyEntries);

                    // Nothing to parse, return.
                    if (parts.Length == 0)
                    {
                        return;
                    }

                    gitServer = parts[0];
                    foreach (KeyValuePair<string, string> replacer in delimiterReplacementsForServer)
                    {
                        gitServer = gitServer.Replace(replacer.Value, replacer.Key);
                    }
                    if (parts.Length == 3)
                    {
                        projectName = parts[1];
                        gitRepositoryName = parts[2];
                    }
                    else if (parts.Length == 2)
                    {
                        gitRepositoryName = parts[1];
                        // For Microsoft hosted repos - if there is no project in the url, it matches the repo name
                        if (gitServer.Contains("dev.azure.com") || gitServer.Contains(".visualstudio.com"))
                        {
                            projectName = parts[1];
                        }
                    }
                }
                else if (gitUrl.Contains("_git"))
                {
                    string[] parts = gitUrl.Split(new string[] { "https://", "/_git/" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        gitServer = parts[0];
                        gitRepositoryName = parts[1];
                    }
                }
            }
        }

        private static readonly ReaderWriterLockSlim extensionVersionLock = new ReaderWriterLockSlim();
        private static string extensionVersion;
        /// <summary>
        /// The version of this extension.
        /// </summary>
        public static string ExtensionVersion
        {
            get
            {
                try
                {
                    // Using a reader/writer lock to keep things fast when there is high contention for this value.
                    extensionVersionLock.EnterUpgradeableReadLock();

                    if (string.IsNullOrEmpty(extensionVersion))
                    {
                        try
                        {
                            extensionVersionLock.EnterWriteLock();

                            var assembly = Assembly.GetExecutingAssembly();
                            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                            extensionVersion = fvi.FileVersion;
                        }
                        finally
                        {
                            extensionVersionLock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    extensionVersionLock.ExitUpgradeableReadLock();
                }

                return extensionVersion;
            }
        }
    }
}
