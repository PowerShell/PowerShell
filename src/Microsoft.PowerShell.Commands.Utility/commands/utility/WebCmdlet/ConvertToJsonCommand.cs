/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Management.Automation;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Globalization;
using Dbg = System.Management.Automation;
using System.Management.Automation.Internal;
#if CORECLR
using Newtonsoft.Json;
#else
using System.Collections.Specialized;
using System.Web.Script.Serialization;
#endif

// FxCop suppressions for resource strings:
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "WebCmdletStrings.resources", MessageId = "json")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "WebCmdletStrings.resources", MessageId = "Json")]

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The ConvertTo-Json command
    /// This command convert an object to a Json string representation
    /// </summary>
    [Cmdlet(VerbsData.ConvertTo, "Json", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=217032", RemotingCapability = RemotingCapability.None)]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public class ConvertToJsonCommand : PSCmdlet
    {
        #region parameters
        /// <summary>
        /// gets or sets the InputObject property
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowNull]
        public object InputObject { get; set; }

        private int _depth = 2;
        private const int maxDepthAllowed = 100;

        /// <summary>
        /// gets or sets the Depth property
        /// </summary>
        [Parameter]
        [ValidateRange(1, int.MaxValue)]
        public int Depth { get { return _depth; } set { _depth = value; } }

        /// <summary>
        /// gets or sets the Compress property.
        /// If the Compress property is set to be true, the Json string will
        /// be output in the compressed way. Otherwise, the Json string will
        /// be output with indentations.
        /// </summary>
        [Parameter]
        public SwitchParameter Compress { get; set; }

        #endregion parameters

        #region overrides

        /// <summary>
        /// Prerequisite checks
        /// </summary>
        protected override void BeginProcessing()
        {
            if (_depth > maxDepthAllowed)
            {
                string errorMessage = StringUtil.Format(WebCmdletStrings.ReachedMaximumDepthAllowed, maxDepthAllowed);
                ThrowTerminatingError(new ErrorRecord(
                                new InvalidOperationException(errorMessage),
                                "ReachedMaximumDepthAllowed",
                                ErrorCategory.InvalidOperation,
                                null));
            }
#if CORECLR
            JsonObject.ImportJsonDotNetModule(this);
#else
            try
            {
                System.Reflection.Assembly.Load(new AssemblyName("System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"));
            }
            catch (System.IO.FileNotFoundException)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new NotSupportedException(WebCmdletStrings.ExtendedProfileRequired),
                    "ExtendedProfileRequired",
                    ErrorCategory.NotInstalled,
                    null));
            }
