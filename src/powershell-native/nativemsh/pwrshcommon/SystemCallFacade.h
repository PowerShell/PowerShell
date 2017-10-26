// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
//  File:      SystemCallFacade.h
//
//  Contents:  Facade for Windows API system calls
//
// ----------------------------------------------------------------------

#pragma once

#include <stdio.h>
#include <Windows.h>

namespace NativeMsh
{
    //
    // Abstract class to abstract system call operations so that they can be
    // replaced with test code during test case execution.
    //
    class SystemCallFacade
    {
    public:
        SystemCallFacade() {}
        virtual ~SystemCallFacade() {}

        // Module Manipulation Wrappers
        virtual HMODULE WINAPI LoadLibraryExW(
            _In_        PCWSTR lpFileName,
            _Reserved_  HANDLE hFile,
            _In_        DWORD dwFlags) = 0;

        virtual DWORD WINAPI GetModuleFileNameA(
            _In_opt_  HMODULE hModule,
            _Out_     LPSTR lpFilename,
            _In_      DWORD nSize) = 0;

        virtual HMODULE WINAPI GetModuleHandleA(
            _In_opt_  PCSTR lpModuleName) = 0;

        virtual FARPROC WINAPI GetProcAddress(
            _In_  HMODULE hModule,
            _In_  LPCSTR lpProcName) = 0;

        virtual BOOL WINAPI FreeLibrary(
            _In_  HMODULE hModule) = 0;

        // File Manipulation Wrappers
        virtual errno_t fopen_s(
            FILE** file,
            const char *filename,
            const char *mode) = 0;

        virtual int fclose(
            FILE *stream) = 0;
    };

} // namespace NativeMsh
