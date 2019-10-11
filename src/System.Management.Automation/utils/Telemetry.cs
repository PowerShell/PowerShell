// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.PowerShell.Telemetry
{
    /// <summary>
    /// The category of telemetry.
    /// </summary>
    internal enum TelemetryType
    {
        /// <summary>
        /// Telemetry of the application type (cmdlet, script, etc).
        /// </summary>
        ApplicationType,

        /// <summary>
        /// Send telemetry when we load a module, only module names in the s_knownModules list
        /// will be reported, otherwise it will be "anonymous".
        /// </summary>
        ModuleLoad,

        /// <summary>
        /// Send telemetry for experimental module feature activation.
        /// All experimental engine features will be have telemetry.
        /// </summary>
        ExperimentalEngineFeatureActivation,

        /// <summary>
        /// Send telemetry for experimental module feature activation.
        /// Experimental module features will send telemetry based on the module it is in.
        /// If we send telemetry for the module, we will also do so for any experimental feature
        /// in that module.
        /// </summary>
        ExperimentalModuleFeatureActivation,

        /// <summary>
        /// Send telemetry for each PowerShell.Create API.
        /// </summary>
        PowerShellCreate,

        /// <summary>
        /// Remote session creation.
        /// </summary>
        RemoteSessionOpen,
    }

    /// <summary>
    /// Send up telemetry for startup.
    /// </summary>
    public static class ApplicationInsightsTelemetry
    {
        // If this env var is true, yes, or 1, telemetry will NOT be sent.
        private const string _telemetryOptoutEnvVar = "POWERSHELL_TELEMETRY_OPTOUT";

        // PSCoreInsight2 telemetry key
        // private const string _psCoreTelemetryKey = "ee4b2115-d347-47b0-adb6-b19c2c763808"; // Production
        private const string _psCoreTelemetryKey = "d26a5ef4-d608-452c-a6b8-a4a55935f70d"; // V7 Preview 3

        // Use "anonymous" as the string to return when you can't report a name
        private const string _anonymous = "anonymous";

        // the telemetry failure string
        private const string _telemetryFailure = "TELEMETRY_FAILURE";

        // Telemetry client to be reused when we start sending more telemetry
        private static TelemetryClient s_telemetryClient { get; set; }

        // the unique identifier for the user, when we start we
        private static string s_uniqueUserIdentifier { get; set; }

        // the session identifier
        private static string s_sessionId { get; set; }

        /// Use a hashset for quick lookups.
        /// We send telemetry only a known set of modules.
        /// If it's not in the list (initialized in the static constructor), then we report anonymous.
        private static HashSet<string> s_knownModules;

        /// <summary>Gets a value indicating whether telemetry can be sent.</summary>
        public static bool CanSendTelemetry { get; private set; }

        /// <summary>
        /// Initializes static members of the <see cref="ApplicationInsightsTelemetry"/> class.
        /// Static constructor determines whether telemetry is to be sent, and then
        /// sets the telemetry key and set the telemetry delivery mode.
        /// Creates the session ID and initializes the HashSet of known module names.
        /// Gets or constructs the unique identifier.
        /// </summary>
        static ApplicationInsightsTelemetry()
        {
            // If we can't send telemetry, there's no reason to do any of this
            CanSendTelemetry = !Utils.GetOptOutEnvVariableAsBool(name: _telemetryOptoutEnvVar, defaultValue: false);
            if (CanSendTelemetry)
            {
                s_telemetryClient = new TelemetryClient();
                TelemetryConfiguration.Active.InstrumentationKey = _psCoreTelemetryKey;

                // Set this to true to reduce latency during development
                TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = false;
                s_sessionId = Guid.NewGuid().ToString();

                // use a hashset when looking for module names, it should be quicker than a string comparison
                s_knownModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "Microsoft.PowerShell.Archive",
                        "Microsoft.PowerShell.Host",
                        "Microsoft.PowerShell.Management",
                        "Microsoft.PowerShell.Security",
                        "Microsoft.PowerShell.Utility",
                        "PackageManagement",
                        "Pester",
                        "PowerShellGet",
                        "PSDesiredStateConfiguration",
                        "PSReadLine",
                        "ThreadJob",
                    };

                s_uniqueUserIdentifier = GetUniqueIdentifier().ToString();
            }
        }

        /// <summary>
        /// Send telemetry as a metric.
        /// </summary>
        /// <param name="metricId">The type of telemetry that we'll be sending.</param>
        /// <param name="data">The specific details about the telemetry.</param>
        internal static void SendTelemetryMetric(TelemetryType metricId, string data)
        {
            if (!CanSendTelemetry)
            {
                return;
            }

            string metricName = metricId.ToString();
            try
            {
                switch (metricId)
                {
                    case TelemetryType.ApplicationType:
                    case TelemetryType.PowerShellCreate:
                    case TelemetryType.RemoteSessionOpen:
                    case TelemetryType.ExperimentalEngineFeatureActivation:
                        s_telemetryClient.GetMetric(metricName, "uuid", "SessionId", "Detail").TrackValue(metricValue: 1.0, s_uniqueUserIdentifier, s_sessionId, data);
                        break;
                    case TelemetryType.ExperimentalModuleFeatureActivation:
                        string experimentalFeatureName = GetExperimentalFeatureName(data);
                        s_telemetryClient.GetMetric(metricName, "uuid", "SessionId", "Detail").TrackValue(metricValue: 1.0, s_uniqueUserIdentifier, s_sessionId, experimentalFeatureName);
                        break;
                    case TelemetryType.ModuleLoad:
                        string moduleName = GetModuleName(data); // This will return anonymous if the modulename is not on the report list
                        s_telemetryClient.GetMetric(metricName, "uuid", "SessionId", "Detail").TrackValue(metricValue: 1.0, s_uniqueUserIdentifier, s_sessionId, moduleName);
                        break;
                }
            }
            catch
            {
                // do nothing, telemetry can't be sent
                // don't send the panic telemetry as if we have failed above, it will likely fail here.
            }
        }

        // Get the experimental feature name. If we can report it, we'll return the name of the feature, otherwise, we'll return "anonymous"
        private static string GetExperimentalFeatureName(string featureNameToValidate)
        {
            // An experimental feature in a module is guaranteed to start with the module name
            // we can strip out the text past the last '.' as the text before that will be the ModuleName
            int lastDotIndex = featureNameToValidate.LastIndexOf('.');
            string moduleName = featureNameToValidate.Substring(0, lastDotIndex);
            if (s_knownModules.Contains(moduleName))
            {
                return featureNameToValidate;
            }

            return _anonymous;
        }

        // Get the module name. If we can report it, we'll return the name, otherwise, we'll return "anonymous"
        private static string GetModuleName(string moduleNameToValidate)
        {
            if (s_knownModules.Contains(moduleNameToValidate))
            {
                return moduleNameToValidate;
            }

            return _anonymous;
        }

        /// <summary>
        /// Create the startup payload and send it up.
        /// This is done only once during for the console host.
        /// </summary>
        /// <param name="mode">The "mode" of the startup.</param>
        internal static void SendPSCoreStartupTelemetry(string mode)
        {
            if (!CanSendTelemetry)
            {
                return;
            }

            var properties = new Dictionary<string, string>();

            // The variable POWERSHELL_DISTRIBUTION_CHANNEL is set in our docker images.
            // This allows us to track the actual docker OS as OSDescription provides only "linuxkit"
            // which has limited usefulness
            var channel = Environment.GetEnvironmentVariable("POWERSHELL_DISTRIBUTION_CHANNEL");

            properties.Add("SessionId", s_sessionId);
            properties.Add("UUID", s_uniqueUserIdentifier);
            properties.Add("GitCommitID", PSVersionInfo.GitCommitId);
            properties.Add("OSDescription", RuntimeInformation.OSDescription);
            properties.Add("OSChannel", string.IsNullOrEmpty(channel) ? "unknown" : channel);
            properties.Add("StartMode", string.IsNullOrEmpty(mode) ? "unknown" : mode);
            try
            {
                s_telemetryClient.TrackEvent("ConsoleHostStartup", properties, null);
            }
            catch
            {
                // do nothing, telemetry cannot be sent
            }
        }

        /// <summary>
        /// Try to read the file and collect the guid.
        /// </summary>
        /// <param name="telemetryFilePath">The path to the telemetry file.</param>
        /// <param name="id">The newly created id.</param>
        /// <returns>
        /// The method returns a bool indicating success or failure of creating the id.
        /// </returns>
        private static bool TryGetIdentifier(string telemetryFilePath, out Guid id)
        {
            if (File.Exists(telemetryFilePath))
            {
                // attempt to read the persisted identifier
                const int GuidSize = 16;
                byte[] buffer = new byte[GuidSize];
                try
                {
                    using (FileStream fs = new FileStream(telemetryFilePath, FileMode.Open, FileAccess.Read))
                    {
                        // if the read is invalid, or wrong size, we return it
                        int n = fs.Read(buffer, 0, GuidSize);
                        if (n == GuidSize)
                        {
                            // it's possible this could through
                            id = new Guid(buffer);
                            if (id != Guid.Empty)
                            {
                                return true;
                            }
                        }
                    }
                }
                catch
                {
                    // something went wrong, the file may not exist or not have enough bytes, so return false
                }
            }

            id = Guid.Empty;
            return false;
        }

        /// <summary>
        /// Try to create a unique identifier and persist it to the telemetry.uuid file.
        /// </summary>
        /// <param name="telemetryFilePath">The path to the persisted telemetry.uuid file.</param>
        /// <param name="id">The created identifier.</param>
        /// <returns>
        /// The method returns a bool indicating success or failure of creating the id.
        /// </returns>
        private static bool TryCreateUniqueIdentifierAndFile(string telemetryFilePath, out Guid id)
        {
            // one last attempt to retrieve before creating incase we have a lot of simultaneous entry into the mutex.
            id = Guid.Empty;
            if (TryGetIdentifier(telemetryFilePath, out id))
            {
                return true;
            }

            // The directory may not exist, so attempt to create it
            // CreateDirectory will simply return the directory if exists
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(telemetryFilePath));
            }
            catch
            {
                // send a telemetry indicating a problem with the cache dir
                // it's likely something is seriously wrong so we should at least report it.
                // We don't want to provide reasons here, that's not the point, but we
                // would like to know if we're having a generalized problem which we can trace statistically
                CanSendTelemetry = false;
                s_telemetryClient.GetMetric(_telemetryFailure, "Detail").TrackValue(1, "cachedir");
                return false;
            }

            // Create and save the new identifier, and if there's a problem, disable telemetry
            try
            {
                id = Guid.NewGuid();
                File.WriteAllBytes(telemetryFilePath, id.ToByteArray());
                return true;
            }
            catch
            {
                // another bit of telemetry to notify us about a problem with saving the unique id.
                s_telemetryClient.GetMetric(_telemetryFailure, "Detail").TrackValue(1, "saveuuid");
            }

            return false;
        }

        /// <summary>
        /// Retrieve the unique identifier from the persisted file, if it doesn't exist create it.
        /// Generate a guid which will be used as the UUID.
        /// </summary>
        /// <returns>A guid which represents the unique identifier.</returns>
        private static Guid GetUniqueIdentifier()
        {
            // Try to get the unique id. If this returns false, we'll
            // create/recreate the telemetry.uuid file to persist for next startup.
            Guid id = Guid.Empty;
            string uuidPath = Path.Join(Platform.CacheDirectory, "telemetry.uuid");
            if (TryGetIdentifier(uuidPath, out id))
            {
                return id;
            }

            // Multiple processes may start simultaneously so we need a system wide
            // way to control access to the file in the case (although remote) when we have
            // simulataneous shell starts without the persisted file which attempt to create the file.
            using (var m = new Mutex(true, "CreateUniqueUserId"))
            {
                // TryCreateUniqueIdentifierAndFile shouldn't throw, but the mutex might
                try
                {
                    m.WaitOne();
                    if (TryCreateUniqueIdentifierAndFile(uuidPath, out id))
                    {
                        return id;
                    }
                }
                catch (Exception)
                {
                    // Any problem in generating a uuid will result in no telemetry being sent.
                    // Try to send the failure in telemetry, but it will have no unique id.
                    s_telemetryClient.GetMetric(_telemetryFailure, "Detail").TrackValue(1, "mutex");
                }
                finally
                {
                    m.ReleaseMutex();
                }
            }

            // something bad happened, turn off telemetry since the unique id wasn't set.
            CanSendTelemetry = false;
            return id;
        }
    }
}
