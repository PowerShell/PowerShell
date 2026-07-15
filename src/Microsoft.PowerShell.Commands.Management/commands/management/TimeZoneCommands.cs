// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A cmdlet to retrieve time zone information.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "TimeZone", DefaultParameterSetName = "Name",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096904")]
    [OutputType(typeof(TimeZoneInfo))]
    [Alias("gtz")]
    public class GetTimeZoneCommand : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// A list of the local time zone ids that the cmdlet should look up.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Id")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Id { get; set; }

        /// <summary>
        /// Specifies that the cmdlet should produce a collection of the
        /// TimeZoneInfo objects that are available on the system.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ListAvailable")]
        public SwitchParameter ListAvailable { get; set; }

        /// <summary>
        /// A list of the local time zone names that the cmdlet should look up.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = "Name")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name { get; set; }

        #endregion Parameters

        /// <summary>
        /// Implementation of the ProcessRecord method for Get-TimeZone.
        /// </summary>
        protected override void ProcessRecord()
        {
            // make sure we've got the latest time zone settings
            TimeZoneInfo.ClearCachedData();

            if (ListAvailable)
            {
                // output the list of all available time zones
                WriteObject(TimeZoneInfo.GetSystemTimeZones(), true);
            }
            else if (this.ParameterSetName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                // lookup each time zone id
                foreach (string tzid in Id)
                {
                    try
                    {
                        WriteObject(TimeZoneInfo.FindSystemTimeZoneById(tzid));
                    }
                    catch (TimeZoneNotFoundException e)
                    {
                        WriteError(new ErrorRecord(e, TimeZoneHelper.TimeZoneNotFoundError,
                            ErrorCategory.InvalidArgument, "Id"));
                    }
                }
            }
            else // ParameterSetName == "Name"
            {
                if (Name != null)
                {
                    // lookup each time zone name (or wildcard pattern)
                    foreach (string tzname in Name)
                    {
                        TimeZoneInfo[] timeZones = TimeZoneHelper.LookupSystemTimeZoneInfoByName(tzname);
                        if (timeZones.Length > 0)
                        {
                            // manually process each object in the array, so if there is only a single
                            // entry then the returned type is TimeZoneInfo and not TimeZoneInfo[], and
                            // it can be pipelined to Set-TimeZone more easily
                            foreach (TimeZoneInfo timeZone in timeZones)
                            {
                                WriteObject(timeZone);
                            }
                        }
                        else
                        {
                            string message = string.Format(CultureInfo.InvariantCulture,
                                TimeZoneResources.TimeZoneNameNotFound, tzname);

                            Exception e = new TimeZoneNotFoundException(message);
                            WriteError(new ErrorRecord(e, TimeZoneHelper.TimeZoneNotFoundError,
                                ErrorCategory.InvalidArgument, "Name"));
                        }
                    }
                }
                else
                {
                    // return the current system local time zone
                    WriteObject(TimeZoneInfo.Local);
                }
            }
        }
    }

