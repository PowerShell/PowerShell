// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Construct the Useragent string.
    /// </summary>
    public static class PSUserAgent
    {

        private static string s_windowsUserAgent;

        internal static string UserAgent
        {
            get
            {
                // format the user-agent string from the various component parts
                string userAgent = string.Format(CultureInfo.InvariantCulture,
                    "{0} ({1}; {2}; {3}) {4}",
                    Compatibility, PlatformName, OS, Culture, App);
                return (userAgent);
            }
        }

        /// <summary>
        /// Useragent string for InternetExplorer (9.0).
        /// </summary>
        public static string InternetExplorer
        {
            get
            {
                // format the user-agent string from the various component parts
                string userAgent = string.Format(CultureInfo.InvariantCulture,
                    "{0} (compatible; MSIE 9.0; {1}; {2}; {3})",
                    Compatibility, PlatformName, OS, Culture);
                return (userAgent);
            }
        }

        /// <summary>
        /// Useragent string for Firefox (4.0).
        /// </summary>
        public static string FireFox
        {
            get
            {
                // format the user-agent string from the various component parts
                string userAgent = string.Format(CultureInfo.InvariantCulture,
                    "{0} ({1}; {2}; {3}) Gecko/20100401 Firefox/4.0",
                    Compatibility, PlatformName, OS, Culture);
                return (userAgent);
            }
        }

        /// <summary>
        /// Useragent string for Chrome (7.0).
        /// </summary>
        public static string Chrome
        {
            get
            {
                // format the user-agent string from the various component parts
                string userAgent = string.Format(CultureInfo.InvariantCulture,
                    "{0} ({1}; {2}; {3}) AppleWebKit/534.6 (KHTML, like Gecko) Chrome/7.0.500.0 Safari/534.6",
                    Compatibility, PlatformName, OS, Culture);
                return (userAgent);
            }
        }

        /// <summary>
        /// Useragent string for Opera (9.0).
        /// </summary>
        public static string Opera
        {
            get
            {
                // format the user-agent string from the various component parts
                string userAgent = string.Format(CultureInfo.InvariantCulture,
                    "Opera/9.70 ({0}; {1}; {2}) Presto/2.2.1",
                    PlatformName, OS, Culture);
                return (userAgent);
            }
        }

        /// <summary>
        /// Useragent string for Safari (5.0).
        /// </summary>
        public static string Safari
        {
            get
            {
                // format the user-agent string from the various component parts
                string userAgent = string.Format(CultureInfo.InvariantCulture,
                    "{0} ({1}; {2}; {3}) AppleWebKit/533.16 (KHTML, like Gecko) Version/5.0 Safari/533.16",
                    Compatibility, PlatformName, OS, Culture);
                return (userAgent);
            }
        }

        internal static string Compatibility
        {
            get
            {
                return ("Mozilla/5.0");
            }
        }

        internal static string App
        {
            get
            {
                string app = string.Format(CultureInfo.InvariantCulture,
                    "PowerShell/{0}", PSVersionInfo.PSVersion);
                return (app);
            }
        }

        internal static string PlatformName
        {
            get
            {
                if (Platform.IsWindows)
                {
                    // only generate the windows user agent once
                    if (s_windowsUserAgent == null)
                    {
                        // find the version in the windows operating system description
                        Regex pattern = new Regex(@"\d+(\.\d+)+");
                        string versionText = pattern.Match(OS).Value;
                        Version windowsPlatformversion = new Version(versionText);
                        s_windowsUserAgent = $"Windows NT {windowsPlatformversion.Major}.{windowsPlatformversion.Minor}";
                    }

                    return s_windowsUserAgent;
                }
                else if (Platform.IsMacOS)
                {
                    return "Macintosh";
                }
                else if (Platform.IsLinux)
                {
                    return "Linux";
                }
                else
                {
                    // unknown/unsupported platform
                    Diagnostics.Assert(false, "Unable to determine Operating System Platform");
                    return string.Empty;
                }
            }
        }

        internal static string OS
        {
            get
            {
                return RuntimeInformation.OSDescription.Trim();
            }
        }

        internal static string Culture
        {
            get
            {
                return (CultureInfo.CurrentCulture.Name);
            }
        }
    }
}
