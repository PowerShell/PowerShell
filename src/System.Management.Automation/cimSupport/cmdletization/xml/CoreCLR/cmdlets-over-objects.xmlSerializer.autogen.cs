// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if CORECLR

using System;
using System.Collections;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;

#pragma warning disable

namespace Microsoft.PowerShell.Cmdletization.Xml
{
    internal class XmlSerializationReader1
    {
        #region Copy_From_XmlSerializationReader

        // The fields, properties and methods in this section are copied from XmlSerializationReader with
        // some necessary adjustment:
        //  1. XmlReader.ReadString() and XmlReader.ReadElementString() are not in CoreCLR. They are replaced by
        //     XmlReader.ReadElementContentAsString() as suggested in MSDN.
        //  2. GetXsiType(). In the context of CDXML deserialization, GetXsiType() will always return null, as all
        //     CDXML files are under the namespace "http://schemas.microsoft.com/cmdlets-over-objects/2009/11".
        //  3. ReadTypedPrimitive(XmlQualifiedName type) and ReadTypedNull(XmlQualifiedName type). See the comments
        //     in them for more information.

        #region "Constructor"

        internal XmlSerializationReader1(XmlReader reader)
        {
            _r = reader;
            _d = null;

            _schemaNsID = _r.NameTable.Add("http://www.w3.org/2001/XMLSchema");
            _schemaNs2000ID = _r.NameTable.Add("http://www.w3.org/2000/10/XMLSchema");
            _schemaNs1999ID = _r.NameTable.Add("http://www.w3.org/1999/XMLSchema");
            _schemaNonXsdTypesNsID = _r.NameTable.Add("http://microsoft.com/wsdl/types/");
            _instanceNsID = _r.NameTable.Add("http://www.w3.org/2001/XMLSchema-instance");
            _instanceNs2000ID = _r.NameTable.Add("http://www.w3.org/2000/10/XMLSchema-instance");
            _instanceNs1999ID = _r.NameTable.Add("http://www.w3.org/1999/XMLSchema-instance");
            _soapNsID = _r.NameTable.Add("http://schemas.xmlsoap.org/soap/encoding/");
            _soap12NsID = _r.NameTable.Add("http://www.w3.org/2003/05/soap-encoding");
            _schemaID = _r.NameTable.Add("schema");
            _wsdlNsID = _r.NameTable.Add("http://schemas.xmlsoap.org/wsdl/");
            _wsdlArrayTypeID = _r.NameTable.Add("arrayType");
            _nullID = _r.NameTable.Add("null");
            _nilID = _r.NameTable.Add("nil");
            _typeID = _r.NameTable.Add("type");
            _arrayTypeID = _r.NameTable.Add("arrayType");
            _itemTypeID = _r.NameTable.Add("itemType");
            _arraySizeID = _r.NameTable.Add("arraySize");
            _arrayID = _r.NameTable.Add("Array");
            _urTypeID = _r.NameTable.Add("anyType");
            InitIDs();
        }

        #endregion "Constructor"

        #region "Field Definition"

        XmlReader _r;
        XmlDocument _d;

        bool _soap12;
        bool _isReturnValue;
        bool _decodeName = true;

        string _schemaNsID;
        string _schemaNs1999ID;
        string _schemaNs2000ID;
        string _schemaNonXsdTypesNsID;
        string _instanceNsID;
        string _instanceNs2000ID;
        string _instanceNs1999ID;
        string _soapNsID;
        string _soap12NsID;
        string _schemaID;
        string _wsdlNsID;
        string _wsdlArrayTypeID;
        string _nullID;
        string _nilID;
        string _typeID;
        string _arrayTypeID;
        string _itemTypeID;
        string _arraySizeID;
        string _arrayID;
        string _urTypeID;
        string _stringID;
        string _intID;
        string _booleanID;
        string _shortID;
        string _longID;
        string _floatID;
        string _doubleID;
        string _decimalID;
        string _dateTimeID;
        string _qnameID;
        string _dateID;
        string _timeID;
        string _hexBinaryID;
        string _base64BinaryID;
        string _base64ID;
        string _unsignedByteID;
        string _byteID;
        string _unsignedShortID;
        string _unsignedIntID;
        string _unsignedLongID;
        string _oldDecimalID;
        string _oldTimeInstantID;

        string _anyURIID;
        string _durationID;
        string _ENTITYID;
        string _ENTITIESID;
        string _gDayID;
        string _gMonthID;
        string _gMonthDayID;
        string _gYearID;
        string _gYearMonthID;
        string _IDID;
        string _IDREFID;
        string _IDREFSID;
        string _integerID;
        string _languageID;
        string _nameID;
        string _NCNameID;
        string _NMTOKENID;
        string _NMTOKENSID;
        string _negativeIntegerID;
        string _nonPositiveIntegerID;
        string _nonNegativeIntegerID;
        string _normalizedStringID;
        string _NOTATIONID;
        string _positiveIntegerID;
        string _tokenID;

        string _charID;
        string _guidID;

        static object s_primitiveTypedObject = new object();

        #endregion "Field Definition"

        #region "Property Definition"

        internal XmlReader Reader
        {
            get
            {
                return _r;
            }
        }

        internal int ReaderCount
        {
            get
            {
                // XmlSerializationReader implementation is:
                //    return checkDeserializeAdvances ? countingReader.AdvanceCount : 0;
                // and checkDeserializeAdvances is set in the static constructor:
                //    XmlSerializerSection configSection = ConfigurationManager.GetSection(ConfigurationStrings.XmlSerializerSectionPath) as XmlSerializerSection;
                //    checkDeserializeAdvances = (configSection == null) ? false : configSection.CheckDeserializeAdvances;
                // When XmlSerializationReader is used in powershell, there is no configuration file defined for it, so 'checkDeserializeAdvances' will actually
                // always be 'false'. Therefore, here we directly return 0 for 'ReaderCount'.
                return 0;
            }
        }

        internal bool DecodeName
        {
            get
            {
                return _decodeName;
            }

            set
            {
                _decodeName = value;
            }
        }

        protected XmlDocument Document
        {
            get
            {
                if (_d == null)
                {
                    _d = new XmlDocument(_r.NameTable);
                }

                return _d;
            }
        }

        #endregion "Property Definition"

        #region "Method Definition"

        internal void InitPrimitiveIDs()
        {
            if (_tokenID != null) return;

            object ns = _r.NameTable.Add("http://www.w3.org/2001/XMLSchema");
            object ns2 = _r.NameTable.Add("http://microsoft.com/wsdl/types/");

            _stringID = _r.NameTable.Add("string");
            _intID = _r.NameTable.Add("int");
            _booleanID = _r.NameTable.Add("boolean");
            _shortID = _r.NameTable.Add("short");
            _longID = _r.NameTable.Add("long");
            _floatID = _r.NameTable.Add("float");
            _doubleID = _r.NameTable.Add("double");
            _decimalID = _r.NameTable.Add("decimal");
            _dateTimeID = _r.NameTable.Add("dateTime");
            _qnameID = _r.NameTable.Add("QName");
            _dateID = _r.NameTable.Add("date");
            _timeID = _r.NameTable.Add("time");
            _hexBinaryID = _r.NameTable.Add("hexBinary");
            _base64BinaryID = _r.NameTable.Add("base64Binary");
            _unsignedByteID = _r.NameTable.Add("unsignedByte");
            _byteID = _r.NameTable.Add("byte");
            _unsignedShortID = _r.NameTable.Add("unsignedShort");
            _unsignedIntID = _r.NameTable.Add("unsignedInt");
            _unsignedLongID = _r.NameTable.Add("unsignedLong");
            _oldDecimalID = _r.NameTable.Add("decimal");
            _oldTimeInstantID = _r.NameTable.Add("timeInstant");
            _charID = _r.NameTable.Add("char");
            _guidID = _r.NameTable.Add("guid");
            _base64ID = _r.NameTable.Add("base64");

            _anyURIID = _r.NameTable.Add("anyURI");
            _durationID = _r.NameTable.Add("duration");
            _ENTITYID = _r.NameTable.Add("ENTITY");
            _ENTITIESID = _r.NameTable.Add("ENTITIES");
            _gDayID = _r.NameTable.Add("gDay");
            _gMonthID = _r.NameTable.Add("gMonth");
            _gMonthDayID = _r.NameTable.Add("gMonthDay");
            _gYearID = _r.NameTable.Add("gYear");
            _gYearMonthID = _r.NameTable.Add("gYearMonth");
            _IDID = _r.NameTable.Add("ID");
            _IDREFID = _r.NameTable.Add("IDREF");
            _IDREFSID = _r.NameTable.Add("IDREFS");
            _integerID = _r.NameTable.Add("integer");
            _languageID = _r.NameTable.Add("language");
            _nameID = _r.NameTable.Add("Name");
            _NCNameID = _r.NameTable.Add("NCName");
            _NMTOKENID = _r.NameTable.Add("NMTOKEN");
            _NMTOKENSID = _r.NameTable.Add("NMTOKENS");
            _negativeIntegerID = _r.NameTable.Add("negativeInteger");
            _nonNegativeIntegerID = _r.NameTable.Add("nonNegativeInteger");
            _nonPositiveIntegerID = _r.NameTable.Add("nonPositiveInteger");
            _normalizedStringID = _r.NameTable.Add("normalizedString");
            _NOTATIONID = _r.NameTable.Add("NOTATION");
            _positiveIntegerID = _r.NameTable.Add("positiveInteger");
            _tokenID = _r.NameTable.Add("token");
        }

        protected void CheckReaderCount(ref int whileIterations, ref int readerCount)
        {
        }

        private string CurrentTag()
        {
            switch (_r.NodeType)
            {
                case XmlNodeType.Element:
                    return "<" + _r.LocalName + " xmlns='" + _r.NamespaceURI + "'>";
                case XmlNodeType.EndElement:
                    return ">";
                case XmlNodeType.Text:
                    return _r.Value;
                case XmlNodeType.CDATA:
                    return "CDATA";
                case XmlNodeType.Comment:
                    return "<--";
                case XmlNodeType.ProcessingInstruction:
                    return "<?";
                default:
                    return "(unknown)";
            }
        }

        protected Exception CreateUnknownNodeException()
        {
            return new InvalidOperationException("XmlUnknownNode: " + CurrentTag());
        }

