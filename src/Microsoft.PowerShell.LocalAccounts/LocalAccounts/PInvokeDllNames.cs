// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// PinvokeDllNames contains the DLL names to be use for PInvoke in FullCLR/CoreCLR powershell.
    ///
    /// * When adding a new DLL name here, make sure that you add both the FullCLR and CoreCLR version
    ///   of it. Add the comment '/*COUNT*/' with the new DLL name, and make sure the 'COUNT' is the
    ///   same for both FullCLR and CoreCLR DLL names.
    /// </summary>
    internal static class PInvokeDllNames
    {
        internal const string LookupAccountSidDllName = "api-ms-win-security-lsalookup-l2-1-1.dll";          /* 1*/
        internal const string LookupAccountNameDllName = "api-ms-win-security-lsalookup-l2-1-1.dll";         /* 2*/
        internal const string FormatMessageDllName = "api-ms-win-core-localization-l1-2-1";                  /* 3*/
    }
}
