// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (C) Microsoft Corporation, 2014.
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

        virtual DWORD WINAPI GetModuleFileNameW(
            _In_opt_  HMODULE hModule,
            _Out_     PWSTR lpFilename,
            _In_      DWORD nSize);

        virtual HMODULE WINAPI GetModuleHandleW(
            _In_opt_  PCWSTR lpModuleName);

        virtual BOOL WINAPI GetModuleHandleExW(
            _In_      DWORD dwFlags,
            _In_opt_  PCWSTR lpModuleName,
            _Out_     HMODULE *phModule);

        virtual FARPROC WINAPI GetProcAddress(
            _In_  HMODULE hModule,
            _In_  LPCSTR lpProcName);

        virtual BOOL WINAPI FreeLibrary(
            _In_  HMODULE hModule);

        // File Manipulation Wrappers
        virtual FILE* _wfopen(
            const wchar_t *filename,
            const wchar_t *mode);

        virtual int fclose(
            FILE *stream);
    };

} // namespace NativeMsh