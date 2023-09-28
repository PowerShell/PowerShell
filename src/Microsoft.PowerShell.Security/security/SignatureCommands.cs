// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Security.Cryptography.X509Certificates;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the base class from which all signature commands
    /// are derived.
    /// </summary>
    public abstract class SignatureCommandsBase : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the path to the file for which to get or set the
        /// digital signature.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByPath")]
        public string[] FilePath
        {
            get
            {
                return _path;
            }

            set
            {
                _path = value;
            }
        }

        private string[] _path;

        /// <summary>
        /// Gets or sets the literal path to the file for which to get or set the
        /// digital signature.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByLiteralPath")]
        [Alias("PSPath")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] LiteralPath
        {
            get
            {
                return _path;
            }

            set
            {
                _path = value;
                _isLiteralPath = true;
            }
        }

        private bool _isLiteralPath = false;

        /// <summary>
        /// Gets or sets the digital signature to be written to
        /// the output pipeline.
        /// </summary>
        protected Signature Signature
        {
            get { return _signature; }

            set { _signature = value; }
        }

        private Signature _signature;

        /// <summary>
        /// Gets or sets the file type of the byte array containing the content with
        /// digital signature.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByContent")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] SourcePathOrExtension
        {
            get
            {
                return _sourcePathOrExtension;
            }

            set
            {
                _sourcePathOrExtension = value;
            }
        }

        private string[] _sourcePathOrExtension;

        /// <summary>
        /// File contents as a byte array.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByContent")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] Content
        {
            get
            {
                return _content;
            }

            set
            {
                _content = value;
            }
        }

        private byte[] _content;

        //
        // name of this command
        //
        private readonly string _commandName;

        /// <summary>
        /// Initializes a new instance of the SignatureCommandsBase class,
        /// using the given command name.
        /// </summary>
        /// <param name="name">
        /// The name of the command.
        /// </param>
        protected SignatureCommandsBase(string name) : base()
        {
            _commandName = name;
        }

        //
        // hide default ctor
        //
        private SignatureCommandsBase() : base() { }

        /// <summary>
        /// Processes records from the input pipeline.
        /// For each input object, the command gets or
        /// sets the digital signature on the object, and
        /// and exports the object.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (Content == null)
            {
                //
                // this cannot happen as we have specified the Path
                // property to be mandatory parameter
                //
                Dbg.Assert((FilePath != null) && (FilePath.Length > 0),
                    "GetSignatureCommand: Param binder did not bind path");

                foreach (string p in FilePath)
                {
                    Collection<string> paths = new();

                    // Expand wildcard characters
                    if (_isLiteralPath)
                    {
                        paths.Add(SessionState.Path.GetUnresolvedProviderPathFromPSPath(p));
                    }
                    else
                    {
                        try
                        {
                            foreach (PathInfo tempPath in SessionState.Path.GetResolvedPSPathFromPSPath(p))
                            {
                                paths.Add(tempPath.ProviderPath);
                            }
                        }
                        catch (ItemNotFoundException)
                        {
                            WriteError(
                                SecurityUtils.CreateFileNotFoundErrorRecord(
                                    SignatureCommands.FileNotFound,
                                    "SignatureCommandsBaseFileNotFound", p));
                        }
                    }

                    if (paths.Count == 0)
                        continue;

                    bool foundFile = false;

                    foreach (string path in paths)
                    {
                        if (!System.IO.Directory.Exists(path))
                        {
                            foundFile = true;

                            string resolvedFilePath = SecurityUtils.GetFilePathOfExistingFile(this, path);

                            if (resolvedFilePath == null)
                            {
                                WriteError(SecurityUtils.CreateFileNotFoundErrorRecord(
                                    SignatureCommands.FileNotFound,
                                    "SignatureCommandsBaseFileNotFound",
                                    path));
                            }
                            else
                            {
                                if ((Signature = PerformAction(resolvedFilePath)) != null)
                                {
                                    WriteObject(Signature);
                                }
                            }
                        }
                    }

                    if (!foundFile)
                    {
                        WriteError(SecurityUtils.CreateFileNotFoundErrorRecord(
                            SignatureCommands.CannotRetrieveFromContainer,
                            "SignatureCommandsBaseCannotRetrieveFromContainer"));
                    }
                }
            }
            else
            {
                foreach (string sourcePathOrExtension in SourcePathOrExtension)
                {
                    if ((Signature = PerformAction(sourcePathOrExtension, Content)) != null)
                    {
                        WriteObject(Signature);
                    }
                }
            }
        }

        /// <summary>
        /// Performs the action (ie: get signature, or set signature)
        /// on the specified file.
        /// </summary>
        /// <param name="filePath">
        /// The name of the file on which to perform the action.
        /// </param>
        protected abstract Signature PerformAction(string filePath);

        /// <summary>
        /// Performs the action (ie: get signature, or set signature)
        /// on the specified contents.
        /// </summary>
        /// <param name="fileName">
        /// The filename used for type if content is specified.
        /// </param>
        /// <param name="content">
        /// The file contents on which to perform the action.
        /// </param>
        protected abstract Signature PerformAction(string fileName, byte[] content);
    }

    /// <summary>
    /// Defines the implementation of the 'get-AuthenticodeSignature' cmdlet.
    /// This cmdlet extracts the digital signature from the given file.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AuthenticodeSignature", DefaultParameterSetName = "ByPath", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096823")]
    [OutputType(typeof(Signature))]
    public sealed class GetAuthenticodeSignatureCommand : SignatureCommandsBase
    {
        /// <summary>
        /// Initializes a new instance of the GetSignatureCommand class.
        /// </summary>
        public GetAuthenticodeSignatureCommand() : base("Get-AuthenticodeSignature") { }

        /// <summary>
        /// Gets the signature from the specified file.
        /// </summary>
        /// <param name="filePath">
        /// The name of the file on which to perform the action.
        /// </param>
        /// <returns>
        /// The signature on the specified file.
        /// </returns>
        protected override Signature PerformAction(string filePath)
        {
            return SignatureHelper.GetSignature(filePath, null);
        }

        /// <summary>
        /// Gets the signature from the specified file contents.
        /// </summary>
        /// <param name="sourcePathOrExtension">The file type associated with the contents.</param>
        /// <param name="content">
        /// The contents of the file on which to perform the action.
        /// </param>
        /// <returns>
        /// The signature on the specified file contents.
        /// </returns>
        protected override Signature PerformAction(string sourcePathOrExtension, byte[] content)
        {
            return SignatureHelper.GetSignature(sourcePathOrExtension, content);
        }
    }

    /// <summary>
    /// Defines the implementation of the 'set-AuthenticodeSignature' cmdlet.
    /// This cmdlet sets the digital signature on a given file.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "AuthenticodeSignature", SupportsShouldProcess = true, DefaultParameterSetName = "ByPath",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096919")]
    [OutputType(typeof(Signature))]
    public sealed class SetAuthenticodeSignatureCommand : SignatureCommandsBase
    {
        /// <summary>
        /// Initializes a new instance of the SetAuthenticodeSignatureCommand class.
        /// </summary>
        public SetAuthenticodeSignatureCommand() : base("set-AuthenticodeSignature") { }

        /// <summary>
        /// Gets or sets the certificate with which to sign the
        /// file.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        public X509Certificate2 Certificate
        {
            get
            {
                return _certificate;
            }

            set
            {
                _certificate = value;
            }
        }

        private X509Certificate2 _certificate;

        /// <summary>
        /// Gets or sets the additional certificates to
        /// include in the digital signature.
        /// Use 'signer' to include only the signer's certificate.
        /// Use 'notroot' to include all certificates in the certificate
        ///    chain, except for the root authority.
        /// Use 'all' to include all certificates in the certificate chain.
        ///
        /// Defaults to 'notroot'.
        /// </summary>
        [Parameter(Mandatory = false)]
        [ValidateSet("signer", "notroot", "all")]
        public string IncludeChain
        {
            get
            {
                return _includeChain;
            }

            set
            {
                _includeChain = value;
            }
        }

        private string _includeChain = "notroot";

        /// <summary>
        /// Gets or sets the Url of the time stamping server.
        /// The time stamping server certifies the exact time
        /// that the certificate was added to the file.
        /// </summary>
        [Parameter(Mandatory = false)]
        public string TimestampServer
        {
            get
            {
                return _timestampServer;
            }

            set
            {
                value ??= string.Empty;

                _timestampServer = value;
            }
        }

        private string _timestampServer = string.Empty;

        /// <summary>
        /// Gets or sets the hash algorithm used for signing.
        /// This string value must represent the name of a Cryptographic Algorithm
        /// Identifier supported by Windows.
        /// </summary>
        [Parameter(Mandatory = false)]
        public string HashAlgorithm
        {
            get
            {
                return _hashAlgorithm;
            }

            set
            {
                _hashAlgorithm = value;
            }
        }

        private string _hashAlgorithm = "SHA256";

        /// <summary>
        /// Property that sets force parameter.
        /// </summary>
        [Parameter]
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
        /// Sets the digital signature on the specified file.
        /// </summary>
        /// <param name="filePath">
        /// The name of the file on which to perform the action.
        /// </param>
        /// <returns>
        /// The signature on the specified file.
        /// </returns>
        protected override Signature PerformAction(string filePath)
        {
            SigningOption option = GetSigningOption(IncludeChain);

            if (Certificate == null)
            {
                throw PSTraceSource.NewArgumentNullException("certificate");
            }

            //
            // if the cert is not good for signing, we cannot
            // process any more files. Exit the command.
            //
            if (!SecuritySupport.CertIsGoodForSigning(Certificate))
            {
                Exception e = PSTraceSource.NewArgumentException(
                        "certificate",
                        SignatureCommands.CertNotGoodForSigning);

                throw e;
            }

            if (!ShouldProcess(filePath))
                return null;

            FileInfo readOnlyFileInfo = null;
            try
            {
                if (this.Force)
                {
                    try
                    {
                        // remove readonly attributes on the file
                        FileInfo fInfo = new(filePath);
                        if (fInfo != null)
                        {
                            // Save some disk write time by checking whether file is readonly..
                            if ((fInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                            {
                                // remember to reset the read-only attribute later
                                readOnlyFileInfo = fInfo;
                                // Make sure the file is not read only
                                fInfo.Attributes &= ~(FileAttributes.ReadOnly);
                            }
                        }
                    }
                    // These are the known exceptions for File.Load and StreamWriter.ctor
                    catch (ArgumentException e)
                    {
                        ErrorRecord er = new(
                            e,
                            "ForceArgumentException",
                            ErrorCategory.WriteError,
                            filePath);
                        WriteError(er);
                        return null;
                    }
                    catch (IOException e)
                    {
                        ErrorRecord er = new(
                            e,
                            "ForceIOException",
                            ErrorCategory.WriteError,
                            filePath);
                        WriteError(er);
                        return null;
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        ErrorRecord er = new(
                            e,
                            "ForceUnauthorizedAccessException",
                            ErrorCategory.PermissionDenied,
                            filePath);
                        WriteError(er);
                        return null;
                    }
                    catch (NotSupportedException e)
                    {
                        ErrorRecord er = new(
                            e,
                            "ForceNotSupportedException",
                            ErrorCategory.WriteError,
                            filePath);
                        WriteError(er);
                        return null;
                    }
                    catch (System.Security.SecurityException e)
                    {
                        ErrorRecord er = new(
                            e,
                            "ForceSecurityException",
                            ErrorCategory.PermissionDenied,
                            filePath);
                        WriteError(er);
                        return null;
                    }
                }

                //
                // ProcessRecord() code in base class has already
                // ascertained that filePath really represents an existing
                // file. Thus we can safely call GetFileSize() below.
                //

                if (SecurityUtils.GetFileSize(filePath) < 4)
                {
                    // Note that the message param comes first
                    string message = string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        UtilsStrings.FileSmallerThan4Bytes, filePath);

                    PSArgumentException e = new(message, nameof(filePath));
                    ErrorRecord er = SecurityUtils.CreateInvalidArgumentErrorRecord(
                            e,
                            "SignatureCommandsBaseFileSmallerThan4Bytes"
                            );

                    WriteError(er);

                    return null;
                }

                return SignatureHelper.SignFile(option,
                                                filePath,
                                                Certificate,
                                                TimestampServer,
                                                _hashAlgorithm);
            }
            finally
            {
                // reset the read-only attribute
                if (readOnlyFileInfo != null)
                {
                    readOnlyFileInfo.Attributes |= FileAttributes.ReadOnly;
                }
            }
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        protected override Signature PerformAction(string sourcePathOrExtension, byte[] content)
        {
            throw new NotImplementedException();
        }

        private struct SigningOptionInfo
        {
            internal SigningOption option;
            internal string optionName;

            internal SigningOptionInfo(SigningOption o, string n)
            {
                option = o;
                optionName = n;
            }
        }

        /// <summary>
        /// Association between SigningOption.* values and the
        /// corresponding string names.
        /// </summary>
        private static readonly SigningOptionInfo[] s_sigOptionInfo =
        {
            new SigningOptionInfo(SigningOption.AddOnlyCertificate, "signer"),
            new SigningOptionInfo(SigningOption.AddFullCertificateChainExceptRoot, "notroot"),
            new SigningOptionInfo(SigningOption.AddFullCertificateChain, "all")
        };

        /// <summary>
        /// Get SigningOption value corresponding to a string name.
        /// </summary>
        /// <param name="optionName">Name of option.</param>
        /// <returns>SigningOption.</returns>
        private static SigningOption GetSigningOption(string optionName)
        {
            foreach (SigningOptionInfo si in s_sigOptionInfo)
            {
                if (string.Equals(optionName, si.optionName,
                                  StringComparison.OrdinalIgnoreCase))
                {
                    return si.option;
                }
            }

            return SigningOption.AddFullCertificateChainExceptRoot;
        }
    }
}
