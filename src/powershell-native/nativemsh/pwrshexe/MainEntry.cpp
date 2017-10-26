// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
//  File:      MainEntry.cpp
//
//  Contents:  Unmanaged startup point for powershell.exe console app.
//
// ----------------------------------------------------------------------

#ifdef _PREFAST_
#pragma prefast (push)
#pragma prefast (disable: 6054)
#endif /* _PREFAST_ */

#include "Nativemsh.h"
#include <windows.h>
#include <mscoree.h>
#include <muiload.h>
#include <shobjidl.h>
#include <propkey.h>
#include <propvarutil.h>
#include <shlobj.h>
#include <atlbase.h>
#include <msxml6.h>
#include <VersionHelpers.h>
#include "OutputWriter.h"
#include "ConfigFileReader.h"
#include "WinSystemCallFacade.h"

// include the tlb for mscorlib for access to the default AppDomain through COM Interop
#import <mscorlib.tlb> raw_interfaces_only high_property_prefixes("_get","_put","_putref")\
    rename("ReportEvent", "CLRReportEvent")

const UINT PS_TASK_NUM = 4;
#define CheckAndReturn(x) if (FAILED(x)){return (x);}

using namespace mscorlib;
using namespace NativeMsh;

LPCWSTR g_ISE_BINARY_PATH= L"%systemroot%\\system32\\windowspowershell\\v1.0\\powershell_ise.exe";
LPCWSTR g_ConsoleHostShortcutTarget_KEY_PATH = L"SOFTWARE\\Microsoft\\PowerShell\\3";
wchar_t g_PROFILE[] = L"profile.ps1";
wchar_t g_PROFILE_WITH_SHELL_ID[] = L"microsoft.powerShell_profile.ps1";
wchar_t g_PRODUCT_NAME[] = L"\\windowspowerShell\\";
wchar_t g_PSHOME_VERSION[] = L"v1.0\\";

WCHAR g_IconApp[MAX_PATH+1];

// All these objects will be destroyed when pwrshCommon goes out of scope.
PwrshExeOutput* pwrshExeOutput = new PwrshExeOutput();
PwrshCommon pwrshCommon(pwrshExeOutput, new ConfigFileReader(), new WinSystemCallFacade());

bool ConvertArgvToSafeArray(
    IN int argc,
    __in_ecount(argc) LPWSTR * argv,
    IN int skipIndex,
    __deref_out_opt SAFEARRAY ** ppSafeArray)
{
    // Collect the command line arguments to the managed exe
    SAFEARRAY *psa = NULL;
    SAFEARRAYBOUND rgsabound[1];
    bool returnResult = true;
    do
    {
        if (NULL == argv || NULL == ppSafeArray)
        {
            returnResult = false;
            break;
        }

        rgsabound[0].lLbound = 0;

        // rgsabound[0].cElements holds the number of elements that need to be passed to managed exe
        rgsabound[0].cElements = argc - 1 - skipIndex;
        psa = SafeArrayCreate(VT_BSTR, 1, rgsabound);

        if (psa == NULL)
        {
            returnResult = false;
            break;
        }

        long psaIndex[1];
        psaIndex[0] = 0;

        if (0 != rgsabound[0].cElements)
        {
            // Skip elements from 1 to argc-rgsabound[0].cElements-1
            for (int i = skipIndex+1; i < argc; i++)
            {
                BSTR bArg = SysAllocString(argv[i]);
                if (NULL == bArg)
                {
                    returnResult = false;
                    break;
                }
                HRESULT hrSafeArrayPutElement = SafeArrayPutElement(psa, psaIndex, bArg);
                // Free bArg
                SysFreeString(bArg);
                psaIndex[0]++;

                if (FAILED(hrSafeArrayPutElement))
                {
                    returnResult = false;
                    break;
                }
            }
        }
    }
    while (false);
    *ppSafeArray = psa;
    return returnResult;
}

bool FileExists(__in LPWSTR pszFileName)
{
    return (GetFileAttributesW(pszFileName) != INVALID_FILE_ATTRIBUTES);
}

unsigned int LaunchManagedMonad(
    LPCWSTR wszMonadVersion,
    int monadMajorVersion,
    LPCWSTR wszConsoleFile,
    LPCWSTR wszRuntimeVersion,
    LPCWSTR wszConsoleHostAssemblyName,
    __in_ecount_opt(1) SAFEARRAY * pArgvSA,
    int skipIndex,
    int argc,
    __in_ecount(argc) LPWSTR * argv)
{
    unsigned int exitCode = EXIT_CODE_SUCCESS;
    HRESULT hr = S_OK;
    BSTR bstrConsoleFile = NULL;
    do
    {
        // don't check StringIsNullOrEmpty(wszConsoleHostAssemblyName) here
        // because it will check below with better error reporting
        if (pwrshCommon.StringIsNullOrEmpty(wszMonadVersion) ||
            pwrshCommon.StringIsNullOrEmpty(wszRuntimeVersion))
        {
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }
        // Use the hosting interfaces from .Net Framework 1.1
        CComPtr<ICorRuntimeHost> pCLR = NULL;

        exitCode = pwrshCommon.LaunchCLR(wszMonadVersion, wszRuntimeVersion, &pCLR);

        if (EXIT_CODE_INIT_FAILURE == exitCode)
        {
            break;
        }

        if(!ConvertArgvToSafeArray(
                argc,
                argv,
                skipIndex,
                &pArgvSA))
        {
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }

        // Get a pointer to the default AppDomain
        CComPtr<_AppDomain> spDefaultDomain = NULL;
        CComPtr<IUnknown>   spAppDomainPunk = NULL;

        hr = pCLR->GetDefaultDomain(&spAppDomainPunk);
        if (FAILED(hr) || spAppDomainPunk == NULL)
        {
            pwrshExeOutput->DisplayMessage(false, g_GETTING_DEFAULT_DOMAIN_FAILED, hr);
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }

        hr = spAppDomainPunk->QueryInterface(__uuidof(_AppDomain), (PVOID*) &spDefaultDomain);
        if (FAILED(hr) || spDefaultDomain == NULL)
        {
            pwrshExeOutput->DisplayMessage(false, g_GETTING_DEFAULT_DOMAIN_FAILED, hr);
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }

        CComPtr<_ObjectHandle> spObjectHandle;

        // use CreateInstance because we use the assembly strong name (as opposed to CreateInstanceFrom)
        _bstr_t bstrConsoleHostAssemblyName = _bstr_t(wszConsoleHostAssemblyName);
        _bstr_t bstrUnmanagedMshEntryClass = _bstr_t(L"Microsoft.PowerShell.UnmanagedPSEntry");

        hr = spDefaultDomain->CreateInstance(
                bstrConsoleHostAssemblyName,
                bstrUnmanagedMshEntryClass,
                &spObjectHandle);
        if (FAILED(hr) || spObjectHandle == NULL)
        {
            pwrshExeOutput->DisplayMessage(false, g_CREATING_MSH_ENTRANCE_FAILED, hr);
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }

        CComVariant VntUnwrapped;
        hr = spObjectHandle->Unwrap(&VntUnwrapped);
        if (FAILED(hr))
        {
            pwrshExeOutput->DisplayMessage(false, g_CREATING_MSH_ENTRANCE_FAILED, hr);
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }

        CComPtr<IDispatch> pDisp;
        pDisp = VntUnwrapped.pdispVal;

        OLECHAR FAR * wszMember = L"Start";

        DISPID dispid;
        //Retrieve the DISPID
        hr = pDisp->GetIDsOfNames (
            IID_NULL,
            &wszMember,
            1,
            LOCALE_SYSTEM_DEFAULT,
            &dispid);

        if (FAILED(hr))
        {
            pwrshExeOutput->DisplayMessage(false, g_GETTING_DISPATCH_ID_FAILED, hr);
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }

        VARIANT pVarArgs[2];

        // Both EnterWithConsoleFile and EnterWithConsoleFile take a string and a string array
        // The order of the arguments need to be reversed in this array
        pVarArgs[0].vt = VT_ARRAY;
        pVarArgs[0].parray = pArgvSA;

        // NTRAID#Windows Out Of Band Releases-918924
        // using _bstr_t here fails consistently on one single machine.
        // changing the call to SysAllocString fixes the problem.
        if (wszConsoleFile != NULL)
        {
            bstrConsoleFile = SysAllocString(wszConsoleFile);
            if (NULL == bstrConsoleFile)
            {
                exitCode = EXIT_CODE_INIT_FAILURE;
                break;
            }
        }
        pVarArgs[1].vt = VT_BSTR;
        pVarArgs[1].bstrVal = bstrConsoleFile;
        DISPPARAMS dispparamsTwoArgs = {pVarArgs, NULL, 2};

        VARIANT varResult;
        varResult.vt = VT_UINT;
        varResult.uintVal = 0x00000000;
        EXCEPINFO exception;
        unsigned int uArgErr = 0;

        //Invoke the method on the Dispatch Interface
        hr = pDisp->Invoke(
                        dispid,
                        IID_NULL,
                        LOCALE_SYSTEM_DEFAULT,
                        DISPATCH_METHOD,
                        &dispparamsTwoArgs,
                        &varResult,
                        &exception,
                        &uArgErr
                        );
        exitCode = varResult.uintVal;
        if (FAILED(hr))
        {
            if (DISP_E_EXCEPTION == hr)
            {
                pwrshExeOutput->DisplayMessage(false, g_MANAGED_MSH_EXCEPTION, exception.bstrDescription);
            }
            else
            {
                pwrshExeOutput->DisplayMessage(false, g_INOVKING_MSH_ENTRANCE_FAILED, hr);
            }
            exitCode = EXIT_CODE_INIT_FAILURE;
            break;
        }
    }
    while (false);

    // No need to call pCLR->Stop() because,
    //  as the common language runtime is automatically unloaded when the process exits.

    SysFreeString(bstrConsoleFile);
    return exitCode;
}

