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
        internal const string GetLastErrorDllName = "api-ms-win-core-errorhandling-l1-1-0.dll";              /* 1*/
        internal const string LookupAccountSidDllName = "api-ms-win-security-lsalookup-l2-1-1.dll";          /* 2*/
        internal const string IsValidSidDllName = "api-ms-win-security-base-l1-2-0.dll";                     /* 3*/
        internal const string GetLengthSidDllName = "api-ms-win-security-base-l1-2-0.dll";                   /* 4*/
        internal const string LsaFreeMemoryDllName = "api-ms-win-security-lsapolicy-l1-1-0.dll";             /* 5*/
        internal const string LsaOpenPolicyDllName = "api-ms-win-security-lsapolicy-l1-1-0.dll";             /* 6*/
        internal const string LsaQueryInformationPolicyDllName = "api-ms-win-security-lsapolicy-l1-1-0.dll"; /* 7*/
        internal const string LsaCloseDllName = "api-ms-win-security-lsapolicy-l1-1-0.dll";                  /* 8*/
        internal const string LookupAccountNameDllName = "api-ms-win-security-lsalookup-l2-1-1.dll";         /* 9*/
        internal const string GetComputerNameDllName = "api-ms-win-downlevel-kernel32-l2-1-0.dll";           /*10*/
        internal const string GetSecurityDescriptorDaclDllName = "api-ms-win-security-base-l1-2-0";          /*11*/
        internal const string SetSecurityDescriptorDaclDllName = "api-ms-win-security-base-l1-2-0";          /*12*/
        internal const string FormatMessageDllName = "api-ms-win-core-localization-l1-2-1";                  /*13*/
        internal const string GetVersionExDllName = "api-ms-win-core-sysinfo-l1-2-1.dll";                    /*14*/
    }
}
