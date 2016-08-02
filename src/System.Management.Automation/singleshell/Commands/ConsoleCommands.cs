/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Internal;
using Dbg = System.Management.Automation.Diagnostics;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace Microsoft.PowerShell.Commands
{
    #region ConsoleCmdletsBase
    /// <summary>
    /// Base class for all the Console related cmdlets.
    /// </summary>
    public abstract class ConsoleCmdletsBase : PSCmdlet
    {
        /// <summary>
        /// Runspace configuration for the current engine
        /// </summary>
        /// <remarks>
        /// Console cmdlets need <see cref="RunspaceConfigForSingleShell"/> object to work with.
        /// </remarks>
        internal RunspaceConfigForSingleShell Runspace
        {
            get
            {
                RunspaceConfigForSingleShell runSpace = Context.RunspaceConfiguration as RunspaceConfigForSingleShell;
                return runSpace;
            }
        }

        /// <summary>
        /// InitialSessionState for the current engine
        /// </summary>
        internal InitialSessionState InitialSessionState
        {
            get
            {
                return Context.InitialSessionState;
            }
        }

        /// <summary>
        /// Throws a terminating error.
        /// </summary>
        /// <param name="targetObject">Object which caused this exception.</param>
        /// <param name="errorId">ErrorId for this error.</param>
        /// <param name="innerException">Complete exception object.</param>
        /// <param name="category">ErrorCategory for this exception.</param>
        internal void ThrowError(
            Object targetObject,
            string errorId,
            Exception innerException,
            ErrorCategory category)
        {
            ThrowTerminatingError(new ErrorRecord(innerException, errorId, category, targetObject));
        }
    }
    #endregion

    #region export-console

    /// <summary>
    /// Class that implements export-console cmdlet.
    /// </summary>
    [Cmdlet(VerbsData.Export, "Console", SupportsShouldProcess = true, HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113298")]
    public sealed class ExportConsoleCommand : ConsoleCmdletsBase
    {
        #region Parameters

        /// <summary>
        /// Property that gets/sets console file name.
        /// </summary>
        /// <remarks>
        /// If a parameter is not supplied then the file represented by $console 
        /// will be used for saving.
        /// </remarks>
        [Parameter(Position = 0, Mandatory = false, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath")]
        public string Path
        {
            get
            {
                return _fileName;
            }

            set
            {
                _fileName = value;
            }
        }
        private string _fileName;

        /// <summary>
        /// Property that sets force parameter.  This will reset the read-only
        /// attribute on an existing file before deleting it.
        /// </summary>
        [Parameter()]
        public SwitchParameter Force
        {
            get
            {
                return _force;
            }
            set
            {
                _force = value;
            }
        }
        private bool _force;

        /// <summary>
        /// Property that prevents file overwrite.
        /// </summary>
        [Parameter()]
        [Alias("NoOverwrite")]
        public SwitchParameter NoClobber
        {
            get
            {
                return _noclobber;
            }
            set
            {
                _noclobber = value;
            }
        }
        private bool _noclobber;

        #endregion

        #region Overrides

        /// <summary>
        /// Saves the current console info into a file.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Get filename..
            string file = GetFileName();

            // if file is null or empty..prompt the user for filename
            if (string.IsNullOrEmpty(file))
            {
                file = PromptUserForFile();
            }

            // if file is still empty..write error and back out..
            if (string.IsNullOrEmpty(file))
            {
                PSArgumentException ae = PSTraceSource.NewArgumentException("file", ConsoleInfoErrorStrings.FileNameNotResolved);
                ThrowError(file, "FileNameNotResolved", ae, ErrorCategory.InvalidArgument);
            }

            if (WildcardPattern.ContainsWildcardCharacters(file))
            {
                ThrowError(file, "WildCardNotSupported",
                    PSTraceSource.NewInvalidOperationException(ConsoleInfoErrorStrings.ConsoleFileWildCardsNotSupported,
                    file), ErrorCategory.InvalidOperation);
            }

            // Ofcourse, you cant write to a file from HKLM: etc..
            string resolvedPath = ResolveProviderAndPath(file);

            // If resolvedPath is empty just return..
            if (string.IsNullOrEmpty(resolvedPath))
            {
                return;
            }

            // Check whether the file ends with valid extension
            if (!resolvedPath.EndsWith(StringLiterals.PowerShellConsoleFileExtension,
                StringComparison.OrdinalIgnoreCase))
            {
                // file does not end with proper extension..create one..
                resolvedPath = resolvedPath + StringLiterals.PowerShellConsoleFileExtension;
            }

            if (!ShouldProcess(this.Path)) // should this be resolvedPath?
                return;

            //check if destination file exists. 
            if (File.Exists(resolvedPath))
            {
                if (NoClobber)
                {
                    string message = StringUtil.Format(
                        ConsoleInfoErrorStrings.FileExistsNoClobber,
                        resolvedPath,
                        "NoClobber"); // prevents localization
                    Exception uae = new UnauthorizedAccessException(message);
                    ErrorRecord errorRecord = new ErrorRecord(
                        uae, "NoClobber", ErrorCategory.ResourceExists, resolvedPath);
                    // NOTE: this call will throw
                    ThrowTerminatingError(errorRecord);
                }
                // Check if the file is read-only
                System.IO.FileAttributes attrib = System.IO.File.GetAttributes(resolvedPath);
                if ((attrib & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                {
                    if (Force)
                    {
                        RemoveFileThrowIfError(resolvedPath);
                        // Note, we do not attempt to set read-only on the new file
                    }
                    else
                    {
                        ThrowError(file, "ConsoleFileReadOnly",
                            PSTraceSource.NewArgumentException(file, ConsoleInfoErrorStrings.ConsoleFileReadOnly, resolvedPath),
                            ErrorCategory.InvalidArgument);
                    }
                }
            }

            try
            {
                if (this.Runspace != null)
                {
                    this.Runspace.SaveAsConsoleFile(resolvedPath);
                }
                else if (InitialSessionState != null)
                {
                    this.InitialSessionState.SaveAsConsoleFile(resolvedPath);
                }
                else
                {
                    Dbg.Assert(false, "Both RunspaceConfiguration and InitialSessionState should not be null");
                    throw PSTraceSource.NewInvalidOperationException(ConsoleInfoErrorStrings.CmdletNotAvailable);
                }
            }
            catch (PSArgumentException mae)
            {
                ThrowError(resolvedPath,
                    "PathNotAbsolute", mae, ErrorCategory.InvalidArgument);
            }
            catch (PSArgumentNullException mane)
            {
                ThrowError(resolvedPath,
                    "PathNull", mane, ErrorCategory.InvalidArgument);
            }
            catch (ArgumentException ae)
            {
                ThrowError(resolvedPath,
                    "InvalidCharacetersInPath", ae, ErrorCategory.InvalidArgument);
            }

            // looks like saving succeeded.
            // Now try changing $console
            Exception e = null;
            try
            {
                //Update $Console variable
                Context.EngineSessionState.SetConsoleVariable();
            }
            catch (ArgumentNullException ane)
            {
                e = ane;
            }
            catch (ArgumentOutOfRangeException aor)
            {
                e = aor;
            }
            catch (ArgumentException ae)
            {
                e = ae;
            }
            catch (SessionStateUnauthorizedAccessException sue)
            {
                e = sue;
            }
            catch (SessionStateOverflowException sof)
            {
                e = sof;
            }
            catch (ProviderNotFoundException pnf)
            {
                e = pnf;
            }
            catch (System.Management.Automation.DriveNotFoundException dnfe)
            {
                e = dnfe;
            }
            catch (NotSupportedException ne)
            {
                e = ne;
            }
            catch (ProviderInvocationException pin)
            {
                e = pin;
            }

            if (e != null)
            {
                throw PSTraceSource.NewInvalidOperationException(e,
                        ConsoleInfoErrorStrings.ConsoleVariableCannotBeSet, resolvedPath);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Removes file specified by destination
        /// </summary>
        /// <param name="destination">Absolute path of the file to be removed.</param>
        private void RemoveFileThrowIfError(string destination)
        {
            Diagnostics.Assert(System.IO.Path.IsPathRooted(destination),
                "RemoveFile expects an absolute path");

            System.IO.FileInfo destfile = new System.IO.FileInfo(destination);
            if (destfile != null)
            {
                Exception e = null;
                try
                {
                    //Make sure the file is not read only
                    destfile.Attributes = destfile.Attributes & ~(FileAttributes.ReadOnly | FileAttributes.Hidden);
                    destfile.Delete();
                }
                catch (FileNotFoundException fnf)
                {
                    e = fnf;
                }
                catch (DirectoryNotFoundException dnf)
                {
                    e = dnf;
                }
                catch (UnauthorizedAccessException uac)
                {
                    e = uac;
                }
                catch (System.Security.SecurityException se)
                {
                    e = se;
                }
                catch (ArgumentNullException ane)
                {
                    e = ane;
                }
                catch (ArgumentException ae)
                {
                    e = ae;
                }
                catch (PathTooLongException pe)
                {
                    e = pe;
                }
                catch (NotSupportedException ns)
                {
                    e = ns;
                }
                catch (IOException ioe)
                {
                    e = ioe;
                }

                if (e != null)
                {
                    throw PSTraceSource.NewInvalidOperationException(e,
                         ConsoleInfoErrorStrings.ExportConsoleCannotDeleteFile,
                         destfile);
                }
            }
        }

        /// <summary>
        /// Resolves the specified path and verifies the path belongs to
        /// FileSystemProvider.
        /// </summary>
        /// <param name="path">Path to resolve</param>
        /// <returns>A fully qualified string representing filename.</returns>
        private string ResolveProviderAndPath(string path)
        {
            // Construct cmdletprovidercontext
            CmdletProviderContext cmdContext = new CmdletProviderContext(this);
            // First resolve path
            PathInfo resolvedPath = ResolvePath(path, true, cmdContext);

            // Check whether this is FileSystemProvider..
            if (resolvedPath != null)
            {
                if (resolvedPath.Provider.ImplementingType == typeof(FileSystemProvider))
                {
                    return resolvedPath.Path;
                }

                throw PSTraceSource.NewInvalidOperationException(ConsoleInfoErrorStrings.ProviderNotSupported, resolvedPath.Provider.Name);
            }

            return null;
        }

        /// <summary>
        /// Resolves the specified path to PathInfo objects
        /// </summary>
        /// 
        /// <param name="pathToResolve">
        /// The path to be resolved. Each path may contain glob characters.
        /// </param>
        /// 
        /// <param name="allowNonexistingPaths">
        /// If true, resolves the path even if it doesn't exist.
        /// </param>
        /// 
        /// <param name="currentCommandContext">
        /// The context under which the command is running.
        /// </param>
        /// 
        /// <returns>
        /// A string representing the resolved path.
        /// </returns>
        /// 
        private PathInfo ResolvePath(
            string pathToResolve,
            bool allowNonexistingPaths,
            CmdletProviderContext currentCommandContext)
        {
            Collection<PathInfo> results = new Collection<PathInfo>();

            try
            {
                // First resolve path
                Collection<PathInfo> pathInfos =
                    SessionState.Path.GetResolvedPSPathFromPSPath(
                        pathToResolve,
                        currentCommandContext);

                foreach (PathInfo pathInfo in pathInfos)
                {
                    results.Add(pathInfo);
                }
            }
            catch (PSNotSupportedException notSupported)
            {
                WriteError(
                    new ErrorRecord(
                        notSupported.ErrorRecord,
                        notSupported));
            }
            catch (System.Management.Automation.DriveNotFoundException driveNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        driveNotFound.ErrorRecord,
                        driveNotFound));
            }
            catch (ProviderNotFoundException providerNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        providerNotFound.ErrorRecord,
                        providerNotFound));
            }
            catch (ItemNotFoundException pathNotFound)
            {
                if (allowNonexistingPaths)
                {
                    ProviderInfo provider = null;
                    System.Management.Automation.PSDriveInfo drive = null;
                    string unresolvedPath =
                        SessionState.Path.GetUnresolvedProviderPathFromPSPath(
                            pathToResolve,
                            currentCommandContext,
                            out provider,
                            out drive);

                    PathInfo pathInfo =
                        new PathInfo(
                            drive,
                            provider,
                            unresolvedPath,
                            SessionState);
                    results.Add(pathInfo);
                }
                else
                {
                    WriteError(
                        new ErrorRecord(
                            pathNotFound.ErrorRecord,
                            pathNotFound));
                }
            }

            if (results.Count == 1)
            {
                return results[0];
            }
            else if (results.Count > 1)
            {
                Exception e = PSTraceSource.NewNotSupportedException();
                WriteError(
                    new ErrorRecord(e,
                    "NotSupported",
                    ErrorCategory.NotImplemented,
                    results));
                return null;
            }
            else
            {
                return null;
            }
        } // ResolvePath

        /// <summary>
        /// Gets the filename for the current operation. If Name parameter is empty
        /// checks $console. If $console is not present, prompts user?
        /// </summary>
        /// <returns>
        /// A string representing filename.
        /// If filename cannot be deduced returns null.
        /// </returns>
        /// <exception cref="PSArgumentException">
        /// 1. $console points to an PSObject that cannot be converted to string.
        /// </exception>
        private string GetFileName()
        {
            if (!string.IsNullOrEmpty(_fileName))
            {
                // if user specified input..just return
                return _fileName;
            }
            // no input is specified..
            // check whether $console is set
            PSVariable consoleVariable = Context.SessionState.PSVariable.Get(SpecialVariables.ConsoleFileName);
            if (consoleVariable == null)
            {
                return string.Empty;
            }

            string consoleValue = consoleVariable.Value as string;

            if (consoleValue == null)
            {
                // $console is not in string format
                // Check whether it is in PSObject format
                PSObject consolePSObject = consoleVariable.Value as PSObject;

                if ((consolePSObject != null) && ((consolePSObject.BaseObject as string) != null))
                {
                    consoleValue = consolePSObject.BaseObject as string;
                }
            }

            if (consoleValue != null)
            {
                // ConsoleFileName variable is found..
                return consoleValue;
            }

            throw PSTraceSource.NewArgumentException("fileName", ConsoleInfoErrorStrings.ConsoleCannotbeConvertedToString);
        }

        /// <summary>
        /// Prompt user for filename.
        /// </summary>
        /// <returns>
        /// User input in string format.
        /// If user chooses not to export, an empty string is returned.
        /// </returns>
        /// <remarks>No exception is thrown</remarks>
        private string PromptUserForFile()
        {
            // ask user what to do..
            if (ShouldContinue(
                    ConsoleInfoErrorStrings.PromptForExportConsole,
                    null))
            {
                string caption = StringUtil.Format(ConsoleInfoErrorStrings.FileNameCaptionForExportConsole, "export-console");
                string message = ConsoleInfoErrorStrings.FileNamePromptMessage;

                // Construct a field description object of required parameters
                Collection<System.Management.Automation.Host.FieldDescription> desc = new Collection<System.Management.Automation.Host.FieldDescription>();
                desc.Add(new System.Management.Automation.Host.FieldDescription("Name"));

                // get user input from the host
                System.Collections.Generic.Dictionary<string, PSObject> returnValue =
                    this.PSHostInternal.UI.Prompt(caption, message, desc);

                if ((returnValue != null) && (returnValue["Name"] != null))
                {
                    return (returnValue["Name"].BaseObject as string);
                }

                // We dont have any input..
                return string.Empty;
            }

            // If user chooses not to export, return empty string.
            return string.Empty;
        }

        #endregion
    }

    #endregion
}

