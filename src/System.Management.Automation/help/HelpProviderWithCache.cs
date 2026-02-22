// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation
{
    /// <summary>
    /// Class HelpProviderWithCache provides a pseudo implementation of HelpProvider
    /// at which results are cached in a hashtable so that later retrieval can be
    /// faster.
    /// </summary>
    internal abstract class HelpProviderWithCache : HelpProvider
    {
        /// <summary>
        /// Constructor for HelpProviderWithCache.
        /// </summary>
        internal HelpProviderWithCache(HelpSystem helpSystem) : base(helpSystem)
        {
        }

        #region Help Provider Interface

        /// <summary>
        /// _helpCache is a hashtable to stores helpInfo.
        /// </summary>
        /// <remarks>
        /// This hashtable is made case-insensitive so that helpInfo can be retrieved case insensitively.
        /// </remarks>
        private readonly Dictionary<string, HelpInfo> _helpCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Exact match help for a target.
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        /// <returns>The HelpInfo found. Null if nothing is found.</returns>
        internal override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            string target = helpRequest.Target;

            if (!this.HasCustomMatch)
            {
                if (_helpCache.TryGetValue(target, out HelpInfo value))
                {
                    yield return value;
                }
            }
            else
            {
                foreach (string key in _helpCache.Keys)
                {
                    if (CustomMatch(target, key))
                    {
                        yield return _helpCache[key];
                    }
                }
            }

            if (!this.CacheFullyLoaded)
            {
                DoExactMatchHelp(helpRequest);
                if (_helpCache.TryGetValue(target, out HelpInfo value))
                {
                    yield return value;
                }
            }
        }

        /// <summary>
        /// This is for child class to indicate that it has implemented
        /// a custom way of match.
        /// </summary>
        /// <value></value>
        protected bool HasCustomMatch { get; set; } = false;

        /// <summary>
        /// This is for implementing custom match algorithm.
        /// </summary>
        /// <param name="target">Target to search.</param>
        /// <param name="key">Key used in cache table.</param>
        /// <returns></returns>
        protected virtual bool CustomMatch(string target, string key)
        {
            return target == key;
        }

        /// <summary>
        /// Do exact match help for a target.
        /// </summary>
        /// <remarks>
        /// Derived class can choose to either override ExactMatchHelp method to DoExactMatchHelp method.
        /// If ExactMatchHelp is overridden, initial cache checking will be disabled by default.
        /// If DoExactMatchHelp is overridden, cache check will be done first in ExactMatchHelp before the
        /// logic in DoExactMatchHelp is in place.
        /// </remarks>
        /// <param name="helpRequest">Help request object.</param>
        internal virtual void DoExactMatchHelp(HelpRequest helpRequest)
        {
        }

        /// <summary>
        /// Search help for a target.
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        /// <param name="searchOnlyContent">
        /// If true, searches for pattern in the help content. Individual
        /// provider can decide which content to search in.
        ///
        /// If false, searches for pattern in the command names.
        /// </param>
        /// <returns>A collection of help info objects.</returns>
        internal override IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent)
        {
            string target = helpRequest.Target;

            string wildcardpattern = GetWildCardPattern(target);

            HelpRequest searchHelpRequest = helpRequest.Clone();
            searchHelpRequest.Target = wildcardpattern;
            if (!this.CacheFullyLoaded)
            {
                IEnumerable<HelpInfo> result = DoSearchHelp(searchHelpRequest);
                if (result != null)
                {
                    foreach (HelpInfo helpInfoToReturn in result)
                    {
                        yield return helpInfoToReturn;
                    }
                }
            }
            else
            {
                int countOfHelpInfoObjectsFound = 0;
                WildcardPattern helpMatcher = WildcardPattern.Get(wildcardpattern, WildcardOptions.IgnoreCase);
                foreach (string key in _helpCache.Keys)
                {
                    if ((!searchOnlyContent && helpMatcher.IsMatch(key)) ||
                        (searchOnlyContent && _helpCache[key].MatchPatternInContent(helpMatcher)))
                    {
                        countOfHelpInfoObjectsFound++;
                        yield return _helpCache[key];
                        if (helpRequest.MaxResults > 0 && countOfHelpInfoObjectsFound >= helpRequest.MaxResults)
                        {
                            yield break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create a wildcard pattern based on a target.
        ///
        /// Here we provide the default implementation of this, covering following
        /// two cases
        ///     a. if target has wildcard pattern, return as it is.
        ///     b. if target doesn't have wildcard pattern, postfix it with *
        ///
        /// Child class of this one may choose to override this function.
        /// </summary>
        /// <param name="target">Target string.</param>
        /// <returns>Wild card pattern created.</returns>
        internal virtual string GetWildCardPattern(string target)
        {
            if (WildcardPattern.ContainsWildcardCharacters(target))
                return target;

            return "*" + target + "*";
        }

        /// <summary>
        /// Do search help. This is for child class to override.
        /// </summary>
        /// <remarks>
        /// Child class can choose to override SearchHelp of DoSearchHelp depending on
        /// whether it want to reuse the logic in SearchHelp for this class.
        /// </remarks>
        /// <param name="helpRequest">Help request object.</param>
        /// <returns>A collection of help info objects.</returns>
        internal virtual IEnumerable<HelpInfo> DoSearchHelp(HelpRequest helpRequest)
        {
            yield break;
        }

        /// <summary>
        /// Add an help entry to cache.
        /// </summary>
        /// <param name="target">The key of the help entry.</param>
        /// <param name="helpInfo">HelpInfo object as the value of the help entry.</param>
        internal void AddCache(string target, HelpInfo helpInfo)
        {
            _helpCache[target] = helpInfo;
        }

        /// <summary>
        /// Get help entry from cache.
        /// </summary>
        /// <param name="target">The key for the help entry to retrieve.</param>
        /// <returns>The HelpInfo in cache corresponding the key specified.</returns>
        internal HelpInfo GetCache(string target)
        {
            return _helpCache[target];
        }

        /// <summary>
        /// Is cached fully loaded?
        ///
        /// If cache is fully loaded, search/exactmatch Help can short cut the logic
        /// in various help providers to get help directly from cache.
        ///
        /// This indicator is usually set by help providers derived from this class.
        /// </summary>
        /// <value></value>
        protected internal bool CacheFullyLoaded { get; set; } = false;

        /// <summary>
        /// This will reset the help cache. Normally this corresponds to a
        /// help culture change.
        /// </summary>
        internal override void Reset()
        {
            base.Reset();

            _helpCache.Clear();
            CacheFullyLoaded = false;
        }

        #endregion
    }
}
