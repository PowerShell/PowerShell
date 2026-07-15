// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56506

using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Resources;
using System.Runtime.Serialization;
using System.Reflection;
using System.Management.Automation.Language;
using System.Security.Permissions;

namespace System.Management.Automation
{
    /// <summary>
    /// Errors reported by PowerShell will be in one of these categories.
    /// </summary>
    /// <remarks>
    /// Do not specify ErrorCategory.NotSpecified when creating an
    /// <see cref="System.Management.Automation.ErrorRecord"/>.
    /// Choose the best match from among the other values.
    /// </remarks>
    public enum ErrorCategory
    {
        /// <summary>
        /// <para>
        /// No error category is specified, or the error category is invalid.
        /// </para>
        /// <para>
        /// Do not specify ErrorCategory.NotSpecified when creating an
        /// <see cref="System.Management.Automation.ErrorRecord"/>.
        /// Choose the best match from among the other values.
        /// </para>
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// </summary>
        OpenError = 1,

        /// <summary>
        /// </summary>
        CloseError = 2,

        /// <summary>
        /// </summary>
        DeviceError = 3,

        /// <summary>
        /// </summary>
        DeadlockDetected = 4,

        /// <summary>
        /// </summary>
        InvalidArgument = 5,

        /// <summary>
        /// </summary>
        InvalidData = 6,

        /// <summary>
        /// </summary>
        InvalidOperation = 7,

        /// <summary>
        /// </summary>
        InvalidResult = 8,

        /// <summary>
        /// </summary>
        InvalidType = 9,

        /// <summary>
        /// </summary>
        MetadataError = 10,

        /// <summary>
        /// </summary>
        NotImplemented = 11,

        /// <summary>
        /// </summary>
        NotInstalled = 12,

        /// <summary>
        /// Object can not be found (file, directory, computer, system resource, etc.)
        /// </summary>
        ObjectNotFound = 13,

        /// <summary>
        /// </summary>
        OperationStopped = 14,

        /// <summary>
        /// </summary>
        OperationTimeout = 15,

        /// <summary>
        /// </summary>
        SyntaxError = 16,

        /// <summary>
        /// </summary>
        ParserError = 17,

        /// <summary>
        /// Operation not permitted.
        /// </summary>
        PermissionDenied = 18,

        /// <summary>
        /// </summary>
        ResourceBusy = 19,

        /// <summary>
        /// </summary>
        ResourceExists = 20,

        /// <summary>
        /// </summary>
        ResourceUnavailable = 21,

        /// <summary>
        /// </summary>
        ReadError = 22,

        /// <summary>
        /// </summary>
        WriteError = 23,

        /// <summary>
        /// <para>
        /// A native command reported an error to its STDERR pipe.
        /// </para>
        /// <para>
        /// The Engine uses this ErrorCategory when it executes a native
        /// console applications and captures the errors reported by the
        /// native application.  Avoid using ErrorCategory.FromStdErr
        /// in other circumstances.
        /// </para>
        /// </summary>
        FromStdErr = 24,

        /// <summary>
        /// Used for security exceptions.
        /// </summary>
        SecurityError = 25,

        /// <summary>
        /// The contract of a protocol is not being followed. Should not happen
        /// with well-behaved components.
        /// </summary>
        ProtocolError = 26,

        /// <summary>
        /// The operation depends on a network connection that cannot be
        /// established or maintained.
        /// </summary>
        ConnectionError = 27,

        /// <summary>
        /// Could not authenticate the user to the service. Could mean that the
        /// credentials are invalid or the authentication system is not
        /// functioning properly.
        /// </summary>
        AuthenticationError = 28,

        /// <summary>
        /// Internal limits prevent the operation from being executed.
        /// </summary>
        LimitsExceeded = 29,

        /// <summary>
        /// Controls on the use of traffic or resources prevent the operation
        /// from being executed.
        /// </summary>
        QuotaExceeded = 30,

        /// <summary>
        /// The operation attempted to use functionality that is currently
        /// disabled.
        /// </summary>
        NotEnabled = 31,
    }

    /// <summary>
    /// Contains auxiliary information about an
    /// <see cref="System.Management.Automation.ErrorRecord"/>
    /// </summary>
    public class ErrorCategoryInfo
    {
        #region ctor
        internal ErrorCategoryInfo(ErrorRecord errorRecord)
        {
            ArgumentNullException.ThrowIfNull(errorRecord);

            _errorRecord = errorRecord;
        }
        #endregion ctor

        #region Properties
        /// <summary></summary>
        /// <see cref="System.Management.Automation.ErrorCategory"/>
        /// for this error
        public ErrorCategory Category
        {
            get { return _errorRecord._category; }
        }

        /// <summary>
        /// Text description of the operation which
        /// encountered the error.
        /// </summary>
        /// <value>text description of the operation</value>
        /// <remarks>
        /// By default, this is the cmdlet name.
        /// The default can be overridden by calling Set with a
        /// non-empty value, for example "Delete".
        /// </remarks>
        public string Activity
        {
            get
            {
                if (!string.IsNullOrEmpty(_errorRecord._activityOverride))
                {
                    return _errorRecord._activityOverride;
                }

                if (_errorRecord.InvocationInfo != null
                    && (_errorRecord.InvocationInfo.MyCommand is CmdletInfo || _errorRecord.InvocationInfo.MyCommand is IScriptCommandInfo)
                    && !string.IsNullOrEmpty(_errorRecord.InvocationInfo.MyCommand.Name)
                    )
                {
                    return _errorRecord.InvocationInfo.MyCommand.Name;
                }

                return string.Empty;
            }

            set
            {
                _errorRecord._activityOverride = value;
            }
        }

        /// <summary>
        /// Text description of the error.
        /// </summary>
        /// <value>text description of the error</value>
        /// <remarks>
        /// By default, this is the exception type.
        /// The default can be overridden by calling Set with a
        /// non-empty value, for example "Permission Denied".
        /// </remarks>
        public string Reason
        {
            get
            {
                _reasonIsExceptionType = false;
                if (!string.IsNullOrEmpty(_errorRecord._reasonOverride))
                {
                    return _errorRecord._reasonOverride;
                }

                if (_errorRecord.Exception != null)
                {
                    _reasonIsExceptionType = true;
                    return _errorRecord.Exception.GetType().Name;
                }

                return string.Empty;
            }

            set
            {
                _errorRecord._reasonOverride = value;
            }
        }

        private bool _reasonIsExceptionType;

        /// <summary>
        /// Text description of the target object.
        /// </summary>
        /// <value>text description of the target object</value>
        /// <remarks>
        /// By default, this is TargetObject.ToString(), or the empty string
        /// if the target object is null.
        /// The default can be overridden by calling Set with a
        /// non-empty value, for example "John Doe".
        /// </remarks>
        public string TargetName
        {
            get
            {
                if (!string.IsNullOrEmpty(_errorRecord._targetNameOverride))
                {
                    return _errorRecord._targetNameOverride;
                }

                if (_errorRecord.TargetObject != null)
                {
                    string targetInString;
                    try
                    {
                        targetInString = _errorRecord.TargetObject.ToString();
                    }
                    catch (Exception)
                    {
                        targetInString = null;
                    }

                    return ErrorRecord.NotNull(targetInString);
                }

                return string.Empty;
            }

            set
            {
                _errorRecord._targetNameOverride = value;
            }
        }

