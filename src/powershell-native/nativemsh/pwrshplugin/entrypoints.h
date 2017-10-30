//  Microsoft Windows NT
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
//  Contents:  Headers used by pwrshplugin.
//  pwrshplugin is totally unmanaged.
// ----------------------------------------------------------------------

#pragma once

#include <windows.h>
#include <wchar.h>
#include "pwrshpluginerrorcodes.h"

const int EXIT_CODE_BAD_INPUT = 100;

extern DWORD GetFormattedErrorMessage(__deref_out PWSTR * pwszErrorMessage, DWORD dwMessageId, ...);
extern unsigned int ConstructPowerShellVersion(int iPSMajorVersion, int iPSMinorVersion, __deref_out_opt PWSTR *pwszMonadVersion);

