// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Microsoft.PowerShell.Development.Formatters
{
    /// <summary>
    /// Format-ForAI cmdlet formats objects in AI-friendly structured formats (JSON/YAML).
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "ForAI")]
    [OutputType(typeof(string))]
    [Alias("fai")]
    public sealed class FormatForAICommand : PSCmdlet
    {
        private readonly List<PSObject> _inputObjects = new List<PSObject>();

        /// <summary>
        /// Output format type.
        /// </summary>
        [Parameter(Position = 0)]
        [ValidateSet("Json", "Yaml", "Compact")]
        public string OutputType { get; set; } = "Json";

        /// <summary>
        /// Maximum depth for serialization.
        /// </summary>
        [Parameter]
        [ValidateRange(1, 100)]
        public int Depth { get; set; } = 10;

        /// <summary>
        /// Include type information in output.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeTypeInfo { get; set; }

        /// <summary>
        /// Input object(s) from pipeline.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        /// <summary>
        /// ProcessRecord - collect input objects.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (InputObject != null)
            {
                _inputObjects.Add(InputObject);
            }
        }

        /// <summary>
        /// EndProcessing - format all collected objects.
        /// </summary>
        protected override void EndProcessing()
        {
            if (_inputObjects.Count == 0)
            {
                return;
            }

            try
            {
                // Convert PSObjects to serializable format
                var serializable = ConvertToSerializable(_inputObjects);

                string output;
                switch (OutputType.ToLowerInvariant())
                {
                    case "json":
                        output = FormatAsJson(serializable);
                        break;
                    case "yaml":
                        output = FormatAsYaml(serializable);
                        break;
                    case "compact":
                        output = FormatAsCompactJson(serializable);
                        break;
                    default:
                        output = FormatAsJson(serializable);
                        break;
                }

                WriteObject(output);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "FormatForAIError",
                    ErrorCategory.InvalidOperation,
                    _inputObjects));
            }
        }

        private object ConvertToSerializable(List<PSObject> objects)
        {
            if (objects.Count == 1)
            {
                return ConvertPSObjectToSerializable(objects[0], 0);
            }
            else
            {
                var list = new List<object>();
                foreach (var obj in objects)
                {
                    list.Add(ConvertPSObjectToSerializable(obj, 0));
                }
                return list;
            }
        }

        private object ConvertPSObjectToSerializable(PSObject psObject, int currentDepth)
        {
            if (currentDepth >= Depth)
            {
                return psObject?.ToString() ?? "null";
            }

            if (psObject == null)
            {
                return null;
            }

            var baseObject = psObject.BaseObject;

            // Handle primitives
            if (baseObject == null ||
                baseObject is string ||
                baseObject is int ||
                baseObject is long ||
                baseObject is double ||
                baseObject is float ||
                baseObject is decimal ||
                baseObject is bool ||
                baseObject is DateTime ||
                baseObject is Guid)
            {
                return baseObject;
            }

            // Handle arrays/lists
            if (baseObject is IEnumerable enumerable && !(baseObject is string))
            {
                var list = new List<object>();
                foreach (var item in enumerable)
                {
                    if (item is PSObject psItem)
                    {
                        list.Add(ConvertPSObjectToSerializable(psItem, currentDepth + 1));
                    }
                    else
                    {
                        list.Add(item);
                    }
                }
                return list;
            }

            // Handle dictionaries
            if (baseObject is IDictionary dictionary)
            {
                var dict = new Dictionary<string, object>();
                foreach (DictionaryEntry entry in dictionary)
                {
                    var key = entry.Key?.ToString() ?? "null";
                    var value = entry.Value is PSObject psValue
                        ? ConvertPSObjectToSerializable(psValue, currentDepth + 1)
                        : entry.Value;
                    dict[key] = value;
                }
                return dict;
            }

            // Handle complex objects - extract properties
            var result = new Dictionary<string, object>();

            if (IncludeTypeInfo.IsPresent)
            {
                result["__TypeName"] = psObject.TypeNames[0];
            }

            foreach (var property in psObject.Properties)
            {
                try
                {
                    var value = property.Value;
                    if (value is PSObject psValue)
                    {
                        result[property.Name] = ConvertPSObjectToSerializable(psValue, currentDepth + 1);
                    }
                    else
                    {
                        result[property.Name] = value;
                    }
                }
                catch
                {
                    // Skip properties that can't be accessed
                    result[property.Name] = $"<Error accessing property>";
                }
            }

            return result;
        }

        private string FormatAsJson(object obj)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                PropertyNamingPolicy = null,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return JsonSerializer.Serialize(obj, options);
        }

        private string FormatAsCompactJson(object obj)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                PropertyNamingPolicy = null
            };

            return JsonSerializer.Serialize(obj, options);
        }

        private string FormatAsYaml(object obj)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .Build();

            return serializer.Serialize(obj);
        }
    }
}
