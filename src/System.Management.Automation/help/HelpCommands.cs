// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Help;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
#if !UNIX
using Microsoft.Win32;
#endif

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements get-help command.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Help", DefaultParameterSetName = "AllUsersView", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096483")]
    public sealed class GetHelpCommand : PSCmdlet
    {
        /// <summary>
        /// Help Views.
        /// </summary>
        internal enum HelpView
        {
            Default = 0x00, // Default View
            DetailedView = 0x01,
            FullView = 0x02,
            ExamplesView = 0x03
        }

        /// <summary>
        /// Default constructor for the GetHelpCommand class.
        /// </summary>
        public GetHelpCommand()
        {
        }

        #region Cmdlet Parameters

        /// <summary>
        /// Target to search for help.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty()]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Path to provider location that user is curious about.
        /// </summary>
        [Parameter]
        public string Path { get; set; }

        /// <summary>
        /// List of help categories to search for help.
        /// </summary>
        [Parameter]
        [ValidateSet(
            "Alias", "Cmdlet", "Provider", "General", "FAQ", "Glossary", "HelpFile", "ScriptCommand", "Function", "Filter", "ExternalScript", "All", "DefaultHelp", "DscResource", "Class", "Configuration",
             IgnoreCase = true)]
        public string[] Category { get; set; }

        private readonly string _provider = string.Empty;

        /// <summary>
        /// Changes the view of HelpObject returned.
        /// </summary>
        /// <remarks>
        /// Currently we support following views:
        ///
        /// 1. Reminder (Default - Experienced User)
        /// 2. Detailed (Beginner - Beginning User)
        /// 3. Full     (All Users)
        /// 4. Examples
        /// 5. Parameters
        ///
        /// Currently we support these views only for Cmdlets.
        /// A SnapIn developer can however change these views.
        /// </remarks>
        [Parameter(ParameterSetName = "DetailedView", Mandatory = true)]
        public SwitchParameter Detailed
        {
            set
            {
                if (value.ToBool())
                {
                    _viewTokenToAdd = HelpView.DetailedView;
                }
            }
        }

        /// <summary>
        /// Changes the view of HelpObject returned.
        /// </summary>
        /// <remarks>
        /// Currently we support following views:
        ///
        /// 1. Reminder (Default - Experienced User)
        /// 2. Detailed (Beginner - Beginning User)
        /// 3. Full     (All Users)
        /// 4. Examples
        /// 5. Parameters
        ///
        /// Currently we support these views only for Cmdlets.
        /// A SnapIn developer can however change these views.
        /// </remarks>
        [Parameter(ParameterSetName = "AllUsersView")]
        public SwitchParameter Full
        {
            set
            {
                if (value.ToBool())
                {
                    _viewTokenToAdd = HelpView.FullView;
                }
            }
        }

        /// <summary>
        /// Changes the view of HelpObject returned.
        /// </summary>
        /// <remarks>
        /// Currently we support following views:
        ///
        /// 1. Reminder (Default - Experienced User)
        /// 2. Detailed (Beginner - Beginning User)
        /// 3. Full     (All Users)
        /// 4. Examples
        ///
        /// Currently we support these views only for Cmdlets.
        /// A SnapIn developer can however change these views.
        /// </remarks>
        [Parameter(ParameterSetName = "Examples", Mandatory = true)]
        public SwitchParameter Examples
        {
            set
            {
                if (value.ToBool())
                {
                    _viewTokenToAdd = HelpView.ExamplesView;
                }
            }
        }

        /// <summary>
        /// Parameter name.
        /// </summary>
        /// <remarks>
        /// Support WildCard strings as supported by WildcardPattern class.
        /// </remarks>
        [Parameter(ParameterSetName = "Parameters", Mandatory = true)]
        public string[] Parameter { get; set; }

        /// <summary>
        /// Gets and sets list of Component's to search on.
        /// </summary>
        [Parameter]
        public string[] Component { get; set; }

        /// <summary>
        /// Gets and sets list of Functionality's to search on.
        /// </summary>
        [Parameter]
        public string[] Functionality { get; set; }

        /// <summary>
        /// Gets and sets list of Role's to search on.
        /// </summary>
        [Parameter]
        public string[] Role { get; set; }

        /// <summary>
        /// This parameter,if true, will direct get-help cmdlet to
        /// navigate to a URL (stored in the command MAML file under
        /// the uri node).
        /// </summary>
        [Parameter(ParameterSetName = "Online", Mandatory = true)]
        public SwitchParameter Online
        {
            get
            {
                return _showOnlineHelp;
            }

            set
            {
                _showOnlineHelp = value;
                if (_showOnlineHelp)
                {
                    VerifyParameterForbiddenInRemoteRunspace(this, "Online");
                }
            }
        }

        private bool _showOnlineHelp;

#if !UNIX
        private GraphicalHostReflectionWrapper graphicalHostReflectionWrapper;
        private bool showWindow;

        /// <summary>
        /// Gets or sets a value indicating whether the help should be displayed in a separate window.
        /// </summary>
        [Parameter(ParameterSetName = "ShowWindow", Mandatory = true)]
        public SwitchParameter ShowWindow
        {
            get
            {
                return showWindow;
            }

            set
            {
                showWindow = value;
                if (showWindow)
                {
                    VerifyParameterForbiddenInRemoteRunspace(this, "ShowWindow");
                }
            }
        }
#endif

        // The following variable controls the view.
        private HelpView _viewTokenToAdd = HelpView.Default;

#if LEGACYTELEMETRY
        private readonly Stopwatch _timer = new Stopwatch();
        private bool _updatedHelp;
#endif

        #endregion

        #region Cmdlet API implementation

        /// <summary>
        /// Implements the BeginProcessing() method for get-help command.
        /// </summary>
        protected override void BeginProcessing()
        {
#if LEGACYTELEMETRY
            _timer.Start();
#endif
        }

        /// <summary>
        /// Implements the ProcessRecord() method for get-help command.
        /// </summary>
        protected override void ProcessRecord()
        {
            HelpSystem helpSystem = this.Context.HelpSystem;
            try
            {
#if !UNIX
                if (this.ShowWindow)
                {
                    this.graphicalHostReflectionWrapper = GraphicalHostReflectionWrapper.GetGraphicalHostReflectionWrapper(this, "Microsoft.PowerShell.Commands.Internal.HelpWindowHelper");
                }
#endif
                helpSystem.OnProgress += HelpSystem_OnProgress;

                bool failed = false;
                HelpCategory helpCategory = ToHelpCategory(Category, ref failed);

                if (failed)
                    return;

                // Validate input parameters
                ValidateAndThrowIfError(helpCategory);

                HelpRequest helpRequest = new HelpRequest(this.Name, helpCategory);

                helpRequest.Provider = _provider;
                helpRequest.Component = Component;
                helpRequest.Role = Role;
                helpRequest.Functionality = Functionality;
                helpRequest.ProviderContext = new ProviderContext(
                    this.Path,
                    this.Context.Engine.Context,
                    this.SessionState.Path);
                helpRequest.CommandOrigin = this.MyInvocation.CommandOrigin;

                // the idea is to use yield statement in the help lookup to speed up
                // perceived user experience....So HelpSystem.GetHelp returns an
                // IEnumerable..
                IEnumerable<HelpInfo> helpInfos = helpSystem.GetHelp(helpRequest);
                // HelpCommand acts differently when there is just one help object and when
                // there are more than one object...so handling this behavior through
                // some variables.
                HelpInfo firstHelpInfoObject = null;
                int countOfHelpInfos = 0;
                foreach (HelpInfo helpInfo in helpInfos)
                {
                    // honor Ctrl-C from user.
                    if (IsStopping)
                    {
                        return;
                    }

                    if (countOfHelpInfos == 0)
                    {
                        firstHelpInfoObject = helpInfo;
                    }
                    else
                    {
                        // write first help object only once.
                        if (firstHelpInfoObject != null)
                        {
                            WriteObjectsOrShowOnlineHelp(firstHelpInfoObject, false);
                            firstHelpInfoObject = null;
                        }

                        WriteObjectsOrShowOnlineHelp(helpInfo, false);
                    }

                    countOfHelpInfos++;
                }

#if LEGACYTELEMETRY
                _timer.Stop();

                if (!string.IsNullOrEmpty(Name))
                    Microsoft.PowerShell.Telemetry.Internal.TelemetryAPI.ReportGetHelpTelemetry(Name, countOfHelpInfos, _timer.ElapsedMilliseconds, _updatedHelp);
#endif
                // Write full help as there is only one help info object
                if (countOfHelpInfos == 1)
                {
                    WriteObjectsOrShowOnlineHelp(firstHelpInfoObject, true);
                }
                else if (_showOnlineHelp && (countOfHelpInfos > 1))
                {
                    throw PSTraceSource.NewInvalidOperationException(HelpErrors.MultipleOnlineTopicsNotSupported, "Online");
                }

                // show errors only if there is no wildcard search or VerboseHelpErrors is true.
                if (((countOfHelpInfos == 0) && (!WildcardPattern.ContainsWildcardCharacters(helpRequest.Target)))
                    || helpSystem.VerboseHelpErrors)
                {
                    // Check if there is any error happened. If yes,
                    // pipe out errors.
                    if (helpSystem.LastErrors.Count > 0)
                    {
                        foreach (ErrorRecord errorRecord in helpSystem.LastErrors)
                        {
                            WriteError(errorRecord);
                        }
                    }
                }
            }
            finally
            {
                helpSystem.OnProgress -= HelpSystem_OnProgress;
                HelpSystem_OnComplete();

                // finally clear the ScriptBlockAst -> Token[] cache
                helpSystem.ClearScriptBlockTokenCache();
            }
        }

        private HelpCategory ToHelpCategory(string[] category, ref bool failed)
        {
            if (category == null || category.Length == 0)
                return HelpCategory.None;

            HelpCategory helpCategory = HelpCategory.None;

            failed = false;

            for (int i = 0; i < category.Length; i++)
            {
                try
                {
                    HelpCategory temp = (HelpCategory)Enum.Parse(typeof(HelpCategory), category[i], true);

                    helpCategory |= temp;
                }
                catch (ArgumentException argumentException)
                {
                    Exception e = new HelpCategoryInvalidException(category[i], argumentException);
                    ErrorRecord errorRecord = new ErrorRecord(e, "InvalidHelpCategory", ErrorCategory.InvalidArgument, null);
                    this.WriteError(errorRecord);

                    failed = true;
                }
            }

            return helpCategory;
        }

        /// <summary>
        /// Change <paramref name="originalHelpObject"/> as per user request.
        ///
        /// This method creates a new type to the existing typenames
        /// depending on Detailed,Full,Example parameters and adds this
        /// new type(s) to the top of the list.
        /// </summary>
        /// <param name="originalHelpObject">Full help object to transform.</param>
        /// <returns>Transformed help object with new TypeNames.</returns>
        /// <remarks>If Detailed and Full are not specified, nothing is changed.</remarks>
        private PSObject TransformView(PSObject originalHelpObject)
        {
            Diagnostics.Assert(originalHelpObject != null,
                "HelpObject should not be null");

            if (_viewTokenToAdd == HelpView.Default)
            {
                s_tracer.WriteLine("Detailed, Full, Examples are not selected. Constructing default view.");
                return originalHelpObject;
            }

            string tokenToAdd = _viewTokenToAdd.ToString();
            // We are changing the types without modifying the original object.
            // The contract between help command and helpsystem does not
            // allow us to modify returned help objects.
            PSObject objectToReturn = originalHelpObject.Copy();
            objectToReturn.TypeNames.Clear();

            if (originalHelpObject.TypeNames.Count == 0)
            {
                string typeToAdd = string.Format(CultureInfo.InvariantCulture, "HelpInfo#{0}", tokenToAdd);
                objectToReturn.TypeNames.Add(typeToAdd);
            }
            else
            {
                // User request at the top..
                foreach (string typeName in originalHelpObject.TypeNames)
                {
                    // dont add new types for System.String and System.Object..
                    // as they are handled differently for F&0..(bug935095)
                    if (typeName.ToLowerInvariant().Equals("system.string") ||
                        typeName.ToLowerInvariant().Equals("system.object"))
                    {
                        continue;
                    }

                    string typeToAdd = string.Format(CultureInfo.InvariantCulture, "{0}#{1}", typeName, tokenToAdd);
                    s_tracer.WriteLine("Adding type {0}", typeToAdd);
                    objectToReturn.TypeNames.Add(typeToAdd);
                }

                // Existing typenames at the bottom..
                foreach (string typeName in originalHelpObject.TypeNames)
                {
                    s_tracer.WriteLine("Adding type {0}", typeName);
                    objectToReturn.TypeNames.Add(typeName);
                }
            }

            return objectToReturn;
        }

        /// <summary>
        /// Gets the parameter info for patterns identified by Parameter property.
        /// </summary>
        /// <param name="helpInfo">HelpInfo object to look for the parameter.</param>
        /// <returns>Array of parameter infos.</returns>
        private PSObject[] GetParameterInfo(HelpInfo helpInfo)
        {
            List<PSObject> parameterInfosList = new List<PSObject>(Parameter.Length);

            foreach (var parameter in Parameter)
            {
                foreach (var parameterInfo in helpInfo.GetParameter(parameter))
                {
                    parameterInfosList.Add(parameterInfo);
                }
            }

            return parameterInfosList.ToArray();
        }

        /// <summary>
        /// Gets the parameter info for patterns identified by Parameter property.
        /// Writes the parameter info(s) to the output stream. An error is thrown
        /// if a parameter with a given pattern is not found.
        /// </summary>
        /// <param name="helpInfo">HelpInfo Object to look for the parameter.</param>
        private void GetAndWriteParameterInfo(HelpInfo helpInfo)
        {
            s_tracer.WriteLine("Searching parameters for {0}", helpInfo.Name);

            PSObject[] pInfos = GetParameterInfo(helpInfo);

            if ((pInfos == null) || (pInfos.Length == 0))
            {
                Exception innerException = PSTraceSource.NewArgumentException("Parameter",
                    HelpErrors.NoParmsFound, Parameter);
                WriteError(new ErrorRecord(innerException, "NoParmsFound", ErrorCategory.InvalidArgument, helpInfo));
            }
            else
            {
                foreach (PSObject pInfo in pInfos)
                {
                    WriteObject(pInfo);
                }
            }
        }

        /// <summary>
        /// Validates input parameters.
        /// </summary>
        /// <param name="cat">Category specified by the user.</param>
        /// <exception cref="ArgumentException">
        /// If the request cant be serviced.
        /// </exception>
        private void ValidateAndThrowIfError(HelpCategory cat)
        {
            if (cat == HelpCategory.None)
            {
                return;
            }

            // categories that support -Parameter, -Role, -Functionality, -Component parameters
            const HelpCategory supportedCategories =
                HelpCategory.Alias | HelpCategory.Cmdlet | HelpCategory.ExternalScript |
                HelpCategory.Filter | HelpCategory.Function | HelpCategory.ScriptCommand;

            if ((cat & supportedCategories) == 0)
            {
                if (Parameter != null)
                {
                    throw PSTraceSource.NewArgumentException("Parameter",
                        HelpErrors.ParamNotSupported, "-Parameter");
                }

                if (Component != null)
                {
                    throw PSTraceSource.NewArgumentException("Component",
                        HelpErrors.ParamNotSupported, "-Component");
                }

                if (Role != null)
                {
                    throw PSTraceSource.NewArgumentException("Role",
                        HelpErrors.ParamNotSupported, "-Role");
                }

                if (Functionality != null)
                {
                    throw PSTraceSource.NewArgumentException("Functionality",
                        HelpErrors.ParamNotSupported, "-Functionality");
                }
            }
        }

        /// <summary>
        /// Helper method used to Write the help object onto the output
        /// stream or show online help (URI extracted from the HelpInfo)
        /// object.
        /// </summary>
        private void WriteObjectsOrShowOnlineHelp(HelpInfo helpInfo, bool showFullHelp)
        {
            if (helpInfo != null)
            {
                // online help can be showed only if showFullHelp is true..
                // showFullHelp will be false when the help tries to display multiple help topics..
                // -Online should not work when multiple help topics are displayed.
                if (showFullHelp && _showOnlineHelp)
                {
                    bool onlineUriFound = false;
                    // show online help
                    s_tracer.WriteLine("Preparing to show help online.");
                    Uri onlineUri = helpInfo.GetUriForOnlineHelp();
                    if (onlineUri != null)
                    {
                        onlineUriFound = true;
                        LaunchOnlineHelp(onlineUri);
                        return;
                    }

                    if (!onlineUriFound)
                    {
                        throw PSTraceSource.NewInvalidOperationException(HelpErrors.NoURIFound);
                    }
                }
#if !UNIX
                else if (showFullHelp && ShowWindow)
                {
                    graphicalHostReflectionWrapper.CallStaticMethod("ShowHelpWindow", helpInfo.FullHelp, this);
                }
#endif
                else
                {
                    // show inline help
                    if (showFullHelp)
                    {
                        if (Parameter != null)
                        {
                            GetAndWriteParameterInfo(helpInfo);
                        }
                        else
                        {
                            PSObject objectToReturn = TransformView(helpInfo.FullHelp);
                            objectToReturn.IsHelpObject = true;
                            WriteObject(objectToReturn);
                        }
                    }
                    else
                    {
                        if (Parameter != null)
                        {
                            PSObject[] pInfos = GetParameterInfo(helpInfo);

                            if ((pInfos == null) || (pInfos.Length == 0))
                            {
                                return;
                            }
                        }

                        WriteObject(helpInfo.ShortHelp);
                    }
                }
            }
        }

        /// <summary>
        /// Opens the Uri. System's default application will be used
        /// to show the uri.
        /// </summary>
        /// <param name="uriToLaunch"></param>
        private void LaunchOnlineHelp(Uri uriToLaunch)
        {
            Diagnostics.Assert(uriToLaunch != null, "uriToLaunch should not be null");

            if (!uriToLaunch.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                !uriToLaunch.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                throw PSTraceSource.NewInvalidOperationException(HelpErrors.ProtocolNotSupported,
                    uriToLaunch.ToString(),
                    "http",
                    "https");
            }

            // we use this test hook is to avoid actually calling out to another process
            if (InternalTestHooks.BypassOnlineHelpRetrieval)
            {
                this.WriteObject(string.Format(CultureInfo.InvariantCulture, HelpDisplayStrings.OnlineHelpUri, uriToLaunch.OriginalString));
                return;
            }

            Exception exception = null;
            bool wrapCaughtException = true;
            try
            {
                this.WriteVerbose(string.Format(CultureInfo.InvariantCulture, HelpDisplayStrings.OnlineHelpUri, uriToLaunch.OriginalString));
                System.Diagnostics.Process browserProcess = new System.Diagnostics.Process();

                if (Platform.IsNanoServer || Platform.IsIoT)
                {
                    // We cannot open the URL in browser on headless SKUs.
                    wrapCaughtException = false;
                    exception = PSTraceSource.NewInvalidOperationException(HelpErrors.CannotLaunchURI, uriToLaunch.OriginalString);
                }
                else
                {
                    browserProcess.StartInfo.FileName = uriToLaunch.OriginalString;
                    browserProcess.StartInfo.UseShellExecute = true;
                    browserProcess.Start();
                }
            }
            catch (InvalidOperationException ioe)
            {
                exception = ioe;
            }
            catch (System.ComponentModel.Win32Exception we)
            {
                exception = we;
            }

            if (exception != null)
            {
                if (wrapCaughtException)
                    throw PSTraceSource.NewInvalidOperationException(exception, HelpErrors.CannotLaunchURI, uriToLaunch.OriginalString);
                else
                    throw exception;
            }
        }

        #endregion

        private void HelpSystem_OnProgress(object sender, HelpProgressEventArgs arg)
        {
            var record = new ProgressRecord(0, this.CommandInfo.Name, arg.Activity)
            {
                PercentComplete = arg.PercentComplete
            };

            WriteProgress(record);
        }

        private void HelpSystem_OnComplete()
        {
            var record = new ProgressRecord(0, this.CommandInfo.Name, "Completed")
            {
                RecordType = ProgressRecordType.Completed
            };

            WriteProgress(record);
        }

        #region Helper methods for verification of parameters against NoLanguage mode

        internal static void VerifyParameterForbiddenInRemoteRunspace(Cmdlet cmdlet, string parameterName)
        {
            if (NativeCommandProcessor.IsServerSide)
            {
                string message = StringUtil.Format(CommandBaseStrings.ParameterNotValidInRemoteRunspace,
                    cmdlet.MyInvocation.InvocationName,
                    parameterName);
                Exception e = new InvalidOperationException(message);
                ErrorRecord errorRecord = new ErrorRecord(e, "ParameterNotValidInRemoteRunspace", ErrorCategory.InvalidArgument, null);
                cmdlet.ThrowTerminatingError(errorRecord);
            }
        }

        #endregion

        #region trace

        [TraceSourceAttribute("GetHelpCommand ", "GetHelpCommand ")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("GetHelpCommand ", "GetHelpCommand ");

        #endregion
    }

    /// <summary>
    /// Helper methods used as powershell extension from a types file.
    /// </summary>
    public static class GetHelpCodeMethods
    {
        /// <summary>
        /// Checks whether the default runspace associated with the current thread has the standard Get-Help cmdlet.
        /// </summary>
        /// <returns>True if Get-Help is found, false otherwise.</returns>
        private static bool DoesCurrentRunspaceIncludeCoreHelpCmdlet()
        {
            InitialSessionState iss = Runspace.DefaultRunspace.InitialSessionState;
            if (iss is null)
            {
                return false;
            }

            Collection<SessionStateCommandEntry> getHelpEntries = iss.Commands["Get-Help"];
            SessionStateCommandEntry getHelpEntry = null;
            for (int i = 0; i < getHelpEntries.Count; ++i)
            {
                if (getHelpEntries[i].Visibility is not SessionStateEntryVisibility.Public)
                {
                    continue;
                }

                // If we have multiple entries for Get-Help,
                // our assumption is that the standard Get-Help is not available.
                if (getHelpEntry is not null)
                {
                    return false;
                }

                getHelpEntry = getHelpEntries[i];
            }

            return getHelpEntry is SessionStateCmdletEntry getHelpCmdlet
                && getHelpCmdlet.ImplementingType == typeof(GetHelpCommand);
        }

        /// <summary>
        /// Retrieves the HelpUri given a CommandInfo instance.
        /// </summary>
        /// <param name="commandInfoPSObject">
        /// CommandInfo instance wrapped as PSObject
        /// </param>
        /// <returns>
        /// null if <paramref name="commandInfoPSObject"/> is not a CommandInfo type.
        /// null if HelpUri could not be retrieved either from CommandMetadata or
        /// help content.
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings")]
        public static string GetHelpUri(PSObject commandInfoPSObject)
        {
            if (commandInfoPSObject == null)
            {
                return string.Empty;
            }

            CommandInfo cmdInfo = PSObject.Base(commandInfoPSObject) as CommandInfo;
            // GetHelpUri helper method is expected to be used only by System.Management.Automation.CommandInfo
            // objects from types.ps1xml
            if ((cmdInfo == null) || (string.IsNullOrEmpty(cmdInfo.Name)))
            {
                return string.Empty;
            }

            // The type checking is needed to avoid a try..catch exception block as
            // the CommandInfo.CommandMetadata throws an InvalidOperationException
            // instead of returning null.
            if ((cmdInfo is CmdletInfo) || (cmdInfo is FunctionInfo) ||
                (cmdInfo is ExternalScriptInfo) || (cmdInfo is ScriptInfo))
            {
                if (!string.IsNullOrEmpty(cmdInfo.CommandMetadata.HelpUri))
                {
                    return cmdInfo.CommandMetadata.HelpUri;
                }
            }

            AliasInfo aliasInfo = cmdInfo as AliasInfo;
            if ((aliasInfo != null) &&
                (aliasInfo.ExternalCommandMetadata != null) &&
                (!string.IsNullOrEmpty(aliasInfo.ExternalCommandMetadata.HelpUri)))
            {
                return aliasInfo.ExternalCommandMetadata.HelpUri;
            }

            // if everything else fails..depend on Get-Help infrastructure to get us the Uri.
            string cmdName = cmdInfo.Name;
            if (!string.IsNullOrEmpty(cmdInfo.ModuleName))
            {
                cmdName = string.Format(CultureInfo.InvariantCulture,
                                        "{0}\\{1}", cmdInfo.ModuleName, cmdInfo.Name);
            }

            if (DoesCurrentRunspaceIncludeCoreHelpCmdlet())
            {
                // Win8: 651300 if core get-help is present in the runspace (and it is the only get-help command), use
                // help system directly and avoid perf penalty.
                var currentContext = System.Management.Automation.Runspaces.LocalPipeline.GetExecutionContextFromTLS();
                if ((currentContext != null) && (currentContext.HelpSystem != null))
                {
                    HelpRequest helpRequest = new HelpRequest(cmdName, cmdInfo.HelpCategory);
                    helpRequest.ProviderContext = new ProviderContext(
                        string.Empty,
                        currentContext,
                        currentContext.SessionState.Path);
                    helpRequest.CommandOrigin = CommandOrigin.Runspace;
                    foreach (
                        Uri result in
                            currentContext.HelpSystem.ExactMatchHelp(helpRequest).Select(
                                helpInfo => helpInfo.GetUriForOnlineHelp()).Where(static result => result != null))
                    {
                        return result.OriginalString;
                    }
                }
            }
            else
            {
                // win8: 546025. Using Get-Help as command, instead of calling HelpSystem.ExactMatchHelp
                // for the following reasons:
                // 1. Exchange creates proxies for Get-Command and Get-Help in their scenario
                // 2. This method is primarily used to get uri faster while serializing the CommandInfo objects (from Get-Command)
                // 3. Exchange uses Get-Help proxy to not call Get-Help cmdlet at-all while serializing CommandInfo objects
                // 4. Using HelpSystem directly will not allow Get-Help proxy to do its job.
                var getHelpPS = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)
                    .AddCommand("get-help")
                    .AddParameter("Name", cmdName)
                    .AddParameter("Category", cmdInfo.HelpCategory.ToString());
                try
                {
                    Collection<PSObject> helpInfos = getHelpPS.Invoke();

                    if (helpInfos != null)
                    {
                        for (int index = 0; index < helpInfos.Count; index++)
                        {
                            HelpInfo helpInfo;
                            if (LanguagePrimitives.TryConvertTo<HelpInfo>(helpInfos[index], out helpInfo))
                            {
                                Uri result = helpInfo.GetUriForOnlineHelp();
                                if (result != null)
                                {
                                    return result.OriginalString;
                                }
                            }
                            else
                            {
                                Uri result = BaseCommandHelpInfo.GetUriFromCommandPSObject(helpInfos[index]);
                                return (result != null) ? result.OriginalString : string.Empty;
                            }
                        }
                    }
                }
                finally
                {
                    getHelpPS.Dispose();
                }
            }

            return string.Empty;
        }
    }
}
