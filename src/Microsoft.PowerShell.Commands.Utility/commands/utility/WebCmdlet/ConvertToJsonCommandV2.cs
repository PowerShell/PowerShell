// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

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

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Specifies how strings are escaped when writing JSON text.
    /// </summary>
    /// <remarks>
    /// The numeric values must match Newtonsoft.Json.StringEscapeHandling for backward compatibility.
    /// Do not change these values.
    /// </remarks>
    public enum JsonStringEscapeHandling
    {
        /// <summary>
        /// Only control characters (e.g. newline) are escaped.
        /// </summary>
        Default = 0,

        /// <summary>
        /// All non-ASCII and control characters are escaped.
        /// </summary>
        EscapeNonAscii = 1,

        /// <summary>
        /// HTML (&lt;, &gt;, &amp;, ', ") and control characters are escaped.
        /// </summary>
        EscapeHtml = 2,
    }

    /// <summary>
    /// Transforms Newtonsoft.Json.StringEscapeHandling to JsonStringEscapeHandling for backward compatibility.
    /// </summary>
    internal sealed class StringEscapeHandlingTransformationAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
            => inputData is Newtonsoft.Json.StringEscapeHandling newtonsoftValue ? (JsonStringEscapeHandling)(int)newtonsoftValue : inputData;
    }

    /// <summary>
    /// The ConvertTo-Json command.
    /// This command converts an object to a JSON string representation.
    /// </summary>
    /// <remarks>
    /// This class is shown when PSJsonSerializerV2 experimental feature is enabled.
    /// </remarks>
    [Experimental(ExperimentalFeature.PSJsonSerializerV2, ExperimentAction.Show)]
    [Cmdlet(VerbsData.ConvertTo, "Json", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096925", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(string))]
    public class ConvertToJsonCommandV2 : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the InputObject property.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowNull]
        public object? InputObject { get; set; }

        /// <summary>
        /// Gets or sets the Depth property.
        /// Default is 2. Maximum allowed is 100.
        /// Use 0 to serialize only top-level properties.
        /// </summary>
        [Parameter]
        [ValidateRange(0, 100)]
        public int Depth { get; set; } = 2;

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
        /// Gets or sets how strings are escaped when writing JSON text.
        /// If the EscapeHandling property is set to EscapeHtml, the result JSON string will
        /// be returned with HTML (&lt;, &gt;, &amp;, ', ") and control characters (e.g. newline) are escaped.
        /// </summary>
        [Parameter]
        [StringEscapeHandlingTransformation]
        public JsonStringEscapeHandling EscapeHandling { get; set; } = JsonStringEscapeHandling.Default;

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
                object? objectToProcess = (_inputObjects.Count > 1 || AsArray) ? _inputObjects : _inputObjects[0];

                string? output = SystemTextJsonSerializer.ConvertToJson(
                    objectToProcess,
                    Depth,
                    EnumsAsStrings.IsPresent,
                    Compress.IsPresent,
                    EscapeHandling,
                    this,
                    PipelineStopToken);

                // null is returned only if the pipeline is stopping (e.g. ctrl+c is signaled).
                // in that case, we shouldn't write the null to the output pipe.
                if (output is not null)
                {
                    WriteObject(output);
                }
            }
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
            JsonStringEscapeHandling stringEscapeHandling,
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

                    // Set high value to avoid System.Text.Json exceptions
                    // User-specified depth is enforced by JsonConverterPSObject (max 100 via ValidateRange)
                    MaxDepth = 1000,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                    Encoder = GetEncoder(stringEscapeHandling),
                };

                if (enumsAsStrings)
                {
                    options.Converters.Add(new JsonStringEnumConverter());
                }

                // Add custom converters for PowerShell-specific types
                if (!ExperimentalFeature.IsEnabled(ExperimentalFeature.PSSerializeJSONLongEnumAsNumber))
                {
                    options.Converters.Add(new JsonConverterInt64Enum());
                }

                options.Converters.Add(new JsonConverterBigInteger());
                options.Converters.Add(new JsonConverterNullString());
                options.Converters.Add(new JsonConverterDBNull());
                options.Converters.Add(new JsonConverterPSObject(cmdlet, maxDepth));
                options.Converters.Add(new JsonConverterJObject());

                var pso = PSObject.AsPSObject(objectToProcess);
                return System.Text.Json.JsonSerializer.Serialize(pso, typeof(PSObject), options);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        private static JavaScriptEncoder GetEncoder(JsonStringEscapeHandling escapeHandling) =>
            escapeHandling switch
            {
                JsonStringEscapeHandling.Default => JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                JsonStringEscapeHandling.EscapeNonAscii => JavaScriptEncoder.Default,
                JsonStringEscapeHandling.EscapeHtml => JavaScriptEncoder.Create(UnicodeRanges.BasicLatin),
                _ => JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
    }

    /// <summary>
    /// Custom JsonConverter for PSObject that handles PowerShell-specific serialization.
    /// </summary>
    internal sealed class JsonConverterPSObject : System.Text.Json.Serialization.JsonConverter<PSObject>
    {
        private readonly PSCmdlet? _cmdlet;
        private readonly int _maxDepth;

        private bool _warningWritten;

        public JsonConverterPSObject(PSCmdlet? cmdlet, int maxDepth)
        {
            _cmdlet = cmdlet;
            _maxDepth = maxDepth;
        }

        public override PSObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, PSObject? pso, JsonSerializerOptions options)
        {
            if (LanguagePrimitives.IsNull(pso))
            {
                writer.WriteNullValue();
                return;
            }

            object? obj = pso.BaseObject;

            int currentDepth = writer.CurrentDepth;

            // Handle special types - check for null-like objects (no depth increment needed)
            if (LanguagePrimitives.IsNull(obj) || obj is DBNull or System.Management.Automation.Language.NullString)
            {
                // Single enumeration: write properties directly as we find them
                var etsProperties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                    pso,
                    PSObject.GetPropertyCollection(PSMemberViewTypes.Extended));

                bool wroteStart = false;
                foreach (var prop in etsProperties)
                {
                    if (!ShouldSkipProperty(prop))
                    {
                        if (!wroteStart)
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName("value");
                            writer.WriteNullValue();
                            wroteStart = true;
                        }

                        WriteProperty(writer, prop, options);
                    }
                }

                if (wroteStart)
                {
                    writer.WriteEndObject();
                }
                else
                {
                    writer.WriteNullValue();
                }

                return;
            }

            // Handle Newtonsoft.Json.Linq.JObject by delegating to JsonConverterJObject
            if (obj is Newtonsoft.Json.Linq.JObject jObject)
            {
                System.Text.Json.JsonSerializer.Serialize(writer, jObject, options);
                return;
            }

            // If PSObject wraps a primitive type, serialize the base object directly (no depth increment)
            if (IsPrimitiveType(obj))
            {
                SerializePrimitive(writer, obj, options);
                return;
            }

            // Check depth limit for complex types only (after primitive check)
            if (currentDepth > _maxDepth)
            {
                WriteDepthExceeded(writer, pso, obj);
                return;
            }

            // For dictionaries, collections, and custom objects
            if (obj is IDictionary dict)
            {
                SerializeDictionary(writer, pso, dict, options);
            }
            else if (obj is IEnumerable enumerable)
            {
                SerializeEnumerable(writer, enumerable, options);
            }
            else
            {
                // For custom objects, serialize as dictionary with properties
                SerializeAsObject(writer, pso, options);
            }
        }

        private void WriteDepthExceeded(Utf8JsonWriter writer, PSObject pso, object obj)
        {
            // Write warning once
            if (!_warningWritten && _cmdlet is not null)
            {
                _warningWritten = true;
                string warningMessage = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    "Resulting JSON is truncated as serialization has exceeded the set depth of {0}.",
                    _maxDepth);
                _cmdlet.WriteWarning(warningMessage);
            }

            // Convert to string when depth exceeded
            string stringValue = LanguagePrimitives.ConvertTo<string>(pso.ImmediateBaseObjectIsEmpty ? pso : obj);
            writer.WriteStringValue(stringValue);
        }

        private void SerializeEnumerable(Utf8JsonWriter writer, IEnumerable enumerable, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in enumerable)
            {
                if (item is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    var psoItem = PSObject.AsPSObject(item);

                    // Recursive call - Write will handle depth tracking
                    Write(writer, psoItem, options);
                }
            }

            writer.WriteEndArray();
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
                || obj is Uri
                || obj is BigInteger;
        }

        private static bool IsPrimitiveTypeOrNull(object? obj)
        {
            return obj is null || IsPrimitiveType(obj);
        }

        private static void SerializePrimitive(Utf8JsonWriter writer, object obj, JsonSerializerOptions options)
        {
            // Handle special floating-point values (Infinity, NaN) as strings for V1 compatibility
            if (obj is double d)
            {
                if (double.IsPositiveInfinity(d))
                {
                    writer.WriteStringValue("Infinity");
                    return;
                }

                if (double.IsNegativeInfinity(d))
                {
                    writer.WriteStringValue("-Infinity");
                    return;
                }

                if (double.IsNaN(d))
                {
                    writer.WriteStringValue("NaN");
                    return;
                }
            }
            else if (obj is float f)
            {
                if (float.IsPositiveInfinity(f))
                {
                    writer.WriteStringValue("Infinity");
                    return;
                }

                if (float.IsNegativeInfinity(f))
                {
                    writer.WriteStringValue("-Infinity");
                    return;
                }

                if (float.IsNaN(f))
                {
                    writer.WriteStringValue("NaN");
                    return;
                }
            }

            System.Text.Json.JsonSerializer.Serialize(writer, obj, obj.GetType(), options);
        }

        private void SerializeDictionary(Utf8JsonWriter writer, PSObject pso, IDictionary dict, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Serialize dictionary entries
            foreach (DictionaryEntry entry in dict)
            {
                string key = entry.Key?.ToString() ?? string.Empty;
                writer.WritePropertyName(key);
                WriteValue(writer, entry.Value, options);
            }

            // Add PSObject extended properties
            AppendPSProperties(writer, pso, options, PSMemberViewTypes.Extended);

            writer.WriteEndObject();
        }

        private void WriteValue(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value is null or DBNull or System.Management.Automation.Language.NullString)
            {
                writer.WriteNullValue();
            }
            else if (IsPrimitiveType(value))
            {
                SerializePrimitive(writer, value, options);
            }
            else if (value is Newtonsoft.Json.Linq.JObject jObject)
            {
                System.Text.Json.JsonSerializer.Serialize(writer, jObject, options);
            }
            else
            {
                // Check if value was originally a PSObject (preserves Extended/Adapted properties)
                // or a raw .NET object (serialize with Base properties only for V1 compatibility)
                if (value is PSObject psoValue)
                {
                    Write(writer, psoValue, options);
                }
                else
                {
                    var pso = PSObject.AsPSObject(value);
                    SerializeRawValue(writer, pso, options);
                }
            }
        }

        private void SerializeAsObject(Utf8JsonWriter writer, PSObject pso, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            AppendPSProperties(writer, pso, options, PSMemberViewTypes.Extended | PSMemberViewTypes.Adapted);
            writer.WriteEndObject();
        }

        /// <summary>
        /// Serializes a raw .NET object with Base properties only (V1 compatible behavior).
        /// </summary>
        private void SerializeRawValue(Utf8JsonWriter writer, PSObject pso, JsonSerializerOptions options)
        {
            object obj = pso.BaseObject;

            // Primitive types: serialize directly
            if (IsPrimitiveType(obj))
            {
                SerializePrimitive(writer, obj, options);
                return;
            }

            // Check depth limit
            int currentDepth = writer.CurrentDepth;
            if (currentDepth > _maxDepth)
            {
                WriteDepthExceeded(writer, pso, obj);
                return;
            }

            // Dictionary: serialize with standard dictionary handling
            if (obj is IDictionary dict)
            {
                SerializeDictionary(writer, pso, dict, options);
                return;
            }

            // Enumerable: serialize with raw item handling
            if (obj is IEnumerable enumerable)
            {
                SerializeEnumerableRaw(writer, enumerable, options);
                return;
            }

            // Object: serialize with Base properties only (MemberType == Property)
            writer.WriteStartObject();
            AppendBaseProperties(writer, pso, options);
            writer.WriteEndObject();
        }

        /// <summary>
        /// Serializes an enumerable with raw .NET object handling (Base properties only).
        /// </summary>
        private void SerializeEnumerableRaw(Utf8JsonWriter writer, IEnumerable enumerable, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in enumerable)
            {
                if (item is null)
                {
                    writer.WriteNullValue();
                }
                else if (item is PSObject psoItem)
                {
                    // Existing PSObject: use standard serialization
                    Write(writer, psoItem, options);
                }
                else
                {
                    // Raw object: serialize with Base properties only
                    var pso = PSObject.AsPSObject(item);
                    SerializeRawValue(writer, pso, options);
                }
            }

            writer.WriteEndArray();
        }

        private void AppendPSProperties(Utf8JsonWriter writer, PSObject pso, JsonSerializerOptions options, PSMemberViewTypes memberTypes)
        {
            var properties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                pso,
                PSObject.GetPropertyCollection(memberTypes));

            foreach (var prop in properties)
            {
                if (ShouldSkipProperty(prop))
                {
                    continue;
                }

                WriteProperty(writer, prop, options);
            }
        }

        /// <summary>
        /// Appends only base .NET properties (MemberType == Property) for V1 compatibility.
        /// </summary>
        private void AppendBaseProperties(Utf8JsonWriter writer, PSObject pso, JsonSerializerOptions options)
        {
            // Use Adapted view which includes .NET properties via DotNetAdapter
            var properties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                pso,
                PSObject.GetPropertyCollection(PSMemberViewTypes.Adapted));

            foreach (var prop in properties)
            {
                // Filter to only Property type (excludes CodeProperty, ScriptProperty, etc.)
                if (prop.MemberType != PSMemberTypes.Property)
                {
                    continue;
                }

                if (ShouldSkipProperty(prop))
                {
                    continue;
                }

                WritePropertyRaw(writer, prop, options);
            }
        }

        /// <summary>
        /// Writes a property value, treating nested objects as raw (Base properties only).
        /// </summary>
        private void WritePropertyRaw(Utf8JsonWriter writer, PSPropertyInfo prop, JsonSerializerOptions options)
        {
            try
            {
                var value = prop.Value;
                writer.WritePropertyName(prop.Name);

                if (LanguagePrimitives.IsNull(value))
                {
                    writer.WriteNullValue();
                }
                else if (_maxDepth == 0 && !IsPrimitiveTypeOrNull(value))
                {
                    writer.WriteStringValue(value!.ToString());
                }
                else
                {
                    // Always treat nested values as raw objects (V1 compatible)
                    var psoValue = PSObject.AsPSObject(value);
                    SerializeRawValue(writer, psoValue, options);
                }
            }
            catch
            {
                // Skip properties that throw on access
            }
        }

        private void WriteProperty(Utf8JsonWriter writer, PSPropertyInfo prop, JsonSerializerOptions options)
        {
            try
            {
                var value = prop.Value;
                writer.WritePropertyName(prop.Name);

                // Handle null values directly (including AutomationNull)
                if (LanguagePrimitives.IsNull(value))
                {
                    writer.WriteNullValue();
                }

                // If maxDepth is 0, convert non-primitive values to string
                else if (_maxDepth == 0 && !IsPrimitiveTypeOrNull(value))
                {
                    writer.WriteStringValue(value!.ToString());
                }
                else
                {
                    // Check if value was originally a PSObject (preserves Extended/Adapted properties)
                    // or a raw .NET object (serialize with Base properties only for V1 compatibility)
                    if (value is PSObject psoValue)
                    {
                        // Existing PSObject: use standard serialization (Extended | Adapted)
                        System.Text.Json.JsonSerializer.Serialize(writer, psoValue, typeof(PSObject), options);
                    }
                    else
                    {
                        // Raw object: serialize with Base properties only (V1 compatible)
                        var pso = PSObject.AsPSObject(value);
                        SerializeRawValue(writer, pso, options);
                    }
                }
            }
            catch
            {
                // Skip properties that throw on access - write nothing for this property
            }
        }

        private static bool ShouldSkipProperty(PSPropertyInfo prop)
        {
            // Check for Hidden attribute
            if (prop.IsHidden)
            {
                return true;
            }

            // Check for JsonIgnoreAttribute on the underlying member
            if (prop is PSProperty psProperty)
            {
                if (psProperty.adapterData is MemberInfo memberInfo &&
                    memberInfo.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() is not null)
                {
                    return true;
                }
            }

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

    /// <summary>
    /// Custom JsonConverter for Newtonsoft.Json.Linq.JObject to isolate Newtonsoft-related code.
    /// </summary>
    internal sealed class JsonConverterJObject : System.Text.Json.Serialization.JsonConverter<Newtonsoft.Json.Linq.JObject>
    {
        public override Newtonsoft.Json.Linq.JObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Newtonsoft.Json.Linq.JObject jObject, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var prop in jObject.Properties())
            {
                writer.WritePropertyName(prop.Name);
                WriteJTokenValue(writer, prop.Value, options);
            }

            writer.WriteEndObject();
        }

        private static void WriteJTokenValue(Utf8JsonWriter writer, Newtonsoft.Json.Linq.JToken token, JsonSerializerOptions options)
        {
            var value = token.Type switch
            {
                Newtonsoft.Json.Linq.JTokenType.String => token.ToObject<string>(),
                Newtonsoft.Json.Linq.JTokenType.Integer => token.ToObject<long>(),
                Newtonsoft.Json.Linq.JTokenType.Float => token.ToObject<double>(),
                Newtonsoft.Json.Linq.JTokenType.Boolean => token.ToObject<bool>(),
                Newtonsoft.Json.Linq.JTokenType.Null => (object?)null,
                _ => token.ToString(),
            };
            System.Text.Json.JsonSerializer.Serialize(writer, value, options);
        }
    }
}
