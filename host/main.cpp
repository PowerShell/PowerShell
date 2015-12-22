#include <stdlib.h>
#include <unistd.h>
#include <string>
#include <iostream>
#include "coreclrutil.h"

namespace Cmdline
{
    void printHelp()
    {
        std::cerr << "PowerShell on Linux native host" << std::endl
                  << "Usage: powershell [-a assembly] [...]" << std::endl
                  << std::endl
                  << "What it does:" << std::endl
                  << "- the host assumes that PSL has been published to $CORE_ROOT," << std::endl
                  << "- the host will launch $CORE_ROOT/Microsoft.PowerShell.Linux.Host.dll" << std::endl
                  << "  if not given an explicit assembly via -a (or --assembly)" << std::endl
                  << "- all additional parameters at the end of the command line are forwarded" << std::endl
                  << "  to the Main function in the assembly" << std::endl
                  << "- the host will execute the Main function in the specified assembly" << std::endl
                  << std::endl
                  << "Example:" << std::endl
                  << "CORE_ROOT=$(pwd)/bin ./powershell get-process" << std::endl;
    }

    struct Args
    {
        Args() :
            argc(0),
            argv(nullptr),
            assembly()
        {
        }

        int argc;
        char** argv;
        std::string assembly;
    };

    // simple CLI parsing so we can run other hosts
    void parseCmdline(int argc, char** argv, Args& args)
    {
        // index of arguments to forward (skip zeroth)
        int i = 1;
        // if we have any arguments
        if (argc > 1)
        {
            std::string arg(argv[i]);

            // handle help if first argument; note that this can't be --help
            // because the managed hosts use it
            if (arg == "--native-help")
            {
                printHelp();
                exit(0);
            }

            // check if given an explicit assembly to launch
            if (argc > 2 && (arg == "-a" || arg == "--assembly"))
            {
                args.assembly.assign(std::string(argv[i+1]));
                i += 2; // don't forward the first two arguments
            }
        }
        // forward arguments
        args.argc = argc - i;
        args.argv = &argv[i];
    }
}

int main(int argc, char** argv)
{
    Cmdline::Args args;
    Cmdline::parseCmdline(argc, argv, args);

    void* hostHandle;
    unsigned int domainId;
    int status = startCoreCLR(
        "psl_cmdline_host",
        &hostHandle,
        &domainId);

    if (!SUCCEEDED(status))
    {
        std::cerr << "could not start CoreCLR" << std::endl;
        return -1;
    }

    // default to powershell.exe if no host specified
    if (args.assembly.empty())
    {
        args.assembly.append(coreRoot);
        args.assembly.append("/");
        args.assembly.append("Microsoft.PowerShell.Linux.Host.dll");
    }

    // call into Main of assembly
    unsigned int exitCode;
    executeAssembly(
        hostHandle, domainId, args.argc,
        (const char**)args.argv,
        args.assembly.c_str(),
        &exitCode);

    status = stopCoreCLR(hostHandle, domainId);
    if (!SUCCEEDED(status))
    {
        std::cerr << "could not stop CoreCLR" << std::endl;
        return -1;
    }

    return exitCode;
}
