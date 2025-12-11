// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Provider;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    #region CoreCommandBase

    /// <summary>
    /// The base command for the core commands.
    /// </summary>
    public abstract class CoreCommandBase : PSCmdlet, IDynamicParameters
    {
        #region Tracer

        /// <summary>
        /// An instance of the PSTraceSource class used for trace output
        /// using "NavigationCommands" as the category.
        /// </summary>
        [Dbg.TraceSource("NavigationCommands", "The namespace navigation tracer")]
        internal static readonly Dbg.PSTraceSource tracer = Dbg.PSTraceSource.GetTracer("NavigationCommands", "The namespace navigation tracer");

        #endregion Tracer

        #region Protected members

        /// <summary>
        /// The context for the command that is passed to the core command providers.
        /// </summary>
        internal virtual CmdletProviderContext CmdletProviderContext
        {
            get
            {
                CmdletProviderContext coreCommandContext = new(this);

                coreCommandContext.Force = Force;

                Collection<string> includeFilter =
                    SessionStateUtilities.ConvertArrayToCollection<string>(Include);

                Collection<string> excludeFilter =
                    SessionStateUtilities.ConvertArrayToCollection<string>(Exclude);

                coreCommandContext.SetFilters(includeFilter, excludeFilter, Filter);
                coreCommandContext.SuppressWildcardExpansion = SuppressWildcardExpansion;
                coreCommandContext.DynamicParameters = RetrievedDynamicParameters;
                stopContextCollection.Add(coreCommandContext);

                return coreCommandContext;
            }
        }

        internal virtual SwitchParameter SuppressWildcardExpansion
        {
            get => _suppressWildcardExpansion;
            set => _suppressWildcardExpansion = value;
        }

        private bool _suppressWildcardExpansion;

        /// <summary>
        /// A virtual method for retrieving the dynamic parameters for a cmdlet. Derived cmdlets
        /// that require dynamic parameters should override this method and return the
        /// dynamic parameter object.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal virtual object GetDynamicParameters(CmdletProviderContext context) => null;

        /// <summary>
        /// Called by the base implementation that checks the SupportShouldProcess provider
        /// capability. This virtual method gives the
        /// derived cmdlet a chance query the CmdletProvider capabilities to determine
        /// if the provider supports ShouldProcess.
        /// </summary>
        /// <value></value>
        protected virtual bool ProviderSupportsShouldProcess => true;

        /// <summary>
        /// A helper for derived classes to call to determine if the paths specified
        /// are for a provider that supports ShouldProcess.
        /// </summary>
        /// <param name="paths">
        /// The paths to check to see if the providers support ShouldProcess.
        /// </param>
        /// <returns>
        /// If the paths are to different providers, and any don't support
        /// ShouldProcess, then the return value is false. If they all
        /// support ShouldProcess then the return value is true.
        /// </returns>
        protected bool DoesProviderSupportShouldProcess(string[] paths)
        {
            // If no paths are specified, then default to true as the paths
            // may be getting piped in.
            bool result = true;

            if (paths != null)
            {
                foreach (string path in paths)
                {
                    ProviderInfo provider = null;
                    PSDriveInfo drive = null;

                    // I don't really care about the returned path, just the provider name
                    SessionState.Path.GetUnresolvedProviderPathFromPSPath(
                        path,
                        this.CmdletProviderContext,
                        out provider,
                        out drive);

                    // Check the provider's capabilities

                    if (!CmdletProviderManagementIntrinsics.CheckProviderCapabilities(
                            ProviderCapabilities.ShouldProcess,
                            provider))
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// The dynamic parameters which have already been retrieved from the provider
        /// and bound by the command processor.
        /// </summary>
        protected internal object RetrievedDynamicParameters => _dynamicParameters;
        /// <summary>
        /// The dynamic parameters for the command. They are retrieved using the
        /// GetDynamicParameters virtual method.
        /// </summary>
        private object _dynamicParameters;

        #endregion Protected members

        #region Public members

        /// <summary>
        /// Stops the processing of the provider by using the
        /// CmdletProviderContext to tunnel the stop message to
        /// the provider instance.
        /// </summary>
        protected override void StopProcessing()
        {
            foreach (CmdletProviderContext stopContext in stopContextCollection)
            {
                stopContext.StopProcessing();
            }
        }

        internal Collection<CmdletProviderContext> stopContextCollection =
            new();

        /// <summary>
        /// Gets or sets the filter property.
        /// </summary>
        /// <remarks>
        /// This is meant to be overridden by derived classes if
        /// they support the Filter parameter. This property is on
        /// the base class to simplify the creation of the CmdletProviderContext.
        /// </remarks>
        public virtual string Filter { get; set; }

        /// <summary>
        /// Gets or sets the include property.
        /// </summary>
        /// <remarks>
        /// This is meant to be overridden by derived classes if
        /// they support the Include parameter. This property is on
        /// the base class to simplify the creation of the CmdletProviderContext.
        /// </remarks>
        public virtual string[] Include
        {
            get;
            set;
        } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the exclude property.
        /// </summary>
        /// <remarks>
        /// This is meant to be overridden by derived classes if
        /// they support the Exclude parameter. This property is on
        /// the base class to simplify the creation of the CmdletProviderContext.
        /// </remarks>
        public virtual string[] Exclude
        {
            get;
            set;
        } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the force property.
        /// </summary>
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        ///
        /// This is meant to be overridden by derived classes if
        /// they support the Force parameter. This property is on
        /// the base class to simplify the creation of the CmdletProviderContext.
        /// </remarks>
        public virtual SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        private bool _force;

        /// <summary>
        /// Retrieves the dynamic parameters for the command from
        /// the provider.
        /// </summary>
        public object GetDynamicParameters()
        {
            // Don't stream errors or Write* to the pipeline.
            CmdletProviderContext context = CmdletProviderContext;
            context.PassThru = false;

            try
            {
                _dynamicParameters = GetDynamicParameters(context);
            }
            catch (ItemNotFoundException)
            {
                _dynamicParameters = null;
            }
            catch (ProviderNotFoundException)
            {
                _dynamicParameters = null;
            }
            catch (DriveNotFoundException)
            {
                _dynamicParameters = null;
            }

            return _dynamicParameters;
        }

        /// <summary>
        /// Determines if the cmdlet and CmdletProvider supports ShouldProcess.
        /// </summary>
        public bool SupportsShouldProcess => ProviderSupportsShouldProcess;

        #endregion Public members
    }

    #endregion CoreCommandBase

    #region CoreCommandWithCredentialsBase

    /// <summary>
    /// The base class for core commands to extend when they require credentials
    /// to be passed as parameters.
    /// </summary>
    public class CoreCommandWithCredentialsBase : CoreCommandBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the credential parameter.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Credential]
        public PSCredential Credential { get; set; }

        #endregion Parameters

        #region parameter data

        #endregion parameter data

        #region Protected members

        /// <summary>
        /// The context for the command that is passed to the core command providers.
        /// </summary>
        internal override CmdletProviderContext CmdletProviderContext
        {
            get
            {
                CmdletProviderContext coreCommandContext = new(this, Credential);
                coreCommandContext.Force = Force;

                Collection<string> includeFilter =
                    SessionStateUtilities.ConvertArrayToCollection<string>(Include);

                Collection<string> excludeFilter =
                    SessionStateUtilities.ConvertArrayToCollection<string>(Exclude);

                coreCommandContext.SetFilters(includeFilter, excludeFilter, Filter);
                coreCommandContext.SuppressWildcardExpansion = SuppressWildcardExpansion;
                coreCommandContext.DynamicParameters = RetrievedDynamicParameters;
                stopContextCollection.Add(coreCommandContext);

                return coreCommandContext;
            }
        }

        #endregion Protected members
    }

    #endregion CoreCommandWithCredentialsBase

    #region GetLocationCommand

    /// <summary>
    /// The get-location command class.
    /// This command does things like list the contents of a container, get
    /// an item at a given path, get the current working directory, etc.
    /// </summary>
    /// <remarks>
    /// </remarks>
    [Cmdlet(VerbsCommon.Get, "Location", DefaultParameterSetName = LocationParameterSet, SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096495")]
    [OutputType(typeof(PathInfo), ParameterSetName = new string[] { LocationParameterSet })]
    [OutputType(typeof(PathInfoStack), ParameterSetName = new string[] { StackParameterSet })]
    public class GetLocationCommand : DriveMatchingCoreCommandBase
    {
        private const string LocationParameterSet = "Location";
        private const string StackParameterSet = "Stack";

        #region Command parameters

        #region Location parameter set parameters

        /// <summary>
        /// Gets or sets the provider from which to get the current location.
        /// </summary>
        [Parameter(ParameterSetName = LocationParameterSet, ValueFromPipelineByPropertyName = true)]
        public string[] PSProvider
        {
            get => _provider;
            set => _provider = value ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets or sets the drive from which to get the current location.
        /// </summary>
        [Parameter(ParameterSetName = LocationParameterSet, ValueFromPipelineByPropertyName = true)]
        public string[] PSDrive { get; set; }

        #endregion Location parameter set parameters

        #region Stack parameter set parameters

        /// <summary>
        /// Gets or sets the Stack switch parameter which is used
        /// to disambiguate parameter sets.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = StackParameterSet)]
        public SwitchParameter Stack
        {
            get => _stackSwitch;
            set => _stackSwitch = value;
        }

        private bool _stackSwitch;

        /// <summary>
        /// Gets or sets the stack ID for the location stack that will
        /// be retrieved.
        /// </summary>
        [Parameter(ParameterSetName = StackParameterSet, ValueFromPipelineByPropertyName = true)]
        public string[] StackName
        {
            get => _stackNames;

            set => _stackNames = value;
        }

        #endregion Stack parameter set parameters

        #endregion Command parameters

        #region command data

        #region Location parameter set data

        /// <summary>
        /// The name of the provider from which to return the current location.
        /// </summary>
        private string[] _provider = Array.Empty<string>();

        #endregion Location parameter set data

        #region Stack parameter set data

        /// <summary>
        /// The name of the location stack from which to return the stack.
        /// </summary>
        private string[] _stackNames;

        #endregion Stack parameter set data

        #endregion command data

        #region command code

        /// <summary>
        /// The main execution method for the get-location command. Depending on
        /// the parameter set that is specified, the command can do many things.
        ///     -locationSet gets the current working directory as a Monad path
        ///     -stackSet gets the directory stack of directories that have been
        ///               pushed by the push-location command.
        /// </summary>
        protected override void ProcessRecord()
        {
            // It is OK to use a switch for string comparison here because we
            // want a case sensitive comparison in the current culture.
            switch (ParameterSetName)
            {
                case LocationParameterSet:
                    PathInfo result = null;

                    if (PSDrive != null && PSDrive.Length > 0)
                    {
                        foreach (string drive in PSDrive)
                        {
                            List<PSDriveInfo> foundDrives = null;
                            try
                            {
                                foundDrives = GetMatchingDrives(drive, PSProvider, null);
                            }
                            catch (DriveNotFoundException e)
                            {
                                ErrorRecord errorRecord =
                                    new(
                                        e,
                                        "GetLocationNoMatchingDrive",
                                        ErrorCategory.ObjectNotFound,
                                        drive);
                                WriteError(errorRecord);
                                continue;
                            }
                            catch (ProviderNotFoundException e)
                            {
                                ErrorRecord errorRecord =
                                    new(
                                        e,
                                        "GetLocationNoMatchingProvider",
                                        ErrorCategory.ObjectNotFound,
                                        PSProvider);
                                WriteError(errorRecord);
                                continue;
                            }
                            catch (ArgumentException argException)
                            {
                                ErrorRecord errorRecord =
                                    new(
                                        argException,
                                        "GetLocationNoMatchingDrive",
                                        ErrorCategory.ObjectNotFound,
                                        drive);
                                WriteError(errorRecord);
                                continue;
                            }

                            // Get the current location for a specific drive and provider

                            foreach (PSDriveInfo workingDrive in foundDrives)
                            {
                                try
                                {
                                    string path =
                                        LocationGlobber.GetDriveQualifiedPath(
                                            workingDrive.CurrentLocation,
                                            workingDrive);

                                    result = new PathInfo(workingDrive, workingDrive.Provider, path, SessionState);

                                    WriteObject(result);
                                }
                                catch (ProviderNotFoundException providerNotFound)
                                {
                                    WriteError(
                                        new ErrorRecord(
                                            providerNotFound.ErrorRecord,
                                            providerNotFound));
                                    continue;
                                }
                            }
                        }
                    }
                    // If the drive wasn't specified but the provider was
                    else if ((PSDrive == null || PSDrive.Length == 0) &&
                             (PSProvider != null && PSProvider.Length > 0))
                    {
                        foreach (string providerName in PSProvider)
                        {
                            bool providerContainsWildcard = WildcardPattern.ContainsWildcardCharacters(providerName);
                            if (!providerContainsWildcard)
                            {
                                // Since the Provider was specified and doesn't contain
                                // wildcard characters, make sure it exists.

                                try
                                {
                                    SessionState.Provider.GetOne(providerName);
                                }
                                catch (ProviderNotFoundException e)
                                {
                                    ErrorRecord errorRecord =
                                        new(
                                            e,
                                            "GetLocationNoMatchingProvider",
                                            ErrorCategory.ObjectNotFound,
                                            providerName);
                                    WriteError(errorRecord);
                                    continue;
                                }
                            }

                            // Match the providers

                            foreach (ProviderInfo providerInfo in SessionState.Provider.GetAll())
                            {
                                if (providerInfo.IsMatch(providerName))
                                {
                                    try
                                    {
                                        WriteObject(SessionState.Path.CurrentProviderLocation(providerInfo.FullName));
                                    }
                                    catch (ProviderNotFoundException providerNotFound)
                                    {
                                        WriteError(
                                            new ErrorRecord(
                                                providerNotFound.ErrorRecord,
                                                providerNotFound));
                                        continue;
                                    }
                                    catch (DriveNotFoundException driveNotFound)
                                    {
                                        if (providerContainsWildcard)
                                        {
                                            // NTRAID#Windows Out Of Band Releases-923607-2005/11/02-JeffJon
                                            // This exception is ignored, because it just means we didn't find
                                            // an active drive for the provider.
                                            continue;
                                        }
                                        else
                                        {
                                            WriteError(
                                                new ErrorRecord(
                                                    driveNotFound.ErrorRecord,
                                                    driveNotFound));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Get the current working directory using the core command API.
                        WriteObject(SessionState.Path.CurrentLocation);
                    }

                    break;

                case StackParameterSet:
                    if (_stackNames != null)
                    {
                        foreach (string stackName in _stackNames)
                        {
                            try
                            {
                                // Get the directory stack. This is similar to the "dirs" command
                                WriteObject(SessionState.Path.LocationStack(stackName), false);
                            }
                            catch (PSArgumentException argException)
                            {
                                WriteError(
                                    new ErrorRecord(
                                        argException.ErrorRecord,
                                        argException));
                                continue;
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            WriteObject(SessionState.Path.LocationStack(null), false);
                        }
                        catch (PSArgumentException argException)
                        {
                            WriteError(
                                new ErrorRecord(
                                    argException.ErrorRecord,
                                    argException));
                        }
                    }

                    break;

                default:
                    Dbg.Diagnostics.Assert(false, string.Create(System.Globalization.CultureInfo.InvariantCulture, $"One of the predefined parameter sets should have been specified, instead we got: {ParameterSetName}"));
                    break;
            }
        }

        #endregion command code
    }
    #endregion GetLocationCommand

    #region SetLocationCommand

    /// <summary>
    /// The core command for setting/changing location.
    /// This is the equivalent of cd command.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "Location", DefaultParameterSetName = PathParameterSet, SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097049")]
    [OutputType(typeof(PathInfo), typeof(PathInfoStack))]
    public class SetLocationCommand : CoreCommandBase
    {
        #region Command parameters
        private const string PathParameterSet = "Path";
        private const string LiteralPathParameterSet = "LiteralPath";
        private const string StackParameterSet = "Stack";

        /// <summary>
        /// Gets or sets the path property.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = PathParameterSet,
                   ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string Path
        {
            get => _path;
            set => _path = value;
        }

        /// <summary>
        /// Gets or sets the path property, when bound from the pipeline.
        /// </summary>
        [Parameter(ParameterSetName = LiteralPathParameterSet,
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string LiteralPath
        {
            get => _path;
            set
            {
                _path = value;
                base.SuppressWildcardExpansion = true;
            }
        }

        /// <summary>
        /// Gets or sets the parameter -passThru which states output from
        /// the command should be placed in the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get => _passThrough;
            set => _passThrough = value;
        }

        /// <summary>
        /// Gets or sets the StackName parameter which determines which location stack
        /// to use for the push. If the parameter is missing or empty the default
        /// location stack is used.
        /// </summary>
        [Parameter(ParameterSetName = StackParameterSet, ValueFromPipelineByPropertyName = true)]
        public string StackName { get; set; }

        #endregion Command parameters

        #region Command data

        /// <summary>
        /// The filter used when doing a dir.
        /// </summary>
        private string _path = string.Empty;

        /// <summary>
        /// Determines if output should be passed through for
        /// set-location.
        /// </summary>
        private bool _passThrough;

        #endregion Command data

        #region Command code

        /// <summary>
        /// The functional part of the code that does the changing of the current
        /// working directory.
        /// </summary>
        protected override void ProcessRecord()
        {
            object result = null;

            switch (ParameterSetName)
            {
                case PathParameterSet:
                case LiteralPathParameterSet:
                    try
                    {
                        // Change the current working directory
                        if (string.IsNullOrEmpty(Path))
                        {
                            // If user just typed 'cd', go to FileSystem provider home directory
                            Path = SessionState.Internal.GetSingleProvider(Commands.FileSystemProvider.ProviderName).Home;
                        }

                        result = SessionState.Path.SetLocation(Path, CmdletProviderContext, ParameterSetName == LiteralPathParameterSet);
                    }
                    catch (PSNotSupportedException notSupported)
                    {
                        WriteError(
                            new ErrorRecord(
                                notSupported.ErrorRecord,
                                notSupported));
                    }
                    catch (DriveNotFoundException driveNotFound)
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
                        WriteError(
                            new ErrorRecord(
                                pathNotFound.ErrorRecord,
                                pathNotFound));
                    }
                    catch (PSArgumentException argException)
                    {
                        WriteError(
                            new ErrorRecord(
                                argException.ErrorRecord,
                                argException));
                    }

                    break;

                case StackParameterSet:

                    try
                    {
                        // Change the default location stack
                        result = SessionState.Path.SetDefaultLocationStack(StackName);
                    }
                    catch (ItemNotFoundException itemNotFound)
                    {
                        WriteError(
                            new ErrorRecord(
                                itemNotFound.ErrorRecord,
                                itemNotFound));
                    }

                    break;

                default:
                    Dbg.Diagnostics.Assert(
                        false,
                        "One of the specified parameter sets should have been called");
                    break;
            }

            if (_passThrough && result != null)
            {
                WriteObject(result);
            }
        }

        #endregion Command code
    }

    #endregion SetLocationCommand

    #region PushLocationCommand

    /// <summary>
    /// The core command for setting/changing location and pushing it onto a location stack.
    /// This is the equivalent of the pushd command.
    /// </summary>
    [Cmdlet(VerbsCommon.Push, "Location", DefaultParameterSetName = PathParameterSet, SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097105")]
    public class PushLocationCommand : CoreCommandBase
    {
        #region Command parameters
        private const string PathParameterSet = "Path";
        private const string LiteralPathParameterSet = "LiteralPath";

        /// <summary>
        /// Gets or sets the path property.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = PathParameterSet,
                   ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string Path
        {
            get => _path;
            set => _path = value;
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = LiteralPathParameterSet,
                   ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string LiteralPath
        {
            get => _path;
            set
            {
                base.SuppressWildcardExpansion = true;
                _path = value;
            }
        }

        /// <summary>
        /// Gets or sets the parameter -passThru which states output from
        /// the command should be placed in the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get => _passThrough;
            set => _passThrough = value;
        }

        /// <summary>
        /// Gets or sets the StackName parameter which determines which location stack
        /// to use for the push. If the parameter is missing or empty the default
        /// location stack is used.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string StackName
        {
            get => _stackName;
            set => _stackName = value;
        }

        #endregion Command parameters

        #region Command data

        /// <summary>
        /// The filter used when doing a dir.
        /// </summary>
        private string _path = string.Empty;

        /// <summary>
        /// Determines if output should be passed through for
        /// push-location.
        /// </summary>
        private bool _passThrough;

        /// <summary>
        /// The ID of the stack to use for the pop.
        /// </summary>
        private string _stackName;

        #endregion Command data

        #region Command code

        /// <summary>
        /// The functional part of the code that does the changing of the current
        /// working directory and pushes the container onto the stack.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Push the current working directory onto the
            // working directory stack
            SessionState.Path.PushCurrentLocation(_stackName);

            if (Path != null)
            {
                try
                {
                    // Now change the directory to the one specified
                    // in the command
                    PathInfo result = SessionState.Path.SetLocation(Path, CmdletProviderContext);

                    if (PassThru)
                    {
                        WriteObject(result);
                    }
                }
                catch (PSNotSupportedException notSupported)
                {
                    WriteError(
                        new ErrorRecord(
                            notSupported.ErrorRecord,
                            notSupported));
                    return;
                }
                catch (DriveNotFoundException driveNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            driveNotFound.ErrorRecord,
                            driveNotFound));
                    return;
                }
                catch (ProviderNotFoundException providerNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            providerNotFound.ErrorRecord,
                            providerNotFound));
                    return;
                }
                catch (ItemNotFoundException pathNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            pathNotFound.ErrorRecord,
                            pathNotFound));
                    return;
                }
                catch (PSArgumentException argException)
                {
                    WriteError(
                        new ErrorRecord(
                            argException.ErrorRecord,
                            argException));
                    return;
                }
            }
        }

        #endregion Command code
    }

    #endregion PushLocationCommand

    #region PopLocationCommand

    /// <summary>
    /// The core command for pop-location.  This is the equivalent of the popd command.
    /// It pops a container from the stack and sets the current location to that container.
    /// </summary>
    [Cmdlet(VerbsCommon.Pop, "Location", SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096907")]
    public class PopLocationCommand : CoreCommandBase
    {
        #region Command parameters

        /// <summary>
        /// Gets or sets the parameter -passThru which states output from
        /// the command should be placed in the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get => _passThrough;
            set => _passThrough = value;
        }

        /// <summary>
        /// Gets or sets the StackName parameter which determines which location stack
        /// to use for the pop. If the parameter is missing or empty the default
        /// location stack is used.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string StackName
        {
            get => _stackName;
            set => _stackName = value;
        }

        #endregion Command parameters

        #region Command data

        /// <summary>
        /// Determines if output should be passed through for
        /// pop-location.
        /// </summary>
        private bool _passThrough;

        /// <summary>
        /// The ID of the stack to use for the pop.
        /// </summary>
        private string _stackName;

        #endregion Command data

        #region Command code

        /// <summary>
        /// Gets the top container from the location stack and sets the
        /// location to it.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                // Pop the top of the location stack.

                PathInfo result = SessionState.Path.PopLocation(_stackName);

                if (PassThru)
                {
                    WriteObject(result);
                }
            }
            catch (DriveNotFoundException driveNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        driveNotFound.ErrorRecord,
                        driveNotFound));
                return;
            }
            catch (ProviderNotFoundException providerNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        providerNotFound.ErrorRecord,
                        providerNotFound));
                return;
            }
            catch (PSArgumentException argException)
            {
                WriteError(
                    new ErrorRecord(
                        argException.ErrorRecord,
                        argException));
                return;
            }
            catch (ItemNotFoundException itemNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        itemNotFound.ErrorRecord,
                        itemNotFound));
                return;
            }
        }

        #endregion Command code
    }

    #endregion PopLocationCommand

    #region Drive commands

    #region NewPSDriveCommand

    /// <summary>
    /// Mounts a drive in PowerShell runspace.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PSDrive", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low,
        SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096815")]
    public class NewPSDriveCommand : CoreCommandWithCredentialsBase
    {
        #region Command parameters

        /// <summary>
        /// Gets or sets the name of the drive.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Name
        {
            get => _name;
            set => _name = value ?? throw PSTraceSource.NewArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the provider ID.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string PSProvider
        {
            get => _provider;
            set => _provider = value ?? throw PSTraceSource.NewArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the root of the drive. This path should be
        /// a namespace specific path.
        /// </summary>
        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [AllowEmptyString]
        public string Root
        {
            get => _root;
            set => _root = value ?? throw PSTraceSource.NewArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the description of the drive.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string Description
        {
            get => _description;
            set => _description = value ?? throw PSTraceSource.NewArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the scope identifier for the drive being created.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(ScopeArgumentCompleter))]
        public string Scope { get; set; }

#if !UNIX
        /// <summary>
        /// Gets or sets the Persist Switch parameter.
        /// If this switch parameter is set then the created PSDrive
        /// would be persisted across PowerShell sessions.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Persist
        {
            get => _persist;
            set => _persist = value;
        }

        private bool _persist = false;
#endif
        /// <summary>
        /// Gets the dynamic parameters for the new-psdrive cmdlet.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            return SessionState.Drive.NewDriveDynamicParameters(PSProvider, context);
        }

        /// <summary>
        /// New-psdrive always supports ShouldProcess.
        /// </summary>
        /// <value></value>
        protected override bool ProviderSupportsShouldProcess => true;

        #endregion Command parameters

        #region Command data

        /// <summary>
        /// The name of the drive.
        /// </summary>
        private string _name;

        /// <summary>
        /// The provider ID for the drive.
        /// </summary>
        private string _provider;

        /// <summary>
        /// The namespace specific path of the root of the drive.
        /// </summary>
        private string _root;

        /// <summary>
        /// A description for the drive.
        /// </summary>
        private string _description;

        #endregion Command data

        #region Command code

        /// <summary>
        /// Adds a new drive to the Monad namespace.
        /// </summary>
        protected override void ProcessRecord()
        {
            ProviderInfo provider = null;

            try
            {
                provider = SessionState.Internal.GetSingleProvider(PSProvider);
            }
            catch (ProviderNotFoundException providerNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        providerNotFound.ErrorRecord,
                        providerNotFound));
                return;
            }

            // Check to see if the provider exists
            if (provider != null)
            {
                // Get the confirmation strings

                string action = NavigationResources.NewDriveConfirmAction;
                string resourceTemplate = NavigationResources.NewDriveConfirmResourceTemplate;

                string resource =
                    string.Format(
                       System.Globalization.CultureInfo.CurrentCulture,
                       resourceTemplate,
                       Name,
                       provider.FullName,
                       Root);

                if (ShouldProcess(resource, action))
                {
#if !UNIX
                    // -Persist switch parameter is supported only for FileSystem provider.
                    if (Persist && !provider.Name.Equals(FileSystemProvider.ProviderName, StringComparison.OrdinalIgnoreCase))
                    {
                        ErrorRecord er = new(new NotSupportedException(FileSystemProviderStrings.PersistNotSupported), "DriveRootNotNetworkPath", ErrorCategory.InvalidArgument, this);
                        ThrowTerminatingError(er);
                    }

                    // Trimming forward and backward slash for FileSystem provider when -Persist is used.
                    if (Persist && provider.Name.Equals(FileSystemProvider.ProviderName, StringComparison.OrdinalIgnoreCase))
                    {
                        Root = Root.TrimEnd('/', '\\');
                    }

                    // Create the new drive
                    PSDriveInfo newDrive =
                        new(
                            Name,
                            provider,
                            Root,
                            Description,
                            Credential,
                            Persist);
#else
                    // Create the new drive
                    PSDriveInfo newDrive =
                        new PSDriveInfo(
                            Name,
                            provider,
                            Root,
                            Description,
                            Credential,
                            persist: false);
#endif
                    try
                    {
                        SessionState.Drive.New(newDrive, Scope, CmdletProviderContext);
                    }
                    catch (PSNotSupportedException notSupported)
                    {
                        WriteError(
                            new ErrorRecord(
                                notSupported.ErrorRecord,
                                notSupported));
                        return;
                    }
                    catch (DriveNotFoundException driveNotFound)
                    {
                        WriteError(
                            new ErrorRecord(
                                driveNotFound.ErrorRecord,
                                driveNotFound));
                        return;
                    }
                    catch (ProviderNotFoundException providerNotFound)
                    {
                        WriteError(
                            new ErrorRecord(
                                providerNotFound.ErrorRecord,
                                providerNotFound));
                        return;
                    }
                    catch (PSArgumentException argException)
                    {
                        WriteError(
                            new ErrorRecord(
                                argException.ErrorRecord,
                                argException));
                        return;
                    }
                    catch (ItemNotFoundException pathNotFound)
                    {
                        WriteError(
                            new ErrorRecord(
                                pathNotFound.ErrorRecord,
                                pathNotFound));
                        return;
                    }
                    catch (SessionStateException sessionStateException)
                    {
                        WriteError(
                            new ErrorRecord(
                                sessionStateException.ErrorRecord,
                                sessionStateException));
                        return;
                    }
                }
            }
        }
        #endregion Command code

    }

    #endregion NewPSDriveCommand

    #region DriveMatchingCoreCommandBase

    /// <summary>
    /// Base class for Drive commands that need to glob drives on both the drive name
    /// and the provider name.
    /// </summary>
    public class DriveMatchingCoreCommandBase : CoreCommandBase
    {
        /// <summary>
        /// Globs on both the drive name and the provider name to get a list of Drives
        /// that match the glob filters.
        /// </summary>
        /// <param name="driveName">
        /// The name of the drive(s) to returned. The name can contain glob characters.
        /// </param>
        /// <param name="providerNames">
        /// The name of the provider(s) to return. The name can contain glob characters.
        /// </param>
        /// <param name="scope">
        /// The scope to get the drives from. If this parameter is null or empty all drives
        /// will be retrieved.
        /// </param>
        /// <returns>
        /// A collection of the drives that match the filters.
        /// </returns>
        /// <exception cref="DriveNotFoundException"></exception>
        /// <exception cref="ProviderNotFoundException"></exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scope"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scope"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        internal List<PSDriveInfo> GetMatchingDrives(
             string driveName,
            string[] providerNames,
            string scope)
        {
            List<PSDriveInfo> results = new();

            if (providerNames == null || providerNames.Length == 0)
            {
                providerNames = new string[] { "*" };
            }

            foreach (string providerName in providerNames)
            {
                tracer.WriteLine("ProviderName: {0}", providerName);

                bool providerNameEmpty = string.IsNullOrEmpty(providerName);
                bool providerNameContainsWildcardCharacters =
                    WildcardPattern.ContainsWildcardCharacters(providerName);

                bool driveNameEmpty = string.IsNullOrEmpty(driveName);
                bool driveNameContainsWildcardCharacters =
                    WildcardPattern.ContainsWildcardCharacters(driveName);

                // This is just a simple check to see if the provider exists
                // if the provider name is specified without glob characters.
                // The call will throw an exception if the provider doesn't
                // exist.
                if (!providerNameEmpty && !providerNameContainsWildcardCharacters)
                {
                    SessionState.Provider.Get(providerName);
                }

                // This is just a simple check to see if the drive exists
                // if the drive name is specified without glob characters.
                // The call will throw an exception if the drive doesn't
                // exist.
                if (!driveNameEmpty && !driveNameContainsWildcardCharacters)
                {
                    if (string.IsNullOrEmpty(scope))
                    {
                        SessionState.Drive.Get(driveName);
                    }
                    else
                    {
                        SessionState.Drive.GetAtScope(driveName, scope);
                    }
                }

                WildcardPattern providerMatcher = null;
                PSSnapinQualifiedName pssnapinQualifiedProviderName = null;

                if (!providerNameEmpty)
                {
                    pssnapinQualifiedProviderName = PSSnapinQualifiedName.GetInstance(providerName);

                    if (pssnapinQualifiedProviderName == null)
                    {
                        // This is a malformed pssnapin-qualified name so there is no chances for a match.
                        continue;
                    }

                    providerMatcher =
                        WildcardPattern.Get(
                            pssnapinQualifiedProviderName.ShortName,
                            WildcardOptions.IgnoreCase);
                }

                WildcardPattern nameMatcher = null;

                if (!driveNameEmpty)
                {
                    nameMatcher =
                        WildcardPattern.Get(
                            driveName,
                            WildcardOptions.IgnoreCase);
                }

                foreach (PSDriveInfo drive in SessionState.Drive.GetAllAtScope(scope))
                {
                    bool addDrive = driveNameEmpty;

                    if (base.SuppressWildcardExpansion)
                    {
                        if (string.Equals(drive.Name, driveName, StringComparison.OrdinalIgnoreCase))
                            addDrive = true;
                    }
                    else
                    {
                        if (nameMatcher != null && nameMatcher.IsMatch(drive.Name))
                            addDrive = true;
                    }

                    if (addDrive)
                    {
                        // Now check to see if it matches the provider
                        if (providerNameEmpty || drive.Provider.IsMatch(providerMatcher, pssnapinQualifiedProviderName))
                        {
                            results.Add(drive);
                        }
                    }
                }
            }

            results.Sort();
            return results;
        }
    }

    #endregion DriveMatchingCoreCommandBase

    #region RemovePSDriveCommand

    /// <summary>
    /// Removes a drive that is mounted in the PowerShell runspace.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PSDrive", DefaultParameterSetName = NameParameterSet, SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097050")]
    public class RemovePSDriveCommand : DriveMatchingCoreCommandBase
    {
        #region Command parameters

        private const string NameParameterSet = "Name";
        private const string LiteralNameParameterSet = "LiteralName";

        /// <summary>
        /// Gets or sets the name of the drive to remove.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = NameParameterSet,
                   Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        [AllowEmptyCollection]
        public string[] Name
        {
            get => _names;
            set => _names = value;
        }

        /// <summary>
        /// Gets or sets the literal name parameter to the command.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = LiteralNameParameterSet,
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        public string[] LiteralName
        {
            get => _names;
            set
            {
                base.SuppressWildcardExpansion = true;
                _names = value;
            }
        }

        /// <summary>
        /// Gets or sets the name provider(s) for which the drives should be removed.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string[] PSProvider
        {
            get => _provider;
            set => _provider = value ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets or sets the scope identifier from which to remove the drive.
        /// If the scope is null or empty, the scope hierarchy will be searched
        /// starting at the current scope through all the parent scopes to the
        /// global scope until a drive of the given name is found to remove.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(ScopeArgumentCompleter))]
        public string Scope { get; set; }

        /// <summary>
        /// Gets or sets the force property which determines if the drive
        /// should be removed even if there were errors.
        /// </summary>
        [Parameter]
        public override SwitchParameter Force
        {
            get => base.Force;
            set => base.Force = value;
        }

        /// <summary>
        /// Determines if the provider for the specified path supports ShouldProcess.
        /// </summary>
        /// <value></value>
        protected override bool ProviderSupportsShouldProcess => true;

        #endregion Command parameters

        #region Command data

        /// <summary>
        /// The name of the drive to remove.
        /// </summary>
        private string[] _names;

        /// <summary>
        /// The name of the provider(s) for which to remove all drives.
        /// </summary>
        private string[] _provider = Array.Empty<string>();

        #endregion Command data

        #region Command code

        /// <summary>
        /// Removes the specified drive from the Monad namespace using the name
        /// of the drive.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Get the confirmation strings

            string action = NavigationResources.RemoveDriveConfirmAction;
            string resourceTemplate = NavigationResources.RemoveDriveConfirmResourceTemplate;

            bool verifyMatch = true;
            if (_names == null)
            {
                _names = new string[] { string.Empty };
                verifyMatch = false;
            }

            foreach (string driveName in _names)
            {
                bool foundMatch = false;
                try
                {
                    foreach (PSDriveInfo drive in GetMatchingDrives(driveName, PSProvider, Scope))
                    {
                        string resource =
                            string.Format(
                                System.Globalization.CultureInfo.CurrentCulture,
                                resourceTemplate,
                                drive.Name,
                                drive.Provider,
                                drive.Root);

                        foundMatch = true;
                        if (ShouldProcess(resource, action))
                        {
                            if (!Force && drive == SessionState.Drive.Current)
                            {
                                PSInvalidOperationException invalidOperation =
                                    (PSInvalidOperationException)PSTraceSource.NewInvalidOperationException(
                                        NavigationResources.RemoveDriveInUse,
                                        drive.Name);

                                WriteError(
                                    new ErrorRecord(
                                        invalidOperation.ErrorRecord,
                                        invalidOperation));
                                continue;
                            }

                            SessionState.Drive.Remove(drive.Name, Force, Scope, CmdletProviderContext);
                        }
                    }
                }
                catch (DriveNotFoundException)
                {
                }
                catch (ProviderNotFoundException)
                {
                }

                // If a name was specified explicitly write an error if the drive wasn't
                // found

                if (verifyMatch && !foundMatch)
                {
                    DriveNotFoundException e = new(
                        driveName,
                        "DriveNotFound",
                        SessionStateStrings.DriveNotFound);
                    WriteError(new ErrorRecord(e.ErrorRecord, e));
                }
            }
        }

        #endregion Command code
    }

    #endregion RemovePSDriveCommand

    #region GetPSDriveCommand

    /// <summary>
    /// Gets a specified or listing of drives that are mounted in PowerShell
    /// namespace.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PSDrive", DefaultParameterSetName = NameParameterSet, SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096494")]
    [OutputType(typeof(PSDriveInfo))]
    public class GetPSDriveCommand : DriveMatchingCoreCommandBase
    {
        #region Command parameters

        private const string NameParameterSet = "Name";
        private const string LiteralNameParameterSet = "LiteralName";

        /// <summary>
        /// Gets or sets the drive name the user is looking for.
        /// </summary>
        /// <remarks>
        /// If the drive name is left empty, all drives will be
        /// returned. A globing or regular expression can also be
        /// supplied and any drive names that match the expression
        /// will be returned.
        /// </remarks>
        [Parameter(Position = 0, ParameterSetName = NameParameterSet, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string[] Name
        {
            get => _name;
            set => _name = value ?? new string[] { "*" };
        }

        /// <summary>
        /// Gets or sets the literal name parameter to the command.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = LiteralNameParameterSet,
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        public string[] LiteralName
        {
            get => _name;
            set
            {
                base.SuppressWildcardExpansion = true;
                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets the scope parameter to the command.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(ScopeArgumentCompleter))]
        public string Scope { get; set; }

        /// <summary>
        /// Gets or sets the provider name for the
        /// drives that should be retrieved.
        /// </summary>
        /// <remarks>
        /// If the provider is left empty, all drives will be
        /// returned. A globing or regular expression can also be
        /// supplied and any drive with providers that match the expression
        /// will be returned.
        /// </remarks>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string[] PSProvider
        {
            get => _provider;
            set => _provider = value ?? Array.Empty<string>();
        }

        #endregion Command parameters

        #region Command data

        /// <summary>
        /// The name of the drive to be retrieved.
        /// </summary>
        private string[] _name = new string[] { "*" };

        /// <summary>
        /// The provider ID for the drives you want to see.
        /// </summary>
        private string[] _provider = Array.Empty<string>();

        #endregion Command data

        #region Command code

        /// <summary>
        /// Prepare the session for the Get-PSDrive command.
        /// Currently, auto-loads the core modules that define drives. Ideally,
        /// we could discover fake PSDriveInfo objects here based on drives exported
        /// from modules.
        /// </summary>
        protected override void BeginProcessing()
        {
            SessionStateInternal.MountDefaultDrive("Cert", Context);
            SessionStateInternal.MountDefaultDrive("WSMan", Context);
        }

        /// <summary>
        /// Retrieves the drives specified by the parameters. If the name is empty, all drives
        /// will be retrieved. If the provider is specified, only drives for that provider
        /// will be retrieved.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string driveName in Name)
            {
                try
                {
                    List<PSDriveInfo> foundDrives = GetMatchingDrives(driveName, PSProvider, Scope);

                    if (foundDrives.Count > 0)
                    {
                        WriteObject(foundDrives, true);
                    }
                    else
                    {
                        // If no drives were found and the user was asking for a specific
                        // drive (no wildcards) then write an error

                        if (!WildcardPattern.ContainsWildcardCharacters(driveName))
                        {
                            DriveNotFoundException driveNotFound =
                                new(
                                    driveName,
                                    "DriveNotFound",
                                    SessionStateStrings.DriveNotFound);

                            WriteError(
                                new ErrorRecord(
                                    driveNotFound,
                                    "GetDriveNoMatchingDrive",
                                    ErrorCategory.ObjectNotFound,
                                    driveName));
                        }
                    }
                }
                catch (DriveNotFoundException driveNotFound)
                {
                    ErrorRecord errorRecord =
                        new(
                            driveNotFound,
                            "GetLocationNoMatchingDrive",
                            ErrorCategory.ObjectNotFound,
                            driveName);
                    WriteError(errorRecord);
                }
                catch (ProviderNotFoundException providerNotFound)
                {
                    ErrorRecord errorRecord =
                        new(
                            providerNotFound,
                            "GetLocationNoMatchingDrive",
                            ErrorCategory.ObjectNotFound,
                            PSProvider);
                    WriteError(errorRecord);
                }
                catch (PSArgumentOutOfRangeException outOfRange)
                {
                    WriteError(
                        new ErrorRecord(
                            outOfRange.ErrorRecord,
                            outOfRange));
                }
                catch (PSArgumentException argException)
                {
                    WriteError(
                        new ErrorRecord(
                            argException.ErrorRecord,
                            argException));
                }
            }
        }

        #endregion Command code
    }

    #endregion GetPSDriveCommand

    #endregion Drive commands

    #region Item commands

    #region GetItemCommand

    /// <summary>
    /// Gets the specified item using the namespace providers.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Item", DefaultParameterSetName = PathParameterSet, SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096812")]
    public class GetItemCommand : CoreCommandWithCredentialsBase
    {
        #region Command parameters

        private const string PathParameterSet = "Path";
        private const string LiteralPathParameterSet = "LiteralPath";

        /// <summary>
        /// Gets or sets the path to item to get.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = PathParameterSet,
                   Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get => _paths;
            set => _paths = value;
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = LiteralPathParameterSet,
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get => _paths;
            set
            {
                base.SuppressWildcardExpansion = true;
                _paths = value;
            }
        }

        /// <summary>
        /// Gets or sets the filter property.
        /// </summary>
        [Parameter]
        public override string Filter
        {
            get => base.Filter;
            set => base.Filter = value;
        }

        /// <summary>
        /// Gets or sets the include property.
        /// </summary>
        [Parameter]
        public override string[] Include
        {
            get => base.Include;
            set => base.Include = value;
        }

        /// <summary>
        /// Gets or sets the exclude property.
        /// </summary>
        [Parameter]
        public override string[] Exclude
        {
            get => base.Exclude;
            set => base.Exclude = value;
        }

        /// <summary>
        /// Gets or sets the force property.
        /// </summary>
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
        [Parameter]
        public override SwitchParameter Force
        {
            get => base.Force;
            set => base.Force = value;
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-item cmdlet.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Item.GetItemDynamicParameters(Path[0], context);
            }

            return InvokeProvider.Item.GetItemDynamicParameters(".", context);
        }

        #endregion Command parameters

        #region Command data
        /// <summary>
        /// The path of the item to get.
        /// </summary>
        private string[] _paths;

        #endregion Command data

        #region Command code

        /// <summary>
        /// Gets the specified item.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string path in _paths)
            {
                try
                {
                    InvokeProvider.Item.Get(path, CmdletProviderContext);
                }
                catch (PSNotSupportedException notSupported)
                {
                    WriteError(
                        new ErrorRecord(
                            notSupported.ErrorRecord,
                            notSupported));
                }
                catch (DriveNotFoundException driveNotFound)
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
                    WriteError(
                        new ErrorRecord(
                            pathNotFound.ErrorRecord,
                            pathNotFound));
                }
            }
        }

        #endregion Command code
    }

    #endregion GetItemCommand

    #region NewItemCommand

    /// <summary>
    /// Creates the specified item using the namespace providers.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Item", DefaultParameterSetName = PathParameterSet, SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096592")]
    public class NewItemCommand : CoreCommandWithCredentialsBase
    {
        #region Command parameters

        private const string NameParameterSet = "nameSet";
        private const string PathParameterSet = "pathSet";

        /// <summary>
        /// Gets or sets the container path to create the item in.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = PathParameterSet, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = NameParameterSet, Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public string[] Path { get; set; }

        /// <summary>
        /// Gets or sets the name of the item to create.
        /// </summary>
        [Parameter(ParameterSetName = NameParameterSet, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        [AllowEmptyString]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the item to create.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Type")]
        public string ItemType { get; set; }

        /// <summary>
        /// Gets or sets the content of the item to create.
        /// </summary>
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("Target")]
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the force property.
        /// </summary>
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
        [Parameter]
        public override SwitchParameter Force
        {
            get => base.Force;
            set => base.Force = value;
        }

        /// <summary>
        /// Gets the dynamic parameters for the new-item cmdlet.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                // Path is only globbed if Name is specified.
                if (string.IsNullOrEmpty(Name))
                    return InvokeProvider.Item.NewItemDynamicParameters(WildcardPattern.Escape(Path[0]), ItemType, Value, context);
                else
                    return InvokeProvider.Item.NewItemDynamicParameters(Path[0], ItemType, Value, context);
            }

            return InvokeProvider.Item.NewItemDynamicParameters(".", ItemType, Value, context);
        }

        /// <summary>
        /// Determines if the provider for the specified path supports ShouldProcess.
        /// </summary>
        /// <value></value>
        protected override bool ProviderSupportsShouldProcess => DoesProviderSupportShouldProcess(Path);

        #endregion Command parameters

        #region Command data

        #endregion Command Data

        #region Command code

        /// <summary>
        /// Creates the specified item.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (Path == null || Path.Length == 0)
            {
                Path = new string[] { string.Empty };
            }

            foreach (string path in Path)
            {
                try
                {
                    InvokeProvider.Item.New(path, Name, ItemType, Value, CmdletProviderContext);
                }
                catch (PSNotSupportedException notSupported)
                {
                    WriteError(
                        new ErrorRecord(
                            notSupported.ErrorRecord,
                            notSupported));
                }
                catch (DriveNotFoundException driveNotFound)
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
                    WriteError(
                        new ErrorRecord(
                            pathNotFound.ErrorRecord,
                            pathNotFound));
                }
            }
        }

        #endregion Command code
    }

    #endregion NewItemCommand

    #region SetItemCommand

    /// <summary>
    /// Sets the specified item using the namespace providers.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "Item", SupportsShouldProcess = true, DefaultParameterSetName = PathParameterSet, SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097055")]
    public class SetItemCommand : CoreCommandWithCredentialsBase
    {
        #region Command parameters

        private const string PathParameterSet = "Path";
        private const string LiteralPathParameterSet = "LiteralPath";

        /// <summary>
        /// Gets or sets the path to item to set.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = PathParameterSet,
                   Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get => _paths;
            set => _paths = value;
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = LiteralPathParameterSet,
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get => _paths;
            set
            {
                base.SuppressWildcardExpansion = true;
                _paths = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of the item to be set.
        /// </summary>
        [Parameter(Position = 1, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the force property.
        /// </summary>
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
        [Parameter]
        public override SwitchParameter Force
        {
            get => base.Force;
            set => base.Force = value;
        }

        /// <summary>
        /// Gets or sets the pass through property which determines
        /// if the object that is set should be written to the pipeline.
        /// Defaults to false.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get => _passThrough;
            set => _passThrough = value;
        }

        /// <summary>
        /// Gets or sets the filter property.
        /// </summary>
        [Parameter]
        public override string Filter
        {
            get => base.Filter;
            set => base.Filter = value;
        }

        /// <summary>
        /// Gets or sets the include property.
        /// </summary>
        [Parameter]
        public override string[] Include
        {
            get => base.Include;
            set => base.Include = value;
        }

        /// <summary>
        /// Gets or sets the exclude property.
        /// </summary>
        [Parameter]
        public override string[] Exclude
        {
            get => base.Exclude;
            set => base.Exclude = value;
        }

        /// <summary>
        /// Gets the dynamic parameters for the set-item cmdlet.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Item.SetItemDynamicParameters(Path[0], Value, context);
            }

            return InvokeProvider.Item.SetItemDynamicParameters(".", Value, context);
        }

        /// <summary>
        /// Determines if the provider for the specified path supports ShouldProcess.
        /// </summary>
        /// <value></value>
        protected override bool ProviderSupportsShouldProcess => DoesProviderSupportShouldProcess(_paths);
        #endregion Command parameters

        #region Command data
        /// <summary>
        /// The path of the item to set.
        /// </summary>
        private string[] _paths;

        /// <summary>
        /// Determines if the object being set should be written to the pipeline.
        /// Defaults to false.
        /// </summary>
        private bool _passThrough;

        #endregion Command data

        #region Command code

        /// <summary>
        /// Sets the specified item.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Default to the CmdletProviderContext that will direct output to
            // the pipeline.

            CmdletProviderContext currentCommandContext = CmdletProviderContext;
            currentCommandContext.PassThru = _passThrough;

            foreach (string path in _paths)
            {
                try
                {
                    InvokeProvider.Item.Set(path, Value, currentCommandContext);
                }
                catch (PSNotSupportedException notSupported)
                {
                    WriteError(
                        new ErrorRecord(
                            notSupported.ErrorRecord,
                            notSupported));
                }
                catch (DriveNotFoundException driveNotFound)
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
                    WriteError(
                        new ErrorRecord(
                            pathNotFound.ErrorRecord,
                            pathNotFound));
                }
            }
        }

        #endregion Command code
    }

    #endregion SetItemCommand

    #region RemoveItemCommand

    /// <summary>
    /// Removes the specified item using the namespace providers.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "Item", SupportsShouldProcess = true, DefaultParameterSetName = PathParameterSet, SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097103")]
    public class RemoveItemCommand : CoreCommandWithCredentialsBase
    {
        #region Command parameters

        private const string PathParameterSet = "Path";
        private const string LiteralPathParameterSet = "LiteralPath";

        /// <summary>
        /// Gets or sets the path property.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = PathParameterSet,
                   Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get => _paths;
            set => _paths = value;
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = LiteralPathParameterSet,
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get => _paths;
            set
            {
                base.SuppressWildcardExpansion = true;
                _paths = value;
            }
        }

        /// <summary>
        /// Gets or sets the filter property.
        /// </summary>
        [Parameter]
        public override string Filter
        {
            get => base.Filter;
            set => base.Filter = value;
        }

        /// <summary>
        /// Gets or sets the include property.
        /// </summary>
        [Parameter]
        public override string[] Include
        {
            get => base.Include;
            set => base.Include = value;
        }

        /// <summary>
        /// Gets or sets the exclude property.
        /// </summary>
        [Parameter]
        public override string[] Exclude
        {
            get => base.Exclude;
            set => base.Exclude = value;
        }

        /// <summary>
        /// Gets or sets the recurse property.
        /// </summary>
        [Parameter]
        public SwitchParameter Recurse
        {
            get => _recurse;
            set => _recurse = value;
        }

        /// <summary>
        /// Gets or sets the force property.
        /// </summary>
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
        [Parameter]
        public override SwitchParameter Force
        {
            get => base.Force;
            set => base.Force = value;
        }

        /// <summary>
        /// Gets the dynamic parameters for the remove-item cmdlet.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Item.RemoveItemDynamicParameters(Path[0], Recurse, context);
            }

            return InvokeProvider.Item.RemoveItemDynamicParameters(".", Recurse, context);
        }

        /// <summary>
        /// Determines if the provider for the specified path supports ShouldProcess.
        /// </summary>
        /// <value></value>
        protected override bool ProviderSupportsShouldProcess => DoesProviderSupportShouldProcess(_paths);
        #endregion Command parameters

        #region Command data

        /// <summary>
        /// The path used when doing a delete.
        /// </summary>
        private string[] _paths;

        /// <summary>
        /// Determines if the remove command should recurse into
        /// sub-containers.
        /// </summary>
        private bool _recurse;

        #endregion Command data

        #region Command code

        /// <summary>
        /// Removes the specified items.
        /// </summary>
        protected override void ProcessRecord()
        {
            CmdletProviderContext currentContext = CmdletProviderContext;

            bool yesToAll = false;
            bool noToAll = false;

            foreach (string path in Path)
            {
                // Resolve the path in case it contains any glob characters
                Collection<PathInfo> resolvedPSPaths = null;

                try
                {
                    // Save the include and exclude filters so that we can ignore
                    // them when doing recursion
                    Collection<string> include = currentContext.Include;
                    Collection<string> exclude = currentContext.Exclude;
                    string filter = currentContext.Filter;

                    if (_recurse)
                    {
                        currentContext.SetFilters(
                            new Collection<string>(),
                            new Collection<string>(),
                            null);
                    }

                    try
                    {
                        resolvedPSPaths = SessionState.Path.GetResolvedPSPathFromPSPath(path, currentContext);
                        if (SuppressWildcardExpansion == true && resolvedPSPaths.Count == 0)
                        {
                            ItemNotFoundException pathNotFound =
                                new(
                                    path,
                                    "PathNotFound",
                                    SessionStateStrings.PathNotFound);
                            WriteError(new ErrorRecord(
                                pathNotFound.ErrorRecord,
                                pathNotFound));
                            continue;
                        }
                    }
                    finally
                    {
                        // Reset the include and exclude filters
                        currentContext.SetFilters(
                            include,
                            exclude,
                            filter);
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

                foreach (PathInfo resolvedPath in resolvedPSPaths)
                {
                    // Check each path to make sure it isn't a parent of the current working location

                    bool isCurrentLocationOrAncestor = false;

                    try
                    {
                        isCurrentLocationOrAncestor = SessionState.Path.IsCurrentLocationOrAncestor(resolvedPath.Path, currentContext);
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

                    if (isCurrentLocationOrAncestor)
                    {
                        PSInvalidOperationException invalidOperation =
                            (PSInvalidOperationException)PSTraceSource.NewInvalidOperationException(
                                NavigationResources.RemoveItemInUse,
                                resolvedPath.Path);

                        WriteError(
                            new ErrorRecord(
                                invalidOperation.ErrorRecord,
                                invalidOperation));
                        continue;
                    }

                    bool hasChildren = false;

                    string providerPath = GetUnresolvedProviderPathFromPSPath(resolvedPath.Path);

                    try
                    {
                        hasChildren = SessionState.Internal.HasChildItems(resolvedPath.Provider.Name, providerPath, currentContext);

                        currentContext.ThrowFirstErrorOrDoNothing();
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

                    bool shouldRecurse = Recurse;
                    bool treatAsFile = false;

                    // only check if path is a directory using DirectoryInfo if using FileSystemProvider
                    if (resolvedPath.Provider.Name.Equals(FileSystemProvider.ProviderName, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            System.IO.DirectoryInfo di = new(providerPath);
                            if (InternalSymbolicLinkLinkCodeMethods.IsReparsePointLikeSymlink(di))
                            {
                                shouldRecurse = false;
                                treatAsFile = true;
                            }
                        }
                        catch (System.IO.FileNotFoundException)
                        {
                            // not a directory
                        }
                    }

                    if (!treatAsFile && !Recurse && hasChildren)
                    {
                        // Get the localized prompt string

                        string prompt = StringUtil.Format(NavigationResources.RemoveItemWithChildren, resolvedPath.Path);

                        // Confirm the user wants to remove all children and the item even if
                        // they did not specify -recurse

                        if (!ShouldContinue(prompt, null, ref yesToAll, ref noToAll))
                        {
                            continue;
                        }

                        shouldRecurse = true;
                    }

                    // Now do the delete
                    // This calls the internal method since it is more efficient
                    // than trying to glob again.  It also will prevent problems
                    // where globbing a second time may not have properly escaped
                    // wildcard characters in the path.

                    try
                    {
                        SessionState.Internal.RemoveItem(
                            resolvedPath.Provider.Name,
                            providerPath,
                            shouldRecurse,
                            currentContext);
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

        #endregion Command code
    }

    #endregion RemoveItemCommand

    #region MoveItemCommand

    /// <summary>
    /// Moves an item from the specified location to the specified destination using
    /// the namespace providers.
    /// </summary>
    [Cmdlet(VerbsCommon.Move, "Item", DefaultParameterSetName = PathParameterSet, SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096591")]
    public class MoveItemCommand : CoreCommandWithCredentialsBase
    {
        #region Command parameters

        private const string PathParameterSet = "Path";
        private const string LiteralPathParameterSet = "LiteralPath";

        /// <summary>
        /// Gets or sets the path property.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = PathParameterSet,
                   Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get => _paths;
            set => _paths = value;
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = LiteralPathParameterSet,
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get => _paths;
            set
            {
                base.SuppressWildcardExpansion = true;
                _paths = value;
            }
        }

        /// <summary>
        /// Gets or sets the destination property.
        /// </summary>
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        public string Destination { get; set; } = ".";

        /// <summary>
        /// Gets or sets the force property.
        /// </summary>
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
        [Parameter]
        public override SwitchParameter Force
        {
            get => base.Force;
            set => base.Force = value;
        }

        /// <summary>
        /// Gets or sets the filter property.
        /// </summary>
        [Parameter]
        public override string Filter
        {
            get => base.Filter;
            set => base.Filter = value;
        }

        /// <summary>
        /// Gets or sets the include property.
        /// </summary>
        [Parameter]
        public override string[] Include
        {
            get => base.Include;
            set => base.Include = value;
        }

        /// <summary>
        /// Gets or sets the exclude property.
        /// </summary>
        [Parameter]
        public override string[] Exclude
        {
            get => base.Exclude;
            set => base.Exclude = value;
        }

        /// <summary>
        /// Gets or sets the pass through property which determines
        /// if the object that is set should be written to the pipeline.
        /// Defaults to false.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get => _passThrough;
            set => _passThrough = value;
        }

        /// <summary>
        /// Gets the dynamic parameters for the move-item cmdlet.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Item.MoveItemDynamicParameters(Path[0], Destination, context);
            }

            return InvokeProvider.Item.MoveItemDynamicParameters(".", Destination, context);
        }

        /// <summary>
        /// Determines if the provider for the specified path supports ShouldProcess.
        /// </summary>
        /// <value></value>
        protected override bool ProviderSupportsShouldProcess => DoesProviderSupportShouldProcess(_paths);

        #endregion Command parameters

        #region Command data
        /// <summary>
        /// The path of the item to move. It is set or retrieved via
        /// the Path property.
        /// </summary>
        private string[] _paths;

        /// <summary>
        /// Determines if the object being set should be written to the pipeline.
        /// Defaults to false.
        /// </summary>
        private bool _passThrough;

        #endregion Command data

        #region Command code

        private Collection<PathInfo> GetResolvedPaths(string path)
        {
            Collection<PathInfo> results = new();
            try
            {
                results = SessionState.Path.GetResolvedPSPathFromPSPath(path, CmdletProviderContext);
            }
            catch (PSNotSupportedException notSupported)
            {
                WriteError(
                    new ErrorRecord(
                        notSupported.ErrorRecord,
                        notSupported));
            }
            catch (DriveNotFoundException driveNotFound)
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
                WriteError(
                    new ErrorRecord(
                        pathNotFound.ErrorRecord,
                        pathNotFound));
            }

            return results;
        }

        /// <summary>
        /// Moves the specified item to the specified destination.
        /// </summary>
        protected override void ProcessRecord()
        {
            // When moving multiple items with wildcards to a non-existent destination,
            // we need to create the destination directory first to ensure all items
            // preserve their directory structure. Otherwise, the first directory gets
            // renamed to the destination, and subsequent items are moved into it.
            
            if (!base.SuppressWildcardExpansion)
            {
                // First, collect all resolved paths to determine if we're moving multiple items
                var allResolvedPaths = new List<string>();
                foreach (string path in Path)
                {
                    Collection<PathInfo> resolvedPaths = GetResolvedPaths(path);
                    foreach (PathInfo resolvedPathInfo in resolvedPaths)
                    {
                        allResolvedPaths.Add(resolvedPathInfo.Path);
                    }
                }

                // If moving multiple items and destination doesn't exist, create it first
                if (allResolvedPaths.Count > 1 && !InvokeProvider.Item.Exists(Destination))
                {
                    try
                    {
                        InvokeProvider.Item.New(Destination, "Directory", null);
                    }
                    catch (Exception)
                    {
                        // If creation fails, continue anyway - the move operation will handle the error
                    }
                }

                // Now proceed with the moves
                foreach (string resolvedPath in allResolvedPaths)
                {
                    MoveItem(resolvedPath, literalPath: true);
                }
            }
            else
            {
                // Literal path mode - process each path directly
                foreach (string path in Path)
                {
                    MoveItem(path, literalPath: true);
                }
            }
        }

        private void MoveItem(string path, bool literalPath = false)
        {
            CmdletProviderContext currentContext = CmdletProviderContext;
            currentContext.SuppressWildcardExpansion = literalPath;

            try
            {
                if (!InvokeProvider.Item.Exists(path, currentContext))
                {
                    PSInvalidOperationException invalidOperation =
                        (PSInvalidOperationException)PSTraceSource.NewInvalidOperationException(
                            NavigationResources.MoveItemDoesntExist,
                            path);

                    WriteError(
                        new ErrorRecord(
                            invalidOperation.ErrorRecord,
                            invalidOperation));
                    return;
                }
            }
            catch (PSNotSupportedException notSupported)
            {
                WriteError(
                    new ErrorRecord(
                        notSupported.ErrorRecord,
                        notSupported));
                return;
            }
            catch (DriveNotFoundException driveNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        driveNotFound.ErrorRecord,
                        driveNotFound));
                return;
            }
            catch (ProviderNotFoundException providerNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        providerNotFound.ErrorRecord,
                        providerNotFound));
                return;
            }
            catch (ItemNotFoundException pathNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        pathNotFound.ErrorRecord,
                        pathNotFound));
                return;
            }

            // See if the item to be moved is in use.
            bool isCurrentLocationOrAncestor = false;
            try
            {
                isCurrentLocationOrAncestor = SessionState.Path.IsCurrentLocationOrAncestor(path, currentContext);
            }
            catch (PSNotSupportedException notSupported)
            {
                WriteError(
                    new ErrorRecord(
                        notSupported.ErrorRecord,
                        notSupported));
                return;
            }
            catch (DriveNotFoundException driveNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        driveNotFound.ErrorRecord,
                        driveNotFound));
                return;
            }
            catch (ProviderNotFoundException providerNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        providerNotFound.ErrorRecord,
                        providerNotFound));
                return;
            }
            catch (ItemNotFoundException pathNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        pathNotFound.ErrorRecord,
                        pathNotFound));
                return;
            }

            if (isCurrentLocationOrAncestor)
            {
                PSInvalidOperationException invalidOperation =
                    (PSInvalidOperationException)PSTraceSource.NewInvalidOperationException(
                        NavigationResources.MoveItemInUse,
                        path);

                WriteError(
                    new ErrorRecord(
                        invalidOperation.ErrorRecord,
                        invalidOperation));
                return;
            }

            // Default to the CmdletProviderContext that will direct output to
            // the pipeline.

            currentContext.PassThru = PassThru;

            tracer.WriteLine("Moving {0} to {1}", path, Destination);

            try
            {
                // Now do the move
                InvokeProvider.Item.Move(path, Destination, currentContext);
            }
            catch (PSNotSupportedException notSupported)
            {
                WriteError(
                    new ErrorRecord(
                        notSupported.ErrorRecord,
                        notSupported));
                return;
            }
            catch (DriveNotFoundException driveNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        driveNotFound.ErrorRecord,
                        driveNotFound));
                return;
            }
            catch (ProviderNotFoundException providerNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        providerNotFound.ErrorRecord,
                        providerNotFound));
                return;
            }
            catch (ItemNotFoundException pathNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        pathNotFound.ErrorRecord,
                        pathNotFound));
                return;
            }
        }
        #endregion Command code

    }

    #endregion MoveItemCommand

    #region RenameItemCommand

    /// <summary>
    /// Renames a specified item to a new name using the namespace providers.
    /// </summary>
    [Cmdlet(VerbsCommon.Rename, "Item", SupportsShouldProcess = true, SupportsTransactions = true, DefaultParameterSetName = ByPathParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097153")]
    public class RenameItemCommand : CoreCommandWithCredentialsBase
    {
        #region Command parameters

        private const string ByPathParameterSet = "ByPath";
        private const string ByLiteralPathParameterSet = "ByLiteralPath";

        /// <summary>
        /// Gets or sets the path property.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = ByPathParameterSet)]
        public string Path
        {
            get => _path;
            set => _path = value;
        }

        /// <summary>
        /// Gets or sets the literal path property.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = ByLiteralPathParameterSet)]
        [Alias("PSPath", "LP")]
        public string LiteralPath
        {
            get => _path;
            set
            {
                _path = value;
                base.SuppressWildcardExpansion = true;
            }
        }

        /// <summary>
        /// Gets or sets the newName property.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string NewName { get; set; }

        /// <summary>
        /// Gets or sets the force property.
        /// </summary>
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
        [Parameter]
        public override SwitchParameter Force
        {
            get => base.Force;
            set => base.Force = value;
        }

        /// <summary>
        /// Gets or sets the pass through property which determines
        /// if the object that is set should be written to the pipeline.
        /// Defaults to false.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get => _passThrough;
            set => _passThrough = value;
        }

        /// <summary>
        /// Gets the dynamic parameters for the rename-item cmdlet.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            return InvokeProvider.Item.RenameItemDynamicParameters(Path, NewName, context);
        }

        /// <summary>
        /// Determines if the provider for the specified path supports ShouldProcess.
        /// </summary>
        /// <value></value>
        protected override bool ProviderSupportsShouldProcess => DoesProviderSupportShouldProcess(new string[] { _path });

        #endregion Command parameters

        #region Command data
        /// <summary>
        /// The path of the item to rename. It is set or retrieved via
        /// the Path property.
        /// </summary>
        private string _path;

        /// <summary>
        /// Determines if the object being set should be written to the pipeline.
        /// Defaults to false.
        /// </summary>
        private bool _passThrough;

        #endregion Command data

        #region Command code

        private Collection<PathInfo> GetResolvedPaths(string path)
        {
            Collection<PathInfo> results = null;
            try
            {
                results = SessionState.Path.GetResolvedPSPathFromPSPath(path, CmdletProviderContext);
            }
            catch (PSNotSupportedException notSupported)
            {
                WriteError(
                    new ErrorRecord(
                        notSupported.ErrorRecord,
                        notSupported));
            }
            catch (DriveNotFoundException driveNotFound)
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
                WriteError(
                    new ErrorRecord(
                        pathNotFound.ErrorRecord,
                        pathNotFound));
            }

            return results;
        }

        /// <summary>
        /// Moves the specified item to the specified destination.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (SuppressWildcardExpansion)
            {
                RenameItem(Path, literalPath: true);
                return;
            }

            Collection<PathInfo> resolvedPaths = GetResolvedPaths(Path);
            if (resolvedPaths == null)
            {
                return;
            }

            if (resolvedPaths.Count == 1)
            {
                RenameItem(resolvedPaths[0].Path, literalPath: true);
            }
            else
            {
                RenameItem(WildcardPattern.Unescape(Path), literalPath: true);
            }
        }

        private void RenameItem(string path, bool literalPath = false)
        {
            CmdletProviderContext currentContext = CmdletProviderContext;
            currentContext.SuppressWildcardExpansion = literalPath;

            try
            {
                if (!InvokeProvider.Item.Exists(path, currentContext))
                {
                    PSInvalidOperationException invalidOperation =
                        (PSInvalidOperationException)PSTraceSource.NewInvalidOperationException(
                            NavigationResources.RenameItemDoesntExist,
                            path);

                    WriteError(
                        new ErrorRecord(
                            invalidOperation.ErrorRecord,
                            invalidOperation));
                    return;
                }
            }
            catch (PSNotSupportedException notSupported)
            {
                WriteError(
                    new ErrorRecord(
                        notSupported.ErrorRecord,
                        notSupported));
                return;
            }
            catch (DriveNotFoundException driveNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        driveNotFound.ErrorRecord,
                        driveNotFound));
                return;
            }
            catch (ProviderNotFoundException providerNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        providerNotFound.ErrorRecord,
                        providerNotFound));
                return;
            }
            catch (ItemNotFoundException pathNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        pathNotFound.ErrorRecord,
                        pathNotFound));
                return;
            }

            // See if the item to be moved is in use.
            bool isCurrentLocationOrAncestor = false;
            try
            {
                isCurrentLocationOrAncestor = SessionState.Path.IsCurrentLocationOrAncestor(path, currentContext);
            }
            catch (PSNotSupportedException notSupported)
            {
                WriteError(
                    new ErrorRecord(
                        notSupported.ErrorRecord,
                        notSupported));
                return;
            }
            catch (DriveNotFoundException driveNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        driveNotFound.ErrorRecord,
                        driveNotFound));
                return;
            }
            catch (ProviderNotFoundException providerNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        providerNotFound.ErrorRecord,
                        providerNotFound));
                return;
            }
            catch (ItemNotFoundException pathNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        pathNotFound.ErrorRecord,
                        pathNotFound));
                return;
            }

            if (isCurrentLocationOrAncestor)
            {
                PSInvalidOperationException invalidOperation =
                    (PSInvalidOperationException)PSTraceSource.NewInvalidOperationException(
                        NavigationResources.RenamedItemInUse,
                        path);

                WriteError(
                    new ErrorRecord(
                        invalidOperation.ErrorRecord,
                        invalidOperation));
                return;
            }

            // Default to the CmdletProviderContext that will direct output to
            // the pipeline.
            currentContext.PassThru = PassThru;

            tracer.WriteLine("Rename {0} to {1}", path, NewName);

            try
            {
                // Now do the rename
                InvokeProvider.Item.Rename(path, NewName, currentContext);
            }
            catch (PSNotSupportedException notSupported)
            {
                WriteError(
                    new ErrorRecord(
                        notSupported.ErrorRecord,
                        notSupported));
                return;
            }
            catch (DriveNotFoundException driveNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        driveNotFound.ErrorRecord,
                        driveNotFound));
                return;
            }
            catch (ProviderNotFoundException providerNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        providerNotFound.ErrorRecord,
                        providerNotFound));
                return;
            }
            catch (ItemNotFoundException pathNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        pathNotFound.ErrorRecord,
                        pathNotFound));
                return;
            }
        }
        #endregion Command code

    }

    #endregion RenameItemCommand

    #region CopyItemCommand

    /// <summary>
    /// Copies a specified item to a new location using the namespace providers.
    /// </summary>
    [Cmdlet(VerbsCommon.Copy, "Item", DefaultParameterSetName = PathParameterSet, SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096990")]
    public class CopyItemCommand : CoreCommandWithCredentialsBase
    {
        #region Command parameters

        private const string PathParameterSet = "Path";
        private const string LiteralPathParameterSet = "LiteralPath";

        /// <summary>
        /// Gets or sets the path property.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = PathParameterSet,
                   Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get => _paths;
            set => _paths = value;
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = LiteralPathParameterSet,
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get => _paths;
            set
            {
                base.SuppressWildcardExpansion = true;
                _paths = value;
            }
        }

        /// <summary>
        /// Gets or sets the destination property.
        /// </summary>
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        public string Destination { get; set; }

        /// <summary>
        /// Gets or sets the container property.
        /// </summary>
        [Parameter]
        public SwitchParameter Container
        {
            get => _container;
            set
            {
                _containerSpecified = true;
                _container = value;
            }
        }

        /// <summary>
        /// Gets or sets the force property.
        /// </summary>
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
        [Parameter]
        public override SwitchParameter Force
        {
            get => base.Force;
            set => base.Force = value;
        }

        /// <summary>
        /// Gets or sets the filter property.
        /// </summary>
        [Parameter]
        public override string Filter
        {
            get => base.Filter;
            set => base.Filter = value;
        }

        /// <summary>
        /// Gets or sets the include property.
        /// </summary>
        [Parameter]
        public override string[] Include
        {
            get => base.Include;
            set => base.Include = value;
        }

        /// <summary>
        /// Gets or sets the exclude property.
        /// </summary>
        [Parameter]
        public override string[] Exclude
        {
            get => base.Exclude;
            set => base.Exclude = value;
        }

        /// <summary>
        /// Gets or sets the recurse property.
        /// </summary>
        [Parameter]
        public SwitchParameter Recurse
        {
            get => _recurse;
            set
            {
                _recurse = value;

                // is, then -Container takes on the same value
                // as -Recurse

                if (!_containerSpecified)
                {
                    _container = _recurse;
                }
            }
        }

        /// <summary>
        /// Gets or sets the pass through property which determines
        /// if the object that is set should be written to the pipeline.
        /// Defaults to false.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get => _passThrough;
            set => _passThrough = value;
        }

        /// <summary>
        /// Gets the dynamic parameters for the copy-item cmdlet.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Item.CopyItemDynamicParameters(Path[0], Destination, Recurse, context);
            }

            return InvokeProvider.Item.CopyItemDynamicParameters(".", Destination, Recurse, context);
        }

        /// <summary>
        /// Determines if the provider for the specified path supports ShouldProcess.
        /// </summary>
        /// <value></value>
        protected override bool ProviderSupportsShouldProcess => DoesProviderSupportShouldProcess(_paths);

        #endregion Command parameters

        #region Command data
        /// <summary>
        /// The path of the item to copy. It is set or retrieved via
        /// the Path property.
        /// </summary>
        private string[] _paths;

        /// <summary>
        /// Determines if the containers should be copied with the items or not.
        /// </summary>
        private bool _container = true;
        private bool _containerSpecified;

        /// <summary>
        /// Determines if the copy command should recurse into
        /// sub-containers.
        /// </summary>
        private bool _recurse;

        /// <summary>
        /// Determines if the object being set should be written to the pipeline.
        /// Defaults to false.
        /// </summary>
        private bool _passThrough;

        #endregion Command data

        #region Command code

        /// <summary>
        /// Copies the specified item(s) to the specified destination.
        /// </summary>
        protected override void ProcessRecord()
        {
            CmdletProviderContext currentCommandContext = CmdletProviderContext;
            currentCommandContext.PassThru = PassThru;

            foreach (string path in _paths)
            {
                tracer.WriteLine("Copy {0} to {1}", path, Destination);

                try
                {
                    CopyContainers copyContainers = (Container) ? CopyContainers.CopyTargetContainer : CopyContainers.CopyChildrenOfTargetContainer;

                    // Now do the copy
                    InvokeProvider.Item.Copy(path, Destination, Recurse, copyContainers, currentCommandContext);
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
        #endregion Command code

    }

    #endregion CopyItemCommand

    #region ClearItemCommand

    /// <summary>
    /// Clears an item at the specified location.
    /// </summary>
    [Cmdlet(VerbsCommon.Clear, "Item", DefaultParameterSetName = PathParameterSet, SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096491")]
    public class ClearItemCommand : CoreCommandWithCredentialsBase
    {
        #region Command parameters

        private const string PathParameterSet = "Path";
        private const string LiteralPathParameterSet = "LiteralPath";

        /// <summary>
        /// Gets or sets the path property.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = PathParameterSet,
                   Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get => _paths;
            set => _paths = value;
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = LiteralPathParameterSet,
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get => _paths;
            set
            {
                base.SuppressWildcardExpansion = true;
                _paths = value;
            }
        }

        /// <summary>
        /// Gets or sets the force property.
        /// </summary>
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
        [Parameter]
        public override SwitchParameter Force
        {
            get => base.Force;
            set => base.Force = value;
        }

        /// <summary>
        /// Gets or sets the filter property.
        /// </summary>
        [Parameter]
        public override string Filter
        {
            get => base.Filter;
            set => base.Filter = value;
        }

        /// <summary>
        /// Gets or sets the include property.
        /// </summary>
        [Parameter]
        public override string[] Include
        {
            get => base.Include;
            set => base.Include = value;
        }

        /// <summary>
        /// Gets or sets the exclude property.
        /// </summary>
        [Parameter]
        public override string[] Exclude
        {
            get => base.Exclude;
            set => base.Exclude = value;
        }

        /// <summary>
        /// Gets the dynamic parameters for the clear-item cmdlet.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Item.ClearItemDynamicParameters(Path[0], context);
            }

            return InvokeProvider.Item.ClearItemDynamicParameters(".", context);
        }

        /// <summary>
        /// Determines if the provider for the specified path supports ShouldProcess.
        /// </summary>
        /// <value></value>
        protected override bool ProviderSupportsShouldProcess => DoesProviderSupportShouldProcess(_paths);

        #endregion Command parameters

        #region Command data
        /// <summary>
        /// The path of the item to move. It is set or retrieved via
        /// the Path property.
        /// </summary>
        private string[] _paths;

        #endregion Command data

        #region Command code

        /// <summary>
        /// Clears the specified item.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Default to the CmdletProviderContext that will direct output to
            // the pipeline.

            CmdletProviderContext currentCommandContext = CmdletProviderContext;
            currentCommandContext.PassThru = false;

            foreach (string path in _paths)
            {
                tracer.WriteLine("Clearing {0}", path);

                try
                {
                    // Now do the move
                    InvokeProvider.Item.Clear(path, currentCommandContext);
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

        #endregion Command code
    }

    #endregion ClearItemCommand

    #region InvokeItemCommand

    /// <summary>
    /// Invokes an item at the specified location.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "Item", DefaultParameterSetName = PathParameterSet, SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096590")]
    public class InvokeItemCommand : CoreCommandWithCredentialsBase
    {
        #region Command parameters

        private const string PathParameterSet = "Path";
        private const string LiteralPathParameterSet = "LiteralPath";

        /// <summary>
        /// Gets or sets the path property.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = PathParameterSet,
                   Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get => _paths;
            set => _paths = value;
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = LiteralPathParameterSet,
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get => _paths;
            set
            {
                base.SuppressWildcardExpansion = true;
                _paths = value;
            }
        }

        /// <summary>
        /// Gets or sets the filter property.
        /// </summary>
        [Parameter]
        public override string Filter
        {
            get => base.Filter;
            set => base.Filter = value;
        }

        /// <summary>
        /// Gets or sets the include property.
        /// </summary>
        [Parameter]
        public override string[] Include
        {
            get => base.Include;
            set => base.Include = value;
        }

        /// <summary>
        /// Gets or sets the exclude property.
        /// </summary>
        [Parameter]
        public override string[] Exclude
        {
            get => base.Exclude;
            set => base.Exclude = value;
        }

        /// <summary>
        /// Gets the dynamic parameters for the invoke-item cmdlet.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Item.InvokeItemDynamicParameters(Path[0], context);
            }

            return InvokeProvider.Item.InvokeItemDynamicParameters(".", context);
        }

        /// <summary>
        /// Determines if the provider for the specified path supports ShouldProcess.
        /// </summary>
        /// <value></value>
        protected override bool ProviderSupportsShouldProcess => DoesProviderSupportShouldProcess(_paths);

        #endregion Command parameters

        #region Command data
        /// <summary>
        /// The path of the item to move. It is set or retrieved via
        /// the Path property.
        /// </summary>
        private string[] _paths;

        #endregion Command data

        #region Command code

        /// <summary>
        /// Invokes the specified item.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string path in _paths)
            {
                tracer.WriteLine("Invoking {0}", path);

                try
                {
                    // Now invoke the action
                    InvokeProvider.Item.Invoke(path, CmdletProviderContext);
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
        #endregion Command code

    }

    #endregion InvokeItemCommand

    #endregion Item commands

    #region Provider commands

    #region GetProviderCommand

    /// <summary>
    /// Gets a core command provider by name.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PSProvider", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096816")]
    [OutputType(typeof(ProviderInfo))]
    public class GetPSProviderCommand : CoreCommandBase
    {
        #region Command parameters

        /// <summary>
        /// Gets or sets the provider that will be removed.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string[] PSProvider
        {
            get => _provider;
            set => _provider = value ?? Array.Empty<string>();
        }

        #endregion Command parameters

        #region Command data
        /// <summary>
        /// The string ID of the provider to remove.
        /// </summary>
        private string[] _provider = Array.Empty<string>();

        #endregion Command data

        #region Command code

        /// <summary>
        /// Gets a provider from the core command namespace.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (PSProvider == null || PSProvider.Length == 0)
            {
                // Get all the providers

                WriteObject(SessionState.Provider.GetAll(), true);
            }
            else
            {
                foreach (string requestedProvider in PSProvider)
                {
                    PSSnapinQualifiedName pssnapinQualifiedProvider = PSSnapinQualifiedName.GetInstance(requestedProvider);

                    if (pssnapinQualifiedProvider != null && WildcardPattern.ContainsWildcardCharacters(pssnapinQualifiedProvider.ShortName))
                    {
                        // The user entered a glob string so use the WildcardPattern to
                        // compare the glob string to the provider names that exist
                        // and write out any that match

                        WildcardPattern matcher =
                            WildcardPattern.Get(
                                pssnapinQualifiedProvider.ShortName,
                                WildcardOptions.IgnoreCase);

                        foreach (ProviderInfo enumeratedProvider in SessionState.Provider.GetAll())
                        {
                            Dbg.Diagnostics.Assert(
                                enumeratedProvider != null,
                                "SessionState.Providers should return only ProviderInfo objects");

                            if (enumeratedProvider.IsMatch(matcher, pssnapinQualifiedProvider))
                            {
                                // A match was found

                                WriteObject(enumeratedProvider);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            Collection<ProviderInfo> matchingProviders =
                                SessionState.Provider.Get(requestedProvider);

                            // The provider was found

                            WriteObject(matchingProviders, true);
                        }
                        catch (ProviderNotFoundException e)
                        {
                            WriteError(
                                new ErrorRecord(
                                    e.ErrorRecord,
                                    e));
                        }
                    }
                }
            }
        }

        #endregion Command code
    }

    #endregion GetProviderCommand

    #endregion Provider commands
}
