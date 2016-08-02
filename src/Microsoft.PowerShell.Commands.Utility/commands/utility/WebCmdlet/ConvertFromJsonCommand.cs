/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Reflection;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The ConvertFrom-Json command
    /// This command convert a Json string representation to a JsonObject
    /// </summary>
    [Cmdlet(VerbsData.ConvertFrom, "Json", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=217031", RemotingCapability = RemotingCapability.None)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public class ConvertFromJsonCommand : Cmdlet
    {
        #region parameters

        /// <summary>
        /// gets or sets the InputString property
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [AllowEmptyString]
        public string InputObject { get; set; }

        /// <summary>
        /// inputObjectBuffer buffers all InputObjet contents avaliable in the pipeline.
        /// </summary>
        private List<string> _inputObjectBuffer = new List<string>();

        #endregion parameters

        #region overrides

        /// <summary>
        /// Prerequisite checks
        /// </summary>
        protected override void BeginProcessing()
        {
#if CORECLR
            JsonObject.ImportJsonDotNetModule(this);
#else
            try
            {
                System.Reflection.Assembly.Load(new AssemblyName("System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"));
            }
            catch (System.IO.FileNotFoundException)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new NotSupportedException(WebCmdletStrings.ExtendedProfileRequired),
                    "ExtendedProfileRequired",
                    ErrorCategory.NotInstalled,
                    null));
            }
#endif
        }

        /// <summary>
        ///  Buffers InputObjet contents avaliable in the pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            _inputObjectBuffer.Add(InputObject);
        }

        /// <summary>
        /// the main execution method for the convertfrom-json command
        /// </summary>
        protected override void EndProcessing()
        {
            // When Input is provided through pipeline, the input can be represented in the following two ways:
            // 1. Each input to the buffer is a complete Json content. There can be multiple inputs of this format. 
            // 2. The complete buffer input collectively represent a single JSon format. This is typically the majority of the case.
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
                        successfullyConverted = ConvertFromJsonHelper(_inputObjectBuffer[0]);
                    }
                    catch (ArgumentException)
                    {
                        // The first input string does not represent a complete Json Syntax. 
                        // Hence consider the the entire input as a single Json content.
                    }
#if CORECLR
                    catch (Newtonsoft.Json.JsonSerializationException)
                    {
                        // we use another serializer for CORECLR implementation
                    }
#endif
                    if (successfullyConverted)
                    {
                        for (int index = 1; index < _inputObjectBuffer.Count; index++)
                        {
                            ConvertFromJsonHelper(_inputObjectBuffer[index]);
                        }
                    }
                    else
                    {
                        ConvertFromJsonHelper(string.Join(System.Environment.NewLine, _inputObjectBuffer.ToArray()));
                    }
                }
            }
        }

        /// <summary>
        /// ConvertFromJsonHelper is a helper method to convert to Json input to .Net Type.
        /// </summary>
        /// <param name="input">Input String.</param>
        /// <returns>True if successfully converted, else returns false.</returns>
        private bool ConvertFromJsonHelper(string input)
        {
            ErrorRecord error = null;
            object result = JsonObject.ConvertFromJson(input, out error);

            if (error != null)
            {
                ThrowTerminatingError(error);
            }

            WriteObject(result);
            return (result != null);
        }

        #endregion overrides
    }
}
