// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Xml;

using Dbg = System.Diagnostics.Debug;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation.Runspaces
{
    // ReSharper disable RedundantCast
    internal class TypesPs1xmlReader
    {
        public TypesPs1xmlReader(LoadContext context)
        {
            _context = context;
            _reader = context.reader;
            _readerLineInfo = (IXmlLineInfo)context.reader;
            InitIDs();
        }

        private readonly LoadContext _context;
        private readonly XmlReader _reader;
        private readonly IXmlLineInfo _readerLineInfo;

        #region Helpers

        private string ReadElementString(string nodeName)
        {
            // This code is based on XmlReader.ReadElementString, which is not available in CoreCLR.
            string result = string.Empty;

            if (_reader.MoveToContent() != XmlNodeType.Element)
            {
                _context.AddError(_readerLineInfo.LineNumber, TypesXmlStrings.NodeShouldHaveInnerText, nodeName);
                return null;
            }

            if (!_reader.IsEmptyElement)
            {
                _reader.Read();
                while (_reader.NodeType == XmlNodeType.Text)
                {
                    result += _reader.Value;
                    if (!_reader.Read())
                    {
                        break;
                    }
                }

                if (_reader.NodeType != XmlNodeType.EndElement)
                {
                    _context.AddError(_readerLineInfo.LineNumber, TypesXmlStrings.NodeShouldHaveInnerText, nodeName);
                    return null;
                }

                result = result.Trim();
                _reader.Read();
            }
            else
            {
                _reader.Read();
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                _context.AddError(_readerLineInfo.LineNumber, TypesXmlStrings.NodeShouldHaveInnerText, nodeName);
                return null;
            }

            return result;
        }

        private bool? ReadIsHiddenAttribute()
        {
            bool? isHidden = default(bool?);

            while (_reader.MoveToNextAttribute())
            {
                if (!isHidden.HasValue && (object)_reader.LocalName == (object)_idIsHidden)
                {
                    isHidden = ToBoolean(_reader.Value);
                }

                // Unknown attributes are ignored.
            }

            return isHidden;
        }

        private void ReadIsHiddenAttributeNotSupported(string node)
        {
            if (ReadIsHiddenAttribute().HasValue)
            {
                _context.AddError(TypesXmlStrings.IsHiddenNotSupported, node, _idIsHidden);
            }
        }

        private void ReadEndElement()
        {
            while (_reader.NodeType == XmlNodeType.Whitespace)
            {
                _reader.Skip();
            }

            if (_reader.NodeType == XmlNodeType.None)
            {
                _reader.Skip();
            }
            else
            {
                _reader.ReadEndElement();
            }
        }

        private void UnknownNode(string node, string expectedNodes)
        {
            if (_reader.NodeType == XmlNodeType.Text)
            {
                _context.AddError(_readerLineInfo.LineNumber, TypesXmlStrings.NodeShouldNotHaveInnerText, node);
                _reader.Read();
            }
            else
            {
                _context.AddError(_readerLineInfo.LineNumber, TypesXmlStrings.UnknownNode, _reader.LocalName, expectedNodes);
                SkipUntilNodeEnd(_reader.LocalName);
            }
        }

        private void SkipUntilNodeEnd(string nodeName)
        {
            while (_reader.Read())
            {
                if (_reader.IsStartElement() && _reader.LocalName.Equals(nodeName))
                {
                    SkipUntilNodeEnd(nodeName);
                }
                else if ((_reader.NodeType == XmlNodeType.EndElement) && _reader.LocalName.Equals(nodeName))
                {
                    break;
                }
            }
        }

        private void NotMoreThanOnce(string node, string parent)
        {
            _context.AddError(_readerLineInfo.LineNumber, TypesXmlStrings.NotMoreThanOnceOne, node, parent);
        }

        private void NodeNotFound(int lineNumber, string node, string parent)
        {
            _context.AddError(lineNumber, TypesXmlStrings.NodeNotFoundOnce, node, parent);
        }

        private ScriptBlock GetScriptBlock(string text, int initialLine)
        {
            if (text == null)
            {
                return null;
            }

            ScriptBlock scriptBlock;
            try
            {
                scriptBlock = _context.IsProductCode
                    ? ScriptBlock.CreateDelayParsedScriptBlock(text, isProductCode: true)
                    : ScriptBlock.Create(text);
            }
            catch (ParseException pe)
            {
                _context.AddError(pe.Errors[0].Extent.StartLineNumber + initialLine - 1, pe.Errors[0].Message);
                return null;
            }

            if (scriptBlock != null)
            {
                if (_context.IsFullyTrusted)
                {
                    scriptBlock.LanguageMode = PSLanguageMode.FullLanguage;
                }
            }

            return scriptBlock;
        }

        private Type ResolveType(string typeName, int line)
        {
            Exception exception;
            var type = TypeResolver.ResolveType(typeName, out exception);
            if (exception != null)
            {
                _context.AddError(line, exception.Message);
            }
            else if (type == null)
            {
                _context.AddError(line, ParserStrings.TypeNotFound, typeName);
            }

            return type;
        }

        private bool ToBoolean(string value)
        {
            value = value.Trim();
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            _context.AddError(_readerLineInfo.LineNumber, TypesXmlStrings.ValueShouldBeTrueOrFalse, value);
            return false;
        }

        private T Converter<T>(object value, string name)
        {
            T result;

            if (!LanguagePrimitives.TryConvertTo(value, out result))
            {
                _context.AddError(TypesXmlStrings.ErrorConvertingNote, name, typeof(T));
            }

            return result;
        }

        private bool BoolConverter(object value, string name)
        {
            if (value is string s)
            {
                return ToBoolean(s);
            }

            return Converter<bool>(value, name);
        }

        private void CheckStandardNote<T>(TypeMemberData member, TypeData typeData, Action<TypeData, T> setter, Func<object, string, T> converter)
        {
            var note = member as NotePropertyData;
            if (note != null)
            {
                T value;
                if (note.Value.GetType() != typeof(T))
                {
                    value = converter(note.Value, note.Name);
                }
                else
                {
                    value = (T)note.Value;
                }

                setter(typeData, value);
            }
            else
            {
                _context.AddError(TypesXmlStrings.MemberShouldBeNote, member.Name);
            }
        }

        private static bool CheckStandardPropertySet(TypeMemberData member, TypeData typeData, Action<TypeData, PropertySetData> setter)
        {
            var propertySet = member as PropertySetData;
            if (propertySet != null)
            {
                setter(typeData, propertySet);
                return true;
            }

            return false;
        }

        #endregion Helpers

        public IEnumerable<TypeData> Read()
        {
            IEnumerable<TypeData> result = null;
            _reader.MoveToContent();
            if (_reader.NodeType == XmlNodeType.Element)
            {
                if ((object)_reader.LocalName == (object)_idTypes)
                {
                    result = Read_Types();
                }
            }

            if (result == null)
                NodeNotFound(0, _idTypes, "Document");

            return result;
        }

        private IEnumerable<TypeData> Read_Types()
        {
            ReadIsHiddenAttributeNotSupported(_idTypes);
            _reader.MoveToElement();

            if (_reader.IsEmptyElement)
            {
                _reader.Skip();
                yield break;
            }

            _reader.ReadStartElement();
            _reader.MoveToContent();
            while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
            {
                if (_reader.NodeType == XmlNodeType.Element)
                {
                    if ((object)_reader.LocalName == (object)_idType)
                    {
                        var p = Read_Type();
                        if (p != null)
                        {
                            yield return p;
                        }
                    }
                    else
                    {
                        UnknownNode(_idTypes, "Type");
                    }
                }
                else
                {
                    UnknownNode(_idTypes, "Type");
                }

                _reader.MoveToContent();
            }

            ReadEndElement();
        }

        private TypeData Read_Type()
        {
            var errorCount = _context.errors.Count;
            var lineNumber = _readerLineInfo.LineNumber;

            MemberSetData standardMembers = null;
            string name = null;
            Collection<TypeMemberData> members = null;
            Type typeConverter = null;
            Type typeAdapter = null;

            ReadIsHiddenAttributeNotSupported(_idType);
            _reader.MoveToElement();
            if (_reader.IsEmptyElement)
            {
                _reader.Skip();
            }
            else
            {
                _reader.ReadStartElement();
                _reader.MoveToContent();
                while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        if ((object)_reader.LocalName == (object)_idName)
                        {
                            if (name != null)
                            {
                                NotMoreThanOnce(_idName, _idType);
                            }

                            name = ReadElementString(_idName);
                        }
                        else if ((object)_reader.LocalName == (object)_idMembers)
                        {
                            if (members != null)
                            {
                                NotMoreThanOnce(_idMembers, _idType);
                            }

                            members = Read_Members(out standardMembers);
                        }
                        else if ((object)_reader.LocalName == (object)_idTypeConverter)
                        {
                            if (typeConverter != null)
                            {
                                NotMoreThanOnce(_idTypeConverter, _idType);
                            }

                            typeConverter = Read_TypeX(_idTypeConverter);
                        }
                        else if ((object)_reader.LocalName == (object)_idTypeAdapter)
                        {
                            if (typeAdapter != null)
                            {
                                NotMoreThanOnce(_idTypeAdapter, _idType);
                            }

                            typeAdapter = Read_TypeX(_idTypeAdapter);
                        }
                        else
                        {
                            UnknownNode(_idType, "Name,Members,TypeConverter,TypeAdapter");
                        }
                    }
                    else
                    {
                        UnknownNode(_idType, "Name,Members,TypeConverter,TypeAdapter");
                    }

                    _reader.MoveToContent();
                }

                ReadEndElement();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                NodeNotFound(lineNumber, _idName, _idType);
            }
            else if (members == null && typeConverter == null && typeAdapter == null)
            {
                _context.AddError(name, lineNumber, TypesXmlStrings.TypeNodeShouldHaveMembersOrTypeConverters);
            }

            if (_context.errors.Count != errorCount)
            {
                return null;
            }

            var typeData = new TypeData(name, true);
            if (members != null)
            {
                foreach (var m in members)
                {
                    typeData.Members.Add(m.Name, m);
                }
            }

            typeData.TypeAdapter = typeAdapter;
            typeData.TypeConverter = typeConverter;

            if (standardMembers != null)
            {
                foreach (var m in standardMembers.Members)
                {
                    if (m.Name.Equals(TypeTable.DefaultDisplayProperty, StringComparison.OrdinalIgnoreCase))
                    {
                        CheckStandardNote(m, typeData, static (t, v) => t.DefaultDisplayProperty = v, Converter<string>);
                    }
                    else if (m.Name.Equals(TypeTable.DefaultDisplayPropertySet, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!CheckStandardPropertySet(m, typeData, (t, p) => t.DefaultDisplayPropertySet = p))
                        {
                            _context.AddError(TypesXmlStrings.MemberShouldHaveType, TypeTable.DefaultDisplayPropertySet, _idPropertySet);
                        }
                    }
                    else if (m.Name.Equals(TypeTable.DefaultKeyPropertySet, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!CheckStandardPropertySet(m, typeData, (t, p) => t.DefaultKeyPropertySet = p))
                        {
                            _context.AddError(TypesXmlStrings.MemberShouldHaveType, TypeTable.DefaultKeyPropertySet, _idPropertySet);
                        }
                    }
                    else if (m.Name.Equals(TypeTable.SerializationMethodNode, StringComparison.OrdinalIgnoreCase))
                    {
                        CheckStandardNote(m, typeData, static (t, v) => t.SerializationMethod = v, Converter<string>);
                    }
                    else if (m.Name.Equals(TypeTable.SerializationDepth, StringComparison.OrdinalIgnoreCase))
                    {
                        CheckStandardNote(m, typeData, static (t, v) => t.SerializationDepth = v, Converter<uint>);
                    }
                    else if (m.Name.Equals(TypeTable.StringSerializationSource, StringComparison.OrdinalIgnoreCase))
                    {
                        var aliasData = m as AliasPropertyData;
                        if (aliasData != null)
                        {
                            typeData.StringSerializationSource = aliasData.ReferencedMemberName;
                        }
                        else
                        {
                            typeData.StringSerializationSourceProperty = m;
                        }
                    }
                    else if (m.Name.Equals(TypeTable.PropertySerializationSet, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!CheckStandardPropertySet(m, typeData, (t, p) => t.PropertySerializationSet = p))
                        {
                            _context.AddError(TypesXmlStrings.MemberShouldHaveType, TypeTable.PropertySerializationSet, _idPropertySet);
                        }
                    }
                    else if (m.Name.Equals(TypeTable.InheritPropertySerializationSet, StringComparison.OrdinalIgnoreCase))
                    {
                        CheckStandardNote(m, typeData, static (t, v) => t.InheritPropertySerializationSet = v, BoolConverter);
                    }
                    else if (m.Name.Equals(TypeTable.TargetTypeForDeserialization, StringComparison.OrdinalIgnoreCase))
                    {
                        CheckStandardNote(m, typeData, static (t, v) => t.TargetTypeForDeserialization = v, Converter<Type>);
                    }
                    else
                    {
                        _context.AddError(TypesXmlStrings.NotAStandardMember, m.Name);
                    }
                }
            }

            return typeData;
        }

        private Type Read_TypeX(string elementName)
        {
            var errorCount = _context.errors.Count;
            var lineNumber = _readerLineInfo.LineNumber;
            int typeLineNumber = 0;

            string typeName = null;

            ReadIsHiddenAttributeNotSupported(elementName);
            _reader.MoveToElement();
            if (_reader.IsEmptyElement)
            {
                _reader.Skip();
            }
            else
            {
                _reader.ReadStartElement();
                _reader.MoveToContent();
                while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        if ((object)_reader.LocalName == (object)_idTypeName)
                        {
                            if (typeName != null)
                            {
                                NotMoreThanOnce(_idTypeName, elementName);
                            }

                            typeLineNumber = _readerLineInfo.LineNumber;
                            typeName = ReadElementString(_idTypeName);
                        }
                        else
                        {
                            UnknownNode(elementName, "TypeName");
                        }
                    }
                    else
                    {
                        UnknownNode(elementName, "TypeName");
                    }

                    _reader.MoveToContent();
                }

                ReadEndElement();
            }

            if (string.IsNullOrWhiteSpace(typeName))
            {
                NodeNotFound(lineNumber, _idTypeName, elementName);
            }

            if (_context.errors.Count != errorCount)
            {
                return null;
            }

            return ResolveType(typeName, typeLineNumber);
        }

        private Collection<TypeMemberData> Read_Members(out MemberSetData standardMembers)
        {
            var errorCount = _context.errors.Count;

            standardMembers = null;
            var members = new Collection<TypeMemberData>();

            ReadIsHiddenAttributeNotSupported(_idMembers);
            _reader.MoveToElement();
            if (_reader.IsEmptyElement)
            {
                _reader.Skip();
            }
            else
            {
                _reader.ReadStartElement();
                _reader.MoveToContent();
                while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        if ((object)_reader.LocalName == (object)_idNoteProperty)
                        {
                            var p = Read_NoteProperty();
                            if (p != null)
                            {
                                members.Add(p);
                            }
                        }
                        else if ((object)_reader.LocalName == (object)_idAliasProperty)
                        {
                            var p = Read_AliasProperty();
                            if (p != null)
                            {
                                members.Add(p);
                            }
                        }
                        else if ((object)_reader.LocalName == (object)_idScriptProperty)
                        {
                            var p = Read_ScriptProperty();
                            if (p != null)
                            {
                                members.Add(p);
                            }
                        }
                        else if ((object)_reader.LocalName == (object)_idCodeProperty)
                        {
                            var p = Read_CodeProperty();
                            if (p != null)
                            {
                                members.Add(p);
                            }
                        }
                        else if ((object)_reader.LocalName == (object)_idScriptMethod)
                        {
                            var p = Read_ScriptMethod();
                            if (p != null)
                            {
                                members.Add(p);
                            }
                        }
                        else if ((object)_reader.LocalName == (object)_idCodeMethod)
                        {
                            var p = Read_CodeMethod();
                            if (p != null)
                            {
                                members.Add(p);
                            }
                        }
                        else if ((object)_reader.LocalName == (object)_idPropertySet)
                        {
                            var p = Read_PropertySet();
                            if (p != null)
                            {
                                members.Add(p);
                            }
                        }
                        else if ((object)_reader.LocalName == (object)_idMemberSet)
                        {
                            var p = Read_MemberSet();
                            if (p != null)
                            {
                                if (p.Name.Equals(TypeTable.PSStandardMembers, StringComparison.OrdinalIgnoreCase))
                                {
                                    standardMembers = p;
                                }
                                else
                                {
                                    members.Add(p);
                                }
                            }
                        }
                        else
                        {
                            UnknownNode(_idMembers, "NoteProperty,AliasProperty,ScriptProperty,CodeProperty,ScriptMethod,CodeMethod,PropertySet,MemberSet");
                        }
                    }
                    else
                    {
                        UnknownNode(_idMembers, "NoteProperty,AliasProperty,ScriptProperty,CodeProperty,ScriptMethod,CodeMethod,PropertySet,MemberSet");
                    }

                    _reader.MoveToContent();
                }

                ReadEndElement();
            }

            return _context.errors.Count == errorCount ? members : null;
        }

        private MemberSetData Read_MemberSet()
        {
            var errorCount = _context.errors.Count;
            var lineNumber = _readerLineInfo.LineNumber;

            string name = null;
            Collection<TypeMemberData> members = null;
            bool? inheritMembers = default(bool?);

            bool? isHidden = ReadIsHiddenAttribute();
            _reader.MoveToElement();
            if (_reader.IsEmptyElement)
            {
                _reader.Skip();
            }
            else
            {
                _reader.ReadStartElement();
                _reader.MoveToContent();
                while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        if ((object)_reader.LocalName == (object)_idName)
                        {
                            if (name != null)
                            {
                                NotMoreThanOnce(_idName, _idMemberSet);
                            }

                            name = ReadElementString(_idName);
                        }
                        else if ((object)_reader.LocalName == (object)_idInheritMembers)
                        {
                            if (inheritMembers.HasValue)
                            {
                                NotMoreThanOnce(_idInheritMembers, _idMemberSet);
                            }

                            inheritMembers = ToBoolean(ReadElementString(_idMemberSet));
                        }
                        else if ((object)_reader.LocalName == (object)_idMembers)
                        {
                            if (members != null)
                            {
                                NotMoreThanOnce(_idMembers, _idMemberSet);
                            }

                            MemberSetData standardMembers;
                            members = Read_Members(out standardMembers);
                            if (standardMembers != null)
                            {
                                // Somewhat pointless - but if we see PSStandardMembers inside a memberset, it's
                                // really just another ordinary member.
                                members.Add(standardMembers);
                            }
                        }
                        else
                        {
                            UnknownNode(_idMemberSet, "Name,InheritMembers,Members");
                        }
                    }
                    else
                    {
                        UnknownNode(_idMemberSet, "Name,InheritMembers,Members");
                    }

                    _reader.MoveToContent();
                }

                ReadEndElement();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                NodeNotFound(lineNumber, _idName, _idMemberSet);
            }

            // Somewhat pointlessly (backcompat), we allow a missing Member node
            members ??= new Collection<TypeMemberData>();

            if (_context.errors.Count != errorCount)
            {
                return null;
            }

            var result = new MemberSetData(name, members)
            {
                IsHidden = isHidden.GetValueOrDefault()
            };

            if (inheritMembers.HasValue)
            {
                result.InheritMembers = inheritMembers.Value;
            }

            return result;
        }

        private PropertySetData Read_PropertySet()
        {
            var errorCount = _context.errors.Count;
            var lineNumber = _readerLineInfo.LineNumber;

            string name = null;
            List<string> referencedProperties = null;

            bool? isHidden = ReadIsHiddenAttribute();
            _reader.MoveToElement();
            if (_reader.IsEmptyElement)
            {
                _reader.Skip();
            }
            else
            {
                _reader.ReadStartElement();
                _reader.MoveToContent();
                while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        if ((object)_reader.LocalName == (object)_idName)
                        {
                            if (name != null)
                            {
                                NotMoreThanOnce(_idName, _idPropertySet);
                            }

                            name = ReadElementString(_idName);
                        }
                        else if ((object)_reader.LocalName == (object)_idReferencedProperties)
                        {
                            if (_reader.IsEmptyElement)
                            {
                                _reader.Skip();
                            }
                            else
                            {
                                _reader.ReadStartElement();
                                _reader.MoveToContent();
                                while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
                                {
                                    if (_reader.NodeType == XmlNodeType.Element)
                                    {
                                        if ((object)_reader.LocalName == (object)_idName)
                                        {
                                            referencedProperties ??= new List<string>(8);

                                            referencedProperties.Add(ReadElementString(_idName));
                                        }
                                        else
                                        {
                                            UnknownNode(_idPropertySet, "Name");
                                        }
                                    }
                                    else
                                    {
                                        UnknownNode(_idPropertySet, "Name");
                                    }

                                    _reader.MoveToContent();
                                }

                                ReadEndElement();
                            }
                        }
                        else
                        {
                            UnknownNode(_idPropertySet, "Name,ReferencedProperties");
                        }
                    }
                    else
                    {
                        UnknownNode(_idPropertySet, "Name,ReferencedProperties");
                    }

                    _reader.MoveToContent();
                }

                ReadEndElement();
            }

            if (string.IsNullOrWhiteSpace(name))
                NodeNotFound(lineNumber, _idName, _idPropertySet);
            if (referencedProperties == null)
                NodeNotFound(lineNumber, _idReferencedProperties, _idPropertySet);

            if (_context.errors.Count != errorCount)
            {
                return null;
            }

            return new PropertySetData(referencedProperties)
            {
                Name = name,
                IsHidden = isHidden.GetValueOrDefault()
            };
        }

        private CodeMethodData Read_CodeMethod()
        {
            var errorCount = _context.errors.Count;
            var lineNumber = _readerLineInfo.LineNumber;
            var methodLineNumber = 0;

            string name = null;
            MethodInfo codeReference = null;

            ReadIsHiddenAttributeNotSupported(_idCodeMethod);
            _reader.MoveToElement();
            if (_reader.IsEmptyElement)
            {
                _reader.Skip();
            }
            else
            {
                _reader.ReadStartElement();
                _reader.MoveToContent();
                while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        if ((object)_reader.LocalName == (object)_idName)
                        {
                            if (name != null)
                            {
                                NotMoreThanOnce(_idName, _idCodeMethod);
                            }

                            name = ReadElementString(_idName);
                        }
                        else if ((object)_reader.LocalName == (object)_idCodeReference)
                        {
                            if (codeReference != null)
                            {
                                NotMoreThanOnce(_idCodeReference, _idCodeMethod);
                            }

                            methodLineNumber = _readerLineInfo.LineNumber;
                            codeReference = Read_CodeReference();
                            if (codeReference == null)
                                _context.AddError(methodLineNumber, ExtendedTypeSystem.CodeMethodMethodFormat);
                        }
                        else
                        {
                            UnknownNode(_idCodeMethod, "Name,CodeReference");
                        }
                    }
                    else
                    {
                        UnknownNode(_idCodeMethod, "Name,CodeReference");
                    }

                    _reader.MoveToContent();
                }

                ReadEndElement();
            }

            if (string.IsNullOrWhiteSpace(name))
                NodeNotFound(lineNumber, _idName, _idCodeMethod);
            if (codeReference == null && methodLineNumber == 0)
                NodeNotFound(lineNumber, _idCodeReference, _idCodeMethod);
            if (codeReference != null && !PSCodeMethod.CheckMethodInfo(codeReference))
                _context.AddError(methodLineNumber, ExtendedTypeSystem.CodeMethodMethodFormat);

            if (_context.errors.Count != errorCount)
            {
                return null;
            }

            return new CodeMethodData(name, codeReference);
        }

        private MethodInfo Read_CodeReference()
        {
            var errorCount = _context.errors.Count;
            var lineNumber = _readerLineInfo.LineNumber;
            int typeLineNumber = 0;
            int methodLineNumber = 0;

            string typeName = null;
            string methodName = null;

            ReadIsHiddenAttributeNotSupported(_idCodeReference);
            _reader.MoveToElement();
            if (_reader.IsEmptyElement)
            {
                _reader.Skip();
            }
            else
            {
                _reader.ReadStartElement();
                _reader.MoveToContent();
                while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        if ((object)_reader.LocalName == (object)_idTypeName)
                        {
                            if (typeName != null)
                            {
                                NotMoreThanOnce(_idTypeName, _idCodeReference);
                            }

                            typeLineNumber = _readerLineInfo.LineNumber;
                            typeName = ReadElementString(_idTypeName);
                        }
                        else if ((object)_reader.LocalName == (object)_idMethodName)
                        {
                            if (methodName != null)
                            {
                                NotMoreThanOnce(_idMethodName, _idCodeReference);
                            }

                            methodLineNumber = _readerLineInfo.LineNumber;
                            methodName = ReadElementString(_idMethodName);
                        }
                        else
                        {
                            UnknownNode(_idCodeReference, "TypeName,MethodName");
                        }
                    }
                    else
                    {
                        UnknownNode(_idCodeReference, "TypeName,MethodName");
                    }

                    _reader.MoveToContent();
                }

                ReadEndElement();
            }

            if (string.IsNullOrWhiteSpace(typeName))
                NodeNotFound(lineNumber, _idTypeName, _idCodeReference);
            if (string.IsNullOrWhiteSpace(methodName))
                NodeNotFound(lineNumber, _idMethodName, _idCodeReference);

            if (_context.errors.Count != errorCount)
            {
                return null;
            }

            MethodInfo member = null;
            var type = ResolveType(typeName, typeLineNumber);
            if (type != null)
            {
                try
                {
                    member = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);
                }
                catch (AmbiguousMatchException e)
                {
                    _context.AddError(methodLineNumber, e.Message);
                }
            }

            return member;
        }

        private ScriptMethodData Read_ScriptMethod()
        {
            var errorCount = _context.errors.Count;
            var lineNumber = _readerLineInfo.LineNumber;

            string name = null;
            string script = null;
            int scriptLineNumber = 0;

            ReadIsHiddenAttributeNotSupported(_idScriptMethod);
            _reader.MoveToElement();
            if (_reader.IsEmptyElement)
            {
                _reader.Skip();
            }
            else
            {
                _reader.ReadStartElement();
                _reader.MoveToContent();
                while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        if ((object)_reader.LocalName == (object)_idName)
                        {
                            if (name != null)
                            {
                                NotMoreThanOnce(_idName, _idScriptMethod);
                            }

                            name = ReadElementString(_idName);
                        }
                        else if ((object)_reader.LocalName == (object)_idScript)
                        {
                            if (script != null)
                            {
                                NotMoreThanOnce(_idScript, _idScriptMethod);
                            }

                            scriptLineNumber = _readerLineInfo.LineNumber;
                            script = ReadElementString(_idScript);
                        }
                        else
                        {
                            UnknownNode(_idScriptMethod, "Name,Script");
                        }
                    }
                    else
                    {
                        UnknownNode(_idScriptMethod, "Name,Script");
                    }

                    _reader.MoveToContent();
                }

                ReadEndElement();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                NodeNotFound(lineNumber, _idName, _idScriptMethod);
            }

            if (string.IsNullOrWhiteSpace(script))
            {
                NodeNotFound(lineNumber, _idScript, _idScriptMethod);
            }

            ScriptBlock scriptBlock = GetScriptBlock(script, scriptLineNumber);

            if (_context.errors.Count != errorCount)
            {
                return null;
            }

            return new ScriptMethodData(name, scriptBlock);
        }

        private CodePropertyData Read_CodeProperty()
        {
            var errorCount = _context.errors.Count;
            var lineNumber = _readerLineInfo.LineNumber;
            var getterLineNumber = 0;
            var setterLineNumber = 0;

            string name = null;
            MethodInfo getter = null;
            MethodInfo setter = null;

            var isHidden = ReadIsHiddenAttribute();
            _reader.MoveToElement();
            if (_reader.IsEmptyElement)
            {
                _reader.Skip();
            }
            else
            {
                _reader.ReadStartElement();
                _reader.MoveToContent();
                while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        if ((object)_reader.LocalName == (object)_idName)
                        {
                            if (name != null)
                            {
                                NotMoreThanOnce(_idName, _idCodeProperty);
                            }

                            name = ReadElementString(_idName);
                        }
                        else if ((object)_reader.LocalName == (object)_idGetCodeReference)
                        {
                            if (getter != null)
                            {
                                NotMoreThanOnce(_idGetCodeReference, _idCodeProperty);
                            }

                            getterLineNumber = _readerLineInfo.LineNumber;
                            getter = Read_CodeReference();
                            if (getter == null)
                            {
                                _context.AddError(getterLineNumber, ExtendedTypeSystem.CodePropertyGetterAndSetterNull);
                            }
                        }
                        else if ((object)_reader.LocalName == (object)_idSetCodeReference)
                        {
                            if (setter != null)
                            {
                                NotMoreThanOnce(_idSetCodeReference, _idCodeProperty);
                            }

                            setterLineNumber = _readerLineInfo.LineNumber;
                            setter = Read_CodeReference();
                            if (setter == null)
                            {
                                _context.AddError(setterLineNumber, ExtendedTypeSystem.CodePropertyGetterAndSetterNull);
                            }
                        }
                        else
                        {
                            UnknownNode(_idCodeProperty, "Name,GetCodeReference,SetCodeReference");
                        }
                    }
                    else
                    {
                        UnknownNode(_idCodeProperty, "Name,GetCodeReference,SetCodeReference");
                    }

                    _reader.MoveToContent();
                }

                ReadEndElement();
            }

            if (string.IsNullOrEmpty(name))
            {
                NodeNotFound(lineNumber, _idName, _idCodeProperty);
            }

            if (getter == null && setter == null && getterLineNumber == 0 && setterLineNumber == 0)
            {
                _context.AddError(lineNumber, TypesXmlStrings.CodePropertyShouldHaveGetterOrSetter);
            }

            if (getter != null && !PSCodeProperty.CheckGetterMethodInfo(getter))
            {
                _context.AddError(getterLineNumber, ExtendedTypeSystem.CodePropertyGetterFormat);
            }

            if (setter != null && !PSCodeProperty.CheckSetterMethodInfo(setter, getter))
            {
                _context.AddError(setterLineNumber, ExtendedTypeSystem.CodePropertySetterFormat);
            }

            if (_context.errors.Count != errorCount)
            {
                return null;
            }

            return new CodePropertyData(name, getter, setter)
            {
                IsHidden = isHidden.GetValueOrDefault()
            };
        }

        private ScriptPropertyData Read_ScriptProperty()
        {
            var errorCount = _context.errors.Count;
            var lineNumber = _readerLineInfo.LineNumber;
            var getterInitialLine = 0;
            var setterInitialLine = 0;

            string name = null;
            string getScriptBlock = null;
            string setScriptBlock = null;

            var isHidden = ReadIsHiddenAttribute();
            _reader.MoveToElement();
            if (_reader.IsEmptyElement)
            {
                _reader.Skip();
            }
            else
            {
                _reader.ReadStartElement();
                _reader.MoveToContent();
                while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        if ((object)_reader.LocalName == (object)_idName)
                        {
                            if (name != null)
                            {
                                NotMoreThanOnce(_idName, _idScriptProperty);
                            }

                            name = ReadElementString(_idName);
                        }
                        else if ((object)_reader.LocalName == (object)_idGetScriptBlock)
                        {
                            if (getScriptBlock != null)
                            {
                                NotMoreThanOnce(_idGetScriptBlock, _idScriptProperty);
                            }

                            getterInitialLine = _readerLineInfo.LineNumber;
                            getScriptBlock = ReadElementString(_idGetScriptBlock);
                        }
                        else if ((object)_reader.LocalName == (object)_idSetScriptBlock)
                        {
                            if (setScriptBlock != null)
                            {
                                NotMoreThanOnce(_idSetScriptBlock, _idScriptProperty);
                            }

                            setterInitialLine = _readerLineInfo.LineNumber;
                            setScriptBlock = ReadElementString(_idSetScriptBlock);
                        }
                        else
                        {
                            UnknownNode(_idScriptProperty, "Name,GetScriptBlock,SetScriptBlock");
                        }
                    }
                    else
                    {
                        UnknownNode(_idScriptProperty, "Name,GetScriptBlock,SetScriptBlock");
                    }

                    _reader.MoveToContent();
                }

                ReadEndElement();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                NodeNotFound(lineNumber, _idName, _idScriptProperty);
            }

            if (string.IsNullOrWhiteSpace(getScriptBlock) && string.IsNullOrWhiteSpace(setScriptBlock))
            {
                _context.AddError(lineNumber, TypesXmlStrings.ScriptPropertyShouldHaveGetterOrSetter);
            }

            ScriptBlock getter = GetScriptBlock(getScriptBlock, getterInitialLine);
            ScriptBlock setter = GetScriptBlock(setScriptBlock, setterInitialLine);

            if (_context.errors.Count != errorCount)
            {
                return null;
            }

            return new ScriptPropertyData(name, getter, setter)
            {
                IsHidden = isHidden.GetValueOrDefault()
            };
        }

        private AliasPropertyData Read_AliasProperty()
        {
            var errorCount = _context.errors.Count;
            var lineNumber = _readerLineInfo.LineNumber;
            var typeLineNumber = 0;

            string name = null;
            string referencedMemberName = null;
            string typeName = null;

            var isHidden = ReadIsHiddenAttribute();
            _reader.MoveToElement();
            if (_reader.IsEmptyElement)
            {
                _reader.Skip();
            }
            else
            {
                _reader.ReadStartElement();
                _reader.MoveToContent();
                while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        if ((object)_reader.LocalName == (object)_idName)
                        {
                            if (name != null)
                            {
                                NotMoreThanOnce(_idName, _idAliasProperty);
                            }

                            name = ReadElementString(_idAliasProperty);
                        }
                        else if ((object)_reader.LocalName == (object)_idReferencedMemberName)
                        {
                            if (referencedMemberName != null)
                            {
                                NotMoreThanOnce(_idReferencedMemberName, _idAliasProperty);
                            }

                            referencedMemberName = ReadElementString(_idReferencedMemberName);
                        }
                        else if ((object)_reader.LocalName == (object)_idTypeName)
                        {
                            if (typeName != null)
                            {
                                NotMoreThanOnce(_idTypeName, _idAliasProperty);
                            }

                            typeLineNumber = _readerLineInfo.LineNumber;
                            typeName = ReadElementString(_idTypeName);
                        }
                        else
                        {
                            UnknownNode(_idAliasProperty, "Name,ReferencedMemberName,TypeName");
                        }
                    }
                    else
                    {
                        UnknownNode(_idAliasProperty, "Name,ReferencedMemberName,TypeName");
                    }

                    _reader.MoveToContent();
                }

                ReadEndElement();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                NodeNotFound(lineNumber, _idName, _idAliasProperty);
            }

            if (string.IsNullOrWhiteSpace(referencedMemberName))
            {
                NodeNotFound(lineNumber, _idReferencedMemberName, _idAliasProperty);
            }

            Type convertToType = (typeName != null) ? ResolveType(typeName, typeLineNumber) : null;

            if (_context.errors.Count != errorCount)
            {
                return null;
            }

            return new AliasPropertyData(name, referencedMemberName, convertToType)
            {
                IsHidden = isHidden.GetValueOrDefault()
            };
        }

        private NotePropertyData Read_NoteProperty()
        {
            var errorCount = _context.errors.Count;
            var lineNumber = _readerLineInfo.LineNumber;
            int typeLineNumber = 0;

            string name = null;
            string valueAsString = null;
            string typeName = null;

            var isHidden = ReadIsHiddenAttribute();
            _reader.MoveToElement();
            if (_reader.IsEmptyElement)
            {
                _reader.Skip();
            }
            else
            {
                _reader.ReadStartElement();
                _reader.MoveToContent();
                while (_reader.NodeType != XmlNodeType.EndElement && _reader.NodeType != XmlNodeType.None)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        if ((object)_reader.LocalName == (object)_idName)
                        {
                            if (name != null)
                            {
                                NotMoreThanOnce(_idName, _idNoteProperty);
                            }

                            name = ReadElementString(_idName);
                        }
                        else if ((object)_reader.LocalName == (object)_idValue)
                        {
                            if (valueAsString != null)
                            {
                                NotMoreThanOnce(_idValue, _idNoteProperty);
                            }

                            valueAsString = ReadElementString(_idValue);
                        }
                        else if ((object)_reader.LocalName == (object)_idTypeName)
                        {
                            if (typeName != null)
                            {
                                NotMoreThanOnce(_idTypeName, _idNoteProperty);
                            }

                            typeLineNumber = _readerLineInfo.LineNumber;
                            typeName = ReadElementString(_idTypeName);
                        }
                        else
                        {
                            UnknownNode(_idNoteProperty, "Name,Value,TypeName");
                        }
                    }
                    else
                    {
                        UnknownNode(_idNoteProperty, "Name,Value,TypeName");
                    }

                    _reader.MoveToContent();
                }

                ReadEndElement();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                NodeNotFound(lineNumber, _idName, _idNoteProperty);
            }

            if (string.IsNullOrWhiteSpace(valueAsString))
            {
                NodeNotFound(lineNumber, _idValue, _idNoteProperty);
            }

            object value = valueAsString;
            Type convertToType = (typeName != null) ? ResolveType(typeName, typeLineNumber) : null;
            if (convertToType != null)
            {
                try
                {
                    value = LanguagePrimitives.ConvertTo(value, convertToType, CultureInfo.InvariantCulture);
                }
                catch (PSInvalidCastException e)
                {
                    _context.AddError(typeLineNumber, e.Message);
                }
            }

            if (_context.errors.Count != errorCount)
            {
                return null;
            }

            return new NotePropertyData(name, value)
            {
                IsHidden = isHidden.GetValueOrDefault()
            };
        }

        private string _idTypeName;
        private string _idSetCodeReference;
        private string _idScriptMethod;
        private string _idValue;
        private string _idScriptProperty;
        private string _idIsHidden;
        private string _idScript;
        private string _idCodeMethod;
        private string _idMemberSet;
        private string _idCodeProperty;
        private string _idMembers;
        private string _idName;
        private string _idGetCodeReference;
        private string _idMethodName;
        private string _idInheritMembers;
        private string _idNoteProperty;
        private string _idTypeAdapter;
        private string _idSetScriptBlock;
        private string _idPropertySet;
        private string _idTypeConverter;
        private string _idAliasProperty;
        private string _idType;
        private string _idCodeReference;
        private string _idTypes;
        private string _idReferencedProperties;
        private string _idGetScriptBlock;
        private string _idReferencedMemberName;

        protected void InitIDs()
        {
            var xmlNameTable = _reader.NameTable;
            _idTypeName = xmlNameTable.Add("TypeName");
            _idSetCodeReference = xmlNameTable.Add("SetCodeReference");
            _idScriptMethod = xmlNameTable.Add("ScriptMethod");
            _idValue = xmlNameTable.Add("Value");
            _idScriptProperty = xmlNameTable.Add("ScriptProperty");
            _idIsHidden = xmlNameTable.Add("IsHidden");
            _idScript = xmlNameTable.Add("Script");
            _idCodeMethod = xmlNameTable.Add("CodeMethod");
            _idMemberSet = xmlNameTable.Add("MemberSet");
            _idCodeProperty = xmlNameTable.Add("CodeProperty");
            _idMembers = xmlNameTable.Add("Members");
            _idName = xmlNameTable.Add("Name");
            _idGetCodeReference = xmlNameTable.Add("GetCodeReference");
            _idMethodName = xmlNameTable.Add("MethodName");
            _idInheritMembers = xmlNameTable.Add("InheritMembers");
            _idNoteProperty = xmlNameTable.Add("NoteProperty");
            _idTypeAdapter = xmlNameTable.Add("TypeAdapter");
            _idSetScriptBlock = xmlNameTable.Add("SetScriptBlock");
            _idPropertySet = xmlNameTable.Add("PropertySet");
            _idTypeConverter = xmlNameTable.Add("TypeConverter");
            _idAliasProperty = xmlNameTable.Add("AliasProperty");
            _idType = xmlNameTable.Add("Type");
            _idCodeReference = xmlNameTable.Add("CodeReference");
            _idTypes = xmlNameTable.Add("Types");
            _idReferencedProperties = xmlNameTable.Add("ReferencedProperties");
            _idGetScriptBlock = xmlNameTable.Add("GetScriptBlock");
            _idReferencedMemberName = xmlNameTable.Add("ReferencedMemberName");
        }
    }
    // ReSharper restore RedundantCast

    /// <summary>
    /// Internal class to provide a Hashtable key out of a Collection of strings
    /// preserving the evaluation of the key.
    /// </summary>
    internal class ConsolidatedString : Collection<string>
    {
        protected override void SetItem(int index, string item)
        {
            if (string.IsNullOrEmpty(item))
            {
                throw PSTraceSource.NewArgumentException(nameof(item));
            }

            base.SetItem(index, item);
            UpdateKey();
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            UpdateKey();
        }

        protected override void InsertItem(int index, string item)
        {
            if (string.IsNullOrEmpty(item))
            {
                throw PSTraceSource.NewArgumentException(nameof(item));
            }

            base.InsertItem(index, item);
            UpdateKey();
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            UpdateKey();
        }

        internal string Key { get; private set; }

        internal bool IsReadOnly { get { return ((ICollection<string>)this).IsReadOnly; } }

        private void UpdateKey()
        {
            Key = string.Join("@@@", this);
        }

        /// <summary>
        /// Copy constructor (deep copy)
        /// </summary>
        /// <param name="other"></param>
        public ConsolidatedString(ConsolidatedString other)
            : base(new List<string>(other))
        {
            this.Key = other.Key;
        }

        /// <summary>
        /// Construct an optionally readonly list of strings.
        /// </summary>
        internal ConsolidatedString(IEnumerable<string> strings, bool interned)
            : base(interned ? (IList<string>)new ReadOnlyCollection<string>(strings.ToList()) : strings.ToList())
        {
            UpdateKey();
        }

        public ConsolidatedString(IEnumerable<string> strings)
            : base(strings.ToList())
        {
            for (int i = 0; i < this.Count; i++)
            {
                string str = this[i];
                if (string.IsNullOrEmpty(str))
                {
                    throw PSTraceSource.NewArgumentException(nameof(strings));
                }
            }

            UpdateKey();
        }

        internal static readonly ConsolidatedString Empty = new ConsolidatedString(Array.Empty<string>());

        internal static readonly IEqualityComparer<ConsolidatedString> EqualityComparer = new ConsolidatedStringEqualityComparer();

        private sealed class ConsolidatedStringEqualityComparer : IEqualityComparer<ConsolidatedString>
        {
            bool IEqualityComparer<ConsolidatedString>.Equals(ConsolidatedString x, ConsolidatedString y)
            {
                return x.Key.Equals(y.Key, StringComparison.Ordinal);
            }

            int IEqualityComparer<ConsolidatedString>.GetHashCode(ConsolidatedString obj)
            {
                return obj.Key.GetHashCode();
            }
        }
    }

    internal class LoadContext
    {
        internal XmlReader reader;
        internal ConcurrentBag<string> errors;
        internal string fileName;
        internal string PSSnapinName;
        internal bool isFullyTrusted;

        internal LoadContext(string PSSnapinName, string fileName, ConcurrentBag<string> errors)
        {
            this.reader = null;
            this.fileName = fileName;
            this.errors = errors;
            this.PSSnapinName = PSSnapinName;
            this.isFullyTrusted = false;
        }

        internal bool IsFullyTrusted
        {
            get { return isFullyTrusted; }

            set { isFullyTrusted = value; }
        }

        internal bool IsProductCode { get; set; }

        internal void AddError(string resourceString, params object[] formatArguments)
        {
            string errorMsg = StringUtil.Format(resourceString, formatArguments);
            string errorLine = StringUtil.Format(TypesXmlStrings.FileError, this.PSSnapinName, this.fileName, errorMsg);
            this.errors.Add(errorLine);
        }

        internal void AddError(int errorLineNumber, string resourceString, params object[] formatArguments)
        {
            string errorMsg = StringUtil.Format(resourceString, formatArguments);
            string errorLine = StringUtil.Format(TypesXmlStrings.FileLineError, this.PSSnapinName, this.fileName, errorLineNumber, errorMsg);
            this.errors.Add(errorLine);
        }

        internal void AddError(string typeName, int errorLineNumber, string resourceString, params object[] formatArguments)
        {
            string errorMsg = StringUtil.Format(resourceString, formatArguments);
            string errorLine = StringUtil.Format(TypesXmlStrings.FileLineTypeError, this.PSSnapinName, this.fileName, errorLineNumber, typeName, errorMsg);
            this.errors.Add(errorLine);
        }
    }

    /// <summary>
    /// This exception is used by TypeTable constructor to indicate errors
    /// occurred during construction time.
    /// </summary>
    public class TypeTableLoadException : RuntimeException
    {
        private readonly Collection<string> _errors;

        #region Constructors

        /// <summary>
        /// This is the default constructor.
        /// </summary>
        public TypeTableLoadException()
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes a localized error message.
        /// </summary>
        /// <param name="message">
        /// A localized error message.
        /// </param>
        public TypeTableLoadException(string message)
            : base(message)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes a localized message and an inner exception.
        /// </summary>
        /// <param name="message">
        /// Localized error message.
        /// </param>
        /// <param name="innerException">
        /// Inner exception.
        /// </param>
        public TypeTableLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes a collection of errors occurred during construction
        /// time.
        /// </summary>
        /// <param name="loadErrors">
        /// The errors that occurred
        /// </param>
        internal TypeTableLoadException(ConcurrentBag<string> loadErrors)
            : base(TypesXmlStrings.TypeTableLoadErrors)

        {
            _errors = new Collection<string>(loadErrors.ToArray());
            SetDefaultErrorRecord();
        }

        #endregion Constructors

        /// <summary>
        /// Set the default ErrorRecord.
        /// </summary>
        protected void SetDefaultErrorRecord()
        {
            SetErrorCategory(ErrorCategory.InvalidData);
            SetErrorId(typeof(TypeTableLoadException).FullName);
        }

        /// <summary>
        /// The specific TypeTable load errors.
        /// </summary>
        public Collection<string> Errors
        {
            get
            {
                return _errors;
            }
        }
    }

    #region TypeData

    /// <summary>
    /// TypeData represent a Type Definition.
    /// </summary>
    public sealed class TypeData
    {
        internal const string NoteProperty = "NoteProperty";
        internal const string AliasProperty = "AliasProperty";
        internal const string ScriptProperty = "ScriptProperty";
        internal const string CodeProperty = "CodeProperty";
        internal const string ScriptMethod = "ScriptMethod";
        internal const string CodeMethod = "CodeMethod";
        internal const string PropertySet = "PropertySet";
        internal const string MemberSet = "MemberSet";

        private TypeData()
        {
            StandardMembers = new Dictionary<string, TypeMemberData>(StringComparer.OrdinalIgnoreCase);
            Members = new Dictionary<string, TypeMemberData>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initialize a TypeData instance by providing the typeName.
        /// </summary>
        /// <param name="typeName"></param>
        public TypeData(string typeName) : this()
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw PSTraceSource.NewArgumentNullException(nameof(typeName));
            this.TypeName = typeName;
        }

        internal TypeData(string typeName, bool typesXml) : this()
        {
            this.fromTypesXmlFile = typesXml;
            this.TypeName = typeName;
        }

        /// <summary>
        /// Initialize a TypeData instance by providing a Type.
        /// </summary>
        /// <param name="type"></param>
        public TypeData(Type type) : this()
        {
            if (type == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(type));
            }

            this.TypeName = type.FullName;
        }

        internal bool fromTypesXmlFile { get; }

        /// <summary>
        /// Get the TypeName.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Get the members of this TypeData instance.
        /// The Key of the dictionary is the member's name, and the Value is a TypeMemberData instance.
        /// </summary>
        public Dictionary<string, TypeMemberData> Members { get; }

        /// <summary>
        /// The type converter.
        /// </summary>
        public Type TypeConverter { get; set; }

        /// <summary>
        /// The type adapter.
        /// </summary>
        public Type TypeAdapter { get; set; }

        /// <summary>
        /// Set to true if override the existing definition.
        /// </summary>
        public bool IsOverride { get; set; }

        #region StandardMember

        internal Dictionary<string, TypeMemberData> StandardMembers { get; }

        /// <summary>
        /// The serializationMethod.
        /// </summary>
        public string SerializationMethod
        {
            get
            {
                return _serializationMethod;
            }

            set
            {
                _serializationMethod = value;
                if (_serializationMethod == null)
                {
                    StandardMembers.Remove(TypeTable.SerializationMethodNode);
                    return;
                }

                TypeMemberData typeMemberData;
                if (StandardMembers.TryGetValue(TypeTable.SerializationMethodNode, out typeMemberData))
                {
                    ((NotePropertyData)typeMemberData).Value = _serializationMethod;
                }
                else
                {
                    NotePropertyData note = new NotePropertyData(TypeTable.SerializationMethodNode, _serializationMethod);
                    StandardMembers.Add(TypeTable.SerializationMethodNode, note);
                }
            }
        }

        /// <summary>
        /// The targetTypeForDeserialization.
        /// </summary>
        public Type TargetTypeForDeserialization
        {
            get
            {
                return _targetTypeForDeserialization;
            }

            set
            {
                _targetTypeForDeserialization = value;
                if (_targetTypeForDeserialization == null)
                {
                    StandardMembers.Remove(TypeTable.TargetTypeForDeserialization);
                    return;
                }

                TypeMemberData typeMemberData;
                if (StandardMembers.TryGetValue(TypeTable.TargetTypeForDeserialization, out typeMemberData))
                {
                    ((NotePropertyData)typeMemberData).Value = _targetTypeForDeserialization;
                }
                else
                {
                    NotePropertyData note = new NotePropertyData(TypeTable.TargetTypeForDeserialization, _targetTypeForDeserialization);
                    StandardMembers.Add(TypeTable.TargetTypeForDeserialization, note);
                }
            }
        }

        /// <summary>
        /// The serializationDepth.
        /// </summary>
        public uint SerializationDepth
        {
            get
            {
                return _serializationDepth;
            }

            set
            {
                _serializationDepth = value;

                TypeMemberData typeMemberData;
                if (StandardMembers.TryGetValue(TypeTable.SerializationDepth, out typeMemberData))
                {
                    ((NotePropertyData)typeMemberData).Value = _serializationDepth;
                }
                else
                {
                    NotePropertyData note = new NotePropertyData(TypeTable.SerializationDepth, _serializationDepth);
                    StandardMembers.Add(TypeTable.SerializationDepth, note);
                }
            }
        }

        /// <summary>
        /// The defaultDisplayProperty.
        /// </summary>
        public string DefaultDisplayProperty
        {
            get
            {
                return _defaultDisplayProperty;
            }

            set
            {
                _defaultDisplayProperty = value;
                if (_defaultDisplayProperty == null)
                {
                    StandardMembers.Remove(TypeTable.DefaultDisplayProperty);
                    return;
                }

                TypeMemberData typeMemberData;
                if (StandardMembers.TryGetValue(TypeTable.DefaultDisplayProperty, out typeMemberData))
                {
                    ((NotePropertyData)typeMemberData).Value = _defaultDisplayProperty;
                }
                else
                {
                    NotePropertyData note = new NotePropertyData(TypeTable.DefaultDisplayProperty, _defaultDisplayProperty);
                    StandardMembers.Add(TypeTable.DefaultDisplayProperty, note);
                }
            }
        }

        /// <summary>
        /// The InheritPropertySerializationSet.
        /// </summary>
        public bool InheritPropertySerializationSet
        {
            get
            {
                return _inheritPropertySerializationSet;
            }

            set
            {
                _inheritPropertySerializationSet = value;
                TypeMemberData typeMemberData;
                if (StandardMembers.TryGetValue(TypeTable.InheritPropertySerializationSet, out typeMemberData))
                {
                    ((NotePropertyData)typeMemberData).Value = _inheritPropertySerializationSet;
                }
                else
                {
                    NotePropertyData note = new NotePropertyData(TypeTable.InheritPropertySerializationSet, _inheritPropertySerializationSet);
                    StandardMembers.Add(TypeTable.InheritPropertySerializationSet, note);
                }
            }
        }

        /// <summary>
        /// The stringSerializationSource.
        /// </summary>
        public string StringSerializationSource
        {
            get
            {
                return _stringSerializationSource;
            }

            set
            {
                if (value == null)
                {
                    // If we set the standard member via StringSerializationSourceProperty, we
                    // don't want to remove it, so only remove the member if our previous
                    // backing field was not null.
                    if (_stringSerializationSource != null)
                    {
                        StandardMembers.Remove(TypeTable.StringSerializationSource);
                    }

                    _stringSerializationSource = null;
                    return;
                }

                _stringSerializationSource = value;
                if (_stringSerializationSourceProperty != null)
                {
                    // Remove existing property
                    StandardMembers.Remove(TypeTable.StringSerializationSource);
                    _stringSerializationSourceProperty = null;
                }

                TypeMemberData typeMemberData;
                if (StandardMembers.TryGetValue(TypeTable.StringSerializationSource, out typeMemberData))
                {
                    ((AliasPropertyData)typeMemberData).ReferencedMemberName = _stringSerializationSource;
                }
                else
                {
                    AliasPropertyData alias = new AliasPropertyData(TypeTable.StringSerializationSource, _stringSerializationSource);
                    StandardMembers.Add(TypeTable.StringSerializationSource, alias);
                }
            }
        }

        /// <summary>
        /// The StringSerializationSource when the property is not an AliasProperty.
        /// If the property is an AliasProperty, prefer <see cref="StringSerializationSource"/>.
        /// </summary>
        public TypeMemberData StringSerializationSourceProperty
        {
            get
            {
                return _stringSerializationSourceProperty;
            }

            set
            {
                if (value == null)
                {
                    // If we set the standard member via StringSerializationSource, we
                    // don't want to remove it, so only remove the member if our previous
                    // backing field was not null.
                    if (_stringSerializationSourceProperty != null)
                    {
                        StandardMembers.Remove(TypeTable.StringSerializationSource);
                    }

                    _stringSerializationSourceProperty = null;
                    return;
                }

                if (value is not NotePropertyData && value is not ScriptPropertyData && value is not CodePropertyData)
                {
                    throw PSTraceSource.NewArgumentException("value");
                }

                // Remove existing property
                StandardMembers.Remove(TypeTable.StringSerializationSource);

                _stringSerializationSourceProperty = value;
                _stringSerializationSource = null;

                StandardMembers.Add(TypeTable.StringSerializationSource, value);
            }
        }

        /// <summary>
        /// The defaultDisplayPropertySet.
        /// </summary>
        public PropertySetData DefaultDisplayPropertySet
        {
            get
            {
                return _defaultDisplayPropertySet;
            }

            set
            {
                _defaultDisplayPropertySet = value;
                if (_defaultDisplayPropertySet != null)
                {
                    _defaultDisplayPropertySet.Name = TypeTable.DefaultDisplayPropertySet;
                }
            }
        }

        /// <summary>
        /// The defaultKeyPropertySet.
        /// </summary>
        public PropertySetData DefaultKeyPropertySet
        {
            get
            {
                return _defaultKeyPropertySet;
            }

            set
            {
                _defaultKeyPropertySet = value;
                if (_defaultKeyPropertySet != null)
                {
                    _defaultKeyPropertySet.Name = TypeTable.DefaultKeyPropertySet;
                }
            }
        }

        /// <summary>
        /// The PropertySerializationSet.
        /// </summary>
        public PropertySetData PropertySerializationSet
        {
            get
            {
                return _propertySerializationSet;
            }

            set
            {
                _propertySerializationSet = value;
                if (_propertySerializationSet != null)
                {
                    _propertySerializationSet.Name = TypeTable.PropertySerializationSet;
                }
            }
        }

        // They are of NoteProperty
        private string _serializationMethod;
        private Type _targetTypeForDeserialization;
        private uint _serializationDepth;
        private string _defaultDisplayProperty;

        // InheritPropertySerializationSet should be true or false
        private bool _inheritPropertySerializationSet;

        // It is of AliasProperty
        private string _stringSerializationSource;

        // Except when it's not
        private TypeMemberData _stringSerializationSourceProperty;

        // They are of propertySet
        private PropertySetData _defaultDisplayPropertySet;
        private PropertySetData _defaultKeyPropertySet;
        private PropertySetData _propertySerializationSet;

        #endregion StandardMember

        /// <summary>
        /// Return a TypeData that is a copy of this one.
        /// </summary>
        /// <returns></returns>
        public TypeData Copy()
        {
            TypeData newTypeData = new TypeData(this.TypeName);

            foreach (KeyValuePair<string, TypeMemberData> entry in this.Members)
            {
                newTypeData.Members.Add(entry.Key, entry.Value.Copy());
            }

            newTypeData.TypeConverter = this.TypeConverter;
            newTypeData.TypeAdapter = this.TypeAdapter;
            newTypeData.IsOverride = this.IsOverride;

            foreach (KeyValuePair<string, TypeMemberData> entry in this.StandardMembers)
            {
                switch (entry.Key)
                {
                    case TypeTable.SerializationMethodNode:
                        newTypeData.SerializationMethod = this.SerializationMethod;
                        break;
                    case TypeTable.TargetTypeForDeserialization:
                        newTypeData.TargetTypeForDeserialization = this.TargetTypeForDeserialization;
                        break;
                    case TypeTable.SerializationDepth:
                        newTypeData.SerializationDepth = this.SerializationDepth;
                        break;
                    case TypeTable.DefaultDisplayProperty:
                        newTypeData.DefaultDisplayProperty = this.DefaultDisplayProperty;
                        break;
                    case TypeTable.InheritPropertySerializationSet:
                        newTypeData.InheritPropertySerializationSet = this.InheritPropertySerializationSet;
                        break;
                    case TypeTable.StringSerializationSource:
                        newTypeData.StringSerializationSource = this.StringSerializationSource;
                        break;
                    default:
                        Dbg.Fail("Standard members should at most contain six kinds of elements");
                        break;
                }
            }

            newTypeData.DefaultDisplayPropertySet = this.DefaultDisplayPropertySet == null
                                                        ? null
                                                        : (PropertySetData)this.DefaultDisplayPropertySet.Copy();
            newTypeData.DefaultKeyPropertySet = this.DefaultKeyPropertySet == null
                                                    ? null
                                                    : (PropertySetData)this.DefaultKeyPropertySet.Copy();
            newTypeData.PropertySerializationSet = this.PropertySerializationSet == null
                                                       ? null
                                                       : (PropertySetData)this.PropertySerializationSet.Copy();

            return newTypeData;
        }
    }

    /// <summary>
    /// TypeMemberData is the base class for type members.
    /// The type members derived from this class are:
    ///     NotePropertyData,
    ///     AliasPropertyData,
    ///     ScriptPropertyData,
    ///     CodePropertyData,
    ///     ScriptMethodData,
    ///     CodeMethodData.
    /// </summary>
    public abstract class TypeMemberData
    {
        /// <summary>
        /// TypeMemberData constructor.
        /// </summary>
        /// <param name="name"></param>
        internal TypeMemberData(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            Name = name;
        }

        internal TypeMemberData()
        {
        }

        /// <summary>
        /// The name of the member.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Return a TypeMemberData that is a copy of this one.
        /// </summary>
        /// <returns></returns>
        internal abstract TypeMemberData Copy();

        internal abstract void Process(ConcurrentBag<string> errors, string typeName, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride);
    }

    /// <summary>
    /// NotePropertyData represents a NoteProperty definition.
    /// </summary>
    [DebuggerDisplay("NoteProperty: {Name,nq} = {Value,nq}")]
    public sealed class NotePropertyData : TypeMemberData
    {
        /// <summary>
        /// NotePropertyData constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public NotePropertyData(string name, object value)
            : base(name)
        {
            Value = value;
        }

        /// <summary>
        /// The value of this NoteProperty.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Set true if the member is supposed to be hidden.
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Return a new NotePropertyData that is a copy of this one.
        /// </summary>
        /// <returns></returns>
        internal override TypeMemberData Copy()
        {
            NotePropertyData newNote = new NotePropertyData(this.Name, this.Value) { IsHidden = this.IsHidden };
            return newNote;
        }

        internal override void Process(ConcurrentBag<string> errors, string typeName, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            TypeTable.ProcessNoteData(errors, typeName, this, membersCollection, isOverride);
        }
    }

    /// <summary>
    /// AliasPropertyData represents a AliasProperty definition.
    /// </summary>
    [DebuggerDisplay("AliasProperty: {Name,nq} = {ReferencedMemberName,nq}")]
    public sealed class AliasPropertyData : TypeMemberData
    {
        /// <summary>
        /// AliasPropertyData constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="referencedMemberName"></param>
        public AliasPropertyData(string name, string referencedMemberName)
            : base(name)
        {
            ReferencedMemberName = referencedMemberName;
        }

        /// <summary>
        /// AliasPropertyData constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="referencedMemberName"></param>
        /// <param name="type"></param>
        public AliasPropertyData(string name, string referencedMemberName, Type type)
            : base(name)
        {
            ReferencedMemberName = referencedMemberName;
            MemberType = type;
        }

        /// <summary>
        /// The name of the referenced member.
        /// </summary>
        public string ReferencedMemberName { get; set; }

        /// <summary>
        /// Specify the Type to which the referenced member value will be
        /// converted to.
        /// </summary>
        public Type MemberType { get; set; }

        /// <summary>
        /// Set true if the member is supposed to be hidden.
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Return a new AliasPropertyData that is a copy of this one.
        /// </summary>
        /// <returns></returns>
        internal override TypeMemberData Copy()
        {
            AliasPropertyData newAlias = new AliasPropertyData(this.Name, this.ReferencedMemberName, this.MemberType)
            {
                IsHidden = this.IsHidden
            };
            return newAlias;
        }

        internal override void Process(ConcurrentBag<string> errors, string typeName, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            TypeTable.ProcessAliasData(errors, typeName, this, membersCollection, isOverride);
        }
    }

    /// <summary>
    /// ScriptPropertyData represents a ScriptProperty definition.
    /// </summary>
    [DebuggerDisplay("ScriptProperty: {Name,nq}")]
    public sealed class ScriptPropertyData : TypeMemberData
    {
        /// <summary>
        /// Initialize the ScriptPropertyData as a read only property.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="getScriptBlock"></param>
        public ScriptPropertyData(string name, ScriptBlock getScriptBlock)
            : base(name)
        {
            GetScriptBlock = getScriptBlock;
        }

        /// <summary>
        /// ScriptPropertyData constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="getScriptBlock"></param>
        /// <param name="setScriptBlock"></param>
        public ScriptPropertyData(string name, ScriptBlock getScriptBlock, ScriptBlock setScriptBlock)
            : base(name)
        {
            GetScriptBlock = getScriptBlock;
            SetScriptBlock = setScriptBlock;
        }

        /// <summary>
        /// The getter ScriptBlock.
        /// </summary>
        public ScriptBlock GetScriptBlock { get; set; }

        /// <summary>
        /// The setter ScriptBlock.
        /// </summary>
        public ScriptBlock SetScriptBlock { get; set; }

        /// <summary>
        /// Set true if the member is supposed to be hidden.
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Return a new ScriptPropertyData that is a copy of this one.
        /// </summary>
        /// <returns></returns>
        internal override TypeMemberData Copy()
        {
            ScriptPropertyData newScriptProperty = new ScriptPropertyData(this.Name, this.GetScriptBlock, this.SetScriptBlock)
            {
                IsHidden = this.IsHidden
            };
            return newScriptProperty;
        }

        internal override void Process(ConcurrentBag<string> errors, string typeName, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            TypeTable.ProcessScriptPropertyData(errors, typeName, this, membersCollection, isOverride);
        }
    }

    /// <summary>
    /// CodePropertyData represents a CodeProperty definition.
    /// </summary>
    public sealed class CodePropertyData : TypeMemberData
    {
        /// <summary>
        /// Initialize the CodePropertyData as a read only property.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="getMethod"></param>
        public CodePropertyData(string name, MethodInfo getMethod)
            : base(name)
        {
            GetCodeReference = getMethod;
        }

        /// <summary>
        /// CodePropertyData constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="getMethod"></param>
        /// <param name="setMethod"></param>
        public CodePropertyData(string name, MethodInfo getMethod, MethodInfo setMethod)
            : base(name)
        {
            GetCodeReference = getMethod;
            SetCodeReference = setMethod;
        }

        /// <summary>
        /// The getter code reference.
        /// </summary>
        public MethodInfo GetCodeReference { get; set; }

        /// <summary>
        /// The setter code reference.
        /// </summary>
        public MethodInfo SetCodeReference { get; set; }

        /// <summary>
        /// Set true if the member is supposed to be hidden.
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Return a CodePropertyData that is a copy of this one.
        /// </summary>
        /// <returns></returns>
        internal override TypeMemberData Copy()
        {
            CodePropertyData newCodeProperty = new CodePropertyData(this.Name, this.GetCodeReference, this.SetCodeReference)
            {
                IsHidden = this.IsHidden
            };
            return newCodeProperty;
        }

        internal override void Process(ConcurrentBag<string> errors, string typeName, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            TypeTable.ProcessCodePropertyData(errors, typeName, this, membersCollection, isOverride);
        }
    }

    /// <summary>
    /// ScriptMethodData represents a ScriptMethod definition.
    /// </summary>
    [DebuggerDisplay(@"ScriptMethod: {Name,nq}")]
    public sealed class ScriptMethodData : TypeMemberData
    {
        /// <summary>
        /// ScriptMethodData constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="scriptToInvoke"></param>
        public ScriptMethodData(string name, ScriptBlock scriptToInvoke)
            : base(name)
        {
            Script = scriptToInvoke;
        }

        /// <summary>
        /// The script method.
        /// </summary>
        public ScriptBlock Script { get; set; }

        /// <summary>
        /// Return a ScriptMethodData that is a copy of this one.
        /// </summary>
        /// <returns></returns>
        internal override TypeMemberData Copy()
        {
            ScriptMethodData newScriptMethod = new ScriptMethodData(this.Name, this.Script);
            return newScriptMethod;
        }

        internal override void Process(ConcurrentBag<string> errors, string typeName, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            TypeTable.ProcessScriptMethodData(errors, typeName, this, membersCollection, isOverride);
        }
    }

    /// <summary>
    /// CodeMethodData represents a CodeMethodData definition.
    /// </summary>
    [DebuggerDisplay("CodeMethod: {Name,nq}")]
    public sealed class CodeMethodData : TypeMemberData
    {
        /// <summary>
        /// CodeMethodData constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="methodToCall"></param>
        public CodeMethodData(string name, MethodInfo methodToCall)
            : base(name)
        {
            CodeReference = methodToCall;
        }

        /// <summary>
        /// The code reference.
        /// </summary>
        public MethodInfo CodeReference { get; set; }

        /// <summary>
        /// Return a CodeMethodData that is a copy of this one.
        /// </summary>
        /// <returns></returns>
        internal override TypeMemberData Copy()
        {
            CodeMethodData newCodeMethod = new CodeMethodData(this.Name, this.CodeReference);
            return newCodeMethod;
        }

        internal override void Process(ConcurrentBag<string> errors, string typeName, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            TypeTable.ProcessCodeMethodData(errors, typeName, this, membersCollection, isOverride);
        }
    }

    /// <summary>
    /// PropertySetData represent a PropertySet definition.
    /// </summary>
    [DebuggerDisplay("PropertySet: {Name,nq}")]
    public sealed class PropertySetData : TypeMemberData
    {
        /// <summary>
        /// PropertySetData constructor.
        /// </summary>
        /// <param name="referencedProperties"></param>
        public PropertySetData(IEnumerable<string> referencedProperties)
        {
            if (referencedProperties == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(referencedProperties));
            }

            ReferencedProperties = new Collection<string>(new List<string>(referencedProperties));
        }

        /// <summary>
        /// The referenced properties.
        /// </summary>
        public Collection<string> ReferencedProperties { get; }

        /// <summary>
        /// The PropertySet name.
        /// </summary>
        internal new string Name { get { return base.Name; } set { base.Name = value; } }

        /// <summary>
        /// Set true if the member is supposed to be hidden.
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Return a new PropertySetData that is a copy of this one.
        /// </summary>
        /// <returns></returns>
        internal override TypeMemberData Copy()
        {
            var newPropertySet = new PropertySetData(this.ReferencedProperties)
            {
                Name = this.Name,
                IsHidden = this.IsHidden
            };
            return newPropertySet;
        }

        internal override void Process(ConcurrentBag<string> errors, string typeName, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            TypeTable.ProcessPropertySetData(errors, typeName, this, membersCollection, isOverride);
        }
    }

    /// <summary>
    /// MemberSetData represents a MemberSet definition.
    /// </summary>
    public class MemberSetData : TypeMemberData
    {
        /// <summary>
        /// MemberSetData constructor.
        /// </summary>
        /// <param name="name">The name of the MemberSet.</param>
        /// <param name="members">The members of the MemberSet.</param>
        public MemberSetData(string name, Collection<TypeMemberData> members)
            : base(name)
        {
            Members = members;
            InheritMembers = true;
        }

        /// <summary>
        /// The members of the MemberSet.
        /// </summary>
        public Collection<TypeMemberData> Members { get; }

        /// <summary>
        /// Set true if the member is supposed to be hidden.
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Indicating if the MemberSet will inherit members of the MemberSet
        /// of the same name in the "parent" class.
        /// </summary>
        public bool InheritMembers { get; set; }

        internal override TypeMemberData Copy()
        {
            MemberSetData newMemberSetData = new MemberSetData(Name, Members)
            {
                IsHidden = this.IsHidden,
                InheritMembers = this.InheritMembers
            };
            return newMemberSetData;
        }

        internal override void Process(ConcurrentBag<string> errors, string typeName, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            TypeTable.ProcessMemberSetData(errors, typeName, this, membersCollection, isOverride);
        }
    }

    #endregion TypeData

    /// <summary>
    /// A class that keeps the information from types.ps1xml files in a cache table.
    /// </summary>
    public sealed partial class TypeTable
    {
        #region private

        #region strings

        internal const string PSStandardMembers = "PSStandardMembers";
        internal const string SerializationDepth = "SerializationDepth";
        internal const string StringSerializationSource = "StringSerializationSource";
        internal const string SerializationMethodNode = "SerializationMethod";
        internal const string TargetTypeForDeserialization = "TargetTypeForDeserialization";
        internal const string PropertySerializationSet = "PropertySerializationSet";
        internal const string InheritPropertySerializationSet = "InheritPropertySerializationSet";
        internal const string Types = "Types";
        internal const string Type = "Type";
        internal const string DefaultDisplayPropertySet = "DefaultDisplayPropertySet";
        internal const string DefaultKeyPropertySet = "DefaultKeyPropertySet";
        internal const string DefaultDisplayProperty = "DefaultDisplayProperty";

        // this is used for extended properties like Note,Alias,Script,Code
        internal const string IsHiddenAttribute = "IsHidden";

        #endregion strings

        #region fields

        /// <summary>
        /// Table from type name list into PSMemberInfoInternalCollection.
        /// </summary>
        private readonly ConcurrentDictionary<string, PSMemberInfoInternalCollection<PSMemberInfo>> _consolidatedMembers =
            new ConcurrentDictionary<string, PSMemberInfoInternalCollection<PSMemberInfo>>(
                concurrencyLevel: 1, capacity: 256, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Table from type name list into Collection of strings.
        /// </summary>
        private readonly ConcurrentDictionary<string, Collection<string>> _consolidatedSpecificProperties =
            new ConcurrentDictionary<string, Collection<string>>(
                concurrencyLevel: 1, capacity: 10, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Table from type name into PSMemberInfoInternalCollection.
        /// </summary>
        private readonly ConcurrentDictionary<string, PSMemberInfoInternalCollection<PSMemberInfo>> _extendedMembers =
            new ConcurrentDictionary<string, PSMemberInfoInternalCollection<PSMemberInfo>>(
                concurrencyLevel: 3, capacity: 300, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Points to a Hashtable from type name to type converter.
        /// </summary>
        private readonly ConcurrentDictionary<string, object> _typeConverters
            = new ConcurrentDictionary<string, object>(
                concurrencyLevel: 1, capacity: 5, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Points to a Hashtable from type name to type adapter.
        /// </summary>
        private readonly ConcurrentDictionary<string, PSObject.AdapterSet> _typeAdapters =
            new ConcurrentDictionary<string, PSObject.AdapterSet>(
                concurrencyLevel: 1, capacity: 5, StringComparer.OrdinalIgnoreCase);

        // this is used to throw errors when updating a shared TypeTable.
        internal readonly bool isShared;
        private readonly List<string> _typeFileList;

        // The member factory is cached to avoid allocating Func<> delegates on each call
        private readonly Func<string, ConsolidatedString, PSMemberInfoInternalCollection<PSMemberInfo>> _memberFactoryFunc;

        // This holds all the type information that is in the typetable
        // Holds file name if types file was used to update the types
        // Holds typename/typedata instance if typename/typedata was used to update the types
        // InitialSessionStateEntryCollection is thread safe, so no locks are needed during updates here.
        internal InitialSessionStateEntryCollection<SessionStateTypeEntry> typesInfo =
            new InitialSessionStateEntryCollection<SessionStateTypeEntry>();

        internal const SerializationMethod DefaultSerializationMethod = SerializationMethod.AllPublicProperties;

        internal const bool DefaultInheritPropertySerializationSet = true;

        private static readonly string[] s_standardMembers = new string[]
        {
            DefaultDisplayProperty,
            DefaultDisplayPropertySet,
            DefaultKeyPropertySet,
            SerializationMethodNode,
            SerializationDepth,
            StringSerializationSource,
            PropertySerializationSet,
            InheritPropertySerializationSet,
            TargetTypeForDeserialization
        };

        // Built-in type file paths.
        internal static readonly string TypesFilePath;
        internal static readonly string TypesV3FilePath;
        internal static readonly string GetEventTypesFilePath;

        #endregion

        #region Update

        #region process nodes

        private static void AddMember(ConcurrentBag<string> errors, string typeName, PSMemberInfo member, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            if (PSMemberInfoCollection<PSMemberInfo>.IsReservedName(member.name))
            {
                AddError(errors, typeName, TypesXmlStrings.ReservedNameMember, member.name);
                return;
            }

            if (membersCollection[member.name] != null && !isOverride)
            {
                AddError(errors, typeName, TypesXmlStrings.DuplicateMember, member.name);
                return;
            }

            member.IsInstance = false;

            if (membersCollection[member.name] == null)
            {
                membersCollection.Add(member);
            }
            else
            {
                membersCollection.Replace(member);
            }
        }

        #region CheckStandardMembers
        private static bool GetCheckNote(ConcurrentBag<string> errors, string typeName, PSMemberInfoInternalCollection<PSMemberInfo> members, string noteName, Type noteType, out PSNoteProperty note)
        {
            note = null;
            PSMemberInfo noteAsMemberInfo = null;
            for (int i = 0; i < members.Count; i++)
            {
                PSMemberInfo member = members[i];
                if (string.Equals(member.Name, noteName, StringComparison.OrdinalIgnoreCase))
                {
                    noteAsMemberInfo = member;
                }
            }

            if (noteAsMemberInfo == null)
            {
                return true;
            }

            note = noteAsMemberInfo as PSNoteProperty;
            if (note == null)
            {
                AddError(errors, typeName, TypesXmlStrings.MemberShouldBeNote, noteAsMemberInfo.Name);
                return false;
            }

            object sourceValue = note.Value;
            if (noteType.GetTypeCode().Equals(TypeCode.Boolean))
            {
                string sourceValueAsString = sourceValue as string;
                if (sourceValueAsString != null)
                {
                    if (sourceValueAsString.Length == 0)
                    {
                        note.noteValue = true;
                    }
                    else
                    {
                        note.noteValue = !string.Equals(sourceValueAsString, "false", StringComparison.OrdinalIgnoreCase);
                    }

                    return true;
                }
            }

            try
            {
                note.noteValue = LanguagePrimitives.ConvertTo(sourceValue, noteType, CultureInfo.InvariantCulture);
            }
            catch (PSInvalidCastException e)
            {
                AddError(errors, typeName, TypesXmlStrings.ErrorConvertingNote, note.Name, e.Message);
                return false;
            }

            return true;
        }

        private static bool EnsureNotPresent(ConcurrentBag<string> errors, string typeName, PSMemberInfoInternalCollection<PSMemberInfo> members, string memberName)
        {
            for (int i = 0; i < members.Count; i++)
            {
                PSMemberInfo member = members[i];
                if (string.Equals(member.Name, memberName, StringComparison.OrdinalIgnoreCase))
                {
                    AddError(errors, typeName, TypesXmlStrings.MemberShouldNotBePresent, member.Name);
                    return false;
                }
            }

            return true;
        }

        private static bool GetCheckMemberType(ConcurrentBag<string> errors, string typeName, PSMemberInfoInternalCollection<PSMemberInfo> members, string noteName, Type memberType, out PSMemberInfo member)
        {
            member = null;
            for (int i = 0; i < members.Count; i++)
            {
                PSMemberInfo m = members[i];
                if (string.Equals(m.Name, noteName, StringComparison.OrdinalIgnoreCase))
                {
                    member = m;
                }
            }

            if (member == null)
            {
                return true;
            }

            if (memberType.IsInstanceOfType(member))
            {
                return true;
            }

            AddError(errors, typeName, TypesXmlStrings.MemberShouldHaveType, member.Name, memberType.Name);
            member = null;
            return false;
        }

        /// <summary>
        /// Issue appropriate errors and remove members as necessary if:
        ///     - The serialization settings do not fall into one of the combinations of the table below
        ///     - If the serialization settings notes' values cannot be converted to the proper type
        ///     - If serialization settings members are of the wrong member type
        ///     - DefaultDisplayPropertySet is not an PSPropertySet
        ///     - DefaultDisplayProperty is not an PSPropertyInfo
        ///     - DefaultKeyPropertySet is not an PSPropertySet
        ///
        /// SerializationMethod       InheritPropertySerializationSet   PropertySerializationSet   SerializationDepth     StringSerializationSource
        /// ---------------------     -------------------------------   ------------------------   -------------------    ---------------------------
        /// String                    must NOT be present               must NOT be present        must NOT be present    optional
        /// SpecificProperties        optional                          must be present            optional               optional
        /// AllPublicProperties       must NOT be present               must NOT be present        optional               optional.
        /// </summary>
        private static bool CheckStandardMembers(ConcurrentBag<string> errors, string typeName, PSMemberInfoInternalCollection<PSMemberInfo> members)
        {
            #region Remove all non standard members
            List<string> membersToBeIgnored = new List<string>();
            for (int i = 0; i < members.Count; i++)
            {
                bool found = false;
                string memberName = members[i].Name;
                for (int j = 0; j < s_standardMembers.Length; j++)
                {
                    if (string.Equals(memberName, s_standardMembers[j], StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    membersToBeIgnored.Add(memberName);
                    AddError(errors, typeName, TypesXmlStrings.NotAStandardMember, memberName);
                }
            }

            foreach (string memberToBeIgnored in membersToBeIgnored)
            {
                members.Remove(memberToBeIgnored);
            }
            #endregion Remove all non standard members

            bool serializationSettingsOk;
            do // false loop
            {
                PSNoteProperty serializationMethodNote;
                serializationSettingsOk = GetCheckNote(errors, typeName, members, SerializationMethodNode, typeof(SerializationMethod), out serializationMethodNote);
                if (!serializationSettingsOk)
                {
                    break;
                }

                SerializationMethod serializationMethod = SerializationMethod.AllPublicProperties;
                if (serializationMethodNote != null)
                {
                    serializationMethod = (SerializationMethod)serializationMethodNote.Value;
                }

                if (serializationMethod == SerializationMethod.String)
                {
                    serializationSettingsOk = EnsureNotPresent(errors, typeName, members, InheritPropertySerializationSet);
                    if (!serializationSettingsOk)
                    {
                        break;
                    }

                    serializationSettingsOk = EnsureNotPresent(errors, typeName, members, PropertySerializationSet);
                    if (!serializationSettingsOk)
                    {
                        break;
                    }

                    serializationSettingsOk = EnsureNotPresent(errors, typeName, members, SerializationDepth);
                    if (!serializationSettingsOk)
                    {
                        break;
                    }
                }
                else if (serializationMethod == SerializationMethod.SpecificProperties)
                {
                    PSNoteProperty inheritPropertiesNote;
                    serializationSettingsOk = GetCheckNote(errors, typeName, members, InheritPropertySerializationSet, typeof(bool), out inheritPropertiesNote);
                    if (!serializationSettingsOk)
                    {
                        break;
                    }

                    PSMemberInfo propertySerializationSet;
                    serializationSettingsOk = GetCheckMemberType(errors, typeName, members,
                        PropertySerializationSet, typeof(PSPropertySet),
                        out propertySerializationSet);
                    if (!serializationSettingsOk)
                    {
                        break;
                    }

                    if (inheritPropertiesNote != null && inheritPropertiesNote.Value.Equals(false) && propertySerializationSet == null)
                    {
                        AddError(
                            errors,
                            typeName,
                            TypesXmlStrings.MemberMustBePresent,
                            PropertySerializationSet,
                            SerializationMethodNode,
                            nameof(SerializationMethod.SpecificProperties),
                            InheritPropertySerializationSet,
                            "false");
                        serializationSettingsOk = false;
                        break;
                    }

                    PSNoteProperty noteProperty;
                    serializationSettingsOk = GetCheckNote(errors, typeName, members, SerializationDepth, typeof(int), out noteProperty);
                    if (!serializationSettingsOk)
                    {
                        break;
                    }
                }
                else if (serializationMethod == SerializationMethod.AllPublicProperties)
                {
                    serializationSettingsOk = EnsureNotPresent(errors, typeName, members, InheritPropertySerializationSet);
                    if (!serializationSettingsOk)
                    {
                        break;
                    }

                    serializationSettingsOk = EnsureNotPresent(errors, typeName, members, PropertySerializationSet);
                    if (!serializationSettingsOk)
                    {
                        break;
                    }

                    PSNoteProperty noteProperty;
                    serializationSettingsOk = GetCheckNote(errors, typeName, members, SerializationDepth, typeof(int), out noteProperty);
                    if (!serializationSettingsOk)
                    {
                        break;
                    }
                }

                PSMemberInfo serializationSource;
                serializationSettingsOk = GetCheckMemberType(errors, typeName, members, StringSerializationSource, typeof(PSPropertyInfo), out serializationSource);
                if (!serializationSettingsOk)
                {
                    break;
                }
            }
            while (false);

            if (!serializationSettingsOk)
            {
                AddError(errors, typeName, TypesXmlStrings.SerializationSettingsIgnored);
                members.Remove(InheritPropertySerializationSet);
                members.Remove(SerializationMethodNode);
                members.Remove(StringSerializationSource);
                members.Remove(PropertySerializationSet);
                members.Remove(SerializationDepth);
            }

            PSMemberInfo otherMember;
            if (!GetCheckMemberType(errors, typeName, members, DefaultDisplayPropertySet, typeof(PSPropertySet), out otherMember))
            {
                members.Remove(DefaultDisplayPropertySet);
            }

            if (!GetCheckMemberType(errors, typeName, members, DefaultKeyPropertySet, typeof(PSPropertySet), out otherMember))
            {
                members.Remove(DefaultKeyPropertySet);
            }

            PSNoteProperty defaultDisplayProperty;
            if (!GetCheckNote(errors, typeName, members, DefaultDisplayProperty, typeof(string), out defaultDisplayProperty))
            {
                members.Remove(DefaultDisplayProperty);
            }

            PSNoteProperty targetTypeForDeserialization;
            if (!GetCheckNote(errors, typeName, members, TargetTypeForDeserialization, typeof(Type), out targetTypeForDeserialization))
            {
                members.Remove(TargetTypeForDeserialization);
            }
            else
            {
                if (targetTypeForDeserialization != null)
                {
                    // GetCheckNote converts the value from string to System.Type.. We should store value as Type
                    // as this will save time spent converting string to Type.
                    members.Remove(TargetTypeForDeserialization);
                    members.Add(targetTypeForDeserialization, true);
                }
            }

            return serializationSettingsOk;
        }
        #endregion CheckStandardMembers

        /// <summary>
        /// Helper for ProcessTypeConverter/ProcessTypeAdapter from TypeData.
        /// </summary>
        /// <param name="errors"></param>
        /// <param name="typeName"></param>
        /// <param name="type"></param>
        /// <param name="errorFormatString"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        private static bool CreateInstance(ConcurrentBag<string> errors, string typeName, Type type, string errorFormatString, out object instance)
        {
            instance = null;

            Exception instanceException = null;

#pragma warning disable 56500
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (TargetInvocationException e)
            {
                instanceException = e.InnerException ?? e;
            }
            catch (Exception e)
            {
                instanceException = e;
            }
#pragma warning restore 56500

            if (instanceException != null)
            {
                AddError(errors, typeName, errorFormatString, type.FullName, instanceException.Message);
                return false;
            }

            return true;
        }

        #endregion process nodes

        #region process type data

        private static void AddError(ConcurrentBag<string> errors, string typeName, string resourceString, params object[] formatArguments)
        {
            string errorMsg = StringUtil.Format(resourceString, formatArguments);
            string errorLine = StringUtil.Format(TypesXmlStrings.TypeDataTypeError, typeName, errorMsg);
            errors.Add(errorLine);
        }

        #region add members from TypeData

        private static void ProcessMembersData(
            ConcurrentBag<string> errors,
            string typeName,
            Dictionary<string, TypeMemberData> membersData,
            PSMemberInfoInternalCollection<PSMemberInfo> membersCollection,
            bool isOverride)
        {
            foreach (TypeMemberData typeMember in membersData.Values)
            {
                typeMember.Process(errors, typeName, membersCollection, isOverride);
            }
        }

        internal static void ProcessNoteData(ConcurrentBag<string> errors, string typeName, NotePropertyData nodeData, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            // value could be null
            PSNoteProperty note = new PSNoteProperty(nodeData.Name, nodeData.Value) { IsHidden = nodeData.IsHidden };
            AddMember(errors, typeName, note, membersCollection, isOverride);
        }

        internal static void ProcessAliasData(ConcurrentBag<string> errors, string typeName, AliasPropertyData aliasData, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            // ReferencedMemberName should not be an empty string
            if (string.IsNullOrEmpty(aliasData.ReferencedMemberName))
            {
                AddError(errors, typeName, TypesXmlStrings.TypeDataShouldHaveValue, "AliasPropertyData", "ReferencedMemberName");
                return;
            }

            PSAliasProperty alias = new PSAliasProperty(aliasData.Name, aliasData.ReferencedMemberName, aliasData.MemberType)
            {
                IsHidden = aliasData.IsHidden
            };
            AddMember(errors, typeName, alias, membersCollection, isOverride);
        }

        internal static void ProcessScriptPropertyData(ConcurrentBag<string> errors, string typeName, ScriptPropertyData scriptPropertyData, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            ScriptBlock getter = scriptPropertyData.GetScriptBlock;
            ScriptBlock setter = scriptPropertyData.SetScriptBlock;

            if (setter == null && getter == null)
            {
                AddError(errors, typeName, TypesXmlStrings.ScriptPropertyShouldHaveGetterOrSetter);
                return;
            }

            PSScriptProperty scriptProperty = new PSScriptProperty(scriptPropertyData.Name, getter, setter, true)
            {
                IsHidden = scriptPropertyData.IsHidden
            };
            AddMember(errors, typeName, scriptProperty, membersCollection, isOverride);
        }

        internal static void ProcessCodePropertyData(ConcurrentBag<string> errors, string typeName, CodePropertyData codePropertyData, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            if ((codePropertyData.GetCodeReference == null) && (codePropertyData.SetCodeReference == null))
            {
                AddError(errors, typeName, TypesXmlStrings.CodePropertyShouldHaveGetterOrSetter);
                return;
            }

            PSCodeProperty codeProperty;
            try
            {
                codeProperty = new PSCodeProperty(codePropertyData.Name, codePropertyData.GetCodeReference, codePropertyData.SetCodeReference);
            }
            catch (ExtendedTypeSystemException exception)
            {
                AddError(errors, typeName, TypesXmlStrings.Exception, exception.Message);
                return;
            }

            codeProperty.IsHidden = codePropertyData.IsHidden;
            AddMember(errors, typeName, codeProperty, membersCollection, isOverride);
        }

        internal static void ProcessScriptMethodData(ConcurrentBag<string> errors, string typeName, ScriptMethodData scriptMethodData, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            if (scriptMethodData.Script == null)
            {
                AddError(errors, typeName, TypesXmlStrings.TypeDataShouldHaveValue, "ScriptMethodData", "Script");
                return;
            }

            PSScriptMethod scriptMethod = new PSScriptMethod(scriptMethodData.Name, scriptMethodData.Script, true);
            AddMember(errors, typeName, scriptMethod, membersCollection, isOverride);
        }

        internal static void ProcessCodeMethodData(ConcurrentBag<string> errors, string typeName, CodeMethodData codeMethodData, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            if (codeMethodData.CodeReference == null)
            {
                AddError(errors, typeName, TypesXmlStrings.TypeDataShouldHaveValue, "CodeMethodData", "CodeReference");
                return;
            }

            PSCodeMethod codeMethod;
            try
            {
                codeMethod = new PSCodeMethod(codeMethodData.Name, codeMethodData.CodeReference);
            }
            catch (ExtendedTypeSystemException exception)
            {
                AddError(errors, typeName, TypesXmlStrings.Exception, exception.Message);
                return;
            }

            AddMember(errors, typeName, codeMethod, membersCollection, isOverride);
        }

        internal static void ProcessPropertySetData(ConcurrentBag<string> errors, string typeName, PropertySetData propertySetData, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            if (propertySetData.ReferencedProperties == null || propertySetData.ReferencedProperties.Count == 0)
            {
                AddError(errors, typeName, TypesXmlStrings.TypeDataShouldHaveValue, "PropertySetData", "ReferencedProperties");
                return;
            }

            // the node cardinality is OneToMany
            var referencedProperties = new List<string>(propertySetData.ReferencedProperties.Count);
            foreach (string name in propertySetData.ReferencedProperties)
            {
                if (string.IsNullOrEmpty(name))
                {
                    AddError(errors, typeName, TypesXmlStrings.TypeDataShouldNotBeNullOrEmpty, "PropertySetData", "ReferencedProperties");
                    continue;
                }

                referencedProperties.Add(name);
            }

            if (referencedProperties.Count == 0)
            {
                return;
            }

            var propertySet = new PSPropertySet(propertySetData.Name, referencedProperties)
            {
                IsHidden = propertySetData.IsHidden
            };
            AddMember(errors, typeName, propertySet, membersCollection, isOverride);
        }

        internal static void ProcessMemberSetData(ConcurrentBag<string> errors, string typeName, MemberSetData memberSetData, PSMemberInfoInternalCollection<PSMemberInfo> membersCollection, bool isOverride)
        {
            var memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(memberSetData.Members.Count);
            foreach (var m in memberSetData.Members)
            {
                m.Process(errors, typeName, memberSetMembers, isOverride);
            }

            var memberSet = new PSMemberSet(memberSetData.Name, memberSetMembers)
            {
                IsHidden = memberSetData.IsHidden,
                inheritMembers = memberSetData.InheritMembers
            };
            AddMember(errors, typeName, memberSet, membersCollection, isOverride);
        }

        private static void ProcessStandardMembers(
            ConcurrentBag<string> errors,
            string typeName,
            Dictionary<string, TypeMemberData> standardMembers,
            List<PropertySetData> propertySets,
            PSMemberInfoInternalCollection<PSMemberInfo> membersCollection,
            bool isOverride)
        {
            int newMemberCount = standardMembers.Count + propertySets.Count;

            // If StandardMembers do not exists, we follow the original logic to create the StandardMembers
            if (membersCollection[PSStandardMembers] == null)
            {
                var memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(newMemberCount);

                ProcessMembersData(errors, typeName, standardMembers, memberSetMembers, false);
                foreach (PropertySetData propertySet in propertySets)
                {
                    ProcessPropertySetData(errors, typeName, propertySet, memberSetMembers, false);
                }

                CheckStandardMembers(errors, typeName, memberSetMembers);
                PSMemberSet standardMemberSet = new PSMemberSet(PSStandardMembers, memberSetMembers)
                {
                    inheritMembers = true,
                    IsHidden = true,
                    ShouldSerialize = false
                };
                AddMember(errors, typeName, standardMemberSet, membersCollection, false);
                return;
            }

            // StandardMembers exist
            var psStandardMemberSet = (PSMemberSet)membersCollection[PSStandardMembers];

            // Copy existing internal PSStandard members
            int totalMemberCount = psStandardMemberSet.InternalMembers.Count + newMemberCount;
            var existingMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(totalMemberCount);
            var oldMembersCopy = new PSMemberInfoInternalCollection<PSMemberInfo>(totalMemberCount);
            foreach (var existingMember in psStandardMemberSet.InternalMembers)
            {
                existingMembers.Add(existingMember.Copy());
                oldMembersCopy.Add(existingMember.Copy());
            }

            // Process the Members directly into the 'existingMembers' collection
            ProcessMembersData(errors, typeName, standardMembers, existingMembers, isOverride);
            foreach (PropertySetData propertySet in propertySets)
            {
                ProcessPropertySetData(errors, typeName, propertySet, existingMembers, isOverride);
            }

            if (CheckStandardMembers(errors, typeName, existingMembers))
            {
                // No conflict in serialization settings, replace the old StandardMembers with the new one
                PSMemberSet standardMemberSet = new PSMemberSet(PSStandardMembers, existingMembers)
                {
                    inheritMembers = true,
                    IsHidden = true,
                    ShouldSerialize = false
                };
                AddMember(errors, typeName, standardMemberSet, membersCollection, true);
            }
            else
            {
                // There are conflicts in serialization settings, add non-serializationSetting configurations
                // into the original member collection. Replace the old StandardMembers with the new one
                foreach (PSMemberInfo member in existingMembers)
                {
                    if (oldMembersCopy[member.name] == null)
                    {
                        oldMembersCopy.Add(member);
                    }
                }

                PSMemberSet standardMemberSet = new PSMemberSet(PSStandardMembers, oldMembersCopy)
                {
                    inheritMembers = true,
                    IsHidden = true,
                    ShouldSerialize = false
                };
                AddMember(errors, typeName, standardMemberSet, membersCollection, true);
            }
        }

        private static void ProcessStandardMembers(
            ConcurrentBag<string> errors,
            string typeName,
            PSMemberInfoInternalCollection<PSMemberInfo> memberSetMembers,
            PSMemberInfoInternalCollection<PSMemberInfo> typeMemberCollection,
            bool isOverride)
        {
            // If StandardMembers do not exists, we follow the original logic to create the StandardMembers
            if (typeMemberCollection[PSStandardMembers] == null)
            {
                CheckStandardMembers(errors, typeName, memberSetMembers);
                PSMemberSet standardMemberSet = new PSMemberSet(PSStandardMembers, memberSetMembers)
                {
                    inheritMembers = true,
                    IsHidden = true,
                    ShouldSerialize = false
                };
                AddMember(errors, typeName, standardMemberSet, typeMemberCollection, false);
                return;
            }

            // StandardMembers exist
            var psStandardMemberSet = (PSMemberSet)typeMemberCollection[PSStandardMembers];

            // Copy existing internal PSStandard members
            int totalMemberCount = psStandardMemberSet.InternalMembers.Count + memberSetMembers.Count;
            var existingMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(totalMemberCount);
            var oldMembersCopy = new PSMemberInfoInternalCollection<PSMemberInfo>(totalMemberCount);
            foreach (var existingMember in psStandardMemberSet.InternalMembers)
            {
                existingMembers.Add(existingMember.Copy());
                oldMembersCopy.Add(existingMember.Copy());
            }

            // Process the Members directly into the 'existingMembers' collection
            foreach (PSMemberInfo member in memberSetMembers)
            {
                AddMember(errors, typeName, member, existingMembers, isOverride);
            }

            if (CheckStandardMembers(errors, typeName, existingMembers))
            {
                // No conflict in serialization settings, replace the old StandardMembers with the new one
                PSMemberSet standardMemberSet = new PSMemberSet(PSStandardMembers, existingMembers)
                {
                    inheritMembers = true,
                    IsHidden = true,
                    ShouldSerialize = false
                };
                AddMember(errors, typeName, standardMemberSet, typeMemberCollection, isOverride: true);
            }
            else
            {
                // There are conflicts in serialization settings, add non-serializationSetting configurations
                // into the original member collection. Replace the old StandardMembers with the new one
                foreach (PSMemberInfo member in existingMembers)
                {
                    if (oldMembersCopy[member.name] == null)
                    {
                        oldMembersCopy.Add(member);
                    }
                }

                PSMemberSet standardMemberSet = new PSMemberSet(PSStandardMembers, oldMembersCopy)
                {
                    inheritMembers = true,
                    IsHidden = true,
                    ShouldSerialize = false
                };
                AddMember(errors, typeName, standardMemberSet, typeMemberCollection, isOverride: true);
            }
        }

        private static void ProcessTypeConverter(
            ConcurrentBag<string> errors,
            string typeName,
            Type converterType,
            ConcurrentDictionary<string, object> typeConverters,
            bool isOverride)
        {
            if (CreateInstance(errors, typeName, converterType, TypesXmlStrings.UnableToInstantiateTypeConverter, out object instance))
            {
                if ((instance is TypeConverter) || (instance is PSTypeConverter))
                {
                    LanguagePrimitives.UpdateTypeConvertFromTypeTable(typeName);
                }
                else
                {
                    AddError(errors, typeName, TypesXmlStrings.TypeIsNotTypeConverter, converterType.FullName);
                }
            }

            if (instance != null && !typeConverters.TryAdd(typeName, instance))
            {
                if (!isOverride)
                {
                    AddError(errors, typeName, TypesXmlStrings.TypeConverterAlreadyPresent);
                }

                // If IsOverride == true, eat the TypeConverterAlreadyPresent failure.
            }
        }

        private static void ProcessTypeAdapter(
            ConcurrentBag<string> errors,
            string typeName,
            Type adapterType,
            ConcurrentDictionary<string, PSObject.AdapterSet> typeAdapters,
            bool isOverride)
        {
            PSObject.AdapterSet adapterSet = null;
            if (CreateInstance(errors, typeName, adapterType, TypesXmlStrings.UnableToInstantiateTypeAdapter, out object instance))
            {
                PSPropertyAdapter psPropertyAdapter = instance as PSPropertyAdapter;

                if (psPropertyAdapter == null)
                {
                    AddError(errors, typeName, TypesXmlStrings.TypeIsNotTypeAdapter, adapterType.FullName);
                }
                else
                {
                    if (LanguagePrimitives.TryConvertTo(typeName, out Type adaptedType))
                    {
                        adapterSet = PSObject.CreateThirdPartyAdapterSet(adaptedType, psPropertyAdapter);
                    }
                    else
                    {
                        AddError(errors, typeName, TypesXmlStrings.InvalidAdaptedType, typeName);
                    }
                }
            }

            if (adapterSet != null && !typeAdapters.TryAdd(typeName, adapterSet))
            {
                if (!isOverride)
                {
                    AddError(errors, typeName, TypesXmlStrings.TypeAdapterAlreadyPresent);
                }

                // If IsOverride == true, eat the TypeConverterAlreadyPresent failure.
            }
        }

        private void ProcessTypeDataToAdd(ConcurrentBag<string> errors, TypeData typeData)
        {
            string typeName = typeData.TypeName;
            Dbg.Assert(!string.IsNullOrEmpty(typeName), "TypeData class guarantees the typeName is not null and not empty");

            var propertySets = new List<PropertySetData>();
            if (typeData.DefaultDisplayPropertySet != null)
            {
                propertySets.Add(typeData.DefaultDisplayPropertySet);
            }

            if (typeData.DefaultKeyPropertySet != null)
            {
                propertySets.Add(typeData.DefaultKeyPropertySet);
            }

            if (typeData.PropertySerializationSet != null)
            {
                propertySets.Add(typeData.PropertySerializationSet);
            }

            if (typeData.Members.Count == 0 && typeData.StandardMembers.Count == 0
                && typeData.TypeConverter == null && typeData.TypeAdapter == null
                && propertySets.Count == 0 && !typeData.fromTypesXmlFile)
            {
                AddError(errors, typeName, TypesXmlStrings.TypeDataShouldNotBeEmpty);
                return;
            }

            PSMemberInfoInternalCollection<PSMemberInfo> typeMembers = null;
            bool hasStandardMembers = typeData.StandardMembers.Count > 0 || propertySets.Count > 0;
            int collectionSize = typeData.Members.Count + (hasStandardMembers ? 1 : 0);

            if (typeData.Members.Count > 0)
            {
                typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(collectionSize));
                ProcessMembersData(errors, typeName, typeData.Members, typeMembers, typeData.IsOverride);

                foreach (var memberName in typeData.Members.Keys)
                {
                    PSGetMemberBinder.TypeTableMemberAdded(memberName);
                }
            }

            if (hasStandardMembers)
            {
                typeMembers ??= _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

                ProcessStandardMembers(errors, typeName, typeData.StandardMembers, propertySets, typeMembers, typeData.IsOverride);
            }

            if (typeData.TypeConverter != null)
            {
                ProcessTypeConverter(errors, typeName, typeData.TypeConverter, _typeConverters, typeData.IsOverride);
            }

            if (typeData.TypeAdapter != null)
            {
                ProcessTypeAdapter(errors, typeName, typeData.TypeAdapter, _typeAdapters, typeData.IsOverride);
            }

            // Record the information that this typedata was removed from the typetable
            // The next time the typetable is updated, we will need to exclude this typedata from the typetable
            typesInfo.Add(new SessionStateTypeEntry(typeData, isRemove: false));
        }

        #endregion add members from TypeData

        #region remove type data

        private void ProcessTypeDataToRemove(ConcurrentBag<string> errors, TypeData typeData)
        {
            string typeName = typeData.TypeName;
            Dbg.Assert(!string.IsNullOrEmpty(typeName), "TypeData class guarantees the typeName is not null and not empty");

            // We always remove the whole type
            bool typeExist = false;
            PSMemberInfoInternalCollection<PSMemberInfo> memberCollection;
            if (_extendedMembers.TryRemove(typeName, out memberCollection))
            {
                typeExist = true;
                foreach (var m in memberCollection)
                {
                    // Invalidate any cached rules with this name.
                    PSGetMemberBinder.TypeTableMemberPossiblyUpdated(m.Name);
                }
            }

            if (_typeConverters.TryRemove(typeName, out _))
            {
                typeExist = true;
                LanguagePrimitives.UpdateTypeConvertFromTypeTable(typeName);
            }

            if (_typeAdapters.TryRemove(typeName, out _))
            {
                typeExist = true;
            }

            if (!typeExist)
            {
                AddError(errors, typeName, TypesXmlStrings.TypeNotFound, typeName);
            }
            else
            {
                typesInfo.Add(new SessionStateTypeEntry(typeData, isRemove: true));
            }
        }

        #endregion remove type data

        #endregion process type data

        private void Update(LoadContext context)
        {
            try
            {
                var xs = new TypesPs1xmlReader(context);
                var types = xs.Read();
                if (types != null)
                {
                    foreach (var t in types)
                    {
                        ProcessTypeDataToAdd(context.errors, t);
                    }
                }
            }
            catch (Exception e)
            {
                context.AddError(TypesXmlStrings.Exception, e.Message);
            }
        }

        private void Update(ConcurrentBag<string> errors, TypeData typeData, bool isRemove)
        {
            if (!isRemove)
            {
                ProcessTypeDataToAdd(errors, typeData);
            }
            else
            {
                ProcessTypeDataToRemove(errors, typeData);
            }
        }

        #endregion Update

        #endregion private

        #region Constructor

        static TypeTable()
        {
            s_valueFactoryCache = new Func<string, PSMemberInfoInternalCollection<PSMemberInfo>>[ValueFactoryCacheCount];

            // Rather than set these members every time we process the standard members, do it
            // just once at startup.
            foreach (var sm in s_standardMembers)
            {
                PSGetMemberBinder.TypeTableMemberAdded(sm);
            }

            PSGetMemberBinder.TypeTableMemberAdded(PSStandardMembers);

            // Set the built-in type file paths.
            var psHome = Utils.DefaultPowerShellAppBase;
            TypesFilePath = Path.Combine(psHome, "types.ps1xml");
            TypesV3FilePath = Path.Combine(psHome, "typesv3.ps1xml");
            GetEventTypesFilePath = Path.Combine(psHome, "GetEvent.types.ps1xml");
        }

        /// <summary>
        /// </summary>
        internal TypeTable() : this(isShared: false)
        {
        }

        internal TypeTable(bool isShared)
        {
            this.isShared = isShared;
            _typeFileList = new List<string>();
            _memberFactoryFunc = MemberFactory;
        }

        /// <summary>
        /// Constructor that creates a TypeTable from a set of type files.
        /// </summary>
        /// <param name="typeFiles">
        /// Type files to load for type information.
        /// </param>
        /// <exception cref="ArgumentException">
        /// 1. Path {0} is not fully qualified. Specify a fully qualified type file path.
        /// </exception>
        /// <exception cref="TypeTableLoadException">
        /// 1. There were errors loading TypeTable. Look in the Errors property to get
        /// detailed error messages.
        /// </exception>
        public TypeTable(IEnumerable<string> typeFiles) : this(typeFiles, null, null)
        {
        }

        /// <summary>
        /// Load types.ps1xml, typesv3.ps1xml into the typetable.
        /// </summary>
        /// <exception cref="System.Security.SecurityException">
        /// If caller doesn't have permission to read the PowerShell registry key.
        /// </exception>
        /// <returns>TypeTable.</returns>
        public static TypeTable LoadDefaultTypeFiles()
        {
            return new TypeTable(GetDefaultTypeFiles());
        }

        /// <summary>
        /// Gets the default types files available in PowerShell.
        /// </summary>
        /// <returns>List of type files.</returns>
        public static List<string> GetDefaultTypeFiles()
        {
            return new List<string>() { TypesFilePath, TypesV3FilePath };
        }

        /// <summary>
        /// Constructor that creates a TypeTable from a set of type files.
        /// </summary>
        /// <param name="typeFiles">
        /// Type files to load for type information.
        /// </param>
        /// <param name="authorizationManager">
        /// Authorization manager to perform signature checks before reading ps1xml files (or null of no checks are needed)
        /// </param>
        /// <param name="host">
        /// Host passed to <paramref name="authorizationManager"/>.  Can be null if no interactive questions should be asked.
        /// </param>
        /// <exception cref="ArgumentException">
        /// 1. Path {0} is not fully qualified. Specify a fully qualified type file path.
        /// </exception>
        /// <exception cref="TypeTableLoadException">
        /// 1. There were errors loading TypeTable. Look in the Errors property to get
        /// detailed error messages.
        /// </exception>
        internal TypeTable(IEnumerable<string> typeFiles, AuthorizationManager authorizationManager, PSHost host) : this(isShared: true)
        {
            if (typeFiles == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(typeFiles));
            }

            ConcurrentBag<string> errors = new ConcurrentBag<string>();
            foreach (string typefile in typeFiles)
            {
                if (string.IsNullOrEmpty(typefile) || (!Path.IsPathRooted(typefile)))
                {
                    throw PSTraceSource.NewArgumentException("typeFile", TypesXmlStrings.TypeFileNotRooted, typefile);
                }

                Initialize(string.Empty, typefile, errors, authorizationManager, host, out _);
                _typeFileList.Add(typefile);
            }

            if (!errors.IsEmpty)
            {
                throw new TypeTableLoadException(errors);
            }
        }

        #endregion

        #region internal methods

        /// <summary>
        /// The first type in the type hierarchy is guaranteed to have SpecificProperties.
        /// </summary>
        /// <param name="types"></param>
        /// <returns>Null if this should not be serialized with SpecificProperties.</returns>
        internal Collection<string> GetSpecificProperties(ConsolidatedString types)
        {
            if (types == null || string.IsNullOrEmpty(types.Key))
            {
                return new Collection<string>();
            }

            Collection<string> result = _consolidatedSpecificProperties.GetOrAdd(types.Key, key =>
            {
                var retValueTable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string type in types)
                {
                    if (!_extendedMembers.TryGetValue(type, out var typeMembers))
                    {
                        continue;
                    }

                    PSMemberSet settings = typeMembers[PSStandardMembers] as PSMemberSet;
                    if (!(settings?.Members[PropertySerializationSet] is PSPropertySet typeProperties))
                    {
                        continue;
                    }

                    foreach (string reference in typeProperties.ReferencedPropertyNames)
                    {
                        retValueTable.Add(reference);
                    }

                    bool inherit = (bool)PSObject.GetNoteSettingValue(
                        settings,
                        InheritPropertySerializationSet,
                        DefaultInheritPropertySerializationSet,
                        expectedType: typeof(bool),
                        shouldReplicateInstance: false,
                        ownerObject: null);
                    if (!inherit)
                    {
                        break;
                    }
                }

                var retValue = new Collection<string>();
                foreach (var value in retValueTable)
                {
                    retValue.Add(value);
                }

                return retValue;
            });
            return result;
        }

        /// <summary>
        /// Gets the MemberInfoCollection for types. This method will cache its
        /// return value for future reference to the same types.
        /// </summary>
        /// <param name="types">List of types to get the member from.</param>
        /// <returns></returns>
        internal PSMemberInfoInternalCollection<T> GetMembers<T>(ConsolidatedString types) where T : PSMemberInfo
        {
            return PSObject.TransformMemberInfoCollection<PSMemberInfo, T>(GetMembers(types));
        }

        internal T GetFirstMemberOrDefault<T>(ConsolidatedString types, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            return GetMembers(types).FirstOrDefault(member => member is T && predicate(member.Name)) as T;
        }

        internal PSMemberInfoInternalCollection<PSMemberInfo> GetMembers(ConsolidatedString types)
        {
            if ((types == null) || string.IsNullOrEmpty(types.Key))
            {
                return new PSMemberInfoInternalCollection<PSMemberInfo>();
            }

            return _consolidatedMembers.GetOrAdd(types.Key, _memberFactoryFunc, types);
        }

        private PSMemberInfoInternalCollection<PSMemberInfo> MemberFactory(string k, ConsolidatedString types)
        {
            var retValue = new PSMemberInfoInternalCollection<PSMemberInfo>();
            for (int i = types.Count - 1; i >= 0; i--)
            {
                if (!_extendedMembers.TryGetValue(types[i], out var typeMembers))
                {
                    continue;
                }

                foreach (PSMemberInfo typeMember in typeMembers)
                {
                    PSMemberInfo currentMember = retValue[typeMember.Name];

                    // If the member was not present, we add it
                    if (currentMember == null)
                    {
                        retValue.Add(typeMember.Copy());
                        continue;
                    }

                    // There was a currentMember with the same name as typeMember
                    PSMemberSet currentMemberAsMemberSet = currentMember as PSMemberSet;
                    PSMemberSet typeMemberAsMemberSet = typeMember as PSMemberSet;

                    // if we are not in a memberset inherit members situation we just replace
                    // the current member with the new more specific member
                    if (currentMemberAsMemberSet == null || typeMemberAsMemberSet == null ||
                        !typeMemberAsMemberSet.InheritMembers)
                    {
                        retValue.Remove(typeMember.Name);
                        retValue.Add(typeMember.Copy());
                        continue;
                    }

                    // We are in a MemberSet InheritMembers situation, so we add the members in
                    // typeMembers to the existing memberset.
                    foreach (PSMemberInfo typeMemberAsMemberSetMember in typeMemberAsMemberSet.Members)
                    {
                        if (currentMemberAsMemberSet.Members[typeMemberAsMemberSetMember.Name] == null)
                        {
                            ((PSMemberInfoIntegratingCollection<PSMemberInfo>)currentMemberAsMemberSet.Members)
                                .AddToTypesXmlCache(typeMemberAsMemberSetMember, false);
                            continue;
                        }

                        // there is a name conflict, the new member wins.
                        Diagnostics.Assert(
                            !typeMemberAsMemberSetMember.IsHidden,
                            "new member in types.xml cannot be hidden");
                        currentMemberAsMemberSet.InternalMembers.Replace(typeMemberAsMemberSetMember);
                    }
                }
            }

            return retValue;
        }

        /// <summary>
        /// Gets the type converter for the typeName.
        /// </summary>
        /// <param name="typeName">Type name with the converter.</param>
        /// <returns>The type converter for the typeName or null, if there is no type converter.</returns>
        internal object GetTypeConverter(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            object result;
            _typeConverters.TryGetValue(typeName, out result);
            return result;
        }

        /// <summary>
        /// Gets the type adapter for the given type.
        /// </summary>
        /// <returns>The type adapter or null, if there is no adapter.</returns>
        internal PSObject.AdapterSet GetTypeAdapter(Type type)
        {
            if (type == null)
            {
                return null;
            }

            /*
             * BUGBUG: This should be a look-up based on Type.IsAssignableFrom instead of a
             *         strict match on the type name. The look-up should be similar to the
             *         code below, except that child classes must override base classes
             *         in the same hierarchy.
             *
             *         The same applies to TypeConverters
             *
             *         The matching for both converters and adapters should be revisited in
             *         the M3 milestone.
             */
#if true
            PSObject.AdapterSet result;
            _typeAdapters.TryGetValue(type.FullName, out result);
            return result;
#else
            foreach (PSObject.AdapterSet adapterSet in this.typeAdapters.Values)
            {
                ThirdPartyAdapter adapter = (ThirdPartyAdapter)adapterSet.OriginalAdapter;

                if (adapter.AdaptedType.IsAssignableFrom(type))
                {
                    return adapterSet;
                }
            }

            return null;
#endif
        }

        private TypeMemberData GetTypeMemberDataFromPSMemberInfo(PSMemberInfo member)
        {
            var note = member as PSNoteProperty;
            if (note != null)
            {
                return new NotePropertyData(note.Name, note.Value);
            }

            var alias = member as PSAliasProperty;
            if (alias != null)
            {
                return new AliasPropertyData(alias.Name, alias.ReferencedMemberName);
            }

            var scriptProperty = member as PSScriptProperty;
            if (scriptProperty != null)
            {
                ScriptBlock getter = scriptProperty.IsGettable ? scriptProperty.GetterScript : null;
                ScriptBlock setter = scriptProperty.IsSettable ? scriptProperty.SetterScript : null;
                return new ScriptPropertyData(scriptProperty.Name, getter, setter);
            }

            var codeProperty = member as PSCodeProperty;
            if (codeProperty != null)
            {
                MethodInfo getter = codeProperty.IsGettable ? codeProperty.GetterCodeReference : null;
                MethodInfo setter = codeProperty.IsSettable ? codeProperty.SetterCodeReference : null;
                return new CodePropertyData(codeProperty.Name, getter, setter);
            }

            var scriptMethod = member as PSScriptMethod;
            if (scriptMethod != null)
            {
                return new ScriptMethodData(scriptMethod.Name, scriptMethod.Script);
            }

            var codeMethod = member as PSCodeMethod;
            if (codeMethod != null)
            {
                return new CodeMethodData(codeMethod.Name, codeMethod.CodeReference);
            }

            var memberSet = member as PSMemberSet;
            if (memberSet != null)
            {
                if (memberSet.Name.Equals(PSStandardMembers, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var membersData = new Collection<TypeMemberData>();
                foreach (var m in memberSet.Members)
                {
                    membersData.Add(GetTypeMemberDataFromPSMemberInfo(m));
                }

                return new MemberSetData(memberSet.Name, membersData);
            }

            return null;
        }

        /// <summary>
        /// Load a PSMemberInfo instance to the passed-in TypeData.
        /// </summary>
        /// <param name="member"></param>
        /// <param name="typeData"></param>
        private void LoadMembersToTypeData(PSMemberInfo member, TypeData typeData)
        {
            Dbg.Assert(member != null, "caller should guarantee that member is not null");
            Dbg.Assert(typeData != null, "caller should guarantee that typeData is not null");

            var memberData = GetTypeMemberDataFromPSMemberInfo(member);
            if (memberData != null)
            {
                typeData.Members.Add(memberData.Name, memberData);
                return;
            }

            var memberSet = member as PSMemberSet;
            if (memberSet != null)
            {
                if (memberSet.Name.Equals(PSStandardMembers, StringComparison.OrdinalIgnoreCase))
                {
                    LoadStandardMembersToTypeData(memberSet, typeData);
                }
            }
        }

        /// <summary>
        /// Helper function to convert an object to a specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceValue"></param>
        /// <returns></returns>
        private static T GetParameterType<T>(object sourceValue)
        {
            return (T)LanguagePrimitives.ConvertTo(sourceValue, typeof(T), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Load the standard members into the passed-in TypeData.
        /// </summary>
        private void LoadStandardMembersToTypeData(PSMemberSet memberSet, TypeData typeData)
        {
            foreach (PSMemberInfo member in memberSet.InternalMembers)
            {
                PSMemberInfo standardMember = member.Copy();
                var memberName = standardMember.Name;
                if (memberName.Equals(SerializationMethodNode, StringComparison.OrdinalIgnoreCase))
                {
                    typeData.SerializationMethod = GetParameterType<string>(((PSNoteProperty)standardMember).Value);
                }
                else if (memberName.Equals(TargetTypeForDeserialization, StringComparison.OrdinalIgnoreCase))
                {
                    // Value property of the TargetTypeForDeserialization member is guaranteed to be of System.Type
                    typeData.TargetTypeForDeserialization = GetParameterType<Type>(((PSNoteProperty)standardMember).Value);
                }
                else if (memberName.Equals(SerializationDepth, StringComparison.OrdinalIgnoreCase))
                {
                    // Value property of the SerializationDepth member is guaranteed to be of type int
                    typeData.SerializationDepth = GetParameterType<uint>(((PSNoteProperty)standardMember).Value);
                }
                else if (memberName.Equals(DefaultDisplayProperty, StringComparison.OrdinalIgnoreCase))
                {
                    // Value property of the DefaultDisplayProperty is guaranteed to be of type string
                    typeData.DefaultDisplayProperty = GetParameterType<string>(((PSNoteProperty)standardMember).Value);
                }
                else if (memberName.Equals(InheritPropertySerializationSet, StringComparison.OrdinalIgnoreCase))
                {
                    // Value property of the InheritPropertySerializationSet is guaranteed to be of type bool
                    typeData.InheritPropertySerializationSet = GetParameterType<bool>(((PSNoteProperty)standardMember).Value);
                }
                else if (memberName.Equals(StringSerializationSource, StringComparison.OrdinalIgnoreCase))
                {
                    var aliasProperty = standardMember as PSAliasProperty;
                    if (aliasProperty != null)
                    {
                        typeData.StringSerializationSource = aliasProperty.ReferencedMemberName;
                    }
                    else
                    {
                        typeData.StringSerializationSourceProperty = GetTypeMemberDataFromPSMemberInfo(standardMember);
                    }
                }
                else if (memberName.Equals(DefaultDisplayPropertySet, StringComparison.OrdinalIgnoreCase))
                {
                    typeData.DefaultDisplayPropertySet =
                        new PropertySetData(((PSPropertySet)standardMember).ReferencedPropertyNames);
                }
                else if (memberName.Equals(DefaultKeyPropertySet, StringComparison.OrdinalIgnoreCase))
                {
                    typeData.DefaultKeyPropertySet =
                        new PropertySetData(((PSPropertySet)standardMember).ReferencedPropertyNames);
                }
                else if (memberName.Equals(PropertySerializationSet, StringComparison.OrdinalIgnoreCase))
                {
                    typeData.PropertySerializationSet =
                        new PropertySetData(((PSPropertySet)standardMember).ReferencedPropertyNames);
                }
            }
        }

        /// <summary>
        /// Get all Type configurations, return a Dictionary with typeName as the key, TypeData as the value.
        /// </summary>
        /// <returns></returns>
        internal Dictionary<string, TypeData> GetAllTypeData()
        {
            Dictionary<string, TypeData> allTypes = new Dictionary<string, TypeData>();
            foreach (string typeName in _extendedMembers.Keys)
            {
                if (allTypes.ContainsKey(typeName))
                {
                    continue;
                }

                TypeData typeData = new TypeData(typeName);
                bool isTypeDataLoaded = false;

                isTypeDataLoaded |= RetrieveMembersToTypeData(typeData);
                isTypeDataLoaded |= RetrieveConverterToTypeData(typeData);
                isTypeDataLoaded |= RetrieveAdapterToTypeData(typeData);

                if (isTypeDataLoaded)
                {
                    allTypes.Add(typeName, typeData);
                }
            }

            foreach (string typeName in _typeConverters.Keys)
            {
                if (allTypes.ContainsKey(typeName))
                {
                    continue;
                }

                TypeData typeData = new TypeData(typeName);
                bool isTypeDataLoaded = false;

                isTypeDataLoaded |= RetrieveConverterToTypeData(typeData);
                isTypeDataLoaded |= RetrieveAdapterToTypeData(typeData);

                if (isTypeDataLoaded)
                {
                    allTypes.Add(typeName, typeData);
                }
            }

            foreach (string typeName in _typeAdapters.Keys)
            {
                if (allTypes.ContainsKey(typeName))
                {
                    continue;
                }

                TypeData typeData = new TypeData(typeName);

                if (RetrieveAdapterToTypeData(typeData))
                {
                    allTypes.Add(typeName, typeData);
                }
            }

            return allTypes;
        }

        private bool RetrieveMembersToTypeData(TypeData typeData)
        {
            Dbg.Assert(typeData != null && typeData.TypeName != null, "The caller needs to make sure typeData != null");
            string typeName = typeData.TypeName;

            PSMemberInfoInternalCollection<PSMemberInfo> typeMembers;
            if (_extendedMembers.TryGetValue(typeName, out typeMembers))
            {
                Dbg.Assert(typeMembers != null, "members should not be null");
                foreach (PSMemberInfo member in typeMembers)
                {
                    PSMemberInfo newMember = member.Copy();
                    LoadMembersToTypeData(newMember, typeData);
                }

                return true;
            }

            return false;
        }

        private bool RetrieveConverterToTypeData(TypeData typeData)
        {
            Dbg.Assert(typeData != null && typeData.TypeName != null, "The caller needs to make sure typeData != null");
            string typeName = typeData.TypeName;

            object converterResult;
            if (_typeConverters.TryGetValue(typeName, out converterResult))
            {
                Dbg.Assert(converterResult != null, "converter should not be null");
                typeData.TypeConverter = converterResult.GetType();
                return true;
            }

            return false;
        }

        private bool RetrieveAdapterToTypeData(TypeData typeData)
        {
            Dbg.Assert(typeData != null && typeData.TypeName != null, "The caller needs to make sure typeData != null");
            string typeName = typeData.TypeName;

            PSObject.AdapterSet adapterResult;
            if (_typeAdapters.TryGetValue(typeName, out adapterResult))
            {
                Dbg.Assert(adapterResult != null, "adapter should not be null");
                typeData.TypeAdapter = ((ThirdPartyAdapter)(adapterResult.OriginalAdapter)).ExternalAdapterType;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clone the TypeTable by doing a shallow copy of all the members.
        /// </summary>
        /// <param name="unshared">
        /// Indicate that the clone of this TypeTable instance should not be marked as "Shared", even if
        /// this TypeTable instance itself is a shared TypeTable.
        /// </param>
        /// <returns>
        /// If <para>unshared</para> is True, return an unshared clone of this TypeTable instance
        /// If <para>unshared</para> is False, return a clone that is exactly the same as this TypeTable instance.
        /// </returns>
        public TypeTable Clone(bool unshared)
        {
            TypeTable result = unshared ? new TypeTable() : new TypeTable(this.isShared);

            // 1. Update the result typeTable with the current typeTable
            // 2. Shallow copy the 'typesInfo'

            // copy the type members
            foreach (var pair in _extendedMembers)
            {
                var resultMembers = new PSMemberInfoInternalCollection<PSMemberInfo>();
                foreach (var member in pair.Value)
                {
                    resultMembers.Add(member.Copy());
                }

                result._extendedMembers.TryAdd(pair.Key, resultMembers);
            }

            // copy the type adapter members
            foreach (var pair in _typeAdapters)
            {
                result._typeAdapters.TryAdd(pair.Key, pair.Value);
            }

            // copy the type converters
            foreach (var pair in _typeConverters)
            {
                result._typeConverters.TryAdd(pair.Key, pair.Value);
            }

            result.typesInfo.Add(this.typesInfo);

            return result;
        }

        /// <summary>
        /// Clear the TypeTable.
        /// </summary>
        internal void Clear()
        {
            foreach (var conv in _typeConverters.Keys)
            {
                LanguagePrimitives.UpdateTypeConvertFromTypeTable(conv);
            }

            _typeConverters.Clear();

            foreach (var ml in _extendedMembers.Values)
            {
                foreach (var m in ml)
                {
                    PSGetMemberBinder.TypeTableMemberPossiblyUpdated(m.Name);
                }
            }

            _extendedMembers.Clear();

            StandardMembersUpdated();

            _typeAdapters.Clear();
            typesInfo.Clear();

            ClearConsolidatedMembers();
        }

        internal void ClearConsolidatedMembers()
        {
            _consolidatedMembers.Clear();
            _consolidatedSpecificProperties.Clear();
        }

        private static void StandardMembersUpdated()
        {
            foreach (var sm in s_standardMembers)
            {
                PSGetMemberBinder.TypeTableMemberPossiblyUpdated(sm);
            }

            PSGetMemberBinder.TypeTableMemberPossiblyUpdated(PSStandardMembers);
        }

        /// <summary>
        /// Load the specified file and report the errors in <paramref name="errors."/>
        /// </summary>
        /// <param name="snapinName"></param>
        /// <param name="fileToLoad">
        /// Type file to load. File should be a fully-qualified path.
        /// </param>
        /// <param name="errors"></param>
        /// <param name="authorizationManager">
        /// Authorization manager to perform signature checks before reading ps1xml files (or null of no checks are needed)
        /// </param>
        /// <param name="host">
        /// Host passed to <paramref name="authorizationManager"/>.  Can be null if no interactive questions should be asked.
        /// </param>
        /// <param name="failToLoadFile">Indicate if the file failed to be loaded.</param>
        internal void Initialize(
            string snapinName,
            string fileToLoad,
            ConcurrentBag<string> errors,
            AuthorizationManager authorizationManager,
            PSHost host,
            out bool failToLoadFile)
        {
            if (ProcessIsBuiltIn(fileToLoad, errors, out failToLoadFile))
            {
                return;
            }

            bool isFullyTrusted;
            bool isProductCode;
            string fileContents = GetModuleContents(snapinName, fileToLoad, errors, authorizationManager, host, out isFullyTrusted, out failToLoadFile, out isProductCode);

            if (fileContents == null)
            {
                return;
            }

            UpdateWithModuleContents(fileContents, snapinName, fileToLoad, isFullyTrusted, isProductCode, errors);
        }

        /// <summary>
        /// Helper method to load content for a module.
        /// </summary>
        private static string GetModuleContents(
            string moduleName,
            string fileToLoad,
            ConcurrentBag<string> errors,
            AuthorizationManager authorizationManager,
            PSHost host,
            out bool isFullyTrusted,
            out bool failToLoadFile,
            out bool isProductCode)
        {
            Dbg.Assert(Path.IsPathRooted(fileToLoad), "fileToLoad should be a fully-qualified path.");

            ExternalScriptInfo ps1xmlInfo;
            string fileContents;
            isFullyTrusted = false;
            isProductCode = false;

            try
            {
                ps1xmlInfo = new ExternalScriptInfo(fileToLoad, fileToLoad);
                fileContents = ps1xmlInfo.ScriptContents;

                if (ps1xmlInfo.DefiningLanguageMode == PSLanguageMode.FullLanguage)
                {
                    isFullyTrusted = true;
                }

                if (SecuritySupport.IsProductBinary(fileToLoad))
                {
                    isProductCode = true;
                }
            }
            catch (SecurityException e)
            {
                string errorMsg = StringUtil.Format(TypesXmlStrings.Exception, e.Message);
                string errorLine = StringUtil.Format(TypesXmlStrings.FileError, moduleName, fileToLoad, errorMsg);
                errors.Add(errorLine);

                failToLoadFile = true;
                return null;
            }

            // Check authorization.
            if (authorizationManager != null)
            {
                try
                {
                    authorizationManager.ShouldRunInternal(ps1xmlInfo, CommandOrigin.Internal, host);
                }
                catch (PSSecurityException reason)
                {
                    string errorMessage = StringUtil.Format(TypesXmlStrings.ValidationException, moduleName, fileToLoad, reason.Message);
                    errors.Add(errorMessage);

                    failToLoadFile = true;
                    return null;
                }
            }

            failToLoadFile = false;
            return fileContents;
        }

        /// <summary>
        /// Helper method to update with module file contents.
        /// </summary>
        /// <param name="fileContents">Module contents.</param>
        /// <param name="moduleName">Module name.</param>
        /// <param name="fileToLoad">Module file path.</param>
        /// <param name="isFullyTrusted">Whether the module contents are fully trusted.</param>
        /// <param name="isProductCode">Whether the module contents are considered part of Windows (e.g. catalog signed).</param>
        /// <param name="errors">Errors.</param>
        private void UpdateWithModuleContents(
            string fileContents,
            string moduleName,
            string fileToLoad,
            bool isFullyTrusted,
            bool isProductCode,
            ConcurrentBag<string> errors)
        {
            LoadContext loadContext = new LoadContext(moduleName, fileToLoad, errors)
            {
                IsFullyTrusted = isFullyTrusted,
                IsProductCode = isProductCode,
            };

            using (StringReader xmlStream = new StringReader(fileContents))
            {
                XmlReader reader = new XmlTextReader(xmlStream) { WhitespaceHandling = WhitespaceHandling.Significant };
                loadContext.reader = reader;
                Update(loadContext);
                reader.Dispose();
            }
        }

        /// <summary>
        /// Update the TypeTable by adding a TypeData instance.
        /// </summary>
        /// <exception cref="PSArgumentNullException">Throw when the argument is null.</exception>
        /// <exception cref="RuntimeException">Throw when there were failures during the update.</exception>
        /// <param name="typeData">A TypeData instance to update the TypeTable.</param>
        public void AddType(TypeData typeData)
        {
            if (typeData == null)
                throw PSTraceSource.NewArgumentNullException(nameof(typeData));

            Dbg.Assert(isShared, "This method should only be called by the developer user. It should not be used internally.");

            ConcurrentBag<string> errors = new ConcurrentBag<string>();

            // Always clear the consolidate members - they need to be recalculated
            // anytime the types are updated...
            ClearConsolidatedMembers();

            Update(errors, typeData, false);
            StandardMembersUpdated();

            // Throw exception if there are any errors
            FormatAndTypeDataHelper.ThrowExceptionOnError("ErrorsUpdatingTypes", errors, FormatAndTypeDataHelper.Category.Types);
        }

        /// <summary>
        /// Remove all type information related to the type name.
        /// </summary>
        /// <exception cref="PSArgumentNullException">Throw when the argument is null or empty.</exception>
        /// <exception cref="RuntimeException">Throw if there were failures when remove the type.</exception>
        /// <param name="typeName">The name of the type to remove from TypeTable.</param>
        public void RemoveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(typeName));
            }

            Dbg.Assert(isShared, "This method should only be called by the developer user. It should not be used internally.");

            TypeData typeData = new TypeData(typeName);
            ConcurrentBag<string> errors = new ConcurrentBag<string>();

            // Always clear the consolidate members - they need to be recalculated
            // anytime the types are updated...
            ClearConsolidatedMembers();

            Update(errors, typeData, true);
            StandardMembersUpdated();

            // Throw exception if there are any errors
            FormatAndTypeDataHelper.ThrowExceptionOnError("ErrorsUpdatingTypes", errors, FormatAndTypeDataHelper.Category.Types);
        }

        /// <summary>
        /// Update type data from a specific file...
        /// </summary>
        /// <param name="moduleName">The name of the module or snapin that this file is associated with.</param>
        /// <param name="filePath">The path to the file to load.</param>
        /// <param name="errors">A place to put the errors...</param>
        /// <param name="authorizationManager">
        /// Authorization manager to perform signature checks before reading ps1xml files (or null of no checks are needed)
        /// </param>
        /// <param name="host">
        /// Host passed to <paramref name="authorizationManager"/>.  Can be null if no interactive questions should be asked.
        /// </param>
        /// <param name="failToLoadFile">Indicate if the file cannot be loaded due to security reason.</param>
        /// <exception cref="InvalidOperationException">
        /// 1. The TypeTable cannot be updated because the TypeTable might have
        /// been created outside of the Runspace.
        /// </exception>
        internal void Update(
            string moduleName,
            string filePath,
            ConcurrentBag<string> errors,
            AuthorizationManager authorizationManager,
            PSHost host,
            out bool failToLoadFile)
        {
            ArgumentNullException.ThrowIfNull(filePath);

            ArgumentNullException.ThrowIfNull(errors);

            if (isShared)
            {
                throw PSTraceSource.NewInvalidOperationException(TypesXmlStrings.SharedTypeTableCannotBeUpdated);
            }

            var etwEnabled = RunspaceEventSource.Log.IsEnabled();
            if (etwEnabled)
            {
                RunspaceEventSource.Log.ProcessTypeFileStart(filePath);
            }

            if (!ProcessIsBuiltIn(filePath, errors, out failToLoadFile))
            {
                // Get file contents and perform authorization check
                // (including possible user prompt) outside of the lock.
                bool isFullyTrusted;
                bool isProductCode;

                var fileContents = GetModuleContents(moduleName, filePath, errors, authorizationManager, host, out isFullyTrusted, out failToLoadFile, out isProductCode);

                if (fileContents != null)
                {
                    UpdateWithModuleContents(fileContents, moduleName, filePath, isFullyTrusted, isProductCode, errors);
                }
            }

            if (etwEnabled)
            {
                RunspaceEventSource.Log.ProcessTypeFileStop(filePath);
            }
        }

        private bool ProcessIsBuiltIn(string filePath, ConcurrentBag<string> errors, out bool failToLoadFile)
        {
            var result = false;
            var errorCount = errors.Count;

            if (string.Equals(TypesFilePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                Process_Types_Ps1Xml(filePath, errors);
                result = true;
            }
            else if (string.Equals(TypesV3FilePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                Process_TypesV3_Ps1Xml(filePath, errors);
                result = true;
            }
            else if (string.Equals(GetEventTypesFilePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                Process_GetEvent_Types_Ps1Xml(filePath, errors);
                result = true;
            }

            failToLoadFile = errorCount < errors.Count;
            return result;
        }

        /// <summary>
        /// Update typetable from a specific strong type data.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="errors"></param>
        /// <param name="isRemove"></param>
        internal void Update(
            TypeData type,
            ConcurrentBag<string> errors,
            bool isRemove)
        {
            ArgumentNullException.ThrowIfNull(type);

            ArgumentNullException.ThrowIfNull(errors);

            if (isShared)
            {
                throw PSTraceSource.NewInvalidOperationException(TypesXmlStrings.SharedTypeTableCannotBeUpdated);
            }

            Update(errors, type, isRemove);
            StandardMembersUpdated();
        }

        #endregion internal methods
    }
}
