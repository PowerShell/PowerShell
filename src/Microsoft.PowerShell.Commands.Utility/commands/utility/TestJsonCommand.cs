using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Test-Json command.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "Json", HelpUri = "")]
    public class TestJsonCommand : PSCmdlet
    {
        /// <summary>
        /// An Json to be validated.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public String Json { get; set; }

        /// <summary>
        /// A schema to validate the Json against.
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty()]
        public String Schema { get; set; }

        private JSchema _jschema = null;

        /// <summary>
        /// Prepare an Json schema.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (Schema != null)
            {
                try
                {
                    _jschema = JSchema.Parse(Schema);
                }
                catch (Exception exc)
                {
                    Exception exception = new Exception(TestJsonCmdletStrings.InvalidJsonSchema, exc);
                    ThrowTerminatingError(new ErrorRecord(exception, "InvalidJsonSchema", ErrorCategory.InvalidData, null));
                }
            }
        }

        /// <summary>
        /// Validate an Json.
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
                    if (!parsedJson.IsValid(_jschema, out IList<string> errorMessages))
                    {
                        result = false;

                        Exception exception = new Exception(TestJsonCmdletStrings.InvalidJsonAgainistSchema);
                        if (errorMessages != null)
                        {
                            foreach (var message in errorMessages)
                            {
                                ErrorRecord errorRecord = new ErrorRecord(exception, "InvalidJsonAgainistSchema", ErrorCategory.InvalidData, null);
                                errorRecord.ErrorDetails = new ErrorDetails(message);
                                WriteError(errorRecord);
                            }
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
