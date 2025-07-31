// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using System.Runtime.Serialization;
using System.Text;

namespace System.Management.Automation
{
    /// <summary>
    /// This exception is thrown when a command cannot be found.
    /// </summary>
    public class CommandNotFoundException : RuntimeException
    {
        /// <summary>
        /// Constructs a CommandNotFoundException. This is the recommended constructor.
        /// </summary>
        /// <param name="commandName">
        /// The name of the command that could not be found.
        /// </param>
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        /// <param name="resourceStr">
        /// This string is message template string
        /// </param>
        /// <param name="errorIdAndResourceId">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// DiscoveryExceptions.txt.
        /// </param>
        /// <param name="messageArgs">
        /// Additional arguments to format into the message.
        /// </param>
        internal CommandNotFoundException(
            string commandName,
            Exception innerException,
            string errorIdAndResourceId,
            string resourceStr,
            params object[] messageArgs)
            : base(BuildMessage(commandName, resourceStr, messageArgs), innerException)
        {
            _commandName = commandName;
            _errorId = errorIdAndResourceId;
        }

        /// <summary>
        /// Constructs a CommandNotFoundException.
        /// </summary>
        public CommandNotFoundException() : base() { }

        /// <summary>
        /// Constructs a CommandNotFoundException.
        /// </summary>
        /// <param name="message">
        /// The message used in the exception.
        /// </param>
        public CommandNotFoundException(string message) : base(message) { }

