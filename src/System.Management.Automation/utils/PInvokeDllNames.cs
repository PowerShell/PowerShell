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
    internal static class PinvokeDllNames
    {
        internal const string QueryDosDeviceDllName = "api-ms-win-core-file-l1-1-0.dll";                     /*1*/
        internal const string CreateSymbolicLinkDllName = "api-ms-win-core-file-l2-1-0.dll";                 /*2*/
        internal const string GetOEMCPDllName = "api-ms-win-core-localization-l1-2-0.dll";                   /*3*/
        internal const string DeviceIoControlDllName = "api-ms-win-core-io-l1-1-0.dll";                      /*4*/
        internal const string CreateFileDllName = "api-ms-win-core-file-l1-1-0.dll";                         /*5*/
        internal const string DeleteFileDllName = "api-ms-win-core-file-l1-1-0.dll";                         /*6*/
        internal const string FindCloseDllName = "api-ms-win-core-file-l1-1-0.dll";                          /*7*/
        internal const string GetFileAttributesDllName = "api-ms-win-core-file-l1-1-0.dll";                  /*8*/
        internal const string FindFirstFileDllName = "api-ms-win-core-file-l1-1-0.dll";                      /*9*/
        internal const string FindNextFileDllName = "api-ms-win-core-file-l1-1-0.dll";                       /*10*/
        internal const string RegEnumValueDllName = "api-ms-win-core-registry-l1-1-0.dll";                   /*11*/
        internal const string RegOpenKeyExDllName = "api-ms-win-core-registry-l1-1-0.dll";                   /*12*/
        internal const string RegOpenKeyTransactedDllName = "api-ms-win-core-registry-l2-1-0.dll";           /*13*/
        internal const string RegQueryInfoKeyDllName = "api-ms-win-core-registry-l1-1-0.dll";                /*14*/
        internal const string RegQueryValueExDllName = "api-ms-win-core-registry-l2-1-0.dll";                /*15*/
        internal const string RegSetValueExDllName = "api-ms-win-core-registry-l1-1-0.dll";                  /*16*/
        internal const string RegCreateKeyTransactedDllName = "api-ms-win-core-registry-l2-1-0.dll";         /*17*/
        internal const string CryptGenKeyDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";               /*18*/
        internal const string CryptDestroyKeyDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";           /*19*/
        internal const string CryptAcquireContextDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";       /*20*/
        internal const string CryptReleaseContextDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";       /*21*/
        internal const string CryptEncryptDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";              /*22*/
        internal const string CryptDecryptDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";              /*23*/
        internal const string CryptExportKeyDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";            /*24*/
        internal const string CryptImportKeyDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";            /*25*/
        internal const string CryptDuplicateKeyDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";         /*26*/
        internal const string GetLastErrorDllName = "api-ms-win-core-errorhandling-l1-1-0.dll";              /*27*/
        internal const string GetCPInfoDllName = "api-ms-win-core-localization-l1-2-0.dll";                  /*28*/
        internal const string CommandLineToArgvDllName = "api-ms-win-downlevel-shell32-l1-1-0.dll";          /*30*/
        internal const string LocalFreeDllName = "api-ms-win-core-misc-l1-1-0.dll";                          /*31*/
        internal const string CloseHandleDllName = "api-ms-win-core-handle-l1-1-0.dll";                      /*32*/
        internal const string GetTokenInformationDllName = "api-ms-win-security-base-l1-1-0.dll";            /*33*/
        internal const string LookupAccountSidDllName = "api-ms-win-security-lsalookup-l2-1-0.dll";          /*34*/
        internal const string OpenProcessTokenDllName = "api-ms-win-core-processsecurity-l1-1-0.dll";        /*35*/
        internal const string DosDateTimeToFileTimeDllName = "api-ms-win-core-kernel32-legacy-l1-1-0.dll";   /*36*/
        internal const string LocalFileTimeToFileTimeDllName = "api-ms-win-core-file-l1-1-0.dll";            /*37*/
        internal const string SetFileTimeDllName = "api-ms-win-core-file-l1-1-0.dll";                        /*38*/
        internal const string SetFileAttributesWDllName = "api-ms-win-core-file-l1-1-0.dll";                 /*39*/
        internal const string CreateHardLinkDllName = "api-ms-win-core-file-l2-1-0.dll";                     /*40*/
        internal const string RegCloseKeyDllName = "api-ms-win-core-registry-l1-1-0.dll";                    /*41*/
        internal const string GetFileInformationByHandleDllName = "api-ms-win-core-file-l1-1-0.dll";         /*42*/
        internal const string FindFirstStreamDllName = "api-ms-win-core-file-l1-2-2.dll";                    /*43*/
        internal const string FindNextStreamDllName = "api-ms-win-core-file-l1-2-2.dll";                     /*44*/
        internal const string GetSystemInfoDllName = "api-ms-win-core-sysinfo-l1-1-0.dll";                   /*45*/
        internal const string GetCurrentThreadIdDllName = "api-ms-win-core-processthreads-l1-1-0.dll";       /*46*/
        internal const string SetLocalTimeDllName = "api-ms-win-core-sysinfo-l1-1-0.dll";                    /*47*/
        internal const string CryptSetProvParamDllName = "api-ms-win-security-cryptoapi-l1-1-0.dll";         /*48*/
        internal const string GetNamedSecurityInfoDllName = "api-ms-win-security-provider-l1-1-0.dll";       /*49*/
        internal const string SetNamedSecurityInfoDllName = "api-ms-win-security-provider-l1-1-0.dll";       /*50*/
        internal const string ConvertStringSidToSidDllName = "api-ms-win-security-sddl-l1-1-0.dll";          /*51*/
        internal const string IsValidSidDllName = "api-ms-win-security-base-l1-1-0.dll";                     /*52*/
        internal const string GetLengthSidDllName = "api-ms-win-security-base-l1-1-0.dll";                   /*53*/
        internal const string LsaFreeMemoryDllName = "api-ms-win-security-lsapolicy-l1-1-0.dll";             /*54*/
        internal const string InitializeAclDllName = "api-ms-win-security-base-l1-1-0.dll";                  /*55*/
        internal const string GetCurrentProcessDllName = "api-ms-win-core-processthreads-l1-1-0.dll";        /*56*/
        internal const string GetCurrentThreadDllName = "api-ms-win-core-processthreads-l1-1-0.dll";         /*57*/
        internal const string OpenThreadTokenDllName = "api-ms-win-core-processthreads-l1-1-0.dll";          /*58*/
        internal const string LookupPrivilegeValueDllName = "api-ms-win-security-lsalookup-l2-1-0.dll";      /*59*/
        internal const string AdjustTokenPrivilegesDllName = "api-ms-win-security-base-l1-1-0.dll";          /*60*/
        internal const string GetStdHandleDllName = "api-ms-win-core-processenvironment-l1-1-0.dll";         /*61*/
        internal const string CreateProcessWithLogonWDllName = "api-ms-win-security-cpwl-l1-1-0.dll";        /*62*/
        internal const string CreateProcessDllName = "api-ms-win-core-processthreads-l1-1-0.dll";            /*63*/
        internal const string ResumeThreadDllName = "api-ms-win-core-processthreads-l1-1-0.dll";             /*64*/
        internal const string OpenSCManagerWDllName = "api-ms-win-service-management-l1-1-0.dll";            /*65*/
        internal const string OpenServiceWDllName = "api-ms-win-service-management-l1-1-0.dll";              /*66*/
        internal const string CloseServiceHandleDllName = "api-ms-win-service-management-l1-1-0.dll";        /*67*/
        internal const string ChangeServiceConfigWDllName = "api-ms-win-service-management-l2-1-0.dll";      /*68*/
        internal const string ChangeServiceConfig2WDllName = "api-ms-win-service-management-l2-1-0.dll";     /*69*/
        internal const string CreateServiceWDllName = "api-ms-win-service-management-l1-1-0.dll";            /*70*/
        internal const string CreateJobObjectDllName = "api-ms-win-core-job-l2-1-0.dll";                     /*71*/
        internal const string AssignProcessToJobObjectDllName = "api-ms-win-core-job-l2-1-0.dll";            /*72*/
        internal const string QueryInformationJobObjectDllName = "api-ms-win-core-job-l2-1-0.dll";           /*73*/
        internal const string CreateNamedPipeDllName = "api-ms-win-core-namedpipe-l1-1-0.dll";               /*74*/
        internal const string WaitNamedPipeDllName = "api-ms-win-core-namedpipe-l1-1-0.dll";                 /*75*/
        internal const string PrivilegeCheckDllName = "api-ms-win-security-base-l1-1-0.dll";                 /*76*/
        internal const string ImpersonateNamedPipeClientDllName = "api-ms-win-core-namedpipe-l1-1-0.dll";    /*77*/
        internal const string RevertToSelfDllName = "api-ms-win-security-base-l1-1-0.dll";                   /*78*/
        internal const string CreateProcessInComputeSystemDllName = "vmcompute.dll";                         /*79*/
        internal const string CLSIDFromProgIDDllName = "api-ms-win-core-com-l1-1-0.dll";                     /*80*/
        internal const string LoadLibraryEx = "api-ms-win-core-libraryloader-l1-1-0.dll";                    /*81*/
        internal const string FreeLibrary = "api-ms-win-core-libraryloader-l1-1-0.dll";                      /*82*/
        internal const string EventActivityIdControlDllName = "api-ms-win-eventing-provider-l1-1-0.dll";     /*83*/
        internal const string GetConsoleCPDllName = "api-ms-win-core-console-l1-1-0.dll";                    /*84*/
        internal const string GetConsoleOutputCPDllName = "api-ms-win-core-console-l1-1-0.dll";              /*85*/
        internal const string GetConsoleWindowDllName = "api-ms-win-core-kernel32-legacy-l1-1-0.dll";        /*86*/
        internal const string GetDCDllName = "ext-ms-win-ntuser-dc-access-ext-l1-1-0.dll";                   /*87*/
        internal const string ReleaseDCDllName = "ext-ms-win-ntuser-dc-access-ext-l1-1-0.dll";               /*88*/
        internal const string TranslateCharsetInfoDllName = "ext-ms-win-gdi-font-l1-1-1.dll";                /*89*/
        internal const string GetTextMetricsDllName = "ext-ms-win-gdi-font-l1-1-1.dll";                      /*90*/
        internal const string GetCharWidth32DllName = "ext-ms-win-gdi-font-l1-1-1.dll";                      /*91*/
        internal const string FlushConsoleInputBufferDllName = "api-ms-win-core-console-l2-1-0.dll";         /*92*/
        internal const string FillConsoleOutputAttributeDllName = "api-ms-win-core-console-l2-1-0.dll";      /*93*/
        internal const string FillConsoleOutputCharacterDllName = "api-ms-win-core-console-l2-1-0.dll";      /*94*/
        internal const string WriteConsoleDllName = "api-ms-win-core-console-l1-1-0.dll";                    /*95*/
        internal const string GetConsoleTitleDllName = "api-ms-win-core-console-l2-1-0.dll";                 /*96*/
        internal const string SetConsoleTitleDllName = "api-ms-win-core-console-l2-1-0.dll";                 /*97*/
        internal const string GetConsoleModeDllName = "api-ms-win-core-console-l1-1-0.dll";                  /*98*/
        internal const string GetConsoleScreenBufferInfoDllName = "api-ms-win-core-console-l2-1-0.dll";      /*99*/
        internal const string GetFileTypeDllName = "api-ms-win-core-file-l1-1-0.dll";                        /*100*/
        internal const string GetLargestConsoleWindowSizeDllName = "api-ms-win-core-console-l2-1-0.dll";     /*101*/
        internal const string ReadConsoleDllName = "api-ms-win-core-console-l1-1-0.dll";                     /*102*/
        internal const string PeekConsoleInputDllName = "api-ms-win-core-console-l2-1-0.dll";                /*103*/
        internal const string GetNumberOfConsoleInputEventsDllName = "api-ms-win-core-console-l1-1-0.dll";   /*104*/
        internal const string SetConsoleCtrlHandlerDllName = "api-ms-win-core-console-l1-1-0.dll";           /*105*/
        internal const string SetConsoleModeDllName = "api-ms-win-core-console-l1-1-0.dll";                  /*106*/
        internal const string SetConsoleScreenBufferSizeDllName = "api-ms-win-core-console-l2-1-0.dll";      /*107*/
        internal const string SetConsoleTextAttributeDllName = "api-ms-win-core-console-l2-1-0.dll";         /*108*/
        internal const string SetConsoleWindowInfoDllName = "api-ms-win-core-console-l2-1-0.dll";            /*109*/
        internal const string WriteConsoleOutputDllName = "api-ms-win-core-console-l2-1-0.dll";              /*110*/
        internal const string ReadConsoleOutputDllName = "api-ms-win-core-console-l2-1-0.dll";               /*111*/
        internal const string ScrollConsoleScreenBufferDllName = "api-ms-win-core-console-l2-1-0.dll";       /*112*/
        internal const string SendInputDllName = "ext-ms-win-ntuser-keyboard-l1-2-1.dll";                    /*113*/
        internal const string GetConsoleCursorInfoDllName = "api-ms-win-core-console-l2-1-0.dll";            /*114*/
        internal const string SetConsoleCursorInfoDllName = "api-ms-win-core-console-l2-1-0.dll";            /*115*/
        internal const string ReadConsoleInputDllName = "api-ms-win-core-console-l1-1-0.dll";                /*116*/
        internal const string GetVersionExDllName = "api-ms-win-core-sysinfo-l1-1-0.dll";                    /*117*/
        internal const string FormatMessageDllName = "api-ms-win-core-localization-l1-2-0.dll";              /*118*/
        internal const string CreateToolhelp32SnapshotDllName = "api-ms-win-core-toolhelp-l1-1-0";           /*119*/
        internal const string Process32FirstDllName = "api-ms-win-core-toolhelp-l1-1-0";                     /*120*/
        internal const string Process32NextDllName = "api-ms-win-core-toolhelp-l1-1-0";                      /*121*/
        internal const string GetACPDllName = "api-ms-win-core-localization-l1-2-0.dll";                     /*122*/
        internal const string DeleteServiceDllName = "api-ms-win-service-management-l1-1-0.dll";             /*123*/
        internal const string QueryServiceConfigDllName = "api-ms-win-service-management-l2-1-0.dll";        /*124*/
        internal const string QueryServiceConfig2DllName = "api-ms-win-service-management-l2-1-0.dll";       /*125*/
        internal const string SetServiceObjectSecurityDllName = "api-ms-win-service-management-l2-1-0.dll";  /*126*/
    }
}
