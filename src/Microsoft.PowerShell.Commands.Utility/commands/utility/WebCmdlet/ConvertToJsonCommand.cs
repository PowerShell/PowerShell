// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading;

using Newtonsoft.Json;
using NewtonsoftStringEscapeHandling = Newtonsoft.Json.StringEscapeHandling;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The ConvertTo-Json command.
    /// This command converts an object to a Json string representation.
    /// </summary>
    /// <remarks>
    /// This class is hidden when PSJsonSerializerV2 experimental feature is enabled.
    /// </remarks>
    [Experimental(ExperimentalFeature.PSJsonSerializerV2, ExperimentAction.Hide)]
    [Cmdlet(VerbsData.ConvertTo, "Json", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096925", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(string))]
    public class ConvertToJsonCommand : PSCmdlet, IDisposable
    {
        /// <summary>
        /// Gets or sets the InputObject property.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowNull]
        public object InputObject { get; set; }

        private int _depth = 2;

        private readonly CancellationTokenSource _cancellationSource = new();

        /// <summary>
        /// Gets or sets the Depth property.
        /// </summary>
        [Parameter]
        [ValidateRange(0, 100)]
        public int Depth
        {
            get { return _depth; }
            set { _depth = value; }
        }

        /// <summary>
        /// Gets or sets the Compress property.
        /// If the Compress property is set to be true, the Json string will
        /// be output in the compressed way. Otherwise, the Json string will
        /// be output with indentations.
        /// </summary>
        [Parameter]
        public SwitchParameter Compress { get; set; }

        /// <summary>
        /// Gets or sets the EnumsAsStrings property.
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
        /// be returned with HTML (&lt;, &gt;, &amp;, ', ") and control characters (e.g. newline) are escaped.
        /// </summary>
        [Parameter]
        public StringEscapeHandling EscapeHandling { get; set; } = StringEscapeHandling.Default;

        /// <summary>
        /// IDisposable implementation, dispose of any disposable resources created by the cmdlet.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implementation of IDisposable for both manual Dispose() and finalizer-called disposal of resources.
        /// </summary>
        /// <param name="disposing">
        /// Specified as true when Dispose() was called, false if this is called from the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationSource.Dispose();
            }
        }

        private readonly List<object> _inputObjects = new();

        /// <summary>
        /// Caching the input objects for the command.
        /// </summary>
        protected override void ProcessRecord()
        {
            _inputObjects.Add(InputObject);
        }

        /// <summary>
        /// Do the conversion to json and write output.
        /// </summary>
        protected override void EndProcessing()
        {
            if (_inputObjects.Count > 0)
            {
                object objectToProcess = (_inputObjects.Count > 1 || AsArray) ? (_inputObjects.ToArray() as object) : _inputObjects[0];

                var context = new JsonObject.ConvertToJsonContext(
                    Depth,
                    EnumsAsStrings.IsPresent,
                    Compress.IsPresent,
                    EscapeHandling,
                    targetCmdlet: this,
                    _cancellationSource.Token);

                // null is returned only if the pipeline is stopping (e.g. ctrl+c is signaled).
                // in that case, we shouldn't write the null to the output pipe.
                string output = JsonObject.ConvertToJson(objectToProcess, in context);
                if (output != null)
                {
                    WriteObject(output);
                }
            }
        }

        /// <summary>
        /// Process the Ctrl+C signal.
        /// </summary>
        protected override void StopProcessing()
        {
            _cancellationSource.Cancel();
        }
    }

