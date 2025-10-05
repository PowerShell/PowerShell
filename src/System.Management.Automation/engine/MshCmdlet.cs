// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Reflection;

using PipelineResultTypes = System.Management.Automation.Runspaces.PipelineResultTypes;

namespace System.Management.Automation
{
    #region Auxiliary

    /// <summary>
    /// An interface that a
    /// <see cref="Cmdlet"/> or <see cref="Provider.CmdletProvider"/>
    /// must implement to indicate that it has dynamic parameters.
    /// </summary>
    /// <remarks>
    /// Dynamic parameters allow a
    /// <see cref="Cmdlet"/> or <see cref="Provider.CmdletProvider"/>
    /// to define additional parameters based on the value of
    /// the formal arguments.  For example, the parameters of
    /// "set-itemproperty" for the file system provider vary
    /// depending on whether the target object is a file or directory.
    /// </remarks>
    /// <seealso cref="Cmdlet"/>
    /// <seealso cref="PSCmdlet"/>
    /// <seealso cref="RuntimeDefinedParameter"/>
    /// <seealso cref="RuntimeDefinedParameterDictionary"/>
#nullable enable
    public interface IDynamicParameters
    {
        /// <summary>
        /// Returns an instance of an object that defines the
        /// dynamic parameters for this
        /// <see cref="Cmdlet"/> or <see cref="Provider.CmdletProvider"/>.
        /// </summary>
        /// <returns>
        /// This method should return an object that has properties and fields
        /// decorated with parameter attributes similar to a
        /// <see cref="Cmdlet"/> or <see cref="Provider.CmdletProvider"/>.
        /// These attributes include <see cref="ParameterAttribute"/>,
        /// <see cref="AliasAttribute"/>, argument transformation and
        /// validation attributes, etc.
        ///
        /// Alternately, it can return a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>
        /// instead.
        ///
        /// The <see cref="Cmdlet"/> or <see cref="Provider.CmdletProvider"/>
        /// should hold on to a reference to the object which it returns from
        /// this method, since the argument values for the dynamic parameters
        /// specified by that object will be set in that object.
        ///
        /// This method will be called after all formal (command-line)
        /// parameters are set, but before <see cref="Cmdlet.BeginProcessing"/>
        /// is called and before any incoming pipeline objects are read.
        /// Therefore, parameters which allow input from the pipeline
        /// may not be set at the time this method is called,
        /// even if the parameters are mandatory.
        /// </returns>
        object? GetDynamicParameters();
    }
#nullable restore

