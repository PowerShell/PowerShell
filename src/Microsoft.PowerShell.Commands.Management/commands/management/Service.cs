// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX // Not built on Unix

using System;
using System.Collections.Generic;
using System.ComponentModel; // Win32Exception
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Runtime.InteropServices; // Marshal, DllImport
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.ServiceProcess;
using Dbg = System.Management.Automation.Diagnostics;
using DWORD = System.UInt32;
using NakedWin32Handle = System.IntPtr;

namespace Microsoft.PowerShell.Commands
{
    #region ServiceBaseCommand

    /// <summary>
    /// This class implements the base for service commands.
    /// </summary>
    public abstract class ServiceBaseCommand : Cmdlet
    {
        #region Internal

        /// <summary>
        /// Confirm that the operation should proceed.
        /// </summary>
        /// <param name="service">Service object to be acted on.</param>
        /// <returns>True if operation should continue, false otherwise.</returns>
        protected bool ShouldProcessServiceOperation(ServiceController service)
        {
            return ShouldProcessServiceOperation(
                service.DisplayName,
                service.ServiceName);
        }

        /// <summary>
        /// Confirm that the operation should proceed.
        /// </summary>
        /// <param name="displayName">Display name of service to be acted on.</param>
        /// <param name="serviceName">Service name of service to be acted on.</param>
        /// <returns>True if operation should continue, false otherwise.</returns>
        protected bool ShouldProcessServiceOperation(
            string displayName, string serviceName)
        {
            string name = StringUtil.Format(ServiceResources.ServiceNameForConfirmation,
                displayName,
                serviceName);

            return ShouldProcess(name);
        }

        /// <summary>
        /// Writes a non-terminating error.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="innerException"></param>
        /// <param name="errorId"></param>
        /// <param name="errorMessage"></param>
        /// <param name="category"></param>
        internal void WriteNonTerminatingError(
            ServiceController service,
            Exception innerException,
            string errorId,
            string errorMessage,
            ErrorCategory category)
        {
            WriteNonTerminatingError(
                service.ServiceName,
                service.DisplayName,
                service,
                innerException,
                errorId,
                errorMessage,
                category);
        }

        /// <summary>
        /// Writes a non-terminating error.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="displayName"></param>
        /// <param name="targetObject"></param>
        /// <param name="innerException"></param>
        /// <param name="errorId"></param>
        /// <param name="errorMessage"></param>
        /// <param name="category"></param>
        internal void WriteNonTerminatingError(
            string serviceName,
            string displayName,
            object targetObject,
            Exception innerException,
            string errorId,
            string errorMessage,
            ErrorCategory category)
        {
            string message = StringUtil.Format(errorMessage,
                serviceName,
                displayName,
                (innerException == null) ? string.Empty : innerException.Message);

            var exception = new ServiceCommandException(message, innerException);
            exception.ServiceName = serviceName;

            WriteError(new ErrorRecord(exception, errorId, category, targetObject));
        }

        internal void SetServiceSecurityDescriptor(
            ServiceController service,
            string securityDescriptorSddl,
            NakedWin32Handle hService)
        {
            var rawSecurityDescriptor = new RawSecurityDescriptor(securityDescriptorSddl);
            RawAcl rawDiscretionaryAcl = rawSecurityDescriptor.DiscretionaryAcl;
            var discretionaryAcl = new DiscretionaryAcl(false, false, rawDiscretionaryAcl);

            byte[] rawDacl = new byte[discretionaryAcl.BinaryLength];
            discretionaryAcl.GetBinaryForm(rawDacl, 0);
            rawSecurityDescriptor.DiscretionaryAcl = new RawAcl(rawDacl, 0);
            byte[] securityDescriptorByte = new byte[rawSecurityDescriptor.BinaryLength];
            rawSecurityDescriptor.GetBinaryForm(securityDescriptorByte, 0);

            bool status = NativeMethods.SetServiceObjectSecurity(
                hService,
                SecurityInfos.DiscretionaryAcl,
                securityDescriptorByte);

            if (!status)
            {
                int lastError = Marshal.GetLastWin32Error();
                Win32Exception exception = new(lastError);
                bool accessDenied = exception.NativeErrorCode == NativeMethods.ERROR_ACCESS_DENIED;
                WriteNonTerminatingError(
                    service,
                    exception,
                    nameof(ServiceResources.CouldNotSetServiceSecurityDescriptorSddl),
                    StringUtil.Format(ServiceResources.CouldNotSetServiceSecurityDescriptorSddl, service.ServiceName, exception.Message),
                    accessDenied ? ErrorCategory.PermissionDenied : ErrorCategory.InvalidOperation);
            }
        }
        #endregion Internal
    }
    #endregion ServiceBaseCommand

    #region MultipleServiceCommandBase

    /// <summary>
    /// This class implements the base for service commands which can
    /// operate on multiple services.
    /// </summary>
    public abstract class MultipleServiceCommandBase : ServiceBaseCommand
    {
        #region Parameters

        /// <summary>
        /// The various process selection modes.
        /// </summary>
        internal enum SelectionMode
        {
            /// <summary>
            /// Select all services.
            /// </summary>
            Default = 0,
            /// <summary>
            /// Select services matching the supplied names.
            /// </summary>
            DisplayName = 1,
            /// <summary>
            /// Select services based on pipeline input.
            /// </summary>
            InputObject = 2,
            /// <summary>
            /// Select services by Service name.
            /// </summary>
            ServiceName = 3
        }
        /// <summary>
        /// Holds the selection mode setting.
        /// </summary>
        internal SelectionMode selectionMode;

        /// <remarks>
        /// The ServiceName parameter is declared in subclasses,
        /// since it is optional for GetService and mandatory otherwise.
        /// </remarks>
        internal string[] serviceNames = null;

        /// <summary>
        /// Gets/sets an array of display names for services.
        /// </summary>
        [Parameter(ParameterSetName = "DisplayName", Mandatory = true)]
        public string[] DisplayName
        {
            get
            {
                return displayNames;
            }

            set
            {
                displayNames = value;
                selectionMode = SelectionMode.DisplayName;
            }
        }

        internal string[] displayNames = null;

        /// <summary>
        /// Lets you include particular services.  Services not matching
        /// one of these (if specified) are excluded.
        /// These are interpreted as either ServiceNames or DisplayNames
        /// according to the parameter set.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string[] Include
        {
            get
            {
                return include;
            }

            set
            {
                include = value;
            }
        }

        internal string[] include = null;

        /// <summary>
        /// Lets you exclude particular services.  Services matching
        /// one of these (if specified) are excluded.
        /// These are interpreted as either ServiceNames or DisplayNames
        /// according to the parameter set.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string[] Exclude
        {
            get
            {
                return exclude;
            }

            set
            {
                exclude = value;
            }
        }

        internal string[] exclude = null;