static bool IsDash(WCHAR wch)
{
    const WCHAR enDash = (WCHAR)0x2013;
    const WCHAR emDash = (WCHAR)0x2014;
    const WCHAR horizontalBar = (WCHAR)0x2015;
    return (enDash == wch || emDash == wch || horizontalBar == wch || '-' == wch);
}

static bool IsParameterMatched(
    LPCWSTR wszParameter,
    int cchParameter,
    LPCWSTR wszCommandLineInput)
{

    assert(cchParameter > 1 && !pwrshCommon.StringIsNullOrEmpty(wszParameter) &&
        wszCommandLineInput != NULL);

    // A parameter has to start with a dash...
    if (!IsDash(wszCommandLineInput[0])) return false;

    // Skip over the dash character...
    wszCommandLineInput = _wcsinc(wszCommandLineInput);

    if (pwrshCommon.StringIsNullOrEmpty(wszCommandLineInput)) return false;

    bool bReturnResult = true;
    do
    {
        if (cchParameter <= 1 || pwrshCommon.StringIsNullOrEmpty(wszParameter)
            && pwrshCommon.StringIsNullOrEmpty(wszCommandLineInput))
        {
            bReturnResult = false;
            break;
        }
        if (L'\0' == *wszCommandLineInput)
        {
            bReturnResult = false;
            break;
        }
        size_t size_tCommandLineInput = 0;
        // Since cchParameter is no larger than STRSAFE_MAX_CCH and wszCommandLineInput can't be
        //  null, StringCchLength won't return S_OK iff wszCommandLineInput is longer than
        //  cchParameter, in which case wszCommandLineInput is not a prefix of wszParameter
        if (FAILED(StringCchLength(wszCommandLineInput, cchParameter, &size_tCommandLineInput)))
        {
            bReturnResult = false;
            break;
        }
        int cchCommandLineInput = (int)size_tCommandLineInput;

        // string function
        // since we know wszCommandLineInput is no longer than wszParameter, just compare the two strings
        // up to the length of wszCommandLineInput
        int comparison = CompareStringW(
            LOCALE_INVARIANT,
            NORM_IGNORECASE,
            wszParameter,
            cchCommandLineInput,       // compare up to the size of wszRHSting
            wszCommandLineInput,
            cchCommandLineInput);

        assert(comparison);
        if (0 == comparison)
        {
            bReturnResult = false;
            break;
        }
        bReturnResult = comparison == CSTR_EQUAL;
    } while (false);
    return bReturnResult;
}

static bool CheckConsoleFileExtension(
    LPCWSTR wszFileName)
{
    bool bReturnResult = true;
    do
    {
        if (pwrshCommon.StringIsNullOrEmpty(wszFileName))
        {
            bReturnResult = false;
            break;
        }
        LPCWSTR dot = wcsrchr(wszFileName, L'.');
        if (NULL == dot)
        {
            bReturnResult = false;
            break;
        }
        //string function
        LPCWSTR wszMonadConsoleFileExtension = L".psc1";
        const int cchMonadConsoleFileExtension = 5;
        int comparison = CompareStringW(
            LOCALE_INVARIANT,
            NORM_IGNORECASE,
            dot,
            -1,
            wszMonadConsoleFileExtension,
            cchMonadConsoleFileExtension);
        assert(comparison);
        if (0 == comparison)
        {
            bReturnResult = false;
            break;
        }
        bReturnResult = comparison == CSTR_EQUAL;
    } while (false);
    if (!bReturnResult)
    {
        pwrshExeOutput->DisplayMessage(false, g_INCORRECT_CONSOLE_FILE_EXTENSION, wszFileName);
    }
    return bReturnResult;
}

bool VerifyConsoleSchemaVersion(
    LPCWSTR wszConsoleSchemaVersion,
    LPCWSTR wszFileName)
{
    bool returnResult = true;
    do
    {
        if (pwrshCommon.StringIsNullOrEmpty(wszFileName))
        {
            returnResult = false;
            break;
        }

        size_t cch = 0;
        // make sure cch is in the range of int for the below call to CompareStringW
        //string function
        if (FAILED(StringCchLength(wszConsoleSchemaVersion, INT_MAX, &cch)))
        {
            pwrshExeOutput->DisplayMessage(false, g_READ_XML_INVALID_CONSOLE_SCHEMA_VERSION, wszFileName);
            returnResult = false;
            break;
        }
        //string function
        LPCWSTR wszSupportedConsoleSchemaVersion = L"1.0";
        const int cchSupportedConsoleSchemaVersion = 3;
        if (CSTR_EQUAL != CompareStringW(
            LOCALE_INVARIANT,
            0,
            wszSupportedConsoleSchemaVersion,
            cchSupportedConsoleSchemaVersion,
            wszConsoleSchemaVersion, (int)cch))
        {
            pwrshExeOutput->DisplayMessage(false, g_READ_XML_INVALID_CONSOLE_SCHEMA_VERSION, wszFileName);
            returnResult = false;
            break;
        }
    } while (false);
    return returnResult;
}