    /// <summary>
    /// Type used to define a parameter on a cmdlet script of function that
    /// can only be used as a switch.
    /// </summary>
    public readonly struct SwitchParameter
    {
        private readonly bool _value;

        /// <summary>
        /// Returns true if the parameter was specified on the command line, false otherwise.
        /// </summary>
        /// <value>True if the parameter was specified, false otherwise</value>
        public bool IsPresent
        {
            get { return _value; }
        }
        /// <summary>
        /// Implicit cast operator for casting SwitchParameter to bool.
        /// </summary>
        /// <param name="switchParameter">The SwitchParameter object to convert to bool.</param>
        /// <returns>The corresponding boolean value.</returns>
        public static implicit operator bool(SwitchParameter switchParameter)
        {
            return switchParameter._value;
        }

        /// <summary>
        /// Implicit cast operator for casting bool to SwitchParameter.
        /// </summary>
        /// <param name="value">The bool to convert to SwitchParameter.</param>
        /// <returns>The corresponding boolean value.</returns>
        public static implicit operator SwitchParameter(bool value)
        {
            return new SwitchParameter(value);
        }

        /// <summary>
        /// Explicit method to convert a SwitchParameter to a boolean value.
        /// </summary>
        /// <returns>The boolean equivalent of the SwitchParameter.</returns>
        public bool ToBool()
        {
            return _value;
        }

        /// <summary>
        /// Construct a SwitchParameter instance with a particular value.
        /// </summary>
        /// <param name="isPresent">
        /// If true, it indicates that the switch is present, false otherwise.
        /// </param>
        public SwitchParameter(bool isPresent)
        {
            _value = isPresent;
        }

        /// <summary>
        /// Static method that returns a instance of SwitchParameter that indicates that it is present.
        /// </summary>
        /// <value>An instance of a switch parameter that will convert to true in a boolean context</value>
        public static SwitchParameter Present
        {
            get { return new SwitchParameter(true); }
        }

        /// <summary>
        /// Compare this switch parameter to another object.
        /// </summary>
        /// <param name="obj">An object to compare against.</param>
        /// <returns>True if the objects are the same value.</returns>
        public override bool Equals(object obj)
        {
            if (obj is bool)
            {
                return _value == (bool)obj;
            }
            else if (obj is SwitchParameter)
            {
                return _value == (SwitchParameter)obj;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Returns the hash code for this switch parameter.
        /// </summary>
        /// <returns>The hash code for this cobject.</returns>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Implement the == operator for switch parameters objects.
        /// </summary>
        /// <param name="first">First object to compare.</param>
        /// <param name="second">Second object to compare.</param>
        /// <returns>True if they are the same.</returns>
        public static bool operator ==(SwitchParameter first, SwitchParameter second)
        {
            return first.Equals(second);
        }
        /// <summary>
        /// Implement the != operator for switch parameters.
        /// </summary>
        /// <param name="first">First object to compare.</param>
        /// <param name="second">Second object to compare.</param>
        /// <returns>True if they are different.</returns>
        public static bool operator !=(SwitchParameter first, SwitchParameter second)
        {
            return !first.Equals(second);
        }
        /// <summary>
        /// Implement the == operator for switch parameters and booleans.
        /// </summary>
        /// <param name="first">First object to compare.</param>
        /// <param name="second">Second object to compare.</param>
        /// <returns>True if they are the same.</returns>
        public static bool operator ==(SwitchParameter first, bool second)
        {
            return first.Equals(second);
        }
        /// <summary>
        /// Implement the != operator for switch parameters and booleans.
        /// </summary>
        /// <param name="first">First object to compare.</param>
        /// <param name="second">Second object to compare.</param>
        /// <returns>True if they are different.</returns>
        public static bool operator !=(SwitchParameter first, bool second)
        {
            return !first.Equals(second);
        }
        /// <summary>
        /// Implement the == operator for bool and switch parameters.
        /// </summary>
        /// <param name="first">First object to compare.</param>
        /// <param name="second">Second object to compare.</param>
        /// <returns>True if they are the same.</returns>
        public static bool operator ==(bool first, SwitchParameter second)
        {
            return first.Equals(second);
        }
        /// <summary>
        /// Implement the != operator for bool and switch parameters.
        /// </summary>
        /// <param name="first">First object to compare.</param>
        /// <param name="second">Second object to compare.</param>
        /// <returns>True if they are different.</returns>
        public static bool operator !=(bool first, SwitchParameter second)
        {
            return !first.Equals(second);
        }

        /// <summary>
        /// Returns the string representation for this object.
        /// </summary>
        /// <returns>The string for this object.</returns>
        public override string ToString()
        {
            return _value.ToString();
        }
    }

    /// <summary>
    /// Interfaces that cmdlets can use to build script blocks and execute scripts.
    /// </summary>
    public class CommandInvocationIntrinsics
    {
        private readonly ExecutionContext _context;
        private readonly PSCmdlet _cmdlet;
        private readonly MshCommandRuntime _commandRuntime;

        internal CommandInvocationIntrinsics(ExecutionContext context, PSCmdlet cmdlet)
        {
            _context = context;
            if (cmdlet != null)
            {
                _cmdlet = cmdlet;
                _commandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
            }
        }

        internal CommandInvocationIntrinsics(ExecutionContext context)
            : this(context, null)
        {
        }

        /// <summary>
        /// If an error occurred while executing the cmdlet, this will be set to true.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return _commandRuntime.PipelineProcessor.ExecutionFailed;
            }

            set
            {
                _commandRuntime.PipelineProcessor.ExecutionFailed = value;
            }
        }

        /// <summary>
        /// Returns a string with all of the variable and expression substitutions done.
        /// </summary>
        /// <param name="source">The string to expand.
        /// </param>
        /// <returns>The expanded string.</returns>
        /// <exception cref="ParseException">
        /// Thrown if a parse exception occurred during subexpression substitution.
        /// </exception>
        public string ExpandString(string source)
        {
            _cmdlet?.ThrowIfStopping();
            return _context.Engine.Expand(source);
        }

        /// <summary>
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public CommandInfo GetCommand(string commandName, CommandTypes type)
        {
            return GetCommand(commandName, type, null);
        }

        /// <summary>
        /// Returns a command info for a given command name and type, using the specified arguments
        /// to resolve dynamic parameters.
        /// </summary>
        /// <param name="commandName">The command name to search for.</param>
        /// <param name="type">The command type to search for.</param>
        /// <param name="arguments">The command arguments used to resolve dynamic parameters.</param>
        /// <returns>A CommandInfo result that represents the resolved command.</returns>
        public CommandInfo GetCommand(string commandName, CommandTypes type, object[] arguments)
        {
            CommandInfo result = null;

            try
            {
                CommandOrigin commandOrigin = CommandOrigin.Runspace;
                if (_cmdlet != null)
                {
                    commandOrigin = _cmdlet.CommandOrigin;
                }
                else if (_context != null)
                {
                    commandOrigin = _context.EngineSessionState.CurrentScope.ScopeOrigin;
                }

                result = CommandDiscovery.LookupCommandInfo(commandName, type, SearchResolutionOptions.None, commandOrigin, _context);

                if ((result != null) && (arguments != null) && (arguments.Length > 0))
                {
                    // We've been asked to retrieve dynamic parameters
                    if (result.ImplementsDynamicParameters)
                    {
                        result = result.CreateGetCommandCopy(arguments);
                    }
                }
            }
            catch (CommandNotFoundException) { }

            return result;
        }

        /// <summary>
        /// This event handler is called when a command is not found.
        /// If should have a single string parameter that is the name
        /// of the command and should return a CommandInfo object or null. By default
        /// it will search the module path looking for a module that exports the
        /// desired command.
        /// </summary>
        public System.EventHandler<CommandLookupEventArgs> CommandNotFoundAction { get; set; }

        /// <summary>
        /// This event handler is called before the command lookup is done.
        /// If should have a single string parameter that is the name
        /// of the command and should return a CommandInfo object or null.
        /// </summary>
        public System.EventHandler<CommandLookupEventArgs> PreCommandLookupAction { get; set; }

        /// <summary>
        /// This event handler is after the command lookup is done but before the event object is
        /// returned to the caller. This allows things like interning scripts to work.
        /// If should have a single string parameter that is the name
        /// of the command and should return a CommandInfo object or null.
        /// </summary>
        public System.EventHandler<CommandLookupEventArgs> PostCommandLookupAction { get; set; }

        /// <summary>
        /// Gets or sets the action that is invoked every time the runspace location (cwd) is changed.
        /// </summary>
        public System.EventHandler<LocationChangedEventArgs> LocationChangedAction { get; set; }

        /// <summary>
        /// Returns the CmdletInfo object that corresponds to the name argument.
        /// </summary>
        /// <param name="commandName">The name of the cmdlet to look for.</param>
        /// <returns>The cmdletInfo object if found, null otherwise.</returns>
        public CmdletInfo GetCmdlet(string commandName)
        {
            return GetCmdlet(commandName, _context);
        }

        /// <summary>
        /// Returns the CmdletInfo object that corresponds to the name argument.
        /// </summary>
        /// <param name="commandName">The name of the cmdlet to look for.</param>
        /// <param name="context">The execution context instance to use for lookup.</param>
        /// <returns>The cmdletInfo object if found, null otherwise.</returns>
        internal static CmdletInfo GetCmdlet(string commandName, ExecutionContext context)
        {
            CmdletInfo current = null;

            CommandSearcher searcher = new CommandSearcher(
                    commandName,
                    SearchResolutionOptions.None,
                    CommandTypes.Cmdlet,
                    context);
            while (true)
            {
                try
                {
                    if (!searcher.MoveNext())
                    {
                        break;
                    }
                }
                catch (ArgumentException)
                {
                    continue;
                }
                catch (PathTooLongException)
                {
                    continue;
                }
                catch (FileLoadException)
                {
                    continue;
                }
                catch (MetadataException)
                {
                    continue;
                }
                catch (FormatException)
                {
                    continue;
                }

                current = ((IEnumerator)searcher).Current as CmdletInfo;
            }

            return current;
        }

        /// <summary>
        /// Get the cmdlet info using the name of the cmdlet's implementing type. This bypasses
        /// session state and retrieves the command directly. Note that the help file and snapin/module
        /// info will both be null on returned object.
        /// </summary>
        /// <param name="cmdletTypeName">The type name of the class implementing this cmdlet.</param>
        /// <returns>CmdletInfo for the cmdlet if found, null otherwise.</returns>
        public CmdletInfo GetCmdletByTypeName(string cmdletTypeName)
        {
            if (string.IsNullOrEmpty(cmdletTypeName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cmdletTypeName));
            }

            Exception e = null;
            Type cmdletType = TypeResolver.ResolveType(cmdletTypeName, out e);
            if (e != null)
            {
                throw e;
            }

            if (cmdletType == null)
            {
                return null;
            }

            CmdletAttribute ca = null;
            foreach (var attr in cmdletType.GetCustomAttributes(true))
            {
                ca = attr as CmdletAttribute;
                if (ca != null)
                    break;
            }

            if (ca == null)
            {
                throw PSTraceSource.NewNotSupportedException();
            }

            string noun = ca.NounName;
            string verb = ca.VerbName;
            string cmdletName = verb + "-" + noun;

            return new CmdletInfo(cmdletName, cmdletType, null, null, _context);
        }

