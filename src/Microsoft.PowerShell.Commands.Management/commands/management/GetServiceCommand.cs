// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.ServiceProcess;

namespace Microsoft.PowerShell.Commands
{

    /// <summary>
    /// This class implements the get-service command on Unix platforms.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Service", DefaultParameterSetName = "Default",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096496", RemotingCapability = RemotingCapability.SupportedByCommand)]
    [OutputType(typeof(UnixServiceController))]
    public sealed class GetServiceCommand : MultipleServiceCommandBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets an array of service names.
        /// </summary>
        /// <remarks>
        /// The ServiceName parameter is declared in subclasses,
        /// since it is optional for GetService and mandatory otherwise.
        /// </remarks>
        [Parameter(Position = 0, ParameterSetName = "Default", ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        [Alias("ServiceName")]
        public string[]? Name
        {
            get
            {
                return ServiceNames;
            }

            set
            {
                ServiceNames = value;
                CurrentSelectionMode = SelectionMode.ServiceName;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this cmdlet returns the services
        /// that depend upon the specified service.
        /// </summary>
        [Parameter]
        [Alias("DS")]
        public SwitchParameter DependentServices { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this cmdlet returns the services
        /// that the specified service requires. This parameter gets the value of the
        /// ServicesDependedOn property of the service.
        /// </summary>
        [Parameter]
        [Alias("SDO", "ServicesDependedOn")]
        public SwitchParameter RequiredServices { get; set; }

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Write the service objects.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (UnixServiceController service in MatchingServices())
            {
                if (!DependentServices.IsPresent && !RequiredServices.IsPresent)
                {
                    WriteObject(UnixServiceCmdletHelper.AddDefaultDisplayProperties(service));
                    continue;
                }

                if (DependentServices.IsPresent)
                {
                    foreach (UnixServiceController dependent in GetDependencies(service, dependents: true))
                    {
                        WriteObject(UnixServiceCmdletHelper.AddDefaultDisplayProperties(dependent));
                    }
                }

                if (RequiredServices.IsPresent)
                {
                    foreach (UnixServiceController required in GetDependencies(service, dependents: false))
                    {
                        WriteObject(UnixServiceCmdletHelper.AddDefaultDisplayProperties(required));
                    }
                }
            }
        }

        #endregion Overrides

        #region Private Members

        /// <summary>
        /// Gets the dependent or required services of a service. Only systemd provides
        /// dependency information; other service managers write a non-terminating error.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="dependents">True for services that depend on the service, false for services the service depends on.</param>
        /// <returns>The dependencies of the service.</returns>
        private List<UnixServiceController> GetDependencies(UnixServiceController service, bool dependents)
        {
            var results = new List<UnixServiceController>();
            List<string>? args = GetDependencyArguments(service, dependents);
            if (args is null)
            {
                string message = StringUtil.Format(
                    ServiceResources.ServiceManagerDoesNotSupportFeature,
                    GetManagerCommandName());
                var exception = new ServiceCommandException(message)
                {
                    ServiceName = service.Name,
                };

                WriteError(new ErrorRecord(exception, "ServiceManagerDoesNotSupportFeature", ErrorCategory.NotImplemented, service));
                return results;
            }

            ServiceCommandResult result = RunServiceManagerCommand(args, elevate: false);
            if (result.ExitCode != 0)
            {
                WriteOperationFailedError(
                    service,
                    result,
                    "CouldNotGetServiceDependencies",
                    ServiceResources.CouldNotGetServiceDependencies,
                    ErrorCategory.InvalidOperation);
                return results;
            }

            List<string> dependencyNames = UnixServiceParser.ParseSystemdDependencyUnits(result.StandardOutput);
            var servicesByName = new Dictionary<string, UnixServiceController>(StringComparer.OrdinalIgnoreCase);
            foreach (UnixServiceController candidate in AllServices)
            {
                servicesByName[candidate.Name] = candidate;
            }

            foreach (string dependencyName in dependencyNames)
            {
                if (string.Equals(dependencyName, service.Name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (servicesByName.TryGetValue(dependencyName, out UnixServiceController? dependency))
                {
                    results.Add(dependency);
                }
                else
                {
                    results.Add(new UnixServiceController
                    {
                        Name = dependencyName,
                        DisplayName = dependencyName,
                        Status = ServiceControllerStatus.Stopped,
                        StartType = ServiceStartMode.Manual,
                    });
                }
            }

            return results;
        }

        #endregion Private Members
    }
}

#endif
