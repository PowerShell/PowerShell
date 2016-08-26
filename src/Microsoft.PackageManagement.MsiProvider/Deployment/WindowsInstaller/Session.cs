//---------------------------------------------------------------------
// <copyright file="Session.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// The Session object controls the installation process. It opens the
    /// install database, which contains the installation tables and data.
    /// </summary>
    /// <remarks><p>
    /// This object is associated with a standard set of action functions,
    /// each performing particular operations on data from one or more tables. Additional
    /// custom actions may be added for particular product installations. The basic engine
    /// function is a sequencer that fetches sequential records from a designated sequence
    /// table, evaluates any specified condition expression, and executes the designated
    /// action. Actions not recognized by the engine are deferred to the UI handler object
    /// for processing, usually dialog box sequences.
    /// </p><p>
    /// Note that only one Session object can be opened by a single process.
    /// </p></remarks>
    internal sealed class Session : InstallerHandle, IFormatProvider
    {
        private Database database;
        private CustomActionData customActionData;
        private bool sessionAccessValidated = false;

        internal Session(IntPtr handle, bool ownsHandle)
            : base(handle, ownsHandle)
        {
        }

        /// <summary>
        /// Gets the Database for the install session.
        /// </summary>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="InstallerException">the Database cannot be accessed</exception>
        /// <remarks><p>
        /// Normally there is no need to close this Database object.  The same object can be
        /// used throughout the lifetime of the Session, and it will be closed when the Session
        /// is closed.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetactivedatabase.asp">MsiGetActiveDatabase</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public Database Database
        {
            get
            {
                if (this.database == null || this.database.IsClosed)
                {
                    lock (this.Sync)
                    {
                        if (this.database == null || this.database.IsClosed)
                        {
                            this.ValidateSessionAccess();

                            int hDb = RemotableNativeMethods.MsiGetActiveDatabase((int) this.Handle);
                            if (hDb == 0)
                            {
                                throw new InstallerException();
                            }
                            this.database = new Database((IntPtr) hDb, true, "", DatabaseOpenMode.ReadOnly);
                        }
                    }
                }
                return this.database;
            }
        }

        /// <summary>
        /// Gets the numeric language ID used by the current install session.
        /// </summary>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetlanguage.asp">MsiGetLanguage</a>
        /// </p></remarks>
        public int Language
        {
            get
            {
                return (int) RemotableNativeMethods.MsiGetLanguage((int) this.Handle);
            }
        }

        /// <summary>
        /// Gets or sets the string value of a named installer property, as maintained by the
        /// Session object in the in-memory Property table, or, if it is prefixed with a percent
        /// sign (%), the value of a system environment variable for the current process.
        /// </summary>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetproperty.asp">MsiGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisetproperty.asp">MsiSetProperty</a>
        /// </p></remarks>
        public string this[string property]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(property))
                {
                    throw new ArgumentNullException("property");
                }

                if (!this.sessionAccessValidated &&
                    !Session.NonImmediatePropertyNames.Contains(property))
                {
                    this.ValidateSessionAccess();
                }

                StringBuilder buf = new StringBuilder();
                uint bufSize = 0;
                uint ret = RemotableNativeMethods.MsiGetProperty((int) this.Handle, property, buf, ref bufSize);
                if (ret == (uint) NativeMethods.Error.MORE_DATA)
                {
                    buf.Capacity = (int) ++bufSize;
                    ret = RemotableNativeMethods.MsiGetProperty((int) this.Handle, property, buf, ref bufSize);
                }

                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
                return buf.ToString();
            }

            set
            {
                if (string.IsNullOrWhiteSpace(property))
                {
                    throw new ArgumentNullException("property");
                }

                this.ValidateSessionAccess();

                if (value == null)
                {
                    value = String.Empty;
                }

                uint ret = RemotableNativeMethods.MsiSetProperty((int) this.Handle, property, value);
                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }
        }

        /// <summary>
        /// Creates a new Session object from an integer session handle.
        /// </summary>
        /// <param name="handle">Integer session handle</param>
        /// <param name="ownsHandle">true to close the handle when this object is disposed or finalized</param>
        /// <remarks><p>
        /// This method is only provided for interop purposes.  A Session object
        /// should normally be obtained by calling <see cref="Installer.OpenPackage(Database,bool)"/>
        /// or <see cref="Installer.OpenProduct"/>.
        /// </p></remarks>
        public static Session FromHandle(IntPtr handle, bool ownsHandle)
        {
            return new Session(handle, ownsHandle);
        }

        /// <summary>
        /// Performs any enabled logging operations and defers execution to the UI handler
        /// object associated with the engine.
        /// </summary>
        /// <param name="messageType">Type of message to be processed</param>
        /// <param name="record">Contains message-specific fields</param>
        /// <returns>A message-dependent return value</returns>
        /// <exception cref="InvalidHandleException">the Session or Record handle is invalid</exception>
        /// <exception cref="ArgumentOutOfRangeException">an invalid message kind is specified</exception>
        /// <exception cref="InstallCanceledException">the user exited the installation</exception>
        /// <exception cref="InstallerException">the message-handler failed for an unknown reason</exception>
        /// <remarks><p>
        /// Logging may be selectively enabled for the various message types.
        /// See the <see cref="Installer.EnableLog(InstallLogModes,string)"/> method.
        /// </p><p>
        /// If record field 0 contains a formatting string, it is used to format the data in
        /// the other fields. Else if the message is an error, warning, or user message, an attempt
        /// is made to find a message template in the Error table for the current database using the
        /// error number found in field 1 of the record for message types and return values.
        /// </p><p>
        /// The <paramref name="messageType"/> parameter may also include message-box flags from
        /// the following enumerations: System.Windows.Forms.MessageBoxButtons,
        /// System.Windows.Forms.MessageBoxDefaultButton, System.Windows.Forms.MessageBoxIcon.  These
        /// flags can be combined with the InstallMessage with a bitwise OR.
        /// </p><p>
        /// Note, this method never returns Cancel or Error values.  Instead, appropriate
        /// exceptions are thrown in those cases.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiprocessmessage.asp">MsiProcessMessage</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public MessageResult Message(InstallMessage messageType, Record record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            int ret = RemotableNativeMethods.MsiProcessMessage((int) this.Handle, (uint) messageType, (int) record.Handle);
            if (ret < 0)
            {
                throw new InstallerException();
            }
            else if (ret == (int) MessageResult.Cancel)
            {
                throw new InstallCanceledException();
            }
            return (MessageResult) ret;
        }

        /// <summary>
        /// Writes a message to the log, if logging is enabled.
        /// </summary>
        /// <param name="msg">The line to be written to the log</param>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiprocessmessage.asp">MsiProcessMessage</a>
        /// </p></remarks>
        public void Log(string msg)
        {
            if (msg == null)
            {
                throw new ArgumentNullException("msg");
            }

            using (Record rec = new Record(0))
            {
                rec.FormatString = msg;
                this.Message(InstallMessage.Info, rec);
            }
        }

        /// <summary>
        /// Writes a formatted message to the log, if logging is enabled.
        /// </summary>
        /// <param name="format">The line to be written to the log, containing 0 or more format specifiers</param>
        /// <param name="args">An array containing 0 or more objects to be formatted</param>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiprocessmessage.asp">MsiProcessMessage</a>
        /// </p></remarks>
        public void Log(string format, params object[] args)
        {
            this.Log(String.Format(CultureInfo.InvariantCulture, format, args));
        }

        /// <summary>
        /// Evaluates a logical expression containing symbols and values.
        /// </summary>
        /// <param name="condition">conditional expression</param>
        /// <returns>The result of the condition evaluation</returns>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="ArgumentNullException">the condition is null or empty</exception>
        /// <exception cref="InvalidOperationException">the conditional expression is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msievaluatecondition.asp">MsiEvaluateCondition</a>
        /// </p></remarks>
        public bool EvaluateCondition(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                throw new ArgumentNullException("condition");
            }

            uint value = RemotableNativeMethods.MsiEvaluateCondition((int) this.Handle, condition);
            if (value == 0)
            {
                return false;
            }
            else if (value == 1)
            {
                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Evaluates a logical expression containing symbols and values, specifying a default
        /// value to be returned in case the condition is empty.
        /// </summary>
        /// <param name="condition">conditional expression</param>
        /// <param name="defaultValue">value to return if the condition is empty</param>
        /// <returns>The result of the condition evaluation</returns>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="InvalidOperationException">the conditional expression is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msievaluatecondition.asp">MsiEvaluateCondition</a>
        /// </p></remarks>
        public bool EvaluateCondition(string condition, bool defaultValue)
        {
            if (condition == null)
            {
                throw new ArgumentNullException("condition");
            }
            else if (condition.Length == 0)
            {
                return defaultValue;
            }
            else
            {
                this.ValidateSessionAccess();
                return this.EvaluateCondition(condition);
            }
        }

        /// <summary>
        /// Formats a string containing installer properties.
        /// </summary>
        /// <param name="format">A format string containing property tokens</param>
        /// <returns>A formatted string containing property data</returns>
        /// <exception cref="InvalidHandleException">the Record handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiformatrecord.asp">MsiFormatRecord</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames")]
        public string Format(string format)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            using (Record formatRec = new Record(0))
            {
                formatRec.FormatString = format;
                return formatRec.ToString(this);
            }
        }

        /// <summary>
        /// Returns a formatted string from record data.
        /// </summary>
        /// <param name="record">Record object containing a template and data to be formatted.
        /// The template string must be set in field 0 followed by any referenced data parameters.</param>
        /// <returns>A formatted string containing the record data</returns>
        /// <exception cref="InvalidHandleException">the Record handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiformatrecord.asp">MsiFormatRecord</a>
        /// </p></remarks>
        public string FormatRecord(Record record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            return record.ToString(this);
        }

        /// <summary>
        /// Returns a formatted string from record data using a specified format.
        /// </summary>
        /// <param name="record">Record object containing a template and data to be formatted</param>
        /// <param name="format">Format string to be used instead of field 0 of the Record</param>
        /// <returns>A formatted string containing the record data</returns>
        /// <exception cref="InvalidHandleException">the Record handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiformatrecord.asp">MsiFormatRecord</a>
        /// </p></remarks>
        [Obsolete("This method is obsolete because it has undesirable side-effects. As an alternative, set the Record's " +
            "FormatString property separately before calling the FormatRecord() override that takes only the Record parameter.")]
        public string FormatRecord(Record record, string format)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            return record.ToString(format, this);
        }

        /// <summary>
        /// Retrieves product properties (not session properties) from the product database.
        /// </summary>
        /// <returns>Value of the property, or an empty string if the property is not set.</returns>
        /// <remarks><p>
        /// Note this is not the correct method for getting ordinary session properties. For that,
        /// see the indexer on the Session class.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetproductproperty.asp">MsiGetProductProperty</a>
        /// </p></remarks>
        public string GetProductProperty(string property)
        {
            if (string.IsNullOrWhiteSpace(property))
            {
                throw new ArgumentNullException("property");
            }

            this.ValidateSessionAccess();

            StringBuilder buf = new StringBuilder();
            uint bufSize = (uint) buf.Capacity;
            uint ret = NativeMethods.MsiGetProductProperty((int) this.Handle, property, buf, ref bufSize);

            if (ret == (uint) NativeMethods.Error.MORE_DATA)
            {
                buf.Capacity = (int) ++bufSize;
                ret = NativeMethods.MsiGetProductProperty((int) this.Handle, property, buf, ref bufSize);
            }

            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
            return buf.ToString();
        }

        /// <summary>
        /// Gets an accessor for components in the current session.
        /// </summary>
        public ComponentInfoCollection Components
        {
            get
            {
                this.ValidateSessionAccess();
                return new ComponentInfoCollection(this);
            }
        }

        /// <summary>
        /// Gets an accessor for features in the current session.
        /// </summary>
        public FeatureInfoCollection Features
        {
            get
            {
                this.ValidateSessionAccess();
                return new FeatureInfoCollection(this);
            }
        }

        /// <summary>
        /// Checks to see if sufficient disk space is present for the current installation.
        /// </summary>
        /// <returns>True if there is sufficient disk space; false otherwise.</returns>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiverifydiskspace.asp">MsiVerifyDiskSpace</a>
        /// </p></remarks>
        public bool VerifyDiskSpace()
        {
            this.ValidateSessionAccess();

            uint ret = RemotableNativeMethods.MsiVerifyDiskSpace((int)this.Handle);
            if (ret == (uint) NativeMethods.Error.DISK_FULL)
            {
                return false;
            }
            else if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
            return true;
        }

        /// <summary>
        /// Gets the total disk space per drive required for the installation.
        /// </summary>
        /// <returns>A list of InstallCost structures, specifying the cost for each drive</returns>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msienumcomponentcosts.asp">MsiEnumComponentCosts</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IList<InstallCost> GetTotalCost()
        {
            this.ValidateSessionAccess();

            IList<InstallCost> costs = new List<InstallCost>();
            StringBuilder driveBuf = new StringBuilder(20);
            for (uint i = 0; true; i++)
            {
                int cost, tempCost;
                uint driveBufSize = (uint) driveBuf.Capacity;
                uint ret = RemotableNativeMethods.MsiEnumComponentCosts(
                    (int) this.Handle,
                    null,
                    i,
                    (int) InstallState.Default,
                    driveBuf,
                    ref driveBufSize,
                    out cost,
                    out tempCost);
                if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS) break;
                if (ret == (uint) NativeMethods.Error.MORE_DATA)
                {
                    driveBuf.Capacity = (int) ++driveBufSize;
                    ret = RemotableNativeMethods.MsiEnumComponentCosts(
                        (int) this.Handle,
                        null,
                        i,
                        (int) InstallState.Default,
                        driveBuf,
                        ref driveBufSize,
                        out cost,
                        out tempCost);
                }

                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
                costs.Add(new InstallCost(driveBuf.ToString(), cost * 512L, tempCost * 512L));
            }
            return costs;
        }

        /// <summary>
        /// Gets the designated mode flag for the current install session.
        /// </summary>
        /// <param name="mode">The type of mode to be checked.</param>
        /// <returns>The value of the designated mode flag.</returns>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="ArgumentOutOfRangeException">an invalid mode flag was specified</exception>
        /// <remarks><p>
        /// Note that only the following run modes are available to read from
        /// a deferred custom action:<list type="bullet">
        /// <item><description><see cref="InstallRunMode.Scheduled"/></description></item>
        /// <item><description><see cref="InstallRunMode.Rollback"/></description></item>
        /// <item><description><see cref="InstallRunMode.Commit"/></description></item>
        /// </list>
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetmode.asp">MsiGetMode</a>
        /// </p></remarks>
        public bool GetMode(InstallRunMode mode)
        {
            return RemotableNativeMethods.MsiGetMode((int) this.Handle, (uint) mode);
        }

        /// <summary>
        /// Sets the designated mode flag for the current install session.
        /// </summary>
        /// <param name="mode">The type of mode to be set.</param>
        /// <param name="value">The desired value of the mode.</param>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="ArgumentOutOfRangeException">an invalid mode flag was specified</exception>
        /// <exception cref="InvalidOperationException">the mode cannot not be set</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisetmode.asp">MsiSetMode</a>
        /// </p></remarks>
        public void SetMode(InstallRunMode mode, bool value)
        {
            this.ValidateSessionAccess();

            uint ret = RemotableNativeMethods.MsiSetMode((int) this.Handle, (uint) mode, value);
            if (ret != 0)
            {
                if (ret == (uint) NativeMethods.Error.ACCESS_DENIED)
                {
                    throw new InvalidOperationException();
                }
                else
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }
        }

        /// <summary>
        /// Gets the full path to the designated folder on the source media or server image.
        /// </summary>
        /// <exception cref="ArgumentException">the folder was not found in the Directory table</exception>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetsourcepath.asp">MsiGetSourcePath</a>
        /// </p></remarks>
        public string GetSourcePath(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentNullException("directory");
            }

            this.ValidateSessionAccess();

            StringBuilder buf = new StringBuilder();
            uint bufSize = 0;
            uint ret = RemotableNativeMethods.MsiGetSourcePath((int) this.Handle, directory, buf, ref bufSize);
            if (ret == (uint) NativeMethods.Error.MORE_DATA)
            {
                buf.Capacity = (int) ++bufSize;
                ret = ret = RemotableNativeMethods.MsiGetSourcePath((int) this.Handle, directory, buf, ref bufSize);
            }

            if (ret != 0)
            {
                if (ret == (uint) NativeMethods.Error.DIRECTORY)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret, directory);
                }
                else
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }
            return buf.ToString();
        }

        /// <summary>
        /// Gets the full path to the designated folder on the installation target drive.
        /// </summary>
        /// <exception cref="ArgumentException">the folder was not found in the Directory table</exception>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigettargetpath.asp">MsiGetTargetPath</a>
        /// </p></remarks>
        public string GetTargetPath(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentNullException("directory");
            }

            this.ValidateSessionAccess();

            StringBuilder buf = new StringBuilder();
            uint bufSize = 0;
            uint ret = RemotableNativeMethods.MsiGetTargetPath((int) this.Handle, directory, buf, ref bufSize);
            if (ret == (uint) NativeMethods.Error.MORE_DATA)
            {
                buf.Capacity = (int) ++bufSize;
                ret = ret = RemotableNativeMethods.MsiGetTargetPath((int) this.Handle, directory, buf, ref bufSize);
            }

            if (ret != 0)
            {
                if (ret == (uint) NativeMethods.Error.DIRECTORY)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret, directory);
                }
                else
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }
            return buf.ToString();
        }

        /// <summary>
        /// Sets the full path to the designated folder on the installation target drive.
        /// </summary>
        /// <exception cref="ArgumentException">the folder was not found in the Directory table</exception>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <remarks><p>
        /// Setting the target path of a directory changes the path specification for the directory
        /// in the in-memory Directory table. Also, the path specifications of all other path objects
        /// in the table that are either subordinate or equivalent to the changed path are updated
        /// to reflect the change. The properties for each affected path are also updated.
        /// </p><p>
        /// If an error occurs in this function, all updated paths and properties revert to
        /// their previous values. Therefore, it is safe to treat errors returned by this function
        /// as non-fatal.
        /// </p><p>
        /// Do not attempt to configure the target path if the components using those paths
        /// are already installed for the current user or for a different user. Check the
        /// ProductState property before setting the target path to determine if the product
        /// containing this component is installed.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisettargetpath.asp">MsiSetTargetPath</a>
        /// </p></remarks>
        public void SetTargetPath(string directory, string value)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentNullException("directory");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            this.ValidateSessionAccess();

            uint ret = RemotableNativeMethods.MsiSetTargetPath((int) this.Handle, directory, value);
            if (ret != 0)
            {
                if (ret == (uint) NativeMethods.Error.DIRECTORY)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret, directory);
                }
                else
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }
        }

        /// <summary>
        /// Sets the install level for the current installation to a specified value and
        /// recalculates the Select and Installed states for all features in the Feature
        /// table. Also sets the Action state of each component in the Component table based
        /// on the new level.
        /// </summary>
        /// <param name="installLevel">New install level</param>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <remarks><p>
        /// The SetInstallLevel method sets the following:<list type="bullet">
        /// <item><description>The installation level for the current installation to a specified value</description></item>
        /// <item><description>The Select and Installed states for all features in the Feature table</description></item>
        /// <item><description>The Action state of each component in the Component table, based on the new level</description></item>
        /// </list>
        /// If 0 or a negative number is passed in the ilnstallLevel parameter,
        /// the current installation level does not change, but all features are still
        /// updated based on the current installation level.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisetinstalllevel.asp">MsiSetInstallLevel</a>
        /// </p></remarks>
        public void SetInstallLevel(int installLevel)
        {
            this.ValidateSessionAccess();

            uint ret = RemotableNativeMethods.MsiSetInstallLevel((int) this.Handle, installLevel);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Executes a built-in action, custom action, or user-interface wizard action.
        /// </summary>
        /// <param name="action">Name of the action to execute.  Case-sensitive.</param>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="InstallCanceledException">the user exited the installation</exception>
        /// <remarks><p>
        /// The DoAction method executes the action that corresponds to the name supplied. If the
        /// name is not recognized by the installer as a built-in action or as a custom action in
        /// the CustomAction table, the name is passed to the user-interface handler object, which
        /// can invoke a function or a dialog box. If a null action name is supplied, the installer
        /// uses the upper-case value of the ACTION property as the action to perform. If no property
        /// value is defined, the default action is performed, defined as "INSTALL".
        /// </p><p>
        /// Actions that update the system, such as the InstallFiles and WriteRegistryValues
        /// actions, cannot be run by calling MsiDoAction. The exception to this rule is if DoAction
        /// is called from a custom action that is scheduled in the InstallExecuteSequence table
        /// between the InstallInitialize and InstallFinalize actions. Actions that do not update the
        /// system, such as AppSearch or CostInitialize, can be called.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidoaction.asp">MsiDoAction</a>
        /// </p></remarks>
        public void DoAction(string action)
        {
            this.DoAction(action, null);
        }

        /// <summary>
        /// Executes a built-in action, custom action, or user-interface wizard action.
        /// </summary>
        /// <param name="action">Name of the action to execute.  Case-sensitive.</param>
        /// <param name="actionData">Optional data to be passed to a deferred custom action.</param>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="InstallCanceledException">the user exited the installation</exception>
        /// <remarks><p>
        /// The DoAction method executes the action that corresponds to the name supplied. If the
        /// name is not recognized by the installer as a built-in action or as a custom action in
        /// the CustomAction table, the name is passed to the user-interface handler object, which
        /// can invoke a function or a dialog box. If a null action name is supplied, the installer
        /// uses the upper-case value of the ACTION property as the action to perform. If no property
        /// value is defined, the default action is performed, defined as "INSTALL".
        /// </p><p>
        /// Actions that update the system, such as the InstallFiles and WriteRegistryValues
        /// actions, cannot be run by calling MsiDoAction. The exception to this rule is if DoAction
        /// is called from a custom action that is scheduled in the InstallExecuteSequence table
        /// between the InstallInitialize and InstallFinalize actions. Actions that do not update the
        /// system, such as AppSearch or CostInitialize, can be called.
        /// </p><p>
        /// If the called action is a deferred, rollback, or commit custom action, then the supplied
        /// <paramref name="actionData"/> will be available via the <see cref="CustomActionData"/>
        /// property of that custom action's session.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidoaction.asp">MsiDoAction</a>
        /// </p></remarks>
        public void DoAction(string action, CustomActionData actionData)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                throw new ArgumentNullException("action");
            }

            this.ValidateSessionAccess();

            if (actionData != null)
            {
                this[action] = actionData.ToString();
            }

            uint ret = RemotableNativeMethods.MsiDoAction((int) this.Handle, action);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Executes an action sequence described in the specified table.
        /// </summary>
        /// <param name="sequenceTable">Name of the table containing the action sequence.</param>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="InstallCanceledException">the user exited the installation</exception>
        /// <remarks><p>
        /// This method queries the specified table, ordering the actions by the numbers in the Sequence column.
        /// For each row retrieved, an action is executed, provided that any supplied condition expression does
        /// not evaluate to FALSE.
        /// </p><p>
        /// An action sequence containing any actions that update the system, such as the InstallFiles and
        /// WriteRegistryValues actions, cannot be run by calling DoActionSequence. The exception to this rule is if
        /// DoActionSequence is called from a custom action that is scheduled in the InstallExecuteSequence table
        /// between the InstallInitialize and InstallFinalize actions. Actions that do not update the system, such
        /// as AppSearch or CostInitialize, can be called.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisequence.asp">MsiSequence</a>
        /// </p></remarks>
        public void DoActionSequence(string sequenceTable)
        {
            if (string.IsNullOrWhiteSpace(sequenceTable))
            {
                throw new ArgumentNullException("sequenceTable");
            }

            this.ValidateSessionAccess();

            uint ret = RemotableNativeMethods.MsiSequence((int) this.Handle, sequenceTable, 0);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Gets custom action data for the session that was supplied by the caller.
        /// </summary>
        /// <seealso cref="DoAction(string,CustomActionData)"/>
        public CustomActionData CustomActionData
        {
            get
            {
                if (this.customActionData == null)
                {
                    this.customActionData = new CustomActionData(this[CustomActionData.PropertyName]);
                }

                return this.customActionData;
            }
        }

        /// <summary>
        /// Implements formatting for <see cref="Record" /> data.
        /// </summary>
        /// <param name="formatType">Type of format object to get.</param>
        /// <returns>The the current instance, if <paramref name="formatType"/> is the same type
        /// as the current instance; otherwise, null.</returns>
        object IFormatProvider.GetFormat(Type formatType)
        {
            return formatType == typeof(Session) ? this : null;
        }

        /// <summary>
        /// Closes the session handle.  Also closes the active database handle, if it is open.
        /// After closing a handle, further method calls may throw an <see cref="InvalidHandleException"/>.
        /// </summary>
        /// <param name="disposing">If true, the method has been called directly
        /// or indirectly by a user's code, so managed and unmanaged resources will
        /// be disposed. If false, only unmanaged resources will be disposed.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (this.database != null)
                    {
                        this.database.Dispose();
                        this.database = null;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Gets the (short) list of properties that are available from non-immediate custom actions.
        /// </summary>
        private static IList<string> NonImmediatePropertyNames
        {
            get
            {
                return new string[] {
                    CustomActionData.PropertyName,
                    "ProductCode",
                    "UserSID"
                };
            }
        }

        /// <summary>
        /// Throws an exception if the custom action is not able to access immediate session details.
        /// </summary>
        private void ValidateSessionAccess()
        {
            if (!this.sessionAccessValidated)
            {
                if (this.GetMode(InstallRunMode.Scheduled) ||
                    this.GetMode(InstallRunMode.Rollback) ||
                    this.GetMode(InstallRunMode.Commit))
                {
                    throw new InstallerException("Cannot access session details from a non-immediate custom action");
                }

                this.sessionAccessValidated = true;
            }
        }
    }
}
