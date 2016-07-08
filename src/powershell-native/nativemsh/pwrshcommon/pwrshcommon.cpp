/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

//
// Implementation of common code used by native PowerShell
//

#include "NativeMsh.h"
#include <stdio.h>
#include <atlbase.h>
#include <corerror.h>
#include <sstream>
#include "WinSystemCallFacade.h"

namespace NativeMsh 
{
    //
    // Defining these as "static" ensures internal linkage of the values.
    // For some reason, LPCWSTR is removing that linkage during macro expansion.
    //
    static LPCWSTR g_MSH_REG_KEY_PATH = L"SOFTWARE\\Microsoft\\PowerShell";
    static LPCWSTR g_MSHVERSION_REG_KEY_PATH_TEMPLATE = L"SOFTWARE\\Microsoft\\PowerShell\\%1!ls!";
    static LPCWSTR g_MSHENGINE_REG_KEY_PATH_TEMPLATE = L"SOFTWARE\\Microsoft\\PowerShell\\%1!ls!\\PowerShellEngine";

    //
    // Definitions of the protected PwrshCommon methods
    //

    bool PwrshCommon::ParseInt(
        const WCHAR * pwchStart,
        const WCHAR * pwchEnd,
        int * pInt)
    {
        bool returnResult = true;
        do
        {
            if (!(*pwchEnd < L'0' || *pwchEnd > L'9'))
            {
                returnResult = false;
                break;
            }
            if (this->StringIsNullOrEmpty(pwchStart) || pwchStart >= pwchEnd)
            {
                returnResult = false;
                break;
            }
            //skip leading 0s
            while (pwchStart < pwchEnd && (*pwchStart == L'0'))
            {
                pwchStart++;
            }
            // MAX_INT has 10 digits only. this ensures the below call to wcstol won't overflow
            if (pwchEnd - pwchStart > g_MAX_VERSION_FIELD_LENGTH)
            {
                returnResult = false;
                break;
            }
            WCHAR * pwchIntEnd = NULL;
            // this should never cause overflow because VerifyInteger gaurantees pwchMinorVersion
            // has less than g_MAX_NUMBER_OF_DIGITS_IN_VERSION which is 10.
            unsigned long ulTempResult = wcstoul(pwchStart, &pwchIntEnd, 10);
            // Make sure the whole string is an integer and fits an int 
            if (pwchEnd != pwchIntEnd || ulTempResult > (unsigned long)INT_MAX)
            {
                returnResult = false;
                break;
            }
            *pInt = (int)ulTempResult;
        } while (false);
        return returnResult;
    }

    _Success_(return) bool PwrshCommon::ExtractFirstVersionComponent(
        LPCWSTR wszVersionString,
        int* lpFirstVersionComponent,
        __deref_out_opt WCHAR** wszRemainingVersionString)
    {
        bool returnResult = true;
        do
        {
            if (this->StringIsNullOrEmpty(wszVersionString) ||
                (NULL == lpFirstVersionComponent) ||
                (NULL == wszRemainingVersionString))
            {
                returnResult = false;
                break;
            }
            const WCHAR * pwchDot = wcschr(wszVersionString, L'.');
            const WCHAR * pwchNull = wcschr(wszVersionString, L'\0');
            assert(NULL != pwchNull);
            if (NULL == pwchNull)
            {
                returnResult = false;
                break;
            }
            if (NULL != pwchDot)
            {
                returnResult = this->ParseInt(
                    wszVersionString,
                    pwchDot,
                    lpFirstVersionComponent);
                if (!returnResult)
                {
                    break;
                }
                *wszRemainingVersionString = _wcsinc(pwchDot);
            }
            else // pwchDot == NULL
            {
                returnResult = this->ParseInt(
                    wszVersionString,
                    pwchNull,
                    lpFirstVersionComponent);
                if (!returnResult)
                {
                    break;
                }
                *wszRemainingVersionString = NULL;
            }
        } while (false);

        return returnResult;
    }

#pragma prefast(push)
#pragma prefast (disable: 6101)
#pragma prefast (disable: 6054)
#pragma prefast (disable: 6001)

    bool PwrshCommon::RegOpenKeyWithErrorReport(
        LPCWSTR wszRegPath,
        LPCWSTR wszMonadVersion,
        __out_ecount(1) PHKEY phResult)
    {
        bool returnResult = true;
        LONG lResult = RegOpenKeyExW(HKEY_LOCAL_MACHINE, wszRegPath, 0, KEY_READ, phResult);
        if (ERROR_SUCCESS != lResult)
        {
            // special case: if the reg key doesn't exist, don't print the win32 system error
            // since it's not descriptive
            if (ERROR_FILE_NOT_FOUND == lResult)
            {
                if (NULL == wszMonadVersion)
                {
                    this->output->DisplayMessage(false,
                        g_MISSING_REG_KEY,
                        wszRegPath);
                }
                else
                {
                    this->output->DisplayMessage(false,
                        g_MISSING_REG_KEY1,
                        wszRegPath,
                        wszMonadVersion);
                }
            }
            else
            {
                this->output->DisplayErrorWithSystemError(
                    lResult,
                    g_OPEN_REG_KEY_FAILED_WITH,
                    wszRegPath);
            }
            returnResult = false;
        }
        return returnResult;
    }

    bool PwrshCommon::FormatStringWithErrorReporting(
        LPCWSTR wszFormat,
        __deref_out_opt PWSTR * pwszResult,
        __out_ecount(1) LPDWORD lpdwLength,
        int errorMessageId,
        ...)
    {
        LPWSTR wszTemp = NULL;
        DWORD dwTempLength = 0;
        bool returnResult = true;
        va_list args;
        va_start(args, errorMessageId);
        do
        {
            if (NULL == wszFormat || NULL == pwszResult || NULL == lpdwLength)
            {
                returnResult = false;
                break;
            }
            dwTempLength = FormatMessageW(
                FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_STRING,
                wszFormat,
                0,
                0,
                (LPWSTR)&wszTemp,
                0,
                &args);
            if (0 == dwTempLength)
            {
                LONG lastError = GetLastError();
                LPWSTR wszSystemErrorMessage = NULL;
                DWORD dwErrorLength =
                    this->GetSystemErrorMessage(
                        lastError,
                        &wszSystemErrorMessage);
                if (dwErrorLength > 0)
                {
                    this->output->DisplayMessage(false, errorMessageId, wszSystemErrorMessage);
                    if (NULL != wszSystemErrorMessage)
                    {
                        delete[] wszSystemErrorMessage;
                        wszSystemErrorMessage = NULL;
                    }
                }
                returnResult = false;
                break;
            }
            *pwszResult = new WCHAR[dwTempLength + 1];
            if (NULL == *pwszResult)
            {
                returnResult = false;
                break;
            }
            //string function
            if (SUCCEEDED(StringCchCopy(*pwszResult, dwTempLength + 1, wszTemp)))
            {
                *lpdwLength = dwTempLength;
            }
            else
            {
                if (NULL != *pwszResult)
                {
                    delete[] * pwszResult;
                    *pwszResult = NULL;
                }
                returnResult = false;
                break;
            }
        } while (false);
        if (0 != dwTempLength)
        {
            LocalFree(wszTemp);
        }
        va_end(args);
        return returnResult;
    }

