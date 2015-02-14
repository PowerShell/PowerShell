#pragma once

#include "pal.h"
#include "rt/palrt.h"

//!
//! This header pulls in actual items from the original mscoree.h and also
//! defines stub classes and functions.
//!

typedef /* [public][public] */
enum __MIDL___MIDL_itf_mscoree_0000_0000_0002
{
    STARTUP_CONCURRENT_GC   = 0x1,
    STARTUP_LOADER_OPTIMIZATION_MASK        = ( 0x3 << 1 ) ,
    STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN       = ( 0x1 << 1 ) ,
    STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN        = ( 0x2 << 1 ) ,
    STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN_HOST   = ( 0x3 << 1 ) ,
    STARTUP_LOADER_SAFEMODE = 0x10,
    STARTUP_LOADER_SETPREFERENCE    = 0x100,
    STARTUP_SERVER_GC       = 0x1000,
    STARTUP_HOARD_GC_VM     = 0x2000,
    STARTUP_SINGLE_VERSION_HOSTING_INTERFACE        = 0x4000,
    STARTUP_LEGACY_IMPERSONATION    = 0x10000,
    STARTUP_DISABLE_COMMITTHREADSTACK       = 0x20000,
    STARTUP_ALWAYSFLOW_IMPERSONATION        = 0x40000,
    STARTUP_TRIM_GC_COMMIT  = 0x80000,
    STARTUP_ETW     = 0x100000,
    STARTUP_ARM     = 0x400000,
    STARTUP_SINGLE_APPDOMAIN        = 0x800000,
    STARTUP_APPX_APP_MODEL  = 0x1000000,
    STARTUP_DISABLE_RANDOMIZED_STRING_HASHING       = 0x2000000
}   STARTUP_FLAGS;


// other datatypes

typedef HRESULT ( __stdcall *FExecuteInAppDomainCallback )(
        void *cookie);


// interfaces

class IUnknown
{
public:
    ULONG AddRef()
    {
        return 0;
    }
    ULONG Release()
    {
        return 0;
    }
    HRESULT QueryInterface(
      REFIID riid,
      void **ppvObject
    )
    {
        return S_OK;
    }
};

struct IActivationFactory
{};

class IHostControl : public IUnknown
{
public:
virtual HRESULT STDMETHODCALLTYPE GetHostManager(
        /* [in] */ REFIID riid,
        /* [out] */ void **ppObject) = 0;

virtual HRESULT STDMETHODCALLTYPE SetAppDomainManager(
        /* [in] */ DWORD dwAppDomainID,
        /* [in] */ IUnknown *pUnkAppDomainManager) = 0;

};

class ICLRControl : public IUnknown
{
public:
virtual HRESULT STDMETHODCALLTYPE GetCLRManager(
        /* [in] */ REFIID riid,
        /* [out] */ void **ppObject) = 0;

virtual HRESULT STDMETHODCALLTYPE SetAppDomainManagerType(
        /* [in] */ LPCWSTR pwzAppDomainManagerAssembly,
        /* [in] */ LPCWSTR pwzAppDomainManagerType) = 0;

};

class ICLRRuntimeHost : public IUnknown
{
public:
virtual HRESULT STDMETHODCALLTYPE Start( void) = 0;

virtual HRESULT STDMETHODCALLTYPE Stop( void) = 0;

virtual HRESULT STDMETHODCALLTYPE SetHostControl(
        /* [in] */ IHostControl *pHostControl) = 0;

virtual HRESULT STDMETHODCALLTYPE GetCLRControl(
        /* [out] */ ICLRControl **pCLRControl) = 0;

virtual HRESULT STDMETHODCALLTYPE UnloadAppDomain(
        /* [in] */ DWORD dwAppDomainId,
/* [in] */ BOOL fWaitUntilDone) = 0;

virtual HRESULT STDMETHODCALLTYPE ExecuteInAppDomain(
        /* [in] */ DWORD dwAppDomainId,
/* [in] */ FExecuteInAppDomainCallback pCallback,
/* [in] */ void *cookie) = 0;

virtual HRESULT STDMETHODCALLTYPE GetCurrentAppDomainId(
        /* [out] */ DWORD *pdwAppDomainId) = 0;

virtual HRESULT STDMETHODCALLTYPE ExecuteApplication(
        /* [in] */ LPCWSTR pwzAppFullName,
/* [in] */ DWORD dwManifestPaths,
/* [in] */ LPCWSTR *ppwzManifestPaths,
/* [in] */ DWORD dwActivationData,
/* [in] */ LPCWSTR *ppwzActivationData,
/* [out] */ int *pReturnValue) = 0;

virtual HRESULT STDMETHODCALLTYPE ExecuteInDefaultAppDomain(
        /* [in] */ LPCWSTR pwzAssemblyPath,
/* [in] */ LPCWSTR pwzTypeName,
/* [in] */ LPCWSTR pwzMethodName,
/* [in] */ LPCWSTR pwzArgument,
/* [out] */ DWORD *pReturnValue) = 0;

};

class ICLRRuntimeHost2 : public ICLRRuntimeHost
{
public:
virtual HRESULT STDMETHODCALLTYPE CreateAppDomainWithManager(
        /* [in] */ LPCWSTR wszFriendlyName,
/* [in] */ DWORD dwFlags,
/* [in] */ LPCWSTR wszAppDomainManagerAssemblyName,
/* [in] */ LPCWSTR wszAppDomainManagerTypeName,
/* [in] */ int nProperties,
/* [in] */ LPCWSTR *pPropertyNames,
/* [in] */ LPCWSTR *pPropertyValues,
/* [out] */ DWORD *pAppDomainID) = 0;

virtual HRESULT STDMETHODCALLTYPE CreateDelegate(
        /* [in] */ DWORD appDomainID,
/* [in] */ LPCWSTR wszAssemblyName,
/* [in] */ LPCWSTR wszClassName,
/* [in] */ LPCWSTR wszMethodName,
/* [out] */ INT_PTR *fnPtr) = 0;

virtual HRESULT STDMETHODCALLTYPE Authenticate(
        /* [in] */ ULONGLONG authKey) = 0;

virtual HRESULT STDMETHODCALLTYPE RegisterMacEHPort( void) = 0;

virtual HRESULT STDMETHODCALLTYPE SetStartupFlags(
        /* [in] */ STARTUP_FLAGS dwFlags) = 0;

virtual HRESULT STDMETHODCALLTYPE DllGetActivationFactory(
        /* [in] */ DWORD appDomainID,
/* [in] */ LPCWSTR wszTypeName,
/* [out] */ IActivationFactory **factory) = 0;

virtual HRESULT STDMETHODCALLTYPE ExecuteAssembly(
        /* [in] */ DWORD dwAppDomainId,
/* [in] */ LPCWSTR pwzAssemblyPath,
/* [in] */ int argc,
/* [in] */ LPCWSTR *argv,
/* [out] */ DWORD *pReturnValue) = 0;

};
