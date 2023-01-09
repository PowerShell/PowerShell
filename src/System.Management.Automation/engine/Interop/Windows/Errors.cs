// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

internal static partial class Interop
{
    internal static partial class Windows
    {
        // List of error constants https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes
        internal const int ERROR_SUCCESS = 0;
        internal const int ERROR_FILE_NOT_FOUND = 2;
        internal const int ERROR_GEN_FAILURE = 31;
        internal const int ERROR_NOT_SUPPORTED = 50;
        internal const int ERROR_NO_NETWORK = 1222;
        internal const int ERROR_MORE_DATA = 234;
        internal const int ERROR_CONNECTION_UNAVAIL = 1201;
    }
}
