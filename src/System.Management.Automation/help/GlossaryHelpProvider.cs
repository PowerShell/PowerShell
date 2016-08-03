/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using System.Xml;
using System.Reflection;

namespace System.Management.Automation
{
    /// <summary>
    /// Class GlossaryHelpProvider implement the help provider for glossary's.
    /// </summary>
    /// 
    /// <remarks>
    /// Glossary Help information are stored in 'glossary.xml' files. These files are
    /// located in the Monad / CustomShell Path as well as in the Application Base
    /// of PSSnapIns
    /// </remarks>
    internal class GlossaryHelpProvider : HelpProviderWithFullCache
    {
        /// <summary>
        /// Constructor for GlossaryHelpProvider
        /// </summary>
        internal GlossaryHelpProvider(HelpSystem helpSystem) : base(helpSystem)
        {
            this.HasCustomMatch = true;
        }

        #region Common Properties

        /// <summary>
        /// Name of this provider
        /// </summary>
        /// <value>Name of this provider</value>
        internal override string Name
        {
            get
            {
                return "Glossary Help Provider";
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
                return HelpCategory.Glossary;
            }
        }

        #endregion

        #region Load cache

        /// <summary>
        /// This is for implementing CustomMatch algorithm to be used in 
        /// HelpProviderWithCache for matching a help target with keys in 
        /// help cache. 
        /// 
        /// For each glossary entry, it can contain multiple terms. The
        /// key stored in help cache is a concatenation of keys. For example,
        /// if there are two terms "foo" and "bar", the key to be used in 
        /// help cache will be "foo, bar". 
        ///  
        /// Because of this mangling, key "foo, bar" should match both 
        /// "foo" and "bar".
        /// 
        /// </summary>
        /// <param name="target">target to search</param>
        /// <param name="key">key used in cache table</param>
        /// <returns></returns>
        protected sealed override bool CustomMatch(string target, string key)
        {
            if (String.IsNullOrEmpty(target) || String.IsNullOrEmpty(key))
                return false;

            string[] terms = key.Split(Utils.Separators.Comma);

            for (int i = 0; i < terms.Length; i++)
            {
                string term = terms[i].Trim();

                if (term.Equals(target, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Load cache for glossary help's
        /// </summary>
        internal sealed override void LoadCache()
        {
            Collection<String> files = MUIFileSearcher.SearchFiles("*.glossary.xml", GetSearchPaths());

            if (files == null)
                return;

            foreach (string file in files)
            {
                if (!_helpFiles.ContainsKey(file))
                {
                    LoadHelpFile(file);
                    // Add this file into _helpFiles hashtable to prevent it to be loaded again.
                    _helpFiles[file] = 0;
                }
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
        /// </remarks>
        /// <param name="helpFile"></param>
        private void LoadHelpFile(string helpFile)
        {
            if (String.IsNullOrEmpty(helpFile))
            {
                return;
            }

            XmlDocument doc;
            try
            {
                doc = InternalDeserializer.LoadUnsafeXmlDocument(
                    new FileInfo(helpFile),
                    false, /* ignore whitespace, comments, etc. */
                    null); /* default maxCharactersInDocument */
            }
            catch (IOException ioException)
            {
                ErrorRecord errorRecord = new ErrorRecord(ioException, "HelpFileLoadFailure", ErrorCategory.OpenError, null);
                errorRecord.ErrorDetails = new ErrorDetails(typeof(GlossaryHelpProvider).GetTypeInfo().Assembly, "HelpErrors", "HelpFileLoadFailure", helpFile, ioException.Message);
                this.HelpSystem.LastErrors.Add(errorRecord);
                return;
            }
            catch (System.Security.SecurityException securityException)
            {
                ErrorRecord errorRecord = new ErrorRecord(securityException, "HelpFileNotAccessible", ErrorCategory.OpenError, null);
                errorRecord.ErrorDetails = new ErrorDetails(typeof(GlossaryHelpProvider).GetTypeInfo().Assembly, "HelpErrors", "HelpFileNotAccessible", helpFile, securityException.Message);
                this.HelpSystem.LastErrors.Add(errorRecord);
                return;
            }
            catch (XmlException xmlException)
            {
                ErrorRecord errorRecord = new ErrorRecord(xmlException, "HelpFileNotValid", ErrorCategory.SyntaxError, null);
                errorRecord.ErrorDetails = new ErrorDetails(typeof(GlossaryHelpProvider).GetTypeInfo().Assembly, "HelpErrors", "HelpFileNotValid", helpFile, xmlException.Message);
                this.HelpSystem.LastErrors.Add(errorRecord);
                return;
            }

            XmlNode helpItemsNode = null;

            if (doc.HasChildNodes)
            {
                for (int i = 0; i < doc.ChildNodes.Count; i++)
                {
                    XmlNode node = doc.ChildNodes[i];
                    if (node.NodeType == XmlNodeType.Element && String.Compare(node.Name, "glossary", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        helpItemsNode = node;
                        break;
                    }
                }
            }

            if (helpItemsNode == null)
                return;

            using (this.HelpSystem.Trace(helpFile))
            {
                if (helpItemsNode.HasChildNodes)
                {
                    for (int i = 0; i < helpItemsNode.ChildNodes.Count; i++)
                    {
                        XmlNode node = helpItemsNode.ChildNodes[i];
                        if (node.NodeType == XmlNodeType.Element && String.Compare(node.Name, "glossaryEntry", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            HelpInfo helpInfo = null;

                            helpInfo = GlossaryHelpInfo.Load(node);

                            if (helpInfo != null)
                            {
                                this.HelpSystem.TraceErrors(helpInfo.Errors);
                                AddCache(helpInfo.Name, helpInfo);
                            }
                            continue;
                        }

                        if (node.NodeType == XmlNodeType.Element && String.Compare(node.Name, "glossaryDiv", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            LoadGlossaryDiv(node);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlNode"></param>
        private void LoadGlossaryDiv(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return;

            for (int i = 0; i < xmlNode.ChildNodes.Count; i++)
            {
                XmlNode node = xmlNode.ChildNodes[i];
                if (node.NodeType == XmlNodeType.Element && String.Compare(node.Name, "glossaryEntry", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    HelpInfo helpInfo = null;

                    helpInfo = GlossaryHelpInfo.Load(node);

                    if (helpInfo != null)
                    {
                        this.HelpSystem.TraceErrors(helpInfo.Errors);
                        AddCache(helpInfo.Name, helpInfo);
                    }
                }
            }
        }

        #endregion

        # region Help Provider Interface

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

        #region Private Data

        /// <summary>
        /// This is a hashtable to track which help files are loaded already. 
        /// 
        /// This will avoid one help file getting loaded again and again. 
        /// </summary>
        private readonly Hashtable _helpFiles = new Hashtable();

        #endregion
    }
}
