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
    internal class CimMethodQualifierCollection : CimReadOnlyKeyedCollection<CimQualifier>
    {
        private readonly Native.ClassHandle classHandle;
        private readonly int methodIndex;

        internal CimMethodQualifierCollection(Native.ClassHandle classHandle, int index)
        {
            this.classHandle = classHandle;
            this.methodIndex = index;
        }

        public override int Count
        {
            get
            {
                int count;
                Native.MiResult result = Native.ClassMethods.GetMethodQualifierCount(
                    this.classHandle,
                    this.methodIndex,
                    out count);
                CimException.ThrowIfMiResultFailure(result);
                return count;
            }
        }

        public override CimQualifier this[string methodName]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(methodName))
                {
                    throw new ArgumentNullException("methodName");
                }

                int index;
                Native.MiResult result = Native.ClassMethods.GetMethodQualifierElement_GetIndex(this.classHandle, methodIndex, methodName, out index);
                switch (result)
                {
                    case Native.MiResult.NOT_FOUND:
                        return null;

                    default:
                        CimException.ThrowIfMiResultFailure(result);
                        return new CimMethodQualifierDeclarationOfMethod(this.classHandle, methodIndex, index);
                }
            }
        }

        public override IEnumerator<CimQualifier> GetEnumerator()
        {
            int count = this.Count;
            for (int i = 0; i < count; i++)
            {
                yield return new CimMethodQualifierDeclarationOfMethod(this.classHandle, methodIndex, i);
            }
        }
    }
}