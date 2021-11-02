// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Xml;

using Dbg = System.Management.Automation.Diagnostics;
using System.Management.Automation.Help;
using System.Reflection;

namespace System.Management.Automation
{
    /// <summary>
    /// Class CommandHelpProvider implement the help provider for commands.
    /// </summary>
    /// <remarks>
    /// Command Help information are stored in 'help.xml' files. Location of these files
    /// can be found from through the engine execution context.
    /// </remarks>
    internal class CommandHelpProvider : HelpProviderWithCache
    {
        /// <summary>
        /// Constructor for CommandHelpProvider.
        /// </summary>
        internal CommandHelpProvider(HelpSystem helpSystem) : base(helpSystem)
        {
            _context = helpSystem.ExecutionContext;
        }

        /// <summary>
        /// </summary>
        static CommandHelpProvider()
        {
            s_engineModuleHelpFileCache.Add("Microsoft.PowerShell.Diagnostics", "Microsoft.PowerShell.Commands.Diagnostics.dll-Help.xml");
            s_engineModuleHelpFileCache.Add("Microsoft.PowerShell.Core", "System.Management.Automation.dll-Help.xml");
            s_engineModuleHelpFileCache.Add("Microsoft.PowerShell.Utility", "Microsoft.PowerShell.Commands.Utility.dll-Help.xml");
            s_engineModuleHelpFileCache.Add("Microsoft.PowerShell.Host", "Microsoft.PowerShell.ConsoleHost.dll-Help.xml");
            s_engineModuleHelpFileCache.Add("Microsoft.PowerShell.Management", "Microsoft.PowerShell.Commands.Management.dll-Help.xml");
            s_engineModuleHelpFileCache.Add("Microsoft.PowerShell.Security", "Microsoft.PowerShell.Security.dll-Help.xml");
            s_engineModuleHelpFileCache.Add("Microsoft.WSMan.Management", "Microsoft.Wsman.Management.dll-Help.xml");
        }

        private static readonly Dictionary<string, string> s_engineModuleHelpFileCache = new Dictionary<string, string>();

        private readonly ExecutionContext _context;

        #region Common Properties

        /// <summary>
        /// Name of this provider.
        /// </summary>
        /// <value>Name of this provider</value>
        internal override string Name
        {
            get
            {
                return "Command Help Provider";
            }
        }

