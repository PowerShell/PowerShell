// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    using PowerShell = System.Management.Automation.PowerShell;

    /// <summary>
    /// This class implements Export-PSSession cmdlet.
    /// Spec: TBD.
    /// </summary>
    [Cmdlet(VerbsData.Export, "PSSession", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096604")]
    [OutputType(typeof(FileInfo))]
    public sealed class ExportPSSessionCommand : ImplicitRemotingCommandBase
    {
        /// <summary>
        /// Version of the script generator used (by this Export-PSSession cmdlet) to generate psm1 and psd1 files.
        /// Generated script checks this version to see if it needs to be regenerated.  There are 2 situations where this is needed
        /// 1. the script needs to be regenerated because a bug fix made previous versions incompatible with the rest of the system (i.e. with ObjectModelWrapper).
        /// 2. ths script needs to be regenerated because a security vulnerability was found inside generated code (there is no way to service generated code, but we can service the dll that reports the version that the generated script checks against).
        /// </summary>
        public static Version VersionOfScriptGenerator { get { return ImplicitRemotingCodeGenerator.VersionOfScriptWriter; } }

        #region Parameters

        /// <summary>
        /// Mandatory file name to write to.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        [Alias("PSPath", "ModuleName")]
        public string OutputModule { get; set; }

        /// <summary>
        /// Property that sets force parameter.
        /// </summary>
        [Parameter]
        public SwitchParameter Force
        {
            get
            {
                return new SwitchParameter(_force);
            }

            set
            {
                _force = value.IsPresent;
            }
        }

        private bool _force;

        /// <summary>
        /// Encoding optional flag.
        /// </summary>
        [Parameter]
        [ArgumentToEncodingTransformationAttribute()]
        [ArgumentEncodingCompletionsAttribute]
        [ValidateNotNullOrEmpty]
        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }

            set
            {
                EncodingConversion.WarnIfObsolete(this, value);
                _encoding = value;
            }
        }

        private Encoding _encoding = Encoding.Default;

        #endregion Parameters

        #region Implementation

        private const string getChildItemScript = @"
                param($path)
                Get-ChildItem -LiteralPath $path
            ";

        private const string copyItemScript = @"
                param($sourcePath, $destinationPath)
                Copy-Item -Recurse $sourcePath\\* -Destination $destinationPath\\
                Remove-item $sourcePath -Recurse -Force
            ";

        private void DisplayDirectory(List<string> generatedFiles)
        {
            ScriptBlock script = this.Context.Engine.ParseScriptBlock(getChildItemScript, false);
            Collection<PSObject> results = script.Invoke(new object[] { generatedFiles.ToArray() });
            foreach (PSObject o in results)
            {
                this.WriteObject(o);
            }
        }

        /// <summary>
        /// Performs initialization of cmdlet execution.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Module and FullyQualifiedModule should not be specified at the same time.
            // Throw out terminating error if this is the case.
            if (IsModuleSpecified && IsFullyQualifiedModuleSpecified)
            {
                string errMsg = StringUtil.Format(SessionStateStrings.GetContent_TailAndHeadCannotCoexist, nameof(Module), nameof(FullyQualifiedModule));
                ErrorRecord error = new(new InvalidOperationException(errMsg), "ModuleAndFullyQualifiedModuleCannotBeSpecifiedTogether", ErrorCategory.InvalidOperation, null);
                ThrowTerminatingError(error);
            }

            DirectoryInfo directory = PathUtils.CreateModuleDirectory(this, this.OutputModule, this.Force.IsPresent);

            // Creating a temporary directory where files will be created.
            // Then, copy the files from this location to the location specified in OutputModule
            // We are doing this since temporary locations in user directory are more secure.
            DirectoryInfo tempDirectory = PathUtils.CreateTemporaryDirectory();

            Dictionary<string, string> alias2resolvedCommandName;
            List<CommandMetadata> listOfCommandMetadata = this.GetRemoteCommandMetadata(out alias2resolvedCommandName);
            List<ExtendedTypeDefinition> listOfFormatData = this.GetRemoteFormatData();

            List<string> generatedFiles = GenerateProxyModule(
                tempDirectory,
                Path.GetFileName(directory.FullName),
                Encoding,
                _force,
                listOfCommandMetadata,
                alias2resolvedCommandName,
                listOfFormatData
                );

            ScriptBlock script = this.Context.Engine.ParseScriptBlock(copyItemScript, false);
            script.Invoke(new object[] { tempDirectory, directory });

            this.DisplayDirectory(new List<string> { directory.FullName });
        }

        #endregion Methods
    }

    /// <summary>
    /// This class implements Import-PSSession cmdlet.
    /// Spec: http://cmdletdesigner/SpecViewer/Default.aspx?Project=PowerShell&amp;Cmdlet=Import-Command .
    /// </summary>
    [Cmdlet(VerbsData.Import, "PSSession", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096712")]
    [OutputType(typeof(PSModuleInfo))]
    public sealed class ImportPSSessionCommand : ImplicitRemotingCommandBase
    {
        #region Hooking runspace closed event into module cleanup

        private const string runspaceStateChangedScript = @"& {
            if ('Closed' -eq $eventArgs.RunspaceStateInfo.State)
            {
                $sourceIdentifier = [system.management.automation.wildcardpattern]::Escape($eventSubscriber.SourceIdentifier)
                Unregister-Event -SourceIdentifier $sourceIdentifier -Force -ErrorAction SilentlyContinue

                $moduleInfo = $event.MessageData
                Remove-Module -ModuleInfo $moduleInfo -Force -ErrorAction SilentlyContinue

                Remove-Item -LiteralPath $moduleInfo.ModuleBase -Recurse -Force -ErrorAction SilentlyContinue
                $moduleInfo = $null
            }
}
            ";

        private const string unregisterEventCleanUpScript = @"
            $sourceIdentifier = [system.management.automation.wildcardpattern]::Escape($eventSubscriber.SourceIdentifier)
            Unregister-Event -SourceIdentifier $sourceIdentifier -Force -ErrorAction SilentlyContinue

            if ($null -ne $previousScript)
            {
                & $previousScript $args
            }
            ";

        private void RegisterModuleCleanUp(PSModuleInfo moduleInfo)
        {
            if (moduleInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(moduleInfo));
            }

            // Note: we are using this.Context.Events to make sure that the event handler
            //       is executing on the pipeline thread (for thread-safety)

            string sourceIdentifier = StringUtil.Format(ImplicitRemotingStrings.EventSourceIdentifier, this.Session.InstanceId, this.ModuleGuid);
            PSEventSubscriber eventSubscriber = this.Context.Events.SubscribeEvent(
                this.Session.Runspace,
                "StateChanged",
                sourceIdentifier,
                PSObject.AsPSObject(moduleInfo),
                this.Context.Engine.ParseScriptBlock(runspaceStateChangedScript, false),
                true, false);

            //
            // hook into moduleInfo.OnRemove to remove the handler when the module goes away
            //

            ScriptBlock newScript = this.Context.Engine.ParseScriptBlock(unregisterEventCleanUpScript, false);
            newScript = newScript.GetNewClosure(); // create a separate scope for variables set below
            newScript.Module.SessionState.PSVariable.Set("eventSubscriber", eventSubscriber);
            newScript.Module.SessionState.PSVariable.Set("previousScript", moduleInfo.OnRemove);

            moduleInfo.OnRemove = newScript;
        }

        #endregion

        #region Creating and importing the module

        private const string importModuleScript = @"
                param($name, $session, $prefix, $disableNameChecking)
                Import-Module -Name $name -Alias * -Function * -Prefix $prefix -DisableNameChecking:$disableNameChecking -PassThru -ArgumentList @($session)
            ";

        private PSModuleInfo CreateModule(string manifestFile)
        {
            ScriptBlock script = this.Context.Engine.ParseScriptBlock(importModuleScript, false);
            Collection<PSObject> results = script.Invoke(manifestFile, this.Session, this.Prefix, _disableNameChecking);
            Dbg.Assert(results != null, "Import-Module should always succeed");
            Dbg.Assert(results.Count == 1, "Import-Module should always succeed");
            Dbg.Assert(results[0].BaseObject is PSModuleInfo, "Import-Module should always succeed");
            return (PSModuleInfo)(results[0].BaseObject);
        }

        #endregion

        #region Extra parameters

        /// <summary>
        /// This parameter specified a prefix used to modify names of imported commands.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public new string Prefix
        {
            get { return base.Prefix; }

            set { base.Prefix = value; }
        }

        /// <summary>
        /// Disable warnings on cmdlet and function names that have non-standard verbs
        /// or non-standard characters in the noun.
        /// Also disable security related checks against command and parameter names.
        /// </summary>
        [Parameter]
        public SwitchParameter DisableNameChecking
        {
            get { return _disableNameChecking; }

            set { _disableNameChecking = value; }
        }

        private bool _disableNameChecking;

        #endregion

        /// <summary>
        /// Performs initialization of cmdlet execution.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Module and FullyQualifiedModule should not be specified at the same time.
            // Throw out terminating error if this is the case.
            if (IsModuleSpecified && IsFullyQualifiedModuleSpecified)
            {
                string errMsg = StringUtil.Format(SessionStateStrings.GetContent_TailAndHeadCannotCoexist, nameof(Module), nameof(FullyQualifiedModule));
                ErrorRecord error = new(new InvalidOperationException(errMsg), "ModuleAndFullyQualifiedModuleCannotBeSpecifiedTogether", ErrorCategory.InvalidOperation, null);
                ThrowTerminatingError(error);
            }

            DirectoryInfo moduleDirectory = PathUtils.CreateTemporaryDirectory();

            Dictionary<string, string> alias2resolvedCommandName;
            List<CommandMetadata> listOfCommandMetadata = this.GetRemoteCommandMetadata(out alias2resolvedCommandName);
            List<ExtendedTypeDefinition> listOfFormatData = this.GetRemoteFormatData();

            List<string> generatedFiles = this.GenerateProxyModule(
                moduleDirectory,
                Path.GetFileName(moduleDirectory.FullName),
                Encoding.Unicode,
                false,
                listOfCommandMetadata,
                alias2resolvedCommandName,
                listOfFormatData);

            string manifestFile = null;
            foreach (string file in generatedFiles)
            {
                if (Path.GetExtension(file).Equals(".psd1", StringComparison.OrdinalIgnoreCase))
                {
                    manifestFile = file;
                }
            }

            Dbg.Assert(manifestFile != null, "A psd1 file should always be generated");

            PSModuleInfo moduleInfo = this.CreateModule(manifestFile);
            this.RegisterModuleCleanUp(moduleInfo);

            this.WriteObject(moduleInfo);
        }
    }

    /// <summary>
    /// Base class for implicit remoting cmdlets.
    /// </summary>
    public class ImplicitRemotingCommandBase : PSCmdlet
    {
        internal const string ImplicitRemotingKey = "ImplicitRemoting";
        internal const string ImplicitRemotingHashKey = "Hash";
        internal const string ImplicitRemotingCommandsToSkipKey = "CommandsToSkip";

        #region Constructor

        internal ImplicitRemotingCommandBase()
        {
            this.CommandName = new string[] { "*" };
            _commandParameterSpecified = false;

            this.FormatTypeName = new string[] { "*" };
            _formatTypeNamesSpecified = false;
        }

        #endregion

        #region Common cmdlet parameters

        #region related to Get-Command

        /// <summary>
        /// Gets or sets the path(s) or name(s) of the commands to retrieve.
        /// </summary>
        [Parameter(Position = 2)]
        [Alias("Name")]
        public string[] CommandName
        {
            get
            {
                return _commandNameParameter;
            }

            set
            {
                _commandNameParameter = value;
                _commandParameterSpecified = true;
                _commandNamePatterns = SessionStateUtilities.CreateWildcardsFromStrings(
                    _commandNameParameter,
                    WildcardOptions.CultureInvariant | WildcardOptions.IgnoreCase);
            }
        }

        private string[] _commandNameParameter;
        private Collection<WildcardPattern> _commandNamePatterns; // initialized to default value in the constructor

        /// <summary>
        /// Allows shadowing and/or overwriting of existing local/client commands.
        /// </summary>
        [Parameter]
        public SwitchParameter AllowClobber { get; set; } = new SwitchParameter(false);

        /// <summary>
        /// The parameter that all additional arguments get bound to. These arguments are used
        /// when retrieving dynamic parameters from cmdlets that support them.
        /// </summary>
        [Parameter]
        [AllowNull]
        [AllowEmptyCollection]
        [Alias("Args")]
        public object[] ArgumentList
        {
            get
            {
                return _commandArgs;
            }

            set
            {
                _commandArgs = value;
                _commandParameterSpecified = true;
            }
        }

        private object[] _commandArgs;

        /// <summary>
        /// Gets or sets the type of the command to get.
        /// </summary>
        [Parameter]
        [Alias("Type")]
        public CommandTypes CommandType
        {
            get
            {
                return _commandType;
            }

            set
            {
                _commandType = value;
                _commandParameterSpecified = true;
            }
        }

        private CommandTypes _commandType = CommandTypes.All & (~(CommandTypes.Application | CommandTypes.Script | CommandTypes.ExternalScript));

        /// <summary>
        /// Gets or sets the PSSnapin parameter to the cmdlet.
        /// </summary>
        [Parameter]
        [Alias("PSSnapin")]
        [ValidateNotNull]
        public string[] Module
        {
            get
            {
                return _PSSnapins;
            }

            set
            {
                value ??= Array.Empty<string>();

                _PSSnapins = value;
                _commandParameterSpecified = true;
                IsModuleSpecified = true;
            }
        }

        private string[] _PSSnapins = Array.Empty<string>();
        internal bool IsModuleSpecified = false;
        /// <summary>
        /// Gets or sets the FullyQualifiedModule parameter to the cmdlet.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public ModuleSpecification[] FullyQualifiedModule
        {
            get
            {
                return _moduleSpecifications;
            }

            set
            {
                if (value != null)
                {
                    _moduleSpecifications = value;
                }

                _commandParameterSpecified = true;
                IsFullyQualifiedModuleSpecified = true;
            }
        }

        private ModuleSpecification[] _moduleSpecifications = Array.Empty<ModuleSpecification>();
        internal bool IsFullyQualifiedModuleSpecified = false;

        private bool _commandParameterSpecified; // initialized to default value in the constructor

        #endregion related to Get-Command

        #region related to F&O

        /// <summary>
        /// Gets or sets the types for which we should get formatting and output data.
        /// </summary>
        [Parameter(Position = 3)]
        public string[] FormatTypeName
        {
            get
            {
                return _formatTypeNameParameter;
            }

            set
            {
                _formatTypeNameParameter = value;
                _formatTypeNamesSpecified = true;
                _formatTypeNamePatterns = SessionStateUtilities.CreateWildcardsFromStrings(
                    _formatTypeNameParameter,
                    WildcardOptions.CultureInvariant | WildcardOptions.IgnoreCase);
            }
        }

        private string[] _formatTypeNameParameter; // initialized to default value in the constructor
        private Collection<WildcardPattern> _formatTypeNamePatterns;
        private bool _formatTypeNamesSpecified; // initialized to default value in the constructor

        #endregion

        #region Related to modules

        /// <summary>
        /// This parameter specified a prefix used to modify names of imported commands.
        /// </summary>
        internal string Prefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the certificate with which to sign the format file and psm1 file.
        /// </summary>
        [Parameter]
        public X509Certificate2 Certificate { get; set; }

        #endregion

        /// <summary>
        /// The PSSession object describing the remote runspace
        /// using which the specified cmdlet operation will be performed.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public PSSession Session { get; set; }

        #endregion Parameters

        #region Localized errors and messages

        internal ErrorDetails GetErrorDetails(string errorId, params object[] args)
        {
            if (string.IsNullOrEmpty(errorId))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(errorId));
            }

            return new ErrorDetails(
                typeof(ImplicitRemotingCommandBase).GetTypeInfo().Assembly,
                "Microsoft.PowerShell.Commands.Utility.resources.ImplicitRemotingStrings",
                errorId,
                args);
        }

        private ErrorRecord GetErrorNoCommandsImportedBecauseOfSkipping()
        {
            const string errorId = "ErrorNoCommandsImportedBecauseOfSkipping";

            ErrorDetails details = this.GetErrorDetails(errorId);

            ErrorRecord errorRecord = new(
                new ArgumentException(details.Message),
                errorId,
                ErrorCategory.InvalidResult,
                null);
            errorRecord.ErrorDetails = details;

            return errorRecord;
        }

        private ErrorRecord GetErrorMalformedDataFromRemoteCommand(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandName));
            }

            const string errorId = "ErrorMalformedDataFromRemoteCommand";

            ErrorDetails details = this.GetErrorDetails(errorId, commandName);

            ErrorRecord errorRecord = new(
                new ArgumentException(details.Message),
                errorId,
                ErrorCategory.InvalidResult,
                null);
            errorRecord.ErrorDetails = details;

            return errorRecord;
        }

        private ErrorRecord GetErrorCommandSkippedBecauseOfShadowing(string commandNames)
        {
            if (string.IsNullOrEmpty(commandNames))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandNames));
            }

            const string errorId = "ErrorCommandSkippedBecauseOfShadowing";

            ErrorDetails details = this.GetErrorDetails(errorId, commandNames);

            ErrorRecord errorRecord = new(
                new InvalidOperationException(details.Message),
                errorId,
                ErrorCategory.InvalidData,
                null);
            errorRecord.ErrorDetails = details;

            return errorRecord;
        }

        private ErrorRecord GetErrorSkippedNonRequestedCommand(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandName));
            }

            const string errorId = "ErrorSkippedNonRequestedCommand";

            ErrorDetails details = this.GetErrorDetails(errorId, commandName);

            ErrorRecord errorRecord = new(
                new InvalidOperationException(details.Message),
                errorId,
                ErrorCategory.ResourceExists,
                null);
            errorRecord.ErrorDetails = details;

            return errorRecord;
        }

        private ErrorRecord GetErrorSkippedNonRequestedTypeDefinition(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(typeName));
            }

            const string errorId = "ErrorSkippedNonRequestedTypeDefinition";

            ErrorDetails details = this.GetErrorDetails(errorId, typeName);

            ErrorRecord errorRecord = new(
                new InvalidOperationException(details.Message),
                errorId,
                ErrorCategory.ResourceExists,
                null);
            errorRecord.ErrorDetails = details;

            return errorRecord;
        }

        private ErrorRecord GetErrorSkippedUnsafeCommandName(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandName));
            }

            const string errorId = "ErrorSkippedUnsafeCommandName";

            ErrorDetails details = this.GetErrorDetails(errorId, commandName);

            ErrorRecord errorRecord = new(
                new InvalidOperationException(details.Message),
                errorId,
                ErrorCategory.InvalidData,
                null);
            errorRecord.ErrorDetails = details;

            return errorRecord;
        }

        private ErrorRecord GetErrorSkippedUnsafeNameInMetadata(string commandName, string nameType, string name)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandName));
            }

            if (string.IsNullOrEmpty(nameType))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(nameType));
            }

            Dbg.Assert(nameType.Equals("Alias") || nameType.Equals("ParameterSet") || nameType.Equals("Parameter"), "nameType matches resource names");
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            string errorId = "ErrorSkippedUnsafe" + nameType + "Name";

            ErrorDetails details = this.GetErrorDetails(errorId, commandName, name);

            ErrorRecord errorRecord = new(
                new InvalidOperationException(details.Message),
                errorId,
                ErrorCategory.InvalidData,
                null);
            errorRecord.ErrorDetails = details;

            return errorRecord;
        }

        private ErrorRecord GetErrorFromRemoteCommand(string commandName, RuntimeException runtimeException)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandName));
            }

            if (runtimeException == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(runtimeException));
            }

            string errorId;
            ErrorDetails errorDetails;
            ErrorRecord errorRecord;

            //
            // handle recognized types of exceptions first
            //
            RemoteException remoteException = runtimeException as RemoteException;
            if ((remoteException != null) && (remoteException.SerializedRemoteException != null))
            {
                if (Deserializer.IsInstanceOfType(remoteException.SerializedRemoteException, typeof(CommandNotFoundException)))
                {
                    errorId = "ErrorRequiredRemoteCommandNotFound";
                    errorDetails = this.GetErrorDetails(errorId, this.MyInvocation.MyCommand.Name);

                    errorRecord = new ErrorRecord(
                        new RuntimeException(errorDetails.Message, runtimeException),
                        errorId,
                        ErrorCategory.ObjectNotFound,
                        null);
                    errorRecord.ErrorDetails = errorDetails;

                    return errorRecord;
                }
            }

            //
            // output a generic error message if exception is not recognized
            //
            errorId = "ErrorFromRemoteCommand";
            errorDetails = this.GetErrorDetails(errorId, commandName, runtimeException.Message);

            errorRecord = new ErrorRecord(
                new RuntimeException(errorDetails.Message, runtimeException),
                errorId,
                ErrorCategory.InvalidResult,
                null);
            errorRecord.ErrorDetails = errorDetails;

            return errorRecord;
        }

        private ErrorRecord GetErrorCouldntResolvedAlias(string aliasName)
        {
            if (string.IsNullOrEmpty(aliasName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(aliasName));
            }

            const string errorId = "ErrorCouldntResolveAlias";

            ErrorDetails details = this.GetErrorDetails(errorId, aliasName);

            ErrorRecord errorRecord = new(
                new ArgumentException(details.Message),
                errorId,
                ErrorCategory.OperationTimeout,
                null);
            errorRecord.ErrorDetails = details;

            return errorRecord;
        }

        private ErrorRecord GetErrorNoResultsFromRemoteEnd(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandName));
            }

            const string errorId = "ErrorNoResultsFromRemoteEnd";

            ErrorDetails details = this.GetErrorDetails(errorId, commandName);

            ErrorRecord errorRecord = new(
                new ArgumentException(details.Message),
                errorId,
                ErrorCategory.InvalidResult,
                null);
            errorRecord.ErrorDetails = details;

            return errorRecord;
        }

        private readonly List<string> _commandsSkippedBecauseOfShadowing = new();

        private void ReportSkippedCommands()
        {
            if (_commandsSkippedBecauseOfShadowing.Count != 0)
            {
                string skippedCommands = string.Join(", ", _commandsSkippedBecauseOfShadowing.ToArray());
                ErrorRecord errorRecord = this.GetErrorCommandSkippedBecauseOfShadowing(skippedCommands);
                this.WriteWarning(errorRecord.ErrorDetails.Message);
            }
        }

        #endregion

        #region Logic to avoid commands we don't want to shadow

        private bool IsCommandNameMatchingParameters(string commandName)
        {
            if (SessionStateUtilities.MatchesAnyWildcardPattern(commandName, _commandNamePatterns, false))
            {
                return true;
            }

            string nameWithoutExtension = Path.GetFileNameWithoutExtension(commandName);
            if (!nameWithoutExtension.Equals(commandName, StringComparison.OrdinalIgnoreCase))
            {
                return SessionStateUtilities.MatchesAnyWildcardPattern(nameWithoutExtension, _commandNamePatterns, false);
            }

            return false;
        }

        private Dictionary<string, object> _existingCommands;

        private Dictionary<string, object> ExistingCommands
        {
            get
            {
                if (_existingCommands == null)
                {
                    _existingCommands = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    CommandSearcher searcher = new(
                        "*",
                        SearchResolutionOptions.CommandNameIsPattern | SearchResolutionOptions.ResolveAliasPatterns | SearchResolutionOptions.ResolveFunctionPatterns,
                        CommandTypes.All,
                        this.Context);
                    foreach (CommandInfo commandInfo in searcher)
                    {
                        _existingCommands[commandInfo.Name] = null;
                    }
                }

                return _existingCommands;
            }
        }

        private bool IsShadowingExistingCommands(string commandName)
        {
            commandName = ModuleCmdletBase.AddPrefixToCommandName(commandName, this.Prefix);

            CommandSearcher searcher = new(commandName, SearchResolutionOptions.None, CommandTypes.All, this.Context);
            foreach (string expandedCommandName in searcher.ConstructSearchPatternsFromName(commandName))
            {
                if (this.ExistingCommands.ContainsKey(expandedCommandName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if command doesn't shadow OR is in the -AllowShadowing parameter.
        /// </summary>
        /// <param name="commandName"></param>
        /// <returns></returns>
        private bool IsCommandNameAllowedForImport(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandName));
            }

            if (this.AllowClobber.IsPresent)
            {
                return true;
            }

            if (IsShadowingExistingCommands(commandName))
            {
                _commandsSkippedBecauseOfShadowing.Add(commandName);
                return false;
            }
            else
            {
                return true;
            }
        }

        #endregion

        #region Logic to skip commands the server doesn't want to import

        private List<string> CommandSkipListFromServer
        {
            get
            {
                // try to get the list from server's application private data
                if (_commandSkipListFromServer == null)
                {
                    string[] serverDeclaredListOfCommandsToSkip;
                    if (PSPrimitiveDictionary.TryPathGet(
                            this.Session.ApplicationPrivateData,
                            out serverDeclaredListOfCommandsToSkip,
                            ImplicitRemotingKey,
                            ImplicitRemotingCommandsToSkipKey))
                    {
                        _commandSkipListFromServer = new List<string>();
                        if (serverDeclaredListOfCommandsToSkip != null)
                        {
                            _commandSkipListFromServer.AddRange(serverDeclaredListOfCommandsToSkip);
                        }
                    }
                }

                // fallback to the default list that hardcodes ...
                if (_commandSkipListFromServer == null)
                {
                    _commandSkipListFromServer = new List<string>();

                    // ... A) 5 commands used (some required some not) by implicit remoting
                    _commandSkipListFromServer.Add("Get-Command");
                    _commandSkipListFromServer.Add("Get-FormatData");
                    _commandSkipListFromServer.Add("Get-Help");
                    _commandSkipListFromServer.Add("Select-Object");
                    _commandSkipListFromServer.Add("Measure-Object");

                    // ... B) 2 commands required for 1:1 remoting
                    _commandSkipListFromServer.Add("Exit-PSSession");
                    _commandSkipListFromServer.Add("Out-Default");
                }

                return _commandSkipListFromServer;
            }
        }

        private List<string> _commandSkipListFromServer;

        private bool IsCommandSkippedByServerDeclaration(string commandName)
        {
            // skipped = commandName is on the CommandSkipListFromServer and is not on the this.CommandName list
            foreach (string commandToSkip in this.CommandSkipListFromServer)
            {
                if (commandToSkip.Equals(commandName, StringComparison.OrdinalIgnoreCase))
                {
                    if (this.CommandName != null)
                    {
                        foreach (string commandNameParameter in this.CommandName)
                        {
                            if (commandName.Equals(commandNameParameter, StringComparison.OrdinalIgnoreCase))
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Generic rehydration helpers

        private T ConvertTo<T>(string commandName, object value)
        {
            return ConvertTo<T>(commandName, value, false);
        }

        private T ConvertTo<T>(string commandName, object value, bool nullOk)
        {
            if (value == null)
            {
                if (nullOk)
                {
                    return default(T);
                }
                else
                {
                    this.ThrowTerminatingError(this.GetErrorMalformedDataFromRemoteCommand(commandName));
                }
            }

            T t;
            if (!LanguagePrimitives.TryConvertTo<T>(value, out t))
            {
                this.ThrowTerminatingError(this.GetErrorMalformedDataFromRemoteCommand(commandName));
            }

            return t;
        }

        private T GetPropertyValue<T>(string commandName, PSObject pso, string propertyName)
        {
            return GetPropertyValue<T>(commandName, pso, propertyName, false);
        }

        private T GetPropertyValue<T>(string commandName, PSObject pso, string propertyName, bool nullOk)
        {
            PSPropertyInfo property = pso.Properties[propertyName];
            if (property == null)
            {
                this.ThrowTerminatingError(this.GetErrorMalformedDataFromRemoteCommand(commandName));
            }

            return ConvertTo<T>(commandName, property.Value, nullOk);
        }

        private List<T> RehydrateList<T>(string commandName, PSObject deserializedObject, string propertyName, Func<PSObject, T> itemRehydrator)
        {
            Dbg.Assert(deserializedObject != null, "deserializedObject parameter != null");

            List<T> result = null;
            PSPropertyInfo deserializedListProperty = deserializedObject.Properties[propertyName];
            if (deserializedListProperty != null)
            {
                result = RehydrateList<T>(commandName, deserializedListProperty.Value, itemRehydrator);
            }

            return result;
        }

        private List<T> RehydrateList<T>(string commandName, object deserializedList, Func<PSObject, T> itemRehydrator)
        {
            itemRehydrator ??= (PSObject pso) => ConvertTo<T>(commandName, pso);

            List<T> result = null;

            ArrayList list = ConvertTo<ArrayList>(commandName, deserializedList, true);
            if (list != null)
            {
                result = new List<T>();
                foreach (object o in list)
                {
                    PSObject deserializedItem = ConvertTo<PSObject>(commandName, o);
                    T item = itemRehydrator(deserializedItem);
                    result.Add(item);
                }
            }

            return result;
        }

        private Dictionary<TKey, TValue> RehydrateDictionary<TKey, TValue>(
            string commandName,
            PSObject deserializedObject,
            string propertyName,
            Func<PSObject, TValue> valueRehydrator)
        {
            Dbg.Assert(deserializedObject != null, "deserializedObject parameter != null");
            Dbg.Assert(!string.IsNullOrEmpty(propertyName), "propertyName parameter != null");

            valueRehydrator ??= (PSObject pso) => ConvertTo<TValue>(commandName, pso);

            Dictionary<TKey, TValue> result = new();
            PSPropertyInfo deserializedDictionaryProperty = deserializedObject.Properties[propertyName];
            if (deserializedDictionaryProperty != null)
            {
                Hashtable deserializedDictionary = ConvertTo<Hashtable>(commandName, deserializedDictionaryProperty.Value, true);
                if (deserializedDictionary != null)
                {
                    foreach (DictionaryEntry deserializedItem in deserializedDictionary)
                    {
                        TKey itemKey = ConvertTo<TKey>(commandName, deserializedItem.Key);

                        PSObject deserializedItemValue = ConvertTo<PSObject>(commandName, deserializedItem.Value);
                        TValue itemValue = valueRehydrator(deserializedItemValue);

                        result.Add(itemKey, itemValue);
                    }
                }
            }

            return result;
        }

        #endregion

        #region CommandInfo-specific rehydration helpers

        /// <summary>
        /// Validates that a name or identifier is safe to use in generated code
        /// (i.e. it can't be used for code injection attacks).
        /// </summary>
        /// <param name="name">Name to validate.</param>
        /// <returns><see langword="true"/> if the name is safe; <see langword="false"/> otherwise.</returns>
        private static bool IsSafeNameOrIdentifier(string name)
        {
            // '.' is needed for stuff like net.exe
            // ':' and '\' are included because they are present in default functions (cd\, C:)
            // \p{Ll}\p{Lu}\p{Lt} => letter lower/upper/title-case
            // \p{Lo}\p{Nd}\p{Lm} => letter other, digit, letter modifier
            // we reject names longer than 100 characters
            return !string.IsNullOrEmpty(name) &&
                Regex.IsMatch(name, CommandMetadata.isSafeNameOrIdentifierRegex,
                RegexOptions.CultureInvariant | RegexOptions.Singleline);
        }

        /// <summary>
        /// Validates that a parameter name is safe to use in generated code
        /// (i.e. it can't be used for code injection attacks).
        /// </summary>
        /// <param name="parameterName">Parameter name to validate.</param>
        /// <returns><see langword="true"/> if the name is safe; <see langword="false"/> otherwise.</returns>
        private static bool IsSafeParameterName(string parameterName)
        {
            return IsSafeNameOrIdentifier(parameterName) && !parameterName.Contains(':');
        }

        /// <summary>
        /// Validates that a type can be safely used as a type constraint
        /// (i.e. it doesn't introduce any side effects on the client).
        /// </summary>
        /// <param name="type">Type to validate.</param>
        /// <returns><see langword="true"/> if the type is safe; <see langword="false"/> otherwise.</returns>
        private static bool IsSafeTypeConstraint(Type type)
        {
            if (type == null)
            {
                return true; // no type constraint => safe
            }

            if (type.IsArray)
            {
                return IsSafeTypeConstraint(type.GetElementType());
            }
            else if (type.Equals(typeof(Hashtable)))
            {
                return true;
            }
            else if (type.Equals(typeof(SwitchParameter)))
            {
                return true;
            }
            else if (type.Equals(typeof(PSCredential)))
            {
                return true;
            }
            else if (type.Equals(typeof(System.Security.SecureString)))
            {
                return true;
            }
            else if (KnownTypes.GetTypeSerializationInfo(type) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Validates that command metadata returned from the (potentially malicious) server is safe.
        /// Writes error messages if necessary.  Modifies command metadata to make it safe if necessary.
        /// </summary>
        /// <param name="commandMetadata">Command metadata to verify.</param>
        /// <returns><see langword="true"/> if the command metadata is safe; <see langword="false"/> otherwise.</returns>
        private bool IsSafeCommandMetadata(CommandMetadata commandMetadata)
        {
            if (!IsCommandNameMatchingParameters(commandMetadata.Name))
            {
                this.WriteError(this.GetErrorSkippedNonRequestedCommand(commandMetadata.Name));
                return false;
            }

            if (!IsSafeNameOrIdentifier(commandMetadata.Name))
            {
                this.WriteError(this.GetErrorSkippedUnsafeCommandName(commandMetadata.Name));
                return false;
            }

            if ((commandMetadata.DefaultParameterSetName != null) &&
                !IsSafeNameOrIdentifier(commandMetadata.DefaultParameterSetName))
            {
                this.WriteError(this.GetErrorSkippedUnsafeNameInMetadata(
                    commandMetadata.Name,
                    "ParameterSet",
                    commandMetadata.DefaultParameterSetName));
                return false;
            }

            Dbg.Assert(commandMetadata.CommandType == null, "CommandType shouldn't get rehydrated");
            Dbg.Assert(!commandMetadata.ImplementsDynamicParameters, "Proxies shouldn't do dynamic parameters");

            if (commandMetadata.Parameters != null)
            {
                foreach (ParameterMetadata parameter in commandMetadata.Parameters.Values)
                {
                    Dbg.Assert(parameter.Attributes == null || parameter.Attributes.Count == 0,
                        "Attributes shouldn't get rehydrated");

                    // sanitize - remove type constraint that are not allowed
                    if (!IsSafeTypeConstraint(parameter.ParameterType))
                    {
                        parameter.ParameterType = null;
                    }

                    if (!IsSafeParameterName(parameter.Name))
                    {
                        this.WriteError(this.GetErrorSkippedUnsafeNameInMetadata(
                            commandMetadata.Name,
                            "Parameter",
                            parameter.Name));
                        return false;
                    }

                    if (parameter.Aliases != null)
                    {
                        foreach (string alias in parameter.Aliases)
                        {
                            if (!IsSafeNameOrIdentifier(alias))
                            {
                                this.WriteError(this.GetErrorSkippedUnsafeNameInMetadata(
                                    commandMetadata.Name,
                                    "Alias",
                                    alias));
                                return false;
                            }
                        }
                    }

                    if (parameter.ParameterSets != null)
                    {
                        foreach (KeyValuePair<string, ParameterSetMetadata> setPair in parameter.ParameterSets)
                        {
                            if (!IsSafeNameOrIdentifier(setPair.Key))
                            {
                                this.WriteError(this.GetErrorSkippedUnsafeNameInMetadata(
                                    commandMetadata.Name,
                                    "ParameterSet",
                                    setPair.Key));
                                return false;
                            }

                            ParameterSetMetadata parameterSet = setPair.Value;
                            Dbg.Assert(string.IsNullOrEmpty(parameterSet.HelpMessageBaseName), "HelpMessageBaseName shouldn't get rehydrated");
                            Dbg.Assert(string.IsNullOrEmpty(parameterSet.HelpMessageResourceId), "HelpMessageResourceId shouldn't get rehydrated");
                        }
                    }
                }
            }

            return true;
        }

        private Type RehydrateParameterType(PSObject deserializedParameterMetadata)
        {
            bool switchParameter = GetPropertyValue<bool>("Get-Command", deserializedParameterMetadata, "SwitchParameter");
            if (switchParameter)
            {
                return typeof(SwitchParameter);
            }
            else
            {
                return typeof(object);
            }
        }

        private ParameterMetadata RehydrateParameterMetadata(PSObject deserializedParameterMetadata)
        {
            if (deserializedParameterMetadata == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(deserializedParameterMetadata));
            }

            string name = GetPropertyValue<string>("Get-Command", deserializedParameterMetadata, "Name");
            bool isDynamic = GetPropertyValue<bool>("Get-Command", deserializedParameterMetadata, "IsDynamic");

            Type parameterType = RehydrateParameterType(deserializedParameterMetadata);
            List<string> aliases = RehydrateList<string>("Get-Command", deserializedParameterMetadata, "Aliases", null);

            ParameterSetMetadata parameterSetMetadata = new(int.MinValue, 0, null);
            Dictionary<string, ParameterSetMetadata> parameterSets = new(StringComparer.OrdinalIgnoreCase);
            parameterSets.Add(ParameterAttribute.AllParameterSets, parameterSetMetadata);

            return new ParameterMetadata(
                aliases == null ? new Collection<string>() : new Collection<string>(aliases),
                isDynamic,
                name,
                parameterSets,
                parameterType);
        }

        private static bool IsProxyForCmdlet(Dictionary<string, ParameterMetadata> parameters)
        {
            // we are not sending CmdletBinding/DefaultParameterSet over the wire anymore
            // we need to infer IsProxyForCmdlet from presence of all common parameters

            foreach (string commonParameterName in Cmdlet.CommonParameters)
            {
                if (!parameters.ContainsKey(commonParameterName))
                {
                    return false;
                }
            }

            return true;
        }

        private CommandMetadata RehydrateCommandMetadata(PSObject deserializedCommandInfo, out string resolvedCommandName)
        {
            if (deserializedCommandInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(deserializedCommandInfo));
            }

            string name = GetPropertyValue<string>("Get-Command", deserializedCommandInfo, "Name");

            CommandTypes commandType = GetPropertyValue<CommandTypes>("Get-Command", deserializedCommandInfo, "CommandType");
            if (commandType == CommandTypes.Alias)
            {
                resolvedCommandName = GetPropertyValue<string>("Get-Command", deserializedCommandInfo, "ResolvedCommandName", true);
                if (string.IsNullOrEmpty(resolvedCommandName))
                {
                    this.WriteError(this.GetErrorCouldntResolvedAlias(name));
                }
            }
            else
            {
                resolvedCommandName = null;
            }

            Dictionary<string, ParameterMetadata> parameters = RehydrateDictionary<string, ParameterMetadata>("Get-Command", deserializedCommandInfo, "Parameters", RehydrateParameterMetadata);

            // add client-side AsJob parameter
            parameters.Remove("AsJob");
            ParameterMetadata asJobParameter = new("AsJob", typeof(SwitchParameter));
            parameters.Add(asJobParameter.Name, asJobParameter);

            return new CommandMetadata(
                                   name: name,
                            commandType: commandType,
                       isProxyForCmdlet: IsProxyForCmdlet(parameters),
                defaultParameterSetName: ParameterAttribute.AllParameterSets,
                  supportsShouldProcess: false,
                          confirmImpact: ConfirmImpact.None,
                         supportsPaging: false,
                   supportsTransactions: false,
                      positionalBinding: true,
                             parameters: parameters);
        }

        private static int GetCommandTypePriority(CommandTypes commandType)
        {
            switch (commandType)
            {
                case CommandTypes.Alias:
                    return 10;

                // there is only one function table
                case CommandTypes.Filter:
                case CommandTypes.Function:
                case CommandTypes.Script:
                    return 20;

                case CommandTypes.Cmdlet:
                    return 30;

                // application/externalScript order depends on remote $env:path variable
                case CommandTypes.Application:
                case CommandTypes.ExternalScript:
                    return 40;

                default:
                    Dbg.Assert(false, "Unknown value of CommandTypes enumeration");
                    return 50;
            }
        }

        /// <summary>
        /// Converts remote (deserialized) CommandInfo objects into CommandMetadata equivalents.
        /// </summary>
        /// <param name="name2commandMetadata">Dictionary where rehydrated CommandMetadata are going to be stored.</param>
        /// <param name="alias2resolvedCommandName">Dictionary mapping alias names to resolved command names.</param>
        /// <param name="remoteCommandInfo">Remote (deserialized) CommandInfo object.</param>
        /// <returns>CommandMetadata equivalents.</returns>
        private void AddRemoteCommandMetadata(
            Dictionary<string, CommandMetadata> name2commandMetadata,
            Dictionary<string, string> alias2resolvedCommandName,
            PSObject remoteCommandInfo)
        {
            Dbg.Assert(name2commandMetadata != null, "name2commandMetadata parameter != null");
            Dbg.Assert(alias2resolvedCommandName != null, "alias2resolvedCommandName parameter != null");
            Dbg.Assert(remoteCommandInfo != null, "remoteCommandInfo parameter != null");

            string resolvedCommandName;
            CommandMetadata commandMetadata = RehydrateCommandMetadata(remoteCommandInfo, out resolvedCommandName);
            if (!IsSafeCommandMetadata(commandMetadata))
            {
                return;
            }

            if (resolvedCommandName != null && !IsSafeNameOrIdentifier(commandMetadata.Name))
            {
                this.WriteError(this.GetErrorSkippedUnsafeCommandName(resolvedCommandName));
                return;
            }

            if (IsCommandSkippedByServerDeclaration(commandMetadata.Name))
            {
                return;
            }

            if (!IsCommandNameAllowedForImport(commandMetadata.Name))
            {
                return;
            }

            CommandMetadata previousCommandWithSameName;
            if (name2commandMetadata.TryGetValue(commandMetadata.Name, out previousCommandWithSameName))
            {
                int previousCommandPriority = GetCommandTypePriority(previousCommandWithSameName.WrappedCommandType);
                int currentCommandPriority = GetCommandTypePriority(commandMetadata.WrappedCommandType);
                if (previousCommandPriority < currentCommandPriority)
                {
                    return;
                }
            }

            if (resolvedCommandName != null)
            {
                alias2resolvedCommandName[commandMetadata.Name] = resolvedCommandName;
                commandMetadata.Name = resolvedCommandName;
            }

            name2commandMetadata[commandMetadata.Name] = commandMetadata;
        }

        #endregion

        #region Logic to avoid format data we don't want to shadow

        private bool IsTypeNameMatchingParameters(string name)
        {
            return SessionStateUtilities.MatchesAnyWildcardPattern(name, _formatTypeNamePatterns, false);
        }

        private bool IsSafeTypeDefinition(ExtendedTypeDefinition typeDefinition)
        {
            if (!IsTypeNameMatchingParameters(typeDefinition.TypeName))
            {
                this.WriteError(this.GetErrorSkippedNonRequestedTypeDefinition(typeDefinition.TypeName));
                return false;
            }

            return true;
        }

        private void AddRemoteTypeDefinition(IList<ExtendedTypeDefinition> listOfTypeDefinitions, PSObject remoteTypeDefinition)
        {
            Dbg.Assert(listOfTypeDefinitions != null, "listOfTypeDefinitions parameter != null");
            Dbg.Assert(remoteTypeDefinition != null, "remoteTypeDefinition parameter != null");

            ExtendedTypeDefinition typeDefinition = ConvertTo<ExtendedTypeDefinition>("Get-FormatData", remoteTypeDefinition);
            if (!IsSafeTypeDefinition(typeDefinition))
            {
                return;
            }

            listOfTypeDefinitions.Add(typeDefinition);
        }

        #endregion

        #region Helpers for executing remote commands

        private bool _assumeMeasureObjectIsAvailable = true;

        private int CountRemoteObjects(PowerShell powerShell)
        {
            if (!_assumeMeasureObjectIsAvailable)
            {
                return -1;
            }

            try
            {
                powerShell.AddCommand("Measure-Object");

                Collection<PSObject> measurements;
                using (new PowerShellStopper(this.Context, powerShell))
                {
                    measurements = powerShell.Invoke();
                }

                if ((measurements == null) || (measurements.Count != 1))
                {
                    _assumeMeasureObjectIsAvailable = false;
                    return -1;
                }

                PSPropertyInfo countProperty = measurements[0].Properties["Count"];
                if (countProperty == null)
                {
                    _assumeMeasureObjectIsAvailable = false;
                    return -1;
                }

                int count;
                if (LanguagePrimitives.TryConvertTo<int>(countProperty.Value, out count))
                {
                    return count;
                }
                else
                {
                    _assumeMeasureObjectIsAvailable = false;
                    return -1;
                }
            }
            catch (RuntimeException)
            {
                // just return -1 if remote Measure-Object invocation fails for any reason
                _assumeMeasureObjectIsAvailable = false;
                return -1;
            }
        }

        internal void DuplicatePowerShellStreams(PowerShell powerShell)
        {
            foreach (ErrorRecord record in powerShell.Streams.Error.ReadAll())
            {
                this.WriteError(record);
            }

            foreach (WarningRecord record in powerShell.Streams.Warning.ReadAll())
            {
                this.WriteWarning(record.Message);
            }

            foreach (VerboseRecord record in powerShell.Streams.Verbose.ReadAll())
            {
                this.WriteVerbose(record.Message);
            }

            foreach (DebugRecord record in powerShell.Streams.Debug.ReadAll())
            {
                this.WriteDebug(record.Message);
            }

            foreach (InformationRecord record in powerShell.Streams.Information.ReadAll())
            {
                this.WriteInformation(record);
            }
        }

        #endregion

        #region Executing remote Get-FormatData (and Get-TypeData in the future?)

        private PowerShell BuildPowerShellForGetFormatData()
        {
            PowerShell powerShell = PowerShell.Create();

            powerShell.AddCommand("Get-FormatData");
            powerShell.AddParameter("TypeName", this.FormatTypeName);

            // For remote PS version 5.1 and greater, we need to include the new -PowerShellVersion parameter
            RemoteRunspace remoteRunspace = Session.Runspace as RemoteRunspace;
            if ((remoteRunspace != null) && (remoteRunspace.ServerVersion != null) &&
                (remoteRunspace.ServerVersion >= new Version(5, 1)))
            {
                powerShell.AddParameter("PowerShellVersion", PSVersionInfo.PSVersion);
            }

            powerShell.Runspace = Session.Runspace;

            return powerShell;
        }

        /// <summary>
        /// Gets CommandMetadata objects from remote runspace.
        /// </summary>
        /// <returns>(rehydrated) CommandMetadata objects.</returns>
        internal List<ExtendedTypeDefinition> GetRemoteFormatData()
        {
            if ((this.FormatTypeName == null) || (this.FormatTypeName.Length == 0) ||
                (_commandParameterSpecified && !_formatTypeNamesSpecified))
            {
                return new List<ExtendedTypeDefinition>();
            }

            this.WriteProgress(StringUtil.Format(ImplicitRemotingStrings.ProgressStatusGetFormatDataStart), null, null);

            using (PowerShell powerShell = this.BuildPowerShellForGetFormatData())
            {
                IAsyncResult asyncResult = null;
                try
                {
                    int expectedCount = -1;
                    using (PowerShell countingPowerShell = this.BuildPowerShellForGetFormatData())
                    {
                        expectedCount = this.CountRemoteObjects(countingPowerShell);
                    }

                    // invoke
                    using (new PowerShellStopper(this.Context, powerShell))
                    {
                        DateTime startTime = DateTime.UtcNow;

                        PSDataCollection<PSObject> asyncOutput = new();

                        // process output and errors as soon as possible
                        asyncResult = powerShell.BeginInvoke<PSObject, PSObject>(null, asyncOutput);
                        int numberOfReceivedObjects = 0;
                        List<ExtendedTypeDefinition> result = new();
                        foreach (PSObject deserializedFormatData in asyncOutput)
                        {
                            AddRemoteTypeDefinition(result, deserializedFormatData);
                            this.DuplicatePowerShellStreams(powerShell);
                            this.WriteProgress(startTime, ++numberOfReceivedObjects, expectedCount, ImplicitRemotingStrings.ProgressStatusGetFormatDataProgress);
                        }

                        this.DuplicatePowerShellStreams(powerShell);
                        powerShell.EndInvoke(asyncResult);

                        if ((numberOfReceivedObjects == 0) && (_formatTypeNamesSpecified))
                        {
                            this.ThrowTerminatingError(this.GetErrorNoResultsFromRemoteEnd("Get-FormatData"));
                        }

                        return result;
                    }
                }
                catch (RuntimeException e)
                {
                    // process terminating errors from remote end
                    this.ThrowTerminatingError(this.GetErrorFromRemoteCommand("Get-FormatData", e));
                }
            }

            // silencing the compiler with the "return" statement
            Dbg.Assert(false, "We should never get here");
            return null;
        }

        #endregion

        #region Executing remote Get-Command

        private PowerShell BuildPowerShellForGetCommand()
        {
            PowerShell powerShell = PowerShell.Create();

            powerShell.AddCommand("Get-Command");
            powerShell.AddParameter("CommandType", this.CommandType);
            if (this.CommandName != null)
            {
                powerShell.AddParameter("Name", this.CommandName);
            }

            powerShell.AddParameter(nameof(Module), this.Module);
            if (IsFullyQualifiedModuleSpecified)
            {
                powerShell.AddParameter(nameof(FullyQualifiedModule), this.FullyQualifiedModule);
            }

            powerShell.AddParameter("ArgumentList", this.ArgumentList);

            powerShell.Runspace = Session.Runspace;
            powerShell.RemotePowerShell.HostCallReceived += HandleHostCallReceived;
            return powerShell;
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void HandleHostCallReceived(object sender, RemoteDataEventArgs<RemoteHostCall> eventArgs)
        {
            System.Management.Automation.Runspaces.Internal.ClientRemotePowerShell.ExitHandler(sender, eventArgs);
        }

        /// <summary>
        /// Gets CommandMetadata objects from remote runspace.
        /// </summary>
        /// <returns>(rehydrated) CommandMetadata objects.</returns>
        internal List<CommandMetadata> GetRemoteCommandMetadata(out Dictionary<string, string> alias2resolvedCommandName)
        {
            bool isReleaseCandidateBackcompatibilityMode =
                this.Session.Runspace.GetRemoteProtocolVersion() == RemotingConstants.ProtocolVersionWin7RC;

            alias2resolvedCommandName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if ((this.CommandName == null) || (this.CommandName.Length == 0) ||
                (!_commandParameterSpecified && _formatTypeNamesSpecified))
            {
                return new List<CommandMetadata>();
            }

            this.WriteProgress(StringUtil.Format(ImplicitRemotingStrings.ProgressStatusGetCommandStart), null, null);

            using (PowerShell powerShell = this.BuildPowerShellForGetCommand())
            {
                powerShell.AddCommand("Select-Object");
                powerShell.AddParameter("Property", new string[] {
                    "Name", "CommandType", "ResolvedCommandName", "DefaultParameterSet", "CmdletBinding", "Parameters"});
                powerShell.IsGetCommandMetadataSpecialPipeline = !isReleaseCandidateBackcompatibilityMode;

                IAsyncResult asyncResult = null;
                try
                {
                    int expectedCount = -1;
                    if (isReleaseCandidateBackcompatibilityMode)
                    {
                        using (PowerShell countingPowerShell = this.BuildPowerShellForGetCommand())
                        {
                            expectedCount = this.CountRemoteObjects(countingPowerShell);
                        }
                    }

                    Dictionary<string, CommandMetadata> name2commandMetadata =
                        new(StringComparer.OrdinalIgnoreCase);

                    // invoke
                    using (new PowerShellStopper(this.Context, powerShell))
                    {
                        DateTime startTime = DateTime.UtcNow;

                        PSDataCollection<PSObject> asyncOutput = new();

                        // process output and errors as soon as possible
                        asyncResult = powerShell.BeginInvoke<PSObject, PSObject>(null, asyncOutput);
                        int numberOfReceivedObjects = 0;
                        foreach (PSObject deserializedCommandInfo in asyncOutput)
                        {
                            if (!isReleaseCandidateBackcompatibilityMode && expectedCount == -1)
                            {
                                expectedCount = RemotingDecoder.GetPropertyValue<int>(
                                    deserializedCommandInfo,
                                    RemoteDataNameStrings.DiscoveryCount);

                                continue;
                            }

                            AddRemoteCommandMetadata(name2commandMetadata, alias2resolvedCommandName, deserializedCommandInfo);
                            this.DuplicatePowerShellStreams(powerShell);
                            this.WriteProgress(startTime, ++numberOfReceivedObjects, expectedCount, ImplicitRemotingStrings.ProgressStatusGetCommandProgress);
                        }

                        this.DuplicatePowerShellStreams(powerShell);
                        powerShell.EndInvoke(asyncResult);

                        if ((numberOfReceivedObjects == 0) && (_commandParameterSpecified))
                        {
                            this.ThrowTerminatingError(this.GetErrorNoResultsFromRemoteEnd("Get-Command"));
                        }

                        return new List<CommandMetadata>(name2commandMetadata.Values);
                    }
                }
                catch (RuntimeException e)
                {
                    // process terminating errors from remote end
                    this.ThrowTerminatingError(this.GetErrorFromRemoteCommand("Get-Command", e));
                }
            }

            // silencing the compiler with the "return" statement
            Dbg.Assert(false, "We should never get here");
            return null;
        }

        #endregion

        #region Reporting progress

        private DateTime _lastTimeProgressWasWritten = DateTime.UtcNow;

        private void WriteProgress(string statusDescription, int? percentComplete, int? secondsRemaining)
        {
            ProgressRecordType recordType;
            if (secondsRemaining.HasValue && secondsRemaining.Value == 0 &&
                percentComplete.HasValue && percentComplete.Value == 100)
            {
                recordType = ProgressRecordType.Completed;
            }
            else
            {
                recordType = ProgressRecordType.Processing;
            }

            if (recordType == ProgressRecordType.Processing)
            {
                TimeSpan timeSinceProgressWasWrittenLast = DateTime.UtcNow - _lastTimeProgressWasWritten;
                if (timeSinceProgressWasWrittenLast < TimeSpan.FromMilliseconds(200))
                {
                    return;
                }
            }

            _lastTimeProgressWasWritten = DateTime.UtcNow;

            string activityDescription = StringUtil.Format(ImplicitRemotingStrings.ProgressActivity);
            ProgressRecord progressRecord = new(
                1905347799, // unique id for ImplicitRemoting (I just picked a random number)
                activityDescription,
                statusDescription);

            if (percentComplete.HasValue)
            {
                progressRecord.PercentComplete = percentComplete.Value;
            }

            if (secondsRemaining.HasValue)
            {
                progressRecord.SecondsRemaining = secondsRemaining.Value;
            }

            progressRecord.RecordType = recordType;

            this.WriteProgress(progressRecord);
        }

        private void WriteProgress(DateTime startTime, int currentCount, int expectedCount, string resourceId)
        {
            Dbg.Assert(currentCount > 0, "Progress shouldn't be written before 1 result is received");

            string message = StringUtil.Format(resourceId, currentCount);
            if (expectedCount <= 0)
            {
                this.WriteProgress(message, null, null);
            }
            else
            {
                double percentComplete = (double)currentCount / expectedCount;
                int? secondsRemaining = ProgressRecord.GetSecondsRemaining(startTime, percentComplete);

                this.WriteProgress(message, (int)(100.0 * percentComplete), secondsRemaining);
            }
        }

        #endregion

        #region Generating a proxy module

        internal Guid ModuleGuid { get; } = Guid.NewGuid();

        /// <summary>
        /// Generates a proxy module in the given directory.
        /// </summary>
        /// <param name="moduleRootDirectory">Base directory for the module.</param>
        /// <param name="moduleNamePrefix">FileName prefix for module files.</param>
        /// <param name="encoding">Encoding of generated files.</param>
        /// <param name="force">Whether to overwrite files.</param>
        /// <param name="listOfCommandMetadata">Remote commands to generate proxies for.</param>
        /// <param name="alias2resolvedCommandName">Dictionary mapping alias names to resolved command names.</param>
        /// <param name="listOfFormatData">Remote format data to generate format.ps1xml for.</param>
        /// <returns>Paths to generated files.</returns>
        internal List<string> GenerateProxyModule(
            DirectoryInfo moduleRootDirectory,
            string moduleNamePrefix,
            Encoding encoding,
            bool force,
            List<CommandMetadata> listOfCommandMetadata,
            Dictionary<string, string> alias2resolvedCommandName,
            List<ExtendedTypeDefinition> listOfFormatData)
        {
            if (_commandsSkippedBecauseOfShadowing.Count != 0)
            {
                this.ReportSkippedCommands();

                if (listOfCommandMetadata.Count == 0)
                {
                    ErrorRecord error = this.GetErrorNoCommandsImportedBecauseOfSkipping();
                    this.ThrowTerminatingError(error);
                }
            }

            ImplicitRemotingCodeGenerator codeGenerator = new(
                this.Session,
                this.ModuleGuid,
                this.MyInvocation);

            List<string> generatedFiles = codeGenerator.GenerateProxyModule(
                moduleRootDirectory,
                moduleNamePrefix,
                encoding,
                force,
                listOfCommandMetadata,
                alias2resolvedCommandName,
                listOfFormatData,
                Certificate);

            this.WriteProgress(StringUtil.Format(ImplicitRemotingStrings.ProgressStatusCompleted), 100, 0);

            return generatedFiles;
        }

        #endregion
    }

    internal class ImplicitRemotingCodeGenerator
    {
        internal static readonly Version VersionOfScriptWriter = new(1, 0);

        #region Constructor and shared private data

        private readonly PSSession _remoteRunspaceInfo;
        private readonly Guid _moduleGuid;
        private readonly InvocationInfo _invocationInfo;

        internal ImplicitRemotingCodeGenerator(
            PSSession remoteRunspaceInfo,
            Guid moduleGuid,
            InvocationInfo invocationInfo)
        {
            Dbg.Assert(remoteRunspaceInfo != null, "Caller should validate remoteRunspaceInfo != null");
            Dbg.Assert(moduleGuid != Guid.Empty, "Caller should validate moduleGuid is not empty");
            Dbg.Assert(invocationInfo != null, "Caller should validate invocationInfo != null");

            _remoteRunspaceInfo = remoteRunspaceInfo;
            _moduleGuid = moduleGuid;
            _invocationInfo = invocationInfo;
        }

        #endregion

        #region Code generation helpers

        /// <summary>
        /// Gets a connection URI associated with the remote runspace.
        /// </summary>
        /// <returns>Connection URI associated with the remote runspace.</returns>
        private string GetConnectionString()
        {
            WSManConnectionInfo connectionInfo = _remoteRunspaceInfo.Runspace.ConnectionInfo as WSManConnectionInfo;
            if (connectionInfo != null)
            {
                return connectionInfo.ConnectionUri.ToString();
            }

            VMConnectionInfo vmConnectionInfo = _remoteRunspaceInfo.Runspace.ConnectionInfo as VMConnectionInfo;
            if (vmConnectionInfo != null)
            {
                return vmConnectionInfo.ComputerName;
            }

            ContainerConnectionInfo containerConnectionInfo = _remoteRunspaceInfo.Runspace.ConnectionInfo as ContainerConnectionInfo;
            if (containerConnectionInfo != null)
            {
                return containerConnectionInfo.ComputerName;
            }

            /*
               wsman will also work with something that Uri.IsWellFormedUriString fails on:
               http://[0000:0000:0000:0000:0000:0000:0000:0001]/wsman
            Dbg.Assert(
                Uri.IsWellFormedUriString(connectionString, UriKind.Absolute),
                "GetConnectionString() should return only well formed uri strings");
             */
            return null;
        }

        private static string EscapeFunctionNameForRemoteHelp(string name)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            StringBuilder result = new(name.Length);
            foreach (char c in name)
            {
                if (!"\"'`$".Contains(c)
                    && !char.IsControl(c)
                    && !char.IsWhiteSpace(c))
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        private const string SectionSeparator = @"
##############################################################################
";

        private static void GenerateSectionSeparator(TextWriter writer)
        {
            writer.Write(SectionSeparator);
        }

        #endregion

        #region Generating manifest for proxy module

        private const string ManifestTemplate = @"
@{{
    GUID = '{0}'
    Description = '{1}'
    ModuleToProcess = @('{2}')
    FormatsToProcess = @('{3}')

    ModuleVersion = '1.0'

    PrivateData = @{{
        ImplicitRemoting = $true
    }}
}}
        ";

        private void GenerateManifest(TextWriter writer, string psm1fileName, string formatPs1xmlFileName)
        {
            if (writer == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(writer));
            }

            GenerateTopComment(writer);

            writer.Write(
                ManifestTemplate,
                CodeGeneration.EscapeSingleQuotedStringContent(_moduleGuid.ToString()),
                CodeGeneration.EscapeSingleQuotedStringContent(StringUtil.Format(ImplicitRemotingStrings.ProxyModuleDescription, this.GetConnectionString())),
                CodeGeneration.EscapeSingleQuotedStringContent(Path.GetFileName(psm1fileName)),
                CodeGeneration.EscapeSingleQuotedStringContent(Path.GetFileName(formatPs1xmlFileName)));
        }

        #endregion

        #region Generating header of a proxy module

        private const string TopCommentTemplate = @"
<#
 # {0}
 # {1}
 # {2}
 # {3}
 #>
        ";

        private void GenerateTopComment(TextWriter writer)
        {
            writer.Write(
                TopCommentTemplate,
                CodeGeneration.EscapeBlockCommentContent(StringUtil.Format(ImplicitRemotingStrings.ModuleHeaderTitle)),
                CodeGeneration.EscapeBlockCommentContent(StringUtil.Format(ImplicitRemotingStrings.ModuleHeaderDate, DateTime.Now.ToString(CultureInfo.CurrentCulture))),
                CodeGeneration.EscapeBlockCommentContent(StringUtil.Format(ImplicitRemotingStrings.ModuleHeaderCommand, _invocationInfo.MyCommand.Name)),
                CodeGeneration.EscapeBlockCommentContent(StringUtil.Format(ImplicitRemotingStrings.ModuleHeaderCommandLine, _invocationInfo.Line)));
        }

        private const string HeaderTemplate = @"
param(
    <# {0} #>
    [System.Management.Automation.Runspaces.PSSession] $PSSessionOverride,
    [System.Management.Automation.Remoting.PSSessionOption] $PSSessionOptionOverride
)

$script:__psImplicitRemoting_versionOfScriptGenerator = {1}
if ($script:__psImplicitRemoting_versionOfScriptGenerator.Major -ne {2})
{{
    throw '{3}'
}}

$script:WriteHost = $executionContext.InvokeCommand.GetCommand('Write-Host', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:WriteWarning = $executionContext.InvokeCommand.GetCommand('Write-Warning', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:WriteInformation = $executionContext.InvokeCommand.GetCommand('Write-Information', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:GetPSSession = $executionContext.InvokeCommand.GetCommand('Get-PSSession', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:NewPSSession = $executionContext.InvokeCommand.GetCommand('New-PSSession', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:ConnectPSSession = $executionContext.InvokeCommand.GetCommand('Connect-PSSession', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:NewObject = $executionContext.InvokeCommand.GetCommand('New-Object', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:RemovePSSession = $executionContext.InvokeCommand.GetCommand('Remove-PSSession', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:InvokeCommand = $executionContext.InvokeCommand.GetCommand('Invoke-Command', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:SetItem = $executionContext.InvokeCommand.GetCommand('Set-Item', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:ImportCliXml = $executionContext.InvokeCommand.GetCommand('Import-CliXml', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:NewPSSessionOption = $executionContext.InvokeCommand.GetCommand('New-PSSessionOption', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:JoinPath = $executionContext.InvokeCommand.GetCommand('Join-Path', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:ExportModuleMember = $executionContext.InvokeCommand.GetCommand('Export-ModuleMember', [System.Management.Automation.CommandTypes]::Cmdlet)
$script:SetAlias = $executionContext.InvokeCommand.GetCommand('Set-Alias', [System.Management.Automation.CommandTypes]::Cmdlet)

$script:MyModule = $MyInvocation.MyCommand.ScriptBlock.Module
        ";

        private void GenerateModuleHeader(TextWriter writer)
        {
            if (writer == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(writer));
            }

            // In Win8, we are no longer loading all assemblies by default.
            // So we need to use the fully qualified name when accessing a type in that assembly
            Type type = typeof(ExportPSSessionCommand);
            string asmName = type.Assembly.GetName().Name;
            string versionOfScriptGenerator = $"[{type.FullName}, {asmName}]::VersionOfScriptGenerator";
            GenerateTopComment(writer);
            writer.Write(
                HeaderTemplate,
                CodeGeneration.EscapeBlockCommentContent(StringUtil.Format(ImplicitRemotingStrings.ModuleHeaderRunspaceOverrideParameter)),
                versionOfScriptGenerator,
                ImplicitRemotingCodeGenerator.VersionOfScriptWriter,
                CodeGeneration.EscapeSingleQuotedStringContent(string.Format(null, PathUtilsStrings.ExportPSSession_ScriptGeneratorVersionMismatch, "Export-PSSession")));
        }

        #endregion

        #region Generating helper functions of a proxy module

        #region Write-PSImplicitRemotingMessage

        private const string HelperFunctionsWriteMessage = @"
function Write-PSImplicitRemotingMessage
{
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]
        $message)

    try { & $script:WriteHost -Object $message -ErrorAction SilentlyContinue } catch { }
}
";

        private static void GenerateHelperFunctionsWriteMessage(TextWriter writer)
        {
            if (writer == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(writer));
            }

            writer.Write(HelperFunctionsWriteMessage);
        }

        #endregion

        #region Set-PSImplicitRemotingSession

        private const string HelperFunctionsSetImplicitRunspaceTemplate = @"
$script:PSSession = $null

function Get-PSImplicitRemotingModuleName {{ $myInvocation.MyCommand.ScriptBlock.File }}

function Set-PSImplicitRemotingSession
{{
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [AllowNull()]
        [Management.Automation.Runspaces.PSSession]
        $PSSession,

        [Parameter(Mandatory = $false, Position = 1)]
        [bool] $createdByModule = $false)

    if ($null -ne $PSSession)
    {{
        $script:PSSession = $PSSession

        if ($createdByModule -and ($null -ne $script:PSSession))
        {{
            $moduleName = Get-PSImplicitRemotingModuleName
            $script:PSSession.Name = '{0}' -f $moduleName

            $oldCleanUpScript = $script:MyModule.OnRemove
            $removePSSessionCommand = $script:RemovePSSession
            $script:MyModule.OnRemove = {{
                & $removePSSessionCommand -Session $PSSession -ErrorAction SilentlyContinue
                if ($oldCleanUpScript)
                {{
                    & $oldCleanUpScript $args
                }}
            }}.GetNewClosure()
        }}
    }}
}}

if ($PSSessionOverride) {{ Set-PSImplicitRemotingSession $PSSessionOverride }}
";

        private static void GenerateHelperFunctionsSetImplicitRunspace(TextWriter writer)
        {
            if (writer == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(writer));
            }

            string runspaceNameTemplate = StringUtil.Format(ImplicitRemotingStrings.ProxyRunspaceNameTemplate);

            writer.Write(
                HelperFunctionsSetImplicitRunspaceTemplate,
                CodeGeneration.EscapeSingleQuotedStringContent(runspaceNameTemplate));
        }

        #endregion

        #region Get-PSImplicitRemotingSessionOption

        private const string HelperFunctionsGetSessionOptionTemplate = @"
function Get-PSImplicitRemotingSessionOption
{{
    if ($null -ne $PSSessionOptionOverride)
    {{
        return $PSSessionOptionOverride
    }}
    else
    {{
        return $({0})
    }}
}}
";

        private void GenerateHelperFunctionsGetSessionOption(TextWriter writer)
        {
            if (writer == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(writer));
            }

            writer.Write(
                HelperFunctionsGetSessionOptionTemplate,
                GenerateNewPSSessionOption());
        }

        private PSPrimitiveDictionary GetApplicationArguments()
        {
            RemoteRunspace remoteRunspace = _remoteRunspaceInfo.Runspace as RemoteRunspace;

            Dbg.Assert(remoteRunspace != null, "PSSessionInfo should refer to a *remote* runspace");
            Dbg.Assert(remoteRunspace.RunspacePool != null, "All remote runspaces are implemented using a runspace pool");
            Dbg.Assert(remoteRunspace.RunspacePool != null, "All remote runspace pools have an internal implementation helper");

            return remoteRunspace.RunspacePool.RemoteRunspacePoolInternal.ApplicationArguments;
        }

        private string GenerateNewPSSessionOption()
        {
            StringBuilder result = new("& $script:NewPSSessionOption ");

            RunspaceConnectionInfo runspaceConnectionInfo = _remoteRunspaceInfo.Runspace.ConnectionInfo as RunspaceConnectionInfo;
            if (runspaceConnectionInfo != null)
            {
                result.Append(null, $"-Culture '{CodeGeneration.EscapeSingleQuotedStringContent(runspaceConnectionInfo.Culture.ToString())}' ");
                result.Append(null, $"-UICulture '{CodeGeneration.EscapeSingleQuotedStringContent(runspaceConnectionInfo.UICulture.ToString())}' ");

                result.Append(null, $"-CancelTimeOut {runspaceConnectionInfo.CancelTimeout} ");
                result.Append(null, $"-IdleTimeOut {runspaceConnectionInfo.IdleTimeout} ");
                result.Append(null, $"-OpenTimeOut {runspaceConnectionInfo.OpenTimeout} ");
                result.Append(null, $"-OperationTimeOut {runspaceConnectionInfo.OperationTimeout} ");
            }

            WSManConnectionInfo wsmanConnectionInfo = _remoteRunspaceInfo.Runspace.ConnectionInfo as WSManConnectionInfo;
            if (wsmanConnectionInfo != null)
            {
                if (!wsmanConnectionInfo.UseCompression)
                {
                    result.Append("-NoCompression ");
                }

                if (wsmanConnectionInfo.NoEncryption)
                {
                    result.Append("-NoEncryption ");
                }

                if (wsmanConnectionInfo.NoMachineProfile)
                {
                    result.Append("-NoMachineProfile ");
                }

                if (wsmanConnectionInfo.UseUTF16)
                {
                    result.Append("-UseUTF16 ");
                }

                if (wsmanConnectionInfo.SkipCACheck)
                {
                    result.Append("-SkipCACheck ");
                }

                if (wsmanConnectionInfo.SkipCNCheck)
                {
                    result.Append("-SkipCNCheck ");
                }

                if (wsmanConnectionInfo.SkipRevocationCheck)
                {
                    result.Append("-SkipRevocationCheck ");
                }

                if (wsmanConnectionInfo.MaximumReceivedDataSizePerCommand.HasValue)
                {
                    result.Append(CultureInfo.InvariantCulture, $"-MaximumReceivedDataSizePerCommand {wsmanConnectionInfo.MaximumReceivedDataSizePerCommand.Value} ");
                }

                if (wsmanConnectionInfo.MaximumReceivedObjectSize.HasValue)
                {
                    result.Append(CultureInfo.InvariantCulture, $"-MaximumReceivedObjectSize {wsmanConnectionInfo.MaximumReceivedObjectSize.Value} ");
                }

                result.Append(CultureInfo.InvariantCulture, $"-MaximumRedirection {wsmanConnectionInfo.MaximumConnectionRedirectionCount} ");

                result.Append(CultureInfo.InvariantCulture, $"-ProxyAccessType {wsmanConnectionInfo.ProxyAccessType} ");
                result.Append(CultureInfo.InvariantCulture, $"-ProxyAuthentication {wsmanConnectionInfo.ProxyAuthentication} ");
                result.Append(this.GenerateProxyCredentialParameter(wsmanConnectionInfo));
            }

            PSPrimitiveDictionary applicationArguments = GetApplicationArguments();
            if (applicationArguments != null)
            {
                result.Append("-ApplicationArguments $(");
                result.Append("& $script:ImportCliXml -Path $(");
                result.Append("& $script:JoinPath -Path $PSScriptRoot -ChildPath ApplicationArguments.xml");
                result.Append(')');
                result.Append(") ");
            }

            return result.ToString();
        }

        // index 0 - dialog title
        // index 1 - dialog body
        // index 2 - user name
        // index 3 - target name (eventually passed to native CredUIPromptForCredentials as pszTargetName)
        private const string ProxyCredentialParameterTemplate =
            "-ProxyCredential ( $host.UI.PromptForCredential( '{0}', '{1}', '{2}', '{3}' ) ) ";

        private string GenerateProxyCredentialParameter(WSManConnectionInfo wsmanConnectionInfo)
        {
            if ((wsmanConnectionInfo == null) || (wsmanConnectionInfo.ProxyCredential == null))
            {
                return string.Empty;
            }
            else
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    ProxyCredentialParameterTemplate,
                    /* 0 */ CodeGeneration.EscapeSingleQuotedStringContent(StringUtil.Format(ImplicitRemotingStrings.CredentialRequestTitle)),
                    /* 1 */ CodeGeneration.EscapeSingleQuotedStringContent(StringUtil.Format(ImplicitRemotingStrings.ProxyCredentialRequestBody, this.GetConnectionString())),
                    /* 2 */ CodeGeneration.EscapeSingleQuotedStringContent(wsmanConnectionInfo.ProxyCredential.UserName),
                    /* 3 */ CodeGeneration.EscapeSingleQuotedStringContent(_remoteRunspaceInfo.ComputerName + @"\httpproxy"));
            }
        }

        #endregion

        #region Get-PSImplicitRemotingSession

        // index 0 - remote runspace id
        // index 1 - message template for new runspace
        // index 2 - expression for getting a new runspace
        // index 3 - message when no runspace is available
        // index 4 - implicit remoting hash
        // index 5 - message for mismatched implicit remoting hash
        // index 6 - key for implicit remoting private data
        // index 7 - key for implicit remoting hash
        private const string HelperFunctionsGetImplicitRunspaceTemplate = @"
function Get-PSImplicitRemotingSession
{{
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]
        $commandName
    )

    $savedImplicitRemotingHash = '{4}'

    if (($null -eq $script:PSSession) -or ($script:PSSession.Runspace.RunspaceStateInfo.State -ne 'Opened'))
    {{
        Set-PSImplicitRemotingSession `
            (& $script:GetPSSession `
                -InstanceId {0} `
                -ErrorAction SilentlyContinue )
    }}

    if (($null -ne $script:PSSession) -and ($script:PSSession.Runspace.RunspaceStateInfo.State -eq 'Disconnected'))
    {{
        # If we are handed a disconnected session, try re-connecting it before creating a new session.
        Set-PSImplicitRemotingSession `
            (& $script:ConnectPSSession `
                -Session $script:PSSession `
                -ErrorAction SilentlyContinue)
    }}

    if (($null -eq $script:PSSession) -or ($script:PSSession.Runspace.RunspaceStateInfo.State -ne 'Opened'))
    {{
        Write-PSImplicitRemotingMessage ('{1}' -f $commandName)

        Set-PSImplicitRemotingSession `
            -CreatedByModule $true `
            -PSSession ( {2} )

        if ($savedImplicitRemotingHash -ne '')
        {{
            $newImplicitRemotingHash = [string]($script:PSSession.ApplicationPrivateData.{6}.{7})
            if ($newImplicitRemotingHash -ne $savedImplicitRemotingHash)
            {{
                & $script:WriteWarning -Message '{5}'
            }}
        }}

        {8}
    }}

    if (($null -eq $script:PSSession) -or ($script:PSSession.Runspace.RunspaceStateInfo.State -ne 'Opened'))
    {{
        throw '{3}'
    }}

    return [Management.Automation.Runspaces.PSSession]$script:PSSession
}}
";

        private void GenerateHelperFunctionsGetImplicitRunspace(TextWriter writer)
        {
            if (writer == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(writer));
            }

            string hashString;
            PSPrimitiveDictionary.TryPathGet(
                _remoteRunspaceInfo.ApplicationPrivateData,
                out hashString,
                ImplicitRemotingCommandBase.ImplicitRemotingKey,
                ImplicitRemotingCommandBase.ImplicitRemotingHashKey);
            hashString ??= string.Empty;

            writer.Write(
                HelperFunctionsGetImplicitRunspaceTemplate,
                /* 0 */ _remoteRunspaceInfo.InstanceId,
                /* 1 */ CodeGeneration.EscapeSingleQuotedStringContent(StringUtil.Format(ImplicitRemotingStrings.CreateNewRunspaceMessageTemplate)),
                /* 2 */ this.GenerateNewRunspaceExpression(),
                /* 3 */ CodeGeneration.EscapeSingleQuotedStringContent(StringUtil.Format(ImplicitRemotingStrings.ErrorNoRunspaceForThisModule)),
                /* 4 */ CodeGeneration.EscapeSingleQuotedStringContent(hashString),
                /* 5 */ CodeGeneration.EscapeSingleQuotedStringContent(StringUtil.Format(ImplicitRemotingStrings.WarningMismatchedImplicitRemotingHash)),
                /* 6 */ ImplicitRemotingCommandBase.ImplicitRemotingKey,
                /* 7 */ ImplicitRemotingCommandBase.ImplicitRemotingHashKey,
                /* 8 */ this.GenerateReimportingOfModules());
        }

        private const string ReimportTemplate = @"
            try {{
                & $script:InvokeCommand -Session $script:PSSession -ScriptBlock {{
                    Get-Module -ListAvailable -Name '{0}' | Import-Module
                }} -ErrorAction SilentlyContinue
            }} catch {{ }}
";

        private string GenerateReimportingOfModules()
        {
            StringBuilder result = new();

            if (_invocationInfo.BoundParameters.ContainsKey(nameof(Module)))
            {
                string[] moduleNames = (string[])_invocationInfo.BoundParameters[nameof(Module)];
                foreach (string moduleName in moduleNames)
                {
                    result.AppendFormat(
                        CultureInfo.InvariantCulture,
                        ReimportTemplate,
                        CodeGeneration.EscapeSingleQuotedStringContent(moduleName));
                }
            }

            return result.ToString();
        }

        #endregion

        #region New-Runspace expression

        // index 0 - connection uri (escaped for inclusion in single quoted string)
        // index 1 - shell name (escaped for inclusion in single quoted string)
        // index 2 - credential (empty string or parameter + value from $host.UI.PromptForCredential)
        // index 3 - certificate thumbprint (empty string or full parameter + value)
        // wsman specific:
        // index 4 - authentication mechanism (empty string of full parameter + value)
        // index 5 - allow redirection
        private const string NewRunspaceTemplate = @"
            $(
                & $script:NewPSSession `
                    {0} -ConfigurationName '{1}' `
                    -SessionOption (Get-PSImplicitRemotingSessionOption) `
                    {2} `
                    {3} `
                    {4} `
                    {5} `
            )
";

        // index 0 - "-VMId <vm id>" (VMId is used instead of VMName due to its uniqueness)
        // index 1 - VM credential
        // index 2 - "-ConfigurationName <configuration name>" or empty string
        private const string NewVMRunspaceTemplate = @"
            $(
                & $script:NewPSSession `
                    {0} `
                    {1} `
                    {2} `
            )
";

        // index 0 - "-ContainerId <container id>"
        // index 1 - "-RunAsAdministrator" or empty string
        // index 2 - "-ConfigurationName <configuration name>" or empty string
        private const string NewContainerRunspaceTemplate = @"
            $(
                & $script:NewPSSession `
                    {0} `
                    {1} `
                    {2} `
            )
";

        private string GenerateNewRunspaceExpression()
        {
            VMConnectionInfo vmConnectionInfo = _remoteRunspaceInfo.Runspace.ConnectionInfo as VMConnectionInfo;
            if (vmConnectionInfo != null)
            {
                string vmConfigurationName = vmConnectionInfo.ConfigurationName;
                return string.Format(
                    CultureInfo.InvariantCulture,
                    NewVMRunspaceTemplate,
                    /* 0 */ this.GenerateConnectionStringForNewRunspace(),
                    /* 1 */ this.GenerateCredentialParameter(),
                    /* 2 */ string.IsNullOrEmpty(vmConfigurationName) ? string.Empty : string.Concat("-ConfigurationName ", vmConfigurationName));
            }
            else
            {
                ContainerConnectionInfo containerConnectionInfo = _remoteRunspaceInfo.Runspace.ConnectionInfo as ContainerConnectionInfo;
                if (containerConnectionInfo != null)
                {
                    string containerConfigurationName = containerConnectionInfo.ContainerProc.ConfigurationName;
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        NewContainerRunspaceTemplate,
                        /* 0 */ this.GenerateConnectionStringForNewRunspace(),
                        /* 1 */ containerConnectionInfo.ContainerProc.RunAsAdmin ? "-RunAsAdministrator" : string.Empty,
                        /* 2 */ string.IsNullOrEmpty(containerConfigurationName) ? string.Empty : string.Concat("-ConfigurationName ", containerConfigurationName));
                }
                else
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        NewRunspaceTemplate,
                        /* 0 */ this.GenerateConnectionStringForNewRunspace(),
                        /* 1 */ CodeGeneration.EscapeSingleQuotedStringContent(_remoteRunspaceInfo.ConfigurationName),
                        /* 2 */ this.GenerateCredentialParameter(),
                        /* 3 */ this.GenerateCertificateThumbprintParameter(),
                        /* 4 */ this.GenerateAuthenticationMechanismParameter(),
                        /* 5 */ this.GenerateAllowRedirectionParameter());
                }
            }
        }

        private const string ComputerNameParameterTemplate = @"-ComputerName '{0}' `
                    -ApplicationName '{1}' {2} {3} ";

        private const string VMIdParameterTemplate = @"-VMId '{0}' ";
        private const string ContainerIdParameterTemplate = @"-ContainerId '{0}' ";

        /// <summary>
        /// This is needed to work with Default Port DCR change from WSMan. See BUG
        /// 542726. If http/https is specified in the connectionURI and no port is
        /// specified then defaults for http/https (80/443) are applied. But WSMan
        /// by default listens on 5985/5986. To overcome this, this function
        /// creates a -ComputerName parameter set or -ConnectionUri parameter
        /// set depending on the situation.
        /// </summary>
        /// <returns></returns>
        private string GenerateConnectionStringForNewRunspace()
        {
            WSManConnectionInfo connectionInfo = _remoteRunspaceInfo.Runspace.ConnectionInfo as WSManConnectionInfo;
            if (connectionInfo == null)
            {
                VMConnectionInfo vmConnectionInfo = _remoteRunspaceInfo.Runspace.ConnectionInfo as VMConnectionInfo;
                if (vmConnectionInfo != null)
                {
                    return string.Format(CultureInfo.InvariantCulture,
                        VMIdParameterTemplate,
                        CodeGeneration.EscapeSingleQuotedStringContent(vmConnectionInfo.VMGuid.ToString()));
                }

                ContainerConnectionInfo containerConnectionInfo = _remoteRunspaceInfo.Runspace.ConnectionInfo as ContainerConnectionInfo;
                if (containerConnectionInfo != null)
                {
                    return string.Format(CultureInfo.InvariantCulture,
                        ContainerIdParameterTemplate,
                        CodeGeneration.EscapeSingleQuotedStringContent(containerConnectionInfo.ContainerProc.ContainerId));
                }

                return null;
            }

            if (connectionInfo.UseDefaultWSManPort)
            {
                bool isSSLSpecified;
                WSManConnectionInfo.GetConnectionString(connectionInfo.ConnectionUri, out isSSLSpecified);
                return string.Format(CultureInfo.InvariantCulture,
                    ComputerNameParameterTemplate,
                    CodeGeneration.EscapeSingleQuotedStringContent(connectionInfo.ComputerName),
                    CodeGeneration.EscapeSingleQuotedStringContent(connectionInfo.AppName),
                    connectionInfo.UseDefaultWSManPort ?
                        string.Empty :
                        string.Create(CultureInfo.InvariantCulture, $"-Port {connectionInfo.Port} "),
                    isSSLSpecified ? "-useSSL" : string.Empty);
            }
            else
            {
                string connectionString = CodeGeneration.EscapeSingleQuotedStringContent(GetConnectionString());
                return string.Create(CultureInfo.InvariantCulture, $"-connectionUri '{connectionString}'");
            }
        }

        private string GenerateAllowRedirectionParameter()
        {
            if (!(_remoteRunspaceInfo.Runspace.ConnectionInfo is WSManConnectionInfo wsmanConnectionInfo))
            {
                return string.Empty;
            }

            if (wsmanConnectionInfo.MaximumConnectionRedirectionCount != 0)
            {
                return "-AllowRedirection";
            }
            else
            {
                return string.Empty;
            }
        }

        private const string AuthenticationMechanismParameterTemplate = "-Authentication {0}";

        private string GenerateAuthenticationMechanismParameter()
        {
            // comment in newrunspacecommand.cs says that -CertificateThumbprint
            // and AuthenticationMechanism are mutually exclusive
            if (_remoteRunspaceInfo.Runspace.ConnectionInfo.CertificateThumbprint != null)
            {
                return string.Empty;
            }

            if (!(_remoteRunspaceInfo.Runspace.ConnectionInfo is WSManConnectionInfo wsmanConnectionInfo))
            {
                return string.Empty;
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                AuthenticationMechanismParameterTemplate,
                wsmanConnectionInfo.AuthenticationMechanism.ToString());
        }

        // index 0 - dialog title
        // index 1 - dialog body
        // index 2 - user name
        // index 3 - target name (eventually passed to native CredUIPromptForCredentials as pszTargetName)
        private const string CredentialParameterTemplate =
            "-Credential ( $host.UI.PromptForCredential( '{0}', '{1}', '{2}', '{3}' ) )";

        private string GenerateCredentialParameter()
        {
            if (_remoteRunspaceInfo.Runspace.ConnectionInfo.Credential == null)
            {
                return string.Empty;
            }
            else
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    CredentialParameterTemplate,
                    /* 0 */ CodeGeneration.EscapeSingleQuotedStringContent(StringUtil.Format(ImplicitRemotingStrings.CredentialRequestTitle)),
                    /* 1 */ CodeGeneration.EscapeSingleQuotedStringContent(StringUtil.Format(ImplicitRemotingStrings.CredentialRequestBody, this.GetConnectionString())),
                    /* 2 */ CodeGeneration.EscapeSingleQuotedStringContent(_remoteRunspaceInfo.Runspace.ConnectionInfo.Credential.UserName),
                    /* 3 */ CodeGeneration.EscapeSingleQuotedStringContent(_remoteRunspaceInfo.ComputerName));
            }
        }

        private const string CertificateThumbprintParameterTemplate = "-CertificateThumbprint '{0}'";

        private string GenerateCertificateThumbprintParameter()
        {
            if (_remoteRunspaceInfo.Runspace.ConnectionInfo.CertificateThumbprint == null)
            {
                return string.Empty;
            }
            else
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    CertificateThumbprintParameterTemplate,
                    CodeGeneration.EscapeSingleQuotedStringContent(_remoteRunspaceInfo.Runspace.ConnectionInfo.CertificateThumbprint));
            }
        }

        #endregion

        #region Get-ClientSideParameters

        private const string HelperFunctionsModifyParameters = @"
function Modify-PSImplicitRemotingParameters
{
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [hashtable]
        $clientSideParameters,

        [Parameter(Mandatory = $true, Position = 1)]
        $PSBoundParameters,

        [Parameter(Mandatory = $true, Position = 2)]
        [string]
        $parameterName,

        [Parameter()]
        [switch]
        $leaveAsRemoteParameter)

    if ($PSBoundParameters.ContainsKey($parameterName))
    {
        $clientSideParameters.Add($parameterName, $PSBoundParameters[$parameterName])
        if (-not $leaveAsRemoteParameter) {
            $null = $PSBoundParameters.Remove($parameterName)
        }
    }
}

function Get-PSImplicitRemotingClientSideParameters
{
    param(
        [Parameter(Mandatory = $true, Position = 1)]
        $PSBoundParameters,

        [Parameter(Mandatory = $true, Position = 2)]
        $proxyForCmdlet)

    $clientSideParameters = @{}

    $parametersToLeaveRemote = 'ErrorAction', 'WarningAction', 'InformationAction', 'ProgressAction'

    Modify-PSImplicitRemotingParameters $clientSideParameters $PSBoundParameters 'AsJob'
    if ($proxyForCmdlet)
    {
        foreach($parameter in [System.Management.Automation.Cmdlet]::CommonParameters)
        {
            if($parametersToLeaveRemote -contains $parameter)
            {
                Modify-PSImplicitRemotingParameters $clientSideParameters $PSBoundParameters $parameter -LeaveAsRemoteParameter
            }
            else
            {
                Modify-PSImplicitRemotingParameters $clientSideParameters $PSBoundParameters $parameter
            }
        }
    }

    return $clientSideParameters
}
";

        private static void GenerateHelperFunctionsClientSideParameters(TextWriter writer)
        {
            if (writer == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(writer));
            }

            writer.Write(HelperFunctionsModifyParameters);
        }

        #endregion

        private void GenerateHelperFunctions(TextWriter writer)
        {
            GenerateSectionSeparator(writer);
            GenerateHelperFunctionsWriteMessage(writer);
            this.GenerateHelperFunctionsGetSessionOption(writer);
            GenerateHelperFunctionsSetImplicitRunspace(writer);
            this.GenerateHelperFunctionsGetImplicitRunspace(writer);
            GenerateHelperFunctionsClientSideParameters(writer);
        }

        #endregion

        #region Generating proxy commands

        // index 0 - name of the command escaped for inclusion inside a single-quoted string
        // index 1 - name of the command escaped for help comment
        // index 2 - not used
        // index 3 - param declaration
        // index 4 - not used
        // index 5 - remote help category
        // index 6 - process block
        // index 7 - end block
        // index 8 - whether or not this is a proxy for a cmdlet-bound command (i.e. should common parameters get special handling)
        private const string CommandProxyTemplate = @"
& $script:SetItem 'function:script:{0}' `
{{
    param(
    {3})

    Begin {{
        try {{
            $positionalArguments = & $script:NewObject collections.arraylist
            foreach ($parameterName in $PSBoundParameters.BoundPositionally)
            {{
                $null = $positionalArguments.Add( $PSBoundParameters[$parameterName] )
                $null = $PSBoundParameters.Remove($parameterName)
            }}

            $positionalArguments.AddRange($args)

            $clientSideParameters = Get-PSImplicitRemotingClientSideParameters $PSBoundParameters ${8}

            $scriptCmd = {{
                & $script:InvokeCommand `
                    @clientSideParameters `
                    -HideComputerName `
                    -Session (Get-PSImplicitRemotingSession -CommandName '{0}') `
                    -Arg ('{0}', $PSBoundParameters, $positionalArguments) `
                    -Script {{ param($name, $boundParams, $unboundParams) & $name @boundParams @unboundParams }} `
            }}

            $steppablePipeline = $scriptCmd.GetSteppablePipeline($myInvocation.CommandOrigin)
            $steppablePipeline.Begin($myInvocation.ExpectingInput, $ExecutionContext)
        }} catch {{
            throw
        }}
    }}

    Process {{ {6} }}

    End {{ {7} }}

    # .ForwardHelpTargetName {1}
    # .ForwardHelpCategory {5}
    # .RemoteHelpRunspace PSSession
}}
        ";

        private static void GenerateCommandProxy(TextWriter writer, CommandMetadata commandMetadata)
        {
            if (writer == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(writer));
            }

            string functionNameForString = CodeGeneration.EscapeSingleQuotedStringContent(commandMetadata.Name);
            string functionNameForHelp = EscapeFunctionNameForRemoteHelp(commandMetadata.Name);
            writer.Write(
                CommandProxyTemplate,
                /* 0 */ functionNameForString,
                /* 1 */ functionNameForHelp,
                /* 2 */ commandMetadata.GetDecl(),
                /* 3 */ commandMetadata.GetParamBlock(),
                /* 4 */ null /* not used */,
                /* 5 */ commandMetadata.WrappedCommandType,
                /* 6 */ ProxyCommand.GetProcess(commandMetadata),
                /* 7 */ ProxyCommand.GetEnd(commandMetadata),
                /* 8 */ commandMetadata.WrappedAnyCmdlet);
        }

        private static void GenerateCommandProxy(TextWriter writer, IEnumerable<CommandMetadata> listOfCommandMetadata)
        {
            if (writer == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(writer));
            }

            if (listOfCommandMetadata == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(listOfCommandMetadata));
            }

            GenerateSectionSeparator(writer);
            foreach (CommandMetadata commandMetadata in listOfCommandMetadata)
            {
                GenerateCommandProxy(writer, commandMetadata);
            }
        }

        #endregion

        #region Generating export declaration of a proxy module

        private const string ExportFunctionsTemplate = @"
& $script:ExportModuleMember -Function {0}
        ";

        private static void GenerateExportDeclaration(TextWriter writer, IEnumerable<CommandMetadata> listOfCommandMetadata)
        {
            if (writer == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(writer));
            }

            if (listOfCommandMetadata == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(listOfCommandMetadata));
            }

            GenerateSectionSeparator(writer);

            List<string> listOfCommandNames = GetListOfCommandNames(listOfCommandMetadata);
            string exportString = GenerateArrayString(listOfCommandNames);
            writer.Write(ExportFunctionsTemplate, exportString);
        }

        private static List<string> GetListOfCommandNames(IEnumerable<CommandMetadata> listOfCommandMetadata)
        {
            if (listOfCommandMetadata == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(listOfCommandMetadata));
            }

            List<string> listOfCommandNames = new();
            foreach (CommandMetadata commandMetadata in listOfCommandMetadata)
            {
                listOfCommandNames.Add(commandMetadata.Name);
            }

            return listOfCommandNames;
        }

        private static string GenerateArrayString(IEnumerable<string> listOfStrings)
        {
            if (listOfStrings == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(listOfStrings));
            }

            StringBuilder arrayString = new();
            foreach (string s in listOfStrings)
            {
                if (arrayString.Length != 0)
                {
                    arrayString.Append(", ");
                }

                arrayString.Append('\'');
                arrayString.Append(CodeGeneration.EscapeSingleQuotedStringContent(s));
                arrayString.Append('\'');
            }

            arrayString.Insert(0, "@(");
            arrayString.Append(')');

            return arrayString.ToString();
        }

        #endregion

        #region Generating aliases

        private const string SetAliasTemplate = @"
& $script:SetAlias -Name '{0}' -Value '{1}' -Force -Scope script
        ";

        private const string ExportAliasesTemplate = @"
& $script:ExportModuleMember -Alias {0}
        ";

        private static void GenerateAliases(TextWriter writer, Dictionary<string, string> alias2resolvedCommandName)
        {
            GenerateSectionSeparator(writer);

            foreach (KeyValuePair<string, string> pair in alias2resolvedCommandName)
            {
                string aliasName = pair.Key;
                string resolvedCommandName = pair.Value;

                writer.Write(
                    SetAliasTemplate,
                    CodeGeneration.EscapeSingleQuotedStringContent(aliasName),
                    CodeGeneration.EscapeSingleQuotedStringContent(resolvedCommandName));
            }

            string exportString = GenerateArrayString(alias2resolvedCommandName.Keys);
            writer.Write(ExportAliasesTemplate, exportString);
        }

        #endregion

        #region Generating format.ps1xml file

        private static void GenerateFormatFile(TextWriter writer, List<ExtendedTypeDefinition> listOfFormatData)
        {
            if (writer == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(writer));
            }

            if (listOfFormatData == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(listOfFormatData));
            }

            XmlWriterSettings settings = new();
            settings.CloseOutput = false;
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.Encoding = writer.Encoding;
            settings.Indent = true;
            using (XmlWriter xmlWriter = XmlWriter.Create(writer, settings))
            {
                FormatXmlWriter.WriteToXml(xmlWriter, listOfFormatData, false);
            }
        }

        #endregion

        /// <summary>
        /// Generates a proxy module in the given directory.
        /// </summary>
        /// <param name="moduleRootDirectory">Base directory for the module.</param>
        /// <param name="fileNamePrefix">Filename prefix for module files.</param>
        /// <param name="encoding">Encoding of generated files.</param>
        /// <param name="force">Whether to overwrite files.</param>
        /// <param name="listOfCommandMetadata">Remote commands to generate proxies for.</param>
        /// <param name="alias2resolvedCommandName">Dictionary mapping alias names to resolved command names.</param>
        /// <param name="listOfFormatData">Remote format data to generate format.ps1xml for.</param>
        /// <param name="certificate">Certificate with which to sign the format files.</param>
        /// <returns>Path to the created files.</returns>
        internal List<string> GenerateProxyModule(
            DirectoryInfo moduleRootDirectory,
            string fileNamePrefix,
            Encoding encoding,
            bool force,
            List<CommandMetadata> listOfCommandMetadata,
            Dictionary<string, string> alias2resolvedCommandName,
            List<ExtendedTypeDefinition> listOfFormatData,
            X509Certificate2 certificate)
        {
            List<string> result = new();

            Dbg.Assert(moduleRootDirectory != null, "Caller should validate moduleRootDirectory != null");
            Dbg.Assert(Directory.Exists(moduleRootDirectory.FullName), "Caller should validate moduleRootDirectory exists");
            Dbg.Assert(encoding != null, "Caller should validate encoding != null");

            string baseName = Path.Combine(moduleRootDirectory.FullName, fileNamePrefix);
            FileMode fileMode = force ? FileMode.OpenOrCreate : FileMode.CreateNew;

            result.Add(baseName + ".psm1");
            FileStream psm1 = new(
                baseName + ".psm1",
                fileMode,
                FileAccess.Write,
                FileShare.None);
            using (TextWriter writer = new StreamWriter(psm1, encoding))
            {
                listOfCommandMetadata ??= new List<CommandMetadata>();

                GenerateModuleHeader(writer);
                GenerateHelperFunctions(writer);
                GenerateCommandProxy(writer, listOfCommandMetadata);
                GenerateExportDeclaration(writer, listOfCommandMetadata);
                GenerateAliases(writer, alias2resolvedCommandName);
                psm1.SetLength(psm1.Position);
            }

            result.Add(baseName + ".format.ps1xml");
            FileStream formatPs1xml = new(
                baseName + ".format.ps1xml",
                fileMode,
                FileAccess.Write,
                FileShare.None);
            using (TextWriter writer = new StreamWriter(formatPs1xml, encoding))
            {
                listOfFormatData ??= new List<ExtendedTypeDefinition>();

                GenerateFormatFile(writer, listOfFormatData);
                formatPs1xml.SetLength(formatPs1xml.Position);
            }
            // Sign psm1 file and format file
            // If certificate is passed, sign the file
            // If certificate is not passed and executionPolicy is set to Restricted/AllSigned, output error
            // Since we will anyway be erroring out during Import-Module, it is better to fail fast
            ExecutionPolicy executionPolicy = SecuritySupport.GetExecutionPolicy(Utils.DefaultPowerShellShellID);
            if (executionPolicy == ExecutionPolicy.Restricted || executionPolicy == ExecutionPolicy.AllSigned)
            {
                if (certificate == null)
                {
                    string message = ImplicitRemotingStrings.CertificateNeeded;
                    throw new PSInvalidOperationException(message);
                }
                else
                {
                    string currentFile = baseName + ".psm1";
                    try
                    {
                        SignatureHelper.SignFile(SigningOption.Default, currentFile, certificate, string.Empty, null);
                        currentFile = baseName + ".format.ps1xml";
                        SignatureHelper.SignFile(SigningOption.Default, currentFile, certificate, string.Empty, null);
                    }
                    catch (Exception e)
                    {
                        string message = StringUtil.Format(ImplicitRemotingStrings.InvalidSigningOperation, currentFile);
                        throw new PSInvalidOperationException(message, e);
                    }
                }
            }

            result.Add(baseName + ".psd1");
            FileInfo manifestFile = new(baseName + ".psd1");
            FileStream psd1 = new(
                manifestFile.FullName,
                fileMode,
                FileAccess.Write,
                FileShare.None);
            using (TextWriter writer = new StreamWriter(psd1, encoding))
            {
                GenerateManifest(writer, baseName + ".psm1", baseName + ".format.ps1xml");
                psd1.SetLength(psd1.Position);
            }

            PSPrimitiveDictionary applicationArguments = GetApplicationArguments();
            if (applicationArguments != null)
            {
                string applicationArgumentsFile = Path.Combine(moduleRootDirectory.FullName, "ApplicationArguments.xml");
                result.Add(applicationArgumentsFile);

                using (var stream = new FileStream(applicationArgumentsFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var xmlWriter = XmlWriter.Create(stream))
                {
                    Serializer serializer = new(xmlWriter);
                    serializer.Serialize(applicationArguments);
                    serializer.Done();
                }
            }

            return result;
        }
    }
}