_Success_(return)
bool ReadConsoleSchemaVersion(
    CComPtr<IXMLDOMNode> spRoot,
    LPCWSTR wszFileName,
    __deref_out_opt PWSTR * pwszConsoleSchemaVersion)
{
    bool returnResult = true;
    CComPtr<IXMLDOMNode> spMshConsoleFile = NULL;
    CComPtr<IXMLDOMNamedNodeMap> spMshConsoleFileAttrs = NULL;
    CComPtr<IXMLDOMNode> spConsoleSchemaVersion = NULL;
    HRESULT hr = 0;
    do
    {
        assert(!pwrshCommon.StringIsNullOrEmpty(wszFileName) &&
            spRoot != NULL &&
            pwszConsoleSchemaVersion != NULL);
        if (pwrshCommon.StringIsNullOrEmpty(wszFileName) ||
            spRoot == NULL ||
            NULL == pwszConsoleSchemaVersion)
        {
            returnResult = false;
            break;
        }
        _bstr_t bstrMshConsoleFileXPath = _bstr_t(L"/PSConsoleFile");
        hr = spRoot->selectSingleNode(bstrMshConsoleFileXPath, &spMshConsoleFile);
        if (FAILED(hr) || spMshConsoleFile == NULL)
        {
            pwrshExeOutput->DisplayMessage(false, g_READ_XML_GET_PSCONSOLEFILE_FAILED, wszFileName);
            returnResult = false;
            break;
        }

        hr = spMshConsoleFile->get_attributes(&spMshConsoleFileAttrs);
        if (FAILED(hr) || spMshConsoleFileAttrs == NULL)
        {
            pwrshExeOutput->DisplayMessage(false, g_READ_XML_GET_CONSOLE_SCHEMA_VERSION_FAILED, wszFileName);
            returnResult = false;
            break;
        }

        _bstr_t bstrConsoleSchemaVersionAttr = _bstr_t(L"ConsoleSchemaVersion");
        hr = spMshConsoleFileAttrs->getNamedItem(bstrConsoleSchemaVersionAttr, &spConsoleSchemaVersion);
        if (FAILED(hr) || spConsoleSchemaVersion == NULL)
        {
            pwrshExeOutput->DisplayMessage(false, g_READ_XML_GET_CONSOLE_SCHEMA_VERSION_FAILED, wszFileName);
            returnResult = false;
            break;
        }

        BSTR bstrConsoleSchemaVersion = NULL;
        hr = spConsoleSchemaVersion->get_text(&bstrConsoleSchemaVersion);

        if (FAILED(hr))
        {
            pwrshExeOutput->DisplayMessage(false, g_READ_XML_GET_CONSOLE_SCHEMA_VERSION_FAILED, wszFileName);
            returnResult = false;
            break;
        }

        *pwszConsoleSchemaVersion = bstrConsoleSchemaVersion;
    } while (false);
    return returnResult;
}

void DisplaySpecificXMLDOMError(
    CComPtr<IXMLDOMDocument> spXMLDoc,
    LPCWSTR wszFileName)
{
    bool bIsSpecificErrorDisplayed = true;
    BSTR bstrReason = NULL;
    do
    {
        assert(spXMLDoc != NULL && !pwrshCommon.StringIsNullOrEmpty(wszFileName));
        if (spXMLDoc == NULL || pwrshCommon.StringIsNullOrEmpty(wszFileName))
        {
            break;
        }
        // check for file existence, permission, etc first because
        // IXMLDOMParseError's message is not as descriptive
        HANDLE xmlFileHandle = CreateFile(wszFileName,
            FILE_READ_DATA,
            FILE_SHARE_READ,
            NULL,
            OPEN_EXISTING,
            0,
            NULL);
        if (INVALID_HANDLE_VALUE == xmlFileHandle)
        {
            LONG systemErrorCode = GetLastError();
            pwrshExeOutput->DisplayErrorWithSystemError(systemErrorCode,
                g_READ_XML_LOAD_FILE_FAILED_WITH_SPECIFIC_ERROR,
                wszFileName);
            break;
        }
        CloseHandle(xmlFileHandle);

        CComPtr<IXMLDOMParseError> spIParseError = NULL;
        HRESULT hr = spXMLDoc->get_parseError(&spIParseError);
        if (FAILED(hr) || spIParseError == NULL)
        {
            bIsSpecificErrorDisplayed = false;
            break;
        }
        hr = spIParseError->get_reason(&bstrReason);
        if (FAILED(hr) || (NULL == bstrReason))
        {
            bIsSpecificErrorDisplayed = false;
            break;
        }
        long line = -1;
        hr = spIParseError->get_line(&line);
        if (FAILED(hr) || (line == -1))
        {
            bIsSpecificErrorDisplayed = false;
            break;
        }
        long linepos = -1;
        hr = spIParseError->get_linepos(&linepos);
        if (FAILED(hr) || (linepos == -1))
        {
            bIsSpecificErrorDisplayed = false;
            break;
        }
        pwrshExeOutput->DisplayMessage(false,
            g_READ_XML_LOAD_FILE_FAILED_WITH_SPECIFIC_POSITION_ERROR,
            wszFileName,
            bstrReason,
            line,
            linepos);
    } while (false);
    SysFreeString(bstrReason);
    if (!bIsSpecificErrorDisplayed)
    {
        // if displaying specific error failed, display a generic error
        pwrshExeOutput->DisplayMessage(false, g_READ_XML_LOAD_FILE_FAILED, wszFileName);
    }
}

static bool ReadVersionFromConsoleFile(
    LPCWSTR wszFileName,
    __deref_out_opt PWSTR * pwszMonadVersion,
    __out_ecount(1) int * lpMonadMajorVersion,
    __out_ecount(1) int * lpMonadMinorVersion)
{
    assert(pwszMonadVersion);
    if (NULL == pwszMonadVersion ||
        NULL == lpMonadMajorVersion || NULL == lpMonadMinorVersion)
    {
        return false;
    }
    *pwszMonadVersion = NULL;
    *lpMonadMajorVersion = *lpMonadMinorVersion = -1;
    if (NULL == wszFileName)
    {
        pwrshExeOutput->DisplayMessage(true, g_INVALID_CONSOLE_FILE_PATH, L"NULL");
        return false;
    }
    bool returnResult = true;
    bool coInited = false;
    BSTR bstrVersion = NULL;
    do
    {
        HRESULT hr = S_OK;
        CComVariant xmlSource = wszFileName;
        CComPtr<IXMLDOMDocument> spXMLDoc = NULL;
        CComPtr<IXMLDOMElement> spDocElem = NULL;
        CComPtr<IXMLDOMNode> spRoot = NULL;
        CComPtr<IXMLDOMNode> spVersion = NULL;
        hr =
            CoInitializeEx(
            NULL,
            COINIT_MULTITHREADED);

        if (FAILED(hr))
        {
            pwrshExeOutput->DisplayMessage(false, g_READ_XML_COM_INIT_FAILED, hr);
            returnResult = false;
            break;
        }
        coInited = true;
        hr = CoCreateInstance(
            CLSID_DOMDocument60, // by default, ProhibitDTD = true, ResolveExternals = false, UseInlineSchema = false
            NULL,
            CLSCTX_INPROC_SERVER,
            IID_IXMLDOMDocument,
            (void **)&spXMLDoc);
        if (FAILED(hr))
        {
            pwrshExeOutput->DisplayMessage(false, g_READ_XML_CREATE_DOMDOCUMENT_FAILED, hr);
            returnResult = false;
            break;
        }

        spXMLDoc->put_async(false);

        VARIANT_BOOL bIsSuccessful = 1;
        hr = spXMLDoc->load(xmlSource, &bIsSuccessful);
        if (FAILED(hr) || !bIsSuccessful)
        {
            DisplaySpecificXMLDOMError(spXMLDoc, wszFileName);
            returnResult = false;
            break;
        }

        hr = spXMLDoc->get_documentElement(&spDocElem);
        if (FAILED(hr) || spDocElem == NULL)
        {
            pwrshExeOutput->DisplayMessage(false, g_READ_XML_GET_MONAD_VERSION_TEXT_FAILED, wszFileName);
            returnResult = false;
            break;
        }

        hr = spDocElem->get_firstChild(&spRoot);
        if (FAILED(hr) || spRoot == NULL)
        {
            pwrshExeOutput->DisplayMessage(false, g_READ_XML_GET_MONAD_VERSION_TEXT_FAILED, wszFileName);
            returnResult = false;
            break;
        };
        LPWSTR wszConsoleSchemaVersion = NULL;
        if (!ReadConsoleSchemaVersion(spRoot, wszFileName, &wszConsoleSchemaVersion))
        {
            returnResult = false;
            break;
        }
        if (!VerifyConsoleSchemaVersion(wszConsoleSchemaVersion, wszFileName))
        {
            returnResult = false;
            break;
        }

        _bstr_t bstrMshVersionTextXPath = _bstr_t(L"/PSConsoleFile/PSVersion/text()");
        hr = spRoot->selectSingleNode(bstrMshVersionTextXPath, &spVersion);
        if (FAILED(hr))
        {
            pwrshExeOutput->DisplayMessage(false, g_READ_XML_GET_MONAD_VERSION_TEXT_FAILED, wszFileName);
            returnResult = false;
            break;
        }
        if (spVersion == NULL)
        {
            pwrshExeOutput->DisplayMessage(false, g_READ_XML_GET_EMPTY_MONAD_VERSION_TEXT, wszFileName);
            returnResult = false;
            break;
        }

        hr = spVersion->get_text(&bstrVersion);
        if (FAILED(hr))
        {
            pwrshExeOutput->DisplayMessage(false, g_READ_XML_GET_MONAD_VERSION_TEXT_FAILED, wszFileName);
            returnResult = false;
            break;
        }
        if (!pwrshCommon.VerifyMonadVersionFormat(bstrVersion, lpMonadMajorVersion, lpMonadMinorVersion, true, true))
        {
            returnResult = false;
            break;
        }
        // SysStringLen does not include terminating null
        UINT wszVersionLength = SysStringLen(bstrVersion) + 1;
        WCHAR * wszVersion = new WCHAR[wszVersionLength];
        if (NULL == wszVersion)
        {
            returnResult = false;
            break;
        }
        if (FAILED(StringCchCopy(wszVersion, wszVersionLength, bstrVersion)))
        {
            returnResult = false;
            break;
        }
        *pwszMonadVersion = wszVersion;
    } while (false);
    if (coInited)
    {
        CoUninitialize();
    }
    SysFreeString(bstrVersion);
    return returnResult;
}

