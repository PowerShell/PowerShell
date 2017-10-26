// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
//  File:      ClrHostWrapper.h
//
//  Contents:  Wrapper for the CLR runtime host
//
// ----------------------------------------------------------------------

#pragma once

#include <string>
#include "NativeMshConstants.h"

namespace NativeMsh
{
    //
    // Abstract class to abstract CLR runtime host operations so that they can be
    // replaced with test code during test case execution.
    //
    class ClrHostWrapper
    {
    private:
        DWORD m_appDomainId;

    public:
        ClrHostWrapper() : m_appDomainId(INVALID_APPDOMAIN_ID) {}
        virtual ~ClrHostWrapper() {}

        virtual bool IsInitialized() { return false; }

        // Graceful clean up of the object to prevent leaks
        virtual unsigned int CleanUpHostWrapper() = 0;

        //
        // The following methods are direct thin wrappers for the host calls.
        //
        virtual unsigned int SetupWrapper(LPCSTR coreClrPathPtr) = 0;

        virtual int InitializeClr(
            const char* exePath,
            const char* appDomainFriendlyName,
            int propertyCount,
            const char** propertyKeys,
            const char** propertyValues) = 0;

        virtual int CreateDelegate(
            const char* entryPointAssemblyName,
            const char* entryPointTypeName,
            const char* entryPointMethodName,
            void** delegate) = 0;

        // TODO: This probably isn't needed
        virtual int ShutdownClr() = 0;
    };

    //
    // Concrete implementation of the wrapper for CoreClr.dll's
    // Platform-Agnostic hosting interface.
    //
    class CoreClrHostingApiWrapper : public ClrHostWrapper
    {
    private:
        // Handle of CoreClr.dll
        HMODULE coreClrHandle;
        HMODULE pinnedModuleHandle;

        // CoreCLR.dll API values that are hidden from the user and kept internal.
        void* hostHandle;
        unsigned int domainId;

        // The name of the CoreCLR native runtime DLL.
        PCSTR coreClrDllName = "CoreCLR.dll";

        //
        // Function Pointer Definitions for the function pointers to load from CoreCLR.dll
        //
        typedef int (STDMETHODCALLTYPE *coreclr_initialize_ptr)(
            const char* exePath,
            const char* appDomainFriendlyName,
            int propertyCount,
            const char** propertyKeys,
            const char** propertyValues,
            void** hostHandle,
            unsigned int* domainId);

        typedef int (STDMETHODCALLTYPE *coreclr_shutdown_ptr)(
            void* hostHandle,
            unsigned int domainId);

        typedef int (STDMETHODCALLTYPE *coreclr_create_delegate_ptr)(
            void* hostHandle,
            unsigned int domainId,
            const char* entryPointAssemblyName,
            const char* entryPointTypeName,
            const char* entryPointMethodName,
            void** delegate);

        // Pointers to exported functions of CoreClr.dll
        coreclr_initialize_ptr initPtr;
        coreclr_shutdown_ptr shutdownPtr;
        coreclr_create_delegate_ptr createDelegatePtr;

    public:
        CoreClrHostingApiWrapper()
            : coreClrHandle(NULL),
              pinnedModuleHandle(NULL),
              hostHandle(NULL),
              domainId(0),
              initPtr(NULL),
              shutdownPtr(NULL),
              createDelegatePtr(NULL)
        {}

        virtual ~CoreClrHostingApiWrapper()
        {
            this->CleanUpHostWrapper();
        }

        virtual bool IsInitialized()
        {
            return (NULL != coreClrHandle);
        }

        //
        // Attempts to load CoreCLR.dll from the specified directory.
        // On success pins the dll, sets coreCLRDirectoryPath and returns the HMODULE.
        // On failure returns NULL.
        //
        virtual unsigned int SetupWrapper(LPCSTR coreClrPathPtr)
        {
            std::string coreClrPath(coreClrPathPtr);
            coreClrPath += coreClrDllName;

            HMODULE result = LoadLibraryExA(coreClrPath.c_str(), NULL, 0);
            if (!result)
            {
                return EXIT_CODE_INIT_FAILURE;
            }

            // Pin the module - CoreCLR.dll does not support being unloaded.
            if (!GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_PIN, coreClrPath.c_str(), &pinnedModuleHandle))
            {
                return EXIT_CODE_INIT_FAILURE;
            }

