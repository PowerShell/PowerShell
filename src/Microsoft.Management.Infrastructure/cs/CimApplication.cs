/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;

namespace Microsoft.Management.Infrastructure.Internal
{
    internal static class CimApplication
    {
        #region Initializing CimApplication singleton

        static private Native.ApplicationHandle GetApplicationHandle()
        {
            Native.ApplicationHandle applicationHandle;
            Native.InstanceHandle errorDetailsHandle;
            Native.MiResult result = Native.ApplicationMethods.Initialize(
                out errorDetailsHandle,
                out applicationHandle);
            CimException.ThrowIfMiResultFailure(result, errorDetailsHandle);

            return applicationHandle;
        }

        static public Native.ApplicationHandle Handle
        {
            get
            {
                return CimApplication.LazyHandle.Value;
            }
        }

        private static readonly Lazy<Native.ApplicationHandle> LazyHandle = new Lazy<Native.ApplicationHandle>(CimApplication.GetApplicationHandle);

        #endregion
    }
}