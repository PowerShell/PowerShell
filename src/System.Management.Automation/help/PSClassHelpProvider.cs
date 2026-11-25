// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Xml;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    internal class PSClassHelpProvider : HelpProviderWithCache
    {
        /// <summary>
        /// Constructor for PSClassHelpProvider.
        /// </summary>
        internal PSClassHelpProvider(HelpSystem helpSystem)
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
        private readonly Hashtable _helpFiles = new Hashtable();

        [TraceSource("PSClassHelpProvider", "PSClassHelpProvider")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("PSClassHelpProvider", "PSClassHelpProvider");

        #region common properties

        /// <summary>
        /// Name of the Help Provider.
        /// </summary>
        internal override string Name
        {
            get { return "Powershell Class Help Provider"; }
        }

        /// <summary>
        /// Supported Help Categories.
        /// </summary>
        internal override HelpCategory HelpCategory
        {
            get { return Automation.HelpCategory.Class; }
        }

        #endregion

        /// <summary>
        /// Override SearchHelp to find a class module with help matching a pattern.
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
                PSClassSearcher searcher = new PSClassSearcher(pattern, useWildCards: true, _context);

                foreach (var helpInfo in GetHelpInfo(searcher))
                {
                    if (helpInfo != null)
                        yield return helpInfo;
                }
            }
        }

        /// <summary>
        /// Override ExactMatchHelp to find the matching class module matching help request.
        /// </summary>
        /// <param name="helpRequest">Help Request for the search.</param>
        /// <returns>Enumerable of HelpInfo objects.</returns>
        internal override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            Debug.Assert(helpRequest != null, "helpRequest cannot be null.");

            if ((helpRequest.HelpCategory & Automation.HelpCategory.Class) == 0)
            {
                yield return null;
            }

            PSClassSearcher searcher = new PSClassSearcher(helpRequest.Target, useWildCards: false, _context);

            foreach (var helpInfo in GetHelpInfo(searcher))
            {
                if (helpInfo != null)
                {
                    yield return helpInfo;
                }
            }
        }

        /// <summary>
        /// Get the help in for the PS Class Info.
        /// </summary>
        /// <param name="searcher">Searcher for PS Classes.</param>
        /// <returns>Next HelpInfo object.</returns>
        private IEnumerable<HelpInfo> GetHelpInfo(PSClassSearcher searcher)
        {
            while (searcher.MoveNext())
            {
                PSClassInfo current = ((IEnumerator<PSClassInfo>)searcher).Current;

                string moduleName = current.Module.Name;
                string moduleDir = current.Module.ModuleBase;

                if (!string.IsNullOrEmpty(moduleName) && !string.IsNullOrEmpty(moduleDir))
                {
                    string helpFileToFind = moduleName + "-Help.xml";

                    string helpFileName = null;

                    Collection<string> searchPaths = new Collection<string>();
                    searchPaths.Add(moduleDir);

                    string externalHelpFile = current.HelpFile;

                    if (!string.IsNullOrEmpty(externalHelpFile))
                    {
                        FileInfo helpFileInfo = new FileInfo(externalHelpFile);
                        DirectoryInfo dirToSearch = helpFileInfo.Directory;

                        if (dirToSearch.Exists)
                        {
                            searchPaths.Add(dirToSearch.FullName);
                            helpFileToFind = helpFileInfo.Name; // If external help file is specified. Then use it.
                        }
                    }

                    HelpInfo helpInfo = GetHelpInfoFromHelpFile(current, helpFileToFind, searchPaths, true, out helpFileName);

                    if (helpInfo != null)
                    {
                        yield return helpInfo;
                    }
                }
            }
        }

        /// <summary>
        /// Prepends helpfileIdentifier to the class name and adds the result to help cache.
        /// </summary>
        /// <param name="helpIdentifier">The path of the help file.</param>
        /// <param name="className">Name of the class.</param>
        /// <param name="helpInfo">Help object for the class.</param>
        private void AddToClassCache(string helpIdentifier, string className, MamlClassHelpInfo helpInfo)
        {
            Debug.Assert(!string.IsNullOrEmpty(className), "Class Name should not be null or empty.");

            string key = className;

            if (!string.IsNullOrEmpty(helpIdentifier))
            {
                key = helpIdentifier + "\\" + key;
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

        private HelpInfo GetHelpInfoFromHelpFile(PSClassInfo classInfo, string helpFileToFind, Collection<string> searchPaths, bool reportErrors, out string helpFile)
        {
            Dbg.Assert(classInfo != null, "Caller should verify that classInfo != null");
            Dbg.Assert(helpFileToFind != null, "Caller should verify that helpFileToFind != null");

            helpFile = MUIFileSearcher.LocateFile(helpFileToFind, searchPaths);

            if (!File.Exists(helpFile))
                return null;

            if (!string.IsNullOrEmpty(helpFile))
            {
                // Load the help file only once. Then use it from the cache.
                if (!_helpFiles.Contains(helpFile))
                {
                    LoadHelpFile(helpFile, helpFile, classInfo.Name, reportErrors);
                }

                return GetFromPSClassHelpCache(helpFile, classInfo.Name, Automation.HelpCategory.Class);
            }

            return null;
        }

        /// <summary>
        /// Gets the HelpInfo object corresponding to the command.
        /// </summary>
        /// <param name="helpFileIdentifier">The full path to the help file.</param>
        /// <param name="className">The name of the class in the help file.</param>
        /// <param name="helpCategory">Help Category for search.</param>
        /// <returns>HelpInfo object.</returns>
        private HelpInfo GetFromPSClassHelpCache(string helpFileIdentifier, string className, HelpCategory helpCategory)
        {
            Debug.Assert(!string.IsNullOrEmpty(className), "Class Name should not be null or empty.");

            string key = className;
            if (!string.IsNullOrEmpty(helpFileIdentifier))
            {
                key = helpFileIdentifier + "\\" + key;
            }

            HelpInfo result = GetCache(key);

            if (result != null)
            {
                MamlClassHelpInfo original = (MamlClassHelpInfo)result;
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
                s_tracer.WriteLine("Error occurred in PSClassHelpProvider {0}", e.Message);

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

                        string nodeLocalName = node.LocalName;

                        bool isClass = (string.Equals(nodeLocalName, "class", StringComparison.OrdinalIgnoreCase));

                        if (node.NodeType == XmlNodeType.Element && isClass)
                        {
                            MamlClassHelpInfo helpInfo = null;

                            if (isMaml)
                            {
                                if (isClass)
                                    helpInfo = MamlClassHelpInfo.Load(node, HelpCategory.Class);
                            }

                            if (helpInfo != null)
                            {
                                this.HelpSystem.TraceErrors(helpInfo.Errors);
                                AddToClassCache(helpFileIdentifier, helpInfo.Name, helpInfo);
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}
