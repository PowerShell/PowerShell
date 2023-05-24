// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Internal;
using System.Runtime.Serialization;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines the exception thrown for all Metadata errors.
    /// </summary>
    public class MetadataException : RuntimeException
    {
        internal const string MetadataMemberInitialization = "MetadataMemberInitialization";
        internal const string BaseName = "Metadata";
        /// <summary>
        /// Initializes a new instance of MetadataException with the message set
        /// to typeof(MetadataException).FullName.
        /// </summary>
        public MetadataException() : base(typeof(MetadataException).FullName)
        {
            SetErrorCategory(ErrorCategory.MetadataError);
        }

        /// <summary>
        /// Initializes a new instance of MetadataException setting the message.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public MetadataException(string message) : base(message)
        {
            SetErrorCategory(ErrorCategory.MetadataError);
        }

        /// <summary>
        /// Initializes a new instance of MetadataException setting the message and innerException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public MetadataException(string message, Exception innerException) : base(message, innerException)
        {
            SetErrorCategory(ErrorCategory.MetadataError);
        }

        internal MetadataException(
            string errorId,
            Exception innerException,
            string resourceStr,
            params object[] arguments)
            : base(
                  StringUtil.Format(resourceStr, arguments),
                  innerException)
        {
            SetErrorCategory(ErrorCategory.MetadataError);
            SetErrorId(errorId);
        }
    }

    /// <summary>
    /// Defines the exception thrown for all Validate attributes.
    /// </summary>
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    public class ValidationMetadataException : MetadataException
    {
        internal const string ValidateRangeElementType = "ValidateRangeElementType";
        internal const string ValidateRangePositiveFailure = "ValidateRangePositiveFailure";
        internal const string ValidateRangeNonNegativeFailure = "ValidateRangeNonNegativeFailure";
        internal const string ValidateRangeNegativeFailure = "ValidateRangeNegativeFailure";
        internal const string ValidateRangeNonPositiveFailure = "ValidateRangeNonPositiveFailure";
        internal const string ValidateRangeMinRangeMaxRangeType = "ValidateRangeMinRangeMaxRangeType";
        internal const string ValidateRangeNotIComparable = "ValidateRangeNotIComparable";
        internal const string ValidateRangeMaxRangeSmallerThanMinRange = "ValidateRangeMaxRangeSmallerThanMinRange";
        internal const string ValidateRangeGreaterThanMaxRangeFailure = "ValidateRangeGreaterThanMaxRangeFailure";
        internal const string ValidateRangeSmallerThanMinRangeFailure = "ValidateRangeSmallerThanMinRangeFailure";

        internal const string ValidateFailureResult = "ValidateFailureResult";

        internal const string ValidatePatternFailure = "ValidatePatternFailure";
        internal const string ValidateScriptFailure = "ValidateScriptFailure";

        internal const string ValidateCountNotInArray = "ValidateCountNotInArray";
        internal const string ValidateCountMaxLengthSmallerThanMinLength = "ValidateCountMaxLengthSmallerThanMinLength";
        internal const string ValidateCountMinLengthFailure = "ValidateCountMinLengthFailure";
        internal const string ValidateCountMaxLengthFailure = "ValidateCountMaxLengthFailure";

        internal const string ValidateLengthMaxLengthSmallerThanMinLength = "ValidateLengthMaxLengthSmallerThanMinLength";
        internal const string ValidateLengthNotString = "ValidateLengthNotString";
        internal const string ValidateLengthMinLengthFailure = "ValidateLengthMinLengthFailure";
        internal const string ValidateLengthMaxLengthFailure = "ValidateLengthMaxLengthFailure";
        internal const string ValidateSetFailure = "ValidateSetFailure";
        internal const string ValidateVersionFailure = "ValidateVersionFailure";
        internal const string InvalidValueFailure = "InvalidValueFailure";

        /// <summary>
        /// Initializes a new instance of ValidationMetadataException with serialization parameters.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ValidationMetadataException(SerializationInfo info, StreamingContext context) 
        { 
            throw new NotSupportedException();
        }

        /// <summary>
        /// Initializes a new instance of ValidationMetadataException with the message set
        /// to typeof(ValidationMetadataException).FullName.
        /// </summary>
        public ValidationMetadataException() : base(typeof(ValidationMetadataException).FullName) { }
        /// <summary>
        /// Initializes a new instance of ValidationMetadataException setting the message.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public ValidationMetadataException(string message) : this(message, false) { }
        /// <summary>
        /// Initializes a new instance of ValidationMetadataException setting the message and innerException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public ValidationMetadataException(string message, Exception innerException) : base(message, innerException) { }

        internal ValidationMetadataException(
            string errorId,
            Exception innerException,
            string resourceStr,
            params object[] arguments)
            : base(errorId, innerException, resourceStr, arguments)
        {
        }

        /// <summary>
        /// Initialize a new instance of ValidationMetadataException. This validation exception could be
        /// ignored in positional binding phase if the <para>swallowException</para> is set to be true.
        /// </summary>
        /// <param name="message">
        /// The error message</param>
        /// <param name="swallowException">
        /// Indicate whether to swallow this exception in positional binding phase
        /// </param>
        internal ValidationMetadataException(string message, bool swallowException) : base(message)
        {
            _swallowException = swallowException;
        }

        /// <summary>
        /// Make the positional binding swallow this exception when it's set to true.
        /// </summary>
        /// <remarks>
        /// This property is only used internally in the positional binding phase
        /// </remarks>
        internal bool SwallowException
        {
            get { return _swallowException; }
        }

        private readonly bool _swallowException = false;
    }

    /// <summary>
    /// Defines the exception thrown for all ArgumentTransformation attributes.
    /// </summary>
    public class ArgumentTransformationMetadataException : MetadataException
    {
        internal const string ArgumentTransformationArgumentsShouldBeStrings = "ArgumentTransformationArgumentsShouldBeStrings";

        /// <summary>
        /// Initializes a new instance of ArgumentTransformationMetadataException with serialization parameters.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ArgumentTransformationMetadataException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Initializes a new instance of ArgumentTransformationMetadataException with the message set
        /// to typeof(ArgumentTransformationMetadataException).FullName.
        /// </summary>
        public ArgumentTransformationMetadataException()
            : base(typeof(ArgumentTransformationMetadataException).FullName) { }

        /// <summary>
        /// Initializes a new instance of ArgumentTransformationMetadataException setting the message.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public ArgumentTransformationMetadataException(string message)
            : base(message) { }

        /// <summary>
        /// Initializes a new instance of ArgumentTransformationMetadataException setting the message and innerException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public ArgumentTransformationMetadataException(string message, Exception innerException)
            : base(message, innerException) { }

        internal ArgumentTransformationMetadataException(
            string errorId,
            Exception innerException,
            string resourceStr,
            params object[] arguments)
            : base(errorId, innerException, resourceStr, arguments)
        {
        }
    }

    /// <summary>
    /// Defines the exception thrown for all parameter binding exceptions related to metadata attributes.
    /// </summary>
    public class ParsingMetadataException : MetadataException
    {
        internal const string ParsingTooManyParameterSets = "ParsingTooManyParameterSets";

        /// <summary>
        /// Initializes a new instance of ParsingMetadataException with serialization parameters.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ParsingMetadataException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Initializes a new instance of ParsingMetadataException with the message set
        /// to typeof(ParsingMetadataException).FullName.
        /// </summary>
        public ParsingMetadataException()
            : base(typeof(ParsingMetadataException).FullName) { }

        /// <summary>
        /// Initializes a new instance of ParsingMetadataException setting the message.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public ParsingMetadataException(string message)
            : base(message) { }

        /// <summary>
        /// Initializes a new instance of ParsingMetadataException setting the message and innerException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public ParsingMetadataException(string message, Exception innerException)
            : base(message, innerException) { }

        internal ParsingMetadataException(
            string errorId,
            Exception innerException,
            string resourceStr,
            params object[] arguments)
            : base(errorId, innerException, resourceStr, arguments)
        {
        }
    }
}
