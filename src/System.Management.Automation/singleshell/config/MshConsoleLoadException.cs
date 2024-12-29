// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

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
    public class PSConsoleLoadException : SystemException, IContainsErrorRecord
    {
        /// <summary>
        /// Initiate an instance of PSConsoleLoadException.
        /// </summary>
        public PSConsoleLoadException() : base()
        {
        }

        /// <summary>
        /// Initiate an instance of PSConsoleLoadException.
        /// </summary>
        /// <param name="message">Error message.</param>
        public PSConsoleLoadException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initiate an instance of PSConsoleLoadException.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        public PSConsoleLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
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
                    sb.Append('\n');
                    sb.Append(e.Message);
                }
            }

            _errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(this), "ConsoleLoadFailure", ErrorCategory.ResourceUnavailable, null);
        }

        private readonly Collection<PSSnapInException> _PSSnapInExceptions = new Collection<PSSnapInException>();

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
    }
}
