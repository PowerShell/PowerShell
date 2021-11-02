// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Defines exception thrown when a PSSnapin was not able to load into current runspace.
    /// </summary>
    /// <!--
    /// Implementation of PSSnapInException requires it to
    ///     1. Implement IContainsErrorRecord,
    ///     2. ISerializable
    ///
    /// Basic information for this exception includes,
    ///     1. PSSnapin name
    ///     2. Inner exception.
    /// -->
    [Serializable]
    public class PSSnapInException : RuntimeException
    {
        /// <summary>
        /// Initiate an instance of PSSnapInException.
        /// </summary>
        /// <param name="PSSnapin">PSSnapin for the exception.</param>
        /// <param name="message">Message with load failure detail.</param>
        internal PSSnapInException(string PSSnapin, string message)
            : base()
        {
            _PSSnapin = PSSnapin;
            _reason = message;
            CreateErrorRecord();
        }

        /// <summary>
        /// Initiate an instance of PSSnapInException.
        /// </summary>
        /// <param name="PSSnapin">PSSnapin for the exception.</param>
        /// <param name="message">Message with load failure detail.</param>
        /// <param name="warning">Whether this is just a warning for PSSnapin load.</param>
        internal PSSnapInException(string PSSnapin, string message, bool warning)
            : base()
        {
            _PSSnapin = PSSnapin;
            _reason = message;
            _warning = warning;
            CreateErrorRecord();
        }

        /// <summary>
        /// Initiate an instance of PSSnapInException.
        /// </summary>
        /// <param name="PSSnapin">PSSnapin for the exception.</param>
        /// <param name="message">Message with load failure detail.</param>
        /// <param name="exception">Exception for PSSnapin load failure.</param>
        internal PSSnapInException(string PSSnapin, string message, Exception exception)
            : base(message, exception)
        {
            _PSSnapin = PSSnapin;
            _reason = message;
            CreateErrorRecord();
        }

        /// <summary>
        /// Initiate an instance of PSSnapInException.
        /// </summary>
        public PSSnapInException() : base()
        {
        }

        /// <summary>
        /// Initiate an instance of PSSnapInException.
        /// </summary>
        /// <param name="message">Error message.</param>
        public PSSnapInException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initiate an instance of PSSnapInException.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        public PSSnapInException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Create the internal error record.
        /// The ErrorRecord created will be stored in the _errorRecord member.
        /// </summary>
        private void CreateErrorRecord()
        {
            // if _PSSnapin or _reason is empty, this exception is created using default
            // constructor. Don't create the error record since there is
            // no useful information anyway.
            if (!string.IsNullOrEmpty(_PSSnapin) && !string.IsNullOrEmpty(_reason))
            {
                Assembly currentAssembly = typeof(PSSnapInException).Assembly;

                if (_warning)
                {
                    _errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(this), "PSSnapInLoadWarning", ErrorCategory.ResourceUnavailable, null);
                    _errorRecord.ErrorDetails = new ErrorDetails(string.Format(ConsoleInfoErrorStrings.PSSnapInLoadWarning, _PSSnapin, _reason));
                }
                else
                {
                    _errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(this), "PSSnapInLoadFailure", ErrorCategory.ResourceUnavailable, null);
                    _errorRecord.ErrorDetails = new ErrorDetails(string.Format(ConsoleInfoErrorStrings.PSSnapInLoadFailure, _PSSnapin, _reason));
                }
            }
        }

        private readonly bool _warning = false;

        private ErrorRecord _errorRecord;
        private bool _isErrorRecordOriginallyNull;

        /// <summary>
        /// Gets error record embedded in this exception.
        /// </summary>
        /// <!--
        /// This property is required as part of IErrorRecordContainer
        /// interface.
        /// -->
        public override ErrorRecord ErrorRecord
        {
            get
            {
                if (_errorRecord == null)
                {
                    _isErrorRecordOriginallyNull = true;
                    _errorRecord = new ErrorRecord(
                        new ParentContainsErrorRecordException(this),
                        "PSSnapInException",
                        ErrorCategory.NotSpecified,
                        null);
                }

                return _errorRecord;
            }
        }

        private readonly string _PSSnapin = string.Empty;
        private readonly string _reason = string.Empty;

        /// <summary>
        /// Gets message for this exception.
        /// </summary>
        public override string Message
        {
            get
            {
                if (_errorRecord != null && !_isErrorRecordOriginallyNull)
                {
                    return _errorRecord.ToString();
                }

                return base.Message;
            }
        }

        #region Serialization

        /// <summary>
        /// Initiate a PSSnapInException instance.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected PSSnapInException(SerializationInfo info,
                                        StreamingContext context)
            : base(info, context)
        {
            _PSSnapin = info.GetString("PSSnapIn");
            _reason = info.GetString("Reason");

            CreateErrorRecord();
        }

        /// <summary>
        /// Get object data from serialization information.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(info));
            }

            base.GetObjectData(info, context);

            info.AddValue("PSSnapIn", _PSSnapin);
            info.AddValue("Reason", _reason);
        }

        #endregion Serialization
    }
}
