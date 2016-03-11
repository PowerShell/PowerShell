/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using Microsoft.Management.Infrastructure.Internal;
using Microsoft.Management.Infrastructure.Internal.Data;
using Microsoft.Management.Infrastructure.Generic;

namespace Microsoft.Management.Infrastructure
{
    /// <summary>
    /// A property declaration of <see cref="CimClass"/>
    /// </summary>
    public abstract class CimPropertyDeclaration
    {
        internal CimPropertyDeclaration()
        {
            // do not allow 3rd parties to derive from / instantiate this class
        }

        /// <summary>
        /// Name of the property
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// <para>
        /// Default value of the property.  <c>null</c> if the property doesn't have a value.
        /// </para>
        /// </summary>
        public abstract object Value { get; }

        /// <summary>
        /// <para>CIM type of the property</para>
        /// <para>
        /// See <see cref="CimType"/> for a description of mapping between CIM types and .NET types.
        /// </para>
        /// </summary>
        public abstract CimType CimType { get; }

        /// <summary>
        /// Flags of the property.
        /// </summary>
        public abstract CimFlags Flags { get; }

        /// <summary>
        /// Qualifiers of the property.
        /// </summary>
        public abstract CimReadOnlyKeyedCollection<CimQualifier> Qualifiers { get; }

        /// <summary>
        /// ClassName of the property's value.
        /// Only valid if the property type is <see cref="CimType.Reference"/> or
        /// <see cref="CimType.ReferenceArray"/>;
        /// </summary>
        public abstract string ReferenceClassName { get; }

        public override string ToString()
        {
            return Helpers.ToStringFromNameAndValue(this.Name, this.Value);
        }
    }
}
