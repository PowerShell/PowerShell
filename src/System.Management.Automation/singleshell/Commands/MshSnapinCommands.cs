/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace Microsoft.PowerShell.Commands
{
    #region PSSnapInCommandBase
    /// <summary>
    /// Base class for all the pssnapin related cmdlets.
    /// </summary>
    public abstract class PSSnapInCommandBase : PSCmdlet, IDisposable
    {
        #region IDisposable Members

        /// <summary>
        /// Set to true when object is disposed
        /// </summary>
        /// 
        private bool _disposed;

        /// <summary>
        /// Dispose method unloads the app domain and the
        /// resource reader if it was created.
        /// </summary>
        /// 
        public void Dispose()
        {
            if (_disposed == false)
            {
                if (_resourceReader != null)
                {
                    _resourceReader.Dispose();
                    _resourceReader = null;
                }
                GC.SuppressFinalize(this);
            }
            _disposed = true;
        }

        #endregion IDisposable Members

        #region Cmdlet Methods

        /// <summary>
        /// Disposes the resource reader.
        /// </summary>
        /// 
        protected override void EndProcessing()
        {
            if (_resourceReader != null)
            {
                _resourceReader.Dispose();
                _resourceReader = null;
            }
        }

        #endregion CmdletMethods

        #region Internal Methods

        /// <summary>
        /// Runspace configuration for the current engine
        /// </summary>
        /// <remarks>
        /// PSSnapIn cmdlets need <see cref="RunspaceConfigForSingleShell"/> object to work with.
        /// </remarks>
        internal RunspaceConfigForSingleShell Runspace
        {
            get
            {
                RunspaceConfigForSingleShell runSpace = Context.RunspaceConfiguration as RunspaceConfigForSingleShell;

                if (runSpace == null)
                {
                    return null;
                }

                return runSpace;
            }
        }

        /// <summary>
        /// Writes a non-terminating error onto the pipeline.
        /// </summary>
        /// <param name="targetObject">Object which caused this exception.</param>
        /// <param name="errorId">ErrorId for this error.</param>
        /// <param name="innerException">Complete exception object.</param>
        /// <param name="category">ErrorCategory for this exception.</param>
        internal void WriteNonTerminatingError(
            Object targetObject,
            string errorId,
            Exception innerException,
            ErrorCategory category)
        {
            WriteError(new ErrorRecord(innerException, errorId, category, targetObject));
        }


        /// <summary>
        /// Searches the input list for the pattern supplied.
        /// </summary>
        /// <param name="searchList">Input list</param>
        /// <param name="pattern">pattern with wildcards</param>
        /// <returns>
        /// A collection of string objects (representing PSSnapIn name)
        /// that match the pattern.
        /// </returns>
        /// <remarks>
        /// Please note that this method will use WildcardPattern class.
        /// So it wont support all the 'regex' patterns
        /// </remarks>
        internal Collection<string>
        SearchListForPattern(Collection<PSSnapInInfo> searchList,
            string pattern)
        {
            Collection<string> listToReturn = new Collection<string>();

            if (null == searchList)
            {
                // return an empty list
                return listToReturn;
            }

            WildcardPattern matcher = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
            // We are doing WildCard search
            foreach (PSSnapInInfo psSnapIn in searchList)
            {
                if (matcher.IsMatch(psSnapIn.Name))
                {
                    listToReturn.Add(psSnapIn.Name);
                }
            }

            // the returned list might contain 0 objects
            return listToReturn;
        }

        /// <summary>
        /// See if the snapin is already loaded..returns load snapin info if true, null otherwise.
        /// </summary>
        /// <returns></returns>
        internal static PSSnapInInfo IsSnapInLoaded(Collection<PSSnapInInfo> loadedSnapins, PSSnapInInfo psSnapInInfo)
        {
            if (null == loadedSnapins)
            {
                return null;
            }

            foreach (PSSnapInInfo loadedPSSnapInInfo in loadedSnapins)
            {
                // See if the assembly-qualified names match and return the existing PSSnapInInfo
                // if they do.
                string loadedSnapInAssemblyName = loadedPSSnapInInfo.AssemblyName;
                if (string.Equals(loadedPSSnapInInfo.Name, psSnapInInfo.Name, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(loadedSnapInAssemblyName)
                    && string.Equals(loadedSnapInAssemblyName, psSnapInInfo.AssemblyName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return loadedPSSnapInInfo;
                }
            }

            return null;
        }

        /// <summary>
        /// Routine to get the list of loaded snapins...
        /// </summary>
        /// <returns></returns>
        protected internal Collection<PSSnapInInfo> GetSnapIns(string pattern)
        {
            // If RunspaceConfiguration is not null, then return the list that it has
            if (Runspace != null)
            {
                if (pattern != null)
                    return Runspace.ConsoleInfo.GetPSSnapIn(pattern, _shouldGetAll);
                else
                    return Runspace.ConsoleInfo.PSSnapIns;
            }

            WildcardPattern matcher = null;
            if (!String.IsNullOrEmpty(pattern))
            {
                bool doWildCardSearch = WildcardPattern.ContainsWildcardCharacters(pattern);

                if (!doWildCardSearch)
                {
                    // Verify PSSnapInID..
                    // This will throw if it not a valid name
                    PSSnapInInfo.VerifyPSSnapInFormatThrowIfError(pattern);
                }
                matcher = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
            }

            Collection<PSSnapInInfo> snapins = new Collection<PSSnapInInfo>();
            if (_shouldGetAll)
            {
                foreach (PSSnapInInfo snapinKey in PSSnapInReader.ReadAll())
                {
                    if (matcher == null || matcher.IsMatch(snapinKey.Name))
                        snapins.Add(snapinKey);
                }
            }
            else
            {
                // Otherwise, just scan through the list of cmdlets and rebuild the table.
                List<CmdletInfo> cmdlets = InvokeCommand.GetCmdlets();
                Dictionary<PSSnapInInfo, bool> snapinTable = new Dictionary<PSSnapInInfo, bool>();
                foreach (CmdletInfo cmdlet in cmdlets)
                {
                    PSSnapInInfo snapin = cmdlet.PSSnapIn;
                    if (snapin != null && !snapinTable.ContainsKey(snapin))
                        snapinTable.Add(snapin, true);
                }

                foreach (PSSnapInInfo snapinKey in snapinTable.Keys)
                {
                    if (matcher == null || matcher.IsMatch(snapinKey.Name))
                        snapins.Add(snapinKey);
                }
            }
            return snapins;
        }

        /// <summary>
        /// Use to indicate if all registered snapins should be listed by GetSnapins...
        /// </summary>
        protected internal bool ShouldGetAll
        {
            get { return _shouldGetAll; }
            set { _shouldGetAll = value; }
        }
        private bool _shouldGetAll;

        /// <summary>
        /// A single instance of the resource indirect reader.  This is used to load the
        /// managed resource assemblies in a different app-domain so that they can be unloaded.
        /// For perf reasons we only want to create one instance for the duration of the command
        /// and be sure it gets disposed when the command completes.
        /// </summary>
        /// 
        internal RegistryStringResourceIndirect ResourceReader
        {
            get {
                return _resourceReader ?? (_resourceReader = RegistryStringResourceIndirect.GetResourceIndirectReader());
            }
        }
        private RegistryStringResourceIndirect _resourceReader;
        #endregion
    }

    #endregion

    #region Add-PSSnapIn

    /// <summary>
    /// Class that implements add-pssnapin cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "PSSnapin", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113281")]
    [OutputType(typeof(PSSnapInInfo))]
    public sealed class AddPSSnapinCommand : PSSnapInCommandBase
    {
        #region Parameters

        /// <summary>
        /// Property that gets/sets PSSnapIn Ids for the cmdlet.
        /// </summary>
        /// <value>An array of strings representing PSSnapIn ids.</value>
        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipelineByPropertyName = true)]
        public string[] Name
        {
            get
            {
                return _pssnapins;
            }

            set
            {
                _pssnapins = value;
            }
        }
        private string[] _pssnapins;

        /// <summary>
        /// Gets or sets the Passthru flag for the operation.
        /// If true, the PSSnapInInfo object is passed down the 
        /// output pipeline.
        /// </summary>
        [Parameter()]
        public SwitchParameter PassThru
        {
            get
            {
                return _passThru;
            }
            set
            {
                _passThru = value;
            }
        }
        private bool _passThru;

        #endregion

        #region Overrides

        /// <summary>
        /// Adds pssnapins to console file and loads the pssnapin dlls into
        /// the current monad runtime.
        /// </summary>
        /// <remarks>
        /// The new pssnapin information is not stored in the console file until
        /// the file is saved.
        /// </remarks>
        protected override void ProcessRecord()
        {
            // Cache for the information stored in the registry
            // update the cache the first time a wildcard is found..
            Collection<PSSnapInInfo> listToSearch = null;

            foreach (string pattern in _pssnapins)
            {
                Exception exception = null;
                Collection<string> listToAdd = new Collection<string>();

                try
                {
                    // check whether there are any wildcard characters
                    bool doWildCardSearch = WildcardPattern.ContainsWildcardCharacters(pattern);
                    if (doWildCardSearch)
                    {
                        // wildcard found in the pattern
                        // Get all the possible candidates for current monad version
                        if (listToSearch == null)
                        {
                            // cache snapin registry information...

                            // For 3.0 PowerShell, we still use "1" as the registry version key for 
                            // Snapin and Custom shell lookup/discovery.
                            // For 3.0 PowerShell, we use "3" as the registry version key only for Engine
                            // related data like ApplicationBase etc.
                            listToSearch = PSSnapInReader.ReadAll(PSVersionInfo.RegistryVersion1Key);
                        }

                        listToAdd = SearchListForPattern(listToSearch, pattern);

                        // listToAdd wont be null..
                        Diagnostics.Assert(listToAdd != null, "Pattern matching returned null");
                        if (listToAdd.Count == 0)
                        {
                            if (_passThru)
                            {
                                // passThru is specified and we have nothing to add...
                                WriteNonTerminatingError(pattern, "NoPSSnapInsFound",
                                    PSTraceSource.NewArgumentException(pattern,
                                    MshSnapInCmdletResources.NoPSSnapInsFound, pattern),
                                    ErrorCategory.InvalidArgument);
                            }

                            continue;
                        }
                    }
                    else
                    {
                        listToAdd.Add(pattern);
                    }

                    // now add all the snapins for this pattern...
                    AddPSSnapIns(listToAdd);
                }
                catch (PSArgumentException ae)
                {
                    exception = ae;
                }
                catch (System.Security.SecurityException se)
                {
                    exception = se;
                }

                if (exception != null)
                {
                    WriteNonTerminatingError(pattern,
                        "AddPSSnapInRead",
                        exception,
                        ErrorCategory.InvalidArgument);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds one or more snapins
        /// </summary>
        /// <param name="snapInList">List of snapin IDs</param>
        /// <remarks>
        /// This is a helper method and should not throw any
        /// exceptions. All exceptions are caught and displayed
        /// to the user using write* methods
        /// </remarks>
        private void AddPSSnapIns(Collection<string> snapInList)
        {
            if (snapInList == null)
            {
                // nothing to add
                return;
            }

            //BUGBUG TODO - brucepay - this is a workaround for not being able to dynamically update
            // the set of cmdlets in a runspace if there is no RunspaceConfiguration object.
            // This is a temporary fix to unblock remoting tests and need to be corrected/completed
            // before we can ship...

            // If there is no RunspaceConfig object, then
            // use an InitialSessionState object to gather and
            // bind the cmdlets from the snapins...
            if (Context.RunspaceConfiguration == null)
            {
                Collection<PSSnapInInfo> loadedSnapins = base.GetSnapIns(null);
                InitialSessionState iss = InitialSessionState.Create();
                bool isAtleastOneSnapinLoaded = false;
                foreach (string snapIn in snapInList)
                {
                    if (InitialSessionState.IsEngineModule(snapIn))
                    {
                        WriteNonTerminatingError(snapIn, "LoadSystemSnapinAsModule",
                        PSTraceSource.NewArgumentException(snapIn,
                        MshSnapInCmdletResources.LoadSystemSnapinAsModule, snapIn),
                        ErrorCategory.InvalidArgument);
                    }
                    else
                    {
                        PSSnapInException warning;
                        try
                        {
                            // Read snapin data
                            PSSnapInInfo newPSSnapIn = PSSnapInReader.Read(Utils.GetCurrentMajorVersion(), snapIn);
                            PSSnapInInfo psSnapInInfo = IsSnapInLoaded(loadedSnapins, newPSSnapIn);

                            // that means snapin is not already loaded ..so load the snapin
                            // now.
                            if (null == psSnapInInfo)
                            {
                                psSnapInInfo = iss.ImportPSSnapIn(snapIn, out warning);
                                isAtleastOneSnapinLoaded = true;
                                Context.InitialSessionState.ImportedSnapins.Add(psSnapInInfo.Name, psSnapInInfo);
                            }
                            // Write psSnapInInfo object only if passthru is specified.
                            if (_passThru)
                            {
                                // Load the pssnapin info properties that are localizable and redirected in the registry
                                psSnapInInfo.LoadIndirectResources(ResourceReader);
                                WriteObject(psSnapInInfo);
                            }
                        }

                        catch (PSSnapInException pse)
                        {
                            WriteNonTerminatingError(snapIn, "AddPSSnapInRead", pse, ErrorCategory.InvalidData);
                        }
                    }
                }

                if (isAtleastOneSnapinLoaded)
                {
                    // Now update the session state with the new stuff...
                    iss.Bind(Context, /*updateOnly*/ true);
                }

                return;
            }

            foreach (string psSnapIn in snapInList)
            {
                Exception exception = null;

                try
                {
                    PSSnapInException warning = null;

                    PSSnapInInfo psSnapInInfo = this.Runspace.AddPSSnapIn(psSnapIn, out warning);

                    if (warning != null)
                    {
                        WriteNonTerminatingError(psSnapIn, "AddPSSnapInRead", warning, ErrorCategory.InvalidData);
                    }

                    // Write psSnapInInfo object only if passthru is specified.
                    if (_passThru)
                    {
                        // Load the pssnapin info properties that are localizable and redirected in the registry
                        psSnapInInfo.LoadIndirectResources(ResourceReader);
                        WriteObject(psSnapInInfo);
                    }
                }
                catch (PSArgumentException ae)
                {
                    exception = ae;
                }
                catch (PSSnapInException sle)
                {
                    exception = sle;
                }
                catch (System.Security.SecurityException se)
                {
                    exception = se;
                }

                if (exception != null)
                {
                    WriteNonTerminatingError(psSnapIn,
                        "AddPSSnapInRead",
                        exception,
                        ErrorCategory.InvalidArgument);
                }
            }
        }

        #endregion
    }

    #endregion

    #region Remove-PSSnapIn

    /// <summary>
    /// Class that implements remove-pssnapin cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PSSnapin", SupportsShouldProcess = true, HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113378")]
    [OutputType(typeof(PSSnapInInfo))]
    public sealed class RemovePSSnapinCommand : PSSnapInCommandBase
    {
        #region Parameters

        /// <summary>
        /// Property that gets/sets PSSnapIn Ids for the cmdlet.
        /// </summary>
        /// <value>An array of strings representing PSSnapIn ids.</value>
        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipelineByPropertyName = true)]
        public string[] Name
        {
            get
            {
                return _pssnapins;
            }

            set
            {
                _pssnapins = value;
            }
        }
        private string[] _pssnapins;


        /// <summary>
        /// Gets or sets the Passthru flag for the operation.
        /// If true, the PSSnapInInfo object is also passed
        /// down the output pipeline.
        /// </summary>
        [Parameter()]
        public SwitchParameter PassThru
        {
            get
            {
                return _passThru;
            }
            set
            {
                _passThru = value;
            }
        }
        private bool _passThru;

        #endregion

        #region Overrides

        /// <summary>
        /// Removes pssnapins from the current console file.
        /// </summary>
        /// <remarks>
        /// The pssnapin is not unloaded from the current engine. So all the cmdlets that are
        /// represented by this pssnapin will continue to work.
        /// </remarks>
        protected override void ProcessRecord()
        {
            foreach (string psSnapIn in _pssnapins)
            {
                Collection<PSSnapInInfo> snapIns = GetSnapIns(psSnapIn);

                // snapIns won't be null..
                Diagnostics.Assert(snapIns != null, "GetSnapIns() returned null");
                if (snapIns.Count == 0)
                {
                    WriteNonTerminatingError(psSnapIn, "NoPSSnapInsFound",
                        PSTraceSource.NewArgumentException(psSnapIn,
                        MshSnapInCmdletResources.NoPSSnapInsFound, psSnapIn),
                        ErrorCategory.InvalidArgument);

                    continue;
                }

                foreach (PSSnapInInfo snapIn in snapIns)
                {
                    // confirm the operation first
                    // this is always false if WhatIf is set
                    if (ShouldProcess(snapIn.Name))
                    {
                        Exception exception = null;

                        if (this.Runspace == null && this.Context.InitialSessionState != null)
                        {
                            try
                            {
                                // Check if this snapin can be removed

                                // Monad has specific restrictions on the mshsnapinid like
                                // mshsnapinid should be A-Za-z0-9.-_ etc.
                                PSSnapInInfo.VerifyPSSnapInFormatThrowIfError(snapIn.Name);

                                if (MshConsoleInfo.IsDefaultPSSnapIn(snapIn.Name, this.Context.InitialSessionState.defaultSnapins))
                                {
                                    throw PSTraceSource.NewArgumentException(snapIn.Name, ConsoleInfoErrorStrings.CannotRemoveDefault, snapIn.Name);
                                }

                                // Handle the initial session state case...
                                InitialSessionState iss = InitialSessionState.Create();
                                PSSnapInException warning;

                                // Get the snapin information...
                                iss.ImportPSSnapIn(snapIn, out warning);
                                iss.Unbind(Context);
                                Context.InitialSessionState.ImportedSnapins.Remove(snapIn.Name);
                            }
                            catch (PSArgumentException ae)
                            {
                                exception = ae;
                            }

                            if (exception != null)
                            {
                                WriteNonTerminatingError(psSnapIn, "RemovePSSnapIn", exception, ErrorCategory.InvalidArgument);
                            }
                        }
                        else
                        {
                            try
                            {
                                PSSnapInException warning = null;

                                PSSnapInInfo psSnapInInfo = this.Runspace.RemovePSSnapIn(snapIn.Name, out warning);

                                if (warning != null)
                                {
                                    WriteNonTerminatingError(snapIn.Name, "RemovePSSnapInRead", warning, ErrorCategory.InvalidData);
                                }

                                if (_passThru)
                                {
                                    // Load the pssnapin info properties that are localizable and redirected in the registry
                                    psSnapInInfo.LoadIndirectResources(ResourceReader);
                                    WriteObject(psSnapInInfo);
                                }
                            }
                            catch (PSArgumentException ae)
                            {
                                exception = ae;
                            }

                            if (exception != null)
                            {
                                WriteNonTerminatingError(psSnapIn, "RemovePSSnapIn", exception, ErrorCategory.InvalidArgument);
                            }
                        }
                    } // ShouldContinue
                }
            }
        }

        #endregion
    }

    #endregion

    #region Get-PSSnapIn

    /// <summary>
    /// Class that implements get-pssnapin cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PSSnapin", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113330")]
    [OutputType(typeof(PSSnapInInfo))]
    public sealed class GetPSSnapinCommand : PSSnapInCommandBase
    {
        #region Parameters

        /// <summary>
        /// Name(s) of PSSnapIn(s).
        /// </summary>
        [Parameter(Position = 0, Mandatory = false)]
        public string[] Name
        {
            get
            {
                return _pssnapins;
            }
            set
            {
                _pssnapins = value;
            }
        }
        private string[] _pssnapins;

        /// <summary>
        /// Property that determines whether to get all pssnapins that are currently
        /// registered ( in registry ).
        /// </summary>
        /// <value>
        /// A boolean that determines whether to get all pssnapins that are currently
        /// registered ( in registry ).
        /// </value>
        [Parameter(Mandatory = false)]
        public SwitchParameter Registered
        {
            get
            {
                return ShouldGetAll;
            }
            set
            {
                ShouldGetAll = value;
            }
        }


        #endregion

        #region Overrides

        /// <summary>
        /// Constructs PSSnapInfo objects as requested by the user and writes them to the 
        /// output buffer.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (_pssnapins != null)
            {
                foreach (string psSnapIn in _pssnapins)
                {
                    Exception exception = null;

                    try
                    {
                        Collection<PSSnapInInfo> psSnapInInfoList = GetSnapIns(psSnapIn);

                        // psSnapInInfoList wont be null..
                        Diagnostics.Assert(psSnapInInfoList != null, "ConsoleInfo.GetPSSnapIn returned null");
                        if (psSnapInInfoList.Count == 0)
                        {
                            WriteNonTerminatingError(psSnapIn, "NoPSSnapInsFound",
                                PSTraceSource.NewArgumentException(psSnapIn,
                                MshSnapInCmdletResources.NoPSSnapInsFound, psSnapIn),
                                ErrorCategory.InvalidArgument);

                            continue;
                        }

                        foreach (PSSnapInInfo pssnapinInfo in psSnapInInfoList)
                        {
                            // Load the pssnapin info properties that are localizable and redirected in the registry
                            pssnapinInfo.LoadIndirectResources(ResourceReader);
                            WriteObject(pssnapinInfo);
                        }
                    }
                    catch (System.Security.SecurityException se)
                    {
                        exception = se;
                    }
                    catch (PSArgumentException ae)
                    {
                        exception = ae;
                    }

                    if (exception != null)
                    {
                        WriteNonTerminatingError(psSnapIn, "GetPSSnapInRead", exception, ErrorCategory.InvalidArgument);
                    }
                }
            }
            else if (ShouldGetAll)
            {
                Exception exception = null;

                try
                {
                    Collection<PSSnapInInfo> psSnapInInfoList = PSSnapInReader.ReadAll();
                    foreach (PSSnapInInfo pssnapinInfo in psSnapInInfoList)
                    {
                        // Load the pssnapin info properties that are localizable and redirected in the registry
                        pssnapinInfo.LoadIndirectResources(ResourceReader);
                        WriteObject(pssnapinInfo);
                    }
                }
                catch (System.Security.SecurityException se)
                {
                    exception = se;
                }
                catch (PSArgumentException ae)
                {
                    exception = ae;
                }

                if (exception != null)
                {
                    WriteNonTerminatingError(this, "GetPSSnapInRead", exception, ErrorCategory.InvalidArgument);
                }
            }
            else
            {
                // this should never throw..
                Collection<PSSnapInInfo> psSnapInInfoList = GetSnapIns(null);
                foreach (PSSnapInInfo pssnapinInfo in psSnapInInfoList)
                {
                    // Load the pssnapin info properties that are localizable and redirected in the registry
                    pssnapinInfo.LoadIndirectResources(ResourceReader);
                    WriteObject(pssnapinInfo);
                }
            }
        }

        #endregion
    }

    #endregion
}