        protected Exception CreateUnknownTypeException(XmlQualifiedName type)
        {
            return new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "XmlUnknownType. Name: {0}, Namespace {1}, CurrentTag: {2}", type.Name, type.Namespace, CurrentTag()));
        }

        protected Exception CreateUnknownConstantException(string value, Type enumType)
        {
            return new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "XmlUnknownConstant. Value: {0}, EnumType: {1}", value, enumType.Name));
        }

        protected Array ShrinkArray(Array a, int length, Type elementType, bool isNullable)
        {
            if (a == null)
            {
                if (isNullable) return null;
                return Array.CreateInstance(elementType, 0);
            }

            if (a.Length == length) return a;
            Array b = Array.CreateInstance(elementType, length);
            Array.Copy(a, b, length);
            return b;
        }

        protected Array EnsureArrayIndex(Array a, int index, Type elementType)
        {
            if (a == null) return Array.CreateInstance(elementType, 32);
            if (index < a.Length) return a;
            Array b = Array.CreateInstance(elementType, a.Length * 2);
            Array.Copy(a, b, index);
            return b;
        }

        protected string CollapseWhitespace(string value)
        {
            if (value == null)
                return null;
            return value.Trim();
        }

        protected bool IsXmlnsAttribute(string name)
        {
            if (!name.StartsWith("xmlns", StringComparison.Ordinal)) return false;
            if (name.Length == 5) return true;
            return name[5] == ':';
        }

        protected void UnknownNode(object o)
        {
            UnknownNode(o, null);
        }

        protected void UnknownNode(object o, string qnames)
        {
            if (_r.NodeType == XmlNodeType.None || _r.NodeType == XmlNodeType.Whitespace)
            {
                _r.Read();
                return;
            }

            if (_r.NodeType == XmlNodeType.EndElement)
                return;

            if (_r.NodeType == XmlNodeType.Attribute)
            {
                return;
            }
            else if (_r.NodeType == XmlNodeType.Element)
            {
                _r.Skip();
                return;
            }
            else
            {
                UnknownNode(Document.ReadNode(_r), o, qnames);
            }
        }

        private void UnknownNode(XmlNode unknownNode, object o, string qnames)
        {
            if (unknownNode == null)
                return;

            // No XmlDeserializationEvents in CoreCLR. The events like 'onUnknownNode', 'onUnknownAttribute' and
            // 'onUnknownElement' are not used in powershell code, so it's safe to not perform extra operations here.
        }

        protected void ReadEndElement()
        {
            while (_r.NodeType == XmlNodeType.Whitespace) _r.Skip();
            if (_r.NodeType == XmlNodeType.None) _r.Skip();
            else _r.ReadEndElement();
        }

        protected string ReadString(string value, bool trim)
        {
            // This method is used only in Read47_ClassMetadataData and Read35_ClassMetadataData when the current XmlNodeType
            // is one of the following:
            //   XmlNodeType.Text
            //   XmlNodeType.CDATA
            //   XmlNodeType.Whitespace
            //   XmlNodeType.SignificantWhitespace
            // In this case, we use 'ReadContentAsString()' to read the text content at the current position.
            // We cannot use 'ReadElementContentAsString()'. It will fail because the XmlReader is not positioned on an Element start node.
            string str = _r.ReadContentAsString();
            if (str != null && trim)
                str = str.Trim();
            if (value == null || value.Length == 0)
                return str;
            return value + str;
        }

        protected XmlQualifiedName ToXmlQualifiedName(string value)
        {
            return ToXmlQualifiedName(value, DecodeName);
        }

        internal XmlQualifiedName ToXmlQualifiedName(string value, bool decodeName)
        {
            int colon = value == null ? -1 : value.LastIndexOf(':');
            string prefix = colon < 0 ? null : value.Substring(0, colon);
            string localName = value.Substring(colon + 1);

            if (decodeName)
            {
                prefix = XmlConvert.DecodeName(prefix);
                localName = XmlConvert.DecodeName(localName);
            }

            if (prefix == null || prefix.Length == 0)
            {
                return new XmlQualifiedName(_r.NameTable.Add(value), _r.LookupNamespace(string.Empty));
            }
            else
            {
                string ns = _r.LookupNamespace(prefix);
                if (ns == null)
                {
                    // Namespace prefix '{0}' is not defined.
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "XmlUndefinedAlias. Prefix: {0}", prefix));
                }

                return new XmlQualifiedName(_r.NameTable.Add(localName), ns);
            }
        }

        /// <summary>
        /// In the context of CDXML deserialization, GetXsiType() will
        /// always return null, as all CDXML files are under the namespace
        /// "http://schemas.microsoft.com/cmdlets-over-objects/2009/11",
        /// so the GetAttribute(..) operation here will always return null.
        /// </summary>
        protected XmlQualifiedName GetXsiType()
        {
            string type = _r.GetAttribute(_typeID, _instanceNsID);
            if (type == null)
            {
                type = _r.GetAttribute(_typeID, _instanceNs2000ID);
                if (type == null)
                {
                    type = _r.GetAttribute(_typeID, _instanceNs1999ID);
                    if (type == null)
                        return null;
                }
            }

            return ToXmlQualifiedName(type, false);
        }

        protected bool GetNullAttr()
        {
            string isNull = _r.GetAttribute(_nilID, _instanceNsID);
            if (isNull == null)
                isNull = _r.GetAttribute(_nullID, _instanceNsID);
            if (isNull == null)
            {
                isNull = _r.GetAttribute(_nullID, _instanceNs2000ID);
                if (isNull == null)
                    isNull = _r.GetAttribute(_nullID, _instanceNs1999ID);
            }

            if (isNull == null || !XmlConvert.ToBoolean(isNull)) return false;
            return true;
        }

        protected bool ReadNull()
        {
            if (!GetNullAttr()) return false;
            if (_r.IsEmptyElement)
            {
                _r.Skip();
                return true;
            }

            _r.ReadStartElement();
            int whileIterations = 0;
            int readerCount = ReaderCount;
            while (_r.NodeType != XmlNodeType.EndElement)
            {
                UnknownNode(null);
                CheckReaderCount(ref whileIterations, ref readerCount);
            }

            ReadEndElement();
            return true;
        }

        bool IsPrimitiveNamespace(string ns)
        {
            return (object)ns == (object)_schemaNsID ||
                   (object)ns == (object)_schemaNonXsdTypesNsID ||
                   (object)ns == (object)_soapNsID ||
                   (object)ns == (object)_soap12NsID ||
                   (object)ns == (object)_schemaNs2000ID ||
                   (object)ns == (object)_schemaNs1999ID;
        }

        protected object ReadTypedPrimitive(XmlQualifiedName type)
        {
            InitPrimitiveIDs();

            // This method is only used in Read1_Object(bool isNullable, bool checkType).
            // This method is called only when we want to get a value for tag elements that don't take values, such as
            // ValidateNotNull, AllowNull, AllEmptyString, ErrorCode and etc. e.g. <ValidateNotNullOrEmpty />, <ErrorCode />.
            // We don't actually use the value, only check if the value is null, so as to decide whether the tag element
            // is specified in CDXML file.
            if (!IsPrimitiveNamespace(type.Namespace) || (object)type.Name == (object)_urTypeID)
                return s_primitiveTypedObject;

            // CDXML files are all under the namespace 'http://schemas.microsoft.com/cmdlets-over-objects/2009/11', so
            // they will never fall into the following namespaces:
            //     schemaNsID, soapNsID, soap12NsID, schemaNs2000ID, schemaNs1999ID, schemaNonXsdTypesNsID
            //
            // Actually, in the context of CDXML deserialization, GetXsiType() will always return null, so
            // the only possible 'type' passed in this method should be like this:
            //     type.Name = "anyType"; type.Namespace = "http://www.w3.org/2001/XMLSchema"
            // Therefore, execution of this method should always fall in the above IF block.
            throw new InvalidOperationException("ReadTypedPrimitive - code should be unreachable for its usage in CDXML.");
        }

        protected object ReadTypedNull(XmlQualifiedName type)
        {
            InitPrimitiveIDs();

            // This method is only used in Read1_Object(bool isNullable, bool checkType).
            // This method is invoked only if GetXsiType() returns a value that is not null. Actually, in the context of
            // CDXML deserialization, GetXsiType() will always return null, so this method will never be called in runtime.
            return null;
        }

        #endregion "Method Definition"

        #endregion Copy_From_XmlSerializationReader

        public object Read50_PowerShellMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id1_PowerShellMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    o = Read39_PowerShellMetadata(false, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:PowerShellMetadata");
            }

            return (object)o;
        }

        public object Read51_ClassMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id3_ClassMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read36_ClassMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":ClassMetadata");
            }

            return (object)o;
        }

        public object Read52_ClassMetadataInstanceCmdlets()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id5_ClassMetadataInstanceCmdlets && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read40_ClassMetadataInstanceCmdlets(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":ClassMetadataInstanceCmdlets");
            }

            return (object)o;
        }

        public object Read53_GetCmdletParameters()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id6_GetCmdletParameters && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read19_GetCmdletParameters(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":GetCmdletParameters");
            }

            return (object)o;
        }

        public object Read54_PropertyMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id7_PropertyMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read15_PropertyMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":PropertyMetadata");
            }

            return (object)o;
        }

        public object Read55_TypeMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id8_TypeMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read2_TypeMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":TypeMetadata");
            }

            return (object)o;
        }

        public object Read56_Association()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id9_Association && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read17_Association(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":Association");
            }

            return (object)o;
        }

        public object Read57_AssociationAssociatedInstance()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id10_AssociationAssociatedInstance && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read41_AssociationAssociatedInstance(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":AssociationAssociatedInstance");
            }

            return (object)o;
        }

        public object Read58_CmdletParameterMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id11_CmdletParameterMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read10_CmdletParameterMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":CmdletParameterMetadata");
            }

            return (object)o;
        }

        public object Read59_Item()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id12_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read11_Item(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":CmdletParameterMetadataForGetCmdletParameter");
            }

            return (object)o;
        }

        public object Read60_Item()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id13_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read12_Item(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":CmdletParameterMetadataForGetCmdletFilteringParameter");
            }

            return (object)o;
        }

        public object Read61_Item()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id14_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read42_Item(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":CmdletParameterMetadataValidateCount");
            }

            return (object)o;
        }

        public object Read62_Item()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id15_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read43_Item(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":CmdletParameterMetadataValidateLength");
            }

            return (object)o;
        }

        public object Read63_Item()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id16_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read44_Item(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":CmdletParameterMetadataValidateRange");
            }

            return (object)o;
        }

        public object Read64_ObsoleteAttributeMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id17_ObsoleteAttributeMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read7_ObsoleteAttributeMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":ObsoleteAttributeMetadata");
            }

            return (object)o;
        }

        public object Read65_Item()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id18_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read9_Item(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":CmdletParameterMetadataForInstanceMethodParameter");
            }

            return (object)o;
        }

        public object Read66_Item()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id19_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read8_Item(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":CmdletParameterMetadataForStaticMethodParameter");
            }

            return (object)o;
        }

        public object Read67_QueryOption()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id20_QueryOption && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read18_QueryOption(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":QueryOption");
            }

            return (object)o;
        }

        public object Read68_GetCmdletMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id21_GetCmdletMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read22_GetCmdletMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":GetCmdletMetadata");
            }

            return (object)o;
        }

        public object Read69_CommonCmdletMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id22_CommonCmdletMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read21_CommonCmdletMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":CommonCmdletMetadata");
            }

            return (object)o;
        }

        public object Read70_ConfirmImpact()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id23_ConfirmImpact && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    {
                        o = Read20_ConfirmImpact(Reader.ReadElementContentAsString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":ConfirmImpact");
            }

            return (object)o;
        }

        public object Read71_StaticCmdletMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id24_StaticCmdletMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read34_StaticCmdletMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":StaticCmdletMetadata");
            }

            return (object)o;
        }

        public object Read72_Item()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id25_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read45_Item(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":StaticCmdletMetadataCmdletMetadata");
            }

            return (object)o;
        }

        public object Read73_CommonMethodMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id26_CommonMethodMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read29_CommonMethodMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":CommonMethodMetadata");
            }

            return (object)o;
        }

        public object Read74_StaticMethodMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id27_StaticMethodMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read28_StaticMethodMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":StaticMethodMetadata");
            }

            return (object)o;
        }

        public object Read75_CommonMethodParameterMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id28_CommonMethodParameterMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read26_CommonMethodParameterMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":CommonMethodParameterMetadata");
            }

            return (object)o;
        }

        public object Read76_StaticMethodParameterMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id29_StaticMethodParameterMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read27_StaticMethodParameterMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":StaticMethodParameterMetadata");
            }

            return (object)o;
        }

        public object Read77_CmdletOutputMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id30_CmdletOutputMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read23_CmdletOutputMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":CmdletOutputMetadata");
            }

            return (object)o;
        }

        public object Read78_Item()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id31_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read25_Item(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":InstanceMethodParameterMetadata");
            }

            return (object)o;
        }

        public object Read79_Item()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id32_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read46_Item(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":CommonMethodMetadataReturnValue");
            }

            return (object)o;
        }

        public object Read80_InstanceMethodMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id33_InstanceMethodMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read30_InstanceMethodMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":InstanceMethodMetadata");
            }

            return (object)o;
        }

        public object Read81_InstanceCmdletMetadata()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id34_InstanceCmdletMetadata && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read31_InstanceCmdletMetadata(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":InstanceCmdletMetadata");
            }

            return (object)o;
        }

        public object Read82_PropertyQuery()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id35_PropertyQuery && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read14_PropertyQuery(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":PropertyQuery");
            }

            return (object)o;
        }

        public object Read83_WildcardablePropertyQuery()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id36_WildcardablePropertyQuery && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read13_WildcardablePropertyQuery(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":WildcardablePropertyQuery");
            }

            return (object)o;
        }

        public object Read84_ItemsChoiceType()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id37_ItemsChoiceType && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    {
                        o = Read3_ItemsChoiceType(Reader.ReadElementContentAsString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":ItemsChoiceType");
            }

            return (object)o;
        }

        public object Read85_ClassMetadataData()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id38_ClassMetadataData && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read47_ClassMetadataData(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":ClassMetadataData");
            }

            return (object)o;
        }

        public object Read86_EnumMetadataEnum()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id39_EnumMetadataEnum && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read48_EnumMetadataEnum(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":EnumMetadataEnum");
            }

            return (object)o;
        }

        public object Read87_EnumMetadataEnumValue()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id40_EnumMetadataEnumValue && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o = Read49_EnumMetadataEnumValue(true, true);
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null, @":EnumMetadataEnumValue");
            }

            return (object)o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue Read49_EnumMetadataEnumValue(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id40_EnumMetadataEnumValue && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id41_Name && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Name = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id42_Value && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Value = CollapseWhitespace(Reader.Value);
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Name, :Value");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations0 = 0;
            int readerCount0 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    UnknownNode((object)o, string.Empty);
                }
                else
                {
                    UnknownNode((object)o, string.Empty);
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations0, ref readerCount0);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum Read48_EnumMetadataEnum(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id39_EnumMetadataEnum && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum();
            global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue[] a_0 = null;
            int ca_0 = 0;
            bool[] paramsRead = new bool[4];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id43_EnumName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@EnumName = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id44_UnderlyingType && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@UnderlyingType = Reader.Value;
                    paramsRead[2] = true;
                }
                else if (!paramsRead[3] && ((object)Reader.LocalName == (object)_id45_BitwiseFlags && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@BitwiseFlags = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@BitwiseFlagsSpecified = true;
                    paramsRead[3] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":EnumName, :UnderlyingType, :BitwiseFlags");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Value = (global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue[])ShrinkArray(a_0, ca_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations1 = 0;
            int readerCount1 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (((object)Reader.LocalName == (object)_id42_Value && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        a_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue[])EnsureArrayIndex(a_0, ca_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue)); a_0[ca_0++] = Read37_EnumMetadataEnumValue(false, true);
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Value");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Value");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations1, ref readerCount1);
            }

            o.@Value = (global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue[])ShrinkArray(a_0, ca_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue Read37_EnumMetadataEnumValue(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id4_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id41_Name && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Name = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id42_Value && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Value = CollapseWhitespace(Reader.Value);
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Name, :Value");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations2 = 0;
            int readerCount2 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    UnknownNode((object)o, string.Empty);
                }
                else
                {
                    UnknownNode((object)o, string.Empty);
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations2, ref readerCount2);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData Read47_ClassMetadataData(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id38_ClassMetadataData && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id41_Name && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Name = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Name");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations3 = 0;
            int readerCount3 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                string tmp = null;
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    UnknownNode((object)o, string.Empty);
                }
                else if (Reader.NodeType == System.Xml.XmlNodeType.Text ||
                Reader.NodeType == System.Xml.XmlNodeType.CDATA ||
                Reader.NodeType == System.Xml.XmlNodeType.Whitespace ||
                Reader.NodeType == System.Xml.XmlNodeType.SignificantWhitespace)
                {
                    tmp = ReadString(tmp, false);
                    o.@Value = tmp;
                }
                else
                {
                    UnknownNode((object)o, string.Empty);
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations3, ref readerCount3);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType Read3_ItemsChoiceType(string s)
        {
            switch (s)
            {
                case @"ExcludeQuery": return global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType.@ExcludeQuery;
                case @"MaxValueQuery": return global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType.@MaxValueQuery;
                case @"MinValueQuery": return global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType.@MinValueQuery;
                case @"RegularQuery": return global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType.@RegularQuery;
                default: throw CreateUnknownConstantException(s, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType));
            }
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.WildcardablePropertyQuery Read13_WildcardablePropertyQuery(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id36_WildcardablePropertyQuery && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.WildcardablePropertyQuery o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.WildcardablePropertyQuery();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id46_AllowGlobbing && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@AllowGlobbing = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@AllowGlobbingSpecified = true;
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":AllowGlobbing");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations4 = 0;
            int readerCount4 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id11_CmdletParameterMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletParameterMetadata = Read12_Item(false, true);
                        paramsRead[0] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations4, ref readerCount4);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataForGetCmdletFilteringParameter Read12_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id13_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataForGetCmdletFilteringParameter o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataForGetCmdletFilteringParameter();
            global::System.String[] a_8 = null;
            int ca_8 = 0;
            global::System.String[] a_11 = null;
            int ca_11 = 0;
            global::System.String[] a_16 = null;
            int ca_16 = 0;
            bool[] paramsRead = new bool[18];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[10] && ((object)Reader.LocalName == (object)_id47_IsMandatory && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@IsMandatory = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@IsMandatorySpecified = true;
                    paramsRead[10] = true;
                }
                else if (((object)Reader.LocalName == (object)_id48_Aliases && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    string listValues = Reader.Value;
                    string[] vals = listValues.Split(null);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        a_11 = (global::System.String[])EnsureArrayIndex(a_11, ca_11, typeof(global::System.String)); a_11[ca_11++] = vals[i];
                    }
                }
                else if (!paramsRead[12] && ((object)Reader.LocalName == (object)_id49_PSName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@PSName = Reader.Value;
                    paramsRead[12] = true;
                }
                else if (!paramsRead[13] && ((object)Reader.LocalName == (object)_id50_Position && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Position = CollapseWhitespace(Reader.Value);
                    paramsRead[13] = true;
                }
                else if (!paramsRead[14] && ((object)Reader.LocalName == (object)_id51_ValueFromPipeline && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ValueFromPipeline = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@ValueFromPipelineSpecified = true;
                    paramsRead[14] = true;
                }
                else if (!paramsRead[15] && ((object)Reader.LocalName == (object)_id52_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ValueFromPipelineByPropertyName = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@ValueFromPipelineByPropertyNameSpecified = true;
                    paramsRead[15] = true;
                }
                else if (((object)Reader.LocalName == (object)_id53_CmdletParameterSets && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    string listValues = Reader.Value;
                    string[] vals = listValues.Split(null);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        a_16 = (global::System.String[])EnsureArrayIndex(a_16, ca_16, typeof(global::System.String)); a_16[ca_16++] = vals[i];
                    }
                }
                else if (!paramsRead[17] && ((object)Reader.LocalName == (object)_id54_ErrorOnNoMatch && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ErrorOnNoMatch = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@ErrorOnNoMatchSpecified = true;
                    paramsRead[17] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":IsMandatory, :Aliases, :PSName, :Position, :ValueFromPipeline, :ValueFromPipelineByPropertyName, :CmdletParameterSets, :ErrorOnNoMatch");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Aliases = (global::System.String[])ShrinkArray(a_11, ca_11, typeof(global::System.String), true);
                o.@CmdletParameterSets = (global::System.String[])ShrinkArray(a_16, ca_16, typeof(global::System.String), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations5 = 0;
            int readerCount5 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id55_AllowEmptyCollection && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowEmptyCollection = Read1_Object(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id56_AllowEmptyString && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowEmptyString = Read1_Object(false, true);
                        paramsRead[1] = true;
                    }
                    else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id57_AllowNull && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowNull = Read1_Object(false, true);
                        paramsRead[2] = true;
                    }
                    else if (!paramsRead[3] && ((object)Reader.LocalName == (object)_id58_ValidateNotNull && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateNotNull = Read1_Object(false, true);
                        paramsRead[3] = true;
                    }
                    else if (!paramsRead[4] && ((object)Reader.LocalName == (object)_id59_ValidateNotNullOrEmpty && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateNotNullOrEmpty = Read1_Object(false, true);
                        paramsRead[4] = true;
                    }
                    else if (!paramsRead[5] && ((object)Reader.LocalName == (object)_id60_ValidateCount && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateCount = Read4_Item(false, true);
                        paramsRead[5] = true;
                    }
                    else if (!paramsRead[6] && ((object)Reader.LocalName == (object)_id61_ValidateLength && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateLength = Read5_Item(false, true);
                        paramsRead[6] = true;
                    }
                    else if (!paramsRead[7] && ((object)Reader.LocalName == (object)_id62_ValidateRange && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateRange = Read6_Item(false, true);
                        paramsRead[7] = true;
                    }
                    else if (((object)Reader.LocalName == (object)_id63_ValidateSet && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        if (!ReadNull())
                        {
                            global::System.String[] a_8_0 = null;
                            int ca_8_0 = 0;
                            if ((Reader.IsEmptyElement))
                            {
                                Reader.Skip();
                            }
                            else
                            {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                int whileIterations6 = 0;
                                int readerCount6 = ReaderCount;
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                                {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                    {
                                        if (((object)Reader.LocalName == (object)_id64_AllowedValue && (object)Reader.NamespaceURI == (object)_id2_Item))
                                        {
                                            {
                                                a_8_0 = (global::System.String[])EnsureArrayIndex(a_8_0, ca_8_0, typeof(global::System.String)); a_8_0[ca_8_0++] = Reader.ReadElementContentAsString();
                                            }
                                        }
                                        else
                                        {
                                            UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowedValue");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowedValue");
                                    }

                                    Reader.MoveToContent();
                                    CheckReaderCount(ref whileIterations6, ref readerCount6);
                                }

                                ReadEndElement();
                            }

                            o.@ValidateSet = (global::System.String[])ShrinkArray(a_8_0, ca_8_0, typeof(global::System.String), false);
                        }
                    }
                    else if (!paramsRead[9] && ((object)Reader.LocalName == (object)_id65_Obsolete && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Obsolete = Read7_ObsoleteAttributeMetadata(false, true);
                        paramsRead[9] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyCollection, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyString, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNullOrEmpty, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateCount, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateLength, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateRange, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateSet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyCollection, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyString, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNullOrEmpty, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateCount, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateLength, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateRange, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateSet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations5, ref readerCount5);
            }

            o.@Aliases = (global::System.String[])ShrinkArray(a_11, ca_11, typeof(global::System.String), true);
            o.@CmdletParameterSets = (global::System.String[])ShrinkArray(a_16, ca_16, typeof(global::System.String), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.ObsoleteAttributeMetadata Read7_ObsoleteAttributeMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id17_ObsoleteAttributeMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.ObsoleteAttributeMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.ObsoleteAttributeMetadata();
            bool[] paramsRead = new bool[1];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id66_Message && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Message = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Message");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations7 = 0;
            int readerCount7 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    UnknownNode((object)o, string.Empty);
                }
                else
                {
                    UnknownNode((object)o, string.Empty);
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations7, ref readerCount7);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateRange Read6_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id4_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateRange o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateRange();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id67_Min && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Min = CollapseWhitespace(Reader.Value);
                    paramsRead[0] = true;
                }
                else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id68_Max && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Max = CollapseWhitespace(Reader.Value);
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Min, :Max");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations8 = 0;
            int readerCount8 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    UnknownNode((object)o, string.Empty);
                }
                else
                {
                    UnknownNode((object)o, string.Empty);
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations8, ref readerCount8);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateLength Read5_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id4_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateLength o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateLength();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id67_Min && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Min = CollapseWhitespace(Reader.Value);
                    paramsRead[0] = true;
                }
                else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id68_Max && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Max = CollapseWhitespace(Reader.Value);
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Min, :Max");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations9 = 0;
            int readerCount9 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    UnknownNode((object)o, string.Empty);
                }
                else
                {
                    UnknownNode((object)o, string.Empty);
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations9, ref readerCount9);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateCount Read4_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id4_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateCount o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateCount();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id67_Min && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Min = CollapseWhitespace(Reader.Value);
                    paramsRead[0] = true;
                }
                else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id68_Max && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Max = CollapseWhitespace(Reader.Value);
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Min, :Max");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations10 = 0;
            int readerCount10 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    UnknownNode((object)o, string.Empty);
                }
                else
                {
                    UnknownNode((object)o, string.Empty);
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations10, ref readerCount10);
            }

            ReadEndElement();
            return o;
        }

        global::System.Object Read1_Object(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (isNull)
                {
                    if (xsiType != null) return (global::System.Object)ReadTypedNull(xsiType);
                    else return null;
                }

                if (xsiType == null)
                {
                    return ReadTypedPrimitive(new System.Xml.XmlQualifiedName("anyType", "http://www.w3.org/2001/XMLSchema"));
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id40_EnumMetadataEnumValue && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read49_EnumMetadataEnumValue(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id39_EnumMetadataEnum && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read48_EnumMetadataEnum(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id38_ClassMetadataData && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read47_ClassMetadataData(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id32_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read46_Item(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id16_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read44_Item(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id15_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read43_Item(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id14_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read42_Item(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id10_AssociationAssociatedInstance && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read41_AssociationAssociatedInstance(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id5_ClassMetadataInstanceCmdlets && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read40_ClassMetadataInstanceCmdlets(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id3_ClassMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read36_ClassMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id24_StaticCmdletMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read34_StaticCmdletMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id34_InstanceCmdletMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read31_InstanceCmdletMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id28_CommonMethodParameterMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read26_CommonMethodParameterMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id29_StaticMethodParameterMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read27_StaticMethodParameterMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id31_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read25_Item(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id26_CommonMethodMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read29_CommonMethodMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id33_InstanceMethodMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read30_InstanceMethodMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id27_StaticMethodMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read28_StaticMethodMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id30_CmdletOutputMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read23_CmdletOutputMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id21_GetCmdletMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read22_GetCmdletMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id22_CommonCmdletMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read21_CommonCmdletMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id25_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read45_Item(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id6_GetCmdletParameters && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read19_GetCmdletParameters(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id20_QueryOption && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read18_QueryOption(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id9_Association && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read17_Association(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id7_PropertyMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read15_PropertyMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id35_PropertyQuery && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read14_PropertyQuery(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id36_WildcardablePropertyQuery && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read13_WildcardablePropertyQuery(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id11_CmdletParameterMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read10_CmdletParameterMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id12_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read11_Item(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id13_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read12_Item(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id18_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read9_Item(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id19_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read8_Item(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id17_ObsoleteAttributeMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read7_ObsoleteAttributeMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id8_TypeMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read2_TypeMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id37_ItemsChoiceType && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                    Reader.ReadStartElement();
                    object e = Read3_ItemsChoiceType(CollapseWhitespace(Reader.ReadElementContentAsString()));
                    ReadEndElement();
                    return e;
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id69_ArrayOfString && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                    global::System.String[] a = null;
                    if (!ReadNull())
                    {
                        global::System.String[] z_0_0 = null;
                        int cz_0_0 = 0;
                        if ((Reader.IsEmptyElement))
                        {
                            Reader.Skip();
                        }
                        else
                        {
                            Reader.ReadStartElement();
                            Reader.MoveToContent();
                            int whileIterations11 = 0;
                            int readerCount11 = ReaderCount;
                            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                            {
                                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                {
                                    if (((object)Reader.LocalName == (object)_id64_AllowedValue && (object)Reader.NamespaceURI == (object)_id2_Item))
                                    {
                                        {
                                            z_0_0 = (global::System.String[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::System.String)); z_0_0[cz_0_0++] = Reader.ReadElementContentAsString();
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowedValue");
                                    }
                                }
                                else
                                {
                                    UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowedValue");
                                }

                                Reader.MoveToContent();
                                CheckReaderCount(ref whileIterations11, ref readerCount11);
                            }

                            ReadEndElement();
                        }

                        a = (global::System.String[])ShrinkArray(z_0_0, cz_0_0, typeof(global::System.String), false);
                    }

                    return a;
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id70_ArrayOfPropertyMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                    global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata[] a = null;
                    if (!ReadNull())
                    {
                        global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata[] z_0_0 = null;
                        int cz_0_0 = 0;
                        if ((Reader.IsEmptyElement))
                        {
                            Reader.Skip();
                        }
                        else
                        {
                            Reader.ReadStartElement();
                            Reader.MoveToContent();
                            int whileIterations12 = 0;
                            int readerCount12 = ReaderCount;
                            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                            {
                                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                {
                                    if (((object)Reader.LocalName == (object)_id71_Property && (object)Reader.NamespaceURI == (object)_id2_Item))
                                    {
                                        z_0_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata)); z_0_0[cz_0_0++] = Read15_PropertyMetadata(false, true);
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Property");
                                    }
                                }
                                else
                                {
                                    UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Property");
                                }

                                Reader.MoveToContent();
                                CheckReaderCount(ref whileIterations12, ref readerCount12);
                            }

                            ReadEndElement();
                        }

                        a = (global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata[])ShrinkArray(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata), false);
                    }

                    return a;
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id72_ArrayOfAssociation && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                    global::Microsoft.PowerShell.Cmdletization.Xml.Association[] a = null;
                    if (!ReadNull())
                    {
                        global::Microsoft.PowerShell.Cmdletization.Xml.Association[] z_0_0 = null;
                        int cz_0_0 = 0;
                        if ((Reader.IsEmptyElement))
                        {
                            Reader.Skip();
                        }
                        else
                        {
                            Reader.ReadStartElement();
                            Reader.MoveToContent();
                            int whileIterations13 = 0;
                            int readerCount13 = ReaderCount;
                            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                            {
                                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                {
                                    if (((object)Reader.LocalName == (object)_id9_Association && (object)Reader.NamespaceURI == (object)_id2_Item))
                                    {
                                        z_0_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.Association[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.Association)); z_0_0[cz_0_0++] = Read17_Association(false, true);
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Association");
                                    }
                                }
                                else
                                {
                                    UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Association");
                                }

                                Reader.MoveToContent();
                                CheckReaderCount(ref whileIterations13, ref readerCount13);
                            }

                            ReadEndElement();
                        }

                        a = (global::Microsoft.PowerShell.Cmdletization.Xml.Association[])ShrinkArray(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.Association), false);
                    }

                    return a;
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id73_ArrayOfQueryOption && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                    global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption[] a = null;
                    if (!ReadNull())
                    {
                        global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption[] z_0_0 = null;
                        int cz_0_0 = 0;
                        if ((Reader.IsEmptyElement))
                        {
                            Reader.Skip();
                        }
                        else
                        {
                            Reader.ReadStartElement();
                            Reader.MoveToContent();
                            int whileIterations14 = 0;
                            int readerCount14 = ReaderCount;
                            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                            {
                                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                {
                                    if (((object)Reader.LocalName == (object)_id74_Option && (object)Reader.NamespaceURI == (object)_id2_Item))
                                    {
                                        z_0_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption)); z_0_0[cz_0_0++] = Read18_QueryOption(false, true);
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Option");
                                    }
                                }
                                else
                                {
                                    UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Option");
                                }

                                Reader.MoveToContent();
                                CheckReaderCount(ref whileIterations14, ref readerCount14);
                            }

                            ReadEndElement();
                        }

                        a = (global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption[])ShrinkArray(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption), false);
                    }

                    return a;
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id23_ConfirmImpact && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                    Reader.ReadStartElement();
                    object e = Read20_ConfirmImpact(CollapseWhitespace(Reader.ReadElementContentAsString()));
                    ReadEndElement();
                    return e;
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id75_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                    global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata[] a = null;
                    if (!ReadNull())
                    {
                        global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata[] z_0_0 = null;
                        int cz_0_0 = 0;
                        if ((Reader.IsEmptyElement))
                        {
                            Reader.Skip();
                        }
                        else
                        {
                            Reader.ReadStartElement();
                            Reader.MoveToContent();
                            int whileIterations15 = 0;
                            int readerCount15 = ReaderCount;
                            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                            {
                                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                {
                                    if (((object)Reader.LocalName == (object)_id76_Parameter && (object)Reader.NamespaceURI == (object)_id2_Item))
                                    {
                                        z_0_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata)); z_0_0[cz_0_0++] = Read27_StaticMethodParameterMetadata(false, true);
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Parameter");
                                    }
                                }
                                else
                                {
                                    UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Parameter");
                                }

                                Reader.MoveToContent();
                                CheckReaderCount(ref whileIterations15, ref readerCount15);
                            }

                            ReadEndElement();
                        }

                        a = (global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata[])ShrinkArray(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata), false);
                    }

                    return a;
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id77_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                    global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata[] a = null;
                    if (!ReadNull())
                    {
                        global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata[] z_0_0 = null;
                        int cz_0_0 = 0;
                        if ((Reader.IsEmptyElement))
                        {
                            Reader.Skip();
                        }
                        else
                        {
                            Reader.ReadStartElement();
                            Reader.MoveToContent();
                            int whileIterations16 = 0;
                            int readerCount16 = ReaderCount;
                            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                            {
                                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                {
                                    if (((object)Reader.LocalName == (object)_id76_Parameter && (object)Reader.NamespaceURI == (object)_id2_Item))
                                    {
                                        z_0_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata)); z_0_0[cz_0_0++] = Read25_Item(false, true);
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Parameter");
                                    }
                                }
                                else
                                {
                                    UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Parameter");
                                }

                                Reader.MoveToContent();
                                CheckReaderCount(ref whileIterations16, ref readerCount16);
                            }

                            ReadEndElement();
                        }

                        a = (global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata[])ShrinkArray(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata), false);
                    }

                    return a;
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id78_ArrayOfStaticCmdletMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                    global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata[] a = null;
                    if (!ReadNull())
                    {
                        global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata[] z_0_0 = null;
                        int cz_0_0 = 0;
                        if ((Reader.IsEmptyElement))
                        {
                            Reader.Skip();
                        }
                        else
                        {
                            Reader.ReadStartElement();
                            Reader.MoveToContent();
                            int whileIterations17 = 0;
                            int readerCount17 = ReaderCount;
                            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                            {
                                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                {
                                    if (((object)Reader.LocalName == (object)_id79_Cmdlet && (object)Reader.NamespaceURI == (object)_id2_Item))
                                    {
                                        z_0_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata)); z_0_0[cz_0_0++] = Read34_StaticCmdletMetadata(false, true);
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Cmdlet");
                                    }
                                }
                                else
                                {
                                    UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Cmdlet");
                                }

                                Reader.MoveToContent();
                                CheckReaderCount(ref whileIterations17, ref readerCount17);
                            }

                            ReadEndElement();
                        }

                        a = (global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata[])ShrinkArray(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata), false);
                    }

                    return a;
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id80_ArrayOfClassMetadataData && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                    global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData[] a = null;
                    if (!ReadNull())
                    {
                        global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData[] z_0_0 = null;
                        int cz_0_0 = 0;
                        if ((Reader.IsEmptyElement))
                        {
                            Reader.Skip();
                        }
                        else
                        {
                            Reader.ReadStartElement();
                            Reader.MoveToContent();
                            int whileIterations18 = 0;
                            int readerCount18 = ReaderCount;
                            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                            {
                                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                {
                                    if (((object)Reader.LocalName == (object)_id81_Data && (object)Reader.NamespaceURI == (object)_id2_Item))
                                    {
                                        z_0_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData)); z_0_0[cz_0_0++] = Read35_ClassMetadataData(false, true);
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Data");
                                    }
                                }
                                else
                                {
                                    UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Data");
                                }

                                Reader.MoveToContent();
                                CheckReaderCount(ref whileIterations18, ref readerCount18);
                            }

                            ReadEndElement();
                        }

                        a = (global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData[])ShrinkArray(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData), false);
                    }

                    return a;
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id82_ArrayOfEnumMetadataEnum && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                    global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum[] a = null;
                    if (!ReadNull())
                    {
                        global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum[] z_0_0 = null;
                        int cz_0_0 = 0;
                        if ((Reader.IsEmptyElement))
                        {
                            Reader.Skip();
                        }
                        else
                        {
                            Reader.ReadStartElement();
                            Reader.MoveToContent();
                            int whileIterations19 = 0;
                            int readerCount19 = ReaderCount;
                            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                            {
                                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                {
                                    if (((object)Reader.LocalName == (object)_id83_Enum && (object)Reader.NamespaceURI == (object)_id2_Item))
                                    {
                                        z_0_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum)); z_0_0[cz_0_0++] = Read38_EnumMetadataEnum(false, true);
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Enum");
                                    }
                                }
                                else
                                {
                                    UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Enum");
                                }

                                Reader.MoveToContent();
                                CheckReaderCount(ref whileIterations19, ref readerCount19);
                            }

                            ReadEndElement();
                        }

                        a = (global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum[])ShrinkArray(z_0_0, cz_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum), false);
                    }

                    return a;
                }
                else
                    return ReadTypedPrimitive((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::System.Object o;
            o = new global::System.Object();
            bool[] paramsRead = Array.Empty<bool>();
            while (Reader.MoveToNextAttribute())
            {
                if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o);
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations20 = 0;
            int readerCount20 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    UnknownNode((object)o, string.Empty);
                }
                else
                {
                    UnknownNode((object)o, string.Empty);
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations20, ref readerCount20);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum Read38_EnumMetadataEnum(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id4_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum();
            global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue[] a_0 = null;
            int ca_0 = 0;
            bool[] paramsRead = new bool[4];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id43_EnumName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@EnumName = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id44_UnderlyingType && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@UnderlyingType = Reader.Value;
                    paramsRead[2] = true;
                }
                else if (!paramsRead[3] && ((object)Reader.LocalName == (object)_id45_BitwiseFlags && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@BitwiseFlags = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@BitwiseFlagsSpecified = true;
                    paramsRead[3] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":EnumName, :UnderlyingType, :BitwiseFlags");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Value = (global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue[])ShrinkArray(a_0, ca_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations21 = 0;
            int readerCount21 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (((object)Reader.LocalName == (object)_id42_Value && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        a_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue[])EnsureArrayIndex(a_0, ca_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue)); a_0[ca_0++] = Read37_EnumMetadataEnumValue(false, true);
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Value");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Value");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations21, ref readerCount21);
            }

            o.@Value = (global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue[])ShrinkArray(a_0, ca_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnumValue), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData Read35_ClassMetadataData(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id4_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id41_Name && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Name = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Name");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations22 = 0;
            int readerCount22 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                string tmp = null;
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    UnknownNode((object)o, string.Empty);
                }
                else if (Reader.NodeType == System.Xml.XmlNodeType.Text ||
                Reader.NodeType == System.Xml.XmlNodeType.CDATA ||
                Reader.NodeType == System.Xml.XmlNodeType.Whitespace ||
                Reader.NodeType == System.Xml.XmlNodeType.SignificantWhitespace)
                {
                    tmp = ReadString(tmp, false);
                    o.@Value = tmp;
                }
                else
                {
                    UnknownNode((object)o, string.Empty);
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations22, ref readerCount22);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata Read34_StaticCmdletMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id24_StaticCmdletMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata();
            global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodMetadata[] a_1 = null;
            int ca_1 = 0;
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o);
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Method = (global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodMetadata[])ShrinkArray(a_1, ca_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodMetadata), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations23 = 0;
            int readerCount23 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id84_CmdletMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletMetadata = Read33_Item(false, true);
                        paramsRead[0] = true;
                    }
                    else if (((object)Reader.LocalName == (object)_id85_Method && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        a_1 = (global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodMetadata[])EnsureArrayIndex(a_1, ca_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodMetadata)); a_1[ca_1++] = Read28_StaticMethodMetadata(false, true);
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletMetadata, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Method");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletMetadata, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Method");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations23, ref readerCount23);
            }

            o.@Method = (global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodMetadata[])ShrinkArray(a_1, ca_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodMetadata), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodMetadata Read28_StaticMethodMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id27_StaticMethodMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodMetadata();
            global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata[] a_2 = null;
            int ca_2 = 0;
            bool[] paramsRead = new bool[4];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id86_MethodName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@MethodName = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!paramsRead[3] && ((object)Reader.LocalName == (object)_id87_CmdletParameterSet && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@CmdletParameterSet = Reader.Value;
                    paramsRead[3] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":MethodName, :CmdletParameterSet");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations24 = 0;
            int readerCount24 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id88_ReturnValue && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ReturnValue = Read24_Item(false, true);
                        paramsRead[0] = true;
                    }
                    else if (((object)Reader.LocalName == (object)_id89_Parameters && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        if (!ReadNull())
                        {
                            global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata[] a_2_0 = null;
                            int ca_2_0 = 0;
                            if ((Reader.IsEmptyElement))
                            {
                                Reader.Skip();
                            }
                            else
                            {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                int whileIterations25 = 0;
                                int readerCount25 = ReaderCount;
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                                {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                    {
                                        if (((object)Reader.LocalName == (object)_id76_Parameter && (object)Reader.NamespaceURI == (object)_id2_Item))
                                        {
                                            a_2_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata[])EnsureArrayIndex(a_2_0, ca_2_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata)); a_2_0[ca_2_0++] = Read27_StaticMethodParameterMetadata(false, true);
                                        }
                                        else
                                        {
                                            UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Parameter");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Parameter");
                                    }

                                    Reader.MoveToContent();
                                    CheckReaderCount(ref whileIterations25, ref readerCount25);
                                }

                                ReadEndElement();
                            }

                            o.@Parameters = (global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata[])ShrinkArray(a_2_0, ca_2_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata), false);
                        }
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ReturnValue, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Parameters");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ReturnValue, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Parameters");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations24, ref readerCount24);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata Read27_StaticMethodParameterMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id29_StaticMethodParameterMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.StaticMethodParameterMetadata();
            bool[] paramsRead = new bool[5];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id90_ParameterName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ParameterName = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id91_DefaultValue && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@DefaultValue = Reader.Value;
                    paramsRead[2] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":ParameterName, :DefaultValue");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations26 = 0;
            int readerCount26 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id92_Type && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Type = Read2_TypeMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[3] && ((object)Reader.LocalName == (object)_id11_CmdletParameterMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletParameterMetadata = Read8_Item(false, true);
                        paramsRead[3] = true;
                    }
                    else if (!paramsRead[4] && ((object)Reader.LocalName == (object)_id30_CmdletOutputMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletOutputMetadata = Read23_CmdletOutputMetadata(false, true);
                        paramsRead[4] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletOutputMetadata");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletOutputMetadata");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations26, ref readerCount26);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CmdletOutputMetadata Read23_CmdletOutputMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id30_CmdletOutputMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CmdletOutputMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CmdletOutputMetadata();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id49_PSName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@PSName = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":PSName");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations27 = 0;
            int readerCount27 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id93_ErrorCode && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ErrorCode = Read1_Object(false, true);
                        paramsRead[0] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ErrorCode");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ErrorCode");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations27, ref readerCount27);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataForStaticMethodParameter Read8_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id19_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataForStaticMethodParameter o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataForStaticMethodParameter();
            global::System.String[] a_8 = null;
            int ca_8 = 0;
            global::System.String[] a_11 = null;
            int ca_11 = 0;
            bool[] paramsRead = new bool[16];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[10] && ((object)Reader.LocalName == (object)_id47_IsMandatory && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@IsMandatory = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@IsMandatorySpecified = true;
                    paramsRead[10] = true;
                }
                else if (((object)Reader.LocalName == (object)_id48_Aliases && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    string listValues = Reader.Value;
                    string[] vals = listValues.Split(null);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        a_11 = (global::System.String[])EnsureArrayIndex(a_11, ca_11, typeof(global::System.String)); a_11[ca_11++] = vals[i];
                    }
                }
                else if (!paramsRead[12] && ((object)Reader.LocalName == (object)_id49_PSName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@PSName = Reader.Value;
                    paramsRead[12] = true;
                }
                else if (!paramsRead[13] && ((object)Reader.LocalName == (object)_id50_Position && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Position = CollapseWhitespace(Reader.Value);
                    paramsRead[13] = true;
                }
                else if (!paramsRead[14] && ((object)Reader.LocalName == (object)_id51_ValueFromPipeline && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ValueFromPipeline = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@ValueFromPipelineSpecified = true;
                    paramsRead[14] = true;
                }
                else if (!paramsRead[15] && ((object)Reader.LocalName == (object)_id52_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ValueFromPipelineByPropertyName = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@ValueFromPipelineByPropertyNameSpecified = true;
                    paramsRead[15] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":IsMandatory, :Aliases, :PSName, :Position, :ValueFromPipeline, :ValueFromPipelineByPropertyName");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Aliases = (global::System.String[])ShrinkArray(a_11, ca_11, typeof(global::System.String), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations28 = 0;
            int readerCount28 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id55_AllowEmptyCollection && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowEmptyCollection = Read1_Object(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id56_AllowEmptyString && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowEmptyString = Read1_Object(false, true);
                        paramsRead[1] = true;
                    }
                    else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id57_AllowNull && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowNull = Read1_Object(false, true);
                        paramsRead[2] = true;
                    }
                    else if (!paramsRead[3] && ((object)Reader.LocalName == (object)_id58_ValidateNotNull && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateNotNull = Read1_Object(false, true);
                        paramsRead[3] = true;
                    }
                    else if (!paramsRead[4] && ((object)Reader.LocalName == (object)_id59_ValidateNotNullOrEmpty && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateNotNullOrEmpty = Read1_Object(false, true);
                        paramsRead[4] = true;
                    }
                    else if (!paramsRead[5] && ((object)Reader.LocalName == (object)_id60_ValidateCount && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateCount = Read4_Item(false, true);
                        paramsRead[5] = true;
                    }
                    else if (!paramsRead[6] && ((object)Reader.LocalName == (object)_id61_ValidateLength && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateLength = Read5_Item(false, true);
                        paramsRead[6] = true;
                    }
                    else if (!paramsRead[7] && ((object)Reader.LocalName == (object)_id62_ValidateRange && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateRange = Read6_Item(false, true);
                        paramsRead[7] = true;
                    }
                    else if (((object)Reader.LocalName == (object)_id63_ValidateSet && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        if (!ReadNull())
                        {
                            global::System.String[] a_8_0 = null;
                            int ca_8_0 = 0;
                            if ((Reader.IsEmptyElement))
                            {
                                Reader.Skip();
                            }
                            else
                            {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                int whileIterations29 = 0;
                                int readerCount29 = ReaderCount;
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                                {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                    {
                                        if (((object)Reader.LocalName == (object)_id64_AllowedValue && (object)Reader.NamespaceURI == (object)_id2_Item))
                                        {
                                            {
                                                a_8_0 = (global::System.String[])EnsureArrayIndex(a_8_0, ca_8_0, typeof(global::System.String)); a_8_0[ca_8_0++] = Reader.ReadElementContentAsString();
                                            }
                                        }
                                        else
                                        {
                                            UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowedValue");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowedValue");
                                    }

                                    Reader.MoveToContent();
                                    CheckReaderCount(ref whileIterations29, ref readerCount29);
                                }

                                ReadEndElement();
                            }

                            o.@ValidateSet = (global::System.String[])ShrinkArray(a_8_0, ca_8_0, typeof(global::System.String), false);
                        }
                    }
                    else if (!paramsRead[9] && ((object)Reader.LocalName == (object)_id65_Obsolete && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Obsolete = Read7_ObsoleteAttributeMetadata(false, true);
                        paramsRead[9] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyCollection, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyString, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNullOrEmpty, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateCount, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateLength, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateRange, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateSet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyCollection, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyString, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNullOrEmpty, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateCount, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateLength, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateRange, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateSet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations28, ref readerCount28);
            }

            o.@Aliases = (global::System.String[])ShrinkArray(a_11, ca_11, typeof(global::System.String), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.TypeMetadata Read2_TypeMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id8_TypeMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.TypeMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.TypeMetadata();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id94_PSType && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@PSType = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id95_ETSType && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ETSType = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":PSType, :ETSType");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations30 = 0;
            int readerCount30 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    UnknownNode((object)o, string.Empty);
                }
                else
                {
                    UnknownNode((object)o, string.Empty);
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations30, ref readerCount30);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CommonMethodMetadataReturnValue Read24_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id4_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CommonMethodMetadataReturnValue o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CommonMethodMetadataReturnValue();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o);
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations31 = 0;
            int readerCount31 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id92_Type && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Type = Read2_TypeMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id30_CmdletOutputMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletOutputMetadata = Read23_CmdletOutputMetadata(false, true);
                        paramsRead[1] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletOutputMetadata");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletOutputMetadata");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations31, ref readerCount31);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadataCmdletMetadata Read33_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id4_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadataCmdletMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadataCmdletMetadata();
            global::System.String[] a_3 = null;
            int ca_3 = 0;
            bool[] paramsRead = new bool[7];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id96_Verb && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Verb = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id97_Noun && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Noun = Reader.Value;
                    paramsRead[2] = true;
                }
                else if (((object)Reader.LocalName == (object)_id48_Aliases && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    string listValues = Reader.Value;
                    string[] vals = listValues.Split(null);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        a_3 = (global::System.String[])EnsureArrayIndex(a_3, ca_3, typeof(global::System.String)); a_3[ca_3++] = vals[i];
                    }
                }
                else if (!paramsRead[4] && ((object)Reader.LocalName == (object)_id23_ConfirmImpact && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ConfirmImpact = Read20_ConfirmImpact(Reader.Value);
                    o.@ConfirmImpactSpecified = true;
                    paramsRead[4] = true;
                }
                else if (!paramsRead[5] && ((object)Reader.LocalName == (object)_id98_HelpUri && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@HelpUri = CollapseWhitespace(Reader.Value);
                    paramsRead[5] = true;
                }
                else if (!paramsRead[6] && ((object)Reader.LocalName == (object)_id99_DefaultCmdletParameterSet && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@DefaultCmdletParameterSet = Reader.Value;
                    paramsRead[6] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Verb, :Noun, :Aliases, :ConfirmImpact, :HelpUri, :DefaultCmdletParameterSet");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Aliases = (global::System.String[])ShrinkArray(a_3, ca_3, typeof(global::System.String), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations32 = 0;
            int readerCount32 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id65_Obsolete && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Obsolete = Read7_ObsoleteAttributeMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations32, ref readerCount32);
            }

            o.@Aliases = (global::System.String[])ShrinkArray(a_3, ca_3, typeof(global::System.String), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.ConfirmImpact Read20_ConfirmImpact(string s)
        {
            switch (s)
            {
                case @"None": return global::Microsoft.PowerShell.Cmdletization.Xml.ConfirmImpact.@None;
                case @"Low": return global::Microsoft.PowerShell.Cmdletization.Xml.ConfirmImpact.@Low;
                case @"Medium": return global::Microsoft.PowerShell.Cmdletization.Xml.ConfirmImpact.@Medium;
                case @"High": return global::Microsoft.PowerShell.Cmdletization.Xml.ConfirmImpact.@High;
                default: throw CreateUnknownConstantException(s, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.ConfirmImpact));
            }
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata Read25_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id31_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata();
            bool[] paramsRead = new bool[5];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id90_ParameterName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ParameterName = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id91_DefaultValue && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@DefaultValue = Reader.Value;
                    paramsRead[2] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":ParameterName, :DefaultValue");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations33 = 0;
            int readerCount33 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id92_Type && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Type = Read2_TypeMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[3] && ((object)Reader.LocalName == (object)_id11_CmdletParameterMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletParameterMetadata = Read9_Item(false, true);
                        paramsRead[3] = true;
                    }
                    else if (!paramsRead[4] && ((object)Reader.LocalName == (object)_id30_CmdletOutputMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletOutputMetadata = Read23_CmdletOutputMetadata(false, true);
                        paramsRead[4] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletOutputMetadata");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletOutputMetadata");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations33, ref readerCount33);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataForInstanceMethodParameter Read9_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id18_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataForInstanceMethodParameter o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataForInstanceMethodParameter();
            global::System.String[] a_8 = null;
            int ca_8 = 0;
            global::System.String[] a_11 = null;
            int ca_11 = 0;
            bool[] paramsRead = new bool[15];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[10] && ((object)Reader.LocalName == (object)_id47_IsMandatory && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@IsMandatory = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@IsMandatorySpecified = true;
                    paramsRead[10] = true;
                }
                else if (((object)Reader.LocalName == (object)_id48_Aliases && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    string listValues = Reader.Value;
                    string[] vals = listValues.Split(null);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        a_11 = (global::System.String[])EnsureArrayIndex(a_11, ca_11, typeof(global::System.String)); a_11[ca_11++] = vals[i];
                    }
                }
                else if (!paramsRead[12] && ((object)Reader.LocalName == (object)_id49_PSName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@PSName = Reader.Value;
                    paramsRead[12] = true;
                }
                else if (!paramsRead[13] && ((object)Reader.LocalName == (object)_id50_Position && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Position = CollapseWhitespace(Reader.Value);
                    paramsRead[13] = true;
                }
                else if (!paramsRead[14] && ((object)Reader.LocalName == (object)_id52_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ValueFromPipelineByPropertyName = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@ValueFromPipelineByPropertyNameSpecified = true;
                    paramsRead[14] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":IsMandatory, :Aliases, :PSName, :Position, :ValueFromPipelineByPropertyName");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Aliases = (global::System.String[])ShrinkArray(a_11, ca_11, typeof(global::System.String), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations34 = 0;
            int readerCount34 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id55_AllowEmptyCollection && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowEmptyCollection = Read1_Object(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id56_AllowEmptyString && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowEmptyString = Read1_Object(false, true);
                        paramsRead[1] = true;
                    }
                    else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id57_AllowNull && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowNull = Read1_Object(false, true);
                        paramsRead[2] = true;
                    }
                    else if (!paramsRead[3] && ((object)Reader.LocalName == (object)_id58_ValidateNotNull && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateNotNull = Read1_Object(false, true);
                        paramsRead[3] = true;
                    }
                    else if (!paramsRead[4] && ((object)Reader.LocalName == (object)_id59_ValidateNotNullOrEmpty && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateNotNullOrEmpty = Read1_Object(false, true);
                        paramsRead[4] = true;
                    }
                    else if (!paramsRead[5] && ((object)Reader.LocalName == (object)_id60_ValidateCount && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateCount = Read4_Item(false, true);
                        paramsRead[5] = true;
                    }
                    else if (!paramsRead[6] && ((object)Reader.LocalName == (object)_id61_ValidateLength && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateLength = Read5_Item(false, true);
                        paramsRead[6] = true;
                    }
                    else if (!paramsRead[7] && ((object)Reader.LocalName == (object)_id62_ValidateRange && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateRange = Read6_Item(false, true);
                        paramsRead[7] = true;
                    }
                    else if (((object)Reader.LocalName == (object)_id63_ValidateSet && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        if (!ReadNull())
                        {
                            global::System.String[] a_8_0 = null;
                            int ca_8_0 = 0;
                            if ((Reader.IsEmptyElement))
                            {
                                Reader.Skip();
                            }
                            else
                            {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                int whileIterations35 = 0;
                                int readerCount35 = ReaderCount;
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                                {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                    {
                                        if (((object)Reader.LocalName == (object)_id64_AllowedValue && (object)Reader.NamespaceURI == (object)_id2_Item))
                                        {
                                            {
                                                a_8_0 = (global::System.String[])EnsureArrayIndex(a_8_0, ca_8_0, typeof(global::System.String)); a_8_0[ca_8_0++] = Reader.ReadElementContentAsString();
                                            }
                                        }
                                        else
                                        {
                                            UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowedValue");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowedValue");
                                    }

                                    Reader.MoveToContent();
                                    CheckReaderCount(ref whileIterations35, ref readerCount35);
                                }

                                ReadEndElement();
                            }

                            o.@ValidateSet = (global::System.String[])ShrinkArray(a_8_0, ca_8_0, typeof(global::System.String), false);
                        }
                    }
                    else if (!paramsRead[9] && ((object)Reader.LocalName == (object)_id65_Obsolete && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Obsolete = Read7_ObsoleteAttributeMetadata(false, true);
                        paramsRead[9] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyCollection, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyString, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNullOrEmpty, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateCount, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateLength, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateRange, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateSet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyCollection, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyString, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNullOrEmpty, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateCount, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateLength, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateRange, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateSet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations34, ref readerCount34);
            }

            o.@Aliases = (global::System.String[])ShrinkArray(a_11, ca_11, typeof(global::System.String), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption Read18_QueryOption(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id20_QueryOption && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption();
            bool[] paramsRead = new bool[3];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id100_OptionName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@OptionName = Reader.Value;
                    paramsRead[2] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":OptionName");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations36 = 0;
            int readerCount36 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id92_Type && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Type = Read2_TypeMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id11_CmdletParameterMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletParameterMetadata = Read11_Item(false, true);
                        paramsRead[1] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations36, ref readerCount36);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataForGetCmdletParameter Read11_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id12_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id13_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read12_Item(isNullable, false);
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataForGetCmdletParameter o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataForGetCmdletParameter();
            global::System.String[] a_8 = null;
            int ca_8 = 0;
            global::System.String[] a_11 = null;
            int ca_11 = 0;
            global::System.String[] a_16 = null;
            int ca_16 = 0;
            bool[] paramsRead = new bool[17];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[10] && ((object)Reader.LocalName == (object)_id47_IsMandatory && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@IsMandatory = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@IsMandatorySpecified = true;
                    paramsRead[10] = true;
                }
                else if (((object)Reader.LocalName == (object)_id48_Aliases && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    string listValues = Reader.Value;
                    string[] vals = listValues.Split(null);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        a_11 = (global::System.String[])EnsureArrayIndex(a_11, ca_11, typeof(global::System.String)); a_11[ca_11++] = vals[i];
                    }
                }
                else if (!paramsRead[12] && ((object)Reader.LocalName == (object)_id49_PSName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@PSName = Reader.Value;
                    paramsRead[12] = true;
                }
                else if (!paramsRead[13] && ((object)Reader.LocalName == (object)_id50_Position && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Position = CollapseWhitespace(Reader.Value);
                    paramsRead[13] = true;
                }
                else if (!paramsRead[14] && ((object)Reader.LocalName == (object)_id51_ValueFromPipeline && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ValueFromPipeline = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@ValueFromPipelineSpecified = true;
                    paramsRead[14] = true;
                }
                else if (!paramsRead[15] && ((object)Reader.LocalName == (object)_id52_Item && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ValueFromPipelineByPropertyName = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@ValueFromPipelineByPropertyNameSpecified = true;
                    paramsRead[15] = true;
                }
                else if (((object)Reader.LocalName == (object)_id53_CmdletParameterSets && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    string listValues = Reader.Value;
                    string[] vals = listValues.Split(null);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        a_16 = (global::System.String[])EnsureArrayIndex(a_16, ca_16, typeof(global::System.String)); a_16[ca_16++] = vals[i];
                    }
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":IsMandatory, :Aliases, :PSName, :Position, :ValueFromPipeline, :ValueFromPipelineByPropertyName, :CmdletParameterSets");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Aliases = (global::System.String[])ShrinkArray(a_11, ca_11, typeof(global::System.String), true);
                o.@CmdletParameterSets = (global::System.String[])ShrinkArray(a_16, ca_16, typeof(global::System.String), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations37 = 0;
            int readerCount37 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id55_AllowEmptyCollection && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowEmptyCollection = Read1_Object(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id56_AllowEmptyString && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowEmptyString = Read1_Object(false, true);
                        paramsRead[1] = true;
                    }
                    else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id57_AllowNull && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowNull = Read1_Object(false, true);
                        paramsRead[2] = true;
                    }
                    else if (!paramsRead[3] && ((object)Reader.LocalName == (object)_id58_ValidateNotNull && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateNotNull = Read1_Object(false, true);
                        paramsRead[3] = true;
                    }
                    else if (!paramsRead[4] && ((object)Reader.LocalName == (object)_id59_ValidateNotNullOrEmpty && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateNotNullOrEmpty = Read1_Object(false, true);
                        paramsRead[4] = true;
                    }
                    else if (!paramsRead[5] && ((object)Reader.LocalName == (object)_id60_ValidateCount && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateCount = Read4_Item(false, true);
                        paramsRead[5] = true;
                    }
                    else if (!paramsRead[6] && ((object)Reader.LocalName == (object)_id61_ValidateLength && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateLength = Read5_Item(false, true);
                        paramsRead[6] = true;
                    }
                    else if (!paramsRead[7] && ((object)Reader.LocalName == (object)_id62_ValidateRange && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateRange = Read6_Item(false, true);
                        paramsRead[7] = true;
                    }
                    else if (((object)Reader.LocalName == (object)_id63_ValidateSet && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        if (!ReadNull())
                        {
                            global::System.String[] a_8_0 = null;
                            int ca_8_0 = 0;
                            if ((Reader.IsEmptyElement))
                            {
                                Reader.Skip();
                            }
                            else
                            {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                int whileIterations38 = 0;
                                int readerCount38 = ReaderCount;
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                                {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                    {
                                        if (((object)Reader.LocalName == (object)_id64_AllowedValue && (object)Reader.NamespaceURI == (object)_id2_Item))
                                        {
                                            {
                                                a_8_0 = (global::System.String[])EnsureArrayIndex(a_8_0, ca_8_0, typeof(global::System.String)); a_8_0[ca_8_0++] = Reader.ReadElementContentAsString();
                                            }
                                        }
                                        else
                                        {
                                            UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowedValue");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowedValue");
                                    }

                                    Reader.MoveToContent();
                                    CheckReaderCount(ref whileIterations38, ref readerCount38);
                                }

                                ReadEndElement();
                            }

                            o.@ValidateSet = (global::System.String[])ShrinkArray(a_8_0, ca_8_0, typeof(global::System.String), false);
                        }
                    }
                    else if (!paramsRead[9] && ((object)Reader.LocalName == (object)_id65_Obsolete && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Obsolete = Read7_ObsoleteAttributeMetadata(false, true);
                        paramsRead[9] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyCollection, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyString, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNullOrEmpty, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateCount, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateLength, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateRange, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateSet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyCollection, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyString, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNullOrEmpty, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateCount, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateLength, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateRange, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateSet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations37, ref readerCount37);
            }

            o.@Aliases = (global::System.String[])ShrinkArray(a_11, ca_11, typeof(global::System.String), true);
            o.@CmdletParameterSets = (global::System.String[])ShrinkArray(a_16, ca_16, typeof(global::System.String), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.Association Read17_Association(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id9_Association && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.Association o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.Association();
            bool[] paramsRead = new bool[4];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id9_Association && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Association1 = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id101_SourceRole && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@SourceRole = Reader.Value;
                    paramsRead[2] = true;
                }
                else if (!paramsRead[3] && ((object)Reader.LocalName == (object)_id102_ResultRole && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ResultRole = Reader.Value;
                    paramsRead[3] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Association, :SourceRole, :ResultRole");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations39 = 0;
            int readerCount39 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id103_AssociatedInstance && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AssociatedInstance = Read16_AssociationAssociatedInstance(false, true);
                        paramsRead[0] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AssociatedInstance");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AssociatedInstance");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations39, ref readerCount39);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.AssociationAssociatedInstance Read16_AssociationAssociatedInstance(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id4_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.AssociationAssociatedInstance o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.AssociationAssociatedInstance();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o);
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations40 = 0;
            int readerCount40 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id92_Type && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Type = Read2_TypeMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id11_CmdletParameterMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletParameterMetadata = Read12_Item(false, true);
                        paramsRead[1] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations40, ref readerCount40);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata Read15_PropertyMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id7_PropertyMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata();
            global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery[] a_1 = null;
            int ca_1 = 0;
            global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType[] choice_a_1 = null;
            int cchoice_a_1 = 0;
            bool[] paramsRead = new bool[3];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id104_PropertyName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@PropertyName = Reader.Value;
                    paramsRead[2] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":PropertyName");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Items = (global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery[])ShrinkArray(a_1, ca_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery), true);
                o.@ItemsElementName = (global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType[])ShrinkArray(choice_a_1, cchoice_a_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations41 = 0;
            int readerCount41 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id92_Type && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Type = Read2_TypeMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else if (((object)Reader.LocalName == (object)_id105_MaxValueQuery && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        a_1 = (global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery[])EnsureArrayIndex(a_1, ca_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery)); a_1[ca_1++] = Read14_PropertyQuery(false, true);
                        choice_a_1 = (global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType[])EnsureArrayIndex(choice_a_1, cchoice_a_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType)); choice_a_1[cchoice_a_1++] = global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType.@MaxValueQuery;
                    }
                    else if (((object)Reader.LocalName == (object)_id106_RegularQuery && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        a_1 = (global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery[])EnsureArrayIndex(a_1, ca_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery)); a_1[ca_1++] = Read13_WildcardablePropertyQuery(false, true);
                        choice_a_1 = (global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType[])EnsureArrayIndex(choice_a_1, cchoice_a_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType)); choice_a_1[cchoice_a_1++] = global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType.@RegularQuery;
                    }
                    else if (((object)Reader.LocalName == (object)_id107_ExcludeQuery && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        a_1 = (global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery[])EnsureArrayIndex(a_1, ca_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery)); a_1[ca_1++] = Read13_WildcardablePropertyQuery(false, true);
                        choice_a_1 = (global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType[])EnsureArrayIndex(choice_a_1, cchoice_a_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType)); choice_a_1[cchoice_a_1++] = global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType.@ExcludeQuery;
                    }
                    else if (((object)Reader.LocalName == (object)_id108_MinValueQuery && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        a_1 = (global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery[])EnsureArrayIndex(a_1, ca_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery)); a_1[ca_1++] = Read14_PropertyQuery(false, true);
                        choice_a_1 = (global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType[])EnsureArrayIndex(choice_a_1, cchoice_a_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType)); choice_a_1[cchoice_a_1++] = global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType.@MinValueQuery;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:MaxValueQuery, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:RegularQuery, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ExcludeQuery, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:MinValueQuery");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:MaxValueQuery, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:RegularQuery, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ExcludeQuery, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:MinValueQuery");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations41, ref readerCount41);
            }

            o.@Items = (global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery[])ShrinkArray(a_1, ca_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery), true);
            o.@ItemsElementName = (global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType[])ShrinkArray(choice_a_1, cchoice_a_1, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.ItemsChoiceType), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery Read14_PropertyQuery(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id35_PropertyQuery && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id36_WildcardablePropertyQuery && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read13_WildcardablePropertyQuery(isNullable, false);
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.PropertyQuery();
            bool[] paramsRead = new bool[1];
            while (Reader.MoveToNextAttribute())
            {
                if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o);
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations42 = 0;
            int readerCount42 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id11_CmdletParameterMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletParameterMetadata = Read12_Item(false, true);
                        paramsRead[0] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations42, ref readerCount42);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadata Read10_CmdletParameterMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id11_CmdletParameterMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id12_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read11_Item(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id13_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read12_Item(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id18_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read9_Item(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id19_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read8_Item(isNullable, false);
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadata();
            global::System.String[] a_8 = null;
            int ca_8 = 0;
            global::System.String[] a_11 = null;
            int ca_11 = 0;
            bool[] paramsRead = new bool[14];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[10] && ((object)Reader.LocalName == (object)_id47_IsMandatory && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@IsMandatory = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    o.@IsMandatorySpecified = true;
                    paramsRead[10] = true;
                }
                else if (((object)Reader.LocalName == (object)_id48_Aliases && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    string listValues = Reader.Value;
                    string[] vals = listValues.Split(null);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        a_11 = (global::System.String[])EnsureArrayIndex(a_11, ca_11, typeof(global::System.String)); a_11[ca_11++] = vals[i];
                    }
                }
                else if (!paramsRead[12] && ((object)Reader.LocalName == (object)_id49_PSName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@PSName = Reader.Value;
                    paramsRead[12] = true;
                }
                else if (!paramsRead[13] && ((object)Reader.LocalName == (object)_id50_Position && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Position = CollapseWhitespace(Reader.Value);
                    paramsRead[13] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":IsMandatory, :Aliases, :PSName, :Position");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Aliases = (global::System.String[])ShrinkArray(a_11, ca_11, typeof(global::System.String), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations43 = 0;
            int readerCount43 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id55_AllowEmptyCollection && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowEmptyCollection = Read1_Object(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id56_AllowEmptyString && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowEmptyString = Read1_Object(false, true);
                        paramsRead[1] = true;
                    }
                    else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id57_AllowNull && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@AllowNull = Read1_Object(false, true);
                        paramsRead[2] = true;
                    }
                    else if (!paramsRead[3] && ((object)Reader.LocalName == (object)_id58_ValidateNotNull && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateNotNull = Read1_Object(false, true);
                        paramsRead[3] = true;
                    }
                    else if (!paramsRead[4] && ((object)Reader.LocalName == (object)_id59_ValidateNotNullOrEmpty && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateNotNullOrEmpty = Read1_Object(false, true);
                        paramsRead[4] = true;
                    }
                    else if (!paramsRead[5] && ((object)Reader.LocalName == (object)_id60_ValidateCount && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateCount = Read4_Item(false, true);
                        paramsRead[5] = true;
                    }
                    else if (!paramsRead[6] && ((object)Reader.LocalName == (object)_id61_ValidateLength && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateLength = Read5_Item(false, true);
                        paramsRead[6] = true;
                    }
                    else if (!paramsRead[7] && ((object)Reader.LocalName == (object)_id62_ValidateRange && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ValidateRange = Read6_Item(false, true);
                        paramsRead[7] = true;
                    }
                    else if (((object)Reader.LocalName == (object)_id63_ValidateSet && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        if (!ReadNull())
                        {
                            global::System.String[] a_8_0 = null;
                            int ca_8_0 = 0;
                            if ((Reader.IsEmptyElement))
                            {
                                Reader.Skip();
                            }
                            else
                            {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                int whileIterations44 = 0;
                                int readerCount44 = ReaderCount;
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                                {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                    {
                                        if (((object)Reader.LocalName == (object)_id64_AllowedValue && (object)Reader.NamespaceURI == (object)_id2_Item))
                                        {
                                            {
                                                a_8_0 = (global::System.String[])EnsureArrayIndex(a_8_0, ca_8_0, typeof(global::System.String)); a_8_0[ca_8_0++] = Reader.ReadElementContentAsString();
                                            }
                                        }
                                        else
                                        {
                                            UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowedValue");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowedValue");
                                    }

                                    Reader.MoveToContent();
                                    CheckReaderCount(ref whileIterations44, ref readerCount44);
                                }

                                ReadEndElement();
                            }

                            o.@ValidateSet = (global::System.String[])ShrinkArray(a_8_0, ca_8_0, typeof(global::System.String), false);
                        }
                    }
                    else if (!paramsRead[9] && ((object)Reader.LocalName == (object)_id65_Obsolete && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Obsolete = Read7_ObsoleteAttributeMetadata(false, true);
                        paramsRead[9] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyCollection, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyString, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNullOrEmpty, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateCount, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateLength, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateRange, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateSet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyCollection, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowEmptyString, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:AllowNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNull, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateNotNullOrEmpty, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateCount, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateLength, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateRange, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ValidateSet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations43, ref readerCount43);
            }

            o.@Aliases = (global::System.String[])ShrinkArray(a_11, ca_11, typeof(global::System.String), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.GetCmdletParameters Read19_GetCmdletParameters(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id6_GetCmdletParameters && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.GetCmdletParameters o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.GetCmdletParameters();
            global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata[] a_0 = null;
            int ca_0 = 0;
            global::Microsoft.PowerShell.Cmdletization.Xml.Association[] a_1 = null;
            int ca_1 = 0;
            global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption[] a_2 = null;
            int ca_2 = 0;
            bool[] paramsRead = new bool[4];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[3] && ((object)Reader.LocalName == (object)_id99_DefaultCmdletParameterSet && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@DefaultCmdletParameterSet = Reader.Value;
                    paramsRead[3] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":DefaultCmdletParameterSet");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations45 = 0;
            int readerCount45 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (((object)Reader.LocalName == (object)_id109_QueryableProperties && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        if (!ReadNull())
                        {
                            global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata[] a_0_0 = null;
                            int ca_0_0 = 0;
                            if ((Reader.IsEmptyElement))
                            {
                                Reader.Skip();
                            }
                            else
                            {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                int whileIterations46 = 0;
                                int readerCount46 = ReaderCount;
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                                {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                    {
                                        if (((object)Reader.LocalName == (object)_id71_Property && (object)Reader.NamespaceURI == (object)_id2_Item))
                                        {
                                            a_0_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata)); a_0_0[ca_0_0++] = Read15_PropertyMetadata(false, true);
                                        }
                                        else
                                        {
                                            UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Property");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Property");
                                    }

                                    Reader.MoveToContent();
                                    CheckReaderCount(ref whileIterations46, ref readerCount46);
                                }

                                ReadEndElement();
                            }

                            o.@QueryableProperties = (global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata[])ShrinkArray(a_0_0, ca_0_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.PropertyMetadata), false);
                        }
                    }
                    else if (((object)Reader.LocalName == (object)_id110_QueryableAssociations && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        if (!ReadNull())
                        {
                            global::Microsoft.PowerShell.Cmdletization.Xml.Association[] a_1_0 = null;
                            int ca_1_0 = 0;
                            if ((Reader.IsEmptyElement))
                            {
                                Reader.Skip();
                            }
                            else
                            {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                int whileIterations47 = 0;
                                int readerCount47 = ReaderCount;
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                                {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                    {
                                        if (((object)Reader.LocalName == (object)_id9_Association && (object)Reader.NamespaceURI == (object)_id2_Item))
                                        {
                                            a_1_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.Association[])EnsureArrayIndex(a_1_0, ca_1_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.Association)); a_1_0[ca_1_0++] = Read17_Association(false, true);
                                        }
                                        else
                                        {
                                            UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Association");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Association");
                                    }

                                    Reader.MoveToContent();
                                    CheckReaderCount(ref whileIterations47, ref readerCount47);
                                }

                                ReadEndElement();
                            }

                            o.@QueryableAssociations = (global::Microsoft.PowerShell.Cmdletization.Xml.Association[])ShrinkArray(a_1_0, ca_1_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.Association), false);
                        }
                    }
                    else if (((object)Reader.LocalName == (object)_id111_QueryOptions && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        if (!ReadNull())
                        {
                            global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption[] a_2_0 = null;
                            int ca_2_0 = 0;
                            if ((Reader.IsEmptyElement))
                            {
                                Reader.Skip();
                            }
                            else
                            {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                int whileIterations48 = 0;
                                int readerCount48 = ReaderCount;
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                                {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                    {
                                        if (((object)Reader.LocalName == (object)_id74_Option && (object)Reader.NamespaceURI == (object)_id2_Item))
                                        {
                                            a_2_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption[])EnsureArrayIndex(a_2_0, ca_2_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption)); a_2_0[ca_2_0++] = Read18_QueryOption(false, true);
                                        }
                                        else
                                        {
                                            UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Option");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Option");
                                    }

                                    Reader.MoveToContent();
                                    CheckReaderCount(ref whileIterations48, ref readerCount48);
                                }

                                ReadEndElement();
                            }

                            o.@QueryOptions = (global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption[])ShrinkArray(a_2_0, ca_2_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.QueryOption), false);
                        }
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:QueryableProperties, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:QueryableAssociations, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:QueryOptions");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:QueryableProperties, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:QueryableAssociations, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:QueryOptions");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations45, ref readerCount45);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadataCmdletMetadata Read45_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id25_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadataCmdletMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadataCmdletMetadata();
            global::System.String[] a_3 = null;
            int ca_3 = 0;
            bool[] paramsRead = new bool[7];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id96_Verb && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Verb = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id97_Noun && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Noun = Reader.Value;
                    paramsRead[2] = true;
                }
                else if (((object)Reader.LocalName == (object)_id48_Aliases && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    string listValues = Reader.Value;
                    string[] vals = listValues.Split(null);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        a_3 = (global::System.String[])EnsureArrayIndex(a_3, ca_3, typeof(global::System.String)); a_3[ca_3++] = vals[i];
                    }
                }
                else if (!paramsRead[4] && ((object)Reader.LocalName == (object)_id23_ConfirmImpact && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ConfirmImpact = Read20_ConfirmImpact(Reader.Value);
                    o.@ConfirmImpactSpecified = true;
                    paramsRead[4] = true;
                }
                else if (!paramsRead[5] && ((object)Reader.LocalName == (object)_id98_HelpUri && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@HelpUri = CollapseWhitespace(Reader.Value);
                    paramsRead[5] = true;
                }
                else if (!paramsRead[6] && ((object)Reader.LocalName == (object)_id99_DefaultCmdletParameterSet && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@DefaultCmdletParameterSet = Reader.Value;
                    paramsRead[6] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Verb, :Noun, :Aliases, :ConfirmImpact, :HelpUri, :DefaultCmdletParameterSet");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Aliases = (global::System.String[])ShrinkArray(a_3, ca_3, typeof(global::System.String), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations49 = 0;
            int readerCount49 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id65_Obsolete && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Obsolete = Read7_ObsoleteAttributeMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations49, ref readerCount49);
            }

            o.@Aliases = (global::System.String[])ShrinkArray(a_3, ca_3, typeof(global::System.String), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CommonCmdletMetadata Read21_CommonCmdletMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id22_CommonCmdletMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id25_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read45_Item(isNullable, false);
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CommonCmdletMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CommonCmdletMetadata();
            global::System.String[] a_3 = null;
            int ca_3 = 0;
            bool[] paramsRead = new bool[6];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id96_Verb && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Verb = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id97_Noun && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Noun = Reader.Value;
                    paramsRead[2] = true;
                }
                else if (((object)Reader.LocalName == (object)_id48_Aliases && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    string listValues = Reader.Value;
                    string[] vals = listValues.Split(null);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        a_3 = (global::System.String[])EnsureArrayIndex(a_3, ca_3, typeof(global::System.String)); a_3[ca_3++] = vals[i];
                    }
                }
                else if (!paramsRead[4] && ((object)Reader.LocalName == (object)_id23_ConfirmImpact && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ConfirmImpact = Read20_ConfirmImpact(Reader.Value);
                    o.@ConfirmImpactSpecified = true;
                    paramsRead[4] = true;
                }
                else if (!paramsRead[5] && ((object)Reader.LocalName == (object)_id98_HelpUri && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@HelpUri = CollapseWhitespace(Reader.Value);
                    paramsRead[5] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Verb, :Noun, :Aliases, :ConfirmImpact, :HelpUri");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Aliases = (global::System.String[])ShrinkArray(a_3, ca_3, typeof(global::System.String), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations50 = 0;
            int readerCount50 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id65_Obsolete && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Obsolete = Read7_ObsoleteAttributeMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Obsolete");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations50, ref readerCount50);
            }

            o.@Aliases = (global::System.String[])ShrinkArray(a_3, ca_3, typeof(global::System.String), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.GetCmdletMetadata Read22_GetCmdletMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id21_GetCmdletMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.GetCmdletMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.GetCmdletMetadata();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o);
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations51 = 0;
            int readerCount51 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id84_CmdletMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletMetadata = Read21_CommonCmdletMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id6_GetCmdletParameters && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@GetCmdletParameters = Read19_GetCmdletParameters(false, true);
                        paramsRead[1] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletMetadata, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:GetCmdletParameters");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletMetadata, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:GetCmdletParameters");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations51, ref readerCount51);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodMetadata Read30_InstanceMethodMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id33_InstanceMethodMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodMetadata();
            global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata[] a_2 = null;
            int ca_2 = 0;
            bool[] paramsRead = new bool[3];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id86_MethodName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@MethodName = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":MethodName");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations52 = 0;
            int readerCount52 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id88_ReturnValue && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ReturnValue = Read24_Item(false, true);
                        paramsRead[0] = true;
                    }
                    else if (((object)Reader.LocalName == (object)_id89_Parameters && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        if (!ReadNull())
                        {
                            global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata[] a_2_0 = null;
                            int ca_2_0 = 0;
                            if ((Reader.IsEmptyElement))
                            {
                                Reader.Skip();
                            }
                            else
                            {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                int whileIterations53 = 0;
                                int readerCount53 = ReaderCount;
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                                {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                    {
                                        if (((object)Reader.LocalName == (object)_id76_Parameter && (object)Reader.NamespaceURI == (object)_id2_Item))
                                        {
                                            a_2_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata[])EnsureArrayIndex(a_2_0, ca_2_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata)); a_2_0[ca_2_0++] = Read25_Item(false, true);
                                        }
                                        else
                                        {
                                            UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Parameter");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Parameter");
                                    }

                                    Reader.MoveToContent();
                                    CheckReaderCount(ref whileIterations53, ref readerCount53);
                                }

                                ReadEndElement();
                            }

                            o.@Parameters = (global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata[])ShrinkArray(a_2_0, ca_2_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.InstanceMethodParameterMetadata), false);
                        }
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ReturnValue, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Parameters");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ReturnValue, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Parameters");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations52, ref readerCount52);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CommonMethodMetadata Read29_CommonMethodMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id26_CommonMethodMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id33_InstanceMethodMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read30_InstanceMethodMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id27_StaticMethodMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read28_StaticMethodMetadata(isNullable, false);
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CommonMethodMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CommonMethodMetadata();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id86_MethodName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@MethodName = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":MethodName");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations54 = 0;
            int readerCount54 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id88_ReturnValue && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@ReturnValue = Read24_Item(false, true);
                        paramsRead[0] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ReturnValue");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:ReturnValue");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations54, ref readerCount54);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CommonMethodParameterMetadata Read26_CommonMethodParameterMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id28_CommonMethodParameterMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id29_StaticMethodParameterMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read27_StaticMethodParameterMetadata(isNullable, false);
                else if (((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id31_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                    return Read25_Item(isNullable, false);
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CommonMethodParameterMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CommonMethodParameterMetadata();
            bool[] paramsRead = new bool[3];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id90_ParameterName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ParameterName = Reader.Value;
                    paramsRead[1] = true;
                }
                else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id91_DefaultValue && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@DefaultValue = Reader.Value;
                    paramsRead[2] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":ParameterName, :DefaultValue");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations55 = 0;
            int readerCount55 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id92_Type && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Type = Read2_TypeMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations55, ref readerCount55);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata Read31_InstanceCmdletMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id34_InstanceCmdletMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata();
            bool[] paramsRead = new bool[3];
            while (Reader.MoveToNextAttribute())
            {
                if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o);
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations56 = 0;
            int readerCount56 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id84_CmdletMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletMetadata = Read21_CommonCmdletMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id85_Method && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Method = Read30_InstanceMethodMetadata(false, true);
                        paramsRead[1] = true;
                    }
                    else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id6_GetCmdletParameters && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@GetCmdletParameters = Read19_GetCmdletParameters(false, true);
                        paramsRead[2] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletMetadata, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Method, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:GetCmdletParameters");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletMetadata, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Method, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:GetCmdletParameters");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations56, ref readerCount56);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadata Read36_ClassMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id3_ClassMetadata && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadata();
            global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata[] a_3 = null;
            int ca_3 = 0;
            global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData[] a_4 = null;
            int ca_4 = 0;
            bool[] paramsRead = new bool[8];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[5] && ((object)Reader.LocalName == (object)_id112_CmdletAdapter && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@CmdletAdapter = Reader.Value;
                    paramsRead[5] = true;
                }
                else if (!paramsRead[6] && ((object)Reader.LocalName == (object)_id113_ClassName && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ClassName = Reader.Value;
                    paramsRead[6] = true;
                }
                else if (!paramsRead[7] && ((object)Reader.LocalName == (object)_id114_ClassVersion && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@ClassVersion = Reader.Value;
                    paramsRead[7] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":CmdletAdapter, :ClassName, :ClassVersion");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations57 = 0;
            int readerCount57 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id115_Version && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        {
                            o.@Version = Reader.ReadElementContentAsString();
                        }

                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id116_DefaultNoun && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        {
                            o.@DefaultNoun = Reader.ReadElementContentAsString();
                        }

                        paramsRead[1] = true;
                    }
                    else if (!paramsRead[2] && ((object)Reader.LocalName == (object)_id117_InstanceCmdlets && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@InstanceCmdlets = Read32_ClassMetadataInstanceCmdlets(false, true);
                        paramsRead[2] = true;
                    }
                    else if (((object)Reader.LocalName == (object)_id118_StaticCmdlets && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        if (!ReadNull())
                        {
                            global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata[] a_3_0 = null;
                            int ca_3_0 = 0;
                            if ((Reader.IsEmptyElement))
                            {
                                Reader.Skip();
                            }
                            else
                            {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                int whileIterations58 = 0;
                                int readerCount58 = ReaderCount;
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                                {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                    {
                                        if (((object)Reader.LocalName == (object)_id79_Cmdlet && (object)Reader.NamespaceURI == (object)_id2_Item))
                                        {
                                            a_3_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata[])EnsureArrayIndex(a_3_0, ca_3_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata)); a_3_0[ca_3_0++] = Read34_StaticCmdletMetadata(false, true);
                                        }
                                        else
                                        {
                                            UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Cmdlet");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Cmdlet");
                                    }

                                    Reader.MoveToContent();
                                    CheckReaderCount(ref whileIterations58, ref readerCount58);
                                }

                                ReadEndElement();
                            }

                            o.@StaticCmdlets = (global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata[])ShrinkArray(a_3_0, ca_3_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.StaticCmdletMetadata), false);
                        }
                    }
                    else if (((object)Reader.LocalName == (object)_id119_CmdletAdapterPrivateData && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        if (!ReadNull())
                        {
                            global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData[] a_4_0 = null;
                            int ca_4_0 = 0;
                            if ((Reader.IsEmptyElement))
                            {
                                Reader.Skip();
                            }
                            else
                            {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                int whileIterations59 = 0;
                                int readerCount59 = ReaderCount;
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                                {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                    {
                                        if (((object)Reader.LocalName == (object)_id81_Data && (object)Reader.NamespaceURI == (object)_id2_Item))
                                        {
                                            a_4_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData[])EnsureArrayIndex(a_4_0, ca_4_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData)); a_4_0[ca_4_0++] = Read35_ClassMetadataData(false, true);
                                        }
                                        else
                                        {
                                            UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Data");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Data");
                                    }

                                    Reader.MoveToContent();
                                    CheckReaderCount(ref whileIterations59, ref readerCount59);
                                }

                                ReadEndElement();
                            }

                            o.@CmdletAdapterPrivateData = (global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData[])ShrinkArray(a_4_0, ca_4_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataData), false);
                        }
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Version, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:DefaultNoun, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:InstanceCmdlets, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:StaticCmdlets, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletAdapterPrivateData");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Version, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:DefaultNoun, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:InstanceCmdlets, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:StaticCmdlets, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletAdapterPrivateData");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations57, ref readerCount57);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataInstanceCmdlets Read32_ClassMetadataInstanceCmdlets(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id4_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataInstanceCmdlets o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataInstanceCmdlets();
            global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata[] a_2 = null;
            int ca_2 = 0;
            bool[] paramsRead = new bool[3];
            while (Reader.MoveToNextAttribute())
            {
                if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o);
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Cmdlet = (global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata[])ShrinkArray(a_2, ca_2, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations60 = 0;
            int readerCount60 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id6_GetCmdletParameters && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@GetCmdletParameters = Read19_GetCmdletParameters(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id120_GetCmdlet && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@GetCmdlet = Read22_GetCmdletMetadata(false, true);
                        paramsRead[1] = true;
                    }
                    else if (((object)Reader.LocalName == (object)_id79_Cmdlet && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        a_2 = (global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata[])EnsureArrayIndex(a_2, ca_2, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata)); a_2[ca_2++] = Read31_InstanceCmdletMetadata(false, true);
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:GetCmdletParameters, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:GetCmdlet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Cmdlet");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:GetCmdletParameters, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:GetCmdlet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Cmdlet");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations60, ref readerCount60);
            }

            o.@Cmdlet = (global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata[])ShrinkArray(a_2, ca_2, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataInstanceCmdlets Read40_ClassMetadataInstanceCmdlets(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id5_ClassMetadataInstanceCmdlets && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataInstanceCmdlets o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.ClassMetadataInstanceCmdlets();
            global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata[] a_2 = null;
            int ca_2 = 0;
            bool[] paramsRead = new bool[3];
            while (Reader.MoveToNextAttribute())
            {
                if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o);
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                o.@Cmdlet = (global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata[])ShrinkArray(a_2, ca_2, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata), true);
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations61 = 0;
            int readerCount61 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id6_GetCmdletParameters && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@GetCmdletParameters = Read19_GetCmdletParameters(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id120_GetCmdlet && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@GetCmdlet = Read22_GetCmdletMetadata(false, true);
                        paramsRead[1] = true;
                    }
                    else if (((object)Reader.LocalName == (object)_id79_Cmdlet && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        a_2 = (global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata[])EnsureArrayIndex(a_2, ca_2, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata)); a_2[ca_2++] = Read31_InstanceCmdletMetadata(false, true);
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:GetCmdletParameters, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:GetCmdlet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Cmdlet");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:GetCmdletParameters, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:GetCmdlet, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Cmdlet");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations61, ref readerCount61);
            }

            o.@Cmdlet = (global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata[])ShrinkArray(a_2, ca_2, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.InstanceCmdletMetadata), true);
            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.AssociationAssociatedInstance Read41_AssociationAssociatedInstance(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id10_AssociationAssociatedInstance && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.AssociationAssociatedInstance o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.AssociationAssociatedInstance();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o);
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations62 = 0;
            int readerCount62 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id92_Type && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Type = Read2_TypeMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id11_CmdletParameterMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletParameterMetadata = Read12_Item(false, true);
                        paramsRead[1] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletParameterMetadata");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations62, ref readerCount62);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateCount Read42_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id14_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateCount o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateCount();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id67_Min && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Min = CollapseWhitespace(Reader.Value);
                    paramsRead[0] = true;
                }
                else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id68_Max && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Max = CollapseWhitespace(Reader.Value);
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Min, :Max");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations63 = 0;
            int readerCount63 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    UnknownNode((object)o, string.Empty);
                }
                else
                {
                    UnknownNode((object)o, string.Empty);
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations63, ref readerCount63);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateLength Read43_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id15_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateLength o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateLength();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id67_Min && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Min = CollapseWhitespace(Reader.Value);
                    paramsRead[0] = true;
                }
                else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id68_Max && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Max = CollapseWhitespace(Reader.Value);
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Min, :Max");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations64 = 0;
            int readerCount64 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    UnknownNode((object)o, string.Empty);
                }
                else
                {
                    UnknownNode((object)o, string.Empty);
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations64, ref readerCount64);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateRange Read44_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id16_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateRange o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CmdletParameterMetadataValidateRange();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id67_Min && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Min = CollapseWhitespace(Reader.Value);
                    paramsRead[0] = true;
                }
                else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id68_Max && (object)Reader.NamespaceURI == (object)_id4_Item))
                {
                    o.@Max = CollapseWhitespace(Reader.Value);
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o, @":Min, :Max");
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations65 = 0;
            int readerCount65 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    UnknownNode((object)o, string.Empty);
                }
                else
                {
                    UnknownNode((object)o, string.Empty);
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations65, ref readerCount65);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.CommonMethodMetadataReturnValue Read46_Item(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id32_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.CommonMethodMetadataReturnValue o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.CommonMethodMetadataReturnValue();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o);
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations66 = 0;
            int readerCount66 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id92_Type && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Type = Read2_TypeMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else if (!paramsRead[1] && ((object)Reader.LocalName == (object)_id30_CmdletOutputMetadata && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@CmdletOutputMetadata = Read23_CmdletOutputMetadata(false, true);
                        paramsRead[1] = true;
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletOutputMetadata");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Type, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:CmdletOutputMetadata");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations66, ref readerCount66);
            }

            ReadEndElement();
            return o;
        }

        global::Microsoft.PowerShell.Cmdletization.Xml.PowerShellMetadata Read39_PowerShellMetadata(bool isNullable, bool checkType)
        {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType)
            {
                if (xsiType == null || ((object)((System.Xml.XmlQualifiedName)xsiType).Name == (object)_id4_Item && (object)((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)_id2_Item))
                {
                }
                else
                    throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }

            if (isNull) return null;
            global::Microsoft.PowerShell.Cmdletization.Xml.PowerShellMetadata o;
            o = new global::Microsoft.PowerShell.Cmdletization.Xml.PowerShellMetadata();
            global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum[] a_1 = null;
            int ca_1 = 0;
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute())
            {
                if (!IsXmlnsAttribute(Reader.Name))
                {
                    UnknownNode((object)o);
                }
            }

            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                return o;
            }

            Reader.ReadStartElement();
            Reader.MoveToContent();
            int whileIterations67 = 0;
            int readerCount67 = ReaderCount;
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
            {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (!paramsRead[0] && ((object)Reader.LocalName == (object)_id121_Class && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        o.@Class = Read36_ClassMetadata(false, true);
                        paramsRead[0] = true;
                    }
                    else if (((object)Reader.LocalName == (object)_id122_Enums && (object)Reader.NamespaceURI == (object)_id2_Item))
                    {
                        if (!ReadNull())
                        {
                            global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum[] a_1_0 = null;
                            int ca_1_0 = 0;
                            if ((Reader.IsEmptyElement))
                            {
                                Reader.Skip();
                            }
                            else
                            {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                int whileIterations68 = 0;
                                int readerCount68 = ReaderCount;
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
                                {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element)
                                    {
                                        if (((object)Reader.LocalName == (object)_id83_Enum && (object)Reader.NamespaceURI == (object)_id2_Item))
                                        {
                                            a_1_0 = (global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum[])EnsureArrayIndex(a_1_0, ca_1_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum)); a_1_0[ca_1_0++] = Read38_EnumMetadataEnum(false, true);
                                        }
                                        else
                                        {
                                            UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Enum");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(null, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Enum");
                                    }

                                    Reader.MoveToContent();
                                    CheckReaderCount(ref whileIterations68, ref readerCount68);
                                }

                                ReadEndElement();
                            }

                            o.@Enums = (global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum[])ShrinkArray(a_1_0, ca_1_0, typeof(global::Microsoft.PowerShell.Cmdletization.Xml.EnumMetadataEnum), false);
                        }
                    }
                    else
                    {
                        UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Class, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Enums");
                    }
                }
                else
                {
                    UnknownNode((object)o, @"http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Class, http://schemas.microsoft.com/cmdlets-over-objects/2009/11:Enums");
                }

                Reader.MoveToContent();
                CheckReaderCount(ref whileIterations67, ref readerCount67);
            }

            ReadEndElement();
            return o;
        }

        string _id72_ArrayOfAssociation;
        string _id46_AllowGlobbing;
        string _id6_GetCmdletParameters;
        string _id25_Item;
        string _id62_ValidateRange;
        string _id118_StaticCmdlets;
        string _id58_ValidateNotNull;
        string _id17_ObsoleteAttributeMetadata;
        string _id49_PSName;
        string _id116_DefaultNoun;
        string _id38_ClassMetadataData;
        string _id114_ClassVersion;
        string _id66_Message;
        string _id65_Obsolete;
        string _id51_ValueFromPipeline;
        string _id108_MinValueQuery;
        string _id119_CmdletAdapterPrivateData;
        string _id21_GetCmdletMetadata;
        string _id120_GetCmdlet;
        string _id67_Min;
        string _id56_AllowEmptyString;
        string _id30_CmdletOutputMetadata;
        string _id106_RegularQuery;
        string _id74_Option;
        string _id75_Item;
        string _id23_ConfirmImpact;
        string _id117_InstanceCmdlets;
        string _id83_Enum;
        string _id40_EnumMetadataEnumValue;
        string _id111_QueryOptions;
        string _id34_InstanceCmdletMetadata;
        string _id60_ValidateCount;
        string _id45_BitwiseFlags;
        string _id81_Data;
        string _id31_Item;
        string _id1_PowerShellMetadata;
        string _id98_HelpUri;
        string _id91_DefaultValue;
        string _id4_Item;
        string _id32_Item;
        string _id43_EnumName;
        string _id122_Enums;
        string _id82_ArrayOfEnumMetadataEnum;
        string _id14_Item;
        string _id48_Aliases;
        string _id115_Version;
        string _id11_CmdletParameterMetadata;
        string _id70_ArrayOfPropertyMetadata;
        string _id9_Association;
        string _id102_ResultRole;
        string _id29_StaticMethodParameterMetadata;
        string _id97_Noun;
        string _id47_IsMandatory;
        string _id35_PropertyQuery;
        string _id54_ErrorOnNoMatch;
        string _id3_ClassMetadata;
        string _id77_Item;
        string _id2_Item;
        string _id22_CommonCmdletMetadata;
        string _id37_ItemsChoiceType;
        string _id36_WildcardablePropertyQuery;
        string _id113_ClassName;
        string _id64_AllowedValue;
        string _id52_Item;
        string _id55_AllowEmptyCollection;
        string _id13_Item;
        string _id76_Parameter;
        string _id19_Item;
        string _id105_MaxValueQuery;
        string _id101_SourceRole;
        string _id5_ClassMetadataInstanceCmdlets;
        string _id112_CmdletAdapter;
        string _id10_AssociationAssociatedInstance;
        string _id93_ErrorCode;
        string _id41_Name;
        string _id68_Max;
        string _id50_Position;
        string _id100_OptionName;
        string _id84_CmdletMetadata;
        string _id87_CmdletParameterSet;
        string _id104_PropertyName;
        string _id28_CommonMethodParameterMetadata;
        string _id107_ExcludeQuery;
        string _id92_Type;
        string _id33_InstanceMethodMetadata;
        string _id63_ValidateSet;
        string _id53_CmdletParameterSets;
        string _id15_Item;
        string _id109_QueryableProperties;
        string _id57_AllowNull;
        string _id80_ArrayOfClassMetadataData;
        string _id99_DefaultCmdletParameterSet;
        string _id20_QueryOption;
        string _id89_Parameters;
        string _id90_ParameterName;
        string _id61_ValidateLength;
        string _id78_ArrayOfStaticCmdletMetadata;
        string _id16_Item;
        string _id39_EnumMetadataEnum;
        string _id7_PropertyMetadata;
        string _id110_QueryableAssociations;
        string _id86_MethodName;
        string _id8_TypeMetadata;
        string _id71_Property;
        string _id27_StaticMethodMetadata;
        string _id94_PSType;
        string _id44_UnderlyingType;
        string _id103_AssociatedInstance;
        string _id79_Cmdlet;
        string _id18_Item;
        string _id85_Method;
        string _id95_ETSType;
        string _id26_CommonMethodMetadata;
        string _id88_ReturnValue;
        string _id69_ArrayOfString;
        string _id24_StaticCmdletMetadata;
        string _id59_ValidateNotNullOrEmpty;
        string _id96_Verb;
        string _id121_Class;
        string _id73_ArrayOfQueryOption;
        string _id12_Item;
        string _id42_Value;

        private void InitIDs()
        {
            _id72_ArrayOfAssociation = Reader.NameTable.Add(@"ArrayOfAssociation");
            _id46_AllowGlobbing = Reader.NameTable.Add(@"AllowGlobbing");
            _id6_GetCmdletParameters = Reader.NameTable.Add(@"GetCmdletParameters");
            _id25_Item = Reader.NameTable.Add(@"StaticCmdletMetadataCmdletMetadata");
            _id62_ValidateRange = Reader.NameTable.Add(@"ValidateRange");
            _id118_StaticCmdlets = Reader.NameTable.Add(@"StaticCmdlets");
            _id58_ValidateNotNull = Reader.NameTable.Add(@"ValidateNotNull");
            _id17_ObsoleteAttributeMetadata = Reader.NameTable.Add(@"ObsoleteAttributeMetadata");
            _id49_PSName = Reader.NameTable.Add(@"PSName");
            _id116_DefaultNoun = Reader.NameTable.Add(@"DefaultNoun");
            _id38_ClassMetadataData = Reader.NameTable.Add(@"ClassMetadataData");
            _id114_ClassVersion = Reader.NameTable.Add(@"ClassVersion");
            _id66_Message = Reader.NameTable.Add(@"Message");
            _id65_Obsolete = Reader.NameTable.Add(@"Obsolete");
            _id51_ValueFromPipeline = Reader.NameTable.Add(@"ValueFromPipeline");
            _id108_MinValueQuery = Reader.NameTable.Add(@"MinValueQuery");
            _id119_CmdletAdapterPrivateData = Reader.NameTable.Add(@"CmdletAdapterPrivateData");
            _id21_GetCmdletMetadata = Reader.NameTable.Add(@"GetCmdletMetadata");
            _id120_GetCmdlet = Reader.NameTable.Add(@"GetCmdlet");
            _id67_Min = Reader.NameTable.Add(@"Min");
            _id56_AllowEmptyString = Reader.NameTable.Add(@"AllowEmptyString");
            _id30_CmdletOutputMetadata = Reader.NameTable.Add(@"CmdletOutputMetadata");
            _id106_RegularQuery = Reader.NameTable.Add(@"RegularQuery");
            _id74_Option = Reader.NameTable.Add(@"Option");
            _id75_Item = Reader.NameTable.Add(@"ArrayOfStaticMethodParameterMetadata");
            _id23_ConfirmImpact = Reader.NameTable.Add(@"ConfirmImpact");
            _id117_InstanceCmdlets = Reader.NameTable.Add(@"InstanceCmdlets");
            _id83_Enum = Reader.NameTable.Add(@"Enum");
            _id40_EnumMetadataEnumValue = Reader.NameTable.Add(@"EnumMetadataEnumValue");
            _id111_QueryOptions = Reader.NameTable.Add(@"QueryOptions");
            _id34_InstanceCmdletMetadata = Reader.NameTable.Add(@"InstanceCmdletMetadata");
            _id60_ValidateCount = Reader.NameTable.Add(@"ValidateCount");
            _id45_BitwiseFlags = Reader.NameTable.Add(@"BitwiseFlags");
            _id81_Data = Reader.NameTable.Add(@"Data");
            _id31_Item = Reader.NameTable.Add(@"InstanceMethodParameterMetadata");
            _id1_PowerShellMetadata = Reader.NameTable.Add(@"PowerShellMetadata");
            _id98_HelpUri = Reader.NameTable.Add(@"HelpUri");
            _id91_DefaultValue = Reader.NameTable.Add(@"DefaultValue");
            _id4_Item = Reader.NameTable.Add(string.Empty);
            _id32_Item = Reader.NameTable.Add(@"CommonMethodMetadataReturnValue");
            _id43_EnumName = Reader.NameTable.Add(@"EnumName");
            _id122_Enums = Reader.NameTable.Add(@"Enums");
            _id82_ArrayOfEnumMetadataEnum = Reader.NameTable.Add(@"ArrayOfEnumMetadataEnum");
            _id14_Item = Reader.NameTable.Add(@"CmdletParameterMetadataValidateCount");
            _id48_Aliases = Reader.NameTable.Add(@"Aliases");
            _id115_Version = Reader.NameTable.Add(@"Version");
            _id11_CmdletParameterMetadata = Reader.NameTable.Add(@"CmdletParameterMetadata");
            _id70_ArrayOfPropertyMetadata = Reader.NameTable.Add(@"ArrayOfPropertyMetadata");
            _id9_Association = Reader.NameTable.Add(@"Association");
            _id102_ResultRole = Reader.NameTable.Add(@"ResultRole");
            _id29_StaticMethodParameterMetadata = Reader.NameTable.Add(@"StaticMethodParameterMetadata");
            _id97_Noun = Reader.NameTable.Add(@"Noun");
            _id47_IsMandatory = Reader.NameTable.Add(@"IsMandatory");
            _id35_PropertyQuery = Reader.NameTable.Add(@"PropertyQuery");
            _id54_ErrorOnNoMatch = Reader.NameTable.Add(@"ErrorOnNoMatch");
            _id3_ClassMetadata = Reader.NameTable.Add(@"ClassMetadata");
            _id77_Item = Reader.NameTable.Add(@"ArrayOfInstanceMethodParameterMetadata");
            _id2_Item = Reader.NameTable.Add(@"http://schemas.microsoft.com/cmdlets-over-objects/2009/11");
            _id22_CommonCmdletMetadata = Reader.NameTable.Add(@"CommonCmdletMetadata");
            _id37_ItemsChoiceType = Reader.NameTable.Add(@"ItemsChoiceType");
            _id36_WildcardablePropertyQuery = Reader.NameTable.Add(@"WildcardablePropertyQuery");
            _id113_ClassName = Reader.NameTable.Add(@"ClassName");
            _id64_AllowedValue = Reader.NameTable.Add(@"AllowedValue");
            _id52_Item = Reader.NameTable.Add(@"ValueFromPipelineByPropertyName");
            _id55_AllowEmptyCollection = Reader.NameTable.Add(@"AllowEmptyCollection");
            _id13_Item = Reader.NameTable.Add(@"CmdletParameterMetadataForGetCmdletFilteringParameter");
            _id76_Parameter = Reader.NameTable.Add(@"Parameter");
            _id19_Item = Reader.NameTable.Add(@"CmdletParameterMetadataForStaticMethodParameter");
            _id105_MaxValueQuery = Reader.NameTable.Add(@"MaxValueQuery");
            _id101_SourceRole = Reader.NameTable.Add(@"SourceRole");
            _id5_ClassMetadataInstanceCmdlets = Reader.NameTable.Add(@"ClassMetadataInstanceCmdlets");
            _id112_CmdletAdapter = Reader.NameTable.Add(@"CmdletAdapter");
            _id10_AssociationAssociatedInstance = Reader.NameTable.Add(@"AssociationAssociatedInstance");
            _id93_ErrorCode = Reader.NameTable.Add(@"ErrorCode");
            _id41_Name = Reader.NameTable.Add(@"Name");
            _id68_Max = Reader.NameTable.Add(@"Max");
            _id50_Position = Reader.NameTable.Add(@"Position");
            _id100_OptionName = Reader.NameTable.Add(@"OptionName");
            _id84_CmdletMetadata = Reader.NameTable.Add(@"CmdletMetadata");
            _id87_CmdletParameterSet = Reader.NameTable.Add(@"CmdletParameterSet");
            _id104_PropertyName = Reader.NameTable.Add(@"PropertyName");
            _id28_CommonMethodParameterMetadata = Reader.NameTable.Add(@"CommonMethodParameterMetadata");
            _id107_ExcludeQuery = Reader.NameTable.Add(@"ExcludeQuery");
            _id92_Type = Reader.NameTable.Add(@"Type");
            _id33_InstanceMethodMetadata = Reader.NameTable.Add(@"InstanceMethodMetadata");
            _id63_ValidateSet = Reader.NameTable.Add(@"ValidateSet");
            _id53_CmdletParameterSets = Reader.NameTable.Add(@"CmdletParameterSets");
            _id15_Item = Reader.NameTable.Add(@"CmdletParameterMetadataValidateLength");
            _id109_QueryableProperties = Reader.NameTable.Add(@"QueryableProperties");
            _id57_AllowNull = Reader.NameTable.Add(@"AllowNull");
            _id80_ArrayOfClassMetadataData = Reader.NameTable.Add(@"ArrayOfClassMetadataData");
            _id99_DefaultCmdletParameterSet = Reader.NameTable.Add(@"DefaultCmdletParameterSet");
            _id20_QueryOption = Reader.NameTable.Add(@"QueryOption");
            _id89_Parameters = Reader.NameTable.Add(@"Parameters");
            _id90_ParameterName = Reader.NameTable.Add(@"ParameterName");
            _id61_ValidateLength = Reader.NameTable.Add(@"ValidateLength");
            _id78_ArrayOfStaticCmdletMetadata = Reader.NameTable.Add(@"ArrayOfStaticCmdletMetadata");
            _id16_Item = Reader.NameTable.Add(@"CmdletParameterMetadataValidateRange");
            _id39_EnumMetadataEnum = Reader.NameTable.Add(@"EnumMetadataEnum");
            _id7_PropertyMetadata = Reader.NameTable.Add(@"PropertyMetadata");
            _id110_QueryableAssociations = Reader.NameTable.Add(@"QueryableAssociations");
            _id86_MethodName = Reader.NameTable.Add(@"MethodName");
            _id8_TypeMetadata = Reader.NameTable.Add(@"TypeMetadata");
            _id71_Property = Reader.NameTable.Add(@"Property");
            _id27_StaticMethodMetadata = Reader.NameTable.Add(@"StaticMethodMetadata");
            _id94_PSType = Reader.NameTable.Add(@"PSType");
            _id44_UnderlyingType = Reader.NameTable.Add(@"UnderlyingType");
            _id103_AssociatedInstance = Reader.NameTable.Add(@"AssociatedInstance");
            _id79_Cmdlet = Reader.NameTable.Add(@"Cmdlet");
            _id18_Item = Reader.NameTable.Add(@"CmdletParameterMetadataForInstanceMethodParameter");
            _id85_Method = Reader.NameTable.Add(@"Method");
            _id95_ETSType = Reader.NameTable.Add(@"ETSType");
            _id26_CommonMethodMetadata = Reader.NameTable.Add(@"CommonMethodMetadata");
            _id88_ReturnValue = Reader.NameTable.Add(@"ReturnValue");
            _id69_ArrayOfString = Reader.NameTable.Add(@"ArrayOfString");
            _id24_StaticCmdletMetadata = Reader.NameTable.Add(@"StaticCmdletMetadata");
            _id59_ValidateNotNullOrEmpty = Reader.NameTable.Add(@"ValidateNotNullOrEmpty");
            _id96_Verb = Reader.NameTable.Add(@"Verb");
            _id121_Class = Reader.NameTable.Add(@"Class");
            _id73_ArrayOfQueryOption = Reader.NameTable.Add(@"ArrayOfQueryOption");
            _id12_Item = Reader.NameTable.Add(@"CmdletParameterMetadataForGetCmdletParameter");
            _id42_Value = Reader.NameTable.Add(@"Value");
        }
    }

    internal sealed class PowerShellMetadataSerializer
    {
        internal object Deserialize(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            XmlSerializationReader1 cdxmlSerializationReader = new XmlSerializationReader1(reader);
            return cdxmlSerializationReader.Read50_PowerShellMetadata();
        }
    }
}
#endif
