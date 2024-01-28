// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// The ProviderCommandHelpInfo class.
    /// </summary>
    internal sealed class ProviderCommandHelpInfo : HelpInfo
    {
        /// <summary>
        /// Help info.
        /// </summary>
        private readonly HelpInfo _helpInfo;

        /// <summary>
        /// Constructor for ProviderCommandHelpInfo.
        /// </summary>
        internal ProviderCommandHelpInfo(HelpInfo genericHelpInfo, ProviderContext providerContext)
        {
            Dbg.Assert(genericHelpInfo != null, "Expected genericHelpInfo != null");
            Dbg.Assert(providerContext != null, "Expected providerContext != null");

            // This should be set to None to prevent infinite forwarding.
            this.ForwardHelpCategory = HelpCategory.None;

            // Now pick which help we should show.
            MamlCommandHelpInfo providerSpecificHelpInfo =
                providerContext.GetProviderSpecificHelpInfo(genericHelpInfo.Name);
            if (providerSpecificHelpInfo == null)
            {
                _helpInfo = genericHelpInfo;
            }
            else
            {
                providerSpecificHelpInfo.OverrideProviderSpecificHelpWithGenericHelp(genericHelpInfo);
                _helpInfo = providerSpecificHelpInfo;
            }
        }

        /// <summary>
        /// Get parameter.
        /// </summary>
        internal override PSObject[] GetParameter(string pattern)
        {
            return _helpInfo.GetParameter(pattern);
        }

        /// <summary>
        /// Returns the Uri used by get-help cmdlet to show help
        /// online. Returns only the first uri found under
        /// RelatedLinks.
        /// </summary>
        /// <returns>
        /// Null if no Uri is specified by the helpinfo or a
        /// valid Uri.
        /// </returns>
        internal override Uri GetUriForOnlineHelp()
        {
            return _helpInfo.GetUriForOnlineHelp();
        }

        /// <summary>
        /// The Name property.
        /// </summary>
        internal override string Name
        {
            get
            {
                return _helpInfo.Name;
            }
        }

        /// <summary>
        /// The Synopsis property.
        /// </summary>
        internal override string Synopsis
        {
            get
            {
                return _helpInfo.Synopsis;
            }
        }

        /// <summary>
        /// The HelpCategory property.
        /// </summary>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return _helpInfo.HelpCategory;
            }
        }

        /// <summary>
        /// The FullHelp property.
        /// </summary>
        internal override PSObject FullHelp
        {
            get
            {
                return _helpInfo.FullHelp;
            }
        }

        /// <summary>
        /// The Component property.
        /// </summary>
        internal override string Component
        {
            get
            {
                return _helpInfo.Component;
            }
        }

        /// <summary>
        /// The Role property.
        /// </summary>
        internal override string Role
        {
            get
            {
                return _helpInfo.Role;
            }
        }

        /// <summary>
        /// The Functionality property.
        /// </summary>
        internal override string Functionality
        {
            get
            {
                return _helpInfo.Functionality;
            }
        }
    }
}