#pragma prefast(push)
#pragma prefast (disable: 6101) // Returning uninitialized memory - pwszRuntimeVersion, pwszConsoleFile, pwszMonadVersion, and pwszRuntimeVersion are not always set on success.


unsigned int ParseCommandLineArguments(
                                IN int argc,
                                __in_ecount(argc) LPWSTR * argv,
                                __deref_out_opt PWSTR * pwszMonadVersion,
                                __out_ecount(1) int * lpMonadMajorVersion,
                                __out_ecount(1) int * lpMonadMinorVersion,
                                __inout_ecount(1) int * lpMonadVersionIndex,
                                __deref_out_opt PWSTR * pwszRuntimeVersion,
                                __out_ecount(1) int * lpRuntimeVersionIndex,
                                __deref_out_opt PWSTR * pwszConsoleFile,
                                __inout_ecount(1) int * lpConsoleFileIndex,
                                __inout_ecount(1) int * lpProfileIndex)
{
    const wchar_t * wszArgumentVersion = L"version";
    const int cchArgumentVersion = 8;       // including null terminating
    const wchar_t * wszArgumentMonadConsole = L"psconsolefile";
    const int cchArgumentMonadConsole = 15; // including null terminating
    const wchar_t * wszArgumentRuntimeVersion = L"runtimeversion";
    const int cchArgumentRuntimeVersion = 15; // including null terminating
    const wchar_t * wszArgumentMonadProfile = L"noprofile";
    const int cchArgumentMonadProfile = 10; // including null terminating
    const wchar_t * wszArgumentMonadProfileShort = L"nop";
    const int cchArgumentMonadProfileShort = 4; // including null terminating
    const wchar_t * wszArgumentServerMode20 = L"servermode";
    const int cchArgumentServerMode20 = 11; // including null terminating
    const wchar_t * wszArgumentServerMode20Short = L"s";
    const int cchArgumentServerMode20Short = 2; // including null terminating
    bool bServerMode20Specified = false;

    int version_lpMonadMajorVersion = -1;
    int version_lpMonadMinorVersion = -1;
    wchar_t *  version_pwszMonadVersion = NULL;

    unsigned int exitCode = EXIT_CODE_SUCCESS;
    do
    {
        assert(pwszMonadVersion && lpMonadVersionIndex && pwszConsoleFile && lpConsoleFileIndex && pwszRuntimeVersion && lpRuntimeVersionIndex);
        if (NULL == pwszMonadVersion ||
            NULL == lpMonadMajorVersion ||
            NULL == lpMonadMinorVersion ||
            NULL == lpMonadVersionIndex ||
            NULL == pwszRuntimeVersion ||
            NULL == lpRuntimeVersionIndex ||
            NULL == pwszConsoleFile ||
            NULL == lpConsoleFileIndex ||
            NULL == lpProfileIndex )
        {
            exitCode = EXIT_CODE_BAD_COMMAND_LINE_PARAMETER;
            break;
        }

        *lpMonadVersionIndex = *lpMonadMinorVersion = -1;
        if (1 >= argc)
        {
            // no parameters given
            break;
        }


        int idxParameterPosition = 1;
        LPCWSTR wszCommandLineInput = argv[idxParameterPosition];

        // Parse for -version
        if (IsParameterMatched(wszArgumentVersion, cchArgumentVersion, wszCommandLineInput))
        {
            if (idxParameterPosition < argc - 1)
            {
                idxParameterPosition++;
                if (!pwrshCommon.VerifyMonadVersionFormat(argv[idxParameterPosition], lpMonadMajorVersion, lpMonadMinorVersion, true, true))
                {
                    exitCode = EXIT_CODE_BAD_COMMAND_LINE_PARAMETER;
                    break;
                }
                *lpMonadVersionIndex = idxParameterPosition - 1;
                *pwszMonadVersion = argv[idxParameterPosition];

                // Save the version values in case we need to override the version in psconsolefile
                version_lpMonadMajorVersion = *lpMonadMajorVersion;
                version_lpMonadMinorVersion = *lpMonadMinorVersion;
                version_pwszMonadVersion = argv[idxParameterPosition];

                if (idxParameterPosition < argc - 1)
                {
                    idxParameterPosition = idxParameterPosition + 1;
                    wszCommandLineInput = argv[idxParameterPosition];
                }
            }
            else
            {
                pwrshExeOutput->DisplayMessage(
                    false,
                    g_MISSING_COMMAND_LINE_ARGUMENT,
                    wszArgumentVersion);
                exitCode = EXIT_CODE_BAD_COMMAND_LINE_PARAMETER;
                break;
            }
        }

        // Parse for -ServerMode.
        // PowerShell 2.0's Start-Job using the string "-s -nologo -noprofile". To let
        // powershell 2.0 launch a 2.0 background job, we are hardcoding the version here.
        // PowerShell 3.0 and beyond, will be using the string "-s -version <> -noprofile -nologo".
        // The -Version parameter processed before should take care of loading correct
        // powershell in 3.0 and beyond case.
        if ((IsParameterMatched(wszArgumentServerMode20, cchArgumentServerMode20, wszCommandLineInput) ||
            IsParameterMatched(wszArgumentServerMode20Short, cchArgumentServerMode20Short, wszCommandLineInput)) &&
            (*lpMonadVersionIndex == -1))
        {
            idxParameterPosition++;
            *lpMonadMajorVersion = 2;
            *lpMonadMinorVersion = -1;
            bServerMode20Specified = true;
            *pwszMonadVersion = L"2.0";

            // let the rest of the parameter processing happen in managed code for -ServerMode
            // hence not updating wszCommandLineInput
        }

        // Parse for CLR Version
        if (IsParameterMatched(wszArgumentRuntimeVersion, cchArgumentRuntimeVersion, wszCommandLineInput))
        {
            if (idxParameterPosition < argc - 1)
            {
                idxParameterPosition++;

                // No need for validation of CLR version, it is validated outside of the
                // parameter processing code (because we might also read it from the registry)
                *lpRuntimeVersionIndex = idxParameterPosition - 1;
                *pwszRuntimeVersion = argv[idxParameterPosition];

                if (idxParameterPosition < argc - 1)
                {
                    idxParameterPosition = idxParameterPosition + 1;
                    wszCommandLineInput = argv[idxParameterPosition];
                }
            }
            else
            {
                pwrshExeOutput->DisplayMessage(
                    false,
                    g_MISSING_COMMAND_LINE_ARGUMENT,
                    wszArgumentRuntimeVersion);
                exitCode = EXIT_CODE_BAD_COMMAND_LINE_PARAMETER;
                break;
            }
        }

        // Parse for MshConsoleFile
        if (IsParameterMatched(wszArgumentMonadConsole, cchArgumentMonadConsole, wszCommandLineInput))
        {
            if (idxParameterPosition < argc - 1)
            {
                idxParameterPosition++;
                if (!CheckConsoleFileExtension(argv[idxParameterPosition]))
                {
                    exitCode = EXIT_CODE_READ_CONSOLE_FILE_FAILURE;
                    break;
                }
                if (!ReadVersionFromConsoleFile(argv[idxParameterPosition], pwszMonadVersion,
                                    lpMonadMajorVersion, lpMonadMinorVersion))
                {
                    exitCode = EXIT_CODE_READ_CONSOLE_FILE_FAILURE;
                    break;
                }
                *lpConsoleFileIndex = idxParameterPosition - 1;
                *pwszConsoleFile = argv[idxParameterPosition];
            }
            else
            {
                pwrshExeOutput->DisplayMessage(
                    false,
                    g_MISSING_COMMAND_LINE_ARGUMENT,
                    wszArgumentMonadConsole);
                exitCode = EXIT_CODE_BAD_COMMAND_LINE_PARAMETER;
                break;
            }
        }

        // Parse for NoProfile
        if (IsParameterMatched(wszArgumentMonadProfile, cchArgumentMonadProfile, wszCommandLineInput) ||
            IsParameterMatched(wszArgumentMonadProfileShort, cchArgumentMonadProfileShort, wszCommandLineInput))
        {
            idxParameterPosition++;
            *lpProfileIndex = idxParameterPosition - 1;
        }

        // If version parameter is not specified, always load the PowerShell 3.0
        if ((*lpMonadVersionIndex == -1) && (!bServerMode20Specified))
        {
            *lpMonadMajorVersion = 3;
            *lpMonadMinorVersion = -1;

            wchar_t * wszConsoleFile = NULL;
            wchar_t * wszConsoleHostAssemblyName = NULL;
            wchar_t * wszRuntimeVersion = NULL;
            wchar_t * tempMonadVersion = NULL;

            // This gets the Monad Version from the registry
            exitCode = pwrshCommon.GetRegistryInfo(
                &tempMonadVersion,
                lpMonadMajorVersion,
                *lpMonadMinorVersion,
                &wszRuntimeVersion,
                &wszConsoleHostAssemblyName);

            if (EXIT_CODE_SUCCESS != exitCode)
            {
                break;
            }

            // This holds 3.0
            *pwszMonadVersion = tempMonadVersion;
        }

        // In case both version and psconsolefile are specified, load the powershell specified in the version parameter.
        if (*lpMonadVersionIndex != -1 && *lpConsoleFileIndex != -1)
        {
            *lpMonadMajorVersion = version_lpMonadMajorVersion;
            *lpMonadMinorVersion = version_lpMonadMinorVersion;
            *pwszMonadVersion = version_pwszMonadVersion;
        }

    }
    while(false);

    return exitCode;
}

