/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections;
using Microsoft.Management.Infrastructure.Options.Internal;
using Microsoft.Management.Infrastructure.Generic;

namespace Microsoft.Management.Infrastructure.Internal.Data
{
    internal sealed class CimMethodDeclarationOfClass : CimMethodDeclaration
    {
        private readonly Native.ClassHandle classHandle;
        private readonly int index;

        internal CimMethodDeclarationOfClass(Native.ClassHandle classHandle, int index)
        {
            this.classHandle = classHandle;
            this.index = index;
        }

        public override string Name
        {
            get
            {
                string name;
                Native.MiResult result = Native.ClassMethods.GetMethodElementAt_GetName(
                    this.classHandle,
                    this.index,
                    out name);
                CimException.ThrowIfMiResultFailure(result);
                return name;
            }
        }


        public override CimType ReturnType
        {
            get
            {
                Native.MiType type;
                Native.MiResult result = Native.ClassMethods.GetMethodElementAt_GetType(
                    this.classHandle,
                    this.index,
                    out type);
                CimException.ThrowIfMiResultFailure(result);
                return type.ToCimType();
            }
        }

        public override CimReadOnlyKeyedCollection<CimMethodParameterDeclaration> Parameters
        {
            get
            {
                return new CimMethodParameterDeclarationCollection(this.classHandle, index);
            }
        }
        public override CimReadOnlyKeyedCollection<CimQualifier> Qualifiers
        {
            get
            {
                return new CimMethodQualifierCollection(classHandle, this.index);
            }
        }
    }
}