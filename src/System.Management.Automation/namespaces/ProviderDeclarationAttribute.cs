// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace System.Management.Automation.Provider
{
    /// <summary>
    /// Declares a class as a Cmdlet provider.
    /// </summary>
    /// <remarks>
    /// The class must be derived from System.Management.Automation.Provider.CmdletProvider to
    /// be recognized by the runspace.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CmdletProviderAttribute : Attribute
    {
        /// <summary>
        /// Constructor for the attribute.
        /// </summary>
        /// <param name="providerName">
        /// The provider name.
        /// </param>
        /// <param name="providerCapabilities">
        /// An enumeration of the capabilities that the provider implements beyond the
        /// default capabilities that are required.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="providerName"/> is null or empty.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="providerName"/> contains any of the following characters: \ [ ] ? * :
        /// </exception>
        public CmdletProviderAttribute(
            string providerName,
            ProviderCapabilities providerCapabilities)
        {
            // verify parameters

            if (string.IsNullOrEmpty(providerName))
            {
                throw PSTraceSource.NewArgumentNullException("providerName");
            }

            if (providerName.IndexOfAny(_illegalCharacters) != -1)
            {
                throw PSTraceSource.NewArgumentException(
                    "providerName",
                    SessionStateStrings.ProviderNameNotValid,
                    providerName);
            }

            ProviderName = providerName;
            ProviderCapabilities = providerCapabilities;
        }

        private char[] _illegalCharacters = new char[] { ':', '\\', '[', ']', '?', '*' };

        /// <summary>
        /// Gets the name of the provider.
        /// </summary>
        public string ProviderName { get; } = string.Empty;

        /// <summary>
        /// Gets the flags that represent the capabilities of the provider.
        /// </summary>
        public ProviderCapabilities ProviderCapabilities { get; } = ProviderCapabilities.None;

        #region private data

        #endregion private data
    }

    /// <summary>
    /// This enumeration defines the capabilities that the provider implements.
    /// </summary>
    [Flags]
    public enum ProviderCapabilities
    {
        /// <summary>
        /// The provider does not add any additional capabilities beyond what the
        /// Monad engine provides.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// The provider does the inclusion filtering for those commands that take an Include
        /// parameter. The Monad engine should not try to do the filtering on behalf of this
        /// provider.
        /// </summary>
        /// <remarks>
        /// Note, the provider should make every effort to filter in a way that is consistent
        /// with the Monad engine. This option is allowed because in many cases the provider
        /// can be much more efficient at filtering.
        /// </remarks>
        Include = 0x1,

        /// <summary>
        /// The provider does the exclusion filtering for those commands that take an Exclude
        /// parameter. The Monad engine should not try to do the filtering on behalf of this
        /// provider.
        /// </summary>
        /// <remarks>
        /// Note, the provider should make every effort to filter in a way that is consistent
        /// with the Monad engine. This option is allowed because in many cases the provider
        /// can be much more efficient at filtering.
        /// </remarks>
        Exclude = 0x2,

        /// <summary>
        /// The provider can take a provider specific filter string.
        /// </summary>
        /// <remarks>
        /// When this attribute is specified a provider specific filter can be passed from
        /// the Core Commands to the provider. This filter string is not interpreted in any
        /// way by the Monad engine.
        /// </remarks>
        Filter = 0x4,

        /// <summary>
        /// The provider does the wildcard matching for those commands that allow for it. The Monad
        /// engine should not try to do the wildcard matching on behalf of the provider when this
        /// flag is set.
        /// </summary>
        /// <remarks>
        /// Note, the provider should make every effort to do the wildcard matching in a way that is consistent
        /// with the Monad engine. This option is allowed because in many cases wildcard matching
        /// cannot occur via the path name or because the provider can do the matching in a much more
        /// efficient manner.
        /// </remarks>
        ExpandWildcards = 0x8,

        /// <summary>
        /// The provider supports ShouldProcess. When this capability is specified, the
        /// -Whatif and -Confirm parameters become available to the user when using
        /// this provider.
        /// </summary>
        ShouldProcess = 0x10,

        /// <summary>
        /// The provider supports credentials. When this capability is specified and
        /// the user passes credentials to the core cmdlets, those credentials will
        /// be passed to the provider. If the provider doesn't specify this capability
        /// and the user passes credentials, an exception is thrown.
        /// </summary>
        Credentials = 0x20,

        /// <summary>
        /// The provider supports transactions. When this capability is specified, PowerShell
        /// lets the provider participate in the current PowerShell transaction.
        /// The provider does not support this capability and the user attempts to apply a
        /// transaction to it, an exception is thrown.
        /// </summary>
        Transactions = 0x40,
    }
}