            initPtr = (coreclr_initialize_ptr)GetProcAddress(result, "coreclr_initialize");
            shutdownPtr = (coreclr_shutdown_ptr)GetProcAddress(result, "coreclr_shutdown");
            createDelegatePtr = (coreclr_create_delegate_ptr)GetProcAddress(result, "coreclr_create_delegate");

            if (NULL == initPtr ||
                NULL == shutdownPtr ||
                NULL == createDelegatePtr)
            {
                return EXIT_CODE_INIT_FAILURE;
            }

            // Initialization succeeded. Save the handle and return success;
            coreClrHandle = result;
            return EXIT_CODE_SUCCESS;
        }

        virtual unsigned int CleanUpHostWrapper()
        {
            if (this->IsInitialized())
            {
                HRESULT status = this->ShutdownClr();
                if (FAILED(status))
                {
                    return g_STOP_CLR_HOST_FAILED;
                }

                if (this->coreClrHandle)
                {
                    // TODO: Is this comment still relevant with the new hosting API?
                    //
                    // Free the module. This is done for completeness, but in fact CoreCLR.dll
                    // was pinned earlier so this call won't actually free it. The pinning is
                    // done because CoreCLR does not support unloading.
                    ::FreeLibrary(this->coreClrHandle);
                    this->coreClrHandle = NULL;
                }
            }
            return EXIT_CODE_SUCCESS;
        }

        virtual int InitializeClr(
            const char* exePath,
            const char* appDomainFriendlyName,
            int propertyCount,
            const char** propertyKeys,
            const char** propertyValues)
        {
            if (initPtr)
            {
                return initPtr(
                        exePath,
                        appDomainFriendlyName,
                        propertyCount,
                        propertyKeys,
                        propertyValues,
                        &(this->hostHandle),
                        &(this->domainId));
            }
            return E_FAIL;
        }

        virtual int CreateDelegate(
            const char* entryPointAssemblyName,
            const char* entryPointTypeName,
            const char* entryPointMethodName,
            void** delegate)
        {
            if (createDelegatePtr)
            {
                return createDelegatePtr(
                        this->hostHandle,
                        this->domainId,
                        entryPointAssemblyName,
                        entryPointTypeName,
                        entryPointMethodName,
                        delegate);
            }
            return E_FAIL;
        }

