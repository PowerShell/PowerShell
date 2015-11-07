#include <unistd.h>
#include <string>
#include <iostream>
#include "coreclrutil.h"

namespace Cmdline
{
    void printHelp()
    {
        std::cerr << "PowerShell on Linux native host" << std::endl
                  << "Usage: powershell [assembly] [...]" << std::endl
                  << std::endl
                  << "What it does:" << std::endl
                  << "- the host assumes that CoreCLR is located $CORE_ROOT," << std::endl
                  << "  else in lib/coreclr"
                  << "- the host assumes that the assembly named" << std::endl
                  << "  Microsoft.PowerShell.CoreCLR.AssemblyLoadContext is " << std::endl
                  << "  located in $PWRSH_ROOT, else in lib/powershell" << std::endl
                  << "- the host will launch $PWRSH_ROOT/powershell.exe" << std::endl
                  << "  if not given an explicit assembly.exe" << std::endl
                  << "- all additional parameters at the end of the command line are forwarded" << std::endl
                  << "  to the Main function in the assembly" << std::endl
                  << "- the host will execute the Main function in the specified assembly" << std::endl
                  << std::endl
                  << "Example:" << std::endl
                  << "CORE_ROOT=/test/coreclr PWRSH_ROOT=/test/powershell ./powershell get-process" << std::endl;
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

    // checks if string ends with ".exe"
    bool isExe(std::string arg)
    {
        std::size_t dot = arg.find_last_of(".");
        return dot == std::string::npos ? false : arg.substr(dot) == ".exe";
    }

    // simple extraction of assembly.exe so we can run other hosts
    void parseCmdline(int argc, char** argv, Args& args)
    {
        // index of arguments to forward (skip zeroth)
        int i = 1;
        // if we have any arguments
        if (argc > 1)
        {
            // check if the first is an assembly.exe
            const std::string arg = argv[i];
            if (isExe(arg))
            {
                args.assembly.assign(arg);
                ++i; // don't forward the first argument
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
        "ps_cmdline_host",
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
        args.assembly.assign(pwrshPath);
        args.assembly.append("/");
        args.assembly.append("powershell.exe");
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
