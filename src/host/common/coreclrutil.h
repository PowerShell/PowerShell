#pragma once

#ifdef __cplusplus
#include <string>
#endif

extern "C"
{
        // Paths of expected things
    int startCoreCLR(
        const char* clrAbsolutePath,
        // Passed to propertyValues
        const char* tpaList,
        const char* appPath,
        const char* nativeDllSearchDirs,
        // Passed to InitializeCoreCLRFunction
        const char* appDomainFriendlyName,
        void** hostHandle,
        unsigned int* domainId);

    int stopCoreCLR(void* hostHandle, unsigned int domainId);

    // Prototype of the coreclr_execute_assembly function from the libcoreclr.so
    typedef int (*ExecuteAssemblyFunction)(
        void* hostHandle,
        unsigned int domainId,
        int argc,
        const char** argv,
        const char* managedAssemblyPath,
        unsigned int* exitCode);

    extern ExecuteAssemblyFunction executeAssembly;

    // Prototype of coreclr_create_delegate function from the libcoreclr.so
    typedef int (*CreateDelegateFunction)(
        void* hostHandle,
        unsigned int domainId,
        const char* entryPointAssemblyName,
        const char* entryPointTypeName,
        const char* entryPointMethodName,
        void** delegate);

    extern CreateDelegateFunction createDelegate;
}

#ifdef __cplusplus
bool GetAbsolutePath(const char* path, std::string& absolutePath);
bool GetDirectory(const char* absolutePath, std::string& directory);
bool GetClrFilesAbsolutePath(const char* currentExePath, const char* clrFilesPath, std::string& clrFilesAbsolutePath);
void AddFilesFromDirectoryToTpaList(const char* directory, std::string& tpaList);
#endif
