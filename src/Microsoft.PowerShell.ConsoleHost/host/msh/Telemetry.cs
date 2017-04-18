using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Management.Automation;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// send up telemetry for startup
    /// </summary>
    public class ApplicationInsightsTelemetry
    {
        // Telemetry client to be reused when we start sending more telemetry
        private static TelemetryClient telemetryClient = null;
        // TODO: Set this to false for release
        private static bool developerMode = true;
        // PSCoreInsight2 telemetry key
        private const string psCoreTelemetryKey = "ee4b2115-d347-47b0-adb6-b19c2c763808"; // PSCoreInsight2
        /// <summary>
        /// Send the telemetry
        /// </summary>
        private static void SendTelemetry(string eventName, Dictionary<string,string>payload) 
        {
            TelemetryConfiguration.Active.InstrumentationKey = psCoreTelemetryKey;
            // This is set to be sure that the telemetry is quickly delivered
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = developerMode;
            if ( telemetryClient == null ) 
            {
                telemetryClient = new TelemetryClient();
            }
            telemetryClient.TrackEvent(eventName, payload, null);
        }
        /// <summary>
        /// Create the startup payload and send it up
        /// </summary>
        public static void SendPSCoreStartupTelemetry()
        {
            var properties = new Dictionary<string, string>();
            properties.Add("GitCommitID", PSVersionInfo.GitCommitId);
            string OSVersion;
            try {
                OSVersion = Environment.OSVersion.ToString();
            }
            catch {
                OSVersion = "unknown";
            }
            properties.Add("OSVersionInfo", OSVersion);
            SendTelemetry("PSCoreStartup", properties);
        }
    }
}
