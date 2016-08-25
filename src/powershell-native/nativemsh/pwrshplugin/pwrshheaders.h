// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (C) Microsoft Corporation, 2008.
//
//  Contents:  Headers used by internal windows teams to access certain
//  Powershell functionality
// ----------------------------------------------------------------------

#pragma once

// Gets the CLR Version for a given PowerShell Version. PowerShell Version is
// supplied with 2 parameters iPSMajorVersion (PowerShell major version) and
// iPSMinorVersion (PowerShell minor version). The CLR version is returned through
// pwszRuntimeVersion and pRuntimeVersionLength represents the size of pwszRuntimeVersion.
// returns: 0 on success, non-zero on failure.
_Success_(return == 0)  // EXIT_CODE_SUCCESS
extern "C"
unsigned int GetCLRVersionForPSVersion(int iPSMajorVersion, 
                      int iPSMinorVersion,
                      size_t runtimeVersionLength,
                      __inout_ecount_part(runtimeVersionLength , *pRuntimeVersionLength) wchar_t* pwszRuntimeVersion,
                      __out_ecount(1) size_t* pRuntimeVersionLength);
