// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Provider;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Exposes the APIs to manage the Cmdlet Providers the Cmdlet base class. The methods of this class
    /// get and set provider data in session state.
    /// </summary>
    public sealed class CmdletProviderManagementIntrinsics
    {
        #region Constructors

        /// <summary>
        /// Hide the default constructor since we always require an instance of SessionState.
        /// </summary>
        private CmdletProviderManagementIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        /// <summary>
        /// The facade for managing providers.
        /// </summary>
        /// <param name="sessionState">
        /// The session to which this is a facade.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sessionState"/> is null.
        /// </exception>
        internal CmdletProviderManagementIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _sessionState = sessionState;
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Gets the specified provider(s).
        /// </summary>
        /// <param name="name">
        /// Either the fully-qualified or friendly name for the provider.
        /// </param>
        /// <returns>
        /// The provider information for the specified provider.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the provider specified by <paramref name="name"/> is not currently
        /// loaded.
        /// </exception>
        public Collection<ProviderInfo> Get(string name)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetProvider(name);
        }

        /// <summary>
        /// Gets the specified provider(s).
        /// </summary>
        /// <param name="name">
        /// Either the fully-qualified or friendly name for the provider.
        /// </param>
        /// <returns>
        /// The provider information for the specified provider.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        /// <exception cref="ProviderNameAmbiguousException">
        /// If <paramref name="name"/> is not PSSnapin-qualified and more than one provider
        /// exists with the specified name.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the provider specified by <paramref name="name"/> is not currently
        /// loaded.
        /// </exception>
        public ProviderInfo GetOne(string name)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetSingleProvider(name);
        }

        /// <summary>
        /// Gets all the Cmdlet Providers that are loaded.
        /// </summary>
        public IEnumerable<ProviderInfo> GetAll()
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.ProviderList;
        }

        #endregion Public methods

        #region Internal methods

        /// <summary>
        /// Determines if the specified provider has the specified capability.
        /// </summary>
        /// <param name="capability">
        /// The capability to check the provider for.
        /// </param>
        /// <param name="provider">
        /// The provider information to use for the check.
        /// </param>
        /// <returns>
        /// True, if the provider has the capability, false otherwise.
        /// </returns>
        internal static bool CheckProviderCapabilities(
            ProviderCapabilities capability,
            ProviderInfo provider)
        {
            // Check the capability

            return (provider.Capabilities & capability) != 0;
        }

        /// <summary>
        /// Gets the count of the number of providers that are loaded.
        /// </summary>
        internal int Count
        {
            get
            {
                return _sessionState.ProviderCount;
            }
        }

        #endregion Internal methods

        #region private data

        private readonly SessionStateInternal _sessionState;

        #endregion private data
    }
}
