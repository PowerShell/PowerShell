/********************************************************************++
 * Copyright (c) Microsoft Corporation.  All rights reserved.
 * --********************************************************************/

using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Remoting;
using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Management.Automation.Tracing;
using System.Globalization;
using Microsoft.PowerShell.Commands;

namespace Microsoft.PowerShell.Workflow
{
    /// <summary>
    /// Configuration for the M3P endpoint
    /// </summary>
    public class PSWorkflowSessionConfiguration : PSSessionConfiguration
    {
        /// <summary>
        /// IsWorkflowTypeEndpoint
        /// </summary>
        internal static bool IsWorkflowTypeEndpoint;

        #region Overrides of PSSessionConfiguration

        /// <summary>
        /// 
        /// </summary>
        /// <param name="senderInfo"></param>
        /// <exception cref="NotImplementedException"></exception>
        /// <returns></returns>
        public override InitialSessionState GetInitialSessionState(PSSenderInfo senderInfo)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionConfigurationData"></param>
        /// <param name="senderInfo"></param>
        /// <param name="configProviderId"></param>
        /// <returns></returns>
        public override InitialSessionState GetInitialSessionState(PSSessionConfigurationData sessionConfigurationData, PSSenderInfo senderInfo, string configProviderId)
        {
            Tracer structuredTracer = new Tracer();

            structuredTracer.Correlate();

            if (sessionConfigurationData == null)
                throw new ArgumentNullException("sessionConfigurationData");

            if (senderInfo == null)
                throw new ArgumentNullException("senderInfo");

            if (string.IsNullOrEmpty(configProviderId))
                throw new ArgumentNullException("configProviderId");

            if (Interlocked.CompareExchange(ref _modulesLoaded, ModulesLoaded, ModulesNotLoaded) == ModulesNotLoaded)
            {
                // it is sufficient if Populate() is called the first time and
                // modules are loaded once

                try
                {
                    IsWorkflowTypeEndpoint = true;

                    PSWorkflowConfigurationProvider workflowConfiguration = WorkflowJobSourceAdapter.GetInstance().GetPSWorkflowRuntime().Configuration;
                    if (workflowConfiguration == null)
                        throw new InvalidOperationException("PSWorkflowConfigurationProvider is null");

                    workflowConfiguration.Populate(sessionConfigurationData.PrivateData, configProviderId, senderInfo);

                    // now get all the modules in the specified path and import the same
                    if (sessionConfigurationData.ModulesToImport != null)
                    {
                        foreach (var module in sessionConfigurationData.ModulesToImport)
                        {
                            ModuleSpecification moduleSpec = null;
                            if (ModuleSpecification.TryParse(module, out moduleSpec))
                            {
                                var modulesToImport = new Collection<ModuleSpecification> { moduleSpec };
                                InitialSessionState.ImportPSModule(modulesToImport);
                            }
                            else
                            {
                                InitialSessionState.ImportPSModule(new[] { Environment.ExpandEnvironmentVariables(module) });
                            }
                        }
                    }

                    // Start the workflow job manager, if not started, to add an event handler for zero active sessions changed events
                    // This is required to auto shutdown the workflow type shared process when no workflow jobs have scheduled/inprogress and when no active sessions
                    WorkflowJobSourceAdapter.GetInstance().GetJobManager();
                }
                catch(Exception)
                {
                    // if there is an exception in either Populate() or Importing modules
                    // we consider that it is not loaded
                    Interlocked.CompareExchange(ref _modulesLoaded, ModulesNotLoaded, ModulesLoaded);
                    throw;
                }
            }
            
            if (configProviderId.ToLower(CultureInfo.InvariantCulture).Equals("http://schemas.microsoft.com/powershell/microsoft.windows.servermanagerworkflows"))
            {
                PSSessionConfigurationData.IsServerManager = true;
            }

            return InitialSessionState;
        }

        private static readonly InitialSessionState InitialSessionState =
            InitialSessionState.CreateRestricted(SessionCapabilities.WorkflowServer | SessionCapabilities.RemoteServer | SessionCapabilities.Language);

        private static int _modulesLoaded = ModulesNotLoaded;
        private const int ModulesNotLoaded = 0;
        private const int ModulesLoaded = 1;

        #endregion
    }
}
