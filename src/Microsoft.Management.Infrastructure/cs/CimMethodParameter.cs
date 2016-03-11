/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using Microsoft.Management.Infrastructure.Internal;
using Microsoft.Management.Infrastructure.Internal.Data;

namespace Microsoft.Management.Infrastructure
{
    /// <summary>
    /// Represents a parameter of a CIM method
    /// </summary>
    public abstract class CimMethodParameter
    {
        internal CimMethodParameter()
        {
            // do not allow 3rd parties to derive from / instantiate this class
        }

        /// <summary>
        /// Name of the parameter
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// <para>
        /// Value of the parameter.  <c>null</c> if the parameter doesn't have a value.
        /// </para>
        /// <para>
        /// See <see cref="CimType"/> for a description of mapping between CIM types and .NET types.
        /// </para>
        /// </summary>
        /// <exception cref="ArgumentException">Thrown by the property setter, when the value doesn't match <see cref="CimMethodParameter.CimType"/></exception>
        public abstract object Value { get; set;  }

        /// <summary>
        /// CIM type of the parameter
        /// </summary>
        public abstract CimType CimType { get; }

        /// <summary>
        /// Flags of the parameter.
        /// </summary>
        public abstract CimFlags Flags { get; }

        /// <summary>
        /// Creates a new parameter. 
        /// This method overload tries to infer <see cref="CimType"/> from the property <paramref name="value"/>
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="value">Value of the parameter.  <c>null</c> is the parameter doesn't have an associated value.</param>
        /// <param name="flags"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown when the <see cref="CimType"/> cannot be inferred from the property <paramref name="value"/> </exception>
        static public CimMethodParameter Create(string name, object value, CimFlags flags)
        {
            CimType cimType = CimConverter.GetCimTypeFromDotNetValueOrThrowAnException(value);
            return Create(name, value, cimType, flags);
        }

        /// <summary>
        /// Creates a new parameter. 
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="value">Value of the parameter.  <c>null</c> is the parameter doesn't have an associated value.</param>
        /// <param name="type"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> doesn't match <paramref name="type"/></exception>
        static public CimMethodParameter Create(string name, object value, CimType type, CimFlags flags)
        {
            CimProperty backingProperty = new CimPropertyStandalone(name, value, type, flags);
            return new CimMethodParameterBackedByCimProperty(backingProperty);
        }

        public override string ToString()
        {
            return Helpers.ToStringFromNameAndValue(this.Name, this.Value);
        }
    }
}