        /// <summary>
        /// Text description of the type of the target object.
        /// </summary>
        /// <value>text description of the type of the target object</value>
        /// <remarks>
        /// By default, this is TargetObject.GetType().ToString(),
        /// or the empty string if the target object is null.
        /// The default can be overridden by calling Set with a
        /// non-empty value, for example "Active Directory User".
        /// </remarks>
        public string TargetType
        {
            get
            {
                if (!string.IsNullOrEmpty(_errorRecord._targetTypeOverride))
                {
                    return _errorRecord._targetTypeOverride;
                }

                if (_errorRecord.TargetObject != null)
                {
                    return _errorRecord.TargetObject.GetType().Name;
                }

                return string.Empty;
            }

            set
            {
                _errorRecord._targetTypeOverride = value;
            }
        }

        #endregion Properties

        #region Methods
        /// <summary>
        /// Concise text description based on
        /// <see cref="System.Management.Automation.ErrorCategoryInfo.Category"/>
        /// </summary>
        /// <returns>Concise text description.</returns>
        /// <remarks>
        /// GetMessage returns a concise string which categorizes the error,
        /// based on
        /// <see cref="System.Management.Automation.ErrorCategoryInfo.Category"/>
        /// and including the other fields of
        /// <see cref="System.Management.Automation.ErrorCategoryInfo"/>
        /// as appropriate.  This string is much shorter
        /// than
        /// <see cref="System.Management.Automation.ErrorDetails.Message"/> or
        /// <see cref="System.Exception.Message"/>, since it only
        /// categorizes the error and does not contain a full description
        /// or recommended actions.  The default host will display this
        /// string instead of the full message if shell variable
        /// $ErrorView is set to "CategoryView".
        /// </remarks>
        public string GetMessage()
        {
            /* Remoting not in E12
            if (!string.IsNullOrEmpty (_errorRecord._serializedErrorCategoryMessageOverride))
                return _errorRecord._serializedErrorCategoryMessageOverride;
            */

            return GetMessage(CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// Concise text description based on
        /// <see cref="System.Management.Automation.ErrorCategoryInfo.Category"/>
        /// </summary>
        /// <param name="uiCultureInfo">Culture in which to display message.</param>
        /// <returns>Concise text description.</returns>
        /// <remarks>
        /// GetMessage returns a concise string which categorizes the error,
        /// based on
        /// <see cref="System.Management.Automation.ErrorCategoryInfo.Category"/>
        /// and including the other fields of
        /// <see cref="System.Management.Automation.ErrorCategoryInfo"/>
        /// as appropriate.  This string is much shorter
        /// than
        /// <see cref="System.Management.Automation.ErrorDetails.Message"/> or
        /// <see cref="System.Exception.Message"/>, since it only
        /// categorizes the error and does not contain a full description
        /// or recommended actions.  The default host will display this
        /// string instead of the full message if shell variable
        /// $ErrorView is set to "CategoryView".
        /// </remarks>
        public string GetMessage(CultureInfo uiCultureInfo)
        {
            // get template text
            string errorCategoryString = Category.ToString();
            if (string.IsNullOrEmpty(errorCategoryString))
            {
                // this probably indicates an invalid ErrorCategory value
                errorCategoryString = nameof(ErrorCategory.NotSpecified);
            }

            string templateText = ErrorCategoryStrings.ResourceManager.GetString(errorCategoryString, uiCultureInfo);

            if (string.IsNullOrEmpty(templateText))
            {
                // this probably indicates an invalid ErrorCategory value
                templateText = ErrorCategoryStrings.NotSpecified;
            }

            Diagnostics.Assert(!string.IsNullOrEmpty(templateText),
                "ErrorCategoryStrings.resx resource failure");

            string activityInUse = Ellipsize(uiCultureInfo, Activity);
            string targetNameInUse = Ellipsize(uiCultureInfo, TargetName);
            string targetTypeInUse = Ellipsize(uiCultureInfo, TargetType);
            // if the reason is a exception type name, we should output the whole name
            string reasonInUse = Reason;
            reasonInUse = _reasonIsExceptionType ? reasonInUse : Ellipsize(uiCultureInfo, reasonInUse);

            // assemble final string
            try
            {
                return string.Format(uiCultureInfo, templateText,
                    activityInUse,
                    targetNameInUse,
                    targetTypeInUse,
                    reasonInUse,
                    errorCategoryString);
            }
            catch (FormatException)
            {
                templateText = ErrorCategoryStrings.InvalidErrorCategory;

                return string.Format(uiCultureInfo, templateText,
                    activityInUse,
                    targetNameInUse,
                    targetTypeInUse,
                    reasonInUse,
                    errorCategoryString);
            }
        }

        /// <summary>
        /// Same as
        /// <see cref="System.Management.Automation.ErrorCategoryInfo.GetMessage()"/>
        /// </summary>
        /// <returns>Developer-readable identifier.</returns>
        public override string ToString()
        {
            return GetMessage(CultureInfo.CurrentUICulture);
        }
        #endregion Methods

        #region Private
        // back-reference for facade class
        private readonly ErrorRecord _errorRecord;

        /// <summary>
        /// The Activity, Reason, TargetName and TargetType strings in
        /// ErrorCategoryInfo can be of unlimited length.  In order to
        /// control the maximum length of the GetMessage() string, we
        /// ellipsize these strings.  The current heuristic is to take
        /// strings longer than 40 characters and ellipsize them to
        /// the first and last 19 characters plus "..." in the middle.
        /// </summary>
        /// <param name="uiCultureInfo">Culture to retrieve template if needed.</param>
        /// <param name="original">Original string.</param>
        /// <returns>Ellipsized version of string.</returns>
        /// <remarks>
        /// "Please do not make this public as ellipsize is not a word."
        /// </remarks>
        internal static string Ellipsize(CultureInfo uiCultureInfo, string original)
        {
            if (original.Length <= 40)
            {
                return original;
            }

            // We are splitting a string > 40 chars in half, so left and right can be
            // at most 19 characters to include the ellipsis in the middle.
            const int MaxHalfWidth = 19;
            string first = original.Substring(0, MaxHalfWidth);
            string last = original.Substring(original.Length - MaxHalfWidth, MaxHalfWidth);
            return
                string.Format(uiCultureInfo, ErrorPackage.Ellipsize, first, last);
        }
        #endregion Private
    }

    /// <summary>
    /// Additional details about an
    /// <see cref="System.Management.Automation.ErrorRecord"/>
    /// </summary>
    /// <remarks>
    /// ErrorDetails represents additional details about an
    /// <see cref="System.Management.Automation.ErrorRecord"/>,
    /// starting with a replacement Message.  Clients can use ErrorDetails
    /// when they want to display a more specific Message than the one
    /// contained in a particular Exception, without having to create
    /// a new Exception or define a new Exception class.
    ///
    /// It is permitted to subclass <see cref="ErrorDetails"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class ErrorDetails : ISerializable
    {
        #region Constructor
        /// <summary>
        /// Creates an instance of ErrorDetails specifying a Message.
        /// </summary>
        /// <remarks>
        /// It is preferred for Cmdlets to use
        /// <see cref="ErrorDetails(Cmdlet,string,string,object[])"/>,
        /// for CmdletProviders to use
        /// <see cref="ErrorDetails(IResourceSupplier,string,string,object[])"/>,
        /// and for other localizable code to use
        /// <see cref="ErrorDetails(Assembly,string,string,object[])"/>
        /// where possible.
        /// </remarks>
        /// <param name="message"></param>
        public ErrorDetails(string message)
        {
            _message = message;
        }

        #region UseResourceId
        /// <summary>
        /// Creates an instance of ErrorDetails specifying a Message.
        /// This variant is used by cmdlets.
        /// </summary>
        /// <param name="cmdlet">Cmdlet containing the template string.</param>
        /// <param name="baseName">by default, the
        /// <see cref="System.Resources.ResourceManager"/>
        /// name</param>
        /// <param name="resourceId">
        /// by default, the resourceId in the
        /// <see cref="System.Resources.ResourceManager"/>
        /// </param>
        /// <param name="args">
        /// <see cref="string.Format(IFormatProvider,string,object[])"/>
        /// insertion parameters
        /// </param>
        /// <remarks>
        /// This variant is a shortcut to build an instance of
        /// <see cref="System.Management.Automation.ErrorDetails"/>
        /// reducing the steps which localizable code generally has to duplicate when it
        /// generates a localizable string.  This variant is preferred over
        /// <see cref="System.Management.Automation.ErrorDetails(string)"/>,
        /// since the improved
        /// information about the error may help enable future scenarios.
        ///
        /// This constructor first loads the error message template string using
        /// <see cref="Cmdlet.GetResourceString"/>.
        /// The default implementation of
        /// <see cref="Cmdlet.GetResourceString"/>
        /// will load a string resource from the cmdlet assembly using
        /// <paramref name="baseName"/> and <paramref name="resourceId"/>;
        /// however, specific cmdlets can override this behavior
        /// by overriding virtual method
        /// <see cref="Cmdlet.GetResourceString"/>.
        /// This constructor then inserts the specified args using
        /// <see cref="string.Format(IFormatProvider,string,object[])"/>.
        /// </remarks>
        public ErrorDetails(
            Cmdlet cmdlet,
            string baseName,
            string resourceId,
            params object[] args)
        {
            _message = BuildMessage(cmdlet, baseName, resourceId, args);
        }
        /// <summary>
        /// Creates an instance of ErrorDetails specifying a Message.
        /// This variant is used by CmdletProviders.
        /// </summary>
        /// <param name="resourceSupplier">
        /// Resource supplier, most often an instance of
        /// <see cref="Provider.CmdletProvider"/>.
        /// </param>
        /// <param name="baseName">by default, the
        /// <see cref="System.Resources.ResourceManager"/>
        /// name</param>
        /// <param name="resourceId">
        /// by default, the resourceId in the
        /// <see cref="System.Resources.ResourceManager"/>
        /// </param>
        /// <param name="args">
        /// <see cref="string.Format(IFormatProvider,string,object[])"/>
        /// insertion parameters
        /// </param>
        /// <remarks>
        /// This variant is a shortcut to build an instance of
        /// <see cref="System.Management.Automation.ErrorDetails"/>
        /// reducing the steps which localizable code generally has to duplicate when it
        /// generates a localizable string.  This variant is preferred over
        /// <see cref="System.Management.Automation.ErrorDetails(string)"/>,
        /// since the improved
        /// information about the error may help enable future scenarios.
        ///
        /// This constructor first loads a template string using
        /// <see cref="System.Management.Automation.IResourceSupplier.GetResourceString"/>.
        /// The default implementation of
        /// <see cref="Provider.CmdletProvider.GetResourceString"/>
        /// will load a string resource from the CmdletProvider assembly using
        /// <paramref name="baseName"/> and <paramref name="resourceId"/>;
        /// however, specific CmdletProviders can override this behavior
        /// by overriding virtual method
        /// <see cref="Provider.CmdletProvider.GetResourceString"/>,
        /// and it is also possible that PSSnapin custom classes
        /// which are not instances of
        /// <see cref="Provider.CmdletProvider"/>
        /// will implement
        /// <see cref="IResourceSupplier"/>.
        /// The constructor then inserts the specified args using
        /// <see cref="string.Format(IFormatProvider,string,object[])"/>.
        /// </remarks>
        public ErrorDetails(
            IResourceSupplier resourceSupplier,
            string baseName,
            string resourceId,
            params object[] args)
        {
            _message = BuildMessage(resourceSupplier, baseName, resourceId, args);
        }
        /// <summary>
        /// Creates an instance of ErrorDetails specifying a Message.
        /// This variant is used by other code without a reference to
        /// a <see cref="Cmdlet"/> or <see cref="Provider.CmdletProvider"/> instance.
        /// </summary>
        /// <param name="assembly">
        /// assembly containing the template string
        /// </param>
        /// <param name="baseName">by default, the
        /// <see cref="System.Resources.ResourceManager"/>
        /// name</param>
        /// <param name="resourceId">
        /// by default, the resourceId in the
        /// <see cref="System.Resources.ResourceManager"/>
        /// </param>
        /// <param name="args">
        /// <see cref="string.Format(IFormatProvider,string,object[])"/>
        /// insertion parameters
        /// </param>
        /// <remarks>
        /// This variant is a shortcut to build an instance of
        /// <see cref="System.Management.Automation.ErrorDetails"/>
        /// reducing the steps which localizable code generally has to duplicate when it
        /// generates a localizable string.  This variant is preferred over
        /// <see cref="System.Management.Automation.ErrorDetails(string)"/>,
        /// since the improved
        /// information about the error may help enable future scenarios.
        ///
        /// This constructor first loads a template string from the assembly using
        /// <see cref="System.Resources.ResourceManager.GetString(string)"/>.
        /// The constructor then inserts the specified args using
        /// <see cref="string.Format(IFormatProvider,string,object[])"/>.
        /// </remarks>
        public ErrorDetails(
            System.Reflection.Assembly assembly,
            string baseName,
            string resourceId,
            params object[] args)
        {
            _message = BuildMessage(assembly, baseName, resourceId, args);
        }
        #endregion UseResourceId

        // deep-copy constructor
        internal ErrorDetails(ErrorDetails errorDetails)
        {
            _message = errorDetails._message;
            _recommendedAction = errorDetails._recommendedAction;
        }
        #endregion Constructor

        #region Serialization
        /// <summary>
        /// Initializes a new instance of the ErrorDetails class
        /// using data serialized via
        /// <see cref="ISerializable"/>
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        /// <returns>Constructed object.</returns>
        protected ErrorDetails(SerializationInfo info,
                               StreamingContext context)
        {
            _message = info.GetString("ErrorDetails_Message");
            _recommendedAction = info.GetString(
                "ErrorDetails_RecommendedAction");
        }

        /// <summary>
        /// Serializer for <see cref="ISerializable"/>
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info != null)
            {
                info.AddValue("ErrorDetails_Message", _message);
                info.AddValue("ErrorDetails_RecommendedAction",
                    _recommendedAction);
            }
        }
        #endregion Serialization

