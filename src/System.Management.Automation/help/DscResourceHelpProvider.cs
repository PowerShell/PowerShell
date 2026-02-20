// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Xml;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    internal class DscResourceHelpProvider : HelpProviderWithCache
    {
        /// <summary>
        /// Constructor for DscResourceHelpProvider.
        /// </summary>
        internal DscResourceHelpProvider(HelpSystem helpSystem)
            : base(helpSystem)
        {
            _context = helpSystem.ExecutionContext;
        }

        /// <summary>
        /// Execution context of the HelpSystem.
        /// </summary>
        private readonly ExecutionContext _context;

        /// <summary>
        /// This is a hashtable to track which help files are loaded already.
        ///
        /// This will avoid one help file getting loaded again and again.
        /// </summary>
        private readonly HashSet<string> _helpFiles = new();

        [TraceSource("DscResourceHelpProvider", "DscResourceHelpProvider")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("DscResourceHelpProvider", "DscResourceHelpProvider");

        #region common properties

        /// <summary>
        /// Name of the Help Provider.
        /// </summary>
        internal override string Name
        {
            get { return "Dsc Resource Help Provider"; }
        }

        /// <summary>
        /// Supported Help Categories.
        /// </summary>
        internal override HelpCategory HelpCategory
        {
            get { return Automation.HelpCategory.DscResource; }
        }

        #endregion

        /// <summary>
        /// Override SearchHelp to find a dsc resource help matching a pattern.
        /// </summary>
        /// <param name="helpRequest">Help request.</param>
        /// <param name="searchOnlyContent">Not used.</param>
        /// <returns></returns>
        internal override IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent)
        {
            Debug.Assert(helpRequest != null, "helpRequest cannot be null.");

            string target = helpRequest.Target;
            Collection<string> patternList = new Collection<string>();

            bool decoratedSearch = !WildcardPattern.ContainsWildcardCharacters(helpRequest.Target);

            if (decoratedSearch)
            {
                patternList.Add("*" + target + "*");
            }
            else
                patternList.Add(target);

            foreach (string pattern in patternList)
            {
                DscResourceSearcher searcher = new DscResourceSearcher(pattern, _context);

                foreach (var helpInfo in GetHelpInfo(searcher))
                {
                    if (helpInfo != null)
                        yield return helpInfo;
                }
            }
        }

        /// <summary>
        /// Override ExactMatchHelp to find the matching DscResource matching help request.
        /// </summary>
        /// <param name="helpRequest">Help Request for the search.</param>
        /// <returns>Enumerable of HelpInfo objects.</returns>
        internal override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            Debug.Assert(helpRequest != null, "helpRequest cannot be null.");

            if ((helpRequest.HelpCategory & Automation.HelpCategory.DscResource) == 0)
            {
                yield return null;
            }

            string target = helpRequest.Target;

            DscResourceSearcher searcher = new DscResourceSearcher(target, _context);

            foreach (var helpInfo in GetHelpInfo(searcher))
            {
                if (helpInfo != null)
                {
                    yield return helpInfo;
                }
            }
        }

        /// <summary>
        /// Get the help in for the DscResource Info.        ///
        /// </summary>
        /// <param name="searcher">Searcher for DscResources.</param>
        /// <returns>Next HelpInfo object.</returns>
        private IEnumerable<HelpInfo> GetHelpInfo(DscResourceSearcher searcher)
        {
            while (searcher.MoveNext())
            {
                DscResourceInfo current = ((IEnumerator<DscResourceInfo>)searcher).Current;

                string moduleName = null;
                string moduleDir = current.ParentPath;

                // for binary modules, current.Module is empty.
                // in such cases use the leaf folder of ParentPath as filename.
                if (current.Module != null)
                {
                    moduleName = current.Module.Name;
                }
                else if (!string.IsNullOrEmpty(moduleDir))
                {
                    string[] splitPath = moduleDir.Split('\\');
                    moduleName = splitPath[splitPath.Length - 1];
                }

                if (!string.IsNullOrEmpty(moduleName) && !string.IsNullOrEmpty(moduleDir))
                {
                    string helpFileToFind = moduleName + "-Help.xml";

                    string helpFileName = null;

                    Collection<string> searchPaths = new Collection<string>();
                    searchPaths.Add(moduleDir);

                    HelpInfo helpInfo = GetHelpInfoFromHelpFile(current, helpFileToFind, searchPaths, true, out helpFileName);

                    if (helpInfo != null)
                    {
                        yield return helpInfo;
                    }
                }
            }
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
        /// <param name="helpFile">File name.</param>
        /// <param name="helpItemsNode">Nodes to check.</param>
        /// <returns></returns>
        internal static bool IsMamlHelp(string helpFile, XmlNode helpItemsNode)
        {
            Debug.Assert(!string.IsNullOrEmpty(helpFile), "helpFile cannot be null.");

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

        #region private methods

        private HelpInfo GetHelpInfoFromHelpFile(DscResourceInfo resourceInfo, string helpFileToFind, Collection<string> searchPaths, bool reportErrors, out string helpFile)
        {
            Dbg.Assert(resourceInfo != null, "Caller should verify that resourceInfo != null");
            Dbg.Assert(helpFileToFind != null, "Caller should verify that helpFileToFind != null");

            helpFile = MUIFileSearcher.LocateFile(helpFileToFind, searchPaths);

            if (!File.Exists(helpFile))
                return null;

            if (!string.IsNullOrEmpty(helpFile))
            {
                // Load the help file only once. Then use it from the cache.
                if (!_helpFiles.Contains(helpFile))
                {
                    LoadHelpFile(helpFile, helpFile, resourceInfo.Name, reportErrors);
                }

                return GetFromResourceHelpCache(helpFile, Automation.HelpCategory.DscResource);
            }

            return null;
        }

        /// <summary>
        /// Gets the HelpInfo object corresponding to the command.
        /// </summary>
        /// <param name="helpFileIdentifier">Help file identifier (either name of PSSnapIn or simply full path to help file).</param>
        /// <param name="helpCategory">Help Category for search.</param>
        /// <returns>HelpInfo object.</returns>
        private HelpInfo GetFromResourceHelpCache(string helpFileIdentifier, HelpCategory helpCategory)
        {
            Debug.Assert(!string.IsNullOrEmpty(helpFileIdentifier), "helpFileIdentifier should not be null or empty.");

            HelpInfo result = GetCache(helpFileIdentifier);

            if (result != null)
            {
                MamlCommandHelpInfo original = (MamlCommandHelpInfo)result;
                result = original.Copy(helpCategory);
            }

            return result;
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

            if (e != null)
                s_tracer.WriteLine("Error occurred in DscResourceHelpProvider {0}", e.Message);

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
            Dbg.Assert(!string.IsNullOrEmpty(helpFile), "HelpFile cannot be null or empty.");
            Dbg.Assert(!string.IsNullOrEmpty(helpFileIdentifier), "helpFileIdentifier cannot be null or empty.");

            XmlDocument doc = InternalDeserializer.LoadUnsafeXmlDocument(
                new FileInfo(helpFile),
                false, /* ignore whitespace, comments, etc. */
                null); /* default maxCharactersInDocument */

            // Add this file into _helpFiles hashtable to prevent it to be loaded again.
            _helpFiles.Add(helpFile);

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

                        string nodeLocalName = node.LocalName;

                        bool isDscResource = (string.Equals(nodeLocalName, "dscResource", StringComparison.OrdinalIgnoreCase));

                        if (node.NodeType == XmlNodeType.Element && isDscResource)
                        {
                            MamlCommandHelpInfo helpInfo = null;

                            if (isMaml)
                            {
                                if (isDscResource)
                                    helpInfo = MamlCommandHelpInfo.Load(node, HelpCategory.DscResource);
                            }

                            if (helpInfo != null)
                            {
                                this.HelpSystem.TraceErrors(helpInfo.Errors);
                                AddCache(helpFileIdentifier, helpInfo);
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}
