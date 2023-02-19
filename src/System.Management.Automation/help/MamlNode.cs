// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;

namespace System.Management.Automation
{
    /// <summary>
    /// MamlNode is an xml node in MAML schema. Maml schema includes formatting oriented tags like para, list
    /// etc, which needs to be taken care of during display. As a result, xml node in Maml schema can't be
    /// converted into PSObject directly with XmlNodeAdapter.
    ///
    /// MamlNode class provides logic in converting formatting tags into the format acceptable by monad format
    /// and output engine.
    ///
    /// Following three kinds of formatting tags are supported per our agreement with Maml team,
    ///     1. para,
    ///         <para>
    ///             para text here
    ///         </para>
    ///     2. list,
    ///         <list class="ordered|unordered">
    ///             <listItem>
    ///                 <para>
    ///                     listItem Text here
    ///                 </para>
    ///             </listItem>
    ///         </list>
    ///     3. definition list,
    ///         <definitionList>
    ///             <definitionListItem>
    ///                 <term>
    ///                     definition term text here
    ///                 </term>
    ///                 <definition>
    ///                     <para>
    ///                         definition text here
    ///                     </para>
    ///                 </definition>
    ///             </definitionListItem>
    ///         </definitionList>
    /// After processing, content of these three tags will be converted into textItem and its derivations,
    ///     1. para => paraTextItem
    ///         <textItem class="paraTextItem">
    ///             <text>para text here</text>
    ///         </textItem>
    ///     2. list => a list of listTextItem's (which can be ordered or unordered)
    ///         <textItem class="unorderedListTextItem">
    ///             <tag>*</tag>
    ///             <text>text for list item 1</text>
    ///         </textItem>
    ///         <textItem class="unorderedListTextItem">
    ///             <tag>*</tag>
    ///             <text>text for list item 2</text>
    ///         </textItem>
    ///     3. definitionList => a list of definitionTextItem's
    ///         <definitionListItem>
    ///             <term>definition term here</term>
    ///             <definition>definition text here</definition>
    ///         </definitionListItem>
    /// </summary>
    internal class MamlNode
    {
        /// <summary>
        /// Constructor for HelpInfo.
        /// </summary>
        internal MamlNode(XmlNode xmlNode)
        {
            _xmlNode = xmlNode;
        }

        private readonly XmlNode _xmlNode;

        /// <summary>
        /// Underline xmlNode for this MamlNode object.
        /// </summary>
        /// <value></value>
        internal XmlNode XmlNode
        {
            get
            {
                return _xmlNode;
            }
        }

        private PSObject _mshObject;

        /// <summary>
        /// MshObject which is converted from XmlNode.
        /// </summary>
        /// <value></value>
        internal PSObject PSObject
        {
            get
            {
                if (_mshObject == null)
                {
                    // There is no XSLT to convert docs to supported maml format
                    // We dont want comments etc to spoil our format.
                    // So remove all unsupported nodes before constructing help
                    // object.
                    RemoveUnsupportedNodes(_xmlNode);
                    _mshObject = GetPSObject(_xmlNode);
                }

                return _mshObject;
            }
        }

        #region Conversion of xmlNode => PSObject

