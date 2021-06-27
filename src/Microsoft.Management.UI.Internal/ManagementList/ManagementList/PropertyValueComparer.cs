// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Provides a mechanism for comparing objects based on specific properties.
    /// </summary>
    internal class PropertyValueComparer : IComparer
    {
        private IPropertyValueGetter propertyValueGetter;
        private List<UIPropertyGroupDescription> dataDescriptions;
        private bool sortRecursively = true;

        /// <summary>
        /// Initializes a new instance of <see cref="PropertyValueComparer"/>.
        /// </summary>
        /// <param name="dataDescriptions">The data descriptions containing sort information for all columns.</param>
        /// <param name="sortRecursively">Whether sorting should compare additional columns when equal values are found.</param>
        /// <param name="valueGetter">The <see cref="PropertyValueGetter"/> used to retrieve property values.</param>
        public PropertyValueComparer(List<UIPropertyGroupDescription> dataDescriptions, bool sortRecursively, IPropertyValueGetter valueGetter)
        {
            this.propertyValueGetter = valueGetter;
            this.dataDescriptions = dataDescriptions;
            this.sortRecursively = sortRecursively;
        }

        /// <summary>
        /// Compares properties of the specified objects and returns a value indicating whether one is less than, equal to or greater than the other.
        /// </summary>
        /// <param name="a">The first object to compare.</param>
        /// <param name="b">The second object to compare.</param>
        /// <returns>
        /// Less than zero if <paramref name="a"/> is less than <paramref name="b"/>;
        /// greater than zero if <paramref name="a"/> is greater than <paramref name="b"/>;
        /// otherwise, zero.
        /// </returns>
        public int Compare(object a, object b)
        {
            foreach (UIPropertyGroupDescription dataDescription in this.dataDescriptions)
            {
                object firstValue;
                object secondValue;
                this.GetPropertyValues(dataDescription.PropertyName, a, b, out firstValue, out secondValue);

                int result = this.CompareData(firstValue, secondValue, dataDescription.StringComparison);

                // If multi-level sorting is enabled and values are the same, determine order from subsequent columns \\
                if (this.sortRecursively && result == 0)
                {
                    continue;
                }

                // Adjust the result according to the sort order \\
                if (dataDescription.SortDirection == ListSortDirection.Descending)
                {
                    return -result;
                }

                return result;
            }

            return 0;
        }

        private void GetPropertyValues(string propertyName, object a, object b, out object firstValue, out object secondValue)
        {
            // NOTE : Being unable to retrieve the value is equivalent to getting a null in
            // this case since they will both be treated as "having no value" for CompareTo.
            firstValue = null;
            secondValue = null;

            if (!this.propertyValueGetter.TryGetPropertyValue(propertyName, a, out firstValue))
            {
                firstValue = null;
            }

            if (!this.propertyValueGetter.TryGetPropertyValue(propertyName, b, out secondValue))
            {
                secondValue = null;
            }
        }

        [SuppressMessage("Performance", "CA1822: Mark members as static", Justification = "Potential breaking change")]
        private int CompareData(object firstValue, object secondValue, StringComparison stringComparison)
        {
            // If both values are null, do nothing; otherwise, if one is null promote the other \\
            if (firstValue == null && secondValue == null)
            {
                return 0;
            }

            if (secondValue == null)
            {
                return 1;
            }

            if (firstValue == null)
            {
                return -1;
            }

            Type firstType = firstValue.GetType();
            Type secondType = secondValue.GetType();

            if (firstType != secondType)
            {
                return LanguagePrimitives.Compare(firstValue, secondValue);
            }

            // If the values are strings, compare them considering stringComparison
            string firstString = firstValue as string;

            if (firstString != null)
            {
                string secondString = secondValue as string;
                return string.Compare(firstString, secondString, stringComparison);
            }
            else
            {
                IComparable firstComparable = firstValue as IComparable;

                // If the values are comparable use CompareTo(), otherwise compare as string considering stringComparison
                if (firstComparable != null)
                {
                    return firstComparable.CompareTo(secondValue);
                }
                else
                {
                    return string.Compare(firstValue.ToString(), secondValue.ToString(), stringComparison);
                }
            }
        }
    }
}
