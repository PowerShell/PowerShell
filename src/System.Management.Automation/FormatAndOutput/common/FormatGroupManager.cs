// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Internal class to manage the grouping algorithm for the
    /// format-xxx commands.
    /// </summary>
    internal sealed class GroupingInfoManager
    {
        /// <summary>
        /// Initialize with the grouping property data.
        /// </summary>
        /// <param name="groupingExpression">Name of the grouping property.</param>
        /// <param name="displayLabel">Display name of the property.</param>
        internal void Initialize(PSPropertyExpression groupingExpression, string displayLabel)
        {
            _groupingKeyExpression = groupingExpression;
            _label = displayLabel;
        }

        internal object CurrentGroupingKeyPropertyValue
        {
            get { return _currentGroupingKeyPropertyValue; }
        }

        internal string GroupingKeyDisplayName
        {
            get
            {
                if (_label != null)
                    return _label;
                return _groupingKeyDisplayName;
            }
        }

        /// <summary>
        /// Compute the string value of the grouping property.
        /// </summary>
        /// <param name="so">Object to use to compute the property value.</param>
        /// <returns>True if there was an update.</returns>
        internal bool UpdateGroupingKeyValue(PSObject so)
        {
            if (_groupingKeyExpression == null)
                return false;

            List<PSPropertyExpressionResult> results = _groupingKeyExpression.GetValues(so);

            // if we have more that one match, we have to select the first one
            if (results.Count > 0 && results[0].Exception == null)
            {
                // no exception got thrown, so we can update
                object newValue = results[0].Result;
                object oldValue = _currentGroupingKeyPropertyValue;

                _currentGroupingKeyPropertyValue = newValue;

                // now do the comparison
                bool update = !(IsEqual(_currentGroupingKeyPropertyValue, oldValue) ||
                                IsEqual(oldValue, _currentGroupingKeyPropertyValue));

                if (update && _label == null)
                {
                    _groupingKeyDisplayName = results[0].ResolvedExpression.ToString();
                }

                return update;
            }

            // we had no matches or we could not get the value:
            // NOTICE: we need to do this to avoid starting a new group every time
            // there is a failure to read the grouping property.
            // For example, for AD, there are objects that throw when trying
            // to read the "distinguishedName" property (used by the brokered property "ParentPath)
            return false;
        }

        private static bool IsEqual(object first, object second)
        {
            if (LanguagePrimitives.TryCompare(first, second, true, CultureInfo.CurrentCulture, out int result))
            {
                return result == 0;
            }

            // Note that this will occur if the objects do not support
            // IComparable.  We fall back to comparing as strings.

            // being here means the first object doesn't support ICompare
            // or an Exception was raised win Compare
            string firstString = PSObject.AsPSObject(first).ToString();
            string secondString = PSObject.AsPSObject(second).ToString();

            return string.Equals(firstString, secondString, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Value of the display label passed in.
        /// </summary>
        private string _label = null;

        /// <summary>
        /// Value of the current active grouping key.
        /// </summary>
        private string _groupingKeyDisplayName = null;

        /// <summary>
        /// Name of the current grouping key.
        /// </summary>
        private PSPropertyExpression _groupingKeyExpression = null;

        /// <summary>
        /// The current value of the grouping key.
        /// </summary>
        private object _currentGroupingKeyPropertyValue = AutomationNull.Value;
    }
}
