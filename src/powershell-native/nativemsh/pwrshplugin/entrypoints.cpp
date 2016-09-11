// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (C) Microsoft Corporation, 2007.
//
//  Contents:  Entry points for PowerShell plugin used to host powershell
//  in a WSMan service.
// ----------------------------------------------------------------------

#include "pwrshplugin.h"
// [Porting note] SQM is for Telemetry in Windows. Temporarily disabled.
//#include <winsqm.h>
//#include "common/WindowsSqmDataID.h"
//#include "common/winrmsqm.h"

#if !CORECLR
#include <muiload.h>
#endif

HINSTANCE g_hResourceInstance = 0; // TODO: Where is this freed? FreeMUILibrary for nonCoreClr and FreeLibrary for CoreCLR
LPCWSTR g_MAIN_BINARY_NAME = L"pwrshplugin.dll";

// [Porting note] SQM is for Telemetry in Windows. Temporarily disabled.
// typedef VOID (NTAPI *PFN_WinSqmSetDWORD)(
//                 __in_opt                HSESSION                    hSession,
//                 __in                    DWORD                       dwDatapointId,
//                 __in                    DWORD                       dwDatapointValue
//                 );

// gets the error message from the resources section of the current module.
// the caller should free pwszErrorMessage using LocalFree().
// returns: If the function succeeds the return value is the number of CHARs stored int the output
// buffer, excluding the terminating null character. If the function fails the return value is zero.
#pragma prefast(push)
#pragma prefast (disable: 28196)
DWORD GetFormattedErrorMessage(__deref_out PWSTR * pwszErrorMessage, DWORD dwMessageId, va_list* arguments)
#pragma prefast(pop)
{
    DWORD dwLength = 0;

    do 
    {
        *pwszErrorMessage = NULL;

        if (NULL == g_hResourceInstance)
        {
#ifdef CORECLR
            g_hResourceInstance = LoadLibraryEx(g_MAIN_BINARY_NAME, 0, LOAD_LIBRARY_AS_DATAFILE | LOAD_LIBRARY_AS_IMAGE_RESOURCE);
#else
            g_hResourceInstance = LoadMUILibraryW(g_MAIN_BINARY_NAME, MUI_LANGUAGE_NAME, 0);
#endif
        }
        
        LPWSTR wszSystemErrorMessage = NULL;
        //string function
        dwLength = FormatMessageW(
            FORMAT_MESSAGE_FROM_HMODULE | FORMAT_MESSAGE_ALLOCATE_BUFFER,
            g_hResourceInstance,
            dwMessageId,
            0,
            (LPWSTR)&wszSystemErrorMessage,
            0,
            arguments);

        if (dwLength > 0)
        {
            *pwszErrorMessage = new WCHAR[dwLength + 1];
            if (NULL != *pwszErrorMessage)
            {
                //string function
                if (FAILED(StringCchCopyW(*pwszErrorMessage, dwLength + 1, wszSystemErrorMessage)))
                {
                    dwLength = 0;
                    delete [] (*pwszErrorMessage);
                    *pwszErrorMessage = NULL;
                }
            }
            LocalFree(wszSystemErrorMessage);
        }

    }while(false);    

    return dwLength;
}

DWORD GetFormattedErrorMessage(__deref_out PZPWSTR pwszErrorMessage, DWORD dwMessageId, ...)
{
    DWORD result = 0;

    va_list args;
    va_start(args, dwMessageId);

    result = GetFormattedErrorMessage(pwszErrorMessage, dwMessageId, &args);

    va_end(args);

    return result;
}

#pragma prefast(push)
#pragma prefast (disable: 6101)
#pragma prefast (disable: 6054)

