namespace Microsoft.PowerShell.Linux.Host
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
            // Custom argument parsing
            string initialScript = null;
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    string arg = args[i];
                    bool hasNext = (i+1) < args.Length;
                    string nextArg = hasNext ? args[i+1] : string.Empty;

                    // lone argument is a script
                    if (!hasNext && arg.EndsWith(".ps1"))
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
                    // --working-dir (not on PowerShell for Windows, may be removed)
                    else if (hasNext && arg == "--working-dir")
                    {
                        Directory.SetCurrentDirectory(nextArg);
                        ++i;
                    }
                }
            }
            // TODO: check for input on stdin

            // Create the listener and run it
            Listener listener = new Listener(initialScript);

            // only run if there was no script file passed in
            if (initialScript == null)
            {
                // Display the welcome message.
                Console.WriteLine();
                Console.WriteLine("PowerShell for Linux interactive console");
                Console.WriteLine("========================================");
                Console.WriteLine();
                Console.WriteLine("Current status:");
                Console.WriteLine("- Type 'exit' to exit");
                Console.WriteLine("- Utility and Management cmdlet modules are loadable");
                Console.WriteLine();

                listener.Run();
            }
        }
    }

    internal class Listener
    {
        /// <summary>
        /// Used to read user input.
        /// </summary>
        internal ConsoleReadLine consoleReadLine = new ConsoleReadLine();

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

        public Listener(string initialScript)
        {
            // Create the host and runspace instances for this interpreter. 
            // Note that this application does not support console files so 
            // only the default snap-ins will be available.
            this.myHost = new MyHost(this);
            InitialSessionState iss = InitialSessionState.CreateDefault2();
            this.myRunSpace = RunspaceFactory.CreateRunspace(this.myHost, iss);
            this.myRunSpace.Open();

            // Create a PowerShell object to run the commands used to create
            // $profile and load the profiles.
            lock (this.instanceLock)
            {
                this.currentPowerShell = PowerShell.Create();
            }

            try
            {
                this.currentPowerShell.Runspace = this.myRunSpace;

                PSCommand[] profileCommands = HostUtilities.GetProfileCommands("PSL");
                foreach (PSCommand command in profileCommands)
                {
                    this.currentPowerShell.Commands = command;
                    this.currentPowerShell.Invoke();
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

            // run the initial script
            if (initialScript != null)
            {
                executeHelper(initialScript, null);
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

            // TODO: if we are in block mode, sep the prompt to ">> "
            Pipeline pipeline = rs.CreatePipeline();
            Command promptCommand = new Command("prompt");

            pipeline.Commands.Add(promptCommand);

            Collection<PSObject> output = pipeline.Invoke();
            foreach (PSObject item in output)
            {
                returnVal = item.BaseObject.ToString();
            }

            return returnVal;
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
        private void executeHelper(string cmd, object input)
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

                this.currentPowerShell.AddScript(cmd);

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
                this.executeHelper(cmd, null);
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
            //Console.TreatControlCAsInput = false;

            // Read commands and run them until the ShouldExit flag is set by
            // the user calling "exit".
            while (!this.ShouldExit && this.myHost.Runspace != null)
            {
                string prompt = Prompt(this.myHost.Runspace);

                this.myHost.UI.Write(ConsoleColor.White, ConsoleColor.Black, prompt);
                string cmd = consoleReadLine.Read(this.myHost.Runspace);
                this.Execute(cmd);
            }

            // Exit with the desired exit code that was set by the exit command.
            // The exit code is set in the host by the MyHost.SetShouldExit() method.
            //Environment.Exit(this.ExitCode);
        }
    }
}
