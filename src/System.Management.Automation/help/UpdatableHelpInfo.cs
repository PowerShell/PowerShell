// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Internal;
using System.Globalization;
using System.Diagnostics;
using System.Text;
using System.Collections.ObjectModel;

namespace System.Management.Automation.Help
{
    /// <summary>
    /// Represents each supported culture.
    /// </summary>
    internal class CultureSpecificUpdatableHelp
    {
        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="culture">Culture info.</param>
        /// <param name="version">Version info.</param>
        internal CultureSpecificUpdatableHelp(CultureInfo culture, Version version)
        {
            Debug.Assert(version != null);
            Debug.Assert(culture != null);

            Culture = culture;
            Version = version;
        }

        /// <summary>
        /// Culture version.
        /// </summary>
        internal Version Version { get; set; }

        /// <summary>
        /// Supported culture.
        /// </summary>
        internal CultureInfo Culture { get; set; }
    }

    /// <summary>
    /// This class represents the HelpInfo metadata XML.
    /// </summary>
    internal class UpdatableHelpInfo
    {
        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="unresolvedUri">Unresolved help content URI.</param>
        /// <param name="cultures">Supported UI cultures.</param>
        internal UpdatableHelpInfo(string unresolvedUri, CultureSpecificUpdatableHelp[] cultures)
        {
            Debug.Assert(cultures != null);

            UnresolvedUri = unresolvedUri;
            HelpContentUriCollection = new Collection<UpdatableHelpUri>();
            UpdatableHelpItems = cultures;
        }

        /// <summary>
        /// Unresolved URI.
        /// </summary>
        internal string UnresolvedUri { get; }

        /// <summary>
        /// Link to the actual help content.
        /// </summary>
        internal Collection<UpdatableHelpUri> HelpContentUriCollection { get; }

        /// <summary>
        /// Supported UI cultures.
        /// </summary>
        internal CultureSpecificUpdatableHelp[] UpdatableHelpItems { get; }

        /// <summary>
        /// Checks if the other HelpInfo has a newer version.
        /// </summary>
        /// <param name="helpInfo">HelpInfo object to check.</param>
        /// <param name="culture">Culture to check.</param>
        /// <returns>True if the other HelpInfo is newer, false if not.</returns>
        internal bool IsNewerVersion(UpdatableHelpInfo helpInfo, CultureInfo culture)
        {
            Debug.Assert(helpInfo != null);

            Version v1 = helpInfo.GetCultureVersion(culture);
            Version v2 = GetCultureVersion(culture);

            Debug.Assert(v1 != null);

            if (v2 == null)
            {
                return true;
            }

            return v1 > v2; ;
        }

        /// <summary>
        /// Checks if a culture is supported.
        /// </summary>
        /// <param name="culture">Culture to check.</param>
        /// <returns>True if supported, false if not.</returns>
        internal bool IsCultureSupported(CultureInfo culture)
        {
            Debug.Assert(culture != null);

            foreach (CultureSpecificUpdatableHelp updatableHelpItem in UpdatableHelpItems)
            {
                if (string.Compare(updatableHelpItem.Culture.Name, culture.Name,
                    StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a string representation of the supported cultures.
        /// </summary>
        /// <returns>Supported cultures in string.</returns>
        internal string GetSupportedCultures()
        {
            if (UpdatableHelpItems.Length == 0)
            {
                return StringUtil.Format(HelpDisplayStrings.None);
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < UpdatableHelpItems.Length; i++)
            {
                sb.Append(UpdatableHelpItems[i].Culture.Name);

                if (i != (UpdatableHelpItems.Length - 1))
                {
                    sb.Append(" | ");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the culture version.
        /// </summary>
        /// <param name="culture">Culture info.</param>
        /// <returns>Culture version.</returns>
        internal Version GetCultureVersion(CultureInfo culture)
        {
            foreach (CultureSpecificUpdatableHelp updatableHelpItem in UpdatableHelpItems)
            {
                if (string.Compare(updatableHelpItem.Culture.Name, culture.Name,
                    StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return updatableHelpItem.Version;
                }
            }

            return null;
        }
    }
}