        #region Public Properties
        /// <summary>
        /// Message which replaces
        /// <see cref="System.Exception.Message"/> in
        /// <see cref="System.Management.Automation.ErrorRecord.Exception"/>
        /// </summary>
        /// <remarks>
        /// When an instance of
        /// <see cref="System.Management.Automation.ErrorRecord"/>
        /// contains a non-null
        /// <see cref="System.Management.Automation.ErrorRecord.ErrorDetails"/>
        /// and
        /// <see cref="System.Management.Automation.ErrorDetails.Message"/>
        /// is non-empty, the default host will display it instead of
        /// the <see cref="System.Exception.Message"/> in
        /// <see cref="System.Management.Automation.ErrorRecord.Exception"/>.
        ///
        /// This should be a grammatically correct localized text string, as with
        /// <see cref="System.Exception.Message"/>
        /// </remarks>
        public string Message
        {
            get { return ErrorRecord.NotNull(_message); }
        }

        private readonly string _message = string.Empty;

        /// <summary>
        /// Text describing the recommended action in the event that this error
        /// occurs.  This is empty unless the code which generates the error
        /// specifies it explicitly.
        /// </summary>
        /// <remarks>
        /// This should be a grammatically correct localized text string.
        /// This may be left empty.
        /// </remarks>
        public string RecommendedAction
        {
            get
            {
                return ErrorRecord.NotNull(_recommendedAction);
            }

            set
            {
                _recommendedAction = value;
            }
        }

