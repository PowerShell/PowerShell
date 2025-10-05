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
    [Cmdlet(VerbsData.Export, "Counter", DefaultParameterSetName = "ExportCounterSet", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=138337")]
    public sealed class ExportCounterCommand : PSCmdlet
    {
        //
        // Path parameter
        //
        [Parameter(
                Mandatory = true,
                Position = 0,
                ValueFromPipelineByPropertyName = true,
                HelpMessageBaseName = "GetEventResources")]
        [Alias("PSPath")]

        public string Path
        {
            get { return _path; }

            set { _path = value; }
        }

        private string _path;
        private string _resolvedPath;

        //
        // Format parameter.
        // Valid strings are "blg", "csv", "tsv" (case-insensitive).
        //
        [Parameter(
                Mandatory = false,
                ValueFromPipeline = false,
                ValueFromPipelineByPropertyName = false,
                HelpMessageBaseName = "GetEventResources")]
        [ValidateNotNull]
        [ValidateSet("blg", "csv", "tsv")]
        public string FileFormat
        {
            get { return _format; }

            set { _format = value; }
        }

        private string _format = "blg";

        //
        // MaxSize parameter
        // Maximum output file size, in megabytes.
        //
        [Parameter(
                HelpMessageBaseName = "GetEventResources")]
        public UInt32 MaxSize
        {
            get { return _maxSize; }

            set { _maxSize = value; }
        }

        private UInt32 _maxSize = 0;

        //
        // InputObject parameter
        //
        [Parameter(
                Mandatory = true,
                ValueFromPipeline = true,
                ValueFromPipelineByPropertyName = true,
                HelpMessageBaseName = "GetEventResources")]

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                            Scope = "member",
                            Target = "Microsoft.PowerShell.Commands.ExportCounterCommand.InputObject",
                            Justification = "A PerformanceCounterSampleSet[] is required here because Powershell supports arrays natively.")]
        public PerformanceCounterSampleSet[] InputObject
        {
            get { return _counterSampleSets; }

            set { _counterSampleSets = value; }
        }

        private PerformanceCounterSampleSet[] _counterSampleSets = new PerformanceCounterSampleSet[0];

        //
        // Force switch
        //
        [Parameter(
                HelpMessageBaseName = "GetEventResources")]
        public SwitchParameter Force
        {
            get { return _force; }

            set { _force = value; }
        }

        private SwitchParameter _force;

        //
        // Circular switch
        //
        [Parameter(
                HelpMessageBaseName = "GetEventResources")]
        public SwitchParameter Circular
        {
            get { return _circular; }

            set { _circular = value; }
        }

        private SwitchParameter _circular;

        private ResourceManager _resourceMgr = null;

        private PdhHelper _pdhHelper = null;

        private bool _stopping = false;

        private bool _queryInitialized = false;

        private PdhLogFileType _outputFormat = PdhLogFileType.PDH_LOG_TYPE_BINARY;

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
            //
            // Determine the OS version: this cmdlet requires Windows 7
            // because it uses new Pdh functionality.
            //
            Version osVersion = System.Environment.OSVersion.Version;
            if (osVersion.Major < 6 ||
                (osVersion.Major == 6 && osVersion.Minor < 1))
            {
                string msg = _resourceMgr.GetString("ExportCtrWin7Required");
                Exception exc = new Exception(msg);
                ThrowTerminatingError(new ErrorRecord(exc, "ExportCtrWin7Required", ErrorCategory.NotImplemented, null));
            }

            _pdhHelper = new PdhHelper(osVersion.Major < 6);
#endif
            _resourceMgr = Microsoft.PowerShell.Commands.Diagnostics.Common.CommonUtilities.GetResourceManager();

            //
            // Set output format (log file type)
            //
            SetOutputFormat();

            if (Circular && _maxSize == 0)
            {
                string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterCircularNoMaxSize"));
                Exception exc = new Exception(msg);
                WriteError(new ErrorRecord(exc, "CounterCircularNoMaxSize", ErrorCategory.InvalidResult, null));
            }

            uint res = _pdhHelper.ConnectToDataSource();
            if (res != 0)
            {
                ReportPdhError(res, true);
            }

            res = _pdhHelper.OpenQuery();
            if (res != 0)
            {
                ReportPdhError(res, true);
            }
        }

        //
        // EndProcessing() is invoked once per pipeline
        //
        protected override void EndProcessing()
        {
            _pdhHelper.Dispose();
        }

        ///
        /// Handle Control-C
        ///
        protected override void StopProcessing()
        {
            _stopping = true;
            _pdhHelper.Dispose();
        }

        //
        // ProcessRecord() override.
        // This is the main entry point for the cmdlet.
        // When counter data comes from the pipeline, this gets invoked for each pipelined object.
        // When it's passed in as an argument, ProcessRecord() is called once for the entire _counterSampleSets array.
        //
        protected override void ProcessRecord()
        {
            Debug.Assert(_counterSampleSets.Length != 0 && _counterSampleSets[0] != null);

            ResolvePath();

            uint res = 0;

            if (!_queryInitialized)
            {
                if (_format.ToLowerInvariant().Equals("blg"))
                {
                    res = _pdhHelper.AddRelogCounters(_counterSampleSets[0]);
                }
                else
                {
                    res = _pdhHelper.AddRelogCountersPreservingPaths(_counterSampleSets[0]);
                }

                if (res != 0)
                {
                    ReportPdhError(res, true);
                }

                res = _pdhHelper.OpenLogForWriting(_resolvedPath, _outputFormat, Force, _maxSize * 1024 * 1024, Circular, null);
                if (res == PdhResults.PDH_FILE_ALREADY_EXISTS)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterFileExists"), _resolvedPath);
                    Exception exc = new Exception(msg);
                    ThrowTerminatingError(new ErrorRecord(exc, "CounterFileExists", ErrorCategory.InvalidResult, null));
                }
                else if (res == PdhResults.PDH_LOG_FILE_CREATE_ERROR)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("FileCreateFailed"), _resolvedPath);
                    Exception exc = new Exception(msg);
                    ThrowTerminatingError(new ErrorRecord(exc, "FileCreateFailed", ErrorCategory.InvalidResult, null));
                }
                else if (res == PdhResults.PDH_LOG_FILE_OPEN_ERROR)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("FileOpenFailed"), _resolvedPath);
                    Exception exc = new Exception(msg);
                    ThrowTerminatingError(new ErrorRecord(exc, "FileOpenFailed", ErrorCategory.InvalidResult, null));
                }
                else if (res != 0)
                {
                    ReportPdhError(res, true);
                }

                _queryInitialized = true;
            }

            foreach (PerformanceCounterSampleSet set in _counterSampleSets)
            {
                _pdhHelper.ResetRelogValues();

                foreach (PerformanceCounterSample sample in set.CounterSamples)
                {
                    bool bUnknownKey = false;
                    res = _pdhHelper.SetCounterValue(sample, out bUnknownKey);
                    if (bUnknownKey)
                    {
                        string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterExportSampleNotInInitialSet"), sample.Path, _resolvedPath);
                        Exception exc = new Exception(msg);
                        WriteError(new ErrorRecord(exc, "CounterExportSampleNotInInitialSet", ErrorCategory.InvalidResult, null));
                    }
                    else if (res != 0)
                    {
                        ReportPdhError(res, true);
                    }
                }

                res = _pdhHelper.WriteRelogSample(set.Timestamp);
                if (res != 0)
                {
                    ReportPdhError(res, true);
                }

                if (_stopping)
                {
                    break;
                }
            }
        }

        // Determines Log File Type based on FileFormat parameter
        //
        private void SetOutputFormat()
        {
            switch (_format.ToLowerInvariant())
            {
                case "csv":
                    _outputFormat = PdhLogFileType.PDH_LOG_TYPE_CSV;
                    break;
                case "tsv":
                    _outputFormat = PdhLogFileType.PDH_LOG_TYPE_TSV;
                    break;
                default:  // By default file format is blg
                    _outputFormat = PdhLogFileType.PDH_LOG_TYPE_BINARY;
                    break;
            }
        }

        private void ResolvePath()
        {
            try
            {
                Collection<PathInfo> result = null;
                result = SessionState.Path.GetResolvedPSPathFromPSPath(_path);
                if (result.Count > 1)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("ExportDestPathAmbiguous"), _path);
                    Exception exc = new Exception(msg);
                    ThrowTerminatingError(new ErrorRecord(exc, "ExportDestPathAmbiguous", ErrorCategory.InvalidArgument, null));
                }

                foreach (PathInfo currentPath in result)
                {
                    _resolvedPath = currentPath.ProviderPath;
                }
            }
            catch (ItemNotFoundException pathNotFound)
            {
                //
                // This is an expected condition - we will be creating a new file
                //
                _resolvedPath = pathNotFound.ItemName;
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
    }
}
