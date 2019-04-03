// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Runtime.InteropServices;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the set-date command.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "Date", DefaultParameterSetName = "Date", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113393")]
    [OutputType(typeof(DateTime))]
    public sealed class SetDateCommand : PSCmdlet
    {
        #region parameters

        /// <summary>
        /// Allows user to override the date/time object that will be processed.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Date", ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public DateTime Date { get; set; }

        /// <summary>
        /// Allows a use to specify a timespan with which to apply to the current time.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Adjust", ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        public TimeSpan Adjust { get; set; }

        /// <summary>
        /// This option determines the default output format used to display the object set-date emits.
        /// </summary>
        [Parameter]
        public DisplayHintType DisplayHint { get; set; } = DisplayHintType.DateTime;

        #endregion

        #region methods

        /// <summary>
        /// Set the date.
        /// </summary>
        [ArchitectureSensitive]
        protected override void ProcessRecord()
        {
            DateTime dateToUse;

            switch (ParameterSetName)
            {
                case "Date":
                    dateToUse = Date;
                    break;

                case "Adjust":
                    dateToUse = DateTime.Now.Add(Adjust);
                    break;

                default:
                    Dbg.Diagnostics.Assert(false, "Only one of the specified parameter sets should be called.");
                    goto case "Date";
            }

            if (ShouldProcess(dateToUse.ToString()))
            {
#if UNIX
                if (!Platform.NonWindowsSetDate(dateToUse))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
#else
                // build up the SystemTime struct to pass to SetSystemTime
                NativeMethods.SystemTime systemTime = new NativeMethods.SystemTime();
                systemTime.Year = (UInt16)dateToUse.Year;
                systemTime.Month = (UInt16)dateToUse.Month;
                systemTime.Day = (UInt16)dateToUse.Day;
                systemTime.Hour = (UInt16)dateToUse.Hour;
                systemTime.Minute = (UInt16)dateToUse.Minute;
                systemTime.Second = (UInt16)dateToUse.Second;
                systemTime.Milliseconds = (UInt16)dateToUse.Millisecond;
#pragma warning disable 56523
                if (!NativeMethods.SetLocalTime(ref systemTime))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                // MSDN says to call this twice to account for changes
                // between DST
                if (!NativeMethods.SetLocalTime(ref systemTime))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
#pragma warning restore 56523
#endif
            }

            // output DateTime object wrapped in an PSObject with DisplayHint attached
            PSObject outputObj = new PSObject(dateToUse);
            PSNoteProperty note = new PSNoteProperty("DisplayHint", DisplayHint);
            outputObj.Properties.Add(note);

            WriteObject(outputObj);
        }

        #endregion

        #region nativemethods

        internal static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct SystemTime
            {
                public UInt16 Year;
                public UInt16 Month;
                public UInt16 DayOfWeek;
                public UInt16 Day;
                public UInt16 Hour;
                public UInt16 Minute;
                public UInt16 Second;
                public UInt16 Milliseconds;
            }

            [DllImport(PinvokeDllNames.SetLocalTimeDllName, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetLocalTime(ref SystemTime systime);
        }
        #endregion
    }
}
