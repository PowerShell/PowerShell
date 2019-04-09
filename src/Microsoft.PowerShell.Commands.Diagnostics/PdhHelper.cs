// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.PowerShell.Commands.GetCounter;
using Microsoft.Win32;

namespace Microsoft.Powershell.Commands.GetCounter.PdhNative
{
    internal static class PdhResults
    {
        public const long PDH_CSTATUS_VALID_DATA = 0x0L;
        public const long PDH_CSTATUS_NEW_DATA = 0x1L;
        public const long PDH_CSTATUS_NO_MACHINE = 0x800007D0L;
        public const long PDH_CSTATUS_NO_INSTANCE = 0x800007D1L;
        public const long PDH_MORE_DATA = 0x800007D2L;
        public const long PDH_CSTATUS_ITEM_NOT_VALIDATED = 0x800007D3L;
        public const long PDH_RETRY = 0x800007D4L;
        public const long PDH_NO_DATA = 0x800007D5L;
        public const long PDH_CALC_NEGATIVE_DENOMINATOR = 0x800007D6L;
        public const long PDH_CALC_NEGATIVE_TIMEBASE = 0x800007D7L;
        public const long PDH_CALC_NEGATIVE_VALUE = 0x800007D8L;
        public const long PDH_DIALOG_CANCELLED = 0x800007D9L;
        public const long PDH_END_OF_LOG_FILE = 0x800007DAL;
        public const long PDH_ASYNC_QUERY_TIMEOUT = 0x800007DBL;
        public const long PDH_CANNOT_SET_DEFAULT_REALTIME_DATASOURCE = 0x800007DCL;
        public const long PDH_UNABLE_MAP_NAME_FILES = 0x80000BD5L;
        public const long PDH_PLA_VALIDATION_WARNING = 0x80000BF3L;
        public const long PDH_CSTATUS_NO_OBJECT = 0xC0000BB8L;
        public const long PDH_CSTATUS_NO_COUNTER = 0xC0000BB9L;
        public const long PDH_CSTATUS_INVALID_DATA = 0xC0000BBAL;
        public const long PDH_MEMORY_ALLOCATION_FAILURE = 0xC0000BBBL;
        public const long PDH_INVALID_HANDLE = 0xC0000BBCL;
        public const long PDH_INVALID_ARGUMENT = 0xC0000BBDL;
        public const long PDH_FUNCTION_NOT_FOUND = 0xC0000BBEL;
        public const long PDH_CSTATUS_NO_COUNTERNAME = 0xC0000BBFL;
        public const long PDH_CSTATUS_BAD_COUNTERNAME = 0xC0000BC0L;
        public const long PDH_INVALID_BUFFER = 0xC0000BC1L;
        public const long PDH_INSUFFICIENT_BUFFER = 0xC0000BC2L;
        public const long PDH_CANNOT_CONNECT_MACHINE = 0xC0000BC3L;
        public const long PDH_INVALID_PATH = 0xC0000BC4L;
        public const long PDH_INVALID_INSTANCE = 0xC0000BC5L;
        public const long PDH_INVALID_DATA = 0xC0000BC6L;
        public const long PDH_NO_DIALOG_DATA = 0xC0000BC7L;
        public const long PDH_CANNOT_READ_NAME_STRINGS = 0xC0000BC8L;
        public const long PDH_LOG_FILE_CREATE_ERROR = 0xC0000BC9L;
        public const long PDH_LOG_FILE_OPEN_ERROR = 0xC0000BCAL;
        public const long PDH_LOG_TYPE_NOT_FOUND = 0xC0000BCBL;
        public const long PDH_NO_MORE_DATA = 0xC0000BCCL;
        public const long PDH_ENTRY_NOT_IN_LOG_FILE = 0xC0000BCDL;
        public const long PDH_DATA_SOURCE_IS_LOG_FILE = 0xC0000BCEL;
        public const long PDH_DATA_SOURCE_IS_REAL_TIME = 0xC0000BCFL;
        public const long PDH_UNABLE_READ_LOG_HEADER = 0xC0000BD0L;
        public const long PDH_FILE_NOT_FOUND = 0xC0000BD1L;
        public const long PDH_FILE_ALREADY_EXISTS = 0xC0000BD2L;
        public const long PDH_NOT_IMPLEMENTED = 0xC0000BD3L;
        public const long PDH_STRING_NOT_FOUND = 0xC0000BD4L;
        public const long PDH_UNKNOWN_LOG_FORMAT = 0xC0000BD6L;
        public const long PDH_UNKNOWN_LOGSVC_COMMAND = 0xC0000BD7L;
        public const long PDH_LOGSVC_QUERY_NOT_FOUND = 0xC0000BD8L;
        public const long PDH_LOGSVC_NOT_OPENED = 0xC0000BD9L;
        public const long PDH_WBEM_ERROR = 0xC0000BDAL;
        public const long PDH_ACCESS_DENIED = 0xC0000BDBL;
        public const long PDH_LOG_FILE_TOO_SMALL = 0xC0000BDCL;
        public const long PDH_INVALID_DATASOURCE = 0xC0000BDDL;
        public const long PDH_INVALID_SQLDB = 0xC0000BDEL;
        public const long PDH_NO_COUNTERS = 0xC0000BDFL;
        public const long PDH_SQL_ALLOC_FAILED = 0xC0000BE0L;
        public const long PDH_SQL_ALLOCCON_FAILED = 0xC0000BE1L;
        public const long PDH_SQL_EXEC_DIRECT_FAILED = 0xC0000BE2L;
        public const long PDH_SQL_FETCH_FAILED = 0xC0000BE3L;
        public const long PDH_SQL_ROWCOUNT_FAILED = 0xC0000BE4L;
        public const long PDH_SQL_MORE_RESULTS_FAILED = 0xC0000BE5L;
        public const long PDH_SQL_CONNECT_FAILED = 0xC0000BE6L;
        public const long PDH_SQL_BIND_FAILED = 0xC0000BE7L;
        public const long PDH_CANNOT_CONNECT_WMI_SERVER = 0xC0000BE8L;
        public const long PDH_PLA_COLLECTION_ALREADY_RUNNING = 0xC0000BE9L;
        public const long PDH_PLA_ERROR_SCHEDULE_OVERLAP = 0xC0000BEAL;
        public const long PDH_PLA_COLLECTION_NOT_FOUND = 0xC0000BEBL;
        public const long PDH_PLA_ERROR_SCHEDULE_ELAPSED = 0xC0000BECL;
        public const long PDH_PLA_ERROR_NOSTART = 0xC0000BEDL;
        public const long PDH_PLA_ERROR_ALREADY_EXISTS = 0xC0000BEEL;
        public const long PDH_PLA_ERROR_TYPE_MISMATCH = 0xC0000BEFL;
        public const long PDH_PLA_ERROR_FILEPATH = 0xC0000BF0L;
        public const long PDH_PLA_SERVICE_ERROR = 0xC0000BF1L;
        public const long PDH_PLA_VALIDATION_ERROR = 0xC0000BF2L;
        public const long PDH_PLA_ERROR_NAME_TOO_LONG = 0xC0000BF4L;
        public const long PDH_INVALID_SQL_LOG_FORMAT = 0xC0000BF5L;
        public const long PDH_COUNTER_ALREADY_IN_QUERY = 0xC0000BF6L;
        public const long PDH_BINARY_LOG_CORRUPT = 0xC0000BF7L;
        public const long PDH_LOG_SAMPLE_TOO_SMALL = 0xC0000BF8L;
        public const long PDH_OS_LATER_VERSION = 0xC0000BF9L;
        public const long PDH_OS_EARLIER_VERSION = 0xC0000BFAL;
        public const long PDH_INCORRECT_APPEND_TIME = 0xC0000BFBL;
        public const long PDH_UNMATCHED_APPEND_COUNTER = 0xC0000BFCL;
        public const long PDH_SQL_ALTER_DETAIL_FAILED = 0xC0000BFDL;
        public const long PDH_QUERY_PERF_DATA_TIMEOUT = 0xC0000BFEL;
    }

