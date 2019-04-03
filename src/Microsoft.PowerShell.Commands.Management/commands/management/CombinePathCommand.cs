// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Collections.ObjectModel;
using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command that adds the parent and child parts of a path together
    /// with the appropriate path separator.
    /// </summary>
    [Cmdlet(VerbsCommon.Join, "Path", SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113347")]
    [OutputType(typeof(string))]
    public class JoinPathCommand : CoreCommandWithCredentialsBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the path parameter to the command.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath")]
        public string[] Path { get; set; }

        /// <summary>
        /// Gets or sets the childPath parameter to the command.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        [AllowEmptyString]
        public string ChildPath { get; set; }

        /// <summary>
        /// Gets or sets additional childPaths to the command.
        /// </summary>
        [Parameter(Position = 2, Mandatory = false, ValueFromPipelineByPropertyName = true, ValueFromRemainingArguments = true)]
        [AllowNull]
        [AllowEmptyString]
        [AllowEmptyCollection]
        public string[] AdditionalChildPath { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Determines if the path should be resolved after being joined.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter Resolve { get; set; }

        #endregion Parameters

        #region Command code

        /// <summary>
        /// Parses the specified path and returns the portion determined by the
        /// boolean parameters.
        /// </summary>
        protected override void ProcessRecord()
        {
            Dbg.Diagnostics.Assert(
                Path != null,
                "Since Path is a mandatory parameter, paths should never be null");

            string combinedChildPath = ChildPath;

            // join the ChildPath elements
            if (AdditionalChildPath != null)
            {
                foreach (string childPath in AdditionalChildPath)
                {
                    combinedChildPath = SessionState.Path.Combine(combinedChildPath, childPath, CmdletProviderContext);
                }
            }

            foreach (string path in Path)
            {
                // First join the path elements

                string joinedPath = null;

                try
                {
                    joinedPath =
                        SessionState.Path.Combine(path, combinedChildPath, CmdletProviderContext);
                }
                catch (PSNotSupportedException notSupported)
                {
                    WriteError(
                        new ErrorRecord(
                            notSupported.ErrorRecord,
                            notSupported));
                    continue;
                }
                catch (DriveNotFoundException driveNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            driveNotFound.ErrorRecord,
                            driveNotFound));
                    continue;
                }
                catch (ProviderNotFoundException providerNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            providerNotFound.ErrorRecord,
                            providerNotFound));
                    continue;
                }
                catch (ItemNotFoundException pathNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            pathNotFound.ErrorRecord,
                            pathNotFound));
                    continue;
                }

                if (Resolve)
                {
                    // Resolve the paths. The default API (GetResolvedPSPathFromPSPath)
                    // does not allow non-existing paths.
                    Collection<PathInfo> resolvedPaths = null;
                    try
                    {
                        resolvedPaths =
                            SessionState.Path.GetResolvedPSPathFromPSPath(joinedPath, CmdletProviderContext);
                    }
                    catch (PSNotSupportedException notSupported)
                    {
                        WriteError(
                            new ErrorRecord(
                                notSupported.ErrorRecord,
                                notSupported));
                        continue;
                    }
                    catch (DriveNotFoundException driveNotFound)
                    {
                        WriteError(
                            new ErrorRecord(
                                driveNotFound.ErrorRecord,
                                driveNotFound));
                        continue;
                    }
                    catch (ProviderNotFoundException providerNotFound)
                    {
                        WriteError(
                            new ErrorRecord(
                                providerNotFound.ErrorRecord,
                                providerNotFound));
                        continue;
                    }
                    catch (ItemNotFoundException pathNotFound)
                    {
                        WriteError(
                            new ErrorRecord(
                                pathNotFound.ErrorRecord,
                                pathNotFound));
                        continue;
                    }

                    for (int index = 0; index < resolvedPaths.Count; ++index)
                    {
                        try
                        {
                            if (resolvedPaths[index] != null)
                            {
                                WriteObject(resolvedPaths[index].Path);
                            }
                        }
                        catch (PSNotSupportedException notSupported)
                        {
                            WriteError(
                                new ErrorRecord(
                                    notSupported.ErrorRecord,
                                    notSupported));
                            continue;
                        }
                        catch (DriveNotFoundException driveNotFound)
                        {
                            WriteError(
                                new ErrorRecord(
                                    driveNotFound.ErrorRecord,
                                    driveNotFound));
                            continue;
                        }
                        catch (ProviderNotFoundException providerNotFound)
                        {
                            WriteError(
                                new ErrorRecord(
                                    providerNotFound.ErrorRecord,
                                    providerNotFound));
                            continue;
                        }
                        catch (ItemNotFoundException pathNotFound)
                        {
                            WriteError(
                                new ErrorRecord(
                                    pathNotFound.ErrorRecord,
                                    pathNotFound));
                            continue;
                        }
                    }
                }
                else
                {
                    if (joinedPath != null)
                    {
                        WriteObject(joinedPath);
                    }
                }
            }
        }
        #endregion Command code

    }
}

