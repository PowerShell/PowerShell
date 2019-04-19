// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Text;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    internal static class InputFileOpenModeConversion
    {
        internal static FileMode Convert(OpenMode openMode)
        {
            return SessionStateUtilities.GetFileModeFromOpenMode(openMode);
        }
    }

    /// <summary>
    /// Implementation for the out-file command.
    /// </summary>
    [Cmdlet(VerbsData.Out, "File", SupportsShouldProcess = true, DefaultParameterSetName = "ByPath", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113363")]
    public class OutFileCommand : FrontEndCommandBase
    {
        /// <summary>
        /// Set inner command.
        /// </summary>
        public OutFileCommand()
        {
            this.implementation = new OutputManagerInner();
        }

        #region Command Line Parameters

        /// <summary>
        /// Mandatory file name to write to.
        /// </summary>
        [Alias("Path")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByPath")]
        public string FilePath
        {
            get { return _fileName; }

            set { _fileName = value; }
        }

        private string _fileName;

        /// <summary>
        /// Mandatory file name to write to.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByLiteralPath")]
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
                _isLiteralPath = true;
            }
        }

        private bool _isLiteralPath = false;

        /// <summary>
        /// Encoding optional flag.
        /// </summary>
        [Parameter(Position = 1)]
        [ArgumentToEncodingTransformationAttribute()]
        [ArgumentEncodingCompletionsAttribute]
        [ValidateNotNullOrEmpty]
        public Encoding Encoding { get; set; } = ClrFacade.GetDefaultEncoding();

        /// <summary>
        /// Property that sets append parameter.
        /// </summary>
        [Parameter()]
        public SwitchParameter Append
        {
            get { return _append; }

            set { _append = value; }
        }

        private bool _append;

        /// <summary>
        /// Property that sets force parameter.
        /// </summary>
        [Parameter()]
        public SwitchParameter Force
        {
            get { return _force; }

            set { _force = value; }
        }

        private bool _force;

        /// <summary>
        /// Property that prevents file overwrite.
        /// </summary>
        [Parameter()]
        [Alias("NoOverwrite")]
        public SwitchParameter NoClobber
        {
            get { return _noclobber; }

            set { _noclobber = value; }
        }

        private bool _noclobber;

        /// <summary>
        /// Optional, number of columns to use when writing to device.
        /// </summary>
        [ValidateRangeAttribute(2, int.MaxValue)]
        [Parameter]
        public int Width
        {
            get { return (_width != null) ? _width.Value : 0; }

            set { _width = value; }
        }

        private int? _width = null;

        /// <summary>
        /// False to add a newline to the end of the output string, true if not.
        /// </summary>
        [Parameter]
        public SwitchParameter NoNewline
        {
            get
            {
                return _suppressNewline;
            }

            set
            {
                _suppressNewline = value;
            }
        }

        private bool _suppressNewline = false;

        #endregion

        /// <summary>
        /// Read command line parameters.
        /// </summary>
        protected override void BeginProcessing()
        {
            // set up the Scree Host interface
            OutputManagerInner outInner = (OutputManagerInner)this.implementation;

            // NOTICE: if any exception is thrown from here to the end of the method, the
            // cleanup code will be called in IDisposable.Dispose()
            outInner.LineOutput = InstantiateLineOutputInterface();

            if (_sw == null)
            {
                return;
            }

            // finally call the base class for general hookup
            base.BeginProcessing();
        }

        /// <summary>
        /// One-time initialization: acquire a screen host interface
        /// by creating one on top of a file.
        /// NOTICE: we assume that at this time the file name is
        /// available in the CRO. JonN recommends: file name has to be
        /// a MANDATORY parameter on the command line.
        /// </summary>
        private LineOutput InstantiateLineOutputInterface()
        {
            string action = StringUtil.Format(FormatAndOut_out_xxx.OutFile_Action);
            if (ShouldProcess(FilePath, action))
            {
                PathUtils.MasterStreamOpen(
                    this,
                    FilePath,
                    Encoding,
                    false, // defaultEncoding
                    Append,
                    Force,
                    NoClobber,
                    out _fs,
                    out _sw,
                    out _readOnlyFileInfo,
                    _isLiteralPath
                    );
            }
            else
                return null;

            // compute the # of columns available
            int computedWidth = int.MaxValue;

            if (_width != null)
            {
                // use the value from the command line
                computedWidth = _width.Value;
            }

            // use the stream writer to create and initialize the Line Output writer
            TextWriterLineOutput twlo = new TextWriterLineOutput(_sw, computedWidth, _suppressNewline);

            // finally have the ILineOutput interface extracted
            return (LineOutput)twlo;
        }

        /// <summary>
        /// Execution entry point.
        /// </summary>
        protected override void ProcessRecord()
        {
            _processRecordExecuted = true;
            if (_sw == null)
            {
                return;
            }

            // NOTICE: if any exception is thrown, the
            // cleanup code will be called in IDisposable.Dispose()
            base.ProcessRecord();
            _sw.Flush();
        }

        /// <summary>
        /// Execution entry point.
        /// </summary>
        protected override void EndProcessing()
        {
            // When the Out-File is used in a redirection pipelineProcessor,
            // its ProcessRecord method may not be called when nothing is written to the
            // output pipe, for example:
            //     Write-Error error > test.txt
            // In this case, the EndProcess method should return immediately as if it's
            // never been called. The cleanup work will be done in IDisposable.Dispose()
            if (!_processRecordExecuted)
            {
                return;
            }

            if (_sw == null)
            {
                return;
            }

            // NOTICE: if any exception is thrown, the
            // cleanup code will be called in IDisposable.Dispose()
            base.EndProcessing();

            _sw.Flush();

            CleanUp();
        }

        /// <summary>
        /// InternalDispose.
        /// </summary>
        protected override void InternalDispose()
        {
            base.InternalDispose();
            CleanUp();
        }

        private void CleanUp()
        {
            if (_fs != null)
            {
                _fs.Dispose();
                _fs = null;
            }

            // reset the read-only attribute
            if (_readOnlyFileInfo != null)
            {
                _readOnlyFileInfo.Attributes |= FileAttributes.ReadOnly;
                _readOnlyFileInfo = null;
            }
        }

        /// <summary>
        /// Handle to file stream.
        /// </summary>
        private FileStream _fs;

        /// <summary>
        /// Stream writer used to write to file.
        /// </summary>
        private StreamWriter _sw = null;

        /// <summary>
        /// Indicate whether the ProcessRecord method was executed.
        /// When the Out-File is used in a redirection pipelineProcessor,
        /// its ProcessRecord method may not be called when nothing is written to the
        /// output pipe, for example:
        ///     Write-Error error > test.txt
        /// In this case, the EndProcess method should return immediately as if it's
        /// never been called.
        /// </summary>
        private bool _processRecordExecuted = false;

        /// <summary>
        /// FileInfo of file to clear read-only flag when operation is complete.
        /// </summary>
        private FileInfo _readOnlyFileInfo = null;
    }
}
