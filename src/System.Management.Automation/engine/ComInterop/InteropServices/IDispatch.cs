// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.InteropServices
{
    [ComImport]
    [Guid("00020400-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDispatch
    {
        int GetTypeInfoCount();

        ComTypes.ITypeInfo GetTypeInfo(
            int iTInfo,
            int lcid);

        void GetIDsOfNames(
            ref Guid riid,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2), In]
            string[] rgszNames,
            int cNames,
            int lcid,
            [Out] int[] rgDispId);

        // The last 3 parameters of Invoke() are optional and must be defined
        // as IntPtr in C#, since there is no language feature for optional ref/out.
        void Invoke(
            int dispIdMember,
            ref Guid riid,
            int lcid,
            InvokeFlags wFlags,
            ref ComTypes.DISPPARAMS pDispParams,
            /* out/optional */ IntPtr pVarResult,
            /* out/optional */ IntPtr pExcepInfo,
            /* out/optional */ IntPtr puArgErr);
    }

    [Flags]
    internal enum InvokeFlags : short
    {
        DISPATCH_METHOD = 1,
        DISPATCH_PROPERTYGET = 2,
        DISPATCH_PROPERTYPUT = 4,
        DISPATCH_PROPERTYPUTREF = 8
    }
}