#pragma prefast(pop)

/*********************************************************
 Loads a string from the modules resource
**********************************************************/
HRESULT SafeLoadString(UINT uId, __deref_out LPWSTR* ppszString)
{
    HINSTANCE hInstance = NULL;
    HRESULT hr = S_OK;

    // get a handle to the current application
    hInstance = GetModuleHandle(NULL);

    if (hInstance != NULL)
    {
        LPWSTR pszString = NULL;
        pszString = static_cast<LPWSTR>(::malloc(sizeof(WCHAR) * (100 + 1)));

        if (pszString != NULL)
        {
            DWORD length = ::LoadStringW(hInstance, uId, pszString, 100);

            if ( length > 0 && length < 100)
            {
                *ppszString = pszString;
            }
            else
            {
                // string with specified resource id is longer,
                // quit without loading
                free(pszString);
                hr = HRESULT_FROM_WIN32(::GetLastError());
            }
        }
        else
        {
            // Allocation failed
            hr = HRESULT_FROM_WIN32(::GetLastError());
        }
        return hr;
    }
    hr = HRESULT_FROM_WIN32(::GetLastError());
    return hr;
}

#ifdef _PREFAST_
#pragma prefast (pop)
#endif /* _PREFAST_ */

#if (WINVER >= _WIN32_WINNT_WIN7)

/*************************************************************************
Creates a shortcut from the specified properties

ppShortCut will contain the IShellLink that represents this
shortcut

Note that the above short cut will not be stored on disk
**************************************************************************/
HRESULT CreateShortCut(PCWSTR pszDisplay, PCWSTR pszAppPath, PCWSTR pszDescription,
                       PCWSTR pszArguments, PCWSTR pszIconAppPath, int iIconIndex, bool requiresElevation,
                       bool console,IShellLink **ppShortCut)
{
    CComPtr<IShellLink> pShellLink;     // pointer to a shell link

    HRESULT hr = pShellLink.CoCreateInstance(CLSID_ShellLink, NULL, CLSCTX_INPROC_SERVER);

    CheckAndReturn(hr);

    // set the path to the application
    hr = pShellLink->SetPath(pszAppPath);
    CheckAndReturn(hr);

    // set the application arguments
        hr = pShellLink->SetArguments(pszArguments);
    CheckAndReturn(hr);

    // set the icon index
    hr = pShellLink->SetIconLocation(pszIconAppPath, iIconIndex);
    CheckAndReturn(hr);

    // IShellLink contains a property store
    // the title for a task should be contained
    // in the PKEY_Title property in this
    // property store
    CComPtr<IPropertyStore> pPropertyStore;

    // query for interface
    hr = pShellLink->QueryInterface(IID_PPV_ARGS(&pPropertyStore));
    CheckAndReturn(hr);

    // set PKEY_Title property to the specified display
    PROPVARIANT propvar;
    hr = InitPropVariantFromString(pszDisplay, &propvar);
    CheckAndReturn(hr);

    hr = pPropertyStore->SetValue(PKEY_Title, propvar);
    PropVariantClear(&propvar);
    CheckAndReturn(hr);

    hr = pPropertyStore->Commit();
    CheckAndReturn(hr);

    // IShellLink implements an interface IShellLinkDataList
    // if we need to set console properties or if the shortcut
    // requires elevation, then we need to query for
    // IShellLinkDataList and work with the same
    CComPtr<IShellLinkDataList> pShellLinkDataList;

    pShellLink->QueryInterface(IID_PPV_ARGS(&pShellLinkDataList));
    CheckAndReturn(hr);

    DWORD dwFlags = 0;
    hr = pShellLinkDataList->GetFlags(&dwFlags);
    CheckAndReturn(hr);

    dwFlags |= SLDF_ALLOW_LINK_TO_LINK;
    hr = pShellLinkDataList->SetFlags(dwFlags);
    CheckAndReturn(hr);

    // if elevation is required, set the flags accordingly
    if (requiresElevation)
    {
        // It has methods GetFlags() and SetFlags() to obtain
        // and modify the flags on a shell link. The
        // SLDF_RUNAS_USER should be set to allow running as an
        // administrator

        dwFlags |= SLDF_RUNAS_USER;
        hr = pShellLinkDataList->SetFlags(dwFlags);
        CheckAndReturn(hr);
    }

    hr = pShellLink->QueryInterface(IID_PPV_ARGS(ppShortCut));

    return hr;
}

#endif

/********************************************************************
Checks to see if ISE is present
*********************************************************************/
INT CheckForISE()
{
    WIN32_FIND_DATA FindFileData;
    HANDLE handle = NULL;
    DWORD length = MAX_PATH;
    int isePresent = 0;

    LPWSTR pszExpandedPath = static_cast<LPWSTR>(::malloc((length + 1) * sizeof(WCHAR)));
    if (pszExpandedPath != NULL)
    {
        DWORD retlen = ExpandEnvironmentStrings(g_ISE_BINARY_PATH, pszExpandedPath, length);

        if (retlen > 0 && retlen <= length)
        {
            handle = ::FindFirstFile(pszExpandedPath, &FindFileData);

            if (handle != INVALID_HANDLE_VALUE)
            {
                isePresent = 1;
                ::FindClose(handle);
            }
        }
        free(pszExpandedPath);
    }

    return isePresent;
}

