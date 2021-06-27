// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using System.Runtime.InteropServices;

#nullable enable
namespace System.Management.Automation
{
    /// <summary>
    /// IInspectable represents the base interface for all WinRT types.
    /// Any run time class exposed through WInRT language projections
    /// like C#, VB.Net, C++ and JavaScript would have implemented
    /// the IInspectable interface.
    /// This interface is needed on long term basis to efficiently support identifying
    /// WinRT type instances created in Powershell session. Hence being
    /// included as part of SMA.
    /// The only purpose of this interface is to identify if the created object is of WinRT type.
    /// Users should not implement this interface for any custom functionalities.
    /// This is like a PInvoke. WinRT team have defined IInspectable in the COM layer.
    /// Through PInvoke this interface is being used in the managed layer.
    /// </summary>
    [Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IInspectable { }

    /// <summary>
    /// Helper class for WinRT types.
    /// </summary>
    internal static class WinRTHelper
    {
        internal static bool IsWinRTType(Type type)
        {
            // All WinRT Types would contain the Attributes flag set to TypeAttributes.WindowsRuntime
            // TypeAttributes.WindowsRuntime is part of CLR 4.5. Inorder to build PowerShell for
            // CLR 4.0, a string comparison for the for the existence of TypeAttributes.WindowsRuntime
            // in the Attributes flag is performed rather than the actual bitwise comparison.
            return type.Attributes.ToString().Contains("WindowsRuntime");
        }
    }
}
