// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Test-Json command.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "Json", DefaultParameterSetName = JsonStringParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096609")]
    [OutputType(typeof(bool))]
    public class TestJsonCommand : PSCmdlet
    {
        private const string JsonStringParameterSet = "JsonString";
        private const string JsonStringWithSchemaStringParameterSet = "JsonStringWithSchemaString";
        private const string JsonStringWithSchemaFileParameterSet = "JsonStringWithSchemaFile";
        private const string JsonFileParameterSet = "JsonFile";
        private const string JsonFileWithSchemaStringParameterSet = "JsonFileWithSchemaString";
        private const string JsonFileWithSchemaFileParameterSet = "JsonFileWithSchemaFile";

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
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = JsonFileParameterSet)]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = JsonFileWithSchemaStringParameterSet)]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = JsonFileWithSchemaFileParameterSet)]
        [Alias("JsonFile")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets schema to validate the JSON against.
        /// This is optional parameter.
        /// If the parameter is absent the cmdlet only attempts to parse the JSON string.
        /// If the parameter present the cmdlet attempts to parse the JSON string and
        /// then validates the JSON against the schema. Before testing the JSON string,
        /// the cmdlet parses the schema doing implicitly check the schema too.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = JsonStringWithSchemaStringParameterSet)]
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = JsonFileWithSchemaStringParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets path to the file containing schema to validate the JSON string against.
        /// This is optional parameter.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = JsonStringWithSchemaFileParameterSet)]
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = JsonFileWithSchemaFileParameterSet)]
        [ValidateNotNullOrEmpty]
        public string SchemaFile { get; set; }

        private string _json;
        private JsonSchema _jschema;

        /// <summary>
        /// Process all exceptions in the AggregateException.
        /// Unwrap TargetInvocationException if any and
        /// rethrow inner exception without losing the stack trace.
        /// </summary>
        /// <param name="e">AggregateException to be unwrapped.</param>
        /// <returns>Return value is unreachable since we always rethrow.</returns>
        private static bool UnwrapException(Exception e)
        {
            if (e.InnerException != null && e is TargetInvocationException)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
            else
            {
                ExceptionDispatchInfo.Capture(e).Throw();
            }

            return true;
        }

        /// <summary>
        /// Prepare a JSON schema.
        /// </summary>
        protected override void BeginProcessing()
        {
            string resolvedpath = string.Empty;

            try
            {
                if (Json != null)
                {
                    _json = Json;
                }
                else if (Path != null)
                {
                    try
                    {
                        resolvedpath = Context.SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
                        _json = File.ReadAllText(Path);
                    }
                    catch (AggregateException ae)
                    {
                        ae.Handle(UnwrapException);
                    }
                }
            }
            catch (Exception e) when (

                // Handle exceptions related to file access to provide more specific error message
                // https://docs.microsoft.com/en-us/dotnet/standard/io/handling-io-errors
                e is IOException ||
                e is UnauthorizedAccessException ||
                e is NotSupportedException ||
                e is SecurityException)
            {
                Exception exception = new(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        TestJsonCmdletStrings.JsonFileOpenFailure,
                        resolvedpath),
                    e);
                ThrowTerminatingError(new ErrorRecord(exception, "JsonFileOpenFailure", ErrorCategory.OpenError, resolvedpath));
            }

            try
            {
                if (Schema != null)
                {
                    try
                    {
                        _jschema = JsonSchema.FromJsonAsync(Schema).Result;
                    }
                    catch (AggregateException ae)
                    {
                        // Even if only one exception is thrown, it is still wrapped in an AggregateException exception
                        // https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/exception-handling-task-parallel-library
                        ae.Handle(UnwrapException);
                    }
                }
                else if (SchemaFile != null)
                {
                    try
                    {
                        resolvedpath = Context.SessionState.Path.GetUnresolvedProviderPathFromPSPath(SchemaFile);
                        _jschema = JsonSchema.FromFileAsync(resolvedpath).Result;
                    }
                    catch (AggregateException ae)
                    {
                        ae.Handle(UnwrapException);
                    }
                }
            }
            catch (Exception e) when (

                // Handle exceptions related to file access to provide more specific error message
                // https://docs.microsoft.com/en-us/dotnet/standard/io/handling-io-errors
                e is IOException ||
                e is UnauthorizedAccessException ||
                e is NotSupportedException ||
                e is SecurityException)
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
                var parsedJson = JToken.Parse(_json);

                if (_jschema != null)
                {
                    var errorMessages = _jschema.Validate(parsedJson);
                    if (errorMessages != null && errorMessages.Count != 0)
                    {
                        result = false;

                        Exception exception = new(TestJsonCmdletStrings.InvalidJsonAgainstSchema);

                        foreach (var message in errorMessages)
                        {
                            ErrorRecord errorRecord = new(exception, "InvalidJsonAgainstSchema", ErrorCategory.InvalidData, null);
                            errorRecord.ErrorDetails = new ErrorDetails(message.ToString());
                            WriteError(errorRecord);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                result = false;

                Exception exception = new(TestJsonCmdletStrings.InvalidJson, exc);
                WriteError(new ErrorRecord(exception, "InvalidJson", ErrorCategory.InvalidData, _json));
            }

            WriteObject(result);
        }
    }
}
