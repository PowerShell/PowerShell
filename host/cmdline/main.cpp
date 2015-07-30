#include <string>
#include <iostream>
#include "common/coreclrutil.h"
#include "common/hostutil.h"
#include <limits.h>
#include <dlfcn.h>
#include <unicode/utypes.h>
#include <unicode/ucnv.h>
#include <unicode/ustring.h>
#include <unicode/uchar.h>

namespace Cmdline
{
    
void printHelp()
{
    std::cerr << "PS CoreCLR host" << std::endl;
    std::cerr << "Usage: host_cmdline [-c coreclr_path] [-alc load_context_assembly] [-s search_paths]" << std::endl;
    std::cerr << "                    [-b base_path] assembly_name type_name function_name [...]" << std::endl;
    std::cerr << std::endl;
    std::cerr << "What it does:" << std::endl;
    std::cerr << "- by default the host assumes that CoreCLR is located in the same folder" << std::endl;
    std::cerr << "  as host_cmdline" << std::endl;
    std::cerr << "  + this behavior can be overridden with the -c command line argument" << std::endl;
    std::cerr << "- by default the host assumes that the assembly named" << std::endl;
    std::cerr << "  Microsoft.PowerShell.CoreCLR.AssemblyLoadContext is part of the" << std::endl;
    std::cerr << "  platform assemblies" << std::endl;
    std::cerr << "  + a custom assembly containing the PowerShellAssemblyLoadContext can" << std::endl;
    std::cerr << "    be provided with the -alc command line argument" << std::endl;
    std::cerr << "- all additional parameters at the end of the command line are forwarded" << std::endl;
    std::cerr << "  to the specified entry function in the assembly" << std::endl;
    std::cerr << "- the host will execute the specified entry function in the specified assembly" << std::endl;
    std::cerr << "  + this assembly has to be located in the search path" << std::endl;
    std::cerr << "- by default the host will add the current working directory to the assembly search path" << std::endl;
    std::cerr << "  + this can be overridden with the -s command line argument" << std::endl;
    std::cerr << "  + if -c is specified, it will be added to the search path instead of the current directory" << std::endl;
    std::cerr << "- by default the host assumes the PS base path for the assembly load context is the current" << std::endl;
    std::cerr << "  working directory" << std::endl;
    std::cerr << "  + this can be overridden with the -b command line argument" << std::endl;
    std::cerr << "- the function signature of the function that gets executed must be:" << std::endl;
    std::cerr << "  public static int UnmanagedMain(int argc, [MarshalAs(UnmanagedType.LPArray,ArraySubType=UnmanagedType.LPStr,SizeParamIndex=0)] String[] argv)" << std::endl;
    std::cerr << std::endl;
    std::cerr << "Options:" << std::endl;
    std::cerr << "-c, --clr-path    path to libcoreclr.so and the managed CLR assemblies" << std::endl;
    std::cerr << "-alc              path to a dll containing Microsoft.PowerShell.CoreCLR.AssemblyLoadContext" << std::endl;
    std::cerr << "-s                a list of assembly search paths, separated by :" << std::endl;
    std::cerr << "-b                the powershell assembly base path" << std::endl;
    std::cerr << "-v                verbose output, show paths" << std::endl;
    std::cerr << "-tpa              additional list of trusted platform assemblies, this references dll and exe files" << std::endl;
    std::cerr << "                  separated by :" << std::endl;
    std::cerr << "                  unless part of the same folder as CoreCLR, the main assembly referenced with the assembly_name" << std::endl;
    std::cerr << "                  argument, must always be added to the TPA list with this parameter" << std::endl;
    std::cerr << "assembly_name     the assembly name of the assembly to execute" << std::endl;
    std::cerr << "                  must be available in the search path" << std::endl;
    std::cerr << "type_name         the type name where the function can be found" << std::endl;
    std::cerr << "function_name     the function to execute (must have the function signature described above!)" << std::endl;
    std::cerr << std::endl;
    std::cerr << "Example:" << std::endl;
    std::cerr << "./host_cmdline -c /test/coreclr -alc /test/ps/Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll -s /test/ps -b /test/ps -tpa /test/ps/powershell-simple.exe 'powershell-simple, version=1.0.0.0, culture=neutral, PublicKeyToken=null' 'ps_hello_world.Program' 'UnmanagedMain' 'get-process'" << std::endl;
}

struct Args
{
    Args() :
        argc(0),
        argv(nullptr),
        verbose(false)
    {
    }

    std::string clrPath;
    std::string assemblyLoadContextFilePath;
    std::string searchPaths;
    std::string basePath;
    std::string tpaList;
    std::string entryAssemblyName;
    std::string entryTypeName;
    std::string entryFunctionName;
    int argc;
    char** argv;
    bool verbose;

