// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if !UNIX
using System;
using System.Buffers;
using System.ComponentModel;
using System.Management.Automation;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Windows
    {
        [LibraryImport(PinvokeDllNames.QueryDosDeviceDllName, EntryPoint = "QueryDosDeviceW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        internal static partial int QueryDosDevice(Span<char> lpDeviceName, Span<char> lpTargetPath, uint ucchMax);

        internal static string GetDosDevice(char deviceName)
        {
            // By default buffer size is set to 300 which would generally be sufficient in most of the cases.
            const int StartLength =
#if DEBUG
                // In debug, validate ArrayPool growth.
                1;
#else
                300;
#endif

            Span<char> buffer = stackalloc char[StartLength + 1];
            Span<char> fullDeviceName = stackalloc char[3] { deviceName, ':', '\0' };
            char[]? rentedArray = null;

            try
            {
                while (true)
                {
                    uint length = (uint)buffer.Length;
                    int retValue = QueryDosDevice(fullDeviceName, buffer, length);
                    if (retValue > 0)
                    {
                        if (buffer.StartsWith("\\??\\"))
                        {
                            // QueryDosDevice always return array of NULL-terinating strings with additional final NULL
                            // so the buffer has always two NULL-s on end.
                            //
                            // "\\??\\UNC\\localhost\\c$\\tmp\0\0" -> "UNC\\localhost\\c$\\tmp\0\0"
                            Span<char> res = buffer.Slice(4);
                            if (res.StartsWith("UNC"))
                            {
                                // -> "C\\localhost\\c$\\tmp\0\0" -> "\\\\localhost\\c$\\tmp"
                                res = res.Slice(2, retValue - 2);
                                res[0] = '\\';

                                // If we want always to have terminating slash -> "\\\\localhost\\c$\\tmp\\"
                                // res = res.Slice(2, retValue - 3);
                                // res[0] = '\\';
                                // res[^1] = '\\';
                            }
                            else if (res[^3] == ':')
                            {
                                // Really it is a dead code since GetDosDevice() is called only if PSDrive.DriveType == DriveType.Network
                                //
                                // The substed path is the root path of a drive. For example: subst Y: C:\
                                // -> "C:\0\0" -> "C:\"
                                res = res.Slice(0, retValue - 1);
                                res[^1] = '\\';
                            }

                            return res.ToString();
                        }
                        else
                        {
                            // Really is is a dead code since GetDosDevice() is called only if PSDrive.DriveType == DriveType.Network
                            //
                            // The drive name is not a substed path, then we return the root path of the drive
                            // "C:\0" -> "C:\\"
                            fullDeviceName[^1] = '\\';
                            return fullDeviceName.ToString();
                        }
                    }

                    // ERROR_INSUFFICIENT_BUFFER = 122 (0x7A)
                    int errorCode = Marshal.GetLastPInvokeError();
                    if (errorCode != 122)
                    {
                        throw new Win32Exception((int)errorCode);
                    }

                    char[]? toReturn = rentedArray;
                    buffer = rentedArray = ArrayPool<char>.Shared.Rent(buffer.Length * 2);
                    if (toReturn is not null)
                    {
                        ArrayPool<char>.Shared.Return(toReturn);
                    }
                }
            }
            finally
            {
                if (rentedArray is not null)
                {
                    ArrayPool<char>.Shared.Return(rentedArray);
                }
            }
        }
    }
}
#endif
