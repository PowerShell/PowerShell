/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Runtime.Serialization;

#if !CORECLR
using System.Security.Permissions;
#else
// Use stub for SerializableAttribute, SecurityPermissionAttribute and ISerializable related types.
using Microsoft.PowerShell.CoreClr.Stubs;
#endif

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
        /// 
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// 
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        /// 
        /// <!--
        /// InvocationInfo.MyCommand.Name == {0}
        /// -->
        /// </param>
        /// 
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        /// If position is null, the one from the InvocationInfo is used.
        /// <!--
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// -->
        /// </param>
        /// 
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        /// <!--
        /// parameterName == {1}
        /// -->
        /// </param>
        /// 
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        /// <!--
        /// parameterType == {2}
        /// -->
        /// </param>
        /// 
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        /// <!--
        /// typeSpecified == {3}
        /// -->
        /// </param>
        /// 
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// 
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// 
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        /// <!--
        /// starts at {6}
        /// -->
        /// </param>
        /// 
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
            if (String.IsNullOrEmpty(resourceString))
            {
                throw PSTraceSource.NewArgumentException("resourceString");
            }

            if (String.IsNullOrEmpty(errorId))
            {
                throw PSTraceSource.NewArgumentException("errorId");
            }

            this.invocationInfo = invocationInfo;

            if (this.invocationInfo != null)
            {
                this.commandName = invocationInfo.MyCommand.Name;
            }

            this.parameterName = parameterName;
            this.parameterType = parameterType;
            this.typeSpecified = typeSpecified;

            if ((errorPosition == null) && (this.invocationInfo != null))
            {
                errorPosition = invocationInfo.ScriptPosition;
            }
            if (errorPosition != null)
            {
                this.line = errorPosition.StartLineNumber;
                this.offset = errorPosition.StartColumnNumber;
            }
            this.resourceString = resourceString;
            this.errorId = errorId;

            if (args != null)
            {
                this.args = args;
            }
        }

        /// <summary>
        /// Constructs a ParameterBindingException
        /// </summary>
        /// 
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        /// 
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// 
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        /// 
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// 
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        /// If position is null, the one from the InvocationInfo is used.
        /// 
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// 
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        /// 
        /// parameterName == {1}
        /// </param>
        /// 
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        /// 
        /// parameterType == {2}
        /// </param>
        /// 
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        /// 
        /// typeSpecified == {3}
        /// </param>
        /// 
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// 
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// 
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        /// 
        /// starts at {6}
        /// </param>
        /// 
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="invocationInfo"/> is null.
        /// </exception>
        /// 
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceString"/> or <paramref name="errorId"/>
        /// is null or empty.
        /// </exception>
        /// 
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
                throw PSTraceSource.NewArgumentNullException("invocationInfo");
            }

            if (String.IsNullOrEmpty(resourceString))
            {
                throw PSTraceSource.NewArgumentException("resourceString");
            }

            if (String.IsNullOrEmpty(errorId))
            {
                throw PSTraceSource.NewArgumentException("errorId");
            }

            this.invocationInfo = invocationInfo;
            this.commandName = invocationInfo.MyCommand.Name;
            this.parameterName = parameterName;
            this.parameterType = parameterType;
            this.typeSpecified = typeSpecified;

            if (errorPosition == null)
            {
                errorPosition = invocationInfo.ScriptPosition;
            }
            if (errorPosition != null)
            {
                this.line = errorPosition.StartLineNumber;
                this.offset = errorPosition.StartColumnNumber;
            }

            this.resourceString = resourceString;
            this.errorId = errorId;

            if (args != null)
            {
                this.args = args;
            }
        }

        /// <summary>
        /// 
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
            : base(String.Empty, innerException)
        {
            if (pbex == null)
            {
                throw PSTraceSource.NewArgumentNullException("pbex");
            }

            if (String.IsNullOrEmpty(resourceString))
            {
                throw PSTraceSource.NewArgumentException("resourceString");
            }

            this.invocationInfo = pbex.CommandInvocation;
            if(this.invocationInfo != null)
            {
                this.commandName = this.invocationInfo.MyCommand.Name;
            }
            IScriptExtent errorPosition = null;
            if (this.invocationInfo != null)
            {
                errorPosition = invocationInfo.ScriptPosition;
            }

            this.line = pbex.Line;
            this.offset = pbex.Offset;

            this.parameterName = pbex.ParameterName;
            this.parameterType = pbex.ParameterType;
            this.typeSpecified = pbex.TypeSpecified;
            this.errorId = pbex.ErrorId;

            this.resourceString = resourceString;

            if (args != null)
            {
                this.args = args;
            }

            base.SetErrorCategory(pbex.ErrorRecord._category);
            base.SetErrorId(errorId);
            if (this.invocationInfo != null)
            {
                base.ErrorRecord.SetInvocationInfo(new InvocationInfo(invocationInfo.MyCommand, errorPosition));
            }
        }
        #endregion Preferred constructors

        #region serialization
        /// <summary>
        /// Constructors a ParameterBindingException using serialized data.
        /// </summary>
        /// 
        /// <param name="info"> 
        /// serialization information 
        /// </param>
        /// 
        /// <param name="context"> 
        /// streaming context 
        /// </param>
        protected ParameterBindingException(
            SerializationInfo info, 
            StreamingContext context) 
            : base(info, context)
        {
            message = info.GetString("ParameterBindingException_Message");
            parameterName = info.GetString("ParameterName");
            line = info.GetInt64("Line");
            offset = info.GetInt64("Offset");
        }

        /// <summary>
        /// Serializes the exception
        /// </summary>
        /// 
        /// <param name="info"> 
        /// serialization information 
        /// </param>
        /// 
        /// <param name="context"> 
        /// streaming context 
        /// </param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new PSArgumentNullException("info");
            }

            base.GetObjectData(info, context);
            info.AddValue("ParameterBindingException_Message", this.Message);
            info.AddValue("ParameterName", parameterName);
            info.AddValue("Line", line);
            info.AddValue("Offset", offset);
        }
        #endregion serialization

        #region Do Not Use

        /// <summary>
        /// Constructs a ParameterBindingException.
        /// </summary>
        /// 
        /// <remarks>
        /// DO NOT USE!!!
        /// </remarks>
        public ParameterBindingException() : base() { ;}

        /// <summary>
        /// Constructors a ParameterBindingException
        /// </summary>
        /// 
        /// <param name="message"> 
        /// Message to be included in exception.
        /// </param>
        /// 
        /// <remarks>
        /// DO NOT USE!!!
        /// </remarks>
        public ParameterBindingException(string message) : base(message) { this.message = message; }

        /// <summary>
        /// Constructs a ParameterBindingException
        /// </summary>
        /// 
        /// <param name="message">
        /// Message to be included in the exception.
        /// </param>
        /// 
        /// <param name="innerException"> 
        /// exception that led to this exception 
        /// </param>
        /// 
        /// <remarks>
        /// DO NOT USE!!!
        /// </remarks>
        public ParameterBindingException(
            string message, 
            Exception innerException) 
            : base (message, innerException) { this.message = message;}

        #endregion Do Not Use
        #endregion Constructors

        #region Properties
        /// <summary>
        /// Gets the message for the exception
        /// </summary>
        public override string Message
        {
            get
            {
                if (message == null)
                {
                    message = BuildMessage();
                }
                return message;
            }
        }

        private string message;

        /// <summary>
        /// Gets the name of the parameter that the parameter binding
        /// error was encountered on.
        /// </summary>
        public string ParameterName
        {
            get
            {
                return parameterName;
            }
        }
        private string parameterName = String.Empty;

        /// <summary>
        /// Gets the type the parameter is expecting.
        /// </summary>
        public Type ParameterType
        {
            get
            {
                return parameterType;
            }
        }
        private Type parameterType;

        /// <summary>
        /// Gets the Type that was specified as the parameter value
        /// </summary>
        public Type TypeSpecified
        {
            get
            {
                return typeSpecified;
            }
        }
        private Type typeSpecified;

        /// <summary>
        /// Gets the errorId of this ParameterBindingException
        /// </summary>
        public string ErrorId
        {
            get
            {
                return errorId;
            }
        }
        private string errorId;

        /// <summary>
        /// Gets the line in the script at which the error occurred.
        /// </summary>
        public Int64 Line
        {
            get
            {
                return line;
            }
        }
        private Int64 line = Int64.MinValue;

        /// <summary>
        /// Gets the offset on the line in the script at which the error occurred.
        /// </summary>
        public Int64 Offset
        {
            get
            {
                return offset;
            }
        }
        private Int64 offset = Int64.MinValue;

        /// <summary>
        /// Gets the invocation information about the command.
        /// </summary>
        public InvocationInfo CommandInvocation
        {
            get
            {
                return invocationInfo;
            }
        }
        private InvocationInfo invocationInfo;
        #endregion Properties

        #region private

        private string resourceString;
        private object[] args = new object[0];
        private string commandName;

        private string BuildMessage()            
        {
            object[] messageArgs = new object[0];

            if (args != null)
            {
                messageArgs = new object[args.Length + 6];
                messageArgs[0] = commandName;
                messageArgs[1] = parameterName;
                messageArgs[2] = parameterType;
                messageArgs[3] = typeSpecified;
                messageArgs[4] = line;
                messageArgs[5] = offset;
                args.CopyTo(messageArgs, 6);
            }

            string result = String.Empty;

            if (!String.IsNullOrEmpty(resourceString))
            {
                result = StringUtil.Format(resourceString, messageArgs);
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
        /// Constructs a ParameterBindingValidationException
        /// </summary>
        /// 
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// 
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        /// 
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// 
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        /// 
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// 
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        /// 
        /// parameterName == {1}
        /// </param>
        /// 
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        /// 
        /// parameterType == {2}
        /// </param>
        /// 
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        /// 
        /// typeSpecified == {3}
        /// </param>
        /// 
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// 
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// 
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        /// 
        /// starts at {6}
        /// </param>
        /// 
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceString"/> or <paramref name="errorId"/>
        /// is null or empty.
        /// </exception>
        /// 
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
        /// Constructs a ParameterBindingValidationException
        /// </summary>
        /// 
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        /// 
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// 
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        /// 
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// 
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        /// 
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// 
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        /// 
        /// parameterName == {1}
        /// </param>
        /// 
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        /// 
        /// parameterType == {2}
        /// </param>
        /// 
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        /// 
        /// typeSpecified == {3}
        /// </param>
        /// 
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// 
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// 
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        /// 
        /// starts at {6}
        /// </param>
        /// 
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="invocationInfo"/> is null.
        /// </exception>
        /// 
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceBaseName"/> or <paramref name="errorIdAndResourceId"/>
        /// is null or empty.
        /// </exception>
        /// 
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
                this._swallowException = true;
            }
        }
        #endregion Preferred constructors

        #region serialization
        /// <summary>
        /// Constructs a ParameterBindingValidationException from serialized data
        /// </summary>
        /// 
        /// <param name="info"> 
        /// serialization information 
        /// </param>
        /// 
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
        /// 
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
        /// Constructs a ParameterBindingArgumentTransformationException
        /// </summary>
        /// 
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// 
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        /// 
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// 
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        /// 
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// 
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        /// 
        /// parameterName == {1}
        /// </param>
        /// 
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        /// 
        /// parameterType == {2}
        /// </param>
        /// 
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        /// 
        /// typeSpecified == {3}
        /// </param>
        /// 
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// 
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// 
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        /// 
        /// starts at {6}
        /// </param>
        /// 
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceString"/> or <paramref name="errorId"/>
        /// is null or empty.
        /// </exception>
        /// 
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
        /// Constructs a ParameterBindingArgumentTransformationException
        /// </summary>
        /// 
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        /// 
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// 
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        /// 
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// 
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        /// 
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// 
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        /// 
        /// parameterName == {1}
        /// </param>
        /// 
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        /// 
        /// parameterType == {2}
        /// </param>
        /// 
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        /// 
        /// typeSpecified == {3}
        /// </param>
        /// 
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// 
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// 
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        /// 
        /// starts at {6}
        /// </param>
        /// 
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="invocationInfo"/> is null.
        /// </exception>
        /// 
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceString"/> or <paramref name="errorId"/>
        /// is null or empty.
        /// </exception>
        /// 
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
        /// Constructs a ParameterBindingArgumentTransformationException using serialized data
        /// </summary>
        /// 
        /// <param name="info"> 
        /// serialization information 
        /// </param>
        /// 
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
        /// Constructs a ParameterBindingParameterDefaultValueException
        /// </summary>
        /// 
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// 
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        /// 
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// 
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        /// 
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// 
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        /// 
        /// parameterName == {1}
        /// </param>
        /// 
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        /// 
        /// parameterType == {2}
        /// </param>
        /// 
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        /// 
        /// typeSpecified == {3}
        /// </param>
        /// 
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// 
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// 
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        /// 
        /// starts at {6}
        /// </param>
        /// 
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceString"/> or <paramref name="errorId"/>
        /// is null or empty.
        /// </exception>
        /// 
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
        /// Constructs a ParameterBindingParameterDefaultValueException
        /// </summary>
        /// 
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        /// 
        /// <param name="errorCategory">
        /// The category for the error.
        /// </param>
        /// 
        /// <param name="invocationInfo">
        /// The information about the command that encountered the error.
        /// 
        /// InvocationInfo.MyCommand.Name == {0}
        /// </param>
        /// 
        /// <param name="errorPosition">
        /// The position for the command or parameter that caused the error.
        /// 
        /// token.LineNumber == {4}
        /// token.OffsetInLine == {5}
        /// </param>
        /// 
        /// <param name="parameterName">
        /// The parameter on which binding caused the error.
        /// 
        /// parameterName == {1}
        /// </param>
        /// 
        /// <param name="parameterType">
        /// The Type the parameter was expecting.
        /// 
        /// parameterType == {2}
        /// </param>
        /// 
        /// <param name="typeSpecified">
        /// The Type that was attempted to be bound to the parameter.
        /// 
        /// typeSpecified == {3}
        /// </param>
        /// 
        /// <param name="resourceString">
        /// The format string for the exception message.
        /// </param>
        /// 
        /// <param name="errorId">
        /// The error ID.
        /// </param>
        /// 
        /// <param name="args">
        /// Additional arguments to pass to the format string.
        /// 
        /// starts at {6}
        /// </param>
        /// 
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="invocationInfo"/> is null.
        /// </exception>
        /// 
        /// <exception cref="ArgumentException">
        /// If <paramref name="resourceString"/> or <paramref name="errorId"/>
        /// is null or empty.
        /// </exception>
        /// 
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
        /// Constructs a ParameterBindingParameterDefaultValueException using serialized data
        /// </summary>
        /// 
        /// <param name="info"> 
        /// serialization information 
        /// </param>
        /// 
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
    
} // namespace System.Management.Automation

