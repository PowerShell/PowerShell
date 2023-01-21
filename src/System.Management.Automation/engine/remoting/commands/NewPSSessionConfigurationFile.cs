// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// New-PSSessionConfigurationFile command implementation
    ///
    /// See Declarative Initial Session State (DISC)
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PSSessionConfigurationFile", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096791")]
    public class NewPSSessionConfigurationFileCommand : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// Destination path.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Path
        {
            get
            {
                return _path;
            }

            set
            {
                _path = value;
            }
        }

        private string _path;

        /// <summary>
        /// Configuration file schema version.
        /// </summary>
        [Parameter()]
        [ValidateNotNull]
        public Version SchemaVersion
        {
            get
            {
                return _schemaVersion;
            }

            set
            {
                _schemaVersion = value;
            }
        }

        private Version _schemaVersion = new Version("2.0.0.0");

        /// <summary>
        /// Configuration file GUID.
        /// </summary>
        [Parameter()]
        public Guid Guid
        {
            get
            {
                return _guid;
            }

            set
            {
                _guid = value;
            }
        }

        private Guid _guid = Guid.NewGuid();

        /// <summary>
        /// Author of the configuration file.
        /// </summary>
        [Parameter()]
        public string Author
        {
            get
            {
                return _author;
            }

            set
            {
                _author = value;
            }
        }

        private string _author;

        /// <summary>
        /// Description.
        /// </summary>
        [Parameter()]
        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                _description = value;
            }
        }

        private string _description;

        /// <summary>
        /// Company name.
        /// </summary>
        [Parameter()]
        public string CompanyName
        {
            get
            {
                return _companyName;
            }

            set
            {
                _companyName = value;
            }
        }

        private string _companyName;

        /// <summary>
        /// Copyright information.
        /// </summary>
        [Parameter()]
        public string Copyright
        {
            get
            {
                return _copyright;
            }

            set
            {
                _copyright = value;
            }
        }

        private string _copyright;

        /// <summary>
        /// Specifies type of initial session state to use.
        /// </summary>
        [Parameter()]
        public SessionType SessionType
        {
            get
            {
                return _sessionType;
            }

            set
            {
                _sessionType = value;
            }
        }

        private SessionType _sessionType = SessionType.Default;

        /// <summary>
        /// Specifies the directory for transcripts to be placed.
        /// </summary>
        [Parameter()]
        public string TranscriptDirectory
        {
            get
            {
                return _transcriptDirectory;
            }

            set
            {
                _transcriptDirectory = value;
            }
        }

        private string _transcriptDirectory = null;

        /// <summary>
        /// Specifies whether to run this configuration under a virtual account.
        /// </summary>
        [Parameter()]
        public SwitchParameter RunAsVirtualAccount { get; set; }

        /// <summary>
        /// Specifies groups a virtual account is part of.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] RunAsVirtualAccountGroups { get; set; }

        /// <summary>
        /// Creates a User PSDrive in the session.
        /// The User drive is used with Copy-Item for file transfer when the FileSystem provider is
        /// not visible in the session.
        /// </summary>
        [Parameter()]
        public SwitchParameter MountUserDrive
        {
            get;
            set;
        }

        /// <summary>
        /// Optional parameter that specifies a maximum size in bytes for the User: drive created with the
        /// MountUserDrive parameter.
        /// If no maximum size is specified then the default drive maximum size is 50MB.
        /// </summary>
        [Parameter()]
        public long UserDriveMaximumSize { get; set; }

        // Temporarily removed until script input parameter validation is implemented.
        /*
        /// <summary>
        /// Optional parameter that enforces script input parameter validation.  When specified all scripts
        /// run in the PSSession must have validation attributes to validate input data or an error is generated.
        /// If a MountUserDrive is specified for the PSSession then input parameter validation will be
        /// enabled automatically.
        /// </summary>
        [Parameter()]
        public SwitchParameter EnforceInputParameterValidation { get; set; }
        */

        /// <summary>
        /// Optional parameter that specifies a Group Managed Service Account name in which the configuration
        /// is run.
        /// </summary>
        [Parameter()]
        public string GroupManagedServiceAccount { get; set; }

        /// <summary>
        /// Scripts to process.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ScriptsToProcess
        {
            get
            {
                return _scriptsToProcess;
            }

            set
            {
                _scriptsToProcess = value;
            }
        }

        private string[] _scriptsToProcess = Array.Empty<string>();

        /// <summary>
        /// Role definitions for this session configuration (Role name -> Role capability)
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IDictionary RoleDefinitions
        {
            get
            {
                return _roleDefinitions;
            }

            set
            {
                _roleDefinitions = value;
            }
        }

        private IDictionary _roleDefinitions;

        /// <summary>
        /// Specifies account groups that are membership requirements for this session.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IDictionary RequiredGroups
        {
            get { return _requiredGroups; }

            set { _requiredGroups = value; }
        }

        private IDictionary _requiredGroups;

        /// <summary>
        /// Language mode.
        /// </summary>
        [Parameter()]
        public PSLanguageMode LanguageMode
        {
            get
            {
                return _languageMode;
            }

            set
            {
                _languageMode = value;
                _isLanguageModeSpecified = true;
            }
        }

        private PSLanguageMode _languageMode = PSLanguageMode.NoLanguage;
        private bool _isLanguageModeSpecified;

        /// <summary>
        /// Execution policy.
        /// </summary>
        [Parameter()]
        public ExecutionPolicy ExecutionPolicy
        {
            get
            {
                return _executionPolicy;
            }

            set
            {
                _executionPolicy = value;
            }
        }

        private ExecutionPolicy _executionPolicy = ExecutionPolicy.Restricted;

        /// <summary>
        /// PowerShell version.
        /// </summary>
        [Parameter()]
        public Version PowerShellVersion
        {
            get
            {
                return _powerShellVersion;
            }

            set
            {
                _powerShellVersion = value;
            }
        }

        private Version _powerShellVersion;

        /// <summary>
        /// A list of modules to import.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] ModulesToImport
        {
            get
            {
                return _modulesToImport;
            }

            set
            {
                _modulesToImport = value;
            }
        }

        private object[] _modulesToImport;

        /// <summary>
        /// A list of visible aliases.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] VisibleAliases
        {
            get
            {
                return _visibleAliases;
            }

            set
            {
                _visibleAliases = value;
            }
        }

        private string[] _visibleAliases = Array.Empty<string>();

        /// <summary>
        /// A list of visible cmdlets.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] VisibleCmdlets
        {
            get
            {
                return _visibleCmdlets;
            }

            set
            {
                _visibleCmdlets = value;
            }
        }

        private object[] _visibleCmdlets = null;

        /// <summary>
        /// A list of visible functions.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] VisibleFunctions
        {
            get
            {
                return _visibleFunctions;
            }

            set
            {
                _visibleFunctions = value;
            }
        }

        private object[] _visibleFunctions = null;

        /// <summary>
        /// A list of visible external commands (scripts and applications)
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] VisibleExternalCommands
        {
            get
            {
                return _visibleExternalCommands;
            }

            set
            {
                _visibleExternalCommands = value;
            }
        }

        private string[] _visibleExternalCommands = Array.Empty<string>();

        /// <summary>
        /// A list of providers.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] VisibleProviders
        {
            get
            {
                return _visibleProviders;
            }

            set
            {
                _visibleProviders = value;
            }
        }

        private string[] _visibleProviders = Array.Empty<string>();

        /// <summary>
        /// A list of aliases.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public IDictionary[] AliasDefinitions
        {
            get
            {
                return _aliasDefinitions;
            }

            set
            {
                _aliasDefinitions = value;
            }
        }

        private IDictionary[] _aliasDefinitions;

        /// <summary>
        /// A list of functions.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public IDictionary[] FunctionDefinitions
        {
            get
            {
                return _functionDefinitions;
            }

            set
            {
                _functionDefinitions = value;
            }
        }

        private IDictionary[] _functionDefinitions;

        /// <summary>
        /// A list of variables.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object VariableDefinitions
        {
            get
            {
                return _variableDefinitions;
            }

            set
            {
                _variableDefinitions = value;
            }
        }

        private object _variableDefinitions;

        /// <summary>
        /// A list of environment variables.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IDictionary EnvironmentVariables
        {
            get
            {
                return _environmentVariables;
            }

            set
            {
                _environmentVariables = value;
            }
        }

        private IDictionary _environmentVariables;

        /// <summary>
        /// A list of types to process.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] TypesToProcess
        {
            get
            {
                return _typesToProcess;
            }

            set
            {
                _typesToProcess = value;
            }
        }

        private string[] _typesToProcess = Array.Empty<string>();

        /// <summary>
        /// A list of format data to process.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] FormatsToProcess
        {
            get
            {
                return _formatsToProcess;
            }

            set
            {
                _formatsToProcess = value;
            }
        }

        private string[] _formatsToProcess = Array.Empty<string>();

        /// <summary>
        /// A list of assemblies to load.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] AssembliesToLoad
        {
            get
            {
                return _assembliesToLoad;
            }

            set
            {
                _assembliesToLoad = value;
            }
        }

        private string[] _assembliesToLoad;

        /// <summary>
        /// Gets or sets whether to include a full expansion of all possible session configuration
        /// keys as comments when creating the session configuration file.
        /// </summary>
        [Parameter()]
        public SwitchParameter Full { get; set; }

        #endregion

        #region Overrides

        /// <summary>
        /// </summary>
        protected override void ProcessRecord()
        {
            Debug.Assert(!string.IsNullOrEmpty(_path));

            ProviderInfo provider = null;
            PSDriveInfo drive;
            string filePath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(_path, out provider, out drive);

            if (!provider.NameEquals(Context.ProviderNames.FileSystem) || !filePath.EndsWith(StringLiterals.PowerShellDISCFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                string message = StringUtil.Format(RemotingErrorIdStrings.InvalidPSSessionConfigurationFilePath, _path);
                InvalidOperationException ioe = new InvalidOperationException(message);
                ErrorRecord er = new ErrorRecord(ioe, "InvalidPSSessionConfigurationFilePath",
                    ErrorCategory.InvalidArgument, _path);
                ThrowTerminatingError(er);
            }

            FileStream fileStream;
            StreamWriter streamWriter;
            FileInfo readOnlyFileInfo;

            // Now open the output file...
            PathUtils.MasterStreamOpen(
                this,
                filePath,
                EncodingConversion.Unicode,
                /* defaultEncoding */ false,
                /* Append */ false,
                /* Force */ false,
                /* NoClobber */ false,
                out fileStream,
                out streamWriter,
                out readOnlyFileInfo,
                false
            );

            try
            {
                StringBuilder result = new StringBuilder();

                result.Append("@{");
                result.Append(streamWriter.NewLine);
                result.Append(streamWriter.NewLine);

                // Schema version
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.SchemaVersion, RemotingErrorIdStrings.DISCSchemaVersionComment,
                    SessionConfigurationUtils.QuoteName(_schemaVersion), streamWriter, false));

                // Guid
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Guid, RemotingErrorIdStrings.DISCGUIDComment, SessionConfigurationUtils.QuoteName(_guid), streamWriter, false));

                // Author
                if (string.IsNullOrEmpty(_author))
                {
                    _author = Environment.UserName;
                }

                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Author, RemotingErrorIdStrings.DISCAuthorComment,
                    SessionConfigurationUtils.QuoteName(_author), streamWriter, false));

                // Description
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Description, RemotingErrorIdStrings.DISCDescriptionComment,
                    SessionConfigurationUtils.QuoteName(_description), streamWriter, string.IsNullOrEmpty(_description)));

                // Company name
                if (ShouldGenerateConfigurationSnippet("CompanyName"))
                {
                    if (string.IsNullOrEmpty(_companyName))
                    {
                        _companyName = Modules.DefaultCompanyName;
                    }

                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.CompanyName, RemotingErrorIdStrings.DISCCompanyNameComment,
                        SessionConfigurationUtils.QuoteName(_companyName), streamWriter, false));
                }

                // Copyright
                if (ShouldGenerateConfigurationSnippet("Copyright"))
                {
                    if (string.IsNullOrEmpty(_copyright))
                    {
                        _copyright = StringUtil.Format(Modules.DefaultCopyrightMessage, _author);
                    }

                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Copyright, RemotingErrorIdStrings.DISCCopyrightComment,
                        SessionConfigurationUtils.QuoteName(_copyright), streamWriter, false));
                }

                // Session type
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.SessionType, RemotingErrorIdStrings.DISCInitialSessionStateComment,
                    SessionConfigurationUtils.QuoteName(_sessionType), streamWriter, false));

                string resultData = null;

                // Transcript directory
                resultData = string.IsNullOrEmpty(_transcriptDirectory) ? "'C:\\Transcripts\\'" : SessionConfigurationUtils.QuoteName(_transcriptDirectory);
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.TranscriptDirectory, RemotingErrorIdStrings.DISCTranscriptDirectoryComment,
                    resultData, streamWriter, string.IsNullOrEmpty(_transcriptDirectory)));

                // Run as virtual account
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.RunAsVirtualAccount, RemotingErrorIdStrings.DISCRunAsVirtualAccountComment,
                    SessionConfigurationUtils.WriteBoolean(true), streamWriter, RunAsVirtualAccount == false));

                // Run as virtual account groups
                if (ShouldGenerateConfigurationSnippet("RunAsVirtualAccountGroups"))
                {
                    bool haveVirtualAccountGroups = (RunAsVirtualAccountGroups != null) && (RunAsVirtualAccountGroups.Length > 0);
                    resultData = (haveVirtualAccountGroups) ? SessionConfigurationUtils.CombineStringArray(RunAsVirtualAccountGroups) : "'Remote Desktop Users', 'Remote Management Users'";
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.RunAsVirtualAccountGroups, RemotingErrorIdStrings.DISCRunAsVirtualAccountGroupsComment,
                        resultData, streamWriter, !haveVirtualAccountGroups));
                }

                // Mount user drive
                if (ShouldGenerateConfigurationSnippet("MountUserDrive"))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.MountUserDrive, RemotingErrorIdStrings.DISCMountUserDriveComment,
                        SessionConfigurationUtils.WriteBoolean(true), streamWriter, MountUserDrive == false));
                }

                // User drive maximum size
                if (ShouldGenerateConfigurationSnippet("UserDriveMaximumSize"))
                {
                    long userDriveMaxSize = (UserDriveMaximumSize > 0) ? UserDriveMaximumSize : 50000000;
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.UserDriveMaxSize, RemotingErrorIdStrings.DISCUserDriveMaxSizeComment,
                        SessionConfigurationUtils.WriteLong(userDriveMaxSize), streamWriter, (UserDriveMaximumSize <= 0)));
                }

                // Temporarily removed until script input parameter validation is implemented.
                /*
                // Enforce input parameter validation
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.EnforceInputParameterValidation, RemotingErrorIdStrings.DISCEnforceInputParameterValidation,
                    SessionConfigurationUtils.WriteBoolean(true), streamWriter, EnforceInputParameterValidation == false));
                */

                // Group Managed Service Account Name
                if (ShouldGenerateConfigurationSnippet("GroupManagedServiceAccount"))
                {
                    bool haveGMSAAccountName = !string.IsNullOrEmpty(GroupManagedServiceAccount);
                    resultData = (!haveGMSAAccountName) ? "'CONTOSO\\GroupManagedServiceAccount'" : SessionConfigurationUtils.QuoteName(GroupManagedServiceAccount);
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.GMSAAccount, RemotingErrorIdStrings.DISCGMSAComment,
                        resultData, streamWriter, !haveGMSAAccountName));
                }

                // Scripts to process
                resultData = (_scriptsToProcess.Length > 0) ? SessionConfigurationUtils.CombineStringArray(_scriptsToProcess) : "'C:\\ConfigData\\InitScript1.ps1', 'C:\\ConfigData\\InitScript2.ps1'";
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ScriptsToProcess, RemotingErrorIdStrings.DISCScriptsToProcessComment,
                    resultData, streamWriter, (_scriptsToProcess.Length == 0)));

                // Role definitions
                if (_roleDefinitions == null)
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.RoleDefinitions, RemotingErrorIdStrings.DISCRoleDefinitionsComment,
                        "@{ 'CONTOSO\\SqlAdmins' = @{ RoleCapabilities = 'SqlAdministration' }; 'CONTOSO\\SqlManaged' = @{ RoleCapabilityFiles = 'C:\\RoleCapability\\SqlManaged.psrc' }; 'CONTOSO\\ServerMonitors' = @{ VisibleCmdlets = 'Get-Process' } } ", streamWriter, true));
                }
                else
                {
                    DISCUtils.ValidateRoleDefinitions(_roleDefinitions);

                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.RoleDefinitions, RemotingErrorIdStrings.DISCRoleDefinitionsComment,
                        SessionConfigurationUtils.CombineHashtable(_roleDefinitions, streamWriter), streamWriter, false));
                }

                // Required groups
                if (ShouldGenerateConfigurationSnippet("RequiredGroups"))
                {
                    if (_requiredGroups == null)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.RequiredGroups, RemotingErrorIdStrings.DISCRequiredGroupsComment,
                            "@{ And = @{ Or = 'CONTOSO\\SmartCard-Logon1', 'CONTOSO\\SmartCard-Logon2' }, 'Administrators' }", streamWriter, true));
                    }
                    else
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.RequiredGroups, RemotingErrorIdStrings.DISCRequiredGroupsComment,
                            SessionConfigurationUtils.CombineRequiredGroupsHash(_requiredGroups), streamWriter, false));
                    }
                }

                // PSLanguageMode languageMode
                if (ShouldGenerateConfigurationSnippet("LanguageMode"))
                {
                    if (!_isLanguageModeSpecified)
                    {
                        if (_sessionType == SessionType.Default)
                        {
                            _languageMode = PSLanguageMode.FullLanguage;
                        }
                    }

                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.LanguageMode, RemotingErrorIdStrings.DISCLanguageModeComment,
                        SessionConfigurationUtils.QuoteName(_languageMode), streamWriter, false));
                }

                // ExecutionPolicy executionPolicy
                if (ShouldGenerateConfigurationSnippet("ExecutionPolicy"))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ExecutionPolicy, RemotingErrorIdStrings.DISCExecutionPolicyComment,
                        SessionConfigurationUtils.QuoteName(_executionPolicy), streamWriter, false));
                }

                // PowerShell version
                bool isExample = false;

                if (ShouldGenerateConfigurationSnippet("PowerShellVersion"))
                {
                    if (_powerShellVersion == null)
                    {
                        isExample = true;
                        _powerShellVersion = PSVersionInfo.PSVersion;
                    }

                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.PowerShellVersion, RemotingErrorIdStrings.DISCPowerShellVersionComment,
                        SessionConfigurationUtils.QuoteName(_powerShellVersion), streamWriter, isExample));
                }

                // Modules to import
                if (_modulesToImport == null)
                {
                    if (Full)
                    {
                        const string exampleModulesToImport = "'MyCustomModule', @{ ModuleName = 'MyCustomModule'; ModuleVersion = '1.0.0.0'; GUID = '4d30d5f0-cb16-4898-812d-f20a6c596bdf' }";
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ModulesToImport, RemotingErrorIdStrings.DISCModulesToImportComment, exampleModulesToImport, streamWriter, true));
                    }
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ModulesToImport, RemotingErrorIdStrings.DISCModulesToImportComment,
                        SessionConfigurationUtils.CombineHashTableOrStringArray(_modulesToImport, streamWriter, this), streamWriter, false));
                }

                // Visible aliases
                if (ShouldGenerateConfigurationSnippet("VisibleAliases"))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleAliases, RemotingErrorIdStrings.DISCVisibleAliasesComment,
                        SessionConfigurationUtils.GetVisibilityDefault(_visibleAliases, streamWriter, this), streamWriter, _visibleAliases.Length == 0));
                }

                // Visible cmdlets
                if ((_visibleCmdlets == null) || (_visibleCmdlets.Length == 0))
                {
                    if (Full)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleCmdlets, RemotingErrorIdStrings.DISCVisibleCmdletsComment,
                            "'Invoke-Cmdlet1', @{ Name = 'Invoke-Cmdlet2'; Parameters = @{ Name = 'Parameter1'; ValidateSet = 'Item1', 'Item2' }, @{ Name = 'Parameter2'; ValidatePattern = 'L*' } }", streamWriter, true));
                    }
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleCmdlets, RemotingErrorIdStrings.DISCVisibleCmdletsComment,
                        SessionConfigurationUtils.GetVisibilityDefault(_visibleCmdlets, streamWriter, this), streamWriter, false));
                }

                // Visible functions
                if ((_visibleFunctions == null) || (_visibleFunctions.Length == 0))
                {
                    if (Full)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleFunctions, RemotingErrorIdStrings.DISCVisibleFunctionsComment,
                            "'Invoke-Function1', @{ Name = 'Invoke-Function2'; Parameters = @{ Name = 'Parameter1'; ValidateSet = 'Item1', 'Item2' }, @{ Name = 'Parameter2'; ValidatePattern = 'L*' } }", streamWriter, true));
                    }
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleFunctions, RemotingErrorIdStrings.DISCVisibleFunctionsComment,
                        SessionConfigurationUtils.GetVisibilityDefault(_visibleFunctions, streamWriter, this), streamWriter, _visibleFunctions.Length == 0));
                }

                // Visible external commands (scripts, executables)
                if (ShouldGenerateConfigurationSnippet("VisibleExternalCommands"))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleExternalCommands, RemotingErrorIdStrings.DISCVisibleExternalCommandsComment,
                        SessionConfigurationUtils.GetVisibilityDefault(_visibleExternalCommands, streamWriter, this), streamWriter, _visibleExternalCommands.Length == 0));
                }

                // Visible providers
                if (ShouldGenerateConfigurationSnippet("VisibleProviders"))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleProviders, RemotingErrorIdStrings.DISCVisibleProvidersComment,
                        SessionConfigurationUtils.GetVisibilityDefault(_visibleProviders, streamWriter, this), streamWriter, _visibleProviders.Length == 0));
                }

                // Alias definitions
                if ((_aliasDefinitions == null) || (_aliasDefinitions.Length == 0))
                {
                    if (Full)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.AliasDefinitions, RemotingErrorIdStrings.DISCAliasDefinitionsComment,
                           "@{ Name = 'Alias1'; Value = 'Invoke-Alias1'}, @{ Name = 'Alias2'; Value = 'Invoke-Alias2'}", streamWriter, true));
                    }
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.AliasDefinitions, RemotingErrorIdStrings.DISCAliasDefinitionsComment,
                        SessionConfigurationUtils.CombineHashtableArray(_aliasDefinitions, streamWriter), streamWriter, false));
                }

                // Function definitions
                if (_functionDefinitions == null)
                {
                    if (Full)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.FunctionDefinitions, RemotingErrorIdStrings.DISCFunctionDefinitionsComment,
                            "@{ Name = 'MyFunction'; ScriptBlock = { param($MyInput) $MyInput } }", streamWriter, true));
                    }
                }
                else
                {
                    Hashtable[] funcHash = DISCPowerShellConfiguration.TryGetHashtableArray(_functionDefinitions);

                    if (funcHash != null)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.FunctionDefinitions, RemotingErrorIdStrings.DISCFunctionDefinitionsComment,
                            SessionConfigurationUtils.CombineHashtableArray(funcHash, streamWriter), streamWriter, false));

                        foreach (Hashtable hashtable in funcHash)
                        {
                            if (!hashtable.ContainsKey(ConfigFileConstants.FunctionNameToken))
                            {
                                PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                    ConfigFileConstants.FunctionDefinitions, ConfigFileConstants.FunctionNameToken, _path));
                                ThrowTerminatingError(e.ErrorRecord);
                            }

                            if (!hashtable.ContainsKey(ConfigFileConstants.FunctionValueToken))
                            {
                                PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                    ConfigFileConstants.FunctionDefinitions, ConfigFileConstants.FunctionValueToken, _path));
                                ThrowTerminatingError(e.ErrorRecord);
                            }

                            if (hashtable[ConfigFileConstants.FunctionValueToken] is not ScriptBlock)
                            {
                                PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCKeyMustBeScriptBlock,
                                    ConfigFileConstants.FunctionValueToken, ConfigFileConstants.FunctionDefinitions, _path));
                                ThrowTerminatingError(e.ErrorRecord);
                            }

                            foreach (string functionKey in hashtable.Keys)
                            {
                                if (!string.Equals(functionKey, ConfigFileConstants.FunctionNameToken, StringComparison.OrdinalIgnoreCase) &&
                                    !string.Equals(functionKey, ConfigFileConstants.FunctionValueToken, StringComparison.OrdinalIgnoreCase) &&
                                    !string.Equals(functionKey, ConfigFileConstants.FunctionOptionsToken, StringComparison.OrdinalIgnoreCase))
                                {
                                    PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeContainsInvalidKey,
                                        functionKey, ConfigFileConstants.FunctionDefinitions, _path));
                                    ThrowTerminatingError(e.ErrorRecord);
                                }
                            }
                        }
                    }
                    else
                    {
                        PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustBeHashtableArray,
                            ConfigFileConstants.FunctionDefinitions, filePath));
                        ThrowTerminatingError(e.ErrorRecord);
                    }
                }

                // Variable definitions
                if (_variableDefinitions == null)
                {
                    if (Full)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VariableDefinitions, RemotingErrorIdStrings.DISCVariableDefinitionsComment,
                            "@{ Name = 'Variable1'; Value = { 'Dynamic' + 'InitialValue' } }, @{ Name = 'Variable2'; Value = 'StaticInitialValue' }", streamWriter, true));
                    }
                }
                else
                {
                    string varString = _variableDefinitions as string;

                    if (varString != null)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VariableDefinitions, RemotingErrorIdStrings.DISCVariableDefinitionsComment,
                            varString, streamWriter, false));
                    }
                    else
                    {
                        Hashtable[] varHash = DISCPowerShellConfiguration.TryGetHashtableArray(_variableDefinitions);

                        if (varHash != null)
                        {
                            result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VariableDefinitions, RemotingErrorIdStrings.DISCVariableDefinitionsComment,
                                SessionConfigurationUtils.CombineHashtableArray(varHash, streamWriter), streamWriter, false));

                            foreach (Hashtable hashtable in varHash)
                            {
                                if (!hashtable.ContainsKey(ConfigFileConstants.VariableNameToken))
                                {
                                    PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                        ConfigFileConstants.VariableDefinitions, ConfigFileConstants.VariableNameToken, _path));
                                    ThrowTerminatingError(e.ErrorRecord);
                                }

                                if (!hashtable.ContainsKey(ConfigFileConstants.VariableValueToken))
                                {
                                    PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                        ConfigFileConstants.VariableDefinitions, ConfigFileConstants.VariableValueToken, _path));
                                    ThrowTerminatingError(e.ErrorRecord);
                                }

                                foreach (string variableKey in hashtable.Keys)
                                {
                                    if (!string.Equals(variableKey, ConfigFileConstants.VariableNameToken, StringComparison.OrdinalIgnoreCase) &&
                                        !string.Equals(variableKey, ConfigFileConstants.VariableValueToken, StringComparison.OrdinalIgnoreCase))
                                    {
                                        PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeContainsInvalidKey,
                                            variableKey, ConfigFileConstants.VariableDefinitions, _path));
                                        ThrowTerminatingError(e.ErrorRecord);
                                    }
                                }
                            }
                        }
                        else
                        {
                            PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustBeHashtableArray,
                                ConfigFileConstants.VariableDefinitions, filePath));
                            ThrowTerminatingError(e.ErrorRecord);
                        }
                    }
                }

                // Environment variables
                if (_environmentVariables == null)
                {
                    if (Full)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.EnvironmentVariables, RemotingErrorIdStrings.DISCEnvironmentVariablesComment,
                            "@{ Variable1 = 'Value1'; Variable2 = 'Value2' }",
                            streamWriter, true));
                    }
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.EnvironmentVariables, RemotingErrorIdStrings.DISCEnvironmentVariablesComment,
                        SessionConfigurationUtils.CombineHashtable(_environmentVariables, streamWriter), streamWriter, false));
                }

                // Types to process
                if (ShouldGenerateConfigurationSnippet("TypesToProcess"))
                {
                    resultData = (_typesToProcess.Length > 0) ? SessionConfigurationUtils.CombineStringArray(_typesToProcess) : "'C:\\ConfigData\\MyTypes.ps1xml', 'C:\\ConfigData\\OtherTypes.ps1xml'";
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.TypesToProcess, RemotingErrorIdStrings.DISCTypesToProcessComment,
                        resultData, streamWriter, (_typesToProcess.Length == 0)));
                }

                // Formats to process
                if (ShouldGenerateConfigurationSnippet("FormatsToProcess"))
                {
                    resultData = (_formatsToProcess.Length > 0) ? SessionConfigurationUtils.CombineStringArray(_formatsToProcess) : "'C:\\ConfigData\\MyFormats.ps1xml', 'C:\\ConfigData\\OtherFormats.ps1xml'";
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.FormatsToProcess, RemotingErrorIdStrings.DISCFormatsToProcessComment,
                        resultData, streamWriter, (_formatsToProcess.Length == 0)));
                }

                // Assemblies to load
                if (ShouldGenerateConfigurationSnippet("AssembliesToLoad"))
                {
                    isExample = false;
                    if ((_assembliesToLoad == null) || (_assembliesToLoad.Length == 0))
                    {
                        isExample = true;
                        _assembliesToLoad = new string[] { "System.Web", "System.OtherAssembly, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" };
                    }

                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.AssembliesToLoad, RemotingErrorIdStrings.DISCAssembliesToLoadComment,
                        SessionConfigurationUtils.CombineStringArray(_assembliesToLoad), streamWriter, isExample));
                }

                result.Append('}');

                streamWriter.Write(result.ToString());
            }
            finally
            {
                streamWriter.Dispose();
            }
        }

        #endregion

        #region Private methods

        private bool ShouldGenerateConfigurationSnippet(string parameterName)
        {
            return Full || MyInvocation.BoundParameters.ContainsKey(parameterName);
        }

        #endregion
    }

    /// <summary>
    /// New-PSRoleCapabilityFile command implementation
    ///
    /// Creates a role capability file suitable for use in a Role Capability (which can be referenced in a Session Configuration file)
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PSRoleCapabilityFile", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=623708")]
    public class NewPSRoleCapabilityFileCommand : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// Destination path.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Path
        {
            get
            {
                return _path;
            }

            set
            {
                _path = value;
            }
        }

        private string _path;

        /// <summary>
        /// Configuration file GUID.
        /// </summary>
        [Parameter()]
        public Guid Guid
        {
            get
            {
                return _guid;
            }

            set
            {
                _guid = value;
            }
        }

        private Guid _guid = Guid.NewGuid();

        /// <summary>
        /// Author of the configuration file.
        /// </summary>
        [Parameter()]
        public string Author
        {
            get
            {
                return _author;
            }

            set
            {
                _author = value;
            }
        }

        private string _author;

        /// <summary>
        /// Description.
        /// </summary>
        [Parameter()]
        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                _description = value;
            }
        }

        private string _description;

        /// <summary>
        /// Company name.
        /// </summary>
        [Parameter()]
        public string CompanyName
        {
            get
            {
                return _companyName;
            }

            set
            {
                _companyName = value;
            }
        }

        private string _companyName;

        /// <summary>
        /// Copyright information.
        /// </summary>
        [Parameter()]
        public string Copyright
        {
            get
            {
                return _copyright;
            }

            set
            {
                _copyright = value;
            }
        }

        private string _copyright;

        /// <summary>
        /// A list of modules to import.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] ModulesToImport
        {
            get
            {
                return _modulesToImport;
            }

            set
            {
                _modulesToImport = value;
            }
        }

        private object[] _modulesToImport;

        /// <summary>
        /// A list of visible aliases.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] VisibleAliases
        {
            get
            {
                return _visibleAliases;
            }

            set
            {
                _visibleAliases = value;
            }
        }

        private string[] _visibleAliases = Array.Empty<string>();

        /// <summary>
        /// A list of visible cmdlets.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] VisibleCmdlets
        {
            get
            {
                return _visibleCmdlets;
            }

            set
            {
                _visibleCmdlets = value;
            }
        }

        private object[] _visibleCmdlets = null;

        /// <summary>
        /// A list of visible functions.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] VisibleFunctions
        {
            get
            {
                return _visibleFunctions;
            }

            set
            {
                _visibleFunctions = value;
            }
        }

        private object[] _visibleFunctions = null;

        /// <summary>
        /// A list of visible external commands (scripts and applications)
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] VisibleExternalCommands
        {
            get
            {
                return _visibleExternalCommands;
            }

            set
            {
                _visibleExternalCommands = value;
            }
        }

        private string[] _visibleExternalCommands = Array.Empty<string>();

        /// <summary>
        /// A list of providers.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] VisibleProviders
        {
            get
            {
                return _visibleProviders;
            }

            set
            {
                _visibleProviders = value;
            }
        }

        private string[] _visibleProviders = Array.Empty<string>();

        /// <summary>
        /// Scripts to process.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ScriptsToProcess
        {
            get
            {
                return _scriptsToProcess;
            }

            set
            {
                _scriptsToProcess = value;
            }
        }

        private string[] _scriptsToProcess = Array.Empty<string>();

        /// <summary>
        /// A list of aliases.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public IDictionary[] AliasDefinitions
        {
            get
            {
                return _aliasDefinitions;
            }

            set
            {
                _aliasDefinitions = value;
            }
        }

        private IDictionary[] _aliasDefinitions;

        /// <summary>
        /// A list of functions.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public IDictionary[] FunctionDefinitions
        {
            get
            {
                return _functionDefinitions;
            }

            set
            {
                _functionDefinitions = value;
            }
        }

        private IDictionary[] _functionDefinitions;

        /// <summary>
        /// A list of variables.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object VariableDefinitions
        {
            get
            {
                return _variableDefinitions;
            }

            set
            {
                _variableDefinitions = value;
            }
        }

        private object _variableDefinitions;

        /// <summary>
        /// A list of environment variables.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IDictionary EnvironmentVariables
        {
            get
            {
                return _environmentVariables;
            }

            set
            {
                _environmentVariables = value;
            }
        }

        private IDictionary _environmentVariables;

        /// <summary>
        /// A list of types to process.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] TypesToProcess
        {
            get
            {
                return _typesToProcess;
            }

            set
            {
                _typesToProcess = value;
            }
        }

        private string[] _typesToProcess = Array.Empty<string>();

        /// <summary>
        /// A list of format data to process.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] FormatsToProcess
        {
            get
            {
                return _formatsToProcess;
            }

            set
            {
                _formatsToProcess = value;
            }
        }

        private string[] _formatsToProcess = Array.Empty<string>();

        /// <summary>
        /// A list of assemblies to load.
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] AssembliesToLoad
        {
            get
            {
                return _assembliesToLoad;
            }

            set
            {
                _assembliesToLoad = value;
            }
        }

        private string[] _assembliesToLoad;

        #endregion

        #region Overrides

        /// <summary>
        /// </summary>
        protected override void ProcessRecord()
        {
            Debug.Assert(!string.IsNullOrEmpty(_path));

            ProviderInfo provider = null;
            PSDriveInfo drive;
            string filePath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(_path, out provider, out drive);

            if (!provider.NameEquals(Context.ProviderNames.FileSystem) || !filePath.EndsWith(StringLiterals.PowerShellRoleCapabilityFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                string message = StringUtil.Format(RemotingErrorIdStrings.InvalidRoleCapabilityFilePath, _path);
                InvalidOperationException ioe = new InvalidOperationException(message);
                ErrorRecord er = new ErrorRecord(ioe, "InvalidRoleCapabilityFilePath",
                    ErrorCategory.InvalidArgument, _path);
                ThrowTerminatingError(er);
            }

            FileStream fileStream;
            StreamWriter streamWriter;
            FileInfo readOnlyFileInfo;

            // Now open the output file...
            PathUtils.MasterStreamOpen(
                this,
                filePath,
                EncodingConversion.Unicode,
                /* defaultEncoding */ false,
                /* Append */ false,
                /* Force */ false,
                /* NoClobber */ false,
                out fileStream,
                out streamWriter,
                out readOnlyFileInfo,
                false
            );

            try
            {
                StringBuilder result = new StringBuilder();

                result.Append("@{");
                result.Append(streamWriter.NewLine);
                result.Append(streamWriter.NewLine);

                // Guid
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Guid, RemotingErrorIdStrings.DISCGUIDComment, SessionConfigurationUtils.QuoteName(_guid), streamWriter, false));

                // Author
                if (string.IsNullOrEmpty(_author))
                {
                    _author = Environment.UserName;
                }

                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Author, RemotingErrorIdStrings.DISCAuthorComment,
                    SessionConfigurationUtils.QuoteName(_author), streamWriter, false));

                // Description
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Description, RemotingErrorIdStrings.DISCDescriptionComment,
                    SessionConfigurationUtils.QuoteName(_description), streamWriter, string.IsNullOrEmpty(_description)));

                // Company name
                if (string.IsNullOrEmpty(_companyName))
                {
                    _companyName = Modules.DefaultCompanyName;
                }

                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.CompanyName, RemotingErrorIdStrings.DISCCompanyNameComment,
                    SessionConfigurationUtils.QuoteName(_companyName), streamWriter, false));

                // Copyright
                if (string.IsNullOrEmpty(_copyright))
                {
                    _copyright = StringUtil.Format(Modules.DefaultCopyrightMessage, _author);
                }

                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Copyright, RemotingErrorIdStrings.DISCCopyrightComment,
                    SessionConfigurationUtils.QuoteName(_copyright), streamWriter, false));

                // Modules to import
                if (_modulesToImport == null)
                {
                    const string exampleModulesToImport = "'MyCustomModule', @{ ModuleName = 'MyCustomModule'; ModuleVersion = '1.0.0.0'; GUID = '4d30d5f0-cb16-4898-812d-f20a6c596bdf' }";
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ModulesToImport, RemotingErrorIdStrings.DISCModulesToImportComment, exampleModulesToImport, streamWriter, true));
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ModulesToImport, RemotingErrorIdStrings.DISCModulesToImportComment,
                        SessionConfigurationUtils.CombineHashTableOrStringArray(_modulesToImport, streamWriter, this), streamWriter, false));
                }

                // Visible aliases
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleAliases, RemotingErrorIdStrings.DISCVisibleAliasesComment,
                    SessionConfigurationUtils.GetVisibilityDefault(_visibleAliases, streamWriter, this), streamWriter, _visibleAliases.Length == 0));

                // Visible cmdlets
                if ((_visibleCmdlets == null) || (_visibleCmdlets.Length == 0))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleCmdlets, RemotingErrorIdStrings.DISCVisibleCmdletsComment,
                        "'Invoke-Cmdlet1', @{ Name = 'Invoke-Cmdlet2'; Parameters = @{ Name = 'Parameter1'; ValidateSet = 'Item1', 'Item2' }, @{ Name = 'Parameter2'; ValidatePattern = 'L*' } }", streamWriter, true));
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleCmdlets, RemotingErrorIdStrings.DISCVisibleCmdletsComment,
                        SessionConfigurationUtils.GetVisibilityDefault(_visibleCmdlets, streamWriter, this), streamWriter, false));
                }

                // Visible functions
                if ((_visibleFunctions == null) || (_visibleFunctions.Length == 0))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleFunctions, RemotingErrorIdStrings.DISCVisibleFunctionsComment,
                        "'Invoke-Function1', @{ Name = 'Invoke-Function2'; Parameters = @{ Name = 'Parameter1'; ValidateSet = 'Item1', 'Item2' }, @{ Name = 'Parameter2'; ValidatePattern = 'L*' } }", streamWriter, true));
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleFunctions, RemotingErrorIdStrings.DISCVisibleFunctionsComment,
                        SessionConfigurationUtils.GetVisibilityDefault(_visibleFunctions, streamWriter, this), streamWriter, _visibleFunctions.Length == 0));
                }

                // Visible external commands (scripts, executables)
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleExternalCommands, RemotingErrorIdStrings.DISCVisibleExternalCommandsComment,
                    SessionConfigurationUtils.GetVisibilityDefault(_visibleExternalCommands, streamWriter, this), streamWriter, _visibleExternalCommands.Length == 0));

                // Visible providers
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleProviders, RemotingErrorIdStrings.DISCVisibleProvidersComment,
                    SessionConfigurationUtils.GetVisibilityDefault(_visibleProviders, streamWriter, this), streamWriter, _visibleProviders.Length == 0));

                // Scripts to process
                string resultData = (_scriptsToProcess.Length > 0) ? SessionConfigurationUtils.CombineStringArray(_scriptsToProcess) : "'C:\\ConfigData\\InitScript1.ps1', 'C:\\ConfigData\\InitScript2.ps1'";
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ScriptsToProcess, RemotingErrorIdStrings.DISCScriptsToProcessComment,
                    resultData, streamWriter, (_scriptsToProcess.Length == 0)));

                // Alias definitions
                if ((_aliasDefinitions == null) || (_aliasDefinitions.Length == 0))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.AliasDefinitions, RemotingErrorIdStrings.DISCAliasDefinitionsComment,
                       "@{ Name = 'Alias1'; Value = 'Invoke-Alias1'}, @{ Name = 'Alias2'; Value = 'Invoke-Alias2'}", streamWriter, true));
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.AliasDefinitions, RemotingErrorIdStrings.DISCAliasDefinitionsComment,
                        SessionConfigurationUtils.CombineHashtableArray(_aliasDefinitions, streamWriter), streamWriter, false));
                }

                // Function definitions
                if (_functionDefinitions == null)
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.FunctionDefinitions, RemotingErrorIdStrings.DISCFunctionDefinitionsComment,
                        "@{ Name = 'MyFunction'; ScriptBlock = { param($MyInput) $MyInput } }", streamWriter, true));
                }
                else
                {
                    Hashtable[] funcHash = DISCPowerShellConfiguration.TryGetHashtableArray(_functionDefinitions);

                    if (funcHash != null)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.FunctionDefinitions, RemotingErrorIdStrings.DISCFunctionDefinitionsComment,
                            SessionConfigurationUtils.CombineHashtableArray(funcHash, streamWriter), streamWriter, false));

                        foreach (Hashtable hashtable in funcHash)
                        {
                            if (!hashtable.ContainsKey(ConfigFileConstants.FunctionNameToken))
                            {
                                PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                    ConfigFileConstants.FunctionDefinitions, ConfigFileConstants.FunctionNameToken, _path));
                                ThrowTerminatingError(e.ErrorRecord);
                            }

                            if (!hashtable.ContainsKey(ConfigFileConstants.FunctionValueToken))
                            {
                                PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                    ConfigFileConstants.FunctionDefinitions, ConfigFileConstants.FunctionValueToken, _path));
                                ThrowTerminatingError(e.ErrorRecord);
                            }

                            if (hashtable[ConfigFileConstants.FunctionValueToken] is not ScriptBlock)
                            {
                                PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCKeyMustBeScriptBlock,
                                    ConfigFileConstants.FunctionValueToken, ConfigFileConstants.FunctionDefinitions, _path));
                                ThrowTerminatingError(e.ErrorRecord);
                            }

                            foreach (string functionKey in hashtable.Keys)
                            {
                                if (!string.Equals(functionKey, ConfigFileConstants.FunctionNameToken, StringComparison.OrdinalIgnoreCase) &&
                                    !string.Equals(functionKey, ConfigFileConstants.FunctionValueToken, StringComparison.OrdinalIgnoreCase) &&
                                    !string.Equals(functionKey, ConfigFileConstants.FunctionOptionsToken, StringComparison.OrdinalIgnoreCase))
                                {
                                    PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeContainsInvalidKey,
                                        functionKey, ConfigFileConstants.FunctionDefinitions, _path));
                                    ThrowTerminatingError(e.ErrorRecord);
                                }
                            }
                        }
                    }
                    else
                    {
                        PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustBeHashtableArray,
                            ConfigFileConstants.FunctionDefinitions, filePath));
                        ThrowTerminatingError(e.ErrorRecord);
                    }
                }

                // Variable definitions
                if (_variableDefinitions == null)
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VariableDefinitions, RemotingErrorIdStrings.DISCVariableDefinitionsComment,
                        "@{ Name = 'Variable1'; Value = { 'Dynamic' + 'InitialValue' } }, @{ Name = 'Variable2'; Value = 'StaticInitialValue' }", streamWriter, true));
                }
                else
                {
                    string varString = _variableDefinitions as string;

                    if (varString != null)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VariableDefinitions, RemotingErrorIdStrings.DISCVariableDefinitionsComment,
                            varString, streamWriter, false));
                    }
                    else
                    {
                        Hashtable[] varHash = DISCPowerShellConfiguration.TryGetHashtableArray(_variableDefinitions);

                        if (varHash != null)
                        {
                            result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VariableDefinitions, RemotingErrorIdStrings.DISCVariableDefinitionsComment,
                                SessionConfigurationUtils.CombineHashtableArray(varHash, streamWriter), streamWriter, false));

                            foreach (Hashtable hashtable in varHash)
                            {
                                if (!hashtable.ContainsKey(ConfigFileConstants.VariableNameToken))
                                {
                                    PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                        ConfigFileConstants.VariableDefinitions, ConfigFileConstants.VariableNameToken, _path));
                                    ThrowTerminatingError(e.ErrorRecord);
                                }

                                if (!hashtable.ContainsKey(ConfigFileConstants.VariableValueToken))
                                {
                                    PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                        ConfigFileConstants.VariableDefinitions, ConfigFileConstants.VariableValueToken, _path));
                                    ThrowTerminatingError(e.ErrorRecord);
                                }

                                foreach (string variableKey in hashtable.Keys)
                                {
                                    if (!string.Equals(variableKey, ConfigFileConstants.VariableNameToken, StringComparison.OrdinalIgnoreCase) &&
                                        !string.Equals(variableKey, ConfigFileConstants.VariableValueToken, StringComparison.OrdinalIgnoreCase))
                                    {
                                        PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeContainsInvalidKey,
                                            variableKey, ConfigFileConstants.VariableDefinitions, _path));
                                        ThrowTerminatingError(e.ErrorRecord);
                                    }
                                }
                            }
                        }
                        else
                        {
                            PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustBeHashtableArray,
                                ConfigFileConstants.VariableDefinitions, filePath));
                            ThrowTerminatingError(e.ErrorRecord);
                        }
                    }
                }

                // Environment variables
                if (_environmentVariables == null)
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.EnvironmentVariables, RemotingErrorIdStrings.DISCEnvironmentVariablesComment,
                        "@{ Variable1 = 'Value1'; Variable2 = 'Value2' }",
                        streamWriter, true));
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.EnvironmentVariables, RemotingErrorIdStrings.DISCEnvironmentVariablesComment,
                        SessionConfigurationUtils.CombineHashtable(_environmentVariables, streamWriter), streamWriter, false));
                }

                // Types to process
                resultData = (_typesToProcess.Length > 0) ? SessionConfigurationUtils.CombineStringArray(_typesToProcess) : "'C:\\ConfigData\\MyTypes.ps1xml', 'C:\\ConfigData\\OtherTypes.ps1xml'";
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.TypesToProcess, RemotingErrorIdStrings.DISCTypesToProcessComment,
                    resultData, streamWriter, (_typesToProcess.Length == 0)));

                // Formats to process
                resultData = (_formatsToProcess.Length > 0) ? SessionConfigurationUtils.CombineStringArray(_formatsToProcess) : "'C:\\ConfigData\\MyFormats.ps1xml', 'C:\\ConfigData\\OtherFormats.ps1xml'";
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.FormatsToProcess, RemotingErrorIdStrings.DISCFormatsToProcessComment,
                    resultData, streamWriter, (_formatsToProcess.Length == 0)));

                // Assemblies to load
                bool isExample = false;
                if ((_assembliesToLoad == null) || (_assembliesToLoad.Length == 0))
                {
                    isExample = true;
                    _assembliesToLoad = new string[] { "System.Web", "System.OtherAssembly, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" };
                }

                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.AssembliesToLoad, RemotingErrorIdStrings.DISCAssembliesToLoadComment,
                    SessionConfigurationUtils.CombineStringArray(_assembliesToLoad), streamWriter, isExample));

                result.Append('}');

                streamWriter.Write(result.ToString());
            }
            finally
            {
                streamWriter.Dispose();
            }
        }

        #endregion
    }

    #region SessionConfigurationUtils

    /// <summary>
    /// Utility methods for configuration file commands.
    /// </summary>
    internal static class SessionConfigurationUtils
    {
        /// <summary>
        /// This routine builds a fragment of the config file
        /// for a particular key. It returns a formatted string that includes
        /// a comment describing the key as well as the key and its value.
        /// </summary>
        internal static string ConfigFragment(string key, string resourceString, string value, StreamWriter streamWriter, bool isExample)
        {
            string nl = streamWriter.NewLine;

            if (isExample)
            {
                return string.Format(CultureInfo.InvariantCulture, "# {0}{1}# {2:19} = {3}{4}{5}", resourceString, nl, key, value, nl, nl);
            }
            
            return string.Format(CultureInfo.InvariantCulture, "# {0}{1}{2:19} = {3}{4}{5}", resourceString, nl, key, value, nl, nl);
        }

        /// <summary>
        /// Return a single-quoted string. Any embedded single quotes will be doubled.
        /// </summary>
        /// <param name="name">The string to quote.</param>
        /// <returns>The quoted string.</returns>
        internal static string QuoteName(object name)
        {
            if (name == null)
                return "''";
            return "'" + System.Management.Automation.Language.CodeGeneration.EscapeSingleQuotedStringContent(name.ToString()) + "'";
        }

        /// <summary>
        /// Return a script block string wrapped in curly braces.
        /// </summary>
        /// <param name="sb">The string to wrap.</param>
        /// <returns>The wrapped string.</returns>
        internal static string WrapScriptBlock(object sb)
        {
            if (sb == null)
                return "{}";
            return "{" + sb.ToString() + "}";
        }

        /// <summary>
        /// Return a script block string wrapped in curly braces.
        /// </summary>
        internal static string WriteBoolean(bool booleanToEmit)
        {
            if (booleanToEmit)
            {
                return "$true";
            }
            else
            {
                return "$false";
            }
        }

        internal static string WriteLong(long longToEmit)
        {
            return longToEmit.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the visibility default value.
        /// </summary>
        internal static string GetVisibilityDefault(object[] values, StreamWriter writer, PSCmdlet caller)
        {
            if ((values != null) && (values.Length > 0))
            {
                return CombineHashTableOrStringArray(values, writer, caller);
            }

            // Default Visibility is Empty which gets commented
            // out in the session config file
            return "'Item1', 'Item2'";
        }

        /// <summary>
        /// Combines a hashtable into a single string block.
        /// </summary>
        internal static string CombineHashtable(IDictionary table, StreamWriter writer, int? indent = 0)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("@{");

            var keys = table.Keys.Cast<string>().Order();
            foreach (var key in keys)
            {
                sb.Append(writer.NewLine);
                sb.AppendFormat("{0," + (4 * (indent + 1)) + "}", string.Empty);
                sb.Append(QuoteName(key));
                sb.Append(" = ");
                if (table[key] is ScriptBlock)
                {
                    sb.Append(WrapScriptBlock(table[key].ToString()));
                    continue;
                }

                IDictionary tableValue = table[key] as IDictionary;
                if (tableValue != null)
                {
                    sb.Append(CombineHashtable(tableValue, writer, indent + 1));
                    continue;
                }

                IDictionary[] tableValues = DISCPowerShellConfiguration.TryGetHashtableArray(table[key]);
                if (tableValues != null)
                {
                    sb.Append(CombineHashtableArray(tableValues, writer, indent + 1));
                    continue;
                }

                string[] stringValues = DISCPowerShellConfiguration.TryGetStringArray(table[key]);
                if (stringValues != null)
                {
                    sb.Append(CombineStringArray(stringValues));
                    continue;
                }

                sb.Append(QuoteName(table[key]));
            }

            sb.Append(" }");

            return sb.ToString();
        }

        /// <summary>
        /// Combines RequireGroups logic operator hash tables / lists
        /// e.g.,
        /// -RequiredGroups @{ Or = 'TrustedGroup1', 'MFAGroup2' }
        /// -RequiredGroups @{ And = 'Administrators', @{ Or = 'MFAGroup1', 'MFAGroup2' } }
        /// -RequiredGroups @{ Or = @{ And = 'Administrators', 'TrustedGroup1' }, @{ And = 'Power Users', 'TrustedGroup1' } }
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        internal static string CombineRequiredGroupsHash(IDictionary table)
        {
            if (table.Count != 1)
            {
                throw new PSInvalidOperationException(RemotingErrorIdStrings.RequiredGroupsHashMultipleKeys);
            }

            StringBuilder sb = new StringBuilder();

            var keyEnumerator = table.Keys.GetEnumerator();
            keyEnumerator.MoveNext();
            string key = keyEnumerator.Current as string;
            object keyObject = table[key];
            sb.Append("@{ ");
            sb.Append(QuoteName(key));
            sb.Append(" = ");

            object[] values = keyObject as object[];
            if (values != null)
            {
                for (int i = 0; i < values.Length;)
                {
                    WriteRequiredGroup(values[i++], sb);

                    if (i < values.Length)
                    {
                        sb.Append(", ");
                    }
                }
            }
            else
            {
                WriteRequiredGroup(keyObject, sb);
            }

            sb.Append(" }");

            return sb.ToString();
        }

        private static void WriteRequiredGroup(object value, StringBuilder sb)
        {
            string strValue = value as string;
            if (strValue != null)
            {
                sb.Append(QuoteName(strValue));
            }
            else
            {
                Hashtable subTable = value as Hashtable;
                if (subTable != null)
                {
                    sb.Append(CombineRequiredGroupsHash(subTable));
                }
                else
                {
                    throw new PSInvalidOperationException(RemotingErrorIdStrings.UnknownGroupMembershipValue);
                }
            }
        }

        /// <summary>
        /// Combines an array of hashtables into a single string block.
        /// </summary>
        internal static string CombineHashtableArray(IDictionary[] tables, StreamWriter writer, int? indent = 0)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < tables.Length; i++)
            {
                sb.Append(CombineHashtable(tables[i], writer, indent));

                if (i < (tables.Length - 1))
                {
                    sb.Append(", ");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Combines an array of strings into a single string block.
        /// </summary>
        /// <param name="values">String values.</param>
        /// <returns>String block.</returns>
        internal static string CombineStringArray(string[] values)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrEmpty(values[i]))
                {
                    sb.Append(QuoteName(values[i]));

                    if (i < (values.Length - 1))
                    {
                        sb.Append(", ");
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Combines an array of strings or hashtables into a single string block.
        /// </summary>
        internal static string CombineHashTableOrStringArray(object[] values, StreamWriter writer, PSCmdlet caller)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                string strVal = values[i] as string;
                if (!string.IsNullOrEmpty(strVal))
                {
                    sb.Append(QuoteName(strVal));
                }
                else
                {
                    Hashtable hashVal = values[i] as Hashtable;
                    if (hashVal == null)
                    {
                        string message = StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustBeStringOrHashtableArray,
                                                           ConfigFileConstants.ModulesToImport);
                        PSArgumentException e = new PSArgumentException(message);
                        caller.ThrowTerminatingError(e.ErrorRecord);
                    }

                    sb.Append(CombineHashtable(hashVal, writer));
                }

                if (i < (values.Length - 1))
                {
                    sb.Append(", ");
                }
            }

            return sb.ToString();
        }
    }

    #endregion
}
