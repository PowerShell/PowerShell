// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        internal const sbyte PHCM_APPLICATION_DEFAULT = 0;
        internal const sbyte PHCM_DISGUISE_PLACEHOLDER = 1;
        internal const sbyte PHCM_EXPOSE_PLACEHOLDERS = 2;
        internal const sbyte PHCM_MAX = 2;
        internal const sbyte PHCM_ERROR_INVALID_PARAMETER = -1;
        internal const sbyte PHCM_ERROR_NO_TEB = -2;

        [LibraryImport("ntdll.dll")]
        internal static partial sbyte RtlQueryProcessPlaceholderCompatibilityMode();

        [LibraryImport("ntdll.dll")]
        internal static partial sbyte RtlSetProcessPlaceholderCompatibilityMode(sbyte pcm);
    }
}
