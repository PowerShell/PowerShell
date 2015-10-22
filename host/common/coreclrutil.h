#pragma once

#ifdef __cplusplus
#include <string>
#endif

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
        /* Paths of expected things */
        const char* clrAbsolutePath,
        /* Passed to propertyValues */
        const char* tpaList,
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

//
// Get the absolute path to use to locate libcoreclr.so and the CLR assemblies are stored. If clrFilesPath is provided,
// this function will return the absolute path to it. Otherwise, the directory of the current executable is used.
//
// Return true in case of a success, false otherwise.
//
bool GetClrFilesAbsolutePath(const char* currentExePath, const char* clrFilesPath, std::string& clrFilesAbsolutePath);

// Add all *.dll, *.ni.dll, *.exe, and *.ni.exe files from the specified directory to the tpaList string.
void AddFilesFromDirectoryToTpaList(const char* directory, std::string& tpaList);
#endif
