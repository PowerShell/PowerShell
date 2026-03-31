// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

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
    /// Defines an entry point from unmanaged code to PowerShell.
    /// </summary>
    public sealed class UnmanagedPSEntry
    {
        /// <summary>
        /// Starts PowerShell.
        /// </summary>
        /// <param name="consoleFilePath">
        /// Deprecated: Console file used to create a runspace configuration to start PowerShell
        /// </param>
        /// <param name="args">
        /// Command line arguments to the PowerShell
        /// </param>
        /// <param name="argc">
        /// Length of the passed in argument array.
        /// </param>
        [Obsolete("Callers should now use UnmanagedPSEntry.Start(string[], int)", error: true)]
        public static int Start(string consoleFilePath, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)] string[] args, int argc)
        {
            return Start(args, argc);
        }

        /// <summary>
        /// Starts PowerShell.
        /// </summary>
        /// <param name="args">
        /// Command line arguments to PowerShell
        /// </param>
        /// <param name="argc">
        /// Length of the passed in argument array.
        /// </param>
        public static int Start([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] args, int argc)
        {
            ArgumentNullException.ThrowIfNull(args);

#if !UNIX
            // On Windows with consoleAllocationPolicy=detached in the manifest,
            // no console is auto-allocated by the OS. We must allocate one ourselves
            // before anything touches CONOUT$/CONIN$ handles.
            // On older Windows the manifest element is ignored and the OS auto-allocates
            // a console, so GetConsoleWindow() != 0 and EarlyConsoleInit is a no-op.
            EarlyConsoleInit(args);
#endif

#if DEBUG
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]) && args[0]!.Equals("-isswait", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Attach the debugger to continue...");
                while (!System.Diagnostics.Debugger.IsAttached)
                {
                    Thread.Sleep(100);
                }

                System.Diagnostics.Debugger.Break();
            }
#endif
            // Warm up some components concurrently on background threads.
            EarlyStartup.Init();

            // Windows Vista and later support non-traditional UI fallback ie., a
            // user on an Arabic machine can choose either French or English(US) as
            // UI fallback language.
            // CLR does not support this (non-traditional) fallback mechanism.
            // The currentUICulture returned NativeCultureResolver supports this non
            // traditional fallback on Vista. So it is important to set currentUICulture
            // in the beginning before we do anything.
            Thread.CurrentThread.CurrentUICulture = NativeCultureResolver.UICulture;
            Thread.CurrentThread.CurrentCulture = NativeCultureResolver.Culture;

            ConsoleHost.ParseCommandLine(args);

            // NOTE: On Unix, logging depends on a command line parsing
            // and must be just after ConsoleHost.ParseCommandLine(args)
            // to allow overriding logging options.
            PSEtwLog.LogConsoleStartup();

            int exitCode = 0;
            try
            {
                var banner = string.Format(
                    CultureInfo.InvariantCulture,
                    ManagedEntranceStrings.ShellBannerNonWindowsPowerShell,
                    PSVersionInfo.GitCommitId);

                ConsoleHost.DefaultInitialSessionState = InitialSessionState.CreateDefault2();

                exitCode = ConsoleHost.Start(
                    bannerText: banner,
                    helpText: ManagedEntranceStrings.UsageHelp,
                    issProvidedExternally: false);
            }
            catch (HostException e)
            {
                if (e.InnerException is Win32Exception win32e)
                {
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

#if !UNIX
        /// <summary>
        /// Allocates a console early in startup to support consoleAllocationPolicy=detached.
        /// On newer Windows (with the detached policy active), the OS does not auto-allocate
        /// a console for CUI apps. On older Windows the manifest is ignored, so the OS
        /// auto-allocates a console and GetConsoleWindow() returns non-zero (early return).
        /// </summary>
        private static void EarlyConsoleInit(string[] args)
        {
            nint existingConsole = Interop.Windows.GetConsoleWindow();
            if (existingConsole != nint.Zero)
            {
                // Console already exists (inherited from parent or auto-allocated on older Windows).
                // Leave -WindowStyle handling to the existing SetConsoleMode code path.
                return;
            }

            // No console exists (GetConsoleWindow() == 0). This means either:
            //   (a) The detached manifest policy is active (newer Windows), or
            //   (b) DETACHED_PROCESS — no console at all, or
            //   (c) CREATE_NO_WINDOW — console session exists but no window.
            //
            // For (c), AllocConsoleWithOptions returns ExistingConsole (no-op).
            // For (a) and (b), behavior depends on the mode:
            //   Default mode: allocates if the parent would have given us a console
            //     on prior Windows versions, returns NoConsole for DETACHED_PROCESS.
            //   NoWindow mode: always creates a console session (overrides DETACHED).
            //
            // When -WindowStyle Hidden is specified, we intentionally use NoWindow
            // even though it overrides DETACHED_PROCESS — the user explicitly asked
            // for invisible PowerShell with working I/O, and the alternative (no
            // console, crashing on stdin/stdout access) is strictly worse.
            //
            // If the API is not available (older Windows), TryAlloc* returns false.
            // On older Windows the manifest is ignored and the OS auto-allocates
            // a console, so GetConsoleWindow() would have returned non-zero above.
            // The only older-Windows path here is DETACHED_PROCESS, where the
            // existing behavior is no console I/O; we preserve that by not
            // falling back to plain AllocConsole() (per DHowett's guidance).
            if (EarlyCheckForHiddenWindowStyle(args))
            {
                Interop.Windows.TryAllocConsoleNoWindow();
            }
            else
            {
                Interop.Windows.TryAllocConsoleDefault();
            }
        }

        /// <summary>
        /// Minimal early scan for -WindowStyle Hidden in command line args.
        /// Matches any unambiguous prefix of "windowstyle" starting from "w"
        /// (e.g. -w, -wi, -win, ..., -windowstyle, --windowstyle) followed by "hidden".
        /// This is a best-effort check that runs before the full parser. False positives
        /// (e.g. a hypothetical future -w parameter) are acceptable because the worst case
        /// is allocating a hidden console that the full parser would later show.
        /// </summary>
        private static bool EarlyCheckForHiddenWindowStyle(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                string arg = args[i];
                if (arg.Length >= 2 && (arg[0] == '-' || arg[0] == '/'))
                {
                    int start = 1;

                    // Strip second dash for --windowstyle (matches full parser behavior).
                    if (arg.Length >= 3 && arg[0] == '-' && arg[1] == '-')
                    {
                        start = 2;
                    }

                    ReadOnlySpan<char> key = arg.AsSpan(start);
                    if (key.Length >= 1
                        && key.Length <= "windowstyle".Length
                        && "windowstyle".AsSpan().StartsWith(key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (args[i + 1].Equals("hidden", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

#endif
    }
}
