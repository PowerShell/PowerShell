// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Runtime.Serialization;

namespace System.Management.Automation
{
    /// <summary>
    /// The exception thrown if the specified value can not be bound parameter of a command.
    /// </summary>
    [Serializable]
    public class ParameterBindingException : RuntimeException
    {
        #region Constructors

        #region Preferred constructors

        /// <summary>
        /// Constructs a ParameterBindingException.
        /// </summary>
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        /// <!--
        /// InvocationInfo.MyCommand.Name == {0}
        /// -->
        /// </param>
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        /// If position is null, the one from the InvocationInfo is used.
        /// <!--
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// -->
        /// </param>
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        /// <!--
        /// parameterName == {1}
        /// -->
        /// </param>
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        /// <!--
        /// parameterType == {2}
        /// -->
        /// </param>
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        /// <!--
        /// typeSpecified == {3}
        /// -->
        /// </param>
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        /// <!--
        /// starts at {6}
        /// -->
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceString"/> or <paramref name="errorId"/>
        /// is null or empty.
        /// </exception>
        internal ParameterBindingException(
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(errorCategory, invocationInfo, errorPosition, errorId, null, null)
        {
            if (string.IsNullOrEmpty(resourceString))
            {
                throw PSTraceSource.NewArgumentException(nameof(resourceString));
            }

            if (string.IsNullOrEmpty(errorId))
            {
                throw PSTraceSource.NewArgumentException(nameof(errorId));
            }

            _invocationInfo = invocationInfo;

            if (_invocationInfo != null)
            {
                _commandName = invocationInfo.MyCommand.Name;
            }

            _parameterName = parameterName;
            _parameterType = parameterType;
            _typeSpecified = typeSpecified;

            if ((errorPosition == null) && (_invocationInfo != null))
            {
                errorPosition = invocationInfo.ScriptPosition;
            }

            if (errorPosition != null)
            {
                _line = errorPosition.StartLineNumber;
                _offset = errorPosition.StartColumnNumber;
            }

            _resourceString = resourceString;
            _errorId = errorId;

            if (args != null)
            {
                _args = args;
            }
        }

        /// <summary>
        /// Constructs a ParameterBindingException.
        /// </summary>
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        ///
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        /// If position is null, the one from the InvocationInfo is used.
        ///
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        ///
        /// parameterName == {1}
        /// </param>
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        ///
        /// parameterType == {2}
        /// </param>
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        ///
        /// typeSpecified == {3}
        /// </param>
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        ///
        /// starts at {6}
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="invocationInfo"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceString"/> or <paramref name="errorId"/>
        /// is null or empty.
        /// </exception>
        internal ParameterBindingException(
            Exception innerException,
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(errorCategory, invocationInfo, errorPosition, errorId, null, innerException)
        {
            if (invocationInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(invocationInfo));
            }

            if (string.IsNullOrEmpty(resourceString))
            {
                throw PSTraceSource.NewArgumentException(nameof(resourceString));
            }

            if (string.IsNullOrEmpty(errorId))
            {
                throw PSTraceSource.NewArgumentException(nameof(errorId));
            }

            _invocationInfo = invocationInfo;
            _commandName = invocationInfo.MyCommand.Name;
            _parameterName = parameterName;
            _parameterType = parameterType;
            _typeSpecified = typeSpecified;

            errorPosition ??= invocationInfo.ScriptPosition;

            if (errorPosition != null)
            {
                _line = errorPosition.StartLineNumber;
                _offset = errorPosition.StartColumnNumber;
            }

            _resourceString = resourceString;
            _errorId = errorId;

            if (args != null)
            {
                _args = args;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="innerException"></param>
        /// <param name="pbex"></param>
        /// <param name="resourceString"></param>
        /// <param name="args"></param>
        internal ParameterBindingException(
            Exception innerException,
            ParameterBindingException pbex,
            string resourceString,
            params object[] args)
            : base(string.Empty, innerException)
        {
            if (pbex == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(pbex));
            }

            if (string.IsNullOrEmpty(resourceString))
            {
                throw PSTraceSource.NewArgumentException(nameof(resourceString));
            }

            _invocationInfo = pbex.CommandInvocation;
            if (_invocationInfo != null)
            {
                _commandName = _invocationInfo.MyCommand.Name;
            }

            IScriptExtent errorPosition = null;
            if (_invocationInfo != null)
            {
                errorPosition = _invocationInfo.ScriptPosition;
            }

            _line = pbex.Line;
            _offset = pbex.Offset;

            _parameterName = pbex.ParameterName;
            _parameterType = pbex.ParameterType;
            _typeSpecified = pbex.TypeSpecified;
            _errorId = pbex.ErrorId;

            _resourceString = resourceString;

            if (args != null)
            {
                _args = args;
            }

            base.SetErrorCategory(pbex.ErrorRecord._category);
            base.SetErrorId(_errorId);
            if (_invocationInfo != null)
            {
                base.ErrorRecord.SetInvocationInfo(new InvocationInfo(_invocationInfo.MyCommand, errorPosition));
            }
        }
        #endregion Preferred constructors

        #region serialization
        /// <summary>
        /// Constructors a ParameterBindingException using serialized data.
        /// </summary>
        /// <param name="info">
        /// serialization information
        /// </param>
        /// <param name="context">
        /// streaming context
        /// </param>
        protected ParameterBindingException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
            _message = info.GetString("ParameterBindingException_Message");
            _parameterName = info.GetString("ParameterName");
            _line = info.GetInt64("Line");
            _offset = info.GetInt64("Offset");
        }

        /// <summary>
        /// Serializes the exception.
        /// </summary>
        /// <param name="info">
        /// serialization information
        /// </param>
        /// <param name="context">
        /// streaming context
        /// </param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new PSArgumentNullException(nameof(info));
            }

