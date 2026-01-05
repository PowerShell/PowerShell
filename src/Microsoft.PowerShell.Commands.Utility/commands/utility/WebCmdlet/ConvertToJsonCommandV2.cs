// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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

        private bool _warningWritten;

        /// <summary>
        /// Writes a warning message once when depth is exceeded.
        /// </summary>
        internal void WriteWarningOnce()
        {
            if (!_warningWritten)
            {
                _warningWritten = true;
                WriteWarning(string.Format(
                    CultureInfo.CurrentCulture,
                    WebCmdletStrings.JsonMaxDepthReached,
                    Depth));
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
                object? objectToProcess = (_inputObjects.Count > 1 || AsArray) ? _inputObjects : _inputObjects[0];

                string? output = SystemTextJsonSerializer.ConvertToJson(
                    objectToProcess,
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
            bool enumsAsStrings,
            bool compressOutput,
            JsonStringEscapeHandling stringEscapeHandling,
            ConvertToJsonCommandV2 cmdlet,
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
                    // User-specified depth is enforced by PSJsonPSObjectConverter (max 100 via ValidateRange)
                    MaxDepth = 1000,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                    Encoder = GetEncoder(stringEscapeHandling),
                };

                if (enumsAsStrings)
                {
                    options.Converters.Add(new JsonStringEnumConverter());
                }

                // Add custom converters for PowerShell-specific types (alphabetical order)
                options.Converters.Add(new PSJsonBigIntegerConverter());
                options.Converters.Add(new PSJsonDBNullConverter());
                options.Converters.Add(new PSJsonDoubleConverter());
                options.Converters.Add(new PSJsonFloatConverter());

                if (!ExperimentalFeature.IsEnabled(ExperimentalFeature.PSSerializeJSONLongEnumAsNumber))
                {
                    options.Converters.Add(new PSJsonInt64EnumConverter());
                }

                options.Converters.Add(new PSJsonJObjectConverter());
                options.Converters.Add(new PSJsonNullStringConverter());
                options.Converters.Add(new PSJsonTypeConverter());

                // PSJsonPSObjectConverter handles PSObject with Extended/Adapted properties
                var factory = new PSJsonCompositeConverterFactory(cmdlet);
                options.Converters.Add(new PSJsonPSObjectConverter(cmdlet));

                // PSJsonCompositeConverterFactory must be last - it handles all non-primitive types
                // that don't have dedicated converters above
                options.Converters.Add(factory);

                // PSObject uses PSJsonPSObjectConverter (Extended/Adapted properties)
                // Raw objects use PSJsonCompositeConverterFactory (Base properties only)
                Type typeToProcess = objectToProcess is PSObject ? typeof(PSObject) : objectToProcess.GetType();
                return System.Text.Json.JsonSerializer.Serialize(objectToProcess, typeToProcess, options);
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
    /// JSON converter for PSObject type.
    /// </summary>
    internal sealed class PSJsonPSObjectConverter : System.Text.Json.Serialization.JsonConverter<PSObject>
    {
        private readonly ConvertToJsonCommandV2 _cmdlet;

        public PSJsonPSObjectConverter(ConvertToJsonCommandV2 cmdlet)
        {
            _cmdlet = cmdlet;
        }

        private int MaxDepth => _cmdlet.Depth;

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

            object obj = pso.BaseObject;
            Debug.Assert(obj is not null, "PSObject.BaseObject should never be null");

            if (TryWriteNullLikeValue(writer, pso, obj, options))
            {
                return;
            }

            if (TryWriteScalar(writer, pso, obj, options))
            {
                return;
            }

            if (TryWriteDepthExceeded(writer, pso, obj, options))
            {
                return;
            }

            WriteComposite(writer, pso, obj, options);
        }

        private bool TryWriteNullLikeValue(Utf8JsonWriter writer, PSObject pso, object obj, JsonSerializerOptions options)
        {
            if (obj is not (DBNull or System.Management.Automation.Language.NullString))
            {
                return false;
            }

            // Check for ETS properties
            var etsProperties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                pso,
                PSObject.GetPropertyCollection(PSMemberViewTypes.Extended));

            bool hasProperties = false;
            foreach (var prop in etsProperties)
            {
                if (JsonSerializerHelper.ShouldSkipProperty(prop))
                {
                    continue;
                }

                if (!hasProperties)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("value");
                    writer.WriteNullValue();
                    hasProperties = true;
                }

                WriteProperty(writer, prop, options);
            }

            if (hasProperties)
            {
                writer.WriteEndObject();
            }
            else
            {
                writer.WriteNullValue();
            }

            return true;
        }

        private static bool TryWriteScalar(Utf8JsonWriter writer, PSObject pso, object obj, JsonSerializerOptions options)
        {
            // JObject needs special handling before scalar check
            if (obj is Newtonsoft.Json.Linq.JObject jObject)
            {
                System.Text.Json.JsonSerializer.Serialize(writer, jObject, options);
                return true;
            }

            if (!JsonSerializerHelper.IsStjNativeScalarType(obj))
            {
                return false;
            }

            // V1 primitive types always serialize as scalars, ignoring ETS properties
            // Non-primitive STJ scalar types (Version, IPAddress, etc.) serialize as objects if they have ETS properties
            if (!IsV1PrimitiveType(obj.GetType()))
            {
                var extendedProps = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                    pso,
                    PSObject.GetPropertyCollection(PSMemberViewTypes.Extended));

                foreach (var prop in extendedProps)
                {
                    if (!JsonSerializerHelper.ShouldSkipProperty(prop))
                    {
                        return false;  // Has ETS properties, don't serialize as scalar
                    }
                }
            }

            JsonSerializerHelper.SerializePrimitive(writer, obj, options);
            return true;
        }

        /// <summary>
        /// Returns true for types that V1 treats as primitives (ETS properties are always ignored).
        /// </summary>
        private static bool IsV1PrimitiveType(Type type)
        {
            return type == typeof(string) ||
                   type == typeof(char) ||
                   type == typeof(bool) ||
                   type == typeof(DateTime) ||
                   type == typeof(DateTimeOffset) ||
                   type == typeof(Guid) ||
                   type == typeof(Uri) ||
                   type == typeof(double) ||
                   type == typeof(float) ||
                   type == typeof(decimal) ||
                   type == typeof(BigInteger) ||
                   type.IsPrimitive ||
                   type.IsEnum;
        }

        private bool TryWriteDepthExceeded(Utf8JsonWriter writer, PSObject pso, object obj, JsonSerializerOptions options)
        {
            if (writer.CurrentDepth <= MaxDepth)
            {
                return false;
            }

            WriteDepthExceeded(writer, pso, obj, options);
            return true;
        }

        private void WriteComposite(Utf8JsonWriter writer, PSObject pso, object obj, JsonSerializerOptions options)
        {
            if (obj is IDictionary dict)
            {
                SerializeDictionary(writer, pso, dict, options);
            }
            else if (obj is IEnumerable enumerable)
            {
                SerializeEnumerableWithEts(writer, pso, enumerable, options);
            }
            else
            {
                SerializeAsObject(writer, pso, options);
            }
        }

        private void WriteDepthExceeded(Utf8JsonWriter writer, PSObject pso, object obj, JsonSerializerOptions options)
        {
            _cmdlet.WriteWarningOnce();

            // Pure PSObject: convert to string only (V1 behavior)
            if (pso.ImmediateBaseObjectIsEmpty)
            {
                string stringValue = LanguagePrimitives.ConvertTo<string>(pso);
                writer.WriteStringValue(stringValue);
                return;
            }

            // Non-pure PSObject: check for ETS properties (V1 AddPsProperties behavior)
            // Include Extended and Adapted properties like V1 does for custom objects
            var etsProperties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                pso,
                PSObject.GetPropertyCollection(PSMemberViewTypes.Extended | PSMemberViewTypes.Adapted));

            bool hasProperties = false;
            string baseStringValue = LanguagePrimitives.ConvertTo<string>(obj);

            foreach (var prop in etsProperties)
            {
                if (JsonSerializerHelper.ShouldSkipProperty(prop))
                {
                    continue;
                }

                if (!hasProperties)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("value");
                    writer.WriteStringValue(baseStringValue);
                    hasProperties = true;
                }

                WriteProperty(writer, prop, options);
            }

            if (hasProperties)
            {
                writer.WriteEndObject();
            }
            else
            {
                writer.WriteStringValue(baseStringValue);
            }
        }

        private static void SerializeEnumerable(Utf8JsonWriter writer, IEnumerable enumerable, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in enumerable)
            {
                WriteValue(writer, item, options);
            }

            writer.WriteEndArray();
        }

        private void SerializeEnumerableWithEts(Utf8JsonWriter writer, PSObject pso, IEnumerable enumerable, JsonSerializerOptions options)
        {
            // Check for ETS properties (Extended only, like V1)
            var etsProperties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                pso,
                PSObject.GetPropertyCollection(PSMemberViewTypes.Extended));

            // Collect ETS properties that should be serialized
            var propsToSerialize = new List<PSPropertyInfo>();
            foreach (var prop in etsProperties)
            {
                if (!JsonSerializerHelper.ShouldSkipProperty(prop))
                {
                    propsToSerialize.Add(prop);
                }
            }

            if (propsToSerialize.Count == 0)
            {
                // No ETS properties, serialize as plain array
                SerializeEnumerable(writer, enumerable, options);
                return;
            }

            // Has ETS properties: serialize as {"value":[...],"prop":"..."}
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            SerializeEnumerable(writer, enumerable, options);

            foreach (var prop in propsToSerialize)
            {
                WriteProperty(writer, prop, options);
            }

            writer.WriteEndObject();
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

        private static void WriteValue(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            // Delegate to JsonSerializerHelper for consistent serialization
            JsonSerializerHelper.WriteValue(writer, value, options);
        }

        private void SerializeAsObject(Utf8JsonWriter writer, PSObject pso, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            AppendPSProperties(writer, pso, options, PSMemberViewTypes.Extended | PSMemberViewTypes.Adapted);

            writer.WriteEndObject();
        }

        private void AppendPSProperties(
            Utf8JsonWriter writer,
            PSObject pso,
            JsonSerializerOptions options,
            PSMemberViewTypes memberTypes)
        {
            var properties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                pso,
                PSObject.GetPropertyCollection(memberTypes));

            foreach (var prop in properties)
            {
                if (JsonSerializerHelper.ShouldSkipProperty(prop))
                {
                    continue;
                }

                WriteProperty(writer, prop, options);
            }
        }

        private void WriteProperty(Utf8JsonWriter writer, PSPropertyInfo prop, JsonSerializerOptions options)
        {
            try
            {
                var value = prop.Value;
                writer.WritePropertyName(prop.Name);

                // If maxDepth is 0 and value is non-null non-scalar, convert to string
                if (MaxDepth == 0 && value is not null && !JsonSerializerHelper.IsStjNativeScalarType(value))
                {
                    writer.WriteStringValue(value.ToString());
                }
                else
                {
                    WriteValue(writer, value, options);
                }
            }
            catch
            {
                // Skip properties that throw on access - write nothing for this property
            }
        }
    }

    /// <summary>
    /// JsonConverter for Int64/UInt64 enums to avoid JavaScript precision issues.
    /// </summary>
    internal sealed class PSJsonInt64EnumConverter : System.Text.Json.Serialization.JsonConverter<Enum>
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
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// JsonConverter for NullString to serialize as null.
    /// </summary>
    internal sealed class PSJsonNullStringConverter : System.Text.Json.Serialization.JsonConverter<System.Management.Automation.Language.NullString>
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
    internal sealed class PSJsonDBNullConverter : System.Text.Json.Serialization.JsonConverter<DBNull>
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
    internal sealed class PSJsonBigIntegerConverter : System.Text.Json.Serialization.JsonConverter<BigInteger>
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
    /// JsonConverter for double to serialize Infinity and NaN as strings for V1 compatibility.
    /// </summary>
    internal sealed class PSJsonDoubleConverter : System.Text.Json.Serialization.JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (double.IsPositiveInfinity(value))
            {
                writer.WriteStringValue("Infinity");
            }
            else if (double.IsNegativeInfinity(value))
            {
                writer.WriteStringValue("-Infinity");
            }
            else if (double.IsNaN(value))
            {
                writer.WriteStringValue("NaN");
            }
            else
            {
                writer.WriteNumberValue(value);
            }
        }
    }

    /// <summary>
    /// JsonConverter for float to serialize Infinity and NaN as strings for V1 compatibility.
    /// </summary>
    internal sealed class PSJsonFloatConverter : System.Text.Json.Serialization.JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            if (float.IsPositiveInfinity(value))
            {
                writer.WriteStringValue("Infinity");
            }
            else if (float.IsNegativeInfinity(value))
            {
                writer.WriteStringValue("-Infinity");
            }
            else if (float.IsNaN(value))
            {
                writer.WriteStringValue("NaN");
            }
            else
            {
                writer.WriteNumberValue(value);
            }
        }
    }

    /// <summary>
    /// JsonConverter for System.Type to serialize as AssemblyQualifiedName string for V1 compatibility.
    /// </summary>
    internal sealed class PSJsonTypeConverter : System.Text.Json.Serialization.JsonConverter<Type>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            // Handle Type and all derived types (e.g., RuntimeType)
            return typeof(Type).IsAssignableFrom(typeToConvert);
        }

        public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.AssemblyQualifiedName);
        }
    }

    /// <summary>
    /// Custom JsonConverter for Newtonsoft.Json.Linq.JObject to isolate Newtonsoft-related code.
    /// </summary>
    internal sealed class PSJsonJObjectConverter : System.Text.Json.Serialization.JsonConverter<Newtonsoft.Json.Linq.JObject>
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

    /// <summary>
    /// JsonConverterFactory that dispatches to appropriate converters for non-primitive types.
    /// Handles depth control and truncation for Dictionary, Enumerable, and composite objects.
    /// </summary>
    internal sealed class PSJsonCompositeConverterFactory : JsonConverterFactory
    {
        private readonly ConvertToJsonCommandV2 _cmdlet;

        // Cached converter instances (one per converter type)
        private PSJsonDictionaryConverter? _dictionaryConverter;
        private PSJsonEnumerableConverter? _enumerableConverter;
        private PSJsonCompositeConverter? _compositeConverter;

        public PSJsonCompositeConverterFactory(ConvertToJsonCommandV2 cmdlet)
        {
            _cmdlet = cmdlet;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            // Types with dedicated converters should be handled before reaching this factory
            Debug.Assert(
                typeToConvert != typeof(PSObject) &&
                !typeof(PSObject).IsAssignableFrom(typeToConvert) &&
                typeToConvert != typeof(BigInteger) &&
                typeToConvert != typeof(System.Management.Automation.Language.NullString) &&
                typeToConvert != typeof(DBNull) &&
                !typeof(Type).IsAssignableFrom(typeToConvert) &&
                typeToConvert != typeof(Newtonsoft.Json.Linq.JObject),
                $"Type {typeToConvert} should be handled by its dedicated converter");

            // Check if STJ handles this type as a scalar
            var typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeToConvert);
            return typeInfo.Kind != JsonTypeInfoKind.None;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            // Return cached non-generic converter instance
            if (typeof(IDictionary).IsAssignableFrom(typeToConvert))
            {
                return _dictionaryConverter ??= new PSJsonDictionaryConverter(this);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(typeToConvert))
            {
                return _enumerableConverter ??= new PSJsonEnumerableConverter(this);
            }
            else
            {
                return _compositeConverter ??= new PSJsonCompositeConverter(this);
            }
        }

        internal void WriteWarningOnce()
        {
            _cmdlet.WriteWarningOnce();
        }

        internal void WriteDepthExceeded(Utf8JsonWriter writer, object obj)
        {
            WriteWarningOnce();
            writer.WriteStringValue(obj.ToString());
        }

        internal int MaxDepth => _cmdlet.Depth;
    }

    /// <summary>
    /// Non-generic JsonConverter for Dictionary types (IDictionary).
    /// </summary>
    internal sealed class PSJsonDictionaryConverter : JsonConverter<object>
    {
        private readonly PSJsonCompositeConverterFactory _factory;

        public PSJsonDictionaryConverter(PSJsonCompositeConverterFactory factory)
        {
            _factory = factory;
        }

        public override bool CanConvert(Type typeToConvert) => typeof(IDictionary).IsAssignableFrom(typeToConvert);

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            var dict = (IDictionary)value;

            // Check depth limit
            if (writer.CurrentDepth > _factory.MaxDepth)
            {
                _factory.WriteDepthExceeded(writer, value);
                return;
            }

            writer.WriteStartObject();

            foreach (DictionaryEntry entry in dict)
            {
                string key = entry.Key?.ToString() ?? string.Empty;
                writer.WritePropertyName(key);
                JsonSerializerHelper.WriteValue(writer, entry.Value, options);
            }

            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Non-generic JsonConverter for Enumerable types (IEnumerable, excluding IDictionary).
    /// </summary>
    internal sealed class PSJsonEnumerableConverter : JsonConverter<object>
    {
        private readonly PSJsonCompositeConverterFactory _factory;

        public PSJsonEnumerableConverter(PSJsonCompositeConverterFactory factory)
        {
            _factory = factory;
        }

        public override bool CanConvert(Type typeToConvert) => typeof(IEnumerable).IsAssignableFrom(typeToConvert);

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            var enumerable = (IEnumerable)value;

            writer.WriteStartArray();

            foreach (var item in enumerable)
            {
                JsonSerializerHelper.WriteValue(writer, item, options);
            }

            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Non-generic JsonConverter for composite objects (non-primitive, non-collection types).
    /// Uses JsonTypeInfo to enumerate properties, leveraging STJ's internal caching and JsonIgnore handling.
    /// </summary>
    internal sealed class PSJsonCompositeConverter : JsonConverter<object>
    {
        private readonly PSJsonCompositeConverterFactory _factory;

        public PSJsonCompositeConverter(PSJsonCompositeConverterFactory factory)
        {
            _factory = factory;
        }

        public override bool CanConvert(Type typeToConvert) => true;

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            // Check depth limit
            if (writer.CurrentDepth > _factory.MaxDepth)
            {
                _factory.WriteDepthExceeded(writer, value);
                return;
            }

            writer.WriteStartObject();

            // Use JsonTypeInfo to enumerate properties - leverages STJ caching and handles JsonIgnore automatically
            var typeInfo = JsonSerializerOptions.Default.GetTypeInfo(value.GetType());
            foreach (var propInfo in typeInfo.Properties)
            {
                WriteProperty(writer, value, propInfo, options);
            }

            writer.WriteEndObject();
        }

        private void WriteProperty(Utf8JsonWriter writer, object obj, JsonPropertyInfo propInfo, JsonSerializerOptions options)
        {
            // Skip write-only properties
            if (propInfo.Get is null)
            {
                return;
            }

            try
            {
                var value = propInfo.Get(obj);
                writer.WritePropertyName(propInfo.Name);

                // If maxDepth is 0 and value is non-null non-scalar, convert to string
                if (_factory.MaxDepth == 0 && value is not null && !JsonSerializerHelper.IsStjNativeScalarType(value))
                {
                    writer.WriteStringValue(value.ToString());
                }
                else
                {
                    JsonSerializerHelper.WriteValue(writer, value, options);
                }
            }
            catch
            {
                // Skip properties that throw on access
            }
        }
    }

    /// <summary>
    /// Shared helper methods for JSON serialization.
    /// </summary>
    internal static class JsonSerializerHelper
    {
        /// <summary>
        /// Determines if STJ natively serializes the type as a scalar (string, number, boolean).
        /// Uses JsonTypeInfoKind to classify types.
        /// </summary>
        public static bool IsStjNativeScalarType(object obj)
        {
            var type = obj.GetType();

            // BigInteger: STJ serializes as object (Kind=Object), but V1 serializes as number.
            // Using JsonSerializerOptions.Default returns Kind=Object; using local options with
            // our custom PSJsonBigIntegerConverter would return Kind=None.
            // Must check explicitly since we use Default options here.
            if (type == typeof(BigInteger))
            {
                return true;
            }

            // System.Object has Kind=None but serializes as {} (not a scalar)
            // See: https://source.dot.net/#System.Text.Json/System/Text/Json/Serialization/Metadata/JsonTypeInfo.cs,1337
            if (type == typeof(object))
            {
                return false;
            }

            // GetTypeInfo() has internal caching, no need for additional cache
            var typeInfo = JsonSerializerOptions.Default.GetTypeInfo(type);
            return typeInfo.Kind == JsonTypeInfoKind.None;
        }

        public static void SerializePrimitive(Utf8JsonWriter writer, object obj, JsonSerializerOptions options)
        {
            // Delegate to STJ - custom converters handle special cases:
            // - PSJsonBigIntegerConverter: BigInteger as number string
            // - PSJsonDoubleConverter/Float: Infinity/NaN as string
            // - PSJsonTypeConverter: Type as AssemblyQualifiedName
            System.Text.Json.JsonSerializer.Serialize(writer, obj, obj.GetType(), options);
        }

        /// <summary>
        /// Determines if a property should be skipped during serialization.
        /// Checks for Hidden attribute and JsonIgnoreAttribute.
        /// Note: This is only called for PSObject-wrapped objects (via PSJsonPSObjectConverter).
        /// Script-defined classes passed as raw objects go through PSJsonCompositeConverter,
        /// which uses STJ's JsonTypeInfo.Properties and does not call this method.
        /// </summary>
        public static bool ShouldSkipProperty(PSPropertyInfo prop)
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

        /// <summary>
        /// Writes a value to the JSON writer, handling null, PSObject, and raw objects.
        /// Used by PSJsonCompositeConverterFactory converters for consistent value serialization.
        /// </summary>
        public static void WriteValue(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (LanguagePrimitives.IsNull(value))
            {
                writer.WriteNullValue();
            }
            else
            {
                Type typeToProcess = value is PSObject ? typeof(PSObject) : value.GetType();
                System.Text.Json.JsonSerializer.Serialize(writer, value, typeToProcess, options);
            }
        }
    }
}
