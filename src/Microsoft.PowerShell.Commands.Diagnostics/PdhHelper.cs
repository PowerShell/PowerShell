// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Microsoft.PowerShell.Commands.GetCounter;
using Microsoft.Win32;

namespace Microsoft.Powershell.Commands.GetCounter.PdhNative
{
    internal static class PdhResults
    {
        public const uint PDH_CSTATUS_VALID_DATA = 0x0;
        public const uint PDH_CSTATUS_NEW_DATA = 0x1;
        public const uint PDH_CSTATUS_NO_MACHINE = 0x800007D0;
        public const uint PDH_CSTATUS_NO_INSTANCE = 0x800007D1;
        public const uint PDH_MORE_DATA = 0x800007D2;
        public const uint PDH_CSTATUS_ITEM_NOT_VALIDATED = 0x800007D3;
        public const uint PDH_RETRY = 0x800007D4;
        public const uint PDH_NO_DATA = 0x800007D5;
        public const uint PDH_CALC_NEGATIVE_DENOMINATOR = 0x800007D6;
        public const uint PDH_CALC_NEGATIVE_TIMEBASE = 0x800007D7;
        public const uint PDH_CALC_NEGATIVE_VALUE = 0x800007D8;
        public const uint PDH_DIALOG_CANCELLED = 0x800007D9;
        public const uint PDH_END_OF_LOG_FILE = 0x800007DA;
        public const uint PDH_ASYNC_QUERY_TIMEOUT = 0x800007DB;
        public const uint PDH_CANNOT_SET_DEFAULT_REALTIME_DATASOURCE = 0x800007DC;
        public const uint PDH_UNABLE_MAP_NAME_FILES = 0x80000BD5;
        public const uint PDH_PLA_VALIDATION_WARNING = 0x80000BF3;
        public const uint PDH_CSTATUS_NO_OBJECT = 0xC0000BB8;
        public const uint PDH_CSTATUS_NO_COUNTER = 0xC0000BB9;
        public const uint PDH_CSTATUS_INVALID_DATA = 0xC0000BBA;
        public const uint PDH_MEMORY_ALLOCATION_FAILURE = 0xC0000BBB;
        public const uint PDH_INVALID_HANDLE = 0xC0000BBC;
        public const uint PDH_INVALID_ARGUMENT = 0xC0000BBD;
        public const uint PDH_FUNCTION_NOT_FOUND = 0xC0000BBE;
        public const uint PDH_CSTATUS_NO_COUNTERNAME = 0xC0000BBF;
        public const uint PDH_CSTATUS_BAD_COUNTERNAME = 0xC0000BC0;
        public const uint PDH_INVALID_BUFFER = 0xC0000BC1;
        public const uint PDH_INSUFFICIENT_BUFFER = 0xC0000BC2;
        public const uint PDH_CANNOT_CONNECT_MACHINE = 0xC0000BC3;
        public const uint PDH_INVALID_PATH = 0xC0000BC4;
        public const uint PDH_INVALID_INSTANCE = 0xC0000BC5;
        public const uint PDH_INVALID_DATA = 0xC0000BC6;
        public const uint PDH_NO_DIALOG_DATA = 0xC0000BC7;
        public const uint PDH_CANNOT_READ_NAME_STRINGS = 0xC0000BC8;
        public const uint PDH_LOG_FILE_CREATE_ERROR = 0xC0000BC9;
        public const uint PDH_LOG_FILE_OPEN_ERROR = 0xC0000BCA;
        public const uint PDH_LOG_TYPE_NOT_FOUND = 0xC0000BCB;
        public const uint PDH_NO_MORE_DATA = 0xC0000BCC;
        public const uint PDH_ENTRY_NOT_IN_LOG_FILE = 0xC0000BCD;
        public const uint PDH_DATA_SOURCE_IS_LOG_FILE = 0xC0000BCE;
        public const uint PDH_DATA_SOURCE_IS_REAL_TIME = 0xC0000BCF;
        public const uint PDH_UNABLE_READ_LOG_HEADER = 0xC0000BD0;
        public const uint PDH_FILE_NOT_FOUND = 0xC0000BD1;
        public const uint PDH_FILE_ALREADY_EXISTS = 0xC0000BD2;
        public const uint PDH_NOT_IMPLEMENTED = 0xC0000BD3;
        public const uint PDH_STRING_NOT_FOUND = 0xC0000BD4;
        public const uint PDH_UNKNOWN_LOG_FORMAT = 0xC0000BD6;
        public const uint PDH_UNKNOWN_LOGSVC_COMMAND = 0xC0000BD7;
        public const uint PDH_LOGSVC_QUERY_NOT_FOUND = 0xC0000BD8;
        public const uint PDH_LOGSVC_NOT_OPENED = 0xC0000BD9;
        public const uint PDH_WBEM_ERROR = 0xC0000BDA;
        public const uint PDH_ACCESS_DENIED = 0xC0000BDB;
        public const uint PDH_LOG_FILE_TOO_SMALL = 0xC0000BDC;
        public const uint PDH_INVALID_DATASOURCE = 0xC0000BDD;
        public const uint PDH_INVALID_SQLDB = 0xC0000BDE;
        public const uint PDH_NO_COUNTERS = 0xC0000BDF;
        public const uint PDH_SQL_ALLOC_FAILED = 0xC0000BE0;
        public const uint PDH_SQL_ALLOCCON_FAILED = 0xC0000BE1;
        public const uint PDH_SQL_EXEC_DIRECT_FAILED = 0xC0000BE2;
        public const uint PDH_SQL_FETCH_FAILED = 0xC0000BE3;
        public const uint PDH_SQL_ROWCOUNT_FAILED = 0xC0000BE4;
        public const uint PDH_SQL_MORE_RESULTS_FAILED = 0xC0000BE5;
        public const uint PDH_SQL_CONNECT_FAILED = 0xC0000BE6;
        public const uint PDH_SQL_BIND_FAILED = 0xC0000BE7;
        public const uint PDH_CANNOT_CONNECT_WMI_SERVER = 0xC0000BE8;
        public const uint PDH_PLA_COLLECTION_ALREADY_RUNNING = 0xC0000BE9;
        public const uint PDH_PLA_ERROR_SCHEDULE_OVERLAP = 0xC0000BEA;
        public const uint PDH_PLA_COLLECTION_NOT_FOUND = 0xC0000BEB;
        public const uint PDH_PLA_ERROR_SCHEDULE_ELAPSED = 0xC0000BEC;
        public const uint PDH_PLA_ERROR_NOSTART = 0xC0000BED;
        public const uint PDH_PLA_ERROR_ALREADY_EXISTS = 0xC0000BEE;
        public const uint PDH_PLA_ERROR_TYPE_MISMATCH = 0xC0000BEF;
        public const uint PDH_PLA_ERROR_FILEPATH = 0xC0000BF0;
        public const uint PDH_PLA_SERVICE_ERROR = 0xC0000BF1;
        public const uint PDH_PLA_VALIDATION_ERROR = 0xC0000BF2;
        public const uint PDH_PLA_ERROR_NAME_TOO_LONG = 0xC0000BF4;
        public const uint PDH_INVALID_SQL_LOG_FORMAT = 0xC0000BF5;
        public const uint PDH_COUNTER_ALREADY_IN_QUERY = 0xC0000BF6;
        public const uint PDH_BINARY_LOG_CORRUPT = 0xC0000BF7;
        public const uint PDH_LOG_SAMPLE_TOO_SMALL = 0xC0000BF8;
        public const uint PDH_OS_LATER_VERSION = 0xC0000BF9;
        public const uint PDH_OS_EARLIER_VERSION = 0xC0000BFA;
        public const uint PDH_INCORRECT_APPEND_TIME = 0xC0000BFB;
        public const uint PDH_UNMATCHED_APPEND_COUNTER = 0xC0000BFC;
        public const uint PDH_SQL_ALTER_DETAIL_FAILED = 0xC0000BFD;
        public const uint PDH_QUERY_PERF_DATA_TIMEOUT = 0xC0000BFE;
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

