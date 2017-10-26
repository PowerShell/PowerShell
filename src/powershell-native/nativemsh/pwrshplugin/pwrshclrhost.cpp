// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
//  Contents:  Source code for abstraction of CLR and worker differences between PowerShell versions.
//  pwrshplugin is totally unmanaged.
// ----------------------------------------------------------------------

#pragma once

#include "pwrshclrhost.h"
#include "NativeMsh.h"
#include "entrypoints.h"
#include "WinSystemCallFacade.h"

#if !CORECLR

// include the tlb for mscorlib for access to the default AppDomain through COM Interop
#import <mscorlib.tlb> raw_interfaces_only high_property_prefixes("_get","_put","_putref")\
    rename("ReportEvent", "CLRReportEvent")

using namespace mscorlib;

#endif

using namespace NativeMsh;

// Init function calls are handled internally here within the context of the mediator singleton
typedef void (WINAPI *InitPluginFuncPtr)(); // Original PS init
typedef DWORD (WINAPI *InitPluginWkrPtrsFuncPtr)(__out PwrshPluginWkr_Ptrs* wkrPtrs); // Updated PS init

#ifdef CORECLR

unsigned int PowerShellCoreClrWorker::LaunchClr(
    _In_ LPCWSTR wszMonadVersion,
    _In_ LPCWSTR wszRuntimeVersion,
    _In_ LPCSTR friendlyName)
{
    return commonLib->LaunchCoreCLR(hostWrapper, hostEnvironment, friendlyName);
}

unsigned int PowerShellCoreClrWorker::LoadWorkerCallbackPtrs(
    _In_ PwrshPluginWkr_Ptrs* workerCallbackPtrs,
    _In_z_ wchar_t* wszMgdPlugInFileName,
    _Outptr_result_maybenull_ PlugInException** pPluginException)
{

    *pPluginException = NULL;

    // Call into powershell entry point
    InitPluginWkrPtrsFuncPtr entryPointDelegate = NULL;

    // Create the function pointer for the managed entry point
    // It must be targeted at a static method in the managed code.
    HRESULT hr = hostWrapper->CreateDelegate(
        "System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
        "System.Management.Automation.Remoting.WSManPluginManagedEntryWrapper",
        "InitPlugin",
        (void**)&entryPointDelegate);

    if (FAILED(hr))
    {
        output->DisplayMessage(false, g_CREATING_MSH_ENTRANCE_FAILED, hr);

        return EXIT_CODE_INIT_FAILURE;
    }

    // Passes empty pointer structure to the function, expects a filled struct if successful
    return entryPointDelegate(workerCallbackPtrs);
}

PowerShellCoreClrWorker::PowerShellCoreClrWorker()
    : systemCalls(new WinSystemCallFacade()),
      hostWrapper(new CoreClrHostingApiWrapper()),
      output(new PwrshPluginOutputDefault()),
      commonLib(new PwrshCommon())
{
}

//
// sysCalls is expected to be new'd by the caller.
// It will be freed in PowerShellCoreClrWorker's destructor.
//
PowerShellCoreClrWorker::PowerShellCoreClrWorker(
    SystemCallFacade* sysCalls,
    ClrHostWrapper* hstWrp,
    PwrshCommon* cmnLib)
    : systemCalls(sysCalls),
      hostWrapper(hstWrp),
      output(new PwrshPluginOutputDefault()),
      commonLib(cmnLib)
{
    if (NULL == systemCalls)
    {
        // Instantiate it even if one is not provided to guarantee that it will
        // always be non-NULL during execution.
        systemCalls = new WinSystemCallFacade();
    }

    if (NULL == hostWrapper)
    {
        // Instantiate it even if one is not provided to guarantee that it will
        // always be non-NULL during execution.
        hostWrapper = new CoreClrHostingApiWrapper();
    }

    if (NULL == commonLib)
    {
        commonLib = new PwrshCommon(new PwrshPluginOutputDefault(), new ConfigFileReader(), new WinSystemCallFacade());
    }
}

PowerShellCoreClrWorker::~PowerShellCoreClrWorker()
{
    unsigned int exitCode = hostWrapper->CleanUpHostWrapper();
    if (EXIT_CODE_SUCCESS != exitCode)
    {
        output->DisplayMessage(false, exitCode);
    }

    if (systemCalls)
    {
        delete systemCalls;
        systemCalls = NULL;
    }

    if (hostWrapper)
    {
        delete hostWrapper;
        hostWrapper = NULL;
    }

    if (output)
    {
        delete output;
        output = NULL;
    }

    if (commonLib)
    {
        delete commonLib;
        commonLib = NULL;
    }
}

