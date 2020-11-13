// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implements the start-transcript cmdlet.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "Transcript", SupportsShouldProcess = true, DefaultParameterSetName = "ByPath", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096485")]
    [OutputType(typeof(string))]
    public sealed class StartTranscriptCommand : PSCmdlet
    {
        /// <summary>
        /// The name of the file in which to write the transcript. If not provided, the file indicated by the variable
        /// $TRANSCRIPT is used.  If neither the filename is supplied or $TRANSCRIPT is not set, the filename shall be $HOME/My
        /// Documents/PowerShell_transcript.YYYYMMDDmmss.txt.
        /// </summary>
        /// <value></value>
        [Parameter(Position = 0, ParameterSetName = "ByPath")]
        [ValidateNotNullOrEmpty]
        public string Path
        {
            get
            {
                return _outFilename;
            }

            set
            {
                _isFilenameSet = true;
                _outFilename = value;
            }
        }

        /// <summary>
        /// The literal name of the file in which to write the transcript.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "ByLiteralPath")]
        [Alias("PSPath", "LP")]
        [ValidateNotNullOrEmpty]
        public string LiteralPath
        {
            get
            {
                return _outFilename;
            }

            set
            {
                _isFilenameSet = true;
                _outFilename = value;
                _isLiteralPath = true;
            }
        }

        private bool _isLiteralPath = false;

        /// <summary>
        /// The literal name of the file in which to write the transcript.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "ByOutputDirectory")]
        [ValidateNotNullOrEmpty]
        public string OutputDirectory
        {
            get; set;
        }

        /// <summary>
        /// Describes the current state of the activity.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter Append
        {
            get
            {
                return _shouldAppend;
            }

            set
            {
                _shouldAppend = value;
            }
        }

        /// <summary>
        /// Property that sets force parameter.  This will reset the read-only
        /// attribute on an existing file.
        /// </summary>
        /// <remarks>
        /// The read-only attribute will not be replaced when the transcript is done.
        /// </remarks>
        [Parameter()]
        public SwitchParameter Force
        {
            get
            {
                return _force;
            }

            set
            {
                _force = value;
            }
        }

        private bool _force;

        /// <summary>
        /// Property that prevents file overwrite.
        /// </summary>
        [Parameter()]
        [Alias("NoOverwrite")]
        public SwitchParameter NoClobber
        {
            get
            {
                return _noclobber;
            }

            set
            {
                _noclobber = value;
            }
        }

        private bool _noclobber;

        /// <summary>
        /// Whether to include command invocation time headers between commands.
        /// </summary>
        [Parameter()]
        public SwitchParameter IncludeInvocationHeader
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets whether to use minimal transcript header.
        /// </summary>
        [Parameter]
        public SwitchParameter UseMinimalHeader
        {
            get; set;
        }

        /// <summary>
        /// Starts the transcription.
        /// </summary>
        protected override void BeginProcessing()
        {
            // If they haven't specified a path, figure out the correct output path.
            if (!_isFilenameSet)
            {
                // read the filename from $TRANSCRIPT
                object value = this.GetVariableValue("global:TRANSCRIPT", null);

                // $TRANSCRIPT is not set, so create a file name (the default: $HOME/My Documents/PowerShell_transcript.YYYYMMDDmmss.txt)
                if (value == null)
                {
                    // If they've specified an output directory, use it. Otherwise, use "My Documents"
                    if (OutputDirectory != null)
                    {
                        _outFilename = System.Management.Automation.Host.PSHostUserInterface.GetTranscriptPath(OutputDirectory, false);
                        _isLiteralPath = true;
                    }
                    else
                    {
                        _outFilename = System.Management.Automation.Host.PSHostUserInterface.GetTranscriptPath();
                    }
                }
                else
                {
                    _outFilename = (string)value;
                }
            }

            // Normalize outFilename here in case it is a relative path
            try
            {
                string effectiveFilePath = ResolveFilePath(Path, _isLiteralPath);

                if (!ShouldProcess(effectiveFilePath))
                    return;

                if (System.IO.File.Exists(effectiveFilePath))
                {
                    if (NoClobber && !Append)
                    {
                        string message = StringUtil.Format(TranscriptStrings.TranscriptFileExistsNoClobber,
                            effectiveFilePath,
                            "NoClobber"); // prevents localization
                        Exception uae = new UnauthorizedAccessException(message);
                        ErrorRecord errorRecord = new ErrorRecord(
                            uae, "NoClobber", ErrorCategory.ResourceExists, effectiveFilePath);

                        // NOTE: this call will throw
                        ThrowTerminatingError(errorRecord);
                    }

                    System.IO.FileInfo fInfo = new System.IO.FileInfo(effectiveFilePath);
                    if ((fInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        // Save some disk write time by checking whether file is readonly..
                        if (Force)
                        {
                            // Make sure the file is not read only
                            // Note that we will not clear the ReadOnly flag later
                            fInfo.Attributes &= ~(FileAttributes.ReadOnly);
                        }
                        else
                        {
                            string errorMessage = string.Format(
                                System.Globalization.CultureInfo.CurrentCulture,
                                TranscriptStrings.TranscriptFileReadOnly,
                                effectiveFilePath);
                            Exception innerException = new ArgumentException(errorMessage);
                            ThrowTerminatingError(new ErrorRecord(innerException, "FileReadOnly", ErrorCategory.InvalidArgument, effectiveFilePath));
                        }
                    }

                    // If they didn't specify -Append, empty the file
                    if (!_shouldAppend)
                    {
                        System.IO.File.WriteAllText(effectiveFilePath, string.Empty);
                    }
                }

                System.Management.Automation.Remoting.PSSenderInfo psSenderInfo =
                    this.SessionState.PSVariable.GetValue("PSSenderInfo") as System.Management.Automation.Remoting.PSSenderInfo;
                Host.UI.StartTranscribing(effectiveFilePath, psSenderInfo, IncludeInvocationHeader.ToBool(), UseMinimalHeader.IsPresent);

                // ch.StartTranscribing(effectiveFilePath, Append);

                // NTRAID#Windows Out Of Band Releases-931008-2006/03/21
                // Previous behavior was to write this even if ShouldProcess
                // returned false.  Why would we want that?
                PSObject outputObject = new PSObject(
                    StringUtil.Format(TranscriptStrings.TranscriptionStarted, Path));
                outputObject.Properties.Add(new PSNoteProperty("Path", Path));
                WriteObject(outputObject);
            }
            catch (Exception e)
            {
                try
                {
                    Host.UI.StopTranscribing();
                }
                catch
                {
                }

                string errorMessage = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    TranscriptStrings.CannotStartTranscription,
                    e.Message);
                ErrorRecord er = new ErrorRecord(
                    PSTraceSource.NewInvalidOperationException(e, errorMessage),
                    "CannotStartTranscription", ErrorCategory.InvalidOperation, null);
                ThrowTerminatingError(er);
            }
        }

        /// resolve a user provided file name or path (including globbing characters)
        /// to a fully qualified file path, using the file system provider
        private string ResolveFilePath(string filePath, bool isLiteralPath)
        {
            string path = null;

            try
            {
                if (isLiteralPath)
                {
                    path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(filePath);
                }
                else
                {
                    ProviderInfo provider = null;
                    Collection<string> filePaths =
                        SessionState.Path.GetResolvedProviderPathFromPSPath(filePath, out provider);

                    if (!provider.NameEquals(this.Context.ProviderNames.FileSystem))
                    {
                        ReportWrongProviderType(provider.FullName);
                    }

                    if (filePaths.Count > 1)
                    {
                        ReportMultipleFilesNotSupported();
                    }

                    path = filePaths[0];
                }
            }
            catch (ItemNotFoundException)
            {
                path = null;
            }

            if (string.IsNullOrEmpty(path))
            {
                CmdletProviderContext cmdletProviderContext = new CmdletProviderContext(this);
                ProviderInfo provider = null;
                PSDriveInfo drive = null;
                path =
                    SessionState.Path.GetUnresolvedProviderPathFromPSPath(
                        filePath, cmdletProviderContext, out provider, out drive);
                cmdletProviderContext.ThrowFirstErrorOrDoNothing();
                if (!provider.NameEquals(this.Context.ProviderNames.FileSystem))
                {
                    ReportWrongProviderType(provider.FullName);
                }
            }

            return path;
        }

        private void ReportWrongProviderType(string providerId)
        {
            ErrorRecord errorRecord = new ErrorRecord(
                PSTraceSource.NewInvalidOperationException(TranscriptStrings.ReadWriteFileNotFileSystemProvider, providerId),
                "ReadWriteFileNotFileSystemProvider",
                ErrorCategory.InvalidArgument,
                null);
            ThrowTerminatingError(errorRecord);
        }

        private void ReportMultipleFilesNotSupported()
        {
            ErrorRecord errorRecord = new ErrorRecord(
                PSTraceSource.NewInvalidOperationException(TranscriptStrings.MultipleFilesNotSupported),
                "MultipleFilesNotSupported",
                ErrorCategory.InvalidArgument,
                null);
            ThrowTerminatingError(errorRecord);
        }

        private bool _shouldAppend;
        private string _outFilename;
        private bool _isFilenameSet;
    }
}