    bool PwrshCommon::OpenLatestMSHEngineRegistry(
        __out_ecount(1) PHKEY phResult,
        __deref_out_opt PWSTR * pwszMshEngineRegKeyPath,
        __deref_out_opt PWSTR * pwszMonadVersion,
        __out_ecount(1) int * lpMonadMajorVersion)
    {
        bool returnResult = true;
        HKEY hMshRegKey = 0;
        bool bMshRegKeyOpened = true;
        LPWSTR lpSubKeyName = NULL;
        do
        {
            if (NULL == phResult || NULL == pwszMshEngineRegKeyPath ||
                NULL == pwszMonadVersion || NULL == lpMonadMajorVersion)
            {
                returnResult = false;
                break;
            }
            *lpMonadMajorVersion = -1;
            LPCWSTR mshRegPath = g_MSH_REG_KEY_PATH;
            if (!this->RegOpenKeyWithErrorReport(mshRegPath, *pwszMonadVersion, &hMshRegKey))
            {
                bMshRegKeyOpened = false;
                returnResult = false;
                break;
            }
            lpSubKeyName = new WCHAR[g_MAX_REG_KEY_LENGTH];
            if (NULL == lpSubKeyName)
            {
                returnResult = false;
                break;
            }

            DWORD dwIndex = 0;
            int latestVersionNumber = 0;
            LPWSTR wszLatestSubKeyName = NULL;
            DWORD cchLatestSubKeyName = 0;
            while (true)
            {
                DWORD dwSubKeyNameLength = g_MAX_REG_KEY_LENGTH;
                FILETIME ftLastWriteTime;
                LONG lRegEnumResult = RegEnumKeyEx(
                    hMshRegKey,
                    dwIndex++,
                    lpSubKeyName,
                    &dwSubKeyNameLength,
                    NULL,
                    NULL,
                    NULL,
                    &ftLastWriteTime);
                if (ERROR_NO_MORE_ITEMS == lRegEnumResult)
                {
                    break;
                }
                if (ERROR_SUCCESS != lRegEnumResult)
                {
                    this->output->DisplayErrorWithSystemError(
                        lRegEnumResult,
                        g_SEARCH_LATEST_REG_KEY_FAILED_WITH,
                        mshRegPath);
                    returnResult = false;
                    break;
                }
                int majorVersionNumber = 0, minorVersionNumberUnused;
                if (this->VerifyMonadVersionFormat(lpSubKeyName, &majorVersionNumber, &minorVersionNumberUnused, false, false))
                {
                    // This key's name is a valid MSH version
                    // now it must be a natural number without sign prefix
                    // string function
                    if (majorVersionNumber > latestVersionNumber)
                    {
                        latestVersionNumber = majorVersionNumber;
                        cchLatestSubKeyName = dwSubKeyNameLength + 1;
                        if (NULL != wszLatestSubKeyName)
                        {
                            delete[] wszLatestSubKeyName;
                            wszLatestSubKeyName = NULL;
                        }
                        wszLatestSubKeyName = new WCHAR[cchLatestSubKeyName];
                        if (NULL == wszLatestSubKeyName)
                        {
                            returnResult = false;
                            break;
                        }
                        // string function
                        if (FAILED(StringCchCopy(wszLatestSubKeyName, cchLatestSubKeyName, lpSubKeyName)))
                        {
                            returnResult = false;
                            break;
                        }
                    }
                }
            }
            if (!returnResult)
            {
                break;
            }
            if (NULL == wszLatestSubKeyName)
            {
                this->output->DisplayMessage(false, g_NO_COMPLETELY_INSTALLED_FOUND_VERSION);
                returnResult = false;
                break;
            }

            *pwszMonadVersion = wszLatestSubKeyName;
            *lpMonadMajorVersion = latestVersionNumber;
            DWORD dwUnused;
            if (!this->FormatStringWithErrorReporting(
                g_MSHENGINE_REG_KEY_PATH_TEMPLATE,
                pwszMshEngineRegKeyPath,
                &dwUnused,
                g_CREATE_MSHENGINE_REG_KEY_PATH_FAILED_WITH,
                wszLatestSubKeyName))
            {
                returnResult = false;
                break;
            }

            returnResult = this->RegOpenKeyWithErrorReport(*pwszMshEngineRegKeyPath, *pwszMonadVersion, phResult);
        } while (false);

        if (bMshRegKeyOpened && (NULL != hMshRegKey))
        {
            RegCloseKey(hMshRegKey);
        }
        if (NULL != lpSubKeyName)
        {
            delete[] lpSubKeyName;
            lpSubKeyName = NULL;
        }
        return returnResult;
    }

