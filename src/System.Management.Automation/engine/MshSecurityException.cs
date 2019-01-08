// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace System.Management.Automation
{
    /// <summary>
    /// This is a wrapper for exception class SecurityException.
    /// </summary>
    [Serializable]
    public class PSSecurityException : RuntimeException
    {
        #region ctor
        /// <summary>
        /// Recommended constructor for class PSSecurityException.
        /// </summary>
        /// <returns>Constructed object.</returns>
        public PSSecurityException()
            : base()
        {
            _errorRecord = new ErrorRecord(
                new ParentContainsErrorRecordException(this),
                "UnauthorizedAccess",
                ErrorCategory.SecurityError,
                null);
            _errorRecord.ErrorDetails = new ErrorDetails(SessionStateStrings.CanNotRun);
            _message = _errorRecord.ErrorDetails.Message;
        }

        /// <summary>
        /// Serialization constructor for class PSSecurityException.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        /// <returns>Constructed object.</returns>
        protected PSSecurityException(SerializationInfo info,
                           StreamingContext context)
            : base(info, context)
        {
            _errorRecord = new ErrorRecord(
                new ParentContainsErrorRecordException(this),
                "UnauthorizedAccess",
                ErrorCategory.SecurityError,
                null);
            _errorRecord.ErrorDetails = new ErrorDetails(SessionStateStrings.CanNotRun);
            _message = _errorRecord.ErrorDetails.Message;
            // no fields, nothing more to serialize
            // no need for a GetObjectData implementation
        }

        /// <summary>
        /// Constructor for class PSSecurityException.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public PSSecurityException(string message)
            : base(message)
        {
            _message = message;
            _errorRecord = new ErrorRecord(
                new ParentContainsErrorRecordException(this),
                "UnauthorizedAccess",
                ErrorCategory.SecurityError,
                null);
            _errorRecord.ErrorDetails = new ErrorDetails(message);
        }

        /// <summary>
        /// Constructor for class PSSecurityException.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns>Constructed object.</returns>
        public PSSecurityException(string message,
                                Exception innerException)
            : base(message, innerException)
        {
            _errorRecord = new ErrorRecord(
                new ParentContainsErrorRecordException(this),
                "UnauthorizedAccess",
                ErrorCategory.SecurityError,
                null);
            _errorRecord.ErrorDetails = new ErrorDetails(message);
            _message = _errorRecord.ErrorDetails.Message;
        }
        #endregion ctor

        /// <summary>
        /// Gets the ErrorRecord information for this exception.
        /// </summary>
        public override ErrorRecord ErrorRecord
        {
            get
            {
                if (_errorRecord == null)
                {
                    _errorRecord = new ErrorRecord(
                        new ParentContainsErrorRecordException(this),
                        "UnauthorizedAccess",
                        ErrorCategory.SecurityError,
                        null);
                }

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord;

        /// <summary>
        /// Exception.Message is get-only, but you can effectively
        /// set it in a subclass by overriding this virtual property.
        /// </summary>
        /// <value></value>
        public override string Message
        {
            get { return _message; }
        }

        private string _message;
    }
}