            base.GetObjectData(info, context);
            info.AddValue("ParameterBindingException_Message", this.Message);
            info.AddValue("ParameterName", _parameterName);
            info.AddValue("Line", _line);
            info.AddValue("Offset", _offset);
        }
        #endregion serialization

        #region Do Not Use

        /// <summary>
        /// Constructs a ParameterBindingException.
        /// </summary>
        /// <remarks>
        /// DO NOT USE!!!
        /// </remarks>
        public ParameterBindingException() : base() { }

        /// <summary>
        /// Constructors a ParameterBindingException.
        /// </summary>
        /// <param name="message">
        /// Message to be included in exception.
        /// </param>
        /// <remarks>
        /// DO NOT USE!!!
        /// </remarks>
        public ParameterBindingException(string message) : base(message) { _message = message; }

        /// <summary>
        /// Constructs a ParameterBindingException.
        /// </summary>
        /// <param name="message">
        /// Message to be included in the exception.
        /// </param>
        /// <param name="innerException">
        /// exception that led to this exception
        /// </param>
        /// <remarks>
        /// DO NOT USE!!!
        /// </remarks>
        public ParameterBindingException(
            string message,
            Exception innerException)
            : base(message, innerException)
        { _message = message; }

        #endregion Do Not Use
        #endregion Constructors

        #region Properties
        /// <summary>
        /// Gets the message for the exception.
        /// </summary>
        public override string Message
        {
            get { return _message ??= BuildMessage(); }
        }

        private string _message;

        /// <summary>
        /// Gets the name of the parameter that the parameter binding
        /// error was encountered on.
        /// </summary>
        public string ParameterName
        {
            get
            {
                return _parameterName;
            }
        }

        private readonly string _parameterName = string.Empty;

        /// <summary>
        /// Gets the type the parameter is expecting.
        /// </summary>
        public Type ParameterType
        {
            get
            {
                return _parameterType;
            }
        }

        private readonly Type _parameterType;

        /// <summary>
        /// Gets the Type that was specified as the parameter value.
        /// </summary>
        public Type TypeSpecified
        {
            get
            {
                return _typeSpecified;
            }
        }

        private readonly Type _typeSpecified;

