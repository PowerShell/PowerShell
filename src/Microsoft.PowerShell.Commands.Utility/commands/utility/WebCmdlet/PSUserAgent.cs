/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Globalization;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Construct the Useragent string
    /// </summary>
    public static class PSUserAgent
    {
        internal static string UserAgent
        {
            get
            {
                // format the user-agent string from the various component parts
                string userAgent = string.Format(CultureInfo.InvariantCulture,
                    "{0} ({1}; {2}; {3}) {4}",
                    Compatibility, Platform, OS, Culture, App);
                return (userAgent);
            }
        }

        /// <summary>
        /// Useragent string for InternetExplorer (9.0)
        /// </summary>
        public static string InternetExplorer
        {
            get
            {
                // format the user-agent string from the various component parts
                string userAgent = string.Format(CultureInfo.InvariantCulture,
                    "{0} (compatible; MSIE 9.0; {1}; {2}; {3})",
                    Compatibility, Platform, OS, Culture);
                return (userAgent);
            }
        }

        /// <summary>
        /// Useragent string for Firefox (4.0)
        /// </summary>
        public static string FireFox
        {
            get
            {
                // format the user-agent string from the various component parts
                string userAgent = string.Format(CultureInfo.InvariantCulture,
                    "{0} ({1}; {2}; {3}) Gecko/20100401 Firefox/4.0",
                    Compatibility, Platform, OS, Culture);
                return (userAgent);
            }
        }

        /// <summary>
        /// Useragent string for Chrome (7.0)
        /// </summary>
        public static string Chrome
        {
            get
            {
                // format the user-agent string from the various component parts
                string userAgent = string.Format(CultureInfo.InvariantCulture,
                    "{0} ({1}; {2}; {3}) AppleWebKit/534.6 (KHTML, like Gecko) Chrome/7.0.500.0 Safari/534.6",
                    Compatibility, Platform, OS, Culture);
                return (userAgent);
            }
        }

        /// <summary>
        /// Useragent string for Opera (9.0)
        /// </summary>
        public static string Opera
        {
            get
            {
                // format the user-agent string from the various component parts
                string userAgent = string.Format(CultureInfo.InvariantCulture,
                    "Opera/9.70 ({0}; {1}; {2}) Presto/2.2.1",
                    Platform, OS, Culture);
                return (userAgent);
            }
        }

        /// <summary>
        /// Useragent string for Safari (5.0)
        /// </summary>
        public static string Safari
        {
            get
            {
                // format the user-agent string from the various component parts
                string userAgent = string.Format(CultureInfo.InvariantCulture,
                    "{0} ({1}; {2}; {3}) AppleWebKit/533.16 (KHTML, like Gecko) Version/5.0 Safari/533.16",
                    Compatibility, Platform, OS, Culture);
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
                    "WindowsPowerShell/{0}", PSVersionInfo.PSVersion);
                return (app);
            }
        }

        internal static string Platform
        {
            get
            {
                return ("Windows NT");
            }
        }

        internal static string OS
        {
            get
            {
#if CORECLR
                return System.Runtime.InteropServices.RuntimeInformation.OSDescription;
#else
                OperatingSystem os = Environment.OSVersion;
                string platform = GetOSName(os.Platform);
                string formattedOS = string.Format(CultureInfo.InvariantCulture,
                    "{0} {1}.{2}", platform, os.Version.Major, os.Version.Minor);
                return (formattedOS);
#endif
            }
        }

        internal static string Culture
        {
            get
            {
                return (CultureInfo.CurrentCulture.Name);
            }
        }

#if !CORECLR
        private static string GetOSName(PlatformID platformId)
        {
            string platform;
            switch (platformId)
            {
                case PlatformID.Win32NT: // is this really the only valid option?
                    platform = "Windows NT";
                    break;
                case PlatformID.Win32Windows:
                    platform = "Windows";
                    break;
                case PlatformID.Win32S:
                case PlatformID.WinCE:
                case PlatformID.Xbox:
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                default:
                    // TODO: should we be doing something more robust here?
                    platform = platformId.ToString();
                    break;
            }

            return (platform);
        }
#endif
    }
}
