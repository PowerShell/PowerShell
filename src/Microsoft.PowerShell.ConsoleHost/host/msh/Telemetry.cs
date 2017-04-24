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

        public static string TelemetrySemaphoreFilePath = Path.Combine(
            Path.GetDirectoryName(typeof(PSVersionInfo).GetTypeInfo().Assembly.Location),
            TelemetrySemaphoreFileName);

        // Telemetry client to be reused when we start sending more telemetry
        private static TelemetryClient _telemetryClient = null;

        // Set this to true to reduce the latency of sending the telemetry
        private static bool _developerMode = false;

        // PSCoreInsight2 telemetry key
        private const string _psCoreTelemetryKey = "ee4b2115-d347-47b0-adb6-b19c2c763808";

        /// <summary>
        /// Send the telemetry
        /// </summary>
        private static void SendTelemetry(string eventName, Dictionary<string,string>payload)
        {
            // if the semaphore file exists, try to send telemetry
            if ( File.Exists(TelemetrySemaphoreFilePath ) )
            {
                try
                {
                    TelemetryConfiguration.Active.InstrumentationKey = _psCoreTelemetryKey;
                    TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = _developerMode;
                    if ( _telemetryClient == null )
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
        }

        /// <summary>
        /// Create the startup payload and send it up
        /// </summary>
        public static void SendPSCoreStartupTelemetry()
        {
            var properties = new Dictionary<string, string>();
            properties.Add("GitCommitID", PSVersionInfo.GitCommitId);
            properties.Add("OSVersionInfo", Environment.OSVersion.VersionString);
            SendTelemetry("ConsoleHostStartup", properties);
        }
    }
}
