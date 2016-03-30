/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections.Generic;
using Microsoft.Management.Infrastructure.Generic;
using Microsoft.Management.Infrastructure.Options.Internal;

namespace Microsoft.Management.Infrastructure.Internal.Data
{
    internal class CimPropertiesCollection : CimKeyedCollection<CimProperty>
    {
        private readonly SharedInstanceHandle _instanceHandle;
        private readonly CimInstance _instance;

        internal CimPropertiesCollection(SharedInstanceHandle instanceHandle, CimInstance instance)
        {
            this._instanceHandle = instanceHandle;
            this._instance = instance;
        }

        public override void Add(CimProperty newProperty)
        {
            if (newProperty == null)
            {
                throw new ArgumentNullException("newProperty");
            }

            Native.MiResult result = Native.InstanceMethods.AddElement(
                this._instanceHandle.Handle,
                newProperty.Name,
                CimInstance.ConvertToNativeLayer(newProperty.Value),
                newProperty.CimType.ToMiType(),
                newProperty.Flags.ToMiFlags());
            CimException.ThrowIfMiResultFailure(result);
        }

        public override int Count
        {
            get
            {
                int count;
                Native.MiResult result = Native.InstanceMethods.GetElementCount(
                    this._instanceHandle.Handle,
                    out count);
                CimException.ThrowIfMiResultFailure(result);
                return count;
            }
        }

        public override CimProperty this[string propertyName]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    throw new ArgumentNullException("propertyName");
                }

                int index;
                Native.MiResult result = Native.InstanceMethods.GetElement_GetIndex(this._instanceHandle.Handle, propertyName, out index);
                switch (result)
                {
                    case Native.MiResult.NO_SUCH_PROPERTY:
                        return null;

                    default:
                        CimException.ThrowIfMiResultFailure(result);
                        return new CimPropertyOfInstance(this._instanceHandle, this._instance, index);
                }
            }
        }

        public override IEnumerator<CimProperty> GetEnumerator()
        {
            int count = this.Count;
            for (int i = 0; i < count; i++)
            {
                yield return new CimPropertyOfInstance(this._instanceHandle, this._instance, i);
            }
        }
    }
}