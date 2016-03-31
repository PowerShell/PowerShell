using System;
using System.IO;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Text;
using System.Diagnostics;
using System.Management.Automation.Internal;
using System.Globalization;

#if CORECLR
// Some APIs are missing from System.Environment. We use System.Management.Automation.Environment as a proxy type:
//  - for missing APIs, System.Management.Automation.Environment has extension implementation.
//  - for existing APIs, System.Management.Automation.Environment redirect the call to System.Environment.
using Environment = System.Management.Automation.Environment;
#endif

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// New-PSSessionConfigurationFile command implementation
    /// 
    /// See Declarative Initial Session State (DISC)
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PSSessionConfigurationFile", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=217036")]
    public class NewPSSessionConfigurationFileCommand : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// Destination path
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
            }
        }
        private string path;

        /// <summary>
        /// Configuration file schema version
        /// </summary>
        [Parameter()]
        [ValidateNotNull]
        public Version SchemaVersion
        {
            get
            {
                return schemaVersion;
            }
            set
            {
                schemaVersion = value;
            }
        }
        private Version schemaVersion = new Version("2.0.0.0");

        /// <summary>
        /// Configuration file GUID
        /// </summary>
        [Parameter()]
        public Guid Guid
        {
            get
            {
                return guid;
            }
            set
            {
                guid = value;
            }
        }
        private Guid guid = Guid.NewGuid();

        /// <summary>
        /// Author of the configuration file
        /// </summary>
        [Parameter()]
        public string Author
        {
            get
            {
                return author;
            }
            set
            {
                author = value;
            }
        }
        private string author;

        /// <summary>
        /// Description
        /// </summary>
        [Parameter()]
        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                description = value;
            }
        }
        private string description;

        /// <summary>
        /// Company name
        /// </summary>
        [Parameter()]
        public string CompanyName
        {
            get
            {
                return companyName;
            }
            set
            {
                companyName = value;
            }
        }
        private string companyName;

        /// <summary>
        /// Copyright information
        /// </summary>
        [Parameter()]
        public string Copyright
        {
            get
            {
                return copyright;
            }
            set
            {
                copyright = value;
            }
        }
        private string copyright;

        /// <summary>
        /// Specifies type of initial session state to use.
        /// </summary>
        [Parameter()]
        public SessionType SessionType
        {
            get
            {
                return sessionType;
            }
            set
            {
                sessionType = value;
            }
        }
        private SessionType sessionType = SessionType.Default;

        /// <summary>
        /// Specifies the directory for transcripts to be placed.
        /// </summary>
        [Parameter()]
        public string TranscriptDirectory
        {
            get
            {
                return transcriptDirectory;
            }
            set
            {
                transcriptDirectory = value;
            }
        }
        private string transcriptDirectory = null;

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
        /// Scripts to process
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ScriptsToProcess
        {
            get
            {
                return scriptsToProcess;
            }
            set
            {
                scriptsToProcess = value;
            }
        }
        private string[] scriptsToProcess = Utils.EmptyArray<string>();

        /// <summary>
        /// Role definitions for this session configuration (Role name -> Role capability)
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IDictionary RoleDefinitions
        {
            get
            {
                return roleDefinitions;
            }
            set
            {
                roleDefinitions = value;
            }
        }
        private IDictionary roleDefinitions;

        /// <summary>
        /// Specifies account groups that are membership requirements for this session
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IDictionary RequiredGroups
        {
            get { return requiredGroups; }
            set { requiredGroups = value; }
        }
        private IDictionary requiredGroups;

        /// <summary>
        /// Languange mode
        /// </summary>
        [Parameter()]
        public PSLanguageMode LanguageMode
        {
            get
            {
                return languageMode;
            }
            set
            {
                languageMode = value;
                isLanguageModeSpecified = true;
            }
        }
        private PSLanguageMode languageMode = PSLanguageMode.NoLanguage;
        private bool isLanguageModeSpecified;

        /// <summary>
        /// Execution policy
        /// </summary>
        [Parameter()]
        public ExecutionPolicy ExecutionPolicy
        {
            get
            {
                return executionPolicy;
            }
            set
            {
                executionPolicy = value;
            }
        }
        private ExecutionPolicy executionPolicy = ExecutionPolicy.Restricted;

        /// <summary>
        /// PowerShell version
        /// </summary>
        [Parameter()]
        public Version PowerShellVersion
        {
            get
            {
                return powerShellVersion;
            }
            set
            {
                powerShellVersion = value;
            }
        }
        private Version powerShellVersion;

        /// <summary>
        /// A list of modules to import
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] ModulesToImport
        {
            get
            {
                return modulesToImport;
            }
            set
            {
                modulesToImport = value;
            }
        }
        private object[] modulesToImport;

        /// <summary>
        /// A list of visible aliases
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] VisibleAliases
        {
            get
            {
                return visibleAliases;
            }
            set
            {
                visibleAliases = value;
            }
        }
        private string[] visibleAliases = Utils.EmptyArray<string>();

        /// <summary>
        /// A list of visible cmdlets
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Object[] VisibleCmdlets
        {
            get
            {
                return visibleCmdlets;
            }
            set
            {
                visibleCmdlets = value;
            }
        }
        private Object[] visibleCmdlets = null;

        /// <summary>
        /// A list of visible functions
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Object[] VisibleFunctions
        {
            get
            {
                return visibleFunctions;
            }
            set
            {
                visibleFunctions = value;
            }
        }
        private Object[] visibleFunctions = null;

        /// <summary>
        /// A list of visible external commands (scripts and applications)
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] VisibleExternalCommands
        {
            get
            {
                return visibleExternalCommands;
            }
            set
            {
                visibleExternalCommands = value;
            }
        }
        private string[] visibleExternalCommands = Utils.EmptyArray<string>();

        /// <summary>
        /// A list of providers
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] VisibleProviders
        {
            get
            {
                return visibleProviders;
            }
            set
            {
                visibleProviders = value;
            }
        }
        private string[] visibleProviders = Utils.EmptyArray<string>();

        /// <summary>
        /// A list of alises
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public IDictionary[] AliasDefinitions
        {
            get
            {
                return aliasDefinitions;
            }
            set
            {
                aliasDefinitions = value;
            }
        }
        private IDictionary[] aliasDefinitions;

        /// <summary>
        /// A list of functions
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public IDictionary[] FunctionDefinitions
        {
            get
            {
                return functionDefinitions;
            }
            set
            {
                functionDefinitions = value;
            }
        }
        private IDictionary[] functionDefinitions;

        /// <summary>
        /// A list of variables
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object VariableDefinitions
        {
            get
            {
                return variableDefinitions;
            }
            set
            {
                variableDefinitions = value;
            }
        }
        private object variableDefinitions;

        /// <summary>
        /// A list of environment variables
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IDictionary EnvironmentVariables
        {
            get
            {
                return environmentVariables;
            }
            set
            {
                environmentVariables = value;
            }
        }
        private IDictionary environmentVariables;

        /// <summary>
        /// A list of types to process
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] TypesToProcess
        {
            get
            {
                return typesToProcess;
            }
            set
            {
                typesToProcess = value;
            }
        }
        private string[] typesToProcess = Utils.EmptyArray<string>();

        /// <summary>
        /// A list of format data to process
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] FormatsToProcess
        {
            get
            {
                return formatsToProcess;
            }
            set
            {
                formatsToProcess = value;
            }
        }
        private string[] formatsToProcess = Utils.EmptyArray<string>();

        /// <summary>
        /// A list of assemblies to load
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] AssembliesToLoad
        {
            get
            {
                return assembliesToLoad;
            }
            set
            {
                assembliesToLoad = value;
            }
        }
        private string[] assembliesToLoad;

        /// <summary>
        /// Gets or sets whether to include a full expansion of all possible session configuration
        /// keys as comments when creating the session configuration file.
        /// </summary>
        [Parameter()]
        public SwitchParameter Full { get; set; }

        #endregion

        #region Overrides

        /// <summary>
        /// 
        /// </summary>
        protected override void ProcessRecord()
        {
            Debug.Assert(!String.IsNullOrEmpty(path));

            ProviderInfo provider = null;
            PSDriveInfo drive;
            string filePath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(path, out provider, out drive);

            if (!provider.NameEquals(Context.ProviderNames.FileSystem) || !filePath.EndsWith(StringLiterals.PowerShellDISCFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                string message = StringUtil.Format(RemotingErrorIdStrings.InvalidPSSessionConfigurationFilePath, path);
                InvalidOperationException ioe = new InvalidOperationException(message);
                ErrorRecord er = new ErrorRecord(ioe, "InvalidPSSessionConfigurationFilePath",
                    ErrorCategory.InvalidArgument, path);
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
                    SessionConfigurationUtils.QuoteName(schemaVersion), streamWriter, false));

                // Guid
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Guid, RemotingErrorIdStrings.DISCGUIDComment, SessionConfigurationUtils.QuoteName(guid), streamWriter, false));

                // Author
                if (String.IsNullOrEmpty(author))
                {
                    author = Environment.UserName;
                }
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Author, RemotingErrorIdStrings.DISCAuthorComment,
                    SessionConfigurationUtils.QuoteName(author), streamWriter, false));

                // Description
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Description, RemotingErrorIdStrings.DISCDescriptionComment,
                    SessionConfigurationUtils.QuoteName(description), streamWriter, String.IsNullOrEmpty(description)));

                // Company name
                if (ShouldGenerateConfigurationSnippet("CompanyName"))
                {
                    if (String.IsNullOrEmpty(companyName))
                    {
                        companyName = Modules.DefaultCompanyName;
                    }
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.CompanyName, RemotingErrorIdStrings.DISCCompanyNameComment,
                        SessionConfigurationUtils.QuoteName(companyName), streamWriter, false));
                }

                // Copyright
                if (ShouldGenerateConfigurationSnippet("Copyright"))
                {
                    if (String.IsNullOrEmpty(copyright))
                    {
                        copyright = StringUtil.Format(Modules.DefaultCopyrightMessage, DateTime.Now.Year, author);
                    }
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Copyright, RemotingErrorIdStrings.DISCCopyrightComment,
                        SessionConfigurationUtils.QuoteName(copyright), streamWriter, false));
                }

                // Session type
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.SessionType, RemotingErrorIdStrings.DISCInitialSessionStateComment,
                    SessionConfigurationUtils.QuoteName(sessionType), streamWriter, false));

                string resultData = null;

                // Transcript directory
                resultData = String.IsNullOrEmpty(transcriptDirectory) ? "'C:\\Transcripts\\'" : SessionConfigurationUtils.QuoteName(transcriptDirectory);
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.TranscriptDirectory, RemotingErrorIdStrings.DISCTranscriptDirectoryComment,
                    resultData, streamWriter, String.IsNullOrEmpty(transcriptDirectory)));

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
                resultData = (scriptsToProcess.Length > 0) ? SessionConfigurationUtils.CombineStringArray(scriptsToProcess) : "'C:\\ConfigData\\InitScript1.ps1', 'C:\\ConfigData\\InitScript2.ps1'";
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ScriptsToProcess, RemotingErrorIdStrings.DISCScriptsToProcessComment,
                    resultData, streamWriter, (scriptsToProcess.Length == 0)));

                // Role definitions
                if (roleDefinitions == null)
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.RoleDefinitions, RemotingErrorIdStrings.DISCRoleDefinitionsComment,
                        "@{ 'CONTOSO\\SqlAdmins' = @{ RoleCapabilities = 'SqlAdministration' }; 'CONTOSO\\ServerMonitors' = @{ VisibleCmdlets = 'Get-Process' } } ", streamWriter, true));
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.RoleDefinitions, RemotingErrorIdStrings.DISCRoleDefinitionsComment,
                        SessionConfigurationUtils.CombineHashtable(roleDefinitions, streamWriter), streamWriter, false));
                }

                // Required groups
                if (ShouldGenerateConfigurationSnippet("RequiredGroups"))
                {
                    if (requiredGroups == null)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.RequiredGroups, RemotingErrorIdStrings.DISCRequiredGroupsComment,
                            "@{ And = @{ Or = 'CONTOSO\\SmartCard-Logon1', 'CONTOSO\\SmartCard-Logon2' }, 'Administrators' }", streamWriter, true));
                    }
                    else
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.RequiredGroups, RemotingErrorIdStrings.DISCRequiredGroupsComment,
                            SessionConfigurationUtils.CombineRequiredGroupsHash(requiredGroups), streamWriter, false));
                    }
                }

                // PSLanguageMode languageMode
                if (ShouldGenerateConfigurationSnippet("LanguageMode"))
                {
                    if (!isLanguageModeSpecified)
                    {
                        if (sessionType == SessionType.Default)
                        {
                            languageMode = PSLanguageMode.FullLanguage;
                        }
                    }
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.LanguageMode, RemotingErrorIdStrings.DISCLanguageModeComment,
                        SessionConfigurationUtils.QuoteName(languageMode), streamWriter, false));
                }

                // ExecutionPolicy executionPolicy
                if (ShouldGenerateConfigurationSnippet("ExecutionPolicy"))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ExecutionPolicy, RemotingErrorIdStrings.DISCExecutionPolicyComment,
                        SessionConfigurationUtils.QuoteName(executionPolicy), streamWriter, false));
                }

                // PowerShell version
                bool isExample = false;

                if (ShouldGenerateConfigurationSnippet("PowerShellVersion"))
                {
                    if (powerShellVersion == null)
                    {
                        isExample = true;
                        powerShellVersion = PSVersionInfo.PSVersion;
                    }
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.PowerShellVersion, RemotingErrorIdStrings.DISCPowerShellVersionComment,
                        SessionConfigurationUtils.QuoteName(powerShellVersion), streamWriter, isExample));
                }

                // Modules to import
                if (modulesToImport == null)
                {
                    if (Full)
                    {
                        string exampleModulesToImport = "'MyCustomModule', @{ ModuleName = 'MyCustomModule'; ModuleVersion = '1.0.0.0'; GUID = '4d30d5f0-cb16-4898-812d-f20a6c596bdf' }";
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ModulesToImport, RemotingErrorIdStrings.DISCModulesToImportComment, exampleModulesToImport, streamWriter, true));
                    }
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ModulesToImport, RemotingErrorIdStrings.DISCModulesToImportComment,
                        SessionConfigurationUtils.CombineHashTableOrStringArray(modulesToImport, streamWriter, this), streamWriter, false));
                }

                // Visible aliases
                if (ShouldGenerateConfigurationSnippet("VisibleAliases"))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleAliases, RemotingErrorIdStrings.DISCVisibleAliasesComment,
                        SessionConfigurationUtils.GetVisibilityDefault(visibleAliases, streamWriter, this), streamWriter, visibleAliases.Length == 0));
                }

                // Visible cmdlets
                if ((visibleCmdlets == null) || (visibleCmdlets.Length == 0))
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
                        SessionConfigurationUtils.GetVisibilityDefault(visibleCmdlets, streamWriter, this), streamWriter, false));
                }

                // Visible functions
                if ((visibleFunctions == null) || (visibleFunctions.Length == 0))
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
                        SessionConfigurationUtils.GetVisibilityDefault(visibleFunctions, streamWriter, this), streamWriter, visibleFunctions.Length == 0));
                }

                // Visible external commands (scripts, executables)
                if (ShouldGenerateConfigurationSnippet("VisibleExternalCommands"))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleExternalCommands, RemotingErrorIdStrings.DISCVisibleExternalCommandsComment,
                        SessionConfigurationUtils.GetVisibilityDefault(visibleExternalCommands, streamWriter, this), streamWriter, visibleExternalCommands.Length == 0));
                }

                // Visible providers
                if (ShouldGenerateConfigurationSnippet("VisibleProviders"))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleProviders, RemotingErrorIdStrings.DISCVisibleProvidersComment,
                        SessionConfigurationUtils.GetVisibilityDefault(visibleProviders, streamWriter, this), streamWriter, visibleProviders.Length == 0));
                }

                // Alias definitions
                if ((aliasDefinitions == null) || (aliasDefinitions.Length == 0))
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
                        SessionConfigurationUtils.CombineHashtableArray(aliasDefinitions, streamWriter), streamWriter, false));
                }

                // Function definitions
                if (functionDefinitions == null)
                {
                    if (Full)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.FunctionDefinitions, RemotingErrorIdStrings.DISCFunctionDefinitionsComment,
                            "@{ Name = 'MyFunction'; ScriptBlock = { param($MyInput) $MyInput } }", streamWriter, true));
                    }
                }
                else
                {
                    Hashtable[] funcHash = DISCPowerShellConfiguration.TryGetHashtableArray(functionDefinitions);

                    if (funcHash != null)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.FunctionDefinitions, RemotingErrorIdStrings.DISCFunctionDefinitionsComment,
                            SessionConfigurationUtils.CombineHashtableArray(funcHash, streamWriter), streamWriter, false));

                        foreach (Hashtable hashtable in funcHash)
                        {
                            if (!hashtable.ContainsKey(ConfigFileConstants.FunctionNameToken))
                            {
                                PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                    ConfigFileConstants.FunctionDefinitions, ConfigFileConstants.FunctionNameToken, path));
                                ThrowTerminatingError(e.ErrorRecord);
                            }

                            if (!hashtable.ContainsKey(ConfigFileConstants.FunctionValueToken))
                            {
                                PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                    ConfigFileConstants.FunctionDefinitions, ConfigFileConstants.FunctionValueToken, path));
                                ThrowTerminatingError(e.ErrorRecord);
                            }
                            
                            if ((hashtable[ConfigFileConstants.FunctionValueToken] as ScriptBlock) == null)
                            {
                                PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCKeyMustBeScriptBlock,
                                    ConfigFileConstants.FunctionValueToken, ConfigFileConstants.FunctionDefinitions, path));
                                ThrowTerminatingError(e.ErrorRecord);
                            }

                            foreach (string functionKey in hashtable.Keys)
                            {
                                if (!String.Equals(functionKey, ConfigFileConstants.FunctionNameToken, StringComparison.OrdinalIgnoreCase) &&
                                    !String.Equals(functionKey, ConfigFileConstants.FunctionValueToken, StringComparison.OrdinalIgnoreCase) &&
                                    !String.Equals(functionKey, ConfigFileConstants.FunctionOptionsToken, StringComparison.OrdinalIgnoreCase))
                                {
                                    PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeContainsInvalidKey,
                                        functionKey, ConfigFileConstants.FunctionDefinitions, path));
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
                if (variableDefinitions == null)
                {
                    if (Full)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VariableDefinitions, RemotingErrorIdStrings.DISCVariableDefinitionsComment,
                            "@{ Name = 'Variable1'; Value = { 'Dynamic' + 'InitialValue' } }, @{ Name = 'Variable2'; Value = 'StaticInitialValue' }", streamWriter, true));
                    }
                }
                else
                {
                    string varString = variableDefinitions as string;

                    if (varString != null)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VariableDefinitions, RemotingErrorIdStrings.DISCVariableDefinitionsComment,
                            varString, streamWriter, false));
                    }
                    else
                    {
                        Hashtable[] varHash = DISCPowerShellConfiguration.TryGetHashtableArray(variableDefinitions);

                        if (varHash != null)
                        {
                            result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VariableDefinitions, RemotingErrorIdStrings.DISCVariableDefinitionsComment,
                                SessionConfigurationUtils.CombineHashtableArray(varHash, streamWriter), streamWriter, false));

                            foreach (Hashtable hashtable in varHash)
                            {
                                if (!hashtable.ContainsKey(ConfigFileConstants.VariableNameToken))
                                {
                                    PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                        ConfigFileConstants.VariableDefinitions, ConfigFileConstants.VariableNameToken, path));
                                    ThrowTerminatingError(e.ErrorRecord);
                                }

                                if (!hashtable.ContainsKey(ConfigFileConstants.VariableValueToken))
                                {
                                    PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                        ConfigFileConstants.VariableDefinitions, ConfigFileConstants.VariableValueToken, path));
                                    ThrowTerminatingError(e.ErrorRecord);
                                }

                                foreach (string variableKey in hashtable.Keys)
                                {
                                    if (!String.Equals(variableKey, ConfigFileConstants.VariableNameToken, StringComparison.OrdinalIgnoreCase) && 
                                        !String.Equals(variableKey, ConfigFileConstants.VariableValueToken, StringComparison.OrdinalIgnoreCase))
                                    {
                                        PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeContainsInvalidKey,
                                            variableKey, ConfigFileConstants.VariableDefinitions, path));
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
                if (environmentVariables == null)
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
                        SessionConfigurationUtils.CombineHashtable(environmentVariables, streamWriter), streamWriter, false));
                }

                // Types to process
                if (ShouldGenerateConfigurationSnippet("TypesToProcess"))
                {
                    resultData = (typesToProcess.Length > 0) ? SessionConfigurationUtils.CombineStringArray(typesToProcess) : "'C:\\ConfigData\\MyTypes.ps1xml', 'C:\\ConfigData\\OtherTypes.ps1xml'";
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.TypesToProcess, RemotingErrorIdStrings.DISCTypesToProcessComment,
                        resultData, streamWriter, (typesToProcess.Length == 0)));
                }

                // Formats to process
                if (ShouldGenerateConfigurationSnippet("FormatsToProcess"))
                {
                    resultData = (formatsToProcess.Length > 0) ? SessionConfigurationUtils.CombineStringArray(formatsToProcess) : "'C:\\ConfigData\\MyFormats.ps1xml', 'C:\\ConfigData\\OtherFormats.ps1xml'";
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.FormatsToProcess, RemotingErrorIdStrings.DISCFormatsToProcessComment,
                        resultData, streamWriter, (formatsToProcess.Length == 0)));
                }

                // Assemblies to load
                if (ShouldGenerateConfigurationSnippet("AssembliesToLoad"))
                {
                    isExample = false;
                    if ((assembliesToLoad == null) || (assembliesToLoad.Length == 0))
                    {
                        isExample = true;
                        assembliesToLoad = new string[] { "System.Web", "System.OtherAssembly, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" };
                    }
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.AssembliesToLoad, RemotingErrorIdStrings.DISCAssembliesToLoadComment,
                        SessionConfigurationUtils.CombineStringArray(assembliesToLoad), streamWriter, isExample));
                }

                result.Append("}");

                streamWriter.Write(result.ToString());
            }
            finally
            {
                streamWriter.Dispose();
            }
        }

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
#if !CORECLR
    [Cmdlet(VerbsCommon.New, "PSRoleCapabilityFile", HelpUri = "http://go.microsoft.com/fwlink/?LinkId=623708")]