        /// <summary>
        /// Convert an xmlNode into an PSObject. There are four scenarios,
        ///     1. Null xml, this will return an PSObject wrapping a null object.
        ///     2. Atomic xml, which is an xmlNode with only one simple text child node
        ///         <atomicXml attribute="value">
        ///             atomic xml text
        ///         </atomicXml>
        ///        In this case, an PSObject that wraps string "atomic xml text" will be returned with following properties
        ///             attribute => name
        ///     3. Composite xml, which is an xmlNode with structured child nodes, but not a special case for Maml formatting.
        ///         <compositeXml attribute="attribute">
        ///             <singleChildNode>
        ///                 single child node text
        ///             </singleChildNode>
        ///             <dupChildNode>
        ///                 dup child node text 1
        ///             </dupChildNode>
        ///             <dupChildNode>
        ///                 dup child node text 2
        ///             </dupChildNode>
        ///         </compositeXml>
        ///        In this case, an PSObject will base generated based on an inside PSObject,
        ///        which in turn has following properties
        ///             a. property "singleChildNode", with its value an PSObject wrapping string "single child node text"
        ///             b. property "dupChildNode", with its value an PSObject array wrapping strings for two dupChildNode's
        ///        The outside PSObject will have property,
        ///             a. property "attribute", with its value an PSObject wrapping string "attribute"
        ///     4. Maml formatting xml, this is a special case for Composite xml, for example
        ///         <description attribute="value">
        ///             <para>
        ///                 para 1
        ///             </para>
        ///             <list>
        ///                 <listItem>
        ///                     <para>
        ///                         list item 1
        ///                     </para>
        ///                 </listItem>
        ///                 <listItem>
        ///                     <para>
        ///                         list item 2
        ///                     </para>
        ///                 </listItem>
        ///             </list>
        ///             <definitionList>
        ///                 <definitionListItem>
        ///                     <term>
        ///                         term 1
        ///                     </term>
        ///                     <definition>
        ///                         definition list item 1
        ///                     </definition>
        ///                 </definitionListItem>
        ///                 <definitionListItem>
        ///                     <term>
        ///                         term 2
        ///                     </term>
        ///                     <definition>
        ///                         definition list item 2
        ///                     </definition>
        ///                 </definitionListItem>
        ///             </definitionList>
        ///         </description>
        ///         In this case, an PSObject based on an PSObject array will be created. The inside PSObject array
        ///         will contain following items
        ///             . a MamlParaTextItem based on "para 1"
        ///             . a MamlUnorderedListItem based on "list item 1"
        ///             . a MamlUnorderedListItem based on "list item 2"
        ///             . a MamlDefinitionListItem based on "definition list item 1"
        ///             . a MamlDefinitionListItem based on "definition list item 2"
        ///
        ///         The outside PSObject will have a property
        ///             attribute => "value"
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private PSObject GetPSObject(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return new PSObject();

            PSObject mshObject = null;

            if (IsAtomic(xmlNode))
            {
                mshObject = new PSObject(xmlNode.InnerText.Trim());
            }
            else if (IncludeMamlFormatting(xmlNode))
            {
                mshObject = new PSObject(GetMamlFormattingPSObjects(xmlNode));
            }
            else
            {
                mshObject = new PSObject(GetInsidePSObject(xmlNode));
                // Add typeNames to this MSHObject and create views so that
                // the output is readable. This is done only for complex nodes.
                mshObject.TypeNames.Clear();

                if (xmlNode.Attributes["type"] != null)
                {
                    if (string.Equals(xmlNode.Attributes["type"].Value, "field", StringComparison.OrdinalIgnoreCase))
                        mshObject.TypeNames.Add("MamlPSClassHelpInfo#field");
                    else if (string.Equals(xmlNode.Attributes["type"].Value, "method", StringComparison.OrdinalIgnoreCase))
                        mshObject.TypeNames.Add("MamlPSClassHelpInfo#method");
                }

                mshObject.TypeNames.Add("MamlCommandHelpInfo#" + xmlNode.LocalName);
            }

            if (xmlNode.Attributes != null)
            {
                foreach (XmlNode attribute in xmlNode.Attributes)
                {
                    mshObject.Properties.Add(new PSNoteProperty(attribute.Name, attribute.Value));
                }
            }

            return mshObject;
        }

        /// <summary>
        /// Get inside PSObject created based on inside nodes of xmlNode.
        ///
        /// The inside PSObject will be based on null. It will created one
        /// property per inside node grouping by node names.
        ///
        /// For example, for xmlNode like,
        ///     <command>
        ///         <name>get-item</name>
        ///         <note>note 1</note>
        ///         <note>note 2</note>
        ///     </command>
        /// It will create an PSObject based on null, with following two properties
        ///     . property 1: name="name" value=an PSObject to wrap string "get-item"
        ///     . property 2: name="note" value=an PSObject array with following two PSObjects
        ///         1. PSObject wrapping string "note 1"
        ///         2. PSObject wrapping string "note 2"
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private PSObject GetInsidePSObject(XmlNode xmlNode)
        {
            Hashtable properties = GetInsideProperties(xmlNode);

            PSObject mshObject = new PSObject();

            IDictionaryEnumerator enumerator = properties.GetEnumerator();

            while (enumerator.MoveNext())
            {
                mshObject.Properties.Add(new PSNoteProperty((string)enumerator.Key, enumerator.Value));
            }

            return mshObject;
        }

