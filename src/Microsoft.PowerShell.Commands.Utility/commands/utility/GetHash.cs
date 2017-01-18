using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Get-StringHash
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "StringHash", HelpUri = "")]
    [OutputType(typeof(String))]
    public class GetStringHashCommand : HashCmdletBase
    {
        /// <summary>
        /// InputString parameter
        /// Calculate a hash of the string
        /// </summary>
        /// <value></value>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [AllowNull()]
        [AllowEmptyString()]
        public String[] InputString { get; set; }

        /// <summary>
        /// BeginProcessing() override
        /// Hash function init
        /// </summary>
        protected override void BeginProcessing()
        {
            InitHasher(Algorithm);
        }

        /// <summary>
        /// ProcessRecord() override
        /// Processing strings from pipeline
        /// </summary>
        protected override void ProcessRecord()
        {
            // For null input string the result hash is 'null'
            if (InputString != null)
            {
                foreach (string str in InputString)
                {
                    byte[] bytehash = null;
                    String hash = null;
                    bytehash = hasher.ComputeHash(Encoding.UTF8.GetBytes(str));
                    hash = BitConverter.ToString(bytehash).Replace("-","");
                    WriteObject(hash);
                }
            }
        }
    }



    /// <summary>
    /// This class implements Get-FileHash
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "FileHash", DefaultParameterSetName = PathParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=517145")]
    [OutputType(typeof(FileHashInfo))]
    public class GetFileHashCommand : HashCmdletBase
    {
        /// <summary>
        /// Path parameter
        /// The paths of the files to calculate a hashs
        /// Resolved wildcards
        /// </summary>
        /// <value></value>
        [Parameter(Mandatory = true, ParameterSetName = PathParameterSet, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public String[] Path
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
        /// LiteralPath parameter
        /// The literal paths of the files to calculate a hashs
        /// Don't resolved wildcards
        /// </summary>
        /// <value></value>
        [Parameter(Mandatory = true, ParameterSetName = LiteralPathParameterSet, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath")]
        public String[] LiteralPath
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

        private String[] _paths;

        /// <summary>
        /// InputStream parameter
        /// The stream of the file to calculate a hash
        /// </summary>
        /// <value></value>
        [Parameter(Mandatory = true, ParameterSetName = StreamParameterSet, Position = 0)]
        public Stream InputStream { get; set; }

        /// <summary>
        /// BeginProcessing() override
        /// This is for hash function init
        /// </summary>
        protected override void BeginProcessing()
        {
            InitHasher(Algorithm);

            if (ParameterSetName == StreamParameterSet)
            {
                byte[] bytehash = null;
                String hash = null;

                bytehash = hasher.ComputeHash(InputStream);

                hash = BitConverter.ToString(bytehash).Replace("-","");
                WriteHashResult(Algorithm, hash, "");

                return;
            }

        }

        /// <summary>
        /// ProcessRecord() override
        /// This is for paths collecting from pipe
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

                        // Unlike 'GetResolvedProviderPathFromPSPath'
                        // 'GetUnresolvedProviderPathFromPSPath' does not check a file existence
                        // so do that explicity
                        if (File.Exists(newPath))
                        {
                            pathsToProcess.Add(newPath);
                        }
                        else
                        {
                            ItemNotFoundException pathNotFound =
                                new ItemNotFoundException(
                                    newPath,
                                    "PathNotFound",
                                    SessionStateStrings.PathNotFound);
                            ErrorRecord errorRecord = new ErrorRecord(pathNotFound,
                                "FileNotFound",
                                ErrorCategory.ObjectNotFound,
                                path);
                            WriteError(errorRecord);
                        }
                    }
                    break;
            }

            foreach (string path in pathsToProcess)
            {
                byte[] bytehash = null;
                String hash = null;

                try
                {
                    Stream openfilestream = File.OpenRead(path);
                    bytehash = hasher.ComputeHash(openfilestream);

                    hash = BitConverter.ToString(bytehash).Replace("-","");
                    WriteHashResult(Algorithm, hash, path);
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// Create FileHashInfo object and output it
        /// </summary>
        private void WriteHashResult(String Algorithm, String hash, String path)
        {
            FileHashInfo result = new FileHashInfo();
            result.Algorithm = Algorithm;
            result.Hash = hash;
            result.Path = path;
            WriteObject(result);
        }

        /// <summary>
        /// Parameter set names
        /// </summary>
        private const string PathParameterSet = "Path";
        private const string LiteralPathParameterSet = "LiteralPath";
        private const string StreamParameterSet = "StreamParameterSet";

    }

    /// <summary>
    /// Base Cmdlet for cmdlets which deal with crypto hashes
    /// </summary>
    public class HashCmdletBase : PSCmdlet
    {
        /// <summary>
        /// Algorithm parameter
        /// The hash algorithm name: "SHA1", "SHA256", "SHA384", "SHA512", "MD5"
        /// </summary>
        /// <value></value>
        [Parameter(Position = 1)]
        [ValidateSet(HashAlgorithmNames.SHA1,
                     HashAlgorithmNames.SHA256,
                     HashAlgorithmNames.SHA384,
                     HashAlgorithmNames.SHA512,
                     HashAlgorithmNames.MD5)]
        public String Algorithm
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

        private String _Algorithm = HashAlgorithmNames.SHA256;

        /// <summary>
        /// Hash algorithm is used
        /// </summary>
        protected HashAlgorithm hasher;

        /// <summary>
        /// Hash algorithm names
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
        /// Init a hash algorithm
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
                Exception exc = new NotSupportedException(UtilityResources.AlgorithmTypeNotSupported);
                ThrowTerminatingError(new ErrorRecord(exc, "AlgorithmTypeNotSupported", ErrorCategory.NotImplemented, null));
            }
        }
    }

    /// <summary>
    /// FileHashInfo class contains information about a file hash
    /// </summary>
     public class FileHashInfo
     {
        /// <summary>
        /// Hash algorithm name
        /// </summary>
        public string Algorithm { get; set;}

        /// <summary>
        /// Hash value
        /// </summary>
        public string Hash { get; set;}

        /// <summary>
        /// File path
        /// </summary>
        public string Path { get; set;}
     }
}
