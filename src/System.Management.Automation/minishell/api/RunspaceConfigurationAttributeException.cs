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
    /// Defines exception thrown when runspace configuration attribute is not defined correctly. 
    /// </summary>
    /// <!--
    /// RunspaceConfigurationAttributeException is either
    /// 
    ///     1. no assembly attribute of type RunspaceConfigurationTypeAttribute defined
    ///     2. or multiple assembly attributes of type RunspaceConfigurationTypeAttribute defined
    /// 
    /// Implementation of RunspaceConfigurationAttributeException requires it to 
    ///     1. Implement IContainsErrorRecord, 
    ///     2. ISerializable
    /// -->
    [Serializable]
    public class RunspaceConfigurationAttributeException : SystemException, IContainsErrorRecord
    {
        /// <summary>
        /// Initiate an instance of RunspaceConfigurationAttributeException.
        /// </summary>
        /// <param name="error">Error detail for the exception</param>
        /// <param name="assemblyName">Assembly on which runspace configuration attribute is defined or should be defined.</param>
        internal RunspaceConfigurationAttributeException(string error, string assemblyName) : base()
        {
            _error = error;
            _assemblyName = assemblyName;
            CreateErrorRecord();
        }

        /// <summary>
        /// Initiate an instance of RunspaceConfigurationAttributeException.
        /// </summary>
        public RunspaceConfigurationAttributeException() : base()
        {
        }

        /// <summary>
        /// Initiate an instance of RunspaceConfigurationAttributeException.
        /// </summary>
        /// <param name="message">Error message</param>
        public RunspaceConfigurationAttributeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initiate an instance of RunspaceConfigurationAttributeException.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public RunspaceConfigurationAttributeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initiate an instance of RunspaceConfigurationAttributeException.
        /// </summary>
        /// <param name="error">Error detail</param>
        /// <param name="assemblyName">Assembly on which runspace configuration attribute is defined or should be defined.</param>
        /// <param name="innerException">The inner exception of this exception</param>
        internal RunspaceConfigurationAttributeException(string error, string assemblyName, Exception innerException) : base(innerException.Message, innerException)
        {
            _error = error;
            _assemblyName = assemblyName;
            CreateErrorRecord();
        }

        /// <summary>
        /// Create the internal error record based on helpTopic.
        /// The ErrorRecord created will be stored in the _errorRecord member.
        /// </summary>
        private void CreateErrorRecord()
        {
            // if _error is empty, this exception is created using default
            // constructor. Don't create the error record since there is 
            // no useful information anyway.
            if (!String.IsNullOrEmpty(_error) && !String.IsNullOrEmpty(_assemblyName))
            {
                _errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(this), _error, ErrorCategory.ResourceUnavailable, null);
                _errorRecord.ErrorDetails = new ErrorDetails(typeof(RunspaceConfigurationAttributeException).GetTypeInfo().Assembly, "MiniShellErrors", _error, _assemblyName);
            }
        }

        private ErrorRecord _errorRecord;

        /// <summary>
        /// Gets error record embedded in this exception. 
        /// </summary>
        /// <!--
        /// This property is required as part of IErrorRecordContainer
        /// interface.
        /// -->
        public ErrorRecord ErrorRecord
        {
            get
            {
                return _errorRecord;
            }
        }

        private string _error = "";

        /// <summary>
        /// Get localized error message. 
        /// </summary>
        /// <value>error</value>
        public string Error
        {
            get
            {
                return _error;
            }
        }

        private string _assemblyName = "";

        /// <summary>
        /// Gets assembly name on which runspace configuration attribute is defined or should be defined.
        /// </summary>
        /// <value>error</value>
        public string AssemblyName
        {
            get
            {
                return _assemblyName;
            }
        }

        /// <summary>
        /// Gets message for this exception. 
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
        protected RunspaceConfigurationAttributeException(SerializationInfo info,
                                        StreamingContext context)
            : base(info, context)
        {
            _error = info.GetString("Error");
            _assemblyName = info.GetString("AssemblyName");

            CreateErrorRecord();
        }

        /// <summary>
        /// Get object data from serialization information.
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

            info.AddValue("Error", _error);
            info.AddValue("AssemblyName", _assemblyName);
        }

        #endregion Serialization
    }
}

