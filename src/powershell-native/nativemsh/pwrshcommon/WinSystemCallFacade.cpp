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

#include "WinSystemCallFacade.h"

namespace NativeMsh 
{
    HMODULE WINAPI WinSystemCallFacade::LoadLibraryExW(
        _In_        LPCTSTR lpFileName,
        _Reserved_  HANDLE hFile,
        _In_        DWORD dwFlags)
    {
        return ::LoadLibraryExW(lpFileName, hFile, dwFlags);
    }

#pragma prefast(push)
#pragma prefast (disable: 26006) // Possibly incorrect single element annotation on string buffer - This is a thin wrapper around a system call, so it's behavior is not controllable.

    DWORD WINAPI WinSystemCallFacade::GetModuleFileNameW(
        _In_opt_ HMODULE hModule,
        _Out_    LPTSTR lpFilename, // _Out_writes_to_(nSize, return +1) OR __out_ecount_part(nSize, return + 1)? __out_ecount(nSize)
        _In_     DWORD nSize)
    {
        return ::GetModuleFileNameW(hModule, lpFilename, nSize);
    }

#pragma prefast(pop)

    HMODULE WINAPI WinSystemCallFacade::GetModuleHandleW(
        _In_opt_  LPCTSTR lpModuleName)
    {
        return ::GetModuleHandleW(lpModuleName);
    }

    BOOL WINAPI WinSystemCallFacade::GetModuleHandleExW(
        _In_      DWORD dwFlags,
        _In_opt_  LPCTSTR lpModuleName,
        _Out_     HMODULE *phModule)
    {
        return ::GetModuleHandleExW(dwFlags, lpModuleName, phModule);
    }

    FARPROC WINAPI WinSystemCallFacade::GetProcAddress(
        _In_  HMODULE hModule,
        _In_  LPCSTR lpProcName)
    {
        return ::GetProcAddress(hModule, lpProcName);
    }

    BOOL WINAPI WinSystemCallFacade::FreeLibrary(
        _In_  HMODULE hModule)
    {
        return ::FreeLibrary(hModule);
    }

    FILE* WinSystemCallFacade::_wfopen(
        const wchar_t *filename,
        const wchar_t *mode)
    {
        return ::_wfopen(filename, mode);
    }

    int WinSystemCallFacade::fclose(
        FILE *stream)
    {
        return ::fclose(stream);
    }

} // namespace NativeMsh