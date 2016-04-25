//---------------------------------------------------------------------
// <copyright file="RemotableNativeMethods.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Assigns ID numbers to the MSI APIs that are remotable.
    /// </summary>
    /// <remarks><p>
    /// This enumeration MUST stay in sync with the
    /// unmanaged equivalent in RemoteMsiSession.h!
    /// </p></remarks>
    internal enum RemoteMsiFunctionId
    {
        EndSession = 0,
        MsiCloseHandle,
        MsiCreateRecord,
        MsiDatabaseGetPrimaryKeys,
        MsiDatabaseIsTablePersistent,
        MsiDatabaseOpenView,
        MsiDoAction,
        MsiEnumComponentCosts,
        MsiEvaluateCondition,
        MsiFormatRecord,
        MsiGetActiveDatabase,
        MsiGetComponentState,
        MsiGetFeatureCost,
        MsiGetFeatureState,
        MsiGetFeatureValidStates,
        MsiGetLanguage,
        MsiGetLastErrorRecord,
        MsiGetMode,
        MsiGetProperty,
        MsiGetSourcePath,
        MsiGetSummaryInformation,
        MsiGetTargetPath,
        MsiProcessMessage,
        MsiRecordClearData,
        MsiRecordDataSize,
        MsiRecordGetFieldCount,
        MsiRecordGetInteger,
        MsiRecordGetString,
        MsiRecordIsNull,
        MsiRecordReadStream,
        MsiRecordSetInteger,
        MsiRecordSetStream,
        MsiRecordSetString,
        MsiSequence,
        MsiSetComponentState,
        MsiSetFeatureAttributes,
        MsiSetFeatureState,
        MsiSetInstallLevel,
        MsiSetMode,
        MsiSetProperty,
        MsiSetTargetPath,
        MsiSummaryInfoGetProperty,
        MsiVerifyDiskSpace,
        MsiViewExecute,
        MsiViewFetch,
        MsiViewGetError,
        MsiViewGetColumnInfo,
        MsiViewModify,
    }

    /// <summary>
    /// Defines the signature of the native function
    /// in SfxCA.dll that implements the remoting call.
    /// </summary>
    internal delegate void MsiRemoteInvoke(
        RemoteMsiFunctionId id,
        [MarshalAs(UnmanagedType.SysInt)]
        IntPtr request,
        [MarshalAs(UnmanagedType.SysInt)]
        out IntPtr response);

    /// <summary>
    /// Redirects native API calls to either the normal NativeMethods class
    /// or to out-of-proc calls via the remoting channel.
    /// </summary>
    internal static class RemotableNativeMethods
    {
        private const int MAX_REQUEST_FIELDS = 4;
        private static int requestFieldDataOffset;
        private static int requestFieldSize;

        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private static IntPtr requestBuf;

        private static MsiRemoteInvoke remotingDelegate;

        /// <summary>
        /// Checks if the current process is using remoting to access the
        /// MSI session and database APIs.
        /// </summary>
        internal static bool RemotingEnabled
        {
            get
            {
                return RemotableNativeMethods.remotingDelegate != null;
            }
        }

        /// <summary>
        /// Sets a delegate that is used to make remote API calls.
        /// </summary>
        /// <remarks><p>
        /// The implementation of this delegate is provided by the
        /// custom action host DLL.
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static MsiRemoteInvoke RemotingDelegate
        {
            set
            {
                RemotableNativeMethods.remotingDelegate = value;

                if (value != null && requestBuf == IntPtr.Zero)
                {
                    requestFieldDataOffset = Marshal.SizeOf(typeof(IntPtr));
                    requestFieldSize = 2 * Marshal.SizeOf(typeof(IntPtr));
                    RemotableNativeMethods.requestBuf = Marshal.AllocHGlobal(
                        requestFieldSize * MAX_REQUEST_FIELDS);
                }
            }
        }

        internal static bool IsRemoteHandle(int handle)
        {
            return (handle & Int32.MinValue) != 0;
        }

        internal static int MakeRemoteHandle(int handle)
        {
            if (handle == 0)
            {
                return handle;
            }

            if (RemotableNativeMethods.IsRemoteHandle(handle))
            {
                throw new InvalidOperationException("Handle already has the remote bit set.");
            }

            return handle ^ Int32.MinValue;
        }

        internal static int GetRemoteHandle(int handle)
        {
            if (handle == 0)
            {
                return handle;
            }

            if (!RemotableNativeMethods.IsRemoteHandle(handle))
            {
                throw new InvalidOperationException("Handle does not have the remote bit set.");
            }

            return handle ^ Int32.MinValue;
        }

        private static void ClearData(IntPtr buf)
        {
            for (int i = 0; i < MAX_REQUEST_FIELDS; i++)
            {
                Marshal.WriteInt32(buf, (i * requestFieldSize), (int) VarEnum.VT_NULL);
                Marshal.WriteIntPtr(buf, (i * requestFieldSize) + requestFieldDataOffset, IntPtr.Zero);
            }
        }

        private static void WriteInt(IntPtr buf, int field, int value)
        {
            Marshal.WriteInt32(buf, (field * requestFieldSize), (int) VarEnum.VT_I4);
            Marshal.WriteInt32(buf, (field * requestFieldSize) + requestFieldDataOffset, value);
        }

        private static void WriteString(IntPtr buf, int field, string value)
        {
            if (value == null)
            {
                Marshal.WriteInt32(buf, (field * requestFieldSize), (int) VarEnum.VT_NULL);
                Marshal.WriteIntPtr(buf, (field * requestFieldSize) + requestFieldDataOffset, IntPtr.Zero);
            }
            else
            {
                IntPtr stringPtr = Marshal.StringToHGlobalUni(value);
                Marshal.WriteInt32(buf, (field * requestFieldSize), (int) VarEnum.VT_LPWSTR);
                Marshal.WriteIntPtr(buf, (field * requestFieldSize) + requestFieldDataOffset, stringPtr);
            }
        }

        private static int ReadInt(IntPtr buf, int field)
        {
            VarEnum vt = (VarEnum) Marshal.ReadInt32(buf, (field * requestFieldSize));
            if (vt == VarEnum.VT_EMPTY)
            {
                return 0;
            }
            else if (vt != VarEnum.VT_I4 && vt != VarEnum.VT_UI4)
            {
                throw new InstallerException("Invalid data received from remote MSI function invocation.");
            }
            return Marshal.ReadInt32(buf, (field * requestFieldSize) + requestFieldDataOffset);
        }

        private static void ReadString(IntPtr buf, int field, StringBuilder szBuf, ref uint cchBuf)
        {
            VarEnum vt = (VarEnum) Marshal.ReadInt32(buf, (field * requestFieldSize));
            if (vt == VarEnum.VT_NULL)
            {
                szBuf.Remove(0, szBuf.Length);
                return;
            }
            else if (vt != VarEnum.VT_LPWSTR)
            {
                throw new InstallerException("Invalid data received from remote MSI function invocation.");
            }

            szBuf.Remove(0, szBuf.Length);
            IntPtr strPtr = Marshal.ReadIntPtr(buf, (field * requestFieldSize) + requestFieldDataOffset);
            string str = Marshal.PtrToStringUni(strPtr);
            if (str != null)
            {
                szBuf.Append(str);
            }
            cchBuf = (uint) szBuf.Length;
        }

        private static void FreeString(IntPtr buf, int field)
        {
            IntPtr stringPtr = Marshal.ReadIntPtr(buf, (field * requestFieldSize) + requestFieldDataOffset);
            if (stringPtr != null)
            {
                Marshal.FreeHGlobal(stringPtr);
            }
        }

        private static void ReadStream(IntPtr buf, int field, byte[] sBuf, int count)
        {
            VarEnum vt = (VarEnum) Marshal.ReadInt32(buf, (field * requestFieldSize));
            if (vt != VarEnum.VT_STREAM)
            {
                throw new InstallerException("Invalid data received from remote MSI function invocation.");
            }

            IntPtr sPtr = Marshal.ReadIntPtr(buf, (field * requestFieldSize) + requestFieldDataOffset);
            Marshal.Copy(sPtr, sBuf, 0, count);
        }

        private static uint MsiFunc_III(RemoteMsiFunctionId id, int in1, int in2, int in3)
        {
            lock (RemotableNativeMethods.remotingDelegate)
            {
                ClearData(requestBuf);
                WriteInt(requestBuf, 0, in1);
                WriteInt(requestBuf, 1, in2);
                WriteInt(requestBuf, 2, in3);
                IntPtr resp;
                remotingDelegate(id, requestBuf, out resp);
                return unchecked ((uint) ReadInt(resp, 0));
            }
        }

        private static uint MsiFunc_IIS(RemoteMsiFunctionId id, int in1, int in2, string in3)
        {
            lock (RemotableNativeMethods.remotingDelegate)
            {
                ClearData(requestBuf);
                WriteInt(requestBuf, 0, in1);
                WriteInt(requestBuf, 1, in2);
                WriteString(requestBuf, 2, in3);
                IntPtr resp;
                remotingDelegate(id, requestBuf, out resp);
                FreeString(requestBuf, 2);
                return unchecked ((uint) ReadInt(resp, 0));
            }
        }

        private static uint MsiFunc_ISI(RemoteMsiFunctionId id, int in1, string in2, int in3)
        {
            lock (RemotableNativeMethods.remotingDelegate)
            {
                ClearData(requestBuf);
                WriteInt(requestBuf, 0, in1);
                WriteString(requestBuf, 1, in2);
                WriteInt(requestBuf, 2, in3);
                IntPtr resp;
                remotingDelegate(id, requestBuf, out resp);
                FreeString(requestBuf, 2);
                return unchecked ((uint) ReadInt(resp, 0));
            }
        }

        private static uint MsiFunc_ISS(RemoteMsiFunctionId id, int in1, string in2, string in3)
        {
            lock (RemotableNativeMethods.remotingDelegate)
            {
                ClearData(requestBuf);
                WriteInt(requestBuf, 0, in1);
                WriteString(requestBuf, 1, in2);
                WriteString(requestBuf, 2, in3);
                IntPtr resp;
                remotingDelegate(id, requestBuf, out resp);
                FreeString(requestBuf, 1);
                FreeString(requestBuf, 2);
                return unchecked ((uint) ReadInt(resp, 0));
            }
        }

        private static uint MsiFunc_II_I(RemoteMsiFunctionId id, int in1, int in2, out int out1)
        {
            lock (RemotableNativeMethods.remotingDelegate)
            {
                ClearData(requestBuf);
                WriteInt(requestBuf, 0, in1);
                WriteInt(requestBuf, 1, in2);
                IntPtr resp;
                remotingDelegate(id, requestBuf, out resp);
                uint ret = unchecked ((uint) ReadInt(resp, 0));
                out1 = ReadInt(resp, 1);
                return ret;
            }
        }

        private static uint MsiFunc_ISII_I(RemoteMsiFunctionId id, int in1, string in2, int in3, int in4, out int out1)
        {
            lock (RemotableNativeMethods.remotingDelegate)
            {
                ClearData(requestBuf);
                WriteInt(requestBuf, 0, in1);
                WriteString(requestBuf, 1, in2);
                WriteInt(requestBuf, 2, in3);
                WriteInt(requestBuf, 3, in4);
                IntPtr resp;
                remotingDelegate(id, requestBuf, out resp);
                FreeString(requestBuf, 1);
                uint ret = unchecked ((uint) ReadInt(resp, 0));
                out1 = ReadInt(resp, 1);
                return ret;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static uint MsiFunc_IS_II(RemoteMsiFunctionId id, int in1, string in2, out int out1, out int out2)
        {
            lock (RemotableNativeMethods.remotingDelegate)
            {
                ClearData(requestBuf);
                WriteInt(requestBuf, 0, in1);
                WriteString(requestBuf, 1, in2);
                IntPtr resp;
                remotingDelegate(id, requestBuf, out resp);
                FreeString(requestBuf, 1);
                uint ret = unchecked ((uint) ReadInt(resp, 0));
                out1 = ReadInt(resp, 1);
                out2 = ReadInt(resp, 2);
                return ret;
            }
        }

        private static uint MsiFunc_II_S(RemoteMsiFunctionId id, int in1, int in2, StringBuilder out1, ref uint cchOut1)
        {
            lock (RemotableNativeMethods.remotingDelegate)
            {
                ClearData(requestBuf);
                WriteInt(requestBuf, 0, in1);
                WriteInt(requestBuf, 1, in2);
                IntPtr resp;
                remotingDelegate(id, requestBuf, out resp);
                uint ret = unchecked ((uint) ReadInt(resp, 0));
                if (ret == 0) ReadString(resp, 1, out1, ref cchOut1);
                return ret;
            }
        }

        private static uint MsiFunc_IS_S(RemoteMsiFunctionId id, int in1, string in2, StringBuilder out1, ref uint cchOut1)
        {
            lock (RemotableNativeMethods.remotingDelegate)
            {
                ClearData(requestBuf);
                WriteInt(requestBuf, 0, in1);
                WriteString(requestBuf, 1, in2);
                IntPtr resp;
                remotingDelegate(id, requestBuf, out resp);
                FreeString(requestBuf, 1);
                uint ret = unchecked ((uint) ReadInt(resp, 0));
                if (ret == 0) ReadString(resp, 1, out1, ref cchOut1);
                return ret;
            }
        }

        private static uint MsiFunc_ISII_SII(RemoteMsiFunctionId id, int in1, string in2, int in3, int in4, StringBuilder out1, ref uint cchOut1, out int out2, out int out3)
        {
            lock (RemotableNativeMethods.remotingDelegate)
            {
                ClearData(requestBuf);
                WriteInt(requestBuf, 0, in1);
                WriteString(requestBuf, 1, in2);
                WriteInt(requestBuf, 2, in3);
                WriteInt(requestBuf, 3, in4);
                IntPtr resp;
                remotingDelegate(id, requestBuf, out resp);
                FreeString(requestBuf, 1);
                uint ret = unchecked ((uint) ReadInt(resp, 0));
                if (ret == 0) ReadString(resp, 1, out1, ref cchOut1);
                out2 = ReadInt(resp, 2);
                out3 = ReadInt(resp, 3);
                return ret;
            }
        }

        internal static int MsiProcessMessage(int hInstall, uint eMessageType, int hRecord)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
            {
                return NativeMethods.MsiProcessMessage(hInstall, eMessageType, hRecord);
            }
            else lock (remotingDelegate)
            {
                // I don't understand why, but this particular function doesn't work
                // when using the static requestBuf -- some data doesn't make it through.
                // But it works when a fresh buffer is allocated here every call.
                IntPtr buf = Marshal.AllocHGlobal(
                    requestFieldSize * MAX_REQUEST_FIELDS);
                ClearData(buf);
                WriteInt(buf, 0, RemotableNativeMethods.GetRemoteHandle(hInstall));
                WriteInt(buf, 1, unchecked ((int) eMessageType));
                WriteInt(buf, 2, RemotableNativeMethods.GetRemoteHandle(hRecord));
                IntPtr resp;
                remotingDelegate(RemoteMsiFunctionId.MsiProcessMessage, buf, out resp);
                Marshal.FreeHGlobal(buf);
                return ReadInt(resp, 0);
            }
        }

        internal static uint MsiCloseHandle(int hAny)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hAny))
                return NativeMethods.MsiCloseHandle(hAny);
            else
                return RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiCloseHandle, RemotableNativeMethods.GetRemoteHandle(hAny), 0, 0);
        }

        internal static uint MsiGetProperty(int hInstall, string szName, StringBuilder szValueBuf, ref uint cchValueBuf)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiGetProperty(hInstall, szName, szValueBuf, ref cchValueBuf);
            else
            {
                return RemotableNativeMethods.MsiFunc_IS_S(
                    RemoteMsiFunctionId.MsiGetProperty,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szName,
                    szValueBuf,
                    ref cchValueBuf);
            }
        }

        internal static uint MsiSetProperty(int hInstall, string szName, string szValue)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiSetProperty(hInstall, szName, szValue);
            else
            {
                return RemotableNativeMethods.MsiFunc_ISS(
                    RemoteMsiFunctionId.MsiSetProperty,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szName,
                    szValue);
            }
        }

        internal static int MsiCreateRecord(uint cParams, int hAny)
        {
            // When remoting is enabled, we might need to create either a local or
            // remote record, depending on the handle it is to have an affinity with.
            // If no affinity handle is specified, create a remote record (the 99% case).
            if (!RemotingEnabled ||
                (hAny != 0 && !RemotableNativeMethods.IsRemoteHandle(hAny)))
            {
                return NativeMethods.MsiCreateRecord(cParams);
            }
            else
            {
                int hRecord = unchecked((int)RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiCreateRecord, (int) cParams, 0, 0));
                return RemotableNativeMethods.MakeRemoteHandle(hRecord);
            }
        }

        internal static uint MsiRecordGetFieldCount(int hRecord)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hRecord))
                return NativeMethods.MsiRecordGetFieldCount(hRecord);
            else
            {
                return RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiRecordGetFieldCount,
                    RemotableNativeMethods.GetRemoteHandle(hRecord),
                    0,
                    0);
            }
        }

        internal static int MsiRecordGetInteger(int hRecord, uint iField)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hRecord))
                return NativeMethods.MsiRecordGetInteger(hRecord, iField);
            else
            {
                return unchecked ((int) RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiRecordGetInteger,
                    RemotableNativeMethods.GetRemoteHandle(hRecord),
                    (int) iField,
                    0));
            }
        }

        internal static uint MsiRecordGetString(int hRecord, uint iField, StringBuilder szValueBuf, ref uint cchValueBuf)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hRecord))
            {
                return NativeMethods.MsiRecordGetString(hRecord, iField, szValueBuf, ref cchValueBuf);
            }
            else
            {
                return RemotableNativeMethods.MsiFunc_II_S(
                    RemoteMsiFunctionId.MsiRecordGetString,
                    RemotableNativeMethods.GetRemoteHandle(hRecord),
                    (int) iField,
                    szValueBuf,
                    ref cchValueBuf);
            }
        }

        internal static uint MsiRecordSetInteger(int hRecord, uint iField, int iValue)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hRecord))
                return NativeMethods.MsiRecordSetInteger(hRecord, iField, iValue);
            else
            {
                return RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiRecordSetInteger,
                    RemotableNativeMethods.GetRemoteHandle(hRecord),
                    (int) iField,
                    iValue);
            }
        }

        internal static uint MsiRecordSetString(int hRecord, uint iField, string szValue)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hRecord))
                return NativeMethods.MsiRecordSetString(hRecord, iField, szValue);
            else
            {
                return RemotableNativeMethods.MsiFunc_IIS(
                    RemoteMsiFunctionId.MsiRecordSetString,
                    RemotableNativeMethods.GetRemoteHandle(hRecord),
                    (int) iField,
                    szValue);
            }
        }

        internal static int MsiGetActiveDatabase(int hInstall)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiGetActiveDatabase(hInstall);
            else
            {
                int hDatabase = (int)RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiGetActiveDatabase,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    0,
                    0);
                return RemotableNativeMethods.MakeRemoteHandle(hDatabase);
            }
        }

        internal static uint MsiDatabaseOpenView(int hDatabase, string szQuery, out int hView)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hDatabase))
                return NativeMethods.MsiDatabaseOpenView(hDatabase, szQuery, out hView);
            else
            {
                uint err = RemotableNativeMethods.MsiFunc_ISII_I(
                    RemoteMsiFunctionId.MsiDatabaseOpenView,
                    RemotableNativeMethods.GetRemoteHandle(hDatabase),
                    szQuery,
                    0,
                    0,
                    out hView);
                hView = RemotableNativeMethods.MakeRemoteHandle(hView);
                return err;
            }
        }

        internal static uint MsiViewExecute(int hView, int hRecord)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hView))
                return NativeMethods.MsiViewExecute(hView, hRecord);
            else
            {
                return RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiViewExecute,
                    RemotableNativeMethods.GetRemoteHandle(hView),
                    RemotableNativeMethods.GetRemoteHandle(hRecord),
                    0);
            }
        }

        internal static uint MsiViewFetch(int hView, out int hRecord)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hView))
                return NativeMethods.MsiViewFetch(hView, out hRecord);
            else
            {
                uint err = RemotableNativeMethods.MsiFunc_II_I(
                    RemoteMsiFunctionId.MsiViewFetch,
                    RemotableNativeMethods.GetRemoteHandle(hView),
                    0,
                    out hRecord);
                hRecord = RemotableNativeMethods.MakeRemoteHandle(hRecord);
                return err;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static uint MsiViewModify(int hView, int iModifyMode, int hRecord)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hView))
                return NativeMethods.MsiViewModify(hView, iModifyMode, hRecord);
            else
            {
                return RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiViewModify,
                    RemotableNativeMethods.GetRemoteHandle(hView),
                    iModifyMode,
                    RemotableNativeMethods.GetRemoteHandle(hRecord));
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static int MsiViewGetError(int hView, StringBuilder szColumnNameBuffer, ref uint cchBuf)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hView))
                return NativeMethods.MsiViewGetError(hView, szColumnNameBuffer, ref cchBuf);
            else
            {
                return unchecked ((int) RemotableNativeMethods.MsiFunc_II_S(
                    RemoteMsiFunctionId.MsiViewGetError,
                    RemotableNativeMethods.GetRemoteHandle(hView),
                    0,
                    szColumnNameBuffer,
                    ref cchBuf));
            }
        }

        internal static uint MsiViewGetColumnInfo(int hView, uint eColumnInfo, out int hRecord)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hView))
                return NativeMethods.MsiViewGetColumnInfo(hView, eColumnInfo, out hRecord);
            else
            {
                uint err = RemotableNativeMethods.MsiFunc_II_I(
                    RemoteMsiFunctionId.MsiViewGetColumnInfo,
                    RemotableNativeMethods.GetRemoteHandle(hView),
                    (int) eColumnInfo,
                    out hRecord);
                hRecord = RemotableNativeMethods.MakeRemoteHandle(hRecord);
                return err;
            }
        }

        internal static uint MsiFormatRecord(int hInstall, int hRecord, StringBuilder szResultBuf, ref uint cchResultBuf)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hRecord))
                return NativeMethods.MsiFormatRecord(hInstall, hRecord, szResultBuf, ref cchResultBuf);
            else
            {
                return RemotableNativeMethods.MsiFunc_II_S(
                    RemoteMsiFunctionId.MsiFormatRecord,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    RemotableNativeMethods.GetRemoteHandle(hRecord),
                    szResultBuf,
                    ref cchResultBuf);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static uint MsiRecordClearData(int hRecord)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hRecord))
                return NativeMethods.MsiRecordClearData(hRecord);
            else
            {
                return RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiRecordClearData,
                    RemotableNativeMethods.GetRemoteHandle(hRecord),
                    0,
                    0);
            }
        }

        internal static bool MsiRecordIsNull(int hRecord, uint iField)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hRecord))
                return NativeMethods.MsiRecordIsNull(hRecord, iField);
            else
            {
                return 0 != RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiRecordIsNull,
                    RemotableNativeMethods.GetRemoteHandle(hRecord),
                    (int) iField,
                    0);
            }
        }

        internal static uint MsiDatabaseGetPrimaryKeys(int hDatabase, string szTableName, out int hRecord)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hDatabase))
                return NativeMethods.MsiDatabaseGetPrimaryKeys(hDatabase, szTableName, out hRecord);
            else
            {
                uint err = RemotableNativeMethods.MsiFunc_ISII_I(
                    RemoteMsiFunctionId.MsiDatabaseGetPrimaryKeys,
                    RemotableNativeMethods.GetRemoteHandle(hDatabase),
                    szTableName,
                    0,
                    0,
                    out hRecord);
                hRecord = RemotableNativeMethods.MakeRemoteHandle(hRecord);
                return err;
            }
        }

        internal static uint MsiDatabaseIsTablePersistent(int hDatabase, string szTableName)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hDatabase))
                return NativeMethods.MsiDatabaseIsTablePersistent(hDatabase, szTableName);
            else
            {
                return RemotableNativeMethods.MsiFunc_ISI(
                    RemoteMsiFunctionId.MsiDatabaseIsTablePersistent,
                    RemotableNativeMethods.GetRemoteHandle(hDatabase),
                    szTableName,
                    0);
            }
        }

        internal static uint MsiDoAction(int hInstall, string szAction)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiDoAction(hInstall, szAction);
            else
            {
                return RemotableNativeMethods.MsiFunc_ISI(
                    RemoteMsiFunctionId.MsiDoAction,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szAction,
                    0);
            }
        }

        internal static uint MsiEnumComponentCosts(int hInstall, string szComponent, uint dwIndex, int iState, StringBuilder lpDriveBuf, ref uint cchDriveBuf, out int iCost, out int iTempCost)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiEnumComponentCosts(hInstall, szComponent, dwIndex, iState, lpDriveBuf, ref cchDriveBuf, out iCost, out iTempCost);
            else
            {
                return RemotableNativeMethods.MsiFunc_ISII_SII(
                    RemoteMsiFunctionId.MsiEvaluateCondition,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szComponent, (int) dwIndex, iState, lpDriveBuf, ref cchDriveBuf, out iCost, out iTempCost);
            }
        }

        internal static uint MsiEvaluateCondition(int hInstall, string szCondition)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiEvaluateCondition(hInstall, szCondition);
            else
            {
                return RemotableNativeMethods.MsiFunc_ISI(
                    RemoteMsiFunctionId.MsiEvaluateCondition,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szCondition,
                    0);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static uint MsiGetComponentState(int hInstall, string szComponent, out int iInstalled, out int iAction)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiGetComponentState(hInstall, szComponent, out iInstalled, out iAction);
            else
            {
                return RemotableNativeMethods.MsiFunc_IS_II(
                    RemoteMsiFunctionId.MsiGetComponentState,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szComponent,
                    out iInstalled,
                    out iAction);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static uint MsiGetFeatureCost(int hInstall, string szFeature, int iCostTree, int iState, out int iCost)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiGetFeatureCost(hInstall, szFeature, iCostTree, iState, out iCost);
            else
            {
                return RemotableNativeMethods.MsiFunc_ISII_I(
                    RemoteMsiFunctionId.MsiGetFeatureCost,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szFeature,
                    iCostTree,
                    iState,
                    out iCost);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static uint MsiGetFeatureState(int hInstall, string szFeature, out int iInstalled, out int iAction)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiGetFeatureState(hInstall, szFeature, out iInstalled, out iAction);
            else
            {
                return RemotableNativeMethods.MsiFunc_IS_II(
                    RemoteMsiFunctionId.MsiGetFeatureState,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szFeature,
                    out iInstalled,
                    out iAction);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static uint MsiGetFeatureValidStates(int hInstall, string szFeature, out uint dwInstalledState)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiGetFeatureValidStates(hInstall, szFeature, out dwInstalledState);
            else
            {
                int iTemp;
                uint ret = RemotableNativeMethods.MsiFunc_ISII_I(
                    RemoteMsiFunctionId.MsiGetFeatureValidStates,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szFeature,
                    0,
                    0,
                    out iTemp);
                dwInstalledState = (uint) iTemp;
                return ret;
            }
        }

        internal static int MsiGetLanguage(int hInstall)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiGetLanguage(hInstall);
            else
            {
                return unchecked((int)RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiGetLanguage,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    0,
                    0));
            }
        }

        internal static int MsiGetLastErrorRecord(int hAny)
        {
            // When remoting is enabled, we might need to create either a local or
            // remote record, depending on the handle it is to have an affinity with.
            // If no affinity handle is specified, create a remote record (the 99% case).
            if (!RemotingEnabled ||
                (hAny != 0 && !RemotableNativeMethods.IsRemoteHandle(hAny)))
            {
                return NativeMethods.MsiGetLastErrorRecord();
            }
            else
            {
                int hRecord = unchecked((int) RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiGetLastErrorRecord, 0, 0, 0));
                return RemotableNativeMethods.MakeRemoteHandle(hRecord);
            }
        }

        internal static bool MsiGetMode(int hInstall, uint iRunMode)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiGetMode(hInstall, iRunMode);
            else
            {
                return 0 != RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiGetMode,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    (int) iRunMode,
                    0);
            }
        }

        internal static uint MsiGetSourcePath(int hInstall, string szFolder, StringBuilder szPathBuf, ref uint cchPathBuf)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiGetSourcePath(hInstall, szFolder, szPathBuf, ref cchPathBuf);
            else
            {
                return RemotableNativeMethods.MsiFunc_IS_S(
                    RemoteMsiFunctionId.MsiGetSourcePath,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szFolder,
                    szPathBuf,
                    ref cchPathBuf);
            }
        }

        internal static uint MsiGetSummaryInformation(int hDatabase, string szDatabasePath, uint uiUpdateCount, out int hSummaryInfo)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hDatabase))
                return NativeMethods.MsiGetSummaryInformation(hDatabase, szDatabasePath, uiUpdateCount, out hSummaryInfo);
            else
            {
                uint err = RemotableNativeMethods.MsiFunc_ISII_I(
                    RemoteMsiFunctionId.MsiGetSummaryInformation,
                    RemotableNativeMethods.GetRemoteHandle(hDatabase),
                    szDatabasePath,
                    (int)uiUpdateCount,
                    0,
                    out hSummaryInfo);
                hSummaryInfo = RemotableNativeMethods.MakeRemoteHandle(hSummaryInfo);
                return err;
            }
        }

        internal static uint MsiGetTargetPath(int hInstall, string szFolder, StringBuilder szPathBuf, ref uint cchPathBuf)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiGetTargetPath(hInstall, szFolder, szPathBuf, ref cchPathBuf);
            else
            {
                return RemotableNativeMethods.MsiFunc_IS_S(
                    RemoteMsiFunctionId.MsiGetTargetPath,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szFolder,
                    szPathBuf,
                    ref cchPathBuf);
            }
        }

        internal static uint MsiRecordDataSize(int hRecord, uint iField)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hRecord))
                return NativeMethods.MsiRecordDataSize(hRecord, iField);
            else
            {
                return RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiRecordDataSize,
                    RemotableNativeMethods.GetRemoteHandle(hRecord),
                    (int) iField, 0);
                }
        }

        internal static uint MsiRecordReadStream(int hRecord, uint iField, byte[] szDataBuf, ref uint cbDataBuf)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hRecord))
            {
                return NativeMethods.MsiRecordReadStream(hRecord, iField, szDataBuf, ref cbDataBuf);
            }
            else lock (RemotableNativeMethods.remotingDelegate)
            {
                ClearData(requestBuf);
                unchecked
                {
                    WriteInt(requestBuf, 0, RemotableNativeMethods.GetRemoteHandle(hRecord));
                    WriteInt(requestBuf, 1, (int) iField);
                    WriteInt(requestBuf, 2, (int) cbDataBuf);
                    IntPtr resp;
                    remotingDelegate(RemoteMsiFunctionId.MsiRecordReadStream, requestBuf, out resp);
                    uint ret = (uint) ReadInt(resp, 0);
                    if (ret == 0)
                    {
                        cbDataBuf = (uint) ReadInt(resp, 2);
                        if (cbDataBuf > 0)
                        {
                            RemotableNativeMethods.ReadStream(resp, 1, szDataBuf, (int) cbDataBuf);
                        }
                    }
                    return ret;
                }
            }
        }

        internal static uint MsiRecordSetStream(int hRecord, uint iField, string szFilePath)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hRecord))
                return NativeMethods.MsiRecordSetStream(hRecord, iField, szFilePath);
            else
            {
                return RemotableNativeMethods.MsiFunc_IIS(
                    RemoteMsiFunctionId.MsiRecordSetStream,
                    RemotableNativeMethods.GetRemoteHandle(hRecord),
                    (int) iField,
                    szFilePath);
            }
        }

        internal static uint MsiSequence(int hInstall, string szTable, int iSequenceMode)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiSequence(hInstall, szTable, iSequenceMode);
            else
            {
                return RemotableNativeMethods.MsiFunc_ISI(
                    RemoteMsiFunctionId.MsiSequence,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szTable,
                    iSequenceMode);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static uint MsiSetComponentState(int hInstall, string szComponent, int iState)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiSetComponentState(hInstall, szComponent, iState);
            else
            {
                return RemotableNativeMethods.MsiFunc_ISI(
                    RemoteMsiFunctionId.MsiSetComponentState,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szComponent,
                    iState);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static uint MsiSetFeatureAttributes(int hInstall, string szFeature, uint dwAttributes)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiSetFeatureAttributes(hInstall, szFeature, dwAttributes);
            else
            {
                return RemotableNativeMethods.MsiFunc_ISI(
                    RemoteMsiFunctionId.MsiSetFeatureAttributes,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szFeature,
                    (int) dwAttributes);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static uint MsiSetFeatureState(int hInstall, string szFeature, int iState)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiSetFeatureState(hInstall, szFeature, iState);
            else
            {
                return RemotableNativeMethods.MsiFunc_ISI(
                    RemoteMsiFunctionId.MsiSetFeatureState,
                    RemotableNativeMethods.GetRemoteHandle(hInstall), szFeature, iState);
            }
        }

        internal static uint MsiSetInstallLevel(int hInstall, int iInstallLevel)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiSetInstallLevel(hInstall, iInstallLevel);
            else
            {
                return RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiSetInstallLevel,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    iInstallLevel,
                    0);
            }
        }

        internal static uint MsiSetMode(int hInstall, uint iRunMode, bool fState)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiSetMode(hInstall, iRunMode, fState);
            else
            {
                return RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiSetMode,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    (int) iRunMode,
                    fState ? 1 : 0);
            }
        }

        internal static uint MsiSetTargetPath(int hInstall, string szFolder, string szFolderPath)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiSetTargetPath(hInstall, szFolder, szFolderPath);
            else
            {
                return RemotableNativeMethods.MsiFunc_ISS(
                    RemoteMsiFunctionId.MsiSetTargetPath,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    szFolder,
                    szFolderPath);
            }
        }

        internal static uint MsiSummaryInfoGetProperty(int hSummaryInfo, uint uiProperty, out uint uiDataType, out int iValue, ref long ftValue, StringBuilder szValueBuf, ref uint cchValueBuf)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hSummaryInfo))
            {
                return NativeMethods.MsiSummaryInfoGetProperty(hSummaryInfo, uiProperty, out uiDataType, out iValue, ref ftValue, szValueBuf, ref cchValueBuf);
            }
            else lock (RemotableNativeMethods.remotingDelegate)
            {
                ClearData(requestBuf);
                WriteInt(requestBuf, 0, RemotableNativeMethods.GetRemoteHandle(hSummaryInfo));
                WriteInt(requestBuf, 1, (int) uiProperty);
                IntPtr resp;
                remotingDelegate(RemoteMsiFunctionId.MsiSummaryInfoGetProperty, requestBuf, out resp);
                unchecked
                {
                    uint ret = (uint) ReadInt(resp, 0);
                    if (ret == 0)
                    {
                        uiDataType = (uint) ReadInt(resp, 1);
                        switch ((VarEnum) uiDataType)
                        {
                            case VarEnum.VT_I2:
                            case VarEnum.VT_I4:
                                iValue = ReadInt(resp, 2);
                                break;

                            case VarEnum.VT_FILETIME:
                                uint ftHigh = (uint) ReadInt(resp, 2);
                                uint ftLow = (uint) ReadInt(resp, 3);
                                ftValue = ((long) ftHigh) << 32 | ((long) ftLow);
                                iValue = 0;
                                break;

                            case VarEnum.VT_LPSTR:
                                ReadString(resp, 2, szValueBuf, ref cchValueBuf);
                                iValue = 0;
                                break;

                            default:
                                iValue = 0;
                                break;
                        }
                    }
                    else
                    {
                        uiDataType = 0;
                        iValue = 0;
                    }
                    return ret;
                }
            }
        }

        internal static uint MsiVerifyDiskSpace(int hInstall)
        {
            if (!RemotingEnabled || !RemotableNativeMethods.IsRemoteHandle(hInstall))
                return NativeMethods.MsiVerifyDiskSpace(hInstall);
            else
            {
                return RemotableNativeMethods.MsiFunc_III(
                    RemoteMsiFunctionId.MsiVerifyDiskSpace,
                    RemotableNativeMethods.GetRemoteHandle(hInstall),
                    0,
                    0);
            }
        }
    }
}
