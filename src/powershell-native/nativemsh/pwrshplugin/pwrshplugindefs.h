// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (C) Microsoft Corporation, 2007.
//
//  Contents:  Headers used by pwrshplugin. 
//  pwrshplugin is totally unmanaged.
// ----------------------------------------------------------------------

#pragma once

#include <windows.h>
#include <ntverp.h>

// include wsman.h header file..it is required to declare the version
// number of the wsman API to use
#define WSMAN_API_VERSION_1_0
#include <wsman.h>

// plugin errors start from 1000
const int g_NULL_PLUGIN_CONTEXT = 1100;
const int g_CREATION_FAILED = 1101;
const int g_MANAGED_PLUGIN_ALREADY_LOADED = 1102;
const int g_MANAGED_PLUGIN_PATH_CONSTRUCTION_ERROR = 1103;
const int g_MANAGED_PLUGIN_LOAD_FAILED = 1104;

const int g_SHELL_CREATION_FAILED = 1201;
const int g_OPTION_SET_MAJOR_VERSION_NOT_MATCH = 1202;
const int g_OPTION_SET_CLR_VERSION_NOT_MATCH = 1203;
const int g_OPTION_SET_APP_BASE_NOT_MATCH = 1204;

const PCWSTR g_VERSION_OPTION_STRING = L"version";
const PCWSTR g_PSPLUGINWKRV3_REGISTRY_KEY = L"PSPluginWkrModuleName";
const PCWSTR g_MANAGED_PLUGIN_FILENAME_STRING = L"pspluginwkr.dll";
const PCWSTR g_MANAGED_PLUGIN_FILENAME_V3_STRING = L"system.management.automation.dll";
const PCWSTR g_PSVERSION_CONFIG = L"PSVersion";
const PCWSTR g_INITIALIZATIONPARAM_CONFIG = L"InitializationParameters";

// The following VER_ macros are defined in ntverp.h
const PCWSTR g_BUILD_VERSION = LVER_PRODUCTVERSION_STR;

// Managed and mixed-mode plugin worker method entrypoints
//
// The v3 version of the worker is a mixed-mode managed C++ DLL, so it can
// be accessed directly using GetProcAddress.
// The CoreCLR-compliant worker is included in System.Management.Automation.dll,
// so we have to use alternate means to acquire the function pointers.
// Once we have the pointers, they may be used in the same way regardless of the
// method used to obtain them.

typedef void (WINAPI *ShutdownPluginFuncPtr)(__in PVOID pluginContext);

typedef void (WINAPI *WSManPluginShellFuncPtr)(
    __in PVOID pluginContext,
    __in WSMAN_PLUGIN_REQUEST *requestDetails,
    __in DWORD flags,
    __in PCWSTR extraInfo,
    __in_opt WSMAN_SHELL_STARTUP_INFO *startupInfo,
    __in_opt WSMAN_DATA *inboundShellInformation
    );

typedef void (WINAPI *WSManPluginConnectFuncPtr)(
    __in PVOID pluginContext,
    __in WSMAN_PLUGIN_REQUEST *requestDetails,
    __in DWORD flags,
    __in PVOID shellContext,
    __in_opt PVOID commandContext,
    __in_opt WSMAN_DATA *inboundConnectInformation
    );

typedef void (WINAPI *WSManPluginReleaseShellContextFuncPtr)(
    __in PVOID pluginContext,
    __in PVOID shellContext
    );

typedef void (WINAPI *WSManPluginCommandFuncPtr)(
    __in PVOID pluginContext,
    __in WSMAN_PLUGIN_REQUEST *requestDetails,
    __in DWORD flags,
    __in PVOID shellContext,
    __in PCWSTR commandLine,
    __in_opt WSMAN_COMMAND_ARG_SET *arguments
    );

typedef void (WINAPI *WSManPluginReleaseCommandContextFuncPtr)(
    __in PVOID pluginContext,
    __in PVOID shellContext,
    __in PVOID commandContext
    );

typedef void (WINAPI *WSManPluginSendFuncPtr)(
    __in PVOID pluginContext,
    __in WSMAN_PLUGIN_REQUEST *requestDetails,
    __in DWORD flags,
    __in PVOID shellContext,
    __in_opt PVOID commandContext,
    __in PCWSTR stream,
    __in WSMAN_DATA *inboundData
    );

typedef void (WINAPI *WSManPluginReceiveFuncPtr)(
    __in PVOID pluginContext,
    __in WSMAN_PLUGIN_REQUEST *requestDetails,
    __in DWORD flags,
    __in PVOID shellContext,
    __in_opt PVOID commandContext,
    __in_opt WSMAN_STREAM_ID_SET* streamSet
    ); 

typedef void (WINAPI *WSManPluginSignalFuncPtr)(
    __in PVOID pluginContext,
    __in WSMAN_PLUGIN_REQUEST *requestDetails,
    __in DWORD flags,
    __in PVOID shellContext,
    __in_opt PVOID commandContext,
    __in PCWSTR code);

typedef void (WINAPI *WSManPluginOperationShutdownFuncPtr)(
    __in PVOID pluginContext);

typedef struct _PwrshPluginWkr_Ptrs
{
    ShutdownPluginFuncPtr shutdownPluginFuncPtr;
    WSManPluginShellFuncPtr wsManPluginShellFuncPtr;
    WSManPluginReleaseShellContextFuncPtr wsManPluginReleaseShellContextFuncPtr;
    WSManPluginCommandFuncPtr wsManPluginCommandFuncPtr;
    WSManPluginReleaseCommandContextFuncPtr wsManPluginReleaseCommandContextFuncPtr;
    WSManPluginSendFuncPtr wsManPluginSendFuncPtr;
    WSManPluginReceiveFuncPtr wsManPluginReceiveFuncPtr;
    WSManPluginSignalFuncPtr wsManPluginSignalFuncPtr;
    WSManPluginConnectFuncPtr wsManPluginConnectFuncPtr;
    WSManPluginOperationShutdownFuncPtr wsmanPluingOperationShutdownFuncPtr; // This ptr is not used in this environment, but is required to keep the memory layout identical between unmanaged and managed code.
} PwrshPluginWkr_Ptrs;

class PlugInException
{
public:
    DWORD dwMessageId;
    // This message is allocated outside the exception, but the exception frees
    // the memory.
    PWSTR extendedErrorInformation;

    PlugInException(DWORD msgId, __in PWSTR msg)
    {
        dwMessageId = msgId;
        extendedErrorInformation = msg;
    }

    ~PlugInException()
    {
        if (NULL != extendedErrorInformation)
        {
            delete[] extendedErrorInformation;
            extendedErrorInformation = NULL;
        }
    }
private:
    // Provided without implementation to prevent automatic instantiation and copying
    // since copying will lead to double frees given the existing code.
    PlugInException();
    PlugInException(const PlugInException&);
    PlugInException& operator=(const PlugInException&);
};
