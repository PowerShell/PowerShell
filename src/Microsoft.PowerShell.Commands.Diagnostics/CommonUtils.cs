// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.PowerShell.Commands.Diagnostics.Common
{
    internal static class CommonUtilities
    {
        private const string LibraryLoadDllName = "api-ms-win-core-libraryloader-l1-2-0.dll";
        private const string LocalizationDllName = "api-ms-win-core-localization-l1-2-1.dll";

        private const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        private const uint FORMAT_MESSAGE_FROM_HMODULE = 0x00000800;

        [DllImport(LocalizationDllName, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint FormatMessage(uint dwFlags, IntPtr lpSource,
           uint dwMessageId, uint dwLanguageId,
           [MarshalAs(UnmanagedType.LPWStr)]
           StringBuilder lpBuffer,
           uint nSize, IntPtr Arguments);

        [DllImport(LibraryLoadDllName, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryEx(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            IntPtr hFile,
            uint dwFlags
            );

        [DllImport(LibraryLoadDllName)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport(LocalizationDllName, EntryPoint = "GetUserDefaultLangID", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        private static extern ushort GetUserDefaultLangID();

        public static uint FormatMessageFromModule(uint lastError, string moduleName, out string msg)
        {
            Debug.Assert(!string.IsNullOrEmpty(moduleName));

            uint formatError = 0;
            msg = string.Empty;

            IntPtr moduleHandle = LoadLibraryEx(moduleName, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
            if (moduleHandle == IntPtr.Zero)
            {
                return (uint)Marshal.GetLastWin32Error();
            }

            try
            {
                uint LANGID = (uint)GetUserDefaultLangID();
                uint langError = (uint)Marshal.GetLastWin32Error();
                if (langError != 0)
                {
                    LANGID = 0; // neutral
                }

                StringBuilder outStringBuilder = new(1024);
                uint nChars = FormatMessage(
                    dwFlags: FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_FROM_HMODULE,
                    lpSource: moduleHandle,
                    dwMessageId: lastError,
                    dwLanguageId: LANGID,
                    lpBuffer: outStringBuilder,
                    nSize: (uint)outStringBuilder.Capacity,
                    Arguments: IntPtr.Zero);

                if (nChars == 0)
                {
                    formatError = (uint)Marshal.GetLastWin32Error();
                }
                else
                {
                    msg = outStringBuilder.ToString();
                    if (msg.EndsWith(Environment.NewLine, StringComparison.Ordinal))
                    {
                        msg = msg.Substring(0, msg.Length - 2);
                    }
                }
            }
            finally
            {
                FreeLibrary(moduleHandle);
            }
            return formatError;
        }

        public static ResourceManager GetResourceManager()
        {
            return new ResourceManager("Microsoft.PowerShell.Commands.Diagnostics.resources.GetEventResources", typeof(CommonUtilities).GetTypeInfo().Assembly);
        }
    }
}
