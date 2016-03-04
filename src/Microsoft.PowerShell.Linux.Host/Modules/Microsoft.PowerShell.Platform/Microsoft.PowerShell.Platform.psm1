# Use the .NET Core APIs to determine the current platform; if a runtime
# exception is thrown, we are on FullCLR, not .NET Core.
try {
    $Runtime = [System.Runtime.InteropServices.RuntimeInformation]
    $OSPlatform = [System.Runtime.InteropServices.OSPlatform]

    $IsCore = $true
    $IsLinux = $Runtime::IsOSPlatform($OSPlatform::Linux)
    $IsOSX = $Runtime::IsOSPlatform($OSPlatform::OSX)
    $IsWindows = $Runtime::IsOSPlatform($OSPlatform::Windows)
} catch [System.Management.Automation.RuntimeException] {
    $IsCore = $false
    $IsLinux = $false
    $IsOSX = $false
    $IsWindows = $true
}

Export-ModuleMember -Variable IsCore, IsLinux, IsOSX, IsWindows
