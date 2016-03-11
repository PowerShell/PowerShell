/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections.Generic;
using Microsoft.Management.Infrastructure.Generic;
using Microsoft.Management.Infrastructure.Options.Internal;
using Microsoft.Management.Infrastructure.Internal.Data;

namespace Microsoft.Management.Infrastructure.Internal.Data
{
    internal class CimMethodParameterQualifierCollection : CimReadOnlyKeyedCollection<CimQualifier>
    {
        private readonly Native.ClassHandle classHandle;
        private readonly int methodIndex;
        private readonly int parameterName;

        internal CimMethodParameterQualifierCollection(Native.ClassHandle classHandle, int methodIndex, int parameterName)
        {
            this.classHandle = classHandle;
            this.methodIndex = methodIndex;
            this.parameterName = parameterName;
        }

        public override int Count
        {
            get
            {
                int count;
                Native.MiResult result = Native.ClassMethods.GetMethodParametersGetQualifiersCount(
                    this.classHandle,
                    this.methodIndex,
                    this.parameterName, 
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
                Native.MiResult result = Native.ClassMethods.GetMethodGetQualifierElement_GetIndex(this.classHandle, methodIndex, parameterName, qualifierName , out index);
                switch (result)
                {
                    case Native.MiResult.NO_SUCH_PROPERTY:
                    case Native.MiResult.NOT_FOUND:
                        return null;

                    default:
                        CimException.ThrowIfMiResultFailure(result);
                        return new CimQualifierOfMethodParameter(this.classHandle, methodIndex, parameterName, index);
                }
            }
        }

        public override IEnumerator<CimQualifier> GetEnumerator()
        {
            int count = this.Count;
            for (int i = 0; i < count; i++)
            {
                yield return new CimQualifierOfMethodParameter(this.classHandle, methodIndex, parameterName, i);
            }
        }
    }
}
