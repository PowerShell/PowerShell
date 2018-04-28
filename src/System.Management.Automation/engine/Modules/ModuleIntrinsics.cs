// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using Microsoft.PowerShell.Commands;
using System.Linq;
using System.Text;
using System.Threading;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    internal static class Constants
    {
        public const string PSModulePathEnvVar = "PSModulePath";
    }

    /// <summary>
    /// Encapsulates the basic module operations for a PowerShell engine instance...
    /// </summary>
    public class ModuleIntrinsics
    {
        /// <summary>
        /// Tracer for module analysis
        /// </summary>
        [TraceSource("Modules", "Module loading and analysis")]
        internal static PSTraceSource Tracer = PSTraceSource.GetTracer("Modules", "Module loading and analysis");

        internal ModuleIntrinsics(ExecutionContext context)
        {
            _context = context;

            // And initialize the module path...
            SetModulePath();
        }
        private readonly ExecutionContext _context;

        // Holds the module collection...
        internal Dictionary<string, PSModuleInfo> ModuleTable { get; } = new Dictionary<string, PSModuleInfo>(StringComparer.OrdinalIgnoreCase);

        private const int MaxModuleNestingDepth = 10;

        internal void IncrementModuleNestingDepth(PSCmdlet cmdlet, string path)
        {
            if (++ModuleNestingDepth > MaxModuleNestingDepth)
            {
                string message = StringUtil.Format(Modules.ModuleTooDeeplyNested, path, MaxModuleNestingDepth);
                InvalidOperationException ioe = new InvalidOperationException(message);
                ErrorRecord er = new ErrorRecord(ioe, "Modules_ModuleTooDeeplyNested",
                    ErrorCategory.InvalidOperation, path);
                // NOTE: this call will throw
                cmdlet.ThrowTerminatingError(er);
            }
        }
        internal void DecrementModuleNestingCount()
        {
            --ModuleNestingDepth;
        }

        internal int ModuleNestingDepth { get; private set; }

        /// <summary>
        /// Create a new module object from a scriptblock specifying the path to set for the module
        /// </summary>
        /// <param name="name">The name of the module</param>
        /// <param name="path">The path where the module is rooted</param>
        /// <param name="scriptBlock">
        /// ScriptBlock that is executed to initialize the module...
        /// </param>
        /// <param name="arguments">
        /// The arguments to pass to the scriptblock used to initialize the module
        /// </param>
        /// <param name="ss">The session state instance to use for this module - may be null</param>
        /// <param name="results">The results produced from evaluating the scriptblock</param>
        /// <returns>The newly created module info object</returns>
        internal PSModuleInfo CreateModule(string name, string path, ScriptBlock scriptBlock, SessionState ss, out List<object> results, params object[] arguments)
        {
            return CreateModuleImplementation(name, path, scriptBlock, null, ss, null, out results, arguments);
        }

        /// <summary>
        /// Create a new module object from a ScriptInfo object
        /// </summary>
        /// <param name="path">The path where the module is rooted</param>
        /// <param name="scriptInfo">The script info to use to create the module</param>
        /// <param name="scriptPosition">The position for the command that loaded this module</param>
        /// <param name="arguments">Optional arguments to pass to the script while executing</param>
        /// <param name="ss">The session state instance to use for this module - may be null</param>
        /// <param name="privateData">The private data to use for this module - may be null</param>
        /// <returns>The constructed module object</returns>
        internal PSModuleInfo CreateModule(string path, ExternalScriptInfo scriptInfo, IScriptExtent scriptPosition, SessionState ss, object privateData, params object[] arguments)
        {
            List<object> result;
            return CreateModuleImplementation(ModuleIntrinsics.GetModuleName(path), path, scriptInfo, scriptPosition, ss, privateData, out result, arguments);
        }

        /// <summary>
        /// Create a new module object from code specifying the path to set for the module
        /// </summary>
        /// <param name="name">The name of the module</param>
        /// <param name="path">The path to use for the module root</param>
        /// <param name="moduleCode">
        /// The code to use to create the module. This can be one of ScriptBlock, string
        /// or ExternalScriptInfo
        /// </param>
        /// <param name="arguments">
        /// Arguments to pass to the module scriptblock during evaluation.
        /// </param>
        /// <param name="result">
        /// The results of the evaluation of the scriptblock.
        /// </param>
        /// <param name="scriptPosition">
        /// The position of the caller of this function so you can tell where the call
        /// to Import-Module (or whatever) occurred. This can be null.
        /// </param>
        /// <param name="ss">The session state instance to use for this module - may be null</param>
        /// <param name="privateData">The private data to use for this module - may be null</param>
        /// <returns>The created module</returns>
        private PSModuleInfo CreateModuleImplementation(string name, string path, object moduleCode, IScriptExtent scriptPosition, SessionState ss, object privateData, out List<object> result, params object[] arguments)
        {
            ScriptBlock sb;

            // By default the top-level scope in a session state object is the global scope for the instance.
            // For modules, we need to set its global scope to be another scope object and, chain the top
            // level scope for this sessionstate instance to be the parent. The top level scope for this ss is the
            // script scope for the ss.

            // Allocate the session state instance for this module.
            if (ss == null)
            {
                ss = new SessionState(_context, true, true);
            }

            // Now set up the module's session state to be the current session state
            SessionStateInternal oldSessionState = _context.EngineSessionState;
            PSModuleInfo module = new PSModuleInfo(name, path, _context, ss);
            ss.Internal.Module = module;
            module.PrivateData = privateData;

            bool setExitCode = false;
            int exitCode = 0;

            try
            {
                _context.EngineSessionState = ss.Internal;

                // Build the scriptblock at this point so the references to the module
                // context are correct...
                ExternalScriptInfo scriptInfo = moduleCode as ExternalScriptInfo;
                if (scriptInfo != null)
                {
                    sb = scriptInfo.ScriptBlock;

                    _context.Debugger.RegisterScriptFile(scriptInfo);
                }
                else
                {
                    sb = moduleCode as ScriptBlock;
                    if (sb != null)
                    {
                        PSLanguageMode? moduleLanguageMode = sb.LanguageMode;
                        sb = sb.Clone();
                        sb.LanguageMode = moduleLanguageMode;

                        sb.SessionState = ss;
                    }
                    else
                    {
                        var sbText = moduleCode as string;
                        if (sbText != null)
                            sb = ScriptBlock.Create(_context, sbText);
                    }
                }
                if (sb == null)
                    throw PSTraceSource.NewInvalidOperationException();

                sb.SessionStateInternal = ss.Internal;

                InvocationInfo invocationInfo = new InvocationInfo(scriptInfo, scriptPosition);

                // Save the module string
                module._definitionExtent = sb.Ast.Extent;
                var ast = sb.Ast;
                while (ast.Parent != null)
                {
                    ast = ast.Parent;
                }

                // The variables set in the interpreted case get set by InvokeWithPipe in the compiled case.
                Diagnostics.Assert(_context.SessionState.Internal.CurrentScope.LocalsTuple == null,
                                    "No locals tuple should have been created yet.");

                List<object> resultList = new List<object>();

                try
                {
                    Pipe outputPipe = new Pipe(resultList);

                    // And run the scriptblock...
                    sb.InvokeWithPipe(
                        useLocalScope: false,
                        errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                        dollarUnder: AutomationNull.Value,
                        input: AutomationNull.Value,
                        scriptThis: AutomationNull.Value,
                        outputPipe: outputPipe,
                        invocationInfo: invocationInfo,
                        args: arguments ?? Utils.EmptyArray<object>());
                }
                catch (ExitException ee)
                {
                    exitCode = (int)ee.Argument;
                    setExitCode = true;
                }
                result = resultList;
            }
            finally
            {
                _context.EngineSessionState = oldSessionState;
            }

            if (setExitCode)
            {
                _context.SetVariable(SpecialVariables.LastExitCodeVarPath, exitCode);
            }

            module.ImplementingAssembly = sb.AssemblyDefiningPSTypes;
            // We force re-population of ExportedTypeDefinitions, now with the actual RuntimeTypes, created above.
            module.CreateExportedTypeDefinitions(sb.Ast as ScriptBlockAst);

            return module;
        }

        /// <summary>
        /// Allocate a new dynamic module then return a new scriptblock
        /// bound to the module instance.
        /// </summary>
        /// <param name="context">Context to use to create bounded script.</param>
        /// <param name="sb">The scriptblock to bind</param>
        /// <param name="linkToGlobal">Whether it should be linked to the global session state or not</param>
        /// <returns>A new scriptblock</returns>
        internal ScriptBlock CreateBoundScriptBlock(ExecutionContext context, ScriptBlock sb, bool linkToGlobal)
        {
            PSModuleInfo module = new PSModuleInfo(context, linkToGlobal);
            return module.NewBoundScriptBlock(sb, context);
        }

        internal List<PSModuleInfo> GetModules(string[] patterns, bool all)
        {
            return GetModuleCore(patterns, all, false);
        }

        internal List<PSModuleInfo> GetExactMatchModules(string moduleName, bool all, bool exactMatch)
        {
            if (moduleName == null) { moduleName = String.Empty; }
            return GetModuleCore(new string[] { moduleName }, all, exactMatch);
        }

        private List<PSModuleInfo> GetModuleCore(string[] patterns, bool all, bool exactMatch)
        {
            string targetModuleName = null;
            List<WildcardPattern> wcpList = new List<WildcardPattern>();

            if (exactMatch)
            {
                Dbg.Assert(patterns.Length == 1, "The 'patterns' should only contain one element when it is for an exact match");
                targetModuleName = patterns[0];
            }
            else
            {
                if (patterns == null)
                {
                    patterns = new string[] { "*" };
                }

                foreach (string pattern in patterns)
                {
                    wcpList.Add(WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase));
                }
            }

            List<PSModuleInfo> modulesMatched = new List<PSModuleInfo>();

            if (all)
            {
                foreach (PSModuleInfo module in ModuleTable.Values)
                {
                    // See if this is the requested module...
                    if ((exactMatch && module.Name.Equals(targetModuleName, StringComparison.OrdinalIgnoreCase)) ||
                        (!exactMatch && SessionStateUtilities.MatchesAnyWildcardPattern(module.Name, wcpList, false)))
                    {
                        modulesMatched.Add(module);
                    }
                }
            }
            else
            {
                // Create a joint list of local and global modules. Only report a module once.
                // Local modules are reported before global modules...
                Dictionary<string, bool> found = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in _context.EngineSessionState.ModuleTable)
                {
                    string path = pair.Key;
                    PSModuleInfo module = pair.Value;
                    // See if this is the requested module...
                    if ((exactMatch && module.Name.Equals(targetModuleName, StringComparison.OrdinalIgnoreCase)) ||
                        (!exactMatch && SessionStateUtilities.MatchesAnyWildcardPattern(module.Name, wcpList, false)))
                    {
                        modulesMatched.Add(module);
                        found[path] = true;
                    }
                }
                if (_context.EngineSessionState != _context.TopLevelSessionState)
                {
                    foreach (var pair in _context.TopLevelSessionState.ModuleTable)
                    {
                        string path = pair.Key;
                        if (!found.ContainsKey(path))
                        {
                            PSModuleInfo module = pair.Value;
                            // See if this is the requested module...
                            if ((exactMatch && module.Name.Equals(targetModuleName, StringComparison.OrdinalIgnoreCase)) ||
                                (!exactMatch && SessionStateUtilities.MatchesAnyWildcardPattern(module.Name, wcpList, false)))
                            {
                                modulesMatched.Add(module);
                            }
                        }
                    }
                }
            }

            return modulesMatched.OrderBy(m => m.Name).ToList();
        }

        internal List<PSModuleInfo> GetModules(ModuleSpecification[] fullyQualifiedName, bool all)
        {
            List<PSModuleInfo> modulesMatched = new List<PSModuleInfo>();

            if (all)
            {
                foreach (var moduleSpec in fullyQualifiedName)
                {
                    foreach (PSModuleInfo module in ModuleTable.Values)
                    {
                        // See if this is the requested module...
                        if (IsModuleMatchingModuleSpec(module, moduleSpec))
                        {
                            modulesMatched.Add(module);
                        }
                    }
                }
            }
            else
            {
                foreach (var moduleSpec in fullyQualifiedName)
                {
                    // Create a joint list of local and global modules. Only report a module once.
                    // Local modules are reported before global modules...
                    Dictionary<string, bool> found = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                    foreach (var pair in _context.EngineSessionState.ModuleTable)
                    {
                        string path = pair.Key;
                        PSModuleInfo module = pair.Value;
                        // See if this is the requested module...
                        if (IsModuleMatchingModuleSpec(module, moduleSpec))
                        {
                            modulesMatched.Add(module);
                            found[path] = true;
                        }
                    }

                    if (_context.EngineSessionState != _context.TopLevelSessionState)
                    {
                        foreach (var pair in _context.TopLevelSessionState.ModuleTable)
                        {
                            string path = pair.Key;
                            if (!found.ContainsKey(path))
                            {
                                PSModuleInfo module = pair.Value;
                                // See if this is the requested module...
                                if (IsModuleMatchingModuleSpec(module, moduleSpec))
                                {
                                    modulesMatched.Add(module);
                                }
                            }
                        }
                    }
                }
            }

            return modulesMatched.OrderBy(m => m.Name).ToList();
        }

        internal static bool IsModuleMatchingModuleSpec(PSModuleInfo moduleInfo, ModuleSpecification moduleSpec)
        {
            if (moduleInfo != null && moduleSpec != null &&
                moduleInfo.Name.Equals(moduleSpec.Name, StringComparison.OrdinalIgnoreCase) &&
                (!moduleSpec.Guid.HasValue || moduleSpec.Guid.Equals(moduleInfo.Guid)) &&
                ((moduleSpec.Version == null && moduleSpec.RequiredVersion == null && moduleSpec.MaximumVersion == null)
                 || (moduleSpec.RequiredVersion != null && moduleSpec.RequiredVersion.Equals(moduleInfo.Version))
                 || (moduleSpec.MaximumVersion == null && moduleSpec.Version != null && moduleSpec.RequiredVersion == null && moduleSpec.Version <= moduleInfo.Version)
                 || (moduleSpec.MaximumVersion != null && moduleSpec.Version == null && moduleSpec.RequiredVersion == null && ModuleCmdletBase.GetMaximumVersion(moduleSpec.MaximumVersion) >= moduleInfo.Version)
                 || (moduleSpec.MaximumVersion != null && moduleSpec.Version != null && moduleSpec.RequiredVersion == null && ModuleCmdletBase.GetMaximumVersion(moduleSpec.MaximumVersion) >= moduleInfo.Version && moduleSpec.Version <= moduleInfo.Version)))
            {
                return true;
            }

            return false;
        }

        internal static Version GetManifestModuleVersion(string manifestPath)
        {
            if (manifestPath != null &&
                manifestPath.EndsWith(StringLiterals.PowerShellDataFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var dataFileSetting =
                        PsUtils.GetModuleManifestProperties(
                            manifestPath,
                            PsUtils.ManifestModuleVersionPropertyName);

                    var versionValue = dataFileSetting["ModuleVersion"];
                    if (versionValue != null)
                    {
                        Version moduleVersion;
                        if (LanguagePrimitives.TryConvertTo(versionValue, out moduleVersion))
                        {
                            return moduleVersion;
                        }
                    }
                }
                catch (PSInvalidOperationException)
                {
                }
            }

            return new Version(0, 0);
        }

        internal static Guid GetManifestGuid(string manifestPath)
        {
            if (manifestPath != null &&
                manifestPath.EndsWith(StringLiterals.PowerShellDataFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var dataFileSetting =
                        PsUtils.GetModuleManifestProperties(
                            manifestPath,
                            PsUtils.ManifestGuidPropertyName);

                    var guidValue = dataFileSetting["GUID"];
                    if (guidValue != null)
                    {
                        Guid guidID;
                        if (LanguagePrimitives.TryConvertTo(guidValue, out guidID))
                        {
                            return guidID;
                        }
                    }
                }
                catch (PSInvalidOperationException)
                {
                }
            }

            return new Guid();
        }

        // The extensions of all of the files that can be processed with Import-Module, put the ni.dll in front of .dll to have higher priority to be loaded.
        internal static string[] PSModuleProcessableExtensions = new string[] {
                            StringLiterals.PowerShellDataFileExtension,
                            StringLiterals.PowerShellScriptFileExtension,
                            StringLiterals.PowerShellModuleFileExtension,
                            StringLiterals.PowerShellCmdletizationFileExtension,
                            StringLiterals.WorkflowFileExtension,
                            StringLiterals.PowerShellNgenAssemblyExtension,
                            StringLiterals.PowerShellILAssemblyExtension};

        // A list of the extensions to check for implicit module loading and discovery, put the ni.dll in front of .dll to have higher priority to be loaded.
        internal static string[] PSModuleExtensions = new string[] {
                            StringLiterals.PowerShellDataFileExtension,
                            StringLiterals.PowerShellModuleFileExtension,
                            StringLiterals.PowerShellCmdletizationFileExtension,
                            StringLiterals.WorkflowFileExtension,
                            StringLiterals.PowerShellNgenAssemblyExtension,
                            StringLiterals.PowerShellILAssemblyExtension};

        /// <summary>
        /// Returns true if the extension is one of the module extensions...
        /// </summary>
        /// <param name="extension">The extension to check</param>
        /// <returns>True if it was a module extension...</returns>
        internal static bool IsPowerShellModuleExtension(string extension)
        {
            foreach (string ext in PSModuleProcessableExtensions)
            {
                if (extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the module name from module path.
        /// </summary>
        /// <param name="path">The path to the module</param>
        /// <returns>The module name</returns>
        internal static string GetModuleName(string path)
        {
            string fileName = path == null ? string.Empty : Path.GetFileName(path);
            string ext;
            if (fileName.EndsWith(StringLiterals.PowerShellNgenAssemblyExtension, StringComparison.OrdinalIgnoreCase))
            {
                ext = StringLiterals.PowerShellNgenAssemblyExtension;
            }
            else
            {
                ext = Path.GetExtension(fileName);
            }
            if (!string.IsNullOrEmpty(ext) && IsPowerShellModuleExtension(ext))
            {
                return fileName.Substring(0, fileName.Length - ext.Length);
            }
            else
            {
                return fileName;
            }
        }

        /// <summary>
        /// Gets the personal module path
        /// </summary>
        /// <returns>personal module path</returns>
        internal static string GetPersonalModulePath()
        {
#if UNIX
            return Platform.SelectProductNameForDirectory(Platform.XDG_Type.USER_MODULES);
#else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Utils.ModuleDirectory);
#endif
        }

        /// <summary>
        /// Gets the PSHome module path, as known as the "system wide module path" in windows powershell.
        /// </summary>
        /// <returns>The PSHome module path</returns>
        internal static string GetPSHomeModulePath()
        {
            if (s_psHomeModulePath != null)
                return s_psHomeModulePath;

            try
            {
                string psHome = Utils.DefaultPowerShellAppBase;
                if (!string.IsNullOrEmpty(psHome))
                {
                    // Win8: 584267 Powershell Modules are listed twice in x86, and cannot be removed
                    // This happens because ModuleTable uses Path as the key and CBS installer
                    // expands the path to include "SysWOW64" (for
                    // HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\PowerShell\3\PowerShellEngine ApplicationBase).
                    // Because of this, the module that is getting loaded during startup (through LocalRunspace)
                    // is using "SysWow64" in the key. Later, when Import-Module is called, it loads the
                    // module using ""System32" in the key.
#if !UNIX
                    psHome = psHome.ToLowerInvariant().Replace("\\syswow64\\", "\\system32\\");
#endif
                    Interlocked.CompareExchange(ref s_psHomeModulePath, Path.Combine(psHome, "Modules"), null);
                }
            }
            catch (System.Security.SecurityException) { }

            return s_psHomeModulePath;
        }

        private static string s_psHomeModulePath;

        /// <summary>
        /// Get the module path that is shared among different users.
        /// It's known as "Program Files" module path in windows powershell.
        /// </summary>
        /// <returns></returns>
        private static string GetSharedModulePath()
        {
#if UNIX
            return Platform.SelectProductNameForDirectory(Platform.XDG_Type.SHARED_MODULES);
#else
            string sharedModulePath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            if (!string.IsNullOrEmpty(sharedModulePath))
            {
                sharedModulePath = Path.Combine(sharedModulePath, Utils.ModuleDirectory);
            }
            return sharedModulePath;
#endif
        }

        /// <summary>
        /// Combine the PS system-wide module path and the DSC module path
        /// to get the system module paths.
        /// </summary>
        /// <returns></returns>
        private static string CombineSystemModulePaths()
        {
            string psHomeModulePath = GetPSHomeModulePath();
            string sharedModulePath = GetSharedModulePath();

            bool isPSHomePathNullOrEmpty = string.IsNullOrEmpty(psHomeModulePath);
            bool isSharedPathNullOrEmpty = string.IsNullOrEmpty(sharedModulePath);

            if (!isPSHomePathNullOrEmpty && !isSharedPathNullOrEmpty)
            {
                return (sharedModulePath + Path.PathSeparator + psHomeModulePath);
            }

            if (!isPSHomePathNullOrEmpty || !isSharedPathNullOrEmpty)
            {
                return isPSHomePathNullOrEmpty ? sharedModulePath : psHomeModulePath;
            }

            return null;
        }

        internal static string GetExpandedEnvironmentVariable(string name, EnvironmentVariableTarget target)
        {
            string result = Environment.GetEnvironmentVariable(name, target);
            if (!string.IsNullOrEmpty(result))
            {
                result = Environment.ExpandEnvironmentVariables(result);
            }
            return result;
        }

        /// <summary>
        /// Checks if a particular string (path) is a member of 'combined path' string (like %Path% or %PSModulePath%)
        /// </summary>
        /// <param name="pathToScan">'Combined path' string to analyze; can not be null.</param>
        /// <param name="pathToLookFor">Path to search for; can not be another 'combined path' (semicolon-separated); can not be null.</param>
        /// <returns>Index of pathToLookFor in pathToScan; -1 if not found.</returns>
        private static int PathContainsSubstring(string pathToScan, string pathToLookFor)
        {
            // we don't support if any of the args are null - parent function should ensure this; empty values are ok
            Diagnostics.Assert(pathToScan != null, "pathToScan should not be null according to contract of the function");
            Diagnostics.Assert(pathToLookFor != null, "pathToLookFor should not be null according to contract of the function");

            int pos = 0; // position of the current substring in pathToScan
            string[] substrings = pathToScan.Split(Utils.Separators.PathSeparator, StringSplitOptions.None); // we want to process empty entries
            string goodPathToLookFor = pathToLookFor.Trim().TrimEnd(Path.DirectorySeparatorChar); // trailing backslashes and white-spaces will mess up equality comparison
            foreach (string substring in substrings)
            {
                string goodSubstring = substring.Trim().TrimEnd(Path.DirectorySeparatorChar);  // trailing backslashes and white-spaces will mess up equality comparison

                // We have to use equality comparison on individual substrings (as opposed to simple 'string.IndexOf' or 'string.Contains')
                // because of cases like { pathToScan="C:\Temp\MyDir\MyModuleDir", pathToLookFor="C:\Temp" }
                if (string.Equals(goodSubstring, goodPathToLookFor, StringComparison.OrdinalIgnoreCase))
                {
                    return pos; // match found - return index of it in the 'pathToScan' string
                }
                else
                {
                    pos += substring.Length + 1; // '1' is for trailing semicolon
                }
            }
            // if we are here, that means a match was not found
            return -1;
        }

        /// <summary>
        /// Adds paths to a 'combined path' string (like %Path% or %PSModulePath%) if they are not already there.
        /// </summary>
        /// <param name="basePath">Path string (like %Path% or %PSModulePath%).</param>
        /// <param name="pathToAdd">Collection of individual paths to add.</param>
        /// <param name="insertPosition">-1 to append to the end; 0 to insert in the beginning of the string; etc...</param>
        /// <returns>Result string.</returns>
        private static string AddToPath(string basePath, string pathToAdd, int insertPosition)
        {
            // we don't support if any of the args are null - parent function should ensure this; empty values are ok
            Diagnostics.Assert(basePath != null, "basePath should not be null according to contract of the function");
            Diagnostics.Assert(pathToAdd != null, "pathToAdd should not be null according to contract of the function");

            StringBuilder result = new StringBuilder(basePath);

            if (!string.IsNullOrEmpty(pathToAdd)) // we don't want to append empty paths
            {
                foreach (string subPathToAdd in pathToAdd.Split(Utils.Separators.PathSeparator, StringSplitOptions.RemoveEmptyEntries)) // in case pathToAdd is a 'combined path' (semicolon-separated)
                {
                    int position = PathContainsSubstring(result.ToString(), subPathToAdd); // searching in effective 'result' value ensures that possible duplicates in pathsToAdd are handled correctly
                    if (-1 == position) // subPathToAdd not found - add it
                    {
                        if (-1 == insertPosition) // append subPathToAdd to the end
                        {
                            bool endsWithPathSeparator = false;
                            if (result.Length > 0) endsWithPathSeparator = (result[result.Length - 1] == Path.PathSeparator);

                            if (endsWithPathSeparator)
                                result.Append(subPathToAdd);
                            else
                                result.Append(Path.PathSeparator + subPathToAdd);
                        }
                        else // insert at the requested location (this is used by DSC (<Program Files> location) and by 'user-specific location' (SpecialFolder.MyDocuments or EVT.User))
                        {
                            result.Insert(insertPosition, subPathToAdd + Path.PathSeparator);
                        }
                    }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Check if the current powershell is likely running in following scenarios:
        ///  - sxs ps started on windows [machine-wide env:PSModulePath will influence]
        ///  - sxs ps started from full ps
        ///  - sxs ps started from inbox nano/iot ps
        ///  - full ps started from sxs ps
        ///  - inbox nano/iot ps started from sxs ps
        /// If it's likely one of them, then we need to clear the current process module path.
        /// </summary>
        private static bool NeedToClearProcessModulePath(string currentProcessModulePath, string personalModulePath, string sharedModulePath, bool runningSxS)
        {
#if UNIX
            return false;
#else
            Dbg.Assert(!string.IsNullOrEmpty(personalModulePath), "caller makes sure personalModulePath not null or empty");
            Dbg.Assert(sharedModulePath != null, "caller makes sure sharedModulePath is not null");

            const string winSxSModuleDirectory = @"PowerShell\Modules";
            const string winLegacyModuleDirectory = @"WindowsPowerShell\Modules";

            if (runningSxS)
            {
                // The machine-wide and user-wide environment variables are only meaningful for full ps,
                // so if the current process module path contains any of them, it's likely that the sxs
                // ps was started directly on windows, or from full ps. The same goes for the legacy personal
                // and shared module paths.
                string hklmModulePath = GetExpandedEnvironmentVariable(Constants.PSModulePathEnvVar, EnvironmentVariableTarget.Machine);
                string hkcuModulePath = GetExpandedEnvironmentVariable(Constants.PSModulePathEnvVar, EnvironmentVariableTarget.User);
                string legacyPersonalModulePath = personalModulePath.Replace(winSxSModuleDirectory, winLegacyModuleDirectory);
                string legacyProgramFilesModulePath = sharedModulePath.Replace(winSxSModuleDirectory, winLegacyModuleDirectory);

                return (!string.IsNullOrEmpty(hklmModulePath) && currentProcessModulePath.IndexOf(hklmModulePath, StringComparison.OrdinalIgnoreCase) != -1) ||
                       (!string.IsNullOrEmpty(hkcuModulePath) && currentProcessModulePath.IndexOf(hkcuModulePath, StringComparison.OrdinalIgnoreCase) != -1) ||
                       currentProcessModulePath.IndexOf(legacyPersonalModulePath, StringComparison.OrdinalIgnoreCase) != -1 ||
                       currentProcessModulePath.IndexOf(legacyProgramFilesModulePath, StringComparison.OrdinalIgnoreCase) != -1;
            }

            // The sxs personal and shared module paths are only meaningful for sxs ps, so if they appear
            // in the current process module path, it's likely the running ps was started from a sxs ps.
            string sxsPersonalModulePath = personalModulePath.Replace(winLegacyModuleDirectory, winSxSModuleDirectory);
            string sxsProgramFilesModulePath = sharedModulePath.Replace(winLegacyModuleDirectory, winSxSModuleDirectory);

            return currentProcessModulePath.IndexOf(sxsPersonalModulePath, StringComparison.OrdinalIgnoreCase) != -1 ||
                   currentProcessModulePath.IndexOf(sxsProgramFilesModulePath, StringComparison.OrdinalIgnoreCase) != -1;
#endif
        }

        /// <summary>
        /// When sxs ps instance B got started from sxs ps instance A, A's pshome module path might
        /// show up in current process module path. It doesn't make sense for B to load modules from
        /// A's pshome module path, so remove it in such case.
        /// </summary>
        private static string RemoveSxSPsHomeModulePath(string currentProcessModulePath, string personalModulePath, string sharedModulePath, string psHomeModulePath)
        {
#if UNIX
            const string powershellExeName = "pwsh";
#else
            const string powershellExeName = "pwsh.exe";
#endif
            const string powershellDepsName = "pwsh.deps.json";

            StringBuilder modulePathString = new StringBuilder(currentProcessModulePath.Length);
            char[] invalidPathChars = Path.GetInvalidPathChars();

            foreach (var path in currentProcessModulePath.Split(Utils.Separators.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimedPath = path.Trim().TrimEnd(Path.DirectorySeparatorChar);
                if (trimedPath.IndexOfAny(invalidPathChars) != -1 || !Path.IsPathRooted(trimedPath))
                {
                    // Path contains invalid characters or it's not an absolute path. Ignore it.
                    continue;
                }

                if (!trimedPath.Equals(personalModulePath, StringComparison.OrdinalIgnoreCase) &&
                    !trimedPath.Equals(sharedModulePath, StringComparison.OrdinalIgnoreCase) &&
                    !trimedPath.Equals(psHomeModulePath, StringComparison.OrdinalIgnoreCase) &&
                    trimedPath.EndsWith("Modules", StringComparison.OrdinalIgnoreCase))
                {
                    string parentDir = Path.GetDirectoryName(trimedPath);
                    string psExePath = Path.Combine(parentDir, powershellExeName);
                    string psDepsPath = Path.Combine(parentDir, powershellDepsName);
                    if ((File.Exists(psExePath) && File.Exists(psDepsPath)))
                    {
                        // Path is a PSHome module path from a different powershell core instance. Ignore it.
                        continue;
                    }
                }

                if (modulePathString.Length > 0)
                {
                    modulePathString.Append(Path.PathSeparator);
                }
                modulePathString.Append(trimedPath);
            }

            return modulePathString.ToString();
        }

        /// <summary>
        /// Checks the various PSModulePath environment string and returns PSModulePath string as appropriate. Note - because these
        /// strings go through the provider, we need to escape any wildcards before passing them
        /// along.
        /// </summary>
        public static string GetModulePath(string currentProcessModulePath, string hklmMachineModulePath, string hkcuUserModulePath)
        {
            string personalModulePath = GetPersonalModulePath();
            string sharedModulePath = GetSharedModulePath(); // aka <Program Files> location
            string psHomeModulePath = GetPSHomeModulePath(); // $PSHome\Modules location
            bool runningSxS = Platform.IsInbox ? false : true;

            if (!string.IsNullOrEmpty(currentProcessModulePath) &&
                NeedToClearProcessModulePath(currentProcessModulePath, personalModulePath, sharedModulePath, runningSxS))
            {
                // Clear the current process module path in the following cases
                //  - start sxs ps on windows [machine-wide env:PSModulePath will influence]
                //  - start sxs ps from full ps
                //  - start sxs ps from inbox nano/iot ps
                //  - start full ps from sxs ps
                //  - start inbox nano/iot ps from sxs ps
                currentProcessModulePath = null;
            }

            // If the variable isn't set, then set it to the default value
            if (currentProcessModulePath == null)  // EVT.Process does Not exist - really corner case
            {
                // Handle the default case...
                if (string.IsNullOrEmpty(hkcuUserModulePath)) // EVT.User does Not exist -> set to <SpecialFolder.MyDocuments> location
                {
                    currentProcessModulePath = personalModulePath; // = SpecialFolder.MyDocuments + Utils.ProductNameForDirectory + Utils.ModuleDirectory
                }
                else // EVT.User exists -> set to EVT.User
                {
                    currentProcessModulePath = hkcuUserModulePath; // = EVT.User
                }

                currentProcessModulePath += Path.PathSeparator;
                if (string.IsNullOrEmpty(hklmMachineModulePath)) // EVT.Machine does Not exist
                {
                    currentProcessModulePath += CombineSystemModulePaths(); // += (SharedModulePath + $PSHome\Modules)
                }
                else
                {
                    currentProcessModulePath += hklmMachineModulePath; // += EVT.Machine
                }
            }
            // EVT.Process exists
            // Now handle the case where the environment variable is already set.
            else if (runningSxS) // The running powershell is an SxS PS instance
            {
                // When SxS PS instance A starts SxS PS instance B, A's PSHome module path might be inherited by B. We need to remove that path from B
                currentProcessModulePath = RemoveSxSPsHomeModulePath(currentProcessModulePath, personalModulePath, sharedModulePath, psHomeModulePath);

                string personalModulePathToUse = string.IsNullOrEmpty(hkcuUserModulePath) ? personalModulePath : hkcuUserModulePath;
                string systemModulePathToUse = string.IsNullOrEmpty(hklmMachineModulePath) ? psHomeModulePath : hklmMachineModulePath;

                currentProcessModulePath = AddToPath(currentProcessModulePath, personalModulePathToUse, 0);
                currentProcessModulePath = AddToPath(currentProcessModulePath, systemModulePathToUse, -1);
            }
            else // The running powershell is Full PS or inbox Core PS
            {
                // If there is no personal path key, then if the env variable doesn't match the system variable,
                // the user modified it somewhere, else prepend the default personal module path
                if (hklmMachineModulePath != null) // EVT.Machine exists
                {
                    if (hkcuUserModulePath == null) // EVT.User does Not exist
                    {
                        if (!(hklmMachineModulePath).Equals(currentProcessModulePath, StringComparison.OrdinalIgnoreCase))
                        {
                            // before returning, use <presence of Windows module path> heuristic to conditionally add <Program Files> location
                            int psHomePosition = PathContainsSubstring(currentProcessModulePath, psHomeModulePath); // index of $PSHome\Modules in currentProcessModulePath
                            if (psHomePosition >= 0) // if $PSHome\Modules IS found - insert <Program Files> location before $PSHome\Modules
                            {
                                return AddToPath(currentProcessModulePath, sharedModulePath, psHomePosition);
                            } // if $PSHome\Modules NOT found = <scenario 4> = 'PSModulePath has been constrained by a user to create a sand boxed environment without including System Modules'

                            return null;
                        }
                        currentProcessModulePath = personalModulePath + Path.PathSeparator + hklmMachineModulePath; // <SpecialFolder.MyDocuments> + EVT.Machine + inserted <ProgramFiles> later in this function
                    }
                    else // EVT.User exists
                    {
                        // PSModulePath is designed to have behaviour like 'Path' var in a sense that EVT.User + EVT.Machine are merged to get final value of PSModulePath
                        string combined = string.Concat(hkcuUserModulePath, Path.PathSeparator, hklmMachineModulePath); // EVT.User + EVT.Machine
                        if (!((combined).Equals(currentProcessModulePath, StringComparison.OrdinalIgnoreCase) ||
                            (hklmMachineModulePath).Equals(currentProcessModulePath, StringComparison.OrdinalIgnoreCase) ||
                            (hkcuUserModulePath).Equals(currentProcessModulePath, StringComparison.OrdinalIgnoreCase)))
                        {
                            // before returning, use <presence of Windows module path> heuristic to conditionally add <Program Files> location
                            int psHomePosition = PathContainsSubstring(currentProcessModulePath, psHomeModulePath); // index of $PSHome\Modules in currentProcessModulePath
                            if (psHomePosition >= 0) // if $PSHome\Modules IS found - insert <Program Files> location before $PSHome\Modules
                            {
                                return AddToPath(currentProcessModulePath, sharedModulePath, psHomePosition);
                            } // if $PSHome\Modules NOT found = <scenario 4> = 'PSModulePath has been constrained by a user to create a sand boxed environment without including System Modules'

                            return null;
                        }
                        currentProcessModulePath = combined; // = EVT.User + EVT.Machine + inserted <ProgramFiles> later in this function
                    }
                }
                else // EVT.Machine does Not exist
                {
                    // If there is no system path key, then if the env variable doesn't match the user variable,
                    // the user modified it somewhere, otherwise append the default system path
                    if (hkcuUserModulePath != null) // EVT.User exists
                    {
                        if (hkcuUserModulePath.Equals(currentProcessModulePath, StringComparison.OrdinalIgnoreCase))
                        {
                            currentProcessModulePath = hkcuUserModulePath + Path.PathSeparator + CombineSystemModulePaths(); // = EVT.User + (SharedModulePath + $PSHome\Modules)
                        }
                        else
                        {
                            // before returning, use <presence of Windows module path> heuristic to conditionally add <Program Files> location
                            int psHomePosition = PathContainsSubstring(currentProcessModulePath, psHomeModulePath); // index of $PSHome\Modules in currentProcessModulePath
                            if (psHomePosition >= 0) // if $PSHome\Modules IS found - insert <Program Files> location before $PSHome\Modules
                            {
                                return AddToPath(currentProcessModulePath, sharedModulePath, psHomePosition);
                            } // if $PSHome\Modules NOT found = <scenario 4> = 'PSModulePath has been constrained by a user to create a sand boxed environment without including System Modules'

                            return null;
                        }
                    }
                    else // EVT.User does Not exist
                    {
                        // before returning, use <presence of Windows module path> heuristic to conditionally add <Program Files> location
                        int psHomePosition = PathContainsSubstring(currentProcessModulePath, psHomeModulePath); // index of $PSHome\Modules in currentProcessModulePath
                        if (psHomePosition >= 0) // if $PSHome\Modules IS found - insert <Program Files> location before $PSHome\Modules
                        {
                            return AddToPath(currentProcessModulePath, sharedModulePath, psHomePosition);
                        } // if $PSHome\Modules NOT found = <scenario 4> = 'PSModulePath has been constrained by a user to create a sand boxed environment without including System Modules'

                        // Neither key is set so go with what the environment variable is already set to
                        return null;
                    }
                }
            }

            // if we reached this point - always add <Program Files> location to EVT.Process
            // everything below is the same behaviour as WMF 4 code
            int indexOfPSHomeModulePath = PathContainsSubstring(currentProcessModulePath, psHomeModulePath); // index of $PSHome\Modules in currentProcessModulePath
            // if $PSHome\Modules not found (psHomePosition == -1) - append <Program Files> location to the end;
            // if $PSHome\Modules IS found (psHomePosition >= 0) - insert <Program Files> location before $PSHome\Modules
            currentProcessModulePath = AddToPath(currentProcessModulePath, sharedModulePath, indexOfPSHomeModulePath);

            return currentProcessModulePath;
        }

        /// <summary>
        /// Checks if $env:PSModulePath is not set and sets it as appropriate. Note - because these
        /// strings go through the provider, we need to escape any wildcards before passing them
        /// along.
        /// </summary>
        internal static string GetModulePath()
        {
            string currentModulePath = GetExpandedEnvironmentVariable(Constants.PSModulePathEnvVar, EnvironmentVariableTarget.Process);
            return currentModulePath;
        }
        /// <summary>
        /// Checks if $env:PSModulePath is not set and sets it as appropriate. Note - because these
        /// strings go through the provider, we need to escape any wildcards before passing them
        /// along.
        /// </summary>
        internal static string SetModulePath()
        {
            string currentModulePath = GetExpandedEnvironmentVariable(Constants.PSModulePathEnvVar, EnvironmentVariableTarget.Process);
            string systemWideModulePath = PowerShellConfig.Instance.GetModulePath(ConfigScope.SystemWide);
            string personalModulePath = PowerShellConfig.Instance.GetModulePath(ConfigScope.CurrentUser);

            string newModulePathString = GetModulePath(currentModulePath, systemWideModulePath, personalModulePath);

            if (!string.IsNullOrEmpty(newModulePathString))
            {
                // Set the environment variable...
                Environment.SetEnvironmentVariable(Constants.PSModulePathEnvVar, newModulePathString);
            }

            return newModulePathString;
        }

        /// <summary>
        /// Get the current module path setting.
        /// </summary>
        /// <param name="includeSystemModulePath">
        /// Include The system wide module path ($PSHOME\Modules) even if it's not in PSModulePath.
        /// In V3-V5, we prepended this path during module auto-discovery which incorrectly preferred
        /// $PSHOME\Modules over user installed modules that might have a command that overrides
        /// a product-supplied command.
        /// For 5.1, we append $PSHOME\Modules in this case to avoid the rare case where PSModulePath
        /// does not contain the path, but a script depends on previous behavior.
        /// Note that appending is still a potential breaking change, but necessary to update in-box
        /// modules long term - e.g. when open sourcing a module and installing from the gallery.
        /// </param>
        /// <param name="context"></param>
        /// <returns>The module path as an array of strings</returns>
        internal static IEnumerable<string> GetModulePath(bool includeSystemModulePath, ExecutionContext context)
        {
            string modulePathString = Environment.GetEnvironmentVariable(Constants.PSModulePathEnvVar) ?? SetModulePath();

            HashSet<string> processedPathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(modulePathString))
            {
                foreach (string envPath in modulePathString.Split(Utils.Separators.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    var processedPath = ProcessOneModulePath(context, envPath, processedPathSet);
                    if (processedPath != null)
                        yield return processedPath;
                }
            }

            if (includeSystemModulePath)
            {
                var processedPath = ProcessOneModulePath(context, GetPSHomeModulePath(), processedPathSet);
                if (processedPath != null)
                    yield return processedPath;
            }
        }

        private static string ProcessOneModulePath(ExecutionContext context, string envPath, HashSet<string> processedPathSet)
        {
            string trimmedenvPath = envPath.Trim();

            bool isUnc = Utils.PathIsUnc(trimmedenvPath);
            if (!isUnc)
            {
                // if the path start with "filesystem::", remove it so we can test for URI and
                // also Directory.Exists (if the file system provider isn't actually loaded.)
                if (trimmedenvPath.StartsWith("filesystem::", StringComparison.OrdinalIgnoreCase))
                {
                    trimmedenvPath = trimmedenvPath.Remove(0, 12 /*"filesystem::".Length*/);
                }

                isUnc = Utils.PathIsUnc(trimmedenvPath);
            }

            // If we have an unc, just return the value as resolving the path is expensive.
            if (isUnc)
            {
                return trimmedenvPath;
            }

            // We prefer using the file system provider to resolve paths so callers can avoid processing
            // duplicates, e.g. the following are all the same:
            //     a\b
            //     a\.\b
            //     a\b\
            // But if the file system provider isn't loaded, we will just check if the directory exists.
            if (context.EngineSessionState.IsProviderLoaded(context.ProviderNames.FileSystem))
            {
                ProviderInfo provider = null;
                IEnumerable<string> resolvedPaths = null;
                try
                {
                    resolvedPaths = context.SessionState.Path.GetResolvedProviderPathFromPSPath(
                        WildcardPattern.Escape(trimmedenvPath), out provider);
                }
                catch (ItemNotFoundException)
                {
                    // silently skip directories that are not found
                }
                catch (DriveNotFoundException)
                {
                    // silently skip drives that are not found
                }
                catch (NotSupportedException)
                {
                    // silently skip invalid path
                    // NotSupportedException is thrown if path contains a colon (":") that is not part of a
                    // volume identifier (for example, "c:\" is Supported but not "c:\temp\Z:\invalidPath")
                }

                if (provider != null && resolvedPaths != null && provider.NameEquals(context.ProviderNames.FileSystem))
                {
                    var result = resolvedPaths.FirstOrDefault();
                    if (processedPathSet.Add(result))
                    {
                        return result;
                    }
                }
            }
            else if (Directory.Exists(trimmedenvPath))
            {
                return trimmedenvPath;
            }

            return null;
        }

        private static void SortAndRemoveDuplicates<T>(List<T> input, Func<T, string> keyGetter)
        {
            Dbg.Assert(input != null, "Caller should verify that input != null");

            input.Sort(
                delegate (T x, T y)
                {
                    string kx = keyGetter(x);
                    string ky = keyGetter(y);
                    return string.Compare(kx, ky, StringComparison.OrdinalIgnoreCase);
                }
            );

            bool firstItem = true;
            string previousKey = null;
            List<T> output = new List<T>(input.Count);
            foreach (T item in input)
            {
                string currentKey = keyGetter(item);
                if ((firstItem) || !currentKey.Equals(previousKey, StringComparison.OrdinalIgnoreCase))
                {
                    output.Add(item);
                }

                previousKey = currentKey;
                firstItem = false;
            }

            input.Clear();
            input.AddRange(output);
        }

        /// <summary>
        /// Mark stuff to be exported from the current environment using the various patterns
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling this method</param>
        /// <param name="sessionState">The session state instance to do the exports on</param>
        /// <param name="functionPatterns">Patterns describing the functions to export</param>
        /// <param name="cmdletPatterns">Patterns describing the cmdlets to export</param>
        /// <param name="aliasPatterns">Patterns describing the aliases to export</param>
        /// <param name="variablePatterns">Patterns describing the variables to export</param>
        /// <param name="doNotExportCmdlets">List of Cmdlets that will not be exported,
        ///     even if they match in cmdletPatterns.</param>
        internal static void ExportModuleMembers(PSCmdlet cmdlet, SessionStateInternal sessionState,
            List<WildcardPattern> functionPatterns, List<WildcardPattern> cmdletPatterns,
            List<WildcardPattern> aliasPatterns, List<WildcardPattern> variablePatterns, List<string> doNotExportCmdlets)
        {
            // If this cmdlet is called, then mark that the export list should be used for exporting
            // module members...

            sessionState.UseExportList = true;

            if (functionPatterns != null)
            {
                IDictionary<string, FunctionInfo> ft = sessionState.ModuleScope.FunctionTable;

                foreach (KeyValuePair<string, FunctionInfo> entry in ft)
                {
                    // Skip AllScope functions
                    if ((entry.Value.Options & ScopedItemOptions.AllScope) != 0)
                    {
                        continue;
                    }

                    if (SessionStateUtilities.MatchesAnyWildcardPattern(entry.Key, functionPatterns, false))
                    {
                        sessionState.ExportedFunctions.Add(entry.Value);
                        string message = StringUtil.Format(Modules.ExportingFunction, entry.Key);
                        cmdlet.WriteVerbose(message);
                    }
                }
                SortAndRemoveDuplicates(sessionState.ExportedFunctions, delegate (FunctionInfo ci) { return ci.Name; });
            }

            if (cmdletPatterns != null)
            {
                IDictionary<string, List<CmdletInfo>> ft = sessionState.ModuleScope.CmdletTable;

                // Subset the existing cmdlet exports if there are any. This will be the case
                // if we're using ModuleToProcess to import a binary module which has nested modules.
                if (sessionState.Module.CompiledExports.Count > 0)
                {
                    CmdletInfo[] copy = sessionState.Module.CompiledExports.ToArray();
                    sessionState.Module.CompiledExports.Clear();

                    foreach (CmdletInfo element in copy)
                    {
                        if (doNotExportCmdlets == null
                            || !doNotExportCmdlets.Exists(cmdletName => string.Equals(element.FullName, cmdletName, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (SessionStateUtilities.MatchesAnyWildcardPattern(element.Name, cmdletPatterns, false))
                            {
                                string message = StringUtil.Format(Modules.ExportingCmdlet, element.Name);
                                cmdlet.WriteVerbose(message);
                                // Copy the cmdlet info, changing the module association to be the current module...
                                CmdletInfo exportedCmdlet = new CmdletInfo(element.Name, element.ImplementingType,
                                    element.HelpFile, null, element.Context)
                                { Module = sessionState.Module };
                                Dbg.Assert(sessionState.Module != null, "sessionState.Module should not be null by the time we're exporting cmdlets");
                                sessionState.Module.CompiledExports.Add(exportedCmdlet);
                            }
                        }
                    }
                }

                // And copy in any cmdlets imported from the nested modules...
                foreach (KeyValuePair<string, List<CmdletInfo>> entry in ft)
                {
                    CmdletInfo cmdletToImport = entry.Value[0];
                    if (doNotExportCmdlets == null
                        || !doNotExportCmdlets.Exists(cmdletName => string.Equals(cmdletToImport.FullName, cmdletName, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (SessionStateUtilities.MatchesAnyWildcardPattern(entry.Key, cmdletPatterns, false))
                        {
                            string message = StringUtil.Format(Modules.ExportingCmdlet, entry.Key);
                            cmdlet.WriteVerbose(message);
                            // Copy the cmdlet info, changing the module association to be the current module...
                            CmdletInfo exportedCmdlet = new CmdletInfo(cmdletToImport.Name, cmdletToImport.ImplementingType,
                                cmdletToImport.HelpFile, null, cmdletToImport.Context)
                            { Module = sessionState.Module };
                            Dbg.Assert(sessionState.Module != null, "sessionState.Module should not be null by the time we're exporting cmdlets");
                            sessionState.Module.CompiledExports.Add(exportedCmdlet);
                        }
                    }
                }

                SortAndRemoveDuplicates(sessionState.Module.CompiledExports, delegate (CmdletInfo ci) { return ci.Name; });
            }

            if (variablePatterns != null)
            {
                IDictionary<string, PSVariable> vt = sessionState.ModuleScope.Variables;

                foreach (KeyValuePair<string, PSVariable> entry in vt)
                {
                    // The magic variables are always private as are all-scope variables...
                    if (entry.Value.IsAllScope || Array.IndexOf(PSModuleInfo._builtinVariables, entry.Key) != -1)
                    {
                        continue;
                    }

                    if (SessionStateUtilities.MatchesAnyWildcardPattern(entry.Key, variablePatterns, false))
                    {
                        string message = StringUtil.Format(Modules.ExportingVariable, entry.Key);
                        cmdlet.WriteVerbose(message);
                        sessionState.ExportedVariables.Add(entry.Value);
                    }
                }
                SortAndRemoveDuplicates(sessionState.ExportedVariables, delegate (PSVariable v) { return v.Name; });
            }

            if (aliasPatterns != null)
            {
                IEnumerable<AliasInfo> mai = sessionState.ModuleScope.AliasTable;

                // Subset the existing alias exports if there are any. This will be the case
                // if we're using ModuleToProcess to import a binary module which has nested modules.
                if (sessionState.Module.CompiledAliasExports.Count > 0)
                {
                    AliasInfo[] copy = sessionState.Module.CompiledAliasExports.ToArray();

                    foreach (var element in copy)
                    {
                        if (SessionStateUtilities.MatchesAnyWildcardPattern(element.Name, aliasPatterns, false))
                        {
                            string message = StringUtil.Format(Modules.ExportingAlias, element.Name);
                            cmdlet.WriteVerbose(message);
                            sessionState.ExportedAliases.Add(NewAliasInfo(element, sessionState));
                        }
                    }
                }

                foreach (AliasInfo entry in mai)
                {
                    // Skip allscope items...
                    if ((entry.Options & ScopedItemOptions.AllScope) != 0)
                    {
                        continue;
                    }

                    if (SessionStateUtilities.MatchesAnyWildcardPattern(entry.Name, aliasPatterns, false))
                    {
                        string message = StringUtil.Format(Modules.ExportingAlias, entry.Name);
                        cmdlet.WriteVerbose(message);
                        sessionState.ExportedAliases.Add(NewAliasInfo(entry, sessionState));
                    }
                }

                SortAndRemoveDuplicates(sessionState.ExportedAliases, delegate (AliasInfo ci) { return ci.Name; });
            }
        }

        private static AliasInfo NewAliasInfo(AliasInfo alias, SessionStateInternal sessionState)
        {
            Dbg.Assert(alias != null, "alias should not be null");
            Dbg.Assert(sessionState != null, "sessionState should not be null");
            Dbg.Assert(sessionState.Module != null, "sessionState.Module should not be null by the time we're exporting aliases");

            // Copy the alias info, changing the module association to be the current module...
            var aliasCopy = new AliasInfo(alias.Name, alias.Definition, alias.Context, alias.Options)
            {
                Module = sessionState.Module
            };
            return aliasCopy;
        }
    } // ModuleIntrinsics

    /// <summary>
    /// Used by Modules/Snapins to provide a hook to the engine for startup initialization
    /// w.r.t compiled assembly loading.
    /// </summary>
    public interface IModuleAssemblyInitializer
    {
        /// <summary>
        /// Gets called when assembly is loaded.
        /// </summary>
        void OnImport();
    }

    /// <summary>
    /// Used by modules to provide a hook to the engine for cleanup on removal
    /// w.r.t. compiled assembly being removed.
    /// </summary>
    public interface IModuleAssemblyCleanup
    {
        /// <summary>
        /// Gets called when the binary module is unloaded.
        /// </summary>
        void OnRemove(PSModuleInfo psModuleInfo);
    }
} // System.Management.Automation
