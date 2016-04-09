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
    internal class CimMethodDeclarationCollection : CimReadOnlyKeyedCollection<CimMethodDeclaration>
    {
        private readonly Native.ClassHandle classHandle;

        internal CimMethodDeclarationCollection(Native.ClassHandle classHandle)
        {
            this.classHandle = classHandle;
        }

        public override int Count
        {
            get
            {
                int count;
                Native.MiResult result = Native.ClassMethods.GetMethodCount(
                    this.classHandle,
                    out count);
                CimException.ThrowIfMiResultFailure(result);
                return count;
            }
        }

        public override CimMethodDeclaration this[string methodName]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(methodName))
                {
                    throw new ArgumentNullException("methodName");
                }

                int index;
                Native.MiResult result = Native.ClassMethods.GetMethod_GetIndex(this.classHandle, methodName, out index);
                switch (result)
                {
                    case Native.MiResult.METHOD_NOT_FOUND:
                        return null;

                    default:
                        CimException.ThrowIfMiResultFailure(result);
                        return new CimMethodDeclarationOfClass(this.classHandle, index);
                }
            }
        }

        public override IEnumerator<CimMethodDeclaration> GetEnumerator()
        {
            int count = this.Count;
            for (int i = 0; i < count; i++)
            {
                yield return new CimMethodDeclarationOfClass(this.classHandle, i);
            }
        }
    }
}