    internal sealed class PdhHelper : IDisposable
    {
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
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PDH_FMT_COUNTERVALUE_DOUBLE
        {
            public uint CStatus;

            public double doubleValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PDH_FMT_COUNTERVALUE_UNICODE
        {
            public uint CStatus;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string WideStringValue;
        }

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

        //
        // This is the structure returned by PdhGetCounterInfo().
        // We only need dwType and lDefaultScale fields from this structure.
        // We access those fields directly. The struct is here for reference only.
        //
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct PDH_COUNTER_INFO
        {
            public uint Length;
            public uint Type;
            public uint CVersion;
            public uint CStatus;
            public int Scale;
            public int DefaultScale;
            public ulong UserData;
            public ulong QueryUserData;
            public ushort* FullPath;
            public _Anonymous_e__Union Anonymous;
            public ushort* ExplainText;
            public fixed uint DataBuffer[1];

            [StructLayout(LayoutKind.Explicit)]
            internal struct _Anonymous_e__Union
            {
                [FieldOffset(0)]
                public PDH_DATA_ITEM_PATH_ELEMENTS_blittable DataItemPath;

                [FieldOffset(0)]
                public PDH_COUNTER_PATH_ELEMENTS_blittable CounterPath;

                [FieldOffset(0)]
                public _Anonymous_e__Struct Anonymous;

                [StructLayout(LayoutKind.Sequential)]
                internal struct PDH_DATA_ITEM_PATH_ELEMENTS_blittable
                {
                    public ushort* MachineName;
                    public Guid ObjectGUID;
                    public uint ItemId;
                    public ushort* InstanceName;
                }

                [StructLayout(LayoutKind.Sequential)]
                internal struct PDH_COUNTER_PATH_ELEMENTS_blittable
                {
                    public ushort* MachineName;
                    public ushort* ObjectName;
                    public ushort* InstanceName;
                    public ushort* ParentInstance;
                    public uint InstanceIndex;
                    public ushort* CounterName;
                }

                [StructLayout(LayoutKind.Sequential)]
                internal struct _Anonymous_e__Struct
                {
                    public ushort* MachineName;
                    public ushort* ObjectName;
                    public ushort* InstanceName;
                    public ushort* ParentInstance;
                    public uint InstanceIndex;
                    public ushort* CounterName;
                }
            }
        }

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhBindInputDataSource(out PdhSafeDataSourceHandle phDataSource, string szLogFileNameList);

        [DllImport("pdh.dll")]
        private static extern uint PdhOpenQueryH(PdhSafeDataSourceHandle hDataSource, IntPtr dwUserData, out PdhSafeQueryHandle phQuery);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhAddCounter(PdhSafeQueryHandle queryHandle, string counterPath, IntPtr userData, out IntPtr counterHandle);

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

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhGetCounterInfo(IntPtr hCounter, [MarshalAs(UnmanagedType.U1)] bool bRetrieveExplainText, ref IntPtr pdwBufferSize, IntPtr lpBuffer);

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
        private readonly Dictionary<string, CounterHandleNInstance> _consumerPathToHandleAndInstanceMap = new();

        /// <summary>
        /// A helper reading in a Unicode string with embedded NULLs and splitting it into a StringCollection.
        /// </summary>
        /// <param name="strNative"></param>
        /// <param name="strSize"></param>
        /// <param name="strColl"></param>
        private static void ReadPdhMultiString(ref IntPtr strNative, Int32 strSize, ref StringCollection strColl)
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

        private static uint GetCounterInfoPlus(IntPtr hCounter, out UInt32 counterType, out UInt32 defaultScale, out UInt64 timeBase)
        {
            counterType = 0;
            defaultScale = 0;
            timeBase = 0;

            Debug.Assert(hCounter != IntPtr.Zero);

            IntPtr pBufferSize = new(0);
            uint res = PdhGetCounterInfo(hCounter, false, ref pBufferSize, IntPtr.Zero);
            if (res != PdhResults.PDH_MORE_DATA)
            {
                return res;
            }

            Int32 bufSize = pBufferSize.ToInt32();
            IntPtr bufCounterInfo = Marshal.AllocHGlobal(bufSize);

            try
            {
                res = PdhGetCounterInfo(hCounter, false, ref pBufferSize, bufCounterInfo);
                if (res == PdhResults.PDH_CSTATUS_VALID_DATA && bufCounterInfo != IntPtr.Zero)
                {
                    PDH_COUNTER_INFO pdhCounterInfo = (PDH_COUNTER_INFO)Marshal.PtrToStructure(bufCounterInfo, typeof(PDH_COUNTER_INFO));
                    counterType = pdhCounterInfo.Type;
                    defaultScale = (uint)pdhCounterInfo.DefaultScale;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(bufCounterInfo);
            }

            res = PdhGetCounterTimeBase(hCounter, out timeBase);
            if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
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
            if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
            {
                // Console.WriteLine("error in PdhBindInputDataSource: " + res);
                return res;
            }

            return PdhResults.PDH_CSTATUS_VALID_DATA;
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
            if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
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

            if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
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

            PDH_TIME_INFO pTimeInfo = new();

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
            IntPtr MachineListTcharSizePtr = new(0);
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
                if (res == PdhResults.PDH_CSTATUS_VALID_DATA)
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
            IntPtr pBufferSize = new(0);
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
                if (res == PdhResults.PDH_CSTATUS_VALID_DATA)
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
            IntPtr pCounterBufferSize = new(0);
            IntPtr pInstanceBufferSize = new(0);

            uint res = PdhEnumObjectItemsH(_hDataSource, machineName, objectName,
                                            IntPtr.Zero, ref pCounterBufferSize,
                                            IntPtr.Zero, ref pInstanceBufferSize,
                                            PerfDetail.PERF_DETAIL_WIZARD, 0);
            if (res == PdhResults.PDH_CSTATUS_NO_INSTANCE)
            {
                instanceNames.Clear();
                return PdhResults.PDH_CSTATUS_VALID_DATA; // masking the error
            }
            else if (res == PdhResults.PDH_CSTATUS_NO_OBJECT)
            {
                counterNames.Clear();
                return PdhResults.PDH_CSTATUS_VALID_DATA; // masking the error
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
                if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
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

            StringCollection machineNames = new();
            uint res = this.EnumBlgFilesMachines(ref machineNames);
            if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
            {
                return res;
            }

            foreach (string machine in machineNames)
            {
                StringCollection counterSets = new();
                res = this.EnumObjects(machine, ref counterSets);
                if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
                {
                    return res;
                }

                foreach (string counterSet in counterSets)
                {
                    // Console.WriteLine("Counter set " + counterSet);

                    StringCollection counterSetCounters = new();
                    StringCollection counterSetInstances = new();

                    res = this.EnumObjectItems(machine, counterSet, ref counterSetCounters, ref counterSetInstances);
                    if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
                    {
                        return res;
                    }

                    res = this.GetValidPaths(machine, counterSet, ref counterSetCounters, ref counterSetInstances, ref validPaths);
                    if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
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
            IntPtr pPathBufferSize = new(0);

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
                if (res == PdhResults.PDH_CSTATUS_VALID_DATA)
                {
                    outPath = Marshal.PtrToStringUni(strPath);

                    ret = (PdhValidatePathEx(_hDataSource, outPath) == PdhResults.PDH_CSTATUS_VALID_DATA);
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
            return (PdhValidatePathEx(_hDataSource, path) == PdhResults.PDH_CSTATUS_VALID_DATA);
        }

        private static uint MakePath(PDH_COUNTER_PATH_ELEMENTS pathElts, out string outPath, bool bWildcardInstances)
        {
            outPath = string.Empty;
            IntPtr pPathBufferSize = new(0);

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
                if (res == PdhResults.PDH_CSTATUS_VALID_DATA)
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

        private static uint MakeAllInstancePath(string origPath, out string unifiedPath)
        {
            unifiedPath = origPath;

            PDH_COUNTER_PATH_ELEMENTS elts = new();

            uint res = ParsePath(origPath, ref elts);
            if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
            {
                return res;
            }

            return MakePath(elts, out unifiedPath, true);
        }

        private static uint ParsePath(string fullPath, ref PDH_COUNTER_PATH_ELEMENTS pCounterPathElements)
        {
            IntPtr bufSize = new(0);

            uint res = PdhParseCounterPath(fullPath,
                                           IntPtr.Zero,
                                           ref bufSize,
                                           0);
            if (res != PdhResults.PDH_MORE_DATA && res != PdhResults.PDH_CSTATUS_VALID_DATA)
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
                if (res == PdhResults.PDH_CSTATUS_VALID_DATA)
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
            uint res = PdhResults.PDH_CSTATUS_VALID_DATA;
            localizedPath = string.Empty;
            PDH_COUNTER_PATH_ELEMENTS pathElts = new();
            res = ParsePath(englishPath, ref pathElts);
            if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
            {
                return res;
            }

            // Check if the path is local and assert if not:
            string machineNameMassaged = pathElts.MachineName.TrimStart('\\');
            Debug.Assert(machineNameMassaged.Equals(System.Environment.MachineName, StringComparison.OrdinalIgnoreCase));

            // Get the registry index
            RegistryKey rootKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Perflib\\009");
            string[] regCounters = (string[])rootKey.GetValue("Counter");

            // NOTE: 1-based enumeration because the name strings follow index strings in the array
            Int32 counterIndex = -1;
            Int32 objIndex = -1;
            for (int enumIndex = 1; enumIndex < regCounters.Length; enumIndex++)
            {
                string regString = regCounters[enumIndex];
                if (regString.Equals(pathElts.CounterName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        counterIndex = Convert.ToInt32(regCounters[enumIndex - 1], CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        return PdhResults.PDH_INVALID_PATH;
                    }
                }
                else if (regString.Equals(pathElts.ObjectName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        objIndex = Convert.ToInt32(regCounters[enumIndex - 1], CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        return PdhResults.PDH_INVALID_PATH;
                    }
                }

                if (counterIndex != -1 && objIndex != -1)
                {
                    break;
                }
            }

            if (counterIndex == -1 || objIndex == -1)
            {
                return PdhResults.PDH_INVALID_PATH;
            }

            // Now, call retrieve the localized names of the object and the counter by index:
            string objNameLocalized;
            res = LookupPerfNameByIndex(pathElts.MachineName, (uint)objIndex, out objNameLocalized);
            if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
            {
                return res;
            }

            pathElts.ObjectName = objNameLocalized;

            string ctrNameLocalized;
            res = LookupPerfNameByIndex(pathElts.MachineName, (uint)counterIndex, out ctrNameLocalized);
            if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
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
            uint res;
            try
            {
                res = PdhLookupPerfNameByIndex(machineName, index, localizedPathPtr, ref strSize);
                if (res == PdhResults.PDH_MORE_DATA)
                {
                    Marshal.FreeHGlobal(localizedPathPtr);
                    localizedPathPtr = Marshal.AllocHGlobal(strSize * sizeof(char));
                    res = PdhLookupPerfNameByIndex(machineName, index, localizedPathPtr, ref strSize);
                }

                if (res == PdhResults.PDH_CSTATUS_VALID_DATA)
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
            PDH_COUNTER_PATH_ELEMENTS pathElts = new();
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

            return PdhResults.PDH_CSTATUS_VALID_DATA;
        }

        public uint AddCounters(ref StringCollection validPaths, bool bFlushOldCounters)
        {
            Debug.Assert(_hQuery != null && !_hQuery.IsInvalid);

            if (bFlushOldCounters)
            {
                _consumerPathToHandleAndInstanceMap.Clear();
            }

            bool bAtLeastOneAdded = false;
            uint res = PdhResults.PDH_CSTATUS_VALID_DATA;

            foreach (string counterPath in validPaths)
            {
                IntPtr counterHandle;
                res = PdhAddCounter(_hQuery, counterPath, IntPtr.Zero, out counterHandle);
                if (res == PdhResults.PDH_CSTATUS_VALID_DATA)
                {
                    CounterHandleNInstance chi = new();
                    chi.hCounter = counterHandle;
                    chi.InstanceName = null;

                    PDH_COUNTER_PATH_ELEMENTS pathElts = new();
                    res = ParsePath(counterPath, ref pathElts);
                    if (res == PdhResults.PDH_CSTATUS_VALID_DATA && pathElts.InstanceName != null)
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

            return bAtLeastOneAdded ? PdhResults.PDH_CSTATUS_VALID_DATA : res;
        }

        public string GetCounterSetHelp(string szMachineName, string szObjectName)
        {
            // API not available to retrieve
            return string.Empty;
        }

        public uint ReadNextSet(out PerformanceCounterSampleSet nextSet, bool bSkipReading)
        {
            Debug.Assert(_hQuery != null && !_hQuery.IsInvalid);

            uint res = PdhResults.PDH_CSTATUS_VALID_DATA;
            nextSet = null;

            Int64 batchTimeStampFT = 0;

            res = PdhCollectQueryDataWithTime(_hQuery, ref batchTimeStampFT);
            if (bSkipReading)
            {
                return res;
            }

            if (res != PdhResults.PDH_CSTATUS_VALID_DATA && res != PdhResults.PDH_NO_DATA)
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
            uint lastErr = PdhResults.PDH_CSTATUS_VALID_DATA;

            foreach (string path in _consumerPathToHandleAndInstanceMap.Keys)
            {
                IntPtr counterTypePtr = new(0);
                UInt32 counterType = (UInt32)PerformanceCounterType.RawBase;
                UInt32 defaultScale = 0;
                UInt64 timeBase = 0;

                IntPtr hCounter = _consumerPathToHandleAndInstanceMap[path].hCounter;
                Debug.Assert(hCounter != IntPtr.Zero);

                res = GetCounterInfoPlus(hCounter, out counterType, out defaultScale, out timeBase);
                if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
                {
                    // Console.WriteLine ("GetCounterInfoPlus for " + path + " failed with " + res);
                }

                PDH_RAW_COUNTER rawValue;
                res = PdhGetRawCounterValue(hCounter, out counterTypePtr, out rawValue);
                if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
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
                                           (rawValue.CStatus == PdhResults.PDH_CSTATUS_VALID_DATA) ? res : rawValue.CStatus);

                    numInvalidDataSamples++;
                    lastErr = res;
                    continue;
                }

                long dtFT = (((long)rawValue.TimeStamp.dwHighDateTime) << 32) +
                                     (uint)rawValue.TimeStamp.dwLowDateTime;

                DateTime dt = new(DateTime.FromFileTimeUtc(dtFT).Ticks, DateTimeKind.Local);

                PDH_FMT_COUNTERVALUE_DOUBLE fmtValueDouble;
                res = PdhGetFormattedCounterValue(hCounter,
                                                  PdhFormat.PDH_FMT_DOUBLE | PdhFormat.PDH_FMT_NOCAP100,
                                                  out counterTypePtr,
                                                  out fmtValueDouble);
                if (res != PdhResults.PDH_CSTATUS_VALID_DATA)
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
                                           (fmtValueDouble.CStatus == PdhResults.PDH_CSTATUS_VALID_DATA) ? res : rawValue.CStatus);

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
                res = PdhResults.PDH_CSTATUS_VALID_DATA;
            }

            return res;
        }

        public uint ExpandWildCardPath(string path, out StringCollection expandedPaths)
        {
            expandedPaths = new StringCollection();
            IntPtr pcchPathListLength = new(0);

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
                if (res == PdhResults.PDH_CSTATUS_VALID_DATA)
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
    }
}
