// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    internal abstract class ColumnInfo
    {
        protected string displayName;
        protected string staleObjectPropertyName;

        internal ColumnInfo(string staleObjectPropertyName, string displayName)
        {
            this.displayName = displayName;
            this.staleObjectPropertyName = GraphicalHostReflectionWrapper.EscapeBinding(staleObjectPropertyName);
        }

        internal string StaleObjectPropertyName()
        {
            return this.staleObjectPropertyName;
        }

        internal string DisplayName()
        {
            return this.displayName;
        }

        internal abstract object GetValue(PSObject liveObject);

        internal Type GetValueType(PSObject liveObject, out object columnValue)
        {
            columnValue = GetValue(liveObject);
            if (columnValue != null && columnValue is IComparable)
            {
                return columnValue.GetType();
            }

            return typeof(string); // Use the String type as default.
        }

        /// <summary>
        /// Auxiliar used in GetValue methods since the list does not deal well with unlimited sized lines.
        /// </summary>
        /// <param name="src">Source string.</param>
        /// <returns>The source string limited in the number of lines.</returns>
        internal static object LimitString(object src)
        {
            if (!(src is string srcString))
            {
                return src;
            }

            return HostUtilities.GetMaxLines(srcString, 10);
        }
    }
}