#endif
    public class NewPSRoleCapabilityFileCommand : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// Destination path
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
            }
        }
        private string path;

        /// <summary>
        /// Configuration file GUID
        /// </summary>
        [Parameter()]
        public Guid Guid
        {
            get
            {
                return guid;
            }
            set
            {
                guid = value;
            }
        }
        private Guid guid = Guid.NewGuid();

        /// <summary>
        /// Author of the configuration file
        /// </summary>
        [Parameter()]
        public string Author
        {
            get
            {
                return author;
            }
            set
            {
                author = value;
            }
        }
        private string author;

        /// <summary>
        /// Description
        /// </summary>
        [Parameter()]
        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                description = value;
            }
        }
        private string description;

        /// <summary>
        /// Company name
        /// </summary>
        [Parameter()]
        public string CompanyName
        {
            get
            {
                return companyName;
            }
            set
            {
                companyName = value;
            }
        }
        private string companyName;

        /// <summary>
        /// Copyright information
        /// </summary>
        [Parameter()]
        public string Copyright
        {
            get
            {
                return copyright;
            }
            set
            {
                copyright = value;
            }
        }
        private string copyright;

        /// <summary>
        /// A list of modules to import
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] ModulesToImport
        {
            get
            {
                return modulesToImport;
            }
            set
            {
                modulesToImport = value;
            }
        }
        private object[] modulesToImport;

        /// <summary>
        /// A list of visible aliases
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] VisibleAliases
        {
            get
            {
                return visibleAliases;
            }
            set
            {
                visibleAliases = value;
            }
        }
        private string[] visibleAliases = Utils.EmptyArray<string>();

        /// <summary>
        /// A list of visible cmdlets
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Object[] VisibleCmdlets
        {
            get
            {
                return visibleCmdlets;
            }
            set
            {
                visibleCmdlets = value;
            }
        }
        private Object[] visibleCmdlets = null;

        /// <summary>
        /// A list of visible functions
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Object[] VisibleFunctions
        {
            get
            {
                return visibleFunctions;
            }
            set
            {
                visibleFunctions = value;
            }
        }
        private Object[] visibleFunctions = null;

        /// <summary>
        /// A list of visible external commands (scripts and applications)
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] VisibleExternalCommands
        {
            get
            {
                return visibleExternalCommands;
            }
            set
            {
                visibleExternalCommands = value;
            }
        }
        private string[] visibleExternalCommands = Utils.EmptyArray<string>();

        /// <summary>
        /// A list of providers
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] VisibleProviders
        {
            get
            {
                return visibleProviders;
            }
            set
            {
                visibleProviders = value;
            }
        }
        private string[] visibleProviders = Utils.EmptyArray<string>();

        /// <summary>
        /// Scripts to process
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ScriptsToProcess
        {
            get
            {
                return scriptsToProcess;
            }
            set
            {
                scriptsToProcess = value;
            }
        }
        private string[] scriptsToProcess = Utils.EmptyArray<string>();

        /// <summary>
        /// A list of alises
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public IDictionary[] AliasDefinitions
        {
            get
            {
                return aliasDefinitions;
            }
            set
            {
                aliasDefinitions = value;
            }
        }
        private IDictionary[] aliasDefinitions;

        /// <summary>
        /// A list of functions
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public IDictionary[] FunctionDefinitions
        {
            get
            {
                return functionDefinitions;
            }
            set
            {
                functionDefinitions = value;
            }
        }
        private IDictionary[] functionDefinitions;

        /// <summary>
        /// A list of variables
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object VariableDefinitions
        {
            get
            {
                return variableDefinitions;
            }
            set
            {
                variableDefinitions = value;
            }
        }
        private object variableDefinitions;

        /// <summary>
        /// A list of environment variables
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IDictionary EnvironmentVariables
        {
            get
            {
                return environmentVariables;
            }
            set
            {
                environmentVariables = value;
            }
        }
        private IDictionary environmentVariables;

        /// <summary>
        /// A list of types to process
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] TypesToProcess
        {
            get
            {
                return typesToProcess;
            }
            set
            {
                typesToProcess = value;
            }
        }
        private string[] typesToProcess = Utils.EmptyArray<string>();

        /// <summary>
        /// A list of format data to process
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] FormatsToProcess
        {
            get
            {
                return formatsToProcess;
            }
            set
            {
                formatsToProcess = value;
            }
        }
        private string[] formatsToProcess = Utils.EmptyArray<string>();

        /// <summary>
        /// A list of assemblies to load
        /// </summary>
        [Parameter()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] AssembliesToLoad
        {
            get
            {
                return assembliesToLoad;
            }
            set
            {
                assembliesToLoad = value;
            }
        }
        private string[] assembliesToLoad;

        #endregion

        #region Overrides

        /// <summary>
        /// 
        /// </summary>
        protected override void ProcessRecord()
        {
            Debug.Assert(!String.IsNullOrEmpty(path));

            ProviderInfo provider = null;
            PSDriveInfo drive;
            string filePath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(path, out provider, out drive);

            if (!provider.NameEquals(Context.ProviderNames.FileSystem) || !filePath.EndsWith(StringLiterals.PowerShellRoleCapabilityFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                string message = StringUtil.Format(RemotingErrorIdStrings.InvalidRoleCapabilityFilePath, path);
                InvalidOperationException ioe = new InvalidOperationException(message);
                ErrorRecord er = new ErrorRecord(ioe, "InvalidRoleCapabilityFilePath",
                    ErrorCategory.InvalidArgument, path);
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
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Guid, RemotingErrorIdStrings.DISCGUIDComment, SessionConfigurationUtils.QuoteName(guid), streamWriter, false));

                // Author
                if (String.IsNullOrEmpty(author))
                {
                    author = Environment.UserName;
                }
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Author, RemotingErrorIdStrings.DISCAuthorComment,
                    SessionConfigurationUtils.QuoteName(author), streamWriter, false));

                // Description
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Description, RemotingErrorIdStrings.DISCDescriptionComment,
                    SessionConfigurationUtils.QuoteName(description), streamWriter, String.IsNullOrEmpty(description)));

                // Company name
                if (String.IsNullOrEmpty(companyName))
                {
                    companyName = Modules.DefaultCompanyName;
                }
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.CompanyName, RemotingErrorIdStrings.DISCCompanyNameComment,
                    SessionConfigurationUtils.QuoteName(companyName), streamWriter, false));

                // Copyright
                if (String.IsNullOrEmpty(copyright))
                {
                    copyright = StringUtil.Format(Modules.DefaultCopyrightMessage, DateTime.Now.Year, author);
                }
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.Copyright, RemotingErrorIdStrings.DISCCopyrightComment,
                    SessionConfigurationUtils.QuoteName(copyright), streamWriter, false));

                // Modules to import
                if (modulesToImport == null)
                {
                    string exampleModulesToImport = "'MyCustomModule', @{ ModuleName = 'MyCustomModule'; ModuleVersion = '1.0.0.0'; GUID = '4d30d5f0-cb16-4898-812d-f20a6c596bdf' }";
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ModulesToImport, RemotingErrorIdStrings.DISCModulesToImportComment, exampleModulesToImport, streamWriter, true));

                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ModulesToImport, RemotingErrorIdStrings.DISCModulesToImportComment,
                        SessionConfigurationUtils.CombineHashTableOrStringArray(modulesToImport, streamWriter, this), streamWriter, false));
                }

                // Visible aliases
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleAliases, RemotingErrorIdStrings.DISCVisibleAliasesComment,
                    SessionConfigurationUtils.GetVisibilityDefault(visibleAliases, streamWriter, this), streamWriter, visibleAliases.Length == 0));

                // Visible cmdlets
                if ((visibleCmdlets == null) || (visibleCmdlets.Length == 0))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleCmdlets, RemotingErrorIdStrings.DISCVisibleCmdletsComment,
                        "'Invoke-Cmdlet1', @{ Name = 'Invoke-Cmdlet2'; Parameters = @{ Name = 'Parameter1'; ValidateSet = 'Item1', 'Item2' }, @{ Name = 'Parameter2'; ValidatePattern = 'L*' } }", streamWriter, true));
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleCmdlets, RemotingErrorIdStrings.DISCVisibleCmdletsComment,
                        SessionConfigurationUtils.GetVisibilityDefault(visibleCmdlets, streamWriter, this), streamWriter, false));
                }

                // Visible functions
                if ((visibleFunctions == null) || (visibleFunctions.Length == 0))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleFunctions, RemotingErrorIdStrings.DISCVisibleFunctionsComment,
                        "'Invoke-Function1', @{ Name = 'Invoke-Function2'; Parameters = @{ Name = 'Parameter1'; ValidateSet = 'Item1', 'Item2' }, @{ Name = 'Parameter2'; ValidatePattern = 'L*' } }", streamWriter, true));
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleFunctions, RemotingErrorIdStrings.DISCVisibleFunctionsComment,
                        SessionConfigurationUtils.GetVisibilityDefault(visibleFunctions, streamWriter, this), streamWriter, visibleFunctions.Length == 0));
                }

                // Visible external commands (scripts, executables)
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleExternalCommands, RemotingErrorIdStrings.DISCVisibleExternalCommandsComment,
                    SessionConfigurationUtils.GetVisibilityDefault(visibleExternalCommands, streamWriter, this), streamWriter, visibleExternalCommands.Length == 0));

                // Visible providers
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VisibleProviders, RemotingErrorIdStrings.DISCVisibleProvidersComment,
                    SessionConfigurationUtils.GetVisibilityDefault(visibleProviders, streamWriter, this), streamWriter, visibleProviders.Length == 0));

                // Scripts to process
                string resultData = (scriptsToProcess.Length > 0) ? SessionConfigurationUtils.CombineStringArray(scriptsToProcess) : "'C:\\ConfigData\\InitScript1.ps1', 'C:\\ConfigData\\InitScript2.ps1'";
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.ScriptsToProcess, RemotingErrorIdStrings.DISCScriptsToProcessComment,
                    resultData, streamWriter, (scriptsToProcess.Length == 0)));

                // Alias definitions
                if ((aliasDefinitions == null) || (aliasDefinitions.Length == 0))
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.AliasDefinitions, RemotingErrorIdStrings.DISCAliasDefinitionsComment,
                       "@{ Name = 'Alias1'; Value = 'Invoke-Alias1'}, @{ Name = 'Alias2'; Value = 'Invoke-Alias2'}", streamWriter, true));
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.AliasDefinitions, RemotingErrorIdStrings.DISCAliasDefinitionsComment,
                        SessionConfigurationUtils.CombineHashtableArray(aliasDefinitions, streamWriter), streamWriter, false));
                }

                // Function definitions
                if (functionDefinitions == null)
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.FunctionDefinitions, RemotingErrorIdStrings.DISCFunctionDefinitionsComment,
                        "@{ Name = 'MyFunction'; ScriptBlock = { param($MyInput) $MyInput } }", streamWriter, true));
                }
                else
                {
                    Hashtable[] funcHash = DISCPowerShellConfiguration.TryGetHashtableArray(functionDefinitions);

                    if (funcHash != null)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.FunctionDefinitions, RemotingErrorIdStrings.DISCFunctionDefinitionsComment,
                            SessionConfigurationUtils.CombineHashtableArray(funcHash, streamWriter), streamWriter, false));

                        foreach (Hashtable hashtable in funcHash)
                        {
                            if (!hashtable.ContainsKey(ConfigFileConstants.FunctionNameToken))
                            {
                                PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                    ConfigFileConstants.FunctionDefinitions, ConfigFileConstants.FunctionNameToken, path));
                                ThrowTerminatingError(e.ErrorRecord);
                            }

                            if (!hashtable.ContainsKey(ConfigFileConstants.FunctionValueToken))
                            {
                                PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                    ConfigFileConstants.FunctionDefinitions, ConfigFileConstants.FunctionValueToken, path));
                                ThrowTerminatingError(e.ErrorRecord);
                            }

                            if ((hashtable[ConfigFileConstants.FunctionValueToken] as ScriptBlock) == null)
                            {
                                PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCKeyMustBeScriptBlock,
                                    ConfigFileConstants.FunctionValueToken, ConfigFileConstants.FunctionDefinitions, path));
                                ThrowTerminatingError(e.ErrorRecord);
                            }

                            foreach (string functionKey in hashtable.Keys)
                            {
                                if (!String.Equals(functionKey, ConfigFileConstants.FunctionNameToken, StringComparison.OrdinalIgnoreCase) &&
                                    !String.Equals(functionKey, ConfigFileConstants.FunctionValueToken, StringComparison.OrdinalIgnoreCase) &&
                                    !String.Equals(functionKey, ConfigFileConstants.FunctionOptionsToken, StringComparison.OrdinalIgnoreCase))
                                {
                                    PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeContainsInvalidKey,
                                        functionKey, ConfigFileConstants.FunctionDefinitions, path));
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
                if (variableDefinitions == null)
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VariableDefinitions, RemotingErrorIdStrings.DISCVariableDefinitionsComment,
                        "@{ Name = 'Variable1'; Value = { 'Dynamic' + 'InitialValue' } }, @{ Name = 'Variable2'; Value = 'StaticInitialValue' }", streamWriter, true));
                }
                else
                {
                    string varString = variableDefinitions as string;

                    if (varString != null)
                    {
                        result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VariableDefinitions, RemotingErrorIdStrings.DISCVariableDefinitionsComment,
                            varString, streamWriter, false));
                    }
                    else
                    {
                        Hashtable[] varHash = DISCPowerShellConfiguration.TryGetHashtableArray(variableDefinitions);

                        if (varHash != null)
                        {
                            result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.VariableDefinitions, RemotingErrorIdStrings.DISCVariableDefinitionsComment,
                                SessionConfigurationUtils.CombineHashtableArray(varHash, streamWriter), streamWriter, false));

                            foreach (Hashtable hashtable in varHash)
                            {
                                if (!hashtable.ContainsKey(ConfigFileConstants.VariableNameToken))
                                {
                                    PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                        ConfigFileConstants.VariableDefinitions, ConfigFileConstants.VariableNameToken, path));
                                    ThrowTerminatingError(e.ErrorRecord);
                                }

                                if (!hashtable.ContainsKey(ConfigFileConstants.VariableValueToken))
                                {
                                    PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeMustContainKey,
                                        ConfigFileConstants.VariableDefinitions, ConfigFileConstants.VariableValueToken, path));
                                    ThrowTerminatingError(e.ErrorRecord);
                                }

                                foreach (string variableKey in hashtable.Keys)
                                {
                                    if (!String.Equals(variableKey, ConfigFileConstants.VariableNameToken, StringComparison.OrdinalIgnoreCase) &&
                                        !String.Equals(variableKey, ConfigFileConstants.VariableValueToken, StringComparison.OrdinalIgnoreCase))
                                    {
                                        PSArgumentException e = new PSArgumentException(StringUtil.Format(RemotingErrorIdStrings.DISCTypeContainsInvalidKey,
                                            variableKey, ConfigFileConstants.VariableDefinitions, path));
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
                if (environmentVariables == null)
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.EnvironmentVariables, RemotingErrorIdStrings.DISCEnvironmentVariablesComment,
                        "@{ Variable1 = 'Value1'; Variable2 = 'Value2' }",
                        streamWriter, true));
                }
                else
                {
                    result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.EnvironmentVariables, RemotingErrorIdStrings.DISCEnvironmentVariablesComment,
                        SessionConfigurationUtils.CombineHashtable(environmentVariables, streamWriter), streamWriter, false));
                }

                // Types to process
                resultData = (typesToProcess.Length > 0) ? SessionConfigurationUtils.CombineStringArray(typesToProcess) : "'C:\\ConfigData\\MyTypes.ps1xml', 'C:\\ConfigData\\OtherTypes.ps1xml'";
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.TypesToProcess, RemotingErrorIdStrings.DISCTypesToProcessComment,
                    resultData, streamWriter, (typesToProcess.Length == 0)));

                // Formats to process
                resultData = (formatsToProcess.Length > 0) ? SessionConfigurationUtils.CombineStringArray(formatsToProcess) : "'C:\\ConfigData\\MyFormats.ps1xml', 'C:\\ConfigData\\OtherFormats.ps1xml'";
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.FormatsToProcess, RemotingErrorIdStrings.DISCFormatsToProcessComment,
                    resultData, streamWriter, (formatsToProcess.Length == 0)));

                // Assemblies to load
                bool isExample = false;
                if ((assembliesToLoad == null) || (assembliesToLoad.Length == 0))
                {
                    isExample = true;
                    assembliesToLoad = new string[] { "System.Web", "System.OtherAssembly, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" };
                }
                result.Append(SessionConfigurationUtils.ConfigFragment(ConfigFileConstants.AssembliesToLoad, RemotingErrorIdStrings.DISCAssembliesToLoadComment,
                    SessionConfigurationUtils.CombineStringArray(assembliesToLoad), streamWriter, isExample));

                result.Append("}");

                streamWriter.Write(result.ToString());
            }
            finally
            {
                streamWriter.Dispose();
            }
        }

        #endregion
    }

    /// <summary>
    /// Utility methods for configuration file commands
    /// </summary>
    internal class SessionConfigurationUtils
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
                return string.Format(CultureInfo.InvariantCulture, "# {0}{1}# {2:19} = {3}{4}{5}",
                    resourceString, nl, key, value, nl, nl);
            }

            return string.Format(CultureInfo.InvariantCulture, "# {0}{1}{2:19} = {3}{4}{5}",
                resourceString, nl, key, value, nl, nl);
        }

        /// <summary>
        /// Return a single-quoted string. Any embedded single quotes will be doubled.
        /// </summary>
        /// <param name="name">The string to quote</param>
        /// <returns>The quoted string</returns>
        internal static string QuoteName(object name)
        {
            if (name == null)
                return "''";
            return "'" + System.Management.Automation.Language.CodeGeneration.EscapeSingleQuotedStringContent(name.ToString()) + "'";
        }

        /// <summary>
        /// Return a script block string wrapped in curly braces.
        /// </summary>
        /// <param name="sb">The string to wrap</param>
        /// <returns>The wrapped string</returns>
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
            if(booleanToEmit)
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
        /// Gets the visibility default value
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
        /// Combines a hashtable into a single string block
        /// </summary>
        internal static string CombineHashtable(IDictionary table, StreamWriter writer, int? indent = 0)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("@{");

            var keys = table.Keys.Cast<String>().OrderBy(x => x);
            foreach (var key in keys)
            {
                sb.Append(writer.NewLine);
                sb.AppendFormat("{0," + (4 * (indent + 1)) + "}", "");
                sb.Append(QuoteName(key));
                sb.Append(" = ");
                if ((table[key] as ScriptBlock) != null)
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
                for (int i = 0; i < values.Length; )
                {
                    WriteRequriedGroup(values[i++], sb);

                    if (i < values.Length)
                    {
                        sb.Append(", ");
                    }
                }
            }
            else
            {
                WriteRequriedGroup(keyObject, sb);
            }

            sb.Append(" }");

            return sb.ToString();
        }

        private static void WriteRequriedGroup(object value, StringBuilder sb)
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
        /// Combines an array of hashtables into a single string block
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
        /// Combines an array of strings into a single string block
        /// </summary>
        /// <param name="values">string values</param>
        /// <returns>string block</returns>
        internal static string CombineStringArray(string[] values)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < values.Length; i++)
            {
                if (!String.IsNullOrEmpty(values[i]))
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
        /// Combines an array of strings or hashtables into a single string block
        /// </summary>
        internal static string CombineHashTableOrStringArray(object[] values, StreamWriter writer, PSCmdlet caller)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                string strVal = values[i] as string;
                if (!String.IsNullOrEmpty(strVal))
                {
                    sb.Append(QuoteName(strVal));
                }
                else
                {
                    Hashtable hashVal = values[i] as Hashtable;
                    if (null == hashVal)
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
}