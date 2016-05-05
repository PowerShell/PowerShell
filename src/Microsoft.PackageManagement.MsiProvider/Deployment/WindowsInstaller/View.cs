//---------------------------------------------------------------------
// <copyright file="View.cs" company="Microsoft Corporation">
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
    /// A View represents a result set obtained when processing a query using the
    /// <see cref="WindowsInstaller.Database.OpenView"/> method of a
    /// <see cref="Database"/>. Before any data can be transferred,
    /// the query must be executed using the <see cref="Execute(Record)"/> method, passing to
    /// it all replaceable parameters designated within the SQL query string.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    internal class View : InstallerHandle, IEnumerable<Record>
    {
        private Database database;
        private string sql;
        private IList<TableInfo> tables;
        private ColumnCollection columns;

        internal View(IntPtr handle, string sql, Database database)
            : base(handle, true)
        {
            this.sql = sql;
            this.database = database;
        }

        /// <summary>
        /// Gets the Database on which this View was opened.
        /// </summary>
        public Database Database
        {
            get { return this.database; }
        }

        /// <summary>
        /// Gets the SQL query string used to open this View.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string QueryString
        {
            get { return this.sql; }
        }

        /// <summary>
        /// Gets the set of tables that were included in the SQL query for this View.
        /// </summary>
        public IList<TableInfo> Tables
        {
            get
            {
                if (this.tables == null)
                {
                    if (this.sql == null)
                    {
                        return null;
                    }

                    // Parse the table names out of the SQL query string by looking
                    // for tokens that can come before or after the list of tables.

                    string parseSql = this.sql.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
                    string upperSql = parseSql.ToUpper(CultureInfo.InvariantCulture);

                    string[] prefixes = new string[] { " FROM ", " INTO ", " TABLE " };
                    string[] suffixes = new string[] { " WHERE ", " ORDER ", " SET ", " (", " ADD " };

                    int index;

                    for (int i = 0; i < prefixes.Length; i++)
                    {
                        if ((index = upperSql.IndexOf(prefixes[i], StringComparison.Ordinal)) > 0)
                        {
                            parseSql = parseSql.Substring(index + prefixes[i].Length);
                            upperSql = upperSql.Substring(index + prefixes[i].Length);
                        }
                    }

                    if (upperSql.StartsWith("UPDATE ", StringComparison.Ordinal))
                    {
                        parseSql = parseSql.Substring(7);
                        upperSql = upperSql.Substring(7);
                    }

                    for (int i = 0; i < suffixes.Length; i++)
                    {
                        if ((index = upperSql.IndexOf(suffixes[i], StringComparison.Ordinal)) > 0)
                        {
                            parseSql = parseSql.Substring(0, index);
                            upperSql = upperSql.Substring(0, index);
                        }
                    }

                    if (upperSql.EndsWith(" HOLD", StringComparison.Ordinal) ||
                        upperSql.EndsWith(" FREE", StringComparison.Ordinal))
                    {
                        parseSql = parseSql.Substring(0, parseSql.Length - 5);
                        upperSql = upperSql.Substring(0, upperSql.Length - 5);
                    }

                    // At this point we should be left with a comma-separated list of table names,
                    // optionally quoted with grave accent marks (`).

                    string[] tableNames = parseSql.Split(',');
                    IList<TableInfo> tableList = new List<TableInfo>(tableNames.Length);
                    for (int i = 0; i < tableNames.Length; i++)
                    {
                        string tableName = tableNames[i].Trim(' ', '`');
                        tableList.Add(new TableInfo(this.database, tableName));
                    }
                    this.tables = tableList;
                }
                return new List<TableInfo>(this.tables);
            }
        }

        /// <summary>
        /// Gets the set of columns that were included in the query for this View,
        /// or null if this view is not a SELECT query.
        /// </summary>
        /// <exception cref="InstallerException">the View is not in an active state</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewgetcolumninfo.asp">MsiViewGetColumnInfo</a>
        /// </p></remarks>
        public ColumnCollection Columns
        {
            get
            {
                if (this.columns == null)
                {
                    this.columns = new ColumnCollection(this);
                }
                return this.columns;
            }
        }

        /// <summary>
        /// Executes a SQL View query and supplies any required parameters. The query uses the
        /// question mark token to represent parameters as described in SQL Syntax. The values of
        /// these parameters are passed in as the corresponding fields of a parameter record.
        /// </summary>
        /// <param name="executeParams">Optional Record that supplies the parameters. This
        /// Record contains values to replace the parameter tokens in the SQL query.</param>
        /// <exception cref="InstallerException">the View could not be executed</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewexecute.asp">MsiViewExecute</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Params"), SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void Execute(Record executeParams)
        {
            uint ret = RemotableNativeMethods.MsiViewExecute(
                (int) this.Handle,
                (executeParams != null ? (int) executeParams.Handle : 0));
            if (ret == (uint) NativeMethods.Error.BAD_QUERY_SYNTAX)
            {
                throw new BadQuerySyntaxException(this.sql);
            }
            else if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Executes a SQL View query.
        /// </summary>
        /// <exception cref="InstallerException">the View could not be executed</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewexecute.asp">MsiViewExecute</a>
        /// </p></remarks>
        public void Execute() { this.Execute(null); }

        /// <summary>
        /// Fetches the next sequential record from the view, or null if there are no more records.
        /// </summary>
        /// <exception cref="InstallerException">the View is not in an active state</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// The Record object should be <see cref="InstallerHandle.Close"/>d after use.
        /// It is best that the handle be closed manually as soon as it is no longer
        /// needed, as leaving lots of unused handles open can degrade performance.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewfetch.asp">MsiViewFetch</a>
        /// </p></remarks>
        public Record Fetch()
        {
            int recordHandle;
            uint ret = RemotableNativeMethods.MsiViewFetch((int) this.Handle, out recordHandle);
            if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS)
            {
                return null;
            }
            else if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }

            Record r = new Record((IntPtr) recordHandle, true, this);
            r.IsFormatStringInvalid = true;
            return r;
        }

        /// <summary>
        /// Updates a fetched Record.
        /// </summary>
        /// <param name="mode">specifies the modify mode</param>
        /// <param name="record">the Record to modify</param>
        /// <exception cref="InstallerException">the modification failed,
        /// or a validation was requested and the data did not pass</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// You can update or delete a record immediately after inserting, or seeking provided you
        /// have NOT modified the 0th field of the inserted or sought record.
        /// </p><p>
        /// To execute any SQL statement, a View must be created. However, a View that does not
        /// create a result set, such as CREATE TABLE, or INSERT INTO, cannot be used with any of
        /// the Modify methods to update tables though the view.
        /// </p><p>
        /// You cannot fetch a record containing binary data from one database and then use
        /// that record to insert the data into another database. To move binary data from one database
        /// to another, you should export the data to a file and then import it into the new database
        /// using a query and the <see cref="Record.SetStream(int,string)"/>. This ensures that each database has
        /// its own copy of the binary data.
        /// </p><p>
        /// Note that custom actions can only add, modify, or remove temporary rows, columns,
        /// or tables from a database. Custom actions cannot modify persistent data in a database,
        /// such as data that is a part of the database stored on disk.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>
        /// </p></remarks>
        /// <seealso cref="Refresh"/>
        /// <seealso cref="Insert"/>
        /// <seealso cref="Update"/>
        /// <seealso cref="Assign"/>
        /// <seealso cref="Replace"/>
        /// <seealso cref="Delete"/>
        /// <seealso cref="InsertTemporary"/>
        /// <seealso cref="Seek"/>
        /// <seealso cref="Merge"/>
        /// <seealso cref="Validate"/>
        /// <seealso cref="ValidateNew"/>
        /// <seealso cref="ValidateFields"/>
        /// <seealso cref="ValidateDelete"/>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Modify(ViewModifyMode mode, Record record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            uint ret = RemotableNativeMethods.MsiViewModify((int) this.Handle, (int) mode, (int) record.Handle);
            if (mode == ViewModifyMode.Insert || mode == ViewModifyMode.InsertTemporary)
            {
                record.IsFormatStringInvalid = true;
            }
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Refreshes the data in a Record.
        /// </summary>
        /// <param name="record">the Record to be refreshed</param>
        /// <exception cref="InstallerException">the refresh failed</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// The Record must have been obtained by calling <see cref="Fetch"/>. Fails with
        /// a deleted Record. Works only with read-write Records.
        /// </p><p>
        /// See <see cref="Modify"/> for more remarks.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Refresh(Record record) { this.Modify(ViewModifyMode.Refresh, record); }

        /// <summary>
        /// Inserts a Record into the view.
        /// </summary>
        /// <param name="record">the Record to be inserted</param>
        /// <exception cref="InstallerException">the insertion failed</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// Fails if a row with the same primary keys exists. Fails with a read-only database.
        /// This method cannot be used with a View containing joins.
        /// </p><p>
        /// See <see cref="Modify"/> for more remarks.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Insert(Record record) { this.Modify(ViewModifyMode.Insert, record); }

        /// <summary>
        /// Updates the View with new data from the Record.
        /// </summary>
        /// <param name="record">the new data</param>
        /// <exception cref="InstallerException">the update failed</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// Only non-primary keys can be updated. The Record must have been obtained by calling
        /// <see cref="Fetch"/>. Fails with a deleted Record. Works only with read-write Records.
        /// </p><p>
        /// See <see cref="Modify"/> for more remarks.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Update(Record record) { this.Modify(ViewModifyMode.Update, record); }

        /// <summary>
        /// Updates or inserts a Record into the View.
        /// </summary>
        /// <param name="record">the Record to be assigned</param>
        /// <exception cref="InstallerException">the assignment failed</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// Updates record if the primary keys match an existing row and inserts if they do not match.
        /// Fails with a read-only database. This method cannot be used with a View containing joins.
        /// </p><p>
        /// See <see cref="Modify"/> for more remarks.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Assign(Record record) { this.Modify(ViewModifyMode.Assign, record); }

        /// <summary>
        /// Updates or deletes and inserts a Record into the View.
        /// </summary>
        /// <param name="record">the Record to be replaced</param>
        /// <exception cref="InstallerException">the replacement failed</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// The Record must have been obtained by calling <see cref="Fetch"/>. Updates record if the
        /// primary keys are unchanged. Deletes old row and inserts new if primary keys have changed.
        /// Fails with a read-only database. This method cannot be used with a View containing joins.
        /// </p><p>
        /// See <see cref="Modify"/> for more remarks.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Replace(Record record) { this.Modify(ViewModifyMode.Replace, record); }

        /// <summary>
        /// Deletes a Record from the View.
        /// </summary>
        /// <param name="record">the Record to be deleted</param>
        /// <exception cref="InstallerException">the deletion failed</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// The Record must have been obtained by calling <see cref="Fetch"/>. Fails if the row has been
        /// deleted. Works only with read-write records. This method cannot be used with a View containing joins.
        /// </p><p>
        /// See <see cref="Modify"/> for more remarks.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Delete(Record record) { this.Modify(ViewModifyMode.Delete, record); }

        /// <summary>
        /// Inserts a Record into the View.  The inserted data is not persistent.
        /// </summary>
        /// <param name="record">the Record to be inserted</param>
        /// <exception cref="InstallerException">the insertion failed</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// Fails if a row with the same primary key exists. Works only with read-write records.
        /// This method cannot be used with a View containing joins.
        /// </p><p>
        /// See <see cref="Modify"/> for more remarks.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void InsertTemporary(Record record) { this.Modify(ViewModifyMode.InsertTemporary, record); }

        /// <summary>
        /// Refreshes the information in the supplied record without changing the position
        /// in the result set and without affecting subsequent fetch operations.
        /// </summary>
        /// <param name="record">the Record to be filled with the result of the seek</param>
        /// <exception cref="InstallerException">the seek failed</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// After seeking, the Record may then be used for subsequent Update, Delete, and Refresh
        /// operations.  All primary key columns of the table must be in the query and the Record must
        /// have at least as many fields as the query. Seek cannot be used with multi-table queries.
        /// This method cannot be used with a View containing joins.
        /// </p><p>
        /// See <see cref="Modify"/> for more remarks.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool Seek(Record record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            uint ret = RemotableNativeMethods.MsiViewModify((int) this.Handle, (int) ViewModifyMode.Seek, (int) record.Handle);
            record.IsFormatStringInvalid = true;
            if (ret == (uint) NativeMethods.Error.FUNCTION_FAILED)
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
        /// Inserts or validates a record.
        /// </summary>
        /// <param name="record">the Record to be merged</param>
        /// <returns>true if the record was inserted or validated, false if there is an existing
        /// record with the same primary keys that is not identical</returns>
        /// <exception cref="InstallerException">the merge failed (for a reason other than invalid data)</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// Works only with read-write records. This method cannot be used with a
        /// View containing joins.
        /// </p><p>
        /// See <see cref="Modify"/> for more remarks.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool Merge(Record record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            uint ret = RemotableNativeMethods.MsiViewModify((int) this.Handle, (int) ViewModifyMode.Merge, (int) record.Handle);
            if (ret == (uint) NativeMethods.Error.FUNCTION_FAILED)
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
        /// Validates a record, returning information about any errors.
        /// </summary>
        /// <param name="record">the Record to be validated</param>
        /// <returns>null if the record was validated; if there is an existing record with
        /// the same primary keys that has conflicting data then error information is returned</returns>
        /// <exception cref="InstallerException">the validation failed (for a reason other than invalid data)</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// The Record must have been obtained by calling <see cref="Fetch"/>.
        /// Works with read-write and read-only records. This method cannot be used
        /// with a View containing joins.
        /// </p><p>
        /// See <see cref="Modify"/> for more remarks.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewgeterror.asp">MsiViewGetError</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ICollection<ValidationErrorInfo> Validate(Record record) { return this.InternalValidate(ViewModifyMode.Validate, record); }

        /// <summary>
        /// Validates a new record, returning information about any errors.
        /// </summary>
        /// <param name="record">the Record to be validated</param>
        /// <returns>null if the record was validated; if there is an existing
        /// record with the same primary keys then error information is returned</returns>
        /// <exception cref="InstallerException">the validation failed (for a reason other than invalid data)</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// Checks for duplicate keys. The Record must have been obtained by
        /// calling <see cref="Fetch"/>. Works with read-write and read-only records.
        /// This method cannot be used with a View containing joins.
        /// </p><p>
        /// See <see cref="Modify"/> for more remarks.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewgeterror.asp">MsiViewGetError</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ICollection<ValidationErrorInfo> ValidateNew(Record record) { return this.InternalValidate(ViewModifyMode.ValidateNew, record); }

        /// <summary>
        /// Validates fields of a fetched or new record, returning information about any errors.
        /// Can validate one or more fields of an incomplete record.
        /// </summary>
        /// <param name="record">the Record to be validated</param>
        /// <returns>null if the record was validated; if there is an existing record with
        /// the same primary keys that has conflicting data then error information is returned</returns>
        /// <exception cref="InstallerException">the validation failed (for a reason other than invalid data)</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// Works with read-write and read-only records. This method cannot be used with
        /// a View containing joins.
        /// </p><p>
        /// See <see cref="Modify"/> for more remarks.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewgeterror.asp">MsiViewGetError</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ICollection<ValidationErrorInfo> ValidateFields(Record record) { return this.InternalValidate(ViewModifyMode.ValidateField, record); }

        /// <summary>
        /// Validates a record that will be deleted later, returning information about any errors.
        /// </summary>
        /// <param name="record">the Record to be validated</param>
        /// <returns>null if the record is safe to delete; if another row refers to
        /// the primary keys of this row then error information is returned</returns>
        /// <exception cref="InstallerException">the validation failed (for a reason other than invalid data)</exception>
        /// <exception cref="InvalidHandleException">the View handle is invalid</exception>
        /// <remarks><p>
        /// Validation does not check for the existence of the primary keys of this row in properties
        /// or strings. Does not check if a column is a foreign key to multiple tables. Works with
        /// read-write and read-only records. This method cannot be used with a View containing joins.
        /// </p><p>
        /// See <see cref="Modify"/> for more remarks.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewmodify.asp">MsiViewModify</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewgeterror.asp">MsiViewGetError</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ICollection<ValidationErrorInfo> ValidateDelete(Record record) { return this.InternalValidate(ViewModifyMode.ValidateDelete, record); }

        /// <summary>
        /// Enumerates over the Records retrieved by the View.
        /// </summary>
        /// <returns>An enumerator of Record objects.</returns>
        /// <exception cref="InstallerException">The View was not <see cref="Execute(Record)"/>d before attempting the enumeration.</exception>
        /// <remarks><p>
        /// Each Record object should be <see cref="InstallerHandle.Close"/>d after use.
        /// It is best that the handle be closed manually as soon as it is no longer
        /// needed, as leaving lots of unused handles open can degrade performance.
        /// However, note that it is not necessary to complete the enumeration just
        /// for the purpose of closing handles, because Records are fetched lazily
        /// on each step of the enumeration.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewfetch.asp">MsiViewFetch</a>
        /// </p></remarks>
        public IEnumerator<Record> GetEnumerator()
        {
            Record rec;
            while ((rec = this.Fetch()) != null)
            {
                yield return rec;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private ICollection<ValidationErrorInfo> InternalValidate(ViewModifyMode mode, Record record)
        {
            uint ret = RemotableNativeMethods.MsiViewModify((int) this.Handle, (int) mode, (int) record.Handle);
            if (ret == (uint) NativeMethods.Error.INVALID_DATA)
            {
                ICollection<ValidationErrorInfo> errorInfo = new List<ValidationErrorInfo>();
                while (true)
                {
                    uint bufSize = 40;
                    StringBuilder column = new StringBuilder("", (int) bufSize);
                    int error = RemotableNativeMethods.MsiViewGetError((int) this.Handle, column, ref bufSize);
                    if (error == -2 /*MSIDBERROR_MOREDATA*/)
                    {
                        column.Capacity = (int) ++bufSize;
                        error = RemotableNativeMethods.MsiViewGetError((int) this.Handle, column, ref bufSize);
                    }

                    if (error == -3 /*MSIDBERROR_INVALIDARG*/)
                    {
                        throw InstallerException.ExceptionFromReturnCode((uint) NativeMethods.Error.INVALID_PARAMETER);
                    }
                    else if (error == 0 /*MSIDBERROR_NOERROR*/)
                    {
                        break;
                    }

                    errorInfo.Add(new ValidationErrorInfo((ValidationError) error, column.ToString()));
                }

                return errorInfo;
            }
            else if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
            return null;
        }
    }
}
