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
        // The semaphore file which indicates whether telemetry should be sent
        // This is temporary code waiting on the acceptance and implementation of the configuration spec
        /// <summary>
        /// The name of the file by when present in $PSHOME will enable telemetry.
        /// If this file is not present, no telemetry will be sent.
        /// </summary>
        public const string TelemetrySemaphoreFilename = "DELETE_ME_TO_DISABLE_CONSOLEHOST_TELEMETRY";

        // Telemetry client to be reused when we start sending more telemetry
        private static TelemetryClient telemetryClient = null;
        // TODO: Set this to false for release
        private static bool developerMode = true;
        // PSCoreInsight2 telemetry key
        private const string psCoreTelemetryKey = "ee4b2115-d347-47b0-adb6-b19c2c763808";
        /// <summary>
        /// Send the telemetry
        /// </summary>
        private static void SendTelemetry(string eventName, Dictionary<string,string>payload)
        {
            // if the semaphore file exists, send the telemetry
            string assemblyPath = Path.GetDirectoryName(typeof(PSVersionInfo).GetTypeInfo().Assembly.Location);
            string telemetrySemaphoreFilePath = Path.Combine(assemblyPath, TelemetrySemaphoreFilename);
            if ( File.Exists(telemetrySemaphoreFilePath ) )
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
        }
        /// <summary>
        /// Create the startup payload and send it up
        /// </summary>
        public static void SendPSCoreStartupTelemetry()
        {
            var properties = new Dictionary<string, string>();
            properties.Add("GitCommitID", PSVersionInfo.GitCommitId);
            properties.Add("OSVersionInfo", Environment.OSVersion.VersionString);
            SendTelemetry("PSCoreStartup", properties);
        }
    }
}
