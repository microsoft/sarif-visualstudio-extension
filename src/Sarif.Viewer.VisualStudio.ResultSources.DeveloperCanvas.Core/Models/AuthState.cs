// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.Identity.Client.Extensions.Msal;

using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Services;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Models
{
    internal class AuthState
    {
        /// <summary>
        /// This flag is set to true when the user has intentionally refused to authenticate. In this case, we do not want to query for insights or continue to show auth popups.
        /// </summary>
        public bool RefusedLogin { get; set; }


        /// <summary>
        /// The name of the file on the users file system that has the msal settings cached.
        /// </summary>
        public const string msalCacheFileName = $"{nameof(DevCanvasResultSourceService)}_MSAL_cache.txt";

        /// <summary>
        /// The absolute path of the file on the users file system that has the msal settings cached.
        /// </summary>
        public readonly string msalCacheFilePath = Path.Combine(MsalCacheHelper.UserRootDirectory, msalCacheFileName);

        /// <summary>
        /// Determines if the user is logged in with a @microsoft.com account
        /// </summary>
        public bool IsLoggedIntoDevCanvas
        { 
            get
            {
                return File.Exists(msalCacheFilePath);
            }
        }

        private AuthState() { }

        public static AuthState Instance;

        public static bool isInitialized = false;
        public static void Initialize()
        {
            if (isInitialized)
            {
                throw new Exception("Already initialized!");
            }
            Instance = new AuthState();
        }
    }
}
