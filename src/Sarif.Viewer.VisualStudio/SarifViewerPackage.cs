// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Services;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.CodeAnalysis.Sarif.Viewer.VisualStudio;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", VersionConstants.AssemblyVersion, IconResourceID = 400)] // Info on this package for Help/About
    [Guid(SarifViewerPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(SarifExplorerWindow), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057", Transient = true)]
    [ProvideService(typeof(SLoadSarifLogService))]
    [ProvideService(typeof(SCloseSarifLogService))]
    [ProvideService(typeof(ISarifLocationTaggerService))]
    [ProvideService(typeof(ITextViewCaretListenerService<>))]
    [ProvideService(typeof(ISarifErrorListEventSelectionService))]
    public sealed class SarifViewerPackage : AsyncPackage
    {
        /// <summary>
        /// OpenSarifFileCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "b97edb99-282e-444c-8f53-7de237f2ec5e";
        public static readonly Guid PackageGuid = new Guid(PackageGuidString);

        public static bool IsUnitTesting { get; set; } = false;

        public static Configuration AppConfig { get; private set; }

        private struct ServiceInformation
        {
            /// <summary>
            /// Function that will create an instance of this service.
            /// </summary>
            public Func<Type, object> Creator;

            /// <summary>
            /// Indicates whether to promote the service to parent service containers.
            /// </summary>
            /// <remarks>
            /// For our purposes, true indicates whether the service is visible outside this package.
            /// </remarks>
            public bool Promote;
        }

        /// <summary>
        /// Contains the list of services and their creator functions.
        /// </summary>
        private static readonly Dictionary<Type, ServiceInformation> ServiceTypeToServiceInformation = new Dictionary<Type, ServiceInformation>
        {
            { typeof(SLoadSarifLogService), new ServiceInformation { Creator = type => new LoadSarifLogService(), Promote = true } },
            { typeof(SCloseSarifLogService), new ServiceInformation { Creator = type => new CloseSarifLogService(), Promote = true } },
            { typeof(ISarifLocationTaggerService), new ServiceInformation { Creator = type => new SarifLocationTaggerService (), Promote = false } },
            { typeof(ISarifErrorListEventSelectionService), new ServiceInformation { Creator = type => new SarifErrorListEventProcessor(), Promote = false } },

            // Services that are exposed as templates are a bit "different", you expose them as
            // ITextViewCaretListenerService<> and then you have to differentiate them when they are
            // asked for.
            { typeof(ITextViewCaretListenerService<>), new ServiceInformation { Creator =  type =>
                {
                    if (type == typeof(ITextViewCaretListenerService<IErrorTag>))
                    {
                        return new TextViewCaretListenerService<IErrorTag>();
                    }

                    if (type == typeof(ITextViewCaretListenerService<ITextMarkerTag>))
                    {
                        return new TextViewCaretListenerService<ITextMarkerTag>();
                    }

                    return null;
                },
                Promote = false
            } }
        };

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            ServiceCreatorCallback callback = new ServiceCreatorCallback(CreateService);
            foreach (KeyValuePair<Type, ServiceInformation> serviceInformationKVP in ServiceTypeToServiceInformation)
            {
                ((IServiceContainer)this).AddService(serviceInformationKVP.Key, callback, promote: serviceInformationKVP.Value.Promote);
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            OpenLogFileCommands.Initialize(this);
            CodeAnalysisResultManager.Instance.Register();
            SarifToolWindowCommand.Initialize(this);
            ErrorList.ErrorListCommand.Initialize(this);
        
            return;
        }

        private object CreateService(IServiceContainer container, Type serviceType)
        {
            return ServiceTypeToServiceInformation.TryGetValue(serviceType, out ServiceInformation serviceInformation) ? serviceInformation.Creator(serviceType) : null;
        }
    }
}