        /// <summary>
        /// Returns a list of all cmdlets...
        /// </summary>
        /// <returns></returns>
        public List<CmdletInfo> GetCmdlets()
        {
            return GetCmdlets("*");
        }

        /// <summary>
        /// Returns all cmdlets whose names match the pattern...
        /// </summary>
        /// <returns>A list of CmdletInfo objects...</returns>
        public List<CmdletInfo> GetCmdlets(string pattern)
        {
            if (pattern == null)
                throw PSTraceSource.NewArgumentNullException(nameof(pattern));

            List<CmdletInfo> cmdlets = new List<CmdletInfo>();

            CmdletInfo current = null;

            CommandSearcher searcher = new CommandSearcher(
                    pattern,
                    SearchResolutionOptions.CommandNameIsPattern,
                    CommandTypes.Cmdlet,
                    _context);
            while (true)
            {
                try
                {
                    if (!searcher.MoveNext())
                    {
                        break;
                    }
                }
                catch (ArgumentException)
                {
                    continue;
                }
                catch (PathTooLongException)
                {
                    continue;
                }
                catch (FileLoadException)
                {
                    continue;
                }
                catch (MetadataException)
                {
                    continue;
                }
                catch (FormatException)
                {
                    continue;
                }

                current = ((IEnumerator)searcher).Current as CmdletInfo;
                if (current != null)
                    cmdlets.Add(current);
            }

            return cmdlets;
        }