unsigned int ConstructPowerShellVersion(int iPSMajorVersion, 
                                        int iPSMinorVersion, 
                                        __deref_out_opt PWSTR *pwszMonadVersion)
{
    unsigned int exitCode = EXIT_CODE_SUCCESS;
    wchar_t* wszMajorVersion = new wchar_t[10];
    wchar_t* wszMinorVersion = new wchar_t[10];

    do
    {
        if ((NULL == pwszMonadVersion) || ( 0 > iPSMajorVersion) || (0 > iPSMinorVersion))
        {
            exitCode = EXIT_CODE_BAD_INPUT;
            break;
        }

        if (0 != _itow_s(iPSMajorVersion, wszMajorVersion, 10, 10))
        {
            exitCode = EXIT_CODE_BAD_INPUT;
            break;
        }

        if (0 != _itow_s(iPSMinorVersion, wszMinorVersion, 10, 10))
        {
            exitCode = EXIT_CODE_BAD_INPUT;
            break;
        }

        size_t iMajorLength;
        size_t iMinorLength;

        if (SUCCEEDED(StringCchLength(wszMajorVersion, 10, &iMajorLength)) &&
            SUCCEEDED(StringCchLength(wszMinorVersion, 10, &iMinorLength)))
        {
            size_t totalLength = iMajorLength + iMinorLength + 2;
            *pwszMonadVersion = new wchar_t[totalLength];
            if (NULL == *pwszMonadVersion)
            {
                exitCode = ERROR_NOT_ENOUGH_MEMORY;
                break;
            }

            *pwszMonadVersion[0] = L'\0';
            if (SUCCEEDED(StringCchCopyW(*pwszMonadVersion, totalLength, wszMajorVersion)) &&
                SUCCEEDED(StringCchCatW(*pwszMonadVersion, totalLength, L".")) &&
                SUCCEEDED(StringCchCatW(*pwszMonadVersion, totalLength, wszMinorVersion)))
            {
                break;
            }
            else
            {
                exitCode = EXIT_CODE_BAD_INPUT;
                break;
            }
        }
        else
        {
            exitCode = EXIT_CODE_BAD_INPUT;
            break;
        }
    }while(false);

    if (NULL != wszMajorVersion)
    {
        delete[] wszMajorVersion;
    }

    if (NULL != wszMinorVersion)
    {
        delete[] wszMinorVersion;
    }

    return exitCode;
}

#pragma prefast(pop)

static PwrshCommon sPwrshCommon;

// Gets the CLR Version for a given PowerShell Version. PowerShell Version is
// supplied with 2 parameters iPSMajorVersion (PowerShell major version) and
// iPSMinorVersion (PowerShell minor version). The CLR version is returned through
// pwszRuntimeVersion and pRuntimeVersionLength represents the size of pwszRuntimeVersion.
// returns: 0 on success, non-zero on failure.
_Success_(return == 0) //EXIT_CODE_SUCCESS
extern "C"
unsigned int GetCLRVersionForPSVersion(int iPSMajorVersion, 
                               int iPSMinorVersion,
                               size_t runtimeVersionLength,
                              __inout_ecount_part(runtimeVersionLength, *pRuntimeVersionLength) wchar_t* pwszRuntimeVersion,
                               __out_ecount(1) size_t* pRuntimeVersionLength)
{
    unsigned int exitCode = EXIT_CODE_SUCCESS;
    wchar_t * wszMonadVersion = NULL;
    wchar_t* wszConsoleHostAssemblyName = NULL;
    wchar_t* wszTempVersion = NULL;
    size_t tempVersionLength = 0;

    if (NULL != pRuntimeVersionLength)
    {
        // Initialize the output size to zero prior to attempting the copy
        *pRuntimeVersionLength = 0;
    }

    do
    {
        int requestedMonadMajorVersion = iPSMajorVersion;
        int requestedMonadMinorVersion = iPSMinorVersion;

        // For GetRegistryInfo call, monadMajorVersion is used to calculate the version key in registry.
        // For PowerShell V2, version key in registry is 1. 
        if (2 == requestedMonadMajorVersion)
        {
            requestedMonadMajorVersion = 1;
        }

        // For PowerShell 3, 4 and 5, the registry is 3.  
        if ((requestedMonadMajorVersion == 4) || (requestedMonadMajorVersion == 5))
        {
            requestedMonadMajorVersion = 3;
        }
        
        exitCode = ConstructPowerShellVersion(iPSMajorVersion, iPSMinorVersion, &wszMonadVersion);
        if (EXIT_CODE_SUCCESS != exitCode)
        {
            break;        
        }
        
        exitCode = sPwrshCommon.GetRegistryInfo(
                &wszMonadVersion,
                &requestedMonadMajorVersion,
                requestedMonadMinorVersion,
                &wszTempVersion,
                &wszConsoleHostAssemblyName);

        if (EXIT_CODE_SUCCESS != exitCode)
        {
            break;
        }

        HRESULT hResult = StringCchLength(wszTempVersion, STRSAFE_MAX_CCH, &tempVersionLength);

        if (FAILED(hResult))
        {
            exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
            break;
        }
        
        if (NULL != pwszRuntimeVersion)
        {
            // +1 for the '\0'
            if (runtimeVersionLength < (tempVersionLength + 1))
            {
                exitCode = EXIT_CODE_BAD_INPUT;
                break;
            }

            hResult = StringCchCopy(pwszRuntimeVersion, tempVersionLength + 1, wszTempVersion);

            if (FAILED(hResult))
            {
                exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
                break;
            }

            if (NULL != pRuntimeVersionLength)
            {
                // OACR warning 26030: Postcondition violation that could result in overflow
                // pRuntimeVersionLength should only be populated if the copy operation succeeded
                // +1 for the '\0'
                *pRuntimeVersionLength = tempVersionLength + 1;
            }
        }
    }while(false);

    if (NULL != wszMonadVersion)
    {
        delete [] wszMonadVersion;
        wszMonadVersion = NULL;
    }

    if (NULL != wszTempVersion)
    {
        delete [] wszTempVersion;
        wszTempVersion = NULL;
    }

    if (NULL != wszConsoleHostAssemblyName)
    {
        delete [] wszConsoleHostAssemblyName;
        wszConsoleHostAssemblyName = NULL;
    }

    return exitCode;
}

