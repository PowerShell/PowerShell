// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Class for Tee-object implementation.
    /// </summary>
    [Cmdlet("Tee", "Object", DefaultParameterSetName = "File", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113417")]
    public sealed class TeeObjectCommand : PSCmdlet, IDisposable
    {
        /// <summary>
        /// Object to process.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject
        {
            get { return _inputObject; }

            set { _inputObject = value; }
        }

        private PSObject _inputObject;

        /// <summary>
        /// FilePath parameter.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "File")]
        [Alias("Path")]
        public string FilePath
        {
            get { return _fileName; }

            set { _fileName = value; }
        }

        private string _fileName;

        /// <summary>
        /// Literal FilePath parameter.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LiteralFile")]
        [Alias("PSPath", "LP")]
        public string LiteralPath
        {
            get
            {
                return _fileName;
            }

            set
            {
                _fileName = value;
            }
        }

        /// <summary>
        /// Append switch.
        /// </summary>
        [Parameter(ParameterSetName = "File")]
        public SwitchParameter Append
        {
            get { return _append; }

            set { _append = value; }
        }

        private bool _append;

        /// <summary>
        /// Variable parameter.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Variable")]
        public string Variable
        {
            get { return _variable; }

            set { _variable = value; }
        }

        private string _variable;

        /// <summary>
        /// </summary>
        protected override void BeginProcessing()
        {
            _commandWrapper = new CommandWrapper();
            if (string.Equals(ParameterSetName, "File", StringComparison.OrdinalIgnoreCase))
            {
                _commandWrapper.Initialize(Context, "out-file", typeof(OutFileCommand));
                _commandWrapper.AddNamedParameter("filepath", _fileName);
                _commandWrapper.AddNamedParameter("append", _append);
            }
            else if (string.Equals(ParameterSetName, "LiteralFile", StringComparison.OrdinalIgnoreCase))
            {
                _commandWrapper.Initialize(Context, "out-file", typeof(OutFileCommand));
                _commandWrapper.AddNamedParameter("LiteralPath", _fileName);
                _commandWrapper.AddNamedParameter("append", _append);
            }
            else
            {
                // variable parameter set
                _commandWrapper.Initialize(Context, "set-variable", typeof(SetVariableCommand));
                _commandWrapper.AddNamedParameter("name", _variable);
                // Can't use set-var's passthru because it writes the var object to the pipeline, we want just
                // the values to be written
            }
        }
        /// <summary>
        /// </summary>
        protected override void ProcessRecord()
        {
            _commandWrapper.Process(_inputObject);
            WriteObject(_inputObject);
        }

        /// <summary>
        /// </summary>
        protected override void EndProcessing()
        {
            _commandWrapper.ShutDown();
        }

        private void Dispose(bool isDisposing)
        {
            if (!_alreadyDisposed)
            {
                _alreadyDisposed = true;
                if (isDisposing && _commandWrapper != null)
                {
                    _commandWrapper.Dispose();
                    _commandWrapper = null;
                }
            }
        }

        /// <summary>
        /// Dispose method in IDisposable.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~TeeObjectCommand()
        {
            Dispose(false);
        }
        #region private
        private CommandWrapper _commandWrapper;
        private bool _alreadyDisposed;
        #endregion private
    }
}
