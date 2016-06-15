/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

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
    /// A class for identifying types which are treated as Known Cim Type by Monad.
    /// A KnownMIType is guranteed to be available on machine on which monad is 
    /// running.
    /// </summary>
    internal static class KnownMITypes
    {
        /// <summary>
        /// Static constructor 
        /// </summary>
        static KnownMITypes()
        {
            for (int i = 0; i < _TypeSerializationInfo.Length; i++)
            {
                _knownTableKeyType.Add(_TypeSerializationInfo[i].Type.FullName, _TypeSerializationInfo[i]);
            }
        }

        /// <summary>
        /// Gets the type serialization information about a type
        /// </summary>
        /// <param name="type">Type for which information is retrieved</param>
        /// <returns>TypeSerializationInfo for the type, null if it doesn't exist</returns>
        internal static MITypeSerializationInfo GetTypeSerializationInfo(Type type)
        {
            MITypeSerializationInfo temp = null;
            _knownTableKeyType.TryGetValue(type.FullName, out temp);
            return temp;
        }

        #region private_fields

        /// <summary>
        /// Array of known types.
        /// </summary>
        private static readonly MITypeSerializationInfo[] _TypeSerializationInfo = new MITypeSerializationInfo[]
		{
			new MITypeSerializationInfo(typeof(Boolean),
								InternalMISerializer.CreateCimInstanceForPrimitiveType,
                                "PS_ObjectProperty_boolean"),

            new MITypeSerializationInfo(typeof(Byte),
                                InternalMISerializer.CreateCimInstanceForPrimitiveType,
                                "PS_ObjectProperty_uint8"),

            new MITypeSerializationInfo(typeof(Char),
								InternalMISerializer.CreateCimInstanceForPrimitiveType,
                                "PS_ObjectProperty_char16"),

            new MITypeSerializationInfo(typeof(Double),
                                InternalMISerializer.CreateCimInstanceForPrimitiveType,
                                "PS_ObjectProperty_real64"),

            new MITypeSerializationInfo(typeof(Guid),
                                null,
                                Microsoft.Management.Infrastructure.CimType.String,
                                "PS_ObjectProperty_string"),

            new MITypeSerializationInfo(typeof(Int16),
                                InternalMISerializer.CreateCimInstanceForPrimitiveType,
                                "PS_ObjectProperty_sint16"),

            new MITypeSerializationInfo(typeof(Int32),
                                InternalMISerializer.CreateCimInstanceForPrimitiveType,
                                "PS_ObjectProperty_sint32"),

            new MITypeSerializationInfo(typeof(Int64),
                                InternalMISerializer.CreateCimInstanceForPrimitiveType,
                                "PS_ObjectProperty_sint64"),

            new MITypeSerializationInfo(typeof(SByte),
                                InternalMISerializer.CreateCimInstanceForPrimitiveType,
                                "PS_ObjectProperty_sint8"),

            new MITypeSerializationInfo(typeof(String),
                                InternalMISerializer.CreateCimInstanceForString,
                                "PS_ObjectProperty_string"),

            new MITypeSerializationInfo(typeof(UInt16),
                                InternalMISerializer.CreateCimInstanceForPrimitiveType,
                                "PS_ObjectProperty_uint16"),

            new MITypeSerializationInfo(typeof(UInt32),
                                InternalMISerializer.CreateCimInstanceForPrimitiveType,
                                "PS_ObjectProperty_uint32"),

            new MITypeSerializationInfo(typeof(UInt64),
                                InternalMISerializer.CreateCimInstanceForPrimitiveType,
                                "PS_ObjectProperty_uint64"),

            new MITypeSerializationInfo(typeof(Decimal),
                                null,
                                Microsoft.Management.Infrastructure.CimType.String,
                                "PS_ObjectProperty_string"),

            //new MITypeSerializationInfo(typeof(DateTime),
            //                    InternalSerializer.WriteDateTime),

            //new MITypeSerializationInfo(typeof(Single),
            //                    InternalSerializer.WriteSingle),

            //new MITypeSerializationInfo(typeof(ScriptBlock),
            //                    InternalSerializer.WriteScriptBlock),

            //new MITypeSerializationInfo(typeof(TimeSpan),
            //                    InternalSerializer.WriteTimeSpan),

            //new MITypeSerializationInfo(typeof(Uri),
            //                    InternalSerializer.WriteUri),
                                
            //new MITypeSerializationInfo(typeof(byte[]),
            //                          InternalSerializer.WriteByteArray),

            //new MITypeSerializationInfo(typeof(System.Version),
            //                          InternalMISerializer.WriteVersion),
            //_xdInfo,

            //new MITypeSerializationInfo(typeof(ProgressRecord),
            //                          InternalSerializer.WriteProgressRecord),

            //new MITypeSerializationInfo(typeof(SecureString),
            //                          InternalSerializer.WriteSecureString),

		};

        /// <summary>
        /// Dictionary of knowntypes. 
        /// Key is Type.FullName and value is Type object.
        /// </summary>
        private static readonly Dictionary<string, MITypeSerializationInfo> _knownTableKeyType = new Dictionary<string, MITypeSerializationInfo>();

        #endregion private_fields
    }
}