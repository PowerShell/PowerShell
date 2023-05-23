// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines the exception thrown for all Extended type system related errors.
    /// </summary>
    public class ExtendedTypeSystemException : RuntimeException
    {
        #region ctor
        /// <summary>
        /// Initializes a new instance of ExtendedTypeSystemException with the message set
        /// to typeof(ExtendedTypeSystemException).FullName.
        /// </summary>
        public ExtendedTypeSystemException()
            : base(typeof(ExtendedTypeSystemException).FullName)
        {
        }

        /// <summary>
        /// Initializes a new instance of ExtendedTypeSystemException setting the message.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public ExtendedTypeSystemException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of ExtendedTypeSystemException setting the message and innerException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public ExtendedTypeSystemException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Recommended constructor for the class.
        /// </summary>
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception, null for none.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal ExtendedTypeSystemException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(
                  StringUtil.Format(resourceString, arguments),
                  innerException)
        {
            SetErrorId(errorId);
        }

        #endregion ctor

    }

    /// <summary>
    /// Defines the exception thrown for Method related errors.
    /// </summary>
    public class MethodException : ExtendedTypeSystemException
    {
        internal const string MethodArgumentCountExceptionMsg = "MethodArgumentCountException";
        internal const string MethodAmbiguousExceptionMsg = "MethodAmbiguousException";
        internal const string MethodArgumentConversionExceptionMsg = "MethodArgumentConversionException";
        internal const string NonRefArgumentToRefParameterMsg = "NonRefArgumentToRefParameter";
        internal const string RefArgumentToNonRefParameterMsg = "RefArgumentToNonRefParameter";

        #region ctor
        /// <summary>
        /// Initializes a new instance of MethodException with the message set
        /// to typeof(MethodException).FullName.
        /// </summary>
        public MethodException()
            : base(typeof(MethodException).FullName)
        {
        }

        /// <summary>
        /// Initializes a new instance of MethodException setting the message.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public MethodException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of MethodException setting the message and innerException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public MethodException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Recommended constructor for the class.
        /// </summary>
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal MethodException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #endregion ctor

    }

    /// <summary>
    /// Defines the exception thrown for Method invocation exceptions.
    /// </summary>
    public class MethodInvocationException : MethodException
    {
        internal const string MethodInvocationExceptionMsg = "MethodInvocationException";
        internal const string CopyToInvocationExceptionMsg = "CopyToInvocationException";
        internal const string WMIMethodInvocationException = "WMIMethodInvocationException";

        #region ctor
        /// <summary>
        /// Initializes a new instance of MethodInvocationException with the message set
        /// to typeof(MethodInvocationException).FullName.
        /// </summary>
        public MethodInvocationException()
            : base(typeof(MethodInvocationException).FullName)
        {
        }

        /// <summary>
        /// Initializes a new instance of MethodInvocationException setting the message.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public MethodInvocationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of MethodInvocationException setting the message and innerException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public MethodInvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Recommended constructor for the class.
        /// </summary>
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal MethodInvocationException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #endregion ctor

    }

    /// <summary>
    /// Defines the exception thrown for errors getting the value of properties.
    /// </summary>
    public class GetValueException : ExtendedTypeSystemException
    {
        internal const string GetWithoutGetterExceptionMsg = "GetWithoutGetterException";
        internal const string WriteOnlyProperty = "WriteOnlyProperty";
        #region ctor
        /// <summary>
        /// Initializes a new instance of GetValueException with the message set
        /// to typeof(GetValueException).FullName.
        /// </summary>
        public GetValueException()
            : base(typeof(GetValueException).FullName)
        {
        }

        /// <summary>
        /// Initializes a new instance of GetValueException setting the message.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public GetValueException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of GetValueException setting the message and innerException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public GetValueException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Recommended constructor for the class.
        /// </summary>
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal GetValueException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #endregion ctor

    }

    /// <summary>
    /// Defines the exception thrown for errors getting the value of properties.
    /// </summary>
    public class PropertyNotFoundException : ExtendedTypeSystemException
    {
        #region ctor
        /// <summary>
        /// Initializes a new instance of GetValueException with the message set
        /// to typeof(GetValueException).FullName.
        /// </summary>
        public PropertyNotFoundException()
            : base(typeof(PropertyNotFoundException).FullName)
        {
        }

        /// <summary>
        /// Initializes a new instance of GetValueException setting the message.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public PropertyNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of GetValueException setting the message and innerException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public PropertyNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Recommended constructor for the class.
        /// </summary>
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal PropertyNotFoundException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }
        #endregion ctor

    }

    /// <summary>
    /// Defines the exception thrown for exceptions thrown by property getters.
    /// </summary>
    public class GetValueInvocationException : GetValueException
    {
        internal const string ExceptionWhenGettingMsg = "ExceptionWhenGetting";

        #region ctor
        /// <summary>
        /// Initializes a new instance of GetValueInvocationException with the message set
        /// to typeof(GetValueInvocationException).FullName.
        /// </summary>
        public GetValueInvocationException()
            : base(typeof(GetValueInvocationException).FullName)
        {
        }

        /// <summary>
        /// Initializes a new instance of GetValueInvocationException setting the message.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public GetValueInvocationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of GetValueInvocationException setting the message and innerException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public GetValueInvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Recommended constructor for the class.
        /// </summary>
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal GetValueInvocationException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }
        #endregion ctor

    }

    /// <summary>
    /// Defines the exception thrown for errors setting the value of properties.
    /// </summary>
    public class SetValueException : ExtendedTypeSystemException
    {
        #region ctor
        /// <summary>
        /// Initializes a new instance of SetValueException with the message set
        /// to typeof(SetValueException).FullName.
        /// </summary>
        public SetValueException()
            : base(typeof(SetValueException).FullName)
        {
        }

        /// <summary>
        /// Initializes a new instance of SetValueException setting the message.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public SetValueException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of SetValueException setting the message and innerException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public SetValueException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Recommended constructor for the class.
        /// </summary>
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal SetValueException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #endregion ctor

    }

    /// <summary>
    /// Defines the exception thrown for exceptions thrown by property setters.
    /// </summary>
    public class SetValueInvocationException : SetValueException
    {
        #region ctor
        /// <summary>
        /// Initializes a new instance of SetValueInvocationException with the message set
        /// to typeof(SetValueInvocationException).FullName.
        /// </summary>
        public SetValueInvocationException()
            : base(typeof(SetValueInvocationException).FullName)
        {
        }

        /// <summary>
        /// Initializes a new instance of SetValueInvocationException setting the message.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public SetValueInvocationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of SetValueInvocationException setting the message and innerException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public SetValueInvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Recommended constructor for the class.
        /// </summary>
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal SetValueInvocationException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #endregion ctor

    }

    /// <summary>
    /// Defines the exception thrown for type conversion errors.
    /// </summary>
    public class PSInvalidCastException : InvalidCastException, IContainsErrorRecord
    {
        /// <summary>
        /// Initializes a new instance of PSInvalidCastException with the message set
        /// to typeof(PSInvalidCastException).FullName.
        /// </summary>
        public PSInvalidCastException()
            : base(typeof(PSInvalidCastException).FullName)
        {
        }
        /// <summary>
        /// Initializes a new instance of PSInvalidCastException setting the message.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public PSInvalidCastException(string message)
            : base(message)
        {
        }
        /// <summary>
        /// Initializes a new instance of PSInvalidCastException setting the message and innerException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public PSInvalidCastException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal PSInvalidCastException(string errorId, string message, Exception innerException)
            : base(message, innerException)
        {
            _errorId = errorId;
        }

        internal PSInvalidCastException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : this(
                errorId, StringUtil.Format(resourceString, arguments),
                innerException)
        {
        }

        /// <summary>
        /// Gets the ErrorRecord associated with this exception.
        /// </summary>
        public ErrorRecord ErrorRecord
        {
            get
            {
                _errorRecord ??= new ErrorRecord(
                    new ParentContainsErrorRecordException(this),
                    _errorId,
                    ErrorCategory.InvalidArgument,
                    null);

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord;
        private readonly string _errorId = "PSInvalidCastException";
    }
}
