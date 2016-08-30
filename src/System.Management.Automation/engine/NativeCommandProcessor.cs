/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
#pragma warning disable 1634, 1691

using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Collections;
using System.Threading;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Xml;
using System.Runtime.InteropServices;
using Dbg = System.Management.Automation.Diagnostics;
using System.Runtime.Serialization;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

#if CORECLR
// Use stubs for SerializableAttribute and ISerializable related types.
using Microsoft.PowerShell.CoreClr.Stubs;
#endif

namespace System.Management.Automation
{
    /// <summary>
    /// Various types of input/output supported by native commands.
    /// </summary>
    /// <remarks>
    /// Most native commands only support text. Other formats
    /// are supported by minishell
    /// </remarks>
    internal enum NativeCommandIOFormat
    {
        Text,
        Xml
    };

    /// <summary>
    /// Different streams produced by minishell output
    /// </summary>
    internal enum MinishellStream
    {
        Output,
        Error,
        Verbose,
        Warning,
        Debug,
        Progress,
        Information,
        Unknown
    }

    /// <summary>
    /// Helper class which holds stream names and also provide conversion
    /// method
    /// </summary>
    internal static class StringToMinishellStreamConverter
    {
        internal const string OutputStream = "output";
        internal const string ErrorStream = "error";
        internal const string DebugStream = "debug";
        internal const string VerboseStream = "verbose";
        internal const string WarningStream = "warning";
        internal const string ProgressStream = "progress";
        internal const string InformationStream = "information";

        internal static MinishellStream ToMinishellStream(string stream)
        {
            Dbg.Assert(stream != null, "caller should validate the parameter");

            MinishellStream ms = MinishellStream.Unknown;
            if (OutputStream.Equals(stream, StringComparison.OrdinalIgnoreCase))
            {
                ms = MinishellStream.Output;
            }
            else if (ErrorStream.Equals(stream, StringComparison.OrdinalIgnoreCase))
            {
                ms = MinishellStream.Error;
            }
            else if (DebugStream.Equals(stream, StringComparison.OrdinalIgnoreCase))
            {
                ms = MinishellStream.Debug;
            }
            else if (VerboseStream.Equals(stream, StringComparison.OrdinalIgnoreCase))
            {
                ms = MinishellStream.Verbose;
            }
            else if (WarningStream.Equals(stream, StringComparison.OrdinalIgnoreCase))
            {
                ms = MinishellStream.Warning;
            }
            else if (ProgressStream.Equals(stream, StringComparison.OrdinalIgnoreCase))
            {
                ms = MinishellStream.Progress;
            }
            else if (InformationStream.Equals(stream, StringComparison.OrdinalIgnoreCase))
            {
                ms = MinishellStream.Information;
            }

            return ms;
        }
    }


    /// <summary>
    /// An output object from the child process.
    /// If it's from the error stream isError will be true
    /// </summary>
    internal class ProcessOutputObject
    {
        /// <summary>
        /// Get the data from this object
        /// </summary>
        /// <value>The data</value>
        internal object Data { get; }

        /// <summary>
        /// Stream to which data belongs
        /// </summary>
        internal MinishellStream Stream { get; }

        /// <summary>
        /// Build an output object
        /// </summary>
        /// <param name="data">The data to output</param>
        /// <param name="stream">stream to which data belongs</param>
        internal ProcessOutputObject(object data, MinishellStream stream)
        {
            Data = data;
            Stream = stream;
        }
    }

    /// <summary>
    /// Provides way to create and execute native commands.
    /// </summary>
    internal class NativeCommandProcessor : CommandProcessorBase
    {
        #region ctor/native command properties

        /// <summary>
        /// Information about application which is invoked by this instance of 
        /// NativeCommandProcessor 
        /// </summary>
        private ApplicationInfo _applicationInfo;

        /// <summary>
        /// Initializes the new instance of NativeCommandProcessor class.
        /// </summary>
        /// 
        /// <param name="applicationInfo">
        /// The information about the application to run.
        /// </param>
        /// 
        /// <param name="context">
        /// The execution context for this command.
        /// </param>
        /// 
        /// <exception cref="ArgumentNullException">
        /// <paramref name="applicationInfo"/> or <paramref name="context"/> is null
        /// </exception>
        internal NativeCommandProcessor(ApplicationInfo applicationInfo, ExecutionContext context)
            : base(applicationInfo)
        {
            if (applicationInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException("applicationInfo");
            }

            _applicationInfo = applicationInfo;
            this._context = context;

            this.Command = new NativeCommand();
            this.Command.CommandInfo = applicationInfo;
            this.Command.Context = context;
            this.Command.commandRuntime = this.commandRuntime = new MshCommandRuntime(context, applicationInfo, this.Command);

            this.CommandScope = context.EngineSessionState.CurrentScope;

            //provide native command a backpointer to this object.
            //When kill is called on the command object,
            //it calls this NCP back to kill the process...
            ((NativeCommand)Command).MyCommandProcessor = this;

            //Create input writer for providing input to the process.
            _inputWriter = new ProcessInputWriter(Command);
        }

        /// <summary>
        /// Gets the NativeCommand associated with this command processor.
        /// </summary>
        private NativeCommand nativeCommand
        {
            get
            {
                NativeCommand command = this.Command as NativeCommand;
                Diagnostics.Assert(command != null, "this.Command is created in the constructor.");
                return command;
            }
        }

        /// <summary>
        /// Gets or sets the name of the native command.
        /// </summary>
        private string NativeCommandName
        {
            get
            {
                string name = _applicationInfo.Name;
                return name;
            }
        }

        /// <summary>
        /// Gets or sets path to the native command.
        /// </summary>
        private string Path
        {
            get
            {
                string path = _applicationInfo.Path;
                return path;
            }
        }

        #endregion ctor/native command properties

        #region parameter binder

        /// <summary>
        /// Variable which is set to true when prepare is called.
        /// Parameter Binder should only be created after Prepare method is called
        /// </summary>
        private bool _isPreparedCalled = false;

        /// <summary>
        /// Parameter binder used by this command processor
        /// </summary>
        private NativeCommandParameterBinderController _nativeParameterBinderController;

        /// <summary>
        /// Gets a new instance of a ParameterBinderController using a NativeCommandParameterBinder
        /// </summary>
        /// 
        /// <param name="command">
        /// The native command to be run.
        /// </param>
        /// 
        /// <returns>
        /// A new parameter binder controller for the specified command.
        /// </returns>
        /// 
        internal ParameterBinderController NewParameterBinderController(InternalCommand command)
        {
            Dbg.Assert(_isPreparedCalled, "parameter binder should not be created before prepared is called");

            if (_isMiniShell)
            {
                _nativeParameterBinderController =
                    new MinishellParameterBinderController(
                        this.nativeCommand);
            }
            else
            {
                _nativeParameterBinderController =
                    new NativeCommandParameterBinderController(
                        this.nativeCommand);
            }

            return _nativeParameterBinderController;
        }

        internal NativeCommandParameterBinderController NativeParameterBinderController
        {
            get
            {
                if (_nativeParameterBinderController == null)
                {
                    NewParameterBinderController(this.Command);
                }
                return _nativeParameterBinderController;
            }
        }

        #endregion parameter binder

        #region internal overrides

        /// <summary>
        /// Prepares the command for execution with the specified CommandParameterInternal.
        /// </summary>
        internal override void Prepare(IDictionary psDefaultParameterValues)
        {
            _isPreparedCalled = true;

            //Check if the application is minishell
            _isMiniShell = IsMiniShell();

            //For minishell parameter binding is done in Complete method because we need 
            //to know if output is redirected before we can bind parameters. 
            if (!_isMiniShell)
            {
                this.NativeParameterBinderController.BindParameters(arguments);
            }
        }

