// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell {
    /// <summary>
    /// Helper functions for process info.
    /// </summary>
    public static class ProcessCodeMethods
    {
        const int InvalidProcessId = -1;

        internal static Process GetParent(this Process process)
        {
            try
            {
                var pid = GetParentPid(process);
                if (pid == InvalidProcessId)
                {
                    return null;
                }

                var candidate = Process.GetProcessById(pid);

                // if the candidate was started later than process, the pid has been recycled
                return candidate.StartTime > process.StartTime ? null : candidate;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// CodeMethod for getting the parent process of a process.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>The parent process, or null if the parent is no longer running.</returns>
        public static object GetParentProcess(PSObject obj)
        {
            var process = PSObject.Base(obj) as Process;
            return process?.GetParent();
        }

        /// <summary>
        /// Returns the parent id of a process or -1 if it fails.
        /// </summary>
        /// <param name="process"></param>
        /// <returns>The pid of the parent process.</returns>
#if UNIX
        internal static int GetParentPid(Process process)
        {
            return Platform.NonWindowsGetProcessParentPid(process.Id);
        }
#else
        internal static int GetParentPid(Process process)
        {
            Diagnostics.Assert(process != null, "Ensure process is not null before calling");
            PROCESS_BASIC_INFORMATION pbi;
            int size;
            var res = NtQueryInformationProcess(process.Handle, 0, out pbi, Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(), out size);

            return res != 0 ? InvalidProcessId : pbi.InheritedFromUniqueProcessId.ToInt32();
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern int NtQueryInformationProcess(
                IntPtr processHandle,
                int processInformationClass,
                out PROCESS_BASIC_INFORMATION processInformation,
                int processInformationLength,
                out int returnLength);
#endif

        }
}
