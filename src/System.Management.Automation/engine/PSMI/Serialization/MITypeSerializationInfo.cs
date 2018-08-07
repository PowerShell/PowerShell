// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Xml;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Serialization;
using Microsoft.PowerShell;
using Dbg = System.Management.Automation.Diagnostics;
using System.Management.Automation.Remoting;

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
            _type = type;
            _serializer = serializer;
            _cimType = CimConverter.GetCimType(type);
            _cimClassName = cimClassName;
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
                                         Microsoft.Management.Infrastructure.CimType cimType, string cimClassName):this(type, serializer, cimClassName)
        {
            _cimType = cimType;
        }

        #region properties

        /// <summary>
        /// Get the type for which this TypeSerializationInfo is created.
        /// </summary>
        internal Type Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Gets the delegate to serialize this type
        /// </summary>
        internal MITypeSerializerDelegate Serializer
        {
            get { return _serializer; }
        }

        /// <summary>
        /// The CimType corresponding to the .NET type 
        /// </summary>
        internal Microsoft.Management.Infrastructure.CimType CimType
        {
            get { return _cimType; }
        }

        /// <summary>
        /// The CimClass name whose instance needs to be created for this type
        /// </summary>
        internal String CimClassName
        {
            get { return _cimClassName; }
        }

        #endregion properties

        #region private

        /// <summary>
        /// Type for which this entry is created
        /// </summary>
        private readonly Type _type;

        /// <summary>
        /// TypeSerializerDelegate for serializing the type
        /// </summary>
        private readonly MITypeSerializerDelegate _serializer;

        /// <summary>
        /// The CimType corresponding to the .NET type 
        /// </summary>
        private readonly Microsoft.Management.Infrastructure.CimType _cimType;

        /// <summary>
        /// The CimClass name whose instance needs to be created for this type
        /// </summary>
        private string _cimClassName;

        #endregion private
    }
}
