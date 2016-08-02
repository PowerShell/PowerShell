//
// Copyright (c) 2007 Microsoft Corporation. All rights reserved.
// 

using System;
using System.Xml;
using System.Net;
using System.Management.Automation;
using System.Reflection;
using System.Globalization;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Security.Principal;
using System.Resources;
using System.Diagnostics.CodeAnalysis;

[assembly: CLSCompliant(false)]

namespace Microsoft.PowerShell.Commands
{
    /// 
    /// Class that implements the Get-WinEvent cmdlet.
    /// 
    [Cmdlet(VerbsCommon.Get, "WinEvent", DefaultParameterSetName = "GetLogSet", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=138336")]
    public sealed class GetWinEventCommand : PSCmdlet
    {
        /// <summary>
        /// ListLog parameter
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

        public string[] ListLog
        {
            get { return _listLog; }
            set { _listLog = value; }
        }
        private string[] _listLog = { "*" };

        /// <summary>
        /// GetLog parameter
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
        public string[] LogName
        {
            get { return _logName; }
            set { _logName = value; }
        }
        private string[] _logName = { "*" };


        /// <summary>
        /// ListProvider parameter
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

        public string[] ListProvider
        {
            get { return _listProvider; }
            set { _listProvider = value; }
        }
        private string[] _listProvider = { "*" };


        /// <summary>
        /// ProviderName parameter
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

        public string[] ProviderName
        {
            get { return _providerName; }
            set { _providerName = value; }
        }
        private string[] _providerName;


        /// <summary>
        /// Path parameter
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
        public string[] Path
        {
            get { return _path; }
            set { _path = value; }
        }
        private string[] _path;


        /// <summary>
        /// MaxEvents parameter
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
        [ValidateRange((Int64)1, Int64.MaxValue)]
        public Int64 MaxEvents
        {
            get { return _maxEvents; }
            set { _maxEvents = value; }
        }
        private Int64 _maxEvents = -1;

        /// <summary>
        /// ComputerName parameter
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
        public string ComputerName
        {
            get { return _computerName; }
            set { _computerName = value; }
        }
        private string _computerName = string.Empty;

        /// <summary>
        /// Credential parameter
        /// </summary>
        [Parameter(ParameterSetName = "ListProviderSet")]
        [Parameter(ParameterSetName = "GetProviderSet")]
        [Parameter(ParameterSetName = "ListLogSet")]
        [Parameter(ParameterSetName = "GetLogSet")]
        [Parameter(ParameterSetName = "HashQuerySet")]
        [Parameter(ParameterSetName = "XmlQuerySet")]
        [Parameter(ParameterSetName = "FileSet")]
        [Credential]
        public PSCredential Credential
        {
            get { return _credential; }
            set { _credential = value; }
        }
        private PSCredential _credential = PSCredential.Empty;


        /// <summary>
        /// FilterXPath parameter
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
        public string FilterXPath
        {
            get { return _filter; }
            set { _filter = value; }
        }
        private string _filter = "*";

        /// <summary>
        /// FilterXml parameter
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

        public XmlDocument FilterXml
        {
            get { return _xmlQuery; }
            set { _xmlQuery = value; }
        }
        private XmlDocument _xmlQuery = null;


        /// <summary>
        /// FilterHashtable parameter
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

        public Hashtable[] FilterHashtable
        {
            get { return _selector; }
            set { _selector = value; }
        }
        private Hashtable[] _selector;

        /// <summary>
        /// Force switch
        /// </summary>
        [Parameter(ParameterSetName = "ListLogSet")]
        [Parameter(ParameterSetName = "GetProviderSet")]
        [Parameter(ParameterSetName = "GetLogSet")]

        [Parameter(ParameterSetName = "HashQuerySet")]
        public SwitchParameter Force
        {
            get { return _force; }
            set { _force = value; }
        }
        private SwitchParameter _force;

        /// <summary>
        /// Oldest switch
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
        private const string queryCloser = "</Select></Query>";
        private const string propOpen = "[";
        private const string propClose = "]";
        private const string filePrefix = "file://";

        //
        // Other private members and constants
        //
        private ResourceManager _resourceMgr = null;
        private Dictionary<string, StringCollection> _providersByLogMap = new Dictionary<string, StringCollection>();

        private StringCollection _logNamesMatchingWildcard = null;
        private StringCollection _resolvedPaths = new StringCollection();

        private List<string> _accumulatedLogNames = new List<string>();
        private List<string> _accumulatedProviderNames = new List<string>();
        private List<string> _accumulatedFileNames = new List<string>();

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


        /// <summary>
        /// BeginProcessing() is invoked once per pipeline: we will load System.Core.dll here
        /// </summary>
        protected override void BeginProcessing()
        {
            _resourceMgr = Microsoft.PowerShell.Commands.Diagnostics.Common.CommonUtilities.GetResourceManager();
        }


        /// <summary>
        /// EndProcessing() is invoked once per pipeline
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
                    WriteDebug(string.Format(CultureInfo.InvariantCulture, "Invalid parameter set name: {0}", ParameterSetName));
                    break;
            }
        }


        //
        // AccumulatePipelineCounters() accumulates log names in the pipeline scenario:
        // we do not want to construct a query until all the log names are supplied.
        //
        private void AccumulatePipelineLogNames()
        {
            _accumulatedLogNames.AddRange(_logName);
        }

        //
        // AccumulatePipelineProviderNames() accumulates provider names in the pipeline scenario:
        // we do not want to construct a query until all the provider names are supplied.
        //
        private void AccumulatePipelineProviderNames()
        {
            _accumulatedProviderNames.AddRange(_logName);
        }

        //
        // AccumulatePipelineFileNames() accumulates log file paths in the pipeline scenario:
        // we do not want to construct a query until all the file names are supplied.
        //
        private void AccumulatePipelineFileNames()
        {
            _accumulatedFileNames.AddRange(_logName);
        }

        //
        // Process GetLog parameter set
        //
        private void ProcessGetLog()
        {
            EventLogSession eventLogSession = CreateSession();

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
                logQuery = new EventLogQuery(_logNamesMatchingWildcard[0], PathType.LogName, _filter);
            }
            logQuery.Session = eventLogSession;
            logQuery.ReverseDirection = !_oldest;

            EventLogReader readerObj = new EventLogReader(logQuery);

            if (readerObj != null)
            {
                ReadEvents(readerObj);
            }
        }


