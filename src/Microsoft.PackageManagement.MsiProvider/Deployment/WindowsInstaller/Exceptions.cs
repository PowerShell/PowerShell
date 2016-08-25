//---------------------------------------------------------------------
// <copyright file="Exceptions.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Exceptions for the Microsoft.Deployment.WindowsInstaller namespace.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Text;

    /// <summary>
    /// Base class for Windows Installer exceptions.
    /// </summary>
    [Serializable]
    internal class InstallerException : SystemException
    {
        private int errorCode;
        private object[] errorData;

        /// <summary>
        /// Creates a new InstallerException with a specified error message and a reference to the
        /// inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the
        /// innerException parameter is not a null reference (Nothing in Visual Basic), the current exception
        /// is raised in a catch block that handles the inner exception.</param>
        public InstallerException(string msg, Exception innerException)
            : this(0, msg, innerException)
        {
        }

        /// <summary>
        /// Creates a new InstallerException with a specified error message.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        public InstallerException(string msg)
            : this(0, msg)
        {
        }

        /// <summary>
        /// Creates a new InstallerException.
        /// </summary>
        public InstallerException()
            : this(0, null)
        {
        }

        internal InstallerException(int errorCode, string msg, Exception innerException)
            : base(msg, innerException)
        {
            this.errorCode = errorCode;
            this.SaveErrorRecord();
        }

        internal InstallerException(int errorCode, string msg)
            : this(errorCode, msg, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InstallerException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected InstallerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            this.errorCode = info.GetInt32("msiErrorCode");
        }

        /// <summary>
        /// Gets the system error code that resulted in this exception, or 0 if not applicable.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int ErrorCode
        {
            get
            {
                return this.errorCode;
            }
        }

        /// <summary>
        /// Gets a message that describes the exception.  This message may contain detailed
        /// formatted error data if it was available.
        /// </summary>
        public override String Message
        {
            get
            {
                string msg = base.Message;
                using (Record errorRec = this.GetErrorRecord())
                {
                    if (errorRec != null)
                    {
                        string errorMsg = Installer.GetErrorMessage(errorRec, CultureInfo.InvariantCulture);
                        msg = Combine(msg, errorMsg);
                    }
                }
                return msg;
            }
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter=true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("msiErrorCode", this.errorCode);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Gets extended information about the error, or null if no further information
        /// is available.
        /// </summary>
        /// <returns>A Record object. Field 1 of the Record contains the installer
        /// message code. Other fields contain data specific to the particular error.</returns>
        /// <remarks><p>
        /// If the record is passed to <see cref="Session.Message"/>, it is formatted
        /// by looking up the string in the current database. If there is no installation
        /// session, the formatted error message may be obtained by a query on the Error table using
        /// the error code, followed by a call to <see cref="Record.ToString()"/>.
        /// Alternatively, the standard MSI message can by retrieved by calling the
        /// <see cref="Installer.GetErrorMessage(Record,CultureInfo)"/> method.
        /// </p><p>
        /// The following methods and properties may report extended error data:
        /// <list type="bullet">
        /// <item><see cref="Database"/> (constructor)</item>
        /// <item><see cref="Database"/>.<see cref="Database.ApplyTransform(string,TransformErrors)"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.Commit"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.Execute(string,object[])"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.ExecuteQuery(string,object[])"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.ExecuteIntegerQuery(string,object[])"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.ExecuteStringQuery(string,object[])"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.Export"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.ExportAll"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.GenerateTransform"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.Import"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.ImportAll"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.Merge(Database,string)"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.OpenView"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.SummaryInfo"/></item>
        /// <item><see cref="Database"/>.<see cref="Database.ViewTransform"/></item>
        /// <item><see cref="View"/>.<see cref="View.Assign"/></item>
        /// <item><see cref="View"/>.<see cref="View.Delete"/></item>
        /// <item><see cref="View"/>.<see cref="View.Execute(Record)"/></item>
        /// <item><see cref="View"/>.<see cref="View.Insert"/></item>
        /// <item><see cref="View"/>.<see cref="View.InsertTemporary"/></item>
        /// <item><see cref="View"/>.<see cref="View.Merge"/></item>
        /// <item><see cref="View"/>.<see cref="View.Modify"/></item>
        /// <item><see cref="View"/>.<see cref="View.Refresh"/></item>
        /// <item><see cref="View"/>.<see cref="View.Replace"/></item>
        /// <item><see cref="View"/>.<see cref="View.Seek"/></item>
        /// <item><see cref="View"/>.<see cref="View.Update"/></item>
        /// <item><see cref="View"/>.<see cref="View.Validate"/></item>
        /// <item><see cref="View"/>.<see cref="View.ValidateFields"/></item>
        /// <item><see cref="View"/>.<see cref="View.ValidateDelete"/></item>
        /// <item><see cref="View"/>.<see cref="View.ValidateNew"/></item>
        /// <item><see cref="SummaryInfo"/> (constructor)</item>
        /// <item><see cref="Record"/>.<see cref="Record.SetStream(int,string)"/></item>
        /// <item><see cref="Session"/>.<see cref="Session.SetInstallLevel"/></item>
        /// <item><see cref="Session"/>.<see cref="Session.GetSourcePath"/></item>
        /// <item><see cref="Session"/>.<see cref="Session.GetTargetPath"/></item>
        /// <item><see cref="Session"/>.<see cref="Session.SetTargetPath"/></item>
        /// <item><see cref="ComponentInfo"/>.<see cref="ComponentInfo.CurrentState"/></item>
        /// <item><see cref="FeatureInfo"/>.<see cref="FeatureInfo.CurrentState"/></item>
        /// <item><see cref="FeatureInfo"/>.<see cref="FeatureInfo.ValidStates"/></item>
        /// <item><see cref="FeatureInfo"/>.<see cref="FeatureInfo.GetCost"/></item>
        /// </list>
        /// </p><p>
        /// The Record object should be <see cref="InstallerHandle.Close"/>d after use.
        /// It is best that the handle be closed manually as soon as it is no longer
        /// needed, as leaving lots of unused handles open can degrade performance.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetlasterrorrecord.asp">MsiGetLastErrorRecord</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public Record GetErrorRecord()
        {
            return this.errorData != null ? new Record(this.errorData) : null;
        }

        internal static Exception ExceptionFromReturnCode(uint errorCode)
        {
            return ExceptionFromReturnCode(errorCode, null);
        }

        internal static Exception ExceptionFromReturnCode(uint errorCode, string msg)
        {
            msg = Combine(GetSystemMessage(errorCode), msg);
            switch (errorCode)
            {
                case (uint) NativeMethods.Error.FILE_NOT_FOUND:
                case (uint) NativeMethods.Error.PATH_NOT_FOUND: return new FileNotFoundException(msg);

                case (uint) NativeMethods.Error.INVALID_PARAMETER:
                case (uint) NativeMethods.Error.DIRECTORY:
                case (uint) NativeMethods.Error.UNKNOWN_PROPERTY:
                case (uint) NativeMethods.Error.UNKNOWN_PRODUCT:
                case (uint) NativeMethods.Error.UNKNOWN_FEATURE:
                case (uint) NativeMethods.Error.UNKNOWN_COMPONENT: return new ArgumentException(msg);

                case (uint) NativeMethods.Error.BAD_QUERY_SYNTAX: return new BadQuerySyntaxException(msg);

                case (uint) NativeMethods.Error.INVALID_HANDLE_STATE:
                case (uint) NativeMethods.Error.INVALID_HANDLE:
                    InvalidHandleException ihex = new InvalidHandleException(msg);
                    ihex.errorCode = (int) errorCode;
                    return ihex;

                case (uint) NativeMethods.Error.INSTALL_USEREXIT: return new InstallCanceledException(msg);

                case (uint) NativeMethods.Error.CALL_NOT_IMPLEMENTED: return new NotImplementedException(msg);

                default: return new InstallerException((int) errorCode, msg);
            }
        }

        internal static string GetSystemMessage(uint errorCode)
        {
            const uint FORMAT_MESSAGE_IGNORE_INSERTS  = 0x00000200;
            const uint FORMAT_MESSAGE_FROM_SYSTEM     = 0x00001000;

            StringBuilder buf = new StringBuilder(1024);
            uint formatCount = NativeMethods.FormatMessage(
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                IntPtr.Zero,
                (uint) errorCode,
                0,
                buf,
                (uint) buf.Capacity,
                IntPtr.Zero);

            if (formatCount != 0)
            {
                return buf.ToString().Trim();
            }
            else
            {
                return null;
            }
        }

        internal void SaveErrorRecord()
        {
            // TODO: pass an affinity handle here?
            int recordHandle = RemotableNativeMethods.MsiGetLastErrorRecord(0);
            if (recordHandle != 0)
            {
                using (Record errorRec = new Record((IntPtr) recordHandle, true, null))
                {
                    this.errorData = new object[errorRec.FieldCount];
                    for (int i = 0; i < this.errorData.Length; i++)
                    {
                        this.errorData[i] = errorRec[i + 1];
                    }
                }
            }
            else
            {
                this.errorData = null;
            }
        }

        private static string Combine(string msg1, string msg2)
        {
            if (msg1 == null) return msg2;
            if (msg2 == null) return msg1;
            return msg1 + " " + msg2;
        }
    }

    /// <summary>
    /// User Canceled the installation.
    /// </summary>
    [Serializable]
    internal class InstallCanceledException : InstallerException
    {
        /// <summary>
        /// Creates a new InstallCanceledException with a specified error message and a reference to the
        /// inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the
        /// innerException parameter is not a null reference (Nothing in Visual Basic), the current exception
        /// is raised in a catch block that handles the inner exception.</param>
        public InstallCanceledException(string msg, Exception innerException)
            : base((int) NativeMethods.Error.INSTALL_USEREXIT, msg, innerException)
        {
        }

        /// <summary>
        /// Creates a new InstallCanceledException with a specified error message.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        public InstallCanceledException(string msg)
            : this(msg, null)
        {
        }

        /// <summary>
        /// Creates a new InstallCanceledException.
        /// </summary>
        public InstallCanceledException()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InstallCanceledException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected InstallCanceledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// A bad SQL query string was passed to <see cref="Database.OpenView"/> or <see cref="Database.Execute(string,object[])"/>.
    /// </summary>
    [Serializable]
    internal class BadQuerySyntaxException : InstallerException
    {
        /// <summary>
        /// Creates a new BadQuerySyntaxException with a specified error message and a reference to the
        /// inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the
        /// innerException parameter is not a null reference (Nothing in Visual Basic), the current exception
        /// is raised in a catch block that handles the inner exception.</param>
        public BadQuerySyntaxException(string msg, Exception innerException)
            : base((int) NativeMethods.Error.BAD_QUERY_SYNTAX, msg, innerException)
        {
        }

        /// <summary>
        /// Creates a new BadQuerySyntaxException with a specified error message.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        public BadQuerySyntaxException(string msg)
            : this(msg, null)
        {
        }

        /// <summary>
        /// Creates a new BadQuerySyntaxException.
        /// </summary>
        public BadQuerySyntaxException()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the BadQuerySyntaxException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected BadQuerySyntaxException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// A method was called on an invalid installer handle.  The handle may have been already closed.
    /// </summary>
    [Serializable]
    internal class InvalidHandleException : InstallerException
    {
        /// <summary>
        /// Creates a new InvalidHandleException with a specified error message and a reference to the
        /// inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the
        /// innerException parameter is not a null reference (Nothing in Visual Basic), the current exception
        /// is raised in a catch block that handles the inner exception.</param>
        public InvalidHandleException(string msg, Exception innerException)
            : base((int) NativeMethods.Error.INVALID_HANDLE, msg, innerException)
        {
        }

        /// <summary>
        /// Creates a new InvalidHandleException with a specified error message.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        public InvalidHandleException(string msg)
            : this(msg, null)
        {
        }

        /// <summary>
        /// Creates a new InvalidHandleException.
        /// </summary>
        public InvalidHandleException()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidHandleException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected InvalidHandleException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// A failure occurred when executing <see cref="Database.Merge(Database,string)"/>.  The exception may contain
    /// details about the merge conflict.
    /// </summary>
    [Serializable]
    internal class MergeException : InstallerException
    {
        private IList<string> conflictTables;
        private IList<int> conflictCounts;

        /// <summary>
        /// Creates a new MergeException with a specified error message and a reference to the
        /// inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the
        /// innerException parameter is not a null reference (Nothing in Visual Basic), the current exception
        /// is raised in a catch block that handles the inner exception.</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public MergeException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }

        /// <summary>
        /// Creates a new MergeException with a specified error message.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public MergeException(string msg)
            : base(msg)
        {
        }

        /// <summary>
        /// Creates a new MergeException.
        /// </summary>
        public MergeException()
            : base()
        {
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal MergeException(Database db, string conflictsTableName)
            : base("Merge failed.")
        {
            if (conflictsTableName != null)
            {
                IList<string> conflictTableList = new List<string>();
                IList<int> conflictCountList = new List<int>();

                using (View view = db.OpenView("SELECT `Table`, `NumRowMergeConflicts` FROM `" + conflictsTableName + "`"))
                {
                    view.Execute();

                    foreach (Record rec in view) using (rec)
                    {
                        conflictTableList.Add(rec.GetString(1));
                        conflictCountList.Add((int) rec.GetInteger(2));
                    }
                }

                this.conflictTables = conflictTableList;
                this.conflictCounts = conflictCountList;
            }
        }

        /// <summary>
        /// Initializes a new instance of the MergeException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected MergeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            this.conflictTables = (string[]) info.GetValue("mergeConflictTables", typeof(string[]));
            this.conflictCounts = (int[]) info.GetValue("mergeConflictCounts", typeof(int[]));
        }

        /// <summary>
        /// Gets the number of merge conflicts in each table, corresponding to the tables returned by
        /// <see cref="ConflictTables"/>.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public IList<int> ConflictCounts
        {
            get
            {
                return new List<int>(this.conflictCounts);
            }
        }

        /// <summary>
        /// Gets the list of tables containing merge conflicts.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public IList<string> ConflictTables
        {
            get
            {
                return new List<string>(this.conflictTables);
            }
        }

        /// <summary>
        /// Gets a message that describes the merge conflicts.
        /// </summary>
        public override String Message
        {
            get
            {
                StringBuilder msg = new StringBuilder(base.Message);
                if (this.conflictTables != null)
                {
                    for (int i = 0; i < this.conflictTables.Count; i++)
                    {
                        msg.Append(i == 0 ? "  Conflicts: " : ", ");
                        msg.Append(this.conflictTables[i]);
                        msg.Append('(');
                        msg.Append(this.conflictCounts[i]);
                        msg.Append(')');
                    }
                }
                return msg.ToString();
            }
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter=true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("mergeConflictTables", this.conflictTables);
            info.AddValue("mergeConflictCounts", this.conflictCounts);
            base.GetObjectData(info, context);
        }
    }
}