        /// <summary>
        /// Gets the errorId of this ParameterBindingException.
        /// </summary>
        public string ErrorId
        {
            get
            {
                return _errorId;
            }
        }

        private readonly string _errorId;

        /// <summary>
        /// Gets the line in the script at which the error occurred.
        /// </summary>
        public Int64 Line
        {
            get
            {
                return _line;
            }
        }

        private readonly Int64 _line = Int64.MinValue;

        /// <summary>
        /// Gets the offset on the line in the script at which the error occurred.
        /// </summary>
        public Int64 Offset
        {
            get
            {
                return _offset;
            }
        }

        private readonly Int64 _offset = Int64.MinValue;

        /// <summary>
        /// Gets the invocation information about the command.
        /// </summary>
        public InvocationInfo CommandInvocation
        {
            get
            {
                return _invocationInfo;
            }
        }

        private readonly InvocationInfo _invocationInfo;
        #endregion Properties

        #region private

        private readonly string _resourceString;
        private readonly object[] _args = Array.Empty<object>();
        private readonly string _commandName;

        private string BuildMessage()
        {
            object[] messageArgs = Array.Empty<object>();

            if (_args != null)
            {
                messageArgs = new object[_args.Length + 6];
                messageArgs[0] = _commandName;
                messageArgs[1] = _parameterName;
                messageArgs[2] = _parameterType;
                messageArgs[3] = _typeSpecified;
                messageArgs[4] = _line;
                messageArgs[5] = _offset;
                _args.CopyTo(messageArgs, 6);
            }

            string result = string.Empty;

            if (!string.IsNullOrEmpty(_resourceString))
            {
                result = StringUtil.Format(_resourceString, messageArgs);
            }

            return result;
        }

