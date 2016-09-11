//
//    Copyright (C) Microsoft.  All rights reserved.
//

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    internal class WindowsErrorReporting
    {
        #region Native enums

        /// <summary>
        /// Copied from \\shindex\winmain\sdpublic\sdk\inc\werapi.h
        /// </summary>
        [Flags]
        private enum DumpFlags : uint
        {
            /// <summary>
            /// If the report is being queued, do not include a heap dump. Using this flag saves disk space.
            /// </summary>
            NoHeap_OnQueue = 1
        }

        /// <summary>
        /// Copied from \\shindex\winmain\sdpublic\sdk\inc\werapi.h
        /// </summary>
        private enum DumpType : uint
        {
            MicroDump = 1,
            MiniDump = 2,
            HeapDump = 3
        }

        /// <summary>
        /// Copied from \\shindex\winmain\sdpublic\sdk\inc\werapi.h (with adapted names)
        /// </summary>
        private enum BucketParameterId : uint
        {
            NameOfExe = 0,
            FileVersionOfSystemManagementAutomation = 1,
            InnermostExceptionType = 2,
            OutermostExceptionType = 3,
            DeepestPowerShellFrame = 4,
            DeepestFrame = 5,
            ThreadName = 6,
            Param7 = 7,
            Param8 = 8,
            Param9 = 9,
        }

        /// <summary>
        /// Copied from \\shindex\winmain\sdpublic\sdk\inc\werapi.h
        /// </summary>
        private enum ReportType : uint
        {
            WerReportNonCritical = 0,
            WerReportCritical = 1,
            WerReportApplicationCrash = 2,
            WerReportApplicationHang = 3,
            WerReportKernel = 4,
            WerReportInvalid = 5
        }

        /// <summary>
        /// <para>
        /// Identifies the type of information that will be written to the minidump file by the MiniDumpWriteDump function
        /// </para>
        /// <para>
        /// More info:
        /// http://msdn.microsoft.com/en-us/library/ms680519(VS.85).aspx
        /// http://www.debuginfo.com/articles/effminidumps.html
        /// </para>
        /// </summary>
        [Flags]
        internal enum MiniDumpType : uint
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000
        }

        /// <summary>
        /// The consent status.
        /// Copied from \\shindex\winmain\sdpublic\sdk\inc\werapi.h
        /// </summary>
        private enum Consent : uint
        {
            NotAsked = 1,
            Approved = 2,
            Denied = 3,
            AlwaysPrompt = 4
        }

        /// <summary>
        /// Used in <see cref="NativeMethods.WerReportSubmit"/>.
        /// Copied from \\shindex\winmain\sdpublic\sdk\inc\werapi.h
        /// </summary>
        [Flags]
        private enum SubmitFlags : uint
        {
            /// <summary>
            /// Honor any recovery registration for the application. For more information, see RegisterApplicationRecoveryCallback.
            /// </summary>
            HonorRecovery = 1,

            /// <summary>
            /// Honor any restart registration for the application. For more information, see RegisterApplicationRestart.
            /// </summary>
            HonorRestart = 2,

            /// <summary>
            /// Add the report to the WER queue without notifying the user. The report is queued only—reporting (sending the report to Microsoft) occurs later based on the user's consent level.
            /// </summary>
            Queue = 4,

            /// <summary>
            /// Show the debug button.
            /// </summary>
            ShowDebug = 8,

            /// <summary>
            /// Add the data registered by WerSetFlags, WerRegisterFile, and WerRegisterMemoryBlock to the report.
            /// </summary>
            AddRegisteredData = 16,

            /// <summary>
            /// Spawn another process to submit the report. The calling thread is blocked until the function returns.
            /// </summary>
            OutOfProcess = 32,

            /// <summary>
            /// Do not display the close dialog box for the critical report.
            /// </summary>
            NoCloseUI = 64,

            /// <summary>
            /// Do not queue the report. If there is adequate user consent the report is sent to Microsoft immediately; otherwise, the report is discarded. You may use this flag for non-critical reports.
            /// The report is discarded for any action that would require the report to be queued. For example, if the computer is offline when you submit the report, the report is discarded. Also, if there is insufficient consent (for example, consent was required for the data portion of the report), the report is discarded.
            /// </summary>
            NoQueue = 128,

            /// <summary>
            /// Do not archive the report.
            /// </summary>
            NoArchive = 256,

            /// <summary>
            /// The initial UI is minimized and flashing.
            /// </summary>
            StartMinimized = 512,

            /// <summary>
            /// Spawn another process to submit the report and return from this function call immediately. Note that the contents of the pSubmitResult parameter are undefined and there is no way to query when the reporting completes or the completion status.
            /// </summary>
            OutOfProcesAsync = 1024,

            BypassDataThrottling = 2048,
            ArchiveParametersOnly = 4096,
        }

        /// <summary>
        /// Used in <see cref="NativeMethods.WerReportSubmit"/>.
        /// Copied from \\shindex\winmain\sdpublic\sdk\inc\werapi.h
        /// </summary>
        private enum SubmitResult : uint
        {
            /// <summary>
            /// The report was queued.
            /// </summary>
            ReportQueued = 1,

            /// <summary>
            /// The report was uploaded.
            /// </summary>
            ReportUploaded = 2,

            /// <summary>
            /// The Debug button was clicked.
            /// </summary>
            ReportDebug = 3,

            /// <summary>
            /// The report submission failed.
            /// </summary>
            ReportFailed = 4,

            /// <summary>
            /// Error reporting was disabled.
            /// </summary>
            Disabled = 5,

            /// <summary>
            /// The report was canceled.
            /// </summary>
            ReportCancelled = 6,

            /// <summary>
            /// Queuing was disabled.
            /// </summary>
            DisabledQueue = 7,

            /// <summary>
            /// The report was asynchronous.
            /// </summary>
            ReportAsync = 8,

            CustomAction = 9
        }

        /// <summary>
        /// The fault reporting settings. Used in <see cref="NativeMethods.WerSetFlags"/>.
        /// Copied from \\shindex\winmain\sdpublic\sdk\inc\werapi.h
        /// </summary>
        private enum ReportingFlags : uint
        {
            /// <summary>
            /// Do not add heap dumps for reports for the process
            /// </summary>
            NoHeap = 1,

            /// <summary>
            /// Queue critical reports for this process
            /// </summary>
            Queue = 2,

            /// <summary>
            /// Do not suspend the process before error reporting
            /// </summary>
            DisableThreadSuspension = 4,

            /// <summary>
            /// Queue critical reports for this process and upload from the queue
            /// </summary>
            QueueUpload = 8,
        }

        #endregion

        #region Native handles

        /// <summary>
        /// Wraps HREPORT value returned by <see cref="NativeMethods.WerReportCreate"/>
        /// </summary>
        private class ReportHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
        {
            private ReportHandle()
                : base(true /* ownsHandle */)
            {
            }

            protected override bool ReleaseHandle()
            {
                return 0 == NativeMethods.WerReportCloseHandle(this.handle);
            }
        }

        #endregion

        #region Native structures

        /// <summary>
        /// Contains information used by the WerReportCreate function.
        /// http://msdn.microsoft.com/en-us/library/bb513637(VS.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class ReportInformation
        {
            /// <summary>
            /// The size of this structure, in bytes.
            /// </summary>
            internal Int32 dwSize;

            /// <summary>
            /// A handle to the process for which the report is being generated. If this member is NULL, this is the calling process.
            /// </summary>
            internal IntPtr hProcess;

            /// <summary>
            /// The name used to look up consent settings. If this member is empty, the default is the name specified by the pwzEventType parameter of WerReportCreate.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            internal string wzConsentKey;

            /// <summary>
            /// The display name. If this member is empty, the default is the name specified by pwzEventType parameter of WerReportCreate.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string wzFriendlyEventName;

            /// <summary>
            /// The name of the application. If this parameter is empty, the default is the base name of the image file.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string wzApplicationName;

            /// <summary>
            /// The full path to the application.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            internal string wzApplicationPath;
            private const int MAX_PATH = 260; // copied from sdpublic\sdk\inc\windef.h:57

            /// <summary>
            /// A description of the problem. This description is displayed in Problem Reports and Solutions.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
            internal string wzDescription;

            /// <summary>
            /// A handle to the parent window.
            /// </summary>
            internal IntPtr hwndParent;
        }

        #endregion

        #region Native methods

        private static class NativeMethods
        {
            internal const string WerDll = "wer.dll";

            /// <summary>
            /// Creates a problem report that describes an application event.
            /// </summary>
            /// <param name="pwzEventType">A pointer to a Unicode string that specifies the name of the event. To register an event that can be used by your application, see About Windows Error Reporting for Software.</param>
            /// <param name="repType">The type of report.</param>
            /// <param name="reportInformation"></param>
            /// <param name="reportHandle">A handle to the report. If the function fails, this handle is NULL.</param>
            /// <returns>hresult</returns>
            [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "ReportInformation.wzDescription")]
            [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "ReportInformation.wzApplicationPath")]
            [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "ReportInformation.wzFriendlyEventName")]
            [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "ReportInformation.wzConsentKey")]
            [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "ReportInformation.wzApplicationName")]
            [DllImport(WerDll, CharSet = CharSet.Unicode)]
            internal static extern int WerReportCreate(
                [MarshalAs(UnmanagedType.LPWStr)] string pwzEventType,
                ReportType repType,
                [MarshalAs(UnmanagedType.LPStruct)] ReportInformation reportInformation,
                out ReportHandle reportHandle);

            /// <summary>
            /// Sets the parameters that uniquely identify an event.
            /// http://msdn.microsoft.com/en-us/library/bb513626(VS.85).aspx
            /// </summary>
            /// <param name="reportHandle">A handle to the report. This handle is returned by the <see cref="WerReportCreate"/> function.</param>
            /// <param name="bucketParameterId">The identifier of the parameter to be set.</param>
            /// <param name="name">A pointer to a Unicode string that contains the name of the event. If this parameter is NULL, the default name is Px, where x matches the integer portion of the value specified in dwparamID.</param>
            /// <param name="value">The parameter value</param>
            /// <returns>hresult</returns>
            [DllImport(WerDll, CharSet = CharSet.Unicode)]
            internal static extern int WerReportSetParameter(
                ReportHandle reportHandle,
                BucketParameterId bucketParameterId,
                [MarshalAs(UnmanagedType.LPWStr)] string name,
                [MarshalAs(UnmanagedType.LPWStr)] string value);

            /// <summary>
            /// Adds a dump of the specified type to the specified report.
            /// http://msdn.microsoft.com/en-us/library/bb513622(VS.85).aspx
            /// </summary>
            /// <param name="reportHandle">A handle to the report. This handle is returned by the <see cref="WerReportCreate"/> function.</param>
            /// <param name="hProcess">A handle to the process for which the report is being generated. This handle must have the STANDARD_RIGHTS_READ and PROCESS_QUERY_INFORMATION access rights.</param>
            /// <param name="hThread">Optional.  A handle to the thread of hProcess for which the report is being generated. If dumpType is WerDumpTypeMicro, this parameter is required.</param>
            /// <param name="dumpType">The type of minidump.</param>
            /// <param name="pExceptionParam">Optional. A pointer to a WER_EXCEPTION_INFORMATION structure that specifies exception information.</param>
            /// <param name="dumpCustomOptions">Optional.  Specifies custom minidump options. If this parameter is <c>null</c>, the standard minidump information is collected.</param>
            /// <param name="dumpFlags"></param>
            /// <returns></returns>
            [DllImport(WerDll, CharSet = CharSet.Unicode)]
            internal static extern int WerReportAddDump(
                ReportHandle reportHandle,
                IntPtr hProcess,
                IntPtr hThread,
                DumpType dumpType,
                IntPtr pExceptionParam,
                IntPtr dumpCustomOptions,
                DumpFlags dumpFlags);

            /// <summary>
            /// Submits the specified report.
            /// </summary>
            /// <param name="reportHandle">A handle to the report. This handle is returned by the WerReportCreate function.</param>
            /// <param name="consent">The consent status.</param>
            /// <param name="flags"></param>
            /// <param name="result">The result of the submission.</param>
            /// <returns>hresult</returns>
            [DllImport(WerDll)]
            internal static extern int WerReportSubmit(
                ReportHandle reportHandle,
                Consent consent,
                SubmitFlags flags,
                out SubmitResult result);

            /// <summary>
            /// Closes the specified report (to be used only from SafeHandle class).
            /// http://msdn.microsoft.com/en-us/library/bb513624(VS.85).aspx
            /// </summary>
            /// <param name="reportHandle">Handle returned by <see cref="WerReportCreate"/></param>
            /// <returns>hresult</returns>
            [DllImport(WerDll)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static extern int WerReportCloseHandle(IntPtr reportHandle);

            /// <summary>
            /// Sets the fault reporting settings for the current process.
            /// </summary>
            /// <param name="flags">The fault reporting settings.</param>
            /// <returns>hresult</returns>
            [DllImport("kernel32.dll")]
            internal static extern int WerSetFlags(ReportingFlags flags);

            /// <summary>
            /// Writes user-mode minidump information to the specified file.
            /// </summary>
            /// <param name="hProcess">A handle to the process for which the information is to be generated.</param>
            /// <param name="processId">The identifier of the process for which the information is to be generated.</param>
            /// <param name="hFile">A handle to the file in which the information is to be written.</param>
            /// <param name="dumpType">The type of dump to be generated.</param>
            /// <param name="exceptionParam">A pointer to a MINIDUMP_EXCEPTION_INFORMATION structure describing the client exception that caused the minidump to be generated. If the value of this parameter is NULL, no exception information is included in the minidump file.</param>
            /// <param name="userStreamParam">A pointer to a MINIDUMP_USER_STREAM_INFORMATION structure. If the value of this parameter is NULL, no user-defined information is included in the minidump file.</param>
            /// <param name="callackParam">A pointer to a MINIDUMP_CALLBACK_INFORMATION structure that specifies a callback routine which is to receive extended minidump information. If the value of this parameter is NULL, no callbacks are performed.</param>
            /// <returns></returns>
            [DllImport("DbgHelp.dll", SetLastError = true)]
            internal static extern bool MiniDumpWriteDump(
                IntPtr hProcess,
                Int32 processId,
                Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
                MiniDumpType dumpType,
                IntPtr exceptionParam,
                IntPtr userStreamParam,
                IntPtr callackParam);
        }

        #endregion Native methods

        #region Our bucketing logic

        private static string TruncateExeName(string nameOfExe, int maxLength)
        {
            nameOfExe = nameOfExe.Trim();

            if (nameOfExe.Length > maxLength)
            {
                const string exeSuffix = ".exe";
                if (nameOfExe.EndsWith(exeSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    nameOfExe = nameOfExe.Substring(0, nameOfExe.Length - exeSuffix.Length);
                }
            }

            return TruncateBucketParameter(nameOfExe, maxLength);
        }

        private static string TruncateTypeName(string typeName, int maxLength)
        {
            if (typeName.Length > maxLength)
            {
                typeName = typeName.Substring(typeName.Length - maxLength, maxLength);
            }

            return typeName;
        }

        private static string TruncateExceptionType(string exceptionType, int maxLength)
        {
            if (exceptionType.Length > maxLength)
            {
                const string exceptionSuffix = "Exception";
                if (exceptionType.EndsWith(exceptionSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    exceptionType = exceptionType.Substring(0, exceptionType.Length - exceptionSuffix.Length);
                }
            }

            if (exceptionType.Length > maxLength)
            {
                exceptionType = TruncateTypeName(exceptionType, maxLength);
            }

            return TruncateBucketParameter(exceptionType, maxLength);
        }

        private static string TruncateBucketParameter(string message, int maxLength)
        {
            if (message == null)
                return string.Empty;

            const string ellipsis = "..";
            int prefixLength = maxLength * 30 / 100;

            Debug.Assert(maxLength >= ellipsis.Length);
            Debug.Assert(prefixLength >= 0);
            Debug.Assert(prefixLength < (maxLength - ellipsis.Length));

            if (message.Length > maxLength)
            {
                int suffixLength = maxLength - prefixLength - ellipsis.Length;
                Debug.Assert((prefixLength + ellipsis.Length + suffixLength) <= maxLength);
                message = message.Substring(0, prefixLength) +
                    ellipsis +
                    message.Substring(message.Length - suffixLength, suffixLength);
            }

            Debug.Assert(message.Length <= maxLength, "message truncation algorithm works");
            return message;
        }

        private static string StackFrame2BucketParameter(StackFrame frame, int maxLength)
        {
            MethodBase method = frame.GetMethod();
            if (method == null)
            {
                return string.Empty;
            }

            Type type = method.DeclaringType;
            if (type == null)
            {
                string rest = method.Name;
                return TruncateBucketParameter(rest, maxLength);
            }
            else
            {
                string typeName = type.FullName;
                string rest = "." + method.Name;

                if (maxLength > rest.Length)
                {
                    typeName = TruncateTypeName(typeName, maxLength - rest.Length);
                }
                else
                {
                    typeName = TruncateTypeName(typeName, 1);
                }

                return TruncateBucketParameter(typeName + rest, maxLength);
            }
        }

        /// <summary>
        /// Returns the deepest frame.
        /// </summary>
        /// <param name="exception">exception with stack trace to analyze</param>
        /// <param name="maxLength">maximum length of the returned string</param>
        /// <returns>frame string</returns>
        private static string GetDeepestFrame(Exception exception, int maxLength)
        {
            StackTrace stackTrace = new StackTrace(exception);
            StackFrame frame = stackTrace.GetFrame(0);
            return StackFrame2BucketParameter(frame, maxLength);
        }

        private static readonly string[] s_powerShellModulesWithoutGlobalMembers = new string[] {
            "Microsoft.PowerShell.Commands.Diagnostics.dll",
            "Microsoft.PowerShell.Commands.Management.dll",
            "Microsoft.PowerShell.Commands.Utility.dll",
            "Microsoft.PowerShell.Security.dll",
            "System.Management.Automation.dll",
            "Microsoft.PowerShell.ConsoleHost.dll",
            "Microsoft.PowerShell.Editor.dll",
            "Microsoft.PowerShell.GPowerShell.dll",
            "Microsoft.PowerShell.GraphicalHost.dll"
        };

        private static readonly string[] s_powerShellModulesWithGlobalMembers = new string[] {
            "powershell.exe",
            "powershell_ise.exe",
            "pwrshplugin.dll",
            "pwrshsip.dll",
            "pshmsglh.dll",
            "PSEvents.dll"
        };

        private static bool IsPowerShellModule(string moduleName, bool globalMember)
        {
            foreach (string powerShellModule in s_powerShellModulesWithGlobalMembers)
            {
                if (moduleName.Equals(powerShellModule, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (!globalMember)
            {
                foreach (string powerShellModule in s_powerShellModulesWithoutGlobalMembers)
                {
                    if (moduleName.Equals(powerShellModule, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the deepest frame belonging to powershell.
        /// </summary>
        /// <param name="exception">exception with stack trace to analyze</param>
        /// <param name="maxLength">maximum length of the returned string</param>
        /// <returns>frame string</returns>
        private static string GetDeepestPowerShellFrame(Exception exception, int maxLength)
        {
            StackTrace stackTrace = new StackTrace(exception);
            foreach (StackFrame frame in stackTrace.GetFrames())
            {
                MethodBase method = frame.GetMethod();
                if (method != null)
                {
                    Module module = method.Module;
                    if (module != null)
                    {
                        Type type = method.DeclaringType;
                        string moduleName = module.Name;
                        if (IsPowerShellModule(moduleName, type == null))
                        {
                            return StackFrame2BucketParameter(frame, maxLength);
                        }
                    }
                }
            }

            return string.Empty;
        }


        /// <exception cref="System.Runtime.InteropServices.COMException">
        /// Some failure HRESULTs map to well-defined exceptions, while others do not map
        /// to a defined exception. If the HRESULT maps to a defined exception, ThrowExceptionForHR
        /// creates an instance of the exception and throws it. Otherwise, it creates an instance 
        /// of System.Runtime.InteropServices.COMException, initializes the error code field with 
        /// the HRESULT, and throws that exception. When this method is invoked, it attempts to
        /// retrieve extra information regarding the error by using the unmanaged GetErrorInfo
        /// function.
        /// </exception>
        private static void SetBucketParameter(ReportHandle reportHandle, BucketParameterId bucketParameterId, string value)
        {
            HandleHResult(NativeMethods.WerReportSetParameter(
                reportHandle,
                bucketParameterId,
                bucketParameterId.ToString(),
                value));
        }

        private static string GetThreadName()
        {
            string threadName = System.Threading.Thread.CurrentThread.Name ?? string.Empty;
            return threadName;
        }

        private static void SetBucketParameters(ReportHandle reportHandle, Exception uncaughtException)
        {
            Exception innermostException = uncaughtException;
            while (innermostException.InnerException != null)
            {
                innermostException = innermostException.InnerException;
            }

            SetBucketParameter(
                reportHandle,
                BucketParameterId.NameOfExe,
                TruncateExeName(s_nameOfExe, 20));

            SetBucketParameter(
                reportHandle,
                BucketParameterId.FileVersionOfSystemManagementAutomation,
                TruncateBucketParameter(s_versionOfPowerShellLibraries, 16));

            SetBucketParameter(
                reportHandle,
                BucketParameterId.InnermostExceptionType,
                TruncateExceptionType(innermostException.GetType().FullName, 40));

            SetBucketParameter(
                reportHandle,
                BucketParameterId.OutermostExceptionType,
                TruncateExceptionType(uncaughtException.GetType().FullName, 40));

            SetBucketParameter(
                reportHandle,
                BucketParameterId.DeepestFrame,
                GetDeepestFrame(uncaughtException, 50));

            SetBucketParameter(
                reportHandle,
                BucketParameterId.DeepestPowerShellFrame,
                GetDeepestPowerShellFrame(uncaughtException, 50));

            SetBucketParameter(
                reportHandle,
                BucketParameterId.ThreadName,
                TruncateBucketParameter(GetThreadName(), 20));
        }

        private static string s_versionOfPowerShellLibraries = string.Empty;
        // This variable will be set during registration phase. We will
        // try to populate this using Process.MainModule but for some reason
        // if this fails, we want to provide a default value.
        private static string s_nameOfExe = "GetMainModuleError";
        private static string s_applicationName = "GetMainModuleError";
        private static string s_applicationPath = "GetMainModuleError";
        private static IntPtr s_hCurrentProcess = IntPtr.Zero;
        private static IntPtr s_hwndMainWindow = IntPtr.Zero;
        private static Process s_currentProcess = null;

        /// <exception cref="NotSupportedException">
        /// You are trying to access the MainModule property for a process that is running 
        /// on a remote computer. This property is available only for processes that are 
        /// running on the local computer.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The process Id is not available (or) The process has exited. 
        /// </exception>
        /// <exception cref="System.ComponentModel.Win32Exception">
        /// 
        /// </exception>
        private static void FindStaticInformation()
        {
            string sma = typeof(PSObject).Assembly.Location;
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(sma);
            s_versionOfPowerShellLibraries = fvi.FileVersion;

            s_currentProcess = Process.GetCurrentProcess();
            ProcessModule mainModule = PsUtils.GetMainModule(s_currentProcess);
            if (mainModule != null)
            {
                s_applicationPath = mainModule.FileName;
            }

            s_nameOfExe = Path.GetFileName(s_applicationPath);
            s_hCurrentProcess = s_currentProcess.Handle;
            s_hwndMainWindow = s_currentProcess.MainWindowHandle;
            s_applicationName = s_currentProcess.ProcessName;
        }

        #endregion

        #region Misc

        /// <summary>
        /// For the mapping from each HRESULT to its comparable exception class in the .NET Framework,
        /// see "How to: Map HRESULTs and Exceptions"->http://msdn.microsoft.com/en-us/library/9ztbc5s1.aspx.
        /// For additional information about GetErrorInfo, see the MSDN library.
        /// </summary>
        /// <param name="hresult"></param>
        /// <exception cref="System.Runtime.InteropServices.COMException">
        /// Some failure HRESULTs map to well-defined exceptions, while others do not map
        /// to a defined exception. If the HRESULT maps to a defined exception, ThrowExceptionForHR
        /// creates an instance of the exception and throws it. Otherwise, it creates an instance 
        /// of System.Runtime.InteropServices.COMException, initializes the error code field with 
        /// the HRESULT, and throws that exception. When this method is invoked, it attempts to
        /// retrieve extra information regarding the error by using the unmanaged GetErrorInfo
        /// function.
        /// </exception>
        private static void HandleHResult(int hresult)
        {
            Marshal.ThrowExceptionForHR(hresult);
        }

        private static bool? s_isWindowsErrorReportingAvailable;
        private static bool IsWindowsErrorReportingAvailable()
        {
            if (!(s_isWindowsErrorReportingAvailable.HasValue))
            {
                Version version = Environment.OSVersion.Version; // WER has been introduced in Vista
                s_isWindowsErrorReportingAvailable = (version.Major >= 6);
            }

            return s_isWindowsErrorReportingAvailable.Value;
        }

        #endregion

        #region Submitting a report

        /// <summary>
        /// Registered??? TODO/FIXME
        /// </summary>
        private const string powerShellEventType = "PowerShell";

        private static readonly object s_reportCreationLock = new object();

        /// <summary>
        /// Submits a Dr. Watson (aka Windows Error Reporting / WER) crash report and then terminates the process.
        /// </summary>
        /// <param name="uncaughtException">Unhandled exception causing the crash</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">
        /// Some failure HRESULTs map to well-defined exceptions, while others do not map
        /// to a defined exception. If the HRESULT maps to a defined exception, ThrowExceptionForHR
        /// creates an instance of the exception and throws it. Otherwise, it creates an instance 
        /// of System.Runtime.InteropServices.COMException, initializes the error code field with 
        /// the HRESULT, and throws that exception. When this method is invoked, it attempts to
        /// retrieve extra information regarding the error by using the unmanaged GetErrorInfo
        /// function.
        /// </exception>
        private static void SubmitReport(Exception uncaughtException)
        {
            lock (s_reportCreationLock)
            {
                if (uncaughtException == null)
                {
                    throw new ArgumentNullException("uncaughtException");
                }

                ReportInformation reportInformation = new ReportInformation();
                reportInformation.dwSize = Marshal.SizeOf(reportInformation);
                reportInformation.hProcess = s_hCurrentProcess;
                reportInformation.hwndParent = s_hwndMainWindow;
                reportInformation.wzApplicationName = s_applicationName;
                reportInformation.wzApplicationPath = s_applicationPath;
                reportInformation.wzConsentKey = null; // null = the name specified by the pwzEventType parameter of WerReportCreate.
                reportInformation.wzDescription = null; // we can't provide a description of the problem - this an uncaught = *unexpected* exception
                reportInformation.wzFriendlyEventName = null; // null = the name specified by pwzEventType parameter of WerReportCreate.

                ReportHandle reportHandle;
                HandleHResult(NativeMethods.WerReportCreate(
                    powerShellEventType,
                    ReportType.WerReportCritical,
                    reportInformation,
                    out reportHandle));

                using (reportHandle)
                {
                    SetBucketParameters(reportHandle, uncaughtException);

                    // http://msdn.microsoft.com/en-us/library/bb513622(VS.85).aspx says:
                    //     If the server asks for a mini dump and you specify WerDumpTypeHeapDump for the dumpType parameter, 
                    //     WER will not send the heap dump to the Watson server. However, if the server asks for a heap dump 
                    //     and the dumpType is WerDumpTypeMiniDump, WER will send the mini dump to the server. 
                    //     Thus, it is recommended that you set dumpType to WerDumpTypeMiniDump

                    HandleHResult(NativeMethods.WerReportAddDump(
                        reportHandle,
                        s_hCurrentProcess,
                        IntPtr.Zero, // thread id is only required for *micro* dumps
                        DumpType.MiniDump,
                        IntPtr.Zero, // exception details.
                        IntPtr.Zero, // dumpCustomOptions - if this parameter is NULL, the standard minidump information is collected.
                                     /*DumpFlags.NoHeap_OnQueue*/0)); // can't use NoHeap_OnQueue, because then we probably won't
                                                                      // be able to request full heap dumps via http://watson web UI

                    SubmitResult submitResult = SubmitResult.ReportFailed;
                    SubmitFlags submitFlags =
                        SubmitFlags.HonorRecovery |
                        SubmitFlags.HonorRestart |
                        SubmitFlags.OutOfProcess |
                        SubmitFlags.AddRegisteredData;
                    if (WindowsErrorReporting.s_unattendedServerMode)
                    {
                        submitFlags |= SubmitFlags.Queue;
                    }
                    HandleHResult(NativeMethods.WerReportSubmit(
                        reportHandle,
                        Consent.NotAsked,
                        submitFlags,
                        out submitResult));

                    // At this point we have submitted the Watson report and we want to terminate the process
                    // as quickly and painlessly as possible (and possibly without sending additional Watson reports
                    // via the default .NET or OS handler).  
                    // Alternatives: native TerminateProcess, managed Environment.Exit, managed Environment.FailFast
                    Environment.Exit((int)submitResult);
                }
            }
        }

        internal static void WaitForPendingReports()
        {
            lock (s_reportCreationLock)
            {
                // only return if the lock is not taken
                return;
            }
        }

        #endregion

        #region Interface to the rest of PowerShell

        /// <summary>
        /// Equivalent to "System.Environment.FailFast(string, System.Exception)" that also does custom Watson reports.
        /// This method suppresses all the exceptions as this is not important for any 
        /// functionality. This feature is primarily used to help Microsoft fix
        /// bugs/crashes from customer data.
        /// </summary>
        /// <param name="exception">
        /// exception causing the failure. It is good to make sure this is not null.
        /// However the code will handle null cases.
        /// </param>
        internal static void FailFast(Exception exception)
        {
            Dbg.Assert(false, "We shouldn't do to FailFast during normal operation");

            try
            {
                if (s_registered && (null != exception))
                {
                    Debug.Assert(IsWindowsErrorReportingAvailable(), "Registration should succeed only if WER.dll is available");
                    WindowsErrorReporting.SubmitReport(exception);
                }
            }
            catch (Exception)
            {
                // SubmitReport can throw exceptions. suppressing those as they are not
                // important to report back.
                // Not calling CommandProcessorBase.CheckForSevereException(e) as it 
                // would introduce a recursion.
            }
            finally
            {
                // FailFast if something went wrong and SubmitReport didn't terminate the process
                // (or simply if not registered)
                Environment.FailFast((null != exception) ? exception.Message : string.Empty);
            }
        }

        private static readonly object s_registrationLock = new object();
        private static bool s_registered = false;
        private static bool s_unattendedServerMode = false;

        /// <summary>
        /// Sets everything up to report unhandled exceptions. 
        /// This method suppresses all the exceptions as this is not important for any 
        /// functionality. This feature is primarily used to help Microsoft fix
        /// bugs/crashes from customer data.
        /// </summary>
        /// <param name="unattendedServer">If <c>true</c>, then reports are not going to require any user interaction</param>
        internal static void RegisterWindowsErrorReporting(bool unattendedServer)
        {
            lock (s_registrationLock)
            {
                if (!s_registered && IsWindowsErrorReportingAvailable())
                {
                    try
                    {
                        FindStaticInformation();
                    }
                    // even if FindStaticInformation throws, we want to continue
                    // to report errors...as FindStaticInformation() is trying
                    // get Process related data..and it is ok to not report
                    // that data (this data helps making debugging/analysing easy)
                    catch (Exception e)
                    {
                        CommandProcessorBase.CheckForSevereException(e);
                        // suppress the exception
                    }

                    try
                    {
                        WindowsErrorReporting.s_unattendedServerMode = unattendedServer;
                        if (unattendedServer)
                        {
                            HandleHResult(NativeMethods.WerSetFlags(ReportingFlags.Queue));
                        }
                        else
                        {
                            HandleHResult(NativeMethods.WerSetFlags(0));
                        }

                        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
                        s_registered = true;
                    }
                    // there is some problem setting flags...handle those cases
                    catch (Exception e)
                    {
                        CommandProcessorBase.CheckForSevereException(e);
                        // suppress the exception
                    }
                }
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Dbg.Assert(false, "We shouldn't get unhandled exceptions during normal operation: " + e.ExceptionObject.ToString());

            Exception exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                WindowsErrorReporting.SubmitReport(exception);
            }
        }

        /// <summary>
        /// Writes a default type of memory dump to the specified file.
        /// </summary>
        /// <param name="file">file to write the dump to</param>
        internal static void WriteMiniDump(string file)
        {
            WriteMiniDump(file, MiniDumpType.MiniDumpNormal);
        }

        /// <summary>
        /// Writes a memory dump to the specified file.
        /// </summary>
        /// <param name="file">file to write the dump to</param>
        /// <param name="dumpType">type of the dump</param>
        internal static void WriteMiniDump(string file, MiniDumpType dumpType)
        {
            Process process = Process.GetCurrentProcess();
            using (FileStream fileStream = new FileStream(file, FileMode.Create))
            {
                NativeMethods.MiniDumpWriteDump(
                    process.Handle,
                    process.Id,
                    fileStream.SafeFileHandle,
                    dumpType,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
        }

        #endregion
    }
}
