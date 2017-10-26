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

#include "WinSystemCallFacade.h"

namespace NativeMsh
{
    HMODULE WINAPI WinSystemCallFacade::LoadLibraryExW(
        _In_        LPCWSTR lpFileName,
        _Reserved_  HANDLE hFile,
        _In_        DWORD dwFlags)
    {
        return ::LoadLibraryExW(lpFileName, hFile, dwFlags);
    }

#pragma prefast(push)
#pragma prefast (disable: 26006) // Possibly incorrect single element annotation on string buffer - This is a thin wrapper around a system call, so it's behavior is not controllable.

    DWORD WINAPI WinSystemCallFacade::GetModuleFileNameA(
        _In_opt_ HMODULE hModule,
        _Out_    LPSTR lpFilename, // _Out_writes_to_(nSize, return +1) OR __out_ecount_part(nSize, return + 1)? __out_ecount(nSize)
        _In_     DWORD nSize)
    {
        return ::GetModuleFileNameA(hModule, lpFilename, nSize);
    }

#pragma prefast(pop)

    HMODULE WINAPI WinSystemCallFacade::GetModuleHandleA(
        _In_opt_  LPCSTR lpModuleName)
    {
        return ::GetModuleHandleA(lpModuleName);
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

    errno_t WinSystemCallFacade::fopen_s(
        FILE** file,
        const char *filename,
        const char *mode)
    {
        return ::fopen_s(file, filename, mode);
    }

    int WinSystemCallFacade::fclose(
        FILE *stream)
    {
        return ::fclose(stream);
    }

} // namespace NativeMsh
