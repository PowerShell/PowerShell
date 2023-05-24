// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.Serialization;

using Microsoft.Management.Infrastructure;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Represents an error during execution of a CIM job.
    /// </summary>
    public class CimJobException : SystemException, IContainsErrorRecord
    {
        #region Standard constructors and methods required for all exceptions

        /// <summary>
        /// Initializes a new instance of the <see cref="CimJobException"/> class.
        /// </summary>
        public CimJobException() : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimJobException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CimJobException(string message) : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimJobException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public CimJobException(string message, Exception inner) : base(message, inner)
        {
            InitializeErrorRecord(null, "CimJob_ExternalError", ErrorCategory.NotSpecified);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimJobException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected CimJobException(
            SerializationInfo info,
            StreamingContext context)
        {
            throw new NotSupportedException();
        }
        
        #endregion

        internal static CimJobException CreateFromCimException(
            string jobDescription,
            CimJobContext jobContext,
            CimException cimException)
        {
            Dbg.Assert(!string.IsNullOrEmpty(jobDescription), "Caller should verify jobDescription != null");
            Dbg.Assert(jobContext != null, "Caller should verify jobContext != null");
            Dbg.Assert(cimException != null, "Caller should verify cimException != null");

            string message = BuildErrorMessage(jobDescription, jobContext, cimException.Message);
            CimJobException cimJobException = new(message, cimException);
            cimJobException.InitializeErrorRecord(jobContext, cimException);
            return cimJobException;
        }

        internal static CimJobException CreateFromAnyException(
            string jobDescription,
            CimJobContext jobContext,
            Exception inner)
        {
            Dbg.Assert(!string.IsNullOrEmpty(jobDescription), "Caller should verify jobDescription != null");
            Dbg.Assert(jobContext != null, "Caller should verify jobContext != null");
            Dbg.Assert(inner != null, "Caller should verify inner != null");

            CimException cimException = inner as CimException;
            if (cimException != null)
            {
                return CreateFromCimException(jobDescription, jobContext, cimException);
            }

            string message = BuildErrorMessage(jobDescription, jobContext, inner.Message);
            CimJobException cimJobException = new(message, inner);
            var containsErrorRecord = inner as IContainsErrorRecord;
            if (containsErrorRecord != null)
            {
                cimJobException.InitializeErrorRecord(
                    jobContext,
                    errorId: "CimJob_" + containsErrorRecord.ErrorRecord.FullyQualifiedErrorId,
                    errorCategory: containsErrorRecord.ErrorRecord.CategoryInfo.Category);
            }
            else
            {
                cimJobException.InitializeErrorRecord(
                    jobContext,
                    errorId: "CimJob_" + inner.GetType().Name,
                    errorCategory: ErrorCategory.NotSpecified);
            }

            return cimJobException;
        }

        internal static CimJobException CreateWithFullControl(
            CimJobContext jobContext,
            string message,
            string errorId,
            ErrorCategory errorCategory,
            Exception inner = null)
        {
            Dbg.Assert(jobContext != null, "Caller should verify jobContext != null");
            Dbg.Assert(!string.IsNullOrEmpty(message), "Caller should verify message != null");
            Dbg.Assert(!string.IsNullOrEmpty(errorId), "Caller should verify errorId != null");

            CimJobException cimJobException = new(jobContext.PrependComputerNameToMessage(message), inner);
            cimJobException.InitializeErrorRecord(jobContext, errorId, errorCategory);
            return cimJobException;
        }

        internal static CimJobException CreateWithoutJobContext(
            string message,
            string errorId,
            ErrorCategory errorCategory,
            Exception inner = null)
        {
            Dbg.Assert(!string.IsNullOrEmpty(message), "Caller should verify message != null");
            Dbg.Assert(!string.IsNullOrEmpty(errorId), "Caller should verify errorId != null");

            CimJobException cimJobException = new(message, inner);
            cimJobException.InitializeErrorRecord(null, errorId, errorCategory);
            return cimJobException;
        }

        internal static CimJobException CreateFromMethodErrorCode(string jobDescription, CimJobContext jobContext, string methodName, string errorCodeFromMethod)
        {
            string rawErrorMessage = string.Format(
                CultureInfo.InvariantCulture,
                CmdletizationResources.CimJob_ErrorCodeFromMethod,
                errorCodeFromMethod);

            string errorMessage = BuildErrorMessage(jobDescription, jobContext, rawErrorMessage);

            CimJobException cje = new(errorMessage);
            cje.InitializeErrorRecord(jobContext, "CimJob_" + methodName + "_" + errorCodeFromMethod, ErrorCategory.InvalidResult);

            return cje;
        }

        private static string BuildErrorMessage(string jobDescription, CimJobContext jobContext, string errorMessage)
        {
            Dbg.Assert(!string.IsNullOrEmpty(errorMessage), "Caller should verify !string.IsNullOrEmpty(errorMessage)");

            if (string.IsNullOrEmpty(jobDescription))
            {
                return jobContext.PrependComputerNameToMessage(errorMessage);
            }
            else
            {
                string errorMessageWithJobDescription = string.Format(
                    CultureInfo.InvariantCulture,
                    CmdletizationResources.CimJob_GenericCimFailure,
                    errorMessage,
                    jobDescription);
                return jobContext.PrependComputerNameToMessage(errorMessageWithJobDescription);
            }
        }

        private void InitializeErrorRecordCore(CimJobContext jobContext, Exception exception, string errorId, ErrorCategory errorCategory)
        {
            ErrorRecord coreErrorRecord = new(
                exception: exception,
                errorId: errorId,
                errorCategory: errorCategory,
                targetObject: jobContext?.TargetObject);

            if (jobContext != null)
            {
                System.Management.Automation.Remoting.OriginInfo originInfo = new(
                    jobContext.Session.ComputerName,
                    Guid.Empty);

                _errorRecord = new System.Management.Automation.Runspaces.RemotingErrorRecord(
                    coreErrorRecord,
                    originInfo);

                _errorRecord.SetInvocationInfo(jobContext.CmdletInvocationInfo);
                _errorRecord.PreserveInvocationInfoOnce = true;
            }
            else
            {
                _errorRecord = coreErrorRecord;
            }
        }

        private void InitializeErrorRecord(CimJobContext jobContext, string errorId, ErrorCategory errorCategory)
        {
            InitializeErrorRecordCore(
                jobContext: jobContext,
                exception: this,
                errorId: errorId,
                errorCategory: errorCategory);
        }

        private void InitializeErrorRecord(CimJobContext jobContext, CimException cimException)
        {
            InitializeErrorRecordCore(
                jobContext: jobContext,
                exception: cimException,
                errorId: cimException.MessageId ?? "MiClientApiError_" + cimException.NativeErrorCode,
                errorCategory: ConvertCimExceptionToErrorCategory(cimException));

            if (cimException.ErrorData != null)
            {
                _errorRecord.CategoryInfo.TargetName = cimException.ErrorSource;
                _errorRecord.CategoryInfo.TargetType = jobContext?.CmdletizationClassName;
            }
        }

        private static ErrorCategory ConvertCimExceptionToErrorCategory(CimException cimException)
        {
            ErrorCategory result = ErrorCategory.NotSpecified;

            if (cimException.ErrorData != null)
            {
                result = ConvertCimErrorToErrorCategory(cimException.ErrorData);
            }

            if (result == ErrorCategory.NotSpecified)
            {
                result = ConvertCimNativeErrorCodeToErrorCategory(cimException.NativeErrorCode);
            }

            return result;
        }

        private static ErrorCategory ConvertCimNativeErrorCodeToErrorCategory(NativeErrorCode nativeErrorCode)
        {
            switch (nativeErrorCode)
            {
                case NativeErrorCode.Ok:
                    return ErrorCategory.NotSpecified;
                case NativeErrorCode.Failed:
                    return ErrorCategory.NotSpecified;
                case NativeErrorCode.AccessDenied:
                    return ErrorCategory.PermissionDenied;
                case NativeErrorCode.InvalidNamespace:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.InvalidParameter:
                    return ErrorCategory.InvalidArgument;
                case NativeErrorCode.InvalidClass:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.NotFound:
                    return ErrorCategory.ObjectNotFound;
                case NativeErrorCode.NotSupported:
                    return ErrorCategory.NotImplemented;
                case NativeErrorCode.ClassHasChildren:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.ClassHasInstances:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.InvalidSuperClass:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.AlreadyExists:
                    return ErrorCategory.ResourceExists;
                case NativeErrorCode.NoSuchProperty:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.TypeMismatch:
                    return ErrorCategory.InvalidType;
                case NativeErrorCode.QueryLanguageNotSupported:
                    return ErrorCategory.NotImplemented;
                case NativeErrorCode.InvalidQuery:
                    return ErrorCategory.InvalidArgument;
                case NativeErrorCode.MethodNotAvailable:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.MethodNotFound:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.NamespaceNotEmpty:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.InvalidEnumerationContext:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.InvalidOperationTimeout:
                    return ErrorCategory.InvalidArgument;
                case NativeErrorCode.PullHasBeenAbandoned:
                    return ErrorCategory.OperationStopped;
                case NativeErrorCode.PullCannotBeAbandoned:
                    return ErrorCategory.CloseError;
                case NativeErrorCode.FilteredEnumerationNotSupported:
                    return ErrorCategory.NotImplemented;
                case NativeErrorCode.ContinuationOnErrorNotSupported:
                    return ErrorCategory.NotImplemented;
                case NativeErrorCode.ServerLimitsExceeded:
                    return ErrorCategory.ResourceBusy;
                case NativeErrorCode.ServerIsShuttingDown:
                    return ErrorCategory.ResourceUnavailable;
                default:
                    return ErrorCategory.NotSpecified;
            }
        }

        private static ErrorCategory ConvertCimErrorToErrorCategory(CimInstance cimError)
        {
            if (cimError == null)
            {
                return ErrorCategory.NotSpecified;
            }

            CimProperty errorCategoryProperty = cimError.CimInstanceProperties["Error_Category"];
            if (errorCategoryProperty == null)
            {
                return ErrorCategory.NotSpecified;
            }

            ErrorCategory errorCategoryValue;
            if (!LanguagePrimitives.TryConvertTo<ErrorCategory>(errorCategoryProperty.Value, CultureInfo.InvariantCulture, out errorCategoryValue))
            {
                return ErrorCategory.NotSpecified;
            }

            return errorCategoryValue;
        }

        /// <summary>
        /// <see cref="ErrorRecord"/> which provides additional information about the error.
        /// </summary>
        public ErrorRecord ErrorRecord
        {
            get { return _errorRecord; }
        }

        private ErrorRecord _errorRecord;

        internal bool IsTerminatingError
        {
            get
            {
                var cimException = this.InnerException as CimException;
                if ((cimException == null) || (cimException.ErrorData == null))
                {
                    return false;
                }

                CimProperty perceivedSeverityProperty = cimException.ErrorData.CimInstanceProperties["PerceivedSeverity"];
                if ((perceivedSeverityProperty == null) || (perceivedSeverityProperty.CimType != CimType.UInt16) || (perceivedSeverityProperty.Value == null))
                {
                    return false;
                }

                ushort perceivedSeverityValue = (ushort)perceivedSeverityProperty.Value;
                if (perceivedSeverityValue != 7)
                {
                    /* from CIM Schema: Interop\CIM_Error.mof:
                         "7 - Fatal/NonRecoverable should be used to indicate an "
                          "error occurred, but it\'s too late to take remedial "
                          "action. \n"
                     */
                    return false;
                }

                return true;
            }
        }
    }
}
