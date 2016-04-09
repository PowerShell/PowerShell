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
    internal class CimClassPropertiesCollection : CimReadOnlyKeyedCollection<CimPropertyDeclaration>
    {
        private readonly Native.ClassHandle classHandle;

        internal CimClassPropertiesCollection(Native.ClassHandle classHandle)
        {
            this.classHandle = classHandle;
        }

        public override int Count
        {
            get
            {
                int count;
                Native.MiResult result = Native.ClassMethods.GetElementCount(
                    this.classHandle,
                    out count);
                CimException.ThrowIfMiResultFailure(result);
                return count;
            }
        }

        public override CimPropertyDeclaration this[string propertyName]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    throw new ArgumentNullException("propertyName");
                }

                int index;
                Native.MiResult result = Native.ClassMethods.GetElement_GetIndex(this.classHandle, propertyName, out index);
                switch (result)
                {
                    case Native.MiResult.NO_SUCH_PROPERTY:
                        return null;

                    default:
                        CimException.ThrowIfMiResultFailure(result);
                        return new CimClassPropertyOfClass(this.classHandle, index);
                }
            }
        }

        public override IEnumerator<CimPropertyDeclaration> GetEnumerator()
        {
            int count = this.Count;
            for (int i = 0; i < count; i++)
            {
                yield return new CimClassPropertyOfClass(this.classHandle, i);
            }
        }
    }
}