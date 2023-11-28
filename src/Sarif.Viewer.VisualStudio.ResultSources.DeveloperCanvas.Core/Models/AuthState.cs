// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using System.Collections;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Models
{
    /// <summary>
    /// This class is a singleton that is the single point of truth as to if the user is logged in or not, and if they have actively refused to log in.
    /// </summary>
    internal class AuthState
    {
        private const string refusedLoginSettingString = $"{nameof(DevCanvasResultSourceService)}-refusedLogin";
        private bool? _refusedLogin;

        /// <summary>
        /// This flag is set to true when the user has intentionally refused to authenticate. In this case, we do not want to query for insights or continue to show auth popups.
        /// </summary>
        public bool RefusedLogin
        {
            get
            {
                if (_refusedLogin == null)
                {
                    return false;
                } 
                return _refusedLogin.Value;
            }
            set
            {
                settingsStore.SetBoolean(nameof(AuthState), refusedLoginSettingString, value);
                _refusedLogin = value;
            }
        }

        /// <summary>
        /// The name of the file on the users file system that has the msal settings cached.
        /// </summary>
        public const string msalCacheFileName = $"{nameof(DevCanvasResultSourceService)}_MSAL_cache.txt";

        /// <summary>
        /// The absolute path of the file on the users file system that has the msal settings cached.
        /// </summary>
        public readonly string msalCacheFilePath = Path.Combine(MsalCacheHelper.UserRootDirectory, msalCacheFileName);

        /// <summary>
        /// Determines if the user is logged in with a @microsoft.com account.
        /// </summary>
        public bool IsLoggedIntoDevCanvas
        { 
            get
            {
                return File.Exists(msalCacheFilePath);
            }
        }

        private readonly WritableSettingsStore settingsStore;

        private AuthState()
        {
            try
            {
                // IVsSettingsManager settingsManager = (IVsSettingsManager)ServiceProvider.GlobalProvider.GetService(typeof(SVsSettingsManager));
                string collectionPath = string.Format("Extensions\\{0}\\{1}", "b97edb99-282e-444c-8f53-7de237f2ec5e", "AuthState");
                // settingsManager.CreateCollection(collectionPath);

                // Get the settings manager for the current user
                SettingsManager settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);

                // Get the writable settings store for your extension
                settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

                // Save a setting

                if (!settingsStore.CollectionExists(collectionPath))
                {
                    settingsStore.CreateCollection(collectionPath);
                }

                _refusedLogin = settingsStore.GetBoolean(collectionPath, refusedLoginSettingString, false);
            }
            catch (Exception)
            { 
                // swallow
                // TODO: send telemetry when this happens to prevent.
            }
        }

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
