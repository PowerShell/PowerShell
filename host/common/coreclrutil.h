#pragma once

#include <string>

namespace CoreCLRUtil
{
    //
    // This code is mostly copied and modified from original CoreCLR project's
    // code on github: https://github.com/dotnet/coreclr
    //

    //
    // these function signatures are the entry point API for CoreCLR
    //

    // Prototype of the coreclr_initialize function from the libcoreclr.so
    typedef int (*InitializeCoreCLRFunction)(
        const char* exePath,
        const char* appDomainFriendlyName,
        int propertyCount,
        const char** propertyKeys,
        const char** propertyValues,
        void** hostHandle,
        unsigned int* domainId);

    // Prototype of the coreclr_shutdown function from the libcoreclr.so
    typedef int (*ShutdownCoreCLRFunction)(
        void* hostHandle,
        unsigned int domainId);

    // Prototype of the coreclr_execute_assembly function from the libcoreclr.so
    typedef int (*ExecuteAssemblyFunction)(
        void* hostHandle,
        unsigned int domainId,
        int argc,
        const char** argv,
        const char* managedAssemblyPath,
        unsigned int* exitCode);

    // Prototype of coreclr_create_delegate function from the libcoreclr.so
    typedef int (*CreateDelegateFunction)(
        void* hostHandle,
        unsigned int domainId,
        const char* entryPointAssemblyName,
        const char* entryPointTypeName,
        const char* entryPointMethodName,
        void** delegate);

    // The name of the CoreCLR native runtime DLL
#if defined(__APPLE__)
    constexpr char coreClrDll[] = "libcoreclr.dylib";
#else
    constexpr char coreClrDll[] = "libcoreclr.so";
#endif

    bool GetAbsolutePath(const char* path, std::string& absolutePath);
    bool GetDirectory(const char* absolutePath, std::string& directory);
    bool GetClrFilesAbsolutePath(const char* currentExePath, const char* clrFilesPath, std::string& clrFilesAbsolutePath);
    void AddFilesFromDirectoryToTpaList(const char* directory, std::string& tpaList);

} // namespace CoreCLRUtil

