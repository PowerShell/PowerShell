/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections;
using System.Diagnostics;

namespace Microsoft.Management.Infrastructure.Internal.Data
{
    internal sealed class CimPropertyStandalone : CimProperty
    {
        internal CimPropertyStandalone(string name, object value, CimType cimType, CimFlags flags)
        {
            Debug.Assert(name != null, "Caller should verify name != null");

            this._name = name;
            this._cimType = cimType;
            this._flags = flags;

            this.Value = value;
        }

        private readonly string _name;
        public override string Name
        {
            get
            {
                return this._name;
            }
        }

        private object _value;
        public override object Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value != null)
                {
                    try
                    {
                        Helpers.ValidateNoNullElements(value as IList);
                        Native.InstanceMethods.ThrowIfMismatchedType(this.CimType.ToMiType(), CimInstance.ConvertToNativeLayer(value, this.CimType));
                    }
                    catch (InvalidCastException e)
                    {
                        throw new ArgumentException(e.Message, "value", e);
                    }
                    catch (FormatException e)
                    {
                        throw new ArgumentException(e.Message, "value", e);
                    }
                    catch (ArgumentException e)
                    {
                        throw new ArgumentException(e.Message, "value", e);
                    }
                }

                this._value = value;
                this.IsValueModified = true;
            }
        }

        private readonly CimType _cimType;
        public override CimType CimType
        {
            get
            {
                return this._cimType;
            }
        }

        private CimFlags _flags;
        public override CimFlags Flags
        {
            get
            {
                return this._flags;
            }
        }

        public override bool IsValueModified
        {
            get
            {
                return base.IsValueModified;
            }
            set
            {
                bool notModifiedFlag = !value;
                if (notModifiedFlag)
                {
                    this._flags = this._flags | CimFlags.NotModified;
                }
                else
                {
                    this._flags = this._flags & (~CimFlags.NotModified);
                }
            }
        }
    }
}