/**********************************************************************
 Function to check if the current process is a WOW64 process
**********************************************************************/
BOOL IsWow64()
{
    HANDLE hCurrentProc = GetCurrentProcess();

    BOOL isWow64Process = FALSE;
    BOOL ret = IsWow64Process(hCurrentProc, &isWow64Process);

    if (ret)
    {
        return isWow64Process;
    }

    return FALSE;
}

/**********************************************************************
 Function to check if the current process is a WinPE process
**********************************************************************/
bool IsWinPEHost()
{
    HKEY hkResult;
    LONG lRes = RegOpenKeyEx(HKEY_LOCAL_MACHINE, L"SYSTEM\\CurrentControlSet\\Control\\MiniNT", 0, KEY_READ, &hkResult) ;

    if(lRes == ERROR_SUCCESS)
    {
        RegCloseKey(hkResult);
    }

    // The Registry Path does not exit on any other SKU except WinPE
    return lRes == ERROR_SUCCESS;
}

/**********************************************************************
 Function to check if the current OS is Win7 or later
**********************************************************************/
bool IsOSLWin7OrLater()
{
    return IsWindows7OrGreater();
}

// Validate that the CLR was present when PowerShell was installed
// (if on a downlevel machine)
unsigned int ValidateDownlevelSetupHadClrWhenInstalled(
                       __in PWSTR wszMonadVersion,
                       int monadMajorVersion,
                       __in PWSTR wszRuntimeVersion,
                       int useMonadMajorVersion)

{
    unsigned int exitCode = ERROR_SUCCESS;

    HKEY hEngineKey = NULL;
    wchar_t * wszMshEngineRegKeyPath = NULL;
    wchar_t * NetFxV4IsInstalledKeyValue = NULL;
    DWORD valueLengthInByte = 0;

    static LPCWSTR g_NetFX_V4_IS_INSTALLED_KEY = L"NetFrameworkV4IsInstalled";

    do
    {
        exitCode = pwrshCommon.OpenEngineRegKey(&hEngineKey, &wszMshEngineRegKeyPath, &wszMonadVersion, &useMonadMajorVersion);
        if (EXIT_CODE_SUCCESS != exitCode)
        {
            break;
        }

        // If NetFramework4IsInstalled key exists and value equals No, output error message.
        // Else, Launch PowerShell
        LONG lResult = RegQueryValueExW(hEngineKey, g_NetFX_V4_IS_INSTALLED_KEY, NULL, NULL, NULL, &valueLengthInByte);
        if (lResult == ERROR_SUCCESS)
        {
            // If the data has the REG_SZ, REG_MULTI_SZ or REG_EXPAND_SZ type, this size includes any terminating null character or characters
            // unless the data was stored without them - http://msdn.microsoft.com/en-us/library/ms724911(VS.85).aspx
            NetFxV4IsInstalledKeyValue = new wchar_t[valueLengthInByte / sizeof(wchar_t)];
            if (NULL == NetFxV4IsInstalledKeyValue)
            {
                exitCode = EXIT_CODE_INIT_FAILURE;
            }
            else
            {
                memset(NetFxV4IsInstalledKeyValue, 0, valueLengthInByte);

                lResult = RegQueryValueExW(
                    hEngineKey,
                    g_NetFX_V4_IS_INSTALLED_KEY,
                    NULL,
                    NULL,
                    (LPBYTE) NetFxV4IsInstalledKeyValue,
                    &valueLengthInByte);

                if (lResult == ERROR_SUCCESS)
                {
                    if (0 == wcsncmp(NetFxV4IsInstalledKeyValue, L"No", 2))
                    {
                        exitCode = EXIT_CODE_INIT_FAILURE;
                        pwrshExeOutput->DisplayMessage(false, g_CLR_VERSION_NOT_INSTALLED, wszRuntimeVersion, wszMonadVersion);
                    }
                }
            }
        }
    }
    while(false);

    if (NULL != hEngineKey)
    {
        RegCloseKey(hEngineKey);
        hEngineKey = NULL;
    }

    if (NULL != wszMshEngineRegKeyPath)
    {
        delete[] wszMshEngineRegKeyPath;
        wszMshEngineRegKeyPath = NULL;
    }

    if (NULL != NetFxV4IsInstalledKeyValue)
    {
        delete[] NetFxV4IsInstalledKeyValue;
        NetFxV4IsInstalledKeyValue = NULL;
    }

    return exitCode;
}

#if (WINVER >= _WIN32_WINNT_WIN7)

#pragma prefast(push)
#pragma prefast (disable: 6031) // Return value ignored - StringCchLengthW return value is ignored

