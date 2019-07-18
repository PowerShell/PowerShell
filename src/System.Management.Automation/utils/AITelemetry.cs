// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// The type of tracking for our telemetry
    /// </summary>
    public enum AITelemetryType
    {
        /// <summary>
        /// Telemetry for execution
        /// </summary>
        PipelineExecution,
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
        PowerShellStart,
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
        private const string TelemetryOptoutEnvVar = "POWERSHELL_TELEMETRY_OPTOUT";

        // Telemetry client to be reused when we start sending more telemetry
        private static TelemetryClient _telemetryClient = new TelemetryClient();

        // Set this to true to reduce the latency of sending the telemetry
        private static bool _developerMode = true;

        // PSCoreInsight2 telemetry key
        // private const string _psCoreTelemetryKey = "ee4b2115-d347-47b0-adb6-b19c2c763808"; // Production
        private const string _psCoreTelemetryKey = "d26a5ef4-d608-452c-a6b8-a4a55935f70d"; // Dev

        // the unique identifier for the user, when we start we
        private static Guid uniqueUserIdentifier = Guid.Empty;

        // the session identifier
        private static Guid sessionId = Guid.NewGuid();

        private static string[] knownModuleNames = new string[] {
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
        public static bool CanSendTelemetry = GetEnvironmentVariableAsBool(name: TelemetryOptoutEnvVar, defaultValue: false);

        private static HashSet<string> knownModules = new HashSet<string>(knownModuleNames, StringComparer.OrdinalIgnoreCase);

        static ApplicationInsightsTelemetry()
        {
            TelemetryConfiguration.Active.InstrumentationKey = _psCoreTelemetryKey;
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = _developerMode;
        }

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
        /// Send the telemetry as a custom event.
        /// </summary>
        private static void SendTelemetry(string eventName, Dictionary<string, string> payload)
        {
            try
            {
                // var enabled = !GetEnvironmentVariableAsBool(name: TelemetryOptoutEnvVar, defaultValue: false);

                if (! CanSendTelemetry)
                {
                    return;
                }

                _telemetryClient.TrackEvent(eventName, payload, null);
            }
            catch (Exception)
            {
                ; // Do nothing, telemetry can't be sent
            }
        }

        /// <summary>send telemetry as a metric</summary>
        public static void SendTelemetryMetric(AITelemetryType metricId, string dim1, string dim2)
        {
            // var enabled = !GetEnvironmentVariableAsBool(name: TelemetryOptoutEnvVar, defaultValue: false);

            if (! CanSendTelemetry)
            {
                return;
            }

            string metricName = Enum.GetName(typeof(AITelemetryType), metricId);
            _telemetryClient.GetMetric(metricName, "execution", "name").TrackValue(1, dim1, dim2);
        }

        /// <summary>
        /// Send telemetry as a metric
        /// </summary>
        public static void SendTelemetryMetric(AITelemetryType metricId, string dimension1)
        {
            var enabled = !GetEnvironmentVariableAsBool(name: TelemetryOptoutEnvVar, defaultValue: false);

            if (!enabled)
            {
                return;
            }

            string uuid;
            try {
                uuid = GetUniqueUserId();
            }
            catch {
                return;
            }


            string metricName = Enum.GetName(typeof(AITelemetryType), metricId);
            switch (metricId)
            {
                case AITelemetryType.ApplicationType:
                    _telemetryClient.GetMetric(metricName, "uuid", "SessionId", "AppType").TrackValue(1, uuid, sessionId.ToString(), dimension1);
                    break;
                case AITelemetryType.ExperimentalFeatureActivation:
                    _telemetryClient.GetMetric(metricName, "uuid", "SessionId", "FeatureName").TrackValue(1, uuid, sessionId.ToString(), dimension1);
                    break;
                case AITelemetryType.ModuleLoad:
                    string moduleName = getModuleName(dimension1); // This will return anonymous if the modulename is not on the report list
                    _telemetryClient.GetMetric(metricName, "uuid", "SessionId", "ModuleName").TrackValue(1, uuid, sessionId.ToString(), moduleName);
                    break;
                case AITelemetryType.PowerShellStart:
                    _telemetryClient.GetMetric(metricName, "uuid", "SessionId", "EngineStart").TrackValue(1, uuid, sessionId.ToString(), dimension1);
                    break;
                case AITelemetryType.RemoteSessionOpen:
                    _telemetryClient.GetMetric(metricName, "uuid", "SessionId", "RemoteSessionOpen").TrackValue(1, uuid, sessionId.ToString(), dimension1);
                    break;
                default:
                    break; // don't log anything, definitely don't throw
            }
        }

        private static string getModuleName(string moduleNameToValidate)
        {
            if ( knownModules.TryGetValue(moduleNameToValidate, out string valOut)) {
                return moduleNameToValidate;
            }
            else {
                return "anonymous";
            }
        }

        private static bool canReportExperimentalFeature(string experimentalFeatureName)
        {
            // don't bother to check if we can't send telemetry
            if ( ! CanSendTelemetry ) 
            {
                return false;
            }

            foreach ( string knownModuleName in knownModuleNames )
            {
                // An experimental feature in a module is guaranteed to start with the module name
                // but we can't look up the experimental feature in the hash because there will be more than just the module name.
                if ( experimentalFeatureName.StartsWith(knownModuleName, StringComparison.OrdinalIgnoreCase ) )
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Create the startup payload and send it up.
        /// This is done only once during for the console host
        /// </summary>
        /// <param name="mode">The "mode" of the startup.</param>
        public static void SendPSCoreStartupTelemetry(string mode)
        {
            var properties = new Dictionary<string, string>();
            properties.Add("SessionId", sessionId.ToString());
            properties.Add("UUID", GetUniqueUserId());
            properties.Add("GitCommitID", PSVersionInfo.GitCommitId);
            properties.Add("OSDescription", RuntimeInformation.OSDescription);
            properties.Add("OSDetail", Environment.GetEnvironmentVariable("PSDestChannel"));
            if ( mode == null || mode == string.Empty ) {
                properties.Add("StartMode", "unknown");
            }
            else {
                properties.Add("StartMode", mode);
            }
            SendTelemetry("ConsoleHostStartup", properties);
        }

        private static void getUserIdImpl()
        {

            string cacheDir = Platform.CacheDirectory;
            string uuidPath = Path.Join(cacheDir, "telemetry.uuid");
            Byte[] buffer = new Byte[16];

            if ( ! Directory.Exists(cacheDir) )
            {
                Directory.CreateDirectory(cacheDir);
            }

            try
            {
                using (FileStream fs = new FileStream(uuidPath, FileMode.Open, FileAccess.Read))
                {
                    // if the read is invalid, or wrong size, throw everything out
                    int n = fs.Read(buffer, 0, 16);
                    if ( n == 16 )
                    {
                        uniqueUserIdentifier = new Guid(buffer);
                    }
                    else
                    {
                        throw new FileNotFoundException(uuidPath);
                    }
                }
            }
            catch ( FileNotFoundException )
            {
                uniqueUserIdentifier = Guid.NewGuid();
                File.WriteAllBytes(uuidPath, uniqueUserIdentifier.ToByteArray());
            }

        }

        /// <summary>
        /// Retrieve the user id from the persisted file, if it doesn't exist create it
        /// Generate a guid which will be used as the UUID
        /// </summary>
        private static string GetUniqueUserId()
        {
            if ( uniqueUserIdentifier != Guid.Empty ) {
                return uniqueUserIdentifier.ToString();
            }

            // this is very unlikely but we still protect it
            Mutex m = null;
            try {
                m = new Mutex(true, "CreateUniqueUserId");
                m.WaitOne(10);
                getUserIdImpl();
            }
            catch (AbandonedMutexException) {
                // if the mutex was abandoned, be sure to not let it spoil our day
            }
            catch ( TimeoutException ) {
            }
            finally {
                m.ReleaseMutex();
            }
            return uniqueUserIdentifier.ToString();

        }
    }
}