        // 1054295-2004/12/01-JonN This also works around 1054295.
        /// <summary>
        /// If the input is a stream of [collections of]
        /// ServiceController objects, we bypass the ServiceName and
        /// DisplayName parameters and read the ServiceControllers
        /// directly.  This allows us to deal with services which
        /// have wildcard characters in their name (servicename or
        /// displayname).
        /// </summary>
        /// <value>ServiceController objects</value>
        [Parameter(ParameterSetName = "InputObject", ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ServiceController[] InputObject
        {
            get
            {
                return _inputObject;
            }

            set
            {
                _inputObject = value;
                selectionMode = SelectionMode.InputObject;
            }
        }

        private ServiceController[] _inputObject = null;
        #endregion Parameters

        #region Internal

        /// <summary>
        /// Gets an array of all services.
        /// </summary>
        /// <value>
        /// An array of <see cref="ServiceController"/> components that represents all the service resources.
        /// </value>
        /// <exception cref="System.Security.SecurityException">
        /// MSDN does not document the list of exceptions,
        /// but it is reasonable to expect that SecurityException is
        /// among them.  Errors here will terminate the cmdlet.
        /// </exception>
        internal ServiceController[] AllServices => _allServices ??= ServiceController.GetServices();

        private ServiceController[] _allServices;

        internal ServiceController GetOneService(string nameOfService)
        {
            Dbg.Assert(!WildcardPattern.ContainsWildcardCharacters(nameOfService), "Caller should verify that nameOfService doesn't contain wildcard characters");

            try
            {
                var sc = new ServiceController(nameOfService);
                // This will throw if the service doesn't exist
                var unused = sc.Status;

                // No exception, then this is an existing, valid service. Return it.
                return sc;
            }
            catch (InvalidOperationException) { }
            catch (ArgumentException) { }

            return null;
        }

        /// <summary>
        /// Retrieve the list of all services matching the ServiceName,
        /// DisplayName, Include and Exclude parameters, sorted by ServiceName.
        /// </summary>
        /// <returns></returns>
        internal List<ServiceController> MatchingServices()
        {
            List<ServiceController> matchingServices;
            switch (selectionMode)
            {
                case SelectionMode.DisplayName:
                    matchingServices = MatchingServicesByDisplayName();
                    break;
                case SelectionMode.InputObject:
                    matchingServices = MatchingServicesByInput();
                    break;
                default:
                    matchingServices = MatchingServicesByServiceName();
                    break;
            }
            // 2004/12/16 Note that the services will be sorted
            //  before being stopped.  JimTru confirms that this is OK.
            matchingServices.Sort(ServiceComparison);
            return matchingServices;
        }

        // sort by servicename
        private static int ServiceComparison(ServiceController x, ServiceController y)
        {
            return string.Compare(x.ServiceName, y.ServiceName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Retrieves the list of all services matching the ServiceName,
        /// Include and Exclude parameters.
        /// Generates a non-terminating error for each specified
        /// service name which is not found even though it contains
        /// no wildcards.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// We do not use the ServiceController(string serviceName)
        /// constructor variant, since the resultant
        /// ServiceController.ServiceName is the provided serviceName
        /// even when that differs from the real ServiceName by case.
        /// </remarks>
        private List<ServiceController> MatchingServicesByServiceName()
        {
            List<ServiceController> matchingServices = new();

            if (serviceNames == null)
            {
                foreach (ServiceController service in AllServices)
                {
                    IncludeExcludeAdd(matchingServices, service, false);
                }

                return matchingServices;
            }

            foreach (string pattern in serviceNames)
            {
                bool found = false;

                if (WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    WildcardPattern wildcard = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                    foreach (ServiceController service in AllServices)
                    {
                        if (!wildcard.IsMatch(service.ServiceName))
                            continue;
                        found = true;
                        IncludeExcludeAdd(matchingServices, service, true);
                    }
                }
                else
                {
                    ServiceController service = GetOneService(pattern);
                    if (service != null)
                    {
                        found = true;
                        IncludeExcludeAdd(matchingServices, service, true);
                    }
                }

                if (!found && !WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    WriteNonTerminatingError(
                        pattern,
                        string.Empty,
                        pattern,
                        null,
                        "NoServiceFoundForGivenName",
                        ServiceResources.NoServiceFoundForGivenName,
                        ErrorCategory.ObjectNotFound);
                }
            }

            return matchingServices;
        }

        /// <summary>
        /// Retrieves the list of all services matching the DisplayName,
        /// Include and Exclude parameters.
        /// Generates a non-terminating error for each specified
        /// display name which is not found even though it contains
        /// no wildcards.
        /// </summary>
        /// <returns></returns>
        private List<ServiceController> MatchingServicesByDisplayName()
        {
            List<ServiceController> matchingServices = new();
            if (DisplayName == null)
            {
                Diagnostics.Assert(false, "null DisplayName");
                throw PSTraceSource.NewInvalidOperationException();
            }

            foreach (string pattern in DisplayName)
            {
                WildcardPattern wildcard =
                    WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                bool found = false;
                foreach (ServiceController service in AllServices)
                {
                    if (!wildcard.IsMatch(service.DisplayName))
                        continue;
                    found = true;
                    IncludeExcludeAdd(matchingServices, service, true);
                }

                if (!found && !WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    WriteNonTerminatingError(
                        string.Empty,
                        pattern,
                        pattern,
                        null,
                        "NoServiceFoundForGivenDisplayName",
                        ServiceResources.NoServiceFoundForGivenDisplayName,
                        ErrorCategory.ObjectNotFound);
                }
            }

            return matchingServices;
        }

        /// <summary>
        /// Retrieves the list of all services matching the InputObject,
        /// Include and Exclude parameters.
        /// </summary>
        /// <returns></returns>
        private List<ServiceController> MatchingServicesByInput()
        {
            List<ServiceController> matchingServices = new();
            if (InputObject == null)
            {
                Diagnostics.Assert(false, "null InputObject");
                throw PSTraceSource.NewInvalidOperationException();
            }

            foreach (ServiceController service in InputObject)
            {
                service.Refresh();
                IncludeExcludeAdd(matchingServices, service, false);
            }

            return matchingServices;
        }

        /// <summary>
        /// Add <paramref name="service"/> to <paramref name="list"/>,
        /// but only if it passes the Include and Exclude filters (if present)
        /// and (if <paramref name="checkDuplicates"/>) if it is not
        /// already on  <paramref name="list"/>.
        /// </summary>
        /// <param name="list">List of services.</param>
        /// <param name="service">Service to add to list.</param>
        /// <param name="checkDuplicates">Check list for duplicates.</param>
        private void IncludeExcludeAdd(
            List<ServiceController> list,
            ServiceController service,
            bool checkDuplicates)
        {
            if (include != null && !Matches(service, include))
                return;
            if (exclude != null && Matches(service, exclude))
                return;
            if (checkDuplicates)
            {
                foreach (ServiceController sc in list)
                {
                    if (sc.ServiceName == service.ServiceName &&
                        sc.MachineName == service.MachineName)
                    {
                        return;
                    }
                }
            }

            list.Add(service);
        }

        /// <summary>
        /// Check whether <paramref name="service"/> matches the list of
        /// patterns in <paramref name="matchList"/>.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="matchList"></param>
        /// <returns></returns>
        private bool Matches(ServiceController service, string[] matchList)
        {
            if (matchList == null)
                throw PSTraceSource.NewArgumentNullException(nameof(matchList));
            string serviceID = (selectionMode == SelectionMode.DisplayName)
                                ? service.DisplayName
                                : service.ServiceName;
            foreach (string pattern in matchList)
            {
                WildcardPattern wildcard = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                if (wildcard.IsMatch(serviceID))
                    return true;
            }

            return false;
        }
        #endregion Internal

    }
    #endregion MultipleServiceCommandBase

    #region GetServiceCommand
    /// <summary>
    /// This class implements the get-service command.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Service", DefaultParameterSetName = "Default",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096496", RemotingCapability = RemotingCapability.SupportedByCommand)]
    [OutputType(typeof(ServiceController))]
    public sealed class GetServiceCommand : MultipleServiceCommandBase
    {
        #region Parameters
        /// <summary>
        /// Gets/sets an array of service names.
        /// </summary>
        /// <remarks>
        /// The ServiceName parameter is declared in subclasses,
        /// since it is optional for GetService and mandatory otherwise.
        /// </remarks>
        [Parameter(Position = 0, ParameterSetName = "Default", ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty()]
        [Alias("ServiceName")]
        public string[] Name
        {
            get
            {
                return serviceNames;
            }

            set
            {
                serviceNames = value;
                selectionMode = SelectionMode.ServiceName;
            }
        }

        /// <summary>
        /// This returns the DependentServices of the specified service.
        /// </summary>
        [Parameter]
        [Alias("DS")]
        public SwitchParameter DependentServices { get; set; }

        /// <summary>
        /// This returns the ServicesDependedOn of the specified service.
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
            foreach (ServiceController service in MatchingServices())
            {
                if (!DependentServices.IsPresent && !RequiredServices.IsPresent)
                {
                    WriteObject(AddProperties(service));
                }
                else
                {
                    if (DependentServices.IsPresent)
                    {
                        foreach (ServiceController dependantserv in service.DependentServices)
                        {
                            WriteObject(dependantserv);
                        }
                    }

                    if (RequiredServices.IsPresent)
                    {
                        foreach (ServiceController servicedependedon in service.ServicesDependedOn)
                        {
                            WriteObject(servicedependedon);
                        }
                    }
                }
            }
        }

        #endregion Overrides

        /// <summary>
        /// Adds UserName, Description, BinaryPathName, DelayedAutoStart and StartupType to a ServiceController object.
        /// </summary>
        /// <param name="service"></param>
        /// <returns>ServiceController as PSObject with UserName, Description and StartupType added.</returns>
        private PSObject AddProperties(ServiceController service)
        {
            NakedWin32Handle hScManager = IntPtr.Zero;
            NakedWin32Handle hService = IntPtr.Zero;
            int lastError = 0;
            PSObject serviceAsPSObj = PSObject.AsPSObject(service);
            try
            {
                hScManager = NativeMethods.OpenSCManagerW(
                    lpMachineName: service.MachineName,
                    lpDatabaseName: null,
                    dwDesiredAccess: NativeMethods.SC_MANAGER_CONNECT
                );
                if (hScManager == IntPtr.Zero)
                {
                    lastError = Marshal.GetLastWin32Error();
                    Win32Exception exception = new(lastError);
                    WriteNonTerminatingError(
                        service,
                        exception,
                        "FailToOpenServiceControlManager",
                        ServiceResources.FailToOpenServiceControlManager,
                        ErrorCategory.PermissionDenied);
                }

                hService = NativeMethods.OpenServiceW(
                    hScManager,
                    service.ServiceName,
                    NativeMethods.SERVICE_QUERY_CONFIG
                );
                if (hService == IntPtr.Zero)
                {
                    lastError = Marshal.GetLastWin32Error();
                    Win32Exception exception = new(lastError);
                    WriteNonTerminatingError(
                        service,
                        exception,
                        "CouldNotGetServiceInfo",
                        ServiceResources.CouldNotGetServiceInfo,
                        ErrorCategory.PermissionDenied);
                }

                NativeMethods.SERVICE_DESCRIPTIONW description = new();
                bool querySuccessful = NativeMethods.QueryServiceConfig2<NativeMethods.SERVICE_DESCRIPTIONW>(hService, NativeMethods.SERVICE_CONFIG_DESCRIPTION, out description);

                NativeMethods.SERVICE_DELAYED_AUTO_START_INFO autostartInfo = new();
                querySuccessful = querySuccessful && NativeMethods.QueryServiceConfig2<NativeMethods.SERVICE_DELAYED_AUTO_START_INFO>(hService, NativeMethods.SERVICE_CONFIG_DELAYED_AUTO_START_INFO, out autostartInfo);

                NativeMethods.QUERY_SERVICE_CONFIG serviceInfo = new();
                querySuccessful = querySuccessful && NativeMethods.QueryServiceConfig(hService, out serviceInfo);

                if (!querySuccessful)
                {
                    WriteNonTerminatingError(
                        service: service,
                        innerException: null,
                        errorId: "CouldNotGetServiceInfo",
                        errorMessage: ServiceResources.CouldNotGetServiceInfo,
                        category: ErrorCategory.PermissionDenied
                        );
                }

                PSProperty noteProperty = new("UserName", serviceInfo.lpServiceStartName);
                serviceAsPSObj.Properties.Add(noteProperty, true);
                serviceAsPSObj.TypeNames.Insert(0, "System.Service.ServiceController#UserName");

                noteProperty = new PSProperty("Description", description.lpDescription);
                serviceAsPSObj.Properties.Add(noteProperty, true);
                serviceAsPSObj.TypeNames.Insert(0, "System.Service.ServiceController#Description");

                noteProperty = new PSProperty("DelayedAutoStart", autostartInfo.fDelayedAutostart);
                serviceAsPSObj.Properties.Add(noteProperty, true);
                serviceAsPSObj.TypeNames.Insert(0, "System.Service.ServiceController#DelayedAutoStart");

                noteProperty = new PSProperty("BinaryPathName", serviceInfo.lpBinaryPathName);
                serviceAsPSObj.Properties.Add(noteProperty, true);
                serviceAsPSObj.TypeNames.Insert(0, "System.Service.ServiceController#BinaryPathName");

                noteProperty = new PSProperty("StartupType", NativeMethods.GetServiceStartupType(service.StartType, autostartInfo.fDelayedAutostart));
                serviceAsPSObj.Properties.Add(noteProperty, true);
                serviceAsPSObj.TypeNames.Insert(0, "System.Service.ServiceController#StartupType");
            }
            finally
            {
                if (hService != IntPtr.Zero)
                {
                    bool succeeded = NativeMethods.CloseServiceHandle(hService);
                    if (!succeeded)
                    {
                        Diagnostics.Assert(lastError != 0, "ErrorCode not success");
                    }
                }

                if (hScManager != IntPtr.Zero)
                {
                    bool succeeded = NativeMethods.CloseServiceHandle(hScManager);
                    if (!succeeded)
                    {
                        Diagnostics.Assert(lastError != 0, "ErrorCode not success");
                    }
                }
            }

            return serviceAsPSObj;
        }
    }
    #endregion GetServiceCommand

    #region ServiceOperationBaseCommand
    /// <summary>
    /// This class implements the base for service commands which actually
    /// act on the service(s).
    /// </summary>
    public abstract class ServiceOperationBaseCommand : MultipleServiceCommandBase
    {
        #region Parameters
        /// <summary>
        /// Gets/sets an array of service names.
        /// </summary>
        /// <remarks>
        /// The ServiceName parameter is declared in subclasses,
        /// since it is optional for GetService and mandatory otherwise.
        /// </remarks>
        [Parameter(Position = 0, ParameterSetName = "Default", Mandatory = true, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
        [Alias("ServiceName")]
        public string[] Name
        {
            get
            {
                return serviceNames;
            }

            set
            {
                serviceNames = value;
                selectionMode = SelectionMode.ServiceName;
            }
        }

        /// <summary>
        /// Service controller objects.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "InputObject", ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public new ServiceController[] InputObject
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
        /// Specifies whether to write the objects successfully operated upon
        /// to the success stream.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        #endregion Parameters

        #region Internal
        /// <summary>
        /// Waits forever for the service to reach the desired status, but
        /// writes a string to WriteWarning every 2 seconds.
        /// </summary>
        /// <param name="serviceController">Service on which to operate.</param>
        /// <param name="targetStatus">Desired status.</param>
        /// <param name="pendingStatus">
        /// This is the expected status while the operation is incomplete.
        /// If the service is in some other state, this means that the
        /// operation failed.
        /// </param>
        /// <param name="resourceIdPending">
        /// resourceId for a string to be written to verbose stream
        /// every 2 seconds
        /// </param>
        /// <param name="errorId">
        /// errorId for a nonterminating error if operation fails
        /// </param>
        ///  <param name="errorMessage">
        /// errorMessage for a nonterminating error if operation fails
        /// </param>
        /// <returns>True if action succeeded.</returns>
        /// <exception cref="PipelineStoppedException">
        /// WriteWarning will throw this if the pipeline has been stopped.
        /// This means that the delay between hitting CTRL-C and stopping
        /// the cmdlet should be 2 seconds at most.
        /// </exception>
        internal bool DoWaitForStatus(
            ServiceController serviceController,
            ServiceControllerStatus targetStatus,
            ServiceControllerStatus pendingStatus,
            string resourceIdPending,
            string errorId,
            string errorMessage)
        {
            while (true)
            {
                try
                {
                    // ServiceController.Start will return before the service is actually started
                    // This API will wait forever
                    serviceController.WaitForStatus(
                        targetStatus,
                        new TimeSpan(20000000) // 2 seconds
                        );
                    return true; // service reached target status
                }
                catch (System.ServiceProcess.TimeoutException) // still waiting
                {
                    if (serviceController.Status != pendingStatus
                        // NTRAID#Windows Out Of Band Releases-919945-2005/09/27-JonN
                        // Close window where service could complete at
                        // just the wrong time
                        && serviceController.Status != targetStatus)
                    {
                        WriteNonTerminatingError(serviceController, null,
                                                 errorId,
                                                 errorMessage,
                                                 ErrorCategory.OpenError);
                        return false;
                    }

                    string message = StringUtil.Format(resourceIdPending,
                        serviceController.ServiceName,
                        serviceController.DisplayName
                        );
                    // will throw PipelineStoppedException if user hit CTRL-C
                    WriteWarning(message);
                }
            }
        }

        /// <summary>
        /// This will start the service.
        /// </summary>
        /// <param name="serviceController">Service to start.</param>
        /// <returns>True iff the service was started.</returns>
        internal bool DoStartService(ServiceController serviceController)
        {
            Exception exception = null;
            try
            {
                serviceController.Start();
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode != NativeMethods.ERROR_SERVICE_ALREADY_RUNNING)
                    exception = e;
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is not Win32Exception eInner
                    || eInner.NativeErrorCode != NativeMethods.ERROR_SERVICE_ALREADY_RUNNING)
                {
                    exception = e;
                }
            }

            if (exception != null)
            {
                // This service refused to accept the start command,
                // so write a non-terminating error.
                WriteNonTerminatingError(serviceController,
                    exception,
                    "CouldNotStartService",
                    ServiceResources.CouldNotStartService,
                    ErrorCategory.OpenError);
                return false;
            }

            // ServiceController.Start will return
            // before the service is actually started.
            if (!DoWaitForStatus(
                serviceController,
                ServiceControllerStatus.Running,
                ServiceControllerStatus.StartPending,
                ServiceResources.StartingService,
                "StartServiceFailed",
                ServiceResources.StartServiceFailed))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// This will stop the service.
        /// </summary>
        /// <param name="serviceController">Service to stop.</param>
        /// <param name="force">Stop dependent services.</param>
        /// <param name="waitForServiceToStop"></param>
        /// <returns>True iff the service was stopped.</returns>
        internal List<ServiceController> DoStopService(ServiceController serviceController, bool force, bool waitForServiceToStop)
        {
            // Ignore ServiceController.CanStop.  CanStop will be set false
            // if the service is not running, but this is not an error.

            List<ServiceController> stoppedServices = new();
            ServiceController[] dependentServices = null;

            try
            {
                dependentServices = serviceController.DependentServices;
            }
            catch (Win32Exception e)
            {
                WriteNonTerminatingError(serviceController, e,
                    "CouldNotAccessDependentServices",
                    ServiceResources.CouldNotAccessDependentServices,
                    ErrorCategory.InvalidOperation);
            }
            catch (InvalidOperationException e)
            {
                WriteNonTerminatingError(serviceController, e,
                    "CouldNotAccessDependentServices",
                    ServiceResources.CouldNotAccessDependentServices,
                    ErrorCategory.InvalidOperation);
            }

            if (!force)
            {
                if ((dependentServices != null)
                    && (dependentServices.Length > 0))
                {
                    // Check if all dependent services are stopped
                    if (!HaveAllDependentServicesStopped(dependentServices))
                    {
                        // This service has dependent services
                        //  and the force flag is not specified.
                        //  Add a non-critical error for it.
                        WriteNonTerminatingError(serviceController,
                        null,
                        "ServiceHasDependentServices",
                        ServiceResources.ServiceHasDependentServices,
                        ErrorCategory.InvalidOperation);

                        return stoppedServices;
                    }
                }
            }

            if (dependentServices != null)
            {
                foreach (ServiceController service in dependentServices)
                {
                    if ((service.Status == ServiceControllerStatus.Running ||
                        service.Status == ServiceControllerStatus.StartPending) &&
                        service.CanStop)
                    {
                        stoppedServices.Add(service);
                    }
                }
            }

            Exception exception = null;
            try
            {
                serviceController.Stop();
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode != NativeMethods.ERROR_SERVICE_NOT_ACTIVE)
                    exception = e;
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is not Win32Exception eInner
                    || eInner.NativeErrorCode != NativeMethods.ERROR_SERVICE_NOT_ACTIVE)
                {
                    exception = e;
                }
            }

            if (exception != null)
            {
                // This service refused to accept the stop command,
                // so write a non-terminating error.
                WriteNonTerminatingError(serviceController,
                    exception,
                    "CouldNotStopService",
                    ServiceResources.CouldNotStopService,
                    ErrorCategory.CloseError);
                RemoveNotStoppedServices(stoppedServices);
                return stoppedServices;
            }

            // ServiceController.Stop will return
            //  before the service is actually stopped.
            if (waitForServiceToStop)
            {
                if (!DoWaitForStatus(
                    serviceController,
                    ServiceControllerStatus.Stopped,
                    ServiceControllerStatus.StopPending,
                    ServiceResources.StoppingService,
                    "StopServiceFailed",
                    ServiceResources.StopServiceFailed))
                {
                    RemoveNotStoppedServices(stoppedServices);
                    return stoppedServices;
                }
            }

            RemoveNotStoppedServices(stoppedServices);
            if ((serviceController.Status.Equals(ServiceControllerStatus.Stopped)) || (serviceController.Status.Equals(ServiceControllerStatus.StopPending)))
            {
                stoppedServices.Add(serviceController);
            }

            return stoppedServices;
        }

        /// <summary>
        /// Check if all dependent services are stopped.
        /// </summary>
        /// <param name="dependentServices"></param>
        /// <returns>
        /// True if all dependent services are stopped
        /// False if not all dependent services are stopped
        /// </returns>
        private static bool HaveAllDependentServicesStopped(ServiceController[] dependentServices)
        {
            return Array.TrueForAll(dependentServices, static service => service.Status == ServiceControllerStatus.Stopped);
        }

        /// <summary>
        /// This removes all services that are not stopped from a list of services.
        /// </summary>
        /// <param name="services">A list of services.</param>
        internal void RemoveNotStoppedServices(List<ServiceController> services)
        {
            // You shall not modify a collection during enumeration.
            services.RemoveAll(service =>
                service.Status != ServiceControllerStatus.Stopped &&
                service.Status != ServiceControllerStatus.StopPending);
        }

        /// <summary>
        /// This will pause the service.
        /// </summary>
        /// <param name="serviceController">Service to pause.</param>
        /// <returns>True iff the service was paused.</returns>
        internal bool DoPauseService(ServiceController serviceController)
        {
            Exception exception = null;
            bool serviceNotRunning = false;
            try
            {
                serviceController.Pause();
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode == NativeMethods.ERROR_SERVICE_NOT_ACTIVE)
                {
                    serviceNotRunning = true;
                }

                exception = e;
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is Win32Exception eInner
                    && eInner.NativeErrorCode == NativeMethods.ERROR_SERVICE_NOT_ACTIVE)
                {
                    serviceNotRunning = true;
                }

                exception = e;
            }

            if (exception != null)
            {
                // This service refused to accept the pause command,
                // so write a non-terminating error.
                string resourceIdAndErrorId = ServiceResources.CouldNotSuspendService;
                if (serviceNotRunning)
                {
                    WriteNonTerminatingError(serviceController,
                        exception,
                        "CouldNotSuspendServiceNotRunning",
                        ServiceResources.CouldNotSuspendServiceNotRunning,
                        ErrorCategory.CloseError);
                }
                else if (!serviceController.CanPauseAndContinue)
                {
                    WriteNonTerminatingError(serviceController,
                        exception,
                        "CouldNotSuspendServiceNotSupported",
                        ServiceResources.CouldNotSuspendServiceNotSupported,
                        ErrorCategory.CloseError);
                }

                WriteNonTerminatingError(serviceController,
                    exception,
                    "CouldNotSuspendService",
                    ServiceResources.CouldNotSuspendService,
                    ErrorCategory.CloseError);

                return false;
            }

            // ServiceController.Pause will return
            // before the service is actually paused.
            if (!DoWaitForStatus(
                serviceController,
                ServiceControllerStatus.Paused,
                ServiceControllerStatus.PausePending,
                ServiceResources.SuspendingService,
                "SuspendServiceFailed",
                ServiceResources.SuspendServiceFailed))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// This will resume the service.
        /// </summary>
        /// <param name="serviceController">Service to resume.</param>
        /// <returns>True iff the service was resumed.</returns>
        internal bool DoResumeService(ServiceController serviceController)
        {
            Exception exception = null;
            bool serviceNotRunning = false;
            try
            {
                serviceController.Continue();
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode == NativeMethods.ERROR_SERVICE_NOT_ACTIVE)
                {
                    serviceNotRunning = true;
                }

                exception = e;
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is Win32Exception eInner
                    && eInner.NativeErrorCode == NativeMethods.ERROR_SERVICE_NOT_ACTIVE)
                {
                    serviceNotRunning = true;
                }

                exception = e;
            }

            if (exception != null)
            {
                // This service refused to accept the continue command,
                // so write a non-terminating error.
                if (serviceNotRunning)
                {
                    WriteNonTerminatingError(serviceController,
                        exception,
                        "CouldNotResumeServiceNotRunning",
                        ServiceResources.CouldNotResumeServiceNotRunning,
                        ErrorCategory.CloseError);
                }
                else if (!serviceController.CanPauseAndContinue)
                {
                    WriteNonTerminatingError(serviceController,
                        exception,
                        "CouldNotResumeServiceNotSupported",
                        ServiceResources.CouldNotResumeServiceNotSupported,
                        ErrorCategory.CloseError);
                }

                WriteNonTerminatingError(serviceController,
                    exception,
                    "CouldNotResumeService",
                    ServiceResources.CouldNotResumeService,
                    ErrorCategory.CloseError);

                return false;
            }

            // ServiceController.Continue will return
            // before the service is actually continued.
            if (!DoWaitForStatus(
                serviceController,
                ServiceControllerStatus.Running,
                ServiceControllerStatus.ContinuePending,
                ServiceResources.ResumingService,
                "ResumeServiceFailed",
                ServiceResources.ResumeServiceFailed))
            {
                return false;
            }

            return true;
        }
        #endregion Internal
    }
    #endregion ServiceOperationBaseCommand

