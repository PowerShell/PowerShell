// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Dbg = System.Management.Automation;
using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    /// <summary>
    /// Exposes the Cmdlet Family Providers to the Cmdlet base class. The methods of this class
    /// use the providers to perform operations.
    /// </summary>
    public sealed class ProviderIntrinsics
    {
        #region Constructors

        /// <summary>
        /// Hide the default constructor since we always require an instance of SessionState.
        /// </summary>
        private ProviderIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        /// <summary>
        /// Constructs a facade over the "real" session state API.
        /// </summary>
        /// <param name="cmdlet">
        /// An instance of the cmdlet.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="cmdlet"/> is null.
        /// </exception>
        internal ProviderIntrinsics(Cmdlet cmdlet)
        {
            if (cmdlet == null)
            {
                throw PSTraceSource.NewArgumentNullException("cmdlet");
            }

            _cmdlet = cmdlet;
            Item = new ItemCmdletProviderIntrinsics(cmdlet);
            ChildItem = new ChildItemCmdletProviderIntrinsics(cmdlet);
            Content = new ContentCmdletProviderIntrinsics(cmdlet);
            Property = new PropertyCmdletProviderIntrinsics(cmdlet);
            SecurityDescriptor = new SecurityDescriptorCmdletProviderIntrinsics(cmdlet);
        }

        /// <summary>
        /// Constructs a facade over the "real" session state API.
        /// </summary>
        /// <param name="sessionState">
        /// An instance of the cmdlet.
        /// </param>
        internal ProviderIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException("sessionState");
            }

            Item = new ItemCmdletProviderIntrinsics(sessionState);
            ChildItem = new ChildItemCmdletProviderIntrinsics(sessionState);
            Content = new ContentCmdletProviderIntrinsics(sessionState);
            Property = new PropertyCmdletProviderIntrinsics(sessionState);
            SecurityDescriptor = new SecurityDescriptorCmdletProviderIntrinsics(sessionState);
        }

        #endregion Constructors

        #region Public members

        /// <summary>
        /// Gets the object that exposes the verbs for the item noun for Cmdlet Providers.
        /// </summary>
        public ItemCmdletProviderIntrinsics Item { get; }

        /// <summary>
        /// Gets the object that exposes the verbs for the childItem noun for Cmdlet Providers.
        /// </summary>
        public ChildItemCmdletProviderIntrinsics ChildItem { get; }

        /// <summary>
        /// Gets the object that exposes the verbs for the content noun for Cmdlet Providers.
        /// </summary>
        public ContentCmdletProviderIntrinsics Content { get; }

        /// <summary>
        /// Gets the object that exposes the verbs for the property noun for Cmdlet Providers.
        /// </summary>
        public PropertyCmdletProviderIntrinsics Property { get; }

        /// <summary>
        /// Gets the object that exposes the verbs for the SecurityDescriptor noun for Cmdlet Providers.
        /// </summary>
        public SecurityDescriptorCmdletProviderIntrinsics SecurityDescriptor { get; }

        #endregion Public members

        #region private data

        private InternalCommand _cmdlet;

        #endregion private data
    }
}

