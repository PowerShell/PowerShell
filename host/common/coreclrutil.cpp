#include "coreclrutil.h"
#include <dirent.h>
#include <dlfcn.h>
#include <assert.h>
#include <string.h>
#include <sys/stat.h>
#include <unistd.h>
#include <iostream>
#include <set>

namespace CoreCLRUtil
{
    // The name of the CoreCLR native runtime DLL
#if defined(__APPLE__)
    constexpr char coreClrDll[] = "libcoreclr.dylib";
#else
    constexpr char coreClrDll[] = "libcoreclr.so";
#endif

    void* coreclrLib;

    // Prototype of the coreclr_initialize function from the libcoreclr.so
    typedef int (*InitializeCoreCLRFunction)(
        const char* exePath,
        const char* appDomainFriendlyName,
        int propertyCount,
        const char** propertyKeys,
        const char** propertyValues,
        void** hostHandle,
        unsigned int* domainId);

    InitializeCoreCLRFunction initializeCoreCLR;

    // Prototype of the coreclr_shutdown function from the libcoreclr.so
    typedef int (*ShutdownCoreCLRFunction)(
        void* hostHandle,
        unsigned int domainId);

    ShutdownCoreCLRFunction shutdownCoreCLR;

    ExecuteAssemblyFunction executeAssembly;
    CreateDelegateFunction createDelegate;

    int startCoreClr(
        // Paths of expected things
        const char* clrAbsolutePath,
        // Passed to propertyValues
        const char* tpaList,
        const char* appPath,
        const char* nativeDllSearchDirs,
        // Passed to InitializeCoreCLRFunction
        const char* appDomainFriendlyName,
        void** hostHandle,
        unsigned int* domainId)
    {
        // the path to the CoreCLR library
        //
        // This is typically libcoreclr.so on Linux and libcoreclr.dylib on Mac
        std::string coreClrDllPath = clrAbsolutePath;
        coreClrDllPath += "/";
        coreClrDllPath += coreClrDll;

        if (coreClrDllPath.size() >= PATH_MAX)
        {
            std::cerr << "Absolute path to CoreCLR library too long" << std::endl;
            return 1;
        }

        // open the shared library
        coreclrLib = dlopen(coreClrDllPath.c_str(), RTLD_NOW|RTLD_LOCAL);
        if (coreclrLib == nullptr)
        {
            char* error = dlerror();
            std::cerr << "dlopen failed to open the CoreCLR library: " << error << std::endl;
            return 2;
        }

        // query the function pointers
        initializeCoreCLR = (InitializeCoreCLRFunction)dlsym(coreclrLib,"coreclr_initialize");
        shutdownCoreCLR = (ShutdownCoreCLRFunction)dlsym(coreclrLib,"coreclr_shutdown");

        executeAssembly = (ExecuteAssemblyFunction)dlsym(coreclrLib,"coreclr_execute_assembly");
        createDelegate = (CreateDelegateFunction)dlsym(coreclrLib,"coreclr_create_delegate");

        if (initializeCoreCLR == nullptr)
        {
            std::cerr << "function coreclr_initialize not found in CoreCLR library" << std::endl;
            return 3;
        }
        if (executeAssembly == nullptr)
        {
            std::cerr << "function coreclr_execute_assembly not found in CoreCLR library" << std::endl;
            return 3;
        }
        if (shutdownCoreCLR == nullptr)
        {
            std::cerr << "function coreclr_shutdown not found in CoreCLR library" << std::endl;
            return 3;
        }
        if (createDelegate == nullptr)
        {
            std::cerr << "function coreclr_create_delegate not found in CoreCLR library" << std::endl;
            return 3;
        }

        // create list of properties to initialize CoreCLR
        const char* propertyKeys[] = {
            "TRUSTED_PLATFORM_ASSEMBLIES",
            "APP_PATHS",
            "APP_NI_PATHS",
            "NATIVE_DLL_SEARCH_DIRECTORIES",
            "AppDomainCompatSwitch"
        };

        const char* propertyValues[] = {
            tpaList,
            appPath,
            appPath,
            nativeDllSearchDirs,
            "UseLatestBehaviorWhenTFMNotSpecified"
        };

        // get path to current executable
        char exePath[PATH_MAX] = { 0 };
        readlink("/proc/self/exe", exePath, PATH_MAX);

        // initialize CoreCLR
        int status = initializeCoreCLR(
            exePath,
            appDomainFriendlyName,
            sizeof(propertyKeys)/sizeof(propertyKeys[0]),
            propertyKeys,
            propertyValues,
            hostHandle,
            domainId);

        if (0 > status)
        {
            std::cerr << "coreclr_initialize failed - status: " << std::hex << status << std::endl;
            return 4;
        }

        return status;
    }

    int stopCoreClr(void* hostHandle, unsigned int domainId)
    {
        // shutdown CoreCLR
        int status = shutdownCoreCLR(hostHandle, domainId);
        if (0 > status)
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

    bool GetAbsolutePath(const char* path, std::string& absolutePath)
    {
        bool result = false;

        char realPath[PATH_MAX];
        if (realpath(path, realPath) != nullptr && realPath[0] != '\0')
        {
            absolutePath.assign(realPath);
            // realpath should return canonicalized path without the trailing slash
            assert(absolutePath.back() != '/');

            result = true;
        }

        return result;
    }

    bool GetDirectory(const char* absolutePath, std::string& directory)
    {
        directory.assign(absolutePath);
        size_t lastSlash = directory.rfind('/');
        if (lastSlash != std::string::npos)
        {
            directory.erase(lastSlash);
            return true;
        }

        return false;
    }

    bool GetClrFilesAbsolutePath(const char* currentExePath, const char* clrFilesPath, std::string& clrFilesAbsolutePath)
    {
        std::string clrFilesRelativePath;
        const char* clrFilesPathLocal = clrFilesPath;
        if (clrFilesPathLocal == nullptr)
        {
            // There was no CLR files path specified, use the folder of the corerun/coreconsole
            if (!GetDirectory(currentExePath, clrFilesRelativePath))
            {
                perror("Failed to get directory from argv[0]");
                return false;
            }

            clrFilesPathLocal = clrFilesRelativePath.c_str();

            // TODO: consider using an env variable (if defined) as a fall-back.
            // The windows version of the corerun uses core_root env variable
        }

        if (!GetAbsolutePath(clrFilesPathLocal, clrFilesAbsolutePath))
        {
            perror("Failed to convert CLR files path to absolute path");
            return false;
        }

        return true;
    }

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
        for (int extIndex = 0; extIndex < sizeof(tpaExtensions) / sizeof(tpaExtensions[0]); extIndex++)
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

        // strip any trailing : from the tpaList
        if (tpaList.size() > 0 && tpaList[tpaList.size()-1] == ':')
            tpaList.resize(tpaList.size()-1);
    }
}
