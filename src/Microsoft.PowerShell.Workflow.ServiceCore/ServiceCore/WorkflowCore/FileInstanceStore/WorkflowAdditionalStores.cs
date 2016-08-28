/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Diagnostics;
using System.IO.Compression;
using System.Management.Automation.Remoting;

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;
    using System.Xml.Linq;
    using System.Runtime.DurableInstancing;
    using System.Globalization;
    using System.Management.Automation.Tracing;
    using System.Activities.DurableInstancing;
    using System.Activities.Persistence;
    using System.Reflection;
    using Microsoft.PowerShell.Commands;
    using System.Text;

    internal class PersistenceVersion
    {
        private readonly PowerShellTraceSource Tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private bool saved = false;
        private object syncLock = new object();

        internal Version StoreVersion { get; set; }
        internal Version CLRVersion { get; set; }
        internal bool EnableEncryption { get; set; }
        internal bool EnableCompression { get; set; }

        internal PersistenceVersion(bool enableEncryption, bool enableCompression)
        {
            StoreVersion = new Version(1, 0);
            CLRVersion = Environment.Version;
            EnableEncryption = enableEncryption;
            EnableCompression = enableCompression;
        }

        internal void save(string versionFileName)
        {
            if (saved) return;

            lock (syncLock)
            {

                if (saved) return;
                saved = true;

                if (File.Exists(versionFileName))
                    return;

                XElement versionXml = new XElement("PersistenceVersion",
                                                    new XElement("StoreVersion", StoreVersion),
                                                    new XElement("CLRVersion", CLRVersion),
                                                    new XElement("EnableEncryption", EnableEncryption),
                                                    new XElement("EnableCompression", EnableCompression));

                versionXml.Save(versionFileName);
            }
        }

        internal void load(string versionFileName)
        {
            try
            {
                if (!File.Exists(versionFileName))
                    return; // use the value provided in the constructor

                XElement versionXml = XElement.Load(versionFileName);

                if (versionXml.Name.LocalName.Equals("PersistenceVersion", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (XElement x in versionXml.Elements())
                    {
                        if (x.Name.LocalName.Equals("StoreVersion", StringComparison.OrdinalIgnoreCase))
                        {
                            this.StoreVersion = new Version(x.Value);
                        }
                        else if (x.Name.LocalName.Equals("CLRVersion", StringComparison.OrdinalIgnoreCase))
                        {
                            this.CLRVersion = new Version(x.Value);
                        }
                        else if (x.Name.LocalName.Equals("EnableEncryption", StringComparison.OrdinalIgnoreCase))
                        {
                            this.EnableEncryption = Convert.ToBoolean(x.Value, CultureInfo.InvariantCulture);
                        }
                        else if (x.Name.LocalName.Equals("EnableCompression", StringComparison.OrdinalIgnoreCase))
                        {
                            this.EnableCompression = Convert.ToBoolean(x.Value, CultureInfo.InvariantCulture);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //default values will be used
                Tracer.WriteMessage(string.Format(CultureInfo.InvariantCulture, "Exception while reading or parsing the version file: {0}", versionFileName));
                Tracer.TraceException(e);
            }
        }
    }



    internal enum InternalStoreComponents
    {
        Streams = 0,
        Metadata = 1,
        Definition = 2,
        Timer = 3,
        JobState = 4,
        TerminatingError = 5,
        Context = 6,
        ActivityState = 7,
    }

    /// <summary>
    /// This class implements the functionality for storing the streams data on to the disk.
    /// </summary>
    public class PSWorkflowFileInstanceStore : PSWorkflowInstanceStore
    {
        # region private variables

        private readonly PowerShellTraceSource Tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private static readonly Tracer etwTracer = new Tracer();

        private readonly string Streams = "Str";
        private readonly string Error = "Err";
        private readonly string Metadatas = "Meta";
        private readonly string Definition = "Def";
        private readonly string WorkflowState = "Stat";

        private readonly string Version_xml = "V.xml";

        private readonly string InputStream_xml = "IS.xml";
        private readonly string OutputStream_xml = "OS.xml";
        private readonly string ErrorStream_xml = "ES.xml";
        private readonly string WarningStream_xml = "WS.xml";
        private readonly string VerboseStream_xml = "VS.xml";
        private readonly string ProgressStream_xml = "PS.xml";
        private readonly string DebugStream_xml = "DS.xml";
        private readonly string InformationStream_xml = "INFS.xml";

        private readonly string ErrorException_xml = "EE.xml";

        private readonly string Input_xml = "I.xml";
        private readonly string PSWorkflowCommonParameters_xml = "UI.xml";
        private readonly string JobMetadata_xml = "JM.xml";
        private readonly string PrivateMetadata_xml = "PM.xml";
        private readonly string Timer_xml = "TI.xml";

        private readonly string WorkflowInstanceState_xml = "WS.xml";

        private readonly string WorkflowDefinition_xaml = "WD.xaml";
        private readonly string RuntimeAssembly_dll = "RA.dll";
        private readonly string RequiredAssemblies_xml = "RA.xml";

        private readonly string State_xml = "S.xml";

        private readonly string ActivityState_xml = "AS.xml";

        private readonly object _syncLock = new object();

        private bool firstTimeStoringDefinition;

        private PSWorkflowConfigurationProvider _configuration;

        private Dictionary<InternalStoreComponents, long> SavedComponentLengths;

        private long writtenTotalBytes = 0;

        # endregion

        // For testing purpose ONLY
        internal static bool TestMode = false;
        // For testing purpose ONLY
        internal static long ObjectCounter = 0;

        /// <summary>
        /// Returns all Workflow guids.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<PSWorkflowId> GetAllWorkflowInstanceIds()
        {
            PSWorkflowConfigurationProvider _configuration = (PSWorkflowConfigurationProvider) PSWorkflowRuntime.Instance.Configuration;
            DirectoryInfo dir = new DirectoryInfo(_configuration.InstanceStorePath);
            if (dir.Exists)
            {
                foreach (DirectoryInfo childDir in dir.GetDirectories())
                {
                    Guid id;
                    if (Guid.TryParse(childDir.Name, out id))
                        yield return new PSWorkflowId(id);
                }
            }
        }


        /// <summary>
        /// PSWorkflowFileInstanceStore ctor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="instance"></param>
        public PSWorkflowFileInstanceStore(PSWorkflowConfigurationProvider configuration, PSWorkflowInstance instance)
            : base(instance)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            if (TestMode)
            {
                System.Threading.Interlocked.Increment(ref ObjectCounter);
            }

            _configuration = configuration;
            firstTimeStoringDefinition = true;
            SavedComponentLengths = new Dictionary<InternalStoreComponents, long>();

            bool enableCompression = true;
            _disablePersistenceLimits = true;
            if (PSSessionConfigurationData.IsServerManager)
            {
                enableCompression = false;
                _disablePersistenceLimits = false;
            }

            _version = new PersistenceVersion(_configuration.PersistWithEncryption, enableCompression);
            _version.load(Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Version_xml));
        }

        private void SaveVersionFile()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString());
            EnsureInstancePath(storePath);

            _version.save(Path.Combine(storePath, Version_xml));
        }

        private PersistenceVersion _version;

        /// <summary>
        /// CreateInstanceStore
        /// </summary>
        /// <returns></returns>
        public override InstanceStore CreateInstanceStore()
        {
            return new FileInstanceStore(this);
        }

        /// <summary>
        /// CreatePersistenceIOParticipant
        /// </summary>
        /// <returns></returns>
        public override PersistenceIOParticipant CreatePersistenceIOParticipant()
        {
            return null;
        }

        # region Serialization

        /// <summary>
        ///  To be called from test code only.
        /// </summary>
        /// <param name="components"></param>
        internal void CallDoSave(IEnumerable<object> components)
        {
            DoSave(components);
        }

        /// <summary>
        /// DoSave
        /// </summary>
        /// <param name="components"></param>
        protected override void DoSave(IEnumerable<object> components)
        {
            // no persistence is allowed if the serialization error has occured.
            if (serializationErrorHasOccured)
                return;

            this.SaveVersionFile();

            if (_disablePersistenceLimits)
            {
                DoSave2(components);
                return;
            }

            lock (_syncLock)
            {
                long contextLengthNew = 0;
                long streamLengthNew = 0;
                long metadataLengthNew = 0;
                long definitionLengthNew = 0;
                long timerLengthNew = 0;
                long jobStateLengthNew = 0;
                long errorExceptionLengthNew = 0;
                long activityStateLengthNew = 0;

                long contextLengthOld = 0;
                long streamLengthOld = 0;
                long metadataLengthOld = 0;
                long definitionLengthOld = 0;
                long timerLengthOld = 0;
                long jobStateLengthOld = 0;
                long errorExceptionLengthOld = 0;
                long activityStateLengthOld = 0;

                foreach (object component in components)
                {
                    Type componentType = component.GetType();

                    if(componentType == typeof(Dictionary<string, object>))
                    {
                        contextLengthNew = this.LoadSerializedContext(component);
                        contextLengthOld = this.SavedComponentLengths.ContainsKey(InternalStoreComponents.Context) ?
                                                this.SavedComponentLengths[InternalStoreComponents.Context] :
                                                this.GetSavedContextLength();

                        this.SaveSerializedContext();
                        if (this.SavedComponentLengths.ContainsKey(InternalStoreComponents.Context))
                            SavedComponentLengths.Remove(InternalStoreComponents.Context);
                        this.SavedComponentLengths.Add(InternalStoreComponents.Context, contextLengthNew);
                    }
                    else if(componentType == typeof(PowerShellStreams<PSObject, PSObject>))
                    {
                        streamLengthNew = this.LoadSerializedStreamData(component);
                        streamLengthOld = this.SavedComponentLengths.ContainsKey(InternalStoreComponents.Streams) ?
                                                this.SavedComponentLengths[InternalStoreComponents.Streams] :
                                                this.GetSavedStreamDataLength();

                        this.SaveSerializedStreamData();
                        if (this.SavedComponentLengths.ContainsKey(InternalStoreComponents.Streams))
                            SavedComponentLengths.Remove(InternalStoreComponents.Streams);
                        this.SavedComponentLengths.Add(InternalStoreComponents.Streams, streamLengthNew);
                    }
                    else if(componentType == typeof(PSWorkflowContext))
                    {
                        metadataLengthNew = this.LoadSerializedMetadata(component);
                        metadataLengthOld = this.SavedComponentLengths.ContainsKey(InternalStoreComponents.Metadata) ?
                                                this.SavedComponentLengths[InternalStoreComponents.Metadata] :
                                                this.GetSavedMetadataLength();

                        this.SaveSerializedMetadata();
                        if (this.SavedComponentLengths.ContainsKey(InternalStoreComponents.Metadata))
                            SavedComponentLengths.Remove(InternalStoreComponents.Metadata);
                        this.SavedComponentLengths.Add(InternalStoreComponents.Metadata, metadataLengthNew);
                    }
                    else if(componentType == typeof(PSWorkflowDefinition))
                    {
                        if (firstTimeStoringDefinition)
                        {
                            definitionLengthNew = this.LoadSerializedDefinition(component);
                            definitionLengthOld = this.SavedComponentLengths.ContainsKey(InternalStoreComponents.Definition) ?
                                                    this.SavedComponentLengths[InternalStoreComponents.Definition] :
                                                    this.GetSavedDefinitionLength();

                            this.SaveSerializedDefinition(component);
                            if (this.SavedComponentLengths.ContainsKey(InternalStoreComponents.Definition))
                                SavedComponentLengths.Remove(InternalStoreComponents.Definition);
                            this.SavedComponentLengths.Add(InternalStoreComponents.Definition, definitionLengthNew);
                        }
                    }
                    else if(componentType == typeof(PSWorkflowTimer))
                    {

                        timerLengthNew = this.LoadSerializedTimer(component as PSWorkflowTimer);
                        timerLengthOld = this.SavedComponentLengths.ContainsKey(InternalStoreComponents.Timer) ?
                                                this.SavedComponentLengths[InternalStoreComponents.Timer] :
                                                this.GetSavedTimerLength();

                        this.SaveSerializedTimer();
                        if (this.SavedComponentLengths.ContainsKey(InternalStoreComponents.Timer))
                            SavedComponentLengths.Remove(InternalStoreComponents.Timer);
                        this.SavedComponentLengths.Add(InternalStoreComponents.Timer, timerLengthNew);
                    }
                    else if(componentType == typeof(JobState))
                    {
                        jobStateLengthNew = this.LoadSerializedJobState(component);
                        jobStateLengthOld = this.SavedComponentLengths.ContainsKey(InternalStoreComponents.JobState) ?
                                                this.SavedComponentLengths[InternalStoreComponents.JobState] :
                                                this.GetSavedJobStateLength();

                        this.SaveSerializedJobState();
                        if (this.SavedComponentLengths.ContainsKey(InternalStoreComponents.JobState))
                            SavedComponentLengths.Remove(InternalStoreComponents.JobState);
                        this.SavedComponentLengths.Add(InternalStoreComponents.JobState, jobStateLengthNew);
                    }
                    else if (component is Exception)
                    {
                        errorExceptionLengthNew = this.LoadSerializedErrorException(component);
                        errorExceptionLengthOld = this.SavedComponentLengths.ContainsKey(InternalStoreComponents.TerminatingError) ?
                                                this.SavedComponentLengths[InternalStoreComponents.TerminatingError] :
                                                this.GetSavedErrorExceptionLength();

                        this.SaveSerializedErrorException();
                        if (this.SavedComponentLengths.ContainsKey(InternalStoreComponents.TerminatingError))
                            SavedComponentLengths.Remove(InternalStoreComponents.TerminatingError);
                        this.SavedComponentLengths.Add(InternalStoreComponents.TerminatingError, errorExceptionLengthNew);
                    }
                    else if (componentType == typeof(PSWorkflowRemoteActivityState))
                    {
                        activityStateLengthNew = this.LoadSerializedActivityState(component as PSWorkflowRemoteActivityState);
                        activityStateLengthOld = this.SavedComponentLengths.ContainsKey(InternalStoreComponents.ActivityState) ?
                                                this.SavedComponentLengths[InternalStoreComponents.ActivityState] :
                                                this.GetSavedActivityStateLength();

                        this.SaveSerializedActivityState();
                        if (this.SavedComponentLengths.ContainsKey(InternalStoreComponents.ActivityState))
                            SavedComponentLengths.Remove(InternalStoreComponents.ActivityState);
                        this.SavedComponentLengths.Add(InternalStoreComponents.ActivityState, activityStateLengthNew);
                    }
                }


                // Now verify if the data can be in allowed max persistence limit 
                long oldValue = (
                                    contextLengthOld +
                                    streamLengthOld +
                                    metadataLengthOld +
                                    definitionLengthOld +
                                    timerLengthOld +
                                    jobStateLengthOld +
                                    activityStateLengthOld +
                                    errorExceptionLengthOld
                                );

                long newValue = (
                                    contextLengthNew +
                                    streamLengthNew +
                                    metadataLengthNew +
                                    definitionLengthNew +
                                    timerLengthNew +
                                    jobStateLengthNew +
                                    activityStateLengthNew +
                                    errorExceptionLengthNew
                                );

                bool allowed = CheckMaxPersistenceSize(oldValue, newValue);

                // if not allowed then write the warning message and continue execution.
                if (allowed == false)
                {
                    this.WriteWarning(Resources.PersistenceSizeReached);
                    etwTracer.PersistenceStoreMaxSizeReached();
                }
            }
        }

        /// <summary>
        /// Delete
        /// </summary>
        protected override void DoDelete()
        {
            lock (_syncLock)
            {
                InternalDelete();
            }
        }

        private void InternalDelete()
        {
            string instanceFolder = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString());
            if (Directory.Exists(instanceFolder))
            {
                try
                {
                    long size = this.GetDirectoryLength(new DirectoryInfo(instanceFolder));
                    Directory.Delete(instanceFolder, true);
                    ReducePersistenceSize(size);
                }
                catch (IOException e)
                {
                    RetryDelete(e, instanceFolder);
                }
                catch (UnauthorizedAccessException e)
                {
                    RetryDelete(e, instanceFolder);
                }
            }
        }

        // RetryDelete throws exception if delete fails again, that exception causes the remove job to fail
        //
        private void RetryDelete(Exception e, string instanceFolder)
        {
            Tracer.TraceException(e);
            Tracer.WriteMessage("Trying to delete one more time.");

            try
            {
                Directory.Delete(instanceFolder, true);
            }
            catch (Exception exc)
            {
                Tracer.TraceException(exc);

                // eating the exceptions as remove job should not throw any errors for deleting the store files.
            }
        }

        # region Stream 

        private ArraySegment<byte> serializedInputStreamData;
        private ArraySegment<byte> serializedOutputStreamData;
        private ArraySegment<byte> serializedErrorStreamData;
        private ArraySegment<byte> serializedWarningStreamData;
        private ArraySegment<byte> serializedVerboseStreamData;
        private ArraySegment<byte> serializedProgressStreamData;
        private ArraySegment<byte> serializedDebugStreamData;
        private ArraySegment<byte> serializedInformationStreamData;

        private long LoadSerializedStreamData(object data)
        {
            Debug.Assert(data.GetType() == typeof (PowerShellStreams<PSObject, PSObject>), "The data should be of type workflow stream");
            PowerShellStreams<PSObject, PSObject> streams = (PowerShellStreams<PSObject, PSObject>) data;

            this.serializedInputStreamData = Encrypt(SerializeObject(streams.InputStream));
            this.serializedOutputStreamData = Encrypt(SerializeObject(streams.OutputStream));
            this.serializedErrorStreamData = Encrypt(SerializeObject(streams.ErrorStream));
            this.serializedWarningStreamData = Encrypt(SerializeObject(streams.WarningStream));
            this.serializedVerboseStreamData = Encrypt(SerializeObject(streams.VerboseStream));
            this.serializedProgressStreamData = Encrypt(SerializeObject(streams.ProgressStream));
            this.serializedDebugStreamData = Encrypt(SerializeObject(streams.DebugStream));
            this.serializedInformationStreamData = Encrypt(SerializeObject(streams.InformationStream));

            long size = (
                            this.serializedInputStreamData.Count +
                            this.serializedOutputStreamData.Count +
                            this.serializedErrorStreamData.Count +
                            this.serializedWarningStreamData.Count +
                            this.serializedVerboseStreamData.Count +
                            this.serializedProgressStreamData.Count +
                            this.serializedDebugStreamData.Count +
                            this.serializedInformationStreamData.Count
                        );

            return size;
        }

        private void EnsureInstancePath(string storePath)
        {
            if (Directory.Exists(storePath) == false)
            {
                try
                {
                    Directory.CreateDirectory(storePath);
                }
                catch (Exception e)
                {
                    Tracer.TraceException(e);
                    throw;
                }
                InstanceStorePermission.SetDirectoryPermissions(storePath);
            }
        }

        private void SaveSerializedStreamData()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Streams);
            EnsureInstancePath(storePath);

            SaveToFile(this.serializedInputStreamData, Path.Combine(storePath, InputStream_xml));
            SaveToFile(this.serializedOutputStreamData, Path.Combine(storePath, OutputStream_xml));
            SaveToFile(this.serializedErrorStreamData, Path.Combine(storePath, ErrorStream_xml));
            SaveToFile(this.serializedWarningStreamData, Path.Combine(storePath, WarningStream_xml));
            SaveToFile(this.serializedVerboseStreamData, Path.Combine(storePath, VerboseStream_xml));
            SaveToFile(this.serializedProgressStreamData, Path.Combine(storePath, ProgressStream_xml));
            SaveToFile(this.serializedDebugStreamData, Path.Combine(storePath, DebugStream_xml));
            SaveToFile(this.serializedInformationStreamData, Path.Combine(storePath, InformationStream_xml));
        }

        private long GetSavedStreamDataLength()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Streams);

            long size = 0;

            if (Directory.Exists(storePath))
            {
                FileInfo info = new FileInfo(Path.Combine(storePath, InputStream_xml));
                size += (info.Exists) ? info.Length : 0;

                info = new FileInfo(Path.Combine(storePath, OutputStream_xml));
                size += (info.Exists) ? info.Length : 0;

                info = new FileInfo(Path.Combine(storePath, ErrorStream_xml));
                size += (info.Exists) ? info.Length : 0;

                info = new FileInfo(Path.Combine(storePath, WarningStream_xml));
                size += (info.Exists) ? info.Length : 0;

                info = new FileInfo(Path.Combine(storePath, VerboseStream_xml));
                size += (info.Exists) ? info.Length : 0;

                info = new FileInfo(Path.Combine(storePath, ProgressStream_xml));
                size += (info.Exists) ? info.Length : 0;

                info = new FileInfo(Path.Combine(storePath, DebugStream_xml));
                size += (info.Exists) ? info.Length : 0;

                info = new FileInfo(Path.Combine(storePath, InformationStream_xml));
                size += (info.Exists) ? info.Length : 0;
            }

            return size;
        }

        # endregion

        # region PSWorkflowContext

        private ArraySegment<byte> serializedWorkflowParameters;
        private ArraySegment<byte> serializedPSWorkflowCommonParameters;
        private ArraySegment<byte> serializedJobMetadata;
        private ArraySegment<byte> serializedPrivateMetadata;

        private long LoadSerializedMetadata(object data)
        {
            Debug.Assert(data.GetType() == typeof (PSWorkflowContext), "The data should be of type workflow metadata");
            PSWorkflowContext metadata = (PSWorkflowContext) data;

            this.serializedWorkflowParameters = Encrypt(SerializeObject(metadata.WorkflowParameters));
            this.serializedPSWorkflowCommonParameters = Encrypt(SerializeObject(metadata.PSWorkflowCommonParameters));
            this.serializedJobMetadata = Encrypt(SerializeObject(metadata.JobMetadata));
            this.serializedPrivateMetadata = Encrypt(SerializeObject(metadata.PrivateMetadata));

            long size = (
                            this.serializedWorkflowParameters.Count +
                            this.serializedPSWorkflowCommonParameters.Count +
                            this.serializedJobMetadata.Count +
                            this.serializedPrivateMetadata.Count
                        );

            return size;

        }

        private void SaveSerializedMetadata()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Metadatas);
            EnsureInstancePath(storePath);

            SaveToFile(this.serializedWorkflowParameters, Path.Combine(storePath, Input_xml));
            SaveToFile(this.serializedPSWorkflowCommonParameters,Path.Combine(storePath, PSWorkflowCommonParameters_xml));
            SaveToFile(this.serializedJobMetadata, Path.Combine(storePath, JobMetadata_xml));
            SaveToFile(this.serializedPrivateMetadata, Path.Combine(storePath, PrivateMetadata_xml));
        }

        private long GetSavedMetadataLength()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(),Metadatas);

            long size = 0;

            if (Directory.Exists(storePath))
            {
                FileInfo info = new FileInfo(Path.Combine(storePath, Input_xml));
                size += (info.Exists) ? info.Length : 0;
                info = new FileInfo(Path.Combine(storePath, PSWorkflowCommonParameters_xml));
                size += (info.Exists) ? info.Length : 0;
                info = new FileInfo(Path.Combine(storePath, JobMetadata_xml));
                size += (info.Exists) ? info.Length : 0;
                info = new FileInfo(Path.Combine(storePath, PrivateMetadata_xml));
                size += (info.Exists) ? info.Length : 0;
            }

            return size;
        }

        # endregion

        # region Error Exception

        private ArraySegment<byte> serializedErrorException;

        private long LoadSerializedErrorException(object data)
        {
            this.serializedErrorException = Encrypt(SerializeObject(data));

            long size = this.serializedErrorException.Count;

            return size;
        }

        private void SaveSerializedErrorException()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Error);
            EnsureInstancePath(storePath);

            SaveToFile(this.serializedErrorException, Path.Combine(storePath, ErrorException_xml));

        }

        private long GetSavedErrorExceptionLength()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Error);

            long size = 0;

            if (Directory.Exists(storePath))
            {
                FileInfo info = new FileInfo(Path.Combine(storePath, ErrorException_xml));
                size += (info.Exists) ? info.Length : 0;
            }

            return size;
        }

        # endregion

        #region Timer

        private ArraySegment<byte> serializedTimerData;

        private long LoadSerializedTimer(PSWorkflowTimer data)
        {
            this.serializedTimerData = Encrypt(SerializeObject(data.GetSerializedData()));

            long size = this.serializedTimerData.Count;

            return size;
        }

        private void SaveSerializedTimer()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Metadatas);
            EnsureInstancePath(storePath);

            SaveToFile(this.serializedTimerData, Path.Combine(storePath, Timer_xml));
        }

        private long GetSavedTimerLength()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Metadatas);

            long size = 0;

            if (Directory.Exists(storePath))
            {
                FileInfo info = new FileInfo(Path.Combine(storePath, Timer_xml));
                size += (info.Exists) ? info.Length : 0;
            }

            return size;
        }

        #endregion

        # region PSWorkflowDefinition
        private ArraySegment<byte> serializedRequiredAssemblies;
        private long LoadSerializedDefinition(object data)
        {
            Debug.Assert(data.GetType() == typeof (PSWorkflowDefinition), "The data should be of type workflow definition");
            PSWorkflowDefinition definition = (PSWorkflowDefinition) data;

            string WorkflowXaml = definition.WorkflowXaml;
            string runtimeAssemblyPath = definition.RuntimeAssemblyPath;

            long size = WorkflowXaml == null ? 0 : WorkflowXaml.Length;

            if (!string.IsNullOrEmpty(runtimeAssemblyPath) && File.Exists(runtimeAssemblyPath))
            {
                FileInfo file = new FileInfo(runtimeAssemblyPath);
                size += file.Length;
            }

            if(definition.RequiredAssemblies != null)
            {
                this.serializedRequiredAssemblies = Encrypt(SerializeObject(definition.RequiredAssemblies));
                size += this.serializedRequiredAssemblies.Count;
            }

            return size;
        }

        private void SaveSerializedDefinition(object data)
        {
            Debug.Assert(data.GetType() == typeof (PSWorkflowDefinition), "The data should be of type workflow definition");
            PSWorkflowDefinition definition = (PSWorkflowDefinition) data;

            if (firstTimeStoringDefinition)
            {
                firstTimeStoringDefinition = false;

                string WorkflowXaml = definition.WorkflowXaml;
                string runtimeAssemblyPath = definition.RuntimeAssemblyPath;

                string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Definition);
                EnsureInstancePath(storePath);

                try
                {
                    File.WriteAllText(Path.Combine(storePath, WorkflowDefinition_xaml), WorkflowXaml);

                    if (!string.IsNullOrEmpty(runtimeAssemblyPath) && File.Exists(runtimeAssemblyPath))
                    {
                        string radllPath = Path.Combine(storePath, RuntimeAssembly_dll);
                        
                        // Copy if file locations are different.
                        // Both paths are same when a suspended job from another console is resumed and end persistence happened.
                        if (!String.Equals(radllPath, runtimeAssemblyPath, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Copy(runtimeAssemblyPath, Path.Combine(storePath, RuntimeAssembly_dll));
                        }
                    }

                    if (definition.RequiredAssemblies != null)
                    {
                        if(_disablePersistenceLimits)
                        {
                            SerializeAndSaveToFile(definition.RequiredAssemblies, Path.Combine(storePath, RequiredAssemblies_xml));
                        }
                        else
                        {
                            SaveToFile(this.serializedRequiredAssemblies, Path.Combine(storePath, RequiredAssemblies_xml));
                        }
                    }
                }
                catch (Exception e)
                {
                    Tracer.TraceException(e);
                    throw;
                }
            }
        }

        private long GetSavedDefinitionLength()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Definition);

            long size = 0;

            if (Directory.Exists(storePath))
            {
                FileInfo info = new FileInfo(Path.Combine(storePath, WorkflowDefinition_xaml));
                size += (info.Exists) ? info.Length : 0;
                info = new FileInfo(Path.Combine(storePath, RuntimeAssembly_dll));
                size += (info.Exists) ? info.Length : 0;
                info = new FileInfo(Path.Combine(storePath, RequiredAssemblies_xml));
                size += (info.Exists) ? info.Length : 0;
            }

            return size;
        }

        # endregion

        # region Job State

        private ArraySegment<byte> serializedJobState;

        private long LoadSerializedJobState(object data)
        {
            this.serializedJobState = Encrypt(SerializeObject(data));

            long size = this.serializedJobState.Count;

            return size;
        }

        private void SaveSerializedJobState()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Metadatas);
            EnsureInstancePath(storePath);

            Debug.Assert(PSWorkflowInstance.PSWorkflowJob != null,
                         "When SerializeWorkflowJobStateToStore is called, the _job instance needs to be populated");
            SaveToFile(this.serializedJobState, Path.Combine(storePath, WorkflowInstanceState_xml));
        }

        private long GetSavedJobStateLength()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Metadatas);

            long size = 0;

            if (Directory.Exists(storePath))
            {
                FileInfo info = new FileInfo(Path.Combine(storePath, WorkflowInstanceState_xml));
                size += (info.Exists) ? info.Length : 0;
            }

            return size;
        }

        # endregion

        #region ActivityState
        private ArraySegment<byte> serializedActivityState;
        private long LoadSerializedActivityState(PSWorkflowRemoteActivityState data)
        {
            this.serializedActivityState = Encrypt(SerializeObject(data.GetSerializedData()));
            return this.serializedActivityState.Count;
        }

        private void SaveSerializedActivityState()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Metadatas);
            EnsureInstancePath(storePath);

            SaveToFile(this.serializedActivityState, Path.Combine(storePath, ActivityState_xml));
        }

        private long GetSavedActivityStateLength()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Metadatas);

            long size = 0;

            if (Directory.Exists(storePath))
            {
                FileInfo info = new FileInfo(Path.Combine(storePath, ActivityState_xml));
                size += (info.Exists) ? info.Length : 0;
            }

            return size;
        }
        #endregion ActivityState


        # region WorkflowContext

        private ArraySegment<byte> serializedContext;

        private long LoadSerializedContext(object data)
        {
            this.serializedContext = Encrypt(SerializeObject(data));

            long size = this.serializedContext.Count;

            return size;
        }

        private void SaveSerializedContext()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), WorkflowState);
            EnsureInstancePath(storePath);

            SaveToFile(this.serializedContext, Path.Combine(storePath, State_xml));
        }

        private long GetSavedContextLength()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), WorkflowState);

            long size = 0;

            if (Directory.Exists(storePath))
            {
                FileInfo info = new FileInfo(Path.Combine(storePath, State_xml));
                size += (info.Exists) ? info.Length : 0;
            }

            return size;
        }

        # endregion


        # endregion

        # region Deserialization

        private PowerShellStreams<PSObject, PSObject> DeserializeWorkflowStreamsFromStore()
        {
            PowerShellStreams<PSObject, PSObject> stream = new PowerShellStreams<PSObject, PSObject>(null);

            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Streams);

            if (Directory.Exists(storePath))
            {
                if (_disablePersistenceLimits)
                {
                    stream.InputStream =
                        (PSDataCollection<PSObject>)
                        LoadFromFileAndDeserialize(Path.Combine(storePath, InputStream_xml));
                    stream.OutputStream =
                        (PSDataCollection<PSObject>)
                        LoadFromFileAndDeserialize(Path.Combine(storePath, OutputStream_xml));
                    stream.ErrorStream =
                        (PSDataCollection<ErrorRecord>)
                        LoadFromFileAndDeserialize(Path.Combine(storePath, ErrorStream_xml));
                    stream.WarningStream =
                        (PSDataCollection<WarningRecord>)
                        LoadFromFileAndDeserialize(Path.Combine(storePath, WarningStream_xml));
                    stream.VerboseStream =
                        (PSDataCollection<VerboseRecord>)
                        LoadFromFileAndDeserialize(Path.Combine(storePath, VerboseStream_xml));
                    stream.ProgressStream =
                        (PSDataCollection<ProgressRecord>)
                        LoadFromFileAndDeserialize(Path.Combine(storePath, ProgressStream_xml));
                    stream.DebugStream =
                        (PSDataCollection<DebugRecord>)
                        LoadFromFileAndDeserialize(Path.Combine(storePath, DebugStream_xml));
                    stream.InformationStream =
                        (PSDataCollection<InformationRecord>)
                        LoadFromFileAndDeserialize(Path.Combine(storePath, InformationStream_xml));
                }
                else
                {
                    stream.InputStream =
                        (PSDataCollection<PSObject>)
                        DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, InputStream_xml))));
                    stream.OutputStream =
                        (PSDataCollection<PSObject>)
                        DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, OutputStream_xml))));
                    stream.ErrorStream =
                        (PSDataCollection<ErrorRecord>)
                        DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, ErrorStream_xml))));
                    stream.WarningStream =
                        (PSDataCollection<WarningRecord>)
                        DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, WarningStream_xml))));
                    stream.VerboseStream =
                        (PSDataCollection<VerboseRecord>)
                        DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, VerboseStream_xml))));
                    stream.ProgressStream =
                        (PSDataCollection<ProgressRecord>)
                        DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, ProgressStream_xml))));
                    stream.DebugStream =
                        (PSDataCollection<DebugRecord>)
                        DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, DebugStream_xml))));
                    stream.InformationStream =
                        (PSDataCollection<InformationRecord>)
                        DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, InformationStream_xml))));
                }
            }

            return stream;
        }

        private Exception DeserializeWorkflowErrorExceptionFromStore()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Error);

            if (Directory.Exists(storePath) == false)
                return null;

            return _disablePersistenceLimits
                       ? (Exception)LoadFromFileAndDeserialize(Path.Combine(storePath, ErrorException_xml))
                       : (Exception)
                         DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, ErrorException_xml))));
        }

        private object DeserializeWorkflowTimerFromStore()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Metadatas);

            if (Directory.Exists(storePath) == false)
                return null;

            return _disablePersistenceLimits
                       ? LoadFromFileAndDeserialize(Path.Combine(storePath, Timer_xml))
                       : DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, Timer_xml))));
        }

        private PSWorkflowContext DeserializeWorkflowMetadataFromStore()
        {
            PSWorkflowContext metadata = new PSWorkflowContext();

            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Metadatas);

            if (Directory.Exists(storePath) == false)
                return metadata;

            if (_disablePersistenceLimits)
            {
                metadata.WorkflowParameters =
                    (Dictionary<string, object>)
                    LoadFromFileAndDeserialize(Path.Combine(storePath, Input_xml));
                metadata.PSWorkflowCommonParameters =
                    (Dictionary<string, object>)
                    LoadFromFileAndDeserialize(Path.Combine(storePath, PSWorkflowCommonParameters_xml));
                metadata.JobMetadata =
                    (Dictionary<string, object>)
                    LoadFromFileAndDeserialize(Path.Combine(storePath, JobMetadata_xml));
                metadata.PrivateMetadata =
                    (Dictionary<string, object>)
                    LoadFromFileAndDeserialize(Path.Combine(storePath, PrivateMetadata_xml));
            }
            else
            {
                metadata.WorkflowParameters =
                    (Dictionary<string, object>)
                    DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, Input_xml))));
                metadata.PSWorkflowCommonParameters =
                    (Dictionary<string, object>)
                    DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, PSWorkflowCommonParameters_xml))));
                metadata.JobMetadata =
                    (Dictionary<string, object>)
                    DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, JobMetadata_xml))));
                metadata.PrivateMetadata =
                    (Dictionary<string, object>)
                    DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, PrivateMetadata_xml))));
            }
            return metadata;
        }

        /// <summary>
        /// To be called from test code ONLY.
        /// </summary>
        /// <param name="componentTypes"></param>
        internal IEnumerable<object> CallDoLoad(IEnumerable<Type> componentTypes)
        {
            return DoLoad(componentTypes);
        }

        /// <summary>
        /// DoLoad
        /// </summary>
        /// <param name="componentTypes"></param>
        protected override IEnumerable<object> DoLoad(IEnumerable<Type> componentTypes)
        {
            Collection<object> loadedComponents = new Collection<object>();

            lock (_syncLock)
            {
                foreach (Type componentType in componentTypes)
                {
                    if (componentType == typeof(JobState))
                    {
                        JobState? state = DeserializeWorkflowInstanceStateFromStore();
                        if (state != null)
                        {
                            JobState jobState = (JobState)state;
                            if (jobState == JobState.Running || jobState == JobState.Suspended || jobState == JobState.Suspending || jobState == JobState.Stopping || jobState == JobState.NotStarted)
                                jobState = JobState.Suspended;

                            loadedComponents.Add(jobState);
                        }
                    }
                    else if (componentType == typeof(Dictionary<string, object>))
                    {
                        Dictionary<string, object> context = DeserializeContextFromStore();

                        if (context != null)
                            loadedComponents.Add(context);
                    }
                    else if (componentType == typeof(PSWorkflowDefinition))
                    {
                        PSWorkflowDefinition workflowDefinition = DeserializeWorkflowDefinitionFromStore();

                        if (workflowDefinition != null)
                            loadedComponents.Add(workflowDefinition);
                    }
                    else if (componentType == typeof(Exception))
                    {
                        Exception exception = DeserializeWorkflowErrorExceptionFromStore();

                        if (exception != null)
                            loadedComponents.Add(exception);
                    }
                    else if (componentType == typeof(PSWorkflowContext))
                    {
                        loadedComponents.Add(DeserializeWorkflowMetadataFromStore());
                    }
                    else if (componentType == typeof(PowerShellStreams<PSObject, PSObject>))
                    {
                        loadedComponents.Add((PowerShellStreams<PSObject, PSObject>)DeserializeWorkflowStreamsFromStore());
                    }
                    else if (componentType == typeof(PSWorkflowTimer))
                    {
                        object value = DeserializeWorkflowTimerFromStore();
                        loadedComponents.Add( value == null ? new PSWorkflowTimer(PSWorkflowInstance) : new PSWorkflowTimer(PSWorkflowInstance, value));
                    }
                    else if (componentType == typeof(PSWorkflowRemoteActivityState))
                    {
                        Dictionary<string, Dictionary<int, Tuple<object, string>>> value = DeserializeActivityStateFromStore();
                        loadedComponents.Add(value == null ? new PSWorkflowRemoteActivityState(this) : new PSWorkflowRemoteActivityState(this, value));
                    }
                }
            }

            return loadedComponents;
        }

        internal JobState? DeserializeWorkflowInstanceStateFromStore()
        {
            string filePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Metadatas, WorkflowInstanceState_xml);

            if (File.Exists(filePath) == false)
            {
                return null;
            }

            return _disablePersistenceLimits
                       ? (JobState) LoadFromFileAndDeserialize(filePath)
                       : (JobState) DeserializeObject(Decrypt(LoadFromFile(filePath)));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        private PSWorkflowDefinition DeserializeWorkflowDefinitionFromStore()
        {
            string WorkflowXaml = string.Empty;
            string runtimeAssemblyPath = null;
            Dictionary<string, string> requiredAssemblies = null;

            try
            {
                string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Definition);

                if (Directory.Exists(storePath) == false)
                    return null;

                WorkflowXaml = File.ReadAllText(Path.Combine(storePath, WorkflowDefinition_xaml));

                if (File.Exists(Path.Combine(storePath, RuntimeAssembly_dll)))
                {
                    runtimeAssemblyPath = Path.Combine(storePath, RuntimeAssembly_dll);
                }

                if (File.Exists(Path.Combine(storePath, RequiredAssemblies_xml)))
                {
                    requiredAssemblies = _disablePersistenceLimits
                       ? (Dictionary<string, string>) LoadFromFileAndDeserialize(Path.Combine(storePath, RequiredAssemblies_xml))
                       : (Dictionary<string, string>) DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, RequiredAssemblies_xml))));
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                throw;
            }

            var definition = new PSWorkflowDefinition(null, WorkflowXaml, runtimeAssemblyPath, requiredAssemblies);

            // Load required assemblies
            if (requiredAssemblies != null && requiredAssemblies.Values.Count > 0)
            {
                foreach (string requiredAssembly in requiredAssemblies.Values)
                {
                    try
                    {
                        // Try first from the GAC
                        Assembly.Load(requiredAssembly);
                    }
                    catch (IOException)
                    {
                        try
                        {
                            // And second by path
                            Assembly.LoadFrom(requiredAssembly);
                        }
                        catch (IOException)
                        {
                            // Finally, by relative path
                            if (System.Management.Automation.Runspaces.Runspace.DefaultRunspace != null)
                            {
                                using (System.Management.Automation.PowerShell nestedPs =
                                    System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace))
                                {
                                    nestedPs.AddCommand("Get-Location");
                                    PathInfo result = nestedPs.Invoke<PathInfo>()[0];

                                    string currentLocation = result.ProviderPath;

                                    try
                                    {
                                        Assembly.LoadFrom(Path.Combine(currentLocation, requiredAssembly));
                                    }
                                    catch (IOException)
                                    {
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (definition.Workflow == null && string.IsNullOrEmpty(definition.WorkflowXaml) == false)
            {
                if (string.IsNullOrEmpty(definition.RuntimeAssemblyPath))
                {
                    definition.Workflow = ImportWorkflowCommand.ConvertXamlToActivity(definition.WorkflowXaml);
                }
                else
                {
                    Assembly assembly = Assembly.LoadFrom(definition.RuntimeAssemblyPath);
                    string assemblyName = assembly.GetName().Name;
                    string assemblyPath = definition.RuntimeAssemblyPath;
                    definition.Workflow = ImportWorkflowCommand.ConvertXamlToActivity(definition.WorkflowXaml, null, requiredAssemblies, ref assemblyPath, ref assembly, ref assemblyName);
                }
            }

            return definition;
        }

        internal Dictionary<string, object> DeserializeContextFromStore()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), WorkflowState);

            if (Directory.Exists(storePath) == false)
                return null;

            Dictionary<string, object> context = _disablePersistenceLimits
                                     ? (Dictionary<string, object>) LoadFromFileAndDeserialize(Path.Combine(storePath, State_xml))
                                     : (Dictionary<string, object>)
                                       DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, State_xml))));
            return context;
        }


        internal Dictionary<string, Dictionary<int, Tuple<object, string>>> DeserializeActivityStateFromStore()
        {
            string storePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString(), Metadatas);

            if (Directory.Exists(storePath) == false)
                return null;

            return _disablePersistenceLimits
                        ? (Dictionary<string, Dictionary<int, Tuple<object, string>>>)LoadFromFileAndDeserialize(Path.Combine(storePath, ActivityState_xml))
                        : (Dictionary<string, Dictionary<int, Tuple<object, string>>>)DeserializeObject(Decrypt(LoadFromFile(Path.Combine(storePath, ActivityState_xml))));        
        }

        # endregion

        # region Helper Methods

        private static readonly byte[] NullArray = {(byte) 'N', (byte) 'U', (byte) 'L', (byte) 'L'};
        private static readonly byte[] EncryptFalse = {(byte) 'F'};

        internal ArraySegment<byte> SerializeObject(object source)
        {
            if (_version.EnableCompression)
                return SerializeObject2(source);

            ArraySegment<byte> scratch = new ArraySegment<byte>(NullArray);

            try
            {
                XmlObjectSerializer serializer = new NetDataContractSerializer();

                using (MemoryStream outputStream = new MemoryStream())
                {
                    serializer.WriteObject(outputStream, source);

                    scratch = new ArraySegment<byte>(outputStream.GetBuffer(), 0, (int)outputStream.Length);
                }
            }
            catch (SerializationException e)
            {
                this.ThrowErrorOrWriteWarning(e);
            }
            catch (InvalidDataContractException e)
            {
                this.ThrowErrorOrWriteWarning(e);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                throw;
            }

            return scratch;
        }

        private void WriteWarning(string warningMessage)
        {
            Tracer.WriteMessage("WorkflowAdditionalStores", "WriteWarning", this.PSWorkflowInstance.Id, warningMessage);

            if (PSWorkflowInstance.Streams.WarningStream.IsOpen)
            {
                PSWorkflowInstance.Streams.WarningStream.Add(new WarningRecord(warningMessage));
            }
        }

        private void ThrowErrorOrWriteWarning(Exception e)
        {
            if (PSSessionConfigurationData.IsServerManager)
            {
                string warningMessage = string.Format(CultureInfo.CurrentCulture, Resources.SerializationWarning, e.Message);
                this.WriteWarning(warningMessage);
            }
            else
            {
                // there is no point in hanging around partial data in the store so deleting the store.
                // and no further persistence allowed after this point
                this.InternalDelete();
                serializationErrorHasOccured = true;

                SerializationException exception = new SerializationException(Resources.SerializationErrorException, e);
                throw exception;
            }
        }
        private bool serializationErrorHasOccured = false;

        internal object DeserializeObject(ArraySegment<byte> source)
        {
            try
            {
                XmlObjectSerializer serializer = new NetDataContractSerializer();
                using (MemoryStream inputStream = new MemoryStream(source.Array, source.Offset, source.Count))
                {
                    object scratch = serializer.ReadObject(inputStream);

                    return scratch;
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                throw;
            }
        }

        /// <summary>
        /// Encrypt
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        protected internal virtual ArraySegment<byte> Encrypt(ArraySegment<byte> source)
        {
            if (_version.EnableCompression)
                return Encrypt2(source);

            if (!_version.EnableEncryption)
            {
                byte[] data = new byte[source.Count + 1];
                data[0] = (byte) 'F';
                Buffer.BlockCopy(source.Array, 0, data, 1, source.Count);

                ArraySegment<byte> scratch = new ArraySegment<byte>(data, 0, data.Length);

                return scratch;
            }
            else
            {
                byte[] data = new byte[source.Count];
                Buffer.BlockCopy(source.Array, 0, data, 0, source.Count);
                byte[] encryptedData = InstanceStoreCryptography.Protect(data);

                byte[] finalData = new byte[encryptedData.Length + 1];
                finalData[0] = (byte) 'T';
                Buffer.BlockCopy(encryptedData, 0, finalData, 1, encryptedData.Length);

                ArraySegment<byte> scratch = new ArraySegment<byte>(finalData, 0, encryptedData.Length + 1);

                return scratch;
            }
        }

        /// <summary>
        /// Decrypt
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        protected internal virtual ArraySegment<byte> Decrypt(ArraySegment<byte> source)
        {
            bool localEncrypt = false;
            if (source.Array[0] == 'T')
            {
                localEncrypt = true;
            }

            if (!localEncrypt)
            {
                ArraySegment<byte> scratch = new ArraySegment<byte>(source.Array, 1, source.Count - 1);

                return scratch;
            }
            else
            {
                byte[] data = new byte[source.Count - 1];
                Buffer.BlockCopy(source.Array, 1, data, 0, source.Count - 1);

                byte[] decryptedData = InstanceStoreCryptography.Unprotect(data);

                ArraySegment<byte> scratch = new ArraySegment<byte>(decryptedData, 0, decryptedData.Length);

                return scratch;
            }
        }

        private void SaveToFile(ArraySegment<byte> source, string filePath)
        {
            if (_version.EnableCompression)
            {
                SaveToFile2(source, filePath);
                return;
            }
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(source.Array, 0, source.Count);
                    stream.Flush();
                    stream.Close();
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                throw;
            }
        }

        private ArraySegment<byte> LoadFromFile(string filePath)
        {
            if (_version.EnableCompression)
                return LoadFromFile2(filePath);

            ArraySegment<byte> tmpBuf = new ArraySegment<byte>();

            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    if (stream.Length != 0)
                    {
                        byte[] buf = new byte[stream.Length];
                        stream.Read(buf, 0, (int) stream.Length);
                        tmpBuf = new ArraySegment<byte>(buf, 0, (int) stream.Length);
                    }

                    stream.Close();
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                throw;
            }

            return tmpBuf;
        }

        #endregion


        #region HighPerf Changes

        private bool _disablePersistenceLimits = false;

        internal ArraySegment<byte> SerializeObject2(object source)
        {
            ArraySegment<byte> scratch = new ArraySegment<byte>(NullArray);

            try
            {
                XmlObjectSerializer serializer = new NetDataContractSerializer();

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    serializer.WriteObject(memoryStream, source);

                    byte[] data = new byte[memoryStream.Length + 1];
                    data[0] = (byte) 'F';
                    Buffer.BlockCopy(memoryStream.GetBuffer(), 0, data, 1, (int) memoryStream.Length);
                    scratch = new ArraySegment<byte>(data, 0, data.Length);
                    memoryStream.Close();
                }
            }
            catch (SerializationException e)
            {
                this.ThrowErrorOrWriteWarning(e);
            }
            catch (InvalidDataContractException e)
            {
                this.ThrowErrorOrWriteWarning(e);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                throw;
            }

            return scratch;
        }

        internal object DeserializeObject2(ArraySegment<byte> source)
        {
            try
            {
                XmlObjectSerializer serializer = new NetDataContractSerializer();
                using (MemoryStream inputStream = new MemoryStream(source.Array, source.Offset, source.Count))
                {
                    object scratch = serializer.ReadObject(inputStream);

                    return scratch;
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                throw;
            }
        }

        /// <summary>
        /// Serialize an object and directly write to the file
        /// </summary>
        /// <param name="source">object to serialize</param>
        /// <param name="filePath">file to write to</param>
        /// <returns>number of bytes written</returns>
        internal int SerializeAndSaveToFile(object source, string filePath)
        {
            // if there is encryption involved we follow the old non performant
            // code path
            if (_version.EnableEncryption)
            {
                ArraySegment<byte> serializedData = Encrypt(SerializeObject(source));
                SaveToFile2(serializedData, filePath);
                return serializedData.Count;
            }

            FileStream stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            GZipStream compressedStream = new GZipStream(stream, CompressionMode.Compress);

            bool sError = false;
            Exception sException = null;

            try
            {
                XmlObjectSerializer serializer = new NetDataContractSerializer();
                compressedStream.Write(EncryptFalse, 0, 1);
                serializer.WriteObject(compressedStream, source);
                return (int) stream.Length;
            }
            catch (SerializationException e)
            {
                sError = true;
                sException = e;
            }
            catch (InvalidDataContractException e)
            {
                sError = true;
                sException = e;
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                throw;
            }
            finally
            {
                compressedStream.Close();
                stream.Close();
                compressedStream.Dispose();
                stream.Dispose();
            }

            // this is needed because we want all the streams to closed before executing this method.
            if(sError == true)
                this.ThrowErrorOrWriteWarning(sException);

            return 0;
        }

        internal object LoadFromFileAndDeserialize(string filePath)
        {
            FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            try
            {
                if (stream.Length != 0)
                {
                    GZipStream decompressedStream = new GZipStream(stream, CompressionMode.Decompress);

                    try
                    {
                        byte[] marker = new byte[1];
                        decompressedStream.Read(marker, 0, 1);

                        if (marker[0] == 'T')
                        {
                            // if there is encryption involved we follow the old non performant
                            // code path
                            decompressedStream.Close();
                            stream.Close();
                            ArraySegment<byte> serializedContents = LoadFromFile2(filePath);
                            return DeserializeObject2(Decrypt(serializedContents));
                        }

                        XmlObjectSerializer serializer = new NetDataContractSerializer();
                        return serializer.ReadObject(decompressedStream);
                    }
                    catch (Exception e)
                    {
                        Tracer.TraceException(e);
                        throw;
                    }
                    finally
                    {
                        decompressedStream.Close();
                        decompressedStream.Dispose();
                    }
                }
            }
            finally
            {
                stream.Close();
                stream.Dispose();
            }
            return null;
        }

        private void SaveToFile2(ArraySegment<byte> source, string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (GZipStream compressedStream = new GZipStream(stream, CompressionMode.Compress))
                    {
                        compressedStream.Write(source.Array, 0, source.Count);
                        compressedStream.Flush();
                        compressedStream.Close();
                    }
                    stream.Close();
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                throw;
            }
        }

        private ArraySegment<byte> LoadFromFile2(string filePath)
        {
            ArraySegment<byte> tmpBuf = new ArraySegment<byte>();

            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    if (stream.Length != 0)
                    {
                        using (GZipStream decompressedStream = new GZipStream(stream, CompressionMode.Decompress))
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                decompressedStream.CopyTo(memoryStream);
                                tmpBuf = new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                                decompressedStream.Close();
                                memoryStream.Close();
                            }
                        }
                    }

                    stream.Close();
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                throw;
            }

            return tmpBuf;
        }        

        internal void DoSave2(IEnumerable<object> components)
        {
            lock (_syncLock)
            {
                string storePath;
                long totalBytesWritten = 0;

                if (writtenTotalBytes == 0)
                {
                    writtenTotalBytes = this.GetSavedContextLength() +
                                        this.GetSavedStreamDataLength() +
                                        this.GetSavedMetadataLength() +
                                        this.GetSavedDefinitionLength() +
                                        this.GetSavedTimerLength() +
                                        this.GetSavedJobStateLength() +
                                        this.GetSavedErrorExceptionLength();
                }

                foreach (object component in components)
                {
                    Type componentType = component.GetType();

                    if (componentType == typeof(Dictionary<string, object>))
                    {
                        CreateAndEnsureInstancePath(WorkflowState, out storePath);

                        totalBytesWritten += SerializeAndSaveToFile(component, Path.Combine(storePath, State_xml));
                    }
                    else if (componentType == typeof(PowerShellStreams<PSObject, PSObject>))
                    {
                        CreateAndEnsureInstancePath(Streams, out storePath);

                        Debug.Assert(component.GetType() == typeof(PowerShellStreams<PSObject, PSObject>),
                                     "The data should be of type workflow stream");
                        PowerShellStreams<PSObject, PSObject> streams = (PowerShellStreams<PSObject, PSObject>)component;

                        totalBytesWritten += SerializeAndSaveToFile(streams.InputStream, Path.Combine(storePath, InputStream_xml));

                        long bytesWritten = 0; 
                        bytesWritten = SerializeAndSaveToFile(streams.OutputStream,
                                                              Path.Combine(storePath, OutputStream_xml));
                        totalBytesWritten += bytesWritten;

                        bytesWritten = SerializeAndSaveToFile(streams.ErrorStream, Path.Combine(storePath, ErrorStream_xml));
                        totalBytesWritten += bytesWritten; 


                        bytesWritten = SerializeAndSaveToFile(streams.WarningStream,
                                                              Path.Combine(storePath, WarningStream_xml));
                        totalBytesWritten += bytesWritten;

                        bytesWritten = SerializeAndSaveToFile(streams.VerboseStream,
                                                              Path.Combine(storePath, VerboseStream_xml));
                        totalBytesWritten += bytesWritten; 
                        
                        bytesWritten = SerializeAndSaveToFile(streams.ProgressStream,
                                                              Path.Combine(storePath, ProgressStream_xml));
                        totalBytesWritten += bytesWritten; 
                        
                        bytesWritten = SerializeAndSaveToFile(streams.DebugStream, Path.Combine(storePath, DebugStream_xml));
                        totalBytesWritten += bytesWritten; 

                        bytesWritten = SerializeAndSaveToFile(streams.InformationStream, Path.Combine(storePath, InformationStream_xml));
                        totalBytesWritten += bytesWritten;
                    }
                    else if (componentType == typeof(PSWorkflowContext))
                    {
                        CreateAndEnsureInstancePath(Metadatas, out storePath);

                        Debug.Assert(component.GetType() == typeof(PSWorkflowContext),
                                     "The data should be of type workflow metadata");
                        PSWorkflowContext metadata = (PSWorkflowContext)component;

                        totalBytesWritten += SerializeAndSaveToFile(metadata.WorkflowParameters, Path.Combine(storePath, Input_xml));
                        totalBytesWritten += SerializeAndSaveToFile(metadata.PSWorkflowCommonParameters,
                                               Path.Combine(storePath, PSWorkflowCommonParameters_xml));
                        totalBytesWritten += SerializeAndSaveToFile(metadata.JobMetadata, Path.Combine(storePath, JobMetadata_xml));
                        totalBytesWritten += SerializeAndSaveToFile(metadata.PrivateMetadata,
                                               Path.Combine(storePath, PrivateMetadata_xml));
                    }
                    else if (componentType == typeof(PSWorkflowDefinition))
                    {
                        if (firstTimeStoringDefinition)
                        {
                            Debug.Assert(component.GetType() == typeof(PSWorkflowDefinition),
                                         "The data should be of type workflow definition");
                            PSWorkflowDefinition definition = (PSWorkflowDefinition)component;

                            // there is no serialization involved, just storing xamls and reference
                            // assemblies. So existing method is good
                            SaveSerializedDefinition(definition);

                            totalBytesWritten += this.GetSavedDefinitionLength();
                        }
                    }
                    else if (componentType == typeof(PSWorkflowTimer))
                    {
                        // timer is stored in the metadatas folder
                        CreateAndEnsureInstancePath(Metadatas, out storePath);

                        PSWorkflowTimer data = (PSWorkflowTimer)component;
                        totalBytesWritten += SerializeAndSaveToFile(data.GetSerializedData(), Path.Combine(storePath, Timer_xml));
                    }
                    else if (componentType == typeof(JobState))
                    {
                        CreateAndEnsureInstancePath(Metadatas, out storePath);
                        totalBytesWritten += SerializeAndSaveToFile(component, Path.Combine(storePath, WorkflowInstanceState_xml));
                    }
                    else if (component is Exception)
                    {
                        CreateAndEnsureInstancePath(Error, out storePath);
                        totalBytesWritten += SerializeAndSaveToFile(component, Path.Combine(storePath, ErrorException_xml));
                    }
                    else if (component is PSWorkflowRemoteActivityState)
                    {
                        CreateAndEnsureInstancePath(Metadatas, out storePath);

                        PSWorkflowRemoteActivityState data = (PSWorkflowRemoteActivityState) component;
                        totalBytesWritten += SerializeAndSaveToFile(data.GetSerializedData(), Path.Combine(storePath, ActivityState_xml));
                    }
                }

                long oldValueTotalBytesWritten = writtenTotalBytes;
                writtenTotalBytes = totalBytesWritten;

                bool allowed = CheckMaxPersistenceSize(oldValueTotalBytesWritten, writtenTotalBytes);

                // if not allowed then write the warning message and continue execution.
                if (allowed == false)
                {
                    this.WriteWarning(Resources.PersistenceSizeReached);
                    etwTracer.PersistenceStoreMaxSizeReached();
                }
            }
        }

        private string WorkflowStorePath
        {
            get
            {
                // it is fine if this segment is not thread-safe
                if (String.IsNullOrEmpty(_workflowStorePath))
                {
                    _workflowStorePath = Path.Combine(_configuration.InstanceStorePath, PSWorkflowInstance.Id.ToString());
                }
                return _workflowStorePath;
            }
        }

        private string _workflowStorePath;

        private void CreateAndEnsureInstancePath(string subPath, out string storePath)
        {
            storePath = Path.Combine(WorkflowStorePath, subPath);
            EnsureInstancePath(storePath);
        }

        private ArraySegment<byte> Encrypt2(ArraySegment<byte> source)
        {
            if (!_version.EnableEncryption)
            {
                return source;
            }
            byte[] data = new byte[source.Count - 1];
            Buffer.BlockCopy(source.Array, 1, data, 0, source.Count - 1);

            byte[] encryptedData = InstanceStoreCryptography.Protect(data);
            byte[] finalData = new byte[encryptedData.Length + 1];
            finalData[0] = (byte)'T';
            Buffer.BlockCopy(encryptedData, 0, finalData, 1, encryptedData.Length);

            ArraySegment<byte> scratch = new ArraySegment<byte>(finalData, 0, encryptedData.Length + 1);

            return scratch;
        }

        #endregion HighPerf Changes

        private static readonly object MaxPersistenceStoreSizeLock = new object();
        internal static long CurrentPersistenceStoreSize = 0;
        private static bool _firstTimeCalculatingCurrentStoreSize = true;

        private bool CheckMaxPersistenceSize(long oldValue, long newValue)
        {
            CalculatePersistenceStoreSizeForFirstTime(); // should not be in lock

            lock (MaxPersistenceStoreSizeLock)
            {
                long calculatedStoreSize = CurrentPersistenceStoreSize - oldValue + newValue;

                long endpointValueInBytes = _configuration.MaxPersistenceStoreSizeGB*1024*1024*1024;

                if (calculatedStoreSize < endpointValueInBytes)
                {
                    CurrentPersistenceStoreSize = calculatedStoreSize;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void ReducePersistenceSize(long value)
        {
            CalculatePersistenceStoreSizeForFirstTime(); // should not be in lock

            lock (MaxPersistenceStoreSizeLock)
            {
                CurrentPersistenceStoreSize -= value;
            }
        }

        internal void CalculatePersistenceStoreSizeForFirstTime()
        {
            // there is no need to calculate the store for the first time because
            // there might be possibility of one process trying to get the length
            // and other is trying to delete it.

            if (this._configuration.IsDefaultStorePath)
                return;

            if (_firstTimeCalculatingCurrentStoreSize == false)
                return;

            lock (MaxPersistenceStoreSizeLock)
            {
                if (_firstTimeCalculatingCurrentStoreSize == false)
                    return;

                _firstTimeCalculatingCurrentStoreSize = false;
                CurrentPersistenceStoreSize = GetDirectoryLength(new DirectoryInfo(_configuration.InstanceStorePath));
            }
        }

        private long GetDirectoryLength(DirectoryInfo directory)
        {
            long size = 0;

            try
            {
                if (directory.Exists)
                {
                    DirectoryInfo[] dirs = directory.GetDirectories();
                    FileInfo[] files = directory.GetFiles();
                    foreach (FileInfo file in files)
                    {
                        size += file.Exists ? file.Length : 0;
                    }
                    foreach (DirectoryInfo dir in dirs)
                    {
                        size += GetDirectoryLength(dir);
                    }
                }
            }
                // It is safe of absorb an exception here because there might be a possibility that the multiple 
                // processes are try work on the same endpoint or default end point store folder.
                // There is a possibility that one process has deleted the a workflow store and the other one is trying to access it for calculating its store size.
                // And we will set the flag to make sure we calculate the size next time.
            catch (DirectoryNotFoundException e)
            {
                Tracer.TraceException(e);
                _firstTimeCalculatingCurrentStoreSize = true;
            }
            catch (FileNotFoundException e)
            {
                Tracer.TraceException(e);
                _firstTimeCalculatingCurrentStoreSize = true;
            }
            catch (UnauthorizedAccessException e)
            {
                Tracer.TraceException(e);
                _firstTimeCalculatingCurrentStoreSize = true;
            }

            return size;
        }
    }
}
    ;