        /// <summary>
        /// Constructs a CommandNotFoundException.
        /// </summary>
        /// <param name="message">
        /// The message used in the exception.
        /// </param>
        /// <param name="innerException">
        /// An exception that led to this exception.
        /// </param>
        public CommandNotFoundException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Serialization constructor for class CommandNotFoundException.
        /// </summary>
        /// <param name="info">
        /// serialization information
        /// </param>
        /// <param name="context">
        /// streaming context
        /// </param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected CommandNotFoundException(SerializationInfo info,
                                        StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #region Properties
        /// <summary>
        /// Gets the ErrorRecord information for this exception.
        /// </summary>
        public override ErrorRecord ErrorRecord
        {
            get
            {
                _errorRecord ??= new ErrorRecord(
                    new ParentContainsErrorRecordException(this),
                    _errorId,
                    _errorCategory,
                    _commandName);

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord;

        /// <summary>
        /// Gets the name of the command that could not be found.
        /// </summary>
        public string CommandName
        {
            get { return _commandName; }

            set { _commandName = value; }
        }

        private string _commandName = string.Empty;

        #endregion Properties

        #region Private
        private readonly string _errorId = "CommandNotFoundException";
        private readonly ErrorCategory _errorCategory = ErrorCategory.ObjectNotFound;

        private static string BuildMessage(
            string commandName,
            string resourceStr,
            params object[] messageArgs
            )
        {
            object[] a;
            if (messageArgs != null && messageArgs.Length > 0)
            {
                a = new object[messageArgs.Length + 1];
                a[0] = commandName;
                messageArgs.CopyTo(a, 1);
            }
            else
            {
                a = new object[1];
                a[0] = commandName;
            }

            return StringUtil.Format(resourceStr, a);
        }
        #endregion Private
    }
    /// <summary>
    /// Defines the exception thrown when a script's requirements to run specified by the #requires
    /// statements are not met.
    /// </summary>
    public class ScriptRequiresException : RuntimeException
    {
        /// <summary>
        /// Constructs an ScriptRequiresException. Recommended constructor for the class for
        /// #requires -shellId MyShellId.
        /// </summary>
        /// <param name="commandName">
        /// The name of the script containing the #requires statement.
        /// </param>
        /// <param name="requiresShellId">
        /// The ID of the shell that is incompatible with the current shell.
        /// </param>
        /// <param name="requiresShellPath">
        /// The path to the shell specified in the #requires -shellId statement.
        /// </param>
        /// <param name="errorId">
        /// The error id for this exception.
        /// </param>
        internal ScriptRequiresException(
            string commandName,
            string requiresShellId,
            string requiresShellPath,
            string errorId)
            : base(BuildMessage(commandName, requiresShellId, requiresShellPath, true))
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(commandName), "commandName is null or empty when constructing ScriptRequiresException");
            Diagnostics.Assert(!string.IsNullOrEmpty(errorId), "errorId is null or empty when constructing ScriptRequiresException");
            _commandName = commandName;
            _requiresShellId = requiresShellId;
            _requiresShellPath = requiresShellPath;
            this.SetErrorId(errorId);
            this.SetTargetObject(commandName);
            this.SetErrorCategory(ErrorCategory.ResourceUnavailable);
        }
        /// <summary>
        /// Constructs an ScriptRequiresException. Recommended constructor for the class for
        /// #requires -version N.
        /// </summary>
        /// <param name="commandName">
        /// The name of the script containing the #requires statement.
        /// </param>
        /// <param name="requiresPSVersion">
        /// The Msh version that the script requires.
        /// </param>
        /// <param name="currentPSVersion">
        /// The current Msh version
        /// </param>
        /// <param name="errorId">
        /// The error id for this exception.
        /// </param>
        internal ScriptRequiresException(
            string commandName,
            Version requiresPSVersion,
            string currentPSVersion,
            string errorId)
            : base(BuildMessage(commandName, requiresPSVersion.ToString(), currentPSVersion, false))
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(commandName), "commandName is null or empty when constructing ScriptRequiresException");
            Diagnostics.Assert(requiresPSVersion != null, "requiresPSVersion is null or empty when constructing ScriptRequiresException");
            Diagnostics.Assert(!string.IsNullOrEmpty(errorId), "errorId is null or empty when constructing ScriptRequiresException");
            _commandName = commandName;
            _requiresPSVersion = requiresPSVersion;
            this.SetErrorId(errorId);
            this.SetTargetObject(commandName);
            this.SetErrorCategory(ErrorCategory.ResourceUnavailable);
        }

        /// <summary>
        /// Constructs an ScriptRequiresException. Recommended constructor for the class for the
        /// #requires -PSSnapin MyPSSnapIn statement.
        /// </summary>
        /// <param name="commandName">
        /// The name of the script containing the #requires statement.
        /// </param>
        /// <param name="missingItems">
        /// The missing snap-ins/modules that the script requires.
        /// </param>
        /// /// <param name="forSnapins">
        /// Indicates whether the error message needs to be constructed for missing snap-ins/ missing modules.
        /// </param>
        /// <param name="errorId">
        /// The error id for this exception.
        /// </param>
        internal ScriptRequiresException(
            string commandName,
            Collection<string> missingItems,
            string errorId,
            bool forSnapins)
            : this(commandName, missingItems, errorId, forSnapins, null)
        {
        }

        /// <summary>
        /// Constructs an ScriptRequiresException. Recommended constructor for the class for the
        /// #requires -PSSnapin MyPSSnapIn statement.
        /// </summary>
        /// <param name="commandName">
        /// The name of the script containing the #requires statement.
        /// </param>
        /// <param name="missingItems">
        /// The missing snap-ins/modules that the script requires.
        /// </param>
        /// /// <param name="forSnapins">
        /// Indicates whether the error message needs to be constructed for missing snap-ins/ missing modules.
        /// </param>
        /// <param name="errorId">
        /// The error id for this exception.
        /// </param>
        /// <param name="errorRecord">
        /// The error Record for this exception.
        /// </param>
        internal ScriptRequiresException(
            string commandName,
            Collection<string> missingItems,
            string errorId,
            bool forSnapins,
            ErrorRecord errorRecord)
            : base(BuildMessage(commandName, missingItems, forSnapins), null, errorRecord)
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(commandName), "commandName is null or empty when constructing ScriptRequiresException");
            Diagnostics.Assert(missingItems != null && missingItems.Count > 0, "missingItems is null or empty when constructing ScriptRequiresException");
            Diagnostics.Assert(!string.IsNullOrEmpty(errorId), "errorId is null or empty when constructing ScriptRequiresException");
            _commandName = commandName;
            _missingPSSnapIns = new ReadOnlyCollection<string>(missingItems);
            this.SetErrorId(errorId);
            this.SetTargetObject(commandName);
            this.SetErrorCategory(ErrorCategory.ResourceUnavailable);
        }

        /// <summary>
        /// Constructs an ScriptRequiresException. Recommended constructor for the class for
        /// #requires -RunAsAdministrator statement.
        /// </summary>
        /// <param name="commandName">
        /// The name of the script containing the #requires statement.
        /// </param>
        /// <param name="errorId">
        /// The error id for this exception.
        /// </param>
        internal ScriptRequiresException(
            string commandName,
            string errorId)
            : base(BuildMessage(commandName))
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(commandName), "commandName is null or empty when constructing ScriptRequiresException");
            Diagnostics.Assert(!string.IsNullOrEmpty(errorId), "errorId is null or empty when constructing ScriptRequiresException");
            _commandName = commandName;
            this.SetErrorId(errorId);
            this.SetTargetObject(commandName);
            this.SetErrorCategory(ErrorCategory.PermissionDenied);
        }

        /// <summary>
        /// Constructs an PSVersionNotCompatibleException.
        /// </summary>
        public ScriptRequiresException() : base() { }

        /// <summary>
        /// Constructs an PSVersionNotCompatibleException.
        /// </summary>
        /// <param name="message">
        /// The message used in the exception.
        /// </param>
        public ScriptRequiresException(string message) : base(message) { }

        /// <summary>
        /// Constructs an PSVersionNotCompatibleException.
        /// </summary>
        /// <param name="message">
        /// The message used in the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that led to this exception.
        /// </param>
        public ScriptRequiresException(string message, Exception innerException) : base(message, innerException) { }

        #region Serialization
        /// <summary>
        /// Constructs an PSVersionNotCompatibleException using serialized data.
        /// </summary>
        /// <param name="info">
        /// serialization information
        /// </param>
        /// <param name="context">
        /// streaming context
        /// </param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ScriptRequiresException(SerializationInfo info,
                                        StreamingContext context)
        {
            throw new NotSupportedException();
        }
        
        #endregion Serialization

        #region Properties

        /// <summary>
        /// Gets the name of the script that contained the #requires statement.
        /// </summary>
        public string CommandName
        {
            get { return _commandName; }
        }

        private readonly string _commandName = string.Empty;

        /// <summary>
        /// Gets the PSVersion that the script requires.
        /// </summary>
        public Version RequiresPSVersion
        {
            get { return _requiresPSVersion; }
        }

        private readonly Version _requiresPSVersion;

        /// <summary>
        /// Gets the missing snap-ins that the script requires.
        /// </summary>
        public ReadOnlyCollection<string> MissingPSSnapIns
        {
            get { return _missingPSSnapIns; }
        }

        private readonly ReadOnlyCollection<string> _missingPSSnapIns = new ReadOnlyCollection<string>(Array.Empty<string>());

        /// <summary>
        /// Gets or sets the ID of the shell.
        /// </summary>
        public string RequiresShellId
        {
            get { return _requiresShellId; }
        }

        private readonly string _requiresShellId;

        /// <summary>
        /// Gets or sets the path to the incompatible shell.
        /// </summary>
        public string RequiresShellPath
        {
            get { return _requiresShellPath; }
        }

        private readonly string _requiresShellPath;

        #endregion Properties

        #region Private

        private static string BuildMessage(
            string commandName,
            Collection<string> missingItems,
            bool forSnapins)
        {
            StringBuilder sb = new StringBuilder();
            if (missingItems == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(missingItems));
            }

            foreach (string missingItem in missingItems)
            {
                sb.Append(missingItem).Append(", ");
            }

            if (sb.Length > 1)
            {
                sb.Remove(sb.Length - 2, 2);
            }

            if (forSnapins)
            {
                return StringUtil.Format(
                    DiscoveryExceptions.RequiresMissingPSSnapIns,
                    commandName,
                    sb.ToString());
            }
            else
            {
                return StringUtil.Format(
                    DiscoveryExceptions.RequiresMissingModules,
                    commandName,
                    sb.ToString());
            }
        }

        private static string BuildMessage(
            string commandName,
            string first,
            string second,
            bool forShellId)
        {
            string resourceStr = null;

            if (forShellId)
            {
                if (string.IsNullOrEmpty(first))
                {
                    resourceStr = DiscoveryExceptions.RequiresShellIDInvalidForSingleShell;
                }
                else
                {
                    resourceStr = string.IsNullOrEmpty(second)
                            ? DiscoveryExceptions.RequiresInterpreterNotCompatibleNoPath
                            : DiscoveryExceptions.RequiresInterpreterNotCompatible;
                }
            }
            else
            {
                resourceStr = DiscoveryExceptions.RequiresPSVersionNotCompatible;
            }

            return StringUtil.Format(resourceStr, commandName, first, second);
        }

        private static string BuildMessage(string commandName)
        {
            return StringUtil.Format(DiscoveryExceptions.RequiresElevation, commandName);
        }

        #endregion Private
    }
}
