using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Management.Automation
{
    /// <summary>
    /// 
    /// Represents a System.Management.Automation.PowerShell object  
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct PSPowerShellPipeline
    {
        internal Boolean IsNested;
        internal Boolean NoInput;
        internal Boolean AddToHistory;
        internal uint ApartmentState;
        /// <summary>
        /// Instance Id
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string InstanceId;
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        internal IntPtr Commands;
    }

    /// <summary>
    /// 
    /// Represents a System.Management.Automation.Command object  
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct PS_Command
    {
        /// <summary>
        /// Command Text
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string CommandText;
        internal Boolean IsScript;
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        internal IntPtr Parameters;
    }

    /// <summary>
    /// 
    /// Represents a System.Management.Automation.Runspaces.CommandParameter object  
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct PS_Parameter
    {
        /// <summary>
        /// Parameter Name
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string Name;
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        internal IntPtr Value;
    }
}