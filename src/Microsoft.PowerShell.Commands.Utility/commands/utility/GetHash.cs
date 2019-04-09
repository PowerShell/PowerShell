// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Security.Cryptography;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Get-FileHash.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "FileHash", DefaultParameterSetName = PathParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=517145")]
    [OutputType(typeof(FileHashInfo))]
    public class GetFileHashCommand : HashCmdletBase
    {
        /// <summary>
        /// Path parameter.
        /// The paths of the files to calculate hash values.
        /// Resolved wildcards.
        /// </summary>
        /// <value></value>
        [Parameter(Mandatory = true, ParameterSetName = PathParameterSet, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get
            {
                return _paths;
            }

            set
            {
                _paths = value;
            }
        }

        /// <summary>
        /// LiteralPath parameter.
        /// The literal paths of the files to calculate a hashs.
        /// Don't resolved wildcards.
        /// </summary>
        /// <value></value>
        [Parameter(Mandatory = true, ParameterSetName = LiteralPathParameterSet, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get
            {
                return _paths;
            }

            set
            {
                _paths = value;
            }
        }

        private string[] _paths;

        /// <summary>
        /// InputStream parameter.
        /// The stream of the file to calculate a hash.
        /// </summary>
        /// <value></value>
        [Parameter(Mandatory = true, ParameterSetName = StreamParameterSet, Position = 0)]
        public Stream InputStream { get; set; }

        /// <summary>
        /// BeginProcessing() override.
        /// This is for hash function init.
        /// </summary>
        protected override void BeginProcessing()
        {
            InitHasher(Algorithm);
        }

        /// <summary>
        /// ProcessRecord() override.
        /// This is for paths collecting from pipe.
        /// </summary>
        protected override void ProcessRecord()
        {
            List<string> pathsToProcess = new List<string>();
            ProviderInfo provider = null;

            switch (ParameterSetName)
            {
                case PathParameterSet:
                    // Resolve paths and check existence
                    foreach (string path in _paths)
                    {
                        try
                        {
                            Collection<string> newPaths = Context.SessionState.Path.GetResolvedProviderPathFromPSPath(path, out provider);
                            if (newPaths != null)
                            {
                                pathsToProcess.AddRange(newPaths);
                            }
                        }
                        catch (ItemNotFoundException e)
                        {
                            if (!WildcardPattern.ContainsWildcardCharacters(path))
                            {
                                ErrorRecord errorRecord = new ErrorRecord(e,
                                    "FileNotFound",
                                    ErrorCategory.ObjectNotFound,
                                    path);
                                WriteError(errorRecord);
                            }
                        }
                    }

                    break;
                case LiteralPathParameterSet:
                    foreach (string path in _paths)
                    {
                        string newPath = Context.SessionState.Path.GetUnresolvedProviderPathFromPSPath(path);
                        pathsToProcess.Add(newPath);
                    }

                    break;
            }

            foreach (string path in pathsToProcess)
            {
                byte[] bytehash = null;
                string hash = null;
                Stream openfilestream = null;

                try
                {
                    openfilestream = File.OpenRead(path);
                    bytehash = hasher.ComputeHash(openfilestream);

                    hash = BitConverter.ToString(bytehash).Replace("-", string.Empty);
                    WriteHashResult(Algorithm, hash, path);
                }
                catch (FileNotFoundException ex)
                {
                    ErrorRecord errorRecord = new ErrorRecord(ex,
                        "FileNotFound",
                        ErrorCategory.ObjectNotFound,
                        path);
                    WriteError(errorRecord);
                }
                finally
                {
                    openfilestream?.Dispose();
                }
            }
        }

        /// <summary>
        /// Perform common error checks.
        /// Populate source code.
        /// </summary>
        protected override void EndProcessing()
        {
            if (ParameterSetName == StreamParameterSet)
            {
                byte[] bytehash = null;
                string hash = null;

                bytehash = hasher.ComputeHash(InputStream);

                hash = BitConverter.ToString(bytehash).Replace("-", string.Empty);
                WriteHashResult(Algorithm, hash, string.Empty);
            }
        }

        /// <summary>
        /// Create FileHashInfo object and output it.
        /// </summary>
        private void WriteHashResult(string Algorithm, string hash, string path)
        {
            FileHashInfo result = new FileHashInfo();
            result.Algorithm = Algorithm;
            result.Hash = hash;
            result.Path = path;
            WriteObject(result);
        }

        /// <summary>
        /// Parameter set names.
        /// </summary>
        private const string PathParameterSet = "Path";
        private const string LiteralPathParameterSet = "LiteralPath";
        private const string StreamParameterSet = "StreamParameterSet";

    }

    /// <summary>
    /// Base Cmdlet for cmdlets which deal with crypto hashes.
    /// </summary>
    public class HashCmdletBase : PSCmdlet
    {
        /// <summary>
        /// Algorithm parameter.
        /// The hash algorithm name: "SHA1", "SHA256", "SHA384", "SHA512", "MD5".
        /// </summary>
        /// <value></value>
        [Parameter(Position = 1)]
        [ValidateSet(HashAlgorithmNames.SHA1,
                     HashAlgorithmNames.SHA256,
                     HashAlgorithmNames.SHA384,
                     HashAlgorithmNames.SHA512,
                     HashAlgorithmNames.MD5)]
        public string Algorithm
        {
            get
            {
                return _Algorithm;
            }

            set
            {
                // A hash algorithm name is case sensitive
                // and always must be in upper case
                _Algorithm = value.ToUpper();
            }
        }

        private string _Algorithm = HashAlgorithmNames.SHA256;

        /// <summary>
        /// Hash algorithm is used.
        /// </summary>
        protected HashAlgorithm hasher;

        /// <summary>
        /// Hash algorithm names.
        /// </summary>
        internal static class HashAlgorithmNames
        {
            public const string MD5 = "MD5";
            public const string SHA1 = "SHA1";
            public const string SHA256 = "SHA256";
            public const string SHA384 = "SHA384";
            public const string SHA512 = "SHA512";
        }

        /// <summary>
        /// Init a hash algorithm.
        /// </summary>
        protected void InitHasher(String Algorithm)
        {
            try
            {
                switch (Algorithm)
                {
                    case HashAlgorithmNames.SHA1:
                        hasher = SHA1.Create();
                        break;
                    case HashAlgorithmNames.SHA256:
                        hasher = SHA256.Create();
                        break;
                    case HashAlgorithmNames.SHA384:
                        hasher = SHA384.Create();
                        break;
                    case HashAlgorithmNames.SHA512:
                        hasher = SHA512.Create();
                        break;
                    case HashAlgorithmNames.MD5:
                        hasher = MD5.Create();
                        break;
                }
            }
            catch
            {
                // Seems it will never throw! Remove?
                Exception exc = new NotSupportedException(UtilityCommonStrings.AlgorithmTypeNotSupported);
                ThrowTerminatingError(new ErrorRecord(exc, "AlgorithmTypeNotSupported", ErrorCategory.NotImplemented, null));
            }
        }
    }

    /// <summary>
    /// FileHashInfo class contains information about a file hash.
    /// </summary>
    public class FileHashInfo
    {
        /// <summary>
        /// Hash algorithm name.
        /// </summary>
        public string Algorithm { get; set; }

        /// <summary>
        /// Hash value.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// File path.
        /// </summary>
        public string Path { get; set; }
    }
}
