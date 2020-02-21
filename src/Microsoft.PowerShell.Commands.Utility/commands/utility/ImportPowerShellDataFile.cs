// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Import-PowerShellDataFile command.
    /// </summary>
    [Cmdlet(VerbsData.Import, "PowerShellDataFile", DefaultParameterSetName = "ByPath",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=623621", RemotingCapability = RemotingCapability.None)]
    public class ImportPowerShellDataFileCommand : PSCmdlet
    {
        private bool _isLiteralPath;

        /// <summary>
        /// Path specified, using globbing to resolve.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByPath")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Path { get; set; }

        /// <summary>
        /// Specifies a path to one or more locations, without globbing.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByLiteralPath", ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("PSPath", "LP")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] LiteralPath
        {
            get { return _isLiteralPath ? Path : null; }

            set { _isLiteralPath = true; Path = value; }
        }

        /// <summary>
        /// For each path, resolve it, parse it and write all hashtables to the output stream.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (var path in Path)
            {
                var resolved = PathUtils.ResolveFilePath(path, this, _isLiteralPath);
                if (!string.IsNullOrEmpty(resolved) && System.IO.File.Exists(resolved))
                {
                    var ast = Parser.ParseFile(resolved, out Token[] tokens, out ParseError[] errors);
                    if (errors.Length > 0)
                    {
                        WriteInvalidDataFileError(resolved, "CouldNotParseAsPowerShellDataFile");
                    }
                    else
                    {
                        var data = ast.Find(a => a is HashtableAst, false);
                        if (data != null)
                        {
                            WriteObject(data.SafeGetValue());
                        }
                        else
                        {
                            WriteInvalidDataFileError(resolved, "CouldNotParseAsPowerShellDataFileNoHashtableRoot");
                        }
                    }
                }
                else
                {
                    WritePathNotFoundError(path);
                }
            }
        }

        private void WritePathNotFoundError(string path)
        {
            var errorId = "PathNotFound";
            var errorCategory = ErrorCategory.InvalidArgument;
            var errorMessage = string.Format(UtilityCommonStrings.PathDoesNotExist, path);
            var exception = new ArgumentException(errorMessage);
            var errorRecord = new ErrorRecord(exception, errorId, errorCategory, path);
            WriteError(errorRecord);
        }

        private void WriteInvalidDataFileError(string resolvedPath, string errorId)
        {
            var errorCategory = ErrorCategory.InvalidData;
            var errorMessage = string.Format(UtilityCommonStrings.CouldNotParseAsPowerShellDataFile, resolvedPath);
            var exception = new InvalidOperationException(errorMessage);
            var errorRecord = new ErrorRecord(exception, errorId, errorCategory, resolvedPath);
            WriteError(errorRecord);
        }
    }
}