#nullable enable

    /// <summary>
    /// The ConvertTo-Json V2 command using System.Text.Json.
    /// This experimental version provides improved performance and better .NET integration.
    /// </summary>
    /// <remarks>
    /// This class is shown when PSJsonSerializerV2 experimental feature is enabled.
    /// </remarks>
    [Experimental(ExperimentalFeature.PSJsonSerializerV2, ExperimentAction.Show)]
    [Cmdlet(VerbsData.ConvertTo, "Json", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096925", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(string))]
    public class ConvertToJsonCommandV2 : PSCmdlet, IDisposable
    {
        /// <summary>
        /// Gets or sets the InputObject property.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowNull]
        public object? InputObject { get; set; }

        private readonly CancellationTokenSource _cancellationSource = new();

        /// <summary>
        /// Gets or sets the Depth property.
        /// Default is 2. Maximum allowed is 1000.
        /// </summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int Depth { get; set; } = 2;

        /// <summary>
        /// Gets or sets the Compress property.
        /// </summary>
        [Parameter]
        public SwitchParameter Compress { get; set; }

        /// <summary>
        /// Gets or sets the EnumsAsStrings property.
        /// </summary>
        [Parameter]
        public SwitchParameter EnumsAsStrings { get; set; }

        /// <summary>
        /// Gets or sets the AsArray property.
        /// </summary>
        [Parameter]
        public SwitchParameter AsArray { get; set; }

        /// <summary>
        /// Specifies how strings are escaped when writing JSON text.
        /// </summary>
        [Parameter]
        public NewtonsoftStringEscapeHandling EscapeHandling { get; set; } = NewtonsoftStringEscapeHandling.Default;

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implementation of IDisposable.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationSource.Dispose();
            }
        }

        private readonly List<object?> _inputObjects = new();

        /// <summary>
        /// Caching the input objects for the command.
        /// </summary>
        protected override void ProcessRecord()
        {
            _inputObjects.Add(InputObject);
        }

        /// <summary>
        /// Do the conversion to json and write output.
        /// </summary>
        protected override void EndProcessing()
        {
            if (_inputObjects.Count > 0)
            {
                object? objectToProcess = (_inputObjects.Count > 1 || AsArray) ? (_inputObjects.ToArray() as object) : _inputObjects[0];

                var context = new JsonObject.ConvertToJsonContext(
                    Depth,
                    EnumsAsStrings.IsPresent,
                    Compress.IsPresent,
                    EscapeHandling,
                    targetCmdlet: this,
                    _cancellationSource.Token);

                string? output = SystemTextJsonSerializer.ConvertToJson(objectToProcess, in context);
                if (output != null)
                {
                    WriteObject(output);
                }
            }
        }

        /// <summary>
        /// Process the Ctrl+C signal.
        /// </summary>
        protected override void StopProcessing()
        {
            _cancellationSource.Cancel();
        }
    }

    /// <summary>
    /// Provides JSON serialization using System.Text.Json with PowerShell-specific handling.
    /// </summary>
    /// <remarks>
    /// This implementation uses a recursive approach with Utf8JsonWriter for direct control
    /// over depth tracking and graceful handling of depth limits. Unlike standard
    /// System.Text.Json behavior (which throws on depth exceeded), this implementation
    /// converts deep objects to their string representation with warnings.
    /// Circular references are detected and converted to null.
    /// </remarks>
    internal static class SystemTextJsonSerializer
    {
        private static bool s_maxDepthWarningWritten;

        /// <summary>
        /// Convert an object to JSON string using System.Text.Json.
        /// </summary>
        /// <param name="objectToProcess">The object to convert.</param>
        /// <param name="context">The context for the conversion.</param>
        /// <returns>A JSON string representation of the object, or null if cancelled.</returns>
        public static string? ConvertToJson(object? objectToProcess, in JsonObject.ConvertToJsonContext context)
        {
            try
            {
                s_maxDepthWarningWritten = false;

                var writerOptions = new JsonWriterOptions
                {
                    Indented = !context.CompressOutput,
                    Encoder = GetEncoder(context.StringEscapeHandling),
                };

                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream, writerOptions))
                {
                    var serializer = new PowerShellJsonWriter(
                        context.MaxDepth,
                        context.EnumsAsStrings,
                        context.Cmdlet,
                        context.CancellationToken);
                    serializer.WriteValue(writer, objectToProcess, 0);
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        private static JavaScriptEncoder GetEncoder(NewtonsoftStringEscapeHandling escapeHandling)
        {
            return escapeHandling switch
            {
                NewtonsoftStringEscapeHandling.Default => JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                NewtonsoftStringEscapeHandling.EscapeNonAscii => JavaScriptEncoder.Default,
                NewtonsoftStringEscapeHandling.EscapeHtml => JavaScriptEncoder.Create(UnicodeRanges.BasicLatin),
                _ => JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
        }

        /// <summary>
        /// Writes the max depth warning message once per serialization.
        /// </summary>
        internal static void WriteMaxDepthWarning(int maxDepth, PSCmdlet? cmdlet)
        {
            if (s_maxDepthWarningWritten || cmdlet is null)
            {
                return;
            }

            s_maxDepthWarningWritten = true;
            string message = string.Format(
                CultureInfo.CurrentCulture,
                WebCmdletStrings.JsonMaxDepthReached,
                maxDepth);
            cmdlet.WriteWarning(message);
        }
    }

    /// <summary>
    /// Writes PowerShell objects to JSON using a recursive approach.
    /// </summary>
    internal sealed class PowerShellJsonWriter
    {
        private readonly int _maxDepth;
        private readonly bool _enumsAsStrings;
        private readonly PSCmdlet? _cmdlet;
        private readonly CancellationToken _cancellationToken;
        private readonly HashSet<object> _visited;

        public PowerShellJsonWriter(
            int maxDepth,
            bool enumsAsStrings,
            PSCmdlet? cmdlet,
            CancellationToken cancellationToken)
        {
            _maxDepth = maxDepth;
            _enumsAsStrings = enumsAsStrings;
            _cmdlet = cmdlet;
            _cancellationToken = cancellationToken;
            _visited = new HashSet<object>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        }

        /// <summary>
        /// Writes a value to JSON recursively.
        /// </summary>
        public void WriteValue(Utf8JsonWriter writer, object? value, int currentDepth)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            // Handle null
            if (value is null || LanguagePrimitives.IsNull(value))
            {
                writer.WriteNullValue();
                return;
            }

            // Unwrap PSObject
            PSObject? pso = value as PSObject;
            object baseObject = pso?.BaseObject ?? value;

            // Handle NullString and DBNull (check for extended properties first)
            if (baseObject is System.Management.Automation.Language.NullString || baseObject is DBNull)
            {
                if (pso is not null && pso.Properties.Any(p => p.MemberType == PSMemberTypes.NoteProperty || p.MemberType == PSMemberTypes.ScriptProperty))
                {
                    // Has extended properties - process as object
                    WriteObject(writer, baseObject, pso, currentDepth);
                    return;
                }
                else
                {
                    writer.WriteNullValue();
                    return;
                }
            }

            // Handle primitives
            if (TryWritePrimitive(writer, baseObject))
            {
                return;
            }

            // Handle enums
            if (baseObject.GetType().IsEnum)
            {
                WriteEnum(writer, baseObject);
                return;
            }

            // Check depth limit before processing complex types
            if (currentDepth > _maxDepth)
            {
                SystemTextJsonSerializer.WriteMaxDepthWarning(_maxDepth, _cmdlet);
                writer.WriteStringValue(LanguagePrimitives.ConvertTo<string>(value));
                return;
            }

            // Handle Newtonsoft.Json.Linq.JObject (implements IDictionary<string, JToken>)
            if (baseObject is Newtonsoft.Json.Linq.JObject jObject)
            {
                WriteJObject(writer, jObject, currentDepth);
                return;
            }

            // Handle dictionaries
            if (baseObject is IDictionary dictionary)
            {
                WriteDictionary(writer, dictionary, currentDepth);
                return;
            }

            // Handle arrays and collections
            if (baseObject is IEnumerable enumerable and not string)
            {
                WriteArray(writer, enumerable, pso, currentDepth);
                return;
            }

            // Handle complex objects
            WriteObject(writer, baseObject, pso, currentDepth);
        }

        private static bool TryWritePrimitive(Utf8JsonWriter writer, object value)
        {
            switch (value)
            {
                case string s:
                    writer.WriteStringValue(s);
                    return true;
                case bool b:
                    writer.WriteBooleanValue(b);
                    return true;
                case byte b:
                    writer.WriteNumberValue(b);
                    return true;
                case sbyte sb:
                    writer.WriteNumberValue(sb);
                    return true;
                case short s:
                    writer.WriteNumberValue(s);
                    return true;
                case ushort us:
                    writer.WriteNumberValue(us);
                    return true;
                case int i:
                    writer.WriteNumberValue(i);
                    return true;
                case uint ui:
                    writer.WriteNumberValue(ui);
                    return true;
                case long l:
                    writer.WriteNumberValue(l);
                    return true;
                case ulong ul:
                    writer.WriteNumberValue(ul);
                    return true;
                case float f:
                    writer.WriteNumberValue(f);
                    return true;
                case double d:
                    writer.WriteNumberValue(d);
                    return true;
                case decimal dec:
                    writer.WriteNumberValue(dec);
                    return true;
                case DateTime dt:
                    writer.WriteStringValue(dt);
                    return true;
                case DateTimeOffset dto:
                    writer.WriteStringValue(dto);
                    return true;
                case Guid guid:
                    writer.WriteStringValue(guid);
                    return true;
                case Uri uri:
                    writer.WriteStringValue(uri.OriginalString);
                    return true;
                case BigInteger bi:
                    writer.WriteRawValue(bi.ToString(CultureInfo.InvariantCulture));
                    return true;
                default:
                    return false;
            }
        }

        private void WriteEnum(Utf8JsonWriter writer, object value)
        {
            Type enumType = value.GetType();

            if (_enumsAsStrings)
            {
                writer.WriteStringValue(value.ToString());
            }
            else
            {
                // Special handling for Int64/UInt64 enums (JavaScript precision issue)
                Type underlyingType = Enum.GetUnderlyingType(enumType);
                if (underlyingType == typeof(long) || underlyingType == typeof(ulong))
                {
                    writer.WriteStringValue(LanguagePrimitives.ConvertTo<string>(value));
                }
                else
                {
                    writer.WriteNumberValue(Convert.ToInt64(value));
                }
            }
        }

        private void WriteArray(Utf8JsonWriter writer, IEnumerable enumerable, PSObject? pso, int currentDepth)
        {
            writer.WriteStartArray();

            foreach (object? item in enumerable)
            {
                WriteValue(writer, item, currentDepth + 1);
            }

            writer.WriteEndArray();
        }

        private void WriteDictionary(Utf8JsonWriter writer, IDictionary dictionary, int currentDepth)
        {
            writer.WriteStartObject();

            foreach (DictionaryEntry entry in dictionary)
            {
                string key = entry.Key?.ToString() ?? string.Empty;
                writer.WritePropertyName(key);
                WriteValue(writer, entry.Value, currentDepth + 1);
            }

            writer.WriteEndObject();
        }

        private void WriteJObject(Utf8JsonWriter writer, Newtonsoft.Json.Linq.JObject jObject, int currentDepth)
        {
            writer.WriteStartObject();

            foreach (var prop in jObject.Properties())
            {
                writer.WritePropertyName(prop.Name);
                // Convert JToken to native object for serialization
                object? value = prop.Value?.ToObject<object>();
                WriteValue(writer, value, currentDepth + 1);
            }

            writer.WriteEndObject();
        }

        private void WriteObject(Utf8JsonWriter writer, object baseObject, PSObject? pso, int currentDepth)
        {
            // Determine the object to use for circular reference detection
            // For PSCustomObject, use the PSObject wrapper itself, not the BaseObject
            object referenceKey = (baseObject is System.Management.Automation.PSCustomObject && pso is not null) ? pso : baseObject;

            // Check for circular reference
            if (!referenceKey.GetType().IsValueType)
            {
                if (!_visited.Add(referenceKey))
                {
                    writer.WriteNullValue();
                    return;
                }
            }

            try
            {
                writer.WriteStartObject();

                var properties = GetProperties(baseObject, pso);
                foreach (var (name, value) in properties)
                {
                    writer.WritePropertyName(name);
                    WriteValue(writer, value, currentDepth + 1);
                }

                writer.WriteEndObject();
            }
            finally
            {
                // Remove from visited set when leaving this object
                if (!referenceKey.GetType().IsValueType)
                {
                    _visited.Remove(referenceKey);
                }
            }
        }

        private static List<(string Name, object? Value)> GetProperties(object obj, PSObject? pso)
        {
            var results = new List<(string, object?)>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Get PSObject properties first (if available)
            if (pso is not null)
            {
                // If BaseObject is not a Dictionary or PSCustomObject and has ETS properties,
                // include the BaseObject itself as "value" property (V1 compatibility)
                if (obj is not IDictionary && obj is not System.Management.Automation.PSCustomObject)
                {
                    // Check if there are any ETS properties
                    bool hasETSProperties = false;
                    foreach (PSPropertyInfo prop in pso.Properties)
                    {
                        if (ShouldSerializeProperty(prop))
                        {
                            hasETSProperties = true;
                            break;
                        }
                    }

                    if (hasETSProperties)
                    {
                        results.Add(("value", obj));
                        seenNames.Add("value");
                    }
                }

                foreach (PSPropertyInfo prop in pso.Properties)
                {
                    if (ShouldSerializeProperty(prop) && seenNames.Add(prop.Name))
                    {
                        try
                        {
                            object? value = prop.Value;
                            results.Add((prop.Name, value));
                        }
                        catch
                        {
                            // Skip properties that throw
                        }
                    }
                }
            }
            else
            {
                // Get .NET properties via reflection
                Type type = obj.GetType();

                // Public fields
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (ShouldSerializeField(field) && seenNames.Add(field.Name))
                    {
                        try
                        {
                            object? value = field.GetValue(obj);
                            results.Add((field.Name, value));
                        }
                        catch
                        {
                            // Skip fields that throw
                        }
                    }
                }

                // Public properties
                foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (ShouldSerializeProperty(prop) && seenNames.Add(prop.Name))
                    {
                        try
                        {
                            object? value = prop.GetValue(obj);
                            results.Add((prop.Name, value));
                        }
                        catch
                        {
                            // Skip properties that throw
                        }
                    }
                }
            }

            return results;
        }

        private static bool ShouldSerializeProperty(PSPropertyInfo prop)
        {
            return prop.IsGettable &&
                   (prop.MemberType == PSMemberTypes.NoteProperty ||
                    prop.MemberType == PSMemberTypes.ScriptProperty ||
                    prop.MemberType == PSMemberTypes.Property);
        }

        private static bool ShouldSerializeField(FieldInfo field)
        {
            return !field.IsDefined(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute)) &&
                   !field.IsDefined(typeof(HiddenAttribute));
        }

        private static bool ShouldSerializeProperty(PropertyInfo prop)
        {
            return prop.CanRead &&
                   prop.GetIndexParameters().Length == 0 &&
                   !prop.IsDefined(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute)) &&
                   !prop.IsDefined(typeof(HiddenAttribute));
        }
    }

}
