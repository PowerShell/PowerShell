// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Internal;
using System.Text;

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

        /// <summary>
        /// Enumerates fallback chain (parents) of the culture, including itself.
        /// </summary>
        /// <param name="culture">Culture to enumerate</param>
        /// <example>
        /// Examples:
        /// en-GB => { en-GB, en }
        /// zh-Hans-CN => { zh-Hans-CN, zh-Hans, zh }.
        /// </example>
        /// <returns>An enumerable list of culture names.</returns>
        internal static IEnumerable<string> GetCultureFallbackChain(CultureInfo culture)
        {
            // We use just names instead because comparing two CultureInfo objects
            // can fail if they are created using different means
            while (culture != null)
            {
                if (string.IsNullOrEmpty(culture.Name))
                {
                    yield break;
                }

                yield return culture.Name;

                culture = culture.Parent;
            }
        }

        /// <summary>
        /// Checks if a culture is supported.
        /// </summary>
        /// <param name="cultureName">Name of the culture to check.</param>
        /// <returns>True if supported, false if not.</returns>
        internal bool IsCultureSupported(string cultureName)
        {
            Debug.Assert(cultureName != null, $"{nameof(cultureName)} may not be null");
            return GetCultureFallbackChain(Culture).Any(fallback => fallback == cultureName);
        }
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

            return v1 > v2;
        }

        /// <summary>
        /// Checks if a culture is supported.
        /// </summary>
        /// <param name="cultureName">Name of the culture to check.</param>
        /// <returns>True if supported, false if not.</returns>
        internal bool IsCultureSupported(string cultureName)
        {
            Debug.Assert(cultureName != null, $"{nameof(cultureName)} may not be null");
            return UpdatableHelpItems.Any(item => item.IsCultureSupported(cultureName));
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
                if (string.Equals(updatableHelpItem.Culture.Name, culture.Name,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return updatableHelpItem.Version;
                }
            }

            return null;
        }
    }
}
