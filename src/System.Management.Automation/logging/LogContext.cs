/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace System.Management.Automation
{
    /// <summary>
    /// LogContext is the class to keep track of context information for each 
    /// event to be logged.
    /// 
    /// LogContext info is collected by Msh Log Engine and passed on to log provider
    /// interface.
    /// </summary>
    /// 
    internal class LogContext
    {
        #region Context Properties

        internal String Severity { get; set; } = "";

        /// <summary>
        /// Name of the host.
        /// </summary>
        /// <value></value>
        internal string HostName { get; set; } = "";

        /// <summary>
        /// Name of the host application.
        /// </summary>
        /// <value></value>
        internal string HostApplication
        {
            get; set;
        }

        /// <summary>
        /// Version of the host.
        /// </summary>
        /// <value></value>
        internal string HostVersion { get; set; } = "";

        /// <summary>
        /// Id of the host that is hosting current monad engine.
        /// </summary>
        /// <value></value>
        internal string HostId { get; set; } = "";

        /// <summary>
        /// Version of monad engine.
        /// </summary>
        /// <value></value>
        internal string EngineVersion { get; set; } = "";

        /// <summary>
        /// Id for currently running runspace
        /// </summary>
        /// <value></value>
        internal string RunspaceId { get; set; } = "";

        /// <summary>
        /// PipelineId of current running pipeline
        /// </summary>
        /// <value></value>
        internal string PipelineId { get; set; } = "";

        /// <summary>
        /// Command text that is typed in from commandline
        /// </summary>
        /// <value></value>
        internal string CommandName { get; set; } = "";

        /// <summary>
        /// Type of the command, which can be Alias, CommandLet, Script, Application, etc.
        /// 
        /// The value of this property is a usually conversion of CommandTypes enum into a string.
        /// </summary>
        /// <value></value>
        internal string CommandType { get; set; } = "";

        /// <summary>
        /// Script file name if current command is executed as a result of script run.
        /// </summary>
        internal string ScriptName { get; set; } = "";

        /// <summary>
        /// Path to the command executable file.
        /// </summary>
        internal string CommandPath { get; set; } = "";

        /// <summary>
        /// Extension for the command executable file.
        /// </summary>
        internal string CommandLine { get; set; } = "";

        /// <summary>
        /// Sequence Id for the event to be logged.
        /// </summary>
        internal string SequenceNumber { get; set; } = "";

        /// <summary>
        /// Current user. 
        /// </summary>
        internal string User { get; set; } = "";

        /// <summary>
        /// The user connected to the machine, if being done with 
        /// PowerShell remoting.
        /// </summary>
        internal string ConnectedUser { get; set; }

        /// <summary>
        /// Event happening time
        /// </summary>
        internal string Time { get; set; } = "";

        #endregion

        #region Shell Id

        /// <summary>
        /// This property should be filled in when logging api is called directly 
        /// with LogContext (when ExecutionContext is not available). 
        /// </summary>
        internal string ShellId { get; set; }

        #endregion

        #region Execution context

        /// <summary>
        /// Execution context is necessary for GetVariableValue
        /// </summary>
        internal ExecutionContext ExecutionContext { get; set; }

        #endregion
    }
}
