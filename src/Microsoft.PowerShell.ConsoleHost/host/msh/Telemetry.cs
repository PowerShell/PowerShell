using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Management.Automation;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// send up telemetry for startup
    /// </summary>
    internal static class ApplicationInsightsTelemetry
    {
        // The semaphore file which indicates whether telemetry should be sent
        // This is temporary code waiting on the acceptance and implementation of the configuration spec
        // The name of the file by when present in $PSHOME will enable telemetry.
        // If this file is not present, no telemetry will be sent.
        private const string TelemetrySemaphoreFilename = "DELETE_ME_TO_DISABLE_CONSOLEHOST_TELEMETRY";
        private const string TelemetryOptoutEnvVar = "POWERSHELL_TELEMETRY_OPTOUT";

        // The path to the semaphore file which enables telemetry
        private static string TelemetrySemaphoreFilePath = Path.Combine(
            Utils.DefaultPowerShellAppBase,
            TelemetrySemaphoreFilename);

        // Telemetry client to be reused when we start sending more telemetry
        private static TelemetryClient _telemetryClient = null;

        // Set this to true to reduce the latency of sending the telemetry
        private static bool _developerMode = false;

        // PSCoreInsight2 telemetry key
        private const string _psCoreTelemetryKey = "ee4b2115-d347-47b0-adb6-b19c2c763808";

        static ApplicationInsightsTelemetry()
        {
            TelemetryConfiguration.Active.InstrumentationKey = _psCoreTelemetryKey;
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = _developerMode;
        }

        internal static bool GetEnvironmentVariableAsBool(string name, bool defaultValue) {
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
        /// Send the telemetry
        /// </summary>
        private static void SendTelemetry(string eventName, Dictionary<string,string>payload)
        {
            try
            {
                // if the semaphore file exists, try to send telemetry
                var enabled = Utils.NativeFileExists(TelemetrySemaphoreFilePath) && !GetEnvironmentVariableAsBool(TelemetryOptoutEnvVar, false);

                if (!enabled)
                {
                    return;
                }

                if (_telemetryClient == null)
                {
                    _telemetryClient = new TelemetryClient();
                }
                _telemetryClient.TrackEvent(eventName, payload, null);
            }
            catch (Exception)
            {
                ; // Do nothing, telemetry can't be sent
            }
        }

        /// <summary>
        /// Create the startup payload and send it up
        /// </summary>
        internal static void SendPSCoreStartupTelemetry()
        {
            var properties = new Dictionary<string, string>();
            properties.Add("GitCommitID", PSVersionInfo.GitCommitId);
            properties.Add("OSDescription", RuntimeInformation.OSDescription);
            SendTelemetry("ConsoleHostStartup", properties);
        }
    }
}