        virtual int ShutdownClr()
        {
            if (shutdownPtr)
            {
                return shutdownPtr(
                        this->hostHandle,
                        this->domainId);
            }
            return E_FAIL;
        }
    };

    // Encapsulates the environment that CoreCLR will run in, including the TPALIST
    class HostEnvironment
    {
    private:
        // The path to this module
        char m_hostPath[MAX_PATH];
        wchar_t m_hostPathW[MAX_PATH];

        // The path to the directory containing this module
        char m_hostDirectoryPath[MAX_PATH];
        wchar_t m_hostDirectoryPathW[MAX_PATH];

        // The name of this module, without the path
        std::string m_hostBinaryName;
        std::wstring m_hostBinaryNameW;

        // The path to the directory that CoreCLR is in
        char m_coreCLRDirectoryPath[MAX_PATH];
        wchar_t m_coreCLRDirectoryPathW[MAX_PATH];

        void convertAnsiToWide(char* ansiArray, wchar_t* wideArray)
        {
            // Generate the wide version of the string and save its value;
            //
            // This is a two call function. The first call is to get the necessary length.
            // The second call is to perform the actual operation.
            int length = ::MultiByteToWideChar(CP_UTF8, 0, ansiArray, -1, NULL, 0);
            if (0 < length)
            {
                LPWSTR result = new wchar_t[length];
                if (NULL != result)
                {
                    length = ::MultiByteToWideChar(CP_UTF8, 0, ansiArray, -1, result, length);
                    if (0 < length)
                    {
                        wcscpy_s(wideArray, MAX_PATH, result);
                    }
                    delete[] result; // Free the allocated string to avoid a memory leak
                }
            }
        }

        void convertWideToAnsi(wchar_t* wideArray, char* ansiArray)
        {
            // Generate the ansi version of the string and save its value;
            //
            // This is a two call function. The first call is to get the necessary length.
            // The second call is to perform the actual operation.
            int length = ::WideCharToMultiByte(CP_ACP, 0, wideArray, -1, NULL, 0, NULL, NULL);
            if (0 < length)
            {
                LPSTR result = new char[length];
                if (NULL != result)
                {
                    length = ::WideCharToMultiByte(CP_ACP, 0, wideArray, -1, result, length, NULL, NULL);
                    if (0 < length)
                    {
                        strcpy_s(ansiArray, MAX_PATH, result);
                    }
                    delete[] result; // Free the allocated string to avoid a memory leak
                }
            }
        }

    public:

        HostEnvironment()
        {
            memset(m_hostPath, 0, sizeof(m_hostPath));
            memset(m_hostDirectoryPath, 0, sizeof(m_hostDirectoryPath));
            memset(m_coreCLRDirectoryPath, 0, sizeof(m_coreCLRDirectoryPath));
        }

        ~HostEnvironment() {}

        // Safely copies in a host path
        void SetHostPath(PCSTR hostPath)
        {
            if (hostPath)
            {
                ::ExpandEnvironmentStringsA(hostPath, m_hostPath, MAX_PATH);

                convertAnsiToWide(m_hostPath, m_hostPathW);
            }
        }
        void SetHostPathW(PCWSTR hostPath)
        {
            if (hostPath)
            {
                ::ExpandEnvironmentStringsW(hostPath, m_hostPathW, MAX_PATH);

                convertWideToAnsi(m_hostPathW, m_hostPath);
            }
        }

        // Returns the path to the host module
        PCSTR GetHostPath()
        {
            return m_hostPath;
        }

        PCWSTR GetHostPathW()
        {
            return m_hostPathW;
        }

        // Safely copies in a host binary name
        void SetHostBinaryName(PCSTR hostBinaryName)
        {
            if (hostBinaryName)
            {
                m_hostBinaryName = std::string(hostBinaryName);
            }
        }

        void SetHostBinaryNameW(PCWSTR hostBinaryName)
        {
            if (hostBinaryName)
            {
                m_hostBinaryNameW = std::wstring(hostBinaryName);
            }
        }

        // Returns the name of the host module
        PCSTR GetHostBinaryName()
        {
            return m_hostBinaryName.c_str();
        }

        PCWSTR GetHostBinaryNameW()
        {
            return m_hostBinaryNameW.c_str();
        }

        // Safely copies in a host directory path
        void SetHostDirectoryPath(PCSTR hostDirPath)
        {
            if (hostDirPath)
            {
                ::ExpandEnvironmentStringsA(hostDirPath, m_hostDirectoryPath, MAX_PATH);

                convertAnsiToWide(m_hostDirectoryPath, m_hostDirectoryPathW);
            }
        }

        void SetHostDirectoryPathW(PCWSTR hostDirPath)
        {
            if (hostDirPath)
            {
                ::ExpandEnvironmentStringsW(hostDirPath, m_hostDirectoryPathW, MAX_PATH);

                convertWideToAnsi(m_hostDirectoryPathW, m_hostDirectoryPath);
            }
        }

        // Returns the directory path of the host module
        PCSTR GetHostDirectoryPath()
        {
            return m_hostDirectoryPath;
        }

        // Returns the directory path of the host module as a wide char string
        PCWSTR GetHostDirectoryPathW()
        {
            return m_hostDirectoryPathW;
        }

        // Safely copies in a core clr directory path
        void SetCoreCLRDirectoryPath(PCSTR hostClrPath)
        {
            if (hostClrPath)
            {
                ::ExpandEnvironmentStringsA(hostClrPath, m_coreCLRDirectoryPath, MAX_PATH);

                convertAnsiToWide(m_coreCLRDirectoryPath, m_coreCLRDirectoryPathW);
            }
        }

        void SetCoreCLRDirectoryPathW(PCWSTR hostClrPath)
        {
            if (hostClrPath)
            {
                ::ExpandEnvironmentStringsW(hostClrPath, m_coreCLRDirectoryPathW, MAX_PATH);

                convertWideToAnsi(m_coreCLRDirectoryPathW, m_coreCLRDirectoryPath);
            }
        }

        // Returns the directory path of the host module
        PCSTR GetCoreCLRDirectoryPath()
        {
            return m_coreCLRDirectoryPath;
        }

        // Returns the directory path of the host module
        PCWSTR GetCoreCLRDirectoryPathW()
        {
            return m_coreCLRDirectoryPathW;
        }


    };
} // namespace NativeMsh