        /// <summary>
        /// Searches for PowerShell commands, optionally using wildcard patterns
        /// and optionally return the full path to applications and scripts rather than
        /// the simple command name.
        /// </summary>
        /// <param name="name">The name of the command to use.</param>
        /// <param name="nameIsPattern">If true treat the name as a pattern to search for.</param>
        /// <param name="returnFullName">If true, return the full path to scripts and applications.</param>
        /// <returns>A list of command names...</returns>
        public List<string> GetCommandName(string name, bool nameIsPattern, bool returnFullName)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            List<string> commands = new List<string>();

            foreach (CommandInfo current in this.GetCommands(name, CommandTypes.All, nameIsPattern))
            {
                if (current.CommandType == CommandTypes.Application)
                {
                    string cmdExtension = System.IO.Path.GetExtension(current.Name);
                    if (!string.IsNullOrEmpty(cmdExtension))
                    {
                        // Only add the application in PATHEXT...
                        foreach (string extension in CommandDiscovery.PathExtensions)
                        {
                            if (extension.Equals(cmdExtension, StringComparison.OrdinalIgnoreCase))
                            {
                                if (returnFullName)
                                {
                                    commands.Add(current.Definition);
                                }
                                else
                                {
                                    commands.Add(current.Name);
                                }
                            }
                        }
                    }
                }
                else if (current.CommandType == CommandTypes.ExternalScript)
                {
                    if (returnFullName)
                    {
                        commands.Add(current.Definition);
                    }
                    else
                    {
                        commands.Add(current.Name);
                    }
                }
                else
                {
                    commands.Add(current.Name);
                }
            }

