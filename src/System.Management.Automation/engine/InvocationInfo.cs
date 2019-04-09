// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation.Internal;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Language;

namespace System.Management.Automation
{
    /// <summary>
    /// Describes how and where this command was invoked.
    /// </summary>
    [DebuggerDisplay("Command = {MyCommand}")]
    public class InvocationInfo
    {
        #region Constructors

        /// <summary>
        /// Constructor for InvocationInfo object when the associated command object is present.
        /// </summary>
        /// <param name="command"></param>
        internal InvocationInfo(InternalCommand command)
            : this(command.CommandInfo,
                   command.InvocationExtent ?? PositionUtilities.EmptyExtent)
        {
            CommandOrigin = command.CommandOrigin;
        }

        /// <summary>
        /// Constructor for InvocationInfo object.
        /// </summary>
        /// <param name="commandInfo">
        /// The command information the invocation info represents.
        /// </param>
        /// <param name="scriptPosition">
        /// The position representing the invocation, or the position representing the error.
        /// </param>
        internal InvocationInfo(CommandInfo commandInfo, IScriptExtent scriptPosition)
            : this(commandInfo, scriptPosition, null)
        {
            // nothing to do here
        }

        /// <summary>
        /// Constructor for InvocationInfo object.
        /// </summary>
        /// <param name="commandInfo">
        /// The command information the invocation info represents.
        /// </param>
        /// <param name="scriptPosition">
        /// The position representing the invocation, or the position representing the error.
        /// </param>
        /// <param name="context">
        /// The context in which the InvocationInfo is being created.
        /// </param>
        internal InvocationInfo(CommandInfo commandInfo, IScriptExtent scriptPosition, ExecutionContext context)
        {
            MyCommand = commandInfo;
            CommandOrigin = CommandOrigin.Internal;
            _scriptPosition = scriptPosition;

            ExecutionContext contextToUse = null;
            if ((commandInfo != null) && (commandInfo.Context != null))
            {
                contextToUse = commandInfo.Context;
            }
            else if (context != null)
            {
                contextToUse = context;
            }

            // Populate the history ID of this command
            if (contextToUse != null)
            {
                Runspaces.LocalRunspace localRunspace = contextToUse.CurrentRunspace as Runspaces.LocalRunspace;
                if (localRunspace != null && localRunspace.History != null)
                {
                    HistoryId = localRunspace.History.GetNextHistoryId();
                }
            }
        }

        /// <summary>
        /// Creates an InformationalRecord from an instance serialized as a PSObject by ToPSObjectForRemoting.
        /// </summary>
        internal InvocationInfo(PSObject psObject)
        {
            CommandOrigin = (CommandOrigin)SerializationUtilities.GetPsObjectPropertyBaseObject(psObject, "InvocationInfo_CommandOrigin");
            ExpectingInput = (bool)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_ExpectingInput");
            _invocationName = (string)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_InvocationName");
            HistoryId = (long)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_HistoryId");
            PipelineLength = (int)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_PipelineLength");
            PipelinePosition = (int)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_PipelinePosition");

            string scriptName = (string)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_ScriptName");
            int scriptLineNumber = (int)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_ScriptLineNumber");
            int offsetInLine = (int)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_OffsetInLine");
            string line = (string)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_Line");
            var scriptPosition = new ScriptPosition(scriptName, scriptLineNumber, offsetInLine, line);

            ScriptPosition scriptEndPosition;
            if (!string.IsNullOrEmpty(line))
            {
                int endColumn = line.Length + 1;
                scriptEndPosition = new ScriptPosition(scriptName, scriptLineNumber, endColumn, line);
            }
            else
            {
                scriptEndPosition = scriptPosition;
            }

            _scriptPosition = new ScriptExtent(scriptPosition, scriptEndPosition);

            MyCommand = RemoteCommandInfo.FromPSObjectForRemoting(psObject);

            //
            // Arrays are de-serialized as ArrayList so we need to convert the deserialized
            // object into an int[] before assigning to pipelineIterationInfo.
            //
            var list = (ArrayList)SerializationUtilities.GetPsObjectPropertyBaseObject(psObject, "InvocationInfo_PipelineIterationInfo");
            if (list != null)
            {
                PipelineIterationInfo = (int[])list.ToArray(typeof(int));
            }
            else
            {
                PipelineIterationInfo = Array.Empty<int>();
            }

            //
            // Dictionaries are de-serialized as Hashtables so we need to convert the deserialized object into a dictionary
            // before assigning to CommandLineParameters.
            //
            Hashtable hashtable = (Hashtable)SerializationUtilities.GetPsObjectPropertyBaseObject(psObject, "InvocationInfo_BoundParameters");

            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            if (hashtable != null)
            {
                foreach (DictionaryEntry entry in hashtable)
                {
                    dictionary.Add((string)entry.Key, entry.Value);
                }
            }

            _boundParameters = dictionary;

            //
            // The unbound parameters are de-serialized as an ArrayList, which we need to convert to a List
            //
            var unboundArguments = (ArrayList)SerializationUtilities.GetPsObjectPropertyBaseObject(psObject, "InvocationInfo_UnboundArguments");

            _unboundArguments = new List<object>();

            if (unboundArguments != null)
            {
                foreach (object o in unboundArguments)
                {
                    _unboundArguments.Add(o);
                }
            }

            object value = SerializationUtilities.GetPropertyValue(psObject, "SerializeExtent");
            bool serializeExtent = false;

            if (value != null)
                serializeExtent = (bool)value;

            if (serializeExtent)
                DisplayScriptPosition = ScriptExtent.FromPSObjectForRemoting(psObject);
        }