#endif
        }

        private List<object> _inputObjects = new List<object>();

        /// <summary>
        /// Caching the input objects for the convertto-json command
        /// </summary>
        protected override void ProcessRecord()
        {
            if (InputObject != null)
            {
                _inputObjects.Add(InputObject);
            }
        }

        /// <summary>
        /// Do the conversion to json and write output
        /// </summary>
        protected override void EndProcessing()
        {
            if (_inputObjects.Count > 0)
            {
                object objectToProcess = (_inputObjects.Count > 1) ? (_inputObjects.ToArray() as object) : (_inputObjects[0]);
                // Pre-process the object so that it serializes the same, except that properties whose
                // values cannot be evaluated are treated as having the value null.
                object preprocessedObject = ProcessValue(objectToProcess, 0);
#if CORECLR
                string output = JsonConvert.SerializeObject(preprocessedObject, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.None, MaxDepth = 1024 });
#else
                // In Full CLR, we use the JavaScriptSerializer for which RecursionLimit was set to the default value of 100 (the actual recursion limit is 99 since
                // at 100 the exception is thrown). See https://msdn.microsoft.com/en-us/library/system.web.script.serialization.javascriptserializer.recursionlimit(v=vs.110).aspx
                // ProcessValue creates an object to be serialized from 1 to depth. However, the properties of the object at 'depth' should also be serialized, 
                // and from the perspective of the serializer, this means it needs to support serializing depth + 1. For the JavaScriptSerializer to support this, 
                // RecursionLimit needs to be set to depth + 2.
                JavaScriptSerializer helper = new JavaScriptSerializer() { RecursionLimit = (maxDepthAllowed + 2) };
                helper.MaxJsonLength = Int32.MaxValue;
                string output = helper.Serialize(preprocessedObject);
#endif
                WriteObject(Compress ? output : ConvertToPrettyJsonString(output));
            }
        }

        #endregion overrides

        #region convertOutputToPrettierFormat

        /// <summary>
        /// Convert the Json string to a more readable format
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private string ConvertToPrettyJsonString(string json)
        {
            if (!json.StartsWith("{", StringComparison.OrdinalIgnoreCase) && !json.StartsWith("[", StringComparison.OrdinalIgnoreCase))
            {
                return json;
            }

            StringBuilder retStr = new StringBuilder();
            if (json.StartsWith("{", StringComparison.OrdinalIgnoreCase))
            {
                retStr.Append('{');
                ConvertDictionary(json, 1, retStr, "", 0);
            }
            else if (json.StartsWith("[", StringComparison.OrdinalIgnoreCase))
            {
                retStr.Append('[');
                ConvertList(json, 1, retStr, "", 0);
            }

            return retStr.ToString();
        }

        /// <summary>
        /// Convert a Json List, which starts with '['.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="index"></param>
        /// <param name="result"></param>
        /// <param name="padString"></param>
        /// <param name="numberOfSpaces"></param>
        /// <returns></returns>
        private int ConvertList(string json, int index, StringBuilder result, string padString, int numberOfSpaces)
        {
            result.Append("\r\n");
            StringBuilder newPadString = new StringBuilder();
            newPadString.Append(padString);
            AddSpaces(numberOfSpaces, newPadString);
            AddIndentations(1, newPadString);

            bool headChar = true;

            for (int i = index; i < json.Length; i++)
            {
                switch (json[i])
                {
                    case '{':
                        result.Append(newPadString.ToString());
                        result.Append(json[i]);
                        i = ConvertDictionary(json, i + 1, result, newPadString.ToString(), 0);
                        headChar = false;
                        break;
                    case '[':
                        result.Append(newPadString.ToString());
                        result.Append(json[i]);
                        i = ConvertList(json, i + 1, result, newPadString.ToString(), 0);
                        headChar = false;
                        break;
                    case ']':
                        result.Append("\r\n");
                        result.Append(padString);
                        AddSpaces(numberOfSpaces, result);
                        result.Append(json[i]);
                        return i;
                    case '"':
                        if (headChar)
                        {
                            result.Append(newPadString.ToString());
                        }
                        result.Append(json[i]);
                        i = ConvertQuotedString(json, i + 1, result);
                        headChar = false;
                        break;
                    case ',':
                        result.Append(json[i]);
                        result.Append("\r\n");
                        headChar = true;
                        break;
                    default:
                        if (headChar)
                        {
                            result.Append(newPadString.ToString());
                        }
                        result.Append(json[i]);
                        headChar = false;
                        break;
                }
            }

            Dbg.Diagnostics.Assert(false, "ConvertDictionary should return when encounter '}'");
            ThrowTerminatingError(NewError());
            return -1;
        }

        /// <summary>
        /// Convert the quoted string.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="index"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private int ConvertQuotedString(string json, int index, StringBuilder result)
        {
            for (int i = index; i < json.Length; i++)
            {
                result.Append(json[i]);
                if (json[i] == '"')
                {
                    // Ensure that the quote is not escaped by iteratively searching backwards for the backslash.
                    // Examples: 
                    //      "a \" b" --> here second quote is escaped
                    //      "c:\\"  --> here second quote is not escaped
                    //
                    var j = i;
                    var escaped = false;
                    while (j > 0 && json[--j] == '\\')
                    {
                        escaped = !escaped;
                    }

                    if (!escaped)
                    {
                        return i;
                    }
                }
            }

            Dbg.Diagnostics.Assert(false, "ConvertDictionary should return when encounter '}'");
            ThrowTerminatingError(NewError());
            return -1;
        }

        /// <summary>
        /// Convert a Json dictionary, which starts with '{'.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="index"></param>
        /// <param name="result"></param>
        /// <param name="padString"></param>
        /// <param name="numberOfSpaces"></param>
        /// <returns></returns>
        private int ConvertDictionary(string json, int index, StringBuilder result, string padString, int numberOfSpaces)
        {
            result.Append("\r\n");
            StringBuilder newPadString = new StringBuilder();
            newPadString.Append(padString);
            AddSpaces(numberOfSpaces, newPadString);
            AddIndentations(1, newPadString);

            bool headChar = true;
            bool beforeQuote = true;
            int newSpaceCount = 0;
            const int spaceCountAfterQuoteMark = 2;

            for (int i = index; i < json.Length; i++)
            {
                switch (json[i])
                {
                    case '{':
                        result.Append(json[i]);
                        i = ConvertDictionary(json, i + 1, result, newPadString.ToString(), newSpaceCount);
                        headChar = false;
                        break;
                    case '[':
                        result.Append(json[i]);
                        i = ConvertList(json, i + 1, result, newPadString.ToString(), newSpaceCount);
                        headChar = false;
                        break;
                    case '}':
                        result.Append("\r\n");
                        result.Append(padString);
                        AddSpaces(numberOfSpaces, result);
                        result.Append(json[i]);
                        return i;
                    case '"':
                        if (headChar)
                        {
                            result.Append(newPadString.ToString());
                        }
                        result.Append(json[i]);
                        int end = ConvertQuotedString(json, i + 1, result);
                        if (beforeQuote)
                        {
                            newSpaceCount += (end - i + 1);
                        }
                        i = end;
                        headChar = false;
                        break;
                    case ':':
                        result.Append(json[i]);
                        AddSpaces(spaceCountAfterQuoteMark, result);
                        newSpaceCount += 3;
                        headChar = false;
                        beforeQuote = false;
                        break;
                    case ',':
                        result.Append(json[i]);
                        result.Append("\r\n");
                        headChar = true;
                        beforeQuote = true;
                        newSpaceCount = 0;
                        break;
                    default:
                        if (headChar)
                        {
                            result.Append(newPadString.ToString());
                        }
                        result.Append(json[i]);
                        if (beforeQuote)
                        {
                            newSpaceCount += 1;
                        }
                        headChar = false;
                        break;
                }
            }

            Dbg.Diagnostics.Assert(false, "ConvertDictionary should return when encounter '}'");
            ThrowTerminatingError(NewError());
            return -1;
        }

        /// <summary>
        /// Add tabs to result
        /// </summary>
        /// <param name="numberOfTabsToReturn"></param>
        /// <param name="result"></param>
        private void AddIndentations(int numberOfTabsToReturn, StringBuilder result)
        {
            int realNumber = numberOfTabsToReturn * 4;
            for (int i = 0; i < realNumber; i++)
            {
                result.Append(' ');
            }
        }

        /// <summary>
        /// Add spaces to result
        /// </summary>
        /// <param name="numberOfSpacesToReturn"></param>
        /// <param name="result"></param>
        private void AddSpaces(int numberOfSpacesToReturn, StringBuilder result)
        {
            for (int i = 0; i < numberOfSpacesToReturn; i++)
            {
                result.Append(' ');
            }
        }

        private ErrorRecord NewError()
        {
            ErrorDetails details = new ErrorDetails(this.GetType().GetTypeInfo().Assembly,
                "WebCmdletStrings", "JsonStringInBadFormat");
            ErrorRecord errorRecord = new ErrorRecord(
                new InvalidOperationException(details.Message),
                "JsonStringInBadFormat",
                ErrorCategory.InvalidOperation,
                InputObject);
            return errorRecord;
        }

        #endregion convertOutputToPrettierFormat

        /// <summary>
        /// Return an alternate representation of the specified object that serializes the same JSON, except 
        /// that properties that cannot be evaluated are treated as having the value null.
        /// 
        /// Primitive types are returned verbatim.  Aggregate types are processed recursively.
        /// </summary>
        /// <param name="obj">The object to be processed</param>
        /// <param name="depth">The current depth into the object graph</param>
        /// <returns>An object suitable for serializing to JSON</returns>
        private object ProcessValue(object obj, int depth)
        {
            PSObject pso = obj as PSObject;

            if (pso != null)
                obj = pso.BaseObject;

            Object rv = obj;
            bool isPurePSObj = false;
            bool isCustomObj = false;

            if (obj == null
                || DBNull.Value.Equals(obj)
                || obj is string
                || obj is char
                || obj is bool
                || obj is DateTime
                || obj is DateTimeOffset
                || obj is Guid
                || obj is Uri
                || obj is double
                || obj is float
                || obj is decimal)
            {
                rv = obj;
            }
            else
            {
                TypeInfo t = obj.GetType().GetTypeInfo();

                if (t.IsPrimitive)
                {
                    rv = obj;
                }
                else if (t.IsEnum)
                {
                    // Win8:378368 Enums based on System.Int64 or System.UInt64 are not JSON-serializable 
                    // because JavaScript does not support the necessary precision.
                    Type enumUnderlyingType = Enum.GetUnderlyingType(obj.GetType());
                    if (enumUnderlyingType.Equals(typeof(Int64)) || enumUnderlyingType.Equals(typeof(UInt64)))
                    {
                        rv = obj.ToString();
                    }
                    else
                    {
                        rv = obj;
                    }
                }
                else
                {
                    if (depth > Depth)
                    {
                        if (pso != null && pso.immediateBaseObjectIsEmpty)
                        {
                            // The obj is a pure PSObject, we convert the original PSObject to a string, 
                            // instead of its base object in this case
                            rv = LanguagePrimitives.ConvertTo(pso, typeof(string),
                                CultureInfo.InvariantCulture);
                            isPurePSObj = true;
                        }
                        else
                        {
                            rv = LanguagePrimitives.ConvertTo(obj, typeof(String),
                                CultureInfo.InvariantCulture);
                        }
                    }
                    else
                    {
                        IDictionary dict = obj as IDictionary;
                        if (dict != null)
                        {
                            rv = ProcessDictionary(dict, depth);
                        }
                        else
                        {
                            IEnumerable enumerable = obj as IEnumerable;
                            if (enumerable != null)
                            {
                                rv = ProcessEnumerable(enumerable, depth);
                            }
                            else
                            {
#if CORECLR
                                rv = ProcessCustomObject<JsonIgnoreAttribute>(obj, depth);
#else
                                rv = ProcessCustomObject<ScriptIgnoreAttribute>(obj, depth);
#endif
                                isCustomObj = true;
                            }
                        }
                    }
                }
            }

            rv = AddPsProperties(pso, rv, depth, isPurePSObj, isCustomObj);

            return rv;
        }

        /// <summary>
        /// Add to a base object any properties that might have been added to an object (via PSObject) through the Add-Member cmdlet.
        /// </summary>
        /// <param name="psobj">The containing PSObject, or null if the base object was not contained in a PSObject</param>
        /// <param name="obj">The base object that might have been decorated with additional properties</param>
        /// <param name="depth">The current depth into the object graph</param>
        /// <param name="isPurePSObj">the processed object is a pure PSObject</param>
        /// <param name="isCustomObj">the processed object is a custom object</param>
        /// <returns>
        /// The original base object if no additional properties had been added,
        /// otherwise a dictionary containing the value of the original base object in the "value" key
        /// as well as the names and values of an additional properties.
        /// </returns>
        private object AddPsProperties(object psobj, object obj, int depth, bool isPurePSObj, bool isCustomObj)
        {
            PSObject pso = psobj as PSObject;

            if (pso == null)
                return obj;

            // when isPurePSObj is true, the obj is guaranteed to be a string converted by LanguagePrimitives
            if (isPurePSObj)
                return obj;

            bool wasDictionary = true;
            IDictionary dict = obj as IDictionary;

            if (dict == null)
            {
                wasDictionary = false;
                dict = new Dictionary<string, object>();
                dict.Add("value", obj);
            }

            AppendPsProperties(pso, dict, depth, isCustomObj);

            if (wasDictionary == false && dict.Count == 1)
                return obj;

            return dict;
        }

        /// <summary>
        /// Append to a dictionary any properties that might have been added to an object (via PSObject) through the Add-Member cmdlet.
        /// If the passed in object is a custom object (not a simple object, not a dictionary, not a list, get processed in ProcessCustomObject method),
        /// we also take Adapted properties into account. Otherwise, we only consider the Extended properties.
        /// When the object is a pure PSObject, it also gets processed in "ProcessCustomObject" before reaching this method, so we will
        /// iterate both extended and adapted properties for it. Since it's a pure PSObject, there will be no adapted properties.
        /// </summary>
        /// <param name="psobj">The containing PSObject, or null if the base object was not contained in a PSObject</param>
        /// <param name="receiver">The dictionary to which any additional properties will be appended</param>
        /// <param name="depth">The current depth into the object graph</param>
        /// <param name="isCustomObject">The processed object is a custom object</param>
        private void AppendPsProperties(PSObject psobj, IDictionary receiver, int depth, bool isCustomObject)
        {
            // serialize only Extended and Adapted properties..
            PSMemberInfoCollection<PSPropertyInfo> srcPropertiesToSearch =
                new PSMemberInfoIntegratingCollection<PSPropertyInfo>(psobj,
                    isCustomObject ? PSObject.GetPropertyCollection(PSMemberViewTypes.Extended | PSMemberViewTypes.Adapted) :
                    PSObject.GetPropertyCollection(PSMemberViewTypes.Extended));

            foreach (PSPropertyInfo prop in srcPropertiesToSearch)
            {
                object value = null;
                try
                {
                    value = prop.Value;
                }
                catch (Exception ex)
                {
                    UtilityCommon.CheckForSevereException(this, ex);
                }

                if (!receiver.Contains(prop.Name))
                {
                    receiver[prop.Name] = ProcessValue(value, depth + 1);
                }
            }
        }

        /// <summary>
        /// Return an alternate representation of the specified dictionary that serializes the same JSON, except 
        /// that any contained properties that cannot be evaluated are treated as having the value null.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        private object ProcessDictionary(IDictionary dict, int depth)
        {
            Dictionary<string, object> result = new Dictionary<string, object>(dict.Count);

            foreach (DictionaryEntry entry in dict)
            {
                string name = entry.Key as string;
                if (name == null)
                {
                    // use the error string that matches the message from JavaScriptSerializer
                    var exception =
                        new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                    WebCmdletStrings.NonStringKeyInDictionary,
                                                                    dict.GetType().FullName));
                    ThrowTerminatingError(new ErrorRecord(exception, "NonStringKeyInDictionary", ErrorCategory.InvalidOperation, dict));
                }

                result.Add(name, ProcessValue(entry.Value, depth + 1));
            }

            return result;
        }

        /// <summary>
        /// Return an alternate representation of the specified collection that serializes the same JSON, except 
        /// that any contained properties that cannot be evaluated are treated as having the value null.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        private object ProcessEnumerable(IEnumerable enumerable, int depth)
        {
            List<object> result = new List<object>();

            foreach (object o in enumerable)
            {
                result.Add(ProcessValue(o, depth + 1));
            }

            return result;
        }

        /// <summary>
        /// Return an alternate representation of the specified aggregate object that serializes the same JSON, except 
        /// that any contained properties that cannot be evaluated are treated as having the value null.
        /// 
        /// The result is a dictionary in which all public fields and public gettable properties of the original object
        /// are represented.  If any exception occurs while retrieving the value of a field or property, that entity
        /// is included in the output dictionary with a value of null.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        private object ProcessCustomObject<T>(object o, int depth)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            Type t = o.GetType();

            foreach (FieldInfo info in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!info.IsDefined(typeof(T), true))
                {
                    object value;
                    try
                    {
                        value = info.GetValue(o);
                    }
                    catch (Exception)
                    {
                        value = null;
                    }

                    result.Add(info.Name, ProcessValue(value, depth + 1));
                }
            }

            foreach (PropertyInfo info2 in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!info2.IsDefined(typeof(T), true))
                {
                    MethodInfo getMethod = info2.GetGetMethod();
                    if ((getMethod != null) && (getMethod.GetParameters().Length <= 0))
                    {
                        object value;
                        try
                        {
                            value = getMethod.Invoke(o, new object[0]);
                        }
                        catch (Exception)
                        {
                            value = null;
                        }

                        result.Add(info2.Name, ProcessValue(value, depth + 1));
                    }
                }
            }
            return result;
        }
    }
}
