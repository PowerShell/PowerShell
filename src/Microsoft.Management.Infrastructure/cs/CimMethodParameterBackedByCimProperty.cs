/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;

namespace Microsoft.Management.Infrastructure.Internal.Data
{
    internal class CimMethodParameterBackedByCimProperty : CimMethodParameter
    {
        private readonly CimProperty _backingProperty;
        private string _cimSessionComputerName;
        private Guid _cimSessionInstanceId;

        internal CimMethodParameterBackedByCimProperty(CimProperty backingProperty)
        {
            Debug.Assert(backingProperty != null, "Caller should verify backingProperty != null");
            this._backingProperty = backingProperty;
            Initialize(null, Guid.Empty);
        }

        internal CimMethodParameterBackedByCimProperty(CimProperty backingProperty,
            string cimSessionComputerName,
            Guid cimSessionInstanceId)
        {
            Debug.Assert(backingProperty != null, "Caller should verify backingProperty != null");
            this._backingProperty = backingProperty;
            Initialize(cimSessionComputerName, cimSessionInstanceId);
        }

        /// <summary>
        /// Initialize members
        /// </summary>
        private void Initialize(string cimSessionComputerName,
            Guid cimSessionInstanceId)
        {
            this._cimSessionComputerName = cimSessionComputerName;
            this._cimSessionInstanceId = cimSessionInstanceId;
        }

        /// <summary>
        /// Set CimSessionComputerName and CimSessionInstanceId properties to
        /// the given object if it is CimInstance or CimInstance[]
        /// </summary>
        private void ProcessPropertyValue(object objectValue)
        {
            if ((this._cimSessionComputerName != null) || (!this._cimSessionInstanceId.Equals(Guid.Empty)))
            {
                CimInstance value = (objectValue as CimInstance);
                if (value != null)
                {
                    value.SetCimSessionComputerName(this._cimSessionComputerName);
                    value.SetCimSessionInstanceId(this._cimSessionInstanceId);
                    return;
                }

                CimInstance[] arrayValue = (objectValue as CimInstance[]);
                if (arrayValue != null)
                {
                    foreach (CimInstance cimInstance in arrayValue)
                    {
                        if (cimInstance != null)
                        {
                            cimInstance.SetCimSessionComputerName(this._cimSessionComputerName);
                            cimInstance.SetCimSessionInstanceId(this._cimSessionInstanceId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Name of the parameter
        /// </summary>
        public override string Name
        {
            get
            {
                return this._backingProperty.Name;
            }
        }

        /// <summary>
        /// <para>
        /// Value of the parameter.  <c>null</c> if the parameter doesn't have a value.
        /// </para>
        /// <para>
        /// See <see cref="CimType"/> for a description of mapping between CIM types and .NET types.
        /// </para>
        /// </summary>
        /// <exception cref="ArgumentException">Thrown by the property setter, when the value doesn't match <see cref="CimMethodParameter.CimType"/></exception>
        public override object Value
        {
            get
            {
                object obj = this._backingProperty.Value;
                ProcessPropertyValue(obj);
                return obj;
            }
            set
            {
                this._backingProperty.Value = value;
            }
        }

        /// <summary>
        /// CIM type of the parameter
        /// </summary>
        public override CimType CimType
        {
            get
            {
                return this._backingProperty.CimType;
            }
        }

        /// <summary>
        /// Flags of the parameter.
        /// </summary>
        public override CimFlags Flags
        {
            get
            {
                return this._backingProperty.Flags;
            }
        }

    }
}