DWORD ReportOperationComplete(WSMAN_PLUGIN_REQUEST *requestDetails, DWORD errorCode)
{
    if (NULL == requestDetails)
    {
        // cannot report if requestDetails is NULL.
        return EXIT_CODE_SUCCESS;
    }

    DWORD result = EXIT_CODE_SUCCESS;
    PWSTR pwszErrorMessage = NULL;
    GetFormattedErrorMessage(&pwszErrorMessage, errorCode);
 
    result = WSManPluginOperationComplete(requestDetails, 0, errorCode, pwszErrorMessage);

    if (NULL != pwszErrorMessage)
    {
        delete[] pwszErrorMessage;
    }

    return result;
}

// -----------------------------------------------------------------------------
// Each plug-in needs to support the Startup callback.  A plug-in may be 
// initialized more than once within the same process, but only once per 
// applicationIdentification.
// -----------------------------------------------------------------------------
extern "C"
DWORD WINAPI WSManPluginStartup(
    __in DWORD flags,
    __in PCWSTR applicationIdentification,
    __in_opt PCWSTR extraInfo,
    __out PVOID *pluginContext
    )
{
// 
#ifdef REMOTINGDEBUG
     // This loop is added to assist debugging server.
    // Attach a debugger to the server and set this variable to true
    // from the debugger.
    bool isDebuggerAttached = false;
    do
    {
       Sleep(1);
    }while(!isDebuggerAttached);
#endif

    PwrshPlugIn* result = NULL;
    try
    {        
        *pluginContext = NULL;
        PwrshPlugInMediator* pluginMediator = PwrshPlugInMediator::GetPwrshPlugInMediator(extraInfo);

        PwrshPlugIn* result = pluginMediator->CreatePwrshPlugIn(applicationIdentification, extraInfo);
        *pluginContext = (PVOID)result;

        // Using global SQM session
        // WinSQMSetDWORD is not available on Vista

        // [Porting note] SQM is for Telemetry in Windows. Temporarily disabled.
        // HMODULE hModule;
        // PFN_WinSqmSetDWORD pfnWinSqmSetDWORD;

        // hModule = GetModuleHandleW(L"ntdll");
        // if (hModule)
        // {
        //     pfnWinSqmSetDWORD = (PFN_WinSqmSetDWORD) GetProcAddress(hModule, "WinSqmSetDWORD");
        //     if (pfnWinSqmSetDWORD)
        //     {
        //         pfnWinSqmSetDWORD(
        //             NULL,
        //             DATAID_WINRMREMOTEENABLED, 
        //             WINRM_SQM_DATA_REMOTEENABLED
        //             );
        //     }
        // }

        return NO_ERROR;
    }
    catch(PlugInException* e)
    {
        DWORD errorId = g_CREATION_FAILED;
        if (NULL != e)
        {
            errorId = e->dwMessageId;
            delete e;
        }

        if (NULL != result)
        {
            delete result;
        }

        return errorId;
    }
}

