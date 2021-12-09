// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace System.Management.Automation
{
    internal sealed class MUIFileSearcher
    {
        /// <summary>
        /// Constructor. It is private so that MUIFileSearcher is used only internal for this class.
        /// To access functionality in this class, static api should be used.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="searchPaths"></param>
        /// <param name="searchMode"></param>
        private MUIFileSearcher(string target, Collection<string> searchPaths, SearchMode searchMode)
        {
            Target = target;
            SearchPaths = searchPaths;
            SearchMode = searchMode;
        }

        /// <summary>
        /// A constructor to make searchMode optional.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="searchPaths"></param>
        private MUIFileSearcher(string target, Collection<string> searchPaths)
            : this(target, searchPaths, SearchMode.Unique)
        {
        }

        #region Basic Properties

        /// <summary>
        /// Search target. It can be
        ///     1. a file name
        ///     2. a search pattern
        /// It can also include a path, in that case,
        ///     1. the path will be searched first for the existence of the files.
        /// </summary>
        internal string Target { get; } = null;

        /// <summary>
        /// Search path as provided by user.
        /// </summary>
        internal Collection<string> SearchPaths { get; } = null;

        /// <summary>
        /// Search mode for this file search.
        /// </summary>
        internal SearchMode SearchMode { get; } = SearchMode.Unique;

        private Collection<string> _result = null;

        /// <summary>
        /// Result of the search.
        /// </summary>
        internal Collection<string> Result
        {
            get
            {
                if (_result == null)
                {
                    _result = new Collection<string>();

                    // SearchForFiles will fill the result collection.
                    SearchForFiles();
                }

                return _result;
            }
        }

        #endregion

        #region File Search

        /// <summary>
        /// _uniqueMatches is used to track matches already found during the search process.
        /// This is useful for ignoring duplicates in the case of unique search.
        /// </summary>
        private readonly Hashtable _uniqueMatches = new Hashtable(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Search for files using the target, searchPaths member of this class.
        /// </summary>
        private void SearchForFiles()
        {
            if (string.IsNullOrEmpty(this.Target))
                return;

            string pattern = Path.GetFileName(this.Target);
            if (string.IsNullOrEmpty(pattern))
                return;

            Collection<string> normalizedSearchPaths = NormalizeSearchPaths(this.Target, this.SearchPaths);

            foreach (string directory in normalizedSearchPaths)
            {
                SearchForFiles(pattern, directory);

                if (this.SearchMode == SearchMode.First && this.Result.Count > 0)
                {
                    return;
                }
            }
        }

        private static string[] GetFiles(string path, string pattern)
        {
#if UNIX
            // On Linux, file names are case sensitive, so we need to add
            // extra logic to select the files that match the given pattern.
            var result = new List<string>();
            string[] files = Directory.GetFiles(path);

            var wildcardPattern = WildcardPattern.ContainsWildcardCharacters(pattern)
                ? WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase)
                : null;

            foreach (string filePath in files)
            {
                if (filePath.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(filePath);
                    break;
                }

                if (wildcardPattern != null)
                {
                    string fileName = Path.GetFileName(filePath);
                    if (wildcardPattern.IsMatch(fileName))
                    {
                        result.Add(filePath);
                    }
                }
            }

            return result.ToArray();
#else
            return Directory.GetFiles(path, pattern);
#endif
        }

        private void AddFiles(string muiDirectory, string directory, string pattern)
        {
            if (Directory.Exists(muiDirectory))
            {
                string[] files = GetFiles(muiDirectory, pattern);

                if (files == null)
                    return;

                foreach (string file in files)
                {
                    string path = Path.Combine(muiDirectory, file);

                    switch (this.SearchMode)
                    {
                        case SearchMode.All:
                            _result.Add(path);
                            break;

                        case SearchMode.Unique:
                            // Construct a Unique filename for this directory.
                            // Remember the file may belong to one of the sub-culture
                            // directories. In this case we should not be returning
                            // same files that are residing in 2 or more sub-culture
                            // directories.
                            string leafFileName = Path.GetFileName(file);
                            string uniqueToDirectory = Path.Combine(directory, leafFileName);

                            if (!_result.Contains(path) && !_uniqueMatches.Contains(uniqueToDirectory))
                            {
                                _result.Add(path);
                                _uniqueMatches[uniqueToDirectory] = true;
                            }

                            break;

                        case SearchMode.First:
                            _result.Add(path);
                            return;

                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Search for files of a particular pattern under a particular directory.
        /// This will do MUI search in which appropriate language directories are
        /// searched in order.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="directory"></param>
        private void SearchForFiles(string pattern, string directory)
        {
            List<string> cultureNameList = new List<string>();
            CultureInfo culture = CultureInfo.CurrentUICulture;

            while (culture != null && !string.IsNullOrEmpty(culture.Name))
            {
                cultureNameList.Add(culture.Name);
                culture = culture.Parent;
            }

            cultureNameList.Add(string.Empty);

            // Add en-US and en as fallback languages
            if (!cultureNameList.Contains("en-US"))
            {
                cultureNameList.Add("en-US");
            }

            if (!cultureNameList.Contains("en"))
            {
                cultureNameList.Add("en");
            }

            foreach (string name in cultureNameList)
            {
                string muiDirectory = Path.Combine(directory, name);

                AddFiles(muiDirectory, directory, pattern);

                if (this.SearchMode == SearchMode.First && this.Result.Count > 0)
                {
                    return;
                }
            }

            return;
        }

        /// <summary>
        /// A help file is located in 3 steps
        ///     1. If file itself contains a path itself, try to locate the file
        ///        from path. LocateFile will fail if this file doesn't exist.
        ///     2. Try to locate the file from searchPaths. Normally the searchPaths will
        ///        contain the cmdlet/provider assembly directory if currently we are searching
        ///        help for cmdlet and providers.
        ///     3. Try to locate the file in the default PowerShell installation directory.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="searchPaths"></param>
        /// <returns></returns>
        private static Collection<string> NormalizeSearchPaths(string target, Collection<string> searchPaths)
        {
            Collection<string> result = new Collection<string>();

            // step 1: if target has path attached, directly locate
            //         file from there.
            if (!string.IsNullOrEmpty(target) && !string.IsNullOrEmpty(Path.GetDirectoryName(target)))
            {
                string directory = Path.GetDirectoryName(target);

                if (Directory.Exists(directory))
                {
                    result.Add(Path.GetFullPath(directory));
                }

                // user specifically wanted to search in a particular directory
                // so return..
                return result;
            }

            // step 2: add directories specified in to search path.
            if (searchPaths != null)
            {
                foreach (string directory in searchPaths)
                {
                    if (!result.Contains(directory) && Directory.Exists(directory))
                    {
                        result.Add(directory);
                    }
                }
            }

            // step 3: locate the file in the default PowerShell installation directory.
            string defaultPSPath = Utils.GetApplicationBase(Utils.DefaultPowerShellShellID);
            if (defaultPSPath != null &&
                !result.Contains(defaultPSPath) &&
                Directory.Exists(defaultPSPath))
            {
                result.Add(defaultPSPath);
            }

            return result;
        }

        #endregion

        #region Static API's

        /// <summary>
        /// Search for files in default search paths.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        internal static Collection<string> SearchFiles(string pattern)
        {
            return SearchFiles(pattern, new Collection<string>());
        }

        /// <summary>
        /// Search for files in specified search paths.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="searchPaths"></param>
        /// <returns></returns>
        internal static Collection<string> SearchFiles(string pattern, Collection<string> searchPaths)
        {
            MUIFileSearcher searcher = new MUIFileSearcher(pattern, searchPaths);

            return searcher.Result;
        }

        /// <summary>
        /// Locate a file in default search paths.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal static string LocateFile(string file)
        {
            return LocateFile(file, new Collection<string>());
        }

        /// <summary>
        /// Get the file in different search paths corresponding to current culture.
        ///
        /// The file name to search is the filename part of path parameter. (Normally path
        /// parameter should contain only the filename part).
        /// </summary>
        /// <param name="file">This is the path to the file. If it has a path, we need to search under that path first.</param>
        /// <param name="searchPaths">Additional search paths.</param>
        /// <returns></returns>
        internal static string LocateFile(string file, Collection<string> searchPaths)
        {
            MUIFileSearcher searcher = new MUIFileSearcher(file, searchPaths, SearchMode.First);

            if (searcher.Result == null || searcher.Result.Count == 0)
                return null;

            return searcher.Result[0];
        }

        #endregion
    }

    /// <summary>
    /// This enum defines different search mode for the MUIFileSearcher.
    /// </summary>
    internal enum SearchMode
    {
        // return the first match
        First,

        // return all matches, with duplicates allowed
        All,

        // return all matches, with duplicates ignored
        Unique
    }
}
