/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/


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
    /// Defines the implementation of the get-pfxcertificate cmdlet
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PfxCertificate", DefaultParameterSetName = "ByPath", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113323")]
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

        //
        // list of files that were not found
        //
        private ArrayList _filesNotFound = new ArrayList();


        /// <summary>
        /// Initializes a new instance of the GetPfxCertificateCommand
        /// class
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
                        try
                        {
                            cert = GetCertFromPfxFile(resolvedProviderPath);
                        }
                        catch (CryptographicException)
                        {
                            //
                            // CryptographicException is thrown when any error
                            // occurs inside the crypto class library. It has a
                            // protected member HResult that indicates the exact
                            // error but it is not available outside the class.
                            // Thus we have to assume that the above exception
                            // was thrown because the pfx file is password
                            // protected.
                            //
                            SecureString password = null;

                            string prompt = null;
                            prompt = CertificateCommands.GetPfxCertPasswordPrompt;

                            password = SecurityUtils.PromptForSecureString(Host.UI, prompt);
                            try
                            {
                                cert = GetCertFromPfxFile(resolvedProviderPath,
                                                          password);
                            }
                            catch (CryptographicException e)
                            {
                                //
                                // since we cannot really figure out the
                                // meaning of a given CryptographicException
                                // we have to use NotSpecified category here
                                //
                                ErrorRecord er =
                                    new ErrorRecord(e,
                                                    "GetPfxCertificateUnknownCryptoError",
                                                    ErrorCategory.NotSpecified,
                                                    null);
                                WriteError(er);
                                continue;
                            }
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

        private static X509Certificate2 GetCertFromPfxFile(string path)
        {
            X509Certificate2 cert = new X509Certificate2();

            cert.Import(path);

            return cert;
        }

        private static X509Certificate2 GetCertFromPfxFile(string path, SecureString password)
        {
            X509Certificate2 cert = new X509Certificate2();

            //
            // NTRAID#DevDiv Bugs-33007-2004/7/08-kumarp
            // the following will not be required once X509Certificate2.Import()
            // accepts a SecureString
            //
            string clearTextPassword = SecurityUtils.GetStringFromSecureString(password);

            cert.Import(path, clearTextPassword, X509KeyStorageFlags.DefaultKeySet);

            return cert;
        }
    }
}

