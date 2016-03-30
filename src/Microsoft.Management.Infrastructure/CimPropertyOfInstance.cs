/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections;
using Microsoft.Management.Infrastructure.Options.Internal;

namespace Microsoft.Management.Infrastructure.Internal.Data
{
    internal sealed class CimPropertyOfInstance : CimProperty
    {
        private readonly SharedInstanceHandle _instanceHandle;
        private readonly CimInstance _instance;
        private readonly int _index;

        internal CimPropertyOfInstance(SharedInstanceHandle instanceHandle, CimInstance instance, int index)
        {
            this._instanceHandle = instanceHandle;
            this._instance = instance;
            this._index = index;
        }

        public override string Name
        {
            get
            {
                string name;
                Native.MiResult result = Native.InstanceMethods.GetElementAt_GetName(
                    this._instanceHandle.Handle,
                    this._index,
                    out name);
                CimException.ThrowIfMiResultFailure(result);
                return name;
            }
        }

        public override object Value
        {
            get
            {
                object value;
                try
                {
                    this._instanceHandle.AddRef();

                    Native.MiResult result = Native.InstanceMethods.GetElementAt_GetValue(
                        this._instanceHandle.Handle,
                        this._index,
                        out value);
                    CimException.ThrowIfMiResultFailure(result);
                    return CimInstance.ConvertFromNativeLayer(
                        value: value,
                        sharedParentHandle: this._instanceHandle,
                        parent: this._instance,
                        clone: false);
                }
                finally
                {
                    this._instanceHandle.Release();
                }
            }
            set
            {
                Native.MiResult result;
                if (value == null)
                {
                    result = Native.InstanceMethods.ClearElementAt(this._instanceHandle.Handle, this._index);
                }
                else
                {
                    try
                    {
                        Helpers.ValidateNoNullElements(value as IList);
                        result = Native.InstanceMethods.SetElementAt_SetValue(
                            this._instanceHandle.Handle, 
                            this._index, 
                            CimInstance.ConvertToNativeLayer(value, this.CimType));
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
                CimException.ThrowIfMiResultFailure(result);
            }
        }

        public override CimType CimType
        {
            get
            {
                Native.MiType type;
                Native.MiResult result = Native.InstanceMethods.GetElementAt_GetType(
                    this._instanceHandle.Handle,
                    this._index,
                    out type);
                CimException.ThrowIfMiResultFailure(result);
                return type.ToCimType();
            }
        }

        public override CimFlags Flags
        {
            get
            {
                Native.MiFlags flags;
                Native.MiResult result = Native.InstanceMethods.GetElementAt_GetFlags(
                    this._instanceHandle.Handle,
                    this._index,
                    out flags);
                CimException.ThrowIfMiResultFailure(result);
                return flags.ToCimFlags();
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
                Native.MiResult result = Native.InstanceMethods.SetElementAt_SetNotModifiedFlag(
                    this._instanceHandle.Handle,
                    this._index,
                    notModifiedFlag);
                CimException.ThrowIfMiResultFailure(result);
            }
        }
    }
}