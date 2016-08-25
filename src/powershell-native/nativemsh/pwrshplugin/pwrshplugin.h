// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (C) Microsoft Corporation, 2007.
//
//  Contents:  Headers used by pwrshplugin. 
//  pwrshplugin is totally unmanaged.
// ----------------------------------------------------------------------

#pragma once

#include "NativeMsh.h"
#include "NativeMshConstants.h"
#include "entrypoints.h"
#include "pwrshheaders.h"
#include "pwrshplugindefs.h"
#include "pwrshclrhost.h"

// include wsman.h header file..it is required to declare the version
// number of the wsman API to use
#define WSMAN_API_VERSION_1_0
#include <wsman.h>

using namespace NativeMsh;

// Forward declaration of class PwrshPlugIn
class PwrshPlugIn;

// To report the plugin completion using WSManPluginReportCompletion API
// g_pPluginContext MUST be the same context that plugin provided to the WSManPluginStartup method
PwrshPlugIn* g_pPluginContext;

class PwrshPlugIn
{
private:
    PCWSTR pAppIdentifier;
    PCWSTR initParameters;

public:
    // applicationIdentification:
    // This relates to the application HTTP suffix that is hosting the plug-in.  
    // For the main WSMan service by default this would be "wsman", whereas for 
    // an IIS host it would relate to the application endpoint for that host and 
    // would be something like "MyCompany/MyApplication".
    PwrshPlugIn(PCWSTR applicationIdentification, PCWSTR pInitParams)
    {
        pAppIdentifier = applicationIdentification;
        initParameters = pInitParams;
    }

    ~PwrshPlugIn()
    {
        if (NULL != initParameters)
        {
            delete[] initParameters;
        }
    }

    PCWSTR GetApplicationIdentifier()
    {
        return pAppIdentifier;
    }

    PCWSTR GetInitParameters()
    {
        return initParameters;
    }
};

class PwrshPlugInMediator
{
private:
    // handle to managed powershell plugin
    ShutdownPluginFuncPtr hShutdownPluginMethodAddress;
    WSManPluginShellFuncPtr hCreateShellMethodAddress;
    WSManPluginReleaseShellContextFuncPtr hReleaseShellMethodAddress;
    WSManPluginCommandFuncPtr hCreateCommandMethodAddress;
    WSManPluginReleaseCommandContextFuncPtr hReleaseCommandMethodAddress;
    WSManPluginSendFuncPtr hSendMethodAddress;
    WSManPluginReceiveFuncPtr hReceiveMethodAddress;
    WSManPluginSignalFuncPtr hSignalMethodAddress;
    WSManPluginConnectFuncPtr hConnectMethodAddress;

    // boolean to keep track if the managed plugin is loaded successfully
    bool bIsPluginLoaded;
    int iMajorVersion;
    wchar_t* wszCLRVersion;
    wchar_t* wszAppBase;
    bool bIsDisposed;

    // fields needed to validate 2 plugins initializing at the same time
    CRITICAL_SECTION criticalSection; 
    bool isCSInitSucceeded; 

    // Abstraction of the differences between CLR hosting environments with
    // respect to the interface with pspluginwkr.
    IPowerShellClrHost* powerShellClrHost;

    // Default no-op implementation used for the output functions.
    NativeMsh::PwrshCommon pwrshCommon;

    // private constructor..to make Mediator Singleton
    PwrshPlugInMediator()
    {
        bIsPluginLoaded = false;
        bIsDisposed = false;
        iMajorVersion = 0;
        wszCLRVersion = NULL;
        wszAppBase = NULL;

        hShutdownPluginMethodAddress = NULL;
        hCreateShellMethodAddress = NULL;
        hReleaseShellMethodAddress = NULL;
        hCreateCommandMethodAddress = NULL;
        hReleaseCommandMethodAddress = NULL;
        hSendMethodAddress = NULL;
        hReceiveMethodAddress = NULL;
        hSignalMethodAddress = NULL;
        hConnectMethodAddress = NULL;

        isCSInitSucceeded = false;

        powerShellClrHost = NULL;

        // spin count is set to 1024
        if (InitializeCriticalSectionAndSpinCount(&criticalSection, 0x80000400))
        {
            isCSInitSucceeded = true;
        }
    }   

    // Clear plugin specific resources.
    void CleanUp()
    {
        if (!bIsDisposed)
        {
            bIsDisposed = true;

            if (NULL != powerShellClrHost)
            {
                delete powerShellClrHost;
                powerShellClrHost = NULL;
            }

            if (NULL != wszCLRVersion)
            {
                delete[] wszCLRVersion;
                wszCLRVersion = NULL;
            }

            if (NULL != wszAppBase)
            {
                delete[] wszAppBase;
                wszAppBase = NULL;
            }
        }
    }

public:

