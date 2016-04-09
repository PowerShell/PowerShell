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
    internal class CimClassQualifierCollection: CimReadOnlyKeyedCollection<CimQualifier>
    {
        private readonly Native.ClassHandle classHandle;

        internal CimClassQualifierCollection(Native.ClassHandle classHandle)
        {
            this.classHandle = classHandle;
        }

        public override int Count
        {
            get
            {
                int count;
                Native.MiResult result = Native.ClassMethods.GetQualifier_Count(
                    this.classHandle,
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
                Native.MiResult result = Native.ClassMethods.GetClassQualifier_Index(this.classHandle, qualifierName, out index);
                switch (result)
                {
                    case Native.MiResult.NOT_FOUND:
                        return null;

                    default:
                        CimException.ThrowIfMiResultFailure(result);
                        return new CimQualifierOfClass(this.classHandle, index);
                }
            }
        }

        public override IEnumerator<CimQualifier> GetEnumerator()
        {
            int count = this.Count;
            for (int i = 0; i < count; i++)
            {
                yield return new CimQualifierOfClass(this.classHandle, i);
            }
        }
    }
}