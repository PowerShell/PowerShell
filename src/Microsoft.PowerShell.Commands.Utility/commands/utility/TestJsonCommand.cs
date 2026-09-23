// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;
using System.Security;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Test-Json command.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "Json", DefaultParameterSetName = JsonStringParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096609")]
    [OutputType(typeof(bool))]
    public class TestJsonCommand : PSCmdlet
    {
        #region Parameter Set Names

        private const string JsonStringParameterSet = "JsonString";
        private const string JsonStringWithSchemaStringParameterSet = "JsonStringWithSchemaString";
        private const string JsonStringWithSchemaFileParameterSet = "JsonStringWithSchemaFile";
        private const string JsonPathParameterSet = "JsonPath";
        private const string JsonPathWithSchemaStringParameterSet = "JsonPathWithSchemaString";
        private const string JsonPathWithSchemaFileParameterSet = "JsonPathWithSchemaFile";
        private const string JsonLiteralPathParameterSet = "JsonLiteralPath";
        private const string JsonLiteralPathWithSchemaStringParameterSet = "JsonLiteralPathWithSchemaString";
        private const string JsonLiteralPathWithSchemaFileParameterSet = "JsonLiteralPathWithSchemaFile";

        #endregion

        #region Json Document Option Constants

        private const string IgnoreCommentsOption = "IgnoreComments";
        private const string AllowTrailingCommasOption = "AllowTrailingCommas";

        #endregion

        #region Parameters

        /// <summary>
        /// Gets or sets JSON string to be validated.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = JsonStringParameterSet)]
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = JsonStringWithSchemaStringParameterSet)]
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = JsonStringWithSchemaFileParameterSet)]
        public string Json { get; set; }

        /// <summary>
        /// Gets or sets JSON file path to be validated.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = JsonPathParameterSet)]
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = JsonPathWithSchemaStringParameterSet)]
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = JsonPathWithSchemaFileParameterSet)]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets JSON literal file path to be validated.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = JsonLiteralPathParameterSet)]
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = JsonLiteralPathWithSchemaStringParameterSet)]
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = JsonLiteralPathWithSchemaFileParameterSet)]
        [Alias("PSPath", "LP")]
        public string LiteralPath
        {
            get
            {
                return _isLiteralPath ? Path : null;
            }

            set
            {
                _isLiteralPath = true;
                Path = value;
            }
        }

        /// <summary>
        /// Gets or sets schema to validate the JSON against.
        /// This is optional parameter.
        /// If the parameter is absent the cmdlet only attempts to parse the JSON string.
        /// If the parameter present the cmdlet attempts to parse the JSON string and
        /// then validates the JSON against the schema. Before testing the JSON string,
        /// the cmdlet parses the schema doing implicitly check the schema too.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = JsonStringWithSchemaStringParameterSet)]
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = JsonPathWithSchemaStringParameterSet)]
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = JsonLiteralPathWithSchemaStringParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets path to the file containing schema to validate the JSON string against.
        /// This is optional parameter.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = JsonStringWithSchemaFileParameterSet)]
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = JsonPathWithSchemaFileParameterSet)]
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = JsonLiteralPathWithSchemaFileParameterSet)]
        [ValidateNotNullOrEmpty]
        public string SchemaFile { get; set; }

        /// <summary>
        /// Gets or sets JSON document options.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [ValidateSet(IgnoreCommentsOption, AllowTrailingCommasOption)]
        public string[] Options { get; set; } = Array.Empty<string>();

        #endregion

        #region Private Members

        private bool _isLiteralPath = false;
        private JsonSchema _jschema;
        private JsonDocumentOptions _documentOptions;

        #endregion

        /// <summary>
        /// Prepare a JSON schema.
        /// </summary>
        protected override void BeginProcessing()
        {
            // By default, a JSON Schema implementation isn't supposed to automatically fetch content.
            // Instead JsonSchema.Net has been set up with a registry so that users can pre-register
            // any schemas they may need to resolve.
            // However, pre-registering schemas doesn't make sense in the context of a Powershell command,
            // and automatically fetching referenced URIs is likely the preferred behavior.  To do that,
            // this property must be set with a method to retrieve and deserialize the content.
            // For more information, see https://json-everything.net/json-schema#automatic-resolution
            SchemaRegistry.Global.Fetch = static uri =>
            {
                try
                {
                    string text;
                    switch (uri.Scheme)
                    {
                        case "http":
                        case "https":
                            {
                                using var client = new HttpClient();
                                text = client.GetStringAsync(uri).Result;
                                break;
                            }
                        case "file":
                            var filename = Uri.UnescapeDataString(uri.AbsolutePath);
                            text = File.ReadAllText(filename);
                            break;
                        default:
                            throw new FormatException(string.Format(TestJsonCmdletStrings.InvalidUriScheme, uri.Scheme));
                    }

                    return JsonSerializer.Deserialize<JsonSchema>(text);
                }
                catch (Exception e)
                {
                    throw new JsonSchemaReferenceResolutionException(e);
                }
            };

            string resolvedpath = string.Empty;

            try
            {
                if (Schema != null)
                {
                    try
                    {
                        _jschema = JsonSchema.FromText(Schema);
                    }
                    catch (JsonException e)
                    {
                        Exception exception = new(TestJsonCmdletStrings.InvalidJsonSchema, e);
                        WriteError(new ErrorRecord(exception, "InvalidJsonSchema", ErrorCategory.InvalidData, Schema));
                    }
                }
                else if (SchemaFile != null)
                {
                    try
                    {
                        resolvedpath = Context.SessionState.Path.GetUnresolvedProviderPathFromPSPath(SchemaFile);
                        _jschema = JsonSchema.FromFile(resolvedpath);
                    }
                    catch (JsonException e)
                    {
                        Exception exception = new(TestJsonCmdletStrings.InvalidJsonSchema, e);
                        WriteError(new ErrorRecord(exception, "InvalidJsonSchema", ErrorCategory.InvalidData, SchemaFile));
                    }
                }
            }
            catch (Exception e) when (
                // Handle exceptions related to file access to provide more specific error message
                // https://learn.microsoft.com/dotnet/standard/io/handling-io-errors
                e is IOException ||
                e is UnauthorizedAccessException ||
                e is NotSupportedException ||
                e is SecurityException
            )
            {
                Exception exception = new(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        TestJsonCmdletStrings.JsonSchemaFileOpenFailure,
                        resolvedpath),
                    e);
                ThrowTerminatingError(new ErrorRecord(exception, "JsonSchemaFileOpenFailure", ErrorCategory.OpenError, resolvedpath));
            }
            catch (Exception e)
            {
                Exception exception = new(TestJsonCmdletStrings.InvalidJsonSchema, e);
                ThrowTerminatingError(new ErrorRecord(exception, "InvalidJsonSchema", ErrorCategory.InvalidData, resolvedpath));
            }

            _documentOptions = new JsonDocumentOptions
            {
                CommentHandling = Options.Contains(IgnoreCommentsOption, StringComparer.OrdinalIgnoreCase)
                    ? JsonCommentHandling.Skip
                    : JsonCommentHandling.Disallow,
                AllowTrailingCommas = Options.Contains(AllowTrailingCommasOption, StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// Validate a JSON.
        /// </summary>
        protected override void ProcessRecord()
        {
            bool result = true;

            string jsonToParse = string.Empty;

            if (Json != null)
            {
                jsonToParse = Json;
            }
            else if (Path != null)
            {
                string resolvedPath = PathUtils.ResolveFilePath(Path, this, _isLiteralPath);

                if (!File.Exists(resolvedPath))
                {
                    ItemNotFoundException exception = new(
                        Path,
                        "PathNotFound",
                        SessionStateStrings.PathNotFound);

                    ThrowTerminatingError(exception.ErrorRecord);
                }

                jsonToParse = File.ReadAllText(resolvedPath);
            }

            try
            {

                var parsedJson = JsonNode.Parse(jsonToParse, nodeOptions: null, _documentOptions);

                if (_jschema != null)
                {
                    EvaluationResults evaluationResults = _jschema.Evaluate(parsedJson, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
                    result = evaluationResults.IsValid;
                    if (!result)
                    {
                        ReportValidationErrors(evaluationResults);
                    }
                }
            }
            catch (JsonSchemaReferenceResolutionException jsonExc)
            {
                result = false;

                Exception exception = new(TestJsonCmdletStrings.InvalidJsonSchema, jsonExc);
                WriteError(new ErrorRecord(exception, "InvalidJsonSchema", ErrorCategory.InvalidData, _jschema));
            }
            catch (Exception exc)
            {
                result = false;

                Exception exception = new(TestJsonCmdletStrings.InvalidJson, exc);
                WriteError(new ErrorRecord(exception, "InvalidJson", ErrorCategory.InvalidData, Json));
            }

            WriteObject(result);
        }

        /// <summary>
        /// Recursively reports validation errors from hierarchical evaluation results.
        /// Skips nodes (and their children) where IsValid is true to avoid false positives
        /// from constructs like OneOf or AnyOf.
        /// </summary>
        /// <param name="evaluationResult">The evaluation result to process.</param>
        private void ReportValidationErrors(EvaluationResults evaluationResult)
        {
            // Skip this node and all children if validation passed
            if (evaluationResult.IsValid)
            {
                return;
            }

            // Report errors at this level
            HandleValidationErrors(evaluationResult);

            // Recursively process child results
            if (evaluationResult.HasDetails)
            {
                foreach (var nestedResult in evaluationResult.Details)
                {
                    ReportValidationErrors(nestedResult);
                }
            }
        }

        private void HandleValidationErrors(EvaluationResults evaluationResult)
        {
            if (!evaluationResult.HasErrors)
            {
                return;
            }

            foreach (var error in evaluationResult.Errors!)
            {
                Exception exception = new(string.Format(TestJsonCmdletStrings.InvalidJsonAgainstSchemaDetailed, error.Value, evaluationResult.InstanceLocation));
                ErrorRecord errorRecord = new(exception, "InvalidJsonAgainstSchemaDetailed", ErrorCategory.InvalidData, null);
                WriteError(errorRecord);
            }
        }
    }
}
