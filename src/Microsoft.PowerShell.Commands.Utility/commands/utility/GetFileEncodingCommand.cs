// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements the Get-FileEncoding command.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "FileEncoding", DefaultParameterSetName = PathParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=1234567")]
    [OutputType(typeof(Encoding))]
    public sealed class GetFileEncodingCommand : PSCmdlet
    {
        #region Parameter Sets

        private const string PathParameterSet = "ByPath";
        private const string LiteralPathParameterSet = "ByLiteralPath";

        #endregion

        #region Parameters

        /// <summary>
        /// Gets or sets path from from which to get encoding.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = PathParameterSet)]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets literal path from which to get encoding.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = LiteralPathParameterSet)]
        [Alias("PSPath", "LP")]
        public string LiteralPath
        {
            get
            {
                return _isLiteralPath ? Path : null;
            }

            set
            {
                Path = value;
                _isLiteralPath = true;
            }
        }

        private bool _isLiteralPath;

        #endregion

        /// <summary>
        /// Process paths to get file encoding.
        /// </summary>
        protected override void ProcessRecord()
        {
            string resolvedPath = PathUtils.ResolveFilePath(Path, this, _isLiteralPath);

            if (!File.Exists(resolvedPath))
            {
                ItemNotFoundException exception = new(Path, "PathNotFound", SessionStateStrings.PathNotFound);
                ThrowTerminatingError(exception.ErrorRecord);
            }

            WriteObject(PathUtils.GetPathEncoding(resolvedPath));
        }
    }
}
