using System.Runtime.InteropServices;
using Microsoft.Management.Infrastructure;

namespace System.Management.Automation
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct PSNegotiationData
    {
        /// <summary>
        /// PowerShell version
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string PSVersion;
    }

    internal static class PSNegotiationHandler
    {
        internal static CimInstance CreatePSNegotiationData(Version powerShellVersion)
        {
            CimInstance c = InternalMISerializer.CreateCimInstance("PS_NegotiationData");
            CimProperty versionproperty = InternalMISerializer.CreateCimProperty("PSVersion", powerShellVersion.ToString(), Microsoft.Management.Infrastructure.CimType.String);
            c.CimInstanceProperties.Add(versionproperty);
            return c;
        }
    }
}