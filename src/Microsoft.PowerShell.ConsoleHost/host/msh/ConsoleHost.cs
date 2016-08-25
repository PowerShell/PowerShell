/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
#pragma warning disable 1634, 1691


using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Remoting;
using System.Management.Automation.Security;
using System.Threading;
using System.Runtime.InteropServices;
using System.Management.Automation.Language;
#if CORECLR
using Microsoft.PowerShell.CoreClr.Stubs;
// Some APIs are missing from System.Environment. We use System.Management.Automation.Environment as a proxy type:
//  - for missing APIs, System.Management.Automation.Environment has extension implementation.
//  - for existing APIs, System.Management.Automation.Environment redirect the call to System.Environment.
using Environment = System.Management.Automation.Environment;
#endif

using Dbg = System.Management.Automation.Diagnostics;
using ConsoleHandle = Microsoft.Win32.SafeHandles.SafeFileHandle;
using NakedWin32Handle = System.IntPtr;
using System.Management.Automation.Tracing;
using Microsoft.PowerShell.Telemetry.Internal;
using Debugger = System.Management.Automation.Debugger;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// 
    /// Subclasses S.M.A.Host to implement a console-mode monad host.
    /// 
    /// </summary>
    ///
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    internal sealed partial class ConsoleHost
        :
        PSHost,
        IDisposable,
        IHostSupportsInteractiveSession,
        IHostProvidesTelemetryData
    {
        #region static methods

        internal const uint ExitCodeSuccess = 0x00000000;
        internal const uint ExitCodeCtrlBreak = 0xFFFE0000;
        internal const uint ExitCodeInitFailure = 0xFFFF0000;
        internal const uint ExitCodeBadCommandLineParameter = 0xFFFD0000;

#if CORECLR
        // AccessViolationException/StackOverflowException Not In CoreCLR.
        // The CoreCLR team told us to not check for these exceptions because they
        // usually won't be caught.
        internal static void CheckForSevereException(Exception e) { }
#else
        // Keep in sync:
        // S.M.A.CommandProcessorBase.CheckForSevereException
        // S.M.A.Internal.ConsoleHost.CheckForSevereException
        // S.M.A.Commands.CommandsCommon.CheckForSevereException
        // S.M.A.Commands.UtilityCommon.CheckForSevereException
        /// <summary>
        /// Checks whether the exception is a severe exception which should
        /// cause immediate process failure.
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>
        /// CB says 02/23/2005: I personally would err on the side
        /// of treating OOM like an application exception, rather than
        /// a critical system failure.I think this will be easier to justify
        /// in Orcas, if we tease apart the two cases of OOM better.
        /// But even in Whidbey, how likely is it that we couldn't JIT
        /// some backout code?  At that point, the process or possibly
        /// the machine is likely to stop executing soon no matter
        /// what you do in this routine.  So I would just consider
        /// AccessViolationException.  (I understand why you have SO here,
        /// at least temporarily).
        /// 
        /// JN/GX 04/15/2005: There is currently no way to log host events,
        /// so these FailFasts cannot be logged.
        /// </remarks>
        internal static void CheckForSevereException(Exception e)
        {
            if (e is AccessViolationException || e is StackOverflowException)
            {
                WindowsErrorReporting.FailFast(e);
            }
        }
#endif
        // NTRAID#Windows Out Of Band Releases-915506-2005/09/09
        // Removed HandleUnexpectedExceptions infrastructure
        /// <summary>
        ///
        /// internal Entry point in msh console host implementation
        ///
        /// </summary>
        /// 
        /// <param name="configuration">
        /// Configuration information to use for creating runspace.
        /// </param>
        /// 
        /// <param name="bannerText">
        /// Banner text to be displayed by ConsoleHost
        /// </param>
        ///
        /// <param name="helpText">
        /// Help text for minishell. This is displayed on 'minishell -?'.
        /// </param>
        ///
        /// <param name="preStartWarning">
        /// 
        /// Warning occurred prior to this point, for example, a snap-in fails to load beforehand.
        /// This string will be printed out.
        /// 
        /// </param>
        /// <param name = "args">
        /// 
        /// Command line parameters to powershell.exe
        /// 
        /// </param>
        /// <returns>
        /// 
        /// The exit code for the shell.
        /// 
        /// NTRAID#Windows OS Bugs-1036968-2005/01/20-sburns The behavior here is related to monitor work.  The low word of the 
        /// exit code is available for the user.  The high word is reserved for the shell and monitor.
        /// 
        /// The shell process needs to return:
        /// 
        /// - if the shell.exe fails init, 0xFFFF0000
        /// - if the exit keyword is called with no parameter at the point of top-level prompt, 0x80000000 (e.g. 0 with the high 
        /// bit set)
        /// - if the exit keyword is called with any int param less than or equal to 0xFFFF, then that int masked with the high 
        /// bit set.  e.g. "exit 3" results in 0x80000003
        /// - if the script ends (in the case of msh -command or msh -commandfile), then 0x80000000.
        /// - if ctrl-break is pressed, with 0xFFFE0000
        /// - if the shell.exe is passed a bad command-line parameter, with 0xFFFD0000.
        /// - if the shell.exe crashes, with 0x00000000
        ///
        /// The monitor process gets the exit code.  If the high bit is set, then the shell process exited normally (though
        /// possibly due to an error).  If not, the shell process crashed.  If the shell.exe exit code is x00000000 (crashed) 
        /// or 0xFFFE0000 (user hit ctrl-break), the monitor should restart the shell.exe. Otherwise, the monitor should exit 
        /// with the same exit code as the shell.exe.
        ///
        /// Anyone checking the exit code of the shell or monitor can mask off the hiword to determine the exit code passed
        /// by the script that the shell last executed.
        /// 
        /// </returns>
        internal static int Start(
            RunspaceConfiguration configuration,
            string bannerText,
            string helpText,
            string preStartWarning,
            string[] args)
        {
#if DEBUG
            if (Environment.GetEnvironmentVariable("POWERSHELL_DEBUG_STARTUP") != null)
            {
                while (!System.Diagnostics.Debugger.IsAttached)
                {
                    Thread.Sleep(1000);
                }
            }
#endif

            try
            {
                string profileDir;
#if UNIX
                profileDir = Platform.SelectProductNameForDirectory(Platform.XDG_Type.CACHE);
#else
                profileDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + 
                    @"\Microsoft\Windows\PowerShell";

                if (!Directory.Exists(profileDir))
                {
                    Directory.CreateDirectory(profileDir);
                }
#endif
                ClrFacade.SetProfileOptimizationRoot(profileDir);
            }
            catch
            {
                // It's safe to ignore errors, the guarded code is just there to try and
                // improve startup performance.
            }

            uint exitCode = ExitCodeSuccess;

            System.Threading.Thread.CurrentThread.Name = "ConsoleHost main thread";

            s_theConsoleHost = ConsoleHost.CreateSingletonInstance(configuration);
            s_theConsoleHost.BindBreakHandler();

            PSHost.IsStdOutputRedirected = Console.IsOutputRedirected;

            if (args == null)
            {
                args = new string[0];
            }

            if (!string.IsNullOrEmpty(preStartWarning))
            {
                s_theConsoleHost.UI.WriteWarningLine(preStartWarning);
            }

            try
            {
                s_cpp = new CommandLineParameterParser(s_theConsoleHost, s_theConsoleHost._ver, bannerText, helpText);
                string[] tempArgs = new string[args.GetLength(0)];
                args.CopyTo(tempArgs, 0);

                s_cpp.Parse(tempArgs);

                // Servermode parameter validation check.
                if ((s_cpp.ServerMode && s_cpp.NamedPipeServerMode) || (s_cpp.ServerMode && s_cpp.SocketServerMode) || (s_cpp.NamedPipeServerMode && s_cpp.SocketServerMode))
                {
                    s_tracer.TraceError("Conflicting server mode parameters, parameters must be used exclusively.");
                    s_theConsoleHost.ui.WriteErrorLine(ConsoleHostStrings.ConflictingServerModeParameters);
                    unchecked
                    {
                        return (int)ExitCodeBadCommandLineParameter;
                    }
                }

                if (s_cpp.ServerMode)
                {
                    ClrFacade.StartProfileOptimization("StartupProfileData-ServerMode");
                    System.Management.Automation.Remoting.Server.OutOfProcessMediator.Run(s_cpp.InitialCommand);
                    exitCode = 0;
                }
                else if (s_cpp.NamedPipeServerMode)
                {
                    ClrFacade.StartProfileOptimization("StartupProfileData-NamedPipeServerMode");
                    System.Management.Automation.Remoting.RemoteSessionNamedPipeServer.RunServerMode(
                        s_cpp.ConfigurationName);
                    exitCode = 0;
                }
                else if (s_cpp.SSHServerMode)
                {
                    ClrFacade.StartProfileOptimization("StartupProfileData-SSHServerMode");
                    System.Management.Automation.Remoting.Server.SSHProcessMediator.Run(s_cpp.InitialCommand);
                    exitCode = 0;
                }
                else if (s_cpp.SocketServerMode)
                {
                    ClrFacade.StartProfileOptimization("StartupProfileData-SocketServerMode");
                    System.Management.Automation.Remoting.Server.HyperVSocketMediator.Run(s_cpp.InitialCommand,
                        s_cpp.ConfigurationName);
                    exitCode = 0;
                }
                else
                {
                    ClrFacade.StartProfileOptimization(
                        s_theConsoleHost.LoadPSReadline()
                            ? "StartupProfileData-Interactive"
                            : "StartupProfileData-NonInteractive");
                    exitCode = s_theConsoleHost.Run(s_cpp, !string.IsNullOrEmpty(preStartWarning));
                }
            }
            finally
            {
                TelemetryAPI.ReportExitTelemetry(s_theConsoleHost);
                s_theConsoleHost.Dispose();
            }

            unchecked
            {
                return (int)exitCode;
            }
        }
        private static CommandLineParameterParser s_cpp;



#if UNIX
        /// <summary>
        ///
        /// The break handler for the program.  Dispatches a break event to the current Executor.
        ///
        /// </summary>
        private static void MyBreakHandler(object sender, ConsoleCancelEventArgs args)
        {
            // Set the Cancel property to true to prevent the process from terminating.
            args.Cancel = true;
            switch (args.SpecialKey)
            {
                case ConsoleSpecialKey.ControlC:
                    SpinUpBreakHandlerThread(false);
                    return;
                case ConsoleSpecialKey.ControlBreak:
                    // Break into script debugger.
                    BreakIntoDebugger();
                    return;
            }
        }
#else
        /// <summary>
        ///
        /// The break handler for the program.  Dispatches a break event to the current Executor.
        ///
        /// </summary>
        /// <param name="signal"></param>
        /// <returns></returns>
        private static bool MyBreakHandler(ConsoleControl.ConsoleBreakSignal signal)
        {
            switch (signal)
            {
                case ConsoleControl.ConsoleBreakSignal.CtrlBreak:
                    // Break into script debugger.
                    BreakIntoDebugger();
                    return true;

                // Run the break handler...
                case ConsoleControl.ConsoleBreakSignal.CtrlC:
                    SpinUpBreakHandlerThread(false);
                    return true;

                case ConsoleControl.ConsoleBreakSignal.Logoff:
                    // Just ignore the logoff signal. This signal is sent to console
                    // apps running as service anytime *any* user logs off which means
                    // that PowerShell couldn't be used in services/tasks if we didn't
                    // suppress this signal...
                    return true;

                case ConsoleControl.ConsoleBreakSignal.Close:
                case ConsoleControl.ConsoleBreakSignal.Shutdown:
                    SpinUpBreakHandlerThread(true);
                    return false;

                default:
                    // Log as much sqm data as possible before we exit.
                    SpinUpBreakHandlerThread(true);
                    return false;
            }
        }
#endif

        private static bool BreakIntoDebugger()
        {
            ConsoleHost host = ConsoleHost.SingletonInstance;
            Debugger debugger = null;
            lock (host.hostGlobalLock)
            {
                if (host._runspaceRef.Runspace != null &&
                    host._runspaceRef.Runspace.GetCurrentlyRunningPipeline() != null)
                {
                    debugger = host._runspaceRef.Runspace.Debugger;
                }
            }
            if (debugger != null)
            {
                debugger.SetDebuggerStepMode(true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// Spin up a new thread to cancel the current pipeline.  This will allow subsequent break interrupts to be received even
        /// if the cancellation is blocked (which can be the case when the pipeline blocks and nothing implements Cmdlet.Stop
        /// properly).  That is because the OS will not inject another thread when a break event occurs if one has already been
        /// injected and is running.
        /// 
        /// </summary>
        /// <param name="shouldEndSession">
        /// 
        /// if true, then flag the parent ConsoleHost that it should shutdown the session.  If false, then only the current 
        /// executing instance is stopped.
        /// 
        ///</param>

        private static void SpinUpBreakHandlerThread(bool shouldEndSession)
        {
            ConsoleHost host = ConsoleHost.SingletonInstance;

            Thread bht = null;

            lock (host.hostGlobalLock)
            {
                if (host._isCtrlCDisabled)
                {
                    return;
                }
                bht = host._breakHandlerThread;
                if (!host.ShouldEndSession && shouldEndSession)
                {
                    host.ShouldEndSession = shouldEndSession;
                }

                // Creation of the tread and starting it should be an atomic operation.
                // otherwise the code in Run method can get instance of the breakhandlerThread
                // after it is created and before started and call join on it. This will result
                // in ThreadStateException. 
                // NTRAID#Windows OutofBand Bugs-938289-2006/07/27-hiteshr
                if (bht == null)
                {
                    // we're not already running HandleBreak on a separate thread, so run it now.

                    host._breakHandlerThread = new Thread(new ThreadStart(ConsoleHost.HandleBreak));
                    host._breakHandlerThread.Name = "ConsoleHost.HandleBreak";
                    host._breakHandlerThread.Start();
                }
            }
        }

        private static void HandleBreak()
        {
            ConsoleHost consoleHost = s_theConsoleHost;
            if (consoleHost != null)
            {
                if (consoleHost.InDebugMode)
                {
                    // Only stop a running user command, ignore prompt evaluation.
                    if (consoleHost.DebuggerCanStopCommand)
                    {
                        // Cancel any executing debugger command if in debug mode.
                        try
                        {
                            consoleHost.Runspace.Debugger.StopProcessCommand();
                        }
                        catch (Exception e)
                        {
                            CommandProcessorBase.CheckForSevereException(e);
                        }
                    }
                }
                else
                {
                    // Cancel the reconnected debugged running pipeline command.
                    if (!StopPipeline(consoleHost.runningCmd))
                    {
                        Executor.CancelCurrentExecutor();
                    }
                }

                if (consoleHost.ShouldEndSession)
                {
                    var runspaceRef = ConsoleHost.SingletonInstance._runspaceRef;
                    if (runspaceRef != null)
                    {
                        var runspace = runspaceRef.Runspace;
                        if (runspace != null)
                        {
                            runspace.Close();
                        }
                    }
                }
            }

            // call the console APIs directly, instead of ui.rawui.FlushInputHandle, as ui may be finalized
            // already if this thread is lagging behind the main thread.

#if !UNIX
            ConsoleHandle handle = ConsoleControl.GetConioDeviceHandle();
            ConsoleControl.FlushConsoleInputBuffer(handle);
#endif

            ConsoleHost.SingletonInstance._breakHandlerThread = null;
        }

        private static bool StopPipeline(Pipeline cmd)
        {
            if (cmd != null &&
                (cmd.PipelineStateInfo.State == PipelineState.Running ||
                 cmd.PipelineStateInfo.State == PipelineState.Disconnected))
            {
                try
                {
                    cmd.StopAsync();
                    return true;
                }
                catch (Exception e)
                {
                    CommandProcessorBase.CheckForSevereException(e);
                }
            }

            return false;
        }

        /// <summary>
        /// Create single instance of ConsoleHost. 
        /// </summary>
        internal static ConsoleHost CreateSingletonInstance(RunspaceConfiguration configuration)
        {
            Dbg.Assert(s_theConsoleHost == null, "CreateSingletonInstance should not be called multiple times");
            s_theConsoleHost = new ConsoleHost(configuration);
            return s_theConsoleHost;
        }

        internal static ConsoleHost SingletonInstance
        {
            get
            {
                Dbg.Assert(s_theConsoleHost != null, "CreateSingletonInstance should be called before calling this method");
                return s_theConsoleHost;
            }
        }

        #endregion static methods

        #region overrides

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override string Name
        {
            get
            {
                const string myName = "ConsoleHost";

                // const, no lock needed.
                return myName;
            }
        }

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override System.Version Version
        {
            get
            {
                // const, no lock needed.
                return _ver;
            }
        }

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override System.Guid InstanceId { get; } = Guid.NewGuid();

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>
        public override PSHostUserInterface UI
        {
            get
            {
                Dbg.Assert(ui != null, "ui should have been allocated in ctor");
                return ui;
            }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        public void PushRunspace(Runspace newRunspace)
        {
            if (_runspaceRef == null) { return; }
            RemoteRunspace remoteRunspace = newRunspace as RemoteRunspace;
            Dbg.Assert(remoteRunspace != null, "Expected remoteRunspace != null");
            remoteRunspace.StateChanged += new EventHandler<RunspaceStateEventArgs>(HandleRemoteRunspaceStateChanged);

            // Unsubscribe the local session debugger.
            if (_runspaceRef.Runspace.Debugger != null)
            {
                _runspaceRef.Runspace.Debugger.DebuggerStop -= OnExecutionSuspended;
            }

            // Subscribe to debugger stop event.
            if (remoteRunspace.Debugger != null)
            {
                remoteRunspace.Debugger.DebuggerStop += OnExecutionSuspended;
            }

            // Connect a disconnected command.
            this.runningCmd = Microsoft.PowerShell.Commands.EnterPSSessionCommand.ConnectRunningPipeline(remoteRunspace);

            // Push runspace.
            _runspaceRef.Override(remoteRunspace, hostGlobalLock, out _isRunspacePushed);
            RunspacePushed.SafeInvoke(this, EventArgs.Empty);

            if (this.runningCmd != null)
            {
                Microsoft.PowerShell.Commands.EnterPSSessionCommand.ContinueCommand(
                    remoteRunspace,
                    this.runningCmd,
                    this,
                    InDebugMode,
                    _runspaceRef.OldRunspace.ExecutionContext);
            }

            this.runningCmd = null;
        }

        /// <summary>
        /// Handles state changed event of the remote runspace. If the remote runspace
        /// gets into a broken or closed state, writes a message and pops out the
        /// runspace
        /// </summary>
        /// <param name="sender">not sure</param>
        /// <param name="eventArgs">arguments describing this event</param>
        private void HandleRemoteRunspaceStateChanged(object sender, RunspaceStateEventArgs eventArgs)
        {
            RunspaceState state = eventArgs.RunspaceStateInfo.State;

            switch (state)
            {
                case RunspaceState.Opening:
                case RunspaceState.Opened:
                    {
                        return;
                    }
                case RunspaceState.Closing:
                case RunspaceState.Closed:
                case RunspaceState.Broken:
                case RunspaceState.Disconnected:
                    {
                        PopRunspace();
                    }
                    break;
            }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        public void PopRunspace()
        {
            if (_runspaceRef == null ||
                !_runspaceRef.IsRunspaceOverridden)
            {
                return;
            }

            if (_inPushedConfiguredSession)
            {
                // For configured endpoint sessions, end session when configured runspace is popped.
                this.ShouldEndSession = true;
            }

            if (_runspaceRef.Runspace.Debugger != null)
            {
                // Unsubscribe pushed runspace debugger.
                _runspaceRef.Runspace.Debugger.DebuggerStop -= OnExecutionSuspended;

                StopPipeline(this.runningCmd);

                if (this.InDebugMode)
                {
                    ExitDebugMode(DebuggerResumeAction.Continue);
                }
            }

            this.runningCmd = null;

            lock (hostGlobalLock)
            {
                _runspaceRef.Revert();
                _isRunspacePushed = false;
            }

            // Re-subscribe local runspace debugger.
            _runspaceRef.Runspace.Debugger.DebuggerStop += OnExecutionSuspended;

            // raise events outside the lock
            RunspacePopped.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// True if a runspace is pushed; false otherwise.
        /// </summary>
        public bool IsRunspacePushed
        {
            get
            {
                return _isRunspacePushed;
            }
        }
        private bool _isRunspacePushed = false;

        /// <summary>
        /// Returns the current runspace associated with this host.
        /// </summary>
        public Runspace Runspace
        {
            get
            {
                if (this.RunspaceRef == null) { return null; }
                return this.RunspaceRef.Runspace;
            }
        }

        internal LocalRunspace LocalRunspace
        {
            get
            {
                if (_isRunspacePushed)
                {
                    return RunspaceRef.OldRunspace as LocalRunspace;
                }
                if (RunspaceRef == null) { return null; }
                return RunspaceRef.Runspace as LocalRunspace;
            }
        }

        public class ConsoleColorProxy
        {
            private ConsoleHostUserInterface _ui;

            public ConsoleColorProxy(ConsoleHostUserInterface ui)
            {
                if (ui == null) throw new ArgumentNullException("ui");
                _ui = ui;
            }

            public ConsoleColor ErrorForegroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                { return _ui.ErrorForegroundColor; }
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                { _ui.ErrorForegroundColor = value; }
            }

            public ConsoleColor ErrorBackgroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                { return _ui.ErrorBackgroundColor; }
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                { _ui.ErrorBackgroundColor = value; }
            }

            public ConsoleColor WarningForegroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                { return _ui.WarningForegroundColor; }
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                { _ui.WarningForegroundColor = value; }
            }

            public ConsoleColor WarningBackgroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                { return _ui.WarningBackgroundColor; }
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                { _ui.WarningBackgroundColor = value; }
            }

            public ConsoleColor DebugForegroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                { return _ui.DebugForegroundColor; }
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                { _ui.DebugForegroundColor = value; }
            }

            public ConsoleColor DebugBackgroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                { return _ui.DebugBackgroundColor; }
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                { _ui.DebugBackgroundColor = value; }
            }

            public ConsoleColor VerboseForegroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                { return _ui.VerboseForegroundColor; }
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                { _ui.VerboseForegroundColor = value; }
            }

            public ConsoleColor VerboseBackgroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                { return _ui.VerboseBackgroundColor; }
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                { _ui.VerboseBackgroundColor = value; }
            }

            public ConsoleColor ProgressForegroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                { return _ui.ProgressForegroundColor; }
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                { _ui.ProgressForegroundColor = value; }
            }

            public ConsoleColor ProgressBackgroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                { return _ui.ProgressBackgroundColor; }
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                { _ui.ProgressBackgroundColor = value; }
            }
        }

        /// <summary>
        /// Return the actual console host object so that the user can get at
        /// the unproxied methods.
        /// </summary>
        public override PSObject PrivateData
        {
            get
            {
                if (ui == null) return null;
                return _consoleColorProxy ?? (_consoleColorProxy = PSObject.AsPSObject(new ConsoleColorProxy(ui)));
            }
        }
        private PSObject _consoleColorProxy;



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override System.Globalization.CultureInfo CurrentCulture
        {
            get
            {
                lock (hostGlobalLock)
                {
#if !CORECLR
                    return NativeCultureResolver.Culture;
#else
                    return CultureInfo.CurrentCulture;
#endif
                }
            }
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override System.Globalization.CultureInfo CurrentUICulture
        {
            get
            {
                lock (hostGlobalLock)
                {
#if !CORECLR
                    return NativeCultureResolver.UICulture;
#else
                    return CultureInfo.CurrentUICulture;
#endif
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception/>

        public override void SetShouldExit(int exitCode)
        {
            lock (hostGlobalLock)
            {
                // Check for the pushed runspace scenario.
                if (this.IsRunspacePushed)
                {
                    this.PopRunspace();
                }
                else if (InDebugMode)
                {
                    ExitDebugMode(DebuggerResumeAction.Continue);
                }
                else
                {
                    _setShouldExitCalled = true;
                    _exitCodeFromRunspace = exitCode;
                    ShouldEndSession = true;
                }
            }
        }

        /// <summary>
        /// 
        /// If an input loop is running, then starts a new, nested input loop.  If an input loop is not running, 
        /// throws an exception.
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// 
        /// If a nested prompt is entered while the host is not running at least one prompt loop.
        /// 
        /// </exception>
        public override void EnterNestedPrompt()
        {
            // save the old Executor, then clear it so that a break does not cancel the pipeline from which this method 
            // might be called.

            Executor oldCurrent = Executor.CurrentExecutor;

            try
            {
                // this assignment is threadsafe -- protected in CurrentExecutor property

                Executor.CurrentExecutor = null;
                lock (hostGlobalLock)
                {
                    IsNested = oldCurrent != null || this.ui.IsCommandCompletionRunning;
                }
                InputLoop.RunNewInputLoop(this, IsNested);
            }
            finally
            {
                Executor.CurrentExecutor = oldCurrent;
            }
        }

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// 
        /// If there is no nested prompt.
        /// 
        /// </exception>
        public override void ExitNestedPrompt()
        {
            lock (hostGlobalLock)
            {
                IsNested = InputLoop.ExitCurrentLoop();
            }
        }

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        public override void NotifyBeginApplication()
        {
            lock (hostGlobalLock)
            {
                ++_beginApplicationNotifyCount;
                if (_beginApplicationNotifyCount == 1)
                {
                    // save the window title when first notified.

                    _savedWindowTitle = ui.RawUI.WindowTitle;
                }
            }
        }

        /// <summary>
        /// 
        /// See base class
        /// 
        /// <seealso cref="NotifyBeginApplication"/>
        /// </summary>
        public override void NotifyEndApplication()
        {
            lock (hostGlobalLock)
            {
                Dbg.Assert(_beginApplicationNotifyCount > 0, "Not running an executable - NotifyBeginApplication was not called!");
                --_beginApplicationNotifyCount;
                if (_beginApplicationNotifyCount == 0)
                {
                    // restore the window title when the last application started has ended.

                    ui.RawUI.WindowTitle = _savedWindowTitle;
                }
            }
        }

        bool IHostProvidesTelemetryData.HostIsInteractive
        {
            get
            {
                return !s_cpp.NonInteractive && !s_cpp.AbortStartup &&
                       ((s_cpp.InitialCommand == null && s_cpp.File == null) || s_cpp.NoExit);
            }
        }
        double IHostProvidesTelemetryData.ProfileLoadTimeInMS { get { return _profileLoadTimeInMS; } }
        double IHostProvidesTelemetryData.ReadyForInputTimeInMS { get { return _readyForInputTimeInMS; } }
        int IHostProvidesTelemetryData.InteractiveCommandCount { get { return _interactiveCommandCount; } }

        private double _profileLoadTimeInMS;
        private double _readyForInputTimeInMS;
        private int _interactiveCommandCount;

        #endregion overrides

        #region non-overrides

        /// <summary>
        /// 
        /// Constructs a new instance
        /// 
        /// </summary>
        internal ConsoleHost(RunspaceConfiguration configuration)
        {
            ClrFacade.SetCurrentThreadUiCulture(this.CurrentUICulture);
            ClrFacade.SetCurrentThreadCulture(this.CurrentCulture);
            // BUG: 610329. Tell PowerShell engine to apply console
            // related properties while launching Pipeline Execution
            // Thread.
            base.ShouldSetThreadUILanguageToZero = true;

            InDebugMode = false;
            _displayDebuggerBanner = true;

            _configuration = configuration;
            this.ui = new ConsoleHostUserInterface(this);
            _consoleWriter = new ConsoleTextWriter(ui);

#if !CORECLR // CurrentDomain.UnhandledException not supported on CoreCLR
            UnhandledExceptionEventHandler handler = new UnhandledExceptionEventHandler(UnhandledExceptionHandler);
            AppDomain.CurrentDomain.UnhandledException += handler;
#endif
        }

        private void BindBreakHandler()
        {
#if UNIX
            Console.CancelKeyPress += new ConsoleCancelEventHandler(MyBreakHandler);
#else
            breakHandlerGcHandle = GCHandle.Alloc(new ConsoleControl.BreakHandler(MyBreakHandler));
            ConsoleControl.AddBreakHandler((ConsoleControl.BreakHandler)breakHandlerGcHandle.Target);
#endif
        }

#if !CORECLR // Not used on NanoServer: CurrentDomain.UnhandledException not supported on CoreCLR
        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            // FYI: sender is a reference to the source app domain

            // The CLR will shut down the app as soon as this handler is complete, but just in case, we want
            // to exit at our next opportunity.

            _shouldEndSession = true;

            Exception e = null;

            if (args != null)
            {
                e = (Exception)args.ExceptionObject;
            }

            ui.WriteLine();
            ui.Write(
                ConsoleColor.Red,
                ui.RawUI.BackgroundColor,
                ConsoleHostStrings.UnhandledExceptionShutdownMessage);
            ui.WriteLine();
        }
#endif

        /// <summary>
        /// 
        /// Finalizes the instance
        /// 
        /// </summary>
        ~ConsoleHost()
        {
            Dispose(false);
        }

        /// <summary>
        /// 
        /// Disposes of this instance, per the IDisposable pattern
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposingNotFinalizing)
        {
            if (!_isDisposed)
            {
#if !UNIX
                Dbg.Assert(breakHandlerGcHandle != null, "break handler should be set");
                ConsoleControl.RemoveBreakHandler();
                breakHandlerGcHandle.Free();
#endif

                if (isDisposingNotFinalizing)
                {
                    if (IsTranscribing)
                    {
                        StopTranscribing();
                    }
                    if (_outputSerializer != null)
                    {
                        _outputSerializer.End();
                    }
                    if (_errorSerializer != null)
                    {
                        _errorSerializer.End();
                    }
                    if (_runspaceRef != null)
                    {
                        // NTRAID#Windows Out Of Band Releases-925297-2005/12/14
                        try
                        {
                            _runspaceRef.Runspace.Dispose();
                        }
                        catch (InvalidRunspaceStateException)
                        {
                        }
                    }
                    _runspaceRef = null;
                    ui = null;
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// 
        /// Indicates if the session should be terminated or not.  Typically set by the break handler for Close, Logoff, and 
        /// Shutdown events.  Note that the only valid transition for this property is from false to true: it is not legal to 
        /// try to set it to false after is was set to true. 
        /// 
        /// </summary> 
        /// <value>
        /// 
        /// true to shut down the session.  false is only allowed if the property is already false.
        /// 
        /// </value>
        internal bool ShouldEndSession
        {
            // This might get called from the main thread, or from the pipeline thread, or from a break handler thread.
            get
            {
                bool result = false;

                lock (hostGlobalLock)
                {
                    result = _shouldEndSession;
                }

                return result;
            }
            set
            {
                lock (hostGlobalLock)
                {
                    // If ShouldEndSession is already true, you can't set it back

                    Dbg.Assert(_shouldEndSession != true || value != false,
                        "ShouldEndSession can only be set from false to true");

                    _shouldEndSession = value;
                }
            }
        }

        /// <summary>
        /// 
        /// The Runspace ref object being used by this Host instance.  A host only opens one Runspace.
        /// 
        /// </summary>
        /// <value></value>
        internal RunspaceRef RunspaceRef
        {
            get
            {
                return _runspaceRef;
            }
        }

        internal WrappedSerializer.DataFormat OutputFormat { get; private set; }


        internal WrappedSerializer.DataFormat InputFormat { get; private set; }

        internal WrappedDeserializer.DataFormat ErrorFormat
        {
            get
            {
                WrappedDeserializer.DataFormat format = OutputFormat;

                //If this shell is invoked in minishell interop mode and error is redirected,
                //always write data in error stream in xml format.
                if (IsInteractive == false && Console.IsErrorRedirected && _wasInitialCommandEncoded)
                {
                    format = Serialization.DataFormat.XML;
                }
                return format;
            }
        }

        internal bool IsRunningAsync
        {
            get
            {
                return !IsInteractive && ((OutputFormat != Serialization.DataFormat.Text) || Console.IsInputRedirected);
            }
        }

        internal bool IsNested { get; private set; }

        internal WrappedSerializer OutputSerializer
        {
            get
            {
                if (_outputSerializer == null)
                {
                    _outputSerializer =
                        new WrappedSerializer(
                            OutputFormat,
                            "Output",
                            Console.IsOutputRedirected ? Console.Out : ConsoleTextWriter);
                }
                return _outputSerializer;
            }
        }

        internal WrappedSerializer ErrorSerializer
        {
            get
            {
                if (_errorSerializer == null)
                {
                    _errorSerializer =
                        new WrappedSerializer(
                            ErrorFormat,
                            "Error",
                            Console.IsErrorRedirected ? Console.Error : ConsoleTextWriter);
                }
                return _errorSerializer;
            }
        }

        internal bool IsInteractive
        {
            get
            {
                // we're running interactive if we're in a prompt loop, and we're not reading keyboard input from stdin.

                return _isRunningPromptLoop && !ui.ReadFromStdin;
            }
        }

        internal TextWriter ConsoleTextWriter
        {
            get
            {
                Dbg.Assert(_consoleWriter != null, "consoleWriter should have been initialized");
                return _consoleWriter;
            }
        }

        /// <summary>
        /// 
        /// The main run loop of the program: processes command line parameters, and starts up a runspace.
        /// 
        /// </summary>
        /// 
        /// <param name="cpp">
        /// Commandline parameter parser. The commandline parameter parser is expected to parse all the 
        /// arguments before calling this method.
        /// </param>
        /// 
        /// <param name="isPrestartWarned">
        /// Is there any warning at startup
        /// </param>
        /// 
        /// <returns>
        /// 
        /// The process exit code to be returned by Main.
        /// 
        /// </returns>

        private uint Run(CommandLineParameterParser cpp, bool isPrestartWarned)
        {
            Dbg.Assert(null != cpp, "CommandLine parameter parser cannot be null.");
            uint exitCode = ExitCodeSuccess;

            do
            {
                s_runspaceInitTracer.WriteLine("starting parse of command line parameters");

                exitCode = ExitCodeSuccess;
                if (!string.IsNullOrEmpty(cpp.InitialCommand) && isPrestartWarned)
                {
                    s_tracer.TraceError("Start up warnings made command \"{0}\" not executed", cpp.InitialCommand);
                    string msg = StringUtil.Format(ConsoleHostStrings.InitialCommandNotExecuted, cpp.InitialCommand);
                    ui.WriteErrorLine(msg);
                    exitCode = ExitCodeInitFailure;
                    break;
                }

                if (cpp.AbortStartup)
                {
                    s_tracer.WriteLine("processing of cmdline args failed, exiting");
                    exitCode = cpp.ExitCode;
                    break;
                }

                OutputFormat = cpp.OutputFormat;
                InputFormat = cpp.InputFormat;
                _wasInitialCommandEncoded = cpp.WasInitialCommandEncoded;

                ui.ReadFromStdin = cpp.ExplicitReadCommandsFromStdin || Console.IsInputRedirected;
                ui.NoPrompt = cpp.NoPrompt;
                ui.ThrowOnReadAndPrompt = cpp.ThrowOnReadAndPrompt;
                _noExit = cpp.NoExit;

                // See if we need to change the process-wide execution
                // policy
                if (!String.IsNullOrEmpty(cpp.ExecutionPolicy))
                {
                    ExecutionPolicy executionPolicy = SecuritySupport.ParseExecutionPolicy(cpp.ExecutionPolicy);
                    SecuritySupport.SetExecutionPolicy(ExecutionPolicyScope.Process, executionPolicy, null);
                }

                // NTRAID#Windows Out Of Band Releases-915506-2005/09/09
                // Removed HandleUnexpectedExceptions infrastructure
                exitCode = DoRunspaceLoop(cpp.InitialCommand, cpp.SkipProfiles, cpp.Args, cpp.StaMode, cpp.ImportSystemModules, cpp.ConfigurationName);
            }
            while (false);

            return exitCode;
        }

        /// <summary>
        /// This method is retained to make V1 tests compatible with V2 as signature of this method
        /// is slightly changed in v2.
        /// </summary>
        /// <param name="bannerText"></param>
        /// <param name="helpText"></param>
        /// <param name="isPrestartWarned"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private uint Run(string bannerText, string helpText, bool isPrestartWarned, string[] args)
        {
            s_cpp = new CommandLineParameterParser(this, _ver, bannerText, helpText);
            s_cpp.Parse(args);
            return Run(s_cpp, isPrestartWarned);
        }

        /// <summary>
        /// 
        /// Loops over the Host's sole Runspace; opens the runspace, initializes it, then recycles it if the Runspace fails.
        /// 
        /// </summary>
        /// <returns>
        /// 
        /// The process exit code to be returned by Main.
        /// 
        /// </returns>
        private uint DoRunspaceLoop(string initialCommand, bool skipProfiles, Collection<CommandParameter> initialCommandArgs, bool staMode,
            bool importSystemModules, string configurationName)
        {
            ExitCode = ExitCodeSuccess;

            while (!ShouldEndSession)
            {
                RunspaceCreationEventArgs args = new RunspaceCreationEventArgs(initialCommand, skipProfiles, staMode, importSystemModules, configurationName, initialCommandArgs);
                CreateRunspace(args);

                if (ExitCode == ExitCodeInitFailure) { break; }

                if (!_noExit)
                {
                    // Wait for runspace to open, init, and run init script before 
                    // setting ShouldEndSession, to allow debugger to work.
                    ShouldEndSession = true;
                }
                else
                {
                    // Start nested prompt loop.
                    EnterNestedPrompt();
                }

                if (_setShouldExitCalled)
                {
                    ExitCode = unchecked((uint)_exitCodeFromRunspace);
                }
                else
                {
                    Executor exec = new Executor(this, false, false);

                    bool dollarHook = exec.ExecuteCommandAndGetResultAsBool("$global:?") ?? false;

                    if (dollarHook && (_lastRunspaceInitializationException == null))
                    {
                        ExitCode = ExitCodeSuccess;
                    }
                    else
                    {
                        ExitCode = ExitCodeSuccess | 0x1;
                    }
                }

                _runspaceRef.Runspace.Close();
                _runspaceRef = null;
                if (staMode) // don't recycle the Runspace in STA mode
                {
                    ShouldEndSession = true;
                }
            }

            return ExitCode;
        }

        private Exception InitializeRunspaceHelper(string command, Executor exec, Executor.ExecutionOptions options)
        {
            Dbg.Assert(!String.IsNullOrEmpty(command), "command should have a value");
            Dbg.Assert(exec != null, "non-null Executor instance needed");

            s_runspaceInitTracer.WriteLine("running command {0}", command);

            Exception e = null;

            if (IsRunningAsync)
            {
                exec.ExecuteCommandAsync(command, out e, options);
            }
            else
            {
                exec.ExecuteCommand(command, out e, options);
            }
            if (e != null)
            {
                ReportException(e, exec);
            }

            return e;
        }

        private void CreateRunspace(object runspaceCreationArgs)
        {
            RunspaceCreationEventArgs args = null;
            try
            {
                args = runspaceCreationArgs as RunspaceCreationEventArgs;
                Dbg.Assert(args != null, "Event Arguments to CreateRunspace should not be null");
                DoCreateRunspace(args.InitialCommand, args.SkipProfiles, args.StaMode, args.ImportSystemModules, args.ConfigurationName, args.InitialCommandArgs);
            }
            catch (ConsoleHostStartupException startupException)
            {
                ReportExceptionFallback(startupException.InnerException, startupException.Message);
                ExitCode = ExitCodeInitFailure;
            }
        }

        /// <summary>
        /// This method is here only to make V1 tests compatible with V2. DO NOT USE THIS FUNCTION! Use DoCreateRunspace instead
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void InitializeRunspace(string initialCommand, bool skipProfiles, Collection<CommandParameter> initialCommandArgs)
        {
            DoCreateRunspace(initialCommand, skipProfiles, staMode: false, importSystemModules: false, configurationName: null, initialCommandArgs: initialCommandArgs);
        }

        private bool LoadPSReadline()
        {
            // Don't load PSReadline if:
            //   * we don't think the process will be interactive, e.g. -command or -file
            //     - exception: when -noexit is specified, we will be interactive after the command/file finishes
            //   * -noninteractive: this should be obvious, they've asked that we don't every prompt
            //
            // Note that PSReadline doesn't support redirected stdin/stdout, but we don't check that here because
            // a future version might, and we should automatically load it at that unknown point in the future.
            // PSReadline will ideally fall back to Console.ReadLine or whatever when stdin/stdout is redirected.
            return ((s_cpp.InitialCommand == null && s_cpp.File == null) || s_cpp.NoExit) && !s_cpp.NonInteractive;
        }

        /// <summary>
        /// 
        /// Opens and Initializes the Host's sole Runspace.  Processes the startup scripts and runs any command passed on the
        /// command line.
        /// 
        /// </summary>

        //private void CreateRunspace(string initialCommand, bool skipProfiles, bool staMode, Collection<CommandParameter> initialCommandArgs)
        private void DoCreateRunspace(string initialCommand, bool skipProfiles, bool staMode, bool importSystemModules, string configurationName, Collection<CommandParameter> initialCommandArgs)
        {
            Dbg.Assert(_runspaceRef == null, "runspace should be null");
#if !DEBUG
            Dbg.Assert(_configuration != null, "configuration should be set");
#endif

            s_runspaceInitTracer.WriteLine("Calling RunspaceFactory.CreateRunspace");

            try
            {
                Runspace consoleRunspace = null;
                bool psReadlineFailed = false;

                // Use InitialSessionState if available.
                if (DefaultInitialSessionState != null)
                {
                    // Load PSReadline by default unless there is no use:
                    //    - we're running a command/file and just exiting
                    //    - stdin is redirected by a parent process
                    //    - we're not interactive
                    //    - we're explicitly reading from stdin (the '-' argument)
                    // It's also important to have a scenario where PSReadline is not loaded so it can be updated, e.g.
                    //    powershell -command "Update-Module PSReadline"
                    // This should work just fine as long as no other instances of PowerShell are running.
                    ReadOnlyCollection<Microsoft.PowerShell.Commands.ModuleSpecification> defaultImportModulesList = null;
                    if (LoadPSReadline())
                    {
                        // Create and open Runspace with PSReadline.
                        defaultImportModulesList = DefaultInitialSessionState.Modules;
                        DefaultInitialSessionState.ImportPSModule(new[] { "PSReadLine" });
                        consoleRunspace = RunspaceFactory.CreateRunspace(this, DefaultInitialSessionState);
                        try
                        {
                            OpenConsoleRunspace(consoleRunspace, staMode);
                        }
                        catch (Exception e)
                        {
                            CommandProcessorBase.CheckForSevereException(e);
                            consoleRunspace = null;
                            psReadlineFailed = true;
                        }
                    }

                    if (consoleRunspace == null)
                    {
                        if (psReadlineFailed)
                        {
                            // Try again but without importing the PSReadline module.
                            DefaultInitialSessionState.ClearPSModules();
                            DefaultInitialSessionState.ImportPSModule(defaultImportModulesList);
                        }
                        consoleRunspace = RunspaceFactory.CreateRunspace(this, DefaultInitialSessionState);
                        OpenConsoleRunspace(consoleRunspace, staMode);
                    }
                }
                else
                {
                    consoleRunspace = RunspaceFactory.CreateRunspace(this, _configuration);
                    OpenConsoleRunspace(consoleRunspace, staMode);
                }

                _runspaceRef = new RunspaceRef(consoleRunspace);

                if (psReadlineFailed)
                {
                    // Notify the user that PSReadline could not be loaded.
                    Console.Error.WriteLine(ConsoleHostStrings.CannotLoadPSReadline);
                }
            }
            catch (Exception e)
            {
                // no need to do CheckForSevereException here
                // since the ConsoleHostStartupException is uncaught 
                // higher in the call stack and the whole process
                // will exit soon
                throw new ConsoleHostStartupException(ConsoleHostStrings.ShellCannotBeStarted, e);
            }
            finally
            {
                // Stop PerfTrack
                PSEtwLog.LogOperationalInformation(PSEventId.Perftrack_ConsoleStartupStop, PSOpcode.WinStop,
                                                   PSTask.PowershellConsoleStartup, PSKeyword.UseAlwaysOperational);
            }

            // Record how long it took from process start to runspace open for telemetry.
            _readyForInputTimeInMS = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;

            DoRunspaceInitialization(importSystemModules, skipProfiles, initialCommand, configurationName, initialCommandArgs);
        }

        private void OpenConsoleRunspace(Runspace runspace, bool staMode)
        {
            // staMode will have following values:
            // On FullPS: 'true'/'false' = default('true'=STA) + possibility of overload through cmdline parameter '-mta'
            // On NanoPS: always 'false' = default('false'=MTA) + NO possibility of overload through cmdline parameter '-mta'
            // ThreadOptions should match on FullPS and NanoPS for corresponding ApartmentStates.
            if (staMode)
            {
                // we can't change ApartmentStates on CoreCLR
#if !CORECLR
                runspace.ApartmentState = ApartmentState.STA;
#endif
                runspace.ThreadOptions = PSThreadOptions.ReuseThread;
            }

            runspace.EngineActivityId = EtwActivity.GetActivityId();

            s_runspaceInitTracer.WriteLine("Calling Runspace.Open");

            runspace.Open();
        }

        private void DoRunspaceInitialization(bool importSystemModules, bool skipProfiles, string initialCommand, string configurationName, Collection<CommandParameter> initialCommandArgs)
        {
            if (_runspaceRef.Runspace.Debugger != null)
            {
                _runspaceRef.Runspace.Debugger.SetDebugMode(DebugModes.LocalScript | DebugModes.RemoteScript);
                _runspaceRef.Runspace.Debugger.DebuggerStop += this.OnExecutionSuspended;
            }

            Executor exec = new Executor(this, false, false);

            // Run import system modules command
            if (importSystemModules)
            {
                Exception exception = InitializeRunspaceHelper("ImportSystemModules", exec, Executor.ExecutionOptions.None);
            }

            if (!string.IsNullOrEmpty(configurationName))
            {
                // If an endpoint configuration is specified then create a loop-back remote runspace targeting
                // the endpoint and push onto runspace ref stack.  Ignore profile and configuration scripts.
                try
                {
                    RemoteRunspace remoteRunspace = HostUtilities.CreateConfiguredRunspace(configurationName, this);
                    remoteRunspace.ShouldCloseOnPop = true;
                    PushRunspace(remoteRunspace);

                    // Ensure that session ends when configured remote runspace is popped.
                    _inPushedConfiguredSession = true;
                }
                catch (Exception e)
                {
                    throw new ConsoleHostStartupException(ConsoleHostStrings.ShellCannotBeStarted, e);
                }
            }
            else
            {
                // Run the built-in scripts
                RunspaceConfigurationEntryCollection<ScriptConfigurationEntry> scripts = new RunspaceConfigurationEntryCollection<ScriptConfigurationEntry>();
                if (_configuration != null)
                    scripts = _configuration.InitializationScripts;

                if ((scripts == null) || (scripts.Count == 0))
                {
                    s_runspaceInitTracer.WriteLine("There are no built-in scripts to run");
                }
                else
                {
                    foreach (ScriptConfigurationEntry s in scripts)
                    {
                        s_runspaceInitTracer.WriteLine("Running script: '{0}'", s.Name);

                        // spec claims that Ctrl-C is not supposed to stop these.

                        try
                        {
                            _isCtrlCDisabled = true;
                            Exception e = InitializeRunspaceHelper(s.Definition, exec, Executor.ExecutionOptions.AddOutputter);
                            if (e != null)
                            {
                                throw new ConsoleHostStartupException(ConsoleHostStrings.InitScriptFailed, e);
                            }
                        }
                        finally
                        {
                            _isCtrlCDisabled = false;
                        }
                    }
                }

                // If -iss has been specified, then there won't be a runspace
                // configuration to get the shell ID from, so we'll use the default...
                string shellId = null;
                if (_configuration != null)
                    shellId = _configuration.ShellId;
                else
                    shellId = "Microsoft.PowerShell"; // TODO: what will happen for custom shells built using Make-Shell.exe

                // If the system lockdown policy says "Enforce", do so. Do this after types / formatting, default functions, etc
                // are loaded so that they are trusted. (Validation of their signatures is done in F&O)
                if (SystemPolicy.GetSystemLockdownPolicy() == SystemEnforcementMode.Enforce)
                {
                    _runspaceRef.Runspace.ExecutionContext.LanguageMode = PSLanguageMode.ConstrainedLanguage;
                }

                string allUsersProfile = HostUtilities.GetFullProfileFileName(null, false);
                string allUsersHostSpecificProfile = HostUtilities.GetFullProfileFileName(shellId, false);
                string currentUserProfile = HostUtilities.GetFullProfileFileName(null, true);
                string currentUserHostSpecificProfile = HostUtilities.GetFullProfileFileName(shellId, true);

                // $PROFILE has to be set from the host
                // Should be "per-user,host-specific profile.ps1"
                // This should be set even if -noprofile is specified
                _runspaceRef.Runspace.SessionStateProxy.SetVariable("PROFILE",
                    HostUtilities.GetDollarProfile(
                        allUsersProfile,
                        allUsersHostSpecificProfile,
                        currentUserProfile,
                        currentUserHostSpecificProfile));

                if (!skipProfiles)
                {
                    // Run the profiles.
                    // Profiles are run in the following order:
                    // 1. host independent profile meant for all users
                    // 2. host specific profile meant for all users
                    // 3. host independent profile of the current user
                    // 4. host specific profile  of the current user

                    var sw = new Stopwatch();
                    sw.Start();
                    RunProfile(allUsersProfile, exec);
                    RunProfile(allUsersHostSpecificProfile, exec);
                    RunProfile(currentUserProfile, exec);
                    RunProfile(currentUserHostSpecificProfile, exec);
                    sw.Stop();

                    var profileLoadTimeInMs = sw.ElapsedMilliseconds;
                    if (profileLoadTimeInMs > 500 && s_cpp.ShowBanner)
                    {
                        Console.Error.WriteLine(ConsoleHostStrings.SlowProfileLoadingMessage, profileLoadTimeInMs);
                    }

                    _profileLoadTimeInMS = profileLoadTimeInMs;
                }
                else
                {
                    s_tracer.WriteLine("-noprofile option specified: skipping profiles");
                }
            }

            // Startup is reported after possibly running the profile, but before running the initial command (or file)
            // if one is specified.
            TelemetryAPI.ReportStartupTelemetry(this);

            // If a file was specified as the argument to run, then run it...
            if (s_cpp != null && s_cpp.File != null)
            {
                string filePath = s_cpp.File;

                s_tracer.WriteLine("running -file '{0}'", filePath);

                Pipeline tempPipeline = exec.CreatePipeline();
                Command c = new Command(filePath, false, false);
                tempPipeline.Commands.Add(c);

                if (initialCommandArgs != null)
                {
                    // add the args passed to the command.

                    foreach (CommandParameter p in initialCommandArgs)
                    {
                        c.Parameters.Add(p);
                    }
                }

                // If we're not going to continue, then get the exit code out of the runspace and
                // and indicate that it should be returned...
                if (!_noExit && !(this.Runspace is RemoteRunspace))
                {
                    this.Runspace.ExecutionContext.ScriptCommandProcessorShouldRethrowExit = true;
                }

                Exception e1;

                if (IsRunningAsync)
                {
                    Executor.ExecutionOptions executionOptions = Executor.ExecutionOptions.AddOutputter;

                    Token[] tokens;
                    ParseError[] errors;

                    // Detect if they're using input. If so, read from it.
                    Ast parsedInput = Parser.ParseFile(filePath, out tokens, out errors);
                    if (AstSearcher.IsUsingDollarInput(parsedInput))
                    {
                        executionOptions |= Executor.ExecutionOptions.ReadInputObjects;

                        // We will consume all of the input to pass to the script, so don't try to read commands from stdin.
                        ui.ReadFromStdin = false;
                    }

                    exec.ExecuteCommandAsyncHelper(tempPipeline, out e1, executionOptions);
                }
                else
                {
                    exec.ExecuteCommandHelper(tempPipeline, out e1, Executor.ExecutionOptions.AddOutputter);
                }

                // Pipeline.Invoke has thrown, that's bad. It means the script did not actually
                // execute properly. These exceptions should be reflected in the exit code
                if (e1 != null)
                {
                    if (!_noExit)
                    {
                        // Set ExitCode to 0x1
                        lock (hostGlobalLock)
                        {
                            _setShouldExitCalled = true;
                            _exitCodeFromRunspace = 0x1;
                            ShouldEndSession = true;
                        }
                    }

                    ReportException(e1, exec);
                }
            }
            else if (!String.IsNullOrEmpty(initialCommand))
            {
                // Run the command passed on the command line

                s_tracer.WriteLine("running initial command");

                Pipeline tempPipeline = exec.CreatePipeline(initialCommand, true);

                if (initialCommandArgs != null)
                {
                    // add the args passed to the command.

                    foreach (CommandParameter p in initialCommandArgs)
                    {
                        tempPipeline.Commands[0].Parameters.Add(p);
                    }
                }

                Exception e1;

                if (IsRunningAsync)
                {
                    Executor.ExecutionOptions executionOptions = Executor.ExecutionOptions.AddOutputter;

                    Token[] tokens;
                    ParseError[] errors;

                    // Detect if they're using input. If so, read from it.
                    Ast parsedInput = Parser.ParseInput(initialCommand, out tokens, out errors);
                    if (AstSearcher.IsUsingDollarInput(parsedInput))
                    {
                        executionOptions |= Executor.ExecutionOptions.ReadInputObjects;

                        // We will consume all of the input to pass to the script, so don't try to read commands from stdin.
                        ui.ReadFromStdin = false;
                    }

                    exec.ExecuteCommandAsyncHelper(tempPipeline, out e1, executionOptions);
                }
                else
                {
                    exec.ExecuteCommandHelper(tempPipeline, out e1, Executor.ExecutionOptions.AddOutputter);
                }

                if (e1 != null)
                {
                    // Remember last exception
                    _lastRunspaceInitializationException = e1;
                    ReportException(e1, exec);
                }
            }
        }

        private void RunProfile(string profileFileName, Executor exec)
        {
            if (!String.IsNullOrEmpty(profileFileName))
            {
                s_runspaceInitTracer.WriteLine("checking profile" + profileFileName);

                try
                {
                    if (File.Exists(profileFileName))
                    {
                        InitializeRunspaceHelper(
                            ". '" + EscapeSingleQuotes(profileFileName) + "'",
                            exec,
                            Executor.ExecutionOptions.AddOutputter);
                    }
                    else
                    {
                        s_runspaceInitTracer.WriteLine("profile file not found");
                    }
                }
                catch (Exception e) // Catch-all OK, 3rd party callout
                {
                    CommandProcessorBase.CheckForSevereException(e);
                    ReportException(e, exec);

                    s_runspaceInitTracer.WriteLine("Could not load profile.");
                }
            }
        }


        /// <summary>
        /// 
        /// Escapes backtick and tick characters with a backtick, returns the result
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>

        internal static string EscapeSingleQuotes(string str)
        {
            // worst case we have to escape every character, so capacity is twice as large as input length
            StringBuilder sb = new StringBuilder(str.Length * 2);

            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];
                if (c == '\'')
                {
                    sb.Append(c);
                }
                sb.Append(c);
            }

            string result = sb.ToString();

            return result;
        }

        private void WriteErrorLine(string line)
        {
            ConsoleColor fg = ConsoleColor.Red;
            ConsoleColor bg = UI.RawUI.BackgroundColor;

            UI.WriteLine(fg, bg, line);
        }

        // NTRAID#Windows Out Of Band Releases-915506-2005/09/09
        // Removed HandleUnexpectedExceptions infrastructure
        private void ReportException(Exception e, Executor exec)
        {
            Dbg.Assert(e != null, "must supply an Exception");
            Dbg.Assert(exec != null, "must supply an Executor");

            // NTRAID#Windows Out Of Band Releases-915506-2005/09/09
            // Removed HandleUnexpectedExceptions infrastructure

            // Attempt to write the exception into the error stream so that the normal F&O machinery will 
            // display it according to preferences.

            object error = null;
            Pipeline tempPipeline = exec.CreatePipeline();

            // NTRAID#Windows OS Bugs-1143621-2005/04/08-sburns 

            IContainsErrorRecord icer = e as IContainsErrorRecord;

            if (icer != null)
            {
                error = icer.ErrorRecord;
            }
            else
            {
                error = (object)new ErrorRecord(e, "ConsoleHost.ReportException", ErrorCategory.NotSpecified, null);
            }

            PSObject wrappedError = new PSObject(error);
            PSNoteProperty note = new PSNoteProperty("writeErrorStream", true);
            wrappedError.Properties.Add(note);

            Exception e1 = null;

            tempPipeline.Input.Write(wrappedError);
            if (IsRunningAsync)
            {
                exec.ExecuteCommandAsyncHelper(tempPipeline, out e1, Executor.ExecutionOptions.AddOutputter);
            }
            else
            {
                exec.ExecuteCommandHelper(tempPipeline, out e1, Executor.ExecutionOptions.AddOutputter);
            }

            if (e1 != null)
            {
                // that didn't work.  Write out the error ourselves as a last resort.

                ReportExceptionFallback(e, null);
            }
        }

        /// <summary>
        /// 
        /// Reports an exception according to the exception reporting settings in effect.
        /// 
        /// </summary>
        /// <param name="e">
        /// 
        /// The exception to report.
        /// 
        /// </param>
        /// <param name="header">
        /// 
        /// Optional header message.  Empty or null means "no header"
        /// 
        /// </param>
        private void ReportExceptionFallback(Exception e, string header)
        {
            if (!string.IsNullOrEmpty(header))
            {
                Console.Error.WriteLine(header);
            }

            if (e == null)
            {
                return;
            }

            // See if the exception has an error record attached to it...
            ErrorRecord er = null;
            IContainsErrorRecord icer = e as IContainsErrorRecord;
            if (icer != null)
                er = icer.ErrorRecord;

            if (e is PSRemotingTransportException)
            {
                // For remoting errors use full fidelity error writer.
                UI.WriteErrorLine(e.Message);
            }
            else if (e is TargetInvocationException)
            {
                Console.Error.WriteLine(e.InnerException.Message);
            }
            else
            {
                Console.Error.WriteLine(e.Message);
            }

            // Add the position message for the error if it's available.
            if (er != null && er.InvocationInfo != null)
                Console.Error.WriteLine(er.InvocationInfo.PositionMessage);
        }

        /// <summary>
        /// raised when the host pops a runspace
        /// </summary>
        internal event EventHandler RunspacePopped;

        /// <summary>
        /// raised when the host pushes a runspace
        /// </summary>
        internal event EventHandler RunspacePushed;


        #endregion non-overrides

        #region debugger

        /// <summary>
        /// Handler for debugger events
        /// </summary>
        private void OnExecutionSuspended(object sender, DebuggerStopEventArgs e)
        {
            // Check local runspace internalHost to see if debugging is enabled.
            LocalRunspace localrunspace = LocalRunspace;
            if ((localrunspace != null) && !localrunspace.ExecutionContext.EngineHostInterface.DebuggerEnabled) { return; }

            _debuggerStopEventArgs = e;
            InputLoop baseLoop = null;

            try
            {
                if (this.IsRunspacePushed)
                {
                    // For remote debugging block data coming from the main (not-nested) 
                    // running command.
                    baseLoop = InputLoop.GetNonNestedLoop();
                    if (baseLoop != null)
                    {
                        baseLoop.BlockCommandOutput();
                    }
                }

                //
                // Display the banner only once per session
                //
                if (_displayDebuggerBanner)
                {
                    WriteDebuggerMessage(ConsoleHostStrings.EnteringDebugger);
                    WriteDebuggerMessage("");
                    _displayDebuggerBanner = false;
                }

                //
                // If we hit a breakpoint output its info
                //
                if (e.Breakpoints.Count > 0)
                {
                    string format = ConsoleHostStrings.HitBreakpoint;

                    foreach (Breakpoint breakpoint in e.Breakpoints)
                    {
                        WriteDebuggerMessage(String.Format(CultureInfo.CurrentCulture, format, breakpoint));
                    }

                    WriteDebuggerMessage("");
                }

                //
                // Write the source line
                //
                if (e.InvocationInfo != null)
                {
                    //    line = StringUtil.Format(ConsoleHostStrings.DebuggerSourceCodeFormat, scriptFileName, e.InvocationInfo.ScriptLineNumber, e.InvocationInfo.Line);
                    WriteDebuggerMessage(e.InvocationInfo.PositionMessage);
                }

                //
                // Start the debug mode
                //
                EnterDebugMode();
            }
            finally
            {
                _debuggerStopEventArgs = null;
                if (baseLoop != null)
                {
                    baseLoop.ResumeCommandOutput();
                }
            }
        }

        /// <summary>
        /// Returns true if the host is in debug mode
        /// </summary>
        private bool InDebugMode { get; set; }

        /// <summary>
        /// True when debugger command is user and available 
        /// for stopping.
        /// </summary>
        internal bool DebuggerCanStopCommand
        {
            get;
            set;
        }

        private Exception _lastRunspaceInitializationException = null;
        internal uint ExitCode;

        /// <summary>
        /// Sets the host to debug mode and enters a nested prompt.
        /// </summary>
        private void EnterDebugMode()
        {
            InDebugMode = true;

            try
            {
                //
                // Note that we need to enter the nested prompt via the InternalHost interface.
                //

                // EnterNestedPrompt must always be run on the local runspace.
                Runspace runspace = _runspaceRef.OldRunspace ?? this.RunspaceRef.Runspace;
                runspace.ExecutionContext.EngineHostInterface.EnterNestedPrompt();
            }
            catch (PSNotImplementedException)
            {
                WriteDebuggerMessage(ConsoleHostStrings.SessionDoesNotSupportDebugger);
            }
            finally
            {
                InDebugMode = false;
            }
        }

        /// <summary>
        /// Exits the debugger's nested prompt.
        /// </summary>
        private void ExitDebugMode(DebuggerResumeAction resumeAction)
        {
            _debuggerStopEventArgs.ResumeAction = resumeAction;

            try
            {
                //
                // Note that we need to exit the nested prompt via the InternalHost interface.
                //

                // ExitNestedPrompt must always be run on the local runspace.
                Runspace runspace = _runspaceRef.OldRunspace ?? this.RunspaceRef.Runspace;
                runspace.ExecutionContext.EngineHostInterface.ExitNestedPrompt();
            }
            catch (ExitNestedPromptException)
            {
                // ignore the exception
            }
        }

        /// <summary>
        /// Writes a line using the debugger colors
        /// </summary>
        private void WriteDebuggerMessage(string line)
        {
            this.ui.WriteWrappedLine(this.ui.DebugForegroundColor, this.ui.DebugBackgroundColor, line);
        }

        #endregion debugger

        #region aux classes

        /// <summary>
        /// 
        /// InputLoop represents the prompt-input-execute loop of the interactive host.  Input loops can be nested, meaning that
        /// one input loop can be interrupted and another started; when the second ends, the first resumes.
        /// 
        /// Neither this class' instances nor its static data is threadsafe.  Caller is responsible for ensuring threadsafe 
        /// access.
        /// 
        /// </summary>
        private class InputLoop
        {
            internal static void RunNewInputLoop(ConsoleHost parent, bool isNested)
            {
                // creates an instance and adds it to the stack and starts it running.

                int stackCount = s_instanceStack.Count;

                if (stackCount == PSHost.MaximumNestedPromptLevel)
                {
                    throw PSTraceSource.NewInvalidOperationException(ConsoleHostStrings.TooManyNestedPromptsError);
                }

                InputLoop il = new InputLoop(parent, isNested);

                s_instanceStack.Push(il);
                il.Run(s_instanceStack.Count > 1);

                // Once the loop has finished running, remove it from the instance stack.

                InputLoop il2 = s_instanceStack.Pop();

                Dbg.Assert(il == il2, "top of instance stack does not correspond to the instance pushed");
            }

            // Presently, this will not work if the Run loop is blocked on a ReadLine call.  Whether that's a
            // problem or not depends on when we expect calls to this function to be made.
            /// <summary>
            /// 
            /// </summary>
            /// <returns>True if next input loop is nested, False otherwise.</returns>
            /// <exception cref="InvalidOperationException">
            ///
            ///  when there is no instanceStack.Count == 0
            /// 
            /// </exception>

            internal static bool ExitCurrentLoop()
            {
                if (s_instanceStack.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(ConsoleHostStrings.InputExitCurrentLoopOutOfSyncError);
                }

                InputLoop il = s_instanceStack.Peek();
                il._shouldExit = true;

                // The main (non-nested) input loop has Count == 1, 
                // so Count == 2 is the value that indicates the next 
                // popped stack input loop is non-nested.
                return (s_instanceStack.Count > 2);
            }

            /// <summary>
            /// Returns current root (non-nested) loop only if there is no
            /// nesting.  This is used *only* by the debugger for remote debugging
            /// where data handling on the base commands needs to be blocked 
            /// during remote debug stop handling.
            /// </summary>
            /// <returns></returns>
            internal static InputLoop GetNonNestedLoop()
            {
                if (s_instanceStack.Count == 1)
                {
                    return s_instanceStack.Peek();
                }

                return null;
            }

            private InputLoop(ConsoleHost parent, bool isNested)
            {
                _parent = parent;
                _isNested = isNested;
                _isRunspacePushed = parent.IsRunspacePushed;
                parent.RunspacePopped += new EventHandler(HandleRunspacePopped);
                parent.RunspacePushed += new EventHandler(HandleRunspacePushed);
                _exec = new Executor(parent, isNested, false);
                _promptExec = new Executor(parent, isNested, true);
            }

            private void HandleRunspacePushed(object sender, EventArgs e)
            {
                lock (_syncObject)
                {
                    _isRunspacePushed = true;
                    _runspacePopped = false;
                }
            }

            /// <summary>
            /// When a runspace is popped, we need to reevaluate the
            /// prompt
            /// </summary>
            /// <param name="sender">sender of this event, unused</param>
            /// <param name="eventArgs">arguments describing this event, unused</param>
            private void HandleRunspacePopped(object sender, EventArgs eventArgs)
            {
                lock (_syncObject)
                {
                    _isRunspacePushed = false;
                    _runspacePopped = true;
                }
            }

            // NTRAID#Windows Out Of Band Releases-915506-2005/09/09
            // Removed HandleUnexpectedExceptions infrastructure
            /// <summary>
            /// 
            /// Evaluates the prompt, displays it, gets a command from the console, and executes it.  Repeats until the command 
            /// is "exit", or until the shutdown flag is set.
            /// 
            /// </summary>
            internal void Run(bool inputLoopIsNested)
            {
                System.Management.Automation.Host.PSHostUserInterface c = _parent.UI;
                ConsoleHostUserInterface ui = c as ConsoleHostUserInterface;

                Dbg.Assert(ui != null, "Host.UI should return an instance.");

                bool inBlockMode = false;
                bool previousResponseWasEmpty = false;
                StringBuilder inputBlock = new StringBuilder();

                while (!_parent.ShouldEndSession && !_shouldExit)
                {
                    try
                    {
                        _parent._isRunningPromptLoop = true;

                        string prompt = null;
                        string line = null;

                        if (!ui.NoPrompt)
                        {
                            if (inBlockMode)
                            {
                                // use a special prompt that denotes block mode

                                prompt = ">> ";
                            }
                            else
                            {
                                // Make sure the cursor is at the start of the line - some external programs don't
                                // write a newline, so we do that for them.
                                if (ui.RawUI.CursorPosition.X != 0)
                                    ui.WriteLine();

                                // Evaluate any suggestions
                                if (!previousResponseWasEmpty)
                                {
                                    EvaluateSuggestions(ui);
                                }

                                // Then output the prompt
                                if (_parent.InDebugMode)
                                {
                                    prompt = EvaluateDebugPrompt();
                                }
                                if (prompt == null)
                                {
                                    prompt = EvaluatePrompt();
                                }
                            }
                            ui.Write(prompt);
                        }
                        previousResponseWasEmpty = false;
                        // There could be a profile. So there could be a user defined custom readline command
                        line = ui.ReadLineWithTabCompletion(_exec);

                        // line will be null in the case that Ctrl-C terminated the input

                        if (line == null)
                        {
                            previousResponseWasEmpty = true;

                            s_tracer.WriteLine("line is null");
                            if (!ui.ReadFromStdin)
                            {
                                // If we're not reading from stdin, the we probably got here
                                // because the user hit ctrl-C. Do a writeline to clean up
                                // the output...
                                ui.WriteLine();
                            }
                            inBlockMode = false;

                            if (Console.IsInputRedirected)
                            {
                                // null is also the result of reading stdin to EOF.
                                _parent.ShouldEndSession = true;
                                break;
                            }

                            continue;
                        }

                        if (line.Trim().Length == 0)
                        {
                            if (inBlockMode)
                            {
                                // end block mode and execute the block accumulated block

                                s_tracer.WriteLine("exiting block mode");
                                line = inputBlock.ToString();
                                inBlockMode = false;
                            }
                            else if (!_parent.InDebugMode)
                            {
                                previousResponseWasEmpty = true;
                                continue;
                            }
                        }
                        else
                        {
                            if (inBlockMode)
                            {
                                s_tracer.WriteLine("adding line to block");
                                inputBlock.Append("\n");
                                inputBlock.Append(line);
                                continue;
                            }
                        }

                        Dbg.Assert(line != null, "line should not be null");
                        Dbg.Assert(line.Length > 0 || _parent.InDebugMode, "line should not be empty unless the host is in debug mode");
                        Dbg.Assert(!inBlockMode, "should not be in block mode at point of pipeline execution");

                        Exception e = null;

                        if (_parent.InDebugMode)
                        {
                            DebuggerCommandResults results = ProcessDebugCommand(line.Trim(), out e);

                            if (results.ResumeAction != null)
                            {
                                _parent.ExitDebugMode(results.ResumeAction.Value);
                            }

                            if (e != null)
                            {
                                var ex = e as PSInvalidOperationException;
                                if (e is PSRemotingTransportException ||
                                    e is RemoteException ||
                                    (ex != null &&
                                     ex.ErrorRecord != null &&
                                     ex.ErrorRecord.FullyQualifiedErrorId.Equals("Debugger:CannotProcessCommandNotStopped", StringComparison.OrdinalIgnoreCase)))
                                {
                                    // Debugger session is broken.  Exit nested loop.
                                    _parent.ExitDebugMode(DebuggerResumeAction.Continue);
                                }
                                else
                                {
                                    // Handle incomplete parse and other errors.
                                    inBlockMode = HandleErrors(e, line, inBlockMode, ref inputBlock);
                                }
                            }

                            continue;
                        }

                        if (_runspacePopped)
                        {
                            string msg = StringUtil.Format(ConsoleHostStrings.CommandNotExecuted, line);
                            ui.WriteErrorLine(msg);
                            _runspacePopped = false;
                        }
                        else
                        {
                            if (_parent.IsRunningAsync && !_parent.IsNested)
                            {
                                _exec.ExecuteCommandAsync(line, out e, Executor.ExecutionOptions.AddOutputter | Executor.ExecutionOptions.AddToHistory);
                            }
                            else
                            {
                                _exec.ExecuteCommand(line, out e, Executor.ExecutionOptions.AddOutputter | Executor.ExecutionOptions.AddToHistory);
                            }

                            Thread bht = null;

                            lock (_parent.hostGlobalLock)
                            {
                                bht = _parent._breakHandlerThread;
                            }

                            if (bht != null)
                            {
                                bht.Join();
                            }

                            // Once the pipeline has been executed, we toss any outstanding progress data and 
                            // take down the display.

                            ui.ResetProgress();

                            if (e != null)
                            {
                                // Handle incomplete parse and other errors.
                                inBlockMode = HandleErrors(e, line, inBlockMode, ref inputBlock);

                                // If a remote runspace is pushed and it is not in a good state
                                // then pop it.
                                if (_isRunspacePushed && (_parent.Runspace != null) &&
                                    ((_parent.Runspace.RunspaceStateInfo.State != RunspaceState.Opened) ||
                                     (_parent.Runspace.RunspaceAvailability != RunspaceAvailability.Available)))
                                {
                                    _parent.PopRunspace();
                                }
                            }

                            if (!inBlockMode)
                                s_theConsoleHost._interactiveCommandCount += 1;
                        }
                    }
                    // NTRAID#Windows Out Of Band Releases-915506-2005/09/09
                    // Removed HandleUnexpectedExceptions infrastructure
                    finally
                    {
                        _parent._isRunningPromptLoop = false;
                    }
                } // end while
            }

            internal void BlockCommandOutput()
            {
                RemotePipeline rCmdPipeline = _parent.runningCmd as RemotePipeline;
                if (rCmdPipeline != null)
                {
                    rCmdPipeline.DrainIncomingData();
                    rCmdPipeline.SuspendIncomingData();
                }
                else
                {
                    _exec.BlockCommandOutput();
                }
            }

            internal void ResumeCommandOutput()
            {
                RemotePipeline rCmdPipeline = _parent.runningCmd as RemotePipeline;
                if (rCmdPipeline != null)
                {
                    rCmdPipeline.ResumeIncomingData();
                }
                else
                {
                    _exec.ResumeCommandOutput();
                }
            }

            private bool HandleErrors(Exception e, string line, bool inBlockMode, ref StringBuilder inputBlock)
            {
                Dbg.Assert(e != null, "Exception reference should not be null.");

                if (IsIncompleteParseException(e))
                {
                    if (!inBlockMode)
                    {
                        inBlockMode = true;
                        inputBlock = new StringBuilder(line);
                    }
                    else
                    {
                        inputBlock.Append(line);
                    }
                }
                else
                {
                    // an exception ocurred when the command was executed.  Tell the user about it.
                    _parent.ReportException(e, _exec);
                }

                return inBlockMode;
            }

            private DebuggerCommandResults ProcessDebugCommand(string cmd, out Exception e)
            {
                DebuggerCommandResults results = null;

                try
                {
                    _parent.DebuggerCanStopCommand = true;

                    // Use PowerShell object to write streaming data to host.
                    using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
                    {
                        PSInvocationSettings settings = new PSInvocationSettings()
                        {
                            Host = _parent
                        };

                        PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
                        ps.AddCommand("Out-Default");
                        IAsyncResult async = ps.BeginInvoke<PSObject>(output, settings, null, null);

                        // Let debugger evaluate command and stream output data.
                        results = _parent.Runspace.Debugger.ProcessCommand(
                            new PSCommand(
                                new Command(cmd, true)),
                            output);

                        output.Complete();
                        ps.EndInvoke(async);
                    }

                    e = null;
                }
                catch (Exception ex)
                {
                    ConsoleHost.CheckForSevereException(ex);
                    e = ex;
                    results = new DebuggerCommandResults(null, false);
                }
                finally
                {
                    _parent.DebuggerCanStopCommand = false;
                }

                // Exit debugger if command fails to evaluate.
                return results ?? new DebuggerCommandResults(DebuggerResumeAction.Continue, false);
            }

            private bool IsIncompleteParseException(Exception e)
            {
                // Check e's type.
                if (e is IncompleteParseException)
                {
                    return true;
                }

                // If it is remote exception ferret out the real exception.
                RemoteException remoteException = e as RemoteException;
                if (remoteException == null || remoteException.ErrorRecord == null)
                {
                    return false;
                }

                return remoteException.ErrorRecord.CategoryInfo.Reason == typeof(IncompleteParseException).Name;
            }

            private void EvaluateSuggestions(ConsoleHostUserInterface ui)
            {
                // Output any training suggestions
                try
                {
                    ArrayList suggestions = HostUtilities.GetSuggestion(_parent.Runspace);

                    if (suggestions.Count > 0)
                    {
                        ui.WriteLine();
                    }

                    bool first = true;
                    foreach (string suggestion in suggestions)
                    {
                        if (!first)
                            ui.WriteLine();

                        ui.WriteLine(suggestion);

                        first = false;
                    }
                }
                catch (TerminateException)
                {
                    // A variable breakpoint may be hit by HostUtilities.GetSuggestion. The debugger throws TerminateExceptions to stop the execution
                    // of the current statement; we do not want to treat these exceptions as errors.
                }
                catch (Exception e)
                {
                    // Catch-all OK. This is a third-party call-out.
                    CommandProcessorBase.CheckForSevereException(e);

                    ui.WriteErrorLine(e.Message);

                    LocalRunspace localRunspace = (LocalRunspace)_parent.Runspace;
                    localRunspace.GetExecutionContext.AppendDollarError(e);
                }
            }

            private string EvaluatePrompt()
            {
                Exception unused = null;
                string promptString = _promptExec.ExecuteCommandAndGetResultAsString("prompt", out unused);

                if (String.IsNullOrEmpty(promptString))
                {
                    promptString = ConsoleHostStrings.DefaultPrompt;
                }

                // Check for the pushed runspace scenario.
                if (_isRunspacePushed)
                {
                    RemoteRunspace remoteRunspace = _parent.Runspace as RemoteRunspace;
                    if (remoteRunspace != null)
                    {
                        promptString = HostUtilities.GetRemotePrompt(remoteRunspace, promptString, _parent._inPushedConfiguredSession);
                    }
                }
                else
                {
                    if (_runspacePopped)
                    {
                        _runspacePopped = false;
                    }
                }

                // Return composed prompt string.
                return promptString;
            }

            private string EvaluateDebugPrompt()
            {
                PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();

                try
                {
                    _parent.Runspace.Debugger.ProcessCommand(
                        new PSCommand(new Command("prompt")),
                        output);
                }
                catch (Exception ex)
                {
                    ConsoleHost.CheckForSevereException(ex);
                    _parent.ReportException(ex, _exec);
                }

                PSObject prompt = output.ReadAndRemoveAt0();
                string promptString = (prompt != null) ? (prompt.BaseObject as string) : null;
                if (promptString != null)
                {
                    RemoteRunspace remoteRunspace = _parent.Runspace as RemoteRunspace;
                    if (remoteRunspace != null)
                    {
                        promptString = HostUtilities.GetRemotePrompt(remoteRunspace, promptString, _parent._inPushedConfiguredSession);
                    }
                }

                return promptString;
            }

            private ConsoleHost _parent;
            private bool _isNested;
            private bool _shouldExit;
            private Executor _exec;
            private Executor _promptExec;
            private object _syncObject = new object();
            private bool _isRunspacePushed = false;
            private bool _runspacePopped = false;

            // The instance stack is used to keep track of which InputLoop instance should be told to exit
            // when PSHost.ExitNestedPrompt is called.

            // threadsafety guaranteed by enclosing class 

            private static Stack<InputLoop> s_instanceStack = new Stack<InputLoop>();
        }

        [Serializable]
        [SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic", Justification =
            "This exception cannot be used outside of the console host application. It is not thrown by a library routine, only by an application.")]
        private class ConsoleHostStartupException : Exception
        {
            internal
            ConsoleHostStartupException()
                :
                base()
            {
            }

            internal
            ConsoleHostStartupException(string message)
                :
                base(message)
            {
            }
#if !CORECLR // ApplicationException & System.Runtime.Serialization.SerializationInfo  are Not In CoreCLR
            protected
            ConsoleHostStartupException(
                System.Runtime.Serialization.SerializationInfo info,
                System.Runtime.Serialization.StreamingContext context)
                :
                base(info, context)
            {
            }
#endif
            internal
            ConsoleHostStartupException(string message, Exception innerException)
                :
                base(message, innerException)
            {
            }
        }

        #endregion aux classes

        /// <summary>
        /// By declaring runspace as ObjectRef&lt;Runspace&gt; we are able to hide the real runspace with 
        /// a remote runspace in the PushRunspace scenario. By declaring it as a mask, the variable 
        /// runspace becomes an indirect reference to the actual runspace which we can override with 
        /// a remote runspace while it is pushed. Also we can easily revert back to the original 
        /// runspace when the PopRunspace command is invoked.
        /// </summary>
        private RunspaceRef _runspaceRef;

#if !UNIX
        private GCHandle breakHandlerGcHandle;
#endif
        private System.Threading.Thread _breakHandlerThread;
        private bool _isDisposed;
        internal ConsoleHostUserInterface ui;
        private string _savedWindowTitle = "";
        private Version _ver = PSVersionInfo.PSVersion;
        private int _exitCodeFromRunspace;
        private bool _noExit = true;
        private bool _isCtrlCDisabled;
        private bool _setShouldExitCalled;
        private bool _isRunningPromptLoop;
        private bool _wasInitialCommandEncoded;
        private RunspaceConfiguration _configuration;

        // hostGlobalLock is used to sync public method calls (in case multiple threads call into the host) and access to 
        // state that persists across method calls, like progress data. It's internal because the ui object also 
        // uses this same object.

        internal object hostGlobalLock = new object();

        // These members are possibly accessed from multiple threads (the break handler thread, a pipeline thread, or the main 
        // thread). We use hostGlobalLock to sync access to them.

        private bool _shouldEndSession;
        private int _beginApplicationNotifyCount;

        private ConsoleTextWriter _consoleWriter;
        private WrappedSerializer _outputSerializer;
        private WrappedSerializer _errorSerializer;
        private bool _displayDebuggerBanner;
        private DebuggerStopEventArgs _debuggerStopEventArgs;
        private bool _inPushedConfiguredSession;
        internal Pipeline runningCmd;

        // The ConsoleHost class is a singleton.  Note that there is not a thread-safety issue with these statics as there can 
        // only be one console host per process.

        private static ConsoleHost s_theConsoleHost;


        internal static InitialSessionState DefaultInitialSessionState;



        [TraceSource("ConsoleHost", "ConsoleHost subclass of S.M.A.PSHost")]
        private static
        PSTraceSource s_tracer = PSTraceSource.GetTracer("ConsoleHost", "ConsoleHost subclass of S.M.A.PSHost");

        [TraceSource("ConsoleHostRunspaceInit", "Initialization code for ConsoleHost's Runspace")]
        private static PSTraceSource s_runspaceInitTracer =
            PSTraceSource.GetTracer("ConsoleHostRunspaceInit", "Initialization code for ConsoleHost's Runspace", false);
    } // ConsoleHost

    /// <summary>
    /// Defines arguments passed to ConsoleHost.CreateRunspace
    /// </summary>
    internal sealed class RunspaceCreationEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs RunspaceCreationEventArgs
        /// </summary>
        /// <param name="initialCommand"> </param>
        /// <param name="skipProfiles"></param>
        /// <param name="staMode"></param>
        /// <param name="importSystemModules"></param>
        /// <param name="configurationName"></param>
        /// <param name="initialCommandArgs"></param>
        internal RunspaceCreationEventArgs(string initialCommand,
                                           bool skipProfiles,
                                           bool staMode,
                                           bool importSystemModules,
                                           string configurationName,
                                           Collection<CommandParameter> initialCommandArgs)
        {
            InitialCommand = initialCommand;
            SkipProfiles = skipProfiles;
            StaMode = staMode;
            ImportSystemModules = importSystemModules;
            ConfigurationName = configurationName;
            InitialCommandArgs = initialCommandArgs;
        }

        internal string InitialCommand { get; set; }
        internal bool SkipProfiles { get; set; }
        internal bool StaMode { get; set; }
        internal bool ImportSystemModules { get; set; }
        internal string ConfigurationName { get; set; }
        internal Collection<CommandParameter> InitialCommandArgs { get; set; }
    }
}   // namespace 


