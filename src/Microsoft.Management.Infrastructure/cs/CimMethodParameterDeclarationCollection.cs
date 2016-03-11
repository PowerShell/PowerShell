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
    internal class CimMethodParameterDeclarationCollection : CimReadOnlyKeyedCollection<CimMethodParameterDeclaration>
    {
        private readonly Native.ClassHandle classHandle;
        private readonly int methodIndex;

        internal CimMethodParameterDeclarationCollection(Native.ClassHandle classHandle, int index)
        {
            this.classHandle = classHandle;
            this.methodIndex = index;
        }

        public override int Count
        {
            get
            {
                int count;
                Native.MiResult result = Native.ClassMethods.GetMethodParametersCount(
                    this.classHandle,
                    this.methodIndex,
                    out count);
                CimException.ThrowIfMiResultFailure(result);
                return count;
            }
        }

        public override CimMethodParameterDeclaration this[string parameterName]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(parameterName))
                {
                    throw new ArgumentNullException("parameterName");
                }

                int index;
                Native.MiResult result = Native.ClassMethods.GetMethodElement_GetIndex(this.classHandle, methodIndex, parameterName, out index);
                switch (result)
                {
                    case Native.MiResult.NOT_FOUND:
                        return null;

                    default:
                        CimException.ThrowIfMiResultFailure(result);
                        return new CimMethodParameterDeclarationOfMethod(this.classHandle, methodIndex, index);
                }
            }
        }

        public override IEnumerator<CimMethodParameterDeclaration> GetEnumerator()
        {
            int count = this.Count;
            for (int i = 0; i < count; i++)
            {
                yield return new CimMethodParameterDeclarationOfMethod(this.classHandle, methodIndex, i);
            }
        }
    }
}