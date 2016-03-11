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
    internal class CimPropertyQualifierCollection : CimReadOnlyKeyedCollection<CimQualifier>
    {
        private readonly Native.ClassHandle classHandle;
        private readonly string name;

        internal CimPropertyQualifierCollection(Native.ClassHandle classHandle, string name)
        {
            this.classHandle = classHandle;
            this.name = name;
        }

        public override int Count
        {
            get
            {
                int count;
                Native.MiResult result = Native.ClassMethods.GetPropertyQualifier_Count(
                    this.classHandle,
                    name,
                    out count);
                CimException.ThrowIfMiResultFailure(result);
                return count;
            }
        }

        public override CimQualifier this[string qualifierName]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(qualifierName))
                {
                    throw new ArgumentNullException("qualifierName");
                }

                int index;
                Native.MiResult result = Native.ClassMethods.GetPropertyQualifier_Index(this.classHandle, name, qualifierName, out index);
                switch (result)
                {
                    case Native.MiResult.NO_SUCH_PROPERTY:
                    case Native.MiResult.NOT_FOUND:
                        return null;

                    default:
                        CimException.ThrowIfMiResultFailure(result);
                        return new CimQualifierOfProperty(this.classHandle, name, index);
                }
            }
        }

        public override IEnumerator<CimQualifier> GetEnumerator()
        {
            int count = this.Count;
            for (int i = 0; i < count; i++)
            {
                yield return new CimQualifierOfProperty(this.classHandle, name, i);
            }
        }
    }
}