    bool PwrshCommon::RegQueryREG_SZValue(
        _In_ HKEY hEngineKey,
        _In_ LPCWSTR wszValueName,
        _In_ LPCWSTR wszMshEngineRegKey,
        __deref_out_opt PWSTR * pwszRegData)
    {
        DWORD regValueType = 0;
        DWORD valueLengthInByte = 0;
        LONG result = 0;
        wchar_t * wszValue = NULL;
        bool returnResult = true;

        do
        {
            if (0 == hEngineKey || this->StringIsNullOrEmpty(wszMshEngineRegKey) || NULL == pwszRegData)
            {
                returnResult = false;
                break;
            }
            // this call checks how many bytes the value occupies
            result = RegQueryValueExW(
                hEngineKey,
                wszValueName,
                NULL,
                &regValueType,
                NULL,
                &valueLengthInByte);

            if (result != ERROR_SUCCESS)
            {
                wchar_t * wszErrorMessage = NULL;
                DWORD errorLength =
                    this->GetSystemErrorMessage(
                        result,
                        &wszErrorMessage);
                if (0 < errorLength)
                {
                    this->output->DisplayMessage(false, g_READ_REG_VALUE_FAILED_WITH, wszMshEngineRegKey, wszValueName, wszErrorMessage);
                    if (NULL != wszErrorMessage)
                    {
                        delete[] wszErrorMessage;
                        wszErrorMessage = NULL;
                    }
                }
                returnResult = false;
                break;
            }
            if (REG_SZ != regValueType)
            {
                this->output->DisplayMessage(false, g_EXPECT_REG_SZ_VALUE, wszMshEngineRegKey, wszValueName);
                returnResult = false;
                break;
            };
            if (0 == valueLengthInByte)
            {
                this->output->DisplayMessage(false, g_EMPTY_REG_SZ_VALUE, wszMshEngineRegKey, wszValueName);
                returnResult = false;
                break;
            }
            DWORD valueLength = valueLengthInByte / sizeof(wchar_t);

            wszValue = new wchar_t[valueLength + 1]; // plus 1 as RegQueryValueExW may not return null terminated string
            if (NULL == wszValue)
            {
                returnResult = false;
                break;
            }
            wszValue[valueLength] = L'\0'; // make sure wszValue is null terminated as RegQueryValueExW's returned value
            // may not be null terminated

            result = RegQueryValueExW(
                hEngineKey,
                wszValueName,
                NULL,
                NULL,
                (LPBYTE)wszValue, //simply casting to LPBYTE will make the returned unicode array work!
                &valueLengthInByte);
            if (result != ERROR_SUCCESS)
            {
                wchar_t * wszErrorMessage = NULL;
                DWORD errorLength =
                    this->GetSystemErrorMessage(
                        result,
                        &wszErrorMessage);
                if (0 < errorLength)
                {
                    this->output->DisplayMessage(false, g_READ_REG_VALUE_FAILED_WITH, wszMshEngineRegKey, wszValueName, wszErrorMessage);
                    if (NULL != wszErrorMessage)
                    {
                        delete[] wszErrorMessage;
                        wszErrorMessage = NULL;
                    }
                }
                delete[] wszValue;
                wszValue = NULL;
                returnResult = false;
                break;
            }
        } while (false);
        if (returnResult && this->StringIsNullOrEmpty(wszValue))
        {
            this->output->DisplayMessage(false, g_EMPTY_REG_SZ_VALUE, wszMshEngineRegKey, wszValueName);
            returnResult = false;
        }
        *pwszRegData = wszValue;
        return returnResult;
    }

    unsigned int PwrshCommon::IsEngineRegKeyWithVersionExisting(
        LPCWSTR wszMonadVersion, 
        LPCWSTR wszMonadMajorVersion)
    {
        unsigned int exitCode = EXIT_CODE_SUCCESS;
        LPWSTR wszVersionKey = NULL;
        DWORD dwLength = 0;
        HKEY hVersionKey = NULL;
        do
        {
            if (this->StringIsNullOrEmpty(wszMonadVersion) ||
                this->StringIsNullOrEmpty(wszMonadMajorVersion))
            {
                exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
                break;
            }
            if (!this->FormatStringWithErrorReporting(
                g_MSHVERSION_REG_KEY_PATH_TEMPLATE,
                &wszVersionKey,
                &dwLength,
                g_CREATE_MSHENGINE_REG_KEY_PATH_FAILED_WITH,
                wszMonadMajorVersion))
            {
                exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
                break;
            }
            LONG result = RegOpenKeyExW(HKEY_LOCAL_MACHINE, wszVersionKey, 0, KEY_READ, &hVersionKey);
            if (ERROR_SUCCESS != result)
            {
                exitCode = ERROR_FILE_NOT_FOUND == result ?
                EXIT_CODE_INCOMPATIBLE_MSH_VERSION :
                                                   EXIT_CODE_READ_REGISTRY_FAILURE;
                this->output->DisplayMessage(false, g_MSH_VERSION_NOT_INSTALLED, wszMonadVersion);
                break;
            }
        } while (false);
        if (NULL != wszVersionKey)
        {
            delete[] wszVersionKey;
            wszVersionKey = NULL;
        }
        if (NULL != hVersionKey)
        {
            RegCloseKey(hVersionKey);
            hVersionKey = NULL;
        }
        return exitCode;
    }

    unsigned int PwrshCommon::OpenEngineRegKeyWithVersion(
        __deref_out_ecount(1) PHKEY phEngineKey,
        __deref_out_opt PWSTR * pwszMshEngineRegKey,
        LPCWSTR wszMonadVersion,
        int monadMajorVersion)
    {
        // version is specified with -version or -mshconsole
        LPWSTR wszSubkey = NULL;
        DWORD dwLength = 0;
        unsigned int exitCode = EXIT_CODE_SUCCESS;
        do
        {
            if (NULL == phEngineKey ||
                NULL == pwszMshEngineRegKey ||
                this->StringIsNullOrEmpty(wszMonadVersion))
            {
                exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
                break;
            }

            // For PowerShell 3 and 4, the registry is 3. 
            if ((monadMajorVersion == 4) || (monadMajorVersion == 5))
            {
                monadMajorVersion = 3;
            }

            WCHAR wszMonadMajorVersion[g_MAX_VERSION_FIELD_LENGTH + 1];
            _itow_s(monadMajorVersion, wszMonadMajorVersion, g_MAX_VERSION_FIELD_LENGTH + 1, 10);
            exitCode = this->IsEngineRegKeyWithVersionExisting(wszMonadVersion, wszMonadMajorVersion);
            if (EXIT_CODE_SUCCESS != exitCode)
            {
                break;
            }
            // g_MAX_VERSION_FIELD_LENGTH + 1 for the null terminating char
            if (!this->FormatStringWithErrorReporting(
                g_MSHENGINE_REG_KEY_PATH_TEMPLATE,
                &wszSubkey,
                &dwLength,
                g_CREATE_MSHENGINE_REG_KEY_PATH_FAILED_WITH,
                wszMonadMajorVersion))
            {
                exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
                break;
            }
            if (!this->RegOpenKeyWithErrorReport(wszSubkey, wszMonadVersion, phEngineKey))
            {
                exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
                break;
            }
            *pwszMshEngineRegKey = wszSubkey;
        } while (false);
        return exitCode;
    }

