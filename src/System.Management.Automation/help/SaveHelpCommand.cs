// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Help;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements the Save-Help cmdlet.
    /// </summary>
    [Cmdlet(VerbsData.Save, "Help", DefaultParameterSetName = SaveHelpCommand.PathParameterSetName,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096794")]
    public sealed class SaveHelpCommand : UpdatableHelpCommandBase
    {
        #region Constructor

        /// <summary>
        /// Class constructor.
        /// </summary>
        public SaveHelpCommand() : base(UpdatableHelpCommandType.SaveHelpCommand)
        {
        }

        #endregion

        private bool _alreadyCheckedOncePerDayPerModule = false;

        #region Parameters

        /// <summary>
        /// Specifies the paths to save updates to.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PathParameterSetName)]
        [ValidateNotNull]
        [Alias("Path")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] DestinationPath
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
        [Parameter(Mandatory = true, ParameterSetName = LiteralPathParameterSetName)]
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
        /// Specifies the modules to update.
        /// </summary>
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = PathParameterSetName)]
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = LiteralPathParameterSetName)]
        [Alias("Name")]
        [ValidateNotNull]
        [ArgumentToModuleTransformation]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public PSModuleInfo[] Module { get; set; }

        /// <summary>
        /// Specifies the Module Specifications to update.
        /// </summary>
        [Parameter(ParameterSetName = PathParameterSetName, ValueFromPipelineByPropertyName = true)]
        [Parameter(ParameterSetName = LiteralPathParameterSetName, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ModuleSpecification[] FullyQualifiedModule { get; set; }

        #endregion

        #region Implementation

        /// <summary>
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

                List<string> moduleNames = null;
                List<PSModuleInfo> moduleInfos = null;

                if (Module != null)
                {
                    moduleNames = new List<string>();
                    moduleInfos = new List<PSModuleInfo>();

                    foreach (PSModuleInfo moduleInfo in Module)
                    {
                        // WinBlue: 268863
                        // this check will cover the cases where
                        // user supplied just name with -Module parameter.
                        // In other cases, user must have supplied either
                        // PSModuleInfo or Deserialized PSModuleInfo
                        if (string.IsNullOrEmpty(moduleInfo.ModuleBase))
                        {
                            moduleNames.Add(moduleInfo.Name);
                        }
                        else
                        {
                            moduleInfos.Add(moduleInfo);
                        }
                    }
                }

                base.Process(moduleNames, FullyQualifiedModule);
                base.Process(moduleInfos);
            }
            finally
            {
                ProgressRecord progress = new ProgressRecord(activityId, HelpDisplayStrings.SaveProgressActivityForModule, HelpDisplayStrings.UpdateProgressInstalling);

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
            Collection<string> resolvedPaths = new Collection<string>();

            // Search for the HelpInfo XML
            foreach (string path in _path)
            {
                UpdatableHelpSystemDrive helpInfoDrive = null;

                try
                {
                    if (string.IsNullOrEmpty(path))
                    {
                        PSArgumentException e = new PSArgumentException(StringUtil.Format(HelpDisplayStrings.PathNullOrEmpty));
                        WriteError(e.ErrorRecord);
                        return false;
                    }

                    string destPath = path;

                    if (_credential != null)
                    {
                        if (path.Contains('*'))
                        {
                            // Deal with wildcards

                            int index = path.IndexOf('*');

                            if (index == 0)
                            {
                                throw new UpdatableHelpSystemException("PathMustBeValidContainers",
                                    StringUtil.Format(HelpDisplayStrings.PathMustBeValidContainers, path), ErrorCategory.InvalidArgument,
                                    null, new ItemNotFoundException());
                            }
                            else
                            {
                                int i = index;
                                for (; i >= 0; i--)
                                {
                                    if (path[i].Equals('/') || path[i].Equals('\\'))
                                    {
                                        break;
                                    }
                                }

                                if (i == 0)
                                {
                                    throw new UpdatableHelpSystemException("PathMustBeValidContainers",
                                        StringUtil.Format(HelpDisplayStrings.PathMustBeValidContainers, path), ErrorCategory.InvalidArgument,
                                        null, new ItemNotFoundException());
                                }

                                helpInfoDrive = new UpdatableHelpSystemDrive(this, path.Substring(0, i), _credential);
                                destPath = Path.Combine(helpInfoDrive.DriveName, path.Substring(i + 1, path.Length - (i + 1)));
                            }
                        }
                        else
                        {
                            helpInfoDrive = new UpdatableHelpSystemDrive(this, path, _credential);
                            destPath = helpInfoDrive.DriveName;
                        }
                    }

                    if (_isLiteralPath)
                    {
                        string destinationPath = GetUnresolvedProviderPathFromPSPath(destPath);
                        if (!Directory.Exists(destinationPath))
                        {
                            throw new UpdatableHelpSystemException("PathMustBeValidContainers",
                                StringUtil.Format(HelpDisplayStrings.PathMustBeValidContainers, path), ErrorCategory.InvalidArgument,
                                null, new ItemNotFoundException());
                        }

                        resolvedPaths.Add(destinationPath);
                    }
                    else
                    {
                        try
                        {
                            // Expand wildcard characters
                            foreach (string tempPath in ResolvePath(destPath, false, false))
                            {
                                resolvedPaths.Add(tempPath);
                            }
                        }
                        catch (ItemNotFoundException e)
                        {
                            throw new UpdatableHelpSystemException("PathMustBeValidContainers",
                                StringUtil.Format(HelpDisplayStrings.PathMustBeValidContainers, path), ErrorCategory.InvalidArgument, null, e);
                        }
                    }
                }
                finally
                {
                    helpInfoDrive?.Dispose();
                }
            }

            if (resolvedPaths.Count == 0)
            {
                return true;
            }

            bool installed = false;

            foreach (string path in resolvedPaths)
            {
                UpdatableHelpInfo currentHelpInfo = null;
                UpdatableHelpInfo newHelpInfo = null;
                string helpInfoUri = null;

                // if -force is specified, no need to read from the current HelpInfo.xml file
                // because it won't be used for checking "IsUpdateNecessary"
                string xml = _force
                                 ? null
                                 : UpdatableHelpSystem.LoadStringFromPath(this,
                                                                          SessionState.Path.Combine(path, module.GetHelpInfoName()),
                                                                          _credential);

                if (xml != null)
                {
                    // constructing the helpinfo object from previous update help log xml..
                    // no need to resolve the uri's in this case.
                    currentHelpInfo = _helpSystem.CreateHelpInfo(xml, module.ModuleName, module.ModuleGuid,
                                                                 currentCulture: null, pathOverride: null, verbose: false,
                                                                 shouldResolveUri: false, ignoreValidationException: false);
                }

                // Don't update too frequently
                if (!_alreadyCheckedOncePerDayPerModule && !CheckOncePerDayPerModule(module.ModuleName, path, module.GetHelpInfoName(), DateTime.UtcNow, _force))
                {
                    return true;
                }

                _alreadyCheckedOncePerDayPerModule = true;

                // Form the actual HelpInfo.xml uri
                helpInfoUri = _helpSystem.GetHelpInfoUri(module, null).ResolvedUri;
                string uri = helpInfoUri + module.GetHelpInfoName();

                newHelpInfo = _helpSystem.GetHelpInfo(_commandType, uri, module.ModuleName, module.ModuleGuid, culture);

                if (newHelpInfo == null)
                {
                    throw new UpdatableHelpSystemException("UnableToRetrieveHelpInfoXml",
                        StringUtil.Format(HelpDisplayStrings.UnableToRetrieveHelpInfoXml, culture), ErrorCategory.ResourceUnavailable,
                        null, null);
                }

                string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
                foreach (UpdatableHelpUri contentUri in newHelpInfo.HelpContentUriCollection)
                {
                    if (!IsUpdateNecessary(module, currentHelpInfo, newHelpInfo, contentUri.Culture, _force))
                    {
                        WriteVerbose(StringUtil.Format(HelpDisplayStrings.SuccessfullyUpdatedHelpContent, module.ModuleName, HelpDisplayStrings.NewestContentAlreadyDownloaded,
                            contentUri.Culture.Name, newHelpInfo.GetCultureVersion(contentUri.Culture)));

                        installed = true;
                        continue;
                    }
                    else
                    {
                        Debug.Assert(helpInfoUri != null, "If we are here, helpInfoUri must not be null");

                        string helpContentUri = contentUri.ResolvedUri;
                        string helpContentName = module.GetHelpContentName(contentUri.Culture);

                        UpdatableHelpSystemDrive helpContentDrive = null;

                        try
                        {
                            if (Directory.Exists(helpContentUri))
                            {
                                File.Copy(SessionState.Path.Combine(helpContentUri, helpContentName),
                                    SessionState.Path.Combine(path, helpContentName), true);
                            }
                            else
                            {
                                // Remote

                                if (_credential != null)
                                {
                                    try
                                    {
                                        helpContentDrive = new UpdatableHelpSystemDrive(this, path, _credential);

                                        if (!_helpSystem.DownloadHelpContent(_commandType, tempPath, helpContentUri, helpContentName, culture))
                                        {
                                            installed = false;
                                            continue;
                                        }

                                        InvokeProvider.Item.Copy(new string[1] { tempPath }, helpContentDrive.DriveName, true, CopyContainers.CopyChildrenOfTargetContainer,
                                            true, true);
                                    }
                                    catch (Exception e)
                                    {
                                        ProcessException(module.ModuleName, contentUri.Culture.Name, e);
                                        installed = false;
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (!_helpSystem.DownloadHelpContent(_commandType, path, helpContentUri, helpContentName, culture))
                                    {
                                        installed = false;
                                        continue;
                                    }
                                }
                            }

                            if (_credential != null)
                            {
                                _helpSystem.GenerateHelpInfo(module.ModuleName, module.ModuleGuid, newHelpInfo.UnresolvedUri, contentUri.Culture.Name, newHelpInfo.GetCultureVersion(contentUri.Culture), tempPath,
                                    module.GetHelpInfoName(), _force);

                                InvokeProvider.Item.Copy(new string[1] { Path.Combine(tempPath, module.GetHelpInfoName()) }, Path.Combine(helpContentDrive.DriveName, module.GetHelpInfoName()), false,
                                    CopyContainers.CopyTargetContainer, true, true);
                            }
                            else
                            {
                                _helpSystem.GenerateHelpInfo(module.ModuleName, module.ModuleGuid, newHelpInfo.UnresolvedUri, contentUri.Culture.Name, newHelpInfo.GetCultureVersion(contentUri.Culture), path,
                                    module.GetHelpInfoName(), _force);
                            }

                            WriteVerbose(StringUtil.Format(HelpDisplayStrings.SuccessfullyUpdatedHelpContent, module.ModuleName,
                                StringUtil.Format(HelpDisplayStrings.SavedHelpContent, System.IO.Path.Combine(path, helpContentName)), contentUri.Culture.Name,
                                newHelpInfo.GetCultureVersion(contentUri.Culture)));

                            LogMessage(StringUtil.Format(HelpDisplayStrings.SaveHelpCompleted, path));
                        }
                        catch (Exception e)
                        {
                            ProcessException(module.ModuleName, contentUri.Culture.Name, e);
                        }
                        finally
                        {
                            helpContentDrive?.Dispose();
                        }
                    }
                }

                installed = true;
            }

            return installed;
        }

        #endregion
    }

    internal sealed class ArgumentToModuleTransformationAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            object argument = PSObject.Base(inputData);

            // deal with scalar string argument
            var strArg = argument as string;
            if (strArg != null)
            {
                return new PSModuleInfo(name: strArg, path: null, context: null, sessionState: null);
            }

            // deal with IList argument
            IList iListArg = ParameterBinderBase.GetIList(argument);
            if (iListArg != null && iListArg.Count > 0)
            {
                int elementCount = iListArg.Count;
                int targetIndex = 0;
                var target = Array.CreateInstance(typeof(object), elementCount);

                foreach (object element in iListArg)
                {
                    var elementValue = PSObject.Base(element);

                    if (elementValue is PSModuleInfo)
                    {
                        target.SetValue(elementValue, targetIndex++);
                    }
                    else if (elementValue is string)
                    {
                        var elementAsModuleObj = new PSModuleInfo(name: (string)elementValue, path: null, context: null, sessionState: null);
                        target.SetValue(elementAsModuleObj, targetIndex++);
                    }
                    else
                    {
                        PSModuleInfo elementValueModuleInfo = null;
                        if (TryConvertFromDeserializedModuleInfo(elementValue, out elementValueModuleInfo))
                        {
                            target.SetValue(elementValueModuleInfo, targetIndex++);
                        }
                        else
                        {
                            target.SetValue(element, targetIndex++);
                        }
                    }
                }

                return target;
            }

            PSModuleInfo moduleInfo = null;
            if (TryConvertFromDeserializedModuleInfo(inputData, out moduleInfo))
            {
                return moduleInfo;
            }

            return inputData;
        }

        private static bool TryConvertFromDeserializedModuleInfo(object inputData, out PSModuleInfo moduleInfo)
        {
            moduleInfo = null;
            PSObject pso = inputData as PSObject;
            if (Deserializer.IsDeserializedInstanceOfType(pso, typeof(PSModuleInfo)))
            {
                string moduleName;
                LanguagePrimitives.TryConvertTo<string>(pso.Properties["Name"].Value, out moduleName);

                Guid moduleGuid;
                LanguagePrimitives.TryConvertTo<Guid>(pso.Properties["Guid"].Value, out moduleGuid);

                Version moduleVersion;
                LanguagePrimitives.TryConvertTo<Version>(pso.Properties["Version"].Value, out moduleVersion);

                string helpInfoUri;
                LanguagePrimitives.TryConvertTo<string>(pso.Properties["HelpInfoUri"].Value, out helpInfoUri);

                moduleInfo = new PSModuleInfo(name: moduleName, path: null, context: null, sessionState: null);
                moduleInfo.SetGuid(moduleGuid);
                moduleInfo.SetVersion(moduleVersion);
                moduleInfo.SetHelpInfoUri(helpInfoUri);
                // setting the base to temp directory as this is a deserialized
                // module info.
                moduleInfo.SetModuleBase(System.IO.Path.GetTempPath());

                return true;
            }

            return false;
        }
    }
}