    void debugPrint() const
    {
        std::cerr << "Args:" << std::endl;
        std::cerr << "- clrPath                       " << clrPath << std::endl;
        std::cerr << "- assemblyLoadContextFilePath   " << assemblyLoadContextFilePath << std::endl;
        std::cerr << "- searchPaths                   " << searchPaths << std::endl;
        std::cerr << "- basePath                      " << basePath << std::endl;
        std::cerr << "- tpaList                       " << tpaList << std::endl;
        std::cerr << "- entryAssemblyName             " << entryAssemblyName << std::endl;
        std::cerr << "- entryTypeName                 " << entryTypeName << std::endl;
        std::cerr << "- entryFunctionName             " << entryFunctionName << std::endl;
        std::cerr << "- argc                          " << argc << std::endl;
        std::cerr << "- verbose                       " << (verbose ? "true" : "false") << std::endl;
    }
};

// this is implemented without any 3rd party lib to keep the list
// of dependencies low
bool parseCmdline(const int argc, char** argv, Args& args)
{
    if (argc <= 1)
    {
        std::cerr << "error: missing arguments" << std::endl;
        return false;
    }

    for (int i = 1; i < argc; ++i)
    {
        const std::string arg = argv[i];
        const bool hasNextArg = i+1 < argc;
        const std::string nextArg = hasNextArg ? std::string(argv[i+1]) : std::string("");

        if (hasNextArg && (arg == "-c" || arg == "--clr-path"))
        {
            args.clrPath = nextArg;
            ++i;
        }
        else if (hasNextArg && arg == "-alc")
        {
            args.assemblyLoadContextFilePath = nextArg;
            ++i;
        }
        else if (hasNextArg && arg == "-s")
        {
            args.searchPaths = nextArg;
            ++i;
        }
        else if (hasNextArg && arg == "-b")
        {
            args.basePath = nextArg;
            ++i;
        }
        else if (hasNextArg && arg == "-tpa")
        {
            args.tpaList = nextArg;
            ++i;
        }
        else if (arg == "-v")
        {
            args.verbose = true;
        }
        else if (args.entryAssemblyName == "")
        {
            args.entryAssemblyName = arg;
        }
        else if (args.entryTypeName == "")
        {
            args.entryTypeName = arg;
        }
        else if (args.entryFunctionName == "")
        {
            args.entryFunctionName = arg;
        }
        else
        {
            // forward command line parameters
            args.argc = argc-i;
            args.argv = &argv[i];

            // explicitly break here because the lines above consume all remaining arguments
            break;
        }
    }

    // check for mandatory parameters
    if (args.entryAssemblyName == "")
    {
        std::cerr << "error: assembly_name argument missing" << std::endl;
    }
    if (args.entryTypeName == "")
    {
        std::cerr << "error: type_name argument missing" << std::endl;
    }
    if (args.entryFunctionName == "")
    {
        std::cerr << "error: function_name argument missing" << std::endl;
    }

    return true;
}

}

