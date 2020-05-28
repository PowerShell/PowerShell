// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Defines an entry point from unmanaged code to managed Msh.
    /// </summary>
    public sealed class UnmanagedPSEntry
    {
        /// <summary>
        /// Starts managed MSH.
        /// </summary>
        /// <param name="consoleFilePath">
        /// Deprecated: Console file used to create a runspace configuration to start MSH
        /// </param>
        /// <param name="args">
        /// Command line arguments to the managed MSH
        /// </param>
        /// <param name="argc">
        /// Length of the passed in argument array.
        /// </param>
        public static int Start(string consoleFilePath, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)]string[] args, int argc)
        {
            // Warm up some components concurrently on background threads.
            EarlyStartup.Init();

            // We need to read the settings file before we create the console host
            CommandLineParameterParser.EarlyParse(args);

#if !UNIX
            // NOTE: On Unix, logging has to be deferred until after command-line parsing
            // complete. On Windows, deferring the call is not needed.
            PSEtwLog.LogConsoleStartup();
#endif

            // Windows Vista and later support non-traditional UI fallback ie., a
            // user on an Arabic machine can choose either French or English(US) as
            // UI fallback language.
            // CLR does not support this (non-traditional) fallback mechanism.
            // The currentUICulture returned NativeCultureResolver supports this non
            // traditional fallback on Vista. So it is important to set currentUICulture
            // in the beginning before we do anything.
            Thread.CurrentThread.CurrentUICulture = NativeCultureResolver.UICulture;
            Thread.CurrentThread.CurrentCulture = NativeCultureResolver.Culture;

#if DEBUG
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]) && args[0].Equals("-isswait", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Attach the debugger to continue...");
                while (!System.Diagnostics.Debugger.IsAttached)
                {
                    Thread.Sleep(100);
                }

                System.Diagnostics.Debugger.Break();
            }
#endif
            int exitCode = 0;
            try
            {
                var banner = string.Format(
                    CultureInfo.InvariantCulture,
                    ManagedEntranceStrings.ShellBannerNonWindowsPowerShell,
                    PSVersionInfo.GitCommitId);

                exitCode = ConsoleShell.Start(banner, ManagedEntranceStrings.UsageHelp, args);
            }
            catch (HostException e)
            {
                if (e.InnerException != null && e.InnerException.GetType() == typeof(Win32Exception))
                {
                    Win32Exception win32e = e.InnerException as Win32Exception;

                    // These exceptions are caused by killing conhost.exe
                    // 1236, network connection aborted by local system
                    // 0x6, invalid console handle
                    if (win32e.NativeErrorCode == 0x6 || win32e.NativeErrorCode == 1236)
                    {
                        return exitCode;
                    }
                }

                System.Environment.FailFast(e.Message, e);
            }
            catch (Exception e)
            {
                System.Environment.FailFast(e.Message, e);
            }

            return exitCode;
        }
    }
}

