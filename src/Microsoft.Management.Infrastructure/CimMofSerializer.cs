/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Management.Infrastructure.Internal;

namespace Microsoft.Management.Infrastructure.Serialization
{    
    /// <summary>
    ///  Class use to craete a mof serializer
    /// </summary>
    internal static class CimMofSerializer
    {
        #region Constructors

        private static CimSerializer CreateCimMofSerializer(string format, uint flags)
        {
            Debug.Assert(!string.IsNullOrEmpty(format), "Caller should verify that format != null");

            Native.SerializerHandle tmpHandle;
            Native.MiResult result = Native.ApplicationMethodsInternal.NewSerializerMOF(
                CimApplication.Handle,
                format,
                flags,
                out tmpHandle);
            if (result == Native.MiResult.INVALID_PARAMETER)
            {
                throw new ArgumentOutOfRangeException("format");
            }
            return new CimSerializer(tmpHandle);
        }

        /// <summary>
        /// Instantiates a default serializer
        /// </summary>
        public static CimSerializer Create()
        {
            return CreateCimMofSerializer(format: "MI_MOF_CIMV2_EXTV1", flags: 0);
        }
        #endregion
    }
}