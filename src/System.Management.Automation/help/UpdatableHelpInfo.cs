/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Management.Automation.Internal;
using System.Globalization;
using System.Diagnostics;
using System.Text;
using System.Collections.ObjectModel;

namespace System.Management.Automation.Help
{
    /// <summary>
    /// Represents each supported culture
    /// </summary>
    internal class CultureSpecificUpdatableHelp
    {
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="culture">culture info</param>
        /// <param name="version">version info</param>
        internal CultureSpecificUpdatableHelp(CultureInfo culture, Version version)
        {
            Debug.Assert(version != null);
            Debug.Assert(culture != null);

            _culture = culture;
            _version = version;
        }

        /// <summary>
        /// Culture version
        /// </summary>
        internal Version Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
            }
        }
        private Version _version;

        /// <summary>
        /// Supported culture
        /// </summary>
        internal CultureInfo Culture
        {
            get
            {
                return _culture;
            }
            set
            {
                _culture = value;
            }
        }
        private CultureInfo _culture;
    }

    /// <summary>
    /// This class represents the HelpInfo metadata XML
    /// </summary>
    internal class UpdatableHelpInfo
    {
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="unresolvedUri">unresolved help content URI</param>
        /// <param name="cultures">supported UI cultures</param>
        internal UpdatableHelpInfo(string unresolvedUri, CultureSpecificUpdatableHelp[] cultures)
        {
            Debug.Assert(cultures != null);

            _unresolvedUri = unresolvedUri;
            _helpContentUriCollection = new Collection<UpdatableHelpUri>();
            _updatableHelpItems = cultures;
        }

        /// <summary>
        /// Unresolved URI
        /// </summary>
        internal string UnresolvedUri
        {
            get
            {
                return _unresolvedUri;
            }
        }
        private string _unresolvedUri;

        /// <summary>
        /// Link to the actual help content
        /// </summary>
        internal Collection<UpdatableHelpUri> HelpContentUriCollection
        {
            get
            {
                return _helpContentUriCollection;
            }
        }
        private Collection<UpdatableHelpUri> _helpContentUriCollection;

        /// <summary>
        /// Supported UI cultures
        /// </summary>
        internal CultureSpecificUpdatableHelp[] UpdatableHelpItems
        {
            get
            {
                return _updatableHelpItems;
            }
        }
        private CultureSpecificUpdatableHelp[] _updatableHelpItems;

        /// <summary>
        /// Checks if the other HelpInfo has a newer version
        /// </summary>
        /// <param name="helpInfo">HelpInfo object to check</param>
        /// <param name="culture">culture to check</param>
        /// <returns>true if the other HelpInfo is newer, false if not</returns>
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
        /// Checks if a culture is supported
        /// </summary>
        /// <param name="culture">culture to check</param>
        /// <returns>true if supported, false if not</returns>
        internal bool IsCultureSupported(CultureInfo culture)
        {
            Debug.Assert(culture != null);

            foreach (CultureSpecificUpdatableHelp updatableHelpItem in _updatableHelpItems)
            {
                if (String.Compare(updatableHelpItem.Culture.Name, culture.Name,
                    StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a string representation of the supported cultures
        /// </summary>
        /// <returns>supported cultures in string</returns>
        internal string GetSupportedCultures()
        {
            if (_updatableHelpItems.Length == 0)
            {
                return StringUtil.Format(HelpDisplayStrings.None);
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _updatableHelpItems.Length; i++)
            {
                sb.Append(_updatableHelpItems[i].Culture.Name);

                if (i != (_updatableHelpItems.Length - 1))
                {
                    sb.Append(" | ");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the culture version
        /// </summary>
        /// <param name="culture">culture info</param>
        /// <returns>culture version</returns>
        internal Version GetCultureVersion(CultureInfo culture)
        {
            foreach (CultureSpecificUpdatableHelp updatableHelpItem in _updatableHelpItems)
            {
                if (String.Compare(updatableHelpItem.Culture.Name, culture.Name,
                    StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return updatableHelpItem.Version;
                }
            }

            return null;
        }
    }
}
