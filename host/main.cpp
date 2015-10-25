#include <unistd.h>
#include <string>
#include <iostream>
#include "coreclrutil.h"

namespace Cmdline
{
    void printHelp()
    {
        std::cerr << "PowerShell on Linux host" << std::endl
                  << "Usage: powershell assembly [...]" << std::endl
                  << std::endl
                  << "What it does:" << std::endl
                  << "- the host assumes that CoreCLR is located $CORE_ROOT" << std::endl
                  << "- the host assumes that the assembly named" << std::endl
                  << "  Microsoft.PowerShell.CoreCLR.AssemblyLoadContext is " << std::endl
                  << "  located in $PWRSH_ROOT" << std::endl
                  << "- all additional parameters at the end of the command line are forwarded" << std::endl
                  << "  to the Main function in the assembly" << std::endl
                  << "- the host will execute the Main function in the specified assembly" << std::endl
                  << "  + this assembly has to be located in the search path" << std::endl
                  << "- the host will add $PWRSH_ROOT to the assembly search path" << std::endl
                  << "- the function signature of the Main function that gets executed must be:" << std::endl
                  << "  static void Main(string[] args)" << std::endl
                  << std::endl
                  << "Options:" << std::endl
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

	    if (hasNextArg && arg == "--File")
            {
                std::cerr << "TODO: should launch script " << nextArg << std::endl;
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

    // get the absolute path of the current directory
    std::string cwd(getcwd(nullptr, 0));

    void* hostHandle;
    unsigned int domainId;
    int status = startCoreCLR(
        "ps_cmdline_host",
        &hostHandle,
        &domainId);

    if (!SUCCEEDED(status))
    {
        std::cerr << "could not start CoreCLR" << std::endl;
        return -1;
    }

    // call into Main of assembly
    unsigned int exitCode;
    executeAssembly(
        hostHandle, domainId, args.argc,
        (const char**)args.argv,
        (cwd + "/" + args.entryAssemblyPath).c_str(),
        &exitCode);

    status = stopCoreCLR(hostHandle, domainId);
    if (!SUCCEEDED(status))
    {
        std::cerr << "could not stop CoreCLR" << std::endl;
        return -1;
    }

    return exitCode;
}
