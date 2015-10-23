#pragma once

#define SUCCEEDED(Status) ((Status) >= 0)

/* PowerShell on Linux custom host interface
 *
 * startCoreCLR() and stopCoreCLR() wrap the initialization of CoreCLR
 * and use of ExecuteAssemblyFunction() and CreateDelegateFunction()
 * should be sandwiched in between them
 *
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
