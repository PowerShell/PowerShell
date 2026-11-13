// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace System.Management.Automation
{
    /// <summary>
    /// Base class for items in the PSInformationalBuffers.
    ///
    /// A PSInformationalRecord consists of a string Message and the InvocationInfo and pipeline state corresponding
    /// to the command that created the record.
    /// </summary>
    [DataContract]
    public abstract class InformationalRecord
    {
        /// <remarks>
        /// This class can be instantiated only by its derived classes
        /// </remarks>
        internal InformationalRecord(string message)
        {
            _message = message;
            _invocationInfo = null;
            _pipelineIterationInfo = null;
            _serializeExtendedInfo = false;
        }

        /// <summary>
        /// Creates an InformationalRecord object from a record serialized as a PSObject by ToPSObjectForRemoting.
        /// </summary>
        internal InformationalRecord(PSObject serializedObject)
        {
            _message = (string)SerializationUtilities.GetPropertyValue(serializedObject, "InformationalRecord_Message");
            _serializeExtendedInfo = (bool)SerializationUtilities.GetPropertyValue(serializedObject, "InformationalRecord_SerializeInvocationInfo");

            if (_serializeExtendedInfo)
            {
                _invocationInfo = new InvocationInfo(serializedObject);

                ArrayList pipelineIterationInfo = (ArrayList)SerializationUtilities.GetPsObjectPropertyBaseObject(serializedObject, "InformationalRecord_PipelineIterationInfo");

                _pipelineIterationInfo = new ReadOnlyCollection<int>((int[])pipelineIterationInfo.ToArray(typeof(int)));
            }
            else
            {
                _invocationInfo = null;
            }
        }

        /// <summary>
        /// The message written by the command that created this record.
        /// </summary>
        public string Message
        {
            get
            {
                return _message;
            }

            set
            {
                _message = value;
            }
        }

        /// <summary>
        /// The InvocationInfo of the command that created this record.
        /// </summary>
        /// <remarks>
        /// The InvocationInfo can be null if the record was not created by a command.
        /// </remarks>
        public InvocationInfo InvocationInfo
        {
            get
            {
                return _invocationInfo;
            }
        }

        /// <summary>
        /// The status of the pipeline when this record was created.
        /// </summary>
        /// <remarks>
        /// The PipelineIterationInfo can be null if the record was not created by a command.
        /// </remarks>
        public ReadOnlyCollection<int> PipelineIterationInfo
        {
            get
            {
                return _pipelineIterationInfo;
            }
        }

        /// <summary>
        /// Sets the InvocationInfo (and PipelineIterationInfo) for this record.
        /// </summary>
        internal void SetInvocationInfo(InvocationInfo invocationInfo)
        {
            _invocationInfo = invocationInfo;

            //
            // Copy a snapshot of the PipelineIterationInfo from the InvocationInfo to this InformationalRecord
            //
            if (invocationInfo.PipelineIterationInfo != null)
            {
                int[] snapshot = (int[])invocationInfo.PipelineIterationInfo.Clone();

                _pipelineIterationInfo = new ReadOnlyCollection<int>(snapshot);
            }
        }

        /// <summary>
        /// Whether to serialize the InvocationInfo and PipelineIterationInfo during remote calls.
        /// </summary>
        internal bool SerializeExtendedInfo
        {
            get
            {
                return _serializeExtendedInfo;
            }

            set
            {
                _serializeExtendedInfo = value;
            }
        }

        /// <summary>
        /// Returns the record's message.
        /// </summary>
        public override string ToString()
        {
            return this.Message;
        }

        /// <summary>
        /// Adds the information about this informational record to a PSObject as note properties.
        /// The PSObject is used to serialize the record during remote operations.
        /// </summary>
        internal virtual void ToPSObjectForRemoting(PSObject psObject)
        {
            RemotingEncoder.AddNoteProperty<string>(psObject, "InformationalRecord_Message", () => this.Message);

            //
            // The invocation info may be null if the record was created via WriteVerbose/Warning/DebugLine instead of WriteVerbose/Warning/Debug, in that case
            // we set InformationalRecord_SerializeInvocationInfo to false.
            //
            if (!this.SerializeExtendedInfo || _invocationInfo == null)
            {
                RemotingEncoder.AddNoteProperty(psObject, "InformationalRecord_SerializeInvocationInfo", () => false);
            }
            else
            {
                RemotingEncoder.AddNoteProperty(psObject, "InformationalRecord_SerializeInvocationInfo", () => true);
                _invocationInfo.ToPSObjectForRemoting(psObject);
                RemotingEncoder.AddNoteProperty<object>(psObject, "InformationalRecord_PipelineIterationInfo", () => this.PipelineIterationInfo);
            }
        }

        [DataMember]
        private string _message;

        private InvocationInfo _invocationInfo;
        private ReadOnlyCollection<int> _pipelineIterationInfo;
        private bool _serializeExtendedInfo;
    }

    /// <summary>
    /// A warning record in the PSInformationalBuffers.
    /// </summary>
    [DataContract]
    public class WarningRecord : InformationalRecord
    {
        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public WarningRecord(string message)
            : base(message)
        { }

        /// <summary>
        /// </summary>
        /// <param name="record"></param>
        public WarningRecord(PSObject record)
            : base(record)
        { }

        /// <summary>
        /// Constructor for Fully qualified warning Id.
        /// </summary>
        /// <param name="fullyQualifiedWarningId">Fully qualified warning Id.</param>
        /// <param name="message">Warning message.</param>
        public WarningRecord(string fullyQualifiedWarningId, string message)
            : base(message)
        {
            _fullyQualifiedWarningId = fullyQualifiedWarningId;
        }

        /// <summary>
        /// Constructor for Fully qualified warning Id.
        /// </summary>
        /// <param name="fullyQualifiedWarningId">Fully qualified warning Id.</param>
        /// <param name="record">Warning serialized object.</param>
        public WarningRecord(string fullyQualifiedWarningId, PSObject record)
            : base(record)
        {
            _fullyQualifiedWarningId = fullyQualifiedWarningId;
        }

        /// <summary>
        /// String which uniquely identifies this warning condition.
        /// </summary>
        public string FullyQualifiedWarningId
        {
            get
            {
                return _fullyQualifiedWarningId ?? string.Empty;
            }
        }

        private readonly string _fullyQualifiedWarningId;
    }

    /// <summary>
    /// A debug record in the PSInformationalBuffers.
    /// </summary>
    [DataContract]
    public class DebugRecord : InformationalRecord
    {
        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public DebugRecord(string message)
            : base(message)
        { }

        /// <summary>
        /// </summary>
        /// <param name="record"></param>
        public DebugRecord(PSObject record)
            : base(record)
        { }
    }

    /// <summary>
    /// A verbose record in the PSInformationalBuffers.
    /// </summary>
    [DataContract]
    public class VerboseRecord : InformationalRecord
    {
        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public VerboseRecord(string message)
            : base(message)
        { }

        /// <summary>
        /// </summary>
        /// <param name="record"></param>
        public VerboseRecord(PSObject record)
            : base(record)
        { }
    }
}
