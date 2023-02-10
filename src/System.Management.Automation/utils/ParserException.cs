// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation.Language;
using System.Runtime.Serialization;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines the exception thrown when a syntax error occurs while parsing PowerShell script text.
    /// </summary>
    [Serializable]
    public class ParseException : RuntimeException
    {
        private const string errorIdString = "Parse";

        private readonly ParseError[] _errors;

        /// <summary>
        /// The list of parser errors.
        /// </summary>
        public ParseError[] Errors
        {
            get { return _errors; }
        }

        #region Serialization
        /// <summary>
        /// Initializes a new instance of the ParseException class and defines the serialization information,
        /// and streaming context.
        /// </summary>
        /// <param name="info">The serialization information to use when initializing this object.</param>
        /// <param name="context">The streaming context to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        protected ParseException(SerializationInfo info,
                           StreamingContext context)
                : base(info, context)
        {
            _errors = (ParseError[])info.GetValue("Errors", typeof(ParseError[]));
        }

        /// <summary>
        /// Add private data for serialization.
        /// </summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new PSArgumentNullException(nameof(info));
            }

            base.GetObjectData(info, context);
            info.AddValue("Errors", _errors);
        }

        #endregion Serialization

        #region ctor

        /// <summary>
        /// Initializes a new instance of the class ParseException.
        /// </summary>
        /// <returns>Constructed object.</returns>
        public ParseException() : base()
        {
            base.SetErrorId(errorIdString);
            base.SetErrorCategory(ErrorCategory.ParserError);
        }

        /// <summary>
        /// Initializes a new instance of the ParseException class and defines the error message.
        /// </summary>
        /// <param name="message">The error message to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        public ParseException(string message) : base(message)
        {
            base.SetErrorId(errorIdString);
            base.SetErrorCategory(ErrorCategory.ParserError);
        }

        /// <summary>
        /// Initializes a new instance of the ParseException class and defines the error message and
        /// errorID.
        /// </summary>
        /// <param name="message">The error message to use when initializing this object.</param>
        /// <param name="errorId">The errorId to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        internal ParseException(string message, string errorId) : base(message)
        {
            base.SetErrorId(errorId);
            base.SetErrorCategory(ErrorCategory.ParserError);
        }

        /// <summary>
        /// Initializes a new instance of the ParseException class and defines the error message,
        /// error ID and inner exception.
        /// </summary>
        /// <param name="message">The error message to use when initializing this object.</param>
        /// <param name="errorId">The errorId to use when initializing this object.</param>
        /// <param name="innerException">The inner exception to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        internal ParseException(string message, string errorId, Exception innerException)
            : base(message, innerException)
        {
            base.SetErrorId(errorId);
            base.SetErrorCategory(ErrorCategory.ParserError);
        }

        /// <summary>
        /// Initializes a new instance of the ParseException class and defines the error message and
        /// inner exception.
        /// </summary>
        /// <param name="message">The error message to use when initializing this object.</param>
        /// <param name="innerException">The inner exception to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        public ParseException(string message,
                        Exception innerException)
                : base(message, innerException)
        {
            base.SetErrorId(errorIdString);
            base.SetErrorCategory(ErrorCategory.ParserError);
        }

        /// <summary>
        /// Initializes a new instance of the ParseException class with a collection of error messages.
        /// </summary>
        /// <param name="errors">The collection of error messages.</param>
        public ParseException(ParseError[] errors)
        {
            ArgumentNullException.ThrowIfNull(errors);

            if (errors.Length == 0)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            _errors = errors;
            // Arbitrarily choose the first error message for the ErrorId.
            base.SetErrorId(_errors[0].ErrorId);
            base.SetErrorCategory(ErrorCategory.ParserError);

            if (errors[0].Extent != null)
                this.ErrorRecord.SetInvocationInfo(new InvocationInfo(null, errors[0].Extent));
        }

        #endregion ctor

        /// <summary>
        /// The error message to display.
        /// </summary>
        public override string Message
        {
            get
            {
                if (_errors == null)
                {
                    return base.Message;
                }

                // Report at most the first 10 errors
                var errorsToReport = (_errors.Length > 10)
                    ? _errors.Take(10).Select(static e => e.ToString()).Append(ParserStrings.TooManyErrors)
                    : _errors.Select(static e => e.ToString());

                return string.Join(Environment.NewLine + Environment.NewLine, errorsToReport);
            }
        }
    }

    /// <summary>
    /// Defines the exception thrown when a incomplete parse error occurs while parsing PowerShell script text.
    /// </summary>
    /// <remarks>
    /// This is a variation on a parsing error that indicates that the parse was incomplete
    /// rather than irrecoverably wrong. A host can catch this exception and then prompt for additional
    /// input to complete the parse.
    /// </remarks>
    [Serializable]
    public class IncompleteParseException
            : ParseException
    {
        #region private
        private const string errorIdString = "IncompleteParse";
        #endregion

        #region ctor

        #region Serialization
        /// <summary>
        /// Initializes a new instance of the IncompleteParseException class and defines the serialization information,
        /// and streaming context.
        /// </summary>
        /// <param name="info">The serialization information to use when initializing this object.</param>
        /// <param name="context">The streaming context to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        protected IncompleteParseException(SerializationInfo info,
                           StreamingContext context)
                : base(info, context)
        {
        }
        #endregion Serialization

        /// <summary>
        /// Initializes a new instance of the class IncompleteParseException.
        /// </summary>
        /// <returns>Constructed object.</returns>
        public IncompleteParseException() : base()
        {
            // Error category is set in base constructor
            base.SetErrorId(errorIdString);
        }

        /// <summary>
        /// Initializes a new instance of the IncompleteParseException class and defines the error message.
        /// </summary>
        /// <param name="message">The error message to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        public IncompleteParseException(string message) : base(message)
        {
            // Error category is set in base constructor
            base.SetErrorId(errorIdString);
        }

        /// <summary>
        /// Initializes a new instance of the IncompleteParseException class and defines the error message and
        /// errorID.
        /// </summary>
        /// <param name="message">The error message to use when initializing this object.</param>
        /// <param name="errorId">The errorId to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        internal IncompleteParseException(string message, string errorId) : base(message, errorId)
        {
            // Error category is set in base constructor
        }

        /// <summary>
        /// Initializes a new instance of the IncompleteParseException class and defines the error message,
        /// error ID and inner exception.
        /// </summary>
        /// <param name="message">The error message to use when initializing this object.</param>
        /// <param name="errorId">The errorId to use when initializing this object.</param>
        /// <param name="innerException">The inner exception to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        internal IncompleteParseException(string message, string errorId, Exception innerException)
            : base(message, errorId, innerException)
        {
            // Error category is set in base constructor
        }

        /// <summary>
        /// Initializes a new instance of the IncompleteParseException class and defines the error message and
        /// inner exception.
        /// </summary>
        /// <param name="message">The error message to use when initializing this object.</param>
        /// <param name="innerException">The inner exception to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        public IncompleteParseException(string message,
                        Exception innerException)
                : base(message, innerException)
        {
            // Error category is set in base constructor
            base.SetErrorId(errorIdString);
        }
        #endregion ctor
    }
}
