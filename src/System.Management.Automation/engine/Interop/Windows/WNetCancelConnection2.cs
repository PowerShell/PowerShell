// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        [LibraryImport("mpr.dll", EntryPoint = "WNetCancelConnection2W", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int WNetCancelConnection2W(string driveName, int flags, [MarshalAs(UnmanagedType.Bool)] bool force);

        internal static int WNetCancelConnection2(string driveName, int flags, bool force)
        {
            if (s_WNetApiNotAvailable)
            {
                return ERROR_NOT_SUPPORTED;
            }

            int errorCode = ERROR_NO_NETWORK;

            try
            {
                errorCode = WNetCancelConnection2W(driveName, flags, force: true);
            }
            catch (System.DllNotFoundException)
            {
                s_WNetApiNotAvailable = true;
                return ERROR_NOT_SUPPORTED;
            }

            return errorCode;
        }
    }
}
