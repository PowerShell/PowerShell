// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

using System.Collections.Generic;
using System.Management.Automation;
using System.ServiceProcess;

namespace Microsoft.PowerShell.Commands
{

    /// <summary>
    /// This class implements the base for service commands on Unix platforms which
    /// actually act on the service(s).
    /// </summary>
    public abstract class ServiceOperationBaseCommand : MultipleServiceCommandBase
    {

        /// <summary>
        /// Gets or sets an array of service names.
        /// </summary>
        /// <remarks>
        /// The ServiceName parameter is declared in subclasses,
        /// since it is optional for GetService and mandatory otherwise.
        /// </remarks>
        [Parameter(Position = 0, ParameterSetName = "Default", Mandatory = true, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
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
        /// Gets or sets the service objects to operate on.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "InputObject", ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public new UnixServiceController[]? InputObject
        {
            get
            {
                return base.InputObject;
            }

            set
            {
                base.InputObject = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to write the objects successfully
        /// operated upon to the success stream.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// Applies the expected status of the service after a successful operation, so
        /// that -PassThru returns an up-to-date object without re-querying the service
        /// manager.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="status">The expected status.</param>
        /// <returns>The service with the expected status.</returns>
        internal static UnixServiceController ApplyExpectedStatus(UnixServiceController service, ServiceControllerStatus status)
        {
            service.Status = status;
            return service;
        }

        /// <summary>
        /// This will start the service.
        /// </summary>
        /// <param name="service">Service to start.</param>
        /// <returns>True if-and-only-if the service was started.</returns>
        internal bool DoStartService(UnixServiceController service)
        {
            List<string> args = GetOperationArguments(ServiceAction.Start, service, noWait: false);
            return InvokeServiceOperation(
                service,
                args,
                "CouldNotStartService",
                ServiceResources.CouldNotStartService,
                ErrorCategory.OpenError);
        }

        /// <summary>
        /// This will stop the service.
        /// </summary>
        /// <param name="service">Service to stop.</param>
        /// <param name="noWait">Do not wait for the service to stop, when the service manager supports it.</param>
        /// <returns>True if-and-only-if the service was stopped.</returns>
        internal bool DoStopService(UnixServiceController service, bool noWait)
        {
            List<string> args = GetOperationArguments(ServiceAction.Stop, service, noWait);
            return InvokeServiceOperation(
                service,
                args,
                "CouldNotStopService",
                ServiceResources.CouldNotStopService,
                ErrorCategory.CloseError);
        }

        /// <summary>
        /// This will restart the service.
        /// </summary>
        /// <param name="service">Service to restart.</param>
        /// <returns>True if-and-only-if the service was restarted.</returns>
        internal bool DoRestartService(UnixServiceController service)
        {
            List<string> args = GetOperationArguments(ServiceAction.Restart, service, noWait: false);
            return InvokeServiceOperation(
                service,
                args,
                "CouldNotRestartService",
                ServiceResources.CouldNotRestartService,
                ErrorCategory.CloseError);
        }

        /// <summary>
        /// Runs the service manager for a mutating operation. Non-zero exit codes
        /// surface the command stderr as an ErrorRecord, and permission failures
        /// surface targeted sudo guidance instead.
        /// </summary>
        /// <param name="service">The service to operate on.</param>
        /// <param name="args">The service manager arguments.</param>
        /// <param name="errorId">The error id for the failure.</param>
        /// <param name="errorMessage">The message resource string for the failure.</param>
        /// <param name="category">The error category for the failure.</param>
        /// <returns>True if-and-only-if the operation succeeded.</returns>
        private bool InvokeServiceOperation(
            UnixServiceController service,
            List<string> args,
            string errorId,
            string errorMessage,
            ErrorCategory category)
        {
            ServiceCommandResult result = RunServiceManagerCommand(args, elevate: true);

            if (result.ExitCode != 0)
            {
                if (result.SudoPasswordRequired || result.SudoUnavailable)
                {
                    WriteSudoRequiredError(service);
                }
                else
                {
                    WriteOperationFailedError(service, result, errorId, errorMessage, category);
                }

                return false;
            }

            return true;
        }

    }
}

#endif
