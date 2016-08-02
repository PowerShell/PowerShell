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

        private String _severity = "";
        internal String Severity
        {
            get
            {
                return _severity;
            }
            set
            {
                _severity = value;
            }
        }

        private string _hostName = "";

        /// <summary>
        /// Name of the host.
        /// </summary>
        /// <value></value>
        internal string HostName
        {
            get
            {
                return _hostName;
            }
            set
            {
                _hostName = value;
            }
        }

        private string _hostVersion = "";

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
        internal string HostVersion
        {
            get
            {
                return _hostVersion;
            }
            set
            {
                _hostVersion = value;
            }
        }

        private string _hostId = "";

        /// <summary>
        /// Id of the host that is hosting current monad engine.
        /// </summary>
        /// <value></value>
        internal string HostId
        {
            get
            {
                return _hostId;
            }
            set
            {
                _hostId = value;
            }
        }

        private string _engineVersion = "";

        /// <summary>
        /// Version of monad engine.
        /// </summary>
        /// <value></value>
        internal string EngineVersion
        {
            get
            {
                return _engineVersion;
            }
            set
            {
                _engineVersion = value;
            }
        }

        private string _runspaceId = "";

        /// <summary>
        /// Id for currently running runspace
        /// </summary>
        /// <value></value>
        internal string RunspaceId
        {
            get
            {
                return _runspaceId;
            }
            set
            {
                _runspaceId = value;
            }
        }

        private string _pipelineId = "";

        /// <summary>
        /// PipelineId of current running pipeline
        /// </summary>
        /// <value></value>
        internal string PipelineId
        {
            get
            {
                return _pipelineId;
            }
            set
            {
                _pipelineId = value;
            }
        }

        private string _commandName = "";

        /// <summary>
        /// Command text that is typed in from commandline
        /// </summary>
        /// <value></value>
        internal string CommandName
        {
            get
            {
                return _commandName;
            }
            set
            {
                _commandName = value;
            }
        }

        private string _commandType = "";

        /// <summary>
        /// Type of the command, which can be Alias, CommandLet, Script, Application, etc.
        /// 
        /// The value of this property is a usually coversion of CommandTypes enum into a string.
        /// </summary>
        /// <value></value>
        internal string CommandType
        {
            get
            {
                return _commandType;
            }
            set
            {
                _commandType = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private string _scriptName = "";

        /// <summary>
        /// Script file name if current command is executed as a result of script run.
        /// </summary>
        /// <value></value>
        internal string ScriptName
        {
            get
            {
                return _scriptName;
            }
            set
            {
                _scriptName = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private string _commandPath = "";

        /// <summary>
        /// Path to the command executable file.
        /// </summary>
        /// <value></value>
        internal string CommandPath
        {
            get
            {
                return _commandPath;
            }
            set
            {
                _commandPath = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private string _commandLine = "";

        /// <summary>
        /// Extension for the command executable file.
        /// </summary>
        /// <value></value>
        internal string CommandLine
        {
            get
            {
                return _commandLine;
            }
            set
            {
                _commandLine = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private string _sequenceNumber = "";

        /// <summary>
        /// Sequence Id for the event to be logged.
        /// </summary>
        /// <value></value>
        internal string SequenceNumber
        {
            get
            {
                return _sequenceNumber;
            }
            set
            {
                _sequenceNumber = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private string _user = "";

        /// <summary>
        /// Current user. 
        /// </summary>
        /// <value></value>
        internal string User
        {
            get
            {
                return _user;
            }
            set
            {
                _user = value;
            }
        }

        /// <summary>
        /// The user connected to the machine, if being done with 
        /// PowerShell remoting.
        /// </summary>
        internal string ConnectedUser { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private string _time = "";

        /// <summary>
        /// Event happening time
        /// </summary>
        /// <value></value>
        internal string Time
        {
            get
            {
                return _time;
            }
            set
            {
                _time = value;
            }
        }

        #endregion

        #region Shell Id

        /// <summary>
        /// 
        /// </summary>
        private string _shellId;

        /// <summary>
        /// This property should be filled in when logging api is called directly 
        /// with LogContext (when ExecutionContext is not available). 
        /// </summary>
        internal string ShellId
        {
            get
            {
                return _shellId;
            }
            set
            {
                _shellId = value;
            }
        }

        #endregion

        #region Execution context

        private ExecutionContext _executionContext;

        /// <summary>
        /// Execution context is necessary for GetVariableValue
        /// </summary>
        internal ExecutionContext ExecutionContext
        {
            get
            {
                return _executionContext;
            }
            set
            {
                _executionContext = value;
            }
        }

        #endregion
    }
}
