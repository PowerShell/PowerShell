// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;
using System.Runtime.Serialization;

namespace System.Management.Automation.Host
{
    /// <summary>
    /// Defines the exception thrown when the Host cannot complete an operation
    /// such as checking whether there is any input available.
    /// </summary>
    [Serializable]
    public
    class HostException : RuntimeException
    {
        #region ctors
        /// <summary>
        /// Initializes a new instance of the HostException class.
        /// </summary>
        public
        HostException()
            : base(StringUtil.Format(HostInterfaceExceptionsStrings.DefaultCtorMessageTemplate, typeof(HostException).FullName))
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// Initializes a new instance of the HostException class and defines the error message.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        public
        HostException(string message)
            : base(message)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// Initializes a new instance of the HostException class and defines the error message and
        /// inner exception.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception. If the <paramref name="innerException"/>
        /// parameter is not a null reference, the current exception is raised in a catch
        /// block that handles the inner exception.
        /// </param>
        public
        HostException(string message, Exception innerException)
            : base(message, innerException)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// Initializes a new instance of the HostException class and defines the error message,
        /// inner exception, the error ID, and the error category.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception. If the <paramref name="innerException"/>
        /// parameter is not a null reference, the current exception is raised in a catch
        /// block that handles the inner exception.
        /// </param>
        /// <param name="errorId">
        /// The string that should uniquely identifies the situation where the exception is thrown.
        /// The string should not contain white space.
        /// </param>
        /// <param name="errorCategory">
        /// The ErrorCategory into which this exception situation falls
        /// </param>
        /// <remarks>
        /// Intentionally public, third-party hosts can call this
        /// </remarks>
        public
        HostException(
            string message,
            Exception innerException,
            string errorId,
            ErrorCategory errorCategory)
            : base(message, innerException)
        {
            SetErrorId(errorId);
            SetErrorCategory(errorCategory);
        }

        /// <summary>
        /// Initializes a new instance of the HostException class and defines the SerializationInfo
        /// and the StreamingContext.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized object data.
        /// </param>
        /// <param name="context">
        /// The contextual information about the source or destination.
        /// </param>
        protected
        HostException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
        #region private
        private void SetDefaultErrorRecord()
        {
            SetErrorCategory(ErrorCategory.ResourceUnavailable);
            SetErrorId(typeof(HostException).FullName);
        }
        #endregion

    }

    /// <summary>
    /// Defines the exception thrown when an error occurs from prompting for a command parameter.
    /// </summary>
    [Serializable]
    public
    class PromptingException : HostException
    {
        #region ctors
        /// <summary>
        /// Initializes a new instance of the PromptingException class.
        /// </summary>
        public
        PromptingException()
            : base(StringUtil.Format(HostInterfaceExceptionsStrings.DefaultCtorMessageTemplate, typeof(PromptingException).FullName))
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// Initializes a new instance of the PromptingException class and defines the error message.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        public
        PromptingException(string message)
            : base(message)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// Initializes a new instance of the PromptingException class and defines the error message and
        /// inner exception.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception. If the <paramref name="innerException"/>
        /// parameter is not a null reference, the current exception is raised in a catch
        /// block that handles the inner exception.
        /// </param>
        public
        PromptingException(string message, Exception innerException)
            : base(message, innerException)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// Initializes a new instance of the PromptingException class and defines the error message,
        /// inner exception, the error ID, and the error category.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception. If the <paramref name="innerException"/>
        /// parameter is not a null reference, the current exception is raised in a catch
        /// block that handles the inner exception.
        /// </param>
        /// <param name="errorId">
        /// The string that should uniquely identifies the situation where the exception is thrown.
        /// The string should not contain white space.
        /// </param>
        /// <param name="errorCategory">
        /// The ErrorCategory into which this exception situation falls
        /// </param>
        /// <remarks>
        /// Intentionally public, third-party hosts can call this
        /// </remarks>
        public
        PromptingException(
            string message,
            Exception innerException,
            string errorId,
            ErrorCategory errorCategory)
            : base(message, innerException, errorId, errorCategory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the HostException class and defines the SerializationInfo
        /// and the StreamingContext.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized object data.
        /// </param>
        /// <param name="context">
        /// The contextual information about the source or destination.
        /// </param>
        protected
        PromptingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
        #endregion

        #region private
        private void SetDefaultErrorRecord()
        {
            SetErrorCategory(ErrorCategory.ResourceUnavailable);
            SetErrorId(typeof(PromptingException).FullName);
        }
        #endregion
    }
}
