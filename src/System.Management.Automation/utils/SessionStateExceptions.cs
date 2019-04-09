// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Management.Automation.Internal;
using System.Security.Permissions;

namespace System.Management.Automation
{
    /// <summary>
    /// An exception that wraps all exceptions that are thrown by providers. This allows
    /// callers of the provider APIs to be able to catch a single exception no matter
    /// what any of the various providers may have thrown.
    /// </summary>
    [Serializable]
    public class ProviderInvocationException : RuntimeException
    {
        #region Constructors
        /// <summary>
        /// Constructs a ProviderInvocationException.
        /// </summary>
        public ProviderInvocationException() : base()
        {
        }

        /// <summary>
        /// Constructs a ProviderInvocationException using serialized data.
        /// </summary>
        /// <param name="info">
        /// serialization information
        /// </param>
        /// <param name="context">
        /// streaming context
        /// </param>
        protected ProviderInvocationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Constructs a ProviderInvocationException with a message.
        /// </summary>
        /// <param name="message">
        /// The message for the exception.
        /// </param>
        public ProviderInvocationException(string message)
            : base(message)
        {
            _message = message;
        }

        /// <summary>
        /// Constructs a ProviderInvocationException with provider information and an inner exception.
        /// </summary>
        /// <param name="provider">
        /// Information about the provider to be used in formatting the message.
        /// </param>
        /// <param name="innerException">
        /// The inner exception for this exception.
        /// </param>
        internal ProviderInvocationException(ProviderInfo provider, Exception innerException)
            : base(RuntimeException.RetrieveMessage(innerException), innerException)
        {
            _message = base.Message;
            _providerInfo = provider;

            IContainsErrorRecord icer = innerException as IContainsErrorRecord;
            if (icer != null && icer.ErrorRecord != null)
            {
                _errorRecord = new ErrorRecord(icer.ErrorRecord, innerException);
            }
            else
            {
                _errorRecord = new ErrorRecord(
                    innerException,
                    "ErrorRecordNotSpecified",
                    ErrorCategory.InvalidOperation,
                    null);
            }
        }

        /// <summary>
        /// Constructs a ProviderInvocationException with provider information and an
        /// ErrorRecord.
        /// </summary>
        /// <param name="provider">
        /// Information about the provider to be used in formatting the message.
        /// </param>
        /// <param name="errorRecord">
        /// Detailed error information
        /// </param>
        internal ProviderInvocationException(ProviderInfo provider, ErrorRecord errorRecord)
            : base(RuntimeException.RetrieveMessage(errorRecord),
                    RuntimeException.RetrieveException(errorRecord))
        {
            if (errorRecord == null)
            {
                throw new ArgumentNullException("errorRecord");
            }

            _message = base.Message;
            _providerInfo = provider;
            _errorRecord = errorRecord;
        }

        /// <summary>
        /// Constructs a ProviderInvocationException with a message
        /// and inner exception.
        /// </summary>
        /// <param name="message">
        /// The message for the exception.
        /// </param>
        /// <param name="innerException">
        /// The inner exception for this exception.
        /// </param>
        public ProviderInvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
            _message = message;
        }

        /// <summary>
        /// Constructs a ProviderInvocationException.
        /// </summary>
        /// <param name="errorId">
        /// This string will be used to construct the FullyQualifiedErrorId,
        /// which is a global identifier of the error condition.  Pass a
        /// non-empty string which is specific to this error condition in
        /// this context.
        /// </param>
        /// <param name="resourceStr">
        /// This string is the message template string.
        /// </param>
        /// <param name="provider">
        /// The provider information used to format into the message.
        /// </param>
        /// <param name="path">
        /// The path that was being processed when the exception occurred.
        /// </param>
        /// <param name="innerException">
        /// The exception that was thrown by the provider.
        /// </param>
        internal ProviderInvocationException(
            string errorId,
            string resourceStr,
            ProviderInfo provider,
            string path,
            Exception innerException)
            : this(errorId, resourceStr, provider, path, innerException, true)
        {
        }

