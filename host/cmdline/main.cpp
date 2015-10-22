#include <string>
#include <iostream>
#include "common/coreclrutil.h"
#include "common/hostutil.h"
#include <limits.h>
#include <unicode/utypes.h>
#include <unicode/ucnv.h>
#include <unicode/ustring.h>
#include <unicode/uchar.h>

namespace Cmdline
{
    void printHelp()
    {
        std::cerr << "PS CoreCLR host" << std::endl
                  << "Usage: host_cmdline [-alc load_context_assembly] [-s search_paths]" << std::endl
                  << "                    [-b base_path] assembly [...]" << std::endl
                  << std::endl
                  << "What it does:" << std::endl
                  << "- by default the host assumes that CoreCLR is located in the same folder" << std::endl
                  << "  as host_cmdline" << std::endl
                  << "- by default the host assumes that the assembly named" << std::endl
                  << "  Microsoft.PowerShell.CoreCLR.AssemblyLoadContext is part of the" << std::endl
                  << "  platform assemblies" << std::endl
                  << "  + a custom assembly containing the PowerShellAssemblyLoadContext can" << std::endl
                  << "    be provided with the -alc command line argument" << std::endl
                  << "- all additional parameters at the end of the command line are forwarded" << std::endl
                  << "  to the Main function in the assembly" << std::endl
                  << "- the host will execute the Main function in the specified assembly" << std::endl
                  << "  + this assembly has to be located in the search path" << std::endl
                  << "- by default the host will add the current working directory to the assembly search path" << std::endl
                  << "  + this can be overridden with the -s command line argument" << std::endl
                  << "- by default the host assumes the PS base path for the assembly load context is the current" << std::endl
                  << "  working directory" << std::endl
                  << "  + this can be overridden with the -b command line argument" << std::endl
                  << "- the function signature of the Main function that gets executed must be:" << std::endl
                  << "  static void Main(string[] args)" << std::endl
                  << std::endl
                  << "Options:" << std::endl
                  << "-alc              path to a dll containing Microsoft.PowerShell.CoreCLR.AssemblyLoadContext" << std::endl
                  << "-s                a list of assembly search paths, separated by :" << std::endl
                  << "-b                the powershell assembly base path" << std::endl
                  << "-v                verbose output, show paths" << std::endl
                  << "-tpa              additional list of trusted platform assemblies, this references dll and exe files" << std::endl
                  << "                  separated by :" << std::endl
                  << "                  unless part of the same folder as CoreCLR, the main assembly referenced with the assembly_name" << std::endl
                  << "                  argument, must always be added to the TPA list with this parameter" << std::endl
                  << "assembly          the path of the assembly to execute relative to current directory" << std::endl
                  << std::endl
                  << "Example:" << std::endl
                  << "CORE_ROOT=/test/coreclr ./host_cmdline -alc /test/ps/Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll -s /test/ps -b /test/ps -tpa /test/ps/powershell-simple.exe 'powershell-simple, version=1.0.0.0, culture=neutral, PublicKeyToken=null' 'get-process'" << std::endl;
    }

    struct Args
    {
        Args() :
            argc(0),
            argv(nullptr),
            verbose(false)
        {
        }

        std::string assemblyLoadContextFilePath;
        std::string searchPaths;
        std::string basePath;
        std::string tpaList;
        std::string entryAssemblyPath;
        int argc;
        char** argv;
        bool verbose;

        void debugPrint() const
        {
            std::cerr << "Args:" << std::endl
                      << "- assemblyLoadContextFilePath   " << assemblyLoadContextFilePath << std::endl
                      << "- searchPaths                   " << searchPaths << std::endl
                      << "- basePath                      " << basePath << std::endl
                      << "- tpaList                       " << tpaList << std::endl
                      << "- entryAssemblyPath             " << entryAssemblyPath << std::endl
                      << "- argc                          " << argc << std::endl
                      << "- verbose                       " << (verbose ? "true" : "false") << std::endl;
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

            if (hasNextArg && arg == "-alc")
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
            else if (args.entryAssemblyPath == "")
            {
                args.entryAssemblyPath = arg;
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
        if (args.entryAssemblyPath == "")
        {
            std::cerr << "error: assembly_name argument missing" << std::endl;
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

    std::string clrAbsolutePath;
    if (!GetAbsolutePath(std::getenv("CORE_ROOT"), clrAbsolutePath))
    {
        std::cerr << "could not get absolute path of CoreCLR" << std::endl;
        return 1;
    }
    if (args.verbose)
        std::cerr << "clrAbsolutePath=" << clrAbsolutePath << std::endl;

    // TPA list
    //
    // The list of platform assemblies must include all CoreCLR assemblies
    // and the Microsoft.PowerShell.CoreCLR.AssemblyLoadContext
    //
    // if the -alc parameter was specified, add it to the TPA list here
    
    std::string tpaList;
    AddFilesFromDirectoryToTpaList(clrAbsolutePath.c_str(),tpaList);
    
    if (args.assemblyLoadContextFilePath != "")
    {
        std::string assemblyLoadContextAbsoluteFilePath;
        if (!GetAbsolutePath(args.assemblyLoadContextFilePath.c_str(),assemblyLoadContextAbsoluteFilePath))
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
    if (!GetAbsolutePath(".",currentDirAbsolutePath))
    {
        std::cerr << "failed to get the absolute path from current working directory" << std::endl;
        return 1;
    }

    // assembly search paths
    //
    // add the current directory, the CoreCLR directory, and anything
    // specified with the -s option

    std::string appPath = currentDirAbsolutePath;

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
        if (!GetAbsolutePath(args.basePath.c_str(),psBasePath))
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

    void* hostHandle;
    unsigned int domainId;
    int status = startCoreCLR(
        tpaList.c_str(),
        appPath.c_str(),
        nativeDllSearchDirs.c_str(),
        "ps_cmdline_host",
        &hostHandle,
        &domainId);

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

    // call into Main of powershell-simple.exe
    unsigned int exitCode;
    executeAssembly(hostHandle, domainId, args.argc,
                                 (const char**)args.argv,
                                 (currentDirAbsolutePath+"/"+args.entryAssemblyPath).c_str(),
                                 &exitCode);

    status = stopCoreCLR(hostHandle, domainId);

    return exitCode;
}

