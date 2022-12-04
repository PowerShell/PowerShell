// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;

using System.Management.Automation.Runspaces;

namespace System.Management.Automation
{
    /// <summary>
    /// Class HelpProvider defines the interface to be implemented by help providers.
    ///
    /// Help Providers:
    ///     The basic contract for help providers is to provide help based on the
    ///     search target.
    ///
    ///     The result of help provider invocation can be three things:
    ///         a. Full help info. (in the case of exact-match and single search result)
    ///         b. Short help info. (in the case of multiple search result)
    ///         c. Partial help info. (in the case of some commandlet help info, which
    ///                                 should be supplemented by provider help info)
    ///         d. Help forwarding info. (in the case of alias, which will change the target
    ///                                   for alias)
    ///
    ///     Help providers may need to provide functionality in following two area,
    ///         a. caching and indexing to boost performance
    ///         b. localization
    ///
    /// Basic properties of a Help Provider
    ///     1. Name
    ///     2. Type
    ///     3. Assembly
    ///
    /// Help Provider Interface
    ///     1. Initialize:
    ///     2. ExactMatchHelp:
    ///     3. SearchHelp:
    ///     4. ProcessForwardedHelp.
    /// </summary>
    internal abstract class HelpProvider
    {
        /// <summary>
        /// Constructor for HelpProvider.
        /// </summary>
        internal HelpProvider(HelpSystem helpSystem)
        {
            _helpSystem = helpSystem;
        }

        private readonly HelpSystem _helpSystem;

        internal HelpSystem HelpSystem
        {
            get
            {
                return _helpSystem;
            }
        }

        #region Common Properties

        /// <summary>
        /// Name for the help provider.
        /// </summary>
        /// <value>Name for the help provider</value>
        /// <remarks>Derived classes should set this.</remarks>
        internal abstract string Name
        {
            get;
        }

        /// <summary>
        /// Help category for the help provider.
        /// </summary>
        /// <value>Help category for the help provider</value>
        /// <remarks>Derived classes should set this.</remarks>
        internal abstract HelpCategory HelpCategory
        {
            get;
        }

#if V2

        /// <summary>
        /// Assembly that contains the help provider.
        /// </summary>
        /// <value>Assembly name</value>
        virtual internal string AssemblyName
        {
            get
            {
                return Assembly.GetExecutingAssembly().FullName;
            }
        }

        /// <summary>
        /// Class that implements the help provider.
        /// </summary>
        /// <value>Class name</value>
        virtual internal string ClassName
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        /// <summary>
        /// Get an provider info object based on the basic information in this provider.
        /// </summary>
        /// <value>An mshObject that contains the providerInfo</value>
        internal PSObject ProviderInfo
        {
            get
            {
                PSObject result = new PSObject();
                result.Properties.Add(new PSNoteProperty("Name", this.Name));
                result.Properties.Add(new PSNoteProperty("Category", this.HelpCategory.ToString()));
                result.Properties.Add(new PSNoteProperty("ClassName", this.ClassName));
                result.Properties.Add(new PSNoteProperty("AssemblyName", this.AssemblyName));

                Collection<string> typeNames = new Collection<string>();
                typeNames.Add("HelpProviderInfo");
                result.TypeNames = typeNames;

                return result;
            }
        }

#endif

        #endregion

        #region Help Provider Interface

        /// <summary>
        /// Retrieve help info that exactly match the target.
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        /// <returns>List of HelpInfo objects retrieved.</returns>
        internal abstract IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest);

        /// <summary>
        /// Search help info that match the target search pattern.
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        /// <param name="searchOnlyContent">
        /// If true, searches for pattern in the help content. Individual
        /// provider can decide which content to search in.
        ///
        /// If false, searches for pattern in the command names.
        /// </param>
        /// <returns>A collection of help info objects.</returns>
        internal abstract IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent);

        /// <summary>
        /// Process a helpinfo forwarded over by another help provider.
        ///
        /// HelpProvider can choose to process the helpInfo or not,
        ///
        ///     1. If a HelpProvider chooses not to process the helpInfo, it can return null to indicate
        ///        helpInfo is not processed.
        ///     2. If a HelpProvider indeed processes the helpInfo, it should create a new helpInfo
        ///        object instead of modifying the passed-in helpInfo object. This is very important
        ///        since the helpInfo object passed in is usually stored in cache, which can
        ///        used in later queries.
        /// </summary>
        /// <param name="helpInfo">HelpInfo passed over by another HelpProvider.</param>
        /// <param name="helpRequest">Help request object.</param>
        /// <returns></returns>
        internal virtual IEnumerable<HelpInfo> ProcessForwardedHelp(HelpInfo helpInfo, HelpRequest helpRequest)
        {
            // Win8: 508648. Remove the current provides category for resolving forward help as the current
            // help provider already process it.
            helpInfo.ForwardHelpCategory ^= this.HelpCategory;
            yield return helpInfo;
        }

        /// <summary>
        /// Reset help provider.
        ///
        /// Normally help provider are reset after a help culture change.
        /// </summary>
        internal virtual void Reset()
        {
            return;
        }

        #endregion

        #region Utility functions

        /// <summary>
        /// Report help file load errors.
        ///
        /// Currently three cases are handled,
        ///
        ///     1. IOException: not able to read the file
        ///     2. SecurityException: not authorized to read the file
        ///     3. XmlException: xml schema error.
        ///
        /// This will be called either from search help or exact match help
        /// to find the error.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="target"></param>
        /// <param name="helpFile"></param>
        internal void ReportHelpFileError(Exception exception, string target, string helpFile)
        {
            ErrorRecord errorRecord = new ErrorRecord(exception, "LoadHelpFileForTargetFailed", ErrorCategory.OpenError, null);
            errorRecord.ErrorDetails = new ErrorDetails(typeof(HelpProvider).Assembly, "HelpErrors", "LoadHelpFileForTargetFailed", target, helpFile, exception.Message);
            this.HelpSystem.LastErrors.Add(errorRecord);
            return;
        }

        /// <summary>
        /// Each Shell ( minishell ) will have its own path specified by the
        /// application base folder, which should be the same as $pshome.
        /// </summary>
        /// <returns>String representing base directory of the executing shell.</returns>
        internal string GetDefaultShellSearchPath()
        {
            return Utils.GetApplicationBase();
        }

        /// <summary>
        /// Gets the search paths. If the current shell is single-shell based, then the returned
        /// search path contains all the directories of currently active PSSnapIns.
        /// </summary>
        /// <returns>A collection of string representing locations.</returns>
        internal Collection<string> GetSearchPaths()
        {
            Collection<string> searchPaths = this.HelpSystem.GetSearchPaths();

            Diagnostics.Assert(searchPaths != null,
                "HelpSystem returned an null search path");

            string defaultShellSearchPath = GetDefaultShellSearchPath();
            if (!searchPaths.Contains(defaultShellSearchPath))
            {
                searchPaths.Add(defaultShellSearchPath);
            }

            return searchPaths;
        }

        #endregion
    }
}
