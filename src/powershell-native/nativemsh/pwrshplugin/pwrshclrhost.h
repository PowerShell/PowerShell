// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
//  Contents:  Headers used by pwrshplugin.
//  pwrshplugin is totally unmanaged.
// ----------------------------------------------------------------------

#pragma once

#include "pwrshplugindefs.h" // PwrshPluginWkr_Ptrs and PlugInException
#if !CORECLR
#include <mscoree.h>
#endif
#include <atlbase.h>
#include <string>
#include "SystemCallFacade.h"
#include "ClrHostWrapper.h"
#include "NativeMsh.h"

class PwrshPluginOutputDefault : public NativeMsh::IPwrshCommonOutput
{
public:
    virtual VOID DisplayMessage(
        bool bUseStdOut,
        DWORD dwMessageId,
        ...)
    {
        return;
    }

    virtual void DisplayErrorWithSystemError(
        LONG lSystemErrorCode,
        int messageId,
        LPCWSTR insertionParam)
    {
        return;
    }
};

class IPowerShellClrHost
{
public:
    // Virtual destructor to ensure that derived destructors are called
    // during base class destruction.
    virtual ~IPowerShellClrHost() {}

    virtual unsigned int LaunchClr(
        _In_ LPCWSTR wszMonadVersion,
        _In_ LPCWSTR wszRuntimeVersion,
        _In_ LPCSTR friendlyName) = 0;

    virtual unsigned int LoadWorkerCallbackPtrs(
        _In_ PwrshPluginWkr_Ptrs* workerCallbackPtrs,
        _In_z_ PWSTR wszMgdPlugInFileName,
        _Outptr_result_maybenull_ PlugInException** pPluginException) = 0;
};

#if !CORECLR // See note for LaunchCLR for an explanation.

class PowerShellClrWorker : public IPowerShellClrHost
{
private:
    CComPtr<ICorRuntimeHost> pHost;
    HMODULE hManagedPluginModule;
    NativeMsh::IPwrshCommonOutput* output;
    NativeMsh::PwrshCommon commonLib;

    const LPCSTR g_INIT_PLUGIN;
    const LPCSTR g_SHUTDOWN_PLUGIN;
    const LPCSTR g_MANAGED_PLUGIN_CREATE_SHELL;
    const LPCSTR g_MANAGED_PLUGIN_RELEASE_SHELL;
    const LPCSTR g_MANAGED_PLUGIN_CREATE_COMMAND;
    const LPCSTR g_MANAGED_PLUGIN_RELEASE_COMMAND;
    const LPCSTR g_MANAGED_PLUGIN_SEND;
    const LPCSTR g_MANAGED_PLUGIN_RECEIVE;
    const LPCSTR g_MANAGED_PLUGIN_SIGNAL;
    const LPCSTR g_MANAGED_PLUGIN_CONNECT;

    NativeMsh::SystemCallFacade* systemCalls;

public:
    PowerShellClrWorker();
    PowerShellClrWorker(NativeMsh::SystemCallFacade* sysCalls);

    virtual ~PowerShellClrWorker();

    //
    // IPowerShellClrHost Methods
    //
    virtual unsigned int LaunchClr(
        _In_ LPCWSTR wszMonadVersion,
        _In_ LPCWSTR wszRuntimeVersion,
        _In_ LPCSTR friendlyName);

    virtual unsigned int LoadWorkerCallbackPtrs(
        _In_ PwrshPluginWkr_Ptrs* workerCallbackPtrs,
        _In_z_ wchar_t* wszMgdPlugInFileName,
        _Outptr_result_maybenull_ PlugInException** pPluginException);
};

class PowerShellClrManagedWorker : public IPowerShellClrHost
{
private:
    CComPtr<ICorRuntimeHost> pHost;
    NativeMsh::IPwrshCommonOutput* output;
    NativeMsh::PwrshCommon commonLib;

public:
    PowerShellClrManagedWorker() : pHost(NULL), output(new PwrshPluginOutputDefault())
    {}

    virtual ~PowerShellClrManagedWorker();

    //
    // IPowerShellClrHost Methods
    //
    virtual unsigned int LaunchClr(
        _In_ LPCWSTR wszMonadVersion,
        _In_ LPCWSTR wszRuntimeVersion,
        _In_ LPCSTR friendlyName);

    virtual unsigned int LoadWorkerCallbackPtrs(
        _In_ PwrshPluginWkr_Ptrs* workerCallbackPtrs,
        _In_z_ wchar_t* wszMgdPlugInFileName,
        _Outptr_result_maybenull_ PlugInException** pPluginException);
};

#else // CORECLR

class PowerShellCoreClrWorker : public IPowerShellClrHost
{
private:
    NativeMsh::ClrHostWrapper* hostWrapper;
    NativeMsh::SystemCallFacade* systemCalls;
    NativeMsh::PwrshCommon* commonLib;
    NativeMsh::IPwrshCommonOutput* output;
    NativeMsh::HostEnvironment hostEnvironment;

public:
    PowerShellCoreClrWorker();

    PowerShellCoreClrWorker(
        NativeMsh::SystemCallFacade* sysCalls,
        NativeMsh::ClrHostWrapper* hstWrp,
        NativeMsh::PwrshCommon* cmnLib);

    virtual ~PowerShellCoreClrWorker();

    std::wstring GetHostDirectory() { return std::wstring(hostEnvironment.GetHostDirectoryPathW()); }
    std::wstring GetClrDirectory() { return std::wstring(hostEnvironment.GetCoreCLRDirectoryPathW()); }

    //
    // IPowerShellClrHost Methods
    //
    virtual unsigned int LaunchClr(
        _In_ LPCWSTR wszMonadVersion,
        _In_ LPCWSTR wszRuntimeVersion,
        _In_ LPCSTR friendlyName);

    virtual unsigned int LoadWorkerCallbackPtrs(
        _In_ PwrshPluginWkr_Ptrs* workerCallbackPtrs,
        _In_z_ PWSTR wszMgdPlugInFileName,
        _Outptr_result_maybenull_ PlugInException** pPluginException);
};

#endif // CORECLR

IPowerShellClrHost* PowerShellClrWorkerFactory(
    _In_ PWSTR wszMgdPlugInFileName);
