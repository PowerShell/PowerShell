// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

using COM = System.Runtime.InteropServices.ComTypes;

#nullable enable
namespace System.Management.Automation
{
    /// <summary>
    /// The IDispatch interface.
    /// </summary>
    [Guid("00020400-0000-0000-c000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IDispatch
    {
        [PreserveSig]
        int GetTypeInfoCount(out int info);

        [PreserveSig]
        int GetTypeInfo(int iTInfo, int lcid, out COM.ITypeInfo? ppTInfo);

        void GetIDsOfNames(
            [MarshalAs(UnmanagedType.LPStruct)] Guid iid,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] rgszNames,
            int cNames,
            int lcid,
            [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I4)] int[] rgDispId);

        void Invoke(
            int dispIdMember,
            [MarshalAs(UnmanagedType.LPStruct)] Guid iid,
            int lcid,
            COM.INVOKEKIND wFlags,
            [In, Out][MarshalAs(UnmanagedType.LPArray)] COM.DISPPARAMS[] paramArray,
            out object? pVarResult,
            out ComInvoker.EXCEPINFO pExcepInfo,
            out uint puArgErr);
    }
}
