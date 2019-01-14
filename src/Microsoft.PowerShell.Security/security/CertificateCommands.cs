// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Dbg = System.Management.Automation.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the implementation of the get-pfxcertificate cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PfxCertificate", DefaultParameterSetName = "ByPath", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113323")]
    [OutputType(typeof(X509Certificate2))]
    public sealed class GetPfxCertificateCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the path of the item for which to obtain the
        /// certificate.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Mandatory = true, ParameterSetName = "ByPath")]
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
        /// Gets or sets the literal path of the item for which to obtain the
        /// certificate.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true, Mandatory = true, ParameterSetName = "ByLiteralPath")]
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
        /// Gets or sets the password for unlocking the certificate.
        /// </summary>
        [Parameter(Mandatory = false)]
        public SecureString Password { get; set; }

        /// <summary>
        /// Do not prompt for password if not given.
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter NoPromptForPassword { get; set; }

        //
        // list of files that were not found
        //
        private ArrayList _filesNotFound = new ArrayList();

        /// <summary>
        /// Initializes a new instance of the GetPfxCertificateCommand
        /// class.
        /// </summary>
        public GetPfxCertificateCommand() : base()
        {
        }

        /// <summary>
        /// Processes records from the input pipeline.
        /// For each input file, the command retrieves its
        /// corresponding certificate.
        /// </summary>
        protected override void ProcessRecord()
        {
            //
            // this cannot happen as we have specified the Path
            // property to be a mandatory parameter
            //
            Dbg.Assert((FilePath != null) && (FilePath.Length > 0),
                       "GetCertificateCommand: Param binder did not bind path");

            X509Certificate2 cert = null;

            foreach (string p in FilePath)
            {
                List<string> paths = new List<string>();

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
                        _filesNotFound.Add(p);
                    }
                }

                foreach (string resolvedPath in paths)
                {
                    string resolvedProviderPath =
                        SecurityUtils.GetFilePathOfExistingFile(this, resolvedPath);

                    if (resolvedProviderPath == null)
                    {
                        _filesNotFound.Add(p);
                    }
                    else
                    {
                        if (Password == null && !NoPromptForPassword.IsPresent) 
                        {
                            try 
                            {
                                cert = GetCertFromPfxFile(resolvedProviderPath, null);
                                WriteObject(cert);
                                continue;
                            } 
                            catch (CryptographicException) 
                            {
                                Password = SecurityUtils.PromptForSecureString(
                                    Host.UI,
                                    CertificateCommands.GetPfxCertPasswordPrompt);
                            }                            
                        }
                        
                        try
                        {
                            cert = GetCertFromPfxFile(resolvedProviderPath, Password);
                        }
                        catch (CryptographicException e)
                        {
                            ErrorRecord er =
                                new ErrorRecord(e,
                                                "GetPfxCertificateUnknownCryptoError",
                                                ErrorCategory.NotSpecified,
                                                null);
                            WriteError(er);
                            continue;
                        }

                        WriteObject(cert);
                    }
                }
            }

            if (_filesNotFound.Count > 0)
            {
                if (_filesNotFound.Count == FilePath.Length)
                {
                    ErrorRecord er =
                        SecurityUtils.CreateFileNotFoundErrorRecord(
                            CertificateCommands.NoneOfTheFilesFound,
                            "GetPfxCertCommandNoneOfTheFilesFound");

                    ThrowTerminatingError(er);
                }
                else
                {
                    //
                    // we found some files but not others.
                    // Write error for each missing file
                    //
                    foreach (string f in _filesNotFound)
                    {
                        ErrorRecord er =
                            SecurityUtils.CreateFileNotFoundErrorRecord(
                                CertificateCommands.FileNotFound,
                                "GetPfxCertCommandFileNotFound",
                                f
                            );

                        WriteError(er);
                    }
                }
            }
        }

        private static X509Certificate2 GetCertFromPfxFile(string path, SecureString password)
        {
            var cert = new X509Certificate2(path, password, X509KeyStorageFlags.DefaultKeySet);
            return cert;
        }
    }
}