    bool PwrshCommon::VerifyDOTNetVersionFormat(
        LPCWSTR wszFullVersion,
        __out_ecount(1) int * lpMajorVersion,
        __out_ecount(1) int * lpMinorVersion)
    {
        bool bReturnResult = true;
        do
        {
            if (this->StringIsNullOrEmpty(wszFullVersion) ||
                NULL == lpMajorVersion ||
                NULL == lpMinorVersion)
            {
                bReturnResult = false;
                break;
            }
            *lpMajorVersion = *lpMinorVersion = -1;
            const WCHAR * pwchDot = wcschr(wszFullVersion, L'.');
            if (NULL == pwchDot)
            {
                bReturnResult = false;
                break;
            }
            bReturnResult = this->ParseInt(
                wszFullVersion,
                pwchDot,
                lpMajorVersion);
            if (!bReturnResult)
            {
                break;
            }
            int cDotInt = 0; // counting how many .int's are after major (valid format: major(.int)+  (.int)+ up to 3)
            int versionFields[3];
            while (true)
            {
                if (2 < cDotInt)
                {
                    bReturnResult = false;
                    break;
                }
                const WCHAR * pwchField = _wcsinc(pwchDot);
                pwchDot = wcschr(pwchField, L'.');
                if (NULL == pwchDot)
                {
                    const WCHAR * pwchNull = wcschr(pwchField, L'\0');
                    if (NULL == pwchNull)
                    {
                        bReturnResult = false;
                        break;
                    }
                    bReturnResult = this->ParseInt(
                        pwchField,
                        pwchNull,
                        versionFields + cDotInt);
                    break;
                }
                bReturnResult = this->ParseInt(
                    pwchField,
                    pwchDot,
                    versionFields + cDotInt);
                if (!bReturnResult)
                {
                    break;
                }
                cDotInt++;
            }
            if (!bReturnResult)
            {
                break;
            }
            *lpMinorVersion = versionFields[0];
        } while (false);
        return bReturnResult;
    }

    // The assemblies that are trusted by CoreCLR. These are the CoreCLR implementation
    // and facade assemblies plus Microsoft.Management.Infrastructure (MI .Net) assemblies.
    // System.Management.Automation must not be listed here. I should exist on the APP_PATH.
    //
    // NOTE: The names must not include the .dll extension because it will be added programmatically.
    static PCWSTR trustedAssemblies[] =
    {
        L"Microsoft.CSharp",
        L"Microsoft.VisualBasic",
        L"Microsoft.Win32.Primitives",
        L"Microsoft.Win32.Registry.AccessControl",
        L"Microsoft.Win32.Registry",
        L"mscorlib",
        L"System.AppContext",
        L"System.Buffers",
        L"System.Collections.Concurrent",
        L"System.Collections",
        L"System.Collections.Immutable",
        L"System.Collections.NonGeneric",
        L"System.Collections.Specialized",
        L"System.ComponentModel.Annotations",
        L"System.ComponentModel.DataAnnotations",
        L"System.ComponentModel",
        L"System.ComponentModel.EventBasedAsync",
        L"System.ComponentModel.Primitives",
        L"System.ComponentModel.TypeConverter",
        L"System.Console",
        L"System.Core",
        L"System.Data.Common",
        L"System.Diagnostics.Contracts",
        L"System.Diagnostics.Debug",
        L"System.Diagnostics.DiagnosticSource",
        L"System.Diagnostics.FileVersionInfo",
        L"System.Diagnostics.Process",
        L"System.Diagnostics.StackTrace",
        L"System.Diagnostics.TextWriterTraceListener",
        L"System.Diagnostics.Tools",
        L"System.Diagnostics.TraceSource",
        L"System.Diagnostics.Tracing",
        L"System",
        L"System.Dynamic.Runtime",
        L"System.Globalization.Calendars",
        L"System.Globalization",
        L"System.Globalization.Extensions",
        L"System.IO.Compression",
        L"System.IO.Compression.ZipFile",
        L"System.IO",
        L"System.IO.FileSystem.AccessControl",
        L"System.IO.FileSystem",
        L"System.IO.FileSystem.DriveInfo",
        L"System.IO.FileSystem.Primitives",
        L"System.IO.FileSystem.Watcher",
        L"System.IO.MemoryMappedFiles",
        L"System.IO.Packaging",
        L"System.IO.Pipes",
        L"System.IO.UnmanagedMemoryStream",
        L"System.Linq",
        L"System.Linq.Expressions",
        L"System.Linq.Parallel",
        L"System.Linq.Queryable",
        L"System.Net",
        L"System.Net.Http",
        L"System.Net.Http.WinHttpHandler",
        L"System.Net.NameResolution",
        L"System.Net.NetworkInformation",
        L"System.Net.Ping",
        L"System.Net.Primitives",
        L"System.Net.Requests",
        L"System.Net.Security",
        L"System.Net.Sockets",
        L"System.Net.WebHeaderCollection",
        L"System.Net.WebSockets.Client",
        L"System.Net.WebSockets",
        L"System.Numerics",
        L"System.Numerics.Vectors",
        L"System.ObjectModel",
        L"System.Private.DataContractSerialization",
        L"System.Private.ServiceModel",
        L"System.Private.Uri",
        L"System.Reflection.DispatchProxy",
        L"System.Reflection",
        L"System.Reflection.Emit",
        L"System.Reflection.Emit.ILGeneration",
        L"System.Reflection.Emit.Lightweight",
        L"System.Reflection.Extensions",
        L"System.Reflection.Metadata",
        L"System.Reflection.Primitives",
        L"System.Reflection.TypeExtensions",
        L"System.Resources.ReaderWriter",
        L"System.Resources.ResourceManager",
        L"System.Runtime.CompilerServices.VisualC",
        L"System.Runtime",
        L"System.Runtime.Extensions",
        L"System.Runtime.Handles",
        L"System.Runtime.InteropServices",
        L"System.Runtime.InteropServices.PInvoke",
        L"System.Runtime.InteropServices.RuntimeInformation",
        L"System.Runtime.Loader",
        L"System.Runtime.Numerics",
        L"System.Runtime.Serialization",
        L"System.Runtime.Serialization.Json",
        L"System.Runtime.Serialization.Primitives",
        L"System.Runtime.Serialization.Xml",
        L"System.Security.AccessControl",
        L"System.Security.Claims",
        L"System.Security.Cryptography.Algorithms",
        L"System.Security.Cryptography.Cng",
        L"System.Security.Cryptography.Csp",
        L"System.Security.Cryptography.Encoding",
        L"System.Security.Cryptography.OpenSsl",
        L"System.Security.Cryptography.Primitives",
        L"System.Security.Cryptography.X509Certificates",
        L"System.Security.Principal",
        L"System.Security.Principal.Windows",
        L"System.Security.SecureString",
        L"System.ServiceModel",
        L"System.ServiceModel.Duplex",
        L"System.ServiceModel.Http",
        L"System.ServiceModel.NetTcp",
        L"System.ServiceModel.Primitives",
        L"System.ServiceModel.Security",
        L"System.ServiceModel.Web",
        L"System.ServiceProcess.ServiceController",
        L"System.Text.Encoding.CodePages",
        L"System.Text.Encoding",
        L"System.Text.Encoding.Extensions",
        L"System.Text.Encodings.Web",
        L"System.Text.RegularExpressions",
        L"System.Threading.AccessControl",
        L"System.Threading",
        L"System.Threading.Overlapped",
        L"System.Threading.Tasks.Dataflow",
        L"System.Threading.Tasks",
        L"System.Threading.Tasks.Extensions",
        L"System.Threading.Tasks.Parallel",
        L"System.Threading.Thread",
        L"System.Threading.ThreadPool",
        L"System.Threading.Timer",
        L"System.Windows",
        L"System.Xml",
        L"System.Xml.Linq",
        L"System.Xml.ReaderWriter",
        L"System.Xml.Serialization",
        L"System.Xml.XDocument",
        L"System.Xml.XmlDocument",
        L"System.Xml.XmlSerializer",
        L"System.Xml.XPath",
        L"System.Xml.XPath.XDocument",
        L"System.Xml.XPath.XmlDocument",
        L"Microsoft.PowerShell.CoreCLR.AssemblyLoadContext"
    };