        /// <summary>
        /// Constructor to make it easy to wrap a provider exception.
        /// </summary>
        /// <param name="errorId">
        /// This string will be used to construct the FullyQualifiedErrorId,
        /// which is a global identifier of the error condition.  Pass a
        /// non-empty string which is specific to this error condition in
        /// this context.
        /// </param>
        /// <param name="resourceStr">
        /// This is the message template string
        /// </param>
        /// <param name="provider">
        /// The provider information used to format into the message.
        /// </param>
        /// <param name="path">
        /// The path that was being processed when the exception occurred.
        /// </param>
        /// <param name="innerException">
        /// The exception that was thrown by the provider.
        /// </param>
        /// <param name="useInnerExceptionMessage">
        /// If true, the message from the inner exception will be used if the exception contains
        /// an ErrorRecord. If false, the error message retrieved using the errorId will be used.
        /// </param>
        internal ProviderInvocationException(
            string errorId,
            string resourceStr,
            ProviderInfo provider,
            string path,
            Exception innerException,
            bool useInnerExceptionMessage)
            : base(
                RetrieveMessage(errorId, resourceStr, provider, path, innerException),
                innerException)
        {
            _providerInfo = provider;

            _message = base.Message;

            Exception errorRecordException = null;
            if (useInnerExceptionMessage)
            {
                errorRecordException = innerException;
            }
            else
            {
                errorRecordException = new ParentContainsErrorRecordException(this);
            }

            IContainsErrorRecord icer = innerException as IContainsErrorRecord;
            if (icer != null && icer.ErrorRecord != null)
            {
                _errorRecord = new ErrorRecord(icer.ErrorRecord, errorRecordException);
            }
            else
            {
                _errorRecord = new ErrorRecord(
                    errorRecordException,
                    errorId,
                    ErrorCategory.InvalidOperation,
                    null);
            }
        }
        #endregion Constructors

        #region Properties
        /// <summary>
        /// Gets the provider information of the provider that threw an exception.
        /// </summary>
        public ProviderInfo ProviderInfo { get { return _providerInfo; } }

        [NonSerialized]
        internal ProviderInfo _providerInfo;

        /// <summary>
        /// Gets the error record.
        /// </summary>
        public override ErrorRecord ErrorRecord
        {
            get
            {
                if (_errorRecord == null)
                {
                    _errorRecord = new ErrorRecord(
                        new ParentContainsErrorRecordException(this),
                        "ProviderInvocationException",
                        ErrorCategory.NotSpecified,
                        null);
                }

                return _errorRecord;
            }
        }

        [NonSerialized]
        private ErrorRecord _errorRecord;
        #endregion Properties