/**********************************************************************
 Adds the required PowerShell Tasks to the custom destination list
***********************************************************************/
HRESULT AddPowerShellTasksToList(ICustomDestinationList *pCustDestList, STARTUPINFO &startupInfo)
{
    LPCWSTR lpParentShortCut = startupInfo.lpTitle;
    LPWSTR pathRegValue = NULL;
    BOOL foundlnkFile = false;

    if (NULL == lpParentShortCut || ( (startupInfo.dwFlags & STARTF_TITLEISLINKNAME) != STARTF_TITLEISLINKNAME ) )
    {
        LPWSTR pathRegValueName =NULL;
        DWORD valueLengthInByte = 0;
        LONG retVal = 0;

        if (IsWow64())
        {
            pathRegValueName = L"ConsoleHostShortcutTargetX86";
        }
        else
        {
            pathRegValueName = L"ConsoleHostShortcutTarget";
        }

        retVal = RegGetValue(HKEY_LOCAL_MACHINE, g_ConsoleHostShortcutTarget_KEY_PATH, pathRegValueName, RRF_RT_REG_SZ | RRF_RT_REG_EXPAND_SZ | RRF_NOEXPAND, NULL, NULL, &valueLengthInByte);
        if (ERROR_SUCCESS == retVal)
        {
            pathRegValue = new wchar_t[valueLengthInByte / sizeof(wchar_t) +1];
            memset(pathRegValue, 0, valueLengthInByte + sizeof(wchar_t));

            retVal = RegGetValue(HKEY_LOCAL_MACHINE, g_ConsoleHostShortcutTarget_KEY_PATH, pathRegValueName, RRF_RT_REG_SZ | RRF_RT_REG_EXPAND_SZ | RRF_NOEXPAND, NULL, pathRegValue, &valueLengthInByte);
            if (retVal == ERROR_SUCCESS)
            {
                lpParentShortCut = pathRegValue;
                foundlnkFile = true;
            }
            else
            {
                if (IsWow64())
                {
					// Check for Threshold: Windows PowerShell (x86).lnk
					if (FileExists(L"%AppData%\\Microsoft\\Windows\\Start Menu\\Programs\\Windows PowerShell\\Windows PowerShell(x86).lnk"))
					{
						lpParentShortCut = L"%AppData%\\Microsoft\\Windows\\Start Menu\\Programs\\Windows PowerShell\\Windows PowerShell(x86).lnk";
						foundlnkFile = true;
					}

                    // Check for Win8 & BLUE: Windows PowerShell (x86).lnk
					if (!foundlnkFile && FileExists(L"%ProgramData%\\Microsoft\\Windows\\Start Menu\\Programs\\Administrative Tools\\Windows PowerShell (x86).lnk"))
                    {
                        lpParentShortCut = L"%ProgramData%\\Microsoft\\Windows\\Start Menu\\Programs\\Administrative Tools\\Windows PowerShell (x86).lnk";
                        foundlnkFile = true;
                    }

                    // Check for Win7: Windows PowerShell (x86).lnk
                    if(!foundlnkFile && FileExists(L"%ProgramData%\\Microsoft\\Windows\\Start Menu\\Programs\\Accessories\\Windows PowerShell\\Windows PowerShell (x86).lnk"))
                    {
                        lpParentShortCut = L"%ProgramData%\\Microsoft\\Windows\\Start Menu\\Programs\\Accessories\\Windows PowerShell\\Windows PowerShell (x86).lnk";
                        foundlnkFile = true;
                    }
                }
                else
                {
					// Check for Threshold: Windows PowerShell.lnk
					if (FileExists(L"%AppData%\\Microsoft\\Windows\\Start Menu\\Programs\\Windows PowerShell\\Windows PowerShell.lnk"))
					{
						lpParentShortCut = L"%AppData%\\Microsoft\\Windows\\Start Menu\\Programs\\Windows PowerShell\\Windows PowerShell.lnk";
						foundlnkFile = true;
					}

                    // Check for Win8 & BLUE: Windows PowerShell.lnk
					if (!foundlnkFile && FileExists(L"%ProgramData%\\Microsoft\\Windows\\Start Menu\\Programs\\System Tools\\Windows PowerShell.lnk"))
                    {
                        lpParentShortCut = L"%ProgramData%\\Microsoft\\Windows\\Start Menu\\Programs\\System Tools\\Windows PowerShell.lnk";
                        foundlnkFile = true;
                    }

                    // Check for Win7: Windows PowerShell.lnk
                    if(!foundlnkFile && FileExists(L"%ProgramData%\\Microsoft\\Windows\\Start Menu\\Programs\\Accessories\\Windows PowerShell\\Windows PowerShell.lnk"))
                    {
                        lpParentShortCut = L"%ProgramData%\\Microsoft\\Windows\\Start Menu\\Programs\\Accessories\\Windows PowerShell\\Windows PowerShell.lnk";
                        foundlnkFile = true;
                    }
                }
            }

            // Update the Console title.
            LPWSTR pszTitle = NULL;
            HRESULT hresult = SafeLoadString(109, &pszTitle);
            CheckAndReturn(hresult);
            SetConsoleTitle(pszTitle);
            free(pszTitle);
        }

        size_t length;
        StringCchLength(lpParentShortCut, STRSAFE_MAX_LENGTH, &length);

        StringCchCopy(g_IconApp, length+1, lpParentShortCut);
    }

    if(!foundlnkFile)
    {
        size_t length2;
        StringCchLength(lpParentShortCut, STRSAFE_MAX_LENGTH, &length2);
        StringCchCopy(g_IconApp, length2+1, lpParentShortCut);
    }

    CComPtr<IPersistFile> pPersistFile;
    CComPtr<IShellLink> pShellLink;

    HRESULT hr = pShellLink.CoCreateInstance(CLSID_ShellLink, NULL, CLSCTX_INPROC_SERVER);
    CheckAndReturn(hr);

    hr = pShellLink->QueryInterface(IID_IPersistFile, (LPVOID*)&pPersistFile);
    CheckAndReturn(hr);

    hr = pPersistFile->Load(g_IconApp, STGM_READWRITE);
    int iconIndex = 0;
    if (SUCCEEDED(hr))
    {
        hr = pShellLink->GetIconLocation(g_IconApp, MAX_PATH, &iconIndex);
        CheckAndReturn(hr);
    }
    else
    {
        LPCWSTR powerShellExec = L"%windir%\\System32\\WindowsPowerShell\\v1.0\\powershell.exe";

        size_t length;

        StringCchLength(powerShellExec, STRSAFE_MAX_LENGTH, &length);

        StringCchCopy(g_IconApp, length+1, powerShellExec);
    }

    CComPtr<IObjectCollection> pShortCutCollection;

    hr = pShortCutCollection.CoCreateInstance(CLSID_EnumerableObjectCollection, NULL, CLSCTX_INPROC);
    CheckAndReturn(hr);

    CComPtr<IShellLink> pShortCut;

    LPWSTR pszDisplay = NULL;           // display string
    LPWSTR pszDescription = NULL;       // description string


    // 1. Add "Run Windows PowerShell as admin"
    hr = SafeLoadString(118, &pszDisplay);
    CheckAndReturn(hr);
    SafeLoadString(119, &pszDescription);
    CheckAndReturn(hr);

    // create shortcut
    hr = CreateShortCut(pszDisplay, lpParentShortCut, pszDescription,
            NULL, g_IconApp, iconIndex, true, true, &pShortCut);
    delete[] pathRegValue;
    free(pszDisplay);
    free(pszDescription);
    CheckAndReturn(hr);

    // add short cut to collection
    hr = pShortCutCollection->AddObject(pShortCut);
    CheckAndReturn(hr);

    // 2. Add Windows PowerShell ISE
    // check if ISE is present
    if (CheckForISE())
    {
        // Add shortcut for "Run ISE as Administrator"
        hr = SafeLoadString(122, &pszDisplay);
        CheckAndReturn(hr);
        SafeLoadString(123, &pszDescription);
        CheckAndReturn(hr);

        // create new shortcut, free the previous one (already transferred to the collection)
        pShortCut = NULL;
        hr = CreateShortCut(pszDisplay, g_ISE_BINARY_PATH, pszDescription, NULL,g_ISE_BINARY_PATH, 0, true, false, &pShortCut);
        free(pszDisplay);
        free(pszDescription);
        CheckAndReturn(hr);

        // add short cut to collection
        hr = pShortCutCollection->AddObject(pShortCut);
        CheckAndReturn(hr);

        hr = SafeLoadString(101, &pszDisplay);
        CheckAndReturn(hr);
        SafeLoadString(111, &pszDescription);
        CheckAndReturn(hr);

        // create new shortcut, free the previous one (already transferred to the collection)
        pShortCut = NULL;
        hr = CreateShortCut(pszDisplay, g_ISE_BINARY_PATH, pszDescription, NULL,g_ISE_BINARY_PATH, 0, false, false, &pShortCut);
        free(pszDisplay);
        free(pszDescription);
        CheckAndReturn(hr);

        // add short cut to collection
        hr = pShortCutCollection->AddObject(pShortCut);
        CheckAndReturn(hr);
    }

    // obtain IObjectArry from the short cut collection
    // since AddUserTasks requires one
    CComPtr<IObjectArray> pObjectArray;
    hr = pShortCutCollection->QueryInterface(IID_PPV_ARGS(&pObjectArray));
    CheckAndReturn(hr);

    hr = pCustDestList->AddUserTasks(pObjectArray);

    return hr;
}

/*********************************************************************
* FileExists is a helper function used to check if the file path
* provided as argument to this function exists or not.
*********************************************************************/
BOOL FileExists(LPCWSTR  fileName)
{
    HANDLE hFile;
    LPSECURITY_ATTRIBUTES pSec = (LPSECURITY_ATTRIBUTES)NULL;
    ::SetLastError(ERROR_SUCCESS);
    hFile = ::CreateFile(fileName,
                         GENERIC_READ,
                         FILE_SHARE_READ, // | FILE_SHARE_WRITE,
                         pSec,
                         OPEN_EXISTING,
                         FILE_ATTRIBUTE_NORMAL | FILE_FLAG_RANDOM_ACCESS,
                         0);
    if(hFile != INVALID_HANDLE_VALUE)
    {
        ::CloseHandle(hFile);
        return TRUE;
    }
    else {
        return FALSE;
    }
}

/*********************************************************************
* Create a Custom Destination List for PowerShell on first run
*********************************************************************/
void CreateCustomDestinationList(STARTUPINFO &startupInfo)
{
    CoInitialize(NULL);

    CComPtr<ICustomDestinationList> pCustDestList;  // pointer to the jump list
    UINT uMaxSlots = 0;                         // maximum slots available for us
                                                // to create items in jump list

    HRESULT hr = pCustDestList.CoCreateInstance(CLSID_DestinationList, NULL, CLSCTX_INPROC_SERVER);

    if (SUCCEEDED(hr))
    {
        CComPtr<IObjectArray> pRemovedItems;        // Items just removed when this transaction begins

        // start a list building transaction
        // this method needs to be called before
        // any of the other methods in ICustomDestinationList
        hr = pCustDestList->BeginList(&uMaxSlots, IID_PPV_ARGS(&pRemovedItems));

        if (SUCCEEDED(hr))
        {
            // we need to verify if there is enough
            // space for us to add the required items
            if (uMaxSlots < PS_TASK_NUM)
            {
                return;
            }

            hr = AddPowerShellTasksToList(pCustDestList, startupInfo);

            if (SUCCEEDED(hr))
            {
                // if adding tasks succeeded commit the list
                hr = pCustDestList->CommitList();
            }
        }
    }

    CoUninitialize();
}

