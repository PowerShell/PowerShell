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
    /// The type of tracking for our telemetry
    /// </summary>
    public enum AITelemetryType
    {
        /// <summary>
        /// Track the application type (cmdlet, script, etc)
        /// </summary>
        ApplicationType,
        /// <summary>
        /// Track when we load a module, only module names in the allow list
        /// will be reported, otherwise it will be "anonymous"
        /// </summary>
        ModuleLoad,
        /// <summary>
        /// Track experimental feature activation
        /// All experimental features will be tracked
        /// </summary>
        ExperimentalFeatureActivation,
        /// <summary>
        /// Track each PowerShell.Create API
        /// </summary>
        PowerShellCreate,
        /// <summary>
        /// Track remote session creation
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

        // Telemetry client to be reused when we start sending more telemetry
        private static TelemetryClient _telemetryClient = new TelemetryClient();

        // Set this to true to reduce the latency of sending the telemetry
        private static bool _developerMode = true;

        // PSCoreInsight2 telemetry key
        // private const string _psCoreTelemetryKey = "ee4b2115-d347-47b0-adb6-b19c2c763808"; // Production
        private const string _psCoreTelemetryKey = "d26a5ef4-d608-452c-a6b8-a4a55935f70d"; // V7 Preview 3

        // the unique identifier for the user, when we start we
        private static Guid uniqueUserIdentifier = Guid.Empty;

        // the session identifier
        private static Guid _sessionId;

        // the telemetry failure string
        private const string _telemetryFailure = "TELEMETRY_FAILURE";

        ///<summary>
        /// We send telemetry only a known set of modules.
        /// If it's not in this list, then we report anonymous
        /// </summary>
        private static string[] _knownModuleNames = new string[] {
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

        /// <summary>Can we send telemetry</summary>
        public static bool CanSendTelemetry;

        // use a hashset for quicker lookups.
        private static HashSet<string> _knownModules;

        /// <summary>
        /// Static constructor determines whether telemetry is to be sent, and then
        /// sets the telemetry key and set the telemetry delivery mode
        /// Creates the session ID
        /// Initializes the HashSet of known module names
        /// Retrieves or constructs the unique identifier
        /// </summary>
        static ApplicationInsightsTelemetry()
        {
            CanSendTelemetry = ! GetEnvironmentVariableAsBool(name: _telemetryOptoutEnvVar, defaultValue: false);
            // If we can't send telemetry, there's no reason to do any of this
            if ( CanSendTelemetry ) {
                TelemetryConfiguration.Active.InstrumentationKey = _psCoreTelemetryKey;
                TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = _developerMode;
                _sessionId = Guid.NewGuid();
                // use a hashset when looking for module names, it should be quicker than a string comparison
                _knownModules = new HashSet<string>(_knownModuleNames, StringComparer.OrdinalIgnoreCase);
                uniqueUserIdentifier = getUniqueIdentifier();
            }
        }

        /// <summary>
        /// determine whether the environment variable is set and how
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
        /// Send telemetry as a metric
        /// </summary>
        /// <parameter name="metricId">The type of telemetry that we'll be sending</parameter>
        /// <parameter name="data">The specific details about the telemetry</parameter>
        public static void SendTelemetryMetric(AITelemetryType metricId, string data)
        {

            if (! CanSendTelemetry)
            {
                return;
            }

            string metricName = Enum.GetName(typeof(AITelemetryType), metricId);
            string uuidString = uniqueUserIdentifier.ToString();
            string sessionIdString  = _sessionId.ToString();
            bool trackValue = false;
            try {
                switch (metricId)
                {
                    case AITelemetryType.ApplicationType:
                    case AITelemetryType.PowerShellCreate:
                    case AITelemetryType.RemoteSessionOpen:
                        trackValue = _telemetryClient.GetMetric(metricName, "uuid", "SessionId", "Detail").TrackValue(1, uuidString, sessionIdString, data);
                        break;
                    case AITelemetryType.ExperimentalFeatureActivation:
                        string experimentalFeatureName = getExperimentalFeatureName(data);
                        trackValue = _telemetryClient.GetMetric(metricName, "uuid", "SessionId", "Detail").TrackValue(1, uuidString, sessionIdString, data);
                        break;
                    case AITelemetryType.ModuleLoad:
                        string moduleName = getModuleName(data); // This will return anonymous if the modulename is not on the report list
                        trackValue = _telemetryClient.GetMetric(metricName, "uuid", "SessionId", "Detail").TrackValue(1, uuidString, sessionIdString, moduleName);
                        break;
                    default:
                        break; // don't log anything
                }
            }
            catch {
                ; // do nothing, telemetry can't be sent
                // don't send the panic telemetry as if we have failed above, it will likely fail here.
            }
        }

        // Get the experimental feature name. If we can report it, we'll return the name, otherwise, we'll return "anonymous"
        private static string getExperimentalFeatureName(string featureNameToValidate)
        {
            foreach ( string knownModuleName in _knownModuleNames )
            {
                // An experimental feature in a module is guaranteed to start with the module name
                // but we can't look up the experimental feature in the hash because there will be more than just the module name.
                // we can't use a hashset because it's a partial comparison.
                if ( featureNameToValidate.StartsWith(knownModuleName, StringComparison.OrdinalIgnoreCase ) )
                {
                    return featureNameToValidate;
                }
            }
            return "anonymous";
        }

        // Get the module name. If we can report it, we'll return the name, otherwise, we'll return "anonymous"
        private static string getModuleName(string moduleNameToValidate)
        {
            if ( _knownModules.TryGetValue(moduleNameToValidate, out string valOut)) {
                return moduleNameToValidate;
            }
            else {
                return "anonymous";
            }
        }

        /// <summary>
        /// Create the startup payload and send it up.
        /// This is done only once during for the console host
        /// </summary>
        /// <param name="mode">The "mode" of the startup.</param>
        public static void SendPSCoreStartupTelemetry(string mode)
        {
            if ( ! CanSendTelemetry ) {
                return;
            }
            var properties = new Dictionary<string, string>();
            var channel = Environment.GetEnvironmentVariable("POWERSHELL_DISTRIBUTION_CHANNEL");

            properties.Add("SessionId", _sessionId.ToString());
            properties.Add("UUID", uniqueUserIdentifier.ToString());
            properties.Add("GitCommitID", PSVersionInfo.GitCommitId);
            properties.Add("OSDescription", RuntimeInformation.OSDescription);
            properties.Add("OSChannel", (channel == null || channel == string.Empty) ? "unknown" : channel);
            properties.Add("StartMode", (mode == null || mode == string.Empty) ? "unknown" : mode);
            try {
                _telemetryClient.TrackEvent("ConsoleHostStartup", properties, null);
            }
            catch {
                ; // do nothing, telemetry cannot be sent
            }
        }

        private static void getUserIdImpl()
        {
            string cacheDir = Platform.CacheDirectory;
            string uuidPath = Path.Join(cacheDir, "telemetry.uuid");
            const int GuidSize = 16;
            Byte[] buffer = new Byte[GuidSize];

            // Don't bother to check if the directory exists
            // we only need to catch if there is a problem and then disable telemetry
            // it's documented to throw if the directory cannot be created (and it doesn't exist)
            // If there's a problem, we can't persist the uuid
            try {
                Directory.CreateDirectory(cacheDir);
            }
            catch {
                CanSendTelemetry = false;
                _telemetryClient.GetMetric(_telemetryFailure, "Detail").TrackValue(1, "cachedir");
                return;
            }

            // attempt to read the persisted identifier
            try
            {
                using (FileStream fs = new FileStream(uuidPath, FileMode.Open, FileAccess.Read))
                {
                    // if the read is invalid, or wrong size, we return it
                    int n = fs.Read(buffer, 0, GuidSize);
                    if ( n == GuidSize )
                    {
                        uniqueUserIdentifier = new Guid(buffer);
                        // If Guid.Empty is provided, then create a new identifier
                        if ( uniqueUserIdentifier == Guid.Empty ) {
                            throw new InvalidOperationException(Guid.Empty.ToString());
                        }
                        return;
                    }
                }
            }
            catch // something went wrong, the file may not exist or not have enough bytes, so recreate the identifier
            {
                uniqueUserIdentifier = Guid.NewGuid();
            }

            // save the new identifier, and if there's a problem, disable telemetry
            try {
                File.WriteAllBytes(uuidPath, uniqueUserIdentifier.ToByteArray());
            }
            catch {
                _telemetryClient.GetMetric(_telemetryFailure, "Detail").TrackValue(1, "saveuuid");
                CanSendTelemetry = false;
            }

        }

        /// <summary>
        /// Retrieve the unique identifier from the persisted file, if it doesn't exist create it
        /// Generate a guid which will be used as the UUID
        /// </summary>
        private static Guid getUniqueIdentifier()
        {
            // although unlikely we still protect this operation
            // because multiple processes may start simultaneously so we need a system wide 
            // way to control access to the file
            using ( var m = new Mutex(true, "CreateUniqueUserId") ) {
                try {
                    m.WaitOne();
                    getUserIdImpl();
                }
                catch (Exception) {
                    // Any problem in generating a uuid will result in no telemetry being sent.
                    // Attempt to send the failure in telemetry, but it will have no unique id.
                    CanSendTelemetry = false;
                    _telemetryClient.GetMetric(_telemetryFailure, "Detail").TrackValue(1, "mutex");

                }
                finally {
                    m.ReleaseMutex();
                }
            }
            return uniqueUserIdentifier;
        }
    }
}
