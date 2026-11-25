// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
#if UNIX
using System.Globalization;
using System.Management.Automation;
using System.Runtime.InteropServices;
#else
using System.Management.Automation;
using System.Management.Automation.Internal;
#endif

#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Removes the Zone.Identifier stream from a file.</summary>
    [Cmdlet(VerbsSecurity.Unblock, "File", DefaultParameterSetName = "ByPath", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097033")]
    public sealed class UnblockFileCommand : PSCmdlet
    {
#if UNIX
        private const string MacBlockAttribute = "com.apple.quarantine";
        private const int RemovexattrFollowSymLink = 0;
#endif

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
            List<string> pathsToProcess = new();
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
                            ErrorRecord errorRecord = new(e,
                                "FileNotFound",
                                ErrorCategory.ObjectNotFound,
                                path);
                            WriteError(errorRecord);
                        }
                    }
                }
            }
#if !UNIX

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
                        WriteError(new ErrorRecord(exception: e, errorId: "RemoveItemUnableToAccessFile", ErrorCategory.ResourceUnavailable, targetObject: path));
                    }
                }
            }
#else
            if (Platform.IsLinux)
            {
                string errorMessage = UnblockFileStrings.LinuxNotSupported;
                Exception e = new PlatformNotSupportedException(errorMessage);
                ThrowTerminatingError(new ErrorRecord(exception: e, errorId: "LinuxNotSupported", ErrorCategory.NotImplemented, targetObject: null));
                return;
            }

            foreach (string path in pathsToProcess)
            {
                if (IsBlocked(path))
                {
                    uint result = RemoveXattr(path, MacBlockAttribute, RemovexattrFollowSymLink);
                    if (result != 0)
                    {
                        string errorMessage = string.Format(CultureInfo.CurrentUICulture, UnblockFileStrings.UnblockError, path);
                        Exception e = new InvalidOperationException(errorMessage);
                        WriteError(new ErrorRecord(exception: e, errorId: "UnblockError", ErrorCategory.InvalidResult, targetObject: path));
                    }
                }
            }

#endif
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
                    ErrorRecord errorRecord = new(
                        new System.IO.FileNotFoundException(resolvedpath),
                        "FileNotFound",
                        ErrorCategory.ObjectNotFound,
                        resolvedpath);
                    WriteError(errorRecord);
                }
                else
                {
                    isValidUnblockableFile = true;
                }
            }

            return isValidUnblockableFile;
        }

#if UNIX
        private static bool IsBlocked(string path)
        {
            const uint valueSize = 1024;
            IntPtr value = Marshal.AllocHGlobal((int)valueSize);
            try
            {
                var resultSize = GetXattr(path, MacBlockAttribute, value, valueSize, 0, RemovexattrFollowSymLink);
                return resultSize != -1;
            }
            finally
            {
                Marshal.FreeHGlobal(value);
            }
        }

        // Ansi means UTF8 on Unix
        // https://developer.apple.com/library/archive/documentation/System/Conceptual/ManPages_iPhoneOS/man2/RemoveXattr.2.html
        [DllImport("libc", SetLastError = true, EntryPoint = "removexattr", CharSet = CharSet.Ansi)]
        private static extern uint RemoveXattr(string path, string name, int options);

        [DllImport("libc", EntryPoint = "getxattr", CharSet = CharSet.Ansi)]
        private static extern long GetXattr(
            [MarshalAs(UnmanagedType.LPStr)] string path,
            [MarshalAs(UnmanagedType.LPStr)] string name,
            IntPtr value,
            ulong size,
            uint position,
            int options);
#endif
    }
}
