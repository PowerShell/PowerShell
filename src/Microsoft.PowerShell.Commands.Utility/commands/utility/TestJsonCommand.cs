// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.ExceptionServices;
using System.Reflection;
using System.Security;
using System.IO;
using System.Management.Automation;

using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Test-Json command.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "Json", DefaultParameterSetName = ParameterAttribute.AllParameterSets, HelpUri = "")]
    public class TestJsonCommand : PSCmdlet
    {
        private const string SchemaPathParameterSet = "SchemaPath";
        private const string SchemaStringParameterSet = "SchemaString";

        /// <summary>
        /// A JSON to be validated.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public string Json { get; set; }

        /// <summary>
        /// A schema to validate the JSON against.
        /// This is optional parameter.
        /// If the parameter is absent the cmdlet only attempts to parse the JSON string.
        /// If the parameter present the cmdlet attempts to parse the JSON string and
        /// then validates the JSON against the schema. Before testing the JSON string,
        /// the cmdlet parses the schema doing implicitly check the schema too.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = TestJsonCommand.SchemaStringParameterSet)]
        [ValidateNotNullOrEmpty()]
        public string Schema { get; set; }

        /// <summary>
        /// A path to the file containg schema to validate the JSON against.
        /// This is optional parameter.
        /// If the parameter is absent the cmdlet only attempts to parse the JSON string.
        /// If the parameter present the cmdlet attempts to parse the JSON string and
        /// then validates the JSON against the schema. Before testing the JSON string,
        /// the cmdlet parses the schema doing implicitly check the schema too.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = TestJsonCommand.SchemaPathParameterSet)]
        [ValidateNotNullOrEmpty()]
        public string SchemaPath { get; set; }

        private JsonSchema _jschema;

        /// <summary>
        /// Prepare a JSON schema.
        /// </summary>
        protected override void BeginProcessing()
        {
            string resolvedpath = string.Empty;

            try
            {
                if (Schema != null)
                {
                    try
                    {
                        _jschema = JsonSchema.FromJsonAsync(Schema).Result;
                    }
                    // Even if only one exception is thrown, it is still wrapped in an AggregateException exception
                    // https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/exception-handling-task-parallel-library
                    catch (AggregateException ae)
                    {
                        // Process all exceptions in the AggregateException
                        ae.Handle(i =>
                            {
                                // Unwrap TargetInvocationException if any
                                // Rethrow inner exception without losing the stack trace
                                if (i is TargetInvocationException)
                                {
                                    ExceptionDispatchInfo.Capture(i.InnerException).Throw();
                                }
                                else
                                {
                                    ExceptionDispatchInfo.Capture(i).Throw();
                                }
                                return true;
                            }
                        );
                    }
                }
                else if (SchemaPath != null)
                {
                    try
                    {
                        resolvedpath = Context.SessionState.Path.GetUnresolvedProviderPathFromPSPath(SchemaPath);
                        _jschema = JsonSchema.FromFileAsync(resolvedpath).Result;
                    }
                    catch (AggregateException ae)
                    {
                        ae.Handle(i =>
                            {
                                if (i is TargetInvocationException)
                                {
                                    ExceptionDispatchInfo.Capture(i.InnerException).Throw();
                                }
                                else
                                {
                                    ExceptionDispatchInfo.Capture(i).Throw();
                                }
                                return true;
                            }
                        );
                    }
                }
            }
            // Handle exceptions related to file access to provide more specific error message
            // https://docs.microsoft.com/en-us/dotnet/standard/io/handling-io-errors
            catch (Exception e) when (
                e is IOException ||
                e is UnauthorizedAccessException ||
                e is NotSupportedException ||
                e is SecurityException
            )
            {
                // Do we really need to wrap exception? Not doing this provides more clear error message upfront.
                // E.g.: "'{}'|Test-Json -SchemaPath c:" results in "Test-Json : Access to the path 'C:\' is denied".
                Exception exception = new Exception("JSON schema file open failure", e); // TODO: Add resource string
                ThrowTerminatingError(new ErrorRecord(exception, "JsonSchemaFileOpenFailure", ErrorCategory.OpenError, null));
            }
            catch (Exception e)
            {
                Exception exception = new Exception(TestJsonCmdletStrings.InvalidJsonSchema, e);
                ThrowTerminatingError(new ErrorRecord(exception, "InvalidJsonSchema", ErrorCategory.InvalidData, null));
            }
        }

        /// <summary>
        /// Validate a JSON.
        /// </summary>
        protected override void ProcessRecord()
        {
            JObject parsedJson = null;
            bool result = true;

            try
            {
                parsedJson = JObject.Parse(Json);

                if (_jschema != null)
                {
                    var errorMessages = _jschema.Validate(parsedJson);
                    if (errorMessages != null && errorMessages.Count != 0)
                    {
                        result = false;

                        Exception exception = new Exception(TestJsonCmdletStrings.InvalidJsonAgainstSchema);

                        foreach (var message in errorMessages)
                        {
                            ErrorRecord errorRecord = new ErrorRecord(exception, "InvalidJsonAgainstSchema", ErrorCategory.InvalidData, null);
                            errorRecord.ErrorDetails = new ErrorDetails(message.ToString());
                            WriteError(errorRecord);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                result = false;

                Exception exception = new Exception(TestJsonCmdletStrings.InvalidJson, exc);
                WriteError(new ErrorRecord(exception, "InvalidJson", ErrorCategory.InvalidData, Json));
            }

            WriteObject(result);
        }
    }
}
