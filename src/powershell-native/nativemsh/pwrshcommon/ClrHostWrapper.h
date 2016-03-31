// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (C) Microsoft Corporation, 2014.
//
//  File:      ClrHostWrapper.h
//
//  Contents:  Wrapper for the CLR runtime host
//
// ----------------------------------------------------------------------

#pragma once

#include <string>
#include <mscoree.h>
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

        // For setting a host ptr since it is not known at container creation time. 
        // TODO: Can I make it immutable?
        virtual void SetClrHost(
            void* hostPtr) = 0;

        // Graceful clean up of the object to prevent leaks
        virtual unsigned int CleanUpHostWrapper() = 0;

        void SetAppDomainId(DWORD id)
        {
            m_appDomainId = id;
        }

        DWORD GetAppDomainId()
        {
            return m_appDomainId;
        }

        //
        // The following methods are direct thin wrappers for the host calls.
        //
        virtual HRESULT STDMETHODCALLTYPE CreateAppDomainWithManager(
            /* [in] */ LPCWSTR wszFriendlyName,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR wszAppDomainManagerAssemblyName,
            /* [in] */ LPCWSTR wszAppDomainManagerTypeName,
            /* [in] */ int nProperties,
            /* [in] */ LPCWSTR *pPropertyNames,
            /* [in] */ LPCWSTR *pPropertyValues,
            /* [out] */ DWORD *pAppDomainID) = 0;

        virtual HRESULT STDMETHODCALLTYPE UnloadAppDomain(
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ BOOL fWaitUntilDone) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateDelegate(
            /* [in] */ DWORD appDomainID,
            /* [in] */ LPCWSTR wszAssemblyName,
            /* [in] */ LPCWSTR wszClassName,
            /* [in] */ LPCWSTR wszMethodName,
            /* [out] */ INT_PTR *fnPtr) = 0;

        virtual HRESULT STDMETHODCALLTYPE Authenticate(
            /* [in] */ ULONGLONG authKey) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetStartupFlags(
            /* [in] */ STARTUP_FLAGS dwFlags) = 0;

        virtual HRESULT STDMETHODCALLTYPE Start() = 0;

        virtual HRESULT STDMETHODCALLTYPE Stop() = 0;

        virtual HRESULT STDMETHODCALLTYPE Release() = 0;
    };

#if CORECLR
    //
    // Concrete implementation of the wrapper for the ICLRRuntimeHost2 interface.
    //
    class ICLRRuntimeHost2Wrapper : public ClrHostWrapper
    {
    private:
        ICLRRuntimeHost2* pHost;

    public:
        ICLRRuntimeHost2Wrapper() : pHost(NULL) {}
        virtual ~ICLRRuntimeHost2Wrapper() 
        {
            CleanUpHostWrapper();
        }

        virtual bool IsInitialized() { return (pHost != NULL); }

        // For setting a host ptr since it is not known at container creation time. 
        virtual void SetClrHost(void* hostPtr)
        {
            pHost = (ICLRRuntimeHost2*)hostPtr;
        }

        virtual unsigned int CleanUpHostWrapper()
        {
            if (this->IsInitialized())
            {
                if (FAILED(this->UnloadAppDomain(this->GetAppDomainId(), true)))
                {
                    return g_UNLOAD_APPDOMAIN_FAILED;
                }

                if (FAILED(this->Stop()))
                {
                    return g_STOP_CLR_HOST_FAILED;
                }

                // Release the reference to the host
                if (FAILED(this->Release()))
                {
                    return g_RELEASE_CLR_HOST_FAILED;
                }
                this->SetClrHost(NULL);
            }
            return EXIT_CODE_SUCCESS;
        }

        virtual HRESULT STDMETHODCALLTYPE CreateAppDomainWithManager(
            /* [in] */ LPCWSTR wszFriendlyName,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR wszAppDomainManagerAssemblyName,
            /* [in] */ LPCWSTR wszAppDomainManagerTypeName,
            /* [in] */ int nProperties,
            /* [in] */ LPCWSTR *pPropertyNames,
            /* [in] */ LPCWSTR *pPropertyValues,
            /* [out] */ DWORD *pAppDomainID)
        {
            if (pHost)
            {
                return pHost->CreateAppDomainWithManager(
                    wszFriendlyName,
                    dwFlags,
                    wszAppDomainManagerAssemblyName,
                    wszAppDomainManagerTypeName,
                    nProperties,
                    pPropertyNames,
                    pPropertyValues,
                    pAppDomainID);
            }
            return E_FAIL;
        }

        virtual HRESULT STDMETHODCALLTYPE UnloadAppDomain(
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ BOOL fWaitUntilDone)
        {
            if (pHost)
            {
                return pHost->UnloadAppDomain(
                    dwAppDomainId,
                    fWaitUntilDone);
            }
            return E_FAIL;
        }

        virtual HRESULT STDMETHODCALLTYPE CreateDelegate(
            /* [in] */ DWORD appDomainID,
            /* [in] */ LPCWSTR wszAssemblyName,
            /* [in] */ LPCWSTR wszClassName,
            /* [in] */ LPCWSTR wszMethodName,
            /* [out] */ INT_PTR *fnPtr)
        {
            if (pHost)
            {
                return pHost->CreateDelegate(
                    appDomainID,
                    wszAssemblyName,
                    wszClassName,
                    wszMethodName,
                    fnPtr);
            }
            return E_FAIL;
        }

        virtual HRESULT STDMETHODCALLTYPE Authenticate(
            /* [in] */ ULONGLONG authKey)
        {
            if (pHost)
            {
                return pHost->Authenticate(
                    authKey);
            }
            return E_FAIL;
        }

        virtual HRESULT STDMETHODCALLTYPE SetStartupFlags(
            /* [in] */ STARTUP_FLAGS dwFlags)
        {
            if (pHost)
            {
                return pHost->SetStartupFlags(
                    dwFlags);
            }
            return E_FAIL;
        }

        virtual HRESULT STDMETHODCALLTYPE Start()
        {
            if (pHost)
            {
                return pHost->Start();
            }
            return E_FAIL;
        }

        virtual HRESULT STDMETHODCALLTYPE Stop()
        {
            if (pHost)
            {
                return pHost->Stop();
            }
            return E_FAIL;
        }

        virtual HRESULT STDMETHODCALLTYPE Release()
        {
            if (pHost)
            {
                return pHost->Release();
            }
            return E_FAIL;
        }
    };
