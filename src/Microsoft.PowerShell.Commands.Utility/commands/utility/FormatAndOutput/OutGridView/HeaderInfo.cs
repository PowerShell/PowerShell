// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    internal sealed class HeaderInfo
    {
        private readonly List<ColumnInfo> _columns = new();

        internal void AddColumn(ColumnInfo col)
        {
            _columns.Add(col);
        }

        internal PSObject AddColumnsToWindow(OutWindowProxy windowProxy, PSObject liveObject)
        {
            PSObject staleObject = new();

            // Initiate arrays to be of the same size.
            int count = _columns.Count;
            string[] propertyNames = new string[count];
            string[] displayNames = new string[count];
            Type[] types = new Type[count];

            count = 0; // Reuse this variable to count cycles.
            foreach (ColumnInfo column in _columns)
            {
                propertyNames[count] = column.StaleObjectPropertyName();
                displayNames[count] = column.DisplayName();
                object columnValue = null;
                types[count] = column.GetValueType(liveObject, out columnValue);

                // Add a property to the stale object since a column value has been evaluated to get column's type.
                staleObject.Properties.Add(new PSNoteProperty(propertyNames[count], columnValue));

                count++;
            }

            windowProxy.AddColumns(propertyNames, displayNames, types);

            return staleObject;
        }

        internal PSObject CreateStalePSObject(PSObject liveObject)
        {
            PSObject staleObject = new();
            foreach (ColumnInfo column in _columns)
            {
                // Add a property to the stale PSObject.
                staleObject.Properties.Add(new PSNoteProperty(column.StaleObjectPropertyName(),
                                           column.GetValue(liveObject)));
            }

            return staleObject;
        }
    }
}
