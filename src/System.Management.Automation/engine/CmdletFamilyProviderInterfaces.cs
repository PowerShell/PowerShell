/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

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
        /// Hide the default constructor since we always require an instance of SessionState
        /// </summary>
        private ProviderIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        } // ProviderIntrinsics private


        /// <summary>
        /// Constructs a facade over the "real" session state API
        /// </summary>
        ///
        /// <param name="cmdlet">
        /// An instance of the cmdlet.
        /// </param>
        ///
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="cmdlet"/> is null.
        /// </exception>
        /// 
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
#if SUPPORTS_IMULTIVALUEPROPERTYCMDLETPROVIDER
            this.propertyValue = new PropertyValueCmdletProviderIntrinsics(cmdlet);
#endif
            SecurityDescriptor = new SecurityDescriptorCmdletProviderIntrinsics(cmdlet);

#if RELATIONSHIP_SUPPORTED
    // 2004/11/24-JeffJon - Relationships have been removed from the Exchange release
            this.relationship = new RelationshipProviderIntrinsics(cmdlet.Context.EngineSessionState);
#endif
        } // ProviderIntrinsics internal

        /// <summary>
        /// Constructs a facade over the "real" session state API
        /// </summary>
        ///
        /// <param name="sessionState">
        /// An instance of the cmdlet.
        /// </param>
        ///
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
#if SUPPORTS_IMULTIVALUEPROPERTYCMDLETPROVIDER
            this.propertyValue = new PropertyValueCmdletProviderIntrinsics(sessionState);
#endif
            SecurityDescriptor = new SecurityDescriptorCmdletProviderIntrinsics(sessionState);

#if RELATIONSHIP_SUPPORTED
    // 2004/11/24-JeffJon - Relationships have been removed from the Exchange release
            this.relationship = new RelationshipProviderIntrinsics(sessionState);
#endif
        } // ProviderIntrinsics internal

        #endregion Constructors

        #region Public members

        /// <summary>
        /// Gets the object that exposes the verbs for the item noun for Cmdlet Providers
        /// </summary>
        public ItemCmdletProviderIntrinsics Item { get; }

        /// <summary>
        /// Gets the object that exposes the verbs for the childItem noun for Cmdlet Providers
        /// </summary>
        public ChildItemCmdletProviderIntrinsics ChildItem { get; }

        /// <summary>
        /// Gets the object that exposes the verbs for the content noun for Cmdlet Providers
        /// </summary>
        public ContentCmdletProviderIntrinsics Content { get; }

        /// <summary>
        /// Gets the object that exposes the verbs for the property noun for Cmdlet Providers
        /// </summary>
        public PropertyCmdletProviderIntrinsics Property { get; }

#if SUPPORTS_IMULTIVALUEPROPERTYCMDLETPROVIDER
        /// <summary>
        /// The object that exposes the verbs for the item propertyvalue for Cmdlet Providers
        /// </summary>
        /// 
        public PropertyValueCmdletProviderIntrinsics PropertyValue
        {
            get { return propertyValue; }
        }
#endif

        /// <summary>
        /// Gets the object that exposes the verbs for the SecurityDescriptor noun for Cmdlet Providers
        /// </summary>
        public SecurityDescriptorCmdletProviderIntrinsics SecurityDescriptor { get; }

#if RELATIONSHIP_SUPPORTED
        // 2004/11/24-JeffJon - Relationships have been removed from the Exchange release

        /// <summary>
        /// The object that exposes the verbs for the relationship providers
        /// </summary>
        /// 
        public RelationshipProviderIntrinsics Relationship
        {
            get { return relationship; }
        }
#endif
        #endregion Public members

        #region private data

        private InternalCommand _cmdlet;
#if SUPPORTS_IMULTIVALUEPROPERTYCMDLETPROVIDER
        private PropertyValueCmdletProviderIntrinsics propertyValue;
#endif

#if RELATIONSHIP_SUPPORTED
        // 2004/11/24-JeffJon - Relationships have been removed from the Exchange release
        private RelationshipProviderIntrinsics relationship = null;
#endif
        #endregion private data
    } // ProviderIntrinsics
}