        /// <summary>
        /// This is for getting inside properties of an XmlNode. Properties are
        /// stored in a hashtable with key as property name and value as property value.
        ///
        /// Inside node with same node names will be grouped into one property with
        /// property value as an array.
        ///
        /// For example, for xmlNode like,
        ///     <command>
        ///         <name>get-item</name>
        ///         <note>note 1</note>
        ///         <note>note 2</note>
        ///     </command>
        /// It will create an PSObject based on null, with following two properties
        ///     . property 1: name="name" value=an PSObject to wrap string "get-item"
        ///     . property 2: name="note" value=an PSObject array with following two PSObjects
        ///         1. PSObject wrapping string "note 1"
        ///         2. PSObject wrapping string "note 2"
        ///
        /// Since we don't know whether an node name will be used more than once,
        /// We are making each property value is an array (PSObject[]) to start with.
        /// At the end, SimplifyProperties will be called to reduce PSObject[] containing
        /// only one element to PSObject itself.
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private Hashtable GetInsideProperties(XmlNode xmlNode)
        {
            Hashtable properties = new Hashtable(StringComparer.OrdinalIgnoreCase);

            if (xmlNode == null)
                return properties;

            if (xmlNode.ChildNodes != null)
            {
                foreach (XmlNode childNode in xmlNode.ChildNodes)
                {
                    AddProperty(properties, childNode.LocalName, GetPSObject(childNode));
                }
            }

            return SimplifyProperties(properties);
        }

        /// <summary>
        /// Removes unsupported child nodes recursively from the given
        /// xml node so that they wont spoil the format.
        /// </summary>
        /// <param name="xmlNode">
        /// Node whose children are verified for maml.
        /// </param>
        private void RemoveUnsupportedNodes(XmlNode xmlNode)
        {
            // Start with the first child..
            // We want to modify only children..
            // The current node is taken care by the callee..
            XmlNode childNode = xmlNode.FirstChild;
            while (childNode != null)
            {
                // We dont want Comments..so remove..
                if (childNode.NodeType == XmlNodeType.Comment)
                {
                    XmlNode nodeToRemove = childNode;
                    childNode = childNode.NextSibling;
                    // Remove this node and its children if any..
                    xmlNode.RemoveChild(nodeToRemove);
                }
                else
                {
                    // Search children...
                    RemoveUnsupportedNodes(childNode);
                    childNode = childNode.NextSibling;
                }
            }
        }

        /// <summary>
        /// This is for adding a property into a property hashtable.
        ///
        /// As mentioned in comment of GetInsideProperties, property values stored in
        /// property hashtable is an array to begin with.
        ///
        /// The property value to be added is an mshObject whose base object can be an
        /// PSObject array itself. In that case, each PSObject in the array will be
        /// added separately into the property value array. This case can only happen when
        /// an node with maml formatting node inside is treated. The side effect of this
        /// is that the properties for outside mshObject will be lost. An example of this
        /// is that,
        /// <command>
        ///     <description attrib1="value1">
        ///         <para></para>
        ///         <list></list>
        ///         <definitionList></definitionList>
        ///     </description>
        /// </command>
        /// After the processing, PSObject corresponding to command will have an property
        /// with name "description" and a value of an PSObject array created based on
        /// maml formatting node inside "description" node. The attribute of description node
        /// "attrib1" will be lost. This seems to be OK with current practice of authoring
        /// monad command help.
        /// </summary>
        /// <param name="properties">Property hashtable.</param>
        /// <param name="name">Property name.</param>
        /// <param name="mshObject">Property value.</param>
        private static void AddProperty(Hashtable properties, string name, PSObject mshObject)
        {
            ArrayList propertyValues = (ArrayList)properties[name];

            if (propertyValues == null)
            {
                propertyValues = new ArrayList();

                properties[name] = propertyValues;
            }

            if (mshObject == null)
                return;

            if (mshObject.BaseObject is PSCustomObject || !mshObject.BaseObject.GetType().Equals(typeof(PSObject[])))
            {
                propertyValues.Add(mshObject);
                return;
            }

            PSObject[] mshObjects = (PSObject[])mshObject.BaseObject;

            for (int i = 0; i < mshObjects.Length; i++)
            {
                propertyValues.Add(mshObjects[i]);
            }

            return;
        }

