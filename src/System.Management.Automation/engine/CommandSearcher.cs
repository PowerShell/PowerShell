// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Internal;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Used to enumerate the commands on the system that match the specified
    /// command name.
    /// </summary>
    internal class CommandSearcher : IEnumerable<CommandInfo>, IEnumerator<CommandInfo>
    {
        /// <summary>
        /// Constructs a command searching enumerator that resolves the location
        /// to a command using a standard algorithm.
        /// </summary>
        /// <param name="commandName">
        /// The name of the command to look for.
        /// </param>
        /// <param name="options">
        /// Determines which types of commands glob resolution of the name will take place on.
        /// </param>
        /// <param name="commandTypes">
        /// The types of commands to look for.
        /// </param>
        /// <param name="context">
        /// The execution context for this engine instance...
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="commandName"/> is null or empty.
        /// </exception>
        internal CommandSearcher(
            string commandName,
            SearchResolutionOptions options,
            CommandTypes commandTypes,
            ExecutionContext context)
        {
            Diagnostics.Assert(context != null, "caller to verify context is not null");
            Diagnostics.Assert(!string.IsNullOrEmpty(commandName), "caller to verify commandName is valid");

            _commandName = commandName;
            _context = context;
            _commandResolutionOptions = options;
            _commandTypes = commandTypes;

            // Initialize the enumerators
            this.Reset();
        }

        /// <summary>
        /// Gets an instance of a command enumerator.
        /// </summary>
        /// <returns>
        /// An instance of this class as IEnumerator.
        /// </returns>
        IEnumerator<CommandInfo> IEnumerable<CommandInfo>.GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        /// <summary>
        /// Moves the enumerator to the next command match. Public for IEnumerable.
        /// </summary>
        /// <returns>
        /// true if there was another command that matches, false otherwise.
        /// </returns>
        public bool MoveNext()
        {
            _currentMatch = null;

            if (_currentState == SearchState.SearchingAliases)
            {
                _currentMatch = SearchForAliases();

                // Why don't we check IsVisible on other scoped items?
                if (_currentMatch != null && SessionState.IsVisible(_commandOrigin, _currentMatch))
                {
                    return true;
                }

                // Make sure Current doesn't return an alias that isn't visible
                _currentMatch = null;

                // Advance the state
                _currentState = SearchState.SearchingFunctions;
            }

            if (_currentState == SearchState.SearchingFunctions)
            {
                _currentMatch = SearchForFunctions();
                // Return the alias info only if it is visible. If not, then skip to the next
                // stage of command resolution...
                if (_currentMatch != null)
                {
                    return true;
                }

                // Advance the state
                _currentState = SearchState.SearchingCmdlets;
            }

            if (_currentState == SearchState.SearchingCmdlets)
            {
                _currentMatch = SearchForCmdlets();
                if (_currentMatch != null)
                {
                    return true;
                }

                // Advance the state
                _currentState = SearchState.StartSearchingForExternalCommands;
            }

            if (_currentState == SearchState.StartSearchingForExternalCommands)
            {
                if ((_commandTypes & (CommandTypes.Application | CommandTypes.ExternalScript)) == 0)
                {
                    // Since we are not requiring any path lookup in this search, just return false now
                    // because all the remaining searches do path lookup.
                    return false;
                }

                // For security reasons, if the command is coming from outside the runspace and it looks like a path,
                // we want to pre-check that path before doing any probing of the network or drives
                if (_commandOrigin == CommandOrigin.Runspace && _commandName.IndexOfAny(Utils.Separators.DirectoryOrDrive) >= 0)
                {
                    bool allowed = false;

                    // Ok - it looks like it might be a path, so we're going to check to see if the command is prefixed
                    // by any of the allowed paths. If so, then we allow the search to proceed...

                    // If either the Applications or Script lists contain just '*' the command is allowed
                    // at this point.
                    if ((_context.EngineSessionState.Applications.Count == 1 &&
                        _context.EngineSessionState.Applications[0].Equals("*", StringComparison.OrdinalIgnoreCase)) ||
                        (_context.EngineSessionState.Scripts.Count == 1 &&
                        _context.EngineSessionState.Scripts[0].Equals("*", StringComparison.OrdinalIgnoreCase)))
                    {
                        allowed = true;
                    }
                    else
                    {
                        // Ok see it it's in the applications list
                        foreach (string path in _context.EngineSessionState.Applications)
                        {
                            if (checkPath(path, _commandName))
                            {
                                allowed = true;
                                break;
                            }
                        }

                        // If it wasn't in the applications list, see it's in the script list
                        if (!allowed)
                        {
                            foreach (string path in _context.EngineSessionState.Scripts)
                            {
                                if (checkPath(path, _commandName))
                                {
                                    allowed = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!allowed)
                    {
                        return false;
                    }
                }

                // Advance the state

                _currentState = SearchState.PowerShellPathResolution;

                _currentMatch = ProcessBuiltinScriptState();

                if (_currentMatch != null)
                {
                    // Set the current state to QualifiedFileSystemPath since
                    // we want to skip the qualified file system path search
                    // in the case where we found a PowerShell qualified path.

                    _currentState = SearchState.QualifiedFileSystemPath;
                    return true;
                }
            }

            if (_currentState == SearchState.PowerShellPathResolution)
            {
                _currentState = SearchState.QualifiedFileSystemPath;

                _currentMatch = ProcessPathResolutionState();

                if (_currentMatch != null)
                {
                    return true;
                }
            }

            // Search using CommandPathSearch

            if (_currentState == SearchState.QualifiedFileSystemPath ||
                    _currentState == SearchState.PathSearch)
            {
                _currentMatch = ProcessQualifiedFileSystemState();

                if (_currentMatch != null)
                {
                    return true;
                }
            }

            if (_currentState == SearchState.PathSearch)
            {
                _currentState = SearchState.PowerShellRelativePath;

                _currentMatch = ProcessPathSearchState();

                if (_currentMatch != null)
                {
                    return true;
                }
            }

            return false;
        }

        private CommandInfo SearchForAliases()
        {
            CommandInfo currentMatch = null;

            if (_context.EngineSessionState != null &&
                (_commandTypes & CommandTypes.Alias) != 0)
            {
                currentMatch = GetNextAlias();
            }

            return currentMatch;
        }

        private CommandInfo SearchForFunctions()
        {
            CommandInfo currentMatch = null;

            if (_context.EngineSessionState != null &&
                (_commandTypes & (CommandTypes.Function | CommandTypes.Filter | CommandTypes.Configuration)) != 0)
            {
                currentMatch = GetNextFunction();
            }

            return currentMatch;
        }

        private CommandInfo SearchForCmdlets()
        {
            CommandInfo currentMatch = null;

            if ((_commandTypes & CommandTypes.Cmdlet) != 0)
            {
                currentMatch = GetNextCmdlet();
            }

            return currentMatch;
        }

        private CommandInfo ProcessBuiltinScriptState()
        {
            CommandInfo currentMatch = null;

            // Check to see if the path is qualified

            if (_context.EngineSessionState != null &&
                _context.EngineSessionState.ProviderCount > 0 &&
                IsQualifiedPSPath(_commandName))
            {
                currentMatch = GetNextFromPath();
            }

            return currentMatch;
        }

        private CommandInfo ProcessPathResolutionState()
        {
            CommandInfo currentMatch = null;

            try
            {
                // Check to see if the path is a file system path that
                // is rooted.  If so that is the next match
                if (Path.IsPathRooted(_commandName) &&
                    File.Exists(_commandName))
                {
                    try
                    {
                        currentMatch = GetInfoFromPath(_commandName);
                    }
                    catch (FileLoadException)
                    {
                    }
                    catch (FormatException)
                    {
                    }
                    catch (MetadataException)
                    {
                    }
                }
            }
            catch (ArgumentException)
            {
                // If the path contains illegal characters that
                // weren't caught by the other APIs, IsPathRooted
                // will throw an exception.
                // For example, looking for a command called
                // `abcdef
                // The `a will be translated into the beep control character
                // which is not a legal file system character, though
                // Path.InvalidPathChars does not contain it as an invalid
                // character.
            }

            return currentMatch;
        }

        private CommandInfo ProcessQualifiedFileSystemState()
        {
            try
            {
                setupPathSearcher();
            }
            catch (ArgumentException)
            {
                _currentState = SearchState.NoMoreMatches;
                throw;
            }
            catch (PathTooLongException)
            {
                _currentState = SearchState.NoMoreMatches;
                throw;
            }

            CommandInfo currentMatch = null;
            _currentState = SearchState.PathSearch;
            if (_canDoPathLookup)
            {
                try
                {
                    while (currentMatch == null && _pathSearcher.MoveNext())
                    {
                        currentMatch = GetInfoFromPath(((IEnumerator<string>)_pathSearcher).Current);
                    }
                }
                catch (InvalidOperationException)
                {
                    // The enumerator may throw if there are no more matches
                }
            }

            return currentMatch;
        }

        private CommandInfo ProcessPathSearchState()
        {
            CommandInfo currentMatch = null;
            string path = DoPowerShellRelativePathLookup();

            if (!string.IsNullOrEmpty(path))
            {
                currentMatch = GetInfoFromPath(path);
            }

            return currentMatch;
        }

        /// <summary>
        /// Gets the CommandInfo representing the current command match.
        /// </summary>
        /// <value></value>
        /// <exception cref="InvalidOperationException">
        /// The enumerator is positioned before the first element of
        /// the collection or after the last element.
        /// </exception>
        CommandInfo IEnumerator<CommandInfo>.Current
        {
            get
            {
                if ((_currentState == SearchState.SearchingAliases && _currentMatch == null) ||
                    _currentState == SearchState.NoMoreMatches ||
                    _currentMatch == null)
                {
                    throw PSTraceSource.NewInvalidOperationException();
                }

                return _currentMatch;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return ((IEnumerator<CommandInfo>)this).Current;
            }
        }

        /// <summary>
        /// Required by the IEnumerator generic interface.
        /// Resets the searcher.
        /// </summary>
        public void Dispose()
        {
            if (_pathSearcher != null)
            {
                _pathSearcher.Dispose();
                _pathSearcher = null;
            }

            Reset();
            GC.SuppressFinalize(this);
        }

        #region private members

        /// <summary>
        /// Gets the next command info using the command name as a path.
        /// </summary>
        /// <returns>
        /// A CommandInfo for the next command if it exists as a path, or null otherwise.
        /// </returns>
        private CommandInfo GetNextFromPath()
        {
            CommandInfo result = null;

            do // false loop
            {
                CommandDiscovery.discoveryTracer.WriteLine(
                    "The name appears to be a qualified path: {0}",
                    _commandName);

                CommandDiscovery.discoveryTracer.WriteLine(
                    "Trying to resolve the path as an PSPath");

                // Find the match if it is.
                // Try literal path resolution if it is set to run first
                if (_commandResolutionOptions.HasFlag(SearchResolutionOptions.ResolveLiteralThenPathPatterns))
                {
                    var path = GetNextLiteralPathThatExists(_commandName, out _);

                    if (path != null)
                    {
                        return GetInfoFromPath(path);
                    }
                }

                Collection<string> resolvedPaths = new Collection<string>();
                if (WildcardPattern.ContainsWildcardCharacters(_commandName))
                {
                    resolvedPaths = GetNextFromPathUsingWildcards(_commandName, out _);
                }

                // Try literal path resolution if wildcards are enable first and wildcard search failed
                if (!_commandResolutionOptions.HasFlag(SearchResolutionOptions.ResolveLiteralThenPathPatterns) &&
                    resolvedPaths.Count == 0)
                {
                    string path = GetNextLiteralPathThatExists(_commandName, out _);

                    if (path != null)
                    {
                        return GetInfoFromPath(path);
                    }
                }

                if (resolvedPaths.Count > 1)
                {
                    CommandDiscovery.discoveryTracer.TraceError(
                        "The path resolved to more than one result so this path cannot be used.");
                    break;
                }

                // If the path was resolved, and it exists
                if (resolvedPaths.Count == 1 &&
                    File.Exists(resolvedPaths[0]))
                {
                    string path = resolvedPaths[0];

                    CommandDiscovery.discoveryTracer.WriteLine(
                        "Path resolved to: {0}",
                        path);

                    result = GetInfoFromPath(path);
                }
            } while (false);

            return result;
        }

        /// <summary>
        /// Gets the next path using WildCards.
        /// </summary>
        /// <param name="command">
        /// The command to search for.
        /// </param>
        /// <param name="provider">The provider that the command was found in.</param>
        /// <returns>
        /// A collection of full paths to the commands which were found.
        /// </returns>
        private Collection<string> GetNextFromPathUsingWildcards(string command, out ProviderInfo provider)
        {
            try
            {
                return _context.LocationGlobber.GetGlobbedProviderPathsFromMonadPath(path: command, allowNonexistingPaths: false, provider: out provider, providerInstance: out _);
            }
            catch (ItemNotFoundException)
            {
                CommandDiscovery.discoveryTracer.TraceError(
                    "The path could not be found: {0}",
                    command);
            }
            catch (DriveNotFoundException)
            {
                CommandDiscovery.discoveryTracer.TraceError(
                    "A drive could not be found for the path: {0}",
                    command);
            }
            catch (ProviderNotFoundException)
            {
                CommandDiscovery.discoveryTracer.TraceError(
                    "A provider could not be found for the path: {0}",
                    command);
            }
            catch (InvalidOperationException)
            {
                CommandDiscovery.discoveryTracer.TraceError(
                    "The path specified a home directory, but the provider home directory was not set. {0}",
                    command);
            }
            catch (ProviderInvocationException providerException)
            {
                CommandDiscovery.discoveryTracer.TraceError(
                    "The provider associated with the path '{0}' encountered an error: {1}",
                    command,
                    providerException.Message);
            }
            catch (PSNotSupportedException)
            {
                CommandDiscovery.discoveryTracer.TraceError(
                    "The provider associated with the path '{0}' does not implement ContainerCmdletProvider",
                    command);
            }

            provider = null;
            return null;
        }

        private static bool checkPath(string path, string commandName)
        {
            return path.StartsWith(commandName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the appropriate CommandInfo instance given the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to create the CommandInfo for.
        /// </param>
        /// <returns>
        /// An instance of the appropriate CommandInfo derivative given the specified path.
        /// </returns>
        /// <exception cref="FileLoadException">
        /// The <paramref name="path"/> refers to a cmdlet, or cmdletprovider
        /// and it could not be loaded as an XML document.
        /// </exception>
        /// <exception cref="FormatException">
        /// The <paramref name="path"/> refers to a cmdlet, or cmdletprovider
        /// that does not adhere to the appropriate file format for its extension.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If <paramref name="path"/> refers to a cmdlet file that
        /// contains invalid metadata.
        /// </exception>
        private CommandInfo GetInfoFromPath(string path)
        {
            CommandInfo result = null;

            do // false loop
            {
                if (!File.Exists(path))
                {
                    CommandDiscovery.discoveryTracer.TraceError("The path does not exist: {0}", path);
                    break;
                }

                // Now create the appropriate CommandInfo using the extension
                string extension = null;

                try
                {
                    extension = Path.GetExtension(path);
                }
                catch (ArgumentException)
                {
                    // If the path contains illegal characters that
                    // weren't caught by the other APIs, GetExtension
                    // will throw an exception.
                    // For example, looking for a command called
                    // `abcdef
                    // The `a will be translated into the beep control character
                    // which is not a legal file system character.
                }

                if (extension == null)
                {
                    result = null;
                    break;
                }

                if (string.Equals(extension, StringLiterals.PowerShellScriptFileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    if ((_commandTypes & CommandTypes.ExternalScript) != 0)
                    {
                        string scriptName = Path.GetFileName(path);

                        CommandDiscovery.discoveryTracer.WriteLine(
                            "Command Found: path ({0}) is a script with name: {1}",
                            path,
                            scriptName);

                        // The path is to a PowerShell script

                        result = new ExternalScriptInfo(scriptName, path, _context);
                        break;
                    }

                    break;
                }

                if ((_commandTypes & CommandTypes.Application) != 0)
                {
                    // Anything else is treated like an application

                    string appName = Path.GetFileName(path);

                    CommandDiscovery.discoveryTracer.WriteLine(
                        "Command Found: path ({0}) is an application with name: {1}",
                        path,
                        appName);

                    result = new ApplicationInfo(appName, path, _context);
                    break;
                }
            } while (false);

            // Verify that this script is not untrusted, if we aren't constrained.
            if (ShouldSkipCommandResolutionForConstrainedLanguage(result, _context))
            {
                result = null;
            }

            return result;
        }

        /// <summary>
        /// Gets the next matching alias.
        /// </summary>
        /// <returns>
        /// A CommandInfo representing the next matching alias if found, otherwise null.
        /// </returns>
        private CommandInfo GetNextAlias()
        {
            CommandInfo result = null;

            if ((_commandResolutionOptions & SearchResolutionOptions.ResolveAliasPatterns) != 0)
            {
                if (_matchingAlias == null)
                {
                    // Generate the enumerator of matching alias names

                    Collection<AliasInfo> matchingAliases = new Collection<AliasInfo>();

                    WildcardPattern aliasMatcher =
                        WildcardPattern.Get(
                            _commandName,
                            WildcardOptions.IgnoreCase);

                    foreach (KeyValuePair<string, AliasInfo> aliasEntry in _context.EngineSessionState.GetAliasTable())
                    {
                        if (aliasMatcher.IsMatch(aliasEntry.Key) ||
                            (_commandResolutionOptions.HasFlag(SearchResolutionOptions.FuzzyMatch) &&
                            FuzzyMatcher.IsFuzzyMatch(aliasEntry.Key, _commandName)))
                        {
                            matchingAliases.Add(aliasEntry.Value);
                        }
                    }

                    // Process alias from modules
                    AliasInfo c = GetAliasFromModules(_commandName);
                    if (c != null)
                    {
                        matchingAliases.Add(c);
                    }

                    _matchingAlias = matchingAliases.GetEnumerator();
                }

                if (!_matchingAlias.MoveNext())
                {
                    // Advance the state
                    _currentState = SearchState.SearchingFunctions;

                    _matchingAlias = null;
                }
                else
                {
                    result = _matchingAlias.Current;
                }
            }
            else
            {
                // Advance the state
                _currentState = SearchState.SearchingFunctions;

                result = _context.EngineSessionState.GetAlias(_commandName) ?? GetAliasFromModules(_commandName);
            }

            // Verify that this alias was not created by an untrusted constrained language,
            // if we aren't constrained.
            if (ShouldSkipCommandResolutionForConstrainedLanguage(result, _context))
            {
                result = null;
            }

            if (result != null)
            {
                CommandDiscovery.discoveryTracer.WriteLine(
                    "Alias found: {0}  {1}",
                    result.Name,
                    result.Definition);
            }

            return result;
        }

        /// <summary>
        /// Gets the next matching function.
        /// </summary>
        /// <returns>
        /// A CommandInfo representing the next matching function if found, otherwise null.
        /// </returns>
        private CommandInfo GetNextFunction()
        {
            CommandInfo result = null;

            if (_commandResolutionOptions.HasFlag(SearchResolutionOptions.ResolveFunctionPatterns))
            {
                if (_matchingFunctionEnumerator == null)
                {
                    Collection<CommandInfo> matchingFunction = new Collection<CommandInfo>();

                    // Generate the enumerator of matching function names
                    WildcardPattern functionMatcher =
                        WildcardPattern.Get(
                            _commandName,
                            WildcardOptions.IgnoreCase);

                    foreach (DictionaryEntry functionEntry in _context.EngineSessionState.GetFunctionTable())
                    {
                        if (functionMatcher.IsMatch((string)functionEntry.Key) ||
                            (_commandResolutionOptions.HasFlag(SearchResolutionOptions.FuzzyMatch) &&
                            FuzzyMatcher.IsFuzzyMatch(functionEntry.Key.ToString(), _commandName)))
                        {
                            matchingFunction.Add((CommandInfo)functionEntry.Value);
                        }
                        else if (_commandResolutionOptions.HasFlag(SearchResolutionOptions.UseAbbreviationExpansion))
                        {
                            if (_commandName.Equals(ModuleUtils.AbbreviateName((string)functionEntry.Key), StringComparison.OrdinalIgnoreCase))
                            {
                                matchingFunction.Add((CommandInfo)functionEntry.Value);
                            }
                        }
                    }

                    // Process functions from modules
                    CommandInfo cmdInfo = GetFunctionFromModules(_commandName);
                    if (cmdInfo != null)
                    {
                        matchingFunction.Add(cmdInfo);
                    }

                    _matchingFunctionEnumerator = matchingFunction.GetEnumerator();
                }

                if (!_matchingFunctionEnumerator.MoveNext())
                {
                    // Advance the state
                    _currentState = SearchState.SearchingCmdlets;

                    _matchingFunctionEnumerator = null;
                }
                else
                {
                    result = _matchingFunctionEnumerator.Current;
                }
            }
            else
            {
                // Advance the state
                _currentState = SearchState.SearchingCmdlets;

                result = GetFunction(_commandName);
            }

            // Verify that this function was not created by an untrusted constrained language,
            // if we aren't constrained.
            if (ShouldSkipCommandResolutionForConstrainedLanguage(result, _context))
            {
                result = null;
            }

            return result;
        }

        // Don't return commands to the user if that might result in:
        //     - Trusted commands calling untrusted functions that the user has overridden
        //     - Debug prompts calling internal functions that are likely to have code injection
        private bool ShouldSkipCommandResolutionForConstrainedLanguage(CommandInfo result, ExecutionContext executionContext)
        {
            if (result == null)
            {
                return false;
            }

            // Don't return untrusted commands to trusted functions
            if ((result.DefiningLanguageMode == PSLanguageMode.ConstrainedLanguage) &&
                (executionContext.LanguageMode == PSLanguageMode.FullLanguage))
            {
                return true;
            }

            // Don't allow invocation of trusted functions from debug breakpoints.
            // They were probably defined within a trusted script, and could be
            // susceptible to injection attacks. However, we do allow execution
            // of functions defined in the global scope (i.e.: "more",) as those
            // are intended to be exposed explicitly to users.
            if ((result is FunctionInfo) &&
                (executionContext.LanguageMode == PSLanguageMode.ConstrainedLanguage) &&
                (result.DefiningLanguageMode == PSLanguageMode.FullLanguage) &&
                (executionContext.Debugger != null) &&
                (executionContext.Debugger.InBreakpoint) &&
                (!(executionContext.TopLevelSessionState.GetFunctionTableAtScope("GLOBAL").ContainsKey(result.Name))))
            {
                return true;
            }

            return false;
        }

        private AliasInfo GetAliasFromModules(string command)
        {
            AliasInfo result = null;

            if (command.IndexOf('\\') > 0)
            {
                // See if it's a module qualified alias...
                PSSnapinQualifiedName qualifiedName = PSSnapinQualifiedName.GetInstance(command);
                if (qualifiedName != null && !string.IsNullOrEmpty(qualifiedName.PSSnapInName))
                {
                    PSModuleInfo module = GetImportedModuleByName(qualifiedName.PSSnapInName);

                    if (module != null)
                    {
                        module.ExportedAliases.TryGetValue(qualifiedName.ShortName, out result);
                    }
                }
            }

            return result;
        }

        private CommandInfo GetFunctionFromModules(string command)
        {
            FunctionInfo result = null;

            if (command.IndexOf('\\') > 0)
            {
                // See if it's a module qualified function call...
                PSSnapinQualifiedName qualifiedName = PSSnapinQualifiedName.GetInstance(command);
                if (qualifiedName != null && !string.IsNullOrEmpty(qualifiedName.PSSnapInName))
                {
                    PSModuleInfo module = GetImportedModuleByName(qualifiedName.PSSnapInName);

                    if (module != null)
                    {
                        module.ExportedFunctions.TryGetValue(qualifiedName.ShortName, out result);
                    }
                }
            }

            return result;
        }

        private PSModuleInfo GetImportedModuleByName(string moduleName)
        {
            PSModuleInfo module = null;
            List<PSModuleInfo> modules = _context.Modules.GetModules(new string[] { moduleName }, false);

            if (modules != null && modules.Count > 0)
            {
                foreach (PSModuleInfo m in modules)
                {
                    if (_context.previousModuleImported.ContainsKey(m.Name) && ((string)_context.previousModuleImported[m.Name] == m.Path))
                    {
                        module = m;
                        break;
                    }
                }

                if (module == null)
                {
                    module = modules[0];
                }
            }

            return module;
        }

        /// <summary>
        /// Gets the FunctionInfo or FilterInfo for the specified function name.
        /// </summary>
        /// <param name="function">
        /// The name of the function/filter to retrieve.
        /// </param>
        /// <returns>
        /// A FunctionInfo if the function name exists and is a function, a FilterInfo if
        /// the filter name exists and is a filter, or null otherwise.
        /// </returns>
        private CommandInfo GetFunction(string function)
        {
            CommandInfo result = _context.EngineSessionState.GetFunction(function);

            if (result != null)
            {
                if (result is FilterInfo)
                {
                    CommandDiscovery.discoveryTracer.WriteLine(
                        "Filter found: {0}",
                        function);
                }
                else if (result is ConfigurationInfo)
                {
                    CommandDiscovery.discoveryTracer.WriteLine(
                        "Configuration found: {0}",
                        function);
                }
                else
                {
                    CommandDiscovery.discoveryTracer.WriteLine(
                        "Function found: {0}  {1}",
                        function);
                }
            }
            else
            {
                result = GetFunctionFromModules(function);
            }

            return result;
        }

        /// <summary>
        /// Gets the next cmdlet from the collection of matching cmdlets.
        /// If the collection doesn't exist yet it is created and the
        /// enumerator is moved to the first item in the collection.
        /// </summary>
        /// <returns>
        /// A CmdletInfo for the next matching Cmdlet or null if there are
        /// no more matches.
        /// </returns>
        private CmdletInfo GetNextCmdlet()
        {
            CmdletInfo result = null;
            bool useAbbreviationExpansion = _commandResolutionOptions.HasFlag(SearchResolutionOptions.UseAbbreviationExpansion);

            if (_matchingCmdlet == null)
            {
                if (_commandResolutionOptions.HasFlag(SearchResolutionOptions.CommandNameIsPattern) || useAbbreviationExpansion)
                {
                    Collection<CmdletInfo> matchingCmdletInfo = new Collection<CmdletInfo>();

                    PSSnapinQualifiedName PSSnapinQualifiedCommandName =
                        PSSnapinQualifiedName.GetInstance(_commandName);

                    if (!useAbbreviationExpansion && PSSnapinQualifiedCommandName == null)
                    {
                        return null;
                    }

                    WildcardPattern cmdletMatcher =
                        WildcardPattern.Get(
                            PSSnapinQualifiedCommandName.ShortName,
                            WildcardOptions.IgnoreCase);

                    SessionStateInternal ss = _context.EngineSessionState;

                    foreach (List<CmdletInfo> cmdletList in ss.GetCmdletTable().Values)
                    {
                        foreach (CmdletInfo cmdlet in cmdletList)
                        {
                            if (cmdletMatcher.IsMatch(cmdlet.Name) ||
                                (_commandResolutionOptions.HasFlag(SearchResolutionOptions.FuzzyMatch) &&
                                FuzzyMatcher.IsFuzzyMatch(cmdlet.Name, _commandName)))
                            {
                                if (string.IsNullOrEmpty(PSSnapinQualifiedCommandName.PSSnapInName) ||
                                    (PSSnapinQualifiedCommandName.PSSnapInName.Equals(
                                        cmdlet.ModuleName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    // If PSSnapin is specified, make sure they match
                                    matchingCmdletInfo.Add(cmdlet);
                                }
                            }
                            else if (useAbbreviationExpansion)
                            {
                                if (_commandName.Equals(ModuleUtils.AbbreviateName(cmdlet.Name), StringComparison.OrdinalIgnoreCase))
                                {
                                    matchingCmdletInfo.Add(cmdlet);
                                }
                            }
                        }
                    }

                    _matchingCmdlet = matchingCmdletInfo.GetEnumerator();
                }
                else
                {
                    _matchingCmdlet = _context.CommandDiscovery.GetCmdletInfo(_commandName,
                        _commandResolutionOptions.HasFlag(SearchResolutionOptions.SearchAllScopes));
                }
            }

            if (!_matchingCmdlet.MoveNext())
            {
                // Advance the state
                _currentState = SearchState.StartSearchingForExternalCommands;

                _matchingCmdlet = null;
            }
            else
            {
                result = _matchingCmdlet.Current;
            }

            return traceResult(result);
        }

        private IEnumerator<CmdletInfo> _matchingCmdlet;

        private static CmdletInfo traceResult(CmdletInfo result)
        {
            if (result != null)
            {
                CommandDiscovery.discoveryTracer.WriteLine(
                    "Cmdlet found: {0}  {1}",
                    result.Name,
                    result.ImplementingType);
            }

            return result;
        }

        private string DoPowerShellRelativePathLookup()
        {
            string result = null;

            if (_context.EngineSessionState != null &&
                _context.EngineSessionState.ProviderCount > 0)
            {
                // NTRAID#Windows OS Bugs-1009294-2004/02/04-JeffJon
                // This is really slow.  Maybe since we are only allowing FS paths right
                // now we should use the file system APIs to verify the existence of the file.

                // Since the path to the command was not found using the PATH variable,
                // maybe it is relative to the current location. Try resolving the
                // path.
                // Relative Path:       ".\command.exe"
                // Home Path:           "~\command.exe"
                // Drive Relative Path: "\Users\User\AppData\Local\Temp\command.exe"
                if (_commandName[0] == '.' || _commandName[0] == '~' || _commandName[0] == '\\')
                {
                    using (CommandDiscovery.discoveryTracer.TraceScope(
                        "{0} appears to be a relative path. Trying to resolve relative path",
                        _commandName))
                    {
                        result = ResolvePSPath(_commandName);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves the given path as an PSPath and ensures that it was resolved
        /// by the FileSystemProvider.
        /// </summary>
        /// <param name="path">
        /// The path to resolve.
        /// </param>
        /// <returns>
        /// The path that was resolved. Null if the path couldn't be resolved or was
        /// not resolved by the FileSystemProvider.
        /// </returns>
        private string ResolvePSPath(string path)
        {
            string result = null;

            try
            {
                ProviderInfo provider = null;
                string resolvedPath = null;

                // Try literal path resolution if it is set to run first
                if (_commandResolutionOptions.HasFlag(SearchResolutionOptions.ResolveLiteralThenPathPatterns))
                {
                    // Cannot return early as this code path only expects
                    // The file system provider and the final check for that
                    // must verify this before we return.
                    resolvedPath = GetNextLiteralPathThatExists(path, out provider);
                }

                if (WildcardPattern.ContainsWildcardCharacters(path) &&
                    ((resolvedPath == null) || (provider == null)))
                {
                    // Let PowerShell resolve relative path with wildcards.
                    Collection<string> resolvedPaths = GetNextFromPathUsingWildcards(path, out provider);

                    if (resolvedPaths.Count == 0)
                    {
                        resolvedPath = null;

                        CommandDiscovery.discoveryTracer.TraceError(
                           "The relative path with wildcard did not resolve to valid path. {0}",
                           path);
                    }
                    else if (resolvedPaths.Count > 1)
                    {
                        resolvedPath = null;

                        CommandDiscovery.discoveryTracer.TraceError(
                        "The relative path with wildcard resolved to multiple paths. {0}",
                        path);
                    }
                    else
                    {
                        resolvedPath = resolvedPaths[0];
                    }
                }

                // Try literal path resolution if wildcards are enabled first and wildcard search failed
                if (!_commandResolutionOptions.HasFlag(SearchResolutionOptions.ResolveLiteralThenPathPatterns) &&
                    ((resolvedPath == null) || (provider == null)))
                {
                    resolvedPath = GetNextLiteralPathThatExists(path, out provider);
                }

                // Verify the path was resolved to a file system path
                if (provider != null && provider.NameEquals(_context.ProviderNames.FileSystem))
                {
                    result = resolvedPath;

                    CommandDiscovery.discoveryTracer.WriteLine(
                        "The relative path was resolved to: {0}",
                        result);
                }
                else
                {
                    // The path was not to the file system
                    CommandDiscovery.discoveryTracer.TraceError(
                        "The relative path was not a file system path. {0}",
                        path);
                }
            }
            catch (InvalidOperationException)
            {
                CommandDiscovery.discoveryTracer.TraceError(
                    "The home path was not specified for the provider. {0}",
                    path);
            }
            catch (ProviderInvocationException providerInvocationException)
            {
                CommandDiscovery.discoveryTracer.TraceError(
                    "While resolving the path, \"{0}\", an error was encountered by the provider: {1}",
                    path,
                    providerInvocationException.Message);
            }
            catch (ItemNotFoundException)
            {
                CommandDiscovery.discoveryTracer.TraceError(
                    "The path does not exist: {0}",
                    path);
            }
            catch (DriveNotFoundException driveNotFound)
            {
                CommandDiscovery.discoveryTracer.TraceError(
                    "The drive does not exist: {0}",
                    driveNotFound.ItemName);
            }

            return result;
        }

        /// <summary>
        /// Gets the next literal path.
        /// Filtering to ones that exist for the filesystem.
        /// </summary>
        /// <param name="command">
        /// The command to search for.
        /// </param>
        /// <param name="provider">The provider that the command was found in.</param>
        /// <returns>
        /// Full path to the command.
        /// </returns>
        private string GetNextLiteralPathThatExists(string command, out ProviderInfo provider)
        {
            string resolvedPath = _context.LocationGlobber.GetProviderPath(command, out provider);

            if (provider.NameEquals(_context.ProviderNames.FileSystem)
                && !File.Exists(resolvedPath)
                && !Directory.Exists(resolvedPath))
            {
                provider = null;
                return null;
            }

            return resolvedPath;
        }

        /// <summary>
        /// Creates a collection of patterns used to find the command.
        /// </summary>
        /// <param name="name">
        /// The name of the command to search for.
        /// </param>
        /// <param name="commandDiscovery">Get names for command discovery.</param>
        /// <returns>
        /// A collection of the patterns used to find the command.
        /// The patterns are as follows:
        ///     1. [commandName].cmdlet
        ///     2. [commandName].ps1
        ///     3..x
        ///         foreach (extension in PATHEXT)
        ///             [commandName].[extension]
        ///     x+1. [commandName]
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> contains one or more of the
        /// invalid characters defined in InvalidPathChars.
        /// </exception>
        internal Collection<string> ConstructSearchPatternsFromName(string name, bool commandDiscovery = false)
        {
            Dbg.Assert(
                !string.IsNullOrEmpty(name),
                "Caller should verify name");

            Collection<string> result = new Collection<string>();

            // First check to see if the commandName has an extension, if so
            // look for that first

            bool commandNameAddedFirst = false;

            if (!string.IsNullOrEmpty(Path.GetExtension(name)))
            {
                result.Add(name);
                commandNameAddedFirst = true;
            }

            // Add the extensions for script, module and data files in that order...
            if ((_commandTypes & CommandTypes.ExternalScript) != 0)
            {
                result.Add(name + StringLiterals.PowerShellScriptFileExtension);
                if (!commandDiscovery)
                {
                    // psd1 and psm1 are not executable, so don't add them
                    result.Add(name + StringLiterals.PowerShellModuleFileExtension);
                    result.Add(name + StringLiterals.PowerShellDataFileExtension);
                }
            }

            if ((_commandTypes & CommandTypes.Application) != 0)
            {
                // Now add each extension from the PATHEXT environment variable

                foreach (string extension in CommandDiscovery.PathExtensions)
                {
                    result.Add(name + extension);
                }
            }

            // Now add the commandName by itself if it wasn't added as the first
            // pattern

            if (!commandNameAddedFirst)
            {
                result.Add(name);
            }

            return result;
        }

        /// <summary>
        /// Determines if the given command name is a qualified PowerShell path.
        /// </summary>
        /// <param name="commandName">
        /// The name of the command.
        /// </param>
        /// <returns>
        /// True if the command name is either a provider-qualified or PowerShell drive-qualified
        /// path. False otherwise.
        /// </returns>
        private static bool IsQualifiedPSPath(string commandName)
        {
            Dbg.Assert(
                !string.IsNullOrEmpty(commandName),
                "The caller should have verified the commandName");

            bool result =
                LocationGlobber.IsAbsolutePath(commandName) ||
                LocationGlobber.IsProviderQualifiedPath(commandName) ||
                LocationGlobber.IsHomePath(commandName) ||
                LocationGlobber.IsProviderDirectPath(commandName);

            return result;
        }

        private enum CanDoPathLookupResult
        {
            Yes,
            PathIsRooted,
            WildcardCharacters,
            DirectorySeparator,
            IllegalCharacters
        }

        /// <summary>
        /// Determines if the command name has any path special
        /// characters which would require resolution. If so,
        /// path lookup will not succeed.
        /// </summary>
        /// <param name="possiblePath">
        /// The command name (or possible path) to look for the special characters.
        /// </param>
        /// <returns>
        /// True if the command name does not contain any special
        /// characters.  False otherwise.
        /// </returns>
        private static CanDoPathLookupResult CanDoPathLookup(string possiblePath)
        {
            CanDoPathLookupResult result = CanDoPathLookupResult.Yes;

            do // false loop
            {
                // If the command name contains any wildcard characters
                // we can't do the path lookup

                if (WildcardPattern.ContainsWildcardCharacters(possiblePath))
                {
                    result = CanDoPathLookupResult.WildcardCharacters;
                    break;
                }

                try
                {
                    if (Path.IsPathRooted(possiblePath))
                    {
                        result = CanDoPathLookupResult.PathIsRooted;
                        break;
                    }
                }
                catch (ArgumentException)
                {
                    result = CanDoPathLookupResult.IllegalCharacters;
                    break;
                }

                // If the command contains any path separators, we can't
                // do the path lookup
                if (possiblePath.IndexOfAny(Utils.Separators.Directory) != -1)
                {
                    result = CanDoPathLookupResult.DirectorySeparator;
                    break;
                }

                // If the command contains any invalid path characters, we can't
                // do the path lookup

                if (possiblePath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                {
                    result = CanDoPathLookupResult.IllegalCharacters;
                    break;
                }
            } while (false);

            return result;
        }

        /// <summary>
        /// The command name to search for.
        /// </summary>
        private string _commandName;

        /// <summary>
        /// Determines which command types will be globbed.
        /// </summary>
        private SearchResolutionOptions _commandResolutionOptions;

        /// <summary>
        /// Determines which types of commands to look for.
        /// </summary>
        private CommandTypes _commandTypes = CommandTypes.All;

        /// <summary>
        /// The enumerator that uses the Path to
        /// search for commands.
        /// </summary>
        private CommandPathSearch _pathSearcher;

        /// <summary>
        /// The execution context instance for the current engine...
        /// </summary>
        private ExecutionContext _context;

        /// <summary>
        /// A routine to initialize the path searcher...
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If the commandName used to construct this object
        /// contains one or more of the invalid characters defined
        /// in InvalidPathChars.
        /// </exception>
        private void setupPathSearcher()
        {
            // If it's already set up, just return...
            if (_pathSearcher != null)
            {
                return;
            }

            // We are never going to look for non-executable commands in CommandSearcher.
            // Even though file types like .DOC, .LOG,.TXT, etc. can be opened / invoked, users think of these as files, not applications.
            // So I don't think we should show applications with the additional extensions at all.
            // Applications should only include files whose extensions are in the PATHEXT list and these would only be returned with the All parameter.

            if ((_commandResolutionOptions & SearchResolutionOptions.CommandNameIsPattern) != 0)
            {
                _canDoPathLookup = true;
                _canDoPathLookupResult = CanDoPathLookupResult.Yes;

                _pathSearcher =
                    new CommandPathSearch(
                        _commandName,
                        _context.CommandDiscovery.GetLookupDirectoryPaths(),
                        _context,
                        acceptableCommandNames: null,
                        useFuzzyMatch: _commandResolutionOptions.HasFlag(SearchResolutionOptions.FuzzyMatch));
            }
            else
            {
                _canDoPathLookupResult = CanDoPathLookup(_commandName);
                if (_canDoPathLookupResult == CanDoPathLookupResult.Yes)
                {
                    _canDoPathLookup = true;
                    _commandName = _commandName.TrimEnd(Utils.Separators.PathSearchTrimEnd);

                    _pathSearcher =
                        new CommandPathSearch(
                            _commandName,
                            _context.CommandDiscovery.GetLookupDirectoryPaths(),
                            _context,
                            ConstructSearchPatternsFromName(_commandName, commandDiscovery: true));
                }
                else if (_canDoPathLookupResult == CanDoPathLookupResult.PathIsRooted)
                {
                    _canDoPathLookup = true;

                    string directory = Path.GetDirectoryName(_commandName);
                    var directoryCollection = new[] { directory };

                    CommandDiscovery.discoveryTracer.WriteLine(
                        "The path is rooted, so only doing the lookup in the specified directory: {0}",
                        directory);

                    string fileName = Path.GetFileName(_commandName);

                    if (!string.IsNullOrEmpty(fileName))
                    {
                        fileName = fileName.TrimEnd(Utils.Separators.PathSearchTrimEnd);
                        _pathSearcher =
                            new CommandPathSearch(
                                fileName,
                                directoryCollection,
                                _context,
                                ConstructSearchPatternsFromName(fileName, commandDiscovery: true));
                    }
                    else
                    {
                        _canDoPathLookup = false;
                    }
                }
                else if (_canDoPathLookupResult == CanDoPathLookupResult.DirectorySeparator)
                {
                    _canDoPathLookup = true;

                    // We must try to resolve the path as an PSPath or else we can't do
                    // path lookup for relative paths.

                    string directory = Path.GetDirectoryName(_commandName);
                    directory = ResolvePSPath(directory);

                    CommandDiscovery.discoveryTracer.WriteLine(
                        "The path is relative, so only doing the lookup in the specified directory: {0}",
                        directory);

                    if (directory == null)
                    {
                        _canDoPathLookup = false;
                    }
                    else
                    {
                        var directoryCollection = new[] { directory };

                        string fileName = Path.GetFileName(_commandName);

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            fileName = fileName.TrimEnd(Utils.Separators.PathSearchTrimEnd);
                            _pathSearcher =
                                new CommandPathSearch(
                                    fileName,
                                    directoryCollection,
                                    _context,
                                    ConstructSearchPatternsFromName(fileName, commandDiscovery: true));
                        }
                        else
                        {
                            _canDoPathLookup = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resets the enumerator to before the first command match, public for IEnumerable.
        /// </summary>
        public void Reset()
        {
            // If this is a command coming from outside the runspace and there are no
            // permitted scripts or applications,
            // remove them from the set of things to search for...
            if (_commandOrigin == CommandOrigin.Runspace)
            {
                if (_context.EngineSessionState.Applications.Count == 0)
                    _commandTypes &= ~CommandTypes.Application;
                if (_context.EngineSessionState.Scripts.Count == 0)
                    _commandTypes &= ~CommandTypes.ExternalScript;
            }

            if (_pathSearcher != null)
            {
                _pathSearcher.Reset();
            }

            _currentMatch = null;
            _currentState = SearchState.SearchingAliases;
            _matchingAlias = null;
            _matchingCmdlet = null;
        }

        internal CommandOrigin CommandOrigin
        {
            get { return _commandOrigin; }

            set { _commandOrigin = value; }
        }

        private CommandOrigin _commandOrigin = CommandOrigin.Internal;

        /// <summary>
        /// An enumerator of the matching aliases.
        /// </summary>
        private IEnumerator<AliasInfo> _matchingAlias;

        /// <summary>
        /// An enumerator of the matching functions.
        /// </summary>
        private IEnumerator<CommandInfo> _matchingFunctionEnumerator;

        /// <summary>
        /// The CommandInfo that references the command that matches the pattern.
        /// </summary>
        private CommandInfo _currentMatch;

        private bool _canDoPathLookup;
        private CanDoPathLookupResult _canDoPathLookupResult = CanDoPathLookupResult.Yes;

        /// <summary>
        /// The current state of the enumerator.
        /// </summary>
        private SearchState _currentState = SearchState.SearchingAliases;

        private enum SearchState
        {
            // the searcher has been reset or has not been advanced since being created.
            SearchingAliases,

            // the searcher has finished alias resolution and is now searching for functions.
            SearchingFunctions,

            // the searcher has finished function resolution and is now searching for cmdlets
            SearchingCmdlets,

            // the search has finished builtin script resolution and is now searching for external commands
            StartSearchingForExternalCommands,

            // the searcher has moved to
            PowerShellPathResolution,

            // the searcher has moved to a qualified file system path
            QualifiedFileSystemPath,

            // the searcher has moved to using a CommandPathSearch object
            // for resolution
            PathSearch,

            // the searcher has moved to using a CommandPathSearch object
            // with get prepended to the command name for resolution
            GetPathSearch,

            // the searcher has moved to resolving the command as a
            // relative PowerShell path
            PowerShellRelativePath,

            // No more matches can be found
            NoMoreMatches,
        }

        #endregion private members
    }

    /// <summary>
    /// Determines which types of commands should be globbed using the specified
    /// pattern. Any flag that is not specified will only match if exact.
    /// </summary>
    [Flags]
    internal enum SearchResolutionOptions
    {
        None = 0x0,
        ResolveAliasPatterns = 0x01,
        ResolveFunctionPatterns = 0x02,
        CommandNameIsPattern = 0x04,
        SearchAllScopes = 0x08,

        /// <summary>Use fuzzy matching.</summary>
        FuzzyMatch = 0x10,

        /// <summary>
        /// Enable searching for cmdlets/functions by abbreviation expansion.
        /// </summary>
        UseAbbreviationExpansion = 0x20,

        /// <summary>
        /// Enable resolving wildcard in paths.
        /// </summary>
        ResolveLiteralThenPathPatterns = 0x40
    }
}
