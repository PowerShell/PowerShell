/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Globalization;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    #region get-date

    /// <summary>
    /// implementation for the get-date command
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Date", DefaultParameterSetName = "net", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113313")]
    [OutputType(typeof(string), ParameterSetName = new string[] { "UFormat", "net" })]
    [OutputType(typeof(DateTime), ParameterSetName = new string[] { "net" })]
    public sealed class GetDateCommand : Cmdlet
    {
        #region parameters

        /// <summary>
        /// Allows user to override the date/time object that will be processed
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("LastWriteTime")]
        public DateTime Date
        {
            get
            {
                return _date;
            }
            set
            {
                _date = value;
                _dateSpecified = true;
            }
        }
        private DateTime _date;
        private bool _dateSpecified;


        /// <summary>
        /// Allows the user to override the year
        /// </summary>
        [Parameter]
        [ValidateRangeAttribute(1, 9999)]
        public int Year
        {
            get
            {
                return _year;
            }
            set
            {
                _year = value;
                _yearSpecified = true;
            }
        }
        private int _year;
        private bool _yearSpecified;


        /// <summary>
        /// Allows the user to override the month
        /// </summary>
        [Parameter]
        [ValidateRangeAttribute(1, 12)]
        public int Month
        {
            get
            {
                return _month;
            }
            set
            {
                _month = value;
                _monthSpecified = true;
            }
        }
        private int _month;
        private bool _monthSpecified;


        /// <summary>
        /// Allows the user to override the day
        /// </summary>
        [Parameter]
        [ValidateRangeAttribute(1, 31)]
        public int Day
        {
            get
            {
                return _day;
            }
            set
            {
                _day = value;
                _daySpecified = true;
            }
        }
        private int _day;
        private bool _daySpecified;


        /// <summary>
        /// Allows the user to override the hour
        /// </summary>
        [Parameter]
        [ValidateRangeAttribute(0, 23)]
        public int Hour
        {
            get
            {
                return _hour;
            }
            set
            {
                _hour = value;
                _hourSpecified = true;
            }
        }
        private int _hour;
        private bool _hourSpecified;


        /// <summary>
        /// Allows the user to override the minute
        /// </summary>
        [Parameter]
        [ValidateRangeAttribute(0, 59)]
        public int Minute
        {
            get
            {
                return _minute;
            }
            set
            {
                _minute = value;
                _minuteSpecified = true;
            }
        }
        private int _minute;
        private bool _minuteSpecified;


        /// <summary>
        /// Allows the user to override the second
        /// </summary>
        [Parameter]
        [ValidateRangeAttribute(0, 59)]
        public int Second
        {
            get
            {
                return _second;
            }
            set
            {
                _second = value;
                _secondSpecified = true;
            }
        }
        private int _second;
        private bool _secondSpecified;

        /// <summary>
        /// Allows the user to override the millisecond
        /// </summary>
        [Parameter]
        [ValidateRangeAttribute(0, 999)]
        public int Millisecond
        {
            get
            {
                return _millisecond;
            }
            set
            {
                _millisecond = value;
                _millisecondSpecified = true;
            }
        }
        private int _millisecond;
        private bool _millisecondSpecified;

        /// <summary>
        /// This option determines the default output format used to display the object get-date emits
        /// </summary>
        [Parameter]
        public DisplayHintType DisplayHint { get; set; } = DisplayHintType.DateTime;


        /// <summary>
        /// Unix format string
        /// </summary>
        [Parameter(ParameterSetName = "UFormat")]
        [ValidateNotNullOrEmpty]
        public string UFormat { get; set; }


        /// <summary>
        /// Unix format string
        /// </summary>
        [Parameter(ParameterSetName = "net")]
        [ArgumentCompletions("FileDate", "FileDateUniversal", "FileDateTime", "FileDateTimeUniversal")]
        public string Format { get; set; }

        #endregion

        #region methods

        /// <summary>
        /// get the time
        /// </summary>
        protected override void ProcessRecord()
        {
            DateTime dateToUse = DateTime.Now;
            int offset;

            // use passed date object if specified
            if (_dateSpecified)
            {
                dateToUse = Date;
            }

            //use passed year if specified
            if (_yearSpecified)
            {
                offset = Year - dateToUse.Year;
                dateToUse = dateToUse.AddYears(offset);
            }

            //use passed month if specified
            if (_monthSpecified)
            {
                offset = Month - dateToUse.Month;
                dateToUse = dateToUse.AddMonths(offset);
            }

            //use passed day if specified
            if (_daySpecified)
            {
                offset = Day - dateToUse.Day;
                dateToUse = dateToUse.AddDays(offset);
            }

            //use passed hour if specified
            if (_hourSpecified)
            {
                offset = Hour - dateToUse.Hour;
                dateToUse = dateToUse.AddHours(offset);
            }

            //use passed minute if specified
            if (_minuteSpecified)
            {
                offset = Minute - dateToUse.Minute;
                dateToUse = dateToUse.AddMinutes(offset);
            }

            //use passed second if specified
            if (_secondSpecified)
            {
                offset = Second - dateToUse.Second;
                dateToUse = dateToUse.AddSeconds(offset);
            }

            //use passed millisecond if specified
            if (_millisecondSpecified)
            {
                offset = Millisecond - dateToUse.Millisecond;
                dateToUse = dateToUse.AddMilliseconds(offset);
                dateToUse = dateToUse.Subtract(TimeSpan.FromTicks(dateToUse.Ticks % 10000));
            }

            if (UFormat != null)
            {
                //format according to UFormat string
                WriteObject(UFormatDateString(dateToUse));
            }
            else if (Format != null)
            {
                //format according to Format string

                // Special case built-in primitives: FileDate, FileDateTime.
                // These are the ISO 8601 "basic" formats, dropping dashes and colons
                // so that they can be used in file names

                if (String.Equals("FileDate", Format, StringComparison.OrdinalIgnoreCase))
                {
                    Format = "yyyyMMdd";
                }
                else if (String.Equals("FileDateUniversal", Format, StringComparison.OrdinalIgnoreCase))
                {
                    dateToUse = dateToUse.ToUniversalTime();
                    Format = "yyyyMMddZ";
                }
                else if (String.Equals("FileDateTime", Format, StringComparison.OrdinalIgnoreCase))
                {
                    Format = "yyyyMMddTHHmmssffff";
                }
                else if (String.Equals("FileDateTimeUniversal", Format, StringComparison.OrdinalIgnoreCase))
                {
                    dateToUse = dateToUse.ToUniversalTime();
                    Format = "yyyyMMddTHHmmssffffZ";
                }

                WriteObject(dateToUse.ToString(Format, CultureInfo.CurrentCulture));
            }
            else
            {
                //output DateTime object wrapped in an PSObject with DisplayHint attached
                PSObject outputObj = new PSObject(dateToUse);
                PSNoteProperty note = new PSNoteProperty("DisplayHint", DisplayHint);
                outputObj.Properties.Add(note);

                WriteObject(outputObj);
            }
        } // EndProcessing


        /// <summary>
        /// This is more an implementation of the UNIX strftime
        /// </summary>
        private string UFormatDateString(DateTime dateTime)
        {
            DateTime epoch = DateTime.Parse("January 1, 1970", System.Globalization.CultureInfo.InvariantCulture);
            int offset = 0;
            StringBuilder sb = new StringBuilder();

            // folks may include the "+" as part of the format string
            if (UFormat[0] == '+')
            {
                offset++;
            }
            for (int i = offset; i < UFormat.Length; i++)
            {
                if (UFormat[i] == '%')
                {
                    i++;
                    switch (UFormat[i])
                    {
                        case 'A':
                            sb.Append("{0:dddd}");
                            break;

                        case 'a':
                            sb.Append("{0:ddd}");
                            break;

                        case 'B':
                            sb.Append("{0:MMMM}");
                            break;

                        case 'b':
                            sb.Append("{0:MMM}");
                            break;

                        case 'h':
                            sb.Append("{0:MMM}");
                            break;

                        case 'C':
                            sb.Append(dateTime.Year / 100);
                            break;

                        case 'c':
                            sb.Append("{0:ddd} {0:MMM} ");
                            sb.Append(StringUtil.Format("{0,2} ", dateTime.Day));
                            sb.Append("{0:HH}:{0:mm}:{0:ss} {0:yyyy}");
                            break;

                        case 'D':
                            sb.Append("{0:MM/dd/yy}");
                            break;

                        case 'd':
                            sb.Append("{0:dd}");
                            break;

                        case 'e':
                            sb.Append(StringUtil.Format("{0,2}", dateTime.Day));
                            break;

                        case 'H':
                            sb.Append("{0:HH}");
                            break;

                        case 'I':
                            sb.Append("{0:hh}");
                            break;

                        case 'j':
                            sb.Append(dateTime.DayOfYear);
                            break;

                        case 'k':
                            sb.Append("{0:HH}");
                            break;

                        case 'l':
                            sb.Append("{0:hh}");
                            break;

                        case 'M':
                            sb.Append("{0:mm}");
                            break;

                        case 'm':
                            sb.Append("{0:MM}");
                            break;

                        case 'n':
                            sb.Append("\n");
                            break;

                        case 'p':
                            sb.Append("{0:tt}");
                            break;

                        case 'R':
                            sb.Append("{0:HH:mm}");
                            break;

                        case 'r':
                            sb.Append("{0:hh:mm:ss tt}");
                            break;

                        case 'S':
                            sb.Append("{0:ss}");
                            break;

                        case 's':
                            sb.Append(dateTime.Subtract(epoch).TotalSeconds);
                            break;

                        case 'T':
                            sb.Append("{0:HH:mm:ss}");
                            break;

                        case 'X':
                            sb.Append("{0:HH:mm:ss}");
                            break;

                        case 't':
                            sb.Append("\t");
                            break;

                        case 'u':
                            sb.Append((int)dateTime.DayOfWeek);
                            break;

                        case 'U':
                            sb.Append(dateTime.DayOfYear / 7);
                            break;

                        case 'V':
                            sb.Append((dateTime.DayOfYear / 7) + 1);
                            break;

                        case 'G':
                            sb.Append("{0:yyyy}");
                            break;

                        case 'g':
                            sb.Append("{0:yy}");
                            break;

                        case 'W':
                            sb.Append(dateTime.DayOfYear / 7);
                            break;

                        case 'w':
                            sb.Append((int)dateTime.DayOfWeek);
                            break;

                        case 'x':
                            sb.Append("{0:MM/dd/yy}");
                            break;

                        case 'y':
                            sb.Append("{0:yy}");
                            break;

                        case 'Y':
                            sb.Append("{0:yyyy}");
                            break;

                        case 'Z':
                            sb.Append("{0:zz}");
                            break;

                        default:
                            sb.Append(UFormat[i]);
                            break;
                    }
                }
                else
                {
                    // It's not a known format specifier, so just append it
                    sb.Append(UFormat[i]);
                }
            }

            return StringUtil.Format(sb.ToString(), dateTime);
        } // UFormatDateString

        #endregion
    } // GetDateCommand

    #endregion

    #region DisplayHintType enum

    /// <summary>
    /// Display Hint type
    /// </summary>
    public enum DisplayHintType
    {
        /// <summary>
        /// Display preference Date-Only
        /// </summary>
        Date,
        /// <summary>
        /// Display preference Time-Only
        /// </summary>
        Time,
        /// <summary>
        /// Display preference Date and Time
        /// </summary>
        DateTime
    }

    #endregion
} // namespace Microsoft.PowerShell.Commands


