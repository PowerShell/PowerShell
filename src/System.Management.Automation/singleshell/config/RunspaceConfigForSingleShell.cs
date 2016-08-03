/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.Reflection;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Runspace config for single shell are a special kind of runspace
    /// configuration that is generated for single shells. 
    /// 
    /// This class needs to handle the standard managment of 
    /// 
    ///     1. consoleInfo: each instance of this class will have 
    ///        a consoleInfo object. 
    ///     2. interface to consoleInfo. This includes "open", 
    ///        "save", and "change" of consoleInfo and console files.
    ///     3. interface to mshsnapin's. This includes add, remove 
    ///        and list mshsnapins in console file. 
    /// 
    /// This class derives from RunspaceConfiguration and supports 
    /// basic information for cmdlets, providers, types, formats, 
    /// etc. 
    /// 
    /// Eventually when minishell model goes away, RunspaceConfiguration
    /// and RunspaceConfigForSingleShell may merge into one class. 
    /// 
    /// </summary>
    internal class RunspaceConfigForSingleShell : RunspaceConfiguration
    {
        #region RunspaceConfigForSingleShell Factory

        /// <exception cref="PSSnapInException">
        /// One or more default mshsnapins cannot be loaded because the
        /// registry is not populated correctly.
        /// </exception>
        /// <exception cref="PSArgumentNullException">
        /// fileName is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// fileName does not specify proper file extension.
        /// </exception>
        /// <exception cref="XmlException">
        /// Unable to load/parse the file specified by fileName.
        /// </exception>
        internal new static RunspaceConfigForSingleShell Create(string consoleFile, out PSConsoleLoadException warning)
        {
            PSConsoleLoadException warning1 = null;

            s_mshsnapinTracer.WriteLine("Creating MshConsoleInfo. consoleFile={0}", consoleFile);

            MshConsoleInfo consoleInfo = MshConsoleInfo.CreateFromConsoleFile(consoleFile, out warning1);

            if (warning1 != null)
            {
                s_mshsnapinTracer.TraceWarning("There was a warning while creating MshConsoleInfo: {0}", warning1.Message);
            }

            // At this time, consoleInfo should not be null. 
            // Otherwise, an exception should have been thrown up. 
            if (consoleInfo != null)
            {
                RunspaceConfigForSingleShell rspcfg = new RunspaceConfigForSingleShell(consoleInfo);
                PSConsoleLoadException warning2 = null;

                rspcfg.LoadConsole(out warning2);

                if (warning2 != null)
                {
                    s_mshsnapinTracer.TraceWarning("There was a warning while loading console: {0}", warning2.Message);
                }

                warning = CombinePSConsoleLoadException(warning1, warning2);

                return rspcfg;
            }

            warning = null;
            return null;
        }

        private static PSConsoleLoadException CombinePSConsoleLoadException(PSConsoleLoadException e1, PSConsoleLoadException e2)
        {
            if ((e1 == null || e1.PSSnapInExceptions.Count == 0) && (e2 == null || e2.PSSnapInExceptions.Count == 0))
                return null;

            if (e1 == null || e1.PSSnapInExceptions.Count == 0)
                return e2;

            if (e2 == null || e2.PSSnapInExceptions.Count == 0)
                return e1;

            foreach (PSSnapInException sile in e2.PSSnapInExceptions)
            {
                e1.PSSnapInExceptions.Add(sile);
            }

            return e1;
        }


        /// <exception cref="PSSnapInException">
        /// One or more default mshsnapins cannot be loaded because the
        /// registry is not populated correctly.
        /// </exception>
        internal static RunspaceConfigForSingleShell CreateDefaultConfiguration()
        {
            s_mshsnapinTracer.WriteLine("Creating default runspace configuration.");

            MshConsoleInfo consoleInfo = MshConsoleInfo.CreateDefaultConfiguration();

            // This should not happen. If there is a failure in creating consoleInfo,
            // an exception should have been thrown up. 
            if (consoleInfo != null)
            {
                RunspaceConfigForSingleShell rspcfg = new RunspaceConfigForSingleShell(consoleInfo);
                PSConsoleLoadException warning = null;

                rspcfg.LoadConsole(out warning);

                if (warning != null)
                {
                    s_mshsnapinTracer.TraceWarning("There was a warning while loading console: {0}", warning.Message);
                }

                return rspcfg;
            }

            s_mshsnapinTracer.WriteLine("Default runspace configuration created.");

            return null;
        }

        private RunspaceConfigForSingleShell(MshConsoleInfo consoleInfo)
        {
            _consoleInfo = consoleInfo;
        }

        #endregion

        #region Console/MshSnapin Manipulation

        private MshConsoleInfo _consoleInfo = null;
        internal MshConsoleInfo ConsoleInfo
        {
            get
            {
                return _consoleInfo;
            }
        }

        internal void SaveConsoleFile()
        {
            if (_consoleInfo == null)
                return;

            _consoleInfo.Save();
        }

        internal void SaveAsConsoleFile(string filename)
        {
            if (_consoleInfo == null)
                return;

            _consoleInfo.SaveAsConsoleFile(filename);
        }

        /// <exception cref="PSArgumentNullException">
        /// mshSnapInID is empty or null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// PSSnapIn is already loaded.
        /// No PSSnapIn with given id found.
        /// PSSnapIn cannot be loaded.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// Caller doesn't have permission to read keys.
        /// </exception>
        internal override PSSnapInInfo DoAddPSSnapIn(string name, out PSSnapInException warning)
        {
            warning = null;

            s_mshsnapinTracer.WriteLine("Adding mshsnapin {0}", name);

            if (_consoleInfo == null)
                return null;

            PSSnapInInfo mshsnapinInfo = null;

            try
            {
                mshsnapinInfo = _consoleInfo.AddPSSnapIn(name);
            }
            catch (PSArgumentException mae)
            {
                s_mshsnapinTracer.TraceError(mae.Message);
                s_mshsnapinTracer.WriteLine("Adding mshsnapin {0} failed.", name);
                throw;
            }
            catch (PSArgumentNullException mane)
            {
                s_mshsnapinTracer.TraceError(mane.Message);
                s_mshsnapinTracer.WriteLine("Adding mshsnapin {0} failed.", name);
                throw;
            }

            if (mshsnapinInfo == null)
                return null;

            LoadPSSnapIn(mshsnapinInfo, out warning);

            if (warning != null)
            {
                s_mshsnapinTracer.TraceWarning("There was a warning when loading mshsnapin {0}: {1}", name, warning.Message);
            }

            s_mshsnapinTracer.WriteLine("MshSnapin {0} added", name);

            return mshsnapinInfo;
        }

        /// <exception cref="PSArgumentNullException">
        /// mshSnapInID is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// mshSnapInID is either a default mshsnapin or not loaded.
        /// </exception>
        internal override PSSnapInInfo DoRemovePSSnapIn(string name, out PSSnapInException warning)
        {
            warning = null;

            if (_consoleInfo == null)
                return null;

            s_mshsnapinTracer.WriteLine("Removing mshsnapin {0}", name);

            PSSnapInInfo mshsnapinInfo = _consoleInfo.RemovePSSnapIn(name);

            UnloadPSSnapIn(mshsnapinInfo, out warning);

            s_mshsnapinTracer.WriteLine("MshSnapin {0} removed", name);

            return mshsnapinInfo;
        }

        internal void UpdateAll()
        {
            string errors = "";

            UpdateAll(out errors);
        }


        internal void UpdateAll(out string errors)
        {
            errors = "";

            this.Cmdlets.Update();
            this.Providers.Update();

            s_mshsnapinTracer.WriteLine("Updating types and formats");

            try
            {
                this.Types.Update();
            }
            catch (RuntimeException e)
            {
                s_mshsnapinTracer.TraceWarning("There was a warning updating types: {0}", e.Message);

                errors += e.Message + "\n";
            }

            try
            {
                this.Formats.Update();
            }
            catch (RuntimeException e)
            {
                s_mshsnapinTracer.TraceWarning("There was a warning updating formats: {0}", e.Message);

                errors += e.Message + "\n";
            }

            try
            {
                this.Assemblies.Update();
            }
            catch (RuntimeException e)
            {
                s_mshsnapinTracer.TraceWarning("There was a warning updating assemblies: {0}", e.Message);

                errors += e.Message + "\n";
            }

            s_mshsnapinTracer.WriteLine("Types and formats updated successfully");
        }

        #endregion

        #region Load Console/MshSnapin's

        private void LoadConsole(out PSConsoleLoadException warning)
        {
            if (_consoleInfo == null)
            {
                warning = null;
                return;
            }

            LoadPSSnapIns(_consoleInfo.PSSnapIns, out warning);
        }

        private void LoadPSSnapIn(PSSnapInInfo mshsnapinInfo, out PSSnapInException warning)
        {
            warning = null;

            try
            {
                LoadPSSnapIn(mshsnapinInfo);
            }
            catch (PSSnapInException)
            {
                if (!mshsnapinInfo.IsDefault)
                    _consoleInfo.RemovePSSnapIn(mshsnapinInfo.Name);

                // exception during load mshsnapin are fatal. 
                throw;
            }

            string errors;

            UpdateAll(out errors);

            if (!String.IsNullOrEmpty(errors))
            {
                s_mshsnapinTracer.TraceWarning("There was a warning while loading mshsnapin {0}:{1}", mshsnapinInfo.Name, errors);

                warning = new PSSnapInException(mshsnapinInfo.Name, errors, true);
            }
        }

        private void LoadPSSnapIns(Collection<PSSnapInInfo> mshsnapinInfos, out PSConsoleLoadException warning)
        {
            warning = null;

            Collection<PSSnapInException> mshsnapinExceptions = new Collection<PSSnapInException>();

            bool partialSuccess = false;

            foreach (PSSnapInInfo mshsnapinInfo in mshsnapinInfos)
            {
                try
                {
                    LoadPSSnapIn(mshsnapinInfo);
                    partialSuccess = true;
                }
                catch (PSSnapInException e)
                {
                    if (!mshsnapinInfo.IsDefault)
                    {
                        _consoleInfo.RemovePSSnapIn(mshsnapinInfo.Name);
                    }
                    else
                    {
                        throw;
                    }

                    mshsnapinExceptions.Add(e);
                }
            }

            if (partialSuccess)
            {
                string errors;
                UpdateAll(out errors);

                if (!String.IsNullOrEmpty(errors))
                {
                    s_mshsnapinTracer.TraceWarning(errors);
                    mshsnapinExceptions.Add(new PSSnapInException(null, errors, true));
                }
            }

            if (mshsnapinExceptions.Count > 0)
            {
                warning = new PSConsoleLoadException(_consoleInfo, mshsnapinExceptions);
                s_mshsnapinTracer.TraceWarning(warning.Message);
            }
        }

        private void LoadPSSnapIn(PSSnapInInfo mshsnapinInfo)
        {
            if (mshsnapinInfo == null)
                return;

#if !CORECLR // CustomPSSnapIn Not Supported On CSS. 
            if (!String.IsNullOrEmpty(mshsnapinInfo.CustomPSSnapInType))
            {
                LoadCustomPSSnapIn(mshsnapinInfo);
                return;
            }
#endif
            Assembly assembly = null;

            s_mshsnapinTracer.WriteLine("Loading assembly for mshsnapin {0}", mshsnapinInfo.Name);

            assembly = LoadMshSnapinAssembly(mshsnapinInfo);

            if (assembly == null)
            {
                s_mshsnapinTracer.TraceError("Loading assembly for mshsnapin {0} failed", mshsnapinInfo.Name);
                return;
            }

            s_mshsnapinTracer.WriteLine("Loading assembly for mshsnapin {0} succeeded", mshsnapinInfo.Name);

            Dictionary<string, SessionStateCmdletEntry> cmdlets = null;
            Dictionary<string, List<SessionStateAliasEntry>> aliases = null;
            Dictionary<string, SessionStateProviderEntry> providers = null;
            string throwAwayHelpFile = null;
            PSSnapInHelpers.AnalyzePSSnapInAssembly(assembly, assembly.Location, mshsnapinInfo, null, false, out cmdlets, out aliases, out providers, out throwAwayHelpFile);
            if (cmdlets != null)
            {
                foreach (var c in cmdlets)
                {
                    CmdletConfigurationEntry cmdlet = new CmdletConfigurationEntry(c.Key, c.Value.ImplementingType,
                                                                                   c.Value.HelpFileName, mshsnapinInfo);
                    _cmdlets.AddBuiltInItem(cmdlet);
                }
            }
            if (providers != null)
            {
                foreach (var p in providers)
                {
                    ProviderConfigurationEntry provider = new ProviderConfigurationEntry(p.Key, p.Value.ImplementingType,
                                                                                         p.Value.HelpFileName,
                                                                                         mshsnapinInfo);
                    _providers.AddBuiltInItem(provider);
                }
            }

            foreach (string file in mshsnapinInfo.Types)
            {
                string path = Path.Combine(mshsnapinInfo.ApplicationBase, file);

                TypeConfigurationEntry typeEntry = new TypeConfigurationEntry(path, path, mshsnapinInfo);
                this.Types.AddBuiltInItem(typeEntry);
            }

            foreach (string file in mshsnapinInfo.Formats)
            {
                string path = Path.Combine(mshsnapinInfo.ApplicationBase, file);

                FormatConfigurationEntry formatEntry = new FormatConfigurationEntry(path, path, mshsnapinInfo);
                this.Formats.AddBuiltInItem(formatEntry);
            }

            AssemblyConfigurationEntry assemblyEntry = new AssemblyConfigurationEntry(mshsnapinInfo.AssemblyName, mshsnapinInfo.AbsoluteModulePath, mshsnapinInfo);
            this.Assemblies.AddBuiltInItem(assemblyEntry);

            return;
        }

#if !CORECLR // CustomPSSnapIn Not Supported On CSS.

        private void LoadCustomPSSnapIn(PSSnapInInfo mshsnapinInfo)
        {
            if (mshsnapinInfo == null)
                return;

            if (String.IsNullOrEmpty(mshsnapinInfo.CustomPSSnapInType))
            {
                return;
            }

            Assembly assembly = null;

            s_mshsnapinTracer.WriteLine("Loading assembly for mshsnapin {0}", mshsnapinInfo.Name);

            assembly = LoadMshSnapinAssembly(mshsnapinInfo);

            if (assembly == null)
            {
                s_mshsnapinTracer.TraceError("Loading assembly for mshsnapin {0} failed", mshsnapinInfo.Name);
                return;
            }

            CustomPSSnapIn customPSSnapIn = null;
            try
            {
                Type type = assembly.GetType(mshsnapinInfo.CustomPSSnapInType, true, false);

                if (type != null)
                {
                    customPSSnapIn = (CustomPSSnapIn)Activator.CreateInstance(type);
                }

                s_mshsnapinTracer.WriteLine("Loading assembly for mshsnapin {0} succeeded", mshsnapinInfo.Name);
            }
            catch (TypeLoadException tle)
            {
                throw new PSSnapInException(mshsnapinInfo.Name, tle.Message);
            }
            catch (ArgumentException ae)
            {
                throw new PSSnapInException(mshsnapinInfo.Name, ae.Message);
            }
            catch (MissingMethodException mme)
            {
                throw new PSSnapInException(mshsnapinInfo.Name, mme.Message);
            }
            catch (InvalidCastException ice)
            {
                throw new PSSnapInException(mshsnapinInfo.Name, ice.Message);
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException != null)
                {
                    throw new PSSnapInException(mshsnapinInfo.Name, tie.InnerException.Message);
                }

                throw new PSSnapInException(mshsnapinInfo.Name, tie.Message);
            }

            MergeCustomPSSnapIn(mshsnapinInfo, customPSSnapIn);
            return;
        }

        private void MergeCustomPSSnapIn(PSSnapInInfo mshsnapinInfo, CustomPSSnapIn customPSSnapIn)
        {
            if (mshsnapinInfo == null || customPSSnapIn == null)
                return;

            s_mshsnapinTracer.WriteLine("Merging configuration from custom mshsnapin {0}", mshsnapinInfo.Name);

            if (customPSSnapIn.Cmdlets != null)
            {
                foreach (CmdletConfigurationEntry entry in customPSSnapIn.Cmdlets)
                {
                    CmdletConfigurationEntry cmdlet = new CmdletConfigurationEntry(entry.Name, entry.ImplementingType, entry.HelpFileName, mshsnapinInfo);
                    _cmdlets.AddBuiltInItem(cmdlet);
                }
            }

            if (customPSSnapIn.Providers != null)
            {
                foreach (ProviderConfigurationEntry entry in customPSSnapIn.Providers)
                {
                    ProviderConfigurationEntry provider = new ProviderConfigurationEntry(entry.Name, entry.ImplementingType, entry.HelpFileName, mshsnapinInfo);
                    _providers.AddBuiltInItem(provider);
                }
            }

            if (customPSSnapIn.Types != null)
            {
                foreach (TypeConfigurationEntry entry in customPSSnapIn.Types)
                {
                    string path = Path.Combine(mshsnapinInfo.ApplicationBase, entry.FileName);
                    TypeConfigurationEntry type = new TypeConfigurationEntry(entry.Name, path, mshsnapinInfo);
                    _types.AddBuiltInItem(type);
                }
            }

            if (customPSSnapIn.Formats != null)
            {
                foreach (FormatConfigurationEntry entry in customPSSnapIn.Formats)
                {
                    string path = Path.Combine(mshsnapinInfo.ApplicationBase, entry.FileName);
                    FormatConfigurationEntry format = new FormatConfigurationEntry(entry.Name, path, mshsnapinInfo);
                    _formats.AddBuiltInItem(format);
                }
            }

            AssemblyConfigurationEntry assemblyEntry = new AssemblyConfigurationEntry(mshsnapinInfo.AssemblyName, mshsnapinInfo.AbsoluteModulePath, mshsnapinInfo);
            this.Assemblies.AddBuiltInItem(assemblyEntry);

            s_mshsnapinTracer.WriteLine("Configuration from custom mshsnapin {0} merged", mshsnapinInfo.Name);
        }
