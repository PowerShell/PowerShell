// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Management.Automation.Host
{
    /// <summary>
    /// Defines the properties and facilities providing by an hosting application deriving from
    /// <see cref="System.Management.Automation.Host.PSHost"/> that offers dialog-oriented and
    /// line-oriented interactive features.
    /// </summary>
    /// <seealso cref="System.Management.Automation.Host.PSHost"/>
    /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface"/>
    public abstract class PSHostUserInterface
    {
        /// <summary>
        /// Gets hosting application's implementation of the
        /// <see cref="System.Management.Automation.Host.PSHostRawUserInterface"/> abstract base class
        /// that implements that class.
        /// </summary>
        /// <value>
        /// A reference to an instance of the hosting application's implementation of a class derived from
        /// <see cref="System.Management.Automation.Host.PSHostUserInterface"/>, or null to indicate that
        /// low-level user interaction is not supported.
        /// </value>
        public abstract System.Management.Automation.Host.PSHostRawUserInterface RawUI
        {
            get;
        }

        /// <summary>
        /// Returns true for hosts that support VT100 like virtual terminals.
        /// </summary>
        public virtual bool SupportsVirtualTerminal { get { return false; } }

        #region Line-oriented interaction
        /// <summary>
        /// Reads characters from the console until a newline (a carriage return) is encountered.
        /// </summary>
        /// <returns>
        /// The characters typed by the user.
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.ReadLineAsSecureString"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForCredential(string, string, string, string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForCredential(string, string, string, string, System.Management.Automation.PSCredentialTypes, System.Management.Automation.PSCredentialUIOptions)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForChoice"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Prompt"/>
        public abstract string ReadLine();

        /// <summary>
        /// Same as ReadLine, except that the result is a SecureString, and that the input is not echoed to the user while it is
        /// collected (or is echoed in some obfuscated way, such as showing a dot for each character).
        /// </summary>
        /// <returns>
        /// The characters typed by the user in an encrypted form.
        /// </returns>
        /// <remarks>
        /// Note that credentials (a user name and password) should be gathered with
        /// <see cref="System.Management.Automation.Host.PSHostUserInterface.PromptForCredential(string, string, string, string)"/>
        /// <see cref="System.Management.Automation.Host.PSHostUserInterface.PromptForCredential(string, string, string, string, System.Management.Automation.PSCredentialTypes, System.Management.Automation.PSCredentialUIOptions)"/>
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.ReadLine"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForCredential(string, string, string, string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForCredential(string, string, string, string, System.Management.Automation.PSCredentialTypes, System.Management.Automation.PSCredentialUIOptions)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForChoice"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Prompt"/>
        public abstract SecureString ReadLineAsSecureString();

        /// <summary>
        /// Writes characters to the screen buffer.  Does not append a carriage return.
        /// <!-- Here we choose to just offer string parameters rather than the 18 overloads from TextWriter -->
        /// </summary>
        /// <param name="value">
        /// The characters to be written.  null is not allowed.
        /// </param>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Write(ConsoleColor, ConsoleColor, string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine()"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine(System.ConsoleColor, System.ConsoleColor, string)"/>
        public abstract void Write(string value);

        /// <summary>
        /// Same as <see cref="System.Management.Automation.Host.PSHostUserInterface.Write(string)"/>,
        /// except that colors can be specified.
        /// </summary>
        /// <param name="foregroundColor">
        /// The foreground color to display the text with.
        /// </param>
        /// <param name="backgroundColor">
        /// The foreground color to display the text with.
        /// </param>
        /// <param name="value">
        /// The characters to be written.  null is not allowed.
        /// </param>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Write(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine()"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine(System.ConsoleColor, System.ConsoleColor, string)"/>
        public abstract void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value);

        /// <summary>
        /// The default implementation writes a carriage return to the screen buffer.
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Write(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Write(System.ConsoleColor, System.ConsoleColor, string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine(System.ConsoleColor, System.ConsoleColor, string)"/>
        /// </summary>
        public virtual void WriteLine()
        {
            WriteLine(string.Empty);
        }

        /// <summary>
        /// Writes characters to the screen buffer, and appends a carriage return.
        /// </summary>
        /// <param name="value">
        /// The characters to be written.  null is not allowed.
        /// </param>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Write(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Write(System.ConsoleColor, System.ConsoleColor, string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine()"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine(System.ConsoleColor, System.ConsoleColor, string)"/>
        public abstract void WriteLine(string value);

        /// <summary>
        /// Same as <see cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine(string)"/>,
        /// except that colors can be specified.
        /// </summary>
        /// <param name="foregroundColor">
        /// The foreground color to display the text with.
        /// </param>
        /// <param name="backgroundColor">
        /// The foreground color to display the text with.
        /// </param>
        /// <param name="value">
        /// The characters to be written.  null is not allowed.
        /// </param>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Write(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Write(System.ConsoleColor, System.ConsoleColor, string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine()"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine(string)"/>
        public virtual void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            // #pragma warning disable 56506

            // expressly not checking for value == null so that attempts to write a null cause an exception

            if ((value != null) && (value.Length != 0))
            {
                Write(foregroundColor, backgroundColor, value);
            }

            Write("\n");

            // #pragma warning restore 56506
        }

        /// <summary>
        /// Writes a line to the "error display" of the host, as opposed to the "output display," which is
        /// written to by the variants of
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Write(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Write(System.ConsoleColor, System.ConsoleColor, string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine()"/> and
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine(string)"/>
        /// </summary>
        /// <param name="value">
        /// The characters to be written.
        /// </param>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Write(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Write(System.ConsoleColor, System.ConsoleColor, string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine()"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteLine(string)"/>
        public abstract void WriteErrorLine(string value);

        /// <summary>
        /// Invoked by <see cref="System.Management.Automation.Cmdlet.WriteDebug"/> to display a debugging message
        /// to the user.
        /// </summary>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteProgress"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteVerboseLine"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteWarningLine"/>
        public abstract void WriteDebugLine(string message);

        /// <summary>
        /// Invoked by <see cref="System.Management.Automation.Cmdlet.WriteProgress(Int64, System.Management.Automation.ProgressRecord)"/> to display a progress record.
        /// </summary>
        /// <param name="sourceId">
        /// Unique identifier of the source of the record.  An int64 is used because typically, the 'this' pointer of
        /// the command from whence the record is originating is used, and that may be from a remote Runspace on a 64-bit
        /// machine.
        /// </param>
        /// <param name="record">
        /// The record being reported to the host.
        /// </param>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteDebugLine"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteVerboseLine"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteWarningLine"/>
        public abstract void WriteProgress(Int64 sourceId, ProgressRecord record);

        /// <summary>
        /// Invoked by <see cref="System.Management.Automation.Cmdlet.WriteVerbose"/> to display a verbose processing message to the user.
        /// </summary>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteDebugLine"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteProgress"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteWarningLine"/>
        public abstract void WriteVerboseLine(string message);

        /// <summary>
        /// Invoked by <see cref="System.Management.Automation.Cmdlet.WriteWarning"/> to display a warning processing message to the user.
        /// </summary>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteDebugLine"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteProgress"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.WriteVerboseLine"/>
        public abstract void WriteWarningLine(string message);

        /// <summary>
        /// Invoked by <see cref="System.Management.Automation.Cmdlet.WriteInformation(InformationRecord)"/> to give the host a chance to intercept
        /// informational messages. These should not be displayed to the user by default, but may be useful to display in
        /// a separate area of the user interface.
        /// </summary>
        public virtual void WriteInformation(InformationRecord record) { }

        private static bool ShouldOutputPlainText(bool isHost, bool? supportsVirtualTerminal)
        {
            var outputRendering = OutputRendering.PlainText;

            if (supportsVirtualTerminal != false)
            {
                switch (PSStyle.Instance.OutputRendering)
                {
                    case OutputRendering.Host:
                        outputRendering = isHost ? OutputRendering.Ansi : OutputRendering.PlainText;
                        break;
                    default:
                        outputRendering = PSStyle.Instance.OutputRendering;
                        break;
                }
            }

            return outputRendering == OutputRendering.PlainText;
        }

        /// <summary>
        /// The format styles that are supported by the host.
        /// </summary>
        public enum FormatStyle
        {
            /// <summary>
            /// Reset the formatting to the default.
            /// </summary>
            Reset,

            /// <summary>
            /// Highlight text used in output formatting.
            /// </summary>
            FormatAccent,

            /// <summary>
            /// Highlight for table headers.
            /// </summary>
            TableHeader,

            /// <summary>
            /// Highlight for detailed error view.
            /// </summary>
            ErrorAccent,

            /// <summary>
            /// Style for error messages.
            /// </summary>
            Error,

            /// <summary>
            /// Style for warning messages.
            /// </summary>
            Warning,

            /// <summary>
            /// Style for verbose messages.
            /// </summary>
            Verbose,

            /// <summary>
            /// Style for debug messages.
            /// </summary>
            Debug,
        }

        /// <summary>
        /// Get the ANSI escape sequence for the given format style.
        /// </summary>
        /// <param name="formatStyle">
        /// The format style to get the escape sequence for.
        /// </param>
        /// <returns>
        /// The ANSI escape sequence for the given format style.
        /// </returns>
        public static string GetFormatStyleString(FormatStyle formatStyle)
        {
            if (PSStyle.Instance.OutputRendering == OutputRendering.PlainText)
            {
                return string.Empty;
            }

            PSStyle psstyle = PSStyle.Instance;                
            switch (formatStyle)
            {
                case FormatStyle.Reset:
                    return psstyle.Reset;
                case FormatStyle.FormatAccent:
                    return psstyle.Formatting.FormatAccent;
                case FormatStyle.TableHeader:
                    return psstyle.Formatting.TableHeader;
                case FormatStyle.ErrorAccent:
                    return psstyle.Formatting.ErrorAccent;
                case FormatStyle.Error:
                    return psstyle.Formatting.Error;
                case FormatStyle.Warning:
                    return psstyle.Formatting.Warning;
                case FormatStyle.Verbose:
                    return psstyle.Formatting.Verbose;
                case FormatStyle.Debug:
                    return psstyle.Formatting.Debug;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Get the appropriate output string based on different criteria.
        /// </summary>
        /// <param name="text">
        /// The text to format.
        /// </param>
        /// <param name="supportsVirtualTerminal">
        /// True if the host supports virtual terminal.
        /// </param>
        /// <returns>
        /// The formatted text.
        /// </returns>
        public static string GetOutputString(string text, bool supportsVirtualTerminal)
        {
            return GetOutputString(text, isHost: true, supportsVirtualTerminal: supportsVirtualTerminal);
        }

        internal static string GetOutputString(string text, bool isHost, bool? supportsVirtualTerminal = null)
        {
            var sd = new ValueStringDecorated(text);

            if (sd.IsDecorated)
            {
                var outputRendering = OutputRendering.Ansi;
                if (ShouldOutputPlainText(isHost, supportsVirtualTerminal))
                {
                    outputRendering = OutputRendering.PlainText;
                }

                text = sd.ToString(outputRendering);
            }

            return text;
        }

        // Gets the state associated with PowerShell transcription.
        //
        // Ideally, this would be associated with the host instance, but remoting recycles host instances
        // for each command that gets invoked (so that it can keep track of the order of commands and their
        // output.) Therefore, we store this transcription data in the runspace. However, the
        // Runspace.DefaultRunspace property isn't always available (i.e.: when the pipeline is being set up),
        // so we have to cache it the first time it becomes available.
        private TranscriptionData TranscriptionData
        {
            get
            {
                // If we have access to a runspace, use the transcription data for that runspace.
                // This is important when you have multiple runspaces within a host.
                LocalRunspace localRunspace = Runspace.DefaultRunspace as LocalRunspace;
                if (localRunspace != null)
                {
                    _volatileTranscriptionData = localRunspace.TranscriptionData;
                    if (_volatileTranscriptionData != null)
                    {
                        return _volatileTranscriptionData;
                    }
                }

                // Otherwise, use the last stored transcription data. This will let us transcribe
                // errors where the runspace has gone away.
                if (_volatileTranscriptionData != null)
                {
                    return _volatileTranscriptionData;
                }

                TranscriptionData temporaryTranscriptionData = new TranscriptionData();
                return temporaryTranscriptionData;
            }
        }

        private TranscriptionData _volatileTranscriptionData;

        /// <summary>
        /// Transcribes a command being invoked.
        /// </summary>
        /// <param name="commandText">The text of the command being invoked.</param>
        /// <param name="invocation">The invocation info of the command being transcribed.</param>
        internal void TranscribeCommand(string commandText, InvocationInfo invocation)
        {
            if (ShouldIgnoreCommand(commandText, invocation))
            {
                return;
            }

            if (IsTranscribing)
            {
                // We don't actually log the output here, because there may be multiple command invocations
                // in a single input - especially in the case of API logging, which logs the command and
                // its parameters as separate calls.
                // Instead, we add this to the 'pendingOutput' collection, which we flush when either
                // the command generates output, or when we are told to invoke ignore the next command.
                foreach (TranscriptionOption transcript in TranscriptionData.Transcripts.Prepend<TranscriptionOption>(TranscriptionData.SystemTranscript))
                {
                    if (transcript != null)
                    {
                        lock (transcript.OutputToLog)
                        {
                            if (transcript.OutputToLog.Count == 0)
                            {
                                if (transcript.IncludeInvocationHeader)
                                {
                                    transcript.OutputToLog.Add("**********************");
                                    transcript.OutputToLog.Add(
                                        string.Format(
                                            Globalization.CultureInfo.InvariantCulture, InternalHostUserInterfaceStrings.CommandStartTime,
                                            DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)));
                                    transcript.OutputToLog.Add("**********************");
                                }

                                transcript.OutputToLog.Add(TranscriptionData.PromptText + commandText);
                            }
                            else
                            {
                                transcript.OutputToLog.Add(">> " + commandText);
                            }
                        }
                    }
                }
            }
        }

        private bool ShouldIgnoreCommand(string logElement, InvocationInfo invocation)
        {
            string commandName = logElement;

            if (invocation != null)
            {
                commandName = invocation.InvocationName;

                // Do not transcribe Out-Default
                CmdletInfo invocationCmdlet = invocation.MyCommand as CmdletInfo;
                if (invocationCmdlet != null)
                {
                    if (invocationCmdlet.ImplementingType == typeof(Microsoft.PowerShell.Commands.OutDefaultCommand))
                    {
                        // We will ignore transcribing the command itself, but not call the IgnoreCommand() method
                        // (because that will ignore the results)
                        return true;
                    }
                }

                // Don't log internal commands to the transcript.
                if (invocation.CommandOrigin == CommandOrigin.Internal)
                {
                    IgnoreCommand(logElement, invocation);
                    return true;
                }
            }

            // Don't log helper commands to the transcript
            string[] helperCommands = { "TabExpansion2", "prompt", "TabExpansion", "PSConsoleHostReadline" };
            foreach (string helperCommand in helperCommands)
            {
                if (string.Equals(helperCommand, commandName, StringComparison.OrdinalIgnoreCase))
                {
                    IgnoreCommand(logElement, invocation);

                    // Record that this is a helper command. In this case, we ignore even the results
                    // from Out-Default
                    TranscriptionData.IsHelperCommand = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Signals that a command being invoked (and its output) should be ignored.
        /// </summary>
        /// <param name="commandText">The text of the command being invoked.</param>
        /// <param name="invocation">The invocation info of the command being transcribed.</param>
        internal void IgnoreCommand(string commandText, InvocationInfo invocation)
        {
            TranscribeCommandComplete(null);

            if (TranscriptionData.CommandBeingIgnored == null)
            {
                TranscriptionData.CommandBeingIgnored = commandText;
                TranscriptionData.IsHelperCommand = false;

                if ((invocation != null) && (invocation.MyCommand != null))
                {
                    TranscriptionData.CommandBeingIgnored = invocation.MyCommand.Name;
                }
            }
        }

        /// <summary>
        /// Flag to determine whether the host is in "Transcribe Only" mode,
        /// so that when content is sent through Out-Default it doesn't
        /// make it to the actual host.
        /// </summary>
        internal bool TranscribeOnly => Interlocked.CompareExchange(ref _transcribeOnlyCount, 0, 0) != 0;

        private int _transcribeOnlyCount = 0;

        internal IDisposable SetTranscribeOnly() => new TranscribeOnlyCookie(this);

        private sealed class TranscribeOnlyCookie : IDisposable
        {
            private readonly PSHostUserInterface _ui;
            private bool _disposed = false;

            public TranscribeOnlyCookie(PSHostUserInterface ui)
            {
                _ui = ui;
                Interlocked.Increment(ref _ui._transcribeOnlyCount);
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    Interlocked.Decrement(ref _ui._transcribeOnlyCount);
                    _disposed = true;
                    GC.SuppressFinalize(this);
                }
            }

            ~TranscribeOnlyCookie() => Dispose();
        }

        /// <summary>
        /// Flag to determine whether the host is transcribing.
        /// </summary>
        internal bool IsTranscribing
        {
            get
            {
                CheckSystemTranscript();
                return (TranscriptionData.Transcripts.Count > 0) || (TranscriptionData.SystemTranscript != null);
            }
        }

        private void CheckSystemTranscript()
        {
            lock (TranscriptionData)
            {
                if (TranscriptionData.SystemTranscript == null)
                {
                    TranscriptionData.SystemTranscript = GetSystemTranscriptOption(TranscriptionData.SystemTranscript);
                    if (TranscriptionData.SystemTranscript != null)
                    {
                        LogTranscriptHeader(null, TranscriptionData.SystemTranscript);
                    }
                }
            }
        }

        internal void StartTranscribing(string path, System.Management.Automation.Remoting.PSSenderInfo senderInfo, bool includeInvocationHeader, bool useMinimalHeader)
        {
            TranscriptionOption transcript = new TranscriptionOption();
            transcript.Path = path;
            transcript.IncludeInvocationHeader = includeInvocationHeader;
            TranscriptionData.Transcripts.Add(transcript);

            LogTranscriptHeader(senderInfo, transcript, useMinimalHeader);
        }

        private void LogTranscriptHeader(System.Management.Automation.Remoting.PSSenderInfo senderInfo, TranscriptionOption transcript, bool useMinimalHeader = false)
        {
            // Transcribe the transcript header
            string line;
            if (useMinimalHeader)
            {
                line =
                    string.Format(
                        Globalization.CultureInfo.InvariantCulture,
                        InternalHostUserInterfaceStrings.MinimalTranscriptPrologue,
                        DateTime.Now);
            }
            else
            {
                string username = Environment.UserDomainName + "\\" + Environment.UserName;
                string runAsUser = username;

                if (senderInfo != null)
                {
                    username = senderInfo.UserInfo.Identity.Name;
                }

                // Add bits from PSVersionTable
                StringBuilder versionInfoFooter = new StringBuilder();
                Hashtable versionInfo = PSVersionInfo.GetPSVersionTable();
                foreach (string versionKey in versionInfo.Keys)
                {
                    object value = versionInfo[versionKey];

                    if (value != null)
                    {
                        var arrayValue = value as object[];
                        string valueString = arrayValue != null ? string.Join(", ", arrayValue) : value.ToString();
                        versionInfoFooter.AppendLine(versionKey + ": " + valueString);
                    }
                }

                string configurationName = string.Empty;
                if (senderInfo != null && !string.IsNullOrEmpty(senderInfo.ConfigurationName))
                {
                    configurationName = senderInfo.ConfigurationName;
                }

                line =
                    string.Format(
                        Globalization.CultureInfo.InvariantCulture,
                        InternalHostUserInterfaceStrings.TranscriptPrologue,
                        DateTime.Now,
                        username,
                        runAsUser,
                        configurationName,
                        Environment.MachineName,
                        Environment.OSVersion.VersionString,
                        string.Join(" ", Environment.GetCommandLineArgs()),
                        Environment.ProcessId,
                        versionInfoFooter.ToString().TrimEnd());
            }

            lock (transcript.OutputToLog)
            {
                transcript.OutputToLog.Add(line);
            }

            TranscribeCommandComplete(null);
        }

        internal string StopTranscribing()
        {
            if (TranscriptionData.Transcripts.Count == 0)
            {
                throw new PSInvalidOperationException(InternalHostUserInterfaceStrings.HostNotTranscribing);
            }

            TranscriptionOption stoppedTranscript = TranscriptionData.Transcripts[TranscriptionData.Transcripts.Count - 1];
            LogTranscriptFooter(stoppedTranscript);
            stoppedTranscript.Dispose();
            TranscriptionData.Transcripts.Remove(stoppedTranscript);

            return stoppedTranscript.Path;
        }

        private void LogTranscriptFooter(TranscriptionOption stoppedTranscript)
        {
            // Transcribe the transcript epilogue
            try
            {
                string message = string.Format(
                    Globalization.CultureInfo.InvariantCulture,
                    InternalHostUserInterfaceStrings.TranscriptEpilogue, DateTime.Now);

                lock (stoppedTranscript.OutputToLog)
                {
                    stoppedTranscript.OutputToLog.Add(message);
                }

                TranscribeCommandComplete(null);
            }
            catch (Exception)
            {
                // Ignoring errors when stopping transcription (i.e.: file in use, access denied)
                // since this is probably handling exactly that error.
            }
        }

        internal void StopAllTranscribing()
        {
            TranscribeCommandComplete(null);

            while (TranscriptionData.Transcripts.Count > 0)
            {
                StopTranscribing();
            }

            lock (TranscriptionData)
            {
                if (TranscriptionData.SystemTranscript != null)
                {
                    LogTranscriptFooter(TranscriptionData.SystemTranscript);
                    TranscriptionData.SystemTranscript.Dispose();
                    TranscriptionData.SystemTranscript = null;

                    lock (s_systemTranscriptLock)
                    {
                        systemTranscript = null;
                    }
                }
            }
        }

        /// <summary>
        /// Transcribes the supplied result text to the transcription buffer.
        /// </summary>
        /// <param name="sourceRunspace">The runspace that was used to generate this result, if it is not the current runspace.</param>
        /// <param name="resultText">The text to be transcribed.</param>
        internal void TranscribeResult(Runspace sourceRunspace, string resultText)
        {
            if (IsTranscribing)
            {
                // If the runspace that this result applies to is not the current runspace, update Runspace.DefaultRunspace
                // so that the transcript paths / etc. will be available to the TranscriptionData accessor.
                Runspace originalDefaultRunspace = null;
                if (sourceRunspace != null)
                {
                    originalDefaultRunspace = Runspace.DefaultRunspace;
                    Runspace.DefaultRunspace = sourceRunspace;
                }

                try
                {
                    // If we're ignoring a command, ignore its output.
                    if (TranscriptionData.CommandBeingIgnored != null)
                    {
                        // If we're ignoring a prompt, capture the value
                        if (string.Equals("prompt", TranscriptionData.CommandBeingIgnored, StringComparison.OrdinalIgnoreCase))
                        {
                            TranscriptionData.PromptText = resultText;
                        }

                        return;
                    }

                    resultText = resultText.TrimEnd();

                    var text = new ValueStringDecorated(resultText);
                    if (text.IsDecorated)
                    {
                        resultText = text.ToString(OutputRendering.PlainText);
                    }

                    foreach (TranscriptionOption transcript in TranscriptionData.Transcripts.Prepend<TranscriptionOption>(TranscriptionData.SystemTranscript))
                    {
                        if (transcript != null)
                        {
                            lock (transcript.OutputToLog)
                            {
                                transcript.OutputToLog.Add(resultText);
                            }
                        }
                    }
                }
                finally
                {
                    if (originalDefaultRunspace != null)
                    {
                        Runspace.DefaultRunspace = originalDefaultRunspace;
                    }
                }
            }
        }

        /// <summary>
        /// Transcribes the supplied result text to the transcription buffer.
        /// </summary>
        /// <param name="resultText">The text to be transcribed.</param>
        internal void TranscribeResult(string resultText)
        {
            TranscribeResult(null, resultText);
        }

        /// <summary>
        /// Transcribes / records the completion of a command.
        /// </summary>
        /// <param name="invocation"></param>
        internal void TranscribeCommandComplete(InvocationInfo invocation)
        {
            FlushPendingOutput();

            if (invocation != null)
            {
                // If we're ignoring a command that was internal, we still want the
                // results of Out-Default. However, if it was a host helper command,
                // ignore all output (including Out-Default)
                string commandNameToCheck = TranscriptionData.CommandBeingIgnored;
                if (TranscriptionData.IsHelperCommand)
                {
                    commandNameToCheck = "Out-Default";
                }

                // If we're completing a command that we were ignoring, start transcribing results / etc. again.
                if ((TranscriptionData.CommandBeingIgnored != null) &&
                    (invocation != null) && (invocation.MyCommand != null) &&
                    string.Equals(commandNameToCheck, invocation.MyCommand.Name, StringComparison.OrdinalIgnoreCase))
                {
                    TranscriptionData.CommandBeingIgnored = null;
                    TranscriptionData.IsHelperCommand = false;
                }
            }
        }

        internal void TranscribePipelineComplete()
        {
            FlushPendingOutput();

            TranscriptionData.CommandBeingIgnored = null;
            TranscriptionData.IsHelperCommand = false;
        }

        private void FlushPendingOutput()
        {
            foreach (TranscriptionOption transcript in TranscriptionData.Transcripts.Prepend<TranscriptionOption>(TranscriptionData.SystemTranscript))
            {
                if (transcript != null)
                {
                    lock (transcript.OutputToLog)
                    {
                        if (transcript.OutputToLog.Count == 0)
                        {
                            continue;
                        }

                        lock (transcript.OutputBeingLogged)
                        {
                            bool alreadyLogging = transcript.OutputBeingLogged.Count > 0;

                            transcript.OutputBeingLogged.AddRange(transcript.OutputToLog);
                            transcript.OutputToLog.Clear();

                            // If there is already a thread trying to log output, add this output to its buffer
                            // and don't start a new thread.
                            if (alreadyLogging)
                            {
                                continue;
                            }
                        }
                    }

                    // Create the file in the main thread and flush the contents in the background thread.
                    // Transcription should begin only if file generation is successful.
                    // If there is an error in file generation, throw the exception.
                    string baseDirectory = Path.GetDirectoryName(transcript.Path);
                    if (Directory.Exists(transcript.Path) || (string.Equals(baseDirectory, transcript.Path.TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal)))
                    {
                        string errorMessage = string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            InternalHostUserInterfaceStrings.InvalidTranscriptFilePath,
                            transcript.Path);
                        throw new ArgumentException(errorMessage);
                    }

                    if (!Directory.Exists(baseDirectory))
                    {
                        Directory.CreateDirectory(baseDirectory);
                    }

                    if (!File.Exists(transcript.Path))
                    {
                        File.Create(transcript.Path).Dispose();
                    }

                    // Do the actual writing in the background so that it doesn't hold up the UI thread.
                    Task writer = Task.Run(() =>
                    {
                        // System transcripts can have high contention. Do exponential back-off on writing
                        // if needed.
                        int delay = new Random().Next(10) + 1;
                        bool written = false;

                        while (!written)
                        {
                            try
                            {
                                transcript.FlushContentToDisk();
                                written = true;
                            }
                            catch (IOException)
                            {
                                System.Threading.Thread.Sleep(delay);
                            }
                            catch (UnauthorizedAccessException)
                            {
                                System.Threading.Thread.Sleep(delay);
                            }

                            // If we are trying to log, but weren't able too, back of the sleep.
                            // If we're already sleeping for 1 second between tries, then just continue
                            // at this pace until the write is successful.
                            if (delay < 1000)
                            {
                                delay *= 2;
                            }
                        }
                    });
                }
            }
        }

        #endregion Line-oriented interaction

        #region Dialog-oriented Interaction

        /// <summary>
        /// Constructs a 'dialog' where the user is presented with a number of fields for which to supply values.
        /// </summary>
        /// <param name="caption">
        /// Caption to precede or title the prompt.  E.g. "Parameters for get-foo (instance 1 of 2)"
        /// </param>
        /// <param name="message">
        /// A text description of the set of fields to be prompt.
        /// </param>
        /// <param name="descriptions">
        /// Array of FieldDescriptions that contain information about each field to be prompted for.
        /// </param>
        /// <returns>
        /// A Dictionary object with results of prompting.  The keys are the field names from the FieldDescriptions, the values
        /// are objects representing the values of the corresponding fields as collected from the user. To the extent possible,
        /// the host should return values of the type(s) identified in the FieldDescription.  When that is not possible (for
        /// example, the type is not available to the host), the host should return the value as a string.
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.ReadLine"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.ReadLineAsSecureString"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForChoice"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForCredential(string, string, string, string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForCredential(string, string, string, string, System.Management.Automation.PSCredentialTypes, System.Management.Automation.PSCredentialUIOptions)"/>
        public abstract Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions);

        /// <summary>
        /// Prompt for credentials.
        /// <!--In future, when we have Credential object from the security team,
        /// this function will be modified to prompt using secure-path
        /// if so configured.-->
        /// </summary>
        /// <summary>
        /// Prompt for credential.
        /// </summary>
        /// <param name="caption">
        /// Caption for the message.
        /// </param>
        /// <param name="message">
        /// Text description for the credential to be prompt.
        /// </param>
        /// <param name="userName">
        /// Name of the user whose credential is to be prompted for. If set to null or empty
        /// string, the function will prompt for user name first.
        /// </param>
        /// <param name="targetName">
        /// Name of the target for which the credential is being collected.
        /// </param>
        /// <returns>
        /// User input credential.
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.ReadLine"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.ReadLineAsSecureString"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Prompt"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForChoice"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForCredential(string, string, string, string, System.Management.Automation.PSCredentialTypes, System.Management.Automation.PSCredentialUIOptions)"/>
        public abstract PSCredential PromptForCredential(string caption, string message,
            string userName, string targetName
        );

        /// <summary>
        /// Prompt for credential.
        /// </summary>
        /// <param name="caption">
        /// Caption for the message.
        /// </param>
        /// <param name="message">
        /// Text description for the credential to be prompt.
        /// </param>
        /// <param name="userName">
        /// Name of the user whose credential is to be prompted for. If set to null or empty
        /// string, the function will prompt for user name first.
        /// </param>
        /// <param name="targetName">
        /// Name of the target for which the credential is being collected.
        /// </param>
        /// <param name="allowedCredentialTypes">
        /// Types of credential can be supplied by the user.
        /// </param>
        /// <param name="options">
        /// Options that control the credential gathering UI behavior
        /// </param>
        /// <returns>
        /// User input credential.
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.ReadLine"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.ReadLineAsSecureString"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Prompt"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForChoice"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForCredential(string, string, string, string)"/>
        public abstract PSCredential PromptForCredential(string caption, string message,
            string userName, string targetName, PSCredentialTypes allowedCredentialTypes,
            PSCredentialUIOptions options
        );

        /// <summary>
        /// Presents a dialog allowing the user to choose an option from a set of options.
        /// </summary>
        /// <param name="caption">
        /// Caption to precede or title the prompt.  E.g. "Parameters for get-foo (instance 1 of 2)"
        /// </param>
        /// <param name="message">
        /// A message that describes what the choice is for.
        /// </param>
        /// <param name="choices">
        /// An Collection of ChoiceDescription objects that describe each choice.
        /// </param>
        /// <param name="defaultChoice">
        /// The index of the label in the choices collection element to be presented to the user as the default choice.  -1
        /// means "no default". Must be a valid index.
        /// </param>
        /// <returns>
        /// The index of the choices element that corresponds to the option selected.
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.ReadLine"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.ReadLineAsSecureString"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Prompt"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForCredential(string, string, string, string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForCredential(string, string, string, string, System.Management.Automation.PSCredentialTypes, System.Management.Automation.PSCredentialUIOptions)"/>
        public abstract int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice);

        #endregion Dialog-oriented interaction

        /// <summary>
        /// Creates a new instance of the PSHostUserInterface class.
        /// </summary>
        protected PSHostUserInterface()
        {
            CheckSystemTranscript();
        }

        /// <summary>
        /// Helper to transcribe an error through formatting and output.
        /// </summary>
        /// <param name="context">The Execution Context.</param>
        /// <param name="invocation">The invocation info associated with the record.</param>
        /// <param name="errorWrap">The error record.</param>
        internal void TranscribeError(ExecutionContext context, InvocationInfo invocation, PSObject errorWrap)
        {
            context.InternalHost.UI.TranscribeCommandComplete(invocation);
            InitialSessionState minimalState = InitialSessionState.CreateDefault2();
            Collection<PSObject> results = PowerShell.Create(minimalState).AddCommand("Out-String").Invoke(
                new List<PSObject>() { errorWrap });
            TranscribeResult(results[0].ToString());
        }

        /// <summary>
        /// Get Module Logging information from the registry.
        /// </summary>
        internal static TranscriptionOption GetSystemTranscriptOption(TranscriptionOption currentTranscript)
        {
            var transcription = InternalTestHooks.BypassGroupPolicyCaching
                ? Utils.GetPolicySetting<Transcription>(Utils.SystemWideThenCurrentUserConfig)
                : s_transcriptionSettingCache.Value;

            if (transcription != null)
            {
                // If we have an existing system transcript for this process, use that.
                // Otherwise, populate the static variable with the result of the group policy setting.
                //
                // This way, multiple runspaces opened by the same process will share the same transcript.
                lock (s_systemTranscriptLock)
                {
                    systemTranscript ??= PSHostUserInterface.GetTranscriptOptionFromSettings(transcription, currentTranscript);
                }
            }

            return systemTranscript;
        }

        internal static TranscriptionOption systemTranscript = null;
        private static readonly object s_systemTranscriptLock = new object();

        private static readonly Lazy<Transcription> s_transcriptionSettingCache = new Lazy<Transcription>(
            static () => Utils.GetPolicySetting<Transcription>(Utils.SystemWideThenCurrentUserConfig),
            isThreadSafe: true);

        private static TranscriptionOption GetTranscriptOptionFromSettings(Transcription transcriptConfig, TranscriptionOption currentTranscript)
        {
            TranscriptionOption transcript = null;

            if (transcriptConfig.EnableTranscripting == true)
            {
                if (currentTranscript != null)
                {
                    return currentTranscript;
                }

                transcript = new TranscriptionOption();

                // Pull out the transcript path
                if (transcriptConfig.OutputDirectory != null)
                {
                    transcript.Path = GetTranscriptPath(transcriptConfig.OutputDirectory, true);
                }
                else
                {
                    transcript.Path = GetTranscriptPath();
                }

                // Pull out the "enable invocation header"
                transcript.IncludeInvocationHeader = transcriptConfig.EnableInvocationHeader == true;
            }

            return transcript;
        }

        internal static string GetTranscriptPath()
        {
            string baseDirectory = Platform.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return GetTranscriptPath(baseDirectory, false);
        }

        internal static string GetTranscriptPath(string baseDirectory, bool includeDate)
        {
            if (string.IsNullOrEmpty(baseDirectory))
            {
                baseDirectory = Platform.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            else
            {
                if (!Path.IsPathRooted(baseDirectory))
                {
                    baseDirectory = Path.Combine(
                        Platform.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        baseDirectory);
                }
            }

            if (includeDate)
            {
                baseDirectory = Path.Combine(baseDirectory, DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
            }

            // transcriptPath includes some randomness so that files can be collected on a central share,
            // and an attacker can't guess the filename and read the contents if the ACL was poor.
            // After testing, a computer can do about 10,000 remote path tests per second. So 6
            // bytes of randomness (2^48 = 2.8e14) would take an attacker about 891 years to guess
            // a filename (assuming they knew the time the transcript was started).
            // (5 bytes = 3 years, 4 bytes = about a month)
            Span<byte> randomBytes = stackalloc byte[6];
            System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
            string filename = string.Format(
                        Globalization.CultureInfo.InvariantCulture,
                        "PowerShell_transcript.{0}.{1}.{2:yyyyMMddHHmmss}.txt",
                        Environment.MachineName,
                        Convert.ToBase64String(randomBytes).Replace('/', '_'),
                        DateTime.Now);

            string transcriptPath = System.IO.Path.Combine(baseDirectory, filename);
            return transcriptPath;
        }
    }

    // Holds runspace-wide transcription data / settings for PowerShell transcription
    internal class TranscriptionData
    {
        internal TranscriptionData()
        {
            Transcripts = new List<TranscriptionOption>();
            SystemTranscript = null;
            CommandBeingIgnored = null;
            IsHelperCommand = false;
            PromptText = "PS>";
        }

        internal List<TranscriptionOption> Transcripts { get; }

        internal TranscriptionOption SystemTranscript { get; set; }

        internal string CommandBeingIgnored { get; set; }

        internal bool IsHelperCommand { get; set; }

        internal string PromptText { get; set; }
    }

    // Holds options for PowerShell transcription
    internal class TranscriptionOption : IDisposable
    {
        internal TranscriptionOption()
        {
            OutputToLog = new List<string>();
            OutputBeingLogged = new List<string>();
        }

        /// <summary>
        /// The path that this transcript is being logged to.
        /// </summary>
        internal string Path { get; set; }

        /// <summary>
        /// Any output to log for this transcript.
        /// </summary>
        internal List<string> OutputToLog { get; }

        /// <summary>
        /// Any output currently being logged for this transcript.
        /// </summary>
        internal List<string> OutputBeingLogged { get; }

        /// <summary>
        /// Whether to include time stamp / command separators in
        /// transcript output.
        /// </summary>
        internal bool IncludeInvocationHeader { get; set; }

        /// <summary>
        /// Logs buffered content to disk. We use this instead of File.AppendAllLines
        /// so that we don't need to pay seek penalties all the time, and so that we
        /// don't need append permission to our own files.
        /// </summary>
        internal void FlushContentToDisk()
        {
            static Encoding GetPathEncoding(string path)
            {
                using StreamReader reader = new StreamReader(path, Encoding.Default, detectEncodingFromByteOrderMarks: true);
                _ = reader.Read();
                return reader.CurrentEncoding;
            }

            lock (OutputBeingLogged)
            {
                if (!_disposed)
                {
                    if (_contentWriter == null)
                    {
                        try
                        {
                            var currentEncoding = GetPathEncoding(this.Path);

                            // Try to first open the file with permissions that will allow us to read from it
                            // later.
                            _contentWriter = new StreamWriter(
                                new FileStream(this.Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read),
                                currentEncoding);
                            _contentWriter.BaseStream.Seek(0, SeekOrigin.End);
                        }
                        catch (IOException)
                        {
                            // If that doesn't work (i.e.: logging to a tightly-ACL'd share), request fewer
                            // file permissions.
                            _contentWriter = new StreamWriter(
                                new FileStream(this.Path, FileMode.Append, FileAccess.Write, FileShare.Read),
                                Encoding.Default);
                        }

                        _contentWriter.AutoFlush = true;
                    }

                    foreach (string line in this.OutputBeingLogged)
                    {
                        _contentWriter.WriteLine(line);
                    }
                }

                OutputBeingLogged.Clear();
            }
        }

        private StreamWriter _contentWriter = null;

        /// <summary>
        /// Disposes this runspace instance. Dispose will close the runspace if not closed already.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) { return; }

            // Wait for any pending output to be flushed to disk so that Stop-Transcript
            // can be trusted to immediately have all content from that session in the file)
            int outputWait = 0;
            while (
                (outputWait < 1000) &&
                ((OutputToLog.Count > 0) || (OutputBeingLogged.Count > 0)))
            {
                System.Threading.Thread.Sleep(100);
                outputWait += 100;
            }

            if (_contentWriter != null)
            {
                try
                {
                    _contentWriter.Flush();
                    _contentWriter.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Do nothing
                }
                catch (IOException)
                {
                    // Do nothing
                }

                _contentWriter = null;
            }

            _disposed = true;
        }

        private bool _disposed = false;
    }

    /// <summary>
    /// This interface needs to be implemented by PSHost objects that want to support PromptForChoice
    /// by giving the user ability to select more than one choice. The PromptForChoice method available
    /// in PSHostUserInterface class supports only one choice selection.
    /// </summary>
#nullable enable
    public interface IHostUISupportsMultipleChoiceSelection
    {
        /// <summary>
        /// Presents a dialog allowing the user to choose options from a set of options.
        /// </summary>
        /// <param name="caption">
        /// Caption to precede or title the prompt.  E.g. "Parameters for get-foo (instance 1 of 2)"
        /// </param>
        /// <param name="message">
        /// A message that describes what the choice is for.
        /// </param>
        /// <param name="choices">
        /// An Collection of ChoiceDescription objects that describe each choice.
        /// </param>
        /// <param name="defaultChoices">
        /// The index of the labels in the choices collection element to be presented to the user as
        /// the default choice(s).
        /// </param>
        /// <returns>
        /// The indices of the choice elements that corresponds to the options selected. The
        /// returned collection may contain duplicates depending on a particular host
        /// implementation.
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForChoice"/>
        Collection<int> PromptForChoice(string? caption, string? message,
            Collection<ChoiceDescription> choices, IEnumerable<int>? defaultChoices);
    }
#nullable restore

    /// <summary>
    /// Helper methods used by PowerShell's Hosts: ConsoleHost and InternalHost to process
    /// PromptForChoice.
    /// </summary>
    internal static class HostUIHelperMethods
    {
        /// <summary>
        /// Constructs a string of the choices and their hotkeys.
        /// </summary>
        /// <param name="choices"></param>
        /// <param name="hotkeysAndPlainLabels"></param>
        /// <exception cref="ArgumentException">
        /// 1. Cannot process the hot key because a question mark ("?") cannot be used as a hot key.
        /// </exception>
        internal static void BuildHotkeysAndPlainLabels(Collection<ChoiceDescription> choices,
            out string[,] hotkeysAndPlainLabels)
        {
            // we will allocate the result array
            hotkeysAndPlainLabels = new string[2, choices.Count];

            for (int i = 0; i < choices.Count; ++i)
            {
                #region SplitLabel
                hotkeysAndPlainLabels[0, i] = string.Empty;
                int andPos = choices[i].Label.IndexOf('&');
                if (andPos >= 0)
                {
                    Text.StringBuilder splitLabel = new Text.StringBuilder(choices[i].Label.Substring(0, andPos), choices[i].Label.Length);
                    if (andPos + 1 < choices[i].Label.Length)
                    {
                        splitLabel.Append(choices[i].Label.AsSpan(andPos + 1));
                        hotkeysAndPlainLabels[0, i] = CultureInfo.CurrentCulture.TextInfo.ToUpper(choices[i].Label.AsSpan(andPos + 1, 1).Trim().ToString());
                    }

                    hotkeysAndPlainLabels[1, i] = splitLabel.ToString().Trim();
                }
                else
                {
                    hotkeysAndPlainLabels[1, i] = choices[i].Label;
                }
                #endregion SplitLabel

                // ? is not localizable
                if (string.Equals(hotkeysAndPlainLabels[0, i], "?", StringComparison.Ordinal))
                {
                    Exception e = PSTraceSource.NewArgumentException(
                        string.Create(Globalization.CultureInfo.InvariantCulture, $"choices[{i}].Label"),
                        InternalHostUserInterfaceStrings.InvalidChoiceHotKeyError);
                    throw e;
                }
            }
        }

        /// <summary>
        /// Searches for a corresponding match between the response string and the choices.  A match is either the response
        /// string is the full text of the label (sans hotkey marker), or is a hotkey.  Full labels are checked first, and take
        /// precedence over hotkey matches.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="choices"></param>
        /// <param name="hotkeysAndPlainLabels"></param>
        /// <returns>
        /// Returns the index into the choices array matching the response string, or -1 if there is no match.
        /// </returns>
        internal static int DetermineChoicePicked(string response, Collection<ChoiceDescription> choices, string[,] hotkeysAndPlainLabels)
        {
            Diagnostics.Assert(choices != null, "choices: expected a value");
            Diagnostics.Assert(hotkeysAndPlainLabels != null, "hotkeysAndPlainLabels: expected a value");

            int result = -1;

            // check the full label first, as this is the least ambiguous
            for (int i = 0; i < choices.Count; ++i)
            {
                // pick the one that matches either the hot key or the full label
                if (string.Equals(response, hotkeysAndPlainLabels[1, i], StringComparison.CurrentCultureIgnoreCase))
                {
                    result = i;
                    break;
                }
            }

            // now check the hotkeys
            if (result == -1)
            {
                for (int i = 0; i < choices.Count; ++i)
                {
                    // Ignore labels with empty hotkeys
                    if (hotkeysAndPlainLabels[0, i].Length > 0)
                    {
                        if (string.Equals(response, hotkeysAndPlainLabels[0, i], StringComparison.CurrentCultureIgnoreCase))
                        {
                            result = i;
                            break;
                        }
                    }
                }
            }

            return result;
        }
    }
}
