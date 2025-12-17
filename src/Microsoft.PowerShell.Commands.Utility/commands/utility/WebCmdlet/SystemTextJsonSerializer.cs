// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

using NewtonsoftStringEscapeHandling = Newtonsoft.Json.StringEscapeHandling;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Provides JSON serialization using System.Text.Json with PowerShell-specific handling.
    /// </summary>
    /// <remarks>
    /// This implementation uses Utf8JsonWriter directly instead of JsonSerializer.Serialize()
    /// to provide full control over depth tracking and graceful handling of depth limits.
    /// Unlike standard System.Text.Json behavior (which throws on depth exceeded),
    /// this implementation converts deep objects to their string representation.
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
                    serializer.WriteValue(writer, objectToProcess);
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
    /// Writes PowerShell objects to JSON using an iterative (non-recursive) approach.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class uses an explicit stack instead of recursion to avoid stack overflow
    /// when serializing deeply nested objects. This allows safe handling of any depth
    /// up to the configured maximum without risking call stack exhaustion.
    /// </para>
    /// <para>
    /// Key features:
    /// - Iterative depth tracking with graceful degradation (string conversion) on depth exceeded
    /// - Support for PSObject with extended/adapted properties
    /// - Support for non-string dictionary keys (converted via ToString())
    /// - Respects JsonIgnoreAttribute and PowerShell's HiddenAttribute
    /// - Special handling for Int64/UInt64 enums (JavaScript precision issue).
    /// </para>
    /// </remarks>
    internal sealed class PowerShellJsonWriter
    {
        private readonly int _maxDepth;
        private readonly bool _enumsAsStrings;
        private readonly PSCmdlet? _cmdlet;
        private readonly CancellationToken _cancellationToken;

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
        }

        #region Stack-based Task Types

        /// <summary>
        /// Represents the type of task to be processed.
        /// </summary>
        private enum TaskType
        {
            /// <summary>Write a value (may be primitive or complex).</summary>
            WriteValue,

            /// <summary>Write end of JSON object.</summary>
            EndObject,

            /// <summary>Write end of JSON array.</summary>
            EndArray,
        }

        /// <summary>
        /// Represents a task on the processing stack.
        /// </summary>
        private readonly struct WriteTask
        {
            public readonly TaskType Type;
            public readonly string? PropertyName;
            public readonly object? Value;
            public readonly PSObject? PSObject;
            public readonly int Depth;

            private WriteTask(TaskType type, string? propertyName, object? value, PSObject? pso, int depth)
            {
                Type = type;
                PropertyName = propertyName;
                Value = value;
                PSObject = pso;
                Depth = depth;
            }

            public static WriteTask ForValue(object? value, int depth, string? propertyName = null)
                => new(TaskType.WriteValue, propertyName, value, value as PSObject, depth);

            public static WriteTask ForValueWithPSObject(object? value, PSObject? pso, int depth, string? propertyName = null)
                => new(TaskType.WriteValue, propertyName, value, pso, depth);

            public static WriteTask ForEndObject() => new(TaskType.EndObject, null, null, null, 0);

            public static WriteTask ForEndArray() => new(TaskType.EndArray, null, null, null, 0);
        }

        #endregion

        #region Main Entry Point

        /// <summary>
        /// Writes a value to JSON using an iterative approach.
        /// </summary>
        internal void WriteValue(Utf8JsonWriter writer, object? value)
        {
            var stack = new Stack<WriteTask>();
            stack.Push(WriteTask.ForValue(value, 0));

            while (stack.Count > 0)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var task = stack.Pop();

                switch (task.Type)
                {
                    case TaskType.EndObject:
                        writer.WriteEndObject();
                        break;

                    case TaskType.EndArray:
                        writer.WriteEndArray();
                        break;

                    case TaskType.WriteValue:
                        ProcessWriteValue(writer, stack, task);
                        break;
                }
            }
        }

        /// <summary>
        /// Processes a WriteValue task.
        /// </summary>
        private void ProcessWriteValue(Utf8JsonWriter writer, Stack<WriteTask> stack, WriteTask task)
        {
            // Write property name if present
            if (task.PropertyName is not null)
            {
                writer.WritePropertyName(task.PropertyName);
            }

            object? value = task.Value;
            int currentDepth = task.Depth;

            // Handle null
            if (value is null || LanguagePrimitives.IsNull(value))
            {
                writer.WriteNullValue();
                return;
            }

            // Unwrap PSObject and get base object
            PSObject? pso = task.PSObject ?? (value as PSObject);
            object baseObject = pso?.BaseObject ?? value;

            // Handle special null-like values (NullString, DBNull)
            if (TryWriteNullLike(writer, stack, baseObject, pso, currentDepth))
            {
                return;
            }

            // Handle primitive types (string, numbers, dates, etc.)
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

            // For complex types, check depth limit
            if (currentDepth > _maxDepth)
            {
                WriteDepthExceeded(writer, baseObject, pso);
                return;
            }

            // Handle complex types by pushing tasks onto the stack
            ProcessComplexValue(writer, stack, baseObject, pso, currentDepth);
        }

        #endregion

        #region Primitive Types

        /// <summary>
        /// Attempts to write a primitive value. Returns true if the value was handled.
        /// </summary>
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

                // Integer types
                case int i:
                    writer.WriteNumberValue(i);
                    return true;
                case long l:
                    writer.WriteNumberValue(l);
                    return true;
                case byte by:
                    writer.WriteNumberValue(by);
                    return true;
                case sbyte sb:
                    writer.WriteNumberValue(sb);
                    return true;
                case short sh:
                    writer.WriteNumberValue(sh);
                    return true;
                case ushort us:
                    writer.WriteNumberValue(us);
                    return true;
                case uint ui:
                    writer.WriteNumberValue(ui);
                    return true;
                case ulong ul:
                    writer.WriteNumberValue(ul);
                    return true;

                // Floating point types
                case double d:
                    writer.WriteNumberValue(d);
                    return true;
                case float f:
                    writer.WriteNumberValue(f);
                    return true;
                case decimal dec:
                    writer.WriteNumberValue(dec);
                    return true;

                // BigInteger (written as raw number to preserve precision)
                case BigInteger bi:
                    writer.WriteRawValue(bi.ToString(CultureInfo.InvariantCulture));
                    return true;

                // Date/time types
                case DateTime dt:
                    writer.WriteStringValue(dt);
                    return true;
                case DateTimeOffset dto:
                    writer.WriteStringValue(dto);
                    return true;

                // Other simple types
                case Guid g:
                    writer.WriteStringValue(g);
                    return true;
                case Uri uri:
                    writer.WriteStringValue(uri.OriginalString);
                    return true;
                case char c:
                    writer.WriteStringValue(c.ToString());
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Writes an enum value, handling Int64/UInt64 specially for JavaScript compatibility.
        /// </summary>
        private void WriteEnum(Utf8JsonWriter writer, object value)
        {
            if (_enumsAsStrings)
            {
                writer.WriteStringValue(value.ToString());
                return;
            }

            // Int64/UInt64 based enums must be written as strings
            // because JavaScript cannot represent them precisely
            Type underlyingType = Enum.GetUnderlyingType(value.GetType());
            if (underlyingType == typeof(long) || underlyingType == typeof(ulong))
            {
                writer.WriteStringValue(value.ToString());
            }
            else
            {
                writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
            }
        }

        #endregion

        #region Complex Types

        /// <summary>
        /// Processes a complex value by pushing appropriate tasks onto the stack.
        /// </summary>
        private static void ProcessComplexValue(Utf8JsonWriter writer, Stack<WriteTask> stack, object value, PSObject? pso, int currentDepth)
        {
            // Handle Newtonsoft.Json JObject (for backward compatibility)
            if (value is Newtonsoft.Json.Linq.JObject jObject)
            {
                ProcessDictionary(writer, stack, jObject.ToObject<Dictionary<string, object?>>()!, null, currentDepth);
                return;
            }

            // Handle dictionaries
            if (value is IDictionary dict)
            {
                ProcessDictionary(writer, stack, dict, pso, currentDepth);
                return;
            }

            // Handle enumerables (arrays, lists, etc.)
            if (value is IEnumerable enumerable)
            {
                ProcessArray(writer, stack, enumerable, currentDepth);
                return;
            }

            // Handle custom objects (classes, structs)
            ProcessCustomObject(writer, stack, value, pso, currentDepth);
        }

        /// <summary>
        /// Processes a dictionary by pushing tasks for each entry onto the stack.
        /// </summary>
        private static void ProcessDictionary(Utf8JsonWriter writer, Stack<WriteTask> stack, IDictionary dict, PSObject? pso, int currentDepth)
        {
            writer.WriteStartObject();

            // Collect entries to push in reverse order (stack is LIFO)
            var entries = new List<(string Key, object? Value)>();

            foreach (DictionaryEntry entry in dict)
            {
                string key = entry.Key?.ToString() ?? string.Empty;
                entries.Add((key, entry.Value));
            }

            // Add extended properties if present
            if (pso is not null)
            {
                CollectExtendedProperties(entries, pso, dict, currentDepth, isCustomObject: false);
            }

            // Push EndObject first (will be processed last)
            stack.Push(WriteTask.ForEndObject());

            // Push entries in reverse order
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                stack.Push(WriteTask.ForValue(entries[i].Value, currentDepth + 1, entries[i].Key));
            }
        }

        /// <summary>
        /// Processes an array by pushing tasks for each element onto the stack.
        /// </summary>
        private static void ProcessArray(Utf8JsonWriter writer, Stack<WriteTask> stack, IEnumerable enumerable, int currentDepth)
        {
            writer.WriteStartArray();

            // Collect items to push in reverse order
            var items = new List<object?>();
            foreach (object? item in enumerable)
            {
                items.Add(item);
            }

            // Push EndArray first (will be processed last)
            stack.Push(WriteTask.ForEndArray());

            // Push items in reverse order
            for (int i = items.Count - 1; i >= 0; i--)
            {
                stack.Push(WriteTask.ForValue(items[i], currentDepth + 1));
            }
        }

        /// <summary>
        /// Processes a custom object by pushing tasks for each property onto the stack.
        /// </summary>
        private static void ProcessCustomObject(Utf8JsonWriter writer, Stack<WriteTask> stack, object value, PSObject? pso, int currentDepth)
        {
            writer.WriteStartObject();

            Type type = value.GetType();
            var entries = new List<(string Key, object? Value)>();
            var writtenProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Collect public fields
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (ShouldSkipMember(field))
                {
                    continue;
                }

                object? fieldValue = TryGetFieldValue(field, value);
                entries.Add((field.Name, fieldValue));
                writtenProperties.Add(field.Name);
            }

            // Collect public properties
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (ShouldSkipMember(property))
                {
                    continue;
                }

                MethodInfo? getter = property.GetGetMethod();
                if (getter is null || getter.GetParameters().Length > 0)
                {
                    continue;
                }

                object? propertyValue = TryGetPropertyValue(getter, value);
                entries.Add((property.Name, propertyValue));
                writtenProperties.Add(property.Name);
            }

            // Add extended properties from PSObject
            if (pso is not null)
            {
                CollectExtendedProperties(entries, pso, writtenProperties, currentDepth, isCustomObject: true);
            }

            // Push EndObject first (will be processed last)
            stack.Push(WriteTask.ForEndObject());

            // Push entries in reverse order
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                stack.Push(WriteTask.ForValue(entries[i].Value, currentDepth + 1, entries[i].Key));
            }
        }

        #endregion

        #region PSObject Support

        /// <summary>
        /// Handles NullString and DBNull values, which may have extended properties.
        /// </summary>
        private static bool TryWriteNullLike(Utf8JsonWriter writer, Stack<WriteTask> stack, object value, PSObject? pso, int currentDepth)
        {
            if (value != System.Management.Automation.Language.NullString.Value && value != DBNull.Value)
            {
                return false;
            }

            if (pso is not null && HasExtendedProperties(pso))
            {
                ProcessObjectWithNullValue(writer, stack, pso, currentDepth);
            }
            else
            {
                writer.WriteNullValue();
            }

            return true;
        }

        /// <summary>
        /// Processes an object with a null base value but with extended properties.
        /// </summary>
        private static void ProcessObjectWithNullValue(Utf8JsonWriter writer, Stack<WriteTask> stack, PSObject pso, int currentDepth)
        {
            writer.WriteStartObject();

            var entries = new List<(string Key, object? Value)>
            {
                ("value", null),
            };

            CollectExtendedProperties(entries, pso, writtenKeys: null, currentDepth, isCustomObject: false);

            // Push EndObject first
            stack.Push(WriteTask.ForEndObject());

            // Push entries in reverse order
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                stack.Push(WriteTask.ForValue(entries[i].Value, currentDepth + 1, entries[i].Key));
            }
        }

        /// <summary>
        /// Collects extended (and optionally adapted) properties from a PSObject.
        /// </summary>
        private static void CollectExtendedProperties(
            List<(string Key, object? Value)> entries,
            PSObject pso,
            object? writtenKeys,
            int currentDepth,
            bool isCustomObject)
        {
            // DateTime and String should not have extended properties appended
            if (pso.BaseObject is string || pso.BaseObject is DateTime)
            {
                return;
            }

            PSMemberViewTypes viewTypes = isCustomObject
                ? PSMemberViewTypes.Extended | PSMemberViewTypes.Adapted
                : PSMemberViewTypes.Extended;

            var properties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                pso,
                PSObject.GetPropertyCollection(viewTypes));

            foreach (PSPropertyInfo prop in properties)
            {
                if (IsPropertyAlreadyWritten(prop.Name, writtenKeys))
                {
                    continue;
                }

                object? propValue = TryGetPSPropertyValue(prop);
                entries.Add((prop.Name, propValue));
            }
        }

        private static bool HasExtendedProperties(PSObject pso)
        {
            var properties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                pso,
                PSObject.GetPropertyCollection(PSMemberViewTypes.Extended));

            foreach (var _ in properties)
            {
                return true;
            }

            return false;
        }

        private static bool IsPropertyAlreadyWritten(string name, object? writtenKeys)
        {
            return writtenKeys switch
            {
                IDictionary dict => dict.Contains(name),
                HashSet<string> hashSet => hashSet.Contains(name),
                _ => false,
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Writes a string representation when max depth is exceeded.
        /// </summary>
        private void WriteDepthExceeded(Utf8JsonWriter writer, object value, PSObject? pso)
        {
            SystemTextJsonSerializer.WriteMaxDepthWarning(_maxDepth, _cmdlet);

            string stringValue = pso is not null && pso.ImmediateBaseObjectIsEmpty
                ? LanguagePrimitives.ConvertTo<string>(pso)
                : LanguagePrimitives.ConvertTo<string>(value);

            writer.WriteStringValue(stringValue);
        }

        /// <summary>
        /// Checks if a member should be skipped during serialization.
        /// </summary>
        private static bool ShouldSkipMember(MemberInfo member)
        {
            return member.IsDefined(typeof(JsonIgnoreAttribute), inherit: true)
                || member.IsDefined(typeof(HiddenAttribute), inherit: true);
        }

        /// <summary>
        /// Safely gets a field value, returning null on exception.
        /// </summary>
        private static object? TryGetFieldValue(FieldInfo field, object obj)
        {
            try
            {
                return field.GetValue(obj);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely gets a property value, returning null on exception.
        /// </summary>
        private static object? TryGetPropertyValue(MethodInfo getter, object obj)
        {
            try
            {
                return getter.Invoke(obj, Array.Empty<object>());
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely gets a PSPropertyInfo value, returning null on exception.
        /// </summary>
        private static object? TryGetPSPropertyValue(PSPropertyInfo prop)
        {
            try
            {
                return prop.Value;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
