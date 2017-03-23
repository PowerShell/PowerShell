/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights resulterved.
--********************************************************************/

using System;
using System.Globalization;
using System.Management.Automation;
using System.Text;
using System.Resources;
using System.Reflection;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Contains ToHumanString CodeMethod implementations for TimeSpan
    /// </summary>
    public static partial class ToStringCodeMethods
    {
        /// <summary>
        /// ToHumanString implementation for TimeSpan
        /// </summary>
        /// <param name="time">time is TimeSpan object</param>

        public static string ToHumanString(TimeSpan time)
        {
            return ToHumanString(time, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// ToHumanString implementation for TimeSpan
        /// </summary>
        /// <param name="time">time is TimeSpan object</param>
        /// <param name="Culture">Culture is CultureInfo object</param>

        public static string ToHumanString(TimeSpan time, CultureInfo Culture)
        {
            if (time == null)
                return String.Empty;

            HumanFormatter frmt = HumanFormatter.CreateHumanFormatter(Culture);

            return frmt?.TimeSpanToHumanString(time) ?? String.Empty;
        }
    }

    /// <summary>
    /// IHumanFormatter is interface for Humanizer
    /// </summary>
    internal interface IHumanFormatter
    {
        string TimeSpanToHumanString(TimeSpan time);
    }

    /// <summary>
    /// Default HumanFormatter class
    /// </summary>
    internal class HumanFormatter : IHumanFormatter
    {
        /// <summary>
        /// Use CreateHumanFormatter instead of HumanFormatter
        /// </summary>
        private HumanFormatter(CultureInfo Culture)
        {
            Humanizer.Culture = Culture;
        }

        /// <summary>
        /// Use CreateHumanFormatter to create HumanFormatter for Culture
        /// </summary>
        public static HumanFormatter CreateHumanFormatter(CultureInfo Culture)
        {
            switch (Culture.Name)
            {
                case "en-US": 
                    {
                        return new HumanFormatter(Culture);
                    }
                default:
                    {
                        return new HumanFormatter(Culture);
                    }
            }
        }

        private static String _delim = ", ";

        /// <summary>
        /// TimeSpanToHumanString is a default method to convert TimeSpan to human string
        /// </summary>
        /// <param name="time">time is converted to human string</param>
        public virtual string TimeSpanToHumanString(TimeSpan time)
        {
            int Days = time.Days;
            int Hours = time.Hours;
            int Minutes = time.Minutes;
            int Seconds = time.Seconds;
            int Milliseconds = time.Milliseconds;

            StringBuilder result = new StringBuilder();

            if (Days > 0)
            {
                result.AppendFormat(Days > 1 ? Humanizer.TimeSpanHumanize_MultipleDays : Humanizer.TimeSpanHumanize_MultipleDays_Singular, Days)
                        .Append(_delim);
            }
            if (Hours > 0)
            {
                result.AppendFormat(Hours > 1 ? Humanizer.TimeSpanHumanize_MultipleHours : Humanizer.TimeSpanHumanize_MultipleHours_Singular, Hours)
                        .Append(_delim);
            }
            if (Minutes > 0)
            {
                result.AppendFormat(Minutes > 1 ? Humanizer.TimeSpanHumanize_MultipleMinutes : Humanizer.TimeSpanHumanize_MultipleMinutes_Singular, Minutes)
                        .Append(_delim);
            }
            if (Seconds > 0)
            {
                result.AppendFormat(Seconds > 1 ? Humanizer.TimeSpanHumanize_MultipleSeconds : Humanizer.TimeSpanHumanize_MultipleSeconds_Singular, Seconds);
            }
            else
            {
                result.AppendFormat(Humanizer.TimeSpanHumanize_MultipleSeconds, Seconds);
            }
            if (Milliseconds > 0)
            {
                result.Append(_delim).AppendFormat(Milliseconds > 1 ? Humanizer.TimeSpanHumanize_MultipleMilliseconds : Humanizer.TimeSpanHumanize_MultipleMilliseconds_Singular, Milliseconds);
            }

            return result.ToString();
        }
    }
}
