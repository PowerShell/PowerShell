// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation.Host;
using System.Security;

using Dbg = System.Management.Automation.Diagnostics;
using InternalHostUserInterface = System.Management.Automation.Internal.Host.InternalHostUserInterface;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// The ServerRemoteHostUserInterface class.
    /// </summary>
    internal class ServerRemoteHostUserInterface : PSHostUserInterface, IHostUISupportsMultipleChoiceSelection
    {
        /// <summary>
        /// Server method executor.
        /// </summary>
        private readonly ServerMethodExecutor _serverMethodExecutor;

        /// <summary>
        /// Constructor for ServerRemoteHostUserInterface.
        /// </summary>
        internal ServerRemoteHostUserInterface(ServerRemoteHost remoteHost)
        {
            Dbg.Assert(remoteHost != null, "Expected remoteHost != null");
            ServerRemoteHost = remoteHost;
            Dbg.Assert(!remoteHost.HostInfo.IsHostUINull, "Expected !remoteHost.HostInfo.IsHostUINull");

            _serverMethodExecutor = remoteHost.ServerMethodExecutor;

            // Use HostInfo to duplicate host-RawUI as null or non-null based on the client's host-RawUI.
            RawUI = remoteHost.HostInfo.IsHostRawUINull ? null : new ServerRemoteHostRawUserInterface(this);
        }

        /// <summary>
        /// Raw ui.
        /// </summary>
        public override PSHostRawUserInterface RawUI { get; }

        /// <summary>
        /// Server remote host.
        /// </summary>
        internal ServerRemoteHost ServerRemoteHost { get; }

        /// <summary>
        /// Read line.
        /// </summary>
        public override string ReadLine()
        {
            return _serverMethodExecutor.ExecuteMethod<string>(RemoteHostMethodId.ReadLine);
        }

        /// <summary>
        /// Prompt for choice.
        /// </summary>
        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            return _serverMethodExecutor.ExecuteMethod<int>(RemoteHostMethodId.PromptForChoice, new object[] { caption, message, choices, defaultChoice });
        }

        /// <summary>
        /// Prompt for choice. User can select multiple choices.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="message"></param>
        /// <param name="choices"></param>
        /// <param name="defaultChoices"></param>
        /// <returns></returns>
        public Collection<int> PromptForChoice(string caption,
            string message,
            Collection<ChoiceDescription> choices,
            IEnumerable<int> defaultChoices)
        {
            return _serverMethodExecutor.ExecuteMethod<Collection<int>>(RemoteHostMethodId.PromptForChoiceMultipleSelection,
                new object[] { caption, message, choices, defaultChoices });
        }

        /// <summary>
        /// Prompt.
        /// </summary>
        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            // forward the call to the client host
            Dictionary<string, PSObject> results = _serverMethodExecutor.ExecuteMethod<Dictionary<string, PSObject>>(
                RemoteHostMethodId.Prompt, new object[] { caption, message, descriptions });

            // attempt to do the requested type casts on the server (it is okay to fail the cast and return the original object)
            foreach (FieldDescription description in descriptions)
            {
                Type requestedType = InternalHostUserInterface.GetFieldType(description);
                if (requestedType != null)
                {
                    PSObject valueFromClient;
                    if (results.TryGetValue(description.Name, out valueFromClient))
                    {
                        object conversionResult;
                        if (LanguagePrimitives.TryConvertTo(valueFromClient, requestedType, CultureInfo.InvariantCulture, out conversionResult))
                        {
                            if (conversionResult != null)
                            {
                                results[description.Name] = PSObject.AsPSObject(conversionResult);
                            }
                            else
                            {
                                results[description.Name] = null;
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Write.
        /// </summary>
        public override void Write(string message)
        {
            message = GetOutputString(message, supportsVirtualTerminal: true);
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.Write1, new object[] { message });
        }

        /// <summary>
        /// Write.
        /// </summary>
        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string message)
        {
            message = GetOutputString(message, supportsVirtualTerminal: true);
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.Write2, new object[] { foregroundColor, backgroundColor, message });
        }

        /// <summary>
        /// Write line.
        /// </summary>
        public override void WriteLine()
        {
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.WriteLine1);
        }

        /// <summary>
        /// Write line.
        /// </summary>
        public override void WriteLine(string message)
        {
            message = GetOutputString(message, supportsVirtualTerminal: true);
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.WriteLine2, new object[] { message });
        }

        /// <summary>
        /// Write line.
        /// </summary>
        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string message)
        {
            message = GetOutputString(message, supportsVirtualTerminal: true);
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.WriteLine3, new object[] { foregroundColor, backgroundColor, message });
        }

        /// <summary>
        /// Write error line.
        /// </summary>
        public override void WriteErrorLine(string message)
        {
            message = GetOutputString(message, supportsVirtualTerminal: true);
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.WriteErrorLine, new object[] { message });
        }

        /// <summary>
        /// Write debug line.
        /// </summary>
        public override void WriteDebugLine(string message)
        {
            message = GetOutputString(message, supportsVirtualTerminal: true);
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.WriteDebugLine, new object[] { message });
        }

        /// <summary>
        /// Write progress.
        /// </summary>
        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.WriteProgress, new object[] { sourceId, record });
        }

        /// <summary>
        /// Write verbose line.
        /// </summary>
        public override void WriteVerboseLine(string message)
        {
            message = GetOutputString(message, supportsVirtualTerminal: true);
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.WriteVerboseLine, new object[] { message });
        }

        /// <summary>
        /// Write warning line.
        /// </summary>
        public override void WriteWarningLine(string message)
        {
            message = GetOutputString(message, supportsVirtualTerminal: true);
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.WriteWarningLine, new object[] { message });
        }

        /// <summary>
        /// Read line as secure string.
        /// </summary>
        public override SecureString ReadLineAsSecureString()
        {
            return _serverMethodExecutor.ExecuteMethod<SecureString>(RemoteHostMethodId.ReadLineAsSecureString);
        }

        /// <summary>
        /// Prompt for credential.
        /// </summary>
        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            return _serverMethodExecutor.ExecuteMethod<PSCredential>(RemoteHostMethodId.PromptForCredential1,
                    new object[] { caption, message, userName, targetName });
        }

        /// <summary>
        /// Prompt for credential.
        /// </summary>
        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            return _serverMethodExecutor.ExecuteMethod<PSCredential>(RemoteHostMethodId.PromptForCredential2,
                    new object[] { caption, message, userName, targetName, allowedCredentialTypes, options });
        }
    }
}