    static PwrshPlugInMediator* GetPwrshPlugInMediator(__in_opt PCWSTR extraInfo) throw (...)
    {
        // to make plugin mediator singleton
        // Create object, when called for the first time
        static PwrshPlugInMediator singletonInstance;

        if (!singletonInstance.isCSInitSucceeded)
        {
            PWSTR msg = NULL;
            GetFormattedErrorMessage(&msg, g_INIT_CRITICALSECTION_FAILED);
            throw new PlugInException(g_INIT_CRITICALSECTION_FAILED, msg);
        }

        // this check avoids entering/exiting critical sections
        // frequently
        if (!singletonInstance.bIsPluginLoaded)
        {
            EnterCriticalSection(&singletonInstance.criticalSection);
            try
            {
                if (!singletonInstance.bIsPluginLoaded)
                {
                    // process extra info initializes the pwrshplugin
                    // by initializing the CLR version and obtaining access
                    // pointers for the plugin worker or for System.Management.Automation.dll.
                    singletonInstance.ProcessExtraInfo(extraInfo, NULL);
                }
            }
            catch(PlugInException* e)
            {
                LeaveCriticalSection(&singletonInstance.criticalSection);
                throw e;
            }

            LeaveCriticalSection(&singletonInstance.criticalSection);
        }

        return &singletonInstance;
    }

    ~PwrshPlugInMediator()
    {   
        if (isCSInitSucceeded)
        {
            DeleteCriticalSection(&criticalSection);
        }

        CleanUp();
    }

    // Clear plugin specific resources.
    DWORD Shutdown(__in DWORD flags, __in DWORD reason) throw (...)
    {
        // this null condition should never occur.. but for server process safety ensure we 
        // fail safely.. 
        if (NULL != hShutdownPluginMethodAddress)
        {
            (hShutdownPluginMethodAddress)((PVOID)this); 
        }

        CleanUp();
        return NO_ERROR;
    }

    PwrshPlugIn* CreatePwrshPlugIn(__in PCWSTR applicationIdentification,
        __in_opt PCWSTR extraInfo) throw (...)
    {
        PWSTR initParameters = NULL;
        // ProcessExtraInfo might throw...
        // storing the extra info for plugin use later.
        VerifyAndStoreExtraInfo(extraInfo, &initParameters);
        PwrshPlugIn* result = new PwrshPlugIn(applicationIdentification, initParameters);
        g_pPluginContext = result;
        return result;
    }

    VOID CreateShell(
        __in PwrshPlugIn *plugInToUseWithCreateShell,
        __in WSMAN_PLUGIN_REQUEST *requestDetails,
        __in DWORD flags,
        __in_opt WSMAN_SHELL_STARTUP_INFO *startupInfo,
        __in_opt WSMAN_DATA *inboundShellInformation)
    {
        if ((NULL == plugInToUseWithCreateShell) ||
            (NULL == requestDetails) || 
            (NULL == startupInfo) || 
            (NULL == requestDetails->operationInfo))
        {
            ReportError(requestDetails, g_INVALID_INPUT, L"WSManPluginShell");
            return;
        }

        // this null condition should never occur.. but for server process safety ensure we 
        // fail safely.. 
        if (NULL == hCreateShellMethodAddress)
        {
            ReportError(requestDetails, g_MANAGED_METHOD_RESOLUTION_FAILED);
            return;
        }

        __try
        {
            PCWSTR initParameters = plugInToUseWithCreateShell->GetInitParameters();
            (hCreateShellMethodAddress)((PVOID)this, requestDetails, flags, initParameters, startupInfo, inboundShellInformation);            
        }
        __except(ProcessException(requestDetails, GetExceptionCode()))
        {
        }
    }

    VOID ReleaseShell(__in PVOID shellContext)
    {
        // this null condition should never occur.. but for server process safety ensure we 
        // fail safely.. 
        if (NULL != hReleaseShellMethodAddress)
        {
            (hReleaseShellMethodAddress)((PVOID)this, shellContext);
        }
    }

    VOID CreateCommand(
        __in WSMAN_PLUGIN_REQUEST *requestDetails,
        __in DWORD flags,
        __in PVOID shellContext,
        __in PCWSTR commandLine,
        __in_opt WSMAN_COMMAND_ARG_SET *arguments)
    {
        if (NULL == requestDetails)
        {
            ReportError(requestDetails, g_INVALID_INPUT, L"WSManPluginCommand");
            return;
        }

        if (NULL == hCreateCommandMethodAddress)
        {
            ReportError(requestDetails, g_MANAGED_METHOD_RESOLUTION_FAILED);
            return;
        }

        __try
        {
            (hCreateCommandMethodAddress)((PVOID)this, requestDetails, flags, shellContext, commandLine, arguments);
        }        
        __except(ProcessException(requestDetails, GetExceptionCode()))
        {
        }
    }

