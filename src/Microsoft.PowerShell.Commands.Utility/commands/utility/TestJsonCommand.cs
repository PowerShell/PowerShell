// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
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
    [Cmdlet(VerbsDiagnostic.Test, "Json", DefaultParameterSetName = ParameterAttribute.AllParameterSets, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096609")]
    [OutputType(typeof(bool))]
    public class TestJsonCommand : PSCmdlet
    {
        private const string SchemaFileParameterSet = "SchemaFile";
        private const string SchemaStringParameterSet = "SchemaString";

        /// <summary>
        /// Gets or sets JSON string to be validated.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public string Json { get; set; }

        /// <summary>
        /// Gets or sets schema to validate the JSON against.
        /// This is optional parameter.
        /// If the parameter is absent the cmdlet only attempts to parse the JSON string.
        /// If the parameter present the cmdlet attempts to parse the JSON string and
        /// then validates the JSON against the schema. Before testing the JSON string,
        /// the cmdlet parses the schema doing implicitly check the schema too.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = SchemaStringParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets path to the file containing schema to validate the JSON string against.
        /// This is optional parameter.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = SchemaFileParameterSet)]
        [ValidateNotNullOrEmpty]
        public string SchemaFile { get; set; }

        private JsonSchema _jschema;

        /// <summary>
        /// Prepare a JSON schema.
        /// </summary>
        protected override void BeginProcessing()
        {
            SchemaRegistry.Global.Fetch = static uri =>
            {
                try
                {
                    string text;
                    switch (uri.Scheme)
                    {
                        case "http":
                        case "https":
                            text = new HttpClient().GetStringAsync(uri).Result;
                            break;
                        case "file":
                            var filename = Uri.UnescapeDataString(uri.AbsolutePath);
                            text = File.ReadAllText(filename);
                            break;
                        default:
                            throw new FormatException(
                                $"URI scheme '{uri.Scheme}' is not supported.  Only HTTP(S) and local file system URIs are allowed.");
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
                // https://docs.microsoft.com/en-us/dotnet/standard/io/handling-io-errors
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
        }

        /// <summary>
        /// Validate a JSON.
        /// </summary>
        protected override void ProcessRecord()
        {
            bool result = true;

            try
            {
                var parsedJson = JsonNode.Parse(Json);

                if (_jschema != null)
                {
                        var validationResults = _jschema.Validate(parsedJson, new ValidationOptions { OutputFormat = OutputFormat.Basic });
                        result = validationResults.IsValid;
                        if (!result)
                        {
                            Exception exception = new(TestJsonCmdletStrings.InvalidJsonAgainstSchema);

                            if (validationResults.Message != null)
                            {
                                ErrorRecord errorRecord = new(exception, "InvalidJsonAgainstSchema", ErrorCategory.InvalidData, null);
                                var message = $"{validationResults.Message} at {validationResults.InstanceLocation}";
                                errorRecord.ErrorDetails = new ErrorDetails(message);
                                WriteError(errorRecord);
                            }

                            if (validationResults.HasNestedResults)
                            {
                                foreach (var nestedResult in validationResults.NestedResults)
                                {
                                    if (nestedResult.Message == null)
                                    {
                                        continue;
                                    }

                                    ErrorRecord errorRecord = new(exception, "InvalidJsonAgainstSchema", ErrorCategory.InvalidData, null);
                                    var message = $"{nestedResult.Message} at {nestedResult.InstanceLocation}";
                                    errorRecord.ErrorDetails = new ErrorDetails(message);
                                    WriteError(errorRecord);
                                }
                            }
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
    }
}