#pragma prefast(push)

#endif

int __cdecl
    wmain(
          int argc,
          __in_ecount(argc) LPWSTR * argv)
{
    #ifdef ALLOWDEBUG
     // This loop is added to assist debugging server.
    // Attach a debugger to the server and set this variable to true
    // from the debugger.
    bool isDebuggerAttached = false;
    do
    {
       Sleep(1);
    }while(!isDebuggerAttached);
    #endif

    wchar_t * wszMonadVersion = NULL;
    wchar_t * wszConsoleFile = NULL;
    SAFEARRAY * pArgvSA = NULL;
    wchar_t * wszDefaultRuntimeVersion = NULL;
    wchar_t * wszRuntimeVersion = NULL;
    wchar_t * wszConsoleHostAssemblyName = NULL;

    bool isDeletewszMonadVersionNeeded = false;

    unsigned int exitCode = EXIT_CODE_SUCCESS;

    // Windows OS Bug: 1979038
    // Change the thread user interface to a language that the
    // windows console can display. This is important as starting
    // from windows vista and later, the OS support thread user
    // interface language separate from thread locale.
    // Setting this will enable OS resource loader to load correct
    // resource that can display properly in a console window.
    // Note: If the language identifier is 0, the function always
    // succeeds.
    SetThreadUILanguage(0);

    if (IsOSLWin7OrLater())
    {
      // populate jumplist (which is supported in w7 or later)
      #if (WINVER >= _WIN32_WINNT_WIN7)
        STARTUPINFO startupInfo;
        GetStartupInfo(&startupInfo);

        // Only populate the jumplist if we are creating a new window, otherwise it's unnecessary
        // because there is no taskbar item to update, or it has been done already.
        if ((startupInfo.dwFlags & STARTF_USESHOWWINDOW) && (startupInfo.wShowWindow != SW_HIDE))
        {
            CreateCustomDestinationList(startupInfo);
        }
      #endif
    }

    do
    {
        // The default powershell version is 3 so that the exe will
        // load the registry key HKLM\Software\Microsoft\PowerShell\3\PowerShellEngine
        int monadMajorVersion = -1;
        int monadMinorVersion = -1;
        int monadVersionIndex = -1;
        int runtimeVersionIndex = -1;
        int consoleFileIndex = -1;
        int profileIndex = -1;
        const float win8DefaultMonadMajorVersion = 3.0;

        exitCode = ParseCommandLineArguments(
                                argc,
                                argv,
                                &wszMonadVersion,
                                &monadMajorVersion,
                                &monadMinorVersion,
                                &monadVersionIndex,
                                &wszRuntimeVersion,
                                &runtimeVersionIndex,
                                &wszConsoleFile,
                                &consoleFileIndex,
                                &profileIndex);
        if (EXIT_CODE_SUCCESS != exitCode)
        {
            break;
        }

        // WinPE supports only PowerShell >=3.0.
        // If the PowerShell host is a WinPE machine & if the requested
        // PowerShell version is not 3.0, then display an not supported monad version error message.
        if(IsWinPEHost() && (monadMajorVersion == 1 || monadMajorVersion == 2))
        {
            exitCode = g_NOTSUPPORTED_MONAD_VERSION;
            pwrshExeOutput->DisplayMessage(false, g_NOTSUPPORTED_MONAD_VERSION, monadMajorVersion, win8DefaultMonadMajorVersion);
            break;
        }

        // This is for remembering which major version is requested from command line.
        // GetRegistryInfo call after this will change monadMajorVersion to be PowerShell
        // major version installed (based on registry).
        // int requestedMonadMajorVersion = monadMajorVersion;

        // For GetRegistryInfo call, monadMajorVersion is used to calculate the version key in registry.
        // For PowerShell V2, version key in registry is 1.
        if (monadMajorVersion == 2)
        {
            monadMajorVersion = 1;
        }
        else if ((monadMajorVersion == 4) || (monadMajorVersion == 5))
        {
            // For PowerShell 4.0 and 5.0 the registry is the same as 3.0.
            monadMajorVersion = 3;
        }
        // if monadVersionIndex is set by ParseCommandLineArguments,
        // wszMonadVersion is pointing to argv[monadVersionIndex] and
        // no need to delete it.
        isDeletewszMonadVersionNeeded = monadVersionIndex == -1;
        exitCode = pwrshCommon.GetRegistryInfo(
                &wszMonadVersion,
                &monadMajorVersion,
                monadMinorVersion,
                &wszDefaultRuntimeVersion,
                &wszConsoleHostAssemblyName);
        if (EXIT_CODE_SUCCESS != exitCode)
        {
            break;
        }

        bool skipProfile = profileIndex != -1;

        // skipIndex holds the position of the last parameter that needs to be handled by the native layer.
        // In other words, skipIndex + 1 will give the position of the first parameter that needs to be passed to managed layer.
        // Find the last native parameter, and chomp everything up until it.
        // We don't do this for NoProfile. We parse it once here and then parse it again in the managed layer.
        int skipIndex = -1;

        if(monadVersionIndex > skipIndex) { skipIndex = monadVersionIndex; }
        if(consoleFileIndex > skipIndex) { skipIndex = consoleFileIndex; }
        if(runtimeVersionIndex > skipIndex) { skipIndex = runtimeVersionIndex; }

        skipIndex = skipIndex + 1;

        // WTR  - Check for the key that indicates absence of NetFx4 and output appropriate message to the user

        // Open PowerShellEngine Registry Key
        // For GetRegistryInfo call, monadMajorVersion is used to calculate the version key in registry.
        // For PowerShell V2, version key in registry is 1.
        int useMonadMajorVersion = -1;
        if (monadMajorVersion == 2)
        {
            useMonadMajorVersion = 1;
        }
        else
        {
            useMonadMajorVersion = 3;
        }

        // If runtimeVersion is not supplied, validate that the defaultRuntimeVersion is installed.
        // We can't do runtimeVersion validation on the user input, as this frequently changes per
        // release of the .NET Framework.
        if(NULL == wszRuntimeVersion)
        {
            wszRuntimeVersion = wszDefaultRuntimeVersion;
            wszDefaultRuntimeVersion = NULL;
        }
        else
        {
            // Display the warning that they're using a version of the runtime that PowerShell is
            // not tested with.
            pwrshExeOutput->DisplayMessage(false, g_NONSTANDARD_CLR_VERSION, wszRuntimeVersion);
        }

        // On downlevel setups, verify that the CLR was present when PowerShell was installed.
        // If it wasn't, then PowerShell is in a bad state and needs to be installed after the
        // CLR.
        exitCode = ValidateDownlevelSetupHadClrWhenInstalled(
            wszMonadVersion,
            monadMajorVersion,
            wszRuntimeVersion,
            useMonadMajorVersion);
        if (EXIT_CODE_SUCCESS != exitCode)
        {
            break;
        }

        exitCode = LaunchManagedMonad(
            wszMonadVersion,
            monadMajorVersion,
            wszConsoleFile,
            wszRuntimeVersion,
            wszConsoleHostAssemblyName,
            pArgvSA,
            skipIndex,
            argc,
            argv);
        if (EXIT_CODE_SUCCESS != exitCode)
        {
            break;
        }
    }
    while (false);

    if (NULL != pArgvSA)
    {
        SafeArrayDestroy(pArgvSA);
        pArgvSA = NULL;
    }

    if (NULL != wszDefaultRuntimeVersion)
    {
        delete [] wszDefaultRuntimeVersion;
        wszDefaultRuntimeVersion = NULL;
    }

    if (NULL != wszConsoleHostAssemblyName)
    {
        delete [] wszConsoleHostAssemblyName;
        wszConsoleHostAssemblyName = NULL;
    }

    if (isDeletewszMonadVersionNeeded)
    {
        delete [] wszMonadVersion;
        wszMonadVersion = NULL;
    }

    if (g_hResInstance)
    {
        FreeMUILibrary(g_hResInstance);
    }

    return exitCode;
}
