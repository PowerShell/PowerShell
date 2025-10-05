// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Resources;
using System.Threading;
using Microsoft.Powershell.Commands.GetCounter.PdhNative;
using Microsoft.PowerShell.Commands.Diagnostics.Common;
using Microsoft.PowerShell.Commands.GetCounter;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Class that implements the Get-Counter cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Counter", DefaultParameterSetName = "GetCounterSet", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2109647")]
    public sealed class GetCounterCommand : PSCmdlet
    {
        //
        // ListSet parameter
        //
        [Parameter(
                Position = 0,
                Mandatory = true,
                ParameterSetName = "ListSetSet",
                ValueFromPipeline = true,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources")]
        [AllowEmptyCollection]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.GetCounterCommand.ListSet",
                            Justification = "A string[] is required here because that is the type Powershell supports")]
        public string[] ListSet { get; set; } = { "*" };

        //
        // Counter parameter
        //
        [Parameter(
                Position = 0,
                ParameterSetName = "GetCounterSet",
                ValueFromPipeline = true,
                ValueFromPipelineByPropertyName = true,
                HelpMessageBaseName = "GetEventResources")]
        [AllowEmptyCollection]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.GetCounterCommand.ListSet",
                            Justification = "A string[] is required here because that is the type Powershell supports")]
        public string[] Counter
        {
            get
            {
                return _counter;
            }

            set
            {
                _counter = value;
                _defaultCounters = false;
            }
        }

        private string[] _counter = {@"\network interface(*)\bytes total/sec",
                                 @"\processor(_total)\% processor time",
                                 @"\memory\% committed bytes in use",
                                 @"\memory\cache faults/sec",
                                 @"\physicaldisk(_total)\% disk time",
                                 @"\physicaldisk(_total)\current disk queue length"};

        private bool _defaultCounters = true;

        private readonly List<string> _accumulatedCounters = new();

        //
        // SampleInterval parameter.
        // Defaults to 1 second.
        //
        [Parameter(
                ParameterSetName = "GetCounterSet",
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources")]
        [ValidateRange((int)1, int.MaxValue)]
        public int SampleInterval { get; set; } = 1;

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
            get
            {
                return _maxSamples;
            }

            set
            {
                _maxSamples = value;
                _maxSamplesSpecified = true;
            }
        }

        private Int64 _maxSamples = 1;
        private bool _maxSamplesSpecified = false;

        //
        // Continuous switch
        //
        [Parameter(ParameterSetName = "GetCounterSet")]
        public SwitchParameter Continuous
        {
            get { return _continuous; }

            set { _continuous = value; }
        }

        private bool _continuous = false;

        //
        // ComputerName parameter
        //
        [Parameter(
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources",
                HelpMessageResourceId = "ComputerNameParamHelp")]
        [ValidateNotNull]
        [AllowEmptyCollection]
        [Alias("Cn")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.GetCounterCommand.ComputerName",
                            Justification = "A string[] is required here because that is the type Powershell supports")]
        public string[] ComputerName { get; set; } = Array.Empty<string>();

        private ResourceManager _resourceMgr = null;

        private PdhHelper _pdhHelper = null;

        private readonly EventWaitHandle _cancelEventArrived = new(false, EventResetMode.ManualReset);

        // Culture identifier(s)
        private const string FrenchCultureId = "fr-FR";
        // The localized Pdh resource strings might use Unicode characters that are different from
        // what the user can type with the keyboard to represent a special character.
        //
        // e.g. the apostrophe in French UI culture: it's [char]39 from keyboard, but it's [char]8217
        // in the resource strings.
        //
        // With this dictionary, we can add special mapping if we find other special cases in the future.
        private readonly Dictionary<string, List<Tuple<char, char>>> _cultureAndSpecialCharacterMap =
            new()
                {
                   {
                       FrenchCultureId, new List<Tuple<char, char>>()
                                            {
                                                // 'APOSTROPHE' to 'RIGHT SINGLE QUOTATION MARK'
                                                new Tuple<char, char>((char)0x0027, (char)0x2019),
                                                // 'MODIFIER LETTER APOSTROPHE' to 'RIGHT SINGLE QUOTATION MARK'
                                                new Tuple<char, char>((char)0x02BC, (char)0x2019),
                                                // 'HEAVY SINGLE COMMA QUOTATION MARK ORNAMENT' to 'RIGHT SINGLE QUOTATION MARK'
                                                new Tuple<char, char>((char)0x275C, (char)0x2019),
                                            }
                   }
                };

        //
        // BeginProcessing() is invoked once per pipeline
        //
        protected override void BeginProcessing()
        {
            if (Platform.IsIoT)
            {
                // IoT does not have the '$env:windir\System32\pdh.dll' assembly which is required by this cmdlet.
                throw new PlatformNotSupportedException();
            }

            _pdhHelper = new PdhHelper();
            _resourceMgr = Microsoft.PowerShell.Commands.Diagnostics.Common.CommonUtilities.GetResourceManager();

            uint res = _pdhHelper.ConnectToDataSource();
            if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
            {
                ReportPdhError(res, true);
                return;
            }

            if (Continuous && _maxSamplesSpecified)
            {
                Exception exc = new(string.Format(CultureInfo.CurrentCulture, _resourceMgr.GetString("CounterContinuousOrMaxSamples")));
                ThrowTerminatingError(new ErrorRecord(exc, "CounterContinuousOrMaxSamples", ErrorCategory.InvalidArgument, null));
            }
        }

        //
        // EndProcessing() is invoked once per pipeline
        //
        protected override void EndProcessing()
        {
            if (ParameterSetName == "GetCounterSet")
            {
                ProcessGetCounter();
            }

            _pdhHelper.Dispose();
        }

        //
        // Handle Control-C
        //
        protected override void StopProcessing()
        {
            _cancelEventArrived.Set();
            _pdhHelper.Dispose();
        }

        //
        // ProcessRecord() override.
        // This is the main entry point for the cmdlet.
        //
        protected override void ProcessRecord()
        {
            try
            {
                switch (ParameterSetName)
                {
                    case "ListSetSet":
                        ProcessListSet();
                        break;

                    case "GetCounterSet":
                        AccumulatePipelineCounters();
                        break;

                    default:
                        Debug.Fail(string.Create(CultureInfo.InvariantCulture, $"Invalid parameter set name: {ParameterSetName}"));
                        break;
                }
            }
            catch (Exception exc)
            {
                ThrowTerminatingError(new ErrorRecord(exc, "CounterApiError", ErrorCategory.InvalidResult, null));
            }
        }

        //
        // AccumulatePipelineCounters() accumulates counter paths in the pipeline scenario:
        // we do not want to start sampling until all the counters are supplied.
        //
        private void AccumulatePipelineCounters()
        {
            _accumulatedCounters.AddRange(_counter);
        }

        //
        // ProcessListSet() does the work to process ListSet parameter set.
        //
        private void ProcessListSet()
        {
            if (ComputerName.Length == 0)
            {
                ProcessListSetPerMachine(null);
            }
            else
                foreach (string machine in ComputerName)
                {
                    ProcessListSetPerMachine(machine);
                }
        }

        //
        // ProcessListSetPerMachine() helper lists counter sets on a machine.
        // NOTE: machine argument should be NULL for the local machine
        //
        private void ProcessListSetPerMachine(string machine)
        {
            StringCollection counterSets = new();
            uint res = _pdhHelper.EnumObjects(machine, ref counterSets);
            if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
            {
                // add an error message
                string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("NoCounterSetsOnComputer"), machine, res);
                Exception exc = new(msg);
                WriteError(new ErrorRecord(exc, "NoCounterSetsOnComputer", ErrorCategory.InvalidResult, machine));
                return;
            }

            CultureInfo culture = GetCurrentCulture();
            List<Tuple<char, char>> characterReplacementList = null;
            StringCollection validPaths = new();

            _cultureAndSpecialCharacterMap.TryGetValue(culture.Name, out characterReplacementList);

            foreach (string pattern in ListSet)
            {
                bool bMatched = false;
                string normalizedPattern = pattern;

                if (characterReplacementList != null)
                {
                    foreach (Tuple<char, char> pair in characterReplacementList)
                    {
                        normalizedPattern = normalizedPattern.Replace(pair.Item1, pair.Item2);
                    }
                }

                WildcardPattern wildLogPattern = new(normalizedPattern, WildcardOptions.IgnoreCase);

                foreach (string counterSet in counterSets)
                {
                    if (!wildLogPattern.IsMatch(counterSet))
                    {
                        continue;
                    }

                    StringCollection counterSetCounters = new();
                    StringCollection counterSetInstances = new();

                    res = _pdhHelper.EnumObjectItems(machine, counterSet, ref counterSetCounters, ref counterSetInstances);
                    if (res == PdhResults.PDH_ACCESS_DENIED)
                    {
                        string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterSetEnumAccessDenied"), counterSet);
                        Exception exc = new(msg);
                        WriteError(new ErrorRecord(exc, "CounterSetEnumAccessDenied", ErrorCategory.InvalidResult, null));
                        continue;
                    }
                    else if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
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

                    //
                    // Special case: no instances present: change to * to create a valid paths
                    //
                    if (instanceArray.Length == 1 &&
                        instanceArray[0].Length == 0)
                    {
                        instanceArray[0] = "*";
                    }

                    Dictionary<string, string[]> counterInstanceMapping = new();
                    foreach (string counter in counterSetCounters)
                    {
                        counterInstanceMapping.TryAdd(counter, instanceArray);
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

                    CounterSet setObj = new(counterSet, machine, categoryType, setHelp, ref counterInstanceMapping);
                    WriteObject(setObj);
                    bMatched = true;
                }

                if (!bMatched)
                {
                    string msg = _resourceMgr.GetString("NoMatchingCounterSetsFound");
                    Exception exc = new(string.Format(CultureInfo.InvariantCulture, msg,
                      machine ?? "localhost", normalizedPattern));
                    WriteError(new ErrorRecord(exc, "NoMatchingCounterSetsFound", ErrorCategory.ObjectNotFound, null));
                }
            }
        }

        //
        // ProcessGetCounter()
        // Does the work to process GetCounterSet parameter set.
        //
        private void ProcessGetCounter()
        {
            // 1. Combine machine names with paths, if needed, to construct full paths
            // 2. Translate default paths into current locale
            // 3. Expand wildcards and validate the paths and write errors for any invalid paths
            // 4. OpenQuery/ AddCounters
            // 5. Skip the first reading

            CultureInfo culture = GetCurrentCulture();
            List<Tuple<char, char>> characterReplacementList = null;
            List<string> paths = CombineMachinesAndCounterPaths();

            if (!_defaultCounters)
            {
                _cultureAndSpecialCharacterMap.TryGetValue(culture.Name, out characterReplacementList);
            }

            StringCollection allExpandedPaths = new();
            uint res;
            foreach (string path in paths)
            {
                string localizedPath = path;
                if (_defaultCounters)
                {
                    res = _pdhHelper.TranslateLocalCounterPath(path, out localizedPath);
                    if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
                    {
                        string msg = string.Format(CultureInfo.CurrentCulture, _resourceMgr.GetString("CounterPathTranslationFailed"), res);
                        Exception exc = new(msg);
                        WriteError(new ErrorRecord(exc, "CounterPathTranslationFailed", ErrorCategory.InvalidResult, null));

                        localizedPath = path;
                    }
                }
                else if (characterReplacementList != null)
                {
                    foreach (Tuple<char, char> pair in characterReplacementList)
                    {
                        localizedPath = localizedPath.Replace(pair.Item1, pair.Item2);
                    }
                }

                StringCollection expandedPaths;
                res = _pdhHelper.ExpandWildCardPath(localizedPath, out expandedPaths);
                if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
                {
                    WriteDebug("Could not expand path " + localizedPath);
                    ReportPdhError(res, false);
                    continue;
                }

                foreach (string expandedPath in expandedPaths)
                {
                    if (!_pdhHelper.IsPathValid(expandedPath))
                    {
                        string msg = string.Format(CultureInfo.CurrentCulture, _resourceMgr.GetString("CounterPathIsInvalid"), localizedPath);
                        Exception exc = new(msg);
                        WriteError(new ErrorRecord(exc, "CounterPathIsInvalid", ErrorCategory.InvalidResult, null));

                        continue;
                    }

                    allExpandedPaths.Add(expandedPath);
                }
            }

            if (allExpandedPaths.Count == 0)
            {
                return;
            }

            res = _pdhHelper.OpenQuery();
            if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
            {
                ReportPdhError(res, false);
            }

            res = _pdhHelper.AddCounters(ref allExpandedPaths, true);
            if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
            {
                ReportPdhError(res, true);

                return;
            }

            PerformanceCounterSampleSet nextSet;

            bool bSkip = true;
            uint sampleReads = 0;

            if (Continuous)
            {
                _maxSamples = KEEP_ON_SAMPLING;
            }

            while (true)
            {
                // read the first set just to get the initial values
                res = _pdhHelper.ReadNextSet(out nextSet, bSkip);

                if (res == PdhResults.PDH_CSTATUS_VALID_DATA)
                {
                    // Display valid data
                    if (!bSkip)
                    {
                        WriteSampleSetObject(nextSet);
                        sampleReads++;
                    }

                    // Don't need to skip anymore
                    bSkip = false;
                }
                else if (res == PdhResults.PDH_NO_DATA || res == PdhResults.PDH_INVALID_DATA)
                {
                    // The provider may not be running.
                    // We should keep on trying - but skip the next valid reading.

                    ReportPdhError(res, false);

                    bSkip = true;

                    // Count this failed attempt as a sample:
                    sampleReads++;
                }
                else
                {
                    // Unexpected error, return
                    ReportPdhError(res, true);
                    return;
                }

                if (_maxSamples != KEEP_ON_SAMPLING && sampleReads >= _maxSamples)
                {
                    break;
                }

                bool cancelled = _cancelEventArrived.WaitOne((int)SampleInterval * 1000, true);
                if (cancelled)
                {
                    break;
                }
            }
        }

        private void ReportPdhError(uint res, bool bTerminate)
        {
            string msg;
            uint formatRes = CommonUtilities.FormatMessageFromModule(res, "pdh.dll", out msg);
            if (formatRes != 0)
            {
                msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterApiError"), res);
            }

            Exception exc = new(msg);
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
        // CombineMachinesAndCounterPaths() helper.
        // For paths that do not contain machine names, creates a path for each machine in machineNames.
        // Paths already containing a machine name will be preserved.
        //
        private List<string> CombineMachinesAndCounterPaths()
        {
            List<string> retColl = new();

            if (ComputerName.Length == 0)
            {
                retColl.AddRange(_accumulatedCounters);
                return retColl;
            }

            foreach (string path in _accumulatedCounters)
            {
                if (path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase)) // NOTE: can we do anything smarter here?
                {
                    retColl.Add(path);
                }
                else
                {
                    foreach (string machine in ComputerName)
                    {
                        string slashBeforePath = path.Length > 0 && path[0] == '\\' ? string.Empty : "\\";
                        if (machine.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
                        {
                            retColl.Add(machine + slashBeforePath + path);
                        }
                        else
                        {
                            retColl.Add("\\\\" + machine + slashBeforePath + path);
                        }
                    }
                }
            }

            return retColl;
        }

        //
        // WriteSampleSetObject() helper.
        // In addition to writing the PerformanceCounterSampleSet object,
        // it writes a single error if one of the samples has an invalid (non-zero) status.
        //
        private void WriteSampleSetObject(PerformanceCounterSampleSet set)
        {
            foreach (PerformanceCounterSample sample in set.CounterSamples)
            {
                if (sample.Status != 0)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterSampleDataInvalid"));
                    Exception exc = new(msg);
                    WriteError(new ErrorRecord(exc, "CounterApiError", ErrorCategory.InvalidResult, null));
                    break;
                }
            }

            WriteObject(set);
        }

        private static CultureInfo GetCurrentCulture()
        {
            return Thread.CurrentThread.CurrentUICulture;
        }
    }
}
