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
using StjJsonIgnoreAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;

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
    /// The ConvertTo-Json command (V2 - System.Text.Json implementation).
    /// This command converts an object to a Json string representation.
    /// </summary>
    /// <remarks>
    /// This class is shown when PSJsonSerializerV2 experimental feature is enabled.
    /// V2 uses System.Text.Json with circular reference detection and unlimited depth by default.
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
        /// Default is 64. Maximum allowed depth is 1000 due to System.Text.Json limitations.
        /// Use 0 to serialize only top-level properties.
        /// </summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int Depth { get; set; } = 64;

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
        public NewtonsoftStringEscapeHandling EscapeHandling { get; set; } = NewtonsoftStringEscapeHandling.Default;

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

                string? output = SystemTextJsonSerializer.ConvertToJson(
                    objectToProcess,
                    Depth,
                    EnumsAsStrings.IsPresent,
                    Compress.IsPresent,
                    EscapeHandling,
                    this,
                    _cancellationSource.Token);

                // null is returned only if the pipeline is stopping (e.g. ctrl+c is signaled).
                // in that case, we shouldn't write the null to the output pipe.
                if (output is not null)
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
    internal static class SystemTextJsonSerializer
    {
        /// <summary>
        /// Convert an object to JSON string using System.Text.Json.
        /// </summary>
        public static string? ConvertToJson(
            object? objectToProcess,
            int maxDepth,
            bool enumsAsStrings,
            bool compressOutput,
            NewtonsoftStringEscapeHandling stringEscapeHandling,
            PSCmdlet? cmdlet,
            CancellationToken cancellationToken)
        {
            if (objectToProcess is null)
            {
                return "null";
            }

            try
            {
                var options = new JsonSerializerOptions()
                {
                    WriteIndented = !compressOutput,
                    MaxDepth = maxDepth,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                    Encoder = GetEncoder(stringEscapeHandling),
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                };

                if (enumsAsStrings)
                {
                    options.Converters.Add(new JsonStringEnumConverter());
                }

                // Add custom converters for PowerShell-specific types
                options.Converters.Add(new JsonConverterInt64Enum());
                options.Converters.Add(new JsonConverterBigInteger());
                options.Converters.Add(new JsonConverterNullString());
                options.Converters.Add(new JsonConverterDBNull());
                options.Converters.Add(new JsonConverterPSObject(cmdlet, maxDepth));

                // Handle JObject specially to avoid IEnumerable serialization
                if (objectToProcess is Newtonsoft.Json.Linq.JObject jObj)
                {
                    // Serialize JObject directly using our custom logic
                    using var stream = new System.IO.MemoryStream();
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = !compressOutput, Encoder = GetEncoder(stringEscapeHandling) }))
                    {
                        writer.WriteStartObject();
                        foreach (var prop in jObj.Properties())
                        {
                            writer.WritePropertyName(prop.Name);
                            var value = prop.Value.Type switch
                            {
                                Newtonsoft.Json.Linq.JTokenType.String => prop.Value.ToObject<string>(),
                                Newtonsoft.Json.Linq.JTokenType.Integer => prop.Value.ToObject<long>(),
                                Newtonsoft.Json.Linq.JTokenType.Float => prop.Value.ToObject<double>(),
                                Newtonsoft.Json.Linq.JTokenType.Boolean => prop.Value.ToObject<bool>(),
                                Newtonsoft.Json.Linq.JTokenType.Null => (object?)null,
                                _ => prop.Value.ToString()
                            };
                            System.Text.Json.JsonSerializer.Serialize(writer, value, options);
                        }
                        writer.WriteEndObject();
                    }
                    return System.Text.Encoding.UTF8.GetString(stream.ToArray());
                }

                // Wrap in PSObject to ensure ETS properties are preserved
                var pso = PSObject.AsPSObject(objectToProcess);
                return System.Text.Json.JsonSerializer.Serialize(pso, typeof(PSObject), options);
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
    }

    /// <summary>
    /// Custom JsonConverter for PSObject that handles PowerShell-specific serialization.
    /// </summary>
    internal sealed class JsonConverterPSObject : System.Text.Json.Serialization.JsonConverter<PSObject>
    {
        private readonly PSCmdlet? _cmdlet;
        private readonly int _maxDepth;

        public JsonConverterPSObject(PSCmdlet? cmdlet, int maxDepth)
        {
            _cmdlet = cmdlet;
            _maxDepth = maxDepth;
        }

        public override PSObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, PSObject pso, JsonSerializerOptions options)
        {
            if (LanguagePrimitives.IsNull(pso))
            {
                writer.WriteNullValue();
                return;
            }

            var obj = pso.BaseObject;

            // Check if PSObject has Extended/Adapted properties
            bool hasETSProperties = pso.Properties.Match("*", PSMemberTypes.NoteProperty | PSMemberTypes.AliasProperty).Count > 0;

            // Handle special types - check for null-like objects
            if (LanguagePrimitives.IsNull(obj) || obj is DBNull or System.Management.Automation.Language.NullString)
            {
                // If there are ETS properties, serialize as object with those properties
                if (hasETSProperties)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("value");
                    writer.WriteNullValue();
                    AppendPSProperties(writer, pso, options, excludeBaseProperties: true);
                    writer.WriteEndObject();
                }
                else
                {
                    writer.WriteNullValue();
                }
                return;
            }

            // Handle Newtonsoft.Json.Linq.JObject by converting properties manually
            if (obj is Newtonsoft.Json.Linq.JObject jObject)
            {
                writer.WriteStartObject();
                foreach (var prop in jObject.Properties())
                {
                    writer.WritePropertyName(prop.Name);
                    // Convert JToken value to appropriate .NET type
                    var value = prop.Value.Type switch
                    {
                        Newtonsoft.Json.Linq.JTokenType.String => prop.Value.ToObject<string>(),
                        Newtonsoft.Json.Linq.JTokenType.Integer => prop.Value.ToObject<long>(),
                        Newtonsoft.Json.Linq.JTokenType.Float => prop.Value.ToObject<double>(),
                        Newtonsoft.Json.Linq.JTokenType.Boolean => prop.Value.ToObject<bool>(),
                        Newtonsoft.Json.Linq.JTokenType.Null => (object?)null,
                        _ => prop.Value.ToString()
                    };
                    System.Text.Json.JsonSerializer.Serialize(writer, value, options);
                }
                writer.WriteEndObject();
                return;
            }
            // If PSObject wraps a primitive type, serialize the base object directly
            if (IsPrimitiveType(obj))
            {
                System.Text.Json.JsonSerializer.Serialize(writer, obj, obj.GetType(), options);
                return;
            }

            // For dictionaries and collections, convert to appropriate structure
            if (obj is IDictionary dict)
            {
                SerializeDictionary(writer, pso, dict, options);
                return;
            }

            if (obj is IEnumerable enumerable and not string)
            {
                System.Text.Json.JsonSerializer.Serialize(writer, enumerable, options);
                return;
            }

            // For custom objects, serialize as dictionary with properties
            SerializeAsObject(writer, pso, options);
        }

        private static bool IsPrimitiveType(object obj)
        {
            var type = obj.GetType();
            return type.IsPrimitive
                || type.IsEnum
                || obj is string
                || obj is decimal
                || obj is DateTime
                || obj is DateTimeOffset
                || obj is Guid
                || obj is Uri;
        }

        private static bool IsPrimitiveTypeOrNull(object? obj)
        {
            return obj is null || IsPrimitiveType(obj);
        }

        private void SerializeDictionary(Utf8JsonWriter writer, PSObject pso, IDictionary dict, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Serialize dictionary entries
            foreach (DictionaryEntry entry in dict)
            {
                string key = entry.Key?.ToString() ?? string.Empty;
                writer.WritePropertyName(key);
                
                // If maxDepth is 0, convert non-primitive values to string
                if (_maxDepth == 0 && entry.Value is not null && !IsPrimitiveTypeOrNull(entry.Value))
                {
                    writer.WriteStringValue(entry.Value.ToString());
                }
                // Handle Newtonsoft.Json.Linq.JObject specially
                else if (entry.Value is Newtonsoft.Json.Linq.JObject jObject)
                {
                    writer.WriteStartObject();
                    foreach (var prop in jObject.Properties())
                    {
                        writer.WritePropertyName(prop.Name);
                        var value = prop.Value.Type switch
                        {
                            Newtonsoft.Json.Linq.JTokenType.String => prop.Value.ToObject<string>(),
                            Newtonsoft.Json.Linq.JTokenType.Integer => prop.Value.ToObject<long>(),
                            Newtonsoft.Json.Linq.JTokenType.Float => prop.Value.ToObject<double>(),
                            Newtonsoft.Json.Linq.JTokenType.Boolean => prop.Value.ToObject<bool>(),
                            Newtonsoft.Json.Linq.JTokenType.Null => (object?)null,
                            _ => prop.Value.ToString()
                        };
                        System.Text.Json.JsonSerializer.Serialize(writer, value, options);
                    }
                    writer.WriteEndObject();
                }
                else
                {
                    System.Text.Json.JsonSerializer.Serialize(writer, entry.Value, options);
                }
            }

            // Add PSObject extended properties
            AppendPSProperties(writer, pso, options, excludeBaseProperties: true);

            writer.WriteEndObject();
        }

        private void SerializeAsObject(Utf8JsonWriter writer, PSObject pso, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            AppendPSProperties(writer, pso, options, excludeBaseProperties: false);
            writer.WriteEndObject();
        }

        private void AppendPSProperties(Utf8JsonWriter writer, PSObject pso, JsonSerializerOptions options, bool excludeBaseProperties)
        {
            var memberTypes = excludeBaseProperties
                ? PSMemberViewTypes.Extended
                : (PSMemberViewTypes.Extended | PSMemberViewTypes.Adapted);

            var properties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                pso,
                PSObject.GetPropertyCollection(memberTypes));

            foreach (var prop in properties)
            {
                // Skip properties with JsonIgnore attribute or Hidden attribute
                if (ShouldSkipProperty(prop))
                {
                    continue;
                }

                try
                {
                    var value = prop.Value;
                    writer.WritePropertyName(prop.Name);
                    
                    // If maxDepth is 0, convert non-primitive values to string
                    if (_maxDepth == 0 && value is not null && !IsPrimitiveTypeOrNull(value))
                    {
                        writer.WriteStringValue(value.ToString());
                    }
                    else
                    {
                        // Handle null values directly (including AutomationNull)
                        if (LanguagePrimitives.IsNull(value))
                        {
                            writer.WriteNullValue();
                        }
                        else
                        {
                            // Wrap value in PSObject to ensure custom converters are applied
                            var psoValue = PSObject.AsPSObject(value);
                            System.Text.Json.JsonSerializer.Serialize(writer, psoValue, typeof(PSObject), options);
                        }
                    }
                }
                catch
                {
                    // Skip properties that throw on access
                    continue;
                }
            }
        }

        private static bool ShouldSkipProperty(PSPropertyInfo prop)
        {
            // Check for Hidden attribute
            if (prop.IsHidden)
            {
                return true;
            }

            // Note: JsonIgnoreAttribute check would require reflection on the underlying member
            // which may not be available for all PSPropertyInfo types. For now, we rely on
            // IsHidden to filter properties that should not be serialized.
            return false;
        }
    }

    /// <summary>
    /// JsonConverter for Int64/UInt64 enums to avoid JavaScript precision issues.
    /// </summary>
    internal sealed class JsonConverterInt64Enum : System.Text.Json.Serialization.JsonConverter<Enum>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsEnum)
            {
                return false;
            }

            var underlyingType = Enum.GetUnderlyingType(typeToConvert);
            return underlyingType == typeof(long) || underlyingType == typeof(ulong);
        }

        public override Enum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Enum value, JsonSerializerOptions options)
        {
            // Convert to string to avoid JavaScript precision issues with large integers
            writer.WriteStringValue(value.ToString("D"));
        }
    }

    /// <summary>
    /// JsonConverter for NullString to serialize as null.
    /// </summary>
    internal sealed class JsonConverterNullString : System.Text.Json.Serialization.JsonConverter<System.Management.Automation.Language.NullString>
    {
        public override System.Management.Automation.Language.NullString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, System.Management.Automation.Language.NullString value, JsonSerializerOptions options)
        {
            writer.WriteNullValue();
        }
    }

    /// <summary>
    /// JsonConverter for DBNull to serialize as null.
    /// </summary>
    internal sealed class JsonConverterDBNull : System.Text.Json.Serialization.JsonConverter<DBNull>
    {
        public override DBNull? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, DBNull value, JsonSerializerOptions options)
        {
            writer.WriteNullValue();
        }
    }

    /// <summary>
    /// JsonConverter for BigInteger to serialize as number string.
    /// </summary>
    internal sealed class JsonConverterBigInteger : System.Text.Json.Serialization.JsonConverter<BigInteger>
    {
        public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
        {
            // Write as number string to preserve precision
            writer.WriteRawValue(value.ToString(CultureInfo.InvariantCulture));
        }
    }

}
