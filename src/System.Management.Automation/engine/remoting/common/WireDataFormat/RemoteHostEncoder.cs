// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation.Host;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Encodes and decodes data types and exceptions for transmission across
    /// the wire. Used for transmitting remote host method call parameters, return
    /// values, and exceptions. The convention is that EncodeObject converts the
    /// objects into a type that can be serialized and deserialized without losing
    /// fidelity. For example, EncodeObject converts Version objects to string,
    /// and converts more complex classes into property bags on PSObjects. This
    /// guarantees that transmitting on the wire will not change the encoded
    /// object's type.
    /// </summary>
    internal static class RemoteHostEncoder
    {
        /// <summary>
        /// Is known type.
        /// </summary>
        private static bool IsKnownType(Type type)
        {
            TypeSerializationInfo info = KnownTypes.GetTypeSerializationInfo(type);
            return (info != null);
        }

        /// <summary>
        /// Is encoding allowed for class or struct.
        /// </summary>
        private static bool IsEncodingAllowedForClassOrStruct(Type type)
        {
            // To enable encoding and decoding for a class or struct, add it here.
            return
                // Struct types.
                type == typeof(KeyInfo) ||
                type == typeof(Coordinates) ||
                type == typeof(Size) ||
                type == typeof(BufferCell) ||
                type == typeof(Rectangle) ||

                // Class types.
                type == typeof(ProgressRecord) ||
                type == typeof(FieldDescription) ||
                type == typeof(ChoiceDescription) ||
                type == typeof(HostInfo) ||
                type == typeof(HostDefaultData) ||
                type == typeof(RemoteSessionCapability);
        }

        /// <summary>
        /// Encode class or struct.
        /// </summary>
        private static PSObject EncodeClassOrStruct(object obj)
        {
            PSObject psObject = RemotingEncoder.CreateEmptyPSObject();
            FieldInfo[] fieldInfos = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Add all the non-null field values to the ps object.
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                object fieldValue = fieldInfo.GetValue(obj);
                if (fieldValue == null)
                {
                    continue;
                }

                object encodedFieldValue = EncodeObject(fieldValue);
                psObject.Properties.Add(new PSNoteProperty(fieldInfo.Name, encodedFieldValue));
            }

            return psObject;
        }

        /// <summary>
        /// Decode class or struct.
        /// </summary>
        private static object DecodeClassOrStruct(PSObject psObject, Type type)
        {
            object obj = FormatterServices.GetUninitializedObject(type);

            // Field values cannot be null - because for null fields we simply don't transport them.
            foreach (PSPropertyInfo propertyInfo in psObject.Properties)
            {
                FieldInfo fieldInfo = type.GetField(propertyInfo.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (propertyInfo.Value == null) { throw RemoteHostExceptions.NewDecodingFailedException(); }

                object fieldValue = DecodeObject(propertyInfo.Value, fieldInfo.FieldType);
                if (fieldValue == null) { throw RemoteHostExceptions.NewDecodingFailedException(); }

                fieldInfo.SetValue(obj, fieldValue);
            }

            return obj;
        }

        /// <summary>
        /// Is collection.
        /// </summary>
        private static bool IsCollection(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Collection<>));
        }

        private static bool IsGenericIEnumerableOfInt(Type type)
        {
            return type.Equals(typeof(IEnumerable<int>));
        }

        /// <summary>
        /// Encode collection.
        /// </summary>
        private static PSObject EncodeCollection(IList collection)
        {
            ArrayList arrayList = new ArrayList();
            foreach (object obj in collection)
            {
                arrayList.Add(EncodeObject(obj));
            }

            return new PSObject(arrayList);
        }

        /// <summary>
        /// Decode collection.
        /// </summary>
        private static IList DecodeCollection(PSObject psObject, Type collectionType)
        {
            // Get the element type.
            Type[] elementTypes = collectionType.GetGenericArguments();
            Dbg.Assert(elementTypes.Length == 1, "Expected elementTypes.Length == 1");
            Type elementType = elementTypes[0];

            // Rehydrate the collection from the array list.
            ArrayList arrayList = SafelyGetBaseObject<ArrayList>(psObject);
            IList collection = (IList)Activator.CreateInstance(collectionType);
            foreach (object element in arrayList)
            {
                collection.Add(DecodeObject(element, elementType));
            }

            return collection;
        }

        /// <summary>
        /// Is dictionary.
        /// </summary>
        private static bool IsDictionary(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Dictionary<,>));
        }

        /// <summary>
        /// Encode dictionary.
        /// </summary>
        private static PSObject EncodeDictionary(IDictionary dictionary)
        {
            // If element type is object then encode as object-dictionary.
            if (IsObjectDictionaryType(dictionary.GetType()))
            {
                return EncodeObjectDictionary(dictionary);
            }

            Hashtable hashtable = new Hashtable();
            foreach (object key in dictionary.Keys)
            {
                hashtable.Add(EncodeObject(key), EncodeObject(dictionary[key]));
            }

            return new PSObject(hashtable);
        }

        /// <summary>
        /// Decode dictionary.
        /// </summary>
        private static IDictionary DecodeDictionary(PSObject psObject, Type dictionaryType)
        {
            // If element type is object then decode as object-dictionary.
            if (IsObjectDictionaryType(dictionaryType))
            {
                return DecodeObjectDictionary(psObject, dictionaryType);
            }

            // Get the element type.
            Type[] elementTypes = dictionaryType.GetGenericArguments();
            Dbg.Assert(elementTypes.Length == 2, "Expected elementTypes.Length == 2");
            Type keyType = elementTypes[0];
            Type valueType = elementTypes[1];

            // Rehydrate the dictionary from the hashtable.
            Hashtable hashtable = SafelyGetBaseObject<Hashtable>(psObject);
            IDictionary dictionary = (IDictionary)Activator.CreateInstance(dictionaryType);
            foreach (object key in hashtable.Keys)
            {
                dictionary.Add(
                    DecodeObject(key, keyType),
                    DecodeObject(hashtable[key], valueType));
            }

            return dictionary;
        }

        /// <summary>
        /// Encode ps object.
        /// </summary>
        private static PSObject EncodePSObject(PSObject psObject)
        {
            // We are not encoding the contents of the PSObject since these can be
            // arbitrarily complex. Only objects that can be serialized and deserialized
            // correctly should be wrapped in PSObjects. This logic should also just do
            // the right thing with PSObject subclasses. Note: This method might seem
            // trivial but it must exist in order to maintain the symmetry with
            // DecodePSObject. There is no perf penalty because: (a) the compiler should
            // inline it, and (b) perf optimization should be based on profiling;
            // premature optimization is the root of all evil.

            return psObject;
        }

        /// <summary>
        /// Decode ps object.
        /// </summary>
        private static PSObject DecodePSObject(object obj)
        {
            if (obj is PSObject)
            {
                return (PSObject)obj;
            }
            else
            {
                // If this is not a PSObject, wrap it in one. This case needs to be handled
                // because the serializer converts Dictionary<string,PSObject> to a Hashtable
                // mapping strings to strings, when the PSObject is a simple wrapper around
                // string.
                return new PSObject(obj);
            }
        }

        /// <summary>
        /// Encode exception.
        /// </summary>
        private static PSObject EncodeException(Exception exception)
        {
            // We are encoding exceptions as ErrorRecord objects because exceptions written
            // to the wire are lost during serialization. By sending across ErrorRecord objects
            // we are able to preserve the exception as well as the stack trace.

            ErrorRecord errorRecord = null;
            IContainsErrorRecord containsErrorRecord = exception as IContainsErrorRecord;
            if (containsErrorRecord == null)
            {
                // If this is a .NET exception then wrap in an ErrorRecord.
                errorRecord = new ErrorRecord(exception, "RemoteHostExecutionException", ErrorCategory.NotSpecified, null);
            }
            else
            {
                // Exception inside the error record is ParentContainsErrorRecordException which
                // doesn't have stack trace. Replace it with top level exception.
                errorRecord = containsErrorRecord.ErrorRecord;
                errorRecord = new ErrorRecord(errorRecord, exception);
            }

            PSObject errorRecordPSObject = RemotingEncoder.CreateEmptyPSObject();
            errorRecord.ToPSObjectForRemoting(errorRecordPSObject);
            return errorRecordPSObject;
        }

        /// <summary>
        /// Decode exception.
        /// </summary>
        private static Exception DecodeException(PSObject psObject)
        {
            ErrorRecord errorRecord = ErrorRecord.FromPSObjectForRemoting(psObject);
            if (errorRecord == null)
            {
                throw RemoteHostExceptions.NewDecodingErrorForErrorRecordException();
            }
            else
                return errorRecord.Exception;
        }

        /// <summary>
        /// Upcast field description subclass and drop attributes.
        /// </summary>
        private static FieldDescription UpcastFieldDescriptionSubclassAndDropAttributes(FieldDescription fieldDescription1)
        {
            // Upcasts derived types back to FieldDescription type and throws away attributes.

            // Create a new field description object.
            FieldDescription fieldDescription2 = new FieldDescription(fieldDescription1.Name);

            // Copy the fields not initialized during construction.
            fieldDescription2.Label = fieldDescription1.Label;
            fieldDescription2.HelpMessage = fieldDescription1.HelpMessage;
            fieldDescription2.IsMandatory = fieldDescription1.IsMandatory;
            fieldDescription2.DefaultValue = fieldDescription1.DefaultValue;

            // Set the type related fields.
            fieldDescription2.SetParameterTypeName(fieldDescription1.ParameterTypeName);
            fieldDescription2.SetParameterTypeFullName(fieldDescription1.ParameterTypeFullName);
            fieldDescription2.SetParameterAssemblyFullName(fieldDescription1.ParameterAssemblyFullName);

            return fieldDescription2;
        }

        /// <summary>
        /// Encode object.
        /// </summary>
        internal static object EncodeObject(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            Type type = obj.GetType();
            if (obj is PSObject)
            {
                // The "is" keyword takes care of PSObject and subclasses.
                return EncodePSObject((PSObject)obj);
            }
            else if (obj is ProgressRecord)
            {
                return ((ProgressRecord)obj).ToPSObjectForRemoting();
            }
            else if (IsKnownType(type))
            {
                return obj;
            }
            else if (type.IsEnum)
            {
                return (int)obj;
            }
            else if (obj is CultureInfo)
            {
                // The "is" keyword takes care of CultureInfo and subclasses.
                return obj.ToString();
            }
            else if (obj is Exception)
            {
                return EncodeException((Exception)obj);
            }
            else if (type == typeof(object[]))
            {
                return EncodeObjectArray((object[])obj);
            }
            else if (type.IsArray)
            {
                return EncodeArray((Array)obj);
            }
            else if (obj is IList && IsCollection(type))
            {
                return EncodeCollection((IList)obj);
            }
            else if (obj is IDictionary && IsDictionary(type))
            {
                return EncodeDictionary((IDictionary)obj);
            }
            else if (type.IsSubclassOf(typeof(FieldDescription)) || type == typeof(FieldDescription))
            {
                // The upcasting removes the Attributes, so we want to do this both when it
                // is a subclass and when it is a FieldDescription object.
                return EncodeClassOrStruct(UpcastFieldDescriptionSubclassAndDropAttributes((FieldDescription)obj));
            }
            else if (IsEncodingAllowedForClassOrStruct(type))
            {
                return EncodeClassOrStruct(obj);
            }
            else if (obj is RemoteHostCall)
            {
                return ((RemoteHostCall)obj).Encode();
            }
            else if (obj is RemoteHostResponse)
            {
                return ((RemoteHostResponse)obj).Encode();
            }
            else if (obj is SecureString)
            {
                return obj;
            }
            else if (obj is PSCredential)
            {
                return obj;
            }
            else if (IsGenericIEnumerableOfInt(type))
            {
                return EncodeCollection((IList)obj);
            }
            else
            {
                throw RemoteHostExceptions.NewRemoteHostDataEncodingNotSupportedException(type);
            }
        }

        /// <summary>
        /// Decode object.
        /// </summary>
        internal static object DecodeObject(object obj, Type type)
        {
            if (obj == null)
            {
                return null;
            }

            Dbg.Assert(type != null, "Expected type != null");
            if (type == typeof(PSObject))
            {
                return DecodePSObject(obj);
            }
            else if (type == typeof(ProgressRecord))
            {
                return ProgressRecord.FromPSObjectForRemoting(PSObject.AsPSObject(obj));
            }
            else if (IsKnownType(type))
            {
                return obj;
            }
            else if (obj is SecureString)
            {
                return obj;
            }
            else if (obj is PSCredential)
            {
                return obj;
            }
            else if (obj is PSObject && type == typeof(PSCredential))
            {
                // BUGBUG: The following piece of code is a workaround
                // because custom serialization is busted. If rehydration
                // works correctly then PSCredential should be available
                PSObject objAsPSObject = (PSObject)obj;
                PSCredential cred = null;
                try
                {
                    cred = new PSCredential((string)objAsPSObject.Properties["UserName"].Value,
                                            (SecureString)objAsPSObject.Properties["Password"].Value);
                }
                catch (GetValueException)
                {
                    cred = null;
                }

                return cred;
            }
            else if (obj is int && type.IsEnum)
            {
                return Enum.ToObject(type, (int)obj);
            }
            else if (obj is string && type == typeof(CultureInfo))
            {
                return new CultureInfo((string)obj);
            }
            else if (obj is PSObject && type == typeof(Exception))
            {
                return DecodeException((PSObject)obj);
            }
            else if (obj is PSObject && type == typeof(object[]))
            {
                return DecodeObjectArray((PSObject)obj);
            }
            else if (obj is PSObject && type.IsArray)
            {
                return DecodeArray((PSObject)obj, type);
            }
            else if (obj is PSObject && IsCollection(type))
            {
                return DecodeCollection((PSObject)obj, type);
            }
            else if (obj is PSObject && IsDictionary(type))
            {
                return DecodeDictionary((PSObject)obj, type);
            }
            else if (obj is PSObject && IsEncodingAllowedForClassOrStruct(type))
            {
                return DecodeClassOrStruct((PSObject)obj, type);
            }
            else if (obj is PSObject && IsGenericIEnumerableOfInt(type))
            {
                // we cannot create an instance of interface type like IEnumerable
                // Since a Collection implements IEnumerable, falling back to use
                // that.
                return DecodeCollection((PSObject)obj, typeof(Collection<int>));
            }
            else if (obj is PSObject && type == typeof(RemoteHostCall))
            {
                return RemoteHostCall.Decode((PSObject)obj);
            }
            else if (obj is PSObject && type == typeof(RemoteHostResponse))
            {
                return RemoteHostResponse.Decode((PSObject)obj);
            }
            else
            {
                throw RemoteHostExceptions.NewRemoteHostDataDecodingNotSupportedException(type);
            }
        }

        /// <summary>
        /// Encode and add as property.
        /// </summary>
        internal static void EncodeAndAddAsProperty(PSObject psObject, string propertyName, object propertyValue)
        {
            Dbg.Assert(psObject != null, "Expected psObject != null");
            Dbg.Assert(propertyName != null, "Expected propertyName != null");
            if (propertyValue == null)
            {
                return;
            }

            psObject.Properties.Add(new PSNoteProperty(propertyName, EncodeObject(propertyValue)));
        }

        /// <summary>
        /// Decode property value.
        /// </summary>
        internal static object DecodePropertyValue(PSObject psObject, string propertyName, Type propertyValueType)
        {
            Dbg.Assert(psObject != null, "Expected psObject != null");
            Dbg.Assert(propertyName != null, "Expected propertyName != null");
            Dbg.Assert(propertyValueType != null, "Expected propertyValueType != null");
            ReadOnlyPSMemberInfoCollection<PSPropertyInfo> matches = psObject.Properties.Match(propertyName);
            if (matches.Count == 0)
            {
                return null;
            }

            Dbg.Assert(matches.Count == 1, "Expected matches.Count == 1");
            return DecodeObject(matches[0].Value, propertyValueType);
        }

        /// <summary>
        /// Encode object array.
        /// </summary>
        private static PSObject EncodeObjectArray(object[] objects)
        {
            ArrayList arrayList = new ArrayList();
            foreach (object obj in objects)
            {
                arrayList.Add(EncodeObjectWithType(obj));
            }

            return new PSObject(arrayList);
        }

        /// <summary>
        /// Decode object array.
        /// </summary>
        private static object[] DecodeObjectArray(PSObject psObject)
        {
            // Rehydrate the array from the array list.
            ArrayList arrayList = SafelyGetBaseObject<ArrayList>(psObject);
            object[] objects = new object[arrayList.Count];
            for (int i = 0; i < arrayList.Count; ++i)
            {
                objects[i] = DecodeObjectWithType(arrayList[i]);
            }

            return objects;
        }

        /// <summary>
        /// Encode object with type.
        /// </summary>
        private static PSObject EncodeObjectWithType(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            PSObject psObject = RemotingEncoder.CreateEmptyPSObject();
            psObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ObjectType, obj.GetType().ToString()));
            psObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ObjectValue, EncodeObject(obj)));
            return psObject;
        }

        /// <summary>
        /// Decode object with type.
        /// </summary>
        private static object DecodeObjectWithType(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            PSObject psObject = SafelyCastObject<PSObject>(obj);
            string typeName = SafelyGetPropertyValue<string>(psObject, RemoteDataNameStrings.ObjectType);
            Type type = LanguagePrimitives.ConvertTo<Type>(typeName);
            object val = SafelyGetPropertyValue<object>(psObject, RemoteDataNameStrings.ObjectValue);
            return DecodeObject(val, type);
        }

        /// <summary>
        /// Array is zero based.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static bool ArrayIsZeroBased(Array array)
        {
            int rank = array.Rank;
            for (int i = 0; i < rank; ++i)
            {
                if (array.GetLowerBound(i) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Encode array.
        /// </summary>
        private static PSObject EncodeArray(Array array)
        {
            Dbg.Assert(array != null, "Expected array != null");
            Dbg.Assert(ArrayIsZeroBased(array), "Expected ArrayIsZeroBased(array)");
            Type arrayType = array.GetType();
            Type elementType = arrayType.GetElementType();
            int rank = array.Rank;
            int[] lengths = new int[rank];
            for (int i = 0; i < rank; ++i)
            {
                lengths[i] = array.GetUpperBound(i) + 1;
            }

            Indexer indexer = new Indexer(lengths);
            ArrayList elements = new ArrayList();
            foreach (int[] index in indexer)
            {
                object elementValue = array.GetValue(index);
                elements.Add(EncodeObject(elementValue));
            }

            PSObject psObject = RemotingEncoder.CreateEmptyPSObject();
            psObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MethodArrayElements, elements));
            psObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MethodArrayLengths, lengths));
            return psObject;
        }

        /// <summary>
        /// Decode array.
        /// </summary>
        private static Array DecodeArray(PSObject psObject, Type type)
        {
            // Extract the type.
            Dbg.Assert(type.IsArray, "Expected type.IsArray");
            Type elementType = type.GetElementType();

            // Extract elements from psObject.
            PSObject psObjectContainingElements = SafelyGetPropertyValue<PSObject>(psObject, RemoteDataNameStrings.MethodArrayElements);
            ArrayList elements = SafelyGetBaseObject<ArrayList>(psObjectContainingElements);

            // Extract lengths from psObject.
            PSObject psObjectContainingLengths = SafelyGetPropertyValue<PSObject>(psObject, RemoteDataNameStrings.MethodArrayLengths);
            ArrayList lengthsArrayList = SafelyGetBaseObject<ArrayList>(psObjectContainingLengths);
            int[] lengths = (int[])lengthsArrayList.ToArray(typeof(int));

            // Reconstitute the array.
            Indexer indexer = new Indexer(lengths);
            Array array = Array.CreateInstance(elementType, lengths);
            int elementIndex = 0;
            foreach (int[] index in indexer)
            {
                object elementValue = DecodeObject(elements[elementIndex++], elementType);
                array.SetValue(elementValue, index);
            }

            return array;
        }

        /// <summary>
        /// Is object dictionary type.
        /// </summary>
        private static bool IsObjectDictionaryType(Type dictionaryType)
        {
            // True if the value-type of the dictionary is object; false otherwise.
            if (!IsDictionary(dictionaryType)) { return false; }

            Type[] elementTypes = dictionaryType.GetGenericArguments();
            if (elementTypes.Length != 2) { return false; }

            Type valueType = elementTypes[1];
            return valueType == typeof(object);
        }

        /// <summary>
        /// Encode object dictionary.
        /// </summary>
        private static PSObject EncodeObjectDictionary(IDictionary dictionary)
        {
            Dbg.Assert(IsObjectDictionaryType(dictionary.GetType()), "Expected IsObjectDictionaryType(dictionary.GetType())");

            // Encode the dictionary as a hashtable.
            Hashtable hashtable = new Hashtable();
            foreach (object key in dictionary.Keys)
            {
                hashtable.Add(EncodeObject(key), EncodeObjectWithType(dictionary[key]));
            }

            return new PSObject(hashtable);
        }

        /// <summary>
        /// Decode object dictionary.
        /// </summary>
        private static IDictionary DecodeObjectDictionary(PSObject psObject, Type dictionaryType)
        {
            Dbg.Assert(IsObjectDictionaryType(dictionaryType), "Expected IsObjectDictionaryType(dictionaryType)");

            // Get the element type.
            Type[] elementTypes = dictionaryType.GetGenericArguments();
            Dbg.Assert(elementTypes.Length == 2, "Expected elementTypes.Length == 2");
            Type keyType = elementTypes[0];
            Type valueType = elementTypes[1];
            Dbg.Assert(valueType == typeof(object), "Expected valueType == typeof(object)");

            // Rehydrate the dictionary from the hashtable.
            Hashtable hashtable = SafelyGetBaseObject<Hashtable>(psObject);
            IDictionary dictionary = (IDictionary)Activator.CreateInstance(dictionaryType);
            foreach (object key in hashtable.Keys)
            {
                dictionary.Add(
                    DecodeObject(key, keyType),
                    DecodeObjectWithType(hashtable[key]));
            }

            return dictionary;
        }

        /// <summary>
        /// Safely get base object.
        /// </summary>
        private static T SafelyGetBaseObject<T>(PSObject psObject)
        {
            if (psObject == null || psObject.BaseObject == null || psObject.BaseObject is not T)
            {
                throw RemoteHostExceptions.NewDecodingFailedException();
            }

            return (T)psObject.BaseObject;
        }

        /// <summary>
        /// Safely cast object.
        /// </summary>
        private static T SafelyCastObject<T>(object obj)
        {
            if (obj is T)
            {
                return (T)obj;
            }

            throw RemoteHostExceptions.NewDecodingFailedException();
        }

        /// <summary>
        /// Safely get property value.
        /// </summary>
        private static T SafelyGetPropertyValue<T>(PSObject psObject, string key)
        {
            PSPropertyInfo propertyInfo = psObject.Properties[key];
            if (propertyInfo == null || propertyInfo.Value == null || propertyInfo.Value is not T)
            {
                throw RemoteHostExceptions.NewDecodingFailedException();
            }

            return (T)propertyInfo.Value;
        }
    }
}