            return commands;
        }

        /// <summary>
        /// Searches for PowerShell commands, optionally using wildcard patterns.
        /// </summary>
        /// <param name="name">The name of the command to use.</param>
        /// <param name="commandTypes">Type of commands to support.</param>
        /// <param name="nameIsPattern">If true treat the name as a pattern to search for.</param>
        /// <returns>Collection of command names...</returns>
        public IEnumerable<CommandInfo> GetCommands(string name, CommandTypes commandTypes, bool nameIsPattern)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            SearchResolutionOptions options = nameIsPattern ?
                (SearchResolutionOptions.CommandNameIsPattern | SearchResolutionOptions.ResolveFunctionPatterns | SearchResolutionOptions.ResolveAliasPatterns)
                : SearchResolutionOptions.None;

            return GetCommands(name, commandTypes, options);
        }

        internal IEnumerable<CommandInfo> GetCommands(string name, CommandTypes commandTypes, SearchResolutionOptions options, CommandOrigin? commandOrigin = null)
        {
            CommandSearcher searcher = new CommandSearcher(
                name,
                options,
                commandTypes,
                _context);

            if (commandOrigin != null)
            {
                searcher.CommandOrigin = commandOrigin.Value;
            }

            while (true)
            {
                try
                {
                    if (!searcher.MoveNext())
                    {
                        break;
                    }
                }
                catch (ArgumentException)
                {
                    continue;
                }
                catch (PathTooLongException)
                {
                    continue;
                }
                catch (FileLoadException)
                {
                    continue;
                }
                catch (MetadataException)
                {
                    continue;
                }
                catch (FormatException)
                {
                    continue;
                }

                CommandInfo commandInfo = ((IEnumerator)searcher).Current as CommandInfo;
                if (commandInfo != null)
                {
                    yield return commandInfo;
                }
            }
        }

        /// <summary>
        /// Executes a piece of text as a script synchronously in the caller's session state.
        /// The given text will be executed in a child scope rather than dot-sourced.
        /// </summary>
        /// <param name="script">The script text to evaluate.</param>
        /// <returns>A collection of PSObjects generated by the script. Never null, but may be empty.</returns>
        /// <exception cref="ParseException">Thrown if there was a parsing error in the script.</exception>
        /// <exception cref="RuntimeException">Represents a script-level exception.</exception>
        /// <exception cref="FlowControlException"></exception>
        public Collection<PSObject> InvokeScript(string script)
        {
            return InvokeScript(script, useNewScope: true, PipelineResultTypes.None, input: null);
        }

        /// <summary>
        /// Executes a piece of text as a script synchronously in the caller's session state.
        /// The given text will be executed in a child scope rather than dot-sourced.
        /// </summary>
        /// <param name="script">The script text to evaluate.</param>
        /// <param name="args">The arguments to the script, available as $args.</param>
        /// <returns>A collection of PSObjects generated by the script. Never null, but may be empty.</returns>
        /// <exception cref="ParseException">Thrown if there was a parsing error in the script.</exception>
        /// <exception cref="RuntimeException">Represents a script-level exception.</exception>
        /// <exception cref="FlowControlException"></exception>
        public Collection<PSObject> InvokeScript(string script, params object[] args)
        {
            return InvokeScript(script, useNewScope: true, PipelineResultTypes.None, input: null, args);
        }

        /// <summary>
        /// Executes a given scriptblock synchronously in the given session state.
        /// The scriptblock will be executed in the calling scope (dot-sourced) rather than in a new child scope.
        /// </summary>
        /// <param name="sessionState">The session state in which to execute the scriptblock.</param>
        /// <param name="scriptBlock">The scriptblock to execute.</param>
        /// <param name="args">The arguments to the scriptblock, available as $args.</param>
        /// <returns>A collection of the PSObjects emitted by the executing scriptblock. Never null, but may be empty.</returns>
        public Collection<PSObject> InvokeScript(
            SessionState sessionState,
            ScriptBlock scriptBlock,
            params object[] args)
        {
            if (scriptBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(scriptBlock));
            }

            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            SessionStateInternal _oldSessionState = _context.EngineSessionState;
            try
            {
                _context.EngineSessionState = sessionState.Internal;
                return InvokeScript(
                    sb: scriptBlock,
                    useNewScope: false,
                    writeToPipeline: PipelineResultTypes.None,
                    input: null,
                    args: args);
            }
            finally
            {
                _context.EngineSessionState = _oldSessionState;
            }
        }

        /// <summary>
        /// Invoke a scriptblock in the current runspace, controlling if it gets a new scope.
        /// </summary>
        /// <param name="useLocalScope">If true, executes the scriptblock in a new child scope, otherwise the scriptblock is dot-sourced into the calling scope.</param>
        /// <param name="scriptBlock">The scriptblock to execute.</param>
        /// <param name="input">Optional input to the command.</param>
        /// <param name="args">Arguments to pass to the scriptblock.</param>
        /// <returns>
        /// A collection of the PSObjects generated by executing the script. Never null, but may be empty.
        /// </returns>
        public Collection<PSObject> InvokeScript(
            bool useLocalScope,
            ScriptBlock scriptBlock,
            IList input,
            params object[] args)
        {
            if (scriptBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(scriptBlock));
            }

            // Force the current runspace onto the callers thread - this is needed
            // if this API is going to be callable through the SessionStateProxy on the runspace.
            var old = System.Management.Automation.Runspaces.Runspace.DefaultRunspace;
            System.Management.Automation.Runspaces.Runspace.DefaultRunspace = _context.CurrentRunspace;
            try
            {
                return InvokeScript(scriptBlock, useLocalScope, PipelineResultTypes.None, input, args);
            }
            finally
            {
                System.Management.Automation.Runspaces.Runspace.DefaultRunspace = old;
            }
        }

        /// <summary>
        /// Executes a piece of text as a script synchronously using the options provided.
        /// </summary>
        /// <param name="script">The script to evaluate.</param>
        /// <param name="useNewScope">If true, evaluate the script in its own scope.
        /// If false, the script will be evaluated in the current scope i.e. it will be dot-sourced.</param>
        /// <param name="writeToPipeline">If set to Output, all output will be streamed
        /// to the output pipe of the calling cmdlet. If set to None, the result will be returned
        /// to the caller as a collection of PSObjects. No other flags are supported at this time and
        /// will result in an exception if used.</param>
        /// <param name="input">The list of objects to use as input to the script.</param>
        /// <param name="args">The array of arguments to the command, available as $args.</param>
        /// <returns>A collection of PSObjects generated by the script. This will be
        /// empty if output was redirected. Never null.</returns>
        /// <exception cref="ParseException">Thrown if there was a parsing error in the script.</exception>
        /// <exception cref="RuntimeException">Represents a script-level exception.</exception>
        /// <exception cref="NotImplementedException">Thrown if any redirect other than output is attempted.</exception>
        /// <exception cref="FlowControlException"></exception>
        public Collection<PSObject> InvokeScript(
            string script,
            bool useNewScope,
            PipelineResultTypes writeToPipeline,
            IList input,
            params object[] args)
        {
            ArgumentNullException.ThrowIfNull(script);

            // Compile the script text into an executable script block.
            ScriptBlock sb = ScriptBlock.Create(_context, script);

            return InvokeScript(sb, useNewScope, writeToPipeline, input, args);
        }

        private Collection<PSObject> InvokeScript(
            ScriptBlock sb,
            bool useNewScope,
            PipelineResultTypes writeToPipeline,
            IList input,
            params object[] args)
        {
            _cmdlet?.ThrowIfStopping();

            Cmdlet cmdletToUse = null;
            ScriptBlock.ErrorHandlingBehavior errorHandlingBehavior = ScriptBlock.ErrorHandlingBehavior.WriteToExternalErrorPipe;

            // Check if they want output
            if ((writeToPipeline & PipelineResultTypes.Output) == PipelineResultTypes.Output)
            {
                cmdletToUse = _cmdlet;
                writeToPipeline &= (~PipelineResultTypes.Output);
            }

            // Check if they want error
            if ((writeToPipeline & PipelineResultTypes.Error) == PipelineResultTypes.Error)
            {
                errorHandlingBehavior = ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe;
                writeToPipeline &= (~PipelineResultTypes.Error);
            }

            if (writeToPipeline != PipelineResultTypes.None)
            {
                // The only output types are Output and Error.
                throw PSTraceSource.NewNotImplementedException();
            }

            // If the cmdletToUse is not null, then the result of the evaluation will be
            // streamed out the output pipe of the cmdlet.
            object rawResult;
            if (cmdletToUse != null)
            {
                sb.InvokeUsingCmdlet(
                    contextCmdlet: cmdletToUse,
                    useLocalScope: useNewScope,
                    errorHandlingBehavior: errorHandlingBehavior,
                    dollarUnder: AutomationNull.Value,
                    input: input,
                    scriptThis: AutomationNull.Value,
                    args: args);
                rawResult = AutomationNull.Value;
            }
            else
            {
                rawResult = sb.DoInvokeReturnAsIs(
                    useLocalScope: useNewScope,
                    errorHandlingBehavior: errorHandlingBehavior,
                    dollarUnder: AutomationNull.Value,
                    input: input,
                    scriptThis: AutomationNull.Value,
                    args: args);
            }

            if (rawResult == AutomationNull.Value)
            {
                return new Collection<PSObject>();
            }

            // If the result is already a collection of PSObjects, just return it...
            Collection<PSObject> result = rawResult as Collection<PSObject>;
            if (result != null)
                return result;

            result = new Collection<PSObject>();

            IEnumerator list = null;
            list = LanguagePrimitives.GetEnumerator(rawResult);

            if (list != null)
            {
                while (list.MoveNext())
                {
                    object val = list.Current;

                    result.Add(LanguagePrimitives.AsPSObjectOrNull(val));
                }
            }
            else
            {
                result.Add(LanguagePrimitives.AsPSObjectOrNull(rawResult));
            }

            return result;
        }

        /// <summary>
        /// Compile a string into a script block.
        /// </summary>
        /// <param name="scriptText">The source text to compile.</param>
        /// <returns>The compiled script block.</returns>
        /// <exception cref="ParseException"></exception>
        public ScriptBlock NewScriptBlock(string scriptText)
        {
            _commandRuntime?.ThrowIfStopping();

            ScriptBlock result = ScriptBlock.Create(_context, scriptText);
            return result;
        }
    }
    #endregion Auxiliary

    /// <summary>
    /// Defines members used by Cmdlets.
    /// All Cmdlets must derive from
    /// <see cref="System.Management.Automation.Cmdlet"/>.
    /// </summary>
    /// <remarks>
    /// Do not attempt to create instances of
    /// <see cref="System.Management.Automation.Cmdlet"/>
    /// or its subclasses.
    /// Instead, derive your own subclasses and mark them with
    /// <see cref="System.Management.Automation.CmdletAttribute"/>,
    /// and when your assembly is included in a shell, the Engine will
    /// take care of instantiating your subclass.
    /// </remarks>
    public abstract partial class PSCmdlet : Cmdlet
    {
        #region private_members

        internal bool HasDynamicParameters
        {
            get { return this is IDynamicParameters; }
        }

        #endregion private_members

        #region public members
        /// <summary>
        /// The name of the parameter set in effect.
        /// </summary>
        /// <value>the parameter set name</value>
        public string ParameterSetName
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return _ParameterSetName;
                }
            }
        }

        /// <summary>
        /// Contains information about the identity of this cmdlet
        /// and how it was invoked.
        /// </summary>
        /// <value></value>
        public new InvocationInfo MyInvocation
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return base.MyInvocation;
                }
            }
        }

        /// <summary>
        /// If the cmdlet declares paging support (via <see cref="CmdletCommonMetadataAttribute.SupportsPaging"/>),
        /// then <see cref="PagingParameters"/> property contains arguments of the paging parameters.
        /// Otherwise <see cref="PagingParameters"/> property is <see langword="null"/>.
        /// </summary>
        public PagingParameters PagingParameters
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    if (!this.CommandInfo.CommandMetadata.SupportsPaging)
                    {
                        return null;
                    }

                    if (_pagingParameters == null)
                    {
                        MshCommandRuntime mshCommandRuntime = this.CommandRuntime as MshCommandRuntime;
                        if (mshCommandRuntime != null)
                        {
                            _pagingParameters = mshCommandRuntime.PagingParameters ?? new PagingParameters(mshCommandRuntime);
                        }
                    }

                    return _pagingParameters;
                }
            }
        }

        private PagingParameters _pagingParameters;

        #region InvokeCommand
        private CommandInvocationIntrinsics _invokeCommand;

        /// <summary>
        /// Provides access to utility routines for executing scripts
        /// and creating script blocks.
        /// </summary>
        /// <value>Returns an object exposing the utility routines.</value>
        public CommandInvocationIntrinsics InvokeCommand
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return _invokeCommand ??= new CommandInvocationIntrinsics(Context, this);
                }
            }
        }
        #endregion InvokeCommand

        #endregion public members

    }
}
