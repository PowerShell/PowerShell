#pragma once

#define SUCCEEDED(Status) ((Status) >= 0)

/* PowerShell on Linux custom host interface
 *
 * startCoreCLR() takes a friendly name, e.g. "powershell", and a
 * writable pointer and identifier
 *
 * executeAssmbly() will be made available after starting CoreCLR, and
 * is used to launch assemblies with a main function
 *
 * createDelegate() works similarly but provides a reverse P/Invoke
 * given an assembly, type, and method; note that on Linux,
 * PublicKeyToken must be null
 *
 * stopCoreCLR() will deinitialize given the handle and identifier
 */

extern "C"
{
    int startCoreCLR(
        const char* appDomainFriendlyName,
        void** hostHandle,
        unsigned int* domainId);

    int stopCoreCLR(void* hostHandle, unsigned int domainId);

    /* Prototype of the coreclr_execute_assembly function from the libcoreclr.so */
    typedef int (*ExecuteAssemblyFunction)(
        void* hostHandle,
        unsigned int domainId,
        int argc,
        const char** argv,
        const char* managedAssemblyPath,
        unsigned int* exitCode);

    /* Prototype of coreclr_create_delegate function from the libcoreclr.so */
    typedef int (*CreateDelegateFunction)(
        void* hostHandle,
        unsigned int domainId,
        const char* entryPointAssemblyName,
        const char* entryPointTypeName,
        const char* entryPointMethodName,
        void** delegate);

    extern ExecuteAssemblyFunction executeAssembly;
    extern CreateDelegateFunction createDelegate;
}
