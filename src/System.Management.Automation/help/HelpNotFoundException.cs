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
    /// The exception that is thrown when there is no help found for a topic.
    /// </summary>
    [Serializable]
    public class HelpNotFoundException : SystemException, IContainsErrorRecord
    {
        /// <summary>
        /// Initializes a new instance of the HelpNotFoundException class with the give help topic.
        /// </summary>
        /// <param name="helpTopic">The help topic for which help is not found.</param>
        public HelpNotFoundException(string helpTopic)
            : base()
        {
            _helpTopic = helpTopic;
            CreateErrorRecord();
        }

        /// <summary>
        /// Initializes a new instance of the HelpNotFoundException class.
        /// </summary>
        public HelpNotFoundException()
            : base()
        {
            CreateErrorRecord();
        }

        /// <summary>
        /// Initializes a new instance of the HelpNotFoundException class with the given help topic
        /// and associated exception.
        /// </summary>
        /// <param name="helpTopic">The help topic for which help is not found.</param>
        /// <param name="innerException">The inner exception.</param>
        public HelpNotFoundException(string helpTopic, Exception innerException)
            : base(
                  (innerException != null) ? innerException.Message : string.Empty,
                  innerException)
        {
            _helpTopic = helpTopic;
            CreateErrorRecord();
        }

        /// <summary>
        /// Creates an internal error record based on helpTopic.
        /// The ErrorRecord created will be stored in the _errorRecord member.
        /// </summary>
        private void CreateErrorRecord()
        {
            string errMessage = string.Format(HelpErrors.HelpNotFound, _helpTopic);

            // Don't do ParentContainsErrorRecordException(this), as this causes recursion, and creates a
            // segmentation fault on Linux
            _errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(errMessage), "HelpNotFound", ErrorCategory.ResourceUnavailable, null);
            _errorRecord.ErrorDetails = new ErrorDetails(typeof(HelpNotFoundException).Assembly, "HelpErrors", "HelpNotFound", _helpTopic);
        }

        private ErrorRecord _errorRecord;

        /// <summary>
        /// Gets ErrorRecord embedded in this exception.
        /// </summary>
        /// <value>ErrorRecord instance.</value>
        public ErrorRecord ErrorRecord
        {
            get
            {
                return _errorRecord;
            }
        }

        private readonly string _helpTopic = string.Empty;

        /// <summary>
        /// Gets help topic for which help is not found.
        /// </summary>
        /// <value>Help topic.</value>
        public string HelpTopic
        {
            get
            {
                return _helpTopic;
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
        /// Initializes a new instance of the HelpNotFoundException class.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected HelpNotFoundException(SerializationInfo info,
                                        StreamingContext context)
            : base(info, context)
        {
            _helpTopic = info.GetString("HelpTopic");
            CreateErrorRecord();
        }

        /// <summary>
        /// Populates a <see cref="System.Runtime.Serialization.SerializationInfo"/> with the
        /// data needed to serialize the HelpNotFoundException object.
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

            info.AddValue("HelpTopic", this._helpTopic);
        }

        #endregion Serialization
    }
}

#pragma warning restore 56506