        #region Private/Internal
        private static string RetrieveMessage(
            string errorId,
            string resourceStr,
            ProviderInfo provider,
            string path,
            Exception innerException)
        {
            if (innerException == null)
            {
                Diagnostics.Assert(false,
                "ProviderInvocationException.RetrieveMessage needs innerException");
                return string.Empty;
            }

            if (string.IsNullOrEmpty(errorId))
            {
                Diagnostics.Assert(false,
                "ProviderInvocationException.RetrieveMessage needs errorId");
                return RuntimeException.RetrieveMessage(innerException);
            }

            if (provider == null)
            {
                Diagnostics.Assert(false,
                "ProviderInvocationException.RetrieveMessage needs provider");
                return RuntimeException.RetrieveMessage(innerException);
            }

            string format = resourceStr;
            if (string.IsNullOrEmpty(format))
            {
                Diagnostics.Assert(false,
                "ProviderInvocationException.RetrieveMessage bad errorId " + errorId);
                return RuntimeException.RetrieveMessage(innerException);
            }

            string result = null;

            if (path == null)
            {
                result =
                    string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        format,
                        provider.Name,
                        RuntimeException.RetrieveMessage(innerException));
            }
            else
            {
                result =
                    string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        format,
                        provider.Name,
                        path,
                        RuntimeException.RetrieveMessage(innerException));
            }

            return result;
        }

        /// <summary>
        /// Gets the exception message.
        /// </summary>
        public override string Message
        {
            get { return (string.IsNullOrEmpty(_message)) ? base.Message : _message; }
        }

        [NonSerialized]
        private string _message /* = null */;

        #endregion Private/Internal
    }

    /// <summary>
    /// Categories of session state objects, used by SessionStateException.
    /// </summary>
    public enum SessionStateCategory
    {
        /// <summary>
        /// Used when an exception is thrown accessing a variable.
        /// </summary>
        Variable = 0,

        /// <summary>
        /// Used when an exception is thrown accessing an alias.
        /// </summary>
        Alias = 1,

        /// <summary>
        /// Used when an exception is thrown accessing a function.
        /// </summary>
        Function = 2,

        /// <summary>
        /// Used when an exception is thrown accessing a filter.
        /// </summary>
        Filter = 3,

        /// <summary>
        /// Used when an exception is thrown accessing a drive.
        /// </summary>
        Drive = 4,

        /// <summary>
        /// Used when an exception is thrown accessing a Cmdlet Provider.
        /// </summary>
        CmdletProvider = 5,

        /// <summary>
        /// Used when an exception is thrown manipulating the PowerShell language scopes.
        /// </summary>
        Scope = 6,

        /// <summary>
        /// Used when generically accessing any type of command...
        /// </summary>
        Command = 7,

        /// <summary>
        /// Other resources not covered by the previous categories...
        /// </summary>
        Resource = 8,

        /// <summary>
        /// Used when an exception is thrown accessing a cmdlet.
        /// </summary>
        Cmdlet = 9,
    }

    /// <summary>
    /// SessionStateException represents an error working with
    /// session state objects: variables, aliases, functions, filters,
    /// drives, or providers.
    /// </summary>
    [Serializable]
    public class SessionStateException : RuntimeException
    {
        #region ctor
        /// <summary>
        /// Constructs a SessionStateException.
        /// </summary>
        /// <param name="itemName">Name of session state object.</param>
        /// <param name="sessionStateCategory">Category of session state object.</param>
        /// <param name="resourceStr">This string is the message template string.</param>
        /// <param name="errorIdAndResourceId">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        /// <param name="errorCategory">ErrorRecord.CategoryInfo.Category.</param>
        /// <param name="messageArgs">
        /// Additional insertion strings used to construct the message.
        /// Note that itemName is always the first insertion string.
        /// </param>
        internal SessionStateException(
            string itemName,
            SessionStateCategory sessionStateCategory,
            string errorIdAndResourceId,
            string resourceStr,
            ErrorCategory errorCategory,
            params object[] messageArgs)
            : base(BuildMessage(itemName, resourceStr, messageArgs))
        {
            _itemName = itemName;
            _sessionStateCategory = sessionStateCategory;
            _errorId = errorIdAndResourceId;
            _errorCategory = errorCategory;
        }

        /// <summary>
        /// Constructs a SessionStateException.
        /// </summary>
        public SessionStateException()
            : base()
        {
        }

        /// <summary>
        /// Constructs a SessionStateException.
        /// </summary>
        /// <param name="message">
        /// The message used in the exception.
        /// </param>
        public SessionStateException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs a SessionStateException.
        /// </summary>
        /// <param name="message">
        /// The message used in the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that caused the error.
        /// </param>
        public SessionStateException(string message,
                                     Exception innerException)
                : base(message, innerException)
        {
        }
        #endregion ctor

        #region Serialization
        /// <summary>
        /// Constructs a SessionStateException using serialized data.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected SessionStateException(SerializationInfo info,
                                        StreamingContext context)
            : base(info, context)
        {
            _sessionStateCategory = (SessionStateCategory)info.GetInt32("SessionStateCategory"); // CODEWORK test this
        }

        /// <summary>
        /// Serializes the exception data.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new PSArgumentNullException("info");
            }

            base.GetObjectData(info, context);
            // If there are simple fields, serialize them with info.AddValue
            info.AddValue("SessionStateCategory", (int)_sessionStateCategory);
        }
        #endregion Serialization

        #region Properties
        /// <summary>
        /// Gets the error record information for this exception.
        /// </summary>
        public override ErrorRecord ErrorRecord
        {
            get
            {
                if (_errorRecord == null)
                {
                    _errorRecord = new ErrorRecord(
                        new ParentContainsErrorRecordException(this),
                        _errorId,
                        _errorCategory,
                        _itemName);
                }

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord;

        /// <summary>
        /// Gets the name of session state object the error occurred on.
        /// </summary>
        public string ItemName
        {
            get { return _itemName; }
        }

        private string _itemName = string.Empty;

        /// <summary>
        /// Gets the category of session state object the error occurred on.
        /// </summary>
        public SessionStateCategory SessionStateCategory
        {
            get { return _sessionStateCategory; }
        }

        private SessionStateCategory _sessionStateCategory = SessionStateCategory.Variable;
        #endregion Properties

        #region Private
        private string _errorId = "SessionStateException";
        private ErrorCategory _errorCategory = ErrorCategory.InvalidArgument;

        private static string BuildMessage(
            string itemName,
            string resourceStr,
            params object[] messageArgs)
        {
            object[] a;
            if (messageArgs != null && 0 < messageArgs.Length)
            {
                a = new object[messageArgs.Length + 1];
                a[0] = itemName;
                messageArgs.CopyTo(a, 1);
            }
            else
            {
                a = new object[1];
                a[0] = itemName;
            }

            return StringUtil.Format(resourceStr, a);
        }
        #endregion Private
    }

    /// <summary>
    /// SessionStateUnauthorizedAccessException occurs when
    /// a change to a session state object cannot be completed
    /// because the object is read-only or constant, or because
    /// an object which is declared constant cannot be removed
    /// or made non-constant.
    /// </summary>
    [Serializable]
    public class SessionStateUnauthorizedAccessException : SessionStateException
    {
        #region ctor
        /// <summary>
        /// Constructs a SessionStateUnauthorizedAccessException.
        /// </summary>
        /// <param name="itemName">
        /// The name of the session state object the error occurred on.
        /// </param>
        /// <param name="sessionStateCategory">
        /// The category of session state object.
        /// </param>
        /// <param name="errorIdAndResourceId">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        /// <param name="resourceStr">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        internal SessionStateUnauthorizedAccessException(
            string itemName,
            SessionStateCategory sessionStateCategory,
            string errorIdAndResourceId,
            string resourceStr
            )
            : base(itemName, sessionStateCategory,
                    errorIdAndResourceId, resourceStr, ErrorCategory.WriteError)
        {
        }

        /// <summary>
        /// Constructs a SessionStateUnauthorizedAccessException.
        /// </summary>
        public SessionStateUnauthorizedAccessException()
            : base()
        {
        }

        /// <summary>
        /// Constructs a SessionStateUnauthorizedAccessException.
        /// </summary>
        /// <param name="message">
        /// The message used by the exception.
        /// </param>
        public SessionStateUnauthorizedAccessException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs a SessionStateUnauthorizedAccessException.
        /// </summary>
        /// <param name="message">
        /// The message used by the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that caused the error.
        /// </param>
        public SessionStateUnauthorizedAccessException(string message,
                                             Exception innerException)
                : base(message, innerException)
        {
        }
        #endregion ctor

        #region Serialization
        /// <summary>
        /// Constructs a SessionStateUnauthorizedAccessException using serialized data.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected SessionStateUnauthorizedAccessException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
        #endregion Serialization
    }

    /// <summary>
    /// ProviderNotFoundException occurs when no provider can be found
    /// with the specified name.
    /// </summary>
    [Serializable]
    public class ProviderNotFoundException : SessionStateException
    {
        #region ctor
        /// <summary>
        /// Constructs a ProviderNotFoundException.
        /// </summary>
        /// <param name="itemName">
        /// The name of provider that could not be found.
        /// </param>
        /// <param name="sessionStateCategory">
        /// The category of session state object
        /// </param>
        /// <param name="errorIdAndResourceId">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        /// <param name="resourceStr">
        /// This string is the message template string
        /// </param>
        /// <param name="messageArgs">
        /// Additional arguments to build the message from.
        /// </param>
        internal ProviderNotFoundException(
            string itemName,
            SessionStateCategory sessionStateCategory,
            string errorIdAndResourceId,
            string resourceStr,
            params object[] messageArgs)
            : base(
                itemName,
                sessionStateCategory,
                errorIdAndResourceId,
                resourceStr,
                ErrorCategory.ObjectNotFound,
                messageArgs)
        {
        }

        /// <summary>
        /// Constructs a ProviderNotFoundException.
        /// </summary>
        public ProviderNotFoundException()
            : base()
        {
        }

        /// <summary>
        /// Constructs a ProviderNotFoundException.
        /// </summary>
        /// <param name="message">
        /// The messaged used by the exception.
        /// </param>
        public ProviderNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs a ProviderNotFoundException.
        /// </summary>
        /// <param name="message">
        /// The message used by the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that caused the error.
        /// </param>
        public ProviderNotFoundException(string message,
                                         Exception innerException)
                : base(message, innerException)
        {
        }
        #endregion ctor

        #region Serialization
        /// <summary>
        /// Constructs a ProviderNotFoundException using serialized data.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected ProviderNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
        #endregion Serialization
    }

    /// <summary>
    /// ProviderNameAmbiguousException occurs when more than one provider exists
    /// for a given name and the request did not contain the PSSnapin name qualifier.
    /// </summary>
    [Serializable]
    public class ProviderNameAmbiguousException : ProviderNotFoundException
    {
        #region ctor
        /// <summary>
        /// Constructs a ProviderNameAmbiguousException.
        /// </summary>
        /// <param name="providerName">
        /// The name of provider that was ambiguous.
        /// </param>
        /// <param name="errorIdAndResourceId">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        /// <param name="resourceStr">
        /// This string is the message template string
        /// </param>
        /// <param name="possibleMatches">
        /// The provider information for the providers that match the specified
        /// name.
        /// </param>
        /// <param name="messageArgs">
        /// Additional arguments to build the message from.
        /// </param>
        internal ProviderNameAmbiguousException(
            string providerName,
            string errorIdAndResourceId,
            string resourceStr,
            Collection<ProviderInfo> possibleMatches,
            params object[] messageArgs)
            : base(
                providerName,
                SessionStateCategory.CmdletProvider,
                errorIdAndResourceId,
                resourceStr,
                messageArgs)
        {
            _possibleMatches = new ReadOnlyCollection<ProviderInfo>(possibleMatches);
        }

        /// <summary>
        /// Constructs a ProviderNameAmbiguousException.
        /// </summary>
        public ProviderNameAmbiguousException()
            : base()
        {
        }

        /// <summary>
        /// Constructs a ProviderNameAmbiguousException.
        /// </summary>
        /// <param name="message">
        /// The messaged used by the exception.
        /// </param>
        public ProviderNameAmbiguousException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs a ProviderNameAmbiguousException.
        /// </summary>
        /// <param name="message">
        /// The message used by the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that caused the error.
        /// </param>
        public ProviderNameAmbiguousException(string message,
                                         Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion ctor

        #region Serialization
        /// <summary>
        /// Constructs a ProviderNameAmbiguousException using serialized data.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected ProviderNameAmbiguousException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
        #endregion Serialization

        #region public properties

        /// <summary>
        /// Gets the information of the providers which might match the specified
        /// provider name.
        /// </summary>
        public ReadOnlyCollection<ProviderInfo> PossibleMatches
        {
            get
            {
                return _possibleMatches;
            }
        }

        private ReadOnlyCollection<ProviderInfo> _possibleMatches;

        #endregion public properties
    }

    /// <summary>
    /// DriveNotFoundException occurs when no drive can be found
    /// with the specified name.
    /// </summary>
    [Serializable]
    public class DriveNotFoundException : SessionStateException
    {
        #region ctor
        /// <summary>
        /// Constructs a DriveNotFoundException.
        /// </summary>
        /// <param name="itemName">
        /// The name of the drive that could not be found.
        /// </param>
        /// <param name="errorIdAndResourceId">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        /// <param name="resourceStr">
        /// This string is the message template string
        /// </param>
        internal DriveNotFoundException(
            string itemName,
            string errorIdAndResourceId,
            string resourceStr
            )
            : base(itemName, SessionStateCategory.Drive,
                    errorIdAndResourceId, resourceStr, ErrorCategory.ObjectNotFound)
        {
        }

        /// <summary>
        /// Constructs a DriveNotFoundException.
        /// </summary>
        public DriveNotFoundException()
            : base()
        {
        }

        /// <summary>
        /// Constructs a DriveNotFoundException.
        /// </summary>
        /// <param name="message">
        /// The message that will be used by the exception.
        /// </param>
        public DriveNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs a DriveNotFoundException.
        /// </summary>
        /// <param name="message">
        /// The message that will be used by the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that caused the error.
        /// </param>
        public DriveNotFoundException(string message,
                                      Exception innerException)
                : base(message, innerException)
        {
        }
        #endregion ctor

        #region Serialization
        /// <summary>
        /// Constructs a DriveNotFoundException using serialized data.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected DriveNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
        #endregion Serialization
    }

    /// <summary>
    /// ItemNotFoundException occurs when the path contained no wildcard characters
    /// and an item at that path could not be found.
    /// </summary>
    [Serializable]
    public class ItemNotFoundException : SessionStateException
    {
        #region ctor
        /// <summary>
        /// Constructs a ItemNotFoundException.
        /// </summary>
        /// <param name="path">
        /// The path that was not found.
        /// </param>
        /// <param name="errorIdAndResourceId">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        /// <param name="resourceStr">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        internal ItemNotFoundException(
            string path,
            string errorIdAndResourceId,
            string resourceStr
            )
            : base(path, SessionStateCategory.Drive,
                    errorIdAndResourceId, resourceStr, ErrorCategory.ObjectNotFound)
        {
        }

        /// <summary>
        /// Constructs a ItemNotFoundException.
        /// </summary>
        public ItemNotFoundException()
            : base()
        {
        }

        /// <summary>
        /// Constructs a ItemNotFoundException.
        /// </summary>
        /// <param name="message">
        /// The message used by the exception.
        /// </param>
        public ItemNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs a ItemNotFoundException.
        /// </summary>
        /// <param name="message">
        /// The message used by the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that caused the error.
        /// </param>
        public ItemNotFoundException(string message,
                                      Exception innerException)
                : base(message, innerException)
        {
        }
        #endregion ctor

        #region Serialization
        /// <summary>
        /// Constructs a ItemNotFoundException using serialized data.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected ItemNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
        #endregion Serialization
    }
}

