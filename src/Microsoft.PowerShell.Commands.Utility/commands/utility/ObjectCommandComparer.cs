// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Collections;
using System.Globalization;
using System.Management.Automation;

#endregion

namespace Microsoft.PowerShell.Commands
{
    #region PSObject Comparer

    /// <summary>
    /// Keeps the property value of inputObject. Because the value of a non-existing property is null,
    /// isExistingProperty is needed to distinguish whether a property exists and its value is null or
    /// the property does not exist at all.
    /// </summary>
    internal class ObjectCommandPropertyValue
    {
        private ObjectCommandPropertyValue() { }

        internal ObjectCommandPropertyValue(object propVal)
        {
            PropertyValue = propVal;
            IsExistingProperty = true;
        }

        /// <summary>
        /// ObjectCommandPropertyValue constructor.
        /// </summary>
        /// <param name="propVal">Property Value.</param>
        /// <param name="isCaseSensitive">Indicates if the Property value comparison has to be case sensitive or not.</param>
        /// <param name="cultureInfo">Culture Info of the Property Value.</param>
        internal ObjectCommandPropertyValue(object propVal, bool isCaseSensitive, CultureInfo cultureInfo)
            : this(propVal)
        {
            _caseSensitive = isCaseSensitive;
            this.cultureInfo = cultureInfo;
        }

        internal object PropertyValue { get; }

        internal bool IsExistingProperty { get; }

        /// <summary>
        /// Indicates if the Property Value comparison has to be Case sensitive or not.
        /// </summary>
        internal SwitchParameter CaseSensitive
        {
            get { return _caseSensitive; }
        }

        /// <summary>
        /// Gets the Culture Info of the Property Value.
        /// </summary>
        internal CultureInfo Culture
        {
            get
            {
                return cultureInfo;
            }
        }

        internal static readonly ObjectCommandPropertyValue NonExistingProperty = new ObjectCommandPropertyValue();
        internal static readonly ObjectCommandPropertyValue ExistingNullProperty = new ObjectCommandPropertyValue(null);
        private bool _caseSensitive;
        internal CultureInfo cultureInfo = null;

        /// <summary>
        /// Provides an Equals implementation.
        /// </summary>
        /// <param name="inputObject">Input Object.</param>
        /// <returns>True if both the objects are same or else returns false.</returns>
        public override bool Equals(Object inputObject)
        {
            ObjectCommandPropertyValue objectCommandPropertyValueObject = inputObject as ObjectCommandPropertyValue;
            if (objectCommandPropertyValueObject == null)
            {
                return false;
            }

            object baseObject = PSObject.Base(PropertyValue);
            object inComingbaseObjectPropertyValue = PSObject.Base(objectCommandPropertyValueObject.PropertyValue);

            if (baseObject is IComparable)
            {
                var success = LanguagePrimitives.TryCompare(baseObject, inComingbaseObjectPropertyValue, CaseSensitive, Culture, out int result);
                return success && result == 0;
            }

            if (baseObject == null && inComingbaseObjectPropertyValue == null)
            {
                return true;
            }

            if (baseObject != null && inComingbaseObjectPropertyValue != null)
            {
                return baseObject.ToString().Equals(inComingbaseObjectPropertyValue.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            // One of the property values being compared is null.
            return false;
        }

        /// <summary>
        /// Provides a GetHashCode() implementation.
        /// </summary>
        /// <returns>Hashcode in the form of an integer.</returns>
        public override int GetHashCode()
        {
            if (PropertyValue == null)
            {
                return 0;
            }

            object baseObject = PSObject.Base(PropertyValue);
            if (baseObject == null)
            {
                return 0;
            }

            if (baseObject is IComparable)
            {
                return baseObject.GetHashCode();
            }
            else
            {
                return baseObject.ToString().GetHashCode();
            }
        }
    }

    /// <summary>
    /// ObjectCommandComparer class.
    /// </summary>
    internal class ObjectCommandComparer : IComparer
    {
        /// <summary>
        /// Constructor that doesn't set any private field.
        /// Necessary because compareTo can compare two objects by calling
        /// ((ICompare)obj1).CompareTo(obj2) without using a key.
        /// </summary>
        internal ObjectCommandComparer(bool ascending, CultureInfo cultureInfo, bool caseSensitive)
        {
            _ascendingOrder = ascending;
            _cultureInfo = cultureInfo ?? CultureInfo.CurrentCulture;
            _caseSensitive = caseSensitive;
        }

        private static bool IsValueNull(object value)
        {
            object val = PSObject.Base(value);
            return (val == null);
        }

        internal int Compare(ObjectCommandPropertyValue first, ObjectCommandPropertyValue second)
        {
            if (first.IsExistingProperty && second.IsExistingProperty)
            {
                return Compare(first.PropertyValue, second.PropertyValue);
            }
            // if first.IsExistingProperty, !second.IsExistingProperty; otherwise the
            // first branch if would return. Regardless of key orders non existing property
            // will be considered greater than others
            if (first.IsExistingProperty)
            {
                return -1;
            }
            // vice versa for the previous branch
            if (second.IsExistingProperty)
            {
                return 1;
            }
            // both are nonexisting
            return 0;
        }

        /// <summary>
        /// Main method that will compare first and second by their keys considering case and order.
        /// </summary>
        /// <param name="first">
        /// First object to extract value.
        /// </param>
        /// <param name="second">
        /// Second object to extract value.
        /// </param>
        /// <returns>
        /// 0 if they are the same, less than 0 if first is smaller, more than 0 if first is greater.
        ///</returns>
        public int Compare(object first, object second)
        {
            // This method will never throw exceptions, two null
            // objects are considered the same
            if (IsValueNull(first) && IsValueNull(second))
            {
                return 0;
            }

            if (first is PSObject firstMsh)
            {
                first = firstMsh.BaseObject;
            }

            if (second is PSObject secondMsh)
            {
                second = secondMsh.BaseObject;
            }

            if (LanguagePrimitives.TryCompare(first, second, !_caseSensitive, _cultureInfo, out int result))
            {
                return result * (_ascendingOrder ? 1 : -1);
            }

            // Note that this will occur if the objects do not support
            // IComparable.  We fall back to comparing as strings.

            // being here means the first object doesn't support ICompare
            string firstString = PSObject.AsPSObject(first).ToString();
            string secondString = PSObject.AsPSObject(second).ToString();

            return _cultureInfo.CompareInfo.Compare(firstString, secondString, _caseSensitive ? CompareOptions.None : CompareOptions.IgnoreCase) * (_ascendingOrder ? 1 : -1);
        }

        private CultureInfo _cultureInfo = null;

        private bool _ascendingOrder = true;

        private bool _caseSensitive = false;
    }
    #endregion
}