        #endregion Constructors

        #region Private Data

        private IScriptExtent _scriptPosition;
        private string _invocationName;
        private Dictionary<string, object> _boundParameters;
        private List<object> _unboundArguments;

        #endregion Internal or Private

        #region Public Members

        /// <summary>
        /// Provide basic information about the command.
        /// </summary>
        /// <value>may be null</value>
        public CommandInfo MyCommand { get; }

        /// <summary>
        /// This member provides a dictionary of the parameters that were bound for this
        /// script or command.
        /// </summary>
        public Dictionary<string, object> BoundParameters
        {
            get {
                return _boundParameters ??
                       (_boundParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
            }

            internal set { _boundParameters = value; }
        }

        /// <summary>
        /// This member provides a list of the arguments that were not bound to any parameter.
        /// </summary>
        public List<object> UnboundArguments
        {
            get { return _unboundArguments ?? (_unboundArguments = new List<object>()); }

            internal set { _unboundArguments = value; }
        }

        /// <summary>
        /// The line number in the executing script that contained this cmdlet.
        /// </summary>
        /// <value>The script line number or -1 if not executing in a script.</value>
        public int ScriptLineNumber
        {
            get { return ScriptPosition.StartLineNumber; }
        }

        /// <summary>
        /// Command's character offset in that line. If the command was
        /// executed directly through the host interfaces, this will be -1.
        /// </summary>
        /// <value>The line offset or -1 if not executed from a text line.</value>
        public int OffsetInLine
        {
            get { return ScriptPosition.StartColumnNumber; }
        }

        /// <summary>
        /// History ID that represents the command. If unavailable, this will be -1.
        /// </summary>
        /// <value>The history ID or -1 if not available.</value>
        public long HistoryId { get; internal set; } = -1;

        /// <summary>
        /// The name of the script containing the cmdlet.
        /// </summary>
        /// <value>The script name or "" if there was no script.</value>
        public string ScriptName
        {
            get { return ScriptPosition.File ?? string.Empty; }
        }

        /// <summary>
        /// The text of the line that contained this cmdlet invocation.
        /// </summary>
        /// <value>Line that was entered to invoke this command</value>
        public string Line
        {
            get
            {
                if (ScriptPosition.StartScriptPosition != null)
                {
                    return ScriptPosition.StartScriptPosition.Line;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Formatted message indicating where the cmdlet appeared
        /// in the line.
        /// </summary>
        /// <value>Formatted string indicating the command's position in the line</value>
        public string PositionMessage
        {
            get { return PositionUtilities.VerboseMessage(ScriptPosition); }
        }

        /// <summary>
        /// This property tells you the directory from where you were being invoked.
        /// </summary>
        public string PSScriptRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(ScriptPosition.File))
                {
                    return Path.GetDirectoryName(ScriptPosition.File);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// This property tells you the full path to the command from where you were being invoked.
        /// </summary>
        public string PSCommandPath
        {
            get { return ScriptPosition.File; }
        }

        /// <summary>
        /// Command name used to invoke this string - if invoked through an alias, then
        /// this would be the alias name.
        /// </summary>
        /// <value>The name string.</value>
        public string InvocationName
        {
            get { return _invocationName ?? string.Empty; }

            internal set { _invocationName = value; }
        }

        /// <summary>
        /// How many elements are in the containing pipeline.
        /// </summary>
        /// <value>number of elements in the containing pipeline</value>
        public int PipelineLength { get; internal set; }

        /// <summary>
        /// Which element this command was in the containing pipeline.
        /// </summary>
        /// <value>which element this command was in the containing pipeline</value>
        public int PipelinePosition { get; internal set; }

        /// <summary>
        /// Is true if this command is expecting input...
        /// </summary>
        public bool ExpectingInput { get; internal set; }

        /// <summary>
        /// This property tells you if you were being invoked inside the runspace or
        /// if it was an external request.
        /// </summary>
        public CommandOrigin CommandOrigin { get; internal set; }

        /// <summary>
        /// The position for the invocation or error.
        /// </summary>
        public IScriptExtent DisplayScriptPosition { get; set; }

        /// <summary>
        /// Create.
        /// </summary>
        /// <param name="commandInfo"></param>
        /// <param name="scriptPosition"></param>
        /// <returns></returns>
        public static InvocationInfo Create(
            CommandInfo commandInfo,
            IScriptExtent scriptPosition)
        {
            var invocationInfo = new InvocationInfo(commandInfo, scriptPosition);
            invocationInfo.DisplayScriptPosition = scriptPosition;

            return invocationInfo;
        }

        #endregion Public Members

        #region Internal Members

        /// <summary>
        /// The position for the invocation or error.
        /// </summary>
        internal IScriptExtent ScriptPosition
        {
            get
            {
                if (DisplayScriptPosition != null)
                {
                    return DisplayScriptPosition;
                }
                else
                {
                    return _scriptPosition;
                }
            }

            set { _scriptPosition = value; }
        }

        /// <summary>
        /// Returns the full text of the script for this invocation info.
        /// </summary>
        internal string GetFullScript()
        {
            return (ScriptPosition != null) && (ScriptPosition.StartScriptPosition != null) ?
                ScriptPosition.StartScriptPosition.GetFullScript() : null;
        }

        /// <summary>
        /// Index of the ProcessRecord iteration for each of the commands in the pipeline.
        /// </summary>
        /// <remarks>
        /// All the commands in a given pipeline share the same PipelinePositionInfo.
        /// </remarks>
        internal int[] PipelineIterationInfo { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Adds the information about this informational record to a PSObject as note properties.
        /// The PSObject is used to serialize the record during remote operations.
        /// </summary>
        /// <remarks>
        /// InvocationInfos are usually serialized as part of another object, so we add "InvocationInfo_" to
        /// the note properties to prevent collisions with any properties set by the containing object.
        /// </remarks>
        internal void ToPSObjectForRemoting(PSObject psObject)
        {
            RemotingEncoder.AddNoteProperty<object>(psObject, "InvocationInfo_BoundParameters", () => this.BoundParameters);
            RemotingEncoder.AddNoteProperty<CommandOrigin>(psObject, "InvocationInfo_CommandOrigin", () => this.CommandOrigin);
            RemotingEncoder.AddNoteProperty<bool>(psObject, "InvocationInfo_ExpectingInput", () => this.ExpectingInput);
            RemotingEncoder.AddNoteProperty<string>(psObject, "InvocationInfo_InvocationName", () => this.InvocationName);
            RemotingEncoder.AddNoteProperty<string>(psObject, "InvocationInfo_Line", () => this.Line);
            RemotingEncoder.AddNoteProperty<int>(psObject, "InvocationInfo_OffsetInLine", () => this.OffsetInLine);
            RemotingEncoder.AddNoteProperty<long>(psObject, "InvocationInfo_HistoryId", () => this.HistoryId);
            RemotingEncoder.AddNoteProperty<int[]>(psObject, "InvocationInfo_PipelineIterationInfo", () => this.PipelineIterationInfo);
            RemotingEncoder.AddNoteProperty<int>(psObject, "InvocationInfo_PipelineLength", () => this.PipelineLength);
            RemotingEncoder.AddNoteProperty<int>(psObject, "InvocationInfo_PipelinePosition", () => this.PipelinePosition);
            RemotingEncoder.AddNoteProperty<string>(psObject, "InvocationInfo_PSScriptRoot", () => this.PSScriptRoot);
            RemotingEncoder.AddNoteProperty<string>(psObject, "InvocationInfo_PSCommandPath", () => this.PSCommandPath);
            // PositionMessage is ignored when deserializing because it is synthesized from the other position related fields, but
            // it is serialized for backwards compatibility.
            RemotingEncoder.AddNoteProperty<string>(psObject, "InvocationInfo_PositionMessage", () => this.PositionMessage);
            RemotingEncoder.AddNoteProperty<int>(psObject, "InvocationInfo_ScriptLineNumber", () => this.ScriptLineNumber);
            RemotingEncoder.AddNoteProperty<string>(psObject, "InvocationInfo_ScriptName", () => this.ScriptName);
            RemotingEncoder.AddNoteProperty<object>(psObject, "InvocationInfo_UnboundArguments", () => this.UnboundArguments);

            ScriptExtent extent = DisplayScriptPosition as ScriptExtent;
            if (extent != null)
            {
                extent.ToPSObjectForRemoting(psObject);
                RemotingEncoder.AddNoteProperty(psObject, "SerializeExtent", () => true);
            }
            else
            {
                RemotingEncoder.AddNoteProperty(psObject, "SerializeExtent", () => false);
            }

            RemoteCommandInfo.ToPSObjectForRemoting(this.MyCommand, psObject);
        }

        #endregion Internal Members
    }

    /// <summary>
    /// A CommandInfo that has been serialized/deserialized as part of an InvocationInfo during a remote invocation.
    /// </summary>
    public class RemoteCommandInfo : CommandInfo
    {
        /// <summary>
        /// </summary>
        private RemoteCommandInfo(string name, CommandTypes type)
            : base(name, type)
        {
            // nothing to do here
        }

        /// <summary>
        /// A string representing the definition of the command.
        /// </summary>
        public override string Definition { get { return _definition; } }

        /// <summary>
        /// Creates a RemoteCommandInfo from an instance serialized as a PSObject by ToPSObjectForRemoting.
        /// </summary>
        internal static RemoteCommandInfo FromPSObjectForRemoting(PSObject psObject)
        {
            RemoteCommandInfo commandInfo = null;

            object ctype = SerializationUtilities.GetPsObjectPropertyBaseObject(psObject, "CommandInfo_CommandType");

            if (ctype != null)
            {
                CommandTypes type = RemotingDecoder.GetPropertyValue<CommandTypes>(psObject, "CommandInfo_CommandType");
                string name = RemotingDecoder.GetPropertyValue<string>(psObject, "CommandInfo_Name");

                commandInfo = new RemoteCommandInfo(name, type);
                commandInfo._definition = RemotingDecoder.GetPropertyValue<string>(psObject, "CommandInfo_Definition");
                commandInfo.Visibility = RemotingDecoder.GetPropertyValue<SessionStateEntryVisibility>(psObject, "CommandInfo_Visibility");
            }

            return commandInfo;
        }

        /// <summary>
        /// Adds the information about this instance to a PSObject as note properties.
        /// The PSObject is used to serialize the CommandInfo during remote operations.
        /// </summary>
        /// <remarks>
        /// CommandInfos are usually serialized as part of InvocationInfos, so we add "CommandInfo_" to
        /// the note properties to prevent collisions with any properties set by the containing object.
        /// </remarks>
        internal static void ToPSObjectForRemoting(CommandInfo commandInfo, PSObject psObject)
        {
            if (commandInfo != null)
            {
                RemotingEncoder.AddNoteProperty<CommandTypes>(psObject, "CommandInfo_CommandType", () => commandInfo.CommandType);
                RemotingEncoder.AddNoteProperty<string>(psObject, "CommandInfo_Definition", () => commandInfo.Definition);
                RemotingEncoder.AddNoteProperty<string>(psObject, "CommandInfo_Name", () => commandInfo.Name);
                RemotingEncoder.AddNoteProperty<SessionStateEntryVisibility>(psObject, "CommandInfo_Visibility", () => commandInfo.Visibility);
            }
        }

        /// <summary>
        /// NYI.
        /// </summary>
        public override ReadOnlyCollection<PSTypeName> OutputType
        {
            get { return null; }
        }

        private string _definition;
    }
}

