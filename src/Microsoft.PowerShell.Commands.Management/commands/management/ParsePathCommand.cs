/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Management.Automation;
using System.Management.Automation.Internal;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to resolve MSH paths containing glob characters to
    /// MSH paths that match the glob strings.
    /// </summary>
    [Cmdlet("Split", "Path", DefaultParameterSetName = "ParentSet", SupportsTransactions = true, HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113404")]
    [OutputType(typeof(string), ParameterSetName = new string[] { SplitPathCommand.leafSet,
                                                                  SplitPathCommand.noQualifierSet,
                                                                  SplitPathCommand.parentSet,
                                                                  SplitPathCommand.qualifierSet,
                                                                  SplitPathCommand.literalPathSet})]
    [OutputType(typeof(bool), ParameterSetName = new string[] { SplitPathCommand.isAbsoluteSet })]
    public class SplitPathCommand : CoreCommandWithCredentialsBase
    {
        #region Parameters

        /// <summary>
        /// The parameter set name to get the parent path
        /// </summary>
        private const string parentSet = "ParentSet";

        /// <summary>
        /// The parameter set name to get the leaf name
        /// </summary>
        private const string leafSet = "LeafSet";

        /// <summary>
        /// The parameter set name to get the qualifier set.
        /// </summary>
        private const string qualifierSet = "QualifierSet";

        /// <summary>
        /// The parameter set name to get the noqualifier set.
        /// </summary>
        private const string noQualifierSet = "NoQualifierSet";

        /// <summary>
        /// The parameter set name to get the IsAbsolute set.
        /// </summary>
        private const string isAbsoluteSet = "IsAbsoluteSet";

        /// <summary>
        /// The parameter set name to get the LiteralPath set.
        /// </summary>
        private const string literalPathSet = "LiteralPathSet";

        /// <summary>
        /// Gets or sets the path parameter to the command
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = parentSet, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = leafSet, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = qualifierSet, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = noQualifierSet, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = isAbsoluteSet, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get
            {
                return _paths;
            } // get

            set
            {
                _paths = value;
            } // set
        } // Path

        /// <summary>
        /// Gets or sets the literal path parameter to the command
        /// </summary>
        [Parameter(ParameterSetName = "LiteralPathSet", Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath")]
        public string[] LiteralPath
        {
            get
            {
                return _paths;
            } // get

            set
            {
                base.SuppressWildcardExpansion = true;
                _paths = value;
            } // set
        } // LiteralPath

        /// <summary>
        /// Determines if the qualifier should be returned
        /// </summary>
        ///
        /// <value>
        /// If true the qualifier of the path will be returned.
        /// The qualifier is the drive or provider that is qualifing
        /// the MSH path.
        /// </value>
        ///
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = qualifierSet, Mandatory = false)]
        public SwitchParameter Qualifier
        {
            get
            {
                return _qualifier;
            } // get

            set
            {
                _qualifier = value;
            } //set
        } // Qualifier

        /// <summary>
        /// Determines if the qualifier should be returned
        /// </summary>
        ///
        /// <value>
        /// If true the qualifier of the path will be returned.
        /// The qualifier is the drive or provider that is qualifing
        /// the MSH path.
        /// </value>
        ///
        [Parameter(ParameterSetName = noQualifierSet, Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter NoQualifier
        {
            get
            {
                return _noqualifier;
            } // get

            set
            {
                _noqualifier = value;
            } //set
        } // NoQualifier


        /// <summary>
        /// Determines if the parent path should be returned
        /// </summary>
        ///
        /// <value>
        /// If true the parent of the path will be returned.
        /// </value>
        ///
        [Parameter(ParameterSetName = parentSet, Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Parent
        {
            get
            {
                return _parent;
            } // get

            set
            {
                _parent = value;
            } //set
        } // Parent

        /// <summary>
        /// Determines if the leaf name should be returned
        /// </summary>
        ///
        /// <value>
        /// If true the leaf name of the path will be returned.
        /// </value>
        ///
        [Parameter(ParameterSetName = leafSet, Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Leaf
        {
            get
            {
                return _leaf;
            } // get

            set
            {
                _leaf = value;
            } //set
        } // Leaf

        /// <summary>
        /// Determines if the path should be resolved before being parsed.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter Resolve
        {
            get
            {
                return _resolve;
            } // get

            set
            {
                _resolve = value;
            } //set
        } // Resolve

        /// <summary>
        /// Determines if the path is an absolute path.
        /// </summary>
        /// 
        [Parameter(ParameterSetName = isAbsoluteSet)]
        public SwitchParameter IsAbsolute
        {
            get
            {
                return _isAbsolute;
            } // get

            set
            {
                _isAbsolute = value;
            } //set
        }
        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The path to resolve
        /// </summary>
        private string[] _paths;

        /// <summary>
        /// Determines if the qualifier of the path should be returned.
        /// The qualifier is either the drive name or provider name that
        /// is qualifying the path.
        /// </summary>
        private bool _qualifier;

        /// <summary>
        /// Determines if the qualifier of the path should be returned.
        /// If false, the qualifier will be returned. If true, it will
        /// be stripped from the path.
        /// The qualifier is either the drive name or provider name that
        /// is qualifying the path.
        /// </summary>
        private bool _noqualifier;

        /// <summary>
        /// Determines if the parent path of the specified path should be returned.
        /// </summary>
        private bool _parent = true;

        /// <summary>
        /// Determines if the leaf name of the specified path should be returned.
        /// </summary>
        private bool _leaf;

        /// <summary>
        /// Determines if the path(s) should be resolved before being parsed.
        /// </summary>
        private bool _resolve;

        /// <summary>
        /// Determines if the path(s) are absolute paths.
        /// </summary>
        private bool _isAbsolute;

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Parses the specified path and returns the portion determined by the 
        /// boolean parameters.
        /// </summary>
        protected override void ProcessRecord()
        {
            StringCollection pathsToParse = new StringCollection();

            if (_resolve)
            {
                CmdletProviderContext currentContext = CmdletProviderContext;

                foreach (string path in _paths)
                {
                    // resolve the paths and then parse each one.

                    Collection<PathInfo> resolvedPaths = null;

                    try
                    {
                        resolvedPaths =
                            SessionState.Path.GetResolvedPSPathFromPSPath(path, currentContext);
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

                    foreach (PathInfo resolvedPath in resolvedPaths)
                    {
                        try
                        {
                            if (InvokeProvider.Item.Exists(resolvedPath.Path, currentContext))
                            {
                                pathsToParse.Add(resolvedPath.Path);
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
            }
            else
            {
                pathsToParse.AddRange(Path);
            }

            // Now parse each path

            for (int index = 0; index < pathsToParse.Count; ++index)
            {
                string result = null;

                switch (ParameterSetName)
                {
                    case isAbsoluteSet:
                        string ignored = null;
                        bool isPathAbsolute =
                            SessionState.Path.IsPSAbsolute(pathsToParse[index], out ignored);

                        WriteObject(isPathAbsolute);
                        continue;

                    case qualifierSet:
                        int separatorIndex = pathsToParse[index].IndexOf(":", StringComparison.CurrentCulture);

                        if (separatorIndex < 0)
                        {
                            FormatException e =
                                new FormatException(
                                    StringUtil.Format(NavigationResources.ParsePathFormatError, pathsToParse[index]));
                            WriteError(
                                new ErrorRecord(
                                    e,
                                    "ParsePathFormatError", // RENAME
                                    ErrorCategory.InvalidArgument,
                                    pathsToParse[index]));
                            continue;
                        }
                        else
                        {
                            // Check to see if it is provider or drive qualified

                            if (SessionState.Path.IsProviderQualified(pathsToParse[index]))
                            {
                                // The plus 2 is for the length of the provider separator
                                // which is "::"

                                result =
                                    pathsToParse[index].Substring(
                                        0,
                                        separatorIndex + 2);
                            }
                            else
                            {
                                result =
                                    pathsToParse[index].Substring(
                                        0,
                                        separatorIndex + 1);
                            }
                        }
                        break;

                    case parentSet:
                    case literalPathSet:
                        bool pathStartsWithRoot =
                            pathsToParse[index].StartsWith("\\", StringComparison.CurrentCulture) ||
                            pathsToParse[index].StartsWith("/", StringComparison.CurrentCulture);

                        try
                        {
                            result =
                                SessionState.Path.ParseParent(
                                    pathsToParse[index],
                                    String.Empty,
                                    CmdletProviderContext,
                                    true);
                        }
                        catch (PSNotSupportedException)
                        {
                            // Since getting the parent path is not supported,
                            // the provider must be a container, item, or drive
                            // provider.  Since the paths for these types of
                            // providers can't be split, asking for the parent
                            // is asking for an empty string.
                            result = String.Empty;
                        }

                        break;

                    case leafSet:
                        try
                        {
                            result =
                                SessionState.Path.ParseChildName(
                                    pathsToParse[index],
                                    CmdletProviderContext,
                                    true);
                        }
                        catch (PSNotSupportedException)
                        {
                            // Since getting the leaf part of a path is not supported,
                            // the provider must be a container, item, or drive
                            // provider.  Since the paths for these types of
                            // providers can't be split, asking for the leaf
                            // is asking for the specified path back.
                            result = pathsToParse[index];
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

                        break;

                    case noQualifierSet:
                        result = RemoveQualifier(pathsToParse[index]);
                        break;

                    default:
                        Dbg.Diagnostics.Assert(
                            false,
                            "Only a known parameter set should be called");
                        break;
                } // switch

                if (result != null)
                {
                    WriteObject(result);
                }
            } // for each path
        } // ProcessRecord
        #endregion Command code

        /// <summary>
        /// Removes either the drive or provider qualifier or both from the path.
        /// </summary>
        /// 
        /// <param name="path">
        /// The path to strip the provider qualifier from.
        /// </param>
        /// 
        /// <returns>
        /// The path without the qualifier.
        /// </returns>
        /// 
        private string RemoveQualifier(string path)
        {
            Dbg.Diagnostics.Assert(
                path != null,
                "Path should be verified by the caller");

            string result = path;

            if (SessionState.Path.IsProviderQualified(path))
            {
                int index = path.IndexOf("::", StringComparison.CurrentCulture);

                if (index != -1)
                {
                    // remove the qualifier
                    result = path.Substring(index + 2);
                }
            }
            else
            {
                string driveName = String.Empty;

                if (SessionState.Path.IsPSAbsolute(path, out driveName))
                {
                    var driveNameLength = driveName.Length;
                    if (path.Length > (driveNameLength + 1) && path[driveNameLength] == ':')
                    {
                        // Remove the drive name and colon
                        result = path.Substring(driveNameLength + 1);
                    }
                }
            }

            return result;
        } // RemoveQualifier
    } // SplitPathCommand
} // namespace Microsoft.PowerShell.Commands