// ------------------------------------------------------------------------------------
//The WSManPluginShutdown method is called after all operations have been cancelled and
//right before the DLL is unloaded.  The DLL entry point name must be WSManPluginShutdown. 
//This method has an important purpose of making sure all plug-in threads are shut down 
//before this method returns.  If the plug-in only handles synchronous operations and all 
//threads report a cancellation result before they return then this method does not have to 
//do anything too complex other than plug-in cleanup.  However for an asynchronous plug-in, 
//any threads that are used to process the plug-in threads, including the ones that just reported 
//the cancellation for all operations need to completely shutdown.  Not doing this will cause
//potential crashes in the DLL because code may be executed after the DLL is unloaded.
// ------------------------------------------------------------------------------------
// reason: If this is a system shutdown this will be WSMAN_PLUGIN_SHUTDOWN_SYSTEM.  
// For WSMan service shutdown this will be WSMAN_PLUGIN_SHUTDOWN_SERVICE.  For an IIS host
//shutdown this will be WSMAN_PLUGIN_SHUTDOWN_IISHOST.
extern "C"
DWORD WINAPI WSManPluginShutdown(
    __in PVOID pluginContext,
    __in DWORD flags,
    __in DWORD reason
    )
{        
    if (NULL == pluginContext)
    {
        return g_NULL_PLUGIN_CONTEXT;
    }

    try
    {
        PwrshPlugInMediator* pluginMediator = PwrshPlugInMediator::GetPwrshPlugInMediator(NULL);
        pluginMediator->Shutdown(flags, reason);
    }
    catch(PlugInException* e)
    {
        // ignore plugin exceptions during shutdown.
        if (NULL != e)
        {         
            delete e;
        }
    }
    
    // free resources occupied by this plugin..
    // WSMan frees shell/command resources before calling
    // plugin shutdown.
    PwrshPlugIn* plugIn = reinterpret_cast<PwrshPlugIn*>(pluginContext);
    delete plugIn;

    return NO_ERROR;
}

#ifndef WIN32_FROM_HRESULT
#define WIN32_FROM_HRESULT(hr) (HRESULT_FACILITY(hr) == FACILITY_WIN32 ? HRESULT_CODE(hr) : hr) 
#endif

// -----------------------------------------------------------------------------
// A plug-in that supports the Shell operations needs to implement this callback
// to allow commands to be created and to allow data to be streamed into either
// a shell or command.  The plug-in must call WSManPluginReportContext to 
// report the shell context.  Once the shell is completed or when it is closed
// via the operationClosed boolean value or operationClosedHandle in the 
// requestDetails the plug-in needs to call WSManPluginOperationComplete.
// The shell is active until this time.
// -----------------------------------------------------------------------------
extern "C"
VOID WINAPI WSManPluginShell(
    __in PVOID pluginContext,
    __in WSMAN_PLUGIN_REQUEST *requestDetails,
    __in DWORD flags,
    __in_opt WSMAN_SHELL_STARTUP_INFO *startupInfo,
    __in_opt WSMAN_DATA *inboundShellInformation
    )
{
    PwrshPlugIn* plugIn = (NULL != pluginContext) ? (PwrshPlugIn*)pluginContext : NULL;
    bool comInitialized = false;

    if (NULL == plugIn)
    {
        ReportOperationComplete(requestDetails, g_INVALID_PLUGIN_CONTEXT);
        return;
    }

    try
    {
        PwrshPlugInMediator* pluginMediator = PwrshPlugInMediator::GetPwrshPlugInMediator(NULL);

        HRESULT hr = CoInitializeEx(0,COINIT_MULTITHREADED);
        if (hr == S_OK)
        {
            comInitialized = true;
        }
        else if (hr == S_FALSE)
        {
            CoUninitialize();
            comInitialized = false;
        }
        else if (hr == RPC_E_CHANGED_MODE)
        {
            comInitialized = false; //ignore 
        }
        else
        {
            ReportOperationComplete(requestDetails, WIN32_FROM_HRESULT(hr));
            return;
        }

        pluginMediator->CreateShell(plugIn, requestDetails, flags, startupInfo, inboundShellInformation);

        if (comInitialized)
        {
            CoUninitialize();
            comInitialized = false;
        }
    }
    catch(PlugInException* e)
    {
        DWORD errorId = g_CREATION_FAILED;
        if (NULL != e)
        {
            errorId = e->dwMessageId;
            delete e;
        }

        if (comInitialized)
        {
            CoUninitialize();
            comInitialized = false;
        }

        ReportOperationComplete(requestDetails, errorId);
    }
}

