// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
//  File:      WinSystemCallFacade.h
//
//  Contents:  Wraps the actual API calls for production scenarios.
//
// ----------------------------------------------------------------------

#pragma once

#include "SystemCallFacade.h"

namespace NativeMsh
{
    //
    // Actual implementation of the system calls for use during production.
    //
    class WinSystemCallFacade : public SystemCallFacade
    {
    public:
        WinSystemCallFacade() {}
        virtual ~WinSystemCallFacade() {}

        // Module Manipulation Wrappers
        virtual HMODULE WINAPI LoadLibraryExW(
            _In_        PCWSTR lpFileName,
            _Reserved_  HANDLE hFile,
            _In_        DWORD dwFlags);

        virtual DWORD WINAPI GetModuleFileNameA(
            _In_opt_  HMODULE hModule,
            _Out_     LPSTR lpFilename,
            _In_      DWORD nSize);

        virtual HMODULE WINAPI GetModuleHandleA(
            _In_opt_  LPCSTR lpModuleName);

        virtual FARPROC WINAPI GetProcAddress(
            _In_  HMODULE hModule,
            _In_  LPCSTR lpProcName);

        virtual BOOL WINAPI FreeLibrary(
            _In_  HMODULE hModule);

        // File Manipulation Wrappers
        virtual errno_t fopen_s(
            FILE** file,
            const char *filename,
            const char *mode);

        virtual int fclose(
            FILE *stream);
    };

} // namespace NativeMsh