    VOID ReleaseCommand(__in PVOID shellContext,
        __in PVOID commandContext)
    {
        // this null condition should never occur.. but for server process safety ensure we 
        // fail safely.. 
        if (NULL != hReleaseShellMethodAddress)
        {
            (hReleaseCommandMethodAddress)((PVOID)this, shellContext, commandContext);
        }
    }

    VOID ExecuteConnectToShellOrCommand(
        __in WSMAN_PLUGIN_REQUEST *requestDetails,
        __in DWORD flags,
        __in PVOID shellContext,
        __in_opt PVOID commandContext,
        __in_opt WSMAN_DATA *inboundConnectInformation)
    {
        if (NULL == requestDetails)
        {
            ReportError(requestDetails, g_INVALID_INPUT, L"WSManPluginConnect");
            return;
        }

        if (NULL == hConnectMethodAddress)
        {
            ReportError(requestDetails, g_MANAGED_CONNECT_METHOD_RESOLUTION_FAILED);
            return;
        }

        __try
        {
            (hConnectMethodAddress)((PVOID)this, requestDetails, flags, shellContext, commandContext, inboundConnectInformation);
        }
        __except(ProcessException(requestDetails, GetExceptionCode()))
        {
        }

    }

    VOID SendOneItemToShellOrCommand(
        __in WSMAN_PLUGIN_REQUEST *requestDetails,
        __in DWORD flags,
        __in PVOID shellContext,
        __in_opt PVOID commandContext,
        __in PCWSTR stream,
        __in WSMAN_DATA *inboundData)
    {
        if (NULL == requestDetails)
        {
            ReportError(requestDetails, g_INVALID_INPUT, L"WSManPluginSend");
            return;
        }

        if (NULL == hSendMethodAddress)
        {
            ReportError(requestDetails, g_MANAGED_METHOD_RESOLUTION_FAILED);
            return;
        }

        __try
        {
            (hSendMethodAddress)((PVOID)this, requestDetails, flags, shellContext, commandContext, stream, inboundData);
        }
        __except(ProcessException(requestDetails, GetExceptionCode()))
        {
        }
    }

    VOID EnableShellOrCommandToSendDataToClient(
        __in WSMAN_PLUGIN_REQUEST *requestDetails,
        __in DWORD flags,
        __in PVOID shellContext,
        __in_opt PVOID commandContext,
        __in_opt WSMAN_STREAM_ID_SET* streamSet)
    {
        if (NULL == requestDetails)
        {
            ReportError(requestDetails, g_INVALID_INPUT, L"WSManPluginReceive");
            return;
        }

        if (NULL == hReceiveMethodAddress)
        {
            ReportError(requestDetails, g_MANAGED_METHOD_RESOLUTION_FAILED);
            return;
        }

        __try
        {
            (hReceiveMethodAddress)((PVOID)this, requestDetails, flags, shellContext, commandContext, streamSet);
        }
        __except(ProcessException(requestDetails, GetExceptionCode()))
        {
        }
    }

    VOID SignalShellOrCmd(        
        __in WSMAN_PLUGIN_REQUEST *requestDetails,
        __in DWORD flags,
        __in PVOID shellContext,
        __in_opt PVOID commandContext,
        __in PCWSTR code)
    {
        if (NULL == requestDetails)
        {
            ReportError(requestDetails, g_INVALID_INPUT, L"WSManPluginSignal");
            return;
        }

        if (NULL == hSignalMethodAddress)
        {
            ReportError(requestDetails, g_MANAGED_METHOD_RESOLUTION_FAILED);
            return;
        }

        __try
        {
            (hSignalMethodAddress)((PVOID)this, requestDetails, flags, shellContext, commandContext, code);
        }
        __except(ProcessException(requestDetails, GetExceptionCode()))
        {
        }
    }

private:
    // checks if the current plugin is active.
    inline bool IsActive()
    {
        return bIsDisposed;
    }

    /* Process error code from the exception. The current code always
    * returns EXCEPTION_CONTINUE_SEARCH after logging the error code.
    *
    * requestDetails is used to report WSMan operation complete
    */
    int ProcessException(WSMAN_PLUGIN_REQUEST *requestDetails, unsigned int errorCode)
    {
        WSManPluginOperationComplete(requestDetails, 0, errorCode, NULL);
        return EXCEPTION_CONTINUE_EXECUTION;
    }

    DWORD ReportError(WSMAN_PLUGIN_REQUEST *requestDetails, DWORD dwMessageId, ...)
    {
        PWSTR extendedErrorInformation = NULL;

        va_list args;
        va_start(args, dwMessageId);

        GetFormattedErrorMessage(&extendedErrorInformation, dwMessageId, &args); 

        va_end(args);

        DWORD errorCode = dwMessageId;
        DWORD result = WSManPluginOperationComplete(requestDetails, 0, errorCode, extendedErrorInformation);

        if (NULL != extendedErrorInformation)
        {
            delete[] extendedErrorInformation;
        }

        return result;
    }

