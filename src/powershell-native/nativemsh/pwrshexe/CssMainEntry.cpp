// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
//  File:      CoreCLRHost.cpp
//
//  Contents:  Unmanaged startup point for powershell.exe console app.
//
// ----------------------------------------------------------------------

#include <windows.h>
#include <string>
#include <stdio.h>
#if !CORECLR
  #include "mscoree.h"
#endif
#include "NativeMsh.h"
#include "ClrHostWrapper.h"
#include "OutputWriter.h"
#include "ConfigFileReader.h"
#include "WinSystemCallFacade.h"

namespace NativeMsh
{
    // Define the function pointer for the powershell entry point.
    typedef int (STDMETHODCALLTYPE *MonadRunHelperFp)(LPCWSTR consoleFilePath, LPCWSTR * args, int argc); // int UnmanagedPSEntry.Start(string consoleFilePath, string[] args, int argc)

    bool TryRun(const int argc, const wchar_t* argv[], HostEnvironment &hostEnvironment, int &exitCode, bool verbose)
    {
        // Assume failure
        exitCode = -1;

        // All these objects will be destroyed when commonFuncs goes out of scope.
        PwrshExeOutput* output = new PwrshExeOutput();
        PwrshCommon commonFuncs(output, new ConfigFileReader(), new WinSystemCallFacade());
        CoreClrHostingApiWrapper hostWrapper;

        exitCode = commonFuncs.LaunchCoreCLR(&hostWrapper, hostEnvironment, "powershell");
        if (EXIT_CODE_SUCCESS != exitCode)
        {
            if (verbose)
                ::wprintf(L"Unable to launch CoreCLR\n");
            return false;
        }

        if (!hostWrapper.IsInitialized()) // TODO: redundant?
        {
            if (verbose)
                ::wprintf(L"Unable to initialize CoreCLR\n");
            return false;
        }

        //-------------------------------------------------------------
        // Start the assembly
        MonadRunHelperFp pfnDelegate = NULL;
        HRESULT hr = hostWrapper.CreateDelegate(
            "Microsoft.PowerShell.ConsoleHost, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
            "Microsoft.PowerShell.UnmanagedPSEntry",
            "Start",
            (void**)&pfnDelegate);

        if (FAILED(hr))
        {
            output->DisplayMessage(false, g_CREATING_MSH_ENTRANCE_FAILED, hr);
        }
        else
        {
            exitCode = pfnDelegate(NULL, &argv[0], argc);
        }

        // Unload the AppDomain
        if (verbose)
            ::wprintf(L"Unloading the AppDomain\n");

        int utilExitCode = hostWrapper.CleanUpHostWrapper();
        if (EXIT_CODE_SUCCESS != utilExitCode)
        {
            if (verbose)
            {
                if (g_UNLOAD_APPDOMAIN_FAILED == utilExitCode)
                {
                    // TODO: If we want localization, these must be added to nativemsh.mc
                    ::wprintf(L"Failed to unload the AppDomain. ERRORCODE: %u\n", utilExitCode);
                }
                else if (g_STOP_CLR_HOST_FAILED == utilExitCode)
                {
                    ::wprintf(L"Failed to stop the host. ERRORCODE: %u\n", utilExitCode);
                }
                else if (g_RELEASE_CLR_HOST_FAILED == utilExitCode)
                {
                    ::wprintf(L"Failed to release the host.ERRORCODE: %u\n", utilExitCode);
                }
            }
            return false;
        }

        return true;
    }

    void showHelp() {
        static PCWSTR coreCLRInstallDirectory = L"%windir%\\system32\\DotNetCore\\v1.0\\";
        ::wprintf(
            L"USAGE: powershell [-Verbose] [-Debug] [-File <filePath> <args>] [-Command] <CommandLine>\r\n"
            L"\r\n"
            L"  CoreCLR is searched for in the directory that powershell.exe is in,\r\n"
            L"  then in %s.\r\n",
            coreCLRInstallDirectory
            );
    }

} // namespace NativeMsh

int __cdecl wmain(const int argc, const wchar_t* argv[])
{
    // Parse the options from the command line
    const wchar_t *verboseParameter = L"-Verbose";
    const wchar_t *debugParameter = L"-Debug";

    const wchar_t *versionParameter = L"-Version";
    const wchar_t *psConsoleFileParameter = L"-PSConsoleFile";
    const wchar_t *runtimeVersionParameter = L"-RuntimeVersion";

    bool verbose = false;
    bool debug = false;
    bool helpRequested = false;

    bool versionSpecified = false;
    bool psConsoleFileSpecified = false;
    bool runtimeVersionSpecified = false;


    int newArgc = argc - 1;
    const wchar_t **newArgv = argv + 1;

    auto stringsMatch = [](const wchar_t * const userInput, const wchar_t * const parameter) -> bool {
        size_t userInputLength = ::wcslen(userInput);
        size_t parameterLength = ::wcslen(parameter);

        // If the user input is longer than the parameter, they cannot be matched.
        if (userInputLength > parameterLength) {
            return false;
        }

        // Return true if the user input is a prefix of the parameter
        return ::_wcsnicmp(userInput, parameter, userInputLength) == 0;
    };

    auto stringsEqual = [](const wchar_t * const a, const wchar_t * const b) -> bool {
        return ::_wcsicmp(a, b) == 0;
    };

    // These parameters are considered 'native only' - they should NOT be passed to managed layer
    auto skipParameter = [&](const wchar_t * arg) -> bool {
        if (stringsMatch(arg, verboseParameter)) {
            verbose = true;
        }
        else if (stringsEqual(arg, L"/?") || stringsEqual(arg, L"-?")) {
            helpRequested = true;
        }
        else if (stringsMatch(arg, debugParameter))
        {
            debug = true;
        }
        else if (stringsEqual(arg, versionParameter))
        {
            versionSpecified = true;
        }
        else if (stringsMatch(arg, psConsoleFileParameter))
        {
            psConsoleFileSpecified = true;
        }
        else if (stringsMatch(arg, runtimeVersionParameter)) {
            runtimeVersionSpecified = true;
        }
        else {
            return false;
        }

        return true;
    };

    // Find the last 'native' parameter, and chomp everything up until it.
    while (newArgc > 0 && skipParameter(newArgv[0]))
    {
        newArgc--;
        newArgv++;

        if (versionSpecified) {
            // skip the next arg if there is one
            versionSpecified = false;
            if (newArgc > 0) {
                newArgc--;
                newArgv++;
            }
        }

        if (psConsoleFileSpecified) {
            // skip the next arg if there is one
            psConsoleFileSpecified = false;
            if (newArgc > 0) {
                newArgc--;
                newArgv++;
            }
        }

        if (runtimeVersionSpecified) {
            // skip the next arg if there is one
            runtimeVersionSpecified = false;
            if (newArgc > 0) {
                newArgc--;
                newArgv++;
            }
        }
    }

    if (debug)
    {
        ::wprintf(L"  Attach the debugger to powershell.exe and press any key to continue\n");
        ::getchar();
    }

    if (helpRequested)
    {
        NativeMsh::showHelp();
        return -1;
    }
    else
    {
        int exitCode;
        NativeMsh::HostEnvironment hostEnvironment;

        auto success = TryRun(newArgc, newArgv, hostEnvironment, exitCode, verbose);

        if (verbose)
            ::wprintf(L"Execution %s\n", (success ? L"succeeded" : L"failed"));
        return exitCode;
    }
}
