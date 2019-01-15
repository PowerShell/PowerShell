// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Runtime.Serialization;
using Microsoft.PowerShell.LocalAccounts;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Base class for cmdlet-specific exceptions.
    /// </summary>
    public class LocalAccountsException : Exception
    {
#region Public Properties
        /// <summary>
        /// Gets the <see cref="System.Management.Automation.ErrorCategory"/>
        /// value for this exception.
        /// </summary>
        public ErrorCategory ErrorCategory
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the target object for this exception. This is used as
        /// the TargetObject member of a PowerShell
        /// <see cref="System.Management.Automation.ErrorRecord"/> object.
        /// </summary>
        public object Target
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the error name. This is used as the ErrorId parameter when
        /// constructing a PowerShell <see cref="System.Management.Automation.ErrorRecord"/>
        /// oject.
        /// </summary>
        public string ErrorName
        {
            get
            {
                string exname = "Exception";
                var exlen = exname.Length;
                var name = this.GetType().Name;

                if (name.EndsWith(exname, StringComparison.OrdinalIgnoreCase) && name.Length > exlen)
                    name = name.Substring(0, name.Length - exlen);
                return name;
            }
        }
#endregion Public Properties

        internal LocalAccountsException(string message, object target, ErrorCategory errorCategory)
            : base(message)
        {
            ErrorCategory = errorCategory;
            Target = target;
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public LocalAccountsException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public LocalAccountsException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public LocalAccountsException(String message, Exception ex) : base(message, ex) { }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected LocalAccountsException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating an error occurred during one of the internal
    /// operations such as opening or closing a handle.
    /// </summary>
    public class InternalException : LocalAccountsException
    {
#region Public Properties
        /// <summary>
        /// Gets the NTSTATUS code for this exception.
        /// </summary>
        public UInt32 StatusCode
        {
            get;
            private set;
        }
#endregion Public Properties

        internal InternalException(UInt32 ntStatus,
                                   string message,
                                   object target,
                                   ErrorCategory errorCategory = ErrorCategory.NotSpecified)
            : base(message, target, errorCategory)
        {
            StatusCode = ntStatus;
        }

        internal InternalException(UInt32 ntStatus,
                                   object target,
                                   ErrorCategory errorCategory = ErrorCategory.NotSpecified)
            : this(ntStatus,
                   StringUtil.Format(Strings.UnspecifiedErrorNtStatus, ntStatus),
                   target,
                   errorCategory)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public InternalException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public InternalException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public InternalException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected InternalException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating an error occurred when a native function
    /// is called that returns a Win32 error code as opposed to an
    /// NT Status code.
    /// </summary>
    public class Win32InternalException : LocalAccountsException
    {
#region Public Properties
        /// <summary>
        /// The Win32 error code for this exception.
        /// </summary>
        public int NativeErrorCode
        {
            get;
            private set;
        }
#endregion Public Properties

        internal Win32InternalException(int errorCode,
                                        string message,
                                        object target,
                                        ErrorCategory errorCategory = ErrorCategory.NotSpecified)
            : base(message, target, errorCategory)
        {
            NativeErrorCode = errorCode;
        }

        internal Win32InternalException(int errorCode,
                                        object target,
                                        ErrorCategory errorCategory = ErrorCategory.NotSpecified)
            : this(errorCode,
                   StringUtil.Format(Strings.UnspecifiedErrorWin32Error, errorCode),
                   target,
                   errorCategory)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public Win32InternalException() : base() {}
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public Win32InternalException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public Win32InternalException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected Win32InternalException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating an invalid password.
    /// </summary>
    public class InvalidPasswordException : LocalAccountsException
    {
        /// <summary>
        /// Generates with a default invalid password message.
        /// </summary>
        public InvalidPasswordException()
            : base(Strings.InvalidPassword, null, ErrorCategory.InvalidArgument)
        {
        }

        /// <summary>
        /// Generates the exception with the specified message.
        /// </summary>
        /// <param name="message"></param>
        public InvalidPasswordException(string message)
            : base(message, null, ErrorCategory.InvalidArgument)
        {
        }

        /// <summary>
        /// Creates a message from the specified error code.
        /// </summary>
        /// <param name="errorCode"></param>
        public InvalidPasswordException(uint errorCode)
            : base(StringUtil.GetSystemMessage(errorCode), null, ErrorCategory.InvalidArgument)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public InvalidPasswordException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected InvalidPasswordException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception thrown when invalid parameter pairing is detected.
    /// </summary>
    public class InvalidParametersException : LocalAccountsException
    {
        /// <summary>
        /// Creates InvalidParametersException using the specified message.
        /// </summary>
        /// <param name="message"></param>
        public InvalidParametersException(string message)
            : base(message, null, ErrorCategory.InvalidArgument)
        {
        }

        internal InvalidParametersException(string parameterA, string parameterB)
            : this(StringUtil.Format(Strings.InvalidParameterPair, parameterA, parameterB))
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public InvalidParametersException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public InvalidParametersException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected InvalidParametersException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating permission denied.
    /// </summary>
    public class AccessDeniedException : LocalAccountsException
    {
        internal AccessDeniedException(object target)
            : base(Strings.AccessDenied, target, ErrorCategory.PermissionDenied)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public AccessDeniedException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public AccessDeniedException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public AccessDeniedException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected AccessDeniedException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating that the name of a user or group is invalid.
    /// </summary>
    public class InvalidNameException : LocalAccountsException
    {
        internal InvalidNameException(string name, object target)
            : base(StringUtil.Format(Strings.InvalidName, name), target, ErrorCategory.InvalidArgument)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public InvalidNameException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public InvalidNameException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public InvalidNameException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected InvalidNameException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating that the specified name is already in use.
    /// </summary>
    public class NameInUseException : LocalAccountsException
    {
        internal NameInUseException(string name, object target)
            : base(StringUtil.Format(Strings.NameInUse, name), target, ErrorCategory.InvalidArgument)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public NameInUseException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public NameInUseException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public NameInUseException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected NameInUseException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating that an entity of some kind was not found.
    /// Also serves as a base class for more specific object-not-found errors.
    /// </summary>
    public class NotFoundException : LocalAccountsException
    {
        internal NotFoundException(string message, object target)
          : base(message, target, ErrorCategory.ObjectNotFound)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public NotFoundException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public NotFoundException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public NotFoundException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected NotFoundException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating that a principal was not Found.
    /// </summary>
    public class PrincipalNotFoundException : NotFoundException
    {
        internal PrincipalNotFoundException(string principal, object target)
            : base(StringUtil.Format(Strings.PrincipalNotFound, principal), target)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public PrincipalNotFoundException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public PrincipalNotFoundException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public PrincipalNotFoundException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected PrincipalNotFoundException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating that a group was not found.
    /// </summary>
    public class GroupNotFoundException : NotFoundException
    {
        internal GroupNotFoundException(string group, object target)
            : base(StringUtil.Format(Strings.GroupNotFound, group), target)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public GroupNotFoundException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public GroupNotFoundException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public GroupNotFoundException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected GroupNotFoundException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating that a user was not found.
    /// </summary>
    public class UserNotFoundException : NotFoundException
    {
        internal UserNotFoundException(string user, object target)
            : base(StringUtil.Format(Strings.UserNotFound, user), target)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public UserNotFoundException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public UserNotFoundException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public UserNotFoundException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected UserNotFoundException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating that a group member was not found.
    /// </summary>
    public class MemberNotFoundException : NotFoundException
    {
        internal MemberNotFoundException(string member, string group)
            : base(StringUtil.Format(Strings.MemberNotFound, member, group), member)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public MemberNotFoundException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public MemberNotFoundException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public MemberNotFoundException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected MemberNotFoundException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating that an entity of some kind already exists.
    /// Also serves as a base class for more specific object-exists errors.
    /// </summary>
    public class ObjectExistsException : LocalAccountsException
    {
        internal ObjectExistsException(string message, object target)
            : base(message, target, ErrorCategory.ResourceExists)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public ObjectExistsException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public ObjectExistsException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public ObjectExistsException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected ObjectExistsException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating that a group already exists.
    /// </summary>
    public class GroupExistsException : ObjectExistsException
    {
        internal GroupExistsException(string group, object target)
            : base(StringUtil.Format(Strings.GroupExists, group), target)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public GroupExistsException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public GroupExistsException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public GroupExistsException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected GroupExistsException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating that a group already exists.
    /// </summary>
    public class UserExistsException : ObjectExistsException
    {
        internal UserExistsException(string user, object target)
            : base(StringUtil.Format(Strings.UserExists, user), target)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public UserExistsException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public UserExistsException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public UserExistsException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected UserExistsException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    /// Exception indicating that an object already exists as a group member.
    /// </summary>
    public class MemberExistsException : ObjectExistsException
    {
        internal MemberExistsException(string member, string group, object target)
            : base(StringUtil.Format(Strings.MemberExists, member, group), target)
        {
        }

        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        public MemberExistsException() : base() { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        public MemberExistsException(String message) : base(message) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public MemberExistsException(String message, Exception ex) : base(message, ex) { }
        /// <summary>
        /// Compliance Constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctx"></param>
        protected MemberExistsException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }
}