        private string _recommendedAction = string.Empty;
        #endregion Public Properties

        #region Internal Properties
        internal Exception TextLookupError
        {
            get { return _textLookupError; }

            set { _textLookupError = value; }
        }

        private Exception _textLookupError /* = null */;
        #endregion Internal Properties

        #region ToString
        /// <summary>
        /// As <see cref="object.ToString()"/>
        /// </summary>
        /// <returns>Developer-readable identifier.</returns>
        public override string ToString()
        {
            return Message;
        }
        #endregion ToString

        #region Private
        private string BuildMessage(
            Cmdlet cmdlet,
            string baseName,
            string resourceId,
            params object[] args)
        {
            if (cmdlet == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cmdlet));
            }

            if (string.IsNullOrEmpty(baseName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(baseName));
            }

            if (string.IsNullOrEmpty(resourceId))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(resourceId));
            }

            string template = string.Empty;

            try
            {
                template = cmdlet.GetResourceString(baseName, resourceId);
            }
            catch (MissingManifestResourceException e)
            {
                _textLookupError = e;
                return string.Empty; // fallback to Exception.Message
            }
            catch (ArgumentException e)
            {
                _textLookupError = e;
                return string.Empty; // fallback to Exception.Message
            }

            return BuildMessage(template, baseName, resourceId, args);
        }

        private string BuildMessage(
            IResourceSupplier resourceSupplier,
            string baseName,
            string resourceId,
            params object[] args)
        {
            if (resourceSupplier == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(resourceSupplier));
            }

            if (string.IsNullOrEmpty(baseName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(baseName));
            }

            if (string.IsNullOrEmpty(resourceId))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(resourceId));
            }

            string template = string.Empty;

            try
            {
                template = resourceSupplier.GetResourceString(baseName, resourceId);
            }
            catch (MissingManifestResourceException e)
            {
                _textLookupError = e;
                return string.Empty; // fallback to Exception.Message
            }
            catch (ArgumentException e)
            {
                _textLookupError = e;
                return string.Empty; // fallback to Exception.Message
            }

            return BuildMessage(template, baseName, resourceId, args);
        }

        private string BuildMessage(
            System.Reflection.Assembly assembly,
            string baseName,
            string resourceId,
            params object[] args)
        {
            if (assembly == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(assembly));
            }

            if (string.IsNullOrEmpty(baseName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(baseName));
            }

            if (string.IsNullOrEmpty(resourceId))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(resourceId));
            }

            string template = string.Empty;

            ResourceManager manager =
                ResourceManagerCache.GetResourceManager(
                        assembly, baseName);
            try
            {
                template = manager.GetString(
                    resourceId,
                    CultureInfo.CurrentUICulture);
            }
            catch (MissingManifestResourceException e)
            {
                _textLookupError = e;
                return string.Empty; // fallback to Exception.Message
            }

            return BuildMessage(template, baseName, resourceId, args);
        }

        private string BuildMessage(
            string template,
            string baseName,
            string resourceId,
            params object[] args)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                _textLookupError = PSTraceSource.NewInvalidOperationException(
                    ErrorPackage.ErrorDetailsEmptyTemplate,
                    baseName,
                    resourceId);
                return string.Empty; // fallback to Exception.Message
            }

            try
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    template,
                    args);
            }
            catch (FormatException e)
            {
                _textLookupError = e;
                return string.Empty; // fallback to Exception.Message
            }
        }
        #endregion Private

    }

    /// <summary>
    /// Represents an error.
    /// </summary>
    /// <remarks>
    /// An ErrorRecord describes an error.  It extends the usual information
    /// in <see cref="System.Exception"/> with the additional information in
    /// <see cref="System.Management.Automation.ErrorRecord.ErrorDetails"/>,
    /// <see cref="System.Management.Automation.ErrorRecord.TargetObject"/>,
    /// <see cref="System.Management.Automation.ErrorRecord.CategoryInfo"/>,
    /// <see cref="System.Management.Automation.ErrorRecord.FullyQualifiedErrorId"/>,
    /// <see cref="System.Management.Automation.ErrorRecord.ErrorDetails"/>, and
    /// <see cref="System.Management.Automation.ErrorRecord.InvocationInfo"/>.
    /// Non-terminating errors are stored as
    /// <see cref="System.Management.Automation.ErrorRecord"/>
    /// instances in shell variable
    /// $error.
    ///
    /// Some terminating errors implement
    /// <see cref="System.Management.Automation.IContainsErrorRecord"/>
    /// which gives them an ErrorRecord property containing this additional
    /// information.  In this case, ErrorRecord.Exception will be an instance of
    /// <see cref="System.Management.Automation.ParentContainsErrorRecordException"/>.
    /// rather than the actual exception, to avoid the mutual references.
    /// </remarks>
    public class ErrorRecord : ISerializable
    {
        #region Constructor

        private ErrorRecord()
        {
        }

        /// <summary>
        /// Creates an instance of ErrorRecord.
        /// </summary>
        /// <param name="exception">
        /// This is an exception which describes the error.
        /// This argument may not be null, but it is not required
        /// that the exception have ever been thrown.
        /// </param>
        /// <param name="errorId">
        /// This string will be used to construct the FullyQualifiedErrorId,
        /// which is a global identifier of the error condition.  Pass a
        /// non-empty string which is specific to this error condition in
        /// this context.
        /// </param>
        /// <param name="errorCategory">
        /// This is the ErrorCategory which best describes the error.
        /// </param>
        /// <param name="targetObject">
        /// This is the object against which the cmdlet or provider
        /// was operating when the error occurred.  This is optional.
        /// </param>
        public ErrorRecord(
            Exception exception,
            string errorId,
            ErrorCategory errorCategory,
            object targetObject)
        {
            if (exception == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(exception));
            }

            errorId ??= string.Empty;

            // targetObject may be null
            _error = exception;
            _errorId = errorId;
            _category = errorCategory;
            _target = targetObject;
        }

        #region Serialization

        // We serialize the exception as its original type, ensuring
        // that the ErrorRecord information arrives in full, but taking
        // the risk that it cannot be serialized/deserialized at all if
        // (1) the exception type does not exist on the target machine, or
        // (2) the exception serializer/deserializer fails or is not
        // implemented/supported.
        //
        // We do not attempt to serialize TargetObject.
        //
        // We do not attempt to serialize InvocationInfo.  There is
        // potentially some useful information there, but serializing
        // InvocationInfo, Token, InternalCommand and its subclasses, and
        // CommandInfo and its subclasses is too expensive.

        /// <summary>
        /// Initializes a new instance of the ErrorRecord class
        /// using data serialized via
        /// <see cref="ISerializable"/>
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        /// <returns>Constructed object.</returns>
        /// <remarks>
        /// ErrorRecord instances which are serialized using
        /// <see cref="ISerializable"/>
        /// will only be partially reconstructed.
        /// </remarks>
        protected ErrorRecord(SerializationInfo info,
                              StreamingContext context)
        {
            PSObject psObject = PSObject.ConstructPSObjectFromSerializationInfo(info, context);
            ConstructFromPSObjectForRemoting(psObject);
        }

        /// <summary>
        /// Deserializer for <see cref="ISerializable"/>
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info != null)
            {
                PSObject psObject = RemotingEncoder.CreateEmptyPSObject();

                // for binary serialization always serialize the extended info
                ToPSObjectForRemoting(psObject, true);

                psObject.GetObjectData(info, context);
            }
        }
        #endregion Serialization

        #region Remoting

        /// <summary>
        /// IsSerialized is set to true if this error record is serialized.
        /// </summary>
        private bool _isSerialized = false;

        /// <summary>
        /// Is this instance serialized.
        /// </summary>
        internal bool IsSerialized { get => _isSerialized; }

        /// <summary>
        /// Value for FullyQualifiedErrorId in case of serialized error record.
        /// </summary>
        private string _serializedFullyQualifiedErrorId = null;

        /// <summary>
        /// Message overridee for CategoryInfo.GetMessage method.
        /// </summary>
        internal string _serializedErrorCategoryMessageOverride = null;

        /// <summary>
        /// This constructor is used by remoting code to create ErrorRecord.
        /// Various information is obtained from serialized ErrorRecord.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="targetObject"></param>
        /// <param name="fullyQualifiedErrorId"></param>
        /// <param name="errorCategory"></param>
        /// <param name="errorCategory_Activity"></param>
        /// <param name="errorCategory_Reason"></param>
        /// <param name="errorCategory_TargetName"></param>
        /// <param name="errorCategory_TargetType"></param>
        /// <param name="errorCategory_Message"></param>
        /// <param name="errorDetails_Message"></param>
        /// <param name="errorDetails_RecommendedAction"></param>
        internal ErrorRecord(
            Exception exception,
            object targetObject,
            string fullyQualifiedErrorId,
            ErrorCategory errorCategory,
            string errorCategory_Activity,
            string errorCategory_Reason,
            string errorCategory_TargetName,
            string errorCategory_TargetType,
            string errorCategory_Message,
            string errorDetails_Message,
            string errorDetails_RecommendedAction)
        {
            PopulateProperties(
                exception, targetObject, fullyQualifiedErrorId, errorCategory, errorCategory_Activity,
                errorCategory_Reason, errorCategory_TargetName, errorCategory_TargetType,
                errorCategory_Message, errorDetails_Message, errorDetails_RecommendedAction, null);
        }

        private void PopulateProperties(
            Exception exception,
            object targetObject,
            string fullyQualifiedErrorId,
            ErrorCategory errorCategory,
            string errorCategory_Activity,
            string errorCategory_Reason,
            string errorCategory_TargetName,
            string errorCategory_TargetType,
            string errorCategory_Message,
            string errorDetails_Message,
            string errorDetails_RecommendedAction,
            string errorDetails_ScriptStackTrace)
        {
            if (exception == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(exception));
            }

            if (fullyQualifiedErrorId == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(fullyQualifiedErrorId));
            }

            // Mark this error record as serialized
            _isSerialized = true;
            _error = exception;
            _target = targetObject;
            _serializedFullyQualifiedErrorId = fullyQualifiedErrorId;
            _category = errorCategory;
            _activityOverride = errorCategory_Activity;
            _reasonOverride = errorCategory_Reason;
            _targetNameOverride = errorCategory_TargetName;
            _targetTypeOverride = errorCategory_TargetType;
            _serializedErrorCategoryMessageOverride = errorCategory_Message;
            if (errorDetails_Message != null)
            {
                ErrorDetails = new ErrorDetails(errorDetails_Message);
                if (errorDetails_RecommendedAction != null)
                {
                    ErrorDetails.RecommendedAction = errorDetails_RecommendedAction;
                }
            }

            _scriptStackTrace = errorDetails_ScriptStackTrace;
        }

        /// <summary>
        /// Adds the information about this error record to PSObject as notes.
        /// </summary>
        /// <returns></returns>
        internal void ToPSObjectForRemoting(PSObject dest)
        {
            ToPSObjectForRemoting(dest, SerializeExtendedInfo);
        }

        private void ToPSObjectForRemoting(PSObject dest, bool serializeExtInfo)
        {
            RemotingEncoder.AddNoteProperty<Exception>(dest, "Exception", () => Exception);
            RemotingEncoder.AddNoteProperty<object>(dest, "TargetObject", () => TargetObject);
            RemotingEncoder.AddNoteProperty<string>(dest, "FullyQualifiedErrorId", () => FullyQualifiedErrorId);
            RemotingEncoder.AddNoteProperty<InvocationInfo>(dest, "InvocationInfo", () => InvocationInfo);
            RemotingEncoder.AddNoteProperty<int>(dest, "ErrorCategory_Category", () => (int)CategoryInfo.Category);
            RemotingEncoder.AddNoteProperty<string>(dest, "ErrorCategory_Activity", () => CategoryInfo.Activity);
            RemotingEncoder.AddNoteProperty<string>(dest, "ErrorCategory_Reason", () => CategoryInfo.Reason);
            RemotingEncoder.AddNoteProperty<string>(dest, "ErrorCategory_TargetName", () => CategoryInfo.TargetName);
            RemotingEncoder.AddNoteProperty<string>(dest, "ErrorCategory_TargetType", () => CategoryInfo.TargetType);
            RemotingEncoder.AddNoteProperty<string>(dest, "ErrorCategory_Message", () => CategoryInfo.GetMessage(CultureInfo.CurrentCulture));

            if (ErrorDetails != null)
            {
                RemotingEncoder.AddNoteProperty<string>(dest, "ErrorDetails_Message", () => ErrorDetails.Message);
                RemotingEncoder.AddNoteProperty<string>(dest, "ErrorDetails_RecommendedAction", () => ErrorDetails.RecommendedAction);
            }

            if (!serializeExtInfo || this.InvocationInfo == null)
            {
                RemotingEncoder.AddNoteProperty(dest, "SerializeExtendedInfo", () => false);
            }
            else
            {
                RemotingEncoder.AddNoteProperty(dest, "SerializeExtendedInfo", () => true);
                this.InvocationInfo.ToPSObjectForRemoting(dest);
                RemotingEncoder.AddNoteProperty<object>(dest, "PipelineIterationInfo", () => PipelineIterationInfo);
            }

            if (!string.IsNullOrEmpty(this.ScriptStackTrace))
            {
                RemotingEncoder.AddNoteProperty(dest, "ErrorDetails_ScriptStackTrace", () => this.ScriptStackTrace);
            }
        }

        /// <summary>
        /// Gets the value for note from mshObject.
        /// </summary>
        /// <param name="mshObject">
        /// PSObject from which value is fetched.
        /// </param>
        /// <param name="note">
        /// name of note whose value is fetched
        /// </param>
        /// <returns>
        /// value of note
        /// </returns>
        private static object GetNoteValue(PSObject mshObject, string note)
        {
            if (mshObject.Properties[note] is PSNoteProperty p)
            {
                return p.Value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Create an ErrorRecord object from serialized ErrorRecord.
        /// serializedErrorRecord PSObject is in the format returned
        /// by ToPSObjectForRemoting method.
        /// </summary>
        /// <param name="serializedErrorRecord">
        /// PSObject to convert to ErrorRecord
        /// </param>
        /// <returns>
        /// ErrorRecord convert from mshObject.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if mshObject parameter is null.
        /// </exception>
        internal static ErrorRecord FromPSObjectForRemoting(PSObject serializedErrorRecord)
        {
            ErrorRecord er = new ErrorRecord();
            er.ConstructFromPSObjectForRemoting(serializedErrorRecord);
            return er;
        }

        private void ConstructFromPSObjectForRemoting(PSObject serializedErrorRecord)
        {
            if (serializedErrorRecord == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(serializedErrorRecord));
            }

            // Get Exception
            PSObject serializedException = RemotingDecoder.GetPropertyValue<PSObject>(serializedErrorRecord, "Exception");

            // Get Target object
            object targetObject = RemotingDecoder.GetPropertyValue<object>(serializedErrorRecord, "TargetObject");

            string exceptionMessage = null;
            if (serializedException != null)
            {
                PSPropertyInfo messageProperty = serializedException.Properties["Message"] as PSPropertyInfo;
                if (messageProperty != null)
                {
                    exceptionMessage = messageProperty.Value as string;
                }
            }

            // Get FullyQualifiedErrorId
            string fullyQualifiedErrorId = RemotingDecoder.GetPropertyValue<string>(serializedErrorRecord, "FullyQualifiedErrorId") ??
                                           "fullyQualifiedErrorId";

            // Get ErrorCategory...
            ErrorCategory errorCategory = RemotingDecoder.GetPropertyValue<ErrorCategory>(serializedErrorRecord, "errorCategory_Category");

            // Get Various ErrorCategory fileds
            string errorCategory_Activity = RemotingDecoder.GetPropertyValue<string>(serializedErrorRecord, "ErrorCategory_Activity");
            string errorCategory_Reason = RemotingDecoder.GetPropertyValue<string>(serializedErrorRecord, "ErrorCategory_Reason");
            string errorCategory_TargetName = RemotingDecoder.GetPropertyValue<string>(serializedErrorRecord, "ErrorCategory_TargetName");
            string errorCategory_TargetType = RemotingDecoder.GetPropertyValue<string>(serializedErrorRecord, "ErrorCategory_TargetType");
            string errorCategory_Message = RemotingDecoder.GetPropertyValue<string>(serializedErrorRecord, "ErrorCategory_Message");

            // Get InvocationInfo (optional property)
            PSObject invocationInfo = Microsoft.PowerShell.DeserializingTypeConverter.GetPropertyValue<PSObject>(
                serializedErrorRecord,
                "InvocationInfo",
                Microsoft.PowerShell.DeserializingTypeConverter.RehydrationFlags.MissingPropertyOk);

            // Get Error Detail (these note properties are optional, so can't right now use RemotingDecoder...)
            string errorDetails_Message =
                GetNoteValue(serializedErrorRecord, "ErrorDetails_Message") as string;

            string errorDetails_RecommendedAction =
                GetNoteValue(serializedErrorRecord, "ErrorDetails_RecommendedAction") as string;

            string errorDetails_ScriptStackTrace =
                GetNoteValue(serializedErrorRecord, "ErrorDetails_ScriptStackTrace") as string;

            RemoteException re = new RemoteException((!string.IsNullOrWhiteSpace(exceptionMessage)) ? exceptionMessage : errorCategory_Message, serializedException, invocationInfo);

            // Create ErrorRecord
            PopulateProperties(
                re,
                targetObject,
                fullyQualifiedErrorId,
                errorCategory,
                errorCategory_Activity,
                errorCategory_Reason,
                errorCategory_TargetName,
                errorCategory_TargetType,
                errorCategory_Message,
                errorDetails_Message,
                errorDetails_RecommendedAction,
                errorDetails_ScriptStackTrace
                );

            re.SetRemoteErrorRecord(this);

            //
            // Get the InvocationInfo
            //
            _serializeExtendedInfo = RemotingDecoder.GetPropertyValue<bool>(serializedErrorRecord, "SerializeExtendedInfo");

            if (_serializeExtendedInfo)
            {
                _invocationInfo = new InvocationInfo(serializedErrorRecord);

                ArrayList iterationInfo = RemotingDecoder.GetPropertyValue<ArrayList>(serializedErrorRecord, "PipelineIterationInfo");
                if (iterationInfo != null)
                {
                    _pipelineIterationInfo = new ReadOnlyCollection<int>((int[])iterationInfo.ToArray(typeof(Int32)));
                }
            }
            else
            {
                _invocationInfo = null;
            }
        }

        #endregion Remoting

        /// <summary>
        /// Copy constructor, for use when a new wrapper exception wraps an
        /// exception which already has an ErrorRecord
        /// ErrorCategoryInfo and ErrorDetails are deep-copied, other fields are not.
        /// </summary>
        /// <param name="errorRecord">Wrapped ErrorRecord.</param>
        /// <param name="replaceParentContainsErrorRecordException">
        /// If the wrapped exception contains a ParentContainsErrorRecordException, the new
        /// ErrorRecord should have this exception as its Exception instead.
        /// </param>
        public ErrorRecord(ErrorRecord errorRecord,
                             Exception replaceParentContainsErrorRecordException)
        {
            if (errorRecord == null)
            {
                throw new PSArgumentNullException(nameof(errorRecord));
            }

            if (replaceParentContainsErrorRecordException != null
                && (errorRecord.Exception is ParentContainsErrorRecordException))
            {
                _error = replaceParentContainsErrorRecordException;
            }
            else
            {
                _error = errorRecord.Exception;
            }

            _target = errorRecord.TargetObject;
            _errorId = errorRecord._errorId;
            _category = errorRecord._category;
            _activityOverride = errorRecord._activityOverride;
            _reasonOverride = errorRecord._reasonOverride;
            _targetNameOverride = errorRecord._targetNameOverride;
            _targetTypeOverride = errorRecord._targetTypeOverride;
            if (errorRecord.ErrorDetails != null)
            {
                ErrorDetails = new ErrorDetails(errorRecord.ErrorDetails);
            }

            SetInvocationInfo(errorRecord._invocationInfo);
            _scriptStackTrace = errorRecord._scriptStackTrace;
            _serializedFullyQualifiedErrorId = errorRecord._serializedFullyQualifiedErrorId;
        }

        #endregion Constructor

        #region Override

        /// <summary>
        /// Wrap the current ErrorRecord instance
        /// A derived class needs to override this method if it contains additional info that needs to be kept when it gets wrapped.
        /// </summary>
        /// <param name="replaceParentContainsErrorRecordException">
        /// If the wrapped exception contains a ParentContainsErrorRecordException, the new
        /// ErrorRecord should have this exception as its Exception instead.
        /// </param>
        /// <returns></returns>
        internal virtual ErrorRecord WrapException(Exception replaceParentContainsErrorRecordException)
        {
            return new ErrorRecord(this, replaceParentContainsErrorRecordException);
        }

        #endregion Override

        #region Public Properties

        /// <summary>
        /// An Exception describing the error.
        /// </summary>
        /// <value>never null</value>
        public Exception Exception
        {
            get
            {
                Diagnostics.Assert(_error != null, "_error is null");
                return _error;
            }
        }

        private Exception _error /* = null */;

        /// <summary>
        /// The object against which the error occurred.
        /// </summary>
        /// <value>may be null</value>
        public object TargetObject { get => _target; }

        private object _target /* = null */;

        internal void SetTargetObject(object target)
        {
            _target = target;
        }

        /// <summary>
        /// Information regarding the ErrorCategory
        /// associated with this error, and with the categorized error message
        /// for that ErrorCategory.
        /// </summary>
        /// <value>never null</value>
        public ErrorCategoryInfo CategoryInfo { get => _categoryInfo ??= new ErrorCategoryInfo(this); }

        private ErrorCategoryInfo _categoryInfo;

        /// <summary>
        /// String which uniquely identifies this error condition.
        /// </summary>
        /// <value>never null</value>
        /// <remarks>
        /// FullyQualifiedErrorid identifies this error condition
        /// more specifically than either the ErrorCategory
        /// or the Exception.  Use FullyQualifiedErrorId to filter specific
        /// error conditions, or to associate special handling with specific
        /// error conditions.
        /// </remarks>
        public string FullyQualifiedErrorId
        {
            get
            {
                if (_serializedFullyQualifiedErrorId != null)
                {
                    return _serializedFullyQualifiedErrorId;
                }

                string typeName = GetInvocationTypeName();
                string delimiter =
                    (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(_errorId))
                        ? string.Empty
                        : ",";
                return NotNull(_errorId) + delimiter + NotNull(typeName);
            }
        }

        /// <summary>
        /// Additional information about the error.
        /// </summary>
        /// <value>may be null</value>
        /// <remarks>
        /// In particular, ErrorDetails.Message (if present and non-empty)
        /// contains a replacement message which should be displayed instead of
        /// Exception.Message.
        /// </remarks>
        public ErrorDetails ErrorDetails { get; set; }

        /// <summary>
        /// Identifies the cmdlet, script, or other command which caused
        /// the error.
        /// </summary>
        /// <value>may be null</value>
        public InvocationInfo InvocationInfo { get => _invocationInfo; }

        private InvocationInfo _invocationInfo /* = null */;

        internal void SetInvocationInfo(InvocationInfo invocationInfo)
        {
            // Save the DisplayScriptPosition, if set
            IScriptExtent savedDisplayScriptPosition = null;
            if (_invocationInfo != null)
            {
                savedDisplayScriptPosition = _invocationInfo.DisplayScriptPosition;
            }

            // Assign the invocationInfo
            if (invocationInfo != null)
            {
                _invocationInfo = new InvocationInfo(invocationInfo.MyCommand, invocationInfo.ScriptPosition);
                _invocationInfo.InvocationName = invocationInfo.InvocationName;
                if (invocationInfo.MyCommand == null)
                {
                    // Pass the history id to new InvocationInfo object of command info is null since history
                    // information cannot be obtained in this case.
                    _invocationInfo.HistoryId = invocationInfo.HistoryId;
                }
            }

            // Restore the DisplayScriptPosition
            if (savedDisplayScriptPosition != null)
            {
                _invocationInfo.DisplayScriptPosition = savedDisplayScriptPosition;
            }

            LockScriptStackTrace();

            //
            // Copy a snapshot of the PipelinePositionInfo from the InvocationInfo to this ErrorRecord
            //
            if (invocationInfo != null && invocationInfo.PipelineIterationInfo != null)
            {
                int[] snapshot = (int[])invocationInfo.PipelineIterationInfo.Clone();

                _pipelineIterationInfo = new ReadOnlyCollection<int>(snapshot);
            }
        }

        // 2005/07/14-913791 "write-error output is confusing and misleading"
        internal bool PreserveInvocationInfoOnce { get; set; }

        /// <summary>
        /// The script stack trace for the error.
        /// </summary>
        public string ScriptStackTrace { get => _scriptStackTrace; }

        private string _scriptStackTrace;

        internal void LockScriptStackTrace()
        {
            if (_scriptStackTrace != null)
            {
                return;
            }

            var context = LocalPipeline.GetExecutionContextFromTLS();
            if (context != null)
            {
                StringBuilder sb = new StringBuilder();
                var callstack = context.Debugger.GetCallStack();
                bool first = true;
                foreach (var frame in callstack)
                {
                    if (!first)
                    {
                        sb.Append(Environment.NewLine);
                    }

                    first = false;
                    sb.Append(frame.ToString());
                }

                _scriptStackTrace = sb.ToString();
            }
        }

        /// <summary>
        /// The status of the pipeline when this record was created.
        /// </summary>
        public ReadOnlyCollection<int> PipelineIterationInfo { get => _pipelineIterationInfo; }

        private ReadOnlyCollection<int> _pipelineIterationInfo = Utils.EmptyReadOnlyCollection<int>();

        /// <summary>
        /// Whether to serialize the InvocationInfo during remote calls.
        /// </summary>
        internal bool SerializeExtendedInfo
        {
            get => _serializeExtendedInfo;

            set => _serializeExtendedInfo = value;
        }

        private bool _serializeExtendedInfo = false;

        #endregion Public Properties

        #region Private
        private readonly string _errorId;

        #region Exposed by ErrorCategoryInfo
        internal ErrorCategory _category;
        internal string _activityOverride;
        internal string _reasonOverride;
        internal string _targetNameOverride;
        internal string _targetTypeOverride;
        #endregion Exposed by ErrorCategoryInfo

        internal static string NotNull(string s) => s ?? string.Empty;

        private string GetInvocationTypeName()
        {
            InvocationInfo invocationInfo = this.InvocationInfo;
            if (invocationInfo == null)
            {
                return string.Empty;
            }

            CommandInfo commandInfo = invocationInfo.MyCommand;
            if (commandInfo == null)
            {
                return string.Empty;
            }

            IScriptCommandInfo scriptInfo = commandInfo as IScriptCommandInfo;
            if (scriptInfo != null)
            {
                return commandInfo.Name;
            }

            if (commandInfo is not CmdletInfo cmdletInfo)
            {
                return string.Empty;
            }

            return cmdletInfo.ImplementingType.FullName;
        }

        #endregion Private

        #region ToString
        /// <summary>
        /// As <see cref="object.ToString()"/>
        /// </summary>
        /// <returns>Developer-readable identifier.</returns>
        public override string ToString()
        {
            if (ErrorDetails != null && !string.IsNullOrEmpty(ErrorDetails.Message))
            {
                return ErrorDetails.Message;
            }

            if (Exception != null)
            {
                return Exception.Message ?? Exception.ToString();
            }

            return base.ToString();
        }
        #endregion ToString

    }

    /// <summary>
    /// Dummy generic class for type inference purposes on typed catch blocks.
    /// </summary>
    /// <typeparam name="TException">Anything that inherits Exception.</typeparam>
    internal class ErrorRecord<TException> : ErrorRecord where TException : Exception
    {
        public new TException Exception { get; }

        public ErrorRecord(Exception exception, string errorId, ErrorCategory errorCategory, object targetObject) : base(exception, errorId, errorCategory, targetObject)
        {
        }
    }

    /// <summary>
    /// Implemented by exception classes which contain additional
    /// <see cref="System.Management.Automation.ErrorRecord"/>
    /// information.
    /// </summary>
    /// <remarks>
    /// PowerShell defines certain exception classes which implement this interface.
    /// This includes wrapper exceptions such as
    /// <see cref="System.Management.Automation.CmdletInvocationException"/>,
    /// and also PowerShell engine errors such as
    /// <see cref="System.Management.Automation.GetValueException"/>.
    /// Cmdlets and providers should not define this interface;
    /// instead, they should use the
    /// WriteError(ErrorRecord) or
    /// ThrowTerminatingError(ErrorRecord) methods.
    /// The ErrorRecord property will contain an ErrorRecord
    /// which contains an instance of
    /// <see cref="System.Management.Automation.ParentContainsErrorRecordException"/>
    /// rather than the actual exception.
    ///
    /// Do not call WriteError(e.ErrorRecord).
    /// The ErrorRecord contained in the ErrorRecord property of
    /// an exception which implements IContainsErrorRecord
    /// should not be passed directly to WriteError, since it contains
    /// a ParentContainsErrorRecordException rather than the real exception.
    ///
    /// It is permitted for PSSnapins to implement custom Exception classes which implement
    /// <see cref="IContainsErrorRecord"/>,
    /// but it is generally preferable for Cmdlets and CmdletProviders to communicate
    /// <see cref="ErrorRecord"/>
    /// information using
    /// <see cref="Cmdlet.ThrowTerminatingError"/>
    /// or
    /// <see cref="Provider.CmdletProvider.ThrowTerminatingError"/>
    /// rather than by throwing an exception which implements
    /// <see cref="IContainsErrorRecord"/>.
    /// Consider implementing
    /// <seealso cref="IContainsErrorRecord"/>
    /// in your custom exception only if you throw it from a context
    /// where a reference to the active
    /// <seealso cref="Cmdlet"/> or
    /// <seealso cref="Provider.CmdletProvider"/>
    /// is no longer available.
    /// </remarks>