    // Define the function pointer for the CLR entry point
    typedef HRESULT(STDAPICALLTYPE *GetCLRRuntimeHostFp)(REFIID riid, IUnknown** pUnk);

    // The name of the CoreCLR native runtime DLL.
    static PCWSTR coreClrDll = L"CoreCLR.dll";

    // The location where CoreCLR is expected to be installed. If CoreCLR.dll isn't
    // found in the same directory as the host, it will be looked for here.
    static PCWSTR coreCLRInstallDirectory = L"%windir%\\system32\\DotNetCore\\v1.0\\";

    // The location where CoreCLR PowerShell Ext binaries are expected to be installed.     
    static PCWSTR coreCLRPowerShellExtInstallDirectory = L"%windir%\\system32\\CoreClrPowerShellExt\\v1.0\\";

    // The default PowerShell install directory. This location may be overridden through a config file in %windir%\System32.
    static PCWSTR powerShellInstallPath = L"%windir%\\System32\\WindowsPowerShell\\v1.0\\";

    unsigned int PwrshCommon::IdentifyHostDirectory(
        HostEnvironment& hostEnvironment)
    {
        // Discover the path to the exe's module (powershell.exe or wsmprovhost.exe). 
        // For remoting, this is expected to be %windir%\system32 since that is the location of wsmprovhost.exe.
        wchar_t hostPath[MAX_PATH];
        DWORD thisModuleLength = sysCalls->GetModuleFileNameW(sysCalls->GetModuleHandleW(NULL), hostPath, MAX_PATH);

        if (0 == thisModuleLength)
        {
            // TODO: Use GetLastError() to find the specific error #
            return EXIT_CODE_INIT_FAILURE;
        }
        // Search for the last backslash in the host path.
        int lastBackslashIndex;
        for (lastBackslashIndex = thisModuleLength - 1; lastBackslashIndex >= 0; lastBackslashIndex--)
        {
            if (hostPath[lastBackslashIndex] == L'\\')
            {
                break;
            }
        }

        // The remaining part of the path after the last '\' is the binary name.
        hostEnvironment.SetHostBinaryName(hostPath + lastBackslashIndex + 1);

        // Copy the directory path portion of the path
        hostPath[lastBackslashIndex + 1] = '\0';
        hostEnvironment.SetHostPath(hostPath);

        hostEnvironment.SetHostDirectoryPath(powerShellInstallPath);

        return EXIT_CODE_SUCCESS;
    }

    // Attempts to load CoreCLR.dll from the specified directory.
    // On success pins the dll, sets coreCLRDirectoryPath and returns the HMODULE.
    // On failure returns NULL.
    HMODULE PwrshCommon::TryLoadCoreCLR(
        _In_ PCWSTR directoryPath)
    {
        std::wstring coreCLRPath(directoryPath);
        coreCLRPath += coreClrDll;

        HMODULE result = sysCalls->LoadLibraryExW(coreCLRPath.c_str(), NULL, 0);
        if (!result)
        {
            return NULL;
        }

        // Pin the module - CoreCLR.dll does not support being unloaded.
        HMODULE dummy_coreCLRModule;
        if (!sysCalls->GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_PIN, coreCLRPath.c_str(), &dummy_coreCLRModule))
        {
            return NULL;
        }

        return result;
    }

    unsigned int PwrshCommon::InitializeClr(
        _In_ ClrHostWrapper* hostWrapper,
        _In_ HMODULE coreClrModule)
    {
#if CORECLR
        // Get the function pointer for "GetCLRRuntimeHost"
        GetCLRRuntimeHostFp getCLRRuntimeHostfp = (GetCLRRuntimeHostFp)sysCalls->GetProcAddress(coreClrModule, "GetCLRRuntimeHost");
        if (getCLRRuntimeHostfp == NULL)
        {
            return EXIT_CODE_INIT_FAILURE;
        }

        // Get the CLR runtime host
        ICLRRuntimeHost2* pHost = NULL;
        HRESULT hResult = getCLRRuntimeHostfp(IID_ICLRRuntimeHost2, (IUnknown**)&pHost);
        if (FAILED(hResult))
        {
            return EXIT_CODE_INIT_FAILURE;
        }

        hostWrapper->SetClrHost(pHost);
        // Authenticate with either
        //  CORECLR_HOST_AUTHENTICATION_KEY  or
        //  CORECLR_HOST_AUTHENTICATION_KEY_NONGEN
        hResult = hostWrapper->Authenticate(CORECLR_HOST_AUTHENTICATION_KEY);
        if (FAILED(hResult))
        {
            return EXIT_CODE_INIT_FAILURE;
        }

        hostWrapper->SetStartupFlags((STARTUP_FLAGS)(STARTUP_SINGLE_APPDOMAIN | STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN));

        hResult = hostWrapper->Start();
        if (FAILED(hResult))
        {
            return EXIT_CODE_INIT_FAILURE;
        }
#endif
        return EXIT_CODE_SUCCESS;
    }

    bool PwrshCommon::DoesAssemblyExist(
        std::wstring& fileToTest)
    {
        FILE *file = sysCalls->_wfopen(fileToTest.c_str(), L"r");

        if (file != NULL) {
            sysCalls->fclose(file);
            return true;
        }
        return false;
    }

    // This assumes that directoryPath already includes a trailing "\\"
    void PwrshCommon::ProbeAssembly(
        _In_z_ PCWSTR directoryPath,
        _In_z_ PCWSTR assemblyName,
        std::wstring& result)
    {
        PCWSTR niExtension = L".ni.dll";
        PCWSTR ilExtension = L".dll";

        // Test NI extension first because it is preferable to IL
        std::wstring fileToTest(directoryPath);
        fileToTest += assemblyName;
        fileToTest += niExtension;
        if (DoesAssemblyExist(fileToTest)) {
            result = fileToTest;
            return;
        }

        // Check IL if NI is not present
        fileToTest = directoryPath;
        fileToTest += assemblyName;
        fileToTest += ilExtension;
        if (DoesAssemblyExist(fileToTest)) {
            result = fileToTest;
        }
    }

    // Returns the semicolon-separated list of paths to runtime dlls that are considered trusted.
    // Do not put powershell assemblies in the TPA list as it will cause 'Security Transparent V.S. Security Critical' error.
    void PwrshCommon::GetTrustedAssemblyList(
        PCWSTR coreCLRDirectoryPath,
        std::wstringstream& assemblyList,
        bool& listEmpty)
    {
        for (const wchar_t * &assembly : trustedAssemblies)
        {
            std::wstring assemblyPath;
            ProbeAssembly(coreCLRDirectoryPath, assembly, assemblyPath);

            if (assemblyPath.length() > 0)
            {
                if (listEmpty)
                    listEmpty = false;
                else
                    assemblyList << L";";
                assemblyList << assemblyPath;
            }
        }
    }

