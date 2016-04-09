namespace Microsoft.PowerShell.CoreConsoleHost
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Management.Automation.Runspaces;
    using System.Text;
    using System.IO;
    using System.Runtime.InteropServices;
    using PowerShell = System.Management.Automation.PowerShell;

    public static class Program
    {
        /// <summary>
        /// Creates and initiates the listener instance.
        /// </summary>
        public static void Main(string[] args)
        {
            // Setup the Assembly Load Context, which Core PowerShell uses to
            // analyze the libraries for types, functions, cmdlets, etc. and
            // provide the ability to load assemblies by file path. Doing this
            // here eliminates the need for a custom native host.
            PowerShellAssemblyLoadContextInitializer.SetPowerShellAssemblyLoadContext(string.Empty);

            // Argument parsing
            string initialScript = null;
            bool loadProfiles = true;

            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    string arg = args[i];
                    bool hasNext = (i+1) < args.Length;
                    string nextArg = hasNext ? args[i+1] : string.Empty;

                    // --help was specified
                    if (arg == "--help" || arg == "-h")
                    {
                        Console.WriteLine(@"
usage: powershell[.exe] [ (--help | -h) ]
                        [ (--file | -f) <filePath> ]
                        [ <script>.ps1 ]
                        [ (--command | -c) <string> ]
                        [ --noprofile ]

SYNOPSIS

    Open PowerShell console can take none or one of several arguments.

OPTIONS

    No arguments
            Will launch PowerShell interactively.

    (--file | -f) <filePath>
            Given a file path, will execute as a PowerShell script.

    <script>.ps1
            Given a .ps1 script, will execute without needing --flag.

    (--command | -c) <string>
            Will execute given string as a PowerShell script.

    --noprofile
            Disables parsing of PowerShell profiles.

    (--help | -h)
            Prints this text.
");
                        return;
                    }
                    else if (arg == "--noprofile")
                    {
                        loadProfiles = false;
                    }
                    // lone argument is a script
                    else if (!hasNext && arg.EndsWith(".ps1"))
                    {
                        initialScript = Path.GetFullPath(arg);
                    }
                    // lone argument is an inline script
                    else if (!hasNext)
                    {
                        initialScript = arg;
                    }
                    // --file <filePath> was specified
                    else if (hasNext && (arg == "--file" || arg == "-f"))
                    {
                        initialScript = Path.GetFullPath(nextArg);
                        ++i;
                    }
                    // --command <string> was specified
                    else if (hasNext && (arg == "--command" || arg == "-c"))
                    {
                        if (nextArg == "-")
                        {
                            initialScript = "\"TODO: read stdin using Console.OpenStandardInput\"";
                        }
                        else
                        {
                            initialScript = nextArg;
                        }
                        ++i;
                    }
                }
            }
            // TODO: check for input on stdin

            ConsoleColor InitialForegroundColor = Console.ForegroundColor;
            ConsoleColor InitialBackgroundColor = Console.BackgroundColor;

            // Create the listener and run it
            Listener listener = new Listener(initialScript, loadProfiles);

            // only run if there was no script file passed in
            if (initialScript == null)
            {
                listener.Run();
            }

            Console.ForegroundColor = InitialForegroundColor;
            Console.BackgroundColor = InitialBackgroundColor;

            // Exit with the desired exit code that was set by the exit command.
            // The exit code is set in the host by the MyHost.SetShouldExit() method.
            System.Environment.Exit(listener.ExitCode);
        }
    }

    internal class Listener
    {
        /// <summary>
        /// Used to read user input.
        /// </summary>
        internal ConsoleReadLine consoleReadLine;

        /// <summary>
        /// Holds a reference to the runspace for this interpeter.
        /// </summary>
        internal Runspace myRunSpace;

        /// <summary>
        /// Indicator to tell the host application that it should exit.
        /// </summary>
        private bool shouldExit;

        /// <summary>
        /// The exit code that the host application will use to exit.
        /// </summary>
        private int exitCode;

        /// <summary>
        /// Holds a reference to the PSHost implementation for this interpreter.
        /// </summary>
        private MyHost myHost;

        /// <summary>
        /// Holds a reference to the currently executing pipeline so that it can be
        /// stopped by the control-C handler.
        /// </summary>
        private PowerShell currentPowerShell;

        /// <summary>
        /// Used to serialize access to instance data.
        /// </summary>
        private object instanceLock = new object();

        /// <summary>
        /// To keep track whether we've displayed the debugger help message
        /// </summary>
        private bool _showHelpMessage;

        /// <summary>
        /// To keep track whether last entered command was complete
        /// </summary>
        private bool incompleteLine = false;

        /// <summary>
        /// To store incomplete lines
        /// </summary>
        private string partialLine = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the host application
        /// should exit.
        /// </summary>
        public bool ShouldExit
        {
            get { return this.shouldExit; }
            set { this.shouldExit = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the host application
        /// should exit.
        /// </summary>
        public int ExitCode
        {
            get { return this.exitCode; }
            set { this.exitCode = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether UI should exit.
        /// </summary>
        public bool HasUI;

        public Listener(string initialScript, bool loadProfiles)
        {
            HasUI = (initialScript == null) ? true : false;

            // Create the host and runspace instances for this interpreter.
            // Note that this application does not support console files so
            // only the default snap-ins will be available.
            this.myHost = new MyHost(this);
            InitialSessionState iss = InitialSessionState.CreateDefault2();
            this.myRunSpace = RunspaceFactory.CreateRunspace(this.myHost, iss);
            this.myRunSpace.Open();
            this.consoleReadLine = new ConsoleReadLine(this.myHost.Runspace, this.myHost.UI);

            if (this.myRunSpace.Debugger != null)
            {
                this.myRunSpace.Debugger.DebuggerStop += HandleDebuggerStopEvent;

                // Workflow debugging is new for PowerShell version 4 and is an opt-in
                // feature.  In order to debug Workflow script functions the debugger
                // DebugMode must include the DebugModes.LocalScript flag.
                this.myRunSpace.Debugger.SetDebugMode(DebugModes.LocalScript);
            }

            if (loadProfiles)
            {
                LoadProfiles();
            }

            // run the initial script
            if (initialScript != null)
            {
                Execute(initialScript);
            }
        }

        internal void LoadProfiles()
        {
            // Create a PowerShell object to run the commands used to create
            // $profile and load the profiles.
            lock (this.instanceLock)
            {
                this.currentPowerShell = PowerShell.Create();
            }

            try
            {
                this.currentPowerShell.Runspace = this.myRunSpace;

                PSCommand[] profileCommands = HostUtilities.GetProfileCommands("Microsoft.PowerShellCore");
                foreach (PSCommand command in profileCommands)
                {
                    RunCommand(command);
                }
            }
            finally
            {
                // Dispose the PowerShell object and set currentPowerShell
                // to null. It is locked because currentPowerShell may be
                // accessed by the ctrl-C handler.
                lock (this.instanceLock)
                {
                    this.currentPowerShell.Dispose();
                    this.currentPowerShell = null;
                }
            }
        }

        /// Sets the prompt equal to the output of the prompt function
        public string Prompt(Runspace rs)
        {
            string returnVal = string.Empty;

            if (this.myHost.IsRunspacePushed)
            {
                returnVal = string.Format($"{System.Environment.NewLine}[{this.myRunSpace.ConnectionInfo.ComputerName}] PSL> ");
                return returnVal;
            }

            if (incompleteLine)
            {
                return ">> ";
            }

            Collection<PSObject> output;
            Command promptCommand = new Command("prompt");

            using (Pipeline pipeline = rs.CreatePipeline())
            {
                pipeline.Commands.Add(promptCommand);
                output = pipeline.Invoke();
            }

            foreach (PSObject item in output)
            {
                returnVal = item.BaseObject.ToString();
            }

            return returnVal;
        }

        /// <summary>
        /// Runs individual commands
        /// </summary>
        /// <param name="command">command to run</param>
        internal void RunCommand(PSCommand command)
        {
            if (command == null)
            {
                return;
            }

            command.AddCommand("out-default");
            command.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);

            this.currentPowerShell.Commands = command;

            try
            {
                this.currentPowerShell.Invoke();
            }
            catch (RuntimeException e)
            {
                this.ReportException(e);
            }
        }

        /// <summary>
        /// A helper class that builds and executes a pipeline that writes
        /// to the default output path. Any exceptions that are thrown are
        /// just passed to the caller. Since all output goes to the default
        /// outter, this method does not return anything.
        /// </summary>
        /// <param name="cmd">The script to run.</param>
        /// <param name="input">Any input arguments to pass to the script.
        /// If null then nothing is passed in.</param>
        private void ExecuteHelper(string cmd, object input)
        {
            // Ignore empty command lines.
            if (string.IsNullOrEmpty(cmd))
            {
                return;
            }

            // Create the pipeline object and make it available to the
            // ctrl-C handle through the currentPowerShell instance
            // variable.
            lock (this.instanceLock)
            {
                this.currentPowerShell = PowerShell.Create();
            }

            // Add a script and command to the pipeline and then run the pipeline. Place
            // the results in the currentPowerShell variable so that the pipeline can be
            // stopped.
            try
            {
                this.currentPowerShell.Runspace = this.myRunSpace;

                string fullCommand = incompleteLine ? (partialLine + cmd) : cmd;
                this.currentPowerShell.AddScript(fullCommand);
                incompleteLine = false;

                // Add the default outputter to the end of the pipe and then call the
                // MergeMyResults method to merge the output and error streams from the
                // pipeline. This will result in the output being written using the PSHost
                // and PSHostUserInterface classes instead of returning objects to the host
                // application.
                this.currentPowerShell.AddCommand("out-default");
                this.currentPowerShell.Commands.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);

                // If there is any input pass it in, otherwise just invoke the
                // the pipeline.
                PSInvocationSettings settings = new PSInvocationSettings();
                settings.AddToHistory = true;
                if (input != null)
                {
                    this.currentPowerShell.Invoke(new object[] { input }, settings);
                }
                else
                {
                    this.currentPowerShell.Invoke(null, settings);
                }
            }
            catch (IncompleteParseException)
            {
                incompleteLine = true;
                partialLine = $"{partialLine}{cmd}{System.Environment.NewLine}";
            }

            finally
            {
                // Dispose the PowerShell object and set currentPowerShell to null.
                // It is locked because currentPowerShell may be accessed by the
                // ctrl-C handler.
                lock (this.instanceLock)
                {
                    this.currentPowerShell.Dispose();
                    this.currentPowerShell = null;
                }

                if (!incompleteLine)
                {
                    partialLine = string.Empty;
                }
            }
        }

        /// <summary>
        /// To display an exception using the display formatter,
        /// run a second pipeline passing in the error record.
        /// The runtime will bind this to the $input variable,
        /// which is why $input is being piped to the Out-String
        /// cmdlet. The WriteErrorLine method is called to make sure
        /// the error gets displayed in the correct error color.
        /// </summary>
        /// <param name="e">The exception to display.</param>
        private void ReportException(Exception e)
        {
            if (e != null)
            {
                // Return non-zero exit code if an exception is thrown
                this.ExitCode = 1;

                object error;
                IContainsErrorRecord icer = e as IContainsErrorRecord;
                if (icer != null)
                {
                    error = icer.ErrorRecord;
                }
                else
                {
                    error = (object)new ErrorRecord(e, "Host.ReportException", ErrorCategory.NotSpecified, null);
                }

                lock (this.instanceLock)
                {
                    this.currentPowerShell = PowerShell.Create();
                }

                this.currentPowerShell.Runspace = this.myRunSpace;

                try
                {
                    this.currentPowerShell.AddScript("$input").AddCommand("out-string");

                    // Do not merge errors, this function will swallow errors.
                    Collection<PSObject> result;
                    PSDataCollection<object> inputCollection = new PSDataCollection<object>();
                    inputCollection.Add(error);
                    inputCollection.Complete();
                    result = this.currentPowerShell.Invoke(inputCollection);

                    if (result.Count > 0)
                    {
                        string str = result[0].BaseObject as string;
                        if (!string.IsNullOrEmpty(str))
                        {
                            // Remove \r\n, which is added by the Out-String cmdlet.
                            this.myHost.UI.WriteErrorLine(str.Substring(0, str.Length - 2));
                        }
                    }
                }
                finally
                {
                    // Dispose of the pipeline and set it to null, locking it  because
                    // currentPowerShell may be accessed by the ctrl-C handler.
                    lock (this.instanceLock)
                    {
                        this.currentPowerShell.Dispose();
                        this.currentPowerShell = null;
                    }
                }
            }
        }

        /// <summary>
        /// Basic script execution routine. Any runtime exceptions are
        /// caught and passed back to the Windows PowerShell engine to
        /// display.
        /// </summary>
        /// <param name="cmd">Script to run.</param>
        private void Execute(string cmd)
        {
            try
            {
                // Run the command with no input.
                this.ExecuteHelper(cmd, null);
            }
            catch (RuntimeException rte)
            {
                this.ReportException(rte);
            }
        }

        /// <summary>
        /// Method used to handle control-C's from the user. It calls the
        /// pipeline Stop() method to stop execution. If any exceptions occur
        /// they are printed to the console but otherwise ignored.
        /// </summary>
        /// <param name="sender">See sender property documentation of
        /// ConsoleCancelEventHandler.</param>
        /// <param name="e">See e property documentation of
        /// ConsoleCancelEventHandler.</param>
        private void HandleControlC(object sender, ConsoleCancelEventArgs e)
        {
            try
            {
                lock (this.instanceLock)
                {
                    if (this.currentPowerShell != null && this.currentPowerShell.InvocationStateInfo.State == PSInvocationState.Running)
                    {
                        this.currentPowerShell.Stop();
                    }
                }
                e.Cancel = true;
            }
            catch (Exception exception)
            {
                this.myHost.UI.WriteErrorLine(exception.ToString());
            }
        }

        /// <summary>
        /// Implements the basic listener loop. It sets up the ctrl-C handler, then
        /// reads a command from the user, executes it and repeats until the ShouldExit
        /// flag is set.
        /// </summary>
        internal void Run()
        {
            // Set up the control-C handler.
            Console.CancelKeyPress += new ConsoleCancelEventHandler(this.HandleControlC);

            string initialCommand = String.Empty;

            // Read commands and run them until the ShouldExit flag is set by
            // the user calling "exit".
            while (!this.ShouldExit && this.myHost.Runspace != null)
            {
                // Reset exit code for each command
                this.ExitCode = 0;

                // If the prompt function failed for any reason, use a sane default
                string prompt;
                try
                {
                    prompt = Prompt(this.myHost.Runspace);
                }
                catch
                {
                    prompt = "PS> ";
                }

                this.myHost.UI.Write(prompt);

                ConsoleReadLine.ReadResult result = consoleReadLine.Read(false, initialCommand);
                
                switch(result.state)
                {
                    case ConsoleReadLine.ReadResult.State.Abort:
                        incompleteLine = false;
                        partialLine = string.Empty;
                        initialCommand = String.Empty;
                        break;
                    case ConsoleReadLine.ReadResult.State.Redraw:
                        initialCommand = result.command;
                        break;
                    case ConsoleReadLine.ReadResult.State.Complete:
                    default:
                        this.Execute(result.command);
                        initialCommand = String.Empty;
                        break;
                }
            }
        }

        /// <summary>
        /// Method to handle the Debugger DebuggerStop event.
        /// </summary>
        /// <param name="sender"> Debugger instance
        /// <param name="args"> DebuggerStop event args
        private void HandleDebuggerStopEvent(object sender, DebuggerStopEventArgs args)
        {
            Debugger debugger = sender as Debugger;
            DebuggerResumeAction? resumeAction = null;

            WriteDebuggerStopMessages(args);

            string initialCommand = String.Empty;

            // loop to process Debugger commands.
            while (resumeAction == null)
            {
                string prompt = incompleteLine ? ">> " : "[DBG] PS >> ";
                this.myHost.UI.Write(prompt);

                ConsoleReadLine.ReadResult result = consoleReadLine.Read(true, initialCommand);
                
                switch(result.state)
                {
                    case ConsoleReadLine.ReadResult.State.Abort:
                        incompleteLine = false;
                        partialLine = string.Empty;
                        initialCommand = String.Empty;
                        continue;
                    case ConsoleReadLine.ReadResult.State.Redraw:
                        initialCommand = result.command;
                        continue;
                    case ConsoleReadLine.ReadResult.State.Complete:
                    default:
                        initialCommand = String.Empty;
                        break;
                }

                // Stream output from command processing to console.
                var output = new PSDataCollection<PSObject>();
                output.DataAdded += (dSender, dArgs) =>
                {
                    foreach (var item in output.ReadAll())
                    {
                        this.myHost.UI.WriteLine(item.ToString());
                    }
                };

                // Process command.
                // The Debugger.ProcesCommand method will parse and handle debugger specific
                // commands such as 'h' (help), 'list', 'stepover', etc.  If the command is
                // not specific to the debugger then it will be evaluated as a PowerShell
                // command or script.  The returned DebuggerCommandResults object will indicate
                // whether the command was evaluated by the debugger and if the debugger should
                // be released with a specific resume action.

                PSCommand psCommand = new PSCommand();

                string fullCommand = incompleteLine ? (partialLine + result.command) : result.command;
                psCommand.AddScript(fullCommand).AddCommand("Out-String").AddParameter("Stream", true);
                incompleteLine = false;

                DebuggerCommandResults results = null;
                try
                {
                    results = debugger.ProcessCommand(psCommand, output);
                }
                catch (IncompleteParseException)
                {
                    incompleteLine = true;
                    partialLine = $"{partialLine}{result.command}{System.Environment.NewLine}";
                }

                if (!incompleteLine)
                {
                    partialLine = string.Empty;
                }

                if (!incompleteLine && results.ResumeAction != null)
                {
                    resumeAction = results.ResumeAction;
                }
            }

            // Return from event handler with user resume action.
            args.ResumeAction = resumeAction.Value;
        }

        /// <summary>
        /// Helper method to write debugger stop messages.
        /// </summary>
        /// <param name="args">DebuggerStopEventArgs for current debugger stop</param>
        private void WriteDebuggerStopMessages(DebuggerStopEventArgs args)
        {
            // Write debugger stop information in yellow.
            ConsoleColor saveFGColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            // Show help message only once.
            if (!_showHelpMessage)
            {
                this.myHost.UI.WriteLine("Entering debug mode. Type 'h' to get help.");
                this.myHost.UI.WriteLine();
                _showHelpMessage = true;
            }

            // Breakpoint stop information.  Writes all breakpoints that
            // pertain to this debugger execution stop point.
            if (args.Breakpoints.Count > 0)
            {
                this.myHost.UI.WriteLine("Debugger hit breakpoint on:");
                foreach (var breakPoint in args.Breakpoints)
                {
                    this.myHost.UI.WriteLine(breakPoint.ToString());
                }
                this.myHost.UI.WriteLine();
            }

            // Script position stop information.
            // This writes the InvocationInfo position message if
            // there is one.
            if (args.InvocationInfo != null)
            {
                this.myHost.UI.WriteLine(args.InvocationInfo.PositionMessage);
                this.myHost.UI.WriteLine();
            }

            Console.ForegroundColor = saveFGColor;
        }
    }
}
