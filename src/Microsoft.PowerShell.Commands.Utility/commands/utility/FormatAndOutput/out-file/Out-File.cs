/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Host;
using System.IO;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{ 
    internal static class InputFileOpenModeConversion
    {
       internal static FileMode Convert (OpenMode openMode)
        {
            return SessionStateUtilities.GetFileModeFromOpenMode(openMode);
        }
    }

    /// <summary>
    /// implementation for the out-file command
    /// </summary>
    [Cmdlet("Out", "File", SupportsShouldProcess = true, DefaultParameterSetName = "ByPath", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113363")]
    public class OutFileCommand : FrontEndCommandBase
    {
        /// <summary>
        /// set inner command
        /// </summary>
        public OutFileCommand ()
        {
            this.implementation = new OutputManagerInner ();
        }

        #region Command Line Parameters

        /// <summary>
        /// mandatory file name to write to
        /// </summary>
        [Parameter(Mandatory=true, Position=0, ParameterSetName = "ByPath")]
        public string FilePath
        {
            get{ return fileName;}
            set{ fileName = value;}
        }

        private string fileName;

        /// <summary>
        /// mandatory file name to write to
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByLiteralPath")]
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
                isLiteralPath = true;
            }
        }
        bool isLiteralPath = false;

        /// <summary>
        /// Encoding optional flag
        /// </summary>
        /// 
        [Parameter(Position=1)]
        [ValidateNotNullOrEmpty]
        [ValidateSetAttribute(new string[] {
            EncodingConversion.Unknown,
            EncodingConversion.String,
            EncodingConversion.Unicode,
            EncodingConversion.BigEndianUnicode,
            EncodingConversion.Utf8,
            EncodingConversion.Utf7,
            EncodingConversion.Utf32,
            EncodingConversion.Ascii,
            EncodingConversion.Default,
            EncodingConversion.OEM })]
        public string Encoding
        {
            get{ return encoding;}
            set{ encoding = value;}
        }

        private string encoding;        

        /// <summary>
        /// Property that sets append parameter.
        /// </summary>
        [Parameter()]
        public SwitchParameter Append
        {
            get { return append; }
            set { append = value; }
        }
        private bool append;

        /// <summary>
        /// Property that sets force parameter.
        /// </summary>
        [Parameter()]
        public SwitchParameter Force
        {
            get { return force; }
            set { force = value; }
        }
        private bool force;

        /// <summary>
        /// Property that prevents file overwrite.
        /// </summary>
        [Parameter()]
        [Alias("NoOverwrite")]
        public SwitchParameter NoClobber
        {
            get { return noclobber; }
            set { noclobber = value; }
        }
        private bool noclobber;

        /// <summary>
        /// optional, number of columns to use when writing to device
        /// </summary>
        [ValidateRangeAttribute(2,int.MaxValue)]
        [Parameter]
        public int Width
        {
            get { return (width != null) ? width.Value : 0; }
            set { width = value; }
        }

        private Nullable<int> width = null;

        /// <summary>
        /// False to add a newline to the end of the output string, true if not.
        /// </summary>
        [Parameter]
        public SwitchParameter NoNewline
        {
            get
            {
                return suppressNewline;
            }
            set
            {
                suppressNewline = value;
            }
        }

        private bool suppressNewline = false;

        #endregion

        /// <summary>
        /// read command line parameters
        /// </summary>
        protected override void BeginProcessing()
        {
            // set up the Scree Host interface
            OutputManagerInner outInner = (OutputManagerInner)this.implementation;

            // NOTICE: if any exception is thrown from here to the end of the method, the 
            // cleanup code will be called in IDisposable.Dispose()
            outInner.LineOutput = InstantiateLineOutputInterface ();

            if (null == sw)
            {
                return;
            }

            // finally call the base class for general hookup
            base.BeginProcessing ();
        }

        
        /// <summary>
        /// one time initialization: acquire a screen host interface
        /// by creating one on top of a file
        /// NOTICE: we assume that at this time the file name is
        /// available in the CRO. JonN recommends: file name has to be
        /// a MANDATORY parameter on the command line
        /// </summary>
        private LineOutput InstantiateLineOutputInterface ()
        {
            string action = StringUtil.Format(FormatAndOut_out_xxx.OutFile_Action);
            if(ShouldProcess(FilePath, action))
            {
                PathUtils.MasterStreamOpen(
                    this,
                    FilePath,
                    encoding,
                    false, // defaultEncoding
                    Append,
                    Force,
                    NoClobber,
                    out fs,
                    out sw,
                    out readOnlyFileInfo,
                    isLiteralPath
                    );
            }
            else
                return null;

            // compute the # of columns available
            int computedWidth = 120;

            if (this.width != null)
            {
                // use the value from the command line
                computedWidth = this.width.Value;
            }
            else
            {
                // use the value we get from the console
                try
                {
                    // NOTE: we subtract 1 because we want to properly handle
                    // the following scenario:
                    // MSH>get-foo|out-file foo.txt
                    // MSH>get-content foo.txt
                    // in this case, if the computed width is (say) 80, get-content
                    // would cause a wrapping of the 80 column long raw strings.
                    // Hence we set the width to 79.
                    computedWidth = this.Host.UI.RawUI.BufferSize.Width - 1;
                }
                catch (HostException)
                {
                    // non interactive host
                }
            }

            // use the stream writer to create and initialize the Line Output writer
            TextWriterLineOutput twlo = new TextWriterLineOutput (sw, computedWidth, suppressNewline);

            // finally have the ILineOutput interface extracted
            return (LineOutput)twlo;

        }

        /// <summary>
        /// execution entry point
        /// </summary>
        protected override void ProcessRecord()
        {
            processRecordExecuted = true;
            if (null == sw)
            {
                return;
            }

            // NOTICE: if any exception is thrown, the 
            // cleanup code will be called in IDisposable.Dispose()
            base.ProcessRecord ();
            sw.Flush ();
        }

        /// <summary>
        /// execution entry point
        /// </summary>
        protected override void EndProcessing()
        {
            // When the Out-File is used in a redirection pipelineProcessor,
            // its ProcessRecord method may not be called when nothing is written to the 
            // output pipe, for example:
            //     Write-Error error > test.txt
            // In this case, the EndProcess method should return immediately as if it's 
            // never been called. The cleanup work will be done in IDisposable.Dispose()
            if (!processRecordExecuted)
            {
                return;
            }

            if (null == sw)
            {
                return;
            }

            // NOTICE: if any exception is thrown, the 
            // cleanup code will be called in IDisposable.Dispose()
            base.EndProcessing ();
            
            sw.Flush ();

            CleanUp ();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void InternalDispose()
        {
            base.InternalDispose ();
            CleanUp ();
            
        }

        private void CleanUp ()
        {
            if (this.fs != null)
            {
                this.fs.Dispose();
                this.fs = null;
            }

            // reset the read-only attribute
            if (null != readOnlyFileInfo)
            {
                readOnlyFileInfo.Attributes |= FileAttributes.ReadOnly;
                this.readOnlyFileInfo = null;
            }
        }

        /// <summary>
        /// handle to file stream
        /// </summary>
        FileStream fs;

        /// <summary>
        /// stream writer used to write to file
        /// </summary>
        private StreamWriter sw = null;

        /// <summary>
        /// indicate whether the ProcessRecord method was executed.
        /// When the Out-File is used in a redirection pipelineProcessor,
        /// its ProcessRecord method may not be called when nothing is written to the 
        /// output pipe, for example:
        ///     Write-Error error > test.txt
        /// In this case, the EndProcess method should return immediately as if it's 
        /// never been called.
        /// </summary>
        private bool processRecordExecuted = false;

        /// <summary>
        /// FileInfo of file to clear read-only flag when operation is complete
        /// </summary>
        private FileInfo readOnlyFileInfo = null;
    }
}

