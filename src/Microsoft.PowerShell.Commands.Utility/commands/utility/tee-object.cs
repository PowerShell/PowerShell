/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Management.Automation;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Class for Tee-object implementation
    /// </summary>
    [Cmdlet("Tee", "Object", DefaultParameterSetName = "File", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113417")]
    public sealed class TeeObjectCommand : PSCmdlet, IDisposable
    {
        /// <summary>
        /// object to process
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject
        {
            get { return inputObject; }
            set { inputObject = value; }
        }
        private PSObject inputObject;
	
        /// <summary>
        /// FilePath parameter
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "File")]
        public string FilePath
        {
            get{ return fileName;}
            set{ fileName = value;}
        }
        private string fileName;

        /// <summary>
        /// Literal FilePath parameter
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LiteralFile")]
        [Alias("PSPath")]
        public string LiteralPath
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
            }       
        }

        /// <summary>
        /// Append switch
        /// </summary>
        [Parameter(ParameterSetName = "File")]
        public SwitchParameter Append
        {
            get{ return append;}
            set{ append = value;}
        }
        private bool append;

        /// <summary>
        /// Variable parameter
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Variable")]
        public string Variable
        {
            get { return variable; }
            set { variable = value; }
        }
        private string variable;

        /// <summary>
        /// 
        /// </summary>
        protected override void BeginProcessing()
        {
            commandWrapper = new CommandWrapper();
            if (String.Equals(ParameterSetName, "File", StringComparison.OrdinalIgnoreCase))
            {
                commandWrapper.Initialize(Context, "out-file", typeof(OutFileCommand));
                commandWrapper.AddNamedParameter("filepath", fileName);
                commandWrapper.AddNamedParameter("append", append);
            }
            else if (String.Equals(ParameterSetName, "LiteralFile", StringComparison.OrdinalIgnoreCase))
            {
                commandWrapper.Initialize(Context, "out-file", typeof(OutFileCommand));
                commandWrapper.AddNamedParameter("LiteralPath", fileName);
                commandWrapper.AddNamedParameter("append", append);
            }
            else
            {
                // variable parameter set
                commandWrapper.Initialize(Context, "set-variable", typeof(SetVariableCommand));
                commandWrapper.AddNamedParameter("name", variable);
                // Can't use set-var's passthru because it writes the var object to the pipeline, we want just
                // the values to be written
            }
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void ProcessRecord()
        {
            commandWrapper.Process(inputObject);
            WriteObject(inputObject);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void EndProcessing()
        {
            commandWrapper.ShutDown();
        }

        private void Dispose(bool isDisposing)
        {
            if (!alreadyDisposed)
            {
                alreadyDisposed = true; 
                if (isDisposing && commandWrapper != null)
                {
                    commandWrapper.Dispose();
                    commandWrapper = null;

                }
            }
        }

        /// <summary>
        /// Dispose method in IDisposeable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~TeeObjectCommand()
        {
            Dispose(false);
        }
        #region private
        private CommandWrapper commandWrapper;
        private bool alreadyDisposed;
        #endregion private
    }
}