#pragma prefast(pop)

    class PwrshCommonOutputDefault : public IPwrshCommonOutput
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

    //
    //
    // The following definitions for publicly accessible functions exposed in
    // NativeMsh.h.
    //
    //

    PwrshCommon::PwrshCommon() 
        : output(new PwrshCommonOutputDefault()), sysCalls(new WinSystemCallFacade())
    {
    }

    PwrshCommon::PwrshCommon(
        IPwrshCommonOutput* outObj, 
        SystemCallFacade* systemCalls) 
        : output(outObj), sysCalls(systemCalls)
    {
        if (NULL == output)
        {
            output = new PwrshCommonOutputDefault();
        }

        if (NULL == sysCalls)
        {
            sysCalls = new WinSystemCallFacade();
        }
    }

    PwrshCommon::~PwrshCommon()
    {
        if (output)
        {
            delete output;
            output = NULL;
        }

        if (sysCalls)
        {
            delete sysCalls;
            sysCalls = NULL;
        }
    }

    bool PwrshCommon::StringIsNullOrEmpty(
        LPCWSTR wsz)
    {
        return NULL == wsz || L'\0' == wsz[0];
    }

    DWORD PwrshCommon::GetSystemErrorMessage(
        IN LONG lErrorCode,
        __deref_out_opt PWSTR * pwszErrorMessage)
    {
        DWORD dwLength = 0;
        do
        {
            if (NULL == pwszErrorMessage)
            {
                break;
            }
            *pwszErrorMessage = NULL;
            LPWSTR wszSystemErrorMessage = NULL;
            dwLength = FormatMessageW(
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER,
                NULL,
                lErrorCode,
                0,
                (LPWSTR)&wszSystemErrorMessage,
                0,
                NULL);
            if (dwLength > 0)
            {
                *pwszErrorMessage = new wchar_t[dwLength + 1];
                if (NULL != *pwszErrorMessage)
                {
                    //string function
                    if (FAILED(StringCchCopy(*pwszErrorMessage, dwLength + 1, wszSystemErrorMessage)))
                    {
                        dwLength = 0;
                        delete[](*pwszErrorMessage);
                        *pwszErrorMessage = NULL;
                    }
                }
                LocalFree(wszSystemErrorMessage);
            }
        } while (false);
        return dwLength;
    }

    bool PwrshCommon::VerifyMonadVersionFormat(
        LPCWSTR wszMonadVersion,
        int * lpMajorVersion,
        int * lpMinorVersion,
        bool bAllowMinorVersion,
        bool bReportError)
    {
        bool returnResult = true;

        do
        {
            if (StringIsNullOrEmpty(wszMonadVersion) ||
                NULL == lpMajorVersion ||
                NULL == lpMinorVersion)
            {
                returnResult = false;
                break;
            }

            WCHAR* wszRemainingVersionStringAfterMajor = NULL;
            returnResult = this->ExtractFirstVersionComponent(wszMonadVersion,
                lpMajorVersion, &wszRemainingVersionStringAfterMajor);

            if (false == returnResult)
            {
                returnResult = false;
                break;
            }

            if (NULL != wszRemainingVersionStringAfterMajor)
            {
                if (!bAllowMinorVersion)
                {
                    returnResult = false;
                    break;
                }

                WCHAR* wszRemainingVersionStringAfterMinor = NULL;
                returnResult = this->ExtractFirstVersionComponent(wszRemainingVersionStringAfterMajor,
                    lpMinorVersion, &wszRemainingVersionStringAfterMinor);

                if (!returnResult)
                {
                    break;
                }
            }
            else
            {
                *lpMinorVersion = -1;
            }
        } while (false);
        if (!returnResult && bReportError)
        {
            this->output->DisplayMessage(
                false,
                g_INVALID_MONAD_VERSION,
                wszMonadVersion);
        }
        return returnResult;
    }

