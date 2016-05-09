/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Threading;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Defines an entry point from unmanaged code to managed Msh
    /// </summary>
    public sealed class UnmanagedPSEntry
    {
        /// <summary>
        /// Starts managed MSH
        /// </summary>
        /// <param name="consoleFilePath">
        /// Console file used to create a runspace configuration to start MSH
        /// </param>
        /// <param name="args">
        /// Command line arguments to the managed MSH
        /// </param>
#if CORECLR
        #pragma warning disable 1573
        public static int Start(string consoleFilePath, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)]string[] args, int argc)
        #pragma warning restore 1573
#else
        public int Start(string consoleFilePath, string[] args)
#endif
        {
            System.Management.Automation.Runspaces.EarlyStartup.Init();

            // Set ETW activity Id
            Guid activityId = EtwActivity.GetActivityId();

            if (activityId == Guid.Empty)
            {
                EtwActivity.SetActivityId(EtwActivity.CreateActivityId());
            }

            PSEtwLog.LogOperationalInformation(PSEventId.Perftrack_ConsoleStartupStart, PSOpcode.WinStart,
                PSTask.PowershellConsoleStartup, PSKeyword.UseAlwaysOperational);

#if !CORECLR
            // Register crash reports in non-server mode
            WindowsErrorReporting.RegisterWindowsErrorReporting(false);
#endif

            try
            {
                // Windows Vista and later support non-traditional UI fallback ie., a 
                // user on an Arabic machine can choose either French or English(US) as
                // UI fallback language.
                // CLR does not support this (non-traditional) fallback mechanism. 
                // The currentUICulture returned NativeCultureResolver supports this non
                // traditional fallback on Vista. So it is important to set currentUICuluture
                // in the beginning before we do anything.
                ClrFacade.SetCurrentThreadUiCulture(NativeCultureResolver.UICulture);
                ClrFacade.SetCurrentThreadCulture(NativeCultureResolver.Culture);

                RunspaceConfigForSingleShell configuration = null;
                PSConsoleLoadException warning = null;
                //      PSSnapInException will cause the control to return back to the native code
                //      and stuff the EXCEPINFO field with the message of the exception.
                //      The native code will print this out and exit the process.
                if (string.IsNullOrEmpty(consoleFilePath))
                {
#if DEBUG
                    // Special switches for debug mode to allow self-hosting on InitialSessionState instead
                    // of runspace configuration...
                    if (args.Length > 0 && !String.IsNullOrEmpty(args[0]) && args[0].Equals("-iss", StringComparison.OrdinalIgnoreCase))
                    {
                        ConsoleHost.DefaultInitialSessionState = InitialSessionState.CreateDefault2();
                        configuration = null;
                    }
                    else if (args.Length > 0 && !String.IsNullOrEmpty(args[0]) && args[0].Equals("-isswait", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Attach the debugger and hit enter to continue:");
                        Console.ReadLine();
                        ConsoleHost.DefaultInitialSessionState = InitialSessionState.CreateDefault2();
                        configuration = null;
                    }
                    else 
                    {
                        ConsoleHost.DefaultInitialSessionState = InitialSessionState.CreateDefault2();
                        configuration = null;
                    }
#else
                    ConsoleHost.DefaultInitialSessionState = InitialSessionState.CreateDefault2();
                configuration = null;
#endif
                }
                else
                {
                    //TODO : Deprecate RunspaceConfiguration and use InitialSessionState
                    configuration =
                        RunspaceConfigForSingleShell.Create(consoleFilePath, out warning);
                }
                int exitCode = 0;
                try
                {
                    exitCode = Microsoft.PowerShell.ConsoleShell.Start(
                        configuration,
                        ManagedEntranceStrings.ShellBanner,
                        ManagedEntranceStrings.ShellHelp,
                        warning == null ? null : warning.Message,
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
#if CORECLR
                    System.Environment.FailFast(e.Message);
#else
                    WindowsErrorReporting.FailFast(e);
#endif
                }
                catch (Exception e)
                {
#if CORECLR
                    System.Management.Automation.Environment.FailFast(e.Message);
#else
                    // exceptions caught here should cause Watson.
                    // Must call FailFast; otherwise the exception will be returned to the native code
                    // which will just print out the exception message without Watson
                    WindowsErrorReporting.FailFast(e);
#endif
                }
                return exitCode;
            }
            finally
            {
#if !CORECLR
                WindowsErrorReporting.WaitForPendingReports();
#endif
            }
        }
    }
}

