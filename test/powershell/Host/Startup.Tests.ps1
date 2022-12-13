# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Validate start of console host" -Tag CI {
    BeforeAll {
        $allowedAssemblies = @(
            'Microsoft.ApplicationInsights.dll'
            'Microsoft.Management.Infrastructure.dll'
            'Microsoft.PowerShell.ConsoleHost.dll'
            'Microsoft.Win32.Primitives.dll'
            'Microsoft.Win32.Registry.dll'
            'netstandard.dll'
            'Newtonsoft.Json.dll'
            'pwsh.dll'
            'System.Collections.Concurrent.dll'
            'System.Collections.dll'
            'System.Collections.Specialized.dll'
            'System.ComponentModel.dll'
            'System.ComponentModel.Primitives.dll'
            'System.ComponentModel.TypeConverter.dll'
            'System.Console.dll'
            'System.Data.Common.dll'
            'System.Diagnostics.Process.dll'
            'System.Diagnostics.TraceSource.dll'
            'System.Diagnostics.Tracing.dll'
            'System.IO.FileSystem.AccessControl.dll'
            'System.IO.FileSystem.DriveInfo.dll'
            'System.IO.Pipes.dll'
            'System.Linq.dll'
            'System.Linq.Expressions.dll'
            'System.Management.Automation.dll'
            'System.Memory.dll'
            'System.Net.Mail.dll'
            'System.Net.NetworkInformation.dll'
            'System.Net.Primitives.dll'
            'System.ObjectModel.dll'
            'System.Private.CoreLib.dll'
            'System.Private.Uri.dll'
            'System.Private.Xml.dll'
            'System.Reflection.Emit.ILGeneration.dll'
            'System.Reflection.Emit.Lightweight.dll'
            'System.Reflection.Primitives.dll'
            'System.Runtime.dll'
            'System.Runtime.InteropServices.dll'
            'System.Runtime.Loader.dll'
            'System.Runtime.Numerics.dll'
            'System.Runtime.Serialization.Formatters.dll'
            'System.Runtime.Serialization.Primitives.dll'
            'System.Security.AccessControl.dll'
            'System.Security.Cryptography.dll'
            'System.Security.Principal.Windows.dll'
            'System.Text.Encoding.CodePages.dll'
            'System.Text.Encoding.Extensions.dll'
            'System.Text.RegularExpressions.dll'
            'System.Threading.dll'
            'System.Threading.Tasks.Parallel.dll'
            'System.Threading.Thread.dll'
            'System.Threading.ThreadPool.dll'
            'System.Xml.ReaderWriter.dll'
        )

        if ($IsWindows) {
            $allowedAssemblies += @(
                'Microsoft.PowerShell.CoreCLR.Eventing.dll'
                'System.DirectoryServices.dll'
                'System.Management.dll'
                'System.Security.Claims.dll'
                'System.Threading.Overlapped.dll'
            )
        }
        else {
            $allowedAssemblies += @(
                'System.Net.Sockets.dll'
            )
        }

        if ($IsWindows) {
            $profileDataFile = Join-Path $env:LOCALAPPDATA "Microsoft\PowerShell\StartupProfileData-NonInteractive"
        } else {
            $profileDataFile = Join-Path ([System.Management.Automation.Platform]::SelectProductNameForDirectory("CACHE")) "StartupProfileData-NonInteractive"
        }

        if (Test-Path $profileDataFile) {
            Remove-Item $profileDataFile -Force
        }

        $loadedAssemblies = & "$PSHOME/pwsh" -noprofile -command '([System.AppDomain]::CurrentDomain.GetAssemblies()).manifestmodule | Where-Object { $_.Name -notlike "<*>" } | ForEach-Object { $_.Name }'
    }

    It "No new assemblies are loaded" {
        if ( (Get-PlatformInfo).Platform -eq "alpine" ) {
            Set-ItResult -Pending -Because "Missing MI library causes list to be different"
            return
        }

        $diffs = Compare-Object -ReferenceObject $allowedAssemblies -DifferenceObject $loadedAssemblies

        if ($null -ne $diffs) {
            $assembliesAllowedButNotLoaded = $diffs | Where-Object SideIndicator -EQ "<=" | ForEach-Object InputObject
            $assembliesLoadedButNotAllowed = $diffs | Where-Object SideIndicator -EQ "=>" | ForEach-Object InputObject

            if ($assembliesAllowedButNotLoaded) {
                Write-Host ("Assemblies that are expected but not loaded: {0}" -f ($assembliesAllowedButNotLoaded -join ", "))
            }
            if ($assembliesLoadedButNotAllowed) {
                Write-Host ("Assemblies that are loaded but not expected: {0}" -f ($assembliesLoadedButNotAllowed -join ", "))
            }
        }

        $diffs | Should -BeExactly $null
    }
}
