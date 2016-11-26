
/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "New-TemporaryFile" cmdlet
    /// </summary>    
    [Cmdlet(VerbsCommon.New, "TemporaryFile", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=526726")]
    [OutputType(typeof(System.IO.FileInfo))]
    public class NewTemporaryFileCommand : Cmdlet
    {      
        /// <summary>
        /// returns a TemporaryFile
        /// </summary>
        protected override void BeginProcessing()
        {
            string filePath = null;
            if (ShouldProcess(System.Environment.GetEnvironmentVariable("TEMP")))
            {
                try
                {
                    filePath = Path.GetTempFileName();                
                }
                catch (Exception e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(
                        e,
                        "NewTemporaryFileWriteError",
                        ErrorCategory.WriteError,
                        System.Environment.GetEnvironmentVariable("TEMP")
                    );
                    WriteError(errorRecord);
                    return;
                }
            FileInfo file = new FileInfo(filePath);
            WriteObject(file);
            }
        }
    }
}