        /// <summary>
        /// This is for simplifying property value array of only one element.
        ///
        /// As mentioned in comments for GetInsideProperties, this is needed
        /// to reduce an array of only one PSObject into the PSObject itself.
        ///
        /// A side effect of this function is to turn property values from
        /// ArrayList into PSObject[].
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private static Hashtable SimplifyProperties(Hashtable properties)
        {
            if (properties == null)
                return null;

            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            IDictionaryEnumerator enumerator = properties.GetEnumerator();

            while (enumerator.MoveNext())
            {
                ArrayList propertyValues = (ArrayList)enumerator.Value;

                if (propertyValues == null || propertyValues.Count == 0)
                    continue;

                if (propertyValues.Count == 1)
                {
                    if (!IsMamlFormattingPSObject((PSObject)propertyValues[0]))
                    {
                        PSObject mshObject = (PSObject)propertyValues[0];

                        // Even for strings or other basic types, they need to be contained in PSObject in case
                        // there is attributes for this object.

                        result[enumerator.Key] = mshObject;

                        continue;
                    }
                }

                result[enumerator.Key] = propertyValues.ToArray(typeof(PSObject));
            }

            return result;
        }

        /// <summary>
        /// An xmlNode is atomic if it contains no structured inside nodes.
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private static bool IsAtomic(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return false;

            if (xmlNode.ChildNodes == null)
                return true;

            if (xmlNode.ChildNodes.Count > 1)
                return false;

            if (xmlNode.ChildNodes.Count == 0)
                return true;

            if (xmlNode.ChildNodes[0].GetType().Equals(typeof(XmlText)))
                return true;

            return false;
        }

        #endregion

        #region Maml formatting