#else // !CORECLR

PowerShellClrWorker::PowerShellClrWorker() :
    pHost(NULL),
    hManagedPluginModule(NULL),
    systemCalls(new WinSystemCallFacade()),
    g_INIT_PLUGIN("InitPlugin"),
    g_SHUTDOWN_PLUGIN("ShutdownPlugin"),
    g_MANAGED_PLUGIN_CREATE_SHELL("WSManPluginShell"),
    g_MANAGED_PLUGIN_RELEASE_SHELL("WSManPluginReleaseShellContext"),
    g_MANAGED_PLUGIN_CREATE_COMMAND("WSManPluginCommand"),
    g_MANAGED_PLUGIN_RELEASE_COMMAND("WSManPluginReleaseCommandContext"),
    g_MANAGED_PLUGIN_SEND("WSManPluginSend"),
    g_MANAGED_PLUGIN_RECEIVE("WSManPluginReceive"),
    g_MANAGED_PLUGIN_SIGNAL("WSManPluginSignal"),
    g_MANAGED_PLUGIN_CONNECT("WSManPluginConnect"),
    output(new PwrshPluginOutputDefault())
{}

PowerShellClrWorker::PowerShellClrWorker(
    SystemCallFacade* sysCalls)
    :   pHost(NULL),
        hManagedPluginModule(NULL),
        systemCalls(sysCalls),
        g_INIT_PLUGIN("InitPlugin"),
        g_SHUTDOWN_PLUGIN("ShutdownPlugin"),
        g_MANAGED_PLUGIN_CREATE_SHELL("WSManPluginShell"),
        g_MANAGED_PLUGIN_RELEASE_SHELL("WSManPluginReleaseShellContext"),
        g_MANAGED_PLUGIN_CREATE_COMMAND("WSManPluginCommand"),
        g_MANAGED_PLUGIN_RELEASE_COMMAND("WSManPluginReleaseCommandContext"),
        g_MANAGED_PLUGIN_SEND("WSManPluginSend"),
        g_MANAGED_PLUGIN_RECEIVE("WSManPluginReceive"),
        g_MANAGED_PLUGIN_SIGNAL("WSManPluginSignal"),
        g_MANAGED_PLUGIN_CONNECT("WSManPluginConnect"),
        output(new PwrshPluginOutputDefault())
{
    if (NULL == systemCalls)
    {
        systemCalls = new WinSystemCallFacade();
    }
}

PowerShellClrWorker::~PowerShellClrWorker()
{
    if (NULL != hManagedPluginModule)
    {
        FreeLibrary(hManagedPluginModule);
        hManagedPluginModule = NULL;
    }

    if (output)
    {
        delete output;
        output = NULL;
    }

    if (systemCalls)
    {
        delete systemCalls;
        systemCalls = NULL;
    }
}

unsigned int PowerShellClrWorker::LaunchClr(
    _In_ LPCWSTR wszMonadVersion,
    _In_ LPCWSTR wszRuntimeVersion,
    _In_ LPCSTR friendlyName)
{
    return commonLib.LaunchCLR(wszMonadVersion, wszRuntimeVersion, &pHost);
}

