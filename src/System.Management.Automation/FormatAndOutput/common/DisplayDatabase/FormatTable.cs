// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Runtime.Serialization;
using System.Security.Permissions;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// This exception is used by Formattable constructor to indicate errors
    /// occurred during construction time.
    /// </summary>    
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "FormatTable")]
    public class FormatTableLoadException : RuntimeException
    {
        private readonly Collection<string> _errors;

        #region Constructors

        /// <summary>
        /// This is the default constructor.
        /// </summary>
        public FormatTableLoadException()
            : base()
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes a localized error message.
        /// </summary>
        /// <param name="message">
        /// A localized error message.
        /// </param>
        public FormatTableLoadException(string message)
            : base(message)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes a localized message and an inner exception.
        /// </summary>
        /// <param name="message">
        /// Localized error message.
        /// </param>
        /// <param name="innerException">
        /// Inner exception.
        /// </param>
        public FormatTableLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes a collection of errors occurred during construction
        /// time.
        /// </summary>
        /// <param name="loadErrors">
        /// The errors that occurred.
        /// </param>
        internal FormatTableLoadException(ConcurrentBag<string> loadErrors)
            : base(StringUtil.Format(FormatAndOutXmlLoadingStrings.FormatTableLoadErrors))
        {
            _errors = new Collection<string>(loadErrors.ToArray());
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor is required by serialization.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected FormatTableLoadException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion Constructors

        /// <summary>
        /// Set the default ErrorRecord.
        /// </summary>
        protected void SetDefaultErrorRecord()
        {
            SetErrorCategory(ErrorCategory.InvalidData);
            SetErrorId(typeof(FormatTableLoadException).FullName);
        }

        /// <summary>
        /// The specific Formattable load errors.
        /// </summary>
        public Collection<string> Errors
        {
            get
            {
                return _errors;
            }
        }
    }

    /// <summary>
    /// A class that keeps the information from format.ps1xml files in a cache table.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "FormatTable")]
    public sealed class FormatTable
    {
        #region Private Data

        private readonly TypeInfoDataBaseManager _formatDBMgr;

        #endregion

        #region Constructor

        /// <summary>
        /// Default Constructor.
        /// </summary>
        internal FormatTable()
        {
            _formatDBMgr = new TypeInfoDataBaseManager();
        }

        /// <summary>
        /// Constructor that creates a FormatTable from a set of format files.
        /// </summary>
        /// <param name="formatFiles">
        /// Format files to load for format information.
        /// </param>
        /// <exception cref="ArgumentException">
        /// 1. Path {0} is not fully qualified. Specify a fully qualified type file path.
        /// </exception>
        /// <exception cref="FormatTableLoadException">
        /// 1. There were errors loading Formattable. Look in the Errors property to
        /// get detailed error messages.
        /// </exception>
        public FormatTable(IEnumerable<string> formatFiles) : this(formatFiles, null, null)
        {
        }

        /// <summary>
        /// Append the formatData to the list of formatting configurations, and update the
        /// entire formatting database.
        /// </summary>
        /// <param name="formatData">
        /// The formatData is of type 'ExtendedTypeDefinition'. It defines the View configuration
        /// including TableControl, ListControl, and WideControl.
        /// </param>
        /// <exception cref="FormatTableLoadException">
        /// 1. There were errors loading Formattable. Look in the Errors property to
        /// get detailed error messages.
        /// </exception>
        public void AppendFormatData(IEnumerable<ExtendedTypeDefinition> formatData)
        {
            if (formatData == null)
                throw PSTraceSource.NewArgumentNullException(nameof(formatData));
            _formatDBMgr.AddFormatData(formatData, false);
        }

        /// <summary>
        /// Prepend the formatData to the list of formatting configurations, and update the
        /// entire formatting database.
        /// </summary>
        /// <param name="formatData">
        /// The formatData is of type 'ExtendedTypeDefinition'. It defines the View configuration
        /// including TableControl, ListControl, and WideControl.
        /// </param>
        /// <exception cref="FormatTableLoadException">
        /// 1. There were errors loading Formattable. Look in the Errors property to
        /// get detailed error messages.
        /// </exception>
        public void PrependFormatData(IEnumerable<ExtendedTypeDefinition> formatData)
        {
            if (formatData == null)
                throw PSTraceSource.NewArgumentNullException(nameof(formatData));
            _formatDBMgr.AddFormatData(formatData, true);
        }

        /// <summary>
        /// Constructor that creates a FormatTable from a set of format files.
        /// </summary>
        /// <param name="formatFiles">
        /// Format files to load for format information.
        /// </param>
        /// <param name="authorizationManager">
        /// Authorization manager to perform signature checks before reading ps1xml files (or null of no checks are needed)
        /// </param>
        /// <param name="host">
        /// Host passed to <paramref name="authorizationManager"/>.  Can be null if no interactive questions should be asked.
        /// </param>
        /// <exception cref="ArgumentException">
        /// 1. Path {0} is not fully qualified. Specify a fully qualified type file path.
        /// </exception>
        /// <exception cref="FormatTableLoadException">
        /// 1. There were errors loading Formattable. Look in the Errors property to
        /// get detailed error messages.
        /// </exception>
        internal FormatTable(IEnumerable<string> formatFiles, AuthorizationManager authorizationManager, PSHost host)
        {
            if (formatFiles == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(formatFiles));
            }

            _formatDBMgr = new TypeInfoDataBaseManager(formatFiles, true, authorizationManager, host);
        }

        #endregion

        #region Internal Methods / Properties

        internal TypeInfoDataBaseManager FormatDBManager
        {
            get { return _formatDBMgr; }
        }

        /// <summary>
        /// Adds the <paramref name="formatFile"/> to the current FormatTable's file list.
        /// The FormatTable will not reflect the change until Update is called.
        /// </summary>
        /// <param name="formatFile"></param>
        /// <param name="shouldPrepend">
        /// if true, <paramref name="formatFile"/> is prepended to the current FormatTable's file list.
        /// if false, it will be appended.
        /// </param>
        internal void Add(string formatFile, bool shouldPrepend)
        {
            _formatDBMgr.Add(formatFile, shouldPrepend);
        }

        /// <summary>
        /// Removes the <paramref name="formatFile"/> from the current FormatTable's file list.
        /// The FormatTable will not reflect the change until Update is called.
        /// </summary>
        /// <param name="formatFile"></param>
        internal void Remove(string formatFile)
        {
            _formatDBMgr.Remove(formatFile);
        }

        #endregion

        #region static methods

        /// <summary>
        /// Returns a format table instance with all default
        /// format files loaded.
        /// </summary>
        /// <returns></returns>
        public static FormatTable LoadDefaultFormatFiles()
        {
            string psHome = Utils.DefaultPowerShellAppBase;
            List<string> defaultFormatFiles = new List<string>();
            if (!string.IsNullOrEmpty(psHome))
            {
                defaultFormatFiles.AddRange(Platform.FormatFileNames.Select(file => Path.Combine(psHome, file)));
            }

            return new FormatTable(defaultFormatFiles);
        }
        #endregion static methods
    }
}
