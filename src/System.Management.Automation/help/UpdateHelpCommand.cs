// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Help;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements the Update-Help cmdlet.
    /// </summary>
    [Cmdlet(VerbsData.Update, "Help", DefaultParameterSetName = PathParameterSetName,
        SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096805")]
    public sealed class UpdateHelpCommand : UpdatableHelpCommandBase
    {
        #region Constructor

        /// <summary>
        /// Class constructor.
        /// </summary>
        public UpdateHelpCommand() : base(UpdatableHelpCommandType.UpdateHelpCommand)
        {
        }

        #endregion

        private bool _alreadyCheckedOncePerDayPerModule = false;

        #region Parameters

        /// <summary>
        /// Specifies the modules to update.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = PathParameterSetName, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = LiteralPathParameterSetName, ValueFromPipelineByPropertyName = true)]
        [Alias("Name")]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Module
        {
            get
            {
                return _module;
            }

            set
            {
                _module = value;
            }
        }

        private string[] _module;

        /// <summary>
        /// Specifies the Module Specifications to update.
        /// </summary>
        [Parameter(ParameterSetName = PathParameterSetName, ValueFromPipelineByPropertyName = true)]
        [Parameter(ParameterSetName = LiteralPathParameterSetName, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ModuleSpecification[] FullyQualifiedModule { get; set; }

        /// <summary>
        /// Specifies the paths to update from.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = PathParameterSetName)]
        [ValidateNotNull]
        [Alias("Path")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] SourcePath
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

        private string[] _path;

        /// <summary>
        /// Specifies the literal path to save updates to.
        /// </summary>
        [Parameter(ParameterSetName = LiteralPathParameterSetName, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] LiteralPath
        {
            get
            {
                return _path;
            }

            set
            {
                _path = value;
                _isLiteralPath = true;
            }
        }

        private bool _isLiteralPath = false;

        /// <summary>
        /// Scans paths recursively.
        /// </summary>
        [Parameter]
        public SwitchParameter Recurse
        {
            get
            {
                return _recurse;
            }

            set
            {
                _recurse = value;
            }
        }

        private bool _recurse;

        #endregion

        private bool _isInitialized = false;

        #region Implementation

        /// <summary>
        /// Begin processing.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (_path == null)
            {
                // Pull default source path from GP
                string defaultSourcePath = _helpSystem.GetDefaultSourcePath();

                if (defaultSourcePath != null)
                {
                    _path = new string[1] { defaultSourcePath };
                }
            }
        }

        /// <summary>
        /// Main cmdlet logic.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                // Module and FullyQualifiedModule should not be specified at the same time.
                // Throw out terminating error if this is the case.
                if (Module != null && FullyQualifiedModule != null)
                {
                    string errMsg = StringUtil.Format(SessionStateStrings.GetContent_TailAndHeadCannotCoexist, "Module", "FullyQualifiedModule");
                    ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "ModuleAndFullyQualifiedModuleCannotBeSpecifiedTogether", ErrorCategory.InvalidOperation, null);
                    ThrowTerminatingError(error);
                }

                if (!_isInitialized)
                {
                    if (_path == null && Recurse.IsPresent)
                    {
                        PSArgumentException e = new PSArgumentException(StringUtil.Format(HelpDisplayStrings.CannotSpecifyRecurseWithoutPath));
                        ThrowTerminatingError(e.ErrorRecord);
                    }

                    _isInitialized = true;
                }

                base.Process(_module, FullyQualifiedModule);

                // Reset the per-runspace help cache
                foreach (HelpProvider provider in Context.HelpSystem.HelpProviders)
                {
                    if (_stopping)
                    {
                        break;
                    }

                    provider.Reset();
                }
            }
            finally
            {
                ProgressRecord progress = new ProgressRecord(activityId, HelpDisplayStrings.UpdateProgressActivityForModule, HelpDisplayStrings.UpdateProgressInstalling);

                progress.PercentComplete = 100;
                progress.RecordType = ProgressRecordType.Completed;

                WriteProgress(progress);
            }
        }

        /// <summary>
        /// Process a single module with a given culture.
        /// </summary>
        /// <param name="module">Module to process.</param>
        /// <param name="culture">Culture to use.</param>
        /// <returns>True if the module has been processed, false if not.</returns>
        internal override bool ProcessModuleWithCulture(UpdatableHelpModuleInfo module, string culture)
        {
            // Simulate culture not found
            if (InternalTestHooks.ThrowHelpCultureNotSupported)
            {
                throw new UpdatableHelpSystemException("HelpCultureNotSupported",
                    StringUtil.Format(HelpDisplayStrings.HelpCultureNotSupported, culture, "en-US"),
                    ErrorCategory.InvalidOperation, null, null);
            }

            UpdatableHelpInfo currentHelpInfo = null;
            UpdatableHelpInfo newHelpInfo = null;
            string helpInfoUri = null;

            string moduleBase = module.ModuleBase;

            if (this.Scope == UpdateHelpScope.CurrentUser)
            {
                moduleBase = HelpUtils.GetModuleBaseForUserHelp(moduleBase, module.ModuleName);
            }

            // reading the xml file even if force is specified
            // Reason: we need the current version for ShouldProcess
            string xml = UpdatableHelpSystem.LoadStringFromPath(this,
                 SessionState.Path.Combine(moduleBase, module.GetHelpInfoName()),
                 null);

            if (xml != null)
            {
                // constructing the helpinfo object from previous update help log xml..
                // no need to resolve the uri's in this case.
                currentHelpInfo = _helpSystem.CreateHelpInfo(xml, module.ModuleName, module.ModuleGuid,
                                                             currentCulture: null, pathOverride: null, verbose: false,
                                                             shouldResolveUri: false,
                                                             // ignore validation exception if _force is true
                                                             ignoreValidationException: _force);
            }

            // Don't update too frequently
            if (!_alreadyCheckedOncePerDayPerModule && !CheckOncePerDayPerModule(module.ModuleName, moduleBase, module.GetHelpInfoName(), DateTime.UtcNow, _force))
            {
                return true;
            }

            _alreadyCheckedOncePerDayPerModule = true;

            if (_path != null)
            {
                UpdatableHelpSystemDrive helpInfoDrive = null;
                try
                {
                    Collection<string> resolvedPaths = new Collection<string>();

                    // Search for the HelpInfo XML
                    foreach (string path in _path)
                    {
                        if (string.IsNullOrEmpty(path))
                        {
                            PSArgumentException e = new PSArgumentException(StringUtil.Format(HelpDisplayStrings.PathNullOrEmpty));
                            WriteError(e.ErrorRecord);
                            return false;
                        }

                        try
                        {
                            string sourcePath = path;

                            if (_credential != null)
                            {
                                UpdatableHelpSystemDrive drive = new UpdatableHelpSystemDrive(this, path, _credential);
                                sourcePath = drive.DriveName;
                            }

                            // Expand wildcard characters
                            foreach (string tempPath in ResolvePath(sourcePath, _recurse, _isLiteralPath))
                            {
                                resolvedPaths.Add(tempPath);
                            }
                        }
                        catch (System.Management.Automation.DriveNotFoundException e)
                        {
                            ThrowPathMustBeValidContainersException(path, e);
                        }
                        catch (ItemNotFoundException e)
                        {
                            ThrowPathMustBeValidContainersException(path, e);
                        }
                    }

                    if (resolvedPaths.Count == 0)
                    {
                        return true;
                    }

                    // Everything in resolvedPaths is a container
                    foreach (string resolvedPath in resolvedPaths)
                    {
                        string literalPath = SessionState.Path.Combine(resolvedPath, module.GetHelpInfoName());

                        xml = UpdatableHelpSystem.LoadStringFromPath(this, literalPath, _credential);

                        if (xml != null)
                        {
                            newHelpInfo = _helpSystem.CreateHelpInfo(xml, module.ModuleName, module.ModuleGuid, culture, resolvedPath,
                                                                     verbose: false, shouldResolveUri: true, ignoreValidationException: false);
                            helpInfoUri = resolvedPath;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new UpdatableHelpSystemException("UnableToRetrieveHelpInfoXml",
                        StringUtil.Format(HelpDisplayStrings.UnableToRetrieveHelpInfoXml, culture), ErrorCategory.ResourceUnavailable,
                        null, e);
                }
                finally
                {
                    helpInfoDrive?.Dispose();
                }
            }
            else
            {
                // Form the actual HelpInfo.xml uri
                helpInfoUri = _helpSystem.GetHelpInfoUri(module, null).ResolvedUri;
                string uri = helpInfoUri + module.GetHelpInfoName();

                newHelpInfo = _helpSystem.GetHelpInfo(UpdatableHelpCommandType.UpdateHelpCommand, uri, module.ModuleName, module.ModuleGuid, culture);
            }

            if (newHelpInfo == null)
            {
                throw new UpdatableHelpSystemException("UnableToRetrieveHelpInfoXml",
                    StringUtil.Format(HelpDisplayStrings.UnableToRetrieveHelpInfoXml, culture), ErrorCategory.ResourceUnavailable,
                    null, null);
            }

            bool installed = false;

            foreach (UpdatableHelpUri contentUri in newHelpInfo.HelpContentUriCollection)
            {
                Version currentHelpVersion = currentHelpInfo?.GetCultureVersion(contentUri.Culture);
                string updateHelpShouldProcessAction = string.Format(CultureInfo.InvariantCulture,
                    HelpDisplayStrings.UpdateHelpShouldProcessActionMessage,
                    module.ModuleName,
                    (currentHelpVersion != null) ? currentHelpVersion.ToString() : "0.0.0.0",
                    newHelpInfo.GetCultureVersion(contentUri.Culture),
                    contentUri.Culture);
                if (!this.ShouldProcess(updateHelpShouldProcessAction, "Update-Help"))
                {
                    continue;
                }

                if (Utils.IsUnderProductFolder(moduleBase) && (!Utils.IsAdministrator()))
                {
                    string message = StringUtil.Format(HelpErrors.UpdatableHelpRequiresElevation);
                    ProcessException(module.ModuleName, null, new UpdatableHelpSystemException("UpdatableHelpSystemRequiresElevation",
                            message, ErrorCategory.InvalidOperation, null, null));
                    return false;
                }

                if (!IsUpdateNecessary(module, _force ? null : currentHelpInfo, newHelpInfo, contentUri.Culture, _force))
                {
                    WriteVerbose(StringUtil.Format(HelpDisplayStrings.SuccessfullyUpdatedHelpContent, module.ModuleName, HelpDisplayStrings.NewestContentAlreadyInstalled,
                        contentUri.Culture.Name, newHelpInfo.GetCultureVersion(contentUri.Culture)));

                    installed = true;
                    continue;
                }
                else
                {
                    try
                    {
                        Debug.Assert(helpInfoUri != null, "If we are here, helpInfoUri must not be null");

                        string helpContentUri = contentUri.ResolvedUri;
                        string xsdPath = SessionState.Path.Combine(Utils.GetApplicationBase(), "Schemas\\PSMaml\\maml.xsd"); // TODO: Edit the maml XSDs and change this

                        // Gather destination paths
                        Collection<string> destPaths = new Collection<string>();

                        if (!Directory.Exists(moduleBase))
                        {
                            Directory.CreateDirectory(moduleBase);
                        }

                        destPaths.Add(moduleBase);

#if !CORECLR // Side-By-Side directories are not present in OneCore environments.
                        if (IsSystemModule(module.ModuleName) && Environment.Is64BitOperatingSystem)
                        {
                            string path = Utils.DefaultPowerShellAppBase.Replace("System32", "SysWOW64");

                            destPaths.Add(path);
                        }
#endif

                        Collection<string> filesInstalled;

                        if (Directory.Exists(helpContentUri))
                        {
                            if (_credential != null)
                            {
                                string helpContentName = module.GetHelpContentName(contentUri.Culture);
                                string tempContentPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));

                                try
                                {
                                    using (UpdatableHelpSystemDrive drive = new UpdatableHelpSystemDrive(this, helpContentUri, _credential))
                                    {
                                        if (!Directory.Exists(tempContentPath))
                                        {
                                            Directory.CreateDirectory(tempContentPath);
                                        }

                                        InvokeProvider.Item.Copy(new string[1] { Path.Combine(drive.DriveName, helpContentName) },
                                            Path.Combine(tempContentPath, helpContentName), false, CopyContainers.CopyTargetContainer, true, true);

                                        // Local
                                        _helpSystem.InstallHelpContent(UpdatableHelpCommandType.UpdateHelpCommand, Context, tempContentPath,
                                            destPaths, module.GetHelpContentName(contentUri.Culture),
                                            Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName())),
                                            contentUri.Culture, xsdPath, out filesInstalled);
                                    }
                                }
                                catch (Exception e)
                                {
                                    throw new UpdatableHelpSystemException("HelpContentNotFound", StringUtil.Format(HelpDisplayStrings.HelpContentNotFound),
                                        ErrorCategory.ResourceUnavailable, null, e);
                                }
                            }
                            else
                            {
                                _helpSystem.InstallHelpContent(UpdatableHelpCommandType.UpdateHelpCommand, Context, helpContentUri,
                                    destPaths, module.GetHelpContentName(contentUri.Culture),
                                    Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName())),
                                    contentUri.Culture, xsdPath, out filesInstalled);
                            }
                        }
                        else
                        {
                            // Remote

                            // Download and install help content
                            if (!_helpSystem.DownloadAndInstallHelpContent(UpdatableHelpCommandType.UpdateHelpCommand, Context,
                                destPaths, module.GetHelpContentName(contentUri.Culture), contentUri.Culture, helpContentUri, xsdPath, out filesInstalled))
                            {
                                installed = false;
                                continue;
                            }
                        }

                        _helpSystem.GenerateHelpInfo(module.ModuleName, module.ModuleGuid, newHelpInfo.UnresolvedUri, contentUri.Culture.Name, newHelpInfo.GetCultureVersion(contentUri.Culture),
                            moduleBase, module.GetHelpInfoName(), _force);

                        foreach (string fileInstalled in filesInstalled)
                        {
                            WriteVerbose(StringUtil.Format(HelpDisplayStrings.SuccessfullyUpdatedHelpContent, module.ModuleName,
                                StringUtil.Format(HelpDisplayStrings.UpdatedHelpContent, fileInstalled), contentUri.Culture.Name,
                                newHelpInfo.GetCultureVersion(contentUri.Culture)));
                        }

                        LogMessage(StringUtil.Format(HelpDisplayStrings.UpdateHelpCompleted));

                        installed = true;
                    }
                    catch (Exception e)
                    {
                        ProcessException(module.ModuleName, contentUri.Culture.Name, e);
                    }
                }
            }

            return installed;
        }

        /// <summary>
        /// Throws PathMustBeValidContainers exception.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="e"></param>
        private static void ThrowPathMustBeValidContainersException(string path, Exception e)
        {
            throw new UpdatableHelpSystemException("PathMustBeValidContainers",
                StringUtil.Format(HelpDisplayStrings.PathMustBeValidContainers, path), ErrorCategory.InvalidArgument,
                null, e);
        }

        #endregion
    }
}
