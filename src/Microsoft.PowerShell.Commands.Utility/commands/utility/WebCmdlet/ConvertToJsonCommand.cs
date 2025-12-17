// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Threading;

using Newtonsoft.Json;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The ConvertTo-Json command.
    /// This command converts an object to a Json string representation.
    /// </summary>
    [Cmdlet(VerbsData.ConvertTo, "Json", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096925", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(string))]
    public class ConvertToJsonCommand : PSCmdlet, IDisposable
    {
        private const int DefaultDepth = 2;
        private const int DefaultDepthV2 = 64;
        private const int DepthAllowed = 100;
        private const int DepthAllowedV2 = 1000;

        /// <summary>
        /// Gets or sets the InputObject property.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowNull]
        public object InputObject { get; set; }

        private int? _depth;

        private readonly CancellationTokenSource _cancellationSource = new();

        /// <summary>
        /// Gets or sets the Depth property.
        /// When PSJsonSerializerV2 is enabled: default is 64, max is 1000.
        /// Otherwise: default is 2, max is 100.
        /// </summary>
        [Parameter]
        [ValidateRange(0, DepthAllowedV2)]
        public int Depth
        {
            get
            {
                if (_depth.HasValue)
                {
                    return _depth.Value;
                }

                return ExperimentalFeature.IsEnabled(ExperimentalFeature.PSJsonSerializerV2)
                    ? DefaultDepthV2
                    : DefaultDepth;
            }

            set
            {
                _depth = value;
            }
        }

        /// <summary>
        /// Gets or sets the Compress property.
        /// If the Compress property is set to be true, the Json string will
        /// be output in the compressed way. Otherwise, the Json string will
        /// be output with indentations.
        /// </summary>
        [Parameter]
        public SwitchParameter Compress { get; set; }

        /// <summary>
        /// Gets or sets the EnumsAsStrings property.
        /// If the EnumsAsStrings property is set to true, enum values will
        /// be converted to their string equivalent. Otherwise, enum values
        /// will be converted to their numeric equivalent.
        /// </summary>
        [Parameter]
        public SwitchParameter EnumsAsStrings { get; set; }

        /// <summary>
        /// Gets or sets the AsArray property.
        /// If the AsArray property is set to be true, the result JSON string will
        /// be returned with surrounding '[', ']' chars. Otherwise,
        /// the array symbols will occur only if there is more than one input object.
        /// </summary>
        [Parameter]
        public SwitchParameter AsArray { get; set; }

        /// <summary>
        /// Specifies how strings are escaped when writing JSON text.
        /// If the EscapeHandling property is set to EscapeHtml, the result JSON string will
        /// be returned with HTML (&lt;, &gt;, &amp;, ', ") and control characters (e.g. newline) are escaped.
        /// </summary>
        [Parameter]
        public StringEscapeHandling EscapeHandling { get; set; } = StringEscapeHandling.Default;

        /// <summary>
        /// IDisposable implementation, dispose of any disposable resources created by the cmdlet.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implementation of IDisposable for both manual Dispose() and finalizer-called disposal of resources.
        /// </summary>
        /// <param name="disposing">
        /// Specified as true when Dispose() was called, false if this is called from the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationSource.Dispose();
            }
        }

        private readonly List<object> _inputObjects = new();

        /// <summary>
        /// Caching the input objects for the command.
        /// </summary>
        protected override void ProcessRecord()
        {
            _inputObjects.Add(InputObject);
        }

        /// <summary>
        /// Validate parameters and prepare for processing.
        /// </summary>
        protected override void BeginProcessing()
        {
            // When PSJsonSerializerV2 is not enabled, enforce the legacy max depth limit
            if (!ExperimentalFeature.IsEnabled(ExperimentalFeature.PSJsonSerializerV2))
            {
                if (_depth.HasValue && _depth.Value > DepthAllowed)
                {
                    var errorRecord = new ErrorRecord(
                        new ArgumentException(
                            string.Format(
                                System.Globalization.CultureInfo.CurrentCulture,
                                WebCmdletStrings.JsonDepthExceedsLimit,
                                _depth.Value,
                                DepthAllowed)),
                        "DepthExceedsLimit",
                        ErrorCategory.InvalidArgument,
                        _depth.Value);
                    ThrowTerminatingError(errorRecord);
                }
            }
        }

        /// <summary>
        /// Do the conversion to json and write output.
        /// </summary>
        protected override void EndProcessing()
        {
            if (_inputObjects.Count > 0)
            {
                object objectToProcess = (_inputObjects.Count > 1 || AsArray) ? (_inputObjects.ToArray() as object) : _inputObjects[0];

                var context = new JsonObject.ConvertToJsonContext(
                    Depth,
                    EnumsAsStrings.IsPresent,
                    Compress.IsPresent,
                    EscapeHandling,
                    targetCmdlet: this,
                    _cancellationSource.Token);

                // null is returned only if the pipeline is stopping (e.g. ctrl+c is signaled).
                // in that case, we shouldn't write the null to the output pipe.
                string output = JsonObject.ConvertToJson(objectToProcess, in context);
                if (output != null)
                {
                    WriteObject(output);
                }
            }
        }

        /// <summary>
        /// Process the Ctrl+C signal.
        /// </summary>
        protected override void StopProcessing()
        {
            _cancellationSource.Cancel();
        }
    }
}
