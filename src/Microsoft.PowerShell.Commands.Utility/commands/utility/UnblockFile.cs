// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !UNIX

#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;

#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Removes the Zone.Identifier stream from a file.</summary>
    [Cmdlet(VerbsSecurity.Unblock, "File", DefaultParameterSetName = "ByPath", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=217450")]
    public sealed class UnblockFileCommand : PSCmdlet
    {
        /// <summary>
        /// The path of the file to unblock.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByPath")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
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
        /// The literal path of the file to unblock.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ByLiteralPath", ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
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
        /// Generate the type(s)
        /// </summary>
        protected override void ProcessRecord()
        {
            List<string> pathsToProcess = new List<string>();
            ProviderInfo provider = null;

            if (string.Equals(this.ParameterSetName, "ByLiteralPath", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string path in _paths)
                {
                    string newPath = Context.SessionState.Path.GetUnresolvedProviderPathFromPSPath(path);

                    if (IsValidFileForUnblocking(newPath))
                    {
                        pathsToProcess.Add(newPath);
                    }
                }
            }
            else
            {
                // Resolve paths
                foreach (string path in _paths)
                {
                    try
                    {
                        Collection<string> newPaths = Context.SessionState.Path.GetResolvedProviderPathFromPSPath(path, out provider);

                        foreach (string currentFilepath in newPaths)
                        {
                            if (IsValidFileForUnblocking(currentFilepath))
                            {
                                pathsToProcess.Add(currentFilepath);
                            }
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
            }

            // Unblock files
            foreach (string path in pathsToProcess)
            {
                if (ShouldProcess(path))
                {
                    try
                    {
                        AlternateDataStreamUtilities.DeleteFileStream(path, "Zone.Identifier");
                    }
                    catch (Exception e)
                    {
                        WriteError(new ErrorRecord(e, "RemoveItemUnableToAccessFile", ErrorCategory.ResourceUnavailable, path));
                    }
                }
            }
        }

        /// <summary>
        /// IsValidFileForUnblocking is a helper method used to validate if
        /// the supplied file path has to be considered for unblocking.
        /// </summary>
        /// <param name="resolvedpath">File or directory path.</param>
        /// <returns>True is the supplied path is a
        /// valid file path or else false is returned.
        /// If the supplied path is a directory path then false is returned.</returns>
        private bool IsValidFileForUnblocking(string resolvedpath)
        {
            bool isValidUnblockableFile = false;

            // Bug 501423 : silently ignore folders given that folders cannot have
            // alternate data streams attached to them (i.e. they're already unblocked).
            if (!System.IO.Directory.Exists(resolvedpath))
            {
                if (!System.IO.File.Exists(resolvedpath))
                {
                    ErrorRecord errorRecord = new ErrorRecord(
                        new System.IO.FileNotFoundException(resolvedpath),
                        "FileNotFound",
                        ErrorCategory.ObjectNotFound,
                        resolvedpath);
                    WriteError(errorRecord);
                }
                else
                {
                    isValidUnblockableFile = true; ;
                }
            }

            return isValidUnblockableFile;
        }
    }
}
#endif
