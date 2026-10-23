// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Windows
    {
        [LibraryImport("kernel32.dll", EntryPoint = "GetCurrentPackageFamilyName", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int GetCurrentPackageFamilyNameNative(ref uint packageFamilyNameLength, Span<char> packageFamilyName);

        /// <summary>
        /// Returns the package family name of the current process when it has package (MSIX) identity; otherwise null.
        /// </summary>
        internal static string? GetCurrentPackageFamilyName()
        {
            const int ErrorSuccess = 0;
            const int PackageFamilyNameMaxLength = 65; // Maximum name plus null terminator.

            try
            {
                uint length = PackageFamilyNameMaxLength;
                Span<char> buffer = stackalloc char[PackageFamilyNameMaxLength];
                int result = GetCurrentPackageFamilyNameNative(ref length, buffer);
                if (result is not ErrorSuccess || length is 0)
                {
                    return null;
                }

                // The returned length includes the null terminator, which we don't want in the managed string.
                return new string(buffer[..(int)(length - 1)]);
            }
            catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
            {
                return null;
            }
        }
    }
}
