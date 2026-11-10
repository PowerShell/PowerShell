// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{

    /// <summary>
    /// Shared helpers for the service cmdlets on Unix platforms.
    /// </summary>
    internal static class UnixServiceCmdletHelper
    {
        /// <summary>
        /// Adds the default display property set to the service so that it is formatted
        /// similarly to the Windows output of Get-Service. The member set is only added
        /// when it is not already present, since the same service instance can be
        /// emitted multiple times (for example when listing dependencies).
        /// </summary>
        /// <param name="service">The service.</param>
        /// <returns>The service as a PSObject with the default display property set.</returns>
        internal static PSObject AddDefaultDisplayProperties(UnixServiceController service)
        {
            PSObject serviceAsPSObj = PSObject.AsPSObject(service);
            if (serviceAsPSObj.Members["PSStandardMember"] is null)
            {
                PSMemberInfo[] defaultPropertySet = new PSMemberInfo[]
                {
                    new PSPropertySet("DefaultDisplayPropertySet", new string[] { "Status", "Name", "DisplayName", "StartType" }),
                };
                serviceAsPSObj.Members.Add(new PSMemberSet("PSStandardMember", defaultPropertySet));
            }

            return serviceAsPSObj;
        }
    }
}

#endif
