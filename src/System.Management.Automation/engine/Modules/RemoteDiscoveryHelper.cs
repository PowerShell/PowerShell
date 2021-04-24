// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.PowerShell;
using Microsoft.PowerShell.Commands;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    internal static class RemoteDiscoveryHelper
    {
        #region PSRP

        private static Collection<string> RehydrateHashtableKeys(PSObject pso, string propertyName)
        {
            const DeserializingTypeConverter.RehydrationFlags rehydrationFlags = DeserializingTypeConverter.RehydrationFlags.NullValueOk |
                                   DeserializingTypeConverter.RehydrationFlags.MissingPropertyOk;
            Hashtable hashtable = DeserializingTypeConverter.GetPropertyValue<Hashtable>(pso, propertyName, rehydrationFlags);
            if (hashtable == null)
            {
                return new Collection<string>();
            }
            else
            {
                List<string> list = hashtable
                    .Keys
                    .Cast<object>()
                    .Where(static k => k != null)
                    .Select(static k => k.ToString())
                    .Where(static s => s != null)
                    .ToList();
                return new Collection<string>(list);
            }
        }

        internal static PSModuleInfo RehydratePSModuleInfo(PSObject deserializedModuleInfo)
        {
            const DeserializingTypeConverter.RehydrationFlags rehydrationFlags = DeserializingTypeConverter.RehydrationFlags.NullValueOk |
                                   DeserializingTypeConverter.RehydrationFlags.MissingPropertyOk;
            string name = DeserializingTypeConverter.GetPropertyValue<string>(deserializedModuleInfo, "Name", rehydrationFlags);
            string path = DeserializingTypeConverter.GetPropertyValue<string>(deserializedModuleInfo, "Path", rehydrationFlags);
            PSModuleInfo moduleInfo = new PSModuleInfo(name, path, context: null, sessionState: null);

            moduleInfo.SetGuid(DeserializingTypeConverter.GetPropertyValue<Guid>(deserializedModuleInfo, "Guid", rehydrationFlags));
            moduleInfo.SetModuleType(DeserializingTypeConverter.GetPropertyValue<ModuleType>(deserializedModuleInfo, "ModuleType", rehydrationFlags));
            moduleInfo.SetVersion(DeserializingTypeConverter.GetPropertyValue<Version>(deserializedModuleInfo, "Version", rehydrationFlags));
            moduleInfo.SetHelpInfoUri(DeserializingTypeConverter.GetPropertyValue<string>(deserializedModuleInfo, "HelpInfoUri", rehydrationFlags));

            moduleInfo.AccessMode = DeserializingTypeConverter.GetPropertyValue<ModuleAccessMode>(deserializedModuleInfo, "AccessMode", rehydrationFlags);
            moduleInfo.Author = DeserializingTypeConverter.GetPropertyValue<string>(deserializedModuleInfo, "Author", rehydrationFlags);
            moduleInfo.ClrVersion = DeserializingTypeConverter.GetPropertyValue<Version>(deserializedModuleInfo, "ClrVersion", rehydrationFlags);
            moduleInfo.CompanyName = DeserializingTypeConverter.GetPropertyValue<string>(deserializedModuleInfo, "CompanyName", rehydrationFlags);
            moduleInfo.Copyright = DeserializingTypeConverter.GetPropertyValue<string>(deserializedModuleInfo, "Copyright", rehydrationFlags);
            moduleInfo.Description = DeserializingTypeConverter.GetPropertyValue<string>(deserializedModuleInfo, "Description", rehydrationFlags);
            moduleInfo.DotNetFrameworkVersion = DeserializingTypeConverter.GetPropertyValue<Version>(deserializedModuleInfo, "DotNetFrameworkVersion", rehydrationFlags);
            moduleInfo.PowerShellHostName = DeserializingTypeConverter.GetPropertyValue<string>(deserializedModuleInfo, "PowerShellHostName", rehydrationFlags);
            moduleInfo.PowerShellHostVersion = DeserializingTypeConverter.GetPropertyValue<Version>(deserializedModuleInfo, "PowerShellHostVersion", rehydrationFlags);
            moduleInfo.PowerShellVersion = DeserializingTypeConverter.GetPropertyValue<Version>(deserializedModuleInfo, "PowerShellVersion", rehydrationFlags);
            moduleInfo.ProcessorArchitecture = DeserializingTypeConverter.GetPropertyValue<Reflection.ProcessorArchitecture>(deserializedModuleInfo, "ProcessorArchitecture", rehydrationFlags);

            moduleInfo.DeclaredAliasExports = RehydrateHashtableKeys(deserializedModuleInfo, "ExportedAliases");
            moduleInfo.DeclaredCmdletExports = RehydrateHashtableKeys(deserializedModuleInfo, "ExportedCmdlets");
            moduleInfo.DeclaredFunctionExports = RehydrateHashtableKeys(deserializedModuleInfo, "ExportedFunctions");
            moduleInfo.DeclaredVariableExports = RehydrateHashtableKeys(deserializedModuleInfo, "ExportedVariables");

            var compatiblePSEditions = DeserializingTypeConverter.GetPropertyValue<string[]>(deserializedModuleInfo, "CompatiblePSEditions", rehydrationFlags);
            if (compatiblePSEditions != null && compatiblePSEditions.Length > 0)
            {
                foreach (var edition in compatiblePSEditions)
                {
                    moduleInfo.AddToCompatiblePSEditions(edition);
                }
            }

            // PowerShellGet related properties
            var tags = DeserializingTypeConverter.GetPropertyValue<string[]>(deserializedModuleInfo, "Tags", rehydrationFlags);
            if (tags != null && tags.Length > 0)
            {
                foreach (var tag in tags)
                {
                    moduleInfo.AddToTags(tag);
                }
            }

            moduleInfo.ReleaseNotes = DeserializingTypeConverter.GetPropertyValue<string>(deserializedModuleInfo, "ReleaseNotes", rehydrationFlags);
            moduleInfo.ProjectUri = DeserializingTypeConverter.GetPropertyValue<Uri>(deserializedModuleInfo, "ProjectUri", rehydrationFlags);
            moduleInfo.LicenseUri = DeserializingTypeConverter.GetPropertyValue<Uri>(deserializedModuleInfo, "LicenseUri", rehydrationFlags);
            moduleInfo.IconUri = DeserializingTypeConverter.GetPropertyValue<Uri>(deserializedModuleInfo, "IconUri", rehydrationFlags);
            moduleInfo.RepositorySourceLocation = DeserializingTypeConverter.GetPropertyValue<Uri>(deserializedModuleInfo, "RepositorySourceLocation", rehydrationFlags);

            return moduleInfo;
        }

        private static EventHandler<DataAddedEventArgs> GetStreamForwarder<T>(Action<T> forwardingAction, bool swallowInvalidOperationExceptions = false)
        {
            // TODO/FIXME: ETW event for extended semantics streams
            return (object sender, DataAddedEventArgs eventArgs) =>
            {
                var psDataCollection = (PSDataCollection<T>)sender;
                foreach (T t in psDataCollection.ReadAll())
                {
                    try
                    {
                        forwardingAction(t);
                    }
                    catch (InvalidOperationException)
                    {
                        if (!swallowInvalidOperationExceptions)
                        {
                            throw;
                        }
                    }
                }
            };
        }

        // This is a static field (instead of a constant) to make it possible to set through tests (and/or by customers if needed for a workaround)
        private static readonly int s_blockingCollectionCapacity = 1000;

        private static IEnumerable<PSObject> InvokeTopLevelPowerShell(
            PowerShell powerShell,
            PSCmdlet cmdlet,
            PSInvocationSettings invocationSettings,
            string errorMessageTemplate,
            CancellationToken cancellationToken)
        {
            using (var mergedOutput = new BlockingCollection<Func<PSCmdlet, IEnumerable<PSObject>>>(s_blockingCollectionCapacity))
            {
                var asyncOutput = new PSDataCollection<PSObject>();
                EventHandler<DataAddedEventArgs> outputHandler = GetStreamForwarder<PSObject>(
                    output => mergedOutput.Add(_ => new[] { output }),
                    swallowInvalidOperationExceptions: true);

                EventHandler<DataAddedEventArgs> errorHandler = GetStreamForwarder<ErrorRecord>(
                    errorRecord => mergedOutput.Add(
                        (PSCmdlet c) =>
                        {
                            errorRecord = GetErrorRecordForRemotePipelineInvocation(errorRecord, errorMessageTemplate);
                            HandleErrorFromPipeline(c, errorRecord, powerShell);
                            return Enumerable.Empty<PSObject>();
                        }),
                     swallowInvalidOperationExceptions: true);

                EventHandler<DataAddedEventArgs> warningHandler = GetStreamForwarder<WarningRecord>(
                    warningRecord => mergedOutput.Add(
                        (PSCmdlet c) =>
                        {
                            c.WriteWarning(warningRecord.Message);
                            return Enumerable.Empty<PSObject>();
                        }),
                     swallowInvalidOperationExceptions: true);

                EventHandler<DataAddedEventArgs> verboseHandler = GetStreamForwarder<VerboseRecord>(
                    verboseRecord => mergedOutput.Add(
                        (PSCmdlet c) =>
                        {
                            c.WriteVerbose(verboseRecord.Message);
                            return Enumerable.Empty<PSObject>();
                        }),
                     swallowInvalidOperationExceptions: true);

                EventHandler<DataAddedEventArgs> debugHandler = GetStreamForwarder<DebugRecord>(
                    debugRecord => mergedOutput.Add(
                        (PSCmdlet c) =>
                        {
                            c.WriteDebug(debugRecord.Message);
                            return Enumerable.Empty<PSObject>();
                        }),
                     swallowInvalidOperationExceptions: true);

                EventHandler<DataAddedEventArgs> informationHandler = GetStreamForwarder<InformationRecord>(
                    informationRecord => mergedOutput.Add(
                        (PSCmdlet c) =>
                        {
                            c.WriteInformation(informationRecord);
                            return Enumerable.Empty<PSObject>();
                        }),
                     swallowInvalidOperationExceptions: true);

                asyncOutput.DataAdded += outputHandler;
                powerShell.Streams.Error.DataAdded += errorHandler;
                powerShell.Streams.Warning.DataAdded += warningHandler;
                powerShell.Streams.Verbose.DataAdded += verboseHandler;
                powerShell.Streams.Debug.DataAdded += debugHandler;
                powerShell.Streams.Information.DataAdded += informationHandler;

                try
                {
                    // TODO/FIXME: ETW event for PowerShell invocation

                    var asyncResult = powerShell.BeginInvoke<PSObject, PSObject>(
                        input: null,
                        output: asyncOutput,
                        settings: invocationSettings,
                        callback: delegate
                                  {
                                      try
                                      {
                                          mergedOutput.CompleteAdding();
                                      }
                                      catch (InvalidOperationException)
                                      // ignore exceptions thrown because mergedOutput.CompleteAdding was called
                                      {
                                      }
                                  },
                        state: null);

                    using (cancellationToken.Register(powerShell.Stop))
                    {
                        try
                        {
                            foreach (Func<PSCmdlet, IEnumerable<PSObject>> mergedOutputItem in mergedOutput.GetConsumingEnumerable())
                            {
                                foreach (PSObject outputObject in mergedOutputItem(cmdlet))
                                {
                                    yield return outputObject;
                                }
                            }
                        }
                        finally
                        {
                            mergedOutput.CompleteAdding();
                            powerShell.EndInvoke(asyncResult);
                        }
                    }
                }
                finally
                {
                    asyncOutput.DataAdded -= outputHandler;
                    powerShell.Streams.Error.DataAdded -= errorHandler;
                    powerShell.Streams.Warning.DataAdded -= warningHandler;
                    powerShell.Streams.Verbose.DataAdded -= verboseHandler;
                    powerShell.Streams.Debug.DataAdded -= debugHandler;
                    powerShell.Streams.Information.DataAdded -= informationHandler;
                }
            }
        }

        private static IEnumerable<PSObject> InvokeNestedPowerShell(
            PowerShell powerShell,
            PSCmdlet cmdlet,
            PSInvocationSettings invocationSettings,
            string errorMessageTemplate,
            CancellationToken cancellationToken)
        {
            EventHandler<DataAddedEventArgs> errorHandler = GetStreamForwarder<ErrorRecord>(
                (ErrorRecord errorRecord) =>
                {
                    errorRecord = GetErrorRecordForRemotePipelineInvocation(errorRecord, errorMessageTemplate);
                    HandleErrorFromPipeline(cmdlet, errorRecord, powerShell);
                });
            powerShell.Streams.Error.DataAdded += errorHandler;

            try
            {
                using (cancellationToken.Register(powerShell.Stop))
                {
                    // TODO/FIXME: ETW event for PowerShell invocation

                    foreach (PSObject outputObject in powerShell.Invoke<PSObject>(null, invocationSettings))
                    {
                        yield return outputObject;
                    }
                }
            }
            finally
            {
                powerShell.Streams.Error.DataAdded -= errorHandler;
            }
        }

        private static void CopyParameterFromCmdletToPowerShell(Cmdlet cmdlet, PowerShell powerShell, string parameterName)
        {
            object parameterValue;
            if (!cmdlet.MyInvocation.BoundParameters.TryGetValue(parameterName, out parameterValue))
            {
                return;
            }

            var commandParameter = new CommandParameter(parameterName, parameterValue);
            foreach (var command in powerShell.Commands.Commands)
            {
                if (command.Parameters.Any(existingParameter => existingParameter.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                command.Parameters.Add(commandParameter);
            }
        }

        internal static ErrorRecord GetErrorRecordForProcessingOfCimModule(Exception innerException, string moduleName)
        {
            string errorMessage = string.Format(
                CultureInfo.InvariantCulture,
                Modules.RemoteDiscoveryFailedToProcessRemoteModule,
                moduleName,
                innerException.Message);

            Exception outerException = new InvalidOperationException(errorMessage, innerException);
            ErrorRecord errorRecord = new ErrorRecord(outerException, innerException.GetType().Name, ErrorCategory.NotSpecified, moduleName);
            return errorRecord;
        }

        private const string DiscoveryProviderNotFoundErrorId = "DiscoveryProviderNotFound";

        private static ErrorRecord GetErrorRecordForRemoteDiscoveryProvider(Exception innerException)
        {
            CimException cimException = innerException as CimException;
            if ((cimException != null) &&
                ((cimException.NativeErrorCode == NativeErrorCode.InvalidNamespace) ||
                 (cimException.NativeErrorCode == NativeErrorCode.InvalidClass) ||
                 (cimException.NativeErrorCode == NativeErrorCode.MethodNotFound) ||
                 (cimException.NativeErrorCode == NativeErrorCode.MethodNotAvailable)))
            {
                string errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    Modules.RemoteDiscoveryProviderNotFound,
                    innerException.Message);
                Exception outerException = new InvalidOperationException(errorMessage, innerException);
                ErrorRecord errorRecord = new ErrorRecord(outerException, DiscoveryProviderNotFoundErrorId, ErrorCategory.NotImplemented, null);
                return errorRecord;
            }
            else
            {
                string errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    Modules.RemoteDiscoveryFailureFromDiscoveryProvider,
                    innerException.Message);
                Exception outerException = new InvalidOperationException(errorMessage, innerException);
                ErrorRecord errorRecord = new ErrorRecord(outerException, "DiscoveryProviderFailure", ErrorCategory.NotSpecified, null);
                return errorRecord;
            }
        }

        private static ErrorRecord GetErrorRecordForRemotePipelineInvocation(Exception innerException, string errorMessageTemplate)
        {
            string errorMessage = string.Format(
                CultureInfo.InvariantCulture,
                errorMessageTemplate,
                innerException.Message);
            Exception outerException = new InvalidOperationException(errorMessage, innerException);

            RemoteException remoteException = innerException as RemoteException;
            ErrorRecord remoteErrorRecord = remoteException?.ErrorRecord;
            string errorId = remoteErrorRecord != null ? remoteErrorRecord.FullyQualifiedErrorId : innerException.GetType().Name;
            ErrorCategory errorCategory = remoteErrorRecord != null ? remoteErrorRecord.CategoryInfo.Category : ErrorCategory.NotSpecified;
            ErrorRecord errorRecord = new ErrorRecord(outerException, errorId, errorCategory, null);

            return errorRecord;
        }

        private static ErrorRecord GetErrorRecordForRemotePipelineInvocation(ErrorRecord innerErrorRecord, string errorMessageTemplate)
        {
            string innerErrorMessage;
            if (innerErrorRecord.ErrorDetails != null && innerErrorRecord.ErrorDetails.Message != null)
            {
                innerErrorMessage = innerErrorRecord.ErrorDetails.Message;
            }
            else if (innerErrorRecord.Exception != null && innerErrorRecord.Exception.Message != null)
            {
                innerErrorMessage = innerErrorRecord.Exception.Message;
            }
            else
            {
                innerErrorMessage = innerErrorRecord.ToString();
            }

            string errorMessage = string.Format(
                CultureInfo.InvariantCulture,
                errorMessageTemplate,
                innerErrorMessage);

            ErrorRecord outerErrorRecord = new ErrorRecord(innerErrorRecord, null /* null means: do not replace the exception */);
            ErrorDetails outerErrorDetails = new ErrorDetails(errorMessage);
            outerErrorRecord.ErrorDetails = outerErrorDetails;

            return outerErrorRecord;
        }

        private static IEnumerable<T> EnumerateWithCatch<T>(IEnumerable<T> enumerable, Action<Exception> exceptionHandler)
        {
            IEnumerator<T> enumerator = null;
            try
            {
                enumerator = enumerable.GetEnumerator();
            }
            catch (Exception e)
            {
                exceptionHandler(e);
            }

            if (enumerator != null)
                using (enumerator)
                {
                    bool gotResults = false;
                    do
                    {
                        try
                        {
                            gotResults = false;
                            gotResults = enumerator.MoveNext();
                        }
                        catch (Exception e)
                        {
                            exceptionHandler(e);
                        }

                        if (gotResults)
                        {
                            T currentItem = default(T);
                            bool gotCurrentItem = false;
                            try
                            {
                                currentItem = enumerator.Current;
                                gotCurrentItem = true;
                            }
                            catch (Exception e)
                            {
                                exceptionHandler(e);
                            }

                            if (gotCurrentItem)
                            {
                                yield return currentItem;
                            }
                            else
                            {
                                yield break;
                            }
                        }
                    } while (gotResults);
                }
        }

        private static void HandleErrorFromPipeline(Cmdlet cmdlet, ErrorRecord errorRecord, PowerShell powerShell)
        {
            if (!cmdlet.MyInvocation.ExpectingInput)
            {
                if (((powerShell.Runspace != null) && (powerShell.Runspace.RunspaceStateInfo.State != RunspaceState.Opened)) ||
                    ((powerShell.RunspacePool != null) && (powerShell.RunspacePool.RunspacePoolStateInfo.State != RunspacePoolState.Opened)))
                {
                    cmdlet.ThrowTerminatingError(errorRecord);
                }
            }

            cmdlet.WriteError(errorRecord);
        }

        internal static IEnumerable<PSObject> InvokePowerShell(
            PowerShell powerShell,
            PSCmdlet cmdlet,
            string errorMessageTemplate,
            CancellationToken cancellationToken)
        {
            CopyParameterFromCmdletToPowerShell(cmdlet, powerShell, "ErrorAction");
            CopyParameterFromCmdletToPowerShell(cmdlet, powerShell, "WarningAction");
            CopyParameterFromCmdletToPowerShell(cmdlet, powerShell, "InformationAction");
            CopyParameterFromCmdletToPowerShell(cmdlet, powerShell, "Verbose");
            CopyParameterFromCmdletToPowerShell(cmdlet, powerShell, "Debug");

            var invocationSettings = new PSInvocationSettings { Host = cmdlet.Host };

            // TODO/FIXME: ETW events for the output stream
            IEnumerable<PSObject> outputStream = powerShell.IsNested
                ? InvokeNestedPowerShell(powerShell, cmdlet, invocationSettings, errorMessageTemplate, cancellationToken)
                : InvokeTopLevelPowerShell(powerShell, cmdlet, invocationSettings, errorMessageTemplate, cancellationToken);

            return EnumerateWithCatch(
                outputStream,
                (Exception exception) =>
                {
                    ErrorRecord errorRecord = GetErrorRecordForRemotePipelineInvocation(exception, errorMessageTemplate);
                    HandleErrorFromPipeline(cmdlet, errorRecord, powerShell);
                });
        }

        #endregion PSRP

        #region CIM

        private const string DiscoveryProviderNamespace = "root/Microsoft/Windows/Powershellv3";
        private const string DiscoveryProviderModuleClass = "PS_Module";
        private const string DiscoveryProviderFileClass = "PS_ModuleFile";
        private const string DiscoveryProviderAssociationClass = "PS_ModuleToModuleFile";

        private static T GetPropertyValue<T>(CimInstance cimInstance, string propertyName, T defaultValue)
        {
            CimProperty cimProperty = cimInstance.CimInstanceProperties[propertyName];
            if (cimProperty == null)
            {
                return defaultValue;
            }

            object propertyValue = cimProperty.Value;
            if (propertyValue is T)
            {
                return (T)propertyValue;
            }

            if (propertyValue is string)
            {
                string stringValue = (string)propertyValue;
                try
                {
                    if (typeof(T) == typeof(bool))
                    {
                        return (T)(object)XmlConvert.ToBoolean(stringValue);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        return (T)(object)UInt16.Parse(stringValue, CultureInfo.InvariantCulture);
                    }
                    else if (typeof(T) == typeof(byte[]))
                    {
                        byte[] contentBytes = Convert.FromBase64String(stringValue);
                        byte[] lengthBytes = BitConverter.GetBytes(contentBytes.Length + 4);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(lengthBytes);
                        }

                        return (T)(object)(lengthBytes.Concat(contentBytes).ToArray());
                    }
                }
                catch (Exception)
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        internal enum CimFileCode
        {
            Unknown = 0,
            PsdV1,
            TypesV1,
            FormatV1,
            CmdletizationV1,
        }

        internal abstract class CimModuleFile
        {
            public CimFileCode FileCode
            {
                get
                {
                    if (this.FileName.EndsWith(".psd1", StringComparison.OrdinalIgnoreCase))
                    {
                        return CimFileCode.PsdV1;
                    }

                    if (this.FileName.EndsWith(".cdxml", StringComparison.OrdinalIgnoreCase))
                    {
                        return CimFileCode.CmdletizationV1;
                    }

                    if (this.FileName.EndsWith(".types.ps1xml", StringComparison.OrdinalIgnoreCase))
                    {
                        return CimFileCode.TypesV1;
                    }

                    if (this.FileName.EndsWith(".format.ps1xml", StringComparison.OrdinalIgnoreCase))
                    {
                        return CimFileCode.FormatV1;
                    }

                    return CimFileCode.Unknown;
                }
            }

            public abstract string FileName { get; }

            internal abstract byte[] RawFileDataCore { get; }

            public byte[] RawFileData
            {
                get { return this.RawFileDataCore.Skip(4).ToArray(); }
            }

            public string FileData
            {
                get
                {
                    if (_fileData == null)
                    {
                        using (var ms = new MemoryStream(this.RawFileData))
                        using (var sr = new StreamReader(ms, detectEncodingFromByteOrderMarks: true))
                        {
                            _fileData = sr.ReadToEnd();
                        }
                    }

                    return _fileData;
                }
            }

            private string _fileData;
        }

        internal class CimModule
        {
            private readonly CimInstance _baseObject;

            internal CimModule(CimInstance baseObject)
            {
                Dbg.Assert(baseObject != null, "Caller should make sure baseObject != null");
                Dbg.Assert(
                    baseObject.CimSystemProperties.ClassName.Equals(DiscoveryProviderModuleClass, StringComparison.OrdinalIgnoreCase),
                    "Caller should make sure baseObject is an instance of the right CIM class");

                _baseObject = baseObject;
            }

            public string ModuleName
            {
                get
                {
                    var rawModuleName = GetPropertyValue<string>(_baseObject, "ModuleName", string.Empty);
                    return Path.GetFileName(rawModuleName);
                }
            }

            private enum DiscoveredModuleType : ushort
            {
                Unknown = 0,
                Cim = 1,
            }

            public bool IsPsCimModule
            {
                get
                {
                    UInt16 moduleTypeInt = GetPropertyValue<UInt16>(_baseObject, "ModuleType", 0);
                    DiscoveredModuleType moduleType = (DiscoveredModuleType)moduleTypeInt;
                    bool isPsCimModule = (moduleType == DiscoveredModuleType.Cim);
                    return isPsCimModule;
                }
            }

            public CimModuleFile MainManifest
            {
                get
                {
                    byte[] rawFileData = GetPropertyValue<byte[]>(_baseObject, "moduleManifestFileData", Array.Empty<byte>());
                    return new CimModuleManifestFile(this.ModuleName + ".psd1", rawFileData);
                }
            }

            public IEnumerable<CimModuleFile> ModuleFiles
            {
                get { return _moduleFiles; }
            }

            internal void FetchAllModuleFiles(CimSession cimSession, string cimNamespace, CimOperationOptions operationOptions)
            {
                IEnumerable<CimInstance> associatedInstances = cimSession.EnumerateAssociatedInstances(
                    cimNamespace,
                    _baseObject,
                    DiscoveryProviderAssociationClass,
                    DiscoveryProviderFileClass,
                    "Antecedent",
                    "Dependent",
                    operationOptions);

                IEnumerable<CimModuleFile> associatedFiles = associatedInstances.Select(static i => new CimModuleImplementationFile(i));
                _moduleFiles = associatedFiles.ToList();
            }

            private List<CimModuleFile> _moduleFiles;

            private class CimModuleManifestFile : CimModuleFile
            {
                internal CimModuleManifestFile(string fileName, byte[] rawFileData)
                {
                    Dbg.Assert(fileName != null, "Caller should make sure fileName != null");
                    Dbg.Assert(rawFileData != null, "Caller should make sure rawFileData != null");

                    FileName = fileName;
                    RawFileDataCore = rawFileData;
                }

                public override string FileName { get; }

                internal override byte[] RawFileDataCore { get; }
            }

            private class CimModuleImplementationFile : CimModuleFile
            {
                private readonly CimInstance _baseObject;

                internal CimModuleImplementationFile(CimInstance baseObject)
                {
                    Dbg.Assert(baseObject != null, "Caller should make sure baseObject != null");
                    Dbg.Assert(
                        baseObject.CimSystemProperties.ClassName.Equals(DiscoveryProviderFileClass, StringComparison.OrdinalIgnoreCase),
                        "Caller should make sure baseObject is an instance of the right CIM class");

                    _baseObject = baseObject;
                }

                public override string FileName
                {
                    get
                    {
                        string rawFileName = GetPropertyValue<string>(_baseObject, "FileName", string.Empty);
                        return Path.GetFileName(rawFileName);
                    }
                }

                internal override byte[] RawFileDataCore
                {
                    get { return GetPropertyValue<byte[]>(_baseObject, "FileData", Array.Empty<byte>()); }
                }
            }
        }

        internal static IEnumerable<CimModule> GetCimModules(
            CimSession cimSession,
            Uri resourceUri,
            string cimNamespace,
            IEnumerable<string> moduleNamePatterns,
            bool onlyManifests,
            Cmdlet cmdlet,
            CancellationToken cancellationToken)
        {
            moduleNamePatterns ??= new[] { "*" };
            HashSet<string> alreadyEmittedNamesOfCimModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<CimModule> remoteModules = moduleNamePatterns
                .SelectMany(moduleNamePattern =>
                    RemoteDiscoveryHelper.GetCimModules(cimSession, resourceUri, cimNamespace, moduleNamePattern, onlyManifests, cmdlet, cancellationToken));
            foreach (CimModule remoteModule in remoteModules)
            {
                if (!alreadyEmittedNamesOfCimModules.Contains(remoteModule.ModuleName))
                {
                    alreadyEmittedNamesOfCimModules.Add(remoteModule.ModuleName);
                    yield return remoteModule;
                }
            }
        }

        private static IEnumerable<CimModule> GetCimModules(
            CimSession cimSession,
            Uri resourceUri,
            string cimNamespace,
            string moduleNamePattern,
            bool onlyManifests,
            Cmdlet cmdlet,
            CancellationToken cancellationToken)
        {
            Dbg.Assert(cimSession != null, "Caller should verify cimSession != null");
            Dbg.Assert(moduleNamePattern != null, "Caller should verify that moduleNamePattern != null");

            const WildcardOptions wildcardOptions = WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant;
            var wildcardPattern = WildcardPattern.Get(moduleNamePattern, wildcardOptions);
            string dosWildcard = WildcardPatternToDosWildcardParser.Parse(wildcardPattern);

            var options = new CimOperationOptions { CancellationToken = cancellationToken };
            options.SetCustomOption("PS_ModuleNamePattern", dosWildcard, mustComply: false);
            if (resourceUri != null)
            {
                options.ResourceUri = resourceUri;
            }

            if (string.IsNullOrEmpty(cimNamespace) && (resourceUri == null))
            {
                cimNamespace = DiscoveryProviderNamespace;
            }

            // TODO/FIXME: ETW for method invocation
            IEnumerable<CimInstance> syncResults = cimSession.EnumerateInstances(
                cimNamespace,
                DiscoveryProviderModuleClass,
                options);
            // TODO/FIXME: ETW for method results
            IEnumerable<CimModule> cimModules = syncResults
                .Select(static cimInstance => new CimModule(cimInstance))
                .Where(cimModule => wildcardPattern.IsMatch(cimModule.ModuleName));

            if (!onlyManifests)
            {
                cimModules = cimModules.Select(
                    (CimModule cimModule) =>
                    {
                        cimModule.FetchAllModuleFiles(cimSession, cimNamespace, options);
                        return cimModule;
                    });
            }

            return EnumerateWithCatch(
                cimModules,
                (Exception exception) =>
                {
                    ErrorRecord errorRecord = GetErrorRecordForRemoteDiscoveryProvider(exception);
                    if (!cmdlet.MyInvocation.ExpectingInput)
                    {
                        if (errorRecord.FullyQualifiedErrorId.Contains(DiscoveryProviderNotFoundErrorId, StringComparison.OrdinalIgnoreCase)
                            || cancellationToken.IsCancellationRequested
                            || exception is OperationCanceledException
                            || !cimSession.TestConnection())
                        {
                            cmdlet.ThrowTerminatingError(errorRecord);
                        }
                    }

                    cmdlet.WriteError(errorRecord);
                });
        }

        internal static Hashtable RewriteManifest(Hashtable originalManifest)
        {
            return RewriteManifest(originalManifest, null, null, null);
        }

        private static readonly string[] s_manifestEntriesToKeepAsString = new[] {
            "GUID",
            "Author",
            "CompanyName",
            "Copyright",
            "ModuleVersion",
            "Description",
            "HelpInfoURI",
        };

        private static readonly string[] s_manifestEntriesToKeepAsStringArray = new[] {
            "FunctionsToExport",
            "VariablesToExport",
            "AliasesToExport",
            "CmdletsToExport",
        };

        internal static Hashtable RewriteManifest(
            Hashtable originalManifest,
            IEnumerable<string> nestedModules,
            IEnumerable<string> typesToProcess,
            IEnumerable<string> formatsToProcess)
        {
            nestedModules ??= Array.Empty<string>();
            typesToProcess ??= Array.Empty<string>();
            formatsToProcess ??= Array.Empty<string>();

            var newManifest = new Hashtable(StringComparer.OrdinalIgnoreCase);
            newManifest["NestedModules"] = nestedModules;
            newManifest["TypesToProcess"] = typesToProcess;
            newManifest["FormatsToProcess"] = formatsToProcess;
            newManifest["PrivateData"] = originalManifest["PrivateData"];

            foreach (DictionaryEntry entry in originalManifest)
            {
                if (s_manifestEntriesToKeepAsString.Contains(entry.Key as string, StringComparer.OrdinalIgnoreCase))
                {
                    var value = (string)LanguagePrimitives.ConvertTo(entry.Value, typeof(string), CultureInfo.InvariantCulture);
                    newManifest[entry.Key] = value;
                }
                else if (s_manifestEntriesToKeepAsStringArray.Contains(entry.Key as string, StringComparer.OrdinalIgnoreCase))
                {
                    var values = (string[])LanguagePrimitives.ConvertTo(entry.Value, typeof(string[]), CultureInfo.InvariantCulture);
                    newManifest[entry.Key] = values;
                }
            }

            return newManifest;
        }

        private static CimCredential GetCimCredentials(PasswordAuthenticationMechanism authenticationMechanism, PSCredential credential)
        {
            NetworkCredential networkCredential = credential.GetNetworkCredential();
            return new CimCredential(authenticationMechanism, networkCredential.Domain, networkCredential.UserName, credential.Password);
        }

        private static Exception GetExceptionWhenAuthenticationRequiresCredential(string authentication)
        {
            string errorMessage = string.Format(
                CultureInfo.InvariantCulture,
                RemotingErrorIdStrings.AuthenticationMechanismRequiresCredential,
                authentication);
            throw new ArgumentException(errorMessage);
        }

        private static CimCredential GetCimCredentials(string authentication, PSCredential credential)
        {
            if (authentication == null || (authentication.Equals("Default", StringComparison.OrdinalIgnoreCase)))
            {
                if (credential == null)
                {
                    return null;
                }
                else
                {
                    return GetCimCredentials(PasswordAuthenticationMechanism.Default, credential);
                }
            }

            if (authentication.Equals("Basic", StringComparison.OrdinalIgnoreCase))
            {
                if (credential == null)
                {
                    throw GetExceptionWhenAuthenticationRequiresCredential(authentication);
                }
                else
                {
                    return GetCimCredentials(PasswordAuthenticationMechanism.Basic, credential);
                }
            }

            if (authentication.Equals("Negotiate", StringComparison.OrdinalIgnoreCase))
            {
                if (credential == null)
                {
                    return new CimCredential(ImpersonatedAuthenticationMechanism.Negotiate);
                }
                else
                {
                    return GetCimCredentials(PasswordAuthenticationMechanism.Negotiate, credential);
                }
            }

            if (authentication.Equals("CredSSP", StringComparison.OrdinalIgnoreCase))
            {
                if (credential == null)
                {
                    throw GetExceptionWhenAuthenticationRequiresCredential(authentication);
                }
                else
                {
                    return GetCimCredentials(PasswordAuthenticationMechanism.CredSsp, credential);
                }
            }

            if (authentication.Equals("Digest", StringComparison.OrdinalIgnoreCase))
            {
                if (credential == null)
                {
                    throw GetExceptionWhenAuthenticationRequiresCredential(authentication);
                }
                else
                {
                    return GetCimCredentials(PasswordAuthenticationMechanism.Digest, credential);
                }
            }

            if (authentication.Equals("Kerberos", StringComparison.OrdinalIgnoreCase))
            {
                if (credential == null)
                {
                    return new CimCredential(ImpersonatedAuthenticationMechanism.Kerberos);
                }
                else
                {
                    return GetCimCredentials(PasswordAuthenticationMechanism.Kerberos, credential);
                }
            }

            Dbg.Assert(false, "Unrecognized authentication mechanism [ValidateSet should prevent that from happening]");
            throw new ArgumentOutOfRangeException(nameof(authentication));
        }

        internal static CimSession CreateCimSession(
            string computerName,
            PSCredential credential,
            string authentication,
            bool isLocalHost,
            PSCmdlet cmdlet,
            CancellationToken cancellationToken)
        {
            if (isLocalHost)
            {
                return CimSession.Create(null);
            }

            var sessionOptions = new CimSessionOptions();

            CimCredential cimCredentials = GetCimCredentials(authentication, credential);
            if (cimCredentials != null)
            {
                sessionOptions.AddDestinationCredentials(cimCredentials);
            }

            CimSession cimSession = CimSession.Create(computerName, sessionOptions);
            return cimSession;
        }

        internal static Hashtable ConvertCimModuleFileToManifestHashtable(RemoteDiscoveryHelper.CimModuleFile cimModuleFile, string temporaryModuleManifestPath, ModuleCmdletBase cmdlet, ref bool containedErrors)
        {
            Dbg.Assert(cimModuleFile.FileCode == RemoteDiscoveryHelper.CimFileCode.PsdV1, "Caller should verify the file is of the right type");

            ScriptBlockAst scriptBlockAst = null;
            if (!containedErrors)
            {
                System.Management.Automation.Language.Token[] throwAwayTokens;
                ParseError[] parseErrors;
                scriptBlockAst = System.Management.Automation.Language.Parser.ParseInput(cimModuleFile.FileData, temporaryModuleManifestPath, out throwAwayTokens, out parseErrors);
                if ((scriptBlockAst == null) || (parseErrors != null && parseErrors.Length > 0))
                {
                    containedErrors = true;
                }
            }

            Hashtable data = null;
            if (!containedErrors)
            {
                ScriptBlock scriptBlock = new ScriptBlock(scriptBlockAst, isFilter: false);
                data = cmdlet.LoadModuleManifestData(
                    temporaryModuleManifestPath,
                    scriptBlock,
                    ModuleCmdletBase.ModuleManifestMembers,
                    0 /* - don't write errors, don't load elements, don't return null on first error */,
                    ref containedErrors);
            }

            return data;
        }

        #endregion CIM

        #region Protocol/transport agnostic functionality

        internal static string GetModulePath(string remoteModuleName, Version remoteModuleVersion, string computerName, Runspace localRunspace)
        {
            computerName ??= string.Empty;

            string sanitizedRemoteModuleName = Regex.Replace(remoteModuleName, "[^a-zA-Z0-9]", string.Empty);
            string sanitizedComputerName = Regex.Replace(computerName, "[^a-zA-Z0-9]", string.Empty);
            string moduleName = string.Format(
                CultureInfo.InvariantCulture,
                "remoteIpMoProxy_{0}_{1}_{2}_{3}",
                sanitizedRemoteModuleName.Substring(0, Math.Min(sanitizedRemoteModuleName.Length, 100)),
                remoteModuleVersion,
                sanitizedComputerName.Substring(0, Math.Min(sanitizedComputerName.Length, 100)),
                localRunspace.InstanceId);
            string modulePath = Path.Combine(Path.GetTempPath(), moduleName);
            return modulePath;
        }

        internal static void AssociatePSModuleInfoWithSession(PSModuleInfo moduleInfo, CimSession cimSession, Uri resourceUri, string cimNamespace)
        {
            AssociatePSModuleInfoWithSession(moduleInfo, (object)new Tuple<CimSession, Uri, string>(cimSession, resourceUri, cimNamespace));
        }

        internal static void AssociatePSModuleInfoWithSession(PSModuleInfo moduleInfo, PSSession psSession)
        {
            AssociatePSModuleInfoWithSession(moduleInfo, (object)psSession);
        }

        private static void AssociatePSModuleInfoWithSession(PSModuleInfo moduleInfo, object weaklyTypedSession)
        {
            s_moduleInfoToSession.Add(moduleInfo, weaklyTypedSession);
        }

        private static readonly ConditionalWeakTable<PSModuleInfo, object> s_moduleInfoToSession = new ConditionalWeakTable<PSModuleInfo, object>();

        internal static void DispatchModuleInfoProcessing(
            PSModuleInfo moduleInfo,
            Action localAction,
            Action<CimSession, Uri, string> cimSessionAction,
            Action<PSSession> psSessionAction)
        {
            object weaklyTypeSession;
            if (!s_moduleInfoToSession.TryGetValue(moduleInfo, out weaklyTypeSession))
            {
                localAction();
                return;
            }

            Tuple<CimSession, Uri, string> cimSessionInfo = weaklyTypeSession as Tuple<CimSession, Uri, string>;
            if (cimSessionInfo != null)
            {
                cimSessionAction(cimSessionInfo.Item1, cimSessionInfo.Item2, cimSessionInfo.Item3);
                return;
            }

            PSSession psSession = weaklyTypeSession as PSSession;
            if (psSession != null)
            {
                psSessionAction(psSession);
                return;
            }

            Dbg.Assert(false, "PSModuleInfo was associated with an unrecognized session type");
        }

        #endregion
    }
}