// -----------------------------------------------------------------------------
// WS-Man calls the WSMAN_PLUGIN_RELEASE_SHELL_CONTEXT entry point during shell 
// shutdown when it is safe to delete the plug-in shell context. Any context 
// reported through WSManPluginReportContext may not be deleted until the 
// corresponding release function has been called. Failure to follow the contract
// will result in errors being generated.
// -----------------------------------------------------------------------------
extern "C"
VOID WINAPI WSManPluginReleaseShellContext(__in PVOID shellContext)
{
    if ((NULL == shellContext))
    {
        return;
    }

    try
    {
        PwrshPlugInMediator* pluginMediator = PwrshPlugInMediator::GetPwrshPlugInMediator(NULL);
        pluginMediator->ReleaseShell(shellContext);
    }
    catch(PlugInException* e)
    {
        // ignore plugin exceptions.
        if (NULL != e)
        {         
            delete e;
        }
    }
}

//
// -----------------------------------------------------------------------------
// A plug-in that supports the Shell operations and needs to create commands
// that are associated with the shell needs to implement this callback.
// The plug-in must call WSManPluginReportContext to 
// report the command context.  Once the command is completed or when it is closed
// via the operationClosed boolean value or operationClosedHandle in the 
// requestDetails the plug-in needs to call WSManPluginOperationComplete.
// The command is active until this time.
// -----------------------------------------------------------------------------
extern "C"
VOID WINAPI WSManPluginCommand(
    __in WSMAN_PLUGIN_REQUEST *requestDetails,
    __in DWORD flags,
    __in PVOID shellContext,
    __in PCWSTR commandLine,
    __in_opt WSMAN_COMMAND_ARG_SET *arguments
)
{
    try
    {
        PwrshPlugInMediator* pluginMediator = PwrshPlugInMediator::GetPwrshPlugInMediator(NULL);
        pluginMediator->CreateCommand(requestDetails, flags, shellContext, commandLine, arguments);
    }
    catch(PlugInException* e)
    {
        DWORD errorId = g_CREATION_FAILED;
        if (NULL != e)
        {
            errorId = e->dwMessageId;
            delete e;
        }

        ReportOperationComplete(requestDetails, errorId);
    }
}

// ---------------------------------------------------------------------------------
// WS-Man calls the WSMAN_PLUGIN_RELEASE_COMMAND_CONTEXT entry point during command 
// shutdown when it is safe to delete the plug-in shell context. Any context 
// reported through WSManPluginReportContext may not be deleted until the 
// corresponding release function has been called. Failure to follow the contract
// will result in errors being generated.
// ---------------------------------------------------------------------------------
extern "C"
VOID WINAPI WSManPluginReleaseCommandContext(
    __in PVOID shellContext,
    __in PVOID commandContext
    )
{
    if ((NULL == shellContext) || (NULL == commandContext))
    {
        return;
    }

    try
    {
        PwrshPlugInMediator* pluginMediator = PwrshPlugInMediator::GetPwrshPlugInMediator(NULL);
        pluginMediator->ReleaseCommand(shellContext, commandContext);
    }
    catch(PlugInException* e)
    {
        // ignore plugin exceptions.
        if (NULL != e)
        {         
            delete e;
        }
    }
}

