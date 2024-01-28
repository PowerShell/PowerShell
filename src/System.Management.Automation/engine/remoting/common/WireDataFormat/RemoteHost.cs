// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Host;
using System.Reflection;

using Dbg = System.Management.Automation.Diagnostics;
using InternalHostUserInterface = System.Management.Automation.Internal.Host.InternalHostUserInterface;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Executes methods; can be encoded and decoded for transmission over the
    /// wire.
    /// </summary>
    internal sealed class RemoteHostCall
    {
        /// <summary>
        /// Method name.
        /// </summary>
        internal string MethodName
        {
            get
            {
                return _methodInfo.Name;
            }
        }

        /// <summary>
        /// Method id.
        /// </summary>
        internal RemoteHostMethodId MethodId { get; }

        /// <summary>
        /// Parameters.
        /// </summary>
        internal object[] Parameters { get; }

        /// <summary>
        /// Method info.
        /// </summary>
        private readonly RemoteHostMethodInfo _methodInfo;

        /// <summary>
        /// Call id.
        /// </summary>
        private readonly long _callId;

        /// <summary>
        /// Call id.
        /// </summary>
        internal long CallId
        {
            get
            {
                return _callId;
            }
        }

        /// <summary>
        /// Computer name to be used in messages.
        /// </summary>
        private string _computerName;

        /// <summary>
        /// Constructor for RemoteHostCall.
        /// </summary>
        internal RemoteHostCall(long callId, RemoteHostMethodId methodId, object[] parameters)
        {
            Dbg.Assert(parameters != null, "Expected parameters != null");
            _callId = callId;
            MethodId = methodId;
            Parameters = parameters;
            _methodInfo = RemoteHostMethodInfo.LookUp(methodId);
        }

        /// <summary>
        /// Encode parameters.
        /// </summary>
        private static PSObject EncodeParameters(object[] parameters)
        {
            // Encode the parameters and wrap the array into an ArrayList and then into a PSObject.
            ArrayList parameterList = new ArrayList();
            for (int i = 0; i < parameters.Length; ++i)
            {
                object parameter = parameters[i] == null ? null : RemoteHostEncoder.EncodeObject(parameters[i]);
                parameterList.Add(parameter);
            }

            return new PSObject(parameterList);
        }

        /// <summary>
        /// Decode parameters.
        /// </summary>
        private static object[] DecodeParameters(PSObject parametersPSObject, Type[] parameterTypes)
        {
            // Extract the ArrayList and decode the parameters.
            ArrayList parameters = (ArrayList)parametersPSObject.BaseObject;
            List<object> decodedParameters = new List<object>();
            Dbg.Assert(parameters.Count == parameterTypes.Length, "Expected parameters.Count == parameterTypes.Length");
            for (int i = 0; i < parameters.Count; ++i)
            {
                object parameter = parameters[i] == null ? null : RemoteHostEncoder.DecodeObject(parameters[i], parameterTypes[i]);
                decodedParameters.Add(parameter);
            }

            return decodedParameters.ToArray();
        }

        /// <summary>
        /// Encode.
        /// </summary>
        internal PSObject Encode()
        {
            // Add all host information as data.
            PSObject data = RemotingEncoder.CreateEmptyPSObject();

            // Encode the parameters for transport.
            PSObject parametersPSObject = EncodeParameters(Parameters);

            // Embed everything into the main PSobject.
            data.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.CallId, _callId));
            data.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MethodId, MethodId));
            data.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MethodParameters, parametersPSObject));

            return data;
        }

        /// <summary>
        /// Decode.
        /// </summary>
        internal static RemoteHostCall Decode(PSObject data)
        {
            Dbg.Assert(data != null, "Expected data != null");

            // Extract all the fields from data.
            long callId = RemotingDecoder.GetPropertyValue<long>(data, RemoteDataNameStrings.CallId);
            PSObject parametersPSObject = RemotingDecoder.GetPropertyValue<PSObject>(data, RemoteDataNameStrings.MethodParameters);
            RemoteHostMethodId methodId = RemotingDecoder.GetPropertyValue<RemoteHostMethodId>(data, RemoteDataNameStrings.MethodId);

            // Look up all the info related to the method.
            RemoteHostMethodInfo methodInfo = RemoteHostMethodInfo.LookUp(methodId);

            // Decode the parameters.
            object[] parameters = DecodeParameters(parametersPSObject, methodInfo.ParameterTypes);

            // Create and return the RemoteHostCall.
            return new RemoteHostCall(callId, methodId, parameters);
        }

        /// <summary>
        /// Is void method.
        /// </summary>
        internal bool IsVoidMethod
        {
            get
            {
                return _methodInfo.ReturnType == typeof(void);
            }
        }

        /// <summary>
        /// Execute void method.
        /// </summary>
        internal void ExecuteVoidMethod(PSHost clientHost)
        {
            // The clientHost can be null if the user creates a runspace object without providing
            // a host parameter.
            if (clientHost == null)
            {
                return;
            }

            RemoteRunspace remoteRunspaceToClose = null;
            if (this.IsSetShouldExitOrPopRunspace)
            {
                remoteRunspaceToClose = GetRemoteRunspaceToClose(clientHost);
            }

            try
            {
                object targetObject = this.SelectTargetObject(clientHost);
                MyMethodBase.Invoke(targetObject, Parameters);
            }
            finally
            {
                remoteRunspaceToClose?.Close();
            }
        }

        /// <summary>
        /// Get remote runspace to close.
        /// </summary>
        private static RemoteRunspace GetRemoteRunspaceToClose(PSHost clientHost)
        {
            // Figure out if we need to close the remote runspace. Return null if we don't.

            // Are we a Start-PSSession enabled host?
            IHostSupportsInteractiveSession host = clientHost as IHostSupportsInteractiveSession;
            if (host == null || !host.IsRunspacePushed)
            {
                return null;
            }

            // Now inspect the runspace.
            RemoteRunspace remoteRunspace = host.Runspace as RemoteRunspace;
            if (remoteRunspace == null || !remoteRunspace.ShouldCloseOnPop)
            {
                return null;
            }

            // At this point it is clear we have to close the remote runspace, so return it.
            return remoteRunspace;
        }

        /// <summary>
        /// My method base.
        /// </summary>
        private MethodBase MyMethodBase
        {
            get
            {
                return (MethodBase)_methodInfo.InterfaceType.GetMethod(_methodInfo.Name, _methodInfo.ParameterTypes);
            }
        }

        /// <summary>
        /// Execute non void method.
        /// </summary>
        internal RemoteHostResponse ExecuteNonVoidMethod(PSHost clientHost)
        {
            // The clientHost can be null if the user creates a runspace object without providing
            // a host parameter.
            if (clientHost == null)
            {
                throw RemoteHostExceptions.NewNullClientHostException();
            }

            object targetObject = this.SelectTargetObject(clientHost);
            RemoteHostResponse remoteHostResponse = this.ExecuteNonVoidMethodOnObject(targetObject);
            return remoteHostResponse;
        }

        /// <summary>
        /// Execute non void method on object.
        /// </summary>
        private RemoteHostResponse ExecuteNonVoidMethodOnObject(object instance)
        {
            // Create variables to store result of execution.
            Exception exception = null;
            object returnValue = null;

            // Invoke the method and store its return values.
            try
            {
                if (MethodId == RemoteHostMethodId.GetBufferContents)
                {
                    throw new PSRemotingDataStructureException(RemotingErrorIdStrings.RemoteHostGetBufferContents,
                        _computerName.ToUpper());
                }

                returnValue = MyMethodBase.Invoke(instance, Parameters);
            }
            catch (Exception e)
            {
                // Catch-all OK, 3rd party callout.
                exception = e.InnerException;
            }

            // Create a RemoteHostResponse object to store the return value and exceptions.
            return new RemoteHostResponse(_callId, MethodId, returnValue, exception);
        }

        /// <summary>
        /// Get the object that this method should be invoked on.
        /// </summary>
        private object SelectTargetObject(PSHost host)
        {
            if (host == null || host.UI == null)
            { return null; }

            if (_methodInfo.InterfaceType == typeof(PSHost))
            { return host; }

            if (_methodInfo.InterfaceType == typeof(IHostSupportsInteractiveSession))
            { return host; }

            if (_methodInfo.InterfaceType == typeof(PSHostUserInterface))
            { return host.UI; }

            if (_methodInfo.InterfaceType == typeof(IHostUISupportsMultipleChoiceSelection))
            { return host.UI; }

            if (_methodInfo.InterfaceType == typeof(PSHostRawUserInterface))
            { return host.UI.RawUI; }

            throw RemoteHostExceptions.NewUnknownTargetClassException(_methodInfo.InterfaceType.ToString());
        }

        /// <summary>
        /// Is set should exit.
        /// </summary>
        internal bool IsSetShouldExit
        {
            get
            {
                return MethodId == RemoteHostMethodId.SetShouldExit;
            }
        }

        /// <summary>
        /// Is set should exit or pop runspace.
        /// </summary>
        internal bool IsSetShouldExitOrPopRunspace
        {
            get
            {
                return
                    MethodId == RemoteHostMethodId.SetShouldExit ||
                    MethodId == RemoteHostMethodId.PopRunspace;
            }
        }

        /// <summary>
        /// This message performs various security checks on the
        /// remote host call message. If there is a need to modify
        /// the message or discard it for security reasons then
        /// such modifications will be made here.
        /// </summary>
        /// <param name="computerName">computer name to use in
        /// warning messages</param>
        /// <returns>A collection of remote host calls which will
        /// have to be executed before this host call can be
        /// executed.</returns>
        internal Collection<RemoteHostCall> PerformSecurityChecksOnHostMessage(string computerName)
        {
            Dbg.Assert(!string.IsNullOrEmpty(computerName),
                "Computer Name must be passed for use in warning messages");
            _computerName = computerName;
            Collection<RemoteHostCall> prerequisiteCalls = new Collection<RemoteHostCall>();

            // check if the incoming message is a PromptForCredential message
            // if so, do the following:
            //       (a) prepend "PowerShell Credential Request" in the title
            //       (b) prepend "Message from Server XXXXX" to the text message
            if (MethodId == RemoteHostMethodId.PromptForCredential1 ||
                MethodId == RemoteHostMethodId.PromptForCredential2)
            {
                // modify the caption which is _parameters[0]
                string modifiedCaption = ModifyCaption((string)Parameters[0]);

                // modify the message which is _parameters[1]
                string modifiedMessage = ModifyMessage((string)Parameters[1], computerName);

                Parameters[0] = modifiedCaption;
                Parameters[1] = modifiedMessage;
            }

            // Check if the incoming message is a Prompt message
            // if so, then do the following:
            //        (a) check if any of the field descriptions
            //            correspond to PSCredential
            //        (b) if field descriptions correspond to
            //            PSCredential modify the caption and
            //            message as in the previous case above
            else if (MethodId == RemoteHostMethodId.Prompt)
            {
                // check if any of the field descriptions is for type
                // PSCredential
                if (Parameters.Length == 3)
                {
                    Collection<FieldDescription> fieldDescs =
                        (Collection<FieldDescription>)Parameters[2];

                    bool havePSCredential = false;

                    foreach (FieldDescription fieldDesc in fieldDescs)
                    {
                        fieldDesc.IsFromRemoteHost = true;

                        Type fieldType = InternalHostUserInterface.GetFieldType(fieldDesc);
                        if (fieldType != null)
                        {
                            if (fieldType == typeof(PSCredential))
                            {
                                havePSCredential = true;
                                fieldDesc.ModifiedByRemotingProtocol = true;
                            }
                            else if (fieldType == typeof(System.Security.SecureString))
                            {
                                prerequisiteCalls.Add(ConstructWarningMessageForSecureString(
                                    computerName, RemotingErrorIdStrings.RemoteHostPromptSecureStringPrompt));
                            }
                        }
                    }

                    if (havePSCredential)
                    {
                        // modify the caption which is parameter[0]
                        string modifiedCaption = ModifyCaption((string)Parameters[0]);

                        // modify the message which is parameter[1]
                        string modifiedMessage = ModifyMessage((string)Parameters[1], computerName);

                        Parameters[0] = modifiedCaption;
                        Parameters[1] = modifiedMessage;
                    }
                }
            }

            // Check if the incoming message is a readline as secure string
            // if so do the following:
            //      (a) Specify a warning message that the server is
            //          attempting to read something securely on the client
            else if (MethodId == RemoteHostMethodId.ReadLineAsSecureString)
            {
                prerequisiteCalls.Add(ConstructWarningMessageForSecureString(
                                    computerName, RemotingErrorIdStrings.RemoteHostReadLineAsSecureStringPrompt));
            }

            // check if the incoming call is GetBufferContents
            // if so do the following:
            //      (a) Specify a warning message that the server is
            //          attempting to read the screen buffer contents
            //          on screen and it has been blocked
            //      (b) Modify the message so that call is not executed
            else if (MethodId == RemoteHostMethodId.GetBufferContents)
            {
                prerequisiteCalls.Add(ConstructWarningMessageForGetBufferContents(computerName));
            }

            return prerequisiteCalls;
        }

        /// <summary>
        /// Provides the modified caption for the given caption
        /// Used in ensuring that remote prompt messages are
        /// tagged with "PowerShell Credential Request"
        /// </summary>
        /// <param name="caption">Caption to modify.</param>
        /// <returns>New modified caption.</returns>
        private static string ModifyCaption(string caption)
        {
            string pscaption = CredUI.PromptForCredential_DefaultCaption;

            if (!caption.Equals(pscaption, StringComparison.OrdinalIgnoreCase))
            {
                string modifiedCaption = PSRemotingErrorInvariants.FormatResourceString(
                    RemotingErrorIdStrings.RemoteHostPromptForCredentialModifiedCaption, caption);

                return modifiedCaption;
            }

            return caption;
        }

        /// <summary>
        /// Provides the modified message for the given one
        /// Used in ensuring that remote prompt messages
        /// contain a warning that they originate from a
        /// different computer.
        /// </summary>
        /// <param name="message">Original message to modify.</param>
        /// <param name="computerName">computername to include in the
        /// message</param>
        /// <returns>Message which contains a warning as well.</returns>
        private static string ModifyMessage(string message, string computerName)
        {
            string modifiedMessage = PSRemotingErrorInvariants.FormatResourceString(
                    RemotingErrorIdStrings.RemoteHostPromptForCredentialModifiedMessage,
                        computerName.ToUpper(),
                            message);

            return modifiedMessage;
        }

        /// <summary>
        /// Creates a warning message which displays to the user a
        /// warning stating that the remote host computer is
        /// actually attempting to read a line as a secure string.
        /// </summary>
        /// <param name="computerName">computer name to include
        /// in warning</param>
        /// <param name="resourceString">Resource string to use.</param>
        /// <returns>A constructed remote host call message
        /// which will display the warning.</returns>
        private static RemoteHostCall ConstructWarningMessageForSecureString(string computerName,
            string resourceString)
        {
            string warning = PSRemotingErrorInvariants.FormatResourceString(
                resourceString,
                    computerName.ToUpper());

            return new RemoteHostCall(ServerDispatchTable.VoidCallId,
                RemoteHostMethodId.WriteWarningLine, new object[] { warning });
        }

        /// <summary>
        /// Creates a warning message which displays to the user a
        /// warning stating that the remote host computer is
        /// attempting to read the host's buffer contents and that
        /// it was suppressed.
        /// </summary>
        /// <param name="computerName">computer name to include
        /// in warning</param>
        /// <returns>A constructed remote host call message
        /// which will display the warning.</returns>
        private static RemoteHostCall ConstructWarningMessageForGetBufferContents(string computerName)
        {
            string warning = PSRemotingErrorInvariants.FormatResourceString(
                RemotingErrorIdStrings.RemoteHostGetBufferContents,
                    computerName.ToUpper());

            return new RemoteHostCall(ServerDispatchTable.VoidCallId,
                RemoteHostMethodId.WriteWarningLine, new object[] { warning });
        }
    }

    /// <summary>
    /// Encapsulates the method response semantics. Method responses are generated when
    /// RemoteHostCallPacket objects are executed. They can contain both the return values of
    /// the execution as well as exceptions that were thrown in the RemoteHostCallPacket
    /// execution. They can be encoded and decoded for transporting over the wire. A
    /// method response can be used to transport the result of an execution and then to
    /// simulate the execution on the other end.
    /// </summary>
    internal sealed class RemoteHostResponse
    {
        /// <summary>
        /// Call id.
        /// </summary>
        private readonly long _callId;

        /// <summary>
        /// Call id.
        /// </summary>
        internal long CallId
        {
            get
            {
                return _callId;
            }
        }

        /// <summary>
        /// Method id.
        /// </summary>
        private readonly RemoteHostMethodId _methodId;

        /// <summary>
        /// Return value.
        /// </summary>
        private readonly object _returnValue;

        /// <summary>
        /// Exception.
        /// </summary>
        private readonly Exception _exception;

        /// <summary>
        /// Constructor for RemoteHostResponse.
        /// </summary>
        internal RemoteHostResponse(long callId, RemoteHostMethodId methodId, object returnValue, Exception exception)
        {
            _callId = callId;
            _methodId = methodId;
            _returnValue = returnValue;
            _exception = exception;
        }

        /// <summary>
        /// Simulate execution.
        /// </summary>
        internal object SimulateExecution()
        {
            if (_exception != null)
            {
                throw _exception;
            }

            return _returnValue;
        }

        /// <summary>
        /// Encode and add return value.
        /// </summary>
        private static void EncodeAndAddReturnValue(PSObject psObject, object returnValue)
        {
            // Do nothing if the return value is null.
            if (returnValue == null)
            { return; }

            // Otherwise add the property.
            RemoteHostEncoder.EncodeAndAddAsProperty(psObject, RemoteDataNameStrings.MethodReturnValue, returnValue);
        }

        /// <summary>
        /// Decode return value.
        /// </summary>
        private static object DecodeReturnValue(PSObject psObject, Type returnType)
        {
            object returnValue = RemoteHostEncoder.DecodePropertyValue(psObject, RemoteDataNameStrings.MethodReturnValue, returnType);
            return returnValue;
        }

        /// <summary>
        /// Encode and add exception.
        /// </summary>
        private static void EncodeAndAddException(PSObject psObject, Exception exception)
        {
            RemoteHostEncoder.EncodeAndAddAsProperty(psObject, RemoteDataNameStrings.MethodException, exception);
        }

        /// <summary>
        /// Decode exception.
        /// </summary>
        private static Exception DecodeException(PSObject psObject)
        {
            object result = RemoteHostEncoder.DecodePropertyValue(psObject, RemoteDataNameStrings.MethodException, typeof(Exception));
            if (result == null)
            { return null; }

            if (result is Exception)
            { return (Exception)result; }

            throw RemoteHostExceptions.NewDecodingFailedException();
        }

        /// <summary>
        /// Encode.
        /// </summary>
        internal PSObject Encode()
        {
            // Create a data object and put everything in that and return it.
            PSObject data = RemotingEncoder.CreateEmptyPSObject();
            EncodeAndAddReturnValue(data, _returnValue);
            EncodeAndAddException(data, _exception);
            data.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.CallId, _callId));
            data.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MethodId, _methodId));
            return data;
        }

        /// <summary>
        /// Decode.
        /// </summary>
        internal static RemoteHostResponse Decode(PSObject data)
        {
            Dbg.Assert(data != null, "Expected data != null");

            // Extract all the fields from data.
            long callId = RemotingDecoder.GetPropertyValue<long>(data, RemoteDataNameStrings.CallId);
            RemoteHostMethodId methodId = RemotingDecoder.GetPropertyValue<RemoteHostMethodId>(data, RemoteDataNameStrings.MethodId);

            // Decode the return value and the exception.
            RemoteHostMethodInfo methodInfo = RemoteHostMethodInfo.LookUp(methodId);
            object returnValue = DecodeReturnValue(data, methodInfo.ReturnType);
            Exception exception = DecodeException(data);

            // Use these values to create a RemoteHostResponse and return it.
            return new RemoteHostResponse(callId, methodId, returnValue, exception);
        }
    }

    /// <summary>
    /// The RemoteHostExceptions class.
    /// </summary>
    internal static class RemoteHostExceptions
    {
        /// <summary>
        /// New remote runspace does not support push runspace exception.
        /// </summary>
        internal static Exception NewRemoteRunspaceDoesNotSupportPushRunspaceException()
        {
            string resourceString = PSRemotingErrorInvariants.FormatResourceString(
                RemotingErrorIdStrings.RemoteRunspaceDoesNotSupportPushRunspace);
            return new PSRemotingDataStructureException(resourceString);
        }

        /// <summary>
        /// New decoding failed exception.
        /// </summary>
        internal static Exception NewDecodingFailedException()
        {
            string resourceString = PSRemotingErrorInvariants.FormatResourceString(
                RemotingErrorIdStrings.RemoteHostDecodingFailed);
            return new PSRemotingDataStructureException(resourceString);
        }

        /// <summary>
        /// New not implemented exception.
        /// </summary>
        internal static Exception NewNotImplementedException(RemoteHostMethodId methodId)
        {
            RemoteHostMethodInfo methodInfo = RemoteHostMethodInfo.LookUp(methodId);
            string resourceString = PSRemotingErrorInvariants.FormatResourceString(
                RemotingErrorIdStrings.RemoteHostMethodNotImplemented, methodInfo.Name);
            return new PSRemotingDataStructureException(resourceString, new PSNotImplementedException());
        }

        /// <summary>
        /// New remote host call failed exception.
        /// </summary>
        internal static Exception NewRemoteHostCallFailedException(RemoteHostMethodId methodId)
        {
            RemoteHostMethodInfo methodInfo = RemoteHostMethodInfo.LookUp(methodId);
            string resourceString = PSRemotingErrorInvariants.FormatResourceString(
                RemotingErrorIdStrings.RemoteHostCallFailed, methodInfo.Name);
            return new PSRemotingDataStructureException(resourceString);
        }

        /// <summary>
        /// New decoding error for error record exception.
        /// </summary>
        internal static Exception NewDecodingErrorForErrorRecordException()
        {
            return new PSRemotingDataStructureException(RemotingErrorIdStrings.DecodingErrorForErrorRecord);
        }

        /// <summary>
        /// New remote host data encoding not supported exception.
        /// </summary>
        internal static Exception NewRemoteHostDataEncodingNotSupportedException(Type type)
        {
            Dbg.Assert(type != null, "Expected type != null");
            return new PSRemotingDataStructureException(
                RemotingErrorIdStrings.RemoteHostDataEncodingNotSupported,
                type.ToString());
        }

        /// <summary>
        /// New remote host data decoding not supported exception.
        /// </summary>
        internal static Exception NewRemoteHostDataDecodingNotSupportedException(Type type)
        {
            Dbg.Assert(type != null, "Expected type != null");
            return new PSRemotingDataStructureException(
                RemotingErrorIdStrings.RemoteHostDataDecodingNotSupported,
                type.ToString());
        }

        /// <summary>
        /// New unknown target class exception.
        /// </summary>
        internal static Exception NewUnknownTargetClassException(string className)
        {
            Dbg.Assert(className != null, "Expected className != null");
            return new PSRemotingDataStructureException(RemotingErrorIdStrings.UnknownTargetClass, className);
        }

        internal static Exception NewNullClientHostException()
        {
            return new PSRemotingDataStructureException(RemotingErrorIdStrings.RemoteHostNullClientHost);
        }
    }
}