        /// <summary>
        /// Check whether an xmlNode contains childnodes which is for
        /// maml formatting.
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private static bool IncludeMamlFormatting(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return false;

            if (xmlNode.ChildNodes == null || xmlNode.ChildNodes.Count == 0)
                return false;

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (IsMamlFormattingNode(childNode))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check whether a node is for maml formatting. This include following nodes,
        ///     a. para
        ///     b. list
        ///     c. definitionList.
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private static bool IsMamlFormattingNode(XmlNode xmlNode)
        {
            if (xmlNode.LocalName.Equals("para", StringComparison.OrdinalIgnoreCase))
                return true;

            if (xmlNode.LocalName.Equals("list", StringComparison.OrdinalIgnoreCase))
                return true;

            if (xmlNode.LocalName.Equals("definitionList", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Check whether an mshObject is created from a maml formatting node.
        /// </summary>
        /// <param name="mshObject"></param>
        /// <returns></returns>
        private static bool IsMamlFormattingPSObject(PSObject mshObject)
        {
            Collection<string> typeNames = mshObject.TypeNames;

            if (typeNames == null || typeNames.Count == 0)
                return false;

            return typeNames[typeNames.Count - 1].Equals("MamlTextItem", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Convert an xmlNode containing maml formatting nodes into an PSObject array.
        ///
        /// For example, for node,
        ///    <description attribute="value">
        ///        <para>
        ///            para 1
        ///        </para>
        ///        <list>
        ///            <listItem>
        ///                <para>
        ///                    list item 1
        ///                </para>
        ///            </listItem>
        ///            <listItem>
        ///                <para>
        ///                    list item 2
        ///                </para>
        ///            </listItem>
        ///        </list>
        ///        <definitionList>
        ///            <definitionListItem>
        ///                <term>
        ///                    term 1
        ///                </term>
        ///                <definition>
        ///                    definition list item 1
        ///                </definition>
        ///            </definitionListItem>
        ///            <definitionListItem>
        ///                <term>
        ///                    term 2
        ///                </term>
        ///                <definition>
        ///                    definition list item 2
        ///                </definition>
        ///            </definitionListItem>
        ///        </definitionList>
        ///    </description>
        ///    In this case, an PSObject based on an PSObject array will be created. The inside PSObject array
        ///    will contain following items
        ///        . a MamlParaTextItem based on "para 1"
        ///        . a MamlUnorderedListItem based on "list item 1"
        ///        . a MamlUnorderedListItem based on "list item 2"
        ///        . a MamlDefinitionListItem based on "definition list item 1"
        ///        . a MamlDefinitionListItem based on "definition list item 2"
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private PSObject[] GetMamlFormattingPSObjects(XmlNode xmlNode)
        {
            ArrayList mshObjects = new ArrayList();

            int paraNodes = GetParaMamlNodeCount(xmlNode.ChildNodes);
            int count = 0;
            // Don't trim the content if this is an "introduction" node.
            bool trim = !string.Equals(xmlNode.Name, "maml:introduction", StringComparison.OrdinalIgnoreCase);
            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.LocalName.Equals("para", StringComparison.OrdinalIgnoreCase))
                {
                    ++count;
                    PSObject paraPSObject = GetParaPSObject(childNode, count != paraNodes, trim: trim);
                    if (paraPSObject != null)
                        mshObjects.Add(paraPSObject);
                    continue;
                }

                if (childNode.LocalName.Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    ArrayList listPSObjects = GetListPSObjects(childNode);

                    for (int i = 0; i < listPSObjects.Count; i++)
                    {
                        mshObjects.Add(listPSObjects[i]);
                    }

                    continue;
                }

                if (childNode.LocalName.Equals("definitionList", StringComparison.OrdinalIgnoreCase))
                {
                    ArrayList definitionListPSObjects = GetDefinitionListPSObjects(childNode);

                    for (int i = 0; i < definitionListPSObjects.Count; i++)
                    {
                        mshObjects.Add(definitionListPSObjects[i]);
                    }

                    continue;
                }

                // If we get here, there is some tags that is not supported by maml.
                WriteMamlInvalidChildNodeError(xmlNode, childNode);
            }

            return (PSObject[])mshObjects.ToArray(typeof(PSObject));
        }

        /// <summary>
        /// Gets the number of para nodes.
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static int GetParaMamlNodeCount(XmlNodeList nodes)
        {
            int i = 0;

            foreach (XmlNode childNode in nodes)
            {
                if (childNode.LocalName.Equals("para", StringComparison.OrdinalIgnoreCase))
                {
                    if (childNode.InnerText.Trim().Equals(string.Empty))
                    {
                        continue;
                    }

                    ++i;
                }
            }

            return i;
        }

        /// <summary>
        /// Write an error to helpsystem to indicate an invalid maml child node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="childNode"></param>
        private void WriteMamlInvalidChildNodeError(XmlNode node, XmlNode childNode)
        {
            ErrorRecord errorRecord = new ErrorRecord(new ParentContainsErrorRecordException("MamlInvalidChildNodeError"), "MamlInvalidChildNodeError", ErrorCategory.SyntaxError, null);
            errorRecord.ErrorDetails = new ErrorDetails(typeof(MamlNode).Assembly, "HelpErrors", "MamlInvalidChildNodeError", node.LocalName, childNode.LocalName, GetNodePath(node));
            this.Errors.Add(errorRecord);
        }

        /// <summary>
        /// Write an error to help system to indicate an invalid child node count.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="childNodeName"></param>
        /// <param name="count"></param>
        private void WriteMamlInvalidChildNodeCountError(XmlNode node, string childNodeName, int count)
        {
            ErrorRecord errorRecord = new ErrorRecord(new ParentContainsErrorRecordException("MamlInvalidChildNodeCountError"), "MamlInvalidChildNodeCountError", ErrorCategory.SyntaxError, null);
            errorRecord.ErrorDetails = new ErrorDetails(typeof(MamlNode).Assembly, "HelpErrors", "MamlInvalidChildNodeCountError", node.LocalName, childNodeName, count, GetNodePath(node));
            this.Errors.Add(errorRecord);
        }

        private static string GetNodePath(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return string.Empty;

            if (xmlNode.ParentNode == null)
                return "\\" + xmlNode.LocalName;

            return GetNodePath(xmlNode.ParentNode) + "\\" + xmlNode.LocalName + GetNodeIndex(xmlNode);
        }

        private static string GetNodeIndex(XmlNode xmlNode)
        {
            if (xmlNode == null || xmlNode.ParentNode == null)
                return string.Empty;

            int index = 0;
            int total = 0;

            foreach (XmlNode siblingNode in xmlNode.ParentNode.ChildNodes)
            {
                if (siblingNode == xmlNode)
                {
                    index = total++;
                    continue;
                }

                if (siblingNode.LocalName.Equals(xmlNode.LocalName, StringComparison.OrdinalIgnoreCase))
                {
                    total++;
                }
            }

            if (total > 1)
            {
                return "[" + index.ToString("d", CultureInfo.CurrentCulture) + "]";
            }

            return string.Empty;
        }

        /// <summary>
        /// Convert a para node into an mshObject.
        ///
        /// For example,
        ///    <para>
        ///        para text
        ///    </para>
        ///    In this case, an PSObject of type "MamlParaTextItem" will be created with following property
        ///        a. text="para text"
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <param name="newLine"></param>
        /// <param name="trim"></param>
        /// <returns></returns>
        private static PSObject GetParaPSObject(XmlNode xmlNode, bool newLine, bool trim = true)
        {
            if (xmlNode == null)
                return null;

            if (!xmlNode.LocalName.Equals("para", StringComparison.OrdinalIgnoreCase))
                return null;

            PSObject mshObject = new PSObject();

            StringBuilder sb = new StringBuilder();

            if (newLine && !xmlNode.InnerText.Trim().Equals(string.Empty))
            {
                sb.AppendLine(xmlNode.InnerText.Trim());
            }
            else
            {
                var innerText = xmlNode.InnerText;
                if (trim)
                {
                    innerText = innerText.Trim();
                }

                sb.Append(innerText);
            }

            mshObject.Properties.Add(new PSNoteProperty("Text", sb.ToString()));

            mshObject.TypeNames.Clear();
            mshObject.TypeNames.Add("MamlParaTextItem");
            mshObject.TypeNames.Add("MamlTextItem");

            return mshObject;
        }

        /// <summary>
        /// Convert a list node into an PSObject array.
        ///
        /// For example,
        ///    <list class="ordered">
        ///        <listItem>
        ///            <para>
        ///                text for list item 1
        ///            </para>
        ///        </listItem>
        ///        <listItem>
        ///            <para>
        ///                text for list item 2
        ///            </para>
        ///        </listItem>
        ///    </list>
        /// In this case, an array of PSObject, each of type "MamlOrderedListText" will be created with following
        /// two properties,
        ///        a. tag=" 1. " or " 2. "
        ///        b. text="text for list item 1" or "text for list item 2"
        /// In the case of unordered list, similar PSObject will created with type to be "MamlUnorderedListText" and tag="*"
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private ArrayList GetListPSObjects(XmlNode xmlNode)
        {
            ArrayList mshObjects = new ArrayList();

            if (xmlNode == null)
                return mshObjects;

            if (!xmlNode.LocalName.Equals("list", StringComparison.OrdinalIgnoreCase))
                return mshObjects;

            if (xmlNode.ChildNodes == null || xmlNode.ChildNodes.Count == 0)
                return mshObjects;

            bool ordered = IsOrderedList(xmlNode);
            int index = 1;

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.LocalName.Equals("listItem", StringComparison.OrdinalIgnoreCase))
                {
                    PSObject listItemPSObject = GetListItemPSObject(childNode, ordered, ref index);

                    if (listItemPSObject != null)
                        mshObjects.Add(listItemPSObject);

                    continue;
                }

                // If we get here, there is some tags that is not supported by maml.
                WriteMamlInvalidChildNodeError(xmlNode, childNode);
            }

            return mshObjects;
        }

        /// <summary>
        /// Check whether a list is ordered or not.
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private static bool IsOrderedList(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return false;

            if (xmlNode.Attributes == null || xmlNode.Attributes.Count == 0)
                return false;

            foreach (XmlNode attribute in xmlNode.Attributes)
            {
                if (attribute.Name.Equals("class", StringComparison.OrdinalIgnoreCase)
                    && attribute.Value.Equals("ordered", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Convert an listItem node into an PSObject with property "tag" and "text"
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <param name="ordered"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private PSObject GetListItemPSObject(XmlNode xmlNode, bool ordered, ref int index)
        {
            if (xmlNode == null)
                return null;

            if (!xmlNode.LocalName.Equals("listItem", StringComparison.OrdinalIgnoreCase))
                return null;

            string text = string.Empty;

            if (xmlNode.ChildNodes.Count > 1)
            {
                WriteMamlInvalidChildNodeCountError(xmlNode, "para", 1);
            }

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.LocalName.Equals("para", StringComparison.OrdinalIgnoreCase))
                {
                    text = childNode.InnerText.Trim();
                    continue;
                }

                WriteMamlInvalidChildNodeError(xmlNode, childNode);
            }

            string tag = string.Empty;
            if (ordered)
            {
                tag = index.ToString("d2", CultureInfo.CurrentCulture);
                tag += ". ";
                index++;
            }
            else
            {
                tag = "* ";
            }

            PSObject mshObject = new PSObject();

            mshObject.Properties.Add(new PSNoteProperty("Text", text));
            mshObject.Properties.Add(new PSNoteProperty("Tag", tag));

            mshObject.TypeNames.Clear();
            if (ordered)
            {
                mshObject.TypeNames.Add("MamlOrderedListTextItem");
            }
            else
            {
                mshObject.TypeNames.Add("MamlUnorderedListTextItem");
            }

            mshObject.TypeNames.Add("MamlTextItem");

            return mshObject;
        }

        /// <summary>
        /// Convert definitionList node into an array of PSObject, an for
        /// each definitionListItem node inside this node.
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private ArrayList GetDefinitionListPSObjects(XmlNode xmlNode)
        {
            ArrayList mshObjects = new ArrayList();

            if (xmlNode == null)
                return mshObjects;

            if (!xmlNode.LocalName.Equals("definitionList", StringComparison.OrdinalIgnoreCase))
                return mshObjects;

            if (xmlNode.ChildNodes == null || xmlNode.ChildNodes.Count == 0)
                return mshObjects;

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.LocalName.Equals("definitionListItem", StringComparison.OrdinalIgnoreCase))
                {
                    PSObject definitionListItemPSObject = GetDefinitionListItemPSObject(childNode);

                    if (definitionListItemPSObject != null)
                        mshObjects.Add(definitionListItemPSObject);

                    continue;
                }

                // If we get here, we found some node that is not supported.
                WriteMamlInvalidChildNodeError(xmlNode, childNode);
            }

            return mshObjects;
        }

        /// <summary>
        /// Convert an definitionListItem node into an PSObject
        ///
        /// For example
        ///        <definitionListItem>
        ///            <term>
        ///                term text
        ///            </term>
        ///            <definition>
        ///                <para>
        ///                    definition text
        ///                </para>
        ///            </definition>
        ///        </definitionListItem>
        /// In this case, an PSObject of type "definitionListText" will be created with following
        /// properties
        ///        a. term="term text"
        ///        b. definition="definition text"
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private PSObject GetDefinitionListItemPSObject(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return null;

            if (!xmlNode.LocalName.Equals("definitionListItem", StringComparison.OrdinalIgnoreCase))
                return null;

            string term = null;
            string definition = null;

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.LocalName.Equals("term", StringComparison.OrdinalIgnoreCase))
                {
                    term = childNode.InnerText.Trim();
                    continue;
                }

                if (childNode.LocalName.Equals("definition", StringComparison.OrdinalIgnoreCase))
                {
                    definition = GetDefinitionText(childNode);
                    continue;
                }

                // If we get here, we found some node that is not supported.
                WriteMamlInvalidChildNodeError(xmlNode, childNode);
            }

            if (string.IsNullOrEmpty(term))
                return null;

            PSObject mshObject = new PSObject();

            mshObject.Properties.Add(new PSNoteProperty("Term", term));
            mshObject.Properties.Add(new PSNoteProperty("Definition", definition));

            mshObject.TypeNames.Clear();
            mshObject.TypeNames.Add("MamlDefinitionTextItem");
            mshObject.TypeNames.Add("MamlTextItem");

            return mshObject;
        }

