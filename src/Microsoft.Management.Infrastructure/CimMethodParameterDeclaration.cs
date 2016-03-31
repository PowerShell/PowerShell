/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using Microsoft.Management.Infrastructure.Internal.Data;
using Microsoft.Management.Infrastructure.Generic;

namespace Microsoft.Management.Infrastructure
{
    /// <summary>
    /// A parameter declaration of <see cref="CimMethodDeclaration"/>
    /// </summary>
    public abstract class CimMethodParameterDeclaration
    {
        internal CimMethodParameterDeclaration()
        {
            // do not allow 3rd parties to derive from / instantiate this class
        }

        /// <summary>
        /// Name of the parameter
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// CIM type of the parameter
        /// </summary>
        public abstract CimType CimType { get; }

        /// <summary>
        /// Qualifiers of the parameter.
        /// </summary>
        public abstract CimReadOnlyKeyedCollection<CimQualifier> Qualifiers { get; }

        /// <summary>
        /// ClassName of the parameter's value.
        /// Only valid if the parameter type is <see cref="CimType.Reference"/> or
        /// <see cref="CimType.ReferenceArray"/>
        /// </summary>
        public abstract string ReferenceClassName { get; }
    }
}