#if !UNIX

    /// <summary>
    /// A cmdlet to set the system's local time zone.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "TimeZone",
        SupportsShouldProcess = true,
        DefaultParameterSetName = "Name",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097056")]
    [OutputType(typeof(TimeZoneInfo))]
    [Alias("stz")]
    public class SetTimeZoneCommand : PSCmdlet
    {
        #region string constants

        private const string TimeZoneTarget = "Local System";

        #endregion string constants

        #region Parameters

        /// <summary>
        /// The name of the local time zone that the system should use.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Id", ValueFromPipelineByPropertyName = true)]
        public string Id { get; set; }

        /// <summary>
        /// A TimeZoneInfo object identifying the local time zone that the system should use.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "InputObject", ValueFromPipeline = true)]
        public TimeZoneInfo InputObject { get; set; }

        /// <summary>
        /// The name of the local time zone that the system should use.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Name")]
        public string Name { get; set; }

        /// <summary>
        /// Request return of the new local time zone as a TimeZoneInfo object.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        #endregion Parameters

        /// <summary>
        /// Implementation of the ProcessRecord method for Set-TimeZone.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Justification = "Since Name is not a parameter of this method, it confuses FXCop. It is the appropriate value for the exception.")]
        protected override void ProcessRecord()
        {
            // make sure we've got fresh data, in case the requested time zone was added
            // to the system (registry) after our process was started
            TimeZoneInfo.ClearCachedData();

            // acquire a TimeZoneInfo if one wasn't supplied.
            if (this.ParameterSetName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    InputObject = TimeZoneInfo.FindSystemTimeZoneById(Id);
                }
                catch (TimeZoneNotFoundException e)
                {
                    ThrowTerminatingError(new ErrorRecord(
                        e,
                        TimeZoneHelper.TimeZoneNotFoundError,
                        ErrorCategory.InvalidArgument,
                        "Id"));
                }
            }
            else if (this.ParameterSetName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                // lookup the time zone name and make sure we have one (and only one) match
                TimeZoneInfo[] timeZones = TimeZoneHelper.LookupSystemTimeZoneInfoByName(Name);
                if (timeZones.Length == 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        TimeZoneResources.TimeZoneNameNotFound, Name);
                    Exception e = new TimeZoneNotFoundException(message);
                    ThrowTerminatingError(new ErrorRecord(e,
                        TimeZoneHelper.TimeZoneNotFoundError,
                        ErrorCategory.InvalidArgument,
                        "Name"));
                }
                else if (timeZones.Length > 1)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        TimeZoneResources.MultipleMatchingTimeZones, Name);
                    ThrowTerminatingError(new ErrorRecord(
                            new PSArgumentException(message, "Name"),
                            TimeZoneHelper.MultipleMatchingTimeZonesError,
                            ErrorCategory.InvalidArgument,
                            "Name"));
                }
                else
                {
                    InputObject = timeZones[0];
                }
            }
            else // ParameterSetName == "InputObject"
            {
                try
                {
                    // a TimeZoneInfo object was supplied, so use it to make sure we can find
                    // a backing system time zone, otherwise it's an error condition
                    InputObject = TimeZoneInfo.FindSystemTimeZoneById(InputObject.Id);
                }
                catch (TimeZoneNotFoundException e)
                {
                    ThrowTerminatingError(new ErrorRecord(
                        e,
                        TimeZoneHelper.TimeZoneNotFoundError,
                        ErrorCategory.InvalidArgument,
                        "InputObject"));
                }
            }

            if (ShouldProcess(TimeZoneTarget))
            {
                bool acquireAccess = false;
                try
                {
                    // check to see if permission to set the time zone is already enabled for this process
                    if (!HasAccess)
                    {
                        // acquire permissions to set the timezone
                        SetAccessToken(true);
                        acquireAccess = true;
                    }
                }
                catch (Win32Exception e)
                {
                    ThrowTerminatingError(new ErrorRecord(e,
                        TimeZoneHelper.InsufficientPermissionsError,
                        ErrorCategory.PermissionDenied, null));
                }

                try
                {
                    // construct and populate a new DYNAMIC_TIME_ZONE_INFORMATION structure
                    NativeMethods.DYNAMIC_TIME_ZONE_INFORMATION dtzi = new();
                    dtzi.Bias -= (int)InputObject.BaseUtcOffset.TotalMinutes;
                    dtzi.StandardName = InputObject.StandardName;
                    dtzi.DaylightName = InputObject.DaylightName;
                    dtzi.TimeZoneKeyName = InputObject.Id;

                    // Request time zone transition information for the current year
                    NativeMethods.TIME_ZONE_INFORMATION tzi = new();
                    if (!NativeMethods.GetTimeZoneInformationForYear((ushort)DateTime.Now.Year, ref dtzi, ref tzi))
                    {
                        ThrowWin32Error();
                    }

                    // copy over the transition times
                    dtzi.StandardBias = tzi.StandardBias;
                    dtzi.StandardDate = tzi.StandardDate;
                    dtzi.DaylightBias = tzi.DaylightBias;
                    dtzi.DaylightDate = tzi.DaylightDate;

                    // set the new local time zone for the system
                    if (!NativeMethods.SetDynamicTimeZoneInformation(ref dtzi))
                    {
                        ThrowWin32Error();
                    }

                    // broadcast a WM_SETTINGCHANGE notification message to all top-level windows so that they
                    // know to update their notion of the current system time (and time zone) if applicable
                    int result = 0;
                    NativeMethods.SendMessageTimeout((IntPtr)NativeMethods.HWND_BROADCAST, NativeMethods.WM_SETTINGCHANGE,
                        (IntPtr)0, "intl", NativeMethods.SMTO_ABORTIFHUNG, 5000, ref result);

                    // clear the time zone data or this PowerShell session
                    // will not recognize the new time zone settings
                    TimeZoneInfo.ClearCachedData();

                    if (PassThru.IsPresent)
                    {
                        // return the TimeZoneInfo object for the (new) current local time zone
                        WriteObject(TimeZoneInfo.Local);
                    }
                }
                catch (Win32Exception e)
                {
                    ThrowTerminatingError(new ErrorRecord(e,
                        TimeZoneHelper.SetTimeZoneFailedError,
                        ErrorCategory.FromStdErr, null));
                }
                finally
                {
                    if (acquireAccess)
                    {
                        // reset the permissions
                        SetAccessToken(false);
                    }
                }
            }
            else
            {
                if (PassThru.IsPresent)
                {
                    // show the user the time zone settings that would have been used.
                    WriteObject(InputObject);
                }
            }
        }

        #region Helper functions

        /// <summary>
        /// True if the current process has access to change the time zone setting.
        /// </summary>
        protected bool HasAccess
        {
            get
            {
                bool hasAccess = false;

                // open the access token for the current process
                IntPtr hToken = IntPtr.Zero;
                IntPtr hProcess = NativeMethods.GetCurrentProcess();
                if (!NativeMethods.OpenProcessToken(hProcess,
                    NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_QUERY, ref hToken))
                {
                    ThrowWin32Error();
                }

                try
                {
                    // setup the privileges being checked
                    NativeMethods.PRIVILEGE_SET ps = new()
                    {
                        PrivilegeCount = 1,
                        Control = 1,
                        Luid = 0,
                        Attributes = NativeMethods.SE_PRIVILEGE_ENABLED,
                    };

                    // lookup the Luid of the SeTimeZonePrivilege
                    if (!NativeMethods.LookupPrivilegeValue(null, NativeMethods.SE_TIME_ZONE_NAME, ref ps.Luid))
                    {
                        ThrowWin32Error();
                    }

                    // set the privilege for the open access token
                    if (!NativeMethods.PrivilegeCheck(hToken, ref ps, ref hasAccess))
                    {
                        ThrowWin32Error();
                    }
                }
                finally
                {
                    NativeMethods.CloseHandle(hToken);
                }

                return (hasAccess);
            }
        }

        /// <summary>
        /// Set the SeTimeZonePrivilege, which controls access to the SetDynamicTimeZoneInformation API.
        /// </summary>
        /// <param name="enable">Set to true to enable (or false to disable) the privilege.</param>
        protected void SetAccessToken(bool enable)
        {
            // open the access token for the current process
            IntPtr hToken = IntPtr.Zero;
            IntPtr hProcess = NativeMethods.GetCurrentProcess();
            if (!NativeMethods.OpenProcessToken(hProcess,
                NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_QUERY, ref hToken))
            {
                ThrowWin32Error();
            }

            try
            {
                // setup the privileges being requested
                NativeMethods.TOKEN_PRIVILEGES tp = new()
                {
                    PrivilegeCount = 1,
                    Luid = 0,
                    Attributes = (enable ? NativeMethods.SE_PRIVILEGE_ENABLED : 0),
                };

                // lookup the Luid of the SeTimeZonePrivilege
                if (!NativeMethods.LookupPrivilegeValue(null, NativeMethods.SE_TIME_ZONE_NAME, ref tp.Luid))
                {
                    ThrowWin32Error();
                }

                // set the privilege for the open access token
                if (!NativeMethods.AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    ThrowWin32Error();
                }
            }
            finally
            {
                NativeMethods.CloseHandle(hToken);
            }
        }

        /// <summary>
        /// Get the Win32 error code from GetLastError and throw an exception.
        /// </summary>
        protected void ThrowWin32Error()
        {
            int error = Marshal.GetLastWin32Error();
            throw new Win32Exception(error);
        }

        #endregion Helper functions

        #region Win32 interop helper

        internal static class NativeMethods
        {
            #region Native DLL locations

            private const string SetDynamicTimeZoneApiDllName = "api-ms-win-core-timezone-l1-1-0.dll";
            private const string GetTimeZoneInformationForYearApiDllName = "api-ms-win-core-timezone-l1-1-0.dll";
            private const string GetCurrentProcessApiDllName = "api-ms-win-downlevel-kernel32-l1-1-0.dll";
            private const string OpenProcessTokenApiDllName = "api-ms-win-downlevel-advapi32-l1-1-1.dll";
            private const string LookupPrivilegeTokenApiDllName = "api-ms-win-downlevel-advapi32-l4-1-0.dll";
            private const string PrivilegeCheckApiDllName = "api-ms-win-downlevel-advapi32-l1-1-1.dll";
            private const string AdjustTokenPrivilegesApiDllName = "api-ms-win-downlevel-advapi32-l1-1-1.dll";
            private const string CloseHandleApiDllName = "api-ms-win-downlevel-kernel32-l1-1-0.dll";
            private const string SendMessageTimeoutApiDllName = "ext-ms-win-rtcore-ntuser-window-ext-l1-1-0.dll";

            #endregion Native DLL locations

            #region Win32 SetDynamicTimeZoneInformation imports

            /// <summary>
            /// Used to marshal win32 SystemTime structure to managed code layer.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct SystemTime
            {
                /// <summary>
                /// The year.
                /// </summary>
                [MarshalAs(UnmanagedType.U2)]
                public short Year;
                /// <summary>
                /// The month.
                /// </summary>
                [MarshalAs(UnmanagedType.U2)]
                public short Month;
                /// <summary>
                /// The day of the week.
                /// </summary>
                [MarshalAs(UnmanagedType.U2)]
                public short DayOfWeek;
                /// <summary>
                /// The day of the month.
                /// </summary>
                [MarshalAs(UnmanagedType.U2)]
                public short Day;
                /// <summary>
                /// The hour.
                /// </summary>
                [MarshalAs(UnmanagedType.U2)]
                public short Hour;
                /// <summary>
                /// The minute.
                /// </summary>
                [MarshalAs(UnmanagedType.U2)]
                public short Minute;
                /// <summary>
                /// The second.
                /// </summary>
                [MarshalAs(UnmanagedType.U2)]
                public short Second;
                /// <summary>
                /// The millisecond.
                /// </summary>
                [MarshalAs(UnmanagedType.U2)]
                public short Milliseconds;
            }

            /// <summary>
            /// Used to marshal win32 DYNAMIC_TIME_ZONE_INFORMATION structure to managed code layer.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct DYNAMIC_TIME_ZONE_INFORMATION
            {
                /// <summary>
                /// The current bias for local time translation on this computer, in minutes.
                /// </summary>
                [MarshalAs(UnmanagedType.I4)]
                public int Bias;
                /// <summary>
                /// A description for standard time.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string StandardName;
                /// <summary>
                /// A SystemTime structure that contains a date and local time when the transition from daylight saving time to standard time occurs on this operating system.
                /// </summary>
                public SystemTime StandardDate;
                /// <summary>
                /// The bias value to be used during local time translations that occur during standard time.
                /// </summary>
                [MarshalAs(UnmanagedType.I4)]
                public int StandardBias;
                /// <summary>
                /// A description for daylight saving time (DST).
                /// </summary>
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string DaylightName;
                /// <summary>
                /// A SystemTime structure that contains a date and local time when the transition from standard time to daylight saving time occurs on this operating system.
                /// </summary>
                public SystemTime DaylightDate;
                /// <summary>
                /// The bias value to be used during local time translations that occur during daylight saving time.
                /// </summary>
                [MarshalAs(UnmanagedType.I4)]
                public int DaylightBias;
                /// <summary>
                /// The name of the time zone registry key on the local computer.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x80)]
                public string TimeZoneKeyName;
                /// <summary>
                /// Indicates whether dynamic daylight saving time is disabled.
                /// </summary>
                [MarshalAs(UnmanagedType.U1)]
                public bool DynamicDaylightTimeDisabled;
            }

            /// <summary>
            /// Used to marshal win32 TIME_ZONE_INFORMATION structure to managed code layer.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct TIME_ZONE_INFORMATION
            {
                /// <summary>
                /// The current bias for local time translation on this computer, in minutes.
                /// </summary>
                [MarshalAs(UnmanagedType.I4)]
                public int Bias;
                /// <summary>
                /// A description for standard time.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string StandardName;
                /// <summary>
                /// A SystemTime structure that contains a date and local time when the transition from daylight saving time to standard time occurs on this operating system.
                /// </summary>
                public SystemTime StandardDate;
                /// <summary>
                /// The bias value to be used during local time translations that occur during standard time.
                /// </summary>
                [MarshalAs(UnmanagedType.I4)]
                public int StandardBias;
                /// <summary>
                /// A description for daylight saving time (DST).
                /// </summary>
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string DaylightName;
                /// <summary>
                /// A SystemTime structure that contains a date and local time when the transition from standard time to daylight saving time occurs on this operating system.
                /// </summary>
                public SystemTime DaylightDate;
                /// <summary>
                /// The bias value to be used during local time translations that occur during daylight saving time.
                /// </summary>
                [MarshalAs(UnmanagedType.I4)]
                public int DaylightBias;
            }

            /// <summary>
            /// PInvoke SetDynamicTimeZoneInformation API.
            /// </summary>
            /// <param name="lpTimeZoneInformation">A DYNAMIC_TIME_ZONE_INFORMATION structure representing the desired local time zone.</param>
            /// <returns></returns>
            [DllImport(SetDynamicTimeZoneApiDllName, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetDynamicTimeZoneInformation([In] ref DYNAMIC_TIME_ZONE_INFORMATION lpTimeZoneInformation);

            [DllImport(GetTimeZoneInformationForYearApiDllName, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetTimeZoneInformationForYear([In] ushort wYear, [In] ref DYNAMIC_TIME_ZONE_INFORMATION pdtzi, ref TIME_ZONE_INFORMATION ptzi);

            #endregion Win32 SetDynamicTimeZoneInformation imports

            #region Win32 AdjustTokenPrivilege imports

            /// <summary>
            /// Definition of TOKEN_QUERY constant from Win32 API.
            /// </summary>
            public const int TOKEN_QUERY = 0x00000008;

            /// <summary>
            /// Definition of TOKEN_ADJUST_PRIVILEGES constant from Win32 API.
            /// </summary>
            public const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;

            /// <summary>
            /// Definition of SE_PRIVILEGE_ENABLED constant from Win32 API.
            /// </summary>
            public const int SE_PRIVILEGE_ENABLED = 0x00000002;

            /// <summary>
            /// Definition of SE_TIME_ZONE_NAME constant from Win32 API.
            /// </summary>
            public const string SE_TIME_ZONE_NAME = "SeTimeZonePrivilege"; // https://msdn.microsoft.com/library/bb530716(VS.85).aspx

            /// <summary>
            /// PInvoke GetCurrentProcess API.
            /// </summary>
            /// <returns></returns>
            [DllImport(GetCurrentProcessApiDllName, ExactSpelling = true)]
            public static extern IntPtr GetCurrentProcess();

            /// <summary>
            /// PInvoke OpenProcessToken API.
            /// </summary>
            /// <param name="ProcessHandle"></param>
            /// <param name="DesiredAccess"></param>
            /// <param name="TokenHandle"></param>
            /// <returns></returns>
            [DllImport(OpenProcessTokenApiDllName, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

            /// <summary>
            /// PInvoke LookupPrivilegeValue API.
            /// </summary>
            /// <param name="lpSystemName"></param>
            /// <param name="lpName"></param>
            /// <param name="lpLuid"></param>
            /// <returns></returns>
            [DllImport(LookupPrivilegeTokenApiDllName, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref long lpLuid);

            /// <summary>
            /// PInvoke PrivilegeCheck API.
            /// </summary>
            /// <param name="ClientToken"></param>
            /// <param name="RequiredPrivileges"></param>
            /// <param name="pfResult"></param>
            /// <returns></returns>
            [DllImport(PrivilegeCheckApiDllName, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool PrivilegeCheck(IntPtr ClientToken, ref PRIVILEGE_SET RequiredPrivileges, ref bool pfResult);

            /// <summary>
            /// PInvoke AdjustTokenPrivilege API.
            /// </summary>
            /// <param name="TokenHandle"></param>
            /// <param name="DisableAllPrivileges"></param>
            /// <param name="NewState"></param>
            /// <param name="BufferLength"></param>
            /// <param name="PreviousState"></param>
            /// <param name="ReturnLength"></param>
            /// <returns></returns>
            [DllImport(AdjustTokenPrivilegesApiDllName, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
                ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

            /// <summary>
            /// PInvoke CloseHandle API.
            /// </summary>
            /// <param name="hObject"></param>
            /// <returns></returns>
            [DllImport(CloseHandleApiDllName, ExactSpelling = true, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hObject);

            /// <summary>
            /// Used to marshal win32 PRIVILEGE_SET structure to managed code layer.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct PRIVILEGE_SET
            {
                public int PrivilegeCount;
                public int Control;
                public long Luid;
                public int Attributes;
            }

            /// <summary>
            /// Used to marshal win32 TOKEN_PRIVILEGES structure to managed code layer.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct TOKEN_PRIVILEGES
            {
                public int PrivilegeCount;
                public long Luid;
                public int Attributes;
            }

            #endregion Win32 AdjustTokenPrivilege imports

            #region Win32 SendMessage imports

            /// <summary>
            /// Definition of WM_SETTINGCHANGE constant from Win32 API.
            /// </summary>
            public const int WM_SETTINGCHANGE = 0x001A;

            /// <summary>
            /// Definition of HWND_BROADCAST constant from Win32 API.
            /// </summary>
            public const int HWND_BROADCAST = (-1);

            /// <summary>
            /// Definition of SMTO_ABORTIFHUNG constant from Win32 API.
            /// </summary>
            public const int SMTO_ABORTIFHUNG = 0x0002;

            /// <summary>
            /// PInvoke SendMessageTimeout API.
            /// </summary>
            /// <param name="hWnd"></param>
            /// <param name="Msg"></param>
            /// <param name="wParam"></param>
            /// <param name="lParam"></param>
            /// <param name="fuFlags"></param>
            /// <param name="uTimeout"></param>
            /// <param name="lpdwResult"></param>
            /// <returns></returns>
            [DllImport(SendMessageTimeoutApiDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, string lParam, int fuFlags, int uTimeout, ref int lpdwResult);

            #endregion Win32 SendMessage imports
        }

        #endregion Win32 interop helper
    }

#endif
    /// <summary>
    /// Static Helper class for working with system time zones.
    /// </summary>
    internal static class TimeZoneHelper
    {
        #region Error Ids

        internal const string TimeZoneNotFoundError = "TimeZoneNotFound";
        internal const string MultipleMatchingTimeZonesError = "MultipleMatchingTimeZones";
        internal const string InsufficientPermissionsError = "InsufficientPermissions";
        internal const string SetTimeZoneFailedError = "SetTimeZoneFailed";

        #endregion Error Ids

        /// <summary>
        /// Find the system time zone by checking first against StandardName and then,
        /// if no matches were found, against the DaylightName.
        /// </summary>
        /// <param name="name">The name (or wildcard pattern) of the system time zone to find.</param>
        /// <returns>A TimeZoneInfo object array containing information about the specified system time zones.</returns>
        internal static TimeZoneInfo[] LookupSystemTimeZoneInfoByName(string name)
        {
            WildcardPattern namePattern = new(name, WildcardOptions.IgnoreCase);
            List<TimeZoneInfo> tzi = new();

            // get the available system time zones
            ReadOnlyCollection<TimeZoneInfo> zones = TimeZoneInfo.GetSystemTimeZones();

            // check against the standard and daylight names for each TimeZoneInfo
            foreach (TimeZoneInfo zone in zones)
            {
                if (namePattern.IsMatch(zone.StandardName) || namePattern.IsMatch(zone.DaylightName))
                {
                    tzi.Add(zone);
                }
            }

            return (tzi.ToArray());
        }
    }
}