        /// <summary>
        /// Executes the command. This method assumes that Prepare is already called.
        /// </summary>
        internal override void ProcessRecord()
        {
            while (Read())
            {
                // Accumulate everything from the pipe and execute at the end.
                _inputWriter.Add(Command.CurrentPipelineObject);
            }
        }

        /// <summary>
        /// Process object for the invoked application
        /// </summary>
        private System.Diagnostics.Process _nativeProcess;

        /// <summary>
        /// This is used for writing input to the process
        /// </summary>
        private ProcessInputWriter _inputWriter = null;

        /// <summary>
        /// This is used for reading input form the process
        /// </summary>
        private ProcessOutputReader _outputReader = null;

        /// <summary>
        /// Is true if this command is to be run "standalone" - that is, with
        /// no redirection.
        /// </summary>        
        private bool _runStandAlone;

        /// <summary>
        /// object used for synchronization between StopProcessing thread and 
        /// Pipeline thread.
        /// </summary>
        private object _sync = new object();

        /// <summary>
        /// Executes the native command once all of the input has been gathered.
        /// </summary>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline is stopping
        /// </exception>
        /// <exception cref="ApplicationFailedException">
        /// The native command could not be run
        /// </exception>
        internal override void Complete()
        {
            // Indicate whether we need to consider redirecting the output/error of the current native command.
            // Usually a windows program which is the last command in a pipeline can be executed as 'background' -- we don't need to capture its output/error streams.
            bool background;

            // Figure out if we're going to run this process "standalone" i.e. without
            // redirecting anything. This is a bit tricky as we always run redirected so
            // we have to see if the redirection is actually being done at the topmost level or not.

            //Calculate if input and output are redirected.
            bool redirectOutput;
            bool redirectError;
            bool redirectInput;

            CalculateIORedirection(out redirectOutput, out redirectError, out redirectInput);

            // Find out if it's the only command in the pipeline.
            bool soloCommand = this.Command.MyInvocation.PipelineLength == 1;

            // Get the start info for the process. 
            ProcessStartInfo startInfo = GetProcessStartInfo(redirectOutput, redirectError, redirectInput, soloCommand);

            if (this.Command.Context.CurrentPipelineStopping)
            {
                throw new PipelineStoppedException();
            }

            // If a problem occurred in running the program, this exception will
            // be set and should be rethrown at the end of the try/catch block...
            Exception exceptionToRethrow = null;
            Host.Coordinates startPosition = new Host.Coordinates();
            bool scrapeHostOutput = false;

            try
            {
                // If this process is being run standalone, tell the host, which might want
                // to save off the window title or other such state as might be tweaked by 
                // the native process
                if (!redirectOutput)
                {
                    this.Command.Context.EngineHostInterface.NotifyBeginApplication();

                    // Also, store the Raw UI coordinates so that we can scrape the screen after
                    // if we are transcribing.
                    try
                    {
                        if (this.Command.Context.EngineHostInterface.UI.IsTranscribing)
                        {
                            scrapeHostOutput = true;
                            startPosition = this.Command.Context.EngineHostInterface.UI.RawUI.CursorPosition;
                            startPosition.X = 0;
                        }
                    }
                    catch (Host.HostException)
                    {
                        // The host doesn't support scraping via its RawUI interface
                        scrapeHostOutput = false;
                    }
                }

                //Start the process. If stop has been called, throw exception.
                //Note: if StopProcessing is called which this method has the lock,
                //Stop thread will wait for nativeProcess to start.
                //If StopProcessing gets the lock first, then it will set the stopped
                //flag and this method will throw PipelineStoppedException when it gets
                //the lock.
                lock (_sync)
                {
                    if (_stopped)
                    {
                        throw new PipelineStoppedException();
                    }

                    try
                    {
                        _nativeProcess = new Process();
                        _nativeProcess.StartInfo = startInfo;
                        _nativeProcess.Start();
                    }
                    catch (Win32Exception)
                    {
#if CORECLR             // Shell doesn't exist on OneCore, so a file cannot be associated with an executable,
                        // and we cannot run an executable as 'ShellExecute' either.
                        throw;
#else
                        // See if there is a file association for this command. If so
                        // then we'll use that. If there's no file association, then
                        // try shell execute...
                        string executable = FindExecutable(startInfo.FileName);
                        bool notDone = true;
                        if (!String.IsNullOrEmpty(executable))
                        {
                            if (IsConsoleApplication(executable))
                            {
                                // Allocate a console if there isn't one attached already...
                                ConsoleVisibility.AllocateHiddenConsole();
                            }

                            string oldArguments = startInfo.Arguments;
                            string oldFileName = startInfo.FileName;
                            startInfo.Arguments = "\"" + startInfo.FileName + "\" " + startInfo.Arguments;
                            startInfo.FileName = executable;
                            try
                            {
                                _nativeProcess.Start();
                                notDone = false;
                            }
                            catch (Win32Exception)
                            {
                                // Restore the old filename and arguments to try shell execute last...
                                startInfo.Arguments = oldArguments;
                                startInfo.FileName = oldFileName;
                            }
                        }
                        // We got here because there was either no executable found for this 
                        // file or we tried to launch the exe and it failed. In either case
                        // we will try launching one last time using ShellExecute...
                        if (notDone)
                        {
                            if (soloCommand && startInfo.UseShellExecute == false)
                            {
                                startInfo.UseShellExecute = true;
                                startInfo.RedirectStandardInput = false;
                                startInfo.RedirectStandardOutput = false;
                                startInfo.RedirectStandardError = false;
                                _nativeProcess.Start();
                            }
                            else
                            {
                                throw;
                            }
                        }
#endif
                    }
                }

                if (this.Command.MyInvocation.PipelinePosition < this.Command.MyInvocation.PipelineLength)
                {
                    // Never background unless you're at the end of a pipe.
                    // Something like
                    //    ls | notepad | sort.exe
                    // should block until the notepad process is terminated.
                    background = false;
                }
                else
                {
                    background = true;
                    if (startInfo.UseShellExecute == false)
                    {
                        background = IsWindowsApplication(_nativeProcess.StartInfo.FileName);
                    }
                }

                try
                {
                    //If input is redirected, start input to process.
                    if (startInfo.RedirectStandardInput)
                    {
                        NativeCommandIOFormat inputFormat = NativeCommandIOFormat.Text;
                        if (_isMiniShell)
                        {
                            inputFormat = ((MinishellParameterBinderController)NativeParameterBinderController).InputFormat;
                        }
                        lock (_sync)
                        {
                            if (!_stopped)
                            {
                                _inputWriter.Start(_nativeProcess, inputFormat);
                            }
                        }
                    }

                    if (background == false)
                    {
                        //if output is redirected, start reading output of process.
                        if (startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
                        {
                            lock (_sync)
                            {
                                if (!_stopped)
                                {
                                    _outputReader = new ProcessOutputReader(_nativeProcess, Path, redirectOutput, redirectError);
                                    _outputReader.Start();
                                }
                            }
                            if (_outputReader != null)
                            {
                                ProcessOutputHelper();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    StopProcessing();
                    throw;
                }
                finally
                {
                    if (background == false)
                    {
                        //Wait for process to exit
                        _nativeProcess.WaitForExit();

                        //Wait for input writer to finish.
                        _inputWriter.Done();

                        //Wait for outputReader to finish
                        if (_outputReader != null)
                        {
                            _outputReader.Done();
                        }

                        // Capture screen output if we are transcribing
                        if (this.Command.Context.EngineHostInterface.UI.IsTranscribing &&
                            scrapeHostOutput)
                        {
                            Host.Coordinates endPosition = this.Command.Context.EngineHostInterface.UI.RawUI.CursorPosition;
                            endPosition.X = this.Command.Context.EngineHostInterface.UI.RawUI.BufferSize.Width - 1;

                            // If the end position is before the start position, then capture the entire buffer.
                            if (endPosition.Y < startPosition.Y)
                            {
                                startPosition.Y = 0;
                            }

                            Host.BufferCell[,] bufferContents = this.Command.Context.EngineHostInterface.UI.RawUI.GetBufferContents(
                                new Host.Rectangle(startPosition, endPosition));

                            StringBuilder lineContents = new StringBuilder();
                            StringBuilder bufferText = new StringBuilder();

                            for (int row = 0; row < bufferContents.GetLength(0); row++)
                            {
                                if (row > 0)
                                {
                                    bufferText.Append(Environment.NewLine);
                                }

                                lineContents.Clear();
                                for (int column = 0; column < bufferContents.GetLength(1); column++)
                                {
                                    lineContents.Append(bufferContents[row, column].Character);
                                }

                                bufferText.Append(lineContents.ToString().TrimEnd(Utils.Separators.SpaceOrTab));
                            }

                            this.Command.Context.InternalHost.UI.TranscribeResult(bufferText.ToString());
                        }

                        this.Command.Context.SetVariable(SpecialVariables.LastExitCodeVarPath, _nativeProcess.ExitCode);
                        if (_nativeProcess.ExitCode != 0)
                            this.commandRuntime.PipelineProcessor.ExecutionFailed = true;
                    }
                }
            }
            catch (Win32Exception e)
            {
                exceptionToRethrow = e;
            } // try
            catch (PipelineStoppedException)
            {
                // If we're stopping the process, just rethrow this exception...
                throw;
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);

                exceptionToRethrow = e;
            }
            finally
            {
                if (!redirectOutput)
                {
                    this.Command.Context.EngineHostInterface.NotifyEndApplication();
                }
                // Do the clean up...
                CleanUp();
            }

            // An exception was thrown while attempting to run the program
            // so wrap and rethrow it here...
            if (exceptionToRethrow != null)
            {
                // It's a system exception so wrap it in one of ours and re-throw.
                string message = StringUtil.Format(ParserStrings.ProgramFailedToExecute,
                    this.NativeCommandName, exceptionToRethrow.Message,
                    this.Command.MyInvocation.PositionMessage);
                ApplicationFailedException appFailedException = new ApplicationFailedException(message, exceptionToRethrow);

                // There is no need to set this exception here since this exception will eventually be caught by pipeline processor.
                // this.commandRuntime.PipelineProcessor.ExecutionFailed = true;

                throw appFailedException;
            }
        }


        #region Process cleanup with Child Process cleanup

        /// <summary>
        /// Utility routine to kill a process, discarding non-critical exceptions.
        /// This utility makes two passes at killing a process. In the first pass,
        /// if the process handle is invalid (as seems to be the case with an ntvdm)
        /// then we try to get a fresh handle based on the original process id.
        /// </summary>
        /// <param name="processToKill">The process to kill</param>
        private static void KillProcess(Process processToKill)
        {
            if (NativeCommandProcessor.IsServerSide)
            {
                Process[] currentlyRunningProcs = Process.GetProcesses();
                ProcessWithParentId[] procsWithParentId = ProcessWithParentId.Construct(currentlyRunningProcs);
                KillProcessAndChildProcesses(processToKill, procsWithParentId);
                return;
            }

            try
            {
                processToKill.Kill();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                try
                {
                    // For processes running in an NTVDM, trying to kill with
                    // the original handle fails with a Win32 error, so we'll 
                    // use the ID and try to get a new handle...
                    Process newHandle = Process.GetProcessById(processToKill.Id);
                    // If the process was not found, we won't get here...
                    newHandle.Kill();
                }
                catch (Exception e) // ignore non-severe exceptions
                {
                    CommandProcessorBase.CheckForSevereException(e);
                }
            }
            catch (Exception e) // ignore non-severe exceptions
            {
                CommandProcessorBase.CheckForSevereException(e);
            }
        }

        /// <summary>
        /// Used by remote server to kill a process tree given
        /// a process id. Process class does not have ParentId
        /// property, so this wrapper uses WMI to get ParentId
        /// and wraps the original process.
        /// </summary>
        internal struct ProcessWithParentId
        {
            public Process OriginalProcessInstance;
            private int _parentId;
            public int ParentId
            {
                get
                {
                    // Construct parent id only once.
                    if (int.MinValue == _parentId)
                    {
                        ConstructParentId();
                    }
                    return _parentId;
                }
            }

            public ProcessWithParentId(Process originalProcess)
            {
                OriginalProcessInstance = originalProcess;
                _parentId = int.MinValue;
            }

            public static ProcessWithParentId[] Construct(Process[] originalProcCollection)
            {
                ProcessWithParentId[] result = new ProcessWithParentId[originalProcCollection.Length];
                for (int index = 0; index < originalProcCollection.Length; index++)
                {
                    result[index] = new ProcessWithParentId(originalProcCollection[index]);
                }
                return result;
            }

            private void ConstructParentId()
            {
                try
                {
                    // note that we have tried to retrieved parent id once.
                    // retrieving parent id might throw exceptions..so
                    // setting this to -1 so that we dont try again to
                    // get the parent id.
                    _parentId = -1;

                    Process parentProcess = PsUtils.GetParentProcess(OriginalProcessInstance);
                    if (parentProcess != null)
                    {
                        _parentId = parentProcess.Id;
                    }
                }
                catch (Win32Exception)
                {
                }
                catch (InvalidOperationException)
                {
                }
                catch (Microsoft.Management.Infrastructure.CimException)
                {
                }
            }
        }

        /// <summary>
        /// Kills the process tree (process + associated child processes)
        /// </summary>
        /// <param name="processToKill"></param>
        /// <param name="currentlyRunningProcs"></param>
        private static void KillProcessAndChildProcesses(Process processToKill,
            ProcessWithParentId[] currentlyRunningProcs)
        {
            try
            {
                // Kill children first..
                int processId = processToKill.Id;
                KillChildProcesses(processId, currentlyRunningProcs);

                // kill the parent after children terminated.
                processToKill.Kill();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                try
                {
                    // For processes running in an NTVDM, trying to kill with
                    // the original handle fails with a Win32 error, so we'll 
                    // use the ID and try to get a new handle...
                    Process newHandle = Process.GetProcessById(processToKill.Id);

                    // If the process was not found, we won't get here...
                    newHandle.Kill();
                }
                catch (Exception e) // ignore non-severe exceptions
                {
                    CommandProcessorBase.CheckForSevereException(e);
                }
            }
            catch (Exception e) // ignore non-severe exceptions
            {
                CommandProcessorBase.CheckForSevereException(e);
            }
        }

        private static void KillChildProcesses(int parentId, ProcessWithParentId[] currentlyRunningProcs)
        {
            foreach (ProcessWithParentId proc in currentlyRunningProcs)
            {
                if ((proc.ParentId > 0) && (proc.ParentId == parentId))
                {
                    KillProcessAndChildProcesses(proc.OriginalProcessInstance, currentlyRunningProcs);
                }
            }
        }

        #endregion

        #region checkForConsoleApplication

        /// <summary>
        /// Return true if the passed in process is a console process.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static bool IsConsoleApplication(string fileName)
        {
            return !IsWindowsApplication(fileName);
        }

        /// <summary>
        /// Check if the passed in process is a windows application.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [ArchitectureSensitive]
        private static bool IsWindowsApplication(string fileName)
        {
#if CORECLR
            return false;
#else
            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr type = SHGetFileInfo(fileName, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_EXETYPE);

            switch ((int)type)
            {
                case 0x0:
                    // 0x0 = not an exe
                    return false;
                case 0x5a4d:
                    // 0x5a4d - DOS .exe or .com file
                    return false;
                case 0x4550:
                    // 0x4550 - windows console app or bat file
                    return false;
                default:
                    // anything else - is a windows program...
                    return true;
            }
#endif
        }

        #endregion checkForConsoleApplication

        /// <summary>
        /// This is set to true when StopProcessing is called
        /// </summary>
        private bool _stopped = false;
        /// <summary>
        /// Routine used to stop this processing on this node...
        /// </summary>
        internal void StopProcessing()
        {
            lock (_sync)
            {
                if (_stopped) return;
                _stopped = true;
            }

            if (_nativeProcess != null)
            {
                if (!_runStandAlone)
                {
                    //Stop input writer
                    _inputWriter.Stop();

                    //stop output writer
                    if (_outputReader != null)
                    {
                        _outputReader.Stop();
                    }

                    KillProcess(_nativeProcess);
                }
            }
        }

        #endregion internal overrides        

        /// <summary>
        /// Aggressively clean everything up...
        /// </summary>
        private void CleanUp()
        {
            try
            {
                if (_nativeProcess != null)
                {
                    _nativeProcess.Dispose();
                }
            }
            catch (Exception e) // ignore non-severe exceptions
            {
                CommandProcessorBase.CheckForSevereException(e);
            }
        }

        /// <summary>
        /// This method process the output
        /// </summary>
        private void ProcessOutputHelper()
        {
            Dbg.Assert(_outputReader != null, "this should be called only when output has been created");

            object value = _outputReader.Read();
            while (value != AutomationNull.Value)
            {
                ProcessOutputObject outputValue = value as ProcessOutputObject;
                Dbg.Assert(outputValue != null, "only object of type ProcessOutputObject expected");

                if (outputValue.Stream == MinishellStream.Error)
                {
                    ErrorRecord record = outputValue.Data as ErrorRecord;
                    Dbg.Assert(record != null, "ProcessReader should ensure that data is ErrorRecord");
                    record.SetInvocationInfo(this.Command.MyInvocation);
                    this.commandRuntime._WriteErrorSkipAllowCheck(record, isNativeError: true);
                }
                else if (outputValue.Stream == MinishellStream.Output)
                {
                    this.commandRuntime._WriteObjectSkipAllowCheck(outputValue.Data);
                }
                else if (outputValue.Stream == MinishellStream.Debug)
                {
                    string temp = outputValue.Data as string;
                    Dbg.Assert(temp != null, "ProcessReader should ensure that data is string");
                    this.Command.PSHostInternal.UI.WriteDebugLine(temp);
                }
                else if (outputValue.Stream == MinishellStream.Verbose)
                {
                    string temp = outputValue.Data as string;
                    Dbg.Assert(temp != null, "ProcessReader should ensure that data is string");
                    this.Command.PSHostInternal.UI.WriteVerboseLine(temp);
                }
                else if (outputValue.Stream == MinishellStream.Warning)
                {
                    string temp = outputValue.Data as string;
                    Dbg.Assert(temp != null, "ProcessReader should ensure that data is string");
                    this.Command.PSHostInternal.UI.WriteWarningLine(temp);
                }
                else if (outputValue.Stream == MinishellStream.Progress)
                {
                    PSObject temp = outputValue.Data as PSObject;
                    if (temp != null)
                    {
                        long sourceId = 0;
                        PSMemberInfo info = temp.Properties["SourceId"];
                        if (info != null)
                        {
                            sourceId = (long)info.Value;
                        }
                        info = temp.Properties["Record"];
                        ProgressRecord rec = null;
                        if (info != null)
                        {
                            rec = info.Value as ProgressRecord;
                        }
                        if (rec != null)
                        {
                            this.Command.PSHostInternal.UI.WriteProgress(sourceId, rec);
                        }
                    }
                }
                else if (outputValue.Stream == MinishellStream.Information)
                {
                    InformationRecord record = outputValue.Data as InformationRecord;
                    Dbg.Assert(record != null, "ProcessReader should ensure that data is InformationRecord");
                    this.commandRuntime.WriteInformation(record);
                }

                if (this.Command.Context.CurrentPipelineStopping)
                {
                    this.StopProcessing();
                    break;
                }
                value = _outputReader.Read();
            }
        }

        /// <summary>
        /// Gets the start info for process
        /// </summary>
        /// <param name="redirectOutput"></param>
        /// <param name="redirectError"></param>
        /// <param name="redirectInput"></param>
        /// <param name="soloCommand"></param>
        /// <returns></returns>
        private ProcessStartInfo GetProcessStartInfo(bool redirectOutput, bool redirectError, bool redirectInput, bool soloCommand)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = this.Path;

            // On Windows, check the extension list and see if we should try to execute this directly.
            // Otherwise, use the platform library to check executability
            if ((Platform.IsWindows && ValidateExtension(this.Path))
                || (!Platform.IsWindows && Platform.NonWindowsIsExecutable(this.Path)))
            {
                startInfo.UseShellExecute = false;
                if (redirectInput)
                {
                    startInfo.RedirectStandardInput = true;
                }
                if (redirectOutput)
                {
                    startInfo.RedirectStandardOutput = true;
                }
                if (redirectError)
                {
                    startInfo.RedirectStandardError = true;
                }
            }
            else
            {
#if CORECLR     // Shell doesn't exist on OneCore, so documents cannot be associated with an application.
                // Therefore, we cannot run document directly on OneCore.
                throw InterpreterError.NewInterpreterException(this.Path, typeof(RuntimeException),
                    this.Command.InvocationExtent, "CantActivateDocumentInPowerShellCore", ParserStrings.CantActivateDocumentInPowerShellCore, this.Path);
#else
                // We only want to ShellExecute something that is standalone...
                if (!soloCommand)
                {
                    throw InterpreterError.NewInterpreterException(this.Path, typeof(RuntimeException),
                        this.Command.InvocationExtent, "CantActivateDocumentInPipeline", ParserStrings.CantActivateDocumentInPipeline, this.Path);
                }

                startInfo.UseShellExecute = true;
#endif
            }

            //For minishell value of -outoutFormat parameter depends on value of redirectOutput.
            //So we delay the parameter binding. Do parameter binding for minishell now.
            if (_isMiniShell)
            {
                MinishellParameterBinderController mpc = (MinishellParameterBinderController)NativeParameterBinderController;
                mpc.BindParameters(arguments, redirectOutput, this.Command.Context.EngineHostInterface.Name);
                startInfo.CreateNoWindow = mpc.NonInteractive;
            }
            startInfo.Arguments = NativeParameterBinderController.Arguments;

            ExecutionContext context = this.Command.Context;

            // Start command in the current filesystem directory
            string rawPath =
                context.EngineSessionState.GetNamespaceCurrentLocation(
                    context.ProviderNames.FileSystem).ProviderPath;
            startInfo.WorkingDirectory = WildcardPattern.Unescape(rawPath);
            return startInfo;
        }

        private bool IsDownstreamOutDefault(Pipe downstreamPipe)
        {
            Diagnostics.Assert(downstreamPipe != null, "Caller makes sure the passed-in parameter is not null.");

            // Check if the downstream cmdlet is Out-Default, which is the default outputter.
            CommandProcessorBase outputProcessor = downstreamPipe.DownstreamCmdlet;
            if (outputProcessor != null)
            {
                // We have the test 'utscript\Engine\TestOutDefaultRedirection.ps1' to check that a user defined
                // Out-Default function should not cause a native command to be redirected. So here we should only 
                // compare the command name to avoid breaking change.
                if (String.Equals(outputProcessor.CommandInfo.Name, "Out-Default", StringComparison.OrdinalIgnoreCase))
                {
                    // Verify that this isn't an Out-Default added for transcribing
                    if (!outputProcessor.Command.MyInvocation.BoundParameters.ContainsKey("Transcript"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// This method calculates if input and output of the process are redirected
        /// </summary>
        /// <param name="redirectOutput"></param>
        /// <param name="redirectError"></param>
        /// <param name="redirectInput"></param>
        private void CalculateIORedirection(out bool redirectOutput, out bool redirectError, out bool redirectInput)
        {
            redirectInput = true;
            redirectOutput = true;
            redirectError = true;


            // Figure out if we're going to run this process "standalone" i.e. without
            // redirecting anything. This is a bit tricky as we always run redirected so
            // we have to see if the redirection is actually being done at the topmost level or not.

            // If we're eligible to be running standalone, that is, without redirection
            // use our pipeline position to determine if we really want to redirect
            // input and error or not. If we're first in the pipeline, then we don't
            // redirect input. If we're last in the pipeline, we don't redirect output.

            if (this.Command.MyInvocation.PipelinePosition == this.Command.MyInvocation.PipelineLength)
            {
                // If the output pipe is the default outputter, for example, calling the native command from command-line host,
                // then we're possibly running standalone.
                //
                // If the downstream cmdlet is explicitly Out-Default, for example: 
                //    $powershell.AddScript('ipconfig.exe') 
                //    $powershell.AddCommand('Out-Default') 
                //    $powershell.Invoke())
                // we should not count it as a redirection.
                if (IsDownstreamOutDefault(this.commandRuntime.OutputPipe))
                {
                    redirectOutput = false;
                }
            }


            // See if the error output stream has been redirected, either through an explicit 2> foo.txt or
            // my merging error into output through 2>&1.
            if (CommandRuntime.ErrorMergeTo != MshCommandRuntime.MergeDataStream.Output)
            {
                // If the error output pipe is the default outputter, for example, calling the native command from command-line host,
                // then we're possibly running standalone.
                //
                // If the downstream cmdlet is explicitly Out-Default, for example: 
                //    $powershell.AddScript('ipconfig.exe') 
                //    $powershell.AddCommand('Out-Default') 
                //    $powershell.Invoke())
                // we should not count that as a redirection.
                if (IsDownstreamOutDefault(this.commandRuntime.ErrorOutputPipe))
                {
                    redirectError = false;
                }
            }

            //In minishell scenario, if output is redirected 
            //then error should also be redirected.
            if (redirectError == false && redirectOutput == true && _isMiniShell)
            {
                redirectError = true;
            }

            if (_inputWriter.Count == 0 && (!this.Command.MyInvocation.ExpectingInput))
                redirectInput = false;

            // Remoting server consideration.
            // Currently, the WinRM is using std io pipes to communicate with PowerShell server.
            // To protect these std io pipes from access from user command, we have replaced the original std io pipes with null pipes.
            // The original std io pipes are taken private, to be used by remoting infrastructure only.
            // Doing so prevents user data to corrupt PowerShell remoting communication data which are encoded in a 
            // special format.
            // In the following, we check for this server condition.
            // If it is the server, then we redirect all std io handles for the native command.

            if (NativeCommandProcessor.IsServerSide)
            {
                redirectInput = true;
                redirectOutput = true;
                redirectError = true;
            }
#if !CORECLR // UI doesn't exist on OneCore, so all applications running on an OneCore client should be console applications.
            // The powershell on the OneCore client should already have a console attached.
            else if (IsConsoleApplication(this.Path))
            {
                // Allocate a console if there isn't one attached already...
                ConsoleVisibility.AllocateHiddenConsole();

                if (ConsoleVisibility.AlwaysCaptureApplicationIO)
                {
                    redirectOutput = true;
                    redirectError = true;
                }
            }
#endif
            if (!(redirectInput || redirectOutput))
                _runStandAlone = true;
        }

        private bool ValidateExtension(string path)
        {
            // Now check the extension and see if it's one of the ones in pathext
            string myExtension = System.IO.Path.GetExtension(path);

            string pathext = (string)LanguagePrimitives.ConvertTo(
                this.Command.Context.GetVariableValue(SpecialVariables.PathExtVarPath),
                typeof(string), CultureInfo.InvariantCulture);
            string[] extensionList;
            if (String.IsNullOrEmpty(pathext))
            {
                extensionList = new string[] { ".exe", ".com", ".bat", ".cmd" };
            }
            else
            {
                extensionList = pathext.Split(Utils.Separators.Semicolon);
            }
            foreach (string extension in extensionList)
            {
                if (String.Equals(extension, myExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

#if !CORECLR // Shell doesn't exist on OneCore, so documents cannot be associated with applications.
        #region Interop for FindExecutable...

        // Constant used to determine the buffer size for a path
        // when looking for an executable. MAX_PATH is defined as 260
        // so this is much larger than what should be permitted
        private const int MaxExecutablePath = 1024;

        // The FindExecutable API is defined in shellapi.h as
        // SHSTDAPI_(HINSTANCE) FindExecutableW(LPCWSTR lpFile, LPCWSTR lpDirectory, __out_ecount(MAX_PATH) LPWSTR lpResult);
        // HINSTANCE is void* so we need to use IntPtr as API return value.

        [DllImport("shell32.dll", EntryPoint = "FindExecutable")]
        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "0")]
        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "1")]
        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "2")]
        private static extern IntPtr FindExecutableW(
          string fileName, string directoryPath, StringBuilder pathFound);

        [ArchitectureSensitive]
        private static string FindExecutable(string filename)
        {
            // Preallocate a
            StringBuilder objResultBuffer = new StringBuilder(MaxExecutablePath);
            IntPtr resultCode = (IntPtr)0;

            try
            {
                resultCode = FindExecutableW(filename, string.Empty, objResultBuffer);
            }
            catch (System.IndexOutOfRangeException e)
            {
                // If we got an index-out-of-range exception here, it's because
                // of a buffer overrun error so we fail fast instead of
                // continuing to run in an possibly unstable environment....
                WindowsErrorReporting.FailFast(e);
            }

            // If FindExecutable returns a result >= 32, then it succeeded
            // and we return the string that was found, otherwise we
            // return null.
            if ((long)resultCode >= 32)
            {
                return objResultBuffer.ToString();
            }

            return null;
        }

        #endregion

        #region Interop for SHGetFileInfo

        private const int SCS_32BIT_BINARY = 0;  // A 32-bit Windows-based application
        private const int SCS_DOS_BINARY = 1;  // An MS-DOS - based application
        private const int SCS_WOW_BINARY = 2;  // A 16-bit Windows-based application
        private const int SCS_PIF_BINARY = 3;  // A PIF file that executes an MS-DOS - based application
        private const int SCS_POSIX_BINARY = 4;  // A POSIX - based application
        private const int SCS_OS216_BINARY = 5;  // A 16-bit OS/2-based application
        private const int SCS_64BIT_BINARY = 6;  // A 64-bit Windows-based application.

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        private const uint SHGFI_EXETYPE = 0x000002000; // flag used to ask to return exe type

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
            ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        #endregion
#endif

        #region Minishell Interop

        private bool _isMiniShell = false;
        /// <summary>
        /// Returns true if native command being invoked is mini-shell.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// If any of the argument supplied to native command is script block, 
        /// we assume it is minishell.
        /// </remarks>
        private bool IsMiniShell()
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                CommandParameterInternal arg = arguments[i];
                if (!arg.ParameterNameSpecified)
                {
                    if (arg.ArgumentValue is ScriptBlock)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion Minishell Interop

        internal static bool IsServerSide { get; set; }
    }

    /// <summary>
    /// Helper class to handle writing input to a process.
    /// </summary>
    internal class ProcessInputWriter
    {
        #region constructor 

        private InternalCommand _command;
        /// <summary>
        /// Creates an instance of ProcessInputWriter 
        /// </summary>
        internal ProcessInputWriter(InternalCommand command)
        {
            Dbg.Assert(command != null, "Caller should validate the parameter");
            _command = command;
        }

        #endregion constructor

        /// <summary>
        /// Input is collected in this list
        /// </summary>
        private ArrayList _inputList = new ArrayList();
        /// <summary>
        /// Add an object to write to process
        /// </summary>
        /// <param name="input"></param>
        internal void Add(object input)
        {
            _inputList.Add(input);
        }

        /// <summary>
        /// Count of object in inputlist
        /// </summary>
        internal int Count
        {
            get
            {
                return _inputList.Count;
            }
        }

        /// <summary>
        /// Stream to which input is written
        /// </summary>
        private StreamWriter _streamWriter;

        /// <summary>
        /// Format of input.
        /// </summary>
        private NativeCommandIOFormat _inputFormat;

        /// <summary>
        /// Thread which writes the input
        /// </summary>
        private Thread _inputThread;

        /// <summary>
        /// Start writing input to process
        /// </summary>
        /// <param name="process">
        /// process to which input is written
        /// </param>
        /// <param name="inputFormat">
        /// </param>
        internal void Start(Process process, NativeCommandIOFormat inputFormat)
        {
            Dbg.Assert(process != null, "caller should validate the parameter");

            //Get the encoding for writing to native command. Note we get the Encoding 
            //from the current scope so a script or function can use a different encoding
            //than global value.
            Encoding pipeEncoding = _command.Context.GetVariableValue(SpecialVariables.OutputEncodingVarPath) as System.Text.Encoding ??
                                    Encoding.ASCII;

            _streamWriter = new StreamWriter(process.StandardInput.BaseStream,
                                            pipeEncoding);
            _inputFormat = inputFormat;

            if (inputFormat == NativeCommandIOFormat.Text)
            {
                ConvertToString();
            }
            _inputThread = new Thread(new ThreadStart(this.WriterThreadProc));
            _inputThread.Start();
        }

        private bool _stopping = false;
        /// <summary>
        /// Stop writing input to process
        /// </summary>
        internal void Stop()
        {
            _stopping = true;
        }

        /// <summary>
        /// This method wait for writer thread to finish.
        /// </summary>
        internal void Done()
        {
            if (_inputThread != null)
            {
                _inputThread.Join();
            }
        }

        /// <summary>
        /// Thread procedure for writing data to the child process...
        /// </summary>
        private void WriterThreadProc()
        {
            try
            {
                if (_inputFormat == NativeCommandIOFormat.Text)
                {
                    WriteTextInput();
                }
                else
                {
                    WriteXmlInput();
                }
            }
            catch (System.IO.IOException)
            {
            }
        }

        private void WriteTextInput()
        {
            try
            {
                foreach (object o in _inputList)
                {
                    if (_stopping) return;

                    string line = PSObject.ToStringParser(_command.Context, o);
                    _streamWriter.Write(line);
                }
            }
            finally
            {
                _streamWriter.Dispose();
            }
        }

        private void WriteXmlInput()
        {
            try
            {
                //Write header
                _streamWriter.WriteLine("#< CLIXML");

                // When (if) switching to XmlTextWriter.Create remember the OmitXmlDeclaration difference
                XmlWriter writer = XmlWriter.Create(_streamWriter);
                Serializer ser = new Serializer(writer);
                foreach (object o in _inputList)
                {
                    if (_stopping) return;
                    ser.Serialize(o);
                }
                ser.Done();
            }
            finally
            {
                _streamWriter.Dispose();
            }
        }

        /// <summary>
        /// Formats the input objects using out-string. Output of out-string
        /// is given as input to native command processor.
        /// This method is to be called from the pipeline thread and not from the
        /// thread which writes input in to process.
        /// </summary>
        private void ConvertToString()
        {
            Dbg.Assert(_inputFormat == NativeCommandIOFormat.Text, "InputFormat should be Text");

            PipelineProcessor p = new PipelineProcessor();
            p.Add(_command.Context.CreateCommand("out-string", false));
            object[] result = (object[])p.SynchronousExecuteEnumerate(_inputList.ToArray());
            _inputList = new ArrayList(result);
        }
    }

    /// <summary>
    /// This helper class reads the output from error and output streams of
    /// process.
    /// </summary>
    internal class ProcessOutputReader
    {
        #region constructor

        /// <summary>
        /// Process whose output is to be read.
        /// </summary>
        private Process _process;

        /// <summary>
        /// Path of the process application
        /// </summary>
        private string _processPath;

        private bool _redirectOutput;

        private bool _redirectError;

        /// <summary>
        /// Process whose output is read
        /// </summary>
        internal ProcessOutputReader(Process process, string processPath, bool redirectOutput, bool redirectError)
        {
            Dbg.Assert(process != null, "caller should validate the parameter");
            Dbg.Assert(processPath != null, "caller should validate the parameter");
            Dbg.Assert(redirectOutput || redirectError, "Either redirectOutput or redirectError must be true");
            _process = process;
            _processPath = processPath;
            _redirectOutput = redirectOutput;
            _redirectError = redirectError;
        }

        #endregion constructor

        /// <summary>
        /// Reader for output stream of process
        /// </summary>
        private ProcessStreamReader _outputReader;
        /// <summary>
        /// Reader for error stream of process
        /// </summary>
        private ProcessStreamReader _errorReader;
        /// <summary>
        /// Synchronized object queue in which object read form output and 
        /// error streams are deposited
        /// </summary>
        private ObjectStream _processOutput;

        /// <summary>
        /// Start reading the output/error. Note all the work is done asynchronously.
        /// </summary>
        internal void Start()
        {
            _processOutput = new ObjectStream(128);

            // Start async reading of error and output
            // readercount variable is used by multiple threads to close "processOutput" ObjectStream.
            // Without properly initializing the readercount, the ObjectStream might get
            // closed early. readerCount is protected here by using the lock.
            lock (_readerLock)
            {
                if (_redirectOutput)
                {
                    _readerCount++;
                    _outputReader = new ProcessStreamReader(_process.StandardOutput, _processPath, true, _processOutput.ObjectWriter, this);
                    _outputReader.Start();
                }
                if (_redirectError)
                {
                    _readerCount++;
                    _errorReader = new ProcessStreamReader(_process.StandardError, _processPath, false, _processOutput.ObjectWriter, this);
                    _errorReader.Start();
                }
            }
        }

        /// <summary>
        /// Stops reading from streams. This is called from NativeCommandProcessor's StopProcessing
        /// method. Note return of this method doesn't mean reading has stopped and all threads are
        /// done. 
        /// Use Done to ensure that all reading threads are finished.
        /// </summary>
        internal void Stop()
        {
            if (_processOutput != null)
            {
                try
                {
                    //Close the reader for the stream.
                    _processOutput.ObjectReader.Close();
                }
                catch (Exception e) // ignore non-severe exceptions
                {
                    CommandProcessorBase.CheckForSevereException(e);
                }

                try
                {
                    _processOutput.Close();
                }
                catch (Exception e) // ignore non-severe exceptions
                {
                    CommandProcessorBase.CheckForSevereException(e);
                }
            }
        }

        /// <summary>
        /// This method returns when all output reader threads have returned
        /// </summary>
        internal void Done()
        {
            if (_outputReader != null)
            {
                _outputReader.Done();
            }
            if (_errorReader != null)
            {
                _errorReader.Done();
            }
        }

        /// <summary>
        /// Return one object which was read from the process.
        /// </summary>
        /// <returns>
        /// AutomationNull.Value if no more objects.
        /// object of type ProcessOutputObject otherwise
        /// </returns>
        internal object Read()
        {
            return _processOutput.ObjectReader.Read();
        }

        /// <summary>
        /// object used for synchronizing ReaderDone call between two readers
        /// </summary>
        private object _readerLock = new object();

        /// <summary>
        /// Count of readers - this is set by Start. If both output and error
        /// are redirected, it will be 2. If only one is redirected, it'll be 1.
        /// </summary>
        private int _readerCount;

        /// <summary>
        /// This method is called by output or error reader when they are
        /// done reading. When it is called two times, we close the writer.
        /// </summary>
        /// <param name="isOutput"></param>
        internal void ReaderDone(bool isOutput)
        {
            int temp;
            lock (_readerLock)
            {
                temp = --_readerCount;
            }
            if (temp == 0)
            {
                _processOutput.ObjectWriter.Close();
            }
        }
    }

    /// <summary>
    /// This class reads the string from output or error streams of process
    /// and processes them appropriately.
    /// </summary>
    /// <remarks>
    /// This class is not thread safe. It is assumed that NativeCommandProcessor
    /// class will synchronize access to this class between different threads.
    /// </remarks>
    internal class ProcessStreamReader
    {
        #region constructor 

        /// <summary>
        /// Stream from which data is read.
        /// </summary>
        private StreamReader _streamReader;

        /// <summary>
        /// Flag which tells if streamReader is for stdout or stderr stream of process
        /// </summary>
        private bool _isOutput;

        /// <summary>
        /// Writer to which data read from stream are written
        /// </summary>
        private PipelineWriter _writer;

        /// <summary>
        /// Path to the process. This is used for setting the name of the thread.
        /// </summary>
        private string _processPath;

        /// <summary>
        /// ProcessReader which owns this stream reader
        /// </summary>
        private ProcessOutputReader _processOutputReader;

        /// <summary>
        /// Creates an instance of ProcessStreamReader
        /// </summary>
        /// <param name="streamReader">
        /// Stream from which data is read
        /// </param>
        /// <param name="processPath">
        /// Path to the process. This is used for setting the name of the thread.
        /// </param>
        /// <param name="isOutput">
        /// if true stream is output stream of process 
        /// else stream is error stream. 
        /// </param>
        /// <param name="writer">
        /// Processed data is written to it
        /// </param>
        /// <param name="processOutputReader">
        /// ProcessOutputReader which owns this stream reader
        /// </param>
        internal ProcessStreamReader(StreamReader streamReader, string processPath, bool isOutput,
            PipelineWriter writer, ProcessOutputReader processOutputReader)
        {
            Dbg.Assert(streamReader != null, "Caller should validate the parameter");
            Dbg.Assert(processPath != null, "Caller should validate the parameter");
            Dbg.Assert(writer != null, "Caller should validate the parameter");
            Dbg.Assert(processOutputReader != null, "Caller should validate the parameter");

            _streamReader = streamReader;
            _processPath = processPath;
            _isOutput = isOutput;
            _writer = writer;
            _processOutputReader = processOutputReader;
        }

        #endregion constructor

        /// <summary>
        /// Thread on which reading happens
        /// </summary>
        private Thread _thread = null;

        /// <summary>
        /// Launches a new thread to start reading.
        /// </summary>
        internal void Start()
        {
            _thread = new Thread(new ThreadStart(ReaderStartProc));
            if (_isOutput)
            {
                _thread.Name = string.Format(CultureInfo.InvariantCulture, "{0} :Output Reader", _processPath);
            }
            else
            {
                _thread.Name = string.Format(CultureInfo.InvariantCulture, "{0} :Error Reader", _processPath);
            }
            _thread.Start();
        }

        /// <summary>
        /// This method returns when reader thread has returned.
        /// </summary>
        internal void Done()
        {
            if (_thread != null)
            {
                _thread.Join();
            }
        }

        /// <summary>
        /// Thread proc for reading
        /// </summary>
        private void ReaderStartProc()
        {
            try
            {
                ReaderStartProcHelper();
            }
            catch (Exception ex)
            {
                CommandProcessorBase.CheckForSevereException(ex);
            }
            finally
            {
                _processOutputReader.ReaderDone(_isOutput);
            }
        }

        private void ReaderStartProcHelper()
        {
            //read the first line to detect the format.
            //for xml, first line is #< CLIXML
            string line = _streamReader.ReadLine();
            if (line == null)
            {
                // nothing to do
            }
            else if (line.Equals("#< CLIXML", StringComparison.Ordinal) == false)
            {
                ReadText(line);
            }
            else
            {
                ReadXml();
            }
        }

        private void ReadText(string line)
        {
            if (_isOutput)
            {
                while (line != null)
                {
                    AddObjectToWriter(line, MinishellStream.Output);
                    line = _streamReader.ReadLine();
                }
            }
            else
            {
                //
                // Produce a regular error record for the first line of the output
                //
                ErrorRecord errorRecord = new ErrorRecord(new RemoteException(line),
                                        "NativeCommandError", ErrorCategory.NotSpecified, line);
                AddObjectToWriter(errorRecord, MinishellStream.Error);

                //
                // Wrap the rest of the output in ErrorRecords with the "NativeCommandErrorMessage" error ID
                //
                while ((line = _streamReader.ReadLine()) != null)
                {
                    AddObjectToWriter(
                        new ErrorRecord(
                            new RemoteException(line),
                            "NativeCommandErrorMessage",
                            ErrorCategory.NotSpecified,
                            null),
                        MinishellStream.Error);
                }
            }
        }

        private void ReadXml()
        {
            try
            {
                XmlReader xmlReader = XmlReader.Create(_streamReader, InternalDeserializer.XmlReaderSettingsForCliXml);
                Deserializer des = new Deserializer(xmlReader);
                while (!des.Done())
                {
                    string streamName;
                    object obj = des.Deserialize(out streamName);

                    //Decide the stream to which data belongs
                    MinishellStream stream = MinishellStream.Unknown;
                    if (streamName != null)
                    {
                        stream = StringToMinishellStreamConverter.ToMinishellStream(streamName);
                    }
                    if (stream == MinishellStream.Unknown)
                    {
                        stream = _isOutput ? MinishellStream.Output : MinishellStream.Error;
                    }

                    //Null is allowed only in output stream
                    if (stream != MinishellStream.Output && obj == null)
                    {
                        continue;
                    }

                    if (stream == MinishellStream.Error)
                    {
                        if (obj is PSObject)
                        {
                            obj = ErrorRecord.FromPSObjectForRemoting(PSObject.AsPSObject(obj));
                        }
                        else
                        {
                            string errorMessage = null;
                            try
                            {
                                errorMessage = (string)LanguagePrimitives.ConvertTo(obj, typeof(string), CultureInfo.InvariantCulture);
                            }
                            catch (PSInvalidCastException)
                            {
                                continue;
                            }
                            obj = new ErrorRecord(new RemoteException(errorMessage),
                                                "NativeCommandError", ErrorCategory.NotSpecified, errorMessage);
                        }
                    }
                    else if (stream == MinishellStream.Information)
                    {
                        if (obj is PSObject)
                        {
                            obj = InformationRecord.FromPSObjectForRemoting(PSObject.AsPSObject(obj));
                        }
                        else
                        {
                            string messageData = null;
                            try
                            {
                                messageData = (string)LanguagePrimitives.ConvertTo(obj, typeof(string), CultureInfo.InvariantCulture);
                            }
                            catch (PSInvalidCastException)
                            {
                                continue;
                            }

                            obj = new InformationRecord(messageData, null);
                        }
                    }
                    else if (stream == MinishellStream.Debug ||
                             stream == MinishellStream.Verbose ||
                             stream == MinishellStream.Warning)
                    {
                        //Convert to string
                        try
                        {
                            obj = LanguagePrimitives.ConvertTo(obj, typeof(string), CultureInfo.InvariantCulture);
                        }
                        catch (PSInvalidCastException)
                        {
                            continue;
                        }
                    }
                    AddObjectToWriter(obj, stream);
                }
            }
            catch (XmlException originalException)
            {
                string template = NativeCP.CliXmlError;
                string message = string.Format(
                    null,
                    template,
                    _isOutput ? MinishellStream.Output : MinishellStream.Error,
                    _processPath,
                    originalException.Message);
                XmlException newException = new XmlException(
                    message,
                    originalException);

                ErrorRecord error = new ErrorRecord(
                    newException,
                    "ProcessStreamReader_CliXmlError",
                    ErrorCategory.SyntaxError,
                    _processPath);
                AddObjectToWriter(error, MinishellStream.Error);
            }
        }

        /// <summary>
        /// Adds one object to writer
        /// </summary>
        private void AddObjectToWriter(object data, MinishellStream stream)
        {
            try
            {
                ProcessOutputObject dataObject = new ProcessOutputObject(data, stream);
                //writer is shared between Error and Output reader.
                lock (_writer)
                {
                    _writer.Write(dataObject);
                }
            }
            catch (PipelineClosedException)
            {
                // The output queue may have been closed asynchronously
                ;
            }
            catch (System.ObjectDisposedException)
            {
                // The output queue may have been disposed asynchronously when StopProcessing is called...
                ;
            }
        }
    }

#if !CORECLR // There is no GUI application on OneCore, so powershell on OneCore should always have a console attached.

    /// <summary>
    /// Static class that allows you to show and hide the console window
    /// associated with this process.
    /// </summary>
    internal static class ConsoleVisibility
    {
        /// <summary>
        /// If set to true, then native commands will always be run redirected...
        /// </summary>
        public static bool AlwaysCaptureApplicationIO { get; set; }

        [DllImport("Kernel32.dll")]
        internal static extern IntPtr GetConsoleWindow();

        internal const int SW_HIDE = 0;
        internal const int SW_SHOWNORMAL = 1;
        internal const int SW_NORMAL = 1;
        internal const int SW_SHOWMINIMIZED = 2;
        internal const int SW_SHOWMAXIMIZED = 3;
        internal const int SW_MAXIMIZE = 3;
        internal const int SW_SHOWNOACTIVATE = 4;
        internal const int SW_SHOW = 5;
        internal const int SW_MINIMIZE = 6;
        internal const int SW_SHOWMINNOACTIVE = 7;
        internal const int SW_SHOWNA = 8;
        internal const int SW_RESTORE = 9;
        internal const int SW_SHOWDEFAULT = 10;
        internal const int SW_FORCEMINIMIZE = 11;
        internal const int SW_MAX = 11;

        /// <summary>
        /// Code to control the display properties of the a window...
        /// </summary>
        /// <param name="hWnd">The window to show...</param>
        /// <param name="nCmdShow">The command to do</param>
        /// <returns>true it it was successful</returns>
        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, Int32 nCmdShow);

        /// <summary>
        /// Code to allocate a console...
        /// </summary>
        /// <returns>true if a console was created...</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool AllocConsole();


        /// <summary>
        /// Called to save the foreground window before allocating a hidden console window
        /// </summary>
        /// <returns>A handle to the foreground window</returns>
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Called to restore the foreground window after allocating a hidden console window
        /// </summary>
        /// <param name="hWnd">A handle to the window that should be activated and brought to the foreground.</param>
        /// <returns>true if the window was brought to the foreground</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// If no console window is attached to this process, then allocate one,
        /// hide it and return true. If there's already a console window attached, then
        /// just return false.
        /// </summary>
        /// <returns></returns>
        internal static bool AllocateHiddenConsole()
        {
            // See if there is already a console attached.
            IntPtr hwnd = ConsoleVisibility.GetConsoleWindow();
            if (hwnd != IntPtr.Zero)
            {
                return false;
            }

            // save the foreground window since allocating a console window might remove focus from it
            IntPtr savedForeground = ConsoleVisibility.GetForegroundWindow();

            // Since there is no console window, allocate and then hide it...
            // Suppress the PreFAST warning about not using Marshal.GetLastWin32Error() to
            // get the error code.
#pragma warning disable 56523
            ConsoleVisibility.AllocConsole();
            hwnd = ConsoleVisibility.GetConsoleWindow();

            bool returnValue;
            if (hwnd == IntPtr.Zero)
            {
                returnValue = false;
            }
            else
            {
                returnValue = true;
                ConsoleVisibility.ShowWindow(hwnd, ConsoleVisibility.SW_HIDE);
                AlwaysCaptureApplicationIO = true;
            }

            if (savedForeground != IntPtr.Zero && ConsoleVisibility.GetForegroundWindow() != savedForeground)
            {
                ConsoleVisibility.SetForegroundWindow(savedForeground);
            }

            return returnValue;
        }

        /// <summary>
        /// If there is a console attached, then make it visible
        /// and allow interactive console applications to be run.
        /// </summary>
        public static void Show()
        {
            IntPtr hwnd = GetConsoleWindow();
            if (hwnd != IntPtr.Zero)
            {
                ShowWindow(hwnd, SW_SHOW);
                AlwaysCaptureApplicationIO = false;
            }
            else
            {
                throw PSTraceSource.NewInvalidOperationException();
            }
        }

        /// <summary>
        /// If there is a console attached, then hide it and always capture
        /// output from the child process.
        /// </summary>
        public static void Hide()
        {
            IntPtr hwnd = GetConsoleWindow();
            if (hwnd != IntPtr.Zero)
            {
                ShowWindow(hwnd, SW_HIDE);
                AlwaysCaptureApplicationIO = true;
            }
            else
            {
                throw PSTraceSource.NewInvalidOperationException();
            }
        }
    }
#endif

    /// <summary>
    /// Exception used to wrap the error coming from 
    /// remote instance of Msh.
    /// </summary>     
    /// <remarks>
    /// This remote instance of Msh can be in a separate process, 
    /// appdomain or machine.
    /// </remarks>
    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    public class RemoteException : RuntimeException
    {
        /// <summary>
        /// Initializes a new instance of RemoteException
        /// </summary>
        public RemoteException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of RemoteException with a specified error message. 
        /// </summary>
        /// <param name="message">
        /// The message that describes the error. 
        /// </param>
        public RemoteException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the RemoteException class 
        /// with a specified error message and a reference to the inner exception 
        /// that is the cause of this exception. 
        /// </summary>
        /// <param name="message">
        /// The message that describes the error.         
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception.
        /// </param>
        public RemoteException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the RemoteException 
        /// with a specified error message, serialized Exception and 
        /// serialized InvocationInfo
        /// </summary>
        /// <param name="message">The message that describes the error. </param>
        /// <param name="serializedRemoteException">
        /// serialized exception from remote msh
        /// </param>
        /// <param name="serializedRemoteInvocationInfo">
        /// serialized invocation info from remote msh
        /// </param>
        internal RemoteException
        (
            string message,
            PSObject serializedRemoteException,
            PSObject serializedRemoteInvocationInfo
        )
            : base(message)
        {
            _serializedRemoteException = serializedRemoteException;
            _serializedRemoteInvocationInfo = serializedRemoteInvocationInfo;
        }


        #region ISerializable Members

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteException"/>
        ///  class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> that holds the serialized object 
        /// data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="StreamingContext"/> that contains contextual information 
        /// about the source or destination.
        /// </param>
        protected RemoteException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion

        [NonSerialized]
        private PSObject _serializedRemoteException;
        [NonSerialized]
        private PSObject _serializedRemoteInvocationInfo;

        /// <summary>
        /// Original Serialized Exception from remote msh
        /// </summary>
        /// <remarks>This is the exception which was thrown in remote.
        /// </remarks>
        public PSObject SerializedRemoteException
        {
            get
            {
                return _serializedRemoteException;
            }
        }

        /// <summary>
        /// InvocationInfo, if any, associated with the SerializedRemoteException.
        /// </summary>
        /// <remarks>
        /// This is the serialized InvocationInfo from the remote msh.
        /// </remarks>
        public PSObject SerializedRemoteInvocationInfo
        {
            get
            {
                return _serializedRemoteInvocationInfo;
            }
        }

        private ErrorRecord _remoteErrorRecord;
        /// <summary>
        /// Sets the remote error record associated with this exception
        /// </summary>
        /// <param name="remoteError"></param>
        internal void SetRemoteErrorRecord(ErrorRecord remoteError)
        {
            _remoteErrorRecord = remoteError;
        }

        /// <summary>
        /// ErrorRecord associated with the exception
        /// </summary>
        public override ErrorRecord ErrorRecord
        {
            get
            {
                if (_remoteErrorRecord != null)
                    return _remoteErrorRecord;

                return base.ErrorRecord;
            }
        }
    }
}
