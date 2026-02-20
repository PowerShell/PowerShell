// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;

namespace TestExe
{
    [Flags]
    internal enum EnvTarget
    {
        User = 1,
        System = 2,
    }

    internal class TestExe
    {
        private static int Main(string[] args)
        {
            int exitCode = 0;
            if (args.Length > 0)
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "-echoargs":
                        EchoArgs(args);
                        break;
                    case "-echocmdline":
                        EchoCmdLine();
                        break;
                    case "-createchildprocess":
                        CreateChildProcess(args);
                        break;
                    case "-returncode":
                        // Used to test functionality depending on $LASTEXITCODE, like &&/|| operators
                        Console.WriteLine(args[1]);
                        return int.Parse(args[1]);
                    case "-stderr":
                        Console.Error.WriteLine(args[1]);
                        break;
                    case "-stderrandout":
                        Console.WriteLine(args[1]);
                        Console.Error.WriteLine(new string(args[1].ToCharArray().Reverse().ToArray()));
                        break;
                    case "-readbytes":
                        ReadBytes();
                        break;
                    case "-writebytes":
                        WriteBytes(args.AsSpan()[1..]);
                        break;
                    case "-updateuserpath":
                        UpdateEnvPath(EnvTarget.User);
                        break;
                    case "-updatesystempath":
                        UpdateEnvPath(EnvTarget.System);
                        break;
                    case "-updateuserandsystempath":
                        UpdateEnvPath(EnvTarget.User | EnvTarget.System);
                        break;
                    case "--help":
                    case "-h":
                        PrintHelp();
                        break;
                    default:
                        exitCode = 1;
                        Console.Error.WriteLine("Unknown test {0}. Run with '-h' for help.", args[0]);
                        break;
                }
            }
            else
            {
                exitCode = 1;
                Console.Error.WriteLine("Test not specified");
            }

            return exitCode;
        }

        private static void WriteBytes(ReadOnlySpan<string> args)
        {
            using Stream stdout = Console.OpenStandardOutput();
            foreach (string arg in args)
            {
                if (!byte.TryParse(arg, NumberStyles.AllowHexSpecifier, provider: null, out byte value))
                {
                    throw new ArgumentException(
                        nameof(args),
                        "All args after -writebytes must be single byte hex strings.");
                }

                stdout.WriteByte(value);
            }
        }

        [SkipLocalsInit]
        private static void ReadBytes()
        {
            using Stream stdin = Console.OpenStandardInput();
            Span<byte> buffer = stackalloc byte[0x200];
            Unsafe.InitBlock(ref MemoryMarshal.GetReference(buffer), 0, 0x200);
            Span<char> hex = stackalloc char[] { '\0', '\0' };
            while (true)
            {
                int received = stdin.Read(buffer);
                if (received is 0)
                {
                    return;
                }

                for (int i = 0; i < received; i++)
                {
                    buffer[i].TryFormat(hex, out _, "X2");
                    Console.Out.WriteLine(hex);
                }
            }
        }

        // <Summary>
        // Echos back to stdout the arguments passed in
        // </Summary>
        private static void EchoArgs(string[] args)
        {
            for (int i = 1; i < args.Length; i++)
            {
                Console.WriteLine("Arg {0} is <{1}>", i - 1, args[i]);
            }
        }

        // <Summary>
        // Echos the raw command line received by the process plus the arguments passed in.
        // </Summary>
        private static void EchoCmdLine()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ReadOnlySpan<char> rawCmdLine = Interop.GetCommandLine();
                Console.WriteLine(rawCmdLine);
            }
            else
            {
                Console.WriteLine("N/A");
            }
        }

        // <Summary>
        // Print help content.
        // </Summary>
        private static void PrintHelp()
        {
            const string Content = @"
Options for echoing args are:
   -echoargs     Echos back to stdout the arguments passed in.
   -echocmdline  Echos the raw command line received by the process.

Other options are for specific tests only. Read source code for details.
";
            Console.WriteLine(Content);
        }

        // <Summary>
        // First argument is the number of child processes to create which are instances of itself
        // Processes automatically exit after 100 seconds
        // </Summary>
        private static void CreateChildProcess(string[] args)
        {
            if (args.Length > 1)
            {
                uint num = uint.Parse(args[1]);
                for (uint i = 0; i < num; i++)
                {
                    Process child = new Process();
                    child.StartInfo.FileName = Environment.ProcessPath;
                    child.StartInfo.Arguments = "-createchildprocess";
                    child.Start();
                }
            }
            // sleep is needed so the process doesn't exit before the test case kill it
            Thread.Sleep(100000);
        }

        private static void UpdateEnvPath(EnvTarget target)
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            const string EnvVarName = "Path";
            const string UserEnvRegPath = "Environment";
            const string SysEnvRegPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";

            if (target.HasFlag(EnvTarget.User))
            {
                // Append to the User Path.
                using RegistryKey? reg = Registry.CurrentUser.OpenSubKey(UserEnvRegPath, writable: true);
                if (reg is not null)
                {
                    UpdateEnvPathImpl(reg, append: true, @"X:\not-exist-user-path");
                }
            }

            if (target.HasFlag(EnvTarget.System))
            {
                // Prepend to the System Path.
                using RegistryKey? reg = Registry.LocalMachine.OpenSubKey(SysEnvRegPath, writable: true);
                if (reg is not null)
                {
                    UpdateEnvPathImpl(reg, append: false, @"X:\not-exist-sys-path");
                }
            }

            static void UpdateEnvPathImpl(RegistryKey regKey, bool append, string newPathItem)
            {
                // Get the registry value kind.
                RegistryValueKind kind = regKey.GetValueKind(EnvVarName);

                // Get the literal registry value (not expanded) for the env var.
                string oldValue = (string)regKey.GetValue(
                    EnvVarName,
                    defaultValue: string.Empty,
                    RegistryValueOptions.DoNotExpandEnvironmentNames);

                string newValue;
                if (append)
                {
                    // Append to the old value.
                    string separator = (oldValue is "" || oldValue.EndsWith(';')) ? string.Empty : ";";
                    newValue = $"{oldValue}{separator}{newPathItem}";
                }
                else
                {
                    // Prepend to the old value.
                    string separator = (oldValue is "" || oldValue.StartsWith(';')) ? string.Empty : ";";
                    newValue = $"{newPathItem}{separator}{oldValue}";
                }

                // Set the new value and preserve the original value kind.
                regKey.SetValue(EnvVarName, newValue, kind);
            }
        }
    }
}

internal static partial class Interop
{
    [LibraryImport("Kernel32.dll")]
    private static unsafe partial char* GetCommandLineW();

    internal static ReadOnlySpan<char> GetCommandLine()
    {
        unsafe
        {
            char* cmdLinePtr = GetCommandLineW();
            return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(cmdLinePtr);
        }
    }
}
