// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation
{
    /// <summary>
    /// Class DefaultHelpProvider implement the help provider for commands.
    ///
    /// Command Help information are stored in 'help.xml' files. Location of these files
    /// can be found from CommandDiscovery.
    /// </summary>
    internal sealed class DefaultHelpProvider : HelpFileHelpProvider
    {
        /// <summary>
        /// Constructor for HelpProvider.
        /// </summary>
        internal DefaultHelpProvider(HelpSystem helpSystem)
            : base(helpSystem)
        {
        }

        #region Common Properties

        /// <summary>
        /// </summary>
        /// <value></value>
        internal override string Name
        {
            get
            {
                return "Default Help Provider";
            }
        }

        /// <summary>
        /// </summary>
        /// <value></value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.DefaultHelp;
            }
        }

        #endregion

        #region Help Provider Interface

        /// <summary>
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        /// <returns></returns>
        internal override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            HelpRequest defaultHelpRequest = helpRequest.Clone();
            defaultHelpRequest.Target = "default";
            return base.ExactMatchHelp(defaultHelpRequest);
        }

        #endregion
    }
}
