// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (C) Microsoft Corporation, 2014.
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

        virtual DWORD WINAPI GetModuleFileNameW(
            _In_opt_  HMODULE hModule,
            _Out_     PWSTR lpFilename,
            _In_      DWORD nSize) = 0;

        virtual HMODULE WINAPI GetModuleHandleW(
            _In_opt_  PCWSTR lpModuleName) = 0;

        virtual BOOL WINAPI GetModuleHandleExW(
            _In_      DWORD dwFlags,
            _In_opt_  PCWSTR lpModuleName,
            _Out_     HMODULE *phModule) = 0;

        virtual FARPROC WINAPI GetProcAddress(
            _In_  HMODULE hModule,
            _In_  LPCSTR lpProcName) = 0;

        virtual BOOL WINAPI FreeLibrary(
            _In_  HMODULE hModule) = 0;

        // File Manipulation Wrappers
        virtual FILE* _wfopen(
            const wchar_t *filename,
            const wchar_t *mode) = 0;

        virtual int fclose(
            FILE *stream) = 0;
    };

} // namespace NativeMsh