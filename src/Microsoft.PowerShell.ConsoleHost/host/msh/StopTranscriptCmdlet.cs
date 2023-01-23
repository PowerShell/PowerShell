// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implements the stop-transcript cmdlet.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "Transcript", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.None, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096798")]
    [OutputType(typeof(string))]
    public sealed class StopTranscriptCommand : PSCmdlet
    {
        /// <summary>
        /// Stops the transcription.
        /// </summary>
        protected override
        void
        BeginProcessing()
        {
            if (!ShouldProcess(string.Empty))
            {
                return;
            }
            
            try
            {
                string outFilename = Host.UI.StopTranscribing();
                if (outFilename != null)
                {
                    PSObject outputObject = new PSObject(
                        StringUtil.Format(TranscriptStrings.TranscriptionStopped, outFilename));
                    outputObject.Properties.Add(new PSNoteProperty("Path", outFilename));
                    WriteObject(outputObject);
                }
            }
            catch (Exception e)
            {
                throw PSTraceSource.NewInvalidOperationException(
                        e, TranscriptStrings.ErrorStoppingTranscript, e.Message);
            }
        }
    }
}
