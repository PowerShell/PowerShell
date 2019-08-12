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
        /// telemetry of the application type (cmdlet, script, etc).
        /// </summary>
        ApplicationType,
        /// <summary>
        /// Send telemetry when we load a module, only module names in s_knownModuleList list
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

        // the telemetry failure string
        private const string _telemetryFailure = "TELEMETRY_FAILURE";

        // Telemetry client to be reused when we start sending more telemetry
        private static TelemetryClient s_telemetryClient { get; set; }

        // Set this to true to reduce the latency of sending the telemetry
        private static bool s_developerMode { get; set; }

        // the unique identifier for the user, when we start we
        private static Guid s_uniqueUserIdentifier { get; set; }

        // the session identifier
        private static Guid s_sessionId {get; set; }

        ///<summary>
        /// We send telemetry only a known set of modules.
        /// If it's not in this list, then we report anonymous.
        /// </summary>
        private static string[] s_knownModuleNames = new[] {
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

        // use a hashset for quicker lookups.
        private static HashSet<string> s_knownModules;

        /// <summary>Can telemetry be sent.</summary>
        public static bool CanSendTelemetry { get; private set; }

        /// <summary>
        /// Static constructor determines whether telemetry is to be sent, and then
        /// sets the telemetry key and set the telemetry delivery mode.
        /// Creates the session ID and
        /// initializes the HashSet of known module names.
        /// Gets or constructs the unique identifier.
        /// </summary>
        static ApplicationInsightsTelemetry()
        {
            CanSendTelemetry = ! GetEnvironmentVariableAsBool(name: _telemetryOptoutEnvVar, defaultValue: false);
            // If we can't send telemetry, there's no reason to do any of this
            if ( CanSendTelemetry ) {
                s_telemetryClient = new TelemetryClient();
                TelemetryConfiguration.Active.InstrumentationKey = _psCoreTelemetryKey;
                TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = s_developerMode;
                s_sessionId = Guid.NewGuid();
                // use a hashset when looking for module names, it should be quicker than a string comparison
                s_knownModules = new HashSet<string>(s_knownModuleNames, StringComparer.OrdinalIgnoreCase);
                s_uniqueUserIdentifier = GetUniqueIdentifier();
                s_developerMode = true; // false for production
            }
        }

        /// <summary>
        /// Determine whether the environment variable is set and how.
        /// </summary>
        private static bool GetEnvironmentVariableAsBool(string name, bool defaultValue)
        {
            var str = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(str))
            {
                return defaultValue;
            }

            switch (str.ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                    return true;
                case "false":
                case "0":
                case "no":
                    return false;
                default:
                    return defaultValue;
            }
        }

        /// <summary>
        /// Send telemetry as a metric.
        /// </summary>
        /// <parameter name="metricId">The type of telemetry that we'll be sending</parameter>
        /// <parameter name="data">The specific details about the telemetry</parameter>
        internal static void SendTelemetryMetric(TelemetryType metricId, string data)
        {

            if (! CanSendTelemetry)
            {
                return;
            }

            string metricName = metricId.ToString();
            string uuidString = s_uniqueUserIdentifier.ToString();
            string sessionIdString  = s_sessionId.ToString();
            try {
                switch (metricId)
                {
                    case TelemetryType.ApplicationType:
                    case TelemetryType.PowerShellCreate:
                    case TelemetryType.RemoteSessionOpen:
                    case TelemetryType.ExperimentalEngineFeatureActivation:
                        s_telemetryClient.GetMetric(metricName, "uuid", "SessionId", "Detail").TrackValue(1, uuidString, sessionIdString, data);
                        break;
                    case TelemetryType.ExperimentalModuleFeatureActivation:
                        string experimentalFeatureName = GetExperimentalFeatureName(data);
                        s_telemetryClient.GetMetric(metricName, "uuid", "SessionId", "Detail").TrackValue(1, uuidString, sessionIdString, experimentalFeatureName);
                        break;
                    case TelemetryType.ModuleLoad:
                        string moduleName = GetModuleName(data); // This will return anonymous if the modulename is not on the report list
                        s_telemetryClient.GetMetric(metricName, "uuid", "SessionId", "Detail").TrackValue(1, uuidString, sessionIdString, moduleName);
                        break;
                }
            }
            catch {
                // do nothing, telemetry can't be sent
                // don't send the panic telemetry as if we have failed above, it will likely fail here.
            }
        }

        // Get the experimental feature name. If we can report it, we'll return the name of the feature, otherwise, we'll return "anonymous"
        private static string GetExperimentalFeatureName(string featureNameToValidate)
        {
            foreach (string knownModuleName in s_knownModuleNames)
            {
                // An experimental feature in a module is guaranteed to start with the module name
                // but we can't look up the experimental feature in the hash because there will be more than just the module name.
                // we can't use a hashset because it's a partial comparison.
                if (featureNameToValidate.StartsWith(knownModuleName, StringComparison.OrdinalIgnoreCase))
                {
                    return featureNameToValidate;
                }
            }
            return "anonymous";
        }

        // Get the module name. If we can report it, we'll return the name, otherwise, we'll return "anonymous"
        private static string GetModuleName(string moduleNameToValidate)
        {
            if (s_knownModules.TryGetValue(moduleNameToValidate, out string valOut)) {
                return moduleNameToValidate;
            }
            else {
                return "anonymous";
            }
        }

        /// <summary>
        /// Create the startup payload and send it up.
        /// This is done only once during for the console host.
        /// </summary>
        /// <param name="mode">The "mode" of the startup.</param>
        internal static void SendPSCoreStartupTelemetry(string mode)
        {
            if ( ! CanSendTelemetry ) {
                return;
            }
            var properties = new Dictionary<string, string>();
            var channel = Environment.GetEnvironmentVariable("POWERSHELL_DISTRIBUTION_CHANNEL");

            properties.Add("SessionId", s_sessionId.ToString());
            properties.Add("UUID", s_uniqueUserIdentifier.ToString());
            properties.Add("GitCommitID", PSVersionInfo.GitCommitId);
            properties.Add("OSDescription", RuntimeInformation.OSDescription);
            properties.Add("OSChannel", (channel == null || channel == string.Empty) ? "unknown" : channel);
            properties.Add("StartMode", (mode == null || mode == string.Empty) ? "unknown" : mode);
            try {
                s_telemetryClient.TrackEvent("ConsoleHostStartup", properties, null);
            }
            catch {
                // do nothing, telemetry cannot be sent
            }
        }

        /// <summary>
        /// Try to read the file and collect the guid.
        /// </summary>
        private static bool TryGetIdentifier(string telemetryFilePath, out Guid id)
        {
            if ( File.Exists(telemetryFilePath))
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
                        if ( n == GuidSize )
                        {
                            // it's possible this could through
                            id = new Guid(buffer);
                            if ( id != Guid.Empty ) {
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
        private static bool TryCreateUniqueIdentifierAndFile(string telemetryFilePath, out Guid id)
        {
            id = Guid.Empty;
            // one last attempt to retrieve before creating incase we have a lot of simultaneous entry into the mutex.
            if ( TryGetIdentifier(telemetryFilePath, out id) ) {
                return true;
            }

            // The directory may not exist, so attempt to create it
            // CreateDirectory will simply return the directory if exists
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(telemetryFilePath));
            }
            catch {
                CanSendTelemetry = false;
                // send a telemetry indicating a problem with the cache dir
                // it's likely something is seriously wrong so we should at least report it.
                // We don't want to provide reasons here, that's not the point, but we
                // would like to know if we're having a generalized problem which we can trace statistically
                s_telemetryClient.GetMetric(_telemetryFailure, "Detail").TrackValue(1, "cachedir");
                return false;
            }

            // Create and save the new identifier, and if there's a problem, disable telemetry
            try {
                id = Guid.NewGuid();
                File.WriteAllBytes(telemetryFilePath, id.ToByteArray());
                return true;
            }
            catch {
                // another bit of telemetry to notify us about a problem with saving the unique id.
                s_telemetryClient.GetMetric(_telemetryFailure, "Detail").TrackValue(1, "saveuuid");
            }

            return false;
        }

        /// <summary>
        /// Retrieve the unique identifier from the persisted file, if it doesn't exist create it.
        /// Generate a guid which will be used as the UUID.
        /// </summary>
        private static Guid GetUniqueIdentifier()
        {
            Guid id = Guid.Empty;
            string uuidPath = Path.Join(Platform.CacheDirectory, "telemetry.uuid");
            // Try to get the unique id. If this returns false, we'll 
            // create/recreate the telemetry.uuid file to persist for next startup.
            if ( TryGetIdentifier(uuidPath, out id) ) {
                return id;
            }

            // Multiple processes may start simultaneously so we need a system wide 
            // way to control access to the file in the case (although remote) when we have
            // simulataneous shell starts without the persisted file which attempt to create the file.
            using ( var m = new Mutex(true, "CreateUniqueUserId") ) {
                // TryCreateUniqueIdentifierAndFile shouldn't throw, but the mutex might
                try {
                    m.WaitOne();
                    if ( TryCreateUniqueIdentifierAndFile(uuidPath, out id) ) {
                        return id;
                    }
                }
                catch (Exception) {
                    // Any problem in generating a uuid will result in no telemetry being sent.
                    // Try to send the failure in telemetry, but it will have no unique id.
                    s_telemetryClient.GetMetric(_telemetryFailure, "Detail").TrackValue(1, "mutex");
                }
                finally {
                    m.ReleaseMutex();
                }
            }
            // something bad happened, turn off telemetry since the unique id wasn't set.
            CanSendTelemetry = false;
            return id;
        }
    }
}