    internal static class PerfDetail
    {
        public const uint PERF_DETAIL_NOVICE = 100;   // The uninformed can understand it
        public const uint PERF_DETAIL_ADVANCED = 200; // For the advanced user
        public const uint PERF_DETAIL_EXPERT = 300;   // For the expert user
        public const uint PERF_DETAIL_WIZARD = 400;   // For the system designer
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2, Size = 16)]
    internal struct SYSTEMTIME
    {
        public UInt16 year;
        public UInt16 month;
        public UInt16 dayOfWeek;
        public UInt16 day;
        public UInt16 hour;
        public UInt16 minute;
        public UInt16 second;
        public UInt16 milliseconds;
    }

    internal static class PdhFormat
    {
        public const uint PDH_FMT_RAW = 0x00000010;
        public const uint PDH_FMT_ANSI = 0x00000020;
        public const uint PDH_FMT_UNICODE = 0x00000040;
        public const uint PDH_FMT_LONG = 0x00000100;
        public const uint PDH_FMT_DOUBLE = 0x00000200;
        public const uint PDH_FMT_LARGE = 0x00000400;
        public const uint PDH_FMT_NOSCALE = 0x00001000;
        public const uint PDH_FMT_1000 = 0x00002000;
        public const uint PDH_FMT_NODATA = 0x00004000;
        public const uint PDH_FMT_NOCAP100 = 0x00008000;
        public const uint PERF_DETAIL_COSTLY = 0x00010000;
        public const uint PERF_DETAIL_STANDARD = 0x0000FFFF;
    }

    internal static class PdhLogAccess
    {
        public const uint PDH_LOG_READ_ACCESS = 0x00010000;
        public const uint PDH_LOG_WRITE_ACCESS = 0x00020000;
        public const uint PDH_LOG_UPDATE_ACCESS = 0x00040000;
        public const uint PDH_LOG_ACCESS_MASK = 0x000F0000;
    }

    internal static class PdhLogOpenMode
    {
        public const uint PDH_LOG_CREATE_NEW = 0x00000001;
        public const uint PDH_LOG_CREATE_ALWAYS = 0x00000002;
        public const uint PDH_LOG_OPEN_ALWAYS = 0x00000003;
        public const uint PDH_LOG_OPEN_EXISTING = 0x00000004;
        public const uint PDH_LOG_CREATE_MASK = 0x0000000F;
    }

    internal static class PdhLogOpenOption
    {
        public const uint PDH_LOG_OPT_USER_STRING = 0x01000000;
        public const uint PDH_LOG_OPT_CIRCULAR = 0x02000000;
        public const uint PDH_LOG_OPT_MAX_IS_BYTES = 0x04000000;
        public const uint PDH_LOG_OPT_APPEND = 0x08000000;
    }

    internal enum PdhLogFileType
    {
        PDH_LOG_TYPE_UNDEFINED = 0,
        PDH_LOG_TYPE_CSV = 1,
        PDH_LOG_TYPE_TSV = 2,
        PDH_LOG_TYPE_TRACE_KERNEL = 4,
        PDH_LOG_TYPE_TRACE_GENERIC = 5,
        PDH_LOG_TYPE_PERFMON = 6,
        PDH_LOG_TYPE_SQL = 7,
        PDH_LOG_TYPE_BINARY = 8
    }

    internal static class PdhWildCardFlag
    {
        public const uint PDH_NOEXPANDCOUNTERS = 1;
        public const uint PDH_NOEXPANDINSTANCES = 2;
        public const uint PDH_REFRESHCOUNTERS = 4;
    }

    internal struct CounterHandleNInstance
    {
        public IntPtr hCounter;
        public string InstanceName;
    }

    internal class PdhHelper : IDisposable
    {
        private bool _isPreVista = false;

        public PdhHelper(bool isPreVista)
        {
            _isPreVista = isPreVista;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PDH_COUNTER_PATH_ELEMENTS
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string MachineName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string ObjectName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string InstanceName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string ParentInstance;

            public UInt32 InstanceIndex;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string CounterName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PDH_FMT_COUNTERVALUE_LARGE
        {
            public uint CStatus;

            public Int64 largeValue;

            // [FieldOffset (4), MarshalAs(UnmanagedType.LPStr)]
            // public string AnsiStringValue;

            // [FieldOffset(4), MarshalAs(UnmanagedType.LPWStr)]
            // public string WideStringValue;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct PDH_FMT_COUNTERVALUE_DOUBLE
        {
            public uint CStatus;

            public double doubleValue;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct PDH_FMT_COUNTERVALUE_UNICODE
        {
            public uint CStatus;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string WideStringValue;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct PDH_RAW_COUNTER
        {
            public uint CStatus;
            public System.Runtime.InteropServices.ComTypes.FILETIME TimeStamp;
            public Int64 FirstValue;
            public Int64 SecondValue;
            public uint MultiCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PDH_TIME_INFO
        {
            public Int64 StartTime;
            public Int64 EndTime;
            public UInt32 SampleCount;
        }

        /*
        //
        // This is the structure returned by PdhGetCounterInfo().
        // We only need dwType and lDefaultScale fields from this structure.
        // We access those fields directly. The struct is here for reference only.
        //
        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        struct PDH_COUNTER_INFO {
            [FieldOffset(0)]  public UInt32 dwLength;
            [FieldOffset(4)]  public UInt32 dwType;
            [FieldOffset(8)]  public UInt32 CVersion;
            [FieldOffset(12)] public UInt32 CStatus;
            [FieldOffset(16)] public UInt32 lScale;
            [FieldOffset(20)] public UInt32 lDefaultScale;
            [FieldOffset(24)] public IntPtr dwUserData;
            [FieldOffset(32)] public IntPtr dwQueryUserData;
            [FieldOffset(40)] public string szFullPath;

            [FieldOffset(48)] public string szMachineName;
            [FieldOffset(56)] public string szObjectName;
            [FieldOffset(64)] public string szInstanceName;
            [FieldOffset(72)] public string szParentInstance;
            [FieldOffset(80)] public UInt32 dwInstanceIndex;
            [FieldOffset(88)] public string szCounterName;

            [FieldOffset(96)] public string szExplainText;
            [FieldOffset(104)]public IntPtr DataBuffer;
        }*/

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhBindInputDataSource(out PdhSafeDataSourceHandle phDataSource, string szLogFileNameList);

        [DllImport("pdh.dll")]
        private static extern uint PdhOpenQueryH(PdhSafeDataSourceHandle hDataSource, IntPtr dwUserData, out PdhSafeQueryHandle phQuery);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhAddCounter(PdhSafeQueryHandle queryHandle, string counterPath, IntPtr userData, out IntPtr counterHandle);

        // Win7+ only
        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhAddRelogCounter(PdhSafeQueryHandle queryHandle, string counterPath,
                                                       UInt32 counterType, UInt32 counterDefaultScale,
                                                       UInt64 timeBase, out IntPtr counterHandle);

        // not on XP
        [DllImport("pdh.dll")]
        private static extern uint PdhCollectQueryDataWithTime(PdhSafeQueryHandle queryHandle, ref Int64 pllTimeStamp);

        [DllImport("pdh.dll")]
        private static extern uint PdhCollectQueryData(PdhSafeQueryHandle queryHandle);

        [DllImport("pdh.dll")]
        internal static extern uint PdhCloseQuery(IntPtr queryHandle);

        [DllImport("pdh.dll")]
        internal static extern uint PdhCloseLog(IntPtr logHandle, uint dwFlags);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhOpenLog(string szLogFileName,
                                               uint dwAccessFlags,
                                               ref PdhLogFileType lpdwLogType,
                                               PdhSafeQueryHandle hQuery,
                                               uint dwMaxSize,
                                               string szUserCaption,
                                               out PdhSafeLogHandle phLog
                                              );

        // Win7+ only
        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern void PdhResetRelogCounterValues(PdhSafeLogHandle LogHandle);

        // Win7+ only
        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhSetCounterValue(IntPtr CounterHandle,
                                                        ref PDH_RAW_COUNTER Value, /*PPDH_RAW_COUNTER */
                                                        string InstanceName
                                                        );

        // Win7+ only
        [DllImport("pdh.dll")]
        private static extern uint PdhWriteRelogSample(PdhSafeLogHandle LogHandle,
                                                        Int64 Timestamp
                                                        );

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhGetFormattedCounterValue(IntPtr counterHandle, uint dwFormat, out IntPtr lpdwType, out PDH_FMT_COUNTERVALUE_DOUBLE pValue);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhGetRawCounterValue(IntPtr hCounter, out IntPtr lpdwType, out PDH_RAW_COUNTER pValue);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhEnumMachinesH(PdhSafeDataSourceHandle hDataSource, IntPtr mszMachineNameList, ref IntPtr pcchBufferLength);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhEnumObjectsH(PdhSafeDataSourceHandle hDataSource, string szMachineName, IntPtr mszObjectList, ref IntPtr pcchBufferLength, uint dwDetailLevel, bool bRefresh);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhEnumObjectItemsH(PdhSafeDataSourceHandle hDataSource,
                                                       string szMachineName,
                                                       string szObjectName,
                                                       IntPtr mszCounterList,
                                                       ref IntPtr pcchCounterListLength,
                                                       IntPtr mszInstanceList,
                                                       ref IntPtr pcchInstanceListLength,
                                                       uint dwDetailLevel,
                                                       uint dwFlags);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhMakeCounterPath(ref PDH_COUNTER_PATH_ELEMENTS pCounterPathElements,
                                                       IntPtr szFullPathBuffer,
                                                       ref IntPtr pcchBufferSize,
                                                       UInt32 dwFlags);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhParseCounterPath(string szFullPathBuffer,
                                                       IntPtr pCounterPathElements, // PDH_COUNTER_PATH_ELEMENTS
                                                       ref IntPtr pdwBufferSize,
                                                       uint dwFlags);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhExpandWildCardPathH(PdhSafeDataSourceHandle hDataSource,
                                                           string szWildCardPath,
                                                           IntPtr mszExpandedPathList,
                                                           ref IntPtr pcchPathListLength,
                                                           uint dwFlags);

        // not available on XP
        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhValidatePathEx(PdhSafeDataSourceHandle hDataSource, string szFullPathBuffer);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhValidatePath(string szFullPathBuffer);

        // not available on XP
        [DllImport("pdh.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall, PreserveSig = true)] // private export
        private static extern IntPtr PdhGetExplainText(string szMachineName, string szObjectName, string szCounterName);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhGetCounterInfo(IntPtr hCounter, [MarshalAs(UnmanagedType.U1)]bool bRetrieveExplainText, ref IntPtr pdwBufferSize, IntPtr lpBuffer);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhGetCounterTimeBase(IntPtr hCounter, out UInt64 pTimeBase);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhGetDataSourceTimeRangeH(PdhSafeDataSourceHandle hDataSource, ref IntPtr pdwNumEntries, ref PDH_TIME_INFO pInfo, ref IntPtr pdwBufferSize);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhSetQueryTimeRange(PdhSafeQueryHandle hQuery, ref PDH_TIME_INFO pInfo);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhLookupPerfNameByIndex(string szMachineName, UInt32 dwNameIndex, IntPtr szNameBuffer, ref int pcchNameBufferSize);

        private PdhSafeDataSourceHandle _hDataSource = null;

        private PdhSafeQueryHandle _hQuery = null;

        private bool _firstReading = true;

        private PdhSafeLogHandle _hOutputLog = null;

        //
        // Implement IDisposable::Dispose() to close native safe handles
        //
        public void Dispose()
        {
            if (_hDataSource != null && !_hDataSource.IsInvalid)
            {
                _hDataSource.Dispose();
            }

            if (_hOutputLog != null && !_hOutputLog.IsInvalid)
            {
                _hOutputLog.Dispose();
            }

            if (_hQuery != null && !_hQuery.IsInvalid)
            {
                _hQuery.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        //
        // m_ConsumerPathToHandleAndInstanceMap map is used for reading counter date (live or from files).
        //
        private Dictionary<string, CounterHandleNInstance> _consumerPathToHandleAndInstanceMap = new Dictionary<string, CounterHandleNInstance>();

        //
        // m_ReloggerPathToHandleAndInstanceMap map is used for writing relog counters.
        //
        private Dictionary<string, CounterHandleNInstance> _reloggerPathToHandleAndInstanceMap = new Dictionary<string, CounterHandleNInstance>();

        /// <summary>
        /// A helper reading in a Unicode string with embedded NULLs and splitting it into a StringCollection.
        /// </summary>
        /// <param name="strNative"></param>
        /// <param name="strSize"></param>
        /// <param name="strColl"></param>

        private void ReadPdhMultiString(ref IntPtr strNative, Int32 strSize, ref StringCollection strColl)
        {
            Debug.Assert(strSize >= 2);
            int offset = 0;
            string allSubstringsWithNulls = string.Empty;
            while (offset <= ((strSize * sizeof(char)) - 4))
            {
                Int32 next4 = Marshal.ReadInt32(strNative, offset);
                if (next4 == 0)
                {
                    break;
                }

                allSubstringsWithNulls += (char)next4;

                offset += 2;
            }

            allSubstringsWithNulls = allSubstringsWithNulls.TrimEnd('\0');

            strColl.AddRange(allSubstringsWithNulls.Split('\0'));
        }

        private uint GetCounterInfoPlus(IntPtr hCounter, out UInt32 counterType, out UInt32 defaultScale, out UInt64 timeBase)
        {
            uint res = 0;
            counterType = 0;
            defaultScale = 0;
            timeBase = 0;

            Debug.Assert(hCounter != null);

            IntPtr pBufferSize = new IntPtr(0);
            res = PdhGetCounterInfo(hCounter, false, ref pBufferSize, IntPtr.Zero);
            if (res != PdhResults.PDH_MORE_DATA)
            {
                return res;
            }

            Int32 bufSize = pBufferSize.ToInt32();
            IntPtr bufCounterInfo = Marshal.AllocHGlobal(bufSize);

            try
            {
                res = PdhGetCounterInfo(hCounter, false, ref pBufferSize, bufCounterInfo);
                if (res == 0 && bufCounterInfo != IntPtr.Zero)
                {
                    // PDH_COUNTER_INFO pdhCounterInfo = (PDH_COUNTER_INFO)Marshal.PtrToStructure(bufCounterInfo, typeof(PDH_COUNTER_INFO));

                    counterType = (uint)Marshal.ReadInt32(bufCounterInfo, 4);
                    defaultScale = (uint)Marshal.ReadInt32(bufCounterInfo, 20);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(bufCounterInfo);
            }

            res = PdhGetCounterTimeBase(hCounter, out timeBase);
            if (res != 0)
            {
                return res;
            }

            return res;
        }

        public uint ConnectToDataSource()
        {
            if (_hDataSource != null && !_hDataSource.IsInvalid)
            {
                _hDataSource.Dispose();
            }

            uint res = PdhHelper.PdhBindInputDataSource(out _hDataSource, null);
            if (res != 0)
            {
                // Console.WriteLine("error in PdhBindInputDataSource: " + res);
                return res;
            }

            return 0;
        }
        /// <summary>
        /// Connects to a single named datasource, initializing m_hDataSource variable.
        /// </summary>
        /// <param name="dataSourceName"></param>
        /// <returns></returns>
        public uint ConnectToDataSource(string dataSourceName)
        {
            if (_hDataSource != null && !_hDataSource.IsInvalid)
            {
                _hDataSource.Dispose();
            }

            uint res = PdhHelper.PdhBindInputDataSource(out _hDataSource, dataSourceName);
            if (res != 0)
            {
                // Console.WriteLine("error in PdhBindInputDataSource: " + res);
            }

            return res;
        }

        public uint ConnectToDataSource(StringCollection blgFileNames)
        {
            if (blgFileNames.Count == 1)
            {
                return ConnectToDataSource(blgFileNames[0]);
            }

            string doubleNullTerminated = string.Empty;
            foreach (string fileName in blgFileNames)
            {
                doubleNullTerminated += fileName + '\0';
            }

            doubleNullTerminated += '\0';

            return ConnectToDataSource(doubleNullTerminated);
        }

        public uint OpenQuery()
        {
            uint res = PdhOpenQueryH(_hDataSource, IntPtr.Zero, out _hQuery);

            if (res != 0)
            {
                // Console.WriteLine("error in PdhOpenQueryH: " + res);
            }

            return res;
        }

        public uint OpenLogForWriting(string logName, PdhLogFileType logFileType, bool bOverwrite, UInt32 maxSize, bool bCircular, string caption)
        {
            Debug.Assert(_hQuery != null);

            UInt32 accessFlags = PdhLogAccess.PDH_LOG_WRITE_ACCESS;
            accessFlags |= bCircular ? PdhLogOpenOption.PDH_LOG_OPT_CIRCULAR : 0;
            accessFlags |= bOverwrite ? PdhLogOpenMode.PDH_LOG_CREATE_ALWAYS : PdhLogOpenMode.PDH_LOG_CREATE_NEW;

            uint res = PdhOpenLog(logName,
                                  accessFlags,
                                  ref logFileType,
                                  _hQuery,
                                  maxSize,
                                  caption,
                                  out _hOutputLog);

            return res;
        }

        public uint SetQueryTimeRange(DateTime startTime, DateTime endTime)
        {
            Debug.Assert(_hQuery != null);
            Debug.Assert(endTime >= startTime);

            PDH_TIME_INFO pTimeInfo = new PDH_TIME_INFO();

            if (startTime != DateTime.MinValue && startTime.Kind == DateTimeKind.Local)
            {
                startTime = new DateTime(startTime.Ticks, DateTimeKind.Utc);
            }

            pTimeInfo.StartTime = (startTime == DateTime.MinValue) ? 0 : startTime.ToFileTimeUtc();

            if (endTime != DateTime.MaxValue && endTime.Kind == DateTimeKind.Local)
            {
                endTime = new DateTime(endTime.Ticks, DateTimeKind.Utc);
            }

            pTimeInfo.EndTime = (endTime == DateTime.MaxValue) ? Int64.MaxValue : endTime.ToFileTimeUtc();

            pTimeInfo.SampleCount = 0;

            return PdhSetQueryTimeRange(_hQuery, ref pTimeInfo);
        }

        public uint EnumBlgFilesMachines(ref StringCollection machineNames)
        {
            IntPtr MachineListTcharSizePtr = new IntPtr(0);
            uint res = PdhHelper.PdhEnumMachinesH(_hDataSource, IntPtr.Zero, ref MachineListTcharSizePtr);
            if (res != PdhResults.PDH_MORE_DATA)
            {
                return res;
            }

            Int32 cChars = MachineListTcharSizePtr.ToInt32(); // should be ok on 64 bit
            IntPtr strMachineList = Marshal.AllocHGlobal(cChars * sizeof(char));

            try
            {
                res = PdhHelper.PdhEnumMachinesH(_hDataSource, (IntPtr)strMachineList, ref MachineListTcharSizePtr);
                if (res == 0)
                {
                    ReadPdhMultiString(ref strMachineList, MachineListTcharSizePtr.ToInt32(), ref machineNames);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(strMachineList);
            }

            return res;
        }

        public uint EnumObjects(string machineName, ref StringCollection objectNames)
        {
            IntPtr pBufferSize = new IntPtr(0);
            uint res = PdhEnumObjectsH(_hDataSource, machineName, IntPtr.Zero, ref pBufferSize, PerfDetail.PERF_DETAIL_WIZARD, false);
            if (res != PdhResults.PDH_MORE_DATA)
            {
                return res;
            }

            Int32 cChars = pBufferSize.ToInt32();
            IntPtr strObjectList = Marshal.AllocHGlobal(cChars * sizeof(char));

            try
            {
                res = PdhEnumObjectsH(_hDataSource, machineName, (IntPtr)strObjectList, ref pBufferSize, PerfDetail.PERF_DETAIL_WIZARD, false);
                if (res == 0)
                {
                    ReadPdhMultiString(ref strObjectList, pBufferSize.ToInt32(), ref objectNames);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(strObjectList);
            }

            return res;
        }

        public uint EnumObjectItems(string machineName, string objectName, ref StringCollection counterNames, ref StringCollection instanceNames)
        {
            IntPtr pCounterBufferSize = new IntPtr(0);
            IntPtr pInstanceBufferSize = new IntPtr(0);

            uint res = PdhEnumObjectItemsH(_hDataSource, machineName, objectName,
                                            IntPtr.Zero, ref pCounterBufferSize,
                                            IntPtr.Zero, ref pInstanceBufferSize,
                                            PerfDetail.PERF_DETAIL_WIZARD, 0);
            if (res == PdhResults.PDH_CSTATUS_NO_INSTANCE)
            {
                instanceNames.Clear();
                return 0; // masking the error
            }
            else if (res == PdhResults.PDH_CSTATUS_NO_OBJECT)
            {
                counterNames.Clear();
                return 0; // masking the error
            }
            else if (res != PdhResults.PDH_MORE_DATA)
            {
                // Console.WriteLine("error in PdhEnumObjectItemsH 1st call: " + res);
                return res;
            }

            Int32 cChars = pCounterBufferSize.ToInt32();
            IntPtr strCountersList = (cChars > 0) ?
                Marshal.AllocHGlobal((cChars) * sizeof(char)) : IntPtr.Zero;
            // re-set count to 0 if it is lte 2
            if (cChars < 0)
            {
                pCounterBufferSize = new IntPtr(0);
            }

            cChars = pInstanceBufferSize.ToInt32();
            IntPtr strInstancesList = (cChars > 0) ?
                Marshal.AllocHGlobal((cChars) * sizeof(char)) : IntPtr.Zero;

            // re-set count to 0 if it is lte 2
            if (cChars < 0)
            {
                pInstanceBufferSize = new IntPtr(0);
            }

            try
            {
                res = PdhEnumObjectItemsH(_hDataSource, machineName, objectName,
                                        strCountersList, ref pCounterBufferSize,
                                        strInstancesList, ref pInstanceBufferSize,
                                        PerfDetail.PERF_DETAIL_WIZARD, 0);
                if (res != 0)
                {
                    // Console.WriteLine("error in PdhEnumObjectItemsH 2nd call: " + res + "\n Counter buffer size is  "
                    //    + pCounterBufferSize.ToInt32() + "\n Instance buffer size is  " + pInstanceBufferSize.ToInt32());
                }
                else
                {
                    ReadPdhMultiString(ref strCountersList, pCounterBufferSize.ToInt32(), ref counterNames);
                    if (strInstancesList != IntPtr.Zero)
                    {
                        ReadPdhMultiString(ref strInstancesList, pInstanceBufferSize.ToInt32(), ref instanceNames);
                    }
                }
            }
            finally
            {
                if (strCountersList != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(strCountersList);
                }

                if (strInstancesList != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(strInstancesList);
                }
            }

            return res;
        }

        public uint GetValidPathsFromFiles(ref StringCollection validPaths)
        {
            Debug.Assert(_hDataSource != null && !_hDataSource.IsInvalid, "Call ConnectToDataSource before GetValidPathsFromFiles");

            StringCollection machineNames = new StringCollection();
            uint res = this.EnumBlgFilesMachines(ref machineNames);
            if (res != 0)
            {
                return res;
            }

            foreach (string machine in machineNames)
            {
                StringCollection counterSets = new StringCollection();
                res = this.EnumObjects(machine, ref counterSets);
                if (res != 0)
                {
                    return res;
                }

                foreach (string counterSet in counterSets)
                {
                    // Console.WriteLine("Counter set " + counterSet);

                    StringCollection counterSetCounters = new StringCollection();
                    StringCollection counterSetInstances = new StringCollection();

                    res = this.EnumObjectItems(machine, counterSet, ref counterSetCounters, ref counterSetInstances);
                    if (res != 0)
                    {
                        return res;
                    }

                    res = this.GetValidPaths(machine, counterSet, ref counterSetCounters, ref counterSetInstances, ref validPaths);
                    if (res != 0)
                    {
                        return res;
                    }
                }
            }

            return res;
        }

        private bool IsPathValid(ref PDH_COUNTER_PATH_ELEMENTS pathElts, out string outPath)
        {
            bool ret = false;
            outPath = string.Empty;
            IntPtr pPathBufferSize = new IntPtr(0);

            uint res = PdhMakeCounterPath(ref pathElts, IntPtr.Zero, ref pPathBufferSize, 0);
            if (res != PdhResults.PDH_MORE_DATA)
            {
                return false;
            }

            Int32 cChars = pPathBufferSize.ToInt32();
            IntPtr strPath = Marshal.AllocHGlobal(cChars * sizeof(char));

            try
            {
                res = PdhMakeCounterPath(ref pathElts, strPath, ref pPathBufferSize, 0);
                if (res == 0)
                {
                    outPath = Marshal.PtrToStringUni(strPath);

                    if (!_isPreVista)
                    {
                        ret = (PdhValidatePathEx(_hDataSource, outPath) == 0);
                    }
                    else
                    {
                        ret = (PdhValidatePath(outPath) == 0);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(strPath);
            }

            return ret;
        }

        public bool IsPathValid(string path)
        {
            if (!_isPreVista)
            {
                return (PdhValidatePathEx(_hDataSource, path) == 0);
            }
            else
            {
                //
                // Note: this assumes the paths already contain machine names
                //
                return (PdhValidatePath(path) == 0);
            }
        }

        private uint MakePath(PDH_COUNTER_PATH_ELEMENTS pathElts, out string outPath, bool bWildcardInstances)
        {
            outPath = string.Empty;
            IntPtr pPathBufferSize = new IntPtr(0);

            if (bWildcardInstances)
            {
                pathElts.InstanceIndex = 0;
                pathElts.InstanceName = "*";
                pathElts.ParentInstance = null;
            }

            uint res = PdhMakeCounterPath(ref pathElts, IntPtr.Zero, ref pPathBufferSize, 0);
            if (res != PdhResults.PDH_MORE_DATA)
            {
                return res;
            }

            Int32 cChars = pPathBufferSize.ToInt32();
            IntPtr strPath = Marshal.AllocHGlobal(cChars * sizeof(char));

            try
            {
                res = PdhMakeCounterPath(ref pathElts, strPath, ref pPathBufferSize, 0);
                if (res == 0)
                {
                    outPath = Marshal.PtrToStringUni(strPath);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(strPath);
            }

            return res;
        }

        private uint MakeAllInstancePath(string origPath, out string unifiedPath)
        {
            unifiedPath = origPath;

            PDH_COUNTER_PATH_ELEMENTS elts = new PDH_COUNTER_PATH_ELEMENTS();

            uint res = ParsePath(origPath, ref elts);
            if (res != 0)
            {
                return res;
            }

            return MakePath(elts, out unifiedPath, true);
        }

        private uint ParsePath(string fullPath, ref PDH_COUNTER_PATH_ELEMENTS pCounterPathElements)
        {
            IntPtr bufSize = new IntPtr(0);

            uint res = PdhParseCounterPath(fullPath,
                                           IntPtr.Zero,
                                           ref bufSize,
                                           0);
            if (res != PdhResults.PDH_MORE_DATA && res != 0)
            {
                // Console.WriteLine("error in PdhParseCounterPath: " + res);
                return res;
            }

            IntPtr structPtr = Marshal.AllocHGlobal(bufSize.ToInt32());

            try
            {
                res = PdhParseCounterPath(fullPath,
                                          structPtr,
                                          ref bufSize,
                                          0);
                if (res == 0)
                {
                    //
                    // Marshal.PtrToStructure will allocate managed memory for the object,
                    // so the unmanaged ptr can be freed safely
                    //
                    pCounterPathElements = Marshal.PtrToStructure<PDH_COUNTER_PATH_ELEMENTS>(structPtr);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(structPtr);
            }

            return res;
        }

        //
        // TranslateLocalCounterPath() helper translates counter paths from English into the current locale language.
        // NOTE: we can only translate counter set and counter names.
        // Translated instance names come from providers
        // This function will leave them unchanged:
        // however, it works for common cases like "*" and "_total"
        // and many instance names are just numbers, anyway.
        //
        // Also - this only supports local paths, b/c connecting to remote registry
        // requires a different firewall exception.
        // This function checks and Asserts if the path is not valid.
        //
        public uint TranslateLocalCounterPath(string englishPath, out string localizedPath)
        {
            uint res = 0;
            localizedPath = string.Empty;
            PDH_COUNTER_PATH_ELEMENTS pathElts = new PDH_COUNTER_PATH_ELEMENTS();
            res = ParsePath(englishPath, ref pathElts);
            if (res != 0)
            {
                return res;
            }

            // Check if the path is local and assert if not:
            string machineNameMassaged = pathElts.MachineName.ToLowerInvariant();
            machineNameMassaged = machineNameMassaged.TrimStart('\\');
            Debug.Assert(machineNameMassaged == System.Environment.MachineName.ToLowerInvariant());

            string lowerEngCtrName = pathElts.CounterName.ToLowerInvariant();
            string lowerEngObjectName = pathElts.ObjectName.ToLowerInvariant();

            // Get the registry index
            RegistryKey rootKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Perflib\\009");
            string[] regCounters = (string[])rootKey.GetValue("Counter");

            // NOTE: 1-based enumeration because the name strings follow index strings in the array
            Int32 counterIndex = -1;
            Int32 objIndex = -1;
            for (uint enumIndex = 1; enumIndex < regCounters.Length; enumIndex++)
            {
                string regString = regCounters[enumIndex];
                if (regString.ToLowerInvariant() == lowerEngCtrName)
                {
                    try
                    {
                        counterIndex = Convert.ToInt32(regCounters[enumIndex - 1], CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        return (uint)PdhResults.PDH_INVALID_PATH;
                    }
                }
                else if (regString.ToLowerInvariant() == lowerEngObjectName)
                {
                    try
                    {
                        objIndex = Convert.ToInt32(regCounters[enumIndex - 1], CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        return (uint)PdhResults.PDH_INVALID_PATH;
                    }
                }

                if (counterIndex != -1 && objIndex != -1)
                {
                    break;
                }
            }

            if (counterIndex == -1 || objIndex == -1)
            {
                return (uint)PdhResults.PDH_INVALID_PATH;
            }

            // Now, call retrieve the localized names of the object and the counter by index:
            string objNameLocalized;
            res = LookupPerfNameByIndex(pathElts.MachineName, (uint)objIndex, out objNameLocalized);
            if (res != 0)
            {
                return res;
            }

            pathElts.ObjectName = objNameLocalized;

            string ctrNameLocalized;
            res = LookupPerfNameByIndex(pathElts.MachineName, (uint)counterIndex, out ctrNameLocalized);
            if (res != 0)
            {
                return res;
            }

            pathElts.CounterName = ctrNameLocalized;

            // Assemble the path back by using the translated object and counter names:

            res = MakePath(pathElts, out localizedPath, false);

            return res;
        }

        public uint LookupPerfNameByIndex(string machineName, uint index, out string locName)
        {
            //
            //  NOTE: to make PdhLookupPerfNameByIndex() work,
            //  localizedPath needs to be pre-allocated on the first call.
            //  This is different from most other PDH functions that tolerate NULL buffers and return required size.
            //

            int strSize = 256;
            IntPtr localizedPathPtr = Marshal.AllocHGlobal(strSize * sizeof(char));
            locName = string.Empty;
            uint res = 0;
            try
            {
                res = PdhLookupPerfNameByIndex(machineName, index, localizedPathPtr, ref strSize);
                if (res == PdhResults.PDH_MORE_DATA)
                {
                    Marshal.FreeHGlobal(localizedPathPtr);
                    localizedPathPtr = Marshal.AllocHGlobal(strSize * sizeof(char));
                    res = PdhLookupPerfNameByIndex(machineName, index, localizedPathPtr, ref strSize);
                }

                if (res == 0)
                {
                    locName = Marshal.PtrToStringUni(localizedPathPtr);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(localizedPathPtr);
            }

            return res;
        }

        public uint GetValidPaths(string machineName,
                                   string objectName,
                                   ref StringCollection counters,
                                   ref StringCollection instances,
                                   ref StringCollection validPaths)
        {
            uint res = 0;

            PDH_COUNTER_PATH_ELEMENTS pathElts = new PDH_COUNTER_PATH_ELEMENTS();
            pathElts.MachineName = machineName;
            pathElts.ObjectName = objectName;

            foreach (string counterName in counters)
            {
                pathElts.CounterName = counterName;

                if (instances.Count == 0)
                {
                    string pathCandidate;
                    if (IsPathValid(ref pathElts, out pathCandidate))
                    {
                        validPaths.Add(pathCandidate);
                    }
                }
                else
                {
                    foreach (string instanceName in instances)
                    {
                        pathElts.InstanceName = instanceName;
                        pathElts.InstanceIndex = 0;

                        string pathCandidate;
                        if (IsPathValid(ref pathElts, out pathCandidate))
                        {
                            validPaths.Add(pathCandidate);
                        }
                    }
                }
            }

            return res;
        }

        public uint AddCounters(ref StringCollection validPaths, bool bFlushOldCounters)
        {
            Debug.Assert(_hQuery != null && !_hQuery.IsInvalid);

            if (bFlushOldCounters)
            {
                _consumerPathToHandleAndInstanceMap.Clear();
            }

            bool bAtLeastOneAdded = false;
            uint res = 0;

            foreach (string counterPath in validPaths)
            {
                IntPtr counterHandle;
                res = PdhAddCounter(_hQuery, counterPath, IntPtr.Zero, out counterHandle);
                if (res == 0)
                {
                    CounterHandleNInstance chi = new CounterHandleNInstance();
                    chi.hCounter = counterHandle;
                    chi.InstanceName = null;

                    PDH_COUNTER_PATH_ELEMENTS pathElts = new PDH_COUNTER_PATH_ELEMENTS();
                    res = ParsePath(counterPath, ref pathElts);
                    if (res == 0 && pathElts.InstanceName != null)
                    {
                        chi.InstanceName = pathElts.InstanceName.ToLowerInvariant();
                    }

                    if (!_consumerPathToHandleAndInstanceMap.ContainsKey(counterPath.ToLowerInvariant()))
                    {
                        _consumerPathToHandleAndInstanceMap.Add(counterPath.ToLowerInvariant(), chi);
                    }

                    bAtLeastOneAdded = true;
                }
            }

            return bAtLeastOneAdded ? 0 : res;
        }

        //
        // AddRelogCounters combines instances and adds counters to m_hQuery.
        // The counter handles and full paths
        //
        public uint AddRelogCounters(PerformanceCounterSampleSet sampleSet)
        {
            Debug.Assert(_hQuery != null && !_hQuery.IsInvalid);

            uint res = 0;

            Dictionary<string, List<PerformanceCounterSample>> prefixInstanceMap = new Dictionary<string, List<PerformanceCounterSample>>();

            //
            // Go through all the samples one, constructing prefixInstanceMap and adding new counters as needed
            //
            foreach (PerformanceCounterSample sample in sampleSet.CounterSamples)
            {
                PDH_COUNTER_PATH_ELEMENTS pathElts = new PDH_COUNTER_PATH_ELEMENTS();
                res = ParsePath(sample.Path, ref pathElts);
                if (res != 0)
                {
                    // Skipping for now, but should be a non-terminating error
                    continue;
                }

                string lowerCaseMachine = pathElts.MachineName.ToLowerInvariant();
                string lowerCaseObject = pathElts.ObjectName.ToLowerInvariant();
                string lowerCaseCounter = pathElts.CounterName.ToLowerInvariant();

                string lcPathMinusInstance = @"\\" + lowerCaseMachine + @"\" + lowerCaseObject + @"\" + lowerCaseCounter;

                List<PerformanceCounterSample> sampleList;
                if (prefixInstanceMap.TryGetValue(lcPathMinusInstance, out sampleList))
                {
                    prefixInstanceMap[lcPathMinusInstance].Add(sample);
                }
                else
                {
                    List<PerformanceCounterSample> newList = new List<PerformanceCounterSample>();
                    newList.Add(sample);
                    prefixInstanceMap.Add(lcPathMinusInstance, newList);
                }

                // Console.WriteLine ("Added path " + sample.Path + " to the 1ist map with prefix " + lcPathMinusInstance);
            }

            //
            // Add counters to the query, consolidating multi-instance with a wildcard path,
            // and construct m_ReloggerPathToHandleAndInstanceMap where each full path would be pointing to its counter handle
            // and an instance name (might be empty for no-instance counter types).
            // You can have multiple full paths inside m_ReloggerPathToHandleAndInstanceMap pointing to the same handle.
            //

            foreach (string prefix in prefixInstanceMap.Keys)
            {
                IntPtr counterHandle;
                string unifiedPath = prefixInstanceMap[prefix][0].Path;

                if (prefixInstanceMap[prefix].Count > 1)
                {
                    res = MakeAllInstancePath(prefixInstanceMap[prefix][0].Path, out unifiedPath);
                    if (res != 0)
                    {
                        // Skipping for now, but should be a non-terminating error
                        continue;
                    }
                }

                res = PdhAddRelogCounter(_hQuery,
                                         unifiedPath,
                                         (UInt32)prefixInstanceMap[prefix][0].CounterType,
                                         prefixInstanceMap[prefix][0].DefaultScale,
                                         prefixInstanceMap[prefix][0].TimeBase,
                                         out counterHandle);
                if (res != 0)
                {
                    // Skipping for now, but should be a non-terminating error
                    // Console.WriteLine ("PdhAddCounter returned " + res + " for counter path " + unifiedPath);
                    continue;
                }

                // now, add all actual paths to m_ReloggerPathToHandleAndInstanceMap
                foreach (PerformanceCounterSample sample in prefixInstanceMap[prefix])
                {
                    PDH_COUNTER_PATH_ELEMENTS pathElts = new PDH_COUNTER_PATH_ELEMENTS();
                    res = ParsePath(sample.Path, ref pathElts);
                    if (res != 0)
                    {
                        // Skipping for now, but should be a non-terminating error
                        continue;
                    }

                    CounterHandleNInstance chi = new CounterHandleNInstance();

                    chi.hCounter = counterHandle;

                    if (pathElts.InstanceName != null)
                    {
                        chi.InstanceName = pathElts.InstanceName.ToLowerInvariant();
                    }

                    if (!_reloggerPathToHandleAndInstanceMap.ContainsKey(sample.Path.ToLowerInvariant()))
                    {
                        _reloggerPathToHandleAndInstanceMap.Add(sample.Path.ToLowerInvariant(), chi);
                        // Console.WriteLine ("added map path:" + sample.Path );
                    }
                }
            }

            // TODO: verify that all counters are in the map

            return (_reloggerPathToHandleAndInstanceMap.Keys.Count > 0) ? 0 : res;
        }

        //
        // AddRelogCountersPreservingPaths preserves all paths and adds as relog counters to m_hQuery.
        // The counter handles and full paths are added to m_ReloggerPathToHandleAndInstanceMap
        //
        public uint AddRelogCountersPreservingPaths(PerformanceCounterSampleSet sampleSet)
        {
            Debug.Assert(_hQuery != null && !_hQuery.IsInvalid);

            uint res = 0;

            //
            // Go through all the samples one, constructing prefixInstanceMap and adding new counters as needed
            //
            foreach (PerformanceCounterSample sample in sampleSet.CounterSamples)
            {
                PDH_COUNTER_PATH_ELEMENTS pathElts = new PDH_COUNTER_PATH_ELEMENTS();
                res = ParsePath(sample.Path, ref pathElts);
                if (res != 0)
                {
                    // Skipping for now, but should be a non-terminating error
                    continue;
                }

                IntPtr counterHandle;
                res = PdhAddRelogCounter(_hQuery,
                                         sample.Path,
                                         (uint)sample.CounterType,
                                         sample.DefaultScale,
                                         sample.TimeBase,
                                         out counterHandle);
                if (res != 0)
                {
                    // Skipping for now, but should be a non-terminating error
                    continue;
                }

                CounterHandleNInstance chi = new CounterHandleNInstance();

                chi.hCounter = counterHandle;
                if (pathElts.InstanceName != null)
                {
                    chi.InstanceName = pathElts.InstanceName.ToLowerInvariant();
                }

                if (!_reloggerPathToHandleAndInstanceMap.ContainsKey(sample.Path.ToLowerInvariant()))
                {
                    _reloggerPathToHandleAndInstanceMap.Add(sample.Path.ToLowerInvariant(), chi);
                }
            }

            return (_reloggerPathToHandleAndInstanceMap.Keys.Count > 0) ? 0 : res;
        }

        public string GetCounterSetHelp(string szMachineName, string szObjectName)
        {
            if (_isPreVista)
            {
                return string.Empty;
            }

            IntPtr retString = PdhGetExplainText(szMachineName, szObjectName, null);
            return Marshal.PtrToStringUni(retString);
        }

        public uint ReadNextSetPreVista(out PerformanceCounterSampleSet nextSet, bool bSkipReading)
        {
            uint res = 0;
            nextSet = null;

            res = PdhCollectQueryData(_hQuery);
            if (bSkipReading)
            {
                return res;
            }

            if (res != 0 && res != PdhResults.PDH_NO_DATA)
            {
                return res;
            }

            PerformanceCounterSample[] samplesArr = new PerformanceCounterSample[_consumerPathToHandleAndInstanceMap.Count];
            uint sampleIndex = 0;
            uint numInvalidDataSamples = 0;
            uint lastErr = 0;

            DateTime sampleTimeStamp = DateTime.Now;

            foreach (string path in _consumerPathToHandleAndInstanceMap.Keys)
            {
                IntPtr counterTypePtr = new IntPtr(0);
                UInt32 counterType = (UInt32)PerformanceCounterType.RawBase;
                UInt32 defaultScale = 0;
                UInt64 timeBase = 0;

                IntPtr hCounter = _consumerPathToHandleAndInstanceMap[path].hCounter;
                Debug.Assert(hCounter != null);

                res = GetCounterInfoPlus(hCounter, out counterType, out defaultScale, out timeBase);
                if (res != 0)
                {
                    // Console.WriteLine ("GetCounterInfoPlus for " + path + " failed with " + res);
                }

                PDH_RAW_COUNTER rawValue;
                res = PdhGetRawCounterValue(hCounter, out counterTypePtr, out rawValue);
                if (res == PdhResults.PDH_INVALID_DATA || res == PdhResults.PDH_NO_DATA)
                {
                    // Console.WriteLine ("PdhGetRawCounterValue returned " + res);
                    samplesArr[sampleIndex++] = new PerformanceCounterSample(path,
                                           _consumerPathToHandleAndInstanceMap[path].InstanceName,
                                           0,
                                           (ulong)0,
                                           (ulong)0,
                                           0,
                                           PerformanceCounterType.RawBase,
                                           defaultScale,
                                           timeBase,
                                           DateTime.Now,
                                           (UInt64)DateTime.Now.ToFileTime(),
                                           rawValue.CStatus);

                    numInvalidDataSamples++;
                    lastErr = res;
                    continue;
                }
                else if (res != 0)
                {
                    return res;
                }

                long dtFT = (((long)rawValue.TimeStamp.dwHighDateTime) << 32) +
                                     (uint)rawValue.TimeStamp.dwLowDateTime;

                //
                // NOTE: PDH returns the filetime as local time, therefore
                // we need to call FromFileTimUtc() to avoid .NET applying the timezone adjustment.
                // However, that would result in the DateTime object having Kind.Utc.
                // We have to copy it once more to correct that (Kind is a read-only property).
                //
                sampleTimeStamp = new DateTime(DateTime.FromFileTimeUtc(dtFT).Ticks, DateTimeKind.Local);

                PDH_FMT_COUNTERVALUE_DOUBLE fmtValueDouble;
                res = PdhGetFormattedCounterValue(hCounter,
                                                  PdhFormat.PDH_FMT_DOUBLE | PdhFormat.PDH_FMT_NOCAP100,
                                                  out counterTypePtr,
                                                  out fmtValueDouble);
                if (res == PdhResults.PDH_INVALID_DATA || res == PdhResults.PDH_NO_DATA)
                {
                    // Console.WriteLine ("PdhGetFormattedCounterValue returned " + res);
                    samplesArr[sampleIndex++] = new PerformanceCounterSample(path,
                                           _consumerPathToHandleAndInstanceMap[path].InstanceName,
                                           0,
                                           (ulong)rawValue.FirstValue,
                                           (ulong)rawValue.SecondValue,
                                           rawValue.MultiCount,
                                           (PerformanceCounterType)counterType,
                                           defaultScale,
                                           timeBase,
                                           sampleTimeStamp,
                                           (UInt64)dtFT,
                                           fmtValueDouble.CStatus);

                    numInvalidDataSamples++;
                    lastErr = res;
                    continue;
                }
                else if (res != 0)
                {
                    // Console.WriteLine ("PdhGetFormattedCounterValue returned " + res);
                    return res;
                }

                samplesArr[sampleIndex++] = new PerformanceCounterSample(path,
                                                           _consumerPathToHandleAndInstanceMap[path].InstanceName,
                                                           fmtValueDouble.doubleValue,
                                                           (ulong)rawValue.FirstValue,
                                                           (ulong)rawValue.SecondValue,
                                                           rawValue.MultiCount,
                                                           (PerformanceCounterType)counterTypePtr.ToInt32(),
                                                           defaultScale,
                                                           timeBase,
                                                           sampleTimeStamp,
                                                           (UInt64)dtFT,
                                                           fmtValueDouble.CStatus);
            }

            //
            // Prior to Vista, PdhCollectQueryDataWithTime() was not available,
            // so we could not collect a timestamp for the entire sample set.
            // We will use the last sample's timestamp instead.
            //
            nextSet = new PerformanceCounterSampleSet(sampleTimeStamp, samplesArr, _firstReading);
            _firstReading = false;

            if (numInvalidDataSamples == samplesArr.Length)
            {
                res = lastErr;
            }
            else
            {
                //
                // Reset the error - any errors are saved per sample in PerformanceCounterSample.Status for kvetching later
                //
                res = 0;
            }

            return res;
        }

        public uint ReadNextSet(out PerformanceCounterSampleSet nextSet, bool bSkipReading)
        {
            Debug.Assert(_hQuery != null && !_hQuery.IsInvalid);

            if (_isPreVista)
            {
                return ReadNextSetPreVista(out nextSet, bSkipReading);
            }

            uint res = 0;
            nextSet = null;

            Int64 batchTimeStampFT = 0;

            res = PdhCollectQueryDataWithTime(_hQuery, ref batchTimeStampFT);
            if (bSkipReading)
            {
                return res;
            }

            if (res != 0 && res != PdhResults.PDH_NO_DATA)
            {
                return res;
            }

            //
            // NOTE: PDH returns the filetime as local time, therefore
            // we need to call FromFileTimUtc() to avoid .NET applying the timezone adjustment.
            // However, that would result in the DateTime object having Kind.Utc.
            // We have to copy it once more to correct that (Kind is a read-only property).
            //
            DateTime batchStamp = DateTime.Now;
            if (res != PdhResults.PDH_NO_DATA)
            {
                batchStamp = new DateTime(DateTime.FromFileTimeUtc(batchTimeStampFT).Ticks, DateTimeKind.Local);
            }

            PerformanceCounterSample[] samplesArr = new PerformanceCounterSample[_consumerPathToHandleAndInstanceMap.Count];
            uint sampleIndex = 0;
            uint numInvalidDataSamples = 0;
            uint lastErr = 0;

            foreach (string path in _consumerPathToHandleAndInstanceMap.Keys)
            {
                IntPtr counterTypePtr = new IntPtr(0);
                UInt32 counterType = (UInt32)PerformanceCounterType.RawBase;
                UInt32 defaultScale = 0;
                UInt64 timeBase = 0;

                IntPtr hCounter = _consumerPathToHandleAndInstanceMap[path].hCounter;
                Debug.Assert(hCounter != null);

                res = GetCounterInfoPlus(hCounter, out counterType, out defaultScale, out timeBase);
                if (res != 0)
                {
                    // Console.WriteLine ("GetCounterInfoPlus for " + path + " failed with " + res);
                }

                PDH_RAW_COUNTER rawValue;
                res = PdhGetRawCounterValue(hCounter, out counterTypePtr, out rawValue);
                if (res != 0)
                {
                    samplesArr[sampleIndex++] = new PerformanceCounterSample(path,
                                           _consumerPathToHandleAndInstanceMap[path].InstanceName,
                                           0,
                                           (ulong)0,
                                           (ulong)0,
                                           0,
                                           PerformanceCounterType.RawBase,
                                           defaultScale,
                                           timeBase,
                                           batchStamp,
                                           (UInt64)batchStamp.ToFileTime(),
                                           (rawValue.CStatus == 0) ? res : rawValue.CStatus);

                    numInvalidDataSamples++;
                    lastErr = res;
                    continue;
                }

                long dtFT = (((long)rawValue.TimeStamp.dwHighDateTime) << 32) +
                                     (uint)rawValue.TimeStamp.dwLowDateTime;

                DateTime dt = new DateTime(DateTime.FromFileTimeUtc(dtFT).Ticks, DateTimeKind.Local);

                PDH_FMT_COUNTERVALUE_DOUBLE fmtValueDouble;
                res = PdhGetFormattedCounterValue(hCounter,
                                                  PdhFormat.PDH_FMT_DOUBLE | PdhFormat.PDH_FMT_NOCAP100,
                                                  out counterTypePtr,
                                                  out fmtValueDouble);
                if (res != 0)
                {
                    samplesArr[sampleIndex++] = new PerformanceCounterSample(path,
                                           _consumerPathToHandleAndInstanceMap[path].InstanceName,
                                           0,
                                           (ulong)rawValue.FirstValue,
                                           (ulong)rawValue.SecondValue,
                                           rawValue.MultiCount,
                                           (PerformanceCounterType)counterType,
                                           defaultScale,
                                           timeBase,
                                           dt,
                                           (UInt64)dtFT,
                                           (fmtValueDouble.CStatus == 0) ? res : rawValue.CStatus);

                    numInvalidDataSamples++;
                    lastErr = res;
                    continue;
                }

                samplesArr[sampleIndex++] = new PerformanceCounterSample(path,
                                                           _consumerPathToHandleAndInstanceMap[path].InstanceName,
                                                           fmtValueDouble.doubleValue,
                                                           (ulong)rawValue.FirstValue,
                                                           (ulong)rawValue.SecondValue,
                                                           rawValue.MultiCount,
                                                           (PerformanceCounterType)counterTypePtr.ToInt32(),
                                                           defaultScale,
                                                           timeBase,
                                                           dt,
                                                           (UInt64)dtFT,
                                                           fmtValueDouble.CStatus);
            }

            nextSet = new PerformanceCounterSampleSet(batchStamp, samplesArr, _firstReading);
            _firstReading = false;

            if (numInvalidDataSamples == samplesArr.Length)
            {
                res = lastErr;
            }
            else
            {
                //
                // Reset the error - any errors are saved per sample in PerformanceCounterSample.Status for kvetching later
                //
                res = 0;
            }

            return res;
        }

        public uint GetFilesSummary(out CounterFileInfo summary)
        {
            IntPtr pNumEntries = new IntPtr(0);
            PDH_TIME_INFO pInfo = new PDH_TIME_INFO();
            IntPtr bufSize = new IntPtr(System.Runtime.InteropServices.Marshal.SizeOf(pInfo));

            uint res = PdhGetDataSourceTimeRangeH(_hDataSource,
                                                    ref pNumEntries,
                                                    ref pInfo,
                                                    ref bufSize);
            if (res != 0)
            {
                summary = new CounterFileInfo();
                return res;
            }

            summary = new CounterFileInfo(new DateTime(DateTime.FromFileTimeUtc(pInfo.StartTime).Ticks, DateTimeKind.Local),
                                           new DateTime(DateTime.FromFileTimeUtc(pInfo.EndTime).Ticks, DateTimeKind.Local),
                                           pInfo.SampleCount);

            return res;
        }

        public uint ExpandWildCardPath(string path, out StringCollection expandedPaths)
        {
            expandedPaths = new StringCollection();
            IntPtr pcchPathListLength = new IntPtr(0);

            uint res = PdhExpandWildCardPathH(_hDataSource,
                                             path,
                                             IntPtr.Zero,
                                             ref pcchPathListLength,
                                             PdhWildCardFlag.PDH_REFRESHCOUNTERS);

            if (res != PdhResults.PDH_MORE_DATA)
            {
                return res;
            }

            Int32 cChars = pcchPathListLength.ToInt32();
            IntPtr strPathList = Marshal.AllocHGlobal(cChars * sizeof(char));

            try
            {
                res = PdhExpandWildCardPathH(_hDataSource, path, strPathList, ref pcchPathListLength, PdhWildCardFlag.PDH_REFRESHCOUNTERS);
                if (res == 0)
                {
                    ReadPdhMultiString(ref strPathList, pcchPathListLength.ToInt32(), ref expandedPaths);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(strPathList);
            }

            return res;
        }

        public void ResetRelogValues()
        {
            Debug.Assert(_hOutputLog != null && !_hOutputLog.IsInvalid);
            PdhResetRelogCounterValues(_hOutputLog);
        }

        public uint WriteRelogSample(DateTime timeStamp)
        {
            Debug.Assert(_hOutputLog != null && !_hOutputLog.IsInvalid);
            return PdhWriteRelogSample(_hOutputLog, (new DateTime(timeStamp.Ticks, DateTimeKind.Utc)).ToFileTimeUtc());
        }

        public uint SetCounterValue(PerformanceCounterSample sample, out bool bUnknownPath)
        {
            Debug.Assert(_hOutputLog != null && !_hOutputLog.IsInvalid);

            bUnknownPath = false;

            string lcPath = sample.Path.ToLowerInvariant();

            if (!_reloggerPathToHandleAndInstanceMap.ContainsKey(lcPath))
            {
                bUnknownPath = true;
                return 0;
            }

            PDH_RAW_COUNTER rawStruct = new PDH_RAW_COUNTER();
            rawStruct.FirstValue = (long)sample.RawValue;
            rawStruct.SecondValue = (long)sample.SecondValue;
            rawStruct.MultiCount = sample.MultipleCount;
            rawStruct.TimeStamp.dwHighDateTime = (int)((new DateTime(sample.Timestamp.Ticks, DateTimeKind.Utc).ToFileTimeUtc() >> 32) & 0xFFFFFFFFL);
            rawStruct.TimeStamp.dwLowDateTime = (int)(new DateTime(sample.Timestamp.Ticks, DateTimeKind.Utc).ToFileTimeUtc() & 0xFFFFFFFFL);
            rawStruct.CStatus = sample.Status;

            return PdhSetCounterValue(_reloggerPathToHandleAndInstanceMap[lcPath].hCounter,
                                        ref rawStruct, /*PPDH_RAW_COUNTER */
                                        _reloggerPathToHandleAndInstanceMap[lcPath].InstanceName);
        }
    }
}
