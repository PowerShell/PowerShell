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
    /// A method declaration of <see cref="CimClass"/>
    /// </summary>
    public abstract class CimMethodDeclaration
    {
        internal CimMethodDeclaration()
        {
            // do not allow 3rd parties to derive from / instantiate this class
        }

        /// <summary>
        /// Name of the method
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Return type of the method
        /// </summary>
        public abstract CimType ReturnType { get; }

        /// <summary>
        /// Return type of the method
        /// </summary>
        public abstract CimReadOnlyKeyedCollection<CimMethodParameterDeclaration> Parameters { get; }

        /// <summary>
        /// Qualifiers of the Method.
        /// </summary>
        public abstract CimReadOnlyKeyedCollection<CimQualifier> Qualifiers { get; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
