// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    /// Send up telemetry for startup.
    /// </summary>
    internal static class ApplicationInsightsTelemetry
    {
        // If this env var is true, yes, or 1, telemetry will NOT be sent.
        private const string TelemetryOptoutEnvVar = "POWERSHELL_TELEMETRY_OPTOUT";

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

        private static bool GetEnvironmentVariableAsBool(string name, bool defaultValue) {
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
        /// Send the telemetry.
        /// </summary>
        private static void SendTelemetry(string eventName, Dictionary<string,string> payload)
        {
            try
            {
                var enabled = !GetEnvironmentVariableAsBool(name : TelemetryOptoutEnvVar, defaultValue : false);

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
        /// Create the startup payload and send it up.
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
