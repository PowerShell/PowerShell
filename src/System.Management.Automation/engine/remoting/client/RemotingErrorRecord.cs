// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Remoting;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Error record in remoting cases.
    /// </summary>
    public class RemotingErrorRecord : ErrorRecord
    {
        /// <summary>
        /// Contains the origin information.
        /// </summary>
        public OriginInfo OriginInfo
        {
            get
            {
                return _originInfo;
            }
        }

        private readonly OriginInfo _originInfo;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="errorRecord">The error record that is wrapped.</param>
        /// <param name="originInfo">Origin information.</param>
        public RemotingErrorRecord(ErrorRecord errorRecord, OriginInfo originInfo)
            : this(errorRecord, originInfo, null) { }

        /// <summary>
        /// Constructor that is used to wrap an error record.
        /// </summary>
        /// <param name="errorRecord"></param>
        /// <param name="originInfo"></param>
        /// <param name="replaceParentContainsErrorRecordException"></param>
        private RemotingErrorRecord(
            ErrorRecord errorRecord,
            OriginInfo originInfo,
            Exception replaceParentContainsErrorRecordException)
            : base(errorRecord, replaceParentContainsErrorRecordException)
        {
            if (errorRecord != null)
            {
                base.SetInvocationInfo(errorRecord.InvocationInfo);
            }

            _originInfo = originInfo;
        }

        #region ISerializable implementation

        /// <summary>
        /// Deserializer constructor.
        /// </summary>
        /// <param name="info">Serializer information.</param>
        /// <param name="context">Streaming context.</param>
        protected RemotingErrorRecord(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Override

        /// <summary>
        /// Wrap the current ErrorRecord instance.
        /// </summary>
        /// <param name="replaceParentContainsErrorRecordException">
        /// If the wrapped exception contains a ParentContainsErrorRecordException, the new
        /// ErrorRecord should have this exception as its Exception instead.
        /// </param>
        /// <returns></returns>
        internal override ErrorRecord WrapException(Exception replaceParentContainsErrorRecordException)
        {
            return new RemotingErrorRecord(this, this.OriginInfo, replaceParentContainsErrorRecordException);
        }

        #endregion Override
    }

    /// <summary>
    /// Progress record containing origin information.
    /// </summary>
    [DataContract()]
    public class RemotingProgressRecord : ProgressRecord
    {
        /// <summary>
        /// Contains the origin information.
        /// </summary>
        public OriginInfo OriginInfo
        {
            get { return _originInfo; }
        }

        [DataMemberAttribute()]
        private readonly OriginInfo _originInfo;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="progressRecord">The progress record that is wrapped.</param>
        /// <param name="originInfo">Origin information.</param>
        public RemotingProgressRecord(ProgressRecord progressRecord, OriginInfo originInfo)
            : base(
                  Validate(progressRecord).ActivityId,
                  Validate(progressRecord).Activity,
                  Validate(progressRecord).StatusDescription)
        {
            _originInfo = originInfo;
            if (progressRecord != null)
            {
                this.PercentComplete = progressRecord.PercentComplete;
                this.ParentActivityId = progressRecord.ParentActivityId;
                this.RecordType = progressRecord.RecordType;
                this.SecondsRemaining = progressRecord.SecondsRemaining;
                if (!string.IsNullOrEmpty(progressRecord.CurrentOperation))
                {
                    this.CurrentOperation = progressRecord.CurrentOperation;
                }
            }
        }

        private static ProgressRecord Validate(ProgressRecord progressRecord)
        {
            ArgumentNullException.ThrowIfNull(progressRecord);
            return progressRecord;
        }
    }

    /// <summary>
    /// Warning record containing origin information.
    /// </summary>
    [DataContract()]
    public class RemotingWarningRecord : WarningRecord
    {
        /// <summary>
        /// Contains the origin information.
        /// </summary>
        public OriginInfo OriginInfo
        {
            get { return _originInfo; }
        }

        [DataMemberAttribute()]
        private readonly OriginInfo _originInfo;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">The warning message that is wrapped.</param>
        /// <param name="originInfo">The origin information.</param>
        public RemotingWarningRecord(string message, OriginInfo originInfo)
            : base(message)
        {
            _originInfo = originInfo;
        }

        /// <summary>
        /// Constructor taking WarningRecord to wrap and OriginInfo.
        /// </summary>
        /// <param name="warningRecord">WarningRecord to wrap.</param>
        /// <param name="originInfo">OriginInfo.</param>
        internal RemotingWarningRecord(
            WarningRecord warningRecord,
            OriginInfo originInfo)
            : base(warningRecord.FullyQualifiedWarningId, warningRecord.Message)
        {
            _originInfo = originInfo;
        }
    }

    /// <summary>
    /// Debug record containing origin information.
    /// </summary>
    [DataContract()]
    public class RemotingDebugRecord : DebugRecord
    {
        /// <summary>
        /// Contains the origin information.
        /// </summary>
        public OriginInfo OriginInfo
        {
            get { return _originInfo; }
        }

        [DataMemberAttribute()]
        private readonly OriginInfo _originInfo;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">The debug message that is wrapped.</param>
        /// <param name="originInfo">The origin information.</param>
        public RemotingDebugRecord(string message, OriginInfo originInfo)
            : base(message)
        {
            _originInfo = originInfo;
        }
    }

    /// <summary>
    /// Verbose record containing origin information.
    /// </summary>
    [DataContract()]
    public class RemotingVerboseRecord : VerboseRecord
    {
        /// <summary>
        /// Contains the origin information.
        /// </summary>
        public OriginInfo OriginInfo
        {
            get { return _originInfo; }
        }

        [DataMemberAttribute()]
        private readonly OriginInfo _originInfo;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">The verbose message that is wrapped.</param>
        /// <param name="originInfo">The origin information.</param>
        public RemotingVerboseRecord(string message, OriginInfo originInfo)
            : base(message)
        {
            _originInfo = originInfo;
        }
    }

    /// <summary>
    /// Information record containing origin information.
    /// </summary>
    [DataContract()]
    public class RemotingInformationRecord : InformationRecord
    {
        /// <summary>
        /// Contains the origin information.
        /// </summary>
        public OriginInfo OriginInfo
        {
            get { return _originInfo; }
        }

        [DataMemberAttribute()]
        private readonly OriginInfo _originInfo;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="record">The Information message that is wrapped.</param>
        /// <param name="originInfo">The origin information.</param>
        public RemotingInformationRecord(InformationRecord record, OriginInfo originInfo)
            : base(record)
        {
            _originInfo = originInfo;
        }
    }
}

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Contains OriginInfo for an error record.
    /// </summary>
    /// <remarks>This class should only be used when
    /// defining origin information for error records.
    /// In case of output objects, the information
    /// should directly be added to the object as
    /// properties</remarks>
    [Serializable]
    [DataContract()]
    public class OriginInfo
    {
        /// <summary>
        /// The HostEntry information for the machine on
        /// which this information originated.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "PSIP")]
        public string PSComputerName
        {
            get
            {
                return _computerName;
            }
        }

        [DataMemberAttribute()]
        private readonly string _computerName;

        /// <summary>
        /// Runspace instance ID.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ID")]
        public Guid RunspaceID
        {
            get
            {
                return _runspaceID;
            }
        }

        [DataMemberAttribute()]
        private readonly Guid _runspaceID;

        /// <summary>
        /// Error record source instance ID.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ID")]
        public Guid InstanceID
        {
            get
            {
                return _instanceId;
            }

            set
            {
                _instanceId = value;
            }
        }

        [DataMemberAttribute()]
        private Guid _instanceId;

        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="computerName">Machine name.</param>
        /// <param name="runspaceID">Instance id of runspace.</param>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ID")]
        public OriginInfo(string computerName, Guid runspaceID)
            : this(computerName, runspaceID, Guid.Empty)
        { }

        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="computerName">Machine name.</param>
        /// <param name="runspaceID">Instance id of runspace.</param>
        /// <param name="instanceID">Instance id for the origin object.</param>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ID")]
        public OriginInfo(string computerName, Guid runspaceID, Guid instanceID)
        {
            _computerName = computerName;
            _runspaceID = runspaceID;
            _instanceId = instanceID;
        }

        /// <summary>
        /// Overridden ToString() method.
        /// </summary>
        /// <returns>Returns the computername.</returns>
        public override string ToString()
        {
            return PSComputerName;
        }
    }
}
