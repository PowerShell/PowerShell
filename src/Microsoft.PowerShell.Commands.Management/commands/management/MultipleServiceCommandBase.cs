// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

using System;
using System.Collections.Generic;
using System.Management.Automation;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{

    /// <summary>
    /// This class implements the base for service commands on Unix platforms which can
    /// operate on multiple services.
    /// </summary>
    public abstract class MultipleServiceCommandBase : ServiceBaseCommand
    {

        /// <summary>
        /// Holds the selection mode setting.
        /// </summary>
        internal SelectionMode CurrentSelectionMode;

        /// <summary>
        /// Holds the service names. The Name parameter is declared in subclasses,
        /// since it is optional for Get-Service and mandatory otherwise.
        /// </summary>
        internal string[]? ServiceNames;

        /// <summary>
        /// Holds the display names. The DisplayName parameter is declared above,
        /// since it is optional for Get-Service and mandatory otherwise.
        /// </summary>
        internal string[]? DisplayNames;

        private UnixServiceController[]? inputObjects;
        private List<UnixServiceController>? allServices;

        /// <summary>
        /// The various service selection modes.
        /// </summary>
        internal enum SelectionMode
        {
            /// <summary>
            /// Select all services.
            /// </summary>
            Default = 0,

            /// <summary>
            /// Select services matching the supplied display names.
            /// </summary>
            DisplayName = 1,

            /// <summary>
            /// Select services based on pipeline input.
            /// </summary>
            InputObject = 2,

            /// <summary>
            /// Select services by service name.
            /// </summary>
            ServiceName = 3,
        }

        /// <summary>
        /// Gets or sets an array of display names for services.
        /// </summary>
        [Parameter(ParameterSetName = "DisplayName", Mandatory = true)]
        public string[]? DisplayName
        {
            get
            {
                return DisplayNames;
            }

            set
            {
                DisplayNames = value;
                CurrentSelectionMode = SelectionMode.DisplayName;
            }
        }

        /// <summary>
        /// Gets or sets the services to include in the operation. Services not matching one of these
        /// (if specified) are excluded. These are interpreted as either service names
        /// or display names according to the parameter set.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string[]? Include { get; set; }

        /// <summary>
        /// Gets or sets the services to exclude from the operation. Services matching one of these
        /// (if specified) are excluded. These are interpreted as either service names
        /// or display names according to the parameter set.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string[]? Exclude { get; set; }

        /// <summary>
        /// Gets or sets the service objects to operate on. This bypasses the Name and
        /// DisplayName parameters and reads the services directly, which allows dealing
        /// with services that have wildcard characters in their name.
        /// </summary>
        [Parameter(ParameterSetName = "InputObject", ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public UnixServiceController[]? InputObject
        {
            get
            {
                return inputObjects;
            }

            set
            {
                inputObjects = value;
                CurrentSelectionMode = SelectionMode.InputObject;
            }
        }

        /// <summary>
        /// Gets an array of all services.
        /// </summary>
        internal List<UnixServiceController> AllServices => allServices ??= EnumerateServices();

        /// <summary>
        /// Gets a single service by name without wildcard characters.
        /// </summary>
        /// <param name="nameOfService">The name of the service, with or without the '.service' suffix.</param>
        /// <returns>The service, or null when it was not found.</returns>
        internal UnixServiceController? GetOneService(string nameOfService)
        {
            Dbg.Assert(!WildcardPattern.ContainsWildcardCharacters(nameOfService), "Caller should verify that nameOfService doesn't contain wildcard characters");

            string name = UnixServiceParser.RemoveServiceSuffix(nameOfService.Trim());
            foreach (UnixServiceController service in AllServices)
            {
                if (string.Equals(service.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    // Services that were never loaded are only known by name; fetch
                    // their details from the service manager when it supports it.
                    RefreshServiceDetails(service);
                    return service;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieve the list of all services matching the ServiceName, DisplayName,
        /// Include and Exclude parameters, sorted by service name.
        /// </summary>
        /// <returns>The matching services.</returns>
        internal List<UnixServiceController> MatchingServices()
        {
            List<UnixServiceController> matchingServices = CurrentSelectionMode switch
            {
                SelectionMode.DisplayName => MatchingServicesByDisplayName(),
                SelectionMode.InputObject => MatchingServicesByInput(),
                _ => MatchingServicesByServiceName(),
            };

            matchingServices.Sort(static (x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
            return matchingServices;
        }

        /// <summary>
        /// Check whether the service name matches the wildcard pattern, accepting both
        /// the bare name and the '.service'-suffixed unit name.
        /// </summary>
        /// <param name="wildcard">The wildcard pattern.</param>
        /// <param name="service">The service.</param>
        /// <returns>True when the pattern matches the service name.</returns>
        private static bool MatchesServiceName(WildcardPattern wildcard, UnixServiceController service)
        {
            return wildcard.IsMatch(service.Name) || wildcard.IsMatch(service.Name + ".service");
        }

        /// <summary>
        /// Retrieves the list of all services matching the ServiceName, Include and
        /// Exclude parameters. Generates a non-terminating error for each specified
        /// service name which is not found even though it contains no wildcards.
        /// </summary>
        /// <returns>The matching services.</returns>
        private List<UnixServiceController> MatchingServicesByServiceName()
        {
            List<UnixServiceController> matchingServices = new();

            if (ServiceNames is null)
            {
                foreach (UnixServiceController service in AllServices)
                {
                    IncludeExcludeAdd(matchingServices, service, checkDuplicates: false);
                }

                return matchingServices;
            }

            foreach (string pattern in ServiceNames)
            {
                bool found = false;

                if (WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    WildcardPattern wildcard = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                    foreach (UnixServiceController service in AllServices)
                    {
                        if (!MatchesServiceName(wildcard, service))
                        {
                            continue;
                        }

                        found = true;
                        IncludeExcludeAdd(matchingServices, service, checkDuplicates: true);
                    }
                }
                else
                {
                    UnixServiceController? service = GetOneService(pattern);
                    if (service is not null)
                    {
                        found = true;
                        IncludeExcludeAdd(matchingServices, service, checkDuplicates: true);
                    }
                }

                if (!found && !WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    WriteNonTerminatingError(
                        pattern,
                        string.Empty,
                        pattern,
                        innerException: null,
                        "NoServiceFoundForGivenName",
                        ServiceResources.NoServiceFoundForGivenName,
                        ErrorCategory.ObjectNotFound);
                }
            }

            return matchingServices;
        }

        /// <summary>
        /// Retrieves the list of all services matching the DisplayName, Include and
        /// Exclude parameters. Generates a non-terminating error for each specified
        /// display name which is not found even though it contains no wildcards.
        /// </summary>
        /// <returns>The matching services.</returns>
        private List<UnixServiceController> MatchingServicesByDisplayName()
        {
            List<UnixServiceController> matchingServices = new();
            if (DisplayName is null)
            {
                Dbg.Assert(false, "null DisplayName");
                throw PSTraceSource.NewInvalidOperationException();
            }

            foreach (string pattern in DisplayName)
            {
                WildcardPattern wildcard = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                bool found = false;
                foreach (UnixServiceController service in AllServices)
                {
                    if (!wildcard.IsMatch(service.DisplayName))
                    {
                        continue;
                    }

                    found = true;
                    IncludeExcludeAdd(matchingServices, service, checkDuplicates: true);
                }

                if (!found && !WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    WriteNonTerminatingError(
                        string.Empty,
                        pattern,
                        pattern,
                        innerException: null,
                        "NoServiceFoundForGivenDisplayName",
                        ServiceResources.NoServiceFoundForGivenDisplayName,
                        ErrorCategory.ObjectNotFound);
                }
            }

            return matchingServices;
        }

        /// <summary>
        /// Retrieves the list of all services matching the InputObject, Include and
        /// Exclude parameters.
        /// </summary>
        /// <returns>The matching services.</returns>
        private List<UnixServiceController> MatchingServicesByInput()
        {
            List<UnixServiceController> matchingServices = new();
            if (InputObject is null)
            {
                Dbg.Assert(false, "null InputObject");
                throw PSTraceSource.NewInvalidOperationException();
            }

            foreach (UnixServiceController service in InputObject)
            {
                IncludeExcludeAdd(matchingServices, service, checkDuplicates: false);
            }

            return matchingServices;
        }

        /// <summary>
        /// Add <paramref name="service"/> to <paramref name="list"/>, but only if it
        /// passes the Include and Exclude filters (if present) and (if
        /// <paramref name="checkDuplicates"/>) if it is not already on
        /// <paramref name="list"/>.
        /// </summary>
        /// <param name="list">List of services.</param>
        /// <param name="service">Service to add to list.</param>
        /// <param name="checkDuplicates">Check list for duplicates.</param>
        private void IncludeExcludeAdd(
            List<UnixServiceController> list,
            UnixServiceController service,
            bool checkDuplicates)
        {
            if (Include is not null && !Matches(service, Include))
            {
                return;
            }

            if (Exclude is not null && Matches(service, Exclude))
            {
                return;
            }

            if (checkDuplicates)
            {
                foreach (UnixServiceController sc in list)
                {
                    if (string.Equals(sc.Name, service.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }

            list.Add(service);
        }

        /// <summary>
        /// Check whether <paramref name="service"/> matches the list of patterns in
        /// <paramref name="matchList"/>.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="matchList">The list of wildcard patterns.</param>
        /// <returns>True when the service matches one of the patterns.</returns>
        private bool Matches(UnixServiceController service, string[] matchList)
        {
            if (matchList is null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(matchList));
            }

            string serviceID = (CurrentSelectionMode == SelectionMode.DisplayName)
                                ? service.DisplayName
                                : service.Name;
            foreach (string pattern in matchList)
            {
                WildcardPattern wildcard = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                if (wildcard.IsMatch(serviceID))
                {
                    return true;
                }
            }

            return false;
        }

    }
}

#endif
