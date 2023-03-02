// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Class to manage the database instances, do the reloading, etc.
    /// </summary>
    internal sealed class TypeInfoDataBaseManager
    {
        #region Private Data

        /// <summary>
        /// Instance of the object holding the format.ps1xml in memory database.
        /// </summary>
        internal TypeInfoDataBase Database { get; private set; }

        // for locking the F&O database
        internal object databaseLock = new object();

        // for locking the update from XMLs
        internal object updateDatabaseLock = new object();
        // this is used to throw errors when updating a shared TypeTable.
        internal bool isShared;
        private readonly List<string> _formatFileList;

        internal bool DisableFormatTableUpdates { get; set; }

        #endregion

        #region Constructors

        internal TypeInfoDataBaseManager()
        {
            isShared = false;
            _formatFileList = new List<string>();
        }

        /// <summary>
        /// </summary>
        /// <param name="formatFiles"></param>
        /// <param name="isShared"></param>
        /// <param name="authorizationManager">
        /// Authorization manager to perform signature checks before reading ps1xml files (or null of no checks are needed)
        /// </param>
        /// <param name="host">
        /// Host passed to <paramref name="authorizationManager"/>.  Can be null if no interactive questions should be asked.
        /// </param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException">
        /// 1. FormatFile is not rooted.
        /// </exception>
        /// <exception cref="FormatTableLoadException">
        /// 1. There were errors loading Formattable. Look in the Errors property to get
        /// detailed error messages.
        /// </exception>
        internal TypeInfoDataBaseManager(
            IEnumerable<string> formatFiles,
            bool isShared,
            AuthorizationManager authorizationManager,
            PSHost host)
        {
            _formatFileList = new List<string>();

            Collection<PSSnapInTypeAndFormatErrors> filesToLoad = new Collection<PSSnapInTypeAndFormatErrors>();
            ConcurrentBag<string> errors = new ConcurrentBag<string>();
            foreach (string formatFile in formatFiles)
            {
                if (string.IsNullOrEmpty(formatFile) || (!Path.IsPathRooted(formatFile)))
                {
                    throw PSTraceSource.NewArgumentException(nameof(formatFiles), FormatAndOutXmlLoadingStrings.FormatFileNotRooted, formatFile);
                }

                PSSnapInTypeAndFormatErrors fileToLoad = new PSSnapInTypeAndFormatErrors(string.Empty, formatFile);
                fileToLoad.Errors = errors;
                filesToLoad.Add(fileToLoad);
                _formatFileList.Add(formatFile);
            }

            PSPropertyExpressionFactory expressionFactory = new PSPropertyExpressionFactory();
            List<XmlLoaderLoggerEntry> logEntries = null;

            // load the files
            LoadFromFile(filesToLoad, expressionFactory, true, authorizationManager, host, false, out logEntries);
            this.isShared = isShared;

            // check to see if there are any errors loading the format files
            if (!errors.IsEmpty)
            {
                throw new FormatTableLoadException(errors);
            }
        }

        #endregion

        internal TypeInfoDataBase GetTypeInfoDataBase()
        {
            return Database;
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
            if (string.IsNullOrEmpty(formatFile) || (!Path.IsPathRooted(formatFile)))
            {
                throw PSTraceSource.NewArgumentException(nameof(formatFile), FormatAndOutXmlLoadingStrings.FormatFileNotRooted, formatFile);
            }

            lock (_formatFileList)
            {
                if (shouldPrepend)
                {
                    _formatFileList.Insert(0, formatFile);
                }
                else
                {
                    _formatFileList.Add(formatFile);
                }
            }
        }

        /// <summary>
        /// Removes the <paramref name="formatFile"/> from the current FormatTable's file list.
        /// The FormatTable will not reflect the change until Update is called.
        /// </summary>
        /// <param name="formatFile"></param>
        internal void Remove(string formatFile)
        {
            lock (_formatFileList)
            {
                _formatFileList.Remove(formatFile);
            }
        }

        /// <summary>
        /// Update a shared formatting database with formatData of 'ExtendedTypeDefinition' type.
        /// This method should only be called from the FormatTable, where are shared formatting
        /// database is created.
        /// </summary>
        /// <param name="formatData">
        /// The format data to update the database
        /// </param>
        /// <param name="shouldPrepend">
        /// Specify the order in which the format data will be loaded
        /// </param>
        internal void AddFormatData(IEnumerable<ExtendedTypeDefinition> formatData, bool shouldPrepend)
        {
            Diagnostics.Assert(isShared, "this method should only be called from FormatTable to update a shared database");

            Collection<PSSnapInTypeAndFormatErrors> filesToLoad = new Collection<PSSnapInTypeAndFormatErrors>();
            ConcurrentBag<string> errors = new ConcurrentBag<string>();
            if (shouldPrepend)
            {
                foreach (ExtendedTypeDefinition typeDefinition in formatData)
                {
                    PSSnapInTypeAndFormatErrors entryToLoad = new PSSnapInTypeAndFormatErrors(string.Empty, typeDefinition);
                    entryToLoad.Errors = errors;
                    filesToLoad.Add(entryToLoad);
                }
                // check if the passed in formatData is empty
                if (filesToLoad.Count == 0)
                {
                    return;
                }
            }

            lock (_formatFileList)
            {
                foreach (string formatFile in _formatFileList)
                {
                    PSSnapInTypeAndFormatErrors fileToLoad = new PSSnapInTypeAndFormatErrors(string.Empty, formatFile);
                    fileToLoad.Errors = errors;
                    filesToLoad.Add(fileToLoad);
                }
            }

            if (!shouldPrepend)
            {
                foreach (ExtendedTypeDefinition typeDefinition in formatData)
                {
                    PSSnapInTypeAndFormatErrors entryToLoad = new PSSnapInTypeAndFormatErrors(string.Empty, typeDefinition);
                    entryToLoad.Errors = errors;
                    filesToLoad.Add(entryToLoad);
                }
                // check if the passed in formatData is empty
                if (filesToLoad.Count == _formatFileList.Count)
                {
                    return;
                }
            }

            PSPropertyExpressionFactory expressionFactory = new PSPropertyExpressionFactory();
            List<XmlLoaderLoggerEntry> logEntries = null;

            // load the formatting data
            LoadFromFile(filesToLoad, expressionFactory, false, null, null, false, out logEntries);

            // check to see if there are any errors loading the format files
            if (!errors.IsEmpty)
            {
                throw new FormatTableLoadException(errors);
            }
        }

        /// <summary>
        /// Update the current formattable with the existing formatFileList.
        /// New files might have been added using Add() or Files might
        /// have been removed using Remove.
        /// </summary>
        /// <param name="authorizationManager">
        /// Authorization manager to perform signature checks before reading ps1xml files (or null of no checks are needed)
        /// </param>
        /// <param name="host">
        /// Host passed to <paramref name="authorizationManager"/>.  Can be null if no interactive questions should be asked.
        /// </param>
        internal void Update(AuthorizationManager authorizationManager, PSHost host)
        {
            if (DisableFormatTableUpdates)
            {
                return;
            }

            if (isShared)
            {
                throw PSTraceSource.NewInvalidOperationException(FormatAndOutXmlLoadingStrings.SharedFormatTableCannotBeUpdated);
            }

            Collection<PSSnapInTypeAndFormatErrors> filesToLoad = new Collection<PSSnapInTypeAndFormatErrors>();
            lock (_formatFileList)
            {
                foreach (string formatFile in _formatFileList)
                {
                    PSSnapInTypeAndFormatErrors fileToLoad = new PSSnapInTypeAndFormatErrors(string.Empty, formatFile);
                    filesToLoad.Add(fileToLoad);
                }
            }

            UpdateDataBase(filesToLoad, authorizationManager, host, false);
        }

        /// <summary>
        /// Update the format data database. If there is any error in loading the format xml files,
        /// the old database is unchanged.
        /// The reference returned should NOT be modified by any means by the caller.
        /// </summary>
        /// <param name="mshsnapins">Files to be loaded and errors to be updated.</param>
        /// <param name="authorizationManager">
        /// Authorization manager to perform signature checks before reading ps1xml files (or null of no checks are needed)
        /// </param>
        /// <param name="host">
        /// Host passed to <paramref name="authorizationManager"/>.  Can be null if no interactive questions should be asked.
        /// </param>
        /// <param name="preValidated">
        /// True if the format data has been pre-validated (build time, manual testing, etc) so that validation can be
        /// skipped at runtime.
        /// </param>
        /// <returns>Database instance.</returns>
        internal void UpdateDataBase(
            Collection<PSSnapInTypeAndFormatErrors> mshsnapins,
            AuthorizationManager authorizationManager,
            PSHost host,
            bool preValidated
            )
        {
            if (DisableFormatTableUpdates)
            {
                return;
            }

            if (isShared)
            {
                throw PSTraceSource.NewInvalidOperationException(FormatAndOutXmlLoadingStrings.SharedFormatTableCannotBeUpdated);
            }

            PSPropertyExpressionFactory expressionFactory = new PSPropertyExpressionFactory();
            List<XmlLoaderLoggerEntry> logEntries = null;
            LoadFromFile(mshsnapins, expressionFactory, false, authorizationManager, host, preValidated, out logEntries);
        }

        /// <summary>
        /// Load the database
        /// NOTE: need to be protected by lock since not thread safe per se.
        /// </summary>
        /// <param name="files">*.formal.xml files to be loaded.</param>
        /// <param name="expressionFactory">Expression factory to validate script blocks.</param>
        /// <param name="acceptLoadingErrors">If true, load the database even if there are loading errors.</param>
        /// <param name="authorizationManager">
        /// Authorization manager to perform signature checks before reading ps1xml files (or null of no checks are needed)
        /// </param>
        /// <param name="host">
        /// Host passed to <paramref name="authorizationManager"/>.  Can be null if no interactive questions should be asked.
        /// </param>
        /// <param name="preValidated">
        /// True if the format data has been pre-validated (build time, manual testing, etc) so that validation can be
        /// skipped at runtime.
        /// </param>
        /// <param name="logEntries">Trace and error logs from loading the format Xml files.</param>
        /// <returns>True if we had a successful load.</returns>
        internal bool LoadFromFile(
            Collection<PSSnapInTypeAndFormatErrors> files,
            PSPropertyExpressionFactory expressionFactory,
            bool acceptLoadingErrors,
            AuthorizationManager authorizationManager,
            PSHost host,
            bool preValidated,
            out List<XmlLoaderLoggerEntry> logEntries)
        {
            bool success;
            try
            {
                TypeInfoDataBase newDataBase = null;
                lock (updateDatabaseLock)
                {
                    newDataBase = LoadFromFileHelper(files, expressionFactory, authorizationManager, host, preValidated, out logEntries, out success);
                }
                // if we have a valid database, assign it to the
                // current database
                lock (databaseLock)
                {
                    if (acceptLoadingErrors || success)
                        Database = newDataBase;
                }
            }
            finally
            {
                // if, for any reason, we failed the load, we initialize the
                // data base to an empty instance
                lock (databaseLock)
                {
                    if (Database == null)
                    {
                        TypeInfoDataBase tempDataBase = new TypeInfoDataBase();
                        AddPreLoadIntrinsics(tempDataBase);
                        AddPostLoadIntrinsics(tempDataBase);
                        Database = tempDataBase;
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// It loads a database from file(s).
        /// </summary>
        /// <param name="files">*.formal.xml files to be loaded.</param>
        /// <param name="expressionFactory">Expression factory to validate script blocks.</param>
        /// <param name="authorizationManager">
        /// Authorization manager to perform signature checks before reading ps1xml files (or null of no checks are needed)
        /// </param>
        /// <param name="host">
        /// Host passed to <paramref name="authorizationManager"/>.  Can be null if no interactive questions should be asked.
        /// </param>
        /// <param name="preValidated">
        /// True if the format data has been pre-validated (build time, manual testing, etc) so that validation can be
        /// skipped at runtime.
        /// </param>
        /// <param name="logEntries">List of logger entries (errors, etc.) to return to the caller.</param>
        /// <param name="success">True if no error occurred.</param>
        /// <returns>A database instance loaded from file(s).</returns>
        private static TypeInfoDataBase LoadFromFileHelper(
            Collection<PSSnapInTypeAndFormatErrors> files,
            PSPropertyExpressionFactory expressionFactory,
            AuthorizationManager authorizationManager,
            PSHost host,
            bool preValidated,
            out List<XmlLoaderLoggerEntry> logEntries,
            out bool success)
        {
            success = true;
            // Holds the aggregated log entries for all files...
            logEntries = new List<XmlLoaderLoggerEntry>();

            // fresh instance of the database
            TypeInfoDataBase db = new TypeInfoDataBase();

            // prepopulate the database with any necessary overriding data
            AddPreLoadIntrinsics(db);

            var etwEnabled = RunspaceEventSource.Log.IsEnabled();

            // load the XML document into a copy of the
            // in memory database
            foreach (PSSnapInTypeAndFormatErrors file in files)
            {
                // Loads formatting data from ExtendedTypeDefinition instance
                if (file.FormatData != null)
                {
                    LoadFormatDataHelper(file.FormatData, expressionFactory, logEntries, ref success, file, db, isBuiltInFormatData: false, isForHelp: false);
                    continue;
                }

                if (etwEnabled)
                {
                    RunspaceEventSource.Log.ProcessFormatFileStart(file.FullPath);
                }

                if (!ProcessBuiltin(file, db, expressionFactory, logEntries, ref success))
                {
                    // Loads formatting data from formatting data XML file
                    XmlFileLoadInfo info =
                        new XmlFileLoadInfo(Path.GetPathRoot(file.FullPath), file.FullPath, file.Errors, file.PSSnapinName);
                    using (TypeInfoDataBaseLoader loader = new TypeInfoDataBaseLoader())
                    {
                        if (!loader.LoadXmlFile(info, db, expressionFactory, authorizationManager, host, preValidated))
                            success = false;

                        foreach (XmlLoaderLoggerEntry entry in loader.LogEntries)
                        {
                            // filter in only errors from the current file...
                            if (entry.entryType == XmlLoaderLoggerEntry.EntryType.Error)
                            {
                                string mshsnapinMessage = StringUtil.Format(FormatAndOutXmlLoadingStrings.MshSnapinQualifiedError, info.psSnapinName, entry.message);
                                info.errors.Add(mshsnapinMessage);
                                if (entry.failToLoadFile) { file.FailToLoadFile = true; }
                            }
                        }
                        // now aggregate the entries...
                        logEntries.AddRange(loader.LogEntries);
                    }
                }

                if (etwEnabled) RunspaceEventSource.Log.ProcessFormatFileStop(file.FullPath);
            }

            // add any sensible defaults to the database
            AddPostLoadIntrinsics(db);

            return db;
        }

        private static void LoadFormatDataHelper(
            ExtendedTypeDefinition formatData,
            PSPropertyExpressionFactory expressionFactory, List<XmlLoaderLoggerEntry> logEntries, ref bool success,
            PSSnapInTypeAndFormatErrors file, TypeInfoDataBase db,
            bool isBuiltInFormatData,
            bool isForHelp)
        {
            using (TypeInfoDataBaseLoader loader = new TypeInfoDataBaseLoader())
            {
                if (!loader.LoadFormattingData(formatData, db, expressionFactory, isBuiltInFormatData, isForHelp))
                    success = false;

                foreach (XmlLoaderLoggerEntry entry in loader.LogEntries)
                {
                    // filter in only errors from the current file...
                    if (entry.entryType == XmlLoaderLoggerEntry.EntryType.Error)
                    {
                        string mshsnapinMessage = StringUtil.Format(FormatAndOutXmlLoadingStrings.MshSnapinQualifiedError,
                            file.PSSnapinName, entry.message);
                        file.Errors.Add(mshsnapinMessage);
                    }
                }
                // now aggregate the entries...
                logEntries.AddRange(loader.LogEntries);
            }
        }

        private delegate IEnumerable<ExtendedTypeDefinition> TypeGenerator();

        private static Dictionary<string, Tuple<bool, TypeGenerator>> s_builtinGenerators;

        private static Tuple<bool, TypeGenerator> GetBuiltin(bool isForHelp, TypeGenerator generator)
        {
            return new Tuple<bool, TypeGenerator>(isForHelp, generator);
        }

        private static bool ProcessBuiltin(
            PSSnapInTypeAndFormatErrors file,
            TypeInfoDataBase db,
            PSPropertyExpressionFactory expressionFactory,
            List<XmlLoaderLoggerEntry> logEntries,
            ref bool success)
        {
            if (s_builtinGenerators == null)
            {
                var builtInGenerators = new Dictionary<string, Tuple<bool, TypeGenerator>>(StringComparer.OrdinalIgnoreCase);

                var psHome = Utils.DefaultPowerShellAppBase;

                builtInGenerators.Add(Path.Combine(psHome, "Certificate.format.ps1xml"), GetBuiltin(false, Certificate_Format_Ps1Xml.GetFormatData));
                builtInGenerators.Add(Path.Combine(psHome, "Diagnostics.Format.ps1xml"), GetBuiltin(false, Diagnostics_Format_Ps1Xml.GetFormatData));
                builtInGenerators.Add(Path.Combine(psHome, "DotNetTypes.format.ps1xml"), GetBuiltin(false, DotNetTypes_Format_Ps1Xml.GetFormatData));
                builtInGenerators.Add(Path.Combine(psHome, "Event.Format.ps1xml"), GetBuiltin(false, Event_Format_Ps1Xml.GetFormatData));
                builtInGenerators.Add(Path.Combine(psHome, "FileSystem.format.ps1xml"), GetBuiltin(false, FileSystem_Format_Ps1Xml.GetFormatData));
                builtInGenerators.Add(Path.Combine(psHome, "Help.format.ps1xml"), GetBuiltin(true, Help_Format_Ps1Xml.GetFormatData));
                builtInGenerators.Add(Path.Combine(psHome, "HelpV3.format.ps1xml"), GetBuiltin(true, HelpV3_Format_Ps1Xml.GetFormatData));
                builtInGenerators.Add(Path.Combine(psHome, "PowerShellCore.format.ps1xml"), GetBuiltin(false, PowerShellCore_Format_Ps1Xml.GetFormatData));
                builtInGenerators.Add(Path.Combine(psHome, "PowerShellTrace.format.ps1xml"), GetBuiltin(false, PowerShellTrace_Format_Ps1Xml.GetFormatData));
                builtInGenerators.Add(Path.Combine(psHome, "Registry.format.ps1xml"), GetBuiltin(false, Registry_Format_Ps1Xml.GetFormatData));
                builtInGenerators.Add(Path.Combine(psHome, "WSMan.Format.ps1xml"), GetBuiltin(false, WSMan_Format_Ps1Xml.GetFormatData));

                Interlocked.CompareExchange(ref s_builtinGenerators, builtInGenerators, null);
            }

            Tuple<bool, TypeGenerator> generator;
            if (!s_builtinGenerators.TryGetValue(file.FullPath, out generator))
                return false;

            ProcessBuiltinFormatViewDefinitions(generator.Item2(), db, expressionFactory, file, logEntries, generator.Item1, ref success);
            return true;
        }

        private static void ProcessBuiltinFormatViewDefinitions(
            IEnumerable<ExtendedTypeDefinition> views,
            TypeInfoDataBase db,
            PSPropertyExpressionFactory expressionFactory,
            PSSnapInTypeAndFormatErrors file,
            List<XmlLoaderLoggerEntry> logEntries,
            bool isForHelp,
            ref bool success)
        {
            foreach (var v in views)
            {
                LoadFormatDataHelper(v, expressionFactory, logEntries, ref success, file, db, isBuiltInFormatData: true, isForHelp: isForHelp);
            }
        }

        /// <summary>
        /// Helper to add any pre-load intrinsics to the db.
        /// </summary>
        /// <param name="db">Db being initialized.</param>
        private static void AddPreLoadIntrinsics(TypeInfoDataBase db)
        {
            // NOTE: nothing to add for the time being. Add here if needed.
        }

        /// <summary>
        /// Helper to add any post-load intrinsics to the db.
        /// </summary>
        /// <param name="db">Db being initialized.</param>
        private static void AddPostLoadIntrinsics(TypeInfoDataBase db)
        {
            // add entry for the output of update-formatdata
            // we want to be able to display this as a list, unless overridden
            // by an entry loaded from file
            FormatShapeSelectionOnType sel = new FormatShapeSelectionOnType();
            sel.appliesTo = new AppliesTo();
            sel.appliesTo.AddAppliesToType("Microsoft.PowerShell.Commands.FormatDataLoadingInfo");
            sel.formatShape = FormatShape.List;

            db.defaultSettingsSection.shapeSelectionDirectives.formatShapeSelectionOnTypeList.Add(sel);
        }
    }
}
