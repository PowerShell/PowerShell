// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Microsoft.PowerShell.Commands.ShowCommandExtension;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Show-Command displays a GUI for a cmdlet, or for all cmdlets if no specific cmdlet is specified.
    /// </summary>
    [Cmdlet(VerbsCommon.Show, "Command", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2109589")]
    public class ShowCommandCommand : PSCmdlet, IDisposable
    {
        #region Private Fields
        /// <summary>
        /// Set to true when ProcessRecord is reached, since it will always open a window.
        /// </summary>
        private bool _hasOpenedWindow;

        /// <summary>
        /// Determines if the command should be sent to the pipeline as a string instead of run.
        /// </summary>
        private bool _passThrough;

        /// <summary>
        /// Uses ShowCommandProxy to invoke WPF GUI object.
        /// </summary>
        private ShowCommandProxy _showCommandProxy;

        /// <summary>
        /// Data container for all cmdlets. This is populated when show-command is called with no command name.
        /// </summary>
        private List<ShowCommandCommandInfo> _commands;

        /// <summary>
        /// List of modules that have been loaded indexed by module name.
        /// </summary>
        private Dictionary<string, ShowCommandModuleInfo> _importedModules;

        /// <summary>
        /// Record the EndProcessing error.
        /// </summary>
        private PSDataCollection<ErrorRecord> _errors = new();

        /// <summary>
        /// Field used for the NoCommonParameter parameter.
        /// </summary>
        private SwitchParameter _noCommonParameter;

        /// <summary>
        /// Object used for ShowCommand with a command name that holds the view model created for the command.
        /// </summary>
        private object _commandViewModelObj;
        #endregion

        #region Input Cmdlet Parameter
        /// <summary>
        /// Gets or sets the command name.
        /// </summary>
        [Parameter(Position = 0)]
        [Alias("CommandName")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Width.
        /// </summary>
        [Parameter]
        [ValidateRange(300, int.MaxValue)]
        public double Height { get; set; }

        /// <summary>
        /// Gets or sets the Width.
        /// </summary>
        [Parameter]
        [ValidateRange(300, int.MaxValue)]
        public double Width { get; set; }

        /// <summary>
        /// Gets or sets a value indicating Common Parameters should not be displayed.
        /// </summary>
        [Parameter]
        public SwitchParameter NoCommonParameter
        {
            get { return _noCommonParameter; }

            set { _noCommonParameter = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating errors should not cause a message window to be displayed.
        /// </summary>
        [Parameter]
        public SwitchParameter ErrorPopup { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the command should be sent to the pipeline as a string instead of run.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return _passThrough; }

            set { _passThrough = value; }
        }
        #endregion

        #region Public and Protected Methods
        /// <summary>
        /// Executes a PowerShell script, writing the output objects to the pipeline.
        /// </summary>
        /// <param name="script">Script to execute.</param>
        public void RunScript(string script)
        {
            if (_showCommandProxy == null || string.IsNullOrEmpty(script))
            {
                return;
            }

            if (_passThrough)
            {
                this.WriteObject(script);
                return;
            }

            if (ErrorPopup)
            {
                this.RunScriptSilentlyAndWithErrorHookup(script);
                return;
            }

            if (_showCommandProxy.HasHostWindow)
            {
                if (!_showCommandProxy.SetPendingISECommand(script))
                {
                    this.RunScriptSilentlyAndWithErrorHookup(script);
                }

                return;
            }

            // Don't send newline at end as PSReadLine shows it rather than executing
            if (!ConsoleInputWithNativeMethods.AddToConsoleInputBuffer(script, newLine: false))
            {
                this.WriteDebug(FormatAndOut_out_gridview.CannotWriteToConsoleInputBuffer);
                this.RunScriptSilentlyAndWithErrorHookup(script);
            }
        }

        /// <summary>
        /// Dispose method in IDisposable.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initialize a proxy instance for show-command.
        /// </summary>
        protected override void BeginProcessing()
        {
            _showCommandProxy = new ShowCommandProxy(this);

            if (_showCommandProxy.ScreenHeight < this.Height)
            {
                ErrorRecord error = new(
                                    new NotSupportedException(string.Format(CultureInfo.CurrentUICulture, FormatAndOut_out_gridview.PropertyValidate, "Height", _showCommandProxy.ScreenHeight)),
                                    "PARAMETER_DATA_ERROR",
                                    ErrorCategory.InvalidData,
                                    null);
                this.ThrowTerminatingError(error);
            }

            if (_showCommandProxy.ScreenWidth < this.Width)
            {
                ErrorRecord error = new(
                                    new NotSupportedException(string.Format(CultureInfo.CurrentUICulture, FormatAndOut_out_gridview.PropertyValidate, "Width", _showCommandProxy.ScreenWidth)),
                                    "PARAMETER_DATA_ERROR",
                                    ErrorCategory.InvalidData,
                                    null);
                this.ThrowTerminatingError(error);
            }
        }

        /// <summary>
        /// ProcessRecord with or without CommandName.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (Name == null)
            {
                _hasOpenedWindow = this.CanProcessRecordForAllCommands();
            }
            else
            {
                _hasOpenedWindow = this.CanProcessRecordForOneCommand();
            }
        }

        /// <summary>
        /// Optionally displays errors in a message.
        /// </summary>
        protected override void EndProcessing()
        {
            if (!_hasOpenedWindow)
            {
                return;
            }

            // We wait until the window is loaded and then activate it
            // to work around the console window gaining activation somewhere
            // in the end of ProcessRecord, which causes the keyboard focus
            // (and use oif tab key to focus controls) to go away from the window
            _showCommandProxy.WindowLoaded.WaitOne();
            _showCommandProxy.ActivateWindow();

            this.WaitForWindowClosedOrHelpNeeded();
            this.RunScript(_showCommandProxy.GetScript());

            if (_errors.Count == 0 || !ErrorPopup)
            {
                return;
            }

            StringBuilder errorString = new();

            for (int i = 0; i < _errors.Count; i++)
            {
                if (i != 0)
                {
                    errorString.AppendLine();
                }

                ErrorRecord error = _errors[i];
                errorString.Append(error.Exception.Message);
            }

            _showCommandProxy.ShowErrorString(errorString.ToString());
        }

        /// <summary>
        /// StopProcessing is called close the window when user press Ctrl+C in the command prompt.
        /// </summary>
        protected override void StopProcessing()
        {
            _showCommandProxy.CloseWindow();
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Runs the script in a new PowerShell instance and hooks up error stream to potentially display error popup.
        /// This method has the inconvenience of not showing to the console user the script being executed.
        /// </summary>
        /// <param name="script">Script to be run.</param>
        private void RunScriptSilentlyAndWithErrorHookup(string script)
        {
            // errors are not created here, because there is a field for it used in the final pop up
            PSDataCollection<object> output = new();

            output.DataAdded += this.Output_DataAdded;
            _errors.DataAdded += this.Error_DataAdded;

            System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace);
            ps.Streams.Error = _errors;

            ps.Commands.AddScript(script);

            ps.Invoke(null, output, null);
        }

        /// <summary>
        /// Issues an error when this.commandName was not found.
        /// </summary>
        private void IssueErrorForNoCommand()
        {
            InvalidOperationException errorException = new(
                string.Format(
                    CultureInfo.CurrentUICulture,
                    FormatAndOut_out_gridview.CommandNotFound,
                    Name));
            this.ThrowTerminatingError(new ErrorRecord(errorException, "NoCommand", ErrorCategory.InvalidOperation, Name));
        }

        /// <summary>
        /// Issues an error when there is more than one command matching this.commandName.
        /// </summary>
        private void IssueErrorForMoreThanOneCommand()
        {
            InvalidOperationException errorException = new(
                string.Format(
                    CultureInfo.CurrentUICulture,
                    FormatAndOut_out_gridview.MoreThanOneCommand,
                    Name,
                    "Show-Command"));
            this.ThrowTerminatingError(new ErrorRecord(errorException, "MoreThanOneCommand", ErrorCategory.InvalidOperation, Name));
        }

        /// <summary>
        /// Called from CommandProcessRecord to run the command that will get the CommandInfo and list of modules.
        /// </summary>
        /// <param name="command">Command to be retrieved.</param>
        /// <param name="modules">List of loaded modules.</param>
        private void GetCommandInfoAndModules(out CommandInfo command, out Dictionary<string, ShowCommandModuleInfo> modules)
        {
            command = null;
            modules = null;
            string commandText = _showCommandProxy.GetShowCommandCommand(Name, true);

            Collection<PSObject> commandResults = this.InvokeCommand.InvokeScript(commandText);

            object[] commandObjects = (object[])commandResults[0].BaseObject;
            object[] moduleObjects = (object[])commandResults[1].BaseObject;
            if (commandResults == null || moduleObjects == null || commandObjects.Length == 0)
            {
                this.IssueErrorForNoCommand();
                return;
            }

            if (commandObjects.Length > 1)
            {
                this.IssueErrorForMoreThanOneCommand();
            }

            command = ((PSObject)commandObjects[0]).BaseObject as CommandInfo;
            if (command == null)
            {
                this.IssueErrorForNoCommand();
                return;
            }

            if (command.CommandType == CommandTypes.Alias)
            {
                commandText = _showCommandProxy.GetShowCommandCommand(command.Definition, false);
                commandResults = this.InvokeCommand.InvokeScript(commandText);
                if (commandResults == null || commandResults.Count != 1)
                {
                    this.IssueErrorForNoCommand();
                    return;
                }

                command = (CommandInfo)commandResults[0].BaseObject;
            }

            modules = _showCommandProxy.GetImportedModulesDictionary(moduleObjects);
        }

        /// <summary>
        /// ProcessRecord when a command name is specified.
        /// </summary>
        /// <returns>True if there was no exception processing this record.</returns>
        private bool CanProcessRecordForOneCommand()
        {
            CommandInfo commandInfo;
            this.GetCommandInfoAndModules(out commandInfo, out _importedModules);
            Diagnostics.Assert(commandInfo != null, "GetCommandInfoAndModules would throw a terminating error/exception");

            try
            {
                _commandViewModelObj = _showCommandProxy.GetCommandViewModel(new ShowCommandCommandInfo(commandInfo), _noCommonParameter.ToBool(), _importedModules, this.Name.Contains('\\'));
                _showCommandProxy.ShowCommandWindow(_commandViewModelObj, _passThrough);
            }
            catch (TargetInvocationException ti)
            {
                this.WriteError(new ErrorRecord(ti.InnerException, "CannotProcessRecordForOneCommand", ErrorCategory.InvalidOperation, Name));
                return false;
            }

            return true;
        }

        /// <summary>
        /// ProcessRecord when a command name is not specified.
        /// </summary>
        /// <returns>True if there was no exception processing this record.</returns>
        private bool CanProcessRecordForAllCommands()
        {
            Collection<PSObject> rawCommands = this.InvokeCommand.InvokeScript(_showCommandProxy.GetShowAllModulesCommand());

            _commands = _showCommandProxy.GetCommandList((object[])rawCommands[0].BaseObject);
            _importedModules = _showCommandProxy.GetImportedModulesDictionary((object[])rawCommands[1].BaseObject);

            try
            {
                _showCommandProxy.ShowAllModulesWindow(_importedModules, _commands, _noCommonParameter.ToBool(), _passThrough);
            }
            catch (TargetInvocationException ti)
            {
                this.WriteError(new ErrorRecord(ti.InnerException, "CannotProcessRecordForAllCommands", ErrorCategory.InvalidOperation, Name));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Waits until the window has been closed answering HelpNeeded events.
        /// </summary>
        private void WaitForWindowClosedOrHelpNeeded()
        {
            while (true)
            {
                int which = WaitHandle.WaitAny(new WaitHandle[] { _showCommandProxy.WindowClosed, _showCommandProxy.HelpNeeded, _showCommandProxy.ImportModuleNeeded });

                if (which == 0)
                {
                    break;
                }

                if (which == 1)
                {
                    Collection<PSObject> helpResults = this.InvokeCommand.InvokeScript(_showCommandProxy.GetHelpCommand(_showCommandProxy.CommandNeedingHelp));
                    _showCommandProxy.DisplayHelp(helpResults);
                    continue;
                }

                Diagnostics.Assert(which == 2, "which is 0,1 or 2 and 0 and 1 have been eliminated in the ifs above");
                string commandToRun = _showCommandProxy.GetImportModuleCommand(_showCommandProxy.ParentModuleNeedingImportModule);
                Collection<PSObject> rawCommands;
                try
                {
                    rawCommands = this.InvokeCommand.InvokeScript(commandToRun);
                }
                catch (RuntimeException e)
                {
                    _showCommandProxy.ImportModuleFailed(e);
                    continue;
                }

                _commands = _showCommandProxy.GetCommandList((object[])rawCommands[0].BaseObject);
                _importedModules = _showCommandProxy.GetImportedModulesDictionary((object[])rawCommands[1].BaseObject);
                _showCommandProxy.ImportModuleDone(_importedModules, _commands);
                continue;
            }
        }

        /// <summary>
        /// Writes the output of a script being run into the pipeline.
        /// </summary>
        /// <param name="sender">Output collection.</param>
        /// <param name="e">Output event.</param>
        private void Output_DataAdded(object sender, DataAddedEventArgs e)
        {
            this.WriteObject(((PSDataCollection<object>)sender)[e.Index]);
        }

        /// <summary>
        /// Writes the errors of a script being run into the pipeline.
        /// </summary>
        /// <param name="sender">Error collection.</param>
        /// <param name="e">Error event.</param>
        private void Error_DataAdded(object sender, DataAddedEventArgs e)
        {
            this.WriteError(((PSDataCollection<ErrorRecord>)sender)[e.Index]);
        }

        /// <summary>
        /// Implements IDisposable logic.
        /// </summary>
        /// <param name="isDisposing">True if being called from Dispose.</param>
        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (_errors != null)
                {
                    _errors.Dispose();
                    _errors = null;
                }
            }
        }
        #endregion

        /// <summary>
        /// Wraps interop code for console input buffer.
        /// </summary>
        internal static class ConsoleInputWithNativeMethods
        {
            /// <summary>
            /// Constant used in calls to GetStdHandle.
            /// </summary>
            internal const int STD_INPUT_HANDLE = -10;

            /// <summary>
            /// Adds a string to the console input buffer.
            /// </summary>
            /// <param name="str">String to add to console input buffer.</param>
            /// <param name="newLine">True to add Enter after the string.</param>
            /// <returns>True if it was successful in adding all characters to console input buffer.</returns>
            internal static bool AddToConsoleInputBuffer(string str, bool newLine)
            {
                IntPtr handle = ConsoleInputWithNativeMethods.GetStdHandle(ConsoleInputWithNativeMethods.STD_INPUT_HANDLE);
                if (handle == IntPtr.Zero)
                {
                    return false;
                }

                uint strLen = (uint)str.Length;

                ConsoleInputWithNativeMethods.INPUT_RECORD[] records = new ConsoleInputWithNativeMethods.INPUT_RECORD[strLen + (newLine ? 1 : 0)];

                for (int i = 0; i < strLen; i++)
                {
                    ConsoleInputWithNativeMethods.INPUT_RECORD.SetInputRecord(ref records[i], str[i]);
                }

                uint written;
                if (!ConsoleInputWithNativeMethods.WriteConsoleInput(handle, records, strLen, out written) || written != strLen)
                {
                    // I do not know of a case where written is not going to be strlen. Maybe for some character that
                    // is not supported in the console. The API suggests this can happen,
                    // so we handle it by returning false
                    return false;
                }

                // Enter is written separately, because if this is a command, and one of the characters in the command was not written
                // (written != strLen) it is desireable to fail (return false) before typing enter and running the command
                if (newLine)
                {
                    ConsoleInputWithNativeMethods.INPUT_RECORD[] enterArray = new ConsoleInputWithNativeMethods.INPUT_RECORD[1];
                    ConsoleInputWithNativeMethods.INPUT_RECORD.SetInputRecord(ref enterArray[0], (char)13);

                    written = 0;
                    if (!ConsoleInputWithNativeMethods.WriteConsoleInput(handle, enterArray, 1, out written))
                    {
                        // I don't think this will happen
                        return false;
                    }

                    Diagnostics.Assert(written == 1, "only Enter is being added and it is a supported character");
                }

                return true;
            }

            /// <summary>
            /// Gets the console handle.
            /// </summary>
            /// <param name="nStdHandle">Which console handle to get.</param>
            /// <returns>The console handle.</returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern IntPtr GetStdHandle(int nStdHandle);

            /// <summary>
            /// Writes to the console input buffer.
            /// </summary>
            /// <param name="hConsoleInput">Console handle.</param>
            /// <param name="lpBuffer">Inputs to be written.</param>
            /// <param name="nLength">Number of inputs to be written.</param>
            /// <param name="lpNumberOfEventsWritten">Returned number of inputs actually written.</param>
            /// <returns>0 if the function fails.</returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool WriteConsoleInput(
                IntPtr hConsoleInput,
                INPUT_RECORD[] lpBuffer,
                uint nLength,
                out uint lpNumberOfEventsWritten);

            /// <summary>
            /// A record to be added to the console buffer.
            /// </summary>
            internal struct INPUT_RECORD
            {
                /// <summary>
                /// The proper event type for a KeyEvent KEY_EVENT_RECORD.
                /// </summary>
                internal const int KEY_EVENT = 0x0001;

                /// <summary>
                /// Input buffer event type.
                /// </summary>
                internal ushort EventType;

                /// <summary>
                /// The actual event. The original structure is a union of many others, but this is the largest of them.
                /// And we don't need other kinds of events.
                /// </summary>
                internal KEY_EVENT_RECORD KeyEvent;

                /// <summary>
                /// Sets the necessary fields of <paramref name="inputRecord"/> for a KeyDown event for the <paramref name="character"/>
                /// </summary>
                /// <param name="inputRecord">Input record to be set.</param>
                /// <param name="character">Character to set the record with.</param>
                internal static void SetInputRecord(ref INPUT_RECORD inputRecord, char character)
                {
                    inputRecord.EventType = INPUT_RECORD.KEY_EVENT;
                    inputRecord.KeyEvent.bKeyDown = true;
                    inputRecord.KeyEvent.UnicodeChar = character;
                }
            }

            /// <summary>
            /// Type of INPUT_RECORD which is a key.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct KEY_EVENT_RECORD
            {
                /// <summary>
                /// True for key down and false for key up, but only needed if wVirtualKeyCode is used.
                /// </summary>
                internal bool bKeyDown;

                /// <summary>
                /// Repeat count.
                /// </summary>
                internal ushort wRepeatCount;

                /// <summary>
                /// Virtual key code.
                /// </summary>
                internal ushort wVirtualKeyCode;

                /// <summary>
                /// Virtual key scan code.
                /// </summary>
                internal ushort wVirtualScanCode;

                /// <summary>
                /// Character in input. If this is specified, wVirtualKeyCode, and others don't need to be.
                /// </summary>
                internal char UnicodeChar;

                /// <summary>
                /// State of keys like Shift and control.
                /// </summary>
                internal uint dwControlKeyState;
            }
        }
    }
}
