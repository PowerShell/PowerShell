// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Text;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Class for Tee-object implementation.
    /// </summary>
    [Cmdlet("Tee", "Object", DefaultParameterSetName = "File", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097034")]
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
        /// Gets or sets the Encoding.
        /// </summary>
        [Parameter(ParameterSetName = "File")]
        [Parameter(ParameterSetName = "LiteralFile")]
        [ArgumentToEncodingTransformationAttribute]
        [ArgumentEncodingCompletionsAttribute]
        [ValidateNotNullOrEmpty]
        public Encoding Encoding { get; set; } = Encoding.Default;

        /// <summary>
        /// Variable parameter.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Variable")]
        public string Variable
        {
            get { return _variable; }

            set { _variable = value; }
        }

        /// <summary>
        /// Gets or Sets the name of the variable used to store ErrorRecords.
        /// <summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("ERV")]
        public string ErrorRecordVariable
        {
            get { return this.errorRecordVariable; }

            set { this.errorRecordVariable = value; }
        }

        private string _variable;
        private string errorRecordVariable;

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
                _commandWrapper.AddNamedParameter("encoding", Encoding);
            }
            else if (string.Equals(ParameterSetName, "LiteralFile", StringComparison.OrdinalIgnoreCase))
            {
                _commandWrapper.Initialize(Context, "out-file", typeof(OutFileCommand));
                _commandWrapper.AddNamedParameter("LiteralPath", _fileName);
                _commandWrapper.AddNamedParameter("append", _append);
                _commandWrapper.AddNamedParameter("encoding", Encoding);
            }
            else
            {
                // variable parameter set
                _commandWrapper.Initialize(Context, "set-variable", typeof(SetVariableCommand));
                _commandWrapper.AddNamedParameter("name", _variable);
                // Can't use set-var's passthru because it writes the var object to the pipeline, we want just
                // the values to be written
            }

            if (!string.IsNullOrEmpty(this.errorRecordVariable))
            {
                this.errorCommandWrapper = new CommandWrapper();
                this.errorCommandWrapper.Initialize(Context, "set-variable", typeof(SetVariableCommand));
                this.errorCommandWrapper.AddNamedParameter("name", this.errorRecordVariable);
            }
        }

        /// <summary>
        /// </summary>
        protected override void ProcessRecord()
        {
            if (this.errorCommandWrapper is not null)
            {
                if (this._inputObject.BaseObject is ErrorRecord er)
                {
                    this.errorCommandWrapper.Process(_inputObject);
                }
            }

            _commandWrapper.Process(_inputObject);
            WriteObject(_inputObject);
        }

        /// <summary>
        /// </summary>
        protected override void EndProcessing()
        {
            // _commandWrapper is always created, but errorCommandWrapper may not be.
            _commandWrapper.ShutDown();
            if (this.errorCommandWrapper is not null)
            {
                errorCommandWrapper.ShutDown();
            }
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

                if (isDisposing && this.errorCommandWrapper is not null)
                {
                    this.errorCommandWrapper.Dispose();
                    errorCommandWrapper = null;
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

        #region private
        private CommandWrapper _commandWrapper;
        private CommandWrapper errorCommandWrapper;
        private bool _alreadyDisposed;
        #endregion private
    }
}