        /// <summary>
        /// Get the text for definition. The will treat some intermediate nodes like "definition" and "para"
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private string GetDefinitionText(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return null;

            if (!xmlNode.LocalName.Equals("definition", StringComparison.OrdinalIgnoreCase))
                return null;

            if (xmlNode.ChildNodes == null || xmlNode.ChildNodes.Count == 0)
                return string.Empty;

            if (xmlNode.ChildNodes.Count > 1)
            {
                WriteMamlInvalidChildNodeCountError(xmlNode, "para", 1);
            }

            string text = string.Empty;

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.LocalName.Equals("para", StringComparison.OrdinalIgnoreCase))
                {
                    text = childNode.InnerText.Trim();
                    continue;
                }

                WriteMamlInvalidChildNodeError(xmlNode, childNode);
            }

            return text;
        }

        #endregion

        #region Preformatted string processing

        /// <summary>
        /// This is for getting preformatted text from an xml document.
        ///
        /// Normally in xml document, preformatted text will be indented by
        /// a fix amount based on its position. The task of this function
        /// is to remove that fixed amount from the text.
        ///
        /// For example, in xml,
        /// <preformatted>
        ///     void function()
        ///     {
        ///         // call some other function here;
        ///     }
        /// </preformatted>
        /// we can find that the preformatted text are indented unanimously
        /// by 4 spaces because of its position in xml.
        ///
        /// After massaging in this function, the result text will be,
        ///
        /// void function
        /// {
        ///     // call some other function here;
        /// }
        ///
        /// please notice that the indention is reduced.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string GetPreformattedText(string text)
        {
            // we are assuming tabsize=4 here.
            // It is discouraged to use tab in preformatted text.

            string noTabText = text.Replace("\t", "    ");
            string[] lines = noTabText.Split('\n');
            string[] trimmedLines = TrimLines(lines);

            if (trimmedLines == null || trimmedLines.Length == 0)
                return string.Empty;

            int minIndentation = GetMinIndentation(trimmedLines);

            string[] shortedLines = new string[trimmedLines.Length];
            for (int i = 0; i < trimmedLines.Length; i++)
            {
                if (IsEmptyLine(trimmedLines[i]))
                {
                    shortedLines[i] = trimmedLines[i];
                }
                else
                {
                    shortedLines[i] = trimmedLines[i].Remove(0, minIndentation);
                }
            }

            StringBuilder result = new StringBuilder();
            for (int i = 0; i < shortedLines.Length; i++)
            {
                result.AppendLine(shortedLines[i]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Trim empty lines from the either end of an string array.
        /// </summary>
        /// <param name="lines">Lines to trim.</param>
        /// <returns>An string array with empty lines trimmed on either end.</returns>
        private static string[] TrimLines(string[] lines)
        {
            if (lines == null || lines.Length == 0)
                return null;

            int i = 0;
            for (i = 0; i < lines.Length; i++)
            {
                if (!IsEmptyLine(lines[i]))
                    break;
            }

            int start = i;

            if (start == lines.Length)
                return null;

            for (i = lines.Length - 1; i >= start; i--)
            {
                if (!IsEmptyLine(lines[i]))
                    break;
            }

            int end = i;

            string[] result = new string[end - start + 1];
            for (i = start; i <= end; i++)
            {
                result[i - start] = lines[i];
            }

            return result;
        }

        /// <summary>
        /// Get minimum indentation of a paragraph.
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private static int GetMinIndentation(string[] lines)
        {
            int minIndentation = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                if (IsEmptyLine(lines[i]))
                    continue;

                int indentation = GetIndentation(lines[i]);

                if (minIndentation < 0 || indentation < minIndentation)
                    minIndentation = indentation;
            }

            return minIndentation;
        }

        /// <summary>
        /// Get indentation of a line, i.e., number of spaces
        /// at the beginning of the line.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static int GetIndentation(string line)
        {
            if (IsEmptyLine(line))
                return 0;

            string leftTrimmedLine = line.TrimStart(' ');

            return line.Length - leftTrimmedLine.Length;
        }

        /// <summary>
        /// Test whether a line is empty.
        ///
        /// A line is empty if it contains only white spaces.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static bool IsEmptyLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return true;

            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
                return true;

            return false;
        }

        #endregion

        #region Error handling

        /// <summary>
        /// This is for tracking the set of errors happened during the parsing of
        /// maml text.
        /// </summary>
        /// <value></value>
        internal Collection<ErrorRecord> Errors { get; } = new Collection<ErrorRecord>();

        #endregion
    }
}
