# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Validate start of console host" -Tag CI {
    BeforeAll {
        $allowedAssemblies = @(
            'System.Private.CoreLib.dll'
            'pwsh.dll'
            'System.Runtime.dll'
            'Microsoft.PowerShell.ConsoleHost.dll'
            'Microsoft.Win32.Primitives.dll'
            'System.Management.Automation.dll'
            'System.Resources.ResourceManager.dll'
            'System.Collections.dll'
            'System.Threading.dll'
            'System.Threading.Thread.dll'
            'System.Runtime.Extensions.dll'
            'System.Security.Cryptography.X509Certificates.dll'
            'System.Security.Cryptography.Encoding.dll'
            'System.Net.Primitives.dll'
            'System.Net.Mail.dll'
            'System.Linq.Expressions.dll'
            'System.Runtime.InteropServices.RuntimeInformation.dll'
            'Microsoft.Management.Infrastructure.dll'
            'System.Runtime.InteropServices.dll'
            'System.Runtime.Serialization.Formatters.dll'
            'System.Runtime.Serialization.Primitives.dll'
            'System.Text.RegularExpressions.dll'
            'System.Collections.Concurrent.dll'
            'System.Private.Uri.dll'
            'System.Diagnostics.FileVersionInfo.dll'
            'System.IO.FileSystem.dll'
            'System.Xml.ReaderWriter.dll'
            'System.Text.Encoding.Extensions.dll'
            'System.Private.Xml.dll'
            'System.Runtime.Numerics.dll'
            'System.Security.AccessControl.dll'
            'System.Net.NetworkInformation.dll'
            'System.Security.Cryptography.Primitives.dll'
            'System.Buffers.dll'
            'System.Collections.Specialized.dll'
            'System.Linq.dll'
            'System.Data.Common.dll'
            'System.ComponentModel.TypeConverter.dll'
            'System.ComponentModel.dll'
            'System.ObjectModel.dll'
            'Newtonsoft.Json.dll'
            'netstandard.dll'
            'System.Runtime.Loader.dll'
            'System.Console.dll'
            'System.Diagnostics.Debug.dll'
            'Microsoft.ApplicationInsights.dll'
            'System.Diagnostics.Process.dll'
            'System.Threading.Tasks.dll'
            'System.ComponentModel.Primitives.dll'
            'System.Reflection.dll'
            'System.Diagnostics.Tracing.dll'
            'System.Diagnostics.Tools.dll'
            'System.Reflection.Extensions.dll'
            'System.Xml.XDocument.dll'
            'System.Private.Xml.Linq.dll'
            'System.Security.Principal.dll'
            'System.Globalization.dll'
            'System.Threading.Timer.dll'
            'System.Collections.NonGeneric.dll'
            'System.IO.Pipes.dll'
            'System.Security.Principal.Windows.dll'
            'Microsoft.PowerShell.Security.dll'
            'Microsoft.Win32.Registry.dll'
            'System.Threading.Tasks.Parallel.dll'
            'System.IO.FileSystem.DriveInfo.dll'
            'System.IO.FileSystem.AccessControl.dll'
            'System.Memory.dll'
            'System.Diagnostics.TraceSource.dll'
            'System.Reflection.Emit.Lightweight.dll'
            'System.Reflection.Primitives.dll'
            'System.Reflection.Emit.ILGeneration.dll'
        )

        if ($IsWindows) {
            $allowedAssemblies += @(
                'Microsoft.PowerShell.CoreCLR.Eventing.dll'
                'System.Management.dll'
                'System.DirectoryServices.dll'
                'System.Security.Claims.dll'
                'System.Threading.Overlapped.dll'
            )
        }
        else {
            $allowedAssemblies += @(
                'System.Reflection.Metadata.dll'
                'System.Collections.Immutable.dll'
                'System.IO.MemoryMappedFiles.dll'
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

        $loadedAssemblies = pwsh -noprofile -command '([System.AppDomain]::CurrentDomain.GetAssemblies()).manifestmodule | Where-Object { $_.Name -notlike ""<*>"" } | ForEach-Object { $_.Name }'
    }

    It "No new assemblies are loaded" {
        $diffs = Compare-Object -ReferenceObject $allowedAssemblies -DifferenceObject $loadedAssemblies

        if ($null -ne $diffs) {
            $assembliesAllowedButNotLoaded = $diffs | Where-Object SideIndicator -eq "<=" | ForEach-Object InputObject
            $assembliesLoadedButNotAllowed = $diffs | Where-Object SideIndicator -eq "=>" | ForEach-Object InputObject

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
