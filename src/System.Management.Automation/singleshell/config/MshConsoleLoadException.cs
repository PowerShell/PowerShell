/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Runtime.Serialization;
using System.Text;
using System.Collections.ObjectModel;

#if CORECLR
// Use stubs for SystemException, SerializationInfo and SecurityPermissionAttribute 
using Microsoft.PowerShell.CoreClr.Stubs;
#else
using System.Security.Permissions;
#endif

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Defines exception thrown when a PSSnapin was not able to load into current runspace. 
    /// </summary>
    /// <!--
    /// Implementation of PSConsoleLoadException requires it to 
    ///     1. Implement IContainsErrorRecord, 
    ///     2. ISerializable
    /// 
    /// Basic information for this exception includes, 
    ///     1. PSSnapin name
    ///     2. Inner exception.
    /// -->
    [Serializable]
    public class PSConsoleLoadException : SystemException, IContainsErrorRecord
    {
        /// <summary>
        /// Initiate an instance of PSConsoleLoadException.
        /// </summary>
        /// <param name="consoleInfo">Console info object for the exception</param>
        /// <param name="exceptions">A collection of PSSnapInExceptions.</param>
        internal PSConsoleLoadException(MshConsoleInfo consoleInfo, Collection<PSSnapInException> exceptions)
            : base()
        {
            if (!String.IsNullOrEmpty(consoleInfo.Filename))
                _consoleFileName = consoleInfo.Filename;

            if (exceptions != null)
            {
                _PSSnapInExceptions = exceptions;
            }

            CreateErrorRecord();
        }

        /// <summary>
        /// Initiate an instance of PSConsoleLoadException.
        /// </summary>
        public PSConsoleLoadException() : base()
        {
        }

        /// <summary>
        /// Initiate an instance of PSConsoleLoadException.
        /// </summary>
        /// <param name="message">Error message</param>
        public PSConsoleLoadException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initiate an instance of PSConsoleLoadException.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public PSConsoleLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Create the internal error record.
        /// The ErrorRecord created will be stored in the _errorRecord member.
        /// </summary>
        private void CreateErrorRecord()
        {
            StringBuilder sb = new StringBuilder();

            if (PSSnapInExceptions != null)
            {
                foreach (PSSnapInException e in PSSnapInExceptions)
                {
                    sb.Append("\n");
                    sb.Append(e.Message);
                }
            }

            _errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(this), "ConsoleLoadFailure", ErrorCategory.ResourceUnavailable, null);
            _errorRecord.ErrorDetails = new ErrorDetails(String.Format(ConsoleInfoErrorStrings.ConsoleLoadFailure, _consoleFileName, sb.ToString()));
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

        private string _consoleFileName = "";

        private Collection<PSSnapInException> _PSSnapInExceptions = new Collection<PSSnapInException>();
        internal Collection<PSSnapInException> PSSnapInExceptions
        {
            get
            {
                return _PSSnapInExceptions;
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
                else
                {
                    return base.Message;
                }
            }
        }

        #region Serialization

        /// <summary>
        /// Initiate a PSConsoleLoadException instance. 
        /// </summary>
        /// <param name="info"> Serialization information </param>
        /// <param name="context"> Streaming context </param>
        protected PSConsoleLoadException(SerializationInfo info,
                                        StreamingContext context)
            : base(info, context)
        {
            _consoleFileName = info.GetString("ConsoleFileName");

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

            info.AddValue("ConsoleFileName", _consoleFileName);
        }

        #endregion Serialization
    }
}

