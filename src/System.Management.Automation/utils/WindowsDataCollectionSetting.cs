// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System;
using System.Management.Automation.Internal;
using System.Management.Automation.Tracing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.PowerShell.Telemetry;

internal enum PlatformDataCollectionLevel : int
{
    /// <summary>
    /// Minimum — only security-related data. Enterprise/education editions only.
    /// </summary>
    Security = 0,

    /// <summary>
    /// Device info, capabilities, and basic reliability data.
    /// </summary>
    Basic = 1,

    /// <summary>
    /// More detailed usage and reliability data, including app/feature usage patterns.
    /// Removed as a user-facing option in Windows 11 (collapsed into Full).
    /// </summary>
    Enhanced = 2,

    /// <summary>
    /// All of the above plus advanced diagnostics data that can help Microsoft fix problems.
    /// </summary>
    Full = 3,
}

/// <summary>
/// Minimal projection of <c>IInspectable</c>, the base interface for all WinRT objects.
/// Slots 3–5 in every WinRT interface vtable (after <c>IUnknown</c>'s QueryInterface/AddRef/Release).
/// </summary>
[GeneratedComInterface]
[Guid("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90")]
internal partial interface IInspectable
{
    void GetIids(out uint iidCount, out nint iids);

    nint GetRuntimeClassName();

    int GetTrustLevel();
}

/// <summary>
/// Projection of the WinRT interface <c>Windows.System.Profile.IPlatformDiagnosticsAndUsageDataSettingsStatics</c>
/// (IID B6E24C1B-7B1C-4B32-8C62-A66597CE723A).
/// Vtable slots 6–9, following the three <c>IInspectable</c> slots.
/// </summary>
[GeneratedComInterface]
[Guid("B6E24C1B-7B1C-4B32-8C62-A66597CE723A")]
internal partial interface IPlatformDiagnosticsAndUsageDataSettingsStatics : IInspectable
{
    PlatformDataCollectionLevel GetCollectionLevel();

    long AddCollectionLevelChanged(nint handler);

    void RemoveCollectionLevelChanged(long token);

    // WinRT marshals bool as a byte; use byte to avoid any MarshalAs ambiguity with the source generator.
    byte CanCollectDiagnostics(PlatformDataCollectionLevel level);
}

/// <summary>
/// Wraps <c>Windows.System.Profile.PlatformDiagnosticsAndUsageDataSettings</c> using compile-time COM interop
/// and source-generated P/Invoke. No extra runtime DLLs are required.
/// </summary>
internal static partial class WindowsDataCollectionSetting
{
    /// <summary>
    /// Returns <see langword="true"/> if the device's diagnostic data collection policy permits collecting at or above <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The minimum <see cref="PlatformDataCollectionLevel"/> to test against.</param>
    internal static bool CanCollectDiagnostics(PlatformDataCollectionLevel level)
    {
        const string ClassName = "Windows.System.Profile.PlatformDiagnosticsAndUsageDataSettings";

        // When initializing WinRT on the calling thread, use the multi-threaded apartment (MTA).
        // This is to cover the case where PowerShell gets used in a thread-pool thread.
        // See the doc at https://learn.microsoft.com/windows/win32/api/roapi/ne-roapi-ro_init_type
        const int RO_INIT_MULTITHREADED = 1;

        // Return values for 'RoInitialize':
        //  - S_OK (0)            - we successfully initialized; must call 'RoUninitialize'.
        //  - S_FALSE (1)         - already initialized with the same apartment type; must still call 'RoUninitialize'.
        //  - RPC_E_CHANGED_MODE  - already initialized with a different apartment type; WinRT still works, do NOT call 'RoUninitialize'.
        const int RPC_E_CHANGED_MODE = unchecked((int)0x80010106);

        int initHr = -1;
        nint hstring = default;
        nint factoryPtr = default;

        try
        {
            // Initialize WinRT on the calling thread. 'RoGetActivationFactory' requires it.
            initHr = RoInitialize(RO_INIT_MULTITHREADED);
            if (initHr < 0 && initHr != RPC_E_CHANGED_MODE)
            {
                // The call to initialize the Windows Runtime failed.
                // Throw an exception with the HRESULT error code to provide more context on the failure.
                Marshal.ThrowExceptionForHR(initHr);
            }

            Marshal.ThrowExceptionForHR(
                WindowsCreateString(ClassName, (uint)ClassName.Length, out hstring));

            Guid iid = new("B6E24C1B-7B1C-4B32-8C62-A66597CE723A");
            Marshal.ThrowExceptionForHR(
                RoGetActivationFactory(hstring, ref iid, out factoryPtr));

            var comWrappers = new StrategyBasedComWrappers();
            var comObject = comWrappers.GetOrCreateObjectForComInstance(factoryPtr, CreateObjectFlags.None);
            var platformSetting = (IPlatformDiagnosticsAndUsageDataSettingsStatics)comObject;

            return platformSetting.CanCollectDiagnostics(level) != 0;
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur during this process, but swallow them and return false to disable telemetry rather than crashing the product.
            // This API is only used to gate telemetry collection, so failure should be non-fatal.
            PSEtwLog.LogOperationalError(
                PSEventId.Telemetry_Setting_Error,
                PSOpcode.Exception,
                PSTask.Telemetry,
                PSKeyword.UseAlwaysOperational,
                ex.GetType().FullName,
                ex.Message,
                ex.StackTrace);

            return false;
        }
        finally
        {
            if (factoryPtr != default)
            {
                Marshal.Release(factoryPtr);
            }

            if (hstring != default)
            {
                _ = WindowsDeleteString(hstring);
            }

            // Per COM documentation: Each successful call to 'RoInitialize' (including S_FALSE)
            // must be balanced by a corresponding call to 'RoUninitialize'.
            if (initHr >= 0)
            {
                RoUninitialize();
            }
        }
    }

    [LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll", StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int WindowsCreateString(
        string sourceString,
        uint length,
        out nint hstring);

    [LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int WindowsDeleteString(nint hstring);

    [LibraryImport("api-ms-win-core-winrt-l1-1-0.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int RoGetActivationFactory(nint activatableClassId, ref Guid iid, out nint factory);

    [LibraryImport("api-ms-win-core-winrt-l1-1-0.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int RoInitialize(int initType);

    [LibraryImport("api-ms-win-core-winrt-l1-1-0.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial void RoUninitialize();
}

#endif
