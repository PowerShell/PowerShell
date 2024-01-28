// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

namespace System.Management.Automation
{
    /// <summary>
    /// Class ProviderHelpProvider implement the help provider for commands.
    /// </summary>
    /// <remarks>
    /// Provider Help information are stored in 'help.xml' files. Location of these files
    /// can be found from CommandDiscovery.
    /// </remarks>
    internal sealed class ProviderHelpProvider : HelpProviderWithCache
    {
        /// <summary>
        /// Constructor for HelpProvider.
        /// </summary>
        internal ProviderHelpProvider(HelpSystem helpSystem) : base(helpSystem)
        {
            _sessionState = helpSystem.ExecutionContext.SessionState;
        }

        private readonly SessionState _sessionState;

        #region Common Properties

        /// <summary>
        /// Name of this help provider.
        /// </summary>
        /// <value>Name of this help provider.</value>
        internal override string Name
        {
            get
            {
                return "Provider Help Provider";
            }
        }

        /// <summary>
        /// Help category of this provider.
        /// </summary>
        /// <value>Help category of this provider</value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.Provider;
            }
        }

        #endregion

        #region Help Provider Interface

        /// <summary>
        /// Do exact match help based on the target.
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        internal override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            Collection<ProviderInfo> matchingProviders = null;

            try
            {
                matchingProviders = _sessionState.Provider.Get(helpRequest.Target);
            }
            catch (ProviderNotFoundException e)
            {
                // We distinguish two cases here,
                //      a. If the "Provider" is the only category to search for in this case,
                //         an error will be written.
                //      b. Otherwise, no errors will be written since in end user's mind,
                //         he may mean to search for provider help.
                if (this.HelpSystem.LastHelpCategory == HelpCategory.Provider)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e, "ProviderLoadError", ErrorCategory.ResourceUnavailable, null);
                    errorRecord.ErrorDetails = new ErrorDetails(typeof(ProviderHelpProvider).Assembly, "HelpErrors", "ProviderLoadError", helpRequest.Target, e.Message);
                    this.HelpSystem.LastErrors.Add(errorRecord);
                }
            }

            if (matchingProviders != null)
            {
                foreach (ProviderInfo providerInfo in matchingProviders)
                {
                    try
                    {
                        LoadHelpFile(providerInfo);
                    }
                    catch (IOException ioException)
                    {
                        ReportHelpFileError(ioException, helpRequest.Target, providerInfo.HelpFile);
                    }
                    catch (System.Security.SecurityException securityException)
                    {
                        ReportHelpFileError(securityException, helpRequest.Target, providerInfo.HelpFile);
                    }
                    catch (XmlException xmlException)
                    {
                        ReportHelpFileError(xmlException, helpRequest.Target, providerInfo.HelpFile);
                    }

                    HelpInfo helpInfo = GetCache(providerInfo.PSSnapInName + "\\" + providerInfo.Name);

                    if (helpInfo != null)
                    {
                        yield return helpInfo;
                    }
                }
            }
        }

        private static string GetProviderAssemblyPath(ProviderInfo providerInfo)
        {
            if (providerInfo == null)
                return null;

            if (providerInfo.ImplementingType == null)
                return null;

            return Path.GetDirectoryName(providerInfo.ImplementingType.Assembly.Location);
        }

        /// <summary>
        /// This is a hashtable to track which help files are loaded already.
        ///
        /// This will avoid one help file getting loaded again and again.
        /// (Which should not happen unless some provider is pointing
        /// to a help file that actually doesn't contain the help for it).
        /// </summary>
        private readonly Hashtable _helpFiles = new Hashtable();

        /// <summary>
        /// Load help file provided.
        /// </summary>
        /// <remarks>
        /// This will load providerHelpInfo from help file into help cache.
        /// </remarks>
        /// <param name="providerInfo">ProviderInfo for which to locate help.</param>
        private void LoadHelpFile(ProviderInfo providerInfo)
        {
            if (providerInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(providerInfo));
            }

            string helpFile = providerInfo.HelpFile;

            if (string.IsNullOrEmpty(helpFile) || _helpFiles.Contains(helpFile))
            {
                return;
            }

            string helpFileToLoad = helpFile;

            // Get the mshsnapinfo object for this cmdlet.
            PSSnapInInfo mshSnapInInfo = providerInfo.PSSnapIn;

            // Search fallback
            // 1. If PSSnapInInfo exists, then always look in the application base
            //    of the mshsnapin
            // Otherwise,
            //    Look in the default search path and cmdlet assembly path
            Collection<string> searchPaths = new Collection<string>();
            if (mshSnapInInfo != null)
            {
                Diagnostics.Assert(!string.IsNullOrEmpty(mshSnapInInfo.ApplicationBase),
                    "Application Base is null or empty.");
                // not minishell case..
                // we have to search only in the application base for a mshsnapin...
                // if you create an absolute path for helpfile, then MUIFileSearcher
                // will look only in that path.
                helpFileToLoad = Path.Combine(mshSnapInInfo.ApplicationBase, helpFile);
            }
            else if ((providerInfo.Module != null) && (!string.IsNullOrEmpty(providerInfo.Module.Path)))
            {
                helpFileToLoad = Path.Combine(providerInfo.Module.ModuleBase, helpFile);
            }
            else
            {
                searchPaths.Add(GetDefaultShellSearchPath());
                searchPaths.Add(GetProviderAssemblyPath(providerInfo));
            }

            string location = MUIFileSearcher.LocateFile(helpFileToLoad, searchPaths);
            if (string.IsNullOrEmpty(location))
                throw new FileNotFoundException(helpFile);

            XmlDocument doc = InternalDeserializer.LoadUnsafeXmlDocument(
                new FileInfo(location),
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
                    if (node.NodeType == XmlNodeType.Element && string.Equals(node.Name, "helpItems", StringComparison.OrdinalIgnoreCase))
                    {
                        helpItemsNode = node;
                        break;
                    }
                }
            }

            if (helpItemsNode == null)
                return;

            using (this.HelpSystem.Trace(location))
            {
                if (helpItemsNode.HasChildNodes)
                {
                    for (int i = 0; i < helpItemsNode.ChildNodes.Count; i++)
                    {
                        XmlNode node = helpItemsNode.ChildNodes[i];
                        if (node.NodeType == XmlNodeType.Element && string.Equals(node.Name, "providerHelp", StringComparison.OrdinalIgnoreCase))
                        {
                            HelpInfo helpInfo = ProviderHelpInfo.Load(node);

                            if (helpInfo != null)
                            {
                                this.HelpSystem.TraceErrors(helpInfo.Errors);
                                // Add snapin qualified type name for this command..
                                // this will enable customizations of the help object.
                                helpInfo.FullHelp.TypeNames.Insert(
                                    index: 0,
                                    string.Create(
                                        CultureInfo.InvariantCulture,
                                        $"ProviderHelpInfo#{providerInfo.PSSnapInName}#{helpInfo.Name}"));

                                if (!string.IsNullOrEmpty(providerInfo.PSSnapInName))
                                {
                                    helpInfo.FullHelp.Properties.Add(new PSNoteProperty("PSSnapIn", providerInfo.PSSnapIn));
                                    helpInfo.FullHelp.TypeNames.Insert(
                                        index: 1,
                                        string.Create(
                                            CultureInfo.InvariantCulture,
                                            $"ProviderHelpInfo#{providerInfo.PSSnapInName}"));
                                }

                                AddCache(providerInfo.PSSnapInName + "\\" + helpInfo.Name, helpInfo);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Search for provider help based on a search target.
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        /// <param name="searchOnlyContent">
        /// If true, searches for pattern in the help content. Individual
        /// provider can decide which content to search in.
        ///
        /// If false, searches for pattern in the command names.
        /// </param>
        /// <returns></returns>
        internal override IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent)
        {
            int countOfHelpInfoObjectsFound = 0;
            string target = helpRequest.Target;
            string pattern = target;
            // this will be used only when searchOnlyContent == true
            WildcardPattern wildCardPattern = null;

            bool decoratedSearch = !WildcardPattern.ContainsWildcardCharacters(target);

            if (!searchOnlyContent)
            {
                if (decoratedSearch)
                {
                    pattern += "*";
                }
            }
            else
            {
                string searchTarget = helpRequest.Target;
                if (decoratedSearch)
                {
                    searchTarget = "*" + helpRequest.Target + "*";
                }

                wildCardPattern = WildcardPattern.Get(searchTarget, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);
                // search in all providers
                pattern = "*";
            }

            PSSnapinQualifiedName snapinQualifiedNameForPattern =
                PSSnapinQualifiedName.GetInstance(pattern);

            if (snapinQualifiedNameForPattern == null)
            {
                yield break;
            }

            foreach (ProviderInfo providerInfo in _sessionState.Provider.GetAll())
            {
                if (providerInfo.IsMatch(pattern))
                {
                    try
                    {
                        LoadHelpFile(providerInfo);
                    }
                    catch (IOException ioException)
                    {
                        if (!decoratedSearch)
                        {
                            ReportHelpFileError(ioException, providerInfo.Name, providerInfo.HelpFile);
                        }
                    }
                    catch (System.Security.SecurityException securityException)
                    {
                        if (!decoratedSearch)
                        {
                            ReportHelpFileError(securityException, providerInfo.Name, providerInfo.HelpFile);
                        }
                    }
                    catch (XmlException xmlException)
                    {
                        if (!decoratedSearch)
                        {
                            ReportHelpFileError(xmlException, providerInfo.Name, providerInfo.HelpFile);
                        }
                    }

                    HelpInfo helpInfo = GetCache(providerInfo.PSSnapInName + "\\" + providerInfo.Name);

                    if (helpInfo != null)
                    {
                        if (searchOnlyContent)
                        {
                            // ignore help objects that do not have pattern in its help
                            // content.
                            if (!helpInfo.MatchPatternInContent(wildCardPattern))
                            {
                                continue;
                            }
                        }

                        countOfHelpInfoObjectsFound++;
                        yield return helpInfo;

                        if (countOfHelpInfoObjectsFound >= helpRequest.MaxResults && helpRequest.MaxResults > 0)
                            yield break;
                    }
                }
            }
        }

        internal override IEnumerable<HelpInfo> ProcessForwardedHelp(HelpInfo helpInfo, HelpRequest helpRequest)
        {
            ProviderCommandHelpInfo providerCommandHelpInfo = new ProviderCommandHelpInfo(
                helpInfo, helpRequest.ProviderContext);
            yield return providerCommandHelpInfo;
        }
#if V2
        /// <summary>
        /// Process a helpInfo forwarded from other providers (normally commandHelpProvider)
        /// </summary>
        /// <remarks>
        /// For command help info, this will
        ///     1. check whether provider-specific commandlet help exists.
        ///     2. merge found provider-specific help with commandlet help provided.
        /// </remarks>
        /// <param name="helpInfo">HelpInfo forwarded in.</param>
        /// <param name="helpRequest">Help request object.</param>
        /// <returns>The help info object after processing.</returns>
        override internal HelpInfo ProcessForwardedHelp(HelpInfo helpInfo, HelpRequest helpRequest)
        {
            if (helpInfo == null)
                return null;

            if (helpInfo.HelpCategory != HelpCategory.Command)
            {
                return helpInfo;
            }

            string providerName = helpRequest.Provider;
            if (string.IsNullOrEmpty(providerName))
            {
                providerName = this._sessionState.Path.CurrentLocation.Provider.Name;
            }

            HelpRequest providerHelpRequest = helpRequest.Clone();
            providerHelpRequest.Target = providerName;

            ProviderHelpInfo providerHelpInfo = (ProviderHelpInfo)this.ExactMatchHelp(providerHelpRequest);

            if (providerHelpInfo == null)
                return null;

            CommandHelpInfo commandHelpInfo = (CommandHelpInfo)helpInfo;

            CommandHelpInfo result = commandHelpInfo.MergeProviderSpecificHelp(providerHelpInfo.GetCmdletHelp(commandHelpInfo.Name), providerHelpInfo.GetDynamicParameterHelp(helpRequest.DynamicParameters));

            // Reset ForwardHelpCategory for the helpinfo to be returned so that it will not be forwarded back again.
            result.ForwardHelpCategory = HelpCategory.None;

            return result;
        }
#endif

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
    }
}
