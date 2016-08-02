/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using Microsoft.Management.Infrastructure;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// A delegate for serializing known type
    /// </summary>
    internal delegate CimInstance MITypeSerializerDelegate(
        string property, object source, MITypeSerializationInfo entry);

    /// <summary>
    /// This class contains serialization information about a type.
    /// </summary>
    internal class MITypeSerializationInfo
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type for which this entry is created</param>
        /// <param name="serializer">TypeSerializerDelegate for serializing the type</param>
        /// <param name="cimClassName">The CimClass name whose instance needs to be created for this type</param>
        internal MITypeSerializationInfo(Type type, MITypeSerializerDelegate serializer,
                                         string cimClassName)
        {
            Type = type;
            Serializer = serializer;
            CimType = CimConverter.GetCimType(type);
            CimClassName = cimClassName;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type for which this entry is created</param>
        /// <param name="serializer">TypeSerializerDelegate for serializing the type</param>
        /// <param name="cimType">CimType corresponding to the .NET type </param>
        /// For some .NET types (Guid, Decimal, etc.), there are no equivalent Cim Types. So, we serialize them as string
        /// <param name="cimClassName">The CimClass name whose instance needs to be created for this type</param>
        internal MITypeSerializationInfo(Type type, MITypeSerializerDelegate serializer,
                                         Microsoft.Management.Infrastructure.CimType cimType, string cimClassName) : this(type, serializer, cimClassName)
        {
            CimType = cimType;
        }

        #region properties

        /// <summary>
        /// Get the type for which this TypeSerializationInfo is created.
        /// </summary>
        internal Type Type { get; }

        /// <summary>
        /// Gets the delegate to serialize this type
        /// </summary>
        internal MITypeSerializerDelegate Serializer { get; }

        /// <summary>
        /// The CimType corresponding to the .NET type 
        /// </summary>
        internal Microsoft.Management.Infrastructure.CimType CimType { get; }

        /// <summary>
        /// The CimClass name whose instance needs to be created for this type
        /// </summary>
        internal String CimClassName { get; }

        #endregion properties

        #region private

        #endregion private
    }
}