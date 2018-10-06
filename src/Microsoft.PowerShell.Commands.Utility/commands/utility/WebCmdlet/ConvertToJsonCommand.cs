// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using Dbg = System.Management.Automation;
using System.Management.Automation;
using System.Management.Automation.Internal;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// FxCop suppressions for resource strings:
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "WebCmdletStrings.resources", MessageId = "json")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "WebCmdletStrings.resources", MessageId = "Json")]

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The ConvertTo-Json command.
    /// This command converts an object to a Json string representation.
    /// </summary>
    [Cmdlet(VerbsData.ConvertTo, "Json", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=217032", RemotingCapability = RemotingCapability.None)]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public class ConvertToJsonCommand : PSCmdlet
    {
        #region parameters
        /// <summary>
        /// Gets or sets the InputObject property.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowNull]
        public object InputObject { get; set; }

        private int _depth = 2;
        private const int maxDepthAllowed = 100;

        /// <summary>
        /// Gets or sets the Depth property.
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

        /// <summary>
        /// gets or sets the EnumsAsStrings property.
        /// If the EnumsAsStrings property is set to true, enum values will
        /// be converted to their string equivalent. Otherwise, enum values
        /// will be converted to their numeric equivalent.
        /// </summary>
        [Parameter]
        public SwitchParameter EnumsAsStrings { get; set; }

        /// <summary>
        /// Gets or sets the AsArray property.
        /// If the AsArray property is set to be true, the result JSON string will
        /// be returned with surrounding '[', ']' chars. Otherwise,
        /// the array symbols will occur only if there is more than one input object.
        /// </summary>
        [Parameter]
        public SwitchParameter AsArray { get; set; }

        /// <summary>
        /// Specifies how strings are escaped when writing JSON text.
        /// If the EscapeHandling property is set to EscapeHtml, the result JSON string will
        /// be returned with HTML (<, >, &, ', ") and control characters (e.g. newline) are escaped.
        /// </summary>
        [Parameter]
        public StringEscapeHandling EscapeHandling { get; set; } = StringEscapeHandling.Default;

        #endregion parameters

        #region overrides

        /// <summary>
        /// Prerequisite checks.
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
        }

        private List<object> _inputObjects = new List<object>();

        /// <summary>
        /// Caching the input objects for the convertto-json command.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (InputObject != null)
            {
                _inputObjects.Add(InputObject);
            }
        }

        /// <summary>
        /// Do the conversion to json and write output.
        /// </summary>
        protected override void EndProcessing()
        {
            if (_inputObjects.Count > 0)
            {
                object objectToProcess = (_inputObjects.Count > 1 || AsArray) ? (_inputObjects.ToArray() as object) : (_inputObjects[0]);
                // Pre-process the object so that it serializes the same, except that properties whose
                // values cannot be evaluated are treated as having the value null.
                object preprocessedObject = null;
                try
                {
                    preprocessedObject = ProcessValue(objectToProcess, 0);
                }
                catch (StoppingException)
                {
                    return;
                }
                JsonSerializerSettings jsonSettings = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None,
                        MaxDepth = 1024,
                        StringEscapeHandling = EscapeHandling
                    };
                if (EnumsAsStrings)
                {
                    jsonSettings.Converters.Add(new StringEnumConverter());
                }
                if (!Compress)
                {
                    jsonSettings.Formatting = Formatting.Indented;
                }
                string output = JsonConvert.SerializeObject(preprocessedObject, jsonSettings);
                WriteObject(output);
            }
        }

        #endregion overrides

        /// <summary>
        /// Return an alternate representation of the specified object that serializes the same JSON, except
        /// that properties that cannot be evaluated are treated as having the value null.
        /// Primitive types are returned verbatim.  Aggregate types are processed recursively.
        /// </summary>
        /// <param name="obj">The object to be processed</param>
        /// <param name="depth">The current depth into the object graph</param>
        /// <returns>An object suitable for serializing to JSON</returns>
        private object ProcessValue(object obj, int depth)
        {
            if (Stopping)
            {
                throw new StoppingException();
            }

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
            else if (obj is Newtonsoft.Json.Linq.JObject jObject)
            {
                rv = jObject.ToObject<Dictionary<object, object>>();
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
                                rv = ProcessCustomObject<JsonIgnoreAttribute>(obj, depth);
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
                catch (Exception)
                {
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

        /// <summary>
        /// Exception used for Stopping.
        /// </summary>
        private class StoppingException : System.Exception { }
    }
}
