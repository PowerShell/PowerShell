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
            // this should never cause overflow because VerifyInteger guarantees pwchMinorVersion
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
    static PCSTR trustedAssemblies[] =
    {
        "Microsoft.CSharp",
        "Microsoft.VisualBasic",
        "Microsoft.Win32.Primitives",
        "Microsoft.Win32.Registry.AccessControl",
        "Microsoft.Win32.Registry",
        "mscorlib",
        "System.AppContext",
        "System.Buffers",
        "System.Collections.Concurrent",
        "System.Collections",
        "System.Collections.Immutable",
        "System.Collections.NonGeneric",
        "System.Collections.Specialized",
        "System.ComponentModel.Annotations",
        "System.ComponentModel.DataAnnotations",
        "System.ComponentModel",
        "System.ComponentModel.EventBasedAsync",
        "System.ComponentModel.Primitives",
        "System.ComponentModel.TypeConverter",
        "System.Console",
        "System.Core",
        "System.Data.Common",
        "System.Diagnostics.Contracts",
        "System.Diagnostics.Debug",
        "System.Diagnostics.DiagnosticSource",
        "System.Diagnostics.FileVersionInfo",
        "System.Diagnostics.Process",
        "System.Diagnostics.StackTrace",
        "System.Diagnostics.TextWriterTraceListener",
        "System.Diagnostics.Tools",
        "System.Diagnostics.TraceSource",
        "System.Diagnostics.Tracing",
        "System",
        "System.Dynamic.Runtime",
        "System.Globalization.Calendars",
        "System.Globalization",
        "System.Globalization.Extensions",
        "System.IO.Compression",
        "System.IO.Compression.ZipFile",
        "System.IO",
        "System.IO.FileSystem.AccessControl",
        "System.IO.FileSystem",
        "System.IO.FileSystem.DriveInfo",
        "System.IO.FileSystem.Primitives",
        "System.IO.FileSystem.Watcher",
        "System.IO.MemoryMappedFiles",
        "System.IO.Packaging",
        "System.IO.Pipes",
        "System.IO.UnmanagedMemoryStream",
        "System.Linq",
        "System.Linq.Expressions",
        "System.Linq.Parallel",
        "System.Linq.Queryable",
        "System.Net",
        "System.Net.Http",
        "System.Net.Http.WinHttpHandler",
        "System.Net.NameResolution",
        "System.Net.NetworkInformation",
        "System.Net.Ping",
        "System.Net.Primitives",
        "System.Net.Requests",
        "System.Net.Security",
        "System.Net.Sockets",
        "System.Net.WebHeaderCollection",
        "System.Net.WebSockets.Client",
        "System.Net.WebSockets",
        "System.Numerics",
        "System.Numerics.Vectors",
        "System.ObjectModel",
        "System.Private.CoreLib",
        "System.Private.DataContractSerialization",
        "System.Private.ServiceModel",
        "System.Private.Uri",
        "System.Reflection.DispatchProxy",
        "System.Reflection",
        "System.Reflection.Emit",
        "System.Reflection.Emit.ILGeneration",
        "System.Reflection.Emit.Lightweight",
        "System.Reflection.Extensions",
        "System.Reflection.Metadata",
        "System.Reflection.Primitives",
        "System.Reflection.TypeExtensions",
        "System.Resources.Reader",
        "System.Resources.ResourceManager",
        "System.Runtime.CompilerServices.VisualC",
        "System.Runtime",
        "System.Runtime.Extensions",
        "System.Runtime.Handles",
        "System.Runtime.InteropServices",
        "System.Runtime.InteropServices.RuntimeInformation",
        "System.Runtime.Loader",
        "System.Runtime.Numerics",
        "System.Runtime.Serialization",
        "System.Runtime.Serialization.Json",
        "System.Runtime.Serialization.Primitives",
        "System.Runtime.Serialization.Xml",
        "System.Security.AccessControl",
        "System.Security.Claims",
        "System.Security.Cryptography.Algorithms",
        "System.Security.Cryptography.Cng",
        "System.Security.Cryptography.Csp",
        "System.Security.Cryptography.Encoding",
        "System.Security.Cryptography.OpenSsl",
        "System.Security.Cryptography.Pkcs",
        "System.Security.Cryptography.Primitives",
        "System.Security.Cryptography.X509Certificates",
        "System.Security.Principal",
        "System.Security.Principal.Windows",
        "System.Security.SecureString",
        "System.ServiceModel",
        "System.ServiceModel.Duplex",
        "System.ServiceModel.Http",
        "System.ServiceModel.NetTcp",
        "System.ServiceModel.Primitives",
        "System.ServiceModel.Security",
        "System.ServiceModel.Web",
        "System.ServiceProcess.ServiceController",
        "System.Text.Encoding.CodePages",
        "System.Text.Encoding",
        "System.Text.Encoding.Extensions",
        "System.Text.Encodings.Web",
        "System.Text.RegularExpressions",
        "System.Threading.AccessControl",
        "System.Threading",
        "System.Threading.Overlapped",
        "System.Threading.Tasks.Dataflow",
        "System.Threading.Tasks",
        "System.Threading.Tasks.Extensions",
        "System.Threading.Tasks.Parallel",
        "System.Threading.Thread",
        "System.Threading.ThreadPool",
        "System.Threading.Timer",
        "System.Windows",
        "System.Xml",
        "System.Xml.Linq",
        "System.Xml.ReaderWriter",
        "System.Xml.Serialization",
        "System.Xml.XDocument",
        "System.Xml.XmlDocument",
        "System.Xml.XmlSerializer",
        "System.Xml.XPath",
        "System.Xml.XPath.XDocument",
        "System.Xml.XPath.XmlDocument",
        "Microsoft.PowerShell.CoreCLR.AssemblyLoadContext"
    };

    // Define the function pointer for the CLR entry point
    typedef HRESULT(STDAPICALLTYPE *GetCLRRuntimeHostFp)(REFIID riid, IUnknown** pUnk);

    // The name of the CoreCLR native runtime DLL.
    static PCSTR coreClrDll = "CoreCLR.dll";

    // The location where CoreCLR is expected to be installed for inbox PowerShell. If CoreCLR.dll isn't
    // found in the same directory as the host, it will be looked for here.
    static PCSTR coreCLRInstallDirectory = "%windir%\\system32\\DotNetCore\\v1.0\\";

    // The location where CoreCLR PowerShell Ext binaries are expected to be installed for inbox PowerShell.
    static PCSTR coreCLRPowerShellExtInstallDirectory = "%windir%\\system32\\CoreClrPowerShellExt\\v1.0\\";

    // The default PowerShell install directory for inbox PowerShell. 
    // This location may be overridden by placing a config file in the same directory as the PowerShell host.
    static PCSTR powerShellInstallPath = "%windir%\\System32\\WindowsPowerShell\\v1.0\\";

    unsigned int PwrshCommon::IdentifyHostDirectory(
        HostEnvironment& hostEnvironment)
    {
        // Discover the path to the plugin or the executable (pwrshplugin.dll or powershell.exe). 
        // For PowerShell Core, the plugin no longer resides in %windir%\\system32 (it is in a sub-directory).
        // If pwrshplugin.dll is not loaded, it means that this is running via powershell.exe.
        wchar_t hostPath[MAX_PATH];
        DWORD thisModuleLength;

        if (GetModuleHandleW(L"pwrshplugin.dll")) 
        {
            thisModuleLength = GetModuleFileNameW(GetModuleHandleW(L"pwrshplugin.dll"), hostPath, MAX_PATH);
        }
        else
        {
            thisModuleLength = GetModuleFileNameW(GetModuleHandleW(NULL), hostPath, MAX_PATH);
        }
        if (0 == thisModuleLength) // Greater than zero means it is the length of the fully qualified path (without the NULL character)
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
        hostEnvironment.SetHostBinaryNameW(hostPath + lastBackslashIndex + 1);

        // Copy the directory path portion of the path
        hostPath[lastBackslashIndex + 1] = '\0';
        hostEnvironment.SetHostPathW(hostPath);

        // Read the config file to determine the appropriate host path and CoreCLR path to use.
        unsigned int result = reader->Read(hostPath);
        if (EXIT_CODE_SUCCESS == result)
        {
            // The config file was successfully parsed. Use those directories.
            hostEnvironment.SetHostDirectoryPathW(reader->GetPathToPowerShell().c_str());
            hostEnvironment.SetCoreCLRDirectoryPathW(reader->GetPathToCoreClr().c_str());
        }
        else 
        {
            // There was an issue accessing or parsing the config file OR
            // we are working for the EXE.
            //
            // TODO: This should not be the fallback for inbox PowerShell.exe. 
            // It should use coreCLRInstallDirectory and coreCLRPowerShellExtInstallDirectory. 
            //
            // Use the directory detected via GetModuleFileName + GetModuleHandle
            hostEnvironment.SetHostDirectoryPathW(hostPath);
            // At the moment, CoreCLR is in the same directory as PowerShell Core.
            // This path must be modified if we decide to use a different directory.
            hostEnvironment.SetCoreCLRDirectoryPathW(hostPath);
        }
        return EXIT_CODE_SUCCESS;
    }

    bool PwrshCommon::DoesAssemblyExist(
        std::string& fileToTest)
    {
        //FILE *file = sysCalls->fopen(fileToTest.c_str(), "r"); // TODO: Use fopen_s?
        FILE *file = NULL;
        errno_t status = sysCalls->fopen_s(&file, fileToTest.c_str(), "r");

        if (file != NULL) {
            sysCalls->fclose(file);
            return (status == 0);
        }
        return false;
    }

    // This assumes that directoryPath already includes a trailing "\\"
    void PwrshCommon::ProbeAssembly(
        _In_z_ PCSTR directoryPath,
        _In_z_ PCSTR assemblyName,
        std::string& result)
    {
        PCSTR niExtension = ".ni.dll";
        PCSTR ilExtension = ".dll";

        // Test NI extension first because it is preferable to IL
        std::string fileToTest(directoryPath);
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
        PCSTR coreCLRDirectoryPath,
        std::stringstream& assemblyList,
        bool& listEmpty)
    {
        for (const char* &assembly : trustedAssemblies)
        {
            std::string assemblyPath;
            ProbeAssembly(coreCLRDirectoryPath, assembly, assemblyPath);

            if (assemblyPath.length() > 0)
            {
                if (listEmpty)
                    listEmpty = false;
                else
                    assemblyList << ";";
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
        : output(new PwrshCommonOutputDefault()), reader(new ConfigFileReader()), sysCalls(new WinSystemCallFacade())
    {
    }

    PwrshCommon::PwrshCommon(
        IPwrshCommonOutput* outObj, 
        ConfigFileReader* rdr,
        SystemCallFacade* systemCalls) 
        : output(outObj), reader(rdr), sysCalls(systemCalls)
    {
        if (NULL == output)
        {
            output = new PwrshCommonOutputDefault();
        }

        if (NULL == reader)
        {
            reader = new ConfigFileReader();
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

        if (reader)
        {
            delete reader;
            reader = NULL;
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

    unsigned int PwrshCommon::LaunchCoreCLR(
        ClrHostWrapper* hostWrapper,
        HostEnvironment& hostEnvironment,
        PCSTR friendlyName)
    {
        unsigned int exitCode = this->IdentifyHostDirectory(hostEnvironment);
        if (EXIT_CODE_SUCCESS != exitCode)
        {
            this->output->DisplayMessage(false, g_STARTING_CLR_FAILED, GetLastError());
            return exitCode;
        }

        exitCode = hostWrapper->SetupWrapper(hostEnvironment.GetCoreCLRDirectoryPath());
        if (EXIT_CODE_SUCCESS != exitCode)
        {
            this->output->DisplayMessage(false, g_STARTING_CLR_FAILED, GetLastError());
            return exitCode;
        }
        
        const int nMaxProps = 8;
        LPCSTR props[nMaxProps];
        LPCSTR vals[nMaxProps];
        int nProps = 0;

        // The TPA list is the required list of CoreCLR assemblies that comprise
        // the trusted platform upon which PowerShell will run.
        std::stringstream assemblyList;
        bool listEmpty = true;
        this->GetTrustedAssemblyList(hostEnvironment.GetCoreCLRDirectoryPath(), assemblyList, listEmpty);

        // Fall back to attempt to load the CLR from the alternate inbox location
        // or if the ALC was not located in the CoreCLR directory.
        std::string assemblyListToSearch = assemblyList.str();
        if (listEmpty ||
            (std::string::npos == assemblyListToSearch.rfind("Microsoft.PowerShell.CoreCLR.AssemblyLoadContext")))
        {
            char coreCLRPowerShellExtInstallPath[MAX_PATH];
            ::ExpandEnvironmentStringsA(coreCLRPowerShellExtInstallDirectory, coreCLRPowerShellExtInstallPath, MAX_PATH);
            this->GetTrustedAssemblyList(coreCLRPowerShellExtInstallPath, assemblyList, listEmpty);
        }
        if (listEmpty)
        {
            // No CoreCLR assemblies were found in either location. There is no 
            // point in continuing.
            this->output->DisplayMessage(false, g_STARTING_CLR_FAILED, GetLastError());
            return EXIT_CODE_INIT_FAILURE;
        }

        props[nProps] = "TRUSTED_PLATFORM_ASSEMBLIES";        
        std::string tempStr = assemblyList.str();
        vals[nProps] = tempStr.c_str();
        nProps++;

        props[nProps] = "APP_PATHS";
        vals[nProps] = "";  // Used to be hostEnvironment.GetHostDirectoryPath()
        nProps++;

        props[nProps] = "APP_NI_PATHS";
        vals[nProps] = "";  // Used to be hostEnvironment.GetHostDirectoryPath()
        nProps++;

        int hr = hostWrapper->InitializeClr(
                hostEnvironment.GetHostDirectoryPath(), 
                friendlyName,
                nProps,
                props,
                vals);

        if (FAILED(hr))
        {
            this->output->DisplayMessage(false, g_STARTING_CLR_FAILED, GetLastError());
            return EXIT_CODE_INIT_FAILURE;
        }

        return EXIT_CODE_SUCCESS;
    }

#if !CORECLR
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
