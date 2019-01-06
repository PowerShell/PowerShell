// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting.Internal
{
    /// <summary>
    /// PSStreamObjectType is for internal (PowerShell) consumption and should not be treated as a public API.
    /// </summary>
    public enum PSStreamObjectType
    {
        /// <summary>
        /// </summary>
        Output = 1,

        /// <summary>
        /// </summary>
        Error = 2,

        /// <summary>
        /// </summary>
        MethodExecutor = 3,

        /// <summary>
        /// </summary>
        Warning = 4,

        /// <summary>
        /// </summary>
        BlockingError = 5,

        /// <summary>
        /// </summary>
        ShouldMethod = 6,

        /// <summary>
        /// </summary>
        WarningRecord = 7,

        /// <summary>
        /// </summary>
        Debug = 8,

        /// <summary>
        /// </summary>
        Progress = 9,

        /// <summary>
        /// </summary>
        Verbose = 10,

        /// <summary>
        /// </summary>
        Information = 11,

        /// <summary>
        /// </summary>
        Exception = 12,
    }

    /// <summary>
    /// Struct which describes whether an object written
    /// to an ObjectStream is of type - output, error,
    /// verbose, debug.
    /// PSStreamObject is for internal (PowerShell) consumption
    /// and should not be treated as a public API.
    /// </summary>
    public class PSStreamObject
    {
        /// <summary>
        /// </summary>
        public PSStreamObjectType ObjectType { get; set; }
        internal object Value { get; set; }
        internal Guid Id { get; set; }

        internal PSStreamObject(PSStreamObjectType objectType, object value, Guid id)
        {
            ObjectType = objectType;
            Value = value;
            Id = id;
        }

        /// <summary>
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="value"></param>
        public PSStreamObject(PSStreamObjectType objectType, object value) :
            this(objectType, value, Guid.Empty)
        {
        }

        /// <summary>
        /// Handle the object obtained from an ObjectStream's reader
        /// based on its type.
        /// </summary>
        /// <param name="cmdlet">Cmdlet to use for outputting the object.</param>
        /// <param name="overrideInquire">Used by Receive-Job to suppress inquire preference.</param>
        public void WriteStreamObject(Cmdlet cmdlet, bool overrideInquire = false)
        {
            if (cmdlet != null)
            {
                switch (this.ObjectType)
                {
                    case PSStreamObjectType.Output:
                        {
                            cmdlet.WriteObject(this.Value);
                        }

                        break;

                    case PSStreamObjectType.Error:
                        {
                            ErrorRecord errorRecord = (ErrorRecord)this.Value;
                            errorRecord.PreserveInvocationInfoOnce = true;
                            MshCommandRuntime mshCommandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
                            if (mshCommandRuntime != null)
                            {
                                mshCommandRuntime.WriteError(errorRecord, overrideInquire);
                            }
                        }

                        break;

                    case PSStreamObjectType.Debug:
                        {
                            string debug = (string)Value;
                            DebugRecord debugRecord = new DebugRecord(debug);
                            MshCommandRuntime mshCommandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
                            if (mshCommandRuntime != null)
                            {
                                mshCommandRuntime.WriteDebug(debugRecord, overrideInquire);
                            }
                        }

                        break;

                    case PSStreamObjectType.Warning:
                        {
                            string warning = (string)Value;
                            WarningRecord warningRecord = new WarningRecord(warning);
                            MshCommandRuntime mshCommandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
                            if (mshCommandRuntime != null)
                            {
                                mshCommandRuntime.WriteWarning(warningRecord, overrideInquire);
                            }
                        }

                        break;

                    case PSStreamObjectType.Verbose:
                        {
                            string verbose = (string)Value;
                            VerboseRecord verboseRecord = new VerboseRecord(verbose);
                            MshCommandRuntime mshCommandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
                            if (mshCommandRuntime != null)
                            {
                                mshCommandRuntime.WriteVerbose(verboseRecord, overrideInquire);
                            }
                        }

                        break;

                    case PSStreamObjectType.Progress:
                        {
                            MshCommandRuntime mshCommandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
                            if (mshCommandRuntime != null)
                            {
                                mshCommandRuntime.WriteProgress((ProgressRecord)Value, overrideInquire);
                            }
                        }

                        break;

                    case PSStreamObjectType.Information:
                        {
                            MshCommandRuntime mshCommandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
                            if (mshCommandRuntime != null)
                            {
                                mshCommandRuntime.WriteInformation((InformationRecord)Value, overrideInquire);
                            }
                        }

                        break;

                    case PSStreamObjectType.WarningRecord:
                        {
                            WarningRecord warningRecord = (WarningRecord)Value;
                            MshCommandRuntime mshCommandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
                            if (mshCommandRuntime != null)
                            {
                                mshCommandRuntime.AppendWarningVarList(warningRecord);
                            }
                        }

                        break;

                    case PSStreamObjectType.MethodExecutor:
                        {
                            Dbg.Assert(this.Value is ClientMethodExecutor,
                                       "Expected psstreamObject.value is ClientMethodExecutor");
                            ClientMethodExecutor methodExecutor = (ClientMethodExecutor)Value;
                            methodExecutor.Execute(cmdlet);
                        }

                        break;

                    case PSStreamObjectType.BlockingError:
                        {
                            CmdletMethodInvoker<object> methodInvoker = (CmdletMethodInvoker<object>)Value;
                            InvokeCmdletMethodAndWaitForResults(methodInvoker, cmdlet);
                        }

                        break;

                    case PSStreamObjectType.ShouldMethod:
                        {
                            CmdletMethodInvoker<bool> methodInvoker = (CmdletMethodInvoker<bool>)Value;
                            InvokeCmdletMethodAndWaitForResults(methodInvoker, cmdlet);
                        }

                        break;

                    case PSStreamObjectType.Exception:
                        {
                            Exception e = (Exception)Value;
                            throw e;
                        }
                }
            }
            else if (ObjectType == PSStreamObjectType.Exception)
            {
                Exception e = (Exception)Value;
                throw e;
            }
        }

        private static void GetIdentifierInfo(string message, out Guid jobInstanceId, out string computerName)
        {
            jobInstanceId = Guid.Empty;
            computerName = string.Empty;

            if (message == null) return;
            string[] parts = message.Split(Utils.Separators.Colon, 3);

            if (parts.Length != 3) return;

            if (!Guid.TryParse(parts[0], out jobInstanceId))
                jobInstanceId = Guid.Empty;

            computerName = parts[1];
        }

        /// <summary>
        /// Handle the object obtained from an ObjectStream's reader
        /// based on its type.
        /// </summary>
        /// <param name="cmdlet">Cmdlet to use for outputting the object.</param>
        /// <param name="instanceId"></param>
        /// <param name="overrideInquire">Suppresses prompt on messages with Inquire preference.
        /// Needed for Receive-Job</param>
        internal void WriteStreamObject(Cmdlet cmdlet, Guid instanceId, bool overrideInquire = false)
        {
            switch (ObjectType)
            {
                case PSStreamObjectType.Output:
                    {
                        if (instanceId != Guid.Empty)
                        {
                            PSObject o = Value as PSObject;
                            if (o != null)
                                AddSourceJobNoteProperty(o, instanceId);
                        }

                        cmdlet.WriteObject(Value);
                    }

                    break;

                case PSStreamObjectType.Error:
                    {
                        ErrorRecord errorRecord = (ErrorRecord)this.Value;
                        RemotingErrorRecord remoteErrorRecord = errorRecord as RemotingErrorRecord;

                        if (remoteErrorRecord == null)
                        {
                            // if we get a base ErrorRecord object, check if the computerName is
                            // populated in the RecommendedAction field
                            if (errorRecord.ErrorDetails != null && !string.IsNullOrEmpty(errorRecord.ErrorDetails.RecommendedAction))
                            {
                                string computerName;
                                Guid jobInstanceId;
                                GetIdentifierInfo(errorRecord.ErrorDetails.RecommendedAction,
                                                  out jobInstanceId, out computerName);

                                errorRecord = new RemotingErrorRecord(errorRecord,
                                                                      new OriginInfo(computerName, Guid.Empty,
                                                                                     jobInstanceId));
                            }
                        }
                        else
                        {
                            errorRecord = remoteErrorRecord;
                        }

                        errorRecord.PreserveInvocationInfoOnce = true;
                        MshCommandRuntime mshCommandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
                        if (mshCommandRuntime != null)
                        {
                            mshCommandRuntime.WriteError(errorRecord, overrideInquire);
                        }
                    }

                    break;

                case PSStreamObjectType.Warning:
                    {
                        string warning = (string)Value;
                        WarningRecord warningRecord = new WarningRecord(warning);
                        MshCommandRuntime mshCommandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
                        if (mshCommandRuntime != null)
                        {
                            mshCommandRuntime.WriteWarning(warningRecord, overrideInquire);
                        }
                    }

                    break;

                case PSStreamObjectType.Verbose:
                    {
                        string verbose = (string)Value;
                        VerboseRecord verboseRecord = new VerboseRecord(verbose);
                        MshCommandRuntime mshCommandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
                        if (mshCommandRuntime != null)
                        {
                            mshCommandRuntime.WriteVerbose(verboseRecord, overrideInquire);
                        }
                    }

                    break;

                case PSStreamObjectType.Progress:
                    {
                        ProgressRecord progressRecord = (ProgressRecord)Value;

                        RemotingProgressRecord remotingProgressRecord = progressRecord as RemotingProgressRecord;
                        if (remotingProgressRecord == null)
                        {
                            Guid jobInstanceId;
                            string computerName;
                            GetIdentifierInfo(progressRecord.CurrentOperation, out jobInstanceId,
                                              out computerName);
                            OriginInfo info = new OriginInfo(computerName, Guid.Empty, jobInstanceId);
                            progressRecord = new RemotingProgressRecord(progressRecord, info);
                        }
                        else
                        {
                            progressRecord = remotingProgressRecord;
                        }

                        MshCommandRuntime mshCommandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
                        if (mshCommandRuntime != null)
                        {
                            mshCommandRuntime.WriteProgress(progressRecord, overrideInquire);
                        }
                    }

                    break;

                case PSStreamObjectType.Debug:
                    {
                        string debug = (string)Value;
                        DebugRecord debugRecord = new DebugRecord(debug);
                        MshCommandRuntime mshCommandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
                        if (mshCommandRuntime != null)
                        {
                            mshCommandRuntime.WriteDebug(debugRecord, overrideInquire);
                        }
                    }

                    break;

                case PSStreamObjectType.Information:
                    {
                        InformationRecord informationRecord = (InformationRecord)this.Value;
                        RemotingInformationRecord remoteInformationRecord = informationRecord as RemotingInformationRecord;

                        if (remoteInformationRecord == null)
                        {
                            // if we get a base InformationRecord object, check if the computerName is
                            // populated in the Source field
                            if (!string.IsNullOrEmpty(informationRecord.Source))
                            {
                                string computerName;
                                Guid jobInstanceId;
                                GetIdentifierInfo(informationRecord.Source, out jobInstanceId, out computerName);
                                informationRecord = new RemotingInformationRecord(informationRecord,
                                                                      new OriginInfo(computerName, Guid.Empty,
                                                                                     jobInstanceId));
                            }
                        }
                        else
                        {
                            informationRecord = remoteInformationRecord;
                        }

                        MshCommandRuntime mshCommandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
                        if (mshCommandRuntime != null)
                        {
                            mshCommandRuntime.WriteInformation(informationRecord, overrideInquire);
                        }
                    }

                    break;

                case PSStreamObjectType.WarningRecord:
                case PSStreamObjectType.MethodExecutor:
                case PSStreamObjectType.BlockingError:
                case PSStreamObjectType.ShouldMethod:
                    {
                        WriteStreamObject(cmdlet, overrideInquire);
                    }

                    break;
            }
        }

        /// <summary>
        /// Handle the object obtained from an ObjectStream's reader
        /// based on its type.
        /// </summary>
        /// <param name="cmdlet">Cmdlet to use for outputting the object.</param>
        /// <param name="writeSourceIdentifier"></param>
        /// <param name="overrideInquire">Overrides the inquire preference, used in Receive-Job to suppress prompts.</param>
        internal void WriteStreamObject(Cmdlet cmdlet, bool writeSourceIdentifier, bool overrideInquire)
        {
            if (writeSourceIdentifier)
                WriteStreamObject(cmdlet, Id, overrideInquire);
            else
                WriteStreamObject(cmdlet, overrideInquire);
        }

        private static void InvokeCmdletMethodAndWaitForResults<T>(CmdletMethodInvoker<T> cmdletMethodInvoker, Cmdlet cmdlet)
        {
            Dbg.Assert(cmdletMethodInvoker != null, "Caller should verify cmdletMethodInvoker != null");

            cmdletMethodInvoker.MethodResult = default(T);
            try
            {
                T tmpMethodResult = cmdletMethodInvoker.Action(cmdlet);
                lock (cmdletMethodInvoker.SyncObject)
                {
                    cmdletMethodInvoker.MethodResult = tmpMethodResult;
                }
            }
            catch (Exception e)
            {
                lock (cmdletMethodInvoker.SyncObject)
                {
                    cmdletMethodInvoker.ExceptionThrownOnCmdletThread = e;
                }

                throw;
            }
            finally
            {
                if (cmdletMethodInvoker.Finished != null)
                {
                    cmdletMethodInvoker.Finished.Set();
                }
            }
        }

        internal static void AddSourceJobNoteProperty(PSObject psObj, Guid instanceId)
        {
            Dbg.Assert(psObj != null, "psObj is null trying to add a note property.");
            if (psObj.Properties[RemotingConstants.SourceJobInstanceId] != null)
            {
                psObj.Properties.Remove(RemotingConstants.SourceJobInstanceId);
            }

            psObj.Properties.Add(new PSNoteProperty(RemotingConstants.SourceJobInstanceId, instanceId));
        }

        internal static string CreateInformationalMessage(Guid instanceId, string message)
        {
            var newMessage = new StringBuilder(instanceId.ToString());
            newMessage.Append(":");
            newMessage.Append(message);
            return newMessage.ToString();
        }

        internal static ErrorRecord AddSourceTagToError(ErrorRecord errorRecord, Guid sourceId)
        {
            if (errorRecord == null) return null;
            if (errorRecord.ErrorDetails == null) errorRecord.ErrorDetails = new ErrorDetails(string.Empty);
            errorRecord.ErrorDetails.RecommendedAction = CreateInformationalMessage(sourceId, errorRecord.ErrorDetails.RecommendedAction);
            return errorRecord;
        }
    }
}

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// </summary>
    public class CmdletMethodInvoker<T>
    {
        /// <summary>
        /// </summary>
        public Func<Cmdlet, T> Action { get; set; }

        /// <summary>
        /// </summary>
        public Exception ExceptionThrownOnCmdletThread { get; set; }

        /// <summary>
        /// </summary>
        public ManualResetEventSlim Finished { get; set; }

        /// <summary>
        /// </summary>
        public object SyncObject { get; set; }

        /// <summary>
        /// </summary>
        public T MethodResult { get; set; }
    }
}
