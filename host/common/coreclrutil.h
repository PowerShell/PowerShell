#pragma once

#ifdef __cplusplus
#include <string>
#endif

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
        /* Passed to propertyValues */
        const char* appPath,
        const char* nativeDllSearchDirs,
        /* Passed to InitializeCoreCLRFunction */
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

#ifdef __cplusplus
// Get absolute path from the specified path.
// Return true in case of a success, false otherwise.
bool GetAbsolutePath(const char* path, std::string& absolutePath);

// Get directory of the specified path.
// Return true in case of a success, false otherwise.
bool GetDirectory(const char* absolutePath, std::string& directory);
#endif
