// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// This class contains strings required for serialization for ConvertTo-XML.
    /// </summary>
    internal static class CustomSerializationStrings
    {
        #region element tags

        /// <summary>
        /// Element tag for root node.
        /// </summary>
        internal const string RootElementTag = "Objects";

        /// <summary>
        /// Element tag for PSObject.
        /// </summary>
        internal const string PSObjectTag = "Object";

        /// <summary>
        /// Element tag for properties.
        /// </summary>
        internal const string Properties = "Property";

        #region attribute tags

        /// <summary>
        /// String for name attribute.
        /// </summary>
        internal const string NameAttribute = "Name";

        /// <summary>
        /// String for type attribute.
        /// </summary>
        internal const string TypeAttribute = "Type";

        #endregion

        #region known container tags

        /// <summary>
        /// Value of name attribute for dictionary key part in dictionary entry.
        /// </summary>
        internal const string DictionaryKey = "Key";

        /// <summary>
        /// Value of name attribute for dictionary value part in dictionary entry.
        /// </summary>
        internal const string DictionaryValue = "Value";

        #endregion

        #endregion
    }
}
