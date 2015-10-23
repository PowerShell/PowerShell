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
                  << "Usage: host_cmdline [-s search_paths] assembly [...]" << std::endl
                  << std::endl
                  << "What it does:" << std::endl
                  << "- the host assumes that CoreCLR is located $CORE_ROOT" << std::endl
                  << "- by default the host assumes that the assembly named" << std::endl
                  << "  Microsoft.PowerShell.CoreCLR.AssemblyLoadContext is " << std::endl
                  << "  located in $PWRSH_ROOT" << std::endl
                  << "- all additional parameters at the end of the command line are forwarded" << std::endl
                  << "  to the Main function in the assembly" << std::endl
                  << "- the host will execute the Main function in the specified assembly" << std::endl
                  << "  + this assembly has to be located in the search path" << std::endl
                  << "- by default the host will add the current working directory to the assembly search path" << std::endl
                  << "  + this can be overridden with the -s command line argument" << std::endl
                  << "- the function signature of the Main function that gets executed must be:" << std::endl
                  << "  static void Main(string[] args)" << std::endl
                  << std::endl
                  << "Options:" << std::endl
                  << "-s                a list of assembly search paths, separated by :" << std::endl
                  << "-v                verbose output" << std::endl
                  << "assembly          the path of the assembly to execute relative to current directory" << std::endl
                  << std::endl
                  << "Example:" << std::endl
                  << "CORE_ROOT=/test/coreclr PWRSH_ROOT=/test/powershell ./host_cmdline -s /test/ps powershell-simple 'get-process'" << std::endl;
    }

    struct Args
    {
        Args() :
            argc(0),
            argv(nullptr),
            verbose(false)
        {
        }

        std::string searchPaths;
        std::string entryAssemblyPath;
        int argc;
        char** argv;
        bool verbose;

        void debugPrint() const
        {
            std::cerr << "Args:" << std::endl
                      << "- searchPaths                   " << searchPaths << std::endl
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

	    if (hasNextArg && arg == "-s")
            {
                args.searchPaths = nextArg;
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

    void* hostHandle;
    unsigned int domainId;
    int status = startCoreCLR(
        appPath.c_str(),
        nativeDllSearchDirs.c_str(),
        "ps_cmdline_host",
        &hostHandle,
        &domainId);

    // call into Main of assembly
    unsigned int exitCode;
    executeAssembly(
        hostHandle, domainId, args.argc,
        (const char**)args.argv,
        (currentDirAbsolutePath+"/"+args.entryAssemblyPath).c_str(),
        &exitCode);

    status = stopCoreCLR(hostHandle, domainId);

    return exitCode;
}

