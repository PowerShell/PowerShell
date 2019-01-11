// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject

using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.ComInterop
{
    [
    ComImport,
    InterfaceType(ComInterfaceType.InterfaceIsIDispatch),
    Guid("00020400-0000-0000-C000-000000000046")
    ]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
    internal interface IDispatchForReflection
    {
    }

    [
    ComImport,
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    Guid("00020400-0000-0000-C000-000000000046"),
    ]
    internal interface IDispatch
    {
        [PreserveSig]
        int TryGetTypeInfoCount(out uint pctinfo);

        [PreserveSig]
        int TryGetTypeInfo(uint iTInfo, int lcid, out IntPtr info);

        [PreserveSig]
        int TryGetIDsOfNames(
            ref Guid iid,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)]
            string[] names,
            uint cNames,
            int lcid,
            [Out]
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I4, SizeParamIndex = 2)]
            int[] rgDispId);

        [PreserveSig]
        int TryInvoke(
            int dispIdMember,
            ref Guid riid,
            int lcid,
            ComTypes.INVOKEKIND wFlags,
            ref ComTypes.DISPPARAMS pDispParams,
            out object VarResult,
            out ComTypes.EXCEPINFO pExcepInfo,
            out uint puArgErr);
    }

    /// <summary>
    /// Layout of the IDispatch vtable.
    /// </summary>
    internal enum IDispatchMethodIndices
    {
        IUnknown_QueryInterface,
        IUnknown_AddRef,
        IUnknown_Release,

        IDispatch_GetTypeInfoCount,
        IDispatch_GetTypeInfo,
        IDispatch_GetIDsOfNames,
        IDispatch_Invoke
    }

    [
    ComImport,
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    Guid("B196B283-BAB4-101A-B69C-00AA00341D07")
    ]
    internal interface IProvideClassInfo
    {
        void GetClassInfo(out IntPtr info);
    }
}

#endif