int main(int argc, char** argv)
{
    // parse the command line arguments
    Cmdline::Args args;
    if (!Cmdline::parseCmdline(argc,argv,args))
    {
        Cmdline::printHelp();
        return 1;
    }
    if (args.verbose)
        args.debugPrint();

    // get the absolute path of the current executable
    std::string currentExeAbsolutePath;
    if (!CoreCLRUtil::GetAbsolutePath(argv[0],currentExeAbsolutePath))
    {
        std::cerr << "could not get absolute path of current executable" << std::endl;
        return 1;
    }
    if (args.verbose)
        std::cerr << "currentExeAbsolutePath=" << currentExeAbsolutePath << std::endl;
 
    // CLR absolute folder path
    //
    // This path is created from the location of this executable or a path
    // specified with the -c command line argument
    std::string clrAbsolutePath;
    const char* clrPathArg = args.clrPath == "" ? nullptr : args.clrPath.c_str();
    if (!CoreCLRUtil::GetClrFilesAbsolutePath(currentExeAbsolutePath.c_str(),clrPathArg,clrAbsolutePath))
    {
        std::cerr << "could not find absolute CLR path" << std::endl;
        return 1;
    }
    if (args.verbose)
        std::cerr << "clrAbsolutePath=" << clrAbsolutePath << std::endl;

    // the path to the CoreCLR library
    //
    // This is typically libcoreclr.so on Linux and libcoreclr.dylib on Mac

    std::string coreClrDllPath = clrAbsolutePath + "/" + CoreCLRUtil::coreClrDll;
    if (coreClrDllPath.size() >= PATH_MAX)
    {
        std::cerr << "Absolute path to CoreCLR library too long" << std::endl;
        return 1;
    }
    if (args.verbose)
        std::cerr << "coreClrDllPath: " << coreClrDllPath << std::endl;

    // TPA list
    //
    // The list of platform assemblies must include all CoreCLR assemblies
    // and the Microsoft.PowerShell.CoreCLR.AssemblyLoadContext
    //
    // if the -alc parameter was specified, add it to the TPA list here
    
    std::string tpaList;
    CoreCLRUtil::AddFilesFromDirectoryToTpaList(clrAbsolutePath.c_str(),tpaList);
    
    if (args.assemblyLoadContextFilePath != "")
    {
        std::string assemblyLoadContextAbsoluteFilePath;
        if (!CoreCLRUtil::GetAbsolutePath(args.assemblyLoadContextFilePath.c_str(),assemblyLoadContextAbsoluteFilePath))
        {
            std::cerr << "Failed to get absolute file path for assembly load context" << std::endl;
            return 1;
        }
        tpaList += ":" + assemblyLoadContextAbsoluteFilePath;
    }

    // add the -tpa command line argument
    if (args.tpaList != "")
    {
        std::string tpaAbsolutePathList = HostUtil::getAbsolutePathList(args.tpaList);
        if (tpaAbsolutePathList != "")
            tpaList += ":" + tpaAbsolutePathList;
    }

    if (args.verbose)
        std::cerr << "tpaList: " << tpaList << std::endl;

    // get the absolute path of the current directory

    std::string currentDirAbsolutePath;
    if (!CoreCLRUtil::GetAbsolutePath(".",currentDirAbsolutePath))
    {
        std::cerr << "failed to get the absolute path from current working directory" << std::endl;
        return 1;
    }

    // assembly search paths
    //
    // add the current directory, and optionally the CoreCLR directory if -c was specified
    // and anything specified with the -s option

    std::string appPath = currentDirAbsolutePath;
    if (args.clrPath != "")
        appPath += ":" + clrAbsolutePath;
    if (args.searchPaths != "")
    {
        std::string searchAbsolutePathList = HostUtil::getAbsolutePathList(args.searchPaths);
        if (searchAbsolutePathList != "")
            appPath += ":" + searchAbsolutePathList;
    }

    if (args.verbose)
        std::cerr << "appPath: " << appPath << std::endl;

    // search paths for native dlls
    //
    // Add both the CoreCLR directory and the regular search paths to this list
    std::string nativeDllSearchDirs = appPath + ":" + clrAbsolutePath;

    // convert the app base to utf-16
    //
    // this is needed as a utf-16 LE string by CoreCLR/PS's assembly load context interface
    // it is either:
    // - the current dir's absolute path
    // - the path specified through the -b argument
    std::string psBasePath = currentDirAbsolutePath;
    if (args.basePath != "")
    {
        if (!CoreCLRUtil::GetAbsolutePath(args.basePath.c_str(),psBasePath))
        {
            std::cerr << "failed to get the absolute path from the base_path argument" << std::endl;
            return 1;
        }
    }
    if (args.verbose)
        std::cerr << "psBasePath=" << psBasePath << std::endl;

    // make sure to leave 1 byte at the end for null termination
    std::basic_string<char16_t> psBasePath16(PATH_MAX+1,0);

    UnicodeString u8str = UnicodeString(psBasePath.c_str(),"UTF-8");
    int32_t targetSize = u8str.extract(0,u8str.length(),(char*)&psBasePath16[0],(psBasePath16.size()-1)*sizeof(char16_t),"UTF-16LE");
    psBasePath16.resize(targetSize/sizeof(char16_t)+1);

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
    loaderDelegate(psBasePath16.c_str());

    // call the unmanaged entry point for PowerShell
    typedef int (*UnmanagedMain)(int argc, char const* const* argv);
    UnmanagedMain unmanagedMain = nullptr;
    status = createDelegate(
                        hostHandle,
                        domainId,
                        args.entryAssemblyName.c_str(),
                        args.entryTypeName.c_str(),
                        args.entryFunctionName.c_str(),
                        (void**)&unmanagedMain);
    if (0 > status)
    {
        std::cerr << "could not create delegate for UnmanagedMain - status: " << std::hex << status << std::endl;
        return 4;
    }

    int exitCode = unmanagedMain(args.argc,args.argv);

    // shutdown CoreCLR
    status = shutdownCoreCLR(hostHandle,domainId);
    if (0 > status)
    {
        std::cerr << "coreclr_shutdown failed - status: " << std::hex << status << std::endl;
    }

    // close the dynamic library
    if (0 != dlclose(coreclrLib))
    {
        std::cerr << "failed to close CoreCLR library" << std::endl;
    }

    return exitCode;
}