unsigned int PowerShellClrWorker::LoadWorkerCallbackPtrs(
    _In_ PwrshPluginWkr_Ptrs* workerCallbackPtrs,
    _In_z_ wchar_t* wszMgdPlugInFileName,
    _Outptr_result_maybenull_ PlugInException** pPluginException)
{
    *pPluginException = NULL;
#pragma prefast(push)
#pragma prefast (disable: 28752)
    if (wszMgdPlugInFileName)
    {
        hManagedPluginModule = systemCalls->LoadLibraryExW(wszMgdPlugInFileName, NULL, 0);
    }
#pragma prefast(pop)

    unsigned int exitCode = EXIT_CODE_SUCCESS;

    if (NULL == hManagedPluginModule)
    {
        // Get Extended error information.. to know why load failed.
        PWSTR msg = NULL;
        exitCode = g_MANAGED_PLUGIN_LOAD_FAILED;
        commonLib.GetSystemErrorMessage(GetLastError(), &msg);
        *pPluginException = new PlugInException(exitCode, msg);
        return exitCode;
    }

    // Now get the proc address for all the plugin API.
    // Init is handled differently because it is not used in the updated
    // worker module.
    InitPluginFuncPtr hInitPluginMethodAddress = (InitPluginFuncPtr)systemCalls->GetProcAddress(hManagedPluginModule, g_INIT_PLUGIN);

    // This is not strictly necessary (they can be directly assigned), but I am doing this
    // to keep all the code paths common
    workerCallbackPtrs->shutdownPluginFuncPtr = (ShutdownPluginFuncPtr)systemCalls->GetProcAddress(hManagedPluginModule, g_SHUTDOWN_PLUGIN);
    workerCallbackPtrs->wsManPluginShellFuncPtr = (WSManPluginShellFuncPtr)systemCalls->GetProcAddress(hManagedPluginModule, g_MANAGED_PLUGIN_CREATE_SHELL);
    workerCallbackPtrs->wsManPluginReleaseShellContextFuncPtr = (WSManPluginReleaseShellContextFuncPtr)systemCalls->GetProcAddress(hManagedPluginModule, g_MANAGED_PLUGIN_RELEASE_SHELL);
    workerCallbackPtrs->wsManPluginCommandFuncPtr = (WSManPluginCommandFuncPtr)systemCalls->GetProcAddress(hManagedPluginModule, g_MANAGED_PLUGIN_CREATE_COMMAND);
    workerCallbackPtrs->wsManPluginReleaseCommandContextFuncPtr = (WSManPluginReleaseCommandContextFuncPtr)systemCalls->GetProcAddress(hManagedPluginModule, g_MANAGED_PLUGIN_RELEASE_COMMAND);
    workerCallbackPtrs->wsManPluginSendFuncPtr = (WSManPluginSendFuncPtr)systemCalls->GetProcAddress(hManagedPluginModule, g_MANAGED_PLUGIN_SEND);
    workerCallbackPtrs->wsManPluginReceiveFuncPtr = (WSManPluginReceiveFuncPtr)systemCalls->GetProcAddress(hManagedPluginModule, g_MANAGED_PLUGIN_RECEIVE);
    workerCallbackPtrs->wsManPluginSignalFuncPtr = (WSManPluginSignalFuncPtr)systemCalls->GetProcAddress(hManagedPluginModule, g_MANAGED_PLUGIN_SIGNAL);
    workerCallbackPtrs->wsManPluginConnectFuncPtr = (WSManPluginConnectFuncPtr)systemCalls->GetProcAddress(hManagedPluginModule, g_MANAGED_PLUGIN_CONNECT);

    // Initialize the loaded module (pspluginwkr.dll)
    if (hInitPluginMethodAddress)
    {
        // Only the "old" version of the worker requires a stand-alone init call.
        // This object is a member of the PwrshPlugInMediator singleton, so this
        // call is made from that context.
        (hInitPluginMethodAddress)();
    }
    else
    {
        PWSTR msg = NULL;
        exitCode = g_MANAGED_METHOD_RESOLUTION_FAILED;
        GetFormattedErrorMessage(&msg, exitCode);
        *pPluginException = new PlugInException(exitCode, msg);
        return exitCode;
    }

    return exitCode;
}

unsigned int PowerShellClrManagedWorker::LaunchClr(
    _In_ LPCWSTR wszMonadVersion,
    _In_ LPCWSTR wszRuntimeVersion,
    _In_ LPCSTR friendlyName)
{
    return commonLib.LaunchCLR(wszMonadVersion, wszRuntimeVersion, &pHost);
}

