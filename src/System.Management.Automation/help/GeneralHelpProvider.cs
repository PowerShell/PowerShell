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
    /// Class GeneralHelpProvider implement the help provider for general help topics.
    /// </summary>
    /// 
    /// <remarks>
    /// General Help information are stored in 'concept.xml' files. These files are
    /// located in the Monad / CustomShell Path as well as in the Application Base
    /// of PSSnapIns
    /// </remarks>
    internal class GeneralHelpProvider : HelpProviderWithFullCache
    {
        /// <summary>
        /// Constructor for GeneralHelpProvider
        /// </summary>
        internal GeneralHelpProvider(HelpSystem helpSystem) : base(helpSystem)
        {
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
                return "General Help Provider";
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
                return HelpCategory.General;
            }
        }

        #endregion

        #region Load cache

        /// <summary>
        /// Load cache for general help's
        /// </summary>
        internal sealed override void LoadCache()
        {
            Collection<String> files = MUIFileSearcher.SearchFiles("*.concept.xml", GetSearchPaths());

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
                errorRecord.ErrorDetails = new ErrorDetails(typeof(GeneralHelpProvider).GetTypeInfo().Assembly, "HelpErrors", "HelpFileLoadFailure", helpFile, ioException.Message);
                this.HelpSystem.LastErrors.Add(errorRecord);
                return;
            }
            catch (System.Security.SecurityException securityException)
            {
                ErrorRecord errorRecord = new ErrorRecord(securityException, "HelpFileNotAccessible", ErrorCategory.OpenError, null);
                errorRecord.ErrorDetails = new ErrorDetails(typeof(GeneralHelpProvider).GetTypeInfo().Assembly, "HelpErrors", "HelpFileNotAccessible", helpFile, securityException.Message);
                this.HelpSystem.LastErrors.Add(errorRecord);
                return;
            }
            catch (XmlException xmlException)
            {
                ErrorRecord errorRecord = new ErrorRecord(xmlException, "HelpFileNotValid", ErrorCategory.SyntaxError, null);
                errorRecord.ErrorDetails = new ErrorDetails(typeof(GeneralHelpProvider).GetTypeInfo().Assembly, "HelpErrors", "HelpFileNotValid", helpFile, xmlException.Message);
                this.HelpSystem.LastErrors.Add(errorRecord);
                return;
            }

            XmlNode helpItemsNode = null;

            if (doc.HasChildNodes)
            {
                for (int i = 0; i < doc.ChildNodes.Count; i++)
                {
                    XmlNode node = doc.ChildNodes[i];
                    if (node.NodeType == XmlNodeType.Element && String.Compare(node.Name, "conceptuals", StringComparison.OrdinalIgnoreCase) == 0)
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
                        if (node.NodeType == XmlNodeType.Element && String.Compare(node.Name, "conceptual", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            HelpInfo helpInfo = null;

                            helpInfo = GeneralHelpInfo.Load(node);

                            if (helpInfo != null)
                            {
                                this.HelpSystem.TraceErrors(helpInfo.Errors);
                                AddCache(helpInfo.Name, helpInfo);
                            }
                        }
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