    DWORD ReportError(WSMAN_PLUGIN_REQUEST *requestDetails, PlugInException* e)
    {
        DWORD errorCode = e->dwMessageId;
        
        DWORD result = WSManPluginOperationComplete(requestDetails, 0, errorCode, e->extendedErrorInformation);        
        return result;
    }

    unsigned int CreateMgdPluginFileName(
        int iVPSMajorVersion,
        int iVPSMinorVersion,
        _In_ PWSTR wszVAppBase,
        __out PWSTR* wszMgdPlugInFileName)
    {
        int monadMajorVersion = iVPSMajorVersion;
        int monadMinorVersion = iVPSMinorVersion;
        wchar_t * wszMonadVersion = NULL;
        wchar_t* wszTempCLRVersion = NULL;
        unsigned int exitCode = EXIT_CODE_SUCCESS;

        do
        {
            iMajorVersion = iVPSMajorVersion;

            exitCode = ConstructPowerShellVersion(iVPSMajorVersion, iVPSMinorVersion, &wszMonadVersion);
            if (EXIT_CODE_SUCCESS != exitCode)
            {
                break;
            }

            // Read managed plugin's full path from registry, if such a path exists.
            exitCode = pwrshCommon.GetRegistryInfo(
                &wszMonadVersion,
                &monadMajorVersion,
                monadMinorVersion,
                &wszTempCLRVersion,
                g_PSPLUGINWKRV3_REGISTRY_KEY,
                wszMgdPlugInFileName);

            if (*wszMgdPlugInFileName == NULL)
            {
                // Reset exit code to EXIT_CODE_SUCCESS
                exitCode = EXIT_CODE_SUCCESS;

                // construct managed plugin's full path
                size_t iAppBaseLength;
                size_t iFileNameLength;
                size_t iTotalLength;

                if (FAILED(StringCchLength(wszVAppBase, STRSAFE_MAX_CCH, &iAppBaseLength)) ||
                    FAILED(StringCchLength(g_MANAGED_PLUGIN_FILENAME_STRING, STRSAFE_MAX_CCH, &iFileNameLength)))
                {
                    exitCode = g_INVALID_INPUT;
                    break;
                }

                iTotalLength = iAppBaseLength + iFileNameLength + 2;

                *wszMgdPlugInFileName = new wchar_t[iTotalLength];
                if (NULL == *wszMgdPlugInFileName)
                {
                    exitCode = E_OUTOFMEMORY;
                    break;
                }
                (*wszMgdPlugInFileName)[0] = '\0';

                if (FAILED(StringCchCopyW(*wszMgdPlugInFileName, iTotalLength, wszVAppBase)) ||
                    FAILED(StringCchCatW(*wszMgdPlugInFileName, iTotalLength, L"\\")) ||
                    FAILED(StringCchCatW(*wszMgdPlugInFileName, iTotalLength, g_MANAGED_PLUGIN_FILENAME_STRING)))
                {
                    exitCode = g_MANAGED_PLUGIN_PATH_CONSTRUCTION_ERROR;
                    break;
                }
            }
        } while (false);

        if (NULL != wszMonadVersion)
        {
            delete[] wszMonadVersion;
        }

        if (NULL != wszTempCLRVersion)
        {
            delete[] wszTempCLRVersion;
        }
        return exitCode;
    }

