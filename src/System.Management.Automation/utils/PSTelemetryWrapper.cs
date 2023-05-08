// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if LEGACYTELEMETRY

using System.Reflection;
using System.Diagnostics.Tracing;

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// This wrapper is for encapsulating all the internal details of Asimov-compatible telemetry in Windows Threshold.
    /// </summary>
    internal static class TelemetryWrapper
    {
        private static readonly PSObject s_eventSourceInstance;
        private static readonly object s_eventSourceOptionsForWrite;

        /// <summary>
        /// Performing EventSource initialization in the Static Constructor since this is thread safe.
        /// Static constructors are guaranteed to be run only once per application domain, before any instances of a class are created or any static members are accessed.
        /// https://docs.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/static-constructors.
        /// </summary>
        static TelemetryWrapper()
        {
            try
            {
                // We build against CLR4.5 so we can run on Win7/Win8, but we want to use apis added to CLR 4.6.
                // So we use reflection, and if that fails, we just silently skip logging our telemetry.

                var diagnosticsTracingAssembly = typeof(EventSource).Assembly;

                Type eventSourceSettingsType = diagnosticsTracingAssembly.GetType("System.Diagnostics.Tracing.EventSourceSettings");
                if (eventSourceSettingsType == null)
                {
                    // Probably on CLR 4.5.
                    return;
                }

                // Beyond here, we skip the null checks because we're pretty sure we have CLR 4.6 and everything
                // should work, but if it doesn't, we're still safe because of the catch.

                // Get the enum EventSourceSettings.EtwSelfDescribingEventFormat, but skip reflection to get the integer
                // value, that can't change.
                const int EtwSelfDescribingEventFormat = 8;
                var eventSourceSettingsEnumObject = Enum.ToObject(eventSourceSettingsType, EtwSelfDescribingEventFormat);

                // Create instance of the class EventSource with Provider name of "Microsoft-PowerShell-Telemetry"
                // Supply this eventSourceTrait to EventSource constructor to enable Asimov type events
                var eventSource = (EventSource)Activator.CreateInstance(typeof(EventSource),
                    new object[] {"Microsoft-PowerShell-Telemetry",
                                  eventSourceSettingsEnumObject,
                                  new[] { "ETW_GROUP", "{4f50731a-89cf-4782-b3e0-dce8c90476ba}" }});

                // Wrap in PSObject so we can invoke a method dynamically using our binder (the C# dynamic binder fails for some reason.)
                s_eventSourceInstance = new PSObject(eventSource);

                // Initialize EventSourceOptions for Writing informational messages
                // WdiContext will ensure Universal Telemetry Client [UTC] will upload telemetry messages to Cosmos/xPert pipeline

                // MeasuresKeyword is to indicate that event is for understanding measures and reporting scenarios.
                // This keyword results in the generation of Asimov compatible events for telemetry
                // Refer ~\minkernel\published\internal\telemetry\MicrosoftTelemetry.h
                const Int64 measuresKeyword = 0x0000400000000000;

                // Create Instance of EventSourceOptions struct
                Type eventSourceOptionsType = diagnosticsTracingAssembly.GetType("System.Diagnostics.Tracing.EventSourceOptions");
                s_eventSourceOptionsForWrite = Activator.CreateInstance(eventSourceOptionsType, null);

                // Set the Level and Keywords properties
                eventSourceOptionsType.GetProperty("Level").SetValue(s_eventSourceOptionsForWrite, EventLevel.Informational, null);
                eventSourceOptionsType.GetProperty("Keywords").SetValue(s_eventSourceOptionsForWrite, measuresKeyword, null);
            }
            catch
            {
                // If there are any exceptions, just disable tracing completely by making sure these are both null
                s_eventSourceInstance = null;
                s_eventSourceOptionsForWrite = null;
            }
        }

        internal static bool IsEnabled
        {
            get { return s_eventSourceInstance != null && ((EventSource)s_eventSourceInstance.BaseObject).IsEnabled(); }
        }

        /// <summary>
        /// TRACEMESSAGE is the Generic method to use to log messages using UTC [Universal Telemetry Client] in Windows Threshold
        /// TRACEMESSAGE calls into EventSource.Write dynamically - https://msdn.microsoft.com/library/dn823293(v=vs.110).aspx.
        /// </summary>

        // EventSource data gets raised on the Client containing OS Environment information and supplied arguments as "data"
        // Events are queued and uploaded to Cosmos/xPert.
        // Format of data generated on the Client:
        // {
        //    "ver": "2.1",
        //    "name": "Microsoft.Windows.PowerShell.CONSOLEHOST_START",
        //    "time": "2015-03-06T20:41:46.6967701Z",
        //    "popSample": 100.000000,
        //    "epoch": "5",
        //    "seqNum": 82463,
        //    "flags": 257,
        //    "os": "Windows",
        //    "osVer": "10.0.10031.0.amd64fre.winmain.150227-1817",
        //    "appId": "W:0000f519feec486de87ed73cb92d3cac802400000000!0000aec24258aebc46e867b932f05b64f025f8a07965!powershell.exe",
        //    "appVer": "2015/02/28:04:23:34!80bc5!powershell.exe",
        //    "ext": {
        //        "device": {
        //            "localId": "s:F4FDD2F5-88CD-444A-B815-19D530BF81E7",
        //            "deviceClass": "Windows.Server"
        //        },
        //        "user": {
        //            "localId": "w:72CEA7E7-5A30-ACFF-DD99-002A4B24DDDA"
        //        },
        //        "utc": {
        //            "stId": "8BA1217F-5700-4034-A79B-9AEAD39AE0BF",
        //            "aId": "C63E5819-5520-0000-4D0C-6DC62055D001",
        //            "cat": 562949953421312,
        //            "flags": 0
        //        }
        //    },
        //    "data": {
        //        "PSVersion": "5.0"
        //    }
        // }

        public static void TraceMessage<T>(string message, T arguments)
        {
            if (s_eventSourceOptionsForWrite != null)
            {
                try
                {
                    // EventSourceInstance is cast to dynamic so we can call a generic method added in CLR 4.6.
                    // We use dynamic to get the benefits of call site caching (we can avoid looking up the generic
                    // method on each call).
                    // We use a PSObject wrapper to force calling the PowerShell binder because the C# binder fails
                    // for an unknown reason.

                    // The ETW provider GUID for events written here is: 5037b0a0-3a31-5cd2-ff19-103e9f160a74
                    ((dynamic)s_eventSourceInstance).Write(message, s_eventSourceOptionsForWrite, arguments);
                }
                catch
                {
                    // No-op on issues arising from calling EventSource.Write<T>
                }
            }
        }
    }
}
#endif
