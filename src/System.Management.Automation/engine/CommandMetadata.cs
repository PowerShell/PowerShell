// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Reflection;
using System.Text;

using Microsoft.PowerShell.Commands;

using Dbg = System.Diagnostics.Debug;
using System.Diagnostics.CodeAnalysis;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines session capabilities provided by a PowerShell session.
    /// </summary>
    /// <seealso cref="System.Management.Automation.Runspaces.InitialSessionState.CreateRestricted"/>
    /// <seealso cref="System.Management.Automation.CommandMetadata.GetRestrictedCommands"/>
    [Flags]
    public enum SessionCapabilities
    {
        /// <summary>
        /// Session with <see cref="RemoteServer"/> capabilities can be made available on a server
        /// that wants to provide a full user experience to PowerShell clients.
        /// Clients connecting to the server will be able to use implicit remoting
        /// (Import-PSSession, Export-PSSession) as well as interactive remoting (Enter-PSSession, Exit-PSSession).
        /// </summary>
        RemoteServer = 0x1,

        /// <summary>
        /// Include language capabilities.
        /// </summary>
        Language = 0x4
    }

    /// <summary>
    /// This class represents the compiled metadata for a command type.
    /// </summary>
    [DebuggerDisplay("CommandName = {Name}; Type = {CommandType}")]
    public sealed class CommandMetadata
    {
        #region Public Constructor

        /// <summary>
        /// Constructs a CommandMetadata object for the given CLS complaint type
        /// <paramref name="commandType"/>.
        /// </summary>
        /// <param name="commandType">
        /// CLS complaint type to inspect for Cmdlet metadata.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// commandType is null.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If a parameter defines the same parameter-set name multiple times.
        /// If the attributes could not be read from a property or field.
        /// </exception>
        public CommandMetadata(Type commandType)
        {
            Init(null, null, commandType, false);
        }

        /// <summary>
        /// Construct a CommandMetadata object for the given commandInfo.
        /// </summary>
        /// <param name="commandInfo">
        /// The commandInfo object to construct CommandMetadata for
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// commandInfo is null.
        /// </exception>
        /// <exception cref="PSNotSupportedException">
        /// If the commandInfo is an alias to an unknown command, or if the commandInfo
        /// is an unsupported command type.
        /// </exception>
        public CommandMetadata(CommandInfo commandInfo)
            : this(commandInfo, false)
        {
        }

        /// <summary>
        /// Construct a CommandMetadata object for the given commandInfo.
        /// </summary>
        /// <param name="commandInfo">
        /// The commandInfo object to construct CommandMetadata for
        /// </param>
        /// <param name="shouldGenerateCommonParameters">
        /// Should common parameters be included in the metadata?
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// commandInfo is null.
        /// </exception>
        /// <exception cref="PSNotSupportedException">
        /// If the commandInfo is an alias to an unknown command, or if the commandInfo
        /// is an unsupported command type.
        /// </exception>
        public CommandMetadata(CommandInfo commandInfo, bool shouldGenerateCommonParameters)
        {
            if (commandInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandInfo));
            }
            while (commandInfo is AliasInfo)
            {
                commandInfo = ((AliasInfo)commandInfo).ResolvedCommand;
                if (commandInfo == null)
                {
                    throw PSTraceSource.NewNotSupportedException();
                }
            }

            CmdletInfo cmdletInfo;
            ExternalScriptInfo scriptInfo;
            FunctionInfo funcInfo;
            if ((cmdletInfo = commandInfo as CmdletInfo) != null)
            {
                Init(commandInfo.Name, cmdletInfo.FullName, cmdletInfo.ImplementingType, shouldGenerateCommonParameters);
            }
            else if ((scriptInfo = commandInfo as ExternalScriptInfo) != null)
            {
                // Accessing the script block property here reads and parses the script
                Init(scriptInfo.ScriptBlock, scriptInfo.Path, shouldGenerateCommonParameters);
                _wrappedCommandType = CommandTypes.ExternalScript;
            }
            else if ((funcInfo = commandInfo as FunctionInfo) != null)
            {
                Init(funcInfo.ScriptBlock, funcInfo.Name, shouldGenerateCommonParameters);
                _wrappedCommandType = commandInfo.CommandType;
            }
            else
            {
                throw PSTraceSource.NewNotSupportedException();
            }
        }

        /// <summary>
        /// Construct a CommandMetadata object for a script file.
        /// </summary>
        /// <param name="path">The path to the script file.</param>
        public CommandMetadata(string path)
        {
            string scriptName = IO.Path.GetFileName(path);
            ExternalScriptInfo scriptInfo = new ExternalScriptInfo(scriptName, path);

            Init(scriptInfo.ScriptBlock, path, false);
            _wrappedCommandType = CommandTypes.ExternalScript;
        }

        /// <summary>
        /// A copy constructor that creates a deep copy of the <paramref name="other"/> CommandMetadata object.
        /// Instances of Attribute and Type classes are copied by reference.
        /// </summary>
        /// <param name="other">Object to copy.</param>
        public CommandMetadata(CommandMetadata other)
        {
            if (other == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(other));
            }

            Name = other.Name;
            ConfirmImpact = other.ConfirmImpact;
            _defaultParameterSetFlag = other._defaultParameterSetFlag;
            _defaultParameterSetName = other._defaultParameterSetName;
            _implementsDynamicParameters = other._implementsDynamicParameters;
            SupportsShouldProcess = other.SupportsShouldProcess;
            SupportsPaging = other.SupportsPaging;
            SupportsTransactions = other.SupportsTransactions;
            this.CommandType = other.CommandType;
            _wrappedAnyCmdlet = other._wrappedAnyCmdlet;
            _wrappedCommand = other._wrappedCommand;
            _wrappedCommandType = other._wrappedCommandType;

            _parameters = new Dictionary<string, ParameterMetadata>(other.Parameters.Count, StringComparer.OrdinalIgnoreCase);

            // deep copy
            foreach (KeyValuePair<string, ParameterMetadata> entry in other.Parameters)
            {
                _parameters.Add(entry.Key, new ParameterMetadata(entry.Value));
            }

            // deep copy of the collection, collection items (Attributes) copied by reference
            if (other._otherAttributes == null)
            {
                _otherAttributes = null;
            }
            else
            {
                _otherAttributes = new Collection<Attribute>(new List<Attribute>(other._otherAttributes.Count));
                foreach (Attribute attribute in other._otherAttributes)
                {
                    _otherAttributes.Add(attribute);
                }
            }

            // not copying those fields/members as they are untouched (and left set to null)
            // by public constructors, so we can't rely on those fields/members to be set
            // when CommandMetadata comes from a user
            _staticCommandParameterMetadata = null;
        }

        /// <summary>
        /// Constructor used by implicit remoting.
        /// </summary>
        internal CommandMetadata(
            string name,
            CommandTypes commandType,
            bool isProxyForCmdlet,
            string defaultParameterSetName,
            bool supportsShouldProcess,
            ConfirmImpact confirmImpact,
            bool supportsPaging,
            bool supportsTransactions,
            bool positionalBinding,
            Dictionary<string, ParameterMetadata> parameters)
        {
            Name = _wrappedCommand = name;
            _wrappedCommandType = commandType;
            _wrappedAnyCmdlet = isProxyForCmdlet;
            _defaultParameterSetName = defaultParameterSetName;
            SupportsShouldProcess = supportsShouldProcess;
            SupportsPaging = supportsPaging;
            ConfirmImpact = confirmImpact;
            SupportsTransactions = supportsTransactions;
            PositionalBinding = positionalBinding;
            this.Parameters = parameters;
        }

        private void Init(string name, string fullyQualifiedName, Type commandType, bool shouldGenerateCommonParameters)
        {
            Name = name;
            this.CommandType = commandType;

            if (commandType != null)
            {
                ConstructCmdletMetadataUsingReflection();
                _shouldGenerateCommonParameters = shouldGenerateCommonParameters;
            }

            // Use fully qualified name if available.
            _wrappedCommand = !string.IsNullOrEmpty(fullyQualifiedName) ? fullyQualifiedName : Name;
            _wrappedCommandType = CommandTypes.Cmdlet;
            _wrappedAnyCmdlet = true;
        }

        private void Init(ScriptBlock scriptBlock, string name, bool shouldGenerateCommonParameters)
        {
            if (scriptBlock.UsesCmdletBinding)
            {
                _wrappedAnyCmdlet = true;
            }
            else
            {
                // Ignore what was passed in, there are no common parameters if cmdlet binding is not used.
                shouldGenerateCommonParameters = false;
            }

            CmdletBindingAttribute cmdletBindingAttribute = scriptBlock.CmdletBindingAttribute;
            if (cmdletBindingAttribute != null)
            {
                ProcessCmdletAttribute(cmdletBindingAttribute);
            }
            else if (scriptBlock.UsesCmdletBinding)
            {
                _defaultParameterSetName = null;
            }

            Obsolete = scriptBlock.ObsoleteAttribute;
            _scriptBlock = scriptBlock;
            _wrappedCommand = Name = name;
            _shouldGenerateCommonParameters = shouldGenerateCommonParameters;
        }

        #endregion

        #region ctor

        /// <summary>
        /// Gets the metadata for the specified cmdlet from the cache or creates
        /// a new instance if its not in the cache.
        /// </summary>
        /// <param name="commandName">
        /// The name of the command that this metadata represents.
        /// </param>
        /// <param name="cmdletType">
        /// The cmdlet to get the metadata for.
        /// </param>
        /// <param name="context">
        /// The current engine context.
        /// </param>
        /// <returns>
        /// The CommandMetadata for the specified cmdlet.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="commandName"/> is null or empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="cmdletType"/> is null.
        /// </exception>
        /// <exception cref="ParsingMetadataException">
        /// If more than int.MaxValue parameter-sets are defined for the command.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If a parameter defines the same parameter-set name multiple times.
        /// If the attributes could not be read from a property or field.
        /// </exception>
        internal static CommandMetadata Get(string commandName, Type cmdletType, ExecutionContext context)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw PSTraceSource.NewArgumentException(nameof(commandName));
            }

            CommandMetadata result = null;

            if ((context != null) && (cmdletType != null))
            {
                string cmdletTypeName = cmdletType.AssemblyQualifiedName;
                s_commandMetadataCache.TryGetValue(cmdletTypeName, out result);
            }

            if (result == null)
            {
                result = new CommandMetadata(commandName, cmdletType, context);

                if ((context != null) && (cmdletType != null))
                {
                    string cmdletTypeName = cmdletType.AssemblyQualifiedName;
                    s_commandMetadataCache.TryAdd(cmdletTypeName, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Constructs an instance of CommandMetadata using reflection against a bindable object.
        /// </summary>
        /// <param name="commandName">
        /// The name of the command that this metadata represents.
        /// </param>
        /// <param name="cmdletType">
        /// An instance of an object type that can be used to bind MSH parameters. A type is
        /// considered bindable if it has at least one field and/or property that is decorated
        /// with the ParameterAttribute.
        /// </param>
        /// <param name="context">
        /// The current engine context. If null, the command and type metadata will be generated
        /// and will not be cached.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="cmdletType"/> is null.
        /// </exception>
        /// <exception cref="ParsingMetadataException">
        /// If more than int.MaxValue parameter-sets are defined for the command.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If a parameter defines the same parameter-set name multiple times.
        /// If the attributes could not be read from a property or field.
        /// </exception>
        internal CommandMetadata(string commandName, Type cmdletType, ExecutionContext context)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw PSTraceSource.NewArgumentException(nameof(commandName));
            }

            Name = commandName;
            this.CommandType = cmdletType;

            if (cmdletType != null)
            {
                InternalParameterMetadata parameterMetadata = InternalParameterMetadata.Get(cmdletType, context, false);
                ConstructCmdletMetadataUsingReflection();
                _staticCommandParameterMetadata = MergeParameterMetadata(context, parameterMetadata, true);
                _defaultParameterSetFlag = _staticCommandParameterMetadata.GenerateParameterSetMappingFromMetadata(_defaultParameterSetName);
                _staticCommandParameterMetadata.MakeReadOnly();
            }
        }

        /// <summary>
        /// Constructor for creating command metadata from a script block.
        /// </summary>
        /// <param name="scriptblock"></param>
        /// <param name="context"></param>
        /// <param name="commandName"></param>
        /// <remarks>
        /// Unlike cmdlet based on a C# type where cmdlet metadata and parameter
        /// metadata is created through reflecting the implementation type, script
        /// cmdlet has different way for constructing metadata.
        ///
        ///     1. Metadata for cmdlet itself comes from cmdlet statement, which
        ///        is parsed into CmdletDeclarationNode and then converted into
        ///        a CmdletAttribute object.
        ///     2. Metadata for parameter comes from parameter declaration statement,
        ///        which is parsed into parameter nodes with parameter annotations.
        ///        Information in ParameterNodes is eventually transformed into a
        ///        dictionary of RuntimeDefinedParameters.
        ///
        /// By the time this constructor is called, information about CmdletAttribute
        /// and RuntimeDefinedParameters for the script block has been setup with
        /// the scriptblock object.
        /// </remarks>
        internal CommandMetadata(ScriptBlock scriptblock, string commandName, ExecutionContext context)
        {
            if (scriptblock == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(scriptblock));
            }

            CmdletBindingAttribute cmdletBindingAttribute = scriptblock.CmdletBindingAttribute;

            if (cmdletBindingAttribute != null)
            {
                ProcessCmdletAttribute(cmdletBindingAttribute);
            }
            else
            {
                _defaultParameterSetName = null;
            }

            Obsolete = scriptblock.ObsoleteAttribute;
            Name = commandName;
            this.CommandType = typeof(PSScriptCmdlet);

            if (scriptblock.HasDynamicParameters)
            {
                _implementsDynamicParameters = true;
            }

            InternalParameterMetadata parameterMetadata = InternalParameterMetadata.Get(scriptblock.RuntimeDefinedParameters, false,
                                                                                        scriptblock.UsesCmdletBinding);
            _staticCommandParameterMetadata = MergeParameterMetadata(context, parameterMetadata, scriptblock.UsesCmdletBinding);
            _defaultParameterSetFlag = _staticCommandParameterMetadata.GenerateParameterSetMappingFromMetadata(_defaultParameterSetName);
            _staticCommandParameterMetadata.MakeReadOnly();
        }

        #endregion ctor

        #region Public Properties

        /// <summary>
        /// Gets the name of the command this metadata represents.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The Type which this CommandMetadata represents.
        /// </summary>
        public Type CommandType { get; private set; }

        // The ScriptBlock which this CommandMetadata represents.
        private ScriptBlock _scriptBlock;

        /// <summary>
        /// Gets/Sets the default parameter set name.
        /// </summary>
        public string DefaultParameterSetName
        {
            get
            {
                return _defaultParameterSetName;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    value = ParameterAttribute.AllParameterSets;
                }

                _defaultParameterSetName = value;
            }
        }

        private string _defaultParameterSetName = ParameterAttribute.AllParameterSets;

        /// <summary>
        /// True if the cmdlet declared that it supports ShouldProcess, false otherwise.
        /// </summary>
        /// <value></value>
        public bool SupportsShouldProcess { get; set; }

        /// <summary>
        /// True if the cmdlet declared that it supports Paging, false otherwise.
        /// </summary>
        /// <value></value>
        public bool SupportsPaging { get; set; }

        /// <summary>
        /// When true, the command will auto-generate appropriate parameter metadata to support positional
        /// parameters if the script hasn't already specified multiple parameter sets or specified positions
        /// explicitly via the <see cref="ParameterAttribute"/>.
        /// </summary>
        public bool PositionalBinding { get; set; } = true;

        /// <summary>
        /// True if the cmdlet declared that it supports transactions, false otherwise.
        /// </summary>
        /// <value></value>
        public bool SupportsTransactions { get; set; }

        /// <summary>
        /// Related link URI for Get-Help -Online.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public string HelpUri { get; set; } = string.Empty;

        /// <summary>
        /// The remoting capabilities of this cmdlet, when exposed in a context
        /// with ambient remoting.
        /// </summary>
        public RemotingCapability RemotingCapability
        {
            get
            {
                RemotingCapability currentRemotingCapability = _remotingCapability;
                if ((currentRemotingCapability == Automation.RemotingCapability.PowerShell) &&
                    ((this.Parameters != null) && this.Parameters.ContainsKey("ComputerName")))
                {
                    _remotingCapability = Automation.RemotingCapability.SupportedByCommand;
                }

                return _remotingCapability;
            }

            set
            {
                _remotingCapability = value;
            }
        }

        private RemotingCapability _remotingCapability = RemotingCapability.PowerShell;

        /// <summary>
        /// Indicates the "destructiveness" of the command operation and
        /// when it should be confirmed.  This is only effective when
        /// the command calls ShouldProcess, which should only occur when
        /// SupportsShouldProcess is specified.
        /// </summary>
        /// <value></value>
        public ConfirmImpact ConfirmImpact { get; set; } = ConfirmImpact.Medium;

        /// <summary>
        /// Gets the parameter data for this command.
        /// </summary>
        public Dictionary<string, ParameterMetadata> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    // Return parameters for a script block
                    if (_scriptBlock != null)
                    {
                        InternalParameterMetadata parameterMetadata = InternalParameterMetadata.Get(_scriptBlock.RuntimeDefinedParameters, false,
                                                                            _scriptBlock.UsesCmdletBinding);
                        MergedCommandParameterMetadata mergedCommandParameterMetadata =
                            MergeParameterMetadata(null, parameterMetadata, _shouldGenerateCommonParameters);
                        _parameters = ParameterMetadata.GetParameterMetadata(mergedCommandParameterMetadata);
                    }
                    else if (this.CommandType != null)
                    {
                        // Construct compiled parameter metadata from this
                        InternalParameterMetadata parameterMetadata = InternalParameterMetadata.Get(this.CommandType, null, false);
                        MergedCommandParameterMetadata mergedCommandParameterMetadata =
                            MergeParameterMetadata(null, parameterMetadata, _shouldGenerateCommonParameters);

                        // Construct parameter metadata from compiled parameter metadata
                        // compiled parameter metadata is used for internal purposes. It has lots of information
                        // which is used by ParameterBinder.
                        _parameters = ParameterMetadata.GetParameterMetadata(mergedCommandParameterMetadata);
                    }
                }

                return _parameters;
            }

            private set
            {
                _parameters = value;
            }
        }

        private Dictionary<string, ParameterMetadata> _parameters;
        private bool _shouldGenerateCommonParameters;

        /// <summary>
        /// Gets or sets the obsolete attribute on the command.
        /// </summary>
        /// <value></value>
        internal ObsoleteAttribute Obsolete { get; set; }

        #endregion

        #region internal members

        /// <summary>
        /// Gets the merged metadata for the command including cmdlet declared parameters,
        /// common parameters, and (optionally) ShouldProcess and Transactions parameters.
        /// </summary>
        /// <value></value>
        internal MergedCommandParameterMetadata StaticCommandParameterMetadata
        {
            get
            {
                return _staticCommandParameterMetadata;
            }
        }

        private readonly MergedCommandParameterMetadata _staticCommandParameterMetadata;

        /// <summary>
        /// True if the cmdlet implements dynamic parameters, or false otherwise.
        /// </summary>
        /// <value></value>
        internal bool ImplementsDynamicParameters
        {
            get { return _implementsDynamicParameters; }
        }

        private bool _implementsDynamicParameters;

        /// <summary>
        /// Gets the bit in the parameter set map for the default parameter set.
        /// </summary>
        internal uint DefaultParameterSetFlag
        {
            get { return _defaultParameterSetFlag; }

            set { _defaultParameterSetFlag = value; }
        }

        private uint _defaultParameterSetFlag;

        /// <summary>
        /// A collection of attributes that were declared at the cmdlet level but were not
        /// recognized by the engine.
        /// </summary>
        private readonly Collection<Attribute> _otherAttributes = new Collection<Attribute>();

        // command this CommandMetadata instance is intended to wrap
        private string _wrappedCommand;
        // the type of command this CommandMetadata instance is intended to wrap
        private CommandTypes _wrappedCommandType;
        // The CommandType for a script cmdlet is not CommandTypes.Cmdlet, yet
        // proxy generation needs to know the difference between script and script cmdlet.
        private bool _wrappedAnyCmdlet;

        internal bool WrappedAnyCmdlet
        {
            get { return _wrappedAnyCmdlet; }
        }

        internal CommandTypes WrappedCommandType
        {
            get
            {
                return _wrappedCommandType;
            }
        }

        #endregion internal members

        #region helper methods

        /// <summary>
        /// Constructs the command metadata by using reflection against the
        /// CLR type.
        /// </summary>
        /// <exception cref="ParsingMetadataException">
        /// If more than int.MaxValue parameter-sets are defined for the command.
        /// </exception>
        private void ConstructCmdletMetadataUsingReflection()
        {
            Diagnostics.Assert(
                CommandType != null,
                "This method should only be called when constructed with the Type");

            // Determine if the cmdlet implements dynamic parameters by looking for the interface

            Type dynamicParametersType = CommandType.GetInterface(nameof(IDynamicParameters), true);

            if (dynamicParametersType != null)
            {
                _implementsDynamicParameters = true;
            }

            // Process the attributes on the cmdlet

            var customAttributes = CommandType.GetCustomAttributes(false);

            foreach (Attribute attribute in customAttributes)
            {
                CmdletAttribute cmdletAttribute = attribute as CmdletAttribute;
                if (cmdletAttribute != null)
                {
                    ProcessCmdletAttribute(cmdletAttribute);
                    this.Name = cmdletAttribute.VerbName + "-" + cmdletAttribute.NounName;
                }
                else if (attribute is ObsoleteAttribute)
                {
                    Obsolete = (ObsoleteAttribute)attribute;
                }
                else
                {
                    _otherAttributes.Add(attribute);
                }
            }
        }

        /// <summary>
        /// Extracts the cmdlet data from the CmdletAttribute.
        /// </summary>
        /// <param name="attribute">
        /// The CmdletAttribute to process
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="attribute"/> is null.
        /// </exception>
        /// <exception cref="ParsingMetadataException">
        /// If more than int.MaxValue parameter-sets are defined for the command.
        /// </exception>
        private void ProcessCmdletAttribute(CmdletCommonMetadataAttribute attribute)
        {
            if (attribute == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(attribute));
            }

            // Process the default parameter set name
            _defaultParameterSetName = attribute.DefaultParameterSetName;

            // Check to see if the cmdlet supports ShouldProcess
            SupportsShouldProcess = attribute.SupportsShouldProcess;

            // Determine the cmdlet's impact confirmation
            ConfirmImpact = attribute.ConfirmImpact;

            // Check to see if the cmdlet supports paging
            SupportsPaging = attribute.SupportsPaging;

            // Check to see if the cmdlet supports transactions
            SupportsTransactions = attribute.SupportsTransactions;

            // Grab related link
            HelpUri = attribute.HelpUri;

            // Remoting support
            _remotingCapability = attribute.RemotingCapability;

            // Check to see if the cmdlet uses positional binding
            var cmdletBindingAttribute = attribute as CmdletBindingAttribute;
            if (cmdletBindingAttribute != null)
            {
                PositionalBinding = cmdletBindingAttribute.PositionalBinding;
            }
        }

        /// <summary>
        /// Merges parameter metadata from different sources: those that are coming from Type,
        /// CommonParameters, should process etc.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parameterMetadata"></param>
        /// <param name="shouldGenerateCommonParameters">
        /// true if metadata info about Verbose,Debug etc needs to be generated.
        /// false otherwise.
        /// </param>
        private MergedCommandParameterMetadata MergeParameterMetadata(ExecutionContext context, InternalParameterMetadata parameterMetadata, bool shouldGenerateCommonParameters)
        {
            // Create an instance of the static metadata class
            MergedCommandParameterMetadata staticCommandParameterMetadata =
                new MergedCommandParameterMetadata();

            // First add the metadata for the formal cmdlet parameters
            staticCommandParameterMetadata.AddMetadataForBinder(
                parameterMetadata,
                ParameterBinderAssociation.DeclaredFormalParameters);

            // Now add the common parameters metadata
            if (shouldGenerateCommonParameters)
            {
                InternalParameterMetadata commonParametersMetadata =
                    InternalParameterMetadata.Get(typeof(CommonParameters), context, false);

                staticCommandParameterMetadata.AddMetadataForBinder(
                    commonParametersMetadata,
                    ParameterBinderAssociation.CommonParameters);

                // If the command supports ShouldProcess, add the metadata for
                // those parameters
                if (this.SupportsShouldProcess)
                {
                    InternalParameterMetadata shouldProcessParametersMetadata =
                        InternalParameterMetadata.Get(typeof(ShouldProcessParameters), context, false);

                    staticCommandParameterMetadata.AddMetadataForBinder(
                        shouldProcessParametersMetadata,
                        ParameterBinderAssociation.ShouldProcessParameters);
                }

                // If the command supports paging, add the metadata for
                // those parameters
                if (this.SupportsPaging)
                {
                    InternalParameterMetadata pagingParametersMetadata =
                        InternalParameterMetadata.Get(typeof(PagingParameters), context, false);

                    staticCommandParameterMetadata.AddMetadataForBinder(
                        pagingParametersMetadata,
                        ParameterBinderAssociation.PagingParameters);
                }

                // If the command supports transactions, add the metadata for
                // those parameters
                if (this.SupportsTransactions)
                {
                    InternalParameterMetadata transactionParametersMetadata =
                        InternalParameterMetadata.Get(typeof(TransactionParameters), context, false);

                    staticCommandParameterMetadata.AddMetadataForBinder(
                        transactionParametersMetadata,
                        ParameterBinderAssociation.TransactionParameters);
                }
            }

            return staticCommandParameterMetadata;
        }

        #endregion helper methods

        #region Proxy Command generation

        /// <summary>
        /// Gets the ScriptCmdlet in string format.
        /// </summary>
        /// <returns></returns>
        internal string GetProxyCommand(string helpComment, bool generateDynamicParameters)
        {
            if (string.IsNullOrEmpty(helpComment))
            {
                helpComment = string.Format(CultureInfo.InvariantCulture, @"
.ForwardHelpTargetName {0}
.ForwardHelpCategory {1}
",
                    _wrappedCommand, _wrappedCommandType);
            }

            string dynamicParamblock = string.Empty;
            if (generateDynamicParameters && this.ImplementsDynamicParameters)
            {
                dynamicParamblock = string.Format(CultureInfo.InvariantCulture, @"
dynamicparam
{{{0}}}

", GetDynamicParamBlock());
            }

            string result = string.Format(CultureInfo.InvariantCulture, @"{0}
param({1})

{2}begin
{{{3}}}

process
{{{4}}}

end
{{{5}}}

clean
{{{6}}}
<#
{7}
#>
",
                GetDecl(),
                GetParamBlock(),
                dynamicParamblock,
                GetBeginBlock(),
                GetProcessBlock(),
                GetEndBlock(),
                GetCleanBlock(),
                CodeGeneration.EscapeBlockCommentContent(helpComment));

            return result;
        }

        internal string GetDecl()
        {
            string result = string.Empty;
            string separator = string.Empty;
            if (_wrappedAnyCmdlet)
            {
                StringBuilder decl = new StringBuilder("[CmdletBinding(");

                if (!string.IsNullOrEmpty(_defaultParameterSetName))
                {
                    decl.Append(separator);
                    decl.Append("DefaultParameterSetName='");
                    decl.Append(CodeGeneration.EscapeSingleQuotedStringContent(_defaultParameterSetName));
                    decl.Append('\'');
                    separator = ", ";
                }

                if (SupportsShouldProcess)
                {
                    decl.Append(separator);
                    decl.Append("SupportsShouldProcess=$true");
                    separator = ", ";
                    decl.Append(separator);
                    decl.Append("ConfirmImpact='");
                    decl.Append(ConfirmImpact);
                    decl.Append('\'');
                }

                if (SupportsPaging)
                {
                    decl.Append(separator);
                    decl.Append("SupportsPaging=$true");
                    separator = ", ";
                }

                if (SupportsTransactions)
                {
                    decl.Append(separator);
                    decl.Append("SupportsTransactions=$true");
                    separator = ", ";
                }

                if (!PositionalBinding)
                {
                    decl.Append(separator);
                    decl.Append("PositionalBinding=$false");
                    separator = ", ";
                }

                if (!string.IsNullOrEmpty(HelpUri))
                {
                    decl.Append(separator);
                    decl.Append("HelpUri='");
                    decl.Append(CodeGeneration.EscapeSingleQuotedStringContent(HelpUri));
                    decl.Append('\'');
                    separator = ", ";
                }

                if (_remotingCapability != RemotingCapability.PowerShell)
                {
                    decl.Append(separator);
                    decl.Append("RemotingCapability='");
                    decl.Append(_remotingCapability);
                    decl.Append('\'');
                    separator = ", ";
                }

                decl.Append(")]");

                result = decl.ToString();
            }

            return result;
        }

        internal string GetParamBlock()
        {
            if (Parameters.Keys.Count > 0)
            {
                StringBuilder parameters = new StringBuilder();
                string prefix = string.Concat(Environment.NewLine, "    ");
                string paramDataPrefix = null;

                foreach (var pair in Parameters)
                {
                    if (paramDataPrefix != null)
                    {
                        parameters.Append(paramDataPrefix);
                    }
                    else
                    {
                        // syntax for parameter separation : comma followed by new-line.
                        paramDataPrefix = string.Concat(",", Environment.NewLine);
                    }
                    // generate the parameter proxy and append to the list
                    string paramData = pair.Value.GetProxyParameterData(prefix, pair.Key, _wrappedAnyCmdlet);
                    parameters.Append(paramData);
                }

                return parameters.ToString();
            }

            return string.Empty;
        }

        internal string GetBeginBlock()
        {
            string result;

            if (string.IsNullOrEmpty(_wrappedCommand))
            {
                string error = ProxyCommandStrings.CommandMetadataMissingCommandName;
                throw new InvalidOperationException(error);
            }

            string commandOrigin = "$myInvocation.CommandOrigin";

            // For functions, don't proxy the command origin, otherwise they will
            // be subject to the runspace restrictions
            if (_wrappedCommandType == CommandTypes.Function)
            {
                commandOrigin = string.Empty;
            }

            if (_wrappedAnyCmdlet)
            {
                result = string.Format(CultureInfo.InvariantCulture, @"
    try {{
        $outBuffer = $null
        if ($PSBoundParameters.TryGetValue('OutBuffer', [ref]$outBuffer))
        {{
            $PSBoundParameters['OutBuffer'] = 1
        }}

        $wrappedCmd = $ExecutionContext.InvokeCommand.GetCommand('{0}', [System.Management.Automation.CommandTypes]::{1})
        $scriptCmd = {{& $wrappedCmd @PSBoundParameters }}

        $steppablePipeline = $scriptCmd.GetSteppablePipeline({2})
        $steppablePipeline.Begin($PSCmdlet)
    }} catch {{
        throw
    }}
",
                    CodeGeneration.EscapeSingleQuotedStringContent(_wrappedCommand),
                    _wrappedCommandType,
                    commandOrigin
                    );
            }
            else
            {
                result = string.Format(CultureInfo.InvariantCulture, @"
    try {{
        $wrappedCmd = $ExecutionContext.InvokeCommand.GetCommand('{0}', [System.Management.Automation.CommandTypes]::{1})
        $PSBoundParameters.Add('$args', $args)
        $scriptCmd = {{& $wrappedCmd @PSBoundParameters }}

        $steppablePipeline = $scriptCmd.GetSteppablePipeline({2})
        $steppablePipeline.Begin($myInvocation.ExpectingInput, $ExecutionContext)
    }} catch {{
        throw
    }}
",
                CodeGeneration.EscapeSingleQuotedStringContent(_wrappedCommand),
                _wrappedCommandType,
                commandOrigin
                );
            }

            return result;
        }

        internal string GetProcessBlock()
        {
            // The reason we wrap scripts in 'try { } catch { throw }' (here and elsewhere) is to turn
            // an exception that could be thrown from .NET method invocation into a terminating error
            // that can be propagated up.
            // By default, an exception thrown from .NET method is not terminating, but when enclosed
            // in try/catch, it will be turned into a terminating error.
            return @"
    try {
        $steppablePipeline.Process($_)
    } catch {
        throw
    }
";
        }

        internal string GetDynamicParamBlock()
        {
            return string.Format(CultureInfo.InvariantCulture, @"
    try {{
        $targetCmd = $ExecutionContext.InvokeCommand.GetCommand('{0}', [System.Management.Automation.CommandTypes]::{1}, $PSBoundParameters)
        $dynamicParams = @($targetCmd.Parameters.GetEnumerator() | Microsoft.PowerShell.Core\Where-Object {{ $_.Value.IsDynamic }})
        if ($dynamicParams.Length -gt 0)
        {{
            $paramDictionary = [Management.Automation.RuntimeDefinedParameterDictionary]::new()
            foreach ($param in $dynamicParams)
            {{
                $param = $param.Value

                if(-not $MyInvocation.MyCommand.Parameters.ContainsKey($param.Name))
                {{
                    $dynParam = [Management.Automation.RuntimeDefinedParameter]::new($param.Name, $param.ParameterType, $param.Attributes)
                    $paramDictionary.Add($param.Name, $dynParam)
                }}
            }}

            return $paramDictionary
        }}
    }} catch {{
        throw
    }}
",
            CodeGeneration.EscapeSingleQuotedStringContent(_wrappedCommand),
            _wrappedCommandType);
        }

        internal string GetEndBlock()
        {
            return @"
    try {
        $steppablePipeline.End()
    } catch {
        throw
    }
";
        }

        internal string GetCleanBlock()
        {
            // Here we don't need to enclose the script in a 'try/catch' like elsewhere, because
            //  1. the 'Clean' block doesn't propagate up any exception (terminating error);
            //  2. only one expression in the script, so nothing else needs to be stopped when invoking the method fails.
            return @"
    if ($null -ne $steppablePipeline) {
        $steppablePipeline.Clean()
    }
";
        }

        #endregion

        #region Helper methods for restricting commands needed by implicit and interactive remoting

        internal const string isSafeNameOrIdentifierRegex = @"^[-._:\\\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Lm}]{1,100}$";

        private static CommandMetadata GetRestrictedCmdlet(string cmdletName, string defaultParameterSet, string helpUri, params ParameterMetadata[] parameters)
        {
            Dictionary<string, ParameterMetadata> parametersDictionary = new Dictionary<string, ParameterMetadata>(StringComparer.OrdinalIgnoreCase);
            foreach (ParameterMetadata parameter in parameters)
            {
                parametersDictionary.Add(parameter.Name, parameter);
            }

            // isProxyForCmdlet:
            // 1a. we would want to set it to false to get rid of unused common parameters
            //    (like OutBuffer - see bug Windows 7: #402213)
            // 1b. otoh common parameters are going to be present anyway on all proxy functions
            //     that the host generates for its cmdlets that need cmdletbinding, so
            //     we should make sure that common parameters are safe, not hide them
            // 2. otoh without cmdletbinding() unspecified parameters get bound to $null which might
            //    unnecessarily trigger validation attribute failures - see bug Windows 7: #477218

            CommandMetadata metadata = new CommandMetadata(
                                   name: cmdletName,
                            commandType: CommandTypes.Cmdlet,
                       isProxyForCmdlet: true,
                defaultParameterSetName: defaultParameterSet,
                  supportsShouldProcess: false,
                          confirmImpact: ConfirmImpact.None,
                         supportsPaging: false,
                   supportsTransactions: false,
                      positionalBinding: true,
                             parameters: parametersDictionary);

            metadata.HelpUri = helpUri;

            return metadata;
        }

        private static CommandMetadata GetRestrictedGetCommand()
        {
            // remote Get-Command called by Import/Export-PSSession to get metadata for remote commands that user wants to import

            // remote Get-Command is also called by interactive remoting before entering the remote session to verify
            // that Out-Default and Exit-PSSession commands are present in the remote session

            // value passed directly from Import-PSSession -CommandName to Get-Command -Name
            // can't really restrict beyond basics
            ParameterMetadata nameParameter = new ParameterMetadata("Name", typeof(string[]));
            nameParameter.Attributes.Add(new ValidateLengthAttribute(0, 1000));
            nameParameter.Attributes.Add(new ValidateCountAttribute(0, 1000));

            // value passed directly from Import-PSSession -PSSnapIn to Get-Command -Module
            // can't really restrict beyond basics
            ParameterMetadata moduleParameter = new ParameterMetadata("Module", typeof(string[]));
            moduleParameter.Attributes.Add(new ValidateLengthAttribute(0, 1000));
            moduleParameter.Attributes.Add(new ValidateCountAttribute(0, 100));

            // value passed directly from Import-PSSession -ArgumentList to Get-Command -ArgumentList
            // can't really restrict beyond basics
            ParameterMetadata argumentListParameter = new ParameterMetadata("ArgumentList", typeof(object[]));
            argumentListParameter.Attributes.Add(new ValidateCountAttribute(0, 100));

            // value passed directly from Import-PSSession -CommandType to Get-Command -CommandType
            // can't really restrict beyond basics
            ParameterMetadata commandTypeParameter = new ParameterMetadata("CommandType", typeof(CommandTypes));

            // we do allow -ListImported switch
            ParameterMetadata listImportedParameter = new ParameterMetadata("ListImported", typeof(SwitchParameter));

            // Need to expose ShowCommandInfo parameter for remote ShowCommand support.
            ParameterMetadata showCommandInfo = new ParameterMetadata("ShowCommandInfo", typeof(SwitchParameter));

            return GetRestrictedCmdlet(
                "Get-Command",
                null, // defaultParameterSet
                "https://go.microsoft.com/fwlink/?LinkID=113309", // helpUri
                nameParameter,
                moduleParameter,
                argumentListParameter,
                commandTypeParameter,
                listImportedParameter,
                showCommandInfo);
        }

        private static CommandMetadata GetRestrictedGetFormatData()
        {
            // remote Get-FormatData called by Import/Export-PSSession to get F&O metadata from remote session

            // value passed directly from Import-PSSession -FormatTypeName to Get-FormatData -TypeName
            // can't really restrict beyond basics
            ParameterMetadata typeNameParameter = new ParameterMetadata("TypeName", typeof(string[]));
            typeNameParameter.Attributes.Add(new ValidateLengthAttribute(0, 1000));
            typeNameParameter.Attributes.Add(new ValidateCountAttribute(0, 1000));

            // This parameter is required for implicit remoting in PS V5.1.
            ParameterMetadata powershellVersionParameter = new ParameterMetadata("PowerShellVersion", typeof(Version));

            return GetRestrictedCmdlet("Get-FormatData", null, "https://go.microsoft.com/fwlink/?LinkID=144303", typeNameParameter, powershellVersionParameter);
        }

        private static CommandMetadata GetRestrictedGetHelp()
        {
            // remote Get-Help is called when help for implicit remoting proxy tries to fetch help content for a remote command

            // This should only be called with 1 "safe" command name (unless ipsn is called with -Force)
            // (it seems ok to disallow getting help for "unsafe" commands [possible when ipsn is called with -Force]
            //  - host can always generate its own proxy for Get-Help if it cares about "unsafe" command names)
            ParameterMetadata nameParameter = new ParameterMetadata("Name", typeof(string));
            nameParameter.Attributes.Add(new ValidatePatternAttribute(isSafeNameOrIdentifierRegex));
            nameParameter.Attributes.Add(new ValidateLengthAttribute(0, 1000));

            // This should only be called with 1 valid category
            ParameterMetadata categoryParameter = new ParameterMetadata("Category", typeof(string[]));
            categoryParameter.Attributes.Add(new ValidateSetAttribute(Enum.GetNames<HelpCategory>()));
            categoryParameter.Attributes.Add(new ValidateCountAttribute(0, 1));

            return GetRestrictedCmdlet("Get-Help", null, "https://go.microsoft.com/fwlink/?LinkID=113316", nameParameter, categoryParameter);
        }

        private static CommandMetadata GetRestrictedSelectObject()
        {
            // remote Select-Object is called by Import/Export-PSSession to
            // 1) restrict what properties are serialized
            // 2) artificially increase serialization depth of selected properties (especially "Parameters" property)

            // only called with a fixed set of values
            string[] validPropertyValues = new string[] {
                "ModuleName", "Namespace", "OutputType", "Count", "HelpUri",
                "Name", "CommandType", "ResolvedCommandName", "DefaultParameterSet", "CmdletBinding", "Parameters" };
            ParameterMetadata propertyParameter = new ParameterMetadata("Property", typeof(string[]));
            propertyParameter.Attributes.Add(new ValidateSetAttribute(validPropertyValues));
            propertyParameter.Attributes.Add(new ValidateCountAttribute(1, validPropertyValues.Length));

            // needed for pipeline input if cmdlet binding has to be used (i.e. if Windows 7: #477218 is not fixed)
            ParameterMetadata inputParameter = new ParameterMetadata("InputObject", typeof(object));
            inputParameter.ParameterSets.Add(
                ParameterAttribute.AllParameterSets,
                new ParameterSetMetadata(
                    int.MinValue, // not positional
                    ParameterSetMetadata.ParameterFlags.ValueFromPipeline | ParameterSetMetadata.ParameterFlags.Mandatory,
                    null)); // no help message

            return GetRestrictedCmdlet("Select-Object", null, "https://go.microsoft.com/fwlink/?LinkID=2096716", propertyParameter, inputParameter);
        }

        private static CommandMetadata GetRestrictedMeasureObject()
        {
            // remote Measure-Object is called by Import/Export-PSSession to measure how many objects
            // it is going to receive and to display a nice progress bar

            // needed for pipeline input if cmdlet binding has to be used (i.e. if Windows 7: #477218 is not fixed)
            ParameterMetadata inputParameter = new ParameterMetadata("InputObject", typeof(object));
            inputParameter.ParameterSets.Add(
                ParameterAttribute.AllParameterSets,
                new ParameterSetMetadata(
                    int.MinValue, // not positional
                    ParameterSetMetadata.ParameterFlags.ValueFromPipeline | ParameterSetMetadata.ParameterFlags.Mandatory,
                    null)); // no help message

            return GetRestrictedCmdlet("Measure-Object", null, "https://go.microsoft.com/fwlink/?LinkID=113349", inputParameter);
        }

        private static CommandMetadata GetRestrictedOutDefault()
        {
            // remote Out-Default is called by interactive remoting (without any parameters, only using pipelines to pass data)

            // needed for pipeline input if cmdlet binding has to be used (i.e. if Windows 7: #477218 is not fixed)
            ParameterMetadata inputParameter = new ParameterMetadata("InputObject", typeof(object));
            inputParameter.ParameterSets.Add(
                ParameterAttribute.AllParameterSets,
                new ParameterSetMetadata(
                    int.MinValue, // not positional
                    ParameterSetMetadata.ParameterFlags.ValueFromPipeline | ParameterSetMetadata.ParameterFlags.Mandatory,
                    null)); // no help message

            return GetRestrictedCmdlet("Out-Default", null, "https://go.microsoft.com/fwlink/?LinkID=113362", inputParameter);
        }

        private static CommandMetadata GetRestrictedExitPSSession()
        {
            // remote Exit-PSSession is not called by PowerShell, but is needed so that users
            // can exit an interactive remoting session

            return GetRestrictedCmdlet("Exit-PSSession", null, "https://go.microsoft.com/fwlink/?LinkID=2096787"); // no parameters are used
        }

        /// <summary>
        /// Returns a dictionary from a command name to <see cref="CommandMetadata"/> describing
        /// how that command can be restricted to limit attack surface while still being usable
        /// by features included in <paramref name="sessionCapabilities"/>.
        ///
        /// For example the implicit remoting feature
        /// (included in <see cref="SessionCapabilities.RemoteServer"/>)
        /// doesn't use all parameters of Get-Help
        /// and uses only a limited set of argument values for the parameters it does use.
        /// <see cref="CommandMetadata"/> can be passed to <see cref="ProxyCommand.Create(CommandMetadata)"/> method to generate
        /// a body of a proxy function that forwards calls to the actual cmdlet, while exposing only the parameters
        /// listed in <see cref="CommandMetadata"/>.  Exposing only the restricted proxy function while making
        /// the actual cmdlet and its aliases private can help in reducing attack surface of the remoting server.
        /// </summary>
        /// <returns></returns>
        /// <seealso cref="System.Management.Automation.Runspaces.InitialSessionState.CreateRestricted(SessionCapabilities)"/>
        public static Dictionary<string, CommandMetadata> GetRestrictedCommands(SessionCapabilities sessionCapabilities)
        {
            List<CommandMetadata> restrictedCommands = new List<CommandMetadata>();

            // all remoting cmdlets need to be included for workflow scenarios as wel
            if ((sessionCapabilities & SessionCapabilities.RemoteServer) == SessionCapabilities.RemoteServer)
            {
                restrictedCommands.AddRange(GetRestrictedRemotingCommands());
            }

            Dictionary<string, CommandMetadata> result = new Dictionary<string, CommandMetadata>(StringComparer.OrdinalIgnoreCase);
            foreach (CommandMetadata restrictedCommand in restrictedCommands)
            {
                result.Add(restrictedCommand.Name, restrictedCommand);
            }

            return result;
        }

        private static Collection<CommandMetadata> GetRestrictedRemotingCommands()
        {
            Collection<CommandMetadata> remotingCommands = new Collection<CommandMetadata>
                                                           {
                                                               GetRestrictedGetCommand(),
                                                               GetRestrictedGetFormatData(),
                                                               GetRestrictedSelectObject(),
                                                               GetRestrictedGetHelp(),
                                                               GetRestrictedMeasureObject(),
                                                               GetRestrictedExitPSSession(),
                                                               GetRestrictedOutDefault()
                                                           };

            return remotingCommands;
        }

#if !CORECLR    // Not referenced on CSS
        private static Collection<CommandMetadata> GetRestrictedJobCommands()
        {
            // all the job cmdlets take a Name parameter. This needs to be
            // restricted to safenames in order to allow only valid wildcards

            // construct the parameterset metadata
            ParameterSetMetadata nameParameterSet = new ParameterSetMetadata(0,
                                                                             ParameterSetMetadata.ParameterFlags.ValueFromPipelineByPropertyName,
                                                                             string.Empty);
            ParameterSetMetadata instanceIdParameterSet = new ParameterSetMetadata(0,
                                                                                   ParameterSetMetadata.ParameterFlags.ValueFromPipelineByPropertyName,
                                                                                   string.Empty);
            ParameterSetMetadata idParameterSet = new ParameterSetMetadata(0,
                                                                           ParameterSetMetadata.ParameterFlags.ValueFromPipelineByPropertyName,
                                                                           string.Empty);
            ParameterSetMetadata stateParameterSet = new ParameterSetMetadata(int.MinValue,
                                                                              ParameterSetMetadata.ParameterFlags.ValueFromPipelineByPropertyName,
                                                                              string.Empty);
            ParameterSetMetadata commandParameterSet = new ParameterSetMetadata(int.MinValue,
                                                                                ParameterSetMetadata.ParameterFlags.ValueFromPipelineByPropertyName,
                                                                                string.Empty);
            ParameterSetMetadata filterParameterSet = new ParameterSetMetadata(0,
                                                                                ParameterSetMetadata.ParameterFlags.ValueFromPipelineByPropertyName,
                                                                                string.Empty);
            ParameterSetMetadata jobParameterSet = new ParameterSetMetadata(0,
                                                                            ParameterSetMetadata.ParameterFlags.ValueFromPipelineByPropertyName |
                                                                            ParameterSetMetadata.ParameterFlags.ValueFromPipeline |
                                                                            ParameterSetMetadata.ParameterFlags.Mandatory, string.Empty);
            ParameterSetMetadata computerNameParameterSet = new ParameterSetMetadata(0,
                                                                                     ParameterSetMetadata.ParameterFlags.Mandatory |
                                                                                     ParameterSetMetadata.ParameterFlags.ValueFromPipeline |
                                                                                     ParameterSetMetadata.ParameterFlags.ValueFromPipelineByPropertyName,
                                                                                     string.Empty);
            ParameterSetMetadata locationParameterSet = new ParameterSetMetadata(0,
                                                                                 ParameterSetMetadata.ParameterFlags.Mandatory |
                                                                                 ParameterSetMetadata.ParameterFlags.ValueFromPipeline |
                                                                                 ParameterSetMetadata.ParameterFlags.ValueFromPipelineByPropertyName,
                                                                                 string.Empty);

            Dictionary<string, ParameterSetMetadata> parameterSets = new Dictionary<string, ParameterSetMetadata>();
            parameterSets.Add(JobCmdletBase.NameParameterSet, nameParameterSet);

            Collection<string> emptyCollection = new Collection<string>();

            ParameterMetadata nameParameter = new ParameterMetadata(emptyCollection, false, JobCmdletBase.NameParameter,
                                                                    parameterSets, typeof(string[]));
            nameParameter.Attributes.Add(new ValidatePatternAttribute(isSafeNameOrIdentifierRegex));
            nameParameter.Attributes.Add(new ValidateLengthAttribute(0, 1000));

            // all the other parameters can be safely allowed
            parameterSets = new Dictionary<string, ParameterSetMetadata>();
            parameterSets.Add(JobCmdletBase.InstanceIdParameterSet, instanceIdParameterSet);
            ParameterMetadata instanceIdParameter = new ParameterMetadata(emptyCollection, false,
                                                                          JobCmdletBase.InstanceIdParameter,
                                                                          parameterSets, typeof(Guid[]));
            instanceIdParameter.Attributes.Add(new ValidateNotNullOrEmptyAttribute());

            parameterSets = new Dictionary<string, ParameterSetMetadata>();
            parameterSets.Add(JobCmdletBase.SessionIdParameterSet, idParameterSet);
            ParameterMetadata idParameter = new ParameterMetadata(emptyCollection, false, "Id", parameterSets, typeof(int[]));
            idParameter.Attributes.Add(new ValidateNotNullOrEmptyAttribute());

            parameterSets = new Dictionary<string, ParameterSetMetadata>();
            parameterSets.Add(JobCmdletBase.StateParameterSet, stateParameterSet);
            ParameterMetadata stateParameter = new ParameterMetadata(emptyCollection, false,
                                                                     JobCmdletBase.StateParameter, parameterSets,
                                                                     typeof(JobState));

            parameterSets = new Dictionary<string, ParameterSetMetadata>();
            parameterSets.Add(JobCmdletBase.CommandParameterSet, commandParameterSet);
            ParameterMetadata commandParameter = new ParameterMetadata(emptyCollection, false,
                                                                       JobCmdletBase.CommandParameter, parameterSets,
                                                                       typeof(string[]));

            parameterSets = new Dictionary<string, ParameterSetMetadata>();
            parameterSets.Add(JobCmdletBase.FilterParameterSet, filterParameterSet);
            ParameterMetadata filterParameter = new ParameterMetadata(emptyCollection, false, JobCmdletBase.FilterParameter, parameterSets, typeof(Hashtable));

            parameterSets = new Dictionary<string, ParameterSetMetadata>();
            parameterSets.Add(JobCmdletBase.JobParameter, jobParameterSet);
            ParameterMetadata jobParameter = new ParameterMetadata(emptyCollection, false, JobCmdletBase.JobParameter,
                                                                   parameterSets, typeof(Job[]));
            jobParameter.Attributes.Add(new ValidateNotNullOrEmptyAttribute());

            parameterSets = new Dictionary<string, ParameterSetMetadata>();
            parameterSets.Add("ComputerName", computerNameParameterSet);
            parameterSets.Add("Location", locationParameterSet);
            ParameterMetadata jobParameter2 = new ParameterMetadata(emptyCollection, false, JobCmdletBase.JobParameter,
                                                                    parameterSets, typeof(Job[]));

            // Start-Job is not really required since the user will be using the name
            // of the workflow to launch them
            Collection<CommandMetadata> restrictedJobCommands = new Collection<CommandMetadata>();

            // Stop-Job cmdlet
            ParameterMetadata passThruParameter = new ParameterMetadata("PassThru", typeof(SwitchParameter));
            ParameterMetadata anyParameter = new ParameterMetadata("Any", typeof(SwitchParameter));

            CommandMetadata stopJob = GetRestrictedCmdlet("Stop-Job", JobCmdletBase.SessionIdParameterSet, "https://go.microsoft.com/fwlink/?LinkID=2096795", nameParameter,
                                                          instanceIdParameter, idParameter,
                                                          stateParameter, filterParameter, jobParameter, passThruParameter);
            restrictedJobCommands.Add(stopJob);

            // Wait-Job cmdlet
            ParameterMetadata timeoutParameter = new ParameterMetadata("Timeout", typeof(int));
            timeoutParameter.Attributes.Add(new ValidateRangeAttribute(-1, Int32.MaxValue));

            CommandMetadata waitJob = GetRestrictedCmdlet("Wait-Job", JobCmdletBase.SessionIdParameterSet, "https://go.microsoft.com/fwlink/?LinkID=2096902", nameParameter,
                                                          instanceIdParameter, idParameter,
                                                          jobParameter, stateParameter, filterParameter, anyParameter, timeoutParameter);
            restrictedJobCommands.Add(waitJob);

            // Get-Job cmdlet
            CommandMetadata getJob = GetRestrictedCmdlet("Get-Job", JobCmdletBase.SessionIdParameterSet, "https://go.microsoft.com/fwlink/?LinkID=113328", nameParameter,
                                                         instanceIdParameter, idParameter,
                                                         stateParameter, filterParameter, commandParameter);
            restrictedJobCommands.Add(getJob);

            // Receive-Job cmdlet
            parameterSets = new Dictionary<string, ParameterSetMetadata>();
            computerNameParameterSet = new ParameterSetMetadata(1,
                                                                ParameterSetMetadata.ParameterFlags.ValueFromPipelineByPropertyName,
                                                                string.Empty);

            parameterSets.Add("ComputerName", computerNameParameterSet);
            ParameterMetadata computerNameParameter = new ParameterMetadata(emptyCollection, false, "ComputerName", parameterSets, typeof(string[]));
            computerNameParameter.Attributes.Add(new ValidateLengthAttribute(0, 1000));
            computerNameParameter.Attributes.Add(new ValidateNotNullOrEmptyAttribute());

            parameterSets = new Dictionary<string, ParameterSetMetadata>();
            locationParameterSet = new ParameterSetMetadata(1,
                                                            ParameterSetMetadata.ParameterFlags.ValueFromPipelineByPropertyName,
                                                            string.Empty);

            parameterSets.Add("Location", locationParameterSet);
            ParameterMetadata locationParameter = new ParameterMetadata(emptyCollection, false, "Location", parameterSets, typeof(string[]));
            locationParameter.Attributes.Add(new ValidateLengthAttribute(0, 1000));
            locationParameter.Attributes.Add(new ValidateNotNullOrEmptyAttribute());

            ParameterMetadata norecurseParameter = new ParameterMetadata("NoRecurse", typeof(SwitchParameter));
            ParameterMetadata keepParameter = new ParameterMetadata("Keep", typeof(SwitchParameter));
            ParameterMetadata waitParameter = new ParameterMetadata("Wait", typeof(SwitchParameter));
            ParameterMetadata writeEventsParameter = new ParameterMetadata("WriteEvents", typeof(SwitchParameter));
            ParameterMetadata writeJobParameter = new ParameterMetadata("WriteJobInResults", typeof(SwitchParameter));
            ParameterMetadata autoRemoveParameter = new ParameterMetadata("AutoRemoveJob", typeof(SwitchParameter));

            CommandMetadata receiveJob = GetRestrictedCmdlet("Receive-Job", "Location", "https://go.microsoft.com/fwlink/?LinkID=2096965", nameParameter,
                                                             instanceIdParameter,
                                                             idParameter, stateParameter, jobParameter2,
                                                             computerNameParameter, locationParameter,
                                                             norecurseParameter, keepParameter, waitParameter,
                                                             writeEventsParameter, writeJobParameter, autoRemoveParameter);
            restrictedJobCommands.Add(receiveJob);

            // Remove-Job cmdlet
            ParameterMetadata forceParameter = new ParameterMetadata("Force", typeof(SwitchParameter));

            CommandMetadata removeJob = GetRestrictedCmdlet("Remove-Job", JobCmdletBase.SessionIdParameterSet, "https://go.microsoft.com/fwlink/?LinkID=2096868",
                                                            nameParameter, instanceIdParameter,
                                                            idParameter, stateParameter, filterParameter, jobParameter, forceParameter);

            restrictedJobCommands.Add(removeJob);

            // Suspend-Job cmdlet
            CommandMetadata suspendJob = GetRestrictedCmdlet("Suspend-Job", JobCmdletBase.SessionIdParameterSet, "https://go.microsoft.com/fwlink/?LinkID=210613",
                                                             nameParameter, instanceIdParameter,
                                                             idParameter, stateParameter, filterParameter, jobParameter, passThruParameter);
            restrictedJobCommands.Add(suspendJob);

            // Suspend-Job cmdlet
            CommandMetadata resumeJob = GetRestrictedCmdlet("Resume-Job", JobCmdletBase.SessionIdParameterSet, "https://go.microsoft.com/fwlink/?LinkID=210611",
                                                             nameParameter, instanceIdParameter,
                                                             idParameter, stateParameter, filterParameter, jobParameter, passThruParameter);
            restrictedJobCommands.Add(resumeJob);

            return restrictedJobCommands;
        }
#endif

        #endregion

        #region Command Metadata cache
        /// <summary>
        /// The command metadata cache. This is separate from the parameterMetadata cache
        /// because it is specific to cmdlets.
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, CommandMetadata> s_commandMetadataCache =
            new System.Collections.Concurrent.ConcurrentDictionary<string, CommandMetadata>(StringComparer.OrdinalIgnoreCase);

        #endregion
    }
}
