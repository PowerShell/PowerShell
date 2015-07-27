#include <string>
#include <iostream>
#include "coreclrutil.h"
#include <limits.h>
#include <dlfcn.h>

int main(int argc, char** argv)
{
    // TODO: read from command line args
    std::string clrAbsolutePath = "/home/peter/gitwd/monad-linux/scripts/exec_env/app_base";

    // managed assembly arguments
    int managedAssemblyArgc = 0;
    const char* managedAssemblyArgv[] = { "" };

    // there are two assemblies involved in hosting PowerShell:
    // - Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll
    //   + this assembly has to be listed as platform assembly
    // - System.Management.Automation.dll
    std::string powershellBaseAbsolutePath = "/home/peter/gitwd/monad-linux/scripts/exec_env/app_base";
    std::string assemblyLoadContextAssemblyName = "Microsoft.PowerShell.CoreCLR.AssemblyLoadContext";
    std::string assemblyLoadContextAbsolutePath = powershellBaseAbsolutePath + "/" + assemblyLoadContextAssemblyName + ".dll";
    std::string systemManagementAutomationAssemblyName = "System.Management.Automation";
    std::string systemManagementAutomationAbsolutePath = powershellBaseAbsolutePath + "/" + systemManagementAutomationAssemblyName + ".dll";

    std::string coreClrDllPath = clrAbsolutePath + "/" + CoreCLRUtil::coreClrDll;
    if (coreClrDllPath.size() >= PATH_MAX)
    {
        std::cerr << "Absolute path to CoreCLR library too long" << std::endl;
        return 1;
    }
    std::cout << "coreClrDllPath: " << coreClrDllPath << std::endl;

    // TPA list
    //
    // The list of platform assemblies must include all CoreCLR assemblies
    // and the Microsoft.PowerShell.CoreCLR.AssemblyLoadContext
    //
    // TODO: move CLR assemblies to separate path during build&run make steps
    // TODO: only add assembly load context to TPA list, not any other PS dll
    
    std::string tpaList;
    CoreCLRUtil::AddFilesFromDirectoryToTpaList(clrAbsolutePath.c_str(),tpaList);
    std::cout << "tpaList: " << tpaList << std::endl;

    // assembly load paths
    //
    // All PowerShell assemblies are assumed to be in the same path

    std::string appPath;
    if (!CoreCLRUtil::GetDirectory(assemblyLoadContextAbsolutePath.c_str(),appPath))
    {
        std::cerr << "failed to get assembly search directory from assembly absolute path" << std::endl;
        return 1;
    }
    std::cout << "appPath: " << appPath << std::endl;

    // search paths for native dlls
    //
    // Add both the CoreCLR directory and the PowerShell directory to this list
    std::string nativeDllSearchDirs = appPath + ":" + clrAbsolutePath;

    // get the absolute path of the current executable
    std::string currentExeAbsolutePath;
    if (!CoreCLRUtil::GetAbsolutePath(argv[0],currentExeAbsolutePath))
    {
        std::cerr << "could not get absolute path of current executable" << std::endl;
        return 1;
    }

    // open the shared library
    void* coreclrLib = dlopen(coreClrDllPath.c_str(), RTLD_NOW|RTLD_LOCAL);
    if (coreclrLib == nullptr)
    {
        char* error = dlerror();
        std::cerr << "dlopen failed to open the CoreCLR library: " << error << std::endl;
        return 2;
    }

    // query the function pointers
    CoreCLRUtil::InitializeCoreCLRFunction initializeCoreCLR = (CoreCLRUtil::InitializeCoreCLRFunction)dlsym(coreclrLib,"coreclr_initialize");
    CoreCLRUtil::ExecuteAssemblyFunction executeAssembly = (CoreCLRUtil::ExecuteAssemblyFunction)dlsym(coreclrLib,"coreclr_execute_assembly");
    CoreCLRUtil::ShutdownCoreCLRFunction shutdownCoreCLR = (CoreCLRUtil::ShutdownCoreCLRFunction)dlsym(coreclrLib,"coreclr_shutdown");
    CoreCLRUtil::CreateDelegateFunction createDelegate = (CoreCLRUtil::CreateDelegateFunction)dlsym(coreclrLib,"coreclr_create_delegate");

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
        tpaList.c_str(),
        appPath.c_str(),
        appPath.c_str(),
        nativeDllSearchDirs.c_str(),
        "UseLatestBehaviorWhenTFMNotSpecified"
    };


    // initialize CoreCLR
    void* hostHandle;
    unsigned int domainId;
    int status = initializeCoreCLR(
                        currentExeAbsolutePath.c_str(),
                        "ps_cmdline_host",
                        sizeof(propertyKeys)/sizeof(propertyKeys[0]),
                        propertyKeys,
                        propertyValues,
                        &hostHandle,
                        &domainId);

    if (0 > status)
    {
        std::cerr << "coreclr_initialize failed - status: " << std::hex << status << std::endl;
        return 4;
    }

    // initialize the PS's custom assembly load context
    typedef void (*LoaderRunHelperFp)(const char16_t* appPath);
    LoaderRunHelperFp loaderDelegate = nullptr;
    status = createDelegate(
                        hostHandle,
                        domainId,
                        "Microsoft.PowerShell.CoreCLR.AssemblyLoadContext, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                        "System.Management.Automation.PowerShellAssemblyLoadContextInitializer",
                        "SetPowerShellAssemblyLoadContext",
                        (void**)&loaderDelegate);
    if (0 > status)
    {
        std::cerr << "could not create delegate for SetPowerShellAssemblyLoadContext - status: " << std::hex << status << std::endl;
        return 4;
    }

    loaderDelegate(u"/home/peter/gitwd/monad-linux/scripts/exec_env/app_base");


    typedef int (*TestDelegate)();
    TestDelegate testDelegate = nullptr;
    status = createDelegate(
                        hostHandle,
                        domainId,
                        "System.Management.Automation, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                        "System.Management.Automation.Platform",
                        "IsLinux",
                        (void**)&testDelegate);
    int returnValue = testDelegate();
    std::cout << "returnValue=" << returnValue << std::endl;


    typedef void (*UnmanagedEntry)();
    UnmanagedEntry unmanagedEntry = nullptr;
    status = createDelegate(
                        hostHandle,
                        domainId,
                        "powershell-simple, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                        "ps_hello_world.Program",
                        "UnmanagedEntry",
                        (void**)&unmanagedEntry);
    if (0 > status)
    {
        std::cerr << "could not create delegate for UnmanagedEntry - status: " << std::hex << status << std::endl;
        return 4;
    }

    unmanagedEntry();

    return 0;
}

