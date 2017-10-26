// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
//  File:      NativeMsh.h
//
//  Contents:  common code required to start powershell (exe and plugin)
//
// ----------------------------------------------------------------------

#pragma once

#include <stdio.h>
#include <strsafe.h>
#include <assert.h>
#include "NativeMshConstants.h"
#include "ClrHostWrapper.h"
#include "SystemCallFacade.h"
#include "ConfigFileReader.h"
#include "IPwrshCommonOutput.h"

#if !CORECLR
#include <mscoree.h>
#endif

namespace NativeMsh
{
    class PwrshCommon
    {
    private:
        IPwrshCommonOutput* output;
        ConfigFileReader* reader;
        SystemCallFacade* sysCalls;

    public:
        // Provides default implementations of all dependencies
        PwrshCommon();

        // Allows users to override the default dependency objects with a specific implementations.
        //
        // Note: This object deletes the pointers that are provided, so clients should
        // allocate them with "new".
        PwrshCommon(
            IPwrshCommonOutput* outObj,     // Enables clients to specify how error messages are displayed or suppressed
            ConfigFileReader* rdr,          // Enables specification of how the config file is read.
            SystemCallFacade* systemCalls); // Wraps all calls to Windows APIs to allow for dependency injection via unit test

        ~PwrshCommon();

        //
        // The following functions are used by the components that link to this lib
        //
        bool StringIsNullOrEmpty(
            LPCWSTR wsz);

        // TODO: This is static because it is needed by the EXE's output object. Design wise, it is a little awkward to do this, but it resolve the circular reference and circular initialization issue
        static DWORD GetSystemErrorMessage(
            IN LONG lErrorCode,
            __deref_out_opt PWSTR * pwszErrorMessage);

        bool VerifyMonadVersionFormat(
            LPCWSTR wszMonadVersion,
            int * lpMajorVersion,
            int * lpMinorVersion,
            bool bAllowMinorVersion,
            bool bReportError);

        unsigned int OpenEngineRegKey(
            __deref_out_ecount(1) PHKEY phEngineKey,
            __deref_out_opt PWSTR * pwszMshEngineRegKey,
            __deref_out_opt PWSTR * pwszMonadVersion,
            __inout_ecount(1) int* lpMonadMajorVersion);

        // API used to read a particular registry key value from the PowerShellEngine
        // regkey path. For example to read "ApplicationBase" or "ConsoleHostAssemblyName"
        //
        // Note: During successful calls the following values must be freed by the caller:
        //      pwszMonadVersion
        //      pwszRuntimeVersion
        //      pwzsRegKeyValue
        //
        // The caller must take care to check to see if they must be freed during error scenarios
        // because the function may fail after allocating one or more strings.
        //
        unsigned int GetRegistryInfo(
            __deref_out_opt PWSTR * pwszMonadVersion,
            __inout_ecount(1) int * lpMonadMajorVersion,
            int monadMinorVersion,
            __deref_out_opt PWSTR * pwszRuntimeVersion,
            LPCWSTR lpszRegKeyNameToRead,
            __deref_out_opt PWSTR * pwzsRegKeyValue);

        unsigned int GetRegistryInfo(
            __deref_out_opt PWSTR * pwszMonadVersion,
            __inout_ecount(1) int * lpMonadMajorVersion,
            int monadMinorVersion,
            __deref_out_opt PWSTR * pwszRuntimeVersion,
            __deref_out_opt PWSTR * pwszConsoleHostAssemblyName);

        unsigned int LaunchCoreCLR(
            ClrHostWrapper* hostWrapper,
            HostEnvironment& hostEnvironment,
            PCSTR friendlyName);

#if !CORECLR
        // NOTE:
        // This must be ifdef'd out of the CoreCLR build because it uses .NET 1.0
        // types that have been deprecated and removed from mscoree.h.
        //
        // This code may be removed from #if protection once ICorRuntimeHost is
        // upgraded to ICLRRuntimeHost.
        //
        unsigned int LaunchCLR(
            LPCWSTR wszMonadVersion,
            LPCWSTR wszRuntimeVersion,
            __in_ecount(1) ICorRuntimeHost** pCLR);
#endif // !CORECLR

    protected:
        // These methods are exposed as protected here to make them unit testable.
        // Note: I tried hiding them within PwrshCommonFriend in pwrshcommon.cpp, but it made the unit testing difficult.
        // Virtual methods may be overridden in the test code.
        bool ParseInt(
            const WCHAR * pwchStart,
            const WCHAR * pwchEnd,
            int * pInt);

        _Success_(return) bool ExtractFirstVersionComponent(
            LPCWSTR wszVersionString,
            int* lpFirstVersionComponent,
            __deref_out_opt WCHAR** wszRemainingVersionString);

        bool RegOpenKeyWithErrorReport(
            LPCWSTR wszRegPath,
            LPCWSTR wszMonadVersion,
            __out_ecount(1) PHKEY phResult);

        bool FormatStringWithErrorReporting(
            LPCWSTR wszFormat,
            __deref_out_opt PWSTR * pwszResult,
            __out_ecount(1) LPDWORD lpdwLength,
            int errorMessageId,
            ...);

        bool OpenLatestMSHEngineRegistry(
            __out_ecount(1) PHKEY phResult,
            __deref_out_opt PWSTR * pwszMshEngineRegKeyPath,
            __deref_out_opt PWSTR * pwszMonadVersion,
            __out_ecount(1) int * lpMonadMajorVersion);

        bool RegQueryREG_SZValue(
            _In_ HKEY hEngineKey,
            _In_ LPCWSTR wszValueName,
            _In_ LPCWSTR wszMshEngineRegKey,
            __deref_out_opt PWSTR * pwszRegData);

        unsigned int IsEngineRegKeyWithVersionExisting(
            LPCWSTR wszMonadVersion,
            LPCWSTR wszMonadMajorVersion);

        unsigned int OpenEngineRegKeyWithVersion(
            __deref_out_ecount(1) PHKEY phEngineKey,
            __deref_out_opt PWSTR * pwszMshEngineRegKey,
            LPCWSTR wszMonadVersion,
            int monadMajorVersion);

        bool VerifyDOTNetVersionFormat(
            LPCWSTR wszFullVersion,
            __out_ecount(1) int * lpMajorVersion,
            __out_ecount(1) int * lpMinorVersion);

        virtual bool DoesAssemblyExist(
            std::string& fileToTest);

        virtual void ProbeAssembly(
            _In_z_ PCSTR directoryPath,
            _In_z_ PCSTR assemblyName,
            std::string& result);

        virtual void GetTrustedAssemblyList(
            PCSTR coreCLRDirectoryPath,
            std::stringstream& assemblyList,
            bool& listEmpty);

        virtual unsigned int IdentifyHostDirectory(
            HostEnvironment& hostEnvironment);

    private:
        // Explicitly disallow copy construction and assignment
        PwrshCommon(const PwrshCommon&);
        PwrshCommon& operator=(const PwrshCommon&);
    };
} // namespace NativeMsh
