/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    /// <summary>
    /// 
    /// Class HelpFileHelpProvider implement the help provider for help.txt kinds of 
    /// help contents.
    /// 
    /// Help File help information are stored in '.help.txt' files. These files are
    /// located in the Monad / CustomShell Path as well as in the Application Base
    /// of PSSnapIns
    /// 
    /// </summary>
    internal class HelpFileHelpProvider: HelpProviderWithCache
    {
        /// <summary>
        /// Constructor for HelpProvider
        /// </summary>
        internal HelpFileHelpProvider(HelpSystem helpSystem) : base(helpSystem)
        {
        }

        #region Common Properties

        /// <summary>
        /// Name of the provider
        /// </summary>
        /// <value>Name of the provider</value>
        override internal string Name
        {
            get
            {
                return "HelpFile Help Provider";
            }
        }

        /// <summary>
        /// Help category of the provider
        /// </summary>
        /// <value>Help category of the provider</value>
        override internal HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.HelpFile;
            }
        }

        #endregion

        #region Help Provider Interface

        internal override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            int countHelpInfosFound = 0;
            string helpFileName = helpRequest.Target + ".help.txt";
            Collection<string> filesMatched = MUIFileSearcher.SearchFiles(helpFileName, GetExtendedSearchPaths());

            Diagnostics.Assert(filesMatched != null, "Files collection should not be null.");

            foreach (string file in filesMatched)
            {
                // Check whether the file is already loaded
                if (!_helpFiles.ContainsKey(file))
                {
                    try
                    {
                        LoadHelpFile(file);
                    }
                    catch (IOException ioException)
                    {
                        ReportHelpFileError(ioException, helpRequest.Target, file);
                    }
                    catch (System.Security.SecurityException securityException)
                    {
                        ReportHelpFileError(securityException, helpRequest.Target, file);
                    }
                }

                HelpInfo helpInfo = GetCache(file);

                if (helpInfo != null)
                {
                    countHelpInfosFound++;
                    yield return helpInfo;

                    if ((countHelpInfosFound >= helpRequest.MaxResults) && (helpRequest.MaxResults > 0))
                        yield break;
                }
            }
        }

        internal override IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent)
        {
            string target = helpRequest.Target;
            string pattern = target;
            int countOfHelpInfoObjectsFound = 0;
            // this will be used only when searchOnlyContent == true
            WildcardPattern wildCardPattern = null;

            if ((!searchOnlyContent) && (!WildcardPattern.ContainsWildcardCharacters(target)))
            {
                // Search all the about conceptual topics. This pattern
                // makes about topics discoverable without actually
                // using the word "about_" as in "get-help while".
                pattern = "*" + pattern + "*";
            }

            if (searchOnlyContent)
            {
                string searchTarget = helpRequest.Target;
                if(!WildcardPattern.ContainsWildcardCharacters(helpRequest.Target))
                {
                    searchTarget = "*" + searchTarget + "*";
                }

                wildCardPattern = WildcardPattern.Get(searchTarget, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);
                // search all about_* topics
                pattern = "*";
            }

            pattern += ".help.txt";

            Collection<String> files = MUIFileSearcher.SearchFiles(pattern, GetExtendedSearchPaths());

            if (files == null)
                yield break;

            foreach (string file in files)
            {
                // Check whether the file is already loaded
                if (!_helpFiles.ContainsKey(file))
                {
                    try
                    {
                        LoadHelpFile(file);
                    }
                    catch (IOException ioException)
                    {
                        ReportHelpFileError(ioException, helpRequest.Target, file);
                    }
                    catch (System.Security.SecurityException securityException)
                    {
                        ReportHelpFileError(securityException, helpRequest.Target, file);
                    }
                }

                HelpFileHelpInfo helpInfo = GetCache(file) as HelpFileHelpInfo;

                if (helpInfo != null)
                {
                    if (searchOnlyContent)
                    {
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

        /// <summary>
        /// Load help file based on the file path.
        /// </summary>
        /// <param name="path">file path to load help from</param>
        /// <returns>Help info object loaded from the file</returns>
        private HelpInfo LoadHelpFile(string path)
        {
            string fileName = Path.GetFileName(path);

            //Bug906435: Get-help for special devices throws an exception
            //There might be situations where path does not end with .help.txt extension
            //The assumption that path ends with .help.txt is broken under special 
            //conditions when user uses "get-help" with device names like "prn","com1" etc.
            //First check whether path ends with .help.txt.                

            // If path does not end with ".help.txt" return.
            if (!path.EndsWith(".help.txt", StringComparison.OrdinalIgnoreCase))
                return null;

            string name = fileName.Substring(0, fileName.Length -  9 /* ".help.txt".Length */);

            if (String.IsNullOrEmpty(name))
                return null;

            HelpInfo helpInfo = GetCache(path);

            if (helpInfo != null)
                return helpInfo;

            string helpText = null;

            using (TextReader tr = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                helpText = tr.ReadToEnd();
            }
            // Add this file into _helpFiles hashtable to prevent it to be loaded again.
            _helpFiles[path] = 0;
            helpInfo = HelpFileHelpInfo.GetHelpInfo(name, helpText, path);

            AddCache(path, helpInfo);

            return helpInfo;
        }

        /// <summary>
        /// Gets the extended search paths for about_topics help. To be able to get about_topics help from unloaded modules,
        /// we will add $pshome and the folders under PS module paths to the collection of paths to search.
        /// </summary>
        /// <returns>a collection of string representing locations</returns>
        internal Collection<string> GetExtendedSearchPaths()
        {
            Collection<String> searchPaths = GetSearchPaths();

            // Add $pshome at the top of the list
            String defaultShellSearchPath = GetDefaultShellSearchPath();
            int index = searchPaths.IndexOf(defaultShellSearchPath);
            if (index != 0)
            {
                if (index > 0)
                {
                    searchPaths.RemoveAt(index);
                }
                searchPaths.Insert(0, defaultShellSearchPath);
            }

            // Add modules that are not loaded. Since using 'get-module -listavailable' is very expensive,
            // we load all the directories (which are not empty) under the module path.
            foreach (string psModulePath in ModuleIntrinsics.GetModulePath(false, this.HelpSystem.ExecutionContext))
            {
                if (Directory.Exists(psModulePath))
                {
                    try
                    {
                        // Get all the directories under the module path
                        // * and SearchOption.AllDirectories gets all the version directories.
                        string[] directories = Directory.GetDirectories(psModulePath, "*", SearchOption.AllDirectories);

                        var possibleModuleDirectories = directories.Where(directory => ModuleUtils.IsPossibleModuleDirectory(directory));
                        
                        foreach (string directory in possibleModuleDirectories)
                        {
                            // Add only directories that are not empty
                            if (Directory.EnumerateFiles(directory).Any())
                            {
                                if (!searchPaths.Contains(directory))
                                {
                                    searchPaths.Add(directory);
                                }
                            }
                        }
                    }
                    // Absorb any exception related to enumerating directories
                    catch (System.ArgumentException) { }
                    catch (System.IO.IOException) { }
                    catch (System.UnauthorizedAccessException) { }
                    catch (System.Security.SecurityException) { }
                }
            }
            return searchPaths;
        }

        /// <summary>
        /// This will reset the help cache. Normally this corresponds to a 
        /// help culture change. 
        /// </summary>
        override internal void Reset()
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
        private Hashtable _helpFiles = new Hashtable();

        #endregion
    }
}

