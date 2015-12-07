#include "coreclrutil.h"
#include <dirent.h>
#include <dlfcn.h>
#include <sys/stat.h>
#include <iostream>
#include <set>

#include <boost/filesystem.hpp>
using namespace boost;

// The name of the CoreCLR native runtime DLL
#if defined(__APPLE__)
constexpr char coreClrDll[] = "libcoreclr.dylib";
#else
constexpr char coreClrDll[] = "libcoreclr.so";
#endif

void* coreclrLib;
char coreRoot[PATH_MAX];

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

InitializeCoreCLRFunction initializeCoreCLR;
ShutdownCoreCLRFunction shutdownCoreCLR;
ExecuteAssemblyFunction executeAssembly;
CreateDelegateFunction createDelegate;

//
// Get the absolute path given the environment variable.
//
// Return true in case of a success, false otherwise.
//
filesystem::path GetEnvAbsolutePath(const char* env)
{
    const char* local = std::getenv(env);
    if (!local)
        return nullptr;

    return filesystem::canonical(local);
}

// Add all *.dll, *.ni.dll, *.exe, and *.ni.exe files from the specified directory to the tpaList string.
// Note: copied from unixcorerun
void AddFilesFromDirectoryToTpaList(const char* directory, std::string& tpaList)
{
    const char * const tpaExtensions[] = {
        ".ni.dll",      // Probe for .ni.dll first so that it's preferred if ni and il coexist in the same dir
        ".dll",
        ".ni.exe",
        ".exe",
    };

    DIR* dir = opendir(directory);
    if (dir == nullptr)
    {
        return;
    }

    std::set<std::string> addedAssemblies;

    // Walk the directory for each extension separately so that we first get files with .ni.dll extension,
    // then files with .dll extension, etc.
    for (unsigned int extIndex = 0; extIndex < sizeof(tpaExtensions) / sizeof(tpaExtensions[0]); extIndex++)
    {
        const char* ext = tpaExtensions[extIndex];
        int extLength = strlen(ext);

        struct dirent* entry;

        // For all entries in the directory
        while ((entry = readdir(dir)) != nullptr)
        {
            // We are interested in files only
            switch (entry->d_type)
            {
            case DT_REG:
                break;

                // Handle symlinks and file systems that do not support d_type
            case DT_LNK:
            case DT_UNKNOWN:
            {
                std::string fullFilename;

                fullFilename.append(directory);
                fullFilename.append("/");
                fullFilename.append(entry->d_name);

                struct stat sb;
                if (stat(fullFilename.c_str(), &sb) == -1)
                {
                    continue;
                }

                if (!S_ISREG(sb.st_mode))
                {
                    continue;
                }
            }
            break;

            default:
                continue;
            }

            std::string filename(entry->d_name);

            // Check if the extension matches the one we are looking for
            int extPos = filename.length() - extLength;
            if ((extPos <= 0) || (filename.compare(extPos, extLength, ext) != 0))
            {
                continue;
            }

            std::string filenameWithoutExt(filename.substr(0, extPos));

            // Make sure if we have an assembly with multiple extensions present,
            // we insert only one version of it.
            if (addedAssemblies.find(filenameWithoutExt) == addedAssemblies.end())
            {
                addedAssemblies.insert(filenameWithoutExt);

                tpaList.append(directory);
                tpaList.append("/");
                tpaList.append(filename);
                tpaList.append(":");
            }
        }

        // Rewind the directory stream to be able to iterate over it for the next extension
        rewinddir(dir);
    }

    closedir(dir);
}

//
// Below is our custom start/stop interface
//
int startCoreCLR(
    const char* appDomainFriendlyName,
    void** hostHandle,
    unsigned int* domainId)
{
    char exePath[PATH_MAX];

    // get path to current executable
    readlink("/proc/self/exe", exePath, PATH_MAX);

    // get the CoreCLR root path
    auto clrAbsolutePath = GetEnvAbsolutePath("CORE_ROOT");
    if(!clrAbsolutePath.is_absolute())
    {
        std::cerr << "Failed to get CORE_ROOT path" << std::endl;
        return -1;
    }
    // copy to shared buffer
    clrAbsolutePath.native().copy(coreRoot, clrAbsolutePath.native().size(), 0);

    // get the CoreCLR shared library path
    filesystem::path coreClrDllPath(clrAbsolutePath);
    coreClrDllPath /= coreClrDll;

    // open the shared library
    coreclrLib = dlopen(coreClrDllPath.c_str(), RTLD_NOW|RTLD_LOCAL);
    if (coreclrLib == nullptr)
    {
        std::cerr << "dlopen failed to open the CoreCLR library: " << dlerror() << std::endl;
        return -1;
    }

    // query and verify the function pointers
    initializeCoreCLR = (InitializeCoreCLRFunction)dlsym(coreclrLib,"coreclr_initialize");
    shutdownCoreCLR = (ShutdownCoreCLRFunction)dlsym(coreclrLib,"coreclr_shutdown");
    executeAssembly = (ExecuteAssemblyFunction)dlsym(coreclrLib,"coreclr_execute_assembly");
    createDelegate = (CreateDelegateFunction)dlsym(coreclrLib,"coreclr_create_delegate");

    if (initializeCoreCLR == nullptr)
    {
        std::cerr << "function coreclr_initialize not found in CoreCLR library" << std::endl;
        return -1;
    }
    if (executeAssembly == nullptr)
    {
        std::cerr << "function coreclr_execute_assembly not found in CoreCLR library" << std::endl;
        return -1;
    }
    if (shutdownCoreCLR == nullptr)
    {
        std::cerr << "function coreclr_shutdown not found in CoreCLR library" << std::endl;
        return -1;
    }
    if (createDelegate == nullptr)
    {
        std::cerr << "function coreclr_create_delegate not found in CoreCLR library" << std::endl;
        return -1;
    }

    // generate the Trusted Platform Assemblies list
    std::string tpaList;

    // add assemblies in the CoreCLR root path
    AddFilesFromDirectoryToTpaList(clrAbsolutePath.c_str(), tpaList);

    // create list of properties to initialize CoreCLR
    const char* propertyKeys[] = {
        "TRUSTED_PLATFORM_ASSEMBLIES",
        "APP_PATHS",
        "APP_NI_PATHS",
        "NATIVE_DLL_SEARCH_DIRECTORIES",
        "AppDomainCompatSwitch"
    };

    const char* propertyValues[] = {
        tpaList.c_str(),
        clrAbsolutePath.c_str(),
        clrAbsolutePath.c_str(),
        clrAbsolutePath.c_str(),
        "UseLatestBehaviorWhenTFMNotSpecified"
    };

    // initialize CoreCLR
    int status = initializeCoreCLR(
        exePath,
        appDomainFriendlyName,
        sizeof(propertyKeys)/sizeof(propertyKeys[0]),
        propertyKeys,
        propertyValues,
        hostHandle,
        domainId);

    if (!SUCCEEDED(status))
    {
        std::cerr << "coreclr_initialize failed - status: " << std::hex << status << std::endl;
        return -1;
    }

    return status;
}

int stopCoreCLR(void* hostHandle, unsigned int domainId)
{
    // shutdown CoreCLR
    int status = shutdownCoreCLR(hostHandle, domainId);
    if (!SUCCEEDED(status))
    {
        std::cerr << "coreclr_shutdown failed - status: " << std::hex << status << std::endl;
    }

    // close the dynamic library
    if (0 != dlclose(coreclrLib))
    {
        std::cerr << "failed to close CoreCLR library" << std::endl;
    }

    return status;
}