#endif

        private void UnloadPSSnapIn(PSSnapInInfo mshsnapinInfo, out PSSnapInException warning)
        {
            warning = null;

            if (mshsnapinInfo != null)
            {
                this.Cmdlets.RemovePSSnapIn(mshsnapinInfo.Name);
                this.Providers.RemovePSSnapIn(mshsnapinInfo.Name);
                this.Assemblies.RemovePSSnapIn(mshsnapinInfo.Name);
                this.Types.RemovePSSnapIn(mshsnapinInfo.Name);
                this.Formats.RemovePSSnapIn(mshsnapinInfo.Name);

                string errors;
                UpdateAll(out errors);

                if (!String.IsNullOrEmpty(errors))
                {
                    s_mshsnapinTracer.TraceWarning(errors);
                    warning = new PSSnapInException(mshsnapinInfo.Name, errors, true);
                }
            }
        }

        private Assembly LoadMshSnapinAssembly(PSSnapInInfo mshsnapinInfo)
        {
            Assembly assembly = null;

            s_mshsnapinTracer.WriteLine("Loading assembly from GAC. Assembly Name: {0}", mshsnapinInfo.AssemblyName);

            try
            {
                // WARNING: DUPLICATE CODE see InitialSessionState
                assembly = Assembly.Load(new AssemblyName(mshsnapinInfo.AssemblyName));
            }
            catch (FileLoadException e)
            {
                s_mshsnapinTracer.TraceWarning("Not able to load assembly {0}: {1}", mshsnapinInfo.AssemblyName, e.Message);
            }
            catch (BadImageFormatException e)
            {
                s_mshsnapinTracer.TraceWarning("Not able to load assembly {0}: {1}", mshsnapinInfo.AssemblyName, e.Message);
            }
            catch (FileNotFoundException e)
            {
                s_mshsnapinTracer.TraceWarning("Not able to load assembly {0}: {1}", mshsnapinInfo.AssemblyName, e.Message);
            }

            if (assembly != null)
                return assembly;

            s_mshsnapinTracer.WriteLine("Loading assembly from path: {0}", mshsnapinInfo.AssemblyName);

            try
            {
                AssemblyName assemblyName = ClrFacade.GetAssemblyName(mshsnapinInfo.AbsoluteModulePath);
                if (string.Compare(assemblyName.FullName, mshsnapinInfo.AssemblyName, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    string message = StringUtil.Format(ConsoleInfoErrorStrings.PSSnapInAssemblyNameMismatch, mshsnapinInfo.AbsoluteModulePath, mshsnapinInfo.AssemblyName);
                    s_mshsnapinTracer.TraceError(message);
                    throw new PSSnapInException(mshsnapinInfo.Name, message);
                }

                assembly = ClrFacade.LoadFrom(mshsnapinInfo.AbsoluteModulePath);
            }
            catch (FileLoadException e)
            {
                s_mshsnapinTracer.TraceError("Not able to load assembly {0}: {1}", mshsnapinInfo.AssemblyName, e.Message);
                throw new PSSnapInException(mshsnapinInfo.Name, e.Message);
            }
            catch (BadImageFormatException e)
            {
                s_mshsnapinTracer.TraceError("Not able to load assembly {0}: {1}", mshsnapinInfo.AssemblyName, e.Message);
                throw new PSSnapInException(mshsnapinInfo.Name, e.Message);
            }
            catch (FileNotFoundException e)
            {
                s_mshsnapinTracer.TraceError("Not able to load assembly {0}: {1}", mshsnapinInfo.AssemblyName, e.Message);
                throw new PSSnapInException(mshsnapinInfo.Name, e.Message);
            }

            return assembly;
        }

        #endregion

        #region RunspaceConfiguration Api implementation

        /// <summary>
        /// Gets the shell id for current runspace configuration.
        /// </summary>
        /// 
        public override string ShellId
        {
            get
            {
                return Utils.DefaultPowerShellShellID;
            }
        }

        private RunspaceConfigurationEntryCollection<CmdletConfigurationEntry> _cmdlets =
            new RunspaceConfigurationEntryCollection<CmdletConfigurationEntry>();

        /// <summary>
        /// Gets the cmdlets defined in runspace configuration.
        /// </summary>
        public override RunspaceConfigurationEntryCollection<CmdletConfigurationEntry> Cmdlets
        {
            get
            {
                return _cmdlets;
            }
        }

        private RunspaceConfigurationEntryCollection<ProviderConfigurationEntry> _providers = new RunspaceConfigurationEntryCollection<ProviderConfigurationEntry>();

        /// <summary>
        /// Gets the providers defined in runspace configuration.
        /// </summary>
        public override RunspaceConfigurationEntryCollection<ProviderConfigurationEntry> Providers
        {
            get
            {
                return _providers;
            }
        }

        private RunspaceConfigurationEntryCollection<TypeConfigurationEntry> _types;

        /// <summary>
        /// Gets the type data files defined in runspace configuration.
        /// </summary>
        public override RunspaceConfigurationEntryCollection<TypeConfigurationEntry> Types
        {
            get { return _types ?? (_types = new RunspaceConfigurationEntryCollection<TypeConfigurationEntry>()); }
        }

        private RunspaceConfigurationEntryCollection<FormatConfigurationEntry> _formats;

        /// <summary>
        /// Gets the format data files defined in runspace configuration.
        /// </summary>
        public override RunspaceConfigurationEntryCollection<FormatConfigurationEntry> Formats
        {
            get {
                return _formats ?? (_formats = new RunspaceConfigurationEntryCollection<FormatConfigurationEntry>());
            }
        }

        private RunspaceConfigurationEntryCollection<ScriptConfigurationEntry> _initializationScripts;

        /// <summary>
        /// Gets the initialization scripts defined in runspace configuration.
        /// </summary>
        public override RunspaceConfigurationEntryCollection<ScriptConfigurationEntry> InitializationScripts
        {
            get {
                return _initializationScripts ??
                       (_initializationScripts = new RunspaceConfigurationEntryCollection<ScriptConfigurationEntry>());
            }
        }

        #endregion

        private static PSTraceSource s_mshsnapinTracer = PSTraceSource.GetTracer("MshSnapinLoadUnload", "Loading and unloading mshsnapins", false);
    }
}
