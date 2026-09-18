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
            const int ErrorInsufficientBuffer = 122;
            const int AppModelErrorNoPackage = 15700;

            try
            {
                uint length = 0;
                int result = GetCurrentPackageFamilyNameNative(ref length, Span<char>.Empty);
                if (result is AppModelErrorNoPackage)
                {
                    return null;
                }

                if (result is not ErrorInsufficientBuffer || length is 0)
                {
                    return null;
                }

                Span<char> buffer = stackalloc char[(int)length];
                result = GetCurrentPackageFamilyNameNative(ref length, buffer);

                // The returned length includes the null terminator, which we don't want in the managed string.
                return result is 0 ? new string(buffer[..(int)(length - 1)]) : null;
            }
            catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
            {
                return null;
            }
        }
    }
}