    #region StopServiceCommand

    /// <summary>
    /// This class implements the stop-service command.
    /// </summary>
    /// <remarks>
    /// Note that the services will be sorted before being stopped.
    /// PM confirms that this is OK.
    /// </remarks>
    [Cmdlet(VerbsLifecycle.Stop, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097052")]
    [OutputType(typeof(ServiceController))]
    public sealed class StopServiceCommand : ServiceOperationBaseCommand
    {
        /// <summary>
        /// Specifies whether to force a service to stop
        /// even if it has dependent services.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Specifies whether to wait for a service to reach the stopped state before returning.
        /// </summary>
        [Parameter]
        public SwitchParameter NoWait { get; set; }

        /// <summary>
        /// Stop the services.
        /// It is a non-terminating error if -Force is not specified and
        ///  the service has dependent services, whether or not they
        ///  are running.
        /// It is a non-terminating error if the service stop operation fails.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (ServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                List<ServiceController> stoppedServices = DoStopService(serviceController, Force, !NoWait);

                if (PassThru && stoppedServices.Count > 0)
                {
                    foreach (ServiceController service in stoppedServices)
                    {
                        WriteObject(service);
                    }
                }
            }
        }
    }
    #endregion StopServiceCommand

    #region StartServiceCommand
    /// <summary>
    /// This class implements the start-service command.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097051")]
    [OutputType(typeof(ServiceController))]
    public sealed class StartServiceCommand : ServiceOperationBaseCommand
    {
        /// <summary>
        /// Start the services.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (ServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                if (DoStartService(serviceController))
                {
                    if (PassThru)
                        WriteObject(serviceController);
                }
            }
        }
    }
    #endregion StartServiceCommand

    #region SuspendServiceCommand
    /// <summary>
    /// This class implements the suspend-service command.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Suspend, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097053")]
    [OutputType(typeof(ServiceController))]
    public sealed class SuspendServiceCommand : ServiceOperationBaseCommand
    {
        /// <summary>
        /// Start the services.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (ServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                if (DoPauseService(serviceController))
                {
                    if (PassThru)
                        WriteObject(serviceController);
                }
            }
        }
    }
    #endregion SuspendServiceCommand

    #region ResumeServiceCommand
    /// <summary>
    /// This class implements the resume-service command.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Resume, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097150")]
    [OutputType(typeof(ServiceController))]
    public sealed class ResumeServiceCommand : ServiceOperationBaseCommand
    {
        /// <summary>
        /// Start the services.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (ServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                if (DoResumeService(serviceController))
                {
                    if (PassThru)
                        WriteObject(serviceController);
                }
            }
        }
    }
    #endregion ResumeServiceCommand

    #region RestartServiceCommand

    /// <summary>
    /// This class implements the restart-service command.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Restart, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097059")]
    [OutputType(typeof(ServiceController))]
    public sealed class RestartServiceCommand : ServiceOperationBaseCommand
    {
        /// <summary>
        /// Specifies whether to force a service to stop
        /// even if it has dependent services.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Stop and restart the services.
        /// It is a non-terminating error if the service is running,
        ///  -Force is not specified and the service has dependent services,
        ///  whether or not the dependent services are running.
        /// It is a non-terminating error if the service stop operation fails.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (ServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                // Set the NoWait parameter to false since we are not adding this switch to this cmdlet.
                List<ServiceController> stoppedServices = DoStopService(serviceController, Force, true);

                if (stoppedServices.Count > 0)
                {
                    foreach (ServiceController service in stoppedServices)
                    {
                        if (DoStartService(service))
                        {
                            if (PassThru)
                                WriteObject(service);
                        }
                    }
                }
            }
        }
    }
    #endregion RestartServiceCommand

    #region SetServiceCommand

    /// <summary>
    /// This class implements the set-service command.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "Service", SupportsShouldProcess = true, DefaultParameterSetName = "Name",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097148", RemotingCapability = RemotingCapability.SupportedByCommand)]
    [OutputType(typeof(ServiceController))]
    public class SetServiceCommand : ServiceOperationBaseCommand
    {
        #region Parameters

        /// <summary>
        /// Service name.
        /// </summary>
        /// <value></value>
        [Parameter(Mandatory = true, ParameterSetName = "Name", Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("ServiceName", "SN")]
        public new string Name
        {
            get
            {
                return serviceName;
            }

            set
            {
                serviceName = value;
            }
        }

        internal string serviceName = null;

        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies a ServiceController object that represents the service to change.
        /// Enter a variable that contains the objects or type a command or expression
        /// that gets the objects.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "InputObject", Position = 0, ValueFromPipeline = true)]
        public new ServiceController InputObject { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "DisplayName".
        /// Specifies a new display name for the cmdlet.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("DN")]
        public new string DisplayName
        {
            get
            {
                return displayName;
            }

            set
            {
                displayName = value;
            }
        }

        internal string displayName = null;

        /// <summary>
        /// Account under which the service should run.
        /// </summary>
        /// <value></value>
        [Parameter]
        [Credential()]
        public PSCredential Credential { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Description".
        /// Specifies a new description for the service.
        /// The service description appears in Services in Computer Management.
        /// Description is not a property of the ServiceController object that
        /// Get-Service retrieve.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Description
        {
            get
            {
                return description;
            }

            set
            {
                description = value;
            }
        }

        internal string description = null;

        /// <summary>
        /// The following is the definition of the input parameter "StartupType".
        /// "Set-Service -StartType" sets ServiceController.InputObject.StartType.
        /// Changes the starting mode of the service. Valid values for StartupType are:
        /// -- Automatic: Start when the system starts.
        /// -- Manual   : Starts only when started by a user or program.
        /// -- Disabled : Can.
        /// </summary>
        [Parameter]
        [Alias("StartMode", "SM", "ST", "StartType")]
        [ValidateNotNullOrEmpty]
        public ServiceStartupType StartupType
        {
            get
            {
                return startupType;
            }

            set
            {
                startupType = value;
            }
        }

        // We set the initial value to an invalid value so that we can
        // distinguish when this is and is not set.
        internal ServiceStartupType startupType = ServiceStartupType.InvalidValue;

        /// <summary>
        /// Sets the SecurityDescriptorSddl of the service using a SDDL string.
        /// </summary>
        [Parameter]
        [Alias("sd")]
        [ValidateNotNullOrEmpty]
        public string SecurityDescriptorSddl
        {
            get;
            set;
        }

        /// <summary>
        /// The following is the definition of the input parameter "Status".
        /// This specifies what state the service should be in (e.g. Running, Stopped,
        /// Paused).  If it is already in that state, do nothing.  If it is not, do the
        /// appropriate action to bring about the desired result (start/stop/suspend the
        /// service) and issue an error if this cannot be achieved.
        ///  Status can be Paused ,  Running and Stopped.
        /// </summary>
        [Parameter]
        [ValidateSetAttribute(new string[] { "Running", "Stopped", "Paused" })]
        public string Status
        {
            get
            {
                return serviceStatus;
            }

            set
            {
                serviceStatus = value;
            }
        }

        internal string serviceStatus = null;

        /// <summary>
        /// The following is the definition of the input parameter "Force".
        /// This parameter is useful only when parameter "Stop" is enabled.
        /// If "Force" is enabled, it will also stop the dependent services.
        /// If not, it will send an error when this service has dependent ones.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// This is not a parameter for this cmdlet.
        /// </summary>
        // This has been shadowed from base class and removed parameter tag to fix gcm "Set-Service" -syntax
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public new string[] Include
        {
            get
            {
                return include;
            }

            set
            {
                include = null;
            }
        }

        internal new string[] include = null;

        /// <summary>
        /// This is not a parameter for this cmdlet.
        /// </summary>
        // This has been shadowed from base class and removed parameter tag to fix gcm "Set-Service" -syntax
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public new string[] Exclude
        {
            get
            {
                return exclude;
            }

            set
            {
                exclude = null;
            }
        }

        internal new string[] exclude = null;
        #endregion Parameters

        #region Overrides
        /// <summary>
        /// </summary>
        protected override void ProcessRecord()
        {
            ServiceController service = null;
            IntPtr password = IntPtr.Zero;
            bool objServiceShouldBeDisposed = false;

            try
            {
                if (InputObject != null)
                {
                    service = InputObject;
                    Name = service.ServiceName;
                    objServiceShouldBeDisposed = false;
                }
                else
                {
                    service = new ServiceController(serviceName);
                    objServiceShouldBeDisposed = true;
                }

                Diagnostics.Assert(!string.IsNullOrEmpty(Name), "null ServiceName");

                // "new ServiceController" will succeed even if
                // there is no such service.  This checks whether
                // the service actually exists.
                string unusedByDesign = service.DisplayName;
            }
            catch (ArgumentException ex)
            {
                // cannot use WriteNonterminatingError as service is null
                ErrorRecord er = new(ex, "ArgumentException", ErrorCategory.ObjectNotFound, Name);
                WriteError(er);
                return;
            }
            catch (InvalidOperationException ex)
            {
                // cannot use WriteNonterminatingError as service is null
                ErrorRecord er = new(ex, "InvalidOperationException", ErrorCategory.ObjectNotFound, Name);
                WriteError(er);
                return;
            }

            try // In finally we ensure dispose, if object not pipelined.
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(service))
                {
                    return;
                }

                NakedWin32Handle hScManager = IntPtr.Zero;
                NakedWin32Handle hService = IntPtr.Zero;
                IntPtr delayedAutoStartInfoBuffer = IntPtr.Zero;
                try
                {
                    hScManager = NativeMethods.OpenSCManagerW(
                        string.Empty,
                        null,
                        NativeMethods.SC_MANAGER_CONNECT
                        );

                    if (hScManager == IntPtr.Zero)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            service,
                            exception,
                            "FailToOpenServiceControlManager",
                            ServiceResources.FailToOpenServiceControlManager,
                            ErrorCategory.PermissionDenied);
                        return;
                    }

                    var access = NativeMethods.SERVICE_CHANGE_CONFIG;
                    if (!string.IsNullOrEmpty(SecurityDescriptorSddl))
                        access |= NativeMethods.WRITE_DAC | NativeMethods.WRITE_OWNER;

                    hService = NativeMethods.OpenServiceW(
                        hScManager,
                        Name,
                        access
                        );

                    if (hService == IntPtr.Zero)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            service,
                            exception,
                            "CouldNotSetService",
                            ServiceResources.CouldNotSetService,
                            ErrorCategory.PermissionDenied);
                        return;
                    }
                    // Modify startup type or display name or credential
                    if (!string.IsNullOrEmpty(DisplayName)
                        || StartupType != ServiceStartupType.InvalidValue || Credential != null)
                    {
                        DWORD dwStartType = NativeMethods.SERVICE_NO_CHANGE;
                        if (!NativeMethods.TryGetNativeStartupType(StartupType, out dwStartType))
                        {
                            WriteNonTerminatingError(StartupType.ToString(), "Set-Service", Name,
                                new ArgumentException(), "CouldNotSetService",
                                ServiceResources.UnsupportedStartupType,
                                ErrorCategory.InvalidArgument);
                            return;
                        }

                        string username = null;
                        if (Credential != null)
                        {
                            username = Credential.UserName;
                            password = Marshal.SecureStringToCoTaskMemUnicode(Credential.Password);
                        }

                        bool succeeded = NativeMethods.ChangeServiceConfigW(
                            hService,
                            NativeMethods.SERVICE_NO_CHANGE,
                            dwStartType,
                            NativeMethods.SERVICE_NO_CHANGE,
                            null,
                            null,
                            IntPtr.Zero,
                            null,
                            username,
                            password,
                            DisplayName
                            );
                        if (!succeeded)
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            Win32Exception exception = new(lastError);
                            WriteNonTerminatingError(
                                service,
                                exception,
                                "CouldNotSetService",
                                ServiceResources.CouldNotSetService,
                                ErrorCategory.PermissionDenied);
                            return;
                        }
                    }

                    NativeMethods.SERVICE_DESCRIPTIONW sd = new();
                    sd.lpDescription = Description;
                    int size = Marshal.SizeOf(sd);
                    IntPtr buffer = Marshal.AllocCoTaskMem(size);
                    Marshal.StructureToPtr(sd, buffer, false);

                    bool status = NativeMethods.ChangeServiceConfig2W(
                        hService,
                        NativeMethods.SERVICE_CONFIG_DESCRIPTION,
                        buffer);

                    if (!status)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            service,
                            exception,
                            "CouldNotSetServiceDescription",
                            ServiceResources.CouldNotSetServiceDescription,
                            ErrorCategory.PermissionDenied);
                    }

                    // Set the delayed auto start
                    NativeMethods.SERVICE_DELAYED_AUTO_START_INFO ds = new();
                    ds.fDelayedAutostart = StartupType == ServiceStartupType.AutomaticDelayedStart;
                    size = Marshal.SizeOf(ds);
                    delayedAutoStartInfoBuffer = Marshal.AllocCoTaskMem(size);
                    Marshal.StructureToPtr(ds, delayedAutoStartInfoBuffer, false);

                    status = NativeMethods.ChangeServiceConfig2W(
                        hService,
                        NativeMethods.SERVICE_CONFIG_DELAYED_AUTO_START_INFO,
                        delayedAutoStartInfoBuffer);

                    if (!status)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            Name,
                            DisplayName,
                            Name,
                            exception,
                            "CouldNotSetServiceDelayedAutoStart",
                            ServiceResources.CouldNotSetServiceDelayedAutoStart,
                            ErrorCategory.PermissionDenied);
                    }

                    // Handle the '-Status' parameter
                    if (!string.IsNullOrEmpty(Status))
                    {
                        if (Status.Equals("Running", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!service.Status.Equals(ServiceControllerStatus.Running))
                            {
                                if (service.Status.Equals(ServiceControllerStatus.Paused))
                                    // resume service
                                    DoResumeService(service);
                                else
                                    // start service
                                    DoStartService(service);
                            }
                        }
                        else if (Status.Equals("Stopped", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!service.Status.Equals(ServiceControllerStatus.Stopped))
                            {
                                // Check for the dependent services as set-service dont have force parameter
                                ServiceController[] dependentServices = service.DependentServices;

                                if ((!Force) && (dependentServices != null) && (dependentServices.Length > 0))
                                {
                                    WriteNonTerminatingError(service, null, "ServiceHasDependentServicesNoForce", ServiceResources.ServiceHasDependentServicesNoForce, ErrorCategory.InvalidOperation);
                                    return;
                                }

                                // Stop service, pass 'true' to the force parameter as we have already checked for the dependent services.
                                DoStopService(service, Force, waitForServiceToStop: true);
                            }
                        }
                        else if (Status.Equals("Paused", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!service.Status.Equals(ServiceControllerStatus.Paused))
                            {
                                DoPauseService(service);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(SecurityDescriptorSddl))
                    {
                        SetServiceSecurityDescriptor(service, SecurityDescriptorSddl, hService);
                    }

                    if (PassThru.IsPresent)
                    {
                        // To display the service, refreshing the service would not show the display name after updating
                        ServiceController displayservice = new(Name);
                        WriteObject(displayservice);
                    }
                }
                finally
                {
                    if (delayedAutoStartInfoBuffer != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(delayedAutoStartInfoBuffer);
                    }

                    if (hService != IntPtr.Zero)
                    {
                        bool succeeded = NativeMethods.CloseServiceHandle(hService);
                        if (!succeeded)
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            Win32Exception exception = new(lastError);
                            WriteNonTerminatingError(
                                service,
                                exception,
                                "CouldNotSetServiceDescription",
                                ServiceResources.CouldNotSetServiceDescription,
                                ErrorCategory.PermissionDenied);
                        }
                    }

                    if (hScManager != IntPtr.Zero)
                    {
                        bool succeeded = NativeMethods.CloseServiceHandle(hScManager);
                        if (!succeeded)
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            Win32Exception exception = new(lastError);
                            WriteNonTerminatingError(
                                service,
                                exception,
                                "CouldNotSetServiceDescription",
                                ServiceResources.CouldNotSetServiceDescription,
                                ErrorCategory.PermissionDenied);
                        }
                    }
                }
            }
            finally
            {
                if (password != IntPtr.Zero)
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(password);
                }

                if (objServiceShouldBeDisposed)
                {
                    service.Dispose();
                }
            }
        }
        #endregion Overrides

    }
    #endregion SetServiceCommand

    #region NewServiceCommand
    /// <summary>
    /// This class implements the New-Service command.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Service", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096905")]
    [OutputType(typeof(ServiceController))]
    public class NewServiceCommand : ServiceBaseCommand
    {
        #region Parameters
        /// <summary>
        /// Name of the service to create.
        /// </summary>
        /// <value></value>
        [Parameter(Position = 0, Mandatory = true)]
        [Alias("ServiceName")]
        public string Name
        {
            get { return serviceName; }

            set { serviceName = value; }
        }

        internal string serviceName = null;

        /// <summary>
        /// The executable which implements this service.
        /// </summary>
        /// <value></value>
        [Parameter(Position = 1, Mandatory = true)]
        [Alias("Path")]
        public string BinaryPathName
        {
            get { return binaryPathName; }

            set { binaryPathName = value; }
        }

        internal string binaryPathName = null;

        /// <summary>
        /// DisplayName of the service to create.
        /// </summary>
        /// <value></value>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string DisplayName
        {
            get { return displayName; }

            set { displayName = value; }
        }

        internal string displayName = null;

        /// <summary>
        /// Description of the service to create.
        /// </summary>
        /// <value></value>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Description
        {
            get { return description; }

            set { description = value; }
        }

        internal string description = null;

        /// <summary>
        /// Should the service start automatically?
        /// </summary>
        /// <value></value>
        [Parameter]
        public ServiceStartupType StartupType
        {
            get { return startupType; }

            set { startupType = value; }
        }

        internal ServiceStartupType startupType = ServiceStartupType.Automatic;

        /// <summary>
        /// Account under which the service should run.
        /// </summary>
        /// <value></value>
        [Parameter]
        [Credential()]
        public PSCredential Credential
        {
            get { return credential; }

            set { credential = value; }
        }

        internal PSCredential credential = null;

        /// <summary>
        /// Sets the SecurityDescriptorSddl of the service using a SDDL string.
        /// </summary>
        [Parameter]
        [Alias("sd")]
        [ValidateNotNullOrEmpty]
        public string SecurityDescriptorSddl
        {
            get;
            set;
        }

        /// <summary>
        /// Other services on which the new service depends.
        /// </summary>
        /// <value></value>
        [Parameter]
        public string[] DependsOn
        {
            get { return dependsOn; }

            set { dependsOn = value; }
        }

        internal string[] dependsOn = null;
        #endregion Parameters

        #region Overrides
        /// <summary>
        /// Create the service.
        /// </summary>
        protected override void BeginProcessing()
        {
            ServiceController service = null;
            Diagnostics.Assert(!string.IsNullOrEmpty(Name),
                "null ServiceName");
            Diagnostics.Assert(!string.IsNullOrEmpty(BinaryPathName),
                "null BinaryPathName");

            // confirm the operation first
            // this is always false if WhatIf is set
            if (!ShouldProcessServiceOperation(DisplayName ?? string.Empty, Name))
            {
                return;
            }

            // Connect to the service controller
            NakedWin32Handle hScManager = IntPtr.Zero;
            NakedWin32Handle hService = IntPtr.Zero;
            IntPtr password = IntPtr.Zero;
            IntPtr delayedAutoStartInfoBuffer = IntPtr.Zero;
            try
            {
                hScManager = NativeMethods.OpenSCManagerW(
                    null,
                    null,
                    NativeMethods.SC_MANAGER_CONNECT | NativeMethods.SC_MANAGER_CREATE_SERVICE
                    );
                if (hScManager == IntPtr.Zero)
                {
                    int lastError = Marshal.GetLastWin32Error();
                    Win32Exception exception = new(lastError);
                    WriteNonTerminatingError(
                        Name,
                        DisplayName,
                        Name,
                        exception,
                        "CouldNotNewService",
                        ServiceResources.CouldNotNewService,
                        ErrorCategory.PermissionDenied);
                    return;
                }

                if (!NativeMethods.TryGetNativeStartupType(StartupType, out DWORD dwStartType))
                {
                    WriteNonTerminatingError(StartupType.ToString(), "New-Service", Name,
                        new ArgumentException(), "CouldNotNewService",
                        ServiceResources.UnsupportedStartupType,
                        ErrorCategory.InvalidArgument);
                    return;
                }
                // set up the double-null-terminated lpDependencies parameter
                IntPtr lpDependencies = IntPtr.Zero;
                if (DependsOn != null)
                {
                    int numchars = 1; // final null
                    foreach (string dependedOn in DependsOn)
                    {
                        numchars += dependedOn.Length + 1;
                    }

                    char[] doubleNullArray = new char[numchars];
                    int pos = 0;
                    foreach (string dependedOn in DependsOn)
                    {
                        Array.Copy(
                            dependedOn.ToCharArray(), 0,
                            doubleNullArray, pos,
                            dependedOn.Length
                            );
                        pos += dependedOn.Length;
                        doubleNullArray[pos++] = (char)0; // null terminator
                    }

                    doubleNullArray[pos++] = (char)0; // double-null terminator
                    Diagnostics.Assert(pos == numchars, "lpDependencies build error");
                    lpDependencies = Marshal.AllocHGlobal(
                        numchars * Marshal.SystemDefaultCharSize);
                    Marshal.Copy(doubleNullArray, 0, lpDependencies, numchars);
                }

                // set up the Credential parameter
                string username = null;
                if (Credential != null)
                {
                    username = Credential.UserName;
                    password = Marshal.SecureStringToCoTaskMemUnicode(Credential.Password);
                }

                // Create the service
                hService = NativeMethods.CreateServiceW(
                    hScManager,
                    Name,
                    DisplayName,
                    NativeMethods.SERVICE_CHANGE_CONFIG | NativeMethods.WRITE_DAC | NativeMethods.WRITE_OWNER,
                    NativeMethods.SERVICE_WIN32_OWN_PROCESS,
                    dwStartType,
                    NativeMethods.SERVICE_ERROR_NORMAL,
                    BinaryPathName,
                    null,
                    null,
                    lpDependencies,
                    username,
                    password
                    );
                if (hService == IntPtr.Zero)
                {
                    int lastError = Marshal.GetLastWin32Error();
                    Win32Exception exception = new(lastError);
                    WriteNonTerminatingError(
                        Name,
                        DisplayName,
                        Name,
                        exception,
                        "CouldNotNewService",
                        ServiceResources.CouldNotNewService,
                        ErrorCategory.PermissionDenied);
                    return;
                }

                // Set the service description
                NativeMethods.SERVICE_DESCRIPTIONW sd = new();
                sd.lpDescription = Description;
                int size = Marshal.SizeOf(sd);
                IntPtr buffer = Marshal.AllocCoTaskMem(size);
                Marshal.StructureToPtr(sd, buffer, false);

                bool succeeded = NativeMethods.ChangeServiceConfig2W(
                    hService,
                    NativeMethods.SERVICE_CONFIG_DESCRIPTION,
                    buffer);

                if (!succeeded)
                {
                    int lastError = Marshal.GetLastWin32Error();
                    Win32Exception exception = new(lastError);
                    WriteNonTerminatingError(
                        Name,
                        DisplayName,
                        Name,
                        exception,
                        "CouldNotNewServiceDescription",
                        ServiceResources.CouldNotNewServiceDescription,
                        ErrorCategory.PermissionDenied);
                }

                // Set the delayed auto start
                if (StartupType == ServiceStartupType.AutomaticDelayedStart)
                {
                    NativeMethods.SERVICE_DELAYED_AUTO_START_INFO ds = new();
                    ds.fDelayedAutostart = true;
                    size = Marshal.SizeOf(ds);
                    delayedAutoStartInfoBuffer = Marshal.AllocCoTaskMem(size);
                    Marshal.StructureToPtr(ds, delayedAutoStartInfoBuffer, false);

                    succeeded = NativeMethods.ChangeServiceConfig2W(
                        hService,
                        NativeMethods.SERVICE_CONFIG_DELAYED_AUTO_START_INFO,
                        delayedAutoStartInfoBuffer);

                    if (!succeeded)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            Name,
                            DisplayName,
                            Name,
                            exception,
                            "CouldNotNewServiceDelayedAutoStart",
                            ServiceResources.CouldNotNewServiceDelayedAutoStart,
                            ErrorCategory.PermissionDenied);
                    }
                }

                // write the ServiceController for the new service
                service = new ServiceController(Name);

                if (!string.IsNullOrEmpty(SecurityDescriptorSddl))
                {
                    SetServiceSecurityDescriptor(service, SecurityDescriptorSddl, hService);
                }

                WriteObject(service);
            }
            finally
            {
                if (delayedAutoStartInfoBuffer != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(delayedAutoStartInfoBuffer);
                }

                if (password != IntPtr.Zero)
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(password);
                }

                if (hService != IntPtr.Zero)
                {
                    bool succeeded = NativeMethods.CloseServiceHandle(hService);
                    if (!succeeded)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            Name,
                            DisplayName,
                            Name,
                            exception,
                            "CouldNotNewServiceDescription",
                            ServiceResources.CouldNotNewServiceDescription,
                            ErrorCategory.PermissionDenied);
                    }
                }

                if (hScManager != IntPtr.Zero)
                {
                    bool succeeded = NativeMethods.CloseServiceHandle(hScManager);
                    if (!succeeded)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            Name,
                            DisplayName,
                            Name,
                            exception,
                            "CouldNotNewServiceDescription",
                            ServiceResources.CouldNotNewServiceDescription,
                            ErrorCategory.PermissionDenied);
                    }
                }
            }
        }
        #endregion Overrides
    }
    #endregion NewServiceCommand

    #region RemoveServiceCommand
    /// <summary>
    /// This class implements the Remove-Service command.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "Service", SupportsShouldProcess = true, DefaultParameterSetName = "Name")]
    public class RemoveServiceCommand : ServiceBaseCommand
    {
        #region Parameters

        /// <summary>
        /// Name of the service to remove.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Name")]
        [Alias("ServiceName", "SN")]
        public string Name { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies ServiceController object representing the services to be removed.
        /// Enter a variable that contains the objects or type a command or expression
        /// that gets the objects.
        /// </summary>
        [Parameter(ValueFromPipeline = true, ParameterSetName = "InputObject")]
        public ServiceController InputObject { get; set; }

        #endregion Parameters

        #region Overrides
        /// <summary>
        /// Remove the service.
        /// </summary>
        protected override void ProcessRecord()
        {
            ServiceController service = null;
            bool objServiceShouldBeDisposed = false;
            try
            {
                if (InputObject != null)
                {
                    service = InputObject;
                    Name = service.ServiceName;
                    objServiceShouldBeDisposed = false;
                }
                else
                {
                    service = new ServiceController(Name);
                    objServiceShouldBeDisposed = true;
                }

                Diagnostics.Assert(!string.IsNullOrEmpty(Name), "null ServiceName");

                // "new ServiceController" will succeed even if there is no such service.
                // This checks whether the service actually exists.
                string unusedByDesign = service.DisplayName;
            }
            catch (ArgumentException ex)
            {
                // Cannot use WriteNonterminatingError as service is null
                ErrorRecord er = new(ex, "ArgumentException", ErrorCategory.ObjectNotFound, Name);
                WriteError(er);
                return;
            }
            catch (InvalidOperationException ex)
            {
                // Cannot use WriteNonterminatingError as service is null
                ErrorRecord er = new(ex, "InvalidOperationException", ErrorCategory.ObjectNotFound, Name);
                WriteError(er);
                return;
            }

            try // In finally we ensure dispose, if object not pipelined.
            {
                // Confirm the operation first.
                // This is always false if WhatIf is set.
                if (!ShouldProcessServiceOperation(service))
                {
                    return;
                }

                NakedWin32Handle hScManager = IntPtr.Zero;
                NakedWin32Handle hService = IntPtr.Zero;
                try
                {
                    hScManager = NativeMethods.OpenSCManagerW(
                        lpMachineName: string.Empty,
                        lpDatabaseName: null,
                        dwDesiredAccess: NativeMethods.SC_MANAGER_ALL_ACCESS
                        );
                    if (hScManager == IntPtr.Zero)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteObject(exception);
                        WriteNonTerminatingError(
                            service,
                            exception,
                            "FailToOpenServiceControlManager",
                            ServiceResources.FailToOpenServiceControlManager,
                            ErrorCategory.PermissionDenied);
                        return;
                    }

                    hService = NativeMethods.OpenServiceW(
                        hScManager,
                        Name,
                        NativeMethods.SERVICE_DELETE
                        );
                    if (hService == IntPtr.Zero)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            service,
                            exception,
                            "CouldNotRemoveService",
                            ServiceResources.CouldNotRemoveService,
                            ErrorCategory.PermissionDenied);
                        return;
                    }

                    bool status = NativeMethods.DeleteService(hService);

                    if (!status)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            service,
                            exception,
                            "CouldNotRemoveService",
                            ServiceResources.CouldNotRemoveService,
                            ErrorCategory.PermissionDenied);
                    }
                }
                finally
                {
                    if (hService != IntPtr.Zero)
                    {
                        bool succeeded = NativeMethods.CloseServiceHandle(hService);
                        if (!succeeded)
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            Diagnostics.Assert(lastError != 0, "ErrorCode not success");
                        }
                    }

                    if (hScManager != IntPtr.Zero)
                    {
                        bool succeeded = NativeMethods.CloseServiceHandle(hScManager);
                        if (!succeeded)
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            Diagnostics.Assert(lastError != 0, "ErrorCode not success");
                        }
                    }
                }
            }
            finally
            {
                if (objServiceShouldBeDisposed)
                {
                    service.Dispose();
                }
            }
        }
        #endregion Overrides
    }
    #endregion RemoveServiceCommand

    #region ServiceCommandException
    /// <summary>
    /// Non-terminating errors occurring in the service noun commands.
    /// </summary>
    [Serializable]
    public class ServiceCommandException : SystemException
    {
        #region ctors
        /// <summary>
        /// Unimplemented standard constructor.
        /// </summary>
        /// <returns>Doesn't return.</returns>
        public ServiceCommandException()
            : base()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public ServiceCommandException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ServiceCommandException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion ctors

        #region Serialization
        /// <summary>
        /// Serialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        /// <returns>Constructed object.</returns>
        protected ServiceCommandException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ArgumentNullException.ThrowIfNull(info);

            _serviceName = info.GetString("ServiceName");
        }
        /// <summary>
        /// Serializer.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            base.GetObjectData(info, context);
            info.AddValue("ServiceName", _serviceName);
        }
        #endregion Serialization

        #region Properties
        /// <summary>
        /// Name of the service which could not be found or operated upon.
        /// </summary>
        /// <value></value>
        public string ServiceName
        {
            get { return _serviceName; }

            set { _serviceName = value; }
        }

        private string _serviceName = string.Empty;
        #endregion Properties
    }
    #endregion ServiceCommandException

    #region NativeMethods
    internal static class NativeMethods
    {
        // from winuser.h
        internal const int ERROR_SERVICE_ALREADY_RUNNING = 1056;
        internal const int ERROR_SERVICE_NOT_ACTIVE = 1062;
        internal const int ERROR_INSUFFICIENT_BUFFER = 122;
        internal const DWORD ERROR_ACCESS_DENIED = 0x5;
        internal const DWORD SC_MANAGER_CONNECT = 1;
        internal const DWORD SC_MANAGER_CREATE_SERVICE = 2;
        internal const DWORD SC_MANAGER_ALL_ACCESS = 0xf003f;
        internal const DWORD SERVICE_QUERY_CONFIG = 1;
        internal const DWORD SERVICE_CHANGE_CONFIG = 2;
        internal const DWORD SERVICE_DELETE = 0x10000;
        internal const DWORD SERVICE_NO_CHANGE = 0xffffffff;
        internal const DWORD SERVICE_AUTO_START = 0x2;
        internal const DWORD SERVICE_DEMAND_START = 0x3;
        internal const DWORD SERVICE_DISABLED = 0x4;
        internal const DWORD SERVICE_CONFIG_DESCRIPTION = 1;
        internal const DWORD SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3;
        internal const DWORD SERVICE_CONFIG_SERVICE_SID_INFO = 5;
        internal const DWORD WRITE_DAC = 262144;
        internal const DWORD WRITE_OWNER = 524288;
        internal const DWORD SERVICE_WIN32_OWN_PROCESS = 0x10;
        internal const DWORD SERVICE_ERROR_NORMAL = 1;

        // from winnt.h
        [DllImport(PinvokeDllNames.OpenSCManagerWDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        NakedWin32Handle OpenSCManagerW(
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpMachineName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpDatabaseName,
            DWORD dwDesiredAccess
            );

        [DllImport(PinvokeDllNames.OpenServiceWDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        NakedWin32Handle OpenServiceW(
            NakedWin32Handle hSCManager,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpServiceName,
            DWORD dwDesiredAccess
            );

        [DllImport(PinvokeDllNames.QueryServiceConfigDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        bool QueryServiceConfigW(
            NakedWin32Handle hSCManager,
            IntPtr lpServiceConfig,
            DWORD cbBufSize,
            out DWORD pcbBytesNeeded
            );

        [DllImport(PinvokeDllNames.QueryServiceConfig2DllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        bool QueryServiceConfig2W(
            NakedWin32Handle hService,
            DWORD dwInfoLevel,
            IntPtr lpBuffer,
            DWORD cbBufSize,
            out DWORD pcbBytesNeeded
            );

        [DllImport(PinvokeDllNames.CloseServiceHandleDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        bool CloseServiceHandle(
            NakedWin32Handle hSCManagerOrService
            );

        [DllImport(PinvokeDllNames.DeleteServiceDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        bool DeleteService(
            NakedWin32Handle hService
            );

        [DllImport(PinvokeDllNames.ChangeServiceConfigWDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        bool ChangeServiceConfigW(
            NakedWin32Handle hService,
            DWORD dwServiceType,
            DWORD dwStartType,
            DWORD dwErrorControl,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpBinaryPathName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpLoadOrderGroup,
            IntPtr lpdwTagId,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpDependencies,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpServiceStartName,
            [In] IntPtr lpPassword,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpDisplayName
            );

        [DllImport(PinvokeDllNames.ChangeServiceConfig2WDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        bool ChangeServiceConfig2W(
            NakedWin32Handle hService,
            DWORD dwInfoLevel,
            IntPtr lpInfo
            );

        [StructLayout(LayoutKind.Sequential)]
        internal struct SERVICE_DESCRIPTIONW
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string lpDescription;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct QUERY_SERVICE_CONFIG
        {
            internal uint dwServiceType;
            internal uint dwStartType;
            internal uint dwErrorControl;
            [MarshalAs(UnmanagedType.LPWStr)] internal string lpBinaryPathName;
            [MarshalAs(UnmanagedType.LPWStr)] internal string lpLoadOrderGroup;
            internal uint dwTagId;
            [MarshalAs(UnmanagedType.LPWStr)] internal string lpDependencies;
            [MarshalAs(UnmanagedType.LPWStr)] internal string lpServiceStartName;
            [MarshalAs(UnmanagedType.LPWStr)] internal string lpDisplayName;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SERVICE_DELAYED_AUTO_START_INFO
        {
            internal bool fDelayedAutostart;
        }

        [DllImport(PinvokeDllNames.CreateServiceWDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        NakedWin32Handle CreateServiceW(
            NakedWin32Handle hSCManager,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpServiceName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpDisplayName,
            DWORD dwDesiredAccess,
            DWORD dwServiceType,
            DWORD dwStartType,
            DWORD dwErrorControl,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpBinaryPathName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpLoadOrderGroup,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpdwTagId,
            [In] IntPtr lpDependencies,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpServiceStartName,
            [In] IntPtr lpPassword
        );

        [DllImport(PinvokeDllNames.SetServiceObjectSecurityDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern
        bool SetServiceObjectSecurity(
            NakedWin32Handle hSCManager,
            System.Security.AccessControl.SecurityInfos dwSecurityInformation,
            byte[] lpSecurityDescriptor
            );

        /// <summary>
        /// CreateJobObject API creates or opens a job object.
        /// </summary>
        /// <param name="lpJobAttributes">
        /// A pointer to a SECURITY_ATTRIBUTES structure that specifies the security descriptor for the
        /// job object and determines whether child processes can inherit the returned handle.
        /// If lpJobAttributes is NULL, the job object gets a default security descriptor
        /// and the handle cannot be inherited.
        /// </param>
        /// <param name="lpName">
        /// The name of the job.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the job object.
        /// If the object existed before the function call, the function
        /// returns a handle to the existing job object.
        /// </returns>
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        /// <summary>
        /// Retrieves job state information from the job object.
        /// </summary>
        /// <param name="hJob">
        /// A handle to the job whose information is being queried.
        /// </param>
        /// <param name="JobObjectInfoClass">
        /// The information class for the limits to be queried.
        /// </param>
        /// <param name="lpJobObjectInfo">
        /// The limit or job state information.
        /// </param>
        /// <param name="cbJobObjectLength">
        /// The count of the job information being queried, in bytes.
        /// </param>
        /// <param name="lpReturnLength">
        /// A pointer to a variable that receives the length of
        /// data written to the structure pointed to by the lpJobObjectInfo parameter.
        /// </param>
        /// <returns>If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero.
        /// </returns>
        [DllImport("Kernel32.dll", EntryPoint = "QueryInformationJobObject", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryInformationJobObject(SafeHandle hJob, int JobObjectInfoClass,
                                    ref JOBOBJECT_BASIC_PROCESS_ID_LIST lpJobObjectInfo,
                                    int cbJobObjectLength, IntPtr lpReturnLength);

        internal static bool QueryServiceConfig(NakedWin32Handle hService, out NativeMethods.QUERY_SERVICE_CONFIG configStructure)
        {
            IntPtr lpBuffer = IntPtr.Zero;
            configStructure = default(NativeMethods.QUERY_SERVICE_CONFIG);
            DWORD bufferSize, bufferSizeNeeded = 0;
            bool status = NativeMethods.QueryServiceConfigW(
                hSCManager: hService,
                lpServiceConfig: lpBuffer,
                cbBufSize: 0,
                pcbBytesNeeded: out bufferSizeNeeded);

            if (!status && Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER)
            {
                return status;
            }

            try
            {
                lpBuffer = Marshal.AllocCoTaskMem((int)bufferSizeNeeded);
                bufferSize = bufferSizeNeeded;

                status = NativeMethods.QueryServiceConfigW(
                    hService,
                    lpBuffer,
                    bufferSize,
                    out bufferSizeNeeded);
                configStructure = (NativeMethods.QUERY_SERVICE_CONFIG)Marshal.PtrToStructure(lpBuffer, typeof(NativeMethods.QUERY_SERVICE_CONFIG));
            }
            finally
            {
                Marshal.FreeCoTaskMem(lpBuffer);
            }

            return status;
        }

        internal static bool QueryServiceConfig2<T>(NakedWin32Handle hService, DWORD infolevel, out T configStructure)
        {
            IntPtr lpBuffer = IntPtr.Zero;
            configStructure = default(T);
            DWORD bufferSize, bufferSizeNeeded = 0;

            bool status = NativeMethods.QueryServiceConfig2W(
                hService: hService,
                dwInfoLevel: infolevel,
                lpBuffer: lpBuffer,
                cbBufSize: 0,
                pcbBytesNeeded: out bufferSizeNeeded);

            if (!status && Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER)
            {
                return status;
            }

            try
            {
                lpBuffer = Marshal.AllocCoTaskMem((int)bufferSizeNeeded);
                bufferSize = bufferSizeNeeded;

                status = NativeMethods.QueryServiceConfig2W(
                    hService,
                    infolevel,
                    lpBuffer,
                    bufferSize,
                    out bufferSizeNeeded);
                configStructure = (T)Marshal.PtrToStructure(lpBuffer, typeof(T));
            }
            finally
            {
                Marshal.FreeCoTaskMem(lpBuffer);
            }

            return status;
        }

        /// <summary>
        /// Get appropriate win32 StartupType.
        /// </summary>
        /// <param name="StartupType">
        /// StartupType provided by the user.
        /// </param>
        /// <param name="dwStartType">
        /// Out parameter of the native win32 StartupType
        /// </param>
        /// <returns>
        /// If a supported StartupType is provided, funciton returns true, otherwise false.
        /// </returns>
        internal static bool TryGetNativeStartupType(ServiceStartupType StartupType, out DWORD dwStartType)
        {
            bool success = true;
            dwStartType = NativeMethods.SERVICE_NO_CHANGE;
            switch (StartupType)
            {
                case ServiceStartupType.Automatic:
                case ServiceStartupType.AutomaticDelayedStart:
                    dwStartType = NativeMethods.SERVICE_AUTO_START;
                    break;
                case ServiceStartupType.Manual:
                    dwStartType = NativeMethods.SERVICE_DEMAND_START;
                    break;
                case ServiceStartupType.Disabled:
                    dwStartType = NativeMethods.SERVICE_DISABLED;
                    break;
                case ServiceStartupType.InvalidValue:
                    dwStartType = NativeMethods.SERVICE_NO_CHANGE;
                    break;
                default:
                    success = false;
                    break;
            }

            return success;
        }

        internal static ServiceStartupType GetServiceStartupType(ServiceStartMode startMode, bool delayedAutoStart)
        {
            ServiceStartupType result = ServiceStartupType.Disabled;
            switch (startMode)
            {
                case ServiceStartMode.Automatic:
                    result = delayedAutoStart ? ServiceStartupType.AutomaticDelayedStart : ServiceStartupType.Automatic;
                    break;
                case ServiceStartMode.Manual:
                    result = ServiceStartupType.Manual;
                    break;
                case ServiceStartMode.Disabled:
                    result = ServiceStartupType.Disabled;
                    break;
            }

            return result;
        }
    }
    #endregion NativeMethods

    #region ServiceStartupType
    /// <summary>
    /// Enum for usage with StartupType. Automatic, Manual and Disabled index matched from System.ServiceProcess.ServiceStartMode
    /// </summary>
    public enum ServiceStartupType
    {
        /// <summary>Invalid service</summary>
        InvalidValue = -1,
        /// <summary>Automatic service</summary>
        Automatic = 2,
        /// <summary>Manual service</summary>
        Manual = 3,
        /// <summary>Disabled service</summary>
        Disabled = 4,
        /// <summary>Automatic (Delayed Start) service</summary>
        AutomaticDelayedStart = 10
    }
    #endregion ServiceStartupType
}

#endif // Not built on Unix
