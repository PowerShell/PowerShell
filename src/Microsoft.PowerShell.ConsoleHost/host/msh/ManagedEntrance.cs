// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Globalization;
using System.Threading;
using System.Runtime.InteropServices;

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
#pragma warning disable 1573
        public static int Start(string consoleFilePath, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)]string[] args, int argc)
#pragma warning restore 1573
        {
            // We need to read the settings file before we create the console host
            Microsoft.PowerShell.CommandLineParameterParser.EarlyParse(args);
            System.Management.Automation.Runspaces.EarlyStartup.Init();

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
                while (!System.Diagnostics.Debugger.IsAttached) {
                    Thread.Sleep(100);
                }

                System.Diagnostics.Debugger.Break();
            }
#endif
            ConsoleHost.DefaultInitialSessionState = InitialSessionState.CreateDefault2();

            int exitCode = 0;
            try
            {
                var banner = ManagedEntranceStrings.ShellBannerNonWindowsPowerShell;
                var formattedBanner = string.Format(CultureInfo.InvariantCulture, banner, PSVersionInfo.GitCommitId);
                exitCode = Microsoft.PowerShell.ConsoleShell.Start(
                    formattedBanner,
                    ManagedEntranceStrings.UsageHelp,
                    args);
            }
            catch (System.Management.Automation.Host.HostException e)
            {
                if (e.InnerException != null && e.InnerException.GetType() == typeof(System.ComponentModel.Win32Exception))
                {
                    System.ComponentModel.Win32Exception win32e = e.InnerException as System.ComponentModel.Win32Exception;

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

