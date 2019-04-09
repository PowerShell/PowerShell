// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Test-Json command.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "Json", HelpUri = "")]
    public class TestJsonCommand : PSCmdlet
    {
        /// <summary>
        /// An JSON to be validated.
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
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty()]
        public string Schema { get; set; }

        private JsonSchema4 _jschema;

        /// <summary>
        /// Prepare an JSON schema.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (Schema != null)
            {
                try
                {
                    _jschema = JsonSchema4.FromJsonAsync(Schema).Result;
                }
                catch (Exception exc)
                {
                    Exception exception = new Exception(TestJsonCmdletStrings.InvalidJsonSchema, exc);
                    ThrowTerminatingError(new ErrorRecord(exception, "InvalidJsonSchema", ErrorCategory.InvalidData, null));
                }
            }
        }

        /// <summary>
        /// Validate an JSON.
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
