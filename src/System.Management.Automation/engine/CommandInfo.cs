// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;

using Microsoft.PowerShell.Commands;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines the types of commands that PowerShell can execute.
    /// </summary>
    [Flags]
    public enum CommandTypes
    {
        /// <summary>
        /// Aliases create a name that refers to other command types.
        /// Aliases are only persisted within the execution of a single engine.
        /// </summary>
        Alias = 0x0001,

        /// <summary>
        /// Script functions that are defined by a script block.
        /// Functions are only persisted within the execution of a single engine.
        /// </summary>
        Function = 0x0002,

        /// <summary>
        /// Script filters that are defined by a script block.
        /// Filters are only persisted within the execution of a single engine.
        /// </summary>
        Filter = 0x0004,

        /// <summary>
        /// A cmdlet.
        /// </summary>
        Cmdlet = 0x0008,

        /// <summary>
        /// An PowerShell script (*.ps1 file)
        /// </summary>
        ExternalScript = 0x0010,

        /// <summary>
        /// Any existing application (can be console or GUI).
        /// An application can have any extension that can be executed either directly through CreateProcess
        /// or indirectly through ShellExecute.
        /// </summary>
        Application = 0x0020,

        /// <summary>
        /// A script that is built into the runspace configuration.
        /// </summary>
        Script = 0x0040,

        /// <summary>
        /// A Configuration.
        /// </summary>
        Configuration = 0x0100,

        /// <summary>
        /// All possible command types.
        /// NOTE: a CommandInfo instance will never specify All as its CommandType
        /// but All can be used when filtering the CommandTypes.
        /// </summary>
        All = Alias | Function | Filter | Cmdlet | Script | ExternalScript | Application | Configuration,
    }

    /// <summary>
    /// The base class for the information about commands. Contains the basic information about
    /// the command, like name and type.
    /// </summary>
    public abstract class CommandInfo : IHasSessionStateEntryVisibility
    {
        #region ctor

        /// <summary>
        /// Creates an instance of the CommandInfo class with the specified name and type.
        /// </summary>
        /// <param name="name">
        /// The name of the command.
        /// </param>
        /// <param name="type">
        /// The type of the command.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        internal CommandInfo(string name, CommandTypes type)
        {
            // The name can be empty for functions and filters but it
            // can't be null

            ArgumentNullException.ThrowIfNull(name);

            Name = name;
            CommandType = type;
        }

        /// <summary>
        /// Creates an instance of the CommandInfo class with the specified name and type.
        /// </summary>
        /// <param name="name">
        /// The name of the command.
        /// </param>
        /// <param name="type">
        /// The type of the command.
        /// </param>
        /// <param name="context">
        /// The execution context for the command.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        internal CommandInfo(string name, CommandTypes type, ExecutionContext context)
            : this(name, type)
        {
            this.Context = context;
        }

        /// <summary>
        /// This is a copy constructor, used primarily for get-command.
        /// </summary>
        internal CommandInfo(CommandInfo other)
        {
            // Computed fields not copied:
            // this._externalCommandMetadata = other._externalCommandMetadata;
            // this._moduleName = other._moduleName;
            // this.parameterSets = other.parameterSets;
            this.Module = other.Module;
            _visibility = other._visibility;
            Arguments = other.Arguments;
            this.Context = other.Context;
            Name = other.Name;
            CommandType = other.CommandType;
            CopiedCommand = other;
            this.DefiningLanguageMode = other.DefiningLanguageMode;
        }

        /// <summary>
        /// This is a copy constructor, used primarily for get-command.
        /// </summary>
        internal CommandInfo(string name, CommandInfo other)
            : this(other)
        {
            Name = name;
        }

        #endregion ctor

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        // Name

        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        public CommandTypes CommandType { get; private set; } = CommandTypes.Application;

        // CommandType

        /// <summary>
        /// Gets the source of the command (shown by default in Get-Command)
        /// </summary>
        public virtual string Source { get { return this.ModuleName; } }

        /// <summary>
        /// Gets the source version (shown by default in Get-Command)
        /// </summary>
        public virtual Version Version
        {
            get
            {
                if (_version == null)
                {
                    if (Module != null)
                    {
                        if (Module.Version.Equals(new Version(0, 0)))
                        {
                            if (Module.Path.EndsWith(StringLiterals.PowerShellDataFileExtension, StringComparison.OrdinalIgnoreCase))
                            {
                                // Manifest module (.psd1)
                                Module.SetVersion(ModuleIntrinsics.GetManifestModuleVersion(Module.Path));
                            }
                            else if (Module.Path.EndsWith(StringLiterals.PowerShellILAssemblyExtension, StringComparison.OrdinalIgnoreCase) ||
                                     Module.Path.EndsWith(StringLiterals.PowerShellILExecutableExtension, StringComparison.OrdinalIgnoreCase))
                            {
                                // Binary module (.dll or .exe)
                                Module.SetVersion(AssemblyName.GetAssemblyName(Module.Path).Version);
                            }
                        }

                        _version = Module.Version;
                    }
                }

                return _version;
            }
        }

        private Version _version;

        /// <summary>
        /// The execution context this command will run in.
        /// </summary>
        internal ExecutionContext Context
        {
            get
            {
                return _context;
            }

            set
            {
                _context = value;
                if ((value != null) && !this.DefiningLanguageMode.HasValue)
                {
                    this.DefiningLanguageMode = value.LanguageMode;
                }
            }
        }

        private ExecutionContext _context;

        /// <summary>
        /// The language mode that was in effect when this alias was defined.
        /// </summary>
        internal PSLanguageMode? DefiningLanguageMode { get; set; }

        internal virtual HelpCategory HelpCategory
        {
            get { return HelpCategory.None; }
        }

        internal CommandInfo CopiedCommand { get; set; }

        /// <summary>
        /// Internal interface to change the type of a CommandInfo object.
        /// </summary>
        /// <param name="newType"></param>
        internal void SetCommandType(CommandTypes newType)
        {
            CommandType = newType;
        }

        /// <summary>
        /// A string representing the definition of the command.
        /// </summary>
        /// <remarks>
        /// This is overridden by derived classes to return specific
        /// information for the command type.
        /// </remarks>
        public abstract string Definition { get; }

        /// <summary>
        /// This is required for renaming aliases, functions, and filters.
        /// </summary>
        /// <param name="newName">
        /// The new name for the command.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="newName"/> is null or empty.
        /// </exception>
        internal void Rename(string newName)
        {
            ArgumentException.ThrowIfNullOrEmpty(newName);

            Name = newName;
        }

        /// <summary>
        /// For diagnostic purposes.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ModuleCmdletBase.AddPrefixToCommandName(Name, Prefix);
        }

        /// <summary>
        /// Indicates if the command is to be allowed to be executed by a request
        /// external to the runspace.
        /// </summary>
        public virtual SessionStateEntryVisibility Visibility
        {
            get
            {
                return CopiedCommand == null ? _visibility : CopiedCommand.Visibility;
            }

            set
            {
                if (CopiedCommand == null)
                {
                    _visibility = value;
                }
                else
                {
                    CopiedCommand.Visibility = value;
                }

                if (value == SessionStateEntryVisibility.Private && Module != null)
                {
                    Module.ModuleHasPrivateMembers = true;
                }
            }
        }

        private SessionStateEntryVisibility _visibility = SessionStateEntryVisibility.Public;

        /// <summary>
        /// Return a CommandMetadata instance that is never exposed publicly.
        /// </summary>
        internal virtual CommandMetadata CommandMetadata
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Returns the syntax of a command.
        /// </summary>
        internal virtual string Syntax
        {
            get { return Definition; }
        }

        /// <summary>
        /// The module name of this command. It will be empty for commands
        /// not imported from either a module or snapin.
        /// </summary>
        public string ModuleName
        {
            get
            {
                string moduleName = null;

                if (Module != null && !string.IsNullOrEmpty(Module.Name))
                {
                    moduleName = Module.Name;
                }
                else
                {
                    CmdletInfo cmdlet = this as CmdletInfo;
                    if (cmdlet != null && cmdlet.PSSnapIn != null)
                    {
                        moduleName = cmdlet.PSSnapInName;
                    }
                }

                if (moduleName == null)
                    return string.Empty;

                return moduleName;
            }
        }

        /// <summary>
        /// The module that defines this cmdlet. This will be null for commands
        /// that are not defined in the context of a module.
        /// </summary>
        public PSModuleInfo Module { get; internal set; }

        /// <summary>
        /// The remoting capabilities of this cmdlet, when exposed in a context
        /// with ambient remoting.
        /// </summary>
        public RemotingCapability RemotingCapability
        {
            get
            {
                try
                {
                    return ExternalCommandMetadata.RemotingCapability;
                }
                catch (PSNotSupportedException)
                {
                    // Thrown on an alias that hasn't been resolved yet (i.e.: in a module that
                    // hasn't been loaded.) Assume the default.
                    return RemotingCapability.PowerShell;
                }
            }
        }

        /// <summary>
        /// True if the command has dynamic parameters, false otherwise.
        /// </summary>
        internal virtual bool ImplementsDynamicParameters
        {
            get { return false; }
        }

        /// <summary>
        /// Constructs the MergedCommandParameterMetadata, using any arguments that
        /// may have been specified so that dynamic parameters can be determined, if any.
        /// </summary>
        /// <returns></returns>
        private MergedCommandParameterMetadata GetMergedCommandParameterMetadataSafely()
        {
            if (_context == null)
                return null;

            MergedCommandParameterMetadata result;
            if (_context != LocalPipeline.GetExecutionContextFromTLS())
            {
                // In the normal case, _context is from the thread we're on, and we won't get here.
                // But, if it's not, we can't safely get the parameter metadata without running on
                // on the correct thread, because that thread may be busy doing something else.
                // One of the things we do here is change the current scope in execution context,
                // that can mess up the runspace our CommandInfo object came from.

                var runspace = (RunspaceBase)_context.CurrentRunspace;
                if (runspace.CanRunActionInCurrentPipeline())
                {
                    GetMergedCommandParameterMetadata(out result);
                }
                else
                {
                    _context.Events.SubscribeEvent(
                            source: null,
                            eventName: PSEngineEvent.GetCommandInfoParameterMetadata,
                            sourceIdentifier: PSEngineEvent.GetCommandInfoParameterMetadata,
                            data: null,
                            handlerDelegate: new PSEventReceivedEventHandler(OnGetMergedCommandParameterMetadataSafelyEventHandler),
                            supportEvent: true,
                            forwardEvent: false,
                            shouldQueueAndProcessInExecutionThread: true,
                            maxTriggerCount: 1);

                    var eventArgs = new GetMergedCommandParameterMetadataSafelyEventArgs();

                    _context.Events.GenerateEvent(
                        sourceIdentifier: PSEngineEvent.GetCommandInfoParameterMetadata,
                        sender: null,
                        args: new[] { eventArgs },
                        extraData: null,
                        processInCurrentThread: true,
                        waitForCompletionInCurrentThread: true);

                    // An exception happened on a different thread, rethrow it here on the correct thread.
                    eventArgs.Exception?.Throw();

                    return eventArgs.Result;
                }
            }

            GetMergedCommandParameterMetadata(out result);
            return result;
        }

        private sealed class GetMergedCommandParameterMetadataSafelyEventArgs : EventArgs
        {
            public MergedCommandParameterMetadata Result;
            public ExceptionDispatchInfo Exception;
        }

        private void OnGetMergedCommandParameterMetadataSafelyEventHandler(object sender, PSEventArgs args)
        {
            var eventArgs = args.SourceEventArgs as GetMergedCommandParameterMetadataSafelyEventArgs;
            if (eventArgs != null)
            {
                try
                {
                    // Save the result in our event args as the return value.
                    GetMergedCommandParameterMetadata(out eventArgs.Result);
                }
                catch (Exception e)
                {
                    // Save the exception so we can throw it on the correct thread.
                    eventArgs.Exception = ExceptionDispatchInfo.Capture(e);
                }
            }
        }

        private void GetMergedCommandParameterMetadata(out MergedCommandParameterMetadata result)
        {
            // MSFT:652277 - When invoking cmdlets or advanced functions, MyInvocation.MyCommand.Parameters do not contain the dynamic parameters
            // When trying to get parameter metadata for a CommandInfo that has dynamic parameters, a new CommandProcessor will be
            // created out of this CommandInfo and the parameter binding algorithm will be invoked. However, when this happens via
            // 'MyInvocation.MyCommand.Parameter', it's actually retrieving the parameter metadata of the same cmdlet that is currently
            // running. In this case, information about the specified parameters are not kept around in 'MyInvocation.MyCommand', so
            // going through the binding algorithm again won't give us the metadata about the dynamic parameters that should have been
            // discovered already.
            // The fix is to check if the CommandInfo is actually representing the currently running cmdlet. If so, the retrieval of parameter
            // metadata actually stems from the running of the same cmdlet. In this case, we can just use the current CommandProcessor to
            // retrieve all bindable parameters, which should include the dynamic parameters that have been discovered already.
            CommandProcessor processor;
            if (Context.CurrentCommandProcessor != null && Context.CurrentCommandProcessor.CommandInfo == this)
            {
                // Accessing the parameters within the invocation of the same cmdlet/advanced function.
                processor = (CommandProcessor)Context.CurrentCommandProcessor;
            }
            else
            {
                IScriptCommandInfo scriptCommand = this as IScriptCommandInfo;
                processor = scriptCommand != null
                    ? new CommandProcessor(scriptCommand, _context, useLocalScope: true, fromScriptFile: false,
                        sessionState: scriptCommand.ScriptBlock.SessionStateInternal ?? Context.EngineSessionState)
                    : new CommandProcessor((CmdletInfo)this, _context);

                ParameterBinderController.AddArgumentsToCommandProcessor(processor, Arguments);
                CommandProcessorBase oldCurrentCommandProcessor = Context.CurrentCommandProcessor;
                try
                {
                    Context.CurrentCommandProcessor = processor;

                    processor.SetCurrentScopeToExecutionScope();
                    processor.CmdletParameterBinderController.BindCommandLineParametersNoValidation(processor.arguments);
                }
                catch (ParameterBindingException)
                {
                    // Ignore the binding exception if no argument is specified
                    if (processor.arguments.Count > 0)
                    {
                        throw;
                    }
                }
                finally
                {
                    Context.CurrentCommandProcessor = oldCurrentCommandProcessor;
                    processor.RestorePreviousScope();
                }
            }

            result = processor.CmdletParameterBinderController.BindableParameters;
        }

        /// <summary>
        /// Return the parameters for this command.
        /// </summary>
        public virtual Dictionary<string, ParameterMetadata> Parameters
        {
            get
            {
                Dictionary<string, ParameterMetadata> result = new Dictionary<string, ParameterMetadata>(StringComparer.OrdinalIgnoreCase);

                if (ImplementsDynamicParameters && Context != null)
                {
                    MergedCommandParameterMetadata merged = GetMergedCommandParameterMetadataSafely();

                    foreach (KeyValuePair<string, MergedCompiledCommandParameter> pair in merged.BindableParameters)
                    {
                        result.Add(pair.Key, new ParameterMetadata(pair.Value.Parameter));
                    }

                    // Don't cache this data...
                    return result;
                }

                return ExternalCommandMetadata.Parameters;
            }
        }

        internal CommandMetadata ExternalCommandMetadata
        {
            get { return _externalCommandMetadata ??= new CommandMetadata(this, true); }

            set { _externalCommandMetadata = value; }
        }

        private CommandMetadata _externalCommandMetadata;

        /// <summary>
        /// Resolves a full, shortened, or aliased parameter name to the actual
        /// cmdlet parameter name, using PowerShell's standard parameter resolution
        /// algorithm.
        /// </summary>
        /// <param name="name">The name of the parameter to resolve.</param>
        /// <returns>The parameter that matches this name.</returns>
        public ParameterMetadata ResolveParameter(string name)
        {
            MergedCommandParameterMetadata merged = GetMergedCommandParameterMetadataSafely();
            MergedCompiledCommandParameter result = merged.GetMatchingParameter(name, true, true, null);
            return this.Parameters[result.Parameter.Name];
        }

        /// <summary>
        /// Gets the information about the parameters and parameter sets for
        /// this command.
        /// </summary>
        public ReadOnlyCollection<CommandParameterSetInfo> ParameterSets
        {
            get
            {
                if (_parameterSets == null)
                {
                    Collection<CommandParameterSetInfo> parameterSetInfo =
                        GenerateCommandParameterSetInfo();

                    _parameterSets = new ReadOnlyCollection<CommandParameterSetInfo>(parameterSetInfo);
                }

                return _parameterSets;
            }
        }

        internal ReadOnlyCollection<CommandParameterSetInfo> _parameterSets;

        /// <summary>
        /// A possibly incomplete or even incorrect list of types the command could return.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public abstract ReadOnlyCollection<PSTypeName> OutputType { get; }

        /// <summary>
        /// Specifies whether this command was imported from a module or not.
        /// This is used in Get-Command to figure out which of the commands in module session state were imported.
        /// </summary>
        internal bool IsImported { get; set; } = false;

        /// <summary>
        /// The prefix that was used when importing this command.
        /// </summary>
        internal string Prefix { get; set; } = string.Empty;

        /// <summary>
        /// Create a copy of commandInfo for GetCommandCommand so that we can generate parameter
        /// sets based on an argument list (so we can get the dynamic parameters.)
        /// </summary>
        internal virtual CommandInfo CreateGetCommandCopy(object[] argumentList)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Generates the parameter and parameter set info from the cmdlet metadata.
        /// </summary>
        /// <returns>
        /// A collection of CommandParameterSetInfo representing the cmdlet metadata.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The type name is invalid or the length of the type name
        /// exceeds 1024 characters.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// The caller does not have the required permission to load the assembly
        /// or create the type.
        /// </exception>
        /// <exception cref="ParsingMetadataException">
        /// If more than int.MaxValue parameter-sets are defined for the command.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If a parameter defines the same parameter-set name multiple times.
        /// If the attributes could not be read from a property or field.
        /// </exception>
        internal Collection<CommandParameterSetInfo> GenerateCommandParameterSetInfo()
        {
            Collection<CommandParameterSetInfo> result;

            if (IsGetCommandCopy && ImplementsDynamicParameters)
            {
                result = GetParameterMetadata(CommandMetadata, GetMergedCommandParameterMetadataSafely());
            }
            else
            {
                result = GetCacheableMetadata(CommandMetadata);
            }

            return result;
        }

        /// <summary>
        /// Gets or sets whether this CmdletInfo instance is a copy used for get-command.
        /// If true, and the cmdlet supports dynamic parameters, it means that the dynamic
        /// parameter metadata will be merged into the parameter set information.
        /// </summary>
        internal bool IsGetCommandCopy { get; set; }

        /// <summary>
        /// Gets or sets the command line arguments/parameters that were specified
        /// which will allow for the dynamic parameters to be retrieved and their
        /// metadata merged into the parameter set information.
        /// </summary>
        internal object[] Arguments { get; set; }

        internal static Collection<CommandParameterSetInfo> GetCacheableMetadata(CommandMetadata metadata)
        {
            return GetParameterMetadata(metadata, metadata.StaticCommandParameterMetadata);
        }

        internal static Collection<CommandParameterSetInfo> GetParameterMetadata(CommandMetadata metadata, MergedCommandParameterMetadata parameterMetadata)
        {
            Collection<CommandParameterSetInfo> result = new Collection<CommandParameterSetInfo>();

            if (parameterMetadata != null)
            {
                if (parameterMetadata.ParameterSetCount == 0)
                {
                    const string parameterSetName = ParameterAttribute.AllParameterSets;

                    result.Add(
                        new CommandParameterSetInfo(
                            parameterSetName,
                            false,
                            uint.MaxValue,
                            parameterMetadata));
                }
                else
                {
                    int parameterSetCount = parameterMetadata.ParameterSetCount;
                    for (int index = 0; index < parameterSetCount; ++index)
                    {
                        uint currentFlagPosition = (uint)0x1 << index;

                        // Get the parameter set name
                        string parameterSetName = parameterMetadata.GetParameterSetName(currentFlagPosition);

                        // Is the parameter set the default?
                        bool isDefaultParameterSet = (currentFlagPosition & metadata.DefaultParameterSetFlag) != 0;

                        result.Add(
                            new CommandParameterSetInfo(
                                parameterSetName,
                                isDefaultParameterSet,
                                currentFlagPosition,
                                parameterMetadata));
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Represents <see cref="System.Type"/>, but can be used where a real type
    /// might not be available, in which case the name of the type can be used.
    /// </summary>
    public class PSTypeName
    {
        /// <summary>
        /// This constructor is used when the type exists and is currently loaded.
        /// </summary>
        /// <param name="type">The type.</param>
        public PSTypeName(Type type)
        {
            _type = type;
            if (_type != null)
            {
                Name = _type.FullName;
            }
        }

        /// <summary>
        /// This constructor is used when the type may not exist, or is not loaded.
        /// </summary>
        /// <param name="name">The name of the type.</param>
        public PSTypeName(string name)
        {
            Name = name;
            _type = null;
        }

        /// <summary>
        /// This constructor is used when the creating a PSObject with a custom typename.
        /// </summary>
        /// <param name="name">The name of the type.</param>
        /// <param name="type">The real type.</param>
        public PSTypeName(string name, Type type)
        {
            Name = name;
            _type = type;
        }

        /// <summary>
        /// This constructor is used when the type is defined in PowerShell.
        /// </summary>
        /// <param name="typeDefinitionAst">The type definition from the ast.</param>
        public PSTypeName(TypeDefinitionAst typeDefinitionAst)
        {
            if (typeDefinitionAst == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(typeDefinitionAst));
            }

            TypeDefinitionAst = typeDefinitionAst;
            Name = typeDefinitionAst.Name;
        }

        /// <summary>
        /// This constructor creates a type from a ITypeName.
        /// </summary>
        public PSTypeName(ITypeName typeName)
        {
            if (typeName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(typeName));
            }

            _type = typeName.GetReflectionType();
            if (_type != null)
            {
                Name = _type.FullName;
            }
            else
            {
                var t = typeName as TypeName;
                if (t != null && t._typeDefinitionAst != null)
                {
                    TypeDefinitionAst = t._typeDefinitionAst;
                    Name = TypeDefinitionAst.Name;
                }
                else
                {
                    _type = null;
                    Name = typeName.FullName;
                }
            }
        }

        /// <summary>
        /// Return the name of the type.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Return the type with metadata, or null if the type is not loaded.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public Type Type
        {
            get
            {
                if (!_typeWasCalculated)
                {
                    if (_type == null)
                    {
                        if (TypeDefinitionAst != null)
                        {
                            _type = TypeDefinitionAst.Type;
                        }
                        else
                        {
                            TypeResolver.TryResolveType(Name, out _type);
                        }
                    }

                    if (_type == null)
                    {
                        // We ignore the exception.
                        if (Name != null &&
                            Name.StartsWith('[') &&
                            Name.EndsWith(']'))
                        {
                            string tmp = Name.Substring(1, Name.Length - 2);
                            TypeResolver.TryResolveType(tmp, out _type);
                        }
                    }

                    _typeWasCalculated = true;
                }

                return _type;
            }
        }

        private Type _type;

        /// <summary>
        /// When a type is defined by PowerShell, the ast for that type.
        /// </summary>
        public TypeDefinitionAst TypeDefinitionAst { get; }

        private bool _typeWasCalculated;

        /// <summary>
        /// Returns a String that represents the current PSTypeName.
        /// </summary>
        /// <returns>String that represents the current PSTypeName.</returns>
        public override string ToString()
        {
            return Name ?? string.Empty;
        }
    }

    [DebuggerDisplay("{PSTypeName} {Name}")]
    internal readonly struct PSMemberNameAndType
    {
        public readonly string Name;

        public readonly PSTypeName PSTypeName;

        public readonly object Value;

        public PSMemberNameAndType(string name, PSTypeName typeName, object value = null)
        {
            Name = name;
            PSTypeName = typeName;
            Value = value;
        }
    }

    /// <summary>
    /// Represents dynamic types such as <see cref="System.Management.Automation.PSObject"/>,
    /// but can be used where a real type might not be available, in which case the name of the type can be used.
    /// The type encodes the members of dynamic objects in the type name.
    /// </summary>
    internal sealed class PSSyntheticTypeName : PSTypeName
    {
        internal static PSSyntheticTypeName Create(string typename, IList<PSMemberNameAndType> membersTypes) => Create(new PSTypeName(typename), membersTypes);

        internal static PSSyntheticTypeName Create(Type type, IList<PSMemberNameAndType> membersTypes) => Create(new PSTypeName(type), membersTypes);

        internal static PSSyntheticTypeName Create(PSTypeName typename, IList<PSMemberNameAndType> membersTypes)
        {
            var typeName = GetMemberTypeProjection(typename.Name, membersTypes);
            var members = new List<PSMemberNameAndType>();
            members.AddRange(membersTypes);
            members.Sort(static (c1, c2) => string.Compare(c1.Name, c2.Name, StringComparison.OrdinalIgnoreCase));
            return new PSSyntheticTypeName(typeName, typename.Type, members);
        }

        private PSSyntheticTypeName(string typeName, Type type, IList<PSMemberNameAndType> membersTypes)
        : base(typeName, type)
        {
            Members = membersTypes;
            if (type != typeof(PSObject))
            {
                return;
            }

            for (int i = 0; i < Members.Count; i++)
            {
                var psMemberNameAndType = Members[i];
                if (IsPSTypeName(psMemberNameAndType))
                {
                    Members.RemoveAt(i);
                    break;
                }
            }
        }

        private static bool IsPSTypeName(in PSMemberNameAndType member) => member.Name.Equals(nameof(PSTypeName), StringComparison.OrdinalIgnoreCase);

        private static string GetMemberTypeProjection(string typename, IList<PSMemberNameAndType> members)
        {
            if (typename == typeof(PSObject).FullName)
            {
                foreach (var mem in members)
                {
                    if (IsPSTypeName(mem))
                    {
                        typename = mem.Value.ToString();
                    }
                }
            }

            var builder = new StringBuilder(typename, members.Count * 7);
            builder.Append('#');
            foreach (var m in members.OrderBy(static m => m.Name))
            {
                if (!IsPSTypeName(m))
                {
                    builder.Append(m.Name).Append(':');
                }
            }

            builder.Length--;
            return builder.ToString();
        }

        public IList<PSMemberNameAndType> Members { get; }
    }

#nullable enable
    internal interface IScriptCommandInfo
    {
        ScriptBlock ScriptBlock { get; }
    }
}
