/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Runtime.Serialization;
using System.Reflection;

#if CORECLR
// Use stubs for SystemException, SerializationInfo and SecurityPermissionAttribute 
using Microsoft.PowerShell.CoreClr.Stubs;
#else
using System.Security.Permissions;
#endif

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Define class for runspace configuration type.
    /// </summary>
    /// <!--
    /// RunspaceConfigurationTypeException is the exception to be thrown when there is no
    /// help found for a topic. 
    /// 
    /// Implementation of RunspaceConfigurationTypeException requires it to 
    ///     1. Implement IContainsErrorRecord, 
    ///     2. ISerializable
    /// -->
    [Serializable]
#if CORECLR
    internal
#else
    public
#endif
    class RunspaceConfigurationTypeException : SystemException, IContainsErrorRecord
    {
        /// <summary>
        /// Initiate an instance for RunspaceConfigurationTypeException
        /// </summary>
        /// <param name="assemblyName">Name of the assembly where <paramref name="typeName"/> is defined.</param>
        /// <param name="typeName">Runspace configuration type</param>
        internal RunspaceConfigurationTypeException(string assemblyName, string typeName) : base()
        {
            _assemblyName = assemblyName;
            _typeName = typeName;
            CreateErrorRecord();
        }

        /// <summary>
        /// Initiate an instance for RunspaceConfigurationTypeException
        /// </summary>
        public RunspaceConfigurationTypeException() : base()
        {
        }

        /// <summary>
        /// Initiate an instance for RunspaceConfigurationTypeException
        /// </summary>
        /// <param name="assemblyName">Name of the assembly where <paramref name="typeName"/> is defined.</param>
        /// <param name="typeName">Runspace configuration type defined in <paramref name="assemblyName"/></param>
        /// <param name="innerException">Inner exception of this exception</param>
        internal RunspaceConfigurationTypeException(string assemblyName, string typeName, Exception innerException) : base(innerException.Message, innerException)
        {
            _assemblyName = assemblyName;
            _typeName = typeName;
            CreateErrorRecord();
        }

        /// <summary>
        /// Initiate an instance for RunspaceConfigurationTypeException
        /// </summary>
        /// <param name="message">Error message</param>
        public RunspaceConfigurationTypeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initiate an instance for RunspaceConfigurationTypeException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception of this exception</param>
        public RunspaceConfigurationTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Create the internal error record based on assembly name and type.
        /// The ErrorRecord created will be stored in the _errorRecord member.
        /// </summary>
        private void CreateErrorRecord()
        {
            if (!String.IsNullOrEmpty(_assemblyName) && !String.IsNullOrEmpty(_typeName))
            {
                _errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(this), "UndefinedRunspaceConfigurationType", ErrorCategory.ResourceUnavailable, null);
                _errorRecord.ErrorDetails = new ErrorDetails(typeof(RunspaceConfigurationTypeException).GetTypeInfo().Assembly, "MiniShellErrors", "UndefinedRunspaceConfigurationType", _assemblyName, _typeName);
            }
        }

        private ErrorRecord _errorRecord;

        /// <summary>
        /// Get the error record embedded in this exception.
        /// </summary>
        public ErrorRecord ErrorRecord
        {
            get
            {
                return _errorRecord;
            }
        }

        private string _assemblyName = "";

        /// <summary>
        /// Get name of the assembly where runspace configuration type is defined.
        /// </summary>
        public string AssemblyName
        {
            get
            {
                return _assemblyName;
            }
        }

        private string _typeName = "";

        /// <summary>
        /// Get the runspace configuration type.
        /// </summary>
        public string TypeName
        {
            get
            {
                return _typeName;
            }
        }

        /// <summary>
        /// Get message for this exception. 
        /// </summary>
        public override string Message
        {
            get
            {
                if (_errorRecord != null)
                {
                    return _errorRecord.ToString();
                }

                return base.Message;
            }
        }

        #region Serialization

        /// <summary>
        /// Initiate a RunspaceConfigurationAttributeException instance. 
        /// </summary>
        /// <param name="info"> Serialization information </param>
        /// <param name="context"> Streaming context </param>
        protected RunspaceConfigurationTypeException(SerializationInfo info,
                                        StreamingContext context)
            : base(info, context)
        {
            _typeName = info.GetString("TypeName");
            _assemblyName = info.GetString("AssemblyName");
        }

        /// <summary>
        /// Get object data from serizliation information.
        /// </summary>
        /// <param name="info"> Serialization information </param>
        /// <param name="context"> Streaming context </param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw PSTraceSource.NewArgumentNullException("info");
            }

            base.GetObjectData(info, context);

            info.AddValue("TypeName", _typeName);
            info.AddValue("AssemblyName", _assemblyName);
        }

        #endregion Serialization
    }
}