// -----------------------------------------------------------------------------
// A plug-in receives an inbound data stream to either the shell or command
// via this callback.  Each piece of data causes the callback to be called once.
// For each piece of data the plug-in calls WSManPluginResultComplete to 
// acknowledge receipt and to allow the next piece of data to be delivered.
// -----------------------------------------------------------------------------
extern "C"
VOID WINAPI WSManPluginSend(
     __in WSMAN_PLUGIN_REQUEST *requestDetails,
    __in DWORD flags,
    __in PVOID shellContext,
    __in_opt PVOID commandContext,
    __in PCWSTR stream,
    __in WSMAN_DATA *inboundData
     )
{
    try
    {
        PwrshPlugInMediator* pluginMediator = PwrshPlugInMediator::GetPwrshPlugInMediator(NULL);
        pluginMediator->SendOneItemToShellOrCommand(requestDetails, flags, shellContext, commandContext, stream, inboundData);
    }
    catch(PlugInException* e)
    {
        DWORD errorId = g_INVALID_PLUGIN_CONTEXT;
        if (NULL != e)
        {
            errorId = e->dwMessageId;
            delete e;
        }

        ReportOperationComplete(requestDetails, errorId);
    }
}

// -----------------------------------------------------------------------------
// A plug-in sends an outbound data stream from either the shell or command
// via this callback.  This API is called when an inbound request from a client
// is received.  This callback may be called against the shell and/or command
// based on the client request.  Each piece of data that needs to be sent back
// to the client is done so through the WSManPluginReceiveResult API.  Once 
// all data has been send, when the stream is terminated via some internal means,
// or if the receive call is cancelled through the operationClosed boolean 
// value or operationClosedHandle, the plug-in needs to call 
// WSManPluginResultComplete.  The operation is marked as active until this 
// time.
// -----------------------------------------------------------------------------
extern "C"
VOID WINAPI WSManPluginReceive(
    __in WSMAN_PLUGIN_REQUEST *requestDetails,
    __in DWORD flags,
    __in PVOID shellContext,
    __in_opt PVOID commandContext,
    __in_opt WSMAN_STREAM_ID_SET *streamSet
    )
{
    try
    {
        PwrshPlugInMediator* pluginMediator = PwrshPlugInMediator::GetPwrshPlugInMediator(NULL);
        pluginMediator->EnableShellOrCommandToSendDataToClient(requestDetails, flags, shellContext, commandContext, streamSet);
    }
    catch(PlugInException* e)
    {
        DWORD errorId = g_INVALID_PLUGIN_CONTEXT;
        if (NULL != e)
        {
            errorId = e->dwMessageId;
            delete e;
        }

        ReportOperationComplete(requestDetails, errorId);
    }
}

// -----------------------------------------------------------------------------
// A plug-in receives an inbound signal to either the shell or command
// via this callback.  Each signal causes the callback to be called once.
// For each callthe plug-in calls WSManPluginResultComplete to 
// acknowledge receipt and to allow the next signal to be received.
// A signal can cause the shell or command to be terminated, so the result
// of this callback may be many completion calls for the Signal, Receive, Command
// and Shell operations.
// -----------------------------------------------------------------------------
extern "C"
VOID WINAPI WSManPluginSignal(
                           __in WSMAN_PLUGIN_REQUEST *requestDetails,
                           __in DWORD flags,
                           __in PVOID shellContext,
                           __in_opt PVOID commandContext,
                           __in PCWSTR code)
{
    try
    {
        PwrshPlugInMediator* pluginMediator = PwrshPlugInMediator::GetPwrshPlugInMediator(NULL);
        pluginMediator->SignalShellOrCmd(requestDetails, flags, shellContext, commandContext, code);
    }
    catch(PlugInException* e)
    {
        DWORD errorId = g_INVALID_PLUGIN_CONTEXT;
        if (NULL != e)
        {
            errorId = e->dwMessageId;
            delete e;
        }

        ReportOperationComplete(requestDetails, errorId);
    }
}

extern "C"
VOID WINAPI WSManPluginConnect(
    __in WSMAN_PLUGIN_REQUEST *requestDetails,
    __in DWORD flags,
    __in PVOID shellContext,
    __in_opt PVOID commandContext,
    __in_opt WSMAN_DATA *inboundConnectInformation)
{
    try
    {
        PwrshPlugInMediator* pluginMediator = PwrshPlugInMediator::GetPwrshPlugInMediator(NULL);
        pluginMediator->ExecuteConnectToShellOrCommand(requestDetails, flags, shellContext, commandContext, inboundConnectInformation);
    }
    catch(PlugInException* e)
    {
        DWORD errorId = g_INVALID_PLUGIN_CONTEXT;
        if (NULL != e)
        {
            errorId = e->dwMessageId;
            delete e;
        }

        ReportOperationComplete(requestDetails, errorId);
    }
}
