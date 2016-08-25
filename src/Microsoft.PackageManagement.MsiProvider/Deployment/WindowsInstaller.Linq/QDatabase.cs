//---------------------------------------------------------------------
// <copyright file="QDatabase.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller.Linq
{
    using System;
    using System.IO;

    /// <summary>
    /// Allows any Database instance to be converted into a queryable database.
    /// </summary>
    internal static class Queryable
    {
        /// <summary>
        /// Converts any Database instance into a queryable database.
        /// </summary>
        /// <param name="db"></param>
        /// <returns>Queryable database instance that operates on the same
        /// MSI handle.</returns>
        /// <remarks>
        /// This extension method is meant for convenient on-the-fly conversion.
        /// If the existing database instance already happens to be a QDatabase,
        /// then it is returned unchanged. Otherwise since the new database
        /// carries the same MSI handle, only one of the instances needs to be
        /// closed, not both.
        /// </remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static QDatabase AsQueryable(this Database db)
        {
            QDatabase qdb = db as QDatabase;
            if (qdb == null && db != null)
            {
                qdb = new QDatabase(db.Handle, true, db.FilePath, db.OpenMode);
            }
            return qdb;
        }
    }

    /// <summary>
    /// Queryable MSI database - extends the base Database class with
    /// LINQ query functionality along with predefined entity types
    /// for common tables.
    /// </summary>
    internal class QDatabase : Database
    {
        /// <summary>
        /// Opens an existing database in read-only mode.
        /// </summary>
        /// <param name="filePath">Path to the database file.</param>
        /// <exception cref="InstallerException">the database could not be created/opened</exception>
        /// <remarks>
        /// Because this constructor initiates database access, it cannot be used with a
        /// running installation.
        /// <para>The Database object should be <see cref="InstallerHandle.Close"/>d after use.
        /// The finalizer will close the handle if it is still open, however due to the nondeterministic
        /// nature of finalization it is best that the handle be closed manually as soon as it is no
        /// longer needed, as leaving lots of unused handles open can degrade performance.</para>
        /// </remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public QDatabase(string filePath)
            : base(filePath)
        {
        }

        /// <summary>
        /// Opens an existing database with another database as output.
        /// </summary>
        /// <param name="filePath">Path to the database to be read.</param>
        /// <param name="outputPath">Open mode for the database</param>
        /// <returns>Database object representing the created or opened database</returns>
        /// <exception cref="InstallerException">the database could not be created/opened</exception>
        /// <remarks>
        /// When a database is opened as the output of another database, the summary information stream
        /// of the output database is actually a read-only mirror of the original database and thus cannot
        /// be changed. Additionally, it is not persisted with the database. To create or modify the
        /// summary information for the output database it must be closed and re-opened.
        /// <para>The returned Database object should be <see cref="InstallerHandle.Close"/>d after use.
        /// The finalizer will close the handle if it is still open, however due to the nondeterministic
        /// nature of finalization it is best that the handle be closed manually as soon as it is no
        /// longer needed, as leaving lots of unused handles open can degrade performance.</para>
        /// </remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public QDatabase(string filePath, string outputPath)
            : base(filePath, outputPath)
        {
        }

        /// <summary>
        /// Opens an existing database or creates a new one.
        /// </summary>
        /// <param name="filePath">Path to the database file. If an empty string
        /// is supplied, a temporary database is created that is not persisted.</param>
        /// <param name="mode">Open mode for the database</param>
        /// <exception cref="InstallerException">the database could not be created/opened</exception>
        /// <remarks>
        /// To make and save changes to a database first open the database in transaction,
        /// create or, or direct mode. After making the changes, always call the Commit method
        /// before closing the database handle. The Commit method flushes all buffers.
        /// <para>Always call the Commit method on a database that has been opened in direct
        /// mode before closing the database. Failure to do this may corrupt the database.</para>
        /// <para>Because this constructor initiates database access, it cannot be used with a
        /// running installation.</para>
        /// <para>The Database object should be <see cref="InstallerHandle.Close"/>d after use.
        /// The finalizer will close the handle if it is still open, however due to the nondeterministic
        /// nature of finalization it is best that the handle be closed manually as soon as it is no
        /// longer needed, as leaving lots of unused handles open can degrade performance.</para>
        /// </remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public QDatabase(string filePath, DatabaseOpenMode mode)
            : base(filePath, mode)
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
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        protected internal QDatabase(
            IntPtr handle, bool ownsHandle, string filePath, DatabaseOpenMode openMode)
            : base(handle, ownsHandle, filePath, openMode)
        {
        }

        /// <summary>
        /// Gets or sets a log where all MSI SQL queries are written.
        /// </summary>
        /// <remarks>
        /// The log can be useful for debugging, or simply to watch the LINQ magic in action.
        /// </remarks>
        public TextWriter Log { get; set; }

        /// <summary>
        /// Gets a queryable table from the database.
        /// </summary>
        /// <param name="table">name of the table</param>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public QTable<QRecord> this[string table]
        {
            get
            {
                return new QTable<QRecord>(this, table);
            }
        }

        #if !CODE_ANALYSIS
        #region Queryable tables

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<Component_> Components
        { get { return new QTable<Component_>(this); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<CreateFolder_> CreateFolders
        { get { return new QTable<CreateFolder_>(this); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<CustomAction_> CustomActions
        { get { return new QTable<CustomAction_>(this); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<Directory_> Directories
        { get { return new QTable<Directory_>(this); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<DuplicateFile_> DuplicateFiles
        { get { return new QTable<DuplicateFile_>(this); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<Feature_> Features
        { get { return new QTable<Feature_>(this); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<FeatureComponent_> FeatureComponents
        { get { return new QTable<FeatureComponent_>(this); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<File_> Files
        { get { return new QTable<File_>(this); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<FileHash_> FileHashes
        { get { return new QTable<FileHash_>(this); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<InstallSequence_> InstallExecuteSequences
        { get { return new QTable<InstallSequence_>(this, "InstallExecuteSequence"); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<InstallSequence_> InstallUISequences
        { get { return new QTable<InstallSequence_>(this, "InstallUISequence"); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<LaunchCondition_> LaunchConditions
        { get { return new QTable<LaunchCondition_>(this); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<Media_> Medias
        { get { return new QTable<Media_>(this); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<Property_> Properties
        { get { return new QTable<Property_>(this); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<Registry_> Registries
        { get { return new QTable<Registry_>(this); } }

        /// <summary>Queryable standard table with predefined specialized record type.</summary>
        public QTable<RemoveFile_> RemoveFiles
        { get { return new QTable<RemoveFile_>(this); } }

        #endregion // Queryable tables
        #endif // !CODE_ANALYSIS
    }
}
