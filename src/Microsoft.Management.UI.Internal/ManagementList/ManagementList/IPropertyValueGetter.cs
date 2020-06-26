// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Provides methods for retrieving the property values of objects.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public interface IPropertyValueGetter
    {
        /// <summary>Gets the value of the specified property on the specified object.</summary>
        /// <param name="propertyName">The name of the property to get the value for.</param>
        /// <param name="value">The object to get value from.</param>
        /// <param name="propertyValue">The value of the property.</param>
        /// <returns><c>true</c> if the property value could be retrieved; otherwise, <c>false</c>.</returns>
        bool TryGetPropertyValue(string propertyName, object value, out object propertyValue);

        /// <summary>Gets the value of the specified property on the specified object.</summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="propertyName">The name of the property to get the value for.</param>
        /// <param name="value">The object to get value from.</param>
        /// <param name="propertyValue">The value of the property if it exists; otherwise, <c>default(T)</c>.</param>
        /// <returns><c>true</c> if the property value of the specified type could be retrieved; otherwise, <c>false</c>.</returns>
        bool TryGetPropertyValue<T>(string propertyName, object value, out T propertyValue);
    }
}