#nullable enable
    public interface IContainsErrorRecord
    {
        /// <summary>
        /// This is the
        /// <see cref="ErrorRecord"/>
        /// which provides additional information about the error.
        /// </summary>
        /// <remarks>
        /// The <see cref="ErrorRecord"/> instance returned by
        /// <see cref="IContainsErrorRecord.ErrorRecord"/>
        /// should contain in its
        /// <see cref="System.Management.Automation.ErrorRecord.Exception"/>
        /// property an instance of
        /// <see cref="ParentContainsErrorRecordException"/>
        /// rather than a reference to the root exception.  This prevents
        /// a recursive reference between the exception implementing
        /// <see cref="IContainsErrorRecord"/> and the
        /// <see cref="ErrorRecord"/>.
        /// Use the
        /// <see cref="ParentContainsErrorRecordException(Exception)"/>
        /// constructor so that the
        /// <see cref="ParentContainsErrorRecordException"/>
        /// will have the same
        /// <see cref="System.Exception.Message"/>
        /// as the root exception.
        /// </remarks>
        ErrorRecord ErrorRecord { get; }
    }
#nullable restore

    /// <summary>
    /// Objects implementing this interface can be used by
    /// <see cref="System.Management.Automation.ErrorDetails(IResourceSupplier,string,string,object[])"/>
    /// </summary>
    /// <remarks>
    /// <see cref="Provider.CmdletProvider"/>
    /// implements this interface.  PSSnapins can implement
    /// <see cref="IResourceSupplier"/>
    /// on their custom classes, but the only purpose would be to permit
    /// the custom class to be used in the
    /// <see cref="ErrorDetails(IResourceSupplier,string,string,object[])"/>.
    /// constructor.
    /// <see cref="ErrorDetails"/> contains special constructor
    /// <see cref="ErrorDetails(IResourceSupplier,string,string,object[])"/>
    /// reducing the steps which localizable code generally has to duplicate when it
    /// generates a localizable string.  This variant is preferred over
    /// <see cref="ErrorDetails(string)"/>,
    /// since the improved
    /// information about the error may help enable future scenarios.
    /// </remarks>
#nullable enable
    public interface IResourceSupplier
    {
        /// <summary>
        /// Gets the error message template string corresponding to
        /// <paramref name="baseName"/> and <paramref name="resourceId"/>.
        /// </summary>
        /// <remarks>
        /// If the desired behavior is simple string lookup
        /// in your assembly, you can use the
        /// <see cref="ErrorDetails(Assembly,string,string,object[])"/>
        /// constructor instead and not bother implementing
        /// <see cref="IResourceSupplier"/>.
        /// Consider implementing <see cref="IResourceSupplier"/>
        /// if you want more complex behavior.
        ///
        /// Insertions will be inserted into the string with
        /// <see cref="string.Format(IFormatProvider,string,object[])"/>
        /// to generate the final error message in
        /// <see cref="ErrorDetails.Message"/>.
        /// </remarks>
        /// <param name="baseName">The base resource name.</param>
        /// <param name="resourceId">The resource id.</param>
        /// <returns>The error message template string corresponding to baseName and resourceId.</returns>
        string GetResourceString(string baseName, string resourceId);
    }
}

#pragma warning restore 56506
