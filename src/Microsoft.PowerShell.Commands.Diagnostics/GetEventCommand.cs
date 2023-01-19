// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Management.Automation;
using System.Net;
using System.Resources;
using System.Security.Principal;
using System.Text;
using System.Xml;

[assembly: CLSCompliant(false)]

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Class that implements the Get-WinEvent cmdlet.
    /// </summary>
    [OutputType(typeof(EventRecord), ParameterSetName = new string[] { "GetLogSet", "GetProviderSet", "FileSet", "HashQuerySet", "XmlQuerySet" })]
    [OutputType(typeof(ProviderMetadata), ParameterSetName = new string[] { "ListProviderSet" })]
    [OutputType(typeof(EventLogConfiguration), ParameterSetName = new string[] { "ListLogSet" })]
    [Cmdlet(VerbsCommon.Get, "WinEvent", DefaultParameterSetName = "GetLogSet", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096581")]
    public sealed class GetWinEventCommand : PSCmdlet
    {
        /// <summary>
        /// ListLog parameter.
        /// </summary>
        [Parameter(
                Position = 0,
                Mandatory = true,
                ParameterSetName = "ListLogSet",
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "ListLogParamHelp")]
        [AllowEmptyCollection]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.GetEvent.ListLog",
                            Justification = "A string[] is required here because that is the type Powershell supports")]
        public string[] ListLog { get; set; } = { "*" };

        /// <summary>
        /// GetLog parameter.
        /// </summary>
        [Parameter(
                Position = 0,
                ParameterSetName = "GetLogSet",
                ValueFromPipeline = true,
                ValueFromPipelineByPropertyName = true,
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "GetLogParamHelp")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.GetEvent.LogName",
                            Justification = "A string[] is required here because that is the type Powershell supports")]
        public string[] LogName { get; set; } = { "*" };

        /// <summary>
        /// ListProvider parameter.
        /// </summary>
        [Parameter(
                Position = 0,
                Mandatory = true,
                ParameterSetName = "ListProviderSet",
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "ListProviderParamHelp")]
        [AllowEmptyCollection]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.GetEvent.ListProvider",
                            Justification = "A string[] is required here because that is the type Powershell supports")]
        public string[] ListProvider { get; set; } = { "*" };

        /// <summary>
        /// ProviderName parameter.
        /// </summary>
        [Parameter(
                Position = 0,
                Mandatory = true,
                ParameterSetName = "GetProviderSet",
                ValueFromPipelineByPropertyName = true,
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "GetProviderParamHelp")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.GetEvent.ProviderName",
                            Justification = "A string[] is required here because that is the type Powershell supports")]
        public string[] ProviderName { get; set; }

        /// <summary>
        /// Path parameter.
        /// </summary>
        [Parameter(
                Position = 0,
                Mandatory = true,
                ParameterSetName = "FileSet",
                ValueFromPipelineByPropertyName = true,
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "PathParamHelp")]
        [Alias("PSPath")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.GetEvent.Path",
                            Justification = "A string[] is required here because that is the type Powershell supports")]
        public string[] Path { get; set; }

        /// <summary>
        /// MaxEvents parameter.
        /// </summary>
        [Parameter(
                ParameterSetName = "FileSet",
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "MaxEventsParamHelp")]
        [Parameter(
                ParameterSetName = "GetProviderSet",
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "MaxEventsParamHelp")]
        [Parameter(
                ParameterSetName = "GetLogSet",
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "MaxEventsParamHelp")]
        [Parameter(
                ParameterSetName = "HashQuerySet",
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "MaxEventsParamHelp")]
        [Parameter(
                ParameterSetName = "XmlQuerySet",
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "MaxEventsParamHelp")]
        [ValidateRange((long)1, long.MaxValue)]
        public long MaxEvents { get; set; } = -1;

        /// <summary>
        /// ComputerName parameter.
        /// </summary>
        [Parameter(
                ParameterSetName = "ListProviderSet",
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "ComputerNameParamHelp")]
        [Parameter(
                ParameterSetName = "GetProviderSet",
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "ComputerNameParamHelp")]
        [Parameter(
                ParameterSetName = "ListLogSet",
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "ComputerNameParamHelp")]
        [Parameter(
                ParameterSetName = "GetLogSet",
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "ComputerNameParamHelp")]
        [Parameter(
                ParameterSetName = "HashQuerySet",
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "ComputerNameParamHelp")]
        [Parameter(
                ParameterSetName = "XmlQuerySet",
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "ComputerNameParamHelp")]
        [ValidateNotNull]
        [Alias("Cn")]
        public string ComputerName { get; set; } = string.Empty;

        /// <summary>
        /// Credential parameter.
        /// </summary>
        [Parameter(ParameterSetName = "ListProviderSet")]
        [Parameter(ParameterSetName = "GetProviderSet")]
        [Parameter(ParameterSetName = "ListLogSet")]
        [Parameter(ParameterSetName = "GetLogSet")]
        [Parameter(ParameterSetName = "HashQuerySet")]
        [Parameter(ParameterSetName = "XmlQuerySet")]
        [Parameter(ParameterSetName = "FileSet")]
        [Credential]
        public PSCredential Credential { get; set; } = PSCredential.Empty;

        /// <summary>
        /// FilterXPath parameter.
        /// </summary>
        [Parameter(
                ParameterSetName = "FileSet",
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources")]
        [Parameter(
                ParameterSetName = "GetProviderSet",
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources")]
        [Parameter(
                ParameterSetName = "GetLogSet",
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources")]
        [ValidateNotNull]
        public string FilterXPath { get; set; } = "*";

        /// <summary>
        /// FilterXml parameter.
        /// </summary>
        [Parameter(
                Position = 0,
                Mandatory = true,
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                ParameterSetName = "XmlQuerySet",
                HelpMessageBaseName = "GetEventResources")]
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.GetEvent.FilterXml",
                            Justification = "An XmlDocument is required here because that is the type Powershell supports")]
        public XmlDocument FilterXml { get; set; }

        /// <summary>
        /// FilterHashtable parameter.
        /// </summary>
        [Parameter(
                Position = 0,
                Mandatory = true,
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                ParameterSetName = "HashQuerySet",
                HelpMessageBaseName = "GetEventResources")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.GetEvent.FilterHashtable",
                            Justification = "A string[] is required here because that is the type Powershell supports")]
        public Hashtable[] FilterHashtable { get; set; }

        /// <summary>
        /// Force switch.
        /// </summary>
        [Parameter(ParameterSetName = "ListLogSet")]
        [Parameter(ParameterSetName = "GetProviderSet")]
        [Parameter(ParameterSetName = "GetLogSet")]
        [Parameter(ParameterSetName = "HashQuerySet")]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Oldest switch.
        /// </summary>
        [Parameter(ParameterSetName = "FileSet")]
        [Parameter(ParameterSetName = "GetProviderSet")]
        [Parameter(ParameterSetName = "GetLogSet")]
        [Parameter(ParameterSetName = "HashQuerySet")]
        [Parameter(ParameterSetName = "XmlQuerySet")]
        public SwitchParameter Oldest
        {
            get { return _oldest; }

            set { _oldest = value; }
        }

        private bool _oldest = false;

        //
        // Query builder constant strings
        //
        private const string queryListOpen = "<QueryList>";
        private const string queryListClose = "</QueryList>";
        private const string queryTemplate = "<Query Id=\"{0}\" Path=\"{1}\"><Select Path=\"{1}\">{2}</Select></Query>";
        private const string queryOpenerTemplate = "<Query Id=\"{0}\" Path=\"{1}\"><Select Path=\"{1}\">*";
        private const string queryCloser = "</Query>";
        private const string SelectCloser = "</Select>";
        private const string suppressOpener = "<Suppress>*";
        private const string suppressCloser = "</Suppress>";
        private const char propOpen = '[';
        private const char propClose = ']';
        private const string filePrefix = "file://";
        private const string NamedDataTemplate = "((EventData[Data[@Name='{0}']='{1}']) or (UserData/*/{0}='{1}'))";
        private const string DataTemplate = "(EventData/Data='{0}')";
        private const string SystemTimePeriodTemplate = "(System/TimeCreated[@SystemTime&gt;='{0}' and @SystemTime&lt;='{1}'])";
        private const string SystemTimeStartTemplate = "(System/TimeCreated[@SystemTime&gt;='{0}'])";
        private const string SystemTimeEndTemplate = "(System/TimeCreated[@SystemTime&lt;='{0}'])";
        private const string SystemLevelTemplate = "(System/Level=";
        private const string SystemEventIDTemplate = "(System/EventID=";
        private const string SystemSecurityTemplate = "(System/Security[@UserID='{0}'])";
        private const string SystemKeywordsTemplate = "System[band(Keywords,{0})]";

        //
        // Other private members and constants
        //
        private ResourceManager _resourceMgr = null;
        private readonly Dictionary<string, StringCollection> _providersByLogMap = new();

        private StringCollection _logNamesMatchingWildcard = null;
        private readonly StringCollection _resolvedPaths = new();

        private readonly List<string> _accumulatedLogNames = new();
        private readonly List<string> _accumulatedProviderNames = new();
        private readonly List<string> _accumulatedFileNames = new();

        private const uint MAX_EVENT_BATCH = 100;

        //
        // Hashtable query key names
        //
        private const string hashkey_logname_lc = "logname";
        private const string hashkey_providername_lc = "providername";
        private const string hashkey_path_lc = "path";
        private const string hashkey_keywords_lc = "keywords";
        private const string hashkey_id_lc = "id";
        private const string hashkey_level_lc = "level";
        private const string hashkey_starttime_lc = "starttime";
        private const string hashkey_endtime_lc = "endtime";
        private const string hashkey_userid_lc = "userid";
        private const string hashkey_data_lc = "data";
        private const string hashkey_supress_lc = "suppresshashfilter";

        /// <summary>
        /// BeginProcessing() is invoked once per pipeline: we will load System.Core.dll here.
        /// </summary>
        protected override void BeginProcessing()
        {
            _resourceMgr = Microsoft.PowerShell.Commands.Diagnostics.Common.CommonUtilities.GetResourceManager();
        }

        /// <summary>
        /// EndProcessing() is invoked once per pipeline.
        /// </summary>
        protected override void EndProcessing()
        {
            switch (ParameterSetName)
            {
                case "GetLogSet":
                    ProcessGetLog();
                    break;

                case "FileSet":
                    ProcessFile();
                    break;

                case "GetProviderSet":
                    ProcessGetProvider();
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// ProcessRecord() override.
        /// This is the main entry point for the cmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case "ListLogSet":
                    ProcessListLog();
                    break;

                case "ListProviderSet":
                    ProcessListProvider();
                    break;

                case "GetLogSet":
                    AccumulatePipelineLogNames();
                    break;

                case "FileSet":
                    AccumulatePipelineFileNames();
                    break;

                case "HashQuerySet":
                    ProcessHashQuery();
                    break;

                case "GetProviderSet":
                    AccumulatePipelineProviderNames();
                    break;

                case "XmlQuerySet":
                    ProcessFilterXml();
                    break;

                default:
                    WriteDebug(string.Create(CultureInfo.InvariantCulture, $"Invalid parameter set name: {ParameterSetName}"));
                    break;
            }
        }

        //
        // AccumulatePipelineCounters() accumulates log names in the pipeline scenario:
        // we do not want to construct a query until all the log names are supplied.
        //
        private void AccumulatePipelineLogNames()
        {
            _accumulatedLogNames.AddRange(LogName);
        }

        //
        // AccumulatePipelineProviderNames() accumulates provider names in the pipeline scenario:
        // we do not want to construct a query until all the provider names are supplied.
        //
        private void AccumulatePipelineProviderNames()
        {
            _accumulatedProviderNames.AddRange(LogName);
        }

        //
        // AccumulatePipelineFileNames() accumulates log file paths in the pipeline scenario:
        // we do not want to construct a query until all the file names are supplied.
        //
        private void AccumulatePipelineFileNames()
        {
            _accumulatedFileNames.AddRange(LogName);
        }

        //
        // Process GetLog parameter set
        //
        private void ProcessGetLog()
        {
            using (EventLogSession eventLogSession = CreateSession())
            {
                FindLogNamesMatchingWildcards(eventLogSession, _accumulatedLogNames);
                if (_logNamesMatchingWildcard.Count == 0)
                {
                    return;
                }

                EventLogQuery logQuery;
                if (_logNamesMatchingWildcard.Count > 1)
                {
                    string query = BuildStructuredQuery(eventLogSession);
                    logQuery = new EventLogQuery(null, PathType.LogName, query);
                    logQuery.TolerateQueryErrors = true;
                }
                else
                {
                    logQuery = new EventLogQuery(_logNamesMatchingWildcard[0], PathType.LogName, FilterXPath);
                }

                logQuery.Session = eventLogSession;
                logQuery.ReverseDirection = !_oldest;

                ReadEvents(logQuery);
            }
        }

        //
        // Process GetProviderSet parameter set
        //
        private void ProcessGetProvider()
        {
            using (EventLogSession eventLogSession = CreateSession())
            {
                FindProvidersByLogForWildcardPatterns(eventLogSession, ProviderName);

                if (_providersByLogMap.Count == 0)
                {
                    //
                    // Just return: errors already written above for each unmatched provider name pattern.
                    //
                    return;
                }

                EventLogQuery logQuery = null;
                if (_providersByLogMap.Count > 1)
                {
                    string query = BuildStructuredQuery(eventLogSession);
                    logQuery = new EventLogQuery(null, PathType.LogName, query);
                    logQuery.TolerateQueryErrors = true;
                }
                else
                {
                    //
                    // There's only one key at this point, but we need an enumerator to get to it.
                    //
                    foreach (string log in _providersByLogMap.Keys)
                    {
                        logQuery = new EventLogQuery(log, PathType.LogName, AddProviderPredicatesToFilter(_providersByLogMap[log]));
                        WriteVerbose(string.Create(CultureInfo.InvariantCulture, $"Log {log} will be queried"));
                    }
                }

                logQuery.Session = eventLogSession;
                logQuery.ReverseDirection = !_oldest;

                ReadEvents(logQuery);
            }
        }

        //
        // Process ListLog parameter set
        //
        private void ProcessListLog()
        {
            using (EventLogSession eventLogSession = CreateSession())
            {
                foreach (string logPattern in ListLog)
                {
                    bool bMatchFound = false;
                    WildcardPattern wildLogPattern = new(logPattern, WildcardOptions.IgnoreCase);

                    foreach (string logName in eventLogSession.GetLogNames())
                    {
                        if (((!WildcardPattern.ContainsWildcardCharacters(logPattern))
                            && string.Equals(logPattern, logName, StringComparison.OrdinalIgnoreCase))
                            ||
                            (wildLogPattern.IsMatch(logName)))
                        {
                            try
                            {
                                EventLogConfiguration logObj = new(logName, eventLogSession);

                                //
                                // Skip direct channels matching the wildcard unless -Force is present.
                                //
                                if (!Force.IsPresent &&
                                    WildcardPattern.ContainsWildcardCharacters(logPattern) &&
                                        (logObj.LogType == EventLogType.Debug ||
                                        logObj.LogType == EventLogType.Analytical))
                                {
                                    continue;
                                }

                                EventLogInformation logInfoObj = eventLogSession.GetLogInformation(logName, PathType.LogName);

                                PSObject outputObj = new(logObj);

                                outputObj.Properties.Add(new PSNoteProperty("FileSize", logInfoObj.FileSize));
                                outputObj.Properties.Add(new PSNoteProperty("IsLogFull", logInfoObj.IsLogFull));
                                outputObj.Properties.Add(new PSNoteProperty("LastAccessTime", logInfoObj.LastAccessTime));
                                outputObj.Properties.Add(new PSNoteProperty("LastWriteTime", logInfoObj.LastWriteTime));
                                outputObj.Properties.Add(new PSNoteProperty("OldestRecordNumber", logInfoObj.OldestRecordNumber));
                                outputObj.Properties.Add(new PSNoteProperty("RecordCount", logInfoObj.RecordCount));

                                WriteObject(outputObj);
                                bMatchFound = true;
                            }
                            catch (Exception exc)
                            {
                                string msg = string.Format(CultureInfo.InvariantCulture,
                                                        _resourceMgr.GetString("LogInfoUnavailable"),
                                                        logName, exc.Message);
                                Exception outerExc = new(msg, exc);
                                WriteError(new ErrorRecord(outerExc, "LogInfoUnavailable", ErrorCategory.NotSpecified, null));
                                continue;
                            }
                        }
                    }

                    if (!bMatchFound)
                    {
                        string msg = _resourceMgr.GetString("NoMatchingLogsFound");
                        Exception exc = new(string.Format(CultureInfo.InvariantCulture, msg, ComputerName, logPattern));
                        WriteError(new ErrorRecord(exc, "NoMatchingLogsFound", ErrorCategory.ObjectNotFound, null));
                    }
                }
            }
        }

        //
        // Process ListProvider parameter set
        //
        private void ProcessListProvider()
        {
            using (EventLogSession eventLogSession = CreateSession())
            {
                foreach (string provPattern in ListProvider)
                {
                    bool bMatchFound = false;
                    WildcardPattern wildProvPattern = new(provPattern, WildcardOptions.IgnoreCase);

                    foreach (string provName in eventLogSession.GetProviderNames())
                    {
                        if (((!WildcardPattern.ContainsWildcardCharacters(provPattern))
                            && string.Equals(provPattern, provName, StringComparison.OrdinalIgnoreCase))
                            ||
                            (wildProvPattern.IsMatch(provName)))
                        {
                            try
                            {
                                ProviderMetadata provObj = new(provName, eventLogSession, CultureInfo.CurrentCulture);
                                WriteObject(provObj);
                                bMatchFound = true;
                            }
                            catch (System.Diagnostics.Eventing.Reader.EventLogException exc)
                            {
                                string msg = string.Format(CultureInfo.InvariantCulture,
                                                        _resourceMgr.GetString("ProviderMetadataUnavailable"),
                                                        provName, exc.Message);
                                Exception outerExc = new(msg, exc);
                                WriteError(new ErrorRecord(outerExc, "ProviderMetadataUnavailable", ErrorCategory.NotSpecified, null));
                                continue;
                            }
                        }
                    }

                    if (!bMatchFound)
                    {
                        string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("NoMatchingProvidersFound"),
                                                ComputerName, provPattern);
                        Exception exc = new(msg);
                        WriteError(new ErrorRecord(exc, "NoMatchingProvidersFound", ErrorCategory.ObjectNotFound, null));
                    }
                }
            }
        }

        //
        // Process FilterXml parameter set
        //
        private void ProcessFilterXml()
        {
            using (EventLogSession eventLogSession = CreateSession())
            {
                if (!Oldest.IsPresent)
                {
                    //
                    // Do minimal parsing of xmlQuery to determine if any direct channels or ETL files are in it.
                    //
                    XmlElement root = FilterXml.DocumentElement;
                    XmlNodeList queryNodes = root.SelectNodes("//Query//Select");
                    foreach (XmlNode queryNode in queryNodes)
                    {
                        XmlAttributeCollection attribs = queryNode.Attributes;
                        foreach (XmlAttribute attrib in attribs)
                        {
                            if (attrib.Name.Equals("Path", StringComparison.OrdinalIgnoreCase))
                            {
                                string logName = attrib.Value;

                                if (logName.StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
                                {
                                    TerminateForNonEvtxFileWithoutOldest(logName);
                                }

                                ValidateLogName(logName, eventLogSession);
                            }
                        }
                    }
                }

                EventLogQuery logQuery = new(null, PathType.LogName, FilterXml.InnerXml);
                logQuery.Session = eventLogSession;
                logQuery.ReverseDirection = !_oldest;

                ReadEvents(logQuery);
            }
        }

        //
        // Process FileSet parameter set
        //
        private void ProcessFile()
        {
            using (EventLogSession eventLogSession = CreateSession())
            {
                //
                // At this point, _path array contains paths that might have wildcards,
                // environment variables or PS drives. Let's resolve those.
                //
                for (int i = 0; i < Path.Length; i++)
                {
                    StringCollection resolvedPaths = ValidateAndResolveFilePath(Path[i]);
                    foreach (string resolvedPath in resolvedPaths)
                    {
                        _resolvedPaths.Add(resolvedPath);
                        WriteVerbose(string.Format(CultureInfo.InvariantCulture, $"Found file {resolvedPath}"));
                    }
                }

                EventLogQuery logQuery = null;
                if (_resolvedPaths.Count == 0)
                {
                    return;
                }
                else if (_resolvedPaths.Count > 1)
                {
                    string query = BuildStructuredQuery(eventLogSession);
                    logQuery = new EventLogQuery(null, PathType.FilePath, query);
                    logQuery.TolerateQueryErrors = true;
                }
                else
                {
                    logQuery = new EventLogQuery(_resolvedPaths[0], PathType.FilePath, FilterXPath);
                }

                logQuery.Session = eventLogSession;
                logQuery.ReverseDirection = !_oldest;

                ReadEvents(logQuery);
            }
        }

        //
        // Process HashQuerySet parameter set
        //
        private void ProcessHashQuery()
        {
            CheckHashTablesForNullValues();

            using (EventLogSession eventLogSession = CreateSession())
            {
                string query = BuildStructuredQuery(eventLogSession);
                if (query.Length == 0)
                {
                    return;
                }

                EventLogQuery logQuery = new(null, PathType.FilePath, query);
                logQuery.Session = eventLogSession;
                logQuery.TolerateQueryErrors = true;
                logQuery.ReverseDirection = !_oldest;

                ReadEvents(logQuery);
            }
        }

        //
        // CreateSession creates an EventLogSession connected to a target machine or localhost.
        // If _credential argument is PSCredential.Empty, the session will be created for the current context.
        //
        private EventLogSession CreateSession()
        {
            EventLogSession eventLogSession = null;

            if (ComputerName == string.Empty)
            {
                // Set _computerName to "localhost" for future error messages,
                // but do not use it for the connection to avoid RPC overhead.
                ComputerName = "localhost";

                if (Credential == PSCredential.Empty)
                {
                    return new EventLogSession();
                }
            }
            else if (Credential == PSCredential.Empty)
            {
                return new EventLogSession(ComputerName);
            }

            // If we are here, either both computer name and credential were passed initially,
            // or credential only - we will use it with "localhost"

            NetworkCredential netCred = (NetworkCredential)Credential;
            eventLogSession = new EventLogSession(ComputerName,
                                 netCred.Domain,
                                 netCred.UserName,
                                 Credential.Password,
                                 SessionAuthentication.Default
                                 );
            //
            // Force the destruction of cached password
            //
            netCred.Password = string.Empty;

            return eventLogSession;
        }

        //
        // ReadEvents helper.
        //
        private void ReadEvents(EventLogQuery logQuery)
        {
            using (EventLogReader readerObj = new(logQuery))
            {
                long numEvents = 0;
                EventRecord evtObj = null;

                while (true)
                {
                    try
                    {
                        evtObj = readerObj.ReadEvent();
                    }
                    catch (Exception exc)
                    {
                        WriteError(new ErrorRecord(exc, exc.Message, ErrorCategory.NotSpecified, null));
                        continue;
                    }

                    if (evtObj == null)
                    {
                        break;
                    }

                    if (MaxEvents != -1 && numEvents >= MaxEvents)
                    {
                        break;
                    }

                    PSObject outputObj = new(evtObj);

                    string evtMessage = _resourceMgr.GetString("NoEventMessage");
                    try
                    {
                        evtMessage = evtObj.FormatDescription();
                    }
                    catch (Exception exc)
                    {
                        WriteError(new ErrorRecord(exc, exc.Message, ErrorCategory.NotSpecified, null));
                    }

                    outputObj.Properties.Add(new PSNoteProperty("Message", evtMessage));

                    //
                    // Enumerate the object one level to get to event payload
                    //
                    WriteObject(outputObj, true);
                    numEvents++;
                }

                if (numEvents == 0)
                {
                    string msg = _resourceMgr.GetString("NoMatchingEventsFound");
                    Exception exc = new(msg);
                    WriteError(new ErrorRecord(exc, "NoMatchingEventsFound", ErrorCategory.ObjectNotFound, null));
                }
            }
        }

        //
        // BuildStructuredQuery() builds a structured query from cmdlet arguments.
        //
        private string BuildStructuredQuery(EventLogSession eventLogSession)
        {
            StringBuilder result = new();

            switch (ParameterSetName)
            {
                case "ListLogSet":
                    break;

                case "ListProviderSet":
                    break;

                case "GetProviderSet":
                    {
                        result.Append(queryListOpen);
                        uint queryId = 0;

                        foreach (string log in _providersByLogMap.Keys)
                        {
                            string providerFilter = AddProviderPredicatesToFilter(_providersByLogMap[log]);
                            result.AppendFormat(CultureInfo.InvariantCulture, queryTemplate, new object[] { queryId++, log, providerFilter });
                        }

                        result.Append(queryListClose);
                    }

                    break;

                case "GetLogSet":
                    {
                        const int WindowsEventLogAPILimit = 256;
                        if (_logNamesMatchingWildcard.Count > WindowsEventLogAPILimit)
                        {
                            string msg = _resourceMgr.GetString("LogCountLimitExceeded");
                            Exception exc = new(string.Format(CultureInfo.InvariantCulture, msg, _logNamesMatchingWildcard.Count, WindowsEventLogAPILimit));
                            ThrowTerminatingError(new ErrorRecord(exc, "LogCountLimitExceeded", ErrorCategory.LimitsExceeded, null));
                        }

                        result.Append(queryListOpen);
                        uint queryId = 0;
                        foreach (string log in _logNamesMatchingWildcard)
                        {
                            result.AppendFormat(CultureInfo.InvariantCulture, queryTemplate, new object[] { queryId++, log, FilterXPath });
                        }

                        result.Append(queryListClose);
                    }

                    break;

                case "FileSet":
                    {
                        result.Append(queryListOpen);
                        uint queryId = 0;
                        foreach (string filePath in _resolvedPaths)
                        {
                            string properFilePath = filePrefix + filePath;
                            result.AppendFormat(CultureInfo.InvariantCulture, queryTemplate, new object[] { queryId++, properFilePath, FilterXPath });
                        }

                        result.Append(queryListClose);
                    }

                    break;

                case "HashQuerySet":
                    result.Append(BuildStructuredQueryFromHashTable(eventLogSession));
                    break;

                default:
                    WriteDebug(string.Create(CultureInfo.InvariantCulture, $"Invalid parameter set name: {ParameterSetName}"));
                    break;
            }

            WriteVerbose(string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("QueryTrace"), result.ToString()));

            return result.ToString();
        }

        //
        // BuildXPathFromHashTable() build xpath from hashtable
        //
        private string BuildXPathFromHashTable(Hashtable hash)
        {
            StringBuilder xpathString = new(string.Empty);
            bool bDateTimeHandled = false;

            foreach (string key in hash.Keys)
            {
                string added = string.Empty;

                switch (key.ToLowerInvariant())
                {
                    case hashkey_logname_lc:
                    case hashkey_path_lc:
                    case hashkey_providername_lc:
                        break;
                    case hashkey_id_lc:
                        added = HandleEventIdHashValue(hash[key]);
                        break;

                    case hashkey_level_lc:
                        added = HandleLevelHashValue(hash[key]);
                        break;

                    case hashkey_keywords_lc:
                        added = HandleKeywordHashValue(hash[key]);
                        break;

                    case hashkey_starttime_lc:
                        if (bDateTimeHandled)
                        {
                            break;
                        }

                        added = HandleStartTimeHashValue(hash[key], hash);

                        bDateTimeHandled = true;
                        break;

                    case hashkey_endtime_lc:
                        if (bDateTimeHandled)
                        {
                            break;
                        }

                        added = HandleEndTimeHashValue(hash[key], hash);

                        bDateTimeHandled = true;
                        break;

                    case hashkey_data_lc:
                        added = HandleDataHashValue(hash[key]);
                        break;

                    case hashkey_userid_lc:
                        added = HandleContextHashValue(hash[key]);
                        break;

                    case hashkey_supress_lc:
                        break;
                    default:
                        {
                            //
                            // None of the recognized values: this must be a named event data field
                            //
                            // Fix Issue #2327
                            added = HandleNamedDataHashValue(key, hash[key]);
                        }

                        break;
                }

                if (added.Length > 0)
                {
                    if (xpathString.Length != 0)
                    {
                        xpathString.Append(" and ");
                    }

                    xpathString.Append(added);
                }
            }

            return xpathString.ToString();
        }

        //
        // BuildStructuredQueryFromHashTable() helper.
        // Builds a structured query from the hashtable (Selector) argument.
        //
        private string BuildStructuredQueryFromHashTable(EventLogSession eventLogSession)
        {
            StringBuilder result = new(string.Empty);

            result.Append(queryListOpen);

            uint queryId = 0;

            foreach (Hashtable hash in FilterHashtable)
            {
                string xpathString = string.Empty;
                string xpathStringSuppress = string.Empty;

                CheckHashTableForQueryPathPresence(hash);

                //
                // Local queriedLogsQueryMap will hold names of logs or files to be queried
                // mapped to the actual query strings being built up.
                //
                Dictionary<string, string> queriedLogsQueryMap = new();

                //
                // queriedLogsQueryMapSuppress is the same as queriedLogsQueryMap but for <Suppress>
                //
                Dictionary<string, string> queriedLogsQueryMapSuppress = new();

                //
                // Process log, _path, or provider parameters first
                // to create initial partially-filled query templates.
                // Error out for direct channels unless -oldest is present.
                //
                // Order is important! Process "providername" key after "logname" and "file".
                //
                if (hash.ContainsKey(hashkey_logname_lc))
                {
                    List<string> logPatterns = new();
                    if (hash[hashkey_logname_lc] is Array)
                    {
                        foreach (object elt in (Array)hash[hashkey_logname_lc])
                        {
                            logPatterns.Add(elt.ToString());
                        }
                    }
                    else
                    {
                        logPatterns.Add(hash[hashkey_logname_lc].ToString());
                    }

                    FindLogNamesMatchingWildcards(eventLogSession, logPatterns);

                    foreach (string logName in _logNamesMatchingWildcard)
                    {
                        queriedLogsQueryMap.Add(logName.ToLowerInvariant(),
                                                string.Format(CultureInfo.InvariantCulture, queryOpenerTemplate, queryId++, logName));
                        queriedLogsQueryMapSuppress.Add(logName.ToLowerInvariant(),
                                                        string.Format(CultureInfo.InvariantCulture, suppressOpener, queryId++, logName));
                    }
                }

                if (hash.ContainsKey(hashkey_path_lc))
                {
                    if (hash[hashkey_path_lc] is Array)
                    {
                        foreach (object elt in (Array)hash[hashkey_path_lc])
                        {
                            StringCollection resolvedPaths = ValidateAndResolveFilePath(elt.ToString());
                            foreach (string resolvedPath in resolvedPaths)
                            {
                                queriedLogsQueryMap.Add(filePrefix + resolvedPath.ToLowerInvariant(),
                                                        string.Format(CultureInfo.InvariantCulture, queryOpenerTemplate, queryId++, filePrefix + resolvedPath));
                                queriedLogsQueryMapSuppress.Add(filePrefix + resolvedPath.ToLowerInvariant(),
                                                                string.Format(CultureInfo.InvariantCulture, suppressOpener, queryId++, filePrefix + resolvedPath));
                            }
                        }
                    }
                    else
                    {
                        StringCollection resolvedPaths = ValidateAndResolveFilePath(hash[hashkey_path_lc].ToString());
                        foreach (string resolvedPath in resolvedPaths)
                        {
                            queriedLogsQueryMap.Add(filePrefix + resolvedPath.ToLowerInvariant(),
                                                    string.Format(CultureInfo.InvariantCulture, queryOpenerTemplate, queryId++, filePrefix + resolvedPath));
                            queriedLogsQueryMapSuppress.Add(filePrefix + resolvedPath.ToLowerInvariant(),
                                                            string.Format(CultureInfo.InvariantCulture, suppressOpener, queryId++, filePrefix + resolvedPath));
                        }
                    }
                }

                if (hash.ContainsKey(hashkey_providername_lc))
                {
                    List<string> provPatterns = new();
                    if (hash[hashkey_providername_lc] is Array)
                    {
                        foreach (object elt in (Array)hash[hashkey_providername_lc])
                        {
                            provPatterns.Add(elt.ToString());
                        }
                    }
                    else
                    {
                        provPatterns.Add(hash[hashkey_providername_lc].ToString());
                    }

                    FindProvidersByLogForWildcardPatterns(eventLogSession, provPatterns);

                    //
                    // If "providername" key is used alone, we will construct a query across all of the providers' logs.
                    // Otherwise, we will use the provider names to add predicates to "logname" and "path" queries.
                    //
                    if (!hash.ContainsKey(hashkey_path_lc) && !hash.ContainsKey(hashkey_logname_lc))
                    {
                        foreach (string keyLogName in _providersByLogMap.Keys)
                        {
                            string providersPredicate = BuildProvidersPredicate(_providersByLogMap[keyLogName]);
                            string query = string.Format(CultureInfo.InvariantCulture, queryOpenerTemplate, queryId++, keyLogName);
                            queriedLogsQueryMap.Add(keyLogName.ToLowerInvariant(),
                                                     query + "[" + providersPredicate);
                            queriedLogsQueryMapSuppress.Add(keyLogName.ToLowerInvariant(),
                                                            string.Format(CultureInfo.InvariantCulture, suppressOpener, queryId++, keyLogName.ToLowerInvariant()));
                        }
                    }
                    else
                    {
                        List<string> keysList = new(queriedLogsQueryMap.Keys);
                        bool bRemovedIrrelevantLogs = false;
                        foreach (string queriedLog in keysList)
                        {
                            if (queriedLog.StartsWith(filePrefix, StringComparison.Ordinal))
                            {
                                queriedLogsQueryMap[queriedLog] += "[" + BuildAllProvidersPredicate();
                            }
                            else
                            {
                                if (_providersByLogMap.ContainsKey(queriedLog))
                                {
                                    string providersPredicate = BuildProvidersPredicate(_providersByLogMap[queriedLog]);
                                    queriedLogsQueryMap[queriedLog] += "[" + providersPredicate;
                                }
                                else
                                {
                                    WriteVerbose(string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("SpecifiedProvidersDontWriteToLog"), queriedLog));
                                    queriedLogsQueryMap.Remove(queriedLog);
                                    queriedLogsQueryMapSuppress.Remove(queriedLog);
                                    bRemovedIrrelevantLogs = true;
                                }
                            }
                        }
                        //
                        // Write an error if we have removed all the logs as irrelevant
                        //
                        if (bRemovedIrrelevantLogs && (queriedLogsQueryMap.Count == 0))
                        {
                            string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("LogsAndProvidersDontOverlap"));
                            Exception exc = new(msg);
                            WriteError(new ErrorRecord(exc, "LogsAndProvidersDontOverlap", ErrorCategory.InvalidArgument, null));
                            continue;
                        }
                    }
                }

                //
                // If none of the logs/paths/providers were valid, queriedLogsQueryMap is empty.
                // Simply continue to the next hashtable since all the errors have been written already.
                //
                if (queriedLogsQueryMap.Count == 0)
                {
                    continue;
                }

                //
                // At this point queriedLogsQueryMap contains all the query openings: missing the actual XPaths
                // Let's build xpathString to attach to each query opening.
                //
                xpathString = BuildXPathFromHashTable(hash);

                //
                // Build xpath for <Suppress>
                //
                Hashtable suppresshash = hash[hashkey_supress_lc] as Hashtable;
                if (suppresshash != null)
                {
                    xpathStringSuppress = BuildXPathFromHashTable(suppresshash);
                }

                //
                // Complete each query with the XPath.
                // Handle the case where the query opener already has provider predicate(s).
                // Add the queries from queriedLogsQueryMap into the resulting string.
                // Add <Suppress> from queriedLogsQueryMapSuppress into the resulting string.
                //
                foreach (string keyLogName in queriedLogsQueryMap.Keys)
                {
                    // For every Log a separate query is
                    string query = queriedLogsQueryMap[keyLogName];
                    result.Append(query);

                    if (query.EndsWith('*'))
                    {
                        //
                        // No provider predicate: just add the XPath string
                        //
                        if (xpathString.Length != 0)
                        {
                            result.Append(propOpen).Append(xpathString).Append(propClose);
                        }
                    }
                    else
                    {
                        //
                        // Add xpathString to provider predicates.
                        //
                        if (xpathString.Length != 0)
                        {
                            result.Append(" and ").Append(xpathString);
                        }

                        result.Append(propClose);
                    }

                    result.Append(SelectCloser);

                    if (xpathStringSuppress.Length != 0)
                    {
                        // Add <Suppress>*xpathStringSuppress</Suppress> into query
                        string suppress = queriedLogsQueryMapSuppress[keyLogName];
                        result.Append(suppress);
                        result.Append(propOpen).Append(xpathStringSuppress).Append(propClose);
                        result.Append(suppressCloser);
                    }

                    result.Append(queryCloser);
                }
            }

            result.Append(queryListClose);

            return result.ToString();
        }

        //
        // HandleEventIdHashValue helper for hashtable structured query builder.
        // Constructs and returns EventId XPath portion as a string.
        //
        private static string HandleEventIdHashValue(object value)
        {
            StringBuilder ret = new();
            Array idsArray = value as Array;
            if (idsArray != null)
            {
                ret.Append('(');
                for (int i = 0; i < idsArray.Length; i++)
                {
                    ret.Append(SystemEventIDTemplate).Append(idsArray.GetValue(i).ToString()).Append(')');
                    if (i < (idsArray.Length - 1))
                    {
                        ret.Append(" or ");
                    }
                }

                ret.Append(')');
            }
            else
            {
                ret.Append(SystemEventIDTemplate).Append(value).Append(')');
            }

            return ret.ToString();
        }

        //
        // HandleLevelHashValue helper for hashtable structured query builder.
        // Constructs and returns Level XPath portion as a string.
        //
        private static string HandleLevelHashValue(object value)
        {
            StringBuilder ret = new();
            Array levelsArray = value as Array;
            if (levelsArray != null)
            {
                ret.Append('(');
                for (int i = 0; i < levelsArray.Length; i++)
                {
                    ret.Append(SystemLevelTemplate).Append(levelsArray.GetValue(i).ToString()).Append(')');
                    if (i < (levelsArray.Length - 1))
                    {
                        ret.Append(" or ");
                    }
                }

                ret.Append(')');
            }
            else
            {
                ret.Append(SystemLevelTemplate).Append(value).Append(')');
            }

            return ret.ToString();
        }

        //
        // HandleKeywordHashValue helper for hashtable structured query builder.
        // Constructs and returns Keyword XPath portion as a string.
        //
        private string HandleKeywordHashValue(object value)
        {
            long keywordsMask = 0;
            long keywordLong = 0;

            Array keywordArray = value as Array;
            if (keywordArray != null)
            {
                foreach (object keyword in keywordArray)
                {
                    if (KeywordStringToInt64(keyword.ToString(), ref keywordLong))
                    {
                        keywordsMask |= keywordLong;
                    }
                }
            }
            else
            {
                if (!KeywordStringToInt64(value.ToString(), ref keywordLong))
                {
                    return string.Empty;
                }

                keywordsMask |= keywordLong;
            }

            return string.Format(CultureInfo.InvariantCulture, SystemKeywordsTemplate, keywordsMask);
        }

        //
        // HandleContextHashValue helper for hashtable structured query builder.
        // Constructs and returns UserID XPath portion as a string.
        // Handles both SIDs and domain account names.
        // Writes an error and returns an empty string if the SID or account names are not valid.
        //
        private string HandleContextHashValue(object value)
        {
            SecurityIdentifier sidCandidate = null;
            try
            {
                sidCandidate = new SecurityIdentifier(value.ToString());
            }
            catch (ArgumentException)
            {
                WriteDebug(string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("InvalidSIDFormat"), value));
            }

            if (sidCandidate == null)
            {
                try
                {
                    NTAccount acct = new(value.ToString());
                    sidCandidate = (SecurityIdentifier)acct.Translate(typeof(SecurityIdentifier));
                }
                catch (ArgumentException exc)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("InvalidContext"), value.ToString());
                    Exception outerExc = new(msg, exc);
                    WriteError(new ErrorRecord(outerExc, "InvalidContext", ErrorCategory.InvalidArgument, null));
                    return string.Empty;
                }
            }

            return string.Format(CultureInfo.InvariantCulture, SystemSecurityTemplate, sidCandidate.ToString());
        }

        //
        // HandleStartTimeHashValue helper for hashtable structured query builder.
        // Constructs and returns TimeCreated XPath portion as a string.
        // NOTE that it also handles the hashtable "endtime" value (if supplied).
        //
        private string HandleStartTimeHashValue(object value, Hashtable hash)
        {
            StringBuilder ret = new();
            DateTime startTime = new();
            if (!StringToDateTime(value.ToString(), ref startTime))
            {
                return string.Empty;
            }

            startTime = startTime.ToUniversalTime();
            string startTimeFormatted = startTime.ToString("s", CultureInfo.InvariantCulture) + "." + startTime.Millisecond.ToString("d3", CultureInfo.InvariantCulture) + "Z";

            if (hash.ContainsKey(hashkey_endtime_lc))
            {
                DateTime endTime = new();
                if (!StringToDateTime(hash[hashkey_endtime_lc].ToString(), ref endTime))
                {
                    return string.Empty;
                }

                endTime = endTime.ToUniversalTime();
                string endTimeFormatted = endTime.ToString("s", CultureInfo.InvariantCulture) + "." + endTime.Millisecond.ToString("d3", CultureInfo.InvariantCulture) + "Z";

                ret.AppendFormat(CultureInfo.InvariantCulture,
                                 SystemTimePeriodTemplate,
                                 startTimeFormatted,
                                 endTimeFormatted);
            }
            else
            {
                ret.AppendFormat(CultureInfo.InvariantCulture,
                                 SystemTimeStartTemplate,
                                 startTimeFormatted);
            }

            return ret.ToString();
        }

        //
        // HandleEndTimeHashValue helper for hashtable structured query builder.
        // Constructs and returns TimeCreated XPath portion as a string.
        // NOTE that it also handles the hashtable "starttime" value (if supplied).
        //
        private string HandleEndTimeHashValue(object value, Hashtable hash)
        {
            StringBuilder ret = new();
            DateTime endTime = new();
            if (!StringToDateTime(value.ToString(), ref endTime))
            {
                return string.Empty;
            }

            endTime = endTime.ToUniversalTime();
            string endTimeFormatted = endTime.ToString("s", CultureInfo.InvariantCulture) + "."
                                                       + endTime.Millisecond.ToString("d3", CultureInfo.InvariantCulture) + "Z";

            if (hash.ContainsKey(hashkey_starttime_lc))
            {
                DateTime startTime = new();
                if (!StringToDateTime(hash[hashkey_starttime_lc].ToString(), ref startTime))
                {
                    return string.Empty;
                }

                startTime = startTime.ToUniversalTime();
                string startTimeFormatted = startTime.ToString("s", CultureInfo.InvariantCulture) + "."
                                                               + startTime.Millisecond.ToString("d3", CultureInfo.InvariantCulture) + "Z";

                ret.AppendFormat(CultureInfo.InvariantCulture,
                                 SystemTimePeriodTemplate,
                                 startTimeFormatted,
                                 endTimeFormatted);
            }
            else
            {
                ret.AppendFormat(CultureInfo.InvariantCulture,
                                 SystemTimeEndTemplate,
                                 endTimeFormatted);
            }

            return ret.ToString();
        }

        //
        // HandleDataHashValue helper for hashtable structured query builder.
        // Constructs and returns EventData/Data XPath portion as a string.
        //
        private static string HandleDataHashValue(object value)
        {
            StringBuilder ret = new();
            Array dataArray = value as Array;
            if (dataArray != null)
            {
                ret.Append('(');
                for (int i = 0; i < dataArray.Length; i++)
                {
                    ret.AppendFormat(CultureInfo.InvariantCulture, DataTemplate, dataArray.GetValue(i).ToString());
                    if (i < (dataArray.Length - 1))
                    {
                        ret.Append(" or ");
                    }
                }

                ret.Append(')');
            }
            else
            {
                ret.AppendFormat(CultureInfo.InvariantCulture, DataTemplate, value);
            }

            return ret.ToString();
        }

        //
        // HandleNamedDataHashValue helper for hashtable structured query builder.
        // Constructs and returns named event data field XPath portion as a string.
        // Fix Issue #2327
        //
        private static string HandleNamedDataHashValue(string key, object value)
        {
            StringBuilder ret = new();
            Array dataArray = value as Array;
            if (dataArray != null)
            {
                ret.Append('(');
                for (int i = 0; i < dataArray.Length; i++)
                {
                    ret.AppendFormat(CultureInfo.InvariantCulture,
                                         NamedDataTemplate,
                                         key, dataArray.GetValue(i).ToString());
                    if (i < (dataArray.Length - 1))
                    {
                        ret.Append(" or ");
                    }
                }

                ret.Append(')');
            }
            else
            {
                ret.AppendFormat(CultureInfo.InvariantCulture,
                                         NamedDataTemplate,
                                         key, value);
            }

            return ret.ToString();
        }

        //
        // Helper checking whether at least one of log, _path, provider is specified.
        // It will ThrowTerminatingError in case none of those keys are present.
        //
        private void CheckHashTableForQueryPathPresence(Hashtable hash)
        {
            bool isLogHash = (hash.ContainsKey(hashkey_logname_lc));
            bool isPathHash = (hash.ContainsKey(hashkey_path_lc));
            bool isProviderHash = (hash.ContainsKey(hashkey_providername_lc));

            if (!isLogHash && !isProviderHash && !isPathHash)
            {
                string msg = _resourceMgr.GetString("LogProviderOrPathNeeded");
                Exception exc = new(msg);
                ThrowTerminatingError(new ErrorRecord(exc, "LogProviderOrPathNeeded", ErrorCategory.InvalidArgument, null));
            }
        }

        //
        // TerminateForNonEvtxFileWithoutOldest terminates for .evt and .etl files unless -Oldest is specified.
        //
        private void TerminateForNonEvtxFileWithoutOldest(string fileName)
        {
            if (!Oldest.IsPresent)
            {
                if (System.IO.Path.GetExtension(fileName).Equals(".etl", StringComparison.OrdinalIgnoreCase) ||
                    System.IO.Path.GetExtension(fileName).Equals(".evt", StringComparison.OrdinalIgnoreCase))
                {
                    string msg = _resourceMgr.GetString("SpecifyOldestForEtlEvt");
                    Exception exc = new(string.Format(CultureInfo.InvariantCulture, msg, fileName));
                    ThrowTerminatingError(new ErrorRecord(exc, "SpecifyOldestForEtlEvt", ErrorCategory.InvalidArgument, fileName));
                }
            }
        }

        //
        // ValidateLogName writes an error if logName is not a valid log.
        // It also terminates for direct ETW channels unless -Oldest is specified.
        //
        private bool ValidateLogName(string logName, EventLogSession eventLogSession)
        {
            EventLogConfiguration logObj;
            try
            {
                logObj = new EventLogConfiguration(logName, eventLogSession);
            }
            catch (EventLogNotFoundException)
            {
                string msg = _resourceMgr.GetString("NoMatchingLogsFound");
                Exception exc = new(string.Format(CultureInfo.InvariantCulture, msg, ComputerName, logName));
                WriteError(new ErrorRecord(exc, "NoMatchingLogsFound", ErrorCategory.ObjectNotFound, logName));
                return false;
            }
            catch (Exception exc)
            {
                string msg = string.Format(CultureInfo.InvariantCulture,
                                         _resourceMgr.GetString("LogInfoUnavailable"),
                                         logName, exc.Message);
                Exception outerExc = new(msg, exc);
                WriteError(new ErrorRecord(outerExc, "LogInfoUnavailable", ErrorCategory.NotSpecified, null));
                return false;
            }

            if (!Oldest.IsPresent)
            {
                if (logObj.LogType == EventLogType.Debug || logObj.LogType == EventLogType.Analytical)
                {
                    string msg = _resourceMgr.GetString("SpecifyOldestForLog");
                    Exception exc = new(string.Format(CultureInfo.InvariantCulture, msg, logName));
                    ThrowTerminatingError(new ErrorRecord(exc, "SpecifyOldestForLog", ErrorCategory.InvalidArgument, logName));
                }
            }

            return true;
        }

        //
        // KeywordStringToInt64 helper converts a string to Int64.
        // Returns true and keyLong ref if successful.
        // Writes an error and returns false if keyString cannot be converted.
        //
        private bool KeywordStringToInt64(string keyString, ref long keyLong)
        {
            try
            {
                keyLong = Convert.ToInt64(keyString, CultureInfo.InvariantCulture);
            }
            catch (Exception exc)
            {
                string msg = _resourceMgr.GetString("KeywordLongExpected");
                Exception outerExc = new(string.Format(CultureInfo.InvariantCulture, msg, keyString), exc);
                WriteError(new ErrorRecord(outerExc, "KeywordLongExpected", ErrorCategory.InvalidArgument, null));
                return false;
            }

            return true;
        }

        //
        // StringToDateTime helper converts a string to DateTime object.
        // Returns true and DateTime ref if successful.
        // Writes an error and returns false if dtString cannot be converted.
        //
        private bool StringToDateTime(string dtString, ref DateTime dt)
        {
            try
            {
                dt = DateTime.Parse(dtString, CultureInfo.CurrentCulture);
            }
            catch (FormatException exc)
            {
                string msg = _resourceMgr.GetString("DateTimeExpected");
                Exception outerExc = new(string.Format(CultureInfo.InvariantCulture, msg, dtString), exc);
                WriteError(new ErrorRecord(outerExc, "DateTimeExpected", ErrorCategory.InvalidArgument, null));
                return false;
            }

            return true;
        }

        //
        // ValidateAndResolveFilePath helper.
        // Returns a string collection of resolved file paths.
        // Writes non-terminating errors for invalid paths
        // and returns an empty collection.
        //
        private StringCollection ValidateAndResolveFilePath(string path)
        {
            StringCollection retColl = new();

            Collection<PathInfo> resolvedPathSubset = null;
            try
            {
                resolvedPathSubset = SessionState.Path.GetResolvedPSPathFromPSPath(path);
            }
            catch (PSNotSupportedException notSupported)
            {
                WriteError(new ErrorRecord(notSupported, string.Empty, ErrorCategory.ObjectNotFound, path));
                return retColl;
            }
            catch (System.Management.Automation.DriveNotFoundException driveNotFound)
            {
                WriteError(new ErrorRecord(driveNotFound, string.Empty, ErrorCategory.ObjectNotFound, path));
                return retColl;
            }
            catch (ProviderNotFoundException providerNotFound)
            {
                WriteError(new ErrorRecord(providerNotFound, string.Empty, ErrorCategory.ObjectNotFound, path));
                return retColl;
            }
            catch (ItemNotFoundException pathNotFound)
            {
                WriteError(new ErrorRecord(pathNotFound, string.Empty, ErrorCategory.ObjectNotFound, path));
                return retColl;
            }
            catch (Exception exc)
            {
                WriteError(new ErrorRecord(exc, string.Empty, ErrorCategory.ObjectNotFound, path));
                return retColl;
            }

            foreach (PathInfo pi in resolvedPathSubset)
            {
                //
                // Check the provider: only FileSystem provider paths are acceptable.
                //
                if (pi.Provider.Name != "FileSystem")
                {
                    string msg = _resourceMgr.GetString("NotAFileSystemPath");
                    Exception exc = new(string.Format(CultureInfo.InvariantCulture, msg, path));
                    WriteError(new ErrorRecord(exc, "NotAFileSystemPath", ErrorCategory.InvalidArgument, path));
                    continue;
                }

                //
                // Check the extension: only .evt, .evtx, and .etl files are allowed.
                // If the file was specified without wildcards, display an error.
                // Otherwise, skip silently.
                //
                if (!System.IO.Path.GetExtension(pi.Path).Equals(".evt", StringComparison.OrdinalIgnoreCase) &&
                    !System.IO.Path.GetExtension(pi.Path).Equals(".evtx", StringComparison.OrdinalIgnoreCase) &&
                    !System.IO.Path.GetExtension(pi.Path).Equals(".etl", StringComparison.OrdinalIgnoreCase))
                {
                    if (!WildcardPattern.ContainsWildcardCharacters(path))
                    {
                        string msg = _resourceMgr.GetString("NotALogFile");
                        Exception exc = new(string.Format(CultureInfo.InvariantCulture, msg, pi.ProviderPath));
                        WriteError(new ErrorRecord(exc, "NotALogFile", ErrorCategory.InvalidArgument, path));
                    }

                    continue;
                }

                TerminateForNonEvtxFileWithoutOldest(pi.ProviderPath);

                retColl.Add(pi.ProviderPath.ToLowerInvariant());
            }

            return retColl;
        }

        //
        // CheckHashTablesForNullValues() checks all _selector values
        // and writes a terminating error when it encounters a null
        // as a single value or as part of an array.
        //
        private void CheckHashTablesForNullValues()
        {
            foreach (Hashtable hash in FilterHashtable)
            {
                foreach (string key in hash.Keys)
                {
                    object value = hash[key];
                    if (value == null)
                    {
                        string msg = _resourceMgr.GetString("NullNotAllowedInHashtable");
                        Exception exc = new(string.Format(CultureInfo.InvariantCulture, msg, key));
                        ThrowTerminatingError(new ErrorRecord(exc, "NullNotAllowedInHashtable", ErrorCategory.InvalidArgument, key));
                    }
                    else
                    {
                        Array eltArray = value as Array;
                        if (eltArray != null)
                        {
                            foreach (object elt in eltArray)
                            {
                                if (elt == null)
                                {
                                    string msg = _resourceMgr.GetString("NullNotAllowedInHashtable");
                                    Exception exc = new(string.Format(CultureInfo.InvariantCulture, msg, key));
                                    ThrowTerminatingError(new ErrorRecord(exc, "NullNotAllowedInHashtable", ErrorCategory.InvalidArgument, key));
                                }
                            }
                        }
                    }
                }
            }
        }

        //
        // AddProviderPredicatesToFilter() builds an XPath query
        // by adding provider predicates to _filter.
        // Note that this is by no means an XPath expression parser
        // and will may produce garbage if the _filterXPath expression provided by the user is invalid.
        // However, we are relying on the EventLog XPath parser to reject the garbage later on.
        //
        private string AddProviderPredicatesToFilter(StringCollection providers)
        {
            if (providers.Count == 0)
            {
                return FilterXPath;
            }

            string ret = FilterXPath;
            string predicate = BuildProvidersPredicate(providers);

            if (FilterXPath.Equals("*", StringComparison.OrdinalIgnoreCase))
            {
                ret += "[" + predicate + "]";
            }
            else
            {
                //
                // Extend the XPath provided in the _filter
                //
                int lastPredClose = FilterXPath.LastIndexOf(']');
                if (lastPredClose == -1)
                {
                    ret += "[" + predicate + "]";
                }
                else
                {
                    ret = ret.Insert(lastPredClose, " and " + predicate);
                }
            }

            return ret;
        }

        //
        // BuildProvidersPredicate() builds a predicate expression like:
        // "System/Provider[@Name='a' or @Name='b']"
        // for all provider names specified in the "providers" argument.
        //
        private static string BuildProvidersPredicate(StringCollection providers)
        {
            if (providers.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder predicate = new("System/Provider[");
            for (int i = 0; i < providers.Count; i++)
            {
                predicate.Append("@Name='").Append(providers[i]).Append('\'');
                if (i < (providers.Count - 1))
                {
                    predicate.Append(" or ");
                }
            }

            predicate.Append(']');

            return predicate.ToString();
        }

        //
        // BuildAllProvidersPredicate() builds a predicate expression like:
        // "System/Provider[@Name='a' or @Name='b']"
        // for all unique provider names specified in _providersByLogMap.
        // Eliminates duplicates, too, since the same provider can
        // be writing to several different logs.
        //
        private string BuildAllProvidersPredicate()
        {
            if (_providersByLogMap.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder predicate = new("System/Provider[");

            List<string> uniqueProviderNames = new();

            foreach (string logKey in _providersByLogMap.Keys)
            {
                for (int i = 0; i < _providersByLogMap[logKey].Count; i++)
                {
                    string lowerCaseProviderName = _providersByLogMap[logKey][i].ToLowerInvariant();
                    if (!uniqueProviderNames.Contains(lowerCaseProviderName))
                    {
                        uniqueProviderNames.Add(lowerCaseProviderName);
                    }
                }
            }

            for (int i = 0; i < uniqueProviderNames.Count; i++)
            {
                predicate.Append("@Name='").Append(uniqueProviderNames[i]).Append('\'');
                if (i < uniqueProviderNames.Count - 1)
                {
                    predicate.Append(" or ");
                }
            }

            predicate.Append(']');

            return predicate.ToString();
        }

        //
        // AddLogsForProviderToInternalMap helper.
        // Retrieves log names to which _providerName writes.
        // NOTE: there are many misconfigured providers in the system.
        // We therefore catch EventLogException exceptions and write them out as non-terminating errors.
        // The results are added to _providersByLogMap dictionary.
        //
        private void AddLogsForProviderToInternalMap(EventLogSession eventLogSession, string providerName)
        {
            try
            {
                ProviderMetadata providerMetadata = new(providerName, eventLogSession, CultureInfo.CurrentCulture);

                System.Collections.IEnumerable logLinks = providerMetadata.LogLinks;

                foreach (EventLogLink logLink in logLinks)
                {
                    if (!_providersByLogMap.ContainsKey(logLink.LogName.ToLowerInvariant()))
                    {
                        //
                        // Skip direct ETW channels unless -force is present.
                        // Error out for direct channels unless -oldest is present.
                        //
                        EventLogConfiguration logObj = new(logLink.LogName, eventLogSession);
                        if (logObj.LogType == EventLogType.Debug || logObj.LogType == EventLogType.Analytical)
                        {
                            if (!Force.IsPresent)
                            {
                                continue;
                            }

                            ValidateLogName(logLink.LogName, eventLogSession);
                        }

                        WriteVerbose(string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("ProviderLogLink"), providerName, logLink.LogName));

                        StringCollection provColl = new();
                        provColl.Add(providerName.ToLowerInvariant());

                        _providersByLogMap.Add(logLink.LogName.ToLowerInvariant(), provColl);
                    }
                    else
                    {
                        //
                        // Log is there: add provider, if needed
                        //
                        StringCollection coll = _providersByLogMap[logLink.LogName.ToLowerInvariant()];

                        if (!coll.Contains(providerName.ToLowerInvariant()))
                        {
                            WriteVerbose(string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("ProviderLogLink"), providerName, logLink.LogName));

                            coll.Add(providerName.ToLowerInvariant());
                        }
                    }
                }
            }
            catch (System.Diagnostics.Eventing.Reader.EventLogException exc)
            {
                string msg = string.Format(CultureInfo.InvariantCulture,
                                           _resourceMgr.GetString("ProviderMetadataUnavailable"),
                                           providerName, exc.Message);
                Exception outerExc = new(msg, exc);
                WriteError(new ErrorRecord(outerExc, "ProviderMetadataUnavailable", ErrorCategory.NotSpecified, null));
                return;
            }
        }

        //
        // FindLogNamesMatchingWildcards helper.
        // Finds all logs whose names match wildcard patterns in the 'logPatterns' argument.
        // For each non-matched pattern, a non-terminating error is written.
        // The results are added to _logNamesMatchingWildcard array.
        //
        private void FindLogNamesMatchingWildcards(EventLogSession eventLogSession, IEnumerable<string> logPatterns)
        {
            if (_logNamesMatchingWildcard == null)
            {
                _logNamesMatchingWildcard = new StringCollection();
            }
            else
            {
                _logNamesMatchingWildcard.Clear();
            }

            foreach (string logPattern in logPatterns)
            {
                bool bMatched = false;
                WildcardPattern wildLogPattern = new(logPattern, WildcardOptions.IgnoreCase);

                foreach (string actualLogName in eventLogSession.GetLogNames())
                {
                    if (((!WildcardPattern.ContainsWildcardCharacters(logPattern))
                        && (logPattern.Equals(actualLogName, StringComparison.OrdinalIgnoreCase)))
                        ||
                        (wildLogPattern.IsMatch(actualLogName)))
                    {
                        //
                        // Skip direct ETW channels matching wildcards unless -force is present.
                        // Error out for direct channels unless -oldest is present.
                        //
                        EventLogConfiguration logObj;
                        try
                        {
                            logObj = new EventLogConfiguration(actualLogName, eventLogSession);
                        }
                        catch (Exception exc)
                        {
                            string msg = string.Format(CultureInfo.InvariantCulture,
                                                     _resourceMgr.GetString("LogInfoUnavailable"),
                                                     actualLogName, exc.Message);
                            Exception outerExc = new(msg, exc);
                            WriteError(new ErrorRecord(outerExc, "LogInfoUnavailable", ErrorCategory.NotSpecified, null));
                            continue;
                        }

                        if (logObj.LogType == EventLogType.Debug || logObj.LogType == EventLogType.Analytical)
                        {
                            if (WildcardPattern.ContainsWildcardCharacters(logPattern) && !Force.IsPresent)
                            {
                                continue;
                            }

                            ValidateLogName(actualLogName, eventLogSession);
                        }

                        if (!_logNamesMatchingWildcard.Contains(actualLogName.ToLowerInvariant()))
                        {
                            _logNamesMatchingWildcard.Add(actualLogName.ToLowerInvariant());
                        }

                        bMatched = true;
                    }
                }

                if (!bMatched)
                {
                    string msg = _resourceMgr.GetString("NoMatchingLogsFound");
                    Exception exc = new(string.Format(CultureInfo.InvariantCulture, msg, ComputerName, logPattern));
                    WriteError(new ErrorRecord(exc, "NoMatchingLogsFound", ErrorCategory.ObjectNotFound, logPattern));
                }
            }
        }

        //
        // FindProvidersByLogForWildcardPatterns helper.
        // Finds all providers whose names match wildcard patterns in 'providerPatterns' argument.
        // For each non-matched pattern, a non-terminating error is written.
        // The results are added to _providersByLogMap dictionary (keyed by log names to which these providers write).
        //
        private void FindProvidersByLogForWildcardPatterns(EventLogSession eventLogSession, IEnumerable<string> providerPatterns)
        {
            _providersByLogMap.Clear();

            foreach (string provPattern in providerPatterns)
            {
                bool bMatched = false;
                WildcardPattern wildProvPattern = new(provPattern, WildcardOptions.IgnoreCase);

                foreach (string provName in eventLogSession.GetProviderNames())
                {
                    if (((!WildcardPattern.ContainsWildcardCharacters(provPattern))
                      && (provPattern.Equals(provName, StringComparison.OrdinalIgnoreCase)))
                      ||
                      (wildProvPattern.IsMatch(provName)))
                    {
                        WriteVerbose(string.Format(CultureInfo.InvariantCulture, $"Found matching provider: {provName}"));
                        AddLogsForProviderToInternalMap(eventLogSession, provName);
                        bMatched = true;
                    }
                }

                if (!bMatched)
                {
                    string msg = _resourceMgr.GetString("NoMatchingProvidersFound");
                    Exception exc = new(string.Format(CultureInfo.InvariantCulture, msg, ComputerName, provPattern));
                    WriteError(new ErrorRecord(exc, "NoMatchingProvidersFound", ErrorCategory.ObjectNotFound, provPattern));
                }
            }
        }
    }
}