#endif

    // Encapsulates the environment that CoreCLR will run in, including the TPALIST
    class HostEnvironment
    {
    private:
        // The path to this module
        wchar_t m_hostPath[MAX_PATH];

        // The path to the directory containing this module
        wchar_t m_hostDirectoryPath[MAX_PATH];

        // The name of this module, without the path
        std::wstring m_hostBinaryName;

        // The path to the directory that CoreCLR is in
        wchar_t m_coreCLRDirectoryPath[MAX_PATH];

        HMODULE m_coreCLRModule;

    public:

        HostEnvironment()
            : m_coreCLRModule(0)
        {
            memset(m_hostPath, 0, sizeof(m_hostPath));
            memset(m_hostDirectoryPath, 0, sizeof(m_hostDirectoryPath));
            memset(m_coreCLRDirectoryPath, 0, sizeof(m_coreCLRDirectoryPath));
        }

        ~HostEnvironment()
        {
            if (m_coreCLRModule)
            {
                // Free the module. This is done for completeness, but in fact CoreCLR.dll
                // was pinned earlier so this call won't actually free it. The pinning is
                // done because CoreCLR does not support unloading.
                ::FreeLibrary(m_coreCLRModule);
            }
        }

        // Safely copies in a host path
        void SetHostPath(PCWSTR hostPath)
        {
            if (hostPath)
            {
                ::ExpandEnvironmentStringsW(hostPath, m_hostPath, MAX_PATH);
                //::wcsncpy_s(m_hostPath, hostPath, _TRUNCATE);
            }
        }

        // Returns the path to the host module
        PCWSTR GetHostPath()
        {
            return m_hostPath;
        }

        // Safely copies in a host binary name
        void SetHostBinaryName(PCWSTR hostBinaryName)
        {
            if (hostBinaryName)
            {
                m_hostBinaryName = std::wstring(hostBinaryName);
            }
        }

        // Returns the name of the host module
        PCWSTR GetHostBinaryName()
        {
            return m_hostBinaryName.c_str();
        }

        // Safely copies in a host directory path
        void SetHostDirectoryPath(PCWSTR hostDirPath)
        {
            if (hostDirPath)
            {
                ::ExpandEnvironmentStringsW(hostDirPath, m_hostDirectoryPath, MAX_PATH);
                //::wcsncpy_s(m_hostDirectoryPath, hostDirPath, _TRUNCATE);
            }
        }

        // Returns the directory path of the host module
        PCWSTR GetHostDirectoryPath()
        {
            return m_hostDirectoryPath;
        }

        // Safely copies in a core clr directory path
        void SetCoreCLRDirectoryPath(PCWSTR hostClrPath)
        {
            if (hostClrPath)
            {
                ::ExpandEnvironmentStringsW(hostClrPath, m_coreCLRDirectoryPath, MAX_PATH);
                //::wcsncpy_s(m_coreCLRDirectoryPath, hostClrPath, _TRUNCATE);
            }
        }

        // Returns the directory path of the host module
        PCWSTR GetCoreCLRDirectoryPath()
        {
            return m_coreCLRDirectoryPath;
        }

        void SetCoreCLRModule(HMODULE module)
        {
            m_coreCLRModule = module;
        }
    };
} // namespace NativeMsh