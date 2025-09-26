// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Xml;

using Microsoft.PowerShell.Commands.Diagnostics.Common;
using Microsoft.PowerShell.Commands.GetCounter;
using Microsoft.Powershell.Commands.GetCounter.PdhNative;

namespace Microsoft.PowerShell.Commands
{
    ///
    /// Class that implements the Get-Counter cmdlet.
    ///
    [Cmdlet(VerbsData.Import, "Counter", DefaultParameterSetName = "GetCounterSet", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=138338")]
    public sealed class ImportCounterCommand : PSCmdlet
    {
        //
        // Path parameter
        //
        [Parameter(
                Position = 0,
                Mandatory = true,
                ValueFromPipeline = true,
                ValueFromPipelineByPropertyName = true,
                HelpMessageBaseName = "GetEventResources")]
        [Alias("PSPath")]

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.GetCounterCommand.ListSet",
                            Justification = "A string[] is required here because that is the type Powershell supports")]
        public string[] Path
        {
            get { return _path; }

            set { _path = value; }
        }

        private string[] _path;

        private StringCollection _resolvedPaths = new StringCollection();

        private List<string> _accumulatedFileNames = new List<string>();

        //
        // ListSet parameter
        //
        [Parameter(
                Mandatory = true,
                ParameterSetName = "ListSetSet",
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources")]
        [AllowEmptyCollection]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.GetCounterCommand.ListSet",
                            Justification = "A string[] is required here because that is the type Powershell supports")]
        public string[] ListSet
        {
            get { return _listSet; }

            set { _listSet = value; }
        }

        private string[] _listSet = Array.Empty<string>();

        //
        // StartTime parameter
        //
        [Parameter(
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                ParameterSetName = "GetCounterSet",
                HelpMessageBaseName = "GetEventResources")]
        public DateTime StartTime
        {
            get { return _startTime; }

            set { _startTime = value; }
        }

        private DateTime _startTime = DateTime.MinValue;

        //
        // EndTime parameter
        //
        [Parameter(
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                ParameterSetName = "GetCounterSet",
                HelpMessageBaseName = "GetEventResources")]
        public DateTime EndTime
        {
            get { return _endTime; }

            set { _endTime = value; }
        }

        private DateTime _endTime = DateTime.MaxValue;

        //
        // Counter parameter
        //
        [Parameter(
                Mandatory = false,
                ParameterSetName = "GetCounterSet",
                ValueFromPipeline = false,
                HelpMessageBaseName = "GetEventResources")]
        [AllowEmptyCollection]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.GetCounterCommand.ListSet",
                            Justification = "A string[] is required here because that is the type Powershell supports")]
        public string[] Counter
        {
            get { return _counter; }

            set { _counter = value; }
        }

        private string[] _counter = Array.Empty<string>();

        //
        // Summary switch
        //
        [Parameter(ParameterSetName = "SummarySet")]
        public SwitchParameter Summary
        {
            get { return _summary; }

            set { _summary = value; }
        }

        private SwitchParameter _summary;

        //
        // MaxSamples parameter
        //
        private const Int64 KEEP_ON_SAMPLING = -1;
        [Parameter(
                ParameterSetName = "GetCounterSet",
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources")]
        [ValidateRange((Int64)1, Int64.MaxValue)]
        public Int64 MaxSamples
        {
            get { return _maxSamples; }

            set { _maxSamples = value; }
        }

        private Int64 _maxSamples = KEEP_ON_SAMPLING;

        private ResourceManager _resourceMgr = null;

        private PdhHelper _pdhHelper = null;

        private bool _stopping = false;

        //
        // AccumulatePipelineFileNames() accumulates counter file paths in the pipeline scenario:
        // we do not want to construct a Pdh query until all the file names are supplied.
        //
        private void AccumulatePipelineFileNames()
        {
            _accumulatedFileNames.AddRange(_path);
        }

        //
        // BeginProcessing() is invoked once per pipeline
        //
        protected override void BeginProcessing()
        {

#if CORECLR
            if (Platform.IsIoT)
            {
                // IoT does not have the '$env:windir\System32\pdh.dll' assembly which is required by this cmdlet.
                throw new PlatformNotSupportedException();
            }

            // PowerShell 7 requires at least Windows 7,
            // so no version test is needed
            _pdhHelper = new PdhHelper(false);
#else
            _pdhHelper = new PdhHelper(System.Environment.OSVersion.Version.Major < 6);
#endif
            _resourceMgr = Microsoft.PowerShell.Commands.Diagnostics.Common.CommonUtilities.GetResourceManager();
        }

        //
        // EndProcessing() is invoked once per pipeline
        //
        protected override void EndProcessing()
        {
            //
            // Resolve and validate the Path argument: present for all parametersets.
            //
            if (!ResolveFilePaths())
            {
                return;
            }

            ValidateFilePaths();

            switch (ParameterSetName)
            {
                case "ListSetSet":
                    ProcessListSet();
                    break;

                case "GetCounterSet":
                    ProcessGetCounter();
                    break;

                case "SummarySet":
                    ProcessSummary();
                    break;

                default:
                    Debug.Assert(false, $"Invalid parameter set name: {ParameterSetName}");
                    break;
            }

            _pdhHelper.Dispose();
        }

        //
        // Handle Control-C
        //
        protected override void StopProcessing()
        {
            _stopping = true;
            _pdhHelper.Dispose();
        }

        //
        // ProcessRecord() override.
        // This is the main entry point for the cmdlet.
        //
        protected override void ProcessRecord()
        {
            AccumulatePipelineFileNames();
        }

        //
        // ProcessSummary().
        // Does the work to process Summary parameter set.
        //
        private void ProcessSummary()
        {
            uint res = _pdhHelper.ConnectToDataSource(_resolvedPaths);
            if (res != 0)
            {
                ReportPdhError(res, true);
                return;
            }

            CounterFileInfo summaryObj;
            res = _pdhHelper.GetFilesSummary(out summaryObj);

            if (res != 0)
            {
                ReportPdhError(res, true);
                return;
            }

            WriteObject(summaryObj);
        }

        //
        // ProcessListSet().
        // Does the work to process ListSet parameter set.
        //
        private void ProcessListSet()
        {
            uint res = _pdhHelper.ConnectToDataSource(_resolvedPaths);
            if (res != 0)
            {
                ReportPdhError(res, true);
                return;
            }

            StringCollection machineNames = new StringCollection();
            res = _pdhHelper.EnumBlgFilesMachines(ref machineNames);
            if (res != 0)
            {
                ReportPdhError(res, true);
                return;
            }

            foreach (string machine in machineNames)
            {
                StringCollection counterSets = new StringCollection();
                res = _pdhHelper.EnumObjects(machine, ref counterSets);
                if (res != 0)
                {
                    return;
                }

                StringCollection validPaths = new StringCollection();

                foreach (string pattern in _listSet)
                {
                    bool bMatched = false;

                    WildcardPattern wildLogPattern = new WildcardPattern(pattern, WildcardOptions.IgnoreCase);

                    foreach (string counterSet in counterSets)
                    {
                        if (!wildLogPattern.IsMatch(counterSet))
                        {
                            continue;
                        }

                        StringCollection counterSetCounters = new StringCollection();
                        StringCollection counterSetInstances = new StringCollection();

                        res = _pdhHelper.EnumObjectItems(machine, counterSet, ref counterSetCounters, ref counterSetInstances);
                        if (res != 0)
                        {
                            ReportPdhError(res, false);
                            continue;
                        }

                        string[] instanceArray = new string[counterSetInstances.Count];
                        int i = 0;
                        foreach (string instance in counterSetInstances)
                        {
                            instanceArray[i++] = instance;
                        }

                        Dictionary<string, string[]> counterInstanceMapping = new Dictionary<string, string[]>();
                        foreach (string counter in counterSetCounters)
                        {
                            counterInstanceMapping.Add(counter, instanceArray);
                        }

                        PerformanceCounterCategoryType categoryType = PerformanceCounterCategoryType.Unknown;
                        if (counterSetInstances.Count > 1)
                        {
                            categoryType = PerformanceCounterCategoryType.MultiInstance;
                        }
                        else // if (counterSetInstances.Count == 1) //???
                        {
                            categoryType = PerformanceCounterCategoryType.SingleInstance;
                        }

                        string setHelp = _pdhHelper.GetCounterSetHelp(machine, counterSet);

                        CounterSet setObj = new CounterSet(counterSet, machine, categoryType, setHelp, ref counterInstanceMapping);
                        WriteObject(setObj);
                        bMatched = true;
                    }

                    if (!bMatched)
                    {
                        string msg = _resourceMgr.GetString("NoMatchingCounterSetsInFile");
                        Exception exc = new Exception(string.Format(CultureInfo.InvariantCulture, msg,
                        CommonUtilities.StringArrayToString(_resolvedPaths),
                        pattern));
                        WriteError(new ErrorRecord(exc, "NoMatchingCounterSetsInFile", ErrorCategory.ObjectNotFound, null));
                    }
                }
            }
        }

        //
        // ProcessGetCounter()
        // Does the work to process GetCounterSet parameter set.
        //
        private void ProcessGetCounter()
        {
            // Validate StartTime-EndTime, if present
            if (_startTime != DateTime.MinValue || _endTime != DateTime.MaxValue)
            {
                if (_startTime >= _endTime)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterInvalidDateRange"));
                    Exception exc = new Exception(msg);
                    ThrowTerminatingError(new ErrorRecord(exc, "CounterInvalidDateRange", ErrorCategory.InvalidArgument, null));
                    return;
                }
            }

            uint res = _pdhHelper.ConnectToDataSource(_resolvedPaths);
            if (res != 0)
            {
                ReportPdhError(res, true);
                return;
            }

            StringCollection validPaths = new StringCollection();
            if (_counter.Length > 0)
            {
                foreach (string path in _counter)
                {
                    StringCollection expandedPaths;
                    res = _pdhHelper.ExpandWildCardPath(path, out expandedPaths);
                    if (res != 0)
                    {
                        WriteDebug(path);
                        ReportPdhError(res, false);
                        continue;
                    }

                    foreach (string expandedPath in expandedPaths)
                    {
                        if (!_pdhHelper.IsPathValid(expandedPath))
                        {
                            string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterPathIsInvalid"), path);
                            Exception exc = new Exception(msg);
                            WriteError(new ErrorRecord(exc, "CounterPathIsInvalid", ErrorCategory.InvalidResult, null));

                            continue;
                        }

                        validPaths.Add(expandedPath);
                    }
                }

                if (validPaths.Count == 0)
                {
                    return;
                }
            }
            else
            {
                res = _pdhHelper.GetValidPathsFromFiles(ref validPaths);
                if (res != 0)
                {
                    ReportPdhError(res, false);
                }
            }

            if (validPaths.Count == 0)
            {
                string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterPathsInFilesInvalid"));
                Exception exc = new Exception(msg);
                ThrowTerminatingError(new ErrorRecord(exc, "CounterPathsInFilesInvalid", ErrorCategory.InvalidResult, null));
            }

            res = _pdhHelper.OpenQuery();
            if (res != 0)
            {
                ReportPdhError(res, false);
            }

            if (_startTime != DateTime.MinValue || _endTime != DateTime.MaxValue)
            {
                res = _pdhHelper.SetQueryTimeRange(_startTime, _endTime);
                if (res != 0)
                {
                    ReportPdhError(res, true);
                }
            }

            res = _pdhHelper.AddCounters(ref validPaths, true);
            if (res != 0)
            {
                ReportPdhError(res, true);
            }

            PerformanceCounterSampleSet nextSet;

            uint samplesRead = 0;

            while (!_stopping)
            {
                res = _pdhHelper.ReadNextSet(out nextSet, false);
                if (res == PdhResults.PDH_NO_MORE_DATA)
                {
                    break;
                }

                if (res != 0 && res != PdhResults.PDH_INVALID_DATA)
                {
                    ReportPdhError(res, false);
                    continue;
                }

                //
                // Display data
                //
                WriteSampleSetObject(nextSet, (samplesRead == 0));

                samplesRead++;

                if (_maxSamples != KEEP_ON_SAMPLING && samplesRead >= _maxSamples)
                {
                    break;
                }
            }
        }

        //
        // ValidateFilePaths() helper.
        // Validates the _resolvedPaths: present for all parametersets.
        // We cannot have more than 32 blg files, or more than one CSV or TSC file.
        // Files have to all be of the same type (.blg, .csv, .tsv).
        //
        private void ValidateFilePaths()
        {
            Debug.Assert(_resolvedPaths.Count > 0);

            string firstExt = System.IO.Path.GetExtension(_resolvedPaths[0]);
            foreach (string fileName in _resolvedPaths)
            {
                WriteVerbose(fileName);
                string curExtension = System.IO.Path.GetExtension(fileName);

                if (!curExtension.Equals(".blg", StringComparison.OrdinalIgnoreCase)
                    && !curExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
                    && !curExtension.Equals(".tsv", StringComparison.OrdinalIgnoreCase))
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterNotALogFile"), fileName);
                    Exception exc = new Exception(msg);
                    ThrowTerminatingError(new ErrorRecord(exc, "CounterNotALogFile", ErrorCategory.InvalidResult, null));
                    return;
                }

                if (!curExtension.Equals(firstExt, StringComparison.OrdinalIgnoreCase))
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterNoMixedLogTypes"), fileName);
                    Exception exc = new Exception(msg);
                    ThrowTerminatingError(new ErrorRecord(exc, "CounterNoMixedLogTypes", ErrorCategory.InvalidResult, null));
                    return;
                }
            }

            if (firstExt.Equals(".blg", StringComparison.OrdinalIgnoreCase))
            {
                if (_resolvedPaths.Count > 32)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("Counter32FileLimit"));
                    Exception exc = new Exception(msg);
                    ThrowTerminatingError(new ErrorRecord(exc, "Counter32FileLimit", ErrorCategory.InvalidResult, null));
                    return;
                }
            }
            else if (_resolvedPaths.Count > 1)
            {
                string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("Counter1FileLimit"));
                Exception exc = new Exception(msg);
                ThrowTerminatingError(new ErrorRecord(exc, "Counter1FileLimit", ErrorCategory.InvalidResult, null));
                return;
            }
        }

        //
        // ResolveFilePath helper.
        // Returns a string collection of resolved file paths.
        // Writes non-terminating errors for invalid paths
        // and returns an empty collection.
        //
        private bool ResolveFilePaths()
        {
            StringCollection retColl = new StringCollection();

            foreach (string origPath in _accumulatedFileNames)
            {
                Collection<PathInfo> resolvedPathSubset = null;
                try
                {
                    resolvedPathSubset = SessionState.Path.GetResolvedPSPathFromPSPath(origPath);
                }
                catch (PSNotSupportedException notSupported)
                {
                    WriteError(new ErrorRecord(notSupported, string.Empty, ErrorCategory.ObjectNotFound, origPath));
                    continue;
                }
                catch (System.Management.Automation.DriveNotFoundException driveNotFound)
                {
                    WriteError(new ErrorRecord(driveNotFound, string.Empty, ErrorCategory.ObjectNotFound, origPath));
                    continue;
                }
                catch (ProviderNotFoundException providerNotFound)
                {
                    WriteError(new ErrorRecord(providerNotFound, string.Empty, ErrorCategory.ObjectNotFound, origPath));
                    continue;
                }
                catch (ItemNotFoundException pathNotFound)
                {
                    WriteError(new ErrorRecord(pathNotFound, string.Empty, ErrorCategory.ObjectNotFound, origPath));
                    continue;
                }
                catch (Exception exc)
                {
                    WriteError(new ErrorRecord(exc, string.Empty, ErrorCategory.ObjectNotFound, origPath));
                    continue;
                }

                foreach (PathInfo pi in resolvedPathSubset)
                {
                    //
                    // Check the provider: only FileSystem provider paths are acceptable.
                    //
                    if (pi.Provider.Name != "FileSystem")
                    {
                        string msg = _resourceMgr.GetString("NotAFileSystemPath");
                        Exception exc = new Exception(string.Format(CultureInfo.InvariantCulture, msg, origPath));
                        WriteError(new ErrorRecord(exc, "NotAFileSystemPath", ErrorCategory.InvalidArgument, origPath));
                        continue;
                    }

                    _resolvedPaths.Add(pi.ProviderPath.ToLowerInvariant());
                }
            }

            return (_resolvedPaths.Count > 0);
        }

        private void ReportPdhError(uint res, bool bTerminate)
        {
            string msg;
            uint formatRes = CommonUtilities.FormatMessageFromModule(res, "pdh.dll", out msg);
            if (formatRes != 0)
            {
                msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterApiError"), res);
            }

            Exception exc = new Exception(msg);
            if (bTerminate)
            {
                ThrowTerminatingError(new ErrorRecord(exc, "CounterApiError", ErrorCategory.InvalidResult, null));
            }
            else
            {
                WriteError(new ErrorRecord(exc, "CounterApiError", ErrorCategory.InvalidResult, null));
            }
        }

        //
        // WriteSampleSetObject() helper.
        // In addition to writing the PerformanceCounterSampleSet object,
        // it writes a single error if one of the samples has an invalid (non-zero) status.
        // The only exception is the first set, where we allow for the formatted value to be 0 -
        // this is expected for CSV and TSV files.

        private void WriteSampleSetObject(PerformanceCounterSampleSet set, bool firstSet)
        {
            if (!firstSet)
            {
                foreach (PerformanceCounterSample sample in set.CounterSamples)
                {
                    if (sample.Status != 0)
                    {
                        string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterSampleDataInvalid"));
                        Exception exc = new Exception(msg);
                        WriteError(new ErrorRecord(exc, "CounterApiError", ErrorCategory.InvalidResult, null));
                        break;
                    }
                }
            }

            WriteObject(set);
        }
    }
}
