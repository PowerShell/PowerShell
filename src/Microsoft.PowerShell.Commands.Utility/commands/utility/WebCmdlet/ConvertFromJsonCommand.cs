// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The ConvertFrom-Json command.
    /// This command converts a Json string representation to a JsonObject.
    /// </summary>
    [Cmdlet(VerbsData.ConvertFrom, "Json", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096606", RemotingCapability = RemotingCapability.None)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public class ConvertFromJsonCommand : Cmdlet
    {
        #region parameters

        /// <summary>
        /// Gets or sets the InputString property.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [AllowEmptyString]
        public string InputObject { get; set; }

        /// <summary>
        /// InputObjectBuffer buffers all InputObject contents available in the pipeline.
        /// </summary>
        private readonly List<string> _inputObjectBuffer = new();

        /// <summary>
        /// Returned data structure is a Hashtable instead a CustomPSObject.
        /// </summary>
        [Parameter]
        public SwitchParameter AsHashtable { get; set; }

        /// <summary>
        /// Gets or sets the maximum depth the JSON input is allowed to have. By default, it is 1024.
        /// </summary>
        [Parameter]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Depth { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the switch to prevent ConvertFrom-Json from unravelling collections during deserialization, instead passing them as a single
        /// object through the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter NoEnumerate { get; set; }

        /// <summary>
        /// Gets or sets the switch to control how DateTime values are to be parsed as a dotnet object.
        /// </summary>
        [Parameter]
        public JsonDateKind DateKind { get; set; } = JsonDateKind.Default;

        #endregion parameters

        #region overrides

        /// <summary>
        /// Buffers InputObjet contents available in the pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            _inputObjectBuffer.Add(InputObject);
        }

        /// <summary>
        /// The main execution method for the ConvertFrom-Json command.
        /// </summary>
        protected override void EndProcessing()
        {
            // When Input is provided through pipeline, the input can be represented in the following two ways:
            // 1. Each input in the collection is a complete Json content. There can be multiple inputs of this format.
            // 2. The complete input is a collection which represents a single Json content. This is typically the majority of the case.
            if (_inputObjectBuffer.Count > 0)
            {
                if (_inputObjectBuffer.Count == 1)
                {
                    ConvertFromJsonHelper(_inputObjectBuffer[0]);
                }
                else
                {
                    bool successfullyConverted = false;
                    try
                    {
                        // Try to deserialize the first element.
                        successfullyConverted = ConvertFromJsonHelper(_inputObjectBuffer[0]);
                    }
                    catch (ArgumentException)
                    {
                        // The first input string does not represent a complete Json Syntax.
                        // Hence consider the entire input as a single Json content.
                    }

                    if (successfullyConverted)
                    {
                        for (int index = 1; index < _inputObjectBuffer.Count; index++)
                        {
                            ConvertFromJsonHelper(_inputObjectBuffer[index]);
                        }
                    }
                    else
                    {
                        // Process the entire input as a single Json content.
                        ConvertFromJsonHelper(string.Join(System.Environment.NewLine, _inputObjectBuffer.ToArray()));
                    }
                }
            }
        }

        /// <summary>
        /// ConvertFromJsonHelper is a helper method to convert to Json input to .Net Type.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>True if successfully converted, else returns false.</returns>
        private bool ConvertFromJsonHelper(string input)
        {
            ErrorRecord error = null;
            object result = JsonObject.ConvertFromJson(input, AsHashtable.IsPresent, Depth, DateKind, out error);

            if (error != null)
            {
                ThrowTerminatingError(error);
            }

            WriteObject(result, !NoEnumerate.IsPresent);
            return (result != null);
        }

        #endregion overrides
    }
}
