// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56506

using System;
using System.Management.Automation;
using System.Runtime.Serialization;
using System.Reflection;
using System.Security.Permissions;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The exception that is thrown when there is no help category matching
    /// a specific input string.
    /// </summary>
    [Serializable]
    public class HelpCategoryInvalidException : ArgumentException, IContainsErrorRecord
    {
        /// <summary>
        /// Initializes a new instance of the HelpCategoryInvalidException class.
        /// </summary>
        /// <param name="helpCategory">The name of help category that is invalid.</param>
        public HelpCategoryInvalidException(string helpCategory)
            : base()
        {
            _helpCategory = helpCategory;
            CreateErrorRecord();
        }

        /// <summary>
        /// Initializes a new instance of the HelpCategoryInvalidException class.
        /// </summary>
        public HelpCategoryInvalidException()
            : base()
        {
            CreateErrorRecord();
        }

        /// <summary>
        /// Initializes a new instance of the HelpCategoryInvalidException class.
        /// </summary>
        /// <param name="helpCategory">The name of help category that is invalid.</param>
        /// <param name="innerException">The inner exception of this exception.</param>
        public HelpCategoryInvalidException(string helpCategory, Exception innerException)
            : base(
                  (innerException != null) ? innerException.Message : string.Empty,
                  innerException)
        {
            _helpCategory = helpCategory;
            CreateErrorRecord();
        }

        /// <summary>
        /// Creates an internal error record based on helpCategory.
        /// </summary>
        private void CreateErrorRecord()
        {
            _errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(this), "HelpCategoryInvalid", ErrorCategory.InvalidArgument, null);
            _errorRecord.ErrorDetails = new ErrorDetails(typeof(HelpCategoryInvalidException).Assembly, "HelpErrors", "HelpCategoryInvalid", _helpCategory);
        }

        private ErrorRecord _errorRecord;

        /// <summary>
        /// Gets ErrorRecord embedded in this exception.
        /// </summary>
        /// <value>ErrorRecord instance</value>
        public ErrorRecord ErrorRecord
        {
            get
            {
                return _errorRecord;
            }
        }

        private readonly string _helpCategory = System.Management.Automation.HelpCategory.None.ToString();

        /// <summary>
        /// Gets name of the help category that is invalid.
        /// </summary>
        /// <value>Name of the help category.</value>
        public string HelpCategory
        {
            get
            {
                return _helpCategory;
            }
        }

        /// <summary>
        /// Gets exception message for this exception.
        /// </summary>
        /// <value>Error message.</value>
        public override string Message
        {
            get
            {
                if (_errorRecord != null)
                {
                    return _errorRecord.ToString();
                }

                return base.Message;
            }
        }

        #region Serialization
        /// <summary>
        /// Initializes a new instance of the HelpCategoryInvalidException class.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected HelpCategoryInvalidException(SerializationInfo info,
                                        StreamingContext context)
            : base(info, context)
        {
            _helpCategory = info.GetString("HelpCategory");
            CreateErrorRecord();
        }

        /// <summary>
        /// Populates a <see cref="System.Runtime.Serialization.SerializationInfo"/> with the
        /// data needed to serialize the HelpCategoryInvalidException object.
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination for this serialization.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(info));
            }

            base.GetObjectData(info, context);

            info.AddValue("HelpCategory", this._helpCategory);
        }

        #endregion Serialization
    }
}

#pragma warning restore 56506