        //
        // Process GetProviderSet parameter set
        //
        private void ProcessGetProvider()
        {
            EventLogSession eventLogSession = CreateSession();

            FindProvidersByLogForWildcardPatterns(eventLogSession, _providerName);

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
                    WriteVerbose(string.Format(CultureInfo.InvariantCulture, "Log {0} will be queried", log));
                }
            }
            logQuery.Session = eventLogSession;
            logQuery.ReverseDirection = !_oldest; ;

            EventLogReader readerObj = new EventLogReader(logQuery);
            if (readerObj != null)
            {
                ReadEvents(readerObj);
            }
        }


        //
        // Process ListLog parameter set
        //
        private void ProcessListLog()
        {
            EventLogSession eventLogSession = CreateSession();

            foreach (string logPattern in _listLog)
            {
                bool bMatchFound = false;

                foreach (string logName in eventLogSession.GetLogNames())
                {
                    WildcardPattern wildLogPattern = new WildcardPattern(logPattern, WildcardOptions.IgnoreCase);

                    if (((!WildcardPattern.ContainsWildcardCharacters(logPattern))
                        && string.Equals(logPattern, logName, StringComparison.CurrentCultureIgnoreCase))
                        ||
                        (wildLogPattern.IsMatch(logName)))
                    {
                        try
                        {
                            EventLogConfiguration logObj = new EventLogConfiguration(logName, eventLogSession);

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

                            PSObject outputObj = new PSObject(logObj);

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
                            Exception outerExc = new Exception(msg, exc);
                            WriteError(new ErrorRecord(outerExc, "LogInfoUnavailable", ErrorCategory.NotSpecified, null));
                            continue;
                        }
                    }
                }
                if (!bMatchFound)
                {
                    string msg = _resourceMgr.GetString("NoMatchingLogsFound");
                    Exception exc = new Exception(string.Format(CultureInfo.InvariantCulture, msg, _computerName, logPattern));
                    WriteError(new ErrorRecord(exc, "NoMatchingLogsFound", ErrorCategory.ObjectNotFound, null));
                }
            }
        }

        //
        // Process ListProvider parameter set
        //
        private void ProcessListProvider()
        {
            EventLogSession eventLogSession = CreateSession();

            foreach (string provPattern in _listProvider)
            {
                bool bMatchFound = false;

                foreach (string provName in eventLogSession.GetProviderNames())
                {
                    WildcardPattern wildProvPattern = new WildcardPattern(provPattern, WildcardOptions.IgnoreCase);

                    if (((!WildcardPattern.ContainsWildcardCharacters(provPattern))
                        && string.Equals(provPattern, provName, StringComparison.CurrentCultureIgnoreCase))
                        ||
                        (wildProvPattern.IsMatch(provName)))
                    {
                        try
                        {
                            ProviderMetadata provObj = new ProviderMetadata(provName, eventLogSession, CultureInfo.CurrentCulture);
                            WriteObject(provObj);
                            bMatchFound = true;
                        }
                        catch (System.Diagnostics.Eventing.Reader.EventLogException exc)
                        {
                            string msg = string.Format(CultureInfo.InvariantCulture,
                                                       _resourceMgr.GetString("ProviderMetadataUnavailable"),
                                                       provName, exc.Message);
                            Exception outerExc = new Exception(msg, exc);
                            WriteError(new ErrorRecord(outerExc, "ProviderMetadataUnavailable", ErrorCategory.NotSpecified, null));
                            continue;
                        }
                    }
                }

                if (!bMatchFound)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("NoMatchingProvidersFound"),
                                             _computerName, provPattern);
                    Exception exc = new Exception(msg);
                    WriteError(new ErrorRecord(exc, "NoMatchingProvidersFound", ErrorCategory.ObjectNotFound, null));
                }
            }
        }

        //
        // Process FilterXml parameter set
        //
        private void ProcessFilterXml()
        {
            EventLogSession eventLogSession = CreateSession();

            if (!Oldest.IsPresent)
            {
                //
                // Do minimal parsing of xmlQuery to determine if any direct channels or ETL files are in it.        
                //
                XmlElement root = _xmlQuery.DocumentElement;
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

            EventLogQuery logQuery = new EventLogQuery(null, PathType.LogName, _xmlQuery.InnerXml);
            logQuery.Session = eventLogSession;
            logQuery.ReverseDirection = !_oldest;

            EventLogReader readerObj = new EventLogReader(logQuery);
            if (readerObj != null)
            {
                ReadEvents(readerObj);
            }
        }


        //
        // Process FileSet parameter set
        //
        private void ProcessFile()
        {
            EventLogSession eventLogSession = CreateSession();

            //
            // At this point, _path array contains paths that might have wildcards,
            // environment variables or PS drives. Let's resolve those.        
            //
            for (int i = 0; i < _path.Length; i++)
            {
                StringCollection resolvedPaths = ValidateAndResolveFilePath(_path[i]);
                foreach (string resolvedPath in resolvedPaths)
                {
                    _resolvedPaths.Add(resolvedPath);
                    WriteVerbose(string.Format(CultureInfo.InvariantCulture, "Found file {0}", resolvedPath));
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
                logQuery = new EventLogQuery(_resolvedPaths[0], PathType.FilePath, _filter);
            }
            logQuery.Session = eventLogSession;
            logQuery.ReverseDirection = !_oldest;

            EventLogReader readerObj = new EventLogReader(logQuery);
            if (readerObj != null)
            {
                ReadEvents(readerObj);
            }
        }

        //
        // Process HashQuerySet parameter set
        //
        private void ProcessHashQuery()
        {
            CheckHashTablesForNullValues();

            EventLogSession eventLogSession = CreateSession();

            string query = BuildStructuredQuery(eventLogSession);
            if (query.Length == 0)
            {
                return;
            }

            EventLogQuery logQuery = new EventLogQuery(null, PathType.FilePath, query);
            logQuery.Session = eventLogSession;
            logQuery.TolerateQueryErrors = true;
            logQuery.ReverseDirection = !_oldest;

            EventLogReader readerObj = new EventLogReader(logQuery);
            if (readerObj != null)
            {
                ReadEvents(readerObj);
            }
        }

        //
        // CreateSession creates an EventLogSession connected to a target machine or localhost.
        // If _credential argment is PSCredential.Empty, the session will be created for the current context.
        //
        private EventLogSession CreateSession()
        {
            EventLogSession eventLogSession = null;

            if (_computerName == string.Empty)
            {
                // Set _computerName to "localhost" for future error messages,
                // but do not use it for the connection to avoid RPC overhead.            
                _computerName = "localhost";

                if (_credential == PSCredential.Empty)
                {
                    return new EventLogSession();
                }
            }
            else if (_credential == PSCredential.Empty)
            {
                return new EventLogSession(_computerName);
            }

            // If we are here, either both computer name and credential were passed initially,
            // or credential only - we will use it with "localhost"

            NetworkCredential netCred = (NetworkCredential)_credential;
            eventLogSession = new EventLogSession(_computerName,
                                 netCred.Domain,
                                 netCred.UserName,
                                 _credential.Password,
                                 SessionAuthentication.Default
                                 );
            //
            // Force the destruction of cached password
            //
            netCred.Password = "";

            return eventLogSession;
        }


        //
        // ReadEvents helper.
        //
        private void ReadEvents(EventLogReader readerObj)
        {
            Int64 numEvents = 0;
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
                if (_maxEvents != -1 && numEvents >= _maxEvents)
                {
                    break;
                }

                PSObject outputObj = new PSObject(evtObj);

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
                Exception exc = new Exception(msg);
                WriteError(new ErrorRecord(exc, "NoMatchingEventsFound", ErrorCategory.ObjectNotFound, null));
            }
        }



        //
        // BuildStructuredQuery() builds a structured query from cmdlet arguments.
        //
        private string BuildStructuredQuery(EventLogSession eventLogSession)
        {
            string result = "";

            switch (ParameterSetName)
            {
                case "ListLogSet":
                    break;

                case "ListProviderSet":
                    break;

                case "GetProviderSet":
                    {
                        result = queryListOpen;
                        uint queryId = 0;

                        foreach (string log in _providersByLogMap.Keys)
                        {
                            string providerFilter = AddProviderPredicatesToFilter(_providersByLogMap[log]);
                            string addedQuery;
                            addedQuery = string.Format(CultureInfo.InvariantCulture, queryTemplate, new object[] { queryId++, log, providerFilter });
                            result += addedQuery;
                        }
                        result += queryListClose;
                    }
                    break;

                case "GetLogSet":
                    {
                        result = queryListOpen;
                        uint queryId = 0;
                        foreach (string log in _logNamesMatchingWildcard)
                        {
                            string addedQuery;
                            addedQuery = string.Format(CultureInfo.InvariantCulture, queryTemplate, new object[] { queryId++, log, _filter });
                            result += addedQuery;
                        }
                        result += queryListClose;
                    }
                    break;

                case "FileSet":
                    {
                        result = queryListOpen;
                        uint queryId = 0;
                        foreach (string filePath in _resolvedPaths)
                        {
                            string properFilePath = filePrefix + filePath;
                            string addedQuery;
                            addedQuery = string.Format(CultureInfo.InvariantCulture, queryTemplate, new object[] { queryId++, properFilePath, _filter });
                            result += addedQuery;
                        }
                        result += queryListClose;
                    }
                    break;

                case "HashQuerySet":
                    result = BuildStructuredQueryFromHashTable(eventLogSession);
                    break;

                default:
                    WriteDebug(string.Format(CultureInfo.InvariantCulture, "Invalid parameter set name: {0}", ParameterSetName));
                    break;
            }

            WriteVerbose(string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("QueryTrace"), result));

            return result;
        }

        //
        // BuildStructuredQueryFromHashTable() helper.
        // Builds a structured query from the hashtable (Selector) argument.
        //
        private string BuildStructuredQueryFromHashTable(EventLogSession eventLogSession)
        {
            string result = "";

            result = queryListOpen;

            uint queryId = 0;

            foreach (Hashtable hash in _selector)
            {
                string xpathString = "";

                CheckHashTableForQueryPathPresence(hash);

                //
                // Local queriedLogsQueryMap will hold names of logs or files to be queried
                // mapped to the actual query strings being built up.
                //
                Dictionary<string, string> queriedLogsQueryMap = new Dictionary<string, string>();

                //
                // Process log, _path, or provider parameters first
                // to create initial partially-filled query templates.
                // Error out for direct channels unless -oldest is present.
                // 
                // Order is important! Process "providername" key after "logname" and "file".
                //            
                if (hash.ContainsKey(hashkey_logname_lc))
                {
                    List<string> logPatterns = new List<string>();
                    if (hash[hashkey_logname_lc] is Array)
                    {
                        foreach (Object elt in (Array)hash[hashkey_logname_lc])
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
                    }
                }
                if (hash.ContainsKey(hashkey_path_lc))
                {
                    if (hash[hashkey_path_lc] is Array)
                    {
                        foreach (Object elt in (Array)hash[hashkey_path_lc])
                        {
                            StringCollection resolvedPaths = ValidateAndResolveFilePath(elt.ToString());
                            foreach (string resolvedPath in resolvedPaths)
                            {
                                queriedLogsQueryMap.Add(filePrefix + resolvedPath.ToLowerInvariant(),
                                                        string.Format(CultureInfo.InvariantCulture, queryOpenerTemplate, queryId++, filePrefix + resolvedPath));
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
                        }
                    }
                }
                if (hash.ContainsKey(hashkey_providername_lc))
                {
                    List<string> provPatterns = new List<string>();
                    if (hash[hashkey_providername_lc] is Array)
                    {
                        foreach (Object elt in (Array)hash[hashkey_providername_lc])
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
                        }
                    }
                    else
                    {
                        List<string> keysList = new List<string>(queriedLogsQueryMap.Keys);
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
                            Exception exc = new Exception(msg);
                            WriteError(new ErrorRecord(exc, "LogsAndProvidersDontOverlap", ErrorCategory.InvalidArgument, null));
                            continue;
                        }
                    }
                }

                //
                // If none of the logs/paths/providers were valid, queriedLogsQueryMap is empty.
                // Simply conitnue to the next hashtable since all the errors have been written already.
                //
                if (queriedLogsQueryMap.Count == 0)
                {
                    continue;
                }

                //
                // At this point queriedLogsQueryMap contains all the query openings: missing the actual XPaths
                // Let's build xpathString to attach to each query opening.
                //
                bool bDateTimeHandled = false;
                foreach (string key in hash.Keys)
                {
                    string added = "";

                    switch (key.ToLowerInvariant())
                    {
                        case hashkey_logname_lc:
                        case hashkey_path_lc:
                        case hashkey_providername_lc:
                            break;
                        case hashkey_id_lc:
                            added = HandleEventIdHashValue(hash[key]);
                            if (added.Length > 0)
                            {
                                ExtendPredicate(ref xpathString);
                                xpathString += added;
                            }
                            break;

                        case hashkey_level_lc:
                            added = HandleLevelHashValue(hash[key]);
                            if (added.Length > 0)
                            {
                                ExtendPredicate(ref xpathString);
                                xpathString += added;
                            }
                            break;

                        case hashkey_keywords_lc:
                            added = HandleKeywordHashValue(hash[key]);
                            if (added.Length > 0)
                            {
                                ExtendPredicate(ref xpathString);
                                xpathString += added;
                            }
                            break;

                        case hashkey_starttime_lc:
                            if (bDateTimeHandled)
                            {
                                break;
                            }
                            added = HandleStartTimeHashValue(hash[key], hash);
                            if (added.Length > 0)
                            {
                                ExtendPredicate(ref xpathString);
                                xpathString += added;
                            }

                            bDateTimeHandled = true;
                            break;

                        case hashkey_endtime_lc:
                            if (bDateTimeHandled)
                            {
                                break;
                            }

                            added = HandleEndTimeHashValue(hash[key], hash);
                            if (added.Length > 0)
                            {
                                ExtendPredicate(ref xpathString);
                                xpathString += added;
                            }

                            bDateTimeHandled = true;
                            break;

                        case hashkey_data_lc:
                            added = HandleDataHashValue(hash[key]);
                            if (added.Length > 0)
                            {
                                ExtendPredicate(ref xpathString);
                                xpathString += added;
                            }
                            break;

                        case hashkey_userid_lc:
                            added = HandleContextHashValue(hash[key]);
                            if (added.Length > 0)
                            {
                                ExtendPredicate(ref xpathString);
                                xpathString += added;
                            }
                            break;

                        default:
                            {
                                //
                                // None of the recognized values: this must be a named payload field
                                //
                                ExtendPredicate(ref xpathString);
                                xpathString += string.Format(CultureInfo.InvariantCulture,
                                                            "([EventData[Data[@Name='{0}']='{1}']] or [UserData/*/{0}='{1}'])",
                                                            key, hash[key]);
                            }
                            break;
                    }
                }

                //
                // Complete each query with the XPath.
                // Handle the case where the query opener already has provider predicate(s).
                // Add the queries from queriedLogsQueryMap into the resulting string.
                //        
                foreach (string query in queriedLogsQueryMap.Values)
                {
                    result += query;

                    if (query.EndsWith("*", StringComparison.OrdinalIgnoreCase))
                    {
                        //
                        // No provider predicate: just add the XPath string
                        //
                        if (xpathString.Length != 0)
                        {
                            result += propOpen + xpathString + propClose;
                        }
                    }
                    else
                    {
                        //
                        // Add xpathString to provider predicates.
                        //
                        if (xpathString.Length != 0)
                        {
                            result += " and " + xpathString;
                        }
                        result += propClose;
                    }

                    result += queryCloser;
                }
            } //end foreach hashtable  


            result += queryListClose;

            return result;
        }

        //
        // HandleEventIdHashValue helper for hashtable structured query builder.
        // Constructs and returns EventId XPath portion as a string.
        //
        private string HandleEventIdHashValue(Object value)
        {
            string ret = "";
            if (value is Array)
            {
                Array idsArray = (Array)(value);
                ret += "(";
                for (int i = 0; i < idsArray.Length; i++)
                {
                    ret += "(System/EventID=" + idsArray.GetValue(i).ToString() + ")";
                    if (i < (idsArray.Length - 1))
                    {
                        ret += " or ";
                    }
                }
                ret += ")";
            }
            else
            {
                ret += "(System/EventID=" + value + ")";
            }

            return ret;
        }

        //
        // HandleLevelHashValue helper for hashtable structured query builder.
        // Constructs and returns Level XPath portion as a string.
        //
        private string HandleLevelHashValue(Object value)
        {
            string ret = "";

            if (value is Array)
            {
                Array levelsArray = (Array)(value);
                ret += "(";
                for (int i = 0; i < levelsArray.Length; i++)
                {
                    ret += "(System/Level=" + levelsArray.GetValue(i).ToString() + ")";
                    if (i < (levelsArray.Length - 1))
                    {
                        ret += " or ";
                    }
                }
                ret += ")";
            }
            else
            {
                ret += "(System/Level=" + value + ")";
            }

            return ret;
        }

        //
        // HandleKeywordHashValue helper for hashtable structured query builder.
        // Constructs and returns Keyword XPath portion as a string.
        //
        private string HandleKeywordHashValue(Object value)
        {
            Int64 keywordsMask = 0;
            Int64 keywordLong = 0;

            if (value is Array)
            {
                foreach (Object keyword in (Array)value)
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
                    return "";
                }
                keywordsMask |= keywordLong;
            }

            return string.Format(CultureInfo.InvariantCulture, "System[band(Keywords,{0})]", keywordsMask);
        }

        //
        // HandleContextHashValue helper for hashtable structured query builder.
        // Constructs and returns UserID XPath portion as a string.
        // Handles both SIDs and domain account names.
        // Writes an error and returns an empty string if the SID or account names are not valid.
        //
        private string HandleContextHashValue(Object value)
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
                    NTAccount acct = new NTAccount(value.ToString());
                    sidCandidate = (SecurityIdentifier)acct.Translate(typeof(SecurityIdentifier));
                }
                catch (ArgumentException exc)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("InvalidContext"), value.ToString());
                    Exception outerExc = new Exception(msg, exc);
                    WriteError(new ErrorRecord(outerExc, "InvalidContext", ErrorCategory.InvalidArgument, null));
                    return "";
                }
            }

            return string.Format(CultureInfo.InvariantCulture, "(System/Security[@UserID='{0}'])", sidCandidate.ToString());
        }


        //
        // HandleStartTimeHashValue helper for hashtable structured query builder.
        // Constructs and returns TimeCreated XPath portion as a string.
        // NOTE that it also handles the hashtable "endtime" value (if supplied).
        //
        private string HandleStartTimeHashValue(Object value, Hashtable hash)
        {
            string ret = "";

            DateTime startTime = new DateTime();
            if (!StringToDateTime(value.ToString(), ref startTime))
            {
                return "";
            }

            startTime = startTime.ToUniversalTime();
            string startTimeFormatted = startTime.ToString("s", CultureInfo.InvariantCulture) + "." + startTime.Millisecond.ToString("d3", CultureInfo.InvariantCulture) + "Z";

            if (hash.ContainsKey(hashkey_endtime_lc))
            {
                DateTime endTime = new DateTime();
                if (!StringToDateTime(hash[hashkey_endtime_lc].ToString(), ref endTime))
                {
                    return "";
                }

                endTime = endTime.ToUniversalTime();
                string endTimeFormatted = endTime.ToString("s", CultureInfo.InvariantCulture) + "." + endTime.Millisecond.ToString("d3", CultureInfo.InvariantCulture) + "Z";

                ret += string.Format(CultureInfo.InvariantCulture,
                                             "(System/TimeCreated[@SystemTime&gt;='{0}' and @SystemTime&lt;='{1}'])",
                                             startTimeFormatted, endTimeFormatted);
            }
            else
            {
                ret += string.Format(CultureInfo.InvariantCulture,
                                             "(System/TimeCreated[@SystemTime&gt;='{0}'])",
                                             startTimeFormatted);
            }

            return ret;
        }


        //
        // HandleEndTimeHashValue helper for hashtable structured query builder.
        // Constructs and returns TimeCreated XPath portion as a string.
        // NOTE that it also handles the hashtable "starttime" value (if supplied).
        //
        private string HandleEndTimeHashValue(Object value, Hashtable hash)
        {
            string ret = "";

            DateTime endTime = new DateTime();
            if (!StringToDateTime(value.ToString(), ref endTime))
            {
                return "";
            }

            endTime = endTime.ToUniversalTime();
            string endTimeFormatted = endTime.ToString("s", CultureInfo.InvariantCulture) + "."
                                                       + endTime.Millisecond.ToString("d3", CultureInfo.InvariantCulture) + "Z";

            if (hash.ContainsKey(hashkey_starttime_lc))
            {
                DateTime startTime = new DateTime();
                if (!StringToDateTime(hash[hashkey_starttime_lc].ToString(), ref startTime))
                {
                    return "";
                }

                startTime = startTime.ToUniversalTime();
                string startTimeFormatted = startTime.ToString("s", CultureInfo.InvariantCulture) + "."
                                                               + startTime.Millisecond.ToString("d3", CultureInfo.InvariantCulture) + "Z";

                ret += string.Format(CultureInfo.InvariantCulture, "(System/TimeCreated[@SystemTime&gt;='{0}' and @SystemTime&lt;='{1}'])",
                                             startTimeFormatted, endTimeFormatted);
            }
            else
            {
                ret += string.Format(CultureInfo.InvariantCulture, "(System/TimeCreated[@SystemTime&lt;='{0}'])",
                                             endTimeFormatted);
            }

            return ret;
        }

        //
        // HandleDataHashValue helper for hashtable structured query builder.
        // Constructs and returns EventData/Data XPath portion as a string.
        //
        private string HandleDataHashValue(Object value)
        {
            string ret = "";
            if (value is Array)
            {
                Array dataArray = (Array)(value);
                ret += "(";
                for (int i = 0; i < dataArray.Length; i++)
                {
                    ret += string.Format(CultureInfo.InvariantCulture, "(EventData/Data='{0}')", dataArray.GetValue(i).ToString());
                    if (i < (dataArray.Length - 1))
                    {
                        ret += " or ";
                    }
                }
                ret += ")";
            }
            else
            {
                ret += string.Format(CultureInfo.InvariantCulture, "(EventData/Data='{0}')", value);
            }

            return ret;
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
                Exception exc = new Exception(msg);
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
                    Exception exc = new Exception(string.Format(CultureInfo.InvariantCulture, msg, fileName));
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
                Exception exc = new Exception(string.Format(CultureInfo.InvariantCulture, msg, _computerName, logName));
                WriteError(new ErrorRecord(exc, "NoMatchingLogsFound", ErrorCategory.ObjectNotFound, logName));
                return false;
            }
            catch (Exception exc)
            {
                string msg = string.Format(CultureInfo.InvariantCulture,
                                         _resourceMgr.GetString("LogInfoUnavailable"),
                                         logName, exc.Message);
                Exception outerExc = new Exception(msg, exc);
                WriteError(new ErrorRecord(outerExc, "LogInfoUnavailable", ErrorCategory.NotSpecified, null));
                return false;
            }
            if (!Oldest.IsPresent)
            {
                if (logObj.LogType == EventLogType.Debug || logObj.LogType == EventLogType.Analytical)
                {
                    string msg = _resourceMgr.GetString("SpecifyOldestForLog");
                    Exception exc = new Exception(string.Format(CultureInfo.InvariantCulture, msg, logName));
                    ThrowTerminatingError(new ErrorRecord(exc, "SpecifyOldestForLog", ErrorCategory.InvalidArgument, logName));
                }
            }
            return true;
        }

        //
        // ExtendPredicate helper for the query builder.
        // Extends the XPath predicate string.
        //
        private void ExtendPredicate(ref string xpathString)
        {
            if (xpathString.Length != 0)
            {
                xpathString += " and ";
            }
        }


        //
        // KeywordStringToInt64 helper converts a string to Int64.
        // Returns true and keyLong ref if successful.
        // Writes an error and returns false if keyString cannot be converted.
        //
        private bool KeywordStringToInt64(string keyString, ref Int64 keyLong)
        {
            try
            {
                keyLong = Convert.ToInt64(keyString, CultureInfo.InvariantCulture);
            }
            catch (Exception exc)
            {
                string msg = _resourceMgr.GetString("KeywordLongExpected");
                Exception outerExc = new Exception(string.Format(CultureInfo.InvariantCulture, msg, keyString), exc);
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
                Exception outerExc = new Exception(string.Format(CultureInfo.InvariantCulture, msg, dtString), exc);
                WriteError(new ErrorRecord(outerExc, "DateTimeExpected", ErrorCategory.InvalidArgument, null));
                return false;
            }

            return true;
        }

        //
        // ValidateAndResolveFilePath helper.
        // Returns a string collection of resolved file paths.
        // Writes non-terminating errors for invalid paths
        // and returns an empty colleciton.
        // 
        private StringCollection ValidateAndResolveFilePath(string path)
        {
            StringCollection retColl = new StringCollection();

            Collection<PathInfo> resolvedPathSubset = null;
            try
            {
                resolvedPathSubset = SessionState.Path.GetResolvedPSPathFromPSPath(path);
            }
            catch (PSNotSupportedException notSupported)
            {
                WriteError(new ErrorRecord(notSupported, "", ErrorCategory.ObjectNotFound, path));
                return retColl;
            }
            catch (System.Management.Automation.DriveNotFoundException driveNotFound)
            {
                WriteError(new ErrorRecord(driveNotFound, "", ErrorCategory.ObjectNotFound, path));
                return retColl;
            }
            catch (ProviderNotFoundException providerNotFound)
            {
                WriteError(new ErrorRecord(providerNotFound, "", ErrorCategory.ObjectNotFound, path));
                return retColl;
            }
            catch (ItemNotFoundException pathNotFound)
            {
                WriteError(new ErrorRecord(pathNotFound, "", ErrorCategory.ObjectNotFound, path));
                return retColl;
            }
            catch (Exception exc)
            {
                WriteError(new ErrorRecord(exc, "", ErrorCategory.ObjectNotFound, path));
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
                    Exception exc = new Exception(string.Format(CultureInfo.InvariantCulture, msg, path));
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
                        Exception exc = new Exception(string.Format(CultureInfo.InvariantCulture, msg, pi.ProviderPath));
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
            foreach (Hashtable hash in _selector)
            {
                foreach (string key in hash.Keys)
                {
                    Object value = hash[key];
                    if (value == null)
                    {
                        string msg = _resourceMgr.GetString("NullNotAllowedInHashtable");
                        Exception exc = new Exception(string.Format(CultureInfo.InvariantCulture, msg, key));
                        ThrowTerminatingError(new ErrorRecord(exc, "NullNotAllowedInHashtable", ErrorCategory.InvalidArgument, key));
                    }
                    else if (value is Array)
                    {
                        foreach (Object elt in (Array)value)
                        {
                            if (elt == null)
                            {
                                string msg = _resourceMgr.GetString("NullNotAllowedInHashtable");
                                Exception exc = new Exception(string.Format(CultureInfo.InvariantCulture, msg, key));
                                ThrowTerminatingError(new ErrorRecord(exc, "NullNotAllowedInHashtable", ErrorCategory.InvalidArgument, key));
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
                return _filter;
            }

            string ret = _filter;
            string predicate = BuildProvidersPredicate(providers);

            if (_filter.Equals("*", StringComparison.OrdinalIgnoreCase))
            {
                ret += "[" + predicate + "]";
            }
            else
            {
                //
                // Extend the XPath provided in the _filter
                //
                int lastPredClose = _filter.LastIndexOf(']');
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
        private string BuildProvidersPredicate(StringCollection providers)
        {
            if (providers.Count == 0)
            {
                return "";
            }

            string predicate = "System/Provider[";
            for (int i = 0; i < providers.Count; i++)
            {
                predicate += "@Name='" + providers[i] + "'";
                if (i < (providers.Count - 1))
                {
                    predicate += " or ";
                }
            }
            predicate += "]";

            return predicate;
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
                return "";
            }

            string predicate = "System/Provider[";

            List<string> uniqueProviderNames = new List<string>();

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
                predicate += "@Name='" + uniqueProviderNames[i] + "'";
                if (i < uniqueProviderNames.Count - 1)
                {
                    predicate += " or ";
                }
            }

            predicate += "]";

            return predicate;
        }


        //
        // AddLogsForProviderToInternalMap helper.
        // Retrieves log names to which _providerName writes.
        // NOTE: there are many misconfigured providers in the system.
        // We therefore catch EventLogException excpetions and write them out as non-terminating errors.
        // The results are added to _providersByLogMap dictionary.  
        //
        private void AddLogsForProviderToInternalMap(EventLogSession eventLogSession, string providerName)
        {
            try
            {
                ProviderMetadata providerMetadata = new ProviderMetadata(providerName, eventLogSession, CultureInfo.CurrentCulture);

                System.Collections.IEnumerable logLinks = providerMetadata.LogLinks;

                foreach (EventLogLink logLink in logLinks)
                {
                    if (!_providersByLogMap.ContainsKey(logLink.LogName.ToLowerInvariant()))
                    {
                        //
                        // Skip direct ETW channels unless -force is present.
                        // Error out for direct channels unless -oldest is present.
                        //                
                        EventLogConfiguration logObj = new EventLogConfiguration(logLink.LogName, eventLogSession);
                        if (logObj.LogType == EventLogType.Debug || logObj.LogType == EventLogType.Analytical)
                        {
                            if (!Force.IsPresent)
                            {
                                continue;
                            }

                            ValidateLogName(logLink.LogName, eventLogSession);
                        }

                        WriteVerbose(string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("ProviderLogLink"), providerName, logLink.LogName));

                        StringCollection provColl = new StringCollection();
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
                Exception outerExc = new Exception(msg, exc);
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
                foreach (string actualLogName in eventLogSession.GetLogNames())
                {
                    WildcardPattern wildLogPattern = new WildcardPattern(logPattern, WildcardOptions.IgnoreCase);

                    if (((!WildcardPattern.ContainsWildcardCharacters(logPattern))
                        && (logPattern.Equals(actualLogName, StringComparison.CurrentCultureIgnoreCase)))
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
                            Exception outerExc = new Exception(msg, exc);
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
                    Exception exc = new Exception(string.Format(CultureInfo.InvariantCulture, msg, _computerName, logPattern));
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
                foreach (string provName in eventLogSession.GetProviderNames())
                {
                    WildcardPattern wildProvPattern = new WildcardPattern(provPattern, WildcardOptions.IgnoreCase);

                    if (((!WildcardPattern.ContainsWildcardCharacters(provPattern))
                      && (provPattern.Equals(provName, StringComparison.CurrentCultureIgnoreCase)))
                      ||
                      (wildProvPattern.IsMatch(provName)))
                    {
                        WriteVerbose(string.Format(CultureInfo.InvariantCulture, "Found matching provider: {0}", provName));
                        AddLogsForProviderToInternalMap(eventLogSession, provName);
                        bMatched = true;
                    }
                }
                if (!bMatched)
                {
                    string msg = _resourceMgr.GetString("NoMatchingProvidersFound");
                    Exception exc = new Exception(string.Format(CultureInfo.InvariantCulture, msg, _computerName, provPattern));
                    WriteError(new ErrorRecord(exc, "NoMatchingProvidersFound", ErrorCategory.ObjectNotFound, provPattern));
                }
            }
        }
    }
}


