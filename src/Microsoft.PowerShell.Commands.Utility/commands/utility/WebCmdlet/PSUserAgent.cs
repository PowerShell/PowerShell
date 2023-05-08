// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

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
        private static string? s_windowsUserAgent;

        // Format the user-agent string from the various component parts
        internal static string UserAgent => string.Create(CultureInfo.InvariantCulture, $"{Compatibility} ({PlatformName}; {OS}; {Culture}) {App}");

        /// <summary>
        /// Useragent string for InternetExplorer (9.0).
        /// </summary>
        public static string InternetExplorer => string.Create(CultureInfo.InvariantCulture, $"{Compatibility} (compatible; MSIE 9.0; {PlatformName}; {OS}; {Culture})");

        /// <summary>
        /// Useragent string for Firefox (4.0).
        /// </summary>
        public static string FireFox => string.Create(CultureInfo.InvariantCulture, $"{Compatibility} ({PlatformName}; {OS}; {Culture}) Gecko/20100401 Firefox/4.0");

        /// <summary>
        /// Useragent string for Chrome (7.0).
        /// </summary>
        public static string Chrome => string.Create(CultureInfo.InvariantCulture, $"{Compatibility} ({PlatformName}; {OS}; {Culture}) AppleWebKit/534.6 (KHTML, like Gecko) Chrome/7.0.500.0 Safari/534.6");

        /// <summary>
        /// Useragent string for Opera (9.0).
        /// </summary>
        public static string Opera => string.Create(CultureInfo.InvariantCulture, $"Opera/9.70 ({PlatformName}; {OS}; {Culture}) Presto/2.2.1");

        /// <summary>
        /// Useragent string for Safari (5.0).
        /// </summary>
        public static string Safari => string.Create(CultureInfo.InvariantCulture, $"{Compatibility} ({PlatformName}; {OS}; {Culture}) AppleWebKit/533.16 (KHTML, like Gecko) Version/5.0 Safari/533.16");

        internal static string Compatibility => "Mozilla/5.0";

        internal static string App => string.Create(CultureInfo.InvariantCulture, $"PowerShell/{PSVersionInfo.PSVersion}");

        internal static string PlatformName
        {
            get
            {
                if (Platform.IsWindows)
                {
                    // Only generate the windows user agent once
                    if (s_windowsUserAgent is null)
                    {
                        // Find the version in the windows operating system description
                        Regex pattern = new(@"\d+(\.\d+)+");
                        string versionText = pattern.Match(OS).Value;
                        Version windowsPlatformversion = new(versionText);
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
                    // Unknown/unsupported platform
                    Diagnostics.Assert(false, "Unable to determine Operating System Platform");
                    return string.Empty;
                }
            }
        }

        internal static string OS => RuntimeInformation.OSDescription.Trim();

        internal static string Culture => CultureInfo.CurrentCulture.Name;
    }
}