    // returns non-zero code on error + plugin exception is populated in some cases
    // like plugin load error. so the caller is expected to check both these 
    // to see if there is any error.
    unsigned int LoadManagedPlugIn(
        _In_ PWSTR wszMgdPlugInFileName,
        _In_ PWSTR wszVCLRVersion,  // Conditionally set to wszCLRVersion on success
        _In_ PWSTR wszVAppBase,     // Conditionally set to wszAppBase on success
        __out_opt PlugInException** pPluginException )
    {
        unsigned int exitCode = EXIT_CODE_SUCCESS;

        if (bIsPluginLoaded)
        {
            return g_MANAGED_PLUGIN_ALREADY_LOADED;
        }

        if ((NULL == wszVCLRVersion) || (NULL == wszVAppBase))
        {
            return g_INVALID_INPUT;
        }

        do
        {
            *pPluginException = NULL;

            // Setting global AppBase and CLR Version
            wszCLRVersion = wszVCLRVersion;
            wszAppBase = wszVAppBase;

            // Load managed plugin worker..

            PwrshPluginWkr_Ptrs workerCallbackPtrs;
            memset(&workerCallbackPtrs, 0, sizeof(PwrshPluginWkr_Ptrs));

            // The loader will obtain the worker pointers from the appropriate DLL.
            // If this call succeeds, PwrshPluginWkr_Ptrs should be populated.
            exitCode = powerShellClrHost->LoadWorkerCallbackPtrs(&workerCallbackPtrs, wszMgdPlugInFileName, pPluginException);

            if (EXIT_CODE_SUCCESS == exitCode)
            {
                // Now get the proc address for all the plugin API..
                hShutdownPluginMethodAddress = workerCallbackPtrs.shutdownPluginFuncPtr;
                hCreateShellMethodAddress = workerCallbackPtrs.wsManPluginShellFuncPtr;
                hReleaseShellMethodAddress = workerCallbackPtrs.wsManPluginReleaseShellContextFuncPtr;
                hCreateCommandMethodAddress = workerCallbackPtrs.wsManPluginCommandFuncPtr;
                hReleaseCommandMethodAddress = workerCallbackPtrs.wsManPluginReleaseCommandContextFuncPtr;
                hSendMethodAddress = workerCallbackPtrs.wsManPluginSendFuncPtr;
                hReceiveMethodAddress = workerCallbackPtrs.wsManPluginReceiveFuncPtr;
                hSignalMethodAddress = workerCallbackPtrs.wsManPluginSignalFuncPtr;
                hConnectMethodAddress = workerCallbackPtrs.wsManPluginConnectFuncPtr;
            }

            if (// if pwrsplugin v2 plugin, this function does not exist
                // (NULL == hShutdownPluginMethodAddress) ||
                (NULL == hCreateShellMethodAddress) || (NULL == hReleaseShellMethodAddress) ||
                (NULL == hCreateCommandMethodAddress) || (NULL == hReleaseCommandMethodAddress) ||
                (NULL == hSendMethodAddress) || (NULL == hReceiveMethodAddress) ||
                (NULL == hSignalMethodAddress))
            {
                PWSTR msg = NULL;
                exitCode = g_MANAGED_METHOD_RESOLUTION_FAILED;
                GetFormattedErrorMessage(&msg, g_MANAGED_METHOD_RESOLUTION_FAILED);
                *pPluginException = new PlugInException(exitCode, msg);
                break;
            }

            bIsPluginLoaded = true;
        }while(false);

        if (exitCode != EXIT_CODE_SUCCESS)
        {
            wszCLRVersion = NULL;
            wszAppBase = NULL;
        }

        return exitCode;
    }