        /// <summary>
        /// Help category for this provider, which is a constant: HelpCategory.Command.
        /// </summary>
        /// <value>Help category for this provider</value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return
                    HelpCategory.Alias |
                    HelpCategory.Cmdlet;
            }
        }

        #endregion

        #region Help Provider Interface

        private static void GetModulePaths(CommandInfo commandInfo, out string moduleName, out string moduleDir, out string nestedModulePath)
        {
            Dbg.Assert(commandInfo != null, "Caller should verify that commandInfo != null");

            CmdletInfo cmdletInfo = commandInfo as CmdletInfo;
            IScriptCommandInfo scriptCommandInfo = commandInfo as IScriptCommandInfo;

            string cmdNameWithoutPrefix = null;
            bool testWithoutPrefix = false;

            moduleName = null;
            moduleDir = null;
            nestedModulePath = null;

            if (commandInfo.Module != null)
            {
                moduleName = commandInfo.Module.Name;
                moduleDir = commandInfo.Module.ModuleBase;

                if (!string.IsNullOrEmpty(commandInfo.Prefix))
                {
                    testWithoutPrefix = true;
                    cmdNameWithoutPrefix = Microsoft.PowerShell.Commands.ModuleCmdletBase.RemovePrefixFromCommandName(commandInfo.Name, commandInfo.Prefix);
                }

                if (commandInfo.Module.NestedModules != null)
                {
                    foreach (PSModuleInfo nestedModule in commandInfo.Module.NestedModules)
                    {
                        if (cmdletInfo != null &&
                             (nestedModule.ExportedCmdlets.ContainsKey(commandInfo.Name) ||
                               (testWithoutPrefix && nestedModule.ExportedCmdlets.ContainsKey(cmdNameWithoutPrefix))))
                        {
                            nestedModulePath = nestedModule.Path;
                            break;
                        }
                        else if (scriptCommandInfo != null &&
                                  (nestedModule.ExportedFunctions.ContainsKey(commandInfo.Name) ||
                                    (testWithoutPrefix && nestedModule.ExportedFunctions.ContainsKey(cmdNameWithoutPrefix))))
                        {
                            nestedModulePath = nestedModule.Path;
                            break;
                        }
                    }
                }
            }
        }

        private static string GetHelpName(CommandInfo commandInfo)
        {
            Dbg.Assert(commandInfo != null, "Caller should verify that commandInfo != null");

            CmdletInfo cmdletInfo = commandInfo as CmdletInfo;

            if (cmdletInfo != null)
            {
                return cmdletInfo.FullName;
            }

            return commandInfo.Name;
        }

        private HelpInfo GetHelpInfoFromHelpFile(CommandInfo commandInfo, string helpFileToFind, Collection<string> searchPaths, bool reportErrors, out string helpFile)
        {
            Dbg.Assert(commandInfo != null, "Caller should verify that commandInfo != null");
            Dbg.Assert(helpFileToFind != null, "Caller should verify that helpFileToFind != null");

            CmdletInfo cmdletInfo = commandInfo as CmdletInfo;
            IScriptCommandInfo scriptCommandInfo = commandInfo as IScriptCommandInfo;

            HelpInfo result = null;

            helpFile = MUIFileSearcher.LocateFile(helpFileToFind, searchPaths);

            if (!string.IsNullOrEmpty(helpFile))
            {
                if (!_helpFiles.Contains(helpFile))
                {
                    if (cmdletInfo != null)
                    {
                        LoadHelpFile(helpFile, cmdletInfo.ModuleName, cmdletInfo.Name, reportErrors);
                    }
                    else if (scriptCommandInfo != null)
                    {
                        LoadHelpFile(helpFile, helpFile, commandInfo.Name, reportErrors);
                    }
                }

                if (cmdletInfo != null)
                {
                    result = GetFromCommandCacheOrCmdletInfo(cmdletInfo);
                }
                else if (scriptCommandInfo != null)
                {
                    result = GetFromCommandCache(helpFile, commandInfo);
                }
            }

            return result;
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "TestUri is created only to check for source helpUriFromDotLink errors.")]
        private HelpInfo GetHelpInfo(CommandInfo commandInfo, bool reportErrors, bool searchOnlyContent)
        {
            Dbg.Assert(commandInfo != null, "Caller should verify that commandInfo != null");

            HelpInfo result = null; // The help result
            string helpFile = null; // The file that contains the help info
            string helpUri = null;
            string helpUriFromDotLink = null;

            CmdletInfo cmdletInfo = commandInfo as CmdletInfo;
            IScriptCommandInfo scriptCommandInfo = commandInfo as IScriptCommandInfo;
            FunctionInfo functionInfo = commandInfo as FunctionInfo;
            bool isCmdlet = cmdletInfo != null;
            bool isScriptCommand = scriptCommandInfo != null;
            bool isFunction = functionInfo != null;

            string moduleName = null;
            string moduleDir = null;
            string nestedModulePath = null;

            // When InternalTestHooks.BypassOnlineHelpRetrieval is enable, we force get-help to generate a metadata
            // driven object, which includes a helpUri that points to the fwlink defined in the cmdlet code.
            // This means that we are not going to load the help content from the GetFromCommandCache and
            // we are not going to read the help file.

            // Only gets help for Cmdlet or script command
            if (!isCmdlet && !isScriptCommand)
                return null;

            // Check if the help of the command is already in the cache.
            // If not, try load the file specified by HelpFile property and retrieve help.
            if (isCmdlet && !InternalTestHooks.BypassOnlineHelpRetrieval)
            {
                result = GetFromCommandCache(cmdletInfo.ModuleName, cmdletInfo.Name, cmdletInfo.HelpCategory);

                if (result == null)
                {
                    // Try load the help file specified by CmdletInfo.HelpFile property
                    helpFile = FindHelpFile(cmdletInfo);
                    if (!string.IsNullOrEmpty(helpFile) && !_helpFiles.Contains(helpFile))
                    {
                        LoadHelpFile(helpFile, cmdletInfo.ModuleName, cmdletInfo.Name, reportErrors);
                    }

                    result = GetFromCommandCacheOrCmdletInfo(cmdletInfo);
                }
            }
            else if (isFunction)
            {
                // Try load the help file specified by FunctionInfo.HelpFile property
                helpFile = functionInfo.HelpFile;
                if (!string.IsNullOrEmpty(helpFile))
                {
                    if (!_helpFiles.Contains(helpFile))
                    {
                        LoadHelpFile(helpFile, helpFile, commandInfo.Name, reportErrors);
                    }

                    result = GetFromCommandCache(helpFile, commandInfo);
                }
            }

            // For scripts, try to retrieve the help from the file specified by .ExternalHelp directive
            if (result == null && isScriptCommand)
            {
                ScriptBlock sb = null;
                try
                {
                    sb = scriptCommandInfo.ScriptBlock;
                }
                catch (RuntimeException)
                {
                    // parsing errors should not block searching for help
                    return null;
                }

                if (sb != null)
                {
                    helpFile = null;
                    // searchOnlyContent == true means get-help is looking into the content, in this case we dont
                    // want to download the content from the remote machine. Reason: In Exchange scenario there
                    // are ~700 proxy commands, downloading help for all the commands and searching in that
                    // content takes a lot of time (in the order of 30 minutes) for their scenarios.
                    result = sb.GetHelpInfo(_context, commandInfo, searchOnlyContent, HelpSystem.ScriptBlockTokenCache,
                        out helpFile, out helpUriFromDotLink);

                    if (!string.IsNullOrEmpty(helpUriFromDotLink))
                    {
                        try
                        {
                            Uri testUri = new Uri(helpUriFromDotLink);
                            helpUri = helpUriFromDotLink;
                        }
                        catch (UriFormatException)
                        {
                            // Do not add if helpUriFromDotLink is not a URI
                        }
                    }

                    if (result != null)
                    {
                        Uri uri = result.GetUriForOnlineHelp();

                        if (uri != null)
                        {
                            helpUri = uri.ToString();
                        }
                    }

                    if (!string.IsNullOrEmpty(helpFile) && !InternalTestHooks.BypassOnlineHelpRetrieval)
                    {
                        if (!_helpFiles.Contains(helpFile))
                        {
                            LoadHelpFile(helpFile, helpFile, commandInfo.Name, reportErrors);
                        }

                        result = GetFromCommandCache(helpFile, commandInfo) ?? result;
                    }
                }
            }

            // If the above fails to get help, try search for a file called <ModuleName>-Help.xml
            // in the appropriate UI culture subfolder of ModuleBase, and retrieve help
            // If still not able to get help, try search for a file called <NestedModuleName>-Help.xml
            // under the ModuleBase and the NestedModule's directory, and retrieve help
            if (result == null && !InternalTestHooks.BypassOnlineHelpRetrieval)
            {
                // Get the name and ModuleBase directory of the command's module
                // and the nested module that implements the command
                GetModulePaths(commandInfo, out moduleName, out moduleDir, out nestedModulePath);

                var userHomeHelpPath = HelpUtils.GetUserHomeHelpSearchPath();

                Collection<string> searchPaths = new Collection<string>() { userHomeHelpPath };

                if (!string.IsNullOrEmpty(moduleDir))
                {
                    searchPaths.Add(moduleDir);
                }

                if (!string.IsNullOrEmpty(userHomeHelpPath) && !string.IsNullOrEmpty(moduleName))
                {
                    searchPaths.Add(Path.Combine(userHomeHelpPath, moduleName));
                }

                if (!string.IsNullOrEmpty(moduleName) && !string.IsNullOrEmpty(moduleDir))
                {
                    // Search for <ModuleName>-Help.xml under ModuleBase folder
                    string helpFileToFind = moduleName + "-Help.xml";
                    result = GetHelpInfoFromHelpFile(commandInfo, helpFileToFind, searchPaths, reportErrors, out helpFile);
                }

                if (result == null && !string.IsNullOrEmpty(nestedModulePath))
                {
                    // Search for <NestedModuleName>-Help.xml under both ModuleBase and NestedModule's directory
                    searchPaths.Add(Path.GetDirectoryName(nestedModulePath));
                    string helpFileToFind = Path.GetFileName(nestedModulePath) + "-Help.xml";
                    result = GetHelpInfoFromHelpFile(commandInfo, helpFileToFind, searchPaths, reportErrors, out helpFile);
                }
            }

            // Set the HelpFile property to the file that contains the help content
            if (result != null && !string.IsNullOrEmpty(helpFile))
            {
                if (isCmdlet)
                {
                    cmdletInfo.HelpFile = helpFile;
                }
                else if (isFunction)
                {
                    functionInfo.HelpFile = helpFile;
                }
            }

            // If the above fails to get help, construct an HelpInfo object using the syntax and definition of the command
            if (result == null)
            {
                if (commandInfo.CommandType == CommandTypes.ExternalScript ||
                    commandInfo.CommandType == CommandTypes.Script)
                {
                    result = SyntaxHelpInfo.GetHelpInfo(commandInfo.Name, commandInfo.Syntax, commandInfo.HelpCategory);
                }
                else
                {
                    PSObject helpInfo = Help.DefaultCommandHelpObjectBuilder.GetPSObjectFromCmdletInfo(commandInfo);

                    helpInfo.TypeNames.Clear();
                    helpInfo.TypeNames.Add(DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp);
                    helpInfo.TypeNames.Add("CmdletHelpInfo");
                    helpInfo.TypeNames.Add("HelpInfo");

                    result = new MamlCommandHelpInfo(helpInfo, commandInfo.HelpCategory);
                }
            }

            if (result != null)
            {
                if (isScriptCommand && result.GetUriForOnlineHelp() == null)
                {
                    if (!string.IsNullOrEmpty(commandInfo.CommandMetadata.HelpUri))
                    {
                        DefaultCommandHelpObjectBuilder.AddRelatedLinksProperties(result.FullHelp, commandInfo.CommandMetadata.HelpUri);
                    }
                    else if (!string.IsNullOrEmpty(helpUri))
                    {
                        DefaultCommandHelpObjectBuilder.AddRelatedLinksProperties(result.FullHelp, helpUri);
                    }
                }

                if (isCmdlet && result.FullHelp.Properties["PSSnapIn"] == null)
                {
                    result.FullHelp.Properties.Add(new PSNoteProperty("PSSnapIn", cmdletInfo.PSSnapIn));
                }

                if (result.FullHelp.Properties["ModuleName"] == null)
                {
                    result.FullHelp.Properties.Add(new PSNoteProperty("ModuleName", commandInfo.ModuleName));
                }
            }

            return result;
        }

        /// <summary>
        /// ExactMatchHelp implementation for this help provider.
        /// </summary>
        /// <remarks>
        /// ExactMatchHelp is overridden instead of DoExactMatchHelp to make sure
        /// all help item retrieval will go through command discovery. Because each
        /// help file can contain multiple help items for different commands. Directly
        /// retrieve help cache can result in a invalid command to contain valid
        /// help item. Forcing each ExactMatchHelp to go through command discovery
        /// will make sure helpInfo for invalid command will not be returned.
        /// </remarks>
        /// <param name="helpRequest">Help request object.</param>
        /// <returns></returns>
        internal override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            int countHelpInfosFound = 0;
            string target = helpRequest.Target;
            // this is for avoiding duplicate result from help output.
            var allHelpNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            CommandSearcher searcher = GetCommandSearcherForExactMatch(target, _context);

            while (searcher.MoveNext())
            {
                CommandInfo current = ((IEnumerator<CommandInfo>)searcher).Current;

                if (!SessionState.IsVisible(helpRequest.CommandOrigin, current))
                {
                    // this command is not visible to the user (from CommandOrigin) so
                    // dont show help topic for it.
                    continue;
                }

                HelpInfo helpInfo = GetHelpInfo(current, true, false);
                string helpName = GetHelpName(current);

                if (helpInfo != null && !string.IsNullOrEmpty(helpName))
                {
                    if (helpInfo.ForwardHelpCategory == helpRequest.HelpCategory &&
                        helpInfo.ForwardTarget.Equals(helpRequest.Target, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new PSInvalidOperationException(HelpErrors.CircularDependencyInHelpForwarding);
                    }

                    if (allHelpNames.Contains(helpName))
                        continue;

                    if (!Match(helpInfo, helpRequest, current))
                    {
                        continue;
                    }

                    countHelpInfosFound++;
                    allHelpNames.Add(helpName);
                    yield return helpInfo;

                    if ((countHelpInfosFound >= helpRequest.MaxResults) && (helpRequest.MaxResults > 0))
                        yield break;
                }
            }
        }

        private static string GetCmdletAssemblyPath(CmdletInfo cmdletInfo)
        {
            if (cmdletInfo == null)
                return null;

            if (cmdletInfo.ImplementingType == null)
                return null;

            return Path.GetDirectoryName(cmdletInfo.ImplementingType.Assembly.Location);
        }

        /// <summary>
        /// This is a hashtable to track which help files are loaded already.
        ///
        /// This will avoid one help file getting loaded again and again.
        /// (Which should not happen unless some commandlet is pointing
        /// to a help file that actually doesn't contain the help for it).
        /// </summary>
        private readonly Hashtable _helpFiles = new Hashtable();

        private string GetHelpFile(string helpFile, CmdletInfo cmdletInfo)
        {
            string helpFileToLoad = helpFile;

            // Get the mshsnapinfo object for this cmdlet.
            PSSnapInInfo mshSnapInInfo = cmdletInfo.PSSnapIn;

            // Search fallback
            // 1. If cmdletInfo.HelpFile is a full path to an existing file, directly load that file
            // 2. If PSSnapInInfo exists, then always look in the application base of the mshsnapin
            // Otherwise,
            //    Look in the default search path and cmdlet assembly path
            Collection<string> searchPaths = new Collection<string>();

            if (!File.Exists(helpFileToLoad))
            {
                helpFileToLoad = Path.GetFileName(helpFileToLoad);

                if (mshSnapInInfo != null)
                {
                    Diagnostics.Assert(!string.IsNullOrEmpty(mshSnapInInfo.ApplicationBase),
                        "Application Base is null or empty.");
                    // not minishell case..
                    // we have to search only in the application base for a mshsnapin...
                    // if you create an absolute path for helpfile, then MUIFileSearcher
                    // will look only in that path.

                    searchPaths.Add(HelpUtils.GetUserHomeHelpSearchPath());
                    searchPaths.Add(mshSnapInInfo.ApplicationBase);
                }
                else if (cmdletInfo.Module != null && !string.IsNullOrEmpty(cmdletInfo.Module.Path) && !string.IsNullOrEmpty(cmdletInfo.Module.ModuleBase))
                {
                    searchPaths.Add(HelpUtils.GetModuleBaseForUserHelp(cmdletInfo.Module.ModuleBase, cmdletInfo.Module.Name));
                    searchPaths.Add(cmdletInfo.Module.ModuleBase);
                }
                else
                {
                    searchPaths.Add(HelpUtils.GetUserHomeHelpSearchPath());
                    searchPaths.Add(GetDefaultShellSearchPath());
                    searchPaths.Add(GetCmdletAssemblyPath(cmdletInfo));
                }
            }
            else
            {
                helpFileToLoad = Path.GetFullPath(helpFileToLoad);
            }

            string location = MUIFileSearcher.LocateFile(helpFileToLoad, searchPaths);

            // let caller take care of getting help info in a different way
            // like "get-command -syntax"
            if (string.IsNullOrEmpty(location))
            {
                s_tracer.WriteLine("Unable to load file {0}", helpFileToLoad);
            }

            return location;
        }

        /// <summary>
        /// Finds a help file associated with the given cmdlet.
        /// </summary>
        /// <param name="cmdletInfo"></param>
        /// <returns></returns>
        private string FindHelpFile(CmdletInfo cmdletInfo)
        {
            if (InternalTestHooks.BypassOnlineHelpRetrieval)
            {
                // By returning null, we force get-help to generate a metadata driven help object,
                // which includes a helpUri that points to the fwlink defined in the cmdlet code.
                return null;
            }

            if (cmdletInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cmdletInfo));
            }

            // Get the help file name from the cmdlet metadata
            string helpFile = cmdletInfo.HelpFile;

            if (string.IsNullOrEmpty(helpFile))
            {
                if (cmdletInfo.Module != null)
                {
                    if (InitialSessionState.IsEngineModule(cmdletInfo.Module.Name))
                    {
                        return System.IO.Path.Combine(cmdletInfo.Module.ModuleBase, CultureInfo.CurrentCulture.Name, s_engineModuleHelpFileCache[cmdletInfo.Module.Name]);
                    }
                }

                return helpFile;
            }

            // This is the path to the help file.
            string location = null;

            if (helpFile.EndsWith(".ni.dll-Help.xml", StringComparison.OrdinalIgnoreCase))
            {
                // For PowerShell on OneCore, we ship Ngen binaries. As a result, the name of the assembly now contains '.ni' on it,
                // e.g., <AssemblyName>.ni.dll as supposed to <AssemblyName>.dll.

                // When cmdlet metadata is generated for the 'HelpFile' field, we use the name assembly and we append '-Help.xml' to it.
                // Because of this, if the cmdlet is part of an Ngen assembly, then 'HelpFile' field will be pointing to a help file which does not exist.
                // If this is the case, we remove '.ni' from the help file name and try again.
                // For example:
                // Ngen assembly name: Microsoft.PowerShell.Commands.Management.ni.dll
                // Cmdlet metadata 'HelpFile': Microsoft.PowerShell.Commands.Management.ni.dll-Help.xml
                // Actual help file name: Microsoft.PowerShell.Commands.Management.dll-Help.xml

                // Make sure that the assembly name contains more than '.ni.dll'
                string assemblyName = helpFile.Replace(".ni.dll-Help.xml", string.Empty);

                if (!string.IsNullOrEmpty(assemblyName))
                {
                    // In the first try, we remove '.ni' from the assembly name and we attempt to find the corresponding help file.
                    string helpFileName = cmdletInfo.HelpFile.Replace(".ni.dll-Help.xml", ".dll-Help.xml");
                    location = GetHelpFile(helpFileName, cmdletInfo);

                    if (string.IsNullOrEmpty(location))
                    {
                        // If the help file could not be found, then it is possible that the actual assembly name is something like
                        // <Name>.ni.dll, e.g., MyAssembly.ni.dll, so let's try to find the original help file in the cmdlet metadata.
                        location = GetHelpFile(helpFile, cmdletInfo);
                    }
                }
                else
                {
                    // the assembly name is actually '.ni.dll'.
                    location = GetHelpFile(helpFile, cmdletInfo);
                }
            }
            else
            {
                location = GetHelpFile(helpFile, cmdletInfo);
            }

            return location;
        }

        private void LoadHelpFile(string helpFile, string helpFileIdentifier, string commandName, bool reportErrors)
        {
            Exception e = null;
            try
            {
                LoadHelpFile(helpFile, helpFileIdentifier);
            }
            catch (IOException ioException)
            {
                e = ioException;
            }
            catch (System.Security.SecurityException securityException)
            {
                e = securityException;
            }
            catch (XmlException xmlException)
            {
                e = xmlException;
            }
            catch (NotSupportedException notSupportedException)
            {
                e = notSupportedException;
            }
            catch (UnauthorizedAccessException unauthorizedAccessException)
            {
                e = unauthorizedAccessException;
            }
            catch (InvalidOperationException invalidOperationException)
            {
                e = invalidOperationException;
            }

            if (reportErrors && (e != null))
            {
                ReportHelpFileError(e, commandName, helpFile);
            }
        }

        /// <summary>
        /// Load help file for HelpInfo objects. The HelpInfo objects will be
        /// put into help cache.
        /// </summary>
        /// <remarks>
        /// 1. Needs to pay special attention about error handling in this function.
        /// Common errors include: file not found and invalid xml. None of these error
        /// should cause help search to stop.
        /// 2. a helpfile cache is used to avoid same file got loaded again and again.
        /// </remarks>
        private void LoadHelpFile(string helpFile, string helpFileIdentifier)
        {
            XmlDocument doc = InternalDeserializer.LoadUnsafeXmlDocument(
                new FileInfo(helpFile),
                false, /* ignore whitespace, comments, etc. */
                null); /* default maxCharactersInDocument */

            // Add this file into _helpFiles hashtable to prevent it to be loaded again.
            _helpFiles[helpFile] = 0;

            XmlNode helpItemsNode = null;

            if (doc.HasChildNodes)
            {
                for (int i = 0; i < doc.ChildNodes.Count; i++)
                {
                    XmlNode node = doc.ChildNodes[i];
                    if (node.NodeType == XmlNodeType.Element && string.Equals(node.LocalName, "helpItems", StringComparison.OrdinalIgnoreCase))
                    {
                        helpItemsNode = node;
                        break;
                    }
                }
            }

            if (helpItemsNode == null)
            {
                s_tracer.WriteLine("Unable to find 'helpItems' element in file {0}", helpFile);
                return;
            }

            bool isMaml = IsMamlHelp(helpFile, helpItemsNode);

            using (this.HelpSystem.Trace(helpFile))
            {
                if (helpItemsNode.HasChildNodes)
                {
                    for (int i = 0; i < helpItemsNode.ChildNodes.Count; i++)
                    {
                        XmlNode node = helpItemsNode.ChildNodes[i];
                        if (node.NodeType == XmlNodeType.Element && string.Equals(node.LocalName, "command", StringComparison.OrdinalIgnoreCase))
                        {
                            MamlCommandHelpInfo helpInfo = null;

                            if (isMaml)
                            {
                                helpInfo = MamlCommandHelpInfo.Load(node, HelpCategory.Cmdlet);
                            }

                            if (helpInfo != null)
                            {
                                this.HelpSystem.TraceErrors(helpInfo.Errors);
                                AddToCommandCache(helpFileIdentifier, helpInfo.Name, helpInfo);
                            }
                        }

                        if (node.NodeType == XmlNodeType.Element && string.Equals(node.Name, "UserDefinedData", StringComparison.OrdinalIgnoreCase))
                        {
                            UserDefinedHelpData userDefinedHelpData = UserDefinedHelpData.Load(node);

                            ProcessUserDefinedHelpData(helpFileIdentifier, userDefinedHelpData);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process user defined help data by finding the corresponding helpInfo and inserting
        /// necessary helpdata info to command help.
        /// </summary>
        /// <param name="mshSnapInId">PSSnapIn Name for the current help file.</param>
        /// <param name="userDefinedHelpData"></param>
        private void ProcessUserDefinedHelpData(string mshSnapInId, UserDefinedHelpData userDefinedHelpData)
        {
            if (userDefinedHelpData == null)
                return;

            if (string.IsNullOrEmpty(userDefinedHelpData.Name))
                return;

            HelpInfo helpInfo = GetFromCommandCache(mshSnapInId, userDefinedHelpData.Name, HelpCategory.Cmdlet);

            if (helpInfo == null)
                return;

            if (!(helpInfo is MamlCommandHelpInfo commandHelpInfo))
                return;

            commandHelpInfo.AddUserDefinedData(userDefinedHelpData);

            return;
        }

        /// <summary>
        /// Gets the HelpInfo object corresponding to the command.
        /// </summary>
        /// <param name="helpFileIdentifier">Help file identifier (either name of PSSnapIn or simply full path to help file).</param>
        /// <param name="commandName">Name of the command.</param>
        /// <param name="helpCategory"></param>
        /// <returns>HelpInfo object.</returns>
        private HelpInfo GetFromCommandCache(string helpFileIdentifier, string commandName, HelpCategory helpCategory)
        {
            Debug.Assert(!string.IsNullOrEmpty(commandName), "Cmdlet Name should not be null or empty.");

            string key = commandName;
            if (!string.IsNullOrEmpty(helpFileIdentifier))
            {
                key = helpFileIdentifier + "\\" + key;
            }

            HelpInfo result = GetCache(key);

            // Win8: Win8:477680: When Function/Workflow Use External Help, Category Property is "Cmdlet"
            if ((result != null) && (result.HelpCategory != helpCategory))
            {
                MamlCommandHelpInfo original = (MamlCommandHelpInfo)result;
                result = original.Copy(helpCategory);
            }

            return result;
        }

        /// <summary>
        /// Gets the HelpInfo object corresponding to the CommandInfo.
        /// </summary>
        /// <param name="helpFileIdentifier">Help file identifier (simply full path to help file).</param>
        /// <param name="commandInfo"></param>
        /// <returns>HelpInfo object.</returns>
        private HelpInfo GetFromCommandCache(string helpFileIdentifier, CommandInfo commandInfo)
        {
            Debug.Assert(commandInfo != null, "commandInfo cannot be null");
            HelpInfo result = GetFromCommandCache(helpFileIdentifier, commandInfo.Name, commandInfo.HelpCategory);
            if (result == null)
            {
                // check if the command is prefixed and try retrieving help by removing the prefix
                if ((commandInfo.Module != null) && (!string.IsNullOrEmpty(commandInfo.Prefix)))
                {
                    MamlCommandHelpInfo newMamlHelpInfo = GetFromCommandCacheByRemovingPrefix(helpFileIdentifier, commandInfo);
                    if (newMamlHelpInfo != null)
                    {
                        // caching the changed help content under the prefixed name for faster
                        // retrieval later.
                        AddToCommandCache(helpFileIdentifier, commandInfo.Name, newMamlHelpInfo);
                        return newMamlHelpInfo;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Tries to get the help for the Cmdlet from cache.
        /// </summary>
        /// <param name="cmdletInfo"></param>
        /// <returns>
        /// HelpInfo object representing help for the command.
        /// </returns>
        private HelpInfo GetFromCommandCacheOrCmdletInfo(CmdletInfo cmdletInfo)
        {
            Debug.Assert(cmdletInfo != null, "cmdletInfo cannot be null");
            HelpInfo result = GetFromCommandCache(cmdletInfo.ModuleName, cmdletInfo.Name, cmdletInfo.HelpCategory);
            if (result == null)
            {
                // check if the command is prefixed and try retrieving help by removing the prefix
                if ((cmdletInfo.Module != null) && (!string.IsNullOrEmpty(cmdletInfo.Prefix)))
                {
                    MamlCommandHelpInfo newMamlHelpInfo = GetFromCommandCacheByRemovingPrefix(cmdletInfo.ModuleName, cmdletInfo);

                    if (newMamlHelpInfo != null)
                    {
                        // Noun exists only for cmdlets...since prefix will change the Noun, updating
                        // the help content accordingly
                        if (newMamlHelpInfo.FullHelp.Properties["Details"] != null &&
                            newMamlHelpInfo.FullHelp.Properties["Details"].Value != null)
                        {
                            PSObject commandDetails = PSObject.AsPSObject(newMamlHelpInfo.FullHelp.Properties["Details"].Value);
                            if (commandDetails.Properties["Noun"] != null)
                            {
                                commandDetails.Properties.Remove("Noun");
                            }

                            commandDetails.Properties.Add(new PSNoteProperty("Noun", cmdletInfo.Noun));
                        }

                        // caching the changed help content under the prefixed name for faster
                        // retrieval later.
                        AddToCommandCache(cmdletInfo.ModuleName, cmdletInfo.Name, newMamlHelpInfo);
                        return newMamlHelpInfo;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Used to retrieve helpinfo by removing the prefix from the noun portion of a command name.
        /// Import-Module and Import-PSSession supports changing the name of a command
        /// by suppling a custom prefix. In those cases, the help content is stored by using the
        /// original command name (without prefix) as the key.
        ///
        /// This method retrieves the help content by suppressing the prefix and then making a copy
        /// of the help content + change the name and then returns the copied help content.
        /// </summary>
        /// <param name="helpIdentifier"></param>
        /// <param name="cmdInfo"></param>
        /// <returns>
        /// Copied help content or null if no help content is found.
        /// </returns>
        private MamlCommandHelpInfo GetFromCommandCacheByRemovingPrefix(string helpIdentifier, CommandInfo cmdInfo)
        {
            Dbg.Assert(cmdInfo != null, "cmdInfo cannot be null");

            MamlCommandHelpInfo result = null;
            MamlCommandHelpInfo originalHelpInfo = GetFromCommandCache(helpIdentifier,
                        Microsoft.PowerShell.Commands.ModuleCmdletBase.RemovePrefixFromCommandName(cmdInfo.Name, cmdInfo.Prefix),
                        cmdInfo.HelpCategory) as MamlCommandHelpInfo;

            if (originalHelpInfo != null)
            {
                result = originalHelpInfo.Copy();
                // command's name can be changed using -Prefix while importing module.To give better user experience for
                // get-help (on par with get-command), it was decided to use the prefixed command name
                // for the help content.
                if (result.FullHelp.Properties["Name"] != null)
                {
                    result.FullHelp.Properties.Remove("Name");
                }

                result.FullHelp.Properties.Add(new PSNoteProperty("Name", cmdInfo.Name));

                if (result.FullHelp.Properties["Details"] != null &&
                    result.FullHelp.Properties["Details"].Value != null)
                {
                    // Note we are making a copy of the original instance and updating
                    // it..This is to protect the help content of the original object.
                    PSObject commandDetails = PSObject.AsPSObject(
                        result.FullHelp.Properties["Details"].Value).Copy();

                    if (commandDetails.Properties["Name"] != null)
                    {
                        commandDetails.Properties.Remove("Name");
                    }

                    commandDetails.Properties.Add(new PSNoteProperty("Name", cmdInfo.Name));

                    // Note we made the change to a copy..so assigning the copy back to
                    // the help content that is returned to the user.
                    result.FullHelp.Properties["Details"].Value = commandDetails;
                }
            }

            return result;
        }

        /// <summary>
        /// Prepends mshsnapin id to the cmdlet name and adds the result to help cache.
        /// </summary>
        /// <param name="mshSnapInId">PSSnapIn name that this cmdlet belongs to.</param>
        /// <param name="cmdletName">Name of the cmdlet.</param>
        /// <param name="helpInfo">Help object for the cmdlet.</param>
        private void AddToCommandCache(string mshSnapInId, string cmdletName, MamlCommandHelpInfo helpInfo)
        {
            Debug.Assert(!string.IsNullOrEmpty(cmdletName), "Cmdlet Name should not be null or empty.");

            string key = cmdletName;

            // Add snapin qualified type name for this command at the top..
            // this will enable customizations of the help object.
            helpInfo.FullHelp.TypeNames.Insert(0, string.Format(CultureInfo.InvariantCulture,
                "MamlCommandHelpInfo#{0}#{1}", mshSnapInId, cmdletName));

            if (!string.IsNullOrEmpty(mshSnapInId))
            {
                key = mshSnapInId + "\\" + key;
                // Add snapin name to the typenames of this object
                helpInfo.FullHelp.TypeNames.Insert(1, string.Format(CultureInfo.InvariantCulture,
                    "MamlCommandHelpInfo#{0}", mshSnapInId));
            }

            AddCache(key, helpInfo);
        }

        /// <summary>
        /// Check whether a HelpItems node indicates that the help content is
        /// authored using maml schema.
        ///
        /// This covers two cases:
        ///     a. If the help file has an extension .maml.
        ///     b. If HelpItems node (which should be the top node of any command help file)
        ///        has an attribute "schema" with value "maml", its content is in maml
        ///        schema.
        /// </summary>
        /// <param name="helpFile"></param>
        /// <param name="helpItemsNode"></param>
        /// <returns></returns>
        internal static bool IsMamlHelp(string helpFile, XmlNode helpItemsNode)
        {
            if (helpFile.EndsWith(".maml", StringComparison.OrdinalIgnoreCase))
                return true;

            if (helpItemsNode.Attributes == null)
                return false;

            foreach (XmlNode attribute in helpItemsNode.Attributes)
            {
                if (attribute.Name.Equals("schema", StringComparison.OrdinalIgnoreCase)
                    && attribute.Value.Equals("maml", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Search help for a specific target.
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        /// <param name="searchOnlyContent">
        /// If true, searches for pattern in the help content of all cmdlets.
        /// Otherwise, searches for pattern in the cmdlet names.
        /// </param>
        /// <returns></returns>
        internal override IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent)
        {
            string target = helpRequest.Target;
            Collection<string> patternList = new Collection<string>();
            // this will be used only when searchOnlyContent == true
            WildcardPattern wildCardPattern = null;
            // Decorated Search means that original match target is a target without
            // wildcard patterns. It come here to because exact match was not found
            // and search target will be decorated with wildcard character '*' to
            // search again.
            bool decoratedSearch = !WildcardPattern.ContainsWildcardCharacters(helpRequest.Target);

            if (!searchOnlyContent)
            {
                if (decoratedSearch)
                {
                    if (target.Contains(StringLiterals.CommandVerbNounSeparator))
                    {
                        patternList.Add(target + "*");
                    }
                    else
                    {
                        patternList.Add("*" + target + "*");
                    }
                }
                else
                {
                    patternList.Add(target);
                }
            }
            else
            {
                // get help for all cmdlets.
                patternList.Add("*");
                string searchTarget = helpRequest.Target;
                if (decoratedSearch)
                {
                    searchTarget = "*" + helpRequest.Target + "*";
                }

                wildCardPattern = WildcardPattern.Get(searchTarget, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);
            }

            int countOfHelpInfoObjectsFound = 0;
            // this is for avoiding duplicate result from help output.
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hiddenCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string pattern in patternList)
            {
                CommandSearcher searcher = GetCommandSearcherForSearch(pattern, _context);

                while (searcher.MoveNext())
                {
                    if (_context.CurrentPipelineStopping)
                    {
                        yield break;
                    }

                    CommandInfo current = ((IEnumerator<CommandInfo>)searcher).Current;

                    HelpInfo helpInfo = GetHelpInfo(current, !decoratedSearch, searchOnlyContent);
                    string helpName = GetHelpName(current);

                    if (helpInfo != null && !string.IsNullOrEmpty(helpName))
                    {
                        if (!SessionState.IsVisible(helpRequest.CommandOrigin, current))
                        {
                            // this command is not visible to the user (from CommandOrigin) so
                            // dont show help topic for it.
                            if (!hiddenCommands.Contains(helpName))
                            {
                                hiddenCommands.Add(helpName);
                            }

                            continue;
                        }

                        if (set.Contains(helpName))
                            continue;

                        // filter out the helpInfo object depending on user request
                        if (!Match(helpInfo, helpRequest, current))
                        {
                            continue;
                        }

                        // Search content
                        if (searchOnlyContent && (!helpInfo.MatchPatternInContent(wildCardPattern)))
                        {
                            continue;
                        }

                        set.Add(helpName);
                        countOfHelpInfoObjectsFound++;
                        yield return helpInfo;

                        if (countOfHelpInfoObjectsFound >= helpRequest.MaxResults && helpRequest.MaxResults > 0)
                            yield break;
                    }
                }

                if (this.HelpCategory == (HelpCategory.Alias | HelpCategory.Cmdlet))
                {
                    foreach (CommandInfo current in ModuleUtils.GetMatchingCommands(pattern, _context, helpRequest.CommandOrigin))
                    {
                        if (_context.CurrentPipelineStopping)
                        {
                            yield break;
                        }

                        if (!SessionState.IsVisible(helpRequest.CommandOrigin, current))
                        {
                            // this command is not visible to the user (from CommandOrigin) so
                            // dont show help topic for it.
                            continue;
                        }

                        HelpInfo helpInfo = GetHelpInfo(current, !decoratedSearch, searchOnlyContent);
                        string helpName = GetHelpName(current);

                        if (helpInfo != null && !string.IsNullOrEmpty(helpName))
                        {
                            if (set.Contains(helpName))
                                continue;

                            if (hiddenCommands.Contains(helpName))
                                continue;

                            // filter out the helpInfo object depending on user request
                            if (!Match(helpInfo, helpRequest, current))
                            {
                                continue;
                            }

                            // Search content
                            if (searchOnlyContent && (!helpInfo.MatchPatternInContent(wildCardPattern)))
                            {
                                continue;
                            }

                            set.Add(helpName);
                            countOfHelpInfoObjectsFound++;
                            yield return helpInfo;

                            if (countOfHelpInfoObjectsFound >= helpRequest.MaxResults && helpRequest.MaxResults > 0)
                                yield break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if a helpInfo object matches the component/role/functionality
        /// criteria from helpRequest.
        /// </summary>
        /// <param name="helpInfo"></param>
        /// <param name="helpRequest"></param>
        /// <param name="commandInfo"></param>
        /// <returns></returns>
        private static bool Match(HelpInfo helpInfo, HelpRequest helpRequest, CommandInfo commandInfo)
        {
            if (helpRequest == null)
                return true;

            if ((helpRequest.HelpCategory & commandInfo.HelpCategory) == 0)
            {
                return false;
            }

            if (helpInfo is not BaseCommandHelpInfo)
                return false;

            if (!Match(helpInfo.Component, helpRequest.Component))
            {
                return false;
            }

            if (!Match(helpInfo.Role, helpRequest.Role))
            {
                return false;
            }

            if (!Match(helpInfo.Functionality, helpRequest.Functionality))
            {
                return false;
            }

            return true;
        }

        private static bool Match(string target, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return true;

            if (string.IsNullOrEmpty(target))
                target = string.Empty;

            WildcardPattern matcher = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);

            return matcher.IsMatch(target);
        }

        /// <summary>
        /// Checks whether <paramref name="target"/> matches any of the patterns
        /// present in <paramref name="patterns"/>
        /// </summary>
        /// <param name="target">Content to search in.</param>
        /// <param name="patterns">String patterns to look for.</param>
        /// <returns>
        /// true if <paramref name="target"/> contains any of the patterns
        /// present in <paramref name="patterns"/>
        /// false otherwise.
        /// </returns>
        private static bool Match(string target, ICollection<string> patterns)
        {
            // patterns should never be null as shell never accepts
            // empty inputs. Keeping this check as a safe measure.
            if (patterns == null || patterns.Count == 0)
                return true;

            foreach (string pattern in patterns)
            {
                if (Match(target, pattern))
                {
                    // we have a match so return true
                    return true;
                }
            }

            // We dont have a match so far..so return false
            return false;
        }

        /// <summary>
        /// Process helpInfo forwarded over from other providers, specificly AliasHelpProvider.
        /// This can return more than 1 helpinfo object.
        /// </summary>
        /// <param name="helpInfo">HelpInfo that is forwarded over.</param>
        /// <param name="helpRequest">Help request object.</param>
        /// <returns>The result helpInfo objects after processing.</returns>
        internal override IEnumerable<HelpInfo> ProcessForwardedHelp(HelpInfo helpInfo, HelpRequest helpRequest)
        {
            const HelpCategory categoriesHandled = (HelpCategory.Alias
                | HelpCategory.ExternalScript | HelpCategory.Filter | HelpCategory.Function | HelpCategory.ScriptCommand);

            if ((helpInfo.HelpCategory & categoriesHandled) != 0)
            {
                HelpRequest commandHelpRequest = helpRequest.Clone();
                commandHelpRequest.Target = helpInfo.ForwardTarget;
                commandHelpRequest.CommandOrigin = CommandOrigin.Internal; // a public command can forward help to a private command

                // We may know what category to forward to.  If so, just set it.  If not, look up the
                // command and determine it's category.  For aliases, we definitely don't know the category
                // to forward to, so always do the lookup.
                if (helpInfo.ForwardHelpCategory != HelpCategory.None && helpInfo.HelpCategory != HelpCategory.Alias)
                {
                    commandHelpRequest.HelpCategory = helpInfo.ForwardHelpCategory;
                }
                else
                {
                    try
                    {
                        CommandInfo targetCommand = _context.CommandDiscovery.LookupCommandInfo(commandHelpRequest.Target);
                        commandHelpRequest.HelpCategory = targetCommand.HelpCategory;
                    }
                    catch (CommandNotFoundException)
                    {
                        // ignore errors for aliases pointing to non-existant commands
                    }
                }

                foreach (HelpInfo helpInfoToReturn in ExactMatchHelp(commandHelpRequest))
                {
                    yield return helpInfoToReturn;
                }
            }
            else
            {
                // command help provider can forward process only an AliasHelpInfo..
                // so returning the original help info here.
                yield return helpInfo;
            }
        }

        /// <summary>
        /// This will reset the help cache. Normally this corresponds to a
        /// help culture change.
        /// </summary>
        internal override void Reset()
        {
            base.Reset();

            _helpFiles.Clear();
        }

        #endregion

        #region Extensions

        /// <summary>
        /// Gets a command searcher used for ExactMatch help lookup.
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal virtual CommandSearcher GetCommandSearcherForExactMatch(string commandName, ExecutionContext context)
        {
            CommandSearcher searcher = new CommandSearcher(
                commandName,
                SearchResolutionOptions.None,
                CommandTypes.Cmdlet,
                context);

            return searcher;
        }

        /// <summary>
        /// Gets a command searcher used for searching help.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal virtual CommandSearcher GetCommandSearcherForSearch(string pattern, ExecutionContext context)
        {
            CommandSearcher searcher =
                    new CommandSearcher(
                        pattern,
                        SearchResolutionOptions.CommandNameIsPattern,
                        CommandTypes.Cmdlet,
                        context);

            return searcher;
        }

        #endregion

        #region tracer

        [TraceSource("CommandHelpProvider", "CommandHelpProvider")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("CommandHelpProvider", "CommandHelpProvider");

        #endregion tracer
    }

    /// <summary>
    /// This is the class to track the user-defined Help Data which is separate from the
    /// commandHelp itself.
    ///
    /// Legally, user-defined Help Data should be within the same file as the corresponding
    /// commandHelp and it should appear after the commandHelp.
    /// </summary>
    internal sealed class UserDefinedHelpData
    {
        private UserDefinedHelpData()
        {
        }

        internal Dictionary<string, string> Properties { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string _name = null;

        internal string Name
        {
            get
            {
                return _name;
            }
        }

        internal static UserDefinedHelpData Load(XmlNode dataNode)
        {
            if (dataNode == null)
                return null;

            UserDefinedHelpData userDefinedHelpData = new UserDefinedHelpData();

            for (int i = 0; i < dataNode.ChildNodes.Count; i++)
            {
                XmlNode node = dataNode.ChildNodes[i];
                if (node.NodeType == XmlNodeType.Element)
                {
                    userDefinedHelpData.Properties[node.Name] = node.InnerText.Trim();
                }
            }

            string name;
            if (!userDefinedHelpData.Properties.TryGetValue("name", out name))
            {
                return null;
            }

            userDefinedHelpData._name = name;

            if (string.IsNullOrEmpty(userDefinedHelpData.Name))
                return null;

            return userDefinedHelpData;
        }
    }
}
