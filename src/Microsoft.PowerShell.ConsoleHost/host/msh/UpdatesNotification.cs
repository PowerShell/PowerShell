// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// A Helper class for printing notification on PowerShell startup when there is a new update.
    /// </summary>
    internal static class UpdatesNotification
    {
        private const string UpdateCheckOptOutEnvVar = "POWERSHELL_UPDATECHECK_OPTOUT";
        private const string Last3ReleasesUri = "https://api.github.com/repos/PowerShell/PowerShell/releases?per_page=3";
        private const string LatestReleaseUri = "https://api.github.com/repos/PowerShell/PowerShell/releases/latest";

        private const string SentinelFileName = "_sentinel_";
        private const string DoneFileNameTemplate = "sentinel-{0}-{1}-{2}.done";
        private const string DoneFileNamePattern = "sentinel-*.done";
        private const string UpdateFileNameTemplate = "update_{0}_{1}";
        private const string UpdateFileNamePattern = "update_v*.*.*_????-??-??";

        private static readonly EnumerationOptions s_enumOptions = new EnumerationOptions();
        private static readonly string s_cacheDirectory = Path.Combine(Platform.CacheDirectory, PSVersionInfo.GitCommitId);

        /// <summary>
        /// Gets a value indicating whether update notification should be done.
        /// </summary>
        internal static readonly bool CanNotifyUpdates = !Utils.GetOptOutEnvVariableAsBool(UpdateCheckOptOutEnvVar, defaultValue: false);

        // Maybe we shouldn't do update check and show notification when it's from a mini-shell, meaning when
        // 'ConsoleShell.Start' is not called by 'ManagedEntrance.Start'.

        internal static void ShowUpdateNotification(PSHostUserInterface hostUI)
        {
            if (!Directory.Exists(s_cacheDirectory))
            {
                return;
            }

            if (TryParseUpdateFile(
                out bool fileFound,
                updateFilePath: out _,
                out SemanticVersion lastUpdateVersion,
                lastUpdateDate: out _) && fileFound)
            {
                string releaseTag = lastUpdateVersion.ToString();
                string notificationMsgTemplate = string.IsNullOrEmpty(lastUpdateVersion.PreReleaseLabel)
                    ? ManagedEntranceStrings.StableUpdateNotificationMessage
                    : ManagedEntranceStrings.PreviewUpdateNotificationMessage;

                string notificationMsg = string.Format(CultureInfo.CurrentCulture, notificationMsgTemplate, releaseTag);
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

            // If the host is not connect to a network, skip the rest of the check.
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return;
            }

            // Create the update cache directory if it hasn't exists
            if (!Directory.Exists(s_cacheDirectory))
            {
                Directory.CreateDirectory(s_cacheDirectory);
            }

            bool parseSuccess = TryParseUpdateFile(
                out bool fileFound,
                out string updateFilePath,
                out SemanticVersion lastUpdateVersion,
                out DateTime lastUpdateDate);

            DateTime today = DateTime.UtcNow;
            if (parseSuccess && fileFound && (today - lastUpdateDate).TotalDays < 7)
            {
                // There is an existing update file, and the last update was less than 1 week ago.
                // It's unlikely a new version is released within 1 week, so we can skip this check.
                return;
            }

            // Construct the sentinel file paths for today's check.
            string todayDoneFileName = string.Format(
                CultureInfo.InvariantCulture,
                DoneFileNameTemplate,
                today.Year.ToString(),
                today.Month.ToString(),
                today.Day.ToString());

            string sentinelFilePath = Path.Combine(s_cacheDirectory, SentinelFileName);
            string todayDoneFilePath = Path.Combine(s_cacheDirectory, todayDoneFileName);

            if (File.Exists(todayDoneFilePath))
            {
                // A successful update check has been done today.
                // We can skip this update check.
                return;
            }

            try
            {
                // Use 'sentinelFilePath' as the file lock.
                // The update-check tasks started by every 'pwsh' process of the same version will compete on holding this file.
                using (FileStream s = new FileStream(sentinelFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    if (File.Exists(todayDoneFilePath))
                    {
                        // After acquiring the file lock, it turns out a successful check has already been done for today.
                        // Then let's skip this update check.
                        return;
                    }

                    // Now it's guaranteed that this is the only process that reaches here.
                    // Clean up the old '.done' file, there should be only one of it.
                    foreach (string oldFile in Directory.EnumerateFiles(s_cacheDirectory, DoneFileNamePattern, s_enumOptions))
                    {
                        File.Delete(oldFile);
                    }

                    if (!parseSuccess)
                    {
                        // The update file is corrupted, either because more than one update files were found unexpectedly,
                        // or because the update file name is not in the valid format.
                        // This is **very unlikely** to happen unless the file is altered manually accidentally.
                        // We try to recover here by cleaning up all update files.
                        foreach (string file in Directory.EnumerateFiles(s_cacheDirectory, UpdateFileNamePattern, s_enumOptions))
                        {
                            File.Delete(file);
                        }
                    }

                    // Do the real update check:
                    //  - Send HTTP request to query for the new release/pre-release;
                    //  - If there is a valid new release that should be reported to the user,
                    //    create the file `update_<tag>_<publish-date>` when no `update` file exists,
                    //    or rename the existing file to `update_<new-version>_<new-publish-date>`.
                    SemanticVersion baselineVersion = lastUpdateVersion ?? PSVersionInfo.PSCurrentVersion;
                    Release release = await QueryNewReleaseAsync(baselineVersion);

                    if (release != null)
                    {
                        string newUpdateFileName = string.Format(
                            CultureInfo.InvariantCulture,
                            UpdateFileNameTemplate,
                            release.TagName,
                            release.PublishAt.Substring(0, 10));

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

        private static bool TryParseUpdateFile(
            out bool fileFound,
            out string updateFilePath,
            out SemanticVersion lastUpdateVersion,
            out DateTime lastUpdateDate)
        {
            fileFound = true;
            updateFilePath = null;
            lastUpdateVersion = null;
            lastUpdateDate = DateTime.MinValue;

            var files = Directory.EnumerateFiles(s_cacheDirectory, UpdateFileNamePattern, s_enumOptions);
            var enumerator = files.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                // No file was found that matches the pattern, but it's OK that an update file doesn't exist.
                // This could happen when there is no new updates yet.
                fileFound = false;
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

            // OK, only found one update file, which is expected.
            // Now let's parse the file name.
            string updateFileName = Path.GetFileName(updateFilePath);
            int dateStartIndex = updateFileName.LastIndexOf('_') + 1;

            bool success = DateTime.TryParse(
                updateFileName.AsSpan().Slice(dateStartIndex),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out lastUpdateDate);

            if (success)
            {
                int versionStartIndex = updateFileName.IndexOf('_') + 2;
                int versionLength = dateStartIndex - versionStartIndex - 1;
                string versionString = updateFileName.Substring(versionStartIndex, versionLength);
                success = SemanticVersion.TryParse(versionString, out lastUpdateVersion);
            }

            if (!success)
            {
                updateFilePath = null;
                lastUpdateVersion = null;
                lastUpdateDate = DateTime.MinValue;
            }

            return success;
        }

        private static async Task<Release> QueryNewReleaseAsync(SemanticVersion baselineVersion)
        {
            bool notPreRelease = string.IsNullOrEmpty(PSVersionInfo.PSCurrentVersion.PreReleaseLabel);
            string queryUri = notPreRelease ? LatestReleaseUri : Last3ReleasesUri;

            using (HttpClient client = new HttpClient())
            {
                string userAgent = string.Format(CultureInfo.InvariantCulture, "PowerShell {0}", PSVersionInfo.GitCommitId);
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.GetAsync(queryUri);
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                using (JsonReader jsonReader = new JsonTextReader(reader))
                {
                    Release releaseToReturn = null;
                    var settings = new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None };
                    var serializer = JsonSerializer.Create(settings);

                    if (notPreRelease)
                    {
                        var release = serializer.Deserialize<JObject>(jsonReader);
                        var tagName = release["tag_name"].ToString();
                        var version = SemanticVersion.Parse(tagName.Substring(1));

                        if (version > baselineVersion)
                        {
                            var publishAt = release["published_at"].ToString();
                            releaseToReturn = new Release(publishAt, tagName);
                        }
                    }
                    else
                    {
                        var last4Releases = serializer.Deserialize<JArray>(jsonReader);
                        var highestVersion = baselineVersion;

                        for (int i=0; i < last4Releases.Count; i++)
                        {
                            var release = last4Releases[i];
                            var tagName = release["tag_name"].ToString();
                            var version = SemanticVersion.Parse(tagName.Substring(1));

                            if (version > highestVersion)
                            {
                                highestVersion = version;
                                var publishAt = release["published_at"].ToString();
                                releaseToReturn = new Release(publishAt, tagName);
                            }
                        }
                    }

                    return releaseToReturn;
                }
            }
        }

        private class Release
        {
            internal Release(string publishAt, string tagName)
            {
                PublishAt = publishAt;
                TagName = tagName;
            }

            // The datetime stamp is in UTC: 2019-03-28T18:42:02Z
            internal string PublishAt { get; }
            internal string TagName { get; }
        }
    }
}
