// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel; // Win32Exception
using System.Diagnostics; // Eventlog class
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    #region GetEventLogCommand
    /// <summary>
    /// This class implements the Get-EventLog command.
    /// </summary>
    /// <remarks>
    /// The CLR EventLogEntryCollection class has problems with managing
    /// rapidly spinning logs (i.e. logs set to "Overwrite" which are
    /// rapidly getting new events and discarding old events).
    /// In particular, if you enumerate forward
    ///     EventLogEntryCollection entries = log.Entries;
    ///     foreach (EventLogEntry entry in entries)
    /// it will occasionally skip an entry.  Conversely, if you are
    /// enumerating backward
    ///     EventLogEntryCollection entries = log.Entries;
    ///     int count = entries.Count;
    ///     for (int i = count-1; i >= 0; i--) {
    ///         EventLogEntry entry = entries[i];
    /// it will occasionally repeat an entry.  Accordingly, we enumerate
    /// backward and try to leave off the repeated entries.
    /// </remarks>
    [Cmdlet(VerbsCommon.Get, "EventLog", DefaultParameterSetName = "LogName",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113314", RemotingCapability = RemotingCapability.SupportedByCommand)]
    [OutputType(typeof(EventLog), typeof(EventLogEntry), typeof(string))]
    public sealed class GetEventLogCommand : PSCmdlet
    {
        #region Parameters
        /// <summary>
        /// Read eventlog entries from this log.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "LogName")]
        [Alias("LN")]
        public string LogName { get; set; }

        /// <summary>
        /// Read eventlog entries from this computer.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("Cn")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Read only this number of entries.
        /// </summary>
        [Parameter(ParameterSetName = "LogName")]
        [ValidateRange(0, Int32.MaxValue)]
        public int Newest { get; set; } = Int32.MaxValue;

        /// <summary>
        /// Return entries "after " this date.
        /// </summary>
        [Parameter(ParameterSetName = "LogName")]
        [ValidateNotNullOrEmpty]
        public DateTime After
        {
            get { return _after; }

            set
            {
                _after = value;
                _isDateSpecified = true;
                _isFilterSpecified = true;
            }
        }

        private DateTime _after;

        /// <summary>
        /// Return entries "Before" this date.
        /// </summary>
        [Parameter(ParameterSetName = "LogName")]
        [ValidateNotNullOrEmpty]
        public DateTime Before
        {
            get { return _before; }

            set
            {
                _before = value;
                _isDateSpecified = true;
                _isFilterSpecified = true;
            }
        }

        private DateTime _before;

        /// <summary>
        /// Return entries for this user.Wild characters is supported.
        /// </summary>
        [Parameter(ParameterSetName = "LogName")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] UserName
        {
            get { return _username; }

            set
            {
                _username = value;
                _isFilterSpecified = true;
            }
        }

        private string[] _username;

        /// <summary>
        /// Match eventlog entries by the InstanceIds
        /// gets or sets an array of instanceIds.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "LogName")]
        [ValidateNotNullOrEmpty]
        [ValidateRangeAttribute((long)0, long.MaxValue)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public long[] InstanceId
        {
            get { return _instanceIds; }

            set
            {
                _instanceIds = value;
                _isFilterSpecified = true;
            }
        }

        private long[] _instanceIds = null;

        /// <summary>
        /// Match eventlog entries by the Index
        /// gets or sets an array of indexes.
        /// </summary>
        [Parameter(ParameterSetName = "LogName")]
        [ValidateNotNullOrEmpty]
        [ValidateRangeAttribute((int)1, int.MaxValue)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public int[] Index
        {
            get { return _indexes; }

            set
            {
                _indexes = value;
                _isFilterSpecified = true;
            }
        }

        private int[] _indexes = null;

        /// <summary>
        /// Match eventlog entries by the EntryType
        /// gets or sets an array of EntryTypes.
        /// </summary>
        [Parameter(ParameterSetName = "LogName")]
        [ValidateNotNullOrEmpty]
        [ValidateSetAttribute(new string[] { "Error", "Information", "FailureAudit", "SuccessAudit", "Warning" })]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Alias("ET")]
        public string[] EntryType
        {
            get { return _entryTypes; }

            set
            {
                _entryTypes = value;
                _isFilterSpecified = true;
            }
        }

        private string[] _entryTypes = null;

        /// <summary>
        /// Get or sets an array of Source.
        /// </summary>
        [Parameter(ParameterSetName = "LogName")]
        [ValidateNotNullOrEmpty]
        [Alias("ABO")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Source
        {
            get
            { return _sources; }

            set
            {
                _sources = value;
                _isFilterSpecified = true;
            }
        }

        private string[] _sources;

        /// <summary>
        /// Get or Set Message string to searched in EventLog.
        /// </summary>
        [Parameter(ParameterSetName = "LogName")]
        [ValidateNotNullOrEmpty]
        [Alias("MSG")]
        public string Message
        {
            get
            {
                return _message;
            }

            set
            {
                _message = value;
                _isFilterSpecified = true;
            }
        }

        private string _message;

        /// <summary>
        /// Returns Log Entry as base object.
        /// </summary>
        [Parameter(ParameterSetName = "LogName")]
        public SwitchParameter AsBaseObject { get; set; }

        /// <summary>
        /// Return the Eventlog objects rather than the log contents.
        /// </summary>
        [Parameter(ParameterSetName = "List")]
        public SwitchParameter List { get; set; }

        /// <summary>
        /// Return the log names rather than the EventLog objects.
        /// </summary>
        [Parameter(ParameterSetName = "List")]
        public SwitchParameter AsString
        {
            get
            {
                return _asString;
            }

            set
            {
                _asString = value;
            }
        }

        private bool _asString /* = false */;
        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Sets true when Filter is Specified.
        /// </summary>
        private bool _isFilterSpecified = false;
        private bool _isDateSpecified = false;
        private bool _isThrowError = true;

        /// <summary>
        /// Process the specified logs.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (ParameterSetName == "List")
            {
                if (ComputerName.Length > 0)
                {
                    foreach (string computerName in ComputerName)
                    {
                        foreach (EventLog log in EventLog.GetEventLogs(computerName))
                        {
                            if (AsString)
                                WriteObject(log.Log);
                            else
                                WriteObject(log);
                        }
                    }
                }
                else
                {
                    foreach (EventLog log in EventLog.GetEventLogs())
                    {
                        if (AsString)
                            WriteObject(log.Log);
                        else
                            WriteObject(log);
                    }
                }
            }
            else
            {
                Diagnostics.Assert(ParameterSetName == "LogName", "Unexpected parameter set");

                if (!WildcardPattern.ContainsWildcardCharacters(LogName))
                {
                    OutputEvents(LogName);
                }
                else
                {
                    //
                    // If we were given a wildcard that matches more than one log, output the matching logs. Otherwise output the events in the matching log.
                    //
                    List<EventLog> matchingLogs = GetMatchingLogs(LogName);

                    if (matchingLogs.Count == 1)
                    {
                        OutputEvents(matchingLogs[0].Log);
                    }
                    else
                    {
                        foreach (EventLog log in matchingLogs)
                        {
                            WriteObject(log);
                        }
                    }
                }
            }
        }
        #endregion Overrides

        #region Private

        private void OutputEvents(string logName)
        {
            // 2005/04/21-JonN This somewhat odd structure works
            // around the FXCOP DisposeObjectsBeforeLosingScope rule.
            bool processing = false;
            try
            {
                if (ComputerName.Length == 0)
                {
                    using (EventLog specificLog = new EventLog(logName))
                    {
                        processing = true;
                        Process(specificLog);
                    }
                }
                else
                {
                    processing = true;

                    foreach (string computerName in ComputerName)
                    {
                        using (EventLog specificLog = new EventLog(logName, computerName))
                        {
                            Process(specificLog);
                        }
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                if (processing)
                {
                    throw;
                }

                ThrowTerminatingError(new ErrorRecord(
                    e, // default exception text is OK
                    "EventLogNotFound",
                    ErrorCategory.ObjectNotFound,
                    logName));
            }
        }

        private void Process(EventLog log)
        {
            bool matchesfound = false;
            if (Newest == 0)
            {
                return;
            }

            // enumerate backward, skipping repeat entries
            EventLogEntryCollection entries = log.Entries;

            int count = entries.Count;
            int lastindex = Int32.MinValue;
            int processed = 0;

            for (int i = count - 1; (i >= 0) && (processed < Newest); i--)
            {
                EventLogEntry entry = null;
                try
                {
                    entry = entries[i];
                }
                catch (ArgumentException e)
                {
                    ErrorRecord er = new ErrorRecord(
                        e,
                        "LogReadError",
                        ErrorCategory.ReadError,
                        null
                        );
                    er.ErrorDetails = new ErrorDetails(
                        this,
                        "EventlogResources",
                        "LogReadError",
                        log.Log,
                        e.Message
                        );
                    WriteError(er);

                    // NTRAID#Windows Out Of Band Releases-2005/09/27-JonN
                    // Break after the first one, rather than repeating this
                    // over and over
                    break;
                }
                catch (Exception e)
                {
                    Diagnostics.Assert(false,
                        "EventLogEntryCollection error "
                       + e.GetType().FullName
                        + ": " + e.Message);
                    throw;
                }

                if ((entry != null) &&
                ((lastindex == Int32.MinValue
                  || lastindex - entry.Index == 1)))
                {
                    lastindex = entry.Index;
                    if (_isFilterSpecified)
                    {
                        if (!FiltersMatch(entry))
                            continue;
                    }

                    if (!AsBaseObject)
                    {
                        // wrapping in PSobject to insert into PStypesnames
                        PSObject logentry = new PSObject(entry);
                        // inserting at zero position in reverse order
                        logentry.TypeNames.Insert(0, logentry.ImmediateBaseObject + "#" + log.Log + "/" + entry.Source);
                        logentry.TypeNames.Insert(0, logentry.ImmediateBaseObject + "#" + log.Log + "/" + entry.Source + "/" + entry.InstanceId);
                        WriteObject(logentry);
                        matchesfound = true;
                    }
                    else
                    {
                        WriteObject(entry);
                        matchesfound = true;
                    }

                    processed++;
                }
            }

            if (!matchesfound && _isThrowError)
            {
                Exception Ex = new ArgumentException(StringUtil.Format(EventlogResources.NoEntriesFound, log.Log, string.Empty));
                WriteError(new ErrorRecord(Ex, "GetEventLogNoEntriesFound", ErrorCategory.ObjectNotFound, null));
            }
        }

        private bool FiltersMatch(EventLogEntry entry)
        {
            if (_indexes != null)
            {
                if (!((IList)_indexes).Contains(entry.Index))
                {
                    return false;
                }
            }

            if (_instanceIds != null)
            {
                if (!((IList)_instanceIds).Contains(entry.InstanceId))
                {
                    return false;
                }
            }

            if (_entryTypes != null)
            {
                bool entrymatch = false;
                foreach (string type in _entryTypes)
                {
                    if (type.Equals(entry.EntryType.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        entrymatch = true;
                        break;
                    }
                }

                if (!entrymatch)
                {
                    return entrymatch;
                }
            }

            if (_sources != null)
            {
                bool sourcematch = false;
                foreach (string source in _sources)
                {
                    if (WildcardPattern.ContainsWildcardCharacters(source))
                    {
                        _isThrowError = false;
                    }

                    WildcardPattern wildcardpattern = WildcardPattern.Get(source, WildcardOptions.IgnoreCase);
                    if (wildcardpattern.IsMatch(entry.Source))
                    {
                        sourcematch = true;
                        break;
                    }
                }

                if (!sourcematch)
                {
                    return sourcematch;
                }
            }

            if (_message != null)
            {
                if (WildcardPattern.ContainsWildcardCharacters(_message))
                {
                    _isThrowError = false;
                }

                WildcardPattern wildcardpattern = WildcardPattern.Get(_message, WildcardOptions.IgnoreCase);
                if (!wildcardpattern.IsMatch(entry.Message))
                {
                    return false;
                }
            }

            if (_username != null)
            {
                bool usernamematch = false;
                foreach (string user in _username)
                {
                    _isThrowError = false;
                    if (entry.UserName != null)
                    {
                        WildcardPattern wildcardpattern = WildcardPattern.Get(user, WildcardOptions.IgnoreCase);
                        if (wildcardpattern.IsMatch(entry.UserName))
                        {
                            usernamematch = true;
                            break;
                        }
                    }
                }

                if (!usernamematch) 
                {
                    return usernamematch;
                }
            }

            if (_isDateSpecified)
            {
                _isThrowError = false;
                bool datematch = false;
                if (!_after.Equals(_initial) && _before.Equals(_initial))
                {
                    if (entry.TimeGenerated > _after)
                    {
                        datematch = true;
                    }
                }
                else if (!_before.Equals(_initial) && _after.Equals(_initial))
                {
                    if (entry.TimeGenerated < _before)
                    {
                        datematch = true;
                    }
                }
                else if (!_after.Equals(_initial) && !_before.Equals(_initial))
                {
                    if (_after > _before || _after == _before)
                    {
                        if ((entry.TimeGenerated > _after) || (entry.TimeGenerated < _before))
                            datematch = true;
                    }
                    else
                    {
                        if ((entry.TimeGenerated > _after) && (entry.TimeGenerated < _before))
                        {
                            datematch = true;
                        }
                    }
                }

                if (!datematch) 
                {
                    return datematch;
                }
            }

            return true;
        }

        private List<EventLog> GetMatchingLogs(string pattern)
        {
            WildcardPattern wildcardPattern = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
            List<EventLog> matchingLogs = new List<EventLog>();
            if (ComputerName.Length == 0)
            {
                foreach (EventLog log in EventLog.GetEventLogs())
                {
                    if (wildcardPattern.IsMatch(log.Log))
                    {
                        matchingLogs.Add(log);
                    }
                }
            }
            else
            {
                foreach (string computerName in ComputerName)
                {
                    foreach (EventLog log in EventLog.GetEventLogs(computerName))
                    {
                        if (wildcardPattern.IsMatch(log.Log))
                        {
                            matchingLogs.Add(log);
                        }
                    }
                }
            }

            return matchingLogs;
        }
        // private string ErrorBase = "EventlogResources";
        private DateTime _initial = new DateTime();

        #endregion Private
    }
    #endregion GetEventLogCommand

    #region ClearEventLogCommand
    /// <summary>
    /// This class implements the Clear-EventLog command.
    /// </summary>

    [Cmdlet(VerbsCommon.Clear, "EventLog", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135198", RemotingCapability = RemotingCapability.SupportedByCommand)]
    public sealed class ClearEventLogCommand : PSCmdlet
    {
        #region Parameters
        /// <summary>
        /// Clear these logs.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Alias("LN")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] LogName { get; set; }

        /// <summary>
        /// Clear eventlog entries from these Computers.
        /// </summary>
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Alias("Cn")]
        public string[] ComputerName { get; set; } = { "." };

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Does the processing.
        /// </summary>
        protected override void BeginProcessing()
        {
            string computer = string.Empty;
            foreach (string compName in ComputerName)
            {
                if ((compName.Equals("localhost", StringComparison.OrdinalIgnoreCase)) || (compName.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    computer = "localhost";
                }
                else
                {
                    computer = compName;
                }

                foreach (string eventString in LogName)
                {
                    try
                    {
                        if (!EventLog.Exists(eventString, compName))
                        {
                            ErrorRecord er = new ErrorRecord(new InvalidOperationException(StringUtil.Format(EventlogResources.LogDoesNotExist, eventString, computer)), null, ErrorCategory.InvalidOperation, null);
                            WriteError(er);
                            continue;
                        }

                        if (!ShouldProcess(StringUtil.Format(EventlogResources.ClearEventLogWarning, eventString, computer)))
                        {
                            continue;
                        }

                        EventLog Log = new EventLog(eventString, compName);
                        Log.Clear();
                    }
                    catch (System.IO.IOException)
                    {
                        ErrorRecord er = new ErrorRecord(new System.IO.IOException(StringUtil.Format(EventlogResources.PathDoesNotExist, null, computer)), null, ErrorCategory.InvalidOperation, null);
                        WriteError(er);
                        continue;
                    }
                    catch (Win32Exception)
                    {
                        ErrorRecord er = new ErrorRecord(new Win32Exception(StringUtil.Format(EventlogResources.NoAccess, null, computer)), null, ErrorCategory.PermissionDenied, null);
                        WriteError(er);
                        continue;
                    }
                    catch (InvalidOperationException)
                    {
                        ErrorRecord er = new ErrorRecord(new InvalidOperationException(StringUtil.Format(EventlogResources.OSWritingError)), null, ErrorCategory.ReadError, null);
                        WriteError(er);
                        continue;
                    }
                }
            }
        }

        // beginprocessing

        #endregion Overrides
    }
    #endregion ClearEventLogCommand

    #region WriteEventLogCommand
    /// <summary>
    /// This class implements the Write-EventLog command.
    /// </summary>

    [Cmdlet(VerbsCommunications.Write, "EventLog", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135281", RemotingCapability = RemotingCapability.SupportedByCommand)]
    public sealed class WriteEventLogCommand : PSCmdlet
    {
        #region Parameters
        /// <summary>
        /// Write eventlog entries in this log.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [Alias("LN")]
        [ValidateNotNullOrEmpty]
        public string LogName { get; set; }

        /// <summary>
        /// The source by which the application is registered on the specified computer.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        [Alias("SRC")]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        /// <summary>
        /// String which represents One of the EventLogEntryType values.
        /// </summary>
        [Parameter(Position = 3)]
        [Alias("ET")]
        [ValidateNotNullOrEmpty]
        [ValidateSetAttribute(new string[] { "Error", "Information", "FailureAudit", "SuccessAudit", "Warning" })]
        public EventLogEntryType EntryType { get; set; } = EventLogEntryType.Information;

        /// <summary>
        /// The application-specific subcategory associated with the message.
        /// </summary>
        [Parameter]
        public Int16 Category { get; set; } = 1;

        /// <summary>
        /// The application-specific identifier for the event.
        /// </summary>
        [Parameter(Position = 2, Mandatory = true)]
        [Alias("ID", "EID")]
        [ValidateNotNullOrEmpty]
        [ValidateRange(0, UInt16.MaxValue)]
        public Int32 EventId { get; set; }

        /// <summary>
        /// The message goes here.
        /// </summary>
        [Parameter(Position = 4, Mandatory = true)]
        [Alias("MSG")]
        [ValidateNotNullOrEmpty]
        [ValidateLength(0, 32766)]
        public string Message { get; set; }

        /// <summary>
        /// Write eventlog entries of this log.
        /// </summary>
        [Parameter]
        [Alias("RD")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] RawData { get; set; }

        /// <summary>
        /// Write eventlog entries of this log.
        /// </summary>
        [Parameter]
        [Alias("CN")]
        [ValidateNotNullOrEmpty]

        public string ComputerName { get; set; } = ".";

        #endregion Parameters
        #region private

        private void WriteNonTerminatingError(Exception exception, string errorId, string errorMessage,
            ErrorCategory category)
        {
            Exception ex = new Exception(errorMessage, exception);
            WriteError(new ErrorRecord(ex, errorId, category, null));
        }

        #endregion private
        #region Overrides

        /// <summary>
        /// Does the processing.
        /// </summary>
        protected override void BeginProcessing()
        {
            string _computerName = string.Empty;
            if ((ComputerName.Equals("localhost", StringComparison.OrdinalIgnoreCase)) || (ComputerName.Equals(".", StringComparison.OrdinalIgnoreCase)))
            {
                _computerName = "localhost";
            }
            else
            {
                _computerName = ComputerName;
            }

            try
            {
                if (!(EventLog.SourceExists(Source, ComputerName)))
                {
                    ErrorRecord er = new ErrorRecord(new InvalidOperationException(StringUtil.Format(EventlogResources.SourceDoesNotExist, null, _computerName, Source)), null, ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                }
                else
                {
                    if (!(EventLog.Exists(LogName, ComputerName)))
                    {
                        ErrorRecord er = new ErrorRecord(new InvalidOperationException(StringUtil.Format(EventlogResources.LogDoesNotExist, LogName, _computerName)), null, ErrorCategory.InvalidOperation, null);
                        WriteError(er);
                    }
                    else
                    {
                        EventLog _myevent = new EventLog(LogName, ComputerName, Source);
                        _myevent.WriteEntry(Message, EntryType, EventId, Category, RawData);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                WriteNonTerminatingError(ex, ex.Message, ex.Message, ErrorCategory.InvalidOperation);
            }
            catch (InvalidOperationException ex)
            {
                WriteNonTerminatingError(ex, "AccessDenied", StringUtil.Format(EventlogResources.AccessDenied, LogName, null, Source), ErrorCategory.PermissionDenied);
            }
            catch (Win32Exception ex)
            {
                WriteNonTerminatingError(ex, "OSWritingError", StringUtil.Format(EventlogResources.OSWritingError, null, null, null), ErrorCategory.WriteError);
            }
            catch (System.IO.IOException ex)
            {
                WriteNonTerminatingError(ex, "PathDoesNotExist", StringUtil.Format(EventlogResources.PathDoesNotExist, null, ComputerName, null), ErrorCategory.InvalidOperation);
            }
        }

        #endregion Overrides
    }
    #endregion WriteEventLogCommand

    #region LimitEventLogCommand
    /// <summary>
    /// This class implements the Limit-EventLog command.
    /// </summary>

    [Cmdlet(VerbsData.Limit, "EventLog", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135227", RemotingCapability = RemotingCapability.SupportedByCommand)]
    public sealed class LimitEventLogCommand : PSCmdlet
    {
        #region Parameters
        /// <summary>
        /// Limit the properties of this log.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [Alias("LN")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] LogName { get; set; }

        /// <summary>
        /// Limit eventlog entries of this computer.
        /// </summary>
        [Parameter]
        [Alias("CN")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName { get; set; } = { "." };

        /// <summary>
        /// Minimum retention days for this log.
        /// </summary>
        [Parameter]
        [Alias("MRD")]
        [ValidateNotNullOrEmpty]
        [ValidateRange(1, 365)]
        public Int32 RetentionDays
        {
            get { return _retention; }

            set
            {
                _retention = value;
                _retentionSpecified = true;
            }
        }

        private Int32 _retention;
        private bool _retentionSpecified = false;
        /// <summary>
        /// Overflow action to be taken.
        /// </summary>
        [Parameter]
        [Alias("OFA")]
        [ValidateNotNullOrEmpty]
        [ValidateSetAttribute(new string[] { "OverwriteOlder", "OverwriteAsNeeded", "DoNotOverwrite" })]

        public System.Diagnostics.OverflowAction OverflowAction
        {
            get { return _overflowaction; }

            set
            {
                _overflowaction = value;
                _overflowSpecified = true;
            }
        }

        private System.Diagnostics.OverflowAction _overflowaction;
        private bool _overflowSpecified = false;
        /// <summary>
        /// Maximum size of this log.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public Int64 MaximumSize
        {
            get { return _maximumKilobytes; }

            set
            {
                _maximumKilobytes = value;
                _maxkbSpecified = true;
            }
        }

        private Int64 _maximumKilobytes;
        private bool _maxkbSpecified = false;
        #endregion Parameters

        #region private
        private void WriteNonTerminatingError(Exception exception, string resourceId, string errorId,
      ErrorCategory category, string _logName, string _compName)
        {
            Exception ex = new Exception(StringUtil.Format(resourceId, _logName, _compName), exception);
            WriteError(new ErrorRecord(ex, errorId, category, null));
        }

        #endregion private

        #region Overrides

        /// <summary>
        /// Does the processing.
        /// </summary>
        protected override
        void
        BeginProcessing()
        {
            string computer = string.Empty;
            foreach (string compname in ComputerName)
            {
                if ((compname.Equals("localhost", StringComparison.OrdinalIgnoreCase)) || (compname.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    computer = "localhost";
                }
                else
                {
                    computer = compname;
                }

                foreach (string logname in LogName)
                {
                    try
                    {
                        if (!EventLog.Exists(logname, compname))
                        {
                            ErrorRecord er = new ErrorRecord(new InvalidOperationException(StringUtil.Format(EventlogResources.LogDoesNotExist, logname, computer)), null, ErrorCategory.InvalidOperation, null);
                            WriteError(er);
                            continue;
                        }
                        else
                        {
                            if (!ShouldProcess(StringUtil.Format(EventlogResources.LimitEventLogWarning, logname, computer)))
                            {
                                continue;
                            }
                            else
                            {
                                EventLog newLog = new EventLog(logname, compname);
                                int _minRetention = newLog.MinimumRetentionDays;
                                System.Diagnostics.OverflowAction _newFlowAction = newLog.OverflowAction;
                                if (_retentionSpecified && _overflowSpecified)
                                {
                                    if (_overflowaction.CompareTo(System.Diagnostics.OverflowAction.OverwriteOlder) == 0)
                                    {
                                        newLog.ModifyOverflowPolicy(_overflowaction, _retention);
                                    }
                                    else
                                    {
                                        ErrorRecord er = new ErrorRecord(new InvalidOperationException(StringUtil.Format(EventlogResources.InvalidOverflowAction)), null, ErrorCategory.InvalidOperation, null);
                                        WriteError(er);
                                        continue;
                                    }
                                }
                                else if (_retentionSpecified && !_overflowSpecified)
                                {
                                    if (_newFlowAction.CompareTo(System.Diagnostics.OverflowAction.OverwriteOlder) == 0)
                                    {
                                        newLog.ModifyOverflowPolicy(_newFlowAction, _retention);
                                    }
                                    else
                                    {
                                        ErrorRecord er = new ErrorRecord(new InvalidOperationException(StringUtil.Format(EventlogResources.InvalidOverflowAction)), null, ErrorCategory.InvalidOperation, null);
                                        WriteError(er);
                                        continue;
                                    }
                                }
                                else if (!_retentionSpecified && _overflowSpecified)
                                {
                                    newLog.ModifyOverflowPolicy(_overflowaction, _minRetention);
                                }

                                if (_maxkbSpecified)
                                {
                                    int kiloByte = 1024;
                                    _maximumKilobytes = _maximumKilobytes / kiloByte;
                                    newLog.MaximumKilobytes = _maximumKilobytes;
                                }
                            }
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        WriteNonTerminatingError(ex, EventlogResources.PermissionDenied, "PermissionDenied", ErrorCategory.PermissionDenied, logname, computer);
                        continue;
                    }
                    catch (System.IO.IOException ex)
                    {
                        WriteNonTerminatingError(ex, EventlogResources.PathDoesNotExist, "PathDoesNotExist", ErrorCategory.InvalidOperation, null, computer);
                        continue;
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        if (!_retentionSpecified && !_maxkbSpecified)
                        {
                            WriteNonTerminatingError(ex, EventlogResources.InvalidArgument, "InvalidArgument", ErrorCategory.InvalidData, null, null);
                        }
                        else
                        {
                            WriteNonTerminatingError(ex, EventlogResources.ValueOutofRange, "ValueOutofRange", ErrorCategory.InvalidData, null, null);
                        }

                        continue;
                    }
                }
            }
        }
        #endregion override

    }
    #endregion LimitEventLogCommand

    #region ShowEventLogCommand
    /// <summary>
    /// This class implements the Show-EventLog command.
    /// </summary>

    [Cmdlet(VerbsCommon.Show, "EventLog", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135257", RemotingCapability = RemotingCapability.SupportedByCommand)]
    public sealed class ShowEventLogCommand : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// Show eventviewer of this computer.
        /// </summary>
        [Parameter(Position = 0)]
        [Alias("CN")]
        [ValidateNotNullOrEmpty]

        public string ComputerName { get; set; } = ".";

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Does the processing.
        /// </summary>
        protected override
        void
        BeginProcessing()
        {
            try
            {
                string eventVwrExe = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "eventvwr.exe");
                Process.Start(eventVwrExe, ComputerName);
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode.Equals(0x00000002))
                {
                    string message = StringUtil.Format(EventlogResources.NotSupported);
                    InvalidOperationException ex = new InvalidOperationException(message);
                    ErrorRecord er = new ErrorRecord(ex, "Win32Exception", ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                }
                else
                {
                    ErrorRecord er = new ErrorRecord(e, "Win32Exception", ErrorCategory.InvalidArgument, null);
                    WriteError(er);
                }
            }
            catch (SystemException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "InvalidComputerName", ErrorCategory.InvalidArgument, ComputerName);
                WriteError(er);
            }
        }
        #endregion override
    }
    #endregion ShowEventLogCommand

    #region NewEventLogCommand
    /// <summary>
    /// This cmdlet creates the new event log .This cmdlet can also be used to
    /// configure a new source for writing entries to an event log on the local
    /// computer or a remote computer.
    /// You can create an event source for an existing event log or a new event log.
    /// When you create a new source for a new event log, the system registers the
    /// source for that log, but the log is not created until the first entry is
    /// written to it.
    /// The operating system stores event logs as files. The associated file is
    /// stored in the %SystemRoot%\System32\Config directory on the specified
    /// computer. The file name is set by appending the first 8 characters of the
    /// Log property with the ".evt" file name extension.
    /// You can register the event source with localized resource file(s) for your
    /// event category and message strings. Your application can write event log
    /// entries using resource identifiers, rather than specifying the actual
    /// string. You can register a separate file for event categories, messages and
    /// parameter insertion strings, or you can register the same resource file for
    /// all three types of strings.
    /// </summary>

    [Cmdlet(VerbsCommon.New, "EventLog", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135235", RemotingCapability = RemotingCapability.SupportedByCommand)]
    public class NewEventLogCommand : PSCmdlet
    {
        #region Parameter
        /// <summary>
        /// The following is the definition of the input parameter "CategoryResourceFile".
        /// Specifies the path of the resource file that contains category strings for
        /// the source
        /// Resource File is expected to be present in Local/Remote Machines.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("CRF")]
        public string CategoryResourceFile { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Specify the Computer Name. The default is local computer.
        /// </summary>
        [Parameter(Position = 2)]
        [ValidateNotNullOrEmpty]
        [Alias("CN")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName { get; set; } = { "." };

        /// <summary>
        /// The following is the definition of the input parameter "LogName".
        /// Specifies the name of the log.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0)]
        [ValidateNotNullOrEmpty]
        [Alias("LN")]
        public string LogName { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "MessageResourceFile".
        /// Specifies the path of the message resource file that contains message
        /// formatting strings for the source
        /// Resource File is expected to be present in Local/Remote Machines.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("MRF")]
        public string MessageResourceFile { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "ParameterResourceFile".
        /// Specifies the path of the resource file that contains message parameter
        /// strings for the source
        /// Resource File is expected to be present in Local/Remote Machines.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("PRF")]
        public string ParameterResourceFile { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Source".
        /// Specifies the Source of the EventLog.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 1)]
        [ValidateNotNullOrEmpty]
        [Alias("SRC")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Source { get; set; }

        #endregion Parameter

        #region private
        private void WriteNonTerminatingError(Exception exception, string resourceId, string errorId,
            ErrorCategory category, string _logName, string _compName, string _source, string _resourceFile)
        {
            Exception ex = new Exception(StringUtil.Format(resourceId, _logName, _compName, _source, _resourceFile), exception);
            WriteError(new ErrorRecord(ex, errorId, category, null));
        }

        #endregion private

        #region override
        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            string computer = string.Empty;
            foreach (string compname in ComputerName)
            {
                if ((compname.Equals("localhost", StringComparison.OrdinalIgnoreCase)) || (compname.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    computer = "localhost";
                }
                else
                {
                    computer = compname;
                }

                try
                {
                    foreach (string _sourceName in Source)
                    {
                        if (!EventLog.SourceExists(_sourceName, compname))
                        {
                            EventSourceCreationData newEventSource = new EventSourceCreationData(_sourceName, LogName);
                            newEventSource.MachineName = compname;
                            if (!string.IsNullOrEmpty(MessageResourceFile))
                                newEventSource.MessageResourceFile = MessageResourceFile;
                            if (!string.IsNullOrEmpty(ParameterResourceFile))
                                newEventSource.ParameterResourceFile = ParameterResourceFile;
                            if (!string.IsNullOrEmpty(CategoryResourceFile))
                                newEventSource.CategoryResourceFile = CategoryResourceFile;
                            EventLog.CreateEventSource(newEventSource);
                        }
                        else
                        {
                            ErrorRecord er = new ErrorRecord(new InvalidOperationException(StringUtil.Format(EventlogResources.SourceExistInComp, null, computer, _sourceName)), null, ErrorCategory.InvalidOperation, null);
                            WriteError(er);
                            continue;
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    WriteNonTerminatingError(ex, EventlogResources.PermissionDenied, "PermissionDenied", ErrorCategory.PermissionDenied, LogName, computer, null, null);
                    continue;
                }
                catch (ArgumentException ex)
                {
                    ErrorRecord er = new ErrorRecord(ex, "NewEventlogException", ErrorCategory.InvalidArgument, null);
                    WriteError(er);
                    continue;
                }
                catch (System.Security.SecurityException ex)
                {
                    WriteNonTerminatingError(ex, EventlogResources.AccessIsDenied, "AccessIsDenied", ErrorCategory.InvalidOperation, null, null, null, null);
                    continue;
                }
            }
        }
        // End BeginProcessing()
        #endregion override
    }
    #endregion NewEventLogCommand

    #region RemoveEventLogCommand
    /// <summary>
    /// This cmdlet is used to delete the specified event log from the specified
    /// computer. This can also be used to Clear the entries of the specified event
    /// log and also to unregister the Source associated with the eventlog.
    /// </summary>

    [Cmdlet(VerbsCommon.Remove, "EventLog",
             SupportsShouldProcess = true, DefaultParameterSetName = "Default",
             HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135248", RemotingCapability = RemotingCapability.SupportedByCommand)]
    public class RemoveEventLogCommand : PSCmdlet
    {
        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Specifies the Computer Name.
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateNotNull]
        [ValidateNotNullOrEmpty]
        [Alias("CN")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName { get; set; } = { "." };

        /// <summary>
        /// The following is the definition of the input parameter "LogName".
        /// Specifies the Event Log Name.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0, ParameterSetName = "Default")]
        [ValidateNotNull]
        [ValidateNotNullOrEmpty]
        [Alias("LN")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] LogName { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "RemoveSource".
        /// Specifies either to remove the event log and associated source or
        /// source. alone.
        /// When this parameter is not specified, the cmdlet uses Delete Method which
        /// clears the eventlog and also the source associated with it.
        /// When this parameter value is true, then this cmdlet uses DeleteEventSource
        /// Method to delete the Source alone.
        /// </summary>
        [Parameter(ParameterSetName = "Source")]
        [ValidateNotNull]
        [ValidateNotNullOrEmpty]
        [Alias("SRC")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Source { get; set; }

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            try
            {
                string computer = string.Empty;
                foreach (string compName in ComputerName)
                {
                    if ((compName.Equals("localhost", StringComparison.OrdinalIgnoreCase)) || (compName.Equals(".", StringComparison.OrdinalIgnoreCase)))
                    {
                        computer = "localhost";
                    }
                    else
                    {
                        computer = compName;
                    }

                    if (ParameterSetName.Equals("Default"))
                    {
                        foreach (string log in LogName)
                        {
                            try
                            {
                                if (EventLog.Exists(log, compName))
                                {
                                    if (!ShouldProcess(StringUtil.Format(EventlogResources.RemoveEventLogWarning, log, computer)))
                                    {
                                        continue;
                                    }

                                    EventLog.Delete(log, compName);
                                }
                                else
                                {
                                    ErrorRecord er = new ErrorRecord(new InvalidOperationException(StringUtil.Format(EventlogResources.LogDoesNotExist, log, computer)), null, ErrorCategory.InvalidOperation, null);
                                    WriteError(er);
                                    continue;
                                }
                            }
                            catch (System.IO.IOException)
                            {
                                ErrorRecord er = new ErrorRecord(new InvalidOperationException(StringUtil.Format(EventlogResources.PathDoesNotExist, null, computer)), null, ErrorCategory.InvalidOperation, null);
                                WriteError(er);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        foreach (string src in Source)
                        {
                            try
                            {
                                if (EventLog.SourceExists(src, compName))
                                {
                                    if (!ShouldProcess(StringUtil.Format(EventlogResources.RemoveSourceWarning, src, computer)))
                                    {
                                        continue;
                                    }

                                    EventLog.DeleteEventSource(src, compName);
                                }
                                else
                                {
                                    ErrorRecord er = new ErrorRecord(new InvalidOperationException(StringUtil.Format(EventlogResources.SourceDoesNotExist, string.Empty, computer, src)), null, ErrorCategory.InvalidOperation, null);
                                    WriteError(er);
                                    continue;
                                }
                            }
                            catch (System.IO.IOException)
                            {
                                ErrorRecord er = new ErrorRecord(new InvalidOperationException(StringUtil.Format(EventlogResources.PathDoesNotExist, null, computer)), null, ErrorCategory.InvalidOperation, null);
                                WriteError(er);
                                continue;
                            }
                        }
                    }
                }
            }
            catch (System.Security.SecurityException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "NewEventlogException", ErrorCategory.SecurityError, null);
                WriteError(er);
            }
        }
    }

    #endregion RemoveEventLogCommand
}
