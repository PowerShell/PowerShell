// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The DateTimeApproximationComparer is responsible for comparing two
    /// DateTime objects at a level of precision determined by
    /// the first object. The comparison either compares at the
    /// date level or the date and time (down to Seconds precision).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class DateTimeApproximationComparer : IComparer<DateTime>
    {
        /// <summary>
        /// Compares two objects and returns a value indicating
        /// whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="value1">
        /// The first object to compare.
        /// </param>
        /// <param name="value2">
        /// The second object to compare.
        /// </param>
        /// <returns>
        /// If value1 is less than value2, then a value less than zero is returned.
        /// If value1 equals value2, than zero is returned.
        /// If value1 is greater than value2, then a value greater than zero is returned.
        /// </returns>
        public int Compare(DateTime value1, DateTime value2)
        {
            DateTime roundedX;
            DateTime roundedY;
            GetRoundedValues(value1, value2, out roundedX, out roundedY);

            return roundedX.CompareTo(roundedY);
        }

        private static void GetRoundedValues(DateTime value1, DateTime value2, out DateTime roundedValue1, out DateTime roundedValue2)
        {
            roundedValue1 = value1;
            roundedValue2 = value2;

            bool hasTimeComponent = HasTimeComponent(value1);

            int hour = hasTimeComponent ? value1.Hour : value2.Hour;
            int minute = hasTimeComponent ? value1.Minute : value2.Minute;
            int second = hasTimeComponent ? value1.Second : value2.Second;

            roundedValue1 = new DateTime(value1.Year, value1.Month, value1.Day, hour, minute, second);
            roundedValue2 = new DateTime(value2.Year, value2.Month, value2.Day, value2.Hour, value2.Minute, value2.Second);
        }

        private static bool HasTimeComponent(DateTime value)
        {
            bool hasNoTimeComponent = value.Hour == 0
                && value.Minute == 0
                && value.Second == 0
                && value.Millisecond == 0;

            return !hasNoTimeComponent;
        }
    }
}