unsigned int PowerShellClrManagedWorker::LoadWorkerCallbackPtrs(
    _In_ PwrshPluginWkr_Ptrs* workerCallbackPtrs,
    _In_z_ wchar_t* wszMgdPlugInFileName,
    _Outptr_result_maybenull_ PlugInException** pPluginException)
{
    unsigned int exitCode = EXIT_CODE_SUCCESS;
    HRESULT hr = S_OK;
    *pPluginException = NULL;

    do
    {
        // Get a pointer to the default AppDomain
        CComPtr<_AppDomain> spDefaultDomain = NULL;
        CComPtr<IUnknown>   spAppDomainPunk = NULL;

        hr = pHost->GetDefaultDomain(&spAppDomainPunk);
        if (FAILED(hr) || spAppDomainPunk == NULL)
        {
            output->DisplayMessage(false, g_GETTING_DEFAULT_DOMAIN_FAILED, hr);
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }

        hr = spAppDomainPunk->QueryInterface(__uuidof(_AppDomain), (PVOID*)&spDefaultDomain);
        if (FAILED(hr) || spDefaultDomain == NULL)
        {
            output->DisplayMessage(false, g_GETTING_DEFAULT_DOMAIN_FAILED, hr);
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }

        CComPtr<_ObjectHandle> spObjectHandle;

        // use CreateInstance because we use the assembly strong name (as opposed to CreateInstanceFrom)
        //
        // wszMgdPlugInFileName is system.management.automation.dll within the powershell install path.
        // For inbox PowerShell, this is %systemdir%\Windows\System32\WindowsPowerShell\v1.0\system.management.automation.dll (Aka $PSHOME\system.management.automation.dll)
        _bstr_t bstrConsoleHostAssemblyName = _bstr_t(L"System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        _bstr_t bstrUnmanagedMshEntryClass = _bstr_t(L"System.Management.Automation.Remoting.WSManPluginManagedEntryInstanceWrapper");

        hr = spDefaultDomain->CreateInstance(
            bstrConsoleHostAssemblyName,
            bstrUnmanagedMshEntryClass,
            &spObjectHandle);
        if (FAILED(hr) || spObjectHandle == NULL)
        {
            output->DisplayMessage(false, g_CREATING_MSH_ENTRANCE_FAILED, hr);
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }

        CComVariant VntUnwrapped;
        hr = spObjectHandle->Unwrap(&VntUnwrapped);
        if (FAILED(hr))
        {
            output->DisplayMessage(false, g_CREATING_MSH_ENTRANCE_FAILED, hr);
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }

        CComPtr<IDispatch> pDisp;
        pDisp = VntUnwrapped.pdispVal;

        OLECHAR FAR * wszMember = L"GetEntryDelegate";

        DISPID dispid;
        //Retrieve the DISPID
        hr = pDisp->GetIDsOfNames(
            IID_NULL,
            &wszMember,
            1,
            LOCALE_SYSTEM_DEFAULT,
            &dispid);

        if (FAILED(hr))
        {
            output->DisplayMessage(false, g_GETTING_DISPATCH_ID_FAILED, hr);
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }

        DISPPARAMS dispparamsOneArg = { NULL, NULL, 0 };
        VARIANT varResult;
        VariantInit(&varResult);
        EXCEPINFO exception;
        unsigned int uArgErr = 0;

        //Invoke the method on the Dispatch Interface
        hr = pDisp->Invoke(
            dispid,
            IID_NULL,
            LOCALE_SYSTEM_DEFAULT,
            DISPATCH_METHOD,
            &dispparamsOneArg,
            &varResult,
            &exception,
            &uArgErr);

        InitPluginWkrPtrsFuncPtr entryPointDelegate = (InitPluginWkrPtrsFuncPtr)varResult.byref;

        if (FAILED(hr) ||
            NULL == entryPointDelegate)
        {
            if (DISP_E_EXCEPTION == hr)
            {
                output->DisplayMessage(false, g_MANAGED_MSH_EXCEPTION, exception.bstrDescription);
            }
            else
            {
                output->DisplayMessage(false, g_INOVKING_MSH_ENTRANCE_FAILED, hr);
            }
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }

        // Passes empty pointer structure to the function, expects a filled struct if successful
        exitCode = entryPointDelegate(workerCallbackPtrs);
    }
    while (false);

    return exitCode;
}

PowerShellClrManagedWorker::~PowerShellClrManagedWorker()
{
    // No need to call pHost->Stop() because,
    //  as the common language runtime is automatically unloaded when the process exits.
    if (output)
    {
        delete output;
        output = NULL;
    }
}

#endif

//
// This factory method analyzes the wszMgdPlugInFileName and determines the
// appropriate IPowerShellClrHost to instantiate. It is the caller's responsibility
// to free the IPowerShellClrHost once finished using it.
//
IPowerShellClrHost* PowerShellClrWorkerFactory(
    _In_ PWSTR wszMgdPlugInFileName)
{
#ifdef CORECLR
    return new PowerShellCoreClrWorker(); // Calls into System.Management.Automation.dll
#else // !CORECLR
    std::wstring pluginFileName(wszMgdPlugInFileName);
    // This is a case-sensitive search. It checks to see if the file name portion of the path matches the v3 module name.
    if (std::wstring::npos != pluginFileName.rfind(g_MANAGED_PLUGIN_FILENAME_V3_STRING))
    {
        return new PowerShellClrManagedWorker(); // Calls into System.Management.Automation.dll
    }
    return new PowerShellClrWorker(); // Calls into pspluginwkr.dll (previous generation of plugin worker)
#endif // !CORECLR
}