        #endregion Private
    }

    [Serializable]
    internal class ParameterBindingValidationException : ParameterBindingException
    {
        #region Preferred constructors

        /// <summary>
        /// Constructs a ParameterBindingValidationException.
        /// </summary>
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        ///
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        ///
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        ///
        /// parameterName == {1}
        /// </param>
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        ///
        /// parameterType == {2}
        /// </param>
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        ///
        /// typeSpecified == {3}
        /// </param>
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        ///
        /// starts at {6}
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceString"/> or <paramref name="errorId"/>
        /// is null or empty.
        /// </exception>
        internal ParameterBindingValidationException(
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(
                errorCategory,
                invocationInfo,
                errorPosition,
                parameterName,
                parameterType,
                typeSpecified,
                resourceString,
                errorId,
                args)
        {
        }

        /// <summary>
        /// Constructs a ParameterBindingValidationException.
        /// </summary>
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        ///
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        ///
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        ///
        /// parameterName == {1}
        /// </param>
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        ///
        /// parameterType == {2}
        /// </param>
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        ///
        /// typeSpecified == {3}
        /// </param>
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        ///
        /// starts at {6}
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="invocationInfo"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceBaseName"/> or <paramref name="errorIdAndResourceId"/>
        /// is null or empty.
        /// </exception>
        internal ParameterBindingValidationException(
            Exception innerException,
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(
                innerException,
                errorCategory,
                invocationInfo,
                errorPosition,
                parameterName,
                parameterType,
                typeSpecified,
                resourceString,
                errorId,
                args)
        {
            ValidationMetadataException validationException = innerException as ValidationMetadataException;
            if (validationException != null && validationException.SwallowException)
            {
                _swallowException = true;
            }
        }
        #endregion Preferred constructors

        #region serialization
        /// <summary>
        /// Constructs a ParameterBindingValidationException from serialized data.
        /// </summary>
        /// <param name="info">
        /// serialization information
        /// </param>
        /// <param name="context">
        /// streaming context
        /// </param>
        protected ParameterBindingValidationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }

        #endregion serialization

        #region Property

        /// <summary>
        /// Make the positional binding ignore this validation exception when it's set to true.
        /// </summary>
        /// <remarks>
        /// This property is only used internally in the positional binding phase
        /// </remarks>
        internal bool SwallowException
        {
            get { return _swallowException; }
        }

        private readonly bool _swallowException = false;

        #endregion Property
    }

    [Serializable]
    internal class ParameterBindingArgumentTransformationException : ParameterBindingException
    {
        #region Preferred constructors

        /// <summary>
        /// Constructs a ParameterBindingArgumentTransformationException.
        /// </summary>
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        ///
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        ///
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        ///
        /// parameterName == {1}
        /// </param>
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        ///
        /// parameterType == {2}
        /// </param>
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        ///
        /// typeSpecified == {3}
        /// </param>
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        ///
        /// starts at {6}
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceString"/> or <paramref name="errorId"/>
        /// is null or empty.
        /// </exception>
        internal ParameterBindingArgumentTransformationException(
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(
                errorCategory,
                invocationInfo,
                errorPosition,
                parameterName,
                parameterType,
                typeSpecified,
                resourceString,
                errorId,
                args)
        {
        }

        /// <summary>
        /// Constructs a ParameterBindingArgumentTransformationException.
        /// </summary>
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        ///
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        ///
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        ///
        /// parameterName == {1}
        /// </param>
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        ///
        /// parameterType == {2}
        /// </param>
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        ///
        /// typeSpecified == {3}
        /// </param>
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        ///
        /// starts at {6}
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="invocationInfo"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceString"/> or <paramref name="errorId"/>
        /// is null or empty.
        /// </exception>
        internal ParameterBindingArgumentTransformationException(
            Exception innerException,
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(
                innerException,
                errorCategory,
                invocationInfo,
                errorPosition,
                parameterName,
                parameterType,
                typeSpecified,
                resourceString,
                errorId,
                args)
        {
        }
        #endregion Preferred constructors
        #region serialization
        /// <summary>
        /// Constructs a ParameterBindingArgumentTransformationException using serialized data.
        /// </summary>
        /// <param name="info">
        /// serialization information
        /// </param>
        /// <param name="context">
        /// streaming context
        /// </param>
        protected ParameterBindingArgumentTransformationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }

        #endregion serialization
    }

    [Serializable]
    internal class ParameterBindingParameterDefaultValueException : ParameterBindingException
    {
        #region Preferred constructors

        /// <summary>
        /// Constructs a ParameterBindingParameterDefaultValueException.
        /// </summary>
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        ///
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        ///
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        ///
        /// parameterName == {1}
        /// </param>
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        ///
        /// parameterType == {2}
        /// </param>
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        ///
        /// typeSpecified == {3}
        /// </param>
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        ///
        /// starts at {6}
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceString"/> or <paramref name="errorId"/>
        /// is null or empty.
        /// </exception>
        internal ParameterBindingParameterDefaultValueException(
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(
                errorCategory,
                invocationInfo,
                errorPosition,
                parameterName,
                parameterType,
                typeSpecified,
                resourceString,
                errorId,
                args)
        {
        }

        /// <summary>
        /// Constructs a ParameterBindingParameterDefaultValueException.
        /// </summary>
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        ///
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        ///
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        ///
        /// parameterName == {1}
        /// </param>
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        ///
        /// parameterType == {2}
        /// </param>
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        ///
        /// typeSpecified == {3}
        /// </param>
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        ///
        /// starts at {6}
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="invocationInfo"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceString"/> or <paramref name="errorId"/>
        /// is null or empty.
        /// </exception>
        internal ParameterBindingParameterDefaultValueException(
            Exception innerException,
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(
                innerException,
                errorCategory,
                invocationInfo,
                errorPosition,
                parameterName,
                parameterType,
                typeSpecified,
                resourceString,
                errorId,
                args)
        {
        }
        #endregion Preferred constructors

        #region serialization
        /// <summary>
        /// Constructs a ParameterBindingParameterDefaultValueException using serialized data.
        /// </summary>
        /// <param name="info">
        /// serialization information
        /// </param>
        /// <param name="context">
        /// streaming context
        /// </param>
        protected ParameterBindingParameterDefaultValueException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }

        #endregion serialization
    }
}
