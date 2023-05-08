// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// This class contains strings required for serialization.
    /// </summary>
    internal static class SerializationStrings
    {
        #region element tags

        /// <summary>
        /// Element tag for root node.
        /// </summary>
        internal const string RootElementTag = "Objs";

        #region PSObject

        /// <summary>
        /// Element tag for PSObject.
        /// </summary>
        internal const string PSObjectTag = "Obj";

        /// <summary>
        /// Element tag for properties.
        /// </summary>
        internal const string AdapterProperties = "Props";

        /// <summary>
        /// TypeNames tag.
        /// </summary>
        internal const string TypeNamesTag = "TN";
        /// <summary>
        /// Tag for type item in typenames.
        /// </summary>
        internal const string TypeNamesItemTag = "T";
        /// <summary>
        /// TypeName reference.
        /// </summary>
        internal const string TypeNamesReferenceTag = "TNRef";

        /// <summary>
        /// Memberset.
        /// </summary>
        internal const string MemberSet = "MS";

        /// <summary>
        /// Individual notes.
        /// </summary>
        internal const string NoteProperty = "N";

        /// <summary>
        /// Tag for ToString value.
        /// </summary>
        internal const string ToStringElementTag = "ToString";

        #endregion PSObject

        #region known container tags

        /// <summary>
        /// Element tag used for IEnumerables.
        /// </summary>
        internal const string CollectionTag = "IE";

        /// <summary>
        /// Element tag used for Dictionary.
        /// </summary>
        internal const string DictionaryTag = "DCT";

        /// <summary>
        /// Element tag used for Dictionary entry.
        /// </summary>
        internal const string DictionaryEntryTag = "En";

        /// <summary>
        /// Value of name attribute for dictionary key part in dictionary entry.
        /// </summary>
        internal const string DictionaryKey = "Key";

        /// <summary>
        /// Value of name attribute for dictionary value part in dictionary entry.
        /// </summary>
        internal const string DictionaryValue = "Value";

        /// <summary>
        /// Element tag used for Stack.
        /// </summary>
        internal const string StackTag = "STK";

        /// <summary>
        /// Element tag used for Queue.
        /// </summary>
        internal const string QueueTag = "QUE";

        /// <summary>
        /// Element tag used for List.
        /// </summary>
        internal const string ListTag = "LST";

        #endregion known container tags

        #region primitive known type tags
        /// <summary>
        /// Element tag for char property.
        /// </summary>
        /// <remarks>This property is used for System.Char type</remarks>
        internal const string CharTag = "C";

        /// <summary>
        /// Element tag for guid property.
        /// </summary>
        /// <remarks>This property is used for System.Guid type</remarks>
        internal const string GuidTag = "G";

        /// <summary>
        /// Element tag for boolean property.
        /// </summary>
        /// <remarks>This property is used for System.Boolean type</remarks>
        internal const string BooleanTag = "B";

        /// <summary>
        /// Element tag for unsignedByte property.
        /// </summary>
        /// <remarks>This property is used for System.Byte type</remarks>
        internal const string UnsignedByteTag = "By";

        /// <summary>
        /// Element tag for dateTime property.
        /// </summary>
        /// <remarks>This property is used for System.DateTime type</remarks>
        internal const string DateTimeTag = "DT";

        /// <summary>
        /// Element tag for decimal property.
        /// </summary>
        /// <remarks>This property is used for System.Decimal type</remarks>
        internal const string DecimalTag = "D";

        /// <summary>
        /// Element tag for double property.
        /// </summary>
        /// <remarks>This property is used for System.Double type</remarks>
        internal const string DoubleTag = "Db";

        /// <summary>
        /// Element tag for duration property.
        /// </summary>
        /// <remarks>This property is used for System.TimeSpan type</remarks>
        internal const string DurationTag = "TS";

        /// <summary>
        /// Element tag for float property.
        /// </summary>
        /// <remarks>This property is used for System.Single type</remarks>
        internal const string FloatTag = "Sg";

        /// <summary>
        /// Element tag for int property.
        /// </summary>
        /// <remarks>This property is used for System.Int32 type</remarks>
        internal const string IntTag = "I32";

        /// <summary>
        /// Element tag for long property.
        /// </summary>
        /// <remarks>This property is used for System.Int64 type</remarks>
        internal const string LongTag = "I64";

        /// <summary>
        /// Element tag for byte property.
        /// </summary>
        /// <remarks>This property is used for System.SByte type</remarks>
        internal const string ByteTag = "SB";

        /// <summary>
        /// Element tag for short property.
        /// </summary>
        /// <remarks>This property is used for System.Int16 type</remarks>
        internal const string ShortTag = "I16";

        /// <summary>
        /// Element tag for base64Binary property.
        /// </summary>
        /// <remarks>This property is used for System.IO.Stream type</remarks>
        internal const string Base64BinaryTag = "BA";

        /// <summary>
        /// Element tag for scriptblock property.
        /// </summary>
        /// <remarks>This property is used for System.Management.Automation.ScriptBlock type</remarks>
        internal const string ScriptBlockTag = "SBK";

        /// <summary>
        /// Element tag for string property.
        /// </summary>
        /// <remarks>This property is used for System.String type</remarks>
        internal const string StringTag = "S";

        /// <summary>
        /// Element tag for secure string property.
        /// </summary>
        /// <remarks>This property is used for System.Security.SecureString type</remarks>
        internal const string SecureStringTag = "SS";

        /// <summary>
        /// Element tag for unsignedShort property.
        /// </summary>
        /// <remarks>This property is used for System.UInt16 Stream type</remarks>
        internal const string UnsignedShortTag = "U16";

        /// <summary>
        /// Element tag for unsignedInt property.
        /// </summary>
        /// <remarks>This property is used for System.UInt32 type</remarks>
        internal const string UnsignedIntTag = "U32";

        /// <summary>
        /// Element tag for unsignedLong property.
        /// </summary>
        /// <remarks>This property is used for System.Long type</remarks>
        internal const string UnsignedLongTag = "U64";

        /// <summary>
        /// Element tag for anyUri property.
        /// </summary>
        /// <remarks>This property is used for System.Uri type</remarks>
        internal const string AnyUriTag = "URI";

        /// <summary>
        /// Element tag for Version property.
        /// </summary>
        internal const string VersionTag = "Version";

        /// <summary>
        /// Element tag for SemanticVersion property.
        /// </summary>
        internal const string SemanticVersionTag = "SemanticVersion";

        /// <summary>
        /// Element tag for XmlDocument.
        /// </summary>
        internal const string XmlDocumentTag = "XD";

        /// <summary>
        /// Element tag for property whose value is null.
        /// </summary>
        internal const string NilTag = "Nil";

        /// <summary>
        /// Element tag for PSObjectReference property.
        /// </summary>
        /// <remarks>This property is used for a reference to a property bag</remarks>
        internal const string ReferenceTag = "Ref";

        #region progress record

        internal const string ProgressRecord = "PR";
        internal const string ProgressRecordActivityId = "AI";
        internal const string ProgressRecordParentActivityId = "PI";
        internal const string ProgressRecordActivity = "AV";
        internal const string ProgressRecordStatusDescription = "SD";
        internal const string ProgressRecordCurrentOperation = "CO";
        internal const string ProgressRecordPercentComplete = "PC";
        internal const string ProgressRecordSecondsRemaining = "SR";
        internal const string ProgressRecordType = "T";

        #endregion progress record

        #endregion primitive known type tags

        #endregion element tags

        #region attribute tags
        /// <summary>
        /// String for reference id attribute.
        /// </summary>
        internal const string ReferenceIdAttribute = "RefId";

        /// <summary>
        /// String for name attribute.
        /// </summary>
        internal const string NameAttribute = "N";

        /// <summary>
        /// String for version attribute.
        /// </summary>
        internal const string VersionAttribute = "Version";

        /// <summary>
        /// String for stream attribute.
        /// </summary>
        internal const string StreamNameAttribute = "S";

        #endregion attribute tags

        #region namespace values

        /// <summary>
        /// Monad namespace.
        /// </summary>
        internal const string MonadNamespace = "http://schemas.microsoft.com/powershell/2004/04";

        /// <summary>
        /// Prefix string for monad namespace.
        /// </summary>
        internal const string MonadNamespacePrefix = "ps";

        #endregion namespace values
    }
}
