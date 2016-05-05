//---------------------------------------------------------------------
// <copyright file="Database.cs" company="Microsoft Corporation">
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
    using System.IO;

    /// <summary>
    /// Accesses a Windows Installer database.
    /// </summary>
    /// <remarks><p>
    /// The <see cref="Commit"/> method must be called before the Database is closed to write out all
    /// persistent changes. If the Commit method is not called, the installer performs an implicit
    /// rollback upon object destruction.
    /// </p><p>
    /// The client can use the following procedure for data access:<list type="number">
    /// <item><description>Obtain a Database object using one of the Database constructors.</description></item>
    /// <item><description>Initiate a query using a SQL string by calling the <see cref="OpenView"/>
    ///		method of the Database.</description></item>
    /// <item><description>Set query parameters in a <see cref="Record"/> and execute the database
    ///		query by calling the <see cref="View.Execute(Record)"/> method of the <see cref="View"/>. This
    ///		produces a result that can be fetched or updated.</description></item>
    /// <item><description>Call the <see cref="View.Fetch"/> method of the View repeatedly to return
    ///		Records.</description></item>
    /// <item><description>Update database rows of a Record object obtained by the Fetch method using
    ///		one of the <see cref="View.Modify"/> methods of the View.</description></item>
    /// <item><description>Release the query and any unfetched records by calling the <see cref="InstallerHandle.Close"/>
    ///		method of the View.</description></item>
    /// <item><description>Persist any database updates by calling the Commit method of the Database.
    ///		</description></item>
    /// </list>
    /// </p></remarks>
    internal partial class Database : InstallerHandle
    {
        private string filePath;
        private DatabaseOpenMode openMode;
        private SummaryInfo summaryInfo;
        private TableCollection tables;
        private IList<string> deleteOnClose;

        /// <summary>
        /// Opens an existing database in read-only mode.
        /// </summary>
        /// <param name="filePath">Path to the database file.</param>
        /// <exception cref="InstallerException">the database could not be created/opened</exception>
        /// <remarks><p>
        /// Because this constructor initiates database access, it cannot be used with a
        /// running installation.
        /// </p><p>
        /// The Database object should be <see cref="InstallerHandle.Close"/>d after use.
        /// It is best that the handle be closed manually as soon as it is no longer
        /// needed, as leaving lots of unused handles open can degrade performance.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiopendatabase.asp">MsiOpenDatabase</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Database(string filePath)
            : this(filePath, DatabaseOpenMode.ReadOnly)
        {
        }

        /// <summary>
        /// Opens an existing database with another database as output.
        /// </summary>
        /// <param name="filePath">Path to the database to be read.</param>
        /// <param name="outputPath">Open mode for the database</param>
        /// <returns>Database object representing the created or opened database</returns>
        /// <exception cref="InstallerException">the database could not be created/opened</exception>
        /// <remarks><p>
        /// When a database is opened as the output of another database, the summary information stream
        /// of the output database is actually a read-only mirror of the original database and thus cannot
        /// be changed. Additionally, it is not persisted with the database. To create or modify the
        /// summary information for the output database it must be closed and re-opened.
        /// </p><p>
        /// The Database object should be <see cref="InstallerHandle.Close"/>d after use.
        /// It is best that the handle be closed manually as soon as it is no longer
        /// needed, as leaving lots of unused handles open can degrade performance.
        /// </p><p>
        /// The database is opened in <see cref="DatabaseOpenMode.CreateDirect" /> mode, and will be
        /// automatically commited when it is closed.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiopendatabase.asp">MsiOpenDatabase</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Database(string filePath, string outputPath)
            : this((IntPtr) Database.Open(filePath, outputPath), true, outputPath, DatabaseOpenMode.CreateDirect)
        {
        }

        /// <summary>
        /// Opens an existing database or creates a new one.
        /// </summary>
        /// <param name="filePath">Path to the database file. If an empty string
        /// is supplied, a temporary database is created that is not persisted.</param>
        /// <param name="mode">Open mode for the database</param>
        /// <exception cref="InstallerException">the database could not be created/opened</exception>
        /// <remarks><p>
        /// Because this constructor initiates database access, it cannot be used with a
        /// running installation.
        /// </p><p>
        /// The database object should be <see cref="InstallerHandle.Close"/>d after use.
        /// The finalizer will close the handle if it is still open, however due to the nondeterministic
        /// nature of finalization it is best that the handle be closed manually as soon as it is no
        /// longer needed, as leaving lots of unused handles open can degrade performance.
        /// </p><p>
        /// A database opened in <see cref="DatabaseOpenMode.CreateDirect" /> or
        /// <see cref="DatabaseOpenMode.Direct" /> mode will be automatically commited when it is
        /// closed. However a database opened in <see cref="DatabaseOpenMode.Create" /> or
        /// <see cref="DatabaseOpenMode.Transact" /> mode must have the <see cref="Commit" /> method
        /// called before it is closed, otherwise no changes will be persisted.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiopendatabase.asp">MsiOpenDatabase</a>
        /// </p></remarks>
        public Database(string filePath, DatabaseOpenMode mode)
            : this((IntPtr) Database.Open(filePath, mode), true, filePath, mode)
        {
        }

        /// <summary>
        /// Creates a new database from an MSI handle.
        /// </summary>
        /// <param name="handle">Native MSI database handle.</param>
        /// <param name="ownsHandle">True if the handle should be closed
        /// when the database object is disposed</param>
        /// <param name="filePath">Path of the database file, if known</param>
        /// <param name="openMode">Mode the handle was originally opened in</param>
        protected internal Database(
            IntPtr handle, bool ownsHandle, string filePath, DatabaseOpenMode openMode)
            : base(handle, ownsHandle)
        {
            this.filePath = filePath;
            this.openMode = openMode;
        }

        /// <summary>
        /// Gets the file path the Database was originally opened from, or null if not known.
        /// </summary>
        public String FilePath
        {
            get
            {
                return this.filePath;
            }
        }

        /// <summary>
        /// Gets the open mode for the database.
        /// </summary>
        public DatabaseOpenMode OpenMode
        {
            get
            {
                return this.openMode;
            }
        }

        /// <summary>
        /// Gets a boolean value indicating whether this database was opened in read-only mode.
        /// </summary>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetdatabasestate.asp">MsiGetDatabaseState</a>
        /// </p></remarks>
        public bool IsReadOnly
        {
            get
            {
                if (RemotableNativeMethods.RemotingEnabled)
                {
                    return true;
                }

                int state = NativeMethods.MsiGetDatabaseState((int) this.Handle);
                return state != 1;
            }
        }

        /// <summary>
        /// Gets the collection of tables in the Database.
        /// </summary>
        public TableCollection Tables
        {
            get
            {
                if (this.tables == null)
                {
                    this.tables = new TableCollection(this);
                }
                return this.tables;
            }
        }

        /// <summary>
        /// Gets or sets the code page of the Database.
        /// </summary>
        /// <exception cref="IOException">error exporting/importing the codepage data</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// Getting or setting the code page is a slow operation because it involves an export or import
        /// of the codepage data to/from a temporary file.
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int CodePage
        {
            get
            {
                string tempFile = Path.GetTempFileName();
                StreamReader reader = null;
                try
                {
                    this.Export("_ForceCodepage", tempFile);
                    reader = File.OpenText(tempFile);
                    reader.ReadLine();  // Skip column name record.
                    reader.ReadLine();  // Skip column defn record.
                    string codePageLine = reader.ReadLine();
                    return Int32.Parse(codePageLine.Split('\t')[0], CultureInfo.InvariantCulture.NumberFormat);
                }
                finally
                {
                    if (reader != null) reader.Close();
                    File.Delete(tempFile);
                }
            }

            set
            {
                string tempFile = Path.GetTempFileName();
                StreamWriter writer = null;
                try
                {
                    writer = File.AppendText(tempFile);
                    writer.WriteLine("");
                    writer.WriteLine("");
                    writer.WriteLine("{0}\t_ForceCodepage", value);
                    writer.Close();
                    writer = null;
                    this.Import(tempFile);
                }
                finally
                {
                    if (writer != null) writer.Close();
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Gets the SummaryInfo object for this database that can be used to examine and modify properties
        /// to the summary information stream.
        /// </summary>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// The object returned from this property does not need to be explicitly persisted or closed.
        /// Any modifications will be automatically saved when the database is committed.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetsummaryinformation.asp">MsiGetSummaryInformation</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public SummaryInfo SummaryInfo
        {
            get
            {
                if (this.summaryInfo == null || this.summaryInfo.IsClosed)
                {
                    lock (this.Sync)
                    {
                        if (this.summaryInfo == null || this.summaryInfo.IsClosed)
                        {
                            int summaryInfoHandle;
                            int maxProperties = this.IsReadOnly ? 0 : SummaryInfo.MAX_PROPERTIES;
                            uint ret = RemotableNativeMethods.MsiGetSummaryInformation((int) this.Handle, null, (uint) maxProperties, out summaryInfoHandle);
                            if (ret != 0)
                            {
                                throw InstallerException.ExceptionFromReturnCode(ret);
                            }
                            this.summaryInfo = new SummaryInfo((IntPtr) summaryInfoHandle, true);
                        }
                    }
                }
                return this.summaryInfo;
            }
        }

        /// <summary>
        /// Creates a new Database object from an integer database handle.
        /// </summary>
        /// <remarks><p>
        /// This method is only provided for interop purposes.  A Database object
        /// should normally be obtained from <see cref="Session.Database"/> or
        /// a public Database constructor.
        /// </p></remarks>
        /// <param name="handle">Integer database handle</param>
        /// <param name="ownsHandle">true to close the handle when this object is disposed</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static Database FromHandle(IntPtr handle, bool ownsHandle)
        {
            return new Database(
                handle,
                ownsHandle,
                null,
                NativeMethods.MsiGetDatabaseState((int) handle) == 1 ? DatabaseOpenMode.Direct : DatabaseOpenMode.ReadOnly);
        }

        /// <summary>
        /// Schedules a file or directory for deletion after the database handle is closed.
        /// </summary>
        /// <param name="path">File or directory path to be deleted. All files and subdirectories
        /// under a directory are deleted.</param>
        /// <remarks><p>
        /// Once an item is scheduled, it cannot be unscheduled.
        /// </p><p>
        /// The items cannot be deleted if the Database object is auto-disposed by the
        /// garbage collector; the handle must be explicitly closed.
        /// </p><p>
        /// Files which are read-only or otherwise locked cannot be deleted,
        /// but they will not cause an exception to be thrown.
        /// </p></remarks>
        public void DeleteOnClose(string path)
        {
            if (this.deleteOnClose == null)
            {
                this.deleteOnClose = new List<string>();
            }
            this.deleteOnClose.Add(path);
        }

        /// <summary>
        /// Merges another database with this database.
        /// </summary>
        /// <param name="otherDatabase">The database to be merged into this database</param>
        /// <param name="errorTable">Optional name of table to contain the names of the tables containing
        /// merge conflicts, the number of conflicting rows within the table, and a reference to the table
        /// with the merge conflict.</param>
        /// <exception cref="MergeException">merge failed due to a schema difference or data conflict</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// Merge does not copy over embedded cabinet files or embedded transforms from the
        /// reference database into the target database. Embedded data streams that are listed in the
        /// Binary table or Icon table are copied from the reference database to the target database.
        /// Storage embedded in the reference database are not copied to the target database.
        /// </p><p>
        /// The Merge method merges the data of two databases. These databases must have the same
        /// codepage. The merge fails if any tables or rows in the databases conflict. A conflict exists
        /// if the data in any row in the first database differs from the data in the corresponding row
        /// of the second database. Corresponding rows are in the same table of both databases and have
        /// the same primary key in both databases. The tables of non-conflicting databases must have
        /// the same number of primary keys, same number of columns, same column types, same column names,
        /// and the same data in rows with identical primary keys. Temporary columns however don't matter
        /// in the column count and corresponding tables can have a different number of temporary columns
        /// without creating conflict as long as the persistent columns match.
        /// </p><p>
        /// If the number, type, or name of columns in corresponding tables are different, the
        /// schema of the two databases are incompatible and the installer will stop processing tables
        /// and the merge fails. The installer checks that the two databases have the same schema before
        /// checking for row merge conflicts. If the schemas are incompatible, the databases have be
        /// modified.
        /// </p><p>
        /// If the data in particular rows differ, this is a row merge conflict, the merge fails
        /// and creates a new table with the specified name. The first column of this table is the name
        /// of the table having the conflict. The second column gives the number of rows in the table
        /// having the conflict.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabasemerge.asp">MsiDatabaseMerge</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Merge(Database otherDatabase, string errorTable)
        {
            if (otherDatabase == null)
            {
                throw new ArgumentNullException("otherDatabase");
            }

            uint ret = NativeMethods.MsiDatabaseMerge((int) this.Handle, (int) otherDatabase.Handle, errorTable);
            if (ret != 0)
            {
                if (ret == (uint) NativeMethods.Error.FUNCTION_FAILED)
                {
                    throw new MergeException(this, errorTable);
                }
                else if (ret == (uint) NativeMethods.Error.DATATYPE_MISMATCH)
                {
                    throw new MergeException("Schema difference between the two databases.");
                }
                else
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }
        }

        /// <summary>
        /// Merges another database with this database.
        /// </summary>
        /// <param name="otherDatabase">The database to be merged into this database</param>
        /// <exception cref="MergeException">merge failed due to a schema difference or data conflict</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// MsiDatabaseMerge does not copy over embedded cabinet files or embedded transforms from
        /// the reference database into the target database. Embedded data streams that are listed in
        /// the Binary table or Icon table are copied from the reference database to the target database.
        /// Storage embedded in the reference database are not copied to the target database.
        /// </p><p>
        /// The Merge method merges the data of two databases. These databases must have the same
        /// codepage. The merge fails if any tables or rows in the databases conflict. A conflict exists
        /// if the data in any row in the first database differs from the data in the corresponding row
        /// of the second database. Corresponding rows are in the same table of both databases and have
        /// the same primary key in both databases. The tables of non-conflicting databases must have
        /// the same number of primary keys, same number of columns, same column types, same column names,
        /// and the same data in rows with identical primary keys. Temporary columns however don't matter
        /// in the column count and corresponding tables can have a different number of temporary columns
        /// without creating conflict as long as the persistent columns match.
        /// </p><p>
        /// If the number, type, or name of columns in corresponding tables are different, the
        /// schema of the two databases are incompatible and the installer will stop processing tables
        /// and the merge fails. The installer checks that the two databases have the same schema before
        /// checking for row merge conflicts. If the schemas are incompatible, the databases have be
        /// modified.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabasemerge.asp">MsiDatabaseMerge</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Merge(Database otherDatabase) { this.Merge(otherDatabase, null); }

        /// <summary>
        /// Checks whether a table exists and is persistent in the database.
        /// </summary>
        /// <param name="table">The table to the checked</param>
        /// <returns>true if the table exists and is persistent in the database; false otherwise</returns>
        /// <exception cref="ArgumentException">the table is unknown</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// To check whether a table exists regardless of persistence,
        /// use <see cref="TableCollection.Contains"/>.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseistablepersistent.asp">MsiDatabaseIsTablePersistent</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsTablePersistent(string table)
        {
            if (string.IsNullOrWhiteSpace(table))
            {
                throw new ArgumentNullException("table");
            }
            uint ret = RemotableNativeMethods.MsiDatabaseIsTablePersistent((int) this.Handle, table);
            if (ret == 3)  // MSICONDITION_ERROR
            {
                throw new InstallerException();
            }
            return ret == 1;
        }

        /// <summary>
        /// Checks whether a table contains a persistent column with a given name.
        /// </summary>
        /// <param name="table">The table to the checked</param>
        /// <param name="column">The name of the column to be checked</param>
        /// <returns>true if the column exists in the table; false if the column is temporary or does not exist.</returns>
        /// <exception cref="InstallerException">the View could not be executed</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// To check whether a column exists regardless of persistence,
        /// use <see cref="ColumnCollection.Contains"/>.
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsColumnPersistent(string table, string column)
        {
            if (string.IsNullOrWhiteSpace(table))
            {
                throw new ArgumentNullException("table");
            }
            if (string.IsNullOrWhiteSpace(column))
            {
                throw new ArgumentNullException("column");
            }
            using (View view = this.OpenView(
                "SELECT `Number` FROM `_Columns` WHERE `Table` = '{0}' AND `Name` = '{1}'", table, column))
            {
                view.Execute();
                using (Record rec = view.Fetch())
                {
                    return (rec != null);
                }
            }
        }

        /// <summary>
        /// Gets the count of all rows in the table.
        /// </summary>
        /// <param name="table">Name of the table whose rows are to be counted</param>
        /// <returns>The count of all rows in the table</returns>
        /// <exception cref="InstallerException">the View could not be executed</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        public int CountRows(string table)
        {
            return this.CountRows(table, null);
        }

        /// <summary>
        /// Gets the count of all rows in the table that satisfy a given condition.
        /// </summary>
        /// <param name="table">Name of the table whose rows are to be counted</param>
        /// <param name="where">Conditional expression, such as could be placed on the end of a SQL WHERE clause</param>
        /// <returns>The count of all rows in the table satisfying the condition</returns>
        /// <exception cref="BadQuerySyntaxException">the SQL WHERE syntax is invalid</exception>
        /// <exception cref="InstallerException">the View could not be executed</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        public int CountRows(string table, string where)
        {
            if (string.IsNullOrWhiteSpace(table))
            {
                throw new ArgumentNullException("table");
            }

            int count;
            using (View view = this.OpenView(
                "SELECT `{0}` FROM `{1}`{2}",
                this.Tables[table].PrimaryKeys[0],
                table,
                (where != null && where.Length != 0 ? " WHERE " + where : "")))
            {
                view.Execute();
                for (count = 0; ; count++)
                {
                    // Avoid creating unnecessary Record objects by not calling View.Fetch().
                    int recordHandle;
                    uint ret = RemotableNativeMethods.MsiViewFetch((int) view.Handle, out recordHandle);
                    if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS)
                    {
                        break;
                    }

                    if (ret != 0)
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret);
                    }

                    RemotableNativeMethods.MsiCloseHandle(recordHandle);
                }
            }
            return count;
        }

        /// <summary>
        /// Finalizes the persistent form of the database. All persistent data is written
        /// to the writeable database, and no temporary columns or rows are written.
        /// </summary>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// For a database open in <see cref="DatabaseOpenMode.ReadOnly"/> mode, this method has no effect.
        /// </p><p>
        /// For a database open in <see cref="DatabaseOpenMode.CreateDirect" /> or <see cref="DatabaseOpenMode.Direct" />
        /// mode, it is not necessary to call this method because the database will be automatically committed
        /// when it is closed. However this method may be called at any time to persist the current state of tables
        /// loaded into memory.
        /// </p><p>
        /// For a database open in <see cref="DatabaseOpenMode.Create" /> or <see cref="DatabaseOpenMode.Transact" />
        /// mode, no changes will be persisted until this method is called. If the database object is closed without
        /// calling this method, the database file remains unmodified.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabasecommit.asp">MsiDatabaseCommit</a>
        /// </p></remarks>
        public void Commit()
        {
            if (this.summaryInfo != null && !this.summaryInfo.IsClosed)
            {
                this.summaryInfo.Persist();
                this.summaryInfo.Close();
                this.summaryInfo = null;
            }
            uint ret = NativeMethods.MsiDatabaseCommit((int) this.Handle);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Copies the structure and data from a specified table to a text archive file.
        /// </summary>
        /// <param name="table">Name of the table to be exported</param>
        /// <param name="exportFilePath">Path to the file to be created</param>
        /// <exception cref="FileNotFoundException">the file path is invalid</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseexport.asp">MsiDatabaseExport</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Export(string table, string exportFilePath)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table");
            }

            FileInfo file = new FileInfo(exportFilePath);
            uint ret = NativeMethods.MsiDatabaseExport((int) this.Handle, table, file.DirectoryName, file.Name);
            if (ret != 0)
            {
                if (ret == (uint) NativeMethods.Error.BAD_PATHNAME)
                {
                    throw new FileNotFoundException(null, exportFilePath);
                }
                else
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }
        }

        /// <summary>
        /// Imports a database table from a text archive file, dropping any existing table.
        /// </summary>
        /// <param name="importFilePath">Path to the file to be imported.
        /// The table name is specified within the file.</param>
        /// <exception cref="FileNotFoundException">the file path is invalid</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseimport.asp">MsiDatabaseImport</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Import(string importFilePath)
        {
            if (string.IsNullOrWhiteSpace(importFilePath))
            {
                throw new ArgumentNullException("importFilePath");
            }

            FileInfo file = new FileInfo(importFilePath);
            uint ret = NativeMethods.MsiDatabaseImport((int) this.Handle, file.DirectoryName, file.Name);
            if (ret != 0)
            {
                if (ret == (uint) NativeMethods.Error.BAD_PATHNAME)
                {
                    throw new FileNotFoundException(null, importFilePath);
                }
                else
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }
        }

        /// <summary>
        /// Exports all database tables, streams, and summary information to archive files.
        /// </summary>
        /// <param name="directoryPath">Path to the directory where archive files will be created</param>
        /// <exception cref="FileNotFoundException">the directory path is invalid</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// The directory will be created if it does not already exist.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseexport.asp">MsiDatabaseExport</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void ExportAll(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentNullException("directoryPath");
            }

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            this.Export("_SummaryInformation", Path.Combine(directoryPath, "_SummaryInformation.idt"));

            using (View view = this.OpenView("SELECT `Name` FROM `_Tables`"))
            {
                view.Execute();

                foreach (Record rec in view) using (rec)
                {
                    string table = (string) rec[1];

                    this.Export(table, Path.Combine(directoryPath, table + ".idt"));
                }
            }

            if (!Directory.Exists(Path.Combine(directoryPath, "_Streams")))
            {
                Directory.CreateDirectory(Path.Combine(directoryPath, "_Streams"));
            }

            using (View view = this.OpenView("SELECT `Name`, `Data` FROM `_Streams`"))
            {
                view.Execute();

                foreach (Record rec in view) using (rec)
                {
                    string stream = (string) rec[1];
                    if (stream.EndsWith("SummaryInformation", StringComparison.Ordinal)) continue;

                    int i = stream.IndexOf('.');
                    if (i >= 0)
                    {
                        if (File.Exists(Path.Combine(
                            directoryPath,
                            Path.Combine(stream.Substring(0, i), stream.Substring(i + 1) + ".ibd"))))
                        {
                            continue;
                        }
                    }
                    rec.GetStream(2, Path.Combine(directoryPath, Path.Combine("_Streams", stream)));
                }
            }
        }

        /// <summary>
        /// Imports all database tables, streams, and summary information from archive files.
        /// </summary>
        /// <param name="directoryPath">Path to the directory from which archive files will be imported</param>
        /// <exception cref="FileNotFoundException">the directory path is invalid</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseimport.asp">MsiDatabaseImport</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void ImportAll(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentNullException("directoryPath");
            }

            if (File.Exists(Path.Combine(directoryPath, "_SummaryInformation.idt")))
            {
                this.Import(Path.Combine(directoryPath, "_SummaryInformation.idt"));
            }

            string[] idtFiles = Directory.GetFiles(directoryPath, "*.idt");
            foreach (string file in idtFiles)
            {
                if (Path.GetFileName(file) != "_SummaryInformation.idt")
                {
                    this.Import(file);
                }
            }

            if (Directory.Exists(Path.Combine(directoryPath, "_Streams")))
            {
                View view = this.OpenView("SELECT `Name`, `Data` FROM `_Streams`");
                Record rec = null;
                try
                {
                    view.Execute();
                    string[] streamFiles = Directory.GetFiles(Path.Combine(directoryPath, "_Streams"));
                    foreach (string file in streamFiles)
                    {
                        rec = this.CreateRecord(2);
                        rec[1] = Path.GetFileName(file);
                        rec.SetStream(2, file);
                        view.Insert(rec);
                        rec.Close();
                        rec = null;
                    }
                }
                finally
                {
                    if (rec != null) rec.Close();
                    view.Close();
                }
            }
        }

        /// <summary>
        /// Creates a new record object with the requested number of fields.
        /// </summary>
        /// <param name="fieldCount">Required number of fields, which may be 0.
        /// The maximum number of fields in a record is limited to 65535.</param>
        /// <returns>A new record object that can be used with the database.</returns>
        /// <remarks><p>
        /// This method is equivalent to directly calling the <see cref="Record" />
        /// constructor in all cases outside of a custom action context. When in a
        /// custom action session, this method allows creation of a record that can
        /// work with a database other than the session database.
        /// </p><p>
        /// The Record object should be <see cref="InstallerHandle.Close"/>d after use.
        /// It is best that the handle be closed manually as soon as it is no longer
        /// needed, as leaving lots of unused handles open can degrade performance.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msicreaterecord.asp">MsiCreateRecord</a>
        /// </p></remarks>
        public Record CreateRecord(int fieldCount)
        {
            int hRecord = RemotableNativeMethods.MsiCreateRecord((uint) fieldCount, (int) this.Handle);
            return new Record((IntPtr) hRecord, true, (View) null);
        }

        /// <summary>
        /// Returns the file path of this database, or the handle value if a file path was not specified.
        /// </summary>
        public override string ToString()
        {
            if (this.FilePath != null)
            {
                return this.FilePath;
            }
            else
            {
                return "#" + ((int) this.Handle).ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Closes the database handle.  After closing a handle, further method calls may throw <see cref="InvalidHandleException"/>.
        /// </summary>
        /// <param name="disposing">If true, the method has been called directly or
        /// indirectly by a user's code, so managed and unmanaged resources will be
        /// disposed. If false, only unmanaged resources will be disposed.</param>
        protected override void Dispose(bool disposing)
        {
            if (!this.IsClosed &&
                (this.OpenMode == DatabaseOpenMode.CreateDirect ||
                 this.OpenMode == DatabaseOpenMode.Direct))
            {
                // Always commit a direct-opened database before closing.
                // This avoids unexpected corruption of the database.
                this.Commit();
            }

            base.Dispose(disposing);

            if (disposing)
            {
                if (this.summaryInfo != null)
                {
                    this.summaryInfo.Close();
                    this.summaryInfo = null;
                }

                if (this.deleteOnClose != null)
                {
                    foreach (string path in this.deleteOnClose)
                    {
                        try
                        {
                            if (Directory.Exists(path))
                            {
                                Directory.Delete(path, true);
                            }
                            else
                            {
                                if (File.Exists(path)) File.Delete(path);
                            }
                        }
                        catch (IOException)
                        {
                        }
                        catch (UnauthorizedAccessException)
                        {
                        }
                    }
                    this.deleteOnClose = null;
                }
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static int Open(string filePath, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentNullException("outputPath");
            }

            int hDb;
            uint ret = NativeMethods.MsiOpenDatabase(filePath, outputPath, out hDb);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
            return hDb;
        }

        private static int Open(string filePath, DatabaseOpenMode mode)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (Path.GetExtension(filePath).Equals(".msp", StringComparison.Ordinal))
            {
                const int DATABASEOPENMODE_PATCH = 32;
                int patchMode = (int) mode | DATABASEOPENMODE_PATCH;
                mode = (DatabaseOpenMode) patchMode;
            }

            int hDb;
            uint ret = NativeMethods.MsiOpenDatabase(filePath, (IntPtr) mode, out hDb);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(
                    ret,
                    String.Format(CultureInfo.InvariantCulture, "Database=\"{0}\"", filePath));
            }
            return hDb;
        }

        /// <summary>
        /// Returns the value of the specified property.
        /// </summary>
        /// <param name="property">Name of the property to retrieve.</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string ExecutePropertyQuery(string property)
        {
            IList<string> values = this.ExecuteStringQuery("SELECT `Value` FROM `Property` WHERE `Property` = '{0}'", property);
            return (values.Count > 0 ? values[0] : null);
        }
    }
}
