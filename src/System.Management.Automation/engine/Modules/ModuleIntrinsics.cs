// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Text;
using System.Threading;

using Microsoft.PowerShell.Commands;
using Microsoft.Win32;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    internal static class Constants
    {
        public const string PSModulePathEnvVar = "PSModulePath";
        public const string PSUserContentPathEnvVar = "PSUserContentPath";
    }

    /// <summary>
    /// Encapsulates the basic module operations for a PowerShell engine instance...
    /// </summary>
    public class ModuleIntrinsics
    {
        /// <summary>
        /// Tracer for module analysis.
        /// </summary>
        [TraceSource("Modules", "Module loading and analysis")]
        internal static readonly PSTraceSource Tracer = PSTraceSource.GetTracer("Modules", "Module loading and analysis");

        // The %WINDIR%\System32\WindowsPowerShell\v1.0\Modules module path,
        // to load forward compatible Windows PowerShell modules from
        private static readonly string s_windowsPowerShellPSHomeModulePath =
            Path.Combine(System.Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "Modules");

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
        /// Create a new module object from a scriptblock specifying the path to set for the module.
        /// </summary>
        /// <param name="name">The name of the module.</param>
        /// <param name="path">The path where the module is rooted.</param>
        /// <param name="scriptBlock">
        /// ScriptBlock that is executed to initialize the module...
        /// </param>
        /// <param name="arguments">
        /// The arguments to pass to the scriptblock used to initialize the module
        /// </param>
        /// <param name="ss">The session state instance to use for this module - may be null.</param>
        /// <param name="results">The results produced from evaluating the scriptblock.</param>
        /// <returns>The newly created module info object.</returns>
        internal PSModuleInfo CreateModule(string name, string path, ScriptBlock scriptBlock, SessionState ss, out List<object> results, params object[] arguments)
        {
            return CreateModuleImplementation(name, path, scriptBlock, null, ss, null, out results, arguments);
        }

        /// <summary>
        /// Create a new module object from a ScriptInfo object.
        /// </summary>
        /// <param name="path">The path where the module is rooted.</param>
        /// <param name="scriptInfo">The script info to use to create the module.</param>
        /// <param name="scriptPosition">The position for the command that loaded this module.</param>
        /// <param name="arguments">Optional arguments to pass to the script while executing.</param>
        /// <param name="ss">The session state instance to use for this module - may be null.</param>
        /// <param name="privateData">The private data to use for this module - may be null.</param>
        /// <returns>The constructed module object.</returns>
        internal PSModuleInfo CreateModule(string path, ExternalScriptInfo scriptInfo, IScriptExtent scriptPosition, SessionState ss, object privateData, params object[] arguments)
        {
            List<object> result;
            return CreateModuleImplementation(ModuleIntrinsics.GetModuleName(path), path, scriptInfo, scriptPosition, ss, privateData, out result, arguments);
        }

        /// <summary>
        /// Create a new module object from code specifying the path to set for the module.
        /// </summary>
        /// <param name="name">The name of the module.</param>
        /// <param name="path">The path to use for the module root.</param>
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
        /// <param name="ss">The session state instance to use for this module - may be null.</param>
        /// <param name="privateData">The private data to use for this module - may be null.</param>
        /// <returns>The created module.</returns>
        private PSModuleInfo CreateModuleImplementation(string name, string path, object moduleCode, IScriptExtent scriptPosition, SessionState ss, object privateData, out List<object> result, params object[] arguments)
        {
            ScriptBlock sb;

            // By default the top-level scope in a session state object is the global scope for the instance.
            // For modules, we need to set its global scope to be another scope object and, chain the top
            // level scope for this sessionstate instance to be the parent. The top level scope for this ss is the
            // script scope for the ss.

            // Allocate the session state instance for this module.
            ss ??= new SessionState(_context, true, true);

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
                module.LanguageMode = sb.LanguageMode;

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
                        args: arguments ?? Array.Empty<object>());
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
        /// <param name="sb">The scriptblock to bind.</param>
        /// <param name="linkToGlobal">Whether it should be linked to the global session state or not.</param>
        /// <returns>A new scriptblock.</returns>
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
            moduleName ??= string.Empty;

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
                patterns ??= new string[] { "*" };

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

            return modulesMatched.OrderBy(static m => m.Name).ToList();
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

            return modulesMatched.OrderBy(static m => m.Name).ToList();
        }

        /// <summary>
        /// Check if a given module info object matches a given module specification.
        /// </summary>
        /// <param name="moduleInfo">The module info object to check.</param>
        /// <param name="moduleSpec">The module specification to match the module info object against.</param>
        /// <param name="skipNameCheck">True if we should skip the name check on the module specification.</param>
        /// <returns>True if the module info object meets all the constraints on the module specification, false otherwise.</returns>
        internal static bool IsModuleMatchingModuleSpec(
            PSModuleInfo moduleInfo,
            ModuleSpecification moduleSpec,
            bool skipNameCheck = false)
        {
            return IsModuleMatchingModuleSpec(out ModuleMatchFailure matchFailureReason, moduleInfo, moduleSpec, skipNameCheck);
        }

        /// <summary>
        /// Check if a given module info object matches a given module specification.
        /// </summary>
        /// <param name="matchFailureReason">The constraint that caused the match failure, if any.</param>
        /// <param name="moduleInfo">The module info object to check.</param>
        /// <param name="moduleSpec">The module specification to match the module info object against.</param>
        /// <param name="skipNameCheck">True if we should skip the name check on the module specification.</param>
        /// <returns>True if the module info object meets all the constraints on the module specification, false otherwise.</returns>
        internal static bool IsModuleMatchingModuleSpec(
            out ModuleMatchFailure matchFailureReason,
            PSModuleInfo moduleInfo,
            ModuleSpecification moduleSpec,
            bool skipNameCheck = false)
        {
            if (moduleSpec == null)
            {
                matchFailureReason = ModuleMatchFailure.NullModuleSpecification;
                return false;
            }

            return IsModuleMatchingConstraints(
                out matchFailureReason,
                moduleInfo,
                skipNameCheck ? null : moduleSpec.Name,
                moduleSpec.Guid,
                moduleSpec.RequiredVersion,
                moduleSpec.Version,
                moduleSpec.MaximumVersion == null ? null : ModuleCmdletBase.GetMaximumVersion(moduleSpec.MaximumVersion));
        }

        /// <summary>
        /// Check if a given module info object matches the given constraints.
        /// Constraints given as null are ignored.
        /// </summary>
        /// <param name="moduleInfo">The module info object to check.</param>
        /// <param name="name">The name or normalized absolute path of the expected module.</param>
        /// <param name="guid">The guid of the expected module.</param>
        /// <param name="requiredVersion">The required version of the expected module.</param>
        /// <param name="minimumVersion">The minimum required version of the expected module.</param>
        /// <param name="maximumVersion">The maximum required version of the expected module.</param>
        /// <returns>True if the module info object matches all given constraints, false otherwise.</returns>
        internal static bool IsModuleMatchingConstraints(
            PSModuleInfo moduleInfo,
            string name = null,
            Guid? guid = null,
            Version requiredVersion = null,
            Version minimumVersion = null,
            Version maximumVersion = null)
        {
            return IsModuleMatchingConstraints(
                out ModuleMatchFailure matchFailureReason,
                moduleInfo,
                name,
                guid,
                requiredVersion,
                minimumVersion,
                maximumVersion);
        }

        /// <summary>
        /// Check if a given module info object matches the given constraints.
        /// Constraints given as null are ignored.
        /// </summary>
        /// <param name="matchFailureReason">The reason for the module constraint match failing.</param>
        /// <param name="moduleInfo">The module info object to check.</param>
        /// <param name="name">The name or normalized absolute path of the expected module.</param>
        /// <param name="guid">The guid of the expected module.</param>
        /// <param name="requiredVersion">The required version of the expected module.</param>
        /// <param name="minimumVersion">The minimum required version of the expected module.</param>
        /// <param name="maximumVersion">The maximum required version of the expected module.</param>
        /// <returns>True if the module info object matches all the constraints on the module specification, false otherwise.</returns>
        internal static bool IsModuleMatchingConstraints(
            out ModuleMatchFailure matchFailureReason,
            PSModuleInfo moduleInfo,
            string name,
            Guid? guid,
            Version requiredVersion,
            Version minimumVersion,
            Version maximumVersion)
        {
            // Define that a null module does not meet any constraints
            if (moduleInfo == null)
            {
                matchFailureReason = ModuleMatchFailure.NullModule;
                return false;
            }

            return AreModuleFieldsMatchingConstraints(
                out matchFailureReason,
                moduleInfo.Name,
                moduleInfo.Path,
                moduleInfo.Guid,
                moduleInfo.Version,
                name,
                guid,
                requiredVersion,
                minimumVersion,
                maximumVersion
            );
        }

        /// <summary>
        /// Check that given module fields meet any given constraints.
        /// </summary>
        /// <param name="moduleName">The name of the module to check.</param>
        /// <param name="modulePath">The path of the module to check.</param>
        /// <param name="moduleGuid">The GUID of the module to check.</param>
        /// <param name="moduleVersion">The version of the module to check.</param>
        /// <param name="requiredName">The name or normalized absolute path the module must have, if any.</param>
        /// <param name="requiredGuid">The GUID the module must have, if any.</param>
        /// <param name="requiredVersion">The exact version the module must have, if any.</param>
        /// <param name="minimumRequiredVersion">The minimum version the module may have, if any.</param>
        /// <param name="maximumRequiredVersion">The maximum version the module may have, if any.</param>
        /// <returns>True if the module parameters match all given constraints, false otherwise.</returns>
        internal static bool AreModuleFieldsMatchingConstraints(
            string moduleName = null,
            string modulePath = null,
            Guid? moduleGuid = null,
            Version moduleVersion = null,
            string requiredName = null,
            Guid? requiredGuid = null,
            Version requiredVersion = null,
            Version minimumRequiredVersion = null,
            Version maximumRequiredVersion = null)
        {
            return AreModuleFieldsMatchingConstraints(
                out ModuleMatchFailure matchFailureReason,
                moduleName,
                modulePath,
                moduleGuid,
                moduleVersion,
                requiredName,
                requiredGuid,
                requiredVersion,
                minimumRequiredVersion,
                maximumRequiredVersion);
        }

        /// <summary>
        /// Check that given module fields meet any given constraints.
        /// </summary>
        /// <param name="matchFailureReason">The reason the match failed, if any.</param>
        /// <param name="moduleName">The name of the module to check.</param>
        /// <param name="modulePath">The path of the module to check.</param>
        /// <param name="moduleGuid">The GUID of the module to check.</param>
        /// <param name="moduleVersion">The version of the module to check.</param>
        /// <param name="requiredName">The name or normalized absolute path the module must have, if any.</param>
        /// <param name="requiredGuid">The GUID the module must have, if any.</param>
        /// <param name="requiredVersion">The exact version the module must have, if any.</param>
        /// <param name="minimumRequiredVersion">The minimum version the module may have, if any.</param>
        /// <param name="maximumRequiredVersion">The maximum version the module may have, if any.</param>
        /// <returns>True if the module parameters match all given constraints, false otherwise.</returns>
        internal static bool AreModuleFieldsMatchingConstraints(
            out ModuleMatchFailure matchFailureReason,
            string moduleName,
            string modulePath,
            Guid? moduleGuid,
            Version moduleVersion,
            string requiredName,
            Guid? requiredGuid,
            Version requiredVersion,
            Version minimumRequiredVersion,
            Version maximumRequiredVersion)
        {
            // If a name is required, check that it matches.
            // A required module name may also be an absolute path, so check it against the given module's path as well.
            if (requiredName != null
                && !requiredName.Equals(moduleName, StringComparison.OrdinalIgnoreCase)
                && !MatchesModulePath(modulePath, requiredName))
            {
                matchFailureReason = ModuleMatchFailure.Name;
                return false;
            }

            // If a GUID is required, check it matches
            if (requiredGuid != null && !requiredGuid.Equals(moduleGuid))
            {
                matchFailureReason = ModuleMatchFailure.Guid;
                return false;
            }

            // Check the versions
            return IsVersionMatchingConstraints(out matchFailureReason, moduleVersion, requiredVersion, minimumRequiredVersion, maximumRequiredVersion);
        }

        /// <summary>
        /// Check that a given module version matches the required or minimum/maximum version constraints.
        /// Null constraints are not checked.
        /// </summary>
        /// <param name="version">The module version to check. Must not be null.</param>
        /// <param name="requiredVersion">The version that the given version must be, if not null.</param>
        /// <param name="minimumVersion">The minimum version that the given version must be greater than or equal to, if not null.</param>
        /// <param name="maximumVersion">The maximum version that the given version must be less then or equal to, if not null.</param>
        /// <returns>
        /// True if the version matches the required version, or if it is absent, is between the minimum and maximum versions, and false otherwise.
        /// </returns>
        internal static bool IsVersionMatchingConstraints(
            Version version,
            Version requiredVersion = null,
            Version minimumVersion = null,
            Version maximumVersion = null)
        {
            return IsVersionMatchingConstraints(out ModuleMatchFailure matchFailureReason, version, requiredVersion, minimumVersion, maximumVersion);
        }

        /// <summary>
        /// Check that a given module version matches the required or minimum/maximum version constraints.
        /// Null constraints are not checked.
        /// </summary>
        /// <param name="matchFailureReason">The reason why the match failed.</param>
        /// <param name="version">The module version to check. Must not be null.</param>
        /// <param name="requiredVersion">The version that the given version must be, if not null.</param>
        /// <param name="minimumVersion">The minimum version that the given version must be greater than or equal to, if not null.</param>
        /// <param name="maximumVersion">The maximum version that the given version must be less then or equal to, if not null.</param>
        /// <returns>
        /// True if the version matches the required version, or if it is absent, is between the minimum and maximum versions, and false otherwise.
        /// </returns>
        internal static bool IsVersionMatchingConstraints(
            out ModuleMatchFailure matchFailureReason,
            Version version,
            Version requiredVersion = null,
            Version minimumVersion = null,
            Version maximumVersion = null)
        {
            Dbg.Assert(version != null, $"Caller to verify that {nameof(version)} is not null");

            // If a RequiredVersion is given it overrides other version settings
            if (requiredVersion != null)
            {
                matchFailureReason = ModuleMatchFailure.RequiredVersion;
                return requiredVersion.Equals(version);
            }

            // Check the version is at least the minimum version
            if (minimumVersion != null && version < minimumVersion)
            {
                matchFailureReason = ModuleMatchFailure.MinimumVersion;
                return false;
            }

            // Check the version is at most the maximum version
            if (maximumVersion != null && version > maximumVersion)
            {
                matchFailureReason = ModuleMatchFailure.MaximumVersion;
                return false;
            }

            matchFailureReason = ModuleMatchFailure.None;
            return true;
        }

        /// <summary>
        /// Checks whether a given module path is the same as
        /// a required path.
        /// </summary>
        /// <param name="modulePath">The path of the module whose path to check. This must be the path to the module file (.psd1, .psm1, .dll, etc).</param>
        /// <param name="requiredPath">The path of the required module. This may be the module directory path or the file path. Only normalized absolute paths will work for this.</param>
        /// <returns>True if the module path matches the required path, false otherwise.</returns>
        internal static bool MatchesModulePath(string modulePath, string requiredPath)
        {
            Dbg.Assert(requiredPath != null, $"Caller to verify that {nameof(requiredPath)} is not null");

            if (modulePath == null)
            {
                return false;
            }

#if UNIX
            const StringComparison strcmp = StringComparison.Ordinal;
#else
            const StringComparison strcmp = StringComparison.OrdinalIgnoreCase;
#endif

            // We must check modulePath (e.g. /path/to/module/module.psd1) against several possibilities:
            // 1. "/path/to/module"                 - Module dir path
            // 2. "/path/to/module/module.psd1"     - Module root file path
            // 3. "/path/to/module/2.1/module.psd1" - Versioned module path

            // If the required module just matches the module path (case 1), we are done
            if (modulePath.Equals(requiredPath, strcmp))
            {
                return true;
            }

            // At this point we are looking for the module directory (case 2 or 3).
            // We can some allocations here if module path doesn't sit under the required path
            // (the required path may still refer to some nested module though)
            if (!modulePath.StartsWith(requiredPath, strcmp))
            {
                return false;
            }

            string moduleDirPath = Path.GetDirectoryName(modulePath);

            // The module itself may be in a versioned directory (case 3)
            if (Version.TryParse(Path.GetFileName(moduleDirPath), out _))
            {
                moduleDirPath = Path.GetDirectoryName(moduleDirPath);
            }

            return moduleDirPath.Equals(requiredPath, strcmp);
        }

        /// <summary>
        /// Takes the name of a module as used in a module specification
        /// and either returns it as a simple name (if it was a simple name)
        /// or a fully qualified, PowerShell-resolved path.
        /// </summary>
        /// <param name="moduleName">The name or path of the module from the specification.</param>
        /// <param name="basePath">The path to base relative paths off.</param>
        /// <param name="executionContext">The current execution context.</param>
        /// <returns>
        /// The simple module name if the given one was simple,
        /// otherwise a fully resolved, absolute path to the module.
        /// </returns>
        /// <remarks>
        /// 2018-11-09 rjmholt:
        /// There are several, possibly inconsistent, path handling mechanisms
        /// in the module cmdlets. After looking through all of them and seeing
        /// they all make some assumptions about their caller I wrote this method.
        /// Hopefully we can find a standard path resolution API to settle on.
        /// </remarks>
        internal static string NormalizeModuleName(
            string moduleName,
            string basePath,
            ExecutionContext executionContext)
        {
            if (moduleName == null)
            {
                return null;
            }

            // Check whether the module is a path -- if not, it is a simple name and we just return it.
            if (!IsModuleNamePath(moduleName))
            {
                return moduleName;
            }

            // Standardize directory separators -- Path.IsPathRooted() will return false for "\path\here" on *nix and for "/path/there" on Windows
            moduleName = moduleName.Replace(StringLiterals.AlternatePathSeparator, StringLiterals.DefaultPathSeparator);

            // Note: Path.IsFullyQualified("\default\root") is false on Windows, but Path.IsPathRooted returns true
            if (!Path.IsPathRooted(moduleName))
            {
                moduleName = Path.Join(basePath, moduleName);
            }

            // Use the PowerShell filesystem provider to fully resolve the path
            // If there is a problem, null could be returned -- so default back to the pre-normalized path
            string normalizedPath = ModuleCmdletBase.GetResolvedPath(moduleName, executionContext)?.TrimEnd(StringLiterals.DefaultPathSeparator);

            // ModuleCmdletBase.GetResolvePath will return null in the unlikely event that it failed.
            // If it does, we return the fully qualified path generated before.
            return normalizedPath ?? Path.GetFullPath(moduleName);
        }

        /// <summary>
        /// Check if a given module name is a path to a module rather than a simple name.
        /// </summary>
        /// <param name="moduleName">The module name to check.</param>
        /// <returns>True if the module name is a path, false otherwise.</returns>
        internal static bool IsModuleNamePath(string moduleName)
        {
            return moduleName.Contains(StringLiterals.DefaultPathSeparator)
                || moduleName.Contains(StringLiterals.AlternatePathSeparator)
                || moduleName.Equals("..")
                || moduleName.Equals(".");
        }

        internal static Version GetManifestModuleVersion(string manifestPath)
        {
            try
            {
                Hashtable dataFileSetting =
                    PsUtils.GetModuleManifestProperties(
                        manifestPath,
                        PsUtils.ManifestModuleVersionPropertyName);

                object versionValue = dataFileSetting["ModuleVersion"];
                if (versionValue != null)
                {
                    Version moduleVersion;
                    if (LanguagePrimitives.TryConvertTo(versionValue, out moduleVersion))
                    {
                        return moduleVersion;
                    }
                }
            }
            catch (PSInvalidOperationException) { }

            return new Version(0, 0);
        }

        internal static Guid GetManifestGuid(string manifestPath)
        {
            try
            {
                Hashtable dataFileSetting =
                    PsUtils.GetModuleManifestProperties(
                        manifestPath,
                        PsUtils.ManifestGuidPropertyName);

                object guidValue = dataFileSetting["GUID"];
                if (guidValue != null)
                {
                    Guid guidID;
                    if (LanguagePrimitives.TryConvertTo(guidValue, out guidID))
                    {
                        return guidID;
                    }
                }
            }
            catch (PSInvalidOperationException) { }

            return new Guid();
        }

        internal static ExperimentalFeature[] GetExperimentalFeature(string manifestPath)
        {
            try
            {
                Hashtable dataFileSetting =
                    PsUtils.GetModuleManifestProperties(
                        manifestPath,
                        PsUtils.ManifestPrivateDataPropertyName);

                object privateData = dataFileSetting["PrivateData"];
                if (privateData is Hashtable hashData && hashData["PSData"] is Hashtable psData)
                {
                    object expFeatureValue = psData["ExperimentalFeatures"];
                    if (expFeatureValue != null &&
                        LanguagePrimitives.TryConvertTo(expFeatureValue, out Hashtable[] features) &&
                        features.Length > 0)
                    {
                        string moduleName = ModuleIntrinsics.GetModuleName(manifestPath);
                        var expFeatureList = new List<ExperimentalFeature>();
                        foreach (Hashtable feature in features)
                        {
                            string featureName = feature["Name"] as string;
                            if (string.IsNullOrEmpty(featureName))
                            {
                                continue;
                            }

                            if (ExperimentalFeature.IsModuleFeatureName(featureName, moduleName))
                            {
                                string featureDescription = feature["Description"] as string;
                                expFeatureList.Add(new ExperimentalFeature(featureName, featureDescription, manifestPath,
                                                                           ExperimentalFeature.IsEnabled(featureName)));
                            }
                        }

                        return expFeatureList.ToArray();
                    }
                }
            }
            catch (PSInvalidOperationException) { }

            return Array.Empty<ExperimentalFeature>();
        }

        // The extensions of all of the files that can be processed with Import-Module, put the ni.dll in front of .dll to have higher priority to be loaded.
        internal static readonly string[] PSModuleProcessableExtensions = new string[]
        {
            StringLiterals.PowerShellDataFileExtension,
            StringLiterals.PowerShellScriptFileExtension,
            StringLiterals.PowerShellModuleFileExtension,
            StringLiterals.PowerShellCmdletizationFileExtension,
            StringLiterals.PowerShellNgenAssemblyExtension,
            StringLiterals.PowerShellILAssemblyExtension,
            StringLiterals.PowerShellILExecutableExtension,
        };

        // A list of the extensions to check for implicit module loading and discovery, put the ni.dll in front of .dll to have higher priority to be loaded.
        internal static readonly string[] PSModuleExtensions = new string[]
        {
            StringLiterals.PowerShellDataFileExtension,
            StringLiterals.PowerShellModuleFileExtension,
            StringLiterals.PowerShellCmdletizationFileExtension,
            StringLiterals.PowerShellNgenAssemblyExtension,
            StringLiterals.PowerShellILAssemblyExtension,
            StringLiterals.PowerShellILExecutableExtension,
        };

        // A list of the extensions to check for required assemblies.
        internal static readonly string[] ProcessableAssemblyExtensions = new string[]
        {
            StringLiterals.PowerShellNgenAssemblyExtension,
            StringLiterals.PowerShellILAssemblyExtension,
            StringLiterals.PowerShellILExecutableExtension
        };

        /// <summary>
        /// Returns true if the extension is one of the module extensions...
        /// </summary>
        /// <param name="extension">The extension to check.</param>
        /// <returns>True if it was a module extension...</returns>
        internal static bool IsPowerShellModuleExtension(string extension)
        {
            foreach (string ext in PSModuleProcessableExtensions)
            {
                if (extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the module name from module path.
        /// </summary>
        /// <param name="path">The path to the module.</param>
        /// <returns>The module name.</returns>
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
        /// Gets the personal module path.
        /// </summary>
        /// <returns>Personal module path.</returns>
        internal static string GetPersonalModulePath()
        {
            return Path.Combine(Utils.GetPSContentPath, "Modules");
        }

        /// <summary>
        /// Gets the PSHome module path, as known as the "system wide module path" in windows powershell.
        /// </summary>
        /// <returns>The PSHome module path.</returns>
        internal static string GetPSHomeModulePath()
        {
            if (s_psHomeModulePath != null)
            {
                return s_psHomeModulePath;
            }

            try
            {
                string psHome = Utils.DefaultPowerShellAppBase;
#if !UNIX
                // Win8: 584267 Powershell Modules are listed twice in x86, and cannot be removed.
                // This happens because 'ModuleTable' uses Path as the key and x86 WinPS has "SysWOW64" in its $PSHOME.
                // Because of this, the module that is getting loaded during startup (through LocalRunspace) is using
                // "SysWow64" in the key. Later, when 'Import-Module' is called, it loads the module using ""System32"
                // in the key.
                // For the cross-platform PowerShell, a user can choose to install it under "C:\Windows\SysWOW64", and
                // thus it may have the same problem as described above. So we keep this line of code.
                psHome = psHome.ToLowerInvariant().Replace(@"\syswow64\", @"\system32\");
#endif
                Interlocked.CompareExchange(ref s_psHomeModulePath, Path.Combine(psHome, "Modules"), null);
            }
            catch (System.Security.SecurityException)
            {
            }

            return s_psHomeModulePath;
        }

        private static string s_psHomeModulePath;

        /// <summary>
        /// Get the module path that is shared among different users.
        /// It's known as "Program Files" module path in windows powershell.
        /// </summary>
        /// <returns></returns>
        internal static string GetSharedModulePath()
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

#if !UNIX
        /// <summary>
        /// Get the path to the Windows PowerShell module directory under the
        /// System32 directory on Windows (the Windows PowerShell $PSHOME).
        /// </summary>
        /// <returns>The path of the Windows PowerShell system module directory.</returns>
        internal static string GetWindowsPowerShellPSHomeModulePath()
        {
            if (!string.IsNullOrEmpty(InternalTestHooks.TestWindowsPowerShellPSHomeLocation))
            {
                return InternalTestHooks.TestWindowsPowerShellPSHomeLocation;
            }

            return s_windowsPowerShellPSHomeModulePath;
        }
#endif

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
            string[] substrings = pathToScan.Split(Path.PathSeparator, StringSplitOptions.None); // we want to process empty entries
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
                foreach (string subPathToAdd in pathToAdd.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)) // in case pathToAdd is a 'combined path' (semicolon-separated)
                {
                    int position = PathContainsSubstring(result.ToString(), subPathToAdd); // searching in effective 'result' value ensures that possible duplicates in pathsToAdd are handled correctly
                    if (position == -1) // subPathToAdd not found - add it
                    {
                        if (insertPosition == -1 || insertPosition > basePath.Length) // append subPathToAdd to the end
                        {
                            bool endsWithPathSeparator = false;
                            if (result.Length > 0)
                            {
                                endsWithPathSeparator = (result[result.Length - 1] == Path.PathSeparator);
                            }

                            if (endsWithPathSeparator)
                            {
                                result.Append(subPathToAdd);
                            }
                            else
                            {
                                result.Append(Path.PathSeparator + subPathToAdd);
                            }
                        }
                        else if (insertPosition > result.Length)
                        {
                            // handle case where path is a singleton with no path separator already
                            result.Append(Path.PathSeparator).Append(subPathToAdd);
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
        /// The available module path scopes.
        /// </summary>
        public enum PSModulePathScope
        {
            /// <summary>The users module path.</summary>
            User,

            /// <summary>The Builtin module path. This is where PowerShell is installed (PSHOME).</summary>
            Builtin,

            /// <summary>The machine module path. This is the shared location for all users of the system.</summary>
            Machine
        }

        /// <summary>
        /// Retrieve the current PSModulePath for the specified scope.
        /// </summary>
        /// <param name="scope">The scope of module path to retrieve. This can be User, Builtin, or Machine.</param>
        /// <returns>The string representing the requested module path type.</returns>
        public static string GetPSModulePath(PSModulePathScope scope)
        {
            if (scope == PSModulePathScope.User)
            {
                return GetPersonalModulePath();
            }
            else if (scope == PSModulePathScope.Builtin)
            {
                return GetPSHomeModulePath();
            }
            else
            {
                return GetSharedModulePath();
            }
        }

        /// <summary>
        /// Checks the various PSModulePath environment string and returns PSModulePath string as appropriate.
        /// </summary>
        public static string GetModulePath(string currentProcessModulePath, string hklmMachineModulePath, string hkcuUserModulePath)
        {
            string personalModulePath = GetPersonalModulePath();
            string sharedModulePath = GetSharedModulePath(); // aka <Program Files> location
            string psHomeModulePath = GetPSHomeModulePath(); // $PSHome\Modules location

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

                if (string.IsNullOrEmpty(currentProcessModulePath))
                {
                    currentProcessModulePath ??= string.Empty;
                }
                else
                {
                    currentProcessModulePath += Path.PathSeparator;
                }

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
            else
            {
                string personalModulePathToUse = string.IsNullOrEmpty(hkcuUserModulePath) ? personalModulePath : hkcuUserModulePath;
                string systemModulePathToUse = string.IsNullOrEmpty(hklmMachineModulePath) ? psHomeModulePath : hklmMachineModulePath;

                // Maintain order of the paths, but ahead of any existing paths:
                // personalModulePath
                // sharedModulePath
                // systemModulePath

                int insertIndex = 0;

                currentProcessModulePath = UpdatePath(currentProcessModulePath, personalModulePathToUse, ref insertIndex);
                currentProcessModulePath = UpdatePath(currentProcessModulePath, sharedModulePath, ref insertIndex);
                currentProcessModulePath = UpdatePath(currentProcessModulePath, systemModulePathToUse, ref insertIndex);
            }

            return currentProcessModulePath;
        }

        private static string UpdatePath(string path, string pathToAdd, ref int insertIndex)
        {
            if (!string.IsNullOrEmpty(pathToAdd))
            {
                path = AddToPath(path, pathToAdd, insertIndex);
                insertIndex = path.IndexOf(Path.PathSeparator, PathContainsSubstring(path, pathToAdd));
                if (insertIndex != -1)
                {
                    // advance past the path separator
                    insertIndex++;
                }
            }
            return path;
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

#if !UNIX
        /// <summary>
        /// Returns a PSModulePath suitable for Windows PowerShell by removing PowerShell's specific
        /// paths from current PSModulePath.
        /// </summary>
        /// <returns>
        /// Returns appropriate PSModulePath for Windows PowerShell.
        /// </returns>
        internal static string GetWindowsPowerShellModulePath()
        {
            string currentModulePath = GetModulePath();

            if (currentModulePath == null)
            {
                return null;
            }

            // PowerShell specific paths including if set in powershell.config.json file we want to exclude
            var excludeModulePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                GetPersonalModulePath(),
                GetSharedModulePath(),
                GetPSHomeModulePath(),
                PowerShellConfig.Instance.GetModulePath(ConfigScope.AllUsers),
                PowerShellConfig.Instance.GetModulePath(ConfigScope.CurrentUser)
            };

            var modulePathList = new List<string>();
            foreach (var path in currentModulePath.Split(';', StringSplitOptions.TrimEntries))
            {
                if (!excludeModulePaths.Contains(path))
                {
                    // make sure this module path is Not part of other PS Core installation
                    var possiblePwshDir = Path.GetDirectoryName(path);

                    if (string.IsNullOrEmpty(possiblePwshDir))
                    {
                        // i.e. module dir is in the drive root
                        modulePathList.Add(path);
                    }
                    else
                    {
                        if (!File.Exists(Path.Combine(possiblePwshDir, "pwsh.dll")))
                        {
                            modulePathList.Add(path);
                        }
                    }
                }
            }

            return string.Join(Path.PathSeparator, modulePathList);
        }
#endif

        /// <summary>
        /// Checks if $env:PSModulePath is not set and sets it as appropriate. Note - because these
        /// strings go through the provider, we need to escape any wildcards before passing them
        /// along.
        /// </summary>
        private static string SetModulePath()
        {
            string currentModulePath = GetExpandedEnvironmentVariable(Constants.PSModulePathEnvVar, EnvironmentVariableTarget.Process);
#if !UNIX
            // if the current process and user env vars are the same, it means we need to append the machine one as it's incomplete
            // otherwise, the user modified it and we should use the process one
            if (string.CompareOrdinal(GetExpandedEnvironmentVariable(Constants.PSModulePathEnvVar, EnvironmentVariableTarget.User), currentModulePath) == 0)
            {
                currentModulePath = currentModulePath + Path.PathSeparator + GetExpandedEnvironmentVariable(Constants.PSModulePathEnvVar, EnvironmentVariableTarget.Machine);
            }
#endif
            string allUsersModulePath = PowerShellConfig.Instance.GetModulePath(ConfigScope.AllUsers);
            string personalModulePath = PowerShellConfig.Instance.GetModulePath(ConfigScope.CurrentUser) ?? GetPersonalModulePath();
            string newModulePathString = GetModulePath(currentModulePath, allUsersModulePath, personalModulePath);

            if (!string.IsNullOrEmpty(newModulePathString))
            {
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
        /// <returns>The module path as an array of strings.</returns>
        internal static IEnumerable<string> GetModulePath(bool includeSystemModulePath, ExecutionContext context)
        {
            string modulePathString = Environment.GetEnvironmentVariable(Constants.PSModulePathEnvVar) ?? SetModulePath();

            HashSet<string> processedPathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(modulePathString))
            {
                foreach (string envPath in modulePathString.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
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

#nullable enable
        private static void SortAndRemoveDuplicates<T>(List<T> input, Func<T, string> keyGetter)
        {
            Dbg.Assert(input is not null, "Caller should verify that input != null");

            input.Sort(
                (T x, T y) =>
                {
                    string kx = keyGetter(x);
                    string ky = keyGetter(y);
                    return string.Compare(kx, ky, StringComparison.OrdinalIgnoreCase);
                }
            );

            string? previousKey = null;
            input.RemoveAll(ShouldRemove);

            bool ShouldRemove(T item)
            {
                string currentKey = keyGetter(item);
                bool match = previousKey is not null
                    && currentKey.Equals(previousKey, StringComparison.OrdinalIgnoreCase);
                previousKey = currentKey;
                return match;
            }
        }
#nullable restore

        /// <summary>
        /// Mark stuff to be exported from the current environment using the various patterns.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling this method.</param>
        /// <param name="sessionState">The session state instance to do the exports on.</param>
        /// <param name="functionPatterns">Patterns describing the functions to export.</param>
        /// <param name="cmdletPatterns">Patterns describing the cmdlets to export.</param>
        /// <param name="aliasPatterns">Patterns describing the aliases to export.</param>
        /// <param name="variablePatterns">Patterns describing the variables to export.</param>
        /// <param name="doNotExportCmdlets">List of Cmdlets that will not be exported, even if they match in cmdletPatterns.</param>
        internal static void ExportModuleMembers(
            PSCmdlet cmdlet,
            SessionStateInternal sessionState,
            List<WildcardPattern> functionPatterns,
            List<WildcardPattern> cmdletPatterns,
            List<WildcardPattern> aliasPatterns,
            List<WildcardPattern> variablePatterns,
            List<string> doNotExportCmdlets)
        {
            // If this cmdlet is called, then mark that the export list should be used for exporting
            // module members...

            sessionState.UseExportList = true;

            if (functionPatterns != null)
            {
                sessionState.FunctionsExported = true;
                if (PatternContainsWildcard(functionPatterns))
                {
                    sessionState.FunctionsExportedWithWildcard = true;
                }

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

                SortAndRemoveDuplicates(sessionState.ExportedFunctions, static (FunctionInfo ci) => ci.Name);
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

                SortAndRemoveDuplicates(sessionState.Module.CompiledExports, static (CmdletInfo ci) => ci.Name);
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

                SortAndRemoveDuplicates(sessionState.ExportedVariables, static (PSVariable v) => v.Name);
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

                SortAndRemoveDuplicates(sessionState.ExportedAliases, static (AliasInfo ci) => ci.Name);
            }
        }

        /// <summary>
        /// Checks pattern list for wildcard characters.
        /// </summary>
        /// <param name="list">Pattern list.</param>
        /// <returns>True if pattern contains '*'.</returns>
        internal static bool PatternContainsWildcard(List<WildcardPattern> list)
        {
            if (list != null)
            {
                foreach (var item in list)
                {
                    if (WildcardPattern.ContainsWildcardCharacters(item.Pattern))
                    {
                        return true;
                    }
                }
            }

            return false;
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
    }

    /// <summary>
    /// Enumeration of reasons for a failure to match a module by constraints.
    /// </summary>
    internal enum ModuleMatchFailure
    {
        /// <summary>Match did not fail.</summary>
        None,

        /// <summary>Match failed because the module was null.</summary>
        NullModule,

        /// <summary>Module name did not match.</summary>
        Name,

        /// <summary>Module GUID did not match.</summary>
        Guid,

        /// <summary>Module version did not match the required version.</summary>
        RequiredVersion,

        /// <summary>Module version was lower than the minimum version.</summary>
        MinimumVersion,

        /// <summary>Module version was greater than the maximum version.</summary>
        MaximumVersion,

        /// <summary>The module specification passed in was null.</summary>
        NullModuleSpecification,
    }

#nullable enable
    /// <summary>
    /// Used by Modules/Snapins to provide a hook to the engine for startup initialization
    /// w.r.t compiled assembly loading.
    /// </summary>
#nullable enable
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
}