    void LoadPowerShell(PCWSTR version) throw (...)
    {
        // Verify incoming powershell version format.
        int iPSMajorVersion = 0;
        int iPSMinorVersion = 0;
        if (!pwrshCommon.VerifyMonadVersionFormat(version, &iPSMajorVersion, &iPSMinorVersion, true, false))
        {
            PWSTR msg = NULL;
            GetFormattedErrorMessage(&msg, g_OPTION_SET_NOT_COMPLY, g_BUILD_VERSION);
            throw new PlugInException(g_OPTION_SET_NOT_COMPLY, msg);
        }

        // client is requesting powershell 1.x version. Remoting doesn't support
        // powershell 1.0, remoting is supported from 2.0
        if (1 >= iPSMajorVersion)
        {
            PWSTR msg = NULL;
            GetFormattedErrorMessage(&msg, g_OPTION_SET_NOT_COMPLY, g_BUILD_VERSION);
            throw new PlugInException(g_OPTION_SET_NOT_COMPLY, msg);
        }

        // the reg key from 2 and 1 is 1..
        int requestedMonadMajorVersion = iPSMajorVersion;
        if (2 == requestedMonadMajorVersion)
        {
            requestedMonadMajorVersion = 1;
        }

        wchar_t* wszMonadVersion = NULL;    // Allocated via ConstructPowerShellVersion || GetRegistryInfo
        wchar_t* wszTempCLRVersion = NULL;  // Allocated via GetRegistryInfo
        wchar_t* wszTempAppBase = NULL;     // Allocated via GetRegistryInfo
        PWSTR wszMgdPlugInFileName = NULL;  // Allocated in CreateMgdPluginFileName
        unsigned int exitCode = EXIT_CODE_SUCCESS;
        PlugInException* pErrorMsg = NULL;

        do 
        {
            exitCode = ConstructPowerShellVersion(iPSMajorVersion, iPSMinorVersion, &wszMonadVersion);
            if (exitCode != EXIT_CODE_SUCCESS)
            {
                PWSTR msg = NULL;
                GetFormattedErrorMessage(&msg, g_OPTION_SET_NOT_COMPLY, g_BUILD_VERSION);
                pErrorMsg = new PlugInException(exitCode, msg);
                break;
            }

            // Read CLR version from registry
            exitCode = pwrshCommon.GetRegistryInfo(
                &wszMonadVersion,
                &requestedMonadMajorVersion,
                iPSMinorVersion,
                &wszTempCLRVersion,
                L"ApplicationBase",
                &wszTempAppBase);
            if (EXIT_CODE_SUCCESS != exitCode)
            {
                PWSTR msg = NULL;
                GetFormattedErrorMessage(&msg, g_OPTION_SET_NOT_COMPLY, g_BUILD_VERSION);
                pErrorMsg = new PlugInException(exitCode, msg);
                break;
            }

            exitCode = CreateMgdPluginFileName(requestedMonadMajorVersion, iPSMinorVersion, wszTempAppBase, &wszMgdPlugInFileName);
            if (EXIT_CODE_SUCCESS != exitCode)
            {
                break;
            }

            if (!bIsPluginLoaded)
            {
                this->powerShellClrHost = PowerShellClrWorkerFactory(wszMgdPlugInFileName);
                if (NULL == this->powerShellClrHost)
                {
                    exitCode = ERROR_NOT_ENOUGH_MEMORY;
                    break;
                }

                exitCode = powerShellClrHost->LaunchClr(wszMonadVersion, wszTempCLRVersion, "PwrshPlugin");
                if (EXIT_CODE_SUCCESS != exitCode)
                {
                    PWSTR msg = NULL;
                    GetFormattedErrorMessage(&msg, g_CLR_LOAD_FAILED, wszTempCLRVersion);
                    pErrorMsg = new PlugInException(g_CLR_LOAD_FAILED, msg);
                    break;
                }

                exitCode = LoadManagedPlugIn(wszMgdPlugInFileName, wszTempCLRVersion, wszTempAppBase, &pErrorMsg);
            }
            else
            {
                if (requestedMonadMajorVersion != iMajorVersion)
                {
                    exitCode = g_OPTION_SET_MAJOR_VERSION_NOT_MATCH;
                }
                else if (0 != _wcsnicmp(wszTempCLRVersion, wszCLRVersion, 20))
                {
                    exitCode = g_OPTION_SET_CLR_VERSION_NOT_MATCH;
                }
                else if (0 != _wcsicmp(wszTempAppBase, wszAppBase))
                {
                    exitCode = g_OPTION_SET_APP_BASE_NOT_MATCH;
                }
            }
        } while (false);

        if (NULL != wszMonadVersion)
        {
            delete[] wszMonadVersion;
        }
        
        if (NULL != wszMgdPlugInFileName)
        {
            delete[] wszMgdPlugInFileName;
        }

        if (exitCode != EXIT_CODE_SUCCESS)
        {
            // Assigned to this->wszCLRVersion on success in LoadManagedPlugIn, so it shouldn't be freed here on success
            if (NULL != wszTempCLRVersion)
            {
                delete[] wszTempCLRVersion;
            }

            // Assigned to this->wszAppBase on success LoadManagedPlugIn, so it shouldn't be freed here on success
            if (NULL != wszTempAppBase)
            {
                delete[] wszTempAppBase;
            }

            if (NULL != pErrorMsg)
            {
                throw pErrorMsg;
            }
            else
            {
                PWSTR msg = NULL;
                GetFormattedErrorMessage(&msg, g_OPTION_SET_NOT_COMPLY, g_BUILD_VERSION);
                throw new PlugInException(exitCode, msg);
            }
        }
    }      

public:
    // extraInfo is supplied by WSMan and WSMan validates the XML syntax
    // before supplying this value to plugin..because of this, the following
    // method will not check for xml element syntax.
    void VerifyAndStoreExtraInfo(PCWSTR extraInfo,
        __deref_opt_out PWSTR *initParameters) throw(...)
    {
        if (NULL == extraInfo)
        {
            PWSTR msg = NULL;
            GetFormattedErrorMessage(&msg, 
                g_PSVERSION_NOT_FOUND_IN_CONFIG, g_PSVERSION_CONFIG, g_INITIALIZATIONPARAM_CONFIG);
            throw new PlugInException(g_PSVERSION_NOT_FOUND_IN_CONFIG, msg);
        }

        size_t initParamsLength;
        if (FAILED(StringCchLength(extraInfo, STRSAFE_MAX_CCH, &initParamsLength)))
        {
            PWSTR msg = NULL;
            GetFormattedErrorMessage(&msg, 
                g_BAD_INITPARAMETERS, g_INITIALIZATIONPARAM_CONFIG);
            throw new PlugInException(g_BAD_INITPARAMETERS, msg);
        }

        if (NULL != initParameters)
        {
            // make a local copy of the extra info for future use
            // this will be used whenever a new shell is created.
            *initParameters = new wchar_t[initParamsLength + 1];
            if (FAILED(StringCchCopyNW(*initParameters, initParamsLength + 1, extraInfo, initParamsLength)))
            {
                PWSTR msg = NULL;
                GetFormattedErrorMessage(&msg, 
                    g_BAD_INITPARAMETERS, g_INITIALIZATIONPARAM_CONFIG);
                throw new PlugInException(g_BAD_INITPARAMETERS, msg);
            }
        }
    }

