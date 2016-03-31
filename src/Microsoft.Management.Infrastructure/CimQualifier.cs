/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Globalization;
using Microsoft.Management.Infrastructure.Internal;
using Microsoft.Management.Infrastructure.Internal.Data;

namespace Microsoft.Management.Infrastructure
{
    /// <summary>
    /// A qualifier of <see cref="CimClass"/>
    /// </summary>
    public abstract class CimQualifier
    {
        internal CimQualifier()
        {
            // do not allow 3rd parties to derive from / instantiate this class
        }

        /// <summary>
        /// Name of the qualifier
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// <para>
        /// Value of the qualifier.  <c>null</c> if the qualifier doesn't have a value.
        /// </para>
        /// <para>
        /// See <see cref="CimType"/> for a description of mapping between CIM types and .NET types.
        /// </para>
        /// </summary>
        public abstract object Value { get; }

        /// <summary>
        /// CIM type of the qualifier
        /// </summary>
        public abstract CimType CimType { get; }

        /// <summary>
        /// Flags of the qualifier.
        /// </summary>
        public abstract CimFlags Flags { get; }

        public override string ToString()
        {
            return Helpers.ToStringFromNameAndValue(this.Name, this.Value);
        }
    }
}
