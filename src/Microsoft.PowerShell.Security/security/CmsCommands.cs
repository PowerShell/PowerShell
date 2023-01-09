// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the implementation of the 'Protect-CmsMessage' cmdlet.
    ///
    /// This cmdlet generates a new encrypted CMS message given the
    /// recipient and content supplied.
    /// </summary>
    [Cmdlet(VerbsSecurity.Protect, "CmsMessage", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096826", DefaultParameterSetName = "ByContent")]
    [OutputType(typeof(string))]
    public sealed class ProtectCmsMessageCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the recipient of the CMS Message.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public CmsMessageRecipient[] To
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the content of the CMS Message.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByContent")]
        [AllowNull()]
        [AllowEmptyString()]
        public PSObject Content
        {
            get;
            set;
        }

        private readonly PSDataCollection<PSObject> _inputObjects = new();

        /// <summary>
        /// Gets or sets the content of the CMS Message by path.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "ByPath")]
        public string Path
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the content of the CMS Message by literal path.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "ByLiteralPath")]
        public string LiteralPath
        {
            get;
            set;
        }

        private string _resolvedPath = null;

        /// <summary>
        /// Emits the protected message to a file path.
        /// </summary>
        [Parameter(Position = 2)]
        public string OutFile
        {
            get;
            set;
        }

        private string _resolvedOutFile = null;

        /// <summary>
        /// Validate / convert arguments.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Validate Path
            if (!string.IsNullOrEmpty(Path))
            {
                ProviderInfo provider = null;
                Collection<string> resolvedPaths = GetResolvedProviderPathFromPSPath(Path, out provider);

                // Ensure the path is a single path from the file system provider
                if ((resolvedPaths.Count > 1) ||
                    (!string.Equals(provider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase)))
                {
                    ErrorRecord error = new(
                        new ArgumentException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                CmsCommands.FilePathMustBeFileSystemPath,
                                Path)),
                        "FilePathMustBeFileSystemPath",
                        ErrorCategory.ObjectNotFound,
                        provider);
                    ThrowTerminatingError(error);
                }

                _resolvedPath = resolvedPaths[0];
            }

            if (!string.IsNullOrEmpty(LiteralPath))
            {
                // Validate that the path exists
                SessionState.InvokeProvider.Item.Get(new string[] { LiteralPath }, false, true);
                _resolvedPath = LiteralPath;
            }

            // Validate OutFile
            if (!string.IsNullOrEmpty(OutFile))
            {
                _resolvedOutFile = GetUnresolvedProviderPathFromPSPath(OutFile);
            }
        }

        /// <summary>
        /// Processes records from the input pipeline.
        /// For each input object, the command encrypts
        /// and exports the object.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (string.Equals("ByContent", this.ParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                _inputObjects.Add(Content);
            }
        }

        /// <summary>
        /// Encrypts and outputs the message.
        /// </summary>
        protected override void EndProcessing()
        {
            byte[] contentBytes = null;

            if (_inputObjects.Count > 0)
            {
                StringBuilder outputString = new();

                Collection<PSObject> output = System.Management.Automation.PowerShell.Create()
                    .AddCommand("Microsoft.PowerShell.Utility\\Out-String")
                    .AddParameter("Stream")
                    .Invoke(_inputObjects);

                foreach (PSObject outputObject in output)
                {
                    if (outputString.Length > 0)
                    {
                        outputString.AppendLine();
                    }

                    outputString.Append(outputObject);
                }

                contentBytes = System.Text.Encoding.UTF8.GetBytes(outputString.ToString());
            }
            else
            {
                contentBytes = System.IO.File.ReadAllBytes(_resolvedPath);
            }

            ErrorRecord terminatingError = null;
            string encodedContent = CmsUtils.Encrypt(contentBytes, To, this.SessionState, out terminatingError);

            if (terminatingError != null)
            {
                ThrowTerminatingError(terminatingError);
            }

            if (string.IsNullOrEmpty(_resolvedOutFile))
            {
                WriteObject(encodedContent);
            }
            else
            {
                System.IO.File.WriteAllText(_resolvedOutFile, encodedContent);
            }
        }
    }

    /// <summary>
    /// Defines the implementation of the 'Get-CmsMessage' cmdlet.
    ///
    /// This cmdlet retrieves information about an encrypted CMS
    /// message.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "CmsMessage", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096598")]
    [OutputType(typeof(EnvelopedCms))]
    public sealed class GetCmsMessageCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the content of the CMS Message.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByContent")]
        [AllowNull()]
        [AllowEmptyString()]
        public string Content
        {
            get;
            set;
        }

        private readonly StringBuilder _contentBuffer = new();

        /// <summary>
        /// Gets or sets the CMS Message by path.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "ByPath")]
        public string Path
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the CMS Message by literal path.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "ByLiteralPath")]
        public string LiteralPath
        {
            get;
            set;
        }

        private string _resolvedPath = null;

        /// <summary>
        /// Validate / convert arguments.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Validate Path
            if (!string.IsNullOrEmpty(Path))
            {
                ProviderInfo provider = null;
                Collection<string> resolvedPaths = GetResolvedProviderPathFromPSPath(Path, out provider);

                // Ensure the path is a single path from the file system provider
                if ((resolvedPaths.Count > 1) ||
                    (!string.Equals(provider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase)))
                {
                    ErrorRecord error = new(
                        new ArgumentException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                CmsCommands.FilePathMustBeFileSystemPath,
                                Path)),
                        "FilePathMustBeFileSystemPath",
                        ErrorCategory.ObjectNotFound,
                        provider);
                    ThrowTerminatingError(error);
                }

                _resolvedPath = resolvedPaths[0];
            }

            if (!string.IsNullOrEmpty(LiteralPath))
            {
                // Validate that the path exists
                SessionState.InvokeProvider.Item.Get(new string[] { LiteralPath }, false, true);
                _resolvedPath = LiteralPath;
            }
        }

        /// <summary>
        /// Processes records from the input pipeline.
        /// For each input object, the command gets the information
        /// about the protected message and exports the object.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (string.Equals("ByContent", this.ParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                if (_contentBuffer.Length > 0)
                {
                    _contentBuffer.Append(System.Environment.NewLine);
                }

                _contentBuffer.Append(Content);
            }
        }

        /// <summary>
        /// Gets the CMS Message object.
        /// </summary>
        protected override void EndProcessing()
        {
            string actualContent = null;

            // Read in the content
            if (string.Equals("ByContent", this.ParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                actualContent = _contentBuffer.ToString();
            }
            else
            {
                actualContent = System.IO.File.ReadAllText(_resolvedPath);
            }

            // Extract out the bytes and Base64 decode them
            byte[] contentBytes = CmsUtils.RemoveAsciiArmor(actualContent, CmsUtils.BEGIN_CMS_SIGIL, CmsUtils.END_CMS_SIGIL, out int _, out int _);
            if (contentBytes == null)
            {
                ErrorRecord error = new(
                    new ArgumentException(CmsCommands.InputContainedNoEncryptedContent),
                    "InputContainedNoEncryptedContent", ErrorCategory.ObjectNotFound, null);
                ThrowTerminatingError(error);
            }

            EnvelopedCms cms = new();
            cms.Decode(contentBytes);

            PSObject result = new(cms);
            List<object> recipients = new();
            foreach (RecipientInfo recipient in cms.RecipientInfos)
            {
                recipients.Add(recipient.RecipientIdentifier.Value);
            }

            result.Properties.Add(
                new PSNoteProperty("Recipients", recipients));
            result.Properties.Add(
                new PSNoteProperty("Content", actualContent));

            WriteObject(result);
        }
    }

    /// <summary>
    /// Defines the implementation of the 'Unprotect-CmsMessage' cmdlet.
    ///
    /// This cmdlet retrieves the clear text content of an encrypted CMS
    /// message.
    /// </summary>
    [Cmdlet(VerbsSecurity.Unprotect, "CmsMessage", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096701", DefaultParameterSetName = "ByWinEvent")]
    [OutputType(typeof(string))]
    public sealed class UnprotectCmsMessageCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the content of the CMS Message.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByContent")]
        [AllowNull()]
        [AllowEmptyString()]
        public string Content
        {
            get;
            set;
        }

        private readonly StringBuilder _contentBuffer = new();

        /// <summary>
        /// Gets or sets the Windows Event Log Message with contents to be decrypted.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByWinEvent")]
        [PSTypeName("System.Diagnostics.Eventing.Reader.EventLogRecord")]
        public PSObject EventLogRecord
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the CMS Message by path.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "ByPath")]
        public string Path
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the CMS Message by literal path.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "ByLiteralPath")]
        public string LiteralPath
        {
            get;
            set;
        }

        private string _resolvedPath = null;

        /// <summary>
        /// Determines whether to include the decrypted content in its original context,
        /// rather than just output the decrypted content itself.
        /// </summary>
        [Parameter()]
        public SwitchParameter IncludeContext
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the recipient of the CMS Message.
        /// </summary>
        [Parameter(Position = 1)]
        public CmsMessageRecipient[] To
        {
            get;
            set;
        }

        /// <summary>
        /// Validate / convert arguments.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Validate Path
            if (!string.IsNullOrEmpty(Path))
            {
                ProviderInfo provider = null;
                Collection<string> resolvedPaths = GetResolvedProviderPathFromPSPath(Path, out provider);

                // Ensure the path is a single path from the file system provider
                if ((resolvedPaths.Count > 1) ||
                    (!string.Equals(provider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase)))
                {
                    ErrorRecord error = new(
                        new ArgumentException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                CmsCommands.FilePathMustBeFileSystemPath,
                                Path)),
                        "FilePathMustBeFileSystemPath",
                        ErrorCategory.ObjectNotFound,
                        provider);
                    ThrowTerminatingError(error);
                }

                _resolvedPath = resolvedPaths[0];
            }

            if (!string.IsNullOrEmpty(LiteralPath))
            {
                // Validate that the path exists
                SessionState.InvokeProvider.Item.Get(new string[] { LiteralPath }, false, true);
                _resolvedPath = LiteralPath;
            }
        }

        /// <summary>
        /// Processes records from the input pipeline.
        /// For each input object, the command gets the information
        /// about the protected message and exports the object.
        /// </summary>
        protected override void ProcessRecord()
        {
            // If we're process by content, collect it.
            if (string.Equals("ByContent", this.ParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                if (_contentBuffer.Length > 0)
                {
                    _contentBuffer.Append(System.Environment.NewLine);
                }

                _contentBuffer.Append(Content);
            }

            // If we're processing event log records, decrypt those inline.
            if (string.Equals("ByWinEvent", this.ParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                string actualContent = EventLogRecord.Properties["Message"].Value.ToString();
                string decrypted = Decrypt(actualContent);

                if (!IncludeContext)
                {
                    WriteObject(decrypted);
                }
                else
                {
                    EventLogRecord.Properties["Message"].Value = decrypted;
                    WriteObject(EventLogRecord);
                }
            }
        }

        /// <summary>
        /// Processes records from the input pipeline.
        /// For each input object, the command gets the information
        /// about the protected message and exports the object.
        /// </summary>
        protected override void EndProcessing()
        {
            if (string.Equals("ByWinEvent", this.ParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string actualContent = null;

            // Read in the content
            if (string.Equals("ByContent", this.ParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                actualContent = _contentBuffer.ToString();
            }
            else
            {
                actualContent = System.IO.File.ReadAllText(_resolvedPath);
            }

            string decrypted = Decrypt(actualContent);
            WriteObject(decrypted);
        }

        private string Decrypt(string actualContent)
        {
            // Extract out the bytes and Base64 decode them
            int startIndex, endIndex;
            byte[] messageBytes = CmsUtils.RemoveAsciiArmor(actualContent, CmsUtils.BEGIN_CMS_SIGIL, CmsUtils.END_CMS_SIGIL, out startIndex, out endIndex);
            if ((messageBytes == null) && (!IncludeContext))
            {
                ErrorRecord error = new(
                    new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            CmsCommands.InputContainedNoEncryptedContentIncludeContext,
                            "-IncludeContext")),
                    "InputContainedNoEncryptedContentIncludeContext",
                    ErrorCategory.ObjectNotFound,
                    targetObject: null);
                ThrowTerminatingError(error);
            }

            // Capture the pre and post context, if there was any
            string preContext = null;
            string postContext = null;
            if (IncludeContext)
            {
                if (startIndex > -1)
                {
                    preContext = actualContent.Substring(0, startIndex);
                }

                if (endIndex > -1)
                {
                    postContext = actualContent.Substring(endIndex);
                }
            }

            EnvelopedCms cms = new();
            X509Certificate2Collection certificates = new();

            if ((To != null) && (To.Length > 0))
            {
                ErrorRecord error = null;

                foreach (CmsMessageRecipient recipient in To)
                {
                    recipient.Resolve(this.SessionState, ResolutionPurpose.Decryption, out error);
                    if (error != null)
                    {
                        ThrowTerminatingError(error);
                        return null;
                    }

                    foreach (X509Certificate2 certificate in recipient.Certificates)
                    {
                        certificates.Add(certificate);
                    }
                }
            }

            string resultString = actualContent;
            if (messageBytes != null)
            {
                cms.Decode(messageBytes);
                cms.Decrypt(certificates);

                resultString = System.Text.Encoding.UTF8.GetString(cms.ContentInfo.Content);
            }

            if (IncludeContext)
            {
                if (preContext != null)
                {
                    resultString = preContext + resultString;
                }

                if (postContext != null)
                {
                    resultString += postContext;
                }
            }

            return resultString;
        }
    }
}