    void ProcessExtraInfo(PCWSTR extraInfo,
        __deref_opt_out PWSTR *initParameters) throw(...) 
    {
        VerifyAndStoreExtraInfo(extraInfo, initParameters);        

        // Get PSVersion and MaxPSVersion values from the config xml
        wchar_t* psversion = NULL;
        wchar_t* maxpsversion = NULL;
        // Win8: 97936: To support backward compatability, if PSVersion = 2.0 and AssemblyToken
        // is specified we set maxPSVersion = 2.0..so that the endpoint is not automatically 
        // transferred to PS 3.0
        wchar_t* assemblyToken = NULL;

        // This will hold the powershell version calculated from psversion and maxpsversion
        wchar_t* version = NULL;

        size_t initParamsLength;
        if (FAILED(StringCchLength(extraInfo, STRSAFE_MAX_CCH, &initParamsLength)))
        {
            PWSTR msg = NULL;
            GetFormattedErrorMessage(&msg, 
                g_BAD_INITPARAMETERS, g_INITIALIZATIONPARAM_CONFIG);
            throw new PlugInException(g_BAD_INITPARAMETERS, msg);
        }        

        PCWSTR param = wcsstr(extraInfo, L"<Param ");
        param += 7;        

        PCWSTR name = NULL;

        while((NULL != param) && ((size_t)(param - extraInfo) < initParamsLength))
        {
            name = wcsstr(param, L"Name=");
            if (NULL != name)
            {
                name += 5;
                // eliminate empty spaces
                while(isspace(*name) && ((size_t)(name - extraInfo) < initParamsLength))
                {
                    name++;
                }
                if ((0 == _wcsnicmp(name, L"\"psversion\"", 11)) || (0 == _wcsnicmp(name, L"\'psversion\'", 11)))
                {
                    psversion = ReadConfigXmlValue(param, extraInfo, initParamsLength);
                }
                else if ((0 == _wcsnicmp(name, L"\"maxpsversion\"", 14)) || (0 == _wcsnicmp(name, L"\'maxpsversion\'", 14)))
                {
                    maxpsversion = ReadConfigXmlValue(param, extraInfo, initParamsLength);
                }
                else if ((0 == _wcsnicmp(name, L"\"assemblyname\"", 14)) || (0 == _wcsnicmp(name, L"\'assemblyname\'", 14)))
                {
                    assemblyToken = ReadConfigXmlValue(param, extraInfo, initParamsLength);
                }

                // To avoid going through other items in xml if psversion,maxpsversion and assemblyToken have been read
                if ((psversion != NULL) && (maxpsversion != NULL) && (assemblyToken != NULL))
                {
                    break;
                }
            }

            // get the next Param element and probe it for psversion/maxpsversion
            param = wcsstr(param, L"<Param ");
            param += 7;
        }

        PWSTR msg = NULL;
        DWORD msgId = 0;
        do 
        {
            // Win8: 97936: To support backward compatability, if PSVersion = 2.0 and AssemblyToken
            // is specified we set maxPSVersion = 2.0..so that the endpoint is not automatically 
            // forwarded to PS 3.0
            if ((psversion != NULL) && (assemblyToken != NULL) && (maxpsversion == NULL))
            {
                // MaxPSVersion was introduced in Win8..so if this endpoint is created in Win7
                // MaxPSVersion should be NULL.
                int majorPSVersionNumber = -1, minorPSVersionNumber = -1;
                if (pwrshCommon.VerifyMonadVersionFormat(psversion, &majorPSVersionNumber, &minorPSVersionNumber, true, true))
                {
                    if ((2 == majorPSVersionNumber))
                    {
                        size_t length;
                        if (FAILED(StringCchLength(psversion, STRSAFE_MAX_CCH, &length)))
                        {        
                            msgId = g_BAD_INITPARAMETERS;
                            GetFormattedErrorMessage(&msg, 
                                g_BAD_INITPARAMETERS, g_INITIALIZATIONPARAM_CONFIG);
                            break;
                        }

                        maxpsversion = new wchar_t[length + 1];
                        // Set maxPSVersion as the psversion
                        if (FAILED(StringCchCopyNW(maxpsversion, length + 1, psversion, length)))
                        {
                            msgId = g_BAD_INITPARAMETERS;
                            GetFormattedErrorMessage(&msg, 
                                g_BAD_INITPARAMETERS, g_INITIALIZATIONPARAM_CONFIG);
                            break;
                        }
                    }
                }
            }

            if (psversion != NULL)
            {
                version = CalculatePowershellVersion(psversion, maxpsversion);
                break;
            }

            // we are here if no "PSVersion" value found..so report the error.
            msgId = g_PSVERSION_NOT_FOUND_IN_CONFIG;
            GetFormattedErrorMessage(&msg, 
                g_PSVERSION_NOT_FOUND_IN_CONFIG, g_PSVERSION_CONFIG, g_INITIALIZATIONPARAM_CONFIG);

        } while(false);

        // free the memory of local variables before loading PowerShell
        if (NULL != psversion)
        {
            delete[] psversion;
        }

        if (NULL != maxpsversion)
        {
            delete[] maxpsversion;
        }

        if (NULL != assemblyToken)
        {
            delete[] assemblyToken;
        }

        if (NULL != version)
        {
            return LoadPowerShell(version);
        }

        throw new PlugInException(msgId, msg);
    }


