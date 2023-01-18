// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// A Helper class for printing notification on PowerShell startup when there is a new update.
    /// </summary>
    /// <remarks>
    /// For the detailed design, please take a look at the corresponding RFC.
    /// </remarks>
    internal static class UpdatesNotification
    {
        private const string UpdateCheckEnvVar = "POWERSHELL_UPDATECHECK";
        private const string LTSBuildInfoURL = "https://aka.ms/pwsh-buildinfo-lts";
        private const string StableBuildInfoURL = "https://aka.ms/pwsh-buildinfo-stable";
        private const string PreviewBuildInfoURL = "https://aka.ms/pwsh-buildinfo-preview";

        /// <summary>
        /// The version of new update is persisted using a file, not as the file content, but instead baked in the file name in the following template:
        ///  `update{notification-type}_{version}_{publish-date}` -- held by 's_updateFileNameTemplate',
        /// while 's_updateFileNamePattern' holds the pattern of this file name.
        /// </summary>
        private static readonly string s_updateFileNameTemplate, s_updateFileNamePattern;

        /// <summary>
        /// For each notification type, we need two files to achieve the synchronization for the update check:
        ///  `_sentinel{notification-type}_` -- held by 's_sentinelFileName';
        ///  `sentinel{notification-type}-{year}-{month}-{day}.done`
        ///     -- held by 's_doneFileNameTemplate', while 's_doneFileNamePattern' holds the pattern of this file name.
        /// The {notification-type} part will be the integer value of the corresponding `NotificationType` member.
        /// The {year}-{month}-{day} part will be filled with the date of current day when the update check runs.
        /// </summary>
        private static readonly string s_sentinelFileName, s_doneFileNameTemplate, s_doneFileNamePattern;

        private static readonly string s_cacheDirectory;
        private static readonly EnumerationOptions s_enumOptions;
        private static readonly NotificationType s_notificationType;

        /// <summary>
        /// Gets a value indicating whether update notification should be done.
        /// </summary>
        internal static readonly bool CanNotifyUpdates;

        static UpdatesNotification()
        {
            s_notificationType = GetNotificationType();
            CanNotifyUpdates = s_notificationType != NotificationType.Off;

            if (CanNotifyUpdates)
            {
                s_enumOptions = new EnumerationOptions();
                s_cacheDirectory = Path.Combine(Platform.CacheDirectory, PSVersionInfo.GitCommitId);

                // Build the template/pattern strings for the configured notification type.
                string typeNum = ((int)s_notificationType).ToString();
                s_sentinelFileName = $"_sentinel{typeNum}_";
                s_doneFileNameTemplate = $"sentinel{typeNum}-{{0}}-{{1}}-{{2}}.done";
                s_doneFileNamePattern = $"sentinel{typeNum}-*.done";
                s_updateFileNameTemplate = $"update{typeNum}_{{0}}_{{1}}";
                s_updateFileNamePattern = $"update{typeNum}_v*.*.*_????-??-??";
            }
        }

        // Maybe we shouldn't do update check and show notification when it's from a mini-shell, meaning when
        // 'ConsoleShell.Start' is not called by 'ManagedEntrance.Start'.
        // But it seems so unusual that it's probably not worth bothering. Also, a mini-shell probably should
        // just disable the update notification feature by setting the opt-out environment variable.

        internal static void ShowUpdateNotification(PSHostUserInterface hostUI)
        {
            if (!Directory.Exists(s_cacheDirectory))
            {
                return;
            }

            if (TryParseUpdateFile(
                    updateFilePath: out _,
                    out SemanticVersion lastUpdateVersion,
                    lastUpdateDate: out _)
               && lastUpdateVersion != null)
            {
                string releaseTag = lastUpdateVersion.ToString();
                string notificationMsgTemplate = s_notificationType == NotificationType.LTS
                    ? ManagedEntranceStrings.LTSUpdateNotificationMessage
                    : string.IsNullOrEmpty(lastUpdateVersion.PreReleaseLabel)
                        ? ManagedEntranceStrings.StableUpdateNotificationMessage
                        : ManagedEntranceStrings.PreviewUpdateNotificationMessage;

                string notificationColor = string.Empty;
                string resetColor = string.Empty;

                string line2Padding = string.Empty;
                string line3Padding = string.Empty;

                // We calculate how much whitespace we need to make it look nice
                if (hostUI.SupportsVirtualTerminal)
                {
                    // Swaps foreground and background colors.
                    notificationColor = "\x1B[7m";
                    resetColor = "\x1B[0m";

                    // The first line is longest, if the message changes, this needs to be updated
                    int line1Length = notificationMsgTemplate.IndexOf('\n');
                    int line2Length = notificationMsgTemplate.IndexOf('\n', line1Length + 1);
                    int line3Length = notificationMsgTemplate.IndexOf('\n', line2Length + 1);
                    line3Length -= line2Length + 1;
                    line2Length -= line1Length + 1;

                    line2Padding = line2Padding.PadRight(line1Length - line2Length + releaseTag.Length);
                    // 3 represents the extra placeholder in the template
                    line3Padding = line3Padding.PadRight(line1Length - line3Length + 3);
                }

                string notificationMsg = string.Format(CultureInfo.CurrentCulture, notificationMsgTemplate, releaseTag, notificationColor, resetColor, line2Padding, line3Padding);

                hostUI.WriteLine();
                hostUI.WriteLine(notificationMsg);
            }
        }

        internal static async Task CheckForUpdates()
        {
            // Delay the update check for 3 seconds so that it has the minimal impact on startup.
            await Task.Delay(3000);

            // A self-built pwsh for development purpose has the SHA1 commit hash baked in 'GitCommitId',
            // which is 40 characters long. So we can quickly check the length of 'GitCommitId' to tell
            // if this is a self-built pwsh, and skip the update check if so.
            if (PSVersionInfo.GitCommitId.Length > 40)
            {
                return;
            }

            // Daily builds do not support update notifications
            string preReleaseLabel = PSVersionInfo.PSCurrentVersion.PreReleaseLabel;
            if (preReleaseLabel != null && preReleaseLabel.StartsWith("daily", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // If the host is not connect to a network, skip the rest of the check.
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return;
            }

            // Create the update cache directory if it doesn't exists
            if (!Directory.Exists(s_cacheDirectory))
            {
                Directory.CreateDirectory(s_cacheDirectory);
            }

            bool parseSuccess = TryParseUpdateFile(
                out string updateFilePath,
                out SemanticVersion lastUpdateVersion,
                out DateTime lastUpdateDate);

            DateTime today = DateTime.UtcNow;
            if (parseSuccess && updateFilePath != null && (today - lastUpdateDate).TotalDays < 7)
            {
                // There is an existing update file, and the last update was less than 1 week ago.
                // It's unlikely a new version is released within 1 week, so we can skip this check.
                return;
            }

            // Construct the sentinel file paths for today's check.
            string todayDoneFileName = string.Format(
                CultureInfo.InvariantCulture,
                s_doneFileNameTemplate,
                today.Year.ToString(),
                today.Month.ToString(),
                today.Day.ToString());

            string todayDoneFilePath = Path.Combine(s_cacheDirectory, todayDoneFileName);
            if (File.Exists(todayDoneFilePath))
            {
                // A successful update check has been done today.
                // We can skip this update check.
                return;
            }

            try
            {
                // Use 's_sentinelFileName' as the file lock.
                // The update-check tasks started by every 'pwsh' process of the same version will compete on holding this file.
                string sentinelFilePath = Path.Combine(s_cacheDirectory, s_sentinelFileName);
                using (new FileStream(sentinelFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.DeleteOnClose))
                {
                    if (File.Exists(todayDoneFilePath))
                    {
                        // After acquiring the file lock, it turns out a successful check has already been done for today.
                        // Then let's skip this update check.
                        return;
                    }

                    // Now it's guaranteed that this is the only process that reaches here.
                    // Clean up the old '.done' file, there should be only one of it.
                    foreach (string oldFile in Directory.EnumerateFiles(s_cacheDirectory, s_doneFileNamePattern, s_enumOptions))
                    {
                        File.Delete(oldFile);
                    }

                    if (!parseSuccess)
                    {
                        // The update file is corrupted, either because more than one update files were found unexpectedly,
                        // or because the update file name failed to be parsed into a release version and a publish date.
                        // This is **very unlikely** to happen unless the file is accidentally altered manually.
                        // We try to recover here by cleaning up all update files for the configured notification type.
                        foreach (string file in Directory.EnumerateFiles(s_cacheDirectory, s_updateFileNamePattern, s_enumOptions))
                        {
                            File.Delete(file);
                        }
                    }

                    // Do the real update check:
                    //  - Send HTTP request to query for the new release/pre-release;
                    //  - If there is a valid new release that should be reported to the user,
                    //    create the file `update<NotificationType>_<tag>_<publish-date>` when no `update` file exists,
                    //    or rename the existing file to `update<NotificationType>_<new-version>_<new-publish-date>`.
                    SemanticVersion baselineVersion = lastUpdateVersion ?? PSVersionInfo.PSCurrentVersion;
                    Release release = await QueryNewReleaseAsync(baselineVersion);

                    if (release != null)
                    {
                        // The date part of the string is 'YYYY-MM-DD'.
                        const int dateLength = 10;
                        string newUpdateFileName = string.Format(
                            CultureInfo.InvariantCulture,
                            s_updateFileNameTemplate,
                            release.TagName,
                            release.PublishAt.Substring(0, dateLength));

                        string newUpdateFilePath = Path.Combine(s_cacheDirectory, newUpdateFileName);

                        if (updateFilePath == null)
                        {
                            new FileStream(newUpdateFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None).Close();
                        }
                        else
                        {
                            File.Move(updateFilePath, newUpdateFilePath);
                        }
                    }

                    // Finally, create the `todayDoneFilePath` file as an indicator that a successful update check has finished today.
                    new FileStream(todayDoneFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None).Close();
                }
            }
            catch (Exception)
            {
                // There are 2 possible reason for the exception:
                // 1. An update check initiated from another `pwsh` process is in progress.
                //    It's OK to just return and let that update check to finish the work.
                // 2. The update check failed (ex. internet connectivity issue, GitHub service failure).
                //    It's OK to just return and let another `pwsh` do the check at later time.
            }
        }

        /// <summary>
        /// Check for the existence of the update file and parse the file name if it exists.
        /// </summary>
        /// <param name="updateFilePath">Get the exact update file path.</param>
        /// <param name="lastUpdateVersion">Get the version of the new release.</param>
        /// <param name="lastUpdateDate">Get the publish date of the new release.</param>
        /// <returns>
        /// False, when
        ///   1. found more than one update files that matched the pattern; OR
        ///   2. found only one update file, but failed to parse its name for version and publish date.
        /// True, when
        ///   1. no update file was found, namely no new updates yet;
        ///   2. found only one update file, and succeeded to parse its name for version and publish date.
        /// </returns>
        private static bool TryParseUpdateFile(
            out string updateFilePath,
            out SemanticVersion lastUpdateVersion,
            out DateTime lastUpdateDate)
        {
            updateFilePath = null;
            lastUpdateVersion = null;
            lastUpdateDate = default;

            var files = Directory.EnumerateFiles(s_cacheDirectory, s_updateFileNamePattern, s_enumOptions);
            var enumerator = files.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                // It's OK that an update file doesn't exist. This could happen when there is no new updates yet.
                return true;
            }

            updateFilePath = enumerator.Current;
            if (enumerator.MoveNext())
            {
                // More than 1 files were found that match the pattern. This is a corrupted state.
                // Theoretically, there should be only one update file at any point of time.
                updateFilePath = null;
                return false;
            }

            // OK, only found one update file for the configured notification type, which is expected.
            // Now let's parse the file name.
            string updateFileName = Path.GetFileName(updateFilePath);
            int dateStartIndex = updateFileName.LastIndexOf('_') + 1;

            if (!DateTime.TryParse(
                    updateFileName.AsSpan(dateStartIndex),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out lastUpdateDate))
            {
                updateFilePath = null;
                return false;
            }

            int versionStartIndex = updateFileName.IndexOf('_') + 2;
            int versionLength = dateStartIndex - versionStartIndex - 1;
            string versionString = updateFileName.Substring(versionStartIndex, versionLength);

            if (SemanticVersion.TryParse(versionString, out lastUpdateVersion))
            {
                return true;
            }

            updateFilePath = null;
            lastUpdateDate = default;
            return false;
        }

        private static async Task<Release> QueryNewReleaseAsync(SemanticVersion baselineVersion)
        {
            bool isStableRelease = string.IsNullOrEmpty(PSVersionInfo.PSCurrentVersion.PreReleaseLabel);
            string[] queryUris = s_notificationType switch
            {
                NotificationType.LTS => new[] { LTSBuildInfoURL },
                NotificationType.Default => isStableRelease
                    ? new[] { StableBuildInfoURL }
                    : new[] { StableBuildInfoURL, PreviewBuildInfoURL },
                _ => Array.Empty<string>()
            };

            using var client = new HttpClient();

            string userAgent = string.Format(CultureInfo.InvariantCulture, $"PowerShell {PSVersionInfo.GitCommitId}");
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Release releaseToReturn = null;
            SemanticVersion highestVersion = baselineVersion;
            var settings = new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None };
            var serializer = JsonSerializer.Create(settings);

            foreach (string queryUri in queryUris)
            {
                // Query the GitHub Rest API and throw if the query fails.
                HttpResponseMessage response = await client.GetAsync(queryUri);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                using var jsonReader = new JsonTextReader(reader);

                JObject release = serializer.Deserialize<JObject>(jsonReader);
                var tagName = release["ReleaseTag"].ToString();
                var version = SemanticVersion.Parse(tagName.Substring(1));

                if (version > highestVersion)
                {
                    highestVersion = version;
                    var publishAt = release["ReleaseDate"].ToString();
                    releaseToReturn = new Release(publishAt, tagName);
                }
            }

            return releaseToReturn;
        }

        /// <summary>
        /// Get the notification type setting.
        /// </summary>
        private static NotificationType GetNotificationType()
        {
            string str = Environment.GetEnvironmentVariable(UpdateCheckEnvVar);
            if (string.IsNullOrEmpty(str))
            {
                return NotificationType.Default;
            }

            if (Enum.TryParse(str, ignoreCase: true, out NotificationType type))
            {
                return type;
            }

            return NotificationType.Default;
        }

        /// <summary>
        /// Notification type that can be configured.
        /// </summary>
        private enum NotificationType
        {
            /// <summary>
            /// Turn off the update notification.
            /// </summary>
            Off = 0,

            /// <summary>
            /// Give you the default behaviors:
            ///  - the preview version 'pwsh' checks for the new preview version and the new GA version.
            ///  - the GA version 'pwsh' checks for the new GA version only.
            /// </summary>
            Default = 1,

            /// <summary>
            /// Both preview and GA version 'pwsh' checks for the new LTS version only.
            /// </summary>
            LTS = 2
        }

        private sealed class Release
        {
            internal Release(string publishAt, string tagName)
            {
                PublishAt = publishAt;
                TagName = tagName;
            }

            /// <summary>
            /// The datetime stamp is in UTC. For example: 2019-03-28T18:42:02Z.
            /// </summary>
            internal string PublishAt { get; }

            /// <summary>
            /// The release tag name.
            /// </summary>
            internal string TagName { get; }
        }
    }
}