#pragma prefast(push)
#pragma prefast (disable: 6101)
#pragma prefast (disable: 6054)
#pragma prefast (disable: 6001)

    unsigned int PwrshCommon::OpenEngineRegKey(
        __deref_out_ecount(1) PHKEY phEngineKey,
        __deref_out_opt PWSTR * pwszMshEngineRegKey,
        __deref_out_opt PWSTR * pwszMonadVersion,
        __inout_ecount(1) int* lpMonadMajorVersion)
    {
        unsigned int exitCode = EXIT_CODE_SUCCESS;
        do
        {
            if (NULL == phEngineKey ||
                NULL == pwszMshEngineRegKey ||
                NULL == pwszMonadVersion ||
                NULL == lpMonadMajorVersion)
            {
                exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
                break;
            }
            if (NULL == *pwszMonadVersion)
            {
                // neither -version nor -monadconsole is used,
                // need to find the latest version from the registry

                if (!this->OpenLatestMSHEngineRegistry(
                    phEngineKey,
                    pwszMshEngineRegKey,
                    pwszMonadVersion,
                    lpMonadMajorVersion))
                {
                    exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
                }
            }
            else
            {
                exitCode = this->OpenEngineRegKeyWithVersion(phEngineKey, pwszMshEngineRegKey, *pwszMonadVersion, *lpMonadMajorVersion);
            }
        } while (false);
        return exitCode;
    }

    // API used to read a particular registry key value from the PowerShellEngine
    // regkey path. For example to read "ApplicationBase" or "ConsoleHostAssemblyName"
    //
    // Note: During successful calls the following values must be freed by the caller:
    //      pwszMonadVersion
    //      pwszRuntimeVersion
    //      pwzsRegKeyValue
    //
    // The caller must take care to check to see if they must be freed during error scenarios
    // because the function may fail after allocating one or more strings.
    //
    unsigned int PwrshCommon::GetRegistryInfo(
        __deref_out_opt PWSTR * pwszMonadVersion,
        __inout_ecount(1) int * lpMonadMajorVersion,
        int monadMinorVersion,
        __deref_out_opt PWSTR * pwszRuntimeVersion,
        LPCWSTR lpszRegKeyNameToRead,
        __deref_out_opt PWSTR * pwzsRegKeyValue)
    {
        HKEY hEngineKey = NULL;
        bool bEngineKeyOpened = true;
        unsigned int exitCode = EXIT_CODE_SUCCESS;
        wchar_t * wszMshEngineRegKeyPath = NULL;
        LPWSTR wszFullMonadVersion = NULL;

        do
        {
            if (NULL == pwszMonadVersion ||
                NULL == lpMonadMajorVersion ||
                NULL == pwszRuntimeVersion ||
                NULL == pwzsRegKeyValue)
            {
                exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
                break;
            }
            exitCode = OpenEngineRegKey(&hEngineKey, &wszMshEngineRegKeyPath, pwszMonadVersion, lpMonadMajorVersion);
            if (EXIT_CODE_SUCCESS != exitCode)
            {
                bEngineKeyOpened = false;
                break;
            }

            LPCWSTR wszMshVersionRegValueName = L"PowerShellVersion";

            if (!this->RegQueryREG_SZValue(hEngineKey, wszMshVersionRegValueName, wszMshEngineRegKeyPath, &wszFullMonadVersion))
            {
                exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
                break;
            }

            //verify pwszFullMonadVersion format
            int installedMajorVersion = -1, installedMinorVersion = -1;
            if (!this->VerifyDOTNetVersionFormat(wszFullMonadVersion, &installedMajorVersion, &installedMinorVersion))
            {
                this->output->DisplayMessage(false, g_INVALID_REG_MSHVERSION_VALUE, wszMshEngineRegKeyPath, wszMshVersionRegValueName);
                exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
                break;
            }

            *lpMonadMajorVersion = installedMajorVersion;

            if (-1 != monadMinorVersion)
            {
                if (installedMinorVersion < monadMinorVersion)
                {
                    this->output->DisplayMessage(false, g_INCOMPATIBLE_MINOR_VERSION, *pwszMonadVersion);
                    exitCode = EXIT_CODE_INCOMPATIBLE_MSH_VERSION;
                    break;
                }
            }

            LPCWSTR wszRuntimeVersionRegValueName = L"RuntimeVersion";
            if (!this->RegQueryREG_SZValue(hEngineKey, wszRuntimeVersionRegValueName,
                wszMshEngineRegKeyPath, pwszRuntimeVersion))
            {
                exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
                break;
            }

            if (NULL != lpszRegKeyNameToRead)
            {
                LPCWSTR wszRequestedRegValueName = lpszRegKeyNameToRead;
                if (!this->RegQueryREG_SZValue(hEngineKey, wszRequestedRegValueName,
                    wszMshEngineRegKeyPath, pwzsRegKeyValue))
                {
                    exitCode = EXIT_CODE_READ_REGISTRY_FAILURE;
                    break;
                }
            }
        } while (false);
        if (NULL != wszMshEngineRegKeyPath)
        {
            delete[] wszMshEngineRegKeyPath;
            wszMshEngineRegKeyPath = NULL;
        }

        if (NULL != wszFullMonadVersion)
        {
            delete[] wszFullMonadVersion;
            wszFullMonadVersion = NULL;
        }

        if (bEngineKeyOpened && (NULL != hEngineKey))
        {
            LONG regCloseResult = RegCloseKey(hEngineKey);
            if (ERROR_SUCCESS != regCloseResult)
            {
                LPWSTR wszSystemErrorMessage = NULL;
                DWORD dwLength =
                    GetSystemErrorMessage(
                    regCloseResult,
                    &wszSystemErrorMessage);

                if (dwLength > 0)
                {
                    this->output->DisplayMessage(false, g_CLOSE_REG_KEY_FAILED_WITH, wszMshEngineRegKeyPath, wszSystemErrorMessage);
                    if (NULL != wszSystemErrorMessage)
                    {
                        delete[] wszSystemErrorMessage;
                        wszSystemErrorMessage = NULL;
                    }
                }
            }
            hEngineKey = NULL;
            // not return false when close registry failed
        }
        return exitCode;
    }

    unsigned int PwrshCommon::GetRegistryInfo(
        __deref_out_opt PWSTR * pwszMonadVersion,
        __inout_ecount(1) int * lpMonadMajorVersion,
        int monadMinorVersion,
        __deref_out_opt PWSTR * pwszRuntimeVersion,
        __deref_out_opt PWSTR * pwszConsoleHostAssemblyName)
    {
        return GetRegistryInfo(pwszMonadVersion,
            lpMonadMajorVersion,
            monadMinorVersion,
            pwszRuntimeVersion, L"ConsoleHostAssemblyName", pwszConsoleHostAssemblyName);
    }

    #if CORECLR
    unsigned int PwrshCommon::LaunchCoreCLR(
        ClrHostWrapper* hostWrapper,
        HostEnvironment& hostEnvironment)
    {
        unsigned int exitCode = this->IdentifyHostDirectory(hostEnvironment);
        if (EXIT_CODE_SUCCESS != exitCode)
        {
            this->output->DisplayMessage(false, g_STARTING_CLR_FAILED, GetLastError());
            return exitCode;
        }

        // Try to load from the well-known location. 
        wchar_t coreCLRInstallPath[MAX_PATH];
        exitCode = ::ExpandEnvironmentStringsW(coreCLRInstallDirectory, coreCLRInstallPath, MAX_PATH);
        if (0 == exitCode || _countof(coreCLRInstallPath) <= exitCode)
        {
            this->output->DisplayMessage(false, g_STARTING_CLR_FAILED, GetLastError());
            return EXIT_CODE_INIT_FAILURE;
        }

        HMODULE coreClrModule = this->TryLoadCoreCLR(coreCLRInstallPath);

        if (coreClrModule)
        {
            // Save the directory that CoreCLR was found in
            WCHAR coreCLRDirectoryPath[MAX_PATH];
            DWORD modulePathLength = sysCalls->GetModuleFileNameW(coreClrModule, coreCLRDirectoryPath, MAX_PATH);

            // Search for the last backslash and terminate it there to keep just the directory path with trailing slash
            for (int lastBackslashIndex = modulePathLength - 1; lastBackslashIndex >= 0; lastBackslashIndex--)
            {
                if (coreCLRDirectoryPath[lastBackslashIndex] == L'\\')
                {
                    coreCLRDirectoryPath[lastBackslashIndex + 1] = L'\0';
                    break;
                }
            }
            hostEnvironment.SetCoreCLRDirectoryPath(coreCLRDirectoryPath);
        }
        else
        {
            this->output->DisplayMessage(false, g_STARTING_CLR_FAILED, GetLastError());
            return EXIT_CODE_INIT_FAILURE;
        }

        exitCode = this->InitializeClr(hostWrapper, coreClrModule);
        if (EXIT_CODE_SUCCESS != exitCode)
        {
            this->output->DisplayMessage(false, g_STARTING_CLR_FAILED, GetLastError());
            return exitCode;
        }

        hostEnvironment.SetCoreCLRModule(coreClrModule);

        return exitCode;
    }

    unsigned int PwrshCommon::CreateAppDomain(
        ClrHostWrapper* hostWrapper,
        PCWSTR friendlyName,
        HostEnvironment& hostEnvironment)
    {
        const int     nMaxProps = 8;
        LPCWSTR       props[nMaxProps];
        LPCWSTR       vals[nMaxProps];

        if (!hostWrapper->IsInitialized())
        {
            return EXIT_CODE_INIT_FAILURE;
        }

        //PAL_LeaveHolder holder;
        DWORD dwDomainFlags = 0;
        dwDomainFlags = APPDOMAIN_SECURITY_DEFAULT;
        dwDomainFlags |= APPDOMAIN_ENABLE_ASSEMBLY_LOADFILE;
        dwDomainFlags |= APPDOMAIN_DISABLE_TRANSPARENCY_ENFORCEMENT;

        // By default CoreCLR only allows platform neutral assembly to be run. To allow
        //   assemblies marked as platform specific, include this flag
        dwDomainFlags |= APPDOMAIN_ENABLE_PLATFORM_SPECIFIC_APPS;
        // Enable PInvoke
        dwDomainFlags |= APPDOMAIN_ENABLE_PINVOKE_AND_CLASSIC_COMINTEROP;
        // This will not tear down an application if a managed exception goes unhandled 
        dwDomainFlags |= APPDOMAIN_IGNORE_UNHANDLED_EXCEPTIONS;

        int nProps = 0;
        props[nProps] = L"APPBASE";
        vals[nProps] = hostEnvironment.GetHostDirectoryPath();
        nProps++;

        // If I do not include my managed enload point dll in this list, I get a security error. 
        std::wstringstream assemblyList;
        bool listEmpty = true;
        this->GetTrustedAssemblyList(hostEnvironment.GetCoreCLRDirectoryPath(), assemblyList, listEmpty);

        wchar_t coreCLRPowerShellExtInstallPath[MAX_PATH];
        ::ExpandEnvironmentStringsW(coreCLRPowerShellExtInstallDirectory, coreCLRPowerShellExtInstallPath, MAX_PATH);
        this->GetTrustedAssemblyList(coreCLRPowerShellExtInstallPath, assemblyList, listEmpty);

        props[nProps] = L"TRUSTED_PLATFORM_ASSEMBLIES";        
        std::wstring tempStr = assemblyList.str();
        vals[nProps] = tempStr.c_str();
        nProps++;

        props[nProps] = L"APP_PATHS";
        vals[nProps] = L"";
        nProps++;

        props[nProps] = L"APP_NI_PATHS";
        vals[nProps] = L"";
        nProps++;

        // Create the customized AppDomainManager out of the SandboxHelper class
        DWORD appDomainId = INVALID_APPDOMAIN_ID;
        HRESULT hr = hostWrapper->CreateAppDomainWithManager(
            friendlyName,
            dwDomainFlags,
            NULL, // AppDomainManager is no longer required now that we can use AssemblyLoadContext to access arbitrary assemblies from within SMA.dll
            NULL,
            nProps,
            props,
            vals,
            &appDomainId);
        if (FAILED(hr))
        {
            //LONG systemErrorCode = GetLastError();
            this->output->DisplayMessage(false, g_GETTING_DEFAULT_DOMAIN_FAILED, hr);

            return EXIT_CODE_INIT_FAILURE;
        }
        hostWrapper->SetAppDomainId(appDomainId);
        return EXIT_CODE_SUCCESS;
    }

    #else // !CORECLR

    // NOTE:
    // This must be ifdef'd out of the CoreCLR build because it uses .NET 1.0
    // types that have been deprecated and removed from mscoree.h.
    //
    // This code may be removed from #if protection once ICorRuntimeHost is
    // upgraded to ICLRRuntimeHost.
    //
    unsigned int PwrshCommon::LaunchCLR(
        LPCWSTR wszMonadVersion,
        LPCWSTR wszRuntimeVersion,
        __in_ecount(1) ICorRuntimeHost** pCLR)
    {
        unsigned int exitCode = EXIT_CODE_SUCCESS;
        HRESULT hr = S_OK;
        do
        {
            // don't check StringIsNullOrEmpty(wszConsoleHostAssemblyName) here
            // because it will check below with better error reporting
            if (StringIsNullOrEmpty(wszMonadVersion) ||
                StringIsNullOrEmpty(wszRuntimeVersion))
            {
                exitCode = EXIT_CODE_INIT_FAILURE;
                break;
            }

            if (NULL == pCLR)
            {
                exitCode = EXIT_CODE_INIT_FAILURE;
                break;
            }

            SetErrorMode(SEM_FAILCRITICALERRORS);

            LPCWSTR wszCLRBuildFlavorWorkStation = L"wks";

            hr = CorBindToRuntimeEx(
                wszRuntimeVersion,
                wszCLRBuildFlavorWorkStation,             // use the workstation build of CLR
                STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN, // add STARTUP_LOADER_SAFEMODE if skipping load CLR policy
                CLSID_CorRuntimeHost,
                IID_ICorRuntimeHost,
                (PVOID*)pCLR);

            if ((CLR_E_SHIM_RUNTIMELOAD == hr) || (NULL == (*pCLR)))
            {
                this->output->DisplayMessage(false, g_CLR_VERSION_NOT_INSTALLED, wszRuntimeVersion, wszMonadVersion);
                exitCode = EXIT_CODE_INIT_FAILURE;
                break;
            }

            hr = (*pCLR)->Start();

            if (FAILED(hr))
            {
                this->output->DisplayMessage(false, g_STARTING_CLR_FAILED, hr);
                exitCode = EXIT_CODE_INIT_FAILURE;
                break;
            }
        } while (false);

        return exitCode;
    }
    #endif // !CORECLR

#pragma prefast(pop)

} // namespace NativeMsh
