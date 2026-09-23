// Copyright (c) Microsoft Corporation.
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
    [Cmdlet(VerbsCommon.Set, "Date", DefaultParameterSetName = "Date", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097133")]
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
                // We are not validating the native call here.
                // We just want to be sure that we're using the value the user provided us.
                if (Dbg.Internal.InternalTestHooks.SetDate)
                {
                    WriteObject(dateToUse);
                }
                else if (!Platform.NonWindowsSetDate(dateToUse))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
#else
                // build up the SystemTime struct to pass to SetSystemTime
                NativeMethods.SystemTime systemTime = new();
                systemTime.Year = (ushort)dateToUse.Year;
                systemTime.Month = (ushort)dateToUse.Month;
                systemTime.Day = (ushort)dateToUse.Day;
                systemTime.Hour = (ushort)dateToUse.Hour;
                systemTime.Minute = (ushort)dateToUse.Minute;
                systemTime.Second = (ushort)dateToUse.Second;
                systemTime.Milliseconds = (ushort)dateToUse.Millisecond;
#pragma warning disable 56523
                if (Dbg.Internal.InternalTestHooks.SetDate)
                {
                    WriteObject(systemTime);
                }
                else
                {
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
                }
#pragma warning restore 56523
#endif
            }

            // output DateTime object wrapped in an PSObject with DisplayHint attached
            PSObject outputObj = new(dateToUse);
            PSNoteProperty note = new("DisplayHint", DisplayHint);
            outputObj.Properties.Add(note);

            // If we've turned on the SetDate test hook, don't emit the output object here because we emitted it earlier.
            if (!Dbg.Internal.InternalTestHooks.SetDate)
            {
                WriteObject(outputObj);
            }
        }

        #endregion

        #region nativemethods

        internal static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct SystemTime
            {
                public ushort Year;
                public ushort Month;
                public ushort DayOfWeek;
                public ushort Day;
                public ushort Hour;
                public ushort Minute;
                public ushort Second;
                public ushort Milliseconds;
            }

            [DllImport(PinvokeDllNames.SetLocalTimeDllName, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetLocalTime(ref SystemTime systime);
        }
        #endregion
    }
}
