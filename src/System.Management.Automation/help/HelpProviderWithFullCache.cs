// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation
{
    /// <summary>
    /// Class HelpProviderWithFullCache provides a pseudo implementation of HelpProvider
    /// at which results are fully cached in a hashtable after initial cache load.
    ///
    /// This class is different from HelpProviderWithCache class in the sense that
    /// help contents for this provider can be loaded once and be used for later
    /// search. So logically class derived from this class only need to provide
    /// a way to load and initialize help cache.
    /// </summary>
    internal abstract class HelpProviderWithFullCache : HelpProviderWithCache
    {
        /// <summary>
        /// Constructor for HelpProviderWithFullCache.
        /// </summary>
        internal HelpProviderWithFullCache(HelpSystem helpSystem) : base(helpSystem)
        {
        }

        /// <summary>
        /// Exact match help for a target. This function will be sealed right here
        /// since this is no need for children class to override this member.
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        /// <returns>The HelpInfo found. Null if nothing is found.</returns>
        internal sealed override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            if (!this.CacheFullyLoaded)
            {
                LoadCache();
            }

            this.CacheFullyLoaded = true;

            return base.ExactMatchHelp(helpRequest);
        }

        /// <summary>
        /// Do exact match help for a target. This member is sealed right here since
        /// children class don't need to override this member.
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        internal sealed override void DoExactMatchHelp(HelpRequest helpRequest)
        {
        }

        /// <summary>
        /// Search help for a target. This function will be sealed right here
        /// since this is no need for children class to override this member.
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        /// <param name="searchOnlyContent">
        /// If true, searches for pattern in the help content. Individual
        /// provider can decide which content to search in.
        ///
        /// If false, searches for pattern in the command names.
        /// </param>
        /// <returns>A collection of help info objects.</returns>
        internal sealed override IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent)
        {
            if (!this.CacheFullyLoaded)
            {
                LoadCache();
            }

            this.CacheFullyLoaded = true;

            return base.SearchHelp(helpRequest, searchOnlyContent);
        }

        /// <summary>
        /// Do search help. This function will be sealed right here
        /// since this is no need for children class to override this member.
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        /// <returns>A collection of help info objects.</returns>
        internal sealed override IEnumerable<HelpInfo> DoSearchHelp(HelpRequest helpRequest)
        {
            return null;
        }

        /// <summary>
        /// Load cache for later searching for help.
        /// </summary>
        /// <remarks>
        /// This is the only member child class need to override for help search purpose.
        /// This function will be called only once (usually this happens at the first time when
        /// end user request some help in the target help category).
        /// </remarks>
        internal virtual void LoadCache()
        {
        }
    }
}
