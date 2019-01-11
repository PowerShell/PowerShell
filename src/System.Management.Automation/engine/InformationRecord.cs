// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using System.Collections.Generic;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines a data structure used to represent informational context destined for the host or user.
    /// </summary>
    /// <remarks>
    /// InformationRecords are passed to <see cref="System.Management.Automation.Cmdlet.WriteInformation(Object, string[])"/>,
    /// which, according to host or user preference, forwards that information on to the host for rendering to the user.
    /// </remarks>
    /// <seealso cref="System.Management.Automation.Cmdlet.WriteInformation(Object, string[])"/>

    [DataContract()]
    public class InformationRecord
    {
        /// <summary>
        /// Initializes a new instance of the InformationRecord class.
        /// </summary>
        /// <param name="messageData">The object to be transmitted to the host.</param>
        /// <param name="source">The source of the message (i.e.: script path, function name, etc.).</param>
        public InformationRecord(Object messageData, string source)
        {
            this.MessageData = messageData;
            this.Source = source;

            this.TimeGenerated = DateTime.Now;
            this.NativeThreadId = PsUtils.GetNativeThreadId();
            this.ManagedThreadId = (uint)System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        private InformationRecord() { }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        internal InformationRecord(InformationRecord baseRecord)
        {
            this.MessageData = baseRecord.MessageData;
            this.Source = baseRecord.Source;

            this.TimeGenerated = baseRecord.TimeGenerated;
            this.Tags = baseRecord.Tags;
            this.User = baseRecord.User;
            this.Computer = baseRecord.Computer;
            this.ProcessId = baseRecord.ProcessId;
            this.NativeThreadId = baseRecord.NativeThreadId;
            this.ManagedThreadId = baseRecord.ManagedThreadId;
        }

        // Some of these setters are internal, while others are public.
        // The ones that are public are left that way because systems that proxy
        // the events may need to alter them (i.e.: workflow). The ones that remain internal
        // are that way because they are fundamental properties of the record itself.

        /// <summary>
        /// The message data for this informational record.
        /// </summary>
        [DataMember]
        public object MessageData { get; internal set; }

        /// <summary>
        /// The source of this informational record (script path, function name, etc.)
        /// </summary>
        [DataMember]
        public string Source { get; set; }

        /// <summary>
        /// The time this informational record was generated.
        /// </summary>
        [DataMember]
        public DateTime TimeGenerated { get; set; }

        /// <summary>
        /// The tags associated with this informational record (if any)
        /// </summary>
        [DataMember]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public List<string> Tags
        {
            get { return _tags ?? (_tags = new List<string>()); }

            internal set { _tags = value; }
        }

        private List<string> _tags;

        /// <summary>
        /// The user that generated this informational record.
        /// </summary>
        [DataMember]
        public string User
        {
            get
            {
                if (this._user == null)
                {
                    // domain\user on Windows, just user on Unix
#if UNIX
                    this._user = Platform.Unix.UserName;
#else
                    this._user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
#endif
                }

                return _user;
            }

            set { _user = value; }
        }

        private string _user;

        /// <summary>
        /// The computer that generated this informational record.
        /// </summary>
        [DataMember]
        public string Computer
        {
            get { return this._computerName ?? (this._computerName = PsUtils.GetHostName()); }

            set { this._computerName = value; }
        }

        private string _computerName;

        /// <summary>
        /// The process that generated this informational record.
        /// </summary>
        [DataMember]
        public uint ProcessId
        {
            get
            {
                if (!this._processId.HasValue)
                {
                    this._processId = (uint) System.Diagnostics.Process.GetCurrentProcess().Id;
                }

                return this._processId.Value;
            }

            set { _processId = value; }
        }

        private uint? _processId;

        /// <summary>
        /// The native thread that generated this informational record.
        /// </summary>
        public uint NativeThreadId { get; set; }

        /// <summary>
        /// The managed thread that generated this informational record.
        /// </summary>
        [DataMember]
        public uint ManagedThreadId { get; set; }

        /// <summary>
        /// Converts an InformationRecord to a string-based representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (MessageData != null)
            {
                return MessageData.ToString();
            }
            else
            {
                return base.ToString();
            }
        }

        internal static InformationRecord FromPSObjectForRemoting(PSObject inputObject)
        {
            InformationRecord informationRecord = new InformationRecord();

            informationRecord.MessageData = RemotingDecoder.GetPropertyValue<Object>(inputObject, "MessageData");
            informationRecord.Source = RemotingDecoder.GetPropertyValue<string>(inputObject, "Source");
            informationRecord.TimeGenerated = RemotingDecoder.GetPropertyValue<DateTime>(inputObject, "TimeGenerated");

            informationRecord.Tags = new List<string>();
            System.Collections.ArrayList tagsArrayList = RemotingDecoder.GetPropertyValue<System.Collections.ArrayList>(inputObject, "Tags");
            foreach (string tag in tagsArrayList)
            {
                informationRecord.Tags.Add(tag);
            }

            informationRecord.User = RemotingDecoder.GetPropertyValue<string>(inputObject, "User");
            informationRecord.Computer = RemotingDecoder.GetPropertyValue<string>(inputObject, "Computer");
            informationRecord.ProcessId = RemotingDecoder.GetPropertyValue<uint>(inputObject, "ProcessId");
            informationRecord.NativeThreadId = RemotingDecoder.GetPropertyValue<uint>(inputObject, "NativeThreadId");
            informationRecord.ManagedThreadId = RemotingDecoder.GetPropertyValue<uint>(inputObject, "ManagedThreadId");

            return informationRecord;
        }

        /// <summary>
        /// Returns this object as a PSObject property bag
        /// that can be used in a remoting protocol data object.
        /// </summary>
        /// <returns>This object as a PSObject property bag.</returns>
        internal PSObject ToPSObjectForRemoting()
        {
            PSObject informationAsPSObject = RemotingEncoder.CreateEmptyPSObject();

            informationAsPSObject.Properties.Add(new PSNoteProperty("MessageData", this.MessageData));
            informationAsPSObject.Properties.Add(new PSNoteProperty("Source", this.Source));
            informationAsPSObject.Properties.Add(new PSNoteProperty("TimeGenerated", this.TimeGenerated));
            informationAsPSObject.Properties.Add(new PSNoteProperty("Tags", this.Tags));
            informationAsPSObject.Properties.Add(new PSNoteProperty("User", this.User));
            informationAsPSObject.Properties.Add(new PSNoteProperty("Computer", this.Computer));
            informationAsPSObject.Properties.Add(new PSNoteProperty("ProcessId", this.ProcessId));
            informationAsPSObject.Properties.Add(new PSNoteProperty("NativeThreadId", this.NativeThreadId));
            informationAsPSObject.Properties.Add(new PSNoteProperty("ManagedThreadId", this.ManagedThreadId));

            return informationAsPSObject;
        }
    }

    /// <summary>
    /// Class that holds informational messages to represent output created by the
    /// Write-Host cmdlet.
    /// </summary>
    public class HostInformationMessage
    {
        /// <summary>
        /// The message being output by the host.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 'True' if the host should not append a NewLine to the message output.
        /// </summary>
        public bool? NoNewLine { get; set; }

        /// <summary>
        /// The foreground color of the message.
        /// </summary>
        public ConsoleColor? ForegroundColor { get; set; }

        /// <summary>
        /// The background color of the message.
        /// </summary>
        public ConsoleColor? BackgroundColor { get; set; }

        /// <summary>
        /// Returns a string-based representation of the host information message.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Message;
        }
    }
}