    wchar_t* ReadConfigXmlValue(PCWSTR param, 
        PCWSTR extraInfo, size_t initParamsLength)
    {
        // We have the PSVersion or MaxPSVersion...
        // Get the version value

        wchar_t* version = NULL;
        PCWSTR value = wcsstr(param, L"Value=");
        if (NULL != value)
        {
            value += 6;

            //eliminate empty spaces
            while(isspace(*value) && ((size_t)(value - extraInfo) < initParamsLength))
            {
                value++;
            }

            if ((L'"' == *value) || (L'\'' == *value))
            {
                value++;
            }
            else
            {
                return version;
            }

            PCWSTR startVersion = value;
            while((L'"' != *value) && (L'\'' != *value) && ((size_t)(value - extraInfo) < initParamsLength))
            {
                value++;
            }

            // copy the version.
            size_t length = value - startVersion;
            if (length > 0)
            {
                version = new wchar_t[length + 1];

                try
                {
                    if (FAILED(StringCchCopyNW(version, length + 1, startVersion, length)))
                    {
                        PWSTR msg = NULL;
                        GetFormattedErrorMessage(&msg, 
                            g_BAD_INITPARAMETERS, g_INITIALIZATIONPARAM_CONFIG);
                        throw new PlugInException(g_BAD_INITPARAMETERS, msg);
                    }
                    return version;                                        
                }
                catch(...)
                {
                    if (NULL != version)
                    {
                        delete[] version;
                    }
                    throw;
                }

                // delete the version in non-throw case.
                if (NULL != version)
                {
                    delete[] version;
                }
            }
        }                    
        return NULL;   
    }

    wchar_t* CalculatePowershellVersion(__in wchar_t* psversion, 
        __in_opt wchar_t* maxpsversion)
    {
        wchar_t* version = NULL;
        int majorPSVersionNumber = -1, minorPSVersionNumber = -1;
        int majorMaxPSVersionNumber = -1, minorMaxPSVersionNumber = -1 ;
        if (pwrshCommon.VerifyMonadVersionFormat(psversion, &majorPSVersionNumber, &minorPSVersionNumber, true, true))
        {
            if (maxpsversion != NULL)
            {
                if (pwrshCommon.VerifyMonadVersionFormat(maxpsversion, &majorMaxPSVersionNumber, &minorMaxPSVersionNumber, true, true))
                {
                    // TODO: Don't understand this.
                    if (majorMaxPSVersionNumber > majorPSVersionNumber)
                    {
                        PWSTR msg = NULL;
                        GetFormattedErrorMessage(&msg, 
                            g_BAD_INITPARAMETERS, g_INITIALIZATIONPARAM_CONFIG);
                        throw new PlugInException(g_BAD_INITPARAMETERS, msg);                        
                    }

                    // TODO: why hardcoding is needed here
                    if (majorPSVersionNumber == 3 && majorMaxPSVersionNumber == 2)
                    {
                        PWSTR msg = NULL;
                        GetFormattedErrorMessage(&msg, 
                            g_BAD_INITPARAMETERS, g_INITIALIZATIONPARAM_CONFIG);
                        throw new PlugInException(g_BAD_INITPARAMETERS, msg);
                    }

                    if (majorPSVersionNumber == 2 && majorMaxPSVersionNumber == 2)
                    {
                        size_t length;
                        if (FAILED(StringCchLength(psversion, STRSAFE_MAX_CCH, &length)))
                        {        
                            PWSTR msg = NULL;
                            GetFormattedErrorMessage(&msg, 
                                g_BAD_INITPARAMETERS, g_INITIALIZATIONPARAM_CONFIG);
                            throw new PlugInException(g_BAD_INITPARAMETERS, msg);
                        }

                        version = new wchar_t[length + 1];
                        // Set version as the psversion
                        if (FAILED(StringCchCopyNW(version, length + 1, psversion, length)))
                        {
                            PWSTR msg = NULL;
                            GetFormattedErrorMessage(&msg, 
                                g_BAD_INITPARAMETERS, g_INITIALIZATIONPARAM_CONFIG);
                            throw new PlugInException(g_BAD_INITPARAMETERS, msg);
                        }
                    }
                }
            }
            else
            {
                version = L"3.0";
            }
        }
        return version;
    }
};

extern "C"
void WINAPI PerformWSManPluginReportCompletion()
{
    // Now report the plugin completion, to indicate that plugin is ready to shutdown.
    // This API is used by plugins to report completion
    // - pluginContext MUST be the same context that plugin provided to the WSManPluginStartup method
    // - flags are reserved, so 0
    WSManPluginReportCompletion(g_pPluginContext, 0);
}
