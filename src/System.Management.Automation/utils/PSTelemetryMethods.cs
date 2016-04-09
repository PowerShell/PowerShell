/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation.Internal;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.Telemetry.Internal
{
    /// <summary>
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public static class TelemetryAPI
    {
        #region Public API

        /// <summary>
        /// Public API to expose Telemetry in PowerShell
        /// Provide meaningful message. Ex: PSCONSOLE_START, PSRUNSPACE_START
        /// arguments are of anonymous type. Ex: new { PSVersion = "5.0", PSRemotingProtocolVersion = "2.2"}
        /// </summary>
        public static void TraceMessage<T>(string message, T arguments)
        {
            TelemetryWrapper.TraceMessage(message, arguments);
        }

        #endregion
    }
}