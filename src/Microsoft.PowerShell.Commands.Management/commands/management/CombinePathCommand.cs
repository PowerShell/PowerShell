/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command that adds the parent and child parts of a path together
    /// with the appropriate path separator.
    /// </summary>
    [Cmdlet("Join", "Path", SupportsTransactions = true, HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113347")]
    [OutputType(typeof(string))]
    public class JoinPathCommand : CoreCommandWithCredentialsBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the path parameter to the command
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath")]
        public string[] Path { get; set; }

        /// <summary>
        /// Gets or sets the childPath parameter to the command
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        [AllowEmptyString]
        public string ChildPath { get; set; } = String.Empty;

        /// <summary>
        /// Determines if the path should be resolved after being joined
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

            foreach (string path in Path)
            {
                // First join the path elements

                string joinedPath = null;

                try
                {
                    joinedPath =
                        SessionState.Path.Combine(path, ChildPath, CmdletProviderContext);
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
                    } // for each path
                }
                else
                {
                    if (joinedPath != null)
                    {
                        WriteObject(joinedPath);
                    }
                }
            }
        } // ProcessRecord
        #endregion Command code


    } // JoinPathCommand
} // namespace Microsoft.PowerShell.